import { existsSync, readFileSync } from 'node:fs';
import { createServer, type Server } from 'node:http';
import { fileURLToPath } from 'node:url';
import { parseEnv } from 'node:util';
import { defineConfig, type Plugin, type PreviewServer, type ViteDevServer } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import { readWebSettings, webApiProxyPattern } from '../../tools/start-web.mjs';

export default defineConfig(({ mode }) => {
  const { environment, hostname, port, routes } = readWebSettings(mode === 'production' ? 'Production' : 'Local');
  const envFile = new URL('./.env', import.meta.url);
  const env = { ...process.env, ...(existsSync(envFile) ? parseEnv(readFileSync(envFile, 'utf8')) : {}) };
  const apiUrl = new URL(env.API_URL ?? 'http://localhost:3100');
  const local = environment === 'Local';
  const apiPort = Number(apiUrl.port || (apiUrl.protocol === 'https:' ? 443 : 80));
  if (local && apiUrl.protocol !== 'http:') throw new Error('Local API_URL must use http:// for the Vite proxy.');
  if (local && apiPort === port) throw new Error('API_URL and WEB_PORT must use different ports.');

  const apiRoutes = routes.map((route) => {
    const target = new URL(route.baseUrl.origin);
    if (local && target.origin === apiUrl.origin) {
      const targetPort = apiPort + (route.key === 'CORE' ? 1 : Number(route.key.slice(2)) + 1);
      if (!Number.isInteger(targetPort) || targetPort > 65535 || targetPort === port) {
        throw new Error(`Invalid local upstream port for ${route.key}: ${targetPort}.`);
      }
      target.port = String(targetPort);
    }
    return { ...route, target: target.origin, pattern: webApiProxyPattern(route.prefix) };
  });
  const proxy = Object.fromEntries(apiRoutes.map((route) => [route.pattern, { target: route.target, changeOrigin: true }]));
  const cors = { origin: `http://${hostname}:${port}`, exposedHeaders: ['Content-Disposition'] };
  let apiServer: Server | undefined;

  const closeApiProxy = async () => {
    const server = apiServer;
    apiServer = undefined;
    if (!server) return;
    await new Promise<void>((resolve, reject) => {
      server.close((error) => error ? reject(error) : resolve());
      server.closeAllConnections();
    });
  };

  const configureApiProxy = async (server: ViteDevServer | PreviewServer) => {
    const patterns = apiRoutes.map((route) => new RegExp(route.pattern));
    const listener = createServer((request, response) => {
      if (!patterns.some((pattern) => pattern.test(request.url ?? ''))) {
        response.writeHead(404, { 'Content-Type': 'application/json' });
        response.end(JSON.stringify({ error: 'API route not found.' }));
        return;
      }
      server.middlewares(request, response);
    });
    await new Promise<void>((resolve, reject) => {
      listener.once('error', reject);
      listener.listen(apiPort, apiUrl.hostname.replace(/^\[|\]$/g, ''), () => {
        listener.off('error', reject);
        resolve();
      });
    });
    apiServer = listener;
    server.httpServer?.once('close', () => {
      void closeApiProxy().catch((error: unknown) => server.config.logger.error(String(error)));
    });
    server.config.logger.info(`Web API proxy: ${apiUrl.origin}\n${apiRoutes.map((route) => `  ${route.prefix} -> ${route.target}${route.prefix}`).join('\n')}`);
  };

  const apiProxyPlugin: Plugin = {
    name: 'web-api-proxy',
    apply: 'serve',
    configureServer: configureApiProxy,
    configurePreviewServer: configureApiProxy,
    closeBundle: closeApiProxy,
  };

  return {
    root: import.meta.dirname,
    envDir: false,
    define: {
      'import.meta.env.API_BASE_URLS': JSON.stringify(Object.fromEntries(routes.map((route) => [route.key, route.baseUrl.href]))),
    },
    cacheDir: 'node_modules/.vite',
    plugins: [tailwindcss(), react(), ...(local ? [apiProxyPlugin] : [])],
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
      },
    },
    server: {
      host: hostname,
      port,
      strictPort: true,
      cors,
      proxy,
    },
    preview: {
      host: hostname,
      port,
      strictPort: true,
      cors,
      proxy,
    },
    build: {
      outDir: 'dist',
      emptyOutDir: true,
    },
  };
});

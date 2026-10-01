import { fileURLToPath } from 'node:url';
import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import { apiProxyPattern, apiRouting } from '../../tools/api-topology.mjs';
import { readApiSettings, resolveApiEnvironment } from '../../tools/api-settings.mjs';

export default defineConfig(({ command, mode }) => {
  const workspaceRoot = fileURLToPath(new URL('../..', import.meta.url));
  const env = { ...loadEnv(mode, workspaceRoot, ''), ...process.env };
  const port = Number(env.WEB_PORT ?? '3000');
  const routing = command === 'serve'
    ? apiRouting(env, readApiSettings({ ...env, DOTNET_ENVIRONMENT: resolveApiEnvironment(env, mode === 'production' ? 'Production' : 'Local') }))
    : undefined;

  if (!Number.isInteger(port) || port < 1 || port > 65535) {
    throw new Error('WEB_PORT must be an integer between 1 and 65535.');
  }
  if (routing?.gatewayPort === port) throw new Error('Api:GatewayPort/API_PORT must be different from WEB_PORT.');

  return {
    root: import.meta.dirname,
    envDir: workspaceRoot,
    cacheDir: '../../node_modules/.vite/apps/web',
    plugins: [tailwindcss(), react()],
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
      },
    },
    server: {
      host: 'localhost',
      port,
      strictPort: true,
      proxy: routing ? {
        [apiProxyPattern(routing)]: {
          target: `http://127.0.0.1:${routing.gatewayPort}`,
          changeOrigin: true,
        },
      } : undefined,
    },
    preview: {
      host: 'localhost',
      port,
      strictPort: true,
    },
    build: {
      outDir: '../../dist/apps/web',
      emptyOutDir: true,
    },
  };
});

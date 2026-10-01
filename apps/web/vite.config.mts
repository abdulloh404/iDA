import { fileURLToPath } from 'node:url';
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import { apiProxyPattern, apiRouting, readApiSettings } from '../../tools/start-api.mjs';
import { readWebSettings } from '../../tools/start-web.mjs';

export default defineConfig(({ command, mode }) => {
  const { env, environment, hostname, port, apiUrl } = readWebSettings(mode === 'production' ? 'Production' : 'Local');
  const apiEnv = { ...env, DOTNET_ENVIRONMENT: environment, ASPNETCORE_ENVIRONMENT: environment };
  const routing = command === 'serve'
    ? apiRouting(apiEnv, readApiSettings(apiEnv))
    : undefined;
  const proxy = routing ? {
    [apiProxyPattern(routing)]: { target: apiUrl.origin, changeOrigin: true },
  } : undefined;

  return {
    root: import.meta.dirname,
    envDir: false,
    cacheDir: 'node_modules/.vite',
    plugins: [tailwindcss(), react()],
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
      },
    },
    server: {
      host: hostname,
      port,
      strictPort: true,
      proxy,
    },
    preview: {
      host: hostname,
      port,
      strictPort: true,
      proxy,
    },
    build: {
      outDir: 'dist',
      emptyOutDir: true,
    },
  };
});

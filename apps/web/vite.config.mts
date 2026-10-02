import { fileURLToPath } from 'node:url';
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import { readWebSettings, webApiProxyPattern } from '../../tools/start-web.mjs';

export default defineConfig(({ mode }) => {
  const { hostname, port, routes } = readWebSettings(mode === 'production' ? 'Production' : 'Local');
  const proxy = Object.fromEntries(routes.map((route) => [webApiProxyPattern(route.prefix), { target: route.baseUrl.origin, changeOrigin: true }]));

  return {
    root: import.meta.dirname,
    envDir: false,
    define: {
      'import.meta.env.API_BASE_URLS': JSON.stringify(Object.fromEntries(routes.map((route) => [route.key, route.baseUrl.href]))),
    },
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

import { fileURLToPath } from 'node:url';
import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

export default defineConfig(({ mode }) => {
  const workspaceRoot = fileURLToPath(new URL('../..', import.meta.url));
  const env = loadEnv(mode, workspaceRoot, 'WEB_PORT');
  const port = Number(process.env.WEB_PORT ?? env.WEB_PORT ?? '3000');

  if (!Number.isInteger(port) || port < 1 || port > 65535) {
    throw new Error('WEB_PORT must be an integer between 1 and 65535.');
  }

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
      host: '127.0.0.1',
      port,
      strictPort: true,
    },
    preview: {
      host: '127.0.0.1',
      port,
      strictPort: true,
    },
    build: {
      outDir: '../../dist/apps/web',
      emptyOutDir: true,
    },
  };
});

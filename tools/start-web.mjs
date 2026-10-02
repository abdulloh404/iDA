import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const result = spawnSync('npm run dev', {
  cwd: fileURLToPath(new URL('../apps/web/', import.meta.url)),
  stdio: 'inherit',
  shell: true,
});

if (result.error) throw result.error;
process.exitCode = result.status ?? 1;

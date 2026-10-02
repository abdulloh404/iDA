import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const windows = process.platform === 'win32';
const args = process.argv.slice(2);

const result = spawnSync(windows ? (process.env.ComSpec ?? 'cmd.exe') : 'bash', windows ? ['/d', '/s', '/c', 'start-api.cmd', ...args] : ['start-api.sh', ...args], {
  cwd: fileURLToPath(new URL('../apps/api/', import.meta.url)),
  stdio: 'inherit',
});

if (result.error) throw result.error;
process.exitCode = result.status ?? 1;

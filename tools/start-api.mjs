import { spawn } from 'node:child_process';
import { constants } from 'node:os';
import { fileURLToPath } from 'node:url';

const windows = process.platform === 'win32';
const args = process.argv.slice(2);
if (windows && args.some((argument) => !/^[a-zA-Z0-9_-]+$/.test(argument))) {
  throw new Error('Use start-api [dev|serve|start] [--environment Local|Development|Production].');
}

const child = spawn(windows ? (process.env.ComSpec ?? 'cmd.exe') : 'bash', windows ? ['/d', '/s', '/c', ['start-api.cmd', ...args].join(' ')] : ['start-api.sh', ...args], {
  cwd: fileURLToPath(new URL('../apps/api/', import.meta.url)),
  stdio: 'inherit',
});
let launchError;
let forwardedSignal;
const signalHandlers = new Map();

for (const signal of ['SIGINT', 'SIGTERM']) {
  const handler = () => {
    forwardedSignal ??= signal;
    child.kill(signal);
  };
  signalHandlers.set(signal, handler);
  process.on(signal, handler);
}

child.once('error', (error) => {
  launchError = error;
  process.stderr.write(`Unable to start API: ${error.message}\n`);
});

child.once('close', (code, signal) => {
  for (const [name, handler] of signalHandlers) process.off(name, handler);
  const exitSignal = forwardedSignal ?? signal;
  process.exitCode = launchError ? 1 : exitSignal ? 128 + (constants.signals[exitSignal] ?? 1) : code ?? 1;
});

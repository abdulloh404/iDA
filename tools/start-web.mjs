import { spawn } from 'node:child_process';
import { constants } from 'node:os';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

function startWeb() {
  const windows = process.platform === 'win32';
  const child = spawn(windows ? (process.env.ComSpec ?? 'cmd.exe') : 'npm', windows ? ['/d', '/s', '/c', 'npm.cmd run dev'] : ['run', 'dev'], {
    cwd: fileURLToPath(new URL('../apps/web/', import.meta.url)),
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
    process.stderr.write(`Unable to start Web: ${error.message}\n`);
  });
  child.once('close', (code, signal) => {
    for (const [name, handler] of signalHandlers) process.off(name, handler);
    const exitSignal = forwardedSignal ?? signal;
    process.exitCode = launchError ? 1 : exitSignal ? 128 + (constants.signals[exitSignal] ?? 1) : code ?? 1;
  });
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  startWeb();
}

import { once } from 'node:events';
import { spawn } from 'node:child_process';
import { createRequire } from 'node:module';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const rawPort = process.env.WEB_PORT ?? process.env.PORT ?? '3000';
const port = Number(rawPort);

if (!Number.isInteger(port) || port < 1 || port > 65535) {
  throw new Error(
    `WEB_PORT/PORT must be an integer between 1 and 65535; received ${rawPort}.`,
  );
}

const hostname = process.env.HOSTNAME || '127.0.0.1';
const require = createRequire(import.meta.url);
const servePackage = require.resolve('serve/package.json');
const serveEntry = resolve(dirname(servePackage), 'build/main.js');
const webRoot = fileURLToPath(new URL('../dist/apps/web/', import.meta.url));
const server = spawn(
  process.execPath,
  [
    serveEntry,
    webRoot,
    '--single',
    '--listen',
    `tcp://${hostname}:${port}`,
    '--no-clipboard',
    '--no-port-switching',
  ],
  { stdio: 'inherit', env: { ...process.env, NO_UPDATE_CHECK: '1' } },
);

const signals = ['SIGINT', 'SIGTERM'];
const forwardSignal = (signal) => {
  if (server.exitCode === null && server.signalCode === null) {
    server.kill(signal);
  }
};
const signalHandlers = new Map(
  signals.map((signal) => [signal, () => forwardSignal(signal)]),
);

for (const [signal, handler] of signalHandlers) {
  process.on(signal, handler);
}

const [exitCode, signalCode] = await once(server, 'exit');

for (const [signal, handler] of signalHandlers) {
  process.off(signal, handler);
}

process.exitCode =
  exitCode ?? (signalCode === 'SIGINT' ? 130 : signalCode === 'SIGTERM' ? 143 : 1);

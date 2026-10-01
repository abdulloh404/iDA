import { spawn } from 'node:child_process';
import { setTimeout as delay } from 'node:timers/promises';
import { fileURLToPath } from 'node:url';

const mode = process.argv[2];
if (!['dev', 'start', 'serve'].includes(mode)) {
  throw new Error('Usage: node tools/run-api.mjs <dev|start|serve>');
}
if (process.platform === 'win32') {
  throw new Error('The API runner requires POSIX process groups. Use WSL on Windows.');
}

const port = Number(process.env.API_PORT);
if (!Number.isInteger(port) || port < 1 || port > 65535) {
  throw new Error('API_PORT must be an integer between 1 and 65535.');
}
const apiUrl = `http://localhost:${port}`;
const services = [
  {
    name: 'Core API',
    directory: 'core-api/api',
    env: { ASPNETCORE_URLS: apiUrl },
    dev: ['watch', '--non-interactive', '--project', 'Ida.Api.csproj', 'run', '--', '--urls', apiUrl],
    start: ['bin/Release/net9.0/iDA.Core.Api.dll'],
    serve: ['run', '--project', 'Ida.Api.csproj', '--no-launch-profile', '--', '--urls', apiUrl],
  },
  {
    name: 'Ingest worker',
    directory: 'ingest-worker',
    dev: ['watch', '--non-interactive', '--project', 'Ida.Worker.Ingest.csproj', 'run', '--', 'serve', '--hospital=all'],
    start: ['bin/Release/net9.0/Ida.Worker.Ingest.dll', 'serve', '--hospital=all'],
    serve: ['run', '--project', 'Ida.Worker.Ingest.csproj', '--', 'serve', '--hospital=all'],
  },
];

const groups = new Set();
const children = [];
let stopping;
let complete;
const finished = new Promise((resolve) => { complete = resolve; });

function signalGroups(signal) {
  for (const pid of groups) {
    try {
      process.kill(-pid, signal);
    } catch (error) {
      if (error.code === 'ESRCH') {
        groups.delete(pid);
      } else {
        process.stderr.write(`Unable to signal API process group ${pid}: ${error.message}\n`);
      }
    }
  }
}

async function waitForGroups(timeout) {
  const deadline = Date.now() + timeout;
  while (groups.size > 0) {
    signalGroups(0);
    if (groups.size === 0 || Date.now() >= deadline) return;
    await delay(100);
  }
}

function stop(code) {
  if (stopping) return;
  process.exitCode = code;
  stopping = (async () => {
    signalGroups('SIGINT');
    await waitForGroups(3000);
    if (groups.size > 0) {
      signalGroups('SIGTERM');
      await waitForGroups(2000);
    }
    signalGroups('SIGKILL');
    await Promise.all(children);
  })().finally(complete);
}

for (const [signal, code] of [['SIGINT', 130], ['SIGTERM', 143], ['SIGHUP', 129]]) {
  process.on(signal, () => stop(code));
}
process.on('exit', () => signalGroups('SIGKILL'));

for (const service of services) {
  if (stopping) break;
  const args = [...service[mode]];
  if (mode !== 'start') {
    const artifactsPath = fileURLToPath(new URL(`../.nx/api-run/${mode}/${service.directory}/`, import.meta.url));
    args.splice(1, 0, '--artifacts-path', artifactsPath);
  }
  const child = spawn('dotnet', [...args, ...process.argv.slice(3)], {
    cwd: fileURLToPath(new URL(`../apps/api/${service.directory}/`, import.meta.url)),
    env: {
      ...process.env,
      ...service.env,
      IDA_API_CONFIG_DIRECTORY: fileURLToPath(new URL('../apps/api/core-api/api/', import.meta.url)),
    },
    stdio: 'inherit',
    detached: true,
  });
  if (child.pid) groups.add(child.pid);
  children.push(new Promise((resolve) => {
    child.once('error', (error) => {
      process.stderr.write(`Unable to start ${service.name}: ${error.message}\n`);
      resolve();
      stop(1);
    });
    child.once('exit', (code, signal) => {
      resolve();
      stop(code ?? (signal === 'SIGINT' ? 130 : signal === 'SIGTERM' ? 143 : 1));
    });
  }));
}

await finished;

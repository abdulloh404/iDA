import { spawn } from 'node:child_process';
import { setTimeout as delay } from 'node:timers/promises';
import { fileURLToPath } from 'node:url';
import { apiTopology } from './api-topology.mjs';
import { readApiSettings, resolveApiEnvironment } from './api-settings.mjs';
import { createApiGateway } from './api-gateway.mjs';

const mode = process.argv[2];
if (!['dev', 'start', 'serve'].includes(mode)) {
  throw new Error('Usage: node tools/run-api.mjs <dev|start|serve>');
}
if (process.platform === 'win32') {
  throw new Error('The API runner requires POSIX process groups. Use WSL on Windows.');
}

const environment = resolveApiEnvironment(process.env, mode === 'start' ? 'Production' : 'Local');
const runnerEnv = { ...process.env, DOTNET_ENVIRONMENT: environment, ASPNETCORE_ENVIRONMENT: environment };
const apiSettings = readApiSettings(runnerEnv);
const topology = apiTopology(runnerEnv, apiSettings);
const apiUrl = topology.core.url;
const sharedEnv = { Api__Core__Url: apiUrl };
for (const tenant of topology.tenants) {
  sharedEnv[`Api__Tenants__${tenant.key}__Url`] = tenant.url;
  sharedEnv[`Api__Tenants__${tenant.key}__PathBase`] = tenant.prefix;
}
const services = [
  {
    name: 'Core API',
    directory: 'iDA.Core/api',
    env: { Api__Mode: 'Core', ASPNETCORE_URLS: apiUrl, Api__PathBase: topology.core.prefix },
    dev: ['watch', '--non-interactive', '--project', 'Ida.Api.csproj', 'run', '--no-launch-profile', '--', '--urls', apiUrl],
    start: ['bin/Release/net9.0/iDA.Core.Api.dll'],
    serve: ['run', '--project', 'Ida.Api.csproj', '--no-launch-profile', '--', '--urls', apiUrl],
  },
  ...topology.tenants.flatMap((tenant) => [
    {
      name: `Tenant API ${tenant.key}`,
      directory: 'iDA.Tanent/api',
      bu: tenant.key,
      env: { Api__Mode: 'Tenant', Api__BuId: tenant.key, BU_ID: tenant.key, ASPNETCORE_URLS: tenant.url, Api__PathBase: tenant.prefix },
      dev: ['watch', '--non-interactive', '--project', 'Ida.Tenant.Api.csproj', 'run', '--no-launch-profile', '--', '--urls', tenant.url],
      start: ['bin/Release/net9.0/iDA.Tenant.Api.dll'],
      serve: ['run', '--project', 'Ida.Tenant.Api.csproj', '--no-launch-profile', '--', '--urls', tenant.url],
    },
    {
      name: `Ingest ${tenant.key}`,
      directory: 'IDA.Ingest-worker',
      bu: tenant.key,
      env: { Api__Mode: 'Tenant', Api__BuId: tenant.key, BU_ID: tenant.key },
      dev: ['watch', '--non-interactive', '--project', 'Ida.Worker.Ingest.csproj', 'run', '--no-launch-profile', '--', 'serve'],
      start: ['bin/Release/net9.0/Ida.Worker.Ingest.dll', 'serve'],
      serve: ['run', '--project', 'Ida.Worker.Ingest.csproj', '--no-launch-profile', '--', 'serve'],
    },
  ]),
];

const gateway = createApiGateway(topology);

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
    gateway.close();
    gateway.closeAllConnections();
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

await new Promise((resolve, reject) => {
  gateway.once('error', reject);
  gateway.listen(topology.gatewayPort, '127.0.0.1', resolve);
});
gateway.on('error', () => stop(1));
process.stdout.write(`API gateway (${environment}): http://127.0.0.1:${topology.gatewayPort}\n`);
for (const route of [topology.core, ...topology.tenants]) {
  process.stdout.write(`  ${route.prefix} -> ${route.url}${route.prefix}\n`);
}

function serviceEnvironment(service) {
  const env = { ...runnerEnv };
  const set = (key, value) => {
    for (const existing of Object.keys(env)) {
      if (existing.toUpperCase() === key.toUpperCase()) delete env[existing];
    }
    env[key] = value;
  };
  for (const [key, value] of apiSettings) {
    if (/^(?:JWT|SECURITY|CONNECTIONSTRINGS):/.test(key) || /^(?:CORE|BU[0-9]+)_(?:DB_|HOSPITAL_ID)/.test(key) || ['API:SERVICEKEY', 'IDA_SERVICE_API_KEY'].includes(key)) {
      set(key.replaceAll(':', '__'), value);
    }
  }
  for (const [key, value] of Object.entries({ ...sharedEnv, ...service.env })) set(key, value);
  for (const key of Object.keys(env)) {
    const database = key.match(/^(CORE|BU[0-9]+)_(?:DB_|HOSPITAL_ID)/)?.[1];
    const connection = key.match(/^ConnectionStrings__(Core|Postgres|BU[0-9]+)(Migration)?$/i)?.[1];
    if (database && database !== (service.bu ?? 'CORE') && key !== 'CORE_DB_SCHEMA') delete env[key];
    if (connection && (service.bu ? connection.toUpperCase() !== service.bu : /^BU/i.test(connection))) delete env[key];
    if (/^(?:CORE|BU[0-9]+)_DB_ADMIN_/i.test(key) || /^ConnectionStrings__.+Migration$/i.test(key)) delete env[key];
  }
  return env;
}

for (const service of services) {
  if (stopping) break;
  const args = [...service[mode]];
  if (mode !== 'start') {
    const artifactsPath = fileURLToPath(new URL(`../.nx/api-run/${mode}/${service.directory}/${service.bu ?? 'CORE'}/`, import.meta.url));
    args.splice(1, 0, '--artifacts-path', artifactsPath);
  }
  process.stdout.write(`Starting ${service.name} (${mode})...\n`);
  const child = spawn('dotnet', [...args, ...process.argv.slice(3)], {
    cwd: fileURLToPath(new URL(`../apps/api/${service.directory}/`, import.meta.url)),
    env: serviceEnvironment(service),
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
      if (!stopping) process.stderr.write(`${service.name} exited with ${signal ? `signal ${signal}` : `code ${code ?? 'unknown'}`}.\n`);
      resolve();
      stop(code ?? (signal === 'SIGINT' ? 130 : signal === 'SIGTERM' ? 143 : 1));
    });
  }));
}

await finished;

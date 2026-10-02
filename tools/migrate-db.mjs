import { spawn } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { constants } from 'node:os';
import { fileURLToPath } from 'node:url';
import { parseEnv } from 'node:util';

const task = process.argv[2];
if (!['migrate', 'seed'].includes(task)) {
  throw new Error('Usage: node tools/migrate-db.mjs <migrate|seed>');
}

const environmentValue = process.env.DOTNET_ENVIRONMENT ?? process.env.ASPNETCORE_ENVIRONMENT ?? 'Local';
const environment = ['Production', 'Development', 'Local'].find(name => name.toLowerCase() === String(environmentValue).toLowerCase());
if (!environment) throw new Error(`Unsupported API environment "${environmentValue}". Use Production, Development, or Local.`);
const apiRoot = new URL('../apps/api/', import.meta.url);
const settings = task === 'migrate' ? { ...parseEnv(readFileSync(new URL('.env', apiRoot), 'utf8')), ...process.env } : null;
const services = {
  core: { directory: 'iDA.Core/api', assembly: 'iDA.Core.Api.dll' },
  tenant: { directory: 'iDA.Tanent/api', assembly: 'iDA.Tenant.Api.dll' },
};
let interruptedSignal;

function serviceEnvironment(service, endpoint) {
  const env = { ...process.env };
  const selectedKeys = new Set(['DOTNET_ENVIRONMENT', 'ASPNETCORE_ENVIRONMENT', 'API__MODE', 'API__BUID', 'BU_ID', 'IDA_DATABASE_ENDPOINT', 'IDA_SETTINGS_DIRECTORY']);
  for (const key of Object.keys(env)) {
    if (selectedKeys.has(key.toUpperCase()) || (settings && /^(CORE_|BU[0-9]+_|CONNECTIONSTRINGS__|API__|IDA_START_SETTINGS$)/i.test(key))) delete env[key];
  }
  env.DOTNET_ENVIRONMENT = environment;
  env.ASPNETCORE_ENVIRONMENT = environment;
  env.IDA_SETTINGS_DIRECTORY = fileURLToPath(new URL(`${service.directory}/`, apiRoot));
  env.Api__Mode = endpoint ? 'Tenant' : 'Core';
  if (endpoint) {
    env.Api__BuId = endpoint.connectionKey;
    env.BU_ID = endpoint.connectionKey;
    env.IDA_DATABASE_ENDPOINT = JSON.stringify(endpoint);
  }
  if (settings) {
    const key = endpoint?.connectionKey.toUpperCase() ?? 'CORE';
    for (const name of [`${key}_CONNECTION`, `${key}_MIGRATION_CONNECTION`]) {
      if (!settings[name]?.trim()) throw new Error(`Set ${name} in apps/api/.env.`);
    }
    const connectionName = endpoint ? 'Tenant' : 'Core';
    env.IDA_START_SETTINGS = JSON.stringify({
      Api: { Mode: endpoint ? 'Tenant' : 'Core', BuId: endpoint?.connectionKey },
      ConnectionStrings: {
        [connectionName]: settings[`${key}_CONNECTION`],
        [`${connectionName}Migration`]: settings[`${key}_MIGRATION_CONNECTION`],
      },
      CORE_DB_SCHEMA: settings.CORE_DB_SCHEMA,
      CORE_DB_PASSWORD: endpoint ? undefined : settings.CORE_DB_PASSWORD,
      CORE_DB_ADMIN_PASSWORD: endpoint ? undefined : settings.CORE_DB_ADMIN_PASSWORD,
      ...(endpoint ? {
        [endpoint.passwordEnvironment]: settings[endpoint.passwordEnvironment],
        [`${key}_DB_ADMIN_PASSWORD`]: settings[`${key}_DB_ADMIN_PASSWORD`],
      } : {}),
    });
    delete env.IDA_SETTINGS_DIRECTORY;
  }
  return env;
}

function run(service, command, endpoint, capture = false) {
  return new Promise((resolve, reject) => {
    if (interruptedSignal) {
      reject(new Error(`Database task cancelled (${interruptedSignal}).`));
      return;
    }
    const child = spawn('dotnet', [`bin/Release/net9.0/${service.assembly}`, command], {
      cwd: fileURLToPath(new URL(`${service.directory}/`, apiRoot)),
      env: serviceEnvironment(service, endpoint),
      stdio: ['inherit', capture ? 'pipe' : 'inherit', 'inherit'],
    });
    const signalHandlers = new Map();
    for (const signal of ['SIGINT', 'SIGTERM']) {
      const handler = () => {
        interruptedSignal ??= signal;
        child.kill(signal);
      };
      signalHandlers.set(signal, handler);
      process.on(signal, handler);
    }
    let output = '';
    if (capture) {
      child.stdout.setEncoding('utf8');
      child.stdout.on('data', chunk => { output += chunk; });
    }
    child.once('error', reject);
    child.once('close', (code, signal) => {
      for (const [name, handler] of signalHandlers) process.off(name, handler);
      if (interruptedSignal || code !== 0) reject(new Error(`${service.assembly} ${command} failed (${interruptedSignal ?? signal ?? code}).`));
      else resolve(output);
    });
  });
}

async function branches() {
  const output = await run(services.core, '--list-tenant-databases', undefined, true);
  let endpoints;
  try {
    endpoints = JSON.parse(output);
  } catch {
    throw new Error('Core returned an invalid database registry response.');
  }
  if (!Array.isArray(endpoints) || endpoints.some(endpoint => !endpoint || endpoint.kind !== 'bu' || typeof endpoint.connectionKey !== 'string' || !/^BU[0-9]+$/i.test(endpoint.connectionKey) || endpoint.connectionKey.length > 40)) {
    throw new Error('Core returned an invalid BU database registry entry.');
  }
  if (new Set(endpoints.map(endpoint => endpoint.connectionKey.toUpperCase())).size !== endpoints.length) {
    throw new Error('Core returned duplicate BU connection keys.');
  }
  return endpoints.sort((left, right) => left.connectionKey.localeCompare(right.connectionKey, 'en', { numeric: true }));
}

const completed = [];
let stage = 'Core';
try {
  process.stdout.write(`${task === 'migrate' ? 'Migrating' : 'Seeding'} Core\n`);
  await run(services.core, task === 'migrate' ? '--migrate-databases' : '--seed');
  completed.push('Core');
  stage = 'Reading registered BU databases from Core';
  const endpoints = await branches();
  for (const endpoint of endpoints) {
    stage = endpoint.connectionKey;
    process.stdout.write(`${task === 'migrate' ? 'Migrating' : 'Verifying security for'} ${endpoint.connectionKey}\n`);
    await run(services.tenant, task === 'migrate' ? '--migrate-databases' : '--verify-security', endpoint);
    completed.push(endpoint.connectionKey);
  }
  process.stdout.write(task === 'migrate'
    ? `Core and ${endpoints.length} registered BU schemas are ready.\n`
    : `Core seed and security checks for ${endpoints.length} registered BUs are complete.\n`);
} catch (error) {
  process.stderr.write(`${stage}: ${error.message}\n`);
  if (task === 'migrate') {
    process.stderr.write('Migration stopped. Uncommitted changes use per-database transaction rollback.\n');
    if (completed.length > 0) process.stderr.write(`Previously completed databases remain committed: ${completed.join(', ')}.\n`);
  }
  process.exitCode = interruptedSignal ? 128 + (constants.signals[interruptedSignal] ?? 1) : 1;
}

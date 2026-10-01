import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { resolveApiEnvironment } from './start-api.mjs';

const task = process.argv[2];
if (!['migrate', 'seed'].includes(task)) {
  throw new Error('Usage: node tools/run-database-task.mjs <migrate|seed>');
}

const environment = resolveApiEnvironment(process.env, 'Local');
const apiRoot = new URL('../apps/api/', import.meta.url);
const services = {
  core: { directory: 'iDA.Core/api', assembly: 'iDA.Core.Api.dll' },
  tenant: { directory: 'iDA.Tanent/api', assembly: 'iDA.Tenant.Api.dll' },
};

function serviceEnvironment(endpoint) {
  const env = { ...process.env };
  const selectedKeys = new Set(['DOTNET_ENVIRONMENT', 'ASPNETCORE_ENVIRONMENT', 'API__MODE', 'API__BUID', 'BU_ID', 'IDA_DATABASE_ENDPOINT']);
  for (const key of Object.keys(env)) {
    if (selectedKeys.has(key.toUpperCase())) delete env[key];
  }
  env.DOTNET_ENVIRONMENT = environment;
  env.ASPNETCORE_ENVIRONMENT = environment;
  env.Api__Mode = endpoint ? 'Tenant' : 'Core';
  if (endpoint) {
    env.Api__BuId = endpoint.connectionKey;
    env.BU_ID = endpoint.connectionKey;
    env.IDA_DATABASE_ENDPOINT = JSON.stringify(endpoint);
  }
  return env;
}

function run(service, command, endpoint, capture = false) {
  return new Promise((resolve, reject) => {
    const child = spawn('dotnet', [`bin/Release/net9.0/${service.assembly}`, command], {
      cwd: fileURLToPath(new URL(`${service.directory}/`, apiRoot)),
      env: serviceEnvironment(endpoint),
      stdio: ['inherit', capture ? 'pipe' : 'inherit', 'inherit'],
    });
    let output = '';
    if (capture) {
      child.stdout.setEncoding('utf8');
      child.stdout.on('data', chunk => { output += chunk; });
    }
    child.once('error', reject);
    child.once('close', (code, signal) => {
      if (code !== 0) reject(new Error(`${service.assembly} ${command} failed (${signal ?? code}).`));
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
  if (!Array.isArray(endpoints) || endpoints.some(endpoint => !endpoint || endpoint.kind !== 'bu' || typeof endpoint.connectionKey !== 'string' || !endpoint.connectionKey.trim() || endpoint.connectionKey.length > 40)) {
    throw new Error('Core returned an invalid BU database registry entry.');
  }
  if (new Set(endpoints.map(endpoint => endpoint.connectionKey.toUpperCase())).size !== endpoints.length) {
    throw new Error('Core returned duplicate BU connection keys.');
  }
  return endpoints;
}

try {
  await run(services.core, task === 'migrate' ? '--migrate-databases' : '--seed');
  const endpoints = await branches();
  for (const endpoint of endpoints) {
    process.stdout.write(`${task === 'migrate' ? 'Migrating' : 'Verifying security for'} ${endpoint.connectionKey}\n`);
    await run(services.tenant, task === 'migrate' ? '--migrate-databases' : '--verify-security', endpoint);
  }
  process.stdout.write(task === 'migrate'
    ? `Core and ${endpoints.length} registered BU schemas are ready.\n`
    : `Core seed and security checks for ${endpoints.length} registered BUs are complete.\n`);
} catch (error) {
  process.stderr.write(`${error.message}\n`);
  process.exitCode = 1;
}

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
const settings = { ...parseEnv(readFileSync(new URL('.env', apiRoot), 'utf8')), ...process.env };
const buKeys = [...new Set(Object.keys(settings).map(key => /^(BU[0-9]+)_/.exec(key)?.[1]).filter(Boolean))]
  .sort((left, right) => left.localeCompare(right, 'en', { numeric: true }));
const tenants = Object.fromEntries(buKeys.map(key => [key, {
  HospitalId: settings[`${key}_HOSPITAL_ID`] ?? key,
}]));
const services = {
  core: { directory: 'iDA.Core/api', assembly: 'iDA.Core.Api.dll' },
  tenant: { directory: 'iDA.Tanent/api', assembly: 'iDA.Tenant.Api.dll' },
};
let interruptedSignal;

function serviceEnvironment(key) {
  const env = { ...process.env };
  const selectedKeys = new Set(['DOTNET_ENVIRONMENT', 'ASPNETCORE_ENVIRONMENT', 'BU_ID', 'IDA_SETTINGS_DIRECTORY']);
  for (const key of Object.keys(env)) {
    if (selectedKeys.has(key.toUpperCase()) || /^(CORE_|BU[0-9]+_|CONNECTIONSTRINGS__|API__|IDA_START_SETTINGS$)/i.test(key)) delete env[key];
  }
  env.DOTNET_ENVIRONMENT = environment;
  env.ASPNETCORE_ENVIRONMENT = environment;
  const isCore = key === 'CORE';
  const connectionName = isCore ? 'Core' : 'Tenant';
  if (!isCore) env.BU_ID = key;
  env.IDA_START_SETTINGS = JSON.stringify({
    Api: { Mode: isCore ? 'Core' : 'Tenant', BuId: isCore ? undefined : key, Tenants: isCore ? tenants : undefined },
    ConnectionStrings: {
      [connectionName]: settings[`${key}_CONNECTION`],
      [`${connectionName}Migration`]: settings[`${key}_MIGRATION_CONNECTION`],
    },
    CORE_DB_SCHEMA: settings.CORE_DB_SCHEMA,
    [`${key}_DB_SCHEMA`]: settings[`${key}_DB_SCHEMA`],
    [`${key}_DB_PASSWORD`]: settings[`${key}_DB_PASSWORD`],
    [`${key}_DB_ADMIN_PASSWORD`]: settings[`${key}_DB_ADMIN_PASSWORD`],
    ...(!isCore ? { [`${key}_HOSPITAL_ID`]: tenants[key].HospitalId } : {}),
    ...(task === 'seed' && isCore ? {
      Jwt: {
        Key: settings.JWT_KEY,
        Issuer: settings.JWT_ISSUER,
        Audience: settings.JWT_AUDIENCE,
        LifetimeHours: settings.JWT_LIFETIME_HOURS ?? '8',
      },
      Security: { DataProtectionKey: settings.SECURITY_DATA_PROTECTION_KEY, HashSalt: settings.SECURITY_HASH_SALT },
      Seed: {
        AdminUsername: settings.SEED_ADMIN_USERNAME ?? settings.Seed__AdminUsername,
        AdminPassword: settings.SEED_ADMIN_PASSWORD ?? settings.Seed__AdminPassword,
      },
      Logging: { LogLevel: { Default: settings.LOG_LEVEL_DEFAULT ?? 'Information', 'Microsoft.AspNetCore': settings.LOG_LEVEL_MICROSOFT_ASPNETCORE ?? 'Warning' } },
    } : {}),
  });
  return env;
}

function run(service, command, key = 'CORE', capture = false) {
  return new Promise((resolve, reject) => {
    if (interruptedSignal) {
      reject(new Error(`Database task cancelled (${interruptedSignal}).`));
      return;
    }
    const child = spawn('dotnet', [`bin/Release/net9.0/${service.assembly}`, command], {
      cwd: fileURLToPath(new URL(`${service.directory}/`, apiRoot)),
      env: serviceEnvironment(key),
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

async function describeDatabase(key) {
  const output = await run(key === 'CORE' ? services.core : services.tenant, '--describe-database', key, true);
  let endpoint;
  try {
    endpoint = JSON.parse(output);
  } catch {
    throw new Error(`${key} returned invalid database configuration metadata.`);
  }
  if (!endpoint || endpoint.connectionKey !== key || endpoint.kind !== (key === 'CORE' ? 'core' : 'bu') || typeof endpoint.host !== 'string' || !Number.isInteger(endpoint.port) || typeof endpoint.databaseName !== 'string' || endpoint.schemaName !== settings[`${key}_DB_SCHEMA`]) {
    throw new Error(`${key} returned inconsistent database configuration metadata.`);
  }
  return endpoint;
}

const completed = [];
let stage = 'Validating apps/api/.env';
try {
  for (const key of ['CORE', ...buKeys]) {
    for (const suffix of ['CONNECTION', 'MIGRATION_CONNECTION', 'DB_SCHEMA']) {
      if (!settings[`${key}_${suffix}`]?.trim()) throw new Error(`Set ${key}_${suffix} in apps/api/.env.`);
    }
  }
  const databases = new Map();
  const hospitals = new Set();
  for (const key of ['CORE', ...buKeys]) {
    stage = `Validating ${key} configuration`;
    const endpoint = await describeDatabase(key);
    const database = JSON.stringify([endpoint.host.toLowerCase(), endpoint.port, endpoint.databaseName]);
    if (databases.has(database)) throw new Error(`${key} and ${databases.get(database)} must use separate databases.`);
    databases.set(database, key);
    if (key !== 'CORE') {
      if (endpoint.hospitalId !== tenants[key].HospitalId || hospitals.has(endpoint.hospitalId)) throw new Error(`${key} must use a unique hospital ID matching its environment configuration.`);
      hospitals.add(endpoint.hospitalId);
      tenants[key].DatabaseName = endpoint.databaseName;
      tenants[key].Host = endpoint.host;
    }
  }
  stage = 'Core';
  process.stdout.write(`${task === 'migrate' ? 'Migrating' : 'Seeding'} Core\n`);
  await run(services.core, task === 'migrate' ? '--migrate-databases' : '--seed');
  completed.push('Core');
  for (const key of buKeys) {
    stage = key;
    process.stdout.write(`${task === 'migrate' ? 'Migrating' : 'Verifying security for'} ${key}\n`);
    await run(services.tenant, task === 'migrate' ? '--migrate-databases' : '--verify-security', key);
    completed.push(key);
  }
  process.stdout.write(task === 'migrate'
    ? `Core and ${buKeys.length} configured BU schemas are ready.\n`
    : `Core seed and security checks for ${buKeys.length} configured BUs are complete.\n`);
} catch (error) {
  process.stderr.write(`${stage}: ${error.message}\n`);
  if (task === 'migrate') {
    process.stderr.write('Migration stopped. Uncommitted changes use per-database transaction rollback.\n');
    if (completed.length > 0) process.stderr.write(`Previously completed databases remain committed: ${completed.join(', ')}.\n`);
  }
  process.exitCode = interruptedSignal ? 128 + (constants.signals[interruptedSignal] ?? 1) : 1;
}

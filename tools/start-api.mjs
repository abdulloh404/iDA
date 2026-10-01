import { spawn } from 'node:child_process';
import { existsSync, readFileSync } from 'node:fs';
import http from 'node:http';
import https from 'node:https';
import { resolve } from 'node:path';
import { setTimeout as delay } from 'node:timers/promises';
import { fileURLToPath } from 'node:url';
import { parseJson } from 'nx/src/utils/json';

const settingsFiles = {
  Production: 'appsettings.json',
  Development: 'appsettings.Development.json',
  Local: 'appsettings.local.json',
};

export function resolveApiEnvironment(env = process.env, fallback = 'Production') {
  const value = env.DOTNET_ENVIRONMENT ?? env.ASPNETCORE_ENVIRONMENT ?? fallback;
  const environment = Object.keys(settingsFiles).find((name) => name.toLowerCase() === String(value).toLowerCase());
  if (!environment) throw new Error(`Unsupported API environment "${value}". Use Production, Development, or Local.`);
  return environment;
}

export function readApiSettings(env = process.env) {
  const root = new URL('../apps/api/', import.meta.url);
  const settings = new Map();
  const add = (value, path = '') => {
    if (value !== null && typeof value === 'object') {
      for (const [key, item] of Object.entries(value)) add(item, path ? `${path}:${key}` : key);
    } else if (value !== null && value !== undefined) {
      settings.set(path.toUpperCase(), String(value));
    }
  };
  const environment = resolveApiEnvironment(env);
  const path = new URL(settingsFiles[environment], root);
  if (!existsSync(path)) throw new Error(`Missing API JSON configuration: ${fileURLToPath(path)}`);
  try {
    add(parseJson(readFileSync(path, 'utf8').replace(/^\uFEFF/, '')));
  } catch {
    throw new Error(`Unable to read or parse JSON configuration: ${fileURLToPath(path)}`);
  }
  for (const [key, value] of Object.entries(env)) {
    if (value !== undefined) settings.set(key.replaceAll('__', ':').toUpperCase(), value);
  }
  return settings;
}

function port(value, fallback, name) {
  const parsed = Number(value ?? fallback);
  if (!Number.isInteger(parsed) || parsed < 1 || parsed > 65535) {
    throw new Error(`${name} must be an integer between 1 and 65535.`);
  }
  return parsed;
}

function serviceAddress(value, fallbackPort, name, gatewayPort) {
  let address;
  try {
    address = new URL(value ?? `http://localhost:${fallbackPort}`);
  } catch {
    throw new Error(`${name} must be an absolute HTTP URL.`);
  }
  if (address.protocol !== 'http:' || address.username || address.password || address.search || address.hash || address.pathname !== '/') {
    throw new Error(`${name} must be an HTTP origin without a path for the local API runner.`);
  }
  if (Number(address.port || 80) === gatewayPort) {
    address = new URL(`http://localhost:${fallbackPort}`);
  }
  const hostname = address.hostname === '0.0.0.0' ? '127.0.0.1' : address.hostname === '[::]' ? '::1' : address.hostname.replace(/^\[|\]$/g, '');
  return { url: address.origin, port: port(address.port || 80, fallbackPort, name), hostname };
}

export function apiRouting(env, settings = new Map()) {
  const gatewayPort = port(env.API_PORT ?? settings.get('API:GATEWAYPORT'), 3100, 'Api:GatewayPort');
  const core = { prefix: env.API_PATH_BASE ?? settings.get('API:CORE:PATHBASE') ?? '/core' };
  if (core.prefix !== '/core') throw new Error('The Core API PathBase must be /core for the current Web routing.');
  const configuredKeys = [...settings.keys()].filter((key) => /^API:TENANTS:BU[0-9]+:/.test(key)).map((key) => key.split(':')[2]);
  const connectionKeys = [...settings.keys()].filter((key) => /^CONNECTIONSTRINGS:BU[0-9]+$/.test(key)).map((key) => key.split(':')[1]);
  const keys = env.BU_IDS
    ? env.BU_IDS.split(',').map((key) => key.trim().toUpperCase())
    : [...new Set(configuredKeys.length ? configuredKeys : [...connectionKeys,
      ...Object.keys(env).filter((key) => /^BU[0-9]+_DB_CONNECTION$/.test(key) && env[key]).map((key) => key.replace(/_DB_CONNECTION$/, ''))])].sort();
  if (keys.length === 0 || keys.some((key) => !/^BU[0-9]+$/.test(key)) || new Set(keys).size !== keys.length) {
    throw new Error('Configure BU database connections or a unique comma-separated BU_IDS list.');
  }
  const tenants = keys.map((key) => {
    const prefix = env[`${key}_API_PATH`] ?? settings.get(`API:TENANTS:${key}:PATHBASE`) ?? `/${key.toLowerCase()}`;
    if (!/^\/[a-z0-9][a-z0-9-]*$/.test(prefix) || ['/core', '/api'].includes(prefix)) {
      throw new Error(`${key}_API_PATH must be a tenant prefix such as /pt1.`);
    }
    return { key, prefix };
  });
  if (new Set(tenants.map((tenant) => tenant.prefix)).size !== tenants.length) {
    throw new Error('Tenant API paths must be unique.');
  }
  return { gatewayPort, core, tenants };
}

export function apiProxyPattern(routing) {
  const prefixes = [routing.core, ...routing.tenants].map((route) => route.prefix).join('|');
  return `^(?:${prefixes})/(?:api(?:/|\\?|$)|hello(?:\\?|$)|healthz?(?:\\?|$))`;
}

export function apiTopology(env, settings = new Map()) {
  const routing = apiRouting(env, settings);
  const { gatewayPort } = routing;
  const corePort = port(env.CORE_API_PORT, gatewayPort + 1, 'CORE_API_PORT');
  const core = { ...routing.core, ...serviceAddress(env.CORE_API_URL ?? settings.get('API:CORE:URL'), corePort, 'Api:Core:Url', gatewayPort) };
  const tenants = routing.tenants.map((tenant, index) => {
    const tenantPort = port(env[`${tenant.key}_API_PORT`], gatewayPort + index + 2, `${tenant.key}_API_PORT`);
    return { ...tenant, ...serviceAddress(env[`${tenant.key}_API_URL`] ?? settings.get(`API:TENANTS:${tenant.key}:URL`), tenantPort, `Api:Tenants:${tenant.key}:Url`, gatewayPort) };
  });
  const ports = [gatewayPort, core.port, ...tenants.map((tenant) => tenant.port)];
  if (new Set(ports).size !== ports.length) throw new Error('Core, Tenant and gateway ports must be unique.');
  return { gatewayPort, core, tenants };
}

export function proxyApiRequest(request, response, port, hostname = 'localhost', protocol = 'http:') {
  const host = hostname.includes(':') ? `[${hostname}]:${port}` : `${hostname}:${port}`;
  const headers = { ...request.headers, host };
  delete headers['x-ida-service-key'];
  headers['x-forwarded-for'] = request.socket.remoteAddress ?? '127.0.0.1';
  headers['x-forwarded-proto'] = 'http';
  const transport = protocol === 'https:' ? https : http;
  const upstream = transport.request({ hostname, port, method: request.method, path: request.url, headers }, (incoming) => {
    response.writeHead(incoming.statusCode ?? 502, incoming.headers);
    incoming.pipe(response);
    incoming.on('error', () => response.destroy());
  });
  upstream.setTimeout(120_000, () => upstream.destroy());
  upstream.on('error', () => {
    if (!response.headersSent) {
      response.writeHead(502, { 'content-type': 'application/json' });
      response.end(JSON.stringify({ error: { code: 'api_unavailable', message: 'API service unavailable.' } }));
    } else response.destroy();
  });
  request.on('aborted', () => upstream.destroy());
  response.on('close', () => { if (!response.writableFinished) upstream.destroy(); });
  request.pipe(upstream);
}

export function createApiGateway(topology) {
  const routes = [topology.core, ...topology.tenants];
  return http.createServer((request, response) => {
    let pathname;
    try {
      pathname = new URL(request.url, 'http://localhost').pathname;
    } catch {
      response.writeHead(400, { 'content-type': 'application/json' });
      response.end(JSON.stringify({ error: { code: 'invalid_url', message: 'Invalid request URL.' } }));
      return;
    }
    const route = routes.find((candidate) => pathname === candidate.prefix || pathname.startsWith(`${candidate.prefix}/`));
    if (!route || pathname.startsWith(`${route.prefix}/api/internal`)) {
      response.writeHead(404, { 'content-type': 'application/json' });
      response.end(JSON.stringify({ error: { code: 'not_found', message: 'API route not found.' } }));
      return;
    }
    proxyApiRequest(request, response, route.port, route.hostname);
  });
}

async function startApi() {
  const mode = process.argv[2];
  if (!['dev', 'start', 'serve'].includes(mode)) {
    throw new Error('Usage: node tools/start-api.mjs <dev|start|serve>');
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
    gateway.listen(topology.gatewayPort, 'localhost', resolve);
  });
  gateway.on('error', () => stop(1));
  const gatewayUrl = `http://localhost:${topology.gatewayPort}`;
  process.stdout.write(`API gateway (${environment}): ${gatewayUrl}\n`);
  for (const route of [topology.core, ...topology.tenants]) {
    process.stdout.write(`  ${route.prefix}: ${gatewayUrl}${route.prefix}\n`);
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
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  await startApi();
}

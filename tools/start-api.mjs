import { existsSync, readFileSync } from 'node:fs';
import http from 'node:http';
import https from 'node:https';
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

import { existsSync, readFileSync } from 'node:fs';
import { parseEnv } from 'node:util';

export function readWebSettings(fallback = 'Production') {
  const file = new URL('.env', import.meta.url);
  const env = { ...process.env, ...(existsSync(file) ? parseEnv(readFileSync(file, 'utf8')) : {}) };

  const environments = ['Local', 'Development', 'Production'];
  const environment = environments.find((name) => name.toLowerCase() === String(env.NODE_ENV ?? fallback).trim().toLowerCase());
  if (!environment) throw new Error('NODE_ENV must be local, development, or production.');
  const hostname = (env.WEB_HOST ?? 'localhost').trim();
  const port = Number(env.WEB_PORT ?? '3000');
  if (!hostname) throw new Error('WEB_HOST must not be empty.');
  if (!Number.isInteger(port) || port < 1 || port > 65535) {
    throw new Error('WEB_PORT must be an integer between 1 and 65535.');
  }

  let apiUrl;
  try {
    apiUrl = new URL(env.API_URL ?? 'http://localhost:3100');
  } catch {
    throw new Error('API_URL must be an absolute HTTP or HTTPS URL.');
  }
  if (!['http:', 'https:'].includes(apiUrl.protocol) || apiUrl.username || apiUrl.password || apiUrl.pathname !== '/' || apiUrl.search || apiUrl.hash) {
    throw new Error('API_URL must be an HTTP or HTTPS origin without credentials, a path, query or fragment.');
  }
  const localHosts = new Set(['localhost', '127.0.0.1', '0.0.0.0', '::', '[::]', '::1', '[::1]']);
  const tenantKeys = Object.keys(env).filter((key) => /^PT[1-9][0-9]*_API_BASE_URL$/.test(key)).sort((a, b) => a.localeCompare(b, 'en', { numeric: true }));
  if (tenantKeys.length === 0) throw new Error('Configure at least one PT<n>_API_BASE_URL in apps/web/.env.');
  const routes = ['CORE_API_BASE_URL', ...tenantKeys].map((name) => {
    const value = env[name]?.trim();
    if (!value) throw new Error(`${name} must not be empty.`);
    let baseUrl;
    try {
      baseUrl = new URL(value, apiUrl);
    } catch {
      throw new Error(`${name} must be an HTTP or HTTPS base URL.`);
    }
    if (!['http:', 'https:'].includes(baseUrl.protocol) || baseUrl.username || baseUrl.password || baseUrl.search || baseUrl.hash) {
      throw new Error(`${name} must be an HTTP or HTTPS base URL without credentials, a query or fragment.`);
    }
    const prefix = baseUrl.pathname.replace(/\/+$/, '');
    if (!/^\/[a-zA-Z0-9_-]+(?:\/[a-zA-Z0-9_-]+)*$/.test(prefix)) {
      throw new Error(`${name} must include a path prefix such as /core or /pt1.`);
    }
    const apiPort = Number(baseUrl.port || (baseUrl.protocol === 'https:' ? 443 : 80));
    if (apiPort === port && (baseUrl.hostname === hostname || (localHosts.has(baseUrl.hostname) && localHosts.has(hostname)))) {
      throw new Error(`${name} must not point back to the Web server.`);
    }
    baseUrl.pathname = prefix;
    return { key: name.replace(/_API_BASE_URL$/, ''), prefix, baseUrl };
  });
  if (new Set(routes.map((route) => route.prefix)).size !== routes.length) {
    throw new Error('CORE_API_BASE_URL and PT<n>_API_BASE_URL must use unique path prefixes.');
  }
  routes.sort((left, right) => right.prefix.length - left.prefix.length);
  return { environment, hostname, port, routes };
}

export function webApiProxyPattern(prefix) {
  return `^${prefix}/(?:api(?:/|\\?|$)|hello(?:\\?|$)|healthz?(?:\\?|$))`;
}

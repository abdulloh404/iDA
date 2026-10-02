import http from 'node:http';
import https from 'node:https';
import { existsSync, readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { parseEnv } from 'node:util';

export function readWebSettings(fallback = 'Production') {
  const file = new URL('../apps/web/.env', import.meta.url);
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

function proxyApiRequest(request, response, baseUrl) {
  const headers = { ...request.headers, host: baseUrl.host };
  delete headers['x-ida-service-key'];
  headers['x-forwarded-for'] = request.socket.remoteAddress ?? '127.0.0.1';
  headers['x-forwarded-proto'] = 'http';
  const transport = baseUrl.protocol === 'https:' ? https : http;
  const upstream = transport.request({ hostname: baseUrl.hostname.replace(/^\[|\]$/g, ''), port: baseUrl.port || (baseUrl.protocol === 'https:' ? 443 : 80), method: request.method, path: request.url, headers }, (incoming) => {
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

function startWeb() {
  const { environment, hostname, port, routes } = readWebSettings();
  const proxies = routes.map((route) => ({ ...route, pattern: new RegExp(webApiProxyPattern(route.prefix)) }));
  const require = createRequire(new URL('../apps/web/package.json', import.meta.url));
  const handler = require('serve-handler');
  const webRoot = fileURLToPath(new URL('../apps/web/dist/', import.meta.url));
  const server = http.createServer((request, response) => {
    const route = proxies.find((item) => item.pattern.test(request.url ?? ''));
    if (route) {
      proxyApiRequest(request, response, route.baseUrl);
      return;
    }
    handler(request, response, { public: webRoot, rewrites: [{ source: '**', destination: '/index.html' }] }).catch(() => {
      if (!response.headersSent) response.writeHead(500);
      response.end();
    });
  });
  server.listen(port, hostname, () => {
    process.stdout.write(`Web (${environment}): http://${hostname}:${port}\n`);
    for (const route of routes) process.stdout.write(`  ${route.prefix} -> ${route.baseUrl.href}\n`);
  });
  for (const [signal, code] of [['SIGINT', 130], ['SIGTERM', 143]]) {
    process.on(signal, () => {
      process.exitCode = code;
      server.close();
      server.closeAllConnections();
    });
  }
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  startWeb();
}

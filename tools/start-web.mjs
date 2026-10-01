import http from 'node:http';
import { existsSync, readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { parseEnv } from 'node:util';
import { apiProxyPattern, apiRouting, proxyApiRequest, readApiSettings, resolveApiEnvironment } from './start-api.mjs';

export function readWebSettings(fallback = 'Production') {
  const file = new URL('../apps/web/.env', import.meta.url);
  const env = { ...process.env, ...(existsSync(file) ? parseEnv(readFileSync(file, 'utf8')) : {}) };

  const environment = resolveApiEnvironment({ DOTNET_ENVIRONMENT: env.NODE_ENV }, fallback);
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
  const apiPort = Number(apiUrl.port || (apiUrl.protocol === 'https:' ? 443 : 80));
  const localHosts = new Set(['localhost', '127.0.0.1', '0.0.0.0', '::', '[::]', '::1', '[::1]']);
  if (apiPort === port && (apiUrl.hostname === hostname || (localHosts.has(apiUrl.hostname) && localHosts.has(hostname)))) {
    throw new Error('API_URL must not point back to the Web server.');
  }
  return { env, environment, hostname, port, apiUrl, apiPort };
}

function startWeb() {
  const { env, environment, hostname, port, apiUrl, apiPort } = readWebSettings();
  const apiEnv = { ...env, DOTNET_ENVIRONMENT: environment, ASPNETCORE_ENVIRONMENT: environment };
  const routing = apiRouting(apiEnv, readApiSettings(apiEnv));
  const apiPattern = new RegExp(apiProxyPattern(routing));
  const require = createRequire(new URL('../apps/web/package.json', import.meta.url));
  const handler = require('serve-handler');
  const webRoot = fileURLToPath(new URL('../apps/web/dist/', import.meta.url));
  const server = http.createServer((request, response) => {
    if (apiPattern.test(request.url ?? '')) {
      proxyApiRequest(request, response, apiPort, apiUrl.hostname.replace(/^\[|\]$/g, ''), apiUrl.protocol);
      return;
    }
    handler(request, response, { public: webRoot, rewrites: [{ source: '**', destination: '/index.html' }] }).catch(() => {
      if (!response.headersSent) response.writeHead(500);
      response.end();
    });
  });
  server.listen(port, hostname, () => {
    process.stdout.write(`Web (${environment}): http://${hostname}:${port}\n`);
    process.stdout.write(`API proxy: ${[routing.core, ...routing.tenants].map((route) => route.prefix).join(', ')} -> ${apiUrl.origin}\n`);
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

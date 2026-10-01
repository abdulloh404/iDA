import http from 'node:http';
import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';
import { proxyApiRequest } from './api-gateway.mjs';
import { apiProxyPattern, apiRouting } from './api-topology.mjs';
import { readApiSettings, resolveApiEnvironment } from './api-settings.mjs';

const environment = resolveApiEnvironment();
const routing = apiRouting(process.env, readApiSettings());
const apiPattern = new RegExp(apiProxyPattern(routing));
const rawPort = process.env.WEB_PORT ?? process.env.PORT ?? 3000;
const port = Number(rawPort);

if (!Number.isInteger(port) || port < 1 || port > 65535) {
  throw new Error(
    `WEB_PORT/PORT must be an integer between 1 and 65535; received ${rawPort}.`,
  );
}

const hostname = process.env.HOSTNAME || '127.0.0.1';
const require = createRequire(import.meta.url);
const handler = createRequire(require.resolve('serve/package.json'))('serve-handler');
const webRoot = fileURLToPath(new URL('../dist/apps/web/', import.meta.url));
const apiPort = routing.gatewayPort;
if (!Number.isInteger(apiPort) || apiPort < 1 || apiPort > 65535 || apiPort === port) {
  throw new Error('API_PORT must be a valid port different from WEB_PORT.');
}
const server = http.createServer((request, response) => {
  if (apiPattern.test(request.url ?? '')) {
    proxyApiRequest(request, response, apiPort);
    return;
  }
  handler(request, response, { public: webRoot, rewrites: [{ source: '**', destination: '/index.html' }] }).catch(() => {
    if (!response.headersSent) response.writeHead(500);
    response.end();
  });
});
server.listen(port, hostname, () => {
  process.stdout.write(`Web (${environment}): http://${hostname}:${port}\n`);
  process.stdout.write(`API proxy: ${[routing.core, ...routing.tenants].map((route) => route.prefix).join(', ')} -> http://127.0.0.1:${apiPort}\n`);
});
for (const [signal, code] of [['SIGINT', 130], ['SIGTERM', 143]]) {
  process.on(signal, () => {
    process.exitCode = code;
    server.close();
    server.closeAllConnections();
  });
}

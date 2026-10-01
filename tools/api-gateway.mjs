import http from 'node:http';

export function proxyApiRequest(request, response, port) {
  const headers = { ...request.headers, host: `localhost:${port}` };
  delete headers['x-ida-service-key'];
  headers['x-forwarded-for'] = request.socket.remoteAddress ?? '127.0.0.1';
  headers['x-forwarded-proto'] = 'http';
  const upstream = http.request({ hostname: '127.0.0.1', port, method: request.method, path: request.url, headers }, (incoming) => {
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
    proxyApiRequest(request, response, route.port);
  });
}

function port(value, fallback, name) {
  const parsed = Number(value ?? fallback);
  if (!Number.isInteger(parsed) || parsed < 1 || parsed > 65535) {
    throw new Error(`${name} must be an integer between 1 and 65535.`);
  }
  return parsed;
}

function serviceAddress(value, fallbackPort, name) {
  let address;
  try {
    address = new URL(value ?? `http://localhost:${fallbackPort}`);
  } catch {
    throw new Error(`${name} must be an absolute HTTP URL.`);
  }
  if (address.protocol !== 'http:' || address.username || address.password || address.search || address.hash || address.pathname !== '/') {
    throw new Error(`${name} must be an HTTP origin without a path for the local API runner.`);
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
  const core = { ...routing.core, ...serviceAddress(env.CORE_API_URL ?? settings.get('API:CORE:URL'), corePort, 'Api:Core:Url') };
  const tenants = routing.tenants.map((tenant, index) => {
    const tenantPort = port(env[`${tenant.key}_API_PORT`], gatewayPort + index + 2, `${tenant.key}_API_PORT`);
    return { ...tenant, ...serviceAddress(env[`${tenant.key}_API_URL`] ?? settings.get(`API:TENANTS:${tenant.key}:URL`), tenantPort, `Api:Tenants:${tenant.key}:Url`) };
  });
  const ports = [gatewayPort, core.port, ...tenants.map((tenant) => tenant.port)];
  if (new Set(ports).size !== ports.length) throw new Error('Core, Tenant and gateway ports must be unique.');
  return { gatewayPort, core, tenants };
}

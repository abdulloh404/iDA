function port(value, fallback, name) {
  const parsed = Number(value ?? fallback);
  if (!Number.isInteger(parsed) || parsed < 1 || parsed > 65535) {
    throw new Error(`${name} must be an integer between 1 and 65535.`);
  }
  return parsed;
}

export function apiTopology(env) {
  const gatewayPort = port(env.API_PORT, 3100, 'API_PORT');
  const corePort = port(env.CORE_API_PORT, gatewayPort + 1, 'CORE_API_PORT');
  const keys = env.BU_IDS
    ? env.BU_IDS.split(',').map((key) => key.trim().toUpperCase())
    : Object.keys(env).filter((key) => /^BU[0-9]+_DB_CONNECTION$/.test(key) && env[key])
      .map((key) => key.replace(/_DB_CONNECTION$/, '')).sort();
  if (keys.length === 0 || keys.some((key) => !/^BU[0-9]+$/.test(key)) || new Set(keys).size !== keys.length) {
    throw new Error('Configure BU database connections or a unique comma-separated BU_IDS list.');
  }
  const tenants = keys.map((key, index) => {
    const prefix = env[`${key}_API_PATH`] ?? `/${key.toLowerCase()}`;
    if (!/^\/[a-z0-9][a-z0-9-]*$/.test(prefix) || ['/core', '/api'].includes(prefix)) {
      throw new Error(`${key}_API_PATH must be a tenant prefix such as /pt1.`);
    }
    const tenantPort = port(env[`${key}_API_PORT`], gatewayPort + index + 2, `${key}_API_PORT`);
    return { key, prefix, port: tenantPort, url: `http://localhost:${tenantPort}` };
  });
  const ports = [gatewayPort, corePort, ...tenants.map((tenant) => tenant.port)];
  if (new Set(ports).size !== ports.length || new Set(tenants.map((tenant) => tenant.prefix)).size !== tenants.length) {
    throw new Error('Core, Tenant and gateway ports and API paths must be unique.');
  }
  return { gatewayPort, core: { prefix: '/core', port: corePort, url: `http://localhost:${corePort}` }, tenants };
}

import { spawnSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import { readFileSync } from 'node:fs';
import { networkInterfaces, tmpdir } from 'node:os';
import { basename, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { parseEnv } from 'node:util';
import { configuredNamespaces } from './kube-namespaces.mjs';

const workspaceRoot = fileURLToPath(new URL('../', import.meta.url));

function kubectl(args) {
  const result = spawnSync('kubectl', args, {
    cwd: workspaceRoot,
    encoding: 'utf8',
    stdio: ['ignore', 'pipe', 'pipe'],
  });
  if (result.error || result.status !== 0) {
    throw new Error(result.stderr?.trim() || result.error?.message || 'kubectl failed.');
  }
  return result.stdout.trim();
}

function currentContext() {
  const context = kubectl(['config', 'current-context']);
  if (!context) throw new Error('kubectl has no current context.');
  return context;
}

function getResource(context, namespace, resource, name) {
  return JSON.parse(kubectl(['--context', context, '-n', namespace, 'get', resource, name, '-o', 'json']));
}

function referencedDatabaseConfigMap(statefulSet, prefix) {
  const references = (statefulSet.spec?.template?.spec?.containers ?? [])
    .flatMap((container) => container.envFrom ?? [])
    .map((source) => source.configMapRef?.name)
    .filter((name) => name?.startsWith(prefix));
  if (references.length !== 1) {
    throw new Error(`StatefulSet ${statefulSet.metadata?.namespace}/${statefulSet.metadata?.name} must reference one database ConfigMap.`);
  }
  return references[0];
}

function databaseServicePort(service) {
  const port = service.spec?.ports?.find(({ name }) => name === 'postgresql') ?? service.spec?.ports?.[0];
  if (!port?.port) throw new Error(`Service ${service.metadata?.namespace}/${service.metadata?.name} has no database port.`);
  return port.port;
}

export function databasePortForwards({ context = currentContext(), namespaces = configuredNamespaces() } = {}) {
  let values = {};
  try {
    values = parseEnv(readFileSync(new URL('../.env', import.meta.url), 'utf8'));
  } catch (error) {
    if (error.code !== 'ENOENT') throw error;
  }
  values = { ...values, ...process.env };
  const basePort = Number(values.KUBE_DB_PORT_FORWARD_BASE_PORT ?? 5442);
  if (!Number.isInteger(basePort) || basePort < 1 || basePort > 65535) {
    throw new Error('KUBE_DB_PORT_FORWARD_BASE_PORT must be an integer from 1 to 65535.');
  }

  const ports = new Set();
  return namespaces.map(({ name: namespace, role, bu }) => {
    const isCore = role === 'core';
    const port = basePort + (isCore ? 0 : Number(bu.slice(2)));
    if (!Number.isInteger(port) || port < 1 || port > 65535 || ports.has(port)) {
      throw new Error('Invalid or duplicate database port-forward port for namespace ' + namespace + '. Check the base port and BU number.');
    }
    ports.add(port);
    const serviceName = (isCore ? 'core-db' : 'bu-db') + '-client';
    const service = getResource(context, namespace, 'service', serviceName);
    return {
      namespace,
      scope: isCore ? 'Core' : bu,
      service: serviceName,
      address: '0.0.0.0',
      port,
      remotePort: databaseServicePort(service),
    };
  });
}

export function databasePortForwardDirectory(context) {
  const stateId = createHash('sha256').update(workspaceRoot + '\0' + context).digest('hex').slice(0, 16);
  return join(tmpdir(), 'ida-kube-port-forward-' + stateId);
}

export function databasePortForwardArgs(context, forward) {
  return [
    '--context', context,
    '-n', forward.namespace,
    'port-forward',
    '--address', forward.address,
    '--pod-running-timeout=60s',
    'service/' + forward.service,
    forward.port + ':' + forward.remotePort,
  ];
}

export function runningDatabasePortForward(context, forward, stateDirectory = databasePortForwardDirectory(context)) {
  try {
    const statePath = join(stateDirectory, forward.namespace + '-' + forward.port + '.json');
    const { pid } = JSON.parse(readFileSync(statePath, 'utf8'));
    if (!Number.isInteger(pid) || pid <= 0) return null;
    const command = readFileSync('/proc/' + pid + '/cmdline', 'utf8').split('\0').filter(Boolean);
    const args = databasePortForwardArgs(context, forward);
    if (basename(command[0] ?? '') === 'kubectl' && JSON.stringify(command.slice(1)) === JSON.stringify(args)) {
      return pid;
    }
  } catch {}
  return null;
}

export function reportDatabasePortForwards({ context = currentContext(), forwards = databasePortForwards({ context }) } = {}) {
  const stateDirectory = databasePortForwardDirectory(context);
  const addresses = [...new Set(Object.values(networkInterfaces())
    .flatMap((interfaces) => interfaces ?? [])
    .filter(({ family, internal }) => (family === 'IPv4' || family === 4) && !internal)
    .map(({ address }) => address))];
  const hostIps = addresses.length ? addresses.join(', ') : '127.0.0.1';
  const rows = forwards.map((forward) => {
    const pid = runningDatabasePortForward(context, forward, stateDirectory);
    return {
      Scope: forward.scope,
      Namespace: forward.namespace,
      'Bind IP': forward.address,
      'Host IPs': hostIps,
      'Forward Port': forward.port,
      'Database Port': forward.remotePort,
      'Port-forward status': pid ? 'Running (PID ' + pid + ')' : 'No managed process',
    };
  });

  console.log('Database port-forward connections on this machine for Kubernetes context ' + context + ':');
  console.table(rows);
  console.log('Bind IP 0.0.0.0 listens on all IPv4 interfaces. Connect using a reachable Host IP and Forward Port, or 127.0.0.1 when connecting on this machine or through an SSH tunnel.');
  return rows;
}

export function reportBuDatabaseConnections({ context = currentContext(), namespaces = configuredNamespaces() } = {}) {

  const rows = namespaces.map(({ name: namespace, role, bu }) => {
    const isCore = role === 'core';
    const scope = isCore ? 'Core' : bu;
    const databaseName = isCore ? 'core-db' : 'bu-db';
    const service = getResource(context, namespace, 'service', `${databaseName}-client`);
    const statefulSet = getResource(context, namespace, 'statefulset', databaseName);
    const configMapName = referencedDatabaseConfigMap(statefulSet, isCore ? 'core-database-config' : 'database-config');
    const configMap = getResource(context, namespace, 'configmap', configMapName);
    const desiredReplicas = statefulSet.spec?.replicas ?? 1;
    const readyReplicas = statefulSet.status?.readyReplicas ?? 0;
    const clusterIp = service.spec?.clusterIP;
    const database = configMap.data?.POSTGRES_DB;
    const username = configMap.data?.APP_DB_USER;
    if (!clusterIp || clusterIp === 'None' || !database || !username) {
      throw new Error(`Database client configuration for ${scope} is incomplete.`);
    }
    return {
      Scope: scope,
      Namespace: namespace,
      Host: clusterIp,
      Port: databaseServicePort(service),
      Database: database,
      Username: username,
      'Password env': isCore ? 'CORE_DB_PASSWORD' : `${bu}_DB_PASSWORD`,
      Status: readyReplicas === desiredReplicas && desiredReplicas > 0 ? `Ready (${readyReplicas}/${desiredReplicas})` : `NotReady (${readyReplicas}/${desiredReplicas})`,
    };
  });

  console.log(`Core and BU database client connections from Kubernetes context ${context}:`);
  console.table(rows);
  if (rows.some(({ Status }) => Status.startsWith('NotReady'))) {
    console.log('Some Core or BU databases are not ready yet. Check Pod/PVC status and image availability before connecting; rerun npm run kube:db-info after they are ready.');
  }
  return rows;
}

if (process.argv[1] && fileURLToPath(import.meta.url) === resolve(process.argv[1])) {
  try {
    const context = currentContext();
    const namespaces = configuredNamespaces();
    const forwards = databasePortForwards({ context, namespaces });
    reportBuDatabaseConnections({ context, namespaces });
    reportDatabasePortForwards({ context, forwards });
  } catch (error) {
    console.error(`Cannot report Core and BU database connections: ${error.message}`);
    process.exitCode = 1;
  }
}

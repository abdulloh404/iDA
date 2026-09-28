import { spawnSync } from 'node:child_process';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
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

export function reportBuDatabaseConnections({ context = currentContext() } = {}) {
  const namespaces = configuredNamespaces();

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
    reportBuDatabaseConnections();
  } catch (error) {
    console.error(`Cannot report Core and BU database connections: ${error.message}`);
    process.exitCode = 1;
  }
}

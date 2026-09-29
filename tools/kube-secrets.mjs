import { spawnSync } from 'node:child_process';
import { createHash, randomBytes } from 'node:crypto';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { parseEnv } from 'node:util';
import { configuredNamespaces } from './kube-namespaces.mjs';

const workspaceRoot = fileURLToPath(new URL('../', import.meta.url));
const queueHost = 'queue.core.svc.cluster.local';
const queueVhost = 'ida';

function readEnv(relativePath) {
  return parseEnv(readFileSync(new URL(`../${relativePath}`, import.meta.url), 'utf8'));
}

function required(values, key, source) {
  const value = values[key];
  if (!value?.trim() || value.includes('REPLACE_ME') || value.includes('example.invalid')) {
    throw new Error(`Set ${key} in ${source} to a real value before preparing Secrets.`);
  }
  return value;
}

function secret(namespace, name, stringData) {
  return { apiVersion: 'v1', kind: 'Secret', metadata: { namespace, name }, type: 'Opaque', stringData };
}

function databasePasswords(values, prefix) {
  const password = required(values, `${prefix}_DB_PASSWORD`, '.env');
  if (/["\r\n]/.test(password)) {
    throw new Error(`${prefix}_DB_PASSWORD must not contain double quotes or newlines; use a random hex password.`);
  }
  const adminPassword = required(values, `${prefix}_DB_ADMIN_PASSWORD`, '.env');
  if (password === adminPassword) {
    throw new Error(`${prefix}: use different passwords for the database administrator and application.`);
  }
  return { password, adminPassword };
}

function queueAccount(values, key) {
  const connection = required(values, key, '.env');
  let url;
  let username;
  let password;
  let vhost;
  try {
    url = new URL(connection);
    username = decodeURIComponent(url.username);
    password = decodeURIComponent(url.password);
    vhost = decodeURIComponent(url.pathname.slice(1));
  } catch {
    throw new Error(`${key} must be a valid AMQP URI; percent-encode special characters in credentials.`);
  }
  if (url.protocol !== 'amqp:' || url.hostname !== queueHost || (url.port && url.port !== '5672') || vhost !== queueVhost || url.search || url.hash) {
    throw new Error(`${key} must target amqp://${queueHost}:5672/${queueVhost} for this development cluster.`);
  }
  if (!username || !password || username === 'guest' || /[\u0000-\u001f\u007f]/.test(username + password)) {
    throw new Error(`${key} must contain a non-guest username and password without control characters.`);
  }
  return { connection, username, password };
}

function rabbitUser(account) {
  const salt = randomBytes(4);
  const digest = createHash('sha256').update(salt).update(account.password, 'utf8').digest();
  return {
    name: account.username,
    password_hash: Buffer.concat([salt, digest]).toString('base64'),
    hashing_algorithm: 'rabbit_password_hashing_sha256',
    tags: [],
  };
}

function queueDefinitions(publisher, workers) {
  const accounts = [publisher, ...workers.map((worker) => worker.account)];
  if (new Set(accounts.map((account) => account.username)).size !== accounts.length) {
    throw new Error('Use a different Queue username for Core and each BU to preserve permissions.');
  }
  return {
    users: accounts.map(rabbitUser),
    vhosts: [{ name: queueVhost }],
    permissions: [
      { user: publisher.username, vhost: queueVhost, configure: '^$', write: '^jobs$', read: '^$' },
      ...workers.map(({ branch, account }) => ({ user: account.username, vhost: queueVhost, configure: '^$', write: '^$', read: `^jobs\\.${branch}$` })),
    ],
    exchanges: [{ name: 'jobs', vhost: queueVhost, type: 'direct', durable: true, auto_delete: false, internal: false, arguments: {} }],
    queues: workers.map(({ branch }) => ({ name: `jobs.${branch}`, vhost: queueVhost, durable: true, auto_delete: false, arguments: { 'x-queue-type': 'classic' } })),
    bindings: workers.map(({ branch }) => ({ source: 'jobs', vhost: queueVhost, destination: `jobs.${branch}`, destination_type: 'queue', routing_key: `jobs.${branch}`, arguments: {} })),
  };
}

function kubectl(args, input) {
  const result = spawnSync('kubectl', args, { cwd: workspaceRoot, input, encoding: 'utf8', stdio: ['pipe', 'pipe', 'pipe'] });
  if (result.error || result.status !== 0) {
    throw new Error('kubectl apply failed. Check kubectl availability, the current context and namespace/Secret permissions. Raw output is hidden to protect credentials.');
  }
  process.stdout.write(result.stdout);
}

try {
  const namespaces = configuredNamespaces();
  const coreNamespace = namespaces.find(({ role }) => role === 'core').name;
  const branches = namespaces.filter(({ role }) => role === 'bu');
  const values = { ...readEnv('.env'), ...process.env };
  const coreDatabase = databasePasswords(values, 'CORE');
  const publisher = queueAccount(values, 'CORE_QUEUE_CONNECTION');
  const workers = [];
  const secrets = [
    secret(coreNamespace, 'core-secrets', {
      ConnectionStrings__Core: `${required(values, 'CORE_DB_CONNECTION', '.env')};Password="${coreDatabase.password}"`,
      Queue__ConnectionString: publisher.connection,
    }),
    secret(coreNamespace, 'core-database-secrets', {
      POSTGRES_PASSWORD: coreDatabase.adminPassword,
      APP_DB_PASSWORD: coreDatabase.password,
    }),
  ];

  for (const { name: branch, bu: prefix } of branches) {
    const database = databasePasswords(values, prefix);
    const account = queueAccount(values, `${prefix}_QUEUE_CONNECTION`);
    workers.push({ branch, account });
    secrets.push(
      secret(branch, 'worker-secrets', {
        Database__Password: database.password,
        Queue__ConnectionString: account.connection,
      }),
      secret(branch, 'database-secrets', { POSTGRES_PASSWORD: database.adminPassword }),
    );
  }
  secrets.push(secret(coreNamespace, 'queue-definitions', { 'definitions.json': JSON.stringify(queueDefinitions(publisher, workers)) }));

  const namespaceResources = namespaces.map(({ name, role, bu }) => ({
    apiVersion: 'v1',
    kind: 'Namespace',
    metadata: { name, labels: { 'ida.io/role': role, ...(bu ? { 'ida.io/bu': bu } : {}) } },
  }));
  kubectl(['apply', '-f', '-'], JSON.stringify({ apiVersion: 'v1', kind: 'List', items: namespaceResources }));
  kubectl(['apply', '-f', '-'], JSON.stringify({ apiVersion: 'v1', kind: 'List', items: secrets }));
  console.log(`Prepared Secrets for Core DB, RabbitMQ and ${branches.map(({ bu }) => bu).join(', ')}. Workload manifests were not applied and no rollout restart was requested.`);
} catch (error) {
  console.error(error.message);
  process.exitCode = 1;
}

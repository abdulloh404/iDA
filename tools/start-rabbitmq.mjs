import { spawn } from 'node:child_process';
import { createHash, randomBytes } from 'node:crypto';
import { chmod, mkdir, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';

const projectRoot = fileURLToPath(new URL('../', import.meta.url));
const composeFile = fileURLToPath(new URL('../docker/rabbitmq/docker-compose.yml', import.meta.url));
const definitionsDirectory = fileURLToPath(new URL('../.nx/rabbitmq/', import.meta.url));
const definitionsFile = fileURLToPath(new URL('../.nx/rabbitmq/definitions.json', import.meta.url));

function commandLineValue(args, ...keys) {
  let value;
  for (let index = 0; index < args.length; index += 1) {
    for (const key of keys) {
      if (args[index].startsWith('--' + key + '=')) {
        value = args[index].slice(key.length + 3);
      } else if (args[index] === '--' + key && args[index + 1] && !args[index + 1].startsWith('--')) {
        value = args[index + 1];
      }
    }
  }
  return value;
}

function passwordHash(password) {
  const salt = randomBytes(4);
  const digest = createHash('sha256').update(salt).update(password, 'utf8').digest();
  return Buffer.concat([salt, digest]).toString('base64');
}

function resourcePattern(resources) {
  return resources.size ? '^(' + [...resources].map((value) => value.replace(/[.*+?^$(){}|[\]\\]/g, '\\$&')).join('|') + ')$' : '^$';
}

function definitionsFromEnvironment(args, port) {
  const connections = new Map(Object.entries(process.env).filter(([key, value]) =>
    value && (key === 'CORE_QUEUE_CONNECTION' || /^BU[0-9]+_QUEUE_CONNECTION$/.test(key))));
  const queueOverride = commandLineValue(args, 'Queue:ConnectionString') ?? process.env.Queue__ConnectionString;
  if (queueOverride) {
    const buId = (commandLineValue(args, 'Bu:Id', 'BU_ID') ?? process.env.Bu__Id ?? process.env.BU_ID ?? 'BU01').trim().toUpperCase();
    if (!/^BU[0-9]+$/.test(buId)) throw new Error('Set Bu:Id or BU_ID to a BU number such as BU01.');
    connections.set(buId + '_QUEUE_CONNECTION', queueOverride);
  }

  const users = new Map();
  const vhosts = new Set();
  const queues = new Map();
  const permissions = new Map();
  for (const [key, value] of connections) {
    let uri;
    try {
      uri = new URL(value);
    } catch {
      throw new Error(key + ' must be a valid AMQP URI.');
    }
    if (!['localhost', '127.0.0.1', '[::1]'].includes(uri.hostname)) continue;
    if (uri.protocol !== 'amqp:' || Number(uri.port || 5672) !== port) {
      throw new Error(key + ' must use amqp and the configured local RABBITMQ_PORT (' + port + ').');
    }
    let user;
    let password;
    let vhost;
    try {
      user = decodeURIComponent(uri.username);
      password = decodeURIComponent(uri.password);
      vhost = uri.pathname ? decodeURIComponent(uri.pathname.slice(1)) : '/';
    } catch {
      throw new Error(key + ' contains invalid URI encoding.');
    }
    if (!user || !password) throw new Error(key + ' must include a RabbitMQ username and password.');
    if (users.has(user) && users.get(user).password !== password) {
      throw new Error('RabbitMQ connection settings must use the same password for a shared username.');
    }
    users.set(user, { password, definition: { name: user, password_hash: passwordHash(password), hashing_algorithm: 'rabbit_password_hashing_sha256', tags: [] } });
    vhosts.add(vhost);
    const permissionKey = JSON.stringify([user, vhost]);
    const permission = permissions.get(permissionKey) ?? { user, vhost, configure: new Set(), write: new Set(), read: new Set() };
    if (key === 'CORE_QUEUE_CONNECTION') {
      permission.write.add('amq.default');
    } else {
      const queueName = 'jobs.' + key.slice(0, -'_QUEUE_CONNECTION'.length).toLowerCase();
      queues.set(JSON.stringify([vhost, queueName]), { name: queueName, vhost, durable: true, auto_delete: false, arguments: { 'x-queue-type': 'classic' } });
      permission.configure.add(queueName);
      permission.read.add(queueName);
    }
    permissions.set(permissionKey, permission);
  }
  return {
    users: [...users.values()].map(({ definition }) => definition),
    vhosts: [...vhosts].map((name) => ({ name })),
    permissions: [...permissions.values()].map(({ user, vhost, configure, write, read }) => ({
      user, vhost, configure: resourcePattern(configure), write: resourcePattern(write), read: resourcePattern(read),
    })),
    queues: [...queues.values()],
  };
}

function runCompose(args, env, signal) {
  return new Promise((resolve, reject) => {
    const child = spawn('docker', ['compose', '-f', composeFile, ...args], {
      cwd: projectRoot,
      env,
      signal,
      stdio: 'inherit',
    });
    child.once('error', (error) => reject(error.name === 'AbortError' ? error :
      new Error('Unable to run Docker Compose. Make sure Docker and the Compose plugin are available.')));
    child.once('exit', (code, exitSignal) => {
      if (code === 0) resolve();
      else reject(new Error('RabbitMQ Docker Compose failed (' + (code ?? exitSignal) + ').'));
    });
  });
}

export async function startRabbitMq({ args = [], signal } = {}) {
  signal?.throwIfAborted();
  const port = Number(process.env.RABBITMQ_PORT ?? 5672);
  if (!Number.isInteger(port) || port < 1 || port > 65535) {
    throw new Error('RABBITMQ_PORT must be an integer between 1 and 65535.');
  }
  const definitions = definitionsFromEnvironment(args, port);
  if (!definitions.users.length) {
    process.stdout.write('Using configured external RabbitMQ; local Docker startup skipped.\n');
    return;
  }
  await mkdir(definitionsDirectory, { recursive: true, mode: 0o700 });
  await chmod(definitionsDirectory, 0o700);
  await writeFile(definitionsFile, JSON.stringify(definitions, null, 2) + '\n', { mode: 0o644, signal });
  await chmod(definitionsFile, 0o644);
  const env = { ...process.env, RABBITMQ_PORT: String(port) };
  process.stdout.write('Starting RabbitMQ with Docker Compose...\n');
  await runCompose(['up', '-d', '--wait', '--wait-timeout', '90', 'rabbitmq'], env, signal);
  await runCompose(['exec', '-T', 'rabbitmq', 'rabbitmqctl', 'import_definitions', '/etc/rabbitmq/definitions.json'], env, signal);
  process.stdout.write('RabbitMQ is ready; starting API and workers.\n');
}

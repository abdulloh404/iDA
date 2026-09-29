import { spawnSync } from 'node:child_process';
import { readFileSync, readdirSync, rmSync } from 'node:fs';
import { basename, isAbsolute, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { databasePortForwardArgs, databasePortForwardDirectory } from './kube-db-info.mjs';
import { idaNamespace } from './kube-namespaces.mjs';

const workspaceRoot = fileURLToPath(new URL('../', import.meta.url));

function runKubectl(args) {
  const result = spawnSync('kubectl', args, {
    cwd: workspaceRoot,
    encoding: 'utf8',
    stdio: ['ignore', 'pipe', 'pipe'],
  });
  if (result.error || result.status !== 0) {
    throw new Error(result.stderr?.trim() || result.error?.message || `kubectl ${args[0]} failed.`);
  }
  return result.stdout.trim();
}

function kubectl(context, args) {
  return runKubectl(['--context', context, ...args]);
}

function ownedNamespace(metadata) {
  const labels = metadata.labels ?? {};
  return idaNamespace(metadata.name, labels['ida.io/role'], labels['ida.io/bu']).name;
}

function ownedLocalVolume(volume) {
  const name = volume.metadata?.name;
  const labels = volume.metadata?.labels ?? {};
  const namespace = labels['ida.io/namespace'];
  const claim = volume.spec?.claimRef;
  const path = volume.spec?.local?.path;
  if (!name
    || labels['ida.io/storage'] !== 'local'
    || !namespace
    || (namespace !== 'core' && !/^bu[0-9]{2,}$/.test(namespace))
    || claim?.namespace !== namespace
    || !claim?.name
    || name !== `ida-local-${namespace}-${claim.name}`
    || !isAbsolute(path ?? '')
    || (volume.spec?.storageClassName ?? '') !== ''
    || (volume.spec?.volumeMode ?? 'Filesystem') !== 'Filesystem'
    || !volume.spec?.nodeAffinity) {
    throw new Error(`PersistentVolume ${name ?? '<unnamed>'} has the iDA local-storage label but does not match the owned local PV shape. Nothing was deleted.`);
  }
  return name;
}

function removeForwardState(stateDirectory, stateName, port) {
  rmSync(join(stateDirectory, stateName), { force: true });
  rmSync(join(stateDirectory, stateName.slice(0, -5) + '.log'), { force: true });
  console.log(`Removed port-forward state for port ${port}.`);
}

function stopManagedPortForwards(context) {
  const stateDirectory = databasePortForwardDirectory(context);
  let entries;
  try {
    entries = readdirSync(stateDirectory, { withFileTypes: true });
  } catch (error) {
    if (error.code === 'ENOENT') return;
    throw error;
  }

  for (const entry of entries) {
    if (!entry.isFile() || !entry.name.endsWith('.json')) continue;
    const match = /^(.+)-([0-9]+)\.json$/.exec(entry.name);
    if (!match) continue;
    const [, namespace, portText] = match;
    const port = Number(portText);
    let pid;
    try {
      ({ pid } = JSON.parse(readFileSync(join(stateDirectory, entry.name), 'utf8')));
    } catch {
      continue;
    }
    if (!Number.isInteger(pid) || pid <= 0 || !Number.isInteger(port) || port < 1 || port > 65535) continue;

    let command;
    try {
      command = readFileSync(`/proc/${pid}/cmdline`, 'utf8').split('\0').filter(Boolean);
    } catch (error) {
      if (error.code === 'ENOENT') removeForwardState(stateDirectory, entry.name, port);
      else throw error;
      continue;
    }

    const args = command.slice(1);
    const pair = /^([1-9][0-9]*):([1-9][0-9]*)$/.exec(args[9] ?? '');
    const address = args[6];
    const service = args[8]?.startsWith('service/') ? args[8].slice('service/'.length) : '';
    const remotePort = Number(pair?.[2]);
    const forward = { namespace, address, service, port, remotePort };
    const isCore = namespace === 'core';
    const isBranch = /^bu[0-9]{2,}$/.test(namespace);
    const expectedService = isCore ? 'core-db-client' : 'bu-db-client';
    const isDatabase = (isCore || isBranch) && address === '0.0.0.0' && service === expectedService;
    const isQueue = isCore && address === '127.0.0.1' && service === 'queue';
    const expectedArgs = databasePortForwardArgs(context, forward);
    const isManaged = basename(command[0] ?? '') === 'kubectl'
      && (isDatabase || isQueue)
      && Number(pair?.[1]) === port
      && remotePort >= 1
      && remotePort <= 65535
      && JSON.stringify(args) === JSON.stringify(expectedArgs);
    if (!isManaged) continue;

    try {
      process.kill(pid, 'SIGTERM');
    } catch (error) {
      if (error.code !== 'ESRCH') throw error;
    }
    removeForwardState(stateDirectory, entry.name, port);
    console.log(`Stopped managed port-forward for ${namespace}/${service} (PID ${pid}).`);
  }

  rmSync(stateDirectory, { recursive: true, force: true });
}

try {
  const context = runKubectl(['config', 'current-context']);
  if (!context) throw new Error('kubectl has no current context.');

  const namespaceResponse = JSON.parse(kubectl(context, ['get', 'namespace', '-l', 'ida.io/role', '-o', 'json']));
  const namespaces = namespaceResponse.items.map(({ metadata }) => ownedNamespace(metadata));
  const volumeResponse = JSON.parse(kubectl(context, ['get', 'pv', '-l', 'ida.io/storage=local', '-o', 'json']));
  const volumes = volumeResponse.items.map(ownedLocalVolume);

  for (const volume of volumeResponse.items) {
    if (volume.spec?.persistentVolumeReclaimPolicy === 'Retain') continue;
    const output = kubectl(context, [
      'patch',
      'pv',
      volume.metadata.name,
      '--type=merge',
      '-p',
      JSON.stringify({ spec: { persistentVolumeReclaimPolicy: 'Retain' } }),
    ]);
    if (output) console.log(output);
  }

  stopManagedPortForwards(context);

  if (namespaces.length) {
    console.log(`Purging namespaces ${namespaces.join(', ')} from context ${context}, including their Secrets and PVCs.`);
    const output = kubectl(context, ['delete', 'namespace', ...namespaces, '--ignore-not-found=true']);
    if (output) console.log(output);
  }

  if (volumes.length) {
    console.log(`Removing iDA local PersistentVolumes ${volumes.join(', ')} while preserving their local data.`);
    const output = kubectl(context, ['delete', 'pv', ...volumes, '--ignore-not-found=true']);
    if (output) console.log(output);
  }

  if (!namespaces.length && !volumes.length) {
    console.log(`No iDA namespaces or local PersistentVolumes remain in context ${context}.`);
  }
  console.log('KUBE_STORAGE_ROOT and all files beneath it were left unchanged.');
} catch (error) {
  console.error(error.message);
  process.exitCode = 1;
}

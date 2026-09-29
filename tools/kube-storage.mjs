import { spawnSync } from 'node:child_process';
import { existsSync, lstatSync, mkdirSync, readFileSync } from 'node:fs';
import { hostname } from 'node:os';
import { dirname, isAbsolute, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { parseEnv } from 'node:util';
import { configuredNamespaces } from './kube-namespaces.mjs';
import { localStorageUser, renderKubernetes } from './kube-render.mjs';

const workspaceRoot = fileURLToPath(new URL('../', import.meta.url));
const statefulSetTemplate = 'go-template={{if eq .kind "StatefulSet"}}{{.metadata.namespace}}{{"\\t"}}{{.metadata.name}}{{"\\n"}}{{end}}';

function run(command, args, { input, inherit = false } = {}) {
  const result = spawnSync(command, args, { cwd: workspaceRoot, input, encoding: 'utf8', stdio: inherit ? 'inherit' : ['pipe', 'pipe', 'pipe'], maxBuffer: 16 * 1024 * 1024 });
  if (result.error || result.status !== 0) {
    throw new Error(result.stderr?.trim() || result.error?.message || `${command} failed with exit code ${result.status ?? 'unknown'}.`);
  }
  return result.stdout?.trim() ?? '';
}

function kubectl(context, args, input) {
  return run('kubectl', ['--context', context, '--request-timeout=15s', ...args], { input });
}

function getResource(context, kind, name, namespace, waitForCreation = false) {
  const args = namespace ? ['-n', namespace] : [];
  const deadline = Date.now() + (waitForCreation ? 60_000 : 0);
  while (true) {
    const output = kubectl(context, [...args, 'get', kind, name, '--ignore-not-found=true', '-o', 'json']);
    if (output) {
      const resource = JSON.parse(output);
      if (!waitForCreation || kind !== 'pvc' || resource.status?.phase) return resource;
    }
    if (Date.now() >= deadline) throw new Error(`${kind} ${namespace ? `${namespace}/` : ''}${name} is missing or has no status after applying the manifests. Check StatefulSet events.`);
    Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, 500);
  }
}

function storageRoot(user) {
  const envPath = join(workspaceRoot, '.env');
  const values = { ...(existsSync(envPath) ? parseEnv(readFileSync(envPath, 'utf8')) : {}), ...process.env };
  const configured = values.KUBE_STORAGE_ROOT || '/var/lib/ida/storage';
  const expanded = configured.startsWith('~/') ? join(user.homedir, configured.slice(2)) : configured;
  const root = resolve(expanded);
  if (!isAbsolute(expanded) || root === dirname(root) || root === resolve(user.homedir)) {
    throw new Error('KUBE_STORAGE_ROOT must be a dedicated absolute directory or a path under ~/, such as /var/lib/ida/storage.');
  }
  return root;
}

function directory(path) {
  for (let current = path; current !== dirname(current); current = dirname(current)) {
    try {
      const info = lstatSync(current);
      if (info.isSymbolicLink() || !info.isDirectory()) throw new Error(`Storage path component ${current} must be a real directory, not a symlink or file.`);
    } catch (error) {
      if (error.code !== 'ENOENT') throw error;
    }
  }
  try {
    return lstatSync(path);
  } catch (error) {
    if (error.code === 'ENOENT') return undefined;
    throw error;
  }
}

function userDirectory(path, user) {
  const info = directory(path);
  if (info && (info.uid !== user.uid || info.gid !== user.gid || (info.mode & 0o700) !== 0o700)) {
    throw new Error(`Storage directory ${path} must be owned and accessible by ${user.username} (${user.uid}:${user.gid}); found ${info.uid}:${info.gid}. Choose a fresh KUBE_STORAGE_ROOT owned by this user. Existing data was not changed.`);
  }
  return info;
}

export function checkLocalStorage(context) {
  const user = localStorageUser();
  const root = storageRoot(user);
  userDirectory(root, user);
  const namespaces = new Set(configuredNamespaces().map(({ name }) => name));
  const existingVolumes = JSON.parse(kubectl(context, ['get', 'pv', '-o', 'json'])).items;
  for (const volume of existingVolumes) {
    if (volume.metadata.labels?.['ida.io/storage'] !== 'local') continue;
    const claim = volume.spec.claimRef;
    if (!namespaces.has(claim?.namespace)) continue;
    const expectedPath = join(root, claim.namespace, claim.name);
    if (volume.spec.local?.path !== expectedPath) {
      throw new Error(`PV ${volume.metadata.name} still uses ${volume.spec.local?.path}; the configured path for ${user.username} is ${expectedPath}. Remove the old deployment and PV before creating fresh storage at this path. npm run kube:purge removes iDA resources while keeping saved directories. No resources or data were changed.`);
    }
    userDirectory(dirname(expectedPath), user);
    const info = userDirectory(expectedPath, user);
    if (!info && volume.status?.phase === 'Bound') {
      throw new Error(`Bound PV ${volume.metadata.name} has lost its directory ${expectedPath}; restore its data before applying.`);
    }
  }
  return { user, root, namespaces, existingVolumes };
}

function localNode(context) {
  const nodes = JSON.parse(kubectl(context, ['get', 'nodes', '-o', 'json'])).items;
  const node = nodes[0];
  if (process.platform !== 'linux' || nodes.length !== 1 || node?.metadata.labels?.['kubernetes.io/hostname'] !== hostname()) {
    throw new Error('Local storage must be prepared on the only Node of this Linux cluster. Remote or multi-node clusters need a separate storage plan.');
  }
  if (node.spec?.unschedulable || !node.status?.conditions?.some(({ type, status }) => type === 'Ready' && status === 'True')) {
    throw new Error(`Node ${node.metadata.name} must be Ready and schedulable before preparing local storage.`);
  }
  return node;
}

function statefulSets(context, manifest, namespaces) {
  const output = kubectl(context, ['create', '--dry-run=client', '--validate=false', '-f', '-', '-o', statefulSetTemplate], manifest);
  return output.split('\n').filter(Boolean).map((line) => {
    const [namespace, name, ...extra] = line.split('\t');
    if (!namespace || !name || extra.length || !namespaces.has(namespace)) throw new Error('Invalid or unowned StatefulSet in the Kubernetes manifests.');
    return getResource(context, 'statefulset', name, namespace);
  });
}

function volumeFor(claim, path, node) {
  const { name, namespace, uid } = claim.metadata;
  return {
    apiVersion: 'v1',
    kind: 'PersistentVolume',
    metadata: { name: `ida-local-${namespace}-${name}`, labels: { 'ida.io/storage': 'local', 'ida.io/namespace': namespace } },
    spec: {
      capacity: { storage: claim.spec.resources.requests.storage },
      volumeMode: 'Filesystem',
      accessModes: claim.spec.accessModes,
      persistentVolumeReclaimPolicy: 'Retain',
      storageClassName: '',
      claimRef: { apiVersion: 'v1', kind: 'PersistentVolumeClaim', name, namespace, uid },
      local: { path },
      nodeAffinity: { required: { nodeSelectorTerms: [{ matchExpressions: [{ key: 'kubernetes.io/hostname', operator: 'In', values: [node.metadata.labels['kubernetes.io/hostname']] }] }] } },
    },
  };
}

function matchingVolume(existing, expected) {
  const spec = existing.spec;
  const desired = expected.spec;
  return existing.metadata.labels?.['ida.io/storage'] === 'local'
    && !['Released', 'Failed'].includes(existing.status?.phase)
    && spec.local?.path === desired.local.path
    && (spec.storageClassName ?? '') === ''
    && spec.persistentVolumeReclaimPolicy === 'Retain'
    && spec.capacity?.storage === desired.capacity.storage
    && (spec.volumeMode ?? 'Filesystem') === 'Filesystem'
    && JSON.stringify(spec.accessModes) === JSON.stringify(desired.accessModes)
    && spec.claimRef?.uid === desired.claimRef.uid
    && spec.claimRef?.name === desired.claimRef.name
    && spec.claimRef?.namespace === desired.claimRef.namespace
    && JSON.stringify(spec.nodeAffinity) === JSON.stringify(desired.nodeAffinity);
}

export function prepareLocalStorage({ restoreExisting = true } = {}) {
  const context = run('kubectl', ['config', 'current-context']);
  if (!context) throw new Error('kubectl has no current context.');
  const node = localNode(context);
  const { user, root, namespaces, existingVolumes } = checkLocalStorage(context);
  const manifest = renderKubernetes(user);
  const applied = kubectl(context, ['apply', '-f', '-'], manifest);
  if (applied) console.log(applied);
  const plans = [];
  const rows = [];

  for (const workload of statefulSets(context, manifest, namespaces)) {
    const { name, namespace } = workload.metadata;
    const security = workload.spec.template.spec.securityContext;
    if (security?.runAsUser !== user.uid || security?.runAsGroup !== user.gid || security?.fsGroup !== user.gid) {
      throw new Error(`StatefulSet ${namespace}/${name} must use the storage owner's UID/GID ${user.uid}:${user.gid}. Render it with npm run kube:render.`);
    }
    for (let ordinal = workload.spec.ordinals?.start ?? 0; ordinal < (workload.spec.ordinals?.start ?? 0) + (workload.spec.replicas ?? 1); ordinal++) {
      for (const template of workload.spec.volumeClaimTemplates ?? []) {
        const claimName = `${template.metadata.name}-${name}-${ordinal}`;
        const claim = getResource(context, 'pvc', claimName, namespace, true);
        if (claim.status?.phase === 'Bound') {
          rows.push({ Namespace: namespace, PVC: claimName, PV: claim.spec.volumeName, Status: 'Already Bound; unchanged' });
          continue;
        }
        if (claim.status?.phase !== 'Pending' || claim.spec.storageClassName || claim.spec.selector || claim.spec.dataSource || claim.spec.dataSourceRef || (claim.spec.volumeMode ?? 'Filesystem') !== 'Filesystem' || JSON.stringify(claim.spec.accessModes) !== '["ReadWriteOnce"]') {
          throw new Error(`PVC ${namespace}/${claimName} must be Pending, classless, ReadWriteOnce and Filesystem without a selector or data source. It was not changed.`);
        }
        const path = join(root, namespace, claimName);
        const expected = volumeFor(claim, path, node);
        const existing = existingVolumes.find(({ metadata }) => metadata.name === expected.metadata.name);
        if (claim.spec.volumeName && claim.spec.volumeName !== expected.metadata.name) throw new Error(`PVC ${namespace}/${claimName} already requests another PV; it was not changed.`);
        if (existing && !matchingVolume(existing, expected)) throw new Error(`PV ${expected.metadata.name} is retained, reserved for another PVC, or has different settings. Review it manually; no data or claimRef was changed.`);
        if (existingVolumes.some((volume) => volume.metadata.name !== expected.metadata.name && (volume.spec.local?.path === path || volume.spec.claimRef?.uid === claim.metadata.uid))) {
          throw new Error(`Another PV already uses ${path} or PVC ${namespace}/${claimName}; refusing to create overlapping storage.`);
        }
        userDirectory(dirname(path), user);
        const info = userDirectory(path, user);
        if (info && !existing && !restoreExisting) throw new Error(`Storage directory ${path} already exists without its matching PV. Run npm run kube:restore to reuse the saved data.`);
        if (!info && existing?.status?.phase === 'Bound') throw new Error(`Bound PV ${existing.metadata.name} has lost its directory ${path}; restore its data manually instead of creating empty storage.`);
        plans.push({ volume: expected, existing, path, info });
        rows.push({ Namespace: namespace, PVC: claimName, PV: expected.metadata.name, Capacity: expected.spec.capacity.storage, Path: path, Status: existing ? 'Existing PV; unchanged' : info ? 'Restore saved storage' : 'Create local PV' });
      }
    }
  }

  if (!rows.length) throw new Error('No StatefulSet PVCs are configured in the Kubernetes manifests.');
  console.log(`Preparing local storage at ${root} as ${user.username} (${user.uid}:${user.gid}) on ${node.metadata.name} in context ${context}:`);
  console.table(rows);
  if (plans.some(({ info }) => !info)) {
    try {
      mkdirSync(root, { recursive: true, mode: 0o700 });
    } catch (error) {
      if (!['EACCES', 'EPERM'].includes(error.code) || directory(root)) throw error;
      run('sudo', ['install', '-d', '-m', '0700', '-o', String(user.uid), '-g', String(user.gid), '--', root], { inherit: true });
    }
    userDirectory(root, user);
  }
  for (const { path, info } of plans) {
    if (info) continue;
    mkdirSync(path, { recursive: true, mode: 0o700 });
    userDirectory(root, user);
    userDirectory(dirname(path), user);
    userDirectory(path, user);
  }

  // PVC เดิมไม่มี StorageClass จึงจอง classless Local PV ด้วย claim UID โดยไม่แก้ PVC/StatefulSet หรือ default ของคลัสเตอร์
  const volumes = plans.filter(({ existing }) => !existing).map(({ volume }) => volume);
  if (volumes.length) console.log(kubectl(context, ['create', '-f', '-'], JSON.stringify({ apiVersion: 'v1', kind: 'List', items: volumes })));
  console.log('Local storage prepared. Saved directories were reused where present; missing directories were created for new workloads. No existing data was deleted or replaced.');
  console.log('Existing Pending Pods can now bind their storage and start automatically. Run npm run kube:apply to open database port-forwards, then npm run kube:db-info.');
}

if (process.argv[1] && fileURLToPath(import.meta.url) === resolve(process.argv[1])) {
  try {
    prepareLocalStorage();
  } catch (error) {
    console.error(`Local storage preparation failed: ${error.message} Some Kubernetes resources or storage directories may already exist; no cleanup or PVC deletion was attempted.`);
    process.exitCode = 1;
  }
}

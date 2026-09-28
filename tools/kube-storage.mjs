import { spawnSync } from 'node:child_process';
import { existsSync, lstatSync, readFileSync } from 'node:fs';
import { hostname } from 'node:os';
import { dirname, isAbsolute, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { parseEnv } from 'node:util';
import { configuredNamespaces } from './kube-namespaces.mjs';

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

function getResource(context, kind, name, namespace) {
  const args = namespace ? ['-n', namespace] : [];
  const output = kubectl(context, [...args, 'get', kind, name, '--ignore-not-found=true', '-o', 'json']);
  if (!output) throw new Error(`${kind} ${namespace ? `${namespace}/` : ''}${name} is missing. Run npm run kube:apply first, then rerun npm run kube:storage after the PVCs exist.`);
  return JSON.parse(output);
}

function storageRoot() {
  const envPath = join(workspaceRoot, '.env');
  const values = { ...(existsSync(envPath) ? parseEnv(readFileSync(envPath, 'utf8')) : {}), ...process.env };
  const configured = values.KUBE_STORAGE_ROOT || '/var/lib/ida/storage';
  const root = resolve(configured);
  if (!isAbsolute(configured) || !root.startsWith('/var/lib/ida/')) {
    throw new Error('KUBE_STORAGE_ROOT must be a dedicated absolute directory under /var/lib/ida/, such as /var/lib/ida/storage.');
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

function statefulSets(context) {
  const namespaces = new Set(configuredNamespaces().map(({ name }) => name));
  const manifest = kubectl(context, ['kustomize', 'infrastructure/kubernetes']);
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

try {
  const context = run('kubectl', ['config', 'current-context']);
  if (!context) throw new Error('kubectl has no current context.');
  const root = storageRoot();
  directory(root);
  const node = localNode(context);
  const existingVolumes = JSON.parse(kubectl(context, ['get', 'pv', '-o', 'json'])).items;
  const plans = [];
  const rows = [];

  for (const workload of statefulSets(context)) {
    const { name, namespace } = workload.metadata;
    const security = workload.spec.template.spec.securityContext;
    for (let ordinal = workload.spec.ordinals?.start ?? 0; ordinal < (workload.spec.ordinals?.start ?? 0) + (workload.spec.replicas ?? 1); ordinal++) {
      for (const template of workload.spec.volumeClaimTemplates ?? []) {
        const claimName = `${template.metadata.name}-${name}-${ordinal}`;
        const claim = getResource(context, 'pvc', claimName, namespace);
        if (claim.status?.phase === 'Bound') {
          rows.push({ Namespace: namespace, PVC: claimName, PV: claim.spec.volumeName, Status: 'Already Bound; unchanged' });
          continue;
        }
        if (claim.status?.phase !== 'Pending' || claim.spec.storageClassName || claim.spec.selector || claim.spec.dataSource || claim.spec.dataSourceRef || (claim.spec.volumeMode ?? 'Filesystem') !== 'Filesystem' || JSON.stringify(claim.spec.accessModes) !== '["ReadWriteOnce"]') {
          throw new Error(`PVC ${namespace}/${claimName} must be Pending, classless, ReadWriteOnce and Filesystem without a selector or data source. It was not changed.`);
        }
        if (![security?.runAsUser, security?.runAsGroup].every((id) => Number.isSafeInteger(id) && id > 0)) {
          throw new Error(`StatefulSet ${namespace}/${name} needs explicit non-root runAsUser/runAsGroup for local directory ownership.`);
        }
        const path = join(root, namespace, claimName);
        const expected = volumeFor(claim, path, node);
        const existing = existingVolumes.find(({ metadata }) => metadata.name === expected.metadata.name);
        if (claim.spec.volumeName && claim.spec.volumeName !== expected.metadata.name) throw new Error(`PVC ${namespace}/${claimName} already requests another PV; it was not changed.`);
        if (existing && !matchingVolume(existing, expected)) throw new Error(`PV ${expected.metadata.name} is retained, reserved for another PVC, or has different settings. Review it manually; no data or claimRef was changed.`);
        if (existingVolumes.some((volume) => volume.metadata.name !== expected.metadata.name && (volume.spec.local?.path === path || volume.spec.claimRef?.uid === claim.metadata.uid))) {
          throw new Error(`Another PV already uses ${path} or PVC ${namespace}/${claimName}; refusing to create overlapping storage.`);
        }
        const info = directory(path);
        if (info && !existing) throw new Error(`Storage directory ${path} already exists without its matching PV. Review any existing data before reusing it.`);
        if (info && (info.uid !== security.runAsUser || info.gid !== security.runAsGroup || (info.mode & 0o700) !== 0o700)) throw new Error(`Existing directory ${path} has different ownership/permissions; it was not changed.`);
        if (!info && existing?.status?.phase === 'Bound') throw new Error(`Bound PV ${existing.metadata.name} has lost its directory ${path}; restore its data manually instead of creating empty storage.`);
        plans.push({ volume: expected, existing, path, info, uid: security.runAsUser, gid: security.runAsGroup });
        rows.push({ Namespace: namespace, PVC: claimName, PV: expected.metadata.name, Capacity: expected.spec.capacity.storage, Path: path, Status: existing ? 'Existing PV; unchanged' : 'Create local PV' });
      }
    }
  }

  if (!rows.length) throw new Error('No StatefulSet PVCs are configured. Run npm run kube:apply first.');
  console.log(`Preparing local storage on ${node.metadata.name} in context ${context}:`);
  console.table(rows);
  const parentPaths = [...new Set(plans.filter(({ info }) => !info).flatMap(({ path }) => [root, dirname(path)]))].filter((path) => !directory(path));
  if (parentPaths.length) run('sudo', ['install', '-d', '-m', '0755', '-o', '0', '-g', '0', '--', ...parentPaths], { inherit: true });
  const owners = new Map();
  for (const { path, info, uid, gid } of plans) {
    if (info) continue;
    const key = `${uid}:${gid}`;
    if (!owners.has(key)) owners.set(key, { uid, gid, paths: [] });
    owners.get(key).paths.push(path);
  }
  for (const { uid, gid, paths } of owners.values()) run('sudo', ['install', '-d', '-m', '0700', '-o', String(uid), '-g', String(gid), '--', ...paths], { inherit: true });

  // PVC เดิมไม่มี StorageClass จึงจอง classless Local PV ด้วย claim UID โดยไม่แก้ PVC/StatefulSet หรือ default ของคลัสเตอร์
  const volumes = plans.filter(({ existing }) => !existing).map(({ volume }) => volume);
  if (volumes.length) console.log(kubectl(context, ['create', '-f', '-'], JSON.stringify({ apiVersion: 'v1', kind: 'List', items: volumes })));
  console.log('Local storage prepared. No PVCs, existing PVs or data were deleted or replaced. Retain keeps local data after PVC deletion; retained PVs require manual review before reuse.');
  console.log('Existing Pending Pods can now bind their storage and start automatically. Rerun npm run kube:db-info after the databases become ready.');
} catch (error) {
  console.error(`Local storage preparation failed: ${error.message} Some new directories/PVs may already exist; no cleanup or PVC deletion was attempted.`);
  process.exitCode = 1;
}

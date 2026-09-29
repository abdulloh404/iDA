import { spawnSync } from 'node:child_process';
import { mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir, userInfo } from 'node:os';
import { relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const workspaceRoot = fileURLToPath(new URL('../', import.meta.url));
const kubernetesDirectory = fileURLToPath(new URL('../infrastructure/kubernetes', import.meta.url));
const normalUserError = 'Local Kubernetes storage requires a normal POSIX user with a UID and GID greater than zero. Run this command without sudo.';

function requireNormalUser(user) {
  if (!Number.isSafeInteger(user?.uid) || !Number.isSafeInteger(user?.gid) || user.uid <= 0 || user.gid <= 0 || !user.username || !user.homedir) {
    throw new Error(normalUserError);
  }
  return user;
}

export function localStorageUser() {
  try {
    return requireNormalUser(userInfo());
  } catch {
    throw new Error(normalUserError);
  }
}

export function renderKubernetes(user = localStorageUser()) {
  const { uid, gid } = requireNormalUser(user);
  const overlayDirectory = mkdtempSync(`${tmpdir()}/ida-kube-render-`);
  const patch = [
    { op: 'add', path: '/spec/template/spec/securityContext/runAsUser', value: uid },
    { op: 'add', path: '/spec/template/spec/securityContext/runAsGroup', value: gid },
    { op: 'add', path: '/spec/template/spec/securityContext/fsGroup', value: gid },
    { op: 'add', path: '/spec/template/spec/securityContext/fsGroupChangePolicy', value: 'OnRootMismatch' },
  ];
  const kustomization = {
    apiVersion: 'kustomize.config.k8s.io/v1beta1',
    kind: 'Kustomization',
    resources: [relative(overlayDirectory, kubernetesDirectory)],
    patches: [
      {
        target: { group: 'apps', version: 'v1', kind: 'StatefulSet', labelSelector: 'ida.io/storage=local' },
        patch: JSON.stringify(patch),
      },
    ],
  };

  try {
    writeFileSync(`${overlayDirectory}/kustomization.yaml`, JSON.stringify(kustomization));
    const result = spawnSync('kubectl', ['kustomize', overlayDirectory, '--load-restrictor=LoadRestrictionsNone'], {
      cwd: workspaceRoot,
      encoding: 'utf8',
      stdio: ['ignore', 'pipe', 'pipe'],
      maxBuffer: 32 * 1024 * 1024,
    });
    if (result.error || result.status !== 0) {
      throw new Error(result.stderr?.trim() || result.error?.message || `kubectl kustomize failed with exit code ${result.status ?? 'unknown'}.`);
    }
    return result.stdout;
  } finally {
    rmSync(overlayDirectory, { recursive: true, force: true });
  }
}

function outputPath(args) {
  if (args.length === 0) return null;
  if (args.length === 2 && args[0] === '--output' && args[1]) return resolve(args[1]);
  throw new Error('Usage: node tools/kube-render.mjs [--output <path>]');
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try {
    const destination = outputPath(process.argv.slice(2));
    const rendered = renderKubernetes();
    if (destination) writeFileSync(destination, rendered);
    else process.stdout.write(rendered);
  } catch (error) {
    console.error(error.message);
    process.exitCode = 1;
  }
}

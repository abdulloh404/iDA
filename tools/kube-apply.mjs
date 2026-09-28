import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { reportBuDatabaseConnections } from './kube-db-info.mjs';

const workspaceRoot = fileURLToPath(new URL('../', import.meta.url));

function currentContext() {
  const result = spawnSync('kubectl', ['config', 'current-context'], {
    cwd: workspaceRoot,
    encoding: 'utf8',
    stdio: ['ignore', 'pipe', 'pipe'],
  });
  if (result.error || result.status !== 0 || !result.stdout.trim()) {
    throw new Error(result.stderr?.trim() || result.error?.message || 'kubectl has no current context.');
  }
  return result.stdout.trim();
}

try {
  const context = currentContext();
  const result = spawnSync('kubectl', ['--context', context, 'apply', '-k', 'infrastructure/kubernetes'], {
    cwd: workspaceRoot,
    stdio: 'inherit',
  });
  if (result.error || result.status !== 0) {
    throw new Error(result.error?.message || `kubectl apply failed with exit code ${result.status ?? 'unknown'}.`);
  }
  try {
    reportBuDatabaseConnections({ context });
  } catch (error) {
    console.error(`Kubernetes manifests were applied, but the BU database connection report failed: ${error.message}`);
    process.exitCode = 1;
  }
} catch (error) {
  console.error(`Kubernetes apply did not complete: ${error.message} Some resources may already have been applied.`);
  process.exitCode = 1;
}

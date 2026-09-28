import { spawnSync } from 'node:child_process';
import { idaNamespace } from './kube-namespaces.mjs';

function kubectl(args) {
  const result = spawnSync('kubectl', args, { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] });
  if (result.error || result.status !== 0) {
    throw new Error(result.stderr?.trim() || result.error?.message || `kubectl ${args[0]} failed.`);
  }
  return result.stdout.trim();
}

try {
  const context = kubectl(['config', 'current-context']);
  const response = JSON.parse(kubectl(['get', 'namespace', '-l', 'ida.io/role', '-o', 'json']));
  const existing = response.items.map(({ metadata }) => {
    const labels = metadata.labels ?? {};
    return idaNamespace(metadata.name, labels['ida.io/role'], labels['ida.io/bu']).name;
  });

  if (existing.length === 0) {
    console.log(`No iDA namespaces remain in context ${context}.`);
  } else {
    console.log(`Purging namespaces ${existing.join(', ')} from context ${context}, including their Secrets and PVCs.`);
    const output = kubectl(['delete', 'namespace', ...existing, '--ignore-not-found=true']);
    if (output) console.log(output);
  }
} catch (error) {
  console.error(error.message);
  process.exitCode = 1;
}

import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const workspaceRoot = fileURLToPath(new URL('../', import.meta.url));
const namespaceTemplate = 'go-template={{if eq .kind "Namespace"}}{{.metadata.name}}{{"\\t"}}{{index .metadata.labels "ida.io/role"}}{{"\\t"}}{{with index .metadata.labels "ida.io/bu"}}{{.}}{{end}}{{"\\n"}}{{end}}';

function kubectl(args, input) {
  const result = spawnSync('kubectl', args, {
    cwd: workspaceRoot,
    input,
    encoding: 'utf8',
    stdio: ['pipe', 'pipe', 'pipe'],
    maxBuffer: 16 * 1024 * 1024,
  });
  if (result.error || result.status !== 0) {
    throw new Error(`Cannot read Kubernetes namespaces from manifests: ${result.stderr?.trim() || result.error?.message || 'kubectl failed.'}`);
  }
  return result.stdout;
}

export function idaNamespace(name, role, bu) {
  if (role === 'core' && name === 'core' && !bu) return { name, role };
  if (role === 'bu' && /^BU[0-9]{2,}$/.test(bu) && name === bu.toLowerCase()) return { name, role, bu };
  throw new Error(`Namespace ${name} has an unexpected iDA role or BU label.`);
}

export function configuredNamespaces() {
  const manifest = kubectl(['kustomize', 'infrastructure/kubernetes']);
  const output = kubectl(['create', '--dry-run=client', '--validate=false', '-f', '-', '-o', namespaceTemplate], manifest);
  const namespaces = output.split('\n').filter(Boolean).map((line) => {
    const [name, role, bu, ...extra] = line.split('\t');
    if (extra.length || !name) throw new Error('Invalid Namespace metadata in Kubernetes manifests.');
    return idaNamespace(name, role, bu);
  });

  if (namespaces.filter(({ role }) => role === 'core').length !== 1 || new Set(namespaces.map(({ name }) => name)).size !== namespaces.length) {
    throw new Error('Kubernetes manifests must define one Core namespace and unique BU namespaces.');
  }
  const core = namespaces.find(({ role }) => role === 'core');
  const branches = namespaces.filter(({ role }) => role === 'bu');
  branches.sort((left, right) => left.name.localeCompare(right.name, undefined, { numeric: true }));
  return [core, ...branches];
}

import { spawnSync } from 'node:child_process';
import { existsSync, mkdtempSync, rmSync } from 'node:fs';
import { hostname, tmpdir } from 'node:os';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';

const workspaceRoot = fileURLToPath(new URL('../', import.meta.url));
const deploymentImageTemplate = 'go-template={{if eq .kind "Deployment"}}{{range .spec.template.spec.initContainers}}{{$.metadata.namespace}}{{"\\t"}}{{$.metadata.name}}{{"\\t"}}{{.image}}{{"\\t"}}{{.imagePullPolicy}}{{"\\n"}}{{end}}{{range .spec.template.spec.containers}}{{$.metadata.namespace}}{{"\\t"}}{{$.metadata.name}}{{"\\t"}}{{.image}}{{"\\t"}}{{.imagePullPolicy}}{{"\\n"}}{{end}}{{end}}';
let archivePath;

function run(command, args, { input, capture = false } = {}) {
  const result = spawn(command, args, { input, capture });
  if (result.error || result.status !== 0) {
    const detail = result.stderr?.trim() || result.error?.message || `exit code ${result.status ?? 'unknown'}`;
    throw new Error(`${command} ${args.join(' ')} failed: ${detail}`);
  }
  return result.stdout?.trim() ?? '';
}

function spawn(command, args, { input, capture }) {
  return spawnSync(command, args, {
    cwd: workspaceRoot,
    encoding: capture ? 'utf8' : undefined,
    input,
    stdio: capture ? ['pipe', 'pipe', 'pipe'] : 'inherit',
  });
}

function runKubectl(context, args, options) {
  return run('kubectl', ['--context', context, '--request-timeout=15s', ...args], options);
}

function currentContext() {
  const context = run('kubectl', ['config', 'current-context'], { capture: true });
  if (!context) {
    throw new Error('kubectl has no current context.');
  }
  return context;
}

function validateLocalSingleNode(context) {
  if (process.platform !== 'linux') {
    throw new Error('Local containerd image import must run on the Linux Kubernetes Node itself.');
  }
  const nodeList = JSON.parse(runKubectl(context, ['get', 'nodes', '-o', 'json'], { capture: true }));
  if (nodeList.items?.length !== 1) {
    throw new Error('Local image import supports exactly one Kubernetes Node. Use a registry for remote or multi-node clusters.');
  }

  const node = nodeList.items[0];
  const nodeName = node.metadata?.name ?? '<unknown>';
  const nodeHostname = node.metadata?.labels?.['kubernetes.io/hostname'];
  const localHostname = hostname();
  const ready = node.status?.conditions?.some(({ type, status }) => type === 'Ready' && status === 'True');
  const runtime = node.status?.nodeInfo?.containerRuntimeVersion ?? '';

  if (!ready) {
    throw new Error(`Kubernetes Node ${nodeName} is not Ready.`);
  }
  if (!nodeHostname || nodeHostname !== localHostname) {
    throw new Error(`Kubernetes Node hostname ${nodeHostname ?? '<missing>'} does not match this machine (${localHostname}). Use a registry or run the import on the Node itself.`);
  }
  if (!runtime.startsWith('containerd://')) {
    throw new Error(`Kubernetes Node ${nodeName} uses ${runtime || '<unknown runtime>'}; this script supports containerd only.`);
  }

  return { nodeName, runtime };
}

function renderedDeploymentImages(context) {
  const manifest = runKubectl(context, ['kustomize', 'infrastructure/kubernetes'], { capture: true });
  const output = runKubectl(
    context,
    ['create', '--dry-run=client', '--validate=false', '-f', '-', '-o', deploymentImageTemplate],
    { input: manifest, capture: true },
  );
  return output.split('\n').filter(Boolean).map((line) => {
    const [namespace, deployment, image, pullPolicy, ...extra] = line.split('\t');
    if (extra.length || !namespace || !deployment || !image) {
      throw new Error('Invalid Deployment image metadata in Kubernetes manifests.');
    }
    return { namespace, deployment, image, pullPolicy };
  });
}

function localApplicationImages(context) {
  const images = new Set();
  for (const { namespace, deployment, image, pullPolicy } of renderedDeploymentImages(context)) {
    if (!image.startsWith('ida/') && !image.startsWith('docker.io/ida/')) {
      continue;
    }
    if (pullPolicy === 'Always') {
      throw new Error(`Deployment ${namespace}/${deployment} uses imagePullPolicy Always for local image ${image}.`);
    }
    images.add(image);
  }
  if (!images.size) {
    throw new Error('No ida/ application images were found in the rendered Deployment manifests.');
  }
  return [...images].sort();
}

function qualifiedImage(image) {
  return image.startsWith('docker.io/') ? image : `docker.io/${image}`;
}

function prepareDockerImages(images) {
  run('npm', ['run', 'docker:build']);
  for (const image of images) {
    run('docker', ['image', 'inspect', image], { capture: true });
    const qualified = qualifiedImage(image);
    if (qualified !== image) {
      run('docker', ['image', 'tag', image, qualified]);
    }
  }
}

function importIntoContainerd(images) {
  const temporaryDirectory = mkdtempSync(join(tmpdir(), 'ida-kube-images-'));
  archivePath = join(temporaryDirectory, 'images.tar');
  const qualifiedImages = images.map(qualifiedImage);

  run('docker', ['image', 'save', '--output', archivePath, ...qualifiedImages]);
  run('sudo', ['ctr', '-n', 'k8s.io', 'images', 'import', archivePath]);

  rmSync(temporaryDirectory, { recursive: true });
  archivePath = undefined;
}

try {
  const context = currentContext();
  const node = validateLocalSingleNode(context);
  const images = localApplicationImages(context);

  console.log(`Preparing ${images.length} local application images for ${node.nodeName} (${node.runtime}) in context ${context}.`);
  prepareDockerImages(images);
  importIntoContainerd(images);
  console.log(`Imported ${images.map(qualifiedImage).join(', ')} into containerd. Kubernetes workloads were not restarted or changed.`);
} catch (error) {
  console.error(`Cannot prepare local Kubernetes images: ${error.message}`);
  if (archivePath && existsSync(archivePath)) {
    console.error(`The image archive was kept at ${archivePath}. After resolving the issue, import it with: sudo ctr -n k8s.io images import ${archivePath}`);
  }
  process.exitCode = 1;
}

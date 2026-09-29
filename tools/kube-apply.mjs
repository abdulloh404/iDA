import { spawn, spawnSync } from 'node:child_process';
import { closeSync, mkdirSync, openSync, readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  databasePortForwardArgs,
  databasePortForwardDirectory,
  databasePortForwards,
  reportBuDatabaseConnections,
  reportDatabasePortForwards,
  runningDatabasePortForward,
} from './kube-db-info.mjs';
import { configuredNamespaces } from './kube-namespaces.mjs';
import { renderKubernetes } from './kube-render.mjs';
import { checkLocalStorage } from './kube-storage.mjs';

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

function waitForForwarding(child, forward, logPath) {
  return new Promise((resolve, reject) => {
    let settled = false;
    const deadline = Date.now() + 75_000;
    const timer = setInterval(() => {
      try {
        if (readFileSync(logPath, 'utf8').includes('Forwarding from ' + forward.address + ':' + forward.port + ' -> ')) {
          finish();
        } else if (Date.now() >= deadline) {
          finish(new Error('Port-forward did not start within 75 seconds. See ' + logPath));
        }
      } catch (error) {
        finish(error);
      }
    }, 100);

    function finish(error) {
      if (settled) return;
      settled = true;
      clearInterval(timer);
      if (error) reject(error);
      else resolve();
    }

    child.once('error', finish);
    child.once('exit', (code, signal) => {
      let details = '';
      try {
        details = readFileSync(logPath, 'utf8').trim();
      } catch {}
      finish(new Error(details || 'kubectl exited with ' + (signal ?? code) + '. See ' + logPath));
    });
  });
}

async function startPortForward(context, stateDirectory, forward) {
  const args = databasePortForwardArgs(context, forward);
  const fileName = forward.namespace + '-' + forward.port;
  const statePath = join(stateDirectory, fileName + '.json');
  const existingPid = runningDatabasePortForward(context, forward, stateDirectory);
  if (existingPid) {
    console.log('Port-forward ' + forward.namespace + ' is already running on ' + forward.address + ':' + forward.port + ' (PID ' + existingPid + ').');
    return;
  }

  const logPath = join(stateDirectory, fileName + '.log');
  const output = openSync(logPath, 'w', 0o600);
  let child;
  try {
    child = spawn('kubectl', args, {
      cwd: workspaceRoot,
      detached: true,
      stdio: ['ignore', output, output],
    });
  } finally {
    closeSync(output);
  }
  child.unref();
  await waitForForwarding(child, forward, logPath);
  writeFileSync(statePath, JSON.stringify({ pid: child.pid }), { mode: 0o600 });
  console.log('Port-forward ' + forward.namespace + ': ' + forward.address + ':' + forward.port + ' -> ' + forward.service + ':' + forward.remotePort + ' (PID ' + child.pid + ').');
}

try {
  const context = currentContext();
  const { user } = checkLocalStorage(context);
  const result = spawnSync('kubectl', ['--context', context, 'apply', '-f', '-'], {
    cwd: workspaceRoot,
    input: renderKubernetes(user),
    stdio: ['pipe', 'inherit', 'inherit'],
  });
  if (result.error || result.status !== 0) {
    throw new Error(result.error?.message || `kubectl apply failed with exit code ${result.status ?? 'unknown'}.`);
  }

  const namespaces = configuredNamespaces();
  const forwards = databasePortForwards({ context, namespaces });
  const stateDirectory = databasePortForwardDirectory(context);
  mkdirSync(stateDirectory, { recursive: true, mode: 0o700 });
  const results = await Promise.allSettled(forwards.map((forward) => startPortForward(context, stateDirectory, forward)));
  for (const [index, forwardResult] of results.entries()) {
    if (forwardResult.status === 'rejected') {
      console.error('Cannot start port-forward for ' + forwards[index].namespace + ': ' + forwardResult.reason.message);
      process.exitCode = 1;
    }
  }
  console.log('Database port-forward logs: ' + stateDirectory);

  try {
    reportBuDatabaseConnections({ context, namespaces });
    reportDatabasePortForwards({ context, forwards });
  } catch (error) {
    console.error(`Kubernetes manifests were applied, but the BU database connection report failed: ${error.message}`);
    process.exitCode = 1;
  }
} catch (error) {
  console.error(`Kubernetes apply/port-forward did not complete: ${error.message} Some resources may already have been applied.`);
  process.exitCode = 1;
}

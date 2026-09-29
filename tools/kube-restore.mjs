import { prepareLocalStorage } from './kube-storage.mjs';

try {
  prepareLocalStorage({ restoreExisting: true });
} catch (error) {
  console.error(`Storage restoration failed: ${error.message} Some Kubernetes resources or storage directories may already exist; no existing data was deleted or replaced.`);
  process.exitCode = 1;
}

import { existsSync, readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { parseJson } from 'nx/src/utils/json';

const settingsFiles = {
  Production: 'appsettings.json',
  Development: 'appsettings.Development.json',
  Local: 'appsettings.local.json',
};

export function resolveApiEnvironment(env = process.env, fallback = 'Production') {
  const value = env.DOTNET_ENVIRONMENT ?? env.ASPNETCORE_ENVIRONMENT ?? fallback;
  const environment = Object.keys(settingsFiles).find((name) => name.toLowerCase() === String(value).toLowerCase());
  if (!environment) throw new Error(`Unsupported API environment "${value}". Use Production, Development, or Local.`);
  return environment;
}

export function readApiSettings(env = process.env) {
  const root = new URL('../apps/api/', import.meta.url);
  const settings = new Map();
  const add = (value, path = '') => {
    if (value !== null && typeof value === 'object') {
      for (const [key, item] of Object.entries(value)) add(item, path ? `${path}:${key}` : key);
    } else if (value !== null && value !== undefined) {
      settings.set(path.toUpperCase(), String(value));
    }
  };
  const environment = resolveApiEnvironment(env);
  const path = new URL(settingsFiles[environment], root);
  if (!existsSync(path)) throw new Error(`Missing API JSON configuration: ${fileURLToPath(path)}`);
  try {
    add(parseJson(readFileSync(path, 'utf8').replace(/^\uFEFF/, '')));
  } catch {
    throw new Error(`Unable to read or parse JSON configuration: ${fileURLToPath(path)}`);
  }
  for (const [key, value] of Object.entries(env)) {
    if (value !== undefined) settings.set(key.replaceAll('__', ':').toUpperCase(), value);
  }
  return settings;
}

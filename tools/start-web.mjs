import { cp } from 'node:fs/promises';

const webRoot = new URL('../apps/web/', import.meta.url);
const standaloneRoot = new URL('.next/standalone/apps/web/', webRoot);

process.env.PORT = process.env.WEB_PORT;

await cp(new URL('public/', webRoot), new URL('public/', standaloneRoot), { recursive: true });
await cp(new URL('.next/static/', webRoot), new URL('.next/static/', standaloneRoot), { recursive: true });
await import(new URL('server.js', standaloneRoot).href);

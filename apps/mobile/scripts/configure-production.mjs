import { writeFile } from 'node:fs/promises';

const rawApiUrl = process.env.GLOWBOOK_API_URL?.trim();
if (!rawApiUrl) throw new Error('GLOWBOOK_API_URL is required for a release build.');

const apiUrl = new URL(rawApiUrl);
if (apiUrl.protocol !== 'https:') throw new Error('GLOWBOOK_API_URL must use HTTPS.');
if (apiUrl.username || apiUrl.password || apiUrl.search || apiUrl.hash)
  throw new Error('GLOWBOOK_API_URL must not include credentials, query parameters, or a fragment.');

const normalizedApiUrl = apiUrl.toString().replace(/\/$/, '');
const output = `export const environment = {\n  production: true,\n  apiUrl: ${JSON.stringify(normalizedApiUrl)},\n};\n`;
await writeFile(new URL('../src/environments/environment.prod.ts', import.meta.url), output, 'utf8');
console.log(`Configured production API origin: ${normalizedApiUrl}`);

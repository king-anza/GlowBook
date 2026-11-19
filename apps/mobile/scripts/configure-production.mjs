import { writeFile } from 'node:fs/promises';

const demoMode = process.env.GLOWBOOK_DEMO_MODE?.trim().toLowerCase() === 'true';
const rawApiUrl = process.env.GLOWBOOK_API_URL?.trim();
if (demoMode && rawApiUrl) throw new Error('Set either GLOWBOOK_DEMO_MODE=true or GLOWBOOK_API_URL, not both.');
if (demoMode) {
  await writeFile(
    new URL('../src/environments/environment.prod.ts', import.meta.url),
    "export const environment = {\n  production: true,\n  apiUrl: '',\n  demoMode: true,\n};\n",
    'utf8',
  );
  console.log('Configured static demonstration mode.');
  process.exit(0);
}
if (!rawApiUrl) throw new Error('GLOWBOOK_API_URL is required unless GLOWBOOK_DEMO_MODE=true.');

const apiUrl = new URL(rawApiUrl);
if (apiUrl.protocol !== 'https:') throw new Error('GLOWBOOK_API_URL must use HTTPS.');
if (apiUrl.username || apiUrl.password || apiUrl.search || apiUrl.hash)
  throw new Error('GLOWBOOK_API_URL must not include credentials, query parameters, or a fragment.');

const normalizedApiUrl = apiUrl.toString().replace(/\/$/, '');
const output = `export const environment = {\n  production: true,\n  apiUrl: ${JSON.stringify(normalizedApiUrl)},\n  demoMode: false,\n};\n`;
await writeFile(new URL('../src/environments/environment.prod.ts', import.meta.url), output, 'utf8');
console.log(`Configured production API origin: ${normalizedApiUrl}`);

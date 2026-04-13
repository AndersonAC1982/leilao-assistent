import { copyFile, access } from 'node:fs/promises';
import { constants } from 'node:fs';
import { resolve } from 'node:path';

const env = (process.argv[2] || '').trim().toLowerCase();
const allowed = new Set(['dev', 'stage', 'prod']);

if (!allowed.has(env)) {
  console.error('Uso: node apps/extension/scripts/set-env.mjs <dev|stage|prod>');
  process.exit(1);
}

const source = resolve('apps/extension/config', `environment.${env}.json`);
const target = resolve('apps/extension/config/environment.json');

try {
  await access(source, constants.R_OK);
  await copyFile(source, target);
  console.log(`Ambiente da extensão definido para: ${env}`);
} catch (error) {
  console.error(`Falha ao configurar ambiente da extensão: ${error?.message || error}`);
  process.exit(1);
}

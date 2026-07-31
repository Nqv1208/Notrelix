import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { checkArchitecture } from './check-frontend-dependencies';
import { checkPackageManifests } from './check-package-manifests';
import { checkFolderBoundaries } from './check-folder-boundaries';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const args = process.argv.slice(2);
const rootArgIndex = args.indexOf('--root');
const rootDir = rootArgIndex !== -1 && args[rootArgIndex + 1]
  ? resolve(args[rootArgIndex + 1])
  : resolve(__dirname, '../../..');

const results = [
  checkPackageManifests(rootDir),
  checkArchitecture(rootDir),
  checkFolderBoundaries(rootDir),
];

const violations = results.flatMap((r) => r.violations);

if (violations.length > 0) {
  for (const v of violations) {
    console.error(v);
  }
  process.exit(1);
} else {
  console.log('✅ All architecture rules passed clean with 0 violations.');
  process.exit(0);
}

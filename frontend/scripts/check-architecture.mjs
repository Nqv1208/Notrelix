import fs from 'fs';
import path from 'path';

const ROOT_DIR = process.cwd();

// Helper to recursively get files
function getFiles(dir, fileList = []) {
  const files = fs.readdirSync(dir);
  for (const file of files) {
    const filePath = path.join(dir, file);
    const stat = fs.statSync(filePath);
    if (stat.isDirectory()) {
      if (['node_modules', '.next', 'out', 'build', 'dist', '.git'].includes(file)) {
        continue;
      }
      getFiles(filePath, fileList);
    } else {
      fileList.push(filePath);
    }
  }
  return fileList;
}

const allFiles = getFiles(ROOT_DIR);
const violations = [];
const ruleCounts = {};

function addViolation(ruleId, file, violatingImport, reason, fix) {
  const relativeFile = path.relative(ROOT_DIR, file).replace(/\\/g, '/');
  violations.push({
    ruleId,
    file: relativeFile,
    violatingImport: violatingImport ? violatingImport.replace(/\\/g, '/') : null,
    reason,
    fix
  });
  ruleCounts[ruleId] = (ruleCounts[ruleId] || 0) + 1;
}

// Extract ts/tsx files
const tsFiles = allFiles.filter(f => f.endsWith('.ts') || f.endsWith('.tsx'));

const importRegex = /(?:import|export)\s+.*?\s+from\s+['"]([^'"]+)['"]/g;
const dynamicImportRegex = /import\(['"]([^'"]+)['"]\)/g;
const requireRegex = /require\(['"]([^'"]+)['"]\)/g;

const businessTerms = ['workspace', 'board', 'card', 'task', 'invoice', 'notification', 'billing', 'governance'];
const allowedUiComponents = ['card.tsx', 'hover-card.tsx'];

// Helper to extract all imports from file content
function getImports(content) {
  const imports = [];
  let match;
  importRegex.lastIndex = 0;
  while ((match = importRegex.exec(content)) !== null) {
    imports.push(match[1]);
  }
  dynamicImportRegex.lastIndex = 0;
  while ((match = dynamicImportRegex.exec(content)) !== null) {
    imports.push(match[1]);
  }
  requireRegex.lastIndex = 0;
  while ((match = requireRegex.exec(content)) !== null) {
    imports.push(match[1]);
  }
  return imports;
}

// Determine if path is a deep feature import (i.e. imports internals of a feature)
function isDeepFeatureImport(importPath) {
  if (!importPath.startsWith('@/features/')) return false;
  const parts = importPath.split('/');
  // parts[0] = '@', parts[1] = 'features', parts[2] = featureName
  // A public API import is exactly "@/features/auth" -> parts.length is 3.
  // Anything longer (e.g. "@/features/auth/hooks/useLogin") is a deep import.
  if (parts.length > 3) {
    // Exception: mock directories or mock services are stubs, but still forbidden on critical paths.
    // However, if it has more than 3 parts, it is a deep import.
    return true;
  }
  return false;
}

// Determine feature name from import path
function getFeatureFromImport(importPath) {
  if (importPath.startsWith('@/features/')) {
    return importPath.split('/')[2];
  }
  return null;
}

// Determine feature name from file path
function getFeatureFromFile(filePath) {
  const relative = path.relative(ROOT_DIR, filePath);
  const parts = relative.split(path.sep);
  if (parts[0] === 'features' && parts[1]) {
    return parts[1];
  }
  return null;
}

// Enforce rule 11: no features/work-management/views/
const wmViewsPath = path.join(ROOT_DIR, 'features', 'work-management', 'views');
if (fs.existsSync(wmViewsPath)) {
  addViolation(
    'ARCH_WM_NO_VIEWS_DIR',
    wmViewsPath,
    null,
    'features/work-management/views/ directory must not exist.',
    'Remove features/work-management/views/ and move view renderers to features/work-management/boards/components/views/'
  );
}

// Enforce rule 10: features/boards compat only
const boardsPath = path.join(ROOT_DIR, 'features', 'boards');
if (fs.existsSync(boardsPath)) {
  const boardFiles = fs.readdirSync(boardsPath);
  for (const file of boardFiles) {
    if (file !== 'index.ts' && !file.endsWith('.md')) {
      const fullPath = path.join(boardsPath, file);
      addViolation(
        'ARCH_BOARDS_COMPAT_ONLY',
        fullPath,
        null,
        'features/boards/ must only contain index.ts compatibility layer and markdown documentation.',
        'Move physical files to features/work-management and reference them via compatibility re-exports.'
      );
    }
  }
}

// Scan each TS/TSX file
for (const file of tsFiles) {
  const content = fs.readFileSync(file, 'utf-8');
  const imports = getImports(content);
  const featureOfFile = getFeatureFromFile(file);
  const relativeFile = path.relative(ROOT_DIR, file).replace(/\\/g, '/');

  // Rule 13: components/ui no business terms
  if (relativeFile.startsWith('components/ui/')) {
    const basename = path.basename(file).toLowerCase();
    if (!allowedUiComponents.includes(basename)) {
      for (const term of businessTerms) {
        if (basename.includes(term)) {
          addViolation(
            'ARCH_UI_NO_BUSINESS_TERMS',
            file,
            null,
            `components/ui/ filenames must be business-agnostic. Found business term "${term}".`,
            'Move the file to a feature-specific components folder or rename it to be generic.'
          );
        }
      }
    }
  }

  // Scan imports
  for (const imp of imports) {
    // Rule 1: lib/ no features/
    if (relativeFile.startsWith('lib/')) {
      if (imp.startsWith('@/features') || imp.includes('/features/')) {
        addViolation(
          'ARCH_LIB_NO_FEATURES',
          file,
          imp,
          'lib/ must not import from features/.',
          'Use dependency injection, callbacks, or move the logic into features/.'
        );
      }
    }

    // Rule 2: components/ui/ no features/
    if (relativeFile.startsWith('components/ui/')) {
      if (imp.startsWith('@/features') || imp.includes('/features/')) {
        addViolation(
          'ARCH_UI_NO_FEATURES',
          file,
          imp,
          'components/ui/ must be business-blind and cannot import from features/.',
          'Remove feature dependencies or move the component into features/.'
        );
      }
    }

    // Rule 3: features/ no app/
    if (featureOfFile) {
      if (imp.startsWith('@/app') || imp.includes('/app/')) {
        addViolation(
          'ARCH_FEATURES_NO_APP',
          file,
          imp,
          'features/ must not import from app/.',
          'Extract shared layout frames or route context into features/workspace/ or components/.'
        );
      }
    }

    // Rule 4 & 5: app/ no deep-feature-imports / only import feature public API
    if (relativeFile.startsWith('app/')) {
      if (isDeepFeatureImport(imp)) {
        const targetFeature = getFeatureFromImport(imp);
        addViolation(
          'ARCH_APP_DEEP_FEATURE_IMPORT',
          file,
          imp,
          'app/ cannot deep-import feature internals.',
          `Import from the feature's public API barrel instead (e.g. "@/features/${targetFeature}").`
        );
      }
    }

    // Rule 6 & 7: Feature A no deep-import internals of Feature B / sibling communication only via public API
    if (featureOfFile) {
      const targetFeature = getFeatureFromImport(imp);
      if (targetFeature && targetFeature !== featureOfFile) {
        if (isDeepFeatureImport(imp)) {
          // Exception: features/boards/index.ts (compat layer) is allowed to import work-management internals
          // Exception: features/workspace/components/workspace-management-panel.tsx is composition root for settings tabs
          if (
            (featureOfFile === 'boards' && relativeFile.endsWith('index.ts') && targetFeature === 'work-management') ||
            (featureOfFile === 'workspace' && relativeFile.endsWith('workspace-management-panel.tsx'))
          ) {
            continue;
          }
          addViolation(
            'ARCH_CROSS_FEATURE_DEEP_IMPORT',
            file,
            imp,
            'Sibling features must only communicate via public API barrels.',
            `Import from "@/features/${targetFeature}" instead of deep internal paths.`
          );
        }
      }
    }

    // Rule 9: features/work-management no features/boards
    if (featureOfFile === 'work-management') {
      if (imp.startsWith('@/features/boards') || imp.includes('/features/boards')) {
        addViolation(
          'ARCH_WM_NO_BOARDS_IMPORT',
          file,
          imp,
          'features/work-management/ must not import from legacy features/boards compatibility layer.',
          'Change import to internal paths or other modules to break the dependency cycle.'
        );
      }
    }
  }

  // Rule 14: Guarded mock imports
  for (const imp of imports) {
    if (imp.includes('/mock/') || imp.includes('mock-data') || imp.includes('sample-data')) {
      const isMockOrTest = relativeFile.includes('/mock/') || relativeFile.includes('.test.') || relativeFile.includes('.spec.');
      if (!isMockOrTest) {
        if (!content.includes('isMockModeEnabled') && !content.includes('isDocsMockModeEnabled')) {
          addViolation(
            'ARCH_UNGUARDED_MOCK_IMPORT',
            file,
            imp,
            'Production files importing mock data must be guarded by isMockModeEnabled check.',
            'Wrap the mock usage with isMockModeEnabled() check or move the mock data behind an adapter.'
          );
        }
      }
    }
  }

  // Rule 15: No raw role checks in UI files (excluding workspace settings and tests)
  if (relativeFile.endsWith('.tsx') && !relativeFile.includes('.test.') && !relativeFile.includes('/mock/')) {
    if (relativeFile !== 'features/workspace/components/workspace-management-panel.tsx') {
      if (content.includes('role ===') || content.includes('role !==')) {
        addViolation(
          'ARCH_NO_RAW_ROLE_CHECK',
          file,
          null,
          'Raw role check found in UI file. Use useCan() or permission guards instead.',
          'Replace raw role comparison with useCan(permission) or <PermissionGuard>.'
        );
      }
    }
  }

  // Rule 16: No raw plan/tier checks in UI files (excluding billing/entitlements and tests)
  if (relativeFile.endsWith('.tsx') && !relativeFile.includes('.test.') && !relativeFile.includes('/mock/') && !relativeFile.includes('features/billing/')) {
    if (content.includes('plan ===') || content.includes('tier ===') || content.includes('subscription.plan')) {
      addViolation(
        'ARCH_NO_RAW_PLAN_CHECK',
        file,
        null,
        'Raw plan or tier check found in UI file. Use useEntitlement() instead.',
        'Replace raw plan comparison with useEntitlement(feature).'
      );
    }
  }

  // Rule 17: No direct fetch() calls outside lib/api/ or tests
  if (!relativeFile.startsWith('lib/api/') && !relativeFile.includes('.test.') && !relativeFile.includes('.spec.')) {
    if (/(?<!\w)fetch\(/.test(content) && !content.includes('window.fetch')) {
      addViolation(
        'ARCH_NO_DIRECT_FETCH',
        file,
        null,
        'Direct fetch() call found outside lib/api/. Use the unified api client instead.',
        'Replace fetch() with api.get, api.post, etc.'
      );
    }
  }

  // Rule 18: No direct response.json() calls outside lib/api/api-client.ts or tests
  if (relativeFile !== 'lib/api/api-client.ts' && !relativeFile.includes('.test.') && !relativeFile.includes('.spec.')) {
    if (content.includes('.json()')) {
      addViolation(
        'ARCH_NO_DIRECT_RESPONSE_JSON',
        file,
        null,
        'Direct response.json() call found. All responses must be parsed safely inside api-client.',
        'Rely on the api-client to parse and return the JSON payload.'
      );
    }
  }

  // Rule 19: Prevent features/theme from reappearing as a business feature
  if (relativeFile.startsWith('features/theme/')) {
    addViolation(
      'ARCH_NO_THEME_IN_FEATURES',
      file,
      null,
      'Theme infrastructure found under features/. Theme must reside in lib/theme or components/theme.',
      'Move theme files to lib/theme or components/theme.'
    );
  }

  // Rule 8: Feature root index.ts must not use export *
  if (relativeFile.match(/^features\/[^/]+\/index\.ts[x]?$/)) {
    if (content.includes('export *')) {
      addViolation(
        'ARCH_NO_EXPORT_STAR',
        file,
        null,
        'Feature root index.ts must not use "export *".',
        'Use explicit named exports to keep the public API boundary clean and clear.'
      );
    }
  }

  // Rule 12: Board view renderers must live under features/work-management/boards/components/views/
  if (featureOfFile === 'work-management' && path.basename(file).endsWith('-view.tsx')) {
    if (!relativeFile.includes('features/work-management/boards/components/views/')) {
      addViolation(
        'ARCH_VIEW_RENDERER_PLACEMENT',
        file,
        null,
        'Board view renderers must live under features/work-management/boards/components/views/.',
        'Move the view component under features/work-management/boards/components/views/.'
      );
    }
  }
}

// Print results in the exact requested format
if (violations.length > 0) {
  for (const v of violations) {
    console.log(`[${v.ruleId}]`);
    console.log(`File: ${v.file}`);
    if (v.violatingImport) {
      console.log(`Import: ${v.violatingImport}`);
    }
    console.log(`Reason: ${v.reason}`);
    console.log(`Fix: ${v.fix}`);
    console.log('');
  }
}

const pass = violations.length === 0;
console.log(`Architecture check: ${pass ? 'PASS' : 'FAIL'}`);
console.log(`Total violations: ${violations.length}`);
console.log('By rule:');
for (const ruleId of Object.keys(ruleCounts)) {
  console.log(`  ${ruleId}: ${ruleCounts[ruleId]}`);
}

process.exit(pass ? 0 : 1);

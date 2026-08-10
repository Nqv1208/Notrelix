#!/usr/bin/env node

/**
 * Product Module Generator
 *
 * Creates a product module under packages/product/<name>/ with only the
 * requested responsibility sub-packages. This generator never creates
 * foundation packages.
 *
 *   core/     always  — pure domain contracts (product-core)
 *   web/      --adapters web|both  — web adapter (product-adapter)
 *   mobile/   --adapters mobile|both  — mobile adapter (product-adapter)
 *   state/    --state  — server-state bridge (product-state)
 *   testing/  --testing  — test fixtures/harness (product-testing)
 *   plugins/  --extension plugins  — plugin surface (product-plugin)
 *   collaboration/  --extension collaboration  — collaboration surface (product-collaboration)
 *
 * Every generated sub-package is registered in the architecture manifest and
 * the package-boundary docs are regenerated afterwards (honoring
 * GENERATOR_ROOT so golden tests never touch the real worktree).
 *
 * Usage:
 *   node index.mjs <product-name> [--adapters web|mobile|both] [--state] [--testing] [--extension none|plugins|collaboration]
 */

import { mkdirSync, writeFileSync, existsSync, symlinkSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import {
  registerManifestEntries,
  refreshArchitectureDocs,
  PRODUCT_SECTION_ANCHOR,
} from "../lib/workspace.mjs";

const __dirname = dirname(fileURLToPath(import.meta.url));
const rootDir = process.env.GENERATOR_ROOT ?? join(__dirname, "../../../..");

function getSampleNodeModules(subPkgName) {
  const targetRel = subPkgName.endsWith("-mobile")
    ? "packages/product/work-management/mobile/node_modules"
    : "packages/features/account/node_modules";

  return existsSync(join(rootDir, targetRel))
    ? join(rootDir, targetRel)
    : existsSync(join(rootDir, "node_modules"))
      ? join(rootDir, "node_modules")
      : null;
}

const args = process.argv.slice(2);
const productName = args.find((a) => !a.startsWith("--"));

function flagValue(flag, fallback) {
  const index = args.indexOf(flag);
  return index !== -1 ? args[index + 1] : fallback;
}

const adapters = flagValue("--adapters", "both");
const withState = args.includes("--state");
const withTesting = args.includes("--testing");
const extension = flagValue("--extension", "none");

if (!productName) {
  console.error(
    "Usage: node index.mjs <product-name> [--adapters web|mobile|both] [--state] [--testing] [--extension none|plugins|collaboration]",
  );
  console.error(
    "Example: node index.mjs analytics --adapters both --state --extension plugins",
  );
  process.exit(1);
}

if (!["web", "mobile", "both"].includes(adapters)) {
  console.error(
    `Invalid --adapters "${adapters}"; expected web, mobile, or both`,
  );
  process.exit(1);
}
if (!["none", "plugins", "collaboration"].includes(extension)) {
  console.error(
    `Invalid --extension "${extension}"; expected none, plugins, or collaboration`,
  );
  process.exit(1);
}

const productDir = join(rootDir, `packages/product/${productName}`);

if (existsSync(productDir)) {
  console.error(`Product "${productName}" already exists at ${productDir}`);
  process.exit(1);
}

console.log(`Creating product module: ${productName}`);
console.log(
  `Adapters: ${adapters}; state: ${withState ? "yes" : "no"}; testing: ${withTesting ? "yes" : "no"}; extension: ${extension}`,
);

function makeTsconfig(extendsRelative) {
  return (
    JSON.stringify(
      {
        extends: extendsRelative,
        compilerOptions: {
          outDir: "./dist",
          rootDir: "./src",
          skipLibCheck: true,
          baseUrl: ".",
          paths: { "~/*": ["./src/*"] },
        },
        include: ["src/**/*"],
        exclude: ["node_modules", "dist"],
      },
      null,
      2,
    ) + "\n"
  );
}

function writeSubPackage(
  subDir,
  subPkgName,
  { tsconfigExtends, eslintConfig, dependencies, devDependencies },
) {
  mkdirSync(join(subDir, "src"), { recursive: true });
  const sampleNodeModules = getSampleNodeModules(subPkgName);
  if (sampleNodeModules) {
    try {
      symlinkSync(sampleNodeModules, join(subDir, "node_modules"), "dir");
    } catch {}
  }
  writeFileSync(join(subDir, "tsconfig.json"), makeTsconfig(tsconfigExtends));
  writeFileSync(
    join(subDir, "package.json"),
    JSON.stringify(
      {
        name: subPkgName,
        version: "0.0.1",
        private: true,
        type: "module",
        main: "./src/index.ts",
        types: "./src/index.ts",
        exports: { ".": "./src/index.ts" },
        scripts: {
          typecheck: "tsc --noEmit",
          lint: "eslint .",
          test: "vitest run",
          clean: "rm -rf node_modules dist",
        },
        dependencies: dependencies ?? {},
        devDependencies: devDependencies ?? {
          typescript: "^5.0.0",
          vitest: "catalog:",
        },
      },
      null,
      2,
    ),
  );
  writeFileSync(
    join(subDir, "eslint.config.js"),
    `import { defineConfig } from "eslint/config";
import baseConfig from "@notrelix/eslint-config";

export default defineConfig([
  {
    ignores: ["dist/**", "node_modules/**", ".turbo/**"],
  },
  ...baseConfig,
]);
`,
  );
  writeFileSync(
    join(subDir, "src/index.ts"),
    `// @notrelix/${subPkgName.replace("@notrelix/", "")}\nexport {};\n`,
  );
}

const corePkg = `@notrelix/${productName}-core`;
const statePkg = `@notrelix/${productName}-state`;
const webPkg = `@notrelix/${productName}-web`;
const mobilePkg = `@notrelix/${productName}-mobile`;
const pluginsPkg = `@notrelix/${productName}-plugins`;
const collaborationPkg = `@notrelix/${productName}-collaboration`;
const testingPkg = `@notrelix/${productName}-testing`;

const entries = [];

// Core (always)
writeSubPackage(join(productDir, "core"), corePkg, {
  tsconfigExtends: "../../../../tooling/tsconfig/base.json",
});
mkdirSync(join(productDir, "core/src/__tests__"), { recursive: true });
writeFileSync(
  join(productDir, "core/src/__tests__/smoke.unit.test.ts"),
  `import { describe, expect, it } from 'vitest';
import * as module from '../index';

describe('@notrelix/${productName}-core', () => {
  it('is importable', () => {
    expect(module).toBeDefined();
  });
});
`,
);
entries.push({
  packageName: corePkg,
  relativePath: `packages/product/${productName}/core`,
  layer: "product-core",
  freezeScope: "core-production",
  allowedInternalImports: ["@notrelix/contracts", "@notrelix/kernel"],
});

// State
if (withState) {
  writeSubPackage(join(productDir, "state"), statePkg, {
    tsconfigExtends: "../../../../tooling/tsconfig/base.json",
    dependencies: {
      [corePkg]: "workspace:*",
      "@notrelix/query": "workspace:*",
    },
  });
  const camelProduct = productName.replace(/-([a-z])/g, (_, c) =>
    c.toUpperCase(),
  );
  mkdirSync(join(productDir, "state/src"), { recursive: true });
  writeFileSync(
    join(productDir, "state/src/index.ts"),
    `import { workspaceQueryKey } from '@notrelix/query';

export const ${camelProduct}QueryKeys = {
  all: (workspaceId: string) => workspaceQueryKey(workspaceId, '${productName}'),
};
`,
  );
  mkdirSync(join(productDir, "state/src/__tests__"), { recursive: true });
  writeFileSync(
    join(productDir, `state/src/__tests__/query-keys.unit.test.ts`),
    `import { describe, expect, it } from 'vitest';
import { ${camelProduct}QueryKeys } from '../index';

describe('@notrelix/${productName}-state query keys', () => {
  it('all uses workspaceQueryKey as root', () => {
    const keys = ${camelProduct}QueryKeys.all('ws-1');
    expect(keys).toBeDefined();
    expect(JSON.stringify(keys)).toContain('ws-1');
    expect(JSON.stringify(keys)).toContain('${productName}');
  });
});
`,
  );
  entries.push({
    packageName: statePkg,
    relativePath: `packages/product/${productName}/state`,
    layer: "product-state",
    freezeScope: "core-production",
    allowedInternalImports: [
      corePkg,
      "@notrelix/contracts",
      "@notrelix/query",
      "@notrelix/platform",
    ],
  });
}

// Adapters
const adaptWeb = adapters === "web" || adapters === "both";
const adaptMobile = adapters === "mobile" || adapters === "both";

if (adaptWeb) {
  const webDeps = { [corePkg]: "workspace:*" };
  if (withState) webDeps[statePkg] = "workspace:*";
  writeSubPackage(join(productDir, "web"), webPkg, {
    tsconfigExtends: "../../../../tooling/tsconfig/react-library.json",
    dependencies: webDeps,
  });
  mkdirSync(join(productDir, "web/src/__tests__"), { recursive: true });
  writeFileSync(
    join(productDir, `web/src/__tests__/smoke.component.test.tsx`),
    `import { describe, expect, it } from 'vitest';
import * as module from '../index';

describe('@notrelix/${productName}-web', () => {
  it('is importable', () => {
    expect(module).toBeDefined();
  });
});
`,
  );
  const webImports = [corePkg, "@notrelix/ui-web", "@notrelix/platform"];
  if (withState) webImports.unshift(statePkg);
  entries.push({
    packageName: webPkg,
    relativePath: `packages/product/${productName}/web`,
    layer: "product-adapter",
    freezeScope: "core-production",
    allowedInternalImports: webImports,
  });
}

if (adaptMobile) {
  const mobileDeps = {
    [corePkg]: "workspace:*",
    "@notrelix/ui-mobile": "workspace:*",
    react: "^19.0.0",
    "react-native": "latest",
  };
  if (withState) mobileDeps[statePkg] = "workspace:*";
  writeSubPackage(join(productDir, "mobile"), mobilePkg, {
    tsconfigExtends: "../../../../tooling/tsconfig/react-library.json",
    dependencies: mobileDeps,
    devDependencies: {
      typescript: "^5.0.0",
      vitest: "catalog:",
      "@types/react-native": "^0.73.0",
    },
  });
  // G4: mobile adapter is a native-safe .tsx value component — no HTML/DOM
  const MobilePascal = productName
    .split("-")
    .map((s) => s.charAt(0).toUpperCase() + s.slice(1))
    .join("");
  const mobileScreenFile = join(
    productDir,
    `mobile/src/${productName}-mobile-screen.tsx`,
  );
  mkdirSync(join(productDir, "mobile/src"), { recursive: true });
  writeFileSync(
    mobileScreenFile,
    `import React from 'react';
import { View, Text, StyleSheet } from 'react-native';

export interface ${MobilePascal}MobileScreenProps {
  workspaceId: string;
}

export function ${MobilePascal}MobileScreen({ workspaceId }: ${MobilePascal}MobileScreenProps) {
  return (
    <View style={styles.container}>
      <Text style={styles.title}>${MobilePascal}</Text>
      <Text style={styles.subtitle}>Workspace: {workspaceId}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, padding: 16 },
  title: { fontSize: 18, fontWeight: 'bold' },
  subtitle: { fontSize: 14, color: '#6b7280', marginTop: 4 },
});
`,
  );
  writeFileSync(
    join(productDir, "mobile/src/index.ts"),
    `export { ${MobilePascal}MobileScreen } from './${productName}-mobile-screen';
export type { ${MobilePascal}MobileScreenProps } from './${productName}-mobile-screen';
`,
  );
  // G6: mobile tests use .mobile.test.tsx taxonomy
  mkdirSync(join(productDir, "mobile/src/__tests__"), { recursive: true });
  writeFileSync(
    join(productDir, `mobile/src/__tests__/smoke.mobile.test.tsx`),
    `import { describe, expect, it } from 'vitest';
import { ${MobilePascal}MobileScreen } from '../index';

describe('@notrelix/${productName}-mobile', () => {
  it('${MobilePascal}MobileScreen is a renderable RN function component', () => {
    expect(typeof ${MobilePascal}MobileScreen).toBe('function');
  });
});
`,
  );
  const mobileImports = [corePkg, "@notrelix/ui-mobile", "@notrelix/platform"];
  if (withState) mobileImports.unshift(statePkg);
  entries.push({
    packageName: mobilePkg,
    relativePath: `packages/product/${productName}/mobile`,
    layer: "product-adapter",
    freezeScope: "core-production",
    allowedInternalImports: mobileImports,
  });
}

// Plugins
if (extension === "plugins") {
  writeSubPackage(join(productDir, "plugins"), pluginsPkg, {
    tsconfigExtends: "../../../../tooling/tsconfig/base.json",
    dependencies: { [corePkg]: "workspace:*" },
  });
  mkdirSync(join(productDir, "plugins/src/__tests__"), { recursive: true });
  writeFileSync(
    join(productDir, `plugins/src/__tests__/smoke.unit.test.ts`),
    `import { describe, expect, it } from 'vitest';
import * as module from '../index';

describe('@notrelix/${productName}-plugins', () => {
  it('is importable', () => {
    expect(module).toBeDefined();
  });
});
`,
  );
  entries.push({
    packageName: pluginsPkg,
    relativePath: `packages/product/${productName}/plugins`,
    layer: "product-plugin",
    freezeScope: "core-production",
    allowedInternalImports: [corePkg],
  });
}

// Collaboration
if (extension === "collaboration") {
  writeSubPackage(join(productDir, "collaboration"), collaborationPkg, {
    tsconfigExtends: "../../../../tooling/tsconfig/base.json",
    dependencies: { [corePkg]: "workspace:*" },
  });
  mkdirSync(join(productDir, "collaboration/src/__tests__"), {
    recursive: true,
  });
  writeFileSync(
    join(productDir, `collaboration/src/__tests__/smoke.unit.test.ts`),
    `import { describe, expect, it } from 'vitest';
import * as module from '../index';

describe('@notrelix/${productName}-collaboration', () => {
  it('is importable', () => {
    expect(module).toBeDefined();
  });
});
`,
  );
  entries.push({
    packageName: collaborationPkg,
    relativePath: `packages/product/${productName}/collaboration`,
    layer: "product-collaboration",
    freezeScope: "core-production",
    allowedInternalImports: [
      corePkg,
      "@notrelix/contracts",
      "@notrelix/platform",
    ],
  });
}

// Testing
if (withTesting) {
  writeSubPackage(join(productDir, "testing"), testingPkg, {
    tsconfigExtends: "../../../../tooling/tsconfig/base.json",
    dependencies: { [corePkg]: "workspace:*" },
  });
  mkdirSync(join(productDir, "testing/src/__tests__"), { recursive: true });
  writeFileSync(
    join(productDir, `testing/src/__tests__/smoke.unit.test.ts`),
    `import { describe, expect, it } from 'vitest';
import * as module from '../index';

describe('@notrelix/${productName}-testing', () => {
  it('is importable', () => {
    expect(module).toBeDefined();
  });
});
`,
  );
  entries.push({
    packageName: testingPkg,
    relativePath: `packages/product/${productName}/testing`,
    layer: "product-testing",
    freezeScope: "verification",
    allowedInternalImports: [corePkg],
  });
}

// Architecture manifest registration + docs refresh
const registered = registerManifestEntries(
  rootDir,
  entries,
  PRODUCT_SECTION_ANCHOR,
);
if (registered) {
  console.log(
    `Registered ${entries.length} package(s) in the architecture manifest`,
  );
  const docsRefreshed = refreshArchitectureDocs(rootDir);
  if (docsRefreshed) console.log("Refreshed generated package-boundary docs");
} else {
  console.log(
    "Skipped manifest registration: no architecture manifest in this workspace",
  );
}

console.log(`\nCreated product module at: ${productDir}`);
console.log(`Sub-packages: ${entries.map((e) => e.packageName).join(", ")}`);

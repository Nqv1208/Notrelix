#!/usr/bin/env node

/**
 * Feature Package Generator
 *
 * Creates a new feature package with the standard structure:
 *   packages/features/<name>/
 *     src/core/{api,query,mutations,model,schemas,permissions}
 *     src/web/      (--ui web|both)
 *     src/mobile/   (--ui mobile|both)
 *     src/testing/
 *
 * Default capabilities: kernel, contracts, platform, query.
 * Flags:
 *   --ui <web|mobile|both>   generate UI subfolders for the requested targets
 *   --realtime               allow imports of @notrelix/realtime
 *
 * Every generated package is registered in the architecture manifest and the
 * package-boundary docs are regenerated afterwards (honoring GENERATOR_ROOT
 * so golden tests never touch the real worktree).
 *
 * Usage: node index.mjs <feature-name> [--ui web|mobile|both] [--realtime]
 */

import { mkdirSync, writeFileSync, existsSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import {
  registerManifestEntries,
  refreshArchitectureDocs,
  FEATURES_SECTION_ANCHOR,
} from "../lib/workspace.mjs";

const __dirname = dirname(fileURLToPath(import.meta.url));
const rootDir = process.env.GENERATOR_ROOT ?? join(__dirname, "../../../..");

const args = process.argv.slice(2);
const featureName = args.find((a) => !a.startsWith("--"));
const uiFlagIndex = args.indexOf("--ui");
const uiTarget = uiFlagIndex !== -1 ? args[uiFlagIndex + 1] : undefined;
const realtime = args.includes("--realtime");

if (!featureName) {
  console.error(
    "Usage: node index.mjs <feature-name> [--ui web|mobile|both] [--realtime]",
  );
  console.error("Example: node index.mjs billing --ui both --realtime");
  process.exit(1);
}

if (uiTarget !== undefined && !["web", "mobile", "both"].includes(uiTarget)) {
  console.error(
    `Invalid --ui target "${uiTarget}"; expected web, mobile, or both`,
  );
  process.exit(1);
}

const uiWeb = uiTarget === "web" || uiTarget === "both";
const uiMobile = uiTarget === "mobile" || uiTarget === "both";

const PascalName = featureName
  .split("-")
  .map((segment) => segment.charAt(0).toUpperCase() + segment.slice(1))
  .join("");
const camelName = PascalName.charAt(0).toLowerCase() + PascalName.slice(1);

const featureDir = join(rootDir, `packages/features/${featureName}`);
const pkgName = `@notrelix/features-${featureName}`;

if (existsSync(featureDir)) {
  console.error(`Feature "${featureName}" already exists at ${featureDir}`);
  process.exit(1);
}

console.log(`Creating feature: ${featureName}`);
console.log(`Package: ${pkgName}`);
console.log(
  `Capabilities: kernel, contracts, platform, query${realtime ? ", realtime" : ""}`,
);
console.log(
  `UI targets: ${uiWeb ? "web" : ""}${uiWeb && uiMobile ? " + " : ""}${uiMobile ? "mobile" : "none"}`,
);

// Create directories
mkdirSync(join(featureDir, "src/core/api"), { recursive: true });
mkdirSync(join(featureDir, "src/core/query"), { recursive: true });
mkdirSync(join(featureDir, "src/core/mutations"), { recursive: true });
mkdirSync(join(featureDir, "src/core/model"), { recursive: true });
mkdirSync(join(featureDir, "src/core/schemas"), { recursive: true });
mkdirSync(join(featureDir, "src/core/permissions"), { recursive: true });
mkdirSync(join(featureDir, "src/testing"), { recursive: true });
if (uiWeb) {
  mkdirSync(join(featureDir, "src/web/screens"), { recursive: true });
  mkdirSync(join(featureDir, "src/web/components"), { recursive: true });
}
if (uiMobile) {
  mkdirSync(join(featureDir, "src/mobile/screens"), { recursive: true });
  mkdirSync(join(featureDir, "src/mobile/components"), { recursive: true });
}

const exportsMap = {
  ".": "./src/index.ts",
  "./core": "./src/core/index.ts",
  "./core/query/keys": "./src/core/query/keys.ts",
};
if (uiWeb) exportsMap["./web"] = "./src/web/index.ts";
if (uiMobile) exportsMap["./mobile"] = "./src/mobile/index.ts";

// Create package.json
writeFileSync(
  join(featureDir, "package.json"),
  JSON.stringify(
    {
      name: pkgName,
      version: "0.0.1",
      private: true,
      type: "module",
      main: "./src/index.ts",
      types: "./src/index.ts",
      exports: exportsMap,
      scripts: {
        typecheck: "tsc --noEmit",
        test: "vitest run",
        clean: "rm -rf node_modules dist",
      },
      devDependencies: {
        typescript: "^5.0.0",
        vitest: "catalog:",
      },
    },
    null,
    2,
  ),
);

// Create tsconfig.json
writeFileSync(
  join(featureDir, "tsconfig.json"),
  JSON.stringify(
    {
      extends: "../../../tooling/tsconfig/react-library.json",
      compilerOptions: {
        outDir: "./dist",
        rootDir: "./src",
        baseUrl: ".",
        paths: { "~/*": ["./src/*"] },
      },
      include: ["src/**/*"],
      exclude: ["node_modules", "dist"],
    },
    null,
    2,
  ),
);

// Create eslint.config.js (web-scoped feature: web boundary rules)
writeFileSync(
  join(featureDir, "eslint.config.js"),
  `import { defineConfig } from "eslint/config";
import webConfig from "@notrelix/eslint-config/web";

export default defineConfig([
  {
    ignores: ["dist/**", "node_modules/**", ".turbo/**"],
  },
  ...webConfig,
]);
`,
);

// Create index.ts
writeFileSync(
  join(featureDir, "src/index.ts"),
  `/**
 * @notrelix/features-${featureName} — ${featureName} feature package.
 */

// Core
export type {} from './core';

// Web
// export {} from './web';

// Mobile
// export {} from './mobile';
`,
);

// Create core index.ts
writeFileSync(
  join(featureDir, "src/core/index.ts"),
  `/**
 * @notrelix/features-${featureName}/core — Core types and API contracts.
 */

// Types
// export type {} from './model/${featureName}.types';

// API
// export { create${featureName.charAt(0).toUpperCase() + featureName.slice(1)}Api } from './api/${featureName}.api';
`,
);

// Create query keys
writeFileSync(
  join(featureDir, "src/core/query/keys.ts"),
  `/**
 * @notrelix/features-${featureName}/core/query — Query keys.
 *
 * Query key contract (05-FOUNDATION-PACKAGES-SPEC):
 * workspace-scoped keys are \`['workspace', workspaceId, <feature>, ...]\`.
 */

export const ${camelName}QueryKeys = {
  all: ['${featureName}'] as const,
  // detail: (id: string) => ['${featureName}', 'detail', id] as const,
} as const;
`,
);

// Create query keys test skeleton
writeFileSync(
  join(featureDir, "src/core/query/keys.test.ts"),
  `import { describe, expect, it } from 'vitest';
import { ${camelName}QueryKeys } from './keys';

describe('${camelName}QueryKeys', () => {
  it('exposes a stable all-keys entry', () => {
    expect(${camelName}QueryKeys.all).toEqual(['${featureName}']);
  });
});
`,
);

// Create web index.ts
if (uiWeb) {
  writeFileSync(
    join(featureDir, "src/web/index.ts"),
    `/**
 * @notrelix/features-${featureName}/web — Web components and hooks.
 */
`,
  );
}

// Create mobile index.ts
if (uiMobile) {
  writeFileSync(
    join(featureDir, "src/mobile/index.ts"),
    `/**
 * @notrelix/features-${featureName}/mobile — Mobile components and screens.
 */
`,
  );
}

// Architecture manifest registration + docs refresh
const entry = {
  packageName: pkgName,
  relativePath: `packages/features/${featureName}`,
  layer: "feature",
  freezeScope: "core-production",
  allowedInternalImports: realtime
    ? "['@notrelix/contracts', '@notrelix/kernel', '@notrelix/platform', '@notrelix/query', '@notrelix/ui-web', '@notrelix/realtime']"
    : "['@notrelix/contracts', '@notrelix/kernel', '@notrelix/platform', '@notrelix/query', '@notrelix/ui-web']",
};

const registered = registerManifestEntries(
  rootDir,
  [entry],
  FEATURES_SECTION_ANCHOR,
);
if (registered) {
  console.log(`Registered ${pkgName} in the architecture manifest`);
  const docsRefreshed = refreshArchitectureDocs(rootDir);
  if (docsRefreshed) console.log("Refreshed generated package-boundary docs");
} else {
  console.log(
    "Skipped manifest registration: no architecture manifest in this workspace",
  );
}

console.log(`\nCreated feature package at: ${featureDir}`);
console.log("\nNext steps:");
console.log(`1. Add dependencies to ${featureDir}/package.json`);
console.log(`2. Implement types in src/core/model/`);
console.log(`3. Implement API in src/core/api/`);
console.log(`4. Run pnpm --filter ${pkgName} typecheck test`);

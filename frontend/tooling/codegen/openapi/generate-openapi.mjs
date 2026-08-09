#!/usr/bin/env node

/**
 * OpenAPI Code Generator
 *
 * Generates TypeScript types from the committed backend OpenAPI spec
 * using openapi-typescript.
 *
 * Input:  backend/contracts/openapi/notrelix.v1.json
 * Output: packages/foundation/contracts/src/generated/rest/schema.ts
 *
 * Usage: node openapi/generate-openapi.mjs
 * Override spec path: NOTRELIX_OPENAPI_SPEC=/path/to/spec.json
 */

import { readFileSync, writeFileSync, mkdirSync, existsSync } from "fs";
import { join, dirname } from "path";
import { fileURLToPath } from "url";
import { execSync } from "child_process";

const __dirname = dirname(fileURLToPath(import.meta.url));
const rootDir = join(__dirname, "../../..");
const repoRoot = join(rootDir, "..");

const specPath =
  process.env.NOTRELIX_OPENAPI_SPEC ||
  join(repoRoot, "backend/contracts/openapi/notrelix.v1.json");

const outputDir = join(
  rootDir,
  "packages/foundation/contracts/src/generated/rest",
);
const outputFile = join(outputDir, "schema.ts");

console.log("OpenAPI Code Generator");
console.log("======================");
console.log(`Spec: ${specPath}`);
console.log(`Output: ${outputFile}`);

if (!existsSync(specPath)) {
  console.error(`\nERROR: OpenAPI spec not found at ${specPath}`);
  console.error("Run the backend export first:");
  console.error(
    "  cd backend && dotnet run --project src/Notrelix.API -- --export-openapi contracts/openapi/notrelix.v1.json",
  );
  process.exit(1);
}

const spec = JSON.parse(readFileSync(specPath, "utf-8"));
console.log(
  `\nParsed spec: ${spec.info?.title || "Unknown"} v${spec.info?.version || "?"}`,
);

mkdirSync(outputDir, { recursive: true });

try {
  execSync(`npx openapi-typescript "${specPath}" -o "${outputFile}"`, {
    cwd: join(__dirname, ".."),
    stdio: "inherit",
  });
  console.log("\nGenerated schema.ts");
} catch {
  console.error("\nERROR: openapi-typescript failed. Ensure it is installed:");
  console.error("  pnpm add -D openapi-typescript --filter @notrelix/codegen");
  process.exit(1);
}

const indexContent = `/**
 * Auto-generated barrel export.
 * DO NOT EDIT MANUALLY.
 */

export type * from './schema';
`;

writeFileSync(join(outputDir, "index.ts"), indexContent);
console.log("Generated index.ts");

console.log("\nDone!");

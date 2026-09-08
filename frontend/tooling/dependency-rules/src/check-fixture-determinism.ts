import { existsSync, readFileSync, readdirSync, statSync } from "node:fs";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const DEFAULT_ROOT = resolve(__dirname, "../../..");

const FORBIDDEN_PATTERNS: ReadonlyArray<[RegExp, string]> = [
  [/\bMath\s*\.\s*random\b/, "Math.random"],
  [/\bcrypto\s*\.\s*randomUUID\b/, "crypto.randomUUID"],
  [/\bDate\s*\.\s*now\s*\(\s*\)/, "Date.now()"],
  [/\bnew\s+Date\s*\(\s*\)/, "argument-less new Date()"],
];

const SCAN_ROOTS = [
  "packages/product/work-management/testing/src/fixtures",
  "packages/product/work-management/testing/src/scenarios",
  "packages/product/work-management/testing/src/controllers",
  "packages/product/work-management/testing/src/support",
];

const SCAN_DIR_NAMES = new Set(["verification"]);

export interface FixtureDeterminismViolation {
  readonly message: string;
}

export interface FixtureDeterminismResult {
  readonly ok: boolean;
  readonly scannedFiles: number;
  readonly violations: FixtureDeterminismViolation[];
}

function walk(root: string, directories: string[]): void {
  if (!existsSync(root)) return;
  for (const entry of readdirSync(root, { withFileTypes: true })) {
    if (!entry.isDirectory()) continue;
    if (
      entry.name.startsWith(".") ||
      entry.name === "node_modules" ||
      entry.name === "dist"
    )
      continue;
    const full = join(root, entry.name);
    directories.push(full);
    walk(full, directories);
  }
}

function isFixtureSource(rootDir: string, filePath: string): boolean {
  const rel = relative(rootDir, filePath);
  if (SCAN_ROOTS.some((scanRoot) => rel.startsWith(scanRoot))) return true;
  const segments = rel.split("/");
  const verificationIndex = segments.indexOf("verification");
  return (
    (verificationIndex !== -1 &&
      segments[0] === "packages" &&
      segments[1] !== "ui") ||
    (verificationIndex !== -1 &&
      segments.slice(0, 2).join("/") === "packages/ui/web")
  );
}

function collectSourceFiles(rootDir: string): string[] {
  const directories: string[] = [rootDir];
  walk(rootDir, directories);
  const files: string[] = [];
  for (const directory of directories) {
    for (const entry of readdirSync(directory, { withFileTypes: true })) {
      if (!entry.isFile()) continue;
      if (!/\.(ts|tsx)$/.test(entry.name)) continue;
      if (entry.name.endsWith(".unit.test.ts")) continue;
      if (entry.name.endsWith(".component.test.tsx")) continue;
      if (entry.name.endsWith(".interaction.component.test.tsx")) continue;
      const fullPath = join(directory, entry.name);
      if (isFixtureSource(rootDir, fullPath)) files.push(fullPath);
    }
  }
  return files;
}

export function checkFixtureDeterminism(
  rootDir: string = DEFAULT_ROOT,
): FixtureDeterminismResult {
  const violations: FixtureDeterminismViolation[] = [];
  const files = collectSourceFiles(rootDir);
  for (const filePath of files) {
    if (!existsSync(filePath)) continue;
    if (!statSync(filePath).isFile()) continue;
    const sourceText = readFileSync(filePath, "utf8");
    for (const [pattern, name] of FORBIDDEN_PATTERNS) {
      if (pattern.test(sourceText)) {
        violations.push({
          message: `${relative(rootDir, filePath)} uses forbidden nondeterministic ${name} in fixture/scenario/controller authority`,
        });
      }
    }
  }
  return {
    ok: violations.length === 0,
    scannedFiles: files.length,
    violations,
  };
}

if (process.argv[1]?.endsWith("check-fixture-determinism.ts")) {
  const result = checkFixtureDeterminism(resolve(process.cwd()));
  if (!result.ok) {
    for (const violation of result.violations) {
      console.error(`[UI_FIXTURES_NONDETERMINISTIC] ${violation.message}`);
    }
    process.exitCode = 1;
  } else {
    console.log(
      `Fixture determinism check valid: ${result.scannedFiles} fixture/scenario/controller files.`,
    );
  }
}

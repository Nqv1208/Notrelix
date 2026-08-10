import ts from "typescript";
import { existsSync, readFileSync, readdirSync, statSync } from "node:fs";
import { join, resolve } from "node:path";

const EXECUTABLE_SOURCE_EXTENSIONS = [".ts", ".tsx", ".js", ".mjs"];
const IGNORED_DIRECTORIES = new Set([
  "node_modules",
  "dist",
  ".next",
  ".turbo",
  "coverage",
]);

function stringProperty(objectLiteral, propertyName) {
  for (const property of objectLiteral.properties) {
    if (!ts.isPropertyAssignment(property)) continue;
    const name = property.name;
    if (!ts.isIdentifier(name) || name.text !== propertyName) continue;
    const initializer = property.initializer;
    if (ts.isStringLiteral(initializer)) return initializer.text;
  }
  return undefined;
}

export function readArchitectureManifest(rootDir = process.cwd()) {
  const manifestPath = join(
    rootDir,
    "tooling/dependency-rules/src/architecture-manifest.ts",
  );
  const sourceText = readFileSync(manifestPath, "utf8");
  const sourceFile = ts.createSourceFile(
    manifestPath,
    sourceText,
    ts.ScriptTarget.Latest,
    true,
  );

  const entries = [];

  function unwrapExpression(expression) {
    let current = expression;
    while (
      current &&
      (ts.isAsExpression(current) || ts.isSatisfiesExpression(current))
    ) {
      current = current.expression;
    }
    return current;
  }

  function visit(node) {
    if (
      ts.isVariableDeclaration(node) &&
      node.name.getText() === "ARCHITECTURE_MANIFEST"
    ) {
      const initializer = node.initializer
        ? unwrapExpression(node.initializer)
        : undefined;
      if (!initializer || !ts.isArrayLiteralExpression(initializer)) return;

      for (const element of initializer.elements) {
        if (!ts.isObjectLiteralExpression(element)) continue;
        const packageName = stringProperty(element, "packageName");
        const relativePath = stringProperty(element, "relativePath");
        const freezeScope = stringProperty(element, "freezeScope");
        if (!packageName || !relativePath || !freezeScope) continue;
        entries.push({ packageName, relativePath, freezeScope });
      }
    }

    ts.forEachChild(node, visit);
  }

  visit(sourceFile);
  return entries;
}

function hasExecutableSourceFile(directory) {
  let entries = [];
  try {
    entries = readdirSync(directory);
  } catch {
    return false;
  }

  for (const entry of entries) {
    if (IGNORED_DIRECTORIES.has(entry) || entry.startsWith(".")) continue;

    const fullPath = join(directory, entry);
    let stat;
    try {
      stat = statSync(fullPath);
    } catch {
      continue;
    }

    if (stat.isDirectory()) {
      if (hasExecutableSourceFile(fullPath)) return true;
      continue;
    }

    if (
      EXECUTABLE_SOURCE_EXTENSIONS.some((extension) =>
        entry.endsWith(extension),
      )
    ) {
      return true;
    }
  }

  return false;
}

function hasExecutableSource(packageDir) {
  return ["src", "app"].some((sourceDir) =>
    hasExecutableSourceFile(join(packageDir, sourceDir)),
  );
}

export function checkLintCoverage(
  rootDir = process.cwd(),
  manifest = readArchitectureManifest(rootDir),
) {
  const root = resolve(rootDir);
  const missing = [];
  let checked = 0;

  for (const entry of manifest) {
    const packageDir = join(root, entry.relativePath);
    const packageJsonPath = join(packageDir, "package.json");
    if (!existsSync(packageJsonPath) || !hasExecutableSource(packageDir)) {
      continue;
    }

    checked += 1;

    let packageJson;
    try {
      packageJson = JSON.parse(readFileSync(packageJsonPath, "utf8"));
    } catch {
      missing.push({
        name: entry.packageName,
        path: entry.relativePath,
        reason: "package.json is unreadable",
      });
      continue;
    }

    const lintScript = packageJson.scripts?.lint;
    if (
      !lintScript ||
      typeof lintScript !== "string" ||
      lintScript.trim().length === 0
    ) {
      missing.push({
        name: entry.packageName,
        path: entry.relativePath,
        reason: "scripts.lint is missing or empty",
      });
    }
  }

  return {
    ok: missing.length === 0,
    checked,
    missing,
  };
}

const invokedPath = process.argv[1] ? resolve(process.argv[1]) : "";
const currentModulePath = resolve(new URL(import.meta.url).pathname);

if (invokedPath === currentModulePath) {
  const result = checkLintCoverage(process.cwd());

  if (!result.ok) {
    console.error(
      "Lint coverage violation. Every source-bearing manifest package must declare a non-empty `scripts.lint`:",
    );
    for (const item of result.missing) {
      console.error(`  - ${item.name} (${item.path}): ${item.reason}`);
    }
    process.exit(1);
  }

  console.log(`Lint coverage OK: ${result.checked} manifest packages checked.`);
}

import ts from "typescript";
import { existsSync, readFileSync, readdirSync, statSync } from "node:fs";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { ARCHITECTURE_MANIFEST } from "./architecture-manifest";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const DEFAULT_ROOT = resolve(__dirname, "../../..");
const MANIFEST_FILE_NAME = "ui-evidence.manifest.json";

const FORBIDDEN_PACKAGE_IMPORTS = [
  "@tanstack/react-query",
  "@notrelix/contracts",
  "@notrelix/runtime-web",
  "@notrelix/runtime-mobile",
  "@notrelix/dev-mock-backend",
  "@notrelix/features-auth",
] as const;

const FORBIDDEN_SOURCE_PATTERNS: ReadonlyArray<[RegExp, string]> = [
  [/\bfetch\s*\(/, "fetch"],
  [/\bXMLHttpRequest\b/, "XMLHttpRequest"],
  [/\bWebSocket\b/, "WebSocket"],
  [/\blocalStorage\b/, "localStorage"],
  [/\bsessionStorage\b/, "sessionStorage"],
  [/\bindexedDB\b/, "indexedDB"],
  [/\bnew\s+QueryClient\s*\(/, "QueryClient"],
  [/\bcreateNotrelixClient\s*\(/, "createNotrelixClient"],
  [/\bcreateFileRoute\s*\(/, "createFileRoute"],
  [/\bcreateRootRoute\s*\(/, "createRootRoute"],
  [/\buseNavigate\s*\(/, "useNavigate"],
  [/\buseRouter\s*\(/, "useRouter"],
] as const;

export interface UiPurityViolation {
  readonly code:
    | "INVALID_MANIFEST"
    | "MISSING_ENTRY"
    | "FORBIDDEN_IMPORT"
    | "FORBIDDEN_SOURCE"
    | "UNRESOLVED_IMPORT";
  readonly message: string;
  readonly chain: string[];
}

export interface UiPurityResult {
  readonly ok: boolean;
  readonly checkedEntries: number;
  readonly violations: UiPurityViolation[];
}

interface ManifestSurface {
  readonly surfaceId?: unknown;
  readonly pureEntry?: unknown;
  readonly checks?: unknown;
}

interface ManifestFile {
  readonly surfaces?: unknown;
}

interface ImportEdge {
  readonly specifier: string;
  readonly names?: readonly string[];
}

function toRelative(rootDir: string, filePath: string): string {
  return relative(rootDir, filePath).replace(/\\/g, "/");
}

function readJson(filePath: string): unknown {
  return JSON.parse(readFileSync(filePath, "utf8"));
}

function walkDirectories(root: string): string[] {
  if (!existsSync(root)) return [];
  const directories = [root];
  for (const entry of readdirSync(root, { withFileTypes: true })) {
    if (!entry.isDirectory()) continue;
    if (
      entry.name.startsWith(".") ||
      entry.name === "node_modules" ||
      entry.name === "dist"
    )
      continue;
    directories.push(...walkDirectories(join(root, entry.name)));
  }
  return directories;
}

function findManifestPaths(rootDir: string): string[] {
  return [
    join(rootDir, "packages/ui/web"),
    join(rootDir, "packages/product"),
    join(rootDir, "packages/features"),
  ].flatMap((root) =>
    walkDirectories(root)
      .map((directory) => join(directory, "verification", MANIFEST_FILE_NAME))
      .filter((manifestPath) => existsSync(manifestPath)),
  );
}

function fileExists(filePath: string): boolean {
  try {
    return statSync(filePath).isFile();
  } catch {
    return false;
  }
}

function resolveAsFileOrIndex(basePath: string): string | undefined {
  const candidates = [
    basePath,
    `${basePath}.ts`,
    `${basePath}.tsx`,
    `${basePath}.mts`,
    `${basePath}.cts`,
    join(basePath, "index.ts"),
    join(basePath, "index.tsx"),
  ];
  return candidates.find(fileExists);
}

function resolveWorkspaceImport(
  rootDir: string,
  specifier: string,
): { readonly filePath: string; readonly packageName: string } | undefined {
  const owner = ARCHITECTURE_MANIFEST.find(
    (entry) =>
      specifier === entry.packageName ||
      specifier.startsWith(`${entry.packageName}/`),
  );
  if (!owner) return undefined;

  const packageRoot = join(rootDir, owner.relativePath);
  if (specifier === owner.packageName) {
    const filePath = resolveAsFileOrIndex(join(packageRoot, "src"));
    return filePath ? { filePath, packageName: owner.packageName } : undefined;
  }
  const subpath = specifier.slice(owner.packageName.length + 1);
  const filePath = resolveAsFileOrIndex(join(packageRoot, subpath));
  return filePath ? { filePath, packageName: owner.packageName } : undefined;
}

function resolveImport(
  rootDir: string,
  importerPath: string,
  edge: ImportEdge,
): { readonly filePaths: string[]; readonly packageName?: string } {
  const { specifier } = edge;
  if (specifier.startsWith(".")) {
    const filePath = resolveAsFileOrIndex(resolve(dirname(importerPath), specifier));
    return { filePaths: filePath ? [filePath] : [] };
  }
  if (specifier.startsWith("@notrelix/")) {
    const resolvedImport = resolveWorkspaceImport(rootDir, specifier);
    if (!resolvedImport) return { filePaths: [] };
    return {
      filePaths:
        edge.names && resolvedImport.filePath.endsWith("/index.ts")
          ? resolveNamedBarrelExports(resolvedImport.filePath, edge.names)
          : [resolvedImport.filePath],
      packageName: resolvedImport.packageName,
    };
  }
  return { filePaths: [] };
}

function collectNamedBindings(clause: ts.ImportClause): string[] | undefined {
  if (!clause.namedBindings || !ts.isNamedImports(clause.namedBindings)) return undefined;
  return clause.namedBindings.elements.map((element) => element.propertyName?.text ?? element.name.text);
}

function collectExportNames(clause: ts.NamedExportBindings): string[] | undefined {
  if (!ts.isNamedExports(clause)) return undefined;
  return clause.elements.map((element) => element.propertyName?.text ?? element.name.text);
}

function collectImports(sourceFile: ts.SourceFile): ImportEdge[] {
  const imports: ImportEdge[] = [];
  for (const statement of sourceFile.statements) {
    if (ts.isImportDeclaration(statement) && ts.isStringLiteral(statement.moduleSpecifier)) {
      imports.push({
        specifier: statement.moduleSpecifier.text,
        names: statement.importClause ? collectNamedBindings(statement.importClause) : undefined,
      });
    }
    if (ts.isExportDeclaration(statement) && statement.moduleSpecifier && ts.isStringLiteral(statement.moduleSpecifier)) {
      imports.push({
        specifier: statement.moduleSpecifier.text,
        names: statement.exportClause ? collectExportNames(statement.exportClause) : undefined,
      });
    }
  }
  return imports;
}

function resolveNamedBarrelExports(
  barrelPath: string,
  importedNames: readonly string[],
): string[] {
  const sourceText = readFileSync(barrelPath, "utf8");
  const sourceFile = ts.createSourceFile(
    barrelPath,
    sourceText,
    ts.ScriptTarget.Latest,
    false,
    ts.ScriptKind.TS,
  );
  const matches: string[] = [];
  for (const statement of sourceFile.statements) {
    if (!ts.isExportDeclaration(statement) || !statement.moduleSpecifier || !ts.isStringLiteral(statement.moduleSpecifier)) {
      continue;
    }

    const exportedNames = statement.exportClause
      ? collectExportNames(statement.exportClause)
      : undefined;
    if (exportedNames && !exportedNames.some((name) => importedNames.includes(name))) continue;

    const resolved = resolveAsFileOrIndex(resolve(dirname(barrelPath), statement.moduleSpecifier.text));
    if (resolved) matches.push(resolved);
  }
  return [...new Set(matches)];
}

function isForbiddenImport(specifier: string): string | undefined {
  const literalForbidden = FORBIDDEN_PACKAGE_IMPORTS.find(
    (forbidden) =>
      specifier === forbidden || specifier.startsWith(`${forbidden}/`),
  );
  if (literalForbidden) return literalForbidden;

  const internalOwner = ARCHITECTURE_MANIFEST.find(
    (entry) =>
      specifier === entry.packageName ||
      specifier.startsWith(`${entry.packageName}/`),
  );
  if (!internalOwner) return undefined;

  if (
    internalOwner.layer === "product-state" ||
    internalOwner.layer === "runtime" ||
    internalOwner.layer === "dev-support" ||
    internalOwner.layer === "app"
  ) {
    return internalOwner.packageName;
  }

  return undefined;
}

function checkSourcePurity(
  rootDir: string,
  filePath: string,
  chain: string[],
  visited: Set<string>,
  violations: UiPurityViolation[],
) {
  const resolved = resolve(filePath);
  if (visited.has(resolved)) return;
  visited.add(resolved);

  if (!fileExists(resolved)) {
    violations.push({
      code: "MISSING_ENTRY",
      message: `missing pure UI source file: ${toRelative(rootDir, resolved)}`,
      chain,
    });
    return;
  }

  const sourceText = readFileSync(resolved, "utf8");
  for (const [pattern, name] of FORBIDDEN_SOURCE_PATTERNS) {
    if (pattern.test(sourceText)) {
      violations.push({
        code: "FORBIDDEN_SOURCE",
        message: `${toRelative(rootDir, resolved)} uses forbidden pure UI primitive ${name}`,
        chain,
      });
    }
  }

  const sourceFile = ts.createSourceFile(
    resolved,
    sourceText,
    ts.ScriptTarget.Latest,
    false,
    ts.ScriptKind.TSX,
  );
  for (const edge of collectImports(sourceFile)) {
    const { specifier } = edge;
    const forbiddenImport = isForbiddenImport(specifier);
    if (forbiddenImport) {
      violations.push({
        code: "FORBIDDEN_IMPORT",
        message: `${toRelative(rootDir, resolved)} imports forbidden pure UI dependency ${forbiddenImport}`,
        chain: [...chain, specifier],
      });
      continue;
    }

    const imported = resolveImport(rootDir, resolved, edge);
    if (imported.filePaths.length === 0) continue;
    for (const importedFile of imported.filePaths) {
      if (!existsSync(importedFile)) {
        violations.push({
          code: "UNRESOLVED_IMPORT",
          message: `${toRelative(rootDir, resolved)} imports unresolved source ${specifier}`,
          chain: [...chain, specifier],
        });
        continue;
      }

      checkSourcePurity(
        rootDir,
        importedFile,
        [...chain, toRelative(rootDir, importedFile)],
        visited,
        violations,
      );
    }
    if (imported.packageName && imported.filePaths.length === 0) {
      violations.push({
        code: "UNRESOLVED_IMPORT",
        message: `${toRelative(rootDir, resolved)} imports unresolved source ${specifier}`,
        chain: [...chain, specifier],
      });
    }
  }
}

function surfacePureEntries(
  rootDir: string,
  manifestPath: string,
  violations: UiPurityViolation[],
): string[] {
  let manifest: ManifestFile;
  try {
    manifest = readJson(manifestPath) as ManifestFile;
  } catch (error) {
    violations.push({
      code: "INVALID_MANIFEST",
      message: `${toRelative(rootDir, manifestPath)} is not valid JSON: ${(error as Error).message}`,
      chain: [toRelative(rootDir, manifestPath)],
    });
    return [];
  }

  if (!Array.isArray(manifest.surfaces)) {
    violations.push({
      code: "INVALID_MANIFEST",
      message: `${toRelative(rootDir, manifestPath)} must contain surfaces[]`,
      chain: [toRelative(rootDir, manifestPath)],
    });
    return [];
  }

  const ownerRoot = dirname(dirname(manifestPath));
  const entries: string[] = [];
  for (const surface of manifest.surfaces as ManifestSurface[]) {
    if (!Array.isArray(surface.checks) || !surface.checks.includes("purity"))
      continue;
    if (
      typeof surface.pureEntry !== "string" ||
      surface.pureEntry.startsWith("/") ||
      surface.pureEntry.includes("..")
    ) {
      violations.push({
        code: "INVALID_MANIFEST",
        message: `${toRelative(rootDir, manifestPath)} surface ${String(surface.surfaceId)} has invalid pureEntry`,
        chain: [toRelative(rootDir, manifestPath)],
      });
      continue;
    }
    entries.push(join(ownerRoot, surface.pureEntry));
  }
  return entries;
}

export function checkUiPurity(rootDir: string = DEFAULT_ROOT): UiPurityResult {
  const violations: UiPurityViolation[] = [];
  const manifestPaths = findManifestPaths(rootDir);
  const entries = manifestPaths.flatMap((manifestPath) =>
    surfacePureEntries(rootDir, manifestPath, violations),
  );

  const visited = new Set<string>();
  for (const entry of entries) {
    checkSourcePurity(
      rootDir,
      entry,
      [toRelative(rootDir, entry)],
      visited,
      violations,
    );
  }

  return {
    ok: violations.length === 0,
    checkedEntries: entries.length,
    violations,
  };
}

if (process.argv[1]?.endsWith("check-ui-purity.ts")) {
  const result = checkUiPurity(resolve(process.cwd()));
  if (!result.ok) {
    for (const violation of result.violations) {
      console.error(`[UI_PURITY_${violation.code}] ${violation.message}`);
      console.error(`  chain: ${violation.chain.join(" -> ")}`);
    }
    process.exitCode = 1;
  } else {
    console.log(`Pure UI check valid: ${result.checkedEntries} entries.`);
  }
}

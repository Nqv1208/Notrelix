import ts from "typescript";
import { existsSync, readFileSync, readdirSync, statSync } from "node:fs";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const DEFAULT_ROOT = resolve(__dirname, "../../..");
const MANIFEST_FILE_NAME = "ui-evidence.manifest.json";

export interface UiActionViolation {
  readonly code: "INVALID_MANIFEST" | "MISSING_SOURCE" | "DEAD_ENABLED_CONTROL";
  readonly message: string;
}

export interface UiActionsResult {
  readonly ok: boolean;
  readonly checkedSources: number;
  readonly violations: UiActionViolation[];
}

interface ManifestSurface {
  readonly surfaceId?: unknown;
  readonly pureEntry?: unknown;
  readonly coveredSources?: unknown;
}

interface ManifestFile {
  readonly surfaces?: unknown;
}

interface ControlContext {
  readonly rootDir: string;
  readonly filePath: string;
  readonly sourceFile: ts.SourceFile;
  readonly importedButtonNames: ReadonlySet<string>;
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

function surfaceSources(
  rootDir: string,
  manifestPath: string,
  violations: UiActionViolation[],
): string[] {
  let manifest: ManifestFile;
  try {
    manifest = readJson(manifestPath) as ManifestFile;
  } catch (error) {
    violations.push({
      code: "INVALID_MANIFEST",
      message: `${toRelative(rootDir, manifestPath)} is not valid JSON: ${(error as Error).message}`,
    });
    return [];
  }

  if (!Array.isArray(manifest.surfaces)) {
    violations.push({
      code: "INVALID_MANIFEST",
      message: `${toRelative(rootDir, manifestPath)} must contain surfaces[]`,
    });
    return [];
  }

  const ownerRoot = dirname(dirname(manifestPath));
  const sources: string[] = [];
  for (const surface of manifest.surfaces as ManifestSurface[]) {
    const push = (relativePath: unknown, role: string): void => {
      if (
        typeof relativePath !== "string" ||
        relativePath.startsWith("/") ||
        relativePath.includes("..")
      ) {
        violations.push({
          code: "INVALID_MANIFEST",
          message: `${toRelative(rootDir, manifestPath)} surface ${String(surface.surfaceId)} has invalid ${role}`,
        });
        return;
      }
      const absPath = join(ownerRoot, relativePath);
      if (!fileExists(absPath)) {
        violations.push({
          code: "MISSING_SOURCE",
          message: `${toRelative(rootDir, manifestPath)} surface ${String(surface.surfaceId)} ${role} is missing: ${relativePath}`,
        });
        return;
      }
      sources.push(absPath);
    };
    push(surface.pureEntry, "pureEntry");
    if (Array.isArray(surface.coveredSources)) {
      for (const covered of surface.coveredSources) {
        push(covered, "coveredSource");
      }
    }
  }
  return sources;
}

function collectImportedButtonAliases(sourceFile: ts.SourceFile): Set<string> {
  const aliases = new Set<string>();
  for (const statement of sourceFile.statements) {
    if (!ts.isImportDeclaration(statement)) continue;
    if (!ts.isStringLiteral(statement.moduleSpecifier)) continue;
    if (!statement.moduleSpecifier.text.startsWith("@notrelix/ui-web")) continue;
    const clause = statement.importClause;
    if (!clause?.namedBindings || !ts.isNamedImports(clause.namedBindings)) continue;
    for (const element of clause.namedBindings.elements) {
      const imported = element.propertyName?.text ?? element.name.text;
      if (imported === "Button") aliases.add(element.name.text);
    }
  }
  return aliases;
}

function attributeName(property: ts.JsxAttribute): string | undefined {
  return ts.isIdentifier(property.name) ? property.name.text : undefined;
}

function hasCallbackProp(attributes: ts.JsxAttributes): boolean {
  return attributes.properties.some((property) => {
    if (!ts.isJsxAttribute(property)) return false;
    const name = attributeName(property);
    return !!name && /^on[A-Z]/.test(name);
  });
}

function hasDisabledSemantics(attributes: ts.JsxAttributes): boolean {
  return attributes.properties.some((property) => {
    if (!ts.isJsxAttribute(property)) return false;
    const name = attributeName(property);
    return name === "disabled" || name === "aria-disabled";
  });
}

function hasLinkSemantics(attributes: ts.JsxAttributes): boolean {
  return attributes.properties.some((property) => {
    if (!ts.isJsxAttribute(property)) return false;
    const name = attributeName(property);
    return name === "asChild" || name === "href";
  });
}

function hasSubmitSemantics(attributes: ts.JsxAttributes): boolean {
  return attributes.properties.some((property) => {
    if (!ts.isJsxAttribute(property)) return false;
    const name = attributeName(property);
    if (name === "form") return true;
    if (name === "type") {
      const { initializer } = property;
      return (
        !!initializer &&
        ts.isStringLiteral(initializer) &&
        initializer.text === "submit"
      );
    }
    return false;
  });
}

function isCompoundTriggerChild(
  parent: ts.Node | undefined,
): boolean {
  if (!parent) return false;

  const directChild = ts.isJsxSelfClosingElement(parent)
    ? parent
    : ts.isJsxElement(parent)
      ? parent
      : undefined;
  if (!directChild) return false;

  const wrapper = ts.isJsxElement(directChild.parent)
    ? directChild.parent
    : undefined;
  if (!wrapper) return false;

  const container = wrapper.openingElement;
  const tagName = ts.isIdentifier(container.tagName)
    ? container.tagName.text
    : undefined;
  if (!tagName) return false;
  if (/Trigger$/.test(tagName)) return hasAsChild(container.attributes);
  if (/Menu$/.test(tagName)) return true;
  return false;
}

function hasAsChild(attributes: ts.JsxAttributes): boolean {
  return attributes.properties.some(
    (property) =>
      ts.isJsxAttribute(property) && attributeName(property) === "asChild",
  );
}

function checkControl(
  context: ControlContext,
  node: ts.JsxOpeningElement | ts.JsxSelfClosingElement,
  violations: UiActionViolation[],
): void {
  if (isCompoundTriggerChild(node.parent)) return;
  const { attributes } = node;
  if (
    hasCallbackProp(attributes) ||
    hasDisabledSemantics(attributes) ||
    hasLinkSemantics(attributes) ||
    hasSubmitSemantics(attributes)
  ) {
    return;
  }
  const { line } = context.sourceFile.getLineAndCharacterOfPosition(
    node.getStart(context.sourceFile),
  );
  const tagName = ts.isIdentifier(node.tagName) ? node.tagName.text : "";
  violations.push({
    code: "DEAD_ENABLED_CONTROL",
    message: `${toRelative(context.rootDir, context.filePath)}:${line + 1} <${tagName}> is enabled with no callback, submit, disabled, or link semantics`,
  });
}

function checkSourceActions(
  context: ControlContext,
  violations: UiActionViolation[],
): void {
  const visit = (node: ts.Node): void => {
    if (ts.isJsxOpeningElement(node) || ts.isJsxSelfClosingElement(node)) {
      const { tagName } = node;
      const name = ts.isIdentifier(tagName)
        ? tagName.text
        : ts.isPropertyAccessExpression(tagName)
          ? tagName.name.text
          : undefined;
      if (name && (name === "button" || context.importedButtonNames.has(name))) {
        checkControl(context, node, violations);
      }
    }
    ts.forEachChild(node, visit);
  };
  visit(context.sourceFile);
}

export function checkUiActions(rootDir: string = DEFAULT_ROOT): UiActionsResult {
  const violations: UiActionViolation[] = [];
  const manifestPaths = findManifestPaths(rootDir);
  const allSources = new Set<string>();

  for (const manifestPath of manifestPaths) {
    for (const source of surfaceSources(rootDir, manifestPath, violations)) {
      allSources.add(source);
    }
  }

  for (const sourcePath of allSources) {
    if (!fileExists(sourcePath)) continue;
    const sourceFile = ts.createSourceFile(
      sourcePath,
      readFileSync(sourcePath, "utf8"),
      ts.ScriptTarget.Latest,
      false,
      ts.ScriptKind.TSX,
    );
    ts.setParentRecursive(sourceFile, true);
    checkSourceActions(
      {
        rootDir,
        filePath: sourcePath,
        sourceFile,
        importedButtonNames: collectImportedButtonAliases(sourceFile),
      },
      violations,
    );
  }

  return {
    ok: violations.length === 0,
    checkedSources: allSources.size,
    violations,
  };
}

if (process.argv[1]?.endsWith("check-ui-actions.ts")) {
  const result = checkUiActions(resolve(process.cwd()));
  if (!result.ok) {
    for (const violation of result.violations) {
      console.error(`[UI_ACTIONS_${violation.code}] ${violation.message}`);
    }
    process.exitCode = 1;
  } else {
    console.log(
      `UI actions check valid: ${result.checkedSources} registered sources.`,
    );
  }
}

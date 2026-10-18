/**
 * WX-FND-006 catalog completeness gate.
 *
 * Discovers every `*.catalog.ts` under `apps/<host>/src` and `packages/**\/src`,
 * loads the exported catalogs, and fails when a key is missing in either
 * locale (vi, en), a plural key lacks a CLDR form the locale needs, a message
 * is empty, or two catalogs claim one namespace.
 */
import { existsSync, readdirSync } from "node:fs";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import {
  findCatalogIssues,
  findDuplicateNamespaces,
  flattenMessages,
  type CatalogIssue,
  type MessageCatalog,
} from "@notrelix/i18n/catalog";

const __dirname = dirname(fileURLToPath(import.meta.url));
const DEFAULT_ROOT = resolve(__dirname, "../../..");

const CATALOG_FILE = /\.catalog\.ts$/;

function walk(dir: string): string[] {
  if (!existsSync(dir)) return [];
  const out: string[] = [];
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    if (
      entry.name === "node_modules" ||
      entry.name === "dist" ||
      entry.name.startsWith(".")
    )
      continue;
    const full = join(dir, entry.name);
    if (entry.isDirectory()) out.push(...walk(full));
    else if (CATALOG_FILE.test(entry.name)) out.push(full);
  }
  return out;
}

export function discoverCatalogFiles(rootDir: string): string[] {
  return [...walk(join(rootDir, "apps")), ...walk(join(rootDir, "packages"))]
    .filter((file) => /\/src\//.test(file.replace(/\\/g, "/")))
    .sort();
}

function isCatalog(value: unknown): value is MessageCatalog {
  if (!value || typeof value !== "object") return false;
  const candidate = value as Partial<MessageCatalog>;
  return (
    typeof candidate.namespace === "string" &&
    !!candidate.messages &&
    typeof candidate.messages === "object"
  );
}

export interface CatalogCheckResult {
  readonly ok: boolean;
  readonly files: number;
  readonly catalogs: number;
  readonly keys: number;
  readonly issues: readonly string[];
}

export async function checkI18nCatalogs(
  rootDir: string = DEFAULT_ROOT,
): Promise<CatalogCheckResult> {
  const files = discoverCatalogFiles(rootDir);
  const catalogs: MessageCatalog[] = [];
  const issues: string[] = [];

  for (const file of files) {
    const rel = relative(rootDir, file).replace(/\\/g, "/");
    const module = (await import(pathToFileURL(file).href)) as Record<
      string,
      unknown
    >;
    const exported = Object.values(module).filter(isCatalog);
    if (exported.length === 0) {
      issues.push(`${rel}: no catalog export (use defineCatalog)`);
    }
    for (const catalog of exported) {
      catalogs.push(catalog);
      for (const issue of findCatalogIssues(catalog)) {
        issues.push(formatIssue(rel, issue));
      }
    }
  }

  for (const namespace of findDuplicateNamespaces(catalogs)) {
    issues.push(
      `namespace "${namespace}" is declared by more than one catalog`,
    );
  }

  const keys = catalogs.reduce(
    (sum, catalog) =>
      sum + Object.keys(flattenMessages(catalog.messages.vi ?? {})).length,
    0,
  );

  return {
    ok: issues.length === 0,
    files: files.length,
    catalogs: catalogs.length,
    keys,
    issues,
  };
}

function formatIssue(file: string, issue: CatalogIssue): string {
  return `${file} [${issue.namespace}] ${issue.locale}: ${issue.kind} "${issue.key}"`;
}

if (process.argv[1]?.endsWith("check-i18n-catalogs.ts")) {
  const result = await checkI18nCatalogs();
  if (!result.ok) {
    console.error("[i18n-catalogs] FAIL");
    for (const issue of result.issues) console.error(`  - ${issue}`);
    process.exitCode = 1;
  } else if (result.catalogs === 0) {
    console.error("[i18n-catalogs] FAIL — no catalogs discovered");
    process.exitCode = 1;
  } else {
    console.log(
      `[i18n-catalogs] PASS — ${result.catalogs} catalog(s) in ${result.files} file(s), ${result.keys} key(s), vi + en complete.`,
    );
  }
}

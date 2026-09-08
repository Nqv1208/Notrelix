import { existsSync, readFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

export interface CommandGraphResult {
  readonly ok: boolean;
  readonly resolvedCommands: readonly string[];
  readonly violations: readonly string[];
}

export interface ScriptCatalog {
  readonly scripts: Record<string, string>;
  readonly workspaceScripts: Record<string, string>;
}

const REQUIRED_UI_COMMANDS = [
  "check:architecture",
  "check:ui-purity",
  "check:ui-actions",
  "check:ui-evidence",
  "typecheck",
  "lint",
  "format:check",
  "test:web:guarded",
  "test:ui:freeze",
];

const FORBIDDEN_UI_COMMANDS = [
  "codegen:check",
  "mock:freeze:check",
  "mock:contract",
  "e2e:mock",
  "e2e:real",
  "codegen",
];

const FORBIDDEN_TOKENS = [
  "docker",
  "docker-compose",
  "dotnet",
  "backend",
  "psql",
  "pg_dump",
  "pg_restore",
];

export function loadScriptCatalog(worktreeRoot: string): ScriptCatalog {
  const scripts = catalogScripts(
    join(worktreeRoot, "frontend", "package.json"),
  );
  const workspaceScripts: Record<string, string> = {};
  for (const dir of ["tooling/dependency-rules", "tooling/storybook/web"]) {
    const pkgPath = join(worktreeRoot, "frontend", dir, "package.json");
    if (!existsSync(pkgPath)) continue;
    const key = dir.replace(/\//g, "-");
    workspaceScripts[key] = catalogScripts(pkgPath);
  }
  return { scripts, workspaceScripts };
}

function catalogScripts(packageJsonPath: string): Record<string, string> {
  if (!existsSync(packageJsonPath)) return {};
  const raw = JSON.parse(readFileSync(packageJsonPath, "utf8")) as {
    scripts?: Record<string, string>;
  };
  return raw.scripts ?? {};
}

interface Traversal {
  readonly traversedScripts: Set<string>;
  readonly leafCommands: Set<string>;
}

function extractPnpmScript(tokens: readonly string[]): string | undefined {
  const runIdx = tokens.findIndex((t) => t === "run");
  if (runIdx >= 0 && tokens[runIdx + 1]) return tokens[runIdx + 1];
  return tokens.slice(1).find((t) => !t.startsWith("-") && t !== "--filter");
}

export function traverseCommandGraph(
  entryScript: string,
  catalog: ScriptCatalog,
): Traversal {
  const traversedScripts = new Set<string>();
  const leafCommands = new Set<string>();
  const allOwners = [
    catalog.scripts,
    ...Object.values(catalog.workspaceScripts),
  ];

  const findOwner = (name: string): Record<string, string> | undefined =>
    allOwners.find((owner) => name in owner);

  const visit = (name: string): void => {
    if (traversedScripts.has(name)) return;
    const owner = findOwner(name);
    if (!owner) {
      traversedScripts.add(name);
      return;
    }
    traversedScripts.add(name);

    const command = owner[name]!;
    const parts = command
      .split(/&&|\|\||;/)
      .map((p) => p.trim())
      .filter(Boolean);

    for (const part of parts) {
      const tokens = part.split(/\s+/).filter(Boolean);
      const first = tokens[0];
      if (first === "pnpm") {
        const scriptName = extractPnpmScript(tokens);
        if (scriptName) visit(scriptName);
        continue;
      }
      const bare = tokens[0];
      if (bare && tokens.length === 1 && findOwner(bare)) {
        visit(bare);
        continue;
      }
      leafCommands.add(part);
    }
  };

  visit(entryScript);
  return { traversedScripts, leafCommands };
}

export function resolveScriptNames(
  scriptName: string,
  catalog: ScriptCatalog,
): { readonly resolved: Set<string> } {
  const { leafCommands } = traverseCommandGraph(scriptName, catalog);
  return { resolved: leafCommands };
}

export function checkCommandGraph(
  scriptName: string,
  catalog: ScriptCatalog,
): CommandGraphResult {
  const violations: string[] = [];
  const { traversedScripts, leafCommands } = traverseCommandGraph(
    scriptName,
    catalog,
  );

  if (scriptName === "validate:ui") {
    for (const required of REQUIRED_UI_COMMANDS) {
      if (!traversedScripts.has(required)) {
        violations.push(`validate:ui missing required command: ${required}`);
      }
    }
  }

  const forbiddenSet = new Set<string>();
  for (const script of traversedScripts) {
    if (FORBIDDEN_UI_COMMANDS.includes(script)) forbiddenSet.add(script);
  }
  for (const item of leafCommands) {
    const tokens = item.split(/\s+/).filter(Boolean);
    for (const t of tokens) {
      const candidate = t.replace(/^pnpm:\/\//, "");
      if (FORBIDDEN_UI_COMMANDS.includes(candidate))
        forbiddenSet.add(candidate);
      if (FORBIDDEN_TOKENS.includes(t.toLowerCase())) forbiddenSet.add(t);
    }
  }

  for (const f of forbiddenSet) {
    violations.push(`validate:ui graph contains forbidden command/token: ${f}`);
  }

  return {
    ok: violations.length === 0,
    resolvedCommands: [...leafCommands],
    violations,
  };
}

if (process.argv[1]?.endsWith("check-command-graph.ts")) {
  const catalog = loadScriptCatalog(resolve(process.cwd()));
  const result = checkCommandGraph("validate:ui", catalog);
  if (!result.ok) {
    for (const violation of result.violations) {
      console.error(`[COMMAND_GRAPH_validate:ui] ${violation}`);
    }
    process.exitCode = 1;
  } else {
    console.log(
      `Command graph valid for validate:ui: ${result.resolvedCommands.length} resolved commands.`,
    );
  }
}

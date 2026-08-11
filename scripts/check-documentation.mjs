#!/usr/bin/env node
import { execFileSync } from "node:child_process";
import { existsSync, readdirSync, readFileSync } from "node:fs";
import { dirname, join, normalize, relative, resolve } from "node:path";

const root = resolve(new URL("..", import.meta.url).pathname);
const failures = [];

function fail(message) {
  failures.push(message);
}

function walk(dir, acc = []) {
  if (!existsSync(dir)) return acc;
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    if (
      entry.name === "node_modules" ||
      entry.name === ".git" ||
      entry.name === ".agents" ||
      entry.name === ".claude" ||
      entry.name === ".codex" ||
      entry.name === ".cursor" ||
      entry.name === ".gemini" ||
      entry.name === ".gstack" ||
      entry.name === ".mimocode" ||
      entry.name === ".opencode" ||
      entry.name === ".qwen" ||
      entry.name === "docs-refoundation"
    ) {
      continue;
    }
    const path = join(dir, entry.name);
    if (entry.isDirectory()) walk(path, acc);
    else acc.push(path);
  }
  return acc;
}

const markdownFiles = walk(root).filter((file) => file.endsWith(".md"));

for (const file of markdownFiles) {
  const text = readFileSync(file, "utf8");
  const display = relative(root, file);
  if (text.includes("file:///")) {
    fail(`${display}: contains absolute file:/// link`);
  }
  const linkPattern = /(?<!!)\[[^\]]+\]\(([^)]+)\)/g;
  for (const match of text.matchAll(linkPattern)) {
    const rawTarget = match[1].trim();
    if (
      rawTarget.startsWith("http://") ||
      rawTarget.startsWith("https://") ||
      rawTarget.startsWith("mailto:") ||
      rawTarget.startsWith("#")
    ) {
      continue;
    }
    const withoutAnchor = rawTarget.split("#")[0];
    if (!withoutAnchor) continue;
    const target = normalize(resolve(dirname(file), withoutAnchor));
    if (!target.startsWith(root)) {
      fail(`${display}: link escapes repository: ${rawTarget}`);
      continue;
    }
    if (!existsSync(target)) {
      fail(`${display}: broken relative link: ${rawTarget}`);
    }
  }
}

const forbiddenExisting = [
  "backend/RULE.md",
  "backend/PROMPT.md",
  "backend/CONFIGURATION.md",
  "frontend/ARCHITECTURE.md",
  "frontend/RULES.md",
  "frontend/MIGRATION_TRACKER.md",
  "docs/engineering",
  "backend/docs/ADR",
  "backend/docs/api",
  "backend/docs/application",
  "backend/docs/caching",
  "backend/docs/concurrency",
  "backend/docs/database",
  "backend/docs/domain",
  "backend/docs/infrastructure/rules",
  "backend/docs/issues",
  "backend/docs/messaging",
  "backend/docs/security",
  "backend/docs/superpowers",
  "backend/docs/testing",
  "frontend/docs/adr",
  "frontend/docs/client",
  "frontend/docs/client-architecture",
  "frontend/docs/plans",
  "frontend/docs/FRONTEND_PLATFORM_FREEZE_SPEC.md",
  "frontend/docs/notrelix-client-technical-project-structure.md",
];

for (const legacyPath of forbiddenExisting) {
  if (existsSync(join(root, legacyPath))) {
    fail(`forbidden legacy authority path still exists: ${legacyPath}`);
  }
}

const forbiddenRefs = [
  /backend\/RULE\.md/,
  /backend\/PROMPT\.md/,
  /backend\/CONFIGURATION\.md/,
  /frontend\/ARCHITECTURE\.md/,
  /frontend\/RULES\.md/,
  /MIGRATION_TRACKER\.md/,
  /docs\/client\/architecture/,
  /docs\/client-architecture/,
  /docs\/client\/adr/,
  /docs\/engineering/,
  /docs\/application/,
  /docs\/domain/,
  /docs\/infrastructure\/rules/,
];

for (const file of markdownFiles) {
  const display = relative(root, file);
  const text = readFileSync(file, "utf8");
  for (const pattern of forbiddenRefs) {
    if (pattern.test(text)) {
      fail(`${display}: references forbidden legacy/canonical path ${pattern}`);
    }
  }
}

function collectAdrIds(dir) {
  if (!existsSync(dir)) return;
  const seen = new Map();
  for (const file of readdirSync(dir)) {
    if (!file.endsWith(".md") || file === "README.md") continue;
    const id = file.replace(/\.md$/, "").split("-").slice(0, 3).join("-");
    if (seen.has(id)) {
      fail(`duplicate ADR id ${id}: ${seen.get(id)} and ${join(dir, file)}`);
    }
    seen.set(id, join(dir, file));
  }
}

collectAdrIds(join(root, "backend/docs/decisions"));
collectAdrIds(join(root, "frontend/docs/decisions"));

const backendSln = readFileSync(join(root, "backend/backend.slnx"), "utf8");
for (const project of [
  "Notrelix.Domain",
  "Notrelix.Application",
  "Notrelix.Infrastructure",
  "Notrelix.Platform",
  "Notrelix.API",
]) {
  if (!backendSln.includes(`src/${project}/${project}.csproj`)) {
    fail(`backend.slnx missing production project ${project}`);
  }
  const overview = readFileSync(
    join(root, "backend/docs/architecture/backend-overview.md"),
    "utf8",
  );
  if (!overview.includes(project)) {
    fail(`backend overview does not document production project ${project}`);
  }
}

for (const family of [
  "apps/*",
  "packages/foundation/*",
  "packages/runtimes/*",
  "packages/ui/*",
  "packages/product/*/*",
  "packages/features/*",
  "tooling/*",
]) {
  const workspace = readFileSync(join(root, "frontend/pnpm-workspace.yaml"), "utf8");
  const overview = readFileSync(
    join(root, "frontend/docs/architecture/frontend-overview.md"),
    "utf8",
  );
  if (!workspace.includes(family)) fail(`pnpm workspace missing ${family}`);
  const docToken = family.replace("/*/*", "").replace("/*", "");
  if (!overview.includes(docToken)) {
    fail(`frontend overview does not document package family ${docToken}`);
  }
}

try {
  execFileSync(
    "pnpm",
    ["--filter", "@notrelix/dependency-rules", "docs:check"],
    {
      cwd: join(root, "frontend"),
      stdio: "pipe",
    },
  );
} catch (error) {
  fail(
    `generated package-boundary drift check failed:\n${error.stdout ?? ""}${error.stderr ?? ""}`,
  );
}

for (const file of markdownFiles) {
  const text = readFileSync(file, "utf8");
  const display = relative(root, file);
  if (/develop branch|branch-specific basis|final-v\d|freeze version|fixed package count/i.test(text)) {
    fail(`${display}: contains forbidden branch/freeze/version authority wording`);
  }
}

if (failures.length > 0) {
  console.error("Documentation authority check failed:");
  for (const failure of failures) console.error(`- ${failure}`);
  process.exit(1);
}

console.log("Documentation authority check passed.");

#!/usr/bin/env node
/**
 * WX-FND-004 build check: Tailwind `@source` covers every package that ships
 * classes. Run after `pnpm --filter @notrelix/app-web build`.
 *
 * Each sentinel is a class that appears in its owner package but nowhere in
 * apps/web/src, so it can only reach the built CSS through that package's
 * `@source` entry in src/styles/globals.css.
 */
import { readFileSync, readdirSync, statSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import process from "node:process";

const log = (line) => process.stdout.write(`${line}\n`);
const fail = (line) => process.stderr.write(`${line}\n`);

const appRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const frontendRoot = resolve(appRoot, "../..");

const SENTINELS = [
  {
    className: "bg-table-bg",
    owner: "packages/product/work-management/web/src",
  },
  { className: "divide-border/40", owner: "packages/features/workspace/src" },
  { className: "text-balance", owner: "packages/ui/web/src" },
];

function walk(dir) {
  const out = [];
  for (const entry of readdirSync(dir)) {
    if (entry === "node_modules" || entry.startsWith(".")) continue;
    const full = join(dir, entry);
    if (statSync(full).isDirectory()) out.push(...walk(full));
    else if (/\.(tsx?|jsx?)$/.test(entry)) out.push(full);
  }
  return out;
}

function sourcesContain(dir, className) {
  return walk(dir).some((file) =>
    readFileSync(file, "utf8").includes(className),
  );
}

function escapeSelector(className) {
  return `.${className.replace(/[/:[\].%]/g, (char) => `\\${char}`)}`;
}

const assetsDir = join(appRoot, "dist/assets");
let css;
try {
  css = readdirSync(assetsDir)
    .filter((file) => file.endsWith(".css"))
    .map((file) => readFileSync(join(assetsDir, file), "utf8"))
    .join("\n");
} catch {
  fail("[css-sources] dist/assets not found — run the web build first.");
  process.exit(1);
}

const failures = [];
for (const { className, owner } of SENTINELS) {
  if (!sourcesContain(join(frontendRoot, owner), className)) {
    failures.push(
      `${className}: sentinel no longer used in ${owner}; pick another`,
    );
    continue;
  }
  if (sourcesContain(join(appRoot, "src"), className)) {
    failures.push(
      `${className}: also used in apps/web/src; not a package-only sentinel`,
    );
    continue;
  }
  if (!css.includes(escapeSelector(className))) {
    failures.push(
      `${className}: missing from built CSS — ${owner} is not covered by @source`,
    );
  }
}

if (failures.length > 0) {
  for (const failure of failures) fail(`[css-sources] ${failure}`);
  process.exit(1);
}
log(
  `[css-sources] PASS — ${SENTINELS.length} package-only sentinel classes present in built CSS.`,
);

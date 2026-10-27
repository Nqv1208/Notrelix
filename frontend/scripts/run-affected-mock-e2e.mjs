#!/usr/bin/env node
// Runs the mock e2e specs that read the UI a change touches, so a stale spec
// fails before the push instead of in CI. Used by scripts/pre-push.sh.
//
//   node scripts/run-affected-mock-e2e.mjs <changed path>...   (repo-root paths)
//   node scripts/run-affected-mock-e2e.mjs --list <changed path>...
//
// A changed spec runs itself. A changed source path runs the specs of its
// area (RULES below); a path in no area runs nothing. The run uses its own
// dev server on a free port and CI-like settings (no design-fidelity specs).
import { spawnSync } from "node:child_process";
import { readdirSync } from "node:fs";
import { createServer } from "node:net";

// First matching rule per path; each lists spec name prefixes in e2e/mock.
const RULES = [
  [/^frontend\/packages\/product\/docs\//, ["docs-page", "documents"]],
  [
    /^frontend\/packages\/dev\/mock-backend\/src\/(contexts\/documents|state\/mock-docs)/,
    ["docs-page", "documents"],
  ],
  [
    /^frontend\/packages\/product\/work-management\//,
    [
      "board-",
      "item-page",
      "table-interaction",
      "work-management",
      "home",
      "my-work",
    ],
  ],
  [
    /^frontend\/packages\/features\/workspace\//,
    ["home", "my-work", "reports", "workspace-", "space-", "tablet-sidebar"],
  ],
  [/^frontend\/packages\/features\/auth\//, ["auth-flow", "auth-workspace"]],
  [
    /^frontend\/apps\/web\/src\/(shell|components\/shell)/,
    ["workspace-shell", "tablet-sidebar", "home"],
  ],
  [/^frontend\/apps\/web\/src\/routes\/(sign-in|invite)/, ["auth-flow"]],
  [
    /^frontend\/packages\/dev\/mock-backend\//,
    ["home", "workspace-shell", "item-page.mock", "board-kanban.mock"],
  ],
];

const SPEC_DIR = "e2e/mock";
const SUFFIX = ".mock.e2e.spec.ts";

function affectedSpecs(changed, specs) {
  const picked = new Set();
  for (const path of changed) {
    const own = /^frontend\/e2e\/mock\/([^/]+\.mock\.e2e\.spec\.ts)$/.exec(
      path,
    );
    if (own) {
      picked.add(own[1]);
      continue;
    }
    const rule = RULES.find(([pattern]) => pattern.test(path));
    if (!rule) continue;
    for (const spec of specs) {
      if (rule[1].some((prefix) => spec.startsWith(prefix))) picked.add(spec);
    }
  }
  // Fidelity specs only run on the dev machine against the Design render.
  return [...picked].filter((spec) => !spec.startsWith("design-fidelity-"));
}

function freePort() {
  return new Promise((resolve, reject) => {
    const server = createServer();
    server.once("error", reject);
    server.listen(0, "127.0.0.1", () => {
      const { port } = server.address();
      server.close(() => resolve(String(port)));
    });
  });
}

const args = process.argv.slice(2);
const listOnly = args[0] === "--list";
const changed = listOnly ? args.slice(1) : args;
const specs = readdirSync(SPEC_DIR).filter((name) => name.endsWith(SUFFIX));
const selected = affectedSpecs(changed, specs).sort();

if (selected.length === 0) {
  console.log("[mock e2e] no mock spec covers the changed files");
  process.exit(0);
}
console.log(
  `[mock e2e] ${selected.length} spec file(s): ${selected.join(", ")}`,
);
if (listOnly) process.exit(0);

const result = spawnSync(
  "pnpm",
  [
    "exec",
    "playwright",
    "test",
    "--config",
    "playwright.mock.config.ts",
    "--reporter=line",
    ...selected.map((spec) => `${SPEC_DIR}/${spec}`),
  ],
  {
    stdio: "inherit",
    env: { ...process.env, CI: "1", MOCK_E2E_PORT: await freePort() },
  },
);
process.exit(result.status ?? 1);

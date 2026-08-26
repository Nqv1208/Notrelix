#!/usr/bin/env node
// Exact-artifact round-trip contract:
//   package (symlink-preserving) -> hash -> restore (symlink-safe)
//   -> restored tree is RUNNABLE (spawn node server, wait readiness)
// plus a negative case: archives with destination-escaping symlinks are
// rejected before extraction.
import { createHash } from "node:crypto";
import { spawnSync } from "node:child_process";
import {
  cpSync,
  existsSync,
  mkdirSync,
  readFileSync,
  rmSync,
  symlinkSync,
  writeFileSync,
} from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";

const scriptDir = new URL(".", import.meta.url).pathname;
const work = join(tmpdir(), `host-artifact-rt-${process.pid}-${Date.now()}`);
rmSync(work, { recursive: true, force: true });
mkdirSync(work, { recursive: true });

// pnpm/Next standalone-shaped fixture: app resolves @swc/helpers through
// relative symlink chains into a content-addressed store.
const fixtureRoot = join(work, "fixture", "frontend");
const standalone = join(fixtureRoot, "apps/mkt/.next/standalone/apps/mkt");
const store = join(
  fixtureRoot,
  "apps/mkt/.next/standalone/node_modules/.pnpm/helpers@1/node_modules/@swc/helpers",
);
mkdirSync(standalone, { recursive: true });
mkdirSync(store, { recursive: true });
writeFileSync(join(store, "index.js"), "module.exports = () => 'ok';\n");
writeFileSync(join(store, "package.json"), '{"name":"@swc/helpers"}\n');
writeFileSync(
  join(standalone, "server.js"),
  [
    "const helper = require('@swc/helpers');",
    "const http = require('node:http');",
    "http.createServer((_, res) => res.end('artifact:' + helper()))",
    "  .listen(Number(process.env.PORT) || 3199, '127.0.0.1');",
    "",
  ].join("\n"),
);
mkdirSync(join(standalone, "node_modules/@swc"), { recursive: true });
symlinkSync(
  "../../../../node_modules/.pnpm/helpers@1/node_modules/@swc/helpers",
  join(standalone, "node_modules/@swc/helpers"),
  "dir",
);

function run(script, args, opts = {}) {
  const r = spawnSync(process.execPath, [join(scriptDir, script), ...args], {
    cwd: fixtureRoot,
    encoding: "utf8",
    ...opts,
  });
  if (r.status !== 0 && !opts.expectFailure) {
    console.error(`${script} failed\n${r.stdout}\n${r.stderr}`);
    process.exit(r.status ?? 1);
  }
  return r;
}

// 1. Package preserving symlink topology.
run("package-host-artifact.mjs", [
  "--component",
  "mkt",
  "--paths-json",
  JSON.stringify(["apps/mkt/.next/standalone"]),
  "--output",
  join(work, "mkt.tar.gz"),
  "--manifest",
  join(work, "mkt.manifest.json"),
]);

// 2. Hash-verified restore into a fresh destination.
const dest = join(work, "restore");
mkdirSync(dest, { recursive: true });
run("restore-host-artifact.mjs", [
  "--component",
  "mkt",
  "--archive",
  join(work, "mkt.tar.gz"),
  "--manifest",
  join(work, "mkt.manifest.json"),
  "--destination",
  dest,
]);
const restoredLink = join(
  dest,
  "apps/mkt/.next/standalone/apps/mkt/node_modules/@swc/helpers",
);

// 3. Symlinks survived; dereference would have produced zero links.
if (!existsSync(join(restoredLink, "index.js"))) {
  console.error("FAIL: symlinked dependency unreachable after restore");
  process.exit(1);
}

// 4. Restored artifact is runnable: start server, wait readiness.
const { spawn } = await import("node:child_process");
const proc = spawn(
  process.execPath,
  [join(dest, "apps/mkt/.next/standalone/apps/mkt/server.js")],
  { env: { ...process.env, PORT: "3199" }, stdio: "ignore" },
);
try {
  let ok = false;
  for (let i = 0; i < 50 && !ok; i++) {
    const curl = spawnSync("curl", ["-fsS", "http://127.0.0.1:3199/"], {
      encoding: "utf8",
    });
    ok = curl.status === 0 && /artifact:ok/.test(curl.stdout ?? "");
    if (!ok) spawnSync("sleep", ["0.1"]);
  }
  if (!ok) {
    console.error("FAIL: restored standalone server did not become ready");
    process.exit(1);
  }
} finally {
  proc.kill();
}

// 5. Negative: destination-escaping symlink rejected before extraction.
const evil = join(work, "evil");
mkdirSync(join(evil, "payload"), { recursive: true });
writeFileSync(join(evil, "payload", "f.txt"), "x\n");
symlinkSync("/etc/passwd", join(evil, "payload", "escape"));
spawnSync("tar", ["-czf", join(work, "evil.tgz"), "-C", evil, "payload"]);
const manifest = JSON.parse(
  readFileSync(join(work, "mkt.manifest.json"), "utf8"),
);
manifest.sha256 = createHash("sha256")
  .update(readFileSync(join(work, "evil.tgz")))
  .digest("hex");
writeFileSync(
  join(work, "evil.manifest.json"),
  JSON.stringify(manifest, null, 2) + "\n",
);
const badDest = join(work, "baddest");
mkdirSync(badDest, { recursive: true });
const bad = run(
  "restore-host-artifact.mjs",
  [
    "--component",
    "mkt",
    "--archive",
    join(work, "evil.tgz"),
    "--manifest",
    join(work, "evil.manifest.json"),
    "--destination",
    badDest,
  ],
  { expectFailure: true },
);
if (
  bad.status === 0 ||
  !/unsafe symlink escape|out-of-contract|unparsable/.test(
    bad.stderr + bad.stdout,
  )
) {
  console.error("FAIL: escaping-symlink archive was not rejected");
  process.exit(1);
}

rmSync(work, { recursive: true, force: true });
console.log("host-artifact roundtrip (symlink-preserving + runnable) PASS");

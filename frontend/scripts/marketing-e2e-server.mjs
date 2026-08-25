#!/usr/bin/env node
/**
 * Marketing production-layout E2E server.
 *
 * Normal local use builds the app first. CI sets MARKETING_E2E_REUSE_BUILD=1
 * after downloading the exact artifact produced by the build-marketing job;
 * in that mode this script MUST NOT rebuild.
 */
import { spawn } from "node:child_process";
import { cpSync, existsSync } from "node:fs";
import path from "node:path";

const root = path.resolve(import.meta.dirname, "..");
const appDir = path.join(root, "apps", "marketing");
const reuseBuild = process.env.MARKETING_E2E_REUSE_BUILD === "1";

function serve() {
  const standaloneRoot = path.join(appDir, ".next", "standalone");
  const standaloneApp = path.join(standaloneRoot, "apps", "marketing");
  if (!existsSync(path.join(standaloneApp, "server.js"))) {
    console.error(
      "[marketing-e2e-server] standalone app output missing:",
      standaloneApp,
    );
    process.exit(1);
  }

  cpSync(
    path.join(appDir, ".next", "static"),
    path.join(standaloneApp, ".next", "static"),
    {
      recursive: true,
    },
  );
  if (existsSync(path.join(appDir, "public"))) {
    cpSync(path.join(appDir, "public"), path.join(standaloneApp, "public"), {
      recursive: true,
    });
  }

  const server = spawn("node", [path.join(standaloneApp, "server.js")], {
    stdio: "inherit",
    cwd: appDir,
    env: {
      ...process.env,
      PORT: process.env.PORT ?? "3100",
      HOSTNAME: process.env.HOSTNAME ?? "127.0.0.1",
    },
  });
  server.on("exit", (code) => process.exit(code ?? 0));
}

if (reuseBuild) {
  console.log("[marketing-e2e-server] reusing exact CI build artifact");
  serve();
} else {
  const build = spawn(
    "pnpm",
    ["--filter", "@notrelix/app-marketing", "build"],
    {
      stdio: "inherit",
      shell: process.platform === "win32",
    },
  );
  build.on("exit", (code) => {
    if (code !== 0) process.exit(code ?? 1);
    serve();
  });
}

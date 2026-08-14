#!/usr/bin/env node

/**
 * Marketing e2e web server.
 *
 * Builds @notrelix/app-marketing, wires the standalone output (static +
 * public) like Dockerfile.marketing does, then serves it on
 * 127.0.0.1:${PORT:-3100}. Kept as a script so e2e uses the exact
 * production layout instead of "next start", which does not work with
 * "output: standalone".
 */

import { spawn } from "node:child_process";
import { cpSync, existsSync } from "node:fs";
import path from "node:path";

const root = path.resolve(import.meta.dirname, "..");
const appDir = path.join(root, "apps", "marketing");

const build = spawn("pnpm", ["--filter", "@notrelix/app-marketing", "build"], {
  stdio: "inherit",
  shell: process.platform === "win32",
});

build.on("exit", (code) => {
  if (code !== 0) process.exit(code ?? 1);

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
    { recursive: true },
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

  server.on("exit", (serverCode) => process.exit(serverCode ?? 0));
});

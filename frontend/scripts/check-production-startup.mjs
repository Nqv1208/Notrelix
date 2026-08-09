#!/usr/bin/env node

import { spawn } from "node:child_process";

const url = process.env.VITE_APP_URL ?? "http://127.0.0.1:4173";
const timeoutMs = 30_000;

const server = spawn(
  "pnpm",
  [
    "--filter",
    "@notrelix/app-web",
    "preview",
    "--host",
    "127.0.0.1",
    "--port",
    "4173",
  ],
  {
    stdio: "pipe",
    shell: process.platform === "win32",
  },
);

let settled = false;

const timer = setTimeout(() => {
  finish(1, `Timed out waiting for production preview at ${url}`);
}, timeoutMs);

server.on("exit", (code) => {
  if (!settled && code !== 0) {
    finish(
      code ?? 1,
      `Production preview exited before startup check completed.`,
    );
  }
});

poll();

async function poll() {
  while (!settled) {
    try {
      const response = await fetch(url);
      if (response.ok) {
        finish(0, `Production preview responded at ${url}`);
        return;
      }
    } catch {
      // Keep polling until timeout.
    }
    await new Promise((resolve) => setTimeout(resolve, 500));
  }
}

function finish(exitCode, message) {
  if (settled) return;
  settled = true;
  clearTimeout(timer);
  server.kill("SIGTERM");
  if (exitCode === 0) {
    console.log(message);
  } else {
    console.error(message);
  }
  process.exit(exitCode);
}

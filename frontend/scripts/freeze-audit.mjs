#!/usr/bin/env node

import { spawnSync } from "node:child_process";

const gates = [
  ["VALIDATE", ["validate"]],
  ["UI_FREEZE", ["test:ui:freeze"]],
  ["BUILD", ["build"]],
  ["PRODUCTION_STARTUP", ["production:startup"]],
  ["E2E", ["e2e"]],
];

const env = {
  ...process.env,
  CI: "1",
  VITE_API_URL:
    process.env.VITE_API_URL ?? "https://api.example.invalid/api/v1",
  VITE_WS_URL: process.env.VITE_WS_URL ?? "wss://api.example.invalid/realtime",
  VITE_APP_URL: process.env.VITE_APP_URL ?? "http://127.0.0.1:4173",
  VITE_RELEASE_SHA: process.env.VITE_RELEASE_SHA ?? "local-freeze-audit",
  VITE_MOCK_API: "false",
  EXPO_PUBLIC_API_URL:
    process.env.EXPO_PUBLIC_API_URL ?? "https://api.example.invalid/api/v1",
  EXPO_PUBLIC_REALTIME_URL:
    process.env.EXPO_PUBLIC_REALTIME_URL ??
    "wss://api.example.invalid/realtime",
  EXPO_PUBLIC_APP_URL:
    process.env.EXPO_PUBLIC_APP_URL ?? "https://app.example.invalid",
  EXPO_PUBLIC_RELEASE_SHA:
    process.env.EXPO_PUBLIC_RELEASE_SHA ?? "local-freeze-audit",
};

let failed = false;

for (const [name, args] of gates) {
  const result = spawnSync("pnpm", args, {
    cwd: process.cwd(),
    env,
    stdio: "inherit",
    shell: process.platform === "win32",
  });

  if (result.status !== 0) {
    failed = true;
    console.error(`[freeze:audit] ${name}: FAIL`);
  } else {
    console.log(`[freeze:audit] ${name}: PASS`);
  }
}

process.exit(failed ? 1 : 0);

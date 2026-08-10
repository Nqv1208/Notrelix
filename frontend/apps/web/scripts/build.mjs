#!/usr/bin/env node

import { spawnSync } from "node:child_process";
import process from "node:process";

const localProductionDefaults =
  process.env.CI !== "1" && process.env.VERCEL !== "1"
    ? {
        VITE_API_URL:
          process.env.VITE_API_URL ?? "https://api.example.invalid/api/v1",
        VITE_WS_URL:
          process.env.VITE_WS_URL ?? "wss://api.example.invalid/realtime",
        VITE_APP_URL: process.env.VITE_APP_URL ?? "http://127.0.0.1:4173",
        VITE_RELEASE_SHA: process.env.VITE_RELEASE_SHA ?? "local-build",
        VITE_MOCK_API: process.env.VITE_MOCK_API ?? "false",
      }
    : {};

const result = spawnSync("vite", ["build"], {
  env: {
    ...process.env,
    ...localProductionDefaults,
  },
  stdio: "inherit",
  shell: process.platform === "win32",
});

process.exit(result.status ?? 1);

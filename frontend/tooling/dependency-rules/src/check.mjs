#!/usr/bin/env node
/* global process */
import { execFileSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const __dirname = dirname(fileURLToPath(import.meta.url));
const args = process.argv.slice(2);

try {
  execFileSync("pnpm", ["exec", "tsx", "src/run-checks.ts", ...args], {
    cwd: join(__dirname, ".."),
    stdio: "inherit",
  });
} catch (err) {
  process.exit(err.status ?? 1);
}

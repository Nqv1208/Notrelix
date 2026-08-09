#!/usr/bin/env node
import { execSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const __dirname = dirname(fileURLToPath(import.meta.url));
const args = process.argv
  .slice(2)
  .map((a) => `"${a}"`)
  .join(" ");

try {
  execSync(`pnpm exec tsx src/run-checks.ts ${args}`, {
    cwd: join(__dirname, ".."),
    stdio: "inherit",
  });
} catch (err) {
  process.exit(err.status ?? 1);
}

#!/usr/bin/env node
import { spawnSync } from "child_process";
import { resolve } from "path";
import { fileURLToPath } from "url";

const __dirname = fileURLToPath(new URL(".", import.meta.url));
const tsx = resolve(__dirname, "../node_modules/.bin/tsx");

const result = spawnSync(tsx, [resolve(__dirname, "check-mock-freeze-logic.ts")], {
  stdio: "inherit",
});

if (result.error) {
  console.error("Failed to run check-mock-freeze-logic.ts", result.error);
  process.exit(1);
}
process.exit(result.status);

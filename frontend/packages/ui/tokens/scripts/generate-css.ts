/**
 * Writes src/css/index.css from the token source.
 * Usage: pnpm --filter @notrelix/ui-tokens generate:css
 */
import { writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { buildTokensCss } from "../src/css/build-css";

const target = resolve(
  dirname(fileURLToPath(import.meta.url)),
  "../src/css/index.css",
);
writeFileSync(target, buildTokensCss());
console.log(`[ui-tokens] wrote ${target}`);

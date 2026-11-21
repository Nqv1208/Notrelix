#!/usr/bin/env node
// Prettier turns a bare __tests__ path into bold **tests**; fail on those lines.
import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";

const root = process.argv[2] ?? "docs";
const pattern = /\*\*(tests|snapshots|mocks|fixtures)\*\*/;
const failures = [];

function walk(dir) {
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const path = join(dir, entry.name);
    if (entry.isDirectory()) walk(path);
    else if (entry.name.endsWith(".md")) {
      readFileSync(path, "utf8")
        .split("\n")
        .forEach((line, index) => {
          const match = pattern.exec(line);
          if (match) {
            failures.push(
              `${path}:${index + 1}: "${match[0]}" is a mangled path; wrap the path in backticks (\`__${match[1]}__\`)`,
            );
          }
        });
    }
  }
}

walk(root);

if (failures.length > 0) {
  console.error(failures.join("\n"));
  process.exit(1);
}
console.log("Mangled-path check passed.");

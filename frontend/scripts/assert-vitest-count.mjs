import { readFileSync } from "node:fs";

const outputFile = process.argv[2];

if (!outputFile) {
  console.error(
    "Usage: node scripts/assert-vitest-count.mjs <vitest-json-output-file>",
  );
  process.exit(1);
}

let report;
try {
  report = JSON.parse(readFileSync(outputFile, "utf8"));
} catch {
  console.error(
    `Zero-test guard: could not read vitest JSON report at ${outputFile}. ` +
      "The suite likely failed before producing a report.",
  );
  process.exit(1);
}

const total = report.numTotalTests ?? 0;

if (total === 0) {
  console.error(
    `Zero-test guard: zero tests discovered (${outputFile}). ` +
      "Critical suites must fail when zero tests are discovered.",
  );
  process.exit(1);
}

console.log(`Zero-test guard: ${total} tests executed (${outputFile}).`);

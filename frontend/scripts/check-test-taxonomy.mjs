import { validateTestTaxonomy } from "./test-taxonomy-core.mjs";

const result = validateTestTaxonomy(process.cwd());

if (!result.valid) {
  console.error(
    "Test taxonomy violation. Every Vitest file must have one explicit suite suffix:",
  );
  for (const file of result.invalid) console.error(`  - ${file}`);
  process.exit(1);
}

console.log(
  `Test taxonomy OK: ${result.testFiles.length} Vitest test files classified.`,
);

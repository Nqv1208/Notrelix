import { readFileSync } from "node:fs";

const reportPath = process.argv[2];

if (!reportPath) {
  console.error(
    "Usage: node scripts/assert-mobile-vitest-coverage.mjs <vitest-json-report>",
  );
  process.exit(1);
}

let report;
try {
  report = JSON.parse(readFileSync(reportPath, "utf8"));
} catch {
  console.error(`Could not read Vitest report: ${reportPath}`);
  process.exit(1);
}

const categories = [
  ["app-mobile", "/apps/mobile/"],
  ["runtime-mobile", "/packages/runtimes/mobile/"],
  ["ui-mobile", "/packages/ui/mobile/"],
  ["work-management-mobile", "/packages/product/work-management/mobile/"],
  ["docs-mobile", "/packages/product/docs/mobile/"],
  ["automation-mobile", "/packages/product/automation/mobile/"],
];

const names = (report.testResults ?? []).map((result) =>
  String(result.name ?? "").replaceAll("\\", "/"),
);

const missing = [];

for (const [label, segment] of categories) {
  if (
    !names.some(
      (name) => name.includes(segment) || name.startsWith(segment.slice(1)),
    )
  ) {
    missing.push(label);
  }
}

if (missing.length > 0) {
  console.error(
    `Mobile test coverage guard failed. Missing categories: ${missing.join(", ")}`,
  );
  process.exit(1);
}

console.log(
  `Mobile test coverage guard passed: ${categories.length} required categories executed.`,
);

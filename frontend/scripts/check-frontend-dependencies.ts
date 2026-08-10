import { checkArchitecture } from "../tooling/dependency-rules/src/check-frontend-dependencies";

const result = checkArchitecture();

if (!result.ok) {
  console.error(
    `❌ Architecture check failed with ${result.violations.length} violation(s):\n`,
  );
  for (const v of result.violations) {
    console.error(`   ${v}`);
  }
  process.exit(1);
} else {
  console.log("✅ AST Architecture check passed with 0 violations.");
  process.exit(0);
}

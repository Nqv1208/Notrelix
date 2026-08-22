import { readFileSync, existsSync } from "fs";
import { resolve } from "path";
import { ENABLED_CONSUMERS } from "../tooling/contracts/enabled-consumer-surface";
import { buildOperationRegistry } from "../packages/dev/mock-backend/src/operations/build-registry";

console.log(
  "[mock:freeze:check] Starting MockData Freeze Automated Verification...",
);

// 1. canonical enabled consumer IDs
const canonicalConsumerIds = new Set(
  ENABLED_CONSUMERS.filter((c) => c.classification === "CANONICAL_MOCKED")
    .map((c) => c.operationId)
    .filter(Boolean) as string[],
);

const gapConsumerIds = new Set(
  ENABLED_CONSUMERS.filter(
    (c) =>
      c.classification === "COMPATIBILITY_GAP_MOCKED" ||
      c.classification === "CONTRACT_BLOCKED_UI_DISABLED",
  )
    .map((c) => c.gapId)
    .filter(Boolean) as string[],
);

const registry = buildOperationRegistry();
const metadata = registry.operationMetadata();

const registryCanonicalIds = new Set(
  metadata
    .filter((m) => m.contract.kind === "openapi")
    .map((m) => (m.contract as any).operationId),
);

const registryGapIds = new Set(
  metadata
    .filter((m) => m.contract.kind === "gap")
    .map((m) => (m.contract as any).gapId),
);

let hasError = false;
function assertSetEqual(
  setName1: string,
  set1: Set<string>,
  setName2: string,
  set2: Set<string>,
) {
  for (const item of set1) {
    if (!set2.has(item)) {
      console.error(`FAIL: ${item} is in ${setName1} but not in ${setName2}`);
      hasError = true;
    }
  }
  for (const item of set2) {
    if (!set1.has(item)) {
      console.error(`FAIL: ${item} is in ${setName2} but not in ${setName1}`);
      hasError = true;
    }
  }
}

console.log(
  `Checking canonical enabled consumer IDs (${canonicalConsumerIds.size}) vs registry canonical IDs (${registryCanonicalIds.size})`,
);
assertSetEqual(
  "Canonical Enabled Consumers",
  canonicalConsumerIds,
  "Registry Canonical Ops",
  registryCanonicalIds,
);

console.log(
  `Checking gap enabled consumer IDs (${gapConsumerIds.size}) vs registry gap IDs (${registryGapIds.size})`,
);
assertSetEqual(
  "Gap Enabled Consumers",
  gapConsumerIds,
  "Registry Gap Ops",
  registryGapIds,
);

if (hasError) {
  process.exit(1);
}

console.log("ALL MOCKDATA FREEZE GATES PASSED CLEANLY.");

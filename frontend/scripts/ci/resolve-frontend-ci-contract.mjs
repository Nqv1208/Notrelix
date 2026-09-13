// Resolves the planner-provided contract for one frontend CI matrix cell.
// Contracts come from the central planner (frontend_host_matrix / mobile_matrix)
// in orchestrated mode or the migration fallbacks in standalone mode. This helper
// is the single place that turns a matrix cell's component_id into the concrete
// build/runtime contract, failing closed on any shape violation.
// Emits fields as STEP OUTPUTS ($GITHUB_OUTPUT) — never ambient GITHUB_ENV state.
// Run: node scripts/ci/resolve-frontend-ci-contract.mjs --component web \
//        --hosts-json '{"include":[...]}' --mobiles-json '{"include":[...]}'
import { appendFileSync } from "node:fs";

const HOST_ONLY_FIELDS = [
  "e2e_script",
  "artifact_name",
  "archive_file",
  "manifest_file",
  "artifact_paths_json",
];
const REQUIRED_HOST_FIELDS = ["workspace", "build_script", ...HOST_ONLY_FIELDS];
const REQUIRED_MOBILE_FIELDS = ["workspace", "build_script"];

class ContractResolutionError extends Error {}

function fail(message) {
  throw new ContractResolutionError(message);
}

function parseMatrix(raw, label) {
  let parsed;
  try {
    parsed = JSON.parse(raw);
  } catch {
    return fail(`${label} is not valid JSON`);
  }
  const include = parsed?.include;
  if (!Array.isArray(include)) {
    return fail(`${label} must be a matrix object with an include array`);
  }
  for (const entry of include) {
    if (
      !entry ||
      typeof entry !== "object" ||
      typeof entry.component_id !== "string" ||
      !entry.component_id
    ) {
      return fail(`${label} contains an entry without a component_id`);
    }
  }
  return include;
}

export function resolveFrontendContract({
  component,
  hostsJson,
  mobilesJson,
  requireHost = false,
}) {
  if (!component) fail("missing --component");
  const hosts = parseMatrix(hostsJson, "hosts matrix");
  const mobiles = parseMatrix(mobilesJson, "mobiles matrix");
  const hostMatches = hosts.filter((entry) => entry.component_id === component);
  const mobileMatches = mobiles.filter(
    (entry) => entry.component_id === component,
  );
  if (
    hostMatches.length > 1 ||
    mobileMatches.length > 1 ||
    hostMatches.length + mobileMatches.length > 1
  ) {
    fail(`duplicate contract entries for component ${component}`);
  }
  if (hostMatches.length === 0 && mobileMatches.length === 0) {
    fail(
      `no contract found for component ${component} in planner host/mobile matrices`,
    );
  }
  const isHost = hostMatches.length === 1;
  const contract = isHost ? hostMatches[0] : mobileMatches[0];
  for (const field of isHost ? REQUIRED_HOST_FIELDS : REQUIRED_MOBILE_FIELDS) {
    if (typeof contract[field] !== "string" || !contract[field]) {
      fail(
        `${isHost ? "host" : "mobile"} contract for ${component} is missing ${field}`,
      );
    }
  }
  if (!isHost && requireHost) {
    fail(
      `component ${component} resolved as a mobile contract; host runtime requires a host contract`,
    );
  }
  return {
    component_id: component,
    workspace: contract.workspace,
    build_script: contract.build_script,
    is_host: String(isHost),
    artifact_name: isHost ? contract.artifact_name : "",
    archive_file: isHost ? contract.archive_file : "",
    manifest_file: isHost ? contract.manifest_file : "",
    artifact_paths_json: isHost ? contract.artifact_paths_json : "",
    e2e_script: isHost ? contract.e2e_script : "",
  };
}

function writeOutputs(contract) {
  const outputPath = process.env.GITHUB_OUTPUT;
  const lines = Object.entries(contract)
    .map(([key, value]) => `${key}=${value}`)
    .join("\n");
  if (!outputPath) {
    console.log(lines);
    return;
  }
  appendFileSync(outputPath, `${lines}\n`);
}

if (
  process.argv[1] &&
  import.meta.url.endsWith(process.argv[1].split("/").pop())
) {
  const argv = process.argv;
  const value = (flag) => {
    const index = argv.indexOf(flag);
    return index >= 0 ? argv[index + 1] : "";
  };
  try {
    const contract = resolveFrontendContract({
      component: value("--component"),
      hostsJson: value("--hosts-json"),
      mobilesJson: value("--mobiles-json"),
      requireHost: argv.includes("--require-host"),
    });
    writeOutputs(contract);
  } catch (error) {
    console.error(
      `::error::frontend contract resolution failed: ${error.message}`,
    );
    process.exit(1);
  }
}

// CONTRACT-01..08 acceptance fixtures for the frontend CI contract resolver.
// Run: node scripts/ci/resolve-frontend-ci-contract.test.mjs
import assert from "node:assert/strict";
import { resolveFrontendContract } from "./resolve-frontend-ci-contract.mjs";

const HOSTS = {
  include: [
    {
      component_id: "web",
      workspace: "@notrelix/app-web",
      build_script: "build",
      e2e_script: "e2e",
      artifact_name: "frontend-host-build-web",
      archive_file: "web.tar.gz",
      manifest_file: "web.manifest.json",
      artifact_paths_json: '["apps/web/dist"]',
    },
    {
      component_id: "marketing",
      workspace: "@notrelix/app-marketing",
      build_script: "build",
      e2e_script: "e2e:marketing",
      artifact_name: "frontend-host-build-marketing",
      archive_file: "marketing.tar.gz",
      manifest_file: "marketing.manifest.json",
      artifact_paths_json: '["apps/marketing/.next/standalone"]',
    },
  ],
};
const MOBILES = {
  include: [
    {
      component_id: "mobile",
      workspace: "@notrelix/app-mobile",
      build_script: "build",
    },
  ],
};

function failingCall(args) {
  try {
    resolveFrontendContract(args);
  } catch (error) {
    assert.ok(error instanceof Error);
    return error.message;
  }
  assert.fail("expected contract resolution to fail");
}

// CONTRACT-01 — valid host contract resolves every execution field
{
  const contract = resolveFrontendContract({
    component: "web",
    hostsJson: JSON.stringify(HOSTS),
    mobilesJson: JSON.stringify(MOBILES),
  });
  assert.equal(contract.workspace, "@notrelix/app-web");
  assert.equal(contract.build_script, "build");
  assert.equal(contract.is_host, "true");
  assert.equal(contract.e2e_script, "e2e");
  assert.equal(contract.artifact_name, "frontend-host-build-web");
  assert.equal(contract.artifact_paths_json, '["apps/web/dist"]');
  console.log("CONTRACT-01 valid host PASS");
}

// CONTRACT-02 — valid mobile contract carries build fields only
{
  const contract = resolveFrontendContract({
    component: "mobile",
    hostsJson: JSON.stringify(HOSTS),
    mobilesJson: JSON.stringify(MOBILES),
  });
  assert.equal(contract.workspace, "@notrelix/app-mobile");
  assert.equal(contract.is_host, "false");
  assert.equal(contract.artifact_name, "");
  assert.equal(contract.e2e_script, "");
  console.log("CONTRACT-02 valid mobile PASS");
}

// CONTRACT-03 — missing component fails closed
{
  const message = failingCall({
    component: "unknown",
    hostsJson: JSON.stringify(HOSTS),
    mobilesJson: JSON.stringify(MOBILES),
  });
  assert.match(message, /no contract found for component unknown/);
  console.log("CONTRACT-03 missing component PASS");
}

// CONTRACT-04 — duplicate component across matrices fails closed
{
  const message = failingCall({
    component: "web",
    hostsJson: JSON.stringify(HOSTS),
    mobilesJson: JSON.stringify({
      include: [
        {
          component_id: "web",
          workspace: "@notrelix/app-web",
          build_script: "build",
        },
        ...MOBILES.include,
      ],
    }),
  });
  assert.match(message, /duplicate contract entries/);
  console.log("CONTRACT-04 duplicate component PASS");
}

// CONTRACT-05 — malformed JSON fails closed
{
  const message = failingCall({
    component: "web",
    hostsJson: "{not json",
    mobilesJson: JSON.stringify(MOBILES),
  });
  assert.match(message, /hosts matrix is not valid JSON/);
  console.log("CONTRACT-05 malformed JSON PASS");
}

// CONTRACT-06 — host contract missing an artifact field fails closed
{
  const broken = {
    include: [{ ...HOSTS.include[0], manifest_file: "" }],
  };
  const message = failingCall({
    component: "web",
    hostsJson: JSON.stringify(broken),
    mobilesJson: JSON.stringify(MOBILES),
  });
  assert.match(message, /host contract for web is missing manifest_file/);
  console.log("CONTRACT-06 host missing artifact field PASS");
}

// CONTRACT-07 — host runtime refuses a mobile contract
{
  const message = failingCall({
    component: "mobile",
    hostsJson: JSON.stringify(HOSTS),
    mobilesJson: JSON.stringify(MOBILES),
    requireHost: true,
  });
  assert.match(message, /host runtime requires a host contract/);
  console.log("CONTRACT-07 mobile refused for host runtime PASS");
}

// CONTRACT-08 — matrix payload without include array fails closed
{
  const message = failingCall({
    component: "web",
    hostsJson: '{"cells":[]}',
    mobilesJson: JSON.stringify(MOBILES),
  });
  assert.match(
    message,
    /hosts matrix must be a matrix object with an include array/,
  );
  console.log("CONTRACT-08 malformed matrix shape PASS");
}

console.log("CONTRACT-01..08 all PASS");

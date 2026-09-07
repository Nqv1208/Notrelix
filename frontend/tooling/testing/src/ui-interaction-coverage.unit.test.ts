import { describe, expect, it } from "vitest";
import { mkdtempSync, writeFileSync, mkdirSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { spawnSync } from "node:child_process";
import { resolve } from "node:path";

const SCRIPT = resolve(
  __dirname,
  "../../../scripts/assert-ui-interaction-coverage.mjs",
);

function runGuard(report) {
  const root = mkdtempSync(join(tmpdir(), "notrelix-ui-coverage-"));
  mkdirSync(join(root, "packages/features/a/verification"), { recursive: true });
  writeFileSync(
    join(root, "packages/features/a/verification/ui-evidence.manifest.json"),
    JSON.stringify({
      schemaVersion: 2,
      owner: "@notrelix/a",
      inventory: { excludedSources: [] },
      surfaces: [
        {
          surfaceId: "a.surface",
          owner: "@notrelix/a",
          surfaceKind: "feedback",
          pureEntry: "src/a.tsx",
          coveredSources: [],
          stories: [],
          stateCoverage: {
            required: [],
            delegated: [],
            notApplicable: [
              {
                state: "Default",
                reason: "Default is owned elsewhere.",
                authority: "docs/product/work-management.md",
              },
            ],
          },
          responsive: false,
          themeAware: false,
          checks: ["interaction"],
          interactionCases: [
            { id: "action", testFile: "src/a.component.test.tsx" },
          ],
        },
      ],
    }),
  );
  const reportPath = join(root, "report.json");
  writeFileSync(reportPath, JSON.stringify(report));
  return spawnSync("node", [SCRIPT, reportPath, root], {
    encoding: "utf8",
    cwd: root,
  });
}

const manifest = {
  entries: {
    "one": {
      id: "one",
      tags: ["fui-surface--a.surface", "fui-state--Default"],
    },
  },
};

function report(assertions) {
  return {
    numTotalTests: assertions.length,
    testResults: [
      {
        assertionResults: assertions.map(([title, status]) => ({
          title,
          status,
        })),
      },
    ],
  };
}

describe("assert-ui-interaction-coverage (synthetic reports)", () => {
  it("satisfies a declared case with a passing exact marker (TST-073, TST-077)", () => {
    const result = runGuard(
      report([["FUI[a.surface:action] emits the action", "passed"]]),
    );
    expect(result.status).toBe(0);
    expect(result.stdout).toContain(
      "1 manifest-declared interaction cases satisfied",
    );
  });

  it("fails a missing required marker (TST-074)", () => {
    const result = runGuard(report([["unrelated green test", "passed"]]));
    expect(result.status).toBe(1);
    expect(result.stderr).toContain(
      "missing passing case marker: FUI[a.surface:action]",
    );
  });

  it("fails a failed or skipped required marker (TST-074)", () => {
    const failed = runGuard(
      report([["FUI[a.surface:action] emits the action", "failed"]]),
    );
    expect(failed.status).toBe(1);
    expect(failed.stderr).toContain("not passing: failed: FUI[a.surface:action]");

    const skipped = runGuard(
      report([["FUI[a.surface:action] emits the action", "skipped"]]),
    );
    expect(skipped.status).toBe(1);
    expect(skipped.stderr).toContain(
      "not passing: skipped: FUI[a.surface:action]",
    );
  });

  it("reports an unknown surface/case marker as drift (TST-075)", () => {
    const result = runGuard(
      report([
        ["FUI[a.surface:action] emits the action", "passed"],
        ["FUI[b.surface:mystery] drift", "passed"],
      ]),
    );
    expect(result.status).toBe(1);
    expect(result.stderr).toContain(
      "unknown case marker in report: FUI[b.surface:mystery]",
    );
  });

  it("is insufficient when a green unrelated assertion lacks the marker (TST-076)", () => {
    const result = runGuard(report([["some other behavior works", "passed"]]));
    expect(result.status).toBe(1);
    expect(result.stderr).toContain("missing passing case marker");
  });

  it("rejects duplicate passing markers", () => {
    const result = runGuard(
      report([
        ["FUI[a.surface:action] one", "passed"],
        ["FUI[a.surface:action] two", "passed"],
      ]),
    );
    expect(result.status).toBe(1);
    expect(result.stderr).toContain("duplicate passing case marker");
  });
});

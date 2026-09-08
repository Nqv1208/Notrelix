import { afterEach, describe, expect, it } from "vitest";
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import {
  checkArchitecture,
  isVerificationSource,
} from "../check-frontend-dependencies";
import { checkPackageManifests } from "../check-package-manifests";

let root: string | undefined;
afterEach(() => {
  if (root) rmSync(root, { recursive: true, force: true });
  root = undefined;
});

function createWebPackage(
  sourcePath: string,
  manifestSection: Record<string, Record<string, string>> = {},
) {
  root = mkdtempSync(join(tmpdir(), "notrelix-verification-import-"));
  const packageRoot = join(root, "packages/product/work-management/web");
  mkdirSync(join(packageRoot, "src", ...sourcePath.split("/").slice(0, -1)), {
    recursive: true,
  });
  writeFileSync(
    join(packageRoot, "package.json"),
    JSON.stringify({
      name: "@notrelix/work-management-web",
      version: "0.0.0",
      ...manifestSection,
    }),
  );
  writeFileSync(
    join(packageRoot, "src", sourcePath),
    'import { boardFixture } from "@notrelix/work-management-testing";\nexport const fixture = boardFixture;\n',
  );
  return root;
}

describe("verification-only dependency authority", () => {
  it("recognizes only repository-approved verification path forms", () => {
    expect(isVerificationSource("/src/x.stories.tsx")).toBe(true);
    expect(isVerificationSource("/src/x.test.tsx")).toBe(true);
    expect(isVerificationSource("/src/x.spec.ts")).toBe(true);
    expect(isVerificationSource("/src/__tests__/x.ts")).toBe(true);
    expect(isVerificationSource("/src/verification/scenarios/x.ts")).toBe(true);
    expect(isVerificationSource("/src/story-helper.ts")).toBe(false);
  });

  it("rejects production source importing product-testing", () => {
    const violations = checkArchitecture(
      createWebPackage("component.tsx"),
    ).violations;
    expect(
      violations.some(
        (value) =>
          value.includes("[NOT_ALLOWED_IMPORT]") &&
          value.includes("component.tsx"),
      ),
    ).toBe(true);
  });

  it("accepts matching story and test imports", () => {
    for (const path of [
      "component.stories.tsx",
      "component.test.tsx",
      "verification/scenarios/board.ts",
    ]) {
      const fixtureRoot = createWebPackage(path);
      const violations = checkArchitecture(fixtureRoot).violations;
      expect(
        violations.some(
          (value) =>
            value.includes("[NOT_ALLOWED_IMPORT]") && value.includes(path),
        ),
      ).toBe(false);
      if (root) rmSync(root, { recursive: true, force: true });
      root = undefined;
    }
  });

  it("requires verification dependencies to be devDependencies", () => {
    const productionRoot = createWebPackage("component.tsx", {
      dependencies: { "@notrelix/work-management-testing": "workspace:*" },
    });
    expect(checkPackageManifests(productionRoot).violations).toContainEqual(
      expect.stringContaining("[VERIFICATION_DEPENDENCY_MUST_BE_DEV]"),
    );
    if (root) rmSync(root, { recursive: true, force: true });
    root = undefined;
    const devRoot = createWebPackage("component.stories.tsx", {
      devDependencies: { "@notrelix/work-management-testing": "workspace:*" },
    });
    expect(
      checkPackageManifests(devRoot).violations.some((value) =>
        value.includes("DECLARED_INTERNAL_DEP_NOT_ALLOWED"),
      ),
    ).toBe(false);
  });
});

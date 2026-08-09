import { afterEach, describe, expect, test } from "vitest";
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { checkArchitecture } from "../check-frontend-dependencies";
import { validateArchitectureManifest } from "../architecture-manifest";
import type { ArchitecturePackagePolicy } from "../architecture-manifest";

let currentRoot: string | null = null;

afterEach(() => {
  if (currentRoot) {
    rmSync(currentRoot, { recursive: true, force: true });
    currentRoot = null;
  }
});

function createFixtureRoot(): string {
  currentRoot = mkdtempSync(join(tmpdir(), "notrelix-closed-world-"));
  return currentRoot;
}

function writePackage(
  root: string,
  packagePath: string,
  packageName: string,
  source = "",
): void {
  const dir = join(root, packagePath);
  mkdirSync(join(dir, "src"), { recursive: true });
  writeFileSync(
    join(dir, "package.json"),
    JSON.stringify(
      { name: packageName, version: "0.0.0", type: "module" },
      null,
      2,
    ),
  );
  writeFileSync(join(dir, "src", "index.ts"), source);
}

describe("closed-world checker preflight", () => {
  test("ARCH-020 unregistered package produces UNREGISTERED_PACKAGE", () => {
    const root = createFixtureRoot();
    writePackage(root, "packages/features/zeta", "@notrelix/features-zeta");

    const { violations } = checkArchitecture(root);

    expect(
      violations.some(
        (v) =>
          v.includes("UNREGISTERED_PACKAGE") &&
          v.includes("@notrelix/features-zeta"),
      ),
    ).toBe(true);
  });

  test("ARCH-021 manifest entry without a discovered package produces STALE_PACKAGE_POLICY", () => {
    const root = createFixtureRoot();
    // An empty workspace means every manifest entry is stale.
    mkdirSync(join(root, "packages"), { recursive: true });

    const { violations } = checkArchitecture(root);

    expect(violations.some((v) => v.includes("STALE_PACKAGE_POLICY"))).toBe(
      true,
    );
  });

  test("ARCH-022 package.json name differing from manifest produces PACKAGE_NAME_MISMATCH", () => {
    const root = createFixtureRoot();
    writePackage(
      root,
      "packages/foundation/kernel",
      "@notrelix/kernel-renamed",
    );

    const { violations } = checkArchitecture(root);

    expect(
      violations.some(
        (v) =>
          v.includes("PACKAGE_NAME_MISMATCH") && v.includes("@notrelix/kernel"),
      ),
    ).toBe(true);
  });

  test("ARCH-023 unknown allowed import target produces UNKNOWN_ALLOWED_IMPORT", () => {
    const badManifest: readonly ArchitecturePackagePolicy[] = [
      {
        packageName: "@notrelix/kernel",
        relativePath: "packages/foundation/kernel",
        layer: "foundation",
        freezeScope: "core-production",
        allowedInternalImports: ["@notrelix/does-not-exist"],
      },
    ];

    const violations = validateArchitectureManifest(badManifest);

    expect(
      violations.some(
        (v) =>
          v.code === "UNKNOWN_ALLOWED_IMPORT" &&
          v.packageName === "@notrelix/kernel",
      ),
    ).toBe(true);
  });

  test("self edge produces SELF_IMPORT_POLICY", () => {
    const badManifest: readonly ArchitecturePackagePolicy[] = [
      {
        packageName: "@notrelix/kernel",
        relativePath: "packages/foundation/kernel",
        layer: "foundation",
        freezeScope: "core-production",
        allowedInternalImports: ["@notrelix/kernel"],
      },
    ];

    const violations = validateArchitectureManifest(badManifest);

    expect(violations.some((v) => v.code === "SELF_IMPORT_POLICY")).toBe(true);
  });

  test("duplicate allowed edge produces DUPLICATE_ALLOWED_IMPORT", () => {
    const badManifest: readonly ArchitecturePackagePolicy[] = [
      {
        packageName: "@notrelix/platform",
        relativePath: "packages/foundation/platform",
        layer: "foundation",
        freezeScope: "core-production",
        allowedInternalImports: ["@notrelix/kernel", "@notrelix/kernel"],
      },
      {
        packageName: "@notrelix/kernel",
        relativePath: "packages/foundation/kernel",
        layer: "foundation",
        freezeScope: "core-production",
        allowedInternalImports: [],
      },
    ];

    const violations = validateArchitectureManifest(badManifest);

    expect(violations.some((v) => v.code === "DUPLICATE_ALLOWED_IMPORT")).toBe(
      true,
    );
  });

  test("duplicate package name produces DUPLICATE_PACKAGE_NAME", () => {
    const badManifest: readonly ArchitecturePackagePolicy[] = [
      {
        packageName: "@notrelix/kernel",
        relativePath: "packages/foundation/kernel",
        layer: "foundation",
        freezeScope: "core-production",
        allowedInternalImports: [],
      },
      {
        packageName: "@notrelix/kernel",
        relativePath: "packages/foundation/kernel-copy",
        layer: "foundation",
        freezeScope: "core-production",
        allowedInternalImports: [],
      },
    ];

    const violations = validateArchitectureManifest(badManifest);

    expect(violations.some((v) => v.code === "DUPLICATE_PACKAGE_NAME")).toBe(
      true,
    );
  });

  test("duplicate relative path produces DUPLICATE_PACKAGE_PATH", () => {
    const badManifest: readonly ArchitecturePackagePolicy[] = [
      {
        packageName: "@notrelix/kernel",
        relativePath: "packages/foundation/kernel",
        layer: "foundation",
        freezeScope: "core-production",
        allowedInternalImports: [],
      },
      {
        packageName: "@notrelix/kernel-alias",
        relativePath: "packages/foundation/kernel",
        layer: "foundation",
        freezeScope: "core-production",
        allowedInternalImports: [],
      },
    ];

    const violations = validateArchitectureManifest(badManifest);

    expect(violations.some((v) => v.code === "DUPLICATE_PACKAGE_PATH")).toBe(
      true,
    );
  });
});

describe("closed-world import enforcement", () => {
  test("ARCH-024 ui-icons importing platform is forbidden", () => {
    const root = createFixtureRoot();
    writePackage(
      root,
      "packages/ui/icons",
      "@notrelix/ui-icons",
      "import { anything } from '@notrelix/platform';\nexport const x = anything;\n",
    );

    const { violations } = checkArchitecture(root);

    expect(
      violations.some(
        (v) =>
          v.includes("NOT_ALLOWED_IMPORT") &&
          v.includes("@notrelix/ui-icons") &&
          v.includes("@notrelix/platform"),
      ),
    ).toBe(true);
  });

  test("ARCH-025 docs-state importing ui-web is forbidden", () => {
    const root = createFixtureRoot();
    writePackage(
      root,
      "packages/product/docs/state",
      "@notrelix/docs-state",
      "import { Button } from '@notrelix/ui-web';\nexport const x = Button;\n",
    );

    const { violations } = checkArchitecture(root);

    expect(
      violations.some(
        (v) =>
          v.includes("NOT_ALLOWED_IMPORT") &&
          v.includes("@notrelix/docs-state") &&
          v.includes("@notrelix/ui-web"),
      ),
    ).toBe(true);
  });

  test("ARCH-026 automation-state importing ui-web is forbidden", () => {
    const root = createFixtureRoot();
    writePackage(
      root,
      "packages/product/automation/state",
      "@notrelix/automation-state",
      "import { Button } from '@notrelix/ui-web';\nexport const x = Button;\n",
    );

    const { violations } = checkArchitecture(root);

    expect(
      violations.some(
        (v) =>
          v.includes("NOT_ALLOWED_IMPORT") &&
          v.includes("@notrelix/automation-state") &&
          v.includes("@notrelix/ui-web"),
      ),
    ).toBe(true);
  });

  test("ARCH-027 app-web importing automation-testing is forbidden", () => {
    const root = createFixtureRoot();
    writePackage(
      root,
      "apps/web",
      "@notrelix/app-web",
      "import { fixture } from '@notrelix/automation-testing';\nexport const x = fixture;\n",
    );

    const { violations } = checkArchitecture(root);

    expect(
      violations.some(
        (v) =>
          v.includes("NOT_ALLOWED_IMPORT") &&
          v.includes("@notrelix/app-web") &&
          v.includes("@notrelix/automation-testing"),
      ),
    ).toBe(true);
  });

  test("ARCH-028 deep src imports remain forbidden under closed-world enforcement", () => {
    const root = createFixtureRoot();
    writePackage(
      root,
      "packages/ui/icons",
      "@notrelix/ui-icons",
      "import { internal } from '@notrelix/platform/src/internal';\nexport const x = internal;\n",
    );

    const { violations } = checkArchitecture(root);

    expect(
      violations.some(
        (v) => v.includes("DEEP_IMPORT") && v.includes("@notrelix/ui-icons"),
      ),
    ).toBe(true);
  });

  test("a registered package with allowed imports passes import enforcement", () => {
    const root = createFixtureRoot();
    writePackage(
      root,
      "packages/product/docs/state",
      "@notrelix/docs-state",
      "import { kernel } from '@notrelix/kernel';\nexport const x = kernel;\n",
    );

    const { violations } = checkArchitecture(root);

    expect(
      violations.some(
        (v) =>
          v.includes("NOT_ALLOWED_IMPORT") &&
          v.includes("@notrelix/docs-state"),
      ),
    ).toBe(false);
  });

  test("ARCH-029 direct localStorage access outside runtimes is forbidden", () => {
    const root = createFixtureRoot();
    writePackage(
      root,
      "packages/ui/web",
      "@notrelix/ui-web",
      "export const read = () => localStorage.getItem('theme');\n",
    );

    const { violations } = checkArchitecture(root);

    expect(violations.some((v) => v.includes("FORBIDDEN_STORAGE_ACCESS"))).toBe(
      true,
    );
  });

  test("ARCH-030 storage access is allowed inside runtime adapters", () => {
    const root = createFixtureRoot();
    writePackage(
      root,
      "packages/runtimes/web",
      "@notrelix/runtime-web",
      "export const read = () => localStorage.getItem('theme');\n",
    );

    const { violations } = checkArchitecture(root);

    expect(violations.some((v) => v.includes("FORBIDDEN_STORAGE_ACCESS"))).toBe(
      false,
    );
  });

  test("ARCH-031 createRoute import from packages is forbidden", () => {
    const root = createFixtureRoot();
    writePackage(
      root,
      "packages/features/auth",
      "@notrelix/features-auth",
      "import { createRoute } from '@tanstack/react-router';\nexport const route = createRoute;\n",
    );

    const { violations } = checkArchitecture(root);

    expect(violations.some((v) => v.includes("FORBIDDEN_ROUTE_CREATION"))).toBe(
      true,
    );
  });

  test("ARCH-032 createRoute call from packages is forbidden", () => {
    const root = createFixtureRoot();
    writePackage(
      root,
      "packages/features/workspace",
      "@notrelix/features-workspace",
      "const route = createRoute({ path: '/' });\nexport const x = route;\n",
    );

    const { violations } = checkArchitecture(root);

    expect(violations.some((v) => v.includes("FORBIDDEN_ROUTE_CREATION"))).toBe(
      true,
    );
  });

  test("ARCH-033 non-router imports from @tanstack/react-router are allowed in features", () => {
    const root = createFixtureRoot();
    writePackage(
      root,
      "packages/features/auth",
      "@notrelix/features-auth",
      "import { Link } from '@tanstack/react-router';\nexport const x = Link;\n",
    );

    const { violations } = checkArchitecture(root);

    expect(violations.some((v) => v.includes("FORBIDDEN_ROUTE_CREATION"))).toBe(
      false,
    );
  });

  test("ARCH-034 mobile WebSocket allowed only in native-websocket-factory.ts", () => {
    const root = createFixtureRoot();
    writePackage(
      root,
      "packages/runtimes/mobile",
      "@notrelix/runtime-mobile",
      "export * from './realtime/native-websocket-factory';\n",
    );
    const dir = join(root, "packages/runtimes/mobile/src/realtime");
    mkdirSync(dir, { recursive: true });
    writeFileSync(
      join(dir, "native-websocket-factory.ts"),
      "export const factory = () => new WebSocket('wss://example.test');\n",
    );

    const { violations } = checkArchitecture(root);

    expect(
      violations.some((v) => v.includes("FORBIDDEN_WEBSOCKET_INSTANTIATION")),
    ).toBe(false);
  });

  test("ARCH-035 mobile WebSocket outside native-websocket-factory.ts is forbidden", () => {
    const root = createFixtureRoot();
    writePackage(
      root,
      "packages/runtimes/mobile",
      "@notrelix/runtime-mobile",
      "export {};\n",
    );
    const dir = join(root, "packages/runtimes/mobile/src/other");
    mkdirSync(dir, { recursive: true });
    writeFileSync(
      join(dir, "rogue.ts"),
      "export const socket = new WebSocket('wss://example.test');\n",
    );

    const { violations } = checkArchitecture(root);

    expect(
      violations.some((v) => v.includes("FORBIDDEN_WEBSOCKET_INSTANTIATION")),
    ).toBe(true);
  });
});

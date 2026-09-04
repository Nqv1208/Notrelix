import { describe, expect, it } from "vitest";
import { readFileSync, readdirSync, statSync, existsSync } from "node:fs";
import { join, resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import {
  ARCHITECTURE_MANIFEST,
  ARCHITECTURE_POLICY_BY_PACKAGE,
} from "../architecture-manifest";

const __dirname = dirname(fileURLToPath(import.meta.url));
const FRONTEND_ROOT = resolve(__dirname, "../../../..");

interface DiscoveredPackage {
  readonly name: string;
  readonly relativePath: string;
}

function discoverWorkspacePackages(): DiscoveredPackage[] {
  const discovered: DiscoveredPackage[] = [];

  function walk(base: string, depth = 0): void {
    for (const entry of readdirSync(base)) {
      const full = join(base, entry);
      if (!statSync(full).isDirectory()) continue;
      if (entry.startsWith(".") || entry === "node_modules" || entry === "dist")
        continue;
      if (existsSync(join(full, "package.json"))) {
        const pkg = JSON.parse(
          readFileSync(join(full, "package.json"), "utf8"),
        ) as { name: string };
        discovered.push({
          name: pkg.name,
          relativePath: full
            .slice(FRONTEND_ROOT.length + 1)
            .replace(/\\/g, "/"),
        });
      } else if (depth < 4) {
        walk(full, depth + 1);
      }
    }
  }

  walk(join(FRONTEND_ROOT, "apps"));
  walk(join(FRONTEND_ROOT, "packages"));
  return discovered;
}

describe("closed-world architecture manifest", () => {
  const discovered = discoverWorkspacePackages();
  const discoveredNames = new Set(discovered.map((pkg) => pkg.name));
  const manifestNames: Set<string> = new Set(
    ARCHITECTURE_MANIFEST.map((entry) => entry.packageName),
  );

  it("ARCH-001 current workspace package set exactly equals manifest package set", () => {
    expect([...manifestNames].sort()).toEqual([...discoveredNames].sort());
  });

  it("ARCH-002 every manifest path exists", () => {
    for (const entry of ARCHITECTURE_MANIFEST) {
      expect(
        existsSync(join(FRONTEND_ROOT, entry.relativePath, "package.json")),
        entry.packageName,
      ).toBe(true);
    }
  });

  it("ARCH-003 manifest name matches package.json name", () => {
    for (const entry of ARCHITECTURE_MANIFEST) {
      const pkg = JSON.parse(
        readFileSync(
          join(FRONTEND_ROOT, entry.relativePath, "package.json"),
          "utf8",
        ),
      ) as { name: string };
      expect(pkg.name, entry.relativePath).toBe(entry.packageName);
    }
  });

  it("ARCH-004 no duplicate package name or path", () => {
    const names = ARCHITECTURE_MANIFEST.map((entry) => entry.packageName);
    const paths = ARCHITECTURE_MANIFEST.map((entry) => entry.relativePath);
    expect(new Set(names).size).toBe(names.length);
    expect(new Set(paths).size).toBe(paths.length);
  });

  it("ARCH-005 no unknown allowed import target", () => {
    for (const entry of ARCHITECTURE_MANIFEST) {
      for (const target of entry.allowedInternalImports) {
        expect(
          manifestNames.has(target),
          `${entry.packageName} -> ${target}`,
        ).toBe(true);
      }
    }
  });

  it("ARCH-006 no self edges", () => {
    for (const entry of ARCHITECTURE_MANIFEST) {
      expect(entry.allowedInternalImports, entry.packageName).not.toContain(
        entry.packageName,
      );
    }
  });

  it("ARCH-007 no duplicate allowed edges", () => {
    for (const entry of ARCHITECTURE_MANIFEST) {
      expect(
        new Set(entry.allowedInternalImports).size,
        entry.packageName,
      ).toBe(entry.allowedInternalImports.length);
    }
  });

  it("ARCH-008 production scope cannot allow product-testing", () => {
    const testingPackages: Set<string> = new Set(
      ARCHITECTURE_MANIFEST.filter(
        (entry) => entry.layer === "product-testing",
      ).map((entry) => entry.packageName),
    );
    for (const entry of ARCHITECTURE_MANIFEST) {
      if (entry.freezeScope !== "core-production") continue;
      for (const target of entry.allowedInternalImports) {
        expect(
          testingPackages.has(target),
          `${entry.packageName} -> ${target}`,
        ).toBe(false);
      }
    }
  });

  it("ARCH-009 @notrelix/ui-icons is present and stale @notrelix/icons is absent", () => {
    expect(manifestNames.has("@notrelix/ui-icons")).toBe(true);
    expect(manifestNames.has("@notrelix/icons")).toBe(false);
  });

  it("ARCH-010 @notrelix/docs-state is present", () => {
    expect(manifestNames.has("@notrelix/docs-state")).toBe(true);
  });

  it("ARCH-011 automation-state and automation-testing are present", () => {
    expect(manifestNames.has("@notrelix/automation-state")).toBe(true);
    expect(manifestNames.has("@notrelix/automation-testing")).toBe(true);
  });

  it("ARCH-012 runtime-web permits realtime and observability", () => {
    const runtimeWeb = ARCHITECTURE_POLICY_BY_PACKAGE.get(
      "@notrelix/runtime-web",
    );
    expect(runtimeWeb).toBeDefined();
    expect(runtimeWeb!.allowedInternalImports).toContain("@notrelix/realtime");
    expect(runtimeWeb!.allowedInternalImports).toContain(
      "@notrelix/observability",
    );
  });

  it("ARCH-013 freeze scope uses only the closed taxonomy", () => {
    const allowed = new Set([
      "core-production",
      "verification",
      "marketing-isolated",
    ]);
    for (const entry of ARCHITECTURE_MANIFEST) {
      expect(allowed.has(entry.freezeScope), entry.packageName).toBe(true);
    }
  });

  it("ARCH-014 mobile structural units are core-production", () => {
    const mobileUnits = [
      "@notrelix/runtime-mobile",
      "@notrelix/ui-mobile",
      "@notrelix/app-mobile",
      "@notrelix/work-management-mobile",
      "@notrelix/docs-mobile",
      "@notrelix/automation-mobile",
    ];
    for (const name of mobileUnits) {
      const entry = ARCHITECTURE_POLICY_BY_PACKAGE.get(name);
      expect(entry, name).toBeDefined();
      expect(entry!.freezeScope, name).toBe("core-production");
    }
  });

  it("ARCH-015 feature roots never allow ui-mobile", () => {
    for (const entry of ARCHITECTURE_MANIFEST) {
      if (entry.layer !== "feature") continue;
      expect(entry.allowedInternalImports, entry.packageName).not.toContain(
        "@notrelix/ui-mobile",
      );
    }
  });

  it("ARCH-016 feature allow-lists match the least-privilege contract", () => {
    const expectExact = (name: string, expected: readonly string[]) => {
      const entry = ARCHITECTURE_POLICY_BY_PACKAGE.get(name);
      expect(entry, name).toBeDefined();
      expect([...entry!.allowedInternalImports].sort(), name).toEqual(
        [...expected].sort(),
      );
    };

    expectExact("@notrelix/features-auth", [
      "@notrelix/contracts",
      "@notrelix/kernel",
      "@notrelix/platform",
      "@notrelix/query",
      "@notrelix/ui-web",
    ]);
    expectExact("@notrelix/features-workspace", [
      "@notrelix/contracts",
      "@notrelix/kernel",
      "@notrelix/platform",
      "@notrelix/query",
      "@notrelix/ui-web",
    ]);
    expectExact("@notrelix/features-account", [
      "@notrelix/contracts",
      "@notrelix/kernel",
      "@notrelix/platform",
      "@notrelix/query",
      "@notrelix/ui-web",
    ]);
    expectExact("@notrelix/features-billing", [
      "@notrelix/contracts",
      "@notrelix/kernel",
      "@notrelix/platform",
      "@notrelix/query",
      "@notrelix/ui-web",
    ]);
    expectExact("@notrelix/features-integrations", [
      "@notrelix/contracts",
      "@notrelix/kernel",
      "@notrelix/platform",
      "@notrelix/query",
      "@notrelix/ui-web",
    ]);
    expectExact("@notrelix/features-governance", [
      "@notrelix/contracts",
      "@notrelix/kernel",
      "@notrelix/platform",
      "@notrelix/query",
      "@notrelix/ui-web",
    ]);
    expectExact("@notrelix/features-search", [
      "@notrelix/contracts",
      "@notrelix/kernel",
      "@notrelix/platform",
      "@notrelix/query",
      "@notrelix/ui-web",
    ]);
    expectExact("@notrelix/features-activity", [
      "@notrelix/contracts",
      "@notrelix/kernel",
      "@notrelix/platform",
      "@notrelix/query",
    ]);
    expectExact("@notrelix/features-collaboration", [
      "@notrelix/contracts",
      "@notrelix/kernel",
      "@notrelix/platform",
      "@notrelix/query",
      "@notrelix/ui-web",
      "@notrelix/realtime",
    ]);
    expectExact("@notrelix/features-notifications", [
      "@notrelix/contracts",
      "@notrelix/kernel",
      "@notrelix/platform",
      "@notrelix/query",
      "@notrelix/ui-web",
      "@notrelix/realtime",
    ]);
  });

  it("ARCH-017 runtime-mobile edges are contracts/kernel/platform/query/realtime/observability", () => {
    const runtimeMobile = ARCHITECTURE_POLICY_BY_PACKAGE.get(
      "@notrelix/runtime-mobile",
    );
    expect(runtimeMobile).toBeDefined();
    expect([...runtimeMobile!.allowedInternalImports].sort()).toEqual(
      [
        "@notrelix/contracts",
        "@notrelix/kernel",
        "@notrelix/observability",
        "@notrelix/platform",
        "@notrelix/query",
        "@notrelix/realtime",
      ].sort(),
    );
  });

  it("ARCH-018 app-mobile edges match the structural contract", () => {
    const appMobile = ARCHITECTURE_POLICY_BY_PACKAGE.get(
      "@notrelix/app-mobile",
    );
    expect(appMobile).toBeDefined();
    expect([...appMobile!.allowedInternalImports].sort()).toEqual(
      [
        "@notrelix/runtime-mobile",
        "@notrelix/query",
        "@notrelix/ui-mobile",
        "@notrelix/ui-tokens",
        "@notrelix/work-management-mobile",
        "@notrelix/docs-mobile",
        "@notrelix/automation-mobile",
      ].sort(),
    );
  });
});

describe("layer policy validation", () => {
  const layerOf = (packageName: string) =>
    ARCHITECTURE_POLICY_BY_PACKAGE.get(packageName)?.layer;

  it("foundation packages cannot allow runtime/ui/product/feature/app packages", () => {
    for (const entry of ARCHITECTURE_MANIFEST) {
      if (entry.layer !== "foundation") continue;
      for (const target of entry.allowedInternalImports) {
        expect(["foundation"], `${entry.packageName} -> ${target}`).toContain(
          layerOf(target),
        );
      }
    }
  });

  it("ui packages cannot allow product/feature/app packages", () => {
    for (const entry of ARCHITECTURE_MANIFEST) {
      if (entry.layer !== "ui") continue;
      for (const target of entry.allowedInternalImports) {
        expect(
          ["ui", "foundation"],
          `${entry.packageName} -> ${target}`,
        ).toContain(layerOf(target));
      }
    }
  });

  it("product core cannot allow state/adapter/mobile/testing packages", () => {
    const forbidden = new Set([
      "product-state",
      "product-collaboration",
      "product-plugin",
      "product-adapter",
      "product-testing",
    ]);
    for (const entry of ARCHITECTURE_MANIFEST) {
      if (entry.layer !== "product-core") continue;
      for (const target of entry.allowedInternalImports) {
        expect(
          forbidden.has(layerOf(target) ?? ""),
          `${entry.packageName} -> ${target}`,
        ).toBe(false);
      }
    }
  });

  it("product state cannot allow UI implementation packages", () => {
    const uiImplementation = new Set([
      "@notrelix/ui-web",
      "@notrelix/ui-mobile",
    ]);
    for (const entry of ARCHITECTURE_MANIFEST) {
      if (entry.layer !== "product-state") continue;
      for (const target of entry.allowedInternalImports) {
        expect(
          uiImplementation.has(target),
          `${entry.packageName} -> ${target}`,
        ).toBe(false);
      }
    }
  });

  it("product testing is never allowed from core-production or app entries", () => {
    const testingPackages: Set<string> = new Set(
      ARCHITECTURE_MANIFEST.filter(
        (entry) => entry.layer === "product-testing",
      ).map((entry) => entry.packageName),
    );
    for (const entry of ARCHITECTURE_MANIFEST) {
      if (entry.layer === "product-testing") continue;
      for (const target of entry.allowedInternalImports) {
        expect(
          testingPackages.has(target),
          `${entry.packageName} -> ${target}`,
        ).toBe(false);
      }
    }
  });
});

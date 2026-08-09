/**
 * Closed-world architecture manifest — the single executable source of truth
 * for the Notrelix client package universe.
 *
 * Every directory under frontend/apps/** and frontend/packages/** that
 * contains a package.json MUST appear here exactly once. The checker preflight
 * enforces set equality; an unregistered package or a stale entry fails the
 * architecture gate.
 *
 * The exact package boundary table in the docs is generated from this file —
 * do not hand-maintain a second matrix.
 */

export type ArchitectureLayer =
  | "foundation"
  | "runtime"
  | "ui"
  | "product-core"
  | "product-state"
  | "product-collaboration"
  | "product-plugin"
  | "product-adapter"
  | "product-testing"
  | "feature"
  | "app";

/**
 * Freeze scope describes architecture coverage, not feature completeness:
 *
 * - `core-production` — every production package/app unit, including all
 *   mobile structural packages;
 * - `verification` — testing-only packages;
 * - `marketing-isolated` — the marketing app.
 */
export type FreezeScope =
  "core-production" | "verification" | "marketing-isolated";

export interface ArchitecturePackagePolicy {
  readonly packageName: string;
  readonly relativePath: string;
  readonly layer: ArchitectureLayer;
  readonly freezeScope: FreezeScope;
  readonly allowedInternalImports: readonly string[];
}

export type ManifestViolationCode =
  | "UNKNOWN_ALLOWED_IMPORT"
  | "SELF_IMPORT_POLICY"
  | "DUPLICATE_ALLOWED_IMPORT"
  | "DUPLICATE_PACKAGE_NAME"
  | "DUPLICATE_PACKAGE_PATH";

export interface ManifestViolation {
  readonly code: ManifestViolationCode;
  readonly packageName: string;
  readonly message: string;
}

/**
 * Feature roots get exact least-privilege allow-lists (A2). No shared base
 * list, no `ui-mobile` grant to feature roots in this phase.
 */
const FEATURE_WEB_BASE = [
  "@notrelix/contracts",
  "@notrelix/kernel",
  "@notrelix/platform",
  "@notrelix/query",
  "@notrelix/ui-web",
] as const;

const FEATURE_WEB_REALTIME = [
  "@notrelix/contracts",
  "@notrelix/kernel",
  "@notrelix/platform",
  "@notrelix/query",
  "@notrelix/ui-web",
  "@notrelix/realtime",
] as const;

const FEATURE_WEB_NO_UI = [
  "@notrelix/contracts",
  "@notrelix/kernel",
  "@notrelix/platform",
  "@notrelix/query",
] as const;

export const ARCHITECTURE_MANIFEST = [
  // ── Foundation ──────────────────────────────────────────────────────
  {
    packageName: "@notrelix/contracts",
    relativePath: "packages/foundation/contracts",
    layer: "foundation",
    freezeScope: "core-production",
    allowedInternalImports: ["@notrelix/kernel"],
  },
  {
    packageName: "@notrelix/kernel",
    relativePath: "packages/foundation/kernel",
    layer: "foundation",
    freezeScope: "core-production",
    allowedInternalImports: [],
  },
  {
    packageName: "@notrelix/platform",
    relativePath: "packages/foundation/platform",
    layer: "foundation",
    freezeScope: "core-production",
    allowedInternalImports: ["@notrelix/kernel", "@notrelix/contracts"],
  },
  {
    packageName: "@notrelix/query",
    relativePath: "packages/foundation/query",
    layer: "foundation",
    freezeScope: "core-production",
    allowedInternalImports: ["@notrelix/kernel"],
  },
  {
    packageName: "@notrelix/realtime",
    relativePath: "packages/foundation/realtime",
    layer: "foundation",
    freezeScope: "core-production",
    allowedInternalImports: ["@notrelix/kernel", "@notrelix/contracts"],
  },
  {
    packageName: "@notrelix/observability",
    relativePath: "packages/foundation/observability",
    layer: "foundation",
    freezeScope: "core-production",
    allowedInternalImports: ["@notrelix/kernel"],
  },

  // ── Runtimes ────────────────────────────────────────────────────────
  {
    packageName: "@notrelix/runtime-web",
    relativePath: "packages/runtimes/web",
    layer: "runtime",
    freezeScope: "core-production",
    allowedInternalImports: [
      "@notrelix/platform",
      "@notrelix/kernel",
      "@notrelix/contracts",
      "@notrelix/realtime",
      "@notrelix/observability",
    ],
  },
  {
    packageName: "@notrelix/runtime-mobile",
    relativePath: "packages/runtimes/mobile",
    layer: "runtime",
    freezeScope: "core-production",
    allowedInternalImports: [
      "@notrelix/kernel",
      "@notrelix/platform",
      "@notrelix/realtime",
      "@notrelix/observability",
    ],
  },

  // ── UI ──────────────────────────────────────────────────────────────
  {
    packageName: "@notrelix/ui-tokens",
    relativePath: "packages/ui/tokens",
    layer: "ui",
    freezeScope: "core-production",
    allowedInternalImports: [],
  },
  {
    packageName: "@notrelix/ui-web",
    relativePath: "packages/ui/web",
    layer: "ui",
    freezeScope: "core-production",
    allowedInternalImports: ["@notrelix/ui-tokens"],
  },
  {
    packageName: "@notrelix/ui-mobile",
    relativePath: "packages/ui/mobile",
    layer: "ui",
    freezeScope: "core-production",
    allowedInternalImports: ["@notrelix/ui-tokens"],
  },
  {
    packageName: "@notrelix/ui-icons",
    relativePath: "packages/ui/icons",
    layer: "ui",
    freezeScope: "core-production",
    allowedInternalImports: [],
  },

  // ── Product: Work Management ────────────────────────────────────────
  {
    packageName: "@notrelix/work-management-core",
    relativePath: "packages/product/work-management/core",
    layer: "product-core",
    freezeScope: "core-production",
    allowedInternalImports: ["@notrelix/contracts", "@notrelix/kernel"],
  },
  {
    packageName: "@notrelix/work-management-state",
    relativePath: "packages/product/work-management/state",
    layer: "product-state",
    freezeScope: "core-production",
    allowedInternalImports: [
      "@notrelix/work-management-core",
      "@notrelix/contracts",
      "@notrelix/query",
      "@notrelix/realtime",
      "@notrelix/platform",
    ],
  },
  {
    packageName: "@notrelix/work-management-plugins",
    relativePath: "packages/product/work-management/plugins",
    layer: "product-plugin",
    freezeScope: "core-production",
    allowedInternalImports: ["@notrelix/work-management-core"],
  },
  {
    packageName: "@notrelix/work-management-web",
    relativePath: "packages/product/work-management/web",
    layer: "product-adapter",
    freezeScope: "core-production",
    allowedInternalImports: [
      "@notrelix/work-management-core",
      "@notrelix/work-management-state",
      "@notrelix/work-management-plugins",
      "@notrelix/ui-web",
      "@notrelix/platform",
    ],
  },
  {
    packageName: "@notrelix/work-management-mobile",
    relativePath: "packages/product/work-management/mobile",
    layer: "product-adapter",
    freezeScope: "core-production",
    allowedInternalImports: [
      "@notrelix/work-management-core",
      "@notrelix/work-management-state",
      "@notrelix/work-management-plugins",
      "@notrelix/ui-mobile",
      "@notrelix/platform",
    ],
  },
  {
    packageName: "@notrelix/work-management-testing",
    relativePath: "packages/product/work-management/testing",
    layer: "product-testing",
    freezeScope: "verification",
    allowedInternalImports: [
      "@notrelix/work-management-core",
      "@notrelix/work-management-state",
    ],
  },

  // ── Product: Docs ──────────────────────────────────────────────────
  {
    packageName: "@notrelix/docs-core",
    relativePath: "packages/product/docs/core",
    layer: "product-core",
    freezeScope: "core-production",
    allowedInternalImports: ["@notrelix/contracts", "@notrelix/kernel"],
  },
  {
    packageName: "@notrelix/docs-state",
    relativePath: "packages/product/docs/state",
    layer: "product-state",
    freezeScope: "core-production",
    allowedInternalImports: [
      "@notrelix/docs-core",
      "@notrelix/contracts",
      "@notrelix/query",
      "@notrelix/kernel",
    ],
  },
  {
    packageName: "@notrelix/docs-collaboration",
    relativePath: "packages/product/docs/collaboration",
    layer: "product-collaboration",
    freezeScope: "core-production",
    allowedInternalImports: ["@notrelix/docs-core", "@notrelix/realtime"],
  },
  {
    packageName: "@notrelix/docs-web",
    relativePath: "packages/product/docs/web",
    layer: "product-adapter",
    freezeScope: "core-production",
    allowedInternalImports: [
      "@notrelix/docs-core",
      "@notrelix/docs-state",
      "@notrelix/docs-collaboration",
      "@notrelix/ui-web",
      "@notrelix/platform",
    ],
  },
  {
    packageName: "@notrelix/docs-mobile",
    relativePath: "packages/product/docs/mobile",
    layer: "product-adapter",
    freezeScope: "core-production",
    allowedInternalImports: [
      "@notrelix/docs-core",
      "@notrelix/docs-collaboration",
      "@notrelix/ui-mobile",
      "@notrelix/platform",
    ],
  },

  // ── Product: Automation ────────────────────────────────────────────
  {
    packageName: "@notrelix/automation-core",
    relativePath: "packages/product/automation/core",
    layer: "product-core",
    freezeScope: "core-production",
    allowedInternalImports: ["@notrelix/contracts", "@notrelix/kernel"],
  },
  {
    packageName: "@notrelix/automation-state",
    relativePath: "packages/product/automation/state",
    layer: "product-state",
    freezeScope: "core-production",
    allowedInternalImports: [
      "@notrelix/automation-core",
      "@notrelix/query",
      "@notrelix/realtime",
    ],
  },
  {
    packageName: "@notrelix/automation-web",
    relativePath: "packages/product/automation/web",
    layer: "product-adapter",
    freezeScope: "core-production",
    allowedInternalImports: [
      "@notrelix/automation-core",
      "@notrelix/ui-web",
      "@notrelix/platform",
    ],
  },
  {
    packageName: "@notrelix/automation-mobile",
    relativePath: "packages/product/automation/mobile",
    layer: "product-adapter",
    freezeScope: "core-production",
    allowedInternalImports: [
      "@notrelix/automation-core",
      "@notrelix/ui-mobile",
      "@notrelix/platform",
    ],
  },
  {
    packageName: "@notrelix/automation-testing",
    relativePath: "packages/product/automation/testing",
    layer: "product-testing",
    freezeScope: "verification",
    allowedInternalImports: [
      "@notrelix/automation-core",
      "@notrelix/automation-state",
      "@notrelix/realtime",
    ],
  },

  // ── Features ───────────────────────────────────────────────────────
  {
    packageName: "@notrelix/features-auth",
    relativePath: "packages/features/auth",
    layer: "feature",
    freezeScope: "core-production",
    allowedInternalImports: [...FEATURE_WEB_BASE],
  },
  {
    packageName: "@notrelix/features-workspace",
    relativePath: "packages/features/workspace",
    layer: "feature",
    freezeScope: "core-production",
    allowedInternalImports: [...FEATURE_WEB_BASE],
  },
  {
    packageName: "@notrelix/features-account",
    relativePath: "packages/features/account",
    layer: "feature",
    freezeScope: "core-production",
    allowedInternalImports: [...FEATURE_WEB_BASE],
  },
  {
    packageName: "@notrelix/features-billing",
    relativePath: "packages/features/billing",
    layer: "feature",
    freezeScope: "core-production",
    allowedInternalImports: [...FEATURE_WEB_BASE],
  },
  {
    packageName: "@notrelix/features-integrations",
    relativePath: "packages/features/integrations",
    layer: "feature",
    freezeScope: "core-production",
    allowedInternalImports: [...FEATURE_WEB_BASE],
  },
  {
    packageName: "@notrelix/features-notifications",
    relativePath: "packages/features/notifications",
    layer: "feature",
    freezeScope: "core-production",
    allowedInternalImports: [...FEATURE_WEB_REALTIME],
  },
  {
    packageName: "@notrelix/features-activity",
    relativePath: "packages/features/activity",
    layer: "feature",
    freezeScope: "core-production",
    allowedInternalImports: [...FEATURE_WEB_NO_UI],
  },
  {
    packageName: "@notrelix/features-governance",
    relativePath: "packages/features/governance",
    layer: "feature",
    freezeScope: "core-production",
    allowedInternalImports: [...FEATURE_WEB_BASE],
  },
  {
    packageName: "@notrelix/features-search",
    relativePath: "packages/features/search",
    layer: "feature",
    freezeScope: "core-production",
    allowedInternalImports: [...FEATURE_WEB_BASE],
  },
  {
    packageName: "@notrelix/features-collaboration",
    relativePath: "packages/features/collaboration",
    layer: "feature",
    freezeScope: "core-production",
    allowedInternalImports: [...FEATURE_WEB_REALTIME],
  },

  // ── Apps ───────────────────────────────────────────────────────────
  {
    packageName: "@notrelix/app-web",
    relativePath: "apps/web",
    layer: "app",
    freezeScope: "core-production",
    allowedInternalImports: [
      "@notrelix/kernel",
      "@notrelix/contracts",
      "@notrelix/platform",
      "@notrelix/query",
      "@notrelix/realtime",
      "@notrelix/observability",
      "@notrelix/runtime-web",
      "@notrelix/ui-tokens",
      "@notrelix/ui-web",
      "@notrelix/ui-icons",
      "@notrelix/work-management-core",
      "@notrelix/work-management-state",
      "@notrelix/work-management-plugins",
      "@notrelix/work-management-web",
      "@notrelix/docs-core",
      "@notrelix/docs-state",
      "@notrelix/docs-collaboration",
      "@notrelix/docs-web",
      "@notrelix/automation-core",
      "@notrelix/automation-web",
      "@notrelix/features-auth",
      "@notrelix/features-workspace",
      "@notrelix/features-account",
      "@notrelix/features-billing",
      "@notrelix/features-integrations",
      "@notrelix/features-notifications",
      "@notrelix/features-activity",
      "@notrelix/features-governance",
      "@notrelix/features-search",
      "@notrelix/features-collaboration",
    ],
  },
  {
    packageName: "@notrelix/app-marketing",
    relativePath: "apps/marketing",
    layer: "app",
    freezeScope: "marketing-isolated",
    allowedInternalImports: [
      "@notrelix/ui-tokens",
      "@notrelix/ui-web",
      "@notrelix/ui-icons",
    ],
  },
  {
    packageName: "@notrelix/app-mobile",
    relativePath: "apps/mobile",
    layer: "app",
    freezeScope: "core-production",
    allowedInternalImports: [
      "@notrelix/runtime-mobile",
      "@notrelix/query",
      "@notrelix/ui-mobile",
      "@notrelix/ui-tokens",
      "@notrelix/work-management-mobile",
      "@notrelix/docs-mobile",
      "@notrelix/automation-mobile",
    ],
  },
] as const satisfies readonly ArchitecturePackagePolicy[];

export const ARCHITECTURE_POLICY_BY_PACKAGE = new Map<
  string,
  ArchitecturePackagePolicy
>(ARCHITECTURE_MANIFEST.map((entry) => [entry.packageName, entry] as const));

/**
 * Pure manifest-graph validation. These codes describe defects in the
 * manifest itself (not in workspace sources) and are checked before any
 * import scan runs.
 */
export function validateArchitectureManifest(
  manifest: readonly ArchitecturePackagePolicy[],
): ManifestViolation[] {
  const violations: ManifestViolation[] = [];
  const known = new Set(manifest.map((entry) => entry.packageName));
  const seenNames = new Set<string>();
  const seenPaths = new Set<string>();

  for (const entry of manifest) {
    if (seenNames.has(entry.packageName)) {
      violations.push({
        code: "DUPLICATE_PACKAGE_NAME",
        packageName: entry.packageName,
        message: `duplicate manifest entry for package "${entry.packageName}"`,
      });
    }
    seenNames.add(entry.packageName);

    if (seenPaths.has(entry.relativePath)) {
      violations.push({
        code: "DUPLICATE_PACKAGE_PATH",
        packageName: entry.packageName,
        message: `duplicate manifest path "${entry.relativePath}"`,
      });
    }
    seenPaths.add(entry.relativePath);

    const seenTargets = new Set<string>();
    for (const target of entry.allowedInternalImports) {
      if (!known.has(target)) {
        violations.push({
          code: "UNKNOWN_ALLOWED_IMPORT",
          packageName: entry.packageName,
          message: `allowed import target "${target}" is not a manifest package`,
        });
      }
      if (target === entry.packageName) {
        violations.push({
          code: "SELF_IMPORT_POLICY",
          packageName: entry.packageName,
          message: `package "${entry.packageName}" allows itself`,
        });
      }
      if (seenTargets.has(target)) {
        violations.push({
          code: "DUPLICATE_ALLOWED_IMPORT",
          packageName: entry.packageName,
          message: `allowed import target "${target}" appears more than once`,
        });
      }
      seenTargets.add(target);
    }
  }

  return violations;
}

/**
 * Allowed imports per package — defines which internal packages each
 * package is architecturally permitted to depend on.
 *
 * Source of truth: docs/notrelix-client-technical-project-structure.md §10
 */
export const ALLOWED_IMPORTS: Record<string, string[]> = {
  // ── Foundation ──────────────────────────────────────────────────────
  "@notrelix/contracts": ["@notrelix/kernel"],
  "@notrelix/kernel": [],
  "@notrelix/platform": ["@notrelix/kernel", "@notrelix/contracts"],
  "@notrelix/query": ["@notrelix/kernel"],
  "@notrelix/realtime": ["@notrelix/kernel", "@notrelix/contracts"],
  "@notrelix/observability": ["@notrelix/kernel"],

  // ── Runtimes ────────────────────────────────────────────────────────
  "@notrelix/runtime-web": ["@notrelix/platform", "@notrelix/kernel", "@notrelix/contracts"],
  "@notrelix/runtime-mobile": ["@notrelix/platform", "@notrelix/kernel", "@notrelix/contracts"],

  // ── UI ──────────────────────────────────────────────────────────────
  "@notrelix/ui-tokens": [],
  "@notrelix/ui-web": ["@notrelix/ui-tokens"],
  "@notrelix/ui-mobile": ["@notrelix/ui-tokens"],
  "@notrelix/icons": [],

  // ── Product: Work Management ────────────────────────────────────────
  "@notrelix/work-management-core": ["@notrelix/contracts", "@notrelix/kernel"],
  "@notrelix/work-management-state": [
    "@notrelix/work-management-core",
    "@notrelix/contracts",
    "@notrelix/query",
    "@notrelix/realtime",
    "@notrelix/platform",
  ],
  "@notrelix/work-management-plugins": ["@notrelix/work-management-core"],
  "@notrelix/work-management-web": [
    "@notrelix/work-management-core",
    "@notrelix/work-management-state",
    "@notrelix/work-management-plugins",
    "@notrelix/ui-web",
    "@notrelix/platform",
  ],
  "@notrelix/work-management-mobile": [
    "@notrelix/work-management-core",
    "@notrelix/work-management-state",
    "@notrelix/work-management-plugins",
    "@notrelix/ui-mobile",
    "@notrelix/platform",
  ],
  "@notrelix/work-management-testing": [
    "@notrelix/work-management-core",
    "@notrelix/work-management-state",
  ],

  // ── Product: Docs ──────────────────────────────────────────────────
  "@notrelix/docs-core": ["@notrelix/contracts", "@notrelix/kernel"],
  "@notrelix/docs-collaboration": ["@notrelix/docs-core", "@notrelix/realtime"],
  "@notrelix/docs-web": [
    "@notrelix/docs-core",
    "@notrelix/docs-collaboration",
    "@notrelix/ui-web",
    "@notrelix/platform",
  ],
  "@notrelix/docs-mobile": [
    "@notrelix/docs-core",
    "@notrelix/docs-collaboration",
    "@notrelix/ui-mobile",
    "@notrelix/platform",
  ],

  // ── Product: Automation ────────────────────────────────────────────
  "@notrelix/automation-core": ["@notrelix/contracts", "@notrelix/kernel"],
  "@notrelix/automation-web": [
    "@notrelix/automation-core",
    "@notrelix/ui-web",
    "@notrelix/platform",
  ],
  "@notrelix/automation-mobile": [
    "@notrelix/automation-core",
    "@notrelix/ui-mobile",
    "@notrelix/platform",
  ],

  // ── Features ───────────────────────────────────────────────────────
  "@notrelix/features-auth": [
    "@notrelix/contracts", "@notrelix/kernel", "@notrelix/platform",
    "@notrelix/query", "@notrelix/ui-web", "@notrelix/ui-mobile",
  ],
  "@notrelix/features-workspace": [
    "@notrelix/contracts", "@notrelix/kernel", "@notrelix/platform",
    "@notrelix/query", "@notrelix/ui-web", "@notrelix/ui-mobile",
  ],
  "@notrelix/features-account": [
    "@notrelix/contracts", "@notrelix/kernel", "@notrelix/platform",
    "@notrelix/query", "@notrelix/ui-web", "@notrelix/ui-mobile",
  ],
  "@notrelix/features-billing": [
    "@notrelix/contracts", "@notrelix/kernel", "@notrelix/platform",
    "@notrelix/query", "@notrelix/ui-web", "@notrelix/ui-mobile",
  ],
  "@notrelix/features-integrations": [
    "@notrelix/contracts", "@notrelix/kernel", "@notrelix/platform",
    "@notrelix/query", "@notrelix/ui-web", "@notrelix/ui-mobile",
  ],
  "@notrelix/features-notifications": [
    "@notrelix/contracts", "@notrelix/kernel", "@notrelix/platform",
    "@notrelix/query", "@notrelix/realtime", "@notrelix/ui-web", "@notrelix/ui-mobile",
  ],
  "@notrelix/features-activity": [
    "@notrelix/contracts", "@notrelix/kernel", "@notrelix/platform",
    "@notrelix/query", "@notrelix/ui-web", "@notrelix/ui-mobile",
  ],
  "@notrelix/features-governance": [
    "@notrelix/contracts", "@notrelix/kernel", "@notrelix/platform",
    "@notrelix/query", "@notrelix/ui-web", "@notrelix/ui-mobile",
  ],
  "@notrelix/features-search": [
    "@notrelix/contracts", "@notrelix/kernel", "@notrelix/platform",
    "@notrelix/query", "@notrelix/ui-web", "@notrelix/ui-mobile",
  ],
  "@notrelix/features-collaboration": [
    "@notrelix/contracts", "@notrelix/kernel", "@notrelix/platform",
    "@notrelix/query", "@notrelix/realtime", "@notrelix/ui-web", "@notrelix/ui-mobile",
  ],

  // ── Apps ───────────────────────────────────────────────────────────
  "@notrelix/app-web": [
    "@notrelix/kernel", "@notrelix/contracts", "@notrelix/platform",
    "@notrelix/query", "@notrelix/realtime", "@notrelix/observability",
    "@notrelix/runtime-web", "@notrelix/ui-tokens", "@notrelix/ui-web",
    "@notrelix/icons", "@notrelix/work-management-core", "@notrelix/work-management-state",
    "@notrelix/work-management-plugins", "@notrelix/work-management-web",
    "@notrelix/docs-core", "@notrelix/docs-collaboration", "@notrelix/docs-web",
    "@notrelix/automation-core", "@notrelix/automation-web",
    "@notrelix/features-auth", "@notrelix/features-workspace",
    "@notrelix/features-account", "@notrelix/features-billing",
    "@notrelix/features-integrations", "@notrelix/features-notifications",
    "@notrelix/features-activity", "@notrelix/features-governance",
    "@notrelix/features-search", "@notrelix/features-collaboration",
  ],
  "@notrelix/app-marketing": [
    "@notrelix/ui-tokens", "@notrelix/ui-web", "@notrelix/icons",
  ],
  "@notrelix/app-mobile": [
    "@notrelix/kernel", "@notrelix/platform", "@notrelix/query",
    "@notrelix/runtime-mobile", "@notrelix/ui-tokens", "@notrelix/ui-mobile",
    "@notrelix/work-management-mobile",
  ],
}

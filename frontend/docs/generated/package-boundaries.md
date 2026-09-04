---
document_id: FE-GEN-PACKAGE-BOUNDARIES
document_type: generated
status: generated
owner: frontend-architecture
applies_to:
  - frontend-package-graph
  - frontend-import-boundaries
  - frontend-architecture-evidence
evidence:
  - tooling/dependency-rules/src/architecture-manifest.ts
  - tooling/dependency-rules/src/generate-architecture-docs.ts
review_on:
  - architecture-manifest-change
  - package-layer-change
  - package-freeze-scope-change
  - package-allowed-import-change
  - package-boundary-generator-change
---

# Notrelix Frontend — Package Boundaries

<!-- GENERATED FILE — DO NOT EDIT. -->
<!-- Source of truth: tooling/dependency-rules/src/architecture-manifest.ts -->
<!-- Producer: tooling/dependency-rules/src/generate-architecture-docs.ts -->
<!-- Regenerate: pnpm --filter @notrelix/dependency-rules docs:generate -->
<!-- Check drift: pnpm --filter @notrelix/dependency-rules docs:check -->

> This file is generated evidence. It is not the semantic architecture owner.
> Read `../architecture/dependency-boundaries.md` for package-boundary meaning and policy.

Package count: 42

| Relative path | Package | Layer | Freeze scope | Allowed internal imports | Verification-only internal imports |
|:---|:---|:---|:---|:---|:---|
| `apps/marketing` | `@notrelix/app-marketing` | `app` | `marketing-isolated` | `@notrelix/ui-tokens`, `@notrelix/ui-web`, `@notrelix/ui-icons` | _(none)_ |
| `apps/mobile` | `@notrelix/app-mobile` | `app` | `core-production` | `@notrelix/runtime-mobile`, `@notrelix/query`, `@notrelix/ui-mobile`, `@notrelix/ui-tokens`, `@notrelix/work-management-mobile`, `@notrelix/docs-mobile`, `@notrelix/automation-mobile` | _(none)_ |
| `apps/web` | `@notrelix/app-web` | `app` | `core-production` | `@notrelix/kernel`, `@notrelix/contracts`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/realtime`, `@notrelix/observability`, `@notrelix/runtime-web`, `@notrelix/ui-tokens`, `@notrelix/ui-web`, `@notrelix/ui-icons`, `@notrelix/work-management-core`, `@notrelix/work-management-state`, `@notrelix/work-management-plugins`, `@notrelix/work-management-web`, `@notrelix/docs-core`, `@notrelix/docs-state`, `@notrelix/docs-collaboration`, `@notrelix/docs-web`, `@notrelix/automation-core`, `@notrelix/automation-web`, `@notrelix/features-auth`, `@notrelix/features-workspace`, `@notrelix/features-account`, `@notrelix/features-billing`, `@notrelix/features-integrations`, `@notrelix/features-notifications`, `@notrelix/features-activity`, `@notrelix/features-governance`, `@notrelix/features-search`, `@notrelix/features-collaboration`, `@notrelix/dev-mock-backend` | _(none)_ |
| `packages/dev/mock-backend` | `@notrelix/dev-mock-backend` | `dev-support` | `verification` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/realtime`, `@notrelix/features-auth`, `@notrelix/features-workspace`, `@notrelix/work-management-core` | _(none)_ |
| `packages/features/account` | `@notrelix/features-account` | `feature` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web` | _(none)_ |
| `packages/features/activity` | `@notrelix/features-activity` | `feature` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query` | _(none)_ |
| `packages/features/auth` | `@notrelix/features-auth` | `feature` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web` | `@notrelix/testing` |
| `packages/features/billing` | `@notrelix/features-billing` | `feature` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web` | `@notrelix/testing` |
| `packages/features/collaboration` | `@notrelix/features-collaboration` | `feature` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web`, `@notrelix/realtime` | `@notrelix/testing` |
| `packages/features/governance` | `@notrelix/features-governance` | `feature` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web` | _(none)_ |
| `packages/features/integrations` | `@notrelix/features-integrations` | `feature` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web` | _(none)_ |
| `packages/features/notifications` | `@notrelix/features-notifications` | `feature` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web`, `@notrelix/realtime` | `@notrelix/testing` |
| `packages/features/search` | `@notrelix/features-search` | `feature` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web` | `@notrelix/testing` |
| `packages/features/workspace` | `@notrelix/features-workspace` | `feature` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web` | `@notrelix/testing` |
| `packages/foundation/contracts` | `@notrelix/contracts` | `foundation` | `core-production` | `@notrelix/kernel` | _(none)_ |
| `packages/foundation/kernel` | `@notrelix/kernel` | `foundation` | `core-production` | _(none)_ | _(none)_ |
| `packages/foundation/observability` | `@notrelix/observability` | `foundation` | `core-production` | `@notrelix/kernel` | _(none)_ |
| `packages/foundation/platform` | `@notrelix/platform` | `foundation` | `core-production` | `@notrelix/kernel`, `@notrelix/contracts` | _(none)_ |
| `packages/foundation/query` | `@notrelix/query` | `foundation` | `core-production` | `@notrelix/kernel` | _(none)_ |
| `packages/foundation/realtime` | `@notrelix/realtime` | `foundation` | `core-production` | `@notrelix/kernel`, `@notrelix/contracts` | _(none)_ |
| `packages/product/automation/core` | `@notrelix/automation-core` | `product-core` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel` | _(none)_ |
| `packages/product/automation/mobile` | `@notrelix/automation-mobile` | `product-adapter` | `core-production` | `@notrelix/automation-core`, `@notrelix/ui-mobile`, `@notrelix/platform` | _(none)_ |
| `packages/product/automation/state` | `@notrelix/automation-state` | `product-state` | `core-production` | `@notrelix/automation-core`, `@notrelix/query`, `@notrelix/realtime` | _(none)_ |
| `packages/product/automation/testing` | `@notrelix/automation-testing` | `product-testing` | `verification` | `@notrelix/automation-core`, `@notrelix/automation-state`, `@notrelix/realtime` | _(none)_ |
| `packages/product/automation/web` | `@notrelix/automation-web` | `product-adapter` | `core-production` | `@notrelix/automation-core`, `@notrelix/ui-web`, `@notrelix/platform` | `@notrelix/automation-testing`, `@notrelix/testing` |
| `packages/product/docs/collaboration` | `@notrelix/docs-collaboration` | `product-collaboration` | `core-production` | `@notrelix/docs-core`, `@notrelix/realtime` | _(none)_ |
| `packages/product/docs/core` | `@notrelix/docs-core` | `product-core` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel` | _(none)_ |
| `packages/product/docs/mobile` | `@notrelix/docs-mobile` | `product-adapter` | `core-production` | `@notrelix/docs-core`, `@notrelix/docs-collaboration`, `@notrelix/ui-mobile`, `@notrelix/platform` | _(none)_ |
| `packages/product/docs/state` | `@notrelix/docs-state` | `product-state` | `core-production` | `@notrelix/docs-core`, `@notrelix/contracts`, `@notrelix/query`, `@notrelix/kernel` | _(none)_ |
| `packages/product/docs/web` | `@notrelix/docs-web` | `product-adapter` | `core-production` | `@notrelix/docs-core`, `@notrelix/docs-state`, `@notrelix/docs-collaboration`, `@notrelix/ui-web`, `@notrelix/platform` | `@notrelix/testing` |
| `packages/product/work-management/core` | `@notrelix/work-management-core` | `product-core` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel` | _(none)_ |
| `packages/product/work-management/mobile` | `@notrelix/work-management-mobile` | `product-adapter` | `core-production` | `@notrelix/work-management-core`, `@notrelix/work-management-state`, `@notrelix/work-management-plugins`, `@notrelix/ui-mobile`, `@notrelix/platform` | _(none)_ |
| `packages/product/work-management/plugins` | `@notrelix/work-management-plugins` | `product-plugin` | `core-production` | `@notrelix/work-management-core` | _(none)_ |
| `packages/product/work-management/state` | `@notrelix/work-management-state` | `product-state` | `core-production` | `@notrelix/work-management-core`, `@notrelix/contracts`, `@notrelix/query`, `@notrelix/realtime`, `@notrelix/platform` | _(none)_ |
| `packages/product/work-management/testing` | `@notrelix/work-management-testing` | `product-testing` | `verification` | `@notrelix/work-management-core` | _(none)_ |
| `packages/product/work-management/web` | `@notrelix/work-management-web` | `product-adapter` | `core-production` | `@notrelix/work-management-core`, `@notrelix/work-management-state`, `@notrelix/work-management-plugins`, `@notrelix/ui-web`, `@notrelix/platform` | `@notrelix/testing`, `@notrelix/work-management-testing` |
| `packages/runtimes/mobile` | `@notrelix/runtime-mobile` | `runtime` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/realtime`, `@notrelix/observability` | _(none)_ |
| `packages/runtimes/web` | `@notrelix/runtime-web` | `runtime` | `core-production` | `@notrelix/platform`, `@notrelix/kernel`, `@notrelix/contracts`, `@notrelix/realtime`, `@notrelix/observability` | _(none)_ |
| `packages/ui/icons` | `@notrelix/ui-icons` | `ui` | `core-production` | _(none)_ | _(none)_ |
| `packages/ui/mobile` | `@notrelix/ui-mobile` | `ui` | `core-production` | `@notrelix/ui-tokens` | _(none)_ |
| `packages/ui/tokens` | `@notrelix/ui-tokens` | `ui` | `core-production` | _(none)_ | _(none)_ |
| `packages/ui/web` | `@notrelix/ui-web` | `ui` | `core-production` | `@notrelix/ui-tokens` | _(none)_ |

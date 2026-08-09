# Notrelix Client — Package Boundaries (generated)

<!-- GENERATED FILE — do not edit. -->
<!-- Source of truth: tooling/dependency-rules/src/architecture-manifest.ts -->
<!-- Regenerate: pnpm --filter @notrelix/dependency-rules docs:generate -->

Package count: 41

| Relative path | Package | Layer | Freeze scope | Allowed internal imports |
|:---|:---|:---|:---|:---|
| `apps/marketing` | `@notrelix/app-marketing` | `app` | `marketing-isolated` | `@notrelix/ui-tokens`, `@notrelix/ui-web`, `@notrelix/ui-icons` |
| `apps/mobile` | `@notrelix/app-mobile` | `app` | `core-production` | `@notrelix/runtime-mobile`, `@notrelix/query`, `@notrelix/ui-mobile`, `@notrelix/ui-tokens`, `@notrelix/work-management-mobile`, `@notrelix/docs-mobile`, `@notrelix/automation-mobile` |
| `apps/web` | `@notrelix/app-web` | `app` | `core-production` | `@notrelix/kernel`, `@notrelix/contracts`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/realtime`, `@notrelix/observability`, `@notrelix/runtime-web`, `@notrelix/ui-tokens`, `@notrelix/ui-web`, `@notrelix/ui-icons`, `@notrelix/work-management-core`, `@notrelix/work-management-state`, `@notrelix/work-management-plugins`, `@notrelix/work-management-web`, `@notrelix/docs-core`, `@notrelix/docs-state`, `@notrelix/docs-collaboration`, `@notrelix/docs-web`, `@notrelix/automation-core`, `@notrelix/automation-web`, `@notrelix/features-auth`, `@notrelix/features-workspace`, `@notrelix/features-account`, `@notrelix/features-billing`, `@notrelix/features-integrations`, `@notrelix/features-notifications`, `@notrelix/features-activity`, `@notrelix/features-governance`, `@notrelix/features-search`, `@notrelix/features-collaboration` |
| `packages/features/account` | `@notrelix/features-account` | `feature` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web` |
| `packages/features/activity` | `@notrelix/features-activity` | `feature` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query` |
| `packages/features/auth` | `@notrelix/features-auth` | `feature` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web` |
| `packages/features/billing` | `@notrelix/features-billing` | `feature` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web` |
| `packages/features/collaboration` | `@notrelix/features-collaboration` | `feature` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web`, `@notrelix/realtime` |
| `packages/features/governance` | `@notrelix/features-governance` | `feature` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web` |
| `packages/features/integrations` | `@notrelix/features-integrations` | `feature` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web` |
| `packages/features/notifications` | `@notrelix/features-notifications` | `feature` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web`, `@notrelix/realtime` |
| `packages/features/search` | `@notrelix/features-search` | `feature` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web` |
| `packages/features/workspace` | `@notrelix/features-workspace` | `feature` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web` |
| `packages/foundation/contracts` | `@notrelix/contracts` | `foundation` | `core-production` | `@notrelix/kernel` |
| `packages/foundation/kernel` | `@notrelix/kernel` | `foundation` | `core-production` | _(none)_ |
| `packages/foundation/observability` | `@notrelix/observability` | `foundation` | `core-production` | `@notrelix/kernel` |
| `packages/foundation/platform` | `@notrelix/platform` | `foundation` | `core-production` | `@notrelix/kernel`, `@notrelix/contracts` |
| `packages/foundation/query` | `@notrelix/query` | `foundation` | `core-production` | `@notrelix/kernel` |
| `packages/foundation/realtime` | `@notrelix/realtime` | `foundation` | `core-production` | `@notrelix/kernel`, `@notrelix/contracts` |
| `packages/product/automation/core` | `@notrelix/automation-core` | `product-core` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel` |
| `packages/product/automation/mobile` | `@notrelix/automation-mobile` | `product-adapter` | `core-production` | `@notrelix/automation-core`, `@notrelix/ui-mobile`, `@notrelix/platform` |
| `packages/product/automation/state` | `@notrelix/automation-state` | `product-state` | `core-production` | `@notrelix/automation-core`, `@notrelix/query`, `@notrelix/realtime` |
| `packages/product/automation/testing` | `@notrelix/automation-testing` | `product-testing` | `verification` | `@notrelix/automation-core`, `@notrelix/automation-state`, `@notrelix/realtime` |
| `packages/product/automation/web` | `@notrelix/automation-web` | `product-adapter` | `core-production` | `@notrelix/automation-core`, `@notrelix/ui-web`, `@notrelix/platform` |
| `packages/product/docs/collaboration` | `@notrelix/docs-collaboration` | `product-collaboration` | `core-production` | `@notrelix/docs-core`, `@notrelix/realtime` |
| `packages/product/docs/core` | `@notrelix/docs-core` | `product-core` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel` |
| `packages/product/docs/mobile` | `@notrelix/docs-mobile` | `product-adapter` | `core-production` | `@notrelix/docs-core`, `@notrelix/docs-collaboration`, `@notrelix/ui-mobile`, `@notrelix/platform` |
| `packages/product/docs/state` | `@notrelix/docs-state` | `product-state` | `core-production` | `@notrelix/docs-core`, `@notrelix/contracts`, `@notrelix/query`, `@notrelix/kernel` |
| `packages/product/docs/web` | `@notrelix/docs-web` | `product-adapter` | `core-production` | `@notrelix/docs-core`, `@notrelix/docs-state`, `@notrelix/docs-collaboration`, `@notrelix/ui-web`, `@notrelix/platform` |
| `packages/product/work-management/core` | `@notrelix/work-management-core` | `product-core` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel` |
| `packages/product/work-management/mobile` | `@notrelix/work-management-mobile` | `product-adapter` | `core-production` | `@notrelix/work-management-core`, `@notrelix/work-management-state`, `@notrelix/work-management-plugins`, `@notrelix/ui-mobile`, `@notrelix/platform` |
| `packages/product/work-management/plugins` | `@notrelix/work-management-plugins` | `product-plugin` | `core-production` | `@notrelix/work-management-core` |
| `packages/product/work-management/state` | `@notrelix/work-management-state` | `product-state` | `core-production` | `@notrelix/work-management-core`, `@notrelix/contracts`, `@notrelix/query`, `@notrelix/realtime`, `@notrelix/platform` |
| `packages/product/work-management/testing` | `@notrelix/work-management-testing` | `product-testing` | `verification` | `@notrelix/work-management-core`, `@notrelix/work-management-state` |
| `packages/product/work-management/web` | `@notrelix/work-management-web` | `product-adapter` | `core-production` | `@notrelix/work-management-core`, `@notrelix/work-management-state`, `@notrelix/work-management-plugins`, `@notrelix/ui-web`, `@notrelix/platform` |
| `packages/runtimes/mobile` | `@notrelix/runtime-mobile` | `runtime` | `core-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/realtime`, `@notrelix/observability` |
| `packages/runtimes/web` | `@notrelix/runtime-web` | `runtime` | `core-production` | `@notrelix/platform`, `@notrelix/kernel`, `@notrelix/contracts`, `@notrelix/realtime`, `@notrelix/observability` |
| `packages/ui/icons` | `@notrelix/ui-icons` | `ui` | `core-production` | _(none)_ |
| `packages/ui/mobile` | `@notrelix/ui-mobile` | `ui` | `core-production` | `@notrelix/ui-tokens` |
| `packages/ui/tokens` | `@notrelix/ui-tokens` | `ui` | `core-production` | _(none)_ |
| `packages/ui/web` | `@notrelix/ui-web` | `ui` | `core-production` | `@notrelix/ui-tokens` |

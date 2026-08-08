# Notrelix Client — Package Boundaries (generated)

<!-- GENERATED FILE — do not edit. -->
<!-- Source of truth: tooling/dependency-rules/src/architecture-manifest.ts -->
<!-- Regenerate: pnpm --filter @notrelix/dependency-rules docs:generate -->

Package count: 41

| Relative path | Package | Layer | Freeze scope | Allowed internal imports |
|:---|:---|:---|:---|:---|
| `apps/marketing` | `@notrelix/app-marketing` | `app` | `excluded-marketing-app` | `@notrelix/ui-tokens`, `@notrelix/ui-web`, `@notrelix/ui-icons` |
| `apps/mobile` | `@notrelix/app-mobile` | `app` | `excluded-mobile` | `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/runtime-mobile`, `@notrelix/ui-tokens`, `@notrelix/ui-mobile`, `@notrelix/work-management-mobile` |
| `apps/web` | `@notrelix/app-web` | `app` | `web-production` | `@notrelix/kernel`, `@notrelix/contracts`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/realtime`, `@notrelix/observability`, `@notrelix/runtime-web`, `@notrelix/ui-tokens`, `@notrelix/ui-web`, `@notrelix/ui-icons`, `@notrelix/work-management-core`, `@notrelix/work-management-state`, `@notrelix/work-management-plugins`, `@notrelix/work-management-web`, `@notrelix/docs-core`, `@notrelix/docs-state`, `@notrelix/docs-collaboration`, `@notrelix/docs-web`, `@notrelix/automation-core`, `@notrelix/automation-web`, `@notrelix/features-auth`, `@notrelix/features-workspace`, `@notrelix/features-account`, `@notrelix/features-billing`, `@notrelix/features-integrations`, `@notrelix/features-notifications`, `@notrelix/features-activity`, `@notrelix/features-governance`, `@notrelix/features-search`, `@notrelix/features-collaboration` |
| `packages/features/account` | `@notrelix/features-account` | `feature` | `web-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web`, `@notrelix/ui-mobile` |
| `packages/features/activity` | `@notrelix/features-activity` | `feature` | `web-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web`, `@notrelix/ui-mobile` |
| `packages/features/auth` | `@notrelix/features-auth` | `feature` | `web-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web`, `@notrelix/ui-mobile` |
| `packages/features/billing` | `@notrelix/features-billing` | `feature` | `web-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web`, `@notrelix/ui-mobile` |
| `packages/features/collaboration` | `@notrelix/features-collaboration` | `feature` | `web-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web`, `@notrelix/ui-mobile`, `@notrelix/realtime` |
| `packages/features/governance` | `@notrelix/features-governance` | `feature` | `web-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web`, `@notrelix/ui-mobile` |
| `packages/features/integrations` | `@notrelix/features-integrations` | `feature` | `web-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web`, `@notrelix/ui-mobile` |
| `packages/features/notifications` | `@notrelix/features-notifications` | `feature` | `web-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web`, `@notrelix/ui-mobile`, `@notrelix/realtime` |
| `packages/features/search` | `@notrelix/features-search` | `feature` | `web-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web`, `@notrelix/ui-mobile` |
| `packages/features/workspace` | `@notrelix/features-workspace` | `feature` | `web-production` | `@notrelix/contracts`, `@notrelix/kernel`, `@notrelix/platform`, `@notrelix/query`, `@notrelix/ui-web`, `@notrelix/ui-mobile` |
| `packages/foundation/contracts` | `@notrelix/contracts` | `foundation` | `web-production` | `@notrelix/kernel` |
| `packages/foundation/kernel` | `@notrelix/kernel` | `foundation` | `web-shared` | _(none)_ |
| `packages/foundation/observability` | `@notrelix/observability` | `foundation` | `web-shared` | `@notrelix/kernel` |
| `packages/foundation/platform` | `@notrelix/platform` | `foundation` | `web-shared` | `@notrelix/kernel`, `@notrelix/contracts` |
| `packages/foundation/query` | `@notrelix/query` | `foundation` | `web-shared` | `@notrelix/kernel` |
| `packages/foundation/realtime` | `@notrelix/realtime` | `foundation` | `web-shared` | `@notrelix/kernel`, `@notrelix/contracts` |
| `packages/product/automation/core` | `@notrelix/automation-core` | `product-core` | `web-production` | `@notrelix/contracts`, `@notrelix/kernel` |
| `packages/product/automation/mobile` | `@notrelix/automation-mobile` | `product-adapter` | `excluded-mobile` | `@notrelix/automation-core`, `@notrelix/ui-mobile`, `@notrelix/platform` |
| `packages/product/automation/state` | `@notrelix/automation-state` | `product-state` | `web-production` | `@notrelix/automation-core`, `@notrelix/query`, `@notrelix/realtime` |
| `packages/product/automation/testing` | `@notrelix/automation-testing` | `product-testing` | `web-verification` | `@notrelix/automation-core`, `@notrelix/automation-state`, `@notrelix/realtime` |
| `packages/product/automation/web` | `@notrelix/automation-web` | `product-adapter` | `web-production` | `@notrelix/automation-core`, `@notrelix/ui-web`, `@notrelix/platform` |
| `packages/product/docs/collaboration` | `@notrelix/docs-collaboration` | `product-collaboration` | `web-production` | `@notrelix/docs-core`, `@notrelix/realtime` |
| `packages/product/docs/core` | `@notrelix/docs-core` | `product-core` | `web-production` | `@notrelix/contracts`, `@notrelix/kernel` |
| `packages/product/docs/mobile` | `@notrelix/docs-mobile` | `product-adapter` | `excluded-mobile` | `@notrelix/docs-core`, `@notrelix/docs-collaboration`, `@notrelix/ui-mobile`, `@notrelix/platform` |
| `packages/product/docs/state` | `@notrelix/docs-state` | `product-state` | `web-production` | `@notrelix/docs-core`, `@notrelix/contracts`, `@notrelix/query`, `@notrelix/kernel` |
| `packages/product/docs/web` | `@notrelix/docs-web` | `product-adapter` | `web-production` | `@notrelix/docs-core`, `@notrelix/docs-state`, `@notrelix/docs-collaboration`, `@notrelix/ui-web`, `@notrelix/platform` |
| `packages/product/work-management/core` | `@notrelix/work-management-core` | `product-core` | `web-production` | `@notrelix/contracts`, `@notrelix/kernel` |
| `packages/product/work-management/mobile` | `@notrelix/work-management-mobile` | `product-adapter` | `excluded-mobile` | `@notrelix/work-management-core`, `@notrelix/work-management-state`, `@notrelix/work-management-plugins`, `@notrelix/ui-mobile`, `@notrelix/platform` |
| `packages/product/work-management/plugins` | `@notrelix/work-management-plugins` | `product-plugin` | `web-production` | `@notrelix/work-management-core` |
| `packages/product/work-management/state` | `@notrelix/work-management-state` | `product-state` | `web-production` | `@notrelix/work-management-core`, `@notrelix/contracts`, `@notrelix/query`, `@notrelix/realtime`, `@notrelix/platform` |
| `packages/product/work-management/testing` | `@notrelix/work-management-testing` | `product-testing` | `web-verification` | `@notrelix/work-management-core`, `@notrelix/work-management-state` |
| `packages/product/work-management/web` | `@notrelix/work-management-web` | `product-adapter` | `web-production` | `@notrelix/work-management-core`, `@notrelix/work-management-state`, `@notrelix/work-management-plugins`, `@notrelix/ui-web`, `@notrelix/platform` |
| `packages/runtimes/mobile` | `@notrelix/runtime-mobile` | `runtime` | `excluded-mobile` | `@notrelix/platform`, `@notrelix/kernel`, `@notrelix/contracts` |
| `packages/runtimes/web` | `@notrelix/runtime-web` | `runtime` | `web-production` | `@notrelix/platform`, `@notrelix/kernel`, `@notrelix/contracts`, `@notrelix/realtime`, `@notrelix/observability` |
| `packages/ui/icons` | `@notrelix/ui-icons` | `ui` | `web-shared` | _(none)_ |
| `packages/ui/mobile` | `@notrelix/ui-mobile` | `ui` | `excluded-mobile` | `@notrelix/ui-tokens` |
| `packages/ui/tokens` | `@notrelix/ui-tokens` | `ui` | `web-shared` | _(none)_ |
| `packages/ui/web` | `@notrelix/ui-web` | `ui` | `web-shared` | `@notrelix/ui-tokens` |

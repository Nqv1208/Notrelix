# Notrelix Frontend — Migration Tracker

**Last Updated:** 2026-07-12  
**Current Milestone:** M6 — Cleanup + Validation  
**Overall Progress:** ███████████████████░ 99%

---

## Phase Status Legend

```txt
⬜ Not started    🔄 In progress    ✅ Complete    ⏸️ Blocked    ❌ Skipped
```

---

## Milestone 1: Audit + Foundation + Boundaries

| Phase | Task | Status | Notes |
|:---:|:---|:---:|:---|
| 0 | Full frontend audit | ✅ | See `audits/current-frontend-audit.md` |
| 0 | Mobile readiness audit | ⬜ | Deferred — no mobile delivery priority |
| 1 | Package skeleton (v4 structure) | ✅ | 35 packages created with nested grouping |
| 1 | Package naming + exports | ✅ | All @notrelix/* scoped packages configured |
| 1 | Boundary baseline | ✅ | `tooling/dependency-rules` with allowed/forbidden matrix |
| 1 | Workspace config update | ✅ | pnpm-workspace.yaml updated for nested paths |
| 1 | App tsconfig paths update | ✅ | apps/app/tsconfig.json points to new package locations |

---

## Milestone 2: UI Layer Extraction

| Phase | Task | Status | Notes |
|:---:|:---|:---:|:---|
| 2 | Extract ui/tokens (cn, design tokens, CSS vars) | ✅ | colors, typography, spacing, radius, shadows, motion, semantic, themes extracted |
| 3 | Move shadcn/ui → packages/ui/web | ✅ | 52 components moved with relative imports |
| 3 | Move feedback components → packages/ui/web | ✅ | 6 components moved |
| 3 | Move layout components → packages/ui/web | ⬜ | Pending — no layout components found in audit |
| 3 | Move theme provider → packages/ui/web | ✅ | ThemeProvider + color theme hooks moved |
| 3 | Create ui/mobile skeleton | ⬜ | Deferred — mobile not a delivery priority |

---

## Milestone 3: Foundation Extraction

| Phase | Task | Status | Notes |
|:---:|:---|:---:|:---|
| 4a | Extract foundation/kernel | ✅ | AppError, env-schema, correlation-id, invariant, validation helpers |
| 4b | Extract foundation/contracts | ✅ | api-client, endpoints, CSRF, types |
| 4c | Extract foundation/platform | ✅ | permissions, auth, workspace, config, routes |
| 4d | Extract foundation/query | ✅ | query-client, shared defaults |
| 4e | Extract foundation/realtime | ✅ | typed events, reconnect, transport |
| 4f | Extract foundation/observability | ✅ | telemetry, metrics, logger |
| 5a | Create runtimes/web | ⬜ | Deferred to Milestone 5 |
| 5b | Create runtimes/mobile | ⬜ | Deferred to Milestone 5 |

---

## Milestone 4: Work Management Extraction

| Phase | Task | Status | Notes |
|:---:|:---|:---:|:---|
| 6 | Extract wm-core (models, validation, mappers) | ✅ | ~30 files |
| 7 | Extract wm-state (queries, mutations, API) | ✅ | ~40 files |
| 8 | Extract wm-plugins (field types) | ✅ | Logic-only definitions |
| 8 | Extract wm-web (views, components) | ✅ | ~60 files |
| 8 | Create wm-mobile skeleton | ✅ | Placeholder screens: board, item detail, workspace home |
| 8 | Create wm-testing package | ✅ | board/item/field fixtures, snapshot factory, mock command bus |
| 8 | Wire workspace deps in apps/app | ✅ | kernel, platform, contracts, query, realtime, observability, wm-* added |

---

## Milestone 5: Features + App Split

| Phase | Task | Status | Notes |
|:---:|:---|:---:|:---|
| 9a | Split docs product (core, collaboration, web) | ✅ | Core populated with types, DTOs, mappers, API contracts |
| 9b | Split automation product | ✅ | Core types + web component populated |
| 9c | Split normal features (auth, workspace, etc.) | ✅ | All 10 feature core packages populated with types, auth has API/schemas/utils |
| 9d | Split query keys by feature | ✅ | 12 feature-owned key files created, old god file replaced with re-exports |
| 10a | Extract apps/marketing (Next.js) | ✅ | Marketing v2 ở `/v2` và `/`, pricing, privacy, terms, contact pages |
| 10b | Create apps/web (Vite + TanStack Router) | ✅ | Auth, workspace, board, docs routes created |
| 10c | Create apps/mobile placeholder | ✅ | Expo skeleton with placeholder screen |

---

## Milestone 6: Cleanup + Validation

| Phase | Task | Status | Notes |
|:---:|:---|:---:|:---|
| 11 | Safe delete audit | ✅ | 4 WM mock files deleted, 52 UI components deleted, apps/app deleted |
| 11 | Delete approved legacy items | ✅ | All legacy items removed |
| 12 | Testing baseline | ✅ | Vitest configured, 273 test files, 1092 tests passing |
| 12 | Contracts + realtime hardening | ⬜ | OpenAPI codegen, typed events |
| 12 | Observability baseline | ⬜ | Telemetry events in dev |
| 12 | Final architecture validation | ✅ | apps/web created with Vite + TanStack Router |

### Marketing v2 validation snapshot

```txt
apps/marketing: lint ✅, typecheck ✅, production build ✅
frontend workspace: typecheck ✅, lint ✅, test ✅, check:deps ✅
visual smoke: desktop/mobile screenshot ✅, tab keyboard flow ✅, mobile scroll width ✅
```

---

## Decision Log

| # | Decision | Date | Rationale |
|:---:|:---|:---:|:---|
| 1 | apps/web uses Vite + React + TanStack Router | 2026-07-06 | Target per v4 design, better DX for SPA |
| 2 | apps/marketing uses Next.js App Router | 2026-07-06 | SEO/SSG/SSR requirements |
| 3 | Mobile is not delivery priority | 2026-07-06 | Mobile-readiness enforced via separation, not implementation |
| 4 | Parallel app operation during migration | 2026-07-06 | apps/app stays live until apps/web fully validated |
| 5 | OpenAPI contracts deferred if backend not ready | 2026-07-06 | Manual contracts first, codegen later |
| 6 | Start from M1 Foundation, not feature moves | 2026-07-06 | Foundation packages are dependency-free, lowest risk |
| 7 | Marketing v2 is canonical at `/` and available at `/v2` | 2026-07-12 | Keep an explicit preview route while making the public marketing entry point production-ready |

# FE-FZ-00 — Baseline Audit Document

## Metadata

| Field | Value |
|-------|-------|
| Date | 2026-07-26 |
| Commit | `ce469240` |
| Node | v24.15.0 |
| pnpm | 10.0.0 |
| OS | macOS |
| Repository | Notrelix frontend monorepo |

## Context

This document establishes the baseline state before the FE-FZ freeze series begins, per the plan in `.gemini/plans/frontend-freeze-completion-plan.md`.

## Current State Summary

### Already Completed (from prior work)
- **API singleton eviction (FE-FZ-04 to FE-FZ-08)**: `configureApi`, `activeBaseUrl`, and global `api` singleton fully deleted from `@notrelix/contracts`. All 16 consumer routes migrated to `useAppRuntime()`. MIGRATION_TRACKER.md shows 0 remaining consumers.
- **`main.tsx`**: Clean composition root using `createAppRuntime(import.meta.env)`, no `configureApi()` call.
- **RealtimeClient**: Heartbeat (30s) and eventId deduplication (LRU 1000 entries) added.
- **`pnpm typecheck`**: PASS — 0 errors across all 44 workspace packages.
- **`pnpm build`**: PASS — `app-web` (Vite) and `app-marketing` (Next.js 16) both build cleanly.

### Gaps Identified (to be addressed in subsequent PRs)

| PR | Gap |
|----|-----|
| FE-FZ-00 | `scripts/freeze-audit.mjs` missing ✅ Created |
| FE-FZ-00 | `docs/frontend-freeze/` missing ✅ Created |
| FE-FZ-01 | `vitest.workspace.ts` missing — vitest currently single config with `environment: "node"` for all tests |
| FE-FZ-01 | `tooling/testing/vitest.node.config.ts` missing |
| FE-FZ-01 | `tooling/testing/vitest.web.config.ts` missing |
| FE-FZ-01 | `tooling/testing/src/setup-web.ts` missing |
| FE-FZ-02 | `apps/web/src/config/read-runtime-environment.ts` missing — `main.tsx` passes raw `import.meta.env` to `createAppRuntime()` |
| FE-FZ-02 | Kernel `env-schema.ts` does not validate `realtimeUrl` in production |
| FE-FZ-03 | `AppRuntime` missing: `sessionEvents`, `dispose()` |
| FE-FZ-03 | `app-providers.tsx` creates `queryClient` at module-level (singleton) |
| FE-FZ-03 | `app-providers.tsx` calls `createAuthProvider()` inline on each render (no memoization → unstable identity) |
| FE-FZ-03 | No `session-lifecycle.tsx` provider |
| FE-FZ-09 | `executeOptimisticCommand` uses single generic `TData` and single `updateFn` for all keys — plan requires `defineOptimisticUpdate` per-key typed updater |
| FE-FZ-10 | `packages/realtime/src/protocol/` directory missing — no protocol layer |
| FE-FZ-11 | `RealtimeClient` lacks: constructor options injection (clock, timers, random), explicit state machine, `dispose()`, `WebSocketFactory` abstraction, online/offline handling, sequence gap detection |
| FE-FZ-12 | No `RealtimeLifecycle` component — runtime creates client but does not connect on auth |
| FE-FZ-13 | No standard `AppError` type; no `getUserFacingErrorMessage()` function |
| FE-FZ-14 | Architecture checker exists but does not guard all 8 rules in plan |
| FE-FZ-15 | No Playwright E2E tests |

## Production Build Evidence

```
apps/marketing: Next.js 16 — ✓ Compiled successfully
apps/web: Vite 8.1.5 — ✓ 7488 modules transformed
```

## Typecheck Evidence

```
pnpm -r --parallel run typecheck → all 44 packages: DONE (0 errors)
```

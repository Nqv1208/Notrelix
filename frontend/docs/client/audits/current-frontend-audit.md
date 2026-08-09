# Notrelix Frontend — Current State Audit

**Date:** 2026-07-06  
**Branch:** `refactor/frontend`  
**Status:** ✅ Complete

---

## 1. Root Structure

```txt
frontend/                          (pnpm + Turborepo monorepo)
├── apps/
│   ├── app/                       ← Next.js 16 App Router (monolith)
│   └── marketing/                 ← Skeleton (placeholder only)
├── packages/                      ← 35 packages (v4 skeleton)
│   ├── foundation/                ← contracts, kernel, platform, query, realtime, observability
│   ├── runtimes/                  ← web, mobile
│   ├── ui/                        ← tokens, web, mobile, icons
│   ├── product/work-management/   ← core, state, plugins, web, mobile, testing
│   ├── product/docs/              ← core, collaboration, web, mobile
│   ├── product/automation/        ← core, web, mobile
│   └── features/                  ← auth, workspace, account, billing, etc.
├── tooling/                       ← eslint-config, tsconfig, testing, dependency-rules
└── docs/                          ← Architecture docs, ADRs, migration tracker
```

---

## 2. App Route Groups

| Route Group        | Function                                        | Target v4        |
| :----------------- | :---------------------------------------------- | :--------------- |
| `app/(app)/`       | Landing, pricing, contact, legal                | `apps/marketing` |
| `app/(auth)/`      | Sign-in, sign-up, forgot-password               | `apps/web`       |
| `app/(dashboard)/` | Dashboard, account, billing, governance, search | `apps/web`       |
| `app/(workspace)/` | Workspace shell, boards, docs, chat             | `apps/web`       |
| `app/invite/`      | Accept workspace invitation                     | `apps/web`       |

---

## 3. Components

| Directory                       | Count | Target v4                                  | Action |
| :------------------------------ | :---: | :----------------------------------------- | :----- |
| `components/ui/`                |  ~80  | `packages/ui/web/src/components/ui/`       | MOVE   |
| `components/feedback/`          |   6   | `packages/ui/web/src/components/feedback/` | MOVE   |
| `components/layout/`            |  ~5   | `packages/ui/web/src/components/layout/`   | MOVE   |
| `components/theme-provider.tsx` |   1   | `packages/ui/web/src/theme/`               | MOVE   |

---

## 4. Features

| Feature         | Type | Files | Target v4                   | Action |
| :-------------- | :--: | :---: | :-------------------------- | :----- |
| auth            |  A   |  ~15  | `features/auth/`            | MOVE   |
| account         |  A   |  ~5   | `features/account/`         | MOVE   |
| workspace       |  A   |  ~40  | `features/workspace/`       | MOVE   |
| billing         |  A   |  ~5   | `features/billing/`         | MOVE   |
| integrations    |  A   |  ~5   | `features/integrations/`    | MOVE   |
| notifications   |  C   |  ~5   | `features/notifications/`   | MOVE   |
| activity        |  C   |  ~3   | `features/activity/`        | MOVE   |
| governance      |  A   |  ~5   | `features/governance/`      | MOVE   |
| search          |  C   |  ~3   | `features/search/`          | MOVE   |
| collaboration   |  C   |  ~10  | `features/collaboration/`   | MOVE   |
| work-management |  B   | ~100  | `product/work-management/*` | SPLIT  |
| docs            |  B   |  ~50  | `product/docs/*`            | SPLIT  |
| automation      |  A   |  ~5   | `product/automation/*`      | MOVE   |

**Type A** = CRUD (TanStack Query) | **Type B** = Collaborative graph | **Type C** = Realtime invalidation

---

## 5. Lib (Infrastructure)

| Module                      | Target v4                          | Action         |
| :-------------------------- | :--------------------------------- | :------------- |
| `lib/api/api-client.ts`     | `foundation/contracts/client/`     | MOVE           |
| `lib/api/endpoints.ts`      | `foundation/contracts/`            | MOVE + RENAME  |
| `lib/query/query-client.ts` | `foundation/query/`                | MOVE           |
| `lib/query/query-keys.ts`   | Split → features + wm-state        | SPLIT          |
| `lib/permissions/`          | `foundation/platform/permissions/` | MOVE           |
| `lib/realtime/`             | `foundation/realtime/`             | MOVE + IMPROVE |
| `lib/routes.ts`             | `foundation/platform/routes/`      | MOVE           |
| `lib/config/env.ts`         | `foundation/kernel/env/`           | MOVE           |
| `lib/errors/`               | `foundation/kernel/result/`        | MOVE           |
| `lib/theme/`                | `packages/ui/web/src/theme/`       | MOVE           |
| `lib/telemetry/`            | `foundation/observability/`        | MOVE           |
| `lib/utils.ts`              | `packages/ui/tokens/`              | MOVE           |

---

## 6. Risks

|  P  | Risk                              | Mitigation                             |
| :-: | :-------------------------------- | :------------------------------------- |
| P0  | Next.js monolith → Vite migration | Build apps/web new, run parallel       |
| P0  | query-keys.ts god-file            | Progressive migration with aliases     |
| P1  | Legacy naming in endpoints        | Rename during contracts extraction     |
| P1  | 80+ shadcn components to move     | Re-export barrels for backward compat  |
| P2  | Framework coupling in permissions | Extract pure logic, keep React adapter |
| P2  | No boundary enforcement           | dependency-rules check script created  |

---

## 7. Keep / Move / Add / Delete

### KEEP

- Current UI (all screens, components)
- shadcn/ui components (move, not rewrite)
- Current styles/messages/i18n

### MOVE

- `components/ui` → `packages/ui/web`
- `lib/permissions` → `foundation/platform`
- `lib/query/query-client` → `foundation/query`
- `lib/realtime` → `foundation/realtime`
- `features/work-management` → `product/work-management/*`
- All other features → `features/<feature>/`

### ADD

- `foundation/kernel`, `foundation/observability`
- `runtimes/web`, `runtimes/mobile`
- `ui/tokens`, `ui/mobile`
- `product/work-management/state`
- `apps/web` (Vite + TanStack Router)
- `tooling/dependency-rules`

### DELETE LATER

- Old compatibility paths
- Legacy endpoint naming
- Stale docs, unused mocks

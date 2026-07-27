# Wave F2 Runtime Migration Specification

> **Target Goal:** Zero legacy `api` / `configureApi` usages across the repository  
> **Tracker Document:** `frontend/MIGRATION_TRACKER.md`  
> **Prerequisite:** Wave F0 & F1 completed  

---

## 1. Objective & Migration Strategy

The goal of Wave F2 is to complete the migration of all remaining **16 legacy component files** currently importing `api` or `endpoints` from `@notrelix/contracts` at module level, replacing them with runtime-injected clients via `useAppRuntime()`.

Upon completion of Phase F2.4, the global `api` singleton, `configureApi()`, and `activeBaseUrl` will be **permanently deleted** from `@notrelix/contracts`.

---

## 2. Migration Pattern Rules

### ❌ Legacy Pattern (Forbidden)
```tsx
import { api, endpoints } from '@notrelix/contracts';

// Factory invoked at module scope using global singleton:
const useWorkspaceList = createUseWorkspaceList({ api, endpoints });

export function HomePage() {
  const { data } = useWorkspaceList();
  return <div>...</div>;
}
```

### ✅ Runtime Injected Pattern (Standard)
```tsx
import { useMemo } from 'react';
import { useAppRuntime } from '@notrelix/runtime-web';
import { createUseWorkspaceList } from '@notrelix/features-workspace';

export function HomePage() {
  const { api: runtimeClient } = useAppRuntime();
  
  // Factory instantiated within hook/component using injected runtime client:
  const useWorkspaceList = useMemo(
    () => createUseWorkspaceList({ api: runtimeClient.api, endpoints: runtimeClient.endpoints }),
    [runtimeClient]
  );
  
  const { data } = useWorkspaceList();
  return <div>...</div>;
}
```

---

## 3. Detailed Phase Breakdown & File Inventory

### Phase F2.1: Authentication Route Migration

| File Path | Component / Factory | Target Replacement |
| :--- | :--- | :--- |
| `apps/web/src/routes/sign-in.tsx` | `createLoginForm({ api, endpoints })` | Wrap in `useMemo` with `runtimeClient.api` |
| `apps/web/src/routes/sign-up.tsx` | `createRegisterForm({ api, endpoints })` | Wrap in `useMemo` with `runtimeClient.api` |
| `apps/web/src/routes/forgot-password.tsx` | `createForgotPasswordForm({ api, endpoints })` | Wrap in `useMemo` with `runtimeClient.api` |

---

### Phase F2.2: App Shell & Workspace Shell Migration

| File Path | Component / Factory | Target Replacement |
| :--- | :--- | :--- |
| `apps/web/src/shell/sidebar/workspace-switcher.tsx` | `createUseWorkspaceList`, `createUseCreateWorkspace` | Consume via `useAppRuntime()` inside component |
| `apps/web/src/shell/sidebar/sidebar.tsx` | `createUseWorkspaceShellData`, `createUseWorkspaceMembers` | Consume via `useAppRuntime()` inside component |
| `apps/web/src/shell/topbar/topbar.tsx` | `createNotificationBell` | Consume via `useAppRuntime()` inside component |
| `apps/web/src/routes/home.tsx` | `createUseWorkspaceList` | Consume via `useAppRuntime()` inside component |

---

### Phase F2.3: Workspace Routes & Member Settings Migration

| File Path | Component / Factory | Target Replacement |
| :--- | :--- | :--- |
| `apps/web/src/routes/workspaces/$workspaceId/settings.tsx` | Workspace settings hooks | Inject runtime client via hook |
| `apps/web/src/routes/workspaces/$workspaceId/dashboard.tsx` | Workspace dashboard hooks | Inject runtime client via hook |
| `apps/web/src/routes/workspaces/$workspaceId/docs/$docId.tsx` | Document view hooks | Inject runtime client via hook |
| `apps/web/src/routes/workspaces/$workspaceId/members.tsx` | Member management hooks | Inject runtime client via hook |
| `apps/web/src/routes/invite/$token.tsx` | Invite processing hook | Inject runtime client via hook |

---

### Phase F2.4: Account Feature Migration & Deletion Gate

#### Files Migrated in Account Scope
- `apps/web/src/routes/workspaces/$workspaceId/account/notifications.tsx`
- `apps/web/src/routes/workspaces/$workspaceId/account/profile.tsx`
- `apps/web/src/routes/workspaces/$workspaceId/account/appearance.tsx`
- `apps/web/src/routes/workspaces/$workspaceId/account/security.tsx`

#### Deletion Tasks
1. Remove `api`, `configureApi`, and `activeBaseUrl` from `packages/foundation/contracts/src/client/api-client.ts`.
2. Remove `api` export from `packages/foundation/contracts/src/index.ts`.
3. Remove `configureApi()` call from `apps/web/src/main.tsx`.
4. Delete `MIGRATION_TRACKER.md`.

---

## 4. Verification & Exit Criteria for Wave F2

- [x] `grep -rn "configureApi" frontend/` outputs 0 matches in code files.
- [x] `grep -rn "import { api" frontend/apps/web/src` outputs 0 matches.
- [x] `pnpm check:deps` passes with 0 violations.
- [x] `pnpm typecheck` passes cleanly across all 44 workspace packages.
- [x] Wave F2 Runtime Migration 100% Complete.

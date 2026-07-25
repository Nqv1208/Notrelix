# FE-FREEZE Migration Tracker

> Track incremental migration from legacy `api` singleton to `useAppRuntime()` pattern.
>
> **Target:** Remove all entries below and delete `api` / `configureApi` from `@notrelix/contracts`.

## Migration Pattern

**Old (module-level, legacy):**
```tsx
import { api, endpoints } from '@notrelix/contracts';
const LoginForm = createLoginForm({ api, endpoints }); // module-level
```

**New (runtime-injected):**
```tsx
function SignInPage() {
  const { api: runtimeClient } = useAppRuntime();
  const LoginForm = useMemo(
    () => createLoginForm({ api: runtimeClient.api, endpoints: runtimeClient.endpoints }),
    [runtimeClient],
  );
  return <LoginForm />;
}
```

---

## ❌ Remaining Files to Migrate

| File | Component | Priority |
|------|-----------|----------|
| `apps/web/src/shell/sidebar/workspace-switcher.tsx` | `createUseWorkspaceList`, `createUseCreateWorkspace` | High |
| `apps/web/src/shell/sidebar/sidebar.tsx` | `createUseWorkspaceShellData`, `createUseWorkspaceMembers` | High |
| `apps/web/src/shell/topbar/topbar.tsx` | `createNotificationBell` | High |
| `apps/web/src/routes/home.tsx` | `createUseWorkspaceList` | High |
| `apps/web/src/routes/workspaces/$workspaceId/settings.tsx` | Various | Medium |
| `apps/web/src/routes/workspaces/$workspaceId/dashboard.tsx` | Various | Medium |
| `apps/web/src/routes/workspaces/$workspaceId/docs/$docId.tsx` | Various | Medium |
| `apps/web/src/routes/workspaces/$workspaceId/members.tsx` | Various | Medium |
| `apps/web/src/routes/workspaces/$workspaceId/account/notifications.tsx` | Various | Low |
| `apps/web/src/routes/workspaces/$workspaceId/account/profile.tsx` | Various | Low |
| `apps/web/src/routes/workspaces/$workspaceId/account/appearance.tsx` | Various | Low |
| `apps/web/src/routes/workspaces/$workspaceId/account/security.tsx` | Various | Low |
| `apps/web/src/routes/invite/$token.tsx` | Various | Medium |
| `apps/web/src/routes/sign-in.tsx` | `createLoginForm` | High |
| `apps/web/src/routes/forgot-password.tsx` | `createForgotPasswordForm` | Medium |
| `apps/web/src/routes/sign-up.tsx` | `createRegisterForm` | High |

## ✅ Migrated Files

| File | Migrated in PR |
|------|---------------|
| `apps/web/src/main.tsx` | FE-FREEZE-01B (bridge) |
| `apps/web/src/providers/workspace-provider.tsx` | FE-FREEZE-01B |
| `apps/web/src/shell/guards/workspace-guard.tsx` | FE-FREEZE-01B |

---

## When to Remove the Bridge

Delete `configureApi` bridge and legacy `api` export when:
1. The table above has 0 rows remaining
2. `grep -r "from '@notrelix/contracts'" apps/web/src` only matches files that import `apiFetch` / `createNotrelixClient` (not `api` or `configureApi`)

# Safe Delete Audit — Notrelix Frontend

**Date:** 2026-07-08
**Auditor:** MiMo Code Agent
**Status:** Initial audit complete

---

## Summary

| Category                 | Count  | Can Delete Now | Delete After Migration |
| ------------------------ | ------ | -------------- | ---------------------- |
| Duplicate mock files     | 4      | 4              | 0                      |
| Old UI components        | 52     | 0              | 52                     |
| Lib re-export files      | 12     | 0              | 12                     |
| Feature index re-exports | 10     | 0              | 10                     |
| **Total**                | **78** | **4**          | **74**                 |

---

## DELETE_NOW — Safe to delete immediately

### 1. Duplicate Work Management Mock Files

These files are exact duplicates of files in `packages/product/work-management/core/src/mock/` and are not imported anywhere.

| File                                                                 | Replacement                                                                  | Imports | Risk |
| -------------------------------------------------------------------- | ---------------------------------------------------------------------------- | ------- | ---- |
| `apps/app/features/work-management/mock/mock-service.ts`             | `packages/product/work-management/core/src/mock/mock-service.ts`             | 0       | None |
| `apps/app/features/work-management/mock/mock-card-detail-service.ts` | `packages/product/work-management/core/src/mock/mock-card-detail-service.ts` | 0       | None |
| `apps/app/features/work-management/mock/mock-data.ts`                | `packages/product/work-management/core/src/mock/mock-data.ts`                | 0       | None |
| `apps/app/features/work-management/mock/mock-delay.ts`               | `packages/product/work-management/core/src/mock/mock-delay.ts`               | 0       | None |

**Action:** Delete `apps/app/features/work-management/mock/` directory.

---

## DELETE_AFTER_MIGRATION — Delete after import migration

### 2. Old UI Components (`apps/app/components/ui/`)

52 shadcn/ui components exist in both `apps/app/components/ui/` and `packages/ui/web/src/components/ui/`.

**Current state:** 0 imports from `@/components/ui/` — all 119 consumer files migrated to `@notrelix/ui-web/components/ui/`.
**Replacement:** `@notrelix/ui-web` package.
**Status:** READY FOR DELETION — all imports migrated.

**Action:** Delete `apps/app/components/ui/` directory.

---

### 3. Lib Re-export Files (`apps/app/lib/`)

12 files re-export from foundation packages. They're compatibility layers.

| File                                           | Replacement           | Imports | Can Migrate |
| ---------------------------------------------- | --------------------- | ------- | ----------- |
| `lib/api/api-client.ts`                        | `@notrelix/contracts` | 46      | Yes         |
| `lib/api/endpoints.ts`                         | `@notrelix/contracts` | 46      | Yes         |
| `lib/api/csrf.ts`                              | `@notrelix/contracts` | 0       | Yes         |
| `lib/api/request-id.ts`                        | `@notrelix/kernel`    | 0       | Yes         |
| `lib/errors/app-error.ts`                      | `@notrelix/kernel`    | 3       | Yes         |
| `lib/errors/error-map.ts`                      | `@notrelix/kernel`    | 0       | Yes         |
| `lib/errors/apply-server-validation-errors.ts` | `@notrelix/kernel`    | 0       | Yes         |
| `lib/permissions/use-can.ts`                   | `@notrelix/platform`  | 0       | Yes         |
| `lib/permissions/ability.ts`                   | `@notrelix/platform`  | 0       | Yes         |
| `lib/permissions/permissions.ts`               | `@notrelix/platform`  | 0       | Yes         |
| `lib/permissions/permission-context.ts`        | `@notrelix/platform`  | 0       | Yes         |
| `lib/permissions/permission-guard.tsx`         | `@notrelix/platform`  | 0       | Yes         |
| `lib/query/query-client.ts`                    | `@notrelix/query`     | 0       | Yes         |
| `lib/realtime/realtime-client.ts`              | `@notrelix/realtime`  | 0       | Yes         |
| `lib/routes.ts`                                | `@notrelix/platform`  | 0       | Yes         |

**Action:** After migrating all imports to packages, delete `apps/app/lib/api/`, `apps/app/lib/errors/`, `apps/app/lib/permissions/`, `apps/app/lib/query/`, `apps/app/lib/realtime/`, `apps/app/lib/routes.ts`.

---

### 4. Feature Index Re-exports (`apps/app/features/*/index.ts`)

10 feature index files re-export from packages.

| Feature                             | Re-exports From                                               | Imports | Can Migrate |
| ----------------------------------- | ------------------------------------------------------------- | ------- | ----------- |
| `features/auth/index.ts`            | `@notrelix/feature-auth`                                      | Many    | Yes         |
| `features/workspace/index.ts`       | `@notrelix/feature-workspace`                                 | Many    | Yes         |
| `features/work-management/index.ts` | `@notrelix/wm-core`, `@notrelix/wm-state`, `@notrelix/wm-web` | Many    | Yes         |
| Others (7)                          | Various packages                                              | Few     | Yes         |

**Action:** After migrating all imports to packages, delete feature index files.

---

## KEEP — Do not delete

### 5. Feature Implementation Files

The actual feature code in `apps/app/features/` (components, hooks, API clients) should NOT be deleted yet. They're still the active implementation. They will be moved to packages in future phases.

### 6. App Route Files

`apps/app/app/(auth)/`, `apps/app/app/(dashboard)/`, `apps/app/app/(workspace)/` are the active routes. Do not delete.

### 7. Marketing Components

`apps/app/app/(app)/_components/` and `apps/app/app/(app)/v2/` are still used by the main app. The marketing app has simplified versions. Do not delete until marketing app is fully validated.

---

## Recommended Deletion Order

1. **Phase 1 (Now):** Delete duplicate WM mock files (4 files)
2. **Phase 2 (After UI migration):** Delete old UI components (52 files)
3. **Phase 3 (After lib migration):** Delete lib re-exports (12 files)
4. **Phase 4 (After feature migration):** Delete feature index files (10 files)

---

## Verification Checklist

Before each deletion:

- [ ] Grep for imports of the file/directory
- [ ] Verify replacement exists in package
- [ ] Run `pnpm type-check`
- [ ] Run `pnpm lint`
- [ ] Run `pnpm build`
- [ ] Manual UI verification

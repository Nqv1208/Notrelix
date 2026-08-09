# FE-FREEZE Migration Tracker

> Track incremental migration from legacy `api` singleton to `useAppRuntime()` pattern.
>
> **Target:** ALL ENTRIES MIGRATED AND `api` / `configureApi` DELETED FROM `@notrelix/contracts`.

## Migration Status: 100% COMPLETE ✅

All legacy module-level singletons have been removed and replaced with runtime-injected clients via `useAppRuntime()`.
The `configureApi` bridge and legacy `api` export have been permanently deleted from `@notrelix/contracts`.

---

## ✅ Migrated Files

| File                                                                          | Component / Hook                                           | Status      |
| ----------------------------------------------------------------------------- | ---------------------------------------------------------- | ----------- |
| `apps/web/src/main.tsx`                                                       | App composition root                                       | ✅ Complete |
| `apps/web/src/providers/workspace-provider.tsx`                               | Workspace Context Provider                                 | ✅ Complete |
| `apps/web/src/shell/guards/workspace-guard.tsx`                               | Workspace Route Guard                                      | ✅ Complete |
| `apps/web/src/shell/sidebar/workspace-switcher.tsx`                           | `useWorkspaceList`, `useCreateWorkspace`                   | ✅ Complete |
| `apps/web/src/shell/sidebar/sidebar.tsx`                                      | `useWorkspaceShellData`, `useWorkspaceMembers`             | ✅ Complete |
| `apps/web/src/shell/topbar/topbar.tsx`                                        | `NotificationBell`                                         | ✅ Complete |
| `apps/web/src/shell/workspace-tabbed-frame.tsx`                               | `useReorderWorkspaceViews`                                 | ✅ Complete |
| `apps/web/src/routes/home.tsx`                                                | `useWorkspaceList`                                         | ✅ Complete |
| `apps/web/src/routes/workspaces/$workspaceId/settings.tsx`                    | `useUpdateWorkspace`                                       | ✅ Complete |
| `apps/web/src/routes/workspaces/$workspaceId/dashboard.tsx`                   | `usePageList`, `useDocsFavorites`, `useWorkspaceMembers`   | ✅ Complete |
| `apps/web/src/routes/workspaces/$workspaceId/docs/$docId.tsx`                 | `DocPageScreen`                                            | ✅ Complete |
| `apps/web/src/routes/workspaces/$workspaceId/members.tsx`                     | Member management hooks                                    | ✅ Complete |
| `apps/web/src/routes/workspaces/$workspaceId/account/notifications.tsx`       | `useNotificationSettings`, `useUpdateNotificationSettings` | ✅ Complete |
| `apps/web/src/routes/workspaces/$workspaceId/account/profile.tsx`             | `useProfile`, `useUpdateProfile`                           | ✅ Complete |
| `apps/web/src/routes/workspaces/$workspaceId/account/appearance.tsx`          | `useAppearanceSettings`, `useUpdateAppearanceSettings`     | ✅ Complete |
| `apps/web/src/routes/workspaces/$workspaceId/account/security.tsx`            | `useSecuritySettings`                                      | ✅ Complete |
| `apps/web/src/routes/invite/$token.tsx`                                       | `useInvitationDetails`, `useAcceptInvitation`              | ✅ Complete |
| `apps/web/src/routes/sign-in.tsx`                                             | `LoginForm`                                                | ✅ Complete |
| `apps/web/src/routes/forgot-password.tsx`                                     | `ForgotPasswordForm`                                       | ✅ Complete |
| `apps/web/src/routes/sign-up.tsx`                                             | `RegisterForm`                                             | ✅ Complete |
| `packages/features/collaboration/src/web/components/resource-comments.tsx`    | `ResourceComments`                                         | ✅ Complete |
| `packages/features/workspace/src/web/components/pending-invitations-menu.tsx` | `PendingInvitationsMenu`                                   | ✅ Complete |
| `packages/features/workspace/src/web/components/workspace-add-view-menu.tsx`  | `WorkspaceAddViewMenu`                                     | ✅ Complete |
| `packages/product/work-management/state/src/api/*.api.ts`                     | Work management API modules                                | ✅ Complete |

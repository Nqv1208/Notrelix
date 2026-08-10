# Symmetry Audit Report — Notrelix Frontend

## 1. Executive Summary

- **Total files analyzed in `main` branch:** 486
- **Successfully mapped to new structure:** 301
- **Unmapped/Refactored/Deleted files:** 170

## 2. Unmapped / Refactored / Deleted Files Detail

Below are the files in the `main` branch that do not have a direct 1-to-1 name mapping in the `refactor/frontend` workspace. In many cases, these files were completely rewritten, replaced, or combined into the new monorepo structure:

| File in `main`                                                                                | Explanation / Target Status                                                                           |
| --------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| `.dockerignore`                                                                               | Deleted / Refactored                                                                                  |
| `Dockerfile.dev`                                                                              | Deleted / Refactored                                                                                  |
| `app/(app)/_components/footer.tsx`                                                            | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(app)/_components/header.tsx`                                                            | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(app)/v2/_components/editorial-actions.tsx`                                              | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(app)/v2/_components/editorial-enterprise.tsx`                                           | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(app)/v2/_components/editorial-final-cta.tsx`                                            | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(app)/v2/_components/editorial-footer.tsx`                                               | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(app)/v2/_components/editorial-hero-preview.tsx`                                         | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(app)/v2/_components/editorial-hero.tsx`                                                 | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(app)/v2/_components/editorial-nav.tsx`                                                  | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(app)/v2/_components/editorial-pillars.tsx`                                              | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(app)/v2/_components/editorial-pricing.tsx`                                              | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(app)/v2/_components/editorial-proof.tsx`                                                | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(app)/v2/_components/editorial-reveal.tsx`                                               | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(app)/v2/_components/editorial-scale.tsx`                                                | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(app)/v2/_components/editorial-section-label.tsx`                                        | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(app)/v2/_components/editorial-showcase.tsx`                                             | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(app)/v2/_components/editorial-stats.tsx`                                                | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(app)/v2/editorial.css`                                                                  | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(dashboard)/_components/app-header.tsx`                                                  | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(dashboard)/_components/app-sidebar.tsx`                                                 | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(dashboard)/_components/dashboard-overview.tsx`                                          | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(dashboard)/_components/header/global-search-button.tsx`                                 | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(dashboard)/_components/header/global-search-dialog.tsx`                                 | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(dashboard)/_components/header/user-menu.tsx`                                            | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(dashboard)/_components/home-data.ts`                                                    | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(dashboard)/_components/sidebar/ai-nav.tsx`                                              | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(dashboard)/_components/sidebar/favorites-section.tsx`                                   | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(dashboard)/_components/sidebar/logo-nav.tsx`                                            | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(dashboard)/_components/sidebar/primary-nav.tsx`                                         | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(dashboard)/_components/sidebar/recent-section.tsx`                                      | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(dashboard)/home/_components/home-workspaces-section.tsx`                                | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(dashboard)/home/loading.tsx`                                                            | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/_components/board-layout/workspace-board-shell.tsx`            | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/_components/chat/workspace-room-chat.tsx`                      | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/_components/dashboard/workspace-data.ts`                       | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/_components/dashboard/workspace-mock-data.ts`                  | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/_components/dashboard/workspace-view-content.tsx`              | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/_components/shell/workspace-management-panel.tsx`              | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/_components/shell/workspace-shell.tsx`                         | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/_components/shell/workspace-sidebar.tsx`                       | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/_components/shell/workspace-tabbed-shell.tsx`                  | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/boards/[boardId]/_components/board-workspace-shell.tsx`        | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/boards/[boardId]/_components/views/board-kanban-view.tsx`      | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/boards/_components/board-workbench.tsx`                        | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/breadcrumb-nav.tsx`                  | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/editor-shell.tsx`                    | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/editor/alignment-controls.tsx`       | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/editor/block-drag-handle.tsx`        | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/editor/block-editor.tsx`             | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/editor/block-renderer.tsx`           | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/editor/block-type-menu.tsx`          | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/editor/comments-popover.tsx`         | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/editor/doc-editor-toolbar.tsx`       | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/editor/document-format-toolbar.tsx`  | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/editor/editable-block.tsx`           | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/editor/editable-page-title.tsx`      | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/editor/floating-actions.tsx`         | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/editor/floating-format-toolbar.tsx`  | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/editor/font-family-dropdown.tsx`     | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/editor/formatting.ts`                | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/editor/inline-toolbar.tsx`           | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/editor/mention-menu.tsx`             | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/editor/monday-doc-editor.tsx`        | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/editor/slash-command-menu.tsx`       | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/editor/slash-command.tsx`            | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/editor/text-style-dropdown.tsx`      | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/metadata/collaborative-presence.tsx` | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/metadata/linked-content.tsx`         | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/metadata/page-activity.tsx`          | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/metadata/page-comments.tsx`          | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/metadata/page-cover.tsx`             | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/metadata/page-header.tsx`            | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/metadata/page-properties.tsx`        | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/_components/metadata/page-toolbar.tsx`           | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/error.tsx`                                       | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/history/_components/history-client.tsx`          | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/[pageId]/loading.tsx`                                     | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/_components/block-renderer.tsx`                           | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/_components/docs-client-page.tsx`                         | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/_components/docs-overview.tsx`                            | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/_components/docs-search.tsx`                              | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/_components/docs-sidebar.tsx`                             | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/_components/docs-toolbar.tsx`                             | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/_components/docs-workspace-chrome.tsx`                    | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/_components/favorites-section.tsx`                        | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/_components/new-page-button.tsx`                          | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/_components/page-editor.tsx`                              | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/_components/page-title.tsx`                               | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/_components/page-tree.tsx`                                | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/_components/recent-pages.tsx`                             | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/_components/slash-command-menu.tsx`                       | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/_components/sortable-block.tsx`                           | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/_components/templates-section.tsx`                        | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/error.tsx`                                                | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/docs/loading.tsx`                                              | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/error.tsx`                                                     | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/(workspace)/[workspaceId]/loading.tsx`                                                   | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/favicon.ico`                                                                             | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `app/providers.tsx`                                                                           | App router path deleted; replaced by TanStack Router route in `apps/web/src/routes`                   |
| `bun.lock`                                                                                    | Deleted / Refactored                                                                                  |
| `components/marketing/AnimatedCursor.tsx`                                                     | Legacy marketing component; replaced by refined sections in `apps/marketing/src/sections/v2/`         |
| `components/marketing/DashboardMock.tsx`                                                      | Legacy marketing component; replaced by refined sections in `apps/marketing/src/sections/v2/`         |
| `components/marketing/DocsEditorMock.tsx`                                                     | Legacy marketing component; replaced by refined sections in `apps/marketing/src/sections/v2/`         |
| `components/marketing/EnterpriseSection.tsx`                                                  | Legacy marketing component; replaced by refined sections in `apps/marketing/src/sections/v2/`         |
| `components/marketing/FeatureGrid.tsx`                                                        | Legacy marketing component; replaced by refined sections in `apps/marketing/src/sections/v2/`         |
| `components/marketing/FinalCTA.tsx`                                                           | Legacy marketing component; replaced by refined sections in `apps/marketing/src/sections/v2/`         |
| `components/marketing/HeroInteractiveDemo.tsx`                                                | Legacy marketing component; replaced by refined sections in `apps/marketing/src/sections/v2/`         |
| `components/marketing/KanbanBoardMock.tsx`                                                    | Legacy marketing component; replaced by refined sections in `apps/marketing/src/sections/v2/`         |
| `components/marketing/LandingHero.tsx`                                                        | Legacy marketing component; replaced by refined sections in `apps/marketing/src/sections/v2/`         |
| `components/marketing/MockBrowserFrame.tsx`                                                   | Legacy marketing component; replaced by refined sections in `apps/marketing/src/sections/v2/`         |
| `components/marketing/StreamingText.tsx`                                                      | Legacy marketing component; replaced by refined sections in `apps/marketing/src/sections/v2/`         |
| `components/marketing/TaskCardMock.tsx`                                                       | Legacy marketing component; replaced by refined sections in `apps/marketing/src/sections/v2/`         |
| `components/marketing/TaskModalMock.tsx`                                                      | Legacy marketing component; replaced by refined sections in `apps/marketing/src/sections/v2/`         |
| `components/marketing/UseCaseSection.tsx`                                                     | Legacy marketing component; replaced by refined sections in `apps/marketing/src/sections/v2/`         |
| `components/marketing/WorkspaceSidebarMock.tsx`                                               | Legacy marketing component; replaced by refined sections in `apps/marketing/src/sections/v2/`         |
| `doctor.config.json`                                                                          | Deleted / Refactored                                                                                  |
| `features/api-contracts.test.ts`                                                              | Deleted / Refactored                                                                                  |
| `features/auth/hooks/useAuth.ts`                                                              | Rewritten/Integrated into standard Auth Provider client model                                         |
| `features/auth/i18n/auth-error-keys.ts`                                                       | Deleted / Refactored                                                                                  |
| `features/auth/i18n/server-error-map.ts`                                                      | Deleted / Refactored                                                                                  |
| `features/auth/schemas/register.schemas.ts`                                                   | Deleted / Refactored                                                                                  |
| `features/auth/types/auth.types.ts`                                                           | Deleted / Refactored                                                                                  |
| `features/boards/api/card.api.ts`                                                             | Deleted / Refactored                                                                                  |
| `features/boards/api/column.api.ts`                                                           | Deleted / Refactored                                                                                  |
| `features/boards/hooks/query-cache.ts`                                                        | Deleted / Refactored                                                                                  |
| `features/boards/types/api-types.ts`                                                          | Deleted / Refactored                                                                                  |
| `features/boards/utils/board-api-mappers.test.ts`                                             | Deleted / Refactored                                                                                  |
| `features/docs/adapters/search-adapter.ts`                                                    | Deleted / Refactored                                                                                  |
| `features/docs/api/block.service.ts`                                                          | Deleted / Refactored                                                                                  |
| `features/docs/api/page-activity.api.ts`                                                      | Deleted / Refactored                                                                                  |
| `features/docs/api/page-comments.api.ts`                                                      | Deleted / Refactored                                                                                  |
| `features/docs/api/page.service.ts`                                                           | Deleted / Refactored                                                                                  |
| `features/docs/data/sample-data.ts`                                                           | Deleted / Refactored                                                                                  |
| `features/docs/hooks/use-doc-toolbar.ts`                                                      | Deleted / Refactored                                                                                  |
| `features/docs/hooks/use-editor-selection.ts`                                                 | Deleted / Refactored                                                                                  |
| `features/docs/hooks/use-favorites.ts`                                                        | Deleted / Refactored                                                                                  |
| `features/docs/hooks/use-page-tree.ts`                                                        | Deleted / Refactored                                                                                  |
| `features/docs/hooks/use-slash-command.ts`                                                    | Deleted / Refactored                                                                                  |
| `features/docs/mock/mock-page-service.ts`                                                     | Legacy mock data; replaced by structured package mocks in `packages/product/work-management/testing/` |
| `features/docs/schemas/block.schema.ts`                                                       | Deleted / Refactored                                                                                  |
| `features/docs/schemas/page.schema.ts`                                                        | Deleted / Refactored                                                                                  |
| `features/docs/store/editor-store.ts`                                                         | Deleted / Refactored                                                                                  |
| `features/docs/types/document.types.ts`                                                       | Deleted / Refactored                                                                                  |
| `features/docs/types/dto.ts`                                                                  | Deleted / Refactored                                                                                  |
| `features/docs/utils/block-helpers.ts`                                                        | Deleted / Refactored                                                                                  |
| `features/docs/utils/page-tree.ts`                                                            | Deleted / Refactored                                                                                  |
| `features/workspace/api/activity.api.ts`                                                      | Deleted / Refactored                                                                                  |
| `features/workspace/api/invitations.api.ts`                                                   | Deleted / Refactored                                                                                  |
| `features/workspace/api/members.api.ts`                                                       | Deleted / Refactored                                                                                  |
| `features/workspace/api/views.api.ts`                                                         | Deleted / Refactored                                                                                  |
| `features/workspace/api/workspace-api-mappers.test.ts`                                        | Deleted / Refactored                                                                                  |
| `features/workspace/api/workspace.api.ts`                                                     | Deleted / Refactored                                                                                  |
| `features/workspace/hooks/queries/use-workspace-activity.ts`                                  | Deleted / Refactored                                                                                  |
| `features/workspace/hooks/queries/use-workspace-snapshot.ts`                                  | Deleted / Refactored                                                                                  |
| `features/workspace/hooks/state/use-active-workspace-view.ts`                                 | Deleted / Refactored                                                                                  |
| `features/workspace/schemas/workspace-view.schema.ts`                                         | Deleted / Refactored                                                                                  |
| `features/workspace/types/dto.ts`                                                             | Deleted / Refactored                                                                                  |
| `features/workspace/utils/settings.test.ts`                                                   | Deleted / Refactored                                                                                  |
| `features/workspace/utils/workspace-routes.test.ts`                                           | Deleted / Refactored                                                                                  |
| `features/workspace/utils/workspace-routes.ts`                                                | Deleted / Refactored                                                                                  |
| `features/workspace/utils/workspace-view.ts`                                                  | Deleted / Refactored                                                                                  |
| `hooks/use-demo-timeline.ts`                                                                  | Deleted / Refactored                                                                                  |
| `hooks/use-mounted.ts`                                                                        | Deleted / Refactored                                                                                  |
| `i18n/request.ts`                                                                             | Deleted / Refactored                                                                                  |
| `lib/utils.ts`                                                                                | Deleted / Refactored                                                                                  |
| `messages/en.json`                                                                            | Deleted / Refactored                                                                                  |
| `messages/vi.json`                                                                            | Deleted / Refactored                                                                                  |
| `types/api.ts`                                                                                | Deleted / Refactored                                                                                  |

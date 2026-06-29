# Notrelix Frontend Enterprise System Blueprint

> Version: 2026-06-27  
> Owner mindset: Frontend Tech Lead / System Designer  
> Scope: `frontend/`  
> Architecture stance: Notrelix is an enterprise workspace operating system from day one. The frontend is not designed as a small app that will be refactored later. The target architecture is the implementation architecture.

---

## 0. Core Decision

Notrelix frontend must be designed as a large enterprise product platform, not as a simple CRUD frontend.

The frontend architecture is based on four principles:

```txt
1. Product capability ownership over generic folders.
2. Route composition is separated from business ownership.
3. Server state, UI state, URL state, permission state and realtime state are different systems.
4. Every feature is designed for scale from the beginning, but only the owning feature exposes a small public API.
```

Notrelix is not just Boards. It is a workspace operating system that includes work management, documents, collaboration, notifications, search, governance, billing, automation and integrations. The frontend must reflect that.

---

## 1. Enterprise Product Capability Model

### 1.1. Product capabilities

```txt
Notrelix
├── Identity & Access
├── Account & User Preferences
├── Workspace Operating Layer
├── Work Management
├── Documents & Knowledge
├── Collaboration
├── Notifications
├── Search & Command System
├── Billing & Entitlements
├── Governance & Permissions
├── Automation
├── Integrations
├── Activity & Audit-facing UX
├── Realtime Experience
├── Design System
└── Frontend Platform Infrastructure
```

These are not just folders. They are ownership boundaries.

A developer should be able to answer in under 30 seconds:

```txt
Where does this feature belong?
Who owns its API calls?
Who owns its query keys?
Who owns its UI components?
Who owns its permissions?
Who owns its error/loading/empty states?
```

If the answer is not obvious, the architecture is not enterprise-ready.

---

## 2. Backend Bounded Context to Frontend Capability Map

Frontend should align with backend bounded contexts for domain language, but must not copy backend 1:1.

Backend bounded contexts protect domain invariants, aggregate boundaries and transactional consistency.

Frontend capabilities protect product experience, screen ownership, data fetching, UI state, permissions and interaction boundaries.

| Backend context | Frontend feature | Frontend responsibility |
|---|---|---|
| Identity | `features/auth` | Sign in, sign up, session bootstrap, refresh, logout, OAuth UI |
| Account/Profile | `features/account` | Profile, preferences, security, sessions, user menu ownership if business-heavy |
| Workspaces | `features/workspace` | Workspace switcher, workspace home, spaces, members, invitations, settings |
| Work Management | `features/work-management` | Boards, items/cards, fields, groups, views, labels, checklists, board UI |
| Documents | `features/docs` | Pages, blocks, editor, tree, templates, document workspace |
| Collaboration | `features/collaboration` | Comments, mentions, reactions, watchers, attachments, presence |
| Notifications | `features/notifications` | Bell, unread count, notification center, stream, preferences |
| Search/Projection | `features/search` | Global search, command palette, quick navigation, result rendering |
| Billing | `features/billing` | Plans, subscriptions, invoices, entitlements, usage, checkout UX |
| Governance | `features/governance` | Roles, permissions, policy matrix, audit-facing admin UX |
| Automation | `features/automation` | Rule builder, triggers, actions, runs, execution history |
| Integrations | `features/integrations` | Providers, OAuth connections, webhooks, sync jobs |
| Activity/Audit | `features/activity` | Product activity feeds, timeline, actor/resource snapshots |

Composition surfaces are not bounded contexts:

```txt
Dashboard
AppShell
WorkspaceHome
CommandPalette
GlobalNavigation
AccountMenu
```

These surfaces may compose many features, but they should not own those features' business logic.

---

## 3. Final Top-Level Frontend Structure

```txt
frontend/
  app/
    (marketing)/
    (auth)/
    (dashboard)/
    (workspace)/
    invite/
    layout.tsx
    providers.tsx
    error.tsx
    not-found.tsx

  components/
    ui/
    layout/
    feedback/
    data-display/
    forms/
    overlays/
    icons/
    marketing/

  features/
    auth/
    account/
    workspace/
    work-management/
    docs/
    collaboration/
    notifications/
    search/
    billing/
    governance/
    automation/
    integrations/
    activity/

  lib/
    api/
    query/
    auth/
    routes/
    config/
    errors/
    permissions/
    realtime/
    telemetry/
    storage/
    feature-flags/
    utils/

  styles/
    globals.css
    tokens.css

  i18n/
  messages/
  types/
  tests/
    architecture/
    e2e/
    fixtures/
```

This is the target architecture. New code must be written directly into this architecture. Legacy code should be moved toward it, not used as the model for future development.

---

## 4. Top-Level Layer Contract

### 4.1. `app/`

`app/` is route composition only.

It owns:

```txt
Routing
Route groups
Layouts
Page composition
Route params/search params parsing
Route-level loading/error/not-found
Route-private composition components
```

It must not own:

```txt
Business API calls
DTO mapping
Mutation orchestration
Optimistic cache updates
Permission evaluator core
Feature service implementation
Business components that can be reused
```

Pattern:

```tsx
// app/(workspace)/[workspaceId]/boards/[boardId]/page.tsx
import { BoardScreen } from "@/features/work-management"

export default function Page() {
  return <BoardScreen />
}
```

### 4.2. `features/`

Feature owns complete business capability:

```txt
Business UI
API modules
DTOs
Mappers
Query hooks
Mutation hooks
UI-state hooks
Schemas
Types
Cache helpers
Permission-aware components
Realtime subscriptions for that feature
Feature public API
```

Feature is not just logic. Feature owns the UI that belongs to its business capability.

### 4.3. `components/`

Shared UI only.

```txt
components/ui          primitive design-system components
components/layout      generic layout primitives
components/feedback    empty/error/loading/permission states
components/data-display generic tables/cards/stats/avatar groups
components/forms       generic form wrappers/field components
components/overlays    generic dialogs/sheets/popovers
components/marketing   public marketing-only sections
```

`components/ui` must never know business concepts like workspace, board, card, invoice, notification or role.

### 4.4. `lib/`

Infrastructure only.

```txt
API client
Query client
Route builders
Permission evaluator
Error mapping
Realtime client
Telemetry
Config/env
Feature flags
Browser storage helpers
Generic utilities
```

`lib` must not import `features`.

---

## 5. Route Architecture Matrix

Routes represent resources, entry points and composition surfaces. Views/tabs are not automatically routes.

| Route | Owner | Type | Notes |
|---|---|---|---|
| `/` | marketing | public route | landing/commercial page |
| `/pricing` | marketing + billing | public route | pricing presentation, no billing mutation |
| `/sign-in` | auth | auth route | sign-in flow |
| `/sign-up` | auth | auth route | registration flow |
| `/forgot-password` | auth | auth route | recovery flow |
| `/reset-password` | auth | auth route | recovery flow |
| `/dashboard` | app composition | composition | recent workspaces, recent boards/docs, notifications |
| `/[workspaceId]` | workspace composition | workspace home | composes workspace/work-management/docs/activity |
| `/[workspaceId]/boards` | work-management | resource list | boards list/overview |
| `/[workspaceId]/boards/[boardId]` | work-management | resource route | board screen |
| `/[workspaceId]/boards/[boardId]?view=table` | work-management | presentation state | not a separate route |
| `/[workspaceId]/boards/[boardId]?view=kanban` | work-management | presentation state | not a separate route |
| `/[workspaceId]/boards/[boardId]/card/[cardId]` | work-management | resource route | card detail deep link |
| `/[workspaceId]/docs` | docs | resource list | page/document tree |
| `/[workspaceId]/docs/[pageId]` | docs | resource route | page editor/reader |
| `/[workspaceId]/settings/general` | workspace | settings | workspace general settings |
| `/[workspaceId]/settings/members` | workspace | settings | members/invitations |
| `/[workspaceId]/settings/permissions` | governance | settings | roles/policies |
| `/[workspaceId]/settings/billing` | billing | settings | subscription/plan/invoices |
| `/[workspaceId]/automation` | automation | product module | automation rules |
| `/[workspaceId]/integrations` | integrations | product module | connections/providers |
| `/account/profile` | account | account route | profile/preferences |
| `/account/security` | account | account route | security/sessions |
| `/invite/[token]` | workspace + auth | composition | invitation accept flow |

Board view rule:

```txt
Table/Kanban/Calendar/Timeline are renderers of a board.
They use the same work data.
They must not become separate resource routes or separate data models.
```

---

## 6. Feature Architecture Standard

Every feature should follow one of two forms.

### 6.1. Small feature shape

Used for `auth`, `account`, small `notifications` surfaces, small `search` slices.

```txt
features/<feature>/
  api/
  components/
  hooks/
    queries/
    mutations/
    state/
  model/
  schemas/
  types/
  utils/
  index.ts
```

### 6.2. Large feature shape

Used for `work-management`, `docs`, `collaboration`, `governance`, `billing`, `automation`, `integrations`.

```txt
features/<feature>/
  <capability-a>/
  <capability-b>/
  <capability-c>/
  shared/
  index.ts
```

Each internal capability can own its own:

```txt
api/
hooks/
components/
model/
schemas/
types/
utils/
```

Not every submodule must have every folder. But ownership must be clear.

---

## 7. Work Management Final Architecture

Work Management is the most complex frontend capability. It must not be treated as a renamed `boards` folder.

### 7.1. Domain/product model

```txt
Board
  owns board metadata, board layout, board view configurations

BoardView
  saved configuration: visible fields, filters, sorts, grouping, mode

BoardItem/Card
  unit of work inside a board

Field
  dynamic schema and values for items

Group
  grouping/swimlane/status grouping inside board

Checklist
  checklist content inside item/card

Label
  categorization and visual metadata
```

### 7.2. Final folder structure

```txt
features/work-management/
  boards/
    api/
      board.api.ts
      board-view.api.ts
      board.dto.ts
      board-view.dto.ts

    model/
      board.mapper.ts
      board-view.mapper.ts
      board.selectors.ts
      board.guards.ts

    hooks/
      queries/
        use-full-board.ts
        use-workspace-boards.ts
        use-resolved-workspace-board.ts
        use-board-view.ts
      mutations/
        use-create-board.ts
        use-update-board.ts
        use-delete-board.ts
        use-create-board-view.ts
        use-update-board-view.ts
      state/
        use-board-view-mode.ts
        use-selected-card-panel.ts

    components/
      board-screen.tsx
      board-shell.tsx
      board-header.tsx
      board-toolbar.tsx
      board-view-switcher.tsx
      board-settings-dialog.tsx
      board-empty-state.tsx
      views/
        table/
          table-board-view.tsx
          board-table.tsx
          table-header.tsx
          table-row.tsx
          table-cell.tsx
          table-filter-bar.tsx
          table-sort-menu.tsx
        kanban/
          kanban-board-view.tsx
          kanban-column.tsx
          kanban-card.tsx
          kanban-filter-bar.tsx
        calendar/
          calendar-board-view.tsx
          calendar-item.tsx
        timeline/
          timeline-board-view.tsx
          timeline-row.tsx
        shared/
          view-empty-state.tsx
          view-loading-state.tsx

    schemas/
      board.schema.ts
      board-view.schema.ts

    types/
      board.types.ts
      board-view.types.ts

  items/
    api/
      item.api.ts
      item.dto.ts

    model/
      item.mapper.ts
      item.selectors.ts
      item.guards.ts

    hooks/
      queries/
        use-card-detail.ts
        use-card-activity.ts
        use-card-comments.ts
        use-card-files.ts
      mutations/
        use-create-card.ts
        use-update-card.ts
        use-delete-card.ts
        use-move-card.ts
        use-duplicate-card.ts
        use-update-field-value.ts
        use-upload-card-file.ts
      state/
        use-card-detail-tabs.ts

    components/
      card-detail/
        card-detail-panel.tsx
        card-detail-header.tsx
        card-detail-tabs.tsx
        card-description.tsx
        card-assignees.tsx
        card-dates.tsx
        card-activity-tab.tsx
        card-comments-tab.tsx
        card-files-tab.tsx
      item-title.tsx
      item-assignees.tsx
      item-status-badge.tsx

    schemas/
      item.schema.ts
      update-field-value.schema.ts

    types/
      item.types.ts
      card-detail.types.ts

  fields/
    api/
      field.api.ts
      field.dto.ts

    model/
      field.mapper.ts
      field-value.mapper.ts
      field-renderer.registry.ts
      field-editor.registry.ts

    hooks/
      mutations/
        use-create-field.ts
        use-update-field.ts
        use-delete-field.ts
      state/
        use-column-resize.ts
        use-column-visibility.ts

    components/
      field-header.tsx
      field-value-cell.tsx
      renderers/
        text-field-renderer.tsx
        number-field-renderer.tsx
        select-field-renderer.tsx
        date-field-renderer.tsx
        people-field-renderer.tsx
      editors/
        text-field-editor.tsx
        number-field-editor.tsx
        select-field-editor.tsx
        date-field-editor.tsx
        people-field-editor.tsx

    schemas/
      field.schema.ts
      field-value.schema.ts

    types/
      field.types.ts

  groups/
    api/
      group.api.ts
      group.dto.ts
    hooks/
      queries/
        use-board-groups.ts
      mutations/
        use-create-group.ts
        use-update-group.ts
        use-delete-group.ts
        use-duplicate-group.ts
    components/
      group-header.tsx
      group-menu.tsx
      group-empty-state.tsx
    schemas/
      group.schema.ts
    types/
      group.types.ts

  checklists/
    api/
      checklist.api.ts
      checklist.dto.ts
    hooks/
      queries/
        use-card-checklists.ts
      mutations/
        use-create-checklist.ts
        use-update-checklist.ts
        use-delete-checklist.ts
    components/
      checklist-section.tsx
      checklist-item.tsx
    schemas/
      checklist.schema.ts
    types/
      checklist.types.ts

  labels/
    api/
      label.api.ts
      label.dto.ts
    hooks/
      queries/
        use-board-labels.ts
      mutations/
        use-create-label.ts
        use-update-label.ts
        use-delete-label.ts
    components/
      label-chip.tsx
      label-picker.tsx
    schemas/
      label.schema.ts
    types/
      label.types.ts

  cache/
    board-cache-updaters.ts
    optimistic-card.ts
    optimistic-group.ts
    optimistic-field-value.ts
    board-invalidation.ts

  shared/
    components/
      assignee-avatar-stack.tsx
      board-member-picker.tsx
      drag-overlay.tsx
    hooks/
      use-dnd-sensors.ts
    types/
      resource-ref.types.ts
    utils/
      position.ts
      board-routes.ts

  index.ts
```

### 7.3. Critical rule about board views

Do not create top-level `features/work-management/views`.

Wrong:

```txt
features/work-management/views/table
features/work-management/views/kanban
```

Correct:

```txt
features/work-management/boards/components/views/table
features/work-management/boards/components/views/kanban
```

Reason:

```txt
Table/Kanban/Calendar/Timeline are presentation renderers of a Board.
They are not bounded contexts.
They must share Board, Item, Field, Group and BoardView models.
```

---

## 8. Documents Final Architecture

Documents is not just a page list. It is a knowledge/productivity subsystem.

```txt
features/docs/
  pages/
    api/
      page.api.ts
      page.dto.ts
    model/
      page.mapper.ts
      page.selectors.ts
    hooks/
      queries/
        use-page.ts
        use-page-breadcrumbs.ts
      mutations/
        use-create-page.ts
        use-update-page.ts
        use-delete-page.ts
        use-move-page.ts
    components/
      page-screen.tsx
      page-header.tsx
      page-cover.tsx
      page-title.tsx
    schemas/
      page.schema.ts
    types/
      page.types.ts

  blocks/
    api/
      block.api.ts
      block.dto.ts
    model/
      block.mapper.ts
      block-normalizer.ts
    hooks/
      queries/
        use-page-blocks.ts
      mutations/
        use-create-block.ts
        use-update-block.ts
        use-delete-block.ts
        use-reorder-blocks.ts
    components/
      block-renderer.tsx
      block-toolbar.tsx
      blocks/
        paragraph-block.tsx
        heading-block.tsx
        code-block.tsx
        embed-block.tsx
    schemas/
      block.schema.ts
    types/
      block.types.ts

  editor/
    components/
      page-editor.tsx
      editor-toolbar.tsx
      slash-command-menu.tsx
    hooks/
      use-editor-commands.ts
      use-editor-selection.ts
    store/
      editor-store.ts
    extensions/

  tree/
    hooks/
      use-page-tree.ts
    components/
      page-tree.tsx
      page-tree-item.tsx
    model/
      page-tree.mapper.ts

  templates/
    api/
      template.api.ts
    hooks/
    components/
      template-gallery.tsx
      template-card.tsx
    types/

  cache/
    docs-invalidation.ts
    optimistic-page.ts
    optimistic-block.ts

  shared/
    components/
    types/
    utils/

  index.ts
```

Rules:

```txt
Page metadata uses TanStack Query.
Editor transient state can use local store.
Block renderer must not call API directly.
Page tree does not know editor internals.
Docs must not import work-management internals.
```

---

## 9. Collaboration Final Architecture

Collaboration is cross-resource and product-facing. It is not infrastructure.

```txt
features/collaboration/
  comments/
    api/
      comment.api.ts
      comment.dto.ts
    model/
      comment.mapper.ts
    hooks/
      queries/
        use-resource-comments.ts
      mutations/
        use-create-comment.ts
        use-update-comment.ts
        use-delete-comment.ts
        use-resolve-comment.ts
    components/
      comment-thread.tsx
      comment-item.tsx
      comment-editor.tsx
      comment-resolve-button.tsx
    schemas/
      comment.schema.ts
    types/
      comment.types.ts

  mentions/
    api/
      mention.api.ts
    hooks/
      use-mention-suggestions.ts
    components/
      mention-input.tsx
      mention-suggestion-list.tsx
    types/

  reactions/
    api/
      reaction.api.ts
    hooks/
      use-reactions.ts
      use-toggle-reaction.ts
    components/
      reaction-bar.tsx
      reaction-picker.tsx
    types/

  watchers/
    api/
      watcher.api.ts
    hooks/
      use-watch-resource.ts
      use-resource-watchers.ts
    components/
      watch-button.tsx
      watcher-list.tsx
    types/

  attachments/
    api/
      attachment.api.ts
    hooks/
      use-resource-attachments.ts
      use-upload-attachment.ts
    components/
      attachment-list.tsx
      attachment-uploader.tsx
    types/

  presence/
    hooks/
      use-resource-presence.ts
      use-typing-presence.ts
    components/
      presence-stack.tsx
      typing-indicator.tsx
    store/
      presence-store.ts
    types/

  shared/
    types/
      resource-ref.types.ts
    utils/
      resource-ref.ts

  index.ts
```

Rules:

```txt
Collaboration components use ResourceRef.
Comments/Mentions/Reactions/Watchers/Attachments must support multiple resource types.
Notifications are not owned by collaboration UI.
Presence is realtime/client state, not normal server-state cache.
```

---

## 10. Notifications Final Architecture

Notifications is a user-facing feature, not the same as Outbox.

```txt
features/notifications/
  center/
    components/
      notification-center.tsx
      notification-list.tsx
      notification-item.tsx
      notification-empty-state.tsx

  bell/
    components/
      notification-bell.tsx
      notification-popover.tsx
      unread-count-badge.tsx

  preferences/
    api/
      notification-preferences.api.ts
    hooks/
      use-notification-preferences.ts
      use-update-notification-preferences.ts
    components/
      notification-preferences-form.tsx

  stream/
    hooks/
      use-notification-stream.ts
    model/
      notification-event.mapper.ts

  api/
    notification.api.ts
    notification.dto.ts

  hooks/
    queries/
      use-notifications.ts
      use-unread-count.ts
    mutations/
      use-mark-notification-read.ts
      use-mark-all-notifications-read.ts

  model/
    notification.mapper.ts

  types/
    notification.types.ts

  index.ts
```

Rules:

```txt
Notification bell and unread count belong here.
Notification stream should invalidate query keys, not duplicate backend logic.
Do not poll notifications every few seconds without explicit approval.
Outbox is backend infrastructure, not frontend notification feature.
```

---

## 11. Workspace Final Architecture

```txt
features/workspace/
  workspaces/
    api/
      workspace.api.ts
      workspace.dto.ts
    model/
      workspace.mapper.ts
    hooks/
      queries/
        use-workspaces.ts
        use-current-workspace.ts
        use-workspace-snapshot.ts
      mutations/
        use-create-workspace.ts
        use-update-workspace.ts
        use-delete-workspace.ts
    components/
      workspace-switcher.tsx
      workspace-card.tsx
      workspace-home-summary.tsx
    schemas/
    types/

  spaces/
    api/
      space.api.ts
    hooks/
    components/
    types/

  members/
    api/
      member.api.ts
    hooks/
      queries/
        use-workspace-members.ts
      mutations/
        use-update-member-role.ts
        use-remove-member.ts
    components/
      member-list.tsx
      member-row.tsx
      member-role-select.tsx
    types/

  invitations/
    api/
      invitation.api.ts
    hooks/
      queries/
        use-pending-invitations.ts
      mutations/
        use-invite-member.ts
        use-accept-invitation.ts
        use-revoke-invitation.ts
    components/
      invite-member-dialog.tsx
      pending-invitations-menu.tsx
    schemas/
    types/

  settings/
    components/
      workspace-management-panel.tsx
      workspace-general-settings.tsx
      workspace-danger-zone.tsx

  shared/
    utils/
      workspace-routes.ts
      workspace-view.ts
    types/

  index.ts
```

Rules:

```txt
Workspace must not fetch boards/docs directly inside workspace service.
Workspace home can compose other feature public components from app route.
Workspace feature owns members, invitations, spaces and workspace metadata.
```

---

## 12. Billing Final Architecture

Billing is not just pricing cards. It owns plan/subscription/entitlement UX.

```txt
features/billing/
  plans/
    api/
    hooks/
    components/
      plan-card.tsx
      plan-comparison-table.tsx
    types/

  subscriptions/
    api/
    hooks/
    components/
      subscription-status-card.tsx
      change-plan-dialog.tsx
      cancel-subscription-dialog.tsx
    types/

  invoices/
    api/
    hooks/
    components/
      invoice-list.tsx
      invoice-row.tsx
    types/

  entitlements/
    api/
    hooks/
    components/
      entitlement-gate.tsx
      usage-limit-banner.tsx
    types/

  usage/
    api/
    hooks/
    components/
      usage-meter.tsx
      usage-breakdown.tsx

  checkout/
    components/
      checkout-redirect.tsx
      checkout-result.tsx

  shared/
  index.ts
```

Rules:

```txt
Entitlement UI is owned by billing but can expose public guard components.
Feature modules may consume billing public API for UX gating, but backend remains source of truth.
Do not hard-code plan names in random components.
```

---

## 13. Governance Final Architecture

Governance owns permission administration and audit-facing access UX.

```txt
features/governance/
  roles/
    api/
    hooks/
    components/
      role-list.tsx
      role-editor.tsx
    types/

  permissions/
    api/
    hooks/
    components/
      permission-matrix.tsx
      permission-group.tsx
      permission-toggle.tsx
    types/

  policies/
    api/
    hooks/
    components/
      policy-editor.tsx
      resource-policy-panel.tsx
    types/

  audit-access/
    hooks/
    components/
      audit-access-log.tsx
      sensitive-action-log.tsx
    types/

  shared/
    permission-labels.ts
    resource-permission-map.ts

  index.ts
```

Rules:

```txt
Governance UI configures permissions.
lib/permissions evaluates current user capability for UI guards.
Do not mix governance admin screens with generic permission evaluator.
```

---

## 14. Automation Final Architecture

```txt
features/automation/
  rules/
    api/
    hooks/
    components/
      automation-rule-list.tsx
      automation-rule-card.tsx
    types/

  builder/
    components/
      automation-builder.tsx
      trigger-selector.tsx
      action-selector.tsx
      condition-builder.tsx
    model/
      automation-builder-state.ts

  triggers/
    components/
    types/

  actions/
    components/
    types/

  runs/
    api/
    hooks/
    components/
      automation-run-history.tsx
      automation-run-detail.tsx
    types/

  templates/
    api/
    hooks/
    components/

  index.ts
```

Rules:

```txt
Automation builder is UI orchestration, not backend rule execution.
Automation run history is server state.
Builder draft state can use local reducer/store.
```

---

## 15. Integrations Final Architecture

```txt
features/integrations/
  providers/
    api/
    hooks/
    components/
      provider-card.tsx
      provider-directory.tsx
    types/

  connections/
    api/
    hooks/
    components/
      connection-list.tsx
      connection-status.tsx
      disconnect-dialog.tsx
    types/

  oauth/
    components/
      oauth-callback.tsx
      oauth-error-state.tsx
    hooks/

  webhooks/
    api/
    hooks/
    components/
      webhook-list.tsx
      webhook-secret-viewer.tsx
    types/

  sync-jobs/
    api/
    hooks/
    components/
      sync-job-list.tsx
      sync-job-status.tsx
    types/

  shared/
  index.ts
```

Rules:

```txt
Provider catalog belongs to integrations.
OAuth callback page can live in app but compose integrations/oauth component.
Secrets must never be logged or stored in global UI state.
```

---

## 16. Search & Command System Architecture

```txt
features/search/
  global-search/
    api/
    hooks/
    components/
      global-search-dialog.tsx
      search-input.tsx
      search-results.tsx
      search-result-item.tsx
    types/

  command-palette/
    hooks/
      use-command-actions.ts
    components/
      command-palette.tsx
      command-group.tsx
      command-item.tsx
    model/
      command-registry.ts

  recent/
    hooks/
    components/
      recent-items.tsx

  shared/
    types/
      searchable-resource.types.ts
    utils/
      search-result-url.ts

  index.ts
```

Rules:

```txt
Search result rendering can delegate to feature public helpers, not deep internals.
Command palette is composition of registered commands.
Features can register commands through explicit public contracts, not by the search feature importing internals.
```

---

## 17. Activity Architecture

```txt
features/activity/
  feed/
    api/
    hooks/
    components/
      activity-feed.tsx
      activity-item.tsx
      activity-empty-state.tsx
    types/

  timeline/
    api/
    hooks/
    components/
      resource-timeline.tsx
      timeline-event.tsx
    types/

  model/
    activity.mapper.ts
    activity-message.formatter.ts

  shared/
  index.ts
```

Rules:

```txt
Activity is product-facing history.
Audit/security logs belong to governance-facing UX.
Activity items should include actor/resource snapshot display models.
```

---

## 18. Auth & Account Architecture

```txt
features/auth/
  api/
    auth.api.ts
    session.api.ts
  hooks/
    queries/
      use-auth-user.ts
    mutations/
      use-login.ts
      use-register.ts
      use-logout.ts
      use-forgot-password.ts
      use-reset-password.ts
  components/
    login-form.tsx
    register-form.tsx
    forgot-password-form.tsx
    reset-password-form.tsx
  schemas/
  types/
  model/
  index.ts

features/account/
  profile/
    api/
    hooks/
    components/
      profile-form.tsx
    schemas/
    types/
  security/
    api/
    hooks/
    components/
      change-password-form.tsx
      active-sessions-list.tsx
  preferences/
    api/
    hooks/
    components/
      account-preferences-form.tsx
  index.ts
```

Rules:

```txt
Auth owns authentication flow.
Account owns user profile/security/preferences.
Logout must clear query cache.
Do not store access tokens in localStorage.
```

---

## 19. Data Contract Strategy

Every feature must distinguish these types:

```txt
DTO          backend request/response shape
Model        frontend domain/view model
FormValues   form input shape
ViewModel    composed display model if different from model
ResourceRef  generic target reference for cross-resource features
```

Naming convention:

```txt
*.dto.ts
*.mapper.ts
*.types.ts
*.schema.ts
*.form.ts if needed
```

Example:

```txt
features/work-management/items/api/item.dto.ts
features/work-management/items/model/item.mapper.ts
features/work-management/items/types/item.types.ts
features/work-management/items/schemas/item.schema.ts
```

Rules:

```txt
Components consume frontend models, not raw DTOs.
Mappers are pure.
API files return DTOs or mapped models consistently per feature convention.
Forms use Zod schemas and explicit FormValues.
Enums from backend must be normalized in mapper/model layer.
Date/time parsing belongs in mapper/model layer.
```

---

## 20. Query Key Taxonomy

Query keys must be centralized and hierarchical.

Recommended shape:

```ts
queryKeys.auth.session()

queryKeys.account.profile()
queryKeys.account.securitySessions()

queryKeys.workspace.list()
queryKeys.workspace.detail(workspaceId)
queryKeys.workspace.snapshot(workspaceId)
queryKeys.workspace.members(workspaceId)
queryKeys.workspace.invitations(workspaceId)

queryKeys.workManagement.boards.list(workspaceId)
queryKeys.workManagement.boards.detail(workspaceId, boardId)
queryKeys.workManagement.boards.full(workspaceId, boardId)
queryKeys.workManagement.items.detail(workspaceId, boardId, itemId)
queryKeys.workManagement.fields.list(workspaceId, boardId)
queryKeys.workManagement.groups.list(workspaceId, boardId)

queryKeys.docs.pages.detail(workspaceId, pageId)
queryKeys.docs.pages.tree(workspaceId)
queryKeys.docs.blocks.list(workspaceId, pageId)

queryKeys.collaboration.comments(resourceRef)
queryKeys.collaboration.attachments(resourceRef)
queryKeys.collaboration.watchers(resourceRef)

queryKeys.notifications.list()
queryKeys.notifications.unreadCount()
queryKeys.notifications.preferences()

queryKeys.billing.subscription(workspaceId)
queryKeys.billing.plans()
queryKeys.billing.invoices(workspaceId)
queryKeys.billing.entitlements(workspaceId)

queryKeys.governance.roles(workspaceId)
queryKeys.governance.permissions(workspaceId)
queryKeys.governance.resourcePolicy(workspaceId, resourceRef)

queryKeys.automation.rules(workspaceId)
queryKeys.automation.runs(workspaceId, ruleId)

queryKeys.integrations.providers()
queryKeys.integrations.connections(workspaceId)

queryKeys.search.global(query)
queryKeys.activity.feed(workspaceId)
queryKeys.activity.resource(resourceRef)
```

Rules:

```txt
No hard-coded query keys in random files.
Mutations invalidate the smallest correct scope.
Optimistic updates must have rollback.
Realtime events should patch cache only if payload is complete; otherwise invalidate scoped keys.
```

---

## 21. State Architecture

### 21.1. Server state

TanStack Query owns:

```txt
session/user
workspace
members
boards/items/docs/comments/notifications
billing/governance/automation/integrations
```

### 21.2. URL state

Search params own:

```txt
board view
filters
sorts
groupBy
selected tab
panel mode
search query if shareable
```

### 21.3. Local component state

React state owns:

```txt
dialog open
hover
small temporary inputs
drag transient state
inline editor open state
```

### 21.4. Global client UI state

Zustand can own:

```txt
sidebar collapsed
command palette open
theme/color preference
local app shell state
transient presence if needed
```

Rules:

```txt
Never put server state in Zustand.
Never duplicate URL state into React state unless derived and synchronized by design.
Do not store sensitive account/session data in global UI stores.
```

---

## 22. Permission & Entitlement Architecture

### 22.1. Permission evaluator

```txt
lib/permissions/
  permissions.ts
  ability.ts
  use-can.ts
  permission-guard.tsx
```

UI should use:

```tsx
const canUpdate = useCan("board.update", {
  workspaceId,
  resourceType: "board",
  resourceId: boardId,
})
```

Never use:

```tsx
user.role === "Owner"
```

### 22.2. Permission matrix

```txt
workspace.create
workspace.update
workspace.delete
workspace.member.invite
workspace.member.remove
workspace.member.role.update
workspace.settings.update

board.create
board.view
board.update
board.delete
board.view.create
board.view.update
board.field.manage
board.group.manage

item.create
item.view
item.update
item.delete
item.move
item.assign
item.comment

page.create
page.view
page.update
page.delete
page.move

comment.create
comment.update
comment.delete
comment.resolve

attachment.upload
attachment.delete
attachment.download

billing.view
billing.manage

permission.view
permission.manage
role.manage
policy.manage

automation.view
automation.manage
integration.view
integration.manage
```

### 22.3. Entitlements

Billing owns entitlements, but features may consume public guard APIs:

```tsx
<EntitlementGate feature="automation.rules">
  <CreateAutomationButton />
</EntitlementGate>
```

Frontend gates are UX helpers. Backend remains source of truth.

---

## 23. Error, Loading and Access UX Architecture

Create generic states:

```txt
components/feedback/
  loading-state.tsx
  empty-state.tsx
  error-state.tsx
  permission-denied-state.tsx
  not-found-state.tsx
  conflict-state.tsx
  offline-state.tsx
```

Feature-specific states belong inside feature:

```txt
features/work-management/boards/components/board-empty-state.tsx
features/docs/pages/components/page-not-found-state.tsx
features/notifications/center/components/notification-empty-state.tsx
```

Status behavior:

| Status | UX |
|---:|---|
| 400 | request/form error |
| 401 | refresh once, redirect if failed |
| 403 | permission denied |
| 404 | not found |
| 409 | conflict/concurrency state |
| 422 | map server validation to form |
| 500 | error boundary + retry |

---

## 24. Realtime Architecture

Realtime is use-case driven.

```txt
Notifications -> SSE or websocket stream invalidating notification keys
Activity -> stream/invalidate resource activity
Presence -> realtime client state/store
Board item updates -> patch or invalidate board detail/full board
Docs editing -> editor-specific collaboration if required
```

Realtime files:

```txt
lib/realtime/
  realtime-client.ts
  realtime-events.ts
  use-realtime-subscription.ts

features/notifications/stream/hooks/use-notification-stream.ts
features/collaboration/presence/hooks/use-resource-presence.ts
features/work-management/boards/hooks/use-board-realtime.ts
features/docs/editor/hooks/use-doc-realtime.ts
```

Rules:

```txt
Realtime handlers must not duplicate backend business logic.
Patch cache only when payload is complete.
Otherwise invalidate scoped query keys.
Do not poll high-frequency state if a stream is required.
Presence is transient client/realtime state, not normal persisted query data.
```

---

## 25. Performance Architecture

Notrelix has heavy surfaces: tables, kanban, editor, search, timeline.

Required rules:

```txt
Large table/list surfaces must use virtualization/windowing when needed.
Board views must avoid remounting when switching view mode.
Heavy editors/charts/importers should use dynamic import where appropriate.
Do not place the entire workspace state in one React context.
Avoid broad providers that rerender the whole app.
Memoize renderer registries and field renderer mappings.
Use scoped query invalidation.
Avoid polling below 30 seconds without approval.
```

Performance-sensitive modules:

```txt
work-management/boards/components/views/table
work-management/boards/components/views/kanban
docs/editor
search/global-search
activity/feed
```

---

## 26. Observability and Telemetry Architecture

```txt
lib/telemetry/
  telemetry-client.ts
  events.ts
  web-vitals.ts
  error-reporter.ts
```

Track:

```txt
page view
route transition
mutation success/failure
API error category
frontend error boundary event
performance web vitals
feature usage
critical UX actions
```

Rules:

```txt
No PII in telemetry payloads.
No secrets/tokens in logs.
Include correlation/request ID if backend exposes one.
Feature modules emit typed telemetry events through lib/telemetry.
```

---

## 27. Testing Architecture

```txt
tests/
  architecture/
    import-boundaries.test.ts
    public-api.test.ts
    no-export-star.test.ts
    no-business-ui-in-components-ui.test.ts
  e2e/
  fixtures/
```

Required test levels:

```txt
Architecture tests
Mapper unit tests
Schema tests
Permission evaluator tests
Cache updater/rollback tests
Hook integration tests
Component tests for critical forms/surfaces
E2E tests for critical user journeys
Accessibility checks for important dialogs/forms/navigation
```

Critical E2E flows:

```txt
sign in
create workspace
invite member
create board
create item/card
switch board views
create doc/page
comment + mention + notification
billing entitlement gate
automation rule create if implemented
integration connection flow if implemented
```

---

## 28. Architecture Enforcement Rules

These must fail before merge:

```txt
lib imports features
components/ui imports features
features import app
app imports feature internals when not explicitly approved
feature index.ts uses export *
feature A imports feature B internals
server state stored in Zustand
role string hard-coded for permission decisions
raw fetch used in feature component/page for feature API
optimistic mutation without rollback
```

Temporary exceptions must be explicit:

```txt
Path:
Violation:
Reason:
Owner:
Removal phase:
```

No vague exception is allowed.

---

## 29. Public API Rules

Each feature has `index.ts`.

Public API can export:

```txt
Screen/business components app needs
Feature query/mutation hooks that are intentionally public
Public boundary types
Route/helper functions intentionally public
Guard/gate components intentionally public
```

Public API must not export:

```txt
Internal API services unless intentionally approved
Internal cache updaters
Internal mapper functions unless needed for tests/tooling
Internal view-specific private components
Entire hooks folder
Entire components folder
```

No `export *`.

---

## 30. Anti-Patterns

Do not implement:

```txt
components/ui/board-card.tsx
components/ui/notification-bell.tsx
app/page.tsx calling api.get directly
features/work-management/views/table as top-level bounded module
features/workspace importing work-management internals
Zustand storing boards/cards/pages from backend
hard-coded user.role === "Owner"
workspace.service.ts fetching boards/docs/notifications together
query keys hard-coded inside hooks
optimistic update without rollback
public API export-all barrels
feature folders that are just dumping grounds
```

---

## 31. Final PR Checklist

```txt
[ ] Does this change belong to app, feature, component, or lib?
[ ] Is the owning feature obvious?
[ ] Is business UI inside the owning feature?
[ ] Is route-private UI inside app/_components?
[ ] Is primitive UI business-free?
[ ] Are API calls inside feature api/ or lib/api only?
[ ] Are DTOs separated from frontend models?
[ ] Are mappers pure?
[ ] Are query keys from queryKeys factory?
[ ] Is mutation invalidation scoped?
[ ] Does optimistic update include rollback?
[ ] Is permission checked through useCan/PermissionGuard?
[ ] Is entitlement checked through billing public guard if needed?
[ ] Are imports through public API?
[ ] Are there no sibling feature deep imports?
[ ] Does this create a new route when search params would be correct?
[ ] Are loading/error/empty states consistent?
[ ] Is telemetry safe and non-PII?
[ ] Are tests updated for mapper/cache/schema/permission when needed?
[ ] Do architecture tests pass?
[ ] Does quality pass?
```

---

## 32. Final Architecture Contract

Notrelix frontend is enterprise-ready when:

```txt
A developer can identify the owning feature in under 30 seconds.
A route is composition, not business logic.
A business component lives next to its hooks/types/schemas.
A UI primitive never knows business concepts.
A feature can evolve without hunting files across the app tree.
Board views share the same work data model.
Docs editor state is separated from persisted page/block state.
Collaboration is resource-generic.
Notifications are user-facing, not confused with backend outbox.
Permission and entitlement gates are standardized.
Query keys and invalidation are predictable.
Architecture violations fail before merge.
```


# Notrelix Use Case Security Matrix

**Purpose:** Inventory every command/query with its scope, permission, transaction, cache, and subscription markers.

**Source folders scanned:**
- `src/Notrelix.Application/Common/CQRS/**`
- `src/Notrelix.Application/Features/**`

## P0 Risks

1. **67 RISK-level use cases** — workspace-scoped operations with NO scope/permission markers (especially Documents, Checklists, Labels, Comments).
2. **24 MISSING-level use cases** — have either workspace or permission marker, but not both; or use manual `IWorkspacePermissionService` without pipeline marker.
3. **Slug-based commands consistently lack markers** — `BySlug` variants are unguarded.
4. **Documents bounded context is most exposed** — all page and block operations lack any scope or permission marker.
5. **`IAccountRequest`, `IAuthorizedCacheableRequest`, `IRequireFeature`, `IRequireSubscription`, `IIdempotentRequest`** all defined but zero implementations.

---

## CQRS Marker Interfaces

| Interface | Purpose |
|---|---|
| `ICommand<T>` / `ICommand` | Command base |
| `IQuery<T>` | Query base |
| `IAccountRequest` | Account-scoped request (Guid AccountId) |
| `IWorkspaceRequest` | Workspace-scoped request (Guid WorkspaceId) |
| `IRequirePermission` | Requires authorization (PermissionAction Action, ResourceRef Resource) |
| `IRequireSubscription` | Requires active subscription (string? MinimumTier) |
| `IRequireFeature` | Requires feature entitlement (string FeatureCode, int Amount) |
| `ITransactionalRequest` | Runs in DB transaction |
| `IIdempotentRequest` | Idempotency support (string IdempotencyKey) |
| `ICacheableQuery<T>` | Public-cacheable query (string CacheKey, TimeSpan? Ttl) |
| `IAuthorizedCacheableRequest` | Private/auth-gated cacheable request (AuthorizedCacheKey, AuthorizedCacheTtl) |
| `IExpectedVersionRequest` | Optimistic concurrency (ResourceRef Resource, long ExpectedVersion) |
| `IRealtimeRequest` | Broadcast via SignalR (RealtimeTopic Topic) |
| `IRlsReadRequest` | RLS-protected DB read |
| `IMessageTriggeredRequest` | Triggered by async message |

---

## 1. Identity Bounded Context

### Auth

| # | Request | Scope | Permission | Transaction | Idempotent | Cache | Sub/Feature | Risk |
|---|---------|-------|------------|-------------|------------|-------|-------------|------|
| 1 | `LoginCommand` | none | none | Transactional | none | none | none | Safe |
| 2 | `LogoutCommand` | none | none | Transactional | none | none | none | Safe |
| 3 | `RefreshTokenCommand` | none | none | Transactional | none | none | none | Safe |
| 4 | `ForgotPasswordCommand` | none | none | none | none | none | none | Safe |
| 5 | `ResetPasswordCommand` | none | none | Transactional | none | none | none | Safe |
| 6 | `GetBootstrapQuery` | none | none | none | none | none | none | Safe |
| 7 | `GetCurrentUserQuery` | none | none | none | none | none | none | Safe |

### Registration

| # | Request | Scope | Permission | Transaction | Idempotent | Cache | Sub/Feature | Risk |
|---|---------|-------|------------|-------------|------------|-------|-------------|------|
| 8 | `RegisterCommand` | none | none | Transactional | none | none | none | Safe |
| 9 | `SendWelcomeEmailCommand` | none | none | Transactional | none | none | none | Safe (internal, IMessageTriggeredRequest) |

### Profiles

| # | Request | Scope | Permission | Transaction | Idempotent | Cache | Sub/Feature | Risk |
|---|---------|-------|------------|-------------|------------|-------|-------------|------|
| 10 | `UpdateProfileCommand` | none | none | Transactional | none | none | none | Safe (UserId from JWT) |

---

## 2. Workspaces Bounded Context

### Workspace CRUD

| # | Request | Scope | Permission | Transaction | Idempotent | Cache | Sub/Feature | Risk |
|---|---------|-------|------------|-------------|------------|-------|-------------|------|
| 11 | `CreateWorkspaceCommand` | none | none | Transactional | none | none | none | Safe |
| 12 | `ArchiveWorkspaceCommand` | Workspace | ManageWorkspace | Transactional | none | none | none | **Safe** |
| 13 | `ArchiveWorkspaceBySlugCommand` | none | none | Transactional | none | none | none | **RISK** |
| 14 | `RestoreWorkspaceCommand` | Workspace | ManageWorkspace | Transactional | none | none | none | **Safe** |
| 15 | `UpdateWorkspaceCommand` | Workspace | ManageWorkspace | Transactional | none | none | none | **Safe** |
| 16 | `GetUserWorkspacesQuery` | none | none | none | none | none | none | Safe |
| 17 | `GetWorkspaceQuery` | none | ViewWorkspace | none | none | none | none | **Missing** (no IWorkspaceRequest) |
| 18 | `GetWorkspaceBySlugQuery` | none | ViewWorkspace | none | none | none | none | **Missing** (no IWorkspaceRequest) |

### Members

| # | Request | Scope | Permission | Transaction | Idempotent | Cache | Sub/Feature | Risk |
|---|---------|-------|------------|-------------|------------|-------|-------------|------|
| 19 | `RemoveMemberCommand` | none | manual check | Transactional | none | none | none | **Missing** |
| 20 | `RemoveMemberBySlugCommand` | none | none | Transactional | none | none | none | **RISK** |
| 21 | `UpdateMemberRoleCommand` | none | manual check | Transactional | none | none | none | **Missing** |
| 22 | `UpdateMemberRoleBySlugCommand` | none | none | Transactional | none | none | none | **RISK** |
| 23 | `GetWorkspaceMembersQuery` | none | ViewMembers | none | none | none | none | **Missing** (no IWorkspaceRequest) |
| 24 | `GetWorkspaceMembersBySlugQuery` | none | ViewMembers | none | none | none | none | **Missing** (no IWorkspaceRequest) |

### Invitations

| # | Request | Scope | Permission | Transaction | Idempotent | Cache | Sub/Feature | Risk |
|---|---------|-------|------------|-------------|------------|-------|-------------|------|
| 25 | `InviteMemberCommand` | Workspace | ManageWorkspace | Transactional | none | none | none | **Safe** |
| 26 | `InviteMemberBySlugCommand` | none | none | Transactional | none | none | none | **RISK** |
| 27 | `CancelInvitationCommand` | none | none | Transactional | none | none | none | **RISK** |
| 28 | `AcceptInvitationCommand` | none | none | Transactional | none | none | none | Safe |
| 29 | `GetWorkspaceInvitationsQuery` | none | ViewWorkspace | none | none | none | none | **Missing** (no IWorkspaceRequest) |
| 30 | `GetUserPendingInvitationsQuery` | none | none | none | none | none | none | Safe |
| 31 | `GetInvitationByTokenQuery` | none | none | none | none | none | none | Safe |

### Activity & Provisioning

| # | Request | Scope | Permission | Transaction | Idempotent | Cache | Sub/Feature | Risk |
|---|---------|-------|------------|-------------|------------|-------|-------------|------|
| 32 | `GetWorkspaceActivityQuery` | none | ViewWorkspace | none | none | none | none | **Missing** (no IWorkspaceRequest) |
| 33 | `GetWorkspaceActivityBySlugQuery` | none | ViewWorkspace | none | none | none | none | **Missing** (no IWorkspaceRequest) |
| 34 | `ProvisionPersonalWorkspaceCommand` | none | none | Transactional | none | none | none | Safe (internal, IMessageTriggeredRequest) |

---

## 3. Governance Bounded Context

| # | Request | Scope | Permission | Transaction | Idempotent | Cache | Sub/Feature | Risk |
|---|---------|-------|------------|-------------|------------|-------|-------------|------|
| 35 | `GrantResourcePermissionCommand` | none | IRequirePermission (dynamic) | Transactional | none | none | none | Safe |
| 36 | `RevokeResourcePermissionCommand` | none | IRequirePermission (dynamic) | Transactional | none | none | none | Safe |
| 37 | `GetResourcePermissionsQuery` | none | IRequirePermission (dynamic) | none | none | none | none | Safe |
| 38 | `CreateShareLinkCommand` | none | IRequirePermission (dynamic) | Transactional | none | none | none | Safe |
| 39 | `DisableShareLinkCommand` | none | IRequirePermission (dynamic) | Transactional | none | none | none | Safe |

---

## 4. Collaboration Bounded Context

### Comments

| # | Request | Scope | Permission | Transaction | Idempotent | Cache | Sub/Feature | Risk |
|---|---------|-------|------------|-------------|------------|-------|-------------|------|
| 40 | `CreateCommentCommand` | none | none | Transactional | none | none | none | **RISK** |
| 41 | `UpdateCommentCommand` | none | none | Transactional | none | none | none | **Missing** |
| 42 | `DeleteCommentCommand` | none | none | Transactional | none | none | none | **Missing** |
| 43 | `ResolveCommentCommand` | none | none | Transactional | none | none | none | **Missing** |
| 44 | `GetCommentsQuery` | none | none | none | none | none | none | **RISK** |

### Attachments

| # | Request | Scope | Permission | Transaction | Idempotent | Cache | Sub/Feature | Risk |
|---|---------|-------|------------|-------------|------------|-------|-------------|------|
| 45 | `CreateBoardItemAttachmentCommand` | none | none | Transactional | none | none | none | **RISK** |
| 46 | `DeleteAttachmentCommand` | none | none | Transactional | none | none | none | **Missing** |
| 47 | `GetBoardItemAttachmentsQuery` | none | none | none | none | none | none | **RISK** |

### Activity

| # | Request | Scope | Permission | Transaction | Idempotent | Cache | Sub/Feature | Risk |
|---|---------|-------|------------|-------------|------------|-------|-------------|------|
| 48 | `GetResourceActivityQuery` | none | none | none | none | none | none | **RISK** |

---

## 5. Documents Bounded Context

### Pages (ALL RISK)

| # | Request | Scope | Permission | Transaction | Idempotent | Cache | Sub/Feature | Risk |
|---|---------|-------|------------|-------------|------------|-------|-------------|------|
| 49 | `CreatePageCommand` | none | none | Transactional | none | none | none | **RISK** |
| 50 | `UpdatePageCommand` | none | none | Transactional | none | none | none | **RISK** |
| 51 | `DeletePageCommand` | none | none | Transactional | none | none | none | **RISK** |
| 52 | `ArchivePageCommand` | none | none | none | none | none | none | **RISK** (stub) |
| 53 | `MovePageCommand` | none | none | none | none | none | none | **RISK** (stub) |
| 54 | `PublishPageCommand` | none | none | none | none | none | none | **RISK** (stub) |
| 55 | `SetPageDeadlineCommand` | none | none | none | none | none | none | **RISK** (stub) |
| 56 | `GetPageQuery` | none | none | none | none | none | none | **RISK** |
| 57 | `GetPageTreeQuery` | none | none | none | none | none | none | **RISK** |
| 58 | `GetPageBreadcrumbQuery` | none | none | none | none | none | none | **RISK** |
| 59 | `GetPageHistoryQuery` | none | none | none | none | none | none | **RISK** |
| 60 | `GetWorkspacePagesQuery` | none | none | none | none | none | none | **RISK** |
| 61 | `SearchPagesQuery` | none | none | none | none | none | none | **RISK** |

### Blocks (ALL RISK)

| # | Request | Scope | Permission | Transaction | Idempotent | Cache | Sub/Feature | Risk |
|---|---------|-------|------------|-------------|------------|-------|-------------|------|
| 62 | `CreateBlockCommand` | none | none | Transactional | none | none | none | **RISK** |
| 63 | `UpdateBlockCommand` | none | none | Transactional | none | none | none | **RISK** |
| 64 | `DeleteBlockCommand` | none | none | Transactional | none | none | none | **RISK** |
| 65 | `ReorderBlocksCommand` | none | none | Transactional | none | none | none | **RISK** |
| 66 | `BatchUpdateBlocksCommand` | none | none | Transactional | none | none | none | **RISK** |
| 67 | `GetPageBlocksQuery` | none | none | none | none | none | none | **RISK** |

---

## 6. Work Management Bounded Context

### Boards

| # | Request | Scope | Permission | Transaction | Idempotent | Cache | Sub/Feature | Risk |
|---|---------|-------|------------|-------------|------------|-------|-------------|------|
| 68 | `CreateBoardInWorkspaceCommand` | Workspace | CreateBoard | Transactional | none | none | none | **Safe** |
| 69 | `CreateBoardBySlugCommand` | none | none | Transactional | none | none | none | **RISK** |
| 70 | `UpdateBoardCommand` | Workspace | ManageBoard | Transactional | none | none | none | **Safe** |
| 71 | `ArchiveBoardCommand` | Workspace | ManageBoard | Transactional | none | none | none | **Safe** |
| 72 | `UnarchiveBoardCommand` | none | manual check | Transactional | none | none | none | **Missing** |
| 73 | `AddBoardMemberCommand` | none | manual check | Transactional | none | none | none | **Missing** |
| 74 | `RemoveBoardMemberCommand` | none | manual check | Transactional | none | none | none | **Missing** |
| 75 | `GetBoardQuery` | Workspace | ViewBoard | none | none | ICacheableQuery | none | **Safe** |
| 76 | `GetBoardsQuery` | Workspace | ViewWorkspace | none | none | none | none | **Safe** |
| 77 | `GetBoardsBySlugQuery` | Workspace | ViewWorkspace | none | none | none | none | **Safe** |
| 78 | `GetBoardMembersQuery` | Workspace | ViewBoard | none | none | none | none | **Safe** |
| 79 | `GetFullBoardQuery` | none | none | none | none | none | none | **RISK** |

### Board Schema & Fields

| # | Request | Scope | Permission | Transaction | Idempotent | Cache | Sub/Feature | Risk |
|---|---------|-------|------------|-------------|------------|-------|-------------|------|
| 80 | `GetBoardSchemaQuery` | Workspace | ViewBoard | none | none | ICacheableQuery | none | **Safe** |
| 81 | `CreateBoardFieldCommand` | none | manual check | Transactional | none | none | none | **Missing** |
| 82 | `UpdateBoardFieldCommand` | none | manual check | Transactional | none | none | none | **Missing** |
| 83 | `DeleteBoardFieldCommand` | none | manual check | Transactional | none | none | none | **Missing** |
| 84 | `ReorderBoardFieldsCommand` | none | manual check | Transactional | none | none | none | **Missing** |

### Board Groups

| # | Request | Scope | Permission | Transaction | Idempotent | Cache | Sub/Feature | Risk |
|---|---------|-------|------------|-------------|------------|-------|-------------|------|
| 85 | `CreateBoardGroupCommand` | none | manual check | Transactional | none | none | none | **Missing** |
| 86 | `UpdateBoardGroupCommand` | none | manual check | Transactional | none | none | none | **Missing** |
| 87 | `ArchiveBoardGroupCommand` | none | manual check | Transactional | none | none | none | **Missing** |
| 88 | `UnarchiveBoardGroupCommand` | none | manual check | Transactional | none | none | none | **Missing** |
| 89 | `DuplicateBoardGroupCommand` | none | none | Transactional | none | none | none | **RISK** |
| 90 | `ReorderBoardGroupsCommand` | none | manual check | Transactional | none | none | none | **Missing** |

### Board Items

| # | Request | Scope | Permission | Transaction | Idempotent | Cache | Sub/Feature | Risk |
|---|---------|-------|------------|-------------|------------|-------|-------------|------|
| 91 | `CreateBoardItemCommand` | Workspace | CreateItem | Transactional | none | none | none | **Safe** |
| 92 | `UpdateBoardItemCommand` | none | manual check | Transactional | none | none | none | **Missing** |
| 93 | `ArchiveBoardItemCommand` | none | manual check | Transactional | none | none | none | **Missing** |
| 94 | `MoveBoardItemCommand` | Workspace | MoveItem | Transactional | none | none | none | **Safe** |
| 95 | `DuplicateBoardItemCommand` | none | none | Transactional | none | none | none | **RISK** |
| 96 | `AssignBoardItemMemberCommand` | Workspace | AssignItem | Transactional | none | none | none | **Safe** |
| 97 | `UnassignBoardItemMemberCommand` | none | none | Transactional | none | none | none | **RISK** |
| 98 | `UpdateBoardItemFieldValueCommand` | Workspace | UpdateItem | Transactional | none | none | none | **Safe** |
| 99 | `UpdateBoardItemFieldValuesCommand` | none | manual check | Transactional | none | none | none | **Missing** |
| 100 | `UpdateBoardItemStatusCommand` | none | manual check | Transactional | none | none | none | **Missing** |
| 101 | `SetBoardItemDueDateCommand` | none | manual check | Transactional | none | none | none | **Missing** |
| 102 | `LinkPageToBoardItemCommand` | none | manual check | Transactional | none | none | none | **Missing** |
| 103 | `UnlinkPageFromBoardItemCommand` | none | manual check | Transactional | none | none | none | **Missing** |
| 104 | `GetBoardItemQuery` | none | none | none | none | none | none | **RISK** |
| 105 | `GetBoardItemsQuery` | Workspace | ViewBoard | none | none | ICacheableQuery | none | **Safe** |
| 106 | `GetMyBoardItemsQuery` | none | none | none | none | none | none | **RISK** (stub) |

### Board Views

| # | Request | Scope | Permission | Transaction | Idempotent | Cache | Sub/Feature | Risk |
|---|---------|-------|------------|-------------|------------|-------|-------------|------|
| 107 | `CreateBoardViewCommand` | Workspace | CreateBoardView | Transactional | none | none | none | **Safe** |
| 108 | `SaveBoardViewCommand` | Workspace | ViewBoard | Transactional | none | none | none | **Safe** |
| 109 | `DeleteBoardViewCommand` | none | none | Transactional | none | none | none | **Missing** |
| 110 | `UpdateBoardViewConfigCommand` | Workspace | UpdateBoardView | Transactional | none | none | none | **Safe** |
| 111 | `GetBoardViewQuery` | Workspace | ViewBoard | none | none | none | none | **Safe** |

### Checklists (ALL RISK)

| # | Request | Scope | Permission | Transaction | Idempotent | Cache | Sub/Feature | Risk |
|---|---------|-------|------------|-------------|------------|-------|-------------|------|
| 112 | `CreateChecklistCommand` | none | none | Transactional | none | none | none | **RISK** |
| 113 | `CreateChecklistItemCommand` | none | none | Transactional | none | none | none | **RISK** |
| 114 | `UpdateChecklistCommand` | none | none | Transactional | none | none | none | **RISK** |
| 115 | `UpdateChecklistItemCommand` | none | none | Transactional | none | none | none | **RISK** |
| 116 | `DeleteChecklistCommand` | none | none | Transactional | none | none | none | **RISK** |
| 117 | `DeleteChecklistItemCommand` | none | none | Transactional | none | none | none | **RISK** |
| 118 | `ToggleChecklistItemCommand` | none | none | none | none | none | none | **RISK** |
| 119 | `GetChecklistsQuery` | none | none | none | none | none | none | **RISK** |

### Labels (ALL RISK)

| # | Request | Scope | Permission | Transaction | Idempotent | Cache | Sub/Feature | Risk |
|---|---------|-------|------------|-------------|------------|-------|-------------|------|
| 120 | `CreateLabelCommand` | none | none | Transactional | none | none | none | **RISK** |
| 121 | `UpdateLabelCommand` | none | none | Transactional | none | none | none | **RISK** |
| 122 | `DeleteLabelCommand` | none | none | Transactional | none | none | none | **RISK** |
| 123 | `AddLabelToBoardItemCommand` | none | none | Transactional | none | none | none | **RISK** |
| 124 | `RemoveLabelFromBoardItemCommand` | none | none | Transactional | none | none | none | **RISK** |
| 125 | `GetLabelsQuery` | none | none | none | none | none | none | **RISK** |

### Item Links (ALL RISK — stubs)

| # | Request | Scope | Permission | Transaction | Idempotent | Cache | Sub/Feature | Risk |
|---|---------|-------|------------|-------------|------------|-------|-------------|------|
| 126 | `CreateBoardItemLinkCommand` | none | none | none | none | none | none | **RISK** |
| 127 | `DeleteBoardItemLinkCommand` | none | none | none | none | none | none | **RISK** |

---

## 7. Automation Bounded Context

| # | Request | Scope | Permission | Transaction | Idempotent | Cache | Sub/Feature | Risk |
|---|---------|-------|------------|-------------|------------|-------|-------------|------|
| 128 | `CreateAutomationRuleCommand` | none | manual check | Transactional | none | none | none | **Missing** |
| 129 | `SetAutomationRuleEnabledCommand` | none | manual check | Transactional | none | none | none | **Missing** |
| 130 | `GetWorkspaceAutomationsQuery` | none | manual check | none | none | none | none | **Missing** |
| 131 | `GetAutomationExecutionsQuery` | none | manual check | none | none | none | none | **Missing** |

---

## 8. Integrations Bounded Context

| # | Request | Scope | Permission | Transaction | Idempotent | Cache | Sub/Feature | Risk |
|---|---------|-------|------------|-------------|------------|-------|-------------|------|
| 132 | `ConnectCalendarCommand` | none | none | none | none | none | none | **RISK** (stub) |
| 133 | `DisconnectCalendarCommand` | none | none | none | none | none | none | **RISK** (stub) |
| 134 | `HandleCalendarWebhookCommand` | none | none | none | none | none | none | Safe (external) |
| 135 | `TriggerCalendarSyncCommand` | none | none | none | none | none | none | **RISK** (stub) |
| 136 | `HandleN8nCallbackCommand` | none | none | Transactional | none | none | none | Safe |

---

## Summary

| Risk Level | Count | Description |
|------------|-------|-------------|
| **Safe** | 45 | Fully guarded (scope + permission markers) |
| **Missing** | 24 | Has either scope OR permission but not both; or uses manual permission service |
| **RISK** | 67 | No scope/permission markers at all |
| **Total** | **136** | All use cases |

### RISK by bounded context

| Context | Count | Notes |
|---------|-------|-------|
| Documents | 19 | All pages + blocks unguarded |
| WorkManagement | 27 | Checklists, labels, item links, slug variants |
| Collaboration | 9 | Comments, attachments, activity |
| Workspaces | 6 | Slug-based commands |
| Integrations | 4 | Stubs |
| **Total RISK** | **67** | |

### Key observations

1. **`IIdempotentRequest`** — 0 implementations
2. **`IRequireSubscription` / `IRequireFeature`** — 0 implementations
3. **`IAccountRequest`** — 0 implementations (defined but unused)
4. **`IAuthorizedCacheableRequest`** — 0 implementations
5. **Slug-based commands** — consistently missing all markers
6. **25 handlers use `IWorkspacePermissionService` manually** — bypassing pipeline authorization

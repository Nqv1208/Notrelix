# Security, Tenancy, and Permissions Rules

## 1. Security model

Notrelix is multi-tenant. Every use case must respect account/workspace/resource boundaries.

Security is not an API-only concern. Authorization must happen in Application pipeline.

## 2. Scope types

### Global request

No account/workspace/resource scope.

Examples:

```txt
Login
Register
Start OAuth login
Public plan catalog
```

### Account-scoped request

Has account boundary but no workspace boundary.

Examples:

```txt
Account settings
Billing account overview
Account members overview
```

### Workspace-scoped request

Has explicit workspace id.

Examples:

```txt
Create board in workspace
List boards in workspace
Update workspace settings
```

### Resource-scoped request

Only has resource id; system resolves account/workspace from resource.

Examples:

```txt
GET /boards/{boardId}
PATCH /items/{itemId}
POST /comments/{commentId}/resolve
```

Resource-scoped requests must be treated as workspace-scoped security boundary.

## 3. Permission rules

Workspace/resource/account scoped request must implement `IRequirePermission`, unless it is an explicitly whitelisted `ISystemInternalRequest`.

The permission declaration must include:

```txt
ResourceRef Resource
PermissionAction Action
```

Example:

```csharp
public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, BoardId);
public PermissionAction Action => PermissionAction.UpdateBoard;
```

Do not use fake broad permission like `ManageWorkspace` unless the use case truly requires workspace-level management.

### ResourceRef convention

| Resource Type | Ref Pattern | Example |
|---|---|---|
| Workspace | `ResourceRef.Create(ResourceType.Workspace, workspaceId, workspaceId)` | `ManageWorkspace` |
| Board | `ResourceRef.Create(ResourceType.Board, boardId, workspaceId)` | `ManageBoard` |
| BoardItem | `ResourceRef.Create(ResourceType.BoardItem, itemId, workspaceId)` | `ManageItem` |
| Page | `ResourceRef.Create(ResourceType.Page, pageId, workspaceId)` | `ManagePage` |
| Comment | `ResourceRef.Create(ResourceType.Comment, commentId, workspaceId)` | `ManageComment` |
| Attachment | `ResourceRef.Create(ResourceType.Attachment, attachmentId, workspaceId)` | `ManageAttachment` |

Rules:
- `ResourceRef` always includes `WorkspaceId` as the third argument.
- `PermissionAction` must match the action being performed (View/Manage/Delete/Create).
- Handlers must not duplicate the permission check — pipeline handles it.

## 4. Resource not found vs forbidden

When permission evaluation determines resource does not exist or user cannot know it exists, return not found instead of leaking existence.

Rule:

```txt
Cross-tenant resource access should not reveal whether resource exists.
```

## 5. Current request context

Handlers must use:

```csharp
ICurrentRequestContext
```

when they need current user/account/workspace.

Example:

```csharp
var actorId = _requestContext.RequireUserId();
var accountId = _requestContext.RequireAccountId();
var workspaceId = _requestContext.RequireWorkspaceId();
```

Do not inject `ICurrentTenantContext` directly into handlers.

`ICurrentTenantContext` is reserved for:

```txt
TenantBootstrapBehavior
ResourceScopeBehavior
DbRequestScopeBehavior
AuthorizationBehavior if needed
ApplicationDbContext filters
RLS session services
System context scopes
Background/consumer tenant wrappers
Infrastructure runtime
```

## 6. System internal request

`ISystemInternalRequest` is an escape hatch, not a convenience marker.

Rules:

```txt
Must be whitelist-reviewed.
Must not be used by normal user-facing endpoint.
Must be documented.
Must have tests proving it cannot be called as public user flow.
```

## 7. Tenant/RLS rules

- Tenant context must be resolved before handler reads tenant-scoped data.
- RLS session must be applied inside DB scope for tenant-scoped reads/writes.
- Raw SQL and `IgnoreQueryFilters` require explicit whitelist and tests.
- System context must be audited and never used to bypass normal user permission casually.

## 8. Security tests required

For every important resource type:

```txt
User A workspace A cannot read resource from workspace B.
User A workspace A cannot update resource from workspace B.
List query does not include workspace B data.
Resource-scoped request resolves tenant before authorization.
Denied request does not use cached response.
```

Minimum resource set:

```txt
Board
BoardItem
BoardField
Comment
Page
ShareLink
Search projection if active
```

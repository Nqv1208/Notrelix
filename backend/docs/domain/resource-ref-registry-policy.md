# ResourceRef Registry Policy

`ResourceRef` is the safe polymorphic reference for collaboration, governance,
automation, activity, notifications, and documents. It must be backed by a
registry of valid resource types and capabilities.

## Registry Row

Every `ResourceType` used by `ResourceRef`, `ResourcePermission`, `ShareLink`,
comments, watchers, activity, automation, or search must have:

| Field | Meaning |
|---|---|
| `Code` | Enum/member name or stable external code. |
| `OwnerContext` | Owning bounded context. |
| `WorkspaceScoped` | Whether the resource belongs to one workspace. |
| `AggregateRootType` | Aggregate root when applicable. |
| `Commentable` | Comments may target it. |
| `Shareable` | Share links may target it. |
| `Watchable` | Watchers/notifications may subscribe. |
| `PermissionProtected` | Governance evaluates permissions. |
| `PermissionEvaluator` | Required evaluator or policy owner. |
| `SearchIndexed` | Search projection may index it. |

## Initial Valid Resource Types

| Resource type | Owner context | Workspace scoped | Commentable | Shareable | Watchable | Permission protected |
|---|---|---:|---:|---:|---:|---:|
| `Workspace` | Workspaces | root | no | no | no | yes |
| `WorkspaceMember` | Workspaces | yes | no | no | no | yes |
| `Space` | Workspaces | yes | no | no | yes | yes |
| `Board` | WorkManagement | yes | yes | yes | yes | yes |
| `BoardGroup` | WorkManagement | yes | no | no | no | yes |
| `BoardField` | WorkManagement | yes | no | no | no | yes |
| `BoardItem` | WorkManagement | yes | yes | yes | yes | yes |
| `BoardView` | WorkManagement | yes | no | yes | no | yes |
| `Page` | Documents | yes | yes | yes | yes | yes |
| `Block` | Documents | yes | yes | no | no | yes |
| `DocumentVersion` | Documents | yes | no | no | no | yes |
| `Comment` | Collaboration | yes | no | no | no | yes |
| `Attachment` | Collaboration | yes | no | no | no | yes |
| `AutomationRule` | Automation | yes | no | no | yes | yes |
| `AutomationExecution` | Automation | yes | no | no | no | yes |
| `IntegrationConnection` | Integrations | yes | no | no | no | yes |
| `Dashboard` | Analytics | yes | yes | yes | yes | yes |
| `Subscription` | Billing | yes | no | no | no | admin/system |
| `Entitlement` | Billing | yes | no | no | no | admin/system |
| `User` | Identity | global | no | no | no | identity/security |

Types such as `Notification`, `ActivityLog`, `PresenceSession`,
`UnreadCounter`, search documents, outbox records, job locks, and idempotency
keys must not become generic `ResourceRef` targets unless a rulebook explicitly
promotes them.

## Prohibited Polymorphic Targets

Do not allow `ResourceRef` to target:

- provider SDK objects;
- webhook delivery attempts;
- outbox messages;
- processed-event records;
- idempotency keys;
- job locks;
- search index documents;
- raw files or binary objects;
- runtime websocket connections;
- unregistered `External` resources without an integration-specific wrapper.

## Permission Evaluator Ownership

Permission evaluation belongs to Governance. Contexts may expose facts needed by
Governance, but they must not duplicate evaluator precedence.

Default precedence:

1. system override;
2. workspace owner/admin;
3. explicit deny;
4. direct resource permission;
5. field/resource-specific permission;
6. role permission;
7. inherited permission;
8. share link/public grant;
9. default deny.

Any resource type marked `PermissionProtected` must name a Governance evaluator
or document why the standard evaluator is enough.

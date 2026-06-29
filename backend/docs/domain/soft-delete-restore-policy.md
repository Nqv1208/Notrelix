# Soft Delete and Restore Policy

`SoftDeletableEntity` provides mechanics only. Each aggregate must define its
business semantics for delete and restore.

## Restore Classifications

| Classification | Meaning |
|---|---|
| Restorable | Normal restore is allowed when parent scope is valid. |
| Non-restorable | Delete is final except purge/admin tooling. |
| Parent-validated restore | Parent workspace/page/board/resource must be active. |
| System-only restore | Only system/admin use case may restore. |
| Cascade-aware restore | Restore requires subtree/dependent resource policy. |

## Default Rules

- Soft delete is a lifecycle mutation: update audit, increment version, classify
  event, and reject further normal mutations through `EnsureNotDeleted`.
- Restore is a lifecycle mutation: clear deleted state, update audit, increment
  version, classify event, and validate parent/resource scope when needed.
- Repeated soft delete and repeated restore may be no-op if documented and
  tested.
- Delete/restore must not cascade implicitly inside an aggregate unless that
  cascade is part of the aggregate boundary.

## Notrelix Aggregate Policies

| Aggregate | Soft delete policy | Restore policy |
|---|---|---|
| `Workspace` | Sets `WorkspaceStatus.SoftDeleted`; no normal user operations. | System/admin restore to active; dependent resources remain scoped and inaccessible while deleted. |
| `WorkspaceMember` | Sets member removed state; must protect last owner through supplied owner count. | Restores to active; parent workspace must be valid in Application. |
| `WorkspaceInvitation` | Deletes invitation record; accept/revoke must reject deleted invitation. | Restore policy should be audited before use. |
| `Board` | Deleted board rejects mutations. | Parent workspace/space should be active before Application restores. |
| `BoardItem` | Deleted item rejects value/group/timeline mutations. | Parent board/group should be active before Application restores. |
| `BoardField` | System fields cannot be deleted. | Parent board should be active; restoring deleted system field needs audit. |
| `Page` | Sets `PageStatus.SoftDeleted`. | Parent page/workspace must be active; current Domain restores to active and needs parent policy audit. |
| `Block` | Deleted block rejects content/properties/move. | Parent page/block should be active before Application restores. |
| `Comment` | Sets comment soft-deleted status. | Target resource should be active/commentable before Application restores. |
| `ResourcePermission` | Revocation policy must decide whether soft-delete event or revoke event is canonical. | Restoring permission must respect registered resource and subject validity. |
| `Subscription` | Delete should be admin/system lifecycle, not payment cancellation. | Restore must not bypass canceled/expired billing state without explicit policy. |
| `Entitlement` | Delete should disable access by `IsActiveAt`. | Restore must preserve or recompute entitlement status deliberately. |

## Archived Parent Behavior

When a workspace, board, page, or custom role is archived:

- reads may remain allowed;
- normal content mutations are denied unless documented;
- delete/restore may be admin/system only;
- dependent resource creation is denied by Application readiness gate.

Domain methods on child aggregates should enforce their own archived/deleted
state. Parent archived/deleted checks are loaded and enforced by Application
using this policy unless the parent state is passed into a pure Domain rule.

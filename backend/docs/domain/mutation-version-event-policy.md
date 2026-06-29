# Mutation, Version, and Event Policy

Every state-changing Domain method must be classified before Application relies
on it. The default rule is strict: persistent state change means invariant
validation, audit update, version increment, event classification, and tests.

## Mutations That Require Version Increment

Increment aggregate version when persistent aggregate state changes:

- root scalar changes: rename, description/background update, visibility change;
- lifecycle changes: archive, unarchive, soft delete, restore, activate,
  suspend, revoke, expire, cancel;
- child entity changes through the root: field options, role permissions, item
  values, execution steps;
- sequence changes: `Board.GenerateNextItemIdentity`;
- workspace-scoped access changes: member role/status, resource permission
  grants or revocations;
- billing access changes: entitlement limit/status and subscription state.

No version increment when:

- the method is a documented idempotent no-op because state is already equal;
- the method only calculates a value;
- the method changes a projection, cache, or ops record outside Domain.

Known audit targets:

- `WorkspaceInvitation.Accept`, `Expire`, and `Revoke` mutate status and should
  be audited for missing version increments.
- `ResourcePermission.Revoke` calls `SoftDelete` and also increments version,
  so the intended single-transition policy must be decided.

## Mutations That Require Audit Update

Update audit fields when persistent state changes and an actor or system action
caused the change.

Use `SetAuditOnUpdate(actorUserId, occurredAt)` for user/system mutations.
Use `actorUserId = null` only for documented system-time transitions such as
expiry jobs.

Examples:

- user-driven changes use a non-empty actor: board rename, page move, comment
  update, permission grant, entitlement revoke;
- system transitions may use null actor: entitlement expiry, invitation expiry,
  share-link expiry;
- create methods must call `SetAuditOnCreate` unless the audit explicitly states
  that the model is an append-only event/projection without creator semantics.

Known audit targets:

- `Entitlement.Create` currently raises a grant event but does not set create
  audit fields.
- `Subscription.SoftDelete` and `Entitlement.SoftDelete` rely on base state but
  should be audited for explicit update audit consistency.

## Mutations That Require Domain Event

Emit a domain event when the state change is a meaningful business fact that
other in-process or durable policies may consume.

Examples:

- `BoardCreated`, `BoardRenamed`, `BoardArchived`;
- `BoardItemFieldValueChanged`;
- `PageMoved`, `BlockContentUpdated`;
- `CommentCreated`, `CommentResolved`;
- `WorkspaceMemberRoleChanged`;
- `SubscriptionChanged`, `EntitlementLimitChanged`.

Do not add events just because a setter changed. Classify first.

## Audit-Only Mutations

Audit-only changes are security/compliance facts that should be traceable but
should not automatically become durable product events.

Examples to classify:

- `UserPasswordChanged`;
- `UserLoggedIn`;
- OAuth token reference rotation;
- MFA requirement changes;
- security login attempts.

These may still have Domain events today, but the event classification must
state whether dispatch should be ignored, local, durable, or integration-ready.

## Internal-State-Only Mutations

Internal state only changes should not emit broad domain events unless a
downstream rule needs them.

Examples:

- normalized cache flags;
- computed mirror/rollup snapshots;
- runtime presence heartbeat;
- retry counters and worker progress;
- search index status.

These usually belong outside core Domain.

## Idempotent Mutation Behavior

Idempotent no-op behavior must be intentional and tested.

Allowed no-op examples:

- archiving an already archived board;
- restoring a non-deleted entity;
- scheduling cancellation when already scheduled;
- setting the same field value;
- changing to the same role or status.

No-op methods must not update audit fields, increment version, or emit events.
If duplicate input should fail instead of no-op, document and test that policy.

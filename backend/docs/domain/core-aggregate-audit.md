# Notrelix Core Aggregate Audit

This audit implements Slice D2 of the Domain hardening plan. It records current
Domain behavior before any production Domain hardening in D5. `Current Status`
uses:

- `OK`: current behavior matches policy from D0 docs.
- `Gap`: current behavior likely violates policy and needs D3 tests before D5.
- `Classify`: behavior exists, but event/audit/version policy must be decided.
- `Follow-up`: outer-layer check is required; do not change Domain yet.

## Aggregate: User

### Bounded Context

Identity.

### Responsibility

Global user identity, profile basics, email/password lifecycle, status
transitions, login timestamp, OAuth account references.

### Owns

Email, normalized email, display name, avatar reference, password hash value,
status, last login timestamp, owned OAuth account references.

### Does Not Own

Workspace membership, sessions, refresh tokens, MFA devices, provider password
hashing/verification, authorization decisions.

### Invariants

- Email and password hash are required at creation.
- Email is normalized in Domain.
- Deleted users reject normal mutations.
- OAuth provider cannot be linked to a different provider id once linked.

### Mutating Methods

| Method | State Change | Required Invariant | Version Required | Event Required | Audit Required | Current Status | Action |
|---|---|---|---|---|---|---|---|
| `Create` | Creates active user | valid email/name/hash | no | `UserRegisteredDomainEvent` | create audit | OK | Keep. |
| `UpdateProfile` | name/avatar | not deleted, non-empty name | yes | profile updated | update audit | OK | Add rule test for version. |
| `UpdateEmail` | email/normalized email | not deleted, valid email | yes | email changed | update audit | OK | Add rule test. |
| `UpdatePassword` | password hash | not deleted, non-empty hash | yes | security/audit classified | update audit | Classify | Decide durable vs audit-only in D3/D4. |
| `RecordLogin` | last login | not deleted | yes? | telemetry/audit classified | update audit | Classify | Decide if frequent login should be audit-only/no durable dispatch. |
| `Activate` | status active | not deleted, actor | yes if changed | activated | update audit | OK | Add no-op/version tests. |
| `Deactivate` | status inactive | not deleted, actor | yes if changed | deactivated | update audit | OK | Add state rule test. |
| `Suspend` | status suspended | not deleted, actor | yes if changed | suspended | update audit | OK | Add state rule test. |
| `LinkOAuthAccount` | OAuth account/token | not deleted, provider id invariant | yes | linked | update audit | OK | Add duplicate provider test. |
| `UnlinkOAuthAccount` | remove OAuth account | not deleted | yes if exists | unlinked | update audit | OK | Add no-op test. |
| `RotateOAuthToken` | token reference | account exists | yes | token rotated/security | update audit | Classify | Decide ops/security event dispatch. |
| `SoftDelete` | deleted state | idempotent | yes if changed | soft deleted | update audit | OK | Add version no-op tests. |
| `Restore` | restored state | only when deleted | yes if changed | restored | update audit | OK | Add version no-op tests. |

### Domain Events

Identity events exist for registration, profile/email/password/status/login,
OAuth link/unlink/rotate, delete/restore. Password/login/OAuth token rotation
need security/audit classification before durable dispatch expansion.

### Cross-Aggregate References

None by full aggregate. Workspace membership is outside User.

### Restore/Delete Policy

Restorable. Normal mutations are rejected after soft delete.

### Risks

User can become too broad if sessions, MFA, login attempts, and security
settings are merged into it. Keep related aggregates separate.

### Required Tests

Version/event/no-op tests for profile/email/status/delete/restore; security
classification tests for password/login/OAuth token rotation.

## Aggregate: Workspace

### Bounded Context

Workspaces.

### Responsibility

Tenant root metadata, lifecycle, workspace settings, personal/account flags.

### Owns

Name, slug, description, status, settings, personal flag, account id.

### Does Not Own

Members, invitations, boards, documents, billing, permissions, activity.

### Invariants

- Workspace is tenant root and does not implement `IWorkspaceScoped`.
- Name, slug, and owner id are required at creation.
- Archived workspace rejects rename/settings update.
- Soft-deleted workspace rejects normal mutations through `EnsureNotDeleted`.

### Mutating Methods

| Method | State Change | Required Invariant | Version Required | Event Required | Audit Required | Current Status | Action |
|---|---|---|---|---|---|---|---|
| `Create` | creates active workspace | owner/name/slug | no | created | create audit | OK | Keep. |
| `AssignToAccount` | account id | not deleted, non-empty account | yes if changed | classify | update audit | Classify | Decide if account assignment event is required. |
| `Rename` | name | not deleted, not archived | yes if changed | renamed | update audit | OK | Add no-op version test. |
| `Archive` | status archived | not deleted | yes if changed | archived | update audit | OK | Keep. |
| `SoftDelete` | status soft deleted/deleted state | idempotent | yes if changed | soft deleted | update audit | OK | Keep. |
| `Restore` | status active/restored state | only when deleted | yes if changed | restored | update audit | OK | Parent/system policy belongs to Application. |
| `UpdateSettings` | settings | not deleted, not archived | yes | classify | update audit | Classify | Decide settings-updated event policy. |

### Domain Events

Workspace create/rename/archive/delete/restore events exist. Account assignment
and settings updates are unclassified and emit no event today.

### Cross-Aggregate References

References account by `AccountId` only.

### Restore/Delete Policy

Workspace soft delete makes tenant inaccessible. Restore is admin/system use
case; child resources remain scoped but are inaccessible while workspace is
deleted.

### Risks

Application may bypass `WorkspaceFactory.CreateWithOwner` and create a
workspace without owner membership. That is an Application follow-up, not a D2
Domain production change.

### Required Tests

Archive guards, settings/account event classification, owner factory contract,
version/no-op tests.

## Aggregate: WorkspaceMember

### Bounded Context

Workspaces.

### Responsibility

Membership lifecycle and workspace role/status transitions.

### Owns

Workspace id, user id, workspace role, member status.

### Does Not Own

User identity, workspace metadata, permission evaluator cache.

### Invariants

- Workspace id, user id, and actor are required at creation.
- Role change requires active member.
- Last-owner downgrade/suspend/remove is protected by supplied owner count.
- Removed member cannot be activated directly; restore is required.

### Mutating Methods

| Method | State Change | Required Invariant | Version Required | Event Required | Audit Required | Current Status | Action |
|---|---|---|---|---|---|---|---|
| `Create` | active membership | workspace/user/actor | no | added | create audit | OK | Keep. |
| `ChangeRole` | role | active, last-owner rule | yes if changed | role changed | update audit | OK | Add no-op/version tests. |
| `Suspend` | status suspended | last-owner rule | yes if changed | suspended | update audit | OK | Keep. |
| `Activate` | status active | not removed | yes if changed | activated | update audit | OK | Add invalid removed test if missing. |
| `Remove` | delegates soft delete | last-owner rule | yes if changed | removed | update audit | OK | Keep. |
| `SoftDelete` | status removed/deleted | actor, idempotent | yes if changed | removed | update audit | OK | Keep. |
| `Restore` | status active/restored | actor, only deleted | yes if changed | restored | update audit | OK | Parent workspace active check is Application. |

### Domain Events

Added, role changed, suspended, activated, removed, restored events exist.

### Cross-Aggregate References

References `WorkspaceId` and `UserId` only. Owner count is supplied by
Application.

### Restore/Delete Policy

Restorable. Restore should be rejected by Application if workspace is archived
or deleted.

### Risks

Membership uniqueness is cross-aggregate/persistence concern and needs
Application/Infrastructure enforcement.

### Required Tests

Last-owner rule tests, version increment tests, no-op tests, removed activation
guard.

## Aggregate: WorkspaceInvitation

### Bounded Context

Workspaces.

### Responsibility

Workspace invite lifecycle from pending to accepted, expired, or revoked.

### Owns

Workspace id, normalized invite email, role, token hash, status, expiry,
inviting actor.

### Does Not Own

Email delivery, token generation, user creation, member creation.

### Invariants

- Workspace id, email, token, and inviter are required.
- Expiry must be positive.
- Accept requires pending and not expired.
- Revoke requires pending and not expired.
- Deleted invitation rejects accept/revoke/expire.

### Mutating Methods

| Method | State Change | Required Invariant | Version Required | Event Required | Audit Required | Current Status | Action |
|---|---|---|---|---|---|---|---|
| `Create` | pending invite | valid email/token/expiry | no | created | create audit | OK | Keep. |
| `Accept` | status accepted | pending, not expired, actor | yes | accepted | update audit | Gap | Add D3 failing test for missing version increment before D5. |
| `Expire` | status expired | pending, system time | yes | expired | update audit with null actor | Gap | Add D3 failing test for missing version increment. |
| `Revoke` | status revoked | pending, not expired, actor | yes | revoked | update audit | Gap | Add D3 failing test for missing version increment. |
| `SoftDelete` | inherited deleted state | idempotent | yes if changed | classify | update audit | Classify | Current inherited base has no invitation event/version override. |
| `Restore` | inherited restore | only when deleted | yes if changed | classify | update audit | Classify | Decide if invitations are restorable. |

### Domain Events

Created, accepted, expired, revoked events exist. Delete/restore lifecycle is not
classified.

### Cross-Aggregate References

References workspace by id and eventual accepted user by id in event only.

### Restore/Delete Policy

Pending decision: likely non-restorable or system-only restore because invite
tokens are security-sensitive.

### Risks

Accepted/expired/revoked mutations currently do not increment aggregate version,
despite persistent status changes.

### Required Tests

Version increment tests for accept/expire/revoke; delete/restore policy tests;
expired/revoked idempotency/throw policy tests.

## Aggregate: Board

### Bounded Context

WorkManagement.

### Responsibility

Board metadata, visibility, archive lifecycle, default group pointer, item
identity sequence.

### Owns

Workspace id, optional space id, title, description, background, visibility,
type/family, item key prefix, item sequence, default group id, archived flag.

### Does Not Own

Board item values, field definitions, view config, comments, permissions,
search index.

### Invariants

- Workspace id, actor, and title are required on create.
- Deleted board rejects mutations.
- Item identity sequence is generated by the board aggregate.
- Duplicate key prevention/concurrency retry belongs to Application/Infrastructure.

### Mutating Methods

| Method | State Change | Required Invariant | Version Required | Event Required | Audit Required | Current Status | Action |
|---|---|---|---|---|---|---|---|
| `Create` | board created | workspace/actor/title | no | created | create audit | OK | Keep. |
| `Rename` | title | not deleted, non-empty | yes if changed | renamed | update audit | OK | Add no-op version test. |
| `UpdateDescription` | description | not deleted | yes if changed | description updated | update audit | OK | Keep. |
| `UpdateBackground` | background | not deleted, non-empty | yes if changed | background updated | update audit | OK | Add invalid background policy if needed. |
| `ChangeVisibility` | visibility | not deleted | yes if changed | visibility changed | update audit | OK | Keep. |
| `Archive` | archived flag | not deleted | yes if changed | archived | update audit | OK | Keep. |
| `Unarchive` | archived flag false | not deleted | yes if changed | unarchived | update audit | OK | Keep. |
| `SetDefaultGroup` | default group id | not deleted | yes if changed | default group set | update audit | Classify | Need group workspace/board validation from Application or reference object. |
| `GenerateNextItemIdentity` | sequence/key | not deleted, actor | yes | identity generated | update audit | OK | Add concurrency policy tests/docs. |
| `SoftDelete` | deleted state | idempotent | yes if changed | soft deleted | update audit | OK | Keep. |
| `Restore` | restored state | only deleted | yes if changed | restored | update audit | OK | Parent workspace/space check in Application. |

### Domain Events

Board lifecycle/metadata/sequence events exist.

### Cross-Aggregate References

References workspace, space, and group by ids.

### Restore/Delete Policy

Restorable when parent workspace/space is active.

### Risks

`SetDefaultGroup` accepts only `Guid groupId`; wrong-board group validation must
be handled by Application or a future `BoardGroupRef`.

### Required Tests

Version tests for sequence/default group/no-op, archive mutation behavior, delete
guards.

## Aggregate: BoardItem

### Bounded Context

WorkManagement.

### Responsibility

Work item row/task lifecycle, placement, hierarchy, timeline, completion, and
dynamic field values.

### Owns

Workspace id, board id, group id, position, name, parent item id, item key,
sequence, level, timeline, completion, board item values.

### Does Not Own

Board metadata, field schema lifecycle, formula execution, rollup computation,
comments, permissions.

### Invariants

- Workspace, board, group, name, position, and actor are required on create.
- Deleted item rejects normal mutations.
- Group move validates workspace and board through `BoardGroupRef`.
- Field updates reject wrong workspace/board/deleted/system fields and invalid
  value/options.
- Parent assignment must not create cycles.

### Mutating Methods

| Method | State Change | Required Invariant | Version Required | Event Required | Audit Required | Current Status | Action |
|---|---|---|---|---|---|---|---|
| `Create` | item created | workspace/board/group/name/position | no | created | create audit | OK | Keep. |
| `Rename` | name | not deleted, non-empty | yes if changed | renamed | update audit | OK | Add version test. |
| `MoveToGroup` | group/position | not deleted, same workspace/board | yes if changed | moved | update audit | OK | Keep. |
| `UpdateFieldValue` | add/update value | not deleted, field valid | yes if changed | field value changed | update audit | OK | Add no-event same value test. |
| `AssignParentItem` | parent/level | no cycle | yes | parent assigned | update audit | Gap | Should no-op if parent/level unchanged? Decide in D3. |
| `SetTimeline` | start/due | due after start | yes if changed | timeline set | update audit | OK | Keep. |
| `Complete` | completed at | not deleted | yes if changed | completed | update audit | OK | Complete/uncomplete naming may need classification. |
| `SoftDelete` | deleted state | idempotent | yes if changed | soft deleted | update audit | OK | Keep. |
| `Restore` | restored state | only deleted | yes if changed | restored | update audit | OK | Parent board/group active check in Application. |

### Domain Events

Item create/rename/move/value/parent/timeline/complete/delete/restore events
exist.

### Cross-Aggregate References

Uses `BoardGroupRef`; accepts full `BoardField` from same bounded context for
field value validation. This is acceptable short term and should be revisited if
field definition loading becomes too broad.

### Restore/Delete Policy

Restorable when parent board/group is active.

### Risks

`AssignParentItem` increments version and emits event even when assigning the
same parent/level. Audit in D3 before changing.

### Required Tests

Same-value field no-op, parent cycle, parent no-op policy, workspace/board
mismatch, version/event assertions.

## Aggregate: BoardField

### Bounded Context

WorkManagement.

### Responsibility

Dynamic board schema field definition, options, settings, classification, and
formula metadata.

### Owns

Workspace id, board id, name, type, settings, position, default value,
system flag, data classification, sensitivity, formula metadata, options.

### Does Not Own

Board item values, formula execution, rollup recomputation, search indexing.

### Invariants

- Workspace, board, name, settings, and position are required.
- Settings must validate against field type.
- Options only allowed for select, multi-select, and status fields.
- Duplicate option names are rejected.
- System fields cannot be deleted.

### Mutating Methods

| Method | State Change | Required Invariant | Version Required | Event Required | Audit Required | Current Status | Action |
|---|---|---|---|---|---|---|---|
| `Create` | field created | valid schema/settings | no | created | create audit | OK | Keep. |
| `UpdateSettings` | settings | not deleted, settings valid | yes | updated | update audit | Gap | Should no-op if settings equal? Decide and test. |
| `AddOption` | option added | option-capable type, unique name | yes | option added | update audit | OK | Keep. |
| `UpdateClassification` | classification/sensitive | not deleted | yes if changed | classification updated | update audit | OK | Keep. |
| `UpdateFormula` | formula flag/expression | not deleted | yes if changed | formula updated | update audit | Classify | Formula syntax policy not enforced yet. |
| `SoftDelete` | deleted state | not system, idempotent | yes if changed | deleted | update audit | OK | Keep. |
| `Restore` | restored state | only deleted | yes if changed | restored | update audit | OK | Parent board active check in Application. |

### Domain Events

Field create/update/delete/restore/option/classification/formula events exist.

### Cross-Aggregate References

References workspace and board by id.

### Restore/Delete Policy

Restorable. System fields cannot be deleted.

### Risks

Formula expression validation is too light for enterprise use and must stay
syntax/policy only inside Domain; execution remains outside Domain.

### Required Tests

Settings no-op policy, formula validation policy, option duplicate/type tests,
system delete guard.

## Aggregate: Page

### Bounded Context

Documents.

### Responsibility

Document page metadata, tree parent relation, visibility/status, archive/delete
lifecycle.

### Owns

Workspace id, parent page id, title, icon/cover metadata, page status,
visibility.

### Does Not Own

Block content lifecycle, realtime editor operations, search index, large
snapshots.

### Invariants

- Workspace, title, and creator are required.
- Archived pages reject rename and move.
- Move rejects cycles using supplied parent-chain facts.
- Deleted pages reject normal mutations.

### Mutating Methods

| Method | State Change | Required Invariant | Version Required | Event Required | Audit Required | Current Status | Action |
|---|---|---|---|---|---|---|---|
| `Create` | active page | workspace/title/actor | no | created | create audit | OK | Keep. |
| `Rename` | title | not deleted/archived, non-empty | yes if changed | renamed | update audit | OK | Add version test. |
| `Move` | parent id | not deleted/archived, no cycle | yes if changed | moved | update audit | OK | Keep. |
| `Archive` | status archived | not deleted | yes if changed | archived | update audit | OK | Keep. |
| `SoftDelete` | status soft deleted/deleted | idempotent | yes if changed | soft deleted | update audit | OK | Keep. |
| `Restore` | status active/restored | only deleted | yes if changed | restored | update audit | Classify | Parent page/workspace active check missing from Domain by design; Application follow-up. |

### Domain Events

Page create/rename/move/archive/delete/restore events exist.

### Cross-Aggregate References

References parent page by id; no full aggregate references.

### Restore/Delete Policy

Parent-validated restore. Application must ensure parent page and workspace are
active before restore.

### Risks

Restore can reactivate a page whose parent is deleted/archived unless
Application checks parent state.

### Required Tests

Cycle tests, archived mutation guards, restore parent-policy tests, version
assertions.

## Aggregate: Block

### Bounded Context

Documents.

### Responsibility

Independently edited block content/properties/order within a page.

### Owns

Workspace id, page id, parent block id, type, content, properties, position.

### Does Not Own

Page tree lifecycle, realtime cursor/typing operations, snapshot storage, search
indexing.

### Invariants

- Workspace, page, actor, content, and position are required.
- Content must validate against block type.
- Move can reject cycles when supplied parent-chain lookup is provided.
- Deleted blocks reject normal mutations.

### Mutating Methods

| Method | State Change | Required Invariant | Version Required | Event Required | Audit Required | Current Status | Action |
|---|---|---|---|---|---|---|---|
| `Create` | block created | valid type/content/position | no | created | create audit | OK | Keep. |
| `UpdateContent` | content | not deleted, content valid | yes if changed | content updated | update audit | OK | Strengthen content schema tests. |
| `UpdateProperties` | properties | not deleted | yes if changed | properties updated | update audit | OK | Keep. |
| `Move` | parent/position | not deleted, optional no cycle | yes if changed | moved | update audit | OK | Require parent-chain validation in Application gate. |
| `SoftDelete` | deleted state | idempotent | yes if changed | soft deleted | update audit | OK | Keep. |
| `Restore` | restored state | only deleted | yes if changed | restored | update audit | Classify | Parent page/block active check belongs to Application. |

### Domain Events

Block create/content/properties/move/delete/restore events exist.

### Cross-Aggregate References

References page and parent block by id.

### Restore/Delete Policy

Parent-validated restore.

### Risks

`BlockContentValidator` is currently light and should be hardened before rich
document use cases expand.

### Required Tests

Block content schema tests, block tree no-cycle tests, restore parent policy,
version/event/no-op tests.

## Aggregate: Comment

### Bounded Context

Collaboration.

### Responsibility

Workspace-scoped comment lifecycle and target validation.

### Owns

Workspace id, target `ResourceRef`, parent comment id, content, anchor, status.

### Does Not Own

Target resource lifecycle, mentions extraction, notification delivery, activity
feed storage, audit log.

### Invariants

- Workspace, target, content, and actor are required.
- Target workspace must match comment workspace when present.
- Deleted comments reject normal mutations.

### Mutating Methods

| Method | State Change | Required Invariant | Version Required | Event Required | Audit Required | Current Status | Action |
|---|---|---|---|---|---|---|---|
| `Create` | active comment | workspace target/content/actor | no | created | create audit | OK | Add ResourceRef registry tests. |
| `UpdateContent` | content | not deleted, non-empty | yes if changed | updated/activity candidate | update audit | OK | Keep. |
| `Resolve` | status resolved | not deleted | yes if changed | resolved | update audit | OK | Add no-op test. |
| `SoftDelete` | status soft deleted/deleted | idempotent | yes if changed | soft deleted | update audit | OK | Keep. |
| `Restore` | status active/restored | only deleted | yes if changed | restored | update audit | Classify | Target active/commentable check belongs to Application/registry. |

### Domain Events

Comment create/update/resolve/delete/restore events exist and are activity and
notification candidates.

### Cross-Aggregate References

Uses `ResourceRef` only. No full target aggregate references.

### Restore/Delete Policy

Target-validated restore.

### Risks

Invalid/unregistered resource targets can be introduced until ResourceRef
registry enforcement exists.

### Required Tests

ResourceRef registry, cross-workspace target rejection, resolve no-op, restore
target policy.

## Aggregate: ResourcePermission

### Bounded Context

Governance.

### Responsibility

Explicit permission grant/revocation for one workspace resource and subject.

### Owns

Workspace id, resource type/id, subject type/id, permission level/effect,
condition JSON, priority.

### Does Not Own

Full permission evaluation query, inheritance cache, workspace owner/admin
membership, share-link runtime access.

### Invariants

- Workspace, resource id, and subject id are required.
- Deleted permission rejects level changes.
- Permission evaluator precedence is Governance policy, not handler logic.

### Mutating Methods

| Method | State Change | Required Invariant | Version Required | Event Required | Audit Required | Current Status | Action |
|---|---|---|---|---|---|---|---|
| `Grant` | permission created | ids/subject/resource | no | granted | create audit | OK | Add registry tests. |
| `ChangeLevel` | level | not deleted | yes if changed | level changed | update audit | OK | Keep. |
| `Revoke` | soft delete plus revoke fact | not deleted | yes once | revoke or soft-delete canonical | update audit | Gap | Current method calls `SoftDelete` then increments/events again; D3 test required. |
| `SoftDelete` | deleted state | idempotent | yes if changed | soft deleted | update audit | Classify | Decide if direct soft delete is allowed distinct from revoke. |
| `Restore` | restored state | only deleted | yes if changed | restored | update audit | OK | Registry/subject active check in Application. |

### Domain Events

Granted, level changed, soft deleted, restored, revoked events exist. Revoke vs
soft-delete canonical event policy is unresolved.

### Cross-Aggregate References

References resources by `ResourceType` and id, subjects by type and id.

### Restore/Delete Policy

Restorable if resource and subject are still valid.

### Risks

Current `Revoke` may double increment version and emit both soft-delete and
revoked events. Tests should pin intended policy before D5.

### Required Tests

Revoke single-transition policy, registry target validation, precedence policy,
restore validity.

## Aggregate: CustomRole

### Bounded Context

Governance.

### Responsibility

Workspace custom role definition and assignment/revocation facts.

### Owns

Workspace id, role name/description/status, owned role permission actions.

### Does Not Own

Workspace membership lifecycle, full authorization evaluation, permission cache.

### Invariants

- Workspace and role name are required.
- Duplicate permission actions are rejected.
- Deleted roles reject normal mutations.

### Mutating Methods

| Method | State Change | Required Invariant | Version Required | Event Required | Audit Required | Current Status | Action |
|---|---|---|---|---|---|---|---|
| `Create` | role created | workspace/name | no | created | create audit | OK | Keep. |
| `Rename` | name | not deleted, non-empty | yes if changed | updated | update audit | OK | Add no-op test. |
| `AddPermission` | child permission | not deleted, unique action | yes | updated | update audit | OK | Keep. |
| `RemovePermission` | child permission removed | not deleted | yes if exists | updated | update audit | OK | Keep. |
| `AssignToMember` | assignment fact only | not deleted | yes? | assigned | update audit | Classify | Method emits event but stores no assignment state; likely Application/projection responsibility. |
| `RevokeFromMember` | revocation fact only | not deleted | yes? | revoked | update audit | Classify | Same as assignment. |
| `Archive` | status archived | not deleted | yes if changed | archived | update audit | OK | Keep. |
| `Activate` | status active | archived only | yes if changed | activated | update audit | Gap | Missing `EnsureNotDeleted`; add D3 failing test before D5. |
| `SoftDelete` | status archived/deleted | idempotent | yes if changed | soft deleted | update audit | OK | Keep. |
| `Restore` | status active/restored | only deleted | yes if changed | restored | update audit | OK | Keep. |

### Domain Events

Created, updated, assigned, revoked, archived, activated, soft deleted, restored.

### Cross-Aggregate References

References members by id in event only.

### Restore/Delete Policy

Restorable. Member assignment state should be modeled separately or projected.

### Risks

Assignment/revocation methods mutate only audit/version and emit event without
owned assignment state. Need classification before Application relies on them.

### Required Tests

Deleted role activation guard, assignment ownership classification, permission
duplicate/no-op/version tests.

## Aggregate: ShareLink

### Bounded Context

Governance.

### Responsibility

Share link lifecycle for registered workspace resources.

### Owns

Workspace id, resource type/id, token hash, access mode, status, expiry.

### Does Not Own

Resource lifecycle, token generation, HTTP access checks, download delivery.

### Invariants

- Workspace, resource id, token hash are required.
- Public share links require expiration.
- Expired/disabled links are not active.

### Mutating Methods

| Method | State Change | Required Invariant | Version Required | Event Required | Audit Required | Current Status | Action |
|---|---|---|---|---|---|---|---|
| `Create` | active share link | ids/token/public expiry | no | created | create audit | OK | Add registry tests. |
| `Disable` | status disabled | not already disabled | yes if changed | disabled | update audit | Gap | Missing `EnsureNotDeleted`; add D3 test. |
| `RotateTokenHash` | token/status | valid hash | yes | rotated | update audit | Gap | Missing `EnsureNotDeleted`; add D3 test. |
| `Expire` | status expired | active only | yes if changed | expired | update audit null actor | Classify | System expiry acceptable; add tests. |
| `SoftDelete` | deleted state | idempotent | yes if changed | soft deleted | update audit | OK | Keep. |
| `Restore` | restored state | only deleted | yes if changed | restored | update audit | Classify | Should restore active or previous status? Decide. |

### Domain Events

Created, disabled, rotated, expired, soft deleted, restored events exist.

### Cross-Aggregate References

References resource by `ResourceType` and id.

### Restore/Delete Policy

Resource-validated restore; expired links likely should not restore to active
without explicit policy.

### Risks

Missing deleted guards on disable/rotate can mutate deleted links.

### Required Tests

Deleted guard tests, public expiry tests, resource registry tests, restore status
policy.

## Aggregate: Subscription

### Bounded Context

Billing.

### Responsibility

Workspace subscription lifecycle and plan/period state.

### Owns

Workspace id, plan id, subscription status/tier, current period, cancel-at-period
flag.

### Does Not Own

Payment provider data, invoices, webhook idempotency, entitlements, usage ledger.

### Invariants

- Workspace and plan id are required.
- Period start must be before end.
- Canceled/expired subscription cannot change plan.
- Deleted subscription rejects normal mutations.

### Mutating Methods

| Method | State Change | Required Invariant | Version Required | Event Required | Audit Required | Current Status | Action |
|---|---|---|---|---|---|---|---|
| `Create` | active subscription | ids/valid period | no | started | create audit | OK | Keep. |
| `ChangePlan` | plan id | not deleted, active/past due | yes | changed | update audit | OK | Add no-op policy if same plan. |
| `ScheduleCancellation` | cancel flag | not deleted | yes if changed | cancellation scheduled | update audit | OK | Keep. |
| `CancelImmediately` | status canceled | not deleted | yes if changed | canceled | update audit | OK | Keep. |
| `Renew` | period/status/cancel flag | valid period | yes | renewed | update audit | OK | Add canceled/expired renew policy. |
| `Expire` | status expired | not deleted | yes if changed | expired | update audit | OK | Keep. |
| `MarkPastDue` | status past due | not deleted | yes if changed | past due | update audit | OK | Keep. |
| `SoftDelete` | deleted state | idempotent | yes if changed | soft deleted | update audit | Gap | Does not call `SetAuditOnUpdate`; D3 test required. |
| `Restore` | restored state | only deleted | yes if changed | restored | update audit | Gap | Relies on base restore audit, but billing status policy unresolved. |

### Domain Events

Started, changed, cancellation scheduled, canceled, renewed, expired, past due,
soft deleted, restored events exist.

### Cross-Aggregate References

References workspace and plan by id.

### Restore/Delete Policy

Admin/system lifecycle. Delete/restore must not replace business cancellation.

### Risks

Restore can return a deleted canceled/expired subscription without explicit
billing policy decision.

### Required Tests

Soft-delete audit update, restore status policy, no-op same plan, renew inactive
policy, provider duplicate webhook idempotency outside Domain.

## Aggregate: Entitlement

### Bounded Context

Billing.

### Responsibility

Workspace feature access limit/status/expiry lifecycle.

### Owns

Workspace id, feature code, limit, source, status, expiry, revocation metadata.

### Does Not Own

Usage aggregation, payment provider state, request pipeline entitlement checks.

### Invariants

- Workspace and feature are required.
- Limit cannot be negative.
- Limit changes require active entitlement.
- Revoked entitlement cannot be disabled or expired.
- Deleted entitlement is inactive.

### Mutating Methods

| Method | State Change | Required Invariant | Version Required | Event Required | Audit Required | Current Status | Action |
|---|---|---|---|---|---|---|---|
| `Create` | active entitlement | workspace/feature/non-negative limit | no | granted | create audit | Gap | Does not call `SetAuditOnCreate`; D3 test required. |
| `ChangeLimit` | limit | active, non-negative, actor | yes if changed | limit changed | update audit | OK | Keep. |
| `Disable` | status disabled | not revoked, actor | yes if changed | disabled | update audit | OK | Keep. |
| `Revoke` | status revoked/revocation metadata | actor | yes if changed | revoked | update audit | OK | Keep. |
| `MarkExpired` | status expired | not revoked | yes if changed | expired | update audit null actor | OK | System expiry acceptable. |
| `IsActiveAt` | query only | status/expiry/deleted | no | no | no | OK | Keep. |
| `SoftDelete` | deleted state | idempotent | yes if changed | soft deleted | update audit | Gap | Does not call `SetAuditOnUpdate`; D3 test required. |
| `Restore` | restored state | only deleted | yes if changed | restored | update audit | Gap | Relies on base restore audit; status/expiry policy unresolved. |

### Domain Events

Granted, limit changed, disabled, revoked, expired, soft deleted, restored events
exist.

### Cross-Aggregate References

References workspace by id and feature by value object.

### Restore/Delete Policy

Restorable only when status/expiry policy is explicit. Restoring an expired or
revoked entitlement must not silently grant access.

### Risks

Create and soft-delete audit consistency gaps can make entitlement changes hard
to trace.

### Required Tests

Create audit, soft-delete audit, restore status/expiry policy, active-at edge
cases, version/no-op tests.

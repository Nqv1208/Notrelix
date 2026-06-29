# Domain vs Projection vs Infrastructure Classification

This policy prevents Notrelix from turning runtime and reporting data into rich
Domain aggregates.

## Classifications

| Classification | Meaning | Belongs in Domain? |
|---|---|---:|
| Aggregate root | Business lifecycle and invariants with independent use cases. | Yes |
| Entity | Identity with parent/context-owned lifecycle. | Yes when business-owned |
| Value object | Immutable value with validation/equality by value. | Yes |
| Projection/read model | Denormalized state for queries/search/display. | No by default |
| Runtime state | Connection/session/worker state. | No by default |
| Ops record | Reliability, retry, locks, outbox, idempotency. | No |
| Audit log | Compliance/security append-only record. | Domain or Governance record only if append-only policy is explicit |
| Activity log | User-facing feed derived from facts. | Projection by default |
| Notification delivery state | Inbox/delivery/read/archive state. | Projection/delivery model by default |
| Search index document | Rebuildable search projection. | No |
| Analytics snapshot | Rebuildable reporting projection. | No by default |
| Job lock | Worker coordination. | No |
| Idempotency key | Duplicate request/event protection. | No |
| Outbox message | Durable dispatch infrastructure. | No |

## Notrelix Decisions

| Concept | Classification | Rule |
|---|---|---|
| `Workspace`, `Board`, `BoardItem`, `BoardField`, `Page`, `Block`, `Comment` | Aggregate root | Core Domain. |
| `WorkspaceMember`, `WorkspaceInvitation`, `Subscription`, `Entitlement` | Aggregate root | Domain lifecycle and invariants. |
| `ResourcePermission`, `CustomRole`, `ShareLink` | Aggregate root | Governance Domain when lifecycle/security policy exists. |
| `AuditLog` | Append-only governance record | No update/delete behavior; not a mutable aggregate. |
| `ActivityLog` | User-facing projection unless lifecycle proves otherwise | Prefer deriving from domain events. |
| `Notification` | Delivery/inbox model | Do not make delivery block aggregate transactions. |
| `UnreadCounter` | Projection | Keep out of core Domain behavior. |
| `PresenceSession` | Runtime state | Should not be durable rich aggregate. |
| `ReportingSnapshot` | Analytics projection | Rebuildable unless user-managed lifecycle is proven. |
| `MirrorValueSnapshot`, `RollupSnapshot` | Computed projection | Formula/rollup execution is not aggregate responsibility. |
| `IntegrationSyncCursor` | Ops/runtime state | Infrastructure unless user-facing lifecycle exists. |
| `WebhookDelivery` | Ops delivery state | Reliability/idempotency outside core Domain. |
| Outbox/processed events | Ops records | Infrastructure only. |
| Search document/index job | Projection/ops | Search infrastructure only. |
| Job locks/idempotency keys | Ops records | Infrastructure only. |

## Promotion Rule

A projection/runtime/ops model may move into Domain only after a rulebook entry
documents:

1. business owner context;
2. user-facing lifecycle;
3. invariants;
4. state transitions;
5. reason it cannot remain a projection or infrastructure record;
6. pure Domain tests.

Without that evidence, keep it outside Domain or classify it as a legacy gap.

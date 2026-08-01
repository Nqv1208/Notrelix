# Domain Lifecycle and Deletion

> Documentation only. Not consumed by tests, snapshots, or build.

## Core principle

Deletion availability is separate from business lifecycle status, unless the
business explicitly defines deletion as the terminal status.

## Deletion policies

| Policy | Meaning | Examples |
|---|---|---|
| **NotSupported** | Aggregate cannot be deleted | AuditLog entries |
| **RecoverableDelete** | Soft delete with restore | Board, BoardField, BoardItem, Page, Block, Comment |
| **ArchiveOnly** | Archive/unarchive, no hard delete | Workspace |
| **BusinessTerminationOnly** | Business lifecycle ends the resource | Subscription (Cancel/Expire), Invitation (Revoke) |
| **AppendOnly** | Immutable once created | ReportingSnapshot, UsageFact |
| **OwnedRemoval** | Deleted with parent aggregate | FieldOption (via BoardField), Block children |
| **BusinessTombstone** | Replaced by a tombstone record | — |

## RecoverableDelete rules

For aggregates using `RecoverableDelete`:

- `Delete` changes only: `IsDeleted`, `DeletedAt`, `DeletedBy`, `DeleteReason`.
- `Restore` changes only: `IsDeleted`, `DeletedAt`, `DeletedBy`, `DeleteReason`.
- Neither operation changes business status.
- Repeated Delete/Restore is a semantic no-op.
- Mutations while deleted are rejected via `EnsureNotDeleted()`.
- Version increments exactly once per successful operation.
- Domain events are raised for Delete and Restore.

## Forbidden patterns

```text
Status = SoftDeleted
_statusBeforeDeletion
Delete → Status = Revoked
Restore → Status = Active
```

## Real business lifecycle verbs

Use domain language, not deletion language:

```text
Archive / Unarchive
Revoke / Expire
Cancel / Renew
Suspend / Activate
Remove
Resolve / Reopen
Watch / Unwatch
```

## Behavior test expectations

For each recoverable aggregate, verify as applicable:

- Delete changes deletion availability
- Restore changes deletion availability
- Delete/Restore preserve business lifecycle state
- Repeated Delete/Restore is a semantic no-op
- Mutations while deleted are rejected
- Version/audit/event behavior is correct
- Failure leaves state unchanged

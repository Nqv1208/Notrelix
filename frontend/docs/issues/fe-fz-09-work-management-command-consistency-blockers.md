# FE-FZ-09 Work Management Command Consistency Blockers

## Status

`OPEN`

## Owner

Frontend Work Management owners, backend/API platform for version/idempotency contract

## Phase

FE-FZ-09 — Query, optimistic command and conflict handling

## Summary

The generic `@notrelix/query` optimistic command primitive has been expanded and one critical Work Management mutation (`useMoveCard`) now uses it. The API client also supports `Idempotency-Key` request options in addition to `X-Correlation-ID`.

FE-FZ-09 cannot be marked complete until all critical Work Management mutations are migrated and the backend contract for expected version/conflict/realtime echo behavior is confirmed.

## Completed Frontend Work

- Added `CommandContext`:

```text
commandId
correlationId
idempotencyKey
```

- `executeOptimisticCommand` now supports:

```text
mutationFn(variables, context)
reconcile(result, queryClient, context)
onConflict(error, snapshots, context)
explicit invalidate keys
reverse-order rollback
refetch conflict policy
```

- `createNotrelixClient` supports:

```text
Idempotency-Key
X-Correlation-ID
```

- `useMoveCard` now uses `executeOptimisticCommand`.

## Remaining Frontend Work

Critical Work Management mutations still need command definitions and migration:

```text
create item
update field value
delete item
duplicate item
create/update/delete group
create/update/delete/reorder columns
checklist mutations
comment/update mutations
```

Each command must define:

```text
affected query keys
optimistic projection
rollback snapshots
server result reconciliation
conflict strategy
realtime echo key
explicit invalidation policy
```

## Backend/API Contract Needed

The frontend cannot finish expected-version and conflict handling without backend confirmation for:

```text
expectedVersion field/header
409 response shape
authoritative replacement payload after create/update/move
idempotency key persistence semantics
correlationId echo into realtime events
```

## Required Follow-up Tests

```text
server result replaces temporary id
concurrent move same item serializes or conflicts clearly
409 expected-version rollback/refetch policy
HTTP success plus realtime echo applies once
network failure rolls cache back and surfaces UI error
all critical mutations use command definitions
```

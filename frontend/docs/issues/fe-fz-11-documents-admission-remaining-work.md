# FE-FZ-11 Documents Admission Remaining Work

## Status

`OPEN`

## Owner

Frontend Documents module owners, backend/API platform for version/conflict contracts

## Phase

FE-FZ-11 — Documents Module Admission

## Summary

`@notrelix/docs-core` has been split so React Query hooks, API adapters, DTOs and mappers now live in `@notrelix/docs-state`. This clears the architecture checker violations where docs core imported React Query and declared React/React Query dependencies.

FE-FZ-11 is not fully complete because command consistency, collaboration scope, realtime/recovery and testing package work remain.

## Completed Frontend Work

- Added package:

```text
frontend/packages/product/docs/state
```

- Moved from docs core to docs state:

```text
api
dto
model mappers
query hooks
```

- Kept pure query keys in docs core:

```text
@notrelix/docs-core/query/keys
```

- Updated docs web and app web imports to use `@notrelix/docs-state`.
- Updated dependency rules to allow docs state as the data/query layer.
- Architecture checker no longer reports `@notrelix/docs-core` React or React Query violations.

## Remaining Work

### Docs Commands

Required commands still need explicit command definitions and optimistic consistency:

```text
create page
rename/update page
create block
update block
move/reorder block
delete block
```

Each command needs:

```text
expected page version
idempotency key
conflict policy
server reconciliation
realtime echo handling
```

### Collaboration ADR

Create:

```text
frontend/docs/adr/ADR-002-documents-collaboration-model.md
```

Decision required:

```text
V1 server-authoritative block commands
or
CRDT/OT collaborative text protocol
```

Generic domain-event WebSocket must not be used to merge character-level edits.

### Docs Realtime and Recovery

Still required:

```text
page/block structural event adapter
gap to authoritative page snapshot recovery
comments/mentions realtime decision
stale page version ignore
```

### Docs Testing

Still required:

```text
docs-testing package
builders
fake repository
fake collaboration transport
move block rollback/conflict tests
editor keyboard/selection/undo tests
realtime structural event tests
```

## Acceptance Criteria

```text
docs-core remains framework-neutral
docs-state owns repositories/query hooks/commands
collaboration ADR exists
structural realtime adapter and recovery are implemented
block command tests cover rollback/conflict/reconcile
docs editor tests cover keyboard/selection/undo
```

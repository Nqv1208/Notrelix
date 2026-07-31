# FE-FZ-10 Work Management Admission Remaining Work

## Status

`OPEN`

## Owner

Frontend Work Management module owners

## Phase

FE-FZ-10 — Work Management Module Admission

## Summary

The Work Management state package no longer imports `sonner` or performs UI toast side effects in the data/state layer. This clears the architecture checker `DATA_UI_SIDE_EFFECT` group for `@notrelix/work-management-state`.

FE-FZ-10 still has substantial remaining module admission work that should be handled in focused follow-up changes.

## Completed Frontend Work

- Removed `sonner` imports from:

```text
frontend/packages/product/work-management/state/src
```

- Removed state-layer toast side effects from mutations, queries and board-view hooks.
- `@notrelix/work-management-state` typecheck passes.
- Architecture checker no longer reports `DATA_UI_SIDE_EFFECT` for Work Management state.

## Remaining Work

### Canonical Language

Source still uses mixed terminology:

```text
Card / BoardItem
List / BoardGroup
Column / BoardField
```

FE-FZ-10 requires canonical source naming:

```text
BoardItem
BoardGroup
BoardField
BoardView
```

Display labels may still say card/task/list where product copy requires it, but source models and command names should converge.

### Core Purity

Work Management core still needs a dedicated audit for:

```text
API DTOs
mappers
mocks/builders
legacy query key aliases
contracts/query/UI/React imports
```

Required target:

```text
core = models, ids, schemas, commands/policies, ordering, canonical query key factory
state = API DTOs, transport mappers, query hooks, mutation hooks
testing = mocks/builders/fakes
```

### Command Consistency

Only `moveCard` has been migrated to the expanded optimistic command primitive. Remaining critical mutations are tracked by:

```text
frontend/docs/issues/fe-fz-09-work-management-command-consistency-blockers.md
```

### UI Feedback Replacement

State-layer toasts were removed. Web/UI package owners should decide where mutation feedback belongs:

```text
web components
web hook wrappers
command event bus / notification presenter
```

Do not reintroduce `sonner` into state/data packages.

## Acceptance Criteria

```text
@notrelix/work-management-core has no framework or transport imports
canonical BoardItem/BoardGroup/BoardField naming is used in source
legacy queryKeys aliases are removed before freeze tag
all critical commands use command definitions
UI feedback is implemented outside state/data packages
dependency checker remains free of Work Management state side effects
```

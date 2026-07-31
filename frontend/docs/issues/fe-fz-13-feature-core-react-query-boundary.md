# FE-FZ-13 Feature Core React Query Boundary

## Status

`RESOLVED_FOR_BOUNDARY_CHECK`

## Owner

Frontend feature module owners

## Phase

FE-FZ-13 — Feature module internal template

## Summary

After FE-FZ-04 through FE-FZ-11 cleanup, the remaining architecture checker failures were feature packages importing React Query from `src/core/query/hooks`.

This violates the target feature package template:

```text
feature/src/
├── core/
├── data/
├── react/
├── web/
└── testing/
```

`core` must stay framework-neutral. React Query hooks belong in `react`, `web`, or a state/data layer depending on feature ownership.

## Resolved Violations

React Query hooks were moved out of `core/query/hooks` for:

```text
packages/features/account/src/web/query/hooks/*
packages/features/collaboration/src/web/query/hooks/*
packages/features/governance/src/web/query/hooks/*
packages/features/integrations/src/web/query/hooks/*
packages/features/notifications/src/web/query/hooks/*
packages/features/workspace/src/web/query/hooks/*
```

`@notrelix/dependency-rules` now reports 0 violations.

## Remaining FE-FZ-13 Admissions

The broader FE-FZ-13 phase still includes feature lifecycle behavior that cannot be truthfully completed without deeper product/API contracts and dedicated feature work:

- Auth: full session state machine, BroadcastChannel adapter contract, concurrent refresh behavior.
- Workspace: authoritative `WorkspaceSnapshot` membership contract and switch orchestration semantics.
- Governance: role/resource/action matrix and permission refresh event contract.
- Billing: entitlement snapshot contract and upgrade boundary rules.
- Account: avatar upload protocol and security-flow storage rules.
- Notifications: realtime event contract, cursor pagination shape, read/unread echo semantics.
- Activity: server read-model contract and realtime prepend/invalidate policy.
- Collaboration: comment/reaction/presence contracts, TTL semantics, mention payload mapping.
- Integrations: OAuth popup/redirect state machine and connection status event contract.
- Search: cross-entity result contract, abort/cursor behavior, workspace scoping guarantees.

## Acceptance Criteria

```text
dependency checker has zero CORE_IMPURE_IMPORT for packages/features/*
feature core folders contain no @tanstack/react-query imports
app/web still typechecks
node tests pass
```

## Follow-up Acceptance Criteria

Create feature-specific implementation tickets once backend/API contracts are confirmed for the remaining admissions above. Do not fake production contracts in frontend to close this phase.

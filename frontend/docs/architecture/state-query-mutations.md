# State, Query, and Mutations

## Scope

Server-state ownership, query keys, tenant/workspace/resource scope, cache
ownership, mutation lifecycle, optimistic updates, rollback, invalidation,
workspace transitions, stale response handling and derived UI state.

## Responsibility / Ownership

Backend is server-state authority. Frontend query/cache owners isolate and
reconcile server state for each product capability.

## Current Architecture

Query/cache behavior is implemented in foundation query packages and
product/feature owners.

## Normative Contracts

- Query keys include tenant/workspace/resource scope required to avoid leakage.
- Each product/server-state area has an owning package and query-key factory.
- Mutations define submit, optimistic admission, rollback and reconciliation.
- Optimistic updates require deterministic rollback and conflict handling.
- Invalidate vs patch is an ownership decision.
- Workspace switch clears/rekeys old workspace state and disconnects/reconnects realtime.
- Stale responses/races cannot overwrite newer workspace/resource state.
- Local stores may hold UI state, not permanent duplicated server truth.

## Allowed Design

Derived UI state, product-owned query hooks and focused optimistic updates with
tested rollback.

## Forbidden Design

No tenant-blind cache keys, permanent local server-state copies, component-local
mutation ownership or ignored stale responses.

## Failure Modes

Cross-workspace stale data, unrollbackable optimistic updates and REST/realtime
races corrupting cache state.

## Change Impact Rules

Query-key, mutation, optimistic update, workspace transition or cache ownership
changes require focused unit/integration tests and relevant host tests.

## Executable Evidence / Tests / Gates

Foundation query tests, product state tests and lifecycle host tests.

## Related ADRs

See `../decisions/README.md`.

## Related Source Manifests

Architecture manifest and product package manifests.

## Explicit Non-responsibilities

This document does not define backend mutation invariants.

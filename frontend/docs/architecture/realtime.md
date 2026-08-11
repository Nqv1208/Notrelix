# Realtime

## Scope

Realtime connection state, subscription ownership, message identity,
duplicate/out-of-order behavior, sequence gap recovery, reconnect, heartbeat,
bounded dedup, cache reconciliation, workspace transition and mobile behavior.

## Responsibility / Ownership

Realtime complements server state. It delivers hints/facts that reconcile with
backend-authoritative REST/query state.

## Current Architecture

Foundation realtime/runtime packages own connection mechanisms. Product packages
own how messages reconcile into query/cache state.

## Normative Contracts

- Messages have stable identity and enough scope to reconcile safely.
- Subscription ownership follows product/resource/workspace boundaries.
- Duplicate messages are bounded-deduped or safely idempotent.
- Out-of-order messages use sequence/version checks or trigger recovery.
- Sequence gaps trigger refetch/recovery.
- Reconnect restores subscriptions and reconciles cache.
- Heartbeat/connection state is runtime concern.
- Workspace switch disconnects or re-scopes old subscriptions.
- Mobile behavior respects app lifecycle and platform constraints.

## Allowed Design

Product reconciliation handlers, runtime connection adapters and refetch on
uncertain ordering/gaps.

## Forbidden Design

No realtime-only persistent truth, global subscriptions for workspace data,
unbounded dedup storage or mobile imports of web realtime runtime.

## Failure Modes

Stale workspace messages patch current cache, reconnect duplicates effects,
sequence gaps silently lose data.

## Change Impact Rules

Connection lifecycle, subscription identity, dedup, ordering, recovery or cache
reconciliation changes require realtime/foundation tests and affected product
tests.

## Executable Evidence / Tests / Gates

Foundation realtime tests, runtime tests and product reconciliation tests.

## Related ADRs

See `../decisions/README.md`.

## Related Source Manifests

Architecture manifest and realtime package manifests.

## Explicit Non-responsibilities

This document does not define backend event production or persistence.

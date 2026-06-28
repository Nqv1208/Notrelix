# ADR-005: Idempotency Semantics

**Date:** 2026-06-27
**Status:** Accepted
**Deciders:** Tech Lead

## Context

In distributed systems, requests may be retried due to network issues, client timeouts, or load balancer retries. Without idempotency, duplicate requests can cause duplicate side effects (double charges, duplicate records, duplicate notifications).

The backend has an `IdempotencyBehavior` in the MediatR pipeline and `IIdempotentRequest` marker interface, but zero requests currently implement it.

## Decision

### When idempotency applies

Requests opt in to idempotency by implementing `IIdempotentRequest` with an `IdempotencyKey` property. `IdempotencyBehavior` handles the lifecycle:

1. **Acquire lock:** Before handler execution, acquire a distributed lock using the idempotency key (5-minute TTL).
2. **Lock acquired → execute handler.**
3. **Lock not acquired → check for cached result:**
   - If a completed result exists, return it (safe replay).
   - If no result exists, throw `ConflictException` (request still in progress).
4. **On success:** Store the response in the idempotency store, release the lock.
5. **On failure:** Release the lock, do NOT store result, rethrow.

### When result is stored

The idempotency result is stored **after** `next()` returns successfully. Because `IdempotencyBehavior` is registered OUTSIDE `TransactionBehavior`, the result is stored AFTER the transaction commits. This means:

- Stored result = transaction committed + handler succeeded.
- If transaction rolls back, the exception propagates before result storage.
- If handler throws, exception propagates before result storage.

### Request hash validation

Each idempotency key is bound to a request hash. If the same key is reused with a different request body, the request should be rejected to prevent accidental misuse. (Currently not implemented — recommended for future hardening.)

## Consequences

- Duplicate requests are safely deduplicated.
- Failed requests can be retried (lock released, no cached result).
- Clients receive consistent responses for the same idempotency key.
- The 5-minute lock TTL is configurable and must be longer than the longest expected handler execution time.

## Rejected alternatives

- **Database unique constraint only:** Too coarse — doesn't handle in-progress vs completed states.
- **Client-generated request IDs without server-side tracking:** Can't detect duplicates across different client instances.
- **Optimistic concurrency only:** Detects conflicts but doesn't provide result caching.

## Verification

- `IdempotencyBehavior` unit tests: `Notrelix.Application.Tests/Behaviors/TransactionBehaviorTests.cs`
- Pipeline-order tests: pending in Slice 3
- `IdempotencyStoreIntegrationTests`: currently 1 failure (Docker-dependent)

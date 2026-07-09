# 10 — Infrastructure Code Review Checklist

Use this checklist before merging any Infrastructure PR.

## Layering

- [ ] Does Infrastructure only implement Application/Domain abstractions?
- [ ] Did Application avoid referencing Infrastructure concrete classes?
- [ ] Did API avoid injecting DbContext or Infrastructure services directly?
- [ ] Is business decision kept out of Infrastructure?

## DI / options

- [ ] Registration is in the correct `{Capability}Registration.cs`.
- [ ] Root `DependencyInjection.cs` remains thin.
- [ ] Service lifetime is correct.
- [ ] Singleton does not capture scoped service.
- [ ] Options are validated on start.
- [ ] Development-only fallback is blocked outside Development.

## Persistence

- [ ] Handler uses bounded-context DbContext interface, not `ApplicationDbContext`.
- [ ] Entity configuration is in correct folder.
- [ ] Schema/table/index are correct.
- [ ] Tenant-scoped uniqueness includes account/workspace dimensions.
- [ ] Soft delete and query filters are correct.
- [ ] RLS policy/migration exists if table is tenant-scoped.
- [ ] No unsafe raw SQL.
- [ ] No `IgnoreQueryFilters()` unless allowlisted and tested.

## RLS / tenant

- [ ] Tenant context is set before tenant-scoped DB access.
- [ ] RLS session is applied before query/write.
- [ ] System context usage is allowlisted and justified.
- [ ] Tenant context is cleared in finally for worker/consumer.

## Events / outbox

- [ ] Handler does not publish bus message directly.
- [ ] Domain event is persisted via interceptor/outbox path.
- [ ] Integration event mapping is deterministic.
- [ ] Outbox message is in same transaction as aggregate mutation.
- [ ] Dispatcher errors lead to retry/dead-letter, not message loss.

## Consumers

- [ ] Exactly one idempotency mechanism is active.
- [ ] No manual dedup in consumer handler.
- [ ] `event_id + consumer_name` is used.
- [ ] Mark processed happens after successful work.
- [ ] Consumer transaction includes side effect record + processed record when DB-based.
- [ ] Tenant/RLS context is set and cleared.

## External services

- [ ] No secret/token logged.
- [ ] Provider client is an adapter only.
- [ ] Timeout/cancellation supported.
- [ ] Retry policy does not retry non-retryable business/security errors.
- [ ] Storage object keys are server-generated and tenant-scoped if needed.

## Tests

- [ ] Unit/infrastructure/integration tests added.
- [ ] Architecture test updated for new boundary.
- [ ] Cross-tenant test added for high-risk data path.
- [ ] Failure path tested, not only happy path.
- [ ] Duplicate/retry/idempotency tested for messaging/jobs.

## Docs/RULE

- [ ] `backend/RULE.md` updated for new rule.
- [ ] `/docs` updated if decision has rationale.
- [ ] ADR added for large architecture trade-off.

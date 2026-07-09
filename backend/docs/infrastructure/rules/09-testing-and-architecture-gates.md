# 09 — Infrastructure Testing and Architecture Gates

## 1. Test types

Infrastructure changes need one or more of:

- Unit tests for adapters/services.
- Infrastructure tests for EF/RLS/converters/outbox.
- Integration tests for real pipeline behavior.
- Architecture tests for dependency/boundary rules.

## 2. Architecture gates

Required gates:

```txt
Application must not reference Infrastructure.
Domain must not reference Infrastructure.
API endpoints must not inject ApplicationDbContext.
Application handlers must not inject Infrastructure concrete types.
Application handlers must not inject ICurrentTenantContext directly.
No handler should call IPublishEndpoint/IIntegrationEventBus directly.
No request cache key should be built in Infrastructure Redis service.
No IgnoreQueryFilters outside allowlist.
No raw SQL outside Infrastructure approved services.
No manual consumer dedup if filter/executor is selected.
```

## 3. Persistence tests

Required:

- Entity configuration creates expected schema/table/index.
- Value converters round-trip.
- Enum string conversion works.
- Soft delete filter hides deleted rows.
- Tenant filter hides other workspace/account.
- System context bypass only in allowlisted path.
- RLS session variables are set.
- RLS disabled for non-system request throws.

## 4. Outbox tests

Required:

- Domain event creates DomainEventLog.
- Integration mapping creates outbox message.
- Rollback prevents outbox persistence.
- Dispatcher claims pending messages.
- Dispatcher retries failed messages.
- Unknown event type marks failed/dead-letter.
- Publish failure does not mark processed.
- Duplicate dispatcher processing is idempotent.

## 5. Consumer tests

Required:

- Duplicate delivery -> side effect once.
- Concurrent duplicate -> one success, one skip.
- Consumer throws -> transaction rollback, not processed.
- Tenant context set from event.
- RLS applied before DB access.
- Tenant context cleared after consumer.

## 6. Cache tests

Required:

- Redis service respects TTL.
- Serialization round-trip.
- Remove/expire works.
- Infrastructure Redis service does not build request cache key.
- Application cache behavior covers key construction.

## 7. Options tests

Required:

- Missing Database/Redis/Jwt/RabbitMQ section fails if required.
- Invalid option values fail on start.
- Development-only provider rejected outside Development.

## 8. Rate limit tests

Required:

- fixed window behavior;
- sliding window behavior;
- retry-after values;
- unimplemented algorithm throws;
- partition key isolation.

## 9. Background job tests

Required:

- cancellation respected;
- scoped services not captured by singleton;
- retry/backoff;
- failure isolation per item;
- idempotent rerun;
- lock prevents duplicate run;
- tenant context cleared.

## 10. Test naming

Use explicit behavior names:

```txt
Should_Apply_Rls_Before_Consumer_Db_Access
Should_Not_Mark_Processed_When_Consumer_Fails
Should_Reject_IgnoreQueryFilters_Outside_Allowlist
```

Avoid vague names:

```txt
Test1
Works
ShouldPass
```

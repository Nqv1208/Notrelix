# 06 — Outbox, Events, Messaging and Consumers

## 1. Event persistence principle

Database state and outbox message must be persisted in the same transaction.

Rule:

```txt
Aggregate mutation + DomainEventLog + IntegrationEvent outbox message = same SaveChanges transaction.
```

Do not publish external messages directly from handler or EF interceptor.

## 2. DomainEventInterceptor rule

`DomainEventInterceptor` may:

- collect domain events from aggregate roots;
- write `DomainEventLog`;
- map domain events to integration events;
- write outbox messages.

It must not:

- call external message bus;
- send email;
- call webhook;
- update read model directly;
- run business decision.

Inline dispatch is forbidden.

## 3. Event type registry rule

All durable event/message types must be registered in a single registry.

Do not deserialize by arbitrary type name from DB without registry.

Message names must be stable.

Changing message name is a breaking change and requires migration/backward compatibility plan.

## 4. Integration event mapper rule

Mapping DomainEvent -> IntegrationEvent belongs in Infrastructure/Event mapping if it concerns external message contract.

Mapper must be deterministic.

It should not query DB unless absolutely necessary. If it needs extra data, reconsider event payload or mapping location.

## 5. Outbox dispatcher rule

Outbox dispatcher provides at-least-once delivery.

It must:

- claim messages atomically;
- handle pending/failed/timeout;
- mark processing/processed/failed/dead-letter;
- retry with backoff;
- record processed event for dispatcher;
- log EventId/MessageName/RetryCount;
- update metrics.

Consumer/handler must be idempotent because message can be delivered more than once.

## 6. Processed event rule

Idempotency key:

```txt
event_id + consumer_name
```

There must be a unique constraint/index on this pair.

Do not use only `event_id` if one event has multiple independent consumers.

## 7. Consumer idempotency rule

Choose exactly one consumer idempotency mechanism.

Allowed models:

### Model A — MassTransit filter-level idempotency

```txt
TenantContextConsumeFilter
DeduplicationConsumeFilter
Consumer
```

Filter owns transaction/idempotency around consumer.

### Model B — ConsumerPipelineExecutor-level idempotency

```txt
MassTransit consumer
ConsumerPipelineExecutor
Application consumer handler
```

Executor owns transaction/idempotency.

Do not combine both for the same path.

Do not implement manual dedup in each consumer handler.

## 8. Tenant context in consumers

Integration events must carry enough scope:

```txt
AccountId
WorkspaceId when workspace-scoped
ActorUserId when relevant
CorrelationId
CausationId
```

Consumer path must:

1. set tenant/account/system context from message;
2. apply RLS session before DB access;
3. clear tenant context in finally.

If integration event lacks account id, consumer must run as system only when event is truly global/system.

## 9. Consumer transaction rule

If consumer writes DB and marks processed, both must be in the same transaction.

Correct:

```txt
Begin transaction
Check processed
Run consumer business side effect / DB write
Mark processed
SaveChanges
Commit
```

If consumer sends external email/webhook, decide whether it is:

- best-effort post-commit action; or
- durable outbox action.

Do not mark processed before durable side effect has been recorded.

## 10. MassTransit registration rule

Transport selection must be explicit:

```txt
InMemory / RabbitMQ / Kafka / None
```

`None` allowed only in Development.

RabbitMQ options must validate on start.

Retry/circuit breaker should ignore non-retryable exceptions:

- Validation errors.
- Security misconfiguration.
- Not found where not retryable.
- Forbidden/unauthorized.

## 11. Consumer naming rule

ConsumerName must be stable.

Do not derive consumer name from unstable endpoint address if endpoint naming can change without migration.

Recommended:

```txt
consumer:{FullConsumerTypeName}
```

or explicit constant.

If current implementation extracts endpoint name, document that endpoint name is part of idempotency contract.

## 12. Testing rule

Required tests:

- Duplicate event delivered twice -> side effect once.
- Concurrent duplicate delivery -> one winner, one skip.
- Consumer failure before mark processed -> retry executes again.
- Consumer success then duplicate -> skip.
- Tenant context applied for workspace event.
- Global event uses system context intentionally.
- Unknown event type -> failed/dead-letter path.
- Outbox publish failure -> retry scheduled.

## 13. Forbidden patterns

Do not:

- Publish MassTransit message from Application handler.
- Call `IPublishEndpoint` directly outside `IntegrationEventBus`/messaging infrastructure.
- Add manual `if processed return` in each consumer.
- Mix filter idempotency and executor idempotency.
- Clear domain events before writing outbox.
- Run inline domain event side effects.

# Events, Outbox, and Messaging Rules

## 1. Event categories

### Domain event

Represents something that happened inside Domain.

Examples:

```txt
BoardCreated
BoardRenamed
WorkspaceMemberAdded
```

### Integration event

Contract crossing bounded context or external boundary.

Examples:

```txt
UserRegisteredIntegrationEvent
WorkspaceProvisioningRequested
SubscriptionChangedIntegrationEvent
```

## 2. Handler side-effect rule

Handler must not directly:

```txt
Publish to MassTransit/RabbitMQ
Send webhook
Send email that must be durable
Call external integration API as side effect
```

Use:

```txt
Domain event -> mapper -> integration event -> outbox
Application integration event collector if use case emits event directly
Post-commit queue for best-effort side effects
```

## 3. Outbox rule

Outbox message must be persisted in the same transaction as state mutation.

Flow:

```txt
Handler mutates aggregate
Domain event captured during SaveChanges
Integration event mapped
Outbox message inserted
Transaction commits
Dispatcher publishes later
```

Rollback means:

```txt
No outbox message should be published.
```

## 4. Consumer idempotency

At-least-once delivery means duplicate messages are expected.

Each consumer must be idempotent by:

```txt
event_id + consumer_name
```

Rule:

```txt
Use exactly one idempotency mechanism for a consumer path.
Do not combine MassTransit dedup filter and ConsumerPipelineExecutor idempotency for the same consumer.
Do not write manual dedup logic inside individual handlers if pipeline/filter owns it.
```

## 5. Recommended consumer flow

```txt
MassTransit receives message
Tenant/correlation context is set
Idempotency checks event_id + consumer_name
Business consumer runs
Processed event is marked only after success
Message ack
```

If business consumer fails:

```txt
Do not mark processed
Let retry policy handle redelivery
```

If duplicate arrives after success:

```txt
Skip business side effect
Ack safely
```

## 6. Tenant context in consumers

Consumer handling workspace/account scoped event must set tenant context before reading/writing tenant data.

Rules:

```txt
Integration event must carry enough account/workspace/correlation metadata.
Consumer must not query tenant data before tenant context/RLS is applied.
System context usage must be explicit and audited.
```

## 7. Tests required

Outbox tests:

```txt
Commit success -> outbox message exists
Rollback -> no outbox message
Dispatcher publish fail -> retry/dead-letter behavior
Unknown event type -> dead-letter or safe failure
```

Consumer tests:

```txt
Same event delivered twice -> side effect once
Concurrent duplicate deliveries -> one winner, one skip
Failure before processed mark -> retry allowed
Tenant context is set before handler data access
```

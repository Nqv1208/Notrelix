# Consumer Idempotency

## Selected Mechanism: DeduplicationConsumeFilter with Claim-Before-Execute

Idempotency is implemented at the MassTransit pipeline level using `DeduplicationConsumeFilter<T>` with a claim-before-execute pattern, not inside individual consumer handlers.

## Flow

```
Message Bus → TenantContextConsumeFilter → DeduplicationConsumeFilter → Consumer
                                                  │
                                                  ▼
                                    IMessageDeduplicationStore
                                        (claim → execute → mark)
```

1. `TenantContextConsumeFilter` sets tenant/RLS context
2. `DeduplicationConsumeFilter` opens transaction
3. `TryClaimProcessingAsync(eventId, consumerName)` — insert claim record with `Status = Processing`
   - Duplicate key / already succeeded → skip, rollback, ack
   - Claim success → continue
4. Actual consumer executes (`next.Send(context)`)
5. `MarkSucceeded(status = Succeeded, processedAt = now)`
6. `SaveChanges` + `Commit`
7. Ack message

## Idempotency Key

`(event_id, consumer_name)` — NOT just `event_id`. This ensures pub/sub semantics where the same event can be processed by different consumers independently.

## Race Condition Prevention

- Claim is inserted BEFORE consumer executes (claim-before-execute)
- Unique constraint on `(event_id, consumer_name)` at DB level prevents concurrent claims
- If consumer fails, transaction rolls back — claim is lost, message can be retried
- Two concurrent workers: one claims successfully, the other gets unique violation and skips

## Entity State

`MessagingProcessedEvent` represents consumer processing state:

| Status | Meaning |
|---|---|
| Processing | Claimed but not yet completed |
| Succeeded | Successfully processed |
| Failed | Processing failed (for future use) |

Fields: `EventId`, `ConsumerName`, `MessageName`, `MessageVersion`, `WorkspaceId`, `Status`, `ClaimedAt`, `ProcessedAt`, `FailedAt`, `ErrorMessage`.

## Failure Handling

If consumer throws after claim:
- Transaction rolls back entirely (claim is lost)
- Message is nacked by MassTransit
- Retry/redelivery can claim again
- External side effects (email/webhook) need their own idempotency at the destination

## Rules

- Do not implement manual consumer deduplication in handlers
- Do not reintroduce `ConsumerPipelineExecutor`
- Deduplication must use claim-before-execute pattern
- Consumers must not run before claim succeeds
- Idempotency key is `event_id + consumer_name`

## Dead Code Removed

- `ConsumerPipelineExecutor` — removed (was registered but never used)
- `IConsumerPipelineExecutor` — removed
- `IIntegrationEventConsumer<T>` — removed
- Manual dedup in `ProvisionPersonalWorkspace` and `SendWelcomeEmail` handlers — removed

## Testing

- Duplicate concurrent delivery: 2 messages with same EventId → exactly 1 consumer executes
- Sequential duplicate: deliver, complete, deliver again → second is skipped
- Different consumers same event: same EventId, different consumer names → both execute
- Consumer throw after claim: transaction rolls back, retry can claim again
- Duplicate while first is Processing: second skips before consumer

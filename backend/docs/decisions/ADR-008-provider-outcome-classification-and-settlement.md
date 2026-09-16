---
document_id: ADR-008
document_type: architecture-decision
status: Accepted
owner: backend-architecture
applies_to:
  - backend
  - backend-platform-messaging
  - provider-integration
evidence:
  - backend/src/Notrelix.Infrastructure/Integrations/Providers/N8nClient.cs
  - backend/src/Notrelix.Application/Features/Automation/Executions/Services/N8nDispatchUseCase.cs
  - backend/src/Notrelix.Infrastructure/Messaging/Consumers/Automation/N8nDispatchConsumer.cs
  - backend/src/Notrelix.Infrastructure/Messaging/DeduplicationConsumeFilter.cs
  - backend/tests/Notrelix.Infrastructure.Tests/Integrations/Providers/N8nClientTests.cs
  - backend/tests/Notrelix.Integration.Tests/Automation/AutomationN8nDurabilityIntegrationTests.cs
  - backend/tests/Notrelix.Integration.Tests/Automation/N8nDispatchRuntimeChainIntegrationTests.cs
review_on:
  - provider-outcome-classification-change
  - unknown-outcome-settlement-change
  - retry-ownership-change
  - consumer-transaction-ownership-change
---

# ADR-008: Provider Outcome Classification and Durable Settlement for External Webhook Dispatch

## ID

`ADR-008`

## Status

`Accepted` — 2026-09-16 (PF-FLOW-04 remediation; supersedes the classification
assumptions previously recorded as complete in the M11 execution record).

## Context

The n8n webhook dispatch chain is the first production consumer of the durable
Integration delivery mechanism. Reviewing the classification seam (M11 closure
audit, TAC 117D) revealed that the adapter classified every transport failure as
retryable and the consumer treated *unknown outcome* as a normal redelivery while
leaving the Automation execution Running. Two durable problems follow:

1. **Unknown outcome is not retriable by default.** A timeout, 5xx, connection
   reset, or bare `HttpRequestException` does not prove the provider never
   processed the call. Auto re-fire risks at-least-once duplication of an
   external side effect (the webhook run). This violates the Integrations
   requirement that unknown provider outcomes require explicit
   idempotency/reconciliation before retry.
2. **Retryable evidence must be durable before redelivery.** When the delivery
   mechanism redelivers, the Automation execution must already have committed
   the retryable attempt (status re-queued, attempt count incremented).
   Otherwise a crash between the provider call and the commit leaves either a
   lost execution or a second attempt that duplicates the first.

The Integrations bounded context owns the provider contract; Automation owns the
execution lifecycle; Platform owns delivery mechanics. The durable decision is
where the boundary between retryable and unknown lies and who is allowed to
re-fire an external call.

## Decision

Define the provider-outcome contract at the adapter seam and bind it to settlement:

```text
Succeeded            → execution.Succeed → intents complete (no re-delivery)
TerminalFailure      → execution.Fail    → intents complete (no re-delivery)
RetryableFailure     → RecordRetryableDispatchFailure (re-queued + attempt++)
                       → durable commit → consumer signals redelivery
UnknownOutcome       → execution.Fail("... — reconciliation required")
                       → intents complete (NO auto re-fire) → human/recon process
```

Classification rules (`N8nClient.ClassifyHttpFailure` + exception mapping):

```text
2xx                          → Succeeded
429                          → RetryableFailure  (only when the provider/gateway
                               contract guarantees reject-before-execution; recorded
                               as a precondition, verified per provider)
408, 5xx                     → UnknownOutcome     (provider may or may not have run)
other 4xx                    → TerminalFailure    (business rejection)
OperationCanceledException
  (caller cancellation)      → rethrow            (never classify as provider outcome)
TaskCanceledException        → UnknownOutcome     (timeout is not proof of non-effect)
HttpRequestException
  + Inner SocketException
  ConnectionRefused/Aborted
  HostNotFound               → RetryableFailure   (request never reached provider)
HttpRequestException (other) → UnknownOutcome     (post-send / reset: indeterminate)
```

The adapter NEVER retries internally; the delivery mechanism is the single retry
owner.

Consumer transaction ownership: the `N8nDispatchConsumer` opens and owns a
consumer-scoped database transaction (with RLS) so attempt evidence commits
atomically **before** any retryable exception reaches the delivery mechanism.
`N8nDispatchRetryableException` is the only throw path after commit. The dedicated
upload/claim path is exempted in `DeduplicationConsumeFilter` command-owned
endpoints because the evidence commit replaces the claim-success marker.

## Alternatives Considered

### Alternative A — Keep every transport failure retryable

**Description**

Treat timeout/reset/5xx as retryable and redeliver until a bounded retry budget
exhausts.

**Why not chosen**

Bounded retry does not remove duplicate side effects; it reduces them.
Integrations explicitly require reconciliation for unknown provider outcomes
(`NRX-009` retry non-idempotent effects without identity/reconciliation), so the
queue cannot be the only recovery control.

### Alternative B — Mark every unknown outcome as a separate "unknown/pending" execution state

**Description**

Introduce a durable `Unknown` Automation execution status and a reclamation job.

**Why not chosen**

Automation's lifecycle owns Succeeded/Failed/Cancelled/Queued/Running. A new
status is a product-semantic change requiring Automation lifecycle governance for
a single provider today, while the durable behavior (no auto re-fire, explicit
reconciliation signal) is fully expressed by Failed + "reconciliation required"
without inventing lifecycle vocabulary.

### Alternative C — Consumer relies on the shared pipeline transaction

**Description**

Keep the earlier consumer that depended on the enqueued pipeline/data-session
transaction and the dedup claim as the crash-recovery marker.

**Why not chosen**

The retryable path must commit before throwing; a claim that is removed on
exception is not durable evidence of the attempt. Consumer-owned commit is the
correct boundary for this provider effect chain and matches the existing
command-owned transaction allowlist pattern.

## Consequences

### Positive

- Unknown outcome never grows/retries an external effect by redelivery alone.
- Retryable attempt evidence survives MassTransit retry and crash windows.
- Classification is pinned by unit tests at the adapter seam and by a
  production-composition runtime-chain integration test.
- Reconcile-by-execution-id is explicit and observable (`— reconciliation required`).

### Negative / trade-offs

- 429 is classified RetryableFailure under a documented precondition; if a real
  provider does not guarantee reject-before-execution, that provider must be
  verified and downgraded to UnknownOutcome.
- The consumer now owns a database transaction; any future change to the
  pipeline transaction contract must account for this endpoint's dedicated tx.
- Unknown outcomes surface as Failed executions requiring a reconciliation
  process to re-enable; no automatic recovery is performed.

### New obligations

- tests/gates: keep `N8nClientTests` classification matrix and the
  `N8nDispatchRuntimeChainIntegrationTests` chains green with this matrix.
- operations: a reconciliation path (manual or future job) resolves
  "reconciliation required" executions against provider truth before re-run.
- security: consumer RLS applies inside the consumer-owned transaction; any new
  consumer adopting this pattern must apply RLS before its business effects.
- documentation: execution record and this ADR must not diverge again; update
  both in the same governed change.

## Compatibility / Migration

Nothing already-durable changes shape. The Automation execution status
enum/API/UI are unchanged (Failed is the pre-existing terminal state). No schema
migration, event payload change, or API contract change. Older queued messages
for executions already Succeeded/Failed are settled by the idempotency guard in
the use case (no re-fire, no re-queue). Rollout is single-version compatible:
new classification only affects outbound provider decisions from this point
forward; historical rows are unchanged.

## Evidence

### Canonical current architecture

- `backend/docs/architecture/platform-and-messaging.md`
- `backend/docs/architecture/application-model.md`
- `docs/architecture/events-realtime-and-delivery-boundary.md`

### Source / manifests

- `backend/src/Notrelix.Infrastructure/Integrations/Providers/N8nClient.cs`
- `backend/src/Notrelix.Application/Features/Automation/Executions/Services/N8nDispatchUseCase.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Automation/N8nDispatchConsumer.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/DeduplicationConsumeFilter.cs`

### Tests / gates

- `backend/tests/Notrelix.Infrastructure.Tests/Integrations/Providers/N8nClientTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/AutomationN8nDurabilityIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/N8nDispatchRuntimeChainIntegrationTests.cs`

## Supersedes

`None` — new decision; the classification model supersedes the assumptions in
the M11 execution record (record corrected in the same change).

## Superseded By

`None`

## Notes

The M11 execution record previously stated the classifier proved
`408/429/5xx → RetryableFailure` and `HttpRequestException → RetryableFailure`
and that UnknownOutcome surfaced a redelivery leaving the execution Running.
Those statements were superseded by this decision on 2026-09-16 and corrected in
the record.
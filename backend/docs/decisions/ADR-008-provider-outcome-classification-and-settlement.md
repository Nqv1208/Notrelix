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
  - backend/src/Notrelix.Infrastructure/Messaging/IProviderEffectClaimStore.cs
  - backend/src/Notrelix.Infrastructure/Messaging/MessageDeduplicationStore.cs
  - backend/src/Notrelix.Infrastructure/Messaging/DeduplicationConsumeFilter.cs
  - backend/tests/Notrelix.Infrastructure.Tests/Integrations/Providers/N8nClientTests.cs
  - backend/tests/Notrelix.Integration.Tests/Automation/AutomationN8nDurabilityIntegrationTests.cs
  - backend/tests/Notrelix.Integration.Tests/Automation/N8nDispatchRuntimeChainIntegrationTests.cs
review_on:
  - provider-outcome-classification-change
  - unknown-outcome-settlement-change
  - retry-ownership-change
  - consumer-transaction-ownership-change
  - provider-effect-claim-lifecycle-change
---

# ADR-008: Provider Outcome Classification and Durable Settlement for External Webhook Dispatch

## ID

`ADR-008`

## Status

`Accepted` — 2026-09-17 (third revision). The first accepted version (2026-09-16)
recorded a single consumer-owned transaction and a claim-off-on-exception model.
The second revision is superseding: the prepare/effect/settle protocol was implemented
as the M11 closure and the earlier version accurately describes the rejected
alternative. This third revision adds the active-vs-stale `Processing` claim gate
and the fail-closed claim transitions (retryable release and terminal settlement
must each prove affected == 1). The protocol decision, classification matrix,
residue contract and enforcement rules below are the current durable contract.

## Context

The n8n webhook dispatch chain is the first production consumer of the durable
Integration delivery mechanism. Reviewing the classification seam (M11 closure
audit, TAC 117D) exposed two durable problems:

1. **Unknown outcome is not retriable by default.** A timeout, 5xx, connection
   reset/abort, or bare `HttpRequestException` does not prove the provider never
   processed the call. Auto re-fire risks at-least-once duplication of an
   external side effect (the webhook run). This violates the Integrations
   requirement that unknown provider outcomes require explicit
   idempotency/reconciliation before retry.
2. **Retryable evidence must be durable before redelivery.** When the delivery
   mechanism redelivers, the Automation execution must already have committed
   the retryable attempt (status re-queued, attempt count incremented).
   Otherwise a crash between the provider call and the commit leaves either a
   lost execution or a second attempt that duplicates the first.

A crash during a provider dispatch can happen at three points, and each leaves a
distinct durable residue that redelivery must reconcile safely:

```text
after first-transaction commit (execution Running + claim Processing)
  → the provider may or may not have been called → MUST NOT re-fire
after a retryable commit is durable but before redelivery
  → execution re-queued + claim released → safe to re-acquire and retry
after a terminal settle commit (claim Succeeded)
  → redelivery must skip, never re-fire
```

The Integrations bounded context owns the provider contract; Automation owns the
execution lifecycle; Platform owns delivery mechanics. The durable decision is
where the boundary between retryable and unknown lies, how a consumer owns the
claim/effect so these residues stay reconcilable, and who is allowed to re-fire
an external call.

## Decision

### 1. Provider-outcome contract at the adapter seam

```text
Succeeded            → execution.Succeed→ intents complete (no re-delivery)
TerminalFailure      → execution.Fail   → intents complete (no re-delivery)
RetryableFailure     → RecordRetryableDispatchFailure (re-queued + attempt++)
                       → durable commit → consumer signals redelivery
UnknownOutcome       → execution.Fail("... — reconciliation required")
                       → intents complete (NO auto re-fire) → reconcile process
```

Classification rules (`N8nClient.ClassifyHttpFailure` + exception mapping):

```text
2xx                        → Succeeded
408, 429, 5xx              → UnknownOutcome  (provider may or may not have run;
                             a 429 WITHOUT a provider/gateway contract that
                             proves reject-before-execution is indeterminate —
                             treat as Unknown, never auto re-fired)
other 4xx                  → TerminalFailure (business rejection)
OperationCanceledException
  (caller cancellation)    → rethrow          (never classify as provider outcome)
TaskCanceledException      → UnknownOutcome   (timeout is not proof of non-effect)
HttpRequestException
  + Inner SocketException
  ConnectionRefused
  HostNotFound             → RetryableFailure (the connection to the provider was
                                               never established — safe to retry)
HttpRequestException (other,
  incl. ConnectionAborted) → UnknownOutcome   (post-send / reset / abort:
                                               the connection may have been
                                               established — indeterminate)
```

The adapter NEVER retries internally; the delivery mechanism is the single retry
owner (the unit tests pin `attempts == 1` alongside each classification).

### 2. Consumer-owned prepare/effect/settle protocol

`N8nDispatchConsumer` owns the full claim-and-effect lifecycle via
`IProviderEffectClaimStore` (implemented by `MessageDeduplicationStore`,
which also serves the shared dedup filter). The lifecycle is:

```text
Tx1 (RLS):
  acquire claim  → Processing          (unique on event_id + consumer_name)
  PrepareAttemptAsync:
    Queued+ready → execution.Start() → Running (in-memory; persisted below)
    Running      → interrupted residue → execution.Fail("... reconciliation required")
    terminal     → nothing to progress
    bad rule/config → execution.Fail(prerequisite)
  ReadyForDispatch:
    commit Tx1                → intent durable (Running + Processing)
  else:
    mark claim Succeeded + commit Tx1 → fully settled inside Tx1
    | (no provider call)

no database transaction:
  CallWebhookAsync  → single provider attempt, duration metric recorded

Tx2 (RLS):
  SettleAttemptAsync:
    Succeeded / TerminalFailure / UnknownOutcome → handled (persist below)
    RetryableFailure → RecordRetryableDispatchFailure → evidence durable below
  handled:
    mark claim Succeeded + SaveChanges + commit Tx2 → terminal
  retryable:
    release claim (DELETE Processing row) + SaveChanges + commit Tx2
    → invariant holds (see §3)
    → throw N8nDispatchRetryableException → delivery mechanism redelivers
```

On a failed acquire (claim already exists), the unique violation aborts the
transaction, so the consumer rolls back Tx1 and opens a **fresh** transaction to
inspect the residual claim (`InspectClaimAsync`) and reconcile only what the
durable state proves. The claim's `ClaimedAt` decides whether a `Processing`
claim is an active attempt or interrupted residue:

```text
claim Succeeded or Missing → nothing to do (normal duplicate/torn) → consume
claim Processing + FRESH (ClaimedAt within ProviderEffectClaimStaleAfter):
  → another attempt may still own the effect → duplicate is IGNORED → consume
    (NO execution mutation, NO claim mutation, NO provider call; the active
     attempt settles the outcome itself)
claim Processing + STALE (older than ProviderEffectClaimStaleAfter):
  execution terminal      → nothing to do → consume
  execution Running       → settle Failed ("reconciliation required"),
                            mark claim Succeeded → consume
                            (interrupted between the two durable transactions:
                             NEVER re-fire)
  execution Queued        → stale torn state → require operator sweep → consume
                            (never re-run the effect blindly; claim is left
                             blocking redelivery until the operator resolves it)
```

`ProviderEffectClaimStaleAfter` is the n8n dispatch protocol's active-ownership
window: configurable via typed options
(`N8n:ProviderEffectClaimStaleAfterSeconds`, default `300`), validated to exceed
the provider-call timeout (`N8nOptions.HttpClientTimeoutSeconds`), with a
fixed-clock semantics that a claim older than the window is a residue candidate.
The window grants the right to **reconcile durable state only** — it never
transfers the effect to the duplicate and never grants permission to re-fire the
provider. This closes the concurrent-duplicate version of crash boundary A: a
duplicate arriving while an attempt is genuinely in flight must not settle the
claim as if it were a crash, because the running attempt will commit its own
outcome.

The residue path never throws and never re-fires the provider; the message is
fully consumed so delivery cannot hot-loop. If the claim vanishes between
inspection and reconciliation, the residue path leaves the state untouched for a
later redelivery rather than inventing an outcome.

### 3. Retry-safe invariant

```text
Execution is queueable-for-retry      IFF no claim row blocks retry
  (execution re-queued + claim released commit atomically in Tx2)
Execution is terminal                 IFF the claim is Succeeded
  (terminal state + claim Succeeded commit atomically)
```

The transitions above are **enforced, not just documented**, at the consumer
boundary:

- Retryable settle: the claim release (`TryReleaseProcessingClaimAsync`) must
  report affected == 1 before the retryable evidence commits. A lost claim on the
  retryable path throws before `SaveChanges`, rolling back Tx2 so retry evidence
  never commits while a claim still blocks redelivery.
- Terminal settle (Tx1 early-settle and Tx2 handled path): the claim transition
  (`TryMarkClaimSucceededAsync`) must report affected == 1 before the terminal
  commit. A Succeeded outcome can never be committed with a claim still
  Processing.
- Residue reconciliation keeps the "never throws" contract: if the claim cannot
  be transitioned, the residue path warns and leaves the state for a later
  redelivery instead of forcing an outcome.

A claim removed while the execution is still Running (or removed before the
retryable evidence commits) would let redelivery re-fire an attempt whose outcome
is unknown, so the consumer only ever releases a `Processing` claim together with
durable retryable evidence.

### 4. Filter topology

`DeduplicationConsumeFilter` adds the dispatch endpoint to a
`ConsumerOwnedProtocolEndpoints` pass-through set (keyed by the shared
`N8nDispatchProtocolEndpoints.DispatchEndpointName` constant). The consumer owns
its RLS transactions and claim lifecycle, so wrapping it in the filter's own
transaction would open a second transaction on the shared connection. The
endpoint is consequently removed from the command-owned set.

## Alternatives Considered

### Alternative A — Keep every transport failure retryable

**Description**

Treat timeout/reset/5xx as retryable and redeliver until a bounded retry budget
exhausts.

**Why not chosen**

Bounded retry does not remove duplicate side effects; it reduces them.
Integrations explicitly require reconciliation for unknown provider outcomes
(`NRX-009`), so the queue cannot be the only recovery control.

### Alternative B — Mark every unknown outcome as a separate "unknown/pending" execution state

**Description**

Introduce a durable `Unknown` Automation execution status and a reclamation job.

**Why not chosen**

Automation's lifecycle owns Succeeded/Failed/Cancelled/Queued/Running. A new
status is a product-semantic change requiring Automation lifecycle governance for
a single provider today, while the durable behavior (no auto re-fire, explicit
reconciliation signal) is fully expressed by Failed + "reconciliation required"
without inventing lifecycle vocabulary.

### Alternative C — Single consumer transaction + claim removed on exception

**Description**

Keep the first accepted ADR-008 model: one consumer-owned transaction that
commits evidence, then removes the claim on `N8nDispatchRetryableException`
(after commit) so redelivery can re-acquire.

**Why not chosen**

Removing the claim after the retryable commit creates a window in which the
execution is re-queued while a `Processing` claim still exists; if the process
crashes in that window the next redelivery's acquire fails and the residue path
would settle the re-queued execution against the invariant instead of retrying.
The prepare/effect/settle protocol (Tx1 intent → Tx2 evidence+claim lifecycle)
removes that window by making the retryable evidence and the claim release one
atomic commit.

### Alternative D — Keep the shared pipeline/dedup transaction and claim as crash marker

**Description**

Do not give the consumer its own transactions; rely on the enqueued
pipeline/data-session transaction and the dedup claim.

**Why not chosen**

The retryable path must commit before throwing, the effect must run outside any
transaction, and a claim that is removed on exception is not durable evidence of
the attempt. Consumer-owned transactions under RLS are the correct boundary for a
provider-effect chain.

## Consequences

### Positive

- Unknown outcome (incl. 408/429/5xx/abort/reset/timeout) never re-fires an
  external effect by redelivery alone.
- Retryable attempt evidence and the claim release commit atomically, closing the
  re-queue window; redelivery re-acquires under the same execution identity.
- A crash at any point leaves a reconcilable residue that never duplicates the
  provider call: STALE Running+Processing → Failed-with-marker, terminal → skip,
  STALE Queued+Processing → operator sweep.
- A FRESH Processing claim is never treated as residue: a concurrent duplicate
  during an active attempt leaves the execution, claim, and provider untouched
  and the active attempt commits the terminal outcome.
- Claim transitions are fail-closed at the consumer boundary: a retryable
  release or terminal settle that cannot prove affected == 1 aborts its
  transaction instead of committing evidence against a stale claim.
- Classification is pinned by unit tests at the adapter seam (one attempt each)
  and by production-composition durability + runtime-chain integration tests.
- Reconcile-by-execution-id is explicit and observable (`— reconciliation required`).

### Negative / trade-offs

- The consumer owns two database transactions plus the out-of-transaction effect;
  future changes to the pipeline transaction contract must account for this
  endpoint's dedicated protocol.
- Unknown outcomes surface as Failed executions requiring a reconciliation
  process to re-enable; no automatic recovery is performed.
- 408/429/5xx/ConnectionAborted are deliberately conservative (Unknown): a
  provider that could safely retry these is not auto-re-fired today.

## Compatibility / Migration

Nothing already-durable changes shape. The Automation execution status
enum/API/UI are unchanged (Failed is the pre-existing terminal state). No schema
migration, event payload change, or API contract change. The claim table and
`MessageDeduplicationStore` already existed; `IProviderEffectClaimStore` exposes a
sub-set of that store's durable rows behind a new interface. Older queued
messages for executions already Succeeded/Failed are settled by the terminal
guards in the use case (no re-fire, no re-queue). Rollout is single-version
compatible: new classification only affects outbound provider decisions from this
point forward; historical rows are unchanged.

## Evidence

### Canonical current architecture

- `backend/docs/architecture/platform-and-messaging.md`
- `backend/docs/architecture/application-model.md`
- `docs/architecture/events-realtime-and-delivery-boundary.md`
- `docs/operations/recovery-and-data-safety.md`

### Source / manifests

- `backend/src/Notrelix.Infrastructure/Integrations/Providers/N8nClient.cs`
- `backend/src/Notrelix.Application/Features/Automation/Executions/Services/N8nDispatchUseCase.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Automation/N8nDispatchConsumer.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/IProviderEffectClaimStore.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/MessageDeduplicationStore.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/DeduplicationConsumeFilter.cs`

### Tests / gates

- `backend/tests/Notrelix.Infrastructure.Tests/Integrations/Providers/N8nClientTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/AutomationN8nDurabilityIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/N8nDispatchRuntimeChainIntegrationTests.cs`

## Supersedes

The 2026-09-16 revision of this ADR (single consumer transaction + claim removed
on exception + `429 → RetryableFailure` and `ConnectionAborted → RetryableFailure`
classifications). That revision is superseded by the prepare/effect/settle
protocol, the conservative classification matrix above, and the consumer-owned
claim lifecycle. The M11 execution record was corrected in the same governed
change. The 2026-09-17 (second) revision remains accurate except where this third
revision amends it: the `Processing` claim gate now distinguishes fresh
(in-flight, duplicate ignored) from stale (residue candidate), and the claim
release/settle transitions are fail-closed on affected == 1.

## Superseded By

`None`

## Notes

SYS-DATA-007 (durable fact: the provider-outcome contract) is owned by this ADR.
Changes to the classification matrix, the claim lifecycle, or retry ownership
must update this ADR, the runbook in `docs/operations/recovery-and-data-safety.md`,
and the pinned tests in the same change.
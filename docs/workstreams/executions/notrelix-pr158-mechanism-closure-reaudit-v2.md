# Notrelix Backend Mechanism Closure Re-Audit v2 — PR #158

> **Supersedes:** `notrelix-pr158-mechanism-closure-audit.md`  
> **Repository:** `Nqv1208/Notrelix`  
> **Pull Request:** `#158 — feat(backend): close architecture execution waves`  
> **Exact audited head:** `b9cccc07bf4b3bf2be04d7336b478eba6170f136`  
> **Base:** `54bcae56f2deeae3febdf2271505667b4d8aa9ea`  
> **Audit target:** mechanism closure by Bounded Context / cross-BC flow.  
> **Explicitly excluded by user:** Application-layer EF Core / `DbSet<T>` decoupling.

---

# 0. Why this v2 re-audit exists

The first PR #158 audit correctly identified the remaining Work event-evolution gap,
Calendar processing provenance gap, AI-FLOW-07 semantic blocker, and M14 CI blocker.

The re-audit found several additional issues and two places where the previous
execution direction was not strict enough:

1. **PF-FLOW-05 must be selectively reopened.**
   PR #158 introduced new Work event schemas, so event-evolution/replay proof is
   directly affected. “M11 unchanged” is too broad; the milestone does not need a
   wholesale reopen, but PF-FLOW-05/TAC-FRZ-018 compatibility evidence does.

2. **The current Calendar webhook implementation is a synthetic HMAC protocol,
   not the real Google Calendar or Microsoft Graph webhook protocol.**
   Since production source explicitly accepts Google/Microsoft Calendar providers,
   the real-provider mechanism is not closed.

3. **AI-FLOW-06 authority and implementation disagree.**
   TESTS/CERT require provider/secret cleanup outcome proof, but
   `DisconnectCalendarCommandHandler` performs only local DB lifecycle changes and
   explicitly says provider cleanup is outside the transaction. There is no
   provider cleanup runtime path.

4. **`CalendarWebhookProcessingRequestedConsumer` acknowledges a missing receipt.**
   `receipt == null` logs and returns, which can convert an invariant violation
   into a permanently successful consume.

5. **Rejected webhook diagnostics are not bounded.**
   Every forged callback can persist an encrypted payload row, while no receipt
   retention/cleanup mechanism exists.

6. **Receipt lifecycle is internally inconsistent.**
   `MarkProcessed()` accepts `Failed -> Processed`, while `Failed` is documented as
   terminal; `failedAt` and `blockedAt` arguments are ignored.

7. **Generated event-manifest tenant metadata is wrong for the new Calendar
   processing event.**
   Runtime envelope is valid, but `notrelix.events.json` reports
   `carriesAccountId=false`, `carriesWorkspaceId=false` because the generator only
   recognizes exact constructor parameter names, while the event uses
   `AccountIdValue` / `WorkspaceIdValue`.

8. **The Work V1/V2 manifest says `Backward` compatibility although V1 cannot
   supply the mandatory semantic `Revision`.**
   This is a false compatibility claim for revision-sensitive consumers.

9. **Platform replay/transport has deceptive skeleton surfaces.**
   Canonical production RabbitMQ is MassTransit and is real; the separate
   `RabbitMqTransportAdapter` is orphaned and throws `NotImplementedException`.
   Replay strategies also contain non-operational `yield break` implementations.
   These must not be counted as runtime closure evidence.

This document replaces the previous execution boundary.

---

# 1. Final verdict after full re-audit

## 1.1 TAC-XC mechanism level

```text
TAC-XC-A  PASS
TAC-XC-B  PASS
TAC-XC-C  CORE DELIVERY PASS
          EVENT EVOLUTION / REPLAY NOT CLOSED
TAC-XC-D  PASS
TAC-XC-E  PASS
TAC-XC-F  PASS
```

No seventh cross-context mechanism is needed.

## 1.2 Flow-level blockers

```text
WorkManagement / Analytics
  AR-FLOW-01:E2        CLOSED in implementation
  AR-FLOW-04           CLOSED in implementation/proof
  Work V1→V2 evolution OPEN
  PF-FLOW-05           REOPENED AFFECTED SLICE

Integrations / Calendar
  AI-FLOW-05 local persisted graph          PASS
  AI-FLOW-05 real provider subscription     OPEN
  AI-FLOW-06 local CAL-CONN-001 lifecycle   PASS
  AI-FLOW-06 provider cleanup lifecycle     OPEN / AUTHORITY-CONTRADICTION
  AI-FLOW-07 synthetic intake mechanics     MOSTLY PASS
  AI-FLOW-07 real Google/Microsoft protocol FAIL
  AI-FLOW-07 processing provenance          OPEN
  AI-FLOW-07 downstream semantic target     BLOCKED-DECISION

Automation
  AI-FLOW-03 runtime                         PASS
  AI-FLOW-03:E3 ProcessState authority       NORMALIZATION REQUIRED

Platform
  canonical MassTransit RabbitMQ             PASS
  PF-FLOW-05 event evolution/replay           OPEN
  orphan RabbitMqTransportAdapter             DELETE/RETIRE
  generic ReplayStrategy skeletons            NOT VALID CLOSURE EVIDENCE

Certification
  CERT table AI-FLOW-07=VERIFIED              INVALID / STALE
  STATUS Wave-E=BLOCKED-DECISION               CORRECT
  M14                                           NOT CLOSED
```

## 1.3 Current overall state

```text
BLOCKED-IMPLEMENTATION
+
BLOCKED-DECISION
+
BLOCKED-EVIDENCE
```

`ARCHITECTURE-CLOSED` is not supportable at the audited SHA.

---

# 2. Scope and unchanged packs

The re-audit reviewed the current authority set plus source changed by PR #158,
runtime dependencies touched by those changes, event contract/evolution surfaces,
Calendar provider mechanics, failure paths, and exact-head CI.

The following packs have no new source-level blocker caused by PR #158 and do not
need a wholesale reopen:

```text
M4 Identity / Accounts
M5 Workspace / Governance
M6 Work Management base ownership
M7 Documents / Collaboration
M9 Billing / Entitlements
```

Existing mandatory source/proof in those packs remains valid unless a later
implementation change modifies one of their contracts.

M11 is **not** globally reopened, but its affected mechanism is:

```text
PF-FLOW-05 — event evolution / compatibility / replay
```

because PR #158 changed three public Work event schemas.

---

# 3. Full flow-pack disposition

This is a mechanism-closure disposition, not a fresh reimplementation order.

## Identity / Accounts

```text
IA-FLOW-01  RETAIN
IA-FLOW-02  RETAIN
IA-FLOW-03  RETAIN
IA-FLOW-04  RETAIN
IA-FLOW-05  RETAIN
IA-FLOW-06  RETAIN
```

No PR #158 change creates a reason to reopen these flows.

## Workspace / Governance

```text
WG-FLOW-01  RETAIN
WG-FLOW-02  RETAIN
WG-FLOW-03  RETAIN
WG-FLOW-04  RETAIN
WG-FLOW-05  RETAIN
WG-FLOW-06  RETAIN
```

## Work Management

```text
WM-FLOW-01  RETAIN
WM-FLOW-02  RETAIN
WM-FLOW-03  RETAIN
WM-FLOW-04  RETAIN
WM-FLOW-05  RETAIN
```

The Work aggregate revision changes are correct. Contract evolution is handled
cross-pack under PF-FLOW-05 rather than reopening Work ownership.

## Documents / Collaboration

```text
DC-FLOW-01  RETAIN
DC-FLOW-02  RETAIN
DC-FLOW-03  RETAIN
DC-FLOW-04  RETAIN
DC-FLOW-05  RETAIN
DC-FLOW-06  RETAIN
DC-FLOW-07  RETAIN
```

The remaining `MovePage`, `SetPageDeadline`, `PublishPage` feature stubs are not
mandatory flow blockers for this architecture execution. Do not expand this
workstream into unrelated Documents feature completion.

## Automation / Integrations

```text
AI-FLOW-01  RETAIN
AI-FLOW-02  RETAIN
AI-FLOW-03  RETAIN SOURCE; NORMALIZE ProcessState AUTHORITY
AI-FLOW-04  RETAIN
AI-FLOW-05  LOCAL GRAPH PASS; REAL PROVIDER LIFECYCLE OPEN
AI-FLOW-06  LOCAL POLICY PASS; PROVIDER CLEANUP OPEN
AI-FLOW-07  REOPEN
```

## Billing

```text
BI-FLOW-01  RETAIN
BI-FLOW-02  RETAIN
BI-FLOW-03  RETAIN
BI-FLOW-04  RETAIN
BI-FLOW-05  CONDITIONAL / NOT REQUIRED AT CURRENT CANDIDATE
```

## Analytics

```text
AR-FLOW-01  CLOSED WITH PRODUCER REVISION
AR-FLOW-02  RETAIN
AR-FLOW-03  RETAIN
AR-FLOW-04  CLOSED WITH REVISION/REBUILD/CONCURRENCY PROOF
```

## Platform

```text
PF-FLOW-01  RETAIN
PF-FLOW-02  RETAIN
PF-FLOW-03  RETAIN
PF-FLOW-04  RETAIN
PF-FLOW-05  REOPEN AFFECTED EVENT-EVOLUTION/REPLAY SLICE
PF-FLOW-06  RETAIN
PF-FLOW-07  RE-RUN AFFECTED WORK V2 / CALENDAR TENANT-ENVELOPE PROOF
```

---

# 4. CLOSED — WorkManagement → Analytics producer-revision mechanism

## Evidence

Production chain:

```text
BoardItem.Version
→ BoardItemCreated/Moved/Archived DomainEvent.Version
→ Board*IntegrationEventV2.Revision
→ Analytics placement consumer
→ WorkspaceWorkItemPlacementService
→ WorkspaceWorkItemPlacementProjection.SourceRevision
```

Relevant source:

```text
backend/src/Notrelix.Domain/WorkManagement/Items/BoardItem.cs
backend/src/Notrelix.Application/EventMappers/WorkManagement/BoardEventMapper.cs
backend/src/Notrelix.Application/Events/WorkManagement/BoardItem*IntegrationEventV2.cs
backend/src/Notrelix.Infrastructure/Messaging/Consumers/Analytics/WorkItemPlacementConsumers.cs
backend/src/Notrelix.Application/Features/Analytics/Placements/Services/WorkspaceWorkItemPlacementService.cs
backend/src/Notrelix.Application/Features/Analytics/Projections/WorkItemPlacement/WorkspaceWorkItemPlacementProjection.cs
```

## Correct invariants

```text
EventId/MessageId = delivery identity
Revision          = semantic ordering authority
OccurredAt         = temporal metadata
xmin               = physical optimistic concurrency
```

Live update:

```text
incoming Revision > local SourceRevision  → apply
incoming Revision <= local SourceRevision → no-op
```

Rebuild:

```text
snapshot Revision < local SourceRevision  → preserve live state
snapshot Revision = local SourceRevision  → deterministic drift repair allowed
snapshot Revision > local SourceRevision  → reconcile forward
```

Rows absent from a stale snapshot are revalidated against the producer before
deletion.

## Migration

Before the dev-stage rebaseline, the placement revision transition was carried
by:

```text
20260919172502_ResetLegacyPlacementSourceRevision
```

That historical migration is now folded into the governed single development
baseline documented below.

Old tick-scale values and aggregate versions are incomparable numeric domains.

## Required action

```text
FREEZE
DO NOT REDESIGN
```

Only event-evolution/cutover work remains.

## Dev-stage migration rebaseline update — 2026-09-21

The migration chain was consolidated under the repository's governed
development-only exception. The current chain is now exactly:

```text
20260702093805_SchemaV2Baseline
```

The former connection-scoped receipt DDL, receipt provenance/terminal-state
DDL, and legacy placement source-revision reset are present in the single
baseline generated from the final model. Development databases created from
the previous chain must be reset rather than upgraded. The historical
transition-only `LegacyPlacementSourceRevisionMigrationTests` proof was
retired because the pre-Wave-B migration boundary no longer exists after the
dev rebaseline.

---

# 5. P0 — Work V1→V2 event evolution / PF-FLOW-05 is not closed

## Evidence

Changed public events:

```text
board.item.created v1 → v2
board_item.moved v1 → v2
board_item.archived v1 → v2
```

V2 adds mandatory semantic:

```text
Revision : Int64
```

The checked-in manifest currently labels both V1 and V2:

```text
compatibility = Backward
```

V1 rows have no active consumers; live consumer endpoints moved to `-v2`.

Generic platform source exists:

```text
IUpcaster
UpcasterRegistry
VersionCompatibilityEvaluator
```

but no production Work V1→V2 upcaster exists.

Existing compatibility/replay test covers PageCreated V1 tenant envelope, not the
changed Work schemas.

## Root cause

V1 does not carry `BoardItem.Version`.

Therefore an automatic transformation cannot safely derive V2 `Revision`.

Forbidden:

```text
Revision = OccurredAt.UtcTicks
Revision = broker sequence
Revision = consumer sequence
Revision = DateTime timestamp
```

Those recreate the defect that Wave B fixed.

## Mandatory resolution

### 5.1 Reclassify compatibility

For these version pairs, use:

```text
SchemaCompatibility.None
```

or an equivalent explicit non-upcastable/breaking classification in the
contract registry.

Do not keep `Backward` merely because V1 CLR types remain checked in.

V1 may be retained as:

```text
schema-history-decodable
```

but not:

```text
semantically equivalent to V2
```

### 5.2 Add an event-evolution policy registry

Introduce an explicit policy for changed event families:

```text
EventName
FromVersion
ToVersion
Disposition
Recovery
CutoverRequirement
```

For these three Work events:

```text
Disposition       = DrainBeforeCutover
Recovery          = ProducerSnapshotRebuild
SyntheticUpcast   = Forbidden
```

The architecture gate must fail every future version bump that has no explicit
evolution policy.

### 5.3 Broker cutover

Changing endpoint name from `-v1` to `-v2` does not migrate old durable queue
messages.

Release procedure must prove:

```text
old V1 queues/backlogs = drained
OR
old V1 queues = explicitly quarantined with a documented reconciliation action
```

before V2-only consumers become authoritative.

Add a deployment checker/runbook for affected queue names.

### 5.4 Analytics recovery

Legacy Work V1 must never be replayed into the revision-ordered projection.

Recovery:

```text
IWorkItemProjectionSource
→ authoritative current producer snapshot
→ snapshot.Revision
→ rebuild/reconcile
```

### 5.5 Automation cutover

Automation consumers also moved to V2.

Do not silently strand V1 automation backlog.

Use the same release gate:

```text
V1 automation endpoint backlog = 0
```

before retiring V1 consumers.

Do not add a V1 automation bridge unless product explicitly requires historical
automation effects to execute after cutover.

## Tests

```text
EVOL-01 V1 created cannot mutate revision projection
EVOL-02 V1 moved cannot mutate revision projection
EVOL-03 V1 archived cannot mutate revision projection
EVOL-04 changed event pair is SchemaCompatibility.None
EVOL-05 changed event has explicit evolution policy
EVOL-06 no timestamp-based upcaster exists
EVOL-07 V2 live path retains producer revision
EVOL-08 old queue cutover gate detects pending V1 messages
EVOL-09 authoritative rebuild recovers projection after V1 retirement
```

## Exit criteria

```text
[ ] manifest no longer falsely says Backward for unsafe V1→V2
[ ] PF-FLOW-05 has concrete policy for the three changed contracts
[ ] no synthetic revision upcast exists
[ ] V1 broker backlog has an executable cutover gate
[ ] Analytics recovery is producer-snapshot rebuild
[ ] Automation V1 backlog is not silently abandoned
[ ] TAC-FRZ-018 compatibility/replay evidence is rerun
```

---

# 6. P0 — Calendar webhook protocol is not real Google/Microsoft protocol

## Current source

Current route:

```text
/api/v1/integrations/calendar/webhooks/{provider}/{webhookPath}
```

Current verifier assumes every provider sends:

```text
X-Calendar-Signature
X-Calendar-Timestamp
HMAC-SHA256(secret, "{timestamp}.{rawBody}")
JSON body with top-level eventId
```

Current API boundary requires:

```text
Content-Type: application/json
non-empty body
valid JSON
```

`ConnectCalendar` maps real provider identities:

```text
IntegrationProvider.Google
IntegrationProvider.Microsoft
→ CalendarProvider.Google / Outlook
```

Therefore the current transport contract presents itself as Google/Microsoft
Calendar integration, not merely a synthetic test provider.

## External protocol verification

Official Google Calendar push-notification documentation states that notification
channels are created with a `watch` operation; delivery is identified by
`X-Goog-Channel-ID`, `X-Goog-Message-Number`, resource headers and optional
`X-Goog-Channel-Token`. Google Calendar notification messages can contain no
message body.

Official Microsoft Graph webhook documentation states that subscriptions must be
created/renewed, notification URL validation uses a `validationToken` request
that must be echoed as `text/plain`, normal payloads may contain multiple
notifications in `value[]`, and `clientState` is used for authenticity for basic
notifications. Rich notifications add validation-token/JWT requirements.

Thus the generic `X-Calendar-* + HMAC + eventId JSON` protocol is not a valid
implementation of either production provider.

## Consequences

Real Google callback:

```text
empty body
→ current endpoint rejects 400
```

Microsoft subscription validation:

```text
text/plain + validationToken query
→ current endpoint rejects 415
```

Google does not provide the assumed top-level JSON `eventId`.

Microsoft can batch notifications, while current intake assumes one callback =
one event ID.

This makes current AI-FLOW-07 evidence a **synthetic protocol proof**, not a real
Google/Microsoft provider proof.

## Mandatory architecture correction

Do not keep one fake universal verifier.

Create provider-specific transport adapters behind a common normalized result.

### 6.1 Inbound adapter boundary

Define a provider-neutral HTTP envelope:

```text
CalendarWebhookHttpEnvelope
  Provider
  WebhookPath
  Method
  ContentType
  RawBytes
  Headers
  Query
  ReceivedAt
```

Provider adapter:

```text
ICalendarWebhookProtocolAdapter
  Provider
  VerifyAndNormalizeAsync(...)
```

Output:

```text
VerifiedCalendarNotificationBatch
  BindingIdentity
  Notifications[]
```

Each normalized notification must contain only provider-neutral technical
delivery metadata needed by Integrations:

```text
ProviderDeliveryId
ProviderSubscriptionIdentity
NotificationKind
ResourceIdentity?     # only when provider contract gives one
ProviderSequence?     # only when provider contract gives one
ReceivedAt
```

Do not pretend this is already a business-domain Calendar event.

### 6.2 Google adapter

Implement Google Calendar semantics:

```text
X-Goog-Channel-ID
X-Goog-Channel-Token
X-Goog-Message-Number
X-Goog-Resource-ID
X-Goog-Resource-State
X-Goog-Resource-URI
X-Goog-Channel-Expiration
```

Required:

```text
channel ID maps to stored provider subscription
channel token verified against protected/hashed stored authority
resource identity matches stored channel
message number treated as provider channel sequence/delivery identity
empty body accepted
sync notification handled explicitly
```

### 6.3 Microsoft Graph adapter

Implement:

```text
notificationUrl validationToken handshake
subscriptionId
clientState verification
batched value[]
subscription expiration/renewal
lifecycle notifications if enabled
rich-notification JWT validation when rich mode is enabled
```

Do not require the synthetic HMAC headers.

### 6.4 HTTP endpoint behavior

The API boundary selects protocol behavior by trusted configured provider
adapter.

Provider-specific accepted content/body shapes are owned by the adapter.

The current global rule:

```text
only application/json + non-empty body
```

must be removed from the generic Calendar webhook endpoint.

Body-size limits remain mandatory, but media type/body requirements are
provider-specific.

## Tests

Use protocol fixtures matching official provider shapes.

Google:

```text
GOOG-01 valid channel/token/header-only notification
GOOG-02 empty body accepted
GOOG-03 wrong channel token rejected
GOOG-04 unknown channel ID rejected
GOOG-05 resource mismatch rejected
GOOG-06 duplicate channel/message number converges
GOOG-07 sync notification does not create false business mutation
```

Microsoft:

```text
MSFT-01 validationToken handshake returns text/plain token
MSFT-02 valid clientState accepted
MSFT-03 clientState mismatch rejected
MSFT-04 batched notifications split into logical deliveries
MSFT-05 unknown subscription rejected
MSFT-06 expired/revoked subscription rejected
MSFT-07 rich notification validates JWT when rich mode enabled
```

## Exit criteria

```text
[ ] generic fake HMAC is no longer Google/Microsoft authority
[ ] Google official delivery shape passes
[ ] Microsoft official validation/delivery shape passes
[ ] provider transport normalizes to one Integrations technical contract
[ ] raw provider protocol does not leak into target BCs
```

---

# 7. P0 — Real Calendar provider subscription lifecycle is missing

## Evidence

`ConnectCalendarCommandHandler` currently creates:

```text
IntegrationConnection
IntegrationSecretVersion
CalendarIntegration(WebhookPath)
```

but no Google `watch` channel or Microsoft Graph subscription is created.

`CalendarIntegration` stores no provider notification-channel/subscription
identity or expiration.

`DisconnectCalendarCommandHandler` explicitly does not perform provider cleanup.

## Authority inconsistency

Current SPEC classifies AI-FLOW-05 remote substitution as not required, but TESTS
for AI-FLOW-06 explicitly require provider/secret cleanup outcome classes:

```text
success
retryable failure
terminal failure
unknown outcome
retry after unknown
```

The certification table marks AI-FLOW-06 VERIFIED despite no such runtime path.

This is not an implementation detail; it is authority drift.

## Mandatory direction

Keep `IntegrationConnection` and `CalendarIntegration` as their current semantic
owners.

Add a separate **Integrations-owned technical provider subscription lifecycle**.

Do not overload `CalendarIntegration` with raw provider protocol state.

Recommended technical state:

```text
CalendarProviderSubscription
  Id
  CalendarIntegrationId
  ConnectionId
  Provider
  ProviderSubscriptionId / ChannelId
  ProviderResourceId
  ProtectedVerificationSecretRef or secure token reference
  ExpiresAt
  Status
  LastProviderSequence?
  CreatedAt
  RenewedAt?
  LastFailureCode?
```

Suggested lifecycle:

```text
Provisioning
Active
RenewalPending
RevokePending
Revoked
Failed
Unknown
```

This state is technical integration reliability state, not a new business
aggregate.

## Provider ports

```text
ICalendarSubscriptionProvider
  ProvisionAsync
  RenewAsync
  RevokeAsync
  ReconcileAsync
```

Implement one adapter per provider.

## Transaction topology

Do not make a provider HTTP operation part of the local DB transaction.

### Connect

```text
ConnectCalendar local transaction
→ persist Connection + SecretVersion + CalendarIntegration
→ enqueue CalendarProviderSubscriptionProvisioningRequested in same outbox commit

after commit:
consumer
→ provider ProvisionAsync
→ persist CalendarProviderSubscription outcome
```

`CalendarIntegration.IsActive` continues to mean the local integration binding is
enabled. Provider notification readiness is represented by
`CalendarProviderSubscription.Status`.

This avoids redefining existing Domain semantics.

### Disconnect

```text
request transaction
→ CalendarIntegration.Deactivate FIRST
→ CAL-CONN-001 local generic Connection decision
→ enqueue provider revoke request when an active provider subscription exists

after commit:
provider cleanup consumer
→ success      → Revoked
→ retryable    → retry
→ terminal     → Failed
→ unknown      → Unknown + reconciliation
→ retry unknown only through explicit Reconcile/operation identity
```

This directly satisfies the provider-cleanup outcome authority without distributed
atomicity.

### Renewal

A background worker/job must renew provider subscriptions before expiry.

The renewal job is tenant/provider scoped and uses the stored subscription
identity.

## Required architecture changes

```text
AI-FLOW-05
  retain local persisted-graph proof
  add supporting provider-subscription provisioning chain

AI-FLOW-06
  retain deactivation-first + CAL-CONN-001 proof
  add provider cleanup runtime proof

AI-FLOW-07
  trusted callback must resolve an Active CalendarProviderSubscription
```

## Exit criteria

```text
[ ] real provider subscription/channel is provisioned
[ ] provider identity/expiration is durable
[ ] renewal exists
[ ] disconnect enqueues cleanup after local deactivation
[ ] provider failure/unknown outcomes are explicit
[ ] no distributed transaction is claimed
[ ] AI-FLOW-06 TESTS and production source agree
```

---

# 8. P0 — AI-FLOW-07 downstream semantic target remains undefined

## Current source

```text
CalendarWebhookProcessingUseCase.ProcessAsync(...)
→ SemanticTargetUndefined
```

Consumer:

```text
SemanticTargetUndefined
→ receipt.MarkBlocked(...)
```

This is a valid no-guess stop state, not completion.

## Important correction

Do **not** wire:

```text
TriggerCalendarSyncCommand
```

merely because it still throws `NotImplementedException`.

Current STATUS explicitly excludes it from AI-FLOW-07, and repository authority
does not prove it is the semantic target.

## Required decision record before coding

Product/Integrations authority must freeze:

```text
1. Which provider notification kinds are supported?
2. Is a notification:
   - a change fact,
   - an invalidation signal requiring provider pull,
   - a lifecycle signal,
   - or a subscription-management signal?
3. What is the exact provider-neutral semantic?
4. Which BC owns the target effect?
5. What exact target-owned command/action/event is used?
6. What is semantic idempotency identity?
7. Is ordering required and what is its authority?
8. How are batch notifications decomposed?
9. What happens when the connection is revoked after intake but before processing?
10. How does SyncDirection affect processing?
11. What is retryable?
12. What is terminal?
13. What is unknown-outcome/reconciliation behavior?
```

The decision is complete only when a coding agent can implement it without
choosing business semantics.

Forbidden authority language:

```text
"process the webhook"
"sync appropriately"
"dispatch to the target service"
"update the calendar"
```

## Final target shape

```text
real provider callback
→ provider protocol adapter
→ verified technical notification
→ durable receipt/dedup
→ tenant-restored Integrations processing
→ provider-neutral semantic translation
→ TAC-XC-A..F approved target interaction
→ target-owned semantic effect
→ receipt Processed
```

`Processed` means business-semantic processing succeeded, not merely transport
verification.

## Exit criteria

```text
[ ] exact semantic frozen
[ ] target BC frozen
[ ] TAC-XC mechanism frozen
[ ] happy path no longer SemanticTargetUndefined
[ ] duplicate callback produces one semantic effect
[ ] retry converges
[ ] terminal/unknown outcomes truthful
[ ] cross-tenant target fails closed
```

Until then:

```text
AI-FLOW-07 = BLOCKED-DECISION
```

---

# 9. P1 — Processing message must be bound to receipt authority

## Current defect

Consumer loads receipt by:

```text
ReceiptId
```

but passes message fields downstream:

```text
ConnectionId
Provider
ExternalEventId
PayloadHash
ReceivedAt
```

without proving they match the receipt.

## Required correction

Accepted receipt must persist immutable trusted provenance:

```text
AccountId
WorkspaceId
ConnectionId
Provider
ProviderDeliveryId
PayloadHash
ReceivedAt
```

`AccountId` and `WorkspaceId` come from the verified binding, never payload.

Change intake port to accept one claim object rather than loose parameters:

```text
CalendarWebhookReceiptClaim
  AccountId
  WorkspaceId
  ConnectionId
  Provider
  ProviderDeliveryId
  RawPayload / provider normalized payload reference
  ReceivedAt
```

Processing consumer order:

```text
1. restore tenant from event envelope
2. load receipt
3. require receipt exists
4. require receipt status eligible
5. compare:
     AccountId
     WorkspaceId
     ConnectionId
     Provider
     ProviderDeliveryId
     PayloadHash
6. on mismatch → invariant failure
7. only then decrypt/read technical payload
8. construct Application processing input FROM RECEIPT AUTHORITY
```

Do not use message copies as business input after the receipt has been loaded.

### ReceivedAt

Do not make exact `DateTimeOffset` equality a security invariant unless database
precision is normalized.

Use receipt `ReceivedAt` as authoritative processing metadata.

## Failure behavior

```text
provenance mismatch
→ terminal technical invariant failure
→ no semantic use case call
→ metric/structured log
→ no infinite retry
```

Tests:

```text
wrong ConnectionId
wrong AccountId
wrong WorkspaceId
wrong Provider
wrong ProviderDeliveryId
wrong PayloadHash
valid provenance
```

---

# 10. P1 — Missing receipt is currently acknowledged as success

## Current code

```text
receipt = ... FirstOrDefaultAsync(...)

if (receipt is null)
{
    log warning
    return;
}
```

In a durable message consumer, this turns a broken invariant into successful
message handling.

Possible causes include:

```text
bad/manual replay
corrupt outbox message
retention race
DB/data repair error
future migration bug
```

## Required behavior

Missing receipt is not an idempotent no-op.

Introduce a typed invariant exception:

```text
CalendarWebhookReceiptNotFoundException
```

Disposition:

```text
message references missing durable receipt
→ fail consume
→ classify non-retryable after DB availability is known
→ fault/DLQ/reconciliation
→ alert metric
```

Database connectivity exceptions remain retryable naturally; an executed query
that conclusively returns no receipt is a data invariant failure.

Never `return` success.

Test:

```text
valid processing message + missing ReceiptId
→ consumer faults
→ semantic use case not called
→ observable fault/reconciliation signal exists
```

---

# 11. P1 — Rejected webhook persistence is an unbounded storage-amplification path

## Current behavior

A bad signature/timestamp executes:

```text
RecordRejectedAsync
→ new InboundWebhookReceipt
→ random rejected:* ExternalEventId
→ encrypted full raw payload
→ DB commit
```

Every rejection is unique.

No receipt-retention cleanup job was found.

IP rate limiting reduces request rate, but does not make durable storage bounded
over system lifetime.

## Security/reliability issue

The caller is unauthenticated/untrusted at this point.

An attacker who reaches a valid path can cause persistent database growth and
encrypted garbage retention.

The entity documentation says rejected diagnostics are “bounded”, but runtime is
not bounded.

## Mandatory correction

Accepted and rejected deliveries must not share the same durable behavior.

### Accepted verified callback

Persist durable `InboundWebhookReceipt`.

### Rejected untrusted callback

Do **not** persist full raw payload in the receipt table.

Emit bounded operational telemetry only:

```text
provider
reason code
request/correlation id
webhook-path hash or binding id if safe
payload hash
payload size
timestamp
remote/rate-limit classification where policy allows
```

Do not log/store raw body.

Use normal log/metric retention owned by Operations.

Remove or stop using:

```text
ICalendarWebhookIntake.RecordRejectedAsync(...)
InboundWebhookReceipt.CaptureRejected(...)
```

for unauthenticated raw payload persistence.

If compliance later requires rejected-request evidence, create a separately
governed bounded security-audit store with explicit TTL; do not silently use the
business/reliability receipt table.

## Tests

```text
forged callback → no inbound receipt row
forged callback → safe rejection metric emitted
forged callback → raw payload absent from DB/log
high-volume rejections → no durable row amplification
```

---

# 12. P1 — Receipt state machine is not internally closed

## Current issues

Entity stores status as free string.

`MarkProcessed()` currently permits:

```text
Captured → Processed
Failed   → Processed
```

but source comments treat Failed as terminal.

`MarkFailed(reason, failedAt)` ignores `failedAt`.

`MarkBlocked(reason, blockedAt)` ignores `blockedAt`.

Only `ProcessedAt` exists.

## Mandatory correction

Introduce an explicit receipt status enum persisted as string:

```text
Captured
Processed
Blocked
Failed
```

`Rejected` leaves this table under finding #11.

Add:

```text
TerminalAt
FailureCode
FailureDetail?   # bounded/sanitized
```

Transition table:

```text
Captured → Processed
Captured → Blocked
Captured → Failed

Processed → no transition
Blocked   → no transition
Failed    → no transition
```

Retryable failure does **not** change terminal state:

```text
Captured
→ throw retryable
→ transaction rollback / redelivery
```

If future processing needs an explicit leased/in-progress state, add it only with
a lease/timeout recovery invariant.

## Tests

```text
terminal states cannot transition
terminal timestamp always populated
Processed has no failure code
Blocked/Failed have machine-readable reason
retryable attempt remains Captured after rollback
```

---

# 13. P1 — Event manifest tenant-envelope metadata generator is wrong

## Evidence

Event:

```text
CalendarWebhookProcessingRequestedV1(
  ...
  Guid AccountIdValue,
  Guid WorkspaceIdValue,
  ...
)
```

passes these values to base:

```text
IntegrationEvent.accountId
IntegrationEvent.workspaceId
```

Runtime tenant restore is therefore valid.

However generated manifest says:

```json
"tenantScope": "Workspace",
"carriesAccountId": false,
"carriesWorkspaceId": false
```

## Root cause

`ScopedEventTenantEnvelopeArchitectureTests` correctly accepts parameter names
that contain:

```text
AccountId
WorkspaceId
```

including `AccountIdValue` / `WorkspaceIdValue`.

`EventManifestGenerator.BuildScope()` only checks exact primary-constructor names:

```text
accountId
workspaceId
```

The gate and generator use different envelope-detection algorithms.

## Mandatory fix

Create one shared envelope-inspection helper used by:

```text
ScopedEventTenantEnvelopeArchitectureTests
EventManifestGenerator
```

Rules:

```text
Workspace-scoped:
  required AccountId-like constructor parameter
  required WorkspaceId-like constructor parameter
  both values flow to base envelope

Account-scoped:
  required AccountId-like parameter
```

Regenerate manifest after fixing generator.

Expected Calendar processing event metadata:

```text
carriesAccountId = true
carriesWorkspaceId = true
```

Add a regression specifically for `AccountIdValue` / `WorkspaceIdValue`.

---

# 14. P1 — AI-FLOW-06 certification currently overclaims provider cleanup

## Source reality

`DisconnectCalendarCommandHandler` correctly:

```text
loads CalendarIntegration
deactivates it first
checks other active bindings
revokes generic IntegrationConnection only for last active binding
```

This closes CAL-CONN-001 local state.

It explicitly states provider cleanup is outside the local transaction.

No production provider-cleanup port/consumer was found.

## Authority reality

TESTS requires:

```text
success
retryable failure
terminal failure
unknown outcome
retry after unknown
```

CERT marks AI-FLOW-06 `VERIFIED`.

Those two facts cannot both be true.

## Required correction

Do not weaken TESTS to match missing source.

Implement the asynchronous provider cleanup mechanism described in finding #7,
then certify it.

Until that exists:

```text
AI-FLOW-06
  local DB lifecycle = VERIFIED
  provider cleanup   = OPEN
  whole-flow status  = NOT FULLY VERIFIED
```

---

# 15. P1 — Certification table and STATUS contradict each other

At exact head:

```text
CERTIFICATION:
  AI-FLOW-07 = VERIFIED

STATUS:
  WAVE-E = BLOCKED-DECISION
  downstream Calendar semantic target undefined
```

The STATUS interpretation is correct because source actually returns
`SemanticTargetUndefined`.

## Mandatory correction

Before any new source work, update the authority state to:

```text
AI-FLOW-07 = BLOCKED-DECISION / IMPLEMENTATION-PARTIAL
```

or the repository's exact approved status vocabulary.

Do not allow the aggregate certification table to remain `VERIFIED`.

Similarly:

```text
AI-FLOW-06
```

must distinguish local CAL-CONN-001 proof from missing provider cleanup.

`ARCHITECTURE-CLOSED` evaluation must consume the corrected table, not an older
VERIFIED row.

---

# 16. P1 — PF-FLOW-05 Platform replay implementation is not valid closure evidence

## Source

Generic components exist:

```text
IUpcaster
UpcasterRegistry
ReplayEngine
ReplayStrategyBase
```

but current replay strategies are non-operational:

```text
CheckpointReplayStrategy → yield break
LatestReplayStrategy     → yield break
SnapshotReplayStrategy   → yield break
TimeWindowReplayStrategy → yield break
```

A separate Infrastructure `ReplayPipeline` publishes a synthetic replay envelope
rather than demonstrably rehydrating the original retained event payload.

## Important distinction

This does **not** mean the whole messaging stack is fake.

Production delivery is real:

```text
Outbox
→ IIntegrationEventBus
→ MassTransit
→ RabbitMQ in staging/production
```

The gap is specifically replay/evolution semantics.

## Mandatory direction

Do not implement a generic event store/replay engine merely to make a test green.

PF-FLOW-05 must define replay per event-family capability:

```text
ReplayableSameSchema
Upcastable
DrainBeforeCutover
RebuildFromAuthority
NotReplayable
```

For Work placement V1→V2:

```text
DrainBeforeCutover
+
RebuildFromAuthority
```

If the Platform `ReplayEngine` remains a public/reference mechanism, it must have
a real retained-event source and executable strategies.

Otherwise:

```text
remove it from closure evidence
mark it deferred/non-canonical
```

Do not count skeleton classes as implementation.

---

# 17. P2 — Orphan RabbitMqTransportAdapter is a deceptive duplicate surface

`Notrelix.Platform.Messaging.Transport.RabbitMqTransportAdapter.SendAsync()`
throws `NotImplementedException`.

However it is not the production RabbitMQ path.

Canonical production source is:

```text
Notrelix.Infrastructure/DependencyInjection/MessagingRegistration.cs
→ MassTransit
→ UsingRabbitMq(...)
```

No DI registration was found for the Platform `ITransportAdapter`.

## Required action

Do **not** implement a second RabbitMQ stack.

Delete/retire:

```text
RabbitMqTransportAdapter
ITransportAdapter
```

if no supported runtime uses them, or explicitly mark the entire package surface
non-canonical/deprecated until removed.

Add a composition test proving production broker transport is MassTransit only.

This is M13 cleanup, not a TAC-XC-C runtime blocker.

---

# 18. P2 — Producer-source “not found” and “unavailable” must remain distinct

Analytics has both:

```text
GetItemPlacementAsync(...) → null
```

and source-fetch exceptions.

These must retain distinct semantics:

```text
null
= authoritative producer says item does not exist

exception / unavailable result
= producer truth could not be obtained
```

Never collapse a remote/network failure into `null`, because rebuild may delete a
valid projection row.

Current rebuild tests cover source-unavailable failure and retry.

One test named:

```text
CreatedConsumer_SnapshotUnavailable_LeavesProjectionUnchanged
```

actually uses a null snapshot result rather than a thrown/unavailable source.

## Required action

Rename the test to reflect `NotFound` semantics and add a true consumer-source
unavailable test that throws/retries if that path can become remote.

This is a future-distribution safety guard, not a current projection blocker.

---

# 19. Real-provider technical architecture to implement

This section is normative for this remediation plan.

## 19.1 Keep current semantic ownership

```text
IntegrationConnection
  generic provider relationship

IntegrationSecretVersion
  versioned secret reference

CalendarIntegration
  Workspace Calendar binding
```

Do not collapse these.

## 19.2 Add technical provider-subscription state

```text
CalendarProviderSubscription
```

This is Integrations technical/provider state.

Minimum fields:

```text
Id
AccountId
WorkspaceId
CalendarIntegrationId
ConnectionId
Provider

ProviderSubscriptionId
ProviderResourceId
ProviderResourceUri?
VerificationSecretReference / protected token authority
ExpiresAt

Status
LastProviderSequence?
LastFailureCode?
CreatedAt
UpdatedAt
```

Provider-specific optional data should be stored in an owned adapter-state model,
not leaked into other BCs.

## 19.3 Outbound provider lifecycle

```text
ConnectCalendar
→ local transaction
→ outbox ProvisionCalendarProviderSubscriptionRequested
→ provider lifecycle consumer
→ Google watch / Graph subscription
→ CalendarProviderSubscription Active

renewal scheduler
→ RenewCalendarProviderSubscriptionRequested
→ provider adapter
→ new expiry

DisconnectCalendar
→ CalendarIntegration.Deactivate first
→ local CAL-CONN-001 decision
→ outbox RevokeCalendarProviderSubscriptionRequested
→ provider adapter cleanup
→ Revoked / Failed / Unknown
```

## 19.4 Inbound provider flow

```text
HTTP
→ provider-specific protocol adapter
→ resolve active provider subscription
→ authenticate provider-specific delivery
→ normalize one or more technical notifications
→ claim receipt(s)
→ outbox processing event(s)
→ tenant restored from immutable receipt/event envelope
→ semantic processing
```

One Microsoft HTTP batch can create multiple logical receipts.

A Google header-only notification can create one logical receipt with no raw
business payload.

## 19.5 Dedup naming

The current name:

```text
ExternalEventId
```

is misleading for provider delivery.

Use semantic name:

```text
ProviderDeliveryId
```

at least in application/technical contracts.

Physical column rename can be done in the same migration if safe.

Dedup authority becomes:

```text
(ConnectionId, Provider, ProviderDeliveryId)
```

only after each provider adapter defines a contract-valid stable delivery
identity.

If a provider does not guarantee a globally unique per-notification ID, the
adapter must document its at-least-once dedup strategy instead of inventing one.

---

# 20. Receipt redesign required for final freeze

Recommended accepted receipt shape:

```text
InboundWebhookReceipt
  Id
  AccountId
  WorkspaceId
  ConnectionId
  Provider
  ProviderDeliveryId
  PayloadHash?            # optional when provider sends no body
  ProtectedPayload?       # optional
  ReceivedAt
  Status
  TerminalAt?
  FailureCode?
  FailureDetail?
```

Rules:

```text
new verified receipts:
  AccountId non-null
  WorkspaceId non-null
  ConnectionId non-null

unverified/rejected traffic:
  not persisted here
```

If adding AccountId/WorkspaceId enables normal Workspace RLS for accepted
receipts, apply it consistently and prove the anonymous bootstrap still never
queries receipt rows before tenant derivation.

---

# 21. Calendar processing outcome model after semantic target exists

Current:

```text
Completed
SemanticTargetUndefined
RetryableFailure
```

Final model should distinguish at least:

```text
Completed
RetryableFailure
TerminalFailure
UnknownOutcome
```

`SemanticTargetUndefined` is temporary architecture state and must not be the
normal production branch after closure.

Mapping:

```text
Completed
→ Processed

RetryableFailure
→ throw/retry, remain Captured

TerminalFailure
→ Failed

UnknownOutcome
→ explicit reconciliation state or retry protocol defined by target owner
```

Do not map unknown outcome directly to success or terminal failure without target
semantics.

---

# 22. Architecture gates to add/tighten

## EVT-EVOL-001

Every public event version pair requires explicit compatibility/evolution policy.

## EVT-EVOL-002

For the three Work V1→V2 pairs:

```text
compatibility != Backward
synthetic upcast forbidden
```

## EVT-EVOL-003

Changed event endpoint version requires broker-cutover evidence.

## EVT-TENANT-004

Event manifest tenant-scope metadata must use the same detector as tenant-envelope
architecture gates.

## CAL-PROTOCOL-001

Google provider may not use the synthetic `X-Calendar-*` HMAC protocol.

## CAL-PROTOCOL-002

Microsoft provider must support notification URL validation and subscription
identity/clientState semantics.

## CAL-LIFECYCLE-003

An active real-provider webhook path must resolve an active
`CalendarProviderSubscription`.

## CAL-RECEIPT-004

A processing message cannot succeed when its referenced receipt is missing.

## CAL-RECEIPT-005

Receipt/event tenant and connection provenance must match before semantic
processing.

## CAL-RECEIPT-006

Rejected unauthenticated callback cannot write raw payload into durable receipt
storage.

## CAL-STATE-007

Terminal receipt status cannot transition to another status.

## PF-REPLAY-008

Skeleton replay strategies cannot count as PF-FLOW-05 implementation evidence.

---

# 23. Test plan

## 23.1 Work event evolution

```text
EVOL-01..09 from section 5
```

Additionally test the manifest:

```text
board.item.created v1/v2 compatibility classification
board_item.moved v1/v2 compatibility classification
board_item.archived v1/v2 compatibility classification
```

## 23.2 Google protocol

```text
valid header-only delivery
empty body
valid/invalid channel token
unknown channel ID
resource mismatch
duplicate message number
out-of-order message number
sync state
channel expiration
```

## 23.3 Microsoft protocol

```text
validationToken endpoint handshake
basic notification clientState
batch >1 logical notification
wrong clientState
unknown subscription
expired subscription
lifecycle notification
rich validation token/JWT if enabled
```

## 23.4 Provider subscription lifecycle

```text
provision success
provision retryable failure
provision terminal failure
provision unknown
renew success
renew retry
revoke success
revoke retryable
revoke terminal
revoke unknown
retry after unknown with reconciliation
```

## 23.5 Receipt lifecycle

```text
valid claim
concurrent duplicate
same provider delivery on different connection
missing receipt consumer
tampered connection
tampered tenant
tampered provider/delivery identity/hash
terminal state cannot transition
terminal timestamp
retryable rollback
```

## 23.6 Security/storage

```text
10k forged callback simulation
→ zero durable receipt rows

raw forged body
→ absent from logs/DB

valid callback
→ accepted receipt only after provider authentication
```

## 23.7 Analytics

Retain all existing revision/rebuild tests.

Add only:

```text
V1 event cannot enter V2 projection
true source-unavailable consumer behavior if remote source becomes possible
```

---

# 24. Migration plan

## Migration A — Event compatibility metadata

No DB migration.

Update contract/evolution registry and regenerate event manifest.

## Migration B — Calendar provider-subscription state

Create technical Integrations table(s) for provider subscription/channel state.

Required indexes:

```text
unique(provider, provider_subscription_id)
calendar_integration_id
connection_id
expires_at
status
```

Use safe nullable/provider-specific fields where necessary.

## Migration C — Accepted receipt provenance

Add:

```text
account_id
workspace_id
provider_delivery_id
terminal_at
failure_code
```

For legacy rows:

```text
do not invent tenant provenance
leave nullable
exclude legacy rows from new processing
```

New accepted rows require all trusted provenance.

If physical `external_event_id` is renamed, do it in an explicit migration with
index recreation.

## Migration D — rejected diagnostics

Stop inserting new Rejected receipt rows.

Existing historical Rejected rows remain until an explicit bounded cleanup
migration/job removes them according to Operations retention policy.

Do not perform an unbounded delete in the request path.

---

# 25. Deployment / rollout order

Execute exactly in this order.

## Wave R0 — Authority repair before new coding

1. Change CERT `AI-FLOW-07` from VERIFIED to partial/BLOCKED-DECISION.
2. Change AI-FLOW-06 whole-flow certification to reflect missing provider cleanup.
3. Selectively reopen PF-FLOW-05/TAC-FRZ-018 evidence for the Work event bump.
4. Remove statement “V1 retained for compatibility/replay” unless backed by the
   explicit policy in this audit.
5. Keep Application EF refactor out of this workstream.

Exit:

```text
authority no longer claims more than source
```

## Wave R1 — Work event evolution closure

1. Add event-evolution policy model.
2. Set Work V1→V2 compatibility to `None`.
3. Add architecture gates.
4. Add V1 broker backlog cutover check.
5. Document authoritative rebuild recovery.
6. Regenerate manifest.

Exit:

```text
PF-FLOW-05 affected Work schema change closed
```

## Wave R2 — Event manifest tenant metadata fix

1. Share constructor-envelope detector.
2. Regenerate manifest.
3. Assert Calendar processing event carries Account/Workspace metadata.

Exit:

```text
manifest and runtime/gates agree
```

## Wave R3 — Provider subscription lifecycle

1. Add technical provider subscription persistence.
2. Add provider lifecycle ports.
3. Implement Google provisioning/renew/revoke adapter.
4. Implement Microsoft provisioning/renew/revoke adapter.
5. Add outbox-driven provisioning/revoke consumers.
6. Add renewal background job.
7. Add outcome/reconciliation state.

Exit:

```text
AI-FLOW-05/06 real provider lifecycle exists
```

## Wave R4 — Real provider inbound protocols

1. Replace universal HMAC verifier with adapter registry.
2. Implement Google header-only protocol.
3. Implement Microsoft validation + notification protocol.
4. Normalize provider notifications.
5. Derive provider delivery identity per adapter.
6. Route only Active provider subscriptions to intake.

Exit:

```text
real Google/Microsoft callbacks pass provider fixtures
```

## Wave R5 — Receipt reliability hardening

1. Add immutable AccountId/WorkspaceId provenance.
2. Replace ExternalEventId semantic name with ProviderDeliveryId.
3. Stop durable raw persistence of rejected callbacks.
4. Fix missing-receipt consumer failure.
5. Bind event envelope to receipt provenance.
6. Introduce status enum + terminal state machine/timestamp.
7. Add tamper/storage tests.

Exit:

```text
receipt is trustworthy durable processing authority
```

## Wave R6 — AI-FLOW-07 semantic decision

Freeze the exact downstream semantic decision record from section 8.

Do not code target behavior before the decision is explicit.

Exit:

```text
coding agent requires zero semantic guesses
```

## Wave R7 — AI-FLOW-07 semantic implementation

1. Translate verified technical notification into provider-neutral semantic.
2. Execute approved TAC-XC target interaction.
3. Add retry/terminal/unknown handling.
4. Mark receipt Processed only after semantic success.
5. Prove duplicate/retry/recovery/cross-tenant behavior.

Exit:

```text
AI-FLOW-07 Real Flow complete
```

## Wave R8 — Automation / affected runtime proof

1. Normalize `AI-FLOW-03:E3 ProcessState = AutomationExecution`.
2. Rerun Work V2 → Automation proof.
3. Rerun PF-FLOW-07 representative tenant delivery.
4. Rerun Analytics revision/rebuild suite.

Exit:

```text
changed chains green
```

## Wave R9 — Platform cleanup

1. Delete/deprecate orphan `RabbitMqTransportAdapter` surface.
2. Remove skeleton replay classes from closure evidence.
3. If retained as supported API, implement them against a real retained source;
   otherwise mark deferred/non-canonical.

Exit:

```text
source topology does not advertise fake runtime mechanisms
```

## Wave R10 — M14 exact-SHA

1. Regenerate frontend OpenAPI contracts.
2. Resolve PostgreSQL runtime image vulnerability.
3. Commit final source.
4. Obtain exact SHA.
5. Run full CI.
6. Record workflow/run/job evidence.
7. Update CERT and STATUS.
8. Evaluate closure only after all mandatory rows are green.

---

# 26. Exact-SHA CI status at this audit

Exact head:

```text
b9cccc07bf4b3bf2be04d7336b478eba6170f136
```

GitHub Actions:

```text
Notrelix CI
run 35558709255
completed
failure

CodeQL
run 35558709079
completed
success
```

Backend architecture/core/API/integration paths were green in the audited run.

Repository-level blockers remain:

```text
frontend generated REST/OpenAPI contract drift
PostgreSQL runtime-image Trivy HIGH:
  CVE-2026-89157
  libpcre2-8-0
```

These are M14 delivery blockers, not reasons to redesign the BC mechanisms.

---

# 27. What must not be done

```text
DO NOT reintroduce OccurredAt ordering.

DO NOT synthesize Work Revision from timestamp.

DO NOT claim V1→V2 Backward compatibility without a valid semantic conversion.

DO NOT implement a second RabbitMQ stack around orphan ITransportAdapter.

DO NOT wire TriggerCalendarSync merely because it is NotImplemented.

DO NOT use one synthetic HMAC protocol and label it Google/Microsoft.

DO NOT persist every forged webhook raw body indefinitely.

DO NOT acknowledge a processing event whose durable receipt is missing.

DO NOT allow Failed/Blocked receipt to later become Processed.

DO NOT mark receipt Processed when only transport verification succeeded.

DO NOT weaken TESTS/CERT to hide missing provider cleanup.

DO NOT invent AI-FLOW-07 downstream business semantics.

DO NOT reopen unrelated BCs wholesale.

DO NOT include the deferred Application EF/DbSet refactor in this execution.
```

---

# 28. Final blocker hierarchy

Execute/close in this priority:

```text
P0-1  Work public-event evolution / PF-FLOW-05
P0-2  Calendar real-provider subscription lifecycle
P0-3  Calendar real Google/Microsoft webhook protocols
P0-4  AI-FLOW-06 provider cleanup runtime
P0-5  AI-FLOW-07 downstream semantic decision/effect

P1-1  receipt immutable provenance
P1-2  missing receipt fail-closed
P1-3  rejected callback bounded telemetry
P1-4  receipt terminal state machine
P1-5  event-manifest tenant metadata consistency
P1-6  certification/status authority reconciliation

P2-1  Platform orphan/skeleton cleanup
P2-2  source-not-found vs source-unavailable naming/gates

FINAL   exact-SHA M14
```

Note: provider lifecycle and provider webhook protocol are implemented before the
business-semantic target. That allows the transport/reliability mechanism to
become truthful without inventing product behavior.

---

# 29. Final Definition of Done

The candidate may be evaluated for `ARCHITECTURE-CLOSED` only when all are true:

```text
WORK / ANALYTICS
[ ] producer revision path retained
[ ] unsafe V1→V2 compatibility claim removed
[ ] explicit cutover policy exists
[ ] V1 broker backlog gate passes
[ ] authoritative rebuild recovery proven

PLATFORM
[ ] PF-FLOW-05 affected schema evolution is certified
[ ] changed-contract replay/recovery policy proven
[ ] PF-FLOW-07 changed tenant runtime proof green
[ ] skeleton replay is not counted as implementation

CALENDAR PROVIDER
[ ] provider subscriptions/channels are actually provisioned
[ ] renewal exists
[ ] revoke/cleanup exists
[ ] Google protocol proof green
[ ] Microsoft protocol proof green
[ ] provider unknown outcomes reconciled

CALENDAR RECEIPT
[ ] immutable tenant/connection provenance persisted
[ ] missing receipt faults
[ ] message/receipt mismatch faults
[ ] rejected forged payload is not durable-amplified
[ ] terminal receipt state machine enforced
[ ] terminal timestamp/reason recorded

AI-FLOW-07
[ ] exact downstream semantic frozen
[ ] target BC and TAC-XC mechanism frozen
[ ] target-owned effect implemented
[ ] Processed == semantic success
[ ] duplicate/retry/recovery/cross-tenant proof green

AUTHORITY
[ ] SPEC consistent
[ ] PLAN consistent
[ ] TESTS consistent
[ ] CERTIFICATION consistent
[ ] STATUS consistent
[ ] no VERIFIED row contradicts source

DELIVERY
[ ] frontend generated contracts synced
[ ] runtime image vulnerability gate green
[ ] final exact SHA fixed
[ ] full CI green
[ ] run IDs recorded
```

---

# 30. Final conclusion

The PR #158 revision work is directionally strong and fixes the original
producer-revision and RLS/bootstrap defects, but the system is **not yet fully
mechanism-closed**.

The most important correction from the first audit is:

```text
This is no longer only:
  V1/V2 compatibility
  + Calendar provenance
  + downstream semantic target.

It is also:
  real provider protocol correctness
  + provider subscription lifecycle
  + AI-FLOW-06 provider cleanup
  + PF-FLOW-05 selective reopen
  + receipt failure-state correctness
  + authority/certification reconciliation.
```

The correct execution path is:

```text
repair authority
→ close Work event evolution
→ fix manifest tenant metadata
→ implement real provider subscription lifecycle
→ implement real Google/Microsoft webhook adapters
→ harden receipt/provenance/state
→ freeze AI-FLOW-07 product semantic
→ implement target-owned semantic effect
→ rerun affected cross-pack proofs
→ remove deceptive Platform skeleton evidence
→ exact-SHA M14
```

Do not declare `ARCHITECTURE-CLOSED` before that chain is complete.

---

# Appendix A — Key source evidence

```text
Work revision:
backend/src/Notrelix.Domain/WorkManagement/Items/BoardItem.cs
backend/src/Notrelix.Application/EventMappers/WorkManagement/BoardEventMapper.cs
backend/src/Notrelix.Application/Events/WorkManagement/BoardItemCreatedIntegrationEventV2.cs
backend/src/Notrelix.Application/Events/WorkManagement/BoardItemMovedIntegrationEventV2.cs
backend/src/Notrelix.Application/Events/WorkManagement/BoardItemArchivedIntegrationEventV2.cs
backend/src/Notrelix.Application/Features/Analytics/Placements/Services/WorkspaceWorkItemPlacementService.cs

Event manifest/evolution:
backend/contracts/events/notrelix.events.json
backend/src/Notrelix.Application/Common/Events/IContractRegistry.cs
backend/src/Notrelix.Platform/Messaging/Contracts/Evolution/IUpcaster.cs
backend/src/Notrelix.Platform/Messaging/Contracts/Evolution/UpcasterRegistry.cs
backend/tests/Notrelix.Architecture.Tests/Events/Support/EventManifestGenerator.cs
backend/tests/Notrelix.Architecture.Tests/Events/ScopedEventTenantEnvelopeArchitectureTests.cs

Calendar:
backend/src/Notrelix.API/Endpoints/Integrations/Calendar/CalendarEndpoints.cs
backend/src/Notrelix.API/Middleware/WebhookBoundedBodyReader.cs
backend/src/Notrelix.Application/Features/Integrations/Calendar/Commands/ConnectCalendar/ConnectCalendar.cs
backend/src/Notrelix.Application/Features/Integrations/Calendar/Commands/DisconnectCalendar/DisconnectCalendar.cs
backend/src/Notrelix.Application/Features/Integrations/Calendar/Commands/HandleCalendarWebhook/HandleCalendarWebhook.cs
backend/src/Notrelix.Application/Features/Integrations/Calendar/Processing/CalendarWebhookProcessingUseCase.cs
backend/src/Notrelix.Infrastructure/Integrations/Webhooks/CalendarWebhookVerifier.cs
backend/src/Notrelix.Infrastructure/Integrations/Webhooks/CalendarWebhookBindingResolver.cs
backend/src/Notrelix.Infrastructure/Integrations/Webhooks/CalendarWebhookIntake.cs
backend/src/Notrelix.Infrastructure/Data/Integrations/InboundWebhookReceipt.cs
backend/src/Notrelix.Infrastructure/Messaging/Consumers/Integrations/CalendarWebhookProcessingRequestedConsumer.cs

Platform:
backend/src/Notrelix.Infrastructure/DependencyInjection/MessagingRegistration.cs
backend/src/Notrelix.Platform/Messaging/Operations/ReplayEngine.cs
backend/src/Notrelix.Platform/Messaging/Operations/CheckpointReplayStrategy.cs
backend/src/Notrelix.Platform/Messaging/Operations/LatestReplayStrategy.cs
backend/src/Notrelix.Platform/Messaging/Operations/SnapshotReplayStrategy.cs
backend/src/Notrelix.Platform/Messaging/Operations/TimeWindowReplayStrategy.cs

Authority:
docs/workstreams/executions/backend-team-architecture-closure/backend-team-architecture-closure.spec.md
docs/workstreams/executions/backend-team-architecture-closure/backend-team-architecture-closure.plan.md
docs/workstreams/executions/backend-team-architecture-closure/backend-team-architecture-closure.tests.md
docs/workstreams/executions/backend-team-architecture-closure/backend-team-architecture-closure.certification.md
docs/workstreams/executions/backend-team-architecture-closure/tac-v26.execution-status.md
```

# Appendix B — External protocol sources checked

Official provider documentation was used only to verify whether the current
transport contract matches the actual providers named by production source.

```text
Google for Developers
  Google Calendar API — Push notifications

Microsoft Learn
  Microsoft Graph — Receive change notifications through webhooks
  Microsoft Graph — Change notifications for Outlook resources
  Microsoft Graph — Change notifications with resource data
```

Provider-specific implementation must be validated against the current official
documentation again at coding time because provider protocols can evolve.

# Execution record — 2026-09-21

This record reports the implementation performed from this re-audit pack. It
does not override the normative requirements above and does not certify
`ARCHITECTURE-CLOSED`.

## Completed evidence

```text
R0 authority repair
  ProcessState is normalized to AutomationExecution in the current authority
  record; historical M8/M11/M12 rows remain historical and are superseded by
  the appended correction in tac-v26.execution-status.md.

R1 Work V1 -> V2 evolution
  V1/V2 Work contracts are explicitly incompatible; the policy requires drain
  or quarantine before V2-only authority and rebuild from the authoritative
  Work producer rather than synthetic revision upcast.

R2 tenant manifest detector
  The detector recognizes both accountId/workspaceId and the generated
  accountIdValue/workspaceIdValue forms; the canonical event manifest was
  regenerated and the affected Work envelope metadata is corrected.

R5 receipt/provenance/state
  Accepted receipts require trusted account/workspace/connection provenance.
  Rejected callbacks emit bounded telemetry without durable raw-body receipt.
  Processing uses persisted receipt authority, rejects provenance mismatch,
  and cannot resurrect a terminal Failed/Blocked receipt.

R9 deceptive transport surface
  The unregistered RabbitMqTransportAdapter placeholder was removed and the
  Platform architecture document no longer lists it as an active mechanism.

R10 contract/runtime delivery blockers
  Frontend REST contracts were regenerated from the backend OpenAPI producer.
  The fallback Postgres digest and its scoped Trivy binding were moved from
  the vulnerable digest to the current official postgres:16.15-bookworm
  manifest digest sha256:efedf3595f1d6f415c08568ba171029bf54052e754cc9f030e3f2412b21f3d67.
```

## Verification

```text
dotnet test backend/backend.slnx --no-restore
  5107 passed, 17 existing warnings, 0 test failures.

dotnet ef migrations list
  contains exactly 20260702093805_SchemaV2Baseline.

dotnet ef migrations has-pending-model-changes
  no pending model changes.

frontend pnpm codegen
  generated schema.ts from backend/contracts/openapi/notrelix.v1.json.
  codegen:check reports only the intentional working-tree generated diff;
  after this diff is committed it is the expected clean-tree gate.
```

## Remaining blockers

```text
R3/R4 provider subscription lifecycle and real Google/Microsoft protocol
  adapters remain blocked by provider credentials, callback lifecycle
  contract, and the required provider decision. The current synthetic HMAC
  path is not promoted as a real provider adapter.

R6/R7 AI-FLOW-07 semantic target and target-owned effect remain
  BLOCKED-DECISION. No TriggerCalendarSyncCommand wiring or fabricated
  product behavior was introduced.

PF-FLOW-05 generic replay strategies remain non-operational skeletons and are
  not counted as closure evidence. A real replay/rebuild authority contract
  is still required for any event family that needs recovery.
```

Current result: implementation/evidence is materially advanced for the
authority, Work evolution, tenant manifest, receipt, transport, and delivery
slices, but the pack remains `BLOCKED-DECISION + BLOCKED-EVIDENCE` and is not
`ARCHITECTURE-CLOSED`.

# Notrelix TAC v2.6 — Full Re-Audit Report

**Repository:** `Nqv1208/Notrelix`  
**Pull Request:** `#145`  
**Branch:** `develop`  
**Implementation SHA:** `54bcae56f2deeae3febdf2271505667b4d8aa9ea`  
**Base:** `main`  
**Base SHA:** `f6f6d5b571a315cc9cebe44ca621bf293c1b8cbf`  
**Audit date:** 2026-09-18

---

# 1. Final certification outcome

```text
BLOCKED-IMPLEMENTATION
```

The candidate must **not** be certified as `ARCHITECTURE-CLOSED`.

The architecture is mostly decided and a large portion of the six TAC cross-BC interaction mechanisms is present in production source, but at least two mandatory reference areas still violate frozen authority:

1. `AR-FLOW-01:E2 / TAC-XC-D` — Analytics projection semantic ordering authority is incorrect.
2. `AI-FLOW-07` — verified inbound Calendar webhook flow is incomplete in several production-level guarantees.

In addition:

- `AI-FLOW-03:E3 / TAC-XC-F` has a SPEC authority inconsistency around `ProcessState`.
- exact-head CI does not provide complete integration/runtime-owner evidence because the OpenAPI drift gate fails before the integration/provider suite runs.
- a runtime image security gate also fails on the PostgreSQL scan.

Therefore the current outcome is still **implementation-blocked**, not merely evidence-blocked.

---

# 2. Authority model used for this audit

The audit follows the five-file authority model:

1. **SPEC** — semantic authority, Flow Cards, mechanism classification, frozen invariants.
2. **PLAN** — implementation order, pack-entry decisions, milestone execution rules.
3. **TESTS** — mandatory behavior/runtime/negative proof.
4. **CERTIFICATION** — closure rules and final outcome semantics.
5. **STATUS** — evidence/state only; it cannot override SPEC/PLAN/TESTS/CERT.

Important audit rule:

```text
STATUS is evidence.
STATUS is not normative authority.
```

Exact-SHA certification is mandatory.

---

# 3. TAC v2.6 mechanism model

There are exactly six canonical cross-bounded-context interaction mechanisms:

```text
TAC-XC-A  Producer Public Direct
TAC-XC-B  Consumer Port + ACL + Adapter
TAC-XC-C  Integration Event
TAC-XC-D  Local Projection
TAC-XC-E  Target-owned Command/Action
TAC-XC-F  Process Manager
```

Supporting roles are not mechanisms:

```text
Local Vertical Slice
Composite Read
ResourceRef
BFF/read composition
Provider Port/Adapter
Platform outbox/dedup/retry
RLS bootstrap
CI
migration
concurrency guards
```

A Real Flow may contain multiple edges and multiple mechanisms.  
Evidence must be evaluated at:

```text
FlowId:EdgeId
```

not by assigning one mechanism to an entire Flow Card.

---

# 4. Re-audited mechanism matrix

| Mechanism | Source implementation | Semantic ownership | Exact-head runtime evidence | Result |
|---|---|---|---|---|
| TAC-XC-A | Present | Correct | Full exact-head integration evidence incomplete | PASS source |
| TAC-XC-B | Present | Correct consumer/producer ownership | Integration suite skipped | PASS source |
| TAC-XC-C | Present | Correct producer fact/outbox/consumer shape | Platform proof passed; full integration skipped | PASS source / evidence pending |
| TAC-XC-D | Present but wrong ordering authority | FAIL | Runtime evidence insufficient | BLOCKER |
| TAC-XC-E | Present | Correct target ownership | Integration suite skipped | PASS source / evidence pending |
| TAC-XC-F | Durable process implementation exists | Implementation broadly correct; Flow Card inconsistent | Full exact-head integration evidence incomplete | PASS implementation / authority correction |

No seventh TAC-XC mechanism is required.

---

# 5. BLOCKER — AR-FLOW-01:E2 / TAC-XC-D

## 5.1 Frozen requirement

The Flow Card freezes the projection ordering rule as:

```text
OrderingRequirement:
Work source revision/version guard;
stale event cannot regress projection
```

The semantic authority is therefore a monotonic Work source revision/version.

---

## 5.2 Current implementation

Current projection:

```text
backend/src/Notrelix.Application/
Features/Analytics/Projections/WorkItemPlacement/
WorkspaceWorkItemPlacementProjection.cs
```

uses:

```csharp
public static long WatermarkOf(DateTimeOffset lastOccurredAt)
    => lastOccurredAt.UtcTicks;
```

and stores:

```csharp
SourceRevision = WatermarkOf(lastOccurredAt);
```

The projection therefore treats:

```text
OccurredAt.UtcTicks
```

as its semantic source revision.

---

## 5.3 Producer already exposes the correct revision

Producer contract:

```text
backend/src/Notrelix.Application/
Features/WorkManagement/Public/ItemPlacement/
IWorkItemProjectionSource.cs
```

defines:

```csharp
WorkItemPlacementSnapshot(
    Guid AccountId,
    Guid ItemId,
    Guid BoardId,
    Guid GroupId,
    bool IsArchived,
    long Revision,
    DateTimeOffset LastOccurredAt);
```

Producer implementation:

```text
WorkItemProjectionSourceService
```

maps:

```csharp
Revision = item.Version
```

Therefore the producer already has a monotonic semantic ordering authority:

```text
BoardItem.Version
```

but Analytics ignores it.

---

## 5.4 Concrete failure

Example:

```text
mutation A
Version = 10
OccurredAt = T

mutation B
Version = 11
OccurredAt = T
```

After A:

```text
SourceRevision = T.UtcTicks
```

When B arrives:

```text
watermark == SourceRevision
```

so:

```text
ApplyNewer() = false
```

The newer semantic state is classified as stale/duplicate.

Timestamp ordering and aggregate mutation ordering are not equivalent guarantees.

---

## 5.5 `xmin` does not solve the semantic ordering problem

EF config uses:

```csharp
builder.Property<uint>("xmin")
    .IsRowVersion();
```

This protects physical concurrent writes.

It does **not** answer:

```text
Work event revision 11
vs
Work event revision 12
```

Therefore responsibilities must be separated:

```text
EventId / MessageId
→ delivery identity / dedup

BoardItem.Version / SourceRevision
→ semantic ordering

OccurredAt
→ temporal metadata

PostgreSQL xmin
→ physical concurrent-write protection
```

---

## 5.6 Required correction

Integration event families that drive the projection must expose producer monotonic revision.

Projection update rule:

```text
incoming.SourceRevision > current.SourceRevision
→ apply

incoming.SourceRevision == current.SourceRevision
→ duplicate/equal-source handling

incoming.SourceRevision < current.SourceRevision
→ stale → reject
```

Rebuild must use:

```text
snapshot.Revision
```

rather than:

```text
snapshot.LastOccurredAt.UtcTicks
```

Live event path and rebuild path must use the same revision scale.

---

## 5.7 Existing persisted rows require migration/rebuild handling

Current `SourceRevision` values are timestamp ticks.

After changing to aggregate version:

```text
existing SourceRevision ~= 638xxxxxxxxxxxxxxx
new aggregate Revision ~= 10, 11, 12...
```

Without migration:

```text
12 < 638xxxxxxxxxxxxxxx
```

and all new events may be incorrectly treated as stale.

Required strategy:

```text
invalidate + producer-backed rebuild
```

or an equivalent authoritative backfill.

Do not simply change the comparison code.

---

## 5.8 Required tests

At minimum:

```text
same timestamp:
rev 10
→ rev 11
→ rev 11 wins

out-of-order:
rev 11
→ rev 10
→ rev 10 rejected

duplicate:
rev 11
→ rev 11
→ idempotent

rebuild snapshot revision
and live event revision
use identical source scale

migration/rebuild from old timestamp-based projection state
converges to producer source truth
```

### Milestone impact

```text
TAC-M10 REOPEN
```

---

# 6. BLOCKER — AI-FLOW-07 verified inbound Calendar webhook

`AI-FLOW-07` is a **Local Flow**, not a TAC-XC A–F edge.

Its Flow Card uses:

```text
Interactions: []
```

However it is still a mandatory reference flow and must satisfy the frozen canonical intake chain.

Canonical shape:

```text
API/Infrastructure raw HTTP request
→ size/content/type bounds
→ signature/secret verification
→ timestamp/replay verification
→ resolve trusted IntegrationConnection
→ resolve active CalendarIntegration
→ derive trusted AccountId/WorkspaceId
→ Infrastructure inbound provider-delivery receipt/dedup
→ enqueue/dispatch provider-neutral semantic input
→ Integrations Application translation
→ approved target-context action/event
```

Current implementation does not complete this chain.

---

# 7. AI-FLOW-07 issue 1 — raw HTTP bounds are not enforced at the raw boundary

Current endpoint reads:

```csharp
using var reader = new StreamReader(request.Body);
var rawBody = await reader.ReadToEndAsync(cancellationToken);
```

Only after the body is fully materialized does Application validation apply:

```csharp
RuleFor(x => x.RawBody)
    .NotEmpty()
    .MaximumLength(65536);
```

This is not an HTTP body-size boundary.

The entire payload has already been read into memory.

The current endpoint also does not show a strict pre-read content-type allowlist.

## Required correction

At API/Infrastructure boundary:

```text
Content-Length / bounded stream
Content-Type allowlist
maximum raw bytes
→ only then read body
→ only then dispatch command
```

Application FluentValidation may remain for semantic contract validation, but it must not be the DoS/body-size protection.

---

# 8. AI-FLOW-07 issue 2 — trusted IntegrationConnection is not resolved

Frozen source authority requires:

```text
provider signature
+
verified IntegrationConnection
+
CalendarIntegration binding
```

Current resolver:

```text
CalendarWebhookBindingResolver
```

only queries:

```csharp
_context.CalendarIntegrations
    .IgnoreQueryFilters()
```

and returns:

```csharp
CalendarWebhookBindingSnapshot(
    AccountId,
    WorkspaceId,
    Provider)
```

It does not carry or validate:

```text
CalendarIntegrationId
ConnectionId
IntegrationConnection lifecycle
IntegrationConnection provider
IntegrationConnection tenant
revoked/deleted connection state
```

The Domain already models:

```csharp
CalendarIntegration.ConnectionId
```

but the intake bootstrap ignores that relationship.

## Failure examples

```text
CalendarIntegration active
IntegrationConnection revoked
→ current flow may still accept
```

or:

```text
CalendarIntegration.Provider = Google
IntegrationConnection.Provider = Microsoft
→ current intake does not validate the mismatch
```

## Required correction

Bootstrap result must include trusted connection identity and verify both sides of the binding.

Example semantic snapshot:

```text
CalendarIntegrationId
ConnectionId
AccountId
WorkspaceId
Provider
CalendarIntegrationActive
ConnectionActive
ConnectionDeleted/Revoked state
```

The exact shape may differ, but Connection authority cannot be omitted.

---

# 9. AI-FLOW-07 issue 3 — inbound receipt identity omits ConnectionId

Current intake dedup authority is:

```text
(provider, external_event_id)
```

Current implementation explicitly uses this pair for its unique claim.

Frozen AI-FLOW-07 semantics require technical receipt identity to contain at least:

```text
ConnectionId
provider
provider delivery/event identity
received-at
verification/processing status
payload hash/reference
```

Using only:

```text
(provider, external event id)
```

can collide when the provider only guarantees delivery/event identity within an account/connection namespace.

## Required correction

Dedup identity should be connection-aware, for example:

```text
(ConnectionId, Provider, ExternalEventId)
```

subject to the exact provider contract.

Do not only change the DB index.

The trusted `ConnectionId` must propagate through:

```text
bootstrap authority
→ verified intake
→ receipt claim
```

---

# 10. AI-FLOW-07 issue 4 — PostgreSQL FORCE RLS bootstrap is unresolved

The command is:

```csharp
IWriteRequest
IAnonymousRequest
IGlobalRequest
```

`DataSessionBehavior` only applies tenant RLS session state for:

```text
Account
Workspace
Resource
```

Global scope therefore has:

```text
ApplyTenantScope = false
```

But:

```text
integration.calendar_integrations
```

uses PostgreSQL RLS with:

```text
ENABLE ROW LEVEL SECURITY
FORCE ROW LEVEL SECURITY
```

App SELECT ultimately uses:

```sql
ops.has_workspace_access(account_id, workspace_id)
```

and `ops.has_workspace_access()` returns false when a normal app scope has no current user/access context.

The bootstrap therefore has a circular dependency:

```text
need CalendarIntegration
→ to discover AccountId/WorkspaceId

but need tenant/access authority
→ to read CalendarIntegration
```

Current code calls:

```csharp
.IgnoreQueryFilters()
```

but this only bypasses EF query filters.

It does not bypass PostgreSQL `FORCE ROW LEVEL SECURITY`.

---

## Current tests do not prove the production bootstrap path

Calendar webhook integration tests seed and execute using:

```text
SystemTenant()
```

which calls:

```text
tenant.SetSystem()
```

This is worker/system behavior, not the anonymous production application-role bootstrap.

Therefore the current tests do not prove:

```text
anonymous HTTP
→ app DB role
→ FORCE RLS
→ trusted binding bootstrap
```

---

## Required correction

Do not disable RLS.

Do not run anonymous public HTTP as unrestricted worker scope.

Use a narrow bootstrap authority.

One valid direction:

```text
restricted SECURITY DEFINER function
```

such as a function conceptually equivalent to:

```text
ops.resolve_calendar_webhook_binding(webhook_path)
```

with:

```text
fixed search_path
minimal returned fields
EXECUTE grant only
no arbitrary table/query capability
```

It should return only trusted bootstrap facts.

After verification:

```text
AdoptDerivedTenant
→ establish normal tenant RLS context
→ continue tenant-scoped receipt/semantic processing
```

---

# 11. AI-FLOW-07 issue 5 — the implementation ends at receipt instead of semantic dispatch

Current `CalendarWebhookIntake.AcceptAsync()`:

1. claims the receipt,
2. immediately marks it:

```text
Processed
```

Production comments describe the flow as “intake-only”.

That contradicts the frozen SPEC.

The required flow continues beyond technical receipt:

```text
receipt/dedup
→ provider-neutral semantic input
→ Integrations Application translation
→ approved target-context action/event
```

Current implementation stops at:

```text
verified callback
→ technical receipt
→ Processed
```

No provider-neutral semantic dispatch is executed.

No downstream semantic translation is executed.

No approved target action/event is reached.

This is an implementation gap, not merely missing evidence.

---

## Processing-state semantic problem

The system currently claims:

```text
Processed
```

when only the transport/intake receipt has been accepted.

A correct model must distinguish transport receipt from business/semantic processing.

Possible semantic progression:

```text
Received / Claimed
→ Verified
→ DispatchPending
→ Processed
```

and failure states such as:

```text
RetryPending
ReconciliationRequired
Failed
```

The exact names are product/model decisions, but the receipt cannot be declared semantically processed before the required downstream effect has occurred.

---

# 12. AI-FLOW-07 combined result

The verified Calendar webhook flow currently has at least five independent production gaps:

```text
1. raw HTTP size/content/type boundary
2. trusted IntegrationConnection resolution/validation
3. ConnectionId-aware receipt identity
4. FORCE-RLS-safe cross-tenant bootstrap
5. provider-neutral semantic dispatch / processing state
```

Therefore:

```text
TAC-M8 REOPEN
```

is mandatory.

---

# 13. AI-FLOW-03:E3 / TAC-XC-F — Process Manager

The frozen edge correctly says:

```text
Mechanism: TAC-XC-F
ProcessOwner: Automation
ConsistencyModel: process-coordinated
CurrentBinding: process-runtime
FutureDistributedBinding: durable-process
```

But the same Flow Card later says:

```text
ProcessState:
NotApplicable — no process state required
```

This is internally inconsistent.

---

## Actual production source has explicit durable process state

`AutomationExecution` carries:

```text
Queued
Running
Succeeded
Failed
Cancelled
AttemptCount
Steps
```

`AutomationMoveItemUseCase` performs:

```text
load AutomationExecution
→ Start
→ call IWorkActionPort
→ classify Work target outcome
→ Succeed / Fail / retryable progression
```

This is real Process Manager behavior.

## Required correction

Normalize the Flow Card to:

```text
ProcessState: AutomationExecution
```

Do not create a second Saga/Process aggregate.

The runtime implementation is broadly aligned; this is primarily an authority/certification consistency correction.

---

# 14. TAC-XC-B recheck — WorkManagement → Collaboration

Current shape:

```text
WorkManagement
IWorkManagementCollaborationReadPort
```

↓

```text
Infrastructure
WorkManagementCollaborationReadAdapter
```

↓

```text
Collaboration
ICollaborationResourceSummary
```

The adapter does not read Collaboration persistence directly.

Ownership is correct:

```text
Port
→ consumer-owned

Public semantic contract
→ producer-owned

adapter
→ runtime-owned
```

Result:

```text
TAC-XC-B PASS source
```

---

# 15. TAC-XC-B/E/F recheck — Automation → WorkManagement

Current chain:

```text
AutomationMoveItemUseCase
→ IWorkActionPort
→ WorkItemActionAdapter
→ WorkManagement.Public IWorkItemActions
→ Work target mutation
```

`IWorkActionPort` carries Automation vocabulary.

`WorkItemActionAdapter` maps through the ACL.

`IWorkItemActions` carries WorkManagement-owned vocabulary and target authority:

```text
OperationId
AccountId
WorkspaceId
ExecutorUserId
```

WorkManagement owns:

```text
scope
authorization
idempotency
mutation
business rejection
```

Automation owns:

```text
workflow/process progression
```

This correctly demonstrates:

```text
TAC-XC-B translation/runtime seam
+
TAC-XC-E target mutation
+
TAC-XC-F surrounding process
```

Result:

```text
PASS source
```

---

# 16. TAC-XC-E recheck — Workspaces → Accounts

`IAccountMembershipActions` still clearly expresses:

```text
Accounts owns mutation
Workspaces owns workflow
BOUND-TX-002 shared current request transaction
semantic boundary ready
runtime substitution blocked by transaction exception
explicit extraction removal trigger
```

No source-level reason was found to reopen this mechanism implementation.

Result:

```text
PASS source
```

---

# 17. Exact-head CI and evidence status

Exact candidate:

```text
54bcae56f2deeae3febdf2271505667b4d8aa9ea
```

Current CI results include:

```text
Architecture tests       PASS
Domain tests             PASS
Application tests        PASS
Infrastructure tests     PASS

Platform messaging tests PASS
API tests                PASS
```

However:

```text
OpenAPI drift/export gate
→ FAIL

Integration/provider tests
→ SKIPPED

Integration proof profile
→ SKIPPED
```

A runtime image security job also fails on the PostgreSQL image scan.

Therefore the exact candidate does not have complete runtime-owner certification evidence.

---

# 18. Certification state transition

Current state:

```text
BLOCKED-IMPLEMENTATION
```

because implementation defects still exist.

After implementation is corrected, if the exact-head runtime/integration proof is still missing:

```text
BLOCKED-EVIDENCE
```

Only after implementation and exact-SHA evidence both pass can the candidate be considered for:

```text
ARCHITECTURE-CLOSED
```

---

# 19. Remaining NotImplementedException seams

Current known production seams:

```text
RabbitMqTransportAdapter
TriggerCalendarSync
MovePage
SetPageDeadline
PublishPage
```

These must not automatically be classified as TAC mechanism failures.

PLAN permits intentional unfinished **non-mandatory** product features to remain when they are clearly outside the mandatory Real Flow set.

CERT prohibits unfinished code from being used as mandatory Real Flow evidence.

Important example:

```text
PublishPage
```

must not be fabricated merely to remove a `NotImplementedException` because the current Page Domain model does not expose a Publish lifecycle transition.

Therefore:

```text
do not blindly implement all five seams
```

and:

```text
do not create new milestones or mechanisms solely because these seams exist
```

Each seam must first be mapped to an actual mandatory Flow Card/reference requirement.

---

# 20. Correction plan

## TAC-M8 — REOPEN

### AI-FLOW-07

Implement/fix:

```text
raw HTTP size/content/type boundary
trusted IntegrationConnection bootstrap
CalendarIntegration ↔ Connection validation
ConnectionId propagation
Connection-aware inbound receipt identity
FORCE-RLS-safe bootstrap authority
derived tenant adoption
provider-neutral semantic dispatch
accurate receipt processing state
real production-role integration tests
```

### AI-FLOW-03:E3

Normalize:

```text
ProcessState = AutomationExecution
```

No process redesign required.

---

## TAC-M10 — REOPEN

Fix `AR-FLOW-01:E2`:

```text
producer SourceRevision
→ integration event revision
→ projection revision guard
→ rebuild snapshot revision
```

Keep:

```text
OccurredAt
→ metadata

xmin
→ physical DB concurrency
```

Handle existing timestamp-based projection rows via rebuild/backfill.

Add same-timestamp and out-of-order revision tests.

---

## TAC-M12 — REOPEN

Re-run only affected production chains:

```text
Calendar raw HTTP
→ bootstrap
→ verification
→ tenant adoption
→ receipt
→ semantic dispatch

Work producer mutation
→ outbox/event
→ Analytics consumer
→ revision-guarded projection

Automation process
→ durable execution
→ Work target action
→ target outcome
→ process progression
```

Do not reimplement unrelated packs.

---

## TAC-M14 — REOPEN

Required final work:

```text
sync OpenAPI artifact
run full backend CI
run Integration.Tests
run provider tests
run affected runtime-owner proofs
capture exact SHA
record exact CI runs
re-evaluate final CERT outcome
```

---

# 21. Milestone scope recommendation

Do **not** reopen all milestones.

Recommended scope:

```text
M8   REOPEN
M10  REOPEN
M12  REOPEN
M14  REOPEN
```

No current source evidence requires reopening the complete:

```text
M4
M5
M6
M7
M9
M11
```

packs.

---

# 22. Final audit state

```text
TAC-XC-A
PASS source

TAC-XC-B
PASS source

TAC-XC-C
PASS source
exact-head runtime evidence pending

TAC-XC-D
FAIL implementation
AR-FLOW-01:E2 ordering authority

TAC-XC-E
PASS source
exact-head runtime evidence pending

TAC-XC-F
PASS implementation
SPEC ProcessState correction required

AI-FLOW-07
FAIL implementation
multiple independent mandatory gaps

M12
affected runtime proof must be rerun

M14
exact-SHA certification incomplete
```

Final certification outcome:

```text
BLOCKED-IMPLEMENTATION
```

`ARCHITECTURE-CLOSED` is not currently justified.

---

# 23. Continuation audit — completeness correction

This section was added after a second completeness pass over:

```text
SPEC
PLAN
TESTS
CERTIFICATION
STATUS
current PR #145 exact-head production source
current exact-head CI
```

It **supersedes any weaker or less-specific statement earlier in this report**.

The first re-audit correctly identified the primary implementation blockers, but the report was still incomplete in four areas:

1. it did not enumerate all 47 Flow IDs;
2. it did not distinguish stale authority/evidence claims from source defects for M8/M10;
3. it did not model the RLS session problem after derived tenant adoption;
4. it did not explicitly invalidate dependent AR-FLOW-04 evidence after the TAC-XC-D ordering defect was found.

The corrected whole-candidate assessment follows.

---

# 24. Exact 47-flow completeness matrix

CERT requires:

```text
47 Flow IDs total
46 executable / verification-required
1 conditional = BI-FLOW-05
```

The current exact-head re-audit classifies them as follows.

Legend:

```text
PASS-SOURCE
  current production source broadly satisfies the frozen flow semantics

PASS-SOURCE / EVIDENCE-PENDING
  source is structurally/semantically acceptable, but exact-head integration
  certification is incomplete because the integration/provider CI stage did not run

FAIL-IMPLEMENTATION
  current production source violates a frozen mandatory invariant

AUTHORITY-NORMALIZATION
  implementation is materially present but authority documents contradict
  themselves/current frozen semantics

PROOF-INVALIDATED
  the source path may exist, but proof relying on a now-invalid guarantee
  must be rerun after the owning defect is corrected

NOT-APPLICABLE
  explicitly conditional at this candidate
```

| Flow ID | Current re-audit result | Notes |
|---|---|---|
| IA-FLOW-01 | PASS-SOURCE | Identity local owner mutation |
| IA-FLOW-02 | PASS-SOURCE / EVIDENCE-PENDING | Accounts-owned provisioning action; BOUND-TX-004 preserved |
| IA-FLOW-03 | PASS-SOURCE / EVIDENCE-PENDING | registration fact → outbox/Platform → Workspace consumer path exists |
| IA-FLOW-04 | PASS-SOURCE | direct Accounts.Public fact remains valid TAC-XC-A reference |
| IA-FLOW-05 | PASS-SOURCE / EVIDENCE-PENDING | Accounts target action; BOUND-TX-002 preserved |
| IA-FLOW-06 | PASS-SOURCE | `RenameAccountCommandHandler` exists and uses Account owner path |
| WG-FLOW-01 | PASS-SOURCE | local Workspace create baseline |
| WG-FLOW-02 | PASS-SOURCE / EVIDENCE-PENDING | multi-edge A/A/E workflow preserved |
| WG-FLOW-03 | PASS-SOURCE / EVIDENCE-PENDING | Workspace membership outward fact/event reaction path exists |
| WG-FLOW-04 | PASS-SOURCE | Governance resource permission variants frozen previously |
| WG-FLOW-05 | PASS-SOURCE | Governance local read baseline |
| WG-FLOW-06 | PASS-SOURCE | canonical authorization pipeline baseline |
| WM-FLOW-01 | PASS-SOURCE | local Work create baseline |
| WM-FLOW-02 | PASS-SOURCE | Work consumer Port → Infra adapter → Collaboration Public boundary |
| WM-FLOW-03 | PASS-SOURCE / EVIDENCE-PENDING | Work target-owned action exists |
| WM-FLOW-04 | PASS-SOURCE / EVIDENCE-PENDING | Work integration-event families exist |
| WM-FLOW-05 | PASS-SOURCE | Work producer projection source ownership normalized at M10 |
| DC-FLOW-01 | PASS-SOURCE | Documents local create baseline |
| DC-FLOW-02 | PASS-SOURCE | `ArchivePageCommandHandler` now exists; Domain `Page.Archive` is authoritative |
| DC-FLOW-03 | PASS-SOURCE | BoardItem ResourceRef comment path |
| DC-FLOW-04 | PASS-SOURCE | Page ResourceRef comment path independently represented |
| DC-FLOW-05 | PASS-SOURCE / EVIDENCE-PENDING | comment/activity projection path exists |
| DC-FLOW-06 | PASS-SOURCE / EVIDENCE-PENDING | Mention actor is now explicit and mapper uses `MentionedByUserId` |
| DC-FLOW-07 | PASS-SOURCE / EVIDENCE-PENDING | Documents scoped event classification/runtime exists |
| AI-FLOW-01 | PASS-SOURCE | Automation rule owner path + Billing gate exists |
| AI-FLOW-02 | PASS-SOURCE / EVIDENCE-PENDING | Work fact → Automation execution start path exists |
| AI-FLOW-03 | PASS-SOURCE + AUTHORITY-NORMALIZATION | B/E/F chain exists; Flow Card `ProcessState` is contradictory |
| AI-FLOW-04 | PASS-SOURCE / EVIDENCE-PENDING | N8n provider action path and durable execution handling exist |
| AI-FLOW-05 | PASS-SOURCE / EVIDENCE-PENDING | ConnectCalendar persists Connection + SecretVersion + CalendarIntegration |
| AI-FLOW-06 | PASS-SOURCE / EVIDENCE-PENDING | CalendarIntegration-first disconnect + CAL-CONN-001 path exists |
| AI-FLOW-07 | **FAIL-IMPLEMENTATION** | verified inbound webhook still violates mandatory chain |
| BI-FLOW-01 | PASS-SOURCE | capability resolution |
| BI-FLOW-02 | PASS-SOURCE | Billing-owned hard-capacity mutation |
| BI-FLOW-03 | PASS-SOURCE / EVIDENCE-PENDING | Automation rule + capacity consistency |
| BI-FLOW-04 | PASS-SOURCE / EVIDENCE-PENDING | capability-after-capacity path |
| BI-FLOW-05 | NOT-APPLICABLE | conditional external Billing provider; do not fabricate one |
| AR-FLOW-01 | **FAIL-IMPLEMENTATION** | E1 event delivery exists; E2 TAC-XC-D ordering authority is wrong |
| AR-FLOW-02 | PASS-SOURCE | local Analytics projection query |
| AR-FLOW-03 | PASS-SOURCE | producer-backed rebuild source ownership is correct |
| AR-FLOW-04 | **PROOF-INVALIDATED** | stale/out-of-order recovery evidence depends on the incorrect timestamp watermark |
| PF-FLOW-01 | PASS-SOURCE | request pipeline |
| PF-FLOW-02 | PASS-SOURCE | DomainEvent → IntegrationEvent → outbox |
| PF-FLOW-03 | PASS-SOURCE | tenant restoration + dedup |
| PF-FLOW-04 | PASS-SOURCE | retry/failure runtime |
| PF-FLOW-05 | PASS-SOURCE / EVIDENCE-PENDING | compatibility/replay proof exists historically; exact-head full integration is incomplete |
| PF-FLOW-06 | PASS-SOURCE | background actor/security |
| PF-FLOW-07 | PASS-SOURCE / EVIDENCE-PENDING | scoped tenant envelope runtime present; exact-head integration suite did not execute |

Whole-catalog result:

```text
47 Flow IDs accounted for

2 implementation-blocked areas:
  AI-FLOW-07
  AR-FLOW-01:E2

1 dependent flow proof invalidated:
  AR-FLOW-04

1 internal authority normalization:
  AI-FLOW-03:E3 ProcessState

1 conditional:
  BI-FLOW-05
```

This is enough to reject the old blanket:

```text
47-flow VERIFIED
```

claim for the current candidate.

---

# 25. M10 authority conflict — not only a source defect

The previous report described `AR-FLOW-01:E2` primarily as an implementation defect.

The continuation audit confirms a deeper authority/evidence inconsistency.

## 25.1 Normative SPEC

The frozen edge says:

```text
AR-FLOW-01:E2
Mechanism: TAC-XC-D

OrderingRequirement:
Work source revision/version guard;
stale event cannot regress projection
```

This is the higher authority.

## 25.2 Current source

Current source uses:

```text
LastOccurredAt.UtcTicks
```

as `SourceRevision`.

## 25.3 CERTIFICATION stale row

The exact 47-flow certification matrix currently describes AR-FLOW-01 as:

```text
VERIFIED — event-fact move projection;
single producer-timestamp watermark
```

That certification wording is incompatible with the frozen SPEC edge.

## 25.4 STATUS stale M10 closure

STATUS also records the M10 closure wave as intentionally selecting:

```text
SourceRevision is always LastOccurredAt.UtcTicks
aggregate Version never enters ordering
```

That evidence record cannot override the SPEC.

Therefore M10 correction requires **both**:

```text
source correction
+
authority/evidence correction
```

Required document normalization:

```text
SPEC
  keep frozen producer revision/version invariant

CERTIFICATION
  remove/supersede the "single producer-timestamp watermark" VERIFIED wording

STATUS
  mark the timestamp-watermark M10 closure as superseded by the reopen audit
  retain it only as historical evidence

TESTS
  retain/reinforce the producer revision ordering proof
```

Do not rewrite history; append a superseding correction record.

---

# 26. AR-FLOW-04 must be reopened for proof, even if its code is not independently broken

`AR-FLOW-04` covers:

```text
duplicate event
stale event
missing producer snapshot
rebuild after drift
cross-workspace isolation
failure/recovery
```

The current stale-event proof is built on the same timestamp watermark that fails the frozen `AR-FLOW-01:E2` ordering authority.

Therefore:

```text
AR-FLOW-04 != independently new source blocker
```

but:

```text
AR-FLOW-04 current closure evidence is invalidated
```

until the projection uses producer revision.

Required rerun after M10 correction:

```text
duplicate revision
stale lower revision
same timestamp / higher revision
out-of-order delivery
rebuild older revision
rebuild equal revision drift repair
rebuild newer revision
concurrent rebuild/live update
source unavailable/retry
cross-workspace isolation
```

This is why M10 reopen scope is:

```text
AR-FLOW-01:E2 implementation
+
AR-FLOW-04 affected recovery proof
```

not only the single projection entity.

---

# 27. AI-FLOW-07 authority drift — STATUS "intake-only freeze" is invalid

STATUS records an M8 decision equivalent to:

```text
accepted/processed technical receipt is the effect
AI-FLOW-07 is intake-only
no downstream calendar mutation claimed
```

This is not supported by the higher authorities.

SPEC requires:

```text
Infrastructure inbound provider-delivery receipt/dedup
→ enqueue/dispatch provider-neutral semantic input
→ Integrations Application translation
→ approved target-context action/event
```

TESTS requires:

```text
Infrastructure inbound receipt/dedup
→ provider-neutral Integrations Application input
```

CERTIFICATION requires:

```text
Infrastructure inbound receipt/dedup
+
provider-neutral Integrations processing
```

Therefore:

```text
"intake-only freeze"
```

is stale evidence/authority drift and must be superseded.

The coding agent MUST NOT preserve that local STATUS interpretation.

---

# 28. AI-FLOW-07 deeper RLS defect — tenant adoption does not establish DB RLS session

The first report correctly identified the pre-tenant lookup problem under:

```text
FORCE ROW LEVEL SECURITY
```

The continuation audit finds a second RLS problem **after** successful verification.

Current command is:

```text
IAnonymousRequest
IGlobalRequest
IWriteRequest
```

`DataSessionBehavior` determines `ApplyTenantScope` **before handler execution**.

For a Global request:

```text
ApplyTenantScope = false
```

The transaction therefore opens without applying:

```text
app.current_account_id
app.current_workspace_id
app.current_user_id
app.request_scope
```

through `RlsSessionContext`.

Inside the handler, current code later calls:

```text
_bindingResolver.AdoptDerivedTenant(binding)
```

which mutates only:

```text
ICurrentTenantContext
```

It does **not** call:

```text
IRlsSessionContext.ApplyAsync()
```

and it does not restart the request descriptor/data session.

Therefore:

```text
in-memory tenant changed
!=
PostgreSQL transaction-local RLS session changed
```

This matters as soon as the mandatory provider-neutral semantic processing touches tenant-scoped tables.

A future downstream operation can appear correctly scoped in Application while PostgreSQL still sees no tenant session.

---

# 29. Required AI-FLOW-07 runtime split

The correction must explicitly solve **both** bootstrap and post-verification execution.

A safe target shape is:

```text
RAW PROVIDER BOUNDARY
  anonymous/global
  raw request bounds
  restricted binding bootstrap
  trusted Connection + CalendarIntegration lookup
  provider signature/timestamp/replay verification
  technical receipt claim

        ↓ verified trusted scope

PROVIDER-NEUTRAL TENANT EXECUTION
  explicit AccountId + WorkspaceId from trusted binding
  system/internal or approved derived-tenant principal
  Workspace-scoped request descriptor
  normal DataSession
  RLS ApplyAsync runs for that tenant
  Integrations Application translation
  target action/event
  receipt outcome finalized
```

The implementation may use another repository-approved pattern, but it must prove the same guarantees.

Forbidden shortcut:

```text
Global request
→ SetWorkspace() in memory
→ continue using the already-open global DB transaction
→ assume RLS is tenant-scoped
```

That is not true in the current pipeline.

---

# 30. Transaction boundary warning for the AI-FLOW-07 fix

Do not create a nested canonical write transaction using the same scoped EF context inside the already-open `IGlobalRequest + IWriteRequest` transaction.

The coding agent must choose one explicit transaction topology.

Preferred direction:

```text
raw/bootstrap intake coordinator is not the tenant business transaction
```

Then:

```text
verified provider-neutral command
```

enters the canonical tenant-scoped data session separately.

If technical receipt and downstream business effect must be atomic, that requires an explicit frozen transaction decision. The current authority instead describes receipt/dedup followed by dispatch/processing; do not silently invent distributed atomicity.

At minimum the flow must define:

```text
when receipt becomes Claimed/Received
when semantic dispatch is durable
when Processed may be written
what happens if semantic processing fails
who retries
how duplicate provider delivery resumes/converges
```

---

# 31. InboundWebhookReceipt source contradicts the actual runtime

Current Infrastructure model comment says the provider callback is:

```text
captured
verified
deduplicated
then handed to the Application use case
```

but current runtime does not perform that handoff.

Current handler calls:

```text
CalendarWebhookIntake.AcceptAsync(...)
```

and the receipt is immediately:

```text
MarkProcessed(...)
```

There is no provider-neutral Application processing command after the claim.

This comment is therefore evidence/source documentation drift and must be corrected together with the runtime.

---

# 32. Connection-aware receipt identity remains mandatory

Current technical receipt contains:

```text
Provider
ExternalEventId
PayloadHash
ProtectedPayload
ReceivedAt
Status
ProcessedAt
FailureReason
```

It has no:

```text
ConnectionId
```

and deduplicates by:

```text
(provider, external_event_id)
```

TESTS/SPEC require a trusted `IntegrationConnection` in the intake chain, and SPEC explicitly includes `ConnectionId` in the technical receipt identity.

Required change:

```text
InboundWebhookReceipt.ConnectionId
```

and connection-aware dedup, for example:

```text
(ConnectionId, Provider, ExternalEventId)
```

subject to the provider's exact event-ID contract.

Also update:

```text
EF configuration
migration
unique index
claim SQL
Application intake contract
integration tests
retention/diagnostic queries if any
```

Do not change only the index while leaving the semantic contract connection-blind.

---

# 33. Webhook path uniqueness is already correctly enforced

The continuation audit checked the current exact-head EF mapping.

`CalendarIntegrationConfiguration` has:

```csharp
builder.HasIndex(x => x.WebhookPath)
    .IsUnique()
```

Therefore:

```text
WebhookPath as a stable globally unique locator
```

is structurally supported.

The problem is not locator uniqueness.

The problem is:

```text
locator resolution
→ does not validate IntegrationConnection
→ cannot safely bootstrap through current FORCE-RLS app path
```

This distinction matters: do not redesign the path generation/index unnecessarily.

---

# 34. Revalidated mandatory flows that were IMPLEMENT-MISSING in the original baseline

Several old Flow Card source-state descriptions are stale because implementation landed later.

The coding agent must not reopen them merely because the original Flow Card says `IMPLEMENT-MISSING`.

Exact-head source revalidation confirms:

## IA-FLOW-06

Present:

```text
RenameAccountCommandHandler
→ IAccountDbContext
→ Account.Rename
```

No reopen required.

## DC-FLOW-02

Present:

```text
ArchivePageCommandHandler
→ IDocumentDbContext
→ Page.Archive
→ PageArchivedDomainEvent
```

No reopen required.

## DC-FLOW-06

Current Mention model/mapper now carries:

```text
MentionedByUserId
```

and mapper sets:

```text
ActorUserId = MentionedByUserId
```

The original actor defect is no longer present.

No reopen required unless affected exact-head integration evidence is requested.

## AI-FLOW-05

Current `ConnectCalendarCommandHandler` persists/relates:

```text
IntegrationConnection
IntegrationSecretVersion
CalendarIntegration(ConnectionId)
```

and supports secret rotation/reconnect semantics.

No source-level reopen required.

## AI-FLOW-06

Current `DisconnectCalendarCommandHandler`:

```text
deactivates CalendarIntegration first
then evaluates remaining active bindings
then revokes generic IntegrationConnection only when appropriate
```

This matches `CAL-CONN-001` shape.

No source-level reopen required.

## AI-FLOW-03

Production `AutomationMoveItemUseCase` exists.

Do not treat the old `IMPLEMENT-MISSING` disposition as current source state.

Only normalize its `ProcessState` authority.

---

# 35. M8 reopen must be selective

M8 was historically marked FROZEN/VERIFIED in STATUS.

The re-audit does **not** invalidate every M8 flow.

Reopen only:

```text
AI-FLOW-07
AI-FLOW-03:E3 authority normalization
```

Retain current source for:

```text
AI-FLOW-01
AI-FLOW-02
AI-FLOW-04
AI-FLOW-05
AI-FLOW-06
```

unless the AI-FLOW-07 implementation change modifies their shared contracts/runtime, in which case rerun affected proof only.

---

# 36. M10 reopen must be selective but wider than one method

Reopen:

```text
AR-FLOW-01:E2
AR-FLOW-04 affected ordering/recovery proof
```

Likely production surfaces:

```text
Work domain/event source revision capture
Work integration event contract(s)
event registry/version compatibility
Analytics consumers
WorkspaceWorkItemPlacementService
WorkspaceWorkItemPlacementProjection
rebuild path
projection migration/rebuild operation
tests
```

Do not reopen:

```text
AR-FLOW-02 local query ownership
AR-FLOW-03 producer-source ownership topology
```

unless implementation changes break them.

---

# 37. Cross-authority correction set

The final correction wave now has three different authority-edit classes.

## 37.1 SPEC correction

Only the internal contradiction:

```text
AI-FLOW-03:E3
ProcessState: NotApplicable
```

must be normalized to:

```text
ProcessState: AutomationExecution
```

This does not change mechanism classification.

## 37.2 CERTIFICATION correction

Supersede the stale AR row:

```text
AR-FLOW-01 VERIFIED — single producer-timestamp watermark
```

because it conflicts with SPEC's revision/version guard.

Do not mark it VERIFIED again until producer revision ordering is implemented and proven.

## 37.3 STATUS correction

Append a new correction wave that supersedes:

```text
M8 AI-FLOW-07 intake-only freeze
M10 producer-timestamp watermark closure
M14 old candidate evidence
```

Historical text remains historical.

STATUS must not delete or rewrite prior evidence.

---

# 38. Revised coding-agent execution order

The coding agent should now execute exactly this sequence.

## Wave A — authority/evidence reopen bookkeeping

```text
1. append correction record to STATUS
2. mark AI-FLOW-07 reopened
3. mark AI-FLOW-03:E3 authority normalization pending
4. mark AR-FLOW-01:E2 reopened
5. mark AR-FLOW-04 proof invalidated
6. mark affected M12 proof stale
7. mark M14 exact-SHA certification stale
```

## Wave B — fix Work producer revision contract

```text
1. identify producer aggregate revision at mutation/event creation
2. carry revision in placement-relevant Work integration facts
3. apply event-version compatibility policy
4. switch Analytics SourceRevision to producer revision
5. switch rebuild reconcile to snapshot.Revision
6. migrate/rebuild old tick-scale projection state
7. run ordering/rebuild/failure/concurrency proof
```

## Wave C — fix Calendar bootstrap and intake identity

```text
1. introduce narrow FORCE-RLS-safe binding bootstrap
2. resolve CalendarIntegration + IntegrationConnection together
3. validate active/provider/tenant lifecycle consistency
4. include ConnectionId in trusted snapshot
5. include ConnectionId in inbound receipt identity
6. update migration + unique index + atomic claim
```

## Wave D — fix raw HTTP boundary

```text
1. content-type allowlist
2. bounded raw-byte/body read before materialization
3. preserve exact raw bytes for signature/hash semantics
4. reject oversized/malformed requests at real HTTP boundary
```

## Wave E — split verified tenant processing from global bootstrap

```text
1. verify provider callback
2. claim/record technical intake according to frozen semantics
3. dispatch provider-neutral Integrations Application input with trusted tenant
4. execute through a tenant-scoped/system-internal approved request
5. ensure RlsSessionContext.ApplyAsync runs for derived tenant
6. execute semantic translation/target action or event
7. only then transition receipt to the correct terminal processed state
```

## Wave F — normalize Process Manager authority

```text
AI-FLOW-03:E3 ProcessState = AutomationExecution
```

No new process aggregate.

## Wave G — affected M12 proofs

Rerun only changed chains.

## Wave H — exact-SHA M14

```text
OpenAPI sync/review
full integration/provider suite
runtime-owner profiles
exact SHA
CI IDs
final CERT outcome
```

---

# 39. Additional AI-FLOW-07 mandatory tests

The previous test list remains required and is expanded by the continuation audit.

## Raw HTTP boundary

```text
Content-Type allowed
Content-Type rejected
Content-Length over limit
chunked/no Content-Length body exceeds streaming limit
empty body
malformed JSON
raw bytes used consistently for signature + hash
```

## Bootstrap

```text
unknown WebhookPath
known WebhookPath + missing Connection
known binding + revoked Connection
known binding + deleted Connection
Connection tenant mismatch
Connection provider mismatch
inactive CalendarIntegration
deleted CalendarIntegration
```

## FORCE RLS

Using the real restricted app role:

```text
ordinary direct SELECT without tenant
→ cannot enumerate CalendarIntegration rows

narrow bootstrap operation by exact WebhookPath
→ returns only minimum binding authority

bootstrap cannot enumerate arbitrary tenant rows
```

## Post-verification RLS

Prove separately:

```text
AdoptDerivedTenant in memory alone
does not count as proof

provider-neutral tenant command
→ DataSession applies AccountId/WorkspaceId to PostgreSQL RLS session
→ tenant-scoped read/write succeeds only in trusted derived tenant
→ cross-tenant target remains invisible/denied
```

## Receipt lifecycle

```text
duplicate same Connection/provider/event
→ one logical receipt

same provider/event on different valid Connections
→ behavior matches provider event-ID scope;
  no accidental cross-connection dedup collision

semantic processing success
→ Processed

semantic processing retryable failure
→ not falsely Processed

terminal/reconciliation outcome
→ durable explicit state

duplicate after partial processing
→ converges without duplicate semantic effect
```

---

# 40. Additional TAC-XC-D mandatory tests

Add to the M10 suite:

```text
producer rev 10 @ timestamp T
producer rev 11 @ same timestamp T
→ rev 11 wins

receive rev 11
then rev 10
→ rev 10 ignored

receive rev 11 twice
→ idempotent

rebuild rev 10 against local rev 11
→ local rev 11 preserved

rebuild rev 11 against drifted local rev 11
→ equal-revision drift policy deterministic

concurrent live rev 12 while rebuild rev 11 writes
→ semantic revision + physical xmin guarantees converge

old tick-based persisted projection
→ rebuilt/backfilled to producer revision domain
→ first post-deployment live event is not falsely stale
```

---

# 41. Revised final blocker hierarchy

The current candidate should be evaluated in this order:

```text
BLOCKER 1
AI-FLOW-07 implementation incomplete

BLOCKER 2
AR-FLOW-01:E2 TAC-XC-D violates frozen producer revision ordering

AUTHORITY CORRECTION
AI-FLOW-03:E3 ProcessState contradiction

DEPENDENT PROOF INVALIDATION
AR-FLOW-04

EVIDENCE BLOCKER AFTER SOURCE FIX
exact-head integration/provider suite + M14
```

Therefore exactly one current final outcome remains:

```text
BLOCKED-IMPLEMENTATION
```

Do not downgrade to `BLOCKED-EVIDENCE` until both implementation blockers are closed.

---

# 42. Final reopen scope after continuation audit

```text
REOPEN M8
  AI-FLOW-07 implementation
  AI-FLOW-03:E3 authority normalization

REOPEN M10
  AR-FLOW-01:E2 implementation
  AR-FLOW-04 affected proof

REOPEN M12
  affected cross-pack/runtime-owner proof only

REOPEN M14
  exact-SHA final certification
```

Do not reopen the complete:

```text
M4
M5
M6
M7
M9
M11
```

based on the evidence currently reviewed.

---

# 43. Final coding-agent stop conditions

The coding agent must STOP rather than invent semantics when any of these occur:

```text
provider webhook signing authority cannot be tied to an existing trusted
connection/provider model without a product/provider decision

the provider's event-id uniqueness scope is unknown and determines whether
ConnectionId belongs in the dedup key

downstream Calendar webhook business effect is not defined by existing
Integrations/Application authority

receipt + downstream effect are required to be atomically committed across
different transaction boundaries but no frozen transaction decision exists

a Work integration-event schema revision requires an incompatible breaking
change without an approved version/upcast rule

projection backfill cannot be completed from IWorkItemProjectionSource
without losing authoritative source state
```

In those cases:

```text
record BLOCKED-DECISION
```

for that exact unresolved semantic decision.

Do not silently select behavior.

---

# 44. Final completeness verdict

After the continuation audit, this file now covers:

```text
47/47 Flow IDs
31 cross-BC edge taxonomy at A..F level
mandatory Local Flow AI-FLOW-07
source defects
authority contradictions
dependent proof invalidation
RLS bootstrap
post-bootstrap RLS execution
projection migration
exact-SHA evidence
selective reopen scope
coding-agent execution order
stop conditions
```

Current state remains:

```text
BLOCKED-IMPLEMENTATION
```

The next valid state after the source corrections, but before a green exact-SHA runtime run, is:

```text
BLOCKED-EVIDENCE
```

Only after exact-SHA runtime-owner proof passes may the candidate be evaluated for:

```text
ARCHITECTURE-CLOSED
```


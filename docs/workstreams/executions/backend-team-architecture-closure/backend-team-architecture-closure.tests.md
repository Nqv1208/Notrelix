---
document_id: EXEC-BACKEND-TEAM-ARCHITECTURE-CLOSURE-TESTS
title: Backend Team Architecture Bootstrap & Closure — Test & Enforcement Specification
version: 2.6
status: execution-ready
supersedes: version 2.5
repository: Nqv1208/Notrelix
branch: architecture/backend-boundary-execution
audit_snapshot:
  commit: b94312b3e10e2212f18440003e775d415aa1b5d7
  pr: 113
audit_revision: interaction-architecture-normalized-source-pattern-tests-v2.6-public-capability-topology
depends_on:
  - backend-team-architecture-closure.v2.6.spec.md@2.6
  - backend-team-architecture-closure.v2.6.plan.md@2.6
upstream_test_authority:
  - ARCH-BC-001..ARCH-BC-008
  - STN-ARCH-001/002/005/006/007/008
---

# Backend Team Architecture Bootstrap & Closure — Test & Enforcement Specification

## 0. Revision intent

historical bootstrap revision retains the v2 positive-existence testing rule and adds v2.1 source-normalization plus concrete-reference pinning. The previous rule was:

```text
missing real flow
→ DEFER-NO-REAL-FLOW
```

with:

```text
mandatory reference flow missing
→ FAIL
```

The backend architecture is not considered ready for team development merely
because no invalid source exists.

It must also prove the positive architecture:

```text
the canonical mechanism exists
+
is executable
+
is wired
+
has failure semantics
+
is protected by architecture gates
+
can be copied by future feature teams
```

This file therefore tests both:

```text
NEGATIVE ARCHITECTURE SAFETY
and
POSITIVE REFERENCE ARCHITECTURE EXISTENCE
```

The current-candidate SPEC additionally makes the **interaction edge** explicit:

```text
Real Flow
→ zero/one/many Interactions[]
→ every actual cross-BC edge
→ exactly one TAC-XC-A..F mechanism
→ explicit semantic/runtime/failure ownership
```

TESTS must therefore prove two separate things:

```text
authority consistency
→ the Flow Card/edge is classified correctly before coding

production proof
→ the code/runtime actually implements the frozen edge semantics
```

This does **not** create a seventh mechanism, a second gate system, a new CI
lane, or a requirement to simulate remote services.

---

# 1. Test philosophy

The v2 test system must answer four questions.

## Q1 — Is the current source legal?

Proven by:

```text
ARCH-BC-*
STN-ARCH-*
```

## Q2 — Does the required canonical reference exist?

Proven by:

```text
TAC-RAP-*
TAC-REF-*
```

reference completeness tests.

## Q3 — Does the reference actually behave?

Proven by:

```text
unit
behavior
integration
runtime/DI
```

tests.

## Q4 — Would the wrong implementation fail?

Proven by:

```text
gate self-tests
negative fixtures
baseline-regrowth tests
cross-context negative scenarios
```

All four are required.

## Q5 — Does the implementation preserve the frozen interaction model?

Proven by:

```text
FlowId + EdgeId traceability
TAC-XC-A..F mechanism-specific proof
current binding proof
runtime-owner proof
extraction-blocker proof where applicable
```

For a cross-BC flow, Q5 is required in addition to Q1–Q4.

---

# 2. Test ownership rule

Existing architecture owners remain authoritative.

Examples:

```text
Public purity
→ ARCH-BC-005

Application transport/provider purity
→ ARCH-BC-006

Integration Event ownership/version
→ ARCH-BC-007

Common semantic leakage
→ ARCH-BC-008

Infrastructure/Services anti-growth
→ STN-ARCH-006
```

TAC tests may:

- harden;
- add self-tests;
- add completeness assertions;
- add reference behavior tests.

TAC MUST NOT create a duplicate competing scanner for the same property.

---

# 3. Test ID families

Use:

```text
TAC-BASE-*    exact baseline/inventory
TAC-GATE-*    gate hardening and self-tests
TAC-TX-*      transaction/consistency
TAC-IA-*      Identity & Accounts pack
TAC-WG-*      Workspace & Governance pack
TAC-WM-*      Work Management pack
TAC-DC-*      Documents & Collaboration pack
TAC-AI-*      Automation & Integrations pack
TAC-BI-*      Billing & Entitlements pack
TAC-AR-*      Analytics & Reporting pack
TAC-PF-*      Platform & Foundation pack
TAC-XPK-*     cross-pack integration
TAC-DI-*      dependency injection
TAC-MIG-*     migration/cleanup
TAC-COPY-*    team-copy/reference completeness
TAC-FINAL-*   exact-SHA closure
```

---

# 4. Mandatory pack result model

For each mandatory pack:

```text
TAC-RAP-IA
TAC-RAP-WG
TAC-RAP-WM
TAC-RAP-DC
TAC-RAP-AI
TAC-RAP-BI
TAC-RAP-AR
TAC-RAP-PF
```

the only valid final test statuses are:

```text
PASS
FAIL
BLOCKED-DECISION
```

The following is forbidden:

```text
DEFER-NO-REAL-FLOW
```

for the pack or any reference marked mandatory by SPEC v2.

---

# 5. Test layers

## L1 — Architecture

Assembly/type/dependency/namespace/source topology.

## L2 — Unit/behavior

Pure contract, mapper, state transition, handler behavior.

## L3 — Persistence/integration

Transactions, outbox, projections, process state, provider adapters.

## L4 — Runtime wiring

DI, consumer registration, handlers, adapter resolution.

## L5 — Cross-pack

Producer→platform→consumer end-to-end reference chain.

## L6 — CI

Exact-SHA build/test proof.

A reference is not complete with only L1.

### Interaction-authority consistency check

Before the current pack starts implementation, perform a source/docs authority
check against the revised SPEC:

```text
Flow Card exists
→ FlowKind/SupportingShapes present
→ Interactions[] present
→ every actual cross-BC edge has EdgeId
→ every edge mechanism ∈ TAC-XC-A..F
→ current-pack DecisionStatus is executable/frozen
```

This is an execution/document consistency proof. It does not require a new
production architecture scanner or CI job.

---

# 5A. Runtime-owner proof rule — v2.6

A mandatory guarantee must be tested at the production layer that owns it.

Examples:

```text
delivery retry
→ real consume filter + consumer transaction

target idempotency
→ target Public action + target persistence

target authorization
→ target authorization entry point

Billing quota
→ entitlement scope + usage ledger + feature lifecycle

Analytics source boundary
→ producer Public implementation + consumer adapter
```

A lower-level unit test can supplement but cannot replace the runtime-owner
proof.

A test that manually calls `SaveChangesAsync()` after invoking a consumer
directly does not prove production commit/rollback behavior when production is
wrapped by `DeduplicationConsumeFilter`.

# 5B. BC-local baseline proof — v2.6

Every business BC must have at least one executable local owner-side flow.

Required:

```text
Identity
Accounts
Workspaces
Governance
Work Management
Documents
Collaboration
Automation
Integrations
Billing
Analytics / Reporting
```

A test suite fails certification if one paired team has only sibling-BC flows.

Examples:

```text
Collaboration comment tests
≠ Documents baseline

N8n tests
≠ Integrations local connection baseline

AcceptInvitation tests
≠ Governance local mutation baseline
```

# 5C. Real Flow evidence rule — v2.6

Each mandatory `*-FLOW-*` must have:

```text
production source
behavior test
runtime/integration test when runtime semantics matter
architecture/gate proof
```

The following do not count:

```text
NotImplementedException handler
*StubConsumer*
log-only consumer
test-only fake flow
direct lower-level test that bypasses production runtime owner
```

# 5D. Variant proof rule — v2.6

If one handler exposes variants with different resource owner/auth semantics,
each variant requires independent proof.

Pinned:

```text
CreateComment.ForBoardItem
CreateComment.ForPage
```

Passing the BoardItem path does not certify the Page path.

# 5E. Semantic-authority proof rule — v2.6

A passing test must prove not only behavior, but that the correct model owns the
behavior.

Examples:

```text
Calendar connect
→ IntegrationConnection generic relationship
→ IntegrationSecretVersion persisted secret reference
→ CalendarIntegration Workspace binding

Calendar disconnect
→ CalendarIntegration deactivation first
→ generic Connection cleanup only under CAL-CONN-001

provider inbound webhook
→ Infrastructure intake/dedup
≠ outbound WebhookDelivery

Mention actor
→ owner-side actor fact
≠ MentionedId
```

# 5F. Persistence-authority proof rule — v2.6

When an in-memory Domain property is not the persisted authority, tests must
round-trip the actual storage model.

Pinned Calendar proof:

```text
create/reuse Connection
persist IntegrationSecretVersion
persist CalendarIntegration
reload DbContext
→ SecretReference/version/binding remain correct
```

A test that only inspects the in-memory `CurrentSecretRef` is false-green.

# 5G. Hard-quota concurrency proof rule — v2.6

Sequential capability/usage tests do not certify a hard quota.

Mandatory race:

```text
remaining capacity = 1

request A || request B
→ exactly one capacity reservation succeeds
→ exactly one capacity-consuming feature creation succeeds
```

This proof must use the real database/concurrency mechanism.

# 5H. Scoped-event runtime proof rule — v2.6

For tenant-scoped business Integration Events:

```text
WorkspaceId != null
→ AccountId != null
```

The runtime proof must execute:

```text
producer/outbox
→ TenantContextConsumeFilter
→ consumer
```

and assert the consumer sees the expected Account + Workspace tenant, not
System context.

# 5I. Interaction-edge proof rule — current candidate

A Real Flow may contain several mechanisms.

Tests MUST NOT assume:

```text
one FlowId
=
one TAC-XC mechanism
```

Cross-BC evidence is addressed as:

```text
FlowId:EdgeId
```

Examples:

```text
IA-FLOW-02:E1
IA-FLOW-03:E1
IA-FLOW-05:E1
```

`EdgeId` is a traceability sub-label only. It is not a new Flow ID or test
family.

For each current-pack edge, the evidence must prove the fields that materially
affect runtime correctness:

```text
Mechanism
ContractOwner
WorkflowOwner
Mutation/Projection/Process owner where applicable
CallerWaitsForOutcome
ConsistencyModel
CurrentBinding
Idempotency
Retry/failure owner
ExtractionBlocker where applicable
```

A field marked `DEFERRED-{MILESTONE}` in SPEC is not a coding-agent choice.
The corresponding deep proof becomes mandatory at that milestone entry.

# 5J. Mechanism-specific readiness proof — current candidate

Do not use one generic "remote substitution" test for every mechanism.

Required interpretation:

```text
TAC-XC-A Public Direct
→ prove semantic owner/Public purity/direct behavior
→ a consumer Port is NOT mandatory
→ runtime substitution may legitimately remain deferred

TAC-XC-B Port + ACL + Adapter
→ prove Application depends on consumer Port
→ ACL is consumer-owned/pure where present
→ adapter is Infrastructure/runtime-owned and policy-thin
→ handler/domain do not depend on transport

TAC-XC-C Integration Event
→ prove committed producer fact + outbox
→ Platform delivery + tenant restoration where scoped
→ consumer dedup + consumer-local transaction
→ producer commit is independent of later consumer failure

TAC-XC-D Projection
→ prove consumer-local derived state
→ duplicate/out-of-order protection
→ local query
→ authoritative rebuild/recovery source

TAC-XC-E Target-owned Action
→ prove target owns mutation/business rejection/idempotency
→ consumer cannot mutate target private state
→ current transaction semantics are tested exactly
→ remote command substitution is NOT certified when an extraction blocker exists

TAC-XC-F Process Manager
→ prove workflow owner + durable progression
→ restart/resume
→ participant outcomes
→ business failure/compensation semantics where authoritative
→ technical delivery retry remains Platform/runtime-owned
```

Supporting shapes (`Local`, `ResourceRef`, provider Port, Platform delivery) are
not valid replacement mechanism values.

# 6. TAC-BASE-001 — Exact implementation SHA

Required before executing closure tests.

Record:

```text
ImplementationSHA
ParentSHA
BaseSHA
```

If source changes:

```text
affected proofs must rerun
```

---

# 7. TAC-BASE-002 — Canonical BC inventory

Assert execution inventory contains exactly the accepted business contexts:

```text
Accounts
Identity
Workspaces
Governance
WorkManagement
Documents
Collaboration
Automation
Integrations
Billing
Analytics
```

Implementation modules such as:

```text
Notifications
```

must map to a canonical context or an exact compatibility classification.

They must not silently become extra BCs.

---

# 8. TAC-BASE-003 — Five production projects

Assert production project topology remains:

```text
Notrelix.Domain
Notrelix.Application
Notrelix.Infrastructure
Notrelix.Platform
Notrelix.API
```

Do not count test/tooling projects.

---

# 9. TAC-BASE-004 — Mandatory Team Operating Pack inventory

The architecture test/evidence layer must contain one manifest or equivalent
structured source of truth identifying all mandatory packs and reference IDs.

Required:

```text
IA-REF-001
IA-REF-002
IA-REF-003

WG-REF-001
WG-REF-002
WG-REF-003

WM-REF-001
WM-REF-002
WM-REF-003
WM-REF-004

DC-REF-001
DC-REF-002
DC-REF-003
DC-REF-004

AI-REF-001
AI-REF-002
AI-REF-003
AI-REF-004

BI-REF-001
BI-REF-002
BI-REF-003 applicable provider boundary

AR-REF-001

PF reference delivery chain
```

The manifest must point to real source/test evidence.

Do not satisfy this with a markdown-only registry.

---

# 10. TAC-BASE-005 — Mandatory reference must have executable evidence

For every mandatory reference:

```text
SourceType
BehaviorTest
ArchitectureGate
```

must exist.

Where runtime-bound:

```text
DIResolutionTest
```

also required.

Where persistence/event/process/projection:

```text
IntegrationTest
```

also required.

A reference containing only an interface fails.

---

# 10A. TAC-BASE-006 — Reference disposition is source-backed

Every mandatory reference evidence row must record one disposition:

```text
REUSED
HARDENED
MIGRATED
IMPLEMENTED-MISSING
```

and cite the source inventory that justified it.

`IMPLEMENTED-MISSING` fails review if a semantically equivalent current source
already existed and was ignored.

This prevents duplicate event, process, ResourceRef, provider and Billing
contract implementations.

# 10B. TAC-AUTO-001 — Mandatory reference pinning

The suite/evidence must prove the implementation uses the historical bootstrap revision pinned
references, not an agent-selected substitute.

Required source identities:

```text
AcceptInvitation
MoveBoardItem
CreateCommentCommand.ForBoardItem
CreateAutomationRule
AutomationExecution
WorkspaceWorkItemPlacementProjection
```

If one is absent/materially changed at the candidate SHA, the execution state is:

```text
STOP-SOURCE-DRIFT
```

not “select another reference”.

# 10C. TAC-AUTO-002 — No unresolved mandatory design choice

Final execution evidence must not contain a mandatory implementation state of:

```text
SELECT-LATER
CHOOSE-A-B-C
TBD
TODO
PREFER-ONE
BLOCKED-DECISION
```

The candidate transaction decision is already:

```text
B / BOUND-TX-002
```

Discovery remains allowed only to verify source identity and detect drift.

# 11. TAC-GATE-001 — ARCH-BC-005 exact Common allowlist

Harden existing `PublicSemanticContractArchitectureTests`.

Required behavior:

```text
System.* → PASS
exact approved technical Common → PASS
arbitrary Common → FAIL
```

Negative fixture examples:

```text
Common PlanTier-like type → FAIL
Common WorkspaceRole-like type → FAIL
Common entitlement business enum → FAIL
```

Existing accepted debt must not become Public automatically.

---

# 12. TAC-GATE-002 — ARCH-BC-005 producer-aware Public rule

Current foreign Public wildcard must be removed.

Classifier input must include:

```text
source Public type
source producer
referenced Public type
referenced producer
```

Required:

```text
Identity.Public → Identity.Public
PASS

Identity.Public → Accounts.Public
FAIL by default
```

Exact reviewed exceptions only.

No namespace wildcard.

---

# 13. TAC-GATE-003 — Public forbidden matrix

Self-test:

```text
Domain aggregate → FAIL
Domain enum → FAIL
DbContext → FAIL
repository → FAIL
Infrastructure type → FAIL
Platform type → FAIL
API type → FAIL
EF type → FAIL
HttpClient → FAIL
MassTransit → FAIL
gRPC → FAIL
provider SDK → FAIL
internal Application type → FAIL
```

---

# 14. TAC-GATE-004 — Selected Public surface non-vacuity

Once historical bootstrap revision references are implemented, every **pinned producer Public surface**
must contain real production types and consumers/tests.

Required candidates include:

```text
Identity Public
Accounts Public
WorkManagement Public target action
Billing-owned capability surface
```

Governance is different:

```text
standard authorization may remain the existing canonical pipeline
```

so this test MUST NOT create/require a ceremonial Governance.Public solely for
non-vacuity.

If WG-REF-002 selects a real use-case-specific Governance Public surface, that
surface then becomes non-vacuity protected.

This prevents both:

```text
reference deleted → false green
```

and:

```text
empty Public folder created just to satisfy test
```
# 15. TAC-GATE-005 — STN-ARCH-006 exact Services baseline

Harden existing owner.

Required helper tests:

```text
baseline {A,B}, actual {A,B} → PASS
baseline {A,B}, actual {A}   → FAIL
baseline {A}, actual {A}     → PASS
baseline {A}, actual {A,B}   → FAIL
```

Full directory removal:

```text
baseline must become empty
```

No early-return with stale names.

---

# 16. TAC-GATE-006 — ARCH-BC-008 Common no-growth

Required:

```text
current exact debt allowed
new business semantic Common type fails
removed debt allowance cannot silently regrow
```

Reference packs may not introduce:

```text
shared PlanTier
shared Permission
shared WorkspaceRole
shared AccountStatus
```

for convenience.

---

# 17. TAC-GATE-007 — ARCH-BC-006 provider/Broker purity

Required negative cases:

```text
System.Net.Http
MassTransit
RabbitMQ.Client if present
Grpc
Stripe
SendGrid
Twilio
Microsoft.Graph
Google.Apis
Amazon
Azure.Messaging
Azure.Storage
StackExchange.Redis
Npgsql low-level client
```

Application business source must fail if referencing provider/runtime clients.

---

# 18. TAC-GATE-008 — No example/demo production namespaces

Production source must not introduce architecture teaching namespaces such as:

```text
Examples
Samples
DemoArchitecture
FakeFeature
ReferenceExample
Playground
```

Exception only if such namespace already has real product meaning and is
reviewed.

The reference implementation itself must be canonical production shape.

---

# 18A. TAC-GATE-008A — Work fact producer ownership migration

After WM-REF-004 / AI-REF-001 migration:

```text
BoardItemMemberAssignedForAutomationIntegrationEvent
```

must no longer remain the active outward contract.

Required proof:

```text
member-assigned outward fact resolves to WorkManagement producer
Automation consumer uses producer-owned Work event
event registry producer = WorkManagement
old consumer-coupled contract removed/retired according to compatibility policy
ARCH-BC-007 exact migrate-on-touch baseline entry removed/shrunk
```

Creating both old and new active semantic events fails.

# 18B. TAC-GATE-008B — Messaging consumer is a thin adapter

For the N8n dispatch reference, the Infrastructure message consumer must not own
Automation business progression.

The final consumer must not directly combine all of:

```text
IAutomationDbContext access
AutomationRule business evaluation
AutomationExecution Start/Succeed/Fail lifecycle
provider invocation
broker retry
```

The business transition belongs to an Application-owned Automation use case/
process. Broker consumer responsibility is transport/context/delegation.

Use semantic/type inspection, not a global ban on all DbContext use by every
technical consumer.

# 18C. TAC-GATE-008C — Domain is broker-neutral

Scan the selected Automation Domain reference for broker-specific concepts.

Forbidden in Domain API/semantic comments:

```text
MassTransit
ConsumeContext
broker redelivery
dead-letter
queue/topic routing
```

`Queued` may remain an Automation execution product state if product semantics
define it.

The known `RequeueForRedelivery` method must be removed or reframed as
Automation-owned retry/recovery semantics.

# 18D. TAC-GATE-008D — No duplicate workflow state model

The reference must reuse `AutomationExecution` as the current durable Automation
execution/process state unless a separate architecture decision proves another
workflow aggregate.

A new `AutomationProcessState`/equivalent created only to satisfy `/Processes`
fails.

# 18E. TAC-GATE-008E — One canonical ResourceRef

`Notrelix.Domain.SharedKernel.ResourceRef` is the current canonical reference.

Do not add a second context-specific ResourceRef with the same semantics.

# 18F. TAC-GATE-008F — Existing provider seam normalized, not duplicated

For N8n:

```text
one Application/semantic provider boundary
one Infrastructure N8n adapter family
```

The old `Application/Common/Integrations/N8n` surface must be migrated,
reclassified as exact technical debt, or removed. A new parallel interface
while the old one remains normative fails.

# 18G. TAC-GATE-008G — Existing Billing entitlement seam normalized, not duplicated

The current `Application/Common/Entitlements` family is source debt/legacy
semantic surface to migrate or explicitly freeze.

A new Billing Public capability plus an unchanged competing Common capability
authority fails.

# 19. TAC-GATE-009 — Canonical role folders cannot be empty

Reuse `STN-ARCH-007`.

Because v2 intentionally creates:

```text
Ports
Processes
Projections
CrossContext
Public
```

for required references, tests must assert each created role folder contains
real production `.cs` types.

Marker-only remains FAIL.

---

# 20. TAC-GATE-010 — Interaction mechanism closure / no seventh mechanism

The same gate owner now verifies both taxonomy and edge classification.

Every actual cross-BC reference edge must be one of:

```text
TAC-XC-A
TAC-XC-B
TAC-XC-C
TAC-XC-D
TAC-XC-E
TAC-XC-F
```

Required authority-consistency assertions:

```text
47 Flow IDs remain exact
every Flow Card has FlowKind
every Flow Card has SupportingShapes
every Flow Card has Interactions[]

local-only flow
→ Interactions[] may be empty

supporting/pipeline flow
→ FlowKind = Supporting is allowed
→ Interactions[] may be empty only when actual business cross-BC edges are
  recorded on participating Flow Cards

Mixed flow
→ Interactions[] must not be empty

cross-BC edge
→ EdgeId present
→ mechanism exactly TAC-XC-A..F

Local / ResourceRef / ProviderPort / PlatformDelivery
→ supporting shape only
→ never accepted as seventh mechanism

multi-edge Flow Card
→ each edge classified independently
→ one flat InteractionClass for the whole flow is forbidden
```

Required negative fixtures/audit cases:

```text
TAC-XC-G
→ FAIL

Mechanism = Local
on a real cross-BC edge
→ FAIL

unknown/unclassified cross-BC edge
→ FAIL

same Flow Card contains TAC-XC-A + TAC-XC-E
→ PASS when they are distinct EdgeIds
```

Implementation guidance:

- reuse the existing mechanism/reference completeness owner;
- an exact manifest/docs consistency check may supplement architecture tests;
- do not create a duplicate competing scanner;
- avoid naive forbidden-name tests.

At PRE-M4, the global 47-flow classification is an authority audit.
Production mechanism-specific runtime proofs land in the owning pack tests.

---

# 21. TAC-TX-001 — BOUND-TX-002 rollback behavior

historical bootstrap revision requires Option B.

Execute the real `AcceptInvitation` path after replacing the foreign private
Accounts abstraction with the Accounts Public target action.

Required proof:

```text
Accounts mutation succeeds
→ later Workspace-side failure
→ one current request transaction rolls back Accounts mutation
```

Also prove:

```text
successful acceptance commits Account + Workspace membership
duplicate/retry does not create duplicate memberships
Workspaces no longer injects IAccountMembershipProvisioner
```

A separate-commit implementation does not satisfy historical bootstrap revision.
# 22. TAC-TX-002A — Separate-authority model

Not selected for historical bootstrap revision.

Assert the final decision record does not select A.

This section remains only to prevent accidental drift into a different
transaction model during implementation.
# 23. TAC-TX-002B — Shared-transaction exception model — REQUIRED

Assert exact decision metadata:

```text
Decision = B
ExceptionId = BOUND-TX-002
WorkflowOwner = Workspaces
AccountsMutationOwner = Accounts
WorkspacesMutationOwner = Workspaces
SemanticBoundaryReady = true
RuntimeSubstitutionReady = false/deferred
ExtractionBlocker = true
```

Assert the exception has an explicit removal trigger:

```text
Accounts/Workspaces service extraction
or
approved product decision allowing partial success/reconciliation
```

Behavior proof is TAC-TX-001.
# 24. TAC-TX-002C — Redesign branch

Not selected for historical bootstrap revision.

Assert the final decision record does not select C.
# 24A. TAC-GATE-011 — Public action semantic fields cannot be ignored

For the pinned WorkManagement target action, source/behavior evidence must show
all declared semantic fields are consumed:

```text
OperationId
WorkspaceId
ExecutorUserId
```

Fail when:

```text
Public request contains field
and
producer implementation never reads/uses it
```

This gate may use focused source/type inspection for the exact pinned reference;
do not create a global brittle regex over all DTOs.

# 24B. TAC-GATE-012 — Analytics consumer adapter cannot own producer persistence

Fail if any type under:

```text
Infrastructure/CrossContext/Analytics/WorkManagement
Infrastructure/Messaging/Consumers/Analytics
```

depends directly on:

```text
IWorkManagementDbContext
ApplicationDbContext Work DbSets
WorkManagement Domain repositories/aggregates
```

The only allowed Work persistence reader for AR-REF-001 is the producer-owned
WorkManagement Application implementation of `IWorkItemProjectionSource`.

# 24C. TAC-GATE-013 — N8n retry false-green protection

A certification test for N8n retry must include:

```text
DeduplicationConsumeFilter<N8nDispatchRequestedV1>
```

or the exact production-equivalent filter pipeline.

Fail the evidence manifest if the only retry test:

```text
constructs N8nDispatchConsumer directly
and
manually calls SaveChangesAsync afterward
```

# 24D. TAC-GATE-014 — Automation MoveItem must have production caller

Fail if:

```text
IWorkActionPort.MoveItemAsync
```

is referenced only by:

```text
tests
adapter
ACL
```

and no production Automation Application action executor invokes it.

# 24E. TAC-GATE-015 — Billing capability/usage scope alignment

For `AUTOMATION_RULE`, the capability reader and usage writer must use the same
scope tuple:

```text
AccountId
WorkspaceId
CapabilityCode
```

Fail if capability is workspace-scoped while usage is aggregated globally
without an explicit Billing policy that says so.

# 24F. TAC-GATE-016 — No mandatory Real Flow remains a stub

For every mandatory flow entry point, fail if the production handler contains:

```text
throw new NotImplementedException()
```

unless the flow is explicitly `BLOCKED-DECISION` because accepted Product/Domain
semantics do not exist.

Pinned mandatory missing implementations expected to become real:

```text
ArchivePage
ConnectCalendar
DisconnectCalendar
HandleCalendarWebhook
```

Pinned non-bootstrap case:

```text
PublishPage
```

must not be treated as implemented merely to clear this gate.

# 24G. TAC-GATE-017 — Stub/log-only consumers cannot satisfy business reactions

Scan mandatory event-consumer evidence.

Fail when the only consumer proof is a type named/classified as:

```text
*StubConsumer*
logging-only
observability-only
```

Such consumers may exist, but the Real Flow manifest must not cite them as the
business reaction.

# 24H. TAC-GATE-018 — Identity cannot depend on private Accounts provisioning

Fail when Identity production source imports or injects:

```text
Features.Accounts.Provisioning.*
Features.Accounts.Abstractions.* mutation contracts
IAccountDbContext
Account aggregate
AccountMember aggregate
```

for `IA-FLOW-02`.

Required dependency:

```text
Accounts.Public provisioning action
```

# 24I. TAC-GATE-019 — Provider webhook auth cannot be user-session auth

For `AI-FLOW-07`, fail if provider webhook authenticity is represented only by:

```text
IAuthenticatedRequest
user/session token
```

Required architecture proof includes provider signature/timestamp/replay
verification before Integrations business processing.

# 24J. TAC-GATE-020 — No synthetic tenant in Collaboration Notification

Fail if `MentionCreatedNotificationConsumer` or successor materializes:

```text
AccountId = Guid.Empty
WorkspaceId = Guid.Empty
```

for a scoped business notification.

The tenant must come from producer-owned event facts or an approved authoritative
resolution path.

# 24K. TAC-GATE-021 — Paired-team completeness

Required pack assertions:

```text
IA → Identity + Accounts local baselines
WG → Workspaces + Governance
DC → Documents + Collaboration
AI → Automation + Integrations
```

A pack cannot be FROZEN if either owned BC lacks a verified local flow.

# 24L0. TAC-GATE-022 — Common public-signature semantic purity

Scan every public type/member under:

```text
Notrelix.Application.Common.*
```

Fail if public dependency graph includes BC-owned business types.

Inspect:

```text
parameters
return types
properties
fields
generic arguments
base/interface types
```

Pinned negative fixture/current debt:

```text
IAccessGrantProjectionService
must not expose AccountRole
must not expose WorkspaceRole
```

This gate certifies `TAC-FRZ-017`.

# 24L1. TAC-GATE-023 — Scoped Integration Event tenant envelope

Enumerate Integration Event contracts.

Fail any tenant-scoped business event where:

```text
WorkspaceId is present/meaningful
and
AccountId is absent/null by contract or producer mapping
```

Explicit Global/System events must be allowlisted by semantic classification,
not by filename.

Also inspect producer mapping for selected reference events so a declared
`AccountId` cannot be silently mapped to null.

This gate certifies the structural side of `TAC-FRZ-018`.

# 24L2. TAC-GATE-024 — Calendar semantic/persistence authority

Fail when Calendar production flow:

```text
creates Calendar capability without CalendarIntegration
uses CalendarIntegration without ConnectionId
treats CurrentSecretRef alone as durable persistence
uses outbound WebhookDelivery as inbound provider receipt
uses user-session auth as sole provider webhook authenticity
trusts tenant scope from unverified payload
```

Expected production concepts:

```text
IntegrationConnection
IntegrationSecretVersion
CalendarIntegration
Infrastructure inbound receipt/dedup
```

This gate certifies the structural side of `TAC-FRZ-019`.

# 24L3. TAC-GATE-025 — Billing capacity semantic/concurrency protections

Fail source/architecture when:

```text
AUTOMATION_RULE is called billable UsageMetric without product authority
numeric zero is hard-coded as unlimited without BILL-LIMIT-001
hard-capacity owner action has no concurrency strategy/owner state
consumer branches on private PlanTier
```

Behavior/integration concurrency remains mandatory; this gate alone is not
sufficient.

This gate certifies the structural side of `TAC-FRZ-020`.

# 24K1. PRE-M4 interaction-authority consistency proof

This is a **pre-implementation authority audit**, not a new milestone and not a
new CI lane.

Before M4 begins:

```text
all 47 Flow Cards are reviewed
every actual cross-BC edge has EdgeId
every edge mechanism ∈ TAC-XC-A..F
no system-level taxonomy ambiguity remains
```

Deep M4 assertions:

```text
IA-FLOW-01
→ Interactions[] empty

IA-FLOW-06
→ owner-local Accounts mutation; no fabricated cross-BC persistence edge

IA-FLOW-02:E1
→ TAC-XC-E
→ Identity → Accounts
→ BOUND-TX-004
→ extraction blocked

IA-FLOW-03:E1
→ TAC-XC-C
→ Identity → Workspaces
→ real outbox/Platform/dedup
→ eventual consumer reaction

IA-FLOW-04:E1
→ TAC-XC-A
→ Workspaces → Accounts
→ direct Public
→ no mandatory consumer Port

IA-FLOW-05:E1
→ TAC-XC-E
→ Workspaces → Accounts
→ BOUND-TX-002
→ extraction blocked
```

Later-pack deep runtime fields may remain explicit `DEFERRED-{MILESTONE}` and
do not block M4 unless they create a system-level contradiction.

# 24K2. PRE-M4 Source Operating Pattern proof

This proof protects the source copy-model without creating a new architecture
gate family.

## M3 prerequisites

`ARCH-BC-005` must include a self-test proving:

```text
own Producer.Public repository/mechanism contract
→ FAIL

own Producer.Public semantic fact/action contract
→ PASS
```

The result must be caused by repository/mechanism classification, not merely by
the Common/foreign-context classifier.

`ARCH-BC-008` must classify `AccessPermissionRule` exactly once:

```text
exact technical pipeline exception
OR
exact governed business-vocabulary debt
```

No contradictory commentary/baseline classification is allowed.

## Producer Public topology proof

Reuse the existing canonical filesystem/topology test owner
`CanonicalPathArchitectureTests`; do not create a competing scanner or CI lane.

Required rule for touched/normalized Public surfaces:

```text
Features/<BC>/Public/<PublishedCapability>/... → PASS

new top-level Public/Commands   → FAIL
new top-level Public/Queries    → FAIL
new top-level Public/Facts      → FAIL
new top-level Public/Actions    → FAIL
new top-level Public/Contracts  → FAIL
new top-level Public/DTOs       → FAIL
new top-level Public/Services   → FAIL
new top-level Public/Common     → FAIL
```

Existing exact legacy paths in not-yet-normalized BCs may be held in a reviewed
monotonic baseline only. The baseline must shrink in the same change that moves
a contract, and recreating a removed legacy path must fail. Do not infer business
meaning from arbitrary folder names; this rule only rejects the known technical
bucket taxonomy at the first level below `Public`.

For M4 specifically, assert:

```text
Accounts/Public/Membership/
Accounts/Public/PersonalAccountProvisioning/

exist as the canonical M4 producer surfaces, and no technical top-level bucket
remains under Accounts/Public.
```

## CrossContext composition proof

Extend an existing canonical-path/dependency architecture-test owner.

Assert:

```text
cross-context Port/adapter runtime registrations
→ dedicated CrossContextRegistration/AddCrossContextBindings owner

PersistenceRegistration
→ does not own WorkItemActionAdapter or other non-persistence runtime adapters
```

Persistence mappings remain allowed:

```text
IAccountDbContext → ApplicationDbContext
IWorkspaceDbContext → ApplicationDbContext
...
```

## Structural reference proof

Automation→Work structural reference must continue to prove:

```text
Automation Application
→ IWorkActionPort
→ consumer semantic mapping
→ WorkItemActionAdapter
→ WorkManagement.Public IWorkItemActions
```

Adapter negative assertions:

```text
no IWorkManagementDbContext
no Work Domain aggregate
no provider/transport business policy
```

This does not certify the full M8 Automation Pack.

## Explicit non-reference proof

The source audit/evidence must record:

```text
WorkManagementCollaborationReadAdapter
→ NON-REFERENCE until DEFERRED-M6-SOURCE-NORMALIZATION closes

WorkItemProjectionSourceAdapter
→ REFERENCE (M10 CLOSED-FROZEN TAC-XC-B) — runtime Adapter delegates to
  WorkManagement Application IWorkItemProjectionSource implementation; it
  never reads Work persistence itself

IdentityBootstrapReadAdapter
→ Supporting Composite Read only

*StubConsumer* / log-only consumer
→ cannot satisfy a mandatory Real Flow reaction
```

## Copy eligibility proof

A source may be named `canonical copy-model` only when:

```text
FlowId VERIFIED
+
substantive behavior evidence
+
runtime-owner proof
+
required architecture gates PASS
```

A structural reference may be cited only for the exact invariant it proves.

# 24L. TAC-IA-FLOW-01 — Identity UpdateProfile local baseline

Execute production `UpdateProfileCommand`.

Assert:

```text
authenticated current user
Identity persistence only
User.UpdateProfile semantics
commit
missing user failure
```

Architecture assertion:

```text
FlowKind = Local
Interactions[] = empty
no Accounts/Workspaces/Governance mutation dependency
no artificial Port/event/remote adapter introduced
```

# 24M. TAC-IA-FLOW-02 — Register→Accounts Public provisioning

Execute production `RegisterCommand` after migration.

Required chain:

```text
RegisterCommandHandler
→ Accounts.Public.PersonalAccountProvisioning action
→ Accounts owner implementation
```

Assert no private Accounts provisioning interface is injected by Identity.

Source-topology proof:

```text
IAccountProvisioningActions + its boundary request/result contracts
→ Accounts/Public/PersonalAccountProvisioning/
→ no duplicate under Accounts/Public/Commands|Actions|Contracts
```

Interaction contract proof:

```text
FlowId = IA-FLOW-02
EdgeId = E1
Mechanism = TAC-XC-E
SourceBC = Identity
TargetBC = Accounts
ContractOwner = Accounts
CurrentBinding = direct-in-process
ConsistencyModel = immediate
TransactionBoundary = BOUND-TX-004 current shared request transaction
SemanticBoundaryReady = true
RuntimeSubstitutionReady = false/deferred
ExtractionBlocker = true
```

Fail when implementation silently replaces this with:

```text
async event provisioning
remote HTTP command
separate commit + reconciliation
```

without reopening BOUND-TX-004.

## BOUND-TX-004 runtime proof

Cases:

```text
success → Identity User + personal Account + owner member commit
Accounts failure → Identity User rollback
request failure after Accounts mutation → no orphan Account
duplicate registration → no duplicate personal Account
event/outbox only on committed registration
```

Also assert the decision/evidence record contains:

```text
WorkflowOwner = Identity registration
IdentityMutationOwner = Identity
AccountMutationOwner = Accounts
Atomicity = current shared request transaction
ExtractionBlocker = true
RemovalTrigger is explicit
```

The test does not simulate distributed partial success while BOUND-TX-004 is
active.

# 24N. TAC-IA-FLOW-03 — RegistrationCompleted→Workspace provisioning

Interaction contract proof:

```text
FlowId = IA-FLOW-03
EdgeId = E1
Mechanism = TAC-XC-C
SourceBC = Identity
TargetBC = Workspaces
ContractOwner = Identity
CallerWaitsForOutcome = no
ConsistencyModel = eventual
CurrentBinding = platform-event-delivery
RemoteReadiness = broker-native
FutureDistributedBinding = broker-event
OutboxRequired = true
ConsumerDedupRequired = true
ExtractionBlocker = false
```

Execute through real delivery runtime:

```text
Identity registration mutation
→ IdentityRegistrationCompletedIntegrationEventV1
→ producer outbox
→ producer commit
→ Platform delivery
→ tenant restore
→ dedup
→ WorkspaceProvisioningConsumer
→ Workspaces Application/owner mutation
→ consumer-local commit
```

Assert:

```text
Workspaces mutation only
scope restored
duplicate event idempotent
retry behavior correct
no Identity/Accounts DbContext in consumer
consumer is an inbound adapter, not the business-logic owner
```

Independence proof:

```text
producer registration commit succeeds
→ later Workspace consumer attempt fails technically
→ committed Identity/Accounts registration state remains committed
→ delivery can retry according to Platform policy
```

The test must not create an alternate teaching broker or manually bypass the
production outbox/dedup path.

# 24O. TAC-IA-FLOW-06 — Accounts RenameAccount local-admin baseline

Execute the production Accounts local command selected by IA-FLOW-06.

Required entry:

```text
RenameAccountCommand
```

or the exact normalized name from implementation.

## Behavior

Cases:

```text
authorized Account admin → rename succeeds
unauthorized actor → denied before mutation
wrong Account scope → rejected
invalid name → Domain/Application error
same current name → canonical no-op/idempotent behavior
```

## Persistence/concurrency

Use real Accounts persistence.

Required race:

```text
two concurrent conflicting rename updates
→ repository's version/concurrency contract produces deterministic result
→ no lost update
```

## Boundary

Assert:

```text
Accounts DbContext only
no Identity/Workspace private persistence
no Common AccountRole/WorkspaceRole signature dependency
```

If an outward AccountRenamed fact is introduced, it must satisfy the scoped
event tenant-envelope policy applicable to its scope.

This is the explicit Accounts local-admin proof; cross-BC provisioning alone
cannot satisfy Accounts local-baseline certification.

Interaction authority assertion:

```text
FlowKind = Local
primary owner = Accounts
no Accounts persistence is exposed through a foreign interaction edge
standard Account-admin authorization reuses canonical authority/pipeline
```

Do not fail this local-baseline test merely because authorization semantics are
owned by Governance; pipeline-owned authorization is not a fabricated foreign
persistence mechanism.

# 25. TAC-IA-001 — Identity Public fact exists

Assert real production contract/provider:

```text
IIdentityUserFacts
IdentityUserFact
```

or the finalized renamed equivalents.

Must live under Identity producer Public.

---

# 26. TAC-IA-002 — Identity fact behavior

Required mapping tests for current semantic scope.

At current reference baseline, cover:

```text
user missing
Active
PendingVerification
Inactive
Suspended
email
email-confirmed state
```

If semantic contract changes:

```text
update expected cases to frozen meaning
```

Must prove Workspaces/Billing/Governance state does not become hidden input to
Identity lifecycle fact.

---

# 27. TAC-IA-003 — Accounts Public fact exists

Assert real producer Public contract/provider.

Current candidate:

```text
IAccountMembershipFacts
AccountMembershipAdmissionFact
```

---

## IA-FLOW-04 interaction classification

The selected membership/admission fact is the M4 `TAC-XC-A` teaching case.

Assert:

```text
FlowId = IA-FLOW-04
EdgeId = E1
Mechanism = TAC-XC-A
SourceBC = Workspaces
TargetBC = Accounts
ContractOwner = Accounts
CurrentBinding = direct-in-process
CallerWaitsForOutcome = yes
RemoteReadiness = semantic-only
```

A Workspaces-owned consumer Port is **not required** for this reference.

Negative architecture proof:

```text
Workspaces directly uses Accounts DbContext/aggregate
→ FAIL

Workspaces uses stable Accounts.Public fact
→ PASS

test invents fake remote adapter/HTTP service solely for service-readiness
→ not required evidence
```

# 28. TAC-IA-004 — Accounts admission behavior

Cover current/final Account-owned lifecycle states.

At audit design baseline:

```text
missing account
Active
Trialing
Suspended
Closed
```

Exact statuses at implementation SHA are authoritative.

Prove selected fact does not imply:

```text
Billing entitlement
Governance permission
Workspace invitation validity
Identity state
```

---

# 29. TAC-IA-005 — Accounts target action exists

Required after M2.

Assert the production Accounts membership action exists under the pinned
Accounts Public producer-owned boundary.

Reject:

```text
Workspaces using Accounts.Abstractions as foreign public API
```

for the migrated target mutation.

---

## IA-FLOW-05 interaction classification

Assert:

```text
FlowId = IA-FLOW-05
EdgeId = E1
Mechanism = TAC-XC-E
SourceBC = Workspaces
TargetBC = Accounts
ContractOwner = Accounts
CurrentBinding = direct/in-process Accounts.Public action
ConsistencyModel = immediate
TransactionBoundary = BOUND-TX-002 current shared request transaction
SemanticBoundaryReady = true
RuntimeSubstitutionReady = false/deferred
ExtractionBlocker = true
```

A separate remote-command test does not satisfy this reference while
BOUND-TX-002 remains authoritative.

# 30. TAC-IA-006 — Accounts target mutation behavior

Required:

```text
valid request
already-member/idempotent request
invalid Account state
missing Account
concurrent duplicate
```

plus transaction branch behavior.

---

# 31. TAC-IA-007 — Same-team boundary negative proof

Architecture test must fail a fixture equivalent to:

```text
Workspaces Handler
→ Accounts.Abstractions.IAccountDbContext
```

and:

```text
Identity Handler
→ Accounts Domain aggregate
```

Same delivery team is not an exception.

---

# 32. TAC-IA-008 — IA pack DI

Resolve:

```text
Identity Public provider
Accounts Public fact provider
Accounts target action implementation
Workspace provisioning event consumer dependencies
```

through production DI registration.

For `IA-FLOW-04` TAC-XC-A, no Workspaces-owned Port is mandatory.
If another IA edge explicitly selects TAC-XC-B, its consumer Port/adapter must
resolve through production DI.

---

# 33. TAC-WG-001 — AcceptInvitation behavior matrix

Required cases:

```text
identity missing
identity unusable
email unconfirmed if required
invitation missing
invitation expired
invitation non-pending
email mismatch
workspace missing
workspace invalid lifecycle
account missing
account ineligible
already workspace member
new member success
```

The exact current product semantics determine expected result codes.

---

# 34. TAC-WG-002 — Decision independence

Mutation handler must keep distinct checks for:

```text
Identity eligibility
Account eligibility
Invitation validity
Workspace lifecycle
```

Do not freeze implementation statements.

Behavior tests should vary one producer fact at a time and verify correct
decision.

This catches semantic over-broad Public booleans.

---

# 35. TAC-WG-003 — Governance authoritative contract/path exists

Required production authority.

Default candidate:

```text
existing canonical authorization pipeline
+ Governance-owned permission semantics
```

A separate `Governance.Public` contract is required only when a real
use-case-specific cross-context consumer needs an authoritative Governance
fact/query outside the standard pipeline.

Whether pipeline-backed or Public, the semantic boundary must not leak:

```text
private role enum
policy aggregate
Governance DbContext
protected resource aggregate
```

The test must also prove there is no second evaluator/decision-store/
authorization-behavior stack.
# 36. TAC-WG-004 — Governance authorization behavior

At minimum:

```text
allowed
denied
unknown/missing principal
missing resource/scope semantics as applicable
```

If standard pipeline uses the contract:

```text
pipeline behavior test
```

If use-case-specific query consumes it:

```text
handler behavior test
```

---

# 37. TAC-WG-005 — Pipeline versus explicit Governance reference

Architecture/reference test must prove:

```text
CreateBoard standard permission
→ pipeline marker/behavior
```

and at least one legitimate explicit Governance Public consumer exists only
where use-case semantics require it.

Do not require every handler to inject Governance.

---

# 38. TAC-WG-006 — Existing Workspace membership Integration Event is reused

Required source disposition:

```text
REUSE/HARDEN-EXISTING
```

At c409 the producer already has:

```text
WorkspaceMemberAddedIntegrationEvent
WorkspaceMemberRemovedIntegrationEvent
WorkspaceEventMapper
```

Tests must prove the selected existing contract:

```text
producer = Workspaces
version/registry valid
outbox enrollment valid
consumer path executable
```

A second membership event with equivalent semantics fails
`TAC-MIG-006`.
# 39. TAC-WG-007 — Membership event emitted only after valid mutation

Behavior/integration tests:

```text
successful membership change → event enrolled
failed validation → no event
failed transaction → no committed outward delivery
```

Use current outbox semantics.

---

# 40. TAC-WG-008 — Governance reference execution path

Required at least one executable path proving Governance composes
source-owned facts while preserving their ownership.

Valid examples:

```text
AuthorizationBehavior
→ canonical permission evaluator
→ source-owned membership/resource/capability facts

or

use-case-specific consumer
→ Governance Public fact/query
```

If membership facts participate:

```text
Workspaces remains membership owner
```

If Billing capability participates:

```text
Billing remains capability owner
```

The reference does not require Governance to consume a membership event if the
canonical authorization path is synchronous/fact-based.
# 41. TAC-WG-009 — WG pack DI/runtime

Resolve the selected canonical Governance runtime:

```text
authorization behavior/evaluator/decision-store path
source-fact providers
optional Governance Public implementation if WG-REF-002 selected one
membership event consumer only if the chosen reference reaction uses it
```

and all required handlers.

Do not create DI registrations solely to manufacture a second authorization
stack.
# 41A. TAC-WG-FLOW-01 — Workspaces CreateWorkspace production flow

Execute production `CreateWorkspaceCommand`.

Required assertions:

```text
trusted Account scope
verified-email policy
canonical CreateWorkspace authorization
Workspace + owner WorkspaceMember commit
grant projection follows selected owner/mechanism
slug uniqueness protected by DB authority
```

Race:

```text
same slug requested concurrently
→ at most one authoritative workspace identity
```

If the flow uses the grant-projection seam touched by `TAC-FRZ-017`, assert no
Common signature leaks `AccountRole`/`WorkspaceRole`.

# 41B. TAC-WG-FLOW-02 — AcceptInvitation cross-BC workflow

Use the production `AcceptInvitation` path.

Required:

```text
Identity Public facts
Accounts Public facts/action
Invitation.Accept
WorkspaceMember
grant projection
BOUND-TX-002
```

Cases:

```text
success
expired/invalid token
already member
Accounts mutation failure
Workspace failure
duplicate retry
```

Run rollback proof at the shared request transaction owner.

# 41C. TAC-WG-FLOW-03 — WorkspaceMemberAdded→Activity

Run real producer/outbox/filter/dedup/consumer chain.

Assert:

```text
WorkspaceMemberAdded event has authoritative AccountId + WorkspaceId
TenantContextConsumeFilter restores tenant, not System
Activity consumer writes Collaboration-owned projection
duplicate event does not duplicate logical Activity
```

# 41D. TAC-WG-FLOW-04 — GrantResourcePermission variants

Run production Governance mutation independently for:

```text
work-management.board
documents.page
```

Each case proves:

```text
ResourceKind
PermissionAction
Account/Workspace scope
granter authority
subject semantics
audit/replacement behavior
```

A guessed Page permission action fails.

# 41D2. TAC-WG-FLOW-04 — RevokeResourcePermission authorization (M5 Wave 5)

Execute production Governance revoke through the canonical pipeline on real
PostgreSQL (`GovernanceResourcePermissionFlowTests`, region 41D2).

Required cases:

```text
Revoke_OnPage_ByOwner_RemovesActiveRowAndAudits
Revoke_PageManagerRank_RevokeEqualOrLowerTargetRank_Allows
Revoke_AuthorityBelowTargetRank_IsForbidden_AndRowStaysActive
Revoke_SubjectWithoutManagementAuthority_IsForbidden_EvenOnOwnRankRow
Revoke_UnknownPermission_IsNotFound
Revoke_ForeignAccountTarget_IsNotFound_AndForeignRowStaysActive
```

Each denial asserts the permission row stays active (no partial mutation);
the owner path asserts the soft-deleted row and the audit record. Being the
subject of a row is not management authority; a foreign-account target is
hidden (NotFound), never Forbidden.

# 41E. TAC-WG-FLOW-05 — GetResourcePermissions

Execute real Governance query.

Assert:

```text
authorized read
Governance-owned state only
correct resource/workspace scope
no target aggregate/DbContext join
```

# 41F. TAC-WG-FLOW-06 — Canonical authorization pipeline

Required runtime cases:

```text
MoveBoardItem allow/deny
CreatePage allow/deny
CreateComment.ForBoardItem allow/deny
CreateComment.ForPage allow/deny
Automation→Work background actor allow/deny
```

All must use the one canonical authorization path.

No second permission evaluator may be introduced.

# 42. TAC-WM-001 — CreateBoard local reference

Assert request uses current required pipeline markers.

Behavior tests remain green.

Architecture assertion:

```text
handler does not inject foreign Workspace/Governance/Billing dependency
for standard pipeline-owned concerns
```

unless a new real use-case-specific fact is separately classified.

---

# 43. TAC-WM-002 — Collaboration consumer Port exists

Current:

```text
IWorkManagementCollaborationReadPort
```

Assert consumer ownership and canonical path.

---

# 44. TAC-WM-003 — Collaboration read adapter exists

Before M6 implementation, resolve the revised SPEC decision:

```text
WM-FLOW-02:E1
DecisionStatus = DEFERRED-M6-SOURCE-NORMALIZATION
Mechanism = TAC-XC-B
```

The final production proof must show:

```text
WorkManagement consumer Port
→ WorkManagement-owned pure ACL if needed
→ Infrastructure adapter
→ Collaboration-owned stable semantic read boundary
```

Fail if the only implementation is:

```text
consumer Port
→ adapter
→ foreign Collaboration DbContext treated as public cross-BC API
```

Assert Infrastructure implementation:

```text
consumer = WorkManagement
producer/purpose = Collaboration
```

No Work handler foreign DbContext.

DI resolves Port→adapter.

---

# 45. TAC-WM-004 — Collaboration aggregate read behavior

Required cases:

```text
empty item IDs
missing IDs
comment counts
attachment counts
soft-delete semantics
resource kind filtering
multi-item aggregation
```

Reuse existing tests.

---

# 46. TAC-WM-005 — MoveBoardItem Public target action exists

Required producer surface:

```text
IWorkItemActions.MoveItemAsync
```

Required producer-local implementation:

```text
MoveBoardItem use case/service
```

Architecture assertions:

```text
Public contract has no MediatR/DbContext/Domain aggregate/provider type
Public implementation uses WorkspaceId
Public implementation uses ExecutorUserId
Public implementation uses OperationId
```

The last three are mandatory semantic fields, not metadata.
# 47. TAC-WM-006 — Work target scope/auth/idempotency behavior

Required tests against the production target action:

## Scope

```text
item.WorkspaceId == request.WorkspaceId
→ may proceed

item.WorkspaceId != request.WorkspaceId
→ fail before mutation
```

## Authorization

```text
authorized ExecutorUserId
→ may proceed

unauthorized ExecutorUserId
→ denied
→ Work state unchanged
```

Use the canonical target authorization implementation; do not mock the
authorization result in the only integration test.

## Idempotency

```text
OperationId O + Move(Item A, Group B)
→ succeeds once

OperationId O + same request again
→ same logical result
→ no second Work mutation/event

OperationId O + Move(Item A, Group C)
→ deterministic conflict
```

## Business rejection

```text
missing item
wrong target group/board
```

remain target-owned business failures.

## Atomicity

Target idempotency record and Work mutation must commit/rollback together.
# 48. TAC-WM-007 — Work Integration Event reference is pinned

Required contract roles:

```text
Automation trigger
→ migrated WorkManagement-owned BoardItemMemberAssignedIntegrationEvent V1

Analytics live placement
→ existing BoardItemMovedIntegrationEvent

Analytics lifecycle reconciliation
→ existing BoardItemCreatedIntegrationEvent
→ existing BoardItemArchivedIntegrationEvent
```

Tests must prove:

```text
producer ownership
version/registry
contract purity
no generic duplicate WorkChanged contract
old Automation-owned member-assigned contract retired
```

No event is selected during implementation.
# 49. TAC-WM-008 — Domain Event → Integration Event mapping

Assert explicit mapping according to repository architecture.

Negative:

```text
raw mutable Work aggregate serialized outward
```

must fail.

---

# 50. TAC-WM-009 — Outbox enrollment

Integration test:

```text
valid Work mutation
→ Work transaction commit
→ exactly one outward event enrollment as semantics require
```

Failed Work mutation:

```text
→ no committed outward event
```

---

# 51. TAC-WM-010 — WM pack reference completeness

Required source/test evidence:

```text
WM-REF-001 local slice
WM-REF-002 Port+adapter
WM-REF-003 target action
WM-REF-004 Integration Event
```

Missing any → FAIL.

---

# 52. TAC-DC-001 — Existing canonical ResourceRef is reused

Assert the reference uses:

```text
Notrelix.Domain.SharedKernel.ResourceRef
```

or its intentional migrated successor.

Current Collaboration entities already use this type.

A second ResourceRef created solely for TAC fails.

Reference behavior must prove stable target identity without ownership
transfer.
# 53. TAC-DC-002 — CreateComment.ForBoardItem ResourceRef behavior

Execute:

```text
CreateCommentCommand.ForBoardItem(boardItemId, content, parentCommentId)
```

Assert the created Collaboration Comment stores the canonical SharedKernel
ResourceRef with:

```text
ResourceKind = work-management.board-item
ResourceId = boardItemId
WorkspaceId = current Workspace
```

Assert the Collaboration handler does not load/mutate WorkManagement aggregate
or persistence.
# 54. TAC-DC-003 — Pinned Collaboration mutation remains local

Pinned mutation:

```text
CreateCommentCommand.ForBoardItem
```

PASS requires:

```text
Comment authority = Collaboration
target identity = ResourceRef
target Work state unchanged
parent-comment validation remains Collaboration-local
```

Thread/Mention/Activity cannot substitute for this bootstrap reference.
# 55. TAC-DC-004 — Target existence behavior

When product semantics require target existence:

```text
existing target → continue
missing target → defined failure
```

If historical/unresolved reference is allowed:

```text
test that semantic instead
```

No guessed behavior.

---

# 56. TAC-DC-005 — Target authorization behavior

When mutation requires protected target:

```text
authorized → success
unauthorized → fail
```

Authorization must be separate from ResourceRef.

---

# 57. TAC-DC-006 — Wrong-scope behavior

Where Workspace/account scope applies:

```text
cross-scope target reference → fail
```

unless product explicitly permits cross-scope linking.

---

# 58. TAC-DC-007 — Mandatory ACL reference

The mandatory ACL is not created in Collaboration.

It is pinned to AI-REF-002:

```text
Automation MoveItem semantic request
→ pure Automation-owned ACL mapper
→ WorkManagement Public MoveItem request
```

Assert mapper has no EF/HttpClient/provider SDK/MediatR dependencies and performs
only semantic translation/validation.
# 59. TAC-DC-008 — Notifications semantic classification

Architecture evidence/test must map production `Features/Notifications` to the
accepted semantic owner.

Final allowed outcomes:

```text
Collaboration-owned module
migrated under Collaboration
exact compatibility debt
```

Forbidden:

```text
implicit twelfth BC
```

---

# 60. TAC-DC-009 — No cross-BC ORM navigation

Reuse ARCH-BC-004.

Add reference-specific negative fixture if current gate does not cover:

```text
Comment.TargetDocument navigation
Comment.TargetBoardItem navigation
```

---

# 60A. TAC-DC-FLOW-01 — Documents CreatePage

Execute production `CreatePageCommand`.

Assert:

```text
trusted Account/Workspace
canonical Page authorization
Page.Create
Documents persistence
Domain Event
outbox where mapped
```

If PageCreated is Workspace-scoped:

```text
AccountId + WorkspaceId must both be authoritative
TenantContextConsumeFilter must restore tenant on delivery
```

# 60B. TAC-DC-FLOW-02 — Documents ArchivePage

Mandatory after `IMPLEMENT-MISSING`.

Execute production `ArchivePageCommand`.

Assert:

```text
resource authorization
Documents Page load
Page.Archive(...)
PageArchivedDomainEvent
commit/outbox
```

Cases:

```text
not found
wrong tenant
unauthorized
already archived
DB failure
outbox failure
```

`PublishPage` is not used as substitute evidence.

# 60C. TAC-DC-FLOW-03 — CreateComment.ForBoardItem

Independent BoardItem resource proof:

```text
ResourceKind = work-management.board-item
Work target exists
Work-specific auth
Collaboration owns Comment mutation
no Work aggregate mutation/navigation
```

# 60D. TAC-DC-FLOW-04 — CreateComment.ForPage

Independent Page resource proof:

```text
ResourceKind = documents.page
Documents target exists
Page-specific auth
Collaboration owns Comment mutation
no Documents aggregate mutation/navigation
```

BoardItem tests cannot satisfy this section.

# 60E. TAC-DC-FLOW-05 — CommentCreated→Activity

Run real delivery runtime.

Assert:

```text
CommentCreated has authoritative AccountId + WorkspaceId
tenant restored correctly
Activity projection persisted
source EventId/idempotency retained
duplicate does not duplicate logical Activity
local Activity read returns materialized result
```

# 60F. TAC-DC-FLOW-06 — MentionCreated→Notification actor + tenant

Producer-side required facts:

```text
AccountId
WorkspaceId
MentionId
MentionedUserId
MentionedByUserId / CreatedByUserId
TargetType
TargetId
```

Critical negative assertion:

```text
MentionedByUserId != MentionedUserId
```

unless the same human intentionally mentions themself in the specific test.

The test must construct distinct users and prove exact mapping.

Forbidden production mapping:

```text
MentionedByUserId = MentionedId
```

Run event through `TenantContextConsumeFilter` + dedup + Notification consumer.

Assert:

```text
Notification.AccountId exact
Recipient.AccountId exact
actor exact
no Guid.Empty
duplicate event idempotent
cross-tenant isolation
```

# 60G. TAC-DC-FLOW-07 — Documents outward event producer + stub exclusion

Assert real producer mapping/outbox for:

```text
PageCreated
PageArchived
```

and `TAC-GATE-023` tenant envelope.

Any Documents `*StubConsumer*`/logging-only consumer is explicitly excluded from
business-flow evidence.

The traceability manifest may only cite a real business consumer when one is
required by a Flow Card.

# 61. TAC-AI-001 — Work event consumer exists in Automation

Mandatory.

Assert real consumer registration/source for:

```text
WorkManagement IntegrationEvent V1
```

---

# 62. TAC-AI-002 — Automation event consumer creates/finds execution

Behavior:

```text
first delivery → execution/progression starts
duplicate delivery → no duplicate execution
```

Execution identity must be stable.

---

# 63. TAC-AI-003 — Existing AutomationExecution is the process state reference

Required disposition:

```text
REUSE/HARDEN-EXISTING
```

Current source already has `AutomationExecution`,
`AutomationExecutionStep` and `AutomationExecutionStatus`.

Tests must prove the reference process uses that durable model rather than a
parallel teaching aggregate.

Only a separately approved ownership decision may introduce another workflow
aggregate.
# 64. TAC-AI-004 — Automation process + real N8n transaction semantics

Required source:

```text
AutomationExecution
N8nDispatchUseCase
N8nDispatchConsumer
DeduplicationConsumeFilter
```

Architecture assertions:

```text
consumer has no IAutomationDbContext
consumer does not drive AutomationExecution lifecycle
Application use case owns lifecycle
Domain has no broker/redelivery vocabulary
```

## Retryable failure — real runtime proof

Execute:

```text
DeduplicationConsumeFilter<N8nDispatchRequestedV1>
→ N8nDispatchConsumer
→ N8nDispatchUseCase
→ RetryableFailure
```

Assert after the thrown retryable exception and transaction rollback:

```text
dedup success is not committed
Automation execution does not falsely persist retry AttemptCount/evidence
terminal success/failure is not committed
```

Then execute a later successful delivery and assert:

```text
execution succeeds once
dedup success commits once
```

## Terminal failure

Execute through the same filter.

Assert:

```text
AutomationExecution terminal failure commits
consumer does not throw delivery retry
dedup success commits
```

## Unknown outcome

Execute through the same filter.

Assert:

```text
unknown outcome enters configured durable reconciliation/terminal state
consumer returns
delivery is not blindly retried
```

Direct consumer tests remain supplementary only.
# 65. TAC-AI-005 — Process state transitions

Required:

```text
initial → running
running → completed
running → failed
```

Invalid:

```text
completed → running
failed terminal → running
```

unless product explicitly supports retry as a new execution.

---

# 66. TAC-AI-006 — Duplicate source event

Same Work event twice:

```text
one semantic Automation execution
```

or another explicitly idempotent equivalent.

---

# 67. TAC-AI-007 — Target business failure

Work action returns producer business failure.

Automation Process must:

```text
record/transition according to Automation semantics
```

Platform must not reinterpret it as transport failure.

---

# 68. TAC-AI-008 — Technical transport failure

Where adapter/runtime simulates technical failure:

```text
technical retry policy belongs to runtime/Platform
```

Process may remain running/pending according to semantics.

Test must distinguish this from Work business rejection.

---

# 69. TAC-AI-009 — Exact Automation MoveItem Port exists

Required:

```text
IWorkActionPort.MoveItemAsync
```

with:

```text
itemId
targetGroupId
executionId
AutomationPrincipal
```

Architecture proof:

```text
production Automation Application executor references the Port
```

not only tests/adapters.

The executor must live in Automation Application semantics and be reachable from
the real Automation execution path.
# 70. TAC-AI-010 — Work adapter exists and remains transport/policy-thin

Required:

```text
Automation IWorkActionPort
→ Infrastructure WorkItemActionAdapter
→ WorkManagement Public IWorkItemActions
```

Adapter must not:

```text
use Work DbContext
use Work Domain aggregate
perform authorization
own idempotency
own Work business rules
```

Those responsibilities belong to WorkManagement target Application.
# 71. TAC-AI-011 — Exact Automation→Work ACL mapping

The normalized interaction topology is:

```text
Automation Application executor
→ IWorkActionPort
→ Automation-owned pure ACL request mapping
→ Infrastructure WorkItemActionAdapter
→ WorkManagement.Public IWorkItemActions
```

The ACL may be represented by an explicit mapper/type or by an equally pure
Application mapping seam, but it cannot be owned by Infrastructure transport.

Pure mapper:

```text
Automation MoveItem
→ WorkManagement Public MoveItem
```

Assert exact semantic mapping:

```text
executionId → OperationId
automationPrincipal.WorkspaceId → WorkspaceId
automationPrincipal.ExecutorUserId → ExecutorUserId
itemId → ItemId
targetGroupId → NewGroupId
```

Then integration proof must verify the target actually enforces all mapped
fields.

A mapper test alone does not satisfy AI-REF-002.
# 71A. TAC-AI-011A — Production Automation MoveItem executor

Required production Application type invokes:

```text
IWorkActionPort.MoveItemAsync
```

from the real Automation action execution path.

Behavior tests:

```text
valid MoveItem config
→ parses ItemId + GroupId
→ ExecutionId used as OperationId
→ actor/workspace propagated
→ target action invoked

invalid/missing config
→ terminal configuration/business failure
→ target action not invoked

target business rejection
→ Automation execution terminal business failure

technical target failure
→ technical failure path
→ not rewritten as business rejection
```

Integration test entry point must start at this executor or a higher production
Automation process entry, not at `IWorkActionPort`.

# 72. TAC-AI-012 — Background actor security

Required tests identify:

```text
execution principal
workspace/account scope
authorization path
```

Negative cases:

```text
missing actor context
wrong workspace
revoked/denied capability as applicable
```

No implicit service bypass.

---

# 73. TAC-AI-013 — Automation target action correlation

Assert correlation/causation or current equivalent can trace:

```text
source Work event
→ Automation execution
→ target Work action
```

Do not require a new observability subsystem.

---

# 74. TAC-AI-014 — Exact N8n provider boundary

Required chain:

```text
Automation N8nDispatchUseCase
→ Integrations.Public IN8nWebhookActions
→ Features/Integrations/Ports/Providers/IN8nClient
→ Infrastructure N8nClient
```

Assert:

```text
old Application/Common/Integrations/N8n/IN8nClient is no longer authoritative
one N8n HTTP adapter implementation family
Public result contains no transport types
```

Required semantic outcomes:

```text
Succeeded
RetryableFailure
TerminalFailure
UnknownOutcome
```
# 75. TAC-AI-015 — Provider adapter exists in Infrastructure

Required real adapter for selected Calendar/N8n/provider seam.

Application must not contain HttpClient/provider SDK.

---

# 76. TAC-AI-016 — Provider success behavior

Test:

```text
valid semantic request
→ provider adapter
→ translated semantic success
```

Use fake/test server/mock provider boundary in tests, not production fake.

---

# 77. TAC-AI-017 — Provider technical failure

Test mapping for:

```text
timeout/network/provider unavailable
```

Expected error must follow current repository technical failure model.

---

# 78. TAC-AI-018 — Provider configuration/credential failure

Where relevant:

```text
missing/invalid configuration
invalid credentials
```

must be distinguishable from target business failure.

---

# 79. TAC-AI-019 — N8n webhookPath

Behavior freeze.

Exact expected path from current implementation is captured and tested.

---

# 80. TAC-AI-020 — N8n webhook_path

Same.

---

# 81. TAC-AI-021 — N8n whitespace

Test exact trim/failure semantics.

---

# 82. TAC-AI-022 — N8n leading slash

Test exact normalization semantics.

---

# 83. TAC-AI-023 — N8n invalid JSON

Test exact safe failure behavior.

---

# 84. TAC-AI-024 — N8n missing property

Test exact behavior.

---

# 85. TAC-AI-025 — N8n non-string property

Test exact behavior.

---

# 86. TAC-AI-026 — AI pack completeness

Mandatory:

```text
Work event consumer
Automation state
Process Manager
Work Port
Work adapter
background security
provider Port
provider adapter
N8n behavior proof
```

Missing any mandatory item → FAIL.

---

# 86A. TAC-AI-FLOW-01 — CreateAutomationRule local baseline

Execute production create rule flow.

Assert:

```text
Automation owns Rule mutation
Governance auth
Billing capability checked
Billing usage consistency model invoked
request idempotency
no PlanTier branch
```

# 86B. TAC-AI-FLOW-02 — Work event→AutomationExecution

Run real Work event delivery.

Cases:

```text
matching rule → execution
no matching rule → no execution
duplicate source event → one execution
restart/retry → stable execution identity
scope/actor preserved
```

# 86C. TAC-AI-FLOW-03 — Production Automation→Work

Entry point must be production action executor, not Port directly.

Assert:

```text
ExecutionId→OperationId
ExecutorUserId propagated
WorkspaceId propagated
target auth enforced
target idempotency enforced
business rejection mapped terminal
technical failure remains technical
restart after target success does not duplicate move
```

# 86D. TAC-AI-FLOW-04 — N8n runtime

Keep v2.6 real `DeduplicationConsumeFilter` proof.

Cases:

```text
success
terminal failure
retryable technical failure
unknown outcome
invalid config
```

# 86E. TAC-AI-FLOW-05 — Calendar Connection + secret version + binding

Execute production ConnectCalendar flow.

Required persisted graph:

```text
IntegrationConnection
IntegrationSecretVersion
CalendarIntegration(ConnectionId)
```

## Happy path

Assert:

```text
trusted Account/Workspace
canonical integration-management authorization
raw provider credentials pass only to secret-store Port
SecretRef/version returned
IntegrationSecretVersion persists SecretReference
IntegrationConnection current version/pointer persisted
CalendarIntegration persists WorkspaceId + ConnectionId + provider + SyncDirection
```

## Round-trip proof

Dispose/recreate DbContext and reload.

Assert the same semantic relationship remains.

A test that only inspects in-memory `CurrentSecretRef` fails certification.

## Duplicate identity

Use stable provider relationship identity.

Cases:

```text
duplicate OAuth callback
same provider account/tenant
same Workspace Calendar binding
```

must not create accidental duplicate logical Connection/binding.

## Split failure

Force:

```text
secret provider stores secret
DB transaction fails
```

Assert explicit cleanup/reconciliation outcome.

## Secret leakage

Assert raw access/refresh credential value is absent from:

```text
Domain persisted fields
Integration Events
Activity/Analytics payloads
captured application logs
```

This section certifies AI-FLOW-05 and part of TAC-FRZ-019.
# 86F. TAC-AI-FLOW-06 — CalendarIntegration disconnect lifecycle

Entry resource is:

```text
integrations.calendar-integration
```

Required first owner mutation:

```text
CalendarIntegration.Deactivate(...)
```

Cases:

```text
success
not found
wrong Workspace
unauthorized
already inactive
```

## CAL-CONN-001 policy

Create scenarios:

```text
one Calendar binding removed while another active binding still references
the generic Connection

last relevant Calendar binding removed
```

Assert implementation follows the frozen policy exactly:

```text
retain Connection
or
disconnect/revoke Connection under approved condition
```

A test that always calls `IntegrationConnection.Disconnect` fails.

Provider/secret cleanup cases:

```text
success
retryable failure
terminal failure
unknown outcome
retry after unknown
```

No distributed atomicity assertion is allowed.

This section certifies AI-FLOW-06 and part of TAC-FRZ-019.
# 86G. TAC-AI-FLOW-07 — Verified inbound Calendar webhook

Test through the real raw HTTP/provider verification boundary.

Required chain:

```text
raw request
→ size/content bounds
→ signature/secret verification
→ timestamp/replay verification
→ trusted IntegrationConnection
→ active CalendarIntegration
→ trusted AccountId/WorkspaceId
→ Infrastructure inbound receipt/dedup
→ provider-neutral Integrations Application input
→ exact target-context effect
→ semantic success condition
```

Until the target decision exists, the only valid processing outcome is the
explicit terminal `Blocked` state. A passing intake/receipt test is not a
semantic success proof.

Cases:

```text
valid signature
invalid signature
expired timestamp
replay
unknown Connection
inactive/missing CalendarIntegration
oversized payload
malformed payload
duplicate provider delivery
payload attempts wrong AccountId/WorkspaceId
```

Assert:

```text
untrusted tenant fields cannot select tenant
user-session auth is not provider authenticity
outbound WebhookDelivery is not used as inbound receipt state
```

If implementation keeps `InboundWebhookEvent` as Domain state, the test suite
must also prove its user-facing lifecycle and removal of LegacyGap
classification. Otherwise the receipt remains Infrastructure technical state.

This section certifies AI-FLOW-07 and completes TAC-FRZ-019 behavior proof.
# 87. TAC-BI-001 — Billing capability surface + scope semantics

Required producer surface:

```text
IBillingCapabilityFacts
BillingCapabilityFact
BillingCapabilityCode.AUTOMATION_RULE
```

Required entitlement tests:

```text
Account entitlement + same Account → applicable
Workspace entitlement + same Workspace → applicable
Workspace A entitlement + request Workspace B → not applicable
expired/revoked/disabled entitlement → unavailable
no entitlement → unavailable
```

Precedence matrix (workspace-targeted beats account-scoped, deterministically):

```text
Workspace entitlement + Account entitlement, same feature -> Workspace wins
multiple grants same scope -> newest CreatedAt wins; Id DESC breaks ties
expired Workspace grant + valid Account grant -> Account applies
  (expiry is filtered BEFORE precedence; the valid Account must not be shadowed)
Workspace grant for A vs B -> only A consumes it; ledger usage isolated
  across workspaces (Used for A excludes B's ledger)
```

Used/Remaining authority matrix (Used = ledger sum in every state):

```text
finite + used 2/5, request 1 -> Available, Limit 5, Used 2, Remaining 3
zero numeric limit with non-zero ledger -> Available=false, Limit 0,
  Used = ledger sum, Remaining 0
unlimited (IsUnlimited) with non-zero ledger -> Available, Limit NULL,
  Used = ledger sum, Remaining NULL
missing entitlement -> Available=false, Limit NULL, Used = ledger sum,
  Remaining NULL (and the owner action still fails closed to zero ceiling)
```

If both Account and Workspace entitlements are applicable, test the exact
Billing-owned precedence/combination rule.

An unordered/arbitrary first-row choice fails.
# 88. TAC-BI-002 — Billing Public purity

Reuse ARCH-BC-005.

Forbidden signature dependencies:

```text
Billing Domain aggregate
provider SDK DTO
Application/Common legacy entitlement enum unless exact technical primitive
Governance internal type
consumer internal type
```

---

# 89. TAC-BI-003 — Capability allowed behavior

Producer tests:

```text
eligible subscription/state
→ capability allowed
```

Exact Billing domain semantics at candidate SHA are authoritative.

---

# 90. TAC-BI-004 — Capability denied behavior

```text
ineligible subscription/state
→ capability denied
```

---

# 91. TAC-BI-005 — Limit semantics

If selected capability has a limit:

```text
limit returned
usage/remaining semantics correct
```

If no limit:

```text
explicit unlimited/not-applicable representation
```

Do not encode magic numeric sentinel unless existing model governs it.

---

# 92. TAC-BI-006 — CreateAutomationRule capability + usage lifecycle

Execute production `CreateAutomationRuleCommand` path.

## Capability

Before rule mutation:

```text
AccountId correct
WorkspaceId correct
Capability = AUTOMATION_RULE
RequestedAmount = 1
```

## Creation usage

After successful rule creation:

```text
Billing usage for Account + Workspace + AUTOMATION_RULE increases by 1
```

Required tests:

```text
available → rule created + usage +1
unavailable → no rule + no usage
duplicate/retry → no double usage
Workspace A create → Workspace B usage unchanged
```

## Capacity release

If a production capacity-releasing rule lifecycle exists:

```text
release → usage -1 exactly once
duplicate release → no second decrement
```

If none exists, record test/evidence state:

```text
NOT-APPLICABLE-UNTIL-LIFECYCLE-EXISTS
```

and explicitly test that current disable/archive state does or does not consume
capacity according to product semantics.

## Consistency

Force Billing usage write failure.

Assert the selected consistency model:

```text
no silent rule-created/usage-missing state
```

or prove the explicit reconciliation/retry path.
# 93. TAC-BI-007 — Consumer remains capability-based

Architecture/source assertion for `CreateAutomationRule`:

Forbidden:

```text
PlanTier
SubscriptionTier
Common.Entitlements direct dependency
provider status
```

Required dependency:

```text
Billing-owned Public capability
Billing-owned Public usage action if usage write is part of the lifecycle
```
# 94. TAC-BI-008 — Billing/Common debt shrink

If selected consumer previously used Common entitlement semantics:

```text
reference migration removes that dependency
active debt baseline shrinks
```

Test stale baseline is not left.

---

# 94A. TAC-BI-008A — Workspace quota isolation

Seed:

```text
Workspace A entitlement/usage
Workspace B entitlement/usage
```

Assert:

```text
A capability decision never consumes B workspace entitlement unless explicitly
Account-scoped by Billing policy

A usage increment never changes B usage
```

# 94B. TAC-BI-008B — Usage write idempotency

For one AutomationRule resource:

```text
OperationId O + Delta +1
→ one ledger/reservation effect

same O repeated
→ no additional effect

same O + conflicting source/delta
→ deterministic conflict
```

# 94C. TAC-BI-008C — Capability reflects lifecycle usage

Integration sequence:

```text
capability before create
→ create AutomationRule
→ capability after create
```

must show the expected remaining capacity/availability change.

The reference fails if capability tests seed ledger manually but production
AutomationRule lifecycle never updates that ledger.

# 95. TAC-BI-009 — No fabricated Billing provider

For c409:

```text
BI-REF-003 = NOT-APPLICABLE-AT-CANDIDATE
```

Assert M9 introduces no Stripe/payment-provider DTO/status/SDK into
Billing Domain/Application/Public.

If a real Billing provider exists at execution candidate SHA, this test reports
`STOP-SOURCE-DRIFT` rather than inventing a translation contract.
# 96. TAC-BI-010 — Billing pack completeness

Mandatory:

```text
Billing Public capability
producer tests
one consumer
no private tier branching in reference consumer
Common no-growth
provider boundary classified
```

---

# 96A. TAC-BI-FLOW-01 — Billing capability/capacity resolution

Before behavior certification, `BILL-LIMIT-001` must be resolved.

Required cases:

```text
no entitlement
inactive/expired entitlement
Account-scoped entitlement
Workspace-scoped entitlement
wrong Workspace
remaining capacity > requested
remaining capacity == requested
remaining capacity < requested
```

Zero-limit test asserts the resolved product semantic exactly.

Forbidden:

```text
test expects zero=unlimited merely because current implementation does
```

Assert consumer receives plan-neutral capacity fact.

# 96B. TAC-BI-FLOW-02 — Hard-capacity owner mutation

Execute production Billing capacity action.

Required identity:

```text
AccountId
WorkspaceId where scoped
CapabilityCode
Amount
LogicalOperationId
SourceResource
```

Behavior:

```text
first reservation/consume
same operation + same payload
same operation + conflicting payload
release/compensate
wrong scope
```

Fail-closed shape: the unavailable fact MUST be exercised with the real
producer shape (`IsAvailable = false, Limit = NULL`) and MUST fail closed to
a zero ceiling — a mock returning a raw null provider result does NOT prove
this.

Materialization: when no `WorkspaceFeatureUsage` row exists but the ledger
has history, the seeded `CurrentUsage` equals the ledger sum (drift is
forbidden).

Limit reconciliation: after `Entitlement.ChangeLimit`, the next capacity op
converges `HardLimit`; a downgrade below current usage retains the usage,
reports `Remaining = 0`, and denies new consumption.

Global identity: the same `LogicalOperationId` used in a DIFFERENT scope
with a different payload is a deterministic conflict (no per-scope dedup).

## Mandatory last-slot concurrency

Seed authoritative capacity:

```text
remaining = 1
```

Launch two concurrent real DB operations.

Assert:

```text
exactly one reservation accepted
authoritative consumed/reserved state == 1
no over-capacity
```

This must use the actual DB concurrency/locking/version mechanism.

### Direct-action last-slot proof

In addition to the handler-level race, prove the last-slot invariant at the
owner-action seam directly:

```text
two DbContexts / two transactions
both invoke IBillingCapacityActions.ConsumeAsync concurrently
distinct LogicalOperationIds
remaining = 1
→ exactly one AlreadyConsumed = false
→ ledger has exactly one +1 effect
→ WorkspaceFeatureUsage.Version advanced exactly once (== 2)
```

### First-use (bootstrap) concurrency

With NO existing `WorkspaceFeatureUsage` row:

```text
no WFU row, finite limit = 2
two concurrent ConsumeAsync (distinct operation ids)
→ BOTH succeed (no false unique-scope conflict)
→ CurrentUsage = 2, ledger = 2 +1 effects
```

This exercises the atomic get-or-create path and the ledger materialization.

# 96C. TAC-BI-FLOW-03 — Concurrent CreateAutomationRule→capacity

Use production `CreateAutomationRule`.

Seed:

```text
remaining AUTOMATION_RULE capacity = 1
```

Run two concurrent commands for distinct logical rules.

Assert:

```text
exactly one rule succeeds
exactly one capacity slot is consumed
losing request receives canonical capacity/conflict failure
```

Failure compensation test:

```text
capacity reserved
→ AutomationRule persistence fails
→ capacity is released/compensated according to frozen protocol
```

Retry test:

```text
same LogicalOperationId
→ no double consumption
```

Disable-capacity semantic:

```text
consume up to the AUTOMATION_RULE limit (CurrentUsage = 2/2)
SetAutomationRuleEnabled disable one rule
→ capacity is unchanged: Used = 2, Remaining = 0
→ a further create is still rejected at the limit
→ no release effect is recorded
```

This pins AUTOMATION_RULE = rule existence capacity (see spec BI-FLOW-03).

# 96D. TAC-BI-FLOW-04 — Capability after real capacity mutation

Production sequence:

```text
capability before
→ real capacity reserve/consume
→ capability after
```

Assert expected remaining capacity.

Also prove:

```text
release/compensation restores capacity
```

No manual current-count seeding can be the only lifecycle proof.

# 96E. TAC-BI-CAPACITY-VS-METERING — No semantic conflation

Architecture/behavior assertion:

```text
AUTOMATION_RULE capacity flow
```

does not require billable UsageMetric fields such as:

```text
metric aggregation
invoice quantity
late usage correction
```

unless product authority explicitly promotes the feature to billable metering.

Future commercial metric flows must receive a separate Flow Card.

# 97. TAC-AR-001 — WorkspaceWorkItemPlacementProjection exists

Required Analytics-owned production type keyed by:

```text
WorkspaceId + ItemId
```

Required state includes:

```text
BoardId
GroupId
LastOccurredAt
SourceRevision when available
Archived/active handling
```

`ReportingSnapshot` does not satisfy AR-REF-001.
# 97B. TAC-AR-001B — Producer Public source implementation ownership

Required production implementation:

```text
WorkManagement Application
→ implements IWorkItemProjectionSource
→ depends on IWorkManagementDbContext
```

Assert no type under:

```text
Infrastructure/CrossContext/Analytics/WorkManagement
```

implements `IWorkItemProjectionSource` by reading Work persistence directly.

Consumer adapter may depend on `IWorkItemProjectionSource`.

It may not depend on `IWorkManagementDbContext`.

# 97C. TAC-AR-001C — Projection-source interaction seam

The old generic "remote substitution" assumption is replaced by the revised
SPEC interaction decision.

M10 closed (Phase 0, frozen):

```text
AR-FLOW-03 / WM-FLOW-05
DecisionStatus = CLOSED-FROZEN (TAC-XC-B)
```

Frozen invariant:

```text
WorkManagement owns authoritative projection snapshot truth
WorkManagement Application implements producer Public source
Analytics cannot implement Work producer truth
Analytics cannot read Work DbContext directly
```

Prove the frozen `TAC-XC-B` path:

```text
Analytics Application depends on Analytics-owned source/rebuild Port
→ Infrastructure adapter (delegate only)
→ WorkManagement.Public IWorkItemProjectionSource
→ WorkManagement Application implementation
```

Forbidden false-green:

```text
Infrastructure Analytics adapter
→ IWorkManagementDbContext directly
```

The test must prove ownership first; remote transport simulation is not
required.

# 98. TAC-AR-002 — Work event consumers use producer boundary

Required consumers:

```text
BoardItemMovedPlacementConsumer
BoardItemArchivedPlacementConsumer
BoardItemCreatedPlacementConsumer
```

When event data is sufficient:

```text
consumer → Analytics projection service
```

When event data is insufficient:

```text
consumer
→ Analytics source adapter
→ WorkManagement.Public IWorkItemProjectionSource
```

Forbidden:

```text
consumer/adapter → IWorkManagementDbContext
```

Test source-fact fallback for created/moved events through the producer
contract.
# 99. TAC-AR-003 — Projection first delivery

Given valid Work event:

```text
Analytics local derived state changes
```

according to selected metric.

---

# 100. TAC-AR-004 — Projection duplicate delivery

Same event twice:

```text
same final derived state
```

No double-counting unless event is intentionally additive with event-id dedup
handled separately.

---

# 101. TAC-AR-005 — Projection out-of-order event

Test selected ordering/revision strategy.

Allowed strategies:

```text
ignore stale revision
reorder by producer revision
commutative aggregation
detect gap and rebuild
```

One must be explicit.

---

# 102. TAC-AR-006 — Projection scope isolation

Events from:

```text
Workspace A
```

must not update:

```text
Workspace B
```

or account equivalent.

---

# 103. TAC-AR-007 — Projection source truth separation

Architecture test:

```text
Analytics consumer/query
does not mutate WorkManagement
```

No Work DbContext/repository/Domain mutation dependency.

---

# 104. TAC-AR-008 — Analytics query reads placement projection

Required query reads only:

```text
WorkspaceWorkItemPlacementProjection
```

for the requested Workspace.

No source Work query occurs on the normal Analytics read path.
# 105. TAC-AR-009 — Exact placement rebuild through producer Public

Required production chain:

```text
RebuildWorkspacePlacements
→ Analytics rebuild service
→ Analytics source adapter/Port
→ WorkManagement.Public IWorkItemProjectionSource
→ WorkManagement Application implementation
→ Work snapshot
→ Analytics projection persistence
```

Tests:

```text
empty source
multiple items
moved item
archived/removed item
duplicate rebuild
repair drift
cross-workspace isolation
```

Architecture assertion:

```text
only WorkManagement producer implementation may depend on
IWorkManagementDbContext
```

Direct Work DbContext adapter fails.
# 106. TAC-AR-010 — Rebuild does not use arbitrary foreign persistence

Architecture proof:

```text
event replay
or approved producer snapshot/read contract
```

No direct Work tables from Analytics Application.

---

# 107. TAC-AR-011 — Consumer failure

Force projection persistence/handler failure.

Assert:

```text
source Work state remains unaffected
delivery can retry/recover
```

according to Platform mechanism.

---

# 108. TAC-AR-012 — Freshness semantics

If selected projection exposes freshness marker/revision:

```text
query returns or internally tracks it correctly
```

If product does not expose freshness publicly:

```text
test last processed revision/timestamp mechanism used for rebuild/order
```

---

# 109. TAC-AR-013 — Analytics pack completeness

Mandatory:

```text
event consumer
projection state
local query
idempotency
ordering
scope
rebuild
source truth separation
```

Missing any → FAIL.

---

# 109D. TAC-AR-FLOW-01 — Live placement projection

Map AR-FLOW-01 to the concrete tests:

```text
TAC-AR-002
TAC-AR-003
TAC-AR-004
TAC-AR-005
TAC-AR-006
TAC-AR-011
```

Additionally, the source Work event must satisfy `TAC-GATE-023` and restore the
correct tenant through Platform runtime.

# 109E. TAC-AR-FLOW-03 — Producer-backed rebuild

Map AR-FLOW-03 to:

```text
TAC-AR-009
TAC-AR-010
TAC-AR-001B
TAC-AR-001C
```

The test must prove Work producer-owned `IWorkItemProjectionSource` and no
Analytics-side direct Work DbContext access.

# 109A. TAC-EVT-REQUEST-001 — Requested facts are not blanket-rejected

`N8nDispatchRequestedV1` is valid only if tests prove:

```text
an Automation execution/dispatch intent is durably accepted
→ event is enrolled with that committed state
→ consumer performs post-commit provider effect
```

The event must not be rejected solely because its name says `Requested`.

This test protects the distinction between:

```text
valid committed requested fact
and
invalid producer instruction disguised as an event
```

# 109B. TAC-AR-FLOW-02 — Analytics local query baseline

Execute production `GetWorkspacePlacementsQuery` or normalized equivalent.

Assert:

```text
reads Analytics local projection only
workspace scoped
no Work Public call on normal read
no IWorkManagementDbContext
```

# 109C. TAC-AR-FLOW-04 — Projection failure/recovery

Cases:

```text
duplicate event
stale event
out-of-order event
consumer crash before commit
retry after crash
producer snapshot unavailable
rebuild retry
concurrent newer live event during rebuild
cross-workspace contamination
```

Assert final projection converges to producer truth without becoming source
authority.

# 110. TAC-PF-001 — Work event enters outbox

Integration test at producer commit.

Assert atomic enrollment according to current transaction mechanism.

---

# 111. TAC-PF-002 — Envelope identity

Assert current envelope contains required technical identity:

```text
message/event id
contract type/version
correlation/causation if current mechanism supports
```

Do not assert business fields not owned by Platform.

---

# 112. TAC-PF-003 — Duplicate delivery handling

Deliver same event twice.

Reference consumers:

```text
Automation
Analytics
```

must remain semantically idempotent.

If Platform inbox prevents second consumer invocation:

```text
test Platform dedup
```

If consumer idempotency handles it:

```text
test consumer state
```

Record owner.

---

# 113. TAC-PF-004 — Technical retry

Simulate transient consumer/adapter failure.

Assert:

```text
delivery retry mechanism executes
```

according to current Platform policy.

Business rejection must not be retried as transport failure unless explicitly
mapped.

---

# 114. TAC-PF-005 — Dead-letter/poison behavior

Where current Platform supports dead-letter/poison handling:

```text
exhaust retry
→ terminal technical handling
```

If current runtime has another terminal model:

```text
test that exact mechanism
```

Not optional for PF pack if mechanism exists.

---

# 115. TAC-PF-006 — Platform semantic purity

Architecture test:

Platform mechanism code must not depend on feature-specific business types
except generic contract/runtime registration boundaries already governed.

Forbidden logic equivalent:

```text
if event is BoardItemChanged then apply product rule
if PlanTier == Pro
if WorkspaceRole == Admin
```

Platform may route contract type technically.

It must not interpret product meaning.

---

# 116. TAC-PF-007 — Broker SDK confinement

Assert broker/provider SDK types exist only in permitted outer/runtime layers.

Domain/Application must remain neutral.

---

# 117. TAC-PF-008 — PF pack completeness

Mandatory:

```text
outbox
delivery
message identity
dedup/idempotency ownership
retry
terminal failure behavior
Broker confinement
semantic purity
```

---

# 117A. TAC-PF-FLOW-01 — Production request pipeline

Choose one protected write and run through real mediator/request pipeline.

Authoritative behavior order is frozen by `PipelineOrderTests` and
`Notrelix.Application/DependencyInjection.cs`. The transaction is opened by
`DataSessionBehavior`, so idempotency runs *inside* the transaction; outbox
enrollment commits with business state and dispatch happens only after commit.

Assert the stage/effect sequence a protected write actually observes:

```text
authentication            (API host, upstream of the MediatR pipeline)
ExceptionMapping          (maps failures to stable results)
ApplicationTracing        (root span)
RequestContract           stage request.contract       → invalid-contract gate
ExecutionContext          stage execution_context.resolve → scope/tenant gate
DataSession               stage data_session.open      → BEGIN transaction + apply RLS tenant scope
AccessControl             stages access_facts.query, policy.evaluate → authorization gate
Idempotency               stage idempotency.acquire    → idempotency gate (INSIDE the transaction)
handler                   stage handler.execute        → Domain mutation + outbox enrollment
Idempotency               stage idempotency.complete   → completes atomically with business state
DataSession                                            → SaveChanges + COMMIT (expected version validated)
outbox dispatch           post-commit only             → broker
```

Conceptual → authoritative mapping (the informal short list
`auth → scope → authorization → idempotency → transaction → handler → outbox → commit`
is superseded by the order above; notably the transaction *wraps*
authorization/idempotency/handler, and commit follows handler+outbox enrollment).

Required invariants:

```text
authorization runs inside the DB/RLS scope (DataSession before AccessControl)
idempotency completes inside the transaction (DataSession before Idempotency)
no dispatch is observable before commit (MVCC: dispatcher cannot see the row pre-commit)
```

Failure at each pre-handler gate (invalid contract, unresolved scope, denied
authorization, idempotency replay/conflict) leaves no committed mutation; a
post-handler rejection rolls back the transaction and enrolls no outbox fact.

# 117B. TAC-PF-FLOW-02 — DomainEvent→IntegrationEvent→outbox

Required producer families:

```text
Identity
Workspaces
WorkManagement
Documents
```

For each selected producer prove same-transaction enrollment and no publication
before commit.

# 117C. TAC-PF-FLOW-03 — Tenant restoration + dedup runtime

Run actual consumer filter chain.

Cases:

```text
first delivery
duplicate
parallel duplicate
wrong/missing tenant
crash before commit
retry
```

Consumer mutation + dedup success must follow runtime atomicity contract.

# 117D. TAC-PF-FLOW-04 — Delivery retry/failure

Using N8n or another real provider consumer, distinguish:

```text
business rejection
technical transient
rate-limit/timeout
unknown outcome
poison/non-retryable
```

Assert exactly one retry owner.

# 117E. TAC-PF-FLOW-05 — Event compatibility and capability-based recovery

Use the real event descriptor and evolution-policy machinery. Recovery cases
are selected by the event family's declared capability; do not require a
retained-event replay proof for families whose recovery is drain or rebuild.

Cases:

```text
supported version
unsupported version
declared upcast
capability-specific cutover/recovery
checkpoint replay only when a retained source exists
resume after interruption only when a retained source exists
consumer dedup during replay only when replay is declared
```

# 117F. TAC-PF-FLOW-06 — Background actor security

Run Automation→Work target action.

Assert explicit:

```text
ExecutorUserId
AccountId
WorkspaceId
correlation/causation
target authorization
```

No system-context permission bypass.

# 117G. TAC-PF-FLOW-07 — Scoped Integration Event tenant envelope

Select representative event families:

```text
WorkspaceMemberAdded
Board/BoardItem event
Page event
Comment/Mention event
```

For each, execute:

```text
producer mutation
→ outbox
→ broker
→ TenantContextConsumeFilter
→ consumer
```

Assert:

```text
event.AccountId == expected Account
event.WorkspaceId == expected Workspace
consumer current tenant == expected Account/Workspace
consumer is NOT System context
```

Negative test:

```text
Workspace-scoped event with AccountId missing
→ architecture/contract gate fails before certification
```

If event schema/version is migrated, run event compatibility/upcaster/replay
tests covering old accepted payloads according to Platform policy.

This section certifies PF-FLOW-07 and TAC-FRZ-018.

# 118. TAC-XPK-001 — Production Work→Automation→Work chain

Entry point must be the production Automation action execution path.

Required chain:

```text
AutomationExecution / real action executor
→ MoveItem
→ IWorkActionPort
→ ACL
→ WorkItemActionAdapter
→ IWorkItemActions
→ target scope/auth/idempotency
→ MoveBoardItem use case
```

Required scenarios:

```text
success
wrong Workspace
unauthorized actor
duplicate OperationId
conflicting OperationId
target business rejection
technical target failure
```

The old test pattern:

```text
test constructs IWorkActionPort
→ calls MoveItemAsync directly
```

may remain supplementary but cannot satisfy TAC-XPK-001.
# 118A. TAC-XPK-001A — Production Work→Analytics chain

Execute:

```text
MoveBoardItem
→ BoardItemMovedIntegrationEvent
→ real dedup delivery
→ Analytics consumer
→ producer Public source if needed
→ Analytics projection persistence
→ Analytics local query
```

Then drift the projection and execute:

```text
Analytics rebuild
→ producer Public IWorkItemProjectionSource
→ WorkManagement Application implementation
→ projection repair
```

Fail if the test bypasses producer Public by constructing an Analytics adapter
over Work DbContext.
# 118B. TAC-XPK-001B — Production N8n retry transaction

Required real pipeline:

```text
DeduplicationConsumeFilter<N8nDispatchRequestedV1>
→ N8nDispatchConsumer
→ N8nDispatchUseCase
```

### Retryable provider failure

Assert:

```text
consumer throws retryable delivery exception
outer transaction rolls back
dedup success not committed
Automation retry AttemptCount/evidence not falsely committed
```

### Subsequent success

Assert:

```text
retry delivery succeeds
AutomationExecution succeeds
dedup success commits once
```

### Terminal failure

Assert:

```text
consumer returns
Automation terminal failure commits
delivery marked succeeded
```

### Unknown outcome

Assert:

```text
consumer returns
manual-reconciliation/terminal state commits
no automatic redelivery
```

# 119. TAC-XPK-002 — Automation→Work target action chain

Continue flagship flow:

```text
Automation Process
→ IWorkActionPort
→ Infrastructure adapter
→ Work Public action
→ Work Application/Domain
```

Assert target mutation occurs only through Work authority.

---

# 120. TAC-XPK-003 — Work target failure chain

Force producer business rejection.

Assert:

```text
Work state unchanged
Automation Process reaches defined failed/business-failure state
Platform does not classify as transient transport retry
```

---

# 121. TAC-XPK-004 — Work→Analytics chain

Same producer event:

```text
→ Analytics consumer
→ projection
→ query
```

Assert source Work state remains authoritative.

---

# 122. TAC-XPK-005 — Fan-out independence

If same Work event feeds Automation and Analytics:

```text
Automation failure
must not corrupt Analytics state

Analytics failure
must not roll back committed Work source state
```

Delivery/retry may be independent according to current mechanism.

---

# 123. TAC-XPK-006 — Identity→Workspaces→Accounts flow

Focused integration:

```text
Identity fact
→ AcceptInvitation
→ Accounts target mutation
→ Workspace mutation
```

Assert selected M2 transaction semantics exactly.

---

# 124. TAC-XPK-007 — Workspace membership→Governance flow

Required selected reference reaction/query.

Assert:

```text
Workspaces remains membership owner
Governance remains policy owner
```

No foreign mutation.

---

# 125. TAC-XPK-008 — Billing→consumer flow

Execute:

```text
Billing Public capability
→ selected consumer
```

Two cases:

```text
allowed
denied
```

Assert no private plan type reaches consumer.

---

# 126. TAC-XPK-009 — ResourceRef→Collaboration flow

Execute:

```text
foreign target
→ Collaboration local mutation
```

Assert target aggregate unchanged.

---

# 127. TAC-XPK-010 — Integrations provider flow

Execute:

```text
Application semantic request
→ provider Port
→ Infrastructure adapter
→ provider test boundary
→ semantic result
```

Cover success and failures.

---

# 126A. TAC-XPK-IA — Register→Accounts→Workspace chain

Execute:

```text
Register
→ Accounts.Public provisioning
→ BOUND-TX-004 commit
→ IdentityRegistrationCompleted
→ Platform
→ WorkspaceProvisioningConsumer
```

Assert one committed AccountId and idempotent Workspace reaction.

# 126B. TAC-XPK-DC — Documents/Collaboration multi-resource chain

Execute:

```text
CreatePage
→ Comment.ForPage
→ CommentCreated
→ Activity

Mention on Page
→ MentionCreated
→ Notification
```

Assert Documents/Collaboration ownership remains separate and tenant scope is
correct throughout.

# 126C. TAC-XPK-INTEGRATIONS — Calendar lifecycle + webhook

Execute:

```text
ConnectCalendar
→ verified provider connection/secret reference

verified webhook
→ trusted connection lookup
→ tenant-scoped Integrations processing

DisconnectCalendar
→ disconnected product state + cleanup policy
```

No raw secret leakage.

# 126D. TAC-XPK-BILLING — Rule capability/usage lifecycle

Execute:

```text
capability
→ CreateAutomationRule
→ usage
→ capability
```

Assert expected capacity change and M2C consistency.

# 126E. TAC-XPK-ACCOUNTS — Local admin + Common purity

Execute Accounts RenameAccount through production auth.

Assert the runtime can resolve the flow without a Common contract exposing
`AccountRole`/`WorkspaceRole`.

# 126F. TAC-XPK-EVENT-TENANT — Scoped event end-to-end tenant chain

Execute at least:

```text
PageCreated or PageArchived
MentionCreated
WorkspaceMemberAdded
BoardItemMoved/member-assigned
```

through real Platform filters.

Assert tenant restoration exact for every family.

# 126G. TAC-XPK-CALENDAR — Connection/binding/webhook lifecycle

Execute:

```text
ConnectCalendar
→ reload Connection + SecretVersion + CalendarIntegration
→ verified webhook resolves same binding/tenant
→ DisconnectCalendar deactivates binding
→ generic Connection follows CAL-CONN-001
```

# 126H. TAC-XPK-BILLING-LAST-SLOT — Hard-quota production race

Execute two concurrent `CreateAutomationRule` commands with one remaining slot.

Assert one successful rule and one capacity consumption.

This is required cross-BC evidence for TAC-FRZ-020.

# 127A. TAC-DI-000 — v2.6 production dependency graph

Production DI must resolve:

```text
Accounts RenameAccount handler
Accounts.Public provisioning/membership actions
canonical Governance authorization

Common technical grant mechanism after TAC-FRZ-017 cleanup

Documents Create/Archive handlers
Collaboration Activity/Notification consumers

Automation action executor
Work target action/auth/idempotency

Integrations:
  secret-store Port
  Connection owner service
  IntegrationSecretVersion persistence
  CalendarIntegration owner service
  ConnectCalendar
  DisconnectCalendar
  inbound webhook verification/intake
  N8n provider chain

Billing:
  capability facts
  capacity reservation/consume/release action
  selected concurrency implementation

Analytics:
  local projection/query/rebuild
  Work producer projection source

Platform:
  TenantContextConsumeFilter
  DeduplicationConsumeFilter
  event compatibility/upcaster/replay
```

No test-only registration satisfies this proof.
# 128. TAC-DI-001 — Public provider resolution

Resolve all required producer services.

At minimum:

```text
Identity facts
Accounts facts/action
Governance authorization
Work action
Billing capability
```

according to final contract shape.

---

# 129. TAC-DI-002 — Consumer Port resolution

Resolve mandatory Ports:

```text
WorkManagement Collaboration
Automation Work action
Integrations provider Port
Collaboration target Port if used
```

---

# 130. TAC-DI-003 — Event consumer registration

Required runtime registrations:

```text
Automation Work event consumer
Analytics Work event consumer
Governance membership event consumer if selected
```

No test-only registration that production composition lacks.

---

# 131. TAC-DI-004 — Process dependencies

Resolve Automation Process runtime dependencies from production DI.

---

# 132. TAC-DI-005 — Projection dependencies

Resolve Analytics consumer/projection store/query from production DI.

---

# 133. TAC-MIG-001 — Foreign Abstractions cleanup

For seams migrated to Public/Port:

```text
consumer production source
→ zero producer Abstractions dependency
```

Especially:

```text
Workspaces → Accounts
```

after M2 reference migration.

Do not globally forbid legitimate local Abstractions.

---

# 134. TAC-MIG-002 — Old type/namespace zero references

Extend existing `STN-ARCH-008` only for migrations actually completed.

No stale old seam.

---

# 135. TAC-MIG-003 — Legacy Data/ReadPorts zero

Reuse `STN-ARCH-005`.

---

# 136. TAC-MIG-004 — Services baseline exact

Reuse hardened `STN-ARCH-006`.

Reference adapters may not be added to generic `Infrastructure/Services`.

---

# 137. TAC-MIG-005 — Common debt shrink

After Billing/reference migrations:

```text
active debt baseline equals remaining source debt
```

Removed allowances are deleted.

---

# 138. TAC-MIG-006 — No duplicate semantic contracts

Audit newly added v2 types.

Fail when two types represent the same producer semantic merely because two
consumers requested it, unless consumer-specific Ports intentionally differ.

Producer Public should be reusable by producer meaning.

---

# 139. TAC-COPY-001 — Local slice teaching reference exists

Required:

```text
CreateBoard
```

or finalized equivalent.

Tests prove pipeline-first.

---

# 140. TAC-COPY-002 — Direct Public teaching reference exists

Required exactly for the bootstrap teaching reference:

```text
Workspaces → Identity.Public
```

No consumer Port is required for this direct producer-Public read.

---

# 141. TAC-COPY-003 — Port+Adapter teaching reference exists

Required:

```text
WorkManagement → Collaboration
```

and:

```text
Automation → WorkManagement
```

The second includes target mutation semantics.

---

# 142. TAC-COPY-004 — ACL teaching reference exists

At least one real production ACL must exist and be exercised.

No `DEFER`.

---

# 143. TAC-COPY-005 — Target mutation teaching reference exists

Both required paths must be classifiable:

```text
Workspaces → Accounts
Automation → WorkManagement
```

At least Automation→Work must be a clean Port+adapter+target Public action
reference.

---

# 144. TAC-COPY-006 — Event teaching reference exists

Required complete Work event chain.

No interface/event contract without delivery consumer.

---

# 145. TAC-COPY-007 — Projection teaching reference exists

Required Analytics projection.

No defer.

---

# 146. TAC-COPY-008 — Process teaching reference exists

Required Automation Process.

No defer.

---

# 147. TAC-COPY-009 — ResourceRef teaching reference exists

Required Collaboration reference.

---

# 148. TAC-COPY-010 — Provider teaching reference exists

Required Integrations provider Port/adapter.

---

# 149. TAC-COPY-011 — Billing teaching reference exists

Required producer capability + consumer.

---

# 150. TAC-COPY-012 — Governance teaching reference exists

Required authoritative semantic contract + executable reference path.

---

# 151. TAC-COPY-013 — Broker/platform teaching reference exists

Required actual outbox/delivery chain.

No alternate demo bus.

---

# 152. TAC-COPY-014 — Transaction teaching reference exists

AcceptInvitation selected M2 model must be executable/tested.

---

# 153. Reference completeness matrix

Final test evidence must show:

| Architecture pattern | Mandatory reference | PASS required |
|---|---|---:|
| local vertical slice | CreateBoard | yes |
| pipeline-first | CreateBoard | yes |
| Public Direct | Identity→Workspaces | yes |
| producer fact | Identity/Accounts | yes |
| Consumer Port | WM→Collab | yes |
| target mutation Port | Automation→Work | yes |
| ACL | selected DC/AI flow | yes |
| composite read | Identity Bootstrap | yes |
| target-owned mutation | Accounts + Work actions | yes |
| Integration Event | Work event | yes |
| outbox/delivery | Platform | yes |
| event consumer | Automation + Analytics | yes |
| Projection | Analytics | yes |
| rebuild | Analytics | yes |
| Process Manager | Automation | yes |
| ResourceRef | Collaboration | yes |
| Billing entitlement | Billing→consumer | yes |
| Governance authorization | Governance reference | yes |
| provider Port/adapter | Integrations | yes |
| background actor/security | Automation | yes |
| transaction decision | AcceptInvitation | yes |

No mandatory row may be blank/deferred.

---

# 154. Team pack completeness matrix

## TAC-RAP-IA

PASS requires:

```text
TAC-IA-001..008 applicable set PASS
TAC-TX selected branch PASS
```

## TAC-RAP-WG

PASS requires:

```text
TAC-WG-001..009 PASS
```

## TAC-RAP-WM

PASS requires:

```text
TAC-WM-001..010 PASS
```

## TAC-RAP-DC

PASS requires:

```text
TAC-DC-001..009 PASS
```

## TAC-RAP-AI

PASS requires:

```text
TAC-AI-001..026 PASS
```

## TAC-RAP-BI

PASS requires:

```text
TAC-BI-001..010 PASS
```

## TAC-RAP-AR

PASS requires:

```text
TAC-AR-001..013 PASS
```

## TAC-RAP-PF

PASS requires:

```text
TAC-PF-001..008 PASS
```

---

# 155. Freeze-to-test matrix

| Freeze | Required tests |
|---|---|
| TAC-FRZ-001 | TAC-TX-* + TAC-IA/WG target mutation |
| TAC-FRZ-002 | TAC-GATE-001..004 |
| TAC-FRZ-003 | TAC-GATE-005 + TAC-MIG-004 |
| TAC-FRZ-004 | composite read reference tests |
| TAC-FRZ-005 | TAC-IA-002/004 + TAC-WG-002 |
| TAC-FRZ-006 | TAC-AI-019..025 |
| TAC-FRZ-007 | TAC-WM event + TAC-PF + TAC-XPK event chain |
| TAC-FRZ-008 | TAC-GATE-006 + TAC-MIG-005 |
| TAC-FRZ-009 | all TAC-RAP pack matrices |
| TAC-FRZ-010 | TAC-AI-001..013 + TAC-XPK-001..003 |
| TAC-FRZ-011 | TAC-AR-001..013 + TAC-XPK-004/005 |
| TAC-FRZ-012 | TAC-BI-001..010 + TAC-XPK-008 |
| TAC-FRZ-013 | TAC-WG-003..009 |
| TAC-FRZ-014 | TAC-DC-001..009 + TAC-XPK-009 |
| TAC-FRZ-015 | TAC-AI-014..025 + TAC-XPK-010 |

---

# 156. Composite read reference tests

Identity Bootstrap remains mandatory supporting reference.

Required:

```text
handler depends on consumer Port
adapter composes foreign/local read sources
no mutation
no producer policy recreation
no foreign Domain leakage
DI resolution
bootstrap behavior
```

Future remote substitution:

```text
handler contract unchanged
```

must be visible through dependency structure.

---

# 157. Public fact semantic isolation tests

For each broad capability boolean:

```text
CanParticipate
CanAdmitMember
CapabilityAllowed
PermissionAllowed
```

tests must show exact owner inputs.

Avoid testing only:

```text
true/false
```

Test which state changes can and cannot affect it.

This protects semantic scope.

---

# 158. Negative fixture strategy

Use test-only fixtures/helpers rather than invalid production types.

Required conceptual invalid cases:

```text
N1 Public exposes Domain aggregate
N2 Public uses unapproved Common business type
N3 Public references foreign Producer.Public
N4 Application uses HttpClient
N5 Application uses Broker client
N6 consumer uses producer internal Application
N7 consumer uses producer DbContext
N8 cross-BC ORM navigation
N9 new generic Infrastructure/Services file
N10 deleted Services file regrows
N11 Process missing workflow owner semantics
N12 Projection mutates producer
N13 Analytics uses Work DbContext
N14 Automation uses Work DbContext
N15 Billing consumer branches on private PlanTier
N16 Platform owns feature-specific policy
N17 ResourceRef carries aggregate/navigation
N18 event is consumer instruction
N19 provider SDK enters Application
N20 Team Operating Pack only contains interface/stub
```

Not all need one architecture class.

Reuse appropriate existing owners.

---

# 159. Positive fixture/reference strategy

Required conceptual valid cases:

```text
P1 Identity Public direct fact
P2 Accounts target action
P3 WorkManagement Collaboration Port
P4 Automation Work Port
P5 Infrastructure InProcessAdapter
P6 Work IntegrationEvent V1
P7 Automation Process
P8 Analytics Projection
P9 Collaboration ResourceRef
P10 Billing capability
P11 Governance permission contract
P12 Integrations provider adapter
P13 Platform outbox/delivery
```

Most positive proofs should be actual production references, not synthetic
fixtures.

---

# 160. Reference anti-stub test

Mandatory reference completeness tooling/review must reject production source
equivalent to:

```csharp
throw new NotImplementedException();
return Task.FromResult(Result.Success());
return default;
```

when used as the only implementation of a mandatory reference.

Do not globally ban all constant-return methods.

Apply to exact reference types.

---

# 161. Reference reachability test

For every mandatory reference source type:

```text
at least one test executes behavior
```

For adapters/consumers:

```text
production DI resolves it
```

This prevents dead architecture teaching code.

---

# 162. Persistence migration tests

If v2 adds durable:

```text
Automation Process state
Analytics Projection state
```

required:

```text
migration applies
schema expected
context-local persistence interface resolves
rollback/migration tests according to repository conventions
```

No cross-BC ORM nav/FK cascade.

---

# 163. Event compatibility tests

Work and Workspace reference events:

```text
version stable
required fields serialized
producer ownership registered
```

Do not snapshot incidental serializer ordering unless repository contract tests
already do so.

---

# 164. Process restart test

If Automation Process state is durable:

```text
persist running state
recreate process service/runtime
resume/handle next outcome
```

must preserve progression.

This is mandatory to prove it is not merely an in-memory orchestrator.

---

# 165. Process terminal-state test

After:

```text
Completed
or
Failed
```

duplicate participant outcome must not reopen state.

---

# 166. Projection rebuild equivalence

Given source history H:

```text
incremental consume(H)
```

and:

```text
rebuild(H)
```

must produce semantically equivalent projection.

Compare business-relevant fields only.

---

# 167. Projection deletion/replay policy

If source event includes deletion/archive semantics, selected reference must
define behavior.

If selected event does not include delete:

```text
not applicable to the reference metric
```

record explicitly.

Do not fabricate delete event.

---

# 168. Background authorization test

For Automation target action:

```text
service/background actor
```

must not bypass target Work invariants.

Test at least one denied path.

---

# 169. Port+ACL+Adapter runtime-substitution dependency test

Use Automation→Work as the primary executable reference.

Consumer Automation Application may depend on:

```text
IWorkActionPort
```

not:

```text
WorkItemActionAdapter
remote transport client
Work DbContext
Work Domain aggregate
```

The production path must prove:

```text
Automation executor
→ IWorkActionPort
→ consumer ACL mapping where needed
→ Infrastructure adapter
→ WorkManagement.Public action
```

This certifies `TAC-XC-B` adapter readiness.

A test may replace the adapter in composition and assert the same Application
behavior contract, but a fake remote service is not required.

## 169A. Producer Public Direct semantic-boundary readiness test

Use IA-FLOW-04 as the primary reference.

Assert:

```text
consumer depends on stable producer-owned Accounts.Public fact
producer owns implementation/state
foreign private persistence is inaccessible
```

Do **not** require:

```text
consumer Port
remote adapter
HTTP/gRPC server
```

merely to pass service-readiness.

Expected result:

```text
SemanticBoundaryReady = true
RuntimeSubstitutionReady = deferred/semantic-only
```

is a valid M4 state.

## 169B. Extraction-blocker negative remote test

For:

```text
IA-FLOW-02:E1 → BOUND-TX-004
IA-FLOW-05:E1 → BOUND-TX-002
```

assert evidence explicitly reports:

```text
SemanticBoundaryReady = true
ExtractionBlocked = true
RuntimeSubstitutionReady = false/deferred
```

Fail certification if implementation replaces the current shared transaction
with:

```text
separate remote commits
eventual consistency
Saga/compensation
```

without reopening the frozen transaction/product decision.

## 169C. Integration Event deployment-independence test

For a flagship Integration Event path, prove the same semantic chain through
the real Platform mechanism:

```text
producer state + outbox commit
→ delivery
→ consumer-local reaction
```

The proof must not rely on both BCs sharing private persistence or a direct
method call.

Physical co-location in the current process is allowed; the event semantics
must not depend on it.

# 170. Provider remote-substitution dependency test

Application Integrations use case depends on semantic Port.

Provider adapter may depend on HTTP/SDK.

Replace adapter in test composition:

```text
same Application behavior contract
```

where practical.

---

# 171. API non-requirement test policy

Do not create a failing test because mandatory reference has no HTTP endpoint.

Reference source is valid if:

```text
Application/runtime/integration reachable
```

API exposure is product-driven.

---

# 172. API orchestration protection

If any API endpoint is added for these references:

```text
endpoint must dispatch to one owning Application use case/process
```

not manually coordinate multiple BC mutations.

Reuse existing API architecture tests or add exact focused test.

---

# 173. CI test ordering

Fast lane:

```text
build
Architecture.Tests
unit/behavior tests
```

Then:

```text
integration
event/runtime
provider
process/projection
```

All required for final exact SHA.

Do not weaken local developer fast lane by removing full CI proof.

---

# 172A. Real Flow traceability — v2.6

Expected catalog:

```text
47 Flow IDs
46 executable/verification-required
1 conditional = BI-FLOW-05
```

Ranges:

```text
IA-FLOW-01..06
WG-FLOW-01..06
WM-FLOW-01..05
DC-FLOW-01..07
AI-FLOW-01..07
BI-FLOW-01..05
AR-FLOW-01..04
PF-FLOW-01..07
```

Certification requires:

```text
every executable FlowId
→ concrete existing evidence heading
→ behavior/runtime assertions
→ architecture gate where applicable

every actual cross-BC Flow interaction
→ FlowId:EdgeId
→ TAC-XC-A..F mechanism
→ owning production proof at the correct milestone
```

A Flow can have several edge rows without creating new Flow IDs.

Presence of an ID or EdgeId in a table is not evidence.
# 172B. Exact Real Flow ID coverage matrix

Every Flow ID maps to a real section/test family that exists in this document.

| Flow | Concrete evidence |
|---|---|
| IA-FLOW-01 | TAC-IA-FLOW-01 |
| IA-FLOW-02 | TAC-IA-FLOW-02; its embedded BOUND-TX-004 runtime proof |
| IA-FLOW-03 | TAC-IA-FLOW-03 |
| IA-FLOW-04 | TAC-IA-003 + TAC-IA-004 |
| IA-FLOW-05 | TAC-IA-005 + TAC-IA-006 + TAC-TX-001 |
| IA-FLOW-06 | TAC-IA-FLOW-06 |
| WG-FLOW-01 | TAC-WG-FLOW-01 |
| WG-FLOW-02 | TAC-WG-FLOW-02 |
| WG-FLOW-03 | TAC-WG-FLOW-03 |
| WG-FLOW-04 | TAC-WG-FLOW-04 |
| WG-FLOW-05 | TAC-WG-FLOW-05 |
| WG-FLOW-06 | TAC-WG-FLOW-06 |
| WM-FLOW-01 | TAC-WM-001 |
| WM-FLOW-02 | TAC-WM-002 + TAC-WM-003 + TAC-WM-004 |
| WM-FLOW-03 | TAC-WM-005 + TAC-WM-006 |
| WM-FLOW-04 | TAC-WM-007 + TAC-WM-008 + TAC-WM-009 |
| WM-FLOW-05 | TAC-AR-001B + TAC-AR-001C + TAC-AR-009 |
| DC-FLOW-01 | TAC-DC-FLOW-01 |
| DC-FLOW-02 | TAC-DC-FLOW-02 |
| DC-FLOW-03 | TAC-DC-FLOW-03 |
| DC-FLOW-04 | TAC-DC-FLOW-04 |
| DC-FLOW-05 | TAC-DC-FLOW-05 |
| DC-FLOW-06 | TAC-DC-FLOW-06 |
| DC-FLOW-07 | TAC-DC-FLOW-07 |
| AI-FLOW-01 | TAC-AI-FLOW-01 |
| AI-FLOW-02 | TAC-AI-FLOW-02 |
| AI-FLOW-03 | TAC-AI-FLOW-03 |
| AI-FLOW-04 | TAC-AI-FLOW-04 + TAC-AI-004 |
| AI-FLOW-05 | TAC-AI-FLOW-05 |
| AI-FLOW-06 | TAC-AI-FLOW-06 |
| AI-FLOW-07 | TAC-AI-FLOW-07 |
| BI-FLOW-01 | TAC-BI-FLOW-01 |
| BI-FLOW-02 | TAC-BI-FLOW-02 |
| BI-FLOW-03 | TAC-BI-FLOW-03 |
| BI-FLOW-04 | TAC-BI-FLOW-04 |
| BI-FLOW-05 | TAC-BI-009 conditional provider classification |
| AR-FLOW-01 | TAC-AR-FLOW-01 |
| AR-FLOW-02 | TAC-AR-FLOW-02 |
| AR-FLOW-03 | TAC-AR-FLOW-03 |
| AR-FLOW-04 | TAC-AR-FLOW-04 |
| PF-FLOW-01 | TAC-PF-FLOW-01 |
| PF-FLOW-02 | TAC-PF-FLOW-02 |
| PF-FLOW-03 | TAC-PF-FLOW-03 |
| PF-FLOW-04 | TAC-PF-FLOW-04 + TAC-XPK-001B |
| PF-FLOW-05 | TAC-PF-FLOW-05 |
| PF-FLOW-06 | TAC-PF-FLOW-06 |
| PF-FLOW-07 | TAC-PF-FLOW-07 |

Validation rule:

```text
FlowId listed
AND
referenced test heading exists
AND
heading contains substantive assertions
```

String-presence alone is insufficient.

A nonexistent evidence label fails traceability.

## 172B1. PRE-M4 mandatory IA interaction-edge matrix

The revised SPEC/PLAN authority for M4 is exact:

| Edge | Mechanism | Primary proof |
|---|---|---|
| IA-FLOW-02:E1 | TAC-XC-E | TAC-IA-FLOW-02 + BOUND-TX-004 proof |
| IA-FLOW-03:E1 | TAC-XC-C | TAC-IA-FLOW-03 + PF outbox/dedup runtime |
| IA-FLOW-04:E1 | TAC-XC-A | TAC-IA-003/004 + IA-FLOW-04 classification proof |
| IA-FLOW-05:E1 | TAC-XC-E | TAC-IA-005/006 + TAC-TX-001/002B |

Local baselines:

```text
IA-FLOW-01 → Interactions[] empty
IA-FLOW-06 → local Accounts owner flow
```

This matrix is an M4 entry/traceability proof, not a new Flow catalog.

## 172B2. Deferred interaction decisions

The test authority keeps these explicit until the owning pack closes them:

```text
WM-FLOW-02:E1
→ DEFERRED-M6-SOURCE-NORMALIZATION

WM-FLOW-05 / AR-FLOW-03 projection-source edge
→ CLOSED-FROZEN (TAC-XC-B, M10-PORT-OWNERSHIP-NORMALIZATION)
```

A coding agent must not convert `DEFERRED-*` into an implementation preference.

# 172C. TAC-FRZ-016 — Team/BC Real Flow Catalog freeze test

Freeze passes only when:

```text
11/11 business bounded contexts have a verified local baseline
Platform/Foundation has a verified technical baseline
all mandatory executable Flow IDs have explicit production/test evidence
paired teams have both owned BCs complete
stub/log-only/NotImplemented source is excluded from Real Flow evidence
```

Required paired-team assertions:

```text
IA → Identity + Accounts
WG → Workspaces + Governance
DC → Documents + Collaboration
AI → Automation + Integrations
```

Failure of any owned BC baseline:

```text
TAC-FRZ-016 = FAIL
```

The freeze must be checked again at the exact candidate SHA.


# 172D. TAC-FRZ-017 — Common signature purity freeze test

PASS when:

```text
TAC-GATE-022 passes
IAccessGrantProjectionService no longer leaks AccountRole/WorkspaceRole
IA/WG production flows use normalized owner-preserving seam
```

# 172E. TAC-FRZ-018 — Scoped event tenant-envelope freeze test

PASS when:

```text
TAC-GATE-023 passes
TAC-PF-FLOW-07 runtime tests pass
selected WG/WM/DC/AI event flows restore expected tenant
event compatibility tests cover changed schemas
```

# 172F. TAC-FRZ-019 — Calendar lifecycle freeze test

PASS when:

```text
TAC-GATE-024 passes
AI-FLOW-05 DB round-trip passes
AI-FLOW-06 CAL-CONN-001 policy passes
AI-FLOW-07 verified intake passes
TAC-XPK-CALENDAR passes
```

# 172G. TAC-FRZ-020 — Billing capacity/concurrency freeze test

PASS when:

```text
BILL-LIMIT-001 resolved
TAC-GATE-025 passes
BI-FLOW-02 last-slot race passes
BI-FLOW-03 concurrent CreateAutomationRule passes
BI-FLOW-04 lifecycle result passes
TAC-XPK-BILLING-LAST-SLOT passes
```

# 173A. Mandatory reference traceability

TESTS must preserve exact mapping from PLAN references.

| Reference | Milestone | Primary test family |
|---|---|---|
| IA-REF-001 | M4A | TAC-IA-001/002 |
| IA-REF-002 | M4B | TAC-IA-003/004 |
| IA-REF-003 | M2/M4C | TAC-IA-005/006 + TAC-TX |
| WG-REF-001 | M2/M5A | TAC-WG-001/002 + TAC-TX |
| WG-REF-002 | M5B | TAC-WG-003..005 |
| WG-REF-003 | M5C | TAC-WG-006..009 |
| WM-REF-001 | M6A | TAC-WM-001 |
| WM-REF-002 | M6B | TAC-WM-002..004 |
| WM-REF-003 | M6C/M6D | TAC-WM-005/006 |
| WM-REF-004 | M6E | TAC-WM-007..009 + GATE-008A |
| DC-REF-001 | M7B | TAC-DC-001..003 |
| DC-REF-002 | M7A | TAC-DC-003 |
| DC-REF-003 | M7C | TAC-DC-004..007 |
| DC-REF-004 | M7D | TAC-DC-008 |
| AI-REF-001 | M6E/M8A | TAC-AI-001/002 + GATE-008A |
| AI-REF-002 | M8D-M8G | TAC-AI-009..013 |
| AI-REF-003 | M8B/M8C | TAC-AI-003..008 + GATE-008B/C/D |
| AI-REF-004 | M8H/M8I | TAC-AI-014..025 + GATE-008F |
| BI-REF-001 | M9B | TAC-BI-001..005 + GATE-008G |
| BI-REF-002 | M9C | TAC-BI-006..008 |
| BI-REF-003 | M9D | TAC-BI-009/010 |
| AR-REF-001 | M10 | TAC-AR-001..013 |

# 173B. Full milestone coverage

## M0/M1

```text
47-flow inventory/card coverage
dual-model Calendar inventory
event tenant-envelope inventory
Common signature inventory
Billing capacity semantic inventory
```

## M2

```text
BOUND-TX-002
BOUND-TX-004
BILL-LIMIT-001
CAL-CONN-001
Mention actor authority
Page permission semantic
Billing capacity consistency
```

## M3

```text
TAC-GATE-022
TAC-GATE-023
TAC-GATE-024
TAC-GATE-025
```

## PRE-M4 execution checkpoint

```text
47 Flow Cards coarse interaction review
TAC-GATE-010 interaction mechanism closure
IA-FLOW-01..06 deep interaction authority
BOUND-TX-002/004 extraction-blocker metadata
```

This is not a TAC milestone and requires no dedicated CI job.

## M4

```text
IA-FLOW-01..06
IA interaction-edge matrix 172B1
```

## M5

```text
WG-FLOW-01..06
```

## M6

```text
WM-FLOW-01..05
```

## M7

```text
DC-FLOW-01..07
actor + tenant exactness
```

## M8

```text
AI-FLOW-01..07
Calendar persistence/lifecycle/webhook
```

## M9

```text
BI-FLOW-01..04
last-slot concurrency
```

## M10

```text
AR-FLOW-01..04
```

## M11

```text
PF-FLOW-01..07
event compatibility for schema migrations
```

## M12/M13/M14

```text
cross-pack runtime proofs
real traceability audit
exact-SHA closure
```
# 174. Focused milestone gates

## M2

```text
TAC-TX
AcceptInvitation behavior
```

## M3

```text
TAC-GATE
Architecture.Tests
```

## PRE-M4 execution checkpoint

```text
TAC-GATE-010
authority/traceability audit for IA-FLOW-01..06
```

No dedicated CI lane.

## M4

```text
TAC-IA
TAC-TX for BOUND-TX-002/004
TAC-PF runtime proof for IA-FLOW-03
```

## M5

```text
TAC-WG
```

## M6

```text
TAC-WM
```

## M7

```text
TAC-DC
```

## M8

```text
TAC-AI
```

## M9

```text
TAC-BI
```

## M10

```text
TAC-AR
```

## M11

```text
TAC-PF
```

## M12

```text
TAC-XPK
full Architecture.Tests
```

---

# 175. Test naming guidance

Prefer names describing behavior/invariant.

Good:

```text
DuplicateWorkEvent_DoesNotCreateSecondAutomationExecution
OutOfOrderWorkEvent_DoesNotRegressAnalyticsProjectionRevision
AutomationWorkAdapter_DoesNotDependOnWorkManagementDbContext
BillingCapabilityContract_DoesNotExposePlanTier
```

Avoid:

```text
ReferenceTest1
ArchitectureBootstrapWorks
HappyPath
```

---

# 176. Test data ownership

Fixtures must create state through owning context builders/helpers.

Do not create foreign aggregate graphs simply because shared DbContext exposes
all entities.

Integration tests may seed directly only according to existing test
infrastructure conventions, but assertions must preserve semantic ownership.

---

# 177. Test doubles

Allowed:

```text
provider fake
Port fake
clock
current actor
message delivery fake/harness
```

Not allowed as final reference evidence:

```text
fake production adapter
fake producer that bypasses owner Domain
```

Cross-pack integration must exercise real production reference adapters where
feasible.

---

# 178. Error contract tests

Every Public action/Port reference should cover expected business errors.

Do not overfreeze error text.

Prefer:

```text
error code/type
semantic category
```

according to repository conventions.

---

# 179. Correlation tests

For event→process→action chain, assert current correlation metadata survives
enough to trace the flow.

Do not assert implementation-specific trace IDs if not part of architecture.

---

# 180. Test evidence record

Each mandatory reference row should record:

```text
ReferenceId
FlowId
EdgeId when cross-BC
Mechanism when cross-BC
DecisionStatus
ProductionType
TestClass
TestMethod/Filter
ArchitectureGate
IntegrationTest
RuntimeOwnerProof
DIProof
ExtractionBlocker when applicable
CandidateSHA
Result
```

---

# 181. Final exact-SHA evidence table

Required:

| Proof | Result | SHA |
|---|---|---|
| build | | |
| ARCH-BC-001..008 | | |
| STN-ARCH-001/002/005/006/007/008 | | |
| Public gate self-tests | | |
| Services baseline self-tests | | |
| Interaction mechanism/edge authority audit | | |
| IA pack | | |
| WG pack | | |
| WM pack | | |
| DC pack | | |
| AI pack | | |
| BI pack | | |
| AR pack | | |
| PF pack | | |
| flagship Work→Automation | | |
| Work→Analytics | | |
| invitation chain | | |
| Billing consumer | | |
| ResourceRef chain | | |
| provider chain | | |
| backend CI | | |

No stale SHA.

---

# 180A. v2.6 Team Operating Framework checklist

```text
[ ] 47 Flow IDs present
[ ] 46 executable flows have real evidence sections
[ ] BI-FLOW-05 correctly classified
[ ] every Flow Card has FlowKind + SupportingShapes + Interactions[]
[ ] every actual cross-BC edge is TAC-XC-A..F
[ ] no supporting shape is misclassified as a seventh mechanism
[ ] PRE-M4 IA edge matrix matches revised SPEC
[ ] source operating pattern structural references match current source
[ ] CrossContext runtime bindings have dedicated composition ownership
[ ] M6/M10 debt implementations are explicitly NON-REFERENCE
[ ] log-only/stub consumers cannot satisfy Real Flow reaction proof
[ ] only VERIFIED flows are canonical copy-models

[ ] IA-FLOW-06 Accounts RenameAccount verified
[ ] Common signature purity gate passes
[ ] AccountRole/WorkspaceRole absent from canonical Common public signatures

[ ] scoped-event contract gate passes
[ ] PF-FLOW-07 runtime tenant restoration passes
[ ] changed event schemas have compatibility/replay evidence

[ ] Mention actor is authoritative
[ ] MentionedByUserId is not copied from MentionedId
[ ] Notification actor and tenant exact
[ ] no Guid.Empty scoped tenant

[ ] ConnectCalendar persists Connection + SecretVersion + CalendarIntegration
[ ] Calendar secret reference survives DB reload
[ ] duplicate callback/binding tests pass
[ ] Disconnect starts at CalendarIntegration
[ ] CAL-CONN-001 policy tests pass
[ ] inbound webhook uses provider verification
[ ] inbound intake does not reuse outbound WebhookDelivery

[ ] BILL-LIMIT-001 resolved
[ ] zero/unlimited expected semantic explicitly tested
[ ] BI-FLOW-02 hard-capacity last-slot race passes
[ ] concurrent CreateAutomationRule last-slot race passes
[ ] feature failure releases/compensates capacity
[ ] AUTOMATION_RULE is not mislabeled billable metering

[ ] every row in 172B points to an existing substantive heading
[ ] no fake traceability labels
[ ] exact candidate SHA captured
[ ] exact-SHA runtime/CI evidence green
```
# 181A. v2.6 blocker checklist

```text
[ ] IA-FLOW-02 remains BOUND-TX-004 current-local-atomic
[ ] IA-FLOW-05 remains BOUND-TX-002 current-local-atomic
[ ] both IA extraction blockers have explicit RemovalTrigger
[ ] IA-FLOW-04 direct Public does not require artificial Port/remote adapter
[ ] IA-FLOW-03 proves producer commit independent of later consumer failure

[ ] N8n retry integration runs real dedup filter
[ ] retryable failure does not falsely persist rolled-back Automation retry state
[ ] unknown N8n outcome is not blind-retried

[ ] Automation production MoveItem executor exists
[ ] Work target enforces WorkspaceId
[ ] Work target authorizes ExecutorUserId
[ ] Work target deduplicates OperationId
[ ] duplicate/conflicting operation tests pass

[ ] Billing enforces workspace entitlement target scope
[ ] AutomationRule create writes usage +1
[ ] usage writes are idempotent
[ ] capacity-release semantics classified/implemented
[ ] rule+usage consistency failure tested

[ ] Analytics projection outside Domain
[ ] WorkManagement Application implements IWorkItemProjectionSource
[ ] Analytics adapter has no IWorkManagementDbContext
[ ] Analytics rebuild uses producer Public implementation
```

Any unchecked item blocks pack freeze.

# 182. Mandatory final checklist

```text
[ ] exact SHA captured
[ ] all 11 BCs inventoried
[ ] 8 mandatory packs present

[ ] ARCH-BC-005 exact Common allowlist
[ ] ARCH-BC-005 foreign Public denied
[ ] ARCH-BC-008 Common no-growth
[ ] STN-ARCH-006 monotonic exact baseline
[ ] ARCH-BC-006 provider/Broker purity
[ ] TAC-GATE-010 interaction edge taxonomy PASS
[ ] all current-pack cross-BC edges have FlowId:EdgeId evidence
[ ] no flat one-mechanism-per-Flow assumption
[ ] no fake remote service required for semantic-only TAC-XC-A
[ ] extraction-blocked TAC-XC-E flows are not falsely certified remote-ready

[ ] IA pack PASS
[ ] WG pack PASS
[ ] WM pack PASS
[ ] DC pack PASS
[ ] AI pack PASS
[ ] BI pack PASS
[ ] AR pack PASS
[ ] PF pack PASS

[ ] target mutation Workspaces→Accounts PASS
[ ] target mutation Automation→Work PASS
[ ] Work Integration Event PASS
[ ] Workspace membership event PASS
[ ] Automation Process PASS
[ ] Analytics Projection PASS
[ ] Analytics rebuild PASS
[ ] Collaboration ResourceRef PASS
[ ] Billing capability consumer PASS
[ ] Governance authoritative contract PASS
[ ] Integrations provider Port/adapter PASS
[ ] N8n behavior PASS
[ ] background actor/security PASS
[ ] transaction decision PASS

[ ] no mandatory DEFER
[ ] no fake example namespaces
[ ] no placeholder folders
[ ] no production NotImplemented reference
[ ] DI PASS
[ ] integration PASS
[ ] architecture PASS
[ ] backend CI exact SHA PASS
```

---

# 183. BLOCKER rules

Final tests are BLOCKED if:

```text
Accounts↔Workspaces transaction unresolved
mandatory producer semantic cannot be defined safely
reference Process workflow owner unclear
reference Projection rebuild source unclear
background actor authorization undefined
Billing capability semantic ownership unclear
Governance permission contract would leak private role model
provider boundary cannot separate Application from SDK
current-pack interaction mechanism/owner remains ambiguous
coding would require choosing sync vs event vs process
extraction-blocked transaction is being changed without reopened decision
```

A blocker requires design resolution.

It does not convert the mandatory pack to defer.

---

# 184. What counts as test completion

A test file being written is not completion.

A mandatory reference is complete only when:

```text
production source exists
+
source is canonical
+
behavior tests execute
+
negative architecture rules protect it
+
runtime wiring resolves
+
cross-pack integration passes where applicable
```

---

# 184A. Interaction Architecture test-authority boundary

The final active authority is split deliberately:

```text
SPEC
→ mechanism semantics + Flow Card Interactions[]

PLAN
→ execution order + pack-entry decision freeze

TESTS
→ behavior/runtime/negative proof

CERTIFICATION
→ closure criteria

STATUS
→ evidence/state only
```

This TESTS file must not re-design a `DEFERRED-*` interaction decision.
It may only state the proof required once the owning SPEC/PLAN decision is
closed.

No standalone interaction working-plan file is required for final test
authority.

---

# 185. Final testing principle

v2.6 must reject six false-closure classes.

## A — Illegal boundary

```text
foreign DbContext/aggregate/private Application dependency
```

→ FAIL.

## B — Missing real flow

```text
mandatory handler remains stub/NotImplemented
BC lacks its same-case copy-model
```

→ FAIL.

## C — False runtime proof

```text
test bypasses production transaction/filter/auth/dedup/concurrency owner
```

→ FAIL.

## D — False semantic authority

```text
Calendar binding certified only by IntegrationConnection
CurrentSecretRef certified without persisted SecretVersion
inbound webhook certified with outbound WebhookDelivery
Mention actor inferred from mentioned user
capacity certified as billable usage by naming
```

→ FAIL.

## E — False traceability

```text
FlowId points to nonexistent test label
ID appears in table but no substantive evidence exists
```

→ FAIL.

## F — False interaction closure

```text
one multi-edge Flow is labeled with one mechanism
supporting shape is treated as TAC-XC-G
TAC-XC-A is failed merely because no consumer Port exists
BOUND-TX-002/004 is labeled remote-ready despite shared-transaction dependency
Integration Event proof bypasses real outbox/dedup owner
Process proof is only an immediate event handler with no durable state
```

→ FAIL.

Desired state:

```text
correct owner
correct persisted authority
correct runtime owner
correct concurrency
correct evidence
cannot be absent or bypassed
```

# 186. Current candidate proof override — PR #158 re-audit

Historical test rows remain historical evidence. Current proof must apply these
boundaries:

```text
PF-FLOW-05 tests select recovery cases from the declared event-family
capability. Checkpoint/resume/retained-payload/dedup cases are mandatory only
for a family explicitly declared ReplayableSameSchema (or an equivalent
replay-capable policy with an executable source). DrainBeforeCutover and
RebuildFromAuthority families must not be made green with synthetic payloads.

AI-FLOW-07 tests must prove the exact target-owned semantic effect and success,
not only receipt, signature verification, tenant derivation, or
provider-neutral Application processing. Until that target decision exists,
the terminal proof state is BLOCKED-DECISION.
```

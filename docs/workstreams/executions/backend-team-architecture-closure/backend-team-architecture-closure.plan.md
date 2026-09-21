---
document_id: EXEC-BACKEND-TEAM-ARCHITECTURE-CLOSURE-PLAN
title: Backend Team Operating Architecture Bootstrap & Closure — Execution Plan
version: 2.6
status: execution-ready
supersedes: version 2.5
repository: Nqv1208/Notrelix
branch: architecture/backend-boundary-execution
audit_snapshot:
  commit: b94312b3e10e2212f18440003e775d415aa1b5d7
  pr: 113
audit_revision: interaction-architecture-normalized-source-pattern-plan-v2.6-public-capability-topology
depends_on:
  - backend-team-architecture-closure.v2.6.spec.md@2.6
  - backend-boundaries
  - backend-structural-topology
---

# Backend Team Operating Architecture Bootstrap & Closure — Execution Plan

## 0. Plan intent

This PLAN executes SPEC v2.6.

The execution unit remains:

```text
Team
→ owned Bounded Context
→ Real Flow Catalog
→ Flow Card
→ production implementation
→ runtime-owner proof
→ architecture fitness
→ certification
```

v2.6 adds one stronger condition:

```text
correct boundary shape
≠ correct semantic authority
```

The implementation must preserve the exact authority frozen in the Flow Card.

The current-candidate SPEC additionally normalizes Real Flow interaction edges:

```text
Real Flow
→ zero/one/many Interactions[]
→ each actual cross-BC edge classified exactly TAC-XC-A..F
```

PLAN does not redefine those mechanisms. It schedules their implementation and
proof.

For every pack:

```text
global Flow/edge mechanism classification
→ current-pack interaction deep-freeze
→ production implementation
→ runtime-owner proof
```

A later-pack interaction may remain `DEFERRED-{MILESTONE}` for deep runtime /
failure semantics, but its mechanism and semantic owners may not remain
unclassified.

New mandatory closure areas:

```text
TAC-FRZ-017 Common public-signature semantic purity
TAC-FRZ-018 scoped Integration Event tenant envelope
TAC-FRZ-019 Calendar connection/binding/secret/webhook lifecycle
TAC-FRZ-020 Billing capacity semantics + hard-quota concurrency
```

The sequence is:

```text
exact source
→ semantic decision freeze
→ Flow Card
→ owner-side production implementation
→ transaction/concurrency/runtime proof
→ architecture gate
→ exact-SHA certification
```

No STOP decision may be replaced by coding-agent preference.

---
# 1. Global execution sequence

```text
TAC-M0   Exact SHA + semantic/source inventory
   ↓
TAC-M1   Freeze 47-flow catalog + Flow Cards
   ↓
TAC-M2   Freeze decisions/consistency
   ├─ BOUND-TX-002 AcceptInvitation
   ├─ BOUND-TX-004 Register→Accounts
   ├─ Billing capacity consistency
   ├─ BILL-LIMIT-001 zero/unlimited semantics
   ├─ CAL-CONN-001 Calendar generic-Connection cleanup
   ├─ Mention actor authority
   └─ Page permission semantic where required
   ↓
TAC-M3   Architecture/runtime gates
   ├─ TAC-FRZ-017 Common signature purity
   ├─ TAC-FRZ-018 scoped-event tenant envelope
   ├─ TAC-FRZ-019 Calendar lifecycle boundaries
   └─ TAC-FRZ-020 Billing capacity/concurrency
   ↓
PRE-M4   Interaction Architecture normalization gate
         (execution checkpoint only; NOT a milestone)
   ├─ coarse-classify all 47 Flow Cards / cross-BC edges
   ├─ deep-freeze IA-FLOW-01..06 interactions
   └─ preserve BOUND-TX-002/004 extraction blockers
   ↓
TAC-M4   Identity & Accounts
TAC-M5   Workspace & Governance
TAC-M6   Work Management
TAC-M7   Documents & Collaboration
TAC-M8   Automation & Integrations
TAC-M9   Billing & Entitlements capacity
TAC-M10  Analytics & Reporting
TAC-M11  Platform/Foundation + tenant envelope
   ↓
TAC-M12  Cross-pack production integration
   ↓
TAC-M13  Team-copy + stale/false-reference cleanup
   ↓
TAC-M14  Exact-SHA certification
```

Primary dependency:

```text
semantic decision frozen
+
Flow Card frozen
before implementation
```

---
# 2. Execution rules

## 2.1 Source-first, but not source-only

Every milestone starts by reading current source.

But absence of a required reference implementation does not end the milestone.

If missing:

```text
design smallest valid reference
→ implement it
→ test it
→ freeze it
```

## 2.2 No placeholder implementation

Forbidden:

```text
interface only
empty DTO only
fake in-memory success implementation in production
throw NotImplementedException
empty folder
```

## 2.3 Reuse accepted business semantics

Reference code must derive from accepted context relationships.

For the pinned bootstrap references, reuse:

```text
existing Work mutation
existing Workspace membership lifecycle
existing comment/resource model
existing N8n/Calendar provider flow
existing Billing domain concepts
```

over inventing new product concepts.

## 2.4 Smallest complete slice

Each **interaction edge** should prove one canonical mechanism completely.

A Real Flow may compose several edges/mechanisms. Do not flatten a multi-edge
workflow into one artificial mechanism merely to simplify implementation.

Do not implement a full future product roadmap.

## 2.5 Hardening before proliferation

Before introducing many Public surfaces/events:

```text
ARCH-BC-005
ARCH-BC-006
ARCH-BC-008
STN-ARCH-006
```

must be hardened sufficiently to catch invalid new reference code.

## 2.6 One architecture owner per property

Reuse existing gates.

Do not create parallel gate systems.

## 2.7 Mandatory Real Flow disposition gate

Before coding each `*-FLOW-*`, record exactly one:

```text
REUSE-EXISTING
HARDEN-EXISTING
MIGRATE-EXISTING
IMPLEMENT-MISSING
FREEZE-EXACT-DEBT
BLOCKED-DECISION
BLOCKED-SOURCE-DRIFT
NOT-APPLICABLE
```

Rules:

```text
REUSE
→ current production flow already satisfies Flow Card

HARDEN
→ flow exists but misses proof/invariant

MIGRATE
→ semantic flow exists but boundary/topology is wrong

IMPLEMENT-MISSING
→ accepted Product/Domain semantics exist but executable flow does not

BLOCKED-DECISION
→ product/semantic decision is missing
```

A `NotImplementedException` handler is never `REUSE-EXISTING`.

A `*StubConsumer*` or log-only consumer is never business-flow evidence.

`NOT-APPLICABLE` cannot satisfy a mandatory BC-local baseline.
## 2.8 No-free-choice execution rule

Mandatory flows use the SPEC v2.6 pinned catalog.

Pinned examples:

```text
IA-FLOW-01 UpdateProfile
IA-FLOW-02 Register→Accounts provisioning

WG-FLOW-01 CreateWorkspace
WG-FLOW-04 GrantResourcePermission

WM-FLOW-01 CreateBoardInWorkspace
WM-FLOW-03 Work Public MoveItem

DC-FLOW-01 CreatePage
DC-FLOW-02 ArchivePage
DC-FLOW-03 CreateComment.ForBoardItem
DC-FLOW-04 CreateComment.ForPage

AI-FLOW-01 CreateAutomationRule
AI-FLOW-05 ConnectCalendar
AI-FLOW-06 DisconnectCalendar
AI-FLOW-07 Calendar webhook

BI-FLOW-02 Billing usage ingestion

AR-FLOW-01 live placement projection
AR-FLOW-03 projection rebuild

PF-FLOW-03 tenant restoration + dedup
```

If pinned source disappeared:

```text
STOP-SOURCE-DRIFT
```

If it exists only as a stub but accepted semantics exist:

```text
IMPLEMENT-MISSING
```

Do not replace it with an easier unrelated feature.
## 2.8A Runtime-proof ownership rule — v2.4

A coding agent must test the layer/mechanism that actually owns the guarantee.

Forbidden false-green pattern:

```text
production:
  outer runtime transaction/filter
    → application/adapter

test:
  application/adapter directly
  → manual SaveChanges
```

when the outer runtime can change commit/rollback semantics.

Mandatory runtime proofs:

```text
N8n retry
→ real DeduplicationConsumeFilter transaction

Automation→Work
→ production Automation action executor
→ target Public action
→ target auth/scope/idempotency

Billing
→ real AutomationRule lifecycle
→ Billing usage lifecycle
→ next capability decision

Analytics
→ producer-owned Work Public implementation
→ consumer adapter
→ projection/rebuild
```

Lower-level tests remain useful but cannot be the sole milestone exit proof.

## 2.8B No ignored Public semantic fields

When a Public request declares:

```text
OperationId
WorkspaceId
ExecutorUserId
```

the implementation must either:

```text
use/enforce each field
```

or:

```text
remove the field because it is not part of the contract
```

Accepting a semantic field and ignoring it is a milestone failure.

## 2.8C BC-local completeness rule — v2.6

Every business BC must have at least one verified local owner-side flow:

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

A sibling BC flow cannot satisfy this requirement.

## 2.8D Flow-variant rule — v2.6

Create separate implementation/test scenarios when one handler's variants differ by:

```text
ResourceKind
resource owner
PermissionAction
scope
foreign fact source
mutation authority
failure semantics
```

Pinned case:

```text
CreateComment.ForBoardItem
CreateComment.ForPage
```

## 2.8E Stub/observability classification rule — v2.6

Inventory every:

```text
*Stub*
NotImplementedException
log-only consumer
```

Classify:

```text
mandatory missing flow → IMPLEMENT-MISSING
intentional observability → observability-only
obsolete fake reference → remove after replacement
```

None may silently count as Real Flow evidence.

## 2.8F Semantic-authority execution rule — v2.6

When two related models exist, record lifecycle ownership explicitly.

Pinned Calendar case:

```text
IntegrationConnection
→ generic provider relationship

CalendarIntegration
→ Workspace Calendar product binding
```

Do not merge them because one is easier to persist.

## 2.8G Persistence-authority rule — v2.6

A Domain property is not automatically durable authority.

For Calendar credentials:

```text
IntegrationSecretVersion
→ persisted SecretReference

IntegrationConnection.CurrentSecretVersion
→ current version/pointer
```

Do not certify a flow from in-memory `CurrentSecretRef` alone.

## 2.8H Scoped-event rule — v2.6

Tenant-scoped business event invariant:

```text
WorkspaceId != null
→ AccountId != null
```

The producer must establish this before Platform delivery.

## 2.8I Hard-quota rule — v2.6

Billing must prove:

```text
remaining = 1
2 concurrent reserve/create requests
→ at most 1 succeeds
```

Sequential idempotency tests are insufficient.

## 2.8J Common signature purity — v2.6

`Application/Common` public signatures cannot expose BC business types.

`AccountRole`/`WorkspaceRole` in a Common contract is a failure even when the
contract name/namespace appears technical.

## 2.8K Interaction-edge execution rule — current candidate

The SPEC `Interactions[]` model is normative.

Before implementing a Flow Card:

```text
1. enumerate actual cross-BC edges;
2. classify each edge exactly TAC-XC-A..F;
3. confirm direction and semantic owners;
4. honor DecisionStatus;
5. implement only after the current-pack edge is sufficiently frozen.
```

A Flow Card may contain:

```text
local owner work
+
TAC-XC-A authoritative read
+
TAC-XC-E target mutation
+
TAC-XC-C outward event
```

without any contradiction.

`Local`, `ResourceRef`, provider Port and Platform delivery are supporting
shapes/roles, not alternative TAC-XC mechanism values.

## 2.8L Cross-process readiness / extraction-blocker rule — current candidate

Do not use one boolean `service-ready`.

Every cross-BC edge must distinguish:

```text
SemanticBoundaryReady
RuntimeSubstitutionReady
ExtractionBlocked
```

Rules:

```text
TAC-XC-A
→ may be semantic-ready while remote binding remains deferred

TAC-XC-B
→ adapter-ready only when the real consumer Port and thin runtime adapter exist

TAC-XC-C/D/F
→ distributed-friendly only after the real Platform/process semantics are proven

BOUND-TX-002 / BOUND-TX-004
→ clean semantic boundary
→ ExtractionBlocked = true while current shared atomicity remains authoritative
```

A coding agent MUST NOT convert a shared local transaction to eventual
consistency merely to make the edge look distributable.

## 2.8M No transport-first implementation — current candidate

The execution order for a cross-process-capable edge is:

```text
business need
→ TAC-XC mechanism
→ semantic contract / Port / ACL
→ current runtime binding
→ failure/consistency semantics
→ only then transport if physical extraction actually occurs
```

This execution does not require fake remote services, fake HTTP/gRPC adapters,
or broker-command facades to prove future extraction.

## 2.9 Team execution precedence

Existing detailed team executions remain authoritative for full product/team
delivery. TAC milestones only own the mandatory architecture-reference delta
and cross-team closure proof.

For Identity/Accounts and Workspace/Governance in particular:

```text
reuse existing team execution decisions/tests
→ do not fork a second team roadmap in TAC
```

---

# 3. Workstreams

## Lane A — Semantic/transaction decisions

Owns:

```text
M0/M1 source + Flow Cards
Interaction[] mechanism/owner decisions
current-pack deep interaction freeze
BOUND-TX-002
BOUND-TX-004
Billing capacity consistency
BILL-LIMIT-001
CAL-CONN-001
Mention actor authority
Page permission semantics
```

## Lane B — BC-local baselines

Adds/owns:

```text
Identity UpdateProfile
Accounts RenameAccount
Accounts Public provisioning
Workspaces CreateWorkspace
Governance Grant/Get permission
Work CreateBoard
Documents Create/Archive Page
Collaboration Comment
Automation CreateRule
Integrations Calendar connect/disconnect
Billing capacity mutation
Analytics local query/projection
```

## Lane C — Cross-context/process/provider

Owns:

```text
same-process binding → future distributed substitution model
Register→Accounts→Workspace
AcceptInvitation
Work→Automation
Automation→Work
Work→Analytics
ResourceRef variants
Activity/Notification
N8n
Calendar secret/provider flow
Calendar verified webhook
Billing capacity caller
Analytics rebuild
```

## Lane D — Runtime/enforcement

Owns:

```text
Public purity
Common signature purity
provider/broker purity
request/outbox
scoped-event envelope
tenant restoration
dedup
retry/failure
compatibility/replay
```

## Lane E — Closure

Owns:

```text
cross-pack integration
stale authority cleanup
traceability validation
exact-SHA certification
```

---
# 4. TAC-M0 — Exact baseline and semantic/source inventory

Pinned audit baseline:

```text
Repository: Nqv1208/Notrelix
PR: #113
Branch: architecture/backend-boundary-execution
SHA: b94312b3e10e2212f18440003e775d415aa1b5d7
```

Re-read HEAD at execution start. Drift → `STOP-SOURCE-DRIFT` and rerun M0 delta.

## M0.1 Real Flow inventory

Required IDs:

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

Total:

```text
47 IDs
46 executable/verification-required
BI-FLOW-05 conditional
```

## M0.2 Calendar semantic/persistence inventory

Record roles for:

```text
IntegrationConnection
CalendarIntegration
IntegrationSecretVersion
WebhookDelivery
InboundWebhookEvent
```

For each capture:

```text
semantic owner
lifecycle
persistence authority
architecture classification
use in AI-FLOW-05/06/07
```

## M0.3 Event tenant-envelope inventory

Enumerate every Integration Event with Workspace scope.

Record:

```text
AccountId present?
producer mapper sets it?
authoritative source?
TenantContextConsumeFilter result?
```

Workspace-scoped business event with missing AccountId = migration blocker.

## M0.4 Mention actor inventory

Record actor source from creation → Domain fact → outward mapping → consumers.

Current `MentionedId` cannot stand in for mention creator.

## M0.5 Common signature inventory

Inspect `Application/Common/**` public signatures.

Pinned debt:

```text
IAccessGrantProjectionService
→ AccountRole
→ WorkspaceRole
```

## M0.6 Billing semantic inventory

Record:

```text
Entitlement limit representation
zero-limit current behavior
unlimited representation
WorkspaceFeatureUsage
FeatureUsageLedger
commercial UsageMetric models
concurrency mechanism/constraints
```

Do not merge these authorities by naming.

## M0.7 Accounts local-admin inventory

Confirm Account Domain rename semantics and current absence/presence of an
authenticated Accounts Application command.

Pinned new reference:

```text
IA-FLOW-06 RenameAccount
```

## Exit

M0 PASS only when all 47 flows and all v2.6 semantic ambiguities are classified.

---
# 5. TAC-M1 — Freeze 47-flow catalogs and Flow Cards

Required ranges:

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

## New v2.6 Flow Cards

### IA-FLOW-06

```text
Accounts RenameAccount
local authenticated Account-admin mutation
```

Freeze Account scope, actor, Governance permission, Domain rename behavior,
concurrency and event/outbox semantics.

### PF-FLOW-07

```text
scoped Integration Event tenant envelope
```

Freeze AccountId/WorkspaceId invariant, Global/System classification, producer
mapping and TenantContextConsumeFilter proof.

## Correct existing cards

AI-FLOW-05:

```text
IntegrationConnection + IntegrationSecretVersion + CalendarIntegration
```

AI-FLOW-06:

```text
CalendarIntegration.Deactivate first
CAL-CONN-001 controls generic Connection cleanup
```

AI-FLOW-07:

```text
Infrastructure inbound receipt/dedup
not outbound WebhookDelivery
not automatic InboundWebhookEvent Domain promotion
```

BI-FLOW-01..04:

```text
capacity semantics
zero/unlimited decision
hard-quota concurrency
reservation/consume/release
not automatically billable UsageMetric
```

DC-FLOW-06:

```text
authoritative mention actor
authoritative tenant envelope
```

## Exit

M1 PASS when all 47 Flow Cards contain semantic/persistence authority,
decision dependencies and runtime proof owner.

---
# 6. TAC-M2 — Transaction, lifecycle and semantic decision freeze

## M2A — BOUND-TX-002

Preserve reviewed AcceptInvitation Accounts↔Workspaces local transaction.

## M2B — BOUND-TX-004

Preserve reviewed Register Identity↔Accounts local transaction.

## M2C — Billing capacity consistency

Freeze one:

```text
BOUND-TX-003
or
IDEMPOTENT-CAPACITY-PROTOCOL
```

Must define reserve → feature mutation → commit/consume → release/compensate,
retry and duplicate/conflict semantics.

## M2D — BILL-LIMIT-001

Product/architecture authority decides:

```text
what numeric zero means
how unlimited is represented
```

Until resolved:

```text
STOP-BILLING-LIMIT-SEMANTIC
```

## M2E — CAL-CONN-001

Decide generic Connection behavior when Calendar binding is deactivated:

```text
A retain generic Connection
or
B disconnect/revoke when policy conditions are satisfied
```

Remaining active bindings/capabilities must be part of the decision.

Until resolved:

```text
STOP-CALENDAR-CONNECTION-LIFECYCLE
```

## M2F — Mention actor authority

Freeze producer-side creator/mentioner actor source.

Mapper/consumer cannot invent it.

## M2G — Page permission semantic

If Page flow currently reuses Board-specific vocabulary without authority:

```text
STOP-AUTHZ-SEMANTIC
```

Freeze correct Governance action.

## Exit

All decisions above must be frozen before dependent implementation milestones.

---
# 7. TAC-M3 — Foundation gate hardening

This milestone should happen before adding many new reference contracts.

---

# 8. TAC-M3A — ARCH-BC-005 Public purity

## Tasks

Modify existing gate owner.

Required target behavior:

```text
System.* allowed
exact approved technical Common types allowed
own Producer.Public allowed
foreign Producer.Public denied by default
arbitrary Common denied
Domain denied
DbContext/repository denied
Infrastructure/Platform/API denied
transport/provider denied
internal Application denied
```

## Self-tests

Required:

```text
own Public PASS
foreign Public FAIL
approved Common PASS
unapproved Common FAIL
Domain FAIL
HttpClient FAIL
MassTransit FAIL
```

## Exit

```text
[ ] no broad Common allowance
[ ] no broad Public allowance
[ ] self-tests green
```

---

# 9. TAC-M3B — STN-ARCH-006 Services monotonic baseline

## Tasks

Convert:

```text
actual ⊆ allowed
```

to exactly:

```text
actual == reviewed baseline
```

The reviewed baseline must shrink in the same change when a legacy file is
removed.

## Regression cases

```text
delete source without shrinking baseline → FAIL
shrink baseline → PASS
recreate deleted file → FAIL
```

## Exit

```text
[ ] no stale reusable allowance
```

---

# 10. TAC-M3C — Common semantic no-growth

## Tasks

Inventory exact:

```text
Application/Common/**
```

Classify:

```text
technical
business debt
remove now
```

Harden existing `ARCH-BC-008`.

## Exit

```text
[ ] new business semantic Common types fail
[ ] current exact debt baselined
```

---

# 11. TAC-M3D — Broker/provider purity

Reuse `ARCH-BC-006`.

Ensure Application rejects direct:

```text
MassTransit
RabbitMQ client
HttpClient
gRPC
provider SDK
```

Reference provider/broker implementations must land in Infrastructure/Platform.

---

## TAC-M3H — TAC-FRZ-017 Common public-signature purity

Architecture tests inspect Common public signature dependencies:

```text
parameters
returns
properties/fields
generic arguments
base/interfaces
```

Pinned cleanup:

```text
IAccessGrantProjectionService
must stop exposing AccountRole/WorkspaceRole through Common
```

## TAC-M3I — TAC-FRZ-018 scoped-event tenant-envelope gate

Rule:

```text
Workspace-scoped business event
→ authoritative AccountId + WorkspaceId
```

Tests must catch both missing contract field and mapper that leaves AccountId
null.

## TAC-M3J — TAC-FRZ-019 Calendar lifecycle gates

Reject:

```text
raw secret in Domain/event/log
Calendar flow with no CalendarIntegration binding
outbound WebhookDelivery used as inbound receipt
provider webhook authenticated only by user session
secret flow without persisted IntegrationSecretVersion authority
```

## TAC-M3K — TAC-FRZ-020 Billing capacity/concurrency gates

Reject:

```text
zero == unlimited magic without resolved decision
hard quota as stale check-then-increment only
capacity treated as billable UsageMetric without authority
consumer PlanTier branching
```

---

# 11A. PRE-M4 — Interaction Architecture normalization gate

This is an **execution checkpoint**, not a new TAC milestone and not a new
certification milestone.

It exists because the revised SPEC makes interaction edges explicit before M4
starts creating/hardening business reference code.

## PRE-M4A — Global coarse classification

Review all 47 Flow Cards.

For every actual cross-BC edge record/confirm:

```text
EdgeId
SourceBC
TargetBC
TAC-XC-A..F mechanism
ContractOwner
WorkflowOwner
MutationOwner / ProjectionOwner / ProcessOwner where applicable
DecisionStatus
known ExtractionBlocker
```

Global coarse classification MUST NOT force later-pack product decisions such
as:

```text
M8 provider compensation
M9 Billing remote consistency
M10 projection-source runtime substitution
```

Those may remain `DEFERRED-{MILESTONE}` where SPEC permits.

## PRE-M4B — Deep-freeze IA-FLOW-01..06

Before M4 production work starts, IA interactions must resolve enough execution
semantics to prevent coding-agent choice.

Pinned:

```text
IA-FLOW-01
→ Local Vertical Slice
→ no cross-BC interaction edge

IA-FLOW-06
→ Local Accounts owner flow
→ Governance authorization through canonical authority/pipeline
→ do not create a fake foreign persistence edge

IA-FLOW-02
→ Identity registration workflow
→ Accounts target-owned provisioning action
→ TAC-XC-E
→ BOUND-TX-004 current-local-atomic
→ ExtractionBlocked = true

IA-FLOW-03
→ Identity committed registration fact
→ Workspaces reaction
→ TAC-XC-C
→ outbox + real Platform delivery + tenant restoration + dedup
→ eventual consumer reaction

IA-FLOW-04
→ Workspaces authoritative Accounts membership/admission read
→ TAC-XC-A
→ RemoteReadiness = semantic-only
→ no fake remote Port is required now

IA-FLOW-05
→ Workspaces invitation workflow
→ Accounts membership target action
→ TAC-XC-E
→ BOUND-TX-002 current-local-atomic
→ ExtractionBlocked = true
```

## PRE-M4C — Interaction implementation authority

For M4, coding agents may choose implementation details only inside the frozen
semantic/runtime envelope.

They MUST NOT choose:

```text
Public vs Port
sync vs event
event vs command
shared transaction vs eventual consistency
Saga vs simple event reaction
remote transport
compensation semantics
```

unless the exact Flow Card leaves that field intentionally implementation-local.

## PRE-M4D — Source Operating Pattern closure

This closes the **source working model** that later Team Packs copy.

It is not a new plan, milestone, TAC mechanism, architecture gate family or
generic framework.

### P0 — M3 prerequisite closure

Before PRE-M4 can PASS:

```text
M3A ARCH-BC-005 repository/mechanism loophole
→ close exact classifier + self-test

M3C ARCH-BC-008 AccessPermissionRule classification
→ exact technical exception OR exact governed business debt
→ one unambiguous classification
```

P0 remains M3 work; PRE-M4 does not absorb it.

### P1 — Pin current structural references

Record current-source teaching shapes:

```text
Local
→ IA-FLOW-01 / IA-FLOW-06 source

TAC-XC-A
→ Accounts.Public IAccountMembershipFacts

TAC-XC-B structural reference
→ Automation IWorkActionPort
→ consumer semantic mapping
→ WorkItemActionAdapter
→ WorkManagement.Public IWorkItemActions

TAC-XC-C candidate
→ IdentityRegistrationCompletedIntegrationEventV1
→ outbox / Platform
→ WorkspaceProvisioningConsumer
→ ProvisionPersonalWorkspaceCommand

TAC-XC-E
→ Accounts.Public target action family
→ IA-FLOW-02 provisioning remains migration-required
```

Do not certify M8 merely because the TAC-XC-B source shape is used as a
structural reference.

### P2 — Normalize CrossContext runtime composition

Move cross-context adapter registrations out of persistence composition.

Target:

```text
Infrastructure/DependencyInjection/CrossContextRegistration.cs
→ AddCrossContextBindings()
```

Move only runtime bindings such as:

```text
IWorkManagementCollaborationReadPort → WorkManagementCollaborationReadAdapter
IIdentityBootstrapReadPort → IdentityBootstrapReadAdapter
IWorkActionPort → WorkItemActionAdapter
projection-source runtime binding → current adapter until M10
```

Do not move:

```text
ApplicationDbContext
I*DbContext → ApplicationDbContext mappings
RLS/data-session persistence
outbox persistence
```

This is source/composition cleanup only; no product semantics change.

### P3 — Mark non-reference implementations

Explicitly prevent copy-from-debt:

```text
WorkManagementCollaborationReadAdapter
→ NON-REFERENCE until M6

WorkItemProjectionSourceAdapter
→ NON-REFERENCE until M10

IdentityBootstrapReadAdapter
→ Supporting Composite Read only

log-only / *StubConsumer*
→ NON-REAL-FLOW
```

### P4 — Extend existing fitness owners

Use existing architecture-test owners; do not create a new gate family.

Required coverage:

```text
ARCH-BC-005
→ repository/mechanism rejected independently of own-Public allowance

ARCH-BC-008
→ AccessPermissionRule exact classification

existing cross-context dependency tests
→ consumer Application cannot use foreign Domain/private Application

existing canonical-path/dependency tests
→ CrossContext runtime bindings live in CrossContextRegistration

existing Real Flow completeness tests
→ log-only/stub consumer cannot satisfy event reaction
```

### P5 — Establish copy eligibility rule

```text
VERIFIED Real Flow
→ canonical copy-model allowed

structural reference
→ may teach only its frozen source invariant

DEFERRED/non-reference source
→ must not be copied
```

PRE-M4 does not require every TAC-XC mechanism to be fully certified.

Expected flagship completion by owning pack:

```text
M4
→ first complete Team Pack
→ Local + TAC-XC-A + TAC-XC-C + TAC-XC-E canonical references

M6
→ TAC-XC-B read/source normalization reference

M8
→ TAC-XC-F Process Manager
→ full Automation TAC-XC-B/process/provider reference

M10
→ TAC-XC-D Projection + rebuild/source reference
```

## PRE-M4 Exit

```text
[ ] all 47 Flow Cards coarse-reviewed
[ ] every actual cross-BC edge classified TAC-XC-A..F
[ ] no system-level mechanism ambiguity remains
[ ] IA-FLOW-01..06 deep interaction decisions are executable
[ ] BOUND-TX-002/004 remain explicit extraction blockers
[ ] source structural references are pinned to current production types
[ ] CrossContext runtime bindings have a dedicated composition owner
[ ] known M6/M10 debt implementations are marked NON-REFERENCE
[ ] log-only/stub consumers are excluded from Real Flow reference eligibility
[ ] only VERIFIED Real Flows may become canonical copy-models
[ ] no universal Port/gateway/cross-context framework introduced
[ ] no artificial remote adapter/service introduced
```

Only then enter TAC-M4 **as a milestone state**.

Repository source may contain early M4 implementation produced before this
interaction-normalized checkpoint. Such source is classified as
`PRE-ENTRY IMPLEMENTATION EVIDENCE`; it may be reused/hardened after the
checkpoint passes, but it does not make TAC-M4 `IN PROGRESS` or `COMPLETE` by
itself.

---

# 12. TAC-M4 — Identity & Accounts Team Operating Pack

Order:

```text
IA-FLOW-01
→ IA-FLOW-06
→ IA-FLOW-02
→ IA-FLOW-03
→ IA-FLOW-04
→ IA-FLOW-05
```

## M4A — IA-FLOW-01 Identity UpdateProfile

Reuse/harden current Identity local flow.

Interaction execution:

```text
FlowKind = Local
Interactions = []
```

Do not create a Port/event merely to make the flow appear distributed-ready.

## M4B — IA-FLOW-06 Accounts RenameAccount

Interaction execution:

```text
FlowKind = Local
primary mutation owner = Accounts
standard Account-admin authorization = canonical Governance/pipeline authority
no foreign private persistence edge
```

Current disposition:

```text
HARDEN-EXISTING
```

The production RenameAccount command/handler/API path now exists on the current
PR branch. Do not implement a second path.

Target/verification shape:

```text
authenticated Account-scoped command
→ Governance Account-admin authorization
→ Accounts Application
→ Account.Rename
→ transaction
→ owner event/outbox if canonical
```

Tasks:

```text
trusted Account scope
actor explicit
no foreign private state
Domain same-name/validation behavior
version/concurrency
rollback
```

Tests:

```text
authorized
unauthorized
wrong Account
same-name/no-op
invalid name
concurrent conflict
rollback
```

## M4C — IA-FLOW-02 Register→Accounts provisioning

Migrate to the capability-first producer surface
`Accounts/Public/PersonalAccountProvisioning/` under BOUND-TX-004. This is a
`SOURCE-RELOCATION`/boundary normalization when the semantic capability already
exists; do not duplicate the capability under a second Public taxonomy.

Pinned interaction:

```text
IA-FLOW-02:E1
Mechanism = TAC-XC-E Target-owned Action
SourceBC = Identity
TargetBC = Accounts
ContractOwner = Accounts
CurrentBinding = direct/in-process Accounts.Public action
ConsistencyModel = immediate
TransactionBoundary = BOUND-TX-004 current shared request transaction
ExtractionBlocker = BOUND-TX-004
FutureDistributedBinding = deferred until transaction/product decision is reopened
```

A clean Public boundary does not authorize replacing the current shared request
transaction with HTTP/eventual provisioning.

Do not build the new reference on Common role-leaking signatures; use the
TAC-FRZ-017-normalized grant boundary.

## M4D — IA-FLOW-03 RegistrationCompleted→Workspace

Pinned interaction:

```text
IA-FLOW-03:E1
Mechanism = TAC-XC-C Integration Event
SourceBC = Identity
TargetBC = Workspaces
ContractOwner = Identity
CurrentBinding = Platform event delivery
ConsistencyModel = eventual
OutboxRequired = true
ConsumerDedupRequired = true
FutureDistributedBinding = broker-event
```

Event must satisfy TAC-FRZ-018 and run through the real outbox, Platform
delivery, tenant restoration and dedup runtime.

Producer registration success does not wait for Workspace consumer completion
after the committed registration fact exists.

## M4E — IA-FLOW-04 membership facts

Producer-owned Accounts Public fact under `Accounts/Public/Membership/`.

Pinned interaction:

```text
IA-FLOW-04:E1
Mechanism = TAC-XC-A Producer Public Direct
SourceBC = Workspaces
TargetBC = Accounts
ContractOwner = Accounts
CallerWaitsForOutcome = yes
ConsistencyModel = immediate
CurrentBinding = direct-in-process
RemoteReadiness = semantic-only
```

Do not introduce a consumer Port solely to simulate future service extraction.
At physical extraction, the runtime-proxy-vs-TAC-XC-B decision is reopened
according to the SPEC.

## M4F — IA-FLOW-05 membership target action

Producer-owned Accounts Public action under `Accounts/Public/Membership/` and BOUND-TX-002.

Pinned interaction:

```text
IA-FLOW-05:E1
Mechanism = TAC-XC-E Target-owned Action
SourceBC = Workspaces
TargetBC = Accounts
ContractOwner = Accounts
CurrentBinding = direct/in-process Accounts.Public action
ConsistencyModel = immediate
TransactionBoundary = BOUND-TX-002 current shared request transaction
ExtractionBlocker = BOUND-TX-002
```

Do not translate this current shared transaction into a remote command/event
workflow during M4.

## Exit

Identity local + Accounts local-admin + Accounts Public-action baselines all
VERIFIED.

Additionally:

```text
IA-FLOW-01..06 interaction decisions match revised SPEC
IA-FLOW-02 BOUND-TX-004 rollback/commit proof passes
IA-FLOW-03 real outbox→Platform→consumer/dedup proof passes
IA-FLOW-04 remains a producer Public Direct teaching case
IA-FLOW-05 BOUND-TX-002 rollback/commit/idempotency proof passes
no M4 handler depends on foreign private persistence/Domain
no fake remote adapter/Saga introduced
Accounts.Public uses capability-first topology for all M4-touched contracts
no top-level Accounts/Public/{Commands,Queries,Facts,Actions,Contracts,DTOs,Services,Common} remains
canonical path/topology architecture proof passes

M4 canonical copy-model:
  Local Vertical Slice → VERIFIED
  TAC-XC-A Public Direct → VERIFIED
  TAC-XC-C Integration Event → VERIFIED
  TAC-XC-E Target-owned Action → VERIFIED
```

---
## Post-M4 pack-entry interaction rule

Before TAC-M5, M6, M7, M8, M9, M10 or M11 begins implementation:

```text
take that pack's `DEFERRED-{MILESTONE}` interaction edges
→ resolve only the semantics required by that pack
→ update SPEC/Flow Card authority
→ update PLAN/TESTS if proof obligations change
→ implement
```

Do not pre-freeze unrelated future product behavior merely because another pack
is starting.

For every new/touched Real Flow:

```text
classify FlowId:EdgeId
→ select matching VERIFIED copy-model
→ copy topology/invariants only
→ implement team semantics
→ prove behavior/runtime/gates
```

If the required mechanism has no VERIFIED canonical reference yet, the owning
milestone must complete that flagship before teams treat another source as the
pattern.

---

# 13. TAC-M5 — Workspace & Governance Team Operating Pack

## M5 order

```text
WG-FLOW-01
WG-FLOW-04
WG-FLOW-05
WG-FLOW-06
WG-FLOW-02
WG-FLOW-03
```

### WG-FLOW-01 — CreateWorkspace

Prove:

```text
Account scope
verified-email requirement
CreateWorkspace permission
WorkspaceFactory.CreateWithOwner
Workspace + owner member atomicity
grant projection
slug DB uniqueness/race
```

Exit: `WG-FLOW-01 VERIFIED`.

### WG-FLOW-04 — GrantResourcePermission

Mandatory variants:

```text
work-management.board
documents.page
```

For each prove ResourceKind, target-specific PermissionAction, scope, granter authority, subject validation, audit, replacement behavior.

If Page permission is guessed/reused incorrectly:

```text
STOP-AUTHZ-SEMANTIC
```

Exit: `WG-FLOW-04 VERIFIED`.

### WG-FLOW-05 — GetResourcePermissions

Governance state only; protected permission-read authorization; no target aggregate join.

Exit: `WG-FLOW-05 VERIFIED`.

### WG-FLOW-06 — canonical authorization pipeline

Pinned protected cases:

```text
MoveBoardItem
CreateComment.ForBoardItem
CreateComment.ForPage
CreatePage
GrantResourcePermission
```

One authorization engine, deny before mutation, correct ResourceKind/scope, explicit background executor.

Exit: `WG-FLOW-06 VERIFIED`.

### WG-FLOW-02 — AcceptInvitation

Normalize:

```text
Identity.Public facts
Accounts.Public facts/action
Invitation.Accept
WorkspaceMember
grant projection
```

Preserve `BOUND-TX-002`.

Test invalid/expired/already-member/duplicate/rollback.

Exit: `WG-FLOW-02 VERIFIED`.

### WG-FLOW-03 — WorkspaceMemberAdded→Activity

Pinned real consumer: `WorkspaceMemberAddedActivityConsumer`.

Prove committed event, Account/Workspace/Actor, outbox, Collaboration projection, dedup.

Exit: `WG-FLOW-03 VERIFIED`.

## M5 exit

Workspaces and Governance both BASELINE-COMPLETE.

---

# 14. TAC-M6 — Work Management Team Operating Pack

### WM-FLOW-01 — CreateBoardInWorkspace

Preserve pipeline-first markers:

```text
auth
workspace scope
permission
idempotency
write transaction
```

Do not Port-ify ordinary scope/auth pipeline.

Exit: verified.

### WM-FLOW-03 — Work Public MoveItem

Target:

```text
IWorkItemActions.MoveItem
→ producer Application/domain mutation
```

Mandatory fields:

```text
AccountId
WorkspaceId
BoardItemId
target placement
ExecutorUserId
OperationId
correlation/causation
```

Prove target auth, workspace enforcement, target-local OperationId dedup, conflicting duplicate failure, one writer.

Exit: verified.

### WM-FLOW-04 — Work Integration Events

Required:

```text
BoardItemMemberAssignedIntegrationEvent
BoardItemMovedIntegrationEvent
```

Producer facts only, versioned, scoped, actor/correlation, outbox atomicity.

Exit: verified.

### WM-FLOW-05 — Work projection source

Pinned `IWorkItemProjectionSource`.

Before implementation, resolve the revised SPEC's M10-facing ownership/runtime
normalization for the projection-source edge. The invariant is already frozen:

```text
WorkManagement owns producer truth
Analytics cannot implement Work producer truth
Analytics cannot read Work DbContext directly
```

Implementation must be WorkManagement Application→Work DbContext→snapshot.

Remote/runtime binding beyond that invariant may remain deferred until the
Analytics rebuild pack if not required by M6 production source.

Exit: verified.

### WM-FLOW-02 — Work→Collaboration read Port

This edge is TAC-XC-B.

Before coding, close the revised SPEC's `DEFERRED-M6-SOURCE-NORMALIZATION`
decision so the adapter calls a legitimate Collaboration-owned semantic
boundary rather than treating Collaboration persistence as an implicit public
API.

Required final shape:

```text
WorkManagement consumer Port
→ WorkManagement-owned ACL if needed
→ Infrastructure adapter
→ Collaboration-owned stable read boundary
```

Forbidden:

```text
Port exists
→ adapter directly treats foreign DbContext as cross-BC public contract
```

Exit: verified.

## M6 exit

All five flows verified; Work baseline complete.

---

# 15. TAC-M7 — Documents & Collaboration Team Operating Pack

## DC-FLOW-01 CreatePage

Use M2G-frozen Page permission semantics.

Any outward Page event satisfies TAC-FRZ-018.

## DC-FLOW-02 ArchivePage

Implement from Page.Archive authority.

PageArchived outward event carries authoritative AccountId + WorkspaceId.

## DC-FLOW-03 Comment.ForBoardItem

Independent Work ResourceRef/auth case.

## DC-FLOW-04 Comment.ForPage

Independent Documents ResourceRef/auth case.

## DC-FLOW-05 CommentCreated→Activity

Run through TenantContextConsumeFilter; tenant must not become System.

## DC-FLOW-06 MentionCreated→Notification

Mandatory producer corrections:

```text
AccountId + WorkspaceId exact
creator/mentioner actor exact
```

At mention creation capture trusted actor into owner state and/or Domain Event.

Forbidden:

```text
MentionedByUserId = MentionedId
consumer reconstructs actor
Guid.Empty tenant
```

Tests include:

```text
mentioned user != actor
actor exactness
tenant exactness
duplicate delivery
cross-tenant isolation
```

## DC-FLOW-07 Documents outward events

Normalize PageCreated/PageArchived tenant envelope and event compatibility.

Stub/log-only consumers remain excluded from business evidence.

## Exit

Documents/Collaboration baselines complete and all their scoped events satisfy
TAC-FRZ-018.

---
# 16. TAC-M8 — Automation & Integrations Team Operating Pack

## AI-FLOW-01 CreateAutomationRule

Automation local rule mutation. Final capacity integration depends on M9.

## AI-FLOW-02 Work event→AutomationExecution

Source event must satisfy TAC-FRZ-018.

## AI-FLOW-03 AutomationExecution→Work

Keep target scope/auth/OperationId proof.

## AI-FLOW-04 N8n

Keep delivery-owned retry + real consume-filter proof.

## AI-FLOW-05 Calendar connection + binding

Required sequence:

```text
trusted Account/Workspace
→ Governance integration permission
→ provider credential input
→ secret-store Port
→ external secret persistence
→ IntegrationSecretVersion(SecretReference)
→ create/reuse IntegrationConnection
→ advance current secret version
→ CalendarIntegration(ConnectionId, WorkspaceId, SyncDirection)
→ Integrations DB commit
```

Required separate ownership:

```text
IntegrationConnection = generic provider relationship
CalendarIntegration = Workspace Calendar binding
```

Integration test must reload from DB and prove Connection + SecretVersion +
CalendarIntegration round-trip.

Test duplicate OAuth callback and secret-stored/DB-failed compensation.

## AI-FLOW-06 Calendar binding disconnect

First mutation:

```text
CalendarIntegration.Deactivate
```

Generic Connection cleanup follows CAL-CONN-001.

Test one-of-many binding, last binding, retain/revoke policy and provider unknown
outcome.

## AI-FLOW-07 verified Calendar webhook

Required:

```text
raw HTTP
→ signature/timestamp/replay verification
→ trusted IntegrationConnection
→ active CalendarIntegration
→ trusted tenant derivation
→ Infrastructure inbound receipt/dedup
→ provider-neutral Integrations Application input
→ exact target-context effect
→ semantic success condition
```

Forbidden:

```text
WebhookDelivery as inbound receipt
untrusted tenant payload
user-session auth as provider auth
```

Existing InboundWebhookEvent can remain Domain only after proving user-facing
lifecycle and removing LegacyGap classification; otherwise intake is technical
Infrastructure state.

The exact downstream target is a prerequisite decision, not an implementation
detail. Do not wire a target command or mark a receipt `Processed` until the
Product/Integrations authority records the target context, action/event,
authorization, idempotency, and success condition.

## Exit

TAC-FRZ-019 FROZEN and all Automation/Integrations flows VERIFIED.

---
# 17. TAC-M9 — Billing & Entitlements capacity pack

## BI-FLOW-01 capability/capacity resolution

Requires BILL-LIMIT-001 resolved.

Calculate from entitlement scope/state + capacity limit + reserved/consumed
capacity + requested amount.

Do not assume zero = unlimited.

AUTOMATION_RULE is capacity unless product authority says billable metric.

## BI-FLOW-02 hard-capacity mutation

Implement/reuse Billing-owned semantic capacity action:

```text
reserve
commit/consume
release/compensate
LogicalOperationId
source resource
scope
```

Select a repository-supported concurrency mechanism:

```text
atomic conditional update
optimistic version
serialized lock
reservation row + unique invariant
```

Mandatory test:

```text
remaining = 1
2 concurrent reservations
→ exactly 1 succeeds
```

## BI-FLOW-03 AutomationRule→capacity

CreateAutomationRule participates in frozen capacity protocol.

Mandatory race:

```text
remaining = 1
2 concurrent CreateAutomationRule
→ exactly 1 successful capacity-consuming rule
```

Feature failure after reserve must release/compensate.

Same LogicalOperationId cannot double consume.

## BI-FLOW-04 capability-after-capacity

Run full production path; no seed-only proof.

## BI-FLOW-05 provider classification

Still conditional at candidate.

## Exit

BILL-LIMIT-001 resolved, last-slot race safe, capacity protocol proven,
TAC-FRZ-020 FROZEN.

---
# 18. TAC-M10 — Analytics & Reporting Team Operating Pack

### AR-FLOW-01 — live Work placement projection

```text
Work event
→ Platform
→ Analytics consumer
→ local placement projection
```

Projection outside Domain. Prove dedup, ordering/revision, tenant isolation.

Exit: verified.

### AR-FLOW-02 — local Analytics query

Read local projection only; no normal-path Work query/DbContext.

Exit: verified.

### AR-FLOW-03 — rebuild

Phase 0 closes the revised SPEC's
`DEFERRED-M10-PORT-OWNERSHIP-NORMALIZATION` interaction decision:

```text
DecisionStatus: CLOSED-FROZEN (TAC-XC-B, M10)
```

Required semantic authority (frozen):

```text
WorkManagement.Public IWorkItemProjectionSource
→ WorkManagement Application owner implementation
→ Work persistence
```

Consumer/runtime shape must not invert that ownership.

Target chain:

```text
Analytics rebuild
→ Analytics Application source/rebuild Port
→ Infrastructure delegate adapter
→ Work.Public IWorkItemProjectionSource
→ Work Application owner implementation
→ snapshot
→ reconcile local projection
```

No outbox-as-event-store and no foreign Work DbContext.

Test drift, missing/extra/update, concurrent live event, workspace isolation.

Exit: verified.

### AR-FLOW-04 — failure/recovery

Prove duplicate, stale, crash, source unavailable, rebuild retry, cross-workspace protection.

Exit: verified.

## M10 exit

Analytics independently supports live projection + local read + producer-backed rebuild.

---

# 19. TAC-M11 — Platform/Foundation Team Operating Pack

Order:

```text
PF-FLOW-01
PF-FLOW-02
PF-FLOW-07
PF-FLOW-03
PF-FLOW-04
PF-FLOW-05
PF-FLOW-06
```

## PF-FLOW-01 request pipeline

Real protected write through production request pipeline.

## PF-FLOW-02 DomainEvent→IntegrationEvent→outbox

Required producer families include Identity, Workspaces, WorkManagement,
Documents/Collaboration.

## PF-FLOW-07 scoped-event tenant envelope

Enumerate tenant-scoped Integration Events and enforce:

```text
WorkspaceId != null
→ AccountId != null
```

Representative runtime proof:

```text
producer mutation
→ outbox
→ broker
→ TenantContextConsumeFilter
→ exact Account/Workspace tenant
→ consumer
```

System/global classification must be explicit.

Where schemas change, use the event compatibility/version policy; do not silently
break contracts.

## PF-FLOW-03 tenant restoration + dedup

Run after envelope normalization.

## PF-FLOW-04 retry/failure

One retry owner.

## PF-FLOW-05 compatibility/recovery

Must cover event changes introduced by tenant-envelope and Mention actor fixes
according to the declared event-family capability:

```text
ReplayableSameSchema
Upcastable
DrainBeforeCutover
RebuildFromAuthority
NotReplayable
```

Work V1→V2 is `SchemaCompatibility.None`, `DrainBeforeCutover`, and
`RebuildFromAuthority`; it is not certified as generic replay.

## PF-FLOW-06 background actor/security

Automation→Work explicit executor identity.

## Exit

PF-FLOW-01..07 VERIFIED and TAC-FRZ-018 FROZEN.

---
# 20. TAC-M12 — Cross-pack production integration and architecture fitness

## Objective

Prove all packs work together and demonstrate the intended architecture.

---

## TAC-M12A — Automation/Work/Analytics chains

## Automation target-action chain

Start above the Port:

```text
Automation action execution
→ AutomationExecution
→ MoveItem action executor
→ IWorkActionPort
→ ACL
→ WorkItemActionAdapter
→ WorkManagement Public action
→ target authorization/scope/idempotency
→ MoveBoardItem use case
```

Required cases:

```text
success
wrong workspace
unauthorized principal
duplicate OperationId
conflicting OperationId
target business rejection
technical failure
```

## Analytics chain

```text
MoveBoardItem
→ BoardItemMovedIntegrationEvent
→ real dedup delivery
→ Analytics consumer
→ producer Public snapshot source if needed
→ Analytics projection
→ local query
```

Rebuild:

```text
Analytics rebuild command
→ producer Public implementation
→ projection repair
```

The test must not manually instantiate a consumer-side adapter that reads
Work DbContext directly.
## TAC-M12B — Identity/Accounts/Workspaces/Governance chains

Execute:

```text
Identity Public fact
→ AcceptInvitation
→ Accounts Public membership action
→ same BOUND-TX-002 request transaction
→ WorkspaceMember mutation
→ WorkspaceMemberAddedIntegrationEvent
→ existing Collaboration activity projection reaction
```

Separately execute canonical Governance authorization:

```text
protected Work/Collaboration request
→ existing authorization pipeline
→ source-owned identity/account/workspace/resource facts
→ allow/deny
```

Do not force Workspace membership events into Governance if the authorization
mechanism is synchronous.
## TAC-M12C — Documents/Collaboration chains

Execute:

```text
foreign resource reference
→ Collaboration local mutation
→ source resource unchanged
```

plus access/existence checks as required.

---

## TAC-M12D — Billing capability + usage chain

Execute:

```text
Billing capability
→ consumer decision
```

Prove no private plan branching.

---

## TAC-M12E — Provider runtime chains

Execute through production delivery mechanics:

```text
N8nDispatchRequestedV1
→ DeduplicationConsumeFilter
→ N8nDispatchConsumer
→ N8nDispatchUseCase
→ Integrations.Public IN8nWebhookActions
→ provider Port
→ Infrastructure N8nClient test HTTP seam
```

Required cases:

```text
success
terminal failure
retryable failure
unknown outcome
invalid configuration
```

Assertions:

```text
success commits Automation state + dedup success
terminal failure commits Automation terminal state
retryable failure rolls back delivery transaction and does not falsely persist
  Automation retry evidence
unknown outcome does not blindly redeliver
later retry success commits once
```
### v2.6 mandatory cross-pack additions

M12 additionally proves:

```text
Register→Accounts Public provisioning→BOUND-TX-004→registration event→Workspace provisioning

Documents CreatePage→PageCreated outbox
Documents ArchivePage→PageArchived outbox

CreateComment.ForPage→Documents-target authorization

MentionCreated→Account/Workspace-correct Notification

ConnectCalendar→secret store→IntegrationConnection
DisconnectCalendar→connection lifecycle + cleanup policy
verified Calendar webhook→trusted connection→tenant/dedup

AutomationRule creation→Billing usage→capability-after-usage

Analytics local query→local projection only

Platform event compatibility/replay
```

### v2.6 mandatory cross-pack additions

M12 proves:

```text
Accounts RenameAccount
→ Governance Account-admin authorization
→ Accounts mutation

Register→Accounts→scoped registration event→Workspace provisioning

Page/Comment/Mention events
→ AccountId + WorkspaceId
→ tenant restore, not System

Mention
→ authoritative actor
→ exact Notification actor/tenant

ConnectCalendar
→ secret store
→ IntegrationSecretVersion
→ IntegrationConnection
→ CalendarIntegration

DisconnectCalendar
→ CalendarIntegration deactivate
→ CAL-CONN-001 Connection policy

Calendar webhook
→ provider verification
→ technical inbound receipt/dedup
→ trusted tenant
→ semantic processing

Billing remaining=1
→ 2 concurrent CreateAutomationRule
→ exactly 1 success

Common grant projection
→ no AccountRole/WorkspaceRole signature leakage
```

## TAC-M12F — Architecture gates

Run exactly:

```text
ARCH-BC-001..008
STN-ARCH-001/002/005/006/007/008
```

If a gate fails, change only the source violating that gate or harden the
existing gate owner when the failure exposes a false-green case. Do not weaken
or bypass a gate.

Reference code must pass without broad exceptions.

---

## TAC-M12G — Real Flow topology checks

Verify:

```text
no fake Examples namespace
no marker-only folders
no duplicate architecture dialect
no generic SharedContracts
no new Common business vocabulary
```

## Exit

```text
[ ] all mandatory reference chains execute
[ ] architecture gates pass
[ ] no cross-pack semantic contradiction
```

---

# 21. TAC-M13 — Team-copy guidance and false-reference cleanup

## Objective

Turn the implemented source into a development model teams can copy.

---

# 72. TAC-M13A — Durable hard-doc update

Update existing architecture authorities only.

Document concise stable rules:

```text
Team Operating Pack map
Public vs Port
target mutation
event chain
projection
process
ResourceRef
provider adapter
transaction STOP
```

Do not copy execution milestone history.

---

# 73. TAC-M13B — Team routing

For each team, ensure existing team docs/agent instructions point to:

```text
relevant reference source
architecture owner docs
tests
STOP conditions
```

No separate architecture dialect per team.

---

# 74. TAC-M13C — Clean temporary compatibility

Remove/shrink:

```text
legacy seam
stale baseline
dead allowance
unused adapter
old namespace
```

created obsolete by reference implementation.

---

# 75. TAC-M13D — Source discoverability

Source names should make roles obvious.

Do not add `Example` suffixes.

Use architecture comments only when intent would otherwise be unclear.

## TAC-M13E — Real Flow Catalog durability

Each Team Pack must let future implementers answer:

```text
which BC owns this?
which verified flow is the same architectural case?
which mechanism?
what scope/auth/tx/idempotency rules?
which tests/gates?
```

Route at least:

```text
local mutation
authoritative Public read
target action
event reaction
projection
process
provider outbound
provider inbound/webhook
ResourceRef variant
background action
```

## TAC-M13F — False-reference cleanup

Rescan:

```text
*StubConsumer*
*StubConsumerDefinitions*
NotImplementedException
Demo
Sample
ReferenceExample
FakeFeature
```

Mandatory replacements must be authoritative; stale fake/stub evidence cannot remain as teaching architecture.

Intentional unfinished non-mandatory product features may remain only if clearly outside the mandatory Real Flow set.

## Exit

```text
[ ] teams can discover reference code from source
[ ] hard docs point to one model
[ ] stale debt shrunk
```

---

# 22. TAC-M14 — Exact-SHA certification

## Objective

Certify one exact candidate SHA.

Record:

```text
ImplementationSHA
ParentSHA
BaseSHA
CI runs
```

---

# 77. Required final test families

At minimum:

```text
Architecture.Tests
Application.Tests
Infrastructure.Tests
Platform.Tests
Integration.Tests
affected API tests
```

plus reference-flow tests.

---

# 78. Required final reference states

All mandatory packs:

```text
TAC-RAP-IA = VERIFIED/FROZEN
TAC-RAP-WG = VERIFIED/FROZEN
TAC-RAP-WM = VERIFIED/FROZEN
TAC-RAP-DC = VERIFIED/FROZEN
TAC-RAP-AI = VERIFIED/FROZEN
TAC-RAP-BI = VERIFIED/FROZEN
TAC-RAP-AR = VERIFIED/FROZEN
TAC-RAP-PF = VERIFIED/FROZEN
```

No mandatory pack may be deferred.

---

# 79. Mandatory freeze states

```text
TAC-FRZ-001 FROZEN
TAC-FRZ-002 FROZEN
TAC-FRZ-003 FROZEN
TAC-FRZ-004 FROZEN
TAC-FRZ-005 FROZEN
TAC-FRZ-006 FROZEN
TAC-FRZ-007 FROZEN
TAC-FRZ-008 FROZEN or exact accepted debt
TAC-FRZ-009 FROZEN
TAC-FRZ-010 FROZEN
TAC-FRZ-011 FROZEN
TAC-FRZ-012 FROZEN
TAC-FRZ-013 FROZEN
TAC-FRZ-014 FROZEN
TAC-FRZ-015 FROZEN
```

---

# 79A. Real Flow → milestone → freeze traceability

| Flow group | Milestone | Primary closure |
|---|---|---|
| IA-FLOW-01..06 | M4 | TAC-FRZ-016/017 + BOUND-TX-004 |
| WG-FLOW-01..06 | M5 | TAC-FRZ-013/016/017 |
| WM-FLOW-01..05 | M6 | TAC-FRZ-010/011/016/018 |
| DC-FLOW-01..07 | M7 | TAC-FRZ-014/016/018 |
| AI-FLOW-01..07 | M8 | TAC-FRZ-010/015/019 |
| BI-FLOW-01..05 | M9 | TAC-FRZ-012/020 |
| AR-FLOW-01..04 | M10 | TAC-FRZ-011/016 |
| PF-FLOW-01..07 | M11 | TAC-FRZ-018 + runtime closure |

Direct dependencies:

```text
IA-FLOW-06 → TAC-FRZ-017
DC-FLOW-06 → Mention actor + TAC-FRZ-018
AI-FLOW-05/06/07 → CAL-CONN-001 + TAC-FRZ-019
BI-FLOW-01..04 → BILL-LIMIT-001 + capacity protocol + TAC-FRZ-020
PF-FLOW-07 → all scoped event families
```
# 79B. v2.6 blocker-resolution matrix

| Blocker | Milestone | Closure |
|---|---|---|
| Accounts lacks local admin command | M4 | IA-FLOW-06 RenameAccount |
| Common grant seam leaks AccountRole/WorkspaceRole | M3/M4/M5 | TAC-FRZ-017 |
| scoped events omit AccountId | M3/M7/M11 | TAC-FRZ-018 |
| Mention actor not authoritative | M2/M7 | owner actor fact + exact mapping |
| Calendar collapses Connection/CalendarIntegration | M8 | dual-model lifecycle |
| CurrentSecretRef treated as durable authority | M8 | IntegrationSecretVersion round-trip |
| disconnect targets wrong lifecycle | M2/M8 | CalendarIntegration first + CAL-CONN-001 |
| inbound webhook reuses outbound delivery state | M8 | Infrastructure intake/dedup |
| Billing zero==unlimited magic | M2/M9 | BILL-LIMIT-001 |
| Billing hard-quota stale race | M9 | last-slot concurrency proof |
| capacity confused with billable UsageMetric | M9 | capacity semantics |
| traceability label has no real evidence | M13 | substantive trace audit |
| stale revision authority conflicts | M13 | historical/non-normative cleanup |
# 80. Dependency DAG

```text
M0 → M1
M1 → M2
M1 → M3

M3
→ PRE-M4 Interaction Architecture normalization
→ M4

PRE-M4 global coarse classification
→ M5..M11 pack-entry deep interaction freeze

M3 TAC-FRZ-017 gate
→ M4 IA-FLOW-06/02
→ M5 Governance/grant flows

M2 Page auth
→ M5 Governance Page variant
→ M7 Page resource flows

M2 Mention actor
+ M3 TAC-FRZ-018
→ M7 DC-FLOW-06

M3 TAC-FRZ-018
→ M4/M5/M6/M7/M8 scoped events
→ M11 PF-FLOW-07

M2 CAL-CONN-001
+ M3 TAC-FRZ-019
→ M8 AI-FLOW-05/06/07

M2 BILL-LIMIT-001
+ M2 capacity consistency
+ M3 TAC-FRZ-020
→ M9 BI-FLOW-01..04
→ M8 AI-FLOW-01 final capacity integration

M6 Work events/source
→ M8 Automation
→ M10 Analytics

M11 PF-FLOW-05
covers event schema migrations introduced earlier

M4..M11 → M12 → M13 → M14
```
# 81. Parallelization model

After M3 foundation gates **and PRE-M4 global interaction classification**:

## Parallel Track 1

```text
M4 Identity/Accounts
M5 Workspace/Governance
```

M5 waits for M2 transaction decision where needed.

## Parallel Track 2

```text
M6 WorkManagement
```

Once Work event/action contracts freeze:

```text
M8 Automation
M10 Analytics
M11 Platform
```

can proceed in coordinated parallel.

## Parallel Track 3

```text
M7 Documents/Collaboration
```

## Parallel Track 4

```text
M9 Billing
```

Cross-pack contract owners must coordinate before merging incompatible
consumer assumptions.

---

# 82. File ownership during parallel execution

Avoid concurrent edits to the same semantic seam.

Examples:

```text
Work Public action
→ WorkManagement pack owner

Automation Port
→ Automation pack owner

Work adapter for Automation
→ Infrastructure cross-context owner coordinated with Automation/Work

Work event schema
→ WorkManagement pack owner

event delivery
→ Platform pack owner
```

Consumer teams do not redefine producer Public contracts independently.

---

# 83. Contract handoff workflow

For every cross-team reference edge:

```text
Producer drafts semantic contract
↓
Consumer validates need
↓
Producer freezes meaning
↓
Consumer implements Port/ACL if needed
↓
Infrastructure implements adapter
↓
Tests prove end-to-end
```

No transport discussion before semantic contract unless runtime constraint is
actually material.

---

# 84. Implementation slicing

Recommended slices:

```text
A  M0/M1 exact source + 47-flow catalog
B  M2 semantic decisions
C  M3 TAC-FRZ-017..020 gates
C2 PRE-M4 interaction-edge normalization (docs/authority only)

D  Accounts RenameAccount
E  Register→Accounts→Workspace
F  Workspaces/Governance
G  Work target/events/source
H  Documents Create/Archive
I  Collaboration Board/Page/Activity
J  Mention actor + tenant + event migration
K  Automation Work process
L  N8n
M  Calendar Connection+SecretVersion+Binding
N  Calendar disconnect policy
O  Calendar verified webhook
P  Billing capacity owner action
Q  Billing concurrency + AutomationRule integration
R  Analytics
S  Platform tenant-envelope/runtime/version-replay
T  integration/cleanup/certification
```

Do not combine Calendar connection and webhook intake into one opaque slice.
# 85. Per-slice acceptance template

Each slice must record:

```text
SemanticOwner
Producer
Consumer
FlowId
EdgeId where cross-BC
Mechanism = TAC-XC-A..F where cross-BC
DecisionStatus
CurrentBinding
RemoteReadiness
Derived SemanticBoundaryReady / RuntimeSubstitutionReady / ExtractionBlocked
FutureDistributedBinding
SourceBefore
SourceAfter
Transaction
Idempotency
FailureSemantics
ExtractionBlocker
RemovalTrigger
ArchitectureGate
BehaviorTests
IntegrationTests
DebtRemoved
DebtAdded
```

---

# 86. M2 decision record template

```text
DecisionId: TAC-TX-ACCOUNTS-WORKSPACES
Selected: A|B|C

WorkflowOwner:
AccountsMutationOwner:
WorkspacesMutationOwner:

Invariant:
AtomicityRequired:
PartialSuccessAllowed:
Retry:
Idempotency:
Compensation:
UnknownOutcome:
SecurityProjection:
FutureRemoteImpact:

Tests:
ExceptionId:
```

---

# 87. Real Flow implementation card

```text
FlowId:
Team:
BoundedContext:
Purpose:
Disposition:
EntryPoint:
ProductionReachability:

SemanticOwner:
WorkflowOwner:
MutationAuthority:
SourceAuthority:
TargetAuthority:
Mechanisms:
FlowKind:
SupportingShapes:
RequestPipeline:

Actor:
Authorization:
AccountScope:
WorkspaceScope:
ResourceScope:

Idempotency:
Concurrency:
Transaction:

ApplicationPath:
DomainPath:
PublicContract:
ConsumerPort:
ACL:
InfrastructureAdapter:
ProviderBoundary:

IntegrationEvents:
Projection:
ProcessState:

FailureClasses:
RetryOwner:
Observability:

DI:
BehaviorTests:
IntegrationTests:
ArchitectureGates:

RemoteSubstitution:
KnownDebt:
STOPConditions:
CompletionEvidence:

Interactions:
  - EdgeId:
    Purpose:
    SourceBC:
    TargetBC:
    Mechanism:
    DecisionStatus:
    ContractOwner:
    PortOwner:
    WorkflowOwner:
    MutationOwner:
    ProjectionOwner:
    ProcessOwner:
    CallerWaitsForOutcome:
    ConsistencyModel:
    CurrentBinding:
    RemoteReadiness:
    FutureDistributedBinding:
    TransactionBoundary:
    OutboxRequired:
    ConsumerDedupRequired:
    IdempotencyKey:
    OrderingRequirement:
    RetryOwner:
    BusinessFailureOwner:
    TechnicalFailureOwner:
    UnknownOutcomePolicy:
    CompensationPolicy:
    ExtractionBlocker:
    RemovalTrigger:
```

`Interactions: []` is valid for a truly local Flow Card.

At completion append:

```text
CandidateSHA:
SourcePaths:
TestPaths:
GateResults:
RuntimeProof:
FinalState:
```

Implementation cannot begin with correctness fields blank.
# 88. Team Operating Pack implementation card

```text
Pack:
OwnedBCs:

BC:
  AuthoritativeState:
  LocalBaselineFlow:
  LocalBaselineState:

FlowCatalog:
  - FlowId:
    State:
    Source:
    Tests:
    Freeze:

CrossPackDependencies:
ProducedPublicContracts:
ConsumedPublicContracts:
ProducedEvents:
ConsumedEvents:
OwnedProjections:
OwnedProcesses:
ProviderBoundaries:
StubInventory:
KnownDebt:
Blockers:
CandidateSHA:
PackState:
```

PackState cannot become `FROZEN` while any owned BC lacks a verified local baseline.
# 89. Team/BC-specific STOP conditions

## Accounts/Common

STOP if RenameAccount authorization is unclear or new flow still depends on
Common signatures exposing AccountRole/WorkspaceRole.

## Events/Platform

STOP if a Workspace-scoped business event lacks authoritative AccountId or event
version migration is undefined.

## Collaboration

STOP if Mention actor has no owner-side authority, mapper derives actor from
MentionedId, or Notification tenant is synthetic.

## Integrations

STOP if Connection/CalendarIntegration are collapsed, secret persistence is
unclear, CAL-CONN-001 unresolved, outbound WebhookDelivery is used for inbound
receipt, user session substitutes provider auth, or untrusted payload chooses
tenant.

## Billing

STOP if BILL-LIMIT-001 unresolved, zero is interpreted as unlimited by
convention, hard quota has no concurrency-safe reservation, capacity is treated
as billable UsageMetric without authority, or consistency protocol is unfrozen.

All carried-forward owner/auth/transaction/projection/retry STOP rules remain.
# 90. Mandatory failure cases per Team Operating Pack

## IA

```text
RenameAccount deny/wrong Account/concurrent conflict
Register Accounts failure rollback
registration event wrong tenant
```

## WG/WM

```text
Page permission mismatch
Common role-signature regression
Work event missing AccountId
scoped event restored as System
```

## DC

```text
mentioned user != actor
actor missing
missing AccountId
Guid.Empty Notification tenant
Board/Page authorization differences
```

## AI

```text
duplicate OAuth callback
secret stored + DB failure
Calendar binding duplicate
one-of-many vs last binding disconnect
provider revoke unknown outcome
invalid webhook signature/timestamp/replay
outbound WebhookDelivery accidentally used as inbound state
```

## BI

```text
zero-limit unresolved
last-slot race
duplicate/conflicting reservation
feature failure after reserve
release/compensation
capacity/billable-metric confusion
```

## AR/PF

All carried-forward projection/retry/replay failures plus scoped event restored as
System and compatibility failure after schema migration.
# 91. Architecture gate evolution rule

When reference implementations reveal a new enforcement need:

1. identify existing owner;
2. harden existing owner if same property;
3. add self-test;
4. avoid broad namespace assumptions;
5. only create new gate ID for genuinely new invariant.

Reference code is a reason to strengthen the architecture suite, not bypass it.

---

# 92. Migration policy

Reference implementation may replace current shortcuts.

Classify each:

```text
REMOVE-NOW
MIGRATE-ON-TOUCH
EXACT-DEBT
```

Any shortcut directly conflicting with a mandatory reference should be:

```text
REMOVE-NOW
```

unless a safe rolling migration requires exact temporary dual-read/dual-path
with one authoritative owner.

No dual writer.

---

# 93. Schema migration policy

If Automation Process or Analytics Projection requires new persistence:

```text
migration must be owned by the corresponding semantic context
```

Even if physically stored in shared DB.

Migration naming should reflect context.

No cross-context FK navigation required.

Use stable IDs.

---

# 94. Test data policy

Reference tests should use realistic minimal state.

Do not seed arbitrary product catalog just to reach a line.

Each fixture should express:

```text
what semantic invariant is under test
```

---

# 95. API policy during reference bootstrap

Default:

```text
no new public endpoint solely for reference
```

If a reference corresponds to an already-intended API use case:

```text
existing endpoint may be wired
```

otherwise:

```text
Application/integration tests are sufficient
```

---

# 96. Observability policy

Reference event/process/provider flows should include enough correlation to
demonstrate current observability mechanism.

Do not build a new observability stack.

Expected:

```text
correlation id
message/execution id
structured failure context
```

according to current Platform abstractions.

---

# 97. Future extraction validation

Future extraction validation is mechanism-specific.

Do not ask only:

```text
"If producer became remote tomorrow, would only adapter/DI change?"
```

That question is valid mainly for adapter-ready TAC-XC-B style boundaries and
can produce false failures for legitimate TAC-XC-A or shared-transaction flows.

For every cross-BC interaction edge record:

```text
SemanticBoundaryReady
RuntimeSubstitutionReady
ExtractionBlocked
ExtractionBlocker
RemovalTrigger
```

Expected interpretation:

```text
TAC-XC-A Public Direct
→ semantic boundary may be closed
→ runtime substitution may remain deferred
→ extraction may later use runtime proxy or migrate consumer to TAC-XC-B

TAC-XC-B Port + ACL + Adapter
→ consumer handler/Domain should remain stable
→ runtime adapter/DI/transport are expected substitution points

TAC-XC-C Integration Event
→ producer fact + consumer reaction semantics remain stable
→ deployment/routing may move across process boundaries
→ outbox/dedup/tenant semantics still apply

TAC-XC-D Projection
→ projection remains consumer-local
→ producer feed/snapshot source stays authoritative

TAC-XC-E Target-owned Action
→ target ownership remains stable
→ remote command adapter is possible only if transaction semantics permit it

TAC-XC-F Process Manager
→ workflow owner/process state remain stable
→ participant transports may change
```

Known blockers:

```text
BOUND-TX-002
BOUND-TX-004
```

While those decisions require current shared atomicity:

```text
SemanticBoundaryReady = yes is possible
RuntimeSubstitutionReady = no/deferred
ExtractionBlocked = true
```

If a team must redesign business semantics merely because it changed the
transport, re-open the exact interaction decision. Do not hide that redesign
inside an adapter.

---

# 98. Completion by milestone

```text
M0  47-flow semantic/source inventory
M1  47 Flow Cards frozen
M2  TX-002/TX-004 + capacity + BILL-LIMIT + CAL-CONN + actor/auth decisions frozen
M3  TAC-FRZ-017..020 gates active
PRE-M4  all 47 flows coarse-classified; IA-FLOW-01..06 interaction-deep-freeze complete
M4  IA-FLOW-01..06 verified
M5  WG-FLOW-01..06 verified
M6  WM-FLOW-01..05 verified
M7  DC-FLOW-01..07 verified
M8  AI-FLOW-01..07 verified + TAC-FRZ-019 frozen
M9  BI-FLOW-01..04 verified; BI-FLOW-05 classified; TAC-FRZ-020 frozen
M10 AR-FLOW-01..04 verified
M11 PF-FLOW-01..07 verified + TAC-FRZ-018 frozen
M12 cross-pack chains green
M13 stale-authority + real-evidence trace cleanup
M14 exact candidate SHA certified
```
# 99. Architecture closure condition

Closed only when:

```text
11 business BC local baselines verified
8 Team Operating Packs FROZEN
47 Flow IDs present
46 executable flows VERIFIED
BI-FLOW-05 correctly classified
TAC-FRZ-001..020 satisfied
BOUND-TX-002/004 satisfied
Billing capacity protocol frozen
BILL-LIMIT-001 resolved
CAL-CONN-001 resolved
Common signature purity proven
scoped-event tenant envelope proven
Mention actor exact
Calendar dual-model/secret/webhook lifecycle proven
hard-quota concurrency proven
runtime-owner tests green
traceability labels resolve to real evidence
exact candidate SHA certified
```

Green CI alone is not closure.
# 100. What remains intentionally outside this execution

This plan does not require:

```text
complete Automation product
complete Analytics dashboards
complete Billing provider integration
complete Collaboration notification redesign
all possible Integration providers
microservice extraction
database split
frontend implementation
full API exposure for reference flows
```

It requires enough source to prove how those feature classes should be built.

---

# 101. Final expected repository state

Future teams must find exemplars for:

```text
Identity local mutation
Accounts local admin + cross-BC Public action
Workspaces/Governance local creation/permission/auth
Work local/target/events/projection-source
Documents/Collaboration lifecycle/resource/activity/actor+tenant
Automation local/process/target/N8n
Integrations generic Connection + persisted SecretVersion + Calendar binding + verified inbound webhook
Billing capacity query + concurrency-safe reserve/consume/release
Analytics live/local/rebuild/recovery
Platform request/outbox/scoped-envelope/tenant/dedup/retry/version-replay/background security
```

No engineer should infer semantic authority from a nearby class name.

# 103. Current candidate execution override — PR #158 re-audit

The historical execution slices above remain traceability records. The active
plan for the current candidate is:

```text
1. Keep PF-FLOW-05 capability-based. Do not implement a universal event store
   or synthetic replay source solely to satisfy the old generic test shape.
2. Derive ContractRegistry compatibility from the canonical evolution policy;
   do not maintain a second Work-event allow-list.
3. Complete AI-FLOW-06 only through the provider-neutral secret/provider
   cleanup outcome contract and post-commit delivery; do not invent provider
   subscription architecture in this TAC pack.
4. Keep AI-FLOW-07 blocked until an accepted decision names the exact
   downstream target effect and semantic success condition. Do not wire a
   fabricated target command.
5. Treat other BCs as RETAIN/PREVIOUSLY VERIFIED until exact-SHA evidence is
   recertified. Treat M14 as open until format and runtime-image security
   gates pass.
```
## Interaction Architecture final-state requirement

At final closure:

```text
SPEC owns TAC-XC-A..F operating semantics + Flow Card Interactions[]
PLAN owns execution order / pack-entry freeze / implementation slicing
TESTS owns behavioral/runtime proof
CERTIFICATION owns final pass conditions
STATUS owns evidence only
```

No standalone Interaction Architecture working-plan file is required as a
permanent competing authority after these changes are merged.

---

# 102. Final execution rule

For every slice:

```text
read exact source
→ identify semantic authority
→ identify persistence authority
→ resolve STOP/decision IDs
→ load Flow Card
→ implement smallest complete production flow
→ test behavior
→ test transaction/concurrency/runtime owner
→ run architecture gates
→ record real evidence IDs
```

Never:

```text
choose aggregate by convenience
assume in-memory property is durable authority
accept WorkspaceId-only event as tenant-safe
derive actor from mentioned user
assume zero == unlimited
call capacity billable metric by convention
prove hard quota only sequentially
write traceability row to nonexistent evidence
```

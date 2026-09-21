---
document_id: EXEC-BACKEND-TEAM-ARCHITECTURE-CLOSURE-CERTIFICATION
title: Backend Team Operating Architecture Bootstrap & Closure — Certification
version: 2.6
status: execution-ready
supersedes: version 2.5
repository: Nqv1208/Notrelix
branch: architecture/backend-boundary-execution
audit_snapshot:
  commit: b94312b3e10e2212f18440003e775d415aa1b5d7
  pr: 113
audit_revision: interaction-architecture-normalized-source-pattern-certification-v2.6-public-capability-topology
depends_on:
  - backend-team-architecture-closure.v2.6.spec.md@2.6
  - backend-team-architecture-closure.v2.6.plan.md@2.6
  - backend-team-architecture-closure.v2.6.tests.md@2.6
upstream_certification:
  - backend-boundaries
  - backend-structural-topology
---

# Backend Team Architecture Bootstrap & Closure — Certification

## 0. Revision intent

historical bootstrap revision retains the v2 positive-existence certification principle and adds v2.1 source-normalization plus concrete-reference pinning. The previous principle was:

```text
missing real flow
→ DEFER-NO-REAL-FLOW
→ architecture may still close
```

with:

```text
mandatory reference flow missing
→ certification FAIL
```

The purpose of this execution is not only to prove:

```text
the current backend does not violate boundaries
```

It must also prove:

```text
every backend team has executable canonical reference architecture
```

for the interaction classes that team will need to implement future features.

Therefore `ARCHITECTURE-CLOSED` in v2 means:

```text
semantic rules are defined
+
remaining blockers are resolved
+
mandatory team Team Operating Packs are implemented
+
reference flows execute
+
architecture gates reject invalid variants
+
tests reject missing variants
+
source topology is copyable by future teams
```

The current candidate additionally requires interaction architecture to be
closed at **edge level**:

```text
Real Flow
→ zero/one/many Interactions[]
→ every actual cross-BC edge
→ exactly one TAC-XC-A..F mechanism
→ explicit semantic/runtime/failure ownership
```

Certification distinguishes:

```text
SemanticBoundaryReady
RuntimeSubstitutionReady
ExtractionBlocked
```

It does not infer service readiness merely from the existence of an interface,
and it does not require fake remote transport to certify the modular monolith.

---

# 1. Final outcomes

Exactly one final outcome is allowed.

## 1.1 ARCHITECTURE-CLOSED

Allowed only when all mandatory packs and freeze items pass.

## 1.2 BLOCKED-DECISION

A mandatory semantic decision is unresolved.

Examples:

```text
Accounts↔Workspaces atomicity
Process workflow owner
Governance permission authority
```

## 1.3 BLOCKED-IMPLEMENTATION

Architecture is decided, but one or more required reference flows are not yet
implemented completely.

Examples:

```text
Analytics Projection missing
Automation Process incomplete
Billing Public capability has no consumer
```

## 1.4 BLOCKED-REGRESSION

A required implementation exists but behavior or architecture tests fail.

## 1.5 BLOCKED-EVIDENCE

The implementation may be correct, but required evidence is missing or stale.

## 1.6 REBASE-REQUIRED

Candidate SHA diverged materially from the audited baseline and must be
re-inventoried.

---

# 2. V2 non-defer rule

The following mandatory packs cannot use:

```text
DEFER-NO-REAL-FLOW
NOT-YET-NEEDED
FUTURE-WORK
OUT-OF-SCOPE
```

as their final state:

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

The following mandatory references also cannot defer:

```text
Identity Public fact
Accounts Public fact
Accounts target action
Governance authoritative contract
Workspace membership event
CreateBoard local/pipeline reference
WorkManagement Collaboration Port+Adapter
Work target action
Work Integration Event
Collaboration ResourceRef
Automation Work event consumer
Automation Process Manager
Automation Work Port+Adapter
Integrations provider Port+Adapter
Billing Public capability
Billing reference consumer
Analytics Projection
Analytics rebuild
Platform outbox/delivery reference
```

If any is absent:

```text
Outcome = BLOCKED-IMPLEMENTATION
```

unless an unresolved semantic decision causes:

```text
Outcome = BLOCKED-DECISION
```

---

# 3. Certification invariant: exact SHA

Every certification run must record:

```text
Repository
Branch
PR
BaseSHA
ImplementationSHA
ParentSHA
CertificationDate
Reviewer
```

Audit snapshot:

```text
historical c409 snapshot2e9fcd968e79fa1e7ef6f208e1a559ef
```

is only a baseline.

If implementation changes:

```text
new exact SHA
→ rerun affected evidence
```

No stale proof.

---

# 4. Certification invariant: architecture must exist positively

A green negative architecture suite is insufficient.

Example:

```text
Automation has no foreign DbContext
```

is not enough.

V2 also requires:

```text
Automation has a canonical Work target Port
Automation has an Infrastructure adapter
Automation has a Process reference
```

Similarly:

```text
Analytics has no foreign Work DbContext
```

is not enough.

V2 requires:

```text
Analytics has an executable local Projection consuming Work event
```

---

# 4A. Certification invariant: source reuse is part of correctness

A mandatory reference can be satisfied by:

```text
REUSED/FROZEN
HARDENED/FROZEN
MIGRATED/FROZEN
IMPLEMENTED-MISSING/FROZEN
```

It is not required to be newly created.

Certification fails when a new reference implementation duplicates an existing
semantic capability that should have been reused or migrated.

The reference manifest must record the disposition for every `*-REF-*`.

# 4B. Certification invariant: coding-agent autonomy

`ARCHITECTURE-CLOSED` requires that mandatory implementation no longer depends
on coding-agent preference.

Pinned closure identities:

```text
AcceptInvitation + BOUND-TX-002
MoveBoardItem Public action
CreateComment.ForBoardItem
AutomationExecution + N8nDispatchUseCase
CreateAutomationRule Billing capability gate
WorkspaceWorkItemPlacementProjection
Integrations-owned N8n chain
```

If one pinned source no longer matches candidate source:

```text
BLOCKED-SOURCE-DRIFT
```

The coding agent may not silently substitute another feature.

# 4C. Certification invariant: runtime-owner evidence — previous Team Operating revision

A green test is valid certification evidence only when it executes the
production mechanism that owns the guarantee.

Required examples:

```text
N8n retry
→ real DeduplicationConsumeFilter transaction

Automation→Work
→ production Automation action executor
→ target Public action
→ target scope/auth/idempotency

Billing
→ real rule lifecycle
→ Billing usage lifecycle
→ subsequent capability result

Analytics
→ producer-owned Work Public source implementation
→ consumer adapter
→ projection/rebuild
```

The following is insufficient by itself:

```text
construct lower-level component directly
→ call method
→ manually SaveChanges
```

when production is wrapped by a filter/transaction/authorization/dedup layer
that changes semantics.

`ARCHITECTURE-CLOSED` therefore requires both:

```text
architecture structure proof
and
production runtime-owner behavior proof
```

# 4D. Certification invariant: every BC has an executable operating baseline — previous Team Operating revision

`ARCHITECTURE-CLOSED` requires a verified owner-side local production baseline
for every business bounded context:

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

and a verified technical baseline for:

```text
Platform / Foundation
```

A paired Team Operating Pack cannot be frozen when only one owned BC is
complete.

Examples:

```text
Collaboration CreateComment ≠ Documents baseline
N8n provider dispatch ≠ Integrations connection baseline
AcceptInvitation ≠ Governance local baseline
Billing capability query ≠ Billing usage-mutation baseline
```

The certified unit is:

```text
Team
→ Bounded Context
→ Real Flow Catalog
→ production Flow Card
→ runtime proof
→ architecture gate
```

# 4E. Certification invariant: no false Real Flow evidence

The following cannot satisfy a mandatory flow:

```text
NotImplementedException handler
*StubConsumer*
log-only/observability-only consumer
test-only fake feature
direct lower-level test bypassing production runtime owner
sibling-BC flow
one ResourceRef variant used as proof for another resource owner
```

Such source may remain only when explicitly classified outside mandatory
business-flow evidence.

# 4F. Certification invariant: semantic authority must match runtime authority — v2.6

`ARCHITECTURE-CLOSED` requires every mandatory flow to prove:

```text
semantic owner
persistent-state authority
runtime owner
transaction/concurrency authority
evidence authority
```

Forbidden false closure includes:

```text
Calendar works but only IntegrationConnection modeled
secret works in memory but SecretReference/version does not round-trip
provider webhook processed using outbound WebhookDelivery as inbound receipt
Workspace-scoped event reaches consumer as System tenant
MentionedId is used as MentionedBy actor
hard quota passes sequentially but fails last-slot concurrency
Common type name is technical but public signature exposes AccountRole
FlowId exists in table but its evidence label does not exist
```

# 4G. Certification invariant: unresolved semantic decisions block closure

Mandatory decision IDs where applicable:

```text
BOUND-TX-002
BOUND-TX-004
BILL-LIMIT-001
CAL-CONN-001
Billing capacity consistency
Mention actor authority
Page permission semantic
```

Unresolved decision:

```text
Outcome = BLOCKED-DECISION
```

No implementation preference may substitute.

# 4H. Certification invariant: current exact-SHA authority only

Only v2.6/current-candidate rules are normative.

Historical c409/historical bootstrap revision/carried-forward runtime revision/previous Team Operating revision material cannot override current source, Flow
Cards, tests or freezes.

Any unresolved stale normative contradiction:

```text
Outcome = BLOCKED-EVIDENCE
```

# 5. Certification invariant: no architecture demo layer

Mandatory references must live in canonical source roles.

Forbidden as final proof:

```text
Examples/
Samples/
Demo/
ReferenceExamples/
FakeFeature/
ArchitecturePlayground/
```

The production shape itself must be the teaching reference.

---

# 6. Certification invariant: no fake business model

Reference implementation is proactive.

But business ownership must still come from accepted product/context semantics.

Certification fails if a reference:

- invents an unapproved BC;
- invents a fake plan;
- invents a fake permission model;
- invents a fake Work lifecycle;
- invents a fake provider semantic;
- invents a fake Process whose workflow owner is unclear.

The required rule is:

```text
proactive architecture implementation
without speculative semantic ownership
```

---

# 7. Candidate identity record

Fill before certification:

```text
Repository:
Nqv1208/Notrelix

Branch:
architecture/backend-boundary-execution

PR:
113

AuditBaseline:
historical c409 snapshot2e9fcd968e79fa1e7ef6f208e1a559ef

ImplementationSHA:
<required>

ParentSHA:
<required>

BaseSHA:
<required>

CertificationDate:
<required>

Reviewer:
<required>
```

---

# 8. Upstream architecture compatibility

V2 must remain compatible with:

```text
Backend Boundaries V3
Backend Structural Topology v1.2
```

Required assertions:

```text
BC != project != service
semantic ownership unchanged
Public producer-owned
Port consumer-owned
ACL consumer-owned/pure
Adapter runtime-owned
Projection consumer-derived
Process workflow-owned
Platform mechanism-only
API not mutation orchestrator
pipeline-first preserved
shared DB != shared ownership
```

If a v2 reference implementation contradicts these:

```text
v2 reference is wrong
```

unless upstream architecture is explicitly revised.

---

# 9. CERT-V2-001 — Canonical BC inventory

PASS requires exactly the accepted semantic BC set:

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
Analytics/Reporting
```

Implementation feature folders do not create BCs automatically.

Example:

```text
Features/Notifications
```

must have explicit semantic classification.

---

# 10. CERT-V2-002 — Production project topology

PASS requires no BC-per-project extraction introduced by this execution.

Production projects remain:

```text
Notrelix.Domain
Notrelix.Application
Notrelix.Infrastructure
Notrelix.Platform
Notrelix.API
```

---

# 11. CERT-V2-003 — Reference graph completeness

PASS requires a single coherent cross-pack reference graph.

At minimum:

```text
Identity
→ Workspaces

Accounts
↔ Workspaces target mutation relationship

Workspace membership
→ Governance reference

WorkManagement
→ Work Integration Event
→ Automation
→ Work target action

WorkManagement
→ Work Integration Event
→ Analytics Projection

Document/Work resource
→ Collaboration ResourceRef

Billing capability
→ non-Billing consumer

Integrations semantic Port
→ provider adapter

Platform
→ outbox/delivery/dedup/retry
```

The graph may evolve in exact class naming.

The interaction roles may not disappear.

---

# 12. CERT-V2-004 — Interaction mechanism closure

Every **actual cross-BC interaction edge** must classify as one of:

```text
TAC-XC-A Public Direct
TAC-XC-B Port + ACL + Adapter
TAC-XC-C Integration Event
TAC-XC-D Projection
TAC-XC-E Target-owned Command/Action
TAC-XC-F Process Manager
```

Supporting patterns remain:

```text
Local vertical slice
Composite read
ResourceRef
BFF read composition
Provider Port + Infrastructure Adapter
Platform delivery mechanics
```

They are not seventh mechanisms.

PASS requires the current Flow Card authority to satisfy:

```text
47 Flow IDs remain exact
every Flow Card has FlowKind
every Flow Card has SupportingShapes
every Flow Card has Interactions[]

local-only Flow
→ Interactions[] may be empty

supporting/pipeline Flow
→ FlowKind = Supporting is allowed
→ Interactions[] may be empty only when its actual business cross-BC edges are
  certified on the participating business Flow Cards

Mixed Flow
→ Interactions[] must not be empty

cross-BC Flow
→ each actual edge has stable EdgeId
→ each edge mechanism is exactly TAC-XC-A..F

multi-edge Flow
→ each edge classified independently
→ no flat one-mechanism-per-Flow shortcut
```

Explicit FAIL cases:

```text
TAC-XC-G or another invented mechanism
supporting shape used as cross-BC Mechanism
unclassified actual cross-BC edge
coding implementation selected before a current-pack DEFERRED decision is closed
```

A Flow Card containing several valid mechanisms is allowed when they belong to
different `EdgeId`s.

---

# 13. CERT-V2-005 — Architecture role topology

PASS requires canonical roles contain real types only when used:

```text
Abstractions
Public
Ports
CrossContext
Processes
Projections
```

In v2, mandatory references MAY create these folders before user-facing
features ship.

But every created folder must contain executable production source.

No placeholder.

---

# 14. CERT-V2-006 — Gate ownership

PASS requires each invariant has one enforcement owner.

At minimum:

```text
ARCH-BC-001
ARCH-BC-002
ARCH-BC-003
ARCH-BC-004
ARCH-BC-005
ARCH-BC-006
ARCH-BC-007
ARCH-BC-008

STN-ARCH-001
STN-ARCH-002
STN-ARCH-005
STN-ARCH-006
STN-ARCH-007
STN-ARCH-008
```

V2 reference completeness tests supplement them.

They do not replace them.

---

# 15. CERT-V2-007 — Public purity hardened

PASS requires:

```text
no broad Application.Common.* allowance
no broad foreign Public allowance
own Public allowed
exact technical Common allowlist only
Domain forbidden
DbContext/repository forbidden
Infrastructure/Platform/API forbidden
transport/provider forbidden
internal Application forbidden
```

Mandatory self-tests must pass.

---

# 16. CERT-V2-008 — Services baseline monotonic

PASS requires exactly:

```text
actual Infrastructure/Services files
==
reviewed active baseline
```

with the baseline shrinking in the same change when a reviewed legacy file is
removed.

Deleted names must not remain reusable.

---

# 17. CERT-V2-009 — Common semantic no-growth

PASS requires:

```text
technical Common types explicit
business semantic debt exact
new business semantic Common types fail
removed debt allowance removed
```

Mandatory Team Operating Packs may not create convenience Common semantics.

---

# 18. CERT-V2-010 — Broker/provider purity

PASS requires:

```text
Domain/Application business code
does not reference Broker/provider runtime SDKs
```

Infrastructure/Platform may.

Reference event/provider flows must prove this separation.

---

# 18A. TAC-FRZ-016 — Team/BC Real Flow Catalog certification

Required catalog:

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
47 Flow IDs
46 executable/verification-required
1 conditional = BI-FLOW-05
```

Required local baseline states:

```text
Identity              VERIFIED
Accounts              VERIFIED
Workspaces            VERIFIED
Governance            VERIFIED
Work Management       VERIFIED
Documents             VERIFIED
Collaboration         VERIFIED
Automation            VERIFIED
Integrations          VERIFIED
Billing               VERIFIED
Analytics / Reporting VERIFIED
Platform / Foundation VERIFIED
```

Accounts requires both:

```text
IA-FLOW-06 local authenticated admin mutation
IA-FLOW-02/04/05 producer Public action/read patterns
```

Every executable FlowId must map to:

```text
existing production source
existing substantive evidence section
runtime-owner proof where needed
architecture gate
candidate SHA
```

Every actual cross-BC interaction must additionally map:

```text
FlowId:EdgeId
→ TAC-XC-A..F mechanism
→ DecisionStatus
→ owning production proof
→ mechanism-specific runtime/consistency evidence
```

`EdgeId` is a traceability sub-label only; it does not increase the 47-flow
catalog count.

A nonexistent evidence label fails this freeze.

`BI-FLOW-05` remains conditional at the audited candidate.

TAC-FRZ-016 freezes only when all 46 executable flows are individually
VERIFIED.
# 18B. TAC-FRZ-017 — Common public-signature semantic purity

FROZEN only when:

```text
TAC-GATE-022 passes
Application/Common public signatures contain no BC business vocabulary
IAccessGrantProjectionService no longer exposes AccountRole/WorkspaceRole
IA/WG flows use owner-preserving grant semantics
```

Inspection covers parameters, returns, properties, fields, generic arguments and
base/interface dependencies.

# 18C. TAC-FRZ-018 — Scoped Integration Event tenant envelope

FROZEN only when:

```text
tenant-scoped events carry authoritative AccountId + WorkspaceId
producer mappings preserve both
TenantContextConsumeFilter restores Account/Workspace tenant
representative consumers do not run as System
event compatibility/replay covers changed contracts
```

Normative invariant:

```text
WorkspaceId != null
→ AccountId != null
```

unless the event is explicitly Global/System.

# 18D. TAC-FRZ-019 — Calendar connection/binding/secret/webhook lifecycle

FROZEN only when:

```text
IntegrationConnection generic relationship role proven
CalendarIntegration Workspace binding role proven
IntegrationSecretVersion persisted SecretReference role proven
ConnectCalendar persists/reloads full graph
DisconnectCalendar deactivates CalendarIntegration first
CAL-CONN-001 governs generic Connection cleanup
provider secrets do not leak
inbound webhook is provider-verified
inbound receipt/dedup is Infrastructure technical state
outbound WebhookDelivery is not reused for inbound receipt
trusted tenant derives from verified Connection + CalendarIntegration
```

# 18E. TAC-FRZ-020 — Billing capacity semantics + hard-quota concurrency

FROZEN only when:

```text
BILL-LIMIT-001 resolved
zero-limit meaning explicit
unlimited representation explicit
AUTOMATION_RULE remains feature capacity unless product says otherwise
capacity owner action real
reservation/consume/release-or-compensate protocol real
last-slot concurrency safe
concurrent CreateAutomationRule race safe
duplicate/conflicting operation semantics proven
capacity not mislabeled billable UsageMetric
```

# 18F. PRE-M4 Interaction Architecture certification checkpoint

This is an **execution-entry checkpoint**, not a TAC milestone, not a new
freeze ID, and not a dedicated CI requirement.

Before TAC-M4 starts implementation, certification authority must confirm:

```text
all 47 Flow Cards coarse-reviewed
every actual cross-BC edge classified TAC-XC-A..F
no system-level mechanism ambiguity remains
IA-FLOW-01..06 deep interaction authority is executable
BOUND-TX-002/004 extraction blockers remain explicit
```

Exact M4 interaction authority:

| Edge | Mechanism | Required state |
|---|---|---|
| IA-FLOW-02:E1 | TAC-XC-E | BOUND-TX-004 current shared transaction; extraction blocked |
| IA-FLOW-03:E1 | TAC-XC-C | outbox + Platform delivery + tenant/dedup; eventual consumer reaction |
| IA-FLOW-04:E1 | TAC-XC-A | producer Public Direct; semantic-only remote readiness |
| IA-FLOW-05:E1 | TAC-XC-E | BOUND-TX-002 current shared transaction; extraction blocked |

Local baselines:

```text
IA-FLOW-01 → Interactions[] empty
IA-FLOW-06 → Accounts owner-local mutation baseline
```

Later-pack deep fields may remain explicit `DEFERRED-{MILESTONE}` where the
revised SPEC permits. That does not block M4 unless the deferred item creates a
system-level contradiction.

PRE-M4 source-pattern PASS additionally requires:

```text
M3A/M3C prerequisite findings closed
current production structural references pinned
cross-context runtime bindings have dedicated composition ownership
M6/M10 debt implementations explicitly excluded from reference eligibility
log-only/stub consumers excluded from Real Flow certification
no generic cross-context gateway/framework introduced
```

This checkpoint may accept an M8 source as a **structural TAC-XC-B reference**
for topology only. It MUST NOT certify the M8 Flow or Team Pack early.

---

# 19. CERT-V2-011 — IA Team Operating Pack certification

Mandatory:

```text
IA-FLOW-01..06
```

## Identity

`IA-FLOW-01` proves real local Identity mutation.

## Accounts local admin

`IA-FLOW-06` PASS requires:

```text
authenticated Account-scoped request
canonical Governance Account-admin authorization
Accounts Application mutation
Account.Rename owner behavior
real persistence/concurrency
no foreign private persistence
```

It must use the normalized TAC-FRZ-017-compliant grant/auth seam.

## Register→Accounts

`IA-FLOW-02:E1` is the pinned `TAC-XC-E` target-owned action reference.

PASS requires:

```text
Accounts.Public.PersonalAccountProvisioning boundary
Accounts.Public.Membership fact/action boundary
Accounts owns Account + owner-member mutation semantics
Identity does not use private Accounts persistence/Domain
BOUND-TX-004 current shared request transaction
success commits Identity + Accounts owner states
Accounts failure rolls back Identity
later failure cannot orphan Account
idempotent duplicate behavior
ExtractionBlocked = true
RuntimeSubstitutionReady = false/deferred
RemovalTrigger recorded
```

A remote command, async event or Saga implementation is not an equivalent
candidate while BOUND-TX-004 remains authoritative.

## Registration event

`IA-FLOW-03:E1` is the pinned `TAC-XC-C` reference.

PASS requires:

```text
Identity-owned committed registration fact
producer outbox enrollment
producer commit
real Platform delivery
tenant restore where scoped
consumer dedup
Workspace consumer as inbound adapter
Workspaces-owned Application mutation
consumer-local transaction
```

Producer commit must remain committed if a later Workspace delivery attempt
fails technically.

## Accounts Public owner flows

`IA-FLOW-04:E1` is the pinned `TAC-XC-A` authoritative read.

PASS requires:

```text
Accounts producer-owned Public fact
Workspaces direct in-process consumption
no foreign Accounts private persistence
no mandatory Workspaces Port merely for future service extraction
SemanticBoundaryReady = true
RuntimeSubstitutionReady = deferred/semantic-only
```

`IA-FLOW-05:E1` is the pinned `TAC-XC-E` target action under BOUND-TX-002.

PASS requires:

```text
Accounts producer-owned Public action
Accounts owns target mutation/business rejection
current shared request transaction
rollback/commit/idempotency proof
ExtractionBlocked = true
RuntimeSubstitutionReady = false/deferred
```

Pack FAILS if Accounts has only cross-context action evidence but no local
admin mutation reference.

Public source-topology closure is also mandatory for CERT-V2-011:

```text
Accounts/Public/Membership/
  → IAccountMembershipFacts
  → AccountMembershipAdmissionFact
  → IAccountMembershipActions

Accounts/Public/PersonalAccountProvisioning/
  → IAccountProvisioningActions
  → PersonalAccountProvisioningResult

no M4-touched Accounts Public contract remains under top-level technical buckets
CanonicalPathArchitectureTests public-topology rule = PASS
```

Behavioral verification obtained before this relocation remains useful evidence,
but it cannot freeze the M4 canonical copy-model until the source topology and
namespace consumers match the current SPEC.

When CERT-V2-011 passes, M4 becomes the first canonical Team Pack copy-model
for:

```text
Local Vertical Slice
TAC-XC-A Producer Public Direct
TAC-XC-C Integration Event
TAC-XC-E Target-owned Action
```

Each copy-model claim must resolve to a VERIFIED FlowId and substantive TESTS
evidence. M4 does not certify TAC-XC-B/D/F by implication.
# 20. IA mandatory references

Required:

```text
IA-REF-001 Identity producer fact
IA-REF-002 Accounts producer fact
IA-REF-003 Accounts target-owned mutation
```

No defer.

The revised interaction teaching cases are additionally mandatory:

```text
IA-FLOW-02:E1 → TAC-XC-E
IA-FLOW-03:E1 → TAC-XC-C
IA-FLOW-04:E1 → TAC-XC-A
IA-FLOW-05:E1 → TAC-XC-E
```

These edge rows supplement the historical `IA-REF-*` aliases; they do not
create new Flow IDs.

---

# 21. IA-REF-001 certification

PASS requires:

```text
Identity Public contract exists
producer implementation exists
exact lifecycle semantics defined
Workspaces consumer exists
Public purity passes
behavior tests pass
DI passes
```

Identity fact must not absorb:

```text
Workspace membership
Governance permission
Billing entitlement
```

---

# 22. IA-REF-002 certification

PASS requires:

```text
Accounts Public fact exists under `Accounts.Public.Membership`
Accounts-owned lifecycle meaning
consumer exists
behavior tests pass
```

Must not imply:

```text
Workspace invitation validity
Identity state
Governance permission
Billing seat/capability
```

unless explicitly owned by final semantic decision.

---

# 23. IA-REF-003 certification

PASS requires:

```text
Accounts target mutation boundary exists under `Accounts.Public.Membership`
producer-owned semantics
Workspaces does not use private Accounts Abstractions as public API
mutation executes Accounts invariant path
transaction semantics frozen
idempotency/failure tests pass
```

---

# 24. CERT-V2-012 — Accounts↔Workspaces transaction

Required historical bootstrap revision final state:

```text
TAC-FRZ-001 = FROZEN-WITH-BOUND-TX-002
Decision = B
```

Certification evidence must show:

```text
WorkflowOwner = Workspaces
AccountsMutationOwner = Accounts
WorkspacesMutationOwner = Workspaces
Accounts target mutation is producer-owned Public
one current request transaction
rollback test
successful commit test
idempotent retry test
SemanticBoundaryReady = true
RuntimeSubstitutionReady = false/deferred
ExtractionBlocker = true
RemovalTrigger recorded
```

Decision A or C is not a valid completion state for this candidate without
reopening architecture review.
# 25. Transaction decision record

Required exact record:

```text
DecisionId: TAC-TX-ACCOUNTS-WORKSPACES
Selected: B
ExceptionId: BOUND-TX-002
WorkflowOwner: Workspaces
AccountsMutationOwner: Accounts
WorkspacesMutationOwner: Workspaces
AtomicityRequired: true-for-current-candidate
PartialSuccess: not-certified-safe
Retry: idempotent current-request retry
Compensation/Reconciliation: not the active model while BOUND-TX-002 exists
UnknownOutcome: handled inside current local request transaction
SecurityProjection: committed/rolled-back with accepted flow
FutureRemoteImpact: extraction blocker
RemovalTrigger:
  Accounts/Workspaces service extraction
  or approved product decision allowing partial success/reconciliation
Tests: TAC-TX-001 + TAC-TX-002B
```

No field is left for the coding agent to choose.

## 25A. BOUND-TX-004 Register→Accounts extraction-blocker certification

PASS requires the exact current-candidate record:

```text
WorkflowOwner = Identity Registration
IdentityMutationOwner = Identity
AccountMutationOwner = Accounts
Atomicity = current shared request transaction
PartialSuccess = not-certified-safe
SemanticBoundaryReady = true
RuntimeSubstitutionReady = false/deferred
ExtractionBlocker = true
RemovalTrigger =
  Identity/Accounts physical extraction
  or approved product workflow allowing asynchronous Account provisioning
```

Certification FAILS if implementation silently changes this edge to:

```text
separate remote commit
eventual account provisioning
Saga/compensation
```

without an explicit reopened product/transaction decision.

# 26. CERT-V2-013 — WG Team Operating Pack certification

Mandatory:

```text
WG-FLOW-01..06
```

PASS requires real Workspaces and Governance local baselines, invitation
workflow and canonical authorization pipeline.

Additional v2.6 conditions:

```text
TAC-FRZ-017 Common signature purity passes
Page permission semantic is authoritative
WorkspaceMemberAdded and other scoped outward events satisfy TAC-FRZ-018
real runtime restores Account/Workspace tenant
```

A guessed Page permission semantic or Common role-signature leakage blocks
freeze.
# 27. WG mandatory references

```text
WG-REF-001 AcceptInvitation workflow
WG-REF-002 Governance authoritative contract
WG-REF-003 Workspace membership Integration Event
```

---

# 28. WG-REF-001 certification

PASS requires:

```text
Identity check independent
Account check independent
Invitation check independent
Workspace lifecycle check independent
target Accounts mutation explicit
Workspace local mutation explicit
security projection semantics explicit
selected transaction semantics tested
```

---

# 29. WG-REF-002 certification

Governance authority must be explicit, but certification MUST prove there is
only one canonical authorization stack.

Current source already contains authorization machinery under
`Application/Common/Security` and the pipeline.

PASS requires:

```text
existing canonical authorization path reused/hardened
Governance business semantics explicit
no second evaluator/decision-store/behavior stack
```

A Governance Public fact/query is required only if the selected
use-case-specific cross-context reference genuinely needs one outside the
standard pipeline.

Private role/policy/DbContext leakage remains forbidden.
# 30. Pipeline-vs-Governance certification

PASS requires a documented/executable distinction:

```text
standard authorization
→ pipeline

use-case-specific authoritative Governance fact/query
→ Governance Public
```

Reference source must not teach every handler to manually query Governance.

---

# 31. WG-REF-003 certification

Workspace membership event reference must reuse/harden existing producer
contracts such as:

```text
WorkspaceMemberAddedIntegrationEvent
WorkspaceMemberRemovedIntegrationEvent
```

PASS requires:

```text
producer = Workspaces
version/registry valid
outbox semantics valid
meaningful reference consumer executes
no duplicate membership event introduced
```
# 32. CERT-V2-014 — WM Team Operating Pack certification

Mandatory:

```text
WM-FLOW-01..05
```

Target action requirements remain:

```text
WorkspaceId enforced
ExecutorUserId authorized
OperationId target-deduplicated
duplicate/conflicting requests deterministic
```

Producer events must additionally satisfy TAC-FRZ-018:

```text
authoritative AccountId
authoritative WorkspaceId
exact producer mapping
TenantContextConsumeFilter restores tenant
```

Work producer source remains the only source authority for Analytics rebuild.
# 33. WM mandatory references

```text
WM-REF-001 CreateBoard local vertical slice
WM-REF-002 WorkManagement→Collaboration Port+Adapter
WM-REF-003 Work Public target action
WM-REF-004 Work Integration Event
```

---

# 34. WM-REF-001 certification

PASS requires:

```text
pipeline-first marker usage
context-local persistence
no artificial foreign Ports for standard scope/authz/idempotency
behavior tests green
```

---

# 35. WM-REF-002 certification

Before M6 implementation, the revised SPEC decision:

```text
WM-FLOW-02:E1
DecisionStatus = DEFERRED-M6-SOURCE-NORMALIZATION
Mechanism = TAC-XC-B
```

must be closed.

Decision closed at M6 Phase 0 (frozen):

```text
WM-FLOW-02:E1
DecisionStatus = CLOSED-FROZEN
Mechanism = TAC-XC-B (in-process adapter; remote deferred-by-design)
Producer boundary = Collaboration/Public/ResourceSummary
  ICollaborationResourceSummary.GetSummariesAsync((kind, resourceId)...)
  CollaborationResourceSummaryFact(ResourceId, ResourceKind, CommentCount, AttachmentCount)
Consumer = WorkManagementCollaborationReadAdapter maps board-item ids
  onto (kind, id) addressing and the WorkItemCollaborationCounts shape
No foreign Collaboration DbContext in the consumer path
No ACL-only class: the (kind, id) → counts mapping is mechanical and
  stays inside the adapter
```

PASS requires:

PASS requires:

```text
WorkManagement consumer-owned Port
WorkManagement-owned pure ACL if needed
Infrastructure adapter
Collaboration-owned stable semantic read boundary
no foreign Collaboration DbContext treated as public cross-BC contract
no legacy Data/ReadPorts
DI mapping
behavior tests
```

---

# 36. WM-REF-003 certification

Pinned mutation:

```text
MoveBoardItem
```

PASS requires:

```text
WorkManagement Public IWorkItemActions MoveItem semantic
one producer-local MoveBoardItem mutation implementation
Automation cannot access Work DbContext/Domain internals
Public contract transport/MediatR neutral
```

previous Team Operating revision additionally requires all Public execution identity fields to be enforced:

```text
WorkspaceId
→ target Work item scope validated

ExecutorUserId
→ target-owned authorization validated

OperationId
→ WorkManagement-owned idempotency/dedup validated
```

Certification scenarios:

```text
authorized valid move → PASS
wrong WorkspaceId → rejected
unauthorized ExecutorUserId → rejected
same OperationId + same request → one logical mutation
same OperationId + conflicting request → deterministic conflict
```

If the fields exist in the contract but are ignored by producer code:

```text
WM-REF-003 = FAIL
```
# 37. WM-REF-004 certification

PASS requires source-aware event closure.

Current WorkManagement events are reused where suitable.

Additionally, the existing consumer-coupled:

```text
BoardItemMemberAssignedForAutomationIntegrationEvent
```

must be migrated to WorkManagement producer ownership because its existing
`ARCH-BC-007` classification is `MIGRATE-ON-TOUCH`, and historical bootstrap revision materially touches
that path.

Certification requires:

```text
WorkManagement-owned replacement/normalized fact
mapper updated
Automation consumer/evaluator updated
registry updated
identity/correlation preserved
old contract retired per compatibility policy
ARCH-BC-007 reviewed allowance removed/shrunk
no parallel duplicate fact event
```
# 38. CERT-V2-015 — DC Team Operating Pack certification

Mandatory:

```text
DC-FLOW-01..07
```

Documents CreatePage and ArchivePage must be real.

BoardItem and Page comment variants remain independently certified.

All Workspace-scoped Page/Comment/Mention outward events must satisfy
TAC-FRZ-018.

## Mention actor

PASS requires:

```text
trusted actor captured at owner mutation
owner state and/or Domain Event owns actor
outward event maps actor exactly
consumer uses exact actor
```

Forbidden generic mapping:

```text
MentionedByUserId = MentionedId
```

Certification uses distinct actor and mentioned-user identities.

## Notification tenant

PASS requires exact Account/Workspace tenant and no `Guid.Empty`.

Stub/log-only consumers remain excluded from business-flow evidence.
# 39. DC mandatory references

```text
DC-REF-001 Collaboration ResourceRef
DC-REF-002 Collaboration local mutation
DC-REF-003 target fact/access boundary
DC-REF-004 Notifications semantic classification
```

---

# 40. DC-REF-001 certification

PASS requires reuse of the canonical SharedKernel `ResourceRef` or an explicitly
approved migrated successor.

Current Collaboration Comment/Mention/Reaction/Attachment state already uses
ResourceRef.

Certification fails if TAC introduced a second equivalent ResourceRef.

Required semantics:

```text
stable target identity
no ownership transfer
no cross-BC aggregate navigation
existence separate
authorization separate
scope separate
```
# 41. DC-REF-002 certification

Pinned local mutation:

```text
CreateCommentCommand.ForBoardItem
```

PASS requires:

```text
Collaboration creates/stores Comment
canonical SharedKernel ResourceRef used
Work aggregate not loaded/mutated by Collaboration handler
existing resource-scoped authorization path used
```

Thread/Mention/Activity cannot substitute for the bootstrap reference.
# 42. DC-REF-003 certification

When target metadata/access is required:

```text
Producer.Public
or
Consumer Port+Adapter
```

must be used.

At least one real ACL must exist globally.

If this pack owns it, ACL purity must pass.

---

# 43. DC-REF-004 certification

`Features/Notifications` must have one explicit semantic outcome:

```text
Collaboration-owned implementation module
migrated under Collaboration
exact compatibility debt
```

It must not remain an implicit twelfth BC.

---

# 44. CERT-V2-016 — AI Team Operating Pack certification

Mandatory:

```text
AI-FLOW-01..07
```

Automation carried-forward execution/Work/N8n proofs remain mandatory.

## AI-FLOW-05

ConnectCalendar PASS requires persisted and reloaded:

```text
IntegrationConnection
IntegrationSecretVersion
CalendarIntegration(ConnectionId)
```

Roles are distinct:

```text
IntegrationConnection → generic provider relationship
IntegrationSecretVersion → durable SecretReference per version
CalendarIntegration → Workspace Calendar binding
```

In-memory `CurrentSecretRef` alone is insufficient.

Duplicate provider callback/logical relationship/binding cases must be safe.

Secret-store success + DB failure requires explicit cleanup/reconciliation.

## AI-FLOW-06

Disconnect must mutate:

```text
CalendarIntegration.Deactivate
```

first.

Generic Connection cleanup only follows frozen `CAL-CONN-001`.

Tests distinguish another active binding vs last relevant binding and all
provider cleanup outcome classes.

## AI-FLOW-07

Provider webhook PASS requires:

```text
raw provider signature/timestamp/replay verification
trusted IntegrationConnection
active CalendarIntegration
trusted tenant derivation
Infrastructure inbound receipt/dedup
provider-neutral Integrations processing
```

Forbidden:

```text
user-session auth as provider authenticity
untrusted payload selecting tenant
outbound WebhookDelivery as inbound receipt
```

AI pack requires TAC-FRZ-019 FROZEN.
# 45. AI mandatory references

```text
AI-REF-001 Automation consumes Work event
AI-REF-002 Automation target Work action
AI-REF-003 Automation Process Manager
AI-REF-004 Integrations provider Port+Adapter
```

No defer.

---

# 46. AI-REF-001 certification

PASS requires:

```text
Work event consumer registered
Automation local execution/process state created/found
duplicate event safe
source resource identity preserved
```

---

# 47. AI-REF-002 certification

Required production chain:

```text
AutomationExecution
→ production Automation MoveItem executor
→ IWorkActionPort
→ Automation ACL
→ WorkItemActionAdapter
→ WorkManagement Public MoveItem
→ target scope/auth/idempotency
→ MoveBoardItem
```

The following does NOT satisfy certification:

```text
integration test constructs IWorkActionPort directly
→ calls MoveItemAsync
```

unless a production Automation Application executor also exists and is
executed by a higher-level production-flow test.

PASS requires:

```text
AutomationActionType.MoveItem production path
ExecutionId → OperationId
Automation principal → ExecutorUserId + WorkspaceId
invalid action config handled before target call
target business rejection → Automation terminal business failure
technical target failure remains technical
```
# 48. AI-REF-003 certification

Pinned process authority:

```text
AutomationExecution
```

Required execution owner:

```text
Automation Application
```

Required N8n path:

```text
DeduplicationConsumeFilter
→ N8nDispatchConsumer
→ N8nDispatchUseCase
→ Integrations Public action
```

PASS requires:

```text
no second Process aggregate
Domain broker-neutral
consumer has no IAutomationDbContext
consumer does not own Automation lifecycle
Application owns business/execution progression
```

## Retryable technical failure

Bootstrap retry model:

```text
delivery-owned retry
```

Certification proof MUST run through the real dedup transaction.

Required result:

```text
consumer throws retryable technical exception
outer delivery transaction rolls back
dedup success not committed
Automation retry AttemptCount/evidence not falsely persisted
later retry may run
```

A direct consumer test with manual `SaveChangesAsync()` does not certify this.

## Success

```text
AutomationExecution succeeds
consumer returns
dedup success + Automation state commit together
```

## Terminal failure

```text
AutomationExecution terminal failure commits
delivery acknowledged
no broker retry
```

## Unknown outcome

Without explicit reconciliation/idempotency:

```text
must not blindly redeliver
must commit a durable terminal/manual-reconciliation outcome
```

Any implementation that saves retry evidence and then relies on a throwing
consumer inside the same outer transaction is not certified.
# 49. AI Process persistence certification

If process is durable:

```text
restart/resume test required
```

If current reference process can complete entirely synchronously but still
stores explicit execution state:

```text
state persistence semantics must be proven
```

A transient service method alone does not satisfy Process Manager reference.

---

# 50. AI background actor certification

PASS requires explicit:

```text
principal/actor
workspace/account scope
authorization path
correlation
idempotency
```

At least one denied security path must be tested.

---

# 51. AI-REF-004 certification

Required exact provider chain:

```text
N8nDispatchUseCase
→ Integrations.Public IN8nWebhookActions
→ Integrations provider Port IN8nClient
→ Infrastructure N8nClient
```

PASS requires:

```text
one canonical N8n provider seam
old Common IN8nClient no longer authoritative
Public result contains no HTTP/broker/provider SDK type
ExecutionId remains operation/correlation identity
```
# 52. N8n freeze certification

All required cases must pass:

```text
webhookPath
webhook_path
whitespace
leading slash
invalid JSON
missing property
non-string property
```

No behavior assumption without test.

---

# 53. CERT-V2-017 — BI Team Operating Pack certification

Mandatory:

```text
BI-FLOW-01..05
```

BI-FLOW-05 remains conditional.

Before capability certification:

```text
BILL-LIMIT-001 = RESOLVED
```

Certification records zero-limit meaning and unlimited representation.

`AUTOMATION_RULE` is feature capacity unless explicit product authority makes it
billable metering.

BI-FLOW-02 must prove real Billing capacity mutation with logical operation
identity and reserve/consume/release-or-compensate semantics.

Mandatory last-slot DB race:

```text
remaining = 1
two concurrent reservations
→ exactly one accepted
```

Mandatory cross-BC race:

```text
remaining = 1
two concurrent CreateAutomationRule commands
→ exactly one successful rule
→ exactly one slot consumed
```

Feature failure after reservation must release/compensate under the frozen
consistency protocol.

Duplicate same operation cannot double-consume; conflicting duplicate must fail.

Capability-after-capacity must change through production lifecycle.

BI pack requires TAC-FRZ-020 FROZEN.
# 54. BI mandatory references

```text
BI-REF-001 Billing Public capability
BI-REF-002 non-Billing capability consumer
BI-REF-003 provider boundary classification/implementation
```

`BI-REF-003` must use real existing provider seam if provider runtime exists.

If no external provider exists at all, the pack still requires an explicit
provider-boundary classification, but may not invent fake provider semantics.

The pack itself still cannot defer.

---

# 55. BI-REF-001 certification

Required producer surface:

```text
IBillingCapabilityFacts
BillingCapabilityFact
BillingCapabilityCode.AUTOMATION_RULE
```

PASS requires no private PlanTier/SubscriptionTier/provider leakage.

## Entitlement scope

Certification must prove:

```text
Account-scoped entitlement
→ applies according to Account Billing policy

Workspace-scoped entitlement
→ applies only to TargetWorkspaceId == requested WorkspaceId

Workspace A entitlement
→ cannot authorize Workspace B
```

If multiple applicable entitlements exist, Billing precedence/combination must
be deterministic and tested.

An arbitrary database `FirstOrDefault` over applicable rows is not sufficient.

## Usage scope

Capability usage must use the same Billing scope semantics as the usage writer.

For the pinned workspace-consumed Automation rule capability:

```text
AccountId
WorkspaceId
AUTOMATION_RULE
```

must remain aligned across read/write paths.
# 56. BI-REF-002 certification

Pinned consumer:

```text
CreateAutomationRuleCommandHandler
```

PASS requires:

```text
capability checked before mutation
RequestedAmount = 1
correct AccountId
correct WorkspaceId
no private tier branch
```

previous Team Operating revision additionally requires the production lifecycle to affect authoritative
Billing usage.

Required sequence:

```text
capability before create
→ create AutomationRule
→ Billing usage +1
→ capability after create reflects reduced remaining capacity
```

Required idempotency:

```text
same creation/retry identity
→ no double usage increment
```

Required isolation:

```text
Workspace A rule creation
→ Workspace B usage unchanged
```

If a real capacity-releasing lifecycle exists:

```text
release/retire
→ usage -1 exactly once
```

If no such lifecycle exists, certification records that specific release path
as `NOT-APPLICABLE-UNTIL-LIFECYCLE-EXISTS`; creation usage remains mandatory.

The consistency model for AutomationRule + Billing usage must be explicit.

Hidden cross-BC atomicity:

```text
FAIL
```
# 56A. BI-REF-003 certification

For audit snapshot c409:

```text
BI-REF-003 = NOT-APPLICABLE-AT-CANDIDATE
```

PASS requires:

```text
no fake Stripe/payment-provider implementation introduced
Billing Domain/Application/Public remain provider-neutral
provider-specific DTO/status/SDK absent from Billing Public
```

If a real Billing provider appears at execution candidate SHA:

```text
BLOCKED-SOURCE-DRIFT
```

and re-audit is required.

# 56B. BI usage consistency certification

Certification record must state exactly one:

```text
BOUND-TX-003
or
IDEMPOTENT-USAGE-PROTOCOL
```

If `BOUND-TX-003`:

```text
workflow owner
mutation owners
business invariant
rollback semantics
extraction blocker
removal trigger
tests
```

are mandatory.

If `IDEMPOTENT-USAGE-PROTOCOL`:

```text
operation identity
failure mode
retry/reconciliation owner
partial-success visibility
tests
```

are mandatory.

No blank/implicit consistency model is allowed.

# 57. Common entitlement debt certification

Selected Billing migration must shrink:

```text
Application/Common entitlement semantic debt
```

where the reference consumer previously depended on it.

Remaining debt:

```text
exact
no-growth
non-normative
```

---

# 58. CERT-V2-018 — AR Team Operating Pack certification

Mandatory:

```text
AR-FLOW-01 live Work placement projection
AR-FLOW-02 local Analytics query
AR-FLOW-03 projection rebuild
AR-FLOW-04 failure/recovery
```

PASS requires derived Analytics placement state outside Domain.

## Live

```text
Work producer event
→ Platform
→ Analytics consumer
→ local placement projection
```

with idempotency/order/revision/tenant isolation.

## Local query

Normal query reads Analytics local projection only.

It must not require Work Public/DbContext in the normal read path.

## Rebuild

M10 closed the revised SPEC decision (frozen at M10 Phase 0):

```text
WM-FLOW-05 / AR-FLOW-03 projection-source edge
DecisionStatus = CLOSED-FROZEN (TAC-XC-B, M10-PORT-OWNERSHIP-NORMALIZATION)
```

PASS requires the frozen canonical runtime shape that preserves:

```text
WorkManagement owns authoritative projection snapshot truth
Analytics owns the rebuild workflow and local projection
Analytics cannot read Work DbContext directly
```

The required frozen path is:

```text
Analytics rebuild
→ Analytics-owned consumer source/rebuild Port (Application)
→ Infrastructure delegate adapter
→ WorkManagement.Public IWorkItemProjectionSource
→ Work Application owner implementation
→ snapshot
→ local reconcile
```

Analytics cannot implement Work producer truth or directly inject
`IWorkManagementDbContext`.

## Recovery

PASS requires duplicate, stale, out-of-order, crash/retry, source-unavailable,
concurrent-live-event and drift-rebuild coverage.

Analytics remains derived state, never Work source authority.
# 59. AR mandatory reference

```text
AR-REF-001 Work Management analytical Projection
```

Must be implemented.

No defer.

---

# 60. Analytics Projection certification

Pinned implementation:

```text
WorkspaceWorkItemPlacementProjection
```

Mandatory placement:

```text
Application/Features/Analytics/Projections/WorkItemPlacement
```

PASS requires:

```text
no Domain placement projection entity
no Domain IWorkspaceScoped workaround
Infrastructure owns projection persistence/runtime mapping
```

## Producer-owned source contract

Required:

```text
WorkManagement.Public IWorkItemProjectionSource
```

with producer-owned Application implementation:

```text
WorkManagement Application
→ IWorkManagementDbContext
→ WorkItemPlacementSnapshot
```

Analytics-side Infrastructure may adapt/delegate to this Public contract.

Certification FAILS if:

```text
Infrastructure/CrossContext/Analytics/WorkManagement
→ injects IWorkManagementDbContext
→ implements producer Public source directly
```

or if Analytics event consumers directly access Work persistence.

## Rebuild

Required path:

```text
Analytics rebuild
→ consumer adapter/Port
→ WorkManagement.Public source
→ producer Application implementation
→ Analytics local projection
```

Required proof:

```text
empty source
multiple items
move
archive
duplicate/stale input
cross-workspace isolation
drift repair
```

`ReportingSnapshot` remains unrelated exact/no-growth LegacyGap debt.
# 61. Analytics source-truth certification

PASS requires:

```text
Analytics does not mutate WorkManagement
Analytics Application does not use Work DbContext
Projection is derived only
```

---

# 62. Analytics rebuild certification

Required:

```text
incremental source history
≈
rebuild result
```

for business-relevant projection state.

Rebuild source must be approved:

```text
event replay
or producer Public/snapshot boundary
```

not arbitrary foreign persistence.

---

# 63. Analytics ordering certification

One explicit strategy must be implemented/tested:

```text
producer revision
event ordering key
commutative aggregation
gap detection + rebuild
```

Undefined ordering:

```text
FAIL
```

---

# 64. CERT-V2-019 — PF Team Operating Pack certification

Mandatory:

```text
PF-FLOW-01..07
```

All carried-forward request/outbox/dedup/retry/replay/background-actor proofs
remain mandatory.

PF-FLOW-07 requires representative scoped event families to run:

```text
producer mutation
→ outbox
→ broker
→ TenantContextConsumeFilter
→ consumer
```

Assert exact AccountId, WorkspaceId and non-System tenant context.

Event contract changes introduced by tenant/actor corrections must be covered by
PF-FLOW-05 compatibility/upcast/replay behavior according to Platform policy.

PF pack requires TAC-FRZ-018 FROZEN.
# 65. PF mandatory mechanism proof

Required:

```text
outbox
message identity
delivery
consumer delivery state/dedup
retry
terminal technical failure/dead-letter model
Broker/provider confinement
semantic purity
```

---

# 66. Platform business-neutral certification

PASS requires Platform not own:

```text
Work rules
Automation progression
Billing capability
Governance permission
Workspace membership meaning
```

Platform may route/serialize technical contract identity.

It may not interpret business meaning.

---

# 66A. v2.6 cross-pack semantic-runtime integration certification

Required chains:

```text
Accounts RenameAccount
→ Governance auth
→ Accounts owner mutation
→ TAC-FRZ-017 compliant seam

Register
→ Accounts.Public
→ BOUND-TX-004
→ scoped registration event
→ tenant restore
→ Workspace provisioning

Workspace/Work/Documents/Collaboration scoped events
→ AccountId + WorkspaceId
→ TenantContextConsumeFilter
→ correct tenant

Mention actor A mentions user B
→ actor A preserved
→ Notification actor/tenant exact

ConnectCalendar
→ secret store
→ IntegrationSecretVersion
→ IntegrationConnection
→ CalendarIntegration
→ DB reload

verified Calendar webhook
→ same trusted Connection/binding
→ Infrastructure receipt/dedup
→ correct tenant

DisconnectCalendar
→ CalendarIntegration deactivate
→ CAL-CONN-001 generic Connection policy

remaining capacity = 1
→ concurrent CreateAutomationRule A || B
→ one success + one consumed slot
```

All chains must execute production owners; test-only direct seam composition is
insufficient.

# 67. CERT-V2-020 — Flagship Work→Automation integration

Required exact chain:

```text
Work mutation
→ Work Integration Event
→ outbox
→ Platform delivery
→ Automation consumer
→ Automation Process
```

PASS requires one semantic Automation execution per source event.

---

# 68. CERT-V2-021 — Automation→Work integration

Continue chain:

```text
Automation Process
→ IWorkActionPort
→ Infrastructure adapter
→ Work Public action
→ Work mutation
```

PASS requires target mutation authority remains WorkManagement.

---

# 69. CERT-V2-022 — Work→Analytics integration

Same Work event:

```text
→ Analytics consumer
→ Projection
→ Query
```

PASS requires source Work state unaffected by Analytics failure.

---

# 70. CERT-V2-023 — Fan-out independence

If Automation and Analytics consume the same Work event:

```text
Automation failure
must not roll back Work source
must not corrupt Analytics

Analytics failure
must not roll back Work source
must not invalidate successful Automation semantics
```

Platform retry handles technical delivery according to mechanism.

---

# 71. CERT-V2-024 — Identity/Accounts/Workspaces integration

Required chain:

```text
Identity fact
→ invitation workflow
→ Accounts target mutation
→ Workspaces membership mutation
```

PASS according to M2 transaction decision.

---

# 72. CERT-V2-025 — Workspace→Governance integration

Required reference must prove:

```text
Workspaces remains membership owner
Governance remains permission/policy owner
```

The exact interaction may be:

```text
membership event reaction
or authoritative query composition
```

but must be executable.

---

# 73. CERT-V2-026 — Billing→consumer integration

Required:

```text
Billing Public capability
→ selected consumer
```

PASS for:

```text
allowed
denied
```

No private Billing plan type crosses boundary.

---

# 74. CERT-V2-027 — ResourceRef→Collaboration integration

Required:

```text
foreign target
→ Collaboration local mutation
```

PASS requires target source state is unchanged.

Existence/authorization checks remain separate.

---

# 75. CERT-V2-028 — Provider integration

Required selected provider flow:

```text
Application semantic request
→ semantic Port
→ Infrastructure adapter
→ provider boundary
→ semantic result
```

PASS for:

```text
success
technical failure
configuration/credential failure where applicable
```

---

# 76. CERT-V2-029 — Composite read reference

Identity Bootstrap remains supporting reference.

PASS requires:

```text
consumer Port
Infrastructure adapter
multiple read sources allowed
no mutation
no producer policy recreation
no foreign Domain leakage
handler runtime-substitutable
behavior tests
DI
```

---

# 77. CERT-V2-030 — ACL reference

Pinned ACL:

```text
Automation MoveItem semantic
→ pure Automation-owned mapper
→ WorkManagement Public MoveItem semantic
```

Required evidence:

```text
Consumer = Automation
Producer = WorkManagement
Input = itemId + targetGroupId + execution/principal context
Output = Work Public MoveItem request
Reason = consumer action vocabulary + execution identity are translated into
         producer mutation contract
Pure Application = yes
Transport = none
Persistence = none
Provider SDK = none
Tests = TAC-AI-011
```

No alternative ACL may be selected for certification.
# 78. CERT-V2-031 — ResourceRef reference

At least one real production reference must be certified.

Required:

```text
stable scalar identity
no ownership transfer
no foreign navigation
separate existence check
separate authorization check
separate scope check where relevant
```

---

# 79. CERT-V2-032 — Background actor reference

Automation reference must demonstrate the repository's approved model for
background protected mutation.

Required:

```text
actor
scope
authorization
correlation
idempotency
```

No implicit bypass.

---

# 80. CERT-V2-033 — Provider boundary reference

At least one provider flow must demonstrate:

```text
Application semantic Port
Infrastructure SDK/HTTP adapter
```

The provider may change without rewriting Application business use-case
semantics.

---

# 81. CERT-V2-034 — Direct Public reference

At least one direct Producer.Public reference must remain.

Preferred:

```text
Workspaces → Identity.Public
```

This teaches teams:

```text
Port is not mandatory for every cross-context read
```

---

# 82. CERT-V2-035 — Consumer Port reference

At least one read Port reference:

```text
WorkManagement → Collaboration
```

and one target mutation Port reference:

```text
Automation → WorkManagement
```

must exist.

---

# 83. CERT-V2-036 — Target-owned mutation reference

Two distinct teaching cases required:

```text
Workspaces → Accounts
Automation → WorkManagement
```

The first teaches transaction decision.

The second teaches process/background target mutation.

---

# 84. CERT-V2-037 — Integration Event reference

At least one full Work event reference is mandatory.

Workspace membership event is also mandatory under WG pack.

Each must:

```text
state producer fact
be versioned
be outbox-delivered
have real consumer path
```

---

# 85. CERT-V2-038 — Process Manager reference

Automation Process is mandatory.

Certification fails if source has only:

```text
event handler
→ immediate direct action
```

with no explicit workflow/execution state when the reference is intended to
teach Process Manager semantics.

---

# 86. CERT-V2-039 — Projection reference

Analytics Projection is mandatory.

Certification fails if Analytics remains only:

```text
Abstractions
```

or performs live foreign table joins instead of a local reference projection.

---

# 87. CERT-V2-040 — Billing reference

Billing must no longer be represented only by inward `Abstractions`.

At least:

```text
Public capability
producer implementation
consumer
```

must exist.

---

# 88. CERT-V2-041 — Governance reference

Governance must have an executable authoritative permission contract.

A private role enum or internal service is not sufficient.

---

# 89. CERT-V2-042 — Integrations reference

Integrations must provide the pinned executable N8n semantic provider
Port/adapter reference:

```text
N8nDispatchUseCase
→ Integrations.Public IN8nWebhookActions
→ Integrations provider Port IN8nClient
→ Infrastructure N8nClient
```

Calendar is not a substitute for this bootstrap reference.

---

# 90. CERT-V2-043 — Team pack positive existence

Each mandatory pack must pass a positive existence check.

Required table:

| Pack | Mandatory source exists | Behavior tests | Integration | DI | Architecture gates | Result |
|---|---:|---:|---:|---:|---:|---|
| IA | | | | | | |
| WG | | | | | | |
| WM | | | | | | |
| DC | | | | | | |
| AI | | | | | | |
| BI | | | | | | |
| AR | | | | | | |
| PF | | | | | | |

Any missing mandatory column:

```text
FAIL
```

unless a specific column is genuinely not applicable to that pack by SPEC.

---

# 91. CERT-V2-044 — Team pack negative protection

Each pack must also have architecture protection.

Examples:

## IA

Reject:

```text
Workspaces → Accounts DbContext
```

## WG

Reject:

```text
consumer → Governance private role/internal policy
```

## WM

Reject:

```text
Automation → Work Domain/DbContext
```

## DC

Reject:

```text
Comment → Document aggregate navigation
```

## AI

Reject:

```text
provider SDK in Application
```

## BI

Reject:

```text
consumer PlanTier branch
```

## AR

Reject:

```text
Analytics → Work DbContext
```

## PF

Reject:

```text
Platform business policy
```

Positive reference + negative gate are both mandatory.

---

# 91AC. CERT-V2-044AC — v2.6 semantic-authority blocker closure

| Finding | Required closure |
|---|---|
| Accounts lacks local admin exemplar | IA-FLOW-06 RenameAccount |
| Common grant contract leaks role types | TAC-FRZ-017 |
| scoped event can omit AccountId | TAC-FRZ-018 |
| Mention actor absent/mis-mapped | owner actor fact + exact mapping |
| Calendar Connection/binding collapsed | distinct models + persisted relation |
| secret reference only in memory | IntegrationSecretVersion round-trip |
| disconnect targets generic Connection by default | CalendarIntegration first + CAL-CONN-001 |
| inbound webhook reuses outbound model | Infrastructure inbound receipt/dedup |
| provider webhook relies on user auth | provider verification boundary |
| Billing zero/unlimited magic | BILL-LIMIT-001 |
| hard quota sequential-only | last-slot concurrency proof |
| capacity conflated with commercial metering | capacity semantics |
| evidence table cites nonexistent label | exact evidence resolution |
| stale old-revision rule conflicts | non-normative historical cleanup |

Any unresolved mandatory row blocks closure.

# 91AB. CERT-V2-044AB — previous Team Operating revision Team Framework blocker closure

Every row is mandatory unless explicitly marked conditional:

| Finding | Required closure |
|---|---|
| Identity Register uses private Accounts provisioning | Accounts.Public + BOUND-TX-004 |
| Documents ArchivePage is NotImplemented | real ArchivePage flow |
| Documents PublishPage lacks Domain transition | no fabricated implementation |
| BoardItem/Page comment auth variants differ | independent canonical proofs |
| Mention event lacks AccountId | authoritative versioned AccountId propagation |
| Notification uses Guid.Empty AccountId | removed |
| Stub/log-only consumers counted as business evidence | excluded/classified |
| ConnectCalendar NotImplemented | real connection flow |
| DisconnectCalendar NotImplemented | real disconnect flow |
| Calendar webhook NotImplemented/session-auth shaped | verified provider inbound boundary |
| Billing has query without real usage mutation | BI-FLOW-02/03/04 |
| paired team could pass with one BC | TAC-FRZ-016 prevents |
| Platform event evolution/replay unproven | PF-FLOW-05 |

Unresolved mandatory row:

```text
BLOCKED-IMPLEMENTATION
or
BLOCKED-DECISION
```

never `ARCHITECTURE-CLOSED`.

# 91AA. CERT-V2-044AA — PR #113 runtime blocker closure

For the candidate derived from PR #113, every row must be resolved.

| Blocker | Required final evidence |
|---|---|
| N8n retry state can be rolled back by delivery transaction | real consume-filter retry test proves delivery-owned retry semantics |
| Work target ignores OperationId | target idempotency storage + duplicate/conflict tests |
| Work target ignores WorkspaceId | wrong-workspace rejection |
| Work target ignores ExecutorUserId | real target authorization allow/deny proof |
| IWorkActionPort only exercised directly by tests | production Automation MoveItem executor + higher-level integration |
| Billing workspace entitlement not enforced | Workspace A/B entitlement isolation |
| AutomationRule lifecycle does not update usage | create → usage +1 → capability changes |
| Billing usage consistency implicit | BOUND-TX-003 or explicit idempotent usage protocol |
| Analytics adapter reads Work DbContext directly | producer Application source implementation + delegating consumer adapter |
| Analytics projection in Domain | zero Domain placement projection entity |

Any unresolved row:

```text
Outcome = BLOCKED-IMPLEMENTATION
```

even if all GitHub Actions checks are green.

# 91A. CERT-V2-044A — Known c409 source-hotspot closure

The following audit anchors must be explicitly dispositioned at final SHA:

| Audit anchor | Required closure |
|---|---|
| Existing WorkManagement Integration Events | reuse/harden; no duplicate generic Work event |
| WorkspaceMemberAdded/Removed Integration Events | reuse/harden; no duplicate membership event |
| BoardItemMemberAssignedForAutomationIntegrationEvent | migrate to WorkManagement ownership; remove ARCH-BC-007 debt |
| AutomationExecution | reuse as workflow/process state unless explicit replacement |
| AutomationExecution.RequeueForRedelivery | remove/reframe broker semantics from Domain |
| N8nDispatchConsumer | thin broker adapter; no Infrastructure-owned Automation workflow |
| IN8nClient / N8nClient | normalize/reuse one provider seam |
| SharedKernel ResourceRef | reuse; no duplicate |
| Common/Entitlements | migrate/normalize into Billing ownership |
| Common/Security authorization stack | reuse/harden; no second Governance evaluator stack |

Any row left `UNRESOLVED` blocks closure.

# 92. CERT-V2-045 — Exact Real Flow manifest

Required:

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
47 Flow IDs
46 executable/verification-required
1 conditional = BI-FLOW-05
```

Manifest fields:

```text
FlowId
Team
BoundedContext
Disposition
FlowKind
SupportingShapes

SemanticOwner
PersistenceAuthority
RuntimeOwner
ConcurrencyAuthority

EntryPoint
ProductionSource
ProductionReachability

DecisionDependencies
TransactionOrConsistency
ActorScope
Authorization
Idempotency

Contract
RuntimeAdapter

Interactions:
  - EdgeId
    SourceBC
    TargetBC
    Mechanism
    DecisionStatus
    ContractOwner
    PortOwner
    WorkflowOwner
    MutationOwner
    ProjectionOwner
    ProcessOwner
    CallerWaitsForOutcome
    ConsistencyModel
    CurrentBinding
    RemoteReadiness
    FutureDistributedBinding
    TransactionBoundary
    OutboxRequired
    ConsumerDedupRequired
    RetryOwner
    UnknownOutcomePolicy
    ExtractionBlocker
    RemovalTrigger

BehaviorEvidenceId
IntegrationEvidenceId
RuntimeOwnerProofId
ArchitectureGateId

CandidateSHA
KnownDebt
Status
```

`Interactions: []` is valid for a truly local flow.

Evidence IDs must resolve to existing substantive TESTS v2.6 sections.

A FlowId appearing only in a table is not certified.
# 93. CERT-V2-046 — Reference source reachability

Every mandatory reference must be exercised.

A production type with no behavior/integration test does not satisfy v2.

No dead teaching source.

---

# 94. CERT-V2-047 — Reference no-stub proof

Mandatory reference implementations cannot be only:

```text
NotImplementedException
default return
always-success fake
```

Test-only fakes are allowed.

Production reference implementations must execute real architecture path.

---

# 95. CERT-V2-048 — DI completeness

All runtime-bound reference contracts must resolve through production
composition.

At minimum:

```text
Identity facts
Accounts facts/action
Governance contract
WorkManagement Collaboration Port
Automation Work Port
Work Public action implementation
Automation consumer/process dependencies
Integrations provider Port
Billing capability
Analytics projection/query dependencies
event consumers
```

---

# 96. CERT-V2-049 — Persistence ownership

Any new durable v2 state must have an explicit semantic owner.

Expected candidates:

```text
Automation Process/Execution state
Analytics Projection state
```

Shared physical DbContext is acceptable.

Semantic ownership must be local.

---

# 97. CERT-V2-050 — No cross-BC ORM navigation

New reference persistence must not introduce cross-BC aggregate navigation or
cascade semantics.

Use stable IDs/ResourceRef.

---

# 98. CERT-V2-051 — Event registry/version completeness

All mandatory reference Integration Events must satisfy existing event
contract/version registry rules.

No unregistered teaching event.

---

# 98A. CERT-V2-051A — Valid requested-fact distinction

Certification must explicitly classify `N8nDispatchRequestedV1`.

PASS when it represents a committed Automation-owned durable dispatch request
and is enrolled consistently with the accepted `AutomationExecution`.

Do not treat its `Requested` suffix as an instruction smell by itself.

This does not excuse the separate
`BoardItemMemberAssignedForAutomationIntegrationEvent` ownership debt.

# 99. CERT-V2-052 — Outbox atomicity

Producer mutation and outward event enrollment must follow current intended
atomic model.

Failed transaction:

```text
must not produce committed outward event
```

---

# 100. CERT-V2-053 — Consumer idempotency

Mandatory event consumers:

```text
Automation
Analytics
Governance if membership event consumer selected
```

must safely handle duplicate delivery.

The exact owner may be:

```text
Platform inbox/dedup
or
consumer semantic idempotency
```

but must be explicit and tested.

---

# 101. CERT-V2-054 — Retry ownership

Required distinction:

```text
technical delivery retry
→ Platform/MassTransit

business workflow retry/progression
→ owning Application/Process
```

For N8n bootstrap reference:

```text
RetryableFailure
→ delivery-owned retry
→ failed delivery transaction rolls back

UnknownOutcome
→ not blindly retried
→ durable reconciliation/terminal outcome
```

Certification fails if the same retry is simultaneously represented as:

```text
durable Automation AttemptCount mutation
and
broker redelivery caused by throwing the same delivery transaction
```

without an independent commit/reconciliation design.
# 102. CERT-V2-055 — Dead-letter/terminal technical failure

If current Platform has DLQ/poison semantics:

```text
reference event path must exercise it
```

If terminal delivery model differs:

```text
certify actual mechanism
```

---

# 103. CERT-V2-056 — Projection idempotency

Same Work event twice must not corrupt Analytics derived state.

---

# 104. CERT-V2-057 — Projection ordering

Out-of-order behavior must be deterministic.

Undefined ordering:

```text
FAIL
```

---

# 105. CERT-V2-058 — Projection rebuild

Mandatory.

Certification requires an executable rebuild path.

A prose-only rebuild plan:

```text
FAIL
```

---

# 106. CERT-V2-059 — Projection query

Analytics reference must include local query/read behavior.

A projection table nobody can read:

```text
incomplete reference
```

---

# 107. CERT-V2-060 — Process state progression

Automation reference must prove:

```text
initial
running
completed
failed
```

or repository-equivalent semantic states.

---

# 108. CERT-V2-061 — Process duplicate outcome

Repeated target outcome must not reopen terminal state or duplicate semantic
completion.

---

# 109. CERT-V2-062 — Process restart/resume

Required when process state is durable.

State must survive service/repository lifecycle reset in integration test.

---

# 110. CERT-V2-063 — Process business failure

Target Work business rejection must become Automation business/process outcome.

It must not be treated as transient Broker failure.

---

# 111. CERT-V2-064 — Process technical failure

Transient adapter/delivery failure must follow technical retry policy.

Process state must remain semantically coherent.

---

# 112. CERT-V2-065 — Billing semantic isolation

Billing Public capability must not absorb Governance permission.

Governance permission must not absorb Billing commercial capability.

If an authorization decision consumes both:

```text
composition preserves source ownership
```

---

# 113. CERT-V2-066 — Governance semantic isolation

Governance answers:

```text
who may perform operation?
```

Identity answers:

```text
who is actor?
```

Workspaces answers:

```text
is actor a workspace member / invitation state?
```

Billing answers:

```text
is capability commercially available?
```

Reference architecture must preserve these distinctions.

---

# 114. CERT-V2-067 — Notification ownership

`Notifications` implementation must be explicitly mapped to Collaboration
semantic ownership or exact compatibility debt.

No hidden new context.

---

# 115. CERT-V2-068 — Provider semantic translation

Provider-native status/type must not become Domain/Application canonical
business vocabulary without translation.

---

# 116. CERT-V2-069 — API policy

Reference implementations do not need public HTTP endpoints.

Certification must not fail for lack of route.

If a route is added:

```text
API must not own cross-BC mutation workflow
```

---

# 117. CERT-V2-070 — Mechanism-specific cross-process readiness

Certification does not use one universal remote-substitution rule.

For each cross-context interaction edge, derive certification state from:

```text
SPEC RemoteReadiness
+ ExtractionBlocker
→ SemanticBoundaryReady
→ RuntimeSubstitutionReady
→ ExtractionBlocked
```

and retain `RemovalTrigger` where a blocker exists.

Certification MUST NOT maintain a second independent readiness taxonomy.

Mechanism-specific PASS interpretation:

## TAC-XC-A — Producer Public Direct

PASS when:

```text
producer owns stable Public meaning
consumer does not access producer private persistence/Domain
direct in-process behavior is correct
```

A consumer Port or remote adapter is **not** mandatory.

Valid current state:

```text
SemanticBoundaryReady = true
RuntimeSubstitutionReady = deferred/semantic-only
```

At future extraction, architecture may choose:

```text
runtime proxy implementing stable producer Public
or
consumer migration to TAC-XC-B
```

## TAC-XC-B — Port + ACL + Adapter

PASS requires:

```text
consumer Application depends on consumer-owned Port
ACL is consumer-owned/pure where needed
Infrastructure adapter is replaceable/policy-thin
consumer business handler/Domain does not depend on transport
```

Conceptual replacement:

```text
InProcessAdapter
→ RemoteAdapter
```

may be recorded. Remote transport implementation is not required.

## TAC-XC-C — Integration Event

PASS requires semantic deployment independence:

```text
producer state + outbox commit
→ Platform delivery
→ consumer-local reaction
```

The event flow cannot depend on direct private-state invocation merely because
both contexts currently share one process.

## TAC-XC-D — Projection

PASS requires:

```text
projection stays consumer-owned/local
producer event/snapshot source remains authoritative
rebuild/recovery semantics remain explicit
```

## TAC-XC-E — Target-owned Action

PASS requires target ownership to remain stable.

Remote command substitution is only certifiable when transaction semantics
permit it.

For:

```text
BOUND-TX-002
BOUND-TX-004
```

required current state is:

```text
SemanticBoundaryReady = true
RuntimeSubstitutionReady = false/deferred
ExtractionBlocked = true
```

Certification FAILS if those flows are labeled remote-ready while current
shared atomicity remains authoritative.

## TAC-XC-F — Process Manager

PASS requires:

```text
workflow owner/process state remain stable
participant transport may change
participant private state remains participant-owned
technical delivery retry remains runtime/Platform-owned
```

Forbidden across all mechanisms:

```text
move producer business rule into consumer
make transport DTO part of consumer Domain/Application semantics
claim service-readiness only because an interface exists
invent fake remote service solely to satisfy certification
```

---

# 118. CERT-V2-071 — No service extraction authorization

This execution explicitly records:

```text
SERVICE EXTRACTION AUTHORIZED?
NO
```

Reference implementation is for semantic/runtime substitution seams.

Not microservice commitment.

---

# 119. CERT-V2-072 — Durable guidance

Stable v2 rules must be promoted into existing durable architecture guidance.

At minimum:

```text
team reference locations
Public vs Port
target mutation
event/projection/process
provider Port/adapter
Flow Card Interactions[] / FlowId:EdgeId traceability
mechanism-specific cross-process readiness
transaction/extraction STOP
Broker/Platform separation
```

Do not create a second permanent architecture corpus.

---

# 120. CERT-V2-073 — Team discoverability

A developer should be able to locate examples from:

```text
source topology
test names
existing team/architecture docs
```

without reading the execution history.

No `Example` production namespace required.

---

# 121. CERT-V2-074 — Same-team BC separation

Must explicitly certify:

```text
Identity ≠ Accounts
Workspaces ≠ Governance
Documents ≠ Collaboration
Automation ≠ Integrations
```

even if maintained by the same team.

---

# 122. CERT-V2-075 — Team copy-model

Each team pack must document or make discoverable:

```text
which source to copy structurally
which semantic ownership not to copy blindly
which tests to mirror
which STOP conditions apply
```

The copy-model belongs in durable guidance or existing team instructions.

---

# 122A. End-to-end traceability matrix

Certification must maintain:

```text
SPEC Flow / Interaction edge
→ PLAN milestone / pack-entry checkpoint
→ TEST family
→ freeze item
→ certification row
```

For cross-BC edges:

```text
FlowId:EdgeId
→ TAC-XC mechanism
→ current DecisionStatus
→ substantive TESTS evidence
```

At minimum:

| Reference | Milestone | Tests | Freeze | Cert |
|---|---|---|---|---|
| IA-REF-001 | M4A | TAC-IA-001/002 | FRZ-005/009 | V2-011 |
| IA-REF-002 | M4B | TAC-IA-003/004 | FRZ-005/009 | V2-011 |
| IA-REF-003 | M2/M4C | TAC-IA-005/006 + TX | FRZ-001/009 | V2-011/012 |
| WG-REF-001 | M2/M5A | TAC-WG-001/002 | FRZ-001/009 | V2-013 |
| WG-REF-002 | M5B | TAC-WG-003..005 | FRZ-013 | V2-013 |
| WG-REF-003 | M5C | TAC-WG-006..009 | FRZ-007/009 | V2-013 |
| WM-REF-001 | M6A | TAC-WM-001 | FRZ-009 | V2-014 |
| WM-REF-002 | M6B | TAC-WM-002..004 | FRZ-009 | V2-014 |
| WM-REF-003 | M6C/M6D | TAC-WM-005/006 | FRZ-010 | V2-014 |
| WM-REF-004 | M6E | TAC-WM-007..009 | FRZ-007 | V2-014 |
| DC-REF-001 | M7B | TAC-DC-001..003 | FRZ-014 | V2-015 |
| DC-REF-002 | M7A | TAC-DC-003 | FRZ-014 | V2-015 |
| DC-REF-003 | M7C | TAC-DC-004..007 | FRZ-014 | V2-015 |
| DC-REF-004 | M7D | TAC-DC-008 | FRZ-009 | V2-015 |
| AI-REF-001 | M6E/M8A | TAC-AI-001/002 | FRZ-007/010 | V2-016 |
| AI-REF-002 | M8D-M8G | TAC-AI-009..013 | FRZ-010 | V2-016 |
| AI-REF-003 | M8B/M8C | TAC-AI-003..008 | FRZ-010 | V2-016 |
| AI-REF-004 | M8H/M8I | TAC-AI-014..025 | FRZ-006/015 | V2-016 |
| BI-REF-001 | M9B | TAC-BI-001..005 | FRZ-008/012 | V2-017 |
| BI-REF-002 | M9C | TAC-BI-006..008 | FRZ-012 | V2-017 |
| BI-REF-003 | M9D | TAC-BI-009/010 | FRZ-012 | V2-017 |
| AR-REF-001 | M10 | TAC-AR-001..013 | FRZ-011 | V2-018 |

Missing mapping is `BLOCKED-EVIDENCE`.

# 122AA. Exact 47-flow certification matrix

Every Flow ID is explicit; ranges are not sufficient evidence.

| FlowId | Required final state |
|---|---|
| `IA-FLOW-01` | VERIFIED |
| `IA-FLOW-02` | VERIFIED |
| `IA-FLOW-03` | VERIFIED |
| `IA-FLOW-04` | VERIFIED |
| `IA-FLOW-05` | VERIFIED |
| `IA-FLOW-06` | VERIFIED |
| `WG-FLOW-01` | VERIFIED |
| `WG-FLOW-02` | VERIFIED |
| `WG-FLOW-03` | VERIFIED |
| `WG-FLOW-04` | VERIFIED — M5 Wave 5 freeze: grant/revoke/get mutations through the one canonical pipeline — owner allow, non-manager deny, grant ceiling max(requested,existing), semantic upsert, active-subject DB race 23505, revoke authority>=target-rank ceiling + soft-delete + audit, subject self-revoke deny, foreign-account revoke hidden NotFound with foreign row intact (GovernanceResourcePermissionFlowTests 41D/41D2, real PostgreSQL) |
| `WG-FLOW-05` | VERIFIED — M5 Wave 5: page ACL read through the same management ladder — owner/Manager allow, ordinary Member deny, Governance-owned state, workspace-scoped, no foreign-resource rows (41E) |
| `WG-FLOW-06` | VERIFIED — M5 Wave 5: authorization-before-effect routing (41F) plus one canonical evaluator pinned by ADR-007 locks (EvaluatorSeam_HasExactlyOneImplementation, Common→Governance import lock, seam-only pipeline stage) and HandlerAuthorizationBypass/PipelineFreeze gates |
| `WM-FLOW-01` | VERIFIED |
| `WM-FLOW-02` | VERIFIED |
| `WM-FLOW-03` | VERIFIED |
| `WM-FLOW-04` | VERIFIED |
| `WM-FLOW-05` | VERIFIED-SEMANTICS at M6 freeze (`ad279ac4`) — semantic interaction + producer authority were frozen at M6; final IMPLEMENTATION OWNERSHIP was normalized at M10 (`M10-PORT-OWNERSHIP-NORMALIZATION`, evidence `b341d4ae`) → CLOSED-FROZEN (M6 Wave 6 superseding correction; see STATUS §M6 correction record — this row deliberately no longer claims implementation-ownership completeness at the M6 SHA) |
| `DC-FLOW-01` | VERIFIED |
| `DC-FLOW-02` | VERIFIED |
| `DC-FLOW-03` | VERIFIED |
| `DC-FLOW-04` | VERIFIED |
| `DC-FLOW-05` | VERIFIED |
| `DC-FLOW-06` | VERIFIED |
| `DC-FLOW-07` | VERIFIED |
| `AI-FLOW-01` | VERIFIED |
| `AI-FLOW-02` | VERIFIED |
| `AI-FLOW-03` | VERIFIED |
| `AI-FLOW-04` | VERIFIED |
| `AI-FLOW-05` | VERIFIED |
| `AI-FLOW-06` | VERIFIED |
| `AI-FLOW-07` | VERIFIED |
| `BI-FLOW-01` | VERIFIED — resolver precedence/Used corrected; Used = ledger sum in every state (wave closure) |
| `BI-FLOW-02` | VERIFIED — finite grant is the ceiling regardless of availability (release never zeroes it); null-limit unavailable fails closed; disabled rule still occupies its slot; direct last-slot/first-use race on real PostgreSQL (wave closure) |
| `BI-FLOW-03` | VERIFIED — concurrent CreateAutomationRule → capacity, deterministic id conflict, atomic rollback (wave closure) |
| `BI-FLOW-04` | VERIFIED — capability after real capacity mutation through production pipeline (wave closure) |
| `BI-FLOW-05` | NOT-REQUIRED / NOT-APPLICABLE-AT-CANDIDATE (BI-REF-003) |
| `AR-FLOW-01` | REOPENED — producer-revision ordering implementation is present and focused proofs are being re-established; the prior single producer-timestamp watermark claim and exact-head CI evidence are superseded pending Wave H certification |
| `AR-FLOW-02` | VERIFIED — production GetWorkspacePlacementsQuery executed in tests; local-only scope |
| `AR-FLOW-03` | VERIFIED — watermark-guarded reconcile; producer-contract substitution proof |
| `AR-FLOW-04` | VERIFIED — xmin row-version rejects stale rebuild writes; snapshot revalidation before delete; crash-before-commit → atomic claim rollback → Platform retry → convergence; source untouched (exact-head CI b341d4ae) |
| `PF-FLOW-01` | VERIFIED |
| `PF-FLOW-02` | VERIFIED |
| `PF-FLOW-03` | VERIFIED |
| `PF-FLOW-04` | VERIFIED |
| `PF-FLOW-05` | VERIFIED |
| `PF-FLOW-06` | VERIFIED |
| `PF-FLOW-07` | VERIFIED |

For every executable ID:

```text
FlowId
→ concrete TESTS v2.6 evidence section
→ production source
→ runtime-owner proof where required
→ architecture gate
→ exact candidate SHA
```

For every actual cross-BC interaction:

```text
FlowId:EdgeId
→ TAC-XC-A..F
→ DecisionStatus
→ mechanism-specific production/runtime proof
```

Required M4 edge rows:

```text
IA-FLOW-02:E1 → TAC-XC-E → BOUND-TX-004 proof
IA-FLOW-03:E1 → TAC-XC-C → outbox/Platform/dedup proof
IA-FLOW-04:E1 → TAC-XC-A → producer Public Direct proof
IA-FLOW-05:E1 → TAC-XC-E → BOUND-TX-002 proof
```

Missing ID, edge classification, evidence resolution or final state = FAIL.
# 122B. Milestone certification coverage

Every PLAN milestone must have an evaluated final state.

The non-milestone PRE-M4 checkpoint must separately be `PASS` before TAC-M4
implementation evidence can be accepted:

```text
global 47-flow interaction classification
+
IA-FLOW-01..06 deep interaction authority
```

| Milestone | Certification evidence |
|---|---|
| TAC-M0 | exact candidate/source inventory |
| TAC-M1 | unified reference graph |
| TAC-M2 | transaction decision |
| TAC-M3 | gate hardening |
| TAC-M4 | IA pack |
| TAC-M5 | WG pack |
| TAC-M6 | WM pack |
| TAC-M7 | DC pack |
| TAC-M8 | AI pack |
| TAC-M9 | BI pack |
| TAC-M10 | AR pack |
| TAC-M11 | PF pack |
| TAC-M12 | cross-pack integration |
| TAC-M13 | durable team-copy guidance + cleanup |
| TAC-M14 | exact-SHA final run |

Final `ARCHITECTURE-CLOSED` requires every row to be `PASS/FROZEN`; a row cannot
be silently omitted.

# 123. Team certification matrix

## Identity & Accounts

Required:

```text
Identity Public fact
Accounts Public fact
Accounts target action
same-team separation
transaction model
```

## Workspace & Governance

Required:

```text
AcceptInvitation
Governance authoritative contract
membership event
pipeline-vs-explicit authorization
```

## Work Management

Required:

```text
CreateBoard local slice
Collaboration Port
Work Public action
Work Integration Event
```

## Documents & Collaboration

Required:

```text
ResourceRef
Collaboration local mutation
target boundary
Notifications classification
```

## Automation & Integrations

Required:

```text
Work event consumer
Automation execution/process
Work target Port+adapter
background actor/security
provider Port+adapter
N8n behavior
```

## Billing & Entitlements

Required:

```text
Billing Public capability
one consumer
no private plan branch
Common debt shrink
provider boundary
```

## Analytics & Reporting

Required:

```text
Work event consumer
local Projection
query
idempotency/order
rebuild
```

## Platform & Foundation

Required:

```text
outbox
delivery
dedup
retry
terminal technical failure
Broker confinement
business neutrality
```

---

# 124. Team pack final state table

| Pack | Required state | Actual | Result |
|---|---|---|---|
| TAC-RAP-IA | FROZEN | | |
| TAC-RAP-WG | FROZEN | | |
| TAC-RAP-WM | FROZEN | | |
| TAC-RAP-DC | FROZEN | | |
| TAC-RAP-AI | FROZEN | | |
| TAC-RAP-BI | FROZEN | | |
| TAC-RAP-AR | FROZEN | FROZEN — M10 @ `b341d4ae`, merged `25dbb34b` | PASS |
| TAC-RAP-PF | FROZEN | | |

Any actual state other than `FROZEN` blocks `ARCHITECTURE-CLOSED`.

---

# 125. Freeze registry v2.6

Certification requires final state for:

```text
TAC-FRZ-001
TAC-FRZ-002
TAC-FRZ-003
TAC-FRZ-004
TAC-FRZ-005
TAC-FRZ-006
TAC-FRZ-007
TAC-FRZ-008
TAC-FRZ-009
TAC-FRZ-010
TAC-FRZ-011
TAC-FRZ-012
TAC-FRZ-013
TAC-FRZ-014
TAC-FRZ-015
TAC-FRZ-016
TAC-FRZ-017
TAC-FRZ-018
TAC-FRZ-019
TAC-FRZ-020
```

All mandatory freezes must be `FROZEN` on the exact candidate SHA.

A freeze cannot be inferred from another freeze; each has explicit evidence.
# 126. TAC-FRZ-009 certification

All eight team packs must be FROZEN.

No defer.

---

# 127. TAC-FRZ-010 certification

Automation target mutation + Process must be fully implemented.

Required:

```text
event consumer
execution state
Process
Work Port
Work adapter
Work Public action
business failure
technical failure
security
idempotency
```

---

# 128. TAC-FRZ-011 certification

Analytics Projection must be implemented.

Required:

```text
event consumer
projection state
query
idempotency
ordering
scope
rebuild
failure
```

If current `ReportingSnapshot` is used by AR-REF-001, certification must also
close its existing architecture classification:

```text
ProjectionClassificationAllowlist
→ ReportingSnapshot
→ LegacyGap
```

Allowed closure:

```text
migrated to projection role/storage
or
intentional user-managed Domain lifecycle proven and classification updated
```

Leaving the same unexplained `LegacyGap` while declaring the Analytics
reference FROZEN fails certification.
# 129. TAC-FRZ-012 certification

Billing entitlement/capability reference must be implemented.

Required:

```text
Public capability
producer behavior
consumer
private tier absence
Common debt no-growth
```

---

# 130. TAC-FRZ-013 certification

Governance authorization reference must be implemented.

---

# 131. TAC-FRZ-014 certification

Collaboration ResourceRef reference must be implemented.

---

# 132. TAC-FRZ-015 certification

Integrations provider Port+adapter reference must be implemented.

---

# 132A. TAC-FRZ-016 certification

PASS only with 47-ID catalog and 46 executable flows individually VERIFIED.

# 132B. TAC-FRZ-017 certification

PASS only when Common signature dependency gate + IA/WG runtime paths prove no
role semantic leakage.

# 132C. TAC-FRZ-018 certification

PASS only when contract gate + PF-FLOW-07 runtime + compatibility/replay prove
tenant envelope.

# 132D. TAC-FRZ-019 certification

PASS only when Calendar connect/disconnect/webhook structural + persistence +
runtime proofs all pass.

# 132E. TAC-FRZ-020 certification

PASS only when BILL-LIMIT-001, capacity protocol and both last-slot concurrency
proofs pass.

```text
STATE at M9 closure wave start: NOT FROZEN
  zero-limit/unlimited explicit        -> resolved (BILL-LIMIT-001)
  AUTOMATION_RULE capacity class       -> resolved (existence capacity)
  effective-resolution precedence      -> corrected in closure wave
  Used authority (ledger sum always)   -> corrected in closure wave
  fail-closed unavailable owner action -> fixed in closure wave
  direct BI-FLOW-02 last-slot race     -> closed (wave closure, real PostgreSQL)
  first-use bootstrap concurrency      -> closed (wave closure, real PostgreSQL)
  duplicate/conflicting global op id   -> closed (wave closure, global unique index +
                                         SamePayload conflict test)
  limit-change reconciliation          -> closed (wave closure, ReconfigureLimits +
                                         reconcile-on-every-op tests)
FROZEN is recorded only after the closure-evidence section in the status doc
lists the passing gate outputs for every criterion above.

STATE now: FROZEN at closure candidate SHA 3bff55b0 (PR #132). Exact-head
Backend CI run #34702604992 (shipping this SHA) is green: Format check, Build,
Platform messaging tests, Architecture tests, Integration and provider tests,
Domain/Application/Infrastructure tests, API tests and OpenAPI contract, Backend
CI gate all success. Container CI (Backend image validation + Container CI gate)
is red at this SHA for a non-M9 reason: aspnet 9.0.19-bookworm-slim base image
Debian package libpcre2-8-0 (2 HIGH, Trivy), remediation tracked as Part B image
re-pin in a separate change, not part of TAC-FRZ-020. The closure-evidence
section in the status doc lists the passing gate outputs for every criterion
above.
```

# 133. Exact debt record

Every remaining debt:

```text
DebtId
Rule
ExactTypeOrPath
SemanticOwner
CurrentConsumer
WhyRetained
Risk
NoGrowthGate
MigrationTrigger
RemovalTrigger
Tests
AcceptedAsPrecedent: NO
```

No wildcard.

---

# 134. Transaction exception record

If M2 chooses shared transaction:

```text
ExceptionId: BOUND-TX-002
Rule
Contexts
WorkflowOwner
BusinessInvariant
ExactUseCase
TransactionMechanism
RollbackSemantics
SecurityProjectionSemantics
WhySeparateCommitUnsafe
ExtractionImpact
ReviewTrigger
RemovalTrigger
ReplacementDirection
Tests
AppliesToOtherUseCases: NO
```

---

# 135. Reference manifest record

Each mandatory reference:

```text
ReferenceId
Pack
SemanticOwner
Producer
Consumer
Mechanism
ProductionType
ProductionPath
RuntimeAdapter
BehaviorTest
IntegrationTest
DIProof
ArchitectureGate
CandidateSHA
Status
```

Required status:

```text
PASS
```

---

# 136. Architecture gate evidence

Required final rows:

| Gate | Result | SHA |
|---|---|---|
| ARCH-BC-001 | | |
| ARCH-BC-002 | | |
| ARCH-BC-003 | | |
| ARCH-BC-004 | | |
| ARCH-BC-005 | | |
| ARCH-BC-006 | | |
| ARCH-BC-007 | | |
| ARCH-BC-008 | | |
| STN-ARCH-001 | | |
| STN-ARCH-002 | | |
| STN-ARCH-005 | | |
| STN-ARCH-006 | | |
| STN-ARCH-007 | | |
| STN-ARCH-008 | | |

Add exact upstream applicable gates discovered at candidate SHA.

---

# 137. Gate self-test evidence

Mandatory:

```text
own Public PASS
foreign Public FAIL
approved Common PASS
unapproved Common FAIL
Domain in Public FAIL
transport/provider in Public FAIL
new Services file FAIL
stale baseline deletion FAIL
deleted Services file regrowth FAIL
provider SDK in Application FAIL
```

---

# 138. Behavior evidence

Required families:

```text
Identity facts
Accounts facts
Accounts target action
AcceptInvitation
Governance authorization
CreateBoard
WorkManagement Collaboration read
Work Public action
Collaboration ResourceRef mutation
Automation event/process/action
Integrations provider flow
N8n parsing
Billing capability
Billing consumer
Analytics projection/query/rebuild
```

---

# 139. Integration evidence

Required:

```text
Work mutation → outbox
Work event → Automation
Automation → Work target action
Work event → Analytics
Identity/Accounts/Workspaces invitation chain
Workspace membership → Governance reference
Billing → consumer
ResourceRef → Collaboration
Integrations provider adapter
```

---

# 140. DI evidence

Required:

```text
all mandatory Public providers/actions
all mandatory Ports
all mandatory adapters
event consumers
Automation Process dependencies
Analytics Projection/query dependencies
provider adapter
```

---

# 141. Migration evidence

Required where reference source changes persistence/topology:

```text
schema migration
old seam removal
old namespace removal
baseline shrink
Common debt shrink
DI replacement
```

No stale compatibility allowance.

---

# 140A. Exact BC-local evidence

Required local owner baseline mapping:

```text
Identity              → IA-FLOW-01
Accounts              → IA-FLOW-02 and/or IA-FLOW-05 owner implementation
Workspaces            → WG-FLOW-01
Governance            → WG-FLOW-04 + WG-FLOW-05
Work Management       → WM-FLOW-01
Documents             → DC-FLOW-01 + DC-FLOW-02
Collaboration         → DC-FLOW-03/04
Automation            → AI-FLOW-01
Integrations          → AI-FLOW-05/06/07
Billing               → BI-FLOW-02
Analytics / Reporting → AR-FLOW-01/02
Platform / Foundation → PF-FLOW-01..06
```

Any missing local baseline:

```text
TAC-FRZ-016 = FAIL
```

# 141A. Runtime-owner evidence

Mandatory exact-SHA behavior groups include:

```text
AccountsRenameAdminAuthorizationConcurrency
CommonSignaturePurity

RegisterAccountsSharedTransaction
RegistrationEventTenantRestoration

GovernanceBoardPermissionMutation
GovernancePagePermissionMutation

WorkMoveTargetScopeAuthIdempotency
WorkScopedEventTenantRestoration
WorkProjectionSourceOwnership

DocumentsCreatePage
DocumentsArchivePage
PageScopedEventTenantRestoration
CommentBoardItemVariant
CommentPageVariant
CommentActivityTenantProjection
MentionActorExactness
MentionNotificationTenantExactness

AutomationTriggerExecution
AutomationMoveFromProductionExecutor
N8nRetryThroughDedupTransaction

CalendarConnectionSecretVersionBindingRoundTrip
CalendarDuplicateCallbackIdentity
CalendarDisconnectBindingFirst
CalendarConnectionPolicyCALCONN001
CalendarWebhookSignatureReplayTenant
CalendarInboundReceiptLayer

BillingLimitSemanticDecision
BillingCapacityLastSlotRace
AutomationRuleLastSlotRace
BillingCapacityCompensation
BillingCapabilityAfterCapacity

AnalyticsLiveProjection
AnalyticsLocalQuery
AnalyticsProducerBackedRebuild
AnalyticsDriftRecovery

RequestPipelineRuntime
DomainEventOutboxAtomicity
ScopedEventTenantEnvelope
TenantDedupConsumerRuntime
DeliveryRetryOwnership
EventVersionCompatibilityReplay
BackgroundActorAuthorization
```

Names may differ, but each behavior maps to an existing substantive TESTS v2.6
section.

Lower-level direct tests cannot replace production owner-layer evidence.
# 142. Exact source scans

Required final candidate checks:

```text
feature .gitkeep
marker-only canonical role folders
legacy Infrastructure/Data/ReadPorts
Infrastructure/Services exact baseline
old seam tokens
foreign Abstractions imports
foreign Domain imports
foreign DbContext imports
Public→foreign Public
Public→unapproved Common
Application Broker/provider dependencies
Analytics→Work persistence
Automation→Work persistence
consumer Billing private plan dependencies
cross-BC ORM nav
Example/Sample/Demo production namespaces
NotImplemented mandatory reference
```

Prefer semantic analysis where possible.

---

# 143. Security evidence

Required affected flows:

```text
AcceptInvitation
Governance authorization
Automation background action
Collaboration protected target
Billing capability decision if used in access composition
```

Security evidence must distinguish:

```text
Identity
membership
commercial capability
Governance permission
resource state
```

---

# 144. Idempotency evidence

Required:

```text
Accounts target mutation
Automation source event
Automation target action where replayable
Automation Process outcome
Analytics Projection event
Platform delivery
provider operation where replayable
```

---

# 145. Concurrency evidence

Required where selected real Work/Accounts mutation already has concurrency
semantics.

Reference flows must preserve them.

Do not introduce unrelated global concurrency redesign.

---

# 146. Failure-semantics evidence

Each reference must classify applicable failures:

```text
validation
not found
authorization
business rejection
conflict/concurrency
technical transient
configuration
unknown outcome
```

Do not collapse all failures into `false`.

---

# 147. Cross-pack semantic ownership proof

Required statements:

```text
WorkManagement owns Work mutation
Automation owns execution/process
Analytics owns derived projection
Platform owns delivery mechanism
Billing owns capability
Governance owns permission
Workspaces owns membership
Identity owns principal lifecycle
Collaboration owns comments/activity
Integrations owns provider connection/sync semantics
```

---

# 148. Cross-pack runtime substitution proof

For each consumer Port reference:

```text
replace in-process adapter
```

must not require changing consumer business semantics.

Record conceptual future replacement:

```text
InProcessAdapter
→ RemoteAdapter
```

No need to implement remote transport now.

---

# 149. CI requirement

Final candidate must pass repository-canonical backend CI on exact SHA.

At minimum:

```text
build
Architecture.Tests
affected Application.Tests
Infrastructure.Tests
Platform.Tests
Integration.Tests
```

Use stronger actual CI commands.

---

# 150. Stale CI prohibition

A green run from audit baseline:

```text
c409...
```

does not certify a later implementation candidate.

Same SHA required.

---

# 150A. Autonomous-execution closure gate

FAIL certification if any mandatory reference still tells the coding agent to:

```text
choose a feature
select a mutation
select a consumer
choose A/B/C
choose a metric
choose a rebuild strategy
choose a provider
```

Allowed discovery only verifies pinned source or reports `SOURCE-DRIFT`.

Expected execution:

```text
read candidate source
→ resolve pinned path
→ implement prescribed target
→ run prescribed tests
→ certify
```

not:

```text
read architecture ideas
→ invent concrete implementation
```

# 151. BLOCKED-DECISION conditions

Return `BLOCKED-DECISION` when any required semantic authority is unresolved,
including:

```text
BILL-LIMIT-001
CAL-CONN-001
Mention actor authority
Page permission semantic
Billing capacity consistency choice
shared transaction decision requiring architecture/product approval
current-pack TAC-XC mechanism/owner ambiguity
current-pack `DEFERRED-{MILESTONE}` decision not closed
sync-vs-event-vs-process choice not frozen
```

Do not continue by choosing a default.
# 152. BLOCKED-IMPLEMENTATION conditions

Examples:

```text
Automation Process not implemented
Analytics Projection not implemented
Billing Public capability exists but no consumer
Governance contract exists but no executable path
provider Port exists but no Infrastructure adapter
ResourceRef type exists but no Collaboration mutation uses it
Work event exists but no consumer
```

This is the key v2 outcome for missing source.

---

# 153. BLOCKED-REGRESSION conditions

Examples:

```text
N8n parsing changed
AcceptInvitation behavior regressed
Work event duplicates side effects
Analytics double counts duplicate event
Automation target business failure gets infinite technical retry
Billing consumer leaks PlanTier
```

---

# 154. BLOCKED-EVIDENCE conditions

Return `BLOCKED-EVIDENCE` when implementation may exist but certification proof
is insufficient, including:

```text
FlowId evidence label does not resolve
runtime test bypasses owner transaction/filter/concurrency
secret only verified in memory
event tenant not tested through TenantContextConsumeFilter
hard quota not tested concurrently
stale revision body conflicts with v2.6
candidate SHA and CI/evidence SHA differ
cross-BC FlowId has no EdgeId/mechanism evidence
FlowId:EdgeId TESTS evidence does not resolve
claimed RuntimeSubstitutionReady contradicts ExtractionBlocker
```

Missing evidence is not equivalent to PASS.
# 155. Architecture closure checklist — semantic

```text
[ ] 11 BCs mapped
[ ] same-team BCs remain separate
[ ] every real/reference edge has owner
[ ] every actual cross-BC edge has FlowId:EdgeId
[ ] every cross-BC edge mechanism is TAC-XC-A..F
[ ] multi-edge Flow Cards classify edges independently
[ ] supporting shapes are not seventh mechanisms
[ ] every mutation has one authority
[ ] workflow owner explicit
[ ] transaction owner explicit
[ ] six mechanisms only
[ ] pipeline-first preserved
[ ] Public producer-owned
[ ] Port consumer-owned
[ ] ACL pure
[ ] Adapter business-policy-free
[ ] TAC-XC-A does not require artificial consumer Port
[ ] TAC-XC-B adapter readiness proven where selected
[ ] extraction-blocked TAC-XC-E is not falsely remote-certified
[ ] Projection derived
[ ] Process workflow-owned
[ ] Platform mechanism-only
[ ] API not mutation orchestrator
```

---

# 156. Architecture closure checklist — positive references

```text
[ ] local vertical slice exists
[ ] direct Public read exists
[ ] producer fact exists
[ ] consumer Port read exists
[ ] composite read exists
[ ] ACL exists
[ ] target mutation exists
[ ] Integration Event exists
[ ] outbox/delivery exists
[ ] event consumer exists
[ ] Projection exists
[ ] Projection rebuild exists
[ ] Process Manager exists
[ ] Process→target mutation exists
[ ] ResourceRef exists
[ ] Billing capability exists
[ ] Billing consumer exists
[ ] Governance authoritative contract exists
[ ] provider Port exists
[ ] provider adapter exists
[ ] background actor model exists
[ ] transaction reference exists
```

No mandatory defer.

---

# 157. Architecture closure checklist — negative enforcement

```text
[ ] foreign DbContext rejected
[ ] foreign Domain rejected
[ ] producer private Application rejected
[ ] cross-BC ORM nav rejected
[ ] broad Common Public dependency rejected
[ ] foreign Public dependency rejected
[ ] transport/provider Application dependency rejected
[ ] Integration Event ownership enforced
[ ] Common semantic growth rejected
[ ] legacy Services regrowth rejected
[ ] old seam regrowth rejected
[ ] empty canonical folder rejected
[ ] fake Example namespace absent
```

---

# 158. Architecture closure checklist — IA

```text
[ ] Identity Public fact
[ ] Identity semantics
[ ] Accounts Public fact
[ ] Accounts semantics
[ ] Accounts target action
[ ] transaction decision
[ ] same-team boundary
[ ] DI
[ ] tests
```

---

# 159. Architecture closure checklist — WG

```text
[ ] AcceptInvitation
[ ] Governance authoritative contract/path
[ ] Governance behavior
[ ] pipeline distinction
[ ] Workspace membership event
[ ] reference consumer
[ ] DI
[ ] tests
```

---

# 160. Architecture closure checklist — WM

```text
[ ] CreateBoard
[ ] Collaboration Port
[ ] Collaboration adapter
[ ] Work Public target action
[ ] Work event V1
[ ] Domain→Integration mapping
[ ] outbox
[ ] tests
```

---

# 161. Architecture closure checklist — DC

```text
[ ] ResourceRef
[ ] Collaboration local mutation
[ ] target existence/access boundary
[ ] no foreign navigation
[ ] ACL or global ACL reference
[ ] Notifications classified
[ ] tests
```

---

# 162. Architecture closure checklist — AI

```text
[ ] Work event consumer
[ ] Automation execution state
[ ] Process Manager
[ ] Work Port
[ ] Work adapter
[ ] business failure
[ ] technical retry distinction
[ ] background actor/security
[ ] provider Port
[ ] provider adapter
[ ] N8n behavior
[ ] DI
[ ] tests
```

---

# 163. Architecture closure checklist — BI

```text
[ ] Billing Public capability
[ ] producer semantics
[ ] one consumer
[ ] allowed/denied
[ ] no private plan branch
[ ] Common debt shrink
[ ] provider boundary
[ ] tests
```

---

# 164. Architecture closure checklist — AR

```text
[ ] Work event consumer
[ ] Analytics projection state
[ ] local query
[ ] duplicate idempotency
[ ] ordering/revision
[ ] scope
[ ] rebuild
[ ] failure recovery
[ ] source truth separation
[ ] tests
```

---

# 165. Architecture closure checklist — PF

```text
[ ] outbox
[ ] message identity
[ ] delivery
[ ] dedup/inbox or consumer idempotency
[ ] retry
[ ] terminal technical failure
[ ] Broker confinement
[ ] Platform business neutrality
[ ] tests
```

---

# 166. Final pack state rule

Before final outcome:

```text
all 8 packs
must equal
FROZEN
```

Anything else blocks closure.

---

# 167. Final freeze state rule

Before final outcome:

```text
TAC-FRZ-001..007 = FROZEN
TAC-FRZ-008 = FROZEN or exact accepted debt
TAC-FRZ-009..015 = FROZEN
```

---

# 168. Final reference count rule

Required catalog:

```text
47 Flow IDs
46 executable VERIFIED
BI-FLOW-05 conditional classification
```

Count-only validation is forbidden.

Every executable ID needs existing substantive evidence.
# 169. Final no-overengineering rule

Certification fails if closure was achieved by building:

```text
fake HTTP between BCs
per-BC projects
per-BC databases
fake microservices
generic SharedContracts
generic Orchestration service
duplicate event buses
```

without operational need.

---

# 170. Final architecture teaching rule

A new developer should be able to answer from source:

```text
Where is the canonical local command?
Where is a direct Public read?
Where is a Consumer Port?
Where is an adapter?
Where is target mutation?
Where is an event?
Where is a Process?
Where is a Projection?
Where is ResourceRef?
Where is Billing capability?
Where is Governance authorization?
Where is provider integration?
Where is Platform delivery?
```

If a mandatory answer is:

```text
only in docs
```

v2 is not complete.

---

# 171. Final exact-SHA evidence table

| Evidence | Result | SHA |
|---|---|---|
| build | | |
| Architecture.Tests | | |
| gate self-tests | | |
| interaction mechanism/edge authority audit | | |
| PRE-M4 IA interaction authority | | |
| IA pack | | |
| WG pack | | |
| WM pack | | |
| DC pack | | |
| AI pack | | |
| BI pack | | |
| AR pack | | |
| PF pack | | |
| Work→Automation | | |
| Automation→Work | | |
| Work→Analytics | | |
| invitation chain | | |
| membership→Governance | | |
| Billing→consumer | | |
| ResourceRef→Collaboration | | |
| provider chain | | |
| N8n behavior | | |
| backend CI | | |

All mandatory rows require PASS.

---

# 172. Final certification decision rule

`ARCHITECTURE-CLOSED` requires all:

```text
exact candidate SHA
11/11 business BC local baselines
Platform/Foundation baseline
8 Team Operating Packs FROZEN

47 Flow IDs present
46 executable flows VERIFIED
BI-FLOW-05 correctly classified

all Flow Cards have FlowKind/SupportingShapes/Interactions[]
every actual cross-BC edge classified TAC-XC-A..F
PRE-M4 interaction checkpoint PASS
all current-pack FlowId:EdgeId evidence resolves
no supporting shape treated as seventh mechanism

TAC-FRZ-001..020 satisfied

BOUND-TX-002 FROZEN + ExtractionBlocked=true
BOUND-TX-004 FROZEN + ExtractionBlocked=true
neither BOUND-TX-002 nor BOUND-TX-004 falsely certified remote-ready
Billing capacity consistency FROZEN
BILL-LIMIT-001 RESOLVED
CAL-CONN-001 RESOLVED
Mention actor authority RESOLVED
Page permission semantic RESOLVED

Common signature purity PASS
scoped-event tenant envelope PASS
Calendar semantic/persistence/runtime authority PASS
Billing hard-quota concurrency PASS
traceability evidence integrity PASS

Architecture.Tests PASS
behavior tests PASS
integration/runtime tests PASS
DI proof PASS
exact-SHA Backend CI PASS
```

Decision mapping:

```text
missing implementation
→ BLOCKED-IMPLEMENTATION

missing semantic/product authority
→ BLOCKED-DECISION

missing/provisional evidence
→ BLOCKED-EVIDENCE

source moved
→ BLOCKED-SOURCE-DRIFT

everything passes
→ ARCHITECTURE-CLOSED
```
# 173. ARCHITECTURE-CLOSED statement template

```text
Execution:
Backend Team Operating Architecture Bootstrap & Closure v2.6

Repository:
Nqv1208/Notrelix

Implementation SHA:
<sha>

Outcome:
ARCHITECTURE-CLOSED

BC local baselines:
Identity: PASS
Accounts: PASS
Workspaces: PASS
Governance: PASS
Work Management: PASS
Documents: PASS
Collaboration: PASS
Automation: PASS
Integrations: PASS
Billing: PASS
Analytics / Reporting: PASS
Platform / Foundation: PASS

Real Flow Catalog:
47 IDs present
46 executable flows VERIFIED
BI-FLOW-05 correctly classified

Interaction Architecture:
Every actual cross-BC edge classified TAC-XC-A..F: PASS
FlowId:EdgeId evidence integrity: PASS
PRE-M4 IA interaction authority: PASS
No seventh mechanism: PASS
BOUND-TX-002 extraction blocker preserved: PASS
BOUND-TX-004 extraction blocker preserved: PASS
No false remote-readiness certification: PASS

Freeze registry:
TAC-FRZ-001..020: FROZEN

Decisions:
BOUND-TX-002: PASS
BOUND-TX-004: PASS
Billing capacity consistency: PASS
BILL-LIMIT-001: RESOLVED
CAL-CONN-001: RESOLVED
Mention actor authority: RESOLVED
Page permission semantic: RESOLVED

Semantic/runtime closure:
Accounts local admin: PASS
Common signature purity: PASS
Scoped event tenant envelope: PASS
Mention actor exactness: PASS
Calendar Connection/SecretVersion/Binding: PASS
Calendar disconnect policy: PASS
Calendar provider webhook intake: PASS
Billing last-slot concurrency: PASS
AutomationRule last-slot concurrency: PASS
Billing compensation: PASS
Analytics live/local/rebuild: PASS
Platform runtime/replay/security: PASS

Evidence integrity:
Every FlowId resolves to substantive TESTS v2.6 evidence: PASS
Every cross-BC FlowId:EdgeId resolves to mechanism-specific evidence: PASS
No stale normative authority: PASS

Architecture gates: PASS
Behavior: PASS
Integration/runtime: PASS
DI: PASS
Exact-SHA Backend CI: PASS

Service extraction authorization:
NONE
```
# 174. BLOCKED-IMPLEMENTATION statement template

Use when architecture decisions are known but mandatory source is incomplete.

```text
Execution:
Backend Team Architecture Bootstrap & Closure v2

Implementation SHA:
<sha>

Outcome:
BLOCKED-IMPLEMENTATION

Missing mandatory pack/reference:
<id>

Semantic design:
RESOLVED

Production reference:
MISSING/INCOMPLETE

Required remaining:
- source
- DI
- behavior tests
- integration tests
- architecture proof

Architecture closure:
NOT CERTIFIED
```

---

# 175. BLOCKED-DECISION statement template

```text
Execution:
Backend Team Architecture Bootstrap & Closure v2

Implementation SHA:
<sha>

Outcome:
BLOCKED-DECISION

Blocking semantic decision:
<id>

Affected packs:
<ids>

Current proven behavior:
<summary>

Unresolved:
<summary>

Safe independent packs:
<status>

Architecture closure:
NOT CERTIFIED
```

---

# 174A. v2.6 final Team Operating Framework checklist

```text
[ ] 47 Flow IDs present
[ ] 46 executable flows individually VERIFIED
[ ] BI-FLOW-05 correctly classified
[ ] every Flow Card has FlowKind + SupportingShapes + Interactions[]
[ ] every actual cross-BC edge is TAC-XC-A..F
[ ] PRE-M4 interaction authority PASS
[ ] M4 IA interaction-edge matrix matches revised SPEC/PLAN/TESTS
[ ] PRE-M4 Source Operating Pattern checkpoint PASS
[ ] cross-context runtime composition owner is explicit
[ ] known non-reference/deferred sources are not used as copy-models
[ ] canonical copy-model labels resolve only to VERIFIED Real Flows
[ ] no universal Port/gateway/cross-context framework exists
[ ] no supporting shape treated as seventh mechanism
[ ] TAC-FRZ-001..020 frozen

[ ] IA-FLOW-06 Accounts RenameAccount real
[ ] IA-FLOW-02 BOUND-TX-004 extraction blocker preserved
[ ] IA-FLOW-03 producer commit→outbox→Platform→consumer/dedup proven
[ ] IA-FLOW-04 direct Public certified without artificial Port
[ ] IA-FLOW-05 BOUND-TX-002 extraction blocker preserved
[ ] Common signature purity passes
[ ] no canonical Common AccountRole/WorkspaceRole leakage

[ ] scoped-event contract gate passes
[ ] PF-FLOW-07 runtime tenant restoration passes
[ ] changed event schemas have compatibility/replay evidence

[ ] Mention actor authoritative
[ ] MentionedByUserId not copied from MentionedId
[ ] Notification actor/tenant exact
[ ] no Guid.Empty scoped tenant

[ ] ConnectCalendar persists IntegrationConnection
[ ] ConnectCalendar persists IntegrationSecretVersion
[ ] ConnectCalendar persists CalendarIntegration(ConnectionId)
[ ] DB reload proves secret-version/binding authority
[ ] duplicate callback/binding safe
[ ] DisconnectCalendar deactivates CalendarIntegration first
[ ] CAL-CONN-001 resolved/tested
[ ] inbound webhook provider-authenticated
[ ] inbound receipt/dedup layer correct
[ ] outbound WebhookDelivery not reused inbound

[ ] BILL-LIMIT-001 resolved
[ ] zero/unlimited semantics explicit
[ ] Billing capacity last-slot race safe
[ ] concurrent CreateAutomationRule last-slot race safe
[ ] failure releases/compensates capacity
[ ] duplicate/conflicting operations safe
[ ] capacity not mislabeled commercial UsageMetric

[ ] every evidence reference resolves to an existing substantive test section
[ ] no fabricated traceability labels
[ ] no stale revision rule overrides v2.6

[ ] exact candidate SHA captured
[ ] runtime-owner evidence exact-SHA
[ ] exact-SHA Backend CI green
```

Any unchecked mandatory item forbids `ARCHITECTURE-CLOSED`.
# 175A. Carried-forward runtime blocker compatibility

Earlier runtime blockers remain mandatory:

```text
N8n real retry transaction
Automation production MoveItem executor
Work target scope/auth/OperationId
Analytics projection/source ownership
reviewed shared transaction exceptions
```

They are necessary but not sufficient.

v2.6 also requires semantic authority, persistence authority, tenant-envelope
correctness, hard-quota concurrency and evidence integrity.
# 176. Post-certification development rule

After closure:

```text
identify BC
→ identify semantic owner
→ identify persisted authority
→ identify runtime/transaction/concurrency owner
→ classify actual cross-BC edge TAC-XC-A..F
→ choose same-case VERIFIED Flow Card / interaction edge
→ copy invariant structure
→ replace feature-specific semantics
→ run mapped tests/gates
```

No future developer should infer aggregate choice, tenant envelope, actor,
provider intake state or quota meaning from naming convenience.
# 177. Reopening architecture review

Review is reopened only for material changes such as:

```text
new ownership conflict
new long-running multi-owner workflow
new consistency requirement
new security isolation
new operational extraction driver
physical extraction requires reopening an ExtractionBlocker
existing TAC-XC-A..F genuinely insufficient
```

Not for:

```text
folder large
team preference
HTTP feels cleaner
shared DB shortcut
same team owns both BCs
```

---

# 177A. Interaction Architecture certification-authority boundary

Final active authority remains:

```text
SPEC
→ TAC-XC semantics + Flow Card Interactions[]

PLAN
→ execution order + pack-entry decision freeze

TESTS
→ behavior/runtime/negative evidence

CERTIFICATION
→ final closure rules

STATUS
→ current evidence/state only
```

This CERTIFICATION file must not resolve a `DEFERRED-*` product/architecture
decision by itself. It certifies the decision once SPEC/PLAN authority has been
closed.

## PR #158 mechanism-closure re-audit v2 correction

The historical flow rows in this file are retained as historical certification
evidence and are superseded for the current PR #158 candidate by
`notrelix-pr158-mechanism-closure-reaudit-v2.md` at the audited head
`b9cccc07bf4b3bf2be04d7336b478eba6170f136`.

For the current candidate, AI-FLOW-07 is `BLOCKED-DECISION` at the undefined
downstream Calendar semantic target; AI-FLOW-06 is only locally verified up to
the Calendar binding lifecycle while provider cleanup remains open; and the
affected PF-FLOW-05 Work V1→V2 event-evolution/replay slice is reopened. The
current candidate is not `ARCHITECTURE-CLOSED`.

The temporary Interaction Architecture working plan is not a sixth permanent
authority document.

---

# 178. Final meaning of v2.6 closure

v2.6 closure means the backend has an executable Team/BC architecture framework
where a new engineer can answer, from verified production references:

```text
which bounded context owns this?
which model owns the lifecycle?
which state is durably authoritative?
which runtime owns transaction/retry/concurrency?
which tenant and actor facts are authoritative?
which TAC-XC mechanism does each cross-BC edge use?
is the edge semantic-ready, runtime-substitution-ready, or extraction-blocked?
which decision IDs are frozen?
which tests/gates prove the FlowId:EdgeId?
```

Closure does NOT mean merely:

```text
the interfaces exist
the folders look clean
CI is green
the FlowId appears in a table
one mechanism label was attached to a multi-edge Flow
an interface was called "service-ready" without transaction/failure analysis
```

It means the next implementation can proceed without inventing architecture or
product semantics.

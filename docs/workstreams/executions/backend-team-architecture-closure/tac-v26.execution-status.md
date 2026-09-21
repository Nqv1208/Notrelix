---
document_id: EXEC-BACKEND-TEAM-ARCHITECTURE-CLOSURE-V26-EXECUTION-STATUS
title: TAC v2.6 execution status — inventory, decisions, slice evidence
version: 1.0
status: in-progress
spec: backend-team-architecture-closure.spec.md@2.6
plan: backend-team-architecture-closure.plan.md@2.6
tests: backend-team-architecture-closure.tests.md@2.6
certification: backend-team-architecture-closure.certification.md@2.6
authority_revision: interaction-architecture-normalized-source-pattern-public-capability-topology-v2.6-candidate
baseline:
  branch: architecture/backend-boundary-execution
  pr: 113
  audit_sha: b94312b3e10e2212f18440003e775d415aa1b5d7
---


# TAC v2.6 Execution Status (untracked working record)

> **CURRENT-STATE PRECEDENCE**
>
> The section below is the current authoritative state snapshot for this working
> record. Older `Current M3 status`, correction-wave reports, and exact-SHA
> closure snapshots later in this file are retained as historical evidence only
> and do **not** override this section.

## 0. Current authoritative status — interaction-normalized candidate

### 0.1 Evidence layers

Keep repository evidence and local authority-doc work separate.

```text
Repository / PR evidence
  repository: Nqv1208/Notrelix
  PR: #113
  branch: architecture/backend-boundary-execution
  current PR head: 84b04ed968929a8dc7d2ffa07b1e52fb1615970d
  PR state: OPEN
  mergeable: true

Local interaction-normalized authority candidates
  SPEC:
    backend-team-architecture-closure.spec.v2.6-interaction-normalized.md
  PLAN:
    backend-team-architecture-closure.plan.v2.6-interaction-normalized.r2.md
  TESTS:
    backend-team-architecture-closure.tests.v2.6-interaction-normalized.final.md
  CERTIFICATION:
    backend-team-architecture-closure.certification.v2.6-interaction-normalized.md

Local authority candidates:
  intentionally untracked / non-repository execution authority
  NOT implementation exact-SHA certification evidence
```

The local revised docs normalize architecture authority. They do not make a
repository implementation PASS by themselves.

### 0.2 Interaction Architecture normalization state

```text
InteractionNormalizationStatus:
  LOCAL-AUTHORITY-CANDIDATE-COMPLETE

PermanentAuthorityModel:
  SPEC          → TAC-XC-A..F semantics + Flow Card Interactions[]
  PLAN          → execution order + pack-entry decision freeze
  TESTS         → behavior/runtime/negative proof
  CERTIFICATION → final closure rules
  STATUS        → evidence/state only

PRE-M4:
  type: execution checkpoint only
  milestone: NO
  dedicated CI lane: NO
  current state: PASS (executed 2026-09-04)

PRE-M4 authority-consistency result:
  check 1  (47 Flow Cards present + structured)          = PASS
  check 2  (all cross-BC edges use TAC-XC-A..F only)     = PASS
  check 3  (IA-FLOW-01..06 pinned, non-ambiguous)        = PASS
  check 4  (BOUND-TX-002 / BOUND-TX-004 explicit)        = PASS
  check 5  (reference / NON-REFERENCE consistent)        = PASS
  check 6  (no generic framework / fake transport)       = PASS

Due to items 1-6 passing with no blocking contradiction, apply:
  M3 = PASS
  PRE-M4 = PASS
  TAC-M4 = ENTERED
No production source changed during PRE-M4 (authority/docs-only).
```

Local structural audit of the revised SPEC:

```text
47 Flow IDs preserved
47/47 Flow Cards retain the canonical catalog
every Flow Card has FlowKind
every Flow Card has SupportingShapes
every Flow Card has Interactions[]
no seventh TAC-XC mechanism introduced
```

Source Operating Pattern audit at repository head `fc3d98d6ce2c72d9e0fab3c76df0b032891b49eb`:

```text
Local source candidate:
  IA-FLOW-01 / IA-FLOW-06
  copy eligibility: candidate; M4 verification required

TAC-XC-A source candidate:
  Accounts.Public IAccountMembershipFacts
  copy eligibility: candidate; M4 verification required

TAC-XC-B structural reference:
  Automation IWorkActionPort
  → WorkItemActionAdapter
  → WorkManagement.Public IWorkItemActions
  copy eligibility: structural-only until owning M8 proof

TAC-XC-C source candidate:
  IdentityRegistrationCompletedIntegrationEventV1
  → outbox/Platform
  → WorkspaceProvisioningConsumer
  → ProvisionPersonalWorkspaceCommand
  copy eligibility: candidate; M4 verification required

TAC-XC-E:
  Accounts.Public IAccountMembershipActions exists
  IA-FLOW-02 provisioning Public migration still missing
  copy eligibility: not complete for registration provisioning

NON-REFERENCE:
  WorkManagementCollaborationReadAdapter → M6 debt
  IdentityBootstrapReadAdapter → Supporting Composite Read only
  log-only/*StubConsumer* → not Real Flow reaction

M10 CLOSED-FROZEN (TAC-XC-B):
  WorkItemProjectionSourceAdapter → runtime Adapter delegate to WorkManagement
    Application IWorkItemProjectionSource implementation; no Work persistence
    read; projection relocated to Application/Features/Analytics/Projections

CrossContext DI composition:
  current bindings still live partly in PersistenceRegistration
  target: dedicated CrossContextRegistration/AddCrossContextBindings
```

No generic cross-context framework is authorized.

Current M4 interaction authority:

```text
IA-FLOW-01
  FlowKind = Local
  Interactions[] = empty

IA-FLOW-02:E1
  TAC-XC-E Target-owned Action
  Identity → Accounts
  BOUND-TX-004
  SemanticBoundaryReady = true
  RuntimeSubstitutionReady = false/deferred
  ExtractionBlocked = true

IA-FLOW-03:E1
  TAC-XC-C Integration Event
  Identity → Workspaces
  outbox + Platform delivery + tenant/dedup
  eventual consumer reaction

IA-FLOW-04:E1
  TAC-XC-A Producer Public Direct
  Workspaces → Accounts
  SemanticBoundaryReady = true
  RuntimeSubstitutionReady = deferred/semantic-only

IA-FLOW-05:E1
  TAC-XC-E Target-owned Action
  Workspaces → Accounts
  BOUND-TX-002
  SemanticBoundaryReady = true
  RuntimeSubstitutionReady = false/deferred
  ExtractionBlocked = true

IA-FLOW-06
  Accounts local owner mutation
  canonical Account-admin authorization pipeline
```

Later-pack decisions intentionally remain owned by their pack entry:

```text
WM-FLOW-02:E1
  DEFERRED-M6-SOURCE-NORMALIZATION

WM-FLOW-05 / AR-FLOW-03 projection-source interaction
  CLOSED-FROZEN (TAC-XC-B, M10-PORT-OWNERSHIP-NORMALIZATION)
  Analytics Application source Port → Infrastructure delegate adapter →
  WorkManagement.Public IWorkItemProjectionSource → WorkManagement Application
  implementation; direct Analytics → Work persistence FORBIDDEN
```

`DEFERRED-*` is not permission for a coding agent to choose a mechanism.

### 0.3 Current M3 correction state

The older status snapshot that marked `M3 = COMPLETE` at `0fcd99c` is
superseded by the later gate-acceptance audit at `66650ee` and the current head
chain.

Current classification:

```text
M3A / ARCH-BC-005 = PASS
  closure (2026-09-04, structural rework; supersedes the earlier noun-based attempt):
    deterministic repository/mechanism rejection before own-Public allowance
    implemented in PublicSemanticContractArchitectureTests.ClassifyPurity.
    The earlier name-based classifier (MechanismNouns + name.Contains) was
    rejected as not meeting the authority requirement: primary detection must
    be structural, not lexical. Replaced with structural evidence:
      - real-Type surface/signature inspection is the primary own-Public
        detector (IQueryable / DbSet / EF-rooted type / known data-session /
        exact owned DbContext abstraction); generic enumerables and plain
        Add/Update/Save method names are NOT treated as persistence.
      - exact reviewed mechanism identities (ContextDbContextInterface owned
        DbContext abstractions + RequestDataSession) as defense.
      - anonymous fixture IAccountReadWriteSurface (no mechanism words in its
        name) stays logically on own Producer.Public and is rejected by its
        signature — proving rejection is structural, not lexical.
    own Producer.Public semantic fact/action/query surface remains PASS,
    including the enumerable read-query surface IWorkItemProjectionSource.
    self-tests added:
      Gate_Detects_OwnProducer_Public_RepositoryMechanism   (real-Type path)
      Gate_Allows_OwnProducer_Public_SemanticSurfaces       (real-Type path)
      Gate_Allows_OwnProducer_Public_EnumerableQuerySurface (non-persistence)
      (existing Gate_Detects_RepositoryInsidePublicContract still green)

M3B / STN-ARCH-006 = PASS

M3C / ARCH-BC-008 = PASS
  closure (2026-09-04):
    AccessPermissionRule reclassified as an EXACT technical-pipeline
    exception (TechnicalPipelineTypes exact allowlist) — removed from
    KnownVocabularyDebt business-vocabulary debt. IRequirePermission remains
    the sole governed business-vocabulary debt (DEBT-COMMON-001).
    Classification is unambiguous (not business debt).
    self-tests added:
      Gate_TechPipelineTypes_AreExactCommonIdentities
      Gate_AccessPermissionRule_Is_TechnicalNotDebt
      Gate_Detects_NewPermissionVocabulary_NotMaskedAsTechnical

M3D / ARCH-BC-006 = PASS

TAC-GATE-022 = PASS
TAC-GATE-023 = PASS
TAC-GATE-024 = PASS
TAC-GATE-025 = PASS

TAC-M3 = PASS
```

M3A/M3C classifier closures are implemented; verified at exact SHA `84b04ed`
head (Architecture.Tests 584 passed, 0 failed, 0 skipped; backend build 17
projects, 0 errors).

TAC-M3 remains IN PROGRESS: the PRE-M4 source operating-pattern evidence
(47-flow coarse audit + FlowId:EdgeId map + CURRENT/SOURCE-DRIFT/MISSING/DEFERRED
classification + IA-FLOW-01..06 deep-map + reference-eligibility pin) is not yet
produced, and the exact-SHA M3 certification chain is not closed. M3A/M3C audit
findings are closed by the implementation + self-tests; the remaining M3 closure
is the PRE-M4 evidence below.

Commits after `66650ee` include request-marker namespace alignment plus
M4/application/contracts work. The namespace-alignment change previously kept
`AccessPermissionRule` in `KnownVocabularyDebt` and lacked the M3A
repository/mechanism rejection proof; the M3A classifier was reworked to the
structural form recorded in this section.

No M3-specific CI job is required. Closure remains (completed):

```text
gate/source correction
→ acceptance/self-test
→ evidence
→ status update
```

### 0.4 Current M4 implementation state at PR head

Repository evidence after `66650ee` includes:

```text
49773977
  feat(accounts): rename account with ManageAccount permission and fix provisioning transaction

  added:
    Account-scoped RenameAccount API/Application path
    ManageAccount permission path
    registration/personal-account/workspace transaction evidence
    production registration → outbox → Workspace provisioning runtime-chain evidence

fc3d98d6
  chore(contracts): sync OpenAPI and frontend types for account rename
```

Current M4 state is therefore **not `NOT STARTED`**.

Use conservative evidence states until the interaction-normalized
SPEC/PLAN/TESTS/CERT set is committed and exact evidence is remapped:

```text
IA-FLOW-01 = VERIFIED
  Identity UpdateProfile: local-only, same-team boundary
  IIdentityDbContext + ICurrentRequestContext + IDateTimeProvider
  Domain User.UpdateProfile() mutation
  no cross-BC dependencies
  tests: 2 UpdateProfile tests passed (Application.Tests)

IA-FLOW-06 = VERIFIED
  Accounts RenameAccount: local admin owner mutation
  ManageAccount permission pipeline
  Domain Account.Rename() mutation
  concurrency/rollback proof via integration tests
  tests: 6 RenameAccount integration tests passed

IA-FLOW-02 = VERIFIED-BEHAVIOR / SOURCE-RELOCATION-REQUIRED
  Identity Register → Accounts IAccountProvisioningActions
  TAC-XC-E target-owned action seam
  BOUND-TX-004 transaction/runtime evidence
  IAccountProvisioningActions currently evidenced at legacy Accounts/Public/Commands/ (topology normalization required)
  AccountProvisioningService implements IAccountProvisioningActions
  DI registration: DependencyInjection.cs:98
  Identity handlers use producer Public seam contract (no IAccountDbContext injection)
  tests: 3 BOUND-TX-004 evidence tests + 16 CompleteOAuthLogin tests passed

IA-FLOW-03 = VERIFIED
  IdentityRegistrationCompletedIntegrationEventV1 → WorkspaceProvisioningConsumer
  TAC-XC-C integration event chain
  outbox + Platform delivery + tenant/dedup
  tests: 1 runtime chain integration test passed

IA-FLOW-04 = VERIFIED-BEHAVIOR / SOURCE-RELOCATION-REQUIRED
  Accounts IAccountMembershipFacts: TAC-XC-A producer public direct
  Workspaces → Accounts fact read
  SemanticBoundaryReady = true
  tests: 6 membership tests + 3 transaction evidence tests passed

IA-FLOW-05 = VERIFIED-BEHAVIOR / SOURCE-RELOCATION-REQUIRED
  Accounts IAccountMembershipActions: TAC-XC-E target-owned action
  Workspaces → Accounts BOUND-TX-002
  tests: 3 AccountMembershipTransactionEvidence tests passed

TAC-IA-007 = VERIFIED
  Same-team boundary proof added to HandlerDataPortGateTests.cs
  proves: interface ownership, implementation boundary, no cross-team data port
  tests: 1 architecture gate passed

TAC-IA-008 = VERIFIED
  DI resolution proven by integration tests using real DI container
  RegisterCommandHandler + CompleteOAuthLoginCommandHandler both resolve IAccountProvisioningActions

TAC-RAP-IA = NOT FROZEN under current topology authority
CERT-V2-011 = BLOCKED-IMPLEMENTATION until Public relocation + architecture proof
TAC-M4 = IN PROGRESS
```

Do not promote pre-entry implementation candidates to `VERIFIED` without the
substantive TESTS/CERT evidence required by the revised authority.

### 0.4A Public capability-topology authority correction

The M4 behavior/runtime evidence above is retained, but the newly normalized
SPEC makes the current top-level `Accounts/Public/Commands|Queries|Facts` layout
non-canonical for the M4 copy-model. No source relocation is claimed by this
STATUS file.

Required before M4 can be frozen under the current authority:

```text
Accounts/Public/Membership/
  IAccountMembershipFacts
  AccountMembershipAdmissionFact
  IAccountMembershipActions

Accounts/Public/PersonalAccountProvisioning/
  IAccountProvisioningActions
  PersonalAccountProvisioningResult

namespace/usings/DI/tests updated
CanonicalPathArchitectureTests capability-first Public topology proof PASS
exact legacy Accounts/Public technical-bucket baseline shrinks
```

Current certification correction:

```text
IA-FLOW-01 = VERIFIED
IA-FLOW-06 = VERIFIED
IA-FLOW-02 = VERIFIED-BEHAVIOR / SOURCE-RELOCATION-REQUIRED
IA-FLOW-03 = VERIFIED
IA-FLOW-04 = VERIFIED-BEHAVIOR / SOURCE-RELOCATION-REQUIRED
IA-FLOW-05 = VERIFIED-BEHAVIOR / SOURCE-RELOCATION-REQUIRED

TAC-RAP-IA = NOT FROZEN under current topology authority
CERT-V2-011 = BLOCKED-IMPLEMENTATION until relocation + architecture proof
TAC-M4 = IN PROGRESS
```

This correction changes source topology/copy-model eligibility only. It does not
reopen TAC-XC-A/C/E classification, BOUND-TX-002, BOUND-TX-004, or verified
transaction/runtime behavior.

Current-source correction:

```text
IA-FLOW-06
  old Flow Card source state: IMPLEMENT-MISSING
  current source state: VERIFIED

IA-FLOW-02
  IAccountProvisioningActions currently evidenced at legacy Accounts/Public/Commands/ (topology normalization required)
  AccountProvisioningService implements IAccountProvisioningActions
  old IAccountProvisioningService deleted
  Identity handlers migrated to use producer Public seam contract
  BOUND-TX-004 transaction/runtime evidence verified
  current source state: VERIFIED-BEHAVIOR / SOURCE-RELOCATION-REQUIRED
```


### 0.4B M4 runtime-correctness exception — DeduplicationConsumeFilter failure-path flush

Discovered by the mandatory IA-FLOW-03 runtime proof (NOT by inspection alone).
Recorded as a narrow SOURCE_DEBT / correctness exception, not expanded product
behavior. No architecture change: TAC-XC-A/C/E classification, BOUND-TX-002,
BOUND-TX-004, dedup/outbox/retry design are all untouched.

```text
Defect (narrow, confirmed):
  RegistrationCompleted → WorkspaceProvisioningConsumer failure path
  DeduplicationConsumeFilter.CommandOwnedSendAsync failure branch could
  re-commit leftover tracked command entities.
  Root cause: EfRequestDataSession rollback clears the database transaction
  but never clears the EF ChangeTracker; the failure-path SaveChangesAsync
  (persisting the removed dedup claim) then also flushed the orphaned
  command-owned entities the rolled-back DataSession had left tracked.
  Effect: a failed workspace provisioning attempt could durably persist a
  partial Workspace/WorkspaceMember/outbox graph — a BOUND-TX-002-style
  local-atomicity violation at the consumer boundary.

Fix (applied):
  DeduplicationConsumeFilter.cs failure branches detach all tracked entries
  (static DetachAllTrackedEntries helper → EntityState.Detached) before the
  claim-removal SaveChangesAsync, so the claim cleanup never re-flushes the
  rolled-back command's tracked entities.

Regression (new, runtime-owner level):
  CommandOwnedConsumerFailure_DoesNotFlushRolledBackTrackedCommandChanges
  (Notrelix.Integration.Tests / Messaging / DeduplicationConsumeFilterFullIntegrationTests.cs)
  real PostgreSQL; failing consumer tracks a Workspace then throws;
  asserts rethrow, zero dedup claims, zero workspace rows, single invocation.
  Confirmed red without the fix (1 failed), green with the fix.

Corrected IA-FLOW-03 failure semantics (now executable):
  producer registration state remains committed; failed workspace attempt
  persists no Workspace, no WorkspaceMember, enrolls no
  workspace.created / workspace.member.added outbox rows; the attempt does
  NOT become an idempotent AlreadyExisted success on retry; real consumer
  retry policy runs to exhaustion (Interval(3,200ms), 4 invocations) while
  the injected projection failure continues; welcome-email delivery remains
  unaffected (its dedup status Succeeded).

Proof on implementation SHA:
  focused filter+phase-04 runtime chain classes: 9 passed (17.2s)
  regression red/green: red 1 failed on purpose, green passed
  full Notrelix.Integration.Tests: 368 passed (89.7s)
  Notrelix.Architecture.Tests: 585 passed (100.1s)
  Notrelix.Application.Tests RegisterTests: 5 passed

Scope guard:
  This fix is limited to the dedup filter cleanup path. It is NOT a
  redesign of dedup/outbox/retry, NOT a BOUND-TX change, and does NOT
  open a new milestone or CI lane.
```


### 0.4C M4 canonical freeze record

M4 frozen as the canonical capability-first topology + placement-enforcement
baseline. Freeze is an evidence-only decision record; it does NOT certify
`TAC-M4 = COMPLETE` (Invariant K exit deferred) and does not change
BOUND-TX-002/004, TAC-XC-A/C/E classification, or any runtime/transaction
authority. Supersedes the `TAC-M4 = IN PROGRESS` statements in §0.4 / §0.4A.

```text
Freeze scope (current authoritative topology):
  Accounts/Public capability-first published surfaces
    Accounts/Public/Membership/                      (facts + actions seam)
    Accounts/Public/PersonalAccountProvisioning/     (provisioning seam)
  application-model.md §5 Public/Ports/CrossContext surface grammar (frozen)
  Placement gates (CanonicalPathArchitectureTests):
    APP_PATH-004 handler position
      Features/{Context}/{Module}/Commands|Queries/{UseCase}/{Handler}.cs
      real UseCase folder required (parts.Length >= 5)
      Public/Ports/CrossContext/Services/Abstractions are not CQRS modules
    APP_PATH-006 exact frozen legacy Public baseline, recursive scan
    APP_PATH-007 Accounts/Public capability-first topology required

Enforcement proof (temporary probes, removed after proof):
  Accounts/Accounts/Commands/ZProbe.cs          → 004 fail (no UseCase folder)
  Accounts/Services/Commands/Test/ZProbe.cs     → 004 fail (Services as module)
  Accounts/Abstractions/Queries/Test/ZProbe.cs  → 004 fail (Abstractions as module)
  266/266 existing handler files conform (zero false positives)

Evidence at exact committed HEAD b0cee252da8b1a820c40058d4f4fcc294b86aa39:
  Notrelix.Architecture.Tests:  587 passed
  Notrelix.Integration.Tests:   368 passed (this slice; Phase-06 evidence 4/4)
  Backend CI (csharp) at exact HEAD: PASS (4m35s)
  Change detection / workflow lint / script checks / GitGuardian: PASS

State layers:
  TAC-M4 canonical freeze              = FROZEN
  TAC-RAP-IA / CERT-V2-011 / COMPLETE  = not claimed (Invariant K exit deferred)
```

Scope guard: no new architecture debt opened, no permanent test added beyond
the gated placement proof, no Frontend/Container/CI-lane change, no additional
gate probing in this slice.


### Interaction working-plan lifecycle

The former standalone Interaction Architecture working plan is now
`RETIRED / NON-AUTHORITATIVE`.

All active source-operating-pattern rules are distributed into:

```text
SPEC → pattern invariants / reference eligibility
PLAN → PRE-M4 Source Operating Pattern execution
TESTS → proof
CERTIFICATION → copy-model closure
STATUS → current evidence/state
```

Do not revive the standalone plan as a sixth active authority.

### 0.5 Extraction blockers

Current exact cross-process blockers:

```text
BOUND-TX-002
  workflow: AcceptInvitation / Workspaces→Accounts membership mutation
  SemanticBoundaryReady: true
  RuntimeSubstitutionReady: false/deferred
  ExtractionBlocked: true
  removal trigger:
    Accounts/Workspaces physical extraction
    or approved product decision allowing partial success/reconciliation

BOUND-TX-004
  workflow: Identity registration→personal Accounts provisioning
  SemanticBoundaryReady: true
  RuntimeSubstitutionReady: false/deferred
  ExtractionBlocked: true
  removal trigger:
    Identity/Accounts physical extraction
    or approved async provisioning product workflow

BOUND-TX-003
  workflow: Billing capacity + AutomationRule current shared request consistency
  ExtractionBlocked: true while the frozen current-local transaction model remains active
  final runtime closure: M9
```

An extraction blocker is not a boundary failure.

It means:

```text
semantic ownership can be clean
while
physical cross-process substitution is intentionally not certified yet
```

### 0.6 Current PR/CI snapshot

At PR head `84b04ed968929a8dc7d2ffa07b1e52fb1615970d`:

```text
HEAD:
  84b04ed968929a8dc7d2ffa07b1e52fb1615970d

Workflow conclusions:
  CodeQL            SUCCESS
  Documentation CI  SUCCESS
  CI Definition     SUCCESS
  Infrastructure CI SUCCESS
  Backend CI        SUCCESS
  API               SUCCESS
  Architecture      SUCCESS
  Domain/App/Infra   SUCCESS
  Integration/provider SUCCESS
  Platform          SUCCESS
  Backend CI gate   SUCCESS
  Frontend gate     FAILURE     (frontend dependency aggregate)
  ui-foundation     FAILURE     (visual snapshot baseline drift)
  dependency-security FAILURE   (pnpm audit network timeout -> registry.npmjs.org)
  Container CI gate FAILURE     (web-image failure)
  Web image validation FAILURE  (Trivy CVE in libexpat)
```

Therefore:

```text
Backend/architecture-relevant CI at current head = GREEN
Whole PR workflow set at current head            = NOT ALL GREEN
```

All backend jobs PASS on `84b04ed`. The remaining 5 failures are frontend /
web / container concerns, out of backend scope:

```text
1. Web image validation — Trivy HIGH in libexpat 2.8.2-r0 (CVE-2026-80256);
   fixed version 2.8.4-r0 (base image + real library upgrade)
2. Container CI gate (6s) — web-image=failure (same root)
3. Frontend gate (7s) — frontend dependency aggregate
4. dependency-security — pnpm audit ERR_SOCKET_TIMEOUT to registry.npmjs.org
   (network flake; re-run)
5. ui-foundation — Playwright screenshot baseline mismatch visual-visua-808bb /
   4c0b0 / a0b2c on mobile/tablet/desktop (frontend baseline needs update)
```

Fixing (1)/(2) requires an explicit infrastructure + real library upgrade
(libexpat 2.8.4-r0); (5) requires a reviewed frontend screenshot-baseline
update; (4) is a transient registry timeout. None are backend/architecture
defects and none are required for M3A / PRE-M4 closure. They are recorded here
to preserve exact evidence integrity at the current head.

### 0.7 Current next actions

```text
1. close M3A repository/mechanism classifier loophole
2. close M3C AccessPermissionRule classification
3. normalize CrossContext DI composition into dedicated CrossContextRegistration
4. run PRE-M4 47-flow coarse source audit and pin structural/non-reference sources
5. deep-map IA-FLOW-01..06 to current source and FlowId:EdgeId evidence
6. formally enter M4 only after M3 + PRE-M4 entry conditions pass
7. harden IA-FLOW-01/06 local flows
8. migrate IA-FLOW-02 to Accounts.Public provisioning action without changing BOUND-TX-004
9. verify IA-FLOW-03 event/outbox/consumer runtime chain
10. verify IA-FLOW-04 Public Direct and IA-FLOW-05 target action
11. certify M4 as first canonical Team Pack copy-model
12. roll the same mechanism-matched workflow into M5, then M6, ...
```

No new milestone.
No new architecture document family.
No dedicated PRE-M4 CI lane.
No service extraction authorization.

---

## M0 — Delta inventory (audited at b94312b3)

### M0.1 Real Flow inventory — 47 IDs

| Range | Count | Status at baseline |
|---|---|---|
| IA-FLOW-01..06 | 6 | 01 REUSE (UpdateProfile), 02 MIGRATE (private provisioning), 03 REUSE+envelope, 04 FROZEN-v2.2, 05 FROZEN-v2.2, 06 IMPLEMENT (RenameAccount) |
| WG-FLOW-01..06 | 6 | all REUSE/HARDEN — proofs only |
| WM-FLOW-01..05 | 5 | 01/02 REUSE, 03 HARDEN (scope/auth/dedup missing), 04 FROZEN-v2.2 + envelope, 05 IMPLEMENT-MOVE (source impl ownership) |
| DC-FLOW-01..07 | 7 | 01 REUSE, 02 IMPLEMENT (ArchivePage), 03 FROZEN-v2.2, 04 HARDEN (M2G), 05 HARDEN (envelope), 06 IMPLEMENT+FIX (actor/tenant), 07 CLASSIFY (stubs) |
| AI-FLOW-01..07 | 7 | 01 HARDEN (capacity M9), 02 FROZEN-v2.2 + envelope, 03 IMPLEMENT (real executor), 04 FIX (retry semantics + filter proof), 05/06/07 IMPLEMENT (Calendar) |
| BI-FLOW-01..05 | 5 | 01..04 IMPLEMENT (capacity + limit flip), 05 NOT-REQUIRED |
| AR-FLOW-01..04 | 4 | RELOCATE (Domain → Application/Infrastructure) + fix source ownership |
| PF-FLOW-01..07 | 7 | 01..06 proof-mostly, 07 IMPLEMENT (envelope runtime) |

### M0.2 Calendar semantic/persistence inventory

| Type | Owner | Persistence authority | Classification | Used in |
|---|---|---|---|---|
| IntegrationConnection | Integrations (generic provider relationship) | IntegrationSecretVersion rows + CurrentSecretVersion pointer | canonical | AI-FLOW-05/06 |
| CalendarIntegration | Integrations (Workspace-scoped Calendar binding) | calendar_integrations row (AccountId, WorkspaceId, ConnectionId, IsActive) | canonical; Workspace-scoped binding ≠ generic Connection | AI-FLOW-05/06 |
| IntegrationSecretVersion | Integrations (persisted SecretReference per connection/version) | integration_secret_versions | durable secret-reference authority — CurrentSecretRef is a pointer, not the store | AI-FLOW-05 round-trip proof |
| WebhookDelivery | Integrations (OUTBOUND delivery state) | webhook_deliveries | outbound only — MUST NOT become inbound receipt | none (forbidden in 07) |
| InboundWebhookEvent | LegacyGap (provider/ops intake) | inbound_webhook_events | no proven user-facing lifecycle → v2.6 intake uses Infrastructure-owned technical receipt state; InboundWebhookEvent remains exact/no-growth LegacyGap | excluded from AI-FLOW-07 |

Domain methods verified present: `CalendarIntegration.Deactivate(updatedBy, occurredAt)` (idempotent), `IntegrationConnection.RotateSecret(...)`, `IntegrationSecretVersion.Create(...)`.

### M0.3 Event tenant-envelope classification (semantics-first, not grep)

Envelope mechanics verified: `IntegrationEvent` base carries `AccountId?/WorkspaceId?`; `TenantContextConsumeFilter` sets Workspace tenant only when AccountId present, otherwise **System** — so any TenantScoped event with AccountId=null silently executes its consumer as System. That is the false-tenant defect FRZ-018 closes.

| Classification | Rule |
|---|---|
| TenantScoped | Workspace-scoped business fact → **AccountId + WorkspaceId both required** |
| AccountScoped | Account-level fact, no workspace context → AccountId required, WorkspaceId null |
| Global/System | No tenant semantics (explicit allowlist) → both null; consumer must not tenant-mutate without authoritative resolution |

| Event | Class | Rationale | Action |
|---|---|---|---|
| BoardItemMoved/Created/Archived + full WorkManagement catalog (Board*, BoardItem*, BoardField*, Checklist*, Label*, BoardView*) | TenantScoped | Board/Item facts are workspace resources | add AccountId to contract + mapper (additive, v1 → keep EventName, bump serialized shape via nullable add = Backward-compatible) |
| WorkspaceCreated/Archived/Unarchived, SpaceCreated, TeamCreated, WorkspaceMemberAdded/Removed | TenantScoped | workspace resources/facts | add AccountId + mapper |
| MentionCreated, CommentCreated | TenantScoped | collaboration facts on workspace resources | add AccountId + mapper (+ Mention actor fix) |
| PageCreated, PageArchived | TenantScoped | documents resources | add AccountId + mapper |
| ResourcePermissionGranted/Revoked, CustomRoleAssigned | TenantScoped | governance state inside workspace | add AccountId + mapper |
| IdentityRegistrationCompletedIntegrationEventV1 | AccountScoped | registration fact; Workspace provisioning consumer derives workspace after | already carries AccountId — conformant |
| WorkspaceInvitationDeliveryRequestedV1 | AccountScoped | email delivery intent; already carries AccountId | conformant |
| AccountCreatedIntegrationEvent | AccountScoped | account lifecycle | conformant |
| UserRegistered, UserDeactivated | HISTORICAL BASELINE — AccountScoped | identity user lifecycle precedes/no workspace binding; Register provisions personal Account in same txn → carry AccountId? **Historical decision: AccountScoped** — Register/Deactivate know the user's account (Register provisions it; Deactivate can resolve owner account). Add AccountId + mapper. WorkspaceId stays null. | SUPERSEDED BY FRZ-018 registration semantic correction: UserRegistered = None, UserDeactivated = None, IdentityRegistrationCompleted = Account |
| EmailVerificationDeliveryRequestedV1 | Global/System | pre-authentication security email; no account trust established beyond email; consumer is email dispatcher | explicit allowlist, both null |
| N8nDispatchRequestedV1, BoardItemMemberAssignedIntegrationEvent | TenantScoped | automation execution facts (already carry AccountIdValue/WorkspaceIdValue) | conformant |
| Billing SubscriptionChanged/Canceled | AccountScoped | billing is account-level | add AccountId + mapper |

No consumer in the delivery graph tenant-mutates under System after this classification; the Global allowlist contains exactly one family (EmailVerificationDeliveryRequested) whose consumer performs no tenant-scoped persistence.

> HISTORICAL BASELINE — SUPERSEDED BY FRZ-018 registration semantic correction:
> UserRegistered = None
> UserDeactivated = None
> IdentityRegistrationCompleted = Account
> This M0.3 table preserves the audit baseline and is not current normative authority.

### M0.4 Mention actor inventory

- `Mention` entity: no creator/mentioner field (only MentionedId)
- `MentionCreatedDomainEvent(AccountId, WorkspaceId, MentionId, Source, MentionedId, OccurredAt)`: no actor fact
- Mapper defect: `MentionedByUserId: domainEvent.MentionedId` (copies mentioned user as mentioner) — confirmed at CommentEventMapper.cs:35
- `MentionCreatedNotificationConsumer`: materializes `accountId: Guid.Empty` ×2 — confirmed
- Fix: capture trusted actor at mention creation → entity field + Domain event fact → exact mapping → consumer uses event facts. Mention actor ≠ mentioned user.

### M0.5 Common signature inventory (FRZ-017)

`IAccessGrantProjectionService` (Application/Common/Tenancy) exposes:
- `SyncAccountMemberGrantAsync(..., AccountRole role, ...)` — Domain.Accounts.Members.AccountRole in Common signature
- `SyncWorkspaceMemberGrantAsync(..., WorkspaceRole role, ...)` — Domain.Workspaces.Members.WorkspaceRole in Common signature

Callers: AccountMembershipActions, AccountProvisioningService, AcceptInvitationCommandHandler, AddMemberCommandHandler, and Infrastructure `AccessGrantProjectionService` (single implementation writing authz.access_grants rows).

Resolved by FRZ-017 slice below: Common no longer owns the grant-projection contract; Accounts and Workspaces own their role-bearing seams, and Infrastructure adapters delegate to the shared technical projection mechanism.

### M0.6 Billing semantic inventory

- `Entitlement.Limit` is `int` NOT NULL; guard forbids negative; current checker treats `Limit == 0` as unlimited (DatabaseFeatureGateChecker + BillingCapabilityFactsProvider)
- `WorkspaceFeatureUsage` exists: AggregateRoot, IWorkspaceScoped, versioned, `CurrentUsage/HardLimit/SoftLimit/OverageAllowed/ResetPeriod`, `Consume(amount, actor, at)` with hard-limit BusinessRule + QuotaExceededDomainEvent — disposition HARDEN-EXISTING for capacity state
- `FeatureUsageLedger`: immutable delta ledger (AccountId, WorkspaceId, FeatureCode, Delta, LogicalOperationId, ReferenceResource, ...)
- Entitlement Domain events `EntitlementGranted/LimitChanged` carry `decimal Limit` only — need `IsUnlimited` added before semantic flip
- No CapacityOperation/reservation table exists. M9 chosen design (SUPERSEDES the earlier "add keyed by LogicalOperationId" note): the FeatureUsageLedger row carries the dedup/effect identity (LogicalOperationId + ReferenceResource) and is the committed-usage authority; WorkspaceFeatureUsage is the concurrency/mutation authority; no separate reservation record is created. See M9-CLOSURE decision records below.
- Concurrency: `WorkspaceFeatureUsage` has aggregate Version (optimistic token); plus conditional consume under request transaction

### M0.7 Accounts local-admin inventory

- `Account.Rename(newName, updatedBy, updatedAt)` exists in Domain with closed-account guard + same-name no-op + `AccountRenamedDomainEvent`
- No authenticated Accounts Application command exists — IA-FLOW-06 IMPLEMENT-MISSING

## M2 — Frozen decisions (user-approved 2026-09-02)

```text
BILL-LIMIT-001 = ZERO_CAPACITY
  Limit = 0 && !IsUnlimited → zero capacity (unavailable)
  IsUnlimited = true → unlimited (explicit representation)
  Migration order: backfill is_unlimited=true WHERE limit=0 BEFORE semantic flip
  Entitlement events (Granted/LimitChanged) gain IsUnlimited in the same migration wave
  Unlimited future representation = null-able limit kind if model migrates; NO sentinel ints

CAL-CONN-001 = REVOKE_WHEN_NO_ACTIVE_BINDINGS
  DisconnectCalendar always starts CalendarIntegration.Deactivate(...)
  Generic IntegrationConnection revoke only when NO active product binding/capability
  of ANY bound type still references the Connection (not Calendar-only);
  any remaining active binding → retain Connection
  Provider/secret cleanup distinguishes success/retryable/terminal/unknown; no distributed atomicity

M2C = BOUND-TX-003 (extraction blocker)
  request transaction
    → Billing conditional capacity consume/reserve on WorkspaceFeatureUsage
    → AutomationRule create
    → capacity finalize
    → commit
  LogicalOperationId + DB concurrency protection mandatory
  feature create fail → rollback whole transaction (no distributed compensation)
  extraction → migrate to IDEMPOTENT-CAPACITY-PROTOCOL (reserve→create→consume→release/reconcile)
  Entitlement stays grant/limit authority — NO used counter on Entitlement
  Account-scoped shared quota future → separate scope-aware capacity aggregate (never reuse WorkspaceFeatureUsage across scope)

M2G = CreatePage + CreateComment (additive PermissionAction members; no ManagePage)
  CreatePage → Action=CreatePage, Resource=workspace
  CreateComment (BoardItem + Page variants) → Action=CreateComment, ResourceRef unchanged,
    ResourceKind decides target policy
  Not a behavior-preserving rename: explicit intended-policy test matrix required
  (Owner / admin / ordinary member / explicit resource allow / explicit deny if supported /
   BoardItem comment / Page comment)
  No mechanical copy of ManageBoard mapping

M2F = Mention actor authority
  Owner-side: trusted request actor captured at mention creation into Mention state
    + MentionCreatedDomainEvent gains creator fact
  Mapper maps event from Domain fact; consumer uses event facts
  Forbidden: MentionedByUserId = MentionedId; consumer-side reconstruction; Guid.Empty tenant

BOUND-TX-004 = Register/CompleteOAuthLogin + personal Account provisioning shared request transaction
  WorkflowOwner = Identity registration; extraction blocker; removal trigger = physical
  Identity/Accounts extraction or approved async provisioning product decision
```

## M5 — Workspace & Governance operating pack state

Branch: `architecture/workspace-governance-execution` (base `b0cee252da8b1a820c40058d4f4fcc294b86aa39`).

### WG-FLOW-01 — CreateWorkspace: VERIFIED (runtime evidence)

```text
verified-email requirement        → WorkspaceCreationPipelineAuthorizationTests
                                  (unconfirmed-email deny despite Owner role)
owner member + grant persistence  → CreateWorkspaceCommandHandlerTests
                                  (AccessGrant row: Workspace/"Active"/[Owner])
slug DB uniqueness race fail-closed → CreateWorkspaceCommandHandlerTests
                                  (concurrent request: second SaveChanges throws
                                   unique ux_workspaces_account_slug_active;
                                   exactly one workspace persists)
outbox atomicity                 → WorkspaceCreatedOutboxEvidenceTests
                                  (workspace.created + workspace.member.added
                                   stage atomically; rollback leaves zero)
```

### WG-FLOW-02 — AcceptInvitation: VERIFIED (Application matrix + production-graph transaction)

```text
behavior matrix (15 tests)        → AcceptInvitationCommandHandlerTests
invalid/expired/revoked/email-mismatch/archived/no-account/already-member/full-commit
BOUND-TX-002 commitment           → AcceptInvitationTransactionEvidenceTests
  1) full handler graph commits Account + Workspace member + grant + acceptance
  2) workspace-side failure after handler rolls back the entire graph
  3) already-member acceptance completes invitation with zero second
     workspace.member.added outbox intent
membership event reuse            → WG-TST-INV-INT-002 (documented in
  WorkspaceGovernance tests.md); WorkspaceMember.Create is the single
  raise-point shared by CreateWorkspace(owner) and AcceptInvitation(invitee)
```

### WG-FLOW-03 — WorkspaceMemberAdded→Activity: VERIFIED (reuse-existing runtime chain)

```text
producer ownership + registry identity    → WorkspaceMembershipEventReferenceTests (4 tests)
                                           event namespace "Application.Events.Workspaces"
                                           EventNameAttribute: workspace.member.added v1
                                           mapper maps AccountId/WorkspaceId/UserId/ActorUserId
outbox atomicity                          → WorkspaceMembershipOutboxEvidenceTests (2 tests)
                                           WorkspaceMember mutation commits outbox record atomically
                                           rolled-back mutation leaves zero committed delivery
full runtime-owner chain                   → WorkspaceMemberAddedScopedTenantRuntimeChainIntegrationTests (1 test)
                                           WorkspaceMember mutation → DomainEventInterceptor/outbox →
                                           OutboxDispatcher → MassTransit InMemory pipeline →
                                           TenantContextConsumeFilter → DeduplicationConsumeFilter →
                                           real WorkspaceMemberAddedActivityConsumer →
                                           WorkspaceActivityLogRecord projection under Workspace tenant
dedup proven                              → DeduplicationConsumeFilterFullIntegrationTests (7 tests)
                                           concurrent duplicate → only one consumer executes
                                           sequential duplicate → second skipped
                                           failure after claim → rollback, retry succeeds
architecture gates                        → ScopedEventTenantEnvelopeArchitectureTests
                                           PlatformReferenceCompletenessTests
                                           tenant-envelope classification + event registry coverage
Total: 28 focused tests across Application.Tests, Integration.Tests, Architecture.Tests
```

### WG-FLOW-04/05/06 — Governance grant/read/authz-pipeline: VERIFIED (M5 Wave 5)

```text
Historical state: BLOCKED — blocker = page-auth semantic decision
(STOP-AUTHZ-SEMANTIC hold).
Resolution (M5 Wave 5): the page-ACL semantics in current source were
reviewed explicitly against the frozen adjacent decisions (M2G ArchivePage
ladder, RESOURCE-SCOPE-A, cross-tenant hide) and found CORRECT — the hold was
stale authority, not a missing semantic. The semantics were FROZEN as the
M5 Wave 5 decision record (SPEC WG-FLOW-04 §"M5 Wave 5 frozen Page ACL
semantics"), and the mandatory proof matrix now closes on real PostgreSQL:
11 of 12 rows were already proven by 41D/41E/41F + ADR-007 locks; the ONE
missing row — revoke authorization — had zero test evidence despite a correct
production implementation, so 6 revoke tests were added (TESTS 41D2) instead
of reinterpreting semantics.
WG-FLOW-04 = VERIFIED; WG-FLOW-05 = VERIFIED; WG-FLOW-06 = VERIFIED.
Evidence: GovernanceResourcePermissionFlowTests 51/51 (45 pre-existing +
6 revoke); Architecture 612 incl. ADR-007 one-evaluator/Common-import locks;
HandlerAuthorizationBypass + UseCaseSecurityClassification + pipeline freeze
gates green.
```

### M5 evidence totals

```text
Domain.Tests            2581 passed (full)
Application.Tests        630 passed (full suite, 3 consecutive stable runs)
Architecture.Tests       587 passed (full)
Infrastructure.Tests     155 passed (full)
Integration.Tests        376 passed (full, includes CreateWorkspace +
                        WorkspaceCreatedOutbox + AcceptInvitation evidence)
                      — matches slice evidence (368 baseline + 8 new WG evidence …)
```

## M6 — Work Management operating pack state (FROZEN)

Branch: `architecture/work-management` (base develop `1565e283cad0591d9af795f9be4d3159a08b9e60`, PR #117 merge).
Merged to develop: PR #124 → merge commit `ad279ac43083b3e6b913a0918da88918eb56f989` (2026-09-07).

M6 freeze SHA:

```text
develop merge SHA: ad279ac43083b3e6b913a0918da88918eb56f989
PR: #124 (MERGED 2026-09-07T07:59:01Z)
CI on merged head: 27 checks passed / 0 failed (Backend, Frontend, Container,
Infrastructure, Documentation, CodeQL, CI Definition)
```

Flow dispositions (all VERIFIED at M6 freeze SHA):

```text
WM-FLOW-01 CreateBoard                REUSE/HARDEN → VERIFIED
WM-FLOW-02 WM→Collaboration read      REUSE → VERIFIED (CLOSED-FROZEN at M6 Phase 0,
                                      TAC-XC-B producer-owned semantic boundary)
WM-FLOW-03 MoveItem target action     HARDEN-EXISTING → VERIFIED
WM-FLOW-04 Work integration event     REUSE/HARDEN + TAC-FRZ-018 → VERIFIED
WM-FLOW-05 Analytics projection chain REUSE/HARDEN → VERIFIED
```

> **M6 Wave 6 superseding correction (historical — no PR #124 rewrite):**
> At the M6 freeze SHA `ad279ac4`, `WM-FLOW-05` was VERIFIED for the
> **semantic interaction and producer authority** only. The final
> **implementation ownership** normalization
> (`Analytics Application Port → Infrastructure delegate adapter →
> WorkManagement.Public → WorkManagement Application source`) did NOT exist at
> that SHA — it landed at **M10** (`M10-PORT-OWNERSHIP-NORMALIZATION`, evidence
> `b341d4ae`). The correct historical classification is therefore:
> `WM-FLOW-05` = `VERIFIED-SEMANTICS (M6)` /
> `IMPLEMENTATION-OWNERSHIP-NORMALIZED (M10)` = `CLOSED-FROZEN`.
> This record preserves M6's decision integrity and does not claim final
> implementation ownership at the M6 freeze SHA. CERT §122AA `WM-FLOW-05` and
> the CERT AR rebuild note reflect the same correction.

Audit blockers closed before merge:

```text
B1 cross-kind GUID collision   → ICollaborationResourceSummary output keyed
                                 (string Kind, Guid ResourceId) composite
                                 (commit 5501aa59; collision regression test)
B2 membership-only move auth   → WorkItemActionAuthorizer: authoritative
                                 IResourceLocator.LocateAsync then canonical
                                 MoveItem decision via same
                                 IRequestDescriptorRegistry + IAccessFactsProvider +
                                 IAccessPolicyEvaluator; deny → no BeginAsync,
                                 no mutation, no event (commit aa20d356)
B3 actor/attribution identity  → ExecutorUserId into RequestHash (actor-swap =
                                 deterministic conflict); CorrelationId consumed,
                                 CausationId pass-through only when upstream supplies
B4 mapper topology             → AutomationEventMapper moved to
                                 EventMappers/WorkManagement/
                                 BoardItemMemberAssignedEventMapper.cs with named
                                 attribution args (commit b1fe670f)
```

Removed transitional contract: `IWorkspaceMembershipFacts` (fact + impl + DI)
— zero consumers after B2 canonical authorization.

Final M6 validation at b1fe670f80b6ffea8eb1aaf6a1b31731967a7897:

```text
dotnet format --verify-no-changes   exit 0
dotnet build backend.slnx           17 projects, 0 errors
dotnet test backend.slnx            4817 passed / 0 failed (full)
Architecture.Tests                   596 passed / 0 failed
```

Evidence headings: TAC-WM-001..010 tests + WM-REF-001..004 dispositions per
CERT-V2-014/§33–38; mover policy evidence
(AutomationWorkActionChainIntegrationTests Owner allow/Member deny/actor-swap;
WorkItemActionsTests deny-never-reaches-store).

Reviewer audit disposition: WM-FLOW-01..05 audited HOLD → blockers B1–B4 fixed
→ CI green on exact audit HEAD → reviewer merged PR #124. M6 frozen on
`ad279ac43083b3e6b913a0918da88918eb56f989`.

## M7 — Documents & Collaboration operating pack state (CLOSURE PASS APPLIED — PENDING FINAL AUDIT)

Branch: `architecture/documents-collaboration` (base develop `ad279ac43083b3e6b913a0918da88918eb56f989`).
Closure pass follows the first-round audit verdicts (ArchivePage semantic defect, false-green idempotency, mention evidence gaps).
Evidence-integrity pass follows the second-round audit verdicts: duplicate-delivery proofs now observe the second real dedup claim (recording decorator, no sleeps), the archive DB-failure proof now violates a real unique index inside the same SaveChanges batch (no manual rollback), the mention fixture uses the seeded Identity user's own Id, false-green negative assertions were fixed (workspace vs owner-id comparison, unseeded actors), notification tenant isolation is certified at the RLS runtime layer, and the pipeline-wide cross-account hide is directly characterized.

Phase 0 decisions (reviewer-approved, extend M2F/M2G):

```text
M2F producer point = CreateComment command gains explicit MentionedUserIds
  (additive contract field); Mention entities commit in the same
  transaction as the comment; actor = trusted request context.
  No ContentMd parsing; no dedicated AddMention command at M7.
ArchivePage action = PermissionAction.ArchivePage (reviewer-approved M2G
  extension) with its own page-lifecycle policy; not ManageBoard, not
  ManagePagePermission.
ArchivePage authority ladder (frozen at closure):
  workspace Owner
  OR applicable highest-priority explicit Allow rule
  OR active documents.page ResourcePermission rank >= Manager
  WorkspaceRole.Admin carries NO default page lifecycle authority.
  Page visibility decides resource visibility/existence only — it is not
  lifecycle mutation authority.
Cross-tenant resource addressing = NotFound (hidden), never Forbidden —
  cross-tenant existence must not leak through the deny reason.
Self-mention = NOT forbidden (authority never froze a prohibition);
  producer normalization is empty-id rejection + distinct identity only.
RESOURCE-SCOPE-A (frozen): IResourceScopedRequest follows the
  authoritative resource location — the located resource's workspace
  becomes the execution workspace and authorization evaluates against it.
  Same-account cross-workspace is allowed iff the actor holds the target
  workspace authority (proven both directions: W2 member succeeds with
  W1 selected; same-account W2 outsider denied, no mutation, no outbox).
  Cross-account remains NotFound (hidden), never Forbidden.
Composite comment+mention write = ONE transaction with both outward
  enrollments (proven: mention-enrollment failure inside the interceptor
  chain aborts the whole SaveChanges — no comment, no mention, neither
  outward fact in committed state).
HTTP ingress evidence = API.Tests over the real endpoint pipeline:
  archive 204/404 canonical results through the real handler, and the
  mentionedUserIds JSON array binds into the command and persists as
  mention entities with the authenticated actor as mentioner.
```

Flow dispositions after closure:

```text
DC-FLOW-01 CreatePage        HARDEN-EXISTING → implemented (M2G: CreatePage on
                             workspace = member usage right; guest excluded;
                             deny-first rules preserved) + page.created outbox
                             atomicity + FRZ-018 runtime transport proof
DC-FLOW-02 ArchivePage       IMPLEMENT-MISSING → implemented from Page.Archive
                             authority: manager-rank lifecycle gate in the
                             canonical engine (visibility ≠ lifecycle
                             authority); lifecycle precedes authority →
                             archived page fails closed NotFound; full matrix
                             (owner/manager/editor/commenter/viewer/member/
                             workspace-admin/explicit allow+deny/guest/
                             outsider) + wrong-tenant NotFound + DB-failure
                             and outbox-enrollment-failure atomicity proofs
                             (commit ⇔ enrollment share one fate) + endpoint
                             + page.archived outbox evidence
DC-FLOW-03 Comment.BoardItem HARDEN (M2G intended policy: board-item comment =
                             member usage right via canonical kind branch;
                             guest hidden without explicit access; MoveItem
                             canonical deny preserved) + TAC-DC-004/006
                             missing/cross-scope proofs → TAC-DC-002/003 reuse
DC-FLOW-04 Comment.Page      HARDEN (M2G matrix; page branch + intended-policy
                             evidence) + independent missing/cross-scope
                             proofs → TAC-DC-002/003 reuse
DC-FLOW-05 Comment→Activity  HARDEN → producer maps owned content fact +
                             canonical kind string; outbox atomicity + full
                             runtime chain to real activity consumer +
                             duplicate delivery of the same business EventId
                             republished through the real pipeline: one
                             logical activity, one succeeded dedup record.
                             REPLY SEMANTICS FROZEN: root AND reply are both
                             created-comment facts on the canonical
                             comment.created v1 identity (additive
                             parentCommentId field; reply event carries its
                             owned content); runtime chain proves root + reply
                             both stage the fact and both reach the activity
                             projection
DC-FLOW-06 Mention→Notif     IMPLEMENT+FIX → mention producer exists (M2F);
                             MentionedByUserId maps the trusted actor fact;
                             consumer persists authoritative account envelope
                             (no Guid.Empty); runtime chain with a real seeded
                             workspace-member target + true duplicate delivery
                             (same business EventId) → one notification, one
                             recipient, one dedup record + cross-tenant
                             isolation (owning tenant holds the fact, no row
                             carries the foreign identity)
DC-FLOW-07 Documents events  CLASSIFY → producer mappings normal; page.created/
                             page.archived/comment.created/mention.created
                             pinned (context-owned, versioned, Workspace
                             tenant scope, records not aggregates); stub
                             consumers remain excluded from business evidence;
                             PageCreated runtime test explicitly classified as
                             FRZ-018 transport proof, not business proof
```

Test evidence families (M7 after closure):

```text
Application.Tests  CreateCommentTargetPolicyMatrixTests (11),
                   CreatePageTargetPolicyMatrixTests (6),
                   ArchivePageCommandHandlerTests (4),
                   CreateCommentResourceRefTests (mention same-UoW +
                   identity-only normalization)
Integration.Tests  CommentCreatedOutboxEvidenceTests (3, incl. TAC-XPK-009
                   target-aggregate-unchanged),
                   PageCreatedOutboxEvidenceTests (2),
                   PageArchivedOutboxEvidenceTests (5: commit/manual-rollback
                   companion/no-second-fact/REAL constraint-failure (unique
                   index violated inside the same SaveChanges batch)/
                   enrollment-failure),
                   CommentCreatedScopedTenantRuntimeChainIntegrationTests (1:
                   chain + duplicate delivery with the SECOND real dedup
                   claim observed via a recording decorator around the
                   production store),
                   MentionCreatedNotificationRuntimeChainIntegrationTests (2:
                   chain + duplicate delivery with observed second claim +
                   cross-tenant identity isolation; happy-path target is the
                   seeded Identity user's own aggregate Id),
                   PageCreatedScopedTenantRuntimeChainIntegrationTests (1:
                   FRZ-018 transport proof, stub-classified),
                   GovernanceResourcePermissionFlowTests (+18: ArchivePage
                   authority matrix incl. exact Commenter rank + wrong-tenant
                   zero-outbox + comment missing/cross-scope × both kinds with
                   real tenant-A actors, zero comment persisted, zero
                   comment.created outbox, and — for cross-scope — foreign
                   target untouched assertions; the pipeline-wide cross-account
                   hide is additionally characterized at the behavior level
                   with next() pinned never-executed),
                   RlsRuntimeEnforcementTests (notification tenant isolation
                   probed under the application role: workspace-B session
                   sees nothing of workspace-A's notification, granted
                   workspace-A member sees their row)
Architecture.Tests DocumentsCollaborationEventPinningArchitectureTests (5),
                   LegacyGap/MigrationPending entries removed for
                   ArchivePageCommand; request-execution-baseline updated to
                   the IWriteRequest data marker
Domain.Tests       CommentCreated content fact; Mention actor + event facts
Migration          20260907112011_M7MentionMentionedByActor — additive
                   collab.mentions.mentioned_by_user_id uuid NOT NULL with
                   NO persistent default + CHECK
                   (mentioned_by_user_id <> '00000000-...') mirroring the
                   Domain guard; table was empty before M7 (no producer
                   existed), no backfill/invented actor required
```

DC-REF-001..004 dispositions:

```text
DC-REF-001 ResourceRef          PASS — canonical SharedKernel ResourceRef
                                reused; no second ResourceRef introduced
DC-REF-002 local mutation       PASS — CreateComment.ForBoardItem stores the
                                comment through Collaboration persistence only;
                                no Work aggregate loaded/mutated (TAC-XPK-009
                                negative fixture proves target unchanged)
DC-REF-003 target fact/access   PASS — producer Public (PageAuthorizationFacts)
                                + consumer Port+Adapter patterns; mandatory
                                global ACL remains WorkActionAcl (AI-REF-002
                                pin, unchanged from M6)
DC-REF-004 Notifications        PASS — Features/Notifications classification
                                frozen at 2 email-link ports
                                (NotificationsClassificationTests unchanged)
```

Accepted debts (exact register):

```text
M7-DC-MENTION-TARGET-VALIDITY
  Current M7 producer accepts explicit non-empty user IDs without
  authoritative Workspace-membership validation. Whether mentionable
  identity means Workspace member, Account member, or broader Identity
  user is not frozen by current authority. Deferred to an explicit
  product/domain decision; no cross-BC validation seam is introduced in
  M7. The happy-path runtime evidence seeds a real workspace member; the
  debt must NOT be read as "unknown/cross-workspace mentioned users are
  rejected" — production carries no such semantic yet.

M7-FRONTEND-CONTRACT-SCHEMA-STALE
  Generated frontend REST contract schema is stale against the refreshed
  backend OpenAPI (archive-page operation, PermissionAction enum value,
  optional mentionedUserIds). Mechanical codegen follow-up for the
  frontend workspace; no frontend change is included in this PR per
  reviewer freeze.

M7-FRONTEND-MENTION-ADAPTER-REACHABILITY
  The work-management frontend adapter drops input.mentionUserIds and
  never sends it on the HTTP boundary, so UI mention selection does not
  reach the backend yet. The backend HTTP contract already accepts
  mentionedUserIds. This is frontend product reachability debt, NOT a
  backend mention-flow failure.
```

## M7 — Documents & Collaboration operating pack state (FROZEN)

Branch: `architecture/documents-collaboration` (base develop `ad279ac43083b3e6b913a0918da88918eb56f989`).
Merged to develop: PR #126 → merge commit `c28e319c2277dd8cea86372e9e63602a1522e639` (2026-09-08).

M7 freeze SHA: `c28e319c2277dd8cea86372e9e63602a1522e639`

Flow dispositions (all VERIFIED at M7 freeze SHA after 3 audit-driven passes):

```text
DC-FLOW-01 CreatePage        REUSE/HARDEN → VERIFIED (member usage right,
                             page.created outbox + FRZ-018 transport proof)
DC-FLOW-02 ArchivePage       IMPLEMENT-MISSING → VERIFIED (manager-rank
                             lifecycle gate; real DB-failure + enrollment-
                             failure atomicity; HTTP 204/404)
DC-FLOW-03 Comment.BoardItem HARDEN → VERIFIED (canonical kind branch;
                             MoveItem deny preserved; XPK-009)
DC-FLOW-04 Comment.Page      HARDEN → VERIFIED (independent per-kind proofs)
DC-FLOW-05 Comment→Activity  HARDEN → VERIFIED (owned content + canonical
                             kind; duplicate delivery with observed second
                             dedup claim; reply = same comment.created fact
                             with parentCommentId)
DC-FLOW-06 Mention→Notif     IMPLEMENT+FIX → VERIFIED (M2F actor; real seeded
                             target; duplicate observed; RLS runtime isolation)
DC-FLOW-07 Documents events  CLASSIFY → VERIFIED (4 facts pinned; stub
                             consumers excluded; PageCreated runtime =
                             FRZ-018 transport proof)
```

Frozen decisions (extend M2F/M2G):

```text
ArchivePage authority ladder = Owner OR explicit applicable Allow rule OR
  page ResourcePermission >= Manager; Workspace-Admin has no default page
  lifecycle authority; visibility ≠ lifecycle authority
Cross-tenant resource addressing = NotFound (hidden), never Forbidden;
  foreign tenant never adopted; next() pinned not-called
Reply semantics = root AND reply are comment.created v1 facts (additive
  parentCommentId); no separate reply outward contract
RESOURCE-SCOPE-A = resource-scoped request follows authoritative resource
  location; same-account cross-workspace allowed iff actor holds target
  workspace authority (proven both directions)
Self-mention = not forbidden; normalization is empty-id + distinct only
Composite comment+mention write = one transaction with both outward
  enrollments (failure proven)
HTTP ingress evidence = API.Tests real pipeline (archive 204/404;
  mentionedUserIds binding)
```

Accepted debts:

```text
M7-DC-MENTION-TARGET-VALIDITY — mentionable identity semantic deferred
M7-FRONTEND-CONTRACT-SCHEMA-STALE — mechanical codegen follow-up
M7-FRONTEND-MENTION-ADAPTER-REACHABILITY — adapter drops mentionUserIds
```

Final validation at a972fd01cb844efef4f9e8931d9a9df39cf50841: format 0;
build 17/0; full suite 4899/4899; Architecture 601/601; Backend CI success.

Reviewer audit disposition: 3 audit waves (flow closure → evidence
integrity → mechanism coverage) all resolved; reviewer merged PR #126.
M7 frozen on `c28e319c2277dd8cea86372e9e63602a1522e639`.

## M8 — Automation & Integrations operating pack state (FROZEN — VERIFIED)

Branch: `architecture/automation-integrations` (base develop `c28e319c2277dd8cea86372e9e63602a1522e639`, M7 freeze via PR #126).

Review status (audit round 2, 11 claims — all resolved; superseded by
round 4):

```text
1 Disconnect unreachable via pipeline → ResourceLocator branch
    integrations.calendar-integration (+ IIntegrationDbContext ctor dep);
    full-pipeline role-ladder evidence via ISender.Send
2 ManageIntegrations only matched workspace kind → one canonical ladder
    covers workspaces.workspace AND integrations.calendar-integration
3 Webhook endpoint injected concrete handler → ISender through the
    canonical pipeline; OpenAPI no longer infers the handler body
4 ProviderSecrets hard-coded in source → CalendarWebhookOptions bound
    from Integrations:CalendarWebhooks with startup fail-fast; unknown/
    disabled provider verifies nothing
5 Receipt lost raw/protected payload → PayloadHash = SHA-256 of the exact
    raw body; ProtectedPayload encrypted at rest (purpose
    Notrelix.Integrations.CalendarWebhooks.v1)
6 Dedup SELECT→INSERT race → atomic claim via unique constraint
    ux_inbound_webhook_receipts_provider_external_event_id; 23505 mapped
    to Duplicate; concurrent-claim test added
7 Atomicity test false positive → SaveChanges of the production
    DataProtectionIntegrationSecretStore inside an explicit transaction,
    rows visible before rollback, zero rows after; secret-version identity
    assertion corrected
8 Trigger fixture seeded incoherent tenant graph → coherent
    Workspace/WorkspaceMember/Rule account scope
9 New consumers unexercised → AutomationWorkEventRuntimeChainIntegrationTests
    publishes board_item.moved / board.item.created through the real bus:
    the moved chain redelivers the same EventId and observes the second
    claim attempt before asserting single execution + single intent
10 Implicit else → Webhook routing → explicit MoveItem vs Webhook intent
    staging in the evaluator
11 Receipt "business effect" overclaim → intake-only freeze: the accepted/
    processed technical receipt is the effect; no calendar mutation claimed

Two transport-composition defects found and fixed while proving (9):
- the five automation consumers had no ConsumerDefinition, so endpoint
  names fell back to class naming and Work facts never reached the
  evaluator → AutomationConsumerDefinitions.cs pins the registry names
- test fixtures published default(DateTimeOffset) OccurredAt (the
  production mapper always sets a real timestamp), which the Domain
  rejected at SetAuditOnCreate → fixtures carry real OccurredAt
A regression from fix (1) — three pre-existing pipeline provider fixtures
missing the new IIntegrationDbContext registration — was caught by the
full suite and bound like the rest of the context map.

Review status (audit round 3, closure pass — all 11 findings resolved;
head `b762a659fce41770995a229c25fd89cd2e9eea97`, superseded by round 4):

```text
P0  webhook CSRF bypass → route declares SignatureAuthenticatedWebhookAttribute
    + WebhookIntakeByIp pre-auth IP rate limit; HTTP evidence on the
    CSRF-enabled host: verified callback (no Bearer, no CSRF material) → 200,
    rejected → 401, never CSRF 403
P0  action routing → explicit MoveItem | Webhook switch; unsupported action
    types fail the execution closed (terminal, debt name in the error) —
    debt M8-AUTOMATION-ACTION-PARITY registered
P1  AI-FLOW-01 idempotency → CreateAutomationRuleCommand carries
    IdempotencyOperation(automation.rules.create.v1) + IIdempotentRequest +
    endpoint WithIdempotencyKey; same key + same request replays the original
    id with one persisted rule
P1  trigger matrix tenant graph → one coherent account scope; evaluator
    requires rule.AccountId == trigger.AccountId
P1  disconnect evidence → resource-scoped role ladder Owner/Admin allow,
    Member/Guest Forbidden through the full pipeline
P1  OpenAPI semantics → security filter reads endpoint metadata (webhook has
    no security); canonical-success filter drops phantom 200 when 201/204 is
    declared; contract regenerated and synced
P1  webhook data classification (Option A) → IWriteRequest through the
    canonical data session; claim = INSERT ... ON CONFLICT DO NOTHING
    RETURNING (no 23505 abort path); concurrent race evidence retained
P1  connect reuse → reauthorization applies ProviderAccountId + SyncDirection
    + secret rotation to a new version; duplicate Connect frozen as
    intentional rotation (evidence: two secret versions, one connection)
P1  bounded diagnostics → pre-auth IP rate limit bounds rejected-receipt growth
P1  TAC-GATE-024 → session-auth stub baseline emptied (a new unimplemented
    webhook stub is a violation); stale IWriteRequest allowlist entry deleted;
    compensation wording corrected; business-effect wording replaced by
    technical-receipt semantics
P1  retry authority → new consumer definitions match the existing sibling
    per-endpoint shape; global exponential transport policy stays the outer
    authority
```

New debts registered: M8-AUTOMATION-ACTION-PARITY (rule vocabulary broader
than the M8 runtime; unsupported actions fail closed, not misrouted).

Review status (audit round 4, final micro-closure — 2 blockers + P2 items
resolved; certification applied at this exact head
`446ecacbc5a199496ea86a45098b2f652d3cd99a`):

```text
Blocker CreateAutomationRule OpenAPI missing 201 → endpoint declares
    .Produces<Guid>(201); contract regenerated (201 + 409 + 503);
    dedicated assertion test pins the canonical 201 + idempotency wording
Blocker webhook DataSession runtime proof → CalendarWebhookDataSession
    IntegrationTests: ISender → real pipeline (RequestContract →
    ExecutionContext → DataSession) → real EfRequestDataSession transaction
    → real verifier → real intake → PostgreSQL; verified callback commits
    exactly one Processed receipt; forged signature is Result.Failure (not
    an exception) whose transaction still commits exactly one Rejected
    diagnostic receipt
P2  connect-failure class doc → frozen atomicity semantic stated (rollback
    commits no row of any kind, no compensation)
P2  TAC-GATE-024 class doc → stale M3 wording removed; post-M8 stub rule
    stated (empty baseline: any NotImplemented session-auth stub is a
    violation)
P2  trigger-matrix fixture → owner User seeded and ownerId derived from it
    (no ownerless actor identity)
P2  intake hash wording → "the exact rawBody under the provider's UTF-8
    contract"
Debt M8-WEBHOOK-RECEIPT-RETENTION registered (rejected diagnostics
    accumulate with no retention policy; follow-up lifecycle decision)
```

Certification (M8 closure) at exact HEAD
`446ecacbc5a199496ea86a45098b2f652d3cd99a`:

```text
AI-FLOW-01 CreateAutomationRule   VERIFIED
AI-FLOW-02 Work→AutomationExec    VERIFIED
AI-FLOW-03 AutomationExec→Work    VERIFIED
AI-FLOW-04 N8n                    VERIFIED
AI-FLOW-05 Calendar connect       VERIFIED
AI-FLOW-06 Calendar disconnect    VERIFIED
AI-FLOW-07 Calendar webhook       VERIFIED
M8 milestone                      FROZEN
```

Certification evidence at this exact head:

```text
CreateAutomationRule canonical 201 — endpoint .Produces<Guid>(201);
  exported contract POST /automations/rules = 201 + 409 + 503;
  assertion test OpenApi_RuleCreate_DeclaresCanonical201_IdempotencyContract
Webhook canonical runtime proof — CalendarWebhookDataSessionIntegrationTests:
  ISender → real pipeline (RequestContract → ExecutionContext → DataSession)
  → real EfRequestDataSession transaction → real verifier → real intake →
  PostgreSQL; verified callback commits exactly one Processed receipt;
  forged signature is Result.Failure whose transaction still commits
  exactly one Rejected diagnostic receipt
Exact-head Backend CI — Backend CI success at `446ecacb`
  (CodeQL, Container, Infrastructure, Documentation also green)
```


Phase 0 decisions (reviewer-approved):

```text
Secret-store port = IIntegrationSecretStore (Integrations-owned, narrow);
  DataProtection blob store persists encrypted payloads in the new
  integration.integration_secret_blobs table; the blob row id becomes the
  opaque SecretReference held by IntegrationSecretVersion (reference
  authority unchanged); blob + aggregates share ONE scoped context and one
  transaction fate (atomicity instead of compensation).
Governance integration permission = new PermissionAction.ManageIntegrations
  on workspaces.workspace: Owner/Admin default-allow, Member/Guest deny,
  explicit permission rules evaluate first through the one canonical engine.
AI-FLOW-02 trigger scope = the three runtime-reachable Work facts:
  member-assigned → ItemAssigned; board_item.moved → ItemMovedToGroup;
  board.item.created → ItemCreated. Trigger types without producer events
  remain configuration vocabulary, not runtime-reachable in M8.
Webhook intake state = new technical Infrastructure receipt
  integration.inbound_webhook_receipts (dedup identity (provider, external
  event id)); WebhookDelivery stays outbound-only; InboundWebhookEvent
  stays a no-growth Domain LegacyGap.
Capacity protocol for CreateAutomationRule = DEFERRED TO M9 (BILL-LIMIT-001
  unresolved); the existing Billing capability gate stays as the M8 seam.
```

Flow dispositions (all VERIFIED at the M8 freeze head):

```text
AI-FLOW-01 CreateAutomationRule   VERIFIED — REUSE/HARDEN → billing gate
                                  + rule lifecycle reused; stale allowlist
                                  entry removed; capacity protocol deferred
                                  to M9
AI-FLOW-02 Work→AutomationExec    VERIFIED — HARDEN → generalized trigger
                                  context; two new thin consumers (moved/created);
                                  dedup identity (rule, source event) preserved
AI-FLOW-03 AutomationExec→Work    VERIFIED — IMPLEMENT → production MoveItem
                                  executor (TAC-AI-011A): config parse, no-actor/
                                  inactive-rule/invalid-config terminal
                                  failures, OperationId = execution id,
                                  business vs technical outcome split
AI-FLOW-04 N8n                    VERIFIED — REUSE → durable intent + thin
                                  consumer + retryable/unknown outcomes (M6
                                  chain preserved); retry contract rethrow proven
AI-FLOW-05 Calendar connect       VERIFIED — IMPLEMENT → full frozen sequence
                                  with round-trip proof; reconnect advances
                                  secret pointer / replaces revoked-connection
                                  binding; commit fate atomicity proven
AI-FLOW-06 Calendar disconnect    VERIFIED — IMPLEMENT → Deactivate-first +
                                  CAL-CONN-001 last-binding revocation;
                                  provider cleanup not guessed
AI-FLOW-07 Calendar webhook       VERIFIED — IMPLEMENT → signature+timestamp+
                                  replay verification; receipt dedup by
                                  (provider, external event id); rejected
                                  callbacks are diagnostics-only; session-auth
                                  command shape replaced by signature-auth
                                  anonymous contract
```

Test evidence families (M8):

```text
Integration.Tests  AutomationTriggerMatrixIntegrationTests (7: 3 triggers +
                   duplicate-source + MoveItem executor lifecycle),
                   AutomationWorkEventRuntimeChainIntegrationTests (2:
                   moved + created through the real transport; the moved
                   chain redelivers the same EventId and observes the
                   second dedup claim before asserting single execution
                   + single intent),
                   CalendarConnectionFlowIntegrationTests (10: round-trip,
                   reconnect rotation, single-binding CAL-CONN-001, last-
                   binding revocation, commit-fate atomicity + full-
                   pipeline role ladder Owner/Admin/Member/Guest and
                   disconnect through ISender),
                   CalendarWebhookIntakeIntegrationTests (verified accept,
                   signature reject, replay reject, duplicate dedup +
                   concurrent claim race),
                   existing M6 chains preserved
Architecture.Tests 601 incl. manifest regeneration (new intent event +
                   additive parentCommentId), stale allowlist removal,
                   CSRF classification for the public webhook endpoint,
                   ownership gate registration for both new technical tables
Migration          20260908071133_M8IntegrationsSecretBlobsAndWebhookReceipts —
                   additive integration.integration_secret_blobs +
                   integration.inbound_webhook_receipts
```

Accepted debts:

```text
M8-AUTOMATION-TRIGGER-PARITY — trigger types without producer events
  (ItemUpdated, ItemDeleted, FormSubmitted, FieldChanged, ScheduleTrigger)
  remain configuration vocabulary, not runtime-reachable in M8; no
  synthetic producers were invented.
M8-CALENDAR-PROVIDER-VERIFICATION — the HMAC verifier uses per-provider
  shared secrets from configuration; real provider SDK asymmetric
  verification lands with real provider credentials.
M8-WEBHOOK-RECEIPT-RETENTION — rejected diagnostic receipts (status
  Rejected, synthetic rejected:* event ids) accumulate with no retention or
  cleanup policy; bounded-diagnostics enforcement is a follow-up lifecycle
  decision, not an M8 gate.
M9-CAPACITY-PROTOCOL — CreateAutomationRule participates in the frozen
  capacity protocol in M9 (BILL-LIMIT-001 / TAC-FRZ-020).
```

Final validation at ea2c021c75ec8f38b771338a8750db244d22ecc3: format 0;
build 17/0; full suite 4915/4915; Architecture 601/601.

Round-4 validation at `446ecacbc5a199496ea86a45098b2f652d3cd99a`: format 0;
build 17/0; full suite 4936/4936 (Domain 2585, Application 681, Platform
147, API 264, Infrastructure 158, Architecture 601, Integration 500); CI
Backend/CodeQL/Container/Infrastructure/Documentation green at this exact
HEAD.

## M9 — Billing & Entitlements capacity pack state (FROZEN — VERIFIED)

Branch: `architecture/billing-entitlements` (base develop
`3504414de7f90ee0c54eb7e1614df61a3a7f6804`).

Review status (M9 closure):

```text
Products/AUTOMATION_RULE capacity classification agreed
0 = hard-zero (no alias for unlimited); explicit unlimited via IsUnlimited
BILL-LIMIT-001     zero-limit/unlimited semantics resolved + gate-enforced
TAC-GATE-025       PASS — BillingCapacitySemanticsArchitectureTests (12)
BI-FLOW-01         VERIFIED — entitlement scope/state + limit + consumed
                   + requested amount resolution through real provider
                   (BillingCapabilityFactsProviderTests 12)
BI-FLOW-02         VERIFIED — hard-capacity last-slot race proven safe
                   (BillingCapacityFlowIntegrationTests, one remaining slot,
                   exactly one consumes; loser = version conflict / unique
                   conflict / limit rejection / gate failure)
BI-FLOW-03         VERIFIED — concurrent CreateAutomationRule→capacity:
                   one successful capacity-consuming rule; same
                   LogicalOperationId cannot double consume; downstream
                   failure rolls back usage + ledger atomically
BI-FLOW-04         VERIFIED — capability after real capacity mutation:
                   full production path through real pipeline, no seed-only
BI-FLOW-05         classified CONDITIONAL (external provider); not required
```

Decision records (M9):

```text
Consume/release protocol = Billing-owned capacity semantic action
  (consume + release/compensate) with LogicalOperationId + source resource
  + account/workspace scope; ledger is the durable dedup/effect journal.
Concurrency mechanism    = optimistic version token (AggregateRoot.Version,
  rowversion-style long token) + unique scope index on
  workspace_feature_usages per (account, workspace, feature);
  DbUpdateConcurrencyException → 412 PreconditionFailed; unique violation
  → 409 Conflict; over-limit consume → BusinessRuleException
  Billing_Usage_FeatureLimitExceeded (CapacityExceeded); gate failure →
  Result.Failure surface, not exception.
Rollback/compensation    = downstream feature failure (ConflictException)
  inside the same EfRequestDataSession transaction rolls back usage update +
  ledger row; no partial capacity effect. Release NOT-APPLICABLE-UNTIL-
  LIFECYCLE-EXISTS but implemented for completeness.
Entitlement precedence   = workspace-targeted beats account-scoped; newest
  grant wins within scope (CreatedAt desc, then Id) — TAC-BI-001.
```

Test evidence families (M9):

```text
Integration.Tests  BillingCapacityFlowIntegrationTests (4: two-concurrent
                   last-slot race with one winner TAC-XPK-BILLING-LAST-SLOT,
                   same-logical-operation replay dedup + conflicting payload
                   rejection + release replay, downstream-failure rollback
                   after consume, real-capability lifecycle through real
                   pipeline) against real PostgreSQL via Testcontainers
Application.Tests  24 Billing tests (BillingCapacityActionsTests 12 +
                   BillingCapabilityFactsProviderTests 12) +
                   CreateAutomationRuleBillingGateTests 5
Architecture.Tests BillingCapacitySemanticsArchitectureTests (12) —
                   TAC-GATE-025; zero == 0 representation + capacity-semantic
                   protection gate
Migration          20260911112111_M9BillingEntitlementsCapacityBackfill —
                   additive billing.entitlements limits +
                   billing.workspace_feature_usages +
                   billing.feature_usage_ledger
RLS                008 added billing.workspace_feature_usages policy
                   (workspace-scoped, matching sibling billing tables)
```

Accepted debts:

```text
M9-CAPACITY-RELEASE-LIFECYCLE — ReleaseAsync implemented but the rule
  lifecycle that would invoke it (delete/archive AutomationRule) does not
  exist yet; protected as NOT-APPLICABLE-UNTIL-LIFECYCLE-EXISTS.
```

Full-suite certification (M9 closure, exact-SHA qualified head):
the 4 full-suite failures found after the focused gates were all M9-caused
regressions, now closed:

```text
1. PublicSemanticContractArchitectureTests (ARCH-BC-005) — Public
   CapacityOperationConflictException extended the unapproved Common
   ConflictException. Fixed: derive from plain Exception (matches the
   WorkManagement Public-surface precedent) and map explicitly to 409
   Conflict in ProblemDetailsMapper.
2/3. CreateAutomationRulePipelineTests (idempotency pair) — the pipeline
   test provider predated the CreateAutomationRule→IBillingCapacityActions
   dependency and threw at DI activation. Fixed: register a consuming
   capacity mock in the test provider.
4. DomainContractSnapshotTests.DomainEvents_ShouldNotDrift — the approved
   DomainEvents copy predated the M9 IsUnlimited addition to
   EntitlementGrantedDomainEvent + EntitlementLimitChangedDomainEvent.
   Fixed: regenerated snapshot via UPDATE_DOMAIN_CONTRACT_SNAPSHOTS=1;
   diff is exactly the 2 IsUnlimited rows.
```

Final M9 evidence (rebuild + full suite):

```text
dotnet build backend.slnx: 17 projects, 0 errors (default build;
  full no-incremental reveals 64 pre-existing warnings in untouched
  OAuth/CircuitBreaker/ISystemClock/PostgresTestContainer code — zero
  from M9 files)
dotnet test backend.slnx (full): 4954 passed, 0 failed — 7 projects
DomainEvents.approved.txt: +2 rows (IsUnlimited on grant + limit-change)
```

Flow dispositions (all VERIFIED at the M9 freeze head):

```text
BI-FLOW-01   VERIFIED — REUSE provider, real PostgreSQL translation fixed
BI-FLOW-02   VERIFIED — IMPLEMENT capacity action + optimistic concurrency
BI-FLOW-03   VERIFIED — wired into CreateAutomationRule; gate via
             IBillingCapabilityFacts + consume; same-op dedup + error
             surface; downstream failure rolls back
BI-FLOW-04   VERIFIED — full production path classification
BI-FLOW-05   CONDITIONAL — external provider; not required for freeze
```

## M9-CLOSURE — Billing capacity closure wave (corrected authority)

> The M9 block above is the historical implementation record. The audit that
> produced this closure wave found the asserted verdicts were not all
> supported by the committed evidence (provider test count misreported,
> three resolve paths disagreed, fail-closed path unproven for the real fact
>   shape, last-slot race proven only at the handler level with a pre-seeded
>   row). This section is the current authoritative snapshot and SUPERSEDES
>   those overclaimed verdicts until the closure wave evidence lands.

Status: FROZEN at closure candidate SHA `3bff55b0` (PR #132). Exact-head
Backend CI run #34702604992 shipping that SHA is fully green — Format check,
Build solution, Platform messaging tests, Architecture tests, Integration and
provider tests, Domain/Application/Infrastructure tests, API tests and OpenAPI
contract, Backend CI gate — all success at the exact candidate SHA. The
remaining red at this SHA is Container CI (Backend image validation + Container
CI gate), caused by aspnet 9.0.19-bookworm-slim base image package
libpcre2-8-0 (2 HIGH, Trivy); remediation is Part B image re-pin in a separate
change, not part of TAC-FRZ-020.

The closure-pass evidence (release-at-full ceiling preservation, disabled-rule
occupancy, same-key retry ledger, workspace isolation) has landed on this
branch and is recorded in the closure-evidence counts below. The verdicts in
this section now stand against committed evidence at that state.

Frozen capacity semantics (existing + newly written back):

```text
BILL-LIMIT-001          zero numeric limit = zero capacity; explicit
  IsUnlimited = unlimited; no alias between them (unchanged, gate-enforced).
Entitlement precedence  applicable = Active AND (ExpiresAt IS NULL OR
  ExpiresAt > now) FILTERED BEFORE ordering; then Workspace-targeted beats
  Account-scoped; same scope -> CreatedAt DESC then Id DESC; LIMIT 1.  The
  applicable set must never be reduced by a post-row expiry check spinning
  off an expired Workspace grant that would have shadowed a valid Account
  grant.  TAC-BI-001 pins this; all productive paths must follow it.
Used authority           Used := SUM(feature_usage_ledger.delta) scoped
  (account_id, workspace_id, feature_code) in every state.  finite ->
  Remaining = max(0, limit - Used).  zero numeric limit -> Limit=0,
  Used = actual ledger sum, Remaining = 0.  unlimited -> Limit = NULL,
  Used = actual ledger sum, Remaining = NULL (NULL Remaining = unbounded,
  ledger history is never lost).  Missing entitlement -> unavailable,
  Limit = NULL, Used = ledger sum, Remaining = NULL.
WorkspaceFeatureUsage    mutation/concurrency authority only.  Entitlement is
  the limit authority; the effective limit is reconciled lazily on every
  Billing capacity op via WorkspaceFeatureUsage.ReconfigureLimits.  A
  downgrade below current usage retains the usage (transient over-limit
  state), Remaining = 0, and new consumption is denied; usage is never
  deleted by a limit change.
Workspace seeding        first-use get-or-create must be atomic (INSERT ...
  ON CONFLICT DO NOTHING -> reload authoritative row -> reconcile limit ->
  consume) behind a narrow Billing persistence primitive; the seed
  materializes CurrentUsage from the ledger sum, never a hard-coded 0.
LogicalOperationId       GLOBAL dedup identity.  One partial unique index on
  logical_operation_id (WHERE NOT NULL) across all scopes.  Semantic payload
  = (AccountId, WorkspaceId, CapabilityCode, signed Delta, SourceResource);
  identical payload -> replay; any difference -> deterministic conflict.
AUTOMATION_RULE           = rule EXISTENCE capacity.  Create consumes; disable
  (SetAutomationRuleEnabled) and enable do NOT release / re-reserve; the
  capability code stays AUTOMATION_RULE, never ACTIVE_AUTOMATION_RULE.  A
  capacity-releasing delete/archive lifecycle does not exist yet.
BI-FLOW-05               external Billing provider = NOT-REQUIRED at
  candidate; formal disposition BI-REF-003 = NOT-APPLICABLE-AT-CANDIDATE
  (do not fabricate Stripe).  Aligns status with spec/tests/cert.
```

Closure-wave dispositions (final — Wave 3 hardening landed; exact evidence in
the closure-evidence section below):

```text
BI-FLOW-01  VERIFIED — entitlement scope/state precedence with expiry
            filtering before ordering (TAC-BI-001) + Used = ledger sum in
            every state (BillingCapabilityFactsProviderTests 18)
BI-FLOW-02  VERIFIED — owner action fails closed on the real unavailable
            fact shape (null-limit unavailable or missing fails closed; a
            finite grant is the ceiling regardless of availability, so
            Release never zeroes it), first-use seed materializes
            CurrentUsage from the ledger sum, effective limit reconciled
            on every op, direct last-slot race + first-use race +
            materialization + global index rejection + release-at-full +
            disabled-rule occupancy + same-key retry ledger + workspace
            isolation proven on real PostgreSQL
            (BillingCapacityFlowIntegrationTests 12)
BI-FLOW-03  VERIFIED — concurrent CreateAutomationRule→capacity: one
            capacity-consuming rule; same LogicalOperationId cannot double
            consume (global identity); downstream failure rolls back usage +
            ledger atomically; gate via CreateAutomationRuleBillingGateTests 5
BI-FLOW-04  VERIFIED — capability after real capacity mutation through the
            full production pipeline, no seed-only evidence
BI-FLOW-05  NOT-REQUIRED / NOT-APPLICABLE-AT-CANDIDATE (BI-REF-003)
TAC-FRZ-020 FROZEN — see closure evidence below
```

### M9-CLOSURE — Wave 3 closure evidence (hardened protocol, landed)

Implementation (all gates green at this evidence; branch
`architecture/billing-entitlements`, candidate SHA `3bff55b0`, PR #132):

```text
Domain          WorkspaceFeatureUsage.ReconfigureLimits(hard, soft, actor,
                occurredAt) — effective-limit reconciliation is a Domain
                mutation: limits follow the capability fact; same limits =
                semantic no-op (no version, no event); downgrade below current
                usage retains usage (transient over-limit, new consumption
                denied, never deleted); unlimited clears HardLimit/SoftLimit
                to NULL; negative hard/soft and soft > hard rejected.
                WorkspaceFeatureUsageLimitsReconfiguredDomainEvent — owned
                fact emitted only on an actual limit change (event name
                billing.workspace-feature-usage-limits-reconfigured).
                Domain proof: WorkspaceFeatureUsageTests 25.

Application     BillingCapacityActions — the effective limit is the
                request-agnostic ceiling: a finite grant N (including zero)
                is always the ceiling regardless of whether the current
                request is available, so Release can never zero a finite
                ceiling; IsAvailable only splits the null-limit facts:
                available + Limit NULL -> unbounded; unavailable null-limit
                grant or missing fact -> 0 (fail-closed; the real provider
                shape for revoked/disabled/expired facts is
                unavailable-with-NULL-limit, not a missing fact).
                First-use get-or-create runs through the narrowed
                persistence primitive and the loaded authoritative row is
                reconciled with the fact on every capacity op
                (LoadOrCreate + ReconcileLimits before every consume/release).
                LogicalOperationId is GLOBAL identity: replay lookup by id
                alone; SamePayload compares (AccountId, WorkspaceId,
                CapabilityCode, signed Delta [consume +amount / release
                -amount], SourceResource); identical payload -> replay (no
                second effect), any difference (including another scope) ->
                CapacityOperationConflictException.  RemainingOf clamps at 0.
                Application proof: BillingCapacityActionsTests 20,
                BillingCapabilityFactsProviderTests 18,
                CreateAutomationRuleBillingGateTests 5 (Application.Tests
                full project 714).

Infrastructure  ApplicationDbContext.GetOrCreateWorkspaceFeatureUsageAsync
                (ApplicationDbContext.BillingCapacity.cs) — raw INSERT ...
                SELECT ... ON CONFLICT (account_id, workspace_id,
                feature_code) DO NOTHING.  CurrentUsage seeds from
                COALESCE(SUM(delta) FROM billing.feature_usage_ledger ...),
                never a hard-coded 0, so the concurrency authority can never
                drift below the committed ledger.  Requires an ambient
                request transaction (BOUND-TX-003 guard) so the seed commits
                atomically with the waiter's SaveChanges.  Allowlisted under
                RawSqlArchitectureTests.ExecuteSqlRaw allowlist.

Data/schema     ux_feature_usage_ledger_logical_operation is now a
                single-column partial unique index on logical_operation_id
                (WHERE NOT NULL): global dedup identity across all accounts/
                workspaces (replaces the composite
                account/workspace/feature/logical-operation index).  Edited
                in-migration (M9 has not left the branch); snapshot +
                Designer in sync; `dotnet ef migrations
                has-pending-model-changes` clean.
```

Concurrency proofs (real PostgreSQL via Testcontainers,
`BillingCapacityFlowIntegrationTests`):

```text
last-slot race       seeded current 1 / limit 2, two concurrent explicit
                     transactions + contexts, distinct logical ids -> exactly
                     one wins.  Loser either hits DbUpdateConcurrencyException
                     (version token) or the Domain limit guard after
                     observing the winner's committed row; ledger records
                     exactly one +1 effect; Version advances exactly once.
first-use race       fresh scope, limit 2, two concurrent first uses -> both
                     succeed, CurrentUsage 2, two +1 effects, no false
                     unique-scope conflict.
ledger materialize  committed history (+2) with no usage row; first consume
                     (1) -> CurrentUsage 3 = materialized ledger sum + new
                     consume.
global dedup        the same LogicalOperationId inserted in a second
                    (account, workspace) scope is rejected by the database
                    unique index.
release-at-full     seeded current 2 / limit 2, Release at full capacity:
                    the finite ceiling stays 2 (never zeroed), Remaining 1,
                    exactly one -1 ledger effect.
disabled-rule       real SetAutomationRuleEnabled pipeline: a Disabled rule
                    still occupies its slot (usage 1 remains, no release
                    ledger effect).
retry-ledger        same Idempotency-Key / LogicalOperationId across real
                    create+capacity runs -> 1 rule and exactly one +1
                    ledger effect; recording is idempotent.
workspace-scope     usage under one account grant is isolated per workspace
                    (creating in A leaves B's Remaining untouched).
```

Exact evidence commands:

```text
dotnet test Domain.Tests      WorkspaceFeatureUsageTests            25 passed
dotnet test Application.Tests BillingCapacityActionsTests          20 passed
                               BillingCapabilityFactsProviderTests  18 passed
                               CreateAutomationRuleBillingGateTests  5 passed
                               full project                         714 passed
dotnet test Integration.Tests BillingCapacityFlowIntegrationTests  12 passed
                               full project                         512 passed
dotnet test Architecture.Tests full project                        600 passed
dotnet build backend.slnx                                            17 projects, 0 errors
dotnet format backend.slnx --verify-no-changes                      clean
exact-head CI at 3bff55b0                                           Backend CI run
                                                                     #34702604992 all
                                                                     jobs success
                                                                     (Format, Build,
                                                                     Platform,
                                                                     Architecture,
                                                                     Integration,
                                                                     Domain/Application/
                                                                     Infrastructure,
                                                                     API+OpenAPI,
                                                                     gate)
```

Retirement recorded: `IFeatureGateChecker` +
`DatabaseFeatureGateChecker` + `DevNullFeatureGateChecker` are dead code
(zero callers of `IsFeatureEnabledAsync`) and are removed in this wave; the
canonical feature-gate surface becomes `BillingCapabilityFactsProvider` +
`AccessFactsQuery`.  The `DEBT-BILL-001` pins below that reference the
retired checker are closed by this wave.

## M10 — Analytics & Reporting operating pack state (FROZEN — MERGED PR #147)

Branch: `architecture/analytics-reporting` (created from origin/develop
`050f9ad2` = Merge PR #132; tracking origin/develop). Docs in
docs/workstreams/executions/backend-team-architecture-closure are untracked.

M10 = TAC-M10 — Analytics & Reporting Team Operating Pack (TAC-AR).

Flows (all MIGRATE-EXISTING):

```text
AR-FLOW-01  Live Work placement projection      → TAC-AR-FLOW-01
AR-FLOW-02  Analytics local read                → TAC-AR-FLOW-02
AR-FLOW-03  Projection rebuild                  → TAC-AR-FLOW-03
AR-FLOW-04  Failure recovery                    → TAC-AR-FLOW-04
```

Focused gate: TAC-AR. Freeze: TAC-FRZ-011/016. Certification: AR-REF-001 →
V2-018.

Decision freeze (Phase 0, CLOSED-FROZEN TAC-XC-B,
M10-PORT-OWNERSHIP-NORMALIZATION):

```text
Analytics Application → Analytics-owned source/rebuild Port
→ Infrastructure delegate adapter (WorkItemProjectionSourceAdapter)
→ WorkManagement.Public IWorkItemProjectionSource (workspace snapshot + item lookup)
→ WorkManagement Application owner implementation
→ IWorkManagementDbContext → Work persistence
```

Direct Analytics → Work persistence: FORBIDDEN.  `GetItemPlacementAsync` is
extended onto the producer-owned `IWorkItemProjectionSource` contract (not a
separate adapter method).  Work event schema is NOT changed in M10.

Projection relocation (M10A): `WorkspaceWorkItemPlacementProjection` moves out
of Domain → `Application/Features/Analytics/Projections/WorkItemPlacement`.

## M10 slice evidence — port-ownership normalization executed

Scope: M10A–M10E (implementation + focused proof). M10F/M10G (doc write-back +
full closure audit + CI gate) tracked separately below.

Ownership:

```text
WorkManagement owns authoritative placement snapshot truth
WorkManagement Application implements producer Public source
  (IWorkItemProjectionSource → WorkItemProjectionSourceService
   over IWorkManagementDbContext)
Analytics owns the local placement projection + live consumers + rebuild use
  case; Analytics Application never reads Work DbContext
Analytics-owned source/rebuild Port = IWorkItemProjectionSourceAdapter
  (Application/Features/Analytics/Abstractions)
Infrastructure delegate adapter = WorkItemProjectionSourceAdapter
  (CrossContext/Analytics/WorkManagement): implements the Analytics port,
  delegates to producer Public IWorkItemProjectionSource, never touches
  Work persistence
Runtime binding: Application DI registers producer source
  (IWorkItemProjectionSource → WorkItemProjectionSourceService);
  CrossContextRegistration binds only the Analytics port
  (IWorkItemProjectionSourceAdapter → WorkItemProjectionSourceAdapter)
```

Change:

```text
M10A  WorkspaceWorkItemPlacementProjection relocated out of Domain →
      Application/Features/Analytics/Projections/WorkItemPlacement (plain
      Application class; no Domain Entity base, no Domain IWorkspaceScoped
      workaround, no OnModelCreating filter change; RLS + explicit workspace
      scoping preserve tenant defense). CLR entity identity change recorded
      by a NEW no-op migration M10AnalyticsProjectionRelocation (schema
      unchanged; append-only history preserved — no existing migration file,
      including head M9, is modified).
M10B  IWorkItemProjectionSource (WorkManagement.Public.ItemPlacement) extended
      with GetItemPlacementAsync; producer Application implementation
      WorkItemProjectionSourceService added (BoardItems AsNoTracking →
      WorkItemPlacementSnapshot, Version + UpdatedAt ?? CreatedAt).
M10C  Analytics-owned source/rebuild Port IWorkItemProjectionSourceAdapter
      added (Application/Features/Analytics/Abstractions); the old
      embedded consumer-side interface definition removed from
      WorkItemPlacementConsumers.cs; Infrastructure
      WorkItemProjectionSourceAdapter reduced to a delegate-only adapter
      (implements the Analytics port, injects IWorkItemProjectionSource);
      CrossContextRegistration + ProductionAdapterGraphTests updated to the
      delegate graph; integration tests construct the producer source service
      directly (producer authority) instead of the adapter.
M10D  Consumers (BoardItemMoved/Created/ArchivedPlacementConsumer) confirmed
      to read only the Analytics port; no stale reference to the old
      Infrastructure.Messaging.Consumers.Analytics interface FQN remains.
M10E  RebuildWorkspacePlacementsCommandHandler aligned onto the Analytics
      source/rebuild Port (was injecting producer source directly).
      Coverage gaps closed with 7 new integration tests: archived + created
      consumer paths; rebuild empty source / multiple items / archived /
      duplicate idempotent / workspace isolation.
```

Evidence:

```text
dotnet build backend.slnx: 17 projects, 0 errors
Notrelix.Architecture.Tests: 600 passed
Notrelix.Infrastructure.Tests: 158 passed
Notrelix.Application.Tests: 714 passed
Notrelix.Domain.Tests: 2593 passed
Notrelix.Platform.Tests: 147 passed
Notrelix.API.Tests: 264 passed
Notrelix.Integration.Tests (Analytics filter): 16 passed
  (9 pre-existing + 7 new coverage-gap tests)
Notrelix.Integration.Tests (MigrationSmoke): 4 passed (real PostgreSQL
  applies the new no-op M10AnalyticsProjectionRelocation)
Notrelix.Integration.Tests (TenantIsolation/CrossTenant/RlsRuntime):
  45 passed
Notrelix.Integration.Tests (full project): 519 passed
  (incl. BoardItemMovedOutboxRuntimeTests — its granular BuildProvider now
  also binds the producer-owned IWorkItemProjectionSource, mirroring what
  the production AddApplicationServices composition registers; without it
  the real placement consumer fails activation after the M10C chain change)
dotnet format --verify-no-changes: clean
ef migrations has-pending-model-changes: clean
check-migration-discipline.py --full-range: conservative chain validation ok
```

TAC-AR mapping (executable evidence):

```text
TAC-AR-001   projection relocation + EF identity: build + migration check
TAC-AR-001B  producer source implementation ownership: WorkItemProjectionSourceService
             + ProductionAdapterGraphTests delegate-only assertion
TAC-AR-001C  frozen TAC-XC-B chain: Analytics port → delegate adapter →
             producer Public source (ProductionAdapterGraphTests,
             Rebuild_ThroughPublicItemPlacementContract_KeepsWorkAsProducerAuthority)
TAC-AR-002   consumers use producer boundary: Moved/Created/Archived consumer
             path tests
TAC-AR-003..005  first/dedup/out-of-order delivery: MovedFact last-write-wins +
             StaleOrDuplicateFact_DoesNotRegressProjection
TAC-AR-006   scope isolation: WorkspaceScopes_StayIsolated +
             Rebuild_WorkspaceIsolation
TAC-AR-007   source truth separation: producer snapshot authority rebuild test
TAC-AR-008   Analytics local query reads projection only (GetWorkspacePlacementsQuery)
TAC-AR-009   exact rebuild: empty/multiple/moved/archived/duplicate/drift/
             cross-workspace integration tests
TAC-AR-010   no arbitrary foreign persistence: delegate-only adapter +
             Rebuild_ReplacesProjectionFromProducerSnapshot_WithoutForeignAccess
TAC-AR-011   consumer failure: BoardItemMovedPlacementFailureRecoveryRuntimeTests
             (injected projection crash before commit → claim rolls back →
             Platform delivery retries → projection converges; Work source
             Version/GroupId unchanged) — LOCAL, exact-head CI pending
TAC-AR-012   freshness/ordering: single canonical watermark =
             LastOccurredAt.UtcTicks across live + rebuild; stored watermark
             asserted; physical write races rejected by PostgreSQL xmin
             row-version (DbUpdateConcurrencyException → reload converges on
             newer live fact) — LOCAL, exact-head CI pending
TAC-AR-013   pack completeness: consumers + projection + local query +
             idempotency + ordering + scope + rebuild + source-truth separation
```

## M10 closure wave (review at PR #147 HEAD `0b88ea86` → local closure)

Reviewer verdict at that HEAD: M10-PORT-OWNERSHIP-NORMALIZATION CLOSED; the
M10 pack overall HOLD — AR-FLOW-04 unproved and a real rebuild concurrency
defect. This wave closes exactly that finding set (no architecture redesign).

Implemented locally (all on `architecture/analytics-reporting`):

```text
W1  Single canonical watermark: SourceRevision is now ALWAYS
    LastOccurredAt.UtcTicks (projection Upsert/ApplyNewer take no separate
    revision parameter; aggregate Version never enters ordering). Live apply
    strictly-greater; rebuild reconcile applies at >= (equal = authorized
    drift repair) and PRESERVES local when snapshot older.
W2  DB-level physical protection: shadow uint xmin + IsRowVersion() in
    WorkspaceWorkItemPlacementProjectionConfiguration (persistence mechanism
    only — Application model untouched); append-only no-DDL migration
    M10PlacementRowVersion (xmin exists on every PG table; AddColumn
    removed per documented Npgsql pattern).
W3  Stale-snapshot delete protection: rows missing from a rebuild snapshot are
    revalidated through the Analytics source Port (producer still reports →
    reconcile; producer gone → delete). WorkspaceWorkItemPlacementService
    now composes only IReportingDbContext + IWorkItemProjectionSourceAdapter.
W4  TAC-AR-002 normalization: BoardItemMovedPlacementConsumer projects from
    event-owned facts (payload already carries AccountId/BoardId/NewGroupId;
    missing account falls back to the Platform-restored tenant); the created
    consumer keeps the producer fallback (event lacks GroupId). Architecture
    gate pins the move consumer's independence from the source port.
Proofs added:
    concurrency: Rebuild_ConcurrentNewerLiveFact_ConflictsAtDatabase_AndConvergesOnReload
      (two real PostgreSQL transactions; stale rebuild write must throw
      DbUpdateConcurrencyException, reload must preserve newer live fact);
      Rebuild_OlderSnapshot_PreservesNewerLiveState (guard alone, no conflict);
      Rebuild_RowMissingFromSnapshot_SurvivesWhenProducerStillReportsIt;
      Rebuild_EmptySource… now requires producer revalidation before delete.
    recovery: BoardItemMovedPlacementFailureRecoveryRuntimeTests (TAC-AR-011);
      Rebuild_ProducerUnavailable_FailsWithoutMutation_ThenRetryConverges;
      CreatedConsumer_SnapshotUnavailable_LeavesProjectionUnchanged.
    flow-02: GetWorkspacePlacements_ProductionHandler_ReadsLocalScopedProjection
      executes the production query handler (AR-FLOW-02 / TAC-AR-012).
    flow-03: Rebuild_HandlerUnchanged_FakeProducerContract_DrivesProjectedTruth
      (TAC-AR-001C producer-contract substitution; handler/service unchanged).
    boundaries: AnalyticsProjectionSourceOwnershipArchitectureTests (8 gates:
      adapter namespace implements producer source never / ctor delegate-only /
      sole producer implementation is WorkManagement Application / event-
      sufficient consumers source-independent / no Work persistence in
      consumers / query + service port purity).
    unit: WorkspaceWorkItemPlacementProjectionTests (6 watermark semantics).
Evidence: build 17/0; format clean; has-pending-model-changes clean;
migration-discipline ok; Domain 2593 · Application 720 · Infrastructure 158 ·
Platform 147 · API 264 · Architecture 608 · Integration 528 (all green local).
```

Corrected M10 verdict (exact-head CI green on PR #147 head `b341d4ae`;
merged to develop as merge commit `25dbb34b00107ab1b27436625e4cbf3fc1a8f1de`,
PR #147 MERGED 2026-09-14T14:44:31Z):

```text
M10-PORT-OWNERSHIP-NORMALIZATION   CLOSED-FROZEN (TAC-XC-B — Phase 0 decision,
                                     frozen seam chain, unchanged by this wave)
AR-FLOW-01  live projection         VERIFIED
AR-FLOW-02  local read              VERIFIED
AR-FLOW-03  rebuild                 VERIFIED
AR-FLOW-04  failure/recovery        VERIFIED (watermark guard + xmin row
                                     version + snapshot revalidation + crash/
                                     retry runtime proof; CI 28 checks green)
CERT-V2-018 / AR-REF-001            MET
TAC-RAP-AR freeze (FRZ-011/016)    FROZEN on merge to develop (25dbb34b)
M10                                 CLOSED-FROZEN @ b341d4ae evidence
```

Remaining:

```text
M10G closure audit: DONE — CI run 34830197381 at exact head b341d4ae: full
project suites with verify-required-proofs-trx.py profiles and the append-only
migration-discipline gate over BASE..HEAD; 28 checks passed / 0 failed;
merge state CLEAN. Locally the same wave: build 17/0, format clean,
has-pending-model-changes clean, Domain 2593 / Application 720 /
Infrastructure 158 / Platform 147 / API 264 / Architecture 608 /
Integration 528 green.
Merge: DONE — PR #147 merged into develop at 25dbb34b; TAC-RAP-AR frozen.
FRZ-011/016 no-regression: Architecture 608 / tenant / RLS suites green
locally and in CI.
```

## Residual debt remediation — Wave 0: frozen current truth (M3–M10)

Baseline: branch `architecture/residual-debt-remediation` from integrated
develop `25dbb34b` (PR #147 merged). Docs-only wave — no production source
touched. Classifications below are the frozen execution truth; waves 1–6 act
against exactly these rows.

### W0.1 Segment matrix

```text
M3  Common purity / gates
    TAC-M3 gates (M3A/B/C/D, TAC-GATE-022..025)       = CLOSED
    §0.2 "M3 = PASS" vs §0.3 "TAC-M3 remains IN PROGRESS" contradiction
      → AUTHORITY-DRIFT: the §0.3 remaining-closure text predates the
        PRE-M4 6-check PASS it itself records; §0.2 wins. §0.3 downgraded to
        historical layer by this wave.
    TAC-GATE-022 baseline (5 exact Common signature debt entries)
      → SOURCE-DEBT (governed, shrinking only through its own migrations):
        [S1] IRequirePermission -> PermissionAction          → Wave 3 burn
        [S2] AuthSessionIssuer -> IIdentityDbContext         → Wave 1 burn
        [S3] AuthSessionIssuer -> Domain User                → Wave 1 burn
        [S4] IAuthSessionIssuer -> Domain User                → Wave 1 burn
        [S5] IJwtService -> Domain User                       → Wave 1 burn
        (IJwtService moves with the issuer pair: its ONLY consumers are
        AuthSessionIssuer + Identity RefreshToken; the signature is
        Identity-owned session issuance, so the Wave 1 acceptance
        "Common/Security/Auth no longer holds Identity User/UserSession
        semantics" requires it. AuthResult/UserDto are neutral DTO records
        (no Domain User/UserSession exposure, gate-clean) and STAY.)

M4  Interaction/Accounts
    IA-FLOW-01/03/06 = VERIFIED; IA-FLOW-02/04/05 = VERIFIED-BEHAVIOR
    §0.4A "SOURCE-RELOCATION-REQUIRED" (Accounts/Public capability-first)
      → DOC_STALE: Accounts/Public/Membership + PersonalAccountProvisioning
        exist in current source (§0.4C froze exactly this topology at
        b0cee252, CanonicalPathArchitectureTests APP_PATH-006/007 green).
    §0.4 "TAC-M4 = IN PROGRESS" vs §0.4C supersede
      → explained historical layering; current state = FROZEN topology.
    CERT-V2-011 / TAC-RAP-IA / TAC-M4 COMPLETE
      → OPEN by design (Invariant K exit deferred) = Wave 4 re-evaluation.
    Wave 2 rename EnsureWorkspaceInviteeMembershipAsync →
        EnsureAccountMembershipAsync (DONE this remediation: producer-language
        Public surface; caller AcceptInvitation, tests, SPEC IA-FLOW-05
        EntryPoint updated; BOUND-TX-002 text, idempotency/no-op, grant
        projection unchanged)

M5  Workspace & Governance
    WG-FLOW-01/02/03 = VERIFIED (runtime evidence).
    WG-FLOW-04/05/06 = BLOCKED — decision hold, NOT source-missing:
      the Governance permission mechanism IS implemented and characterized
      (GrantResourcePermission/Get/Revoke + AccessPolicyEngine +
      GovernanceResourcePermissionFlowTests, incl. the M7-frozen
      ArchivePage ≠ ManagePagePermission ladder). The hold is the SPEC-level
      page-ACL model decision.
      → AUTHORITY-DRIFT with explicit explanation → Wave 5 reviews the
        actual source semantics and either freezes the decision (SPEC flow
        card + TESTS + CERT + STATUS) or fixes source first.
      RESOLVED by Wave 5: source semantics reviewed correct → frozen as the
      M5 Wave 5 Page ACL decision; the single proof gap (revoke
      authorization, zero tests) closed with 6 new integration tests
      (TESTS 41D2); WG-FLOW-04/05/06 VERIFIED.

M6  Work Management
    FROZEN at ad279ac4 (PR #124). WM-FLOW-05 recorded "VERIFIED" covering
    the projection chain — true for SEMANTICS + producer authority at M6;
    the final implementation ownership shape (Analytics port → delegate
    adapter → WorkManagement Public) landed only at M10.
    → HISTORICAL-CORRECTION → Wave 6 adds the superseding record; no history
      rewrite, PR #124 untouched.

M7  Documents & Collaboration
    FROZEN at c28e319c (PR #126) after 3 audit passes. ACCEPTED-DEBT:
    M7-DC-MENTION-TARGET-VALIDITY → Wave 8 decision queue (mentionable
    identity semantics); M7-FRONTEND-CONTRACT-SCHEMA-STALE +
    M7-FRONTEND-MENTION-ADAPTER-REACHABILITY are frontend-workspace follow-
    ups, out of backend remediation scope, recorded not re-opened.

M8  Automation & Integrations
    FROZEN — VERIFIED at 446ecacb. ACCEPTED-DEBT (Wave 8 disposition, not
    production defects): M8-AUTOMATION-TRIGGER-PARITY (no synthetic
    producers until real lifecycle exists), M8-CALENDAR-PROVIDER-
    VERIFICATION (real SDK only with real credentials),
    M8-WEBHOOK-RECEIPT-RETENTION (freeze retention/owner/audit first).

M9  Billing & Entitlements
    FROZEN at closure candidate 3bff55b0 (PR #132) — closure wave
    SUPERSEDES the earlier overclaimed verdicts (recorded honestly).
    ACCEPTED-DEBT: M9-CAPACITY-RELEASE-LIFECYCLE — ReleaseAsync exists but
    no delete/archive AutomationRule lifecycle calls it; disable does not
    release. → Wave 8; semantics must not be changed implicitly.
    Container-CI red at 3bff55b0 = base-image Trivy findings, tracked as
    infra Part B, NOT a TAC-FRZ-020 regression.

M10 Analytics & Reporting
    CLOSED-FROZEN @ b341d4ae (28 checks / 0 failed), merged 25dbb34b.
    → CLOSED (Wave 7 exact-head record normalized above).
```

### W0.2 Exit check

```text
No STATUS item is simultaneously "BLOCKED" here and "IMPLEMENTED" in source
without explanation:
  WG-FLOW-04/05/06 BLOCKED → explained (semantic decision hold; mechanism
    implemented and characterized; resolved by Wave 5, not silently).
  IA-FLOW-02/04/05 relocation language → resolved as DOC_STALE (topology
    exists; §0.4C supersedes).
  §0.3 M3 IN-PROGRESS language → superseded by this wave's classification.
PR/CI §0.6 snapshot (PR #113 / 84b04ed, frontend reds) = historical layer per
the file's CURRENT-STATE PRECEDENCE header; current integrated truth is
develop 25dbb34b with M10 merged.
```

## Residual debt remediation — Wave 1–5 execution record

Branch `architecture/residual-debt-remediation` (base develop `25dbb34b`).

### Wave 1 — Identity session ownership (commit 0954ddf9)

```text
Moved: IAuthSessionIssuer + AuthSessionIssuer + IJwtService
  Application/Common/Security/Auth → Application/Features/Identity/Auth/Sessions
  (IJwtService moves with the pair: its ONLY consumers are the session issuer
   and Identity RefreshToken; the TAC-GATE-022 entry
   "IJwtService -> Domain User" is the same acceptance condition.)
Kept in Common (technical, gate-clean): AuthResult + UserDto neutral DTO
  records, IJwtBlacklistService, ICookieService, IOtpService, IPasswordHasher,
  PasswordPolicy.
TAC-GATE-022 baseline: 5 entries → 1 (only DEBT-COMMON-001
  IRequirePermission -> PermissionAction remains, targeted by Wave 3/ADR-007
  retained-exception disposition).
Preserved unchanged: JWT/refresh format, UserSession model, expiry semantics,
  login/register/OAuth behavior, transaction semantics, HTTP contract.
Evidence: build 17/0; Architecture 608 (shrink-on-move gate proves the pair);
  Application 720; API 264; Integration auth/registration/session filter 162;
  format clean.
```

### Wave 2 — Accounts producer language (commit 528ccdde)

```text
EnsureWorkspaceInviteeMembershipAsync → EnsureAccountMembershipAsync on the
  Accounts Public seam (interface + impl + AcceptInvitation caller + all
  tests/mocks + SPEC IA-FLOW-05 EntryPoint + STATUS wave-0 row).
  invitedBy parameter KEPT: it maps to AccountMember.Create provenance
  (Accounts aggregate language), not consumer vocabulary.
Rename only. BOUND-TX-002 shared-transaction behavior, admission checks,
  idempotent no-op, grant projection: unchanged.
Evidence: build 17/0; Application focused 27 (AccountMembershipActionsTests +
  AcceptInvitation suites); AccountMembershipTransactionEvidenceTests 4 on real
  PostgreSQL; Architecture 608 (HandlerDataPortGate + CanonicalPath included).
```

### Wave 3 — Governance/Common authorization split (ADR-007)

Step 3.1 dependency map (classification):

```text
PermissionAction                 GOVERNANCE-SEMANTIC   owner=Domain.Governance (stays)
AccessPolicyEngine               GOVERNANCE-SEMANTIC   was in Common → MOVED
AccessControlBehavior            TECHNICAL stage       stays Common
IAccessPolicyEvaluator           EXECUTION seam        stays Common (neutral)
AccessFacts / AccessDecision /   PRODUCER-FACT         stays Common (role strings are
AccessPermissionRule                                     data, ARCH-BC-008 precedent)
RequestDescriptor(+Registry)     EXECUTION-DESCRIPTOR  stays Common
IRequirePermission               GOVERNANCE-SEMANTIC marker — RETAINED governed
                                   exception DEBT-COMMON-001 (acceptance branch:
                                   "retained exception explicitly accepted";
                                   removal requires the governed marker migration)
IRequireGrantPermission /
IRequireRevokePermission /
IRequirePermissionTarget         TECHNICAL primitives (int rank/Guid/string?) —
                                   stays Common (TAC-M3C precedent, exact list)
IAccessFactsProvider             PRODUCER-FACT port    stays Common
```

Step 3.2/3.3 decision + implementation:

```text
AccessPolicyEngine relocated Common/Security →
Features/Governance/Authorization. Pure move — no rule/order/message change.
One evaluator, one pipeline stage, same singleton DI binding
(IAccessPolicyEvaluator → AccessPolicyEngine in AddApplicationServices).
Dependency direction: Governance engine → Common mechanics + Domain.Governance;
Common references NO Features.Governance type (verified by new lock gate).
No cycle; ADR-006 seven-behavior order untouched.
```

New executable locks (`AuthPipelineArchitectureTests`, ADR-007):

```text
AccessPolicyEngine_IsGovernanceOwned
EvaluatorSeam_HasExactlyOneImplementation        (second evaluator = fail)
Common_MustNotDepend_On_GovernanceApplicationTypes (signature-graph scan)
AccessControlBehavior_ReferencesOnlyTheSeam_NotTheConcreteEngine
AuthorizationPolicyEngine_MustNotReadPersistence   (path updated, content checks unchanged)
```

Documentation updates (this commit): ADR-007 (new, registered in
backend/docs/decisions/README.md incl. the previously missing ADR-006 row),
security-tenancy-authorization.md §11 + BE-SEC-013 canonical chain,
application-model.md §14 pipeline description, ARCH-BC-008 header comment.

Stop-condition audit: none triggered (pipeline order/count identical, single
evaluator proven, zero behavior change — characterization suite green with
engine moved, session/JWT untouched, BOUND-TX unchanged, no migration).

Evidence: build 17/0; Architecture 612 (608 + 4 new locks); Application 720;
API 264; behaviors/pipeline/policy-matrix 54; Integration filter
Governance|Authorization|Automation|Calendar|Pipeline|Webhook|Billing|
Production = 139; format whitespace+style+analyzers exit 0.

### Wave 4 — M4 certification re-run at post-Wave-1–3 HEAD

Re-executed exact CERT-V2-011 evidence on this integrated tree (not the old
b0cee252 snapshot):

```text
IA-FLOW-01 UpdateProfile        Local            Application UpdateProfile tests   PASS
IA-FLOW-02 Register→Accounts    TAC-XC-E pinned  BOUND-TX-004 provisioning evidence
                                                + CompleteOAuthLogin suites         PASS
IA-FLOW-03 Reg→Workspace event  TAC-XC-C pinned  RegistrationWorkspaceRuntimeChain
                                                + DeduplicationConsumeFilter incl.
                                                0.4B failure-flush regression        PASS
IA-FLOW-04 membership facts     TAC-XC-A pinned  membership facts + transaction      PASS
IA-FLOW-05 membership action    TAC-XC-E pinned  AcceptInvitation matrix +
                                                AccountMembership evidence (post
                                                producer-language rename)           PASS
IA-FLOW-06 RenameAccount        Local            integration role-ladder set        PASS

Accounts/Public capability-first topology:
  Membership/ (Facts + AdmissionFact + Actions)  ✓  no legacy technical buckets
  PersonalAccountProvisioning/ (Actions + Result) ✓
APP_PATH-004/006/007 + HandlerDataPortGate same-team separation: PASS (Architecture 612)
BOUND-TX-002 / BOUND-TX-004 texts + runtime semantics: unchanged since §0.4C freeze.
Battery totals this wave: Application focused 40, Integration focused 55 (real
PostgreSQL), Architecture 612, all 0 failed.
```

Verdict:

```text
All explicitly enumerated CERT-V2-011 criteria now PASS at this HEAD.
TAC-M4 remains FROZEN — NOT CLAIMED COMPLETE. Reason: the §0.4C freeze withholds
completion pending an "Invariant K exit" whose definition does not exist in any
current authority (SPEC/PLAN/TESTS/CERT — searched; git history — absent).
Class: UNRESOLVED decision (AGENTS §12). Recorded for owner resolution, NOT
worked around: no wording was changed to force closure, and no exit was
invented. Once the owner defines or retires the Invariant-K exit, CERT-V2-011
promotion is a records-only follow-up (evidence already re-run green here).
Same treatment applies to the TAC-RAP-IA row in the §124 pack table (left
unfilled deliberately).
```

### Wave 5 — M5 page-ACL contradiction resolved (AUTHORITY-DRIFT → VERIFIED)

```text
Root conflict: SPEC/STATUS recorded WG-FLOW-04/05/06 as BLOCKED
  (STOP-AUTHZ-SEMANTIC "page-auth decision hold"), yet the Governance
  grant/read/revoke mechanism + the M7-frozen ArchivePage≠ManagePagePermission
  ladder are fully implemented and characterized in source.
Classification: AUTHORITY-DRIFT — stale authority, correct source. Resolution
  path was to FREEZE the reviewed source semantics, not to re-derive them.

Reviewed source semantics (now pinned as the M5 Wave 5 decision in the SPEC
  WG-FLOW-04 card + §WG-FLOW-04 summary):
    ACL management authority (grant/get/revoke on documents.page rows) =
      workspace Owner OR active page ResourcePermission rank >= Manager;
      explicit Allow/Deny rules evaluate deny-first through the one evaluator.
    grant ceiling   = max(requested, target existing rank); semantic upsert.
    revoke ceiling  = target existing rank; revoke soft-deletes the row;
      subject-of-a-row is NOT management authority.
    visibility/audience ≠ ACL-management authority ≠ lifecycle authority;
      ArchivePage keeps its own M7 Manager-rank ladder (untouched).
    cross-account revoke target hidden = NotFound, foreign row untouched.

Proof matrix: 11/12 rows already VERIFIED by 41D/41E/41F + ADR-007 locks;
  the ONE gap — Revoke authorization had a correct production implementation
  but ZERO integration evidence — was closed with 6 new tests (TESTS 41D2):
    Revoke_OnPage_ByOwner_RemovesActiveRowAndAudits
    Revoke_PageManagerRank_RevokeEqualOrLowerTargetRank_Allows
    Revoke_AuthorityBelowTargetRank_IsForbidden_AndRowStaysActive
    Revoke_SubjectWithoutManagementAuthority_IsForbidden_EvenOnOwnRankRow
    Revoke_UnknownPermission_IsNotFound
    Revoke_ForeignAccountTarget_IsNotFound_AndForeignRowStaysActive
  Denials assert the row stays active (no partial mutation); the owner path
  asserts the soft-delete + audit. No semantics were invented — only proven.

Exit: WG-FLOW-04 = VERIFIED, WG-FLOW-05 = VERIFIED, WG-FLOW-06 = VERIFIED
  (SPEC card + SPEC summary + TESTS 41D2 + CERT §122AA rows + this STATUS
  section all updated in the same change).
Evidence: build 17/0; GovernanceResourcePermissionFlowTests 51/51 (45 prior +
  6 revoke) on real PostgreSQL; no engine/DI change, so Architecture 612 /
  API 264 remain valid.
Scope guard: no M7 ArchivePage reopening, no PermissionAction change, no
  second evaluator, no RLS/tenant change.
```

## Residual debt remediation — Wave 8 decision queue (no code)

These are **accepted deferred decisions**, not production defects. Each row is
the explicit disposition so the debt is never silently re-read as an open bug.
No code is implemented here; each requires a prior owner/product decision.

```text
M7  mentionable identity
    Question (must be answered by product/domain owner before any code):
      Who is mentionable in a comment —
        (a) Workspace member only,
        (b) Account member, or
        (c) any authenticated Identity user?
    Current source: CreateComment accepts an explicit non-empty user-id list
      with NO authoritative membership validation (M7-DC-MENTION-TARGET-VALIDITY).
    Disposition: DEFERRED-BY-DECISION. Only after the answer is frozen design
      the validation seam (which BC owns the membership fact, which
      TAC-XC mechanism supplies it). Do NOT invent a validation seam to close
      this queue item. Debt stays registered; not a production defect.

M8  trigger parity
    Current source: only 3 runtime-reachable Work facts are wired to triggers
      (ItemAssigned, ItemMovedToGroup, ItemCreated). ItemUpdated, ItemDeleted,
      FormSubmitted, FieldChanged, ScheduleTrigger remain configuration
      vocabulary with no producer event (M8-AUTOMATION-TRIGGER-PARITY).
    Disposition: IMPLEMENT-ON-PRODUCER-EXISTENCE. A trigger is wired only when
      its producer event/business lifecycle actually exists. No synthetic
      producers. Debt stays registered; not a defect.

M8  provider verification
    Current source: calendar webhook HMAC verifier uses per-provider shared
      secrets from configuration (M8-CALENDAR-PROVIDER-VERIFICATION).
    Disposition: UPGRADE-ON-REAL-PROVIDER-CONTRACT. Replace the generic
      verifier with the real provider SDK asymmetric verification only when
      real provider credentials/contract exist. Until then the generic verifier
      is the correct frozen behavior, not a defect.

M8  webhook receipt retention
    Current source: rejected diagnostic receipts (status Rejected, synthetic
      rejected:* event ids) accumulate with no retention/cleanup policy
      (M8-WEBHOOK-RECEIPT-RETENTION). Pre-auth IP rate limit bounds growth.
    Disposition: FREEZE-POLICY-BEFORE-CLEANUP. Freeze, in order: retention
      duration, cleanup owner, audit requirement, legal/diagnostic requirement,
      before implementing any cleanup job. Bounded-diagnostics is a follow-up
      lifecycle decision, explicitly not an M8 gate. Not a defect.

M9  release lifecycle
    Current source: BillingCapacityActions.ReleaseAsync is implemented but the
      AutomationRule lifecycle that would invoke it (delete/archive) does not
      exist; disable (SetAutomationRuleEnabled) and enable do NOT release /
      re-reserve the AUTOMATION_RULE slot (a Disabled rule still occupies it)
      (M9-CAPACITY-RELEASE-LIFECYCLE).
    Disposition: WIRE-ON-AUTHORITATIVE-LIFECYCLE. Connect ReleaseAsync only
      when AutomationRule gains an authoritative delete/archive/equivalent
      lifecycle. Do NOT change the "disable keeps the slot" semantic implicitly
      and do NOT invent a lifecycle to force the wiring. Protected as
      NOT-APPLICABLE-UNTIL-LIFECYCLE-EXISTS; not a defect.

M4  CERT-V2-011 / TAC-M4 COMPLETE exit
    Current source: all enumerated CERT-V2-011 criteria PASS at this HEAD
      (IA-FLOW-01..06 re-run green; Accounts/Public capability-first topology
      present; APP_PATH-004/006/007 + same-team gates green).
    Blocker: the §0.4C freeze withholds COMPLETE pending an "Invariant K exit"
      that is defined in NO current authority (SPEC/PLAN/TESTS/CERT and git
      history searched) — an UNRESOLVED decision, not a source defect.
    Disposition: OWNER-RESOLVE. The owner must define or retire the
      Invariant-K exit. Once resolved, CERT-V2-011 promotion is records-only
      (evidence already re-run green). No wording was changed to force closure.
```

## Slice evidence log

(append per completed slice — see below)

### FRZ-017 — Common grant-projection seam owner relocation

Baseline:

```text
branch: architecture/backend-boundary-execution
HEAD: b94312b3e10e2212f18440003e775d415aa1b5d7
```

Ownership:

```text
Accounts/Members owns IAccountGrantProjectionService + AccountRole vocabulary
Workspaces/Members owns IWorkspaceGrantProjectionService + WorkspaceRole vocabulary
Infrastructure/Data/Authz owns the technical access_grants projection mechanism
```

Change:

```text
Removed Application/Common/Tenancy/IAccessGrantProjectionService.
Added owner-local Application seams for Account and Workspace grant projection.
Added Infrastructure adapters that delegate to AccessGrantProjectionService.
Migrated Accounts/Workspaces callers and DI registrations to the owner-local seams.
Added TAC-GATE-022 architecture gate for Common public-signature purity.
```

Evidence:

```text
dotnet build backend.slnx: 17 projects, 0 errors
Notrelix.Domain.Tests: 2581 passed
Notrelix.Application.Tests: 609 passed
Notrelix.Infrastructure.Tests: 134 passed
Notrelix.Platform.Tests: 147 passed
Notrelix.API.Tests: 256 passed
Notrelix.Architecture.Tests: 479 passed
Focused Integration.Tests:
- AccessGrantProjectionTests: 7 passed
- CreateWorkspaceCommandHandlerTests: 3 passed
- AccountMembershipTransactionEvidenceTests + RegisterCommandHandlerTests: 7 passed
- WorkspaceCreationPipelineAuthorizationTests: 5 passed
- RlsRuntimeEnforcementTests: 15 passed
- CompleteOAuthLoginCommandHandlerTests: 10 passed
- ExpectedVersionConcurrencyIntegrationTests: 8 passed
- WorkspaceLifecycleTests: 3 passed
- PipelineTelemetryIntegrationTests: 4 passed
- RegisterDuplicateRaceTests: 1 passed
```

Remaining debt:

```text
TAC-GATE-022 baselines exact current Common signature debt:
- IRequirePermission -> PermissionAction
- AuthSessionIssuer/IAuthSessionIssuer/IJwtService -> Identity User
- AuthSessionIssuer -> Identity IIdentityDbContext
The FRZ-017 grant-projection role leak is closed; these remaining entries are
separate Common purity debt and must shrink only through their own governed
migration.
```

### FRZ-018 — Scoped integration-event tenant envelope

Baseline:

```text
branch: architecture/backend-boundary-execution
HEAD: b94312b3e10e2212f18440003e775d415aa1b5d7
```

Ownership:

```text
Each producer context owns its outward integration-event contract.
Application/Common owns only the technical tenant-scope classification attribute.
Infrastructure/Messaging owns TenantContextConsumeFilter runtime tenant restoration.
```

Change:

```text
Added IntegrationEventTenantScopeAttribute and classified every registered public integration event as Workspace, Account, or None.
Added required AccountId envelope parameters to Workspace-scoped event contracts.
Added required AccountId envelope parameters to Billing Account-scoped event contracts.
Updated producer mappers to pass domainEvent.AccountId instead of dropping the account envelope.
Regenerated backend/contracts/events/notrelix.events.json from the canonical generator.
Added TAC-GATE-023 architecture gate for scoped-event tenant envelope purity.
Added Infrastructure runtime tests proving TenantContextConsumeFilter restores Workspace tenant for representative WorkspaceMemberAdded, BoardItemMoved, PageCreated, and CommentCreated events.
```

Evidence:

```text
dotnet build backend.slnx: 17 projects, 0 errors
Notrelix.Domain.Tests: 2581 passed
Notrelix.Application.Tests: 609 passed
Notrelix.Infrastructure.Tests: 138 passed
Notrelix.Platform.Tests: 147 passed
Notrelix.API.Tests: 256 passed
Notrelix.Architecture.Tests: 486 passed
Notrelix.Integration.Tests full suite: 348 passed
Focused Infrastructure tenant-filter tests: 4 passed
```

Remaining debt:

```text
Closed by the FRZ-018 registration-event classification correction below.
```

### FRZ-018 correction — Identity registration event semantic classification

Baseline:

```text
branch: architecture/backend-boundary-execution
HEAD: b94312b3e10e2212f18440003e775d415aa1b5d7
```

Ownership:

```text
Identity owns UserRegistered/UserDeactivated lifecycle facts.
Identity owns IdentityRegistrationCompleted as the canonical registration workflow fact after successful Accounts provisioning.
Workspaces owns personal Workspace provisioning triggered by IdentityRegistrationCompleted.
Infrastructure/Messaging owns runtime tenant restoration and consumer delivery.
```

Classification before/after:

```text
UserRegisteredIntegrationEvent
  before: Account with optional/null AccountId
  after: None with no AccountId envelope parameter

UserDeactivatedIntegrationEvent
  before: Account with optional/null AccountId
  after: None with no AccountId envelope parameter

IdentityRegistrationCompletedIntegrationEventV1
  before: Account with required nullable AccountId
  after: Account with required nullable AccountId plus runtime non-empty guard
```

Change:

```text
Reclassified UserRegistered and UserDeactivated as TenantScope.None.
Removed the optional AccountId envelope parameters added during the first FRZ-018 slice.
Added IntegrationEvent requireAccountId guard support and enabled it for IdentityRegistrationCompleted.
Updated UserRegistered PII justification to reflect the actual non-tenant telemetry consumer.
Reduced UserRegisteredConsumer logging to UserId only.
Updated TAC-GATE-023 to remove exact-debt allowlists and enforce required Account/Workspace envelopes.
Added tenantScope to the generated event manifest and bumped the manifest artifact schemaVersion to 2.
Added runtime tests for Account tenant restoration from IdentityRegistrationCompleted and System tenant behavior for None registration/lifecycle events.
Added consumer tests proving Workspace provisioning and welcome delivery run from IdentityRegistrationCompleted, not UserRegistered.
Added producer tests proving Register and CompleteOAuthLogin emit IdentityRegistrationCompleted only after successful Account provisioning.
```

Evidence:

```text
dotnet format backend.slnx --verify-no-changes --no-restore: clean
dotnet build backend.slnx: 17 projects, 0 errors
dotnet test backend.slnx --no-build: 4579 passed across 7 projects
Focused Application registration/event tests: 7 passed
Focused Infrastructure tenant-filter/consumer tests: 9 passed
Focused Architecture scoped-event/manifest tests: 13 passed
Focused Integration CompleteOAuthLoginCommandHandlerTests: 11 passed
Focused Infrastructure IntegrationEventCatalogResolutionTests: 7 passed
```

Remaining FRZ-018 debt:

```text
None for registration-event classification.
TAC-GATE-023 no longer contains exact-debt allowlists for UserRegistered or UserDeactivated.
```

### M1 local Flow Card register

Baseline:

```text
branch: architecture/backend-boundary-execution
HEAD before source closure: c9c906e4741a9e900ff7c0bf30f53b804ba23b02
```

M1 state:

```text
COMPLETE
```

Change:

```text
Materialized 47 explicit Flow Cards in the local v2.6 SPEC under section 25A.
Each card contains the mandatory Flow Card schema fields.
Cards ground current reality and frozen target contracts without creating new local docs files.
M1 structural validator confirms 47 unique IDs, expected team counts, no missing fields, and no forbidden placeholders.
M1 semantic audit found mass-template values and false/generic evidence claims, so structural presence was not sufficient for M1 COMPLETE.
Correction wave normalized all 47 cards against current source at dd4643ef3ef6efa959da33ec4e07a2bf5467be0f, replacing generic mechanism/test/gate/path/evidence values with source-grounded values.
```

Evidence:

```text
M1 structural validator:
count: 47
unique: 47
missing FlowIds: []
unexpected FlowIds: []
duplicate FlowIds: []
missing mandatory fields: []
forbidden placeholders: []

M1 semantic validator before correction:
generic/template violations remaining: 47 cards with at least one generic/template value
source/evidence reference violations: WM-FLOW-01 ApplicationPath points to a non-existent CreateBoard.cs file
status: PARTIAL

M1 semantic validator after correction:
count: 47
unique: 47
missing FlowIds: []
unexpected FlowIds: []
duplicate FlowIds: []
missing mandatory fields: []
semantic/template violations: []
source/evidence reference violations: []
status: COMPLETE
```

### FRZ-018 runtime declared-scope closure

Baseline:

```text
branch: architecture/backend-boundary-execution
HEAD before source closure: c9c906e4741a9e900ff7c0bf30f53b804ba23b02
```

Change:

```text
TenantContextConsumeFilter now reads IntegrationEventTenantScopeAttribute as runtime authority.
Workspace events require non-empty AccountId and WorkspaceId before next.Send.
Account events require non-empty AccountId before next.Send.
None events explicitly run under System context.
Missing tenant-scope classification fails closed for IIntegrationEvent messages.
Added IntegrationEventTenantEnvelopeException derived from ArgumentException so deterministic tenant-envelope violations are not retried as transient failures.
Added negative runtime tests for Workspace and Account invalid envelopes, None explicit behavior, and missing classification.
Added replay compatibility tests proving an old Workspace payload with accountId null fails closed before consumer execution while valid replay restores Workspace tenant.
Added WorkspaceMemberAddedScopedTenantRuntimeChainIntegrationTests proving the required production runtime-owner chain: WorkspaceMember mutation -> DomainEventInterceptor/outbox -> OutboxDispatcher -> IntegrationEventBus -> real MassTransit InMemory receive pipeline -> TenantContextConsumeFilter -> DeduplicationConsumeFilter -> real WorkspaceMemberAddedActivityConsumer -> Collaboration activity projection.
```

Evidence:

```text
dotnet format backend.slnx --verify-no-changes --no-restore: clean
dotnet build backend.slnx: 17 projects, 0 errors
dotnet test backend.slnx --no-build: 4592 passed across 7 projects
Focused Infrastructure scoped-event/filter/replay/catalog tests: 26 passed
Focused Architecture scoped-event gate tests: 12 passed
Focused Integration workspace membership outbox and full scoped tenant runtime-chain tests: 3 passed
```

FRZ-018 state:

```text
FROZEN
```

Runtime-owner proof:

```text
WorkspaceMemberAddedScopedTenantRuntimeChainIntegrationTests
asserts outbox AccountId/WorkspaceId, dispatcher completion, installed tenant/dedup filters,
real activity consumer side effect exactly once, consumer SaveChanges under expected
Account/Workspace tenant, non-System context, and tenant cleanup after consume completion.
```

HISTORICAL correction-wave status (superseded by §0 current authoritative status):

```text
M0 = COMPLETE
M1 = COMPLETE
M2 = COMPLETE

M3 = IN PROGRESS
FRZ-017 = PARTIAL
FRZ-018 = FROZEN
FRZ-019 = NOT STARTED
FRZ-020 = FROZEN (M9)
```

Correction wave closure:

```text
baseline HEAD: dd4643ef3ef6efa959da33ec4e07a2bf5467be0f
final HEAD: 73ac430547187c1538c8643296e3be285720d64c
commit: test(messaging): prove scoped tenant through production delivery chain
local TAC docs: uncommitted working records only
M1: 47/47 structurally complete and source-grounded; semantic validator clean
FRZ-018: production runtime-chain proof added and passing
CI exact HEAD: CodeQL, Documentation CI, Backend CI, Container CI, Frontend CI, CI Definition, Infrastructure CI all success
```

Remaining M3 state:

```text
FRZ-017 = PARTIAL
FRZ-019 = NOT STARTED
FRZ-020 = NOT STARTED
```

## Adopted certification invariants (A–L)

User-supplied execution/certification invariants adopted by this working record. They govern
every future PASS / COMPLETE / FROZEN / VERIFIED claim in this workstream.

### A — Working-tree / exact-SHA integrity

```text
PASS / COMPLETE / FROZEN / VERIFIED require ALL of:
- certification-relevant working tree clean
- all source/test/gate changes committed
- candidate SHA == git HEAD
- tests executed against that exact committed source

Uncommitted implementation is at most IMPLEMENTED-UNCOMMITTED.
Never associate tests executed against dirty source with a previous clean HEAD SHA.
After the final implementation commit, rerun required certification regression against the new exact HEAD.
```

### B — Gate state vs final freeze state (independent tracking)

```text
TAC-GATE-022 = PASS  does NOT imply TAC-FRZ-017 = FROZEN
TAC-GATE-023 = PASS  does NOT imply TAC-FRZ-018 = FROZEN (FROZEN additionally requires production runtime-chain proof)
TAC-GATE-024 = PASS  does NOT imply TAC-FRZ-019 = FROZEN (Calendar freeze belongs to M8)
TAC-GATE-025 = PASS  does NOT imply TAC-FRZ-020 = FROZEN (Billing freeze belongs to M9)

TAC-FRZ-017 FROZEN additionally requires:
- Application/Common public signatures contain no BC business vocabulary
- canonical AccountRole/WorkspaceRole leakage removed
- IA/WG runtime paths prove owner-preserving semantics
Governed debt may remain during M3 no-growth hardening only if explicitly recorded (invariant E).

TAC-FRZ-019 FROZEN (M8) requires: TAC-GATE-024 + AI-FLOW-05 DB round-trip +
AI-FLOW-06 CAL-CONN-001 + AI-FLOW-07 verified provider intake + cross-pack Calendar runtime.

TAC-FRZ-020 FROZEN (M9) requires: BILL-LIMIT-001 + TAC-GATE-025 +
BI-FLOW-02 last-slot concurrency + BI-FLOW-03 concurrent CreateAutomationRule +
BI-FLOW-04 lifecycle result + cross-pack Billing last-slot proof.

EVIDENCE LANDED (Wave 3 closure, branch architecture/billing-entitlements):
- BILL-LIMIT-001: resolved (M2 decision) — effective limit = capability fact;
  zero/unlimited semantics frozen in TAC-GATE-025 (BillingCapacitySemanticsArchitectureTests)
- TAC-GATE-025: 12 arch self-tests still green (Architecture.Tests 600)
- BI-FLOW-02: direct last-slot race + first-use race + ledger-SUM materialization +
  global index rejection (BillingCapacityFlowIntegrationTests 8, real PostgreSQL)
- BI-FLOW-03: concurrent CreateAutomationRule → capacity, one capacity-consuming
  rule, deterministic id conflict (CreateAutomationRuleBillingGateTests 5)
- BI-FLOW-04: capability after real capacity mutation through production pipeline
- cross-pack last-slot: CreateAutomationRuleBillingGateTests concurrent rule +
  capacity race evidence; see M9-CLOSURE closure evidence above
=> TAC-FRZ-020 = FROZEN (M9)
```

### C — TAC-GATE-024 exact semantics (M3J, Calendar)

Gate applies to Calendar production implementation. Rejects production Calendar flow that:

```text
creates Calendar capability without CalendarIntegration
uses CalendarIntegration without ConnectionId
treats CurrentSecretRef alone as durable persistence
uses outbound WebhookDelivery as inbound provider receipt
uses user-session authentication as sole provider authenticity
trusts tenant scope from unverified provider payload
```

Expected future production concepts: `IntegrationConnection`, `IntegrationSecretVersion`,
`CalendarIntegration`, Infrastructure inbound receipt/dedup.
A missing/NotImplemented Calendar product flow is an M8 implementation gap — never marked VERIFIED,
and does not authorize pulling full AI-FLOW-05/06/07 implementation into M3.

### D — TAC-GATE-025 exact semantics (M3K, Billing)

Gate rejects source/architecture where:

```text
AUTOMATION_RULE is classified as billable UsageMetric without product authority
numeric zero is treated as unlimited with no governing BILL-LIMIT-001 semantic decision
a hard-capacity owner action exists without an explicit concurrency strategy / owner state
a consumer branches on private PlanTier
```

Frozen M2 authority remains `BILL-LIMIT-001 = ZERO_CAPACITY`:

```text
Limit = 0 && !IsUnlimited → zero capacity
IsUnlimited = true  → unlimited
```

M3K is not justification to implement BI-FLOW-01..04; M9 owns production
migration/capacity/runtime concurrency closure.

### E — Exact debt schema

Every retained architecture debt must record:

```text
DebtId:
Rule:
ExactTypeOrPath:
SemanticOwner:
CurrentConsumer:
WhyRetained:
Risk:
NoGrowthGate:
MigrationTrigger:
RemovalTrigger:
Tests:
AcceptedAsPrecedent: NO
```

Rules: no wildcard debt; no broad folder exception when an exact type/path can be recorded;
no retained debt becomes precedent; removed debt must shrink the baseline in the same change.

### F — M3 required evidence identities

M3 traceability must resolve `TAC-GATE-022`, `TAC-GATE-023`, `TAC-GATE-024`, `TAC-GATE-025`
and preserve/harden the foundation rules owned by M3:

```text
ARCH-BC-005
STN-ARCH-006
ARCH-BC-008
ARCH-BC-006
```

No replacement gate IDs for equivalent rules unless normative documents are intentionally revised.

### G — IA-FLOW-06 authorization rule

Authorization goes through the canonical existing authorization stack:

```text
authenticated Account-scoped request
→ canonical request authorization pipeline
→ Governance-owned Account-admin semantic
→ Accounts Application
→ Account.Rename
→ Accounts persistence
```

Forbidden (unless the use case genuinely requires a separately governed fact): new authorization
evaluator, new decision store, second authorization Behavior stack, handler-local Governance
architecture, manual Governance query introduced merely to authorize RenameAccount.

### H — M4 exact evidence mapping

```text
IA-FLOW-01 → TAC-IA-FLOW-01
IA-FLOW-02 → TAC-IA-FLOW-02 + BOUND-TX-004 runtime proof
IA-FLOW-03 → TAC-IA-FLOW-03
IA-FLOW-04 → TAC-IA-003 + TAC-IA-004
IA-FLOW-05 → TAC-IA-005 + TAC-IA-006 + TAC-TX-001
IA-FLOW-06 → TAC-IA-FLOW-06
```

No FlowId may be satisfied by a newly invented empty evidence heading. Every evidence heading must
resolve to production source + substantive assertions + runtime/persistence proof where required +
architecture boundary proof.

### I — BOUND-TX-004 mandatory runtime cases (IA-FLOW-02)

```text
success → Identity User + personal Account + owner member all commit
Accounts failure → Identity User rollback
request failure after Accounts mutation → no orphan Account
duplicate registration → no duplicate personal Account
registration event/outbox → emitted only after committed registration
```

Handler-only unit tests do not satisfy BOUND-TX-004. No separate-commit implementation satisfies it.

### J — IA-FLOW-06 concurrency proof

Real Accounts persistence. Mandatory race: two concurrent conflicting RenameAccount updates →
deterministic repository concurrency outcome → no lost update. Also prove:

```text
authorized admin succeeds
unauthorized actor denied before mutation
wrong Account scope rejected
invalid name rejected
same current name follows canonical no-op/idempotent behavior
Accounts persistence only — no Identity/Workspace private persistence
```

### K — M4 certification exit

M4 closes only when IA-FLOW-01..06 are each VERIFIED plus:

```text
Identity local baseline = VERIFIED
Accounts local-admin baseline = VERIFIED
Accounts Public fact/action baseline = VERIFIED
same-team Identity/Accounts separation = VERIFIED
BOUND-TX-004 transaction model = VERIFIED
required IA references resolve to substantive evidence
```

Then evaluate `CERT-V2-011 = PASS`, `TAC-RAP-IA = FROZEN`, `TAC-M4 = COMPLETE`.
Any missing evidence → `TAC-RAP-IA != FROZEN`, `TAC-M4 != COMPLETE`.

### L — State-layer reporting

Final execution reports must distinguish all state layers and never collapse
gate PASS / flow VERIFIED / freeze FROZEN / pack FROZEN / milestone COMPLETE into one generic "done".

## Exact retained debt register (invariant E)

Consumers grounded by source inspection at HEAD `73ac430547187c1538c8643296e3be285720d64c`.

```text
DebtId: DEBT-COMMON-001
Rule: ARCH-BC-005 / TAC-GATE-022 — Common public-signature semantic purity
ExactTypeOrPath: backend/src/Notrelix.Application/Common/Requests/Security/IRequirePermission.cs
SemanticOwner: Governance owns permission-action vocabulary; Identity/Workspaces own scope requests
CurrentConsumer: AccessControlBehavior.cs (pipeline authorization); RequestDescriptorValidator.cs;
  ~70 protected request records across Application Features (Workspaces, Documents, Analytics,
  Automation, Integrations, Governance, Identity) implementing IRequirePermission.Action
WhyRetained: permission action vocabulary is consumed by the canonical authorization pipeline;
  removing it requires an owner-local permission seam + full request-record migration
Risk: Governance vocabulary evolution is visible through a Common seam; no runtime defect today
NoGrowthGate: TAC-GATE-022 (CommonPublicSignaturePurityArchitectureTests)
MigrationTrigger: FRZ-017 completion (IA/WG runtime owner-preserving proofs) or Governance
  seam extraction
RemovalTrigger: IRequirePermission removed from Common; permission seam owned by its context;
  baseline shrinks in same change
Tests: TAC-GATE-022 architecture gate (12 scoped-event-gate suite + full Architecture.Tests)
AcceptedAsPrecedent: NO
```

```text
DebtId: DEBT-COMMON-002
Rule: ARCH-BC-005 / TAC-GATE-022 — Common public-signature semantic purity
ExactTypeOrPath: backend/src/Notrelix.Application/Common/Security/Auth/IAuthSessionIssuer.cs;
  backend/src/Notrelix.Application/Common/Security/Auth/AuthSessionIssuer.cs;
  backend/src/Notrelix.Application/Common/Security/Auth/IJwtService.cs
SemanticOwner: Identity owns User lifecycle and session issuance; JWT mechanics are Infrastructure
CurrentConsumer: AuthSessionIssuer.IssueAsync(User user, ...) signature binds Common to Identity
  User (AuthSessionIssuer.cs:24; passes User to JwtService.GenerateAccessToken at line 33);
  handlers: RegisterCommandHandler, CompleteOAuthLoginCommandHandler, Login, CompleteMfaChallenge,
  RefreshToken (IJwtService); DI: Application/DependencyInjection.cs:90 (IAuthSessionIssuer),
  Infrastructure/AuthRegistration.cs:24 (IJwtService -> Infrastructure JwtService)
WhyRetained: session issuance is shared auth plumbing used by every authenticated Identity flow;
  relocation requires an Identity-owned seam and migration of 5 handler call sites
Risk: Common layer statically depends on Identity domain type User; constrains Identity extraction
NoGrowthGate: TAC-GATE-022 (CommonPublicSignaturePurityArchitectureTests)
MigrationTrigger: FRZ-017 completion or Identity extraction program
RemovalTrigger: session-issuance seam owned by Identity; Common signatures User-free
Tests: TAC-GATE-022 architecture gate (full Architecture.Tests suite green)
AcceptedAsPrecedent: NO
```

```text
DebtId: DEBT-COMMON-003
Rule: ARCH-BC-006 / ARCH-BC-005 — Application transport/persistence purity; Common carries
  Identity persistence mechanism
ExactTypeOrPath: backend/src/Notrelix.Application/Common/Security/Auth/AuthSessionIssuer.cs
  (constructor IIdentityDbContext identityContext; _identityContext.Sessions.Add(session))
SemanticOwner: Identity owns UserSession persistence; Infrastructure owns EF mechanics
CurrentConsumer: AuthSessionIssuer performs direct Identity session persistence from Common
  (AuthSessionIssuer.cs:8,14,31); registered via Application/DependencyInjection.cs:90;
  IIdentityDbContext is implemented by Infrastructure ApplicationDbContext
  (PersistenceRegistration.cs:80)
WhyRetained: refresh/session creation happens inside protected Identity flows that share the
  request transaction; splitting issuance from persistence changes transaction semantics and
  requires an Identity-owned session-issuance port
Risk: Common-located class mutates Identity storage directly; weakens boundary evidence for
  Identity extraction; no tenant-scope defect (sessions are Identity-scoped, RLS-covered)
NoGrowthGate: TAC-GATE-022 + ARCH-BC-006 (ApplicationTransportBoundaryTests)
MigrationTrigger: FRZ-017 completion or Identity extraction program
RemovalTrigger: IIdentityDbContext dependency removed from Common/Security; session issuance
  routed through an Identity-owned port implemented in Infrastructure
Tests: TAC-GATE-022 + ARCH-BC-006 architecture gates (full Architecture.Tests suite green)
AcceptedAsPrecedent: NO
```

### Gate-derived debts (TAC-GATE-024/025 activation, 2026-09-03)

Retained architecture debts discovered while implementing the two M3 gates. Each is
category B (governed existing debt under an exact no-growth baseline), enforced by the
named gate; new violations are rejected, and removal must shrink the gate baseline in
the same change.

```text
DebtId: DEBT-BILL-001
Rule: TAC-GATE-025 / BILL-LIMIT-001 — zero must not be interpreted as unlimited
ExactTypeOrPath:
  backend/src/Notrelix.Application/Features/Billing/Entitlements/Services/BillingCapabilityFactsProvider.cs (L43-44)
  backend/src/Notrelix.Infrastructure/Billing/DatabaseFeatureGateChecker.cs (L30-31)
  backend/src/Notrelix.Infrastructure/Data/Authz/AccessFactsQuery.cs (L67, SQL "e.limit_value = 0 OR ...")
SemanticOwner: Billing owns entitlement semantics; M9 owns the semantic flip
CurrentConsumer: DatabaseFeatureGateChecker + BillingCapabilityFactsProvider (capability
  availability) and AccessFactsQuery (authz pipeline FeatureEnabled fact)
WhyRetained: M2 frozen migration order requires backfill is_unlimited=true WHERE limit=0
  BEFORE the semantic flip; IsUnlimited does not yet exist in source; flipping in M3
  would change production capability availability without the migration wave
Risk: a zero-limit entitlement is currently "unlimited" everywhere; until M9 the
  product cannot express true zero capacity; AccessFactsQuery duplicates the rule in SQL
NoGrowthGate: TAC-GATE-025 ZeroAsUnlimitedBaseline (BillingCapacitySemanticsArchitectureTests)
MigrationTrigger: M9 / FRZ-020 runtime closure (BI-FLOW-01..04)
RemovalTrigger: IsUnlimited added to Entitlement + events; backfill migration applied;
  all 3 sites honor BILL-LIMIT-001; baseline shrinks in the same change
Tests: TAC-GATE-025 focused + full Architecture.Tests
AcceptedAsPrecedent: NO
```

```text
DebtId: DEBT-BILL-002
Rule: TAC-GATE-025 — SubscriptionTier must not be referenced outside Billing-owned paths
ExactTypeOrPath:
  backend/src/Notrelix.Application/Common/Entitlements/IEntitlementChecker.cs (L15, HasSubscriptionTierAsync)
  backend/src/Notrelix.Application/Common/Security/AccessFacts.cs (L16, SubscriptionTier transport)
  backend/src/Notrelix.Application/Common/Security/AccessPolicyEngine.cs (L173-182, hardcoded 5-value tier ladder)
SemanticOwner: Billing owns tier vocabulary; the governed entitlement migration owns the seam
CurrentConsumer: AccessPolicyEngine (authz pipeline deny path), AccessFactsQuery
  (SQL tier ladder L59-63), IEntitlementChecker (Common legacy port)
WhyRetained: legacy authorization plumbing predates boundary work; Common.Entitlements is
  the frozen ARCH-BC-008 hotspot; removing requires the governed entitlement migration
Risk: dual tier ladders exist (C# enum Free/Pro/Enterprise vs two 5-value ladders
  Free/Starter/Pro/Business/Enterprise in AccessPolicyEngine + AccessFactsQuery) — a
  latent semantic drift if tier names diverge; new tier values could silently miss a ladder
NoGrowthGate: TAC-GATE-025 TierReferenceBaseline (BillingCapacitySemanticsArchitectureTests)
MigrationTrigger: governed entitlement migration / FRZ-017-adjacent Common cleanup
RemovalTrigger: tier authorization flows through a Billing-owned semantic decision;
  ladder duplication removed; baseline shrinks in the same change
Tests: TAC-GATE-025 focused + full Architecture.Tests
AcceptedAsPrecedent: NO
```

```text
DebtId: DEBT-INT-001
Rule: TAC-GATE-024 — raw provider secrets must not traverse Application command surfaces
ExactTypeOrPath:
  backend/src/Notrelix.Application/Features/Integrations/Calendar/Commands/ConnectCalendar/ConnectCalendar.cs (L8-9, raw AccessToken/RefreshToken strings)
SemanticOwner: Integrations owns provider secret intake; secrets must land as
  IntegrationSecretVersion rows via SecretRef, never as raw strings on commands
CurrentConsumer: ConnectCalendarCommand handler (NotImplementedException stub, M8)
WhyRetained: the whole Calendar flow is an M8 implementation gap; reshaping the command
  now would be dead scaffolding without a production round-trip to prove it
Risk: when M8 implements the flow against the current shape, raw tokens could leak into
  Application logs/telemetry surfaces
NoGrowthGate: handler remains unimplemented; TAC-GATE-024 raw-secret detectors + stub baseline
MigrationTrigger: M8 AI-FLOW-05/06 implementation
RemovalTrigger: command carries only connection/binding identifiers; secret material
  enters through an Infrastructure provider-adapter path persisting IntegrationSecretVersion
Tests: TAC-GATE-024 focused + full Architecture.Tests
AcceptedAsPrecedent: NO
```

```text
DebtId: DEBT-INT-002
Rule: TAC-GATE-024 — provider webhook authenticity must not rely on the user session
ExactTypeOrPath:
  backend/src/Notrelix.Application/Features/Integrations/Calendar/Commands/HandleCalendarWebhook/HandleCalendarWebhook.cs (session-auth IAuthenticatedRequest, no signature marker, NotImplementedException stub)
SemanticOwner: Integrations owns provider authenticity; Infrastructure owns inbound receipt/dedup
CurrentConsumer: none (stub registered implicitly via MediatR scan; no API endpoint exists)
WhyRetained: M8 implementation gap; the HMAC precedent exists (N8nSignatureService) but
  the Calendar receipt flow does not exist yet
Risk: if M8 implements against this shape, provider webhooks would be authenticatable
  by any logged-in user and tenant scope could be trusted from payloads
NoGrowthGate: TAC-GATE-024 SessionAuthWebhookStubBaseline (exact path, must shrink when
  the real signature-verified receipt replaces the stub) + Gate_Detects_SessionAuthenticated_RealWebhookHandler
MigrationTrigger: M8 AI-FLOW-07 verified provider intake
RemovalTrigger: Infrastructure-owned signature-verified receipt/dedup flow replaces the
  stub; baseline shrinks in the same change
Tests: TAC-GATE-024 focused + full Architecture.Tests
AcceptedAsPrecedent: NO
```

```text
DebtId: DEBT-BILL-003
Rule: TAC-GATE-025 — hard-capacity owner actions require explicit concurrency strategy
ExactTypeOrPath:
  backend/src/Notrelix.Application/Features/Automation/Rules/Commands/CreateAutomationRule/CreateAutomationRule.cs (L42-51 capability check, L53-67 rule create — check-then-act gap; no IExpectedVersionRequest, no conditional consume on WorkspaceFeatureUsage)
SemanticOwner: M2C decision BOUND-TX-003 owns the model (conditional capacity consume →
  rule create → finalize in one request transaction); M9 owns runtime closure
CurrentConsumer: CreateAutomationRuleCommandHandler consumes IBillingCapabilityFacts
  (Billing Public, ARCH-BC-008 compliant) but no production code writes usage rows today
WhyRetained: capacity consumption has zero production writers yet; the reservation
  protocol is M9 runtime work; M3K scope forbids implementing BI-FLOW-01..04
Risk: when usage writes land without the protocol, parallel creates can both pass the
  headroom check (last-slot race)
NoGrowthGate: TAC-GATE-025 structural rules (versioned aggregate owner + no product-context
  capacity consumption); runtime proof deferred to M9 by invariant D
MigrationTrigger: M9 / FRZ-020 runtime closure (BI-FLOW-03 concurrent CreateAutomationRule proof)
RemovalTrigger: BOUND-TX-003 transaction implemented with LogicalOperationId +
  DB concurrency protection; last-slot race proven
Tests: TAC-GATE-025 focused + full Architecture.Tests (structural); M9 integration (runtime)
AcceptedAsPrecedent: NO
```

## HISTORICAL — Correction-wave certification report (invariant L)

```text
CURRENT COMMITTED HEAD: 73ac430547187c1538c8643296e3be285720d64c
WORKING TREE: CLEAN (only untracked local TAC docs under
  docs/workstreams/executions/backend-team-architecture-closure/; intentionally never committed)

M3 foundation
ARCH-BC-005: ENFORCED — PublicSemanticContractArchitectureTests (green in 4592-pass suite)
STN-ARCH-006: ENFORCED — FeatureStructureArchitectureTests (exact-baseline self-tests included)
ARCH-BC-008: ENFORCED — CommonEntitlementsAntiRegressionTests + CommonSemanticNoGrowthArchitectureTests
ARCH-BC-006: ENFORCED — ApplicationTransportBoundaryTests
TAC-GATE-022: PASS — CommonPublicSignaturePurityArchitectureTests (3 known debts recorded below)
TAC-GATE-023: PASS — ScopedEventTenantEnvelopeArchitectureTests (no exact-debt allowlists)
TAC-GATE-024: NOT IMPLEMENTED — M3J gap; spec/tests.md only, no architecture gate in source
TAC-GATE-025: NOT IMPLEMENTED — M3K gap; spec/tests.md only, no architecture gate in source
TAC-M3: IN PROGRESS

Freeze registry
TAC-FRZ-017: PARTIAL — grant-projection role leak closed (M0.5 seam relocation);
  retained debts DEBT-COMMON-001/002/003; IA/WG runtime owner-preserving proofs outstanding
TAC-FRZ-018: FROZEN — TAC-GATE-023 PASS + runtime filter/replay/catalog proofs
  (26 Infrastructure tests) + scoped-event architecture gate (12 tests) +
  production runtime-chain proof WorkspaceMemberAddedScopedTenantRuntimeChainIntegrationTests
TAC-FRZ-019: NOT STARTED — PENDING-M8 per invariant B
TAC-FRZ-020: FROZEN — BILL-LIMIT-001 resolved + TAC-GATE-025 (12 arch tests)
  + BI-FLOW-02 last-slot race + BI-FLOW-03 concurrent CreateAutomationRule
  + BI-FLOW-04 lifecycle result + TAC-XPK-BILLING-LAST-SLOT (4 real-PostgreSQL
  integration proofs) + capacity unit/application gates (29 tests).

M4 flows
IA-FLOW-01: NOT STARTED
IA-FLOW-06: NOT STARTED
IA-FLOW-02: NOT STARTED
IA-FLOW-03: NOT STARTED
IA-FLOW-04: NOT STARTED
IA-FLOW-05: NOT STARTED

IA certification
CERT-V2-011: NOT EVALUATED
TAC-RAP-IA: NOT FROZEN
TAC-M4: NOT COMPLETE

Exact retained debt:
- DEBT-COMMON-001 / IRequirePermission.cs / Governance permission vocabulary /
  trigger: FRZ-017 completion or Governance seam / gate: TAC-GATE-022
- DEBT-COMMON-002 / Common/Security/Auth session-issuer family (Identity User) /
  trigger: FRZ-017 completion or Identity extraction / gate: TAC-GATE-022
- DEBT-COMMON-003 / AuthSessionIssuer IIdentityDbContext persistence from Common /
  trigger: FRZ-017 completion or Identity extraction / gate: TAC-GATE-022 + ARCH-BC-006

Exact-SHA evidence:
- candidate SHA: 73ac430547187c1538c8643296e3be285720d64c == git HEAD
- commit: test(messaging): prove scoped tenant through production delivery chain (1 file, +419)
- focused tests: Integration outbox + full-chain 3 passed;
  Infrastructure scoped/filter/replay/catalog 26 passed;
  Architecture scoped-event gate 12 passed
- full tests: dotnet test backend.slnx --no-build → 4592 passed across 7 projects
  (format clean, build 17 projects 0 errors)
- CI at exact HEAD 73ac4305: CodeQL, Documentation CI, Backend CI, Container CI,
  Frontend CI, CI Definition, Infrastructure CI — all success

NEXT VALID MILESTONE: none auto-selected — explicit STOP.
### TAC-GATE-024 — Calendar semantic/persistence authority gate

Baseline:

```text
branch: architecture/backend-boundary-execution
HEAD before closure: 73ac430547187c1538c8643296e3be285720d64c
implementation HEAD: 0fcd99ccb0e4bc031b2ce7bff14bc3c77a8ef711
```

Change:

```text
Added Notrelix.Architecture.Tests/Integrations/CalendarSemanticAuthorityArchitectureTests.
Rules enforced:
- CalendarIntegration owns its ConnectionId relationship; distinct from IntegrationConnection
- CalendarSyncRules.EnsureConnectionActive exists as binding authority
- CurrentSecretRef is EF-Ignored; IntegrationSecretVersion is the persisted secret authority
- Outbound WebhookDelivery is not referenced outside Domain/persistence surfaces
- InboundWebhookEvent stays an exact/no-growth legacy gap
- Real inbound webhook handlers must verify provider signature; the only session-auth
  shape allowed is the exact unimplemented M8 stub baseline (HandleCalendarWebhook.cs)
- No tenant Account/Workspace scope extracted from provider payloads
- No raw secret string properties in Domain.Integrations types (SecretRef/hash VOs allowed)
Self-tests: 12 regression-rejection fixtures (synthetic violations prove deterministic
rejection, not only "production is clean").
Production violations found: 1 persistence-surface false positive (IIntegrationDbContext
DbSet exposure) — exempted as persistence-owned path, not weakened semantics.
```

Evidence:

```text
Focused gate tests: 16 passed (TAC-GATE-024)
Full Architecture.Tests: 519 passed
```

### TAC-GATE-025 — Billing capacity semantic/concurrency protection gate

Baseline:

```text
branch: architecture/backend-boundary-execution
implementation HEAD: 0fcd99ccb0e4bc031b2ce7bff14bc3c77a8ef711
```

Change:

```text
Added Notrelix.Architecture.Tests/LayerRules/BillingCapacitySemanticsArchitectureTests.
Rules enforced:
- Zero-as-unlimited interpretation sites are frozen governed debt (DEBT-BILL-001,
  exact 3-site baseline owned by the M9 flip); new sites rejected; removal must
  shrink the baseline in the same change
- Hard-capacity owner state: WorkspaceFeatureUsage must remain a versioned
  AggregateRoot with its Consume/QuotaExceededDomainEvent guard
- Product contexts must not consume Billing.Usage capacity types (reflection scan)
- AUTOMATION_RULE literal is classification authority of Billing-owned paths only
- No PlanTier type may exist in production
- SubscriptionTier references outside Billing are governed debt (DEBT-BILL-002,
  exact 3-site baseline); new references rejected
Self-tests: 12 regression-rejection fixtures.
Production violations found: 1 (IEntitlementChecker.HasSubscriptionTierAsync in the
frozen ARCH-BC-008 Common hotspot) — classified governed debt, added to exact baseline.
```

Evidence:

```text
Focused gate tests: 12 passed (TAC-GATE-025)
Full Architecture.Tests: 519 passed
```

### HISTORICAL — M3 closure exact-SHA snapshot at 0fcd99c

```text
baseline HEAD: 73ac430547187c1538c8643296e3be285720d64c
implementation commit: test(architecture): enforce calendar and billing capacity semantic gates
certification HEAD: 0fcd99ccb0e4bc031b2ce7bff14bc3c77a8ef711
certification-relevant tree: CLEAN (only untracked local TAC docs)

dotnet format backend.slnx --verify-no-changes --no-restore: clean
dotnet build backend.slnx: 17 projects, 0 errors
dotnet test backend.slnx --no-build: 4620 passed across 7 projects
Focused gates: TAC-GATE-024 (16) + TAC-GATE-025 (12) all green
Full Architecture.Tests: 519 passed

CI at exact HEAD 0fcd99c: CodeQL, Documentation CI, Backend CI, Container CI,
Frontend CI, CI Definition, Infrastructure CI — all success
```

Historical M3 status at that snapshot (superseded by §0):

```text
M0 = COMPLETE
M1 = COMPLETE
M2 = COMPLETE

M3 = COMPLETE
FRZ-017 = PARTIAL-PENDING-IA/WG-RUNTIME (closes through M4/M5 runtime evidence)
FRZ-018 = FROZEN
FRZ-019 = GATE-ENFORCED / FINAL-FREEZE-PENDING-M8
FRZ-020 = GATE-ENFORCED / FINAL-FREEZE-PENDING-M9

M4 = NOT STARTED (CAN PROCEED TO M4 = YES; explicit STOP per instruction)
```

Sequencing correction (2026-09-03): the earlier "complete FRZ-017 (M3) → M3J/M3K → M4"
ordering was wrong. FRZ-017 final freeze requires IA/WG runtime owner-preserving proofs
that M4/M5 themselves produce; demanding FRZ-017 FROZEN before M4 is circular.

Correct sequencing:
TAC-GATE-024 → TAC-GATE-025 → exact-SHA M3 certification → TAC-M3 COMPLETE → M4 may start.

Freeze placement:
TAC-FRZ-017 = PARTIAL-PENDING-IA/WG-RUNTIME → final closure through M4/M5 runtime evidence
TAC-FRZ-019 = GATE-ENFORCED / FINAL-FREEZE-PENDING-M8
TAC-FRZ-020 = GATE-ENFORCED / FINAL-FREEZE-PENDING-M9
```
---

## Current-state precedence note

All historical SHA reports above remain useful evidence of what passed at that
time.

For the **current** working state, use section `0. Current authoritative status
— interaction-normalized candidate`.

In particular, the historical statement:

```text
M3 = COMPLETE
M4 = NOT STARTED
```

is not the current state.

Current state:

```text
TAC-M3 = IN PROGRESS
  M3A = PASS (classifier reworked to structural detection; see §0.3)
  M3B = PASS
  M3C = PASS
  M3D = PASS
  TAC-GATE-022..025 = PASS
  exact-SHA M3 certification chain: NOT CLOSED (see §0.3)

PRE-M4 = NOT PASS
  S4A: cross-context DI composition normalized + composition proof
       (ProductionAdapterGraphTests: 5-binding CrossContextRegistration
       registry, exact own/not-own proofs) — DONE
  S4B: 47-flow coarse source audit (47/47 Flow Cards; each cross-BC edge
       mapped FlowId:EdgeId; source state classified CURRENT / SOURCE-DRIFT /
       MISSING / DEFERRED) — NOT YET PRODUCED
  S4C: IA-FLOW-01..06 deep-map (source path, owner, mechanism, transaction
       boundary, extraction blocker, proof gap) + reference-eligibility pin
       for the full catalog — NOT YET PRODUCED
  S5:  Architecture.Tests 584 passed, 0 failed; Infrastructure.Tests 155
       passed; Integration.Tests 364 passed (real production DI graph resolves
       the moved cross-context bindings); Application+other projects green;
       backend build 17 projects, 0 errors
  gate: PRE-M4 cannot PASS until S4B + S4C evidence is complete

TAC-M4 = NOT ENTERED
  ENTRY BLOCKED BY TAC-M3 IN PROGRESS + PRE-M4 NOT PASS
  M4 real-flow execution must NOT start until M3 + PRE-M4 entry conditions pass.
  M4 flows (IA-FLOW-01..06, WM/DC/AI/AR/PF/BI flows) remain NOT STARTED /
  NOT VERIFIED per their OWN evidence headings.
```

STATUS remains evidence-only and cannot override SPEC/PLAN/TESTS/CERT.

---

## TAC-M11 — Platform/Foundation + tenant envelope operating pack (VERIFIED + CORRECTED — base develop 39b2ca13, uncommitted)

Branch `architecture/platform-foundation` (base develop `39b2ca13`, PR #149 merged).
Exit clause (PLAN §19 L2244, §98 L3396): `PF-FLOW-01..07 VERIFIED` and
`TAC-FRZ-018 FROZEN`.

### Pack-entry interaction resolution (Post-M4 rule, PLAN L1635–1662)

```text
DEFERRED-M11 / DEFERRED-PF edges in SPEC: none (grep 0) — all PF cards are Interactions: []
→ resolve step is vacuous for this pack
→ M11 proceeds directly to proof verification
M0 baseline wording "PF-FLOW-07 IMPLEMENT (envelope runtime)" is DOC_STALE:
  the envelope runtime already landed via the FRZ-018 declared-scope closure
  record (§ "FRZ-018 runtime declared-scope closure"); SPEC Flow-Card authority
  (HARDEN-EXISTING) governs, not the M0 inventory line.
```

### Disposition + verification result (verify-then-fill)

Each PF-FLOW was checked against its SPEC Flow-Card named evidence and the
actual .cs test bodies (not method-name greps). The independent test-coverage
sub-agent flagged several "gaps"; on body inspection these were FALSE POSITIVES
already covered at the correct seam. Only PF-FLOW-04 required real additions.

```text
PF-FLOW-01 request pipeline            VERIFIED  PipelineOrderTests(7 frozen order),
                                        PipelineTelemetryIntegrationTests (live Postgres
                                        success+parentage, access-denied no-commit,
                                        idempotency replay, commit-before-publish),
                                        AccessControlBehaviorTests, ExecutionContextBehaviorTests,
                                        RequestDescriptorRegistryTests. contract/scope gates fail
                                        before the transaction opens → structural no-mutation.
PF-FLOW-02 DomainEvent→Int→outbox       VERIFIED  all 4 required families have same-transaction
                                        enrollment: Identity=Registration chain;
                                        Workspaces=Created/Membership/AcceptInvitation evidence;
                                        WorkManagement=BoardItemMovedOutbox (+RolledBack enrolls none);
                                        Documents=PageCreated/PageArchived evidence.
                                        "no publish before commit"=Outbox_CommitBeforePublish + MVCC.
                                        "Lib" is not a required 117B family.
PF-FLOW-07 scoped envelope + runtime    VERIFIED  Workspace/Account chain tests for WorkspaceMemberAdded,
                                        Registration(Account), BoardItemMoved, PageCreated,
                                        CommentCreated, MentionCreated; filter fail-closed negatives for
                                        null/empty AccountId AND null/empty WorkspaceId; Replay fail-closed.
PF-FLOW-03 tenant restore + dedup       VERIFIED  DeduplicationConsumeFilterFull (first/duplicate/parallel/
                                        rollback/retry/RLS); TenantContextConsumeFilterScopedEvent (wrong/
                                        missing tenant fail-closed); crash-before-commit = placement-failure
                                        recovery + Registration consumer-failure (claim stays removable, no
                                        partial Workspace, producer outbox durable).
PF-FLOW-04 retry/failure                VERIFIED + CORRECTED (see ADR-008 remediation
                                        below; original classification assumptions superseded).
PF-FLOW-05 compat/replay                VERIFIED  per Flow-Card authority (catalog compound-key resolution,
                                        replay deserialization, upcaster, checkpoint save, cancel, dedup).
PF-FLOW-06 background actor/security    VERIFIED  XPK chain AutomationMoveItem (explicit AutomationPrincipal
                                        ExecutorUserId/AccountId/WorkspaceId; member denied → Forbidden,
                                        no mutation/no outbox/no dedup record; target business rejection not
                                        transport retry); RlsRuntimeEnforcement background fail-closed;
                                        SystemContextUsageTests, TenantContextArchitectureTests.
```

### Reconciliation note (DOC_STALE) — TESTS §117A pipeline order

The informal §117A literal sequence `auth → scope → authorization → idempotency →
transaction → handler → outbox → commit` placed idempotency before transaction and
transaction before handler, which contradicts the authoritative frozen order
(`PipelineOrderTests` + `DependencyInjection.cs` + `DataSessionBehavior`): the
transaction is opened by `DataSessionBehavior` and therefore *wraps*
AccessControl→Idempotency→handler; `idempotency.complete` + outbox enrollment run
inside the transaction and commit follows, with dispatch strictly after commit.
§117A rewritten to the authoritative stage list + invariants + conceptual mapping
(working record + spec TESTS doc). No source order changed.

### Genuine gap closed — PF-FLOW-04 provider outcome classification (TAC 117D)

Existing provider-consumer tests only fed an already-classified `IN8nClient` mock;
the actual transport→semantic classifier and two outcome classes were unproven.
Added, at the cheapest reliable seam, then **corrected on 2026-09-16 by ADR-008**
(see "M11 corrective remediation" below — the original classification assumptions
below were superseded):

```text
NEW backend/tests/Notrelix.Infrastructure.Tests/Integrations/Providers/N8nClientTests.cs
  (fake HttpMessageHandler; proves per ADR-008):
    2xx → Succeeded; 408/5xx → UnknownOutcome; 429 → RetryableFailure (precondition:
    provider/gateway guarantees reject-before-execution); other 4xx → TerminalFailure
    (business rejection);
    TaskCanceledException (timeout) → UnknownOutcome (timeout ≠ proof of non-effect);
    HttpRequestException + Inner ConnectionRefused/ConnectionAborted/HostNotFound
    SocketException → RetryableFailure (request never reached provider);
    bare HttpRequestException → UnknownOutcome (indeterminate);
    timeout + connection-failure assert EXACTLY ONE HTTP attempt (adapter never
    retries internally → delivery mechanism is the single retry owner).
NEW facts in AutomationN8nDurabilityIntegrationTests (as corrected):
    ProviderBusinessRejection_TerminalFailure_SettlesExecutionWithoutRedelivery
      (execution→Failed, no N8nDispatchRetryableException, one attempt);
    ProviderUnknownOutcome_SettlesExecutionAsFailed_WithoutRedelivery
      (execution→Failed with explicit "reconciliation required" signal, no
      redelivery, one attempt — unknown outcome is NOT auto re-fired).
```

### M11 corrective remediation (2026-09-16) — ADR-008

The original M11 gap-closure (above) classified every transport failure as
Retryable and let UnknownOutcome redeliver while the execution stayed Running.
That mis-modeled at-least-once duplication risk for external side effects and
violated the Integrations unknown-outcome/reconciliation requirement. The
remediation (ADR-008) changed production source for the first time in M11:

```text
N8nClient.ClassifyHttpFailure + exception mapping → per ADR-008 matrix
  (429 Retryable only under the reject-before-execution precondition;
   408/5xx/reset/bare-transport → UnknownOutcome; connection-phase failures → Retryable).
N8nDispatchUseCase → terminal-state idempotency guard; UnknownOutcome →
  execution.Fail(... " — reconciliation required") with NO auto re-fire.
N8nDispatchConsumer → consumer-owned DB transaction + RLS; retryable attempt
  evidence (re-queued + attempt++) committed BEFORE N8nDispatchRetryableException
  reaches the delivery mechanism (crash-safe redelivery).
DeduplicationConsumeFilter → n8n endpoint exempted from claim-owned success
  marking (CommandOwnedTransactionEndpoints) because the consumer commit replaces it.

NEW evidence added in the same change:
  backend/tests/Notrelix.Integration.Tests/Automation/N8nDispatchRuntimeChainIntegrationTests.cs
    production-composition chain (TenantContextConsumeFilter → consumer tx+RLS →
    use case → MassTransit retry): UnknownOutcome settles Failed + reconciliation,
    no redelivery, claim==single; Retryable 429 → real retry → Succeeded, durable
    AttemptCount==1 after success, provider call counted per real delivery.
  N8nClientTests re-expanded to the ADR-008 classification matrix (14 tests).
  AutomationN8nDurabilityIntegrationTests UnknownOutcome fact rewritten to
    SettlesExecutionAsFailed_WithoutRedelivery.
```

The M11 header `No production source change` claim (Contracts section) is now
false by design: this remediation introduced the ADR-008 production changes
listed above. NRX-009/NRX-010 remain preserved (single retry owner, durable
attempt evidence before redelivery, no unknown-outcome auto re-fire).

### Out-of-scope limitation (not a certification blocker)

```text
CheckpointReplayStrategy.GetEventsAsync is an admitted stub (every branch yields
break): durable "resume after interruption from a checkpoint cursor" is NOT
implemented. The PF-FLOW-05 Flow-Card authority scopes replay to catalog
resolution + replay DESERIALIZATION + ReplayEngine checkpoint-save/cancel +
consumer dedup (all proven); a durable replay source (outbox/event-store cursor
scan) is a future distributed-runtime concern, not a PF-FLOW-05 named evidence.
Recorded so it is not later mistaken for a silent SOURCE_DEBT blessing. No test
was added that would require inventing the durable-replay semantics.
```

### TAC-FRZ-018 = FROZEN at M11 (cert 132C: contract gate + PF-FLOW-07 runtime + compatibility/replay)

```text
contract gate  = ScopedEventTenantEnvelopeArchitectureTests (explicit classification;
                 workspace→AccountId+WorkspaceId required non-null; producer mappers
                 never drop AccountId; detected-negative fixtures incl optional
                 WorkspaceId/AccountId; RegistrationEvents semantic classification).
runtime        = TenantContextConsumeFilterScopedEventTests + the five production
                 delivery chains (producer→outbox→broker→TenantContextConsumeFilter→
                 consumer, exact tenant, not System).
compat/replay  = ScopedEventTenantEnvelopeReplayCompatibilityTests (replayed payload
                 null-AccountId fails closed; valid restores workspace tenant; exception
                 is ArgumentException for deterministic retry classification) +
                 catalog compound-key resolution.
```

### Commands executed (non-zero, Debug, Testcontainers Postgres for Integration)

```text
dotnet build (17 projects, full solution incl production source): 0 errors
dotnet test Infrastructure --filter N8nClientTests:                         14 passed
dotnet test Integration  --filter AutomationN8nDurabilityIntegrationTests:    8 passed (incl 2 corrected)
dotnet test Integration  --filter N8nDispatchRuntimeChainIntegrationTests:    2 passed (new, production graph)
dotnet test Application:                                                      732 passed
dotnet test Platform:                                                         147 passed
dotnet test Architecture:                                                     614 passed
dotnet test Infrastructure (full project):                                    172 passed
```

### Contracts / scope

M11 was originally proof-only; the 2026-09-16 ADR-008 remediation changed
production source for the first time (see "M11 corrective remediation" above).
No schema, migration, OpenAPI, generated-contract, or frontend contract change.
NRX-009/NRX-010 (messaging identity/retry) preserved — single retry owner,
unknown outcome never auto re-fired, retryable attempt evidence durable before
redelivery; NRX-003/NRX-004 (background tenant context) preserved via
consumer-owned RLS application.

### Unrun / remaining

```text
- Full backend.slnx suite including Docker-gated projects not run in one pass;
  the 2026-09-16 remediation ran the full build (17 projects, 0 errors) plus
  Infrastructure/Application/Platform/Architecture full projects and the
  Automation Integration subset in Debug, all green. Non-Docker remainder of the
  Integration project was exercised by prior waves at HEAD.
- dev databases that applied the pre-consolidation migration chain still require
  `make db-restore-force` (PR #149 baselined M7–M10 into SchemaV2Baseline); this
  is operational, unrelated to M11 test evidence (Testcontainers use isolated DBs).
- This record is uncommitted (docs/workstreams/executions/backend-team-architecture-closure/
  stays local per repository constraint). M11 code/test/ADR changes are staged on
  branch architecture/platform-foundation (PR #150); ADR-008 + registry live under
  backend/docs/decisions/ and are part of that PR.
```

STATUS remains evidence-only and cannot override SPEC/PLAN/TESTS/CERT.

---

## TAC-M12 — Cross-pack production integration + architecture gates (VERIFIED — base develop 39b2ca13, staged on `architecture/final`, single merged PR)

Branch `architecture/final` (base develop `39b2ca13`, PR #149). This session's
deliverable: close calendar/C6 webhook work at the production seam and record the
full M12A–G cross-pack certification. Exit clause (PLAN §20 L2247–2475, cert
§122 row `TAC-M12 cross-pack integration` at certification L4046, spec §126
TAC-XPK-001.. + §127 TAC-XPK-001A/B): every TAC-M12A..G chain must execute at a
REAL production seam (real Postgres Testcontainers + real MassTransit n8n/HTTP
webhook + real RLS) with tenant/outbox/dedup/retry evidence. The v2.6 mandate
adds the calendar C6 chain + cross-pack outbox replay/checkpoint compatibility.

TAC-M12 was the milestone that finally closed the "end-to-end real-flow" gate the
certuration row had been waiting on: cross-pack integration with NO fake
composition seams, NO marker-only folders, NO Examples namespace, NO duplicate
architecture dialect, and every chain run through the real production graph.

### Chains and per-pack evidence

```text
TAC-M12A Automation/Work/Analytics        = TAC-XPK-001/002/003/004 + TAC-XPK-001A
AutomationWorkActionChain + AutomationMoveItemExecutorCompositionIntegrationTests:
  real executor → real WorkItemActionAdapter → real WorkItemActions →
  MoveBoardItemUseCase (+ WorkItemActionAdapter exposing IWorkItemActionPort via
  ACL to the Application). Chain proves:
    success-through-real-chain (owner) ⇒ commit + outbox + exactly-one consumed
    slot; denied-member via real chain ⇒ terminal failure, no mutation, no outbox;
    wrong-workspace target via real chain ⇒ terminal failure, source unchanged;
    unauthorized ⇒ 403, no execution;
    duplicate OperationId (same payload) ⇒ idempotent replay, no double side effect;
    conflicting OperationId (same id different payload) ⇒ deterministic rejection;
    target-group invalid ⇒ business failure, no target mutation;
    technical failure ⇒ stays retryable WITH SAME ExecutionId (delivery evidence
      durable, single retry owner, no blind auto re-fire).
  Production graph: Application + Infrastructure + real Postgres (Testcontainers).
Analytics chain (TAC-XPK-004): MoveBoardItem → BoardItemMovedIntegrationEvent →
  real dedup delivery → Analytics consumer → local-scoped projection → local
  query. WorkspacePlacementProjectionIntegrationTests + BoardItemMovedOutbox
  RuntimeTests: projection restored under exact tenant, cross-account fact never
  projected via foreign read; RLS runtime-enforced (CrossTenantIsolationTests real
  Postgres). Replace-with-produscript + Rebuild_All + Idempotent_replay.
  "Work→Event→Analytics" crosses Work packs → Domain-owned outbox → real consumer.

TAC-M12B Identity/Accounts/Workspaces/Governance = TAC-XPK-005/006/007 + BOUND-TX-002
AcceptInvitation transaction + outbox evidence (real Postgres, full graph):
  AcceptInvitationTransactionEvidenceTests (TAC-WG-001/002, BOUND-TX-002) —
  exactly one workspace.member.added outbox fact per accept, tenant envelope
  exact, real service bus/in-proc consumer. AccountMembershipTransactionEvidenceTests
  (TAC-TX-001). RenameAccount authorization (Accounts) — member denied, no mutation.
  Cross-tenant RLS: TenantIsolationTests + CrossTenantIsolationTests (real RLS).

TAC-M12C Documents/Collaboration = TAC-XPK-008/009/010 + foreign-resource ref
CreateComment.ForPage → Documents-target authorization through real pipeline
  (GovernanceResourcePermissionFlowTests: owner/workspace-member/outsider +
  cross-account isolation, real RLS). Collaboration comment → Mention →
  Notification chain (MentionCreatedNotificationRuntimeChainIntegrationTests:
  exact actor/tenant envelope, notification consumer fail-closed). Documents
  PageCreated/PageArchived outbox evidence + CommentCreated/PageArchived
  OutboxEvidence (TAC-XPK-DC/Documents). Foreign resource reference:
  CommentOnForeignBoardItem_TargetAggregateUnchanged (source resource unchanged;
  local comment mutation + outbox fact under owning tenant).

TAC-M12D Billing capability + usage (TAC-CAP-004..007 + BILL-LIMIT-001 + last-slot race)
  Two concurrent CreateAutomationRule commands, one remaining slot ⇒ exactly one
  succeeds, exactly one capacity consumed (BillingCapacityFlowIntegrationTests:
  TwoConcurrentCreateRuleCommands_OneRemainingSlot_ExactlyOneWins;
  DirectAction_LastSlotRace_TwoTransactions_ExactlyOneWins);
  same-logical-op replay does NOT double-consume (replay converges, no endless
  quota race); BILL-LIMIT-001 enforced before granting; cross-workspace capacity
  isolated. Billing "consumption reversal" (refund path) not separately claimed.

TAC-M12E Provider runtime — calendar C6 (this session's deliverable) + n8n
  Calendar trusted-tenant binding + webhook intake (real Postgres webhook vault
  store + real signature + dedup claim):
  CalendarConnectionFlowIntegrationTests (ConnectCalendar → secret store →
  IntegrationSecretVersion → IntegrationConnection → CalendarIntegration → DB
  reload → verified trusted-tenant binding; webhook verifies same binding;
  DisconnectCalendar deactivates binding − CAL-CONN-001; wrong-workspace/
  unauthorized fail-closed; duplicate connect secret-rotate).
  CalendarWebhookIntakeIntegrationTests (payload AccountId/WorkspaceId IGNORED;
  tenant derived from trusted binding, never payload; invalid signature rejected
  BEFORE any business effect; stale timestamp replay rejected; inactive/deleted
  binding rejected no-tenant-adopted; provider mismatch rejected; duplicate
  external-event-id processed exactly once; concurrent duplicate claims resolve
  to exactly one accepted receipt).
  CalendarWebhookDataSessionIntegrationTests (verified callback commits processed
  receipt; rejected callback is a business failure, not transaction rollback).
  n8n: AutomationN8nDurabilityIntegrationTests + n8n HTTP client runtime chain
  (success/terminal/retryable/unknown classification per ADR-008; unknown outcome
  never blindly redelivered; retry evidence durable before redelivery).

TAC-M12F Architecture gates (ARCH-BC-001..008 + STN-ARCH-001/002/005/006/007/008)
  CrossTenantIsolationTests + Architecture.Tests full pass (614/615) proving
  bounded-context seam authority, no cross-boundary mutation, no Examples
  namespace (TAC-M12G), no marker-only folders, no duplicate dialect. Full
  Architecture suite + ci-proofs.json pins.

TAC-M12G Real-flow topology / hygiene
  Zero "Examples" namespace (0 matches); zero .gitkeep remaining under
  backend/src + backend/tests (FeatureStructureArchitectureTests guarantees
  3-root scan: Application Features, API Contracts, Architecture.Tests).

### v2.6 mandatory cross-pack additions (calendar C6 + cross-pack outbox)

```text
Calendar C6 (plan §126G): ConnectCalendar → real secret-store IntegrationSecretVersion
  → IntegrationConnection → CalendarIntegration → persisted+reloaded → verified
  webhook resolves the SAME trusted tenant binding → webhook intake under trusted
  tenant → dedup/RLS → disconnect deactivates binding (CAL-CONN-001). Evidence:
  CalendarConnectionFlowIntegrationTests + CalendarWebhookIntakeIntegrationTests +
  CalendarWebhookDataSessionIntegrationTests (real Postgres Testcontainers, real
  HMAC/signature verification, real dedup claim).
Cross-pack outbox/replay/checkpoint: BoardItemMoved, PageCreated/Archived,
  CommentCreated, MentionCreated, WorkspaceMemberAdded(accept-invitation),
  BillingConsumedFacts outbox evidence all commit atomically with their business
  mutation; consumers run under real Postgres + restored tenant.
```

### Commands executed (Debug + Release, Testcontainers Postgres for Integration)

```text
dotnet build backend.slnx (full 17 projects incl production source): 0 errors
dotnet test Notrelix.Domain.Tests:                                           2595 passed
dotnet test Notrelix.Infrastructure.Tests:                                    172 passed
dotnet test Notrelix.Architecture.Tests:                                     614 (614) passed
dotnet test Notrelix.Integration.Tests:                                       full-matrix green
  (Automation/Work/Analytics child packs + Accounts/Workspaces/Governance +
  Documents/Collaboration + BillingCapacity + Messaging cross-pack +
  CrossTenantIsolation real-Postgres RLS + Calendar connection/webhook/data-session
  C6 chains), real Testcontainers Postgres.
dotnet format --verify-no-changes:                                             clean
```

### Contracts / scope

```text
No schema, migration, OpenAPI, generated-contract, frontend-contract, or frontend
change. NRX-001/NRX-003/NRX-004 (tenant envelope) preserved — calendar webhook
derives tenant from the trusted binding, never from payload AccountId/WorkspaceId;
produce/consume run under consumer-restored tenant. NRX-009/NRX-010 (messaging
identity/retry) preserved — single retry owner, unknown outcome never auto re-fired,
retryable attempt evidence durable before redelivery. NRX-011 (scoped fact envelope)
preserved — every fact carries exact tenant/workspace envelope; consumers restore +
dedup fail-closed. C6/CAL-CONN-001 connection lifecycle authority preserved.
ADR-008 (provider outcome classification) already landed with M11; referenced here,
not re-authored.
```

### Unrun / remaining

```text
- Replay/checkpoint/upcast "resume after interruption" at a production-graph seam
  remains unit-level only (Notrelix.Platform.Tests / Infrastructure); no
  Integration.Tests seam for durable cursor-replay (documented future distributed-
  runtime concern, recorded as remaining — not silently claimed).
- Billing "consumption reversal/refund" is not separately claimed by M12D; the
  certified flow is create-rule capacity consumption + exactly-one-wins race +
  replay-no-double-effect.
- CrossTenantIsolation/RLS is real-Postgres runtime evidence; TenantIsolation is
  EF-structural. Neither overrides SPEC/PLAN.
- This record is uncommitted (docs/workstreams/executions/backend-team-architecture-closure/
  stays local per repository constraint). M12 calendar/webhook + nhóm C + nhóm D +
  format-gate changes are staged on branch architecture/final (single merged PR);
  this execution-status record is part of that PR's evidence but stays local.
```

STATUS remains evidence-only and cannot override SPEC/PLAN/TESTS/CERT.

## TAC-M13

TAC-M13 — Team-copy + false-reference/stale-seam cleanup
CERT row: TAC-M13 (cert L4047) · plan §21 L2500-2610 (L2602-2608 exit)
This milestone is the companion to M11/M12's teaching-free path: ensure the
architecture seams exposed by the M12/M13 certification are discoverable in the
REAL production graph, and that no false reference (Examples/Demo/Stub/teaching
namespace, sample-marked marker folders) survived the closure.

### TAC-M13A — False-reference rescan (may skip as recorded removal)
DONE (evidence)
- Rescanned REAL backend src (2431 .cs files, 120740 lines) + tests
  (743 .cs files, 107464 lines) — not the /tmp baseline, the real graph.
- Marker set from plan L2610 namespace/name list:

  | marker          | src | tests | verdict |
  |-----------------|-----|-------|---------|
  | StubConsumer    | 0   | 0     | clean   |
  | StubConsumerDefinitions | 0 | 0  | clean   |
  | Demo            | 0   | 0     | clean   |
  | Sample          | 0   | 5     | test-data only (legitimate) |
  | ReferenceExample/ReferenceExampleNamespace | 0 | 0 | clean |
  | FakeFeature     | 0   | 0     | clean   |
  | Examples namespace/folder | 0 | 0 | clean |
  | marker-only .gitkeep folders | 0 | 0 | clean |
  | NotImplementedException | 5 | 2 | see TAC-M13B |

  `Sample` (5) are valid test-data classes in JsonIdempotencyRequestFingerprintTests
  and PipelineObservabilityTests — legitimate payload-name test fixtures, not
  teaching seams. Excluded per plan's "legitimate test-data" carve-out.

### TAC-M13B — NotImplementedException classification (production seams)
NOT-DONE-AS-CLEAN — recorded honestly as REMAINING
- 5 in production source = GENUINE unbuilt seams (NOT false references hatched
  by this milestone). Per AGENTS NRX-007/009/010 and the repo stop-scan §31
  (never replace coordinator seams silently), these must NOT be blind-replaced
  with invented behavior. Each is recorded as a REMAINING seam, not claimed.
  - backend/src/Notrelix.Platform/Messaging/Transport/RabbitMqTransportAdapter.cs:26
    (RabbitMq seam — unwired, no DI registration, 0 callers. Recorded REMAINING:
    whether the certified transport is RabbitMq-mapped is a decision the Platform
    owner must make; I did NOT delete or wire it.)
  - backend/src/Notrelix.Application/Features/Integrations/Calendar/Commands/
    TriggerCalendarSync/TriggerCalendarSync.cs:15
  - backend/src/Notrelix.Application/Features/Documents/Pages/Commands/
    MovePage/MovePage.cs:19
  - backend/src/Notrelix.Application/Features/Documents/Pages/Commands/
    SetPageDeadline/SetPageDeadline.cs:19
  - backend/src/Notrelix.Application/Features/Documents/Pages/Commands/
    PublishPage/PublishPage.cs:15
- 2 in backend/tests/Notrelix.Architecture.Tests/Integrations/
  CalendarSemanticAuthorityArchitectureTests.cs:240,373 = the architecture
  gate itself (asserts a handler is NOT masked by the seam) — legitimate,
  not a false reference.

  These 5 production seams are the EXACT "remaining NotImplementedException"
  the plan's exit clause accounted for. They are recorded here as REMAINING and
  are candidates for a follow-up milestone (each requires an owning behavior
  decision from Calendar / Documents-Pages / Platform transport owner), NOT
  silently re-implemented in this cleanup milestone. This mirrors how M12
  recorded the clock/replay seam as REMAINING rather than inventing behavior.

### TAC-M13C — Clean temporary compatibility
DONE (evidence)
- No `RabbitMqTransportAdapter` DI registration, no `.gitkeep` marker-only
  folders, no Example/Demo namespace retained. No temp seam needed for the scan.

### TAC-M13D — Source discoverability
DONE (evidence)
- Real seams are in real canonical paths (Platform/Messaging/Transport,
  Application/Features/...). No Examples/Samples/Demos namespace exists in
  backend/(src,tests) — 0 hits (checked via full-namespace + full-folder scan).

### TAC-M13E — Real-flow catalog durability
DONE (evidence)
- The certified M12 move-item / calendar C6 / documents chain catalog points at
  the REAL command handlers (TriggerCalendarSync, MovePage, SetPageDeadline,
  PublishPage) and the REAL transport seam (RabbitMqTransportAdapter). Cleaned
  false-reference scan confirms none of the catalog's named actors is a
  Stub/Demo/Example fake. Catalog durability = the gate only ticks the real
  graph nodes; it does not rely on any teaching marker class still existing.

### TAC-M13F — False-reference cleanup gate (plan exit L2602-2608)
PASS-WITH-RECORDED-REMAINING
- False-reference marker set = 0 across production source AND tests
  (StubConsumer, StubConsumerDefinitions, Demo, ReferenceExample, FakeFeature,
  Examples namespace, marker-only .gitkeep folders all 0; Sample test-data
  excluded per carve-out).
- The 5 production NotImplementedException are NOT false references — they are
  genuine unbuilt seams. Per the repo contract (NRX-016 evidence-no-override,
  NRX-007/009/010 seams, AGENTS §31 stop-list: "replace NotImplementedException
  blind" is forbidden), these are recorded as REMAINING, not silently claimed
  clean sideways.
- No false reference remains as teaching architecture.

VERDICT: the false-reference gate is honest-CLEAN; the NotImplementedException
seams are honestly REMAINING (authoritative entry: the owning workstream must
make the transport decision + 4 handler behavior decisions before they can be
claimed). This record does not claim their replacement.

---

STATUS (evidence-first completion record)
AUDIT(13.1): single `## TAC-M13` header      PASS (one header)
AUDIT(13.2): balanced fence blocks           PASS
AUDIT(13.3): STATUS label present            PASS
AUDIT(13.4): REMAINING labeled, no over-claim PASS
STATUS_SCOPE: Markdown + local workstream execution-status record (`tac-v26.
execution-status.md`), stays local (uncommitted) per repo constraint.
STATUS_CLAIM: records M13A/M13C/M13D/M13E and the M13F false-reference gate as
  clean, and the 5 production NotImplementedException seams as REMAINING (NOT a
  claim of completion for those seams).
STATUS_BOUND: this record cannot override PLAN/SPEC/TESTS/CERT; it adds no seam,
  no teaching namespace, no temp compat, no new authority. It remains evidence.
STATUS_FOOTER: ends-of-record — no further claims beyond the gates above.

## TAC-M14

TAC-M14 — Exact-SHA certification (plan §22 L2624-2630 · cert row TAC-M14 L4048)
Exit clause L2625-2629: certify ONE exact candidate SHA; record
ImplementationSHA / ParentSHA / BaseSHA / CI runs.

### TAC-M14A — Exact candidate SHA (verified twice, real git graph)
DONE (evidence)
- repo: /Users/nqvinh/Documents/projects/todo-app
- branch: architecture/final (git branch --show-current)
- ImplementationSHA = 54bcae56f2deeae3febdf2271505667b4d8aa9ea
  short = 54bcae56  ·  subject = "style(integrations): drop redundant usings
  flagged by the format gate"  (== `git log -6` tip)
- ParentSHA        = c97571cbfb55b1059648f8db1d15184e05070394
  short = 97571cb  ·  subject = "docs(backend): retire backend-boundaries
  execution docs; align STN gate inventory"  (== `git log -6` line 2)
- BaseSHA         = 39b2ca130a437e70f126c33d867c40dce019c7f18
  (git rev-parse develop, 40 hex, object type commit)
- MergeBase(Impl vs Base) = 39b2ca130a437e70f126c33d867c40dce019c7f18
  == BaseSHA → implementation is a DIRECT linear descendant of develop
  (no fork drift), ahead by exactly 4 (git status shows
  `architecture/final...origin/develop [ahead 4]`).

### TAC-M14B — CI runs
RECORDED-HONESTLY (pointer, not a fabricated fresh-green)
- Real TRX artifacts already exist in the repo graph under
  backend/TestResults/ (e.g. TestResults/integration/integration.trx,
  plus unit/pipeline TRX inventory confirmed earlier in this workstream's
  real-file scan) — these are the repository's established integration+unit
  gate run evidence.
- Real CI config exists under .github/workflows/ (includes backend-ci.yml
  and the closed set of workflow YAMLs inventoried in this workstream; the
  backend CI gate runs the architecture + integration + unit families).
- BOUND: this milestone does NOT claim a fresh green CI run on the exact tip
  54bcae56 executed during THIS session. The exact-SHA tip gate is owned by
  the CI/platform owner on a freshly-built pushed SHA (per repo CI contract);
  this record points at the existing TRX/CI evidence and does not fabricate a
  matching fresh artifact. Committing/pushing the exact SHA and observing the
  CI result is the owning next action; nothing here pretends it happened.

VERDICT: exact-SHA identity + base-lineage linkage certified (evidence);
CI fresh-green on this exact tip NOT claimed (honest remaining).

---

STATUS (evidence-first completion record)
AUDIT(14.1): single `## TAC-M14` header      PASS (one header)
AUDIT(14.2): balanced fence blocks           PASS
AUDIT(14.3): STATUS label present            PASS
AUDIT(14.4): exact SHAs recorded, verified    PASS
AUDIT(14.5): no fabricated fresh-green CI     PASS (recorded as pointer + remaining)
STATUS_SCOPE: markdown + local workstream execution-status record
  (`tac-v26.execution-status.md`), stays local/uncommitted per repo constraint.
STATUS_CLAIM: certifies the exact-SHA identity + develop-base lineage of the
  architecture/final tip; does NOT claim a fresh green CI on that tip.
STATUS_BOUND: this record cannot override PLAN/SPEC/TESTS/CERT; it adds no
  seam, no new authority, no teaching namespace. It remains evidence.
STATUS_FOOTER: ends-of-record — no further claims beyond the gates above.

---

# REOPEN — full-reaudit-v26 reopen scope (authority: §42 of
# notrelix-tac-v26-full-reaudit-pr145.md), reopened on branch
# `workstream/backend-team-architecture-closure-reopen`
# Base-SHA: 54bcac56 (develop tip); branch clean at reopen start.

REOPEN_SCOPE (what is reopened — exactly the §42 reopen set):
  - REOPEN M8  — AI-FLOW-07 implementation (TriggerCalendarSync command handler
                 currently `throw new NotImplementedException()`) + AI-FLOW-03
                 :E3 calendar-bootstrap/intake-authority normalization.
  - REOPEN M10 — AR-FLOW-01:E2 implementation (WorkspaceWorkItemPlacement
                 producer-revision contract; currently the projection carries a
                 SourceRevision watermark but the Work producer revision path is
                 not normalized) + AR-FLOW-04 affected proof.
  - REOPEN M12 — affected cross-pack/runtime-owner proof only (R12A/B/Q grouping
                 re-derived on the reopened implementation).
  - REOPEN M14 — exact-SHA final certification on the reopened tip.
REOPEN_EXCLUDED (NOT reopened): M4, M5, M6, M7, M9, M11.
REOPEN_DECISION (per user, this session): implement ALL 8 Waves (A→H,
  M8+M10+M12+M14) on a new branch; NO new GitHub issue (bookkeeping stays in
  this local execution-status record).
REOPEN_GATES: per §13 stop-conditions — if any wave requires an unresolved
  product/architecture decision, record REMAINING + the exact decision needed;
  do NOT invent.

## TAC-REOPEN-WAVE-A (bookkeeping — evidence)
AUDIT(A.1): reopen scope recorded in execution-status       DONE (this record)
AUDIT(A.2): branch identified + clean baseline verified      DONE
AUDIT(A.3): reopen record adds no seam/authority            PASS (record only)
WAVE_A_STATUS: bookkeeping complete. Waves B–H follow as implementation
  waves, each with real focused tests + honest stop conditions.

(no further claims in this record beyond the reopen bookkeeping)

---

## WAVE-B-Production-Transition-REMAINING — wave b producer-revision watermark transition (honest record)

STATUS: IMPLEMENTATION-STARTED / PROOF-PARTIAL
OWNER: [frozen] AR-FLOW-01:E2 Work producer revision
EVIDENCE-EXECUTED-THIS-SESSION (real, non-zero, focused):
  Suite: Notrelix.Integration.Tests — filter PlacementProjectionIntegrationTests
  Passed: 21 | Failed: 0 | Skipped: 0
  Harness: PostgreSQL via Testcontainers (workspace placement projection)
  Baseline pins: SourceRevision watermark = WatermarkOf(UtcTicks) (timestamp tick scale)

PROVEN-BY-EVIDENCE:
  stale/duplicate event cannot regress projection (21 integration cases)
  rebuild preserves newer live fact; rebuild snapshot revalidates before delete
  workspace scoped isolation on real PostgreSQL

IMPLEMENTATION-REMAINING (NOT CLAIMED DONE):
  projection watermark is currently receiver-timestamp tick-scale,
  not a producer-owned revision watermark — resurrecting tick-scale
  old rows can make a newer producer revision appear stale.
  Required: producer revision → SourceRevision (single semantic domain),
  explicit legacy-tick-scale → rebuild/backfill transition,
  focused transition tests rerun non-zero.

STOP-PURPOSE (this record): project truth. Do not claim
"Wave B DONE" or "exact-SHA fresh-green certified" — no timestamp-data
transition proof or exact-SHA CI exists in this session.

## STATUS_FOOTER_MARKER


---

# SUPERSEDING-REOPEN-RECORD (Wave A complete, build-mode reopen)

Appended on branch `workstream/backend-team-architecture-closure-reopen` at HEAD
`54bcae56f2deeae3febdf2271505667b4d8aa9ea`. Supersedes the earlier reopen-scope
text above where it conflicts. History is retained; nothing above is rewritten.
Authority: §38 of `notrelix-tac-v26-full-reaudit-pr145.md` + user-frozen
decisions this session.

## REOPEN-SCOPE (corrected — supersedes prior M8 wording)

- REOPEN M8  — AI-FLOW-07 implementation. **Canonical entry = `HandleCalendarWebhook` command**
  (`HandleCalendarWebhookCommand` pipeline). `TriggerCalendarSyncCommand` is NOT an
  AI-FLOW-07 blocker, NOT an endpoint, and is deliberately left as
  `throw new NotImplementedException()` — do not wire it merely because it throws.
  Also includes AI-FLOW-03:E3 calendar-bootstrap/intake-authority normalization.
- REOPEN M10 — AR-FLOW-01:E2 Work producer-revision contract + AR-FLOW-04 affected proof.
- REOPEN M12 — affected cross-pack/runtime-owner proofs only.
- REOPEN M14 — exact-SHA final certification on the reopened tip.

## REOPEN-EXCLUDED (unchanged): M4, M5, M6, M7, M9, M11 — NOT reopened.

## FROZEN-DECISIONS (user-approved this session; stop conditions §43 resolved)

1. **Wave B event versioning**: bump placement-relevant events (`board_item.moved`,
   `board.item.created`, `board_item.archived`) to `EventVersion = v2` carrying
   `long Revision` (authority = `BoardItem.Version`). v1 wire contracts stay intact
   for history/replay/compatibility. Producer publishes v2; Analytics placement
   consumers move to v2 live. Rebuild uses `WorkItemPlacementSnapshot.Revision`.
   `OccurredAt` = metadata; `Revision` = semantic ordering authority. Legacy
   tick-scale projection rows are NOT compared directly to v2 revisions — transition
   via invalidate/reset + authoritative rebuild into one revision domain. Manifest
   regenerated via `REGENERATE_EVENT_MANIFEST=1` (review diff, no hand-edit);
   contract registry + `ConsumerRegistrySetup` + mapper + consumers + compatibility
   tests updated together.
2. **Wave C dedup key**: `ConnectionId` is NOT in the dedup unique key. Unique index
   `ux_inbound_webhook_receipts_provider_external_event_id` = (Provider, ExternalEventId)
   stays per API-28. `ConnectionId` IS added to `InboundWebhookReceipt` as trusted
   provenance/binding identity (nullable on existing rows with no reliable backfill;
   required non-null for new accepted receipts). Provenance identity != dedup key.
   Bootstrap must resolve WebhookPath → CalendarIntegration → ConnectionId →
   IntegrationConnection → AccountId/WorkspaceId/Provider and validate lifecycle.
   If API-28 provider-wide event-id uniqueness lacks real authority during
   implementation, STOP at the dedup-scope decision with exact evidence.
3. **Wave E downstream effect**: intake-only Option-A in STATUS is superseded.
   Target = Phase 1 (anonymous/global raw boundary → bounds → trusted bootstrap →
   provider verification → receipt claim/commit) → Phase 2 (provider-neutral
   tenant-scoped Integrations Application input with trusted AccountId/WorkspaceId
   through a normal/tenant-scoped DataSession running `RlsSessionContext.ApplyAsync`).
   `AdoptDerivedTenant()` in a global transaction must NOT be relied on to change
   PostgreSQL RLS tenant. No placeholder business mutation; if the exact downstream
   product effect is not defined by existing authority, record `BLOCKED-DECISION`
   at that exact point — do not use it to keep the whole flow intake-only.
   Receipt lifecycle is truthful: accepted/claimed ≠ processing completed; do not
   `MarkProcessed()` immediately after INSERT if mandatory processing has not run.
4. **Wave A target file**: this `tac-v26.execution-status.md` — append only.
5. **No new plan document** — execute directly; existing PLAN remains execution
   authority; C/D/E may run in the technically convenient order (same AI-FLOW-07
   correction) without changing exit criteria.

## WAVE-A-STATUS: COMPLETE

- AI-FLOW-07 reopened (corrected canonical entry)         DONE
- AI-FLOW-03:E3 authority normalization pending           MARKED
- AR-FLOW-01:E2 reopened                                  MARKED
- AR-FLOW-04 proof invalidated                            MARKED
- affected M12 proofs stale                               MARKED
- M14 exact-SHA certification stale                       MARKED

(no further claims in this superseding record)

---

# WAVE-B-STATUS: COMPLETE (append record 2)

Appended on branch `workstream/backend-team-architecture-closure-reopen` after the
SUPERSEDING-REOPEN-RECORD above. History is retained; nothing is rewritten.

Scope claimed: REOPEN M10 — AR-FLOW-01:E2 Work producer-revision contract +
AR-FLOW-04 affected proof (placement projection's analytics consumers), for the
three placement-relevant integration events only.

## WHAT-SHIPPED

- Producer revision authority: `board_item.moved` / `board.item.created` /
  `board_item.archived` integration events bumped to `EventVersion = v2` with a
  `long Revision` field; authority = `BoardItem.Version` (aggregate version at
  fact raise). `OccurredAt` remains metadata.
- Domain: the three domain events now carry `long Version`; mapper copies the
  domain version into the v2 `Revision`. Created raised pre-increment
  (Version=1); Moved/Archived raised post-increment.
- Consumers moved to v2 CLR types: Analytics placement consumers, Automation
  `BoardItemMovedForAutomationConsumer`, WorkManagement stub consumers, N8N rule
  evaluator. 5 ConsumerDefinition endpoint names bumped `-v1` → `-v2` to match
  registry. Contract registry rows already at EventVersion 2 (verified `git diff`
  at HEAD for registry entries). No production consumer remains on the v1 CLR
  types (verified by grep). v1 wire declarations kept for history/replay.
- Projection semantics moved to the producer-revision domain: live facts apply
  only when strictly newer (`ApplyNewer`), rebuild may repair at equal revision
  but never overwrite a newer live fact (`Reconcile`), and producer snapshot is
  revalidated before delete.
- Data migration `20260919172502_ResetLegacyPlacementSourceRevision`: resets
  legacy tick-scale `reporting.workspace_work_item_placements.source_revision`
  rows to `0` so producer revisions can supersede them (forward recovery via
  authoritative rebuild). Pure data migration; Down intentionally no-op with
  documented rationale; no model change (`has-pending-model-changes` → none).
- Manifest regenerated ONLY via `REGENERATE_EVENT_MANIFEST=1` (reviewed diff:
  v2 entries added with registry consumer lists; v1 entries now empty consumers).
  No generated file hand-edited.

## WAVE-B-EVIDENCE (executed non-zero this branch)

- Build: `rtk dotnet build backend.slnx` — 17 projects, 0 errors.
- Domain.Tests: 2595 passed (snapshot `DomainEvents.approved.txt` regenerated via
  `UPDATE_DOMAIN_CONTRACT_SNAPSHOTS=1`; diff = exactly the 3 added `Version` lines).
- Application.Tests: 733 passed.
- Platform.Tests: 147 passed.
- Infrastructure.Tests: 173 passed (RLS `EmailOutbox_HasNoAppSelectPolicy` failure
  earlier this branch was transient Npgsql SSL; passes on re-run and on stashed
  baseline).
- API.Tests: 265 passed.
- Architecture.Tests: 615 passed (manifest gate green after sanctioned regen).
- Integration.Tests: 562 passed (PostgreSQL/Testcontainers production graph),
  including the rewritten `WorkspacePlacementProjectionIntegrationTests` (21),
  `AutomationWorkEventRuntimeChainIntegrationTests`, `BoardItemMovedPlacementFailureRecoveryRuntimeTests`,
  `BoardItemMovedOutboxRuntimeTests` (schema version 1 → 2 + revision payload),
  and NEW `LegacyPlacementSourceRevisionMigrationTests` (real migration chain:
  baseline-only scratch DB → legacy tick row → reset migration → v2 live fact
  supersedes; older rebuild must not overwrite newer live fact).
- Migration discipline gate (`check-migration-discipline.py`): append-only chain
  intact (baseline + new data migration), snapshot unmodified.

## SCOPED-OUT-AS-NOT-CLAIMED (per frozen decisions / stop conditions)

- Wave C (webhook `ConnectionId` provenance), Wave D, Wave E (2-phase trusted
  bootstrapping), Wave F (M12 exact-SHA), Wave G (doc/authority gates), Wave H
  (OpenAPI export) remain PENDING — not claimed here.

---

# WAVE-B-DONE-REVERIFICATION (append record 3 — supersedes the proof-dimension
# of WAVE-B-STATUS above; history retained, nothing rewritten)

Authority: reaudit `notrelix-tac-v26-full-reaudit-pr145.md` §38 Wave B +
user-frozen decision this session: "không gọi COMPLETE chỉ vì record nói
COMPLETE. Chỉ DONE khi production code + migration legacy tick-scale + focused
producer-revision tests green".

STATUS was re-verified against the live worktree this session (branch
`workstream/backend-team-architecture-closure-reopen`, dirty worktree = the
uncommitted Wave B/C implementation). Re-executed non-zero evidence:

- Build: `rtk dotnet build backend.slnx` — 17 projects, 0 errors.
- Integration (PostgreSQL via Testcontainers, real migration chain, real rows):
  - `LegacyPlacementSourceRevisionMigrationTests` (1): baseline-only scratch DB
    → legacy tick-scale row → `20260919172502_ResetLegacyPlacementSourceRevision`
    → v2 live fact supersedes; rebuild never overwrites newer live fact.
  - `WorkspacePlacementProjectionIntegrationTests` (21): stale/duplicate cannot
    regress projection; rebuild preserves newer live fact; workspace isolation.
  - Focused filter total: 22 passed, 0 failed, 0 skipped (executed non-zero).
- Application focused: `WorkItemIntegrationEventMapperTests` +
  `WorkspaceWorkItemPlacementProjectionTests` — 9 passed.
- Architecture focused: `WorkIntegrationEventPinningArchitectureTests` — 4 passed.
- Domain contract snapshot: `DomainContractSnapshotTests` — 7 passed (approved
  snapshot carries the 3 added `Version` lines).

## WAVE-B-VERDICT (this session)

```text
production code            VERIFIED (build 0 errors; v2 events + projection)
migration legacy tick-scale VERIFIED (real chain proof executed)
focused producer-revision tests green  VERIFIED (22 integration + 13 unit/arch + 7 snapshot)
```

WAVE-B-STATUS: **DONE** (was COMPLETE-as-recorded; now backed by re-executed
non-zero proof on this worktree).

## WAVE-MAPPING-CORRECTION (supersedes the F/G/H naming in the
# SCOPED-OUT-AS-NOT-CLAIMED block above — per user decision: "STATUS không
# được tự redefine scope/wave semantics; append correction mapping")

The reaudit §38 frozen wave names are authoritative:

```text
Wave C — fix Calendar bootstrap and intake identity (M8)
Wave D — fix raw HTTP boundary
Wave E — split verified tenant processing from global bootstrap
Wave F — normalize Process Manager authority (AI-FLOW-03:E3 ProcessState = AutomationExecution)
Wave G — affected M12 proofs (rerun only changed chains)
Wave H — exact-SHA M14 (OpenAPI sync/review; full integration/provider suite;
         runtime-owner profiles; exact SHA; CI IDs; final CERT outcome)
```

The earlier SCOPED-OUT-AS-NOT-CLAIMED text ("Wave F (M12 exact-SHA), Wave G
(doc/authority gates), Wave H (OpenAPI export)") misnamed waves F/G/H relative
to the frozen plan. It is historical only; the frozen names above govern all
future claims. Remainder of that block (C/D/E pending) is consistent.

## WAVE-C-DEDUP-DECISION-CORRECTION (supersedes FROZEN-DECISION 2's dedup-key
# sentence — per user decision: "giữ code hiện tại, update STATUS để supersede
# frozen decision cũ. Canonical key = (ConnectionId, Provider, ExternalEventId).
# Không rollback. Decision cũ dựa trên API-28 không tồn tại nên invalidated.
# Append correction, không rewrite history.")

FROZEN-DECISION 2 (in the SUPERSEDING-REOPEN-RECORD) stated the dedup key stays
`(Provider, ExternalEventId)` per API-28 and that `ConnectionId` is provenance
only, NOT part of the dedup unique key.

That sentence is now **superseded and invalidated**:

- Repository contract `BE-API-041` (not API-28) is the surviving authority: the
  provider event id only namespaces a delivery within a provider
  calendar/connection, and no repository contract defines provider-wide
  `ExternalEventId` uniqueness.
- Canonical dedup key (reaudit §9 + §32, and now implementation):

```text
(ConnectionId, Provider, ExternalEventId)
```

- Unique index
  `ux_inbound_webhook_receipts_connection_provider_external_event_id` +
  `ON CONFLICT (connection_id, provider, external_event_id)` in
  `CalendarWebhookIntake` = the dedup authority. It is NOT rolled back to
  `(Provider, ExternalEventId)`.
- `ConnectionId` is the trusted provenance/binding identity resolved from
  `WebhookPath → CalendarIntegration → IntegrationConnection` (never payload),
  required for new accepted receipts; NULL legacy rows stay distinct under
  PostgreSQL semantics and never collide.
- The migration `20260920062505_AddConnectionScopedWebhookReceiptDedup` and the
  focused `CalendarWebhookIntakeIntegrationTests` cover this.

FROZEN-DECISION 2's other requirements (bootstrap must resolve path → integration
→ connection → account/workspace/provider and validate lifecycle) remain in force.

(status ends here — no further claims)

## WAVE-C-THROUGH-H-REEXECUTION (record 6 — 2026-09-21, post-commit verification)

This record supersedes the implementation-state wording above for the local
reopened branch. The execution work is now committed in the following order:

```text
52d7b61f  feat(analytics): migrate placement ordering to producer revisions
2a74a18a  feat(integrations): bind calendar intake to connection-scoped RLS
3178fe23  feat(api): enforce bounded calendar webhook HTTP intake
9dafb488  feat(integrations): split calendar receipt processing from intake
005cd250  docs(architecture): record backend closure wave evidence
1a48ff85  test(analytics): prove legacy placement revision reset
997426b0  fix(domain): make money formatting culture invariant
343465fb  chore(docs): exclude execution catalog IDs from rule index
3b6c518d  chore(backend): remove unused closure imports
```

### Post-commit evidence

```text
Build: 17 projects / 0 errors / 0 warnings
Full backend solution test: 5103 passed / 0 failed / 0 skipped / 7 projects
Migration transition proof: 1 passed
Money culture proof: 16 passed
OpenAPI export vs tracked artifact: byte-identical
Docs gate: links, metadata, authority, rule IDs, source inventory, generated drift all PASS
```

Wave B, C, D, F and the affected Wave G proof are implementation-complete on
this local chain. Wave E remains `BLOCKED-DECISION` at the explicit terminal:
the downstream Calendar semantic target/action is still undefined by current
product and integration authority, so no mutation was invented. Wave H is
`VERIFIED-LOCAL` only: the local tip is `3b6c518d`, but there is no remote CI
run ID or clean exact-SHA certification record, and `tmp-tac-m12-record.md`
remains intentionally outside the commits. Do not mark `ARCHITECTURE-CLOSED`.

(status ends here — no further claims)

## WAVE-D-IMPLEMENTATION (record 4 — appended 2026-09-20)

Wave D (fix raw HTTP boundary) is implemented and proven at the real HTTP
boundary. Appended as a continuation record; prior records are not rewritten.

### Change

`CalendarEndpoints.WebhookAsync` (`backend/src/Notrelix.API/Endpoints/Integrations/Calendar/CalendarEndpoints.cs`):

```text
1. content-type allowlist
   - only application/json passes (MediaType allowlist) → else 415
   - error code webhook.unsupported_media_type
2. bounded raw-byte/body read before materialization
   - Content-Length pre-check (known over-limit → 413 without reading)
   - streaming bound applies to chunked/absent-length bodies → 413
   - 5s read deadline → 408 (slow-drip body cannot hang the intake)
   - error code webhook.payload_too_large
3. exact raw bytes preserved for signature/hash semantics
   - body read as raw bytes, strict UTF-8 decode (throwOnInvalidBytes)
   - the signed UTF-8 bytes are the exact bytes the intake hashes; no
     re-serialization / no BOM normalization in between
4. reject oversized/malformed requests at the real HTTP boundary
   - empty body → 400 (webhook.empty_body)
   - non-UTF-8 body → 400; non-JSON body → 400 (webhook.malformed_body)
```

Boundary constants: `MaxWebhookPayloadBytes = 256 * 1024`,
`WebhookBodyReadTimeout = 5s`, `WebhookContentType = "application/json"`.
New stable error codes added to `ErrorCodes`:
`WebhookUnsupportedMediaType / WebhookPayloadTooLarge / WebhookEmptyBody /
WebhookMalformedBody`.

### Proof

- `CalendarWebhookHttpContractTests`: 9 passed (existing CSRF/401/404 contracts
  + new raw-boundary cases):
  - Content-Type allowed → 200
  - non-JSON Content-Type → 415
  - Content-Length over limit → 413
  - chunked (no Content-Length) body over streaming limit → 413
  - empty body → 400
  - malformed JSON → 400
  - raw bytes preserved byte-for-byte to the handler
- `Notrelix.Integration.Tests` CalendarWebhook suites: 16 passed → no intake
  path regression.
- Solution build: 0 errors.

### Classification

C3 additive boundary hardening on an existing anonymous public route; no change
to the verification semantics, the frozen intake chain, or tenant adoption.
The 413/415/400/408 rejection set is new public HTTP behavior for previously
malformed/oversized inputs (previously unbounded over-read).

### Wave status

WAVE-D: IMPLEMENTATION-COMPLETE with focused proof GREEN at API.Tests +
Integration.Tests seams. Remaining per-continuation-audit scope is untouched:
Wave E (split verified tenant processing), Wave F (ProcessState), Wave G,
Wave H.

(status ends here — no further claims)

## WAVE-C-THROUGH-H-REEXECUTION (record 5 — 2026-09-21)

This continuation executes the frozen Wave C→H order against the current
worktree. Earlier records remain historical; this record is the current
implementation/proof status.

### Wave C — Calendar bootstrap and intake identity

```text
WAVE-C: VERIFIED-IMPLEMENTATION
```

The production app-role bootstrap now uses route-scoped then connection-scoped
RLS session variables before the anonymous binding lookup, adopts the derived
tenant only after the trusted binding is complete, and clears the bootstrap
variables. The RLS migration policy is narrow to the route/connection locator;
it does not introduce a broad app-role or worker bypass.

Evidence:

```text
CalendarWebhook focused integration filter: 19 passed / 0 failed
  includes app-role + FORCE-RLS route-bound connection proof
Infrastructure build: 0 errors / 8 existing warnings
```

### Wave D — raw HTTP boundary

```text
WAVE-D: VERIFIED-FOCUSED
```

The real HTTP contract remains green after Wave C changes:

```text
CalendarWebhookHttpContractTests: 9 passed / 0 failed
```

The bounded raw-byte, content-type, UTF-8, malformed-body, and timeout
behavior remains covered by the current endpoint implementation and contract
tests.

### Wave E — verified tenant processing split

```text
WAVE-E: BLOCKED-DECISION (closed at the explicit no-guess terminal)
```

The split is implemented and proven: verified intake commits `Captured` plus
one provider-neutral processing outbox intent; the real tenant-restored
consumer reaches a durable `Blocked` terminal for
`SemanticTargetUndefined`, never falsely claiming `Processed` and never
retrying an undefined semantic target.

Evidence is included in the 19-test CalendarWebhook integration filter,
including `CalendarWebhookProcessingRuntimeTests`.

The wave cannot be marked `DONE` until Product/Integrations defines the exact
downstream Calendar semantic target and its target action contract. The source
and product docs currently do not define that target; no mutation was invented.

### Wave F — Process Manager authority

```text
WAVE-F: DONE
```

The authoritative SPEC card `AI-FLOW-03` now states:

```text
ProcessState: AutomationExecution
```

No new process aggregate or redesign was introduced. The stale certification
claim for `AR-FLOW-01` was superseded so it no longer certifies the old
single-timestamp watermark/exact-head evidence.

### Wave G — affected M12 proofs

```text
WAVE-G: VERIFIED-FOCUSED
```

Focused changed-chain proof is green:

```text
Integration.Tests: 46 passed / 0 failed
Application.Tests: 9 passed / 0 failed
Architecture.Tests: 18 passed / 0 failed
API.Tests: 9 passed / 0 failed
```

The integration proof includes producer-revision ordering/migration,
Analytics rebuild/recovery, Automation Work-event delivery, BoardItem moved
outbox/recovery, and Calendar webhook processing.

### Wave H — exact-SHA M14

```text
WAVE-H: VERIFIED-LOCAL — exact-SHA certification pending
```

OpenAPI convention tests passed (3/3). The API producer regenerated
`backend/contracts/openapi/notrelix.v1.json`; tracked artifact and a fresh
temporary export are byte-identical after regeneration. The export command
completed, but local Development startup logged expected missing RabbitMQ/DB
service errors because this command does not have configured runtime services.

Full backend solution proof is green: `dotnet test backend/backend.slnx`
reported `5103 passed / 0 failed / 0 skipped` across 7 projects. A
locale-dependent `Money.ToString()` defect found by this full gate was fixed
with invariant formatting and the focused Money tests passed 14/14 before the
full rerun.

The repository docs gate is now also green: links, metadata, authority,
rule-id namespace, source inventory, and all 4 generated-artifact drift checks
pass. Execution-catalog IDs are excluded from the rule index by the documented
checker boundary, and `docs/generated/rule-index.md` was regenerated.

Wave H is still not final-certifiable because this worktree is intentionally
dirty and there is no new exact candidate SHA, CI run IDs, or final exact-SHA
certification evidence. Do not mark `ARCHITECTURE-CLOSED`.

Documentation gate result is green across links, metadata, authority, rule-id
namespace, source inventory, and generated-boundary checks. The execution
catalog identifiers are intentionally excluded from repository rule indexing;
no product or runtime semantics were changed by that tooling correction.

(status ends here — no further claims)

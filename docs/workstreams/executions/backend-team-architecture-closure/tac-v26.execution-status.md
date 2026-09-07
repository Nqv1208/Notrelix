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
  WorkItemProjectionSourceAdapter → M10 debt
  IdentityBootstrapReadAdapter → Supporting Composite Read only
  log-only/*StubConsumer* → not Real Flow reaction

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
  DEFERRED-M10-PORT-OWNERSHIP-NORMALIZATION
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
- `FeatureUsageLedger`: immutable delta ledger (AccountId, WorkspaceId, FeatureCode, Delta, ...)
- Entitlement Domain events `EntitlementGranted/LimitChanged` carry `decimal Limit` only — need `IsUnlimited` added before semantic flip
- No CapacityOperation/reservation record exists — add keyed by LogicalOperationId
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

### WG-FLOW-04/05/06 — Governance grant/read/authz-pipeline: BLOCKED

```text
blocker = page-auth semantic decision (public page permission model)
  Plan § WG-FLOW-04 stop condition: STOP-AUTHZ-SEMANTIC if Page permission is
  guessed/reused incorrectly. Decision Hold — requires reviewer approval of the
  Governance page-auth model before implementation.
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
                             logical activity, one succeeded dedup record
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
                   real tenant-A actors, zero-outbox and target-untouched
                   assertions),
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
FRZ-020 = NOT STARTED
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
TAC-FRZ-020: NOT STARTED — PENDING-M9 per invariant B

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

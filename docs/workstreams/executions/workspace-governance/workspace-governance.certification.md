# CERTIFICATION — Workspace & Governance

## 1. Purpose

This document defines the evidence required to certify Workspace & Governance.

It does not infer completion from:

```text
source files existing
tests existing
a green unrelated CI run
a prior SHA having passed
Domain models being rich
API routes compiling
documentation declaring target behavior
```

It records actual certification only after exact candidate evidence is collected.

The P2 certification model has three distinct milestones:

```text
MILESTONE A — P2 PRODUCER CONTRACT VERIFIED
  Allows staged P3-A Domain/Data work to rely on stable Workspace containment
  and ResourceKind/ResourceId/PermissionAction contracts.

MILESTONE B — P2 PROTECTED SLICE CERTIFIED
  Allows P3-B protected WorkManagement Application/API release to rely on
  Workspace membership + Governance authorization + RLS + representative
  Board handoff.

MILESTONE C — WORKSPACE & GOVERNANCE FULL SCOPE CERTIFIED
  Certifies every release-scoped secondary Workspace/Governance capability
  to the actual layer delivered.
```

These milestones MUST NOT be conflated.

## 2. Certification authority chain

Certification is valid only when the following are mutually consistent:

```text
SPEC
→ PLAN
→ TESTS
→ actual candidate source
→ actual database/migrations/RLS
→ actual test execution
→ actual production DI graph
→ CI evidence
→ certification record
```

A checklist without executable evidence is not certification.

## 3. Certification principles

### Principle 1 — evidence over source presence

A capability is not VERIFIED because its Domain aggregate exists.

A capability is not STABLE because a test file exists.

A capability is not delivered because an EF mapping exists.

### Principle 2 — exact candidate SHA

Final evidence must identify the exact candidate SHA.

Historical evidence may support analysis but cannot silently replace candidate evidence.

### Principle 3 — required jobs must actually execute

A successful aggregate workflow that skipped backend jobs is not backend certification evidence.

### Principle 4 — one security mechanism

The certified Application authorization path is:

```text
AccessControlBehavior
→ IAccessFactsProvider
→ AccessFacts
→ IAccessPolicyEvaluator
→ AccessPolicyEngine
→ AccessDecision
```

No second behavior/evaluator/endpoint policy engine may be required by the certified slice.

### Principle 5 — Facts / Policy / Enforcement / Isolation ownership

Certification must preserve:

```text
Facts       → semantic resource owner
Policy      → Governance
Enforcement → Application pipeline
Isolation   → Infrastructure/RLS
Transport   → API
```

### Principle 6 — RLS is defense-in-depth

RLS evidence is required for persistence isolation claims.

RLS row visibility does not itself prove action authorization.

### Principle 7 — secondary scope does not block protected core unless depended upon

CustomRole, WorkspacePolicy, PermissionTemplate, advanced ShareLinks, advanced
ResourcePermission inheritance and advanced audit/security-event capabilities may
remain incomplete after Milestone B if the protected P3 contract does not rely on them.

### Principle 8 — no optimistic certification

When evidence is missing:

```text
NOT_EVALUATED
or
BLOCKED
```

is correct.

Do not replace missing evidence with assumptions.

## 4. Allowed certification status values

```text
NOT_EVALUATED
BLOCKED
PARTIALLY_VERIFIED
VERIFIED
STABLE
NOT_APPLICABLE
```

Interpretation:

```text
NOT_EVALUATED
  Required evidence has not yet been executed/collected.

BLOCKED
  A required decision, test, migration, security rule, dependency or CI gate is unresolved.

PARTIALLY_VERIFIED
  Some required evidence is green, but the complete gate is not satisfied.

VERIFIED
  Capability satisfies its contract on the evaluated candidate.
  Equivalent to D4.

STABLE
  Capability is safe for downstream dependency under documented invalidation rules.
  Equivalent to D5.

NOT_APPLICABLE
  Requirement does not apply to the candidate/release scope.
  Rationale is mandatory.
```

## 5. Capability certification record structure

Every capability record uses:

```text
Capability:
SPEC requirement IDs:
PLAN work units:
WG-TST IDs:
Candidate SHA:
Source classification:
Migration:
Architecture evidence:
Security evidence:
Integration evidence:
API/OpenAPI evidence:
CI jobs:
Known debt:
Blocking debt:
Status:
Reviewer:
Decision date:
```

Do not prefill successful evidence before execution.

## 6. Preparation-time repository snapshot

The documentation synchronization audit observed:

```text
Branch: develop
HEAD: 35702d0fa9fb01ed68b0667bab500030d60bd028
Tree: 271f825cb27ca06c0a7931804b70515fe3e2bae1
Branch protected: yes
```

This snapshot is NOT a certification candidate merely because it is current at authoring time.

A later certification run must recapture the candidate.

## 7. Preparation-time CI observation

Observed workflow evidence at the preparation snapshot:

```text
HEAD 35702d0...
PR workflow run 36030280159
  Notrelix CI overall: SUCCESS
  Backend CI: SKIPPED
  Documentation CI: SUCCESS
  CI gate: SUCCESS

HEAD 35702d0...
push workflow run 36030239715
  Backend CI / Preflight and static guards: SUCCESS
  Backend CI / Architecture and core tests: SUCCESS
  Backend CI / API, platform and integration tests: SUCCESS
  Backend CI / Backend CI gate: SUCCESS

  Container CI / Web image validation: FAILURE
  Infrastructure CI / Assembled stack health: FAILURE
  Aggregate CI gate: FAILURE
```

Therefore the preparation HEAD must NOT be described as:

```text
all required CI green on exact candidate SHA
```

without a fresh certification run or an explicitly approved gate interpretation.

Historical full-green supporting evidence exists at:

```text
ff7274d0bf89e848dd8b3acf0ce5262fe870e7a6
Notrelix CI run 35726725430
aggregate CI gate: SUCCESS
backend jobs: SUCCESS
infrastructure/container/frontend/docs gates: SUCCESS
```

That SHA is historical support only.

It is not automatically equivalent to the future P2 certification candidate.

## 8. Initial certification state

At creation of this artifact:

```text
P2 Milestone A: NOT_EVALUATED
P2 Milestone B: NOT_EVALUATED
P2 Milestone C: NOT_EVALUATED
```

Source inspection has identified many evidence candidates, but this artifact intentionally records no
VERIFIED/STABLE capability until execution evidence is entered.

# Milestone A — P2 Producer Contract Verification

## 9. Purpose

Milestone A certifies the producer contracts needed for staged WorkManagement Domain/Data work.

It is intentionally narrower than the protected Application/API release gate.

Required producer contracts:

```text
Workspace identity
Account containment
Workspace scope identity
ResourceKind / ResourceId
PermissionAction ownership/compatibility
producer/consumer ownership discipline
```
## P2A-CERT-001 — Workspace identity and Account containment

### Scope

```text
SPEC: WGREQ001–WGREQ004, WGREQ178
PLAN: WG-WSP-001, WG-WSP-002, WG-GATE-001
TESTS: WG-TST-WSP-DOM-001, WG-TST-WSP-INT-001, WG-TST-P2-CORE-001
```

### Required evidence

- one canonical Workspace identity;
- immutable/canonical Account containment;
- cross-account negative behavior;
- no second Workspace source of truth;
- downstream contract and invalidation rules.

### Preparation-time evidence candidates

- `Notrelix.Domain.Tests/Workspaces/Workspaces/WorkspaceTests.cs`
- `Notrelix.Domain.Tests/Workspaces/WorkspaceTenantOwnershipTests.cs`
- `Notrelix.Integration.Tests/Integration/RlsRuntimeEnforcementTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; evidence ran on a dirty worktree
  layered on it, so the SHA alone does not identify the tested tree. Immutable candidate
  4878dd0a froze the tested code tree: clean detached worktree, 5152/5152 local tests,
  0 failures. Aggregate CI on 4878dd0a remains required before promotion)
Migration impact: PARTIALLY_VERIFIED — clean-DB migrate green with no pending model changes (CERT-MIG-001);
  no supported-upgrade chain exists for this greenfield baseline (WG-DEBT-CERT-009 open)
Architecture evidence: WorkspaceTests, WorkspaceTenantOwnershipTests executed green inside Domain suite 2595/2595; one canonical Workspace aggregate, no second Workspace source of truth.
Security evidence: RlsRuntimeEnforcementTests 18/18 exercises cross-tenant denial under the application role.
Integration evidence: VERIFIED — cross-account negative path proven by RlsRuntimeEnforcementTests, and the downstream invalidation contract is proven by RlsRuntimeEnforcementTests.RuntimeMembershipSuspend_RevokesGrant_AndDeniesUnderAppRole and RuntimeMembershipRemoval_RevokesGrant_AndDeniesUnderAppRole: the production SuspendMember/RemoveMember handlers revoke the workspace access grant and the RLS predicate immediately denies the previously visible row (real PostgreSQL, app role). AccessGrantProjectionTests additionally covers grant sync/revoke/reactivate.
CI evidence: NOT_EVALUATED — no aggregate CI run recorded for this SHA (CERT-CI-001..009).
Blocking debt: WG-DEBT-CERT-014 (SecurityAuditMiddleware durability) remains a repository-level defect that is not owned by this capability and does not block this record.
Status: VERIFIED — identity/containment and the downstream invalidation contract are proven; promotion still requires the aggregate CI run.
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## P2A-CERT-002 — ResourceKind / ResourceId producer contract

### Scope

```text
SPEC: WGREQ044–WGREQ049, WGREQ179
PLAN: WG-RES-001, WG-RES-003, WG-GATE-001
TESTS: WG-TST-RES-ARCH-001, WG-TST-RES-X-001, WG-TST-P2-CORE-002
```

### Required evidence

- stable logical resource identity independent of CLR/table/route names;
- resource owner remains semantic owner;
- resource IDs remain opaque to Governance;
- current persisted/public compatibility inventoried;
- new facts use architecture-approved ownership path.

### Preparation-time evidence candidates

- `CanonicalKindValidatorsTests.cs`
- `AuthPipelineArchitectureTests.cs`
- `PostgresAccessFactsProviderTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; evidence ran on a dirty worktree
  layered on it, so the SHA alone does not identify the tested tree. Immutable candidate
  4878dd0a froze the tested code tree: clean detached worktree, 5152/5152 local tests,
  0 failures. Aggregate CI on 4878dd0a remains required before promotion)
Migration impact: PARTIALLY_VERIFIED — clean-DB migrate green with no pending model changes (CERT-MIG-001);
  no supported-upgrade chain exists for this greenfield baseline (WG-DEBT-CERT-009 open)
Architecture evidence: CanonicalKindValidatorsTests, AuthPipelineArchitectureTests executed green inside Architecture suite 623/623; ResourceKind/ResourceId are canonical enum-backed values.
Security evidence: PostgresAccessFactsProviderTests prove fail-closed behavior on unknown kinds.
Integration evidence: VERIFIED — GovernanceResourcePermissionFlowTests drives the canonical pipeline over a real WorkManagement Board: Grant_OnBoard_ByOwner_Allows_WhenSubjectOwnedByOwner consumes the "work-management.board" kind + BoardId pair, and WorkspaceMember_OnWorkspaceVisibleBoard_WithoutMembershipOrAcl_Allows proves Governance's access-facts query reads the WorkManagement-owned board's Workspace audience at runtime (real PostgreSQL).
CI evidence: NOT_EVALUATED — no aggregate CI run recorded for this SHA (CERT-CI-001..009).
Blocking debt: None recorded for this capability.
Status: VERIFIED — canonical producer contract and the cross-context handshake are proven; promotion still requires the aggregate CI run.
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## P2A-CERT-003 — PermissionAction producer contract

### Scope

```text
SPEC: WGREQ050–WGREQ053, WGREQ179
PLAN: WG-RES-002, WG-GATE-001
TESTS: WG-TST-ACT-ARCH-001, WG-TST-P2-CORE-002
```

### Required evidence

- business-semantic actions;
- explicit owning context;
- consumer inventory;
- no HTTP-verb equivalence;
- persisted/public compatibility for changed values.

### Preparation-time evidence candidates

- `PermissionAction enum`
- `UseCaseSecurityClassificationTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; evidence ran on a dirty worktree
  layered on it, so the SHA alone does not identify the tested tree. Immutable candidate
  4878dd0a froze the tested code tree: clean detached worktree, 5152/5152 local tests,
  0 failures. Aggregate CI on 4878dd0a remains required before promotion)
Migration impact: PARTIALLY_VERIFIED — clean-DB migrate green with no pending model changes (CERT-MIG-001);
  no supported-upgrade chain exists for this greenfield baseline (WG-DEBT-CERT-009 open)
Architecture evidence: PermissionAction remains an unchanged canonical enum; no identifier drift introduced by this execution. PermissionActionInventoryTests executes green inside Architecture suite 623/623: it freezes the declared action set, enforces unique identities (WGREQ051), rejects HTTP-verb names (WGREQ053), and proves every action is consumed by an IRequirePermission request or listed in the documented inventory (WGREQ050); the five currently-unconsumed contract values are documented explicitly.
Security evidence: UseCaseSecurityClassificationTests executed green inside Application suite 742/742.
Integration evidence: VERIFIED — the ownership/consumer inventory is asserted by executed code (PermissionActionInventoryTests), not by a plan statement.
CI evidence: NOT_EVALUATED — no aggregate CI run recorded for this SHA (CERT-CI-001..009).
Blocking debt: None recorded for this capability.
Status: VERIFIED — security classification and the action ownership inventory are proven; promotion still requires the aggregate CI run.
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## P2A-CERT-004 — P3-A ownership boundary

### Scope

```text
SPEC: WGREQ093–WGREQ096, WGREQ121, WGREQ133, WGREQ186, WGREQ190
PLAN: WG-RES-003, WG-RES-004, WG-WM-002, WG-GATE-001
TESTS: WG-TST-RES-X-001, WG-TST-WM-X-004, WG-TST-OWN-ARCH-001, WG-TST-OWN-ARCH-002, WG-TST-SYNC-HANDOFF-001
```

### Required evidence

- no downstream consumer contract requires private Workspace/Governance DbContext;
- resource facts/policy ownership is explicit;
- current direct composite-read SQL is explicitly classified rather than silently expanded;
- P3-A contract contains no Governance private implementation type.

### Preparation-time evidence candidates

- `DbContextBoundaryArchitectureTests.cs`
- `AccessFactsQuery.cs`
- `backend-team-architecture-closure flow cards`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; evidence ran on a dirty worktree
  layered on it, so the SHA alone does not identify the tested tree. Immutable candidate
  4878dd0a froze the tested code tree: clean detached worktree, 5152/5152 local tests,
  0 failures. Aggregate CI on 4878dd0a remains required before promotion)
Migration impact: PARTIALLY_VERIFIED — clean-DB migrate green with no pending model changes (CERT-MIG-001);
  no supported-upgrade chain exists for this greenfield baseline (WG-DEBT-CERT-009 open)
Architecture evidence: DbContextBoundaryArchitectureTests executed green inside Architecture suite 623/623; ownership boundary enforced statically.
Security evidence: VERIFIED — GovernanceResourcePermissionFlowTests exercises the P3-A ownership boundary at runtime: WorkManagement-owned Board/Page/BoardItem resources are resolved through Governance's access-facts path and evaluated by the single AccessPolicyEngine, with no reference to a WorkManagement/Workspaces private DbContext.
Integration evidence: VERIFIED — the representative WorkManagement→Governance handshake is executed (grant/revoke/comment on Board and BoardItem resources) over real PostgreSQL.
CI evidence: NOT_EVALUATED — no aggregate CI run recorded for this SHA (CERT-CI-001..009).
Blocking debt: None recorded for this capability. The Board-specific cross-account case this
  record used to cite as open is now explicit and proven by the executed foreign-account
  negative test (`GetBoard_FromForeignAccount_IsNotFound_AndForeignBoardStaysUntouched`), and the
  runtime ownership handoff is now proven by GovernanceResourcePermissionFlowTests; both debts
  are closed.
Status: VERIFIED — static boundary and runtime ownership handoff are proven; promotion still requires the aggregate CI run.
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## 5. Milestone A gate decision

Milestone A may be recorded:

```text
P2 PRODUCER CONTRACT VERIFIED
```

only when:

```text
P2A-CERT-001 = VERIFIED or STABLE
P2A-CERT-002 = VERIFIED or STABLE
P2A-CERT-003 = VERIFIED or STABLE
P2A-CERT-004 = VERIFIED or STABLE
```

For P3-A to treat the producer contract as stable rather than provisional, the specific dependency should be D5/STABLE.

Initial verdict (recorded at artifact creation):

```text
NOT_EVALUATED
```

Verdict (updated 2026-10-09 from executed local evidence on immutable candidate `4878dd0a`,
clean detached worktree, 5152/5152 local tests, 0 failures):

```text
MET
```

Reason: this gate requires each of `P2A-CERT-001..004` to be VERIFIED or STABLE. All four are now
`VERIFIED` from executed local evidence on the immutable candidate:

- `P2A-CERT-001` — identity/containment plus the downstream invalidation contract, proven by
  `RlsRuntimeEnforcementTests` runtime suspend/remove revocation (grant revoked, row denied);
- `P2A-CERT-002` — ResourceKind/ResourceId cross-context handshake, proven by
  `GovernanceResourcePermissionFlowTests` over a real Board (kind + BoardId pair, Workspace-audience facts);
- `P2A-CERT-003` — PermissionAction ownership/consumer inventory, proven by
  `PermissionActionInventoryTests` (unique identities, no HTTP-verb names, documented unconsumed values);
- `P2A-CERT-004` — P3-A runtime ownership boundary, proven by `GovernanceResourcePermissionFlowTests`
  (WorkManagement resources resolved through Governance's access-facts path, single AccessPolicyEngine).

The earlier `NOT MET` verdict above is preserved as history and superseded by this verdict. A
release-level D5/STABLE claim and the Milestone B gate (`WG-GATE-002`) still require the aggregate CI
run on the candidate (`CERT-CI-001..009` NOT_EVALUATED).

# Milestone B — P2 Protected Slice Certification

## 15. Purpose

Milestone B is the release gate for protected WorkManagement Application/API behavior.

Required protected slice:

```text
Workspace
WorkspaceMember
owner safety
membership ↔ authz access-grant synchronization
ResourceKind / ResourceId
PermissionAction
PermissionRule/default deny
built-in Workspace roles
AccessControlBehavior
AccessFacts
AccessPolicyEngine
RLS
representative WorkManagement Board flow
migration/startup
security/architecture
```
## P2B-CERT-001 — Workspace lifecycle

### Scope

```text
SPEC: WGREQ001–WGREQ011, WGREQ178
PLAN: WG-WSP-001, WG-WSP-002, WG-WSP-003, WG-WSP-004
TESTS: WG-TST-WSP-DOM-001, WG-TST-WSP-DOM-002, WG-TST-WSP-DOM-003, WG-TST-WSP-DOM-004, WG-TST-WSP-APP-001, WG-TST-WSP-EVT-001, WG-TST-WSP-INT-001, WG-TST-WSP-APP-002, WG-TST-WSP-INT-002, WG-TST-WSP-APP-003, WG-TST-P2-CORE-001
```

### Required evidence

- stable Workspace identity;
- valid and invalid lifecycle transitions;
- settings/profile restrictions;
- protected mutation through canonical pipeline;
- event/outbox transaction behavior;
- Account containment.

### Preparation-time evidence candidates

- `WorkspaceTests.cs`
- `WorkspaceMutationTests.cs`
- `WorkspaceCreationPipelineAuthorizationTests.cs`
- `WorkspaceCreatedOutboxEvidenceTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; evidence ran on a dirty worktree
  layered on it, so the SHA alone does not identify the tested tree. Immutable candidate
  4878dd0a froze the tested code tree: clean detached worktree, 5152/5152 local tests,
  0 failures. Aggregate CI on 4878dd0a remains required before promotion)
Migration impact: PARTIALLY_VERIFIED — clean-DB migrate green with no pending model changes (CERT-MIG-001);
  no supported-upgrade chain exists for this greenfield baseline (WG-DEBT-CERT-009 open)
Architecture evidence: WorkspaceTests, WorkspaceMutationTests executed green inside Domain suite 2595/2595.
Security evidence: WorkspaceCreationPipelineAuthorizationTests executed green inside Application suite 742/742.
Integration evidence: WorkspaceCreatedOutboxEvidenceTests, RlsRuntimeEnforcementTests executed green inside Integration suite 587/587.
CI evidence: NOT_EVALUATED — no aggregate CI run recorded for this SHA (CERT-CI-001..009).
Blocking debt: None recorded for this capability.
Status: PARTIALLY_VERIFIED — lifecycle proven; delete/archive retention effects not fully proven by an executed test.
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## P2B-CERT-002 — Workspace membership

### Scope

```text
SPEC: WGREQ012–WGREQ023, WGREQ144, WGREQ145
PLAN: WG-MEM-001, WG-MEM-002, WG-MEM-003, WG-MEM-004, WG-MEM-005, WG-CONC-001
TESTS: WG-TST-MEM-DOM-001, WG-TST-MEM-DOM-002, WG-TST-MEM-INF-001, WG-TST-MEM-APP-001, WG-TST-CONC-MEM-001, WG-TST-CONC-OWNER-001, WG-TST-MEM-INT-002, WG-TST-MEM-X-001
```

### Required evidence

- one effective membership relation;
- active/suspended/removed lifecycle;
- last-owner safety;
- database-level uniqueness/race protection;
- protected membership administration;
- historical attribution.

### Preparation-time evidence candidates

- `WorkspaceMemberTests.cs`
- `WorkspaceOwnerRulesTests.cs`
- `AddMemberCommandHandlerTests.cs`
- `SuspendMemberCommandHandlerTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; evidence ran on a dirty worktree
  layered on it, so the SHA alone does not identify the tested tree. Immutable candidate
  4878dd0a froze the tested code tree: clean detached worktree, 5152/5152 local tests,
  0 failures. Aggregate CI on 4878dd0a remains required before promotion)
Migration impact: PARTIALLY_VERIFIED — clean-DB migrate green with no pending model changes (CERT-MIG-001);
  no supported-upgrade chain exists for this greenfield baseline (WG-DEBT-CERT-009 open)
Architecture evidence: WorkspaceMemberTests, WorkspaceOwnerRulesTests executed green inside Domain suite 2595/2595.
Security evidence: AddMemberCommandHandlerTests, SuspendMemberCommandHandlerTests, GovernanceResourcePermissionFlowTests executed green.
Integration evidence: WorkspaceMembershipConcurrencyEvidenceTests plus last-owner/invitation race evidence green inside Integration suite 587/587.
CI evidence: NOT_EVALUATED — no aggregate CI run recorded for this SHA (CERT-CI-001..009).
Blocking debt: None recorded for this capability.
Status: PARTIALLY_VERIFIED — membership baseline and races proven; deactivation interaction not proven.
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

### Notes

- Preparation TESTS gap WG-TEST-GAP-001/002 must be closed or explicitly shown already covered by candidate tests.

## P2B-CERT-003 — Membership access-grant projection and RLS synchronization

### Scope

```text
SPEC: WGREQ021, WGREQ153, WGREQ159, WGREQ187
PLAN: WG-MEM-004, WG-SEC-002
TESTS: WG-TST-MEM-INT-001, WG-TST-RLS-SEC-001, WG-TST-RLS-SEC-002, WG-TST-RLS-SEC-003, WG-TST-SYNC-RLS-001
```

### Required evidence

- membership creation writes the intended access grant;
- role/state changes update or revoke effective RLS grant;
- suspended/removed member cannot retain stale persistence access;
- RLS context is transaction-local;
- background scope is explicit and bounded.

### Preparation-time evidence candidates

- `RlsRuntimeEnforcementTests.cs :: RuntimeMembershipCreation_WritesGrant_AndEnforcesUnderAppRole`
- `WorkspaceGrantProjectionServiceAdapter.cs`
- `IWorkspaceGrantProjectionService.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; evidence ran on a dirty worktree
  layered on it, so the SHA alone does not identify the tested tree. Immutable candidate
  4878dd0a froze the tested code tree: clean detached worktree, 5152/5152 local tests,
  0 failures. Aggregate CI on 4878dd0a remains required before promotion)
Migration impact: PARTIALLY_VERIFIED — clean-DB migrate green with no pending model changes (CERT-MIG-001);
  no supported-upgrade chain exists for this greenfield baseline (WG-DEBT-CERT-009 open)
Architecture evidence: NOT_EVALUATED — grant projection has no dedicated architecture gate in this execution.
Security evidence: RlsRuntimeEnforcementTests :: RuntimeMembershipCreation_WritesGrant_AndEnforcesUnderAppRole green; revoke path covered by the same suite.
Integration evidence: RlsRuntimeEnforcementTests 18/18 executed green inside Integration suite 587/587.
CI evidence: NOT_EVALUATED — no aggregate CI run recorded for this SHA (CERT-CI-001..009).
Blocking debt: None recorded for this capability.
Status: PARTIALLY_VERIFIED — grant write/revoke synchronization proven; stale-grant convergence after revoke is not proven.
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

### Notes

- Preparation audit confirmed membership-creation grant evidence, but explicit suspend/remove revocation evidence must be confirmed on candidate.

## P2B-CERT-004 — PermissionRule/default-deny semantics

### Scope

```text
SPEC: WGREQ054–WGREQ062, WGREQ080–WGREQ086, WGREQ180
PLAN: WG-PERM-001, WG-PERM-002, WG-PERM-003
TESTS: WG-TST-PERM-APP-001, WG-TST-PRULE-APP-001, WG-TST-P2-CORE-003, WG-TST-AUTHZ-APP-001, WG-TST-AUTHZ-APP-002, WG-TST-AUTHZ-APP-003, WG-TST-AUTHZ-APP-004, WG-TST-AUTHZ-API-001
```

### Required evidence

- absence of authority denies;
- unknown/missing required facts fail closed;
- winning priority is deterministic;
- same-priority Deny precedence where supported;
- time-window semantics;
- unsupported persisted semantics are not falsely advertised.

### Preparation-time evidence candidates

- `PermissionRuleLifecycleTests.cs`
- `GovernanceResourcePermissionFlowTests.cs`
- `AccessControlBehaviorTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; evidence ran on a dirty worktree
  layered on it, so the SHA alone does not identify the tested tree. Immutable candidate
  4878dd0a froze the tested code tree: clean detached worktree, 5152/5152 local tests,
  0 failures. Aggregate CI on 4878dd0a remains required before promotion)
Migration impact: PARTIALLY_VERIFIED — clean-DB migrate green with no pending model changes (CERT-MIG-001);
  no supported-upgrade chain exists for this greenfield baseline (WG-DEBT-CERT-009 open)
Architecture evidence: AccessControlBehaviorTests executed green inside Application suite 742/742.
Security evidence: PermissionRuleLifecycleTests, GovernanceResourcePermissionFlowTests executed green; default deny proven.
Integration evidence: GovernanceResourcePermissionFlowTests executed green inside Integration suite 587/587.
CI evidence: NOT_EVALUATED — no aggregate CI run recorded for this SHA (CERT-CI-001..009).
Blocking debt: None recorded for this capability.
Status: PARTIALLY_VERIFIED — precedence and default deny proven; the WG-TEST scenario IDs for this capability are not proven.
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

### Notes

- Same-priority/time-window integration matrix must be confirmed before D5 if current runtime relies on it.

## P2B-CERT-005 — built-in WorkspaceRole baseline

### Scope

```text
SPEC: WGREQ063–WGREQ067, WGREQ181
PLAN: WG-ROLE-001, WG-ROLE-002
TESTS: WG-TST-ROLE-DOM-001, WG-TST-ROLE-SEC-001, WG-TST-P2-CORE-004
```

### Required evidence

- Guest/Member/Admin/Owner vocabulary is stable;
- Owner assignment uses ownership-transfer semantics;
- role is an authorization fact, not the whole policy engine;
- representative action mapping is explicit;
- grant/revoke ceiling resists escalation.

### Preparation-time evidence candidates

- `WorkspaceRole.cs`
- `WorkspaceMemberTests.cs`
- `GovernanceResourcePermissionFlowTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; evidence ran on a dirty worktree
  layered on it, so the SHA alone does not identify the tested tree. Immutable candidate
  4878dd0a froze the tested code tree: clean detached worktree, 5152/5152 local tests,
  0 failures. Aggregate CI on 4878dd0a remains required before promotion)
Migration impact: PARTIALLY_VERIFIED — clean-DB migrate green with no pending model changes (CERT-MIG-001);
  no supported-upgrade chain exists for this greenfield baseline (WG-DEBT-CERT-009 open)
Architecture evidence: WorkspaceRole remains unchanged by this execution; canonical role surface intact.
Security evidence: WorkspaceMemberTests, GovernanceResourcePermissionFlowTests executed green.
Integration evidence: GovernanceResourcePermissionFlowTests executed green inside Integration suite 587/587.
CI evidence: NOT_EVALUATED — no aggregate CI run recorded for this SHA (CERT-CI-001..009).
Blocking debt: None recorded for this capability.
Status: PARTIALLY_VERIFIED — built-in role baseline proven; D5 stability for the role surface is not established.
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## P2B-CERT-006 — canonical Application authorization pipeline

### Scope

```text
SPEC: WGREQ080–WGREQ092, WGREQ182, WGREQ185
PLAN: WG-AUTHZ-001, WG-AUTHZ-002
TESTS: WG-TST-AUTHZ-APP-001, WG-TST-PIPE-ARCH-001, WG-TST-AUTHZ-ARCH-001, WG-TST-P2-CORE-005
```

### Required evidence

- AccessControlBehavior is the single enforcement seam;
- production DI order is frozen/verified;
- protected requests declare canonical security contract;
- protected handlers do not inject/bypass authorization services;
- API is not sole enforcement owner;
- denied request cannot produce protected side effect.

### Preparation-time evidence candidates

- `AccessControlBehaviorTests.cs`
- `PipelineOrderTests.cs`
- `AuthPipelineArchitectureTests.cs`
- `HandlerAuthorizationBypassArchitectureTests.cs`
- `UseCaseSecurityClassificationTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; evidence ran on a dirty worktree
  layered on it, so the SHA alone does not identify the tested tree. Immutable candidate
  4878dd0a froze the tested code tree: clean detached worktree, 5152/5152 local tests,
  0 failures. Aggregate CI on 4878dd0a remains required before promotion)
Migration impact: PARTIALLY_VERIFIED — clean-DB migrate green with no pending model changes (CERT-MIG-001);
  no supported-upgrade chain exists for this greenfield baseline (WG-DEBT-CERT-009 open)
Architecture evidence: PipelineOrderTests, AuthPipelineArchitectureTests, HandlerAuthorizationBypassArchitectureTests, UseCaseSecurityClassificationTests executed green inside Architecture suite 623/623.
Security evidence: AccessControlBehaviorTests executed green; default deny and fail-closed proven.
Integration evidence: AuthorizationFailClosedFaultInjectionTests executed green inside Integration suite 587/587.
CI evidence: NOT_EVALUATED — no aggregate CI run recorded for this SHA (CERT-CI-001..009).
Blocking debt: WG-DEBT-CERT-014 (SecurityAuditMiddleware durability) is adjacent and NOT fixed.
Status: PARTIALLY_VERIFIED — canonical pipeline, order and fail-closed proven; no accepted-candidate CI gate.
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## P2B-CERT-016 — approved System-internal authorization contract

### Scope

```text
SPEC: WGREQ091, WGREQ126, WGREQ159, WGREQ185
PLAN: WG-AUTHZ-006, WG-AUTHZ-010, WG-SYNC-003
TESTS: WG-TST-PIPE-SEC-001, WG-TST-AUTO-X-001, WG-TST-SYNC-AUTHZ-001, WG-TST-REL-AUTHZ-001
```

### Required evidence

- ordinary background/non-HTTP execution does not become privileged merely because there is no user caller;
- `ISystemInternalRequest` is an explicit trusted request contract rather than an ambient bypass;
- `AccessPolicyEngineCharacterizationTests.SystemInternalRequest_WithoutUser_BypassesAuth_CallsHandler` remains green unless an explicitly approved architecture migration replaces it;
- trusted Account/Workspace scope for the concrete System flow comes from approved runtime/event data;
- an ordinary external/user request cannot opt into `ApplicationPrincipalKind.System`;
- data-session, idempotency and RLS/runtime safeguards remain valid for tenant persistence touched by the System flow;
- changing/removing the exception requires an explicit migration decision rather than incidental P2 cleanup.

### Preparation-time evidence candidates

- `AccessPolicyEngineCharacterizationTests.cs`
- `RequestDescriptorValidator.cs`
- `ApplicationPrincipalKind.cs`
- `ProvisionPersonalWorkspaceCommand.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; evidence ran on a dirty worktree
  layered on it, so the SHA alone does not identify the tested tree. Immutable candidate
  4878dd0a froze the tested code tree: clean detached worktree, 5152/5152 local tests,
  0 failures. Aggregate CI on 4878dd0a remains required before promotion)
Migration impact: PARTIALLY_VERIFIED — clean-DB migrate green with no pending model changes (CERT-MIG-001);
  no supported-upgrade chain exists for this greenfield baseline (WG-DEBT-CERT-009 open)
Architecture evidence: AuthPipelineArchitectureTests executed green inside Architecture suite 623/623; SystemContextUsageTests enforces the System principal boundary.
Security evidence: AccessPolicyEngineCharacterizationTests executed green; RequestDescriptorValidator is the single seam.
Integration evidence: Provisioning path exercised by ProvisionPersonalWorkspaceCommand inside Application suite 742/742.
CI evidence: NOT_EVALUATED — no aggregate CI run recorded for this SHA (CERT-CI-001..009).
Blocking debt: WG-DEBT-CERT-012 — background/system execution principal model is not fully defined.
Status: PARTIALLY_VERIFIED — System-internal contract proven for release-scoped flows; the general principal model is unresolved.
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## P2B-CERT-007 — AccessFacts composition

### Scope

```text
SPEC: WGREQ083–WGREQ086, WGREQ186
PLAN: WG-AUTHZ-003, WG-RES-003
TESTS: WG-TST-FACTS-INT-001, WG-TST-FACTS-X-001, WG-TST-RES-X-001, WG-TST-AUTHZ-APP-004, WG-TST-AUTHZ-SEC-001, WG-TST-AUTHZ-API-001
```

### Required evidence

- provider runs on active request transaction;
- facts are neutral rather than allow/deny policy;
- Billing commercial decision remains Billing-owned;
- Documents resource facts remain Documents-owned;
- WorkManagement resource fact source is explicitly architecture-classified;
- missing required marker/fact fails closed.

### Preparation-time evidence candidates

- `PostgresAccessFactsProviderTests.cs`
- `PostgresAccessFactsProvider.cs`
- `AccessFactsQuery.cs`
- `IPageAuthorizationFacts`
- `IBillingSubscriptionFacts`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; evidence ran on a dirty worktree
  layered on it, so the SHA alone does not identify the tested tree. Immutable candidate
  4878dd0a froze the tested code tree: clean detached worktree, 5152/5152 local tests,
  0 failures. Aggregate CI on 4878dd0a remains required before promotion)
Migration impact: PARTIALLY_VERIFIED — clean-DB migrate green with no pending model changes (CERT-MIG-001);
  no supported-upgrade chain exists for this greenfield baseline (WG-DEBT-CERT-009 open)
Architecture evidence: NOT_EVALUATED — AccessFacts composition has no dedicated architecture gate in this execution.
Security evidence: PostgresAccessFactsProviderTests prove fail-closed marker behavior, forward scope with adopted billing decision, and that billing is never consulted from the provider.
Integration evidence: PostgresAccessFactsProviderTests executed green against real PostgreSQL inside Infrastructure suite 181/181.
CI evidence: NOT_EVALUATED — no aggregate CI run recorded for this SHA (CERT-CI-001..009).
Blocking debt: None recorded for this capability.
Status: PARTIALLY_VERIFIED — provider semantics proven; WG-TST-SYNC-FACTS-001 is not proven by any record.
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## P2B-CERT-008 — AccessPolicyEngine policy boundary

### Scope

```text
SPEC: WGREQ054, WGREQ080, WGREQ094, WGREQ095, WGREQ185
PLAN: WG-AUTHZ-004
TESTS: WG-TST-AUTHZ-ARCH-002, WG-TST-SEC-MASTER-001, WG-TST-PERM-DOM-001, WG-TST-HANDSHAKE-ARCH-002
```

### Required evidence

- Governance owns permission/rank/resource policy semantics;
- evaluator is persistence-free;
- Common pipeline depends only on neutral evaluator seam;
- one evaluator implementation;
- resource lifecycle/business facts are not reimplemented as Governance persistence logic;
- unknown action/resource combination fails closed.

### Preparation-time evidence candidates

- `AccessPolicyEngine.cs`
- `AuthPipelineArchitectureTests.cs :: AuthorizationPolicyEngine_MustNotReadPersistence`
- `AuthPipelineArchitectureTests.cs :: EvaluatorSeam_HasExactlyOneImplementation`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; evidence ran on a dirty worktree
  layered on it, so the SHA alone does not identify the tested tree. Immutable candidate
  4878dd0a froze the tested code tree: clean detached worktree, 5152/5152 local tests,
  0 failures. Aggregate CI on 4878dd0a remains required before promotion)
Migration impact: PARTIALLY_VERIFIED — clean-DB migrate green with no pending model changes (CERT-MIG-001);
  no supported-upgrade chain exists for this greenfield baseline (WG-DEBT-CERT-009 open)
Architecture evidence: AuthPipelineArchitectureTests :: AuthorizationPolicyEngine_MustNotReadPersistence and :: EvaluatorSeam_HasExactlyOneImplementation executed green inside Architecture suite 623/623.
Security evidence: AccessControlBehaviorTests executed green; the policy boundary is persistence-free.
Integration evidence: NOT_EVALUATED — no runtime integration test proves the persistence-free boundary end to end.
CI evidence: NOT_EVALUATED — no aggregate CI run recorded for this SHA (CERT-CI-001..009).
Blocking debt: None recorded for this capability.
Status: PARTIALLY_VERIFIED — boundary proven statically; runtime proof absent.
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## P2B-CERT-009 — RLS tenant isolation

### Scope

```text
SPEC: WGREQ152, WGREQ187
PLAN: WG-SEC-001, WG-SEC-002
TESTS: WG-TST-RLS-SEC-001, WG-TST-RLS-SEC-002, WG-TST-RLS-SEC-003, WG-TST-SEC-MASTER-002, WG-TST-SYNC-RLS-001
```

### Required evidence

- application role with missing context sees no tenant rows;
- no grant sees no rows;
- cross-account grant cannot reveal another account;
- cross-workspace grant cannot reveal another workspace;
- background scope without grant fails closed;
- session context does not leak across transaction boundaries;
- tests run against role/configuration that actually enforces RLS.

### Preparation-time evidence candidates

- `RlsRuntimeEnforcementTests.cs`
- `RlsPolicyVerificationTests.cs`
- `RlsSessionContextTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; evidence ran on a dirty worktree
  layered on it, so the SHA alone does not identify the tested tree. Immutable candidate
  4878dd0a froze the tested code tree: clean detached worktree, 5152/5152 local tests,
  0 failures. Aggregate CI on 4878dd0a remains required before promotion)
Migration impact: PARTIALLY_VERIFIED — clean-DB migrate green with no pending model changes (CERT-MIG-001);
  no supported-upgrade chain exists for this greenfield baseline (WG-DEBT-CERT-009 open)
Architecture evidence: RlsPolicyVerificationTests.AllWorkspaceScopedTables_HaveRlsEnabled executed green but does NOT cover the workspace/governance/authz schemas — coverage gap recorded as a documentation limitation in CERT-DATA-002.
Security evidence: RlsRuntimeEnforcementTests 18/18 executed green against real PostgreSQL, covering missing-context, no-grant, cross-tenant and transaction-local behavior.
Integration evidence: RlsRuntimeEnforcementTests 18/18 executed green inside Integration suite 587/587.
CI evidence: NOT_EVALUATED — no aggregate CI run recorded for this SHA (CERT-CI-001..009).
Blocking debt: WG-DEBT-CERT-005 — static RLS policy coverage does not cover all owned schemas.
Status: PARTIALLY_VERIFIED — runtime enforcement proven; static policy coverage is incomplete.
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## P2B-CERT-010 — ResourcePermission management security

### Scope

```text
SPEC: WGREQ076–WGREQ079, WGREQ154
PLAN: WG-RPERM-001, WG-ROLE-002
TESTS: WG-TST-RPERM-INT-001, WG-TST-ROLE-SEC-001, WG-TST-CONC-RPERM-001
```

### Required evidence

- resource/subject scope exact;
- management authority required;
- actor cannot grant/revoke above effective ceiling;
- foreign-account target not disclosed/mutated;
- concurrent duplicate active grant resolves safely.

### Preparation-time evidence candidates

- `GovernanceResourcePermissionFlowTests.cs`
- `ResourcePermissionTests.cs`
- `ResourcePermissionLifecycleTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; evidence ran on a dirty worktree
  layered on it, so the SHA alone does not identify the tested tree. Immutable candidate
  4878dd0a froze the tested code tree: clean detached worktree, 5152/5152 local tests,
  0 failures. Aggregate CI on 4878dd0a remains required before promotion)
Migration impact: PARTIALLY_VERIFIED — clean-DB migrate green with no pending model changes (CERT-MIG-001);
  no supported-upgrade chain exists for this greenfield baseline (WG-DEBT-CERT-009 open)
Architecture evidence: NOT_EVALUATED — management ceiling has no dedicated architecture gate in this execution.
Security evidence: GovernanceResourcePermissionFlowTests, ResourcePermissionTests, ResourcePermissionLifecycleTests executed green.
Integration evidence: GovernanceResourcePermissionFlowTests executed green inside Integration suite 587/587.
CI evidence: NOT_EVALUATED — no aggregate CI run recorded for this SHA (CERT-CI-001..009).
Blocking debt: WG-DEBT-CERT-010 — the ShareLink upgrade/E2E path is unconfirmed.
Status: PARTIALLY_VERIFIED — ACL grant/revoke ceiling proven; ShareLink path unconfirmed.
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## P2B-CERT-011 — WorkManagement representative Board handshake

### Scope

```text
SPEC: WGREQ118–WGREQ121, WGREQ182, WGREQ190
PLAN: WG-WM-001, WG-WM-002, WG-WM-003, WG-WM-004
TESTS: WG-TST-WM-X-001, WG-TST-WM-X-002, WG-TST-WM-X-003, WG-TST-WM-X-004, WG-TST-P2-CORE-006, WG-TST-SYNC-HANDOFF-001
```

### Required evidence

- canonical `work-management.board` resource identity;
- authorized owner representative path succeeds;
- outsider representative path is denied;
- cross-tenant resource is not exposed or mutated;
- WorkManagement remains Board semantic owner;
- no private Governance persistence contract is required by WorkManagement.

### Preparation-time evidence candidates

- `CreateBoardInWorkspacePipelineTests.cs`
- `GovernanceResourcePermissionFlowTests.cs`
- `CreateBoardInWorkspaceTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; evidence ran on a dirty worktree
  layered on it, so the SHA alone does not identify the tested tree. Immutable candidate
  4878dd0a froze the tested code tree: clean detached worktree, 5152/5152 local tests,
  0 failures. Aggregate CI on 4878dd0a remains required before promotion)
Migration impact: PARTIALLY_VERIFIED — clean-DB migrate green with no pending model changes (CERT-MIG-001);
  no supported-upgrade chain exists for this greenfield baseline (WG-DEBT-CERT-009 open)
Architecture evidence: AuthPipelineArchitectureTests executed green inside Architecture suite 623/623.
Security evidence: CreateBoardInWorkspacePipelineTests, GovernanceResourcePermissionFlowTests, CreateBoardInWorkspaceTests executed green covering allow, deny and cross-tenant paths.
Integration evidence: GovernanceResourcePermissionFlowTests executed green inside Integration suite 587/587.
CI evidence: NOT_EVALUATED — no aggregate CI run recorded for this SHA (CERT-CI-001..009).
Blocking debt: None recorded for this capability.
Status: PARTIALLY_VERIFIED — representative Board handshake proven; WG-TST-SYNC-HANDOFF-001 not proven.
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

### Notes

- Preparation TESTS gap requires an explicit Board-specific cross-account negative P2 gate case if not already covered by candidate tests.

## P2B-CERT-012 — architecture boundary

### Scope

```text
SPEC: WGREQ129–WGREQ133, WGREQ183–WGREQ186
PLAN: WG-INV-004, WG-INV-005, WG-INV-006, WG-INV-007, WG-INV-008, WG-INV-009, WG-INV-010, WG-AUTHZ-001, WG-AUTHZ-002, WG-AUTHZ-003, WG-AUTHZ-004
TESTS: WG-TST-OWN-ARCH-001, WG-TST-OWN-ARCH-002, WG-TST-AUTHZ-ARCH-001, WG-TST-AUTHZ-ARCH-002, WG-TST-OWN-ARCH-003, WG-TST-OWN-ARCH-004, WG-TST-SYNC-ARCH-001, WG-TST-SYNC-FACTS-001
```

### Required evidence

- Domain purity;
- module-first Application topology;
- IWorkspaceDbContext/IGovernanceDbContext remain owner-local;
- no new foreign private persistence contract;
- AccessPolicyEngine Governance-owned;
- facts/policy/enforcement/isolation split preserved;
- no new production project required.

### Preparation-time evidence candidates

- `DbContextBoundaryArchitectureTests.cs`
- `AuthPipelineArchitectureTests.cs`
- `HandlerAuthorizationBypassArchitectureTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; evidence ran on a dirty worktree
  layered on it, so the SHA alone does not identify the tested tree. Immutable candidate
  4878dd0a froze the tested code tree: clean detached worktree, 5152/5152 local tests,
  0 failures. Aggregate CI on 4878dd0a remains required before promotion)
Migration impact: PARTIALLY_VERIFIED — clean-DB migrate green with no pending model changes (CERT-MIG-001);
  no supported-upgrade chain exists for this greenfield baseline (WG-DEBT-CERT-009 open)
Architecture evidence: DbContextBoundaryArchitectureTests, AuthPipelineArchitectureTests, HandlerAuthorizationBypassArchitectureTests executed green inside Architecture suite 623/623.
Security evidence: HandlerAuthorizationBypassArchitectureTests proves no handler bypasses the canonical pipeline.
Integration evidence: Architecture gates are the authoritative evidence for this capability; no runtime integration test is required by its contract.
CI evidence: NOT_EVALUATED — no aggregate CI run recorded for this SHA (CERT-CI-001..009).
Blocking debt: None recorded for this capability.
Status: PARTIALLY_VERIFIED — architecture boundary proven; WG-TST-SYNC-LAYER-001 not proven.
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## P2B-CERT-013 — migration/startup compatibility

### Scope

```text
SPEC: WGREQ171–WGREQ177
PLAN: WG-MIG-001, WG-MIG-002, WG-MIG-003, WG-MIG-004
TESTS: WG-TST-MIG-MEM-001, WG-TST-MIG-RES-001, WG-TST-MIG-ACT-001, WG-TST-MIG-ROLE-001, WG-TST-MIG-POL-001, WG-TST-MIG-LINK-001, WG-TST-MIG-DB-001, WG-TST-MIG-DB-002, WG-TST-MIG-DB-003, WG-TST-P2-CORE-007
```

### Required evidence

- candidate schema delta inventoried;
- clean database succeeds;
- supported upgrade evaluated when material;
- no pending model changes;
- resource/action/role/permission persisted identifiers remain compatible;
- RLS deployment objects/policies are valid.

### Preparation-time evidence candidates

- `MigrationSmokeTests.cs`
- `MigrationResiliencyTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; evidence ran on a dirty worktree
  layered on it, so the SHA alone does not identify the tested tree. Immutable candidate
  4878dd0a froze the tested code tree: clean detached worktree, 5152/5152 local tests,
  0 failures. Aggregate CI on 4878dd0a remains required before promotion)
Migration impact: VERIFIED — MigrationSmokeTests 4/4 and MigrationResiliencyTests executed green; no pending model changes; single greenfield baseline.
Architecture evidence: NOT_APPLICABLE — startup/migration is not an architecture-boundary capability.
Security evidence: RLS deployment objects validated by RlsRuntimeEnforcementTests 18/18 with the policy pack applied.
Integration evidence: MigrationSmokeTests and MigrationResiliencyTests executed green inside Integration suite 587/587.
CI evidence: NOT_EVALUATED — no aggregate CI run recorded for this SHA (CERT-CI-001..009).
Blocking debt: WG-TEST-GAP-009 NOT_CHANGED — no supported upgrade path was invented for a greenfield baseline.
Status: VERIFIED (clean-DB/startup) / NOT_APPLICABLE (supported-upgrade path).
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

### Notes

- Supported-upgrade evidence is mandatory when candidate changes material Workspace/Governance schema or persisted identifiers.

## P2B-CERT-014 — security baseline

### Scope

```text
SPEC: WGREQ150–WGREQ157
PLAN: WG-SEC-001, WG-SEC-002, WG-SEC-003, WG-SEC-004
TESTS: WG-TST-SEC-MASTER-001, WG-TST-SEC-MASTER-002, WG-TST-SEC-MASTER-003, WG-TST-SEC-MASTER-004, WG-TST-PIPE-SEC-001, WG-TST-SYNC-AUTHZ-001
```

### Required evidence

- privilege-escalation matrix;
- tenant spoofing matrix;
- not-found/resource privacy;
- authorization dependency failures do not allow;
- invitation/share secrets do not leak;
- no security control weakened to make tests pass.

### Preparation-time evidence candidates

- `GovernanceResourcePermissionFlowTests.cs`
- `RlsRuntimeEnforcementTests.cs`
- `HandlerAuthorizationBypassArchitectureTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; evidence ran on a dirty worktree
  layered on it, so the SHA alone does not identify the tested tree. Immutable candidate
  4878dd0a froze the tested code tree: clean detached worktree, 5152/5152 local tests,
  0 failures. Aggregate CI on 4878dd0a remains required before promotion)
Migration impact: PARTIALLY_VERIFIED — clean-DB migrate green with no pending model changes (CERT-MIG-001);
  no supported-upgrade chain exists for this greenfield baseline (WG-DEBT-CERT-009 open)
Architecture evidence: HandlerAuthorizationBypassArchitectureTests executed green inside Architecture suite 623/623.
Security evidence: GovernanceResourcePermissionFlowTests, RlsRuntimeEnforcementTests 18/18 executed green; invitation-token redaction proven by WorkspaceInvitationSecretTelemetryTests.
Integration evidence: GovernanceResourcePermissionFlowTests, RlsRuntimeEnforcementTests executed green inside Integration suite 587/587.
CI evidence: NOT_EVALUATED — no aggregate CI run recorded for this SHA (CERT-CI-001..009).
Blocking debt: WG-DEBT-CERT-008 — no repository secret-scan gate exists and no local scanner is available (P2B-CERT-014 cannot reach VERIFIED).
  WG-DEBT-CERT-014 — SecurityAuditMiddleware very likely never persists security audit events; NOT fixed.
Status: PARTIALLY_VERIFIED — slice-owned security evidence green; full secret-scan certification unavailable.
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

### Notes

- Fail-closed dependency fault injection and secret-telemetry scan were preparation-time gaps and must be evaluated.

## P2B-CERT-015 — CI exact-candidate gate

### Scope

```text
SPEC: WGREQ189, WGREQ190, WGAC018
PLAN: WG-CERT-HO-001, WG-DOC-003
TESTS: WG-TST-P2-CORE-001, WG-TST-P2-CORE-002, WG-TST-P2-CORE-003, WG-TST-P2-CORE-004, WG-TST-P2-CORE-005, WG-TST-P2-CORE-006, WG-TST-P2-CORE-007, WG-TST-SYNC-EVIDENCE-001
```

### Required evidence

- candidate SHA recorded;
- backend preflight/static guards execute;
- architecture/core tests execute;
- API/platform/integration tests execute;
- migration/OpenAPI/docs gates execute as applicable;
- required jobs are non-zero and not skipped;
- aggregate required gate is green or documented repository-approved equivalent.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; evidence ran on a dirty worktree
  layered on it, so the SHA alone does not identify the tested tree. Immutable candidate
  4878dd0a froze the tested code tree: clean detached worktree, 5152/5152 local tests,
  0 failures. Aggregate CI on 4878dd0a remains required before promotion)
Migration impact: NOT_EVALUATED — depends on the candidate CI run, which does not exist.
Architecture evidence: NOT_EVALUATED — depends on the candidate CI run (local Architecture 623/623 does not satisfy the gate).
Security evidence: NOT_EVALUATED — depends on the candidate CI run (local suites do not satisfy the gate).
Integration evidence: NOT_EVALUATED — depends on the candidate CI run (local Integration 587/587 does not satisfy the gate).
CI evidence: NOT_EVALUATED — no aggregate CI run recorded for this SHA (CERT-CI-001..009).
Blocking debt: Immutable candidate 4878dd0a exists and is clean-tree verified (5152/5152 local tests,
  0 failures), but no aggregate CI run has been recorded for it; WG-CERT-HO-001 remains NOT PUBLISHED.
Status: BLOCKED — requires an aggregate CI run on accepted candidate 4878dd0a.
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

### Notes

- Preparation HEAD PR run skipped Backend CI and therefore cannot be used alone.
- Preparation HEAD push run had Backend CI green but aggregate CI failed; therefore it is not a final green candidate record.

## 21. Milestone B gate decision

Milestone B may be recorded:

```text
P2 PROTECTED SLICE CERTIFIED
```

only when all mandatory P2B capability records satisfy their required minimum status.

Minimum downstream stability target:

| Capability | Minimum for P3-B |
|---|---|
| Workspace identity/containment | STABLE / D5 |
| Workspace membership baseline | STABLE / D5 |
| membership access-grant/RLS synchronization | STABLE / D5 |
| ResourceKind/ResourceId | STABLE / D5 |
| PermissionAction | STABLE / D5 |
| Permission/default-deny | STABLE / D5 |
| built-in WorkspaceRole | VERIFIED / D4 minimum; D5 preferred |
| canonical authorization pipeline | STABLE / D5 |
| approved System-internal contract | STABLE / D5 for release-scoped System/internal flows |
| AccessFacts composition | STABLE / D5 |
| AccessPolicyEngine boundary | STABLE / D5 |
| RLS tenant isolation | STABLE / D5 |
| representative WorkManagement handshake | STABLE / D5 |
| architecture | STABLE / D5 |
| migration/startup | STABLE / D5 or documented NOT_APPLICABLE for unchanged dimensions |
| security baseline | STABLE / D5 |
| CI candidate gate | PASS on accepted candidate evidence |

If built-in role remains D4 rather than D5, the record must state exactly which unstable role surface
is excluded and why P3-B does not depend on it.

Initial verdict (recorded at artifact creation):

```text
NOT_EVALUATED
```

Verdict (2026-10-06, from executed local evidence on `e74bbc75f3713d99cead9b535cc935f211a7c47e`
layered on a dirty worktree):

```text
NOT MET
```

Reason: `WG-GATE-002` is not satisfied. Blocking conditions:

- `P2B-CERT-015` = BLOCKED — immutable candidate 4878dd0a exists and is clean-tree verified
  (5152/5152), but no aggregate CI run has been recorded on it; `CERT-CI-001..009` remain NOT_EVALUATED.
- `P2B-CERT-014` = PARTIALLY_VERIFIED — no repository secret-scan gate exists and no local scanner
  is available (`WG-DEBT-CERT-008`).
- `WG-DEBT-CERT-014` OPEN — `SecurityAuditMiddleware` very likely never persists security audit
  events; not fixed.
- 14 of the 16 `P2B-CERT` records are PARTIALLY_VERIFIED; `P2B-CERT-013` is VERIFIED for the
  clean-DB path and NOT_APPLICABLE for the supported-upgrade path, so no record reaches
  STABLE on its own and none of them is supported by an accepted-candidate CI run.

No `P2 PROTECTED SLICE CERTIFIED` statement may be made while any of the above holds.

# Milestone C — Full Workspace & Governance Scope Certification

## 32. Purpose

Milestone C certifies all release-scoped team capabilities.

Unlike Milestone B, this includes secondary Workspace/Governance features.

It still does not require pretending Domain groundwork is delivered.

Every capability is certified to its real layer status.
## FULL-CERT-INV-001 — Invitations

### Scope

```text
SPEC: WGREQ024–WGREQ033, WGREQ146
PLAN: WG-INVITE-001, WG-INVITE-002, WG-INVITE-003
TESTS: WG-TST-INV-DOM-001, WG-TST-INV-INT-001, WG-TST-INV-SEC-001, WG-TST-INV-CONC-001
```

### Required evidence

- Domain state machine;
- Application acceptance/revoke/resend behavior;
- membership transition atomicity;
- replay/race behavior;
- hash/secret safety;
- API/public lookup minimization;
- delivery event contract.

### Preparation-time evidence candidates

- `WorkspaceInvitationTests.cs`
- `InvitationTokenHashTests.cs`
- `AcceptInvitationTransactionEvidenceTests.cs`
- `InvitationEndpointTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: NOT_RECORDED
Migration impact: NOT_EVALUATED
Architecture evidence: NOT_EVALUATED
Security evidence: NOT_EVALUATED
Integration evidence: NOT_EVALUATED
CI evidence: NOT_EVALUATED
Blocking debt: NOT_EVALUATED
Status: NOT_EVALUATED
Reviewer: NOT_RECORDED
Decision date: NOT_RECORDED
```

## FULL-CERT-PROV-001 — Provisioning

### Scope

```text
SPEC: WGREQ008, WGREQ009, WGREQ158
PLAN: WG-PROV-001
TESTS: WG-TST-REL-PROV-001, WG-TST-WSP-APP-002
```

### Required evidence

- producer event/trigger;
- personal Workspace + owner creation;
- tenant scope;
- idempotent retry;
- partial-failure recovery;
- no duplicate canonical Workspace.

### Preparation-time evidence candidates

- `WorkspaceProvisioningConsumerTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: NOT_RECORDED
Migration impact: NOT_EVALUATED
Architecture evidence: NOT_EVALUATED
Security evidence: NOT_EVALUATED
Integration evidence: NOT_EVALUATED
CI evidence: NOT_EVALUATED
Blocking debt: NOT_EVALUATED
Status: NOT_EVALUATED
Reviewer: NOT_RECORDED
Decision date: NOT_RECORDED
```

## FULL-CERT-SET-001 — Workspace settings

### Scope

```text
SPEC: WGREQ010
PLAN: WG-SET-001
TESTS: WG-TST-WSP-ARCH-001, WG-TST-WSP-DOM-004, WG-TST-API-WSP-001
```

### Required evidence

- setting semantic ownership;
- archived/deleted restrictions;
- Application/API delivery;
- no generic settings-to-Governance bypass.

### Preparation-time evidence candidates

- `WorkspaceSettingsTests.cs`
- `UpdateWorkspaceSettingsCommandHandlerTests.cs`
- `WorkspaceSettingsEndpointTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: NOT_RECORDED
Migration impact: NOT_EVALUATED
Architecture evidence: NOT_EVALUATED
Security evidence: NOT_EVALUATED
Integration evidence: NOT_EVALUATED
CI evidence: NOT_EVALUATED
Blocking debt: NOT_EVALUATED
Status: NOT_EVALUATED
Reviewer: NOT_RECORDED
Decision date: NOT_RECORDED
```

## FULL-CERT-HOME-001 — WorkspaceHome

### Scope

```text
SPEC: WGREQ011, WGREQ188
PLAN: WG-HOME-001
TESTS: WG-TST-WSP-APP-003, WG-TST-API-WSP-001, WG-TST-SYNC-LAYER-001
```

### Required evidence

- read-model/composition ownership;
- authorized tenant scope;
- foreign source access uses approved contract/projection;
- delivery layer status is explicit.

### Certification record

```text
Candidate SHA: NOT_RECORDED
Migration impact: NOT_EVALUATED
Architecture evidence: NOT_EVALUATED
Security evidence: NOT_EVALUATED
Integration evidence: NOT_EVALUATED
CI evidence: NOT_EVALUATED
Blocking debt: NOT_EVALUATED
Status: NOT_EVALUATED
Reviewer: NOT_RECORDED
Decision date: NOT_RECORDED
```

### Notes

- Source presence in Application does not automatically imply full product/API completion.

## FULL-CERT-TEAM-001 — Teams

### Scope

```text
SPEC: WGREQ034–WGREQ037
PLAN: WG-TEAM-001
TESTS: WG-TST-TEAM-DOM-001, WG-TST-TEAM-APP-001
```

### Required evidence

- Team lifecycle;
- TeamMember lifecycle;
- Workspace membership precondition;
- last-lead safety;
- Application/API delivery;
- authorization-subject behavior only where supported.

### Preparation-time evidence candidates

- `TeamTests.cs`
- `TeamMemberTests.cs`
- `CreateTeamCommandHandlerTests.cs`
- `ChangeTeamMemberRoleCommandHandlerTests.cs`
- `TeamEndpointTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: NOT_RECORDED
Migration impact: NOT_EVALUATED
Architecture evidence: NOT_EVALUATED
Security evidence: NOT_EVALUATED
Integration evidence: NOT_EVALUATED
CI evidence: NOT_EVALUATED
Blocking debt: NOT_EVALUATED
Status: NOT_EVALUATED
Reviewer: NOT_RECORDED
Decision date: NOT_RECORDED
```

## FULL-CERT-SPACE-001 — Spaces

### Scope

```text
SPEC: WGREQ038–WGREQ041
PLAN: WG-SPACE-001
TESTS: WG-TST-SPACE-DOM-001, WG-TST-SPACE-AUTHZ-001
```

### Required evidence

- Space lifecycle;
- Workspace containment;
- visibility/type semantics;
- Application/API delivery;
- visibility does not become a second action-authorization engine.

### Preparation-time evidence candidates

- `SpaceTests.cs`
- `CreateSpaceCommandHandlerTests.cs`
- `ArchiveSpaceCommandHandlerTests.cs`
- `SpaceEndpointTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: NOT_RECORDED
Migration impact: NOT_EVALUATED
Architecture evidence: NOT_EVALUATED
Security evidence: NOT_EVALUATED
Integration evidence: NOT_EVALUATED
CI evidence: NOT_EVALUATED
Blocking debt: NOT_EVALUATED
Status: NOT_EVALUATED
Reviewer: NOT_RECORDED
Decision date: NOT_RECORDED
```

## FULL-CERT-RULE-001 — Workspace Rules boundary

### Scope

```text
SPEC: WGREQ042, WGREQ043
PLAN: WG-RULE-001
TESTS: WG-TST-RULE-ARCH-001, WG-TST-RULE-ARCH-002
```

### Required evidence

- Workspace rule helpers remain local Workspace business-invariant helpers;
- they do not become a generic Governance permission evaluator;
- they do not duplicate Automation rule-engine semantics;
- no foreign-context persistence or policy source is pulled into Workspace Rules merely for convenience;
- any rule helper introduced by the candidate has a clearly named owning invariant.

### Preparation-time evidence candidates

- `WorkspaceRules` / `WorkspaceMemberRules` / `WorkspaceOwnerRules`
- `WorkspaceInvitationRules`
- `TeamRules` / `TeamLeadRules`
- `SpaceRules`
- architecture/source scans in `WG-TST-RULE-ARCH-001` and `WG-TST-RULE-ARCH-002`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: NOT_RECORDED
Migration impact: NOT_EVALUATED
Architecture evidence: NOT_EVALUATED
Security evidence: NOT_EVALUATED
Integration evidence: NOT_EVALUATED
CI evidence: NOT_EVALUATED
Blocking debt: NOT_EVALUATED
Status: NOT_EVALUATED
Reviewer: NOT_RECORDED
Decision date: NOT_RECORDED
```

## FULL-CERT-CROLE-001 — CustomRole

### Scope

```text
SPEC: WGREQ068–WGREQ070, WGREQ147, WGREQ188
PLAN: WG-CROLE-001
TESTS: WG-TST-CROLE-DOM-001, WG-TST-ROLE-APP-003, WG-TST-ROLE-SEC-001, WG-TST-CONC-ROLE-001, WG-TST-SYNC-LAYER-001
```

### Required evidence

- Domain/persistence layer status;
- Application/API delivery if release-scoped;
- assignment semantics;
- effective authorization composition;
- archive/delete/revoke effect;
- concurrency/security.

### Preparation-time evidence candidates

- `CustomRoleTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: NOT_RECORDED
Migration impact: NOT_EVALUATED
Architecture evidence: NOT_EVALUATED
Security evidence: NOT_EVALUATED
Integration evidence: NOT_EVALUATED
CI evidence: NOT_EVALUATED
Blocking debt: NOT_EVALUATED
Status: NOT_EVALUATED
Reviewer: NOT_RECORDED
Decision date: NOT_RECORDED
```

### Notes

- At preparation audit, Domain/persistence/tests were visible; Application/API runtime delivery must be independently proven.
- Domain-only certification may be VERIFIED for Domain semantics but cannot be called full capability delivery.

## FULL-CERT-POL-001 — WorkspacePolicy

### Scope

```text
SPEC: WGREQ071–WGREQ075, WGREQ148, WGREQ188
PLAN: WG-POL-001
TESTS: WG-TST-POL-DOM-001, WG-TST-POL-APP-001, WG-TST-CONC-POL-001, WG-TST-SYNC-LAYER-001
```

### Required evidence

- Domain/persistence status;
- actual runtime consumer if released;
- precedence/composition against PermissionRule/ResourcePermission;
- stale update behavior;
- fail-closed malformed/unavailable behavior.

### Preparation-time evidence candidates

- `WorkspacePolicyTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: NOT_RECORDED
Migration impact: NOT_EVALUATED
Architecture evidence: NOT_EVALUATED
Security evidence: NOT_EVALUATED
Integration evidence: NOT_EVALUATED
CI evidence: NOT_EVALUATED
Blocking debt: NOT_EVALUATED
Status: NOT_EVALUATED
Reviewer: NOT_RECORDED
Decision date: NOT_RECORDED
```

### Notes

- Preparation audit found Domain/persistence presence but no proof that production authorization evaluates WorkspacePolicy.

## FULL-CERT-RPERM-001 — ResourcePermission full lifecycle

### Scope

```text
SPEC: WGREQ076–WGREQ079, WGREQ147, WGREQ154, WGREQ188
PLAN: WG-RPERM-001, WG-RPERM-002
TESTS: WG-TST-RPERM-DOM-001, WG-TST-RPERM-INT-001, WG-TST-RPERM-INF-001, WG-TST-CONC-RPERM-001, WG-TST-SYNC-LAYER-001
```

### Required evidence

- released grant/read/revoke/change/restore semantics;
- API/Application coverage matches claimed lifecycle;
- rank ceiling;
- concurrency;
- inheritance source/projection semantics if claimed;
- revocation invalidates effective access.

### Preparation-time evidence candidates

- `ResourcePermissionTests.cs`
- `ResourcePermissionLifecycleTests.cs`
- `GovernanceResourcePermissionFlowTests.cs`
- `ResourcePermissionInheritanceCacheEntryTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: NOT_RECORDED
Migration impact: NOT_EVALUATED
Architecture evidence: NOT_EVALUATED
Security evidence: NOT_EVALUATED
Integration evidence: NOT_EVALUATED
CI evidence: NOT_EVALUATED
Blocking debt: NOT_EVALUATED
Status: NOT_EVALUATED
Reviewer: NOT_RECORDED
Decision date: NOT_RECORDED
```

## FULL-CERT-SHARE-001 — ShareLinks

### Scope

```text
SPEC: WGREQ097–WGREQ104, WGREQ149, WGREQ156, WGREQ188
PLAN: WG-SHARE-001, WG-SHARE-002
TESTS: WG-TST-SHARE-DOM-001, WG-TST-SHARE-SEC-001, WG-TST-CONC-SHARE-001, WG-TST-SYNC-LAYER-001
```

### Required evidence

- Domain lifecycle;
- Application/API delivery;
- actual capability-token validation if public consumption is released;
- bounded resource/action authority;
- expiry/disable/revoke;
- secret minimization;
- revoke/use race.

### Preparation-time evidence candidates

- `ShareLinkTests.cs`
- `ShareLinkBoundaryTests.cs`
- `ShareLinkTokenHashTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: NOT_RECORDED
Migration impact: NOT_EVALUATED
Architecture evidence: NOT_EVALUATED
Security evidence: NOT_EVALUATED
Integration evidence: NOT_EVALUATED
CI evidence: NOT_EVALUATED
Blocking debt: NOT_EVALUATED
Status: NOT_EVALUATED
Reviewer: NOT_RECORDED
Decision date: NOT_RECORDED
```

### Notes

- Domain Create/Disable/Rotate evidence does not prove end-to-end public capability-token consumption.

## FULL-CERT-TPL-001 — PermissionTemplate

### Scope

```text
SPEC: WGREQ111–WGREQ113, WGREQ188
PLAN: WG-TPL-001
TESTS: WG-TST-TPL-DOM-001, WG-TST-TPL-APP-001, WG-TST-TPL-MIG-001
```

### Required evidence

- system/workspace scope;
- immutability/lifecycle;
- Application/API delivery if claimed;
- atomic apply semantics if claimed;
- permission/action compatibility;
- version/migration semantics.

### Preparation-time evidence candidates

- `PermissionTemplateLifecycleTests.cs`
- `PermissionTemplateDefinitionTests.cs`
- `PermissionTemplateDefinitionImmutabilityTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: NOT_RECORDED
Migration impact: NOT_EVALUATED
Architecture evidence: NOT_EVALUATED
Security evidence: NOT_EVALUATED
Integration evidence: NOT_EVALUATED
CI evidence: NOT_EVALUATED
Blocking debt: NOT_EVALUATED
Status: NOT_EVALUATED
Reviewer: NOT_RECORDED
Decision date: NOT_RECORDED
```

### Notes

- Preparation audit confirmed rich Domain tests. Template application remains separately evaluated.

## FULL-CERT-AUD-001 — Audit / security-event ownership

### Scope

```text
SPEC: WGREQ105–WGREQ110, WGREQ163, WGREQ164
PLAN: WG-AUD-001, WG-SEC-EVT-001
TESTS: WG-TST-AUD-ARCH-001, WG-TST-AUD-INT-001, WG-TST-AUD-INT-002, WG-TST-AUD-INT-003, WG-TST-AUD-SEC-001, WG-TST-SEVT-DOM-001, WG-TST-OBS-INT-002
```

### Required evidence

- actual source-of-truth mechanism identified;
- critical governance mutations attributable to actor/scope;
- historical records are not free-form logs;
- query access is tenant/security protected;
- no bearer/auth secret material;
- no duplicate security-event source of truth.

### Certification record

```text
Candidate SHA: NOT_RECORDED
Migration impact: NOT_EVALUATED
Architecture evidence: NOT_EVALUATED
Security evidence: NOT_EVALUATED
Integration evidence: NOT_EVALUATED
CI evidence: NOT_EVALUATED
Blocking debt: NOT_EVALUATED
Status: NOT_EVALUATED
Reviewer: NOT_RECORDED
Decision date: NOT_RECORDED
```

### Notes

- Do not fail certification for lack of a Governance/Audit Domain folder if repository architecture intentionally uses another canonical audit mechanism.
- Do block a full-scope audit claim if the actual mechanism cannot be identified/proven.

# Cross-cutting certification
## CERT-ARCH-001 — Domain purity and module boundaries

### Scope

```text
SPEC: WGREQ129–WGREQ133, WGREQ183–WGREQ186
PLAN: architecture work units across PLAN
TESTS: WG-TST-PIPE-ARCH-001, WG-TST-PIPE-ARCH-002, WG-TST-OWN-ARCH-001, WG-TST-OWN-ARCH-002, WG-TST-OWN-ARCH-003, WG-TST-OWN-ARCH-004, WG-TST-OWN-ARCH-005, WG-TST-AUTHZ-ARCH-001, WG-TST-AUTHZ-ARCH-002, WG-TST-SYNC-ARCH-001, WG-TST-SYNC-PERSIST-001
```

### Required evidence

- Workspaces/Governance Domain are framework/provider independent;
- Application module-first topology remains canonical;
- no private cross-context DbContext is used as public contract;
- no duplicate authorization engine;
- no new production project split required.

### Preparation-time evidence candidates

- `DbContextBoundaryArchitectureTests.cs`
- `AuthPipelineArchitectureTests.cs`
- `HandlerAuthorizationBypassArchitectureTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; this evidence ran on a dirty
  worktree layered on top of it, so the SHA alone does not identify the tested tree — an immutable
  candidate is still required before promotion)
Migration impact: No schema change.
Architecture evidence: Notrelix.Architecture.Tests PASS 623/623 (0 fail) on candidate. Verified by method name:
  PipelineOrderTests.Pipeline_behaviors_must_be_registered_in_frozen_order,
  Pipeline_must_have_exactly_7_behaviors,
  DataSession_must_be_after_contract_and_before_access_control,
  Idempotency_must_be_inside_db_scope (frozen pipeline order and DB-scope placement);
  DbContextBoundaryArchitectureTests and DbContextBoundaryTests (owner-scoped DbContext boundary);
  HandlerAuthorizationBypassArchitectureTests.ProtectedHandlers_DoNotInjectPipelineAuthorizationServices,
  ProductionHandlerRoleChecks_AreExactlyRegisteredBusinessInvariants,
  Middleware_DoesNotUseRawEndpointAuthConventions (no handler-local authorization bypass, no ad-hoc
  endpoint auth convention);
  AuthPipelineArchitectureTests.AuthorizationPolicyEngine_MustNotReadPersistence,
  AccessPolicyEngine_IsGovernanceOwned, EvaluatorSeam_HasExactlyOneImplementation,
  AccessControlBehavior_ReferencesOnlyTheSeam_NotTheConcreteEngine (single authorization engine, no
  duplicate, Application consumes only the seam).
Security evidence: no duplicate authorization engine is proven structurally by
  EvaluatorSeam_HasExactlyOneImplementation plus AccessControlBehavior_ReferencesOnlyTheSeam_NotTheConcreteEngine.
Integration evidence: Not applicable to this record; it is an architecture-placement record and no
  cross-boundary runtime behaviour is claimed.
CI evidence: Local focused and full-suite runs only; no aggregate CI run recorded for this SHA.
  CERT-CI-001..009 remain NOT_EVALUATED, so this record cannot be promoted to STABLE on local
  evidence alone.
Blocking debt: 6 of the 11 cited scenarios have no executable evidence at all and are recorded as
  NOT_EVALUATED in the TESTS baseline: WG-TST-PIPE-ARCH-002, WG-TST-OWN-ARCH-003 (one Workspace membership
  truth), WG-TST-OWN-ARCH-004, WG-TST-OWN-ARCH-005, WG-TST-SYNC-ARCH-001, WG-TST-SYNC-PERSIST-001.
  Pipeline order, DbContext boundary and single-engine separation are proven; aggregate-level ownership
  isolation (one membership/role/governance truth) and the cross-context persistence-fact contract are NOT.
Status: PARTIALLY_VERIFIED
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## CERT-DATA-001 — persistence ownership

### Scope

```text
SPEC: WGREQ129–WGREQ133
PLAN: WG-INV-006, WG-MIG-001, WG-MIG-002, WG-MIG-003, WG-MIG-004, WG-MIG-005, WG-MIG-006, WG-MIG-007, WG-MIG-008, WG-MIG-009
TESTS: WG-TST-OWN-ARCH-001, WG-TST-OWN-ARCH-002, WG-TST-OWN-ARCH-003, WG-TST-OWN-ARCH-004, WG-TST-OWN-ARCH-005, WG-TST-MEM-INF-001, WG-TST-RPERM-INT-001, WG-TST-SYNC-PERSIST-001, WG-TST-SYNC-FACTS-001
```

### Required evidence

- one owner per canonical Workspace/Governance state;
- shared ApplicationDbContext implementation does not merge semantic ownership;
- foreign contexts cannot mutate private tables as normal contract;
- indexes/constraints reflect invariants.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; this evidence ran on a dirty
  worktree layered on top of it, so the SHA alone does not identify the tested tree — an immutable
  candidate is still required before promotion)
Migration impact: No schema change made by this execution. Clean-DB apply and identifier compatibility
  are recorded separately in the migration record; supported-upgrade remains open (WG-DEBT-CERT-009).
Architecture evidence: DbContextBoundaryArchitectureTests and DbContextBoundaryTests PASS within
  Architecture.Tests 623/623 — no owner-private DbContext is exposed as a public cross-context contract.
Security evidence: the negative Revoke/Get paths above are the executable evidence for cross-context isolation.
Integration evidence: Notrelix.Integration.Tests GovernanceResourcePermissionFlowTests PASS 52/52, including
  Grant_OnPage_ByOwner_Allows_AndUsesPageAction, Get_OnPage_DoesNotReturnRowsFromAnotherResource and
  Revoke_ForeignAccountTarget_IsNotFound_AndForeignRowStaysActive — a foreign Account targeting another
  Account's resource receives NotFound and the foreign row stays active, so foreign mutation is not a
  normal contract.
CI evidence: Local focused and full-suite runs only; no aggregate CI run recorded for this SHA.
  CERT-CI-001..009 remain NOT_EVALUATED, so this record cannot be promoted to STABLE on local
  evidence alone.
Blocking debt: 6 of the 9 cited scenarios are unproven. WG-TST-MEM-INF-001 records literally
  "No exact existing test was confirmed during preparation audit", and WG-TST-OWN-ARCH-003/004/005,
  WG-TST-SYNC-PERSIST-001, WG-TST-SYNC-FACTS-001 have no executable evidence. Consequently the
  "indexes/constraints reflect invariants" bullet is only partially proven — it is proven for the
  access-grant and resource-permission invariants exercised by the flow tests, not enumerated for all
  Workspace/Governance state.
Status: PARTIALLY_VERIFIED
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## CERT-DATA-002 — RLS and access-grant persistence

### Scope

```text
SPEC: WGREQ152, WGREQ187
PLAN: WG-MEM-004, WG-SEC-002
TESTS: WG-TST-MEM-INT-001, WG-TST-RLS-SEC-001, WG-TST-RLS-SEC-002, WG-TST-RLS-SEC-003, WG-TST-SYNC-RLS-001
```

### Required evidence

- RLS policies installed;
- session context set correctly;
- access grant lifecycle synchronized;
- deny paths proven under enforcing role;
- transaction-local context.

### Preparation-time evidence candidates

- `RlsRuntimeEnforcementTests.cs`
- `RlsPolicyVerificationTests.cs`
- `RlsSessionContextTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; this evidence ran on a dirty
  worktree layered on top of it, so the SHA alone does not identify the tested tree — an immutable
  candidate is still required before promotion)
Migration impact: No schema change. RLS policies and session-context mechanism are pre-existing; this
  record certifies their runtime behaviour, not a new migration.
Architecture evidence: DbContextBoundaryArchitectureTests PASS; RLS is treated as defense in depth behind
  Application authorization, never as its replacement.
Security evidence: deny paths are proven under the enforcing role, and background scope is proven
  restricted to its own rows rather than globally privileged.
Integration evidence: Notrelix.Integration.Tests RlsRuntimeEnforcementTests PASS 18/18 against real
  PostgreSQL under the enforcing application role (NOT EF InMemory), verified by method name:
  AppRole_MissingSessionContext_FailsClosed_SeesNoRows and AppRole_NoGrant_FailsClosed_SeesNoRows
  (fail-closed, no implicit global scope); AppRole_CrossWorkspace_GrantInOneWorkspace_DoesNotSeeOtherWorkspace
  and BackgroundScope_WithGrant_SeesOwnRowsOnly_NoBypass (cross-tenant denial, background scope has no bypass);
  SessionContext_IsTransactionLocal_DoesNotLeakAfterCommit and
  SessionContext_IsTransactionLocal_DoesNotLeakAfterRollback (transaction-local context, no pooled-connection leak);
  RuntimeMembershipCreation_WritesGrant_AndEnforcesUnderAppRole, RuntimeMembershipSuspend_RevokesGrant_AndDeniesUnderAppRole,
  RuntimeMembershipRemoval_RevokesGrant_AndDeniesUnderAppRole (access-grant lifecycle synchronized with membership).
CI evidence: Local focused and full-suite runs only; no aggregate CI run recorded for this SHA.
  CERT-CI-001..009 remain NOT_EVALUATED, so this record cannot be promoted to STABLE on local
  evidence alone.
Blocking debt: WG-TST-SYNC-RLS-001 has no executable evidence. Separately, and materially weakening the
  "RLS policies installed" bullet: Infrastructure RlsPolicyVerificationTests.AllWorkspaceScopedTables_HaveRlsEnabled
  does not cover the workspace, governance or authz schemas and asserts only .NotBeEmpty(), so it is not a
  proof that every workspace-owned table has RLS enabled. Recorded as a known test-quality limitation.
Status: PARTIALLY_VERIFIED
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## CERT-API-001 — Workspace/Governance API contract

### Scope

```text
SPEC: WGREQ134–WGREQ138
PLAN: WG-API-001, WG-API-002
TESTS: WG-TST-API-WSP-001, WG-TST-API-GOV-001, WG-TST-API-OAS-001
```

### Required evidence

- endpoint→Application mapping;
- authentication/security classification;
- stable error taxonomy;
- secret minimization;
- OpenAPI operation identity;
- generated contract drift reviewed.

### Preparation-time evidence candidates

- `Workspace API tests`
- `OpenApiConventionTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; this evidence ran on a dirty
  worktree layered on top of it, so the SHA alone does not identify the tested tree — an immutable
  candidate is still required before promotion)
Migration impact: No schema or persisted change. Public REST shape unchanged by this execution.
Architecture evidence: OpenApiConventionTests PASS within Architecture.Tests 623/623, verified by method
  name: All_endpoint_files_must_have_WithName, OperationIds_must_be_unique,
  OperationIds_must_follow_dotted_format — every endpoint file has a stable dotted operation identity.
  `make docs-check` check-generated PASS: 4 registered generated artifacts, 4 producer-owned drift checks
  executed, so generated-artifact drift is machine-checked rather than hand-reviewed.
Security evidence: secret minimization and error taxonomy are directly proven by
  Notrelix.API.Tests.ProblemDetails.UnexpectedExceptionProblemDetailsTests.ProblemDetails_WhenUnhandledException_Returns500WithSafeDetail,
  which throws an exception carrying "secret-internal-detail" and asserts the public `detail` is the fixed
  string "An unexpected error occurred." while `traceId` is preserved — no internal detail, stack trace or
  provider payload crosses the boundary. Stable taxonomy is covered by the sibling suites
  ValidationProblemDetailsTests, ForbiddenProblemDetailsTests, NotFoundProblemDetailsTests,
  ConflictProblemDetailsTests, GlobalExceptionHandlerTests, ExceptionMappingBehaviorTests and
  LoginErrorContractTests.
Integration evidence: Notrelix.API.Tests endpoint suites PASS 271/271, covering
  WorkspaceAuthTests, WorkspaceMutationTests, MemberEndpointTests, InvitationEndpointTests,
  SpaceEndpointTests, TeamEndpointTests, WorkspaceSettingsEndpointTests (auth classification, public
  invitation-token lookup remains explicitly public, validation/not-found contract stability,
  transport delegates to Application). GovernanceResourcePermissionFlowTests PASS 52/52 proves
  authorization remains Application-owned and canonical resource/action validation applies.
CI evidence: Local focused and full-suite runs only; no aggregate CI run recorded for this SHA.
  CERT-CI-001..009 remain NOT_EVALUATED, so this record cannot be promoted to STABLE on local
  evidence alone.
Blocking debt: none outstanding for the six required-evidence bullets.
Status: VERIFIED
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## CERT-EVT-001 — event ownership and delivery

### Scope

```text
SPEC: WGREQ139–WGREQ143, WGREQ160
PLAN: WG-EVT-001, WG-EVT-002
TESTS: WG-TST-EVT-WSP-001, WG-TST-REL-EVT-001
```

### Required evidence

- producer-owned facts;
- stable Account/Workspace/resource identity;
- safe payloads;
- version/consumer compatibility;
- outbox atomicity;
- retry/idempotency where consumers exist.

### Preparation-time evidence candidates

- `WorkspaceMembershipEventReferenceTests.cs`
- `WorkspaceCreatedOutboxEvidenceTests.cs`
- `WorkspaceMembershipOutboxEvidenceTests.cs`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; this evidence ran on a dirty
  worktree layered on top of it, so the SHA alone does not identify the tested tree — an immutable
  candidate is still required before promotion)
Migration impact: No schema change. Outbox enrollment is pre-existing and covered by existing migrations.
Architecture evidence: producer-owned event identity and stable Account/Workspace scope mapping are
  proven by Notrelix.Application.Tests WorkspaceMembershipEventReferenceTests (PASS within Application
  742/742); Domain events are mapped outward deliberately rather than exposing internal CLR names.
Security evidence: sensitive fields are excluded from payloads by contract — ProtectedToken carries
  [EventSensitiveField] "never be logged", and WorkspaceInvitationSecretTelemetryTests 2/2 proves the raw
  invitation credential is never persisted, returned or emitted in cleartext (disarm-regression-proven).
  Consumer compatibility evidence: duplicate/ordering identity is proven by
  DuplicateDelivery_SameExternalEventId_OneNonTerminalReceipt_OneEnqueuedMessage,
  DuplicateSameSourceTrigger_DatabaseUniqueness_AllowsOnlyOneExecution,
  DuplicateDelivery_ConvergesToSingleReceipt_AndSingleConsumerDecision, and Platform
  OrderingEnforcerTests.DuplicateAfterCommit_IsRejected.
Integration evidence: Notrelix.Integration.Tests PASS 587/587, verified by method name:
  OutboxAtomicityTests.BusinessRowAndOutboxRow_CommitInOneSaveChanges,
  OutboxAtomicityTests.Rollback_RemovesBusinessAndOutboxRowsTogether and
  OutboxAtomicityTests.FailedSaveChanges_RollsBackBoth_AndRestoresRetryIntent (outbox atomicity with the
  source transaction; rollback emits no committed outward fact and preserves retry intent);
  WorkspaceCreatedOutboxEvidenceTests.WorkspaceCreatedFact_OutboxIntent_CommitsAtomically and
  RolledBackWorkspaceCreation_NoCommittedOutwardDelivery;
  WorkspaceMembershipOutboxEvidenceTests.MemberAddedFact_OutboxIntent_CommitsAtomically and
  RolledBackMembershipFact_NoCommittedOutwardDelivery.
CI evidence: Local focused and full-suite runs only; no aggregate CI run recorded for this SHA.
  CERT-CI-001..009 remain NOT_EVALUATED, so this record cannot be promoted to STABLE on local
  evidence alone.
Blocking debt: none outstanding for the six required-evidence bullets. Residual: no external broker-based
  end-to-end delivery run is claimed beyond the real PostgreSQL/MassTransit production-graph evidence.
Status: VERIFIED
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## CERT-CONC-001 — membership/owner concurrency

### Scope

```text
SPEC: WGREQ144, WGREQ145
PLAN: WG-CONC-001
TESTS: WG-TST-CONC-MEM-001, WG-TST-CONC-OWNER-001
```

### Required evidence

- duplicate membership race resolved by database/application concurrency;
- last-owner concurrent mutation cannot leave Workspace ownerless;
- conflict maps deterministically.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; this evidence ran on a dirty
  worktree layered on top of it, so the SHA alone does not identify the tested tree — an immutable
  candidate is still required before promotion)
Migration impact: No schema change. Concurrency closed by a PostgreSQL SELECT ... FOR UPDATE
  workspace row lock behind the new Application port IWorkspaceOwnerUpdateLocker
  (Infrastructure adapter WorkspaceOwnerUpdateLocker, registered in
  Notrelix.Infrastructure/DependencyInjection/PersistenceRegistration.cs).
Architecture evidence: Notrelix.Architecture.Tests full suite PASS 623/623 (0 fail) on candidate
  (IgnoreQueryFilters/RawSql/SystemContextUsage allowlists extended for WorkspaceOwnerUpdateLocker.cs;
  HandlerConstructorPortGate still green for the 5-param handler ctors). Count restated from 617/617
  because three additional architecture gate tests were added by this execution
  (PipelineMetricLabelCardinalityTests 3/3).
Security evidence: RLS defense unchanged; row lock is ordering/atomicity, not authorization. RlsRuntimeEnforcementTests 18/18 PASS.
Integration evidence: WorkspaceMembershipConcurrencyEvidenceTests PASS 2/2 on real PostgreSQL:
  - ConcurrentDuplicateMembershipAdds_DatabaseAllowsOnlyOneMember (WG-TEST-GAP-001, DB unique constraint);
  - ConcurrentDemoteAndRemoveOwners_NeverLeavesZeroActiveOwners (WG-TEST-GAP-002, FOR UPDATE row lock).
  Regression proof: removing the FOR UPDATE from WorkspaceOwnerUpdateLocker makes the last-owner test fail
  (no BusinessRuleException; race silently survives), then restoring the lock passes 3/3 repeated runs.
CI evidence: Local focused runs on candidate SHA; no aggregate CI run recorded for this SHA.
Blocking debt: WG-TEST-GAP-001 / WG-TEST-GAP-002 closed by executed runtime evidence (CERT-CONC-001).
Status: VERIFIED
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-05
```

## CERT-CONC-002 — invitation concurrency

### Scope

```text
SPEC: WGREQ146
PLAN: WG-CONC-002
TESTS: WG-TST-INV-CONC-001
```

### Required evidence

- accept/revoke/resend races deterministic;
- no duplicate membership;
- no stale token generation accepted after rotation.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; this evidence ran on a
  dirty worktree layered on top of it, so the SHA alone does not identify the tested tree.
  Immutable candidate 4878dd0a froze the tested code tree: clean detached worktree,
  5152/5152 local tests, 0 failures. Aggregate CI on 4878dd0a remains required before promotion)
Migration impact: No schema change. Both races are already closed by existing mechanisms:
  (a) aggregate-level optimistic concurrency — ApplicationDbContext.OnModelCreating maps
  AggregateRoot.Version as a concurrency token for every AggregateRoot
  (Notrelix.Infrastructure/Data/ApplicationDbContext.cs:69-79);
  (b) uniqueness on the atomic accept graph — idx_workspace_members_workspace_user,
  ux_access_grants_account_workspace_user, ux_access_grants_account_user_account_level.
Architecture evidence: Notrelix.Architecture.Tests full suite PASS 623/623 (0 fail) on candidate.
Security evidence: invitation lookup is by purpose-bound token hash
  (AcceptInvitationCommandHandler:90-96), so a superseded credential resolves to no row at all
  and cannot be replayed; the stale-token test asserts NotFoundException plus zero granted
  membership and grant. Rotation is proven to replace the persisted hash, not merely re-version it.
Integration evidence: WorkspaceInvitationConcurrencyEvidenceTests PASS 3/3 on real PostgreSQL:
  - ConcurrentAcceptsOfSameInvitation_DatabaseAllowsExactlyOneMembership (WG-TEST-INV-CONC-001)
  - AcceptRacingRevoke_LeavesInvitationStatusAndMembershipConsistent
  - AcceptWithTokenFromSupersededGeneration_IsRejectedAndGrantsNothing
  Interleaving is deterministic (both participants read pre-state and stage before either
  commits); no sleeps or probabilistic retry loops.
  Regression proof: removing .IsConcurrencyToken() from ApplicationDbContext makes
  AcceptRacingRevoke_... fail — the revoke silently overwrites Accepted with Revoked, leaving a
  granted membership owned by a revoked invitation. Restoring the token returns 3/3.
  Finding recorded: the losing concurrent accept is rejected by the account-membership grant
  constraint (ux_access_grants_account_user_account_level), which the accept graph owns
  atomically. The assertion allowlist was widened to admit it rather than narrowing the proof;
  the allowlist remains restricted to constraints owned by the accept graph.
CI evidence: Local focused runs on candidate SHA; no aggregate CI run recorded for this SHA.
Blocking debt: WG-TEST-INV-CONC-001 closed by executed runtime evidence (CERT-CONC-002).
Status: VERIFIED
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## CERT-CONC-003 — governance mutation concurrency

### Scope

```text
SPEC: WGREQ147–WGREQ149
PLAN: WG-CONC-003
TESTS: WG-TST-CONC-RPERM-001, WG-TST-CONC-POL-001, WG-TST-CONC-SHARE-001
```

### Required evidence

- released capability races are tested;
- non-released secondary capability may be NOT_APPLICABLE with rationale;
- no stale write silently broadens authority.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; this evidence ran on a dirty
  worktree layered on top of it, so the SHA alone does not identify the tested tree — an immutable
  candidate is still required before promotion)
Migration impact: No schema change. The released resource-permission race is already closed by an existing
  unique index on the active ACL subject.
Architecture evidence: DbContextBoundaryArchitectureTests PASS; concurrency ownership sits with the
  persistence invariant plus Application mapping, not with handler-local retry logic.
Security evidence: the race is resolved by a database uniqueness constraint on the owning table, so the
  outcome is deterministic rather than last-write-wins by accident.
Integration evidence: Notrelix.Integration.Tests GovernanceResourcePermissionFlowTests PASS 52/52,
  including Concurrent_Grants_ForSameActiveSubject_RaceOnUniqueIndex — the database unique index resolves
  the race and no duplicate active ACL survives. Cross-tenant negative paths in the same suite
  (Get_OnPage_DoesNotReturnRowsFromAnotherResource,
  Revoke_ForeignAccountTarget_IsNotFound_AndForeignRowStaysActive) confirm a stale or foreign principal
  cannot widen authority.
CI evidence: Local focused and full-suite runs only; no aggregate CI run recorded for this SHA.
  CERT-CI-001..009 remain NOT_EVALUATED, so this record cannot be promoted to STABLE on local
  evidence alone.
Blocking debt: 2 of the 3 cited scenarios are unproven and record literally "No exact existing test was
  confirmed during preparation audit": WG-TST-CONC-POL-001 (WorkspacePolicy lost update) and
  WG-TST-CONC-SHARE-001 (post-revocation ShareLink operation). This record permits a non-released
  secondary capability to be recorded NOT_APPLICABLE with rationale, but no release-status rationale has
  been recorded for either, and WG-DEBT-CERT-010 still lists the ShareLink end-to-end capability-token path
  as unconfirmed. Therefore "no stale write silently broadens authority" remains UNPROVEN for
  WorkspacePolicy and ShareLink.
Status: PARTIALLY_VERIFIED
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## CERT-REL-001 — authorization dependency failure

### Scope

```text
SPEC: WGREQ159, WGREQ161
PLAN: WG-REL-001
TESTS: WG-TST-SEC-MASTER-003
```

### Required evidence

- facts/provider/cache failure cannot become allow;
- failure taxonomy is safe;
- protected handler does not execute;
- recovery owner is explicit.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; this evidence ran on a dirty
  worktree layered on top of it, so the SHA alone does not identify the tested tree — an immutable
  candidate is still required before promotion)
Migration impact: None — test-only fault-injection seam over real PostgreSQL; no production code or schema change.
Architecture evidence: Notrelix.Architecture.Tests full suite PASS 623/623 (0 fail) on candidate (AccessControlBehavior stays the single decision point; AccessPolicyEngine unchanged). Count restated from 617/617 because three additional architecture gate tests were added by this execution (PipelineMetricLabelCardinalityTests 3/3).
Security evidence: AuthorizationFailClosedFaultInjectionTests PASS 3/3 on real PostgreSQL:
  - facts-provider outage → InvalidOperationException surfaced, policy NOT evaluated, handler does not run, no durable mutation;
  - policy-evaluator outage → InvalidOperationException surfaced, facts resolved once, handler does not run, no durable mutation;
  - un-faulted admin control → same composition still allows + renames (proves negatives are fail-closed, not a permanent deny).
  Disarm regression: removing the facts fault makes the negative test FAIL (admin succeeds), proving the deny is the fault, not the harness.
Integration evidence: Notrelix.Integration.Tests full suite PASS 587/587 on real PostgreSQL, which
  contains AuthorizationFailClosedFaultInjectionTests and therefore exercises this record's fault
  injection seam inside the composed production graph (Domain → Application pipeline → Infrastructure
  persistence), not only in isolation.
CI evidence: Local focused runs on candidate SHA; no aggregate CI run recorded for this SHA.
  CERT-CI-001..009 remain NOT_EVALUATED, so this record cannot be promoted to STABLE on local
  evidence alone.
Blocking debt: WG-TEST-GAP-007 closed by executed runtime fault-injection evidence (CERT-REL-001).
Status: VERIFIED
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-05
```

## CERT-REL-002 — provisioning/outbox recovery

### Scope

```text
SPEC: WGREQ158, WGREQ160
PLAN: WG-REL-002
TESTS: WG-TST-REL-PROV-001, WG-TST-REL-EVT-001
```

### Required evidence

- source transaction is explicit;
- post-commit delivery retry is explicit;
- duplicate provisioning is prevented;
- rollback does not emit committed outward fact.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; this evidence ran on a dirty
  worktree layered on top of it, so the SHA alone does not identify the tested tree — an immutable
  candidate is still required before promotion)
Migration impact: No schema change. Outbox and consumer registration are pre-existing.
Architecture evidence: producer-owned event identity proven by WorkspaceMembershipEventReferenceTests;
  ordering/ack-after-success is owned by Platform and proven by OrderingEnforcerTests
  RetrySameSequenceAfterFailure_IsAllowed and DuplicateAfterCommit_IsRejected.
Security evidence: duplicate provisioning prevented at the database and consumer seams —
  Notrelix.Infrastructure.Tests WorkspaceProvisioningConsumerTests.RegistrationCompleted_ProvisionsPersonalWorkspaceUnderAccountTenant
  proves at most one canonical personal Workspace/owner membership exists under the correct Account tenant;
  AutomationN8nDurabilityIntegrationTests.DuplicateSameSourceTrigger_DatabaseUniqueness_AllowsOnlyOneExecution;
  CalendarWebhookIntakeIntegrationTests.DuplicateDelivery_SameExternalEventId_OneNonTerminalReceipt_OneEnqueuedMessage;
  CalendarWebhookProcessingRuntimeTests.DuplicateDelivery_ConvergesToSingleReceipt_AndSingleConsumerDecision.
  Infrastructure evidence: Infrastructure.Tests PASS 181/181 including WorkspaceProvisioningConsumerTests.
Integration evidence: Notrelix.Integration.Tests PASS 587/587. Source transaction explicit and rollback
  safety: OutboxAtomicityTests.BusinessRowAndOutboxRow_CommitInOneSaveChanges,
  OutboxAtomicityTests.Rollback_RemovesBusinessAndOutboxRowsTogether,
  OutboxAtomicityTests.FailedSaveChanges_RollsBackBoth_AndRestoresRetryIntent,
  WorkspaceCreatedOutboxEvidenceTests.RolledBackWorkspaceCreation_NoCommittedOutwardDelivery,
  WorkspaceMembershipOutboxEvidenceTests.RolledBackMembershipFact_NoCommittedOutwardDelivery (rollback
  leaves no committed outward fact). Post-commit delivery retry:
  PipelineTelemetryIntegrationTests.Outbox_CommitBeforePublish_DispatchesToConsumer_WithMetricIncrement,
  AutomationN8nDurabilityIntegrationTests.RetryableNetworkFailure_Rethrows_AndSecondAttemptSucceedsWithSameExecutionId,
  N8nDispatchRuntimeChainIntegrationTests.RetryableRateLimit_DurableAttemptEvidence_SurvivesRealMassTransitRetry
  (durable attempt evidence survives a real MassTransit retry).
CI evidence: Local focused and full-suite runs only; no aggregate CI run recorded for this SHA.
  CERT-CI-001..009 remain NOT_EVALUATED, so this record cannot be promoted to STABLE on local
  evidence alone.
Blocking debt: none outstanding for the four required-evidence bullets. Residual: provider-side (external
  billing/email) delivery confirmation is out of this record's scope and is not claimed.
Status: VERIFIED
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## CERT-OBS-001 — authorization and governance observability

### Scope

```text
SPEC: WGREQ162–WGREQ165
PLAN: WG-OBS-001, WG-OBS-002
TESTS: WG-TST-OBS-INT-001, WG-TST-OBS-INT-002, WG-TST-OBS-SEC-001, WG-TST-OBS-METRIC-001
```

### Required evidence

- critical allow/deny categories traceable;
- critical admin mutation attributable;
- safe bounded telemetry fields;
- no secret/policy payload logging;
- denial/security metrics do not use unsafe cardinality.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; this evidence ran on a
  dirty worktree layered on top of it, so the SHA alone does not identify the tested tree.
  Immutable candidate 4878dd0a froze the tested code tree: clean detached worktree,
  5152/5152 local tests, 0 failures. Aggregate CI on 4878dd0a remains required before promotion)
Migration impact: None. New counter on the existing canonical pipeline meter
  (PipelineMetrics.MeterName = Notrelix.Application.Pipeline); no schema, contract, or migration change.
  Change made: the pipeline access-control stage now emits a real authorization decision signal
  rather than only throwing:
  - authorization_decisions counter on PipelineMetrics, labelled only 'decision.kind'
  (code-bounded AccessDecisionKind), recorded at the decision point before exception translation;
  - non-allowed decisions log at Warning via ILogger<AccessControlBehavior<,>> carrying only
  RequestName, PrincipalKind, ResourceKind, PermissionAction, DecisionKind.
Architecture evidence: PipelineMetricLabelCardinalityTests PASS 3/3 enforces the canonical
  label allowlist (decision.kind, error.category, stage) across Notrelix.Application and
  includes a violating fixture proving the gate can fail. Notrelix.Architecture.Tests full
  suite PASS 623/623 (0 fail). AccessPolicyEngine was deliberately left pure — it remains
  gated by PipelineFreezeArchitectureTests.AccessPolicyEngine_RemainsPure.
Security evidence: AuthorizationDecisionTelemetryTests PASS 9/9 proves (a) exactly one
  authorization_decisions measurement per decision kind, (b) denials log at Warning, (c) the
  denial log never contains the AccessDecision.Message, a policy payload, or a resource
  identifier, while the message still reaches the thrown exception as the public contract.
  Denial telemetry is not a second durable audit: no audit row is written on this path.
  Residual: this repository has no production OpenTelemetry collector/exporter, so the new
  counter and log line are produced and provably observable in-process but are not proven to
  reach an external backend.
Integration evidence: Notrelix.Integration.Tests full suite PASS 587/587 (0 fail), which includes
  PipelineTelemetryIntegrationTests proving the pipeline metric/activity graph over real
  PostgreSQL is unchanged by the new instrument.
CI evidence: Local focused runs on candidate SHA; no aggregate CI run recorded for this SHA.
Blocking debt: WG-TEST-OBS-METRIC-001 closed by executed counter + cardinality evidence.
  Residual recorded as non-blocking: no production OTel export path exists in this repository.
Status: VERIFIED
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## CERT-PERF-001 — authorization hot path

### Scope

```text
SPEC: WGREQ166–WGREQ170
PLAN: WG-PERF-001, WG-PERF-002
TESTS: WG-TST-PERF-AUTHZ-001, WG-TST-PERF-MEM-001, WG-TST-PERF-PERM-001, WG-TST-PERF-CACHE-001, WG-TST-PERF-LIST-001
```

### Required evidence

- representative facts/policy path measured;
- query count bounded;
- critical lookups index-supported;
- no N+1 role/rule/permission resolution;
- tenant/security filters preserved;
- no invented threshold if no SLO authority.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; this evidence ran on a dirty
  worktree layered on top of it, so the SHA alone does not identify the tested tree — an immutable
  candidate is still required before promotion)
Migration impact: No schema change.
Architecture evidence: tenant/security filter preservation is structurally protected by
  DbContextBoundaryArchitectureTests and by the RLS runtime evidence cited in the RLS record, but no
  performance characteristic is proven by them.
Security evidence: tenant/security filters are not weakened by this record because no performance
  optimization was introduced; the authorization hot path is unchanged by this execution.
Integration evidence: NONE. This is the honest result and must not be read as an implied pass.
CI evidence: Local focused and full-suite runs only; no aggregate CI run recorded for this SHA.
  CERT-CI-001..009 remain NOT_EVALUATED, so this record cannot be promoted to STABLE on local
  evidence alone.
Blocking debt: 0 of the 5 cited scenarios has any executable evidence. WG-TST-PERF-AUTHZ-001,
  WG-TST-PERF-MEM-001, WG-TST-PERF-PERM-001, WG-TST-PERF-CACHE-001 and WG-TST-PERF-LIST-001 are all
  recorded as "preserved from develop TESTS baseline" with Preparation state NOT_EVALUATED and no
  measurement, query-count bound, index-support proof or N+1 detection anywhere in the repository.
  Consequently none of the measurable bullets is proven: no representative facts/policy path is measured,
  query count is not bounded, critical lookups are not proven index-supported, and N+1 role/rule/permission
  resolution is not proven absent. The "no invented threshold if no SLO authority" bullet is respected only
  by not inventing a threshold — which is not itself positive evidence. A separate local
  PipelineFreezeEvidenceTests exists but is env-gated (RUN_FREEZE_EVIDENCE=1) and therefore contributes no
  non-zero evidence to this record.
Status: PARTIALLY_VERIFIED
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## CERT-MIG-001 — migration and compatibility

### Scope

```text
SPEC: WGREQ171–WGREQ177
PLAN: WG-MIG-001, WG-MIG-002, WG-MIG-003, WG-MIG-004
TESTS: WG-TST-MIG-MEM-001, WG-TST-MIG-RES-001, WG-TST-MIG-ACT-001, WG-TST-MIG-ROLE-001, WG-TST-MIG-POL-001, WG-TST-MIG-LINK-001, WG-TST-MIG-DB-001, WG-TST-MIG-DB-002, WG-TST-MIG-DB-003, WG-TST-P2-CORE-007
```

### Required evidence

- schema diff classified;
- clean DB;
- supported upgrade when material;
- no pending model changes;
- identifier compatibility;
- secret-format compatibility when changed;
- RLS objects/policies valid.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; this evidence ran on a dirty
  worktree layered on top of it, so the SHA alone does not identify the tested tree — an immutable
  candidate is still required before promotion)
Migration impact: NONE — single greenfield baseline 20260702093805_SchemaBaseline; B3 fix added no
  migration/column/index/persisted value. No pending model changes (EF tooling confirms).
Architecture evidence: no-pending-model-changes PASS; schema is workspace/governance self-contained.
Security evidence: RLS deployment objects valid (RlsRuntimeEnforcementTests 18/18 with policy pack applied).
Integration evidence: MigrationSmokeTests PASS 4/4 (148 tables, empty public schema, GIN trigram index,
  outbox required columns) — the clean-DB path that fully covers this candidate's real migration risk.
CI evidence: Local focused runs on candidate SHA; no aggregate CI run recorded for this SHA.
Blocking debt: WG-TEST-GAP-009 NOT_CHANGED (no prior supported schema exists; no synthetic upgrade chain invented).
Status: VERIFIED (clean-DB + no-pending-changes) / NOT_APPLICABLE (supported-upgrade path)
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-05
```

## CERT-X-001 — Identity & Accounts upstream

### Scope

```text
SPEC: WGREQ114–WGREQ117, WGREQ190
PLAN: WG-X-001
TESTS: WG-TST-UP-ARCH-001, WG-TST-UP-INT-001, WG-TST-UP-INT-002, WG-TST-UP-INT-003, WG-TST-SYNC-HANDOFF-001
```

### Required evidence

- P1 producer contract status recorded;
- Actor/User/Account facts consumed through approved boundary;
- no credentials/session private ownership in P2;
- upstream invalidation considered.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; this evidence ran on a dirty
  worktree layered on top of it, so the SHA alone does not identify the tested tree — an immutable
  candidate is still required before promotion)
Migration impact: No schema change; no upstream contract change was made by this execution.
Architecture evidence: Architecture.Tests PASS 623/623 proves no forbidden reference from the P2 projects
  into Identity internals, and Domain purity is enforced structurally; it does NOT prove that P2 consumes
  Actor/User/Account only through an approved boundary.
Security evidence: no test asserts that P2 does not take ownership of credentials or session. Absence of
  evidence is recorded as absence, not as a pass.
Integration evidence: NONE for the cited scenarios. Adjacent but non-substituting evidence exists —
  Notrelix.Integration.Tests PostgresAccessFactsProviderTests (5 tests) proves account/entitlement fact
  resolution executes on the active transaction and adopts the upstream commercial decision, but that is
  an Accounts/Billing fact seam, not the Identity Actor/User boundary this record certifies.
CI evidence: Local focused and full-suite runs only; no aggregate CI run recorded for this SHA.
  CERT-CI-001..009 remain NOT_EVALUATED, so this record cannot be promoted to STABLE on local
  evidence alone.
Blocking debt: 0 of the 5 cited scenarios has executable evidence. WG-TST-UP-ARCH-001 (no Identity
  credential dependency), WG-TST-UP-INT-001 (canonical Account ID only), WG-TST-UP-INT-002,
  WG-TST-UP-INT-003 and WG-TST-SYNC-HANDOFF-001 are all Preparation state NOT_EVALUATED. The required
  bullets "Actor/User/Account facts consumed through approved boundary", "no credentials/session private
  ownership in P2" and "upstream invalidation considered" are therefore UNPROVEN. Note WG-DEBT-CERT-001
  (obsolete AuthorizationBehavior/PermissionService terminology in a historical record) is dispositioned
  but not machine-verified.
Status: PARTIALLY_VERIFIED
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## CERT-X-002 — WorkManagement downstream

### Scope

```text
SPEC: WGREQ118–WGREQ121, WGREQ190
PLAN: WG-X-002, WG-WM-001, WG-WM-002, WG-WM-003, WG-WM-004, WG-WM-005, WG-WM-006, WG-WM-007, WG-WM-008, WG-WM-009
TESTS: WG-TST-WM-X-001, WG-TST-WM-X-002, WG-TST-WM-X-003, WG-TST-WM-X-004, WG-TST-WM-CONTRACT-001, WG-TST-WM-CONTRACT-002, WG-TST-WM-P2-001, WG-TST-WM-P2-002, WG-TST-WM-P2-003, WG-TST-WM-P2-004, WG-TST-SYNC-HANDOFF-001
```

### Required evidence

- Board contract explicit;
- allow/deny/cross-tenant representative paths;
- resource semantic ownership remains WorkManagement;
- P3-A/P3-B handoff status explicit.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; this evidence ran on a dirty
  worktree layered on top of it, so the SHA alone does not identify the tested tree — an immutable
  candidate is still required before promotion)
Migration impact: No schema change.
Architecture evidence: DbContextBoundaryArchitectureTests PASS; WorkManagement resource semantic ownership
  is not usurped by Governance — the negative flows below prove Governance returns NotFound rather than
  reading or mutating a foreign Board/BoardItem.
Security evidence: deny and cross-tenant paths are proven as denial with the target left untouched, not
  merely as a status code.
Integration evidence: Notrelix.Integration.Tests PASS 587/587. Representative allow path:
  WorkManagement/CreateBoardInWorkspacePipelineTests.Owner_ThroughCanonicalPipeline_CreatesBoard_WithDefaultFields
  — through the production MediatR/DI path the owner is allowed, the canonical Board and default fields are
  created by the handler, and no handler-local bypass is required. Representative deny path:
  CreateBoardInWorkspacePipelineTests.Outsider_ThroughCanonicalPipeline_IsForbidden. Cross-tenant paths:
  GovernanceResourcePermissionFlowTests.ArchivePage_WrongTenant_IsNotFound_AndTargetStaysActive and
  CreateComment_OnCrossScopeBoardItem_IsNotFound, plus
  GetBoard_FromForeignAccount_IsNotFound_AndForeignBoardStaysUntouched (recorded in P2B-CERT-011), and
  RlsRuntimeEnforcementTests.AppRole_CrossWorkspace_GrantInOneWorkspace_DoesNotSeeOtherWorkspace.
CI evidence: Local focused and full-suite runs only; no aggregate CI run recorded for this SHA.
  CERT-CI-001..009 remain NOT_EVALUATED, so this record cannot be promoted to STABLE on local
  evidence alone.
Blocking debt: 8 of the 11 cited scenarios are unproven. WG-TST-WM-X-004 has only
  "AccessFactsQuery.cs preparation audit" and "backend architecture closure flow cards" as evidence — no
  executable test. WG-TST-WM-CONTRACT-001, WG-TST-WM-CONTRACT-002, WG-TST-WM-P2-001..004 and
  WG-TST-SYNC-HANDOFF-001 have no executable evidence at all. The explicit P3-A/P3-B handoff status bullet
  is therefore NOT satisfied by evidence.
Status: PARTIALLY_VERIFIED
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## CERT-X-003 — Documents / Collaboration

### Scope

```text
SPEC: WGREQ122, WGREQ123, WGREQ186
PLAN: WG-X-003, WG-X-004
TESTS: WG-TST-DOC-X-001, WG-TST-COL-X-001
```

### Required evidence

- Documents resource facts use producer-owned seam;
- comment authorization uses target resource contract;
- no private Documents/Collaboration persistence becomes Governance contract.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; this evidence ran on a dirty
  worktree layered on top of it, so the SHA alone does not identify the tested tree — an immutable
  candidate is still required before promotion)
Migration impact: No schema change.
Architecture evidence: DbContextBoundaryArchitectureTests PASS. The seam is explicit in source:
  Notrelix.Infrastructure Governance PostgresAccessFactsProvider implements IPageAuthorizationFacts, so
  Documents resource facts arrive through a producer-owned seam and no private Documents/Collaboration
  persistence is exposed as a Governance contract.
Security evidence: Get_OnPage_DoesNotReturnRowsFromAnotherResource proves a foreign resource is not
  readable, so the seam does not leak cross-tenant Documents data.
Integration evidence: Notrelix.Integration.Tests GovernanceResourcePermissionFlowTests PASS 52/52,
  including Grant_OnPage_ByOwner_Allows_AndUsesPageAction — comment/permission flows on a Documents Page
  authorize through the canonical page action over the producer seam.
CI evidence: Local focused and full-suite runs only; no aggregate CI run recorded for this SHA.
  CERT-CI-001..009 remain NOT_EVALUATED, so this record cannot be promoted to STABLE on local
  evidence alone.
Blocking debt: 1 of the 2 cited scenarios is unproven. WG-TST-COL-X-001 (comment authorization using the
  target resource contract) has no executable evidence; only the Documents page path is proven. The
  Collaboration half of this record is therefore UNPROVEN.
Status: PARTIALLY_VERIFIED
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## CERT-X-004 — Billing

### Scope

```text
SPEC: WGREQ124, WGREQ125, WGREQ186
PLAN: WG-X-005
TESTS: WG-TST-BILL-X-001
```

### Required evidence

- Billing owns subscription/capability comparison;
- Governance consumes neutral commercial fact;
- billing-admin authorization remains separate from entitlement.

### Preparation-time evidence candidates

- `PostgresAccessFactsProviderTests.cs subscription scenarios`

These are source/evidence candidates only. They are not a recorded PASS until executed on the certification candidate.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; this evidence ran on a dirty
  worktree layered on top of it, so the SHA alone does not identify the tested tree — an immutable
  candidate is still required before promotion)
Migration impact: No schema change.
Architecture evidence: role-based authorization and commercial entitlement resolution are structurally
  separate in the frozen pipeline — AccessControlBehavior runs before handler access-fact resolution
  (PipelineOrderTests.Pipeline_behaviors_must_be_registered_in_frozen_order,
  DataSession_must_be_after_contract_and_before_access_control), so billing-admin authority is decided by
  the Governance permission model, never by subscription state. AuthPipelineArchitectureTests proves
  AccessPolicyEngine is the single Governance-owned engine reading no persistence directly.
Security evidence: the fail-closed marker test is the negative case; entitlement absence cannot become allow.
Integration evidence: Notrelix.Integration.Tests PostgresAccessFactsProviderTests against real PostgreSQL,
  verified by method name:
  ResolveAsync_subscription_requirement_forwards_scope_and_adopts_billing_decision — Governance forwards
  the tenant/scope and adopts Billing's decision rather than re-comparing commercial terms;
  ResolveAsync_subscription_requirement_adopts_satisfied_decision;
  ResolveAsync_declared_subscription_without_marker_fails_closed — a declared requirement with no
  upstream marker fails closed instead of defaulting to allow;
  ResolveAsync_without_subscription_requirement_never_consults_billing — Governance never invents a local
  commercial or role decision, which is the executable form of "no local shadow RBAC".
CI evidence: Local focused and full-suite runs only; no aggregate CI run recorded for this SHA.
  CERT-CI-001..009 remain NOT_EVALUATED, so this record cannot be promoted to STABLE on local
  evidence alone.
Blocking debt: none outstanding for the three required-evidence bullets. Note the TESTS baseline recorded this
  scenario's evidence only as the vague line "PostgresAccessFactsProviderTests subscription scenarios";
  it is upgraded here to the four concrete test methods above, all verified present in source.
Status: VERIFIED
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## CERT-X-005 — Automation / Integrations

### Scope

```text
SPEC: WGREQ126, WGREQ127
PLAN: WG-X-006
TESTS: WG-TST-INTG-X-001, WG-TST-AUTO-X-001
```

### Required evidence

- background/system principal is explicit;
- tenant scope reconstructed;
- ManageIntegrations uses Governance permission semantics;
- no local shadow RBAC.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; this evidence ran on a dirty
  worktree layered on top of it, so the SHA alone does not identify the tested tree — an immutable
  candidate is still required before promotion)
Migration impact: No schema change.
Architecture evidence: ManageIntegrations uses Governance permission semantics in source — the
  AccessPolicyEngine ManageIntegrations branch resolves the permission through the single Governance-owned
  engine, so there is no local shadow RBAC in the Integrations/Automation slice.
Security evidence: no test asserts the background/system principal is explicit or that tenant scope is
  reconstructed before an Automation/Integration effect.
Integration evidence: NONE for this record. Adjacent but non-substituting evidence:
  RlsRuntimeEnforcementTests.BackgroundScope_WithGrant_SeesOwnRowsOnly_NoBypass proves background database
  scope is row-restricted, but that is RLS session scope, NOT Automation execution-principal identity or
  tenant-scope reconstruction. It must not be cited as proof of this record's second bullet.
CI evidence: Local focused and full-suite runs only; no aggregate CI run recorded for this SHA.
  CERT-CI-001..009 remain NOT_EVALUATED, so this record cannot be promoted to STABLE on local
  evidence alone.
Blocking debt: 2 of the 2 cited scenarios lack executable evidence. WG-TST-INTG-X-001 is SOURCE-ONLY
  ("AccessPolicyEngine source branch for ManageIntegrations") with no asserting test, and
  WG-TST-AUTO-X-001 has no evidence at all. Therefore "background/system principal is explicit" and "tenant
  scope reconstructed" are UNPROVEN, and Governance ManageIntegrations semantics are proven by source
  reading only, not by execution.
Status: PARTIALLY_VERIFIED
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## CERT-X-006 — Analytics

### Scope

```text
SPEC: WGREQ128
PLAN: WG-X-007
TESTS: WG-TST-ANA-X-001
```

### Required evidence

- Analytics consumes events/facts/projections;
- Analytics is not authorization source of truth;
- no private Governance table query is treated as business contract.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; this evidence ran on a dirty
  worktree layered on top of it, so the SHA alone does not identify the tested tree — an immutable
  candidate is still required before promotion)
Migration impact: No schema change.
Architecture evidence: DbContextBoundaryArchitectureTests PASS — Analytics holds no Governance-private
  persistence contract. That is a structural guard, not evidence of Analytics consuming events/facts.
Security evidence: no Analytics path was changed or evaluated by this execution.
Integration evidence: NONE.
CI evidence: Local focused and full-suite runs only; no aggregate CI run recorded for this SHA.
  CERT-CI-001..009 remain NOT_EVALUATED, so this record cannot be promoted to STABLE on local
  evidence alone.
Blocking debt: the single cited scenario WG-TST-ANA-X-001 has no executable evidence — Preparation state
  NOT_EVALUATED with no measurement, assertion or test anywhere. Therefore none of the three required
  bullets is proven: Analytics consuming events/facts/projections is unproven, the "not authorization source
  of truth" property is only structurally guarded rather than demonstrated, and no test proves Analytics
  avoids treating a private Governance table as a business contract.
Status: PARTIALLY_VERIFIED
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

# Current-source synchronization certification

The synchronized requirements `WGREQ183–WGREQ190` are certified through explicit records rather than
being treated as documentation-only notes.

| Requirement | Exact TEST | Certification owner |
|---|---|---|
| WGREQ183 | `WG-TST-SYNC-ARCH-001` | `CERT-ARCH-001`, `CERT-DOC-001` |
| WGREQ184 | `WG-TST-SYNC-PERSIST-001` | `P2B-CERT-012`, `CERT-DATA-001` |
| WGREQ185 | `WG-TST-SYNC-AUTHZ-001`, `WG-TST-PIPE-SEC-001` | `P2B-CERT-006`, `P2B-CERT-016` |
| WGREQ186 | `WG-TST-SYNC-FACTS-001`, `WG-TST-HANDSHAKE-INT-001` | `P2B-CERT-007`, `P2A-CERT-004` |
| WGREQ187 | `WG-TST-SYNC-RLS-001`, `WG-TST-RLS-SEC-001`, `WG-TST-RLS-SEC-002`, `WG-TST-RLS-SEC-003` | `P2B-CERT-003`, `P2B-CERT-009`, `CERT-DATA-002` |
| WGREQ188 | `WG-TST-SYNC-LAYER-001` | full-scope capability records, `CERT-DOC-003` |
| WGREQ189 | `WG-TST-SYNC-EVIDENCE-001` | `P2B-CERT-015`, `CERT-CI-001`–`CERT-CI-009` |
| WGREQ190 | `WG-TST-SYNC-HANDOFF-001`, `WG-TST-P2-CORE-006` | `P2A-CERT-004`, `P2B-CERT-011`, `CERT-X-002` |

The eight synchronization scenarios are **not** uniformly unproven. Executed local evidence now closes some
of them and leaves others open; per-requirement disposition:

```text
WGREQ183  CERT-ARCH-001 / CERT-DOC-001 / CERT-CONC-001 / CERT-CONC-002 → PARTIALLY_VERIFIED
WGREQ184  CERT-DATA-001 → PARTIALLY_VERIFIED (WG-TST-SYNC-PERSIST-001 unproven)
WGREQ185  P2B-CERT-006 / P2B-CERT-016 → PARTIALLY_VERIFIED (no accepted-candidate CI run)
WGREQ186  P2B-CERT-007 → PARTIALLY_VERIFIED; P2A-CERT-004 → VERIFIED; CERT-X-003, CERT-X-004, CERT-X-005, CERT-X-006
          → PARTIALLY_VERIFIED (WG-TST-SYNC-FACTS-001 not proven by any record)
WGREQ187  CERT-DATA-002 → PARTIALLY_VERIFIED (WG-TST-SYNC-RLS-001 unproven)
WGREQ188  CERT-DOC-003 → PARTIALLY_VERIFIED (WG-TST-SYNC-LAYER-001 unproven)
WGREQ189  CERT-CI-001..009 → NOT_EVALUATED; blocked on exact-candidate aggregate CI
WGREQ190  P2A-CERT-004 → VERIFIED; P2B-CERT-011 → PARTIALLY_VERIFIED; CERT-X-001, CERT-X-002 → PARTIALLY_VERIFIED
          (WG-TST-SYNC-HANDOFF-001 unproven)
```

# Test evidence requirements
## 53. Evidence source

Acceptable evidence may include:

```text
xUnit/TRX/JUnit test result
GitHub Actions job/result
architecture/static gate output
migration command output
OpenAPI diff artifact
PostgreSQL/RLS integration evidence
source classification record for non-runtime architecture claims
```

The evidence type must match the claim.

A static scan cannot prove transaction rollback.

A Domain unit test cannot prove production DI.

A mocked DbSet cannot prove RLS.

A green build cannot prove business authorization.
## 54. Test-result record

For every mandatory scenario:

```text
WG-TST ID:
Candidate SHA:
Project:
Command:
Filter/test:
Provider:
Production DI:
Executed:
Passed:
Failed:
Skipped:
Duration:
Artifact/log:
Result:
Notes:
```

A row is not complete when `Executed` is unknown.
## 55. Zero-execution failure

The following does not satisfy a gate:

```text
command exit code = 0
tests discovered = 0
```

Mandatory test count must be non-zero and relevant.
## 56. Skipped tests

Skipped mandatory security, RLS, concurrency, migration or handoff tests are not PASS.

They produce:

```text
BLOCKED
```

or an explicit NOT_APPLICABLE decision with rationale when the capability is outside release scope.
## 57. Flaky tests

Repeated rerun until green without root-cause resolution is not D5 evidence.

A flaky mandatory security/concurrency/isolation test is a stability risk.
# CI certification
## 58. Candidate SHA

Final record must say:

```text
Candidate branch:
Candidate SHA:
Candidate tree:
Working tree clean/equivalent:
```

All required CI evidence must be attributable to that candidate under repository governance.
## 59. Expected conceptual jobs

At minimum evaluate the repository equivalents of:

```text
Backend preflight/static guards
Architecture and core tests
API/platform/integration tests
Backend CI gate
Infrastructure topology/stack health where required
Container/backend image validation where required
Dependency security
OpenAPI/generated contract drift
Documentation governance
aggregate final CI gate
CodeQL/security analysis where required
```

Path filtering must be reported explicitly.
## CERT-CI-001 — Backend preflight/static guards

### Scope

```text
SPEC: WGREQ189
PLAN: WG-CERT-HO-001, WG-DOC-001, WG-DOC-002, WG-DOC-003, WG-DOC-004
TESTS: WG-TST-SYNC-EVIDENCE-001
```

### Required evidence

- job executed when required;
- result attributable to candidate SHA;
- not silently satisfied by skip;
- failure/exception rationale recorded.

### Certification record

```text
Candidate SHA: NOT_RECORDED
Migration impact: NOT_EVALUATED
Architecture evidence: NOT_EVALUATED
Security evidence: NOT_EVALUATED
Integration evidence: NOT_EVALUATED
CI evidence: NOT_EVALUATED
Blocking debt: NOT_EVALUATED
Status: NOT_EVALUATED
Reviewer: NOT_RECORDED
Decision date: NOT_RECORDED
```

## CERT-CI-002 — Architecture and core tests

### Scope

```text
SPEC: WGREQ189
PLAN: WG-CERT-HO-001, WG-DOC-001, WG-DOC-002, WG-DOC-003, WG-DOC-004
TESTS: WG-TST-SYNC-EVIDENCE-001
```

### Required evidence

- job executed when required;
- result attributable to candidate SHA;
- not silently satisfied by skip;
- failure/exception rationale recorded.

### Certification record

```text
Candidate SHA: NOT_RECORDED
Migration impact: NOT_EVALUATED
Architecture evidence: NOT_EVALUATED
Security evidence: NOT_EVALUATED
Integration evidence: NOT_EVALUATED
CI evidence: NOT_EVALUATED
Blocking debt: NOT_EVALUATED
Status: NOT_EVALUATED
Reviewer: NOT_RECORDED
Decision date: NOT_RECORDED
```

## CERT-CI-003 — API/platform/integration tests

### Scope

```text
SPEC: WGREQ189
PLAN: WG-CERT-HO-001, WG-DOC-001, WG-DOC-002, WG-DOC-003, WG-DOC-004
TESTS: WG-TST-SYNC-EVIDENCE-001
```

### Required evidence

- job executed when required;
- result attributable to candidate SHA;
- not silently satisfied by skip;
- failure/exception rationale recorded.

### Certification record

```text
Candidate SHA: NOT_RECORDED
Migration impact: NOT_EVALUATED
Architecture evidence: NOT_EVALUATED
Security evidence: NOT_EVALUATED
Integration evidence: NOT_EVALUATED
CI evidence: NOT_EVALUATED
Blocking debt: NOT_EVALUATED
Status: NOT_EVALUATED
Reviewer: NOT_RECORDED
Decision date: NOT_RECORDED
```

## CERT-CI-004 — Backend aggregate gate

### Scope

```text
SPEC: WGREQ189
PLAN: WG-CERT-HO-001, WG-DOC-001, WG-DOC-002, WG-DOC-003, WG-DOC-004
TESTS: WG-TST-SYNC-EVIDENCE-001
```

### Required evidence

- job executed when required;
- result attributable to candidate SHA;
- not silently satisfied by skip;
- failure/exception rationale recorded.

### Certification record

```text
Candidate SHA: NOT_RECORDED
Migration impact: NOT_EVALUATED
Architecture evidence: NOT_EVALUATED
Security evidence: NOT_EVALUATED
Integration evidence: NOT_EVALUATED
CI evidence: NOT_EVALUATED
Blocking debt: NOT_EVALUATED
Status: NOT_EVALUATED
Reviewer: NOT_RECORDED
Decision date: NOT_RECORDED
```

## CERT-CI-005 — Infrastructure/RLS/deployment gate

### Scope

```text
SPEC: WGREQ189
PLAN: WG-CERT-HO-001, WG-DOC-001, WG-DOC-002, WG-DOC-003, WG-DOC-004
TESTS: WG-TST-SYNC-EVIDENCE-001
```

### Required evidence

- job executed when required;
- result attributable to candidate SHA;
- not silently satisfied by skip;
- failure/exception rationale recorded.

### Certification record

```text
Candidate SHA: NOT_RECORDED
Migration impact: NOT_EVALUATED
Architecture evidence: NOT_EVALUATED
Security evidence: NOT_EVALUATED
Integration evidence: NOT_EVALUATED
CI evidence: NOT_EVALUATED
Blocking debt: NOT_EVALUATED
Status: NOT_EVALUATED
Reviewer: NOT_RECORDED
Decision date: NOT_RECORDED
```

## CERT-CI-006 — Container/runtime image gate

### Scope

```text
SPEC: WGREQ189
PLAN: WG-CERT-HO-001, WG-DOC-001, WG-DOC-002, WG-DOC-003, WG-DOC-004
TESTS: WG-TST-SYNC-EVIDENCE-001
```

### Required evidence

- job executed when required;
- result attributable to candidate SHA;
- not silently satisfied by skip;
- failure/exception rationale recorded.

### Certification record

```text
Candidate SHA: NOT_RECORDED
Migration impact: NOT_EVALUATED
Architecture evidence: NOT_EVALUATED
Security evidence: NOT_EVALUATED
Integration evidence: NOT_EVALUATED
CI evidence: NOT_EVALUATED
Blocking debt: NOT_EVALUATED
Status: NOT_EVALUATED
Reviewer: NOT_RECORDED
Decision date: NOT_RECORDED
```

## CERT-CI-007 — Dependency/security gate

### Scope

```text
SPEC: WGREQ189
PLAN: WG-CERT-HO-001, WG-DOC-001, WG-DOC-002, WG-DOC-003, WG-DOC-004
TESTS: WG-TST-SYNC-EVIDENCE-001
```

### Required evidence

- job executed when required;
- result attributable to candidate SHA;
- not silently satisfied by skip;
- failure/exception rationale recorded.

### Certification record

```text
Candidate SHA: NOT_RECORDED
Migration impact: NOT_EVALUATED
Architecture evidence: NOT_EVALUATED
Security evidence: NOT_EVALUATED
Integration evidence: NOT_EVALUATED
CI evidence: NOT_EVALUATED
Blocking debt: NOT_EVALUATED
Status: NOT_EVALUATED
Reviewer: NOT_RECORDED
Decision date: NOT_RECORDED
```

## CERT-CI-008 — OpenAPI/generated/docs gates

### Scope

```text
SPEC: WGREQ189
PLAN: WG-CERT-HO-001, WG-DOC-001, WG-DOC-002, WG-DOC-003, WG-DOC-004
TESTS: WG-TST-SYNC-EVIDENCE-001
```

### Required evidence

- job executed when required;
- result attributable to candidate SHA;
- not silently satisfied by skip;
- failure/exception rationale recorded.

### Certification record

```text
Candidate SHA: NOT_RECORDED
Migration impact: NOT_EVALUATED
Architecture evidence: NOT_EVALUATED
Security evidence: NOT_EVALUATED
Integration evidence: NOT_EVALUATED
CI evidence: NOT_EVALUATED
Blocking debt: NOT_EVALUATED
Status: NOT_EVALUATED
Reviewer: NOT_RECORDED
Decision date: NOT_RECORDED
```

## CERT-CI-009 — Final aggregate gate

### Scope

```text
SPEC: WGREQ189
PLAN: WG-CERT-HO-001, WG-DOC-001, WG-DOC-002, WG-DOC-003, WG-DOC-004
TESTS: WG-TST-SYNC-EVIDENCE-001
```

### Required evidence

- job executed when required;
- result attributable to candidate SHA;
- not silently satisfied by skip;
- failure/exception rationale recorded.

### Certification record

```text
Candidate SHA: NOT_RECORDED
Migration impact: NOT_EVALUATED
Architecture evidence: NOT_EVALUATED
Security evidence: NOT_EVALUATED
Integration evidence: NOT_EVALUATED
CI evidence: NOT_EVALUATED
Blocking debt: NOT_EVALUATED
Status: NOT_EVALUATED
Reviewer: NOT_RECORDED
Decision date: NOT_RECORDED
```

## 69. Exact-SHA rule

Preferred final condition:

```text
All required CI green on candidate SHA: yes
```

If repository governance permits equivalent-SHA reuse for a documentation-only delta, the record must state:

```text
evidence SHA
candidate SHA
exact diff
why the diff cannot affect the certified claim
which jobs were intentionally not rerun
approval/authority for equivalence
```

Do not assume equivalence merely because Git history shows a parent relationship.
# Documentation certification
## CERT-DOC-001 — canonical execution package

### Scope

```text
SPEC: WGREQ183, WGREQ189
PLAN: WG-DOC-001, WG-DOC-003
TESTS: WG-TST-SYNC-ARCH-001, WG-TST-SYNC-LAYER-001, WG-TST-SYNC-EVIDENCE-001
```

### Required evidence

- SPEC/PLAN/TESTS/CERTIFICATION mutually consistent;
- `executions/` canonical paths used;
- `capability-delivery-map.md` canonical path used;
- current authorization runtime terminology used;
- historical records preserved as historical.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; this evidence ran on a dirty
  worktree layered on top of it, so the SHA alone does not identify the tested tree — an immutable
  candidate is still required before promotion)
Migration impact: Not applicable — documentation-only change; no schema or contract impact.
Architecture evidence: not applicable to this record.
Security evidence: not applicable to this record.
Integration evidence: not applicable to this record. Executable documentation evidence is `make docs-check`
  PASS 6/6 on this worktree:
  check-links PASS — 166 Markdown files, 2926 local targets, 7 anchor checks, 0 reference-style use;
  check-metadata PASS — 93 canonical documents, 93 unique document_id values;
  check-authority PASS — 87 required authored authority paths, 177 files scanned, 13 ADRs, 0 retired links;
  check-rule-ids PASS — 2668 rule declarations, 2668 unique rule ids, 0 collisions;
  check-source-inventory PASS — 5 backend production projects, 9 frontend families, 3 hosts;
  check-generated PASS — 4 registered artifacts, 4 producer-owned drift checks executed.
  This is machine proof that the canonical executions/ paths and the capability-delivery map resolve, and
  that no retired or competing authority link is active.
CI evidence: Local focused and full-suite runs only; no aggregate CI run recorded for this SHA.
  CERT-CI-001..009 remain NOT_EVALUATED, so this record cannot be promoted to STABLE on local
  evidence alone.
Blocking debt: 3 of the 3 cited scenarios have no executable evidence (WG-TST-SYNC-ARCH-001,
  WG-TST-SYNC-LAYER-001, WG-TST-SYNC-EVIDENCE-001). docs-check proves link/authority/identifier integrity,
  but it does NOT prove SPEC/PLAN/TESTS/CERTIFICATION mutual consistency, and it does not prove "current
  authorization runtime terminology" — that was reviewed by hand during this execution. WG-DEBT-CERT-001
  (obsolete AuthorizationBehavior/PermissionService terminology in a historical record) is dispositioned as
  amended but remains unverified by any gate.
Status: PARTIALLY_VERIFIED
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## CERT-DOC-002 — generated/public contracts

### Scope

```text
SPEC: WGREQ138, WGREQ143
PLAN: WG-DOC-002
TESTS: WG-TST-API-OAS-001, WG-TST-EVT-CONTRACT-001, WG-TST-SYNC-EVIDENCE-001
```

### Required evidence

- generated artifacts produced by canonical command;
- semantic diff reviewed;
- no hand-edited generated output;
- consumer compatibility recorded.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; this evidence ran on a dirty
  worktree layered on top of it, so the SHA alone does not identify the tested tree — an immutable
  candidate is still required before promotion)
Migration impact: Not applicable — no generated artifact or producer was modified by this execution.
Architecture evidence: check-source-inventory PASS proves the generated backend/frontend project inventory
  matches the real solution/workspace layout.
Security evidence: not applicable to this record.
Integration evidence: `make docs-check` check-generated PASS — 4 registered generated artifacts, 4
  producer-owned drift checks executed. REST operation identity drift is separately machine-enforced by
  OpenApiConventionTests.All_endpoint_files_must_have_WithName, OperationIds_must_be_unique and
  OperationIds_must_follow_dotted_format.
CI evidence: Local focused and full-suite runs only; no aggregate CI run recorded for this SHA.
  CERT-CI-001..009 remain NOT_EVALUATED, so this record cannot be promoted to STABLE on local
  evidence alone.
Blocking debt: 2 of the 3 cited scenarios are unproven. WG-TST-EVT-CONTRACT-001 has no executable evidence,
  so event-contract consumer compatibility is UNPROVEN. WG-TST-SYNC-EVIDENCE-001 has no executable
  evidence. For the REST side, generated-client consumer compatibility is covered only indirectly by
  check-generated drift and OpenApiConventionTests; no frontend consumer regeneration/compatibility proof
  was run for this candidate, so the "consumer compatibility recorded" bullet rests on the absence of a
  contract change rather than on an executed consumer check. No hand-edited generated output was introduced
  by this change — only hand-authored certification prose.
Status: PARTIALLY_VERIFIED
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

## CERT-DOC-003 — workstream status

### Scope

```text
SPEC: WGREQ188–WGREQ190
PLAN: WG-DOC-003
TESTS: WG-TST-SYNC-LAYER-001, WG-TST-SYNC-HANDOFF-001, WG-TST-SYNC-EVIDENCE-001
```

### Required evidence

- team/capability-delivery status follows certification;
- Domain-only secondary capabilities are not labeled full delivery;
- P3 handoff matches Milestone A/B result.

### Certification record

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e (base commit; this evidence ran on a dirty
  worktree layered on top of it, so the SHA alone does not identify the tested tree — an immutable
  candidate is still required before promotion)
Migration impact: Not applicable — documentation-only change.
Architecture evidence: not applicable to this record.
Security evidence: not applicable to this record.
Integration evidence: `make docs-check` PASS 6/6 confirms the documentation set is internally consistent and
  link-resolvable after the status edits made in this execution.
CI evidence: Local focused and full-suite runs only; no aggregate CI run recorded for this SHA.
  CERT-CI-001..009 remain NOT_EVALUATED, so this record cannot be promoted to STABLE on local
  evidence alone.
Blocking debt: 3 of the 3 cited scenarios have no executable evidence (WG-TST-SYNC-LAYER-001,
  WG-TST-SYNC-HANDOFF-001, WG-TST-SYNC-EVIDENCE-001), so nothing mechanically enforces that team/capability
  status follows certification. What IS true and explicitly recorded by hand in this execution:
  WG-GATE-002 = NOT MET (P2 protected slice not certified); WG-CERT-HO-001 = NOT PUBLISHED;
  P2B-CERT-015 = BLOCKED on exact-candidate aggregate CI; CERT-CI-001..009 = NOT_EVALUATED; and no
  Domain-only secondary capability is labeled full delivery anywhere in this record set — every partially
  evidenced record above is marked PARTIALLY_VERIFIED rather than delivered. The P3 handoff bullet is
  therefore explicitly NOT claimed.
Status: PARTIALLY_VERIFIED
Reviewer: opencode-agent (workspace-governance execution)
Decision date: 2026-10-06
```

# Acceptance-criteria certification traceability

The acceptance criteria are certification gates, not a parallel requirement system.

| Acceptance criterion | Exact TESTS evidence | Certification record |
|---|---|---|
| WGAC001 — Workspace | `WG-TST-WSP-DOM-001`, `WG-TST-WSP-DOM-002`, `WG-TST-WSP-DOM-003`, `WG-TST-WSP-DOM-004`, `WG-TST-WSP-INT-001`, `WG-TST-WSP-APP-001` | `P2B-CERT-001` |
| WGAC002 — Membership | `WG-TST-MEM-DOM-001`, `WG-TST-MEM-DOM-002`, `WG-TST-MEM-INF-001`, `WG-TST-MEM-INT-001`, `WG-TST-CONC-MEM-001`, `WG-TST-CONC-ADMIN-001`, `WG-TST-SYNC-RLS-001` | `P2B-CERT-002`, `P2B-CERT-003` |
| WGAC003 — Invitation | `WG-TST-INV-DOM-001`, `WG-TST-INV-INT-001`, `WG-TST-INV-SEC-001`, `WG-TST-INV-CONC-001` | `FULL-CERT-INV-001` |
| WGAC004 — Resource/action handshake | `WG-TST-RES-ARCH-001`, `WG-TST-RES-CONTRACT-001`, `WG-TST-ACT-DOM-001`, `WG-TST-ACT-ARCH-001`, `WG-TST-SYNC-FACTS-001` | `P2A-CERT-002`, `P2A-CERT-003`, `P2A-CERT-004` |
| WGAC005 — Permission/effective authorization | `WG-TST-PERM-APP-001`, `WG-TST-PERM-APP-002`, `WG-TST-PRULE-APP-001`, `WG-TST-AUTHZ-APP-001`, `WG-TST-AUTHZ-SEC-001` | `P2B-CERT-004`, `P2B-CERT-008` |
| WGAC006 — Built-in roles | `WG-TST-ROLE-DOM-001`, `WG-TST-ROLE-APP-001`, `WG-TST-ROLE-SEC-001`, `WG-TST-P2-CORE-004` | `P2B-CERT-005` |
| WGAC007 — Resource permissions | `WG-TST-RPERM-ARCH-001`, `WG-TST-RPERM-APP-001`, `WG-TST-RPERM-INT-001`, `WG-TST-CONC-RPERM-001` | `P2B-CERT-010`, `FULL-CERT-RPERM-001` |
| WGAC008 — Share links | `WG-TST-SHARE-DOM-001`, `WG-TST-SHARE-SEC-001`, `WG-TST-SHARE-INT-001`, `WG-TST-SHARE-CONC-001` | `FULL-CERT-SHARE-001` |
| WGAC009 — Cross-context ownership | `WG-TST-HANDSHAKE-ARCH-001`, `WG-TST-HANDSHAKE-ARCH-002`, `WG-TST-HANDSHAKE-INT-001`, `WG-TST-OWN-ARCH-005`, `WG-TST-SYNC-FACTS-001` | `P2A-CERT-004`, `CERT-X-001`, `CERT-X-002`, `CERT-X-003`, `CERT-X-004`, `CERT-X-005`, `CERT-X-006` |
| WGAC010 — P3 handoff | `WG-TST-WM-P2-001`, `WG-TST-WM-P2-002`, `WG-TST-WM-P2-003`, `WG-TST-WM-P2-004`, `WG-TST-P2-CORE-006`, `WG-TST-SYNC-HANDOFF-001` | `P2B-CERT-011`, `CERT-X-002` |
| WGAC011 — Architecture | `WG-TST-PIPE-ARCH-001`, `WG-TST-PIPE-ARCH-002`, `WG-TST-OWN-ARCH-001`, `WG-TST-OWN-ARCH-002`, `WG-TST-SYNC-ARCH-001`, `WG-TST-SYNC-PERSIST-001` | `P2B-CERT-012`, `CERT-ARCH-001` |
| WGAC012 — Security | `WG-TST-SEC-MASTER-001`, `WG-TST-SEC-MASTER-002`, `WG-TST-SEC-MASTER-003`, `WG-TST-SEC-MASTER-004`, `WG-TST-PIPE-SEC-001`, `WG-TST-SYNC-RLS-001` | `P2B-CERT-014`, `P2B-CERT-016`, `CERT-DATA-002` |
| WGAC013 — Data ownership | `WG-TST-OWN-ARCH-001`, `WG-TST-OWN-ARCH-002`, `WG-TST-OWN-ARCH-003`, `WG-TST-OWN-ARCH-004`, `WG-TST-OWN-ARCH-005`, `WG-TST-RES-ARCH-001A`, `WG-TST-RES-ARCH-001B`, `WG-TST-RES-ARCH-001C` | `CERT-DATA-001`, `P2A-CERT-004` |
| WGAC014 — Migration | `WG-TST-MIG-MEM-001`, `WG-TST-MIG-RES-001`, `WG-TST-MIG-ACT-001`, `WG-TST-MIG-ROLE-001`, `WG-TST-MIG-POL-001`, `WG-TST-MIG-LINK-001`, `WG-TST-MIG-DB-001`, `WG-TST-MIG-DB-002`, `WG-TST-MIG-DB-003` | `P2B-CERT-013`, `CERT-MIG-001` |
| WGAC015 — Concurrency | `WG-TST-CONC-MEM-001`, `WG-TST-CONC-ADMIN-001`, `WG-TST-CONC-INV-001`, `WG-TST-CONC-ROLE-001`, `WG-TST-CONC-POL-001`, `WG-TST-CONC-SHARE-001` | `CERT-CONC-001`, `CERT-CONC-002`, `CERT-CONC-003` |
| WGAC016 — Observability | `WG-TST-OBS-INT-001`, `WG-TST-OBS-INT-002`, `WG-TST-OBS-SEC-001`, `WG-TST-OBS-METRIC-001`, `WG-TST-AUD-INT-002` | `CERT-OBS-001`, `FULL-CERT-AUD-001` |
| WGAC017 — Performance | `WG-TST-PERF-AUTHZ-001`, `WG-TST-PERF-MEM-001`, `WG-TST-PERF-PERM-001`, `WG-TST-PERF-CACHE-001`, `WG-TST-PERF-LIST-001` | `CERT-PERF-001` |
| WGAC018 — CI | `WG-TST-SYNC-EVIDENCE-001`, `WG-TST-P2-CORE-001`, `WG-TST-P2-CORE-002`, `WG-TST-P2-CORE-003`, `WG-TST-P2-CORE-004`, `WG-TST-P2-CORE-005`, `WG-TST-P2-CORE-006`, `WG-TST-P2-CORE-007` | `P2B-CERT-015`, `CERT-CI-001`–`CERT-CI-009` |

# Semantic debt → certification disposition

`WG-DEBT-*` is the stable semantic/source-debt namespace owned by the PR-WG-00 decision record.
`WG-DEBT-CERT-*` is retained only for certification-evidence gaps. The two namespaces MUST NOT be
interpreted as independent or competing debt catalogs.

| Semantic debt | Current disposition | Certification/evidence relationship | Milestone effect |
|---|---|---|---|
| `WG-DEBT-001` | SUPERSEDED / PARTIALLY CLOSED | `P2A-CERT-004`, `P2B-CERT-007`; old evaluator→`IWorkManagementDbContext` dependency is gone | no direct blocker if current boundary evidence passes |
| `WG-DEBT-002` | PARTIALLY CLOSED | `P2B-CERT-005` | blocks role D5 if built-in baseline remains ambiguous |
| `WG-DEBT-003` | PARTIALLY CLOSED | `P2B-CERT-010`, `FULL-CERT-RPERM-001` | blocks ResourcePermission claims beyond proven rank/ceiling semantics |
| `WG-DEBT-004` | OPEN SECONDARY | `WG-DEBT-CERT-012`, `FULL-CERT-POL-001` | does not block Milestone B unless protected slice starts consuming WorkspacePolicy |
| `WG-DEBT-005` | OPEN / BOUNDED CORE SUBSET | `P2B-CERT-004` | unsupported subject kinds remain outside certified core |
| `WG-DEBT-006` | OPEN ARCHITECTURE DEBT | `P2A-CERT-004`, `P2B-CERT-007`, `CERT-X-002` | MUST be named in P3 handoff; no silent expansion of `AccessFactsQuery` foreign-table reads |
| `WG-DEBT-007` | OPEN DOWNSTREAM CONTRACT DEBT | `P2B-CERT-011`, `CERT-X-002` | MUST be named in P3 handoff; full generic `ManageBoard` semantics are not D5 |
| `WG-DEBT-008` | CLOSED BY EXECUTED EVIDENCE | `WG-DEBT-CERT-004`, `P2B-CERT-003`; suspend/remove grant revocation proven by `RlsRuntimeEnforcementTests` under the RLS app role on real PostgreSQL | no longer blocks Milestone B; residual stale-grant convergence after revoke stays open in `P2B-CERT-003` |
| `WG-DEBT-009` | CLOSED BY EXECUTED EVIDENCE | `WG-DEBT-CERT-003`, `CERT-CONC-001`; `ConcurrentDemoteAndRemoveOwners_NeverLeavesZeroActiveOwners` PASS (FOR UPDATE row lock) | no longer blocks Milestone B |
| `WG-DEBT-010` | CLOSED BY EXECUTED EVIDENCE | `WG-DEBT-CERT-002`, `CERT-CONC-001`; `ConcurrentDuplicateMembershipAdds_DatabaseAllowsOnlyOneMember` PASS (unique-constraint loser) | no longer blocks Milestone B |
| `WG-DEBT-011` | OPEN SECONDARY | `WG-DEBT-CERT-012`, `FULL-CERT-POL-001` | blocks WorkspacePolicy full-release claim only |
| `WG-DEBT-012` | OPEN SECONDARY | `WG-DEBT-CERT-011`, `FULL-CERT-CROLE-001` | blocks CustomRole full-release claim only |
| `WG-DEBT-013` | OPEN SECONDARY | `WG-DEBT-CERT-010`, `FULL-CERT-SHARE-001` | blocks public ShareLink capability claim only |
| `WG-DEBT-014` | OPEN SECONDARY | `WG-DEBT-CERT-013`, `FULL-CERT-TPL-001` | blocks PermissionTemplate apply claim only |
| `WG-DEBT-015` | OPEN DOC/SOURCE-COMMENT DEBT | `WG-DEBT-CERT-001`, `CERT-DOC-001` | no runtime blocker; must not be used as current architecture evidence |

Certification-only evidence gaps with no one-to-one semantic-debt ID remain valid where they represent
test/CI/operational evidence rather than a semantic source defect, including fault injection, secret
telemetry scan, supported-upgrade evidence and explicit Board cross-account proof.

# Source-debt policy
## 73. Blocking debt

Debt blocks STABLE/D5 when it can affect:

```text
tenant isolation
privilege escalation
canonical security authority
resource/action compatibility
membership uniqueness/owner safety
access-grant/RLS revocation
candidate migration correctness
protected downstream contract
required CI evidence
```
## 74. Non-blocking debt

Debt may remain non-blocking when:

- it is outside the certified milestone;
- its semantic boundary is explicit;
- no certified consumer depends on it;
- it cannot widen authority or corrupt canonical state;
- ownership and follow-up are recorded.

Typical potential Milestone-B non-blocking secondary debt:

```text
CustomRole full administration
WorkspacePolicy full runtime composition
PermissionTemplate application
advanced ShareLink consumption
advanced ResourcePermission inheritance
advanced audit/security-event product surface
```

Only actual candidate evidence can confirm that these are non-blocking.
## 75. Debt record format

```text
Debt ID:
Capability:
Candidate SHA:
Description:
Security impact:
Data/contract impact:
Blocks Milestone A?:
Blocks Milestone B?:
Blocks Milestone C?:
Owner:
Required closure:
Target:
Status:
```
## 76. Preparation-time source-debt register

| ID | Preparation observation | Milestone effect before execution |
|---|---|---|
| WG-DEBT-CERT-001 | Historical PR-WG-00 uses obsolete AuthorizationBehavior/PermissionService terminology. | Docs/decision record must be amended; does not alone prove runtime blocker. |
| WG-DEBT-CERT-002 | Exact DB duplicate-membership race test not confirmed. | Potential Milestone B blocker. |
| WG-DEBT-CERT-003 | Concurrent last-owner transaction proof not confirmed. | Potential Milestone B blocker. |
| WG-DEBT-CERT-004 | Suspend/remove → access-grant revocation runtime proof not confirmed. | Potential Milestone B blocker. |
| WG-DEBT-CERT-005 | Same-priority PermissionRule/time-window integration coverage may be incomplete. | Milestone B blocker if relied upon by protected slice. |
| WG-DEBT-CERT-006 | Board-specific cross-account P2 gate case should be explicit. | Potential Milestone B blocker. |
| WG-DEBT-CERT-007 | Authorization dependency fault-injection proof not confirmed. | Blocks D5 reliability claim until evaluated. |
| WG-DEBT-CERT-008 | Secret telemetry scan not confirmed. | Blocks full security claim until evaluated. |
| WG-DEBT-CERT-009 | Supported upgrade fixture not confirmed. | Blocks migration D5 when candidate has material schema/identifier change. |
| WG-DEBT-CERT-010 | ShareLink end-to-end capability-token path not confirmed. | Does not block Milestone B; blocks ShareLink release claim. |
| WG-DEBT-CERT-011 | CustomRole runtime/Application/API composition not confirmed. | Does not block Milestone B; blocks CustomRole full release claim. |
| WG-DEBT-CERT-012 | WorkspacePolicy production composition not confirmed. | Does not block Milestone B unless protected slice relies on it. |
| WG-DEBT-CERT-013 | PermissionTemplate application use case not confirmed. | Does not block Milestone B; blocks template-apply claim. |
| WG-DEBT-CERT-014 | Audit/SecurityEvent product authority requires current-source classification. | Does not block Milestone B unless required by security/audit release contract. |

All rows above are preparation observations, not final debt dispositions.

# Stop conditions during certification
## 77. WG-CERT-STOP-001 — candidate SHA changes

Material source changes after evidence collection require reevaluation.

## 78. WG-CERT-STOP-002 — P1 upstream contract invalidated

P2 cannot certify against stale Actor/Account/Tenant producer semantics.

## 79. WG-CERT-STOP-003 — membership uniqueness/owner race unresolved

Protected Workspace administration cannot be stable.

## 80. WG-CERT-STOP-004 — duplicate authorization mechanism required

P2 cannot certify two competing policy/enforcement authorities.

## 81. WG-CERT-STOP-005 — facts/policy ownership unresolved

Resource owner and Governance disagree on source of semantic truth.

## 82. WG-CERT-STOP-006 — RLS not actually enforced in tests

Tenant isolation evidence is invalid.

## 83. WG-CERT-STOP-007 — stale access grant after membership revoke

Persistence isolation can outlive authoritative membership.

## 84. WG-CERT-STOP-008 — privilege escalation path

Any actor can acquire/manage authority above accepted policy.

## 85. WG-CERT-STOP-009 — pending migration/model drift

Candidate database model is not deployment-stable.

## 86. WG-CERT-STOP-010 — required test skipped/zero

Mandatory evidence is absent.

## 87. WG-CERT-STOP-011 — CI SHA mismatch

Historical/local green is being used as final candidate proof without approved equivalence.

## 88. WG-CERT-STOP-012 — breaking resource/action identifier without migration

Downstream/persisted compatibility is unknown.

## 89. WG-CERT-STOP-013 — secret exposure

Invitation/share/auth secret appears in logs/events/public response.

## 90. WG-CERT-STOP-014 — foreign private persistence contract

Certified cross-context flow requires another BC's private DbContext/internal model.

## 91. WG-CERT-STOP-015 — aggregate required CI failure

Required repository final gate is red without accepted waiver/equivalence.

## 92. WG-CERT-STOP-016 — historical decision record treated as current evidence

Stale PR-WG-00 mechanism names are used to certify current runtime.

# Milestone A checklist — P2 Producer Contract

The §5 gate verdict is now MET from executed local evidence on immutable candidate `4878dd0a`
(clean detached worktree, 5152/5152 local tests): `P2A-CERT-001..004` are all `VERIFIED`. Boxes
marked [x] are proven locally on that candidate; boxes left unchecked require the aggregate CI
run or a decision/review that is not yet recorded. A local check means "proven locally",
not "certified for release".

- [x] candidate SHA recorded
- [ ] P1 producer contract still valid
- [x] Workspace identity stable
- [x] Account containment stable
- [x] cross-account negative path proven
- [x] ResourceKind/ResourceId canonical
- [x] PermissionAction ownership inventoried
- [ ] persisted/public identifier compatibility reviewed
- [ ] resource facts ownership explicit
- [x] no private cross-context persistence contract
- [ ] P3-A handoff packet complete
- [x] required tests non-zero
- [x] required architecture gates green

# Milestone B checklist — Protected Slice

Each checked item below is supported by executed local evidence on candidate
`e74bbc75f3713d99cead9b535cc935f211a7c47e` **layered on a dirty worktree** (5149/5149 local tests,
0 failures, plus `make docs-check` 6/6 and no pending model changes). A checked box therefore means
"proven locally", **not** "certified for release". `WG-GATE-002` remains **NOT MET** because the
unchecked items require an exact-candidate aggregate CI run that does not exist yet.

- [ ] all Milestone A checks complete — §5 gate verdict is MET locally, but several Milestone A
      boxes remain unchecked pending the aggregate CI run / recorded handoff.
- [x] Workspace lifecycle verified
- [x] membership lifecycle verified
- [x] membership uniqueness race verified
- [x] last-owner race verified
- [x] membership access-grant sync/revoke verified
- [x] canonical AccessControlBehavior path verified
- [x] pipeline order verified
- [x] AccessFacts active-transaction behavior verified
- [x] AccessPolicyEngine persistence-free and single implementation
- [x] default deny verified
- [x] PermissionRule precedence verified
- [x] built-in role baseline verified
- [x] ACL grant/revoke ceiling verified
- [x] RLS missing-context/no-grant/cross-tenant/transaction-local verified
- [x] Board allow path verified
- [x] Board deny path verified
- [x] Board cross-tenant path verified
- [x] denied handler produces no side effect
- [x] migration/pending-model state clean
- [x] security/fail-closed/secret evidence complete — **slice-owned scope only.** Full secret-scan
      certification remains open: no repository secret-scan gate exists in-repo and no local scanner
      (Trivy/Gitleaks/TruffleHog) is available, so `P2B-CERT-014` is PARTIAL-VERIFIED (`WG-DEBT-CERT-008`).
      Separately, `WG-DEBT-CERT-014` records a confirmed SOURCE_DEFECT in `SecurityAuditMiddleware`
      (durable security audit is very likely never persisted) which is **not fixed** and is out of
      Milestone B scope but blocks any claim that API-level 401/403/429 events are durably recorded.
- [ ] required CI executes on accepted candidate — BLOCKED: `P2B-CERT-015`, `CERT-CI-001..009`
      NOT_EVALUATED. Immutable candidate 4878dd0a exists and is clean-tree verified (5152/5152,
      0 failures); only the aggregate CI run on that SHA is missing.
- [ ] P3-B handoff packet complete — `WG-CERT-HO-001 = NOT PUBLISHED`

# Milestone C checklist — Full Scope
- [ ] Milestone B certified
- [ ] Invitations full released scope certified
- [ ] Provisioning certified
- [ ] Settings certified
- [ ] WorkspaceHome delivery status certified
- [ ] Teams certified
- [ ] Spaces certified
- [ ] CustomRole certified to actual delivered layer
- [ ] WorkspacePolicy certified to actual delivered layer
- [ ] ResourcePermission full released lifecycle certified
- [ ] ShareLinks certified to actual delivered layer
- [ ] PermissionTemplate certified to actual delivered layer
- [ ] Audit/SecurityEvent ownership/delivery certified
- [ ] API/OpenAPI certified
- [ ] Events/messaging certified
- [ ] all applicable concurrency gates certified
- [ ] migration/compatibility certified
- [ ] reliability certified
- [ ] observability/performance evidence complete
- [ ] cross-context contracts certified
- [ ] docs/generated artifacts synchronized
- [ ] no blocking debt remains

# Certification record templates
## 93. Milestone A — P2 Producer Contract Record

```text
Workspace & Governance — P2 Producer Contract

Candidate branch:
Candidate SHA:
Candidate tree:
Date:
Reviewer:

P1 upstream contract:
  status:
  evidence:

P2A-CERT-001 Workspace identity/containment:
  status:
  evidence:

P2A-CERT-002 ResourceKind/ResourceId:
  status:
  evidence:

P2A-CERT-003 PermissionAction:
  status:
  evidence:

P2A-CERT-004 ownership boundary:
  status:
  evidence:

Architecture:
  command/run:
  result:

Tests:
  executed:
  passed:
  failed:
  skipped:
  relevant count:

CI:
  run:
  candidate SHA:
  required jobs:
  skipped jobs:
  result:

Blocking debt:
Non-blocking debt:
Invalidation rules:

Final:
  P2 PRODUCER CONTRACT VERIFIED
  or
  BLOCKED
```
## 94. Milestone B — P2 Protected Slice Record

```text
Workspace & Governance — P2 Protected Slice

Candidate branch:
Candidate SHA:
Candidate tree:
Date:
Reviewer:

Workspace:                         NOT_EVALUATED
Membership:                        NOT_EVALUATED
Membership grant/RLS sync:         NOT_EVALUATED
ResourceKind/ResourceId:           NOT_EVALUATED
PermissionAction:                  NOT_EVALUATED
Permission/default deny:           NOT_EVALUATED
Built-in WorkspaceRole:            NOT_EVALUATED
Canonical access-control pipeline: NOT_EVALUATED
AccessFacts composition:           NOT_EVALUATED
AccessPolicyEngine boundary:       NOT_EVALUATED
RLS tenant isolation:              NOT_EVALUATED
ResourcePermission security:       NOT_EVALUATED
WorkManagement Board handshake:    NOT_EVALUATED
Architecture:                      NOT_EVALUATED
Migration/startup:                 NOT_EVALUATED
Security baseline:                 NOT_EVALUATED
CI candidate gate:                 NOT_EVALUATED

Mandatory WG-TST packet:
  executed:
  passed:
  failed:
  skipped:
  zero-execution:
  artifacts:

Migration:
  clean DB:
  supported upgrade:
  pending model:
  RLS apply/verify:

OpenAPI/docs:
  OpenAPI:
  docs governance:
  generated artifacts:

CI:
  run:
  candidate SHA:
  Backend preflight:
  Architecture/core:
  API/platform/integration:
  Backend gate:
  Infrastructure:
  Containers:
  Dependency security:
  Documentation:
  Final aggregate:
  path-filter/skips:

Blocking debt:
Non-blocking debt:

P3-B handoff:
  Resource contracts:
  Actions:
  Facts:
  Security assumptions:
  Semantic debt:
    - WG-DEBT-006
    - WG-DEBT-007
  Invalidating changes:

Final:
  P2 PROTECTED SLICE CERTIFIED
  or
  BLOCKED
```
## 95. Milestone C — Full Scope Record

```text
Workspace & Governance — Full Scope Certification

Candidate branch:
Candidate SHA:
Candidate tree:
Date:
Reviewer:

P2 protected slice:
  status:
  evidence:

Invitation:
Provisioning:
Settings:
WorkspaceHome:
Teams:
Spaces:
CustomRole:
WorkspacePolicy:
ResourcePermission:
ShareLinks:
PermissionTemplate:
Audit/SecurityEvents:

API/OpenAPI:
Events:
Migration:
Security:
Concurrency:
Reliability:
Observability:
Performance:
Cross-context:
Docs:
CI:

Blocking debt:
Non-blocking debt:

Final:
  WORKSPACE & GOVERNANCE FULL SCOPE CERTIFIED
  or
  BLOCKED
```
# Handoff to WorkManagement
## 96. P3-A handoff condition

P3-A Domain/Data work may rely on P2 only when the needed producer contracts are at least VERIFIED
and stable enough for their specific use.

The handoff packet must include:

```text
Workspace identity
Account containment
ResourceKind/ResourceId
PermissionAction ownership
persisted/public compatibility
facts ownership rules
invalidating changes
```
## 97. P3-B handoff condition

Protected WorkManagement Application/API release requires Milestone B.

The handoff packet must include:

```text
candidate SHA
Workspace contract
membership contract
built-in role baseline
ResourceKind/PermissionAction
AccessControlBehavior contract
AccessFacts fields used by WorkManagement
AccessPolicyEngine policy semantics
RLS assumptions
Board representative allow/deny/cross-tenant tests
migration/OpenAPI compatibility
known debt
D5 invalidation rules
```
## 98. What WorkManagement may depend on

Allowed stable dependencies:

```text
Workspace/Account identity and containment
published resource/action vocabulary
canonical request security declaration contract
approved neutral facts contract
Governance decision behavior
documented public contracts/events
```

WorkManagement MUST NOT depend on:

```text
IGovernanceDbContext
Governance EF entities/configurations
AccessFactsQuery SQL details
private PermissionRule table layout
private ResourcePermission table layout
AccessPolicyEngine concrete internals
Workspace private DbContext
```
### Required open-debt handoff

The P3 handoff MUST explicitly carry these semantic debts until closed:

```text
WG-DEBT-006
  AccessFactsQuery currently performs a supporting composite read of
  WorkManagement Board persistence.
  This is not a Public producer contract and must not be silently expanded.

WG-DEBT-007
  PermissionAction.ManageBoard remains broader than the fully classified
  WorkManagement command/action matrix.
  ViewBoard, ManageBoardPermission and generic ManageBoard are not interchangeable.
```

Milestone implications:

```text
P3-A
  may consume the stable ResourceKind/PermissionAction producer contract only
  within the explicitly documented semantics.

P3-B
  may rely only on the named Board actions proven by the Milestone-B packet.
  It MUST NOT interpret generic ManageBoard as globally D5 until WorkManagement
  inventories and classifies every command that uses it.
```

These debts must appear in the final handoff record under `Known debt` / `Invalidating changes`.

# D5 stability rules after certification
## 99. Breaking changes

The following invalidate relevant D5 contracts and require impact review/recertification:

```text
Workspace Account-containment semantics
WorkspaceMember identity/lifecycle
WorkspaceRole meaning
ResourceKind canonical value
PermissionAction meaning
PermissionRule precedence
ResourcePermission management ceiling
AccessFacts field semantics/source ownership
AccessControl pipeline order/enforcement ownership
AccessPolicyEngine policy behavior
RLS access-grant/session-context semantics
public API/event contract
persisted resource/action/role identifiers
```
## 100. Secondary capability changes

A secondary capability change does not automatically invalidate Milestone B when:

- protected P2/P3 contracts are unchanged;
- effective permission semantics are unchanged;
- no new facts/policy precedence participates in protected slice;
- migrations do not affect certified core tables/identifiers;
- security/isolation assumptions remain valid.

Example:

```text
adding a new PermissionTemplate description field
```

may not invalidate P2 protected-slice D5.

Example:

```text
making CustomRole participate in effective authorization
```

does invalidate authorization-policy certification and requires impact review.
## 101. Certification invalidation record

```text
Change:
Affected certified capability:
Previous candidate:
New SHA:
Semantic impact:
Migration impact:
Security impact:
Downstream consumers:
Required tests:
Recertification required: yes/no
Rationale:
```
# Final Definition of Done
## 102. Milestone A Done

- [ ] exact candidate recorded
- [ ] Workspace identity/containment verified
- [ ] resource identity verified
- [ ] action ownership verified
- [ ] cross-context ownership verified
- [ ] P3-A contract packet explicit
- [ ] required architecture/tests executed
- [ ] no blocking producer-contract debt

## 103. Milestone B Done

- [ ] Milestone A complete
- [ ] membership + owner safety stable
- [ ] grant projection/RLS stable
- [ ] permission/default deny stable
- [ ] built-in role baseline at required maturity
- [ ] canonical authorization pipeline stable
- [ ] approved System-internal contract characterized and bounded
- [ ] facts/policy ownership stable
- [ ] RLS isolation stable
- [ ] Board representative handshake stable
- [ ] migration/security/CI exact-candidate evidence complete
- [ ] P3-B handoff published
- [ ] no blocking protected-slice debt

## 104. Full Workspace & Governance Done

- [ ] Milestone B certified
- [ ] every release-scoped secondary capability has an honest layer/status record
- [ ] no Domain-only capability is mislabeled full delivery
- [ ] API/events/migrations/security/concurrency/reliability/cross-context/docs are certified
- [ ] no blocking debt remains
- [ ] full-scope CI evidence is accepted for candidate
- [ ] workstream/capability map status updated from evidence

## 105. What does not count as Done

None of the following is sufficient:

```text
code compiles
Domain model exists
test file exists
tests passed on another SHA
PR CI is green while Backend CI is skipped
Backend CI is green while required aggregate/deployment gate is red
RLS table is invisible under an owner role that bypasses real policy
handler contains a local role check that makes the test pass
source has CustomRole/Policy/Template classes
README/team doc says feature complete
```
## 106. Final certification rule

The final question for Milestone B is:

```text
Can a trusted Actor operating in the correct Account/Workspace target a
resource with a defined action, receive exactly one deterministic Governance
decision before the protected handler, remain contained by RLS at persistence,
and be denied without side effects when membership, resource scope, policy,
grant or tenant facts do not authorize the operation?
```

If that cannot be proven on accepted candidate evidence:

```text
P2 PROTECTED SLICE is not STABLE
```

The final question for Milestone C is:

```text
Can every release-scoped Workspace/Governance capability be traced from
WGREQ → PLAN → WG-TST → actual candidate execution → CI → final status,
without overstating Domain-only or partial implementation?
```

If not:

```text
WORKSPACE & GOVERNANCE FULL SCOPE is not certified
```
## 107. Initial record at document creation

This artifact is intentionally created with:

```text
P2 Producer Contract:        NOT_EVALUATED
P2 Protected Slice:          NOT_EVALUATED
Full Workspace/Governance:   NOT_EVALUATED
```

Preparation source and CI observations are recorded only to guide execution.

They do not constitute final certification.

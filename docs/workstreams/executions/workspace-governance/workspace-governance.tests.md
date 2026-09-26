---
document_id: WRK-TESTS-WORKSPACE-GOVERNANCE
document_type: workstream-tests
status: active
owner: workspace-governance-team
applies_to:
  - backend
  - workspaces
  - governance
  - workspace
  - membership
  - invitations
  - teams
  - spaces
  - provisioning
  - resource-kind
  - permission-actions
  - permission-rules
  - roles
  - policies
  - resource-permissions
  - share-links
  - permission-templates
  - authorization
  - access-facts
  - rls
  - tenant-isolation
  - migrations
  - events
  - ci
  - workmanagement-handoff
evidence:
  - docs/workstreams/executions/workspace-governance/workspace-governance.spec.md
  - docs/workstreams/executions/workspace-governance/workspace-governance.plan.md
  - docs/workstreams/executions/workspace-governance/workspace-governance.certification.md
  - docs/workstreams/executions/workspace-governance/decisions/PR-WG-00-semantic-inventory.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/capability-delivery-map.md
  - docs/workstreams/cross-team-dependencies.md
  - docs/workstreams/teams/workspace-governance.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/testing-and-quality-gates.md
  - docs/workstreams/executions/backend-team-architecture-closure/backend-team-architecture-closure.spec.md
review_on:
  - workspace-governance-spec-change
  - workspace-governance-plan-change
  - access-control-pipeline-change
  - access-facts-provider-change
  - permission-policy-change
  - role-model-change
  - rls-contract-change
  - public-contract-change
  - migration-change
  - p2-gate-change
  - ci-gate-change
---

# TESTS — Workspace & Governance

## 1. Purpose

This document is the canonical verification design for Workspace & Governance.

It converts the target requirements and PLAN work units into evidence that can be executed against one exact candidate source revision.

The required chain is:

```text
WGREQ
  ↓
PLAN work unit
  ↓
WG-TST scenario
  ↓
actual test project / command
  ↓
exact result on candidate SHA
  ↓
CERTIFICATION decision
```

TESTS defines what must be proven.

TESTS does not declare that source currently passes.

CERTIFICATION is the only artifact that may record PASS/FAIL, VERIFIED/STABLE, or a release-gate decision.

## 2. Preparation snapshot

The source audit used to synchronize this document observed:

```text
branch: develop
SHA: 35702d0fa9fb01ed68b0667bab500030d60bd028
tree: 271f825cb27ca06c0a7931804b70515fe3e2bae1
```

This is preparation evidence only.

At execution time, every result must be bound to a newly captured candidate SHA.

## 3. Evidence-state vocabulary

This document uses the following preparation-time labels:

```text
EXISTING-SOURCE
  A relevant test file/method exists in the preparation snapshot.
  This does NOT mean it passed on the future certification SHA.

PARTIAL-SOURCE
  Relevant tests exist but do not prove the whole scenario.

GAP
  No exact executable proof was confirmed for the full scenario.

PLANNED
  Scenario is required if the capability is promoted into released scope.

NOT-APPLICABLE
  The scenario is intentionally outside the released capability slice.

CERTIFICATION-ONLY
  The proof is primarily an exact-SHA command/gate/evidence record rather than one unit test.
```

Forbidden interpretation:

```text
EXISTING-SOURCE == PASS
```

## 4. Canonical runtime under test

The production Application pipeline at preparation time is:

```text
ExceptionMappingBehavior
→ ApplicationTracingBehavior
→ RequestContractBehavior
→ ExecutionContextBehavior
→ DataSessionBehavior
→ AccessControlBehavior
→ IdempotencyBehavior
→ handler
```

Authorization flow:

```text
RequestDescriptorRegistry
+ ExecutionContextSnapshot
+ IAccessFactsProvider
      ↓
AccessFacts
      ↓
IAccessPolicyEvaluator
      ↓
AccessPolicyEngine
      ↓
AccessDecision
      ↓
AccessControlBehavior
      ↓
handler only when allowed
```

Infrastructure facts path:

```text
PostgresAccessFactsProvider
→ AccessFactsQuery.Sql
→ active request transaction/connection
```

Persistence isolation:

```text
Application authorization
+
RLS defense-in-depth
```

Tests MUST target this current production shape, not obsolete `AuthorizationBehavior`,
`PermissionService`, or decision-store terminology.

## 5. Test authority rules

Tests may verify accepted semantics.

Tests MUST NOT invent:

- Account→Workspace ownership;
- new built-in roles;
- new role-to-action policy;
- PermissionRule/WorkspacePolicy precedence not accepted by SPEC;
- CustomRole runtime composition not implemented;
- ResourcePermission inheritance simply because a cache type exists;
- ShareLink anonymous authority beyond released contract;
- a new resource-kind/action naming scheme;
- direct foreign DbContext access as a public contract.

If source and accepted authority conflict, mark the scenario BLOCKED/GAP and resolve the decision before encoding a new expectation.

## 6. Test-level model

### T0 — Static / compile / source guards

Used for:

- architecture boundaries;
- forbidden dependencies;
- request security classification;
- endpoint/OpenAPI convention;
- generated-contract drift;
- zero-test/gate sanity.

### T1 — Domain

Used for:

- aggregate invariants;
- lifecycle;
- value objects;
- owner/last-owner rules;
- Domain events;
- no-op/version semantics.

### T2 — Application

Used for:

- commands/queries;
- validation;
- orchestration;
- request security declaration;
- policy evaluation as pure Application logic.

### T3 — Infrastructure

Used for:

- EF mappings;
- RLS helpers/policies;
- projection/cache adapters;
- SQL/provider mechanics;
- migration/bootstrap mechanics.

### T4 — API

Used for:

- routes;
- auth/CSRF/idempotency/version metadata;
- request/response/error mapping;
- OpenAPI operation identity.

### T5 — Architecture

Used for:

- Domain purity;
- module-first source layout;
- private persistence boundaries;
- canonical access-control path;
- handler/endpoint bypass prohibition;
- bounded-context contract rules.

### T6 — Integration

Used for:

- production DI;
- PostgreSQL constraints;
- active request transaction;
- authorization before side effect;
- outbox;
- cross-context adapter wiring.

### T7 — Security

Used for:

- privilege escalation;
- enumeration/not-found privacy;
- tenant spoofing;
- stale authorization;
- secret leakage;
- fail-closed behavior.

### T8 — Concurrency

Used for:

- membership uniqueness;
- last-owner race;
- invitation race;
- ACL/policy/share races.

### T9 — Migration

Used for:

- clean database;
- supported upgrade;
- resource/action/role persisted compatibility;
- no pending model changes.

### T10 — Reliability / observability / performance

Used for:

- retry/recovery;
- projection/caching failure;
- traceability;
- authorization hot-path evidence.

### T11 — Cross-context contracts

Used for:

- producer ownership;
- Public contract / consumer port;
- event compatibility;
- representative downstream handshake.

## 7. Test ID convention

```text
WG-TST-<AREA>-<LAYER>-NNN
```

Examples:

```text
WG-TST-WSP-DOM-001
WG-TST-MEM-INT-003
WG-TST-AUTHZ-ARCH-002
WG-TST-RLS-SEC-001
WG-TST-WM-X-001
```

IDs are stable documentation identities.

Do not renumber an existing ID merely because test files move.

## 8. Scenario evidence record

Every certification row records:

```text
Test ID:
WGREQ:
PLAN work unit:
Candidate SHA:
Evidence state before execution:
Project:
Test file/method or command:
Provider:
Production DI: yes/no
Executed:
Result:
Test count:
Skipped:
Duration:
Artifact/log:
Notes:
```

## 9. Existing source-test topology

Preparation audit confirmed relevant source in:

```text
Notrelix.Domain.Tests
Notrelix.Application.Tests
Notrelix.Infrastructure.Tests
Notrelix.API.Tests
Notrelix.Architecture.Tests
Notrelix.Integration.Tests
Notrelix.Platform.Tests
```

Representative existing source includes:

```text
Domain:
  Workspaces/Workspaces/WorkspaceTests.cs
  Workspaces/Workspaces/WorkspaceOwnerRulesTests.cs
  Workspaces/Members/WorkspaceMemberTests.cs
  Workspaces/Invitations/WorkspaceInvitationTests.cs
  Workspaces/Teams/TeamTests.cs
  Workspaces/Spaces/SpaceTests.cs
  Governance/Permissions/PermissionRuleLifecycleTests.cs
  Governance/Permissions/ResourcePermissionTests.cs
  Governance/Roles/CustomRoleTests.cs
  Governance/Policies/WorkspacePolicyTests.cs
  Governance/ShareLinks/ShareLinkTests.cs
  Governance/PermissionTemplateLifecycleTests.cs

Application:
  Common/Behaviors/AccessControlBehaviorTests.cs
  Common/Behaviors/PipelineOrderTests.cs
  Features/Governance/CanonicalKindValidatorsTests.cs
  Features/Governance/GrantResourcePermissionCommandValidatorTests.cs
  Features/Workspaces/...
  Features/WorkManagement/Boards/CreateBoardInWorkspaceTests.cs

Architecture:
  Authorization/AuthPipelineArchitectureTests.cs
  Authorization/HandlerAuthorizationBypassArchitectureTests.cs
  Authorization/UseCaseSecurityClassificationTests.cs
  Authorization/RlsArchitectureTests.cs
  Authorization/RlsPolicyArchitectureTests.cs
  DataAccess/DbContextBoundaryArchitectureTests.cs
  EndpointContracts/OpenApiConventionTests.cs

Integration:
  Governance/PostgresAccessFactsProviderTests.cs
  Governance/GovernanceResourcePermissionFlowTests.cs
  Integration/RlsRuntimeEnforcementTests.cs
  WorkManagement/CreateBoardInWorkspacePipelineTests.cs
  Workspaces/WorkspaceCreationPipelineAuthorizationTests.cs
  Baselines/AccessPolicyEngineCharacterizationTests.cs
  Workspaces/AcceptInvitationTransactionEvidenceTests.cs
  Workspaces/WorkspaceCreatedOutboxEvidenceTests.cs
  Workspaces/WorkspaceMembershipOutboxEvidenceTests.cs
  Data/MigrationSmokeTests.cs
```

This inventory is a starting map, not a certification result.

# Master traceability map

## 10. Requirement-family mapping

| Requirements | Primary verification families |
|---|---|
| WGREQ001–WGREQ011 | Workspace / Provisioning / Settings / Home |
| WGREQ012–WGREQ023 | Membership / owner safety / Identity interaction |
| WGREQ024–WGREQ033 | Invitation / token / replay / race |
| WGREQ034–WGREQ043 | Teams / Spaces / Workspace Rules |
| WGREQ044–WGREQ053 | ResourceKind / ResourceRef / PermissionAction |
| WGREQ054–WGREQ062 | Permission / PermissionRule / default deny |
| WGREQ063–WGREQ070 | built-in roles / CustomRole |
| WGREQ071–WGREQ079 | WorkspacePolicy / ResourcePermission |
| WGREQ080–WGREQ096 | effective authorization / canonical pipeline |
| WGREQ097–WGREQ113 | ShareLinks / audit / templates |
| WGREQ114–WGREQ128 | upstream/downstream contracts |
| WGREQ129–WGREQ143 | data ownership / API / events |
| WGREQ144–WGREQ170 | concurrency / security / reliability / observability / performance |
| WGREQ171–WGREQ177 | migration / compatibility |
| WGREQ178–WGREQ182 | P2 core producer gate |
| WGREQ183–WGREQ190 | architecture synchronization / evidence / handoff |
## 1. WG-TST-WSP-DOM-001 — Workspace creation produces stable Account-scoped identity

Explicit requirement coverage: WGREQ001, WGREQ002

### Traceability

```text
Requirements: WGREQ001–WGREQ003
PLAN: WG-WSP-001, WG-WSP-002
Layer: T1
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.Domain.Tests/Workspaces/Workspaces/WorkspaceTests.cs :: Create_ShouldSucceed`
- `WorkspaceTests.cs :: Create_PersonalWorkspace_ShouldHaveAccountId`

### Setup

- valid AccountId
- valid owner actor
- valid name/slug

### Execute

1. Create Workspace using canonical Domain factory/API.
2. Capture Id, AccountId, lifecycle state and emitted Domain event.

### Required assertions

- Workspace receives stable non-empty ID.
- AccountId equals authoritative Account.
- initial status is Active.
- event carries matching Account/Workspace identity.

### Certification evidence

- Exact test method result on candidate SHA.
- No source exception/mutation outside Domain contract.

## 2. WG-TST-WSP-DOM-002 — Workspace + initial owner creation preserves Domain atomic result

### Traceability

```text
Requirements: WGREQ001, WGREQ012, WGREQ019
PLAN: WG-WSP-001, WG-MEM-001
Layer: T1
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `WorkspaceTests.cs :: CreateWithOwner_ShouldCreateWorkspaceAndOwnerMember`

### Setup

- valid account and owner user

### Execute

1. Create Workspace with owner through the existing Domain factory/result.
2. Inspect returned Workspace/member.

### Required assertions

- one Workspace is returned.
- one Owner membership is returned.
- scope IDs match.

### Certification evidence

- Domain test result.

### Gap / follow-up

- Application transaction atomicity is verified separately; Domain result alone is insufficient.

## 3. WG-TST-WSP-DOM-003 — Workspace lifecycle valid transitions

### Traceability

```text
Requirements: WGREQ005–WGREQ007
PLAN: WG-WSP-001
Layer: T1
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `WorkspaceTests.cs :: Archive_ShouldSetStatusToArchived_AndRaiseEvent`
- `WorkspaceTests.cs :: Unarchive_ShouldSetStatusToActive_AndRaiseEvent`
- `WorkspaceTests.cs :: Delete_ShouldSetIsDeleted_AndRaiseEvent`
- `WorkspaceTests.cs :: Restore_ShouldSetIsDeleted_AndRaiseEvent`

### Setup

- active Workspace

### Execute

1. Archive/unarchive/delete/restore through Domain behavior.

### Required assertions

- state transition matches SPEC.
- version/audit/event behavior remains deterministic.
- idempotent/no-op transitions do not emit duplicate facts where source contract says no-op.

### Certification evidence

- Domain methods executed on exact SHA.

## 4. WG-TST-WSP-DOM-004 — Archived Workspace rejects mutable profile/settings changes

### Traceability

```text
Requirements: WGREQ005, WGREQ007, WGREQ010
PLAN: WG-WSP-001, WG-SET-001
Layer: T1
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `WorkspaceTests.cs :: Rename_ArchivedWorkspace_ShouldThrow`
- `WorkspaceTests.cs :: UpdateDescription_ArchivedWorkspace_ShouldThrow`
- `WorkspaceTests.cs :: UpdateSettings_ArchivedWorkspace_ShouldThrow`

### Setup

- archived Workspace

### Execute

1. Attempt rename, description update and settings update.

### Required assertions

- business-rule failure is deterministic.
- original state remains unchanged.

### Certification evidence

- Domain negative tests.

## 5. WG-TST-WSP-INT-001 — Account A cannot observe/mutate Account B Workspace

### Traceability

```text
Requirements: WGREQ003, WGREQ004, WGREQ152
PLAN: WG-WSP-002, WG-SEC-001
Layer: T6/T7
Preparation evidence state: PARTIAL-SOURCE
```

### Existing source evidence

- `Notrelix.Integration.Tests/Integration/RlsRuntimeEnforcementTests.cs :: AppRole_CrossAccount_GrantInOneAccount_DoesNotSeeOtherAccount`
- `Notrelix.Domain.Tests/Workspaces/WorkspaceTenantOwnershipTests.cs`

### Setup

- Account A + Workspace A
- Account B + Workspace B
- principal scoped/granted only to A

### Execute

1. Execute supported Workspace read and representative mutation using B identifiers under A context.

### Required assertions

- B data is not returned.
- mutation does not occur.
- failure is privacy-safe.
- RLS independently blocks cross-account persistence.

### Certification evidence

- Production-DI integration + PostgreSQL RLS evidence.

### Gap / follow-up

- Add/confirm a Workspace endpoint/use-case-specific negative test if existing RLS test does not exercise the exact P2 path.

## 6. WG-TST-WSP-APP-001 — Workspace mutation uses canonical pipeline authorization

### Traceability

```text
Requirements: WGREQ087–WGREQ090
PLAN: WG-WSP-003, WG-AUTHZ-001
Layer: T2/T6
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.Integration.Tests/Workspaces/WorkspaceCreationPipelineAuthorizationTests.cs :: CreateWorkspace_IsAuthorizedByPipeline_ForRepresentativeRoles`
- `WorkspaceCreationPipelineAuthorizationTests.cs :: CreateWorkspace_WhenEmailUnconfirmed_IsDeniedDespiteOwnerRole`

### Setup

- production DI
- authorized and denied principals

### Execute

1. Send representative Workspace command through MediatR pipeline.

### Required assertions

- allowed role reaches handler.
- denied/verification-failed principal does not perform source mutation.
- decision is produced before handler effect.

### Certification evidence

- Integration result on exact SHA.

## 7. WG-TST-WSP-EVT-001 — Workspace creation outward fact is atomic with source commit

### Traceability

```text
Requirements: WGREQ139–WGREQ143, WGREQ160
PLAN: WG-WSP-004, WG-EVT-001
Layer: T6
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.Integration.Tests/Workspaces/WorkspaceCreatedOutboxEvidenceTests.cs :: WorkspaceCreatedFact_OutboxIntent_CommitsAtomically`
- `WorkspaceCreatedOutboxEvidenceTests.cs :: RolledBackWorkspaceCreation_NoCommittedOutwardDelivery`

### Setup

- transactional Workspace creation

### Execute

1. Execute successful and rolled-back creation.

### Required assertions

- successful source commit includes required outbox intent.
- rollback leaves no committed outward delivery intent.

### Certification evidence

- PostgreSQL/outbox integration evidence.

## 8. WG-TST-MEM-DOM-001 — WorkspaceMember lifecycle and role state

Explicit requirement coverage: WGREQ012, WGREQ013

### Traceability

```text
Requirements: WGREQ012–WGREQ021
PLAN: WG-MEM-001
Layer: T1
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.Domain.Tests/Workspaces/Members/WorkspaceMemberTests.cs :: CreateMember_ShouldSucceed_AndRaiseEvent`
- `WorkspaceMemberTests.cs :: Suspend_ShouldSetStatusToSuspended_AndRaiseEvent`
- `WorkspaceMemberTests.cs :: Activate_FromSuspended_ShouldSetStatusToActive_AndRaiseEvent`
- `WorkspaceMemberTests.cs :: RemoveMember_ShouldSetStatusToRemoved_AndRaiseEvent`

### Setup

- valid Workspace/member identities

### Execute

1. Create → suspend → activate → remove using valid actors.

### Required assertions

- scope remains unchanged.
- status transitions are valid.
- events and version behavior are deterministic.

### Certification evidence

- Domain test results.

## 9. WG-TST-MEM-DOM-002 — last-owner mutation is rejected without state corruption

Explicit requirement coverage: WGREQ015

### Traceability

```text
Requirements: WGREQ019, WGREQ145
PLAN: WG-MEM-001
Layer: T1
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `WorkspaceOwnerRulesTests.cs :: WorkspaceOwnerRules_ShouldNotAllowActionsOnLastOwner`
- `WorkspaceMemberTests.cs :: ChangeMemberRole_OnLastOwner_ShouldThrow`
- `WorkspaceMemberTests.cs :: Suspend_OnLastOwner_ShouldThrow`
- `WorkspaceMemberTests.cs :: RemoveMember_OnLastOwner_ShouldThrow`

### Setup

- Workspace with exactly one active owner

### Execute

1. Attempt downgrade, suspend and remove.

### Required assertions

- all prohibited transitions fail.
- role/status remain unchanged.
- no success event is emitted.

### Certification evidence

- Domain tests.

### Gap / follow-up

- Concurrency safety requires separate database/application race proof.

## 10. WG-TST-MEM-INF-001 — duplicate effective membership is blocked by persistence

### Traceability

```text
Requirements: WGREQ014, WGREQ144
PLAN: WG-MEM-002
Layer: T3/T8
Preparation evidence state: GAP
```

### Existing source evidence

- No exact existing test was confirmed during preparation audit.

### Setup

- same Account/Workspace/User
- two competing add attempts
- real PostgreSQL

### Execute

1. Submit concurrent or deliberately duplicated membership creation.

### Required assertions

- at most one effective active membership persists.
- losing operation maps to stable conflict/idempotent result.
- no duplicate access grant remains.

### Certification evidence

- PostgreSQL constraint + integration result.
- Migration/index evidence.

### Gap / follow-up

- Preparation audit confirmed Domain `ConcurrencyTests.cs`, but an exact DB-level duplicate-membership race test was not verified by name.

## 11. WG-TST-MEM-APP-001 — membership admin mutation is pipeline-protected

### Traceability

```text
Requirements: WGREQ016–WGREQ021, WGREQ087–WGREQ090
PLAN: WG-MEM-003
Layer: T2/T6
Preparation evidence state: PARTIAL-SOURCE
```

### Existing source evidence

- `Notrelix.Application.Tests/Features/Workspaces/Members/Commands/AddMemberCommandHandlerTests.cs`
- `SuspendMemberCommandHandlerTests.cs`
- `Notrelix.API.Tests/Workspaces/MemberEndpointTests.cs`

### Setup

- authorized administrator
- ordinary member/outsider
- target member

### Execute

1. Execute add/suspend/remove/update-role through canonical request path.

### Required assertions

- authorized operation follows Domain invariants.
- unauthorized actor cannot mutate target.
- endpoint is not sole authorization owner.

### Certification evidence

- Application/API/integration tests on exact SHA.

### Gap / follow-up

- Ensure at least one production-DI denied membership mutation test exists; API unauthenticated tests alone are insufficient.

## 12. WG-TST-MEM-INT-001 — membership mutation synchronizes authz access-grant projection

### Traceability

```text
Requirements: WGREQ021, WGREQ153, WGREQ159, WGREQ187
PLAN: WG-MEM-004
Layer: T6/T7
Preparation evidence state: PARTIAL-SOURCE
```

### Existing source evidence

- `Notrelix.Integration.Tests/Integration/RlsRuntimeEnforcementTests.cs :: RuntimeMembershipCreation_WritesGrant_AndEnforcesUnderAppRole`
- `Notrelix.Integration.Tests/Workspaces/WorkspaceMembershipOutboxEvidenceTests.cs`

### Setup

- member creation and later suspension/removal
- real PostgreSQL/RLS

### Execute

1. Create member.
2. Verify grant projection allows expected row scope.
3. Suspend/remove member.
4. Attempt scoped persistence access again.

### Required assertions

- creation writes usable grant.
- revocation/suspension removes or invalidates grant according to design.
- stale grant cannot preserve access indefinitely.

### Certification evidence

- RLS integration evidence.
- Exact state transition → grant effect record.

### Gap / follow-up

- Preparation audit confirmed create/grant behavior; explicit suspend/remove revocation runtime test must be confirmed or added.

## 13. WG-TST-MEM-EVT-001 — membership outward fact is producer-owned and emitted once

### Traceability

```text
Requirements: WGREQ023, WGREQ139–WGREQ143
PLAN: WG-MEM-003, WG-EVT-001
Layer: T2/T6
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.Application.Tests/EventMappers/Workspaces/WorkspaceMembershipEventReferenceTests.cs`
- `Notrelix.Integration.Tests/Workspaces/WorkspaceMembershipOutboxEvidenceTests.cs :: MemberAddedFact_OutboxIntent_CommitsAtomically`
- `Notrelix.Integration.Tests/Workspaces/AcceptInvitationTransactionEvidenceTests.cs :: AcceptInvitation_FullHandlerGraphInOneComposition_EmitsExactlyOneMembershipOutboxFact`

### Setup

- membership created by direct add and invitation acceptance

### Execute

1. Execute membership creation sources.

### Required assertions

- outward fact is Workspaces-owned.
- registry identity is stable.
- one canonical membership fact is emitted for one business transition.

### Certification evidence

- Mapper + integration/outbox evidence.

## 14. WG-TST-INV-DOM-001 — Invitation state machine and expiry

### Traceability

```text
Requirements: WGREQ024–WGREQ031
PLAN: WG-INVITE-001
Layer: T1
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.Domain.Tests/Workspaces/Invitations/WorkspaceInvitationTests.cs :: Accept_ShouldSucceed_WhenPendingAndNotExpired`
- `Accept_ShouldThrow_WhenExpired`
- `Accept_ShouldThrow_WhenRevoked`
- `Resend_ShouldIncrementTokenGeneration_AndRaiseEvent`
- `Decline_ShouldSucceed_AndRaiseEvent`

### Setup

- pending invitation

### Execute

1. Exercise accept/decline/expire/revoke/resend transitions.

### Required assertions

- only valid transitions succeed.
- expired/revoked invitation cannot be accepted.
- resend rotates generation/hash state.
- Owner role cannot be invited.

### Certification evidence

- Domain tests.

## 15. WG-TST-INV-INT-001 — Invitation acceptance + membership commit atomically

### Traceability

```text
Requirements: WGREQ030, WGREQ031, WGREQ146, WGREQ160
PLAN: WG-INVITE-002
Layer: T6/T8
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.Integration.Tests/Workspaces/AcceptInvitationTransactionEvidenceTests.cs :: AcceptInvitation_InsideTransactionalSession_CommitsAccountAndWorkspaceTogether`
- `AcceptInvitation_WhenWorkspaceSideFails_RollsBackEntireGraph`
- `AlreadyMember_AcceptsInvitation_EmitsNoSecondMembershipEvent`

### Setup

- pending invitation
- candidate User/Account/Workspace

### Execute

1. Execute successful acceptance, simulated Workspace-side failure and already-member retry.

### Required assertions

- membership/invitation result is transactionally consistent.
- failure rolls back graph.
- retry/already-member does not duplicate membership fact.

### Certification evidence

- Integration transaction evidence.

## 16. WG-TST-INV-SEC-001 — Invitation token material is one-way/minimized

Explicit requirement coverage: WGREQ025, WGREQ033

### Traceability

```text
Requirements: WGREQ032, WGREQ033, WGREQ137
PLAN: WG-INVITE-003
Layer: T1/T7
Preparation evidence state: PARTIAL-SOURCE
```

### Existing source evidence

- `Notrelix.Domain.Tests/Workspaces/Invitations/InvitationTokenHashTests.cs`
- `Notrelix.API.Tests/Workspaces/InvitationEndpointTests.cs :: GetInvitationByToken_WithInvalidToken_ReturnsNotFound`

### Setup

- valid and invalid invitation tokens

### Execute

1. Inspect persisted invitation representation.
2. Exercise public token lookup.
3. Inspect API/log/event payloads.

### Required assertions

- raw reusable token is not persisted as ordinary state.
- invalid token is enumeration-safe.
- normal list/read/event/log output does not expose reusable secret.

### Certification evidence

- Domain/API/static secret-scan evidence.

### Gap / follow-up

- Explicit log/event secret scan must be executed during certification.

## 17. WG-TST-INV-CONC-001 — Invitation accept vs revoke/resend race

### Traceability

```text
Requirements: WGREQ031, WGREQ146
PLAN: WG-INVITE-002
Layer: T8
Preparation evidence state: GAP
```

### Existing source evidence

- No exact existing test was confirmed during preparation audit.

### Setup

- one pending invitation
- concurrent accept and revoke/resend
- real persistence

### Execute

1. Run competing transitions with expected version/transaction rules.

### Required assertions

- only one valid terminal business outcome wins.
- no duplicate membership.
- retry is deterministic.

### Certification evidence

- Concurrency/integration evidence.

### Gap / follow-up

- Domain state-machine tests exist, but exact concurrent persistence race was not confirmed.

## 18. WG-TST-TEAM-DOM-001 — Team lifecycle and last-lead safety

### Traceability

```text
Requirements: WGREQ034–WGREQ037
PLAN: WG-TEAM-001
Layer: T1
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.Domain.Tests/Workspaces/Teams/TeamTests.cs :: CreateWithLead_ShouldCreateTeamAndLeadMember`
- `ChangeMemberRole_DowngradeLastLead_ShouldThrow`
- `RemoveMember_ShouldThrow_WhenRemovingLastLead`
- `Archive_EmptyActor_ShouldNotMutateStatus`

### Setup

- Workspace-scoped Team

### Execute

1. Create Team with lead; mutate member roles; archive/restore/delete.

### Required assertions

- Team remains Workspace-scoped.
- last-lead rule is preserved.
- invalid actor/lifecycle changes do not corrupt state.

### Certification evidence

- Domain tests.

## 19. WG-TST-TEAM-APP-001 — Team member addition requires valid Workspace membership

### Traceability

```text
Requirements: WGREQ035, WGREQ037
PLAN: WG-TEAM-001
Layer: T2/T6
Preparation evidence state: PARTIAL-SOURCE
```

### Existing source evidence

- `Notrelix.Application.Tests/Features/Workspaces/Teams/Commands/CreateTeamCommandHandlerTests.cs`
- `ChangeTeamMemberRoleCommandHandlerTests.cs`
- `Notrelix.API.Tests/Workspaces/TeamEndpointTests.cs`

### Setup

- Workspace member and non-member candidates

### Execute

1. Add/change Team member through Application.

### Required assertions

- accepted member satisfies Workspace membership invariant.
- outside user cannot gain Workspace access solely through Team membership.

### Certification evidence

- Application/integration evidence.

### Gap / follow-up

- Confirm/add explicit non-member negative integration case if absent.

## 20. WG-TST-SPACE-DOM-001 — Space lifecycle/type/visibility

### Traceability

```text
Requirements: WGREQ038–WGREQ041
PLAN: WG-SPACE-001
Layer: T1
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.Domain.Tests/Workspaces/Spaces/SpaceTests.cs`

### Setup

- Workspace-scoped Space

### Execute

1. Exercise rename/archive/unarchive/delete/restore/visibility/type mutations.

### Required assertions

- WorkspaceId remains stable.
- archived/deleted restrictions hold.
- visibility/type changes emit expected facts.

### Certification evidence

- Domain tests.

## 21. WG-TST-SPACE-AUTHZ-001 — Space visibility is not a second authorization engine

### Traceability

```text
Requirements: WGREQ040, WGREQ041, WGREQ083
PLAN: WG-SPACE-001, WG-AUTHZ-004
Layer: T5/T7
Preparation evidence state: GAP
```

### Existing source evidence

- No exact existing test was confirmed during preparation audit.

### Setup

- Private and Workspace-visible spaces
- members with differing Governance authority

### Execute

1. Attempt representative action where Space visibility is only one fact.

### Required assertions

- visibility alone does not grant unrelated action permission.
- Governance policy remains canonical action authorization.

### Certification evidence

- Architecture/security integration evidence.

### Gap / follow-up

- Required only when a released protected resource actually consumes Space visibility.

## 22. WG-TST-RULE-ARCH-001 — Workspace Rules stay aggregate/local

### Traceability

```text
Requirements: WGREQ042, WGREQ043
PLAN: WG-RULE-001
Layer: T5
Preparation evidence state: PARTIAL-SOURCE
```

### Existing source evidence

- `Notrelix.Domain.Tests/Workspaces/*RulesTests.cs`

### Setup

- candidate source scan

### Execute

1. Inspect Rules types/dependencies and usages.

### Required assertions

- rules remain Domain/business invariant helpers.
- no generic Governance/Automation engine is duplicated.

### Certification evidence

- Architecture/source guard or review evidence.

### Gap / follow-up

- Add static gate if recurrent drift is observed.

## 23. WG-TST-RES-ARCH-001 — ResourceKind/ResourceRef remain logical identities

### Traceability

```text
Requirements: WGREQ044–WGREQ049
PLAN: WG-RES-001
Layer: T5
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.Application.Tests/Features/Governance/CanonicalKindValidatorsTests.cs`
- `Notrelix.Architecture.Tests/Authorization/AuthPipelineArchitectureTests.cs`

### Setup

- canonical and malformed resource kinds

### Execute

1. Validate accepted kind representation and request resource contracts.

### Required assertions

- authorization identity does not depend on CLR/table/route name.
- invalid canonical kinds fail deterministically.

### Certification evidence

- Application + architecture tests.

## 24. WG-TST-ACT-ARCH-001 — PermissionAction is business-semantic, not HTTP verb

### Traceability

```text
Requirements: WGREQ050–WGREQ053
PLAN: WG-RES-002
Layer: T5
Preparation evidence state: PARTIAL-SOURCE
```

### Existing source evidence

- `PermissionAction enum source`
- `UseCaseSecurityClassificationTests.cs`

### Setup

- protected requests across Workspace/Governance/WorkManagement

### Execute

1. Inventory declared actions.

### Required assertions

- every protected request uses accepted business action.
- HTTP method alone is never action authority.
- orphan action values are documented.

### Certification evidence

- Static inventory/architecture evidence.

### Gap / follow-up

- A dedicated action ownership registry/gate may need extension for full coverage.

## 25. WG-TST-RES-X-001 — resource facts come from approved semantic owner

### Traceability

```text
Requirements: WGREQ047–WGREQ049, WGREQ093–WGREQ096, WGREQ186
PLAN: WG-RES-003
Layer: T5/T11
Preparation evidence state: PARTIAL-SOURCE
```

### Existing source evidence

- `Notrelix.Integration.Tests/Governance/PostgresAccessFactsProviderTests.cs`
- `AuthPipelineArchitectureTests.cs :: AuthorizationPolicyEngine_MustNotReadPersistence`
- `backend Application/Infrastructure cross-context architecture gates`

### Setup

- Board/Page/Billing representative facts

### Execute

1. Resolve facts for each representative resource.

### Required assertions

- facts are neutral.
- Documents/Billing facts use producer-owned seams as defined.
- Governance policy evaluator does not read persistence.
- new foreign facts are not sourced by arbitrary private DbContext access.

### Certification evidence

- Architecture + integration evidence.

### Gap / follow-up

- Current Board clauses in AccessFactsQuery require explicit architecture classification in certification; do not silently treat them as ideal producer contract.

## 26. WG-TST-PERM-APP-001 — protected request defaults to deny without authority

### Traceability

```text
Requirements: WGREQ054–WGREQ059, WGREQ085
PLAN: WG-PERM-001
Layer: T2/T7
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.Application.Tests/Common/Behaviors/AccessControlBehaviorTests.cs`
- `Notrelix.Integration.Tests/Governance/GovernanceResourcePermissionFlowTests.cs`

### Setup

- authenticated principal lacking required membership/grant

### Execute

1. Execute representative protected request.

### Required assertions

- request does not reach protected side effect.
- result is Forbidden/NotFound according to privacy semantics.
- no implicit allow occurs.

### Certification evidence

- Application/integration evidence.

## 27. WG-TST-PRULE-APP-001 — PermissionRule priority and deny precedence

Explicit requirement coverage: WGREQ060, WGREQ061

### Traceability

```text
Requirements: WGREQ057, WGREQ060–WGREQ062, WGREQ084
PLAN: WG-PERM-002
Layer: T2/T6
Preparation evidence state: PARTIAL-SOURCE
```

### Existing source evidence

- `Notrelix.Domain.Tests/Governance/Permissions/PermissionRuleLifecycleTests.cs`
- `GovernanceResourcePermissionFlowTests.cs :: ArchivePage_ExplicitDenyRule_IsForbidden_EvenForManager`
- `ArchivePage_ExplicitAllowRule_Allows`

### Setup

- same-scope allow/deny rules with priority/time windows

### Execute

1. Resolve AccessFacts and execute protected action under competing rules.

### Required assertions

- rules outside active time window are ignored.
- minimum-priority group controls.
- Deny at winning priority dominates Allow.
- unsupported condition semantics are not silently treated as supported.

### Certification evidence

- Domain + PostgreSQL integration result.

### Gap / follow-up

- Add explicit same-priority allow+deny and starts/expires integration cases if not already covered.

## 28. WG-TST-PERM-CACHE-001 — authorization projection/cache cannot preserve revoked access

### Traceability

```text
Requirements: WGREQ059, WGREQ153, WGREQ169
PLAN: WG-PERM-003
Layer: T7/T10
Preparation evidence state: GAP
```

### Existing source evidence

- No exact existing test was confirmed during preparation audit.

### Setup

- active grant then revoke
- any inheritance/access cache enabled

### Execute

1. Prime cache/projection.
2. Revoke authority.
3. Execute protected request.

### Required assertions

- revoked actor is denied within documented consistency contract.
- cache failure/staleness never widens access.

### Certification evidence

- Integration/reliability evidence.

### Gap / follow-up

- Mark NOT-APPLICABLE if no cache participates in released decision path.

## 29. WG-TST-ROLE-DOM-001 — WorkspaceRole vocabulary remains Guest/Member/Admin/Owner

### Traceability

```text
Requirements: WGREQ063–WGREQ067
PLAN: WG-ROLE-001
Layer: T1/T5
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `WorkspaceRole enum`
- `WorkspaceMemberTests.cs`

### Setup

- all built-in role values

### Execute

1. Persist/use role facts through membership transitions.

### Required assertions

- identity is stable and scoped to Workspace.
- ordinary ChangeRole cannot assign Owner.
- role remains input, not sole resource-policy engine.

### Certification evidence

- Domain + architecture evidence.

## 30. WG-TST-ROLE-SEC-001 — grant/revoke ceiling prevents ACL escalation

Explicit requirement coverage: WGREQ069, WGREQ154

### Traceability

```text
Requirements: WGREQ066, WGREQ154
PLAN: WG-ROLE-002
Layer: T7/T6
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `GovernanceResourcePermissionFlowTests.cs :: Grant_OnBoard_ByNonOwnerWithNoResourceLevel_IsForbidden`
- `Grant_OnBoard_CannotDowngrade_ExistingHigherTargetLevel`
- `Revoke_AuthorityBelowTargetRank_IsForbidden_AndRowStaysActive`
- `Revoke_SubjectWithoutManagementAuthority_IsForbidden_EvenOnOwnRankRow`

### Setup

- actors with Owner/Manager/lower/no effective rank

### Execute

1. Grant/revoke target ACLs at equal, lower and higher ranks.

### Required assertions

- actor cannot manage authority above own ceiling.
- failed operation leaves row unchanged.
- resource visibility alone does not grant ACL management.

### Certification evidence

- PostgreSQL production-flow integration evidence.

## 31. WG-TST-CROLE-DOM-001 — CustomRole Domain behavior is verified without claiming runtime delivery

### Traceability

```text
Requirements: WGREQ068–WGREQ070, WGREQ188
PLAN: WG-CROLE-001
Layer: T1
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.Domain.Tests/Governance/Roles/CustomRoleTests.cs`

### Setup

- CustomRole aggregate

### Execute

1. Create; add/remove permission; archive/activate.

### Required assertions

- Domain invariants/events hold.
- evidence is labeled Domain-only unless Application/runtime composition is separately proven.

### Certification evidence

- Domain test result.
- Layer-status matrix.

### Gap / follow-up

- Do not use this scenario to mark CustomRole Application/API/integration VERIFIED.

## 32. WG-TST-POL-DOM-001 — WorkspacePolicy Domain configuration semantics

### Traceability

```text
Requirements: WGREQ071–WGREQ075, WGREQ188
PLAN: WG-POL-001
Layer: T1
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.Domain.Tests/Governance/Policies/WorkspacePolicyTests.cs`

### Setup

- WorkspacePolicy

### Execute

1. Create and update guest/resource/sharing policy components.

### Required assertions

- Workspace scope is required.
- updates preserve omitted components.
- event reflects update.

### Certification evidence

- Domain result.

### Gap / follow-up

- Runtime enforcement remains GAP unless a concrete evaluator flow is proven.

## 33. WG-TST-POL-APP-001 — WorkspacePolicy runtime composition only when released

### Traceability

```text
Requirements: WGREQ072–WGREQ075
PLAN: WG-POL-001
Layer: T2/T6
Preparation evidence state: PLANNED
```

### Existing source evidence

- No exact existing test was confirmed during preparation audit.

### Setup

- only if product/release scope promotes WorkspacePolicy into effective authorization

### Execute

1. Execute a flow whose accepted policy explicitly consumes WorkspacePolicy.

### Required assertions

- precedence with PermissionRule/ResourcePermission is deterministic.
- malformed/unavailable policy fails safely.

### Certification evidence

- Application/integration evidence.

### Gap / follow-up

- NOT-APPLICABLE for P2 core if WorkspacePolicy remains secondary configuration groundwork.

## 34. WG-TST-RPERM-DOM-001 — ResourcePermission lifecycle

### Traceability

```text
Requirements: WGREQ076–WGREQ079
PLAN: WG-RPERM-001
Layer: T1
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.Domain.Tests/Governance/Permissions/ResourcePermissionTests.cs`
- `ResourcePermissionLifecycleTests.cs`

### Setup

- resource ACL

### Execute

1. Grant; change level; revoke/delete; restore where applicable.

### Required assertions

- scope/subject/resource identity remains stable.
- revoke produces intended Domain event semantics.
- soft-delete lifecycle is deterministic.

### Certification evidence

- Domain tests.

## 35. WG-TST-RPERM-INT-001 — released ResourcePermission flow is exact-resource and Workspace-scoped

### Traceability

```text
Requirements: WGREQ076–WGREQ079, WGREQ154
PLAN: WG-RPERM-001
Layer: T6/T7
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.Integration.Tests/Governance/GovernanceResourcePermissionFlowTests.cs :: Grant_OnPage_ByOwner_Allows_AndUsesPageAction`
- `Get_OnPage_DoesNotReturnRowsFromAnotherResource`
- `Revoke_ForeignAccountTarget_IsNotFound_AndForeignRowStaysActive`

### Setup

- multiple resources/workspaces/accounts

### Execute

1. Grant/read/revoke through production flow.

### Required assertions

- only exact resource rows are affected/returned.
- foreign account/workspace target is not exposed or mutated.
- management authority is enforced.

### Certification evidence

- PostgreSQL integration evidence.

## 36. WG-TST-RPERM-INF-001 — ResourcePermission inheritance cache is not mistaken for source truth

### Traceability

```text
Requirements: WGREQ078, WGREQ059
PLAN: WG-RPERM-002
Layer: T3
Preparation evidence state: PARTIAL-SOURCE
```

### Existing source evidence

- `Notrelix.Infrastructure.Tests/Data/Projections/ResourcePermissionInheritanceCacheEntryTests.cs`

### Setup

- inheritance cache entry/projection

### Execute

1. Verify infrastructure model behavior and locate all consumers.

### Required assertions

- cache/projection identity is deterministic.
- presence of cache structure alone does not prove inheritance is active.
- source/rebuild owner is explicit.

### Certification evidence

- Infrastructure test + source-consumer inventory.

### Gap / follow-up

- Full inheritance behavior remains GAP unless runtime producer/evaluator and invalidation are proven.

## 37. WG-TST-AUTHZ-APP-001 — AccessControlBehavior owns pre-handler decision enforcement

### Traceability

```text
Requirements: WGREQ080–WGREQ092, WGREQ185
PLAN: WG-AUTHZ-001
Layer: T2
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.Application.Tests/Common/Behaviors/AccessControlBehaviorTests.cs`

### Setup

- request descriptors for anonymous/protected flows
- fake facts/evaluator

### Execute

1. Invoke behavior under allowed/denied/misconfigured decision paths.

### Required assertions

- unprotected anonymous request avoids datastore facts.
- required facts are resolved only when declared.
- denial maps to stable exception category.
- handler executes only on Allowed.

### Certification evidence

- Application test result.

## 38. WG-TST-PIPE-ARCH-001 — pipeline registration and order are frozen

### Traceability

```text
Requirements: WGREQ088, WGREQ092, WGREQ185
PLAN: WG-AUTHZ-001, WG-AUTHZ-002
Layer: T5
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.Application.Tests/Common/Behaviors/PipelineOrderTests.cs :: Pipeline_behaviors_must_be_registered_in_frozen_order`
- `Pipeline_must_have_exactly_7_behaviors`
- `DataSession_must_be_after_contract_and_before_access_control`
- `Idempotency_must_be_inside_db_scope`

### Setup

- production Application DI registration

### Execute

1. Inspect/resolve registered behaviors.

### Required assertions

- exact expected order is preserved.
- DataSession surrounds fact resolution.
- Idempotency remains inside data-session scope.

### Certification evidence

- Application test result.

## 39. WG-TST-AUTHZ-ARCH-001 — no handler/endpoint authorization bypass

### Traceability

```text
Requirements: WGREQ089, WGREQ090, WGREQ185
PLAN: WG-AUTHZ-001
Layer: T5
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.Architecture.Tests/Authorization/HandlerAuthorizationBypassArchitectureTests.cs :: ProtectedHandlers_DoNotInjectPipelineAuthorizationServices`
- `ScopedRequests_DeclareCanonicalAuthorizationContract`
- `ProductionHandlerRoleChecks_AreExactlyRegisteredBusinessInvariants`
- `Middleware_DoesNotUseRawEndpointAuthConventions`

### Setup

- Application/API assemblies

### Execute

1. Run architecture scanners.

### Required assertions

- protected handlers do not invoke canonical auth service manually.
- scoped requests declare canonical contract.
- role-check exceptions are exact business invariants only.
- middleware/endpoints do not create competing policy.

### Certification evidence

- Architecture test result.

## 40. WG-TST-AUTHZ-ARCH-002 — AccessPolicyEngine is Governance-owned and persistence-free

### Traceability

```text
Requirements: WGREQ080, WGREQ094, WGREQ185
PLAN: WG-AUTHZ-004
Layer: T5
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.Architecture.Tests/Authorization/AuthPipelineArchitectureTests.cs :: AuthorizationPolicyEngine_MustNotReadPersistence`
- `AccessPolicyEngine_IsGovernanceOwned`
- `EvaluatorSeam_HasExactlyOneImplementation`
- `AccessControlBehavior_ReferencesOnlyTheSeam_NotTheConcreteEngine`

### Setup

- Application/Common and Governance Application assemblies

### Execute

1. Run architecture tests.

### Required assertions

- policy engine owns permission semantics without DbContext/provider reads.
- Common pipeline knows only neutral seam.
- exactly one evaluator implementation is registered/allowed.

### Certification evidence

- Architecture test result.

## 41. WG-TST-FACTS-INT-001 — PostgresAccessFactsProvider runs on active request transaction

### Traceability

```text
Requirements: WGREQ083, WGREQ092, WGREQ186
PLAN: WG-AUTHZ-002, WG-AUTHZ-003
Layer: T6
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.Integration.Tests/Governance/PostgresAccessFactsProviderTests.cs :: ResolveAsync_executes_on_the_active_transaction_and_returns_one_snapshot`

### Setup

- real PostgreSQL
- active data-session transaction

### Execute

1. Resolve AccessFacts through provider.

### Required assertions

- one coherent snapshot is returned.
- provider uses active connection/transaction.
- missing required session is not silently recreated outside the pipeline.

### Certification evidence

- PostgreSQL integration result.

## 42. WG-TST-FACTS-X-001 — Billing subscription fact remains Billing-owned

### Traceability

```text
Requirements: WGREQ125, WGREQ186
PLAN: WG-AUTHZ-003, WG-X-005
Layer: T6/T11
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `PostgresAccessFactsProviderTests.cs :: ResolveAsync_without_subscription_requirement_never_consults_billing`
- `ResolveAsync_subscription_requirement_forwards_scope_and_adopts_billing_decision`
- `ResolveAsync_subscription_requirement_adopts_satisfied_decision`

### Setup

- requests with/without subscription gate

### Execute

1. Resolve facts.

### Required assertions

- Billing is not consulted when gate absent.
- when present, scope/requirement is passed to Billing seam.
- Governance consumes neutral satisfied boolean and does not order tiers.

### Certification evidence

- Integration contract evidence.

## 43. WG-TST-AUTHZ-SEC-001 — missing context/facts fail closed

### Traceability

```text
Requirements: WGREQ085, WGREQ151, WGREQ159
PLAN: WG-AUTHZ-002, WG-REL-001
Layer: T7
Preparation evidence state: PARTIAL-SOURCE
```

### Existing source evidence

- `AccessControlBehaviorTests.cs`
- `PostgresAccessFactsProviderTests.cs :: ResolveAsync_declared_subscription_without_marker_fails_closed`

### Setup

- missing account/workspace/resource or malformed request contract

### Execute

1. Invoke protected request.

### Required assertions

- SecurityMisconfiguration/Forbidden/NotFound is deterministic as specified.
- handler does not execute.
- failure never becomes allow.

### Certification evidence

- Application/integration result.

### Gap / follow-up

- Ensure representative missing Workspace/resource context case is explicitly covered.

## 44. WG-TST-RLS-SEC-001 — app role without context/grant fails closed

### Traceability

```text
Requirements: WGREQ152, WGREQ187
PLAN: WG-SEC-002
Layer: T7/T6
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.Integration.Tests/Integration/RlsRuntimeEnforcementTests.cs :: AppRole_MissingSessionContext_FailsClosed_SeesNoRows`
- `AppRole_NoGrant_FailsClosed_SeesNoRows`

### Setup

- real PostgreSQL app role

### Execute

1. Query workspace-scoped data with missing context and with context but no grant.

### Required assertions

- zero unauthorized rows visible.
- no implicit global scope.

### Certification evidence

- PostgreSQL RLS integration evidence.

## 45. WG-TST-RLS-SEC-002 — RLS prevents cross-Workspace access

### Traceability

```text
Requirements: WGREQ152, WGREQ187
PLAN: WG-SEC-002
Layer: T7/T6
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `RlsRuntimeEnforcementTests.cs :: AppRole_CrossWorkspace_GrantInOneWorkspace_DoesNotSeeOtherWorkspace`
- `BackgroundScope_WithGrant_SeesOwnRowsOnly_NoBypass`

### Setup

- Workspace A/B records
- grant only for A

### Execute

1. Read under app/background scope.

### Required assertions

- A scope cannot see B.
- background grant is still bounded.
- workspace policy bypass is limited to explicitly trusted worker/system scopes.

### Certification evidence

- RLS integration result.

## 46. WG-TST-RLS-SEC-003 — RLS session context is transaction-local

### Traceability

```text
Requirements: WGREQ187
PLAN: WG-SEC-002
Layer: T6
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `RlsRuntimeEnforcementTests.cs :: SessionContext_IsTransactionLocal_DoesNotLeakAfterCommit`
- `SessionContext_IsTransactionLocal_DoesNotLeakAfterRollback`

### Setup

- reused database connection across transactions

### Execute

1. Set scoped context; commit/rollback; start subsequent transaction.

### Required assertions

- prior tenant/security context does not leak.

### Certification evidence

- PostgreSQL integration result.

## 47. WG-TST-WM-X-001 — WorkManagement Board protected allow path

### Traceability

```text
Requirements: WGREQ118–WGREQ121, WGREQ182
PLAN: WG-WM-001, WG-WM-003
Layer: T6/T11
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.Integration.Tests/WorkManagement/CreateBoardInWorkspacePipelineTests.cs :: Owner_ThroughCanonicalPipeline_CreatesBoard_WithDefaultFields`

### Setup

- Workspace owner
- valid Workspace

### Execute

1. Send CreateBoardInWorkspace through production MediatR/DI path.

### Required assertions

- AccessControl allows.
- handler creates canonical Board/default fields.
- no handler-local bypass is required.

### Certification evidence

- Production-DI integration result.

## 48. WG-TST-WM-X-002 — WorkManagement Board protected deny path

### Traceability

```text
Requirements: WGREQ118–WGREQ121, WGREQ182
PLAN: WG-WM-003
Layer: T6/T11
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `CreateBoardInWorkspacePipelineTests.cs :: Outsider_ThroughCanonicalPipeline_IsForbidden`

### Setup

- outsider principal
- valid target Workspace

### Execute

1. Execute same protected command.

### Required assertions

- request is denied.
- no Board/default-field side effect exists.

### Certification evidence

- Production-DI integration result.

## 49. WG-TST-WM-X-003 — cross-tenant Board/resource privacy

### Traceability

```text
Requirements: WGREQ086, WGREQ120, WGREQ152, WGREQ182
PLAN: WG-WM-003
Layer: T7/T11
Preparation evidence state: PARTIAL-SOURCE
```

### Existing source evidence

- `GovernanceResourcePermissionFlowTests.cs :: ArchivePage_WrongTenant_IsNotFound_AndTargetStaysActive`
- `CreateComment_OnCrossScopeBoardItem_IsNotFound`
- `RlsRuntimeEnforcementTests.cs`

### Setup

- resource from foreign account/workspace

### Execute

1. Execute representative protected resource action.

### Required assertions

- foreign resource is not disclosed/modified.
- NotFound/Forbidden follows privacy contract.
- RLS also prevents persistence escape.

### Certification evidence

- Integration/security evidence.

### Gap / follow-up

- Confirm a Board-specific cross-account negative case in P2 gate packet.

## 50. WG-TST-WM-X-004 — Board resource facts ownership is architecture-approved

### Traceability

```text
Requirements: WGREQ093–WGREQ096, WGREQ121, WGREQ186
PLAN: WG-WM-002
Layer: T5/T11
Preparation evidence state: CERTIFICATION-ONLY
```

### Existing source evidence

- `AccessFactsQuery.cs preparation audit`
- `backend architecture closure flow cards`

### Setup

- candidate source

### Execute

1. Inspect Board existence/audience/member facts path and architecture decision.

### Required assertions

- facts remain neutral.
- WorkManagement remains semantic owner.
- no new private foreign persistence dependency is introduced.

### Certification evidence

- Exact source classification + architecture gate result.

### Gap / follow-up

- Current direct SQL clauses must be classified explicitly; this is not automatically equivalent to ideal producer Public contract.

## 51. WG-TST-SHARE-DOM-001 — ShareLink Domain lifecycle

### Traceability

```text
Requirements: WGREQ097–WGREQ103
PLAN: WG-SHARE-001
Layer: T1
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.Domain.Tests/Governance/ShareLinks/ShareLinkTests.cs :: Create_ShouldHashToken_AndRaiseEvent`
- `IsExpired_ShouldReturnTrue_WhenExpirationPassed`
- `Disable_ShouldSetStatusToDisabled_AndRaiseEvent`
- `RotateTokenHash_ShouldUpdateHash_AndRaiseEvent`

### Setup

- ShareLink with bounded resource

### Execute

1. Create, expire-check, disable, rotate hash.

### Required assertions

- public mode requires expiry per Domain contract.
- hash state changes without exposing raw capability.
- disabled/expired link cannot be active.

### Certification evidence

- Domain result.

## 52. WG-TST-SHARE-SEC-001 — ShareLink secret/bounded capability

Explicit requirement coverage: WGREQ098, WGREQ156

### Traceability

```text
Requirements: WGREQ099–WGREQ104, WGREQ156
PLAN: WG-SHARE-002
Layer: T7/T6
Preparation evidence state: GAP
```

### Existing source evidence

- No exact existing test was confirmed during preparation audit.

### Setup

- released ShareLink validation endpoint/path
- valid/expired/disabled/forged token
- two resources/workspaces

### Execute

1. Use token against exact and non-target resources.

### Required assertions

- capability applies only to intended resource/actions.
- expired/disabled/forged token fails.
- token does not become general membership authority.
- secret is absent from logs/list APIs.

### Certification evidence

- End-to-end security evidence.

### Gap / follow-up

- Domain Create/Disable presence does not prove public capability-token consumption path. Mark NOT-APPLICABLE if such consumption is not released.

## 53. WG-TST-TPL-DOM-001 — PermissionTemplate scope/lifecycle

### Traceability

```text
Requirements: WGREQ111–WGREQ113, WGREQ188
PLAN: WG-TPL-001
Layer: T1
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.Domain.Tests/Governance/PermissionTemplateLifecycleTests.cs`

### Setup

- system and workspace templates

### Execute

1. Create system/workspace template; archive workspace template; attempt system archive.

### Required assertions

- scope rules hold.
- system template is immutable under archive operation.
- version/event behavior is deterministic.

### Certification evidence

- Domain result.

### Gap / follow-up

- Template apply/runtime delivery is separate and may remain GAP/NOT-APPLICABLE.

## 54. WG-TST-AUD-ARCH-001 — audit capability is distinguished from operational logs

### Traceability

```text
Requirements: WGREQ105–WGREQ110
PLAN: WG-AUD-001
Layer: T5
Preparation evidence state: GAP
```

### Existing source evidence

- No exact existing test was confirmed during preparation audit.

### Setup

- candidate auditing/activity/event source

### Execute

1. Inventory audit service/storage and critical governance mutations.

### Required assertions

- authoritative audit fact owner is explicit.
- free-form operational log is not treated as immutable audit record.
- secret material is excluded.

### Certification evidence

- Architecture/source classification and integration evidence where released.

### Gap / follow-up

- Do not create Governance/Audit Domain model solely to satisfy document symmetry.

## 55. WG-TST-UP-X-001 — P2 uses upstream Actor/Account contract only

### Traceability

```text
Requirements: WGREQ114–WGREQ117, WGREQ133
PLAN: WG-P1-001, WG-P1-002, WG-X-001
Layer: T5/T11
Preparation evidence state: PARTIAL-SOURCE
```

### Existing source evidence

- `DbContextBoundaryArchitectureTests.cs`
- `Identity/Accounts producer certification`

### Setup

- candidate Workspace/Governance Application

### Execute

1. Scan for private Identity/Accounts persistence/Domain dependencies and exercise representative scope resolution.

### Required assertions

- no credential/session private state is owned by P2.
- Account/User identity is consumed through approved contract/context.
- P1 lifecycle facts remain upstream-owned.

### Certification evidence

- Architecture + contract evidence.

## 56. WG-TST-DOC-X-001 — Documents Page facts remain Documents-owned

### Traceability

```text
Requirements: WGREQ122, WGREQ186
PLAN: WG-X-003, WG-AUTHZ-003
Layer: T11
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `GovernanceResourcePermissionFlowTests.cs page scenarios`
- `PostgresAccessFactsProvider.cs uses IPageAuthorizationFacts`

### Setup

- active/archived/cross-scope pages

### Execute

1. Execute Page ACL/comment/archive authorization.

### Required assertions

- page existence/lifecycle/visibility facts come from Documents seam.
- Governance interprets permission without reading Documents private persistence in policy engine.
- archived/cross-scope page is not actionable.

### Certification evidence

- Integration + architecture evidence.

## 57. WG-TST-BILL-X-001 — entitlement and authorization remain separate

### Traceability

```text
Requirements: WGREQ124, WGREQ125, WGREQ186
PLAN: WG-X-005
Layer: T11
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `PostgresAccessFactsProviderTests subscription scenarios`

### Setup

- request with commercial gate plus Governance permission

### Execute

1. Resolve commercial fact and execute authorization.

### Required assertions

- Billing decides commercial requirement.
- Governance consumes neutral fact.
- role/permission decision remains distinct.

### Certification evidence

- Integration evidence.

## 58. WG-TST-INTG-X-001 — ManageIntegrations is dedicated Governance action

### Traceability

```text
Requirements: WGREQ127
PLAN: WG-X-006
Layer: T2/T11
Preparation evidence state: PARTIAL-SOURCE
```

### Existing source evidence

- `AccessPolicyEngine source branch for ManageIntegrations`

### Setup

- Owner/Admin/Member/Guest
- workspace/integration resource

### Execute

1. Execute protected Connect/Disconnect representative requests when released.

### Required assertions

- Owner/Admin allowed per accepted mapping.
- ordinary Member/Guest denied absent explicit rule.
- action is not aliased to ManageWorkspaceSettings.

### Certification evidence

- Application/integration evidence.

### Gap / follow-up

- Add exact runtime tests if current integrations suite does not cover Governance decision.

## 59. WG-TST-OWN-ARCH-001 — Workspace persistence abstractions remain owner-local

### Traceability

```text
Requirements: WGREQ129, WGREQ133, WGREQ184
PLAN: WG-INV-004, WG-X-*
Layer: T5
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.Architecture.Tests/DataAccess/DbContextBoundaryArchitectureTests.cs`
- `DbContextBoundaryTests.cs`

### Setup

- source assemblies

### Execute

1. Run bounded-context persistence scanners.

### Required assertions

- foreign contexts do not treat IWorkspaceDbContext as public API.
- shared ApplicationDbContext implementation does not merge semantic ownership.

### Certification evidence

- Architecture result.

## 60. WG-TST-OWN-ARCH-002 — Governance persistence abstractions remain owner-local

### Traceability

```text
Requirements: WGREQ130, WGREQ133, WGREQ184
PLAN: WG-INV-005, WG-X-*
Layer: T5
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `DbContextBoundaryArchitectureTests.cs`

### Setup

- source assemblies

### Execute

1. Scan IGovernanceDbContext references.

### Required assertions

- only Governance-owned Application persistence path uses owner-local abstraction except explicitly approved infrastructure composition.
- no consumer uses it as cross-context contract.

### Certification evidence

- Architecture result.

## 61. WG-TST-API-WSP-001 — Workspace endpoint authentication/contract baseline

### Traceability

```text
Requirements: WGREQ134–WGREQ138
PLAN: WG-API-001
Layer: T4
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.API.Tests/Workspaces/WorkspaceAuthTests.cs`
- `WorkspaceMutationTests.cs`
- `MemberEndpointTests.cs`
- `InvitationEndpointTests.cs`
- `SpaceEndpointTests.cs`
- `TeamEndpointTests.cs`
- `WorkspaceSettingsEndpointTests.cs`

### Setup

- authenticated and unauthenticated clients

### Execute

1. Exercise representative Workspace endpoint families.

### Required assertions

- protected routes reject unauthenticated requests.
- public invitation-token lookup remains explicitly public.
- validation/not-found contracts are stable.
- transport delegates to Application.

### Certification evidence

- API test results.

## 62. WG-TST-API-GOV-001 — Governance ResourcePermission/ShareLink endpoint contract

### Traceability

```text
Requirements: WGREQ135–WGREQ138
PLAN: WG-API-001
Layer: T4
Preparation evidence state: PARTIAL-SOURCE
```

### Existing source evidence

- `API/Endpoints/Governance/ResourcePermissions source`
- `API/Endpoints/Governance/ShareLinks source`
- `GovernanceResourcePermissionFlowTests.cs`

### Setup

- authorized/unauthorized clients
- valid/invalid resource contracts

### Execute

1. Exercise grant/revoke/get and released share-link endpoints.

### Required assertions

- canonical resource/action validation applies.
- authorization remains Application-owned.
- error/secret response is safe.

### Certification evidence

- API + integration evidence.

### Gap / follow-up

- Preparation audit did not enumerate a dedicated Governance API test file; add/confirm transport-level coverage.

## 63. WG-TST-API-OAS-001 — OpenAPI operation identity and drift

### Traceability

```text
Requirements: WGREQ138
PLAN: WG-API-002, WG-DOC-002
Layer: T0/T5
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.Architecture.Tests/EndpointContracts/OpenApiConventionTests.cs :: All_endpoint_files_must_have_WithName`
- `OperationIds_must_be_unique`
- `OperationIds_must_follow_dotted_format`

### Setup

- candidate API source and generated OpenAPI

### Execute

1. Run endpoint convention tests and canonical export/drift command.

### Required assertions

- all endpoints have stable operation IDs.
- IDs are unique/dotted convention.
- generated artifact has no unexplained drift.

### Certification evidence

- Architecture tests + OpenAPI diff/gate.

### Gap / follow-up

- Convention tests alone do not prove no semantic OpenAPI drift; export comparison remains certification evidence.

## 64. WG-TST-EVT-WSP-001 — Workspace public event mapping/registry compatibility

### Traceability

```text
Requirements: WGREQ139–WGREQ143
PLAN: WG-EVT-001
Layer: T2/T11
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `WorkspaceMembershipEventReferenceTests.cs`
- `WorkspaceCreatedOutboxEvidenceTests.cs`
- `WorkspaceMembershipOutboxEvidenceTests.cs`

### Setup

- successful Workspace/member transition

### Execute

1. Map Domain fact and persist outward intent.

### Required assertions

- producer owns event identity.
- scope IDs are stable.
- outbox commit follows source transaction.
- rollback emits no committed intent.

### Certification evidence

- Application/integration evidence.

## 65. WG-TST-CONC-MEM-001 — duplicate membership race

### Traceability

```text
Requirements: WGREQ144
PLAN: WG-CONC-001
Layer: T8
Preparation evidence state: GAP
```

### Existing source evidence

- No exact existing test was confirmed during preparation audit.

### Setup

- two concurrent add-member operations
- real PostgreSQL

### Execute

1. Synchronize race to compete before commit.

### Required assertions

- at most one effective membership/grant survives.
- conflict result is deterministic.

### Certification evidence

- Concurrency integration test.

## 66. WG-TST-CONC-OWNER-001 — last-owner concurrent downgrade/remove race

### Traceability

```text
Requirements: WGREQ145
PLAN: WG-CONC-001
Layer: T8
Preparation evidence state: GAP
```

### Existing source evidence

- No exact existing test was confirmed during preparation audit.

### Setup

- Workspace with two owners
- concurrent removal/downgrade operations that could leave zero owner

### Execute

1. Execute competing transactions.

### Required assertions

- database/application concurrency mechanism prevents zero active owners.
- losing operation fails predictably.

### Certification evidence

- PostgreSQL concurrency evidence.

### Gap / follow-up

- Domain owner-count checks alone do not prove race safety.

## 67. WG-TST-CONC-RPERM-001 — concurrent duplicate ACL grant

### Traceability

```text
Requirements: WGREQ147, WGREQ154
PLAN: WG-CONC-003
Layer: T8
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `GovernanceResourcePermissionFlowTests.cs :: Concurrent_Grants_ForSameActiveSubject_RaceOnUniqueIndex`

### Setup

- same active subject/resource ACL

### Execute

1. Submit concurrent grants.

### Required assertions

- database uniqueness resolves race.
- no duplicate active ACL survives.

### Certification evidence

- PostgreSQL integration result.

## 68. WG-TST-CONC-POL-001 — stale WorkspacePolicy write

### Traceability

```text
Requirements: WGREQ148
PLAN: WG-CONC-003
Layer: T8
Preparation evidence state: PLANNED
```

### Existing source evidence

- No exact existing test was confirmed during preparation audit.

### Setup

- only if WorkspacePolicy mutation is released

### Execute

1. Submit two stale competing updates.

### Required assertions

- lost update is detected or accepted policy explicitly defines merge/last-write behavior.

### Certification evidence

- Concurrency evidence.

### Gap / follow-up

- NOT-APPLICABLE if WorkspacePolicy remains Domain/persistence groundwork only.

## 69. WG-TST-CONC-SHARE-001 — ShareLink revoke/use race

### Traceability

```text
Requirements: WGREQ149, WGREQ156
PLAN: WG-CONC-003
Layer: T8/T7
Preparation evidence state: PLANNED
```

### Existing source evidence

- No exact existing test was confirmed during preparation audit.

### Setup

- only if public ShareLink consumption is released

### Execute

1. Race capability use against disable/revoke.

### Required assertions

- post-revocation operation cannot commit outside accepted race/consistency contract.

### Certification evidence

- Concurrency/security evidence.

### Gap / follow-up

- NOT-APPLICABLE when no ShareLink consumption path exists.

## 70. WG-TST-SEC-MASTER-001 — privilege escalation matrix

### Traceability

```text
Requirements: WGREQ150–WGREQ157
PLAN: WG-SEC-003
Layer: T7
Preparation evidence state: PARTIAL-SOURCE
```

### Existing source evidence

- `GovernanceResourcePermissionFlowTests.cs multiple grant/revoke/archive/comment scenarios`
- `HandlerAuthorizationBypassArchitectureTests.cs`

### Setup

- Guest/Member/Admin/Owner
- resource Viewer/Commenter/Editor/Manager/Owner
- explicit rules

### Execute

1. Execute role change, ACL grant/revoke, management actions and resource use.

### Required assertions

- no actor can grant/manage above authority.
- visibility is distinct from management.
- explicit deny wins where applicable.
- Owner special path cannot bypass missing/archived resource lifecycle.

### Certification evidence

- Security/integration matrix with exact test mapping.

### Gap / follow-up

- Certification must enumerate covered matrix cells and explicit gaps.

## 71. WG-TST-SEC-MASTER-002 — tenant spoofing matrix

### Traceability

```text
Requirements: WGREQ152
PLAN: WG-SEC-001
Layer: T7
Preparation evidence state: PARTIAL-SOURCE
```

### Existing source evidence

- `RlsRuntimeEnforcementTests.cs`
- `GovernanceResourcePermissionFlowTests.cs cross-scope cases`

### Setup

- valid Account/Workspace/resource combinations and mismatches

### Execute

1. Vary request AccountId, WorkspaceId and resource IDs across tenants.

### Required assertions

- scope mismatch never widens access.
- foreign rows unchanged.
- privacy-safe failure.

### Certification evidence

- Production-DI + PostgreSQL evidence.

## 72. WG-TST-SEC-MASTER-003 — authorization failure cannot become implicit allow

### Traceability

```text
Requirements: WGREQ151, WGREQ159, WGREQ161
PLAN: WG-REL-001
Layer: T7/T10
Preparation evidence state: GAP
```

### Existing source evidence

- No exact existing test was confirmed during preparation audit.

### Setup

- facts provider throws/unavailable
- cache unavailable
- producer semantic fact unavailable

### Execute

1. Inject controlled dependency failure through production-equivalent seam.

### Required assertions

- protected handler does not run.
- failure is categorized safely.
- no stale broader authority is substituted.

### Certification evidence

- Fault-injection test evidence.

## 73. WG-TST-SEC-MASTER-004 — secret telemetry scan

### Traceability

```text
Requirements: WGREQ109, WGREQ137, WGREQ156, WGREQ164
PLAN: WG-SEC-004
Layer: T0/T7
Preparation evidence state: GAP
```

### Existing source evidence

- No exact existing test was confirmed during preparation audit.

### Setup

- invitation/share tokens
- representative logs/events/problem responses

### Execute

1. Run static/runtime redaction scan.

### Required assertions

- no raw reusable token/hash secret is logged or emitted publicly.
- no Authorization/cookie credentials are captured.

### Certification evidence

- Static scan + representative runtime logs.

## 74. WG-TST-REL-PROV-001 — personal Workspace provisioning retry safety

### Traceability

```text
Requirements: WGREQ009, WGREQ158
PLAN: WG-PROV-001, WG-REL-002
Layer: T6/T10
Preparation evidence state: PARTIAL-SOURCE
```

### Existing source evidence

- `Notrelix.Infrastructure.Tests/Messaging/Consumers/WorkspaceProvisioningConsumerTests.cs :: RegistrationCompleted_ProvisionsPersonalWorkspaceUnderAccountTenant`

### Setup

- same registration event replayed/retried

### Execute

1. Deliver event once and again under retry conditions.

### Required assertions

- at most one canonical personal Workspace/owner membership exists.
- tenant scope remains correct.
- retry outcome is deterministic.

### Certification evidence

- Consumer/integration evidence.

### Gap / follow-up

- Existing named test confirms provisioning scope; explicit duplicate-delivery/idempotency proof must be confirmed or added.

## 75. WG-TST-REL-EVT-001 — outbox prevents publication-before-commit

### Traceability

```text
Requirements: WGREQ160
PLAN: WG-REL-002, WG-EVT-002
Layer: T6/T10
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `WorkspaceCreatedOutboxEvidenceTests.cs`
- `WorkspaceMembershipOutboxEvidenceTests.cs`

### Setup

- successful and rolled-back source transaction

### Execute

1. Execute transition and force rollback variant.

### Required assertions

- outward intent commits atomically with source.
- rollback leaves no committed fact.

### Certification evidence

- Integration evidence.

## 76. WG-TST-OBS-001 — authorization trace uses safe bounded fields

### Traceability

```text
Requirements: WGREQ162, WGREQ164, WGREQ165
PLAN: WG-OBS-001
Layer: T10/T7
Preparation evidence state: GAP
```

### Existing source evidence

- No exact existing test was confirmed during preparation audit.

### Setup

- representative allow/deny requests

### Execute

1. Capture trace/metrics/log output.

### Required assertions

- correlation exists.
- decision category and safe resource kind are available.
- secrets/policy JSON are absent.
- high-cardinality identifiers are not unsafe metric labels.

### Certification evidence

- Telemetry integration evidence.

## 77. WG-TST-PERF-001 — authorization hot-path query is bounded and index-supported

### Traceability

```text
Requirements: WGREQ166–WGREQ169
PLAN: WG-PERF-001
Layer: T10
Preparation evidence state: CERTIFICATION-ONLY
```

### Existing source evidence

- `AccessFactsQuery.Sql`
- `relevant PostgreSQL indexes`

### Setup

- representative tenant with realistic membership/rule/ACL cardinality

### Execute

1. Execute canonical facts query with EXPLAIN/ANALYZE where allowed.

### Required assertions

- query count is bounded per request.
- critical lookups use intended indexes.
- no N+1 role/rule/permission fetch.
- security filters are preserved.

### Certification evidence

- Plan/latency evidence from target-like PostgreSQL.
- No invented absolute threshold unless repository SLO defines one.

## 78. WG-TST-MIG-DB-001 — clean database migration

### Traceability

```text
Requirements: WGREQ176
PLAN: WG-MIG-004
Layer: T9
Preparation evidence state: EXISTING-SOURCE
```

### Existing source evidence

- `Notrelix.Integration.Tests/Data/MigrationSmokeTests.cs :: Migrations_WhenApplied_CreatesAll148Tables`
- `Migrations_WhenApplied_PublicSchemaIsEmpty`
- `Migrations_WhenApplied_CreatesOutboxTableWithRequiredColumns`

### Setup

- empty supported PostgreSQL database

### Execute

1. Apply canonical migrations/bootstrap.

### Required assertions

- schema builds successfully.
- expected schema separation exists.
- required messaging/runtime tables exist.
- RLS bootstrap verification follows repository deployment contract.

### Certification evidence

- Migration smoke + deployment/RLS verification evidence.

## 79. WG-TST-MIG-DB-002 — supported upgrade database

### Traceability

```text
Requirements: WGREQ176
PLAN: WG-MIG-004
Layer: T9
Preparation evidence state: GAP
```

### Existing source evidence

- No exact existing test was confirmed during preparation audit.

### Setup

- supported previous migration/schema fixture with representative Workspace/Governance rows

### Execute

1. Upgrade to candidate migration head.

### Required assertions

- rows preserve Account/Workspace/resource/action/role/ACL meaning.
- authorization continues after upgrade.
- no secret material is exposed during migration.

### Certification evidence

- Upgrade integration evidence.

### Gap / follow-up

- Generic migration smoke from empty DB is not sufficient for compatibility claims.

## 80. WG-TST-MIG-DB-003 — no pending EF model changes

### Traceability

```text
Requirements: WGREQ177
PLAN: WG-MIG-001, WG-MIG-004
Layer: T9
Preparation evidence state: CERTIFICATION-ONLY
```

### Existing source evidence

- No exact existing test was confirmed during preparation audit.

### Setup

- candidate source
- canonical EF migration tooling

### Execute

1. Run repository-supported pending-model check.

### Required assertions

- no unexplained model drift remains.

### Certification evidence

- Exact command/output in certification.

## 81. WG-TST-MIG-ID-001 — persisted ResourceKind/Action/Role/PermissionLevel compatibility

### Traceability

```text
Requirements: WGREQ172, WGREQ173
PLAN: WG-MIG-002
Layer: T9
Preparation evidence state: GAP
```

### Existing source evidence

- No exact existing test was confirmed during preparation audit.

### Setup

- fixture containing currently persisted identifiers

### Execute

1. Load/upgrade and execute representative authorization.

### Required assertions

- old supported values remain interpretable or are explicitly migrated.
- source rename alone never changes semantic meaning.

### Certification evidence

- Migration compatibility test.

### Gap / follow-up

- Required only when candidate changes persisted/public vocabulary; otherwise certification records NOT-CHANGED.

## 82. WG-TST-MIG-SECRET-001 — invitation/share token representation compatibility

### Traceability

```text
Requirements: WGREQ175
PLAN: WG-MIG-003
Layer: T9/T7
Preparation evidence state: PLANNED
```

### Existing source evidence

- No exact existing test was confirmed during preparation audit.

### Setup

- old supported hash version/token format fixture

### Execute

1. Upgrade and verify/rotate according to accepted contract.

### Required assertions

- valid supported old token remains verifiable or is explicitly invalidated by policy.
- plaintext secret is never required.

### Certification evidence

- Migration/security evidence.

### Gap / follow-up

- NOT-CHANGED when candidate does not alter token representation.

## 83. WG-TST-P2-CORE-001 — Workspace producer D5 evidence packet

### Traceability

```text
Requirements: WGREQ178
PLAN: WG-GATE-001, WG-CERT-HO-001
Layer: T0–T11
Preparation evidence state: CERTIFICATION-ONLY
```

### Existing source evidence

- No exact existing test was confirmed during preparation audit.

### Setup

- exact candidate SHA
- Workspace focused scenarios

### Execute

1. Execute required build/tests/migration/docs checks.

### Required assertions

- Workspace identity/containment/lifecycle contract has no blocking semantic gap.
- downstream invalidation rules are documented.
- evidence is exact-SHA.

### Certification evidence

- Certification rows + CI evidence.

## 84. WG-TST-P2-CORE-002 — Resource/Action producer D5 evidence packet

### Traceability

```text
Requirements: WGREQ179
PLAN: WG-GATE-001, WG-CERT-HO-001
Layer: T0/T5/T11
Preparation evidence state: CERTIFICATION-ONLY
```

### Existing source evidence

- No exact existing test was confirmed during preparation audit.

### Setup

- canonical resource/action registry
- consumer contract tests

### Execute

1. Run canonical-kind/action ownership and compatibility checks.

### Required assertions

- resource/action identity is stable.
- no forced naming migration remains.
- P3-A contract is explicit.

### Certification evidence

- Certification packet.

## 85. WG-TST-P2-CORE-003 — Permission producer D5 evidence packet

### Traceability

```text
Requirements: WGREQ180
PLAN: WG-GATE-002, WG-CERT-HO-001
Layer: T2/T6/T7
Preparation evidence state: CERTIFICATION-ONLY
```

### Existing source evidence

- No exact existing test was confirmed during preparation audit.

### Setup

- default deny
- PermissionRule precedence
- ACL management paths

### Execute

1. Execute focused policy/security scenarios.

### Required assertions

- effective permission semantics are deterministic.
- unknown/missing authority fails closed.
- escalation tests are green.

### Certification evidence

- Certification packet.

## 86. WG-TST-P2-CORE-004 — built-in Role D4+ evidence packet

### Traceability

```text
Requirements: WGREQ181
PLAN: WG-GATE-002
Layer: T1/T2/T7
Preparation evidence state: CERTIFICATION-ONLY
```

### Existing source evidence

- No exact existing test was confirmed during preparation audit.

### Setup

- Guest/Member/Admin/Owner matrix

### Execute

1. Execute representative role decisions.

### Required assertions

- role facts are stable.
- role mapping covers P2 protected slice.
- CustomRole absence does not silently alter core.

### Certification evidence

- Certification packet.

## 87. WG-TST-P2-CORE-005 — canonical authorization integration D5 evidence packet

### Traceability

```text
Requirements: WGREQ182, WGREQ185–WGREQ187
PLAN: WG-GATE-002
Layer: T5/T6/T7
Preparation evidence state: CERTIFICATION-ONLY
```

### Existing source evidence

- No exact existing test was confirmed during preparation audit.

### Setup

- pipeline/order
- facts provider
- policy evaluator
- RLS

### Execute

1. Run production-DI allow/deny/misconfig/cross-tenant suite.

### Required assertions

- one canonical enforcement path.
- facts/policy ownership is accepted.
- RLS is active defense-in-depth.
- denied handler has no side effect.

### Certification evidence

- Certification packet.

## 88. WG-TST-P2-CORE-006 — WorkManagement protected handshake D5 evidence packet

### Traceability

```text
Requirements: WGREQ182, WGREQ190
PLAN: WG-WM-004, WG-GATE-002
Layer: T6/T11
Preparation evidence state: CERTIFICATION-ONLY
```

### Existing source evidence

- No exact existing test was confirmed during preparation audit.

### Setup

- CreateBoard representative flow
- resource ACL flow

### Execute

1. Run exact P3-B handoff scenarios.

### Required assertions

- authorized representative flow succeeds.
- outsider/cross-tenant flow denies.
- WorkManagement remains resource semantic owner.

### Certification evidence

- Certification packet + downstream handoff record.

## 89. Secondary capability release gates

| Capability | Requirements | Scenario family | Release rule |
|---|---|---|---|
| Invitation | WGREQ024–WGREQ033 | `WG-TST-INV-*` | Domain + Application + API + transaction + token security + race as applicable. |
| Teams | WGREQ034–WGREQ037 | `WG-TST-TEAM-*` | Domain + Application + API; Workspace membership precondition; last-lead safety. |
| Spaces | WGREQ038–WGREQ041 | `WG-TST-SPACE-*` | Domain + Application + API; visibility semantics; no duplicate auth engine. |
| CustomRole | WGREQ068–WGREQ070 | `WG-TST-CROLE-*` | Do not promote beyond Domain until Application/API/runtime composition is proven. |
| WorkspacePolicy | WGREQ071–WGREQ075 | `WG-TST-POL-*` | Do not claim enforcement unless a concrete consumer/evaluator path exists. |
| ResourcePermission depth | WGREQ076–WGREQ079 | `WG-TST-RPERM-*` | Released lifecycle + grant ceiling + persistence + concurrency. |
| ShareLink | WGREQ097–WGREQ104 | `WG-TST-SHARE-*` | Domain lifecycle plus actual capability-token validation if released. |
| PermissionTemplate | WGREQ111–WGREQ113 | `WG-TST-TPL-*` | Domain-only vs delivered apply semantics must be explicit. |
| Audit/SecurityEvent | WGREQ105–WGREQ110 | `WG-TST-AUD-*` | Actual mechanism ownership and query/security evidence, not folder symmetry. |

## 90. P2 Milestone B mandatory minimum before protected WorkManagement release

The minimum evidence packet is non-optional:

```text
WG-TST-WSP-DOM-001
WG-TST-WSP-INT-001
WG-TST-WSP-APP-001

WG-TST-MEM-DOM-001
WG-TST-MEM-DOM-002
WG-TST-MEM-INF-001
WG-TST-MEM-INT-001

WG-TST-RES-ARCH-001
WG-TST-ACT-ARCH-001
WG-TST-RES-X-001

WG-TST-PERM-APP-001
WG-TST-PRULE-APP-001
WG-TST-ROLE-SEC-001

WG-TST-AUTHZ-APP-001
WG-TST-PIPE-ARCH-001
WG-TST-AUTHZ-ARCH-001
WG-TST-AUTHZ-ARCH-002
WG-TST-FACTS-INT-001
WG-TST-AUTHZ-SEC-001

WG-TST-RLS-SEC-001
WG-TST-RLS-SEC-002
WG-TST-RLS-SEC-003

WG-TST-WM-X-001
WG-TST-WM-X-002
WG-TST-WM-X-003
WG-TST-WM-X-004

WG-TST-SEC-MASTER-001
WG-TST-SEC-MASTER-002

WG-TST-MIG-DB-001
WG-TST-MIG-DB-003

WG-TST-API-OAS-001
WG-TST-P2-CORE-001
WG-TST-P2-CORE-002
WG-TST-P2-CORE-003
WG-TST-P2-CORE-004
WG-TST-P2-CORE-005
WG-TST-P2-CORE-006
```

A scenario may be proven by an existing test, a new focused test, or a documented exact-SHA gate only when its evidence type genuinely fits the claim.

No mandatory scenario may be silently omitted because a broad suite is green.
## 91. Cross-layer vertical scenario — Workspace creation

Required flow:

```text
HTTP / Application request
→ RequestContractBehavior
→ ExecutionContextBehavior
→ DataSessionBehavior
→ AccessControlBehavior
→ IdempotencyBehavior
→ Workspace Application handler
→ Workspace Domain
→ owner-local persistence
→ outbox
→ commit
```

Verify:

- Account scope is authoritative;
- role/verification gate is evaluated before source mutation;
- Domain creates valid Workspace;
- transaction commits source + required outbox intent;
- rollback produces no committed outward fact;
- API maps result/errors;
- no foreign private persistence is mutated.
## 92. Cross-layer vertical scenario — Membership admin

Required flow:

```text
protected member command
→ canonical access control
→ WorkspaceMember Domain invariant
→ owner safety
→ membership persistence
→ access-grant projection sync/revoke
→ outbox fact
→ commit
→ RLS sees resulting grant state
```

Verify both:

```text
add/activate/role-change
suspend/remove
```

The final persistence isolation state must agree with the authoritative membership state.
## 93. Cross-layer vertical scenario — Board authorization

Required flow:

```text
WorkManagement request
→ ExecutionContext
→ DataSession
→ AccessFacts
→ AccessPolicyEngine
→ AccessControlBehavior
→ WorkManagement handler
→ WorkManagement Domain/persistence
```

Verify:

- resource identity is `work-management.board`;
- Workspace/Account scope is coherent;
- resource facts are source-owned/architecture-approved;
- explicit Governance deny wins where applicable;
- management authority differs from simple visibility;
- outsider/cross-tenant actor cannot reach mutation;
- WorkManagement owns Board business invariants.
## 94. Cross-layer vertical scenario — Invitation acceptance

Required flow:

```text
token lookup/verification
→ invitation state validation
→ membership transition
→ access-grant projection
→ outward membership fact
→ one transaction
```

Verify:

- expired/revoked/wrong token cannot create membership;
- retry/already-member does not duplicate membership or event;
- partial Workspace-side failure rolls back;
- raw token does not become persisted/logged public state.
## 95. Privilege-escalation master matrix

| Actor/fact | Operation | Expected evidence |
|---|---|---|
| Guest | ordinary view/collaboration action | only accepted actions; no management by role alone |
| Member | Board/Page use action | resource policy determines; no ACL management by visibility alone |
| Admin | Workspace administration | allowed only for accepted Workspace-level actions |
| Owner | owner fast-path | cannot resurrect/mutate missing/deleted resource; lifecycle still checked |
| Manager ACL | grant/revoke equal/lower rank | may succeed if exact management action contract allows |
| Editor/Viewer ACL | manage ACL | must fail |
| Explicit Deny | otherwise allowed actor | deny wins at selected priority |
| Outsider | any protected Workspace resource | forbidden/not-found; no effect |
| Cross-tenant member | foreign resource | not-found/deny + RLS isolation |
| Share token | non-target resource/global action | must fail |

## 96. Tenant-isolation master matrix

| Dimension | Positive | Negative | Required layer |
|---|---|---|---|
| Account | A reads A | A cannot read/mutate B | T6+T7 RLS/Application |
| Workspace | member reads own Workspace | grant in W1 does not expose W2 | T6+T7 |
| Membership | active member has accepted scope | suspended/removed stale access denied | T6+T7 |
| Resource | authorized target accessible | foreign/hidden resource not disclosed | T6+T7 |
| ACL | exact resource/subject grant applies | grant does not bleed to sibling resource | T6 |
| Background | derived tenant grant applies | missing grant/global ambient scope denied | T6+T7 |
| Token/share | target capability applies | wrong tenant/resource/action denied | T7 |

## 97. Test fixture design

Core fixture should create stable identities for:

```text
Account A
  Workspace A1
    Owner A
    Admin A
    Member A
    Guest A
    Outsider X (not a member)
    Board A
    Page A

  Workspace A2
    Member A2

Account B
  Workspace B1
    Owner B
    Board B
    Page B
```

Additional facts are layered per scenario:

```text
PermissionRule allow/deny
ResourcePermission Viewer/Commenter/Editor/Manager/Owner
ShareLink
Invitation pending/expired/revoked
CustomRole rows
WorkspacePolicy rows
access_grants
```

Never let the fixture itself silently grant broad roles/ACLs that make negative scenarios meaningless.
## 98. PostgreSQL fixture rules

Use real PostgreSQL for claims involving:

- RLS;
- Npgsql transaction/session behavior;
- unique indexes;
- concurrency conflicts;
- `AccessFactsQuery`;
- migration/bootstrap;
- outbox atomicity;
- cross-tenant persistence isolation.

An EF in-memory provider or mock DbSet does not prove these claims.
## 99. Production-DI fixture rules

Use production registration for claims involving:

- pipeline order;
- `AccessControlBehavior`;
- `PostgresAccessFactsProvider`;
- `AccessPolicyEngine`;
- cross-context adapter wiring;
- request descriptor registry;
- RLS/data-session lifecycle;
- protected handler reachability.

Do not manually instantiate a fake graph that omits a production security component and then call the result an integration proof.
## 100. Test anti-patterns

- Do not mock authorization decision in the representative authorization integration tests.
- Do not mock database uniqueness for membership/ACL concurrency invariants.
- Do not treat API unauthenticated tests as proof of business authorization.
- Do not assert role strings in every handler; assert the centralized policy outcome.
- Do not make CustomRole/WorkspacePolicy tests imply runtime integration if only Domain source exists.
- Do not treat ResourcePermissionInheritanceCacheEntry as proof of working inheritance.
- Do not assert HTTP message text when stable status/error code is the contract.
- Do not auto-update OpenAPI/event snapshots before reviewing semantic diff.
- Do not mark broad solution green as proof that a mandatory scenario executed.
- Do not use RLS row visibility as the only proof an action is authorized.
- Do not test cross-context semantics by directly injecting the producer's private DbContext.
- Do not rewrite historical decision tests/fixtures to hide compatibility debt.

## 101. Infrastructure verification checklist

- [ ] Workspace/WorkspaceMember/Invitation/Team/Space mappings match Domain scope.
- [ ] Governance mappings preserve AccountId/WorkspaceId/resource identity.
- [ ] membership unique constraint exists and is tested against PostgreSQL.
- [ ] ResourcePermission active uniqueness/conflict semantics are explicit.
- [ ] AccessFactsQuery uses active request transaction.
- [ ] RLS context helpers and policies are installed/verified.
- [ ] access-grant projection synchronization has repair/invalidation semantics.
- [ ] secret hash/token columns do not expose raw reusable secrets.
- [ ] migration head matches EF model.

## 102. API verification checklist

- [ ] every Workspace/Governance endpoint maps to one Application request/use case
- [ ] protected endpoint requires correct authentication/security classification
- [ ] public token endpoint is explicitly classified
- [ ] validation and not-found/forbidden mapping is stable
- [ ] no endpoint performs direct DbContext persistence
- [ ] no endpoint implements independent role matrix
- [ ] secret fields are minimized
- [ ] OpenAPI operation IDs are stable and unique

## 103. Architecture verification checklist

- [ ] Workspaces Domain pure
- [ ] Governance Domain pure
- [ ] module-first Application topology preserved
- [ ] IWorkspaceDbContext owner-local only
- [ ] IGovernanceDbContext owner-local only
- [ ] AccessPolicyEngine Governance-owned and persistence-free
- [ ] IAccessPolicyEvaluator has one canonical implementation
- [ ] AccessControlBehavior depends only on neutral seam
- [ ] scoped requests declare security contract
- [ ] protected handlers do not inject authorization services
- [ ] handler role checks are only registered business invariants
- [ ] RLS remains Infrastructure defense-in-depth
- [ ] resource owner remains semantic owner of resource facts

## 104. Migration applicability matrix

| Change | Mandatory evidence |
|---|---|
| No schema/public identifier change | clean DB + pending-model gate; supported-upgrade may be NOT-CHANGED if repository policy allows |
| Workspace/member schema | clean DB + upgrade fixture + uniqueness/RLS |
| ResourceKind/PermissionAction rename | data inventory + compatibility migration + old-row authorization test |
| WorkspaceRole/PermissionLevel semantic change | stored value fixture + behavior compatibility |
| PermissionRule schema/precedence | upgrade fixture + evaluator behavior |
| Share/invite hash format | version/verifier compatibility or explicit invalidation |
| RLS policy/helper change | clean apply + runtime enforcement + connection/session lifecycle |
| OpenAPI breaking change | generated diff + consumer migration/deprecation |

## 105. CI mapping

CERTIFICATION must resolve the repository's actual current workflow/job names.

At minimum evidence must include the equivalent of:

```text
restore/build
Domain tests
Application tests
Infrastructure tests
API tests
Architecture tests
Integration tests
OpenAPI/export drift
migration/model drift
RLS/security gates
docs governance
dependency/security gates where repository requires them
```

Preparation history shows backend CI can be path-filtered.

Therefore:

```text
current HEAD green
```

is insufficient if backend jobs were skipped.

For P2 certification, either:

- the exact candidate SHA ran the required backend jobs; or
- an explicitly documented equivalent source SHA ran them and the diff to candidate cannot affect those claims, if repository governance permits such equivalence.

Preferred evidence is exact-SHA execution.
## 106. CI non-zero requirement

For every mandatory test command:

```text
process exit success
AND
relevant tests discovered > 0
AND
required tests not skipped
```

A command that succeeds with zero relevant tests does not satisfy the gate.
## 107. Flaky/skipped test rule

A mandatory security, tenant-isolation, concurrency, migration or handoff test that is:

```text
skipped
quarantined
flaky
timed out
environment-unavailable
```

is not silently counted as PASS.

CERTIFICATION records:

```text
BLOCKING
or
explicit non-blocking rationale approved by the owning quality gate
```

according to the claim being made.
## 108. Recommended implementation order

When gaps require new tests, implement in this order:

```text
1. Domain invariants already required by source
2. architecture guards
3. Application/policy unit tests
4. PostgreSQL facts/RLS/constraint tests
5. production-DI protected verticals
6. security/tenant escalation matrix
7. concurrency
8. migration compatibility
9. API/OpenAPI
10. reliability/observability/performance
11. secondary capability tests
```

Do not write advanced CustomRole/Policy/Share tests ahead of unresolved semantics.
## 109. Requirement-family completeness — WGREQ001–WGREQ011

Must be covered by:

```text
WG-TST-WSP-DOM-001..004
WG-TST-WSP-INT-001
WG-TST-WSP-APP-001
WG-TST-WSP-EVT-001
WG-TST-REL-PROV-001
Workspace settings/home scenarios in secondary release scope
```

Certification must explicitly state whether WorkspaceHome is delivered, partial, or deferred.
## 110. Requirement-family completeness — WGREQ012–WGREQ023

Must be covered by:

```text
WG-TST-MEM-DOM-001
WG-TST-MEM-DOM-002
WG-TST-MEM-INF-001
WG-TST-MEM-APP-001
WG-TST-MEM-INT-001
WG-TST-MEM-EVT-001
WG-TST-CONC-MEM-001
WG-TST-CONC-OWNER-001
```

Identity lifecycle/historical attribution must be verified through approved upstream facts/contracts.
## 111. Requirement-family completeness — WGREQ024–WGREQ043

Invitation:

```text
WG-TST-INV-DOM-001
WG-TST-INV-INT-001
WG-TST-INV-SEC-001
WG-TST-INV-CONC-001
```

Teams/Spaces/Rules:

```text
WG-TST-TEAM-DOM-001
WG-TST-TEAM-APP-001
WG-TST-SPACE-DOM-001
WG-TST-SPACE-AUTHZ-001
WG-TST-RULE-ARCH-001
```
## 112. Requirement-family completeness — WGREQ044–WGREQ079

Resource/action/permission/role/policy/ACL families require:

```text
WG-TST-RES-ARCH-001
WG-TST-ACT-ARCH-001
WG-TST-RES-X-001

WG-TST-PERM-APP-001
WG-TST-PRULE-APP-001
WG-TST-PERM-CACHE-001

WG-TST-ROLE-DOM-001
WG-TST-ROLE-SEC-001
WG-TST-CROLE-DOM-001

WG-TST-POL-DOM-001
WG-TST-POL-APP-001

WG-TST-RPERM-DOM-001
WG-TST-RPERM-INT-001
WG-TST-RPERM-INF-001
```

Secondary scenarios may be NOT-APPLICABLE for P2 protected core but must never be mislabeled delivered.
## 113. Requirement-family completeness — WGREQ080–WGREQ113

Effective authorization:

```text
WG-TST-AUTHZ-APP-001
WG-TST-PIPE-ARCH-001
WG-TST-AUTHZ-ARCH-001
WG-TST-AUTHZ-ARCH-002
WG-TST-FACTS-INT-001
WG-TST-AUTHZ-SEC-001
```

Share/template/audit:

```text
WG-TST-SHARE-DOM-001
WG-TST-SHARE-SEC-001
WG-TST-TPL-DOM-001
WG-TST-AUD-ARCH-001
```

A Domain test never substitutes for a missing released Application/API flow.
## 114. Requirement-family completeness — WGREQ114–WGREQ143

Cross-context / data/API/event:

```text
WG-TST-UP-X-001
WG-TST-WM-X-001..004
WG-TST-DOC-X-001
WG-TST-BILL-X-001
WG-TST-INTG-X-001
WG-TST-OWN-ARCH-001
WG-TST-OWN-ARCH-002
WG-TST-API-WSP-001
WG-TST-API-GOV-001
WG-TST-API-OAS-001
WG-TST-EVT-WSP-001
```

Any additional downstream contract required by released scope must receive an explicit scenario ID before certification.
## 115. Requirement-family completeness — WGREQ144–WGREQ177

Concurrency/security/reliability/observability/performance/migration:

```text
WG-TST-CONC-MEM-001
WG-TST-CONC-OWNER-001
WG-TST-CONC-RPERM-001
WG-TST-CONC-POL-001
WG-TST-CONC-SHARE-001

WG-TST-SEC-MASTER-001
WG-TST-SEC-MASTER-002
WG-TST-SEC-MASTER-003
WG-TST-SEC-MASTER-004

WG-TST-REL-PROV-001
WG-TST-REL-EVT-001
WG-TST-OBS-001
WG-TST-PERF-001

WG-TST-MIG-DB-001
WG-TST-MIG-DB-002
WG-TST-MIG-DB-003
WG-TST-MIG-ID-001
WG-TST-MIG-SECRET-001
```
## 116. Requirement-family completeness — WGREQ178–WGREQ190

P2 gate and synchronization:

```text
WG-TST-P2-CORE-001
WG-TST-P2-CORE-002
WG-TST-P2-CORE-003
WG-TST-P2-CORE-004
WG-TST-P2-CORE-005
WG-TST-P2-CORE-006
```

These are certification packets, not ordinary unit tests.

They must include:

```text
exact candidate SHA
source classification
named scenario results
production-DI evidence
PostgreSQL evidence where applicable
architecture gate result
migration/model state
OpenAPI/docs state
known debt
P3 handoff
```
## 117. Preparation-time gap register

| Gap ID | Gap | Blocking for P2 Milestone B? |
|---|---|---|
| WG-TEST-GAP-001 | Exact DB-level duplicate membership race proof not confirmed | YES |
| WG-TEST-GAP-002 | Explicit last-owner concurrent transaction proof not confirmed | YES |
| WG-TEST-GAP-003 | Membership suspend/remove → access-grant revocation runtime proof must be confirmed | YES |
| WG-TEST-GAP-004 | Same-priority PermissionRule allow+deny/time-window matrix may need extension | YES if current policy relies on it |
| WG-TEST-GAP-005 | Board-specific cross-account negative P2 gate case should be explicit | YES |
| WG-TEST-GAP-006 | Governance transport-level API test suite not confirmed by dedicated file | NO for P2 core if integration+OpenAPI prove release surface; required for full API DoD |
| WG-TEST-GAP-007 | Authorization dependency fault-injection/fail-closed proof not confirmed | YES for D5 reliability claim |
| WG-TEST-GAP-008 | Secret telemetry scan not confirmed | YES for full security certification |
| WG-TEST-GAP-009 | Supported upgrade DB fixture not confirmed | YES if candidate includes material schema/identifier change; otherwise NOT-CHANGED |
| WG-TEST-GAP-010 | ShareLink end-to-end capability-token consumption path not confirmed | NO for P2 core; blocks ShareLink full release |
| WG-TEST-GAP-011 | WorkspacePolicy runtime composition not confirmed | NO for P2 core; blocks claiming policy enforcement |
| WG-TEST-GAP-012 | CustomRole runtime composition/Application/API not confirmed | NO for P2 core; blocks CustomRole release |
| WG-TEST-GAP-013 | PermissionTemplate application use case not confirmed | NO for P2 core; blocks template-apply release |
| WG-TEST-GAP-014 | Audit/SecurityEvent product mechanism needs explicit classification | NO for P2 core; blocks full-scope claim |

## 118. Certification handoff format

For every mandatory scenario, CERTIFICATION records:

```text
Scenario:
Requirement:
PLAN unit:
Candidate SHA:
Evidence state:
Command:
Project:
Test filter/file:
Provider:
Production DI:
Started:
Completed:
Passed:
Failed:
Skipped:
Result:
Evidence artifact:
Notes:
```

For architecture/source-review claims:

```text
Scenario:
Candidate SHA:
Source paths:
Gate/test:
Observed rule:
Violation count:
Result:
```

For migration/CI claims:

```text
Gate:
Candidate SHA:
Workflow/run or local command:
Job:
Result:
Skipped?:
Relevant test count:
Artifact/log:
```
## 119. Missing test handling

If a mandatory scenario has no executable test:

```text
do not mark verified
record GAP
record owning PLAN work unit
record whether it blocks P2 core, secondary release, or full-team certification
```

Source inspection may support design classification but cannot replace runtime evidence for runtime claims.
## 120. Existing test handling

If a matching test exists in source:

```text
EXISTING-SOURCE
```

until it is executed on the candidate SHA.

At certification:

```text
EXISTING-SOURCE + executed green exact-SHA
→ PASS evidence

EXISTING-SOURCE + not executed
→ NOT_EVALUATED

EXISTING-SOURCE + skipped/flaky
→ BLOCKED or explicitly qualified
```
## 121. Test-review checklist — Workspace

- [ ] Workspace Account containment tested
- [ ] archive/delete/restore semantics tested
- [ ] protected mutations go through pipeline
- [ ] cross-account negative path tested
- [ ] outbox commit/rollback evidence present
- [ ] settings/home scope honestly classified

## 122. Test-review checklist — Membership

- [ ] membership lifecycle tested
- [ ] last-owner Domain rule tested
- [ ] DB uniqueness/race tested
- [ ] owner race tested
- [ ] membership authorization tested
- [ ] access-grant projection add/update/revoke tested
- [ ] RLS reflects grant lifecycle
- [ ] membership outward event emitted exactly once

## 123. Test-review checklist — Authorization

- [ ] AccessControlBehavior is canonical
- [ ] pipeline order exact
- [ ] descriptor classification complete
- [ ] facts provider uses active transaction
- [ ] AccessPolicyEngine persistence-free
- [ ] default deny tested
- [ ] deny precedence tested
- [ ] grant ceiling tested
- [ ] resource facts ownership classified
- [ ] missing context/dependency fails closed
- [ ] denied handler has no side effect

## 124. Test-review checklist — RLS/security

- [ ] missing RLS context sees no rows
- [ ] no grant sees no rows
- [ ] cross-account denied
- [ ] cross-workspace denied
- [ ] background scope bounded
- [ ] session context transaction-local
- [ ] tenant spoofing matrix complete
- [ ] secret telemetry scan complete
- [ ] not-found privacy verified

## 125. Test-review checklist — migration/contracts

- [ ] clean database succeeds
- [ ] supported upgrade evaluated
- [ ] pending model changes clear
- [ ] resource/action persisted compatibility reviewed
- [ ] role/permission persisted compatibility reviewed
- [ ] OpenAPI diff reviewed
- [ ] event contract compatibility reviewed
- [ ] docs refer to current execution paths/mechanisms

## 126. Definition of Done — TESTS artifact

- [ ] Every WGREQ family maps to at least one scenario family.
- [ ] Every P2 Milestone B mandatory claim maps to executable evidence.
- [ ] Existing source tests are labeled EXISTING-SOURCE rather than PASS.
- [ ] Known test gaps are explicit and classified as blocking/non-blocking.
- [ ] Production-DI requirements are stated for runtime security claims.
- [ ] PostgreSQL is required for RLS/constraint/transaction/migration claims.
- [ ] Cross-context facts/policy ownership is tested architecturally and behaviorally.
- [ ] Secondary Domain-present capabilities cannot be mistaken for released full-stack features.
- [ ] Exact-SHA and non-zero-test rules are explicit.
- [ ] CERTIFICATION handoff schema is complete.
- [ ] Current authorization naming matches AccessControlBehavior / AccessFacts / AccessPolicyEngine.
- [ ] Canonical execution paths use `docs/workstreams/executions/`.

## 127. TESTS document does not imply current implementation passes

The presence of this artifact means only:

```text
the verification contract is defined
```

It does not mean:

```text
all tests exist
all tests pass
P2 is D4
P2 is D5
P3-B is open
full Workspace & Governance is complete
```

Those statements require `workspace-governance.certification.md` with exact-SHA evidence.
## 128. Final verification rule

For Workspace & Governance:

```text
source presence
≠
test existence
≠
test execution
≠
verification
≠
stability
```

The valid chain is:

```text
accepted requirement
→ source implementation
→ executable scenario
→ candidate-SHA execution
→ evidence
→ certification
→ downstream handoff
```

Any missing link remains visible.

# Preserved canonical scenario granularity

The scenarios below restore canonical scenario IDs present in the `develop` TESTS baseline that were collapsed by the earlier rewrite. They preserve the existing verification granularity while binding the checks to the current authorization architecture. Their preparation state is `NOT_EVALUATED` until executed on the accepted candidate SHA.

## WG-TST-WSP-X-001 — Workspace archive/delete effects explicit

### Traceability

```text
Requirements: WGREQ006
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **Workspace archive/delete effects explicit**.
- Verify the named Workspace behavior against canonical Account containment and source-defined Domain lifecycle semantics.
- Where the scenario reaches Application/runtime, use the canonical access-control/data-session path; do not treat API authentication or RLS alone as business authorization.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-WSP-APP-002 — Workspace provisioning ownership

### Traceability

```text
Requirements: WGREQ008
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **Workspace provisioning ownership**.
- Verify the named Workspace behavior against canonical Account containment and source-defined Domain lifecycle semantics.
- Where the scenario reaches Application/runtime, use the canonical access-control/data-session path; do not treat API authentication or RLS alone as business authorization.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-WSP-INT-002 — provisioning retry idempotency

### Traceability

```text
Requirements: WGREQ009
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **provisioning retry idempotency**.
- Verify the named Workspace behavior against canonical Account containment and source-defined Domain lifecycle semantics.
- Where the scenario reaches Application/runtime, use the canonical access-control/data-session path; do not treat API authentication or RLS alone as business authorization.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-WSP-ARCH-001 — settings ownership

### Traceability

```text
Requirements: WGREQ010
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **settings ownership**.
- Verify the named Workspace behavior against canonical Account containment and source-defined Domain lifecycle semantics.
- Where the scenario reaches Application/runtime, use the canonical access-control/data-session path; do not treat API authentication or RLS alone as business authorization.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-WSP-APP-003 — WorkspaceHome classification

### Traceability

```text
Requirements: WGREQ011
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **WorkspaceHome classification**.
- Verify the named Workspace behavior against canonical Account containment and source-defined Domain lifecycle semantics.
- Where the scenario reaches Application/runtime, use the canonical access-control/data-session path; do not treat API authentication or RLS alone as business authorization.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-MEM-APP-002 — remove member requires authorization

### Traceability

```text
Requirements: WGREQ017
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **remove member requires authorization**.
- Use `WorkspaceMember` as the single Workspace membership truth.
- Verify owner safety, effective-access removal and `authz.access_grants` synchronization where the transition changes persistence authority.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-MEM-APP-003 — self leave allowed/denied according to canonical policy

### Traceability

```text
Requirements: WGREQ018
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **self leave allowed/denied according to canonical policy**.
- Use `WorkspaceMember` as the single Workspace membership truth.
- Verify owner safety, effective-access removal and `authz.access_grants` synchronization where the transition changes persistence authority.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-MEM-CONC-001 — last-admin concurrent removal

### Traceability

```text
Requirements: WGREQ019, WGREQ145
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **last-admin concurrent removal**.
- Use `WorkspaceMember` as the single Workspace membership truth.
- Verify owner safety, effective-access removal and `authz.access_grants` synchronization where the transition changes persistence authority.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-MEM-APP-004 — role association delegates to Governance

### Traceability

```text
Requirements: WGREQ020
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **role association delegates to Governance**.
- Use `WorkspaceMember` as the single Workspace membership truth.
- Verify owner safety, effective-access removal and `authz.access_grants` synchronization where the transition changes persistence authority.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-MEM-INT-002 — disabled Identity User loses effective access

### Traceability

```text
Requirements: WGREQ022, WGREQ117
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **disabled Identity User loses effective access**.
- Use `WorkspaceMember` as the single Workspace membership truth.
- Verify owner safety, effective-access removal and `authz.access_grants` synchronization where the transition changes persistence authority.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-MEM-X-001 — removed member historical attribution preserved

### Traceability

```text
Requirements: WGREQ023
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **removed member historical attribution preserved**.
- Use `WorkspaceMember` as the single Workspace membership truth.
- Verify owner safety, effective-access removal and `authz.access_grants` synchronization where the transition changes persistence authority.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-INV-APP-001 — valid invite target

### Traceability

```text
Requirements: WGREQ026
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **valid invite target**.
- Exercise the current invitation state machine, token-hash/generation rules and membership transaction boundary.
- No invalid/expired/revoked/replayed secret may create duplicate or stale access.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-INV-APP-002 — inviter cannot grant stronger access than authorized

### Traceability

```text
Requirements: WGREQ027, WGREQ154
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **inviter cannot grant stronger access than authorized**.
- Exercise the current invitation state machine, token-hash/generation rules and membership transaction boundary.
- No invalid/expired/revoked/replayed secret may create duplicate or stale access.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-INV-APP-003 — expired invitation rejected

### Traceability

```text
Requirements: WGREQ028
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **expired invitation rejected**.
- Exercise the current invitation state machine, token-hash/generation rules and membership transaction boundary.
- No invalid/expired/revoked/replayed secret may create duplicate or stale access.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-INV-APP-004 — revoked invitation rejected

### Traceability

```text
Requirements: WGREQ029
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **revoked invitation rejected**.
- Exercise the current invitation state machine, token-hash/generation rules and membership transaction boundary.
- No invalid/expired/revoked/replayed secret may create duplicate or stale access.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-INV-INT-002 — membership event reuse across membership sources

### Traceability

```text
Requirements: WGREQ030
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **membership event reuse across membership sources**.
- Exercise the current invitation state machine, token-hash/generation rules and membership transaction boundary.
- No invalid/expired/revoked/replayed secret may create duplicate or stale access.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-INV-API-001 — invitation lookup minimizes information

### Traceability

```text
Requirements: WGREQ032
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **invitation lookup minimizes information**.
- Exercise the current invitation state machine, token-hash/generation rules and membership transaction boundary.
- No invalid/expired/revoked/replayed secret may create duplicate or stale access.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-INV-SEC-002 — invitation secret absent from logs

### Traceability

```text
Requirements: WGREQ033, WGREQ164
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **invitation secret absent from logs**.
- Exercise the current invitation state machine, token-hash/generation rules and membership transaction boundary.
- No invalid/expired/revoked/replayed secret may create duplicate or stale access.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-TEAM-DOM-002 — Team lifecycle downstream references

### Traceability

```text
Requirements: WGREQ036
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **Team lifecycle downstream references**.
- Preserve Workspace containment and Workspace-membership preconditions.
- Team facts do not create independent Workspace authority unless an explicit Governance subject contract is released.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-TEAM-AUTHZ-001 — Team as Governance subject only when supported

### Traceability

```text
Requirements: WGREQ037
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **Team as Governance subject only when supported**.
- Preserve Workspace containment and Workspace-membership preconditions.
- Team facts do not create independent Workspace authority unless an explicit Governance subject contract is released.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-SPACE-DOM-002 — Space lifecycle

### Traceability

```text
Requirements: WGREQ039
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **Space lifecycle**.
- Preserve Workspace containment/lifecycle.
- `SpaceVisibility` is a source-owned fact and must not become a second permission evaluator.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-SPACE-ARCH-001 — visibility does not become second authorization engine

### Traceability

```text
Requirements: WGREQ041
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **visibility does not become second authorization engine**.
- Preserve Workspace containment/lifecycle.
- `SpaceVisibility` is a source-owned fact and must not become a second permission evaluator.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-RULE-ARCH-002 — no rule-engine duplication

### Traceability

```text
Requirements: WGREQ043
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **no rule-engine duplication**.
- Keep Workspace rule helpers limited to Workspace invariants.
- Reject duplication of Governance permission policy or Automation rule-engine semantics.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-RES-ARCH-001A — resource facts adapter ownership

### Traceability

```text
Requirements: WGREQ044, WGREQ093, WGREQ186
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **resource facts adapter ownership**.
- Resource identity/lifecycle facts remain owned by the resource context and are exposed as neutral facts.
- The current WorkManagement SQL composite read is explicitly classified; it is not a license for new foreign private persistence coupling.
- For new semantic facts, the concrete private-persistence reader is owned by the resource context or by an explicitly approved shared composite-read mechanism.
- Do not reintroduce `Governance evaluator → IWorkManagementDbContext`.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-RES-ARCH-001B — facts provider is transport-neutral

### Traceability

```text
Requirements: WGREQ093, WGREQ096, WGREQ186
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **facts provider is transport-neutral**.
- Resource identity/lifecycle facts remain owned by the resource context and are exposed as neutral facts.
- The current WorkManagement SQL composite read is explicitly classified; it is not a license for new foreign private persistence coupling.
- Canonical facts/application contracts contain no HTTP/gRPC/broker/provider types.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-RES-ARCH-001C — facts do not encode a second policy engine

### Traceability

```text
Requirements: WGREQ083, WGREQ094, WGREQ186
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **facts do not encode a second policy engine**.
- Resource identity/lifecycle facts remain owned by the resource context and are exposed as neutral facts.
- The current WorkManagement SQL composite read is explicitly classified; it is not a license for new foreign private persistence coupling.
- Resource projections return neutral existence/lifecycle/audience/relationship facts and do not independently decide Governance actions.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-RES-DOM-001 — resource authorization category stability

### Traceability

```text
Requirements: WGREQ045
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **resource authorization category stability**.
- Resource identity/lifecycle facts remain owned by the resource context and are exposed as neutral facts.
- The current WorkManagement SQL composite read is explicitly classified; it is not a license for new foreign private persistence coupling.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-RES-ARCH-002 — no CLR-name authorization identity

### Traceability

```text
Requirements: WGREQ045
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **no CLR-name authorization identity**.
- Resource identity/lifecycle facts remain owned by the resource context and are exposed as neutral facts.
- The current WorkManagement SQL composite read is explicitly classified; it is not a license for new foreign private persistence coupling.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-RES-APP-001 — resource ID treated opaquely

### Traceability

```text
Requirements: WGREQ046
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **resource ID treated opaquely**.
- Resource identity/lifecycle facts remain owned by the resource context and are exposed as neutral facts.
- The current WorkManagement SQL composite read is explicitly classified; it is not a license for new foreign private persistence coupling.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-RES-INT-001 — Account/Workspace/resource scope consistency

### Traceability

```text
Requirements: WGREQ047, WGREQ152
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **Account/Workspace/resource scope consistency**.
- Resource identity/lifecycle facts remain owned by the resource context and are exposed as neutral facts.
- The current WorkManagement SQL composite read is explicitly classified; it is not a license for new foreign private persistence coupling.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-RES-ARCH-003 — resource registration explicit

### Traceability

```text
Requirements: WGREQ048
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **resource registration explicit**.
- Resource identity/lifecycle facts remain owned by the resource context and are exposed as neutral facts.
- The current WorkManagement SQL composite read is explicitly classified; it is not a license for new foreign private persistence coupling.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-RES-CONTRACT-001 — resource owner controls action declaration

### Traceability

```text
Requirements: WGREQ049, WGREQ094
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **resource owner controls action declaration**.
- Resource identity/lifecycle facts remain owned by the resource context and are exposed as neutral facts.
- The current WorkManagement SQL composite read is explicitly classified; it is not a license for new foreign private persistence coupling.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-ACT-DOM-001 — stable business action identity

### Traceability

```text
Requirements: WGREQ050, WGREQ051
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **stable business action identity**.
- Use stable business-semantic `PermissionAction` identity rather than HTTP verbs or CLR names.
- Any persisted/public rename requires compatibility evidence.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-ACT-MIG-001 — persisted action rename requires migration

### Traceability

```text
Requirements: WGREQ052, WGREQ172
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **persisted action rename requires migration**.
- Use stable business-semantic `PermissionAction` identity rather than HTTP verbs or CLR names.
- Any persisted/public rename requires compatibility evidence.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-PERM-DOM-001 — canonical Permission meaning

### Traceability

```text
Requirements: WGREQ054
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **canonical Permission meaning**.
- Verify default-deny, tenant/resource scoping and accepted Permission/PermissionRule semantics through `AccessPolicyEngine`.
- Revocation/staleness cannot widen authority.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-PERM-MIG-001 — persisted permission identity stable

### Traceability

```text
Requirements: WGREQ055
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **persisted permission identity stable**.
- Verify default-deny, tenant/resource scoping and accepted Permission/PermissionRule semantics through `AccessPolicyEngine`.
- Revocation/staleness cannot widen authority.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-PERM-INT-001 — Workspace A permission cannot authorize Workspace B

### Traceability

```text
Requirements: WGREQ056, WGREQ152
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **Workspace A permission cannot authorize Workspace B**.
- Verify default-deny, tenant/resource scoping and accepted Permission/PermissionRule semantics through `AccessPolicyEngine`.
- Revocation/staleness cannot widen authority.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-PERM-APP-002 — default deny

### Traceability

```text
Requirements: WGREQ058, WGREQ085
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **default deny**.
- Verify default-deny, tenant/resource scoping and accepted Permission/PermissionRule semantics through `AccessPolicyEngine`.
- Revocation/staleness cannot widen authority.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-PERM-INT-002 — permission cache revocation

### Traceability

```text
Requirements: WGREQ059, WGREQ153
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **permission cache revocation**.
- Verify default-deny, tenant/resource scoping and accepted Permission/PermissionRule semantics through `AccessPolicyEngine`.
- Revocation/staleness cannot widen authority.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-PRULE-SEC-001 — client claims not trusted as rule facts

### Traceability

```text
Requirements: WGREQ062
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **client claims not trusted as rule facts**.
- Verify active/time-bounded rule selection, winning priority and deny precedence as implemented.
- Client-provided claims never become trusted rule facts without server-side validation.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-ROLE-APP-001 — built-in role maps to expected permission set

### Traceability

```text
Requirements: WGREQ064
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **built-in role maps to expected permission set**.
- Treat Workspace role as an authorization fact, not the whole policy engine.
- Assignment/removal/owner safety and authority ceilings must be explicit.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-ROLE-DOM-002 — built-in Role ID stable across display-name change

### Traceability

```text
Requirements: WGREQ065
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **built-in Role ID stable across display-name change**.
- Treat Workspace role as an authorization fact, not the whole policy engine.
- Assignment/removal/owner safety and authority ceilings must be explicit.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-ROLE-APP-002 — role assignment to supported subject

### Traceability

```text
Requirements: WGREQ066
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **role assignment to supported subject**.
- Treat Workspace role as an authorization fact, not the whole policy engine.
- Assignment/removal/owner safety and authority ceilings must be explicit.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-ROLE-INT-001 — Workspace-scoped role does not leak

### Traceability

```text
Requirements: WGREQ067
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **Workspace-scoped role does not leak**.
- Treat Workspace role as an authorization fact, not the whole policy engine.
- Assignment/removal/owner safety and authority ceilings must be explicit.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-ROLE-APP-003 — custom role not required for P2 gate

### Traceability

```text
Requirements: WGREQ068
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **custom role not required for P2 gate**.
- Treat Workspace role as an authorization fact, not the whole policy engine.
- Assignment/removal/owner safety and authority ceilings must be explicit.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-ROLE-DOM-003 — role deletion leaves no broader access

### Traceability

```text
Requirements: WGREQ070
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **role deletion leaves no broader access**.
- Treat Workspace role as an authorization fact, not the whole policy engine.
- Assignment/removal/owner safety and authority ceilings must be explicit.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-POL-APP-002 — policy composition precedence

### Traceability

```text
Requirements: WGREQ073, WGREQ084
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **policy composition precedence**.
- Do not infer runtime WorkspacePolicy composition from Domain presence.
- If released, precedence/failure/versioning must be deterministic and fail closed.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-POL-MIG-001 — policy schema version migration

### Traceability

```text
Requirements: WGREQ074, WGREQ174
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **policy schema version migration**.
- Do not infer runtime WorkspacePolicy composition from Domain presence.
- If released, precedence/failure/versioning must be deterministic and fail closed.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-POL-SEC-001 — malformed policy fails closed

### Traceability

```text
Requirements: WGREQ075, WGREQ151
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **malformed policy fails closed**.
- Do not infer runtime WorkspacePolicy composition from Domain presence.
- If released, precedence/failure/versioning must be deterministic and fail closed.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-RPERM-ARCH-001 — ResourcePermission meaning fixed

### Traceability

```text
Requirements: WGREQ076
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **ResourcePermission meaning fixed**.
- Verify exact Account/Workspace/resource/subject scope and management authority ceiling.
- Inheritance is claimed only when a concrete runtime producer/evaluator/invalidation path is proven.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-RPERM-APP-001 — direct grant scope exact

### Traceability

```text
Requirements: WGREQ077
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **direct grant scope exact**.
- Verify exact Account/Workspace/resource/subject scope and management authority ceiling.
- Inheritance is claimed only when a concrete runtime producer/evaluator/invalidation path is proven.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-RPERM-APP-002 — inheritance behavior only if supported

### Traceability

```text
Requirements: WGREQ078
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **inheritance behavior only if supported**.
- Verify exact Account/Workspace/resource/subject scope and management authority ceiling.
- Inheritance is claimed only when a concrete runtime producer/evaluator/invalidation path is proven.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-AUTHZ-APP-002 — valid authentication still requires authorization

### Traceability

```text
Requirements: WGREQ081
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **valid authentication still requires authorization**.
- Execute through `AccessControlBehavior → IAccessFactsProvider → AccessFacts → IAccessPolicyEvaluator → AccessPolicyEngine`.
- A denied/misconfigured request must not reach the protected side effect.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-AUTHZ-APP-003 — active membership prerequisite

### Traceability

```text
Requirements: WGREQ082
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **active membership prerequisite**.
- Execute through `AccessControlBehavior → IAccessFactsProvider → AccessFacts → IAccessPolicyEvaluator → AccessPolicyEngine`.
- A denied/misconfigured request must not reach the protected side effect.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-AUTHZ-APP-004 — supported permission sources compose deterministically

### Traceability

```text
Requirements: WGREQ083, WGREQ084
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **supported permission sources compose deterministically**.
- Execute through `AccessControlBehavior → IAccessFactsProvider → AccessFacts → IAccessPolicyEvaluator → AccessPolicyEngine`.
- A denied/misconfigured request must not reach the protected side effect.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-AUTHZ-API-001 — not-found privacy mapping

### Traceability

```text
Requirements: WGREQ086, WGREQ136
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **not-found privacy mapping**.
- Execute through `AccessControlBehavior → IAccessFactsProvider → AccessFacts → IAccessPolicyEvaluator → AccessPolicyEngine`.
- A denied/misconfigured request must not reach the protected side effect.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-PIPE-APP-001 — protected request declares resource/action requirement

### Traceability

```text
Requirements: WGREQ087
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **protected request declares resource/action requirement**.
- Verify the production behavior order and canonical request security declaration.
- The API/handler must not become a parallel authorization engine.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-PIPE-INT-001 — authorization runs before protected side effect

### Traceability

```text
Requirements: WGREQ088, WGREQ182
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **authorization runs before protected side effect**.
- Verify the production behavior order and canonical request security declaration.
- The API/handler must not become a parallel authorization engine.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-PIPE-ARCH-002 — API endpoint not sole business auth owner

### Traceability

```text
Requirements: WGREQ090
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **API endpoint not sole business auth owner**.
- Verify the production behavior order and canonical request security declaration.
- The API/handler must not become a parallel authorization engine.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-PIPE-SEC-001 — background/System execution follows explicit trusted contract

### Traceability

```text
Requirements: WGREQ091, WGREQ126, WGREQ185
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **background/System execution follows explicit trusted contract**.
- Verify the production behavior order and canonical request security declaration.
- The API/handler must not become a parallel authorization engine.
- Preserve `AccessPolicyEngineCharacterizationTests.SystemInternalRequest_WithoutUser_BypassesAuth_CallsHandler` as the approved internal exception.
- Prove an ordinary external/user/background request cannot become `ApplicationPrincipalKind.System` merely to bypass permission evaluation.
- Verify the concrete internal flow obtains trusted Account/Workspace scope and still obeys applicable data-session/RLS/runtime constraints.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-PIPE-APP-002 — pipeline order compatibility

### Traceability

```text
Requirements: WGREQ092, WGREQ185
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **pipeline order compatibility**.
- Verify the production behavior order and canonical request security declaration.
- The API/handler must not become a parallel authorization engine.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-HANDSHAKE-ARCH-001 — downstream owns resource semantics

### Traceability

```text
Requirements: WGREQ093, WGREQ094
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **downstream owns resource semantics**.
- Preserve Facts → Policy → Enforcement ownership.
- Downstream contexts own resource semantics; Governance owns permission policy; Application owns enforcement.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-HANDSHAKE-ARCH-002 — downstream does not encode role names

### Traceability

```text
Requirements: WGREQ095
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **downstream does not encode role names**.
- Preserve Facts → Policy → Enforcement ownership.
- Downstream contexts own resource semantics; Governance owns permission policy; Application owns enforcement.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-HANDSHAKE-INT-001 — Governance uses approved resource lookup contract

### Traceability

```text
Requirements: WGREQ096, WGREQ133, WGREQ186
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **Governance uses approved resource lookup contract**.
- Preserve Facts → Policy → Enforcement ownership.
- Downstream contexts own resource semantics; Governance owns permission policy; Application owns enforcement.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-SHARE-INT-001 — bounded resource access only

### Traceability

```text
Requirements: WGREQ099, WGREQ100
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **bounded resource access only**.
- Treat a ShareLink as a bounded capability, never as implicit Workspace membership/global authority.
- Secret, expiry, disable/revoke and resource privacy behavior must be proven if the consumption path is released.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-SHARE-INT-002 — expiry invalidates cached access

### Traceability

```text
Requirements: WGREQ101, WGREQ153
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **expiry invalidates cached access**.
- Treat a ShareLink as a bounded capability, never as implicit Workspace membership/global authority.
- Secret, expiry, disable/revoke and resource privacy behavior must be proven if the consumption path is released.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-SHARE-CONC-001 — revoke/use race

### Traceability

```text
Requirements: WGREQ102, WGREQ149
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **revoke/use race**.
- Treat a ShareLink as a bounded capability, never as implicit Workspace membership/global authority.
- Secret, expiry, disable/revoke and resource privacy behavior must be proven if the consumption path is released.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-SHARE-SEC-002 — guessing public identifier does not expose privileged resource

### Traceability

```text
Requirements: WGREQ103
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **guessing public identifier does not expose privileged resource**.
- Treat a ShareLink as a bounded capability, never as implicit Workspace membership/global authority.
- Secret, expiry, disable/revoke and resource privacy behavior must be proven if the consumption path is released.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-SHARE-AUD-001 — share access audit identity

### Traceability

```text
Requirements: WGREQ104
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **share access audit identity**.
- Treat a ShareLink as a bounded capability, never as implicit Workspace membership/global authority.
- Secret, expiry, disable/revoke and resource privacy behavior must be proven if the consumption path is released.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-AUD-INT-001 — historical audit record not mutated by later product edits

### Traceability

```text
Requirements: WGREQ106
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **historical audit record not mutated by later product edits**.
- Distinguish durable audit/security evidence from ordinary operational logging.
- Tenant/query authorization and secret minimization are mandatory.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-AUD-INT-002 — critical governance change records actor/resource/action

### Traceability

```text
Requirements: WGREQ107
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **critical governance change records actor/resource/action**.
- Distinguish durable audit/security evidence from ordinary operational logging.
- Tenant/query authorization and secret minimization are mandatory.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-SEVT-DOM-001 — SecurityEvent represents governance fact

### Traceability

```text
Requirements: WGREQ108
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **SecurityEvent represents governance fact**.
- Classify the actual current security-event mechanism before claiming a dedicated product surface.
- Do not create a duplicate source of truth solely for documentation symmetry.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-AUD-SEC-001 — no bearer/authentication secrets in audit/security events

### Traceability

```text
Requirements: WGREQ109, WGREQ164
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **no bearer/authentication secrets in audit/security events**.
- Distinguish durable audit/security evidence from ordinary operational logging.
- Tenant/query authorization and secret minimization are mandatory.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-AUD-INT-003 — audit query tenant isolation

### Traceability

```text
Requirements: WGREQ110, WGREQ157
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **audit query tenant isolation**.
- Distinguish durable audit/security evidence from ordinary operational logging.
- Tenant/query authorization and secret minimization are mandatory.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-TPL-APP-001 — template apply validates current permission/action catalog

### Traceability

```text
Requirements: WGREQ112
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **template apply validates current permission/action catalog**.
- Domain template semantics do not prove an Application/API apply workflow.
- If apply is released, validate current resource/action compatibility, atomicity and audit.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-TPL-MIG-001 — stored template version compatibility

### Traceability

```text
Requirements: WGREQ113
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **stored template version compatibility**.
- Domain template semantics do not prove an Application/API apply workflow.
- If apply is released, validate current resource/action compatibility, atomicity and audit.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-UP-ARCH-001 — no Identity credential dependency

### Traceability

```text
Requirements: WGREQ114
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **no Identity credential dependency**.
- Consume Identity/Accounts through the approved upstream contract/context.
- Do not duplicate credential, Actor, Account or lifecycle ownership.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-UP-INT-001 — canonical Account ID only

### Traceability

```text
Requirements: WGREQ115
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **canonical Account ID only**.
- Consume Identity/Accounts through the approved upstream contract/context.
- Do not duplicate credential, Actor, Account or lifecycle ownership.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-UP-INT-002 — disabled Account blocks protected Workspace operations

### Traceability

```text
Requirements: WGREQ116
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **disabled Account blocks protected Workspace operations**.
- Consume Identity/Accounts through the approved upstream contract/context.
- Do not duplicate credential, Actor, Account or lifecycle ownership.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-UP-INT-003 — disabled User cannot retain access via stale membership cache

### Traceability

```text
Requirements: WGREQ117, WGREQ153
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **disabled User cannot retain access via stale membership cache**.
- Consume Identity/Accounts through the approved upstream contract/context.
- Do not duplicate credential, Actor, Account or lifecycle ownership.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-WM-CONTRACT-001 — Board resource-category registration

### Traceability

```text
Requirements: WGREQ118, WGREQ179
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **Board resource-category registration**.
- Use the representative Board contract through the canonical access-control pipeline.
- WorkManagement remains Board semantic owner; protected allow/deny/cross-tenant behavior is proven through production DI.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-WM-CONTRACT-002 — BoardItem independent resource only if required

### Traceability

```text
Requirements: WGREQ119
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **BoardItem independent resource only if required**.
- Use the representative Board contract through the canonical access-control pipeline.
- WorkManagement remains Board semantic owner; protected allow/deny/cross-tenant behavior is proven through production DI.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-WM-P2-001 — allowed Board action

### Traceability

```text
Requirements: WGREQ120, WGREQ182, WGREQ190
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **allowed Board action**.
- Use the representative Board contract through the canonical access-control pipeline.
- WorkManagement remains Board semantic owner; protected allow/deny/cross-tenant behavior is proven through production DI.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-WM-P2-002 — denied Board action

### Traceability

```text
Requirements: WGREQ120, WGREQ182, WGREQ190
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **denied Board action**.
- Use the representative Board contract through the canonical access-control pipeline.
- WorkManagement remains Board semantic owner; protected allow/deny/cross-tenant behavior is proven through production DI.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-WM-P2-003 — cross-tenant Board denial

### Traceability

```text
Requirements: WGREQ120, WGREQ152, WGREQ190
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **cross-tenant Board denial**.
- Use the representative Board contract through the canonical access-control pipeline.
- WorkManagement remains Board semantic owner; protected allow/deny/cross-tenant behavior is proven through production DI.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-WM-P2-004 — WorkManagement Domain invariant remains local

### Traceability

```text
Requirements: WGREQ121, WGREQ186
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **WorkManagement Domain invariant remains local**.
- Use the representative Board contract through the canonical access-control pipeline.
- WorkManagement remains Board semantic owner; protected allow/deny/cross-tenant behavior is proven through production DI.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-COL-X-001 — Comment authorization uses target access contract

### Traceability

```text
Requirements: WGREQ123
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **Comment authorization uses target access contract**.
- Authorize collaboration against the target resource contract rather than a local shadow RBAC model.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-BILL-X-002 — entitlement and authorization remain separate

### Traceability

```text
Requirements: WGREQ125, WGREQ186
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **entitlement and authorization remain separate**.
- Keep commercial entitlement/subscription decisions Billing-owned and permission decisions Governance-owned.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-AUTO-X-001 — background automation actor follows approved System/internal semantics

### Traceability

```text
Requirements: WGREQ126, WGREQ185
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **background automation actor follows approved System/internal semantics**.
- Ordinary background execution is not implicitly privileged.
- An explicit `ISystemInternalRequest` may use the frozen System contract only with trusted scope and approved runtime safeguards.
- Exercise at least one Automation/background flow and classify whether it is ordinary scoped work or an explicit `ISystemInternalRequest`.
- There is no fake global-admin User; approved System execution is represented by the explicit request contract.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-ANA-X-001 — Analytics uses approved governance facts only

### Traceability

```text
Requirements: WGREQ128
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **Analytics uses approved governance facts only**.
- Analytics consumes approved events/facts/projections and never becomes an authorization source of truth or treats private Governance tables as its business contract.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-OWN-ARCH-003 — one Workspace membership truth

### Traceability

```text
Requirements: WGREQ131
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **one Workspace membership truth**.
- Preserve one semantic owner and one canonical truth for Workspace membership/roles/governance state.
- Owner-local DbContext abstractions are not cross-context Public contracts.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-OWN-ARCH-004 — one Role truth

### Traceability

```text
Requirements: WGREQ132
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **one Role truth**.
- Preserve one semantic owner and one canonical truth for Workspace membership/roles/governance state.
- Owner-local DbContext abstractions are not cross-context Public contracts.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-OWN-ARCH-005 — no private cross-context DB dependency as public contract

### Traceability

```text
Requirements: WGREQ133, WGREQ184, WGREQ186
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **no private cross-context DB dependency as public contract**.
- Preserve one semantic owner and one canonical truth for Workspace membership/roles/governance state.
- Owner-local DbContext abstractions are not cross-context Public contracts.
- Reject foreign private DbContext/repository use as a public integration contract.
- Classify existing `AccessFactsQuery` shared SQL separately as brownfield supporting composite read; do not silently expand it.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-API-SEC-001 — invitation/share secret response minimization

### Traceability

```text
Requirements: WGREQ137
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **invitation/share secret response minimization**.
- Verify transport contract, authentication classification, safe error mapping and secret minimization.
- Business authorization remains Application-owned.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-EVT-GOV-001 — Governance event production

### Traceability

```text
Requirements: WGREQ140
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **Governance event production**.
- Verify producer ownership, stable scope IDs, safe payload/versioning and transactional outbox behavior where applicable.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-EVT-INT-001 — event carries stable Account/Workspace/resource scope

### Traceability

```text
Requirements: WGREQ141
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **event carries stable Account/Workspace/resource scope**.
- Verify producer ownership, stable scope IDs, safe payload/versioning and transactional outbox behavior where applicable.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-EVT-SEC-001 — event secret minimization

### Traceability

```text
Requirements: WGREQ142
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **event secret minimization**.
- Verify producer ownership, stable scope IDs, safe payload/versioning and transactional outbox behavior where applicable.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-EVT-CONTRACT-001 — event compatibility

### Traceability

```text
Requirements: WGREQ143
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **event compatibility**.
- Verify producer ownership, stable scope IDs, safe payload/versioning and transactional outbox behavior where applicable.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-CONC-ADMIN-001 — last-owner/admin race

### Traceability

```text
Requirements: WGREQ145
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **last-owner/admin race**.
- Use real database/application concurrency evidence where persistence races matter.
- A losing race cannot leave broader access, duplicate truth or invalid owner state.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-CONC-INV-001 — invite accept/revoke/resend race

### Traceability

```text
Requirements: WGREQ146
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **invite accept/revoke/resend race**.
- Use real database/application concurrency evidence where persistence races matter.
- A losing race cannot leave broader access, duplicate truth or invalid owner state.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-CONC-ROLE-001 — role assignment remove/add race

### Traceability

```text
Requirements: WGREQ147
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **role assignment remove/add race**.
- Use real database/application concurrency evidence where persistence races matter.
- A losing race cannot leave broader access, duplicate truth or invalid owner state.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-SEC-MASTER-005 — denied policy details privacy

### Traceability

```text
Requirements: WGREQ155
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **denied policy details privacy**.
- Exercise the negative security path through production-equivalent authorization and persistence isolation.
- No failure, stale fact or privacy mapping may widen access.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-SEC-MASTER-006 — share bearer secret

### Traceability

```text
Requirements: WGREQ156
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **share bearer secret**.
- Exercise the negative security path through production-equivalent authorization and persistence isolation.
- No failure, stale fact or privacy mapping may widen access.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-SEC-MASTER-007 — audit API authorization

### Traceability

```text
Requirements: WGREQ157
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **audit API authorization**.
- Exercise the negative security path through production-equivalent authorization and persistence isolation.
- No failure, stale fact or privacy mapping may widen access.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-REL-AUTHZ-001 — resource/facts/policy dependency unavailable

### Traceability

```text
Requirements: WGREQ159
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **resource/facts/policy dependency unavailable**.
- Failure/retry semantics must preserve canonical source truth and fail closed for authorization dependencies.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-REL-CACHE-001 — authorization cache/projection outage

### Traceability

```text
Requirements: WGREQ161
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **authorization cache/projection outage**.
- Failure/retry semantics must preserve canonical source truth and fail closed for authorization dependencies.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-OBS-INT-001 — authorization correlation

### Traceability

```text
Requirements: WGREQ162
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **authorization correlation**.
- Capture bounded correlation/decision/admin evidence without reusable secrets or unsafe high-cardinality metric labels.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-OBS-INT-002 — governance mutation traceability

### Traceability

```text
Requirements: WGREQ163
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **governance mutation traceability**.
- Capture bounded correlation/decision/admin evidence without reusable secrets or unsafe high-cardinality metric labels.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-OBS-SEC-001 — no secret telemetry

### Traceability

```text
Requirements: WGREQ164
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **no secret telemetry**.
- Capture bounded correlation/decision/admin evidence without reusable secrets or unsafe high-cardinality metric labels.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-OBS-METRIC-001 — denial/security metric integration

### Traceability

```text
Requirements: WGREQ165
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **denial/security metric integration**.
- Capture bounded correlation/decision/admin evidence without reusable secrets or unsafe high-cardinality metric labels.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-PERF-AUTHZ-001 — authorization hot-path baseline

### Traceability

```text
Requirements: WGREQ166
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **authorization hot-path baseline**.
- Measure/query the current hot path without weakening tenant/security filters.
- Use repository SLO/baseline authority rather than inventing an absolute threshold.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-PERF-MEM-001 — membership lookup indexing

### Traceability

```text
Requirements: WGREQ167
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **membership lookup indexing**.
- Measure/query the current hot path without weakening tenant/security filters.
- Use repository SLO/baseline authority rather than inventing an absolute threshold.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-PERF-PERM-001 — permission lookup bounded

### Traceability

```text
Requirements: WGREQ168
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **permission lookup bounded**.
- Measure/query the current hot path without weakening tenant/security filters.
- Use repository SLO/baseline authority rather than inventing an absolute threshold.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-PERF-CACHE-001 — cache key correctness

### Traceability

```text
Requirements: WGREQ169
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **cache key correctness**.
- Measure/query the current hot path without weakening tenant/security filters.
- Use repository SLO/baseline authority rather than inventing an absolute threshold.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-PERF-LIST-001 — pagination

### Traceability

```text
Requirements: WGREQ170
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **pagination**.
- Measure/query the current hot path without weakening tenant/security filters.
- Use repository SLO/baseline authority rather than inventing an absolute threshold.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-MIG-MEM-001 — membership schema migration

### Traceability

```text
Requirements: WGREQ171
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **membership schema migration**.
- Use clean-database and supported-upgrade evidence when the semantic/schema dimension changes.
- Persisted identifiers and security behavior remain compatible or are explicitly migrated.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-MIG-RES-001 — ResourceKind migration

### Traceability

```text
Requirements: WGREQ172
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **ResourceKind migration**.
- Use clean-database and supported-upgrade evidence when the semantic/schema dimension changes.
- Persisted identifiers and security behavior remain compatible or are explicitly migrated.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-MIG-ACT-001 — PermissionAction migration

### Traceability

```text
Requirements: WGREQ172
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **PermissionAction migration**.
- Use clean-database and supported-upgrade evidence when the semantic/schema dimension changes.
- Persisted identifiers and security behavior remain compatible or are explicitly migrated.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-MIG-ROLE-001 — built-in role semantic migration

### Traceability

```text
Requirements: WGREQ173
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **built-in role semantic migration**.
- Use clean-database and supported-upgrade evidence when the semantic/schema dimension changes.
- Persisted identifiers and security behavior remain compatible or are explicitly migrated.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-MIG-POL-001 — policy schema migration

### Traceability

```text
Requirements: WGREQ174
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **policy schema migration**.
- Use clean-database and supported-upgrade evidence when the semantic/schema dimension changes.
- Persisted identifiers and security behavior remain compatible or are explicitly migrated.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-MIG-LINK-001 — invitation/share token format migration

### Traceability

```text
Requirements: WGREQ175
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **invitation/share token format migration**.
- Use clean-database and supported-upgrade evidence when the semantic/schema dimension changes.
- Persisted identifiers and security behavior remain compatible or are explicitly migrated.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

## WG-TST-P2-CORE-007 — migration/startup

### Traceability

```text
Requirements: WGREQ171, WGREQ172, WGREQ173, WGREQ174, WGREQ175, WGREQ176, WGREQ177
Origin: preserved from develop Workspace/Governance TESTS baseline
Preparation state: NOT_EVALUATED
```

### Verification contract

- Preserve the canonical scenario intent: **migration/startup**.
- This is a certification packet, not a unit-test existence claim.
- Bind all evidence to the accepted candidate SHA and record gaps/skips explicitly.
- Record clean DB, supported upgrade applicability, pending-model state and RLS/bootstrap validity.

### Certification evidence

- Record the exact candidate SHA, executable test/gate, result, skip count and evidence artifact.
- Source/test presence alone is not PASS.

# Current-source synchronization scenarios

## WG-TST-SYNC-ARCH-001 — module-first Application topology

### Traceability

```text
Requirements: WGREQ183
Preparation state: NOT_EVALUATED
```

### Required assertions

- New/touched P2 Application use cases remain in `Features/Workspaces` or `Features/Governance`.
- No deprecated parallel feature root is reintroduced.

### Certification evidence

- Exact candidate SHA plus named architecture/integration/CI evidence.

## WG-TST-SYNC-PERSIST-001 — owner-local persistence abstractions

### Traceability

```text
Requirements: WGREQ184
Preparation state: NOT_EVALUATED
```

### Required assertions

- `IWorkspaceDbContext` and `IGovernanceDbContext` remain owner-local under the documented EF exception.
- No foreign bounded context uses either abstraction as its Public contract.

### Certification evidence

- Exact candidate SHA plus named architecture/integration/CI evidence.

## WG-TST-SYNC-AUTHZ-001 — canonical access-control runtime and System exception

### Traceability

```text
Requirements: WGREQ185, WGREQ091, WGREQ126
Preparation state: NOT_EVALUATED
```

### Required assertions

- Production enforcement is `AccessControlBehavior` through the neutral facts/policy seams.
- `AccessPolicyEngineCharacterizationTests` freezes the approved `ISystemInternalRequest` path.
- An ordinary request cannot opt into System authority.

### Certification evidence

- Exact candidate SHA plus named architecture/integration/CI evidence.

## WG-TST-SYNC-FACTS-001 — authorization facts ownership

### Traceability

```text
Requirements: WGREQ186
Preparation state: NOT_EVALUATED
```

### Required assertions

- Every `AccessFacts` field is mapped to its semantic owner and current source mechanism.
- Documents/Billing producer seams remain producer-owned.
- WorkManagement Board clauses in `AccessFactsQuery` are explicitly classified and not silently expanded.

### Certification evidence

- Exact candidate SHA plus named architecture/integration/CI evidence.

## WG-TST-SYNC-RLS-001 — RLS remains defense-in-depth

### Traceability

```text
Requirements: WGREQ187
Preparation state: NOT_EVALUATED
```

### Required assertions

- Application action authorization is independently tested.
- RLS missing-context/no-grant/cross-tenant/transaction-local behavior is tested under an enforcing database role.
- Membership suspend/remove cannot retain stale access-grant authority.

### Certification evidence

- Exact candidate SHA plus named architecture/integration/CI evidence.

## WG-TST-SYNC-LAYER-001 — capability layer-status honesty

### Traceability

```text
Requirements: WGREQ188
Preparation state: NOT_EVALUATED
```

### Required assertions

- For CustomRole, WorkspacePolicy, ShareLink, PermissionTemplate and other secondary surfaces, Domain presence is reported separately from Application/API/runtime delivery.
- Certification never upgrades layer status from source presence alone.

### Certification evidence

- Exact candidate SHA plus named architecture/integration/CI evidence.

## WG-TST-SYNC-EVIDENCE-001 — exact-SHA evidence discipline

### Traceability

```text
Requirements: WGREQ189
Preparation state: NOT_EVALUATED
```

### Required assertions

- Every final PASS/VERIFIED/STABLE claim records the accepted candidate SHA.
- Skipped backend jobs, zero-test execution and historical green runs cannot silently satisfy a gate.

### Certification evidence

- Exact candidate SHA plus named architecture/integration/CI evidence.

## WG-TST-SYNC-HANDOFF-001 — staged P2 to P3 handoff

### Traceability

```text
Requirements: WGREQ190
Preparation state: NOT_EVALUATED
```

### Required assertions

- P2 Milestone A publishes only the stable producer contract required by P3-A Domain/Data work.
- P2 Milestone B additionally proves protected Application/API authorization, RLS and representative Board handshake.
- The handoff contains D5 invalidation rules and explicit non-blocking debt.

### Certification evidence

- Exact candidate SHA plus named architecture/integration/CI evidence.

# Acceptance-criteria traceability

| Acceptance criterion | Required scenario evidence |
|---|---|
| WGAC001 — Workspace | `WG-TST-WSP-*` |
| WGAC002 — Membership | `WG-TST-MEM-*`, `WG-TST-CONC-MEM-001`, `WG-TST-CONC-ADMIN-001`, `WG-TST-SYNC-RLS-001` |
| WGAC003 — Invitation | `WG-TST-INV-*`, `WG-TST-CONC-INV-001` |
| WGAC004 — Resource/action handshake | `WG-TST-RES-*`, `WG-TST-ACT-*`, `WG-TST-WM-CONTRACT-*`, `WG-TST-SYNC-FACTS-001` |
| WGAC005 — Permission/effective authorization | `WG-TST-PERM-*`, `WG-TST-PRULE-*`, `WG-TST-AUTHZ-*`, `WG-TST-PIPE-*` |
| WGAC006 — Built-in roles | `WG-TST-ROLE-*`, `WG-TST-P2-CORE-004` |
| WGAC007 — Resource permissions | `WG-TST-RPERM-*`, `WG-TST-ROLE-SEC-001` |
| WGAC008 — Share links | `WG-TST-SHARE-*` when release-scoped; otherwise explicit NOT_APPLICABLE |
| WGAC009 — Cross-context ownership | `WG-TST-HANDSHAKE-*`, `WG-TST-OWN-*`, `WG-TST-SYNC-PERSIST-001`, `WG-TST-SYNC-FACTS-001` |
| WGAC010 — P3 handoff | `WG-TST-WM-P2-*`, `WG-TST-P2-CORE-006`, `WG-TST-SYNC-HANDOFF-001` |
| WGAC011 — Architecture | `WG-TST-PIPE-ARCH-*`, `WG-TST-OWN-ARCH-*`, `WG-TST-SYNC-ARCH-001`, `WG-TST-SYNC-PERSIST-001` |
| WGAC012 — Security | `WG-TST-SEC-MASTER-*`, `WG-TST-RLS-SEC-*`, `WG-TST-PIPE-SEC-001` |
| WGAC013 — Data ownership | `WG-TST-OWN-*`, `WG-TST-RES-ARCH-001A/B/C` |
| WGAC014 — Migration | `WG-TST-MIG-*`, `WG-TST-P2-CORE-007` |
| WGAC015 — Concurrency | `WG-TST-CONC-*`, membership/invitation/resource-permission race scenarios |
| WGAC016 — Observability | `WG-TST-OBS-*`, `WG-TST-AUD-*` |
| WGAC017 — Performance | `WG-TST-PERF-*` |
| WGAC018 — CI | `WG-TST-SYNC-EVIDENCE-001` plus certification CI gate mapping |

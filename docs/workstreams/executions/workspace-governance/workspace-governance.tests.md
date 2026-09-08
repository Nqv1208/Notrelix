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
  - workspace-rules
  - resource-kind
  - actions
  - permissions
  - permission-rules
  - roles
  - policies
  - resource-permissions
  - authorization
  - share-links
  - audit
  - security-events
  - templates
  - tenant-isolation
  - workmanagement-handoff
  - migrations
  - ci
evidence:
  - docs/workstreams/execution/workspace-governance/workspace-governance.spec.md
  - docs/workstreams/execution/workspace-governance/workspace-governance.plan.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/teams/workspace-governance.md
  - docs/workstreams/teams/identity-accounts.md
  - docs/workstreams/teams/platform-foundation.md
  - backend/docs/architecture/domain-modeling.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/api-and-contracts.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/testing-and-quality-gates.md
review_on:
  - workspace-governance-spec-change
  - workspace-governance-plan-change
  - p1-contract-change
  - membership-model-change
  - resource-action-change
  - permission-model-change
  - role-policy-change
  - authorization-pipeline-change
  - share-link-change
  - migration-change
  - p2-gate-change
  - ci-gate-change
---

# TESTS — Workspace & Governance

## 1. Purpose

This document is the canonical verification plan for Workspace & Governance.

It defines how the P2 execution package proves:

```text
Workspace containment
membership
resource/action semantics
permission semantics
role/policy behavior
central authorization
tenant isolation
privilege-escalation resistance
WorkManagement handoff
migration compatibility
```

It is not an implementation plan.

Implementation belongs to:

```text
workspace-governance.plan.md
```

Target semantics belong to:

```text
workspace-governance.spec.md
```

Certification evidence belongs to:

```text
workspace-governance.certification.md
```

## 2. Verification principle

The required traceability chain is:

```text
WGREQ requirement
        ↓
PLAN work unit
        ↓
WG-TST scenario
        ↓
test project/suite
        ↓
CI gate
        ↓
P2/FULL certification
```

A P2 capability is incomplete if a material invariant has no executable verification strategy.

This revision additionally treats cross-context ownership as an executable invariant:

```text
Facts       → resource owner
Policy      → Governance
Enforcement → Application pipeline
```

Tests MUST prove both authorization behavior and absence of forbidden private-context coupling.

## 3. Test authority rule

Tests verify accepted semantics.

Tests MUST NOT silently invent:

- Account→Workspace relation;
- membership ownership;
- explicit deny;
- inheritance;
- custom-role behavior;
- Policy precedence;
- ResourcePermission meaning;
- ShareLink semantics.

Where the SPEC/PLAN says semantics must first be classified from source, tests are written only after that decision is resolved.

## 4. Expected backend test topology

Use existing canonical backend test projects.

Expected families:

```text
Architecture
Domain
Application
Infrastructure
Platform
API
Integration
Testing support
```

Do not create a separate Governance test project merely for symmetry unless current test architecture cannot express required coverage and an architecture decision approves it.

## 5. Test-layer model

### T0 — Static / compile / source guards

For:

- forbidden dependencies;
- generated contract drift;
- analyzers;
- architecture/source scanners.

### T1 — Domain tests

For:

- Workspace;
- Member;
- Invitation;
- Team;
- Space;
- Role;
- Permission;
- Policy;
- ResourcePermission;
- ShareLink;
- Domain events.

### T2 — Application tests

For:

- commands/queries;
- validation;
- provisioning;
- authorization declarations;
- effective decision orchestration;
- failure mapping.

### T3 — Infrastructure tests

For:

- EF mapping;
- unique constraints;
- indexes;
- policy/resource-permission storage;
- invitation/share-token verifier storage;
- cache adapters;
- migrations.

### T4 — API tests

For:

- routes/contracts;
- auth/authz;
- forbidden/not-found;
- invitation/share-link boundary;
- OpenAPI.

### T5 — Architecture tests

For:

- Domain purity;
- Workspaces/Governance separation;
- pipeline-owned authorization;
- private persistence boundaries;
- project/layer dependency.

### T6 — Integration tests

For:

- P1 Actor/Account → Workspace;
- tenant isolation;
- real authorization behavior;
- production DI;
- DB behavior;
- WorkManagement handshake.

### T7 — Security tests

For:

- privilege escalation;
- cross-tenant spoofing;
- stale authorization cache;
- invitation/share bearer-secret abuse;
- fail-closed behavior;
- background bypass.

### T8 — Concurrency tests

For:

- duplicate membership;
- last-admin invariant;
- invitation accept/revoke race;
- role assignment races;
- policy stale writes;
- share-link revoke/use race.

### T9 — Migration tests

For:

- membership schema;
- resource/action IDs;
- role/permission semantics;
- policy schema;
- link token format;
- clean/upgrade DB.

### T10 — Performance / reliability

For:

- authorization hot path;
- membership lookup;
- permission lookup;
- cache failure;
- evaluator/resource dependency failure;
- provisioning partial failure.

### T11 — Cross-context contract tests

For:

- Identity/Accounts;
- WorkManagement;
- Documents;
- Collaboration;
- Billing;
- Automation;
- Analytics.

## 6. Test ID convention

Use:

```text
WG-TST-<AREA>-<LAYER>-<NNN>
```

Examples:

```text
WG-TST-WSP-DOM-001
WG-TST-MEM-CONC-002
WG-TST-RES-ARCH-001
WG-TST-AUTHZ-INT-006
WG-TST-SHARE-SEC-003
WG-TST-MIG-PERM-002
```

These are execution IDs, not governance rule IDs.

## 7. Scenario structure

Each critical test should clearly identify:

```text
Given
When
Then
WGREQ IDs
Expected result
```

Prefer business-language names over implementation-language names.

# Master traceability map

## 8. Requirement family mapping

| Requirement range | Capability | Primary layers |
|---|---|---|
| WGREQ001–WGREQ011 | Workspace | Domain/Application/Infrastructure/Integration |
| WGREQ012–WGREQ023 | Membership | Domain/Application/Integration/Security/Concurrency |
| WGREQ024–WGREQ033 | Invitations | Domain/Application/API/Security/Concurrency |
| WGREQ034–WGREQ043 | Teams/Spaces/Rules | Domain/Application/Architecture |
| WGREQ044–WGREQ062 | Resource/Action/Permission/Rules | Domain/Application/Architecture/Integration |
| WGREQ063–WGREQ086 | Roles/Policies/ResourcePermissions/Auth decision | Domain/Application/Security |
| WGREQ087–WGREQ096 | Pipeline/resource handshake | Application/Architecture/Integration |
| WGREQ097–WGREQ113 | ShareLinks/Audit/Templates | Domain/API/Security/Integration |
| WGREQ114–WGREQ143 | Cross-context/API/Events | Architecture/API/Integration |
| WGREQ144–WGREQ170 | Concurrency/Security/Reliability/Perf | Security/Concurrency/Integration/Perf |
| WGREQ171–WGREQ177 | Migration | Migration/Infrastructure |
| WGREQ178–WGREQ182 | P2 gate | Cross-layer/Certification |

# Workspace tests

## 9. WG-TST-WSP-DOM-001 — stable Workspace identity

Requirements:

```text
WGREQ001
WGREQ002
```

Given a Workspace
When mutable metadata changes
Then canonical Workspace ID remains stable.

## 10. WG-TST-WSP-DOM-002 — Account containment invariant

Requirements:

```text
WGREQ003
```

Given Account A
When Workspace is created under A
Then persisted Workspace references canonical Account A.

Negative:

```text
invalid/nonexistent Account
→ rejected according to current cross-context contract
```

## 11. WG-TST-WSP-INT-001 — Account A Workspace not visible as Account B

Requirements:

```text
WGREQ003
WGREQ004
WGREQ152
```

Given Actor operating in Account A
When requesting Workspace under Account B
Then access is denied/not-found according to API privacy policy.

This is a mandatory P2 tenant-isolation test.

## 12. WG-TST-WSP-DOM-003 — valid Workspace lifecycle

Requirements:

```text
WGREQ005
```

Cover every canonical lifecycle transition actually supported.

## 13. WG-TST-WSP-DOM-004 — invalid Workspace lifecycle rejected

Requirements:

```text
WGREQ005
```

No persistence side effect.

## 14. WG-TST-WSP-X-001 — Workspace archive/delete effects explicit

Requirements:

```text
WGREQ006
```

Representative downstream fixture verifies lifecycle does not rely solely on accidental database cascade.

## 15. WG-TST-WSP-APP-001 — generic Workspace update cannot modify Governance state accidentally

Requirements:

```text
WGREQ007
```

Attempt to change role/permission policy through Workspace metadata update.

Expected:

```text
rejected / impossible by contract
```

## 16. WG-TST-WSP-APP-002 — Workspace provisioning ownership

Requirements:

```text
WGREQ008
```

If provisioning creates multiple objects, verify each canonical owner executes through approved boundary.

## 17. WG-TST-WSP-INT-002 — provisioning retry idempotency

Requirements:

```text
WGREQ009
```

Given same idempotent provisioning operation retried
Then no duplicate:

- Workspace;
- bootstrap Member;
- role assignment;
- Team/Space if included.

## 18. WG-TST-WSP-ARCH-001 — settings ownership

Requirements:

```text
WGREQ010
```

Source/architecture test or review guard ensures Workspace settings do not import Governance/Identity private implementation.

## 19. WG-TST-WSP-APP-003 — WorkspaceHome classification

Requirements:

```text
WGREQ011
```

Only once Phase 0 classification is complete.

If WorkspaceHome is composition/read model, tests must verify it does not become authoritative mutable business state accidentally.

# Membership tests

## 20. WG-TST-MEM-DOM-001 — WorkspaceMember references stable upstream identity

Requirements:

```text
WGREQ012
WGREQ013
```

Verify canonical User/Actor reference and Workspace reference.

## 21. WG-TST-MEM-INF-001 — duplicate active membership blocked

Requirements:

```text
WGREQ014
WGREQ144
```

Use real DB constraint/transaction path.

Two concurrent add/accept operations for same subject+Workspace must not create duplicate active membership.

## 22. WG-TST-MEM-DOM-002 — membership lifecycle transitions

Requirements:

```text
WGREQ015
```

Cover supported states only.

## 23. WG-TST-MEM-APP-001 — add member requires authorization

Requirements:

```text
WGREQ016
```

Given Actor without manage-members permission
When add-member command executes
Then authorization pipeline denies before state mutation.

## 24. WG-TST-MEM-APP-002 — remove member requires authorization

Requirements:

```text
WGREQ017
```

## 25. WG-TST-MEM-APP-003 — self leave allowed/denied according to canonical policy

Requirements:

```text
WGREQ018
```

Do not invent owner behavior.

## 26. WG-TST-MEM-CONC-001 — last-admin concurrent removal

Requirements:

```text
WGREQ019
WGREQ145
```

Only if last-admin invariant exists.

Given two admins
When concurrent operations remove/demote both
Then invariant prevents invalid no-admin final state.

## 27. WG-TST-MEM-APP-004 — role association delegates to Governance

Requirements:

```text
WGREQ020
```

WorkspaceMember does not carry an independent permission list that bypasses Governance.

## 28. WG-TST-MEM-INT-001 — suspended/inactive member loses access

Requirements:

```text
WGREQ021
WGREQ153
```

Must exercise actual cache/authorization path if cached.

## 29. WG-TST-MEM-INT-002 — disabled Identity User loses effective access

Requirements:

```text
WGREQ022
WGREQ117
```

Given membership still exists historically
When upstream User becomes invalid
Then protected operation is not authorized.

## 30. WG-TST-MEM-X-001 — removed member historical attribution preserved

Requirements:

```text
WGREQ023
```

Representative downstream resource/audit reference remains valid.

# Invitation tests

## 31. WG-TST-INV-DOM-001 — Invitation distinct from active Membership

Requirements:

```text
WGREQ024
```

Creating invitation must not create active membership.

## 32. WG-TST-INV-SEC-001 — invitation identity/secret boundary

Requirements:

```text
WGREQ025
WGREQ033
```

Ordinary invitation ID/list response must not expose reusable privileged secret.

## 33. WG-TST-INV-APP-001 — valid invite target

Requirements:

```text
WGREQ026
```

Cover supported target types only.

## 34. WG-TST-INV-APP-002 — inviter cannot grant stronger access than authorized

Requirements:

```text
WGREQ027
WGREQ154
```

Critical privilege-escalation test.

## 35. WG-TST-INV-APP-003 — expired invitation rejected

Requirements:

```text
WGREQ028
```

Use controllable clock if available.

## 36. WG-TST-INV-APP-004 — revoked invitation rejected

Requirements:

```text
WGREQ029
```

## 37. WG-TST-INV-INT-001 — repeated acceptance idempotent

Requirements:

```text
WGREQ030
```

No duplicate member, role assignment or privilege grant.

## 37A. WG-TST-INV-INT-002 — membership event reuse across membership sources

Requirements:

```text
WGREQ030
```

The Workspaces-owned membership fact is a single event family
`workspace.member.added` v1, staged through the shared
`WorkspaceMember.Create` factory regardless of membership source:

```text
CreateWorkspace (owner member via WorkspaceFactory.CreateWithOwner)
AcceptInvitation (invited member via WorkspaceMember.Create)
```

Reuse is proven by the production-graph outbox evidence:

```text
- WorkspaceCreatedOutboxEvidenceTests: owner-member creation stages
  exactly one workspace.created and one workspace.member.added;
  rollback leaves zero of both.
- AcceptInvitationTransactionEvidenceTests: a no-op, already-member
  acceptance completes the invitation without staging a second
  workspace.member.added.
```

The invariant: a membership fact is emitted once per distinct membership
creation; an acceptance that creates no new membership emits no new fact.

## 38. WG-TST-INV-CONC-001 — accept vs revoke race

Requirements:

```text
WGREQ031
WGREQ146
```

Authoritative revoke must not result in granted access if revoke wins according to model.

## 39. WG-TST-INV-API-001 — invitation lookup minimizes information

Requirements:

```text
WGREQ032
```

Unauthenticated/public flow returns only product-approved fields.

## 40. WG-TST-INV-SEC-002 — invitation secret absent from logs

Requirements:

```text
WGREQ033
WGREQ164
```

Use sentinel secret + captured logs.

# Team tests

## 41. WG-TST-TEAM-DOM-001 — Team belongs to Workspace

Requirements:

```text
WGREQ034
```

## 42. WG-TST-TEAM-APP-001 — Team member must satisfy Workspace membership rule

Requirements:

```text
WGREQ035
WGREQ154
```

User outside Workspace cannot gain effective access only by malformed Team membership.

## 43. WG-TST-TEAM-DOM-002 — Team lifecycle downstream references

Requirements:

```text
WGREQ036
```

Delete/archive behavior for role/resource bindings must be explicit.

## 44. WG-TST-TEAM-AUTHZ-001 — Team as Governance subject only when supported

Requirements:

```text
WGREQ037
```

Do not create permission subject semantics if source/product does not support it.

# Space tests

## 45. WG-TST-SPACE-DOM-001 — canonical Space meaning/containment

Requirements:

```text
WGREQ038
```

After source classification, test only approved semantics.

## 46. WG-TST-SPACE-DOM-002 — Space lifecycle

Requirements:

```text
WGREQ039
```

## 47. WG-TST-SPACE-AUTHZ-001 — Space resource handshake

Requirements:

```text
WGREQ040
```

Only if Space is independently governable.

## 48. WG-TST-SPACE-ARCH-001 — visibility does not become second authorization engine

Requirements:

```text
WGREQ041
```

# Workspace Rules tests

## 49. WG-TST-RULE-ARCH-001 — Workspaces Rules classification

Requirements:

```text
WGREQ042
```

Every current rule type must be classified in execution evidence.

This is an architecture/source completeness guard.

## 50. WG-TST-RULE-ARCH-002 — no rule-engine duplication

Requirements:

```text
WGREQ043
```

After classification, architecture/source tests should prevent Workspaces rule code from depending on Automation/Governance internals in an ownership-violating way.

# Resource model tests

## 51. WG-TST-RES-ARCH-001 — business resource owned by resource context

Requirements:

```text
WGREQ044
```

Governance Domain/Application must not reference private WorkManagement/Documents entity implementation merely to own their lifecycle.

### WG-TST-RES-ARCH-001A — resource facts adapter ownership

For every onboarded resource context, architecture/source guards verify that an implementation which reads
that context's private persistence is owned by that resource context.

Mandatory Board negative guard:

```text
Notrelix.Infrastructure.Governance.*
  MUST NOT reference IWorkManagementDbContext / Board private persistence
```

A Board authorization facts resolver may implement a neutral SPI, but the concrete data-access adapter must
be owned by WorkManagement.

### WG-TST-RES-ARCH-001B — facts provider is transport-neutral

Canonical Application/Domain authorization contracts must not reference gRPC/HTTP/message transport types.
Infrastructure adapters may use them only when a real extraction/integration requires them.

### WG-TST-RES-ARCH-001C — facts do not encode a second policy engine

Source/review guard and representative tests ensure resource projections expose resource-owned facts rather
than effective Governance decisions. Any mapping such as private Board role → generic access fact must have
an approved semantic mapping and must not independently grant Governance actions.

## 52. WG-TST-RES-DOM-001 — resource authorization category stability

Requirements:

```text
WGREQ045
```

If the canonical source calls the category `ResourceType`, no test may require a rename to `ResourceKind`.
If any persisted/configured category identifier changes, prove semantic equivalence and migration of roles,
permissions, policies and consumers. A display/type refactor must not silently change authorization identity.

## 53. WG-TST-RES-ARCH-002 — no CLR-name authorization identity

Requirements:

```text
WGREQ045
```

Where architecture forbids it, ensure resource identity is not assembly-qualified runtime type name.

## 54. WG-TST-RES-APP-001 — resource ID treated opaquely

Requirements:

```text
WGREQ046
```

Governance should use approved lookup/context contract, not parse foreign resource internals.

## 55. WG-TST-RES-INT-001 — Account/Workspace/resource scope consistency

Requirements:

```text
WGREQ047
WGREQ152
```

Cases:

```text
Account A + Workspace A + Resource A → valid candidate
Account A + Workspace B(Account B) + Resource B → reject
Account A + Workspace A + Resource B(Account B) → reject
```

## 56. WG-TST-RES-ARCH-003 — resource registration explicit

Requirements:

```text
WGREQ048
```

Every downstream Governable resource used in tests must register through approved mechanism.

## 57. WG-TST-RES-CONTRACT-001 — resource owner controls action declaration

Requirements:

```text
WGREQ049
WGREQ094
```

Governance cannot add WorkManagement actions without owner contract.

# Action tests

## 58. WG-TST-ACT-DOM-001 — stable business action identity

Requirements:

```text
WGREQ050
WGREQ051
```

## 59. WG-TST-ACT-MIG-001 — persisted action rename requires migration

Requirements:

```text
WGREQ052
WGREQ172
```

If action identifier changes, test existing roles/policies/resource permissions migrate.

## 60. WG-TST-ACT-ARCH-001 — HTTP verb not canonical action

Requirements:

```text
WGREQ053
```

Source review/architecture guard.

# Permission tests

## 61. WG-TST-PERM-DOM-001 — canonical Permission meaning

Requirements:

```text
WGREQ054
```

Once Phase 2 classification is complete, tests must prove one semantic model.

## 62. WG-TST-PERM-MIG-001 — persisted permission identity stable

Requirements:

```text
WGREQ055
```

## 63. WG-TST-PERM-INT-001 — Workspace A permission cannot authorize Workspace B

Requirements:

```text
WGREQ056
WGREQ152
```

Mandatory tenant isolation.

## 64. WG-TST-PERM-APP-001 — explicit deny precedence if supported

Requirements:

```text
WGREQ057
WGREQ084
```

Only if current canonical model supports explicit deny.

If not supported, mark NOT_APPLICABLE in certification.

## 65. WG-TST-PERM-APP-002 — default deny

Requirements:

```text
WGREQ058
WGREQ085
```

No applicable grant/policy → denied.

## 66. WG-TST-PERM-INT-002 — permission cache revocation

Requirements:

```text
WGREQ059
WGREQ153
```

Given cached allow
When permission is revoked
Then access stops within accepted security window.

## 67. WG-TST-PRULE-APP-001 — PermissionRule deterministic evaluation

Requirements:

```text
WGREQ060
WGREQ061
```

Only after semantic classification.

## 68. WG-TST-PRULE-SEC-001 — client claims not trusted as rule facts

Requirements:

```text
WGREQ062
```

Attempt client-supplied fake owner/admin/resource attribute.

Expected:

```text
server-derived trusted facts win
```

# Role tests

## 69. WG-TST-ROLE-DOM-001 — Role semantics separate from Membership

Requirements:

```text
WGREQ063
```

## 70. WG-TST-ROLE-APP-001 — built-in role maps to expected permission set

Requirements:

```text
WGREQ064
```

Expected permissions come from canonical product/source authority, not test author invention.

## 71. WG-TST-ROLE-DOM-002 — built-in Role ID stable across display-name change

Requirements:

```text
WGREQ065
```

## 72. WG-TST-ROLE-APP-002 — role assignment to supported subject

Requirements:

```text
WGREQ066
```

## 73. WG-TST-ROLE-INT-001 — Workspace-scoped role does not leak

Requirements:

```text
WGREQ067
```

## 74. WG-TST-ROLE-APP-003 — custom role not required for P2 gate

Requirements:

```text
WGREQ068
```

This is a release/gate assertion rather than runtime test.

P2 core CI must not depend on custom-role suites unless product explicitly requires them.

## 75. WG-TST-ROLE-SEC-001 — unauthorized custom-role escalation

Requirements:

```text
WGREQ069
WGREQ154
```

If custom roles are release-scoped.

## 76. WG-TST-ROLE-DOM-003 — role deletion leaves no broader access

Requirements:

```text
WGREQ070
```

# Policy tests

## 77. WG-TST-POL-DOM-001 — Policy semantics constrained to current model

Requirements:

```text
WGREQ071
```

Do not assert speculative expressiveness.

## 78. WG-TST-POL-APP-001 — deterministic policy evaluation

Requirements:

```text
WGREQ072
```

Same trusted inputs → same decision.

## 79. WG-TST-POL-APP-002 — policy composition precedence

Requirements:

```text
WGREQ073
WGREQ084
```

Only once canonical composition is defined.

## 80. WG-TST-POL-MIG-001 — policy schema version migration

Requirements:

```text
WGREQ074
WGREQ174
```

## 81. WG-TST-POL-SEC-001 — malformed policy fails closed

Requirements:

```text
WGREQ075
WGREQ151
```

# ResourcePermission tests

## 82. WG-TST-RPERM-ARCH-001 — ResourcePermission meaning fixed

Requirements:

```text
WGREQ076
```

After Phase 2, architecture/source evidence must identify direct ACL vs role binding vs override.

## 83. WG-TST-RPERM-APP-001 — direct grant scope exact

Requirements:

```text
WGREQ077
```

If direct ACL.

## 84. WG-TST-RPERM-APP-002 — inheritance behavior only if supported

Requirements:

```text
WGREQ078
```

If unsupported:

```text
NOT_APPLICABLE
```

Do not invent parent traversal tests.

## 85. WG-TST-RPERM-INT-001 — direct permission revocation invalidates access

Requirements:

```text
WGREQ079
WGREQ153
```

# Effective authorization tests

## 86. WG-TST-AUTHZ-APP-001 — one canonical decision path

Requirements:

```text
WGREQ080
```

Representative command must not combine a second handler-local permission decision after pipeline allow/deny.

## 87. WG-TST-AUTHZ-APP-002 — valid authentication still requires authorization

Requirements:

```text
WGREQ081
```

Authenticated Actor lacking required permission is denied.

## 88. WG-TST-AUTHZ-APP-003 — active membership prerequisite

Requirements:

```text
WGREQ082
```

If Workspace membership is required for selected resource.

## 89. WG-TST-AUTHZ-APP-004 — supported permission sources compose deterministically

Requirements:

```text
WGREQ083
WGREQ084
```

Populate expected cases only after Phase 2 semantics resolved.

## 90. WG-TST-AUTHZ-SEC-001 — missing context fails closed

Requirements:

```text
WGREQ085
WGREQ151
```

Missing:

- Actor;
- Account;
- Workspace;
- Resource;
- Action;

must not become allow.

## 91. WG-TST-AUTHZ-API-001 — not-found privacy mapping

Requirements:

```text
WGREQ086
WGREQ136
```

Representative hidden-resource case follows API policy.

# Pipeline tests

## 92. WG-TST-PIPE-APP-001 — protected request declares resource/action requirement

Requirements:

```text
WGREQ087
```

## 93. WG-TST-PIPE-INT-001 — authorization runs before protected side effect

Requirements:

```text
WGREQ088
WGREQ182
```

Critical P2 gate test.

Given denial
Then handler protected state is unchanged.

## 94. WG-TST-PIPE-ARCH-001 — handler-local role check prohibited as canonical enforcement

Requirements:

```text
WGREQ089
WGREQ095
```

Use architecture/source scan where practical.

## 95. WG-TST-PIPE-ARCH-002 — API endpoint not sole business auth owner

Requirements:

```text
WGREQ090
```

Representative endpoint test/architecture rule.

## 96. WG-TST-PIPE-SEC-001 — background operation no implicit global bypass

Requirements:

```text
WGREQ091
```

## 97. WG-TST-PIPE-APP-002 — pipeline order compatibility

Requirements:

```text
WGREQ092
```

Verify current pipeline composition with:

```text
validation
authorization
idempotency
transaction
```

Do not assert a guessed absolute ordering if architecture allows multiple legitimate compositions; assert required safety invariants.

# Resource-team handshake tests

## 98. WG-TST-HANDSHAKE-ARCH-001 — downstream owns resource semantics

Requirements:

```text
WGREQ093
WGREQ094
```

## 99. WG-TST-HANDSHAKE-ARCH-002 — downstream does not encode role names

Requirements:

```text
WGREQ095
```

Search representative WorkManagement/Application code for local role display-name dependency after migration.

## 100. WG-TST-HANDSHAKE-INT-001 — Governance uses approved resource lookup contract

Requirements:

```text
WGREQ096
WGREQ133
```

No direct WorkManagement EF table read.

# ShareLink tests

## 101. WG-TST-SHARE-DOM-001 — ShareLink lifecycle

Requirements:

```text
WGREQ097
WGREQ099
WGREQ101
WGREQ102
```

Cover:

- create;
- active;
- expire;
- revoke.

## 102. WG-TST-SHARE-SEC-001 — bearer secret protected

Requirements:

```text
WGREQ098
WGREQ156
```

Verify:

- high-entropy generation through approved mechanism;
- raw secret not persisted if verifier model supports hashing/protection;
- no ordinary readback.

## 103. WG-TST-SHARE-INT-001 — bounded resource access only

Requirements:

```text
WGREQ099
WGREQ100
```

Given valid link to Resource R
Then cannot access unrelated Workspace resources.

## 104. WG-TST-SHARE-INT-002 — expiry invalidates cached access

Requirements:

```text
WGREQ101
WGREQ153
```

## 105. WG-TST-SHARE-CONC-001 — revoke/use race

Requirements:

```text
WGREQ102
WGREQ149
```

## 106. WG-TST-SHARE-SEC-002 — guessing public identifier does not expose privileged resource

Requirements:

```text
WGREQ103
```

## 107. WG-TST-SHARE-AUD-001 — share access audit identity

Requirements:

```text
WGREQ104
```

If product requires audit, record access mechanism/link identity without pretending anonymous actor is a normal User.

# Audit / SecurityEvent tests

## 108. WG-TST-AUD-ARCH-001 — Governance audit not duplicate observability log

Requirements:

```text
WGREQ105
```

Source/architecture classification.

## 109. WG-TST-AUD-INT-001 — historical audit record not mutated by later product edits

Requirements:

```text
WGREQ106
```

Where audit immutability is canonical.

## 110. WG-TST-AUD-INT-002 — critical governance change records actor/resource/action

Requirements:

```text
WGREQ107
```

## 111. WG-TST-SEVT-DOM-001 — SecurityEvent represents governance fact

Requirements:

```text
WGREQ108
```

## 112. WG-TST-AUD-SEC-001 — no bearer/authentication secrets in audit/security events

Requirements:

```text
WGREQ109
WGREQ164
```

## 113. WG-TST-AUD-INT-003 — audit query tenant isolation

Requirements:

```text
WGREQ110
WGREQ157
```

# Template tests

## 114. WG-TST-TPL-DOM-001 — template distinct from live assignment

Requirements:

```text
WGREQ111
```

## 115. WG-TST-TPL-APP-001 — template apply validates current permission/action catalog

Requirements:

```text
WGREQ112
```

## 116. WG-TST-TPL-MIG-001 — stored template version compatibility

Requirements:

```text
WGREQ113
```

# Upstream Identity/Account tests

## 117. WG-TST-UP-ARCH-001 — no Identity credential dependency

Requirements:

```text
WGREQ114
```

Workspaces/Governance must not reference:

- password;
- OAuth token;
- Session EF;
- MFA secret.

## 118. WG-TST-UP-INT-001 — canonical Account ID only

Requirements:

```text
WGREQ115
```

## 119. WG-TST-UP-INT-002 — disabled Account blocks protected Workspace operations

Requirements:

```text
WGREQ116
```

## 120. WG-TST-UP-INT-003 — disabled User cannot retain access via stale membership cache

Requirements:

```text
WGREQ117
WGREQ153
```

# WorkManagement handoff tests

## 121. WG-TST-WM-CONTRACT-001 — Board resource-category registration

Requirements:

```text
WGREQ118
WGREQ179
```

Board is represented through approved resource contract.

## 122. WG-TST-WM-CONTRACT-002 — BoardItem independent resource only if required

Requirements:

```text
WGREQ119
```

If not independently governable:

```text
NOT_APPLICABLE
```

and prove Board-level policy remains canonical.

## 123. WG-TST-WM-P2-001 — allowed Board action

Requirements:

```text
WGREQ120
WGREQ182
```

Given:

```text
Actor valid
Account valid
Workspace valid
membership active
permission granted
```

When Board protected action executes
Then allowed and handler commits.

## 124. WG-TST-WM-P2-002 — denied Board action

Requirements:

```text
WGREQ120
WGREQ182
```

Given no permission
Then denied and no handler side effect.

## 125. WG-TST-WM-P2-003 — cross-tenant Board denial

Requirements:

```text
WGREQ120
WGREQ152
```

## 126. WG-TST-WM-P2-004 — WorkManagement Domain invariant remains local

Requirements:

```text
WGREQ121
```

Even after authorization allows, invalid WorkManagement business transition still fails in WorkManagement.

This proves authorization did not absorb Domain rules.

# Documents / Collaboration tests

## 127. WG-TST-DOC-X-001 — Page resource handshake

Requirements:

```text
WGREQ122
```

Only when Documents integration is release-scoped.

## 128. WG-TST-COL-X-001 — Comment authorization uses target access contract

Requirements:

```text
WGREQ123
```

Collaboration must not query target private tables directly.

# Billing tests

## 129. WG-TST-BILL-X-001 — Billing admin authorization

Requirements:

```text
WGREQ124
```

## 130. WG-TST-BILL-X-002 — entitlement and authorization remain separate

Requirements:

```text
WGREQ125
```

Matrix:

```text
entitled + authorized → allowed
entitled + unauthorized → denied
not entitled + authorized → business/entitlement denial
```

Exact error taxonomy follows canonical Billing/API policy.

# Automation / Integrations tests

## 131. WG-TST-AUTO-X-001 — background automation actor follows approved semantics

Requirements:

```text
WGREQ126
```

No fake global-admin User.

## 132. WG-TST-INTG-X-001 — integration administration uses Governance

Requirements:

```text
WGREQ127
```

# Analytics tests

## 133. WG-TST-ANA-X-001 — Analytics uses approved governance facts only

Requirements:

```text
WGREQ128
```

No private-table dependency.

# Data ownership tests

## 134. WG-TST-OWN-ARCH-001 — Workspace persistence private

Requirements:

```text
WGREQ129
```

## 135. WG-TST-OWN-ARCH-002 — Governance persistence private

Requirements:

```text
WGREQ130
```

## 136. WG-TST-OWN-ARCH-003 — one Workspace membership truth

Requirements:

```text
WGREQ131
```

No second canonical membership aggregate in Governance.

## 137. WG-TST-OWN-ARCH-004 — one Role truth

Requirements:

```text
WGREQ132
```

Identity/Workspace/Product contexts must not own duplicate business role enums used as independent authorization.

## 138. WG-TST-OWN-ARCH-005 — no private cross-context DB join as public contract

Requirements:

```text
WGREQ133
```

# API tests

## 139. WG-TST-API-WSP-001 — Workspace endpoint contract matrix

Requirements:

```text
WGREQ134
WGREQ136
```

For each release-scoped endpoint classify:

```text
happy
validation
unauthenticated
forbidden
not-found/privacy
conflict
```

## 140. WG-TST-API-GOV-001 — Governance endpoint contract matrix

Requirements:

```text
WGREQ135
WGREQ136
```

## 141. WG-TST-API-SEC-001 — invitation/share secret response minimization

Requirements:

```text
WGREQ137
```

## 142. WG-TST-API-OAS-001 — OpenAPI drift

Requirements:

```text
WGREQ138
```

Must run existing OpenAPI drift gate when contracts change.

# Event tests

## 143. WG-TST-EVT-WSP-001 — Workspace event production

Requirements:

```text
WGREQ139
```

Representative producer-owned lifecycle facts only.

## 144. WG-TST-EVT-GOV-001 — Governance event production

Requirements:

```text
WGREQ140
```

## 145. WG-TST-EVT-INT-001 — event carries stable Account/Workspace/resource scope

Requirements:

```text
WGREQ141
```

## 146. WG-TST-EVT-SEC-001 — event secret minimization

Requirements:

```text
WGREQ142
```

## 147. WG-TST-EVT-CONTRACT-001 — event compatibility

Requirements:

```text
WGREQ143
```

Serialization/contract fixtures where downstream consumers exist.

# Concurrency matrix

## 148. WG-TST-CONC-MEM-001 — duplicate membership race

Requirements:

```text
WGREQ144
```

## 149. WG-TST-CONC-ADMIN-001 — last-admin race

Requirements:

```text
WGREQ145
```

If invariant exists.

## 150. WG-TST-CONC-INV-001 — invite accept/revoke

Requirements:

```text
WGREQ146
```

## 151. WG-TST-CONC-ROLE-001 — role assignment remove/add race

Requirements:

```text
WGREQ147
```

## 152. WG-TST-CONC-POL-001 — stale policy write

Requirements:

```text
WGREQ148
```

If version/concurrency control exists.

## 153. WG-TST-CONC-SHARE-001 — share-link revoke/use

Requirements:

```text
WGREQ149
```

# Security master matrix

## 154. WG-TST-SEC-MASTER-001 — privilege escalation

Requirements:

```text
WGREQ150
WGREQ154
```

Mandatory cases:

```text
self-promote role
assign stronger role without authority
grant direct resource permission above authority
invite with stronger role than allowed
Team membership escape
policy mutation without manage-policy authority
cross-Workspace grant
```

Expected:

```text
denied
no side effect
audit/security evidence where applicable
```

## 155. WG-TST-SEC-MASTER-002 — evaluator failure closes access

Requirements:

```text
WGREQ151
WGREQ159
```

Inject:

- missing policy data;
- evaluator exception;
- resource lookup failure.

Expected:

```text
deny/fail safely
```

Never allow.

## 156. WG-TST-SEC-MASTER-003 — tenant spoofing matrix

Requirements:

```text
WGREQ152
```

Matrix:

```text
Actor A + Account B
Actor A + Workspace B
Actor A + Resource B
valid Workspace + foreign Resource
valid Resource ID + foreign Account context
```

## 157. WG-TST-SEC-MASTER-004 — stale authorization cache

Requirements:

```text
WGREQ153
```

Revocation scenarios:

```text
membership
role
permission
policy if cached
resource permission
share link
```

## 158. WG-TST-SEC-MASTER-005 — denied policy details privacy

Requirements:

```text
WGREQ155
```

API/log output does not reveal hidden policy/resource details beyond accepted diagnostics.

## 159. WG-TST-SEC-MASTER-006 — share bearer secret

Requirements:

```text
WGREQ156
```

No raw secret in:

- logs;
- events;
- list APIs;
- audit payload.

## 160. WG-TST-SEC-MASTER-007 — audit API authorization

Requirements:

```text
WGREQ157
```

Unauthorized member cannot read governance audit/security events.

# Reliability tests

## 161. WG-TST-REL-PROV-001 — provisioning partial failure

Requirements:

```text
WGREQ158
```

Inject failure after one bootstrap step.

Verify canonical retry/compensation/partial-state semantics.

## 162. WG-TST-REL-AUTHZ-001 — resource/policy dependency unavailable

Requirements:

```text
WGREQ159
```

Fail safely.

## 163. WG-TST-REL-EVT-001 — integration-event publication follows outbox

Requirements:

```text
WGREQ160
```

If current architecture uses transactional outbox, verify state + event durability contract.

## 164. WG-TST-REL-CACHE-001 — authorization cache outage

Requirements:

```text
WGREQ161
```

Fallback must not default allow.

# Observability tests

## 165. WG-TST-OBS-INT-001 — authorization correlation

Requirements:

```text
WGREQ162
```

Representative protected operation should expose safe:

```text
Actor
Account
Workspace
ResourceKind
ResourceId
Action
Decision
Correlation
```

according to logging/privacy authority.

## 166. WG-TST-OBS-INT-002 — governance mutation traceability

Requirements:

```text
WGREQ163
```

Membership/role/policy/share mutations traceable.

## 167. WG-TST-OBS-SEC-001 — no secret telemetry

Requirements:

```text
WGREQ164
```

## 168. WG-TST-OBS-METRIC-001 — denial/security metric integration

Requirements:

```text
WGREQ165
```

Only if existing observability mechanism supports it.

No new vendor required.

# Performance tests

## 169. WG-TST-PERF-AUTHZ-001 — authorization hot-path baseline

Requirements:

```text
WGREQ166
```

Measure/review:

```text
DB calls
cache calls
policy evaluations
resource-parent lookups
```

No arbitrary threshold invented here.

## 170. WG-TST-PERF-MEM-001 — membership lookup indexing

Requirements:

```text
WGREQ167
```

Use query plan/index evidence where appropriate.

## 171. WG-TST-PERF-PERM-001 — permission lookup bounded

Requirements:

```text
WGREQ168
```

No unbounded full permission scan on hot path.

## 172. WG-TST-PERF-CACHE-001 — cache key correctness

Requirements:

```text
WGREQ169
```

If cache exists, key must distinguish:

```text
Actor/subject
Account
Workspace
resource
action
policy version as required
```

## 173. WG-TST-PERF-LIST-001 — pagination

Requirements:

```text
WGREQ170
```

Member/audit/permission lists use canonical pagination and avoid uncontrolled full-table retrieval.

# Migration tests

## 174. Migration applicability rule

Migration tests are required only when corresponding persisted contract changes.

No speculative migration should be created just to satisfy this document.

## 175. WG-TST-MIG-MEM-001 — membership schema migration

Requirements:

```text
WGREQ171
```

Preserve:

- User ID;
- Workspace ID;
- role assignments;
- historical references.

## 176. WG-TST-MIG-RES-001 — ResourceKind migration

Requirements:

```text
WGREQ172
```

If resource kind changes:

```text
existing permissions
roles
policies
resource permissions
share links
consumer declarations
```

must remain semantically correct.

## 177. WG-TST-MIG-ACT-001 — Action migration

Requirements:

```text
WGREQ172
```

## 178. WG-TST-MIG-ROLE-001 — built-in role semantic migration

Requirements:

```text
WGREQ173
```

Existing Workspace effective permissions must match intended new semantics.

## 179. WG-TST-MIG-POL-001 — policy schema migration

Requirements:

```text
WGREQ174
```

## 180. WG-TST-MIG-LINK-001 — invitation/share token format migration

Requirements:

```text
WGREQ175
```

Existing link behavior follows explicit compatibility/rotation/revoke policy.

## 181. WG-TST-MIG-DB-001 — clean database

Requirements:

```text
WGREQ176
```

## 182. WG-TST-MIG-DB-002 — supported upgrade database

Requirements:

```text
WGREQ176
```

## 183. WG-TST-MIG-DB-003 — no pending model changes

Requirements:

```text
WGREQ177
```

Do not suppress EF warning.

# P2 core gate tests

## 184. Purpose

These tests determine whether WorkManagement may rely on Workspace/Governance.

## 185. WG-TST-P2-CORE-001 — Workspace D5

Requirements:

```text
WGREQ178
```

Evidence must include:

- stable Workspace identity;
- Account containment;
- lifecycle baseline;
- tenant isolation.

## 186. WG-TST-P2-CORE-002 — Resource/Action D5

Requirements:

```text
WGREQ179
```

Evidence:

- stable resource kind;
- stable action identity;
- owner context registration;
- no private persistence coupling;
- migration semantics.

## 187. WG-TST-P2-CORE-003 — Permission D5

Requirements:

```text
WGREQ180
```

Evidence:

- stable meaning;
- tenant scope;
- role mapping;
- default-deny/fail-closed;
- revocation/cache;
- migration.

## 188. WG-TST-P2-CORE-004 — built-in Role D4+

Requirements:

```text
WGREQ181
```

Only initial required product roles.

Custom roles do not block.

## 189. WG-TST-P2-CORE-005 — central authorization D5

Requirements:

```text
WGREQ182
```

Must prove:

```text
allow
deny
deny-before-handler-side-effect
cross-tenant deny
production DI registration
```

## 190. WG-TST-P2-CORE-006 — WorkManagement handshake D5

Mandatory scenarios:

```text
Board allow
Board deny
Board cross-tenant deny
no WorkManagement role-name check
no Governance private WorkManagement DB access
```

## 191. WG-TST-P2-CORE-007 — migration/startup

If schema changed:

```text
clean DB
upgrade DB
seed/init if affected
no pending model
production graph starts
```

## 192. P2 gate rule

P3 uses two verification gates:

```text
P3-A Domain/Data parallelization
→ Workspace/containment/resource-category/Action producer contracts D4+ + ownership architecture guards

P3-B protected Application/API release
→ required P2 protected-slice authorization/security groups pass in canonical CI
```

Secondary ShareLink/custom-role/template tests do not block P3-B unless release architecture made them part of the initial authorization contract.

# Secondary capability release gates

## 193. Invitation release gate

Required:

- create;
- authorize;
- expiry;
- revoke;
- replay;
- race;
- secret safety.

## 194. Team release gate

Required:

- Workspace containment;
- valid member relation;
- no escalation;
- lifecycle;
- Governance subject behavior if supported.

## 195. Space release gate

Required:

- canonical meaning;
- containment;
- lifecycle;
- authorization if governable.

## 196. Custom-role release gate

Required:

- create/update/delete;
- permission mapping;
- privilege escalation;
- assignment scope;
- revocation/cache;
- migration.

## 197. Policy release gate

Required:

- deterministic evaluation;
- trusted inputs;
- fail-closed;
- precedence;
- versioning.

## 198. ResourcePermission release gate

Required:

- canonical semantics;
- exact scope;
- revocation;
- inheritance only if supported.

## 199. ShareLink release gate

Required:

- secure issuance;
- bounded access;
- secret safety;
- expiry;
- revocation;
- race;
- audit where required.

# Cross-layer vertical scenarios

## 200. Workspace creation vertical slice

```text
API
→ Application command
→ Actor/Account context
→ authorization
→ Workspace Domain
→ Infrastructure persistence
→ event
→ API response
```

Required layers:

```text
Domain
Application
API
Integration
Migration if schema changed
```

## 201. Membership admin vertical slice

```text
API
→ Application
→ authorization
→ WorkspaceMember
→ Governance role assignment if applicable
→ persistence
→ event/audit
```

## 202. Board authorization vertical slice

```text
Actor
→ Account
→ Workspace
→ Board resource contract
→ Governance effective decision
→ Application pipeline
→ WorkManagement handler
```

This is the primary P2→P3 proof.

## 203. ShareLink vertical slice

```text
issue
→ return/share secret
→ verify
→ resource scope
→ effective access
→ expire/revoke
→ deny
```

# Privilege-escalation master matrix

## 204. Member self-escalation

```text
member attempts own admin role
→ denied unless canonical policy explicitly permits
```

## 205. Admin grant beyond authority

```text
limited admin grants permission/action they cannot delegate
→ denied
```

## 206. Invitation escalation

```text
inviter creates invitation with stronger role than authorized
→ denied
```

## 207. Team escalation

```text
non-member inserted into Team
→ must not gain Workspace access
```

## 208. ResourcePermission escalation

```text
subject creates direct grant above authority
→ denied
```

## 209. Policy escalation

```text
subject edits policy they cannot administer
→ denied
```

## 210. Cross-Workspace escalation

```text
grant from Workspace A targets resource in B
→ denied
```

# Tenant-isolation master matrix

## 211. Workspace boundary

```text
Account A Actor
→ Workspace B
→ deny/not-found
```

## 212. Membership boundary

```text
Member A
→ membership B
→ deny
```

## 213. Permission boundary

```text
permission/role in Workspace A
→ Resource B
→ deny
```

## 214. Share boundary

```text
ShareLink A
→ Resource B
→ deny
```

## 215. Audit boundary

```text
Workspace A audit reader
→ Workspace B audit
→ deny
```

## 216. Background boundary

```text
missing Account/Workspace scope
→ protected background mutation
→ fail safely
```

# Test fixture design

## 217. Core tenant fixture

Provide reusable valid fixtures:

```text
Account A
Account B

Workspace A1 under A
Workspace B1 under B

User/Actor AdminA
User/Actor MemberA
User/Actor OutsiderB

Membership AdminA→A1
Membership MemberA→A1
```

This fixture supports most tenant/authz tests.

## 218. Governance fixture

Provide canonical builders for:

```text
ResourceKind
Action
Permission
Role
Policy if supported
ResourcePermission if supported
```

Fixtures must use real Domain invariants, not duplicate business validation.

## 219. Invitation fixture

Support:

```text
active
expired
revoked
accepted
```

with deterministic clock if architecture provides one.

## 220. ShareLink fixture

Use sentinel bearer secret for leakage detection.

Never emit actual secret value in test failure logs.

## 221. Resource-owner fixture

For P2 gate, use representative Board resource contract or dedicated in-test resource stub only if the architecture tests a generic Governance component.

Final WorkManagement handshake must use actual WorkManagement-facing contract.

# Test anti-patterns

## 222. Do not mock authorization decision in authorization integration tests

Bad:

```text
mock evaluator returns denied
→ claim pipeline is correct
```

Good:

```text
real Governance evaluator + representative role/permission data
→ pipeline denies
```

## 223. Do not mock DB uniqueness for concurrency invariants

Use actual DB/constraint.

## 224. Do not hardcode role display names in test infrastructure

Tests should reference canonical role identity/catalog from source.

## 225. Do not encode unresolved PermissionRule/Policy semantics

Wait for Phase 2 decision.

## 226. Do not test every entity as separately governable

Only resources approved by product/resource owner get resource-level tests.

## 227. Do not overfit HTTP error messages

Assert typed/status/category contract unless exact text is public API.

# Infrastructure verification

## 228. Unique constraints

Where applicable:

```text
Workspace unique key/slug if canonical
Membership uniqueness
Role assignment uniqueness
ResourcePermission uniqueness
ShareLink verifier identity
Invitation verifier identity
```

## 229. Indexes

Verify hot queries have supporting indexes based on actual schema:

```text
Workspace by Account
Member by Workspace+User
Role assignment by subject+Workspace
Permission/resource by resource+subject
Audit by Workspace/time
```

Do not create indexes without measured/query rationale.

## 230. RLS/query filters

If production uses RLS/query filters, integration tests must exercise production-like configuration.

## 231. Secret verifier storage

Invitation/share bearer secrets must follow approved secure storage.

# API verification

## 232. Endpoint security matrix

Every sensitive endpoint should classify:

```text
authentication required?
resource/action?
tenant scope?
CSRF if browser mutation?
secret response?
audit?
```

## 233. Unauthorized vs forbidden

Representative API tests distinguish:

```text
no authenticated Actor
valid Actor but no permission
resource hidden/not-found
```

according to canonical policy.

## 234. Share/invite public endpoint

If public:

- strict token validation;
- minimal data exposure;
- no tenant enumeration.

# Architecture verification

## 235. Workspaces Domain purity

No Infrastructure/API/HTTP/provider dependencies.

## 236. Governance Domain purity

Same.

## 237. Workspaces/Governance separation

One team ownership does not allow direct private model mutation between contexts beyond approved references/contracts.

## 238. Resource-owner boundary

Governance must not own WorkManagement/Document/Billing entities.

## 239. Authorization pipeline ownership

Feature handlers do not become primary authorization engines.

## 240. No production-project split

Unless approved ADR.

# Integration verification

## 241. Production DI

At least one P2 critical path must run with production DI:

```text
Actor
Account
Workspace
Governance evaluator
authorization behavior
handler
DB
```

## 242. Persistence provider

Use supported production-like provider for:

- uniqueness;
- isolation;
- migrations;
- transaction behavior.

## 243. Cross-context contracts

Use Application/event contracts, not test-only direct repository shortcuts.

# Migration matrix

## 244. Change class → test

| Change | Mandatory evidence |
|---|---|
| Workspace schema | clean + upgrade |
| Membership unique key/state | upgrade with representative existing rows |
| ResourceType/ResourceKind/category rename | semantic justification + permissions/roles/policies/consumer migration |
| Action rename | same |
| Built-in Role semantics | existing Workspace effective-permission comparison |
| Policy schema | version/backfill |
| ResourcePermission schema | upgrade + access equivalence |
| Invitation/share token verifier | compatibility/rotation/revoke |
| new index/constraint | migration with conflicting/valid representative data |

# Performance / reliability evidence

## 245. Authorization query count

Record before/after for representative P2→P3 protected Board action if performance changes materially.

## 246. No N+1 role/policy resolution

Permission evaluation must not load role/policy collections per item in a bulk/list operation without bounded strategy.

## 247. Cache invalidation correctness

Correctness test first; performance optimization second.

## 248. Cache outage

No default allow.

# CI mapping

## 249. architecture-tests

Must include:

```text
WG-TST-WSP-ARCH-*
WG-TST-RULE-ARCH-*
WG-TST-RES-ARCH-*
WG-TST-PIPE-ARCH-*
WG-TST-HANDSHAKE-ARCH-*
WG-TST-OWN-ARCH-*
WG-TST-RES-ARCH-001A/B/C
```

where implemented by structural tests.

## 250. core-tests — Domain

Must include:

```text
Workspace
Membership
Invitation
Role
Permission
Policy
ShareLink
```

as release-scoped.

## 251. core-tests — Application

Must include:

```text
Workspace operations
Membership operations
Resource/Action
Permission/Role
authorization evaluator
ShareLink/Policy operations
```

## 252. core-tests — Infrastructure

Must include:

```text
persistence
unique constraints
cache adapters
secret verifier storage
migrations
```

where applicable.

## 253. platform-tests

Required if central authorization pipeline mechanism itself changes.

If only Governance semantics change and pipeline mechanism remains untouched, specific Platform gate may be N/A while overall CI still runs canonical jobs.

## 254. api-tests

Must include:

- Workspace;
- membership/invitations;
- Governance;
- share/public boundary where release-scoped;
- OpenAPI drift.

## 255. integration-tests

Mandatory P2 gate evidence:

```text
P1 upstream contract
tenant isolation
membership revocation
role/permission evaluation
production DI
Board handshake
migration/startup if applicable
```

## 256. CI non-zero requirement

Relevant filtered suites must execute at least one intended test.

Zero matching tests:

```text
not certification evidence
```

## 257. Exact SHA

Final certification only uses CI on exact candidate SHA.

# Test implementation order

## 258. Recommended order

```text
1. architecture/source guards
2. Workspace tests
3. Membership tests
4. Resource/Action tests
5. Permission tests
6. Built-in Role tests
7. authorization pipeline tests
8. WorkManagement handshake tests
9. P2 gate tests
10. Invitation/provisioning
11. Teams/Spaces
12. Policy/ResourcePermission/custom Role
13. ShareLinks
14. Audit/SecurityEvents/Templates
15. migration/security/concurrency/performance hardening
```

## 259. Test-first guidance

Write failing tests before code when:

- semantics are already stable;
- regression is known;
- security invariant is clear.

For ambiguous models:

```text
classify first
then encode accepted semantics
```

# Requirement-family completeness

## 260. WGREQ001–WGREQ011

Covered by:

```text
WG-TST-WSP-*
```

## 261. WGREQ012–WGREQ023

Covered by:

```text
WG-TST-MEM-*
```

## 262. WGREQ024–WGREQ033

Covered by:

```text
WG-TST-INV-*
```

## 263. WGREQ034–WGREQ043

Covered by:

```text
WG-TST-TEAM-*
WG-TST-SPACE-*
WG-TST-RULE-*
```

## 264. WGREQ044–WGREQ062

Covered by:

```text
WG-TST-RES-*
WG-TST-ACT-*
WG-TST-PERM-*
WG-TST-PRULE-*
```

## 265. WGREQ063–WGREQ086

Covered by:

```text
WG-TST-ROLE-*
WG-TST-POL-*
WG-TST-RPERM-*
WG-TST-AUTHZ-*
```

## 266. WGREQ087–WGREQ096

Covered by:

```text
WG-TST-PIPE-*
WG-TST-HANDSHAKE-*
```

## 267. WGREQ097–WGREQ113

Covered by:

```text
WG-TST-SHARE-*
WG-TST-AUD-*
WG-TST-SEVT-*
WG-TST-TPL-*
```

## 268. WGREQ114–WGREQ143

Covered by:

```text
WG-TST-UP-*
WG-TST-WM-*
WG-TST-DOC-*
WG-TST-COL-*
WG-TST-BILL-*
WG-TST-AUTO-*
WG-TST-INTG-*
WG-TST-ANA-*
WG-TST-OWN-*
WG-TST-API-*
WG-TST-EVT-*
```

## 269. WGREQ144–WGREQ170

Covered by:

```text
WG-TST-CONC-*
WG-TST-SEC-MASTER-*
WG-TST-REL-*
WG-TST-OBS-*
WG-TST-PERF-*
```

## 270. WGREQ171–WGREQ177

Covered by:

```text
WG-TST-MIG-*
```

only when corresponding migration exists.

## 271. WGREQ178–WGREQ182

Covered by:

```text
WG-TST-P2-CORE-*
```

# P2 mandatory minimum test set

## 272. Mandatory before P3-B protected release opens

The following are non-optional:

```text
Workspace stable ID
Account→Workspace containment
cross-account Workspace denial
Membership uniqueness
member add/remove authorization
member revocation access loss
resource-category/Action stability (preserve valid current naming)
default deny/fail closed
permission tenant scope
built-in Role baseline
central authorization production registration
deny-before-handler-side-effect
Board allow
Board deny
Board cross-tenant deny
no handler role-string coupling
no Governance private WorkManagement DB dependency
resource-owner facts adapter placement proven
facts-vs-policy boundary proven
no duplicate authorization behavior/evaluator stack
transport-neutral extraction seam
architecture gates
clean/upgrade DB if schema changed
production DI graph
```

## 273. Not mandatory for P2 core unless initial product requires them

```text
custom roles
advanced policy DSL
resource inheritance
advanced ShareLinks
governance templates
advanced Team/Space behavior
full audit UX
```

# Certification handoff format

## 274. Required evidence record

For each P2 certification capability:

```text
Capability:
WGREQ IDs:
WG-TST IDs:
Test project:
Command:
Executed count:
Passed:
Failed:
Skipped:
CI job:
Candidate SHA:
Known exclusions:
```

## 275. Missing test

If required invariant cannot be exercised:

```text
verification gap
→ explicit test-infrastructure work item
→ capability cannot reach D5
```

## 276. Skipped critical test

Skipped critical scenario is not PASS.

## 277. Flaky critical test

Repeated rerun without root-cause resolution is not D5 evidence.

# Review checklists

## 278. Workspace review

- [ ] Workspace ID stable.
- [ ] Account containment tested.
- [ ] lifecycle positive/negative.
- [ ] cross-account isolation.
- [ ] no generic settings → permission bypass.

## 279. Membership review

- [ ] duplicate membership blocked.
- [ ] add/remove authorized.
- [ ] last-admin concurrency if applicable.
- [ ] inactive/removed member loses access.
- [ ] historical attribution preserved.
- [ ] Identity disable interaction.

## 280. Authorization semantics review

- [ ] Resource authorization category canonical; valid current naming preserved unless migration approved.
- [ ] Resource-owner facts provider ownership proven.
- [ ] No Governance private downstream DbContext access.
- [ ] Existing Application authorization path reused/hardened; no parallel stack.
- [ ] Facts / Policy / Enforcement ownership proven.
- [ ] Future extraction seam is transport-neutral.
- [ ] Actions stable.
- [ ] Permission meaning unique.
- [ ] PermissionRule classified.
- [ ] Policy classified.
- [ ] ResourcePermission classified.
- [ ] default deny.
- [ ] precedence explicit where needed.

## 281. Privilege review

- [ ] no self role escalation.
- [ ] no overpowered invite.
- [ ] no Team escape.
- [ ] no cross-Workspace grant.
- [ ] no policy mutation without authority.
- [ ] no direct ACL escalation.

## 282. Pipeline review

- [ ] production DI.
- [ ] denial before side effect.
- [ ] API not sole enforcement.
- [ ] background no implicit bypass.
- [ ] pipeline order safety.

## 283. ShareLink review

- [ ] secure secret.
- [ ] bounded access.
- [ ] no ordinary readback.
- [ ] expiry.
- [ ] revoke.
- [ ] race.
- [ ] secret absent logs/events.

## 284. Migration review

- [ ] clean DB.
- [ ] upgrade DB.
- [ ] resource/action compatibility.
- [ ] role/permission impact.
- [ ] policy schema if changed.
- [ ] no pending model changes.

## 285. Cross-context review

- [ ] P1 upstream only through stable contracts.
- [ ] Board handshake.
- [ ] no private DB cross-context dependency.
- [ ] entitlement vs authorization separate.
- [ ] Analytics uses approved facts.

## 286. CI review

- [ ] architecture non-zero PASS.
- [ ] Domain non-zero relevant PASS.
- [ ] Application non-zero relevant PASS.
- [ ] Infrastructure relevant PASS.
- [ ] API PASS.
- [ ] Integration PASS.
- [ ] Platform PASS if mechanism changed.
- [ ] OpenAPI drift PASS if changed.
- [ ] exact candidate SHA.

# Definition of Done — TESTS artifact

## 287. TESTS complete when

- every WGREQ family has a mapped verification family;
- P2 core gate has concrete mandatory tests;
- privilege escalation has a dedicated matrix;
- tenant isolation has a dedicated matrix;
- ambiguous models are explicitly gated until source classification;
- concurrency and migration applicability are explicit;
- WorkManagement handoff is executable;
- architecture/private-persistence rules are verified;
- CI ownership is mapped.

## 288. TESTS does not imply current implementation passes

The implementation may reveal:

```text
missing test
stale test
wrong test
duplicate test
unreachable test path
missing test infrastructure
```

These findings feed back into PLAN and later CERTIFICATION.

## 289. Final verification rule

Before P2 is marked D5, ask:

```text
Can we prove that an Actor:
- is in the correct Account,
- is in the correct Workspace,
- targets the correct resource,
- requests a defined action,
- receives one deterministic Governance decision,
- cannot bypass that decision,
- cannot escalate through stale or conflicting state?
```

If any critical part cannot be proven:

```text
P2 is not STABLE
```

That rule is mandatory.

# Cross-context contract certification — revised mandatory matrix

Before FULL certification, each integrated bounded context must have at least one contract record and an
architecture test proving the same ownership discipline.

| Producer | Typical facts/actions owned | Governance consumes | Forbidden coupling |
|---|---|---|---|
| Identity/Accounts | Actor validity, Account identity/lifecycle | trusted actor/account context | Governance mutating credentials/session state |
| Workspaces | Workspace containment, membership facts | workspace/member scope | Governance owning Workspace lifecycle |
| WorkManagement | Board/Item identity, containment, visibility/relationship facts, product actions | facts + action IDs | Governance querying WorkManagement private EF |
| Documents | Page/Block identity, containment and document actions | facts + action IDs | Governance querying Documents private persistence |
| Billing | subscription/payment resource identity and billing-admin actions | policy mapping for authorized actors | Governance owning Billing business state |
| Automation/Integrations | automation/integration resource identity and actions | permission/policy evaluation | local shadow RBAC or private-table mutation |
| Collaboration | collaboration resource facts/actions where independently governable | policy evaluation | duplicate permission engine |
| Analytics/Audit | downstream facts/traceability unless separately authoritative | authorization/audit facts | analytics becoming source of authorization truth |

For each row actually onboarded, verify:

1. owner can change its private persistence without requiring Governance source changes beyond the stable contract;
2. Governance cannot read/mutate that private persistence directly;
3. authorization still fails closed under stale/missing required facts according to accepted consistency policy;
4. contract can be adapted from in-process implementation to gRPC/event projection/cache/hybrid without changing Domain ownership;
5. no network transport is introduced merely to simulate future microservices inside the current modular monolith.


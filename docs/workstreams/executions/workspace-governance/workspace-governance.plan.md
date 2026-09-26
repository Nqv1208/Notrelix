---
document_id: WRK-SPEC-WORKSPACE-GOVERNANCE
document_type: workstream-spec
status: active
owner: workspace-governance-team
applies_to:
  - backend
  - workspaces
  - governance
  - workspace
  - workspace-membership
  - invitations
  - teams
  - spaces
  - workspace-rules
  - provisioning
  - workspace-settings
  - resource-kind
  - resource-action
  - permissions
  - permission-rules
  - roles
  - policies
  - resource-permissions
  - share-links
  - governance-templates
  - authorization
  - audit-security-evidence
  - tenant-isolation
evidence:
  - docs/product/workspaces.md
  - docs/product/governance.md
  - docs/architecture/bounded-context-map.md
  - docs/architecture/contract-boundaries.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - docs/delivery/team-ownership.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/capability-delivery-map.md
  - docs/workstreams/cross-team-dependencies.md
  - docs/workstreams/teams/workspace-governance.md
  - docs/workstreams/teams/identity-accounts.md
  - docs/workstreams/teams/platform-foundation.md
  - docs/workstreams/executions/identity-accounts/identity-accounts.spec.md
  - docs/workstreams/executions/backend-team-architecture-closure/backend-team-architecture-closure.spec.md
  - backend/docs/architecture/backend-overview.md
  - backend/docs/architecture/domain-modeling.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/api-and-contracts.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/testing-and-quality-gates.md
  - backend/docs/generated/project-map.md
review_on:
  - workspace-boundary-change
  - governance-boundary-change
  - account-workspace-contract-change
  - membership-model-change
  - invitation-model-change
  - resource-action-contract-change
  - permission-model-change
  - role-model-change
  - authorization-enforcement-change
  - access-facts-change
  - rls-authorization-change
  - share-link-security-change
  - downstream-resource-registration-change
  - application-persistence-exception-change
  - p2-exit-gate-change
---

# SPEC — Workspace & Governance

## 1. Purpose

This is the master **WHAT** contract for the Workspace & Governance workstream on the backend critical path.

It is intentionally brownfield and source-first. The document defines the target invariants while explicitly separating:

```text
source exists
!= capability is complete
!= capability is verified
!= capability is stable for downstream consumers
```

The P2 protected-slice output is:

```text
stable Account → Workspace containment
+ stable Workspace membership
+ stable ResourceKind / ResourceId / PermissionAction contract
+ stable permission / built-in-role semantics
+ one canonical Application access-control path
+ source-owned authorization facts
+ RLS defense-in-depth
+ named P3 resource handshake
```

Implementation order belongs to `workspace-governance.plan.md`.
Verification mapping belongs to `workspace-governance.tests.md`.
Executed evidence and D4/D5 status belong to `workspace-governance.certification.md`.

No requirement in this SPEC may be marked complete merely because a class, table, endpoint, migration or test file exists.

## 2. Revision intent and compatibility

This revision aligns the Workspace & Governance execution contract with the audited `develop` source while preserving the established product/bounded-context model.

It deliberately corrects stale execution-document assumptions:

1. the canonical Application layout is module-first under `Features/Workspaces` and `Features/Governance`;
2. the current access-control seam is `AccessControlBehavior`, not an older `AuthorizationBehavior`/decision-store description;
3. `IWorkspaceDbContext` and `IGovernanceDbContext` currently exist inside Application and expose EF `DbSet` as an approved local exception, but they are not public cross-context contracts;
4. Governance Domain breadth is materially larger than current Governance Application/API breadth;
5. RLS is persistence defense-in-depth, not the business authorization engine;
6. current `AccessFactsQuery` is runtime evidence and must not be expanded into an ungoverned cross-context query surface;
7. documentation paths use `docs/workstreams/executions/...` and `docs/workstreams/capability-delivery-map.md`.

This SPEC does **not** authorize a mass source refactor merely to make paths resemble documentation.

## 3. Authority and precedence

For conflicts, use this order:

```text
approved product/bounded-context authority
→ accepted backend architecture / ADR / architecture-closure decisions
→ exact candidate source behavior
→ this workstream SPEC
→ PLAN implementation steps
→ TESTS verification mapping
→ CERTIFICATION executed status
```

If product authority and source disagree materially, execution stops and records a decision instead of silently choosing the easier implementation.

## 4. Relationship to Identity & Accounts

P2 consumes the P1 producer contract for stable:

- Actor/User identity;
- Account identity;
- current Account/tenant semantics;
- authentication/session principal formation;
- tenant-isolation prerequisites.

Workspace & Governance MUST NOT reconstruct credentials, sessions, User profile ownership, Account identity or API-token authentication.

Workspace membership is not Account membership, and Workspace containment is not authorization.

## 5. Workspaces vs Governance ownership

The canonical split is:

```text
Facts       → resource-owning bounded context
Policy      → Governance
Enforcement → Application access-control pipeline
Persistence isolation → Infrastructure/RLS defense-in-depth
Transport protection  → API
```

### 5.1 Workspaces owns

- Workspace aggregate/lifecycle and `AccountId` containment;
- WorkspaceMember state and owner/admin safety;
- WorkspaceInvitation lifecycle;
- Team/TeamMember and Space state;
- Workspace settings and true Workspace rules;
- personal Workspace provisioning semantics owned by Workspaces;
- Workspace-originated facts/events.

### 5.2 Governance owns

- permission/action policy semantics;
- PermissionRule semantics;
- ResourcePermission semantics;
- built-in role-to-authority interpretation where Governance policy consumes Workspace role facts;
- CustomRole/MemberRoleAssignment semantics;
- WorkspacePolicy semantics;
- ShareLink access-capability semantics;
- PermissionTemplate semantics;
- final policy evaluation implementation behind the Application security port.

### 5.3 Resource owner retains

A resource-owning context retains:

- resource lifecycle;
- resource containment and visibility facts;
- resource-specific membership/relationship facts;
- resource-specific action vocabulary and business meaning;
- its private persistence.

Governance does not gain ownership of Board/Page/etc. merely because it authorizes access.

## 6. Physical architecture constraint

The production backend remains:

```text
Notrelix.Domain
Notrelix.Application
Notrelix.Infrastructure
Notrelix.Platform
Notrelix.API
```

P2 does not create new production projects such as `Notrelix.Governance.Service` or `Notrelix.Authorization.Service`.

Logical bounded-context ownership exists inside the modular monolith and survives any future extraction.

## 7. Audited source snapshot — preparation evidence only

Preparation audit:

```text
branch: develop
commit: 35702d0fa9fb01ed68b0667bab500030d60bd028
tree:   271f825cb27ca06c0a7931804b70515fe3e2bae1
```

PLAN Phase 0 MUST recapture the exact execution candidate. This SHA is not automatically the implementation/certification SHA.

Approximate file evidence at this audit snapshot:

| Surface | Files observed | Meaning |
|---|---:|---|
| Domain/Workspaces | 68 | broad Domain model exists |
| Domain/Governance | 55 | broad Governance Domain exists |
| Application/Features/Workspaces | 94 | broad Workspace use-case surface exists |
| Application/Features/Governance | 13 | Governance Application surface is much narrower than Domain |
| API Workspaces contracts/endpoints | 79 | Workspace transport surface is broad |
| API Governance contracts/endpoints | 9 | Governance transport is currently limited |
| Infrastructure paths related to Workspace/Governance/Authz | ~57 | persistence/authz/messaging adapters exist |
| tests matching Workspace names | ~85 | significant but not certification by itself |
| tests matching Governance/Authz names | ~27 | significant but not certification by itself |

Counts are discovery evidence, not acceptance criteria.

## 8. Current source inventory that execution must classify

### 8.1 Domain — Workspaces

Observed capability families:

```text
Workspaces/Workspaces
  Workspace
  WorkspaceSettings
  WorkspaceStatus
  WorkspaceFactory
  lifecycle Domain events

Workspaces/Members
  WorkspaceMember
  WorkspaceRole { Guest, Member, Admin, Owner }
  WorkspaceMemberStatus
  owner/member rules
  membership Domain events

Workspaces/Invitations
  WorkspaceInvitation
  WorkspaceInvitationStatus
  InvitationTokenHash
  token generation/hash version/expiry
  invitation Domain events

Workspaces/Teams
  Team
  TeamMember
  TeamMemberRole / TeamMemberStatus / TeamStatus

Workspaces/Spaces
  Space
  SpaceType / SpaceVisibility / SpaceStatus

Workspaces/Rules
  WorkspaceRules
  WorkspaceOwnerRules
  WorkspaceMemberRules
  WorkspaceInvitationRules
  TeamRules / TeamLeadRules / SpaceRules
```

### 8.2 Domain — Governance

Observed capability families:

```text
Governance/Permissions
  PermissionAction
  PermissionLevel
  PermissionEffect
  PermissionScope / PermissionScopeType
  PermissionSubjectType
  PermissionRule
  ResourcePermission
  FieldPermission

Governance/Roles
  CustomRole
  CustomRolePermission
  MemberRoleAssignment

Governance/Policies
  WorkspacePolicy
  GuestAccessPolicy
  ResourcePolicy
  SharingPolicy

Governance/ShareLinks
  ShareLink
  ShareLinkTokenHash
  ShareLinkAccessMode
  ShareLinkStatus

Governance/Templates
  PermissionTemplate
  PermissionTemplateDefinition
  PermissionTemplateEntry
  PermissionTemplateScope / Status
```

Domain presence MUST be classified separately from delivered Application/API capability.

### 8.3 Application — current module-first topology

Observed Workspaces feature areas:

```text
Features/Workspaces/
  Abstractions
  DTOs
  Invitations
  Members
  Provisioning
  Settings
  Spaces
  Teams
  WorkspaceHome
  Workspaces
```

Observed Governance feature areas:

```text
Features/Governance/
  Abstractions
  Authorization
  DTOs
  ResourcePermissions
  ShareLinks
```

This asymmetry is important. It means `CustomRole`, `WorkspacePolicy`, `PermissionTemplate`, etc. cannot be called Application/API complete merely because Domain/persistence types exist.

### 8.4 API surface

Observed Workspace endpoint families:

```text
Activity
Invitations
Members
Settings
Spaces
Teams
Workspaces
```

Observed Governance endpoint families:

```text
ResourcePermissions
ShareLinks
```

PLAN must inventory exact commands/queries/routes/OpenAPI at its candidate SHA.

### 8.5 Persistence and runtime authorization evidence

Current owner-local Application abstractions include:

```text
IWorkspaceDbContext
IGovernanceDbContext
```

They currently expose EF `DbSet` and are an approved Application EF exception under `application-model.md`.

Normative restriction:

```text
owner-local persistence abstraction
!= Producer.Public contract
!= cross-context integration API
```

Current canonical protected-request seam:

```text
Request descriptor
→ ExecutionContextSnapshot
→ AccessControlBehavior<TRequest,TResponse>
→ IAccessFactsProvider
→ AccessFacts
→ IAccessPolicyEvaluator
→ AccessPolicyEngine
→ allow / unauthorized / forbidden / not-found / security-misconfiguration
→ handler only when allowed
```

Current Infrastructure facts implementation includes:

```text
PostgresAccessFactsProvider
AccessFactsQuery
```

`AccessFactsQuery` is an important brownfield boundary: at the audited SHA it still reads several shared-schema tables directly for user/account/workspace membership, WorkManagement Board facts, Governance permission state and feature-entitlement state. That source is **current mechanism evidence**, not proof that every direct read is the preferred future cross-context contract. PLAN Phase 0 must classify each read against the backend architecture-closure taxonomy before expanding it.

Current source already demonstrates the safer composition direction in two places:

- Documents Page authorization facts are composed through the Documents-owned `IPageAuthorizationFacts` contract;
- Billing subscription requirements are composed through `IBillingSubscriptionFacts`.

New resource families MUST prefer producer-owned facts/Public contracts where required by architecture. Do not add a new foreign table branch to `AccessFactsQuery` merely because the database is physically shared.

Current source also includes `WorkspaceGrantProjectionServiceAdapter` and RLS/session-context infrastructure. PLAN/TESTS must verify how membership lifecycle changes project into database access grants and whether suspension/removal/revocation reaches the RLS defense layer correctly.

Current RLS scripts/policies exist under Infrastructure and remain **defense-in-depth**.

### 8.6 Events/messaging evidence

Current source contains Workspace integration events and consumers for flows such as:

- registration → personal Workspace provisioning;
- invitation delivery;
- Workspace created;
- Workspace member added/removed;
- activity/realtime downstream consumers where registered.

Only mapped/published integration contracts are public facts. A Domain event is not automatically an external contract.

## 9. Capability status vocabulary for PLAN/CERTIFICATION

Every capability discovered in source must be classified using evidence such as:

```text
NOT_FOUND
DOMAIN_PRESENT
APPLICATION_PRESENT
INFRA_PRESENT
API_PRESENT
TEST_PRESENT
INTEGRATED
VERIFIED (D4)
STABLE (D5)
SOURCE_DEBT
BLOCKED
NOT_APPLICABLE
```

Do not skip intermediate classification by writing `implemented` from folder presence.

## 10. P2 core vs secondary scope

### P2 mandatory protected slice

P2 cannot open protected P3 release until these are evidence-backed:

1. Workspace identity and Account containment;
2. WorkspaceMember baseline and owner safety;
3. stable ResourceKind / ResourceId contract;
4. stable `PermissionAction` contract for representative downstream resources;
5. permission/default-deny semantics;
6. built-in Workspace role facts and deterministic authority interpretation;
7. canonical `AccessControlBehavior` handshake;
8. source-owned authorization facts;
9. tenant/RLS defense-in-depth;
10. representative WorkManagement Board protected flow.

### P2 secondary scope

May continue after the P3 protected slice is opened, unless a dependency promotes it to a blocker:

- advanced invitation UX/lifecycle hardening;
- Teams/Spaces depth;
- CustomRole administration;
- WorkspacePolicy administration;
- PermissionRule authoring depth;
- ResourcePermission advanced lifecycle/inheritance;
- ShareLink lifecycle breadth;
- PermissionTemplate application;
- governance audit/security UX;
- advanced caches/projections.

Secondary does not mean unowned or undocumented.

## 11. Capability map

```text
WG-WSP-01 Workspace identity/lifecycle
WG-WSP-02 Account containment
WG-WSP-03 Workspace settings/home
WG-WSP-04 personal Workspace provisioning

WG-MEM-01 membership identity/lifecycle
WG-MEM-02 owner/admin safety
WG-MEM-03 membership authorization facts

WG-INV-01 invitation lifecycle
WG-INV-02 token/expiry/replay security

WG-TEAM-01 Team lifecycle/membership
WG-SPACE-01 Space lifecycle/visibility

WG-GOV-01 ResourceKind / ResourceId
WG-GOV-02 PermissionAction
WG-GOV-03 PermissionRule
WG-GOV-04 ResourcePermission
WG-GOV-05 built-in role interpretation
WG-GOV-06 CustomRole
WG-GOV-07 WorkspacePolicy
WG-GOV-08 effective authorization
WG-GOV-09 request declaration/enforcement
WG-GOV-10 facts-provider handshake

WG-SHR-01 ShareLink lifecycle/security
WG-TPL-01 PermissionTemplate
WG-AUD-01 governance audit/security evidence

WG-X-01 Identity/Accounts
WG-X-02 WorkManagement
WG-X-03 Documents/Collaboration
WG-X-04 Billing/Entitlements
WG-X-05 Automation/Integrations
WG-X-06 Analytics/Reporting
```

# Workspace core

## 12. WGREQ001 — Workspace is a business boundary beneath Account

`Workspace` is an Account-scoped aggregate (`IAccountScoped`) and is the collaborative/product container beneath Account; it is not Account, Team, Space, Board or Subscription.

Workspaces owns this semantic and MUST preserve `AccountId` containment, stable `WorkspaceId`, and explicit lifecycle state without treating membership as equivalent to containment.

Acceptance evidence MUST be mapped in `workspace-governance.tests.md`; execution status belongs only in `workspace-governance.certification.md`.

## 13. WGREQ002 — stable Workspace identity

Workspace identity is the aggregate `Id`; name/slug are mutable lookup/display attributes and MUST NOT become foreign-key identity.

Workspaces owns this semantic and MUST preserve `AccountId` containment, stable `WorkspaceId`, and explicit lifecycle state without treating membership as equivalent to containment.


## 14. WGREQ003 — Account containment

Every Workspace carries `AccountId`. Cross-account Workspace access is invalid even when a WorkspaceId is guessed correctly.

Workspaces owns this semantic and MUST preserve `AccountId` containment, stable `WorkspaceId`, and explicit lifecycle state without treating membership as equivalent to containment.

Acceptance evidence MUST be mapped in `workspace-governance.tests.md`; execution status belongs only in `workspace-governance.certification.md`.

## 15. WGREQ004 — containment is not authorization

Workspaces owns this semantic and MUST preserve `AccountId` containment, stable `WorkspaceId`, and explicit lifecycle state without treating membership as equivalent to containment.


## 16. WGREQ005 — Workspace lifecycle

Current Domain supports Active/Archived plus soft-delete/restore semantics. New states require product/domain authority rather than documentation invention.

Workspaces owns this semantic and MUST preserve `AccountId` containment, stable `WorkspaceId`, and explicit lifecycle state without treating membership as equivalent to containment.


## 17. WGREQ006 — Workspace delete/archive effects

Workspaces owns this semantic and MUST preserve `AccountId` containment, stable `WorkspaceId`, and explicit lifecycle state without treating membership as equivalent to containment.


## 18. WGREQ007 — Workspace update

Workspaces owns this semantic and MUST preserve `AccountId` containment, stable `WorkspaceId`, and explicit lifecycle state without treating membership as equivalent to containment.


## 19. WGREQ008 — Workspace provisioning

Current source includes `ProvisionPersonalWorkspaceCommand` and an Identity-registration consumer. Provisioning must orchestrate owned writes without creating a cross-context mega-aggregate.

Workspaces owns this semantic and MUST preserve `AccountId` containment, stable `WorkspaceId`, and explicit lifecycle state without treating membership as equivalent to containment.


## 20. WGREQ009 — provisioning idempotency

Provisioning/retry must not duplicate Workspace, member or bootstrap grant state; idempotency evidence belongs to PLAN/TESTS.

Workspaces owns this semantic and MUST preserve `AccountId` containment, stable `WorkspaceId`, and explicit lifecycle state without treating membership as equivalent to containment.


## 21. WGREQ010 — Workspace settings

Workspaces owns this semantic and MUST preserve `AccountId` containment, stable `WorkspaceId`, and explicit lifecycle state without treating membership as equivalent to containment.


## 22. WGREQ011 — Workspace home/composition

Workspaces owns this semantic and MUST preserve `AccountId` containment, stable `WorkspaceId`, and explicit lifecycle state without treating membership as equivalent to containment.


# Membership

## 23. WGREQ012 — WorkspaceMember is a Workspace concept

`WorkspaceMember` currently stores `AccountId`, `WorkspaceId`, `UserId`, `WorkspaceRole`, `WorkspaceMemberStatus`; it does not own Identity credentials/profile.

Membership MUST remain Workspace-owned, reference stable upstream user identity, and be enforced with persisted/concurrency-safe invariants rather than UI-only checks.

Acceptance evidence MUST be mapped in `workspace-governance.tests.md`; execution status belongs only in `workspace-governance.certification.md`.

## 24. WGREQ013 — membership identity

Membership MUST remain Workspace-owned, reference stable upstream user identity, and be enforced with persisted/concurrency-safe invariants rather than UI-only checks.


## 25. WGREQ014 — one effective membership relation

Membership MUST remain Workspace-owned, reference stable upstream user identity, and be enforced with persisted/concurrency-safe invariants rather than UI-only checks.


## 26. WGREQ015 — membership lifecycle

Current member states are source-defined; Active/Suspended/Removed transitions and versioning must be preserved unless an approved product change exists.

Membership MUST remain Workspace-owned, reference stable upstream user identity, and be enforced with persisted/concurrency-safe invariants rather than UI-only checks.


## 27. WGREQ016 — add member authorization

Membership MUST remain Workspace-owned, reference stable upstream user identity, and be enforced with persisted/concurrency-safe invariants rather than UI-only checks.


## 28. WGREQ017 — remove member authorization

Membership MUST remain Workspace-owned, reference stable upstream user identity, and be enforced with persisted/concurrency-safe invariants rather than UI-only checks.


## 29. WGREQ018 — self-leave semantics

Membership MUST remain Workspace-owned, reference stable upstream user identity, and be enforced with persisted/concurrency-safe invariants rather than UI-only checks.


## 30. WGREQ019 — last-admin/owner protection

Owner safety is a Domain invariant (`WorkspaceOwnerRules`) and must be proven under concurrent downgrade/suspend/remove/transfer flows.

Membership MUST remain Workspace-owned, reference stable upstream user identity, and be enforced with persisted/concurrency-safe invariants rather than UI-only checks.

Acceptance evidence MUST be mapped in `workspace-governance.tests.md`; execution status belongs only in `workspace-governance.certification.md`.

## 31. WGREQ020 — member role association

Membership MUST remain Workspace-owned, reference stable upstream user identity, and be enforced with persisted/concurrency-safe invariants rather than UI-only checks.


## 32. WGREQ021 — member suspension/inactive behavior

Membership MUST remain Workspace-owned, reference stable upstream user identity, and be enforced with persisted/concurrency-safe invariants rather than UI-only checks.


## 33. WGREQ022 — Identity deactivation interaction

Membership MUST remain Workspace-owned, reference stable upstream user identity, and be enforced with persisted/concurrency-safe invariants rather than UI-only checks.


## 34. WGREQ023 — membership historical attribution

Membership MUST remain Workspace-owned, reference stable upstream user identity, and be enforced with persisted/concurrency-safe invariants rather than UI-only checks.


# Invitations

## 35. WGREQ024 — Invitation is not Membership

`WorkspaceInvitation` currently holds target email, role intent, token hash/version/generation, status, expiry and inviter; acceptance creates/activates membership through Application semantics, not by treating pending invite as membership.

Invitation state MUST be distinct from active membership. Token material, expiry, replay and accept/revoke races MUST be handled as security-sensitive state transitions.

Acceptance evidence MUST be mapped in `workspace-governance.tests.md`; execution status belongs only in `workspace-governance.certification.md`.

## 36. WGREQ025 — stable invitation identity

Invitation state MUST be distinct from active membership. Token material, expiry, replay and accept/revoke races MUST be handled as security-sensitive state transitions.


## 37. WGREQ026 — invite target

Invitation state MUST be distinct from active membership. Token material, expiry, replay and accept/revoke races MUST be handled as security-sensitive state transitions.


## 38. WGREQ027 — invitation role/access intent

Invitation state MUST be distinct from active membership. Token material, expiry, replay and accept/revoke races MUST be handled as security-sensitive state transitions.


## 39. WGREQ028 — invitation expiry

Current invitation expiry is persisted and checked on acceptance. Expired credentials cannot be accepted merely because a transport token is structurally valid.

Invitation state MUST be distinct from active membership. Token material, expiry, replay and accept/revoke races MUST be handled as security-sensitive state transitions.


## 40. WGREQ029 — invitation revoke

Invitation state MUST be distinct from active membership. Token material, expiry, replay and accept/revoke races MUST be handled as security-sensitive state transitions.


## 41. WGREQ030 — invitation replay

Repeated acceptance must not duplicate active membership or grants. Resend rotates token material/generation rather than reviving the old bearer secret.

Invitation state MUST be distinct from active membership. Token material, expiry, replay and accept/revoke races MUST be handled as security-sensitive state transitions.

Acceptance evidence MUST be mapped in `workspace-governance.tests.md`; execution status belongs only in `workspace-governance.certification.md`.

## 42. WGREQ031 — invitation race

Invitation state MUST be distinct from active membership. Token material, expiry, replay and accept/revoke races MUST be handled as security-sensitive state transitions.


## 43. WGREQ032 — invite enumeration resistance

Invitation state MUST be distinct from active membership. Token material, expiry, replay and accept/revoke races MUST be handled as security-sensitive state transitions.


## 44. WGREQ033 — invitation secret safety

Current Domain stores `InvitationTokenHash`, not a reusable raw token. Raw invitation secret must remain transport-only and secret-safe.

Invitation state MUST be distinct from active membership. Token material, expiry, replay and accept/revoke races MUST be handled as security-sensitive state transitions.


# Teams

## 45. WGREQ034 — Team belongs to Workspace

Team semantics remain inside Workspaces. Team membership MUST NOT become a second Workspace membership truth or a bypass around Governance policy.


## 46. WGREQ035 — Team membership

Team semantics remain inside Workspaces. Team membership MUST NOT become a second Workspace membership truth or a bypass around Governance policy.


## 47. WGREQ036 — Team lifecycle

Team semantics remain inside Workspaces. Team membership MUST NOT become a second Workspace membership truth or a bypass around Governance policy.


## 48. WGREQ037 — Team as authorization subject

`PermissionSubjectType.Team` exists, but Team-as-subject effective authorization must not be certified until facts/evaluation/application coverage is executed and proven.

Team semantics remain inside Workspaces. Team membership MUST NOT become a second Workspace membership truth or a bypass around Governance policy.


# Spaces and Workspace rules

## 49. WGREQ038 — Space semantics

Space/Workspace-rule behavior remains Workspace-owned only where it describes Workspace business state. Generic authorization or automation policy MUST NOT be duplicated here.


## 50. WGREQ039 — Space lifecycle

Space/Workspace-rule behavior remains Workspace-owned only where it describes Workspace business state. Generic authorization or automation policy MUST NOT be duplicated here.


## 51. WGREQ040 — Space authorization

Space/Workspace-rule behavior remains Workspace-owned only where it describes Workspace business state. Generic authorization or automation policy MUST NOT be duplicated here.


## 52. WGREQ041 — Space visibility

Current Space visibility values are `Private` and `Workspace`; visibility is a resource fact and not automatically a complete permission decision.

Space/Workspace-rule behavior remains Workspace-owned only where it describes Workspace business state. Generic authorization or automation policy MUST NOT be duplicated here.


## 53. WGREQ042 — Workspace Rules boundary

Space/Workspace-rule behavior remains Workspace-owned only where it describes Workspace business state. Generic authorization or automation policy MUST NOT be duplicated here.


## 54. WGREQ043 — no rule-engine duplication

`Workspaces/Rules/*` are Domain rule helpers. They MUST NOT evolve into a generic policy engine competing with Governance `PermissionRule`/policy evaluation or Automation rules.

Space/Workspace-rule behavior remains Workspace-owned only where it describes Workspace business state. Generic authorization or automation policy MUST NOT be duplicated here.


# Governance resource model

## 55. WGREQ044 — resource ownership remains with business context

The resource-owning bounded context owns lifecycle, containment and resource-specific facts. Governance consumes stable resource identity/kind and MUST NOT take ownership of foreign aggregates.


## 56. WGREQ045 — resource authorization category is semantic, not CLR type identity

Use stable semantic resource kinds (for example `work-management.board`, `documents.page`, `workspaces.workspace`) rather than CLR namespace/type names.

The resource-owning bounded context owns lifecycle, containment and resource-specific facts. Governance consumes stable resource identity/kind and MUST NOT take ownership of foreign aggregates.

Acceptance evidence MUST be mapped in `workspace-governance.tests.md`; execution status belongs only in `workspace-governance.certification.md`.

## 57. WGREQ046 — resource ID is opaque to Governance

The resource-owning bounded context owns lifecycle, containment and resource-specific facts. Governance consumes stable resource identity/kind and MUST NOT take ownership of foreign aggregates.


## 58. WGREQ047 — resource containment scope

The resource-owning bounded context owns lifecycle, containment and resource-specific facts. Governance consumes stable resource identity/kind and MUST NOT take ownership of foreign aggregates.


## 59. WGREQ048 — resource registration

Resource registration means the owning context publishes a stable resource kind, identity, actions and authorization facts contract. It does not mean copying the resource into Governance.

The resource-owning bounded context owns lifecycle, containment and resource-specific facts. Governance consumes stable resource identity/kind and MUST NOT take ownership of foreign aggregates.


## 60. WGREQ049 — resource registration ownership

The resource-owning bounded context owns lifecycle, containment and resource-specific facts. Governance consumes stable resource identity/kind and MUST NOT take ownership of foreign aggregates.


# Actions

## 61. WGREQ050 — Action is business meaningful

Current `PermissionAction` includes Workspace, Board, Page, Comment and Integration capabilities. Additions/removals are contract changes and must be traceable to the owning context.

Actions are stable business capabilities, not HTTP verbs or handler names. Their identity is part of the Governance contract and changes require compatibility review.

Acceptance evidence MUST be mapped in `workspace-governance.tests.md`; execution status belongs only in `workspace-governance.certification.md`.

## 62. WGREQ051 — action uniqueness

Actions are stable business capabilities, not HTTP verbs or handler names. Their identity is part of the Governance contract and changes require compatibility review.


## 63. WGREQ052 — action versioning/migration

Actions are stable business capabilities, not HTTP verbs or handler names. Their identity is part of the Governance contract and changes require compatibility review.


## 64. WGREQ053 — no HTTP verb equivalence

Actions are stable business capabilities, not HTTP verbs or handler names. Their identity is part of the Governance contract and changes require compatibility review.


# Permissions

## 65. WGREQ054 — Permission connects action to authorization semantics

Permission semantics are Governance-owned and MUST evaluate within explicit Account/Workspace/resource scope. Default behavior is deny/fail-closed unless an accepted rule grants access.


## 66. WGREQ055 — permission persistence stability

Permission semantics are Governance-owned and MUST evaluate within explicit Account/Workspace/resource scope. Default behavior is deny/fail-closed unless an accepted rule grants access.


## 67. WGREQ056 — permission evaluation scope

Permission semantics are Governance-owned and MUST evaluate within explicit Account/Workspace/resource scope. Default behavior is deny/fail-closed unless an accepted rule grants access.


## 68. WGREQ057 — deny semantics

Explicit deny at the highest applicable priority wins over allow at that priority in the current `AccessPolicyEngine`; changes require a deliberate authorization-policy decision and tests.

Permission semantics are Governance-owned and MUST evaluate within explicit Account/Workspace/resource scope. Default behavior is deny/fail-closed unless an accepted rule grants access.

Acceptance evidence MUST be mapped in `workspace-governance.tests.md`; execution status belongs only in `workspace-governance.certification.md`.

## 69. WGREQ058 — default-deny principle

Unknown/unsupported protected operations must deny rather than fall through to broad role-based allow.

Permission semantics are Governance-owned and MUST evaluate within explicit Account/Workspace/resource scope. Default behavior is deny/fail-closed unless an accepted rule grants access.


## 70. WGREQ059 — permission cache

Permission semantics are Governance-owned and MUST evaluate within explicit Account/Workspace/resource scope. Default behavior is deny/fail-closed unless an accepted rule grants access.


# Permission rules

## 71. WGREQ060 — PermissionRule ownership

Current `PermissionRule` is persisted Governance state with scope/subject/action/effect/condition/priority/time window/status. Its evaluator semantics must remain singular and deterministic.

PermissionRule evaluation MUST be deterministic, time/scope aware, and based only on trusted inputs. Competing rule engines are prohibited.


## 72. WGREQ061 — deterministic rule evaluation

PermissionRule evaluation MUST be deterministic, time/scope aware, and based only on trusted inputs. Competing rule engines are prohibited.


## 73. WGREQ062 — rule input trust

PermissionRule evaluation MUST be deterministic, time/scope aware, and based only on trusted inputs. Competing rule engines are prohibited.


# Roles

## 74. WGREQ063 — Role is a Governance concept

Built-in Workspace roles currently are `Guest`, `Member`, `Admin`, `Owner`; `CustomRole` is a separate Governance aggregate. Do not collapse them by string convention alone.

Role vocabulary and role-to-permission semantics are Governance-owned. Workspace membership may reference role state but MUST NOT independently redefine permission meaning.

Acceptance evidence MUST be mapped in `workspace-governance.tests.md`; execution status belongs only in `workspace-governance.certification.md`.

## 75. WGREQ064 — built-in roles

Role vocabulary and role-to-permission semantics are Governance-owned. Workspace membership may reference role state but MUST NOT independently redefine permission meaning.


## 76. WGREQ065 — built-in role identity stability

Role vocabulary and role-to-permission semantics are Governance-owned. Workspace membership may reference role state but MUST NOT independently redefine permission meaning.


## 77. WGREQ066 — role assignment

Role vocabulary and role-to-permission semantics are Governance-owned. Workspace membership may reference role state but MUST NOT independently redefine permission meaning.


## 78. WGREQ067 — role assignment scope

Role vocabulary and role-to-permission semantics are Governance-owned. Workspace membership may reference role state but MUST NOT independently redefine permission meaning.


## 79. WGREQ068 — custom roles

`CustomRole` exists in Domain and persistence, but current Application/API feature surface is not equivalent to full custom-role administration. PLAN must classify the missing delivery layers before claiming capability complete.

Role vocabulary and role-to-permission semantics are Governance-owned. Workspace membership may reference role state but MUST NOT independently redefine permission meaning.


## 80. WGREQ069 — custom-role mutation

Role vocabulary and role-to-permission semantics are Governance-owned. Workspace membership may reference role state but MUST NOT independently redefine permission meaning.


## 81. WGREQ070 — role deletion

Role vocabulary and role-to-permission semantics are Governance-owned. Workspace membership may reference role state but MUST NOT independently redefine permission meaning.


# Policies

## 82. WGREQ071 — Policy semantics

`WorkspacePolicy` currently models guest/resource/sharing policy values. Domain existence is not evidence that these policies are integrated into every effective authorization decision.

Policy composition MUST be deterministic and versionable. Current Domain policy types are source evidence, not proof that an Application/API administration flow is complete.


## 83. WGREQ072 — policy evaluation

Policy composition MUST be deterministic and versionable. Current Domain policy types are source evidence, not proof that an Application/API administration flow is complete.


## 84. WGREQ073 — policy composition

Policy composition MUST be deterministic and versionable. Current Domain policy types are source evidence, not proof that an Application/API administration flow is complete.


## 85. WGREQ074 — policy versioning

Policy composition MUST be deterministic and versionable. Current Domain policy types are source evidence, not proof that an Application/API administration flow is complete.


## 86. WGREQ075 — policy failure

Policy composition MUST be deterministic and versionable. Current Domain policy types are source evidence, not proof that an Application/API administration flow is complete.


# Resource permissions

## 87. WGREQ076 — ResourcePermission meaning

`ResourcePermission` currently captures `ResourceKind`, `ResourceId`, `SubjectType`, `SubjectId`, `PermissionLevel`, effect/condition/priority and soft-delete lifecycle.

ResourcePermission is explicit Governance grant state scoped by Account, Workspace, resource kind/id and subject. Grant/revoke/level semantics must respect authority ceilings and resource ownership.

Acceptance evidence MUST be mapped in `workspace-governance.tests.md`; execution status belongs only in `workspace-governance.certification.md`.

## 88. WGREQ077 — direct grant scope

ResourcePermission is explicit Governance grant state scoped by Account, Workspace, resource kind/id and subject. Grant/revoke/level semantics must respect authority ceilings and resource ownership.


## 89. WGREQ078 — inheritance

ResourcePermission is explicit Governance grant state scoped by Account, Workspace, resource kind/id and subject. Grant/revoke/level semantics must respect authority ceilings and resource ownership.


## 90. WGREQ079 — revocation

Revoke is a security-state change. Stale caches/projections must not keep revoked permission effective.

ResourcePermission is explicit Governance grant state scoped by Account, Workspace, resource kind/id and subject. Grant/revoke/level semantics must respect authority ceilings and resource ownership.


# Effective authorization and enforcement

## 91. WGREQ080 — one effective authorization decision

One Application request gets one authoritative access decision before protected handler effects. Secondary helpers may supply facts, not parallel final decisions.

Protected use cases MUST converge on the canonical Application access-control pipeline; ad-hoc endpoint/handler role checks cannot become a parallel authorization authority.

Acceptance evidence MUST be mapped in `workspace-governance.tests.md`; execution status belongs only in `workspace-governance.certification.md`.

## 92. WGREQ081 — authentication vs authorization

Authentication establishes principal identity; authorization establishes permission for Account/Workspace/resource/action. A valid JWT/session/API token alone is never sufficient resource authorization.

Protected use cases MUST converge on the canonical Application access-control pipeline; ad-hoc endpoint/handler role checks cannot become a parallel authorization authority.


## 93. WGREQ082 — membership precondition

Protected use cases MUST converge on the canonical Application access-control pipeline; ad-hoc endpoint/handler role checks cannot become a parallel authorization authority.


## 94. WGREQ083 — effective permission sources

Effective permission sources currently include account/workspace membership, permission rules, explicit resource permissions and resource-owned visibility/membership facts. Future sources require explicit precedence.

Protected use cases MUST converge on the canonical Application access-control pipeline; ad-hoc endpoint/handler role checks cannot become a parallel authorization authority.


## 95. WGREQ084 — precedence

Current engine is deny-sensitive at the minimum rule priority and applies explicit authority checks for grant/revoke/resource management. This precedence must be captured by tests rather than implied by UI behavior.

Protected use cases MUST converge on the canonical Application access-control pipeline; ad-hoc endpoint/handler role checks cannot become a parallel authorization authority.


## 96. WGREQ085 — fail closed

Protected use cases MUST converge on the canonical Application access-control pipeline; ad-hoc endpoint/handler role checks cannot become a parallel authorization authority.


## 97. WGREQ086 — resource-not-found privacy

For restricted resources, unauthorized visibility may normalize to NotFound to avoid resource enumeration where the canonical policy requires it.

Protected use cases MUST converge on the canonical Application access-control pipeline; ad-hoc endpoint/handler role checks cannot become a parallel authorization authority.


## 98. WGREQ087 — Application declares resource/action requirement

Protected Application requests declare security through request descriptors/interfaces such as `IRequirePermission`; resource/action context must be explicit and validated.

Protected use cases MUST converge on the canonical Application access-control pipeline; ad-hoc endpoint/handler role checks cannot become a parallel authorization authority.


## 99. WGREQ088 — pipeline owns enforcement

At the audited baseline, canonical enforcement is `AccessControlBehavior<TRequest,TResponse>` using `IExecutionContextReader`, `IAccessFactsProvider`, and `IAccessPolicyEvaluator` before `next()`.

Protected use cases MUST converge on the canonical Application access-control pipeline; ad-hoc endpoint/handler role checks cannot become a parallel authorization authority.

Acceptance evidence MUST be mapped in `workspace-governance.tests.md`; execution status belongs only in `workspace-governance.certification.md`.

## 100. WGREQ089 — handler-local role checks are not canonical enforcement

A handler may enforce Domain invariants but must not implement a second authorization engine with `if (role == ...)` as canonical request protection.

Protected use cases MUST converge on the canonical Application access-control pipeline; ad-hoc endpoint/handler role checks cannot become a parallel authorization authority.


## 101. WGREQ090 — API endpoint does not own business authorization

Minimal API endpoints may map host metadata and contracts, but business permission outcome is owned by the Application security pipeline.

Protected use cases MUST converge on the canonical Application access-control pipeline; ad-hoc endpoint/handler role checks cannot become a parallel authorization authority.


## 102. WGREQ091 — background authorization
An operation does not become trusted merely because it runs outside HTTP.

The principal model distinguishes ordinary background/message execution from an explicitly classified trusted system request.

An `ISystemInternalRequest` MAY use the repository's approved System-principal path, including the current `AccessPolicyEngine` System fast-path, only when the use case is intentionally classified as System and the surrounding runtime contract supplies the required trusted scope and operation metadata.

Representative approved pattern:

```text
Identity committed event
→ MassTransit tenant envelope
→ deduplication
→ WorkspaceProvisioningConsumer
→ ProvisionPersonalWorkspaceCommand
   : ISystemInternalRequest
   : ISystemOperation
   : IMessageTriggeredRequest
   : IGlobalRequest
→ Workspaces-owned mutation
```

For that class of request:

- absence of an HTTP user does not make the operation invalid;
- the System principal is explicit in the request contract rather than inferred from execution environment;
- tenant/account information is derived from a trusted event/envelope or other approved source;
- idempotency/deduplication and system-operation audit metadata remain mandatory where the flow requires them;
- System classification MUST NOT be used to convert an ordinary authenticated/resource request into an authorization bypass.

Ordinary authenticated/background work that represents a User MUST still satisfy the normal `AccessControlBehavior → AccessFacts → AccessPolicyEngine` authorization contract.

The frozen `SystemInternalRequest_WithoutUser_BypassesAuth_CallsHandler` characterization is therefore an approved special contract, not a general rule that background work bypasses security.


## 103. WGREQ092 — authorization idempotency/order interaction

Protected use cases MUST converge on the canonical Application access-control pipeline; ad-hoc endpoint/handler role checks cannot become a parallel authorization authority.


# Resource-team handshake

## 104. WGREQ093 — downstream resource registration contract

Downstream P3+ requests must be able to state `Actor + Account + Workspace + ResourceKind + ResourceId + PermissionAction` (or the canonical descriptor equivalent) without importing Governance persistence types.

Downstream resource teams publish stable resource/action/facts contracts. Governance evaluates policy but does not query/mutate their private persistence as a normal integration contract.

Acceptance evidence MUST be mapped in `workspace-governance.tests.md`; execution status belongs only in `workspace-governance.certification.md`.

## 105. WGREQ094 — Governance does not invent WorkManagement semantics

WorkManagement owns Board/BoardItem lifecycle, visibility and action meaning. Governance only evaluates policy over published facts/actions.

Downstream resource teams publish stable resource/action/facts contracts. Governance evaluates policy but does not query/mutate their private persistence as a normal integration contract.


## 106. WGREQ095 — product context does not invent role names

Downstream resource teams publish stable resource/action/facts contracts. Governance evaluates policy but does not query/mutate their private persistence as a normal integration contract.


## 107. WGREQ096 — resource lookup contract

Resource facts provider contracts must be owner-safe. The current code already composes Documents page facts through the Documents public authorization contract; new contexts must follow the same ownership discipline or an accepted architecture decision.

Downstream resource teams publish stable resource/action/facts contracts. Governance evaluates policy but does not query/mutate their private persistence as a normal integration contract.


# Share links

## 108. WGREQ097 — ShareLink is a Governance access mechanism

`ShareLink` is Governance state over a foreign/current resource reference; it does not transfer ownership of that resource into Governance.

Share links are bearer-capability security objects. Raw reusable secrets MUST not be persisted or logged; scope, expiry, revocation and least privilege are mandatory.

Acceptance evidence MUST be mapped in `workspace-governance.tests.md`; execution status belongs only in `workspace-governance.certification.md`.

## 109. WGREQ098 — share-link secret

Current source stores `ShareLinkTokenHash`; raw share tokens must not be logged/persisted in ordinary state or returned by list APIs.

Share links are bearer-capability security objects. Raw reusable secrets MUST not be persisted or logged; scope, expiry, revocation and least privilege are mandatory.


## 110. WGREQ099 — share-link scope

Share links are bearer-capability security objects. Raw reusable secrets MUST not be persisted or logged; scope, expiry, revocation and least privilege are mandatory.


## 111. WGREQ100 — share-link least privilege

Share links are bearer-capability security objects. Raw reusable secrets MUST not be persisted or logged; scope, expiry, revocation and least privilege are mandatory.


## 112. WGREQ101 — share-link expiry

Public share links currently require expiry in Domain. Any relaxation is a security/product decision, not a coding convenience.

Share links are bearer-capability security objects. Raw reusable secrets MUST not be persisted or logged; scope, expiry, revocation and least privilege are mandatory.


## 113. WGREQ102 — share-link revocation

Share links are bearer-capability security objects. Raw reusable secrets MUST not be persisted or logged; scope, expiry, revocation and least privilege are mandatory.


## 114. WGREQ103 — share-link enumeration

Share links are bearer-capability security objects. Raw reusable secrets MUST not be persisted or logged; scope, expiry, revocation and least privilege are mandatory.


## 115. WGREQ104 — share-link audit

Share links are bearer-capability security objects. Raw reusable secrets MUST not be persisted or logged; scope, expiry, revocation and least privilege are mandatory.


# Audit and security events

## 116. WGREQ105 — Audit ownership

Audit/security evidence must preserve Account/Workspace/actor/resource/action correlation without becoming a secret sink or a mutable substitute for source business state.


## 117. WGREQ106 — immutable history semantics

Audit/security evidence must preserve Account/Workspace/actor/resource/action correlation without becoming a secret sink or a mutable substitute for source business state.


## 118. WGREQ107 — audit actor/resource/action

Audit/security evidence must preserve Account/Workspace/actor/resource/action correlation without becoming a secret sink or a mutable substitute for source business state.


## 119. WGREQ108 — security events

Audit/security evidence must preserve Account/Workspace/actor/resource/action correlation without becoming a secret sink or a mutable substitute for source business state.


## 120. WGREQ109 — no secret material

Audit/security evidence must preserve Account/Workspace/actor/resource/action correlation without becoming a secret sink or a mutable substitute for source business state.


## 121. WGREQ110 — audit tenant isolation

Audit/security evidence must preserve Account/Workspace/actor/resource/action correlation without becoming a secret sink or a mutable substitute for source business state.


# Governance templates

## 122. WGREQ111 — template meaning

`PermissionTemplate` supports System and Workspace scope in Domain. System templates are immutable through workspace mutation paths.

Permission templates are Governance-owned reusable definitions. System/workspace scope and application semantics must remain explicit and must not silently mutate unrelated authorization state.


## 123. WGREQ112 — template application

Permission templates are Governance-owned reusable definitions. System/workspace scope and application semantics must remain explicit and must not silently mutate unrelated authorization state.


## 124. WGREQ113 — template versioning

Permission templates are Governance-owned reusable definitions. System/workspace scope and application semantics must remain explicit and must not silently mutate unrelated authorization state.


# Cross-context contracts

## 125. WGREQ114 — consume stable Actor/User only

Workspaces/Governance consume stable Identity contract.

They MUST NOT reference:

- password;
- OAuth tokens;
- Session EF entity;
- MFA secrets.

## 126. WGREQ115 — consume stable Account

Workspace Account containment must use P1 canonical Account ID.

No second tenant identifier may be invented.

## 127. WGREQ116 — Account disabled behavior

When upstream Account becomes disabled/inactive according to contract, Workspace/Governance protected operations must fail according to lifecycle policy.

## 128. WGREQ117 — User disabled behavior

When Identity Actor becomes invalid, Governance must not continue authorizing based solely on stale membership cache.

# WorkManagement integration

## 129. WGREQ118 — Board resource registration

WorkManagement must be able to register/declare Board resources/actions without Governance depending on WorkManagement internals.

## 130. WGREQ119 — BoardItem resource registration

If BoardItem has independently governable actions, the contract must be explicit.

Do not assume every nested entity requires independent ACL.

## 131. WGREQ120 — WorkManagement staged entry gates

P3 is opened in two stages so Governance does not become an unnecessary bottleneck for resource-owned
Domain/Data work while protected execution remains safe.

**P3-A — Domain/Data parallelization gate** may open when:

```text
Workspace identity + Account containment are stable enough for downstream references (D4+)
Workspace/resource containment contract is stable (D4+)
resource authorization category + resource-owned Action vocabulary are stable (D4+)
WorkManagement can model its own invariants without importing Governance internals
```

At P3-A, WorkManagement may implement Domain/Data transactional core and unprotected internal fixtures,
but MUST NOT release protected Application/API operations that rely on incomplete authorization semantics.

**P3-B — Protected Application/API gate** requires:

```text
Workspace / Account containment release-ready
WorkspaceMember baseline D4+
resource category/action contract release-ready
Permission semantics D5 for the representative slice
built-in role policy D4+
existing central authorization enforcement D5
resource facts provider/lookup boundary proven and source ownership explicitly classified
representative allow / deny / cross-tenant deny proven
```

P3 product release/certification uses P3-B, not P3-A.

## 132. WGREQ121 — WorkManagement local ownership invariant

WorkManagement may still enforce Domain invariants such as valid state transition.

Governance authorization must not absorb WorkManagement business rules.

# Documents/Collaboration integration

## 133. WGREQ122 — Page/Document resource

Documents defines Page/Document action semantics.

Governance evaluates access.

## 134. WGREQ123 — comment authorization target

Collaboration authorization should depend on the target resource access contract rather than owning target resource tables.

# Billing integration

## 135. WGREQ124 — billing administration

Billing defines business actions such as managing subscription/payment methods.

Governance controls which Account actors may perform those actions.

## 136. WGREQ125 — entitlement vs authorization

Billing Entitlement answers:

```text
is this product capability available under plan/usage?
```

Governance answers:

```text
may this actor perform this action?
```

They MUST NOT be conflated.

An operation may require both.

# Automation / Integrations

## 137. WGREQ126 — automation actor/authorization

Automation execution may act on behalf of a User/system principal according to approved actor model.

Governance must evaluate business authorization where appropriate.

## 138. WGREQ127 — Integration administration

Creating/managing provider connections is a governable resource/action owned semantically by Integrations and authorized by Governance.

# Analytics

## 139. WGREQ128 — governance analytics facts

Analytics may consume derived facts such as:

- membership counts;
- role assignments;
- authorization/security events;

subject to privacy.

Analytics MUST NOT use Governance private tables as an uncontrolled source of truth.

# Data ownership

## 140. WGREQ129 — Workspace persistence private

Other contexts MUST NOT mutate Workspace/Member/Invitation/Team/Space tables directly.

## 141. WGREQ130 — Governance persistence private

Other contexts MUST NOT mutate Role/Permission/Policy/ResourcePermission/ShareLink tables directly.

## 142. WGREQ131 — no dual membership truth

There MUST NOT be one Workspace membership model in Workspaces and another canonical membership model in Governance.

Governance references/authorizes membership; Workspaces owns membership state.

## 143. WGREQ132 — no dual role truth

Role semantics must have one canonical owner in Governance.

Workspace/Identity/Product contexts must not maintain their own business role enums that independently authorize the same actions.

## 144. WGREQ133 — no private cross-context joins as contract

Same physical database does not authorize Governance to join private WorkManagement/Documents/Billing tables as its public business contract.

# API

## 145. WGREQ134 — Workspace API categories

Workspace APIs may include release-scoped:

```text
Workspace lifecycle
Members
Invitations
Teams
Spaces
Settings
Provisioning
WorkspaceHome
```

Exact endpoints follow current API architecture.

## 146. WGREQ135 — Governance API categories

Governance APIs may include:

```text
Roles
Permissions
Policies
ResourcePermissions
PermissionRules
ShareLinks
AuditLogs
SecurityEvents
```

Only release-scoped/canonical surfaces are implemented.

## 147. WGREQ136 — API error taxonomy

API must distinguish according to canonical policy:

```text
unauthenticated
forbidden
not found/privacy
validation
conflict
expired/revoked invitation/share link
invalid lifecycle
```

## 148. WGREQ137 — no secret response leakage

Invitation/share-link secret material must be minimized according to issuance/read lifecycle.

## 149. WGREQ138 — OpenAPI compatibility

API changes update OpenAPI/generated contract evidence where required.

# Events

## 150. WGREQ139 — Workspace event ownership

Workspaces owns facts such as:

```text
Workspace lifecycle
membership lifecycle
invitation lifecycle
Team/Space lifecycle
```

where cross-context consumers need them.

## 151. WGREQ140 — Governance event ownership

Governance owns facts such as:

```text
role/policy/permission change
share link lifecycle
security/governance event
```

where externally meaningful.

## 152. WGREQ141 — event scope

Cross-context events must include enough stable:

```text
Account
Workspace
subject/resource identity
```

to consume without private table access.

## 153. WGREQ142 — event security

Events must not expose privileged secrets or unnecessary personal data.

## 154. WGREQ143 — event compatibility

Once downstream consumers rely on D4/D5 events, breaking changes require migration/rollout coordination.

# Concurrency

## 155. WGREQ144 — membership uniqueness concurrency

Concurrent add/accept operations must not create duplicate active membership.

## 156. WGREQ145 — last-admin concurrency

Concurrent remove/demote operations cannot leave a Workspace in an invalid no-admin state if such invariant exists.

## 157. WGREQ146 — invitation accept/revoke race

Final state must be deterministic and cannot grant access after authoritative revocation.

## 158. WGREQ147 — role assignment concurrency

Concurrent assignment/removal must not create duplicate or contradictory effective authorization records.

## 159. WGREQ148 — policy update concurrency

If policies are versioned/edited concurrently, stale writes must be handled according to current concurrency architecture.

## 160. WGREQ149 — share-link revoke race

A revoked link must not be reactivated by stale writes/caches.

# Security

## 161. WGREQ150 — authorization is high sensitivity

Changes to:

- resource/action semantics;
- roles;
- permissions;
- policies;
- share links;
- membership admin;
- invitation admin;

require negative/security verification.

## 162. WGREQ151 — fail closed on evaluation failure

An authorization evaluation exception/missing rule must not become allow.

## 163. WGREQ152 — tenant spoofing resistance

Actor/Account/Workspace/resource scope must agree.

Client-provided Workspace/Resource IDs cannot cross tenants.

## 164. WGREQ153 — stale authorization cache

Revoked membership/role/permission/share link must stop granting access within accepted security window.

## 165. WGREQ154 — privilege escalation resistance

Users cannot:

- assign themselves a stronger role;
- create a grant above their authority;
- invite with stronger access than permitted;
- mutate policies they cannot manage;
- use Team membership to escape Workspace membership.

## 166. WGREQ155 — authorization decision privacy

Denial/error output should not leak sensitive policy structure or hidden resource existence beyond canonical API policy.

## 167. WGREQ156 — share-link bearer security

Share links are bearer credentials where applicable and require:

- entropy;
- safe comparison/verification;
- no ordinary log exposure;
- revocation/expiry enforcement.

## 168. WGREQ157 — audit access security

Audit/security-event APIs require explicit Governance permission.

# Reliability

## 169. WGREQ158 — provisioning partial failure

If Workspace provisioning orchestrates Account/Workspace/Governance bootstrap, partial failure behavior must be explicit.

The system must not report a fully provisioned Workspace when required authoritative pieces failed.

## 170. WGREQ159 — authorization dependency failure

If required resource/policy data cannot be resolved, protected operations fail safely.

## 171. WGREQ160 — event publication failure

State-changing Governance/Workspace operations that require integration events must follow existing transactional outbox/delivery architecture.

## 172. WGREQ161 — cache outage

Authorization cache failure must have an explicit safe fallback.

Do not default to allow.

# Observability

## 173. WGREQ162 — authorization traceability

A denied/allowed protected operation should be diagnosable with safe metadata:

```text
correlation
actor
Account
Workspace
resource kind/id
action
decision
policy/permission category where safe
```

## 174. WGREQ163 — membership/admin change observability

Critical membership/role/policy/share-link changes should be observable/auditable.

## 175. WGREQ164 — no secret telemetry

Logs/traces must not contain invitation/share-link bearer secrets or authentication credentials.

## 176. WGREQ165 — denial metrics

The system may expose safe aggregate authorization-denial/security metrics through existing observability mechanisms.

No new vendor/framework is implied.

# Performance

## 177. WGREQ166 — authorization is a hot path

Effective authorization may execute on most protected product requests.

It must avoid unbounded:

- cross-context database calls;
- policy scans;
- role scans;
- recursive resource traversal.

## 178. WGREQ167 — membership lookup efficiency

Workspace membership resolution should use appropriate indexing/cache while preserving revocation correctness.

## 179. WGREQ168 — resource permission lookup efficiency

Resource-level permissions must be indexed/scoped by relevant tenant/resource/subject dimensions.

## 180. WGREQ169 — cache correctness over raw speed

Any authorization cache must specify:

```text
key dimensions
invalidation triggers
maximum stale security window
tenant isolation
```

## 181. WGREQ170 — list/query scaling

Member/audit/permission lists need pagination/filter behavior consistent with API quality standards.

# Migration

## 182. WGREQ171 — membership migration

Changing membership identity/state schema requires preserving:

- User references;
- Workspace references;
- role assignments;
- historical audit.

## 183. WGREQ172 — resource/action migration

Renaming resource kinds/actions is a contract migration, not merely code refactor.

Must update:

- persisted permissions;
- roles;
- policies;
- share links;
- downstream declarations;
- tests.

## 184. WGREQ173 — role/permission migration

Changing built-in role semantics requires explicit impact analysis for existing Workspaces.

## 185. WGREQ174 — policy schema migration

Persisted policy changes require compatibility/backfill/versioning.

## 186. WGREQ175 — invitation/share-link secret migration

Changing token/hash format requires validity/rotation/revocation policy for existing links.

## 187. WGREQ176 — clean/upgrade database

Every schema-affecting P2 delivery requires both fresh DB and supported upgrade evidence.

## 188. WGREQ177 — no pending model changes

P2 completion cannot suppress EF pending-model warnings.

# P2 core producer contract

## 189. P2 mandatory core

The critical P2 core is:

```text
Workspace identity
Account→Workspace containment
WorkspaceMember baseline
resource kind/resource contract
Action contract
Permission semantics
built-in role baseline
central authorization integration
```

## 190. P2 secondary scope

The following may continue after P3 WorkManagement starts:

```text
advanced invitations
Teams advanced features
Spaces advanced features
custom roles
advanced policies
advanced share links
governance templates
advanced audit UX/query
```

provided they do not change the already stable P2 producer contract.

## 191. WGREQ178 — Workspace D5 gate

Workspace reaches D5 when:

- stable identity;
- Account containment;
- lifecycle baseline;
- membership baseline;
- tenant isolation;
- downstream resource containment contract

are verified.

## 192. WGREQ179 — Resource/Action D5 gate

Resource/action contract reaches D5 when WorkManagement can register/declare a resource/action without Governance/private coupling.

## 193. WGREQ180 — Permission D5 gate

Permission semantics reach D5 when:

- stable identifier/meaning;
- tenant scope;
- role mapping;
- effective decision;
- revocation;
- migration

are verified.

## 194. WGREQ181 — built-in Role D4+ gate

Built-in roles must be sufficiently verified for initial product actions.

Custom roles are not required to open P3 unless product says otherwise.

## 195. WGREQ182 — authorization integration D5 gate

Protected representative operations must be rejected before handler side effects when authorization fails.

## 196. P2 → P3 exit contract

The P2→P3 dependency is intentionally non-serial:

```text
P3-A Domain/Data work:
  Workspace + containment + resource category/action contract D4+

P3-B protected Application/API and release:
  Workspace D5
  Account→Workspace containment D5
  WorkspaceMember D4+
  resource category/resource contract D5
  Action D5
  Permission D5 for representative protected slice
  Built-in role policy D4+
  current AccessControlBehavior + IAccessFactsProvider + IAccessPolicyEvaluator/AccessPolicyEngine path D5
  resource-owner facts-provider ownership proven
  representative WorkManagement allow/deny/cross-tenant proof
```

Secondary Governance features do not block P3-B unless they are part of the initial product contract.

# Source-synchronization and architecture-closure amendments

## 197. WGREQ183 — current module-first Application topology is authoritative for touched use cases

The audited Application topology is `Notrelix.Application/Features/Workspaces` and `.../Governance`. New/touched P2 use cases follow this module-first topology; deprecated legacy paths are not extended.

This amendment synchronizes the execution contract with the current `develop` architecture and is mandatory for all touched P2 work.

Acceptance evidence MUST be mapped in `workspace-governance.tests.md`; execution status belongs only in `workspace-governance.certification.md`.

## 198. WGREQ184 — bounded-context DbContext interfaces are local persistence abstractions, not public cross-context contracts

`IWorkspaceDbContext`/`IGovernanceDbContext` may remain as current owner-local persistence abstractions under the documented EF exception. They MUST NOT be re-exported through Public or injected into foreign-context adapters/handlers.

This amendment synchronizes the execution contract with the current `develop` architecture and is mandatory for all touched P2 work.

Acceptance evidence MUST be mapped in `workspace-governance.tests.md`; execution status belongs only in `workspace-governance.certification.md`.

## 199. WGREQ185 — AccessControlBehavior is the canonical Application enforcement seam

Do not introduce a second `AuthorizationBehavior`, `IAuthorizationDecisionStore`, endpoint filter or handler-level engine. The audited canonical seam is `AccessControlBehavior` + request descriptor + execution context + facts provider + policy evaluator.

This amendment synchronizes the execution contract with the current `develop` architecture and is mandatory for all touched P2 work.

Acceptance evidence MUST be mapped in `workspace-governance.tests.md`; execution status belongs only in `workspace-governance.certification.md`.

## 200. WGREQ186 — authorization facts are source-owned and composed without expanding foreign persistence coupling

`PostgresAccessFactsProvider`/`AccessFactsQuery` is current runtime evidence, not a license to add arbitrary foreign-table reads. New resource facts should come from producer-owned Public contracts/adapters where architecture requires that ownership split.

This amendment synchronizes the execution contract with the current `develop` architecture and is mandatory for all touched P2 work.

Acceptance evidence MUST be mapped in `workspace-governance.tests.md`; execution status belongs only in `workspace-governance.certification.md`.

## 201. WGREQ187 — RLS remains defense-in-depth and cannot replace Application authorization

PostgreSQL RLS and request data-session context constrain persistence as defense-in-depth. A row being visible under RLS does not mean the actor is authorized for a business action.

This amendment synchronizes the execution contract with the current `develop` architecture and is mandatory for all touched P2 work.

Acceptance evidence MUST be mapped in `workspace-governance.tests.md`; execution status belongs only in `workspace-governance.certification.md`.

## 202. WGREQ188 — Domain presence does not imply Application/API delivery completeness

Domain-only capabilities (notably CustomRole/WorkspacePolicy/PermissionTemplate and parts of ShareLink/ResourcePermission lifecycle) must be classified `DOMAIN_PRESENT`, `APPLICATION_PRESENT`, `API_PRESENT`, `TESTED`, or `DELIVERED`; no layer may infer another.

This amendment synchronizes the execution contract with the current `develop` architecture and is mandatory for all touched P2 work.

Acceptance evidence MUST be mapped in `workspace-governance.tests.md`; execution status belongs only in `workspace-governance.certification.md`.

## 203. WGREQ189 — source evidence and certification status are separate

SPEC/PLAN may record source presence. Only CERTIFICATION may declare VERIFIED/STABLE after exact-SHA commands, test counts, CI evidence and named contract proof are recorded.

This amendment synchronizes the execution contract with the current `develop` architecture and is mandatory for all touched P2 work.

Acceptance evidence MUST be mapped in `workspace-governance.tests.md`; execution status belongs only in `workspace-governance.certification.md`.

## 204. WGREQ190 — P2 handoff requires exact-SHA evidence and named downstream contract proof

P2 opens protected P3 release only after the candidate SHA proves Workspace/containment/membership + resource/action + permission/built-in-role + central access-control handshake against a representative WorkManagement resource.

This amendment synchronizes the execution contract with the current `develop` architecture and is mandatory for all touched P2 work.

Acceptance evidence MUST be mapped in `workspace-governance.tests.md`; execution status belongs only in `workspace-governance.certification.md`.


# Cross-cutting acceptance criteria

## Functional acceptance

### WGAC001 — Workspace

Workspace creation/read/update/lifecycle must preserve canonical Account containment and source-defined Domain invariants.

### WGAC002 — Membership

Membership operations must preserve uniqueness, owner safety, inactive-member access removal and historical attribution.

### WGAC003 — Invitation

Invitation create/resend/accept/decline/revoke/expiry paths must be token-safe, replay-safe and race-aware.

### WGAC004 — Resource/action handshake

At least one representative downstream resource must prove stable ResourceKind/ResourceId/PermissionAction declaration and owner-provided facts.

### WGAC005 — Permission/effective authorization

Allowed and denied paths must converge on one Application access-control seam with deterministic Governance policy.

### WGAC006 — Built-in roles

Guest/Member/Admin/Owner behavior used by protected P2/P3 flows must be explicit, testable and free from endpoint/handler-local parallel policy.

### WGAC007 — Resource permissions

Grant/revoke/list plus authority-ceiling behavior must be scoped to the correct Account/Workspace/resource and fail closed for stale/missing target authority.

### WGAC008 — Share links

Where exposed, share-link creation/use/disable/expiry must preserve secret safety, bounded scope, revocation and resource existence/privacy semantics.

### WGAC009 — Cross-context ownership

No downstream consumer may require `IWorkspaceDbContext`, `IGovernanceDbContext`, Governance entities or private tables as its public integration contract.

### WGAC010 — P3 handoff

WorkManagement can consume stable Workspace/Governance contracts without owning Workspace/Governance policy or persistence.

## Non-functional acceptance

### WGAC011 — Architecture

- Domain remains framework/persistence independent according to canonical Domain rules.
- Application remains orchestration/policy declaration; current EF Core use is contained to approved local abstractions and is not expanded as precedent.
- Infrastructure owns concrete EF/PostgreSQL/RLS/adapters.
- API owns transport/composition only.
- no new production project/service is introduced by this workstream.

### WGAC012 — Security

- negative authorization tests exist for protected representative flows;
- tenant spoofing and cross-workspace access are denied;
- suspended/removed/revoked state is not accepted from stale authority;
- invitation/share secrets are not logged or returned through ordinary read models;
- authorization dependency failure is fail-closed.

### WGAC013 — Data ownership

- Workspaces writes Workspace-owned state;
- Governance writes Governance-owned state;
- foreign bounded contexts do not mutate those tables directly;
- shared physical DB does not become shared semantic ownership.

### WGAC014 — Migration

- clean database path passes;
- supported upgrade path passes;
- RLS/bootstrap lifecycle is verified where applicable;
- no unintended pending EF model changes remain.

### WGAC015 — Concurrency

Representative evidence covers:

- duplicate member/add;
- owner transfer/removal/demotion races;
- invitation accept/revoke/resend races;
- resource permission grant/revoke conflict;
- stale version/concurrency behavior on aggregate mutations.

### WGAC016 — Observability

Authorization and critical membership/admin operations carry bounded correlation and result telemetry without secrets/high-cardinality raw resource URLs.

### WGAC017 — Performance

Authorization facts and representative membership/resource permission queries must have bounded call/query behavior under realistic data size. Cache is not accepted as a correctness substitute.

### WGAC018 — CI

The exact certification SHA must pass all required backend, architecture, integration, OpenAPI, docs and security gates selected by TESTS/CERTIFICATION. Docs-only CI is not backend certification.

# Requirement traceability contract

Every `WGREQxxx` MUST resolve to one of:

```text
TEST-ID
STATIC/ARCHITECTURE GUARD
EXECUTED MIGRATION/DB PROOF
NAMED CROSS-CONTEXT CONTRACT PROOF
DOCUMENTED NOT_APPLICABLE rationale
BLOCKER / SOURCE_DEBT record
```

A row that only says "covered by existing tests" is insufficient.

Minimum traceability columns:

| Requirement | Capability | Source authority | Execution work unit | Test/evidence ID | Status | Blocking debt |
|---|---|---|---|---|---|---|

`workspace-governance.tests.md` owns the mapping; `workspace-governance.certification.md` records executed outcomes.

# Source-audit rules

Before changing code, PLAN must inventory exact candidate paths for:

```text
Domain/Workspaces
Domain/Governance
Application/Features/Workspaces
Application/Features/Governance
Application/Common request/security pipeline
Infrastructure Data/Authz/RLS/configurations
Infrastructure messaging consumers/projections
API Workspaces/Governance contracts/endpoints
Domain/Application/Infrastructure/API/Architecture/Integration tests
named downstream consumers
```

Every discovered capability is classified `RETAIN`, `HARDEN`, `COMPLETE`, `REHOME`, `DEPRECATE`, `NOT_APPLICABLE`, `SOURCE_DEBT`, or `BLOCKED` before material implementation.

Source-first does not mean source is automatically correct. It means execution begins from observed reality rather than inventing a parallel system.

# Explicit prohibited patterns

P2 MUST NOT introduce:

```text
endpoint-only role authorization
handler-local authorization engine
second global authorization pipeline
Governance adapter reading arbitrary foreign DbContexts
consumer injecting IWorkspaceDbContext/IGovernanceDbContext
resource lifecycle moved into Governance
RLS used as the only business authorization decision
raw invitation/share token persistence/logging
plan-tier logic encoded as Governance role policy
Workspace Rules used as generic Automation/Permission engine
duplicate Workspace membership table/source of truth
CustomRole reported complete from Domain presence alone
hand-edited OpenAPI as contract authority
new production service/project to solve folder-boundary concerns
```

# Expected verification layers

The revised TESTS artifact must map requirements to the smallest layer that proves the claim.

```text
T0  static/source/document guards
T1  Domain unit tests
T2  Application handler/policy tests
T3  Infrastructure adapter/persistence tests
T4  API/contract/OpenAPI tests
T5  Architecture boundary tests
T6  Integration tests with production-like DI/PostgreSQL
T7  security negative tests
T8  concurrency/race tests
T9  migration/RLS/clean-upgrade tests
T10 performance/reliability evidence
T11 cross-context producer/consumer contract tests
```

Rules:

- do not use an API unit test to claim a Domain invariant;
- do not use EF InMemory to claim PostgreSQL RLS behavior;
- do not use a Domain event unit test to claim public integration-event delivery;
- do not use source grep alone to claim effective authorization semantics;
- do not use a green docs-only workflow as backend exact-SHA certification.

# Coding-agent decision boundary

## Agent MAY decide locally

When all canonical semantics are already fixed, an implementation agent may decide:

- private method extraction;
- local naming consistent with repository conventions;
- test fixture organization;
- query implementation details that preserve the contract;
- validation placement within the already-approved Application/API boundary;
- refactoring of touched code that does not change ownership/public contracts.

## Agent MUST NOT decide locally

The agent must stop/record a decision for:

- changing Account ↔ Workspace ownership;
- changing WorkspaceRole meaning or inventing a new built-in role;
- changing allow/deny precedence;
- treating CustomRole as equivalent to WorkspaceRole without a decision;
- adding a new ResourceKind or PermissionAction whose business owner is unclear;
- adding arbitrary foreign-table reads to shared authorization SQL;
- creating a new global authorization pipeline;
- weakening RLS or bypassing the request data-session model;
- changing invitation/share-token protection strategy;
- exposing owner-local DbContext interfaces as cross-context contracts;
- promoting a secondary Governance capability into the P2 blocking gate without dependency evidence;
- declaring VERIFIED/STABLE without certification evidence.

# Stop conditions

Execution stops and records a blocker when any of the following is true:

## WGSTOP001 — P1 producer contract is not sufficiently stable

P2 cannot harden against an unresolved Actor/Account/tenant contract.

## WGSTOP002 — Account vs Workspace tenancy semantics conflict

Do not duplicate tenant roots to make a handler work.

## WGSTOP003 — duplicate membership truth is required

A second active Workspace membership authority is not allowed.

## WGSTOP004 — role/permission engines compete

If `WorkspaceRole`, `CustomRole`, `PermissionRule`, `WorkspacePolicy` or resource permissions imply contradictory authority, resolve semantics before adding new rules.

## WGSTOP005 — resource permission meaning is ambiguous

Do not expose new grant APIs until subject/scope/resource/authority-ceiling semantics are fixed.

## WGSTOP006 — PermissionRule vs WorkspacePolicy precedence is unknown for the touched flow

Do not guess allow/deny order.

## WGSTOP007 — Workspace Rules overlap Governance or Automation

Classify semantic owner first.

## WGSTOP008 — resource/action compatibility risk is unknown

Do not rename/remove resource kinds/actions with unknown consumers.

## WGSTOP009 — background/global path bypasses canonical access control

Do not certify while protected non-HTTP execution has an unbounded bypass.

## WGSTOP010 — share-link security policy is missing

Do not ship reusable public bearer access without explicit scope/expiry/revocation/secret handling.

## WGSTOP011 — handler-local role checks are necessary for correctness

This signals the canonical access-control contract is incomplete; fix the contract rather than normalize the bypass.

## WGSTOP012 — cross-context private persistence is required

Create/consume an owner-safe Public contract/Port/adapter or reopen the architecture decision.

## WGSTOP013 — architecture gate contradicts proposed implementation

Do not weaken the test merely to merge feature code.

## WGSTOP014 — pending migration/model drift exists

Certification is blocked until persistence state is deliberate and reproducible.

## WGSTOP015 — stale execution-document topology

If PLAN/TESTS/CERT reference `docs/workstreams/execution/...`, old Application non-module paths, obsolete authorization types, or missing canonical files, fix the docs before using them as coding authority.

## WGSTOP016 — Domain-only capability is being reported as delivered

The capability must be classified by layer and proven by its actual intended delivery surface.

# P2 readiness model

## D4 VERIFIED

A P2 capability is D4 only when:

- requirement and source owner are resolved;
- implementation exists on the candidate SHA;
- mapped tests/evidence were executed;
- required integration/security/migration evidence passes;
- blocking debt for that capability is zero or explicitly prevents D4.

## D5 STABLE

D5 additionally requires:

- named consumer compatibility;
- contract stability/change protocol;
- no known blocking source debt;
- exact-SHA CI evidence;
- operationally relevant revocation/tenant/failure semantics proven for the capability.

# Downstream handoff contract

P3 protected WorkManagement work may consume:

```text
stable AccountId
stable WorkspaceId
stable active Workspace/member facts
canonical ResourceKind
canonical ResourceId
canonical PermissionAction
canonical request security declaration
central AccessControlBehavior decision
Governance policy semantics
source-owner facts contract
```

P3 MUST NOT consume:

```text
Workspace/Governance DbContext interfaces
Governance EF entities
raw private tables as a public contract
handler-local role-name policy
RLS visibility as business permission
```

# Documentation handoff

The complete execution package is:

```text
docs/workstreams/executions/workspace-governance/
  workspace-governance.spec.md
  workspace-governance.plan.md
  workspace-governance.tests.md
  workspace-governance.certification.md
  decisions/
    PR-WG-00-semantic-inventory.md
    ...execution decision/evidence records as required
```

References MUST use `executions` (plural).

# Final Definition of Done — SPEC contract

This SPEC is ready to govern implementation only when:

- current source topology and canonical access-control mechanism are correctly described;
- every Workspace/Governance source capability has an explicit owner/classification path;
- P2 core vs secondary scope is explicit;
- local EF/persistence exception is constrained and not exported as a cross-context API;
- RLS is explicitly defense-in-depth;
- Domain presence is separated from delivered capability status;
- all requirements can be traced by the revised TESTS artifact;
- PLAN can execute from exact-SHA inventory without inventing a new architecture;
- CERTIFICATION can record D4/D5 independently from this SPEC.

The final operating invariant is:

```text
Identity/Accounts provides principal + tenant identity
        ↓
Workspaces owns collaborative containment + membership facts
        ↓
resource owner provides resource/action/visibility facts
        ↓
Governance owns policy semantics
        ↓
Application AccessControlBehavior enforces one decision
        ↓
handler executes only after allow
        ↓
Infrastructure/RLS constrains persistence as defense-in-depth
```

No layer may silently replace another layer's authority.

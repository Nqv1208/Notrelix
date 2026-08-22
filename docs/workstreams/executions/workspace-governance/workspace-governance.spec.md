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
  - audit
  - security-events
  - share-links
  - governance-templates
  - authorization
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
  - docs/workstreams/capability-map.md
  - docs/workstreams/cross-team-dependencies.md
  - docs/workstreams/teams/workspace-governance.md
  - docs/workstreams/teams/identity-accounts.md
  - docs/workstreams/teams/platform-foundation.md
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
  - share-link-security-change
  - audit-security-event-change
  - downstream-resource-registration-change
  - p2-exit-gate-change
---

# SPEC — Workspace & Governance

## 1. Purpose

This specification defines the complete target capability contract for the Workspace & Governance team.

It is the master WHAT document for Priority 2 on the backend critical path.

It defines what must be true before WorkManagement and other resource-owning bounded contexts can safely build protected product features.

The primary P2 output is not merely "Workspace CRUD".

The primary P2 output is:

```text
stable Workspace containment
+
stable membership
+
stable resource/action model
+
stable permission semantics
+
stable authorization handoff
```

so downstream product contexts can answer:

```text
Which Workspace owns this resource?
Who is acting?
What action is being attempted?
What resource is targeted?
Is that action allowed?
```

without inventing local permission systems.

## 2. Relationship to Identity & Accounts

P2 consumes the P1 producer contract:

```text
stable Actor
stable User identity
stable Account identity
stable Account/Tenant semantics
stable current Account contract
tenant-isolation guarantee
```

Workspace & Governance MUST NOT reconstruct:

- authentication;
- password/session internals;
- Account identity;
- User identity;
- API-token authentication.

If P1 core is not D4/D5 according to the backend roadmap, P2 implementation may prepare source inventory/specs but must not harden against unstable upstream contracts.

## 3. Relationship between Workspaces and Governance

Workspaces and Governance are separate bounded contexts even though one team owns both.

### 3.1 Normative ownership rule — Facts / Policy / Enforcement

The authorization boundary for every current and future bounded context is:

```text
resource-owning context
→ owns resource business state, resource lifecycle, resource-specific action vocabulary,
  containment facts, visibility facts and actor↔resource relationship facts

Governance
→ owns permission, role, policy, grant/deny semantics and effective authorization policy

Application authorization contract
→ carries Actor + Account + Workspace/resource + resource category + ResourceId + Action

Application pipeline
→ enforces the decision before protected handler side effects

Infrastructure adapter owned by the resource context
→ may read that resource context's private persistence to produce authorization facts
```

The short invariant is:

```text
Facts       → resource owner
Policy      → Governance
Enforcement → Application pipeline
```

A consumer needing facts does not become the owner of the adapter that reads those facts.
A `Governance` infrastructure namespace MUST NOT contain adapters that query private
WorkManagement/Documents/Billing/etc. persistence merely because Governance consumes the output.

Workspaces owns:

```text
Workspace
Workspace containment under Account
Workspace lifecycle
Workspace membership
Invitations
Teams
Spaces
Workspace settings/rules where they define Workspace behavior
Provisioning semantics
Workspace home/composition state where business-owned
```

Governance owns:

```text
permission semantics
role semantics
policy semantics
resource-permission / grant semantics
permission-rule semantics where retained by source classification
effective authorization policy
share-link governance
governance audit/security facts where those capabilities actually exist
governance templates where retained
resource/action registry mechanics only when generic and semantics-neutral
```

Governance does **not** own the business meaning of resource-specific actions. For example,
WorkManagement owns what `archive-board`, `move-item` or equivalent product actions mean;
Governance may map those stable actions to permissions/roles/policies but MUST NOT invent them.

The team MUST NOT merge them into one giant aggregate or namespace merely because they are developed together.

## 4. Platform/Application authorization responsibility

The ownership handshake is:

```text
resource-owning context
→ defines resource identity + meaningful actions

Governance
→ defines permission/policy/role semantics

Application
→ declares authorization requirement

Application pipeline
→ enforces requirement through the existing canonical authorization mechanism
```

Governance does NOT own the generic request pipeline.

The existing Application security pipeline is the default authority and MUST be hardened/reused
rather than replaced by a second P2 authorization pipeline. At the current source baseline this
includes the `AuthorizationBehavior` / `IAuthorizationDecisionStore` family; exact types remain
candidate-SHA evidence and must be rediscovered before coding.

Platform does NOT own permission semantics. Platform may provide generic runtime mechanisms only
when already canonical or separately approved; P2 does not move authorization business policy to Platform.

Resource teams do NOT own a second authorization engine.

## 5. Current source evidence boundary

All source-tree descriptions in this SPEC are **illustrative preparation-time evidence only**.
The authoritative source baseline for execution is the exact candidate SHA captured by PLAN Phase 0.
If a listed folder/type does not exist at that SHA, the coding agent MUST NOT create it merely to make
source match this SPEC. If the source contains additional canonical types, they MUST be inventoried.

At the preparation baseline, Domain source contains approximately:

```text
Workspaces/
├── Invitations
├── Members
├── Rules
├── Spaces
├── Teams
├── Workspaces
└── WorkspaceRuleCodes.cs (or candidate-SHA equivalent)
```

and:

```text
Governance/
├── Permissions
├── Policies
├── Roles
├── ShareLinks
├── Templates
└── GovernanceRuleCodes.cs (or candidate-SHA equivalent)

Audit/SecurityEvent surfaces MUST be discovered from the actual Domain/Application source;
their presence in product scope is not evidence that a Domain folder must exist.
```

The current Application source visibly contains Workspaces feature areas such as:

```text
Abstractions
DTOs
Events
Invitations
Members
Provisioning
Settings
Spaces
Teams
WorkspaceHome
Workspaces
```

and Governance feature areas such as:

```text
AuditLogs
DTOs
PermissionRules
Permissions
Policies
ResourcePermissions
Roles
SecurityEvents
ShareLinks
```

This SPEC treats these as current source evidence, not proof that every current folder is correctly bounded or complete.

## 6. Physical architecture constraint

The backend remains:

```text
Notrelix.Domain
Notrelix.Application
Notrelix.Infrastructure
Notrelix.Platform
Notrelix.API
```

This SPEC does not authorize:

```text
Notrelix.Workspaces.Service
Notrelix.Governance.Service
Notrelix.Authorization.Service
```

production projects.

Logical bounded contexts remain inside the modular monolith until an approved extraction decision exists.

### 6.1 Application module-first evolution rule

P2 MUST follow the canonical Application organization documented by the current source baseline.
If Governance/Workspaces are in an active module-first migration, every touched use case MUST move or
be created in the canonical module-first location instead of adding new production code to a deprecated
legacy path. This is incremental slice migration, not a prerequisite mass folder rewrite.

Folder movement never changes bounded-context ownership, and no module-first migration may introduce
cross-context private persistence access.

## 7. Capability map

The complete Workspace & Governance scope is organized as:

```text
WG-WSP-01 Workspace identity
WG-WSP-02 Account containment
WG-WSP-03 Workspace lifecycle
WG-WSP-04 Workspace provisioning
WG-WSP-05 Workspace settings
WG-WSP-06 Workspace home/composition

WG-MEM-01 Membership identity
WG-MEM-02 Membership lifecycle
WG-MEM-03 Add/remove member
WG-MEM-04 Member state
WG-MEM-05 Last-admin/owner protection where defined
WG-MEM-06 Membership queries
WG-MEM-07 Identity lifecycle interaction

WG-INV-01 Invitation identity
WG-INV-02 Invitation creation
WG-INV-03 Invitation acceptance
WG-INV-04 Invitation rejection/revoke
WG-INV-05 Invitation expiry
WG-INV-06 Duplicate/replay handling
WG-INV-07 Invitation authorization

WG-TEAM-01 Workspace Team identity
WG-TEAM-02 Team membership
WG-TEAM-03 Team lifecycle
WG-TEAM-04 Team authorization handoff

WG-SPACE-01 Space identity
WG-SPACE-02 Space lifecycle
WG-SPACE-03 Space containment
WG-SPACE-04 Space authorization handoff

WG-GOV-01 Resource kind/resource identity contract
WG-GOV-02 Action contract
WG-GOV-03 Permission model
WG-GOV-04 Permission rule model
WG-GOV-05 Role model
WG-GOV-06 Built-in roles
WG-GOV-07 Custom roles where supported
WG-GOV-08 Policy model
WG-GOV-09 Resource permission model
WG-GOV-10 Effective authorization evaluation
WG-GOV-11 Authorization declaration contract
WG-GOV-12 Authorization pipeline handoff
WG-GOV-13 Resource registration/handshake

WG-SHR-01 Share-link identity
WG-SHR-02 Share-link lifecycle
WG-SHR-03 Share-link permission scope
WG-SHR-04 Share-link expiry/revocation
WG-SHR-05 Share-link security

WG-AUD-01 Governance audit fact
WG-AUD-02 Security event
WG-AUD-03 actor/resource/action traceability

WG-TPL-01 Governance template semantics
WG-TPL-02 role/policy template application where supported

WG-X-01 Identity/Accounts contract
WG-X-02 WorkManagement resource contract
WG-X-03 Documents resource contract
WG-X-04 Billing admin resource contract
WG-X-05 Automation/Integrations governance contract
WG-X-06 Analytics governance/audit contract
```

These are capability identifiers, not governance rule IDs.

# Workspace core

## 8. WGREQ001 — Workspace is a business boundary beneath Account

Workspace MUST represent the canonical collaborative/product container defined by product architecture.

Workspace MUST NOT be confused with:

- Account/Tenant;
- User;
- Team;
- Space;
- Board;
- Billing Subscription.

If current source uses Workspace as the tenant root while Accounts becomes distinct upstream, implementation must preserve the approved Account→Workspace relationship rather than duplicate tenancy.

## 9. WGREQ002 — stable Workspace identity

Workspace MUST have a stable canonical identity safe for reference by:

- Governance;
- WorkManagement;
- Documents;
- Collaboration;
- Automation;
- Billing/Entitlements where needed;
- Analytics.

Workspace identity MUST NOT depend on name/slug remaining unchanged.

## 10. WGREQ003 — Account containment

Every Workspace MUST belong to exactly the canonical Account/Tenant relationship defined by P1/product authority.

The invariant must be concurrency-safe and persisted.

No Workspace may float globally unless product architecture explicitly supports a system-level workspace.

## 11. WGREQ004 — containment is not authorization

Knowing an Account contains a Workspace is not the same as an Actor having access to that Workspace.

Containment and access MUST remain separate concepts.

## 12. WGREQ005 — Workspace lifecycle

Workspace lifecycle MUST define supported states and transitions.

Examples may include active/archive/delete, but this SPEC does not invent a status enum.

Canonical source/product semantics determine exact states.

## 13. WGREQ006 — Workspace delete/archive effects

Workspace lifecycle changes MUST define effects on:

- membership;
- invitations;
- teams;
- spaces;
- WorkManagement resources;
- Documents;
- Automation;
- Integrations;
- Billing/usage if applicable;
- Analytics.

Database cascades MUST NOT silently define cross-context business semantics.

## 14. WGREQ007 — Workspace update

Mutable Workspace metadata must be distinct from security-sensitive governance settings.

A generic Workspace update MUST NOT silently modify role/permission policy unless that policy is Workspace-owned by canonical authority.

## 15. WGREQ008 — Workspace provisioning

Provisioning MUST define which context owns each created object.

If personal/new-account provisioning creates:

```text
Account
Workspace
Member
Role assignment
initial Team/Space
```

the orchestration must preserve context ownership.

Provisioning does not justify a giant cross-context transaction by default.

## 16. WGREQ009 — provisioning idempotency

Repeated/retried provisioning MUST NOT create duplicate canonical Workspace/member/bootstrap governance state.

The exact idempotency mechanism follows Application/Platform architecture.

## 17. WGREQ010 — Workspace settings

Workspace-owned settings MUST be distinguished from:

- Governance policy;
- Identity/User preferences;
- Account settings;
- product-view settings.

Only true Workspace business settings belong here.

## 18. WGREQ011 — Workspace home/composition

If WorkspaceHome exists as a business concept, its ownership and persistence semantics must be explicit.

It MUST NOT become an untyped container for arbitrary cross-context state.

# Membership

## 19. WGREQ012 — WorkspaceMember is a Workspace concept

Workspace membership belongs to Workspaces unless canonical product authority explicitly places Account membership elsewhere.

WorkspaceMember MUST reference stable User/Actor identity from P1.

It MUST NOT own User credentials/profile identity.

## 20. WGREQ013 — membership identity

Membership needs a stable identity or stable composite business identity sufficient for lifecycle, audit and authorization.

Exact identity shape follows current architecture.

## 21. WGREQ014 — one effective membership relation

The system MUST prevent accidental duplicate active membership for the same canonical subject in the same Workspace unless product semantics explicitly allow distinct membership types.

## 22. WGREQ015 — membership lifecycle

Membership lifecycle MUST define supported transitions such as:

```text
invited/pending if modeled
active
suspended/disabled if modeled
removed
```

Do not invent states merely for symmetry.

## 23. WGREQ016 — add member authorization

Adding a member is a protected Workspace/Governance operation.

It MUST pass central authorization.

The handler MUST NOT rely on ad-hoc `IsAdmin`/role-name checks.

## 24. WGREQ017 — remove member authorization

Removing a member is protected and must preserve administrative invariants.

## 25. WGREQ018 — self-leave semantics

If members may leave a Workspace, the operation must define:

- whether owners/admins may self-leave;
- what happens to last required administrator;
- historical attribution;
- outstanding work ownership if relevant.

## 26. WGREQ019 — last-admin/owner protection

If product semantics require at least one owner/admin, concurrent removals/demotions MUST NOT violate the invariant.

This must be concurrency-safe, not only validated in UI.

## 27. WGREQ020 — member role association

WorkspaceMember may be assigned Governance role(s) or another approved authorization relation.

Membership MUST NOT itself become an alternate permission engine.

## 28. WGREQ021 — member suspension/inactive behavior

If suspension/inactive membership exists, authorization effects must be deterministic.

Inactive membership MUST NOT continue granting access through stale cache.

## 29. WGREQ022 — Identity deactivation interaction

When upstream Identity disables/deletes a User:

- Workspace membership history may remain;
- access must follow current identity/actor validity;
- Workspace MUST NOT mutate Identity-owned state.

## 30. WGREQ023 — membership historical attribution

Removing a member MUST NOT erase historical authorship/audit attribution in downstream contexts unless retention policy explicitly requires it.

# Invitations

## 31. WGREQ024 — Invitation is not Membership

Invitation represents intent to join or grant access.

It MUST NOT create active membership before the accepted business transition.

## 32. WGREQ025 — stable invitation identity

Invitation must be safely addressable/revocable without exposing a guessable privileged secret as ordinary identity.

## 33. WGREQ026 — invite target

Invitation target semantics MUST be explicit.

Possible targets may include:

- email;
- existing User;
- another identity reference.

Do not support every target type unless current product scope requires it.

## 34. WGREQ027 — invitation role/access intent

An invitation may carry intended access/role information, but final membership authorization state must follow current Governance semantics.

Invitation MUST NOT bypass policy validation at acceptance.

## 35. WGREQ028 — invitation expiry

Invitations with expiry MUST fail deterministically after expiry.

## 36. WGREQ029 — invitation revoke

Revoked invitation cannot later create membership.

## 37. WGREQ030 — invitation replay

Repeated acceptance of the same invitation MUST be idempotently safe.

It must not create duplicate member records or repeated privilege grants.

## 38. WGREQ031 — invitation race

Accept vs revoke/expire races must resolve deterministically.

## 39. WGREQ032 — invite enumeration resistance

Public/unauthenticated invitation lookup should expose only information required for the flow and avoid unnecessary Workspace/member/security leakage.

## 40. WGREQ033 — invitation secret safety

If invitation uses a secret/token:

- raw privileged token should not be logged;
- persisted verification material should use approved protection where applicable;
- ordinary list APIs must not return reusable secret material.

# Teams

## 41. WGREQ034 — Team belongs to Workspace

A Workspace Team is a collaboration/grouping construct within Workspace scope.

It is distinct from the engineering organization/team described in delivery docs.

## 42. WGREQ035 — Team membership

Team membership MUST reference valid Workspace members according to product semantics.

The system should not accidentally allow a User outside Workspace to gain resource access merely through malformed Team membership.

## 43. WGREQ036 — Team lifecycle

Deleting/archiving a Team must define effect on:

- role assignments;
- resource permissions;
- historical audit;
- downstream references.

## 44. WGREQ037 — Team as authorization subject

If Governance supports Team as a permission subject, the subject contract must be explicit.

Do not assume every Team automatically becomes a Governance principal.

# Spaces

## 45. WGREQ038 — Space semantics

Space must have one canonical business meaning.

If it is a Workspace sub-container, containment must be explicit:

```text
Account
→ Workspace
→ Space
```

This SPEC does not assume Space is equivalent to Board/project/folder.

## 46. WGREQ039 — Space lifecycle

Space lifecycle must define effects on contained resources according to ownership boundaries.

## 47. WGREQ040 — Space authorization

If Space is a Governable resource, resource/action registration belongs to the resource-owner/Governance handshake.

## 48. WGREQ041 — Space visibility

Visibility/access settings must not create a second authorization engine.

They should map to Governance semantics where access control is involved.

# Workspace rules/settings

## 49. WGREQ042 — Workspace Rules boundary

Current source contains a Workspaces `Rules` area.

PLAN must classify each rule as:

```text
Workspace business rule
Governance policy
automation rule
validation rule
```

A Workspaces folder name does not automatically prove ownership.

## 50. WGREQ043 — no rule-engine duplication

Workspace rules MUST NOT duplicate:

- Governance policy engine;
- Automation rules;
- Platform validation.

If current source overlaps, target ownership must be resolved before expansion.

# Governance resource model

## 51. WGREQ044 — resource ownership remains with business context

Governance does not own Board/Page/Invoice/etc. business state.

Each resource-owning context owns its resource identity and lifecycle.

Governance consumes an authorization representation sufficient to evaluate access.

## 52. WGREQ045 — resource authorization category is semantic, not CLR type identity

The resource category must represent a stable business authorization category.

`ResourceType`, `ResourceKind` or another existing source name may be canonical. The SPEC does not
pre-authorize a rename. The current source name MUST be preserved when its semantics are already valid;
a rename to `ResourceKind` requires explicit semantic benefit plus persistence/consumer migration proof.

It MUST NOT rely on assembly-qualified CLR type names as external/stable permission identity unless architecture explicitly defines that model.

## 53. WGREQ046 — resource ID is opaque to Governance

Governance should not need private resource persistence to parse business state merely to identify a resource.

When contextual facts are needed, use approved authorization/resource lookup contracts.

## 54. WGREQ047 — resource containment scope

Resource authorization must carry enough scope to prevent cross-Account/Workspace confusion.

Conceptually:

```text
Actor
Account
Workspace
ResourceCategory (current source may call this ResourceType/ResourceKind)
ResourceId
Action
```

Exact request type follows current architecture.

## 55. WGREQ048 — resource registration

A resource-owning context MUST have an explicit mechanism to expose:

- stable resource authorization category;
- supported resource-owned actions;
- scope/parent facts when required;
- only the actor↔resource facts actually needed by the accepted authorization model.

The coding agent MUST NOT invent endpoint-specific arbitrary strings.

## 56. WGREQ049 — resource registration ownership

The resource team owns business action semantics.

Governance owns permission/policy mapping.

Neither side may silently add actions on behalf of the other.

### Resource authorization facts provider contract

When effective authorization needs resource-owned facts that are not already present in the request
context, use a transport-neutral provider/lookup contract.

Required ownership:

```text
neutral Application security SPI / approved resource-owner contract
                ↑ implemented by
resource-owner Infrastructure adapter
                ↓ reads
resource-owner private persistence
```

For a Board example, a resolver may project facts such as `WorkspaceId`, visibility/audience and an
actor↔Board relationship, but the implementation that reads `Boards` / `BoardMembers` belongs to
WorkManagement infrastructure. It MUST NOT live under `Infrastructure.Governance` while directly
querying `IWorkManagementDbContext` or equivalent private storage.

The projection MUST carry **facts**, not a second policy engine. A resource adapter may translate its
private enum into a stable fact vocabulary only when the mapping is source/product-approved. Governance
then combines those facts with membership/permission/role/policy semantics to decide Allow/Deny.

The contract MUST remain transport-neutral. Today an adapter may use an in-process EF/read-model query;
a future extracted service may replace that implementation with gRPC, an event-fed local projection,
cache, or a hybrid strategy without changing the business-facing authorization contract.

# Actions

## 57. WGREQ050 — Action is business meaningful

Authorization Action must represent a stable operation such as:

```text
view
edit
delete
manage-members
share
```

or resource-specific action where needed.

Action names must be stable enough for policy/role persistence and migration.

## 58. WGREQ051 — action uniqueness

Action identity must be unambiguous within the chosen namespace/resource model.

## 59. WGREQ052 — action versioning/migration

Renaming/removing a persisted action requires policy/role migration.

Do not treat it as cosmetic string refactoring.

## 60. WGREQ053 — no HTTP verb equivalence

HTTP verbs do not automatically equal Governance Actions.

For example, multiple POST endpoints may represent different business actions.

# Permissions

## 61. WGREQ054 — Permission connects action to authorization semantics

Permission must have one canonical meaning.

It should not be duplicated as:

```text
string role check
enum permission check
resource permission table
policy expression
```

without a clear hierarchy.

## 62. WGREQ055 — permission persistence stability

Persisted Permission identifiers require stable migration semantics.

## 63. WGREQ056 — permission evaluation scope

Permission evaluation must be scoped to the correct Account/Workspace/resource context.

A permission from Workspace A MUST NOT authorize Workspace B.

## 64. WGREQ057 — deny semantics

If the model supports explicit deny, precedence must be unambiguous.

If the model does not support explicit deny, the coding agent MUST NOT introduce one casually.

## 65. WGREQ058 — default-deny principle

Where no applicable grant/policy exists, protected business actions SHOULD fail closed according to canonical authorization architecture.

## 66. WGREQ059 — permission cache

If effective permission is cached:

- cache key includes relevant actor/tenant/resource/policy dimensions;
- revocation invalidates within accepted security window;
- stale data cannot grant cross-tenant access.

# Permission rules

## 67. WGREQ060 — PermissionRule ownership

Current Application contains PermissionRules.

PLAN must determine whether they are:

- stored Governance rules;
- policy conditions;
- resource rules;
- legacy abstractions.

No new rule DSL should be introduced without architecture/product need.

## 68. WGREQ061 — deterministic rule evaluation

Rule evaluation must be deterministic for a given trusted authorization context.

## 69. WGREQ062 — rule input trust

Rules must distinguish trusted server-derived facts from client-supplied claims.

Client-provided fields cannot become authoritative authorization facts merely because a rule reads them.

# Roles

## 70. WGREQ063 — Role is a Governance concept

Role is a named collection/assignment construct over permissions/policies.

Role MUST NOT become canonical User identity or Workspace membership itself.

## 71. WGREQ064 — built-in roles

Built-in role semantics required for P2 must be stable enough to support initial product access.

Names alone are insufficient; effective permissions/actions are the real contract.

## 72. WGREQ065 — built-in role identity stability

If persisted, built-in role identity should remain stable across display-name changes.

## 73. WGREQ066 — role assignment

Role assignment must target approved subjects such as:

- member;
- team;
- another supported principal.

Exact subject model must be explicit.

## 74. WGREQ067 — role assignment scope

A Workspace-scoped role must not leak to another Workspace/Account.

## 75. WGREQ068 — custom roles

Custom roles are secondary to the P2 core unless the initial product requires them.

They MUST NOT block WorkManagement core if built-in roles + permissions are already stable.

## 76. WGREQ069 — custom-role mutation

Changing a custom role can affect many subjects/resources and must define:

- authorization;
- audit;
- cache invalidation;
- concurrency;
- deletion behavior.

## 77. WGREQ070 — role deletion

Deleting a role must not silently leave invalid role assignments or broaden access.

# Policies

## 78. WGREQ071 — Policy semantics

Policy represents conditional/governance authorization semantics beyond a simple role-permission mapping where current architecture supports it.

The model MUST NOT be made more expressive merely for theoretical completeness.

## 79. WGREQ072 — policy evaluation

Policy evaluation must be:

- deterministic;
- tenant-scoped;
- based on trusted facts;
- observable enough to diagnose denials without leaking sensitive policy internals.

## 80. WGREQ073 — policy composition

If multiple policies apply, composition/precedence must be explicit.

No implicit "last rule wins" unless canonical design says so.

## 81. WGREQ074 — policy versioning

Persisted policy schema changes require migration.

## 82. WGREQ075 — policy failure

Invalid/malformed policy state must fail safely.

Authorization MUST NOT default to allow because policy evaluation failed.

# Resource permissions

## 83. WGREQ076 — ResourcePermission meaning

Current Application exposes ResourcePermissions.

The canonical model must define whether a ResourcePermission is:

- direct ACL grant;
- role binding to a resource;
- exception/override;
- another explicit construct.

Do not leave overlapping semantics with Role/Policy unresolved.

## 84. WGREQ077 — direct grant scope

A direct resource grant, if supported, must include exact subject/resource/action scope.

## 85. WGREQ078 — inheritance

If permissions inherit from Workspace/Space/parent resource, inheritance rules must be explicit.

If inheritance is not part of current product semantics, do not invent hierarchy evaluation.

## 86. WGREQ079 — revocation

Removing direct permission must take effect within accepted security window and clear relevant caches.

# Effective authorization

## 87. WGREQ080 — one effective authorization decision

For a protected operation, the system should produce one canonical allow/deny outcome from Governance semantics.

Feature handlers MUST NOT combine a separate local permission decision after central authorization except for Domain invariants that are not authorization.

## 88. WGREQ081 — authentication vs authorization

Governance evaluates authorization for an already authenticated/trusted Actor.

It does not own authentication.

## 89. WGREQ082 — membership precondition

If Workspace membership is required for a resource, authorization must account for active membership.

Knowledge of Resource ID alone is insufficient.

## 90. WGREQ083 — effective permission sources

The allowed sources of access must be explicit.

Potential sources may include:

```text
built-in role
custom role
direct resource permission
policy
share link
```

Only supported sources may participate.

## 91. WGREQ084 — precedence

When multiple permission sources apply, precedence/combination must be deterministic.

## 92. WGREQ085 — fail closed

Missing subject/resource/policy context must not turn into global access.

## 93. WGREQ086 — resource-not-found privacy

Where privacy requires hiding resource existence, authorization/not-found mapping must follow canonical API policy.

# Authorization declaration and pipeline

## 94. WGREQ087 — Application declares resource/action requirement

Protected Application requests must declare enough information for central enforcement.

The exact mechanism follows the existing pipeline.

This SPEC does not authorize a new decorator/attribute/base interface if the existing model is sufficient.

## 95. WGREQ088 — pipeline owns enforcement

Authorization must execute in the approved pipeline before protected handler side effects commit.

## 96. WGREQ089 — handler-local role checks are not canonical enforcement

Patterns such as:

```text
if role == "Admin"
if user.IsOwner
if permissions.Contains(...)
```

inside feature handlers are source debt when they duplicate central Governance.

Domain invariants about ownership state may still exist if they are business invariants, not permission policy.

## 97. WGREQ090 — API endpoint does not own business authorization

API may adapt resource identifiers/transport.

It MUST NOT become the only enforcement point for Application business operations.

## 98. WGREQ091 — background authorization

Background/system operations must have explicit actor/authorization semantics where the business operation requires authorization.

Do not assume background execution is globally privileged.

## 99. WGREQ092 — authorization idempotency/order interaction

Authorization must compose safely with validation, idempotency and transaction behaviors.

The exact pipeline order follows backend architecture and must be verified rather than guessed.

# Resource-team handshake

## 100. WGREQ093 — downstream resource registration contract

Before a product context reaches D5, it must expose its Governable resource/action contract.

Examples:

```text
WorkManagement → Board / BoardItem actions
Documents → Page / Block actions where applicable
Billing → billing-admin actions
```

## 101. WGREQ094 — Governance does not invent WorkManagement semantics

Governance cannot independently decide that BoardItem has "move", "archive", "assign" actions without WorkManagement owning those semantics.

## 102. WGREQ095 — product context does not invent role names

WorkManagement/Documents/etc. must ask Governance whether action is allowed, not encode Governance role names locally.

## 103. WGREQ096 — resource lookup contract

If authorization requires owner/parent/workspace facts, the lookup interface must preserve context ownership.

Governance MUST NOT query another context's private EF tables directly.

# Share Links

## 104. WGREQ097 — ShareLink is a Governance access mechanism

A share link grants bounded access according to product semantics.

It MUST NOT automatically create a WorkspaceMember unless product explicitly defines that transition.

## 105. WGREQ098 — share-link secret

If a ShareLink uses a bearer secret:

- raw secret is generated securely;
- not logged;
- not returned by ordinary list APIs;
- protected verification material is stored appropriately.

## 106. WGREQ099 — share-link scope

A share link must identify:

```text
resource
allowed access/actions
tenant/workspace scope
expiry/revocation where supported
```

## 107. WGREQ100 — share-link least privilege

A share link MUST NOT grant broader Workspace access than intended resource scope.

## 108. WGREQ101 — share-link expiry

Expired link cannot continue authorizing through stale cache.

## 109. WGREQ102 — share-link revocation

Revocation becomes effective within accepted security window.

## 110. WGREQ103 — share-link enumeration

Guessing public identifiers must not reveal privileged resources.

## 111. WGREQ104 — share-link audit

Use of share links should be attributable to the link/access mechanism where product/audit requirements require it, without pretending the anonymous user is a normal authenticated User.

# Audit / Security events

## 112. WGREQ105 — Audit ownership

Governance Audit records authorization/governance-relevant facts.

It should not become a duplicate of every Domain event or general observability log.

## 113. WGREQ106 — immutable history semantics

Where audit records are meant as historical evidence, ordinary product mutation should not rewrite past audit facts.

Exact immutability/storage implementation follows architecture.

## 114. WGREQ107 — audit actor/resource/action

Critical Governance changes should record enough stable context:

```text
actor/principal
Account
Workspace
resource
action
result/change
time
correlation
```

subject to privacy policy.

## 115. WGREQ108 — security events

Governance Security events should represent security/governance facts, not raw infrastructure exceptions.

## 116. WGREQ109 — no secret material

Audit/security events MUST NOT include:

- bearer share secrets;
- session/API tokens;
- OAuth/MFA secrets;
- authorization headers.

## 117. WGREQ110 — audit tenant isolation

Audit queries themselves are tenant/workspace protected.

# Governance templates

## 118. WGREQ111 — template meaning

Governance templates may predefine role/policy configurations where product supports it.

A template is not a live permission source after application unless canonical semantics say so.

## 119. WGREQ112 — template application

Applying a template must validate current resource/action/permission schema.

It must not silently create unknown/stale permissions.

## 120. WGREQ113 — template versioning

Template schema changes require compatibility handling if stored/reused.

# Account/Identity integration

## 121. WGREQ114 — consume stable Actor/User only

Workspaces/Governance consume stable Identity contract.

They MUST NOT reference:

- password;
- OAuth tokens;
- Session EF entity;
- MFA secrets.

## 122. WGREQ115 — consume stable Account

Workspace Account containment must use P1 canonical Account ID.

No second tenant identifier may be invented.

## 123. WGREQ116 — Account disabled behavior

When upstream Account becomes disabled/inactive according to contract, Workspace/Governance protected operations must fail according to lifecycle policy.

## 124. WGREQ117 — User disabled behavior

When Identity Actor becomes invalid, Governance must not continue authorizing based solely on stale membership cache.

# WorkManagement integration

## 125. WGREQ118 — Board resource registration

WorkManagement must be able to register/declare Board resources/actions without Governance depending on WorkManagement internals.

## 126. WGREQ119 — BoardItem resource registration

If BoardItem has independently governable actions, the contract must be explicit.

Do not assume every nested entity requires independent ACL.

## 127. WGREQ120 — WorkManagement staged entry gates

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
resource facts provider/lookup boundary proven
representative allow / deny / cross-tenant deny proven
```

P3 product release/certification uses P3-B, not P3-A.

## 128. WGREQ121 — WorkManagement local ownership invariant

WorkManagement may still enforce Domain invariants such as valid state transition.

Governance authorization must not absorb WorkManagement business rules.

# Documents/Collaboration integration

## 129. WGREQ122 — Page/Document resource

Documents defines Page/Document action semantics.

Governance evaluates access.

## 130. WGREQ123 — comment authorization target

Collaboration authorization should depend on the target resource access contract rather than owning target resource tables.

# Billing integration

## 131. WGREQ124 — billing administration

Billing defines business actions such as managing subscription/payment methods.

Governance controls which Account actors may perform those actions.

## 132. WGREQ125 — entitlement vs authorization

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

## 133. WGREQ126 — automation actor/authorization

Automation execution may act on behalf of a User/system principal according to approved actor model.

Governance must evaluate business authorization where appropriate.

## 134. WGREQ127 — Integration administration

Creating/managing provider connections is a governable resource/action owned semantically by Integrations and authorized by Governance.

# Analytics

## 135. WGREQ128 — governance analytics facts

Analytics may consume derived facts such as:

- membership counts;
- role assignments;
- authorization/security events;

subject to privacy.

Analytics MUST NOT use Governance private tables as an uncontrolled source of truth.

# Data ownership

## 136. WGREQ129 — Workspace persistence private

Other contexts MUST NOT mutate Workspace/Member/Invitation/Team/Space tables directly.

## 137. WGREQ130 — Governance persistence private

Other contexts MUST NOT mutate Role/Permission/Policy/ResourcePermission/ShareLink tables directly.

## 138. WGREQ131 — no dual membership truth

There MUST NOT be one Workspace membership model in Workspaces and another canonical membership model in Governance.

Governance references/authorizes membership; Workspaces owns membership state.

## 139. WGREQ132 — no dual role truth

Role semantics must have one canonical owner in Governance.

Workspace/Identity/Product contexts must not maintain their own business role enums that independently authorize the same actions.

## 140. WGREQ133 — no private cross-context joins as contract

Same physical database does not authorize Governance to join private WorkManagement/Documents/Billing tables as its public business contract.

# API

## 141. WGREQ134 — Workspace API categories

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

## 142. WGREQ135 — Governance API categories

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

## 143. WGREQ136 — API error taxonomy

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

## 144. WGREQ137 — no secret response leakage

Invitation/share-link secret material must be minimized according to issuance/read lifecycle.

## 145. WGREQ138 — OpenAPI compatibility

API changes update OpenAPI/generated contract evidence where required.

# Events

## 146. WGREQ139 — Workspace event ownership

Workspaces owns facts such as:

```text
Workspace lifecycle
membership lifecycle
invitation lifecycle
Team/Space lifecycle
```

where cross-context consumers need them.

## 147. WGREQ140 — Governance event ownership

Governance owns facts such as:

```text
role/policy/permission change
share link lifecycle
security/governance event
```

where externally meaningful.

## 148. WGREQ141 — event scope

Cross-context events must include enough stable:

```text
Account
Workspace
subject/resource identity
```

to consume without private table access.

## 149. WGREQ142 — event security

Events must not expose privileged secrets or unnecessary personal data.

## 150. WGREQ143 — event compatibility

Once downstream consumers rely on D4/D5 events, breaking changes require migration/rollout coordination.

# Concurrency

## 151. WGREQ144 — membership uniqueness concurrency

Concurrent add/accept operations must not create duplicate active membership.

## 152. WGREQ145 — last-admin concurrency

Concurrent remove/demote operations cannot leave a Workspace in an invalid no-admin state if such invariant exists.

## 153. WGREQ146 — invitation accept/revoke race

Final state must be deterministic and cannot grant access after authoritative revocation.

## 154. WGREQ147 — role assignment concurrency

Concurrent assignment/removal must not create duplicate or contradictory effective authorization records.

## 155. WGREQ148 — policy update concurrency

If policies are versioned/edited concurrently, stale writes must be handled according to current concurrency architecture.

## 156. WGREQ149 — share-link revoke race

A revoked link must not be reactivated by stale writes/caches.

# Security

## 157. WGREQ150 — authorization is high sensitivity

Changes to:

- resource/action semantics;
- roles;
- permissions;
- policies;
- share links;
- membership admin;
- invitation admin;

require negative/security verification.

## 158. WGREQ151 — fail closed on evaluation failure

An authorization evaluation exception/missing rule must not become allow.

## 159. WGREQ152 — tenant spoofing resistance

Actor/Account/Workspace/resource scope must agree.

Client-provided Workspace/Resource IDs cannot cross tenants.

## 160. WGREQ153 — stale authorization cache

Revoked membership/role/permission/share link must stop granting access within accepted security window.

## 161. WGREQ154 — privilege escalation resistance

Users cannot:

- assign themselves a stronger role;
- create a grant above their authority;
- invite with stronger access than permitted;
- mutate policies they cannot manage;
- use Team membership to escape Workspace membership.

## 162. WGREQ155 — authorization decision privacy

Denial/error output should not leak sensitive policy structure or hidden resource existence beyond canonical API policy.

## 163. WGREQ156 — share-link bearer security

Share links are bearer credentials where applicable and require:

- entropy;
- safe comparison/verification;
- no ordinary log exposure;
- revocation/expiry enforcement.

## 164. WGREQ157 — audit access security

Audit/security-event APIs require explicit Governance permission.

# Reliability

## 165. WGREQ158 — provisioning partial failure

If Workspace provisioning orchestrates Account/Workspace/Governance bootstrap, partial failure behavior must be explicit.

The system must not report a fully provisioned Workspace when required authoritative pieces failed.

## 166. WGREQ159 — authorization dependency failure

If required resource/policy data cannot be resolved, protected operations fail safely.

## 167. WGREQ160 — event publication failure

State-changing Governance/Workspace operations that require integration events must follow existing transactional outbox/delivery architecture.

## 168. WGREQ161 — cache outage

Authorization cache failure must have an explicit safe fallback.

Do not default to allow.

# Observability

## 169. WGREQ162 — authorization traceability

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

## 170. WGREQ163 — membership/admin change observability

Critical membership/role/policy/share-link changes should be observable/auditable.

## 171. WGREQ164 — no secret telemetry

Logs/traces must not contain invitation/share-link bearer secrets or authentication credentials.

## 172. WGREQ165 — denial metrics

The system may expose safe aggregate authorization-denial/security metrics through existing observability mechanisms.

No new vendor/framework is implied.

# Performance

## 173. WGREQ166 — authorization is a hot path

Effective authorization may execute on most protected product requests.

It must avoid unbounded:

- cross-context database calls;
- policy scans;
- role scans;
- recursive resource traversal.

## 174. WGREQ167 — membership lookup efficiency

Workspace membership resolution should use appropriate indexing/cache while preserving revocation correctness.

## 175. WGREQ168 — resource permission lookup efficiency

Resource-level permissions must be indexed/scoped by relevant tenant/resource/subject dimensions.

## 176. WGREQ169 — cache correctness over raw speed

Any authorization cache must specify:

```text
key dimensions
invalidation triggers
maximum stale security window
tenant isolation
```

## 177. WGREQ170 — list/query scaling

Member/audit/permission lists need pagination/filter behavior consistent with API quality standards.

# Migration

## 178. WGREQ171 — membership migration

Changing membership identity/state schema requires preserving:

- User references;
- Workspace references;
- role assignments;
- historical audit.

## 179. WGREQ172 — resource/action migration

Renaming resource kinds/actions is a contract migration, not merely code refactor.

Must update:

- persisted permissions;
- roles;
- policies;
- share links;
- downstream declarations;
- tests.

## 180. WGREQ173 — role/permission migration

Changing built-in role semantics requires explicit impact analysis for existing Workspaces.

## 181. WGREQ174 — policy schema migration

Persisted policy changes require compatibility/backfill/versioning.

## 182. WGREQ175 — invitation/share-link secret migration

Changing token/hash format requires validity/rotation/revocation policy for existing links.

## 183. WGREQ176 — clean/upgrade database

Every schema-affecting P2 delivery requires both fresh DB and supported upgrade evidence.

## 184. WGREQ177 — no pending model changes

P2 completion cannot suppress EF pending-model warnings.

# P2 core producer contract

## 185. P2 mandatory core

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

## 186. P2 secondary scope

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

## 187. WGREQ178 — Workspace D5 gate

Workspace reaches D5 when:

- stable identity;
- Account containment;
- lifecycle baseline;
- membership baseline;
- tenant isolation;
- downstream resource containment contract

are verified.

## 188. WGREQ179 — Resource/Action D5 gate

Resource/action contract reaches D5 when WorkManagement can register/declare a resource/action without Governance/private coupling.

## 189. WGREQ180 — Permission D5 gate

Permission semantics reach D5 when:

- stable identifier/meaning;
- tenant scope;
- role mapping;
- effective decision;
- revocation;
- migration

are verified.

## 190. WGREQ181 — built-in Role D4+ gate

Built-in roles must be sufficiently verified for initial product actions.

Custom roles are not required to open P3 unless product says otherwise.

## 191. WGREQ182 — authorization integration D5 gate

Protected representative operations must be rejected before handler side effects when authorization fails.

## 192. P2 → P3 exit contract

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
  existing AuthorizationBehavior/decision-store path D5 or candidate-SHA equivalent
  resource-owner facts-provider ownership proven
  representative WorkManagement allow/deny/cross-tenant proof
```

Secondary Governance features do not block P3-B unless they are part of the initial product contract.

# Functional acceptance criteria

## 193. WGAC001 — Workspace acceptance

A Workspace exists as stable Account-contained business boundary with deterministic lifecycle.

## 194. WGAC002 — membership acceptance

Membership can be added/removed/queried without duplicate active membership or privilege ambiguity.

## 195. WGAC003 — invitation acceptance

Invitation lifecycle cannot create duplicate membership or grant access after revoke/expiry.

## 196. WGAC004 — resource registration acceptance

A downstream context can register/declare a resource kind/action contract without Governance reading its private persistence.

## 197. WGAC005 — permission acceptance

Effective authorization decisions are deterministic and tenant-scoped.

## 198. WGAC006 — role acceptance

Built-in role semantics produce expected permission sets without handler-local role checks.

## 199. WGAC007 — authorization pipeline acceptance

Unauthorized protected Application operations do not commit business side effects.

## 200. WGAC008 — share-link acceptance

A valid share link grants only intended bounded access and expires/revokes correctly.

## 201. WGAC009 — audit acceptance

Governance changes are auditable without secret leakage.

## 202. WGAC010 — cross-context acceptance

WorkManagement can consume Workspace/Governance contracts without private DB dependencies.

# Non-functional acceptance criteria

## 203. WGAC011 — architecture

Workspaces and Governance remain separate logical contexts.

No new production project/service.

## 204. WGAC012 — security

Cross-tenant access, stale privilege escalation and bearer-secret leakage are prevented.

## 205. WGAC013 — data ownership

One canonical owner exists for Workspace membership, Role, Permission and Policy state.

## 206. WGAC014 — migration

Persisted resource/action/role/policy changes have migration strategies.

## 207. WGAC015 — concurrency

Critical membership/admin/revocation invariants hold under concurrency.

## 208. WGAC016 — observability

Authorization/member/governance failures are diagnosable without exposing secrets.

## 209. WGAC017 — performance

Authorization hot-path changes respect current performance quality standards and avoid obvious query explosion.

## 210. WGAC018 — CI

Required architecture/core/API/integration suites execute non-zero and pass on candidate SHA before certification.

# Requirement traceability contract

## 211. TESTS artifact obligation

`workspace-governance.tests.md` MUST map every material requirement family to verification.

Minimum families:

```text
WGREQ001–WGREQ011
→ Workspace/provisioning/settings

WGREQ012–WGREQ023
→ Membership

WGREQ024–WGREQ033
→ Invitations

WGREQ034–WGREQ043
→ Teams/Spaces/Workspace Rules

WGREQ044–WGREQ062
→ Resource/Action/Permission/PermissionRules

WGREQ063–WGREQ086
→ Roles/Policies/ResourcePermissions/effective authorization

WGREQ087–WGREQ096
→ Application pipeline/resource-team handshake

WGREQ097–WGREQ113
→ ShareLinks/Audit/SecurityEvents/Templates

WGREQ114–WGREQ143
→ upstream/downstream/API/events

WGREQ144–WGREQ170
→ concurrency/security/reliability/observability/performance

WGREQ171–WGREQ177
→ migrations

WGREQ178–WGREQ182
→ P2 exit gate
```

# Expected test layers

## 212. Domain tests

Must cover applicable invariants for:

- Workspace;
- Member;
- Invitation;
- Team;
- Space;
- Role;
- Permission;
- Policy;
- ResourcePermission;
- ShareLink.

## 213. Application tests

Must cover:

- commands/queries;
- authorization declarations;
- provisioning;
- membership administration;
- invitation flows;
- role/policy mutations;
- effective authorization requests.

## 214. Infrastructure tests

Must cover:

- persistence;
- unique constraints;
- role/permission indexes;
- invitation/share secret verification storage;
- migration;
- cache adapters where applicable.

## 215. API tests

Must cover:

- Workspace/member/invitation/admin APIs;
- Governance APIs;
- unauthenticated/forbidden/not-found distinction;
- share-link boundary;
- OpenAPI.

## 216. Architecture tests

Must cover:

- Domain purity;
- Workspaces/Governance separation;
- downstream private persistence prohibition;
- pipeline-owned authorization;
- no feature-local dependency inversion violations.

## 217. Integration tests

Must cover:

- P1 Actor/Account → Workspace;
- tenant isolation;
- membership revocation;
- role/permission evaluation;
- production DI graph;
- WorkManagement resource handshake;
- migration/startup.

## 218. Security tests

Must cover:

- privilege escalation;
- cross-tenant spoofing;
- stale permission cache;
- invitation/share replay;
- bearer secret non-exposure;
- background actor misuse.

## 219. Concurrency tests

Must cover:

- duplicate membership;
- last-admin invariant if applicable;
- invitation revoke/accept;
- role/permission mutation;
- share-link revoke.

## 220. Migration tests

Required for any persisted:

- membership model;
- resource/action IDs;
- roles;
- permissions;
- policies;
- link secret formats.

# Coding-agent local decision boundary

## 221. Agent MAY decide locally

Within approved requirements, the coding agent may decide:

- private helper decomposition;
- internal mapper details;
- local EF query shape;
- test fixture structure;
- non-public variable names;
- local optimization preserving semantics.

## 222. Agent MUST NOT decide

The coding agent MUST NOT independently decide:

- Account equals Workspace;
- Workspace membership equals Account membership;
- Governance should own WorkspaceMember;
- Workspaces should own Role/Permission;
- role-name checks are acceptable as final architecture;
- every Domain entity needs its own ResourceKind;
- every nested resource needs independent ACL;
- custom roles are required before P3;
- explicit deny/inheritance should be added;
- share links create normal Users/Members;
- background jobs bypass authorization;
- resource teams may be queried through private EF tables;
- authorization pipeline may be weakened;
- permissions can be renamed without migration;
- Workspace/Governance should become separate services now.

# Stop conditions

## 223. WGSTOP001 — P1 core contract unstable

If Actor/Account/tenant semantics are not stable enough:

```text
STOP affected P2 hardening
```

Source inventory/spec work may continue.

## 224. WGSTOP002 — Account vs Workspace ambiguity

Do not implement containment until canonical relation is resolved.

## 225. WGSTOP003 — duplicate membership truth

If Account membership and Workspace membership overlap ambiguously:

```text
STOP membership redesign
→ clarify semantic boundary
```

## 226. WGSTOP004 — role/permission duplicate engines

If current source contains two active permission models:

```text
STOP expansion
→ identify canonical model/legacy source
```

## 227. WGSTOP005 — ResourcePermission semantics unclear

Do not add more ACL features until direct grant vs role binding vs override semantics are explicit.

## 228. WGSTOP006 — PermissionRules vs Policies overlap

Do not invent a new DSL or merge models without architecture/product decision.

## 229. WGSTOP007 — Workspace Rules overlap Governance/Automation

Classify rules before implementation.

## 230. WGSTOP008 — resource/action migration risk unknown

If changing persisted resource/action identity with unknown consumers:

```text
STOP rename/removal
→ inventory policies/roles/consumers
```

## 231. WGSTOP009 — background/global authorization bypass

Do not implement fake admin/system bypass to make jobs pass.

## 232. WGSTOP010 — share-link security policy missing

If bearer secret/expiry/scope behavior is undefined:

```text
STOP share-link release
```

P2 core may continue if share links are secondary.

## 233. WGSTOP011 — handler-local authorization required to pass

Do not weaken architecture.

Resolve resource/action/policy handshake.

## 234. WGSTOP012 — cross-context private DB dependency

Stop integration and define approved Application/event/read contract.

## 235. WGSTOP013 — architecture test conflict

Do not disable/relax valid architecture gates.

## 236. WGSTOP014 — migration pending-model drift

Do not suppress EF pending model warning.

# Readiness model

## 237. P2 D4 VERIFIED

P2 core reaches D4 when:

- Workspace/Member/resource/action/permission baseline implemented;
- representative authorization path works;
- tenant isolation verified;
- WorkManagement consumer handshake proven in integration/test fixture.

## 238. P2 D5 STABLE

P2 core reaches D5 when:

- required D5 gates meet backend roadmap;
- migrations complete;
- security hardening complete;
- downstream WorkManagement can depend without private coupling;
- exact-SHA required CI passes;
- no unresolved critical permission/tenant ambiguity remains.

# SPEC implementation readiness

## 239. PLAN responsibilities

`workspace-governance.plan.md` must next:

1. inventory Workspaces/Governance across Domain/Application/Infrastructure/API/tests;
2. map all current folders to this SPEC;
3. classify Workspaces `Rules`;
4. classify Governance `PermissionRules`, `Policies`, `ResourcePermissions`;
5. verify P1 producer contract availability;
6. order Workspace/membership before Governance downstream gate where dependencies require it;
7. verify central authorization pipeline;
8. define WorkManagement resource-registration handshake;
9. define exact migrations after source inventory;
10. split P2 core from secondary features;
11. define PR decomposition;
12. map work units to WGREQ IDs.

## 240. TESTS responsibilities

`workspace-governance.tests.md` must:

- map WGREQ families to test layers;
- define positive/negative/privilege-escalation cases;
- define concurrency cases;
- define tenant-isolation matrix;
- define resource/action/permission migration tests;
- define pipeline/architecture tests;
- define WorkManagement handoff gate;
- map all critical tests to CI.

## 241. CERTIFICATION responsibilities

`workspace-governance.certification.md` must define at least:

```text
P2 CORE CERTIFIED
→ WorkManagement may rely on Workspace/Governance

WORKSPACE & GOVERNANCE FULL SCOPE CERTIFIED
→ all release-scoped secondary capabilities complete
```

As with P1, P3 MUST NOT be forced to wait for every custom-role/share-link/template feature if P2 core is already stable.

# Final target state

## 242. Workspace target

At the end of P2 core:

```text
Account
→ Workspace
→ WorkspaceMember
```

must be stable, tenant-safe and consumable.

## 243. Governance target

At the end of P2 core:

```text
Actor
+
Workspace/resource scope
+
ResourceKind
+
Action
+
Permission/Role/Policy semantics
→ one canonical authorization decision
```

must be stable.

## 244. Downstream target

WorkManagement must be able to define:

```text
Board / BoardItem
+
their actions
```

and consume Governance without:

- role strings;
- Identity private data;
- Governance private implementation;
- direct policy-table reads.

## 245. Architecture target

The backend remains a modular monolith.

Workspaces and Governance remain separate logical bounded contexts.

One team may own both without merging their models.

## 246. Final operating rule

For every Workspace/Governance implementation decision, ask:

```text
Is this Workspace containment/membership/lifecycle?
→ Workspaces owns it.

Is this permission/role/policy/access semantics?
→ Governance owns it.

Is this resource business meaning/action?
→ resource-owning context owns it.

Is this generic enforcement/runtime mechanism?
→ Application/Platform owns it.

Is ownership unclear?
→ stop before coding.
```

That rule is mandatory for preserving P2 as the authorization backbone for all downstream product development.

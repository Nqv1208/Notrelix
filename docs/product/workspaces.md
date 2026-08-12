---
document_id: PROD-WORKSPACES
document_type: product-context
status: active
owner: workspaces
applies_to:
  - workspaces
  - workspace-membership
  - workspace-invitations
  - spaces
  - teams
  - workspace-scope
evidence:
  - PRODUCT.md
  - docs/product/product-model.md
  - docs/product/contexts/accounts.md
  - docs/product/contexts/identity.md
  - docs/product/contexts/governance.md
  - docs/architecture/bounded-context-map.md
  - backend/src/Notrelix.Domain/Workspaces/
  - backend/tests/Notrelix.Domain.Tests/
  - backend/tests/Notrelix.Integration.Tests/
  - frontend/packages/features/workspace/
review_on:
  - workspace-lifecycle-change
  - workspace-membership-change
  - workspace-invitation-change
  - workspace-role-change
  - team-or-space-model-change
  - workspace-scope-change
  - workspace-switch-change
  - account-workspace-relationship-change
  - workspace-deletion-or-retention-change
---

# Workspaces Context

> **Workspaces owns the collaboration tenant structure in which most day-to-day product work is scoped.**
>
> It owns Workspace lifecycle, Workspace membership/invitations, and Workspace-owned organizational containers where product-approved.

This document is the canonical product owner for Workspaces semantics.

# 1. Mission

Workspaces provides a stable collaboration/work tenant boundary for:

```text
membership
workspace scope
resource containment/reference
organization inside a Workspace
tenant transition
workspace lifecycle
```

It does not own every resource merely because that resource is Workspace-scoped.

# 2. Does not own

```text
principal authentication
→ Identity

Account-level administration/membership
→ Accounts

generic permission/policy/sharing
→ Governance

Boards/Items
→ Work Management

Pages/Blocks
→ Documents

comments/activity
→ Collaboration

commercial entitlement
→ Billing
```

# 3. Workspace identity

A Workspace has stable identity independent of:

- display name;
- current members;
- current plan entitlement;
- frontend “current workspace” selection;
- Account display metadata.

# 4. WSP-001 — Workspace is a collaboration tenant, not a generic global scope

Workspace is the collaboration boundary for most product resources.

Do not invent Workspace scope for operations that are genuinely:

- Identity-global;
- Account-level;
- provider-global;
- system-level.

# 5. WSP-002 — Workspace scope is explicit

Workspace-scoped commands, queries, caches, search, events, background jobs, realtime subscriptions, and persistence/RLS contexts carry or resolve Workspace identity explicitly where required.

A process-local “current workspace” variable is not sufficient durable identity.

# 6. Workspace lifecycle

Workspace lifecycle may include product states such as:

```text
active
archived/suspended where product defines them
deleted
restored where allowed
```

The lifecycle is independent from Account/Billing lifecycle.

# 7. WSP-003 — Workspace lifecycle is not Account lifecycle

An Account can contain multiple Workspaces with independent lifecycle.

Account suspension may affect Workspace access through policy, but the Account does not become the Workspace aggregate.

# 8. Workspace creation

Creating a Workspace establishes:

- stable Workspace identity;
- parent Account relationship where applicable;
- initial settings;
- initial owner/member relationship;
- valid initial lifecycle.

Additional resources should be created through their own context contracts.

# 9. Account relationship

An Account may administratively contain or route Workspaces.

Accounts may control:

- who can create;
- default region;
- routing/onboarding;
- entitlement prerequisites.

Workspaces owns the created Workspace.

# 10. WSP-004 — Account relationship does not replace Workspace membership

Being an Account Member does not automatically imply active membership in every Workspace unless explicit product policy provisions it.

# 11. Workspace Member

A Workspace Member represents the relationship between a stable Identity and one Workspace.

It owns Workspace participation lifecycle.

# 12. WSP-005 — Workspace membership is a security boundary

A principal must not read/write a Workspace-scoped resource merely because:

- the resource ID exists;
- they are authenticated;
- they are an Account Member.

Authoritative server mechanisms must establish the required Workspace/access facts.

# 13. Membership lifecycle

Membership may include:

```text
active
suspended
removed/left
```

where product-approved.

Invitation is not a membership state.

# 14. WSP-006 — Invitation is not Membership

A pending Workspace invitation is not active access.

Acceptance creates/resolves membership through explicit workflow.

Revoked/expired invitations must not authorize access.

# 15. Membership role

Workspace role expresses Workspace-level participation/administration where product-approved.

It may be:

```text
owner
admin
member
guest
```

or another defined vocabulary.

# 16. WSP-007 — Workspace role is not universal resource permission

A Workspace role may be an input to Governance authorization.

It does not automatically grant every action on every Board/Page/resource.

Resource policy and context semantics still apply.

# 17. Last-owner/admin invariant

Workspace should not enter an administratively orphaned state when product policy requires at least one owner/admin.

Removing, leaving, or demoting the last required owner must fail before authoritative mutation.

# 18. WSP-008 — Cross-member invariants fail atomically

When membership changes depend on external counts/facts:

- Application may load needed facts;
- Domain validates transition;
- rejection leaves state/version/events unchanged.

Do not hide repository callbacks inside Domain rules.

# 19. Leave Workspace

Leaving is a membership transition, not Identity deletion.

It should define:

- last-owner restriction;
- transfer requirements if any;
- effect on assigned work;
- historical attribution;
- future notifications.

# 20. Remove Member

Removal ends one Workspace participation relationship.

It does not globally disable Identity.

# 21. Suspend Member

Suspension can temporarily block Workspace participation while retaining the relationship/history if product policy supports it.

# 22. Guest/external participation

Guest/external membership may have constrained Workspace/resource access.

Do not infer guest semantics only from email domain.

# 23. Workspace invitation

Invitation represents proposed Workspace participation.

It may include:

```text
Workspace
invitee identity/email
intended role
inviter
expiry
token/claim identity
status
```

# 24. WSP-009 — Invitation acceptance is retry-safe

Accepting the same valid invitation repeatedly must not create duplicate memberships.

Revocation/acceptance races need deterministic outcome.

# 25. Invitation token

Raw invitation secrets/tokens are security-sensitive.

Workspace product semantics may expose safe invitation identity/status, not raw reusable token storage.

# 26. Invitation expiry

Expired invitations cannot be used to create access.

Resending may create a new invitation authority or extend according to explicit policy.

# 27. Account invitation versus Workspace invitation

Account invitations belong to Accounts.

Workspace invitations belong to Workspaces.

They may be combined in onboarding flow, but they remain different scopes.

# 28. Identity relation

Workspace Member references stable Identity.

Workspaces does not own:

- password;
- MFA;
- sessions;
- OAuth login.

# 29. WSP-010 — One Identity may belong to many Workspaces

Identity is global/stable enough to participate in several Workspaces.

Workspace-specific role/status must not be stored as one User-global property.

# 30. Governance relation

Governance consumes Workspace facts such as:

- membership;
- role;
- Workspace/resource relation.

Governance evaluates operation-level access.

# 31. Billing relation

Billing entitlement can limit:

- Workspace creation;
- member count;
- premium Workspace features.

Billing does not own membership or Workspace lifecycle.

# 32. WSP-011 — Entitlement and membership are distinct

A Workspace Member may be valid while a capability is not commercially entitled.

A paid plan does not automatically grant a principal membership.

# 33. Work Management relation

Boards/Items are often Workspace-scoped.

Work Management owns those resources.

Workspaces supplies tenant identity/membership facts.

# 34. Documents relation

Pages/Documents may be Workspace-scoped.

Documents remains owner of document content/lifecycle.

# 35. Collaboration relation

Comments/activity/notifications target Workspace-scoped resources.

Collaboration owns those facts.

# 36. Automation relation

Automations may be Workspace-scoped and react to Workspace resources.

Automation owns rules/executions.

# 37. Integrations relation

Connections may be Account- or Workspace-scoped depending on product semantics.

A Workspace integration does not make Workspaces own provider synchronization.

# 38. Analytics relation

Analytics may aggregate Workspace-level facts.

Analytics remains derived.

# 39. Workspace settings

Workspace-owned settings should be limited to semantics genuinely attached to Workspace lifecycle/behavior.

Do not put Account-wide SSO/SCIM/Billing/security policy into Workspace settings merely for UI convenience.

# 40. WSP-012 — Workspace settings cannot absorb Account administration

Account-wide domain, IdP, SCIM, region, ownership, and commercial semantics remain with their canonical owners.

# 41. Spaces

A Space can organize Workspace resources if product-approved.

Potential semantics:

```text
hierarchical/organizational container
visibility
status
type
parent relation
```

# 42. WSP-013 — Space is organizational structure, not implicit authorization

Space hierarchy or visibility may influence Governance policy.

It must not become an undocumented permission shortcut.

# 43. Space hierarchy

If hierarchy exists:

- parent must be in same Workspace;
- cycles are forbidden;
- moving between parents preserves Workspace scope.

# 44. Space visibility

Visibility labels such as public/private/internal require explicit Governance relationship.

Do not assume a boolean visibility flag fully implements resource access policy.

# 45. Teams

A Team can group Workspace members for collaboration/organization.

Current source includes Team and TeamMember concepts.

Product semantics should distinguish Team membership from Workspace membership.

# 46. WSP-014 — Team membership requires Workspace membership compatibility

A Team belongs to a Workspace.

A Team Member should reference a principal/member compatible with that Workspace.

Do not allow cross-Workspace Team membership through raw IDs.

# 47. Team role

Team-level role may describe responsibility inside Team.

It is not automatically a Workspace role or universal resource permission.

# 48. Teams and Governance

Governance may use Team identity as a subject/group in permission policy if product-approved.

Workspaces owns Team composition.

Governance owns permission meaning.

# 49. Organizational containers

Spaces and Teams exist only if they provide real product organization value.

Do not create more hierarchy by symmetry.

# 50. Workspace switch

Workspace switch is a scope transition across frontend/backend state.

It is not simply changing one client variable.

# 51. WSP-015 — Workspace switch invalidates old-scope assumptions

On switch:

```text
old subscriptions dispose
old query/cache scope no longer applies
late old HTTP responses must not patch new scope
new membership/authz resolves
new child data loads
```

# 52. Backend scope

Every Workspace-scoped request establishes/validates Workspace identity server-side.

Backend never trusts only a client “active workspace” singleton.

# 53. Frontend scope

Frontend state/query keys/realtime subscriptions include Workspace scope where required.

Selection state is provisional UI context; it cannot override server authorization.

# 54. RLS/persistence

Workspace scope should propagate into persistence/RLS context where the data model requires tenant isolation.

RLS is defense in depth, not the sole membership/permission rule.

# 55. Cache/search/realtime

Workspace identity participates in cache, search, query, event, and realtime scope as required to avoid cross-tenant collisions/leakage.

# 56. WSP-016 — Workspace identity survives every relevant boundary

Scope must not disappear when work moves from:

```text
HTTP
→ Application
→ DB/cache/search
→ outbox/message
→ background worker
→ realtime
→ frontend query cache
```

# 57. Workspace facts/events

Potential stable facts include:

```text
WorkspaceCreated
WorkspaceRenamed
WorkspaceArchived/Suspended
WorkspaceDeleted
WorkspaceRestored
WorkspaceMemberAdded
WorkspaceMemberRoleChanged
WorkspaceMemberRemoved
WorkspaceInvitationAccepted/Revoked
SpaceCreated/Moved
TeamMembershipChanged
```

Only publicize facts with stable consumers.

# 58. WSP-017 — Workspace events carry stable scope, not foreign aggregates

Cross-boundary events should carry stable IDs/facts.

Do not serialize complete Account/User/Board/Page aggregates into Workspace events.

# 59. Account lifecycle effects

Account suspension/closure may restrict Workspace operations.

The Account fact comes from Accounts.

Workspaces owns how its Workspace lifecycle/access reacts according to approved policy.

# 60. Workspace archive

Archive may:

- preserve data;
- block new mutation;
- retain historical access;
- pause integrations/automation.

Exact semantics belong to Workspaces plus participant contexts.

# 61. Workspace deletion

Deletion has cross-context consequences.

It must define:

```text
members
Boards
Documents
Collaboration
Automation
Integrations
Billing association
Analytics
audit
retention/export
```

# 62. WSP-018 — Workspace deletion is a process, not ORM cascade

A database cascade cannot define product retention, legal/export, provider, or historical-reference semantics.

# 63. Restore

If Workspace restore exists, define:

- membership restoration;
- child-resource access;
- integrations/automation;
- deletion window;
- what remains irreversible.

# 64. Cross-context deletion

Workspaces should not directly delete every downstream aggregate.

Use explicit workflow/contracts/events and context-owned retention rules.

# 65. Failure — no membership

Authenticated principal with no valid membership cannot use membership-required Workspace resources.

# 66. Failure — invitation not active

Expired/revoked/accepted invitation cannot be reused as active invitation authority.

# 67. Failure — last owner

Leave/remove/demote that violates last-owner policy fails before mutation.

# 68. Failure — cross-Workspace parent/member

Space/Team relationships must reject resources/members from another Workspace.

# 69. Failure — stale Workspace switch

Old Workspace request/realtime response arriving after switch must not contaminate current scope.

# 70. Failure — entitlement

Workspace creation/member limit/premium feature can fail due to Billing entitlement.

This is different from membership/authorization failure.

# 71. Concurrency

High-risk membership/role/lifecycle changes should avoid silent stale overwrite.

Examples:

- simultaneous owner demotions;
- invitation accept/revoke race;
- Workspace archive vs mutation;
- Team membership changes.

# 72. WSP-019 — Membership and invitation races resolve deterministically

Retry/concurrent acceptance/removal must not produce duplicate active memberships or violate owner invariants.

# 73. Idempotency

Creation/invitation acceptance/member removal may be retried at transport/application level.

Stable operation semantics must prevent duplicated membership/resource effects.

# 74. Workspace onboarding

Conceptual:

```text
Account/principal eligible
→ create Workspace
→ establish owner membership
→ create minimal settings
→ optional product setup through owning context contracts
```

# 75. Invite onboarding

```text
issue Workspace invitation
→ invitee authenticates/resolves Identity
→ validate invitation
→ create/activate membership
→ invitation becomes terminal
```

# 76. Account-to-Workspace provisioning

Accounts/SCIM may provision Workspace memberships through explicit Workspaces operations.

Provisioning does not directly edit Workspace tables from Accounts.

# 77. WSP-020 — External provisioning uses Workspaces contracts

SCIM/Account orchestration can request membership changes.

Workspaces remains responsible for membership invariants.

# 78. Team onboarding

Creating Team establishes Workspace-owned Team identity.

Adding Team Member must respect Workspace membership compatibility.

# 79. Space organization

Moving resources into Spaces requires the target resource/context to support the relationship explicitly.

Space containment does not become a generic cross-context object graph.

# 80. Audit

High-impact Workspace operations may require audit:

- owner/admin change;
- member remove/suspend;
- invitation/security-sensitive changes;
- archive/delete/restore;
- Team/Space policy-relevant changes.

Audit is Governance/security evidence, not ordinary activity.

# 81. Activity

User-facing collaboration activity may report Workspace changes.

It remains Collaboration semantics and may be curated differently from audit.

# 82. Privacy

Workspace membership lists, guest identities, Teams, invitations, and private Spaces can be sensitive.

Queries/search/realtime must enforce scope and authorization.

# 83. Frontend implications

The frontend Workspace feature should own:

- Workspace selection/switch UX;
- Workspace membership/admin surfaces;
- invitations;
- Workspace-owned organizational settings.

It must not absorb Account-wide administration or resource-specific business state.

# 84. Scope indicator

Current Workspace should be visible enough to avoid accidental actions in the wrong tenant.

Especially during:

- member management;
- destructive actions;
- Team/Space organization;
- Workspace settings.

# 85. Empty state

A new Workspace may legitimately contain no Boards/Documents yet.

The empty experience should route creation to the owning product capabilities without implying Workspaces owns those resources.

# 86. Read-only/degraded state

Archived/suspended or unauthorized Workspace should clearly distinguish:

- no membership;
- read-only;
- archived;
- plan-limited;
- temporarily unavailable.

# 87. Service extraction

Workspaces is a semantic extraction seam.

Extraction requires stable membership/authz contracts, tenant propagation, no direct foreign writes, and clear Account/Governance/Billing relationships.

# 88. Current source alignment

Current Domain Workspaces contains:

```text
Workspaces
Members
Invitations
Spaces
Teams
Rules
```

Current source includes `Workspace`, `WorkspaceSettings`, `WorkspaceStatus`, `WorkspaceMember`, `WorkspaceRole`, `WorkspaceInvitation`, `Space`, `Team`, and related status/role concepts.

This supports a richer Workspace organization context than membership alone.

# 89. Source ambiguity watch

Do not normalize:

- WorkspaceRole into universal permission;
- SpaceVisibility into complete authorization;
- TeamRole into WorkspaceRole;
- current Workspace selection into durable Identity state.

Those are different semantic layers.

# 90. Change impact — membership

Review:

```text
Identity
Accounts
Governance
all Workspace-scoped contexts
frontend query/realtime scope
RLS
audit
```

# 91. Change impact — Workspace lifecycle

Review:

```text
Work Management
Documents
Collaboration
Automation
Integrations
Billing
Analytics
retention/export
frontend shell
```

# 92. Change impact — Space/Team

Review resource organization, Governance permissions, cross-Workspace guards, frontend navigation, search, and migration.

# 93. Testing/evidence

Critical tests should cover:

```text
Workspace creation/lifecycle
membership add/remove/suspend/role
last-owner guard
invitation accept/revoke/expiry/retry
cross-Workspace rejection
Space cycles/parentage
Team membership compatibility
tenant propagation
RLS/authz integration
Workspace switch stale-response/realtime behavior
deletion/retention workflows
```

# 94. Stop conditions

Stop rather than guess if:

- authenticated User is treated as Workspace Member automatically;
- Account Member is assumed to belong to every Workspace;
- invitation is treated as membership;
- Workspace role is treated as universal resource authorization;
- Team/Space hierarchy bypasses Governance;
- cross-Workspace references are accepted by raw ID;
- switching Workspace only changes one frontend variable;
- Workspace deletion is implemented only by cascade;
- Account-wide SSO/SCIM/region is being moved into Workspace settings for convenience;
- source conflicts with ownership and no drift classification exists.

# 95. Related canonical owners

```text
docs/product/contexts/accounts.md
docs/product/contexts/identity.md
docs/product/contexts/governance.md
docs/product/contexts/billing.md
docs/product/contexts/work-management.md
docs/product/contexts/documents.md
docs/product/product-model.md
docs/architecture/bounded-context-map.md
docs/architecture/data-ownership-and-consistency.md
backend/docs/architecture/security-tenancy-authorization.md
frontend/docs/architecture/state-query-mutations.md
frontend/docs/architecture/realtime.md
```

# 96. Final Workspaces rule

For every Workspace capability, answer:

```text
Which Workspace is the tenant?
Which Identity participates?
Is there an active membership?
What Workspace role exists?
What Governance permission still applies?
What Account relationship exists?
What Billing entitlement applies?
What child resources are only scoped versus actually owned?
What happens on switch, suspension, archive, or delete?
How is scope preserved through backend/realtime/frontend?
```

The target is:

> **a stable collaboration tenant and membership boundary that scopes product work strongly without becoming the owner of every Workspace-scoped resource or every authorization rule.**

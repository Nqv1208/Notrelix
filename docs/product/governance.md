---
document_id: PROD-GOVERNANCE
document_type: product-context
status: active
owner: governance
applies_to:
  - governance
  - authorization
  - permissions
  - policies
  - roles
  - sharing
  - share-links
  - security-audit
evidence:
  - PRODUCT.md
  - docs/product/product-model.md
  - docs/product/contexts/accounts.md
  - docs/product/contexts/identity.md
  - docs/product/contexts/workspaces.md
  - docs/architecture/bounded-context-map.md
  - docs/architecture/contract-boundaries.md
  - docs/architecture/data-ownership-and-consistency.md
  - backend/src/Notrelix.Domain/Governance/
  - backend/src/Notrelix.Application/
  - backend/tests/
  - frontend/packages/features/governance/
review_on:
  - authorization-model-change
  - permission-vocabulary-change
  - role-or-policy-change
  - share-link-change
  - guest-access-change
  - permission-cache-change
  - audit-semantics-change
  - new-protected-resource-kind
  - account-or-workspace-role-change
---

# Governance Context

> **Governance owns the product semantics for deciding who may do what to which protected resource, under which scope and policy, and for preserving governed security/administrative evidence.**
>
> It does not own authentication, membership lifecycle, commercial entitlement, or the protected business resources themselves.

This document is the canonical product owner for Governance semantics.

Backend security architecture owns exact authorization pipeline/runtime mechanics.

Frontend owns authorization-aware presentation.

Product contexts remain owners of their protected business state.

---

# 1. Mission

Governance gives Notrelix a coherent authorization language across product contexts.

It exists so that protected operations do not devolve into:

```text
if user.Role == "Admin"
```

scattered across:

- handlers;
- endpoints;
- UI components;
- queries;
- background consumers.

Governance turns explicit principal, membership, policy, entitlement, scope, and resource facts into a fail-closed access decision.

---

# 2. Owns

Governance owns product semantics for:

```text
permission action vocabulary
protected resource vocabulary
permission subject vocabulary
permission rules
resource permissions
policy semantics
custom roles where supported
role-permission composition
sharing policy
guest access policy
share-link lifecycle
permission templates where product-approved
authorization decision meaning
governed security/administrative audit meaning
```

Current Domain source contains `Permissions`, `Policies`, `Roles`, `ShareLinks`, and `Templates`, which is strong implementation evidence for these capabilities.

---

# 3. Does not own

```text
credentials, sessions, MFA, OAuth login
→ Identity

Account membership/enterprise administration
→ Accounts

Workspace membership/invitations/Teams/Spaces
→ Workspaces

Board/Item/Field/View state
→ Work Management

Page/Block state
→ Documents

subscription/entitlement/payment
→ Billing

user-facing comments/activity/notifications
→ Collaboration
```

Governance may consume facts from those owners.

It does not absorb their lifecycle.

---

# 4. Governance inputs

A material authorization decision may depend on:

```text
principal
credential/session security
Account scope
Workspace scope
membership
Account/Workspace role
Team/group relation
resource identity
resource lifecycle
resource ownership
permission policy
share-link capability
commercial entitlement
context-specific business constraints
```

Not every decision uses every input.

The inputs remain owned by their source contexts.

---

# 5. GOV-001 — Backend authorization is final

Every material operation over protected data is authorized server-side.

This includes:

```text
command
query
list
search
export
realtime subscription
background action on behalf of a principal
```

Frontend permission guards are UX only.

---

# 6. Queries are protected operations

A query can leak:

- resource existence;
- names;
- membership;
- private configuration;
- sensitive metadata;
- exportable data.

Therefore “read only” is not an authorization exemption.

---

# 7. GOV-002 — Authorization happens before protected data leaks

Do not:

```text
load all rows
→ filter unauthorized objects in memory
```

when the query path can expose or materialize protected data.

Permission/resource scope must be enforced at an appropriate server data-access/application boundary.

---

# 8. Authentication versus authorization

Identity establishes the principal.

Governance decides protected access.

A valid session does not imply access to an arbitrary Account, Workspace, Board, Page, or administrative operation.

---

# 9. Membership versus authorization

Workspaces owns Workspace membership.

Accounts owns Account membership.

Membership can be an authorization input.

Membership is not the entire permission system.

---

# 10. Entitlement versus authorization

Billing answers whether a commercial capability/limit is available.

Governance answers whether a principal may perform an action on a protected resource.

Both may be required.

They remain separate decisions/facts.

---

# 11. GOV-003 — Commercial entitlement does not directly grant resource permission

A paid plan does not automatically mean:

```text
this User may edit this Board
```

Likewise, resource permission cannot create Billing entitlement.

---

# 12. Resource vocabulary

Each protected context should expose stable resource kinds/identities.

Examples:

```text
account
workspace
board
board-item
board-view
page
document
automation
integration-connection
billing-admin surface
```

Exact vocabulary must follow product semantics.

---

# 13. GOV-004 — Protected resource identity is logical and stable

Resource authorization must not rely only on:

- endpoint path;
- UI route;
- database table name;
- CLR class name.

A protected resource kind should survive internal implementation refactors.

---

# 14. Action vocabulary

Actions represent product operations such as:

```text
view
list
create
edit
delete
share
manage-members
manage-permissions
export
configure-security
administer
```

A context may require more specific actions.

Action names should represent product meaning, not HTTP verbs alone.

---

# 15. GOV-005 — Authorization action reflects business operation

`POST`, `PUT`, `DELETE`, or handler class name is not sufficient product authorization vocabulary.

Two mutations may have different risk despite using the same HTTP method.

---

# 16. Subject vocabulary

Authorization subjects may include:

```text
User
Account Member
Workspace Member
Team/group
custom role assignment
share-link/capability
service/system principal
```

Subject identity and subject type must be explicit.

---

# 17. Current Permission source

Current Governance source includes concepts such as:

```text
PermissionAction
PermissionEffect
PermissionLevel
PermissionRule
PermissionScope
PermissionSubjectType
ResourcePermission
FieldPermission
```

This indicates a richer permission model than simple role strings.

Exact current types remain implementation evidence.

---

# 18. Permission rule

A permission rule represents a governed relationship between:

- subject;
- action/capability;
- resource/scope;
- effect/level;
- lifecycle/status

where product-approved.

Rules do not automatically replace inherited role/membership/policy semantics.

---

# 19. GOV-006 — One ACL entry is not the whole authorization system

An explicit resource permission cannot by itself represent every source of authority such as:

```text
Workspace membership
Account administration
inherited role
guest policy
share link
entitlement
resource-specific invariant
```

The final decision composes relevant facts intentionally.

---

# 20. Allow and deny

If Governance supports explicit allow/deny semantics, conflict precedence must be deterministic and documented.

Do not rely on iteration order or whichever rule is loaded last.

---

# 21. Permission level

Permission levels may simplify common capability groups.

They must map deterministically to concrete actions.

A level label must not become an undocumented wildcard.

---

# 22. Permission scope

Permission scope can identify where a rule applies:

```text
Account
Workspace
specific resource
field or subresource where explicitly supported
```

Scope inheritance must be explicit.

---

# 23. GOV-007 — Scope inheritance is explicit

Do not assume:

```text
Account access
→ all Workspace access
→ all resource access
```

unless an approved policy says so.

Similarly, resource sharing does not make linked resources transitively public.

---

# 24. Field-level permissions

Current source contains `FieldPermission`.

Field-level authorization is more granular than ordinary Board/Item permission.

If used, it must define:

- applicable field types;
- read versus edit semantics;
- interaction with hidden/visible UI fields;
- export/search behavior;
- Automation/Integrations behavior.

---

# 25. GOV-008 — Hidden UI field is not a security boundary

If a field is security-restricted, backend query/mutation/export/realtime paths must enforce that policy.

Frontend hiding alone is insufficient.

---

# 26. Policies

Current source contains:

```text
ResourcePolicy
SharingPolicy
GuestAccessPolicy
WorkspacePolicy
```

Policies represent reusable product-level authorization rules around context facts.

They should remain explicit rather than dispersed conditionals.

---

# 27. Resource policy

A Resource Policy defines how a protected resource participates in authorization.

It may use:

- ownership;
- visibility;
- role;
- explicit permission;
- lifecycle;
- sharing.

The protected context still owns the resource state.

---

# 28. Workspace policy

Workspace policy may translate membership/Workspace role into baseline capability.

Workspaces owns membership/role facts.

Governance owns the permission meaning assigned to them.

---

# 29. Guest access policy

Guest access is constrained by default.

A guest/external principal may have access to explicitly shared resources without receiving workspace-wide enumeration.

---

# 30. GOV-009 — Guest access is resource-limited by default

Guest or external access MUST NOT imply:

```text
list every Board
list every Page
enumerate every Workspace member
discover every private resource
```

unless an explicit policy intentionally grants such capability.

---

# 31. Sharing policy

Sharing determines how an owned resource can expose access beyond default membership/policy.

Sharing does not transfer resource ownership to Governance.

---

# 32. Share link

A Share Link represents an explicit access capability associated with a resource/scope.

Current source includes:

```text
ShareLink
ShareLinkAccessMode
ShareLinkStatus
ShareLinkTokenHash
```

This is evidence for a first-class share-link lifecycle.

---

# 33. GOV-010 — Share link is a scoped capability, not global public mode

A Share Link grants only the approved capability to the approved target resource/scope.

It does not make:

- the entire Workspace public;
- linked resources public;
- embedded resources transitively accessible.

---

# 34. Share-link lifecycle

Product semantics should distinguish as applicable:

```text
created
active
expired
revoked
disabled
```

A revoked/expired link must stop authorizing future access.

---

# 35. Share-link secret

A public/share token is security-sensitive.

Persist only a safe verifier/hash where possible.

Do not expose raw reusable token in:

- logs;
- audit payload;
- events;
- generated client state beyond the creation/URL flow that requires it.

---

# 36. GOV-011 — Share-link revocation is authoritative

Revocation must invalidate access even if:

- the client retains an old URL;
- a realtime connection exists;
- a permission decision was cached.

Cache/session/realtime behavior must converge to revoked authority.

---

# 37. Share-link access mode

If a link supports modes such as view/comment/edit, the mode must map to explicit action semantics.

Do not implement “edit link” as a boolean that bypasses every resource-specific rule.

---

# 38. Password-protected links

If supported, link password/proof is a capability guard.

It does not authenticate the visitor as a full Identity unless a separate login occurs.

---

# 39. Share-link expiry

Expiry is evaluated authoritatively server-side.

Client clocks/UI labels do not decide validity.

---

# 40. Linked-resource access

Example:

```text
public Page embeds private Board
```

The Page link does not automatically grant Board permission.

Target authorization is evaluated independently unless an approved compound-sharing rule exists.

---

# 41. Roles

Current Governance source includes:

```text
CustomRole
CustomRolePermission
MemberRoleAssignment
```

This supports governed reusable custom-role semantics beyond hard-coded Workspace role labels.

---

# 42. GOV-012 — Role is a permission composition, not an identity

A custom role groups or names permission semantics.

It does not become:

- User identity;
- Workspace membership itself;
- Account membership itself.

Role assignment attaches the role to a valid subject/context.

---

# 43. Built-in versus custom roles

Built-in roles may originate from Account/Workspace product semantics.

Custom roles may extend authorization vocabulary.

The system must define how built-in and custom roles interact.

---

# 44. Owner, Admin, Member, Guest

These labels are not interchangeable.

Typical product distinction:

```text
Owner
    final lifecycle/security responsibility

Admin
    broad operational administration

Member
    normal collaboration capability

Guest
    constrained/external access
```

Exact matrices may evolve.

High-risk owner semantics remain fail-closed.

---

# 45. GOV-013 — Admin does not silently become Owner

Actions such as:

- deleting/closing critical scope;
- transferring ownership;
- security configuration;
- final-owner removal

must use explicit policy.

Broad Admin capability must not silently include every Owner-only action.

---

# 46. Role assignment

Role assignment requires:

- valid subject;
- valid role;
- compatible Account/Workspace/resource scope;
- assignment lifecycle;
- authorization to assign.

Cross-scope role assignment through raw IDs is forbidden.

---

# 47. Role deletion

Deleting/deactivating a custom role must define what happens to assignments.

Do not leave ambiguous orphaned authorization state.

---

# 48. Permission template

Current Governance source includes `PermissionTemplate` and related definition/entry/scope/status types.

A Permission Template can seed repeatable permission configuration.

---

# 49. GOV-014 — Permission template is creation/configuration input, not hidden live authority

Unless product explicitly defines linked templates, applying a template creates ordinary permission/policy state.

Later editing the template must not silently rewrite all previously configured resources.

---

# 50. Template versioning

If templates evolve, product semantics should distinguish:

- template definition;
- applied permission state;
- migration/re-application.

Do not make runtime authorization depend on a mutable template by accident.

---

# 51. Permission decision

A decision should be semantically one of:

```text
allow
deny
```

with safe reason/context metadata where needed.

Do not expose internal rule stack to unauthorized clients.

---

# 52. GOV-015 — Authorization fails closed

When required facts are:

- missing;
- stale beyond safe use;
- ambiguous;
- unsupported;
- unavailable with no safe fallback,

protected operations must not default to allow.

---

# 53. Policy evaluation

Policy evaluation may combine:

```text
Identity principal
Account facts
Workspace membership/role
Team/group
resource owner/lifecycle
explicit permission
share capability
Billing entitlement
context business prerequisite
```

The order/precedence is governed and testable.

---

# 54. Business invariant versus authorization

Authorization answers whether the caller may attempt the operation.

The owning product context still validates:

- lifecycle;
- domain invariant;
- concurrency;
- semantic preconditions.

Allow does not guarantee business success.

---

# 55. GOV-016 — Governance never mutates protected aggregate as authorization side effect

Authorization should return a decision/context.

It must not:

```text
authorize edit
→ also mutate Board
```

Governance remains separate from target business ownership.

---

# 56. Permission cache

Permission decisions can be cached only with safe identity and invalidation semantics.

Relevant dimensions may include:

```text
Account
Workspace
principal/subject
resource
action
policy/permission version
membership version
entitlement version
```

depending on the decision.

---

# 57. GOV-017 — Permission-sensitive cache is scope/version aware

A cache entry such as:

```text
user123 -> canEdit = true
```

is insufficient when access varies by Workspace/resource/policy version.

Placeholder versions such as `unknown` must not make protected cache fail-open.

---

# 58. Cache invalidation

Changes that can invalidate decisions include:

- member removed/suspended;
- role change;
- policy change;
- permission grant/revoke;
- share-link revoke;
- entitlement change;
- resource lifecycle/ownership change.

The invalidation model must match actual authority.

---

# 59. Realtime authorization

Realtime subscription itself is a protected operation.

Connection establishment does not permanently freeze access for the connection lifetime.

---

# 60. GOV-018 — Realtime access can be revoked while connected

If membership/share/policy changes remove access:

- future events stop;
- subscription/session state converges;
- client should refetch/leave protected state as needed.

Do not keep sending private events until reconnect.

---

# 61. Event payload security

Permission/governance events should not reveal sensitive ACL details to unauthorized consumers.

Event routing and payload must preserve tenant/resource security.

---

# 62. Search/list/export

Authorization must apply to:

```text
single resource
collection
search result
export
analytics/drill-down
```

A bulk endpoint is not exempt.

---

# 63. GOV-019 — Collection authorization is not fetch-all-then-hide

Query design should filter protected resource access in the server query/projection path.

Do not materialize unauthorized records before filtering when this can leak or scale badly.

---

# 64. Export

Export often exposes more data than ordinary screen views.

It may require:

- stronger action permission;
- field-level permission;
- audit;
- rate/size policy.

---

# 65. Audit

Governance owns the product meaning of security/administrative audit.

Audit is append-oriented evidence, not ordinary editable content.

---

# 66. Audit versus activity

```text
Audit
→ governed/security/administrative evidence

Activity
→ user-facing collaborative history
```

They may derive from the same product operation.

They are not interchangeable stores.

---

# 67. GOV-020 — Audit evidence is not mutable Activity

Audit must not be edited/deleted like ordinary comments/activity merely because both record actions.

Retention/privacy policy governs audit lifecycle.

---

# 68. Audit record semantics

A governed audit fact may need:

```text
actor
principal type
operation/action
target/resource
Account/Workspace scope
decision/outcome
time
correlation
safe metadata
```

Do not store reusable secrets.

---

# 69. Audit coverage

High-impact examples include:

- permission grant/revoke;
- role assignment;
- membership/owner change;
- share-link lifecycle;
- security policy change;
- Account/Workspace lifecycle;
- export/delete;
- audit-log access;
- enterprise identity/provisioning configuration.

The exact required set is policy-owned.

---

# 70. GOV-021 — Permission change is auditable when policy requires it

A successful grant/revoke/role/share change should not silently disappear from security history.

Audit recording must follow the committed result.

---

# 71. Denied-operation audit

Some denied attempts may require audit.

Not every ordinary authorization denial must necessarily become durable audit noise.

Policy defines the governed set.

---

# 72. Audit retention

Audit may outlive the source object or membership.

Retention must account for:

- security;
- compliance;
- privacy;
- legal requirements.

Generic soft-delete symmetry is inappropriate.

---

# 73. Governance deletion

Permission rules, role assignments, share links, and policies can be revoked/deactivated/deleted according to their own lifecycle.

Historical audit remains separate.

---

# 74. Governance and Accounts

Accounts owns:

- Account lifecycle;
- Account members/owner relations;
- organization-wide IdP/SCIM/domain/region administration.

Governance uses Account facts to decide protected Account operations.

---

# 75. Governance and Workspaces

Workspaces owns:

- Workspace lifecycle;
- membership;
- Workspace role;
- Team/Space composition.

Governance interprets those facts within authorization policy.

---

# 76. Governance and Identity

Identity proves the principal and credential/security state.

Governance consumes principal facts.

It does not manage password/MFA/session.

---

# 77. Governance and Billing

Billing supplies entitlement/usage/commercial facts.

Governance can include entitlement in a decision.

Billing does not directly grant a resource permission.

---

# 78. Governance and Work Management

Work Management declares protected resource/action semantics such as:

```text
Board view/edit/delete
Item create/edit/delete
Field schema manage
View manage/share
Form publish/respond
Approval decide
```

Governance evaluates access.

Work Management still validates its own invariants.

---

# 79. Governance and Documents

Documents supplies Page/Block/document resource semantics.

A shared Page does not automatically authorize embedded Work Management resources.

---

# 80. Governance and Collaboration

Comments/mentions/activity may require target-resource permission.

Collaboration does not independently reimplement target authorization from role strings.

---

# 81. Governance and Automation

Automation actions execute under an explicit principal/service authorization model.

Automation cannot bypass Governance merely because a rule was previously created by an authorized user.

---

# 82. Governance and Integrations

Integration callbacks/sync operate under explicit connection/service/resource authority.

Provider authentication is not automatically authorization to mutate every mapped resource.

---

# 83. Governance and Analytics

Analytics/reporting must preserve source authorization.

A report cannot expose inaccessible resources merely because data was projected earlier.

---

# 84. Policy versioning

A material policy change may invalidate:

- permission cache;
- active share/realtime access;
- role-derived decisions;
- generated access projections.

Version/invalidation semantics must be explicit.

---

# 85. Permission migration

Changing action/resource vocabulary may require migration of:

```text
stored rules
roles
templates
frontend guards
audit mappings
API resource/action mapping
tests
```

Treat it as contract/data change, not enum-only refactor.

---

# 86. Resource rename/move

Renaming a CLR/domain type does not necessarily rename the logical authorization resource kind.

Changing logical resource identity is a compatibility migration.

---

# 87. New protected resource admission

For a new product resource define:

```text
resource kind
owner context
scope
read/list actions
mutation actions
administration/share actions
default policy
guest behavior
audit-sensitive actions
frontend states
```

before scattering checks across implementation.

---

# 88. GOV-022 — Every new protected resource has explicit authorization vocabulary

A product feature is incomplete if it introduces a protected resource but no coherent resource/action policy.

---

# 89. Default policy

Defaults should be fail-closed enough to prevent accidental broad access.

Do not rely on “everything visible until permission system catches up”.

---

# 90. Migration from role-only authorization

If legacy code uses role strings directly, migrate:

```text
identify product action/resource
→ map current role behavior
→ encode canonical policy
→ add tests
→ remove scattered role checks
```

Do not preserve duplicate authority.

---

# 91. Frontend implications

Frontend may consume:

- permission result;
- capability set;
- read-only reason;
- entitlement distinction.

It should not reproduce server policy independently.

---

# 92. GOV-023 — Frontend capability state is derived

Client-side `canEdit`/`canDelete` state is a presentation/cache projection of server authority.

It must not become independently authored policy.

---

# 93. Permission uncertainty UX

While authorization is unresolved:

- avoid flashing protected content;
- avoid showing destructive controls as active;
- fail toward safe/read-only/loading presentation.

---

# 94. Share-link UX

Share UI should show:

- target;
- access mode;
- expiry;
- status;
- revocation;
- password requirement where supported.

Do not imply transitive access to embedded/linked resources.

---

# 95. Guest UX

Guest users should see only the scope/capabilities they have.

Do not expose Workspace navigation that implies unavailable enumeration.

---

# 96. Audit UX

Audit surfaces should communicate:

- actor;
- action;
- target;
- outcome;
- time/scope.

Do not allow ordinary inline editing of audit evidence.

---

# 97. Current source alignment

Current Governance Domain contains:

```text
Permissions
Policies
Roles
ShareLinks
Templates
```

with current classes including `PermissionAction`, `PermissionRule`, `PermissionScope`, `ResourcePermission`, `FieldPermission`, `ResourcePolicy`, `SharingPolicy`, `GuestAccessPolicy`, `WorkspacePolicy`, `CustomRole`, `MemberRoleAssignment`, `ShareLink`, and `PermissionTemplate`.

This is implementation evidence for a broad authorization/governance context. citeturn333630view0turn333630view1turn333630view2turn851615view0turn851615view1

---

# 98. Current ambiguity watch

Do not normalize these shortcuts if they appear in source:

```text
WorkspaceRole
→ universal permission

SpaceVisibility
→ complete resource security

Billing plan
→ resource permission

share-link visibility
→ transitive public access

frontend hidden field
→ field security
```

Canonical Governance semantics remain richer.

---

# 99. GOV-024 — Authorization is compositional but deterministic

Multiple relevant facts may participate in one decision.

The result must still be deterministic, testable, explainable to operators/developers, and fail closed.

Avoid unbounded ad-hoc policy accumulation.

---

# 100. Testing/evidence

Critical evidence should cover:

```text
allow/deny
authentication vs authorization
Account/Workspace role distinctions
Owner/Admin/Member/Guest
cross-Workspace resource ID rejection
list/search/export filtering
custom role assignment
explicit permission grant/revoke
permission precedence
share-link create/expire/revoke
non-transitive sharing
field-level security where used
permission cache invalidation/version
realtime subscription revocation
audit creation/retention behavior
frontend permission states
```

---

# 101. Change impact — resource/action vocabulary

Review:

```text
all affected product contexts
Application authorization
API resources
frontend capability handling
stored roles/rules/templates
audit
cache
tests
```

---

# 102. Change impact — role/policy

Review:

```text
Accounts
Workspaces
custom roles
share links
guest behavior
frontend settings
permission cache
audit
```

---

# 103. Change impact — sharing

Review:

```text
resource owner
linked/embedded resources
public clients
realtime
search
export
expiry/revocation
audit
```

---

# 104. Change impact — audit

Review:

```text
retention/privacy
Operations
security incident response
Account/Workspace lifecycle
export/delete
frontend audit surfaces
```

---

# 105. Governance checklist

```text
[ ] protected resource owner is explicit
[ ] logical resource kind is stable
[ ] action vocabulary is explicit
[ ] subject type is explicit
[ ] Account/Workspace scope is explicit
[ ] membership is not mistaken for full authorization
[ ] entitlement is not mistaken for permission
[ ] query/list/search/export are protected
[ ] guest behavior is explicit
[ ] sharing is non-transitive unless explicitly designed
[ ] permission cache has scope/version
[ ] audit-sensitive actions are identified
```

---

# 106. New permission feature checklist

```text
[ ] owner context
[ ] resource
[ ] action
[ ] subject
[ ] scope
[ ] default/fallback
[ ] precedence/inheritance
[ ] cache invalidation
[ ] query behavior
[ ] realtime behavior
[ ] audit
[ ] frontend UX
[ ] migration
[ ] tests
```

---

# 107. Stop conditions

Stop rather than guess if:

- protected operation relies on frontend-only guard;
- handler hard-codes a role string without canonical policy justification;
- a query fetches unauthorized resources then hides them;
- Account/Workspace membership is being equated with full resource authorization;
- Billing entitlement is becoming permission;
- share link grants transitive hidden-resource access accidentally;
- permission cache lacks scope/version;
- audit is being treated as editable activity;
- resource/action vocabulary for a new protected capability is undefined;
- custom role/permission precedence is ambiguous.

---

# 108. Related canonical owners

```text
docs/product/product-model.md
docs/product/product-experience.md
docs/product/contexts/accounts.md
docs/product/contexts/identity.md
docs/product/contexts/workspaces.md
docs/product/contexts/work-management.md
docs/product/contexts/documents.md
docs/product/contexts/collaboration.md
docs/product/contexts/automation.md
docs/product/contexts/integrations.md
docs/product/contexts/billing.md

docs/architecture/contract-boundaries.md
docs/architecture/data-ownership-and-consistency.md
docs/architecture/events-realtime-and-delivery-boundary.md

backend/docs/architecture/security-tenancy-authorization.md
```

---

# 109. Final Governance rule

For every protected operation, Notrelix must be able to answer:

```text
Who is the principal?
Which Account/Workspace/resource is targeted?
Which context owns the resource?
What action is being attempted?
Which membership/role/team facts apply?
Which explicit permissions/policies apply?
Does a share capability apply?
Does Billing entitlement matter separately?
What happens if facts are stale/missing?
What must queries/realtime/export hide?
What audit evidence is required?
```

The target is:

> **one coherent, fail-closed authorization language that composes identity, membership, resource, policy, sharing, and entitlement facts without turning Governance into the owner of the protected business state.**

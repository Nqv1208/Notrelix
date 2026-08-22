---
document_id: WRK-TEAM-WORKSPACE-GOVERNANCE
document_type: workstream-team-spec
status: active
owner: workspace-governance-team
applies_to:
  - workspaces
  - governance
  - membership
  - invitations
  - roles
  - permissions
  - resource-authorization
  - share-links
evidence:
  - docs/product/workspaces.md
  - docs/product/governance.md
  - docs/architecture/bounded-context-map.md
  - docs/architecture/contract-boundaries.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/delivery/team-ownership.md
  - docs/workstreams/capability-map.md
  - docs/workstreams/cross-team-dependencies.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - frontend/docs/architecture/state-query-mutations.md
review_on:
  - workspace-capability-change
  - governance-capability-change
  - resource-authorization-change
  - membership-model-change
  - invitation-model-change
  - role-permission-change
---

# Workspace & Governance Workstream

## 1. Purpose

This workstream defines execution for:

- Workspaces;
- Governance.

It exists to establish the stable workspace/resource security frame consumed by all later product teams.

Canonical semantics remain in:

```text
docs/product/workspaces.md
docs/product/governance.md
```

This file must not become a second policy engine specification or a duplicate workspace domain model.

## 2. Team scope

Primary bounded contexts:

```text
Workspaces
Governance
```

Primary responsibility:

- workspace lifecycle;
- workspace identity/slug;
- workspace membership;
- workspace invitations;
- workspace settings;
- role semantics;
- custom roles;
- permissions;
- resource authorization policy;
- share-link governance where canonically owned.

The team does not own:

- user authentication;
- Account lifecycle;
- WorkManagement resource lifecycle;
- Document resource lifecycle;
- Billing entitlement semantics;
- generic authorization pipeline implementation.

## 3. Separation rule

Workspaces and Governance share one team for delivery efficiency.

They remain separate semantic boundaries.

Workspaces owns:

```text
workspace exists
member belongs to workspace
invitation exists
workspace lifecycle
```

Governance owns:

```text
role grants permission
resource/action is allowed or denied
custom role semantics
share policy
```

Do not merge these into one "workspace permissions" domain merely because they interact frequently.

## 4. Capability map

### Workspaces

1. workspace create;
2. workspace read/resolve;
3. workspace update;
4. workspace lifecycle transition;
5. membership read;
6. membership add/remove/change;
7. invitation create;
8. invitation accept/reject/revoke;
9. workspace selection/switch integration;
10. workspace settings.

### Governance

1. resource/action model;
2. built-in roles;
3. custom roles;
4. permission assignment;
5. effective permission evaluation;
6. protected operation integration;
7. share-link lifecycle/policy where owned;
8. governance administration UI;
9. architecture coverage for resource kinds/actions.

## 5. Delivery order

### WG-01 — Workspace baseline

Establish:

- create;
- read/resolve;
- update;
- account containment.

### WG-02 — Membership

Establish:

- member identity;
- member lifecycle;
- member state;
- membership authorization.

### WG-03 — Invitations

Establish:

- invite;
- accept;
- reject/revoke;
- expiration;
- duplicate handling.

### WG-04 — Governance resource model

Establish:

- subject;
- resource kind;
- resource identity;
- action;
- role/permission mapping.

### WG-05 — Built-in permission enforcement

Integrate central authorization pipeline with stable workspace/resource semantics.

### WG-06 — Custom roles

Implement role lifecycle and assignment only after the resource/action model is stable.

### WG-07 — Share governance

Implement share-link behavior where product/governance authority assigns ownership here.

### WG-08 — Hardening

Verify:

- account isolation;
- workspace isolation;
- member removal;
- invite replay;
- stale membership;
- permission denial;
- role mutation;
- cross-resource authorization.

## 6. Backend ownership surfaces

Expected primary areas:

```text
backend/src/Notrelix.Domain/
  Workspaces/
  Governance/

backend/src/Notrelix.Application/
  Features/Workspaces/
  Features/Governance/

backend/src/Notrelix.Infrastructure/
  ...Workspaces...
  ...Governance...

backend/src/Notrelix.API/
  Endpoints/Workspaces/
  Endpoints/Governance/
```

Exact folder names follow source reality.

Do not move policy semantics into Platform merely because authorization is cross-cutting.

## 7. Workspaces Domain responsibilities

Workspaces owns:

- workspace identity;
- slug/name/state;
- account containment;
- membership domain state;
- invitation domain state;
- workspace lifecycle events.

The exact aggregate boundaries must follow canonical domain/source authority.

Do not recreate ownership solely from database relationships.

## 8. Governance Domain responsibilities

Governance owns:

- role semantics;
- custom role semantics;
- permission semantics;
- resource policy semantics;
- share policy where canonical.

Governance MUST NOT own private state of:

- Board;
- BoardItem;
- Page;
- Block;
- Comment;
- Subscription;
- Integration.

It references resources through explicit identities/contracts.

## 9. Application responsibilities

Application coordinates:

- workspace use cases;
- membership/invitation workflows;
- role/permission use cases;
- authorization declarations;
- cross-context contract lookup where approved.

The central authorization pipeline enforces policy.

Handler-local ad hoc permission checks must not become the default pattern.

## 10. Infrastructure responsibilities

Infrastructure may implement:

- Workspace/Governance persistence;
- role/permission storage;
- policy lookup support;
- invitation persistence;
- share token storage/verification mechanism where approved.

Infrastructure does not own permission meaning.

## 11. API responsibilities

API owns:

- endpoint routing;
- transport;
- request/response;
- OpenAPI;
- current actor/account/workspace context adaptation.

API MUST NOT decide effective permissions independently from Application/Governance.

## 12. Frontend ownership surfaces

Frontend primary areas include:

- workspace selector;
- workspace settings;
- member management;
- invitation flows;
- role management;
- permission-aware UI;
- share administration.

Frontend permission checks are UX behavior only.

Backend remains the enforcement authority.

Never treat hidden/disabled UI as security enforcement.

## 13. Producer dependencies

### Identity & Accounts

Required facts:

- authenticated actor;
- account identity;
- subject identity.

Expected readiness:

```text
D5 for actor/account identity contract
D4+ for account transition behavior
```

### Platform/Foundation

Required mechanisms:

- authorization pipeline;
- tenant/account/workspace context infrastructure;
- persistence foundation;
- frontend query/API foundation.

Expected readiness:

```text
D4 VERIFIED
```

for critical protected flows.

## 14. Consumer dependencies

All resource-owning teams consume Governance.

Primary consumers:

- Work Management;
- Documents & Collaboration;
- Automation & Integrations;
- Billing & Entitlements;
- Analytics & Reporting.

Therefore resource/action contract changes are high-impact.

Governance must avoid unstable resource naming conventions that every downstream team must rewrite.

## 15. Workspace/account ownership

Workspace must have one clear containing account/tenant relationship according to canonical product architecture.

Do not allow:

```text
Workspace owns Account state
```

or:

```text
Account owns all Workspace internals
```

without explicit product authority.

Workspace lifecycle and Account lifecycle require explicit cross-context coordination.

## 16. Membership model

Membership capability must define:

- subject identity;
- workspace identity;
- membership status;
- role/permission relationship;
- add/remove/change transitions;
- self-removal behavior;
- last-admin/owner invariant where product semantics require it;
- stale session/access behavior after removal.

Member removal must revoke effective access through backend enforcement.

Frontend cache invalidation alone is insufficient.

## 17. Invitation model

Invitation capability should define:

- inviter;
- target email/subject if applicable;
- workspace;
- intended role/access;
- expiration;
- accepted/rejected/revoked state;
- duplicate invite behavior;
- replay behavior;
- account/identity resolution on acceptance.

Invitation acceptance must not create duplicate memberships.

## 18. Governance subject model

Governance must use stable subject identity.

Potential subjects can include:

- user;
- workspace member;
- service/system actor;
- API token principal;

only where canonical architecture supports them.

Do not invent new subject types inside individual features.

## 19. Resource model

A protected operation must identify:

```text
Subject
ResourceKind
ResourceId
Action
Context
```

The resource-owning context owns:

- resource identity;
- resource lifecycle;
- valid business actions.

Governance owns:

- permission/role semantics;
- grant/deny policy.

Platform/Application owns:

- enforcement mechanism.

## 20. Resource registration handshake

When another team introduces a protected resource:

1. resource team defines the resource and action semantics;
2. Governance reviews policy mapping;
3. resource kind/action representation is added;
4. Application authorization declarations are added;
5. denied/allowed integration tests are added;
6. frontend permission UX consumes the resulting contract if needed.

The resource team cannot silently invent a parallel policy model.

## 21. Built-in roles

Built-in roles should be canonical and stable.

If a built-in role changes effective permission meaning, treat it as a contract/product change.

Do not encode built-in role behavior in scattered feature handlers.

## 22. Custom roles

Custom-role capability should define:

- create;
- rename/update;
- permission assignment;
- delete/archive;
- assignment behavior;
- default/built-in role restrictions;
- invalid permission combinations where applicable.

Changing a custom role should affect authorization through the canonical policy path.

## 23. Effective permission evaluation

Effective permission evaluation should have one authoritative backend path.

Avoid multiple interpretations across:

- API endpoints;
- handlers;
- frontend;
- background jobs.

Frontend may consume permission summaries/capabilities for UX, but backend must re-evaluate for protected operations.

## 24. Share-link governance

Where Governance owns share links, define:

- target resource;
- token/identifier;
- scope;
- expiration;
- revocation;
- anonymous/authenticated behavior;
- permission granted;
- auditability;
- resource-deletion interaction.

Share links must not bypass tenant/resource isolation.

If the resource-owning context instead owns lifecycle semantics, preserve that separation and keep Governance focused on access policy.

## 25. Authorization pipeline contract

The team depends on central Application authorization enforcement.

Business teams declare requirements.

Governance provides policy meaning.

Platform/Application infrastructure provides execution.

Do not implement:

```text
if (user.Role == "Admin")
```

inside arbitrary handlers as a replacement for policy.

## 26. Data ownership

Workspaces owns workspace/membership/invitation persistence.

Governance owns role/permission/policy persistence.

Other contexts reference IDs/contracts.

Governance may need resource metadata for evaluation, but direct private-table reads require explicit architecture approval.

Prefer stable resource descriptors/contracts.

## 27. Cross-context lifecycle

Explicit contracts are required for scenarios such as:

```text
Account disabled
→ Workspace access?

Workspace deleted
→ Boards/Documents/Automations?

Member removed
→ Comments/history attribution?

Resource deleted
→ ResourcePermission/ShareLink cleanup?
```

Do not let relational cascade rules silently define business lifecycle.

## 28. Events

Potential facts include:

- workspace created;
- workspace lifecycle changed;
- member added/removed;
- invitation accepted;
- role changed;
- permission assignment changed;
- share revoked.

Exact events must follow current source/ADR authority.

Resource teams must be considered before changing events they consume.

## 29. Realtime

Possible realtime UX:

- member list changes;
- permission changes;
- workspace settings changes.

Backend authorization must remain correct even if realtime delivery is delayed.

A stale frontend permission view must not grant access.

## 30. Billing dependencies

Billing may depend on:

- account/workspace administration authority;
- role/permission for billing UI;
- workspace/account limits.

Governance may enforce "who can administer billing".

Billing owns "what is entitled/billable".

Do not merge these semantics.

## 31. Automation/Integrations dependencies

Installation or automation administration may require Governance actions.

Governance owns permission.

Automation/Integrations own the capability itself.

Example:

```text
Governance:
Can actor install integration?

Integrations:
How installation works.
```

## 32. Analytics dependencies

Analytics/reporting consumers may need authorization filtering.

Governance defines visibility semantics.

Analytics implements authorized read models/reporting.

Do not export private permission tables as an analytics shortcut.

## 33. Migration considerations

Workspace/Governance migrations must preserve:

- tenant isolation;
- workspace ownership;
- membership uniqueness;
- invitation uniqueness/state;
- role assignment integrity;
- permission integrity;
- share token secrecy;
- rollback/forward-fix path.

Permission migrations require special care because incorrect data can become a security incident even when the schema migration succeeds.

## 34. Test matrix

### Domain tests

Verify:

- workspace invariants;
- membership transitions;
- invitation transitions;
- role invariants;
- permission invariants;
- share policy invariants where modeled.

### Application tests

Verify:

- validation;
- actor/account/workspace requirements;
- policy declarations;
- allowed/denied flows;
- cross-context resource mapping.

### Infrastructure tests

Verify:

- persistence mappings;
- uniqueness;
- tenant/workspace scoping;
- role/permission storage;
- invitation expiration/state;
- share secret storage.

### Architecture tests

Verify:

- no policy bypass pattern where governed;
- bounded-context isolation;
- central authorization pipeline expectations;
- resource registration coverage where architecture gates exist.

### API tests

Verify:

- endpoint contracts;
- forbidden vs unauthorized;
- invalid workspace/account context;
- role/member/invite APIs;
- share behavior.

### Integration tests

Critical flows:

```text
account owner
→ creates workspace
→ invites user
→ user accepts
→ receives expected access

member removed
→ next protected request denied

custom role changed
→ effective permissions change
→ protected endpoint behavior changes

resource permission denied
→ backend rejects even if frontend still displays control
```

### Frontend tests

Verify:

- workspace switching;
- membership UI;
- invitation UX;
- role management;
- permission-aware controls;
- forbidden/error states;
- stale permission refresh behavior.

## 35. Capability blockers

Do not mark capability complete while:

- Identity actor/account contract is unstable;
- central authorization enforcement is bypassed;
- resource ownership is unclear;
- account/workspace tenancy scope is ambiguous;
- frontend treats permission display state as enforcement;
- destructive lifecycle semantics are delegated to DB cascade.

## 36. Team-local decisions

May decide locally:

- internal aggregate helper structure;
- local handler decomposition;
- role editor component composition;
- invitation UI decomposition;
- test fixtures.

May NOT decide locally:

- new tenancy model;
- new identity subject model;
- new central authorization architecture;
- direct private persistence access to another context;
- arbitrary new resource-kind scheme;
- new backend project/service;
- weakening of policy enforcement;
- public contract break without coordination.

## 37. Escalation conditions

Escalate when:

- Accounts vs Workspace containment is unclear;
- membership and Governance role ownership overlap;
- resource kind cannot represent another team's resource cleanly;
- authorization requires synchronous private-table reads across contexts;
- share-link ownership is ambiguous;
- custom roles require semantics not supported by current policy model;
- workspace deletion requires multi-context cascade behavior.

## 38. Parallelization

After WG-01/WG-04 contracts stabilize, safe parallel work includes:

- membership;
- invitations;
- custom role UI;
- permission admin UI;
- resource authorization integration for separate downstream contexts.

Do not allow downstream teams to define their own temporary permission systems while Governance is incomplete.

## 39. Definition of Done

Workspace & Governance foundation is mature enough for broad feature delivery when:

- account→workspace scope is D5 stable;
- actor/member identity is D5 stable;
- central authorization path is D5 stable;
- resource/action contract is D5 stable;
- membership/invitation flows are D4+ verified;
- WorkManagement/Documents can register/use protected resources without private Governance persistence access;
- allowed/denied integration tests pass;
- frontend permission UX does not act as enforcement;
- no architecture gate is weakened.

## 40. Service extraction readiness

Workspaces and Governance remain separate extraction candidates even though one team owns both.

Before extraction, prove:

- independent data ownership;
- explicit identity/account dependency;
- explicit resource/policy contracts;
- no hidden cross-context DB coupling;
- policy latency/failure behavior understood;
- operational ownership;
- observability;
- migration strategy.

Do not split them solely because authorization is widely used.

---
document_id: WRK-TEAM-IDENTITY-ACCOUNTS
document_type: workstream-team-spec
status: active
owner: identity-accounts-team
applies_to:
  - accounts
  - identity
  - authentication
  - sessions
  - account-context
  - account-scoped-frontend-state
evidence:
  - docs/product/accounts.md
  - docs/product/identity.md
  - docs/architecture/bounded-context-map.md
  - docs/architecture/contract-boundaries.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/delivery/team-ownership.md
  - docs/workstreams/capability-map.md
  - docs/workstreams/cross-team-dependencies.md
  - backend/docs/architecture/backend-overview.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/api-and-contracts.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - frontend/docs/architecture/frontend-overview.md
  - frontend/docs/architecture/api-and-contracts.md
  - frontend/docs/architecture/state-query-mutations.md
review_on:
  - accounts-capability-change
  - identity-capability-change
  - authentication-contract-change
  - session-contract-change
  - account-switching-change
  - governance-subject-contract-change
---

# Identity & Accounts Workstream

## 1. Purpose

This workstream defines the execution boundary for the team responsible for:

- Accounts;
- Identity.

It exists to let backend/frontend engineers and coding agents implement feature slices without inventing:

- ownership boundaries;
- account/session semantics;
- authorization ownership;
- frontend account-state behavior;
- cross-team contracts;
- delivery sequencing.

This file is execution guidance.

Canonical product semantics remain in:

```text
docs/product/accounts.md
docs/product/identity.md
```

Canonical system/backend/frontend architecture remains in their respective architecture documents.

## 2. Team scope

Primary bounded contexts:

```text
Accounts
Identity
```

Primary responsibility:

- account lifecycle;
- account context;
- user identity;
- authentication;
- session lifecycle;
- OAuth linkage;
- MFA/security settings;
- API token lifecycle where Identity owns it;
- frontend auth/account flows;
- account-scoped state transition semantics.

The team does not own:

- Workspace lifecycle;
- Governance permission semantics;
- Billing entitlement semantics;
- generic API transport;
- generic query cache infrastructure;
- generic CSRF transport mechanism;
- deployment/runtime architecture.

## 3. Delivery objectives

The team should establish a stable identity/account plane that downstream product teams can consume.

Target outcome:

```text
Identity established
        ↓
Account context resolved
        ↓
Workspace context selected
        ↓
Protected product flow
```

The three steps above MUST remain distinguishable.

Do not collapse them into one ambiguous "current context" abstraction unless canonical architecture explicitly requires it.

## 4. Capability map

### Accounts

1. account create;
2. account read;
3. account update;
4. account lifecycle transition;
5. account-context resolution;
6. account switch/transition behavior;
7. account-scoped frontend state isolation;
8. account-level administration integration.

### Identity

1. session bootstrap;
2. login;
3. logout;
4. session expiration;
5. OAuth start;
6. OAuth callback;
7. provider linking/unlinking where supported;
8. MFA enrollment;
9. MFA challenge;
10. MFA recovery;
11. security settings;
12. API token lifecycle;
13. identity profile read/update where canonical Identity owns it.

## 5. Delivery order

Recommended sequence:

### IA-01 — Session contract stabilization

Establish:

- authentication transport;
- session bootstrap;
- session expiration contract;
- browser credential handling;
- CSRF contract alignment.

This is a prerequisite for expanding protected browser mutations.

### IA-02 — Identity baseline

Implement/verify:

- login;
- logout;
- current identity;
- session expired behavior.

### IA-03 — Account resolution

Implement/verify:

- account context resolution;
- account selection;
- account transition.

### IA-04 — Account state isolation

Prove that frontend server state cannot leak across account transitions.

### IA-05 — OAuth

Implement/verify:

- OAuth start;
- callback;
- PKCE/state/nonce handling;
- provider link/unlink semantics where supported.

### IA-06 — MFA/security

Implement/verify:

- enrollment;
- challenge;
- recovery;
- security settings.

### IA-07 — API tokens

Implement/verify API token lifecycle according to Identity ownership and current security architecture.

### IA-08 — Hardening

Verify:

- concurrency;
- session invalidation;
- tenant/account isolation;
- authz denial;
- invalid OAuth state;
- stale account context;
- frontend error/session transitions.

## 6. Backend ownership surfaces

Expected primary areas:

```text
backend/src/Notrelix.Domain/
  Accounts/
  Identity/

backend/src/Notrelix.Application/
  Features/Accounts/
  Features/Identity/

backend/src/Notrelix.Infrastructure/
  ...Accounts...
  ...Identity...

backend/src/Notrelix.API/
  Endpoints/Accounts/
  Endpoints/Identity/
```

Exact local folders may differ.

The invariant is bounded-context/layer ownership, not spelling of every folder.

## 7. Backend Domain responsibilities

Accounts Domain owns:

- account identity;
- account lifecycle state;
- account invariants;
- account-domain events.

Identity Domain owns:

- user identity state;
- authentication-related domain state where modeled;
- MFA/security settings state;
- API token domain state where modeled;
- Identity-domain events.

Domain MUST NOT depend on Infrastructure/API.

Domain MUST NOT encode frontend navigation/session storage behavior.

## 8. Application responsibilities

Application owns use-case orchestration.

Expected responsibilities include:

- command/query handlers;
- validation;
- authorization declaration;
- current actor/account requirements;
- interfaces for persistence/external providers;
- application-level failure mapping.

Authorization enforcement remains pipeline-owned.

Handlers MUST NOT invent local authorization bypasses.

## 9. Infrastructure responsibilities

Infrastructure may implement:

- account/identity persistence;
- OAuth/provider adapters;
- token/session persistence mechanisms;
- security-provider adapters;
- technical credential storage;
- persistence mappings.

Infrastructure MUST NOT redefine account/identity business rules.

## 10. API responsibilities

API owns:

- transport;
- endpoint mapping;
- request/response shape;
- cookie/header integration;
- OpenAPI exposure;
- auth middleware integration.

API MUST NOT become the canonical owner of identity/account business semantics.

## 11. Frontend ownership surfaces

Expected primary business packages include the existing auth/account feature areas.

Frontend responsibilities:

- session bootstrap consumption;
- login/logout UX;
- account selection/switch UX;
- OAuth redirects/callback UX;
- MFA flows;
- account administration UX;
- session-expired handling;
- account-scoped query/mutation behavior.

Shared generic runtime/query/API client logic remains Platform/Foundation-owned.

## 12. Frontend account-state isolation

This is a mandatory architecture-hardening item.

Before account switching is considered complete, prove one of these models:

### Model A — hard reset

Account transition destroys all account-scoped server state before the new account becomes active.

### Model B — explicit query identity

Every account-scoped query identity includes account identity.

Example conceptually:

```text
["account", accountId, ...resourceKey]
```

### Model C — another formally documented mechanism

Any other approach requires equivalent proof that data from Account A cannot be reused in Account B.

Do not rely on "the UI always reloads" without executable evidence.

## 13. Session and CSRF contract blocker

Known issue:

Backend/frontend CSRF naming/transport semantics have previously shown mismatch.

Affected areas:

- protected browser mutations;
- login/session security flows;
- refresh/session continuation behavior.

This workstream MUST NOT normalize the mismatch by documenting whichever side happens to exist.

Required resolution:

1. identify canonical browser session/CSRF contract;
2. align producer and consumer;
3. update tests;
4. update generated/public contract evidence where applicable;
5. remove stale implementation assumptions.

Until resolved, affected slices remain `BLOCKED` or `CONTRACTED`, not `DONE`.

## 14. Producer dependencies

### Platform/Foundation

Required mechanisms:

- authentication/session middleware;
- generic API client behavior;
- CSRF transport;
- query foundation;
- tenant/account context propagation;
- observability.

Expected readiness before protected-flow completion:

```text
D4 VERIFIED
```

### Billing & Entitlements

Accounts may be the billable tenant identity.

Required contract:

- Billing references account identity;
- Accounts does not absorb plan/entitlement rules.

### Workspace & Governance

Identity/account subjects are consumed by workspace membership and policy.

Required contract:

- actor/subject identity stable;
- account scope stable;
- no duplicate identity store.

## 15. Consumer dependencies

Downstream consumers include:

- Workspace & Governance;
- Billing & Entitlements;
- Work Management;
- Documents & Collaboration;
- Automation & Integrations;
- Analytics & Reporting.

The team must treat changes to identity/account IDs, session claims, account context, and actor representation as cross-team contract changes.

## 16. Authorization model

Identity/account operations must use the central authorization pipeline.

Resource/action semantics should identify:

- actor;
- account resource where applicable;
- identity/security resource where applicable;
- required action.

Do not embed policy logic directly in API endpoints.

Do not use "authenticated" as a substitute for "authorized".

## 17. Data ownership

Accounts owns account-domain state.

Identity owns identity-domain state.

Other contexts may reference stable identifiers/contracts.

They must not own private Accounts/Identity tables.

Account deletion/deactivation effects on:

- workspaces;
- subscriptions;
- identities;
- integrations

require explicit lifecycle contracts rather than database cascades chosen for convenience.

## 18. API contracts

Contract-affecting work should be classified as:

- additive;
- breaking;
- internal;
- public/external.

For browser session APIs, explicitly specify:

- cookie behavior;
- credential mode;
- CSRF requirement;
- expiration behavior;
- unauthorized/session-expired response behavior.

Frontend MUST consume the approved contract rather than maintain a divergent handwritten interpretation.

## 19. OAuth contract

OAuth execution must account for:

- provider;
- state;
- nonce;
- PKCE verifier where applicable;
- callback expiration;
- return URL validation;
- provider identity mapping;
- existing-linked-account behavior;
- replay prevention.

The team MUST NOT weaken OAuth security to simplify provider integration.

Provider-specific transport belongs in Infrastructure/Integrations as appropriate; Identity owns identity linkage semantics.

## 20. MFA/security contract

MFA work should define:

- enrollment state;
- verification;
- recovery;
- disable/reset behavior;
- session impact;
- audit/security event implications.

Administrative MFA reset must have explicit authorization semantics.

## 21. API token contract

If API tokens are supported, define:

- token creation;
- display-once semantics where applicable;
- hashing/storage model;
- scope/permission model;
- revocation;
- expiration;
- actor/account association;
- auditability.

Never persist or expose raw token material beyond the minimum required lifecycle.

## 22. Events

Candidate domain/integration facts may include:

- account created;
- account lifecycle changed;
- identity created;
- security setting changed;
- OAuth account linked;
- token revoked.

Exact event existence and naming must follow current canonical source/ADR design.

Do not create events solely because another team wants a convenient callback.

The source context owns event meaning.

## 23. Realtime

Identity/account features are not inherently realtime-first.

Use realtime only where product semantics require it.

Potential cases:

- session/security invalidation;
- account administration state change.

Generic realtime transport remains Platform-owned.

## 24. Migration considerations

Data migrations must explicitly handle:

- account/tenant isolation;
- user/account association;
- session/token invalidation;
- OAuth provider identity uniqueness;
- backward compatibility;
- rollback/forward-fix strategy.

A schema migration is not considered complete until application compatibility is verified.

## 25. Test matrix

### Domain tests

Verify:

- account invariants;
- identity/security invariants;
- lifecycle transitions;
- invalid transitions;
- token/security state where modeled.

### Application tests

Verify:

- handler orchestration;
- validation;
- authz declarations;
- missing account/actor;
- provider failure paths.

### Infrastructure tests

Verify:

- persistence mapping;
- uniqueness;
- provider adapter behavior;
- token/session storage behavior;
- migration compatibility.

### API tests

Verify:

- OpenAPI;
- session/cookie contract;
- CSRF;
- validation errors;
- unauthorized/forbidden behavior.

### Integration tests

Verify:

- login/session;
- account resolution;
- account isolation;
- OAuth;
- MFA;
- API token lifecycle;
- tenant isolation.

### Frontend tests

Verify:

- session bootstrap;
- login/logout;
- session expiration;
- account switch;
- account-scoped cache isolation;
- OAuth callback state;
- MFA UX;
- permission/error states.

### E2E

Critical flows should include at minimum:

```text
login
→ resolve account
→ enter protected app

account A
→ switch to account B
→ no account A data survives

session expires
→ app receives typed session-expired behavior
→ host owns navigation/recovery
```

## 26. Required evidence before DONE

A slice cannot be `DONE` unless applicable evidence includes:

- product authority reference;
- backend tests;
- frontend tests;
- architecture gates;
- contract/OpenAPI evidence;
- integration/E2E;
- docs update if governed behavior changed;
- no unresolved account/session isolation issue.

## 27. Team-local decisions

May decide locally:

- private handler decomposition;
- private mapper/helper layout;
- local UI component composition;
- test fixture structure;
- local non-public validation helper.

May NOT decide locally:

- new auth framework;
- new session architecture;
- new account boundary;
- new tenant model;
- direct Governance persistence access;
- public contract break;
- new service;
- bypass of central authorization;
- new generic frontend cache architecture.

## 28. Escalation conditions

Escalate when:

- Account and Workspace ownership overlap is unclear;
- authenticated identity and account subject semantics diverge;
- session/CSRF contract cannot be reconciled without breaking consumers;
- account switch requires global frontend architecture change;
- OAuth provider logic requires new cross-cutting security mechanism;
- API token permissions overlap Governance policy;
- Account deletion requires cross-context destructive behavior.

## 29. Parallelization

Safe parallel work after IA-01 contract is stable:

- account CRUD;
- identity profile;
- OAuth adapter work;
- MFA domain/application work;
- API token domain/application work.

Do not parallelize frontend protected flows against an unstable session/CSRF contract.

## 30. Definition of Done

Identity & Accounts workstream is sufficiently mature for downstream teams when:

- session contract is D5 stable;
- account context contract is D5 stable;
- account switching/isolation is D4+ verified;
- actor/account identifiers are stable;
- Workspace/Governance can consume identity/account contracts without private persistence access;
- Billing can associate entitlements/subscriptions without owning Account state;
- critical auth/account E2E passes;
- no architecture gate is weakened to enable delivery.

## 31. Service extraction readiness

Accounts and Identity remain separate extraction candidates even though one team owns both.

Do not extract them merely because the team boundary exists.

Before extraction, prove:

- separate data ownership;
- explicit inbound/outbound contracts;
- session/identity operational boundary;
- tenant/account propagation;
- failure behavior;
- no private cross-context persistence dependency;
- observability;
- migration plan.

Until then, implement inside the current backend production projects.

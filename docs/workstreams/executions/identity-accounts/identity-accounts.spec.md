---
document_id: WRK-SPEC-IDENTITY-ACCOUNTS
document_type: workstream-spec
status: active
owner: identity-accounts-team
applies_to:
  - backend
  - identity
  - accounts
  - actor
  - users
  - profiles
  - sessions
  - oauth
  - mfa
  - security
  - api-tokens
  - account-context
  - tenant-isolation
evidence:
  - docs/product/accounts.md
  - docs/product/identity.md
  - docs/architecture/bounded-context-map.md
  - docs/architecture/contract-boundaries.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - docs/delivery/team-ownership.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/capability-map.md
  - docs/workstreams/cross-team-dependencies.md
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
  - identity-boundary-change
  - account-boundary-change
  - actor-contract-change
  - session-contract-change
  - authentication-model-change
  - account-context-change
  - oauth-contract-change
  - mfa-contract-change
  - api-token-contract-change
  - identity-account-event-change
  - downstream-consumer-change
---

# SPEC — Identity & Accounts

## 1. Purpose

This specification defines the complete target capability contract for the Identity & Accounts team.

It is the master WHAT document for the first business capability on the backend critical path.

It defines what must be true before Identity & Accounts can be certified as a stable producer for:

- Workspace & Governance;
- Billing & Entitlements;
- WorkManagement;
- Documents & Collaboration;
- Automation & Integrations;
- Analytics & Reporting;
- Platform runtime consumers.

This specification does not define implementation order in source.

Implementation order belongs to:

```text
identity-accounts.plan.md
```

Verification traceability belongs to:

```text
identity-accounts.tests.md
```

Certification evidence belongs to:

```text
identity-accounts.certification.md
```

## 2. Why Identity & Accounts is first

All downstream business contexts need stable answers to:

```text
Who is acting?
Who is the user?
What session proves the actor?
Which Account owns the request?
Which Account is active?
How is Account/Tenant scope represented?
What happens when identity/session/account state changes?
```

If those answers are unstable, downstream contexts will independently create:

- current-user abstractions;
- Account/Tenant identifiers;
- session assumptions;
- authorization subject mappings;
- account-scoped persistence/query conventions;
- security workarounds.

This specification prevents that divergence.

## 3. Authority boundary

This SPEC is subordinate to:

```text
Product authority
System architecture
Backend architecture
Accepted ADRs
Team ownership
Backend roadmap
```

If source contradicts higher authority, the contradiction is source debt.

If higher authorities contradict each other, implementation must stop until the conflict is resolved.

The coding agent MUST NOT choose whichever interpretation is easiest to implement.

## 4. Current source evidence boundary

The current backend Domain source visibly contains an `Identity` module with subareas conceptually covering:

```text
Mfa
OAuth
Profiles
Security
Sessions
Tokens
Users
```

Phase 0/1 inventory (PR-IA-00) classified this as `DOC_STALE`: the Domain DOES expose a canonical Accounts module (`backend/src/Notrelix.Domain/Accounts/`) and Application exposes Accounts abstractions/services (`backend/src/Notrelix.Application/Features/Accounts/`), persisted in the `account.*` schema with RLS policies.

This SPEC therefore distinguishes:

```text
target bounded-context capability
```

from:

```text
current physical module placement
```

It does NOT authorize creation of a new `Accounts` production project or folder merely for symmetry.

The implementation PLAN must first determine where current Account semantics live and whether they must be:

- retained;
- rehomed;
- split;
- introduced;
- or represented through an existing canonical model.

## 5. Physical architecture constraint

The backend remains:

```text
Notrelix.Domain
Notrelix.Application
Notrelix.Infrastructure
Notrelix.Platform
Notrelix.API
```

This SPEC MUST NOT be interpreted as permission to create:

```text
Notrelix.Accounts
Notrelix.Identity.Service
Notrelix.Auth
```

production projects.

Bounded contexts remain logical/module boundaries inside the existing architecture until an extraction ADR says otherwise.

## 6. Capability ownership split

Identity owns:

```text
User identity
Actor identity semantics
Profile identity data where canonical
Authentication identity linkage
Session business state
MFA/security identity state
OAuth identity linkage
API token principal state where canonical
Identity lifecycle facts
```

Accounts owns:

```text
Account identity
Account lifecycle
Billable/tenant Account semantics
Account-level state
Current Account business context
Account lifecycle facts
```

Platform owns mechanisms such as:

```text
cookie/header transport
CSRF mechanism
runtime context propagation
authorization enforcement mechanism
secret storage mechanism
generic observability
```

Workspace owns:

```text
Workspace
Workspace membership
Workspace invitation
```

Governance owns:

```text
role
permission
policy
resource authorization semantics
```

These boundaries MUST remain explicit even when code currently co-locates responsibilities.

## 7. Explicit non-ownership

Identity & Accounts MUST NOT own:

- Workspace lifecycle;
- WorkspaceMember business state;
- role/permission policy;
- Board/BoardItem;
- Page/Block;
- Comment;
- Automation rules;
- Integration connections;
- Billing Plan/Subscription/Entitlement;
- Analytics metrics;
- generic message transport;
- generic authorization engine;
- generic realtime transport.

## 8. Capability map

The complete Identity & Accounts capability set is:

```text
IA-CORE-01 User identity
IA-CORE-02 Actor contract
IA-CORE-03 Profile identity
IA-CORE-04 Session lifecycle
IA-CORE-05 Session invalidation
IA-CORE-06 Account identity
IA-CORE-07 Account lifecycle
IA-CORE-08 Current Account resolution
IA-CORE-09 Account/Tenant isolation
IA-CORE-10 Identity ↔ Account contract

IA-AUTH-01 Password/local authentication where supported
IA-AUTH-02 Authentication failure semantics
IA-AUTH-03 Session bootstrap/current identity
IA-AUTH-04 Logout
IA-AUTH-05 Session expiry
IA-AUTH-06 Credential/session revocation

IA-OAUTH-01 OAuth provider contract
IA-OAUTH-02 OAuth start
IA-OAUTH-03 OAuth callback
IA-OAUTH-04 Link provider identity
IA-OAUTH-05 Unlink provider identity
IA-OAUTH-06 OAuth replay/state protection
IA-OAUTH-07 OAuth migration/recovery

IA-MFA-01 MFA capability model
IA-MFA-02 MFA enrollment
IA-MFA-03 MFA challenge
IA-MFA-04 MFA recovery
IA-MFA-05 MFA disable/reset
IA-MFA-06 MFA session impact

IA-SEC-01 Security settings
IA-SEC-02 Sensitive-operation verification
IA-SEC-03 Session/device visibility where product-defined
IA-SEC-04 Security event production
IA-SEC-05 Security state recovery

IA-TOK-01 API token lifecycle
IA-TOK-02 Token secret issuance
IA-TOK-03 Token verification
IA-TOK-04 Token revocation
IA-TOK-05 Token expiry
IA-TOK-06 Token scope/principal mapping
IA-TOK-07 Token auditability

IA-X-01 Workspace/Governance consumer contract
IA-X-02 Billing consumer contract
IA-X-03 Platform context contract
IA-X-04 Automation/system-actor contract
IA-X-05 Analytics identity-data contract
```

These identifiers are workstream requirement groupings, not governance rule IDs.

# Core identity

## 9. IAREQ001 — Stable User identity

The system MUST expose one stable canonical User identity for downstream contracts.

The identity MUST:

- remain stable across authentication mechanisms;
- not depend on email remaining unchanged;
- not depend on OAuth provider username;
- not depend on frontend username/display name;
- be safe to reference from downstream contexts.

Email, provider subject, username and display name MUST NOT silently become interchangeable primary identities.

## 10. IAREQ002 — User identity uniqueness

Identity MUST define uniqueness for each login/discovery identifier that is actually supported.

Examples may include:

- normalized email;
- local username;
- provider subject + provider;
- API token identity.

Uniqueness MUST be enforced at a layer that remains correct under concurrency.

Frontend validation alone is insufficient.

## 11. IAREQ003 — User lifecycle

User lifecycle MUST define supported states.

The exact model follows canonical source/product authority.

Possible concepts may include:

```text
active
disabled
deleted/tombstoned
pending verification
```

This SPEC does not invent a new status enum.

The implementation must preserve existing accepted semantics or escalate if they are insufficient.

## 12. IAREQ004 — Identity deletion/deactivation

Identity deactivation/deletion MUST explicitly define effects on:

- active sessions;
- OAuth links;
- MFA methods;
- API tokens;
- Account relationships;
- historical attribution;
- downstream references.

Database cascades MUST NOT silently define lifecycle semantics.

## 13. IAREQ005 — Historical attribution

A User becoming disabled/deleted MUST NOT automatically destroy historical business attribution in:

- WorkManagement;
- Documents;
- Collaboration;
- Governance audit;
- Billing records;
- Analytics

unless canonical retention policy explicitly requires it.

Downstream contexts should reference stable identity, not mutable profile fields as historical keys.

# Actor contract

## 14. IAREQ006 — Actor is not HTTP identity

Application/Domain code MUST NOT depend on raw HTTP concepts to represent the actor.

The canonical Actor contract must be usable by:

- API requests;
- background jobs;
- message consumers;
- API tokens;
- system operations

according to supported architecture.

## 15. IAREQ007 — Actor principal types

Supported actor/principal types MUST be explicit.

Potential types may include:

```text
User session
API token principal
System/background actor
```

Only types present in canonical architecture may be implemented.

The coding agent MUST NOT invent service principals or super-admin actors as shortcuts.

## 16. IAREQ008 — Actor identity vs User identity

The model MUST distinguish when necessary:

```text
User
```

from:

```text
current acting principal
```

For example, an API token may be associated with a User but is not necessarily semantically identical to a browser session.

## 17. IAREQ009 — Actor trust boundary

Actor identity MUST only be created from trusted authentication/runtime mechanisms.

Client-provided actor IDs MUST NOT be accepted as proof of identity.

# Profile identity

## 18. IAREQ010 — Profile ownership

Identity may own profile fields that describe the User rather than Workspace-specific membership state.

Examples may include:

- display name;
- avatar;
- locale/timezone where canonical;
- personal identity preferences.

Workspace-specific role/title/member metadata belongs to Workspace/Governance where defined.

## 19. IAREQ011 — Profile mutation authorization

Profile mutation MUST distinguish:

- self-service mutation;
- administrative mutation if supported;
- immutable/security-sensitive identity fields.

Not every profile field should automatically be self-editable.

# Authentication

## 20. IAREQ012 — Authentication mechanisms converge on User identity

All supported authentication mechanisms MUST resolve to the same canonical User model.

Supported mechanisms may include:

- local credentials;
- OAuth;
- MFA as secondary factor;
- API tokens as non-browser principal.

They MUST NOT create parallel incompatible user stores.

## 21. IAREQ013 — Authentication success contract

Successful authentication MUST result in a canonical authenticated Actor/session state according to architecture.

The response/transport mechanism belongs partly to Platform/API.

Identity owns the meaning of authenticated state.

## 22. IAREQ014 — Authentication failure contract

Authentication failure MUST avoid leaking sensitive account existence information beyond accepted product/security policy.

Failure classes should be stable enough for API/frontend behavior without exposing:

- password validity details;
- MFA secret information;
- OAuth internal details;
- provider token material.

## 23. IAREQ015 — Credential storage

Raw passwords, OAuth secrets and raw API token secrets MUST NOT be persisted in reversible/plain form unless an explicit external-provider protocol requires protected secret storage.

Password hashing/credential protection belongs to security infrastructure.

Identity owns the credential lifecycle semantics.

# Sessions

## 24. IAREQ016 — Session is first-class identity state

If current architecture models sessions, Session lifecycle MUST remain explicit rather than being reduced to a stateless token assumption.

Session state must support the product/security requirements already accepted by source and docs.

## 25. IAREQ017 — Session creation

Session creation MUST define:

- User;
- Actor/principal;
- issued time;
- expiry;
- revocation state;
- authentication method/context where needed;
- security metadata only where product-defined.

## 26. IAREQ018 — Session bootstrap

The system MUST expose a deterministic way for a client/application to establish:

```text
authenticated?
who is the User?
what session state matters?
```

without requiring downstream features to reimplement authentication probing.

## 27. IAREQ019 — Session expiry

Session expiry semantics MUST be unambiguous.

Expired sessions MUST:

- cease authorizing protected actions;
- produce stable API/runtime behavior;
- not be treated as ordinary authorization denial;
- support frontend/application recovery through the generic Platform contract.

## 28. IAREQ020 — Logout

Logout MUST define whether it revokes:

- current session only;
- all sessions;
- selected sessions

according to the requested operation.

A logout response before durable revocation completes MUST NOT create a security gap if revocation is meant to be authoritative.

## 29. IAREQ021 — Session revocation

Revocation MUST be effective for subsequent protected requests.

Revoked session state must not remain valid due solely to stale cache.

If revocation caching exists, the maximum stale security window must be explicitly acceptable.

## 30. IAREQ022 — Session concurrency

Concurrent session creation/revocation MUST preserve invariants.

Examples:

- revoke all sessions while one refreshes;
- logout while another request is in flight;
- disable User while sessions are active.

## 31. IAREQ023 — Session security metadata

If the current product exposes device/session visibility, metadata must be:

- privacy-conscious;
- non-authoritative for identity;
- safe to display;
- not based on untrusted client labels without qualification.

Do not invent device fingerprinting unless already product-approved.

# Account capability

## 32. IAREQ024 — Account is a business boundary

Account MUST represent the canonical business/tenant boundary described by product architecture.

It MUST NOT be confused with:

- User account credentials;
- Workspace;
- Billing subscription;
- OAuth provider account.

Terminology in source must be reconciled during PLAN if "account" is overloaded.

## 33. IAREQ025 — Account identity

Account MUST have a stable canonical identity usable by:

- Workspaces;
- Billing;
- Governance;
- Analytics;
- tenant isolation;
- frontend/runtime context.

## 34. IAREQ026 — Account lifecycle

Account lifecycle MUST define supported operations/states.

At minimum, source/product authority must clarify:

- create;
- read;
- update;
- disable/archive/delete where supported;
- restoration if supported.

This SPEC does not invent restoration semantics.

## 35. IAREQ027 — Account ownership/administration semantics

The system MUST define who can initially administer an Account.

This is not the same as long-term Governance role policy.

Bootstrap semantics may require a founding User/owner relationship, but ongoing Workspace/Governance policy must not be duplicated in Accounts.

## 36. IAREQ028 — Account and User relationship

The system MUST explicitly define whether:

- a User may belong to multiple Accounts;
- an Account may contain multiple Users;
- membership is represented in Accounts or another context;
- Workspace membership is separate.

The implementation MUST follow canonical product/architecture authority.

It MUST NOT infer membership semantics from foreign-key shape.

## 37. IAREQ029 — Account billable identity

Billing MUST be able to reference one stable billable Account identity.

Accounts MUST NOT absorb Billing Plan/Subscription/Entitlement semantics.

## 38. IAREQ030 — Account lifecycle downstream effects

Account disable/delete MUST define cross-context consequences for:

- Workspaces;
- Billing;
- Automation;
- Integrations;
- Documents;
- WorkManagement;
- Analytics.

A single database cascade MUST NOT be used as implicit policy.

# Current Account and tenant context

## 39. IAREQ031 — Current Account is explicit

The system MUST define how an Actor operates within an Account context.

Current Account MUST NOT be inferred from arbitrary resource IDs alone.

## 40. IAREQ032 — Account context resolution

For a protected request requiring Account scope, the runtime/Application boundary MUST be able to determine the Account through an approved trusted mechanism.

The exact transport may be:

- route;
- resource;
- header;
- session selection;
- another accepted mechanism.

This SPEC does not choose the transport.

## 41. IAREQ033 — Account context authorization

Possessing an Account ID is not proof that an Actor may operate in that Account.

Resolution and authorization are separate concerns.

## 42. IAREQ034 — Tenant isolation

Every Account-scoped path MUST preserve:

```text
Account A state cannot be read or mutated as Account B
```

This applies to:

- commands;
- queries;
- persistence;
- cache;
- background jobs;
- events;
- API token principals;
- downstream integrations.

## 43. IAREQ035 — Account context in background work

Background execution MUST carry or resolve Account context explicitly.

Forbidden shortcuts include:

```text
null Account means all Accounts
default Account
first Account
global administrator bypass
```

unless an explicit system operation is approved.

## 44. IAREQ036 — Account switch semantics

Where users can switch Accounts, the semantic transition must define:

- source Account;
- target Account;
- authorization;
- active-context change;
- stale state handling;
- downstream client/runtime implications.

Frontend mechanics live outside backend Identity/Accounts but the business contract must be stable.

# Identity ↔ Account consumer contract

## 45. IAREQ037 — Minimal stable consumer contract

Workspace/Governance must be able to consume stable identifiers for:

```text
Actor/User
Account
```

without depending on private Identity persistence.

## 46. IAREQ038 — No private persistence contract

Downstream contexts MUST NOT depend on:

- Identity EF entities;
- Identity tables;
- Account private tables;
- authentication provider DTOs.

They consume stable IDs/contracts/events.

## 47. IAREQ039 — Consumer-safe profile access

If downstream features need User display information, they must use an approved read contract/projection rather than copying mutable profile data into every context unless product semantics require snapshots.

# OAuth

## 48. IAREQ040 — OAuth provider identity contract

OAuth linkage MUST identify external identity by a stable provider-owned subject/identifier appropriate to the provider protocol.

Provider email/username alone MUST NOT be treated as universally stable identity unless protocol/product explicitly guarantees it.

## 49. IAREQ041 — OAuth start

OAuth start MUST define:

- provider;
- state;
- PKCE where applicable;
- nonce where applicable;
- return/callback target;
- expiry;
- initiating client/session context.

## 50. IAREQ042 — OAuth callback

Callback MUST validate:

- expected provider;
- state;
- PKCE/nonce where applicable;
- expiry;
- replay;
- safe return target;
- provider token response.

## 51. IAREQ043 — OAuth link vs login

The system MUST distinguish:

```text
authenticate existing linked identity
```

from:

```text
link provider to an already authenticated User
```

These flows have different security risks.

## 52. IAREQ044 — OAuth collision handling

The model MUST define behavior when:

- provider identity already linked to another User;
- returned email matches another User;
- current User already has provider linked;
- provider changes email/username;
- callback is replayed.

The agent MUST NOT auto-merge identities merely because emails match unless canonical policy explicitly permits it.

## 53. IAREQ045 — OAuth unlink

Unlink MUST prevent the User from accidentally removing their only viable authentication method if product/security policy requires at least one remaining method.

## 54. IAREQ046 — OAuth token storage

Provider access/refresh tokens, if persisted, MUST use approved secret protection.

They MUST NOT be emitted in Domain events or ordinary logs.

## 55. IAREQ047 — OAuth failure normalization

Provider-specific errors should be mapped into stable Identity/Application failures sufficient for clients without leaking provider secrets.

# MFA

## 56. IAREQ048 — MFA capability model

MFA MUST be represented as Identity security state rather than a frontend-only challenge.

Supported methods MUST follow current source/product authority.

## 57. IAREQ049 — MFA enrollment

Enrollment MUST define:

- initiating authenticated User;
- method;
- verification before activation;
- secret/recovery material handling;
- duplicate enrollment;
- cancellation/expiry.

## 58. IAREQ050 — MFA challenge

Challenge MUST define:

- authentication stage;
- eligible methods;
- attempts/rate controls;
- expiry;
- replay prevention;
- successful transition to authenticated session.

## 59. IAREQ051 — MFA recovery

Recovery MUST be explicitly designed.

It MUST NOT reduce security to "disable MFA if email is known".

Recovery may require:

- recovery code;
- admin recovery;
- another accepted mechanism.

Exact policy follows canonical authority.

## 60. IAREQ052 — MFA disable/reset

Disable/reset is security-sensitive and MUST require appropriate proof/authorization.

Administrative reset, if supported, MUST be auditable.

## 61. IAREQ053 — MFA secrets

Raw MFA secret material MUST be protected.

Recovery codes should be handled according to security policy, preferably non-recoverably where semantics allow.

## 62. IAREQ054 — MFA session impact

Enabling/disabling/resetting MFA MUST define whether current sessions remain valid.

The policy must be explicit, not accidental.

# Security settings

## 63. IAREQ055 — Security settings ownership

Identity owns User-level security settings.

Workspace/Governance owns workspace policy.

Do not move workspace-wide security policy into User security settings.

## 64. IAREQ056 — Sensitive setting mutations

Changes such as:

- password;
- MFA;
- OAuth linkage;
- API token issuance;
- security email/identity fields

must have stronger verification where product/security policy requires it.

## 65. IAREQ057 — Security event production

Security-sensitive changes SHOULD produce stable audit/security facts according to the system audit model.

Examples:

- password changed;
- MFA enabled/disabled;
- provider linked/unlinked;
- token created/revoked;
- session revoked.

Exact event ownership/naming follows canonical event docs.

## 66. IAREQ058 — Security failure privacy

Security error details must not reveal:

- secret values;
- password hashes;
- token hashes;
- provider access tokens;
- MFA seed;
- recovery material.

# API tokens

## 67. IAREQ059 — API token is a principal credential

An API token MUST be modeled as a credential/principal mechanism, not as a User session cookie substitute.

## 68. IAREQ060 — Token secret issuance

If API tokens are supported:

- raw secret is generated securely;
- raw secret is shown only according to approved lifecycle, typically once;
- persisted form is non-reversible where possible;
- token ID/metadata remain separately addressable.

## 69. IAREQ061 — Token verification

Verification MUST be resistant to:

- raw secret DB disclosure;
- token enumeration;
- tenant/account confusion;
- timing/lookup issues according to implementation risk.

## 70. IAREQ062 — Token lifecycle

Define:

- create;
- list metadata;
- revoke;
- expire;
- rotate/recreate if supported.

Raw secret MUST NOT be returned by list/read metadata endpoints.

## 71. IAREQ063 — Token Account scope

API token Account/Tenant scope MUST be explicit.

The token MUST NOT grant access to every Account associated with a User unless product/security architecture explicitly says so.

## 72. IAREQ064 — Token authorization

API tokens still pass through Governance/Application authorization.

Authentication by token is not universal authorization.

## 73. IAREQ065 — Token scopes

If token scopes exist, define whether they are:

- credential-level capability restrictions;
- Governance permissions;
- both through an explicit intersection.

Do not create a second unrelated authorization model accidentally.

## 74. IAREQ066 — Token revocation

Revocation MUST take effect within the accepted security window.

Caches MUST NOT indefinitely preserve revoked token validity.

## 75. IAREQ067 — Token auditability

Token use and lifecycle should be traceable without logging raw token values.

# Cross-team contracts

## 76. IAREQ068 — Workspace producer contract

Identity & Accounts MUST provide Workspace/Governance with:

- stable Actor/User ID;
- stable Account ID;
- Account scope semantics;
- identity availability/lifecycle signals if needed.

Workspace/Governance MUST NOT require password/session internals.

## 77. IAREQ069 — Billing producer contract

Billing MUST receive:

- billable Account identity;
- Account lifecycle state/facts required by Billing;
- actor identity for billing administration.

Billing MUST NOT require Identity credential internals.

## 78. IAREQ070 — WorkManagement producer contract

WorkManagement must be able to authorize/attribute operations through stable:

- Actor;
- Account;
- Workspace/Governance handoff.

WorkManagement MUST NOT depend on Identity tables.

## 79. IAREQ071 — Documents/Collaboration producer contract

Documents/Collaboration must be able to retain stable historical author references without owning Identity lifecycle.

## 80. IAREQ072 — Automation/system actor contract

Automation/background execution may require an Actor/principal model.

Identity must expose only the semantics required by approved architecture.

It MUST NOT create fake User records for every technical process unless explicitly designed.

## 81. IAREQ073 — Analytics identity contract

Analytics may consume derived identity/account facts.

Sensitive identity/profile fields must not be exposed simply because Analytics wants convenient reporting.

# Data ownership

## 82. IAREQ074 — Identity persistence is private

Identity owns persistence for its canonical state.

Other contexts MUST NOT mutate Identity-owned tables directly.

## 83. IAREQ075 — Accounts persistence is private

Where Account state physically resides, its persistence MUST have one accountable owner.

If Account semantics currently live in another module, PLAN must reconcile ownership before new writes are introduced.

## 84. IAREQ076 — No dual Account source of truth

The implementation MUST NOT create:

```text
Identity.Account
```

and:

```text
Workspace.Account
```

or similar competing canonical Account records.

One canonical business Account identity is required.

## 85. IAREQ077 — External identity mapping is Identity-owned

OAuth/provider identity mapping belongs to Identity.

Integrations provider connections are a separate context.

## 86. IAREQ078 — Session data ownership

Session persistence belongs to Identity even if cache/storage mechanisms are implemented in Infrastructure/Platform.

Mechanism ownership does not transfer business state ownership.

# API contract

## 87. Phase 13 closure authority

Phase 13 is the final harmonization pass for API, authorization and Identity/Accounts event contracts.

The source-audit baseline for this closure revision is:

```text
branch: develop
SHA: 4efd37bdff79f93f97059586928aa94af67ba8b1
```

At that baseline the following Phase 13 work is accepted and MUST remain green:

```text
IA-API-002   error taxonomy
IA-AUTHZ-001 resource/action inventory and account-scope enforcement
IA-AUTHZ-002 self-service hardening
IA-API-004   canonical OpenAPI regeneration/drift evidence
```

The following are NOT closure-complete at that baseline and are governed by the requirements below:

```text
IA-API-003   browser CSRF contract
IA-AUTHZ-003 Account administration/Governance reconciliation
IA-AUTHZ-004 handler authorization-bypass gate
IA-EVT-001   event inventory
IA-EVT-002   payload minimization/classification
IA-EVT-003   event versioning and compatibility
```

A prior implementation or review note that marks one of these items `deferred` does not satisfy this SPEC.

## 88. IAREQ079 — API surface categories

Identity & Accounts APIs may include:

```text
authentication/session
current identity/profile
Account lifecycle/context
OAuth
MFA
security settings
API token management
CSRF bootstrap for browser clients
```

Exact endpoint layout follows current API architecture.

Endpoint code MUST remain transport/adaptation code. Business authorization and business invariants belong to the approved Application/Domain paths.

## 89. IAREQ080 — Authentication vs authorization errors

API behavior MUST distinguish authentication failure from authorization denial according to canonical API policy.

At minimum the public taxonomy must preserve the semantic distinction between:

```text
unauthenticated / invalid authentication
expired or revoked session
forbidden
not-found/privacy masking
validation
conflict
security-sensitive generic failure
```

Security-sensitive failures MUST NOT expose account existence, credential correctness, internal policy details or provider secret material.

All new Phase 13 security middleware failures MUST use the same canonical ProblemDetails/error-writing contract rather than manually creating a parallel JSON error shape.

## 90. IAREQ081 — Sensitive response minimization

Identity APIs MUST return only required fields.

Do not expose:

- password hashes;
- OAuth secrets;
- MFA secrets;
- token hashes;
- raw session/refresh material not explicitly part of the accepted transport;
- internal security flags not intended for clients.

## 91. IAREQ082 — Stable IDs in contracts

External/internal API DTOs should use stable typed identifiers as architecture dictates.

Do not expose EF-specific or provider-specific identities as canonical system IDs.

## 92. IAREQ083 — OpenAPI compatibility

Any public contract change MUST update OpenAPI/generation evidence as required by backend CI.

The OpenAPI artifact at the candidate SHA must be generated from the same source being certified.

Handwritten frontend contract divergence is not acceptable when a canonical generated/typed contract exists.

# Browser CSRF contract

## 93. IAREQ126 — One browser CSRF protocol

Browser cookie-authenticated state-changing requests MUST use one Platform-owned CSRF protocol.

The canonical Phase 13 closure protocol is:

```text
cookie: csrf_token
header: X-CSRF-Token
```

The frontend MUST NOT use a second convention such as:

```text
XSRF-TOKEN
X-XSRF-TOKEN
meta-tag-only token discovery
```

unless a later accepted ADR supersedes this requirement.

Identity owns which browser operations are security-sensitive. Platform/API owns the reusable CSRF mechanism and enforcement.

## 94. IAREQ127 — Explicit CSRF bootstrap

The browser client MUST be able to obtain a CSRF token without reading a cookie owned by another host.

The target contract is an explicit safe bootstrap operation under the existing Identity/Auth API group.

Conceptually:

```text
GET <existing Identity/Auth group>/csrf
→ generate cryptographically random token
→ set csrf_token cookie
→ return the same token in the response body
```

The response-body token is the client transport value. The cookie is the server comparison value.

The browser client MUST keep the response token in instance-scoped memory.

It MUST NOT persist the CSRF token in:

```text
localStorage
sessionStorage
IndexedDB as a durable auth secret store
```

The mechanism MUST NOT rely on JavaScript reading the API host's cookie.

## 95. IAREQ128 — CSRF cookie and cross-origin deployment semantics

The CSRF cookie MUST remain host-scoped unless an accepted architecture decision explicitly requires a broader Domain attribute.

For the current cross-origin-capable browser/API model:

```text
production:
  Secure = true
  SameSite = None
  Path = /

development:
  Secure follows local HTTPS capability
  SameSite = Lax
  Path = /
```

The exact expiry may follow the accepted security configuration, but token expiry/renewal behavior MUST be deterministic and testable.

The CSRF cookie does not need to be JavaScript-readable because the response body carries the client token.

## 96. IAREQ129 — CSRF applicability

CSRF enforcement applies to unsafe browser operations that rely on or establish ambient cookie-based authentication/session state.

Unsafe methods are:

```text
POST
PUT
PATCH
DELETE
```

The browser transport MUST obtain a token before an unsafe request that requires the browser-cookie CSRF contract and MUST attach `X-CSRF-Token`.

The same transport rule MUST cover refresh. `refresh` MUST NOT bypass CSRF by using a raw fetch path that omits the token.

Non-browser principals that authenticate exclusively with an explicit non-ambient credential such as an API token MUST NOT be forced into the browser CSRF protocol.

Applicability MUST be expressed through the canonical authentication/endpoint contract or reusable request classifier. It MUST NOT be maintained as a growing path-string allow/deny list or scattered endpoint-local `[SkipCsrf]` convention.

### Phase 13 endpoint-applicability evidence

The candidate source state MUST have a complete, reviewable classification of every release-scoped unsafe browser-reachable operation owned by Identity/Accounts and every Account/Workspace mutation whose authorization/session semantics are part of this workstream.

The inventory MUST be discovered from the candidate source/endpoint metadata rather than copied from a stale hand-maintained route list.

For each applicable endpoint/operation, record at minimum:

```text
endpoint type / operation
HTTP method
endpoint metadata/scope family
accepted authentication mode(s)
uses or establishes ambient cookie session? yes/no
CSRF_REQUIRED | CSRF_NOT_REQUIRED
reason
owning/representative test
```

The current API endpoint model already distinguishes semantic families such as public, authenticated, Account, Workspace, resource, admin and internal endpoints. Those semantics MAY be inputs to the classifier, but no family name alone is sufficient to infer CSRF behavior.

In particular:

- `public` MUST NOT automatically mean `CSRF_NOT_REQUIRED` when an unsafe operation establishes ambient browser session state;
- `authenticated` MUST NOT automatically mean cookie-only when the same operation can be invoked through an explicit non-ambient credential;
- login, refresh, logout/revoke and other session-establishing or session-mutating operations MUST be classified from their actual credential/session behavior;
- API-token issuance/revocation, profile/security/MFA/OAuth mutations and Account/Workspace mutations in release scope MUST be inventoried according to their actual browser/non-browser credential modes;
- safe methods do not become CSRF-required merely to make the matrix exhaustive.

A new unsafe release-scoped endpoint or a change in accepted authentication mode MUST cause the applicability evidence/gate to be reviewed.

This evidence is a closure/audit contract. Production middleware MUST NOT turn the evidence table into a path-string allowlist.

## 97. IAREQ130 — CSRF failure contract and rollout

When CSRF protection is enabled:

```text
missing cookie
missing header
mismatched token
expired/invalid token
```

must produce the canonical forbidden/security error contract.

CSRF MUST remain feature-gated until backend, frontend and cross-origin integration tests for this SPEC are green.

Enabling `Security:Csrf:Enabled` before the compatible frontend transport is deployed is forbidden.

# Events

## 98. IAREQ084 — Identity event ownership

Identity owns identity/security facts.

Potential categories include:

- User lifecycle;
- session lifecycle where externally meaningful;
- OAuth linkage;
- MFA/security state;
- API token lifecycle.

Exact event names and payloads follow source/event authority.

Domain events and integration events MUST remain semantically distinct. A Domain event is not automatically a public cross-context contract.

## 99. IAREQ085 — Account event ownership

Accounts owns Account lifecycle facts.

Potential consumers:

- Workspaces;
- Billing;
- Analytics;
- Integrations/Automation where product-defined.

## 100. IAREQ086 — Event payload minimization

Identity/security events MUST NOT contain:

- raw credentials;
- password hashes;
- OAuth access/refresh tokens;
- MFA secrets or recovery material;
- raw API tokens;
- private keys/client secrets;
- raw authorization/session headers.

For integration/public events, personally identifiable information is permitted only when a real consumer contract requires it.

For every PII-bearing public event, execution evidence MUST record:

```text
PII field
semantic purpose
consumer(s)
why stable ID/reference alone is insufficient
retention/delivery implication
compatibility impact
```

If a consumer can operate from stable User/Account IDs and an approved read contract, unnecessary mutable PII SHOULD be removed from the integration payload.

## 101. IAREQ087 — Event identity and scope

Events intended for cross-context consumption must carry enough stable identity/scope to be consumed without private persistence access.

Where semantically applicable, the contract must distinguish:

```text
User/Actor identity
Account scope
Workspace/resource scope
occurred-at/semantic time
```

Scope MUST be part of the business fact when downstream correctness depends on it; consumers must not reconstruct tenant scope from unrelated private tables.

## 102. IAREQ088 — Event compatibility

A public integration event is identified by the compound contract identity:

```text
(logical event name, version)
```

Logical name alone is NOT a sufficient deserialization/registry key once versioning exists.

A breaking public event change MUST NOT mutate an existing version in place.

The system MUST support coexistence such as:

```text
identity.some-fact v1
identity.some-fact v2
```

for a controlled migration window.

During the foundation freeze, the compatibility policy is intentionally conservative:

> Any public integration-event payload schema change requires an explicit version bump unless an accepted compatibility authority proves the serialized contract is unchanged.

This rule prevents individual coding agents from deciding whether an additive-looking payload change is safe for unknown consumers.

## 103. IAREQ131 — Compound event registry identity

All runtime/source registries involved in public integration-event resolution MUST use `(Name, Version)` consistently.

This includes the current equivalents of:

```text
IntegrationEventCatalog
ContractRegistry
EventTypeRegistry/deserialization lookup
ConsumerRegistry/consumer contract lookup
architecture/contract tests
```

Name-only APIs such as conceptually:

```text
Resolve(messageName)
TryResolve(messageName)
```

must be replaced by or migrated to version-aware APIs.

After internal callers are migrated, a name-only fallback MUST NOT remain as a silent production compatibility path.

## 104. IAREQ132 — Public event schema baseline

The repository MUST contain one machine-verifiable canonical integration-event contract baseline.

The Phase 13 target path is:

```text
backend/contracts/events/notrelix.events.json
```

This file is the GLOBAL backend baseline for public integration-event contracts discoverable through the canonical production messaging contract/registry. It is NOT an Identity-only event snapshot.

Identity & Accounts Phase 13 owns the business-contract decisions for events produced by Identity/Accounts. Its implementation MAY change shared manifest schema/generation mechanics when required by the accepted Platform event-versioning contract, but MUST NOT silently change the business payload, logical version, ownership or consumer maturity of an unrelated bounded context merely to make this workstream pass.

If generation reveals unrelated-context semantic drift:

```text
fail the drift check
→ identify the owning bounded context
→ route the change to that owner / accepted cross-context change
```

Do not normalize the failure by editing another team's event contract from this workstream.

Domain-only internal events that are not public integration contracts are not rows in this global integration-event manifest merely because they have internal event metadata.

The artifact must be generated or validated deterministically from the source contract registry and contain, at minimum:

```text
logical name
version
CLR/source type identity
payload property names
serialized property types/nullability
scope metadata where available
classification/PII marker where applicable
consumer maturity summary
```

A CI/architecture gate MUST fail when source public-event shape changes without the corresponding accepted version/contract update.

The contract baseline is not a replacement for Domain tests; it is a public compatibility guard.

## 105. IAREQ133 — Event consumer inventory and maturity

Identity/Accounts event inventory MUST be source-completable.

For every public integration event record:

```text
producer
semantic fact
payload
Account/Workspace scope
logical name
version
classification
PII/security content
registered consumer
actual IConsumer<T> implementation where applicable
consumer bounded context
consumer maturity
compatibility status
```

Consumer maturity MUST distinguish:

```text
IMPLEMENTED
STUB
NONE
```

A registry entry pointing to a stub is NOT evidence of an implemented downstream capability.

Runtime outbox/DLQ state is separate operational evidence and MUST NOT be used as a reason to defer the source inventory.

## 106. IAREQ134 — Event version migration protocol

For a breaking public event migration from v1 to v2, the allowed deployment sequence is:

```text
1. add v2 contract without deleting v1;
2. deploy consumer dual-read v1 + v2 where the consumer is affected;
3. verify v2 handling;
4. switch producer to v2;
5. observe/drain relevant v1 outbox/retry/DLQ backlog;
6. verify no required v1 delivery remains;
7. retire v1 consumer path;
8. remove v1 contract only after the accepted retention/rollback window.
```

Producer-first breaking deployment is forbidden.

Poison-message handling is not a substitute for contract compatibility.

## 107. IAREQ135 — Operational event evidence is separate

Operational verification SHOULD be able to report, by event/version/consumer where the platform supports it:

```text
outbox pending count
oldest pending age
retry backlog
DLQ/poison count
unsupported-version count
```

If no deployment environment exists for the candidate, this evidence may be recorded as:

```text
NOT_APPLICABLE_UNTIL_DEPLOYMENT
```

That status does not permit IA-EVT-001, IA-EVT-002 or IA-EVT-003 source work to remain open.

# Authorization

## 108. IAREQ089 — Identity resource actions

Identity must define resource/action semantics for its protected administrative/security operations where required.

Examples conceptually:

```text
view profile
edit profile
manage sessions
manage MFA
manage API tokens
```

Exact action names follow Governance/resource authority.

Handlers MUST NOT create a second role engine.

## 109. IAREQ090 — Account resource actions

Accounts must define Account-level business actions required by Governance.

At the Phase 13 closure baseline the currently relevant account-scope permission actions include:

```text
ViewWorkspace
CreateWorkspace
```

The closure baseline for current Account roles is:

| AccountRole | ViewWorkspace | CreateWorkspace |
|---|---:|---:|
| Owner | allow | allow |
| Admin | allow | allow |
| Member | allow | deny |
| BillingAdmin | allow | deny |
| SecurityAdmin | allow | deny |

This table is a baseline fallback for currently existing actions only.

It MUST NOT be interpreted as permission to invent speculative actions such as `ManageBilling` or `ManageSecurity` merely because `BillingAdmin` or `SecurityAdmin` roles exist.

When a real Account-level action is introduced, Governance/product authority must define it explicitly.

## 110. IAREQ091 — Bootstrap administration

If Account creation requires establishing initial administrative authority, bootstrap semantics must be explicit and bounded.

It MUST NOT become a permanent authorization bypass.

After bootstrap, ordinary Account/Workspace administration MUST use the canonical authorization path.

## 111. IAREQ092 — Self-service vs Governance

Identity self-service operations may use actor-is-resource semantics where approved.

Workspace/Account administration should still use Governance where policy applies.

Self-service exceptions must be explicit request contracts, not endpoint/handler role shortcuts.

## 112. IAREQ136 — One authoritative Application authorization path

For a protected Application use case, authorization must be owned by the canonical pipeline/request contract unless a specifically approved architecture contract says otherwise.

For normal permission-protected commands/queries:

```text
request declares authorization requirement
→ AuthorizationBehavior evaluates
→ handler executes only after authorization succeeds
```

A handler MUST NOT re-authorize the current actor by directly using:

```text
IPermissionService
IPermissionEvaluator
IWorkspacePermissionService
IAuthorizationDecisionStore
```

or equivalent production authorization services when the request is already pipeline-owned.

A handler MUST NOT query current membership/role solely to reproduce an authorization decision already owned by the pipeline.

## 113. IAREQ137 — Role-check classification

Role/state comparisons inside Application handlers must be classified by semantics:

```text
FORBIDDEN AUTHORIZATION BYPASS
  current actor's role/membership decides whether the use case is allowed

ALLOWED DOMAIN/BUSINESS INVARIANT
  target entity/member role or state is inspected after authorization
  e.g. last-owner protection, ownership transfer invariant
```

Any allowed exception to the source gate must identify:

```text
exact type/file
exact member or pattern
reason
owned invariant
review trigger
```

Wildcard allowlists are forbidden.

## 114. IAREQ138 — Explicit Governance rule precedence

For account-scope authorization, the target decision order is:

```text
1. active Account membership required;
2. Account Owner retains canonical owner authority;
3. applicable explicit Governance rules are evaluated;
4. current AccountRole baseline fallback is applied;
5. otherwise deny.
```

An explicit Governance deny must not be converted into allow by the non-owner baseline fallback.

The handler MUST NOT duplicate this order.

# Security

## 115. IAREQ093 — Security-sensitive capability class

The following are high sensitivity:

```text
login/session establishment
session creation/revocation/refresh
password/credential change
OAuth link/unlink
MFA enrollment/reset
API token issuance/revocation
Account switch/context
Account lifecycle
browser CSRF bootstrap/enforcement
Account/Governance administration
public security-event production
```

They require negative tests and security review.

## 116. IAREQ094 — Secret logging prohibition

Logs/traces/events/DLQ diagnostics MUST NOT include:

- passwords;
- raw session secrets;
- JWT/refresh secrets if present;
- OAuth tokens;
- MFA seeds/recovery codes;
- raw API tokens;
- Authorization headers;
- private keys/client secrets;
- CSRF tokens except controlled test-only sentinel handling where the token is not emitted to ordinary logs.

## 117. IAREQ095 — Enumeration resistance

Authentication/recovery/token management APIs should avoid unnecessary information leakage allowing attackers to enumerate identities or security state.

## 118. IAREQ096 — Replay resistance

Replay-sensitive flows include:

- OAuth callback;
- MFA challenge/recovery;
- token creation result;
- password reset/recovery if supported;
- CSRF token/cookie pairing when a stale/rotated token is presented.

Replay behavior must be defined.

## 119. IAREQ097 — Rate/abuse controls

Identity owns classification of sensitive operations requiring abuse controls.

Platform owns the generic rate-limit mechanism.

Identity MUST NOT create a second rate-limit framework.

## 120. IAREQ098 — Security state changes and sessions

High-risk security changes must define whether they invalidate:

- current session;
- all sessions;
- API tokens;
- OAuth grants.

No accidental policy.

# Concurrency and consistency

## 121. IAREQ099 — Identity uniqueness concurrency

Concurrent registration/linking must not create duplicate canonical identities for the same unique key.

## 122. IAREQ100 — OAuth linking concurrency

Concurrent callbacks/link attempts must preserve provider-subject uniqueness.

## 123. IAREQ101 — Account creation concurrency

If Account creation involves slug/name uniqueness or bootstrap relationships, those invariants must be concurrency-safe.

## 124. IAREQ102 — Session revocation race

Session revocation must define behavior against concurrent requests/refreshes.

Refresh performed through browser cookie auth must also preserve the CSRF requirement.

## 125. IAREQ103 — API token creation/revocation race

Revoked tokens must not be reactivated by stale writes.

# Migration and compatibility

## 126. IAREQ104 — Current Account-location reconciliation

PR-IA-00 inventory found the canonical Accounts module in Domain (`backend/src/Notrelix.Domain/Accounts/`) and Accounts abstractions/services in Application; the original "not visibly exposed" premise is `DOC_STALE`.

The PLAN must preserve the accepted RETAIN decision unless later architecture evidence explicitly changes ownership.

No physical project split is authorized by this SPEC.

## 127. IAREQ105 — Identity module preservation

Existing Identity subareas such as:

```text
Users
Profiles
Sessions
OAuth
Mfa
Security
Tokens
```

are current source evidence.

Do not refactor folder structure solely for aesthetic symmetry.

## 128. IAREQ106 — Identity ID migration

Changing User/Session/Token IDs is high-risk because downstream references may exist.

Any ID change requires:

- consumer inventory;
- DB migration;
- event/API compatibility;
- rollback/forward-fix.

## 129. IAREQ107 — Account ID migration

Changing Account/Tenant identity requires system-wide migration planning.

It MUST NOT be included casually in a closure PR.

## 130. IAREQ108 — OAuth mapping migration

Provider mapping changes must preserve:

- provider identity uniqueness;
- existing links;
- login continuity;
- rollback/recovery.

## 131. IAREQ109 — MFA migration

Changing MFA storage/method representation must protect existing enrolled Users and secret material.

## 132. IAREQ110 — API token migration

Changing token hashing/format must define whether existing tokens remain valid, rotate, or are revoked.

## 133. IAREQ139 — Event contract migration is a first-class compatibility change

A change to event contract identity, registry keying or public schema baseline is a compatibility change even when no database migration is required.

Phase 14 compatibility review MUST therefore include:

```text
registry caller migration
producer/consumer deployment order
v1/v2 coexistence where needed
outbox/DLQ compatibility
rollback/forward-fix
```

# Observability

## 134. IAREQ111 — Authentication observability

Observe safely:

- login success/failure category;
- session creation;
- session expiry/revocation;
- provider failure class;
- MFA failure class;
- token verification failure class;
- latency.

Do not log secrets.

## 135. IAREQ112 — Account-context observability

Where safe, traces/logs should include stable Account ID for scoped operations.

This assists tenant-isolation diagnosis.

## 136. IAREQ113 — Security event correlation

Security-sensitive operations should be correlatable across:

```text
request
→ Application operation
→ persistence
→ event/audit
```

without exposing secret material.

## 137. IAREQ114 — Closure observability

The existing observability platform should expose safe failure categories for:

```text
CSRF validation failure
authorization denial/misconfiguration
unknown integration-event contract
unsupported integration-event version
consumer retry/poison/DLQ where available
```

This SPEC does not require a new observability vendor.

# Performance

## 138. IAREQ115 — Hot-path requirements

The following are hot paths:

- actor/session resolution;
- token verification;
- Account context resolution;
- basic profile/current identity query;
- authorization evaluation.

They must avoid unnecessary cross-context/database roundtrips.

## 139. IAREQ116 — Authorization dependency efficiency

Identity/Account resolution must not force every protected request to perform redundant expensive lookups when safe caching/projection exists.

Correctness and revocation semantics take priority over caching.

Pipeline ownership MUST NOT cause a handler to repeat the same authorization lookup.

## 140. IAREQ117 — Session cache semantics

If session validity is cached, cache behavior must preserve accepted revocation/expiry guarantees.

## 141. IAREQ118 — Account context cache semantics

Account context caching MUST be Account-scoped and invalidated on relevant lifecycle/security changes.

# Reliability and failure modes

## 142. IAREQ119 — External OAuth provider failure

Provider outage must not corrupt local identity state.

A failed callback/token exchange must leave the User/provider link in a defined state.

## 143. IAREQ120 — Persistence failure

Security-sensitive state must not be reported as successful before authoritative persistence is complete.

Examples:

- token revoke;
- MFA enable;
- OAuth link;
- session revoke.

## 144. IAREQ121 — Partial Account creation

If Account creation spans multiple context/bootstrap steps, partial failure behavior must be defined.

Do not hide an accidental cross-context distributed transaction inside a handler.

## 145. IAREQ122 — Recovery

Operational repair should be possible for:

- stuck OAuth link state;
- invalid session state;
- duplicate external identity mapping;
- broken Account bootstrap mapping;
- event-contract rollout/backlog failures where operational tooling exists.

Repair tools require authorization/audit.

# Data privacy

## 146. IAREQ123 — Data minimization

Identity stores and publishes only information required by product/security semantics.

Do not accumulate provider/profile/event payload data without a defined purpose.

## 147. IAREQ124 — Sensitive profile fields

Sensitive personal fields require explicit authorization and retention semantics.

## 148. IAREQ125 — Account data separation

Account business metadata and User personal identity data should not be merged merely because the same actor created the Account.

Event contracts must preserve the same separation: Account business facts should not automatically carry mutable User PII.

# Phase 13 closure requirement

## 149. IAREQ140 — Phase 13 is not closable by deferral

Phase 13 reaches `DONE` only when all of the following are complete on the candidate source state:

```text
IA-API-002 DONE
IA-API-003 DONE
IA-API-004 DONE
IA-AUTHZ-001 DONE
IA-AUTHZ-002 DONE
IA-AUTHZ-003 DONE
IA-AUTHZ-004 DONE
IA-EVT-001 DONE
IA-EVT-002 DONE
IA-EVT-003 DONE
```

Only environment-dependent operational event evidence may be classified:

```text
VERIFIED
or
NOT_APPLICABLE_UNTIL_DEPLOYMENT
```

A source-level Phase 13 work unit cannot be closed as `deferred` merely because runtime evidence is unavailable.

# Downstream readiness contract

## 150. P1 critical producer contract

The existing P1 producer contract remains authoritative:

```text
Actor identity D5
User identity D5
Account identity D5
Account/Tenant boundary D5
Current Account resolution D5
Account isolation D5
Session identity D4+
Identity ↔ Account consumer contract D5
```

Phase 13 closure MUST NOT destabilize these already established contracts.

## 151. Phase 13 downstream contract

Before final team certification, Workspace/Governance/Billing/Platform/frontend consumers must be able to rely on:

```text
canonical auth/error taxonomy
one browser CSRF protocol
pipeline-owned authorization
Account role/action baseline for current actions
version-aware integration-event identity
machine-verifiable event schema baseline
explicit event payload classification
```

No downstream consumer may require private Identity/Account persistence to compensate for missing event scope or authorization semantics.

# Functional acceptance criteria

## 152. IAAC001 — User identity acceptance

A canonical User can be referenced stably across sessions and authentication mechanisms.

## 153. IAAC002 — Actor acceptance

Protected Application operations can consume a trusted Actor without raw HTTP dependency.

## 154. IAAC003 — Session acceptance

Valid, expired and revoked session states produce deterministic behavior.

## 155. IAAC004 — Account acceptance

A canonical Account exists with stable identity and lifecycle semantics.

## 156. IAAC005 — Tenant acceptance

Account A cannot access Account B state through Identity/Account paths.

## 157. IAAC006 — Consumer acceptance

Workspace/Governance can consume User/Actor/Account contracts without private persistence access.

## 158. IAAC007 — OAuth acceptance

OAuth flow protects state/replay and maps one provider identity to the correct canonical User.

## 159. IAAC008 — MFA acceptance

Enrollment/challenge/recovery/reset preserve security invariants.

## 160. IAAC009 — API token acceptance

Token secrets are safely issued/verified/revoked and still pass through authorization.

## 161. IAAC010 — Security acceptance

No secret material appears in ordinary API responses/logs/events.

## 162. IAAC018 — CSRF acceptance

A supported browser can:

```text
bootstrap CSRF
perform unsafe cookie-auth mutation
refresh session
repeat unsafe mutation after refresh
```

using the single canonical CSRF transport across the supported frontend/API deployment topology.

Missing/mismatched CSRF is rejected with canonical API error semantics.

## 163. IAAC019 — Authorization closure acceptance

Account administration and Workspace creation use the centralized Governance/permission path.

No protected handler contains an unclassified authorization bypass.

## 164. IAAC020 — Event closure acceptance

Identity/Account integration events have:

```text
source-complete inventory
no prohibited secret payloads
explicit PII classification when applicable
(Name, Version) contract identity
schema compatibility evidence
consumer maturity classification
```

# Non-functional acceptance criteria

## 165. IAAC011 — Architecture

- Domain remains free of Infrastructure/API dependencies.
- No new production project/service is introduced.
- Platform mechanism ownership remains explicit.
- authorization remains pipeline-owned.
- public event versioning is consistent across registries.

## 166. IAAC012 — Ownership

Identity/Accounts business state has one canonical owner each.

No dual Account source of truth exists.

## 167. IAAC013 — Security

All sensitive operations have negative/security verification in the TESTS artifact, including CSRF and event payload safety.

## 168. IAAC014 — Migration

Every source/schema/public-contract change has a compatibility/migration strategy.

## 169. IAAC015 — Observability

Critical auth/session/account/CSRF/event-contract failures can be diagnosed without secret logging.

## 170. IAAC016 — Performance

Hot identity/context/authorization paths remain within backend performance budgets defined by system quality authority.

## 171. IAAC017 — CI

All required backend architecture/core/API/integration gates pass on the candidate SHA before certification.

Frontend contract tests required by the CSRF closure must also pass before CSRF is enabled in the deployable configuration.

## 172. IAAC021 — Phase 13 closure

No source-level Phase 13 work unit remains `OPEN`, `PARTIAL` or `DEFERRED` at final Identity & Accounts certification.

# Requirement traceability contract

## 173. TESTS artifact obligation

`identity-accounts.tests.md` MUST map each material requirement group to verification.

Minimum mapping families:

```text
IAREQ001–IAREQ011
→ User/Actor/Profile tests

IAREQ012–IAREQ023
→ Authentication/Session tests

IAREQ024–IAREQ039
→ Account/tenant/consumer tests

IAREQ040–IAREQ067
→ OAuth/MFA/Security/API-token tests

IAREQ068–IAREQ078
→ cross-team/data ownership tests

IAREQ079–IAREQ083
→ API/error/OpenAPI tests

IAREQ126–IAREQ130
→ CSRF backend/frontend/integration tests

IAREQ084–IAREQ088 + IAREQ131–IAREQ135
→ event inventory/payload/version/compatibility tests

IAREQ089–IAREQ092 + IAREQ136–IAREQ138
→ authorization/pipeline/bypass tests

IAREQ093–IAREQ103
→ security/concurrency tests

IAREQ104–IAREQ110 + IAREQ139
→ migration/compatibility tests

IAREQ111–IAREQ125
→ observability/performance/reliability/privacy tests

IAREQ140
→ Phase 13 closure/certification gate
```

## 174. Source-audit evidence rule

When a closure requirement is based on a current-source deficiency, PLAN and TESTS must name the source surface that established the deficiency.

Current Phase 13 source facts include:

```text
backend/src/Notrelix.Infrastructure/Auth/Csrf/CsrfProtector.cs
backend/src/Notrelix.API/Middleware/CsrfValidationMiddleware.cs
frontend/packages/foundation/contracts/src/client/csrf.ts
frontend/packages/foundation/contracts/src/client/api-client.ts
backend/src/Notrelix.Domain/Accounts/Members/AccountRole.cs
backend/src/Notrelix.Domain/Governance/Permissions/PermissionAction.cs
backend/src/Notrelix.Application/Common/Security/PermissionService.cs
backend/src/Notrelix.Infrastructure/Messaging/IntegrationEventCatalog.cs
```

Physical paths may move only through an accepted refactor; semantic requirements remain.

# Stop conditions

## 175. IASTOP001 — conflicting Account ownership

If current source reveals two competing canonical business Account models:

```text
STOP structural work
→ resolve canonical owner
```

## 176. IASTOP002 — security downgrade

Any closure implementation requiring:

- plaintext secrets;
- broad auth bypass;
- global tenant access;
- disabling CSRF/auth controls as the final state;
- leaking credential material;
- name-only event deserialization after versioned contracts are introduced

must stop.

## 177. IASTOP003 — CSRF architecture conflict

If the supported deployment topology cannot satisfy the explicit bootstrap + cookie/header protocol without broadening cookie trust unexpectedly:

```text
STOP enablement
→ update/supersede the CSRF ADR through architecture review
```

Do not silently change cookie Domain/SameSite policy to make a test pass.

## 178. IASTOP004 — authorization policy conflict

If product/Governance authority contradicts the Phase 13 Account role/action baseline:

```text
STOP the affected action
→ record the canonical policy decision
→ update SPEC/PLAN/TESTS together
```

The coding agent must not choose a new role matrix.

## 179. IASTOP005 — event contract identity conflict

If a Platform messaging contract requires logical-name-only identity and cannot coexist with `(Name, Version)`:

```text
STOP event compatibility closure
→ architecture decision required
```

Do not fake version support by storing a version field that runtime resolution ignores.

## 180. IASTOP006 — unknown public PII requirement

If removing PII would break a real consumer but there is no documented consumer purpose/retention expectation:

```text
STOP payload mutation
→ classify and approve the public contract first
```

## 181. IASTOP007 — architecture gate conflict

Do not weaken a valid architecture/security gate merely because closure implementation fails.

Either implementation is wrong or architecture authority must change explicitly.

# Definition of Done — Identity & Accounts final scope

## 182. Functional DoD

- canonical User/Actor/Session/Account contracts remain stable;
- browser CSRF bootstrap and unsafe request flow work end-to-end;
- current Account administration follows Governance/pipeline authorization;
- handler authorization bypasses are eliminated or exact-classified as business invariants;
- public Identity/Account events are inventoried and version-aware;
- public event payloads contain no prohibited secrets;
- PII is intentional and justified;
- v1/v2 compatibility protocol is executable.

## 183. Architecture DoD

- no new production service/project;
- Domain purity preserved;
- one Account owner;
- Platform mechanism ownership preserved;
- one authoritative Application authorization path;
- event contract identity is `(Name, Version)` through production resolution;
- no silent name-only event fallback;
- generated public event baseline is canonical and drift-checked.

## 184. Security DoD

- CSRF enabled only after compatible backend/frontend deployment evidence;
- secret response/log/event guards green;
- enumeration/replay/tenant-spoofing/revocation tests green;
- no raw authorization bypass;
- API-token principal remains outside browser CSRF when using explicit non-ambient auth;
- event/DLQ diagnostics do not expose prohibited secrets.

## 185. Verification DoD

Before final certification, all material requirements must have:

```text
SPEC requirement
→ PLAN work unit
→ TEST ID
→ test suite/project
→ CI gate
→ exact-SHA evidence
```

No `TODO`, `deferred`, `follow-up` or `future hardening` marker may hide an unresolved material Phase 13 closure item.

---
document_id: WRK-PLAN-IDENTITY-ACCOUNTS
document_type: workstream-plan
status: active
owner: identity-accounts-team
applies_to:
  - backend
  - identity
  - accounts
  - actor
  - registration
  - authentication
  - sessions
  - profiles
  - oauth
  - sso
  - mfa
  - security
  - api-tokens
  - account-context
  - tenant-isolation
evidence:
  - docs/workstreams/execution/identity-accounts/identity-accounts.spec.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/teams/identity-accounts.md
  - docs/workstreams/teams/platform-foundation.md
  - docs/workstreams/cross-team-dependencies.md
  - backend/docs/architecture/domain-modeling.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/api-and-contracts.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/testing-and-quality-gates.md
  - backend/docs/generated/project-map.md
review_on:
  - identity-spec-change
  - account-ownership-resolution
  - source-layout-change
  - session-contract-change
  - auth-model-change
  - account-context-change
  - oauth-sso-change
  - mfa-change
  - token-change
  - identity-migration-change
  - p1-exit-gate-change
---

# PLAN — Identity & Accounts

## 1. Purpose

This PLAN converts `identity-accounts.spec.md` into a deterministic backend execution sequence.

It answers:

```text
What must the coding agent inspect first?
What source ambiguity must be resolved before implementation?
Which capabilities form the P1 critical path?
Which capabilities may continue after Workspace/Governance is unblocked?
Which backend layers are touched by each work unit?
Which migrations/contracts/events must be considered?
What may run in parallel?
What must stop?
What evidence must be handed to TESTS and CERTIFICATION?
```

This PLAN does not replace the SPEC.

The SPEC defines target truth.

This PLAN defines implementation order and execution boundaries.

## 2. Master execution goal

Move Identity & Accounts from the current source state to:

```text
D5 stable producer contract for:
- Actor
- User
- Account
- Account/Tenant boundary
- Current Account
- Session identity
- downstream consumer contract
```

while continuing secondary Identity security capabilities without blocking Workspace/Governance unnecessarily.

The critical delivery model is:

```text
Source audit
↓
Account ownership resolution
↓
Platform prerequisite verification
↓
User / Actor
↓
Session / Authentication
↓
Account / Current Account / Tenant isolation
↓
Downstream producer contract
↓
P1 CORE D4/D5 GATE
──────────────────────────────
Workspace/Governance may start
──────────────────────────────
↓
Registration/Credentials hardening
↓
OAuth / SSO
↓
MFA
↓
Security settings
↓
API tokens
↓
Full Identity hardening
↓
Final team certification
```

## 3. Critical distinction: P1 core vs complete Identity team scope

The team MUST NOT wait for all secondary security features before unblocking P2.

### P1 core

Mandatory before Workspace/Governance broad implementation:

```text
Actor
User
Session identity
Account identity
Account lifecycle baseline
Account context
Tenant isolation
Identity ↔ Account consumer contract
```

### Secondary Identity scope

May continue in parallel after P1 core reaches D4/D5:

```text
Registration hardening
Credentials
OAuth
SSO
MFA
Security settings
advanced session controls
API tokens
```

This distinction is mandatory.

## 4. Current source evidence to verify, not redesign

The execution begins from current source, not from an idealized folder tree.

Current source evidence indicates:

### Domain

Visible Identity areas include:

```text
Identity/
├── Mfa
├── OAuth
├── Profiles
├── Security
├── Sessions
├── Tokens
└── Users
```

No separate top-level `Accounts` Domain folder is currently visible.

### Application

Visible Application feature areas include:

```text
Features/
├── Accounts/Abstractions
└── Identity/
    ├── Abstractions
    ├── ApiTokens
    ├── Auth
    ├── Credentials
    ├── OAuth
    ├── Profiles
    ├── Registration
    ├── SSO
    ├── Security
    ├── Sessions
    └── Users
```

This asymmetry is a source fact to investigate.

It is NOT permission to create missing folders mechanically.

## 5. Source-first execution rule

Before changing any capability, the coding agent MUST:

1. locate existing Domain state;
2. locate Application use cases;
3. locate Infrastructure persistence/configuration;
4. locate API endpoints;
5. locate tests;
6. locate integration-event mappings;
7. locate downstream consumers;
8. classify source as:
   - canonical;
   - legacy;
   - duplicate;
   - incomplete;
   - source debt;
9. only then modify source.

## 6. Prohibited execution pattern

The following is forbidden:

```text
read SPEC
→ create ideal folder layout
→ move code
→ make tests pass
```

Required pattern:

```text
read SPEC
→ audit current source
→ reconcile semantics
→ identify minimum target changes
→ preserve valid source
→ migrate only where necessary
```

# Master phases

## 7. Phase map

```text
Phase 0  Baseline and full source inventory
Phase 1  Account semantic/ownership resolution
Phase 2  Platform prerequisite verification
Phase 3  User / Actor / Profile core
Phase 4  Authentication / Session core
Phase 5  Account / Current Account / Tenant isolation
Phase 6  P1 downstream producer contract
Phase 7  P1 core verification and Workspace/Governance handoff

Phase 8  Registration / Credentials
Phase 9  OAuth / SSO
Phase 10 MFA
Phase 11 Security settings and session-security hardening
Phase 12 API Tokens

Phase 13 API / Authorization / Event harmonization
Phase 14 Persistence / Migration / Compatibility
Phase 15 Security hardening
Phase 16 Observability / Reliability / Performance
Phase 17 Cross-team integration hardening
Phase 18 TESTS handoff
Phase 19 Documentation / generated-contract handoff
Phase 20 Final certification handoff
```

Phases 8–12 may partially overlap after Phase 7.

Phases 13–17 run continuously where relevant but have explicit final hardening passes.

# Phase 0 — Baseline and source inventory

## 8. IA-INV-001 — Capture exact baseline

### Goal

Ensure all execution evidence refers to one exact source baseline.

### Required actions

Record:

```text
branch
HEAD SHA
backend solution/project inventory
migration head
test projects
CI workflow names relevant to backend
```

Use repository-native commands.

Conceptually:

```bash
git status --short
git branch --show-current
git rev-parse HEAD
```

### Required output

PR/execution notes:

```text
Identity & Accounts execution baseline
Branch:
SHA:
Backend solution:
Migration head:
Relevant CI:
```

### Stop condition

If working tree contains unrelated uncommitted changes that make source inventory ambiguous:

```text
STOP implementation
→ preserve/segregate unrelated work
```

## 9. IA-INV-002 — Domain inventory

### Requirement coverage

Supports:

```text
IAREQ001–IAREQ011
IAREQ016–IAREQ023
IAREQ024–IAREQ030
IAREQ040–IAREQ067
IAREQ074–IAREQ078
IAREQ099–IAREQ110
```

### Search

Run repository equivalent of:

```bash
find backend/src/Notrelix.Domain/Identity -maxdepth 4 -type f | sort

rg -n \
  "class User|record User|class Session|UserSession|OAuth|Mfa|MFA|ApiToken|Token|Profile|Security" \
  backend/src/Notrelix.Domain

rg -n \
  "AccountId|TenantId|Account\b|Tenant\b" \
  backend/src/Notrelix.Domain
```

### Inventory table

For every discovered Domain type record:

```text
Type:
Namespace:
Aggregate / Entity / Value Object / Enum:
Owner:
ID type:
Lifecycle:
Important invariants:
Events:
Cross-context references:
Persistence assumptions visible in Domain:
SPEC requirements covered:
```

### Required outcome

Identify actual canonical candidates for:

- User;
- Session;
- OAuth link;
- MFA method/state;
- API token;
- security settings;
- Profile;
- Account/Tenant if present anywhere in Domain.

## 10. IA-INV-003 — Application inventory

### Search

```bash
find backend/src/Notrelix.Application/Features/Identity -maxdepth 6 -type f | sort
find backend/src/Notrelix.Application/Features/Accounts -maxdepth 6 -type f | sort

rg -n \
  "IAccount|AccountId|TenantId|CurrentAccount|CurrentTenant|CurrentUser|CurrentActor" \
  backend/src/Notrelix.Application
```

### Required classification

For every module:

```text
Identity/Users
Identity/Auth
Identity/Credentials
Identity/Registration
Identity/Sessions
Identity/Profiles
Identity/OAuth
Identity/SSO
Identity/Security
Identity/ApiTokens
Accounts/Abstractions
```

record:

```text
Use cases:
Commands:
Queries:
Abstractions:
Authorization:
Transactions:
Idempotency:
External dependencies:
Domain dependencies:
Potential duplicate responsibility:
Current tests:
```

### Special requirement

`Accounts/Abstractions` MUST be inspected before any Account Domain design.

Determine whether it represents:

```text
read-only account context abstraction
cross-context lookup interface
legacy boundary
placeholder
full Account capability abstraction
```

Do not infer from folder name.

## 11. IA-INV-004 — Infrastructure inventory

### Search

Use source patterns, not guessed filenames:

```bash
rg -n \
  "UserConfiguration|SessionConfiguration|OAuth|Mfa|ApiToken|Account|Tenant|Password|Hash|Credential" \
  backend/src/Notrelix.Infrastructure

rg -n \
  "DbSet<.*User|DbSet<.*Session|DbSet<.*Token|DbSet<.*Account|DbSet<.*Tenant" \
  backend/src/Notrelix.Infrastructure
```

### Inventory

Record:

- EF configurations;
- DbSets;
- repositories/adapters;
- session/token storage;
- password hashing;
- OAuth provider clients;
- MFA secret protection;
- current-user/account implementations;
- Account/Tenant filters;
- migrations affecting these tables;
- indexes/unique constraints;
- RLS/tenant filtering behavior where applicable.

## 12. IA-INV-005 — API inventory

### Search

```bash
rg -n \
  "Identity|Auth|Session|Profile|OAuth|SSO|Mfa|MFA|ApiToken|Account|Tenant" \
  backend/src/Notrelix.API
```

### Record

For every endpoint:

```text
Route:
HTTP method:
Use case:
Authentication:
Authorization:
CSRF relevance:
Request:
Response:
Error mapping:
OpenAPI representation:
SPEC requirement:
```

### Special checks

Identify:

- login;
- registration;
- current identity;
- logout;
- session management;
- OAuth start/callback/link/unlink;
- SSO;
- MFA;
- profile;
- API tokens;
- Account endpoints if any.

## 13. IA-INV-006 — test inventory

### Search

```bash
rg -n \
  "Identity|Account|Tenant|Session|OAuth|Mfa|MFA|ApiToken|Authentication|CurrentUser" \
  backend/tests
```

### Classify tests into

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

### Output

Map existing coverage to SPEC requirement ranges.

Do NOT create TESTS.md yet from assumptions.

Inventory first.

## 14. IA-INV-007 — downstream dependency inventory

### Search

```bash
rg -n \
  "UserId|AccountId|TenantId|CurrentUser|CurrentAccount|Identity" \
  backend/src/Notrelix.Application/Features \
  backend/src/Notrelix.Domain
```

Exclude Identity itself when reviewing consumers.

### Required consumers

At minimum inspect:

```text
Workspaces
Governance
WorkManagement
Documents
Collaboration
Automation
Integrations
Billing
Analytics
```

### Record

```text
Consumer:
Identity dependency:
Account dependency:
Private source dependency?:
Stable ID dependency?:
Event dependency?:
Migration risk:
```

## 15. IA-INV-008 — duplicate mechanism scan

Search for competing implementations of:

```text
current user
actor
account/tenant resolver
session validation
password hashing
OAuth state
MFA challenge
API token verification
```

### Stop condition

If two production mechanisms claim the same authority:

```text
STOP affected capability
→ classify canonical vs legacy
→ do not merge them by convenience
```

## 16. Phase 0 exit

Phase 0 exits only when the team can answer:

```text
Where is canonical User?
Where is canonical Session?
Where is current Account/Tenant semantics?
What does Accounts/Abstractions do?
Where are OAuth/SSO/MFA/token responsibilities?
Which downstream contexts depend on Identity/Account?
Which pieces are complete vs placeholders?
```

# Phase 1 — Account semantic and ownership resolution

## 17. Why Phase 1 is mandatory

The original PLAN text assumed no top-level Account Domain module exists while Application exposes Accounts abstractions.

Phase 0/1 inventory (PR-IA-00) classified this as `DOC_STALE`:

```text
Domain DOES expose a canonical Accounts context:
backend/src/Notrelix.Domain/Accounts/
  Accounts/, Members/, Invitations/, Domains/, IdentityProviders/,
  Scim/, WorkspaceRoutes/, Regions/, Rules/

Application exposes Accounts abstractions and services:
backend/src/Notrelix.Application/Features/Accounts/
  Abstractions/, Services/, Provisioning/

Persistence: account.* schema (accounts, account_members, ...) with RLS policies (007)
```

The canonical Account model therefore exists in an acceptable owner.

Account implementation MUST NOT start until current semantics are identified (RETAIN evidence is recorded in PR-IA-00), but Phase 1 no longer assumes absence of a Domain module.

## 18. IA-ACC-DISC-001 — build Account representation inventory

Search all backend source for:

```bash
rg -n \
  "AccountId|TenantId|Account\b|Tenant\b|Organization\b" \
  backend/src backend/tests
```

Do not assume "Organization" exists; include only to catch possible semantic aliases.

### Build table

```text
Representation:
Layer:
Namespace:
Meaning:
Canonical or adapter:
Persistence:
Consumers:
Lifecycle:
Tenant boundary?:
Workspace relation:
Billing relation:
```

## 19. IA-ACC-DISC-002 — distinguish meanings of "account"

Classify every use into one of:

```text
A. Business Account / SaaS tenant
B. Login/user account terminology
C. External provider account
D. Billing provider customer/account
E. Infrastructure account/config
```

Only category A participates in the Accounts bounded context.

## 20. IA-ACC-DISC-003 — determine Account ↔ Workspace semantics

Inspect current source and product authority to answer:

```text
Can one Account own multiple Workspaces?
Is Workspace itself currently the tenant root?
Does Account exist only as a planned parent?
Where does membership live?
Where does billing attach?
```

### Stop condition

If source and canonical docs materially disagree:

```text
STOP structural Account implementation
→ architecture/product decision required
```

## 21. IA-ACC-DISC-004 — determine Account ownership action

After evidence, classify exactly one:

### RETAIN

Existing canonical Account model already exists in an acceptable owner.

Action:

```text
keep location
fill capability gaps
do not move for symmetry
```

### REHOME

Account semantics exist but are owned by the wrong bounded context.

Requires:

- explicit ownership decision;
- migration;
- consumer update;
- compatibility plan.

### SPLIT

A current type conflates Account with another concept.

Requires architecture/product approval.

### INTRODUCE

No canonical business Account exists.

Introduce logical Account capability inside existing production projects according to architecture.

This does NOT authorize a new project.

## 22. IA-ACC-DISC-005 — create Account decision note

Record in execution/PR evidence:

```text
Current representations:
Canonical business meaning:
Selected action: RETAIN | REHOME | SPLIT | INTRODUCE
Why:
Affected layers:
Affected tables:
Affected consumers:
Migration required:
Architecture decision required:
```

If architecture decision required is yes, stop structural implementation until approved.

## 23. Phase 1 exit

Must know:

- canonical Account identity target;
- physical source ownership;
- relation to User;
- relation to Workspace;
- relation to Billing;
- migration impact.

# Phase 2 — Platform prerequisite verification

## 24. Purpose

Identity core depends on existing foundation mechanisms.

Do not reimplement Platform inside Identity.

## 25. IA-PLT-001 — actor/current-user mechanism

Verify existing Application abstraction and runtime implementation for current actor/user.

### Required checks

- Application-owned abstraction;
- Infrastructure/API implementation;
- DI registration;
- trusted authentication source;
- tests;
- no Domain HttpContext dependency.

### Requirements

```text
IAREQ006–IAREQ009
IAREQ068
```

## 26. IA-PLT-002 — session/CSRF contract

Verify platform/API session transport and browser CSRF behavior.

Identity owns Session semantics.

Platform owns transport.

Do not redesign transport unless current contract blocks SPEC.

### Required result

Identity implementation can create/revoke/inspect sessions without duplicating cookie/CSRF mechanics.

## 27. IA-PLT-003 — Account/Tenant context mechanism

Verify existing context abstraction/mechanism.

Determine whether it currently represents:

- Account;
- Workspace;
- tenant;
- another scope.

Do not relabel it without semantic evidence.

## 28. IA-PLT-004 — authorization pipeline

Verify central Application authorization behavior is production-registered and usable by Identity/Accounts operations.

Identity must not implement handler-local role engines.

## 29. IA-PLT-005 — secret protection dependencies

Verify mechanisms for:

- password hashing;
- OAuth tokens if persisted;
- MFA secrets;
- API token hashes.

Identity owns lifecycle.

Infrastructure/Platform owns protection mechanisms.

## 30. Phase 2 blocker rule

If a prerequisite is missing:

```text
open targeted Platform blocker
→ implement only required mechanism
→ return to Identity critical path
```

Do NOT expand into full Platform roadmap.

# Phase 3 — User / Actor / Profile core

## 31. IA-USER-001 — validate canonical User aggregate/model

### Requirements

```text
IAREQ001–IAREQ005
IAREQ099
```

### Domain work

Inspect current `Identity/Users`.

Confirm:

- typed identity;
- lifecycle;
- unique identifiers;
- invariants;
- events;
- status semantics;
- soft-delete behavior if applicable.

### Allowed changes

Only changes required to satisfy SPEC.

### Forbidden

- replacing User solely to make Account cleaner;
- changing ID type without migration;
- embedding Workspace roles.

## 32. IA-USER-002 — uniqueness hardening

Verify concurrency-safe uniqueness for supported identifiers.

Potentially:

- normalized email;
- username;
- external provider subject through OAuth mapping.

### Layer split

Domain:

```text
semantic validation
```

Infrastructure/DB:

```text
concurrency-safe unique constraint
```

Application:

```text
friendly failure mapping
```

### Requirements

```text
IAREQ002
IAREQ099
```

## 33. IA-USER-003 — User lifecycle and historical references

Verify deactivate/delete semantics.

Inventory downstream references before any destructive behavior.

### Requirements

```text
IAREQ003–IAREQ005
IAREQ030
```

### Stop

If delete behavior would orphan downstream historical references and policy is undefined.

## 34. IA-ACTOR-001 — canonical Actor abstraction

Map User/session/API-token principal to the approved actor abstraction.

### Requirements

```text
IAREQ006–IAREQ009
```

### Layer contract

Domain:

```text
no HTTP dependency
```

Application:

```text
trusted actor abstraction
```

Infrastructure/API:

```text
runtime principal adaptation
```

## 35. IA-ACTOR-002 — principal-type classification

Inventory supported principals.

Do not add types absent from source/product authority.

For each principal:

```text
authentication mechanism
User association
Account scope
authorization behavior
audit identity
```

## 36. IA-PROFILE-001 — profile ownership cleanup

Inspect `Identity/Profiles`.

Classify fields:

```text
personal identity/profile
workspace-member metadata
security state
```

Move nothing until semantics are clear.

### Requirements

```text
IAREQ010–IAREQ011
IAREQ123–IAREQ125
```

## 37. IA-PROFILE-002 — profile mutation contract

Ensure self-service/admin mutation uses approved authorization.

Sensitive identity/security fields should not be generic profile updates.

## 38. Phase 3 exit

Required:

- User identity stable;
- Actor abstraction stable;
- Profile boundary stable;
- uniqueness constraints understood;
- lifecycle does not break historical attribution.

# Phase 4 — Authentication and Session core

## 39. IA-AUTH-001 — classify current auth mechanisms

Inspect:

```text
Identity/Auth
Identity/Credentials
Identity/Registration
Identity/OAuth
Identity/SSO
Identity/Sessions
```

Create table:

```text
Mechanism:
Purpose:
Primary auth / secondary auth / linking / enterprise SSO:
Creates session?:
Requires existing User?:
External provider?:
Current completeness:
SPEC group:
```

## 40. IA-AUTH-002 — local credentials contract

If local credential authentication is supported:

Verify:

- identifier normalization;
- password verification;
- hashing;
- failure normalization;
- lock/rate mechanism integration;
- session creation.

### Requirements

```text
IAREQ012–IAREQ015
IAREQ093–IAREQ098
```

Do not introduce password auth if source/product does not support it.

## 41. IA-AUTH-003 — auth result normalization

All auth mechanisms should converge on canonical:

```text
User
Actor
Session where browser/session flow applies
```

No parallel user store.

## 42. IA-SESSION-001 — Session Domain audit

Inspect current `Identity/Sessions`.

Map:

- Session aggregate/entity;
- state;
- expiry;
- revocation;
- User association;
- Account association if any;
- metadata;
- events.

### Requirements

```text
IAREQ016–IAREQ023
IAREQ102
IAREQ117
```

## 43. IA-SESSION-002 — session creation

Ensure successful authentication creates authoritative session state according to architecture.

### Work surfaces

Domain:
- invariant/state.

Application:
- command/use case.

Infrastructure:
- persistence/cache.

API:
- transport/cookie.

Platform:
- generic transport only.

## 44. IA-SESSION-003 — current-session bootstrap

Implement/verify deterministic "current identity/session" query.

Must not require each feature to probe auth independently.

### Requirements

```text
IAREQ018
```

## 45. IA-SESSION-004 — expiry

Verify expiry across:

- Domain state;
- persistence;
- cache;
- runtime auth;
- API failure;
- tests.

Expired session must cease authorizing protected operations.

## 46. IA-SESSION-005 — logout current session

Implement/verify authoritative current-session revocation.

Do not report success before durable security state is committed where current architecture requires persistence.

## 47. IA-SESSION-006 — revoke all/select sessions

Only implement operations already product-defined.

If current source supports session management, align it with SPEC.

## 48. IA-SESSION-007 — User disable interaction

Verify disabling/deleting User invalidates or blocks existing sessions according to policy.

### Stop

If User status and session validity disagree with no canonical rule.

## 49. IA-SESSION-008 — revocation cache proof

If session validity is cached:

Test/measure stale revocation window.

Do not optimize until correctness is known.

## 50. Phase 4 exit

Required:

```text
User → Actor → Session
```

flow stable enough for P1 core.

OAuth/MFA need not yet be complete.

# Phase 5 — Account / Current Account / Tenant isolation

## 51. IA-ACC-001 — implement selected Account ownership action

Execute only the decision from Phase 1:

```text
RETAIN | REHOME | SPLIT | INTRODUCE
```

### Mandatory preservation

No new production project.

No duplicate Account source of truth.

## 52. IA-ACC-002 — Account Domain model

Where target Account Domain state belongs, implement/verify:

- stable typed ID;
- lifecycle;
- tenant boundary;
- business metadata;
- creation;
- update;
- disable/archive/delete if canonical;
- events.

### Requirements

```text
IAREQ024–IAREQ030
```

## 53. IA-ACC-003 — Account bootstrap semantics

Account creation may require initial administration.

Determine canonical relation to:

- founding User;
- Workspace creation, if any;
- Governance bootstrap;
- Billing customer creation, if any.

### Rule

Do not create synchronous cross-context cascade unless architecture explicitly requires it.

Prefer explicit orchestration/events where ownership is separate.

## 54. IA-ACC-004 — User ↔ Account relationship

Determine whether Account membership belongs here or elsewhere.

### Stop

If implementation requires inventing a new membership aggregate without product authority.

Workspace membership remains Workspace-owned.

## 55. IA-ACC-005 — current Account resolution

Implement/verify Application-facing contract for current Account.

### Requirements

```text
IAREQ031–IAREQ036
```

### Must distinguish

```text
resolution
authorization
```

An Account ID can resolve without granting access.

## 56. IA-ACC-006 — tenant spoofing protection

Add/verify paths preventing client-controlled Account ID from bypassing scope.

Test:

```text
Actor authorized in A
→ requests B ID
→ cannot read/write B
```

## 57. IA-ACC-007 — Account context in background work

Inventory Identity/Account background consumers.

Ensure explicit Account context.

Do not use:

```text
null tenant = global
```

as convenience.

## 58. IA-ACC-008 — Account lifecycle downstream contract

For disable/delete, define handoff effects to:

- Workspaces;
- Billing;
- Automation/Integrations;
- Analytics.

Do not implement all consumer behavior inside Account transaction.

## 59. IA-ACC-009 — Account persistence isolation

Verify DB-level/application-level tenant isolation mechanisms consistent with backend architecture.

Where RLS/filters exist, ensure Account semantics align.

## 60. IA-ACC-010 — Account migration

If introducing/rehoming Account:

- schema migration;
- existing rows;
- FK/reference migration;
- seed data;
- test fixtures;
- integration-event migration;
- downstream IDs.

No destructive migration without repair plan.

## 61. Phase 5 exit

Required:

- canonical Account exists;
- current Account resolution stable;
- tenant isolation proven;
- no dual Account truth;
- downstream references identified.

# Phase 6 — P1 downstream producer contract

## 62. IA-X-001 — define stable User/Actor reference contract

Downstream consumers must use:

- stable IDs;
- approved read contracts/events.

No private Identity entities.

### Requirements

```text
IAREQ037–IAREQ039
IAREQ068–IAREQ073
```

## 63. IA-X-002 — Workspace/Governance handoff

Produce explicit contract:

```text
Actor/User ID
Account ID
Account scope semantics
User availability/lifecycle semantics if needed
```

Workspace must not need:

- password;
- OAuth;
- MFA internals;
- session DB entity.

## 64. IA-X-003 — Billing handoff

Billing receives:

```text
billable Account identity
Account lifecycle fact
actor identity for billing admin
```

No Plan/Subscription in Accounts.

## 65. IA-X-004 — WorkManagement/Documents handoff

Verify existing consumers reference stable User/Account IDs and do not require Identity private reads.

## 66. IA-X-005 — background/system actor handoff

If Automation/Platform uses system/background actor semantics, inventory current mechanism.

Do not invent fake User identities.

## 67. IA-X-006 — Analytics handoff

Define allowed identity/account facts for Analytics.

Protect personal/sensitive data.

## 68. IA-X-007 — integration contract tests handoff

Document required tests for `identity-accounts.tests.md`:

```text
Workspace consumes stable Actor/Account
Billing consumes Account
downstream cannot access private Identity tables
```

# Phase 7 — P1 core verification and handoff

## 69. P1 core gate

Before Workspace/Governance broad implementation begins, prove:

| Contract | Target |
|---|---|
| User identity | D5 |
| Actor contract | D5 |
| Session identity | D4+ |
| Account identity | D5 |
| Account/Tenant boundary | D5 |
| Current Account resolution | D5 |
| Tenant isolation | D5 |
| Identity ↔ Account consumer contract | D5 |

## 70. IA-GATE-001 — core source review

Review all changes against:

```text
IAREQ001–IAREQ039
IAREQ068–IAREQ078
IAREQ089–IAREQ103
IAREQ104–IAREQ107
```

## 71. IA-GATE-002 — downstream smoke integration

At minimum:

```text
User/Actor
→ Account
→ Workspace/Governance consumer boundary
```

must compile/test through approved contracts.

## 72. IA-GATE-003 — core migration proof

If Account/User/session schema changed:

- clean DB;
- upgrade DB;
- seed/init;
- integration startup.

## 73. IA-GATE-004 — open P2

Once gate passes:

```text
Workspace & Governance
→ may start broad implementation
```

Identity team continues secondary scope.

This gate must be visible in certification evidence.

# Phase 8 — Registration and Credentials

## 74. Why after core gate

Registration/credentials are important but should not block Workspace if User/Actor/Account core is stable.

## 75. IA-REG-001 — registration semantics

Inspect current `Identity/Registration`.

Determine:

- who can register;
- what User state is created;
- whether Account is created automatically;
- whether Workspace is bootstrapped;
- verification requirements;
- duplicate handling.

### Critical rule

Registration orchestration MUST NOT silently merge context ownership.

If registration creates Account + Workspace, orchestration must make ownership explicit.

## 76. IA-REG-002 — registration idempotency/concurrency

Handle duplicate/concurrent registrations according to identifier uniqueness.

Tests belong in TESTS artifact.

## 77. IA-CRED-001 — credential lifecycle

Inspect `Identity/Credentials`.

Classify:

- set credential;
- change password;
- reset password;
- forgot-password flow;
- credential verification.

Do not invent missing flows not in product scope.

## 78. IA-CRED-002 — sensitive-operation verification

Changing credentials must follow security policy.

If current source requires re-authentication or second factor, preserve it.

## 79. IA-CRED-003 — password reset/recovery

If supported:

- opaque one-time identity;
- expiry;
- replay prevention;
- enumeration resistance;
- session invalidation policy.

## 80. IA-CRED-004 — credential migration

If hashing parameters/format change:

- support existing credentials;
- rehash-on-login or explicit migration if accepted;
- no mass plaintext handling.

# Phase 9 — OAuth and SSO

## 81. IA-OAUTH-001 — classify OAuth vs SSO

Current Application exposes both `OAuth` and `SSO`.

The PLAN MUST determine:

```text
OAuth = social/external identity linking/login?
SSO = enterprise identity federation?
SSO = wrapper over OAuth/OIDC?
SSO = placeholder?
```

Do not merge them until semantics are known.

## 82. IA-OAUTH-002 — provider contract inventory

For every supported provider:

```text
provider ID
protocol
subject identifier
scopes
callback
state/PKCE/nonce
token storage
profile mapping
link/unlink
tests
```

## 83. IA-OAUTH-003 — OAuth start

Align current Application/API flow with:

```text
IAREQ040–IAREQ047
```

Ensure state/replay protection.

## 84. IA-OAUTH-004 — OAuth callback

Validate:

- provider;
- state;
- code;
- PKCE/nonce where applicable;
- expiry;
- safe return target.

Map provider identity to canonical User.

## 85. IA-OAUTH-005 — collision matrix

Implement explicit cases:

```text
provider identity already linked to current User
provider identity linked to another User
same email but different provider subject
current User already has provider link
provider changed email
callback replay
```

### Stop

If product/security policy for same-email auto-link is undefined.

## 86. IA-OAUTH-006 — link flow

Linking requires authenticated current User.

Do not treat login callback and link callback as identical if security semantics differ.

## 87. IA-OAUTH-007 — unlink flow

Ensure viable authentication policy.

Do not strand User without auth method where policy forbids it.

## 88. IA-OAUTH-008 — provider-token protection

If access/refresh tokens persisted:

- encrypted/protected;
- no event/log leakage;
- lifecycle defined.

## 89. IA-SSO-001 — SSO source audit

Inspect current `Identity/SSO` thoroughly.

Classify:

```text
enterprise SSO
OIDC/SAML abstraction
provider-specific login
planned placeholder
```

## 90. IA-SSO-002 — SSO ownership

If SSO is enterprise identity:

- Identity owns authentication semantics;
- Account/Workspace may own organization enablement/config relationship;
- Governance owns admin permission;
- provider configuration ownership must be explicit.

### Stop

If SSO configuration requires Account/Workspace model not yet defined.

## 91. IA-SSO-003 — SSO login mapping

Ensure external subject maps to canonical User and correct Account/tenant access.

Do not grant Account membership merely because IdP assertion exists unless product contract says so.

## 92. IA-SSO-004 — domain restriction / discovery

If supported, domain/email discovery rules must be explicit and resistant to account-takeover assumptions.

# Phase 10 — MFA

## 93. IA-MFA-001 — current MFA model audit

Inspect Domain/Application/Infrastructure/API.

Record:

```text
method types
enrollment states
secret storage
challenge representation
recovery
disable/reset
session interaction
```

## 94. IA-MFA-002 — enrollment

Implement/verify:

- authenticated initiation;
- pending state;
- verification;
- activation;
- cancellation/expiry.

### Requirements

```text
IAREQ048–IAREQ054
```

## 95. IA-MFA-003 — challenge

Define where challenge sits in authentication flow.

Avoid creating a full authenticated session before MFA success unless canonical architecture explicitly supports restricted pre-auth sessions.

## 96. IA-MFA-004 — rate/replay protection

Use Platform abuse-control mechanism.

Do not create MFA-local rate framework.

## 97. IA-MFA-005 — recovery

Implement only approved recovery mechanism.

### Stop

If recovery semantics absent.

No weak "email known → disable MFA".

## 98. IA-MFA-006 — disable/reset

Require appropriate proof/authorization.

Administrative reset if supported must be auditable.

## 99. IA-MFA-007 — session impact

Explicitly implement policy for:

- enable MFA;
- disable MFA;
- reset MFA;
- recover MFA.

Determine session revocation behavior.

## 100. IA-MFA-008 — secret migration

If storage/protection changes, migrate without exposing raw secret material.

# Phase 11 — Security settings and session hardening

## 101. IA-SEC-001 — security settings classification

Inspect `Identity/Security`.

Separate:

```text
User-level security state
session security
MFA security
OAuth security
Workspace policy
```

Workspace policy must not be absorbed into Identity.

## 102. IA-SEC-002 — sensitive-operation verification

Apply approved stronger verification to:

- password change;
- MFA changes;
- OAuth link/unlink;
- token issuance;
- security identity changes.

## 103. IA-SEC-003 — security events

Map sensitive mutations to approved audit/security event mechanism.

Do not emit secrets.

## 104. IA-SEC-004 — session/device management

If current source supports listing devices/sessions:

- define metadata;
- revoke current/other;
- privacy;
- stale-state behavior.

Do not invent fingerprinting.

## 105. IA-SEC-005 — security state recovery

Operational repair for broken Identity security state must be controlled and auditable.

Do not create public/admin bypass endpoints casually.

# Phase 12 — API Tokens

## 106. IA-TOK-001 — source audit

Inspect:

```text
Domain/Identity/Tokens
Application/Features/Identity/ApiTokens
Infrastructure token persistence/verification
API endpoints
tests
```

## 107. IA-TOK-002 — token identity model

Separate:

```text
token metadata ID
raw secret
hash/protected verification material
User/Actor association
Account scope
token scope
```

## 108. IA-TOK-003 — issuance

Generate secret securely.

Return raw secret only according to approved one-time lifecycle.

Never persist raw secret where hashing is viable.

## 109. IA-TOK-004 — verification

Verify without exposing raw secret.

### Requirements

```text
IAREQ059–IAREQ067
```

## 110. IA-TOK-005 — Account scope

Token must have explicit tenant scope.

### Stop

If current token allows implicit access to all User Accounts and product policy is unclear.

## 111. IA-TOK-006 — Governance intersection

Determine how token scopes interact with Governance.

Allowed conceptual model:

```text
effective authorization
=
principal/token restriction
∩
Governance permission
```

only if architecture supports it.

Do not invent a parallel RBAC system.

## 112. IA-TOK-007 — revocation

Prove revoked token cannot continue through cache indefinitely.

## 113. IA-TOK-008 — audit

Record safe token lifecycle/use metadata without raw token.

## 114. IA-TOK-009 — migration

If token format/hash changes:

- existing-token compatibility;
- rotation;
- forced revoke;
- migration path.

# Phase 13 — API / Authorization / Event harmonization hard-close

## 115. Phase 13 closure purpose

Phase 13 is not a documentation-only consistency review.

It is the source hard-close pass for:

```text
public API/error contracts
browser CSRF transport
Application authorization ownership
Account/Governance action semantics
Identity/Account public event inventory
public event payload safety
public event version/compatibility
```

A work unit may be classified `NOT_APPLICABLE` only when the SPEC explicitly permits it.

A work unit that remains source-actionable MUST NOT be marked `DEFERRED` merely because environment/runtime evidence is unavailable.

## 116. P13-CLOSE-00 — reset Phase 13 status to evidence-based truth

### Baseline

Use the exact current source baseline before execution.

For this closure revision the audited baseline is:

```text
branch: develop
SHA: 4efd37bdff79f93f97059586928aa94af67ba8b1
```

Re-run baseline capture if HEAD changes before implementation.

### Already accepted at audited baseline

Do not revert or redesign these without a concrete regression discovered during closure:

| Work unit | Status | Audited commit |
|---|---|---|
| IA-API-002 | DONE | `5bc9ec91` |
| IA-AUTHZ-001 | DONE | `75d4d811` |
| IA-AUTHZ-002 | DONE | `06c3269c` |
| IA-API-004 | DONE | `4efd37bd` |

### Reopen/continue

Record these as source work, not generic deferral:

```text
IA-API-003   OPEN
IA-AUTHZ-003 OPEN
IA-AUTHZ-004 PARTIAL
IA-EVT-001   OPEN
IA-EVT-002   OPEN
IA-EVT-003   OPEN
```

### Required output

Execution note:

```text
Phase 13 closure baseline
Branch:
SHA:
Accepted units:
Open units:
Source debt discovered since original PLAN:
Architecture decisions superseded/required:
```

### Exit

The phase status table must reflect source truth before any implementation starts.

---

# Phase 13A — Browser CSRF closure

## 117. P13-CSRF-01 — supersede the stale browser CSRF transport contract

### Problem

Current source contains an incompatible cross-stack contract:

Backend:

```text
cookie: csrf_token
header: X-CSRF-Token
cookie: host-scoped
SameSite: Strict
middleware emits cookie on GET
```

Frontend:

```text
reads meta csrf-token or XSRF-TOKEN from document.cookie
sends X-XSRF-TOKEN
refreshOnce() uses raw fetch and sends no CSRF header
```

A frontend hosted on a different origin cannot read the API host-only cookie, so renaming only the frontend header/cookie is insufficient.

### Required source inspection

Inspect and preserve valid behavior in:

```text
backend/docs/decisions/ADR-003-csrf-protection.md
backend/src/Notrelix.Infrastructure/Auth/Csrf/CsrfProtector.cs
backend/src/Notrelix.API/Middleware/CsrfValidationMiddleware.cs
backend/src/Notrelix.Infrastructure/Auth/Cookies/CookieService.cs
backend/src/Notrelix.API/appsettings*.json
backend/src/Notrelix.API/Endpoints/Identity/Auth/**
frontend/packages/foundation/contracts/src/client/csrf.ts
frontend/packages/foundation/contracts/src/client/api-client.ts
frontend/docs/decisions/FE-ADR-005-auth-session-model.md
```

### Architecture decision

Create a new ADR or amend the current accepted decision using the repository's ADR governance so that ADR-003 is explicitly superseded where its JS-readable-cookie assumption conflicts with the supported cross-origin topology.

The accepted target is:

```text
GET <existing Identity/Auth group>/csrf
→ random token
→ Set-Cookie: csrf_token=<token>
→ JSON body carries the same token

unsafe browser request
→ Cookie sent by browser
→ X-CSRF-Token sent by client
→ fixed-time equality validation
```

The client reads the response body, not the API cookie.

### Required policy

```text
production csrf cookie:
  Secure = true
  SameSite = None
  Path = /
  host-scoped

development:
  SameSite = Lax
  Path = /
```

Do not broaden the cookie `Domain` to sibling subdomains merely to let JavaScript read it.

### Prohibited implementation

Do not:

- keep `XSRF-TOKEN`/`X-XSRF-TOKEN` as a second convention;
- use localStorage/sessionStorage for CSRF token persistence;
- rely on `<meta name="csrf-token">` as canonical runtime bootstrap;
- silently change auth cookie policy;
- enable CSRF in production before frontend compatibility is green.

### Required docs output

The decision must define:

```text
protocol
bootstrap route ownership
cookie/header names
SameSite/Secure behavior
token lifetime/renewal behavior
frontend memory lifecycle
refresh behavior
non-browser principal behavior
feature-flag rollout
```

### Required endpoint-applicability inventory

Before implementing/enabling the classifier, inventory the candidate SHA from source.

The audited API already exposes semantic mapping families through the existing endpoint mapping extensions:

```text
Public
Authenticated
Account
Workspace
Resource
Admin
Internal
```

Identity release endpoints currently include the `Identity/Auth`, `Identity/ApiTokens` and `Identity/Profile` endpoint groups, while Account/Workspace mutations can physically live under their owning endpoint groups. Do not limit the scan to one directory merely because this is the Identity & Accounts workstream.

For every release-scoped unsafe operation owned by this workstream, record:

```text
endpoint/operation
HTTP method
semantic endpoint family
accepted authentication mode(s)
ambient cookie session used/established? yes/no
CSRF_REQUIRED | CSRF_NOT_REQUIRED
reason
test ID / representative test
```

Rules:

- discover endpoint registrations from candidate source;
- classify from credential/session semantics, not from route name;
- a public unsafe endpoint that establishes a cookie session is not automatically exempt;
- an endpoint supporting explicit API-token/service authentication is not automatically browser-CSRF-required for that non-ambient mode;
- new unsafe endpoint/auth-mode drift must fail the closure gate until classified;
- the evidence table is not a production path allowlist.

Required verification is `IA-TST-CSRF-ARCH-001`.

### Exit

No architectural ambiguity remains about how the browser obtains and returns the token in a cross-origin deployment, and no release-scoped unsafe operation in this workstream remains CSRF-unclassified.

## 118. P13-CSRF-02 — backend bootstrap and enforcement implementation

### Goal

Implement the canonical browser CSRF contract without endpoint-local conventions.

### Primary source surfaces

Modify the current equivalents of:

```text
backend/src/Notrelix.Infrastructure/Auth/Csrf/CsrfProtector.cs
backend/src/Notrelix.API/Middleware/CsrfValidationMiddleware.cs
backend/src/Notrelix.API/Endpoints/Identity/Auth/**
backend/src/Notrelix.API/appsettings*.json
backend API error writer / ProblemDetails infrastructure
backend DI registration for CSRF services
```

### Step 1 — token primitive

`CsrfProtector` must own only reusable token mechanics:

```text
generate secure random token
set canonical csrf_token cookie
fixed-time compare cookie/header
identify unsafe methods or expose reusable validation primitive
```

Remove the assumption that JavaScript must read the cookie.

### Step 2 — bootstrap endpoint

Add one safe GET endpoint to the existing Identity/Auth route group.

Response shape must be minimal and typed, conceptually:

```json
{
  "token": "..."
}
```

The same generated token is placed in the `csrf_token` cookie.

Do not create a new top-level API group solely for CSRF.

### Step 3 — middleware behavior

Remove implicit CSRF-cookie issuance from every GET.

Middleware should validate only when the canonical browser-CSRF applicability classifier says the request requires it.

The implementation MUST NOT be a hard-coded list of endpoint path strings.

The target behavior is:

```text
safe request
→ no CSRF validation

unsafe browser cookie-session request
→ require cookie + X-CSRF-Token

unsafe API-token/non-ambient request
→ do not require browser CSRF
```

Browser operations that establish/refresh/revoke cookie session state must follow the approved browser classification from P13-CSRF-01.

### Step 4 — error mapping

CSRF failure must use canonical Phase 13 API ProblemDetails/error writer.

Do not keep an anonymous middleware-only JSON shape if that bypasses the canonical writer.

Expected semantic result:

```text
403 forbidden/security failure
stable problem type/code
correlation/error metadata consistent with API policy
no token details in response/log
```

### Step 5 — configuration

`Security:Csrf:Enabled` remains false/default-safe during implementation unless test configuration explicitly enables it.

Production enablement occurs only at P13-CSRF-04 exit.

### Tests required before commit

At minimum backend API/Platform tests for:

```text
disabled flag → middleware does not interfere
bootstrap → body token + csrf_token cookie
bootstrap cookie attributes in production policy
safe GET → no validation
unsafe required + valid pair → continues
unsafe required + missing cookie → 403
unsafe required + missing header → 403
unsafe required + mismatch → 403
API-token/non-browser credential path → not browser-CSRF rejected
CSRF failure → canonical ProblemDetails
```

### Exit

Backend owns one working canonical protocol with no implicit every-GET issuance.

## 119. P13-CSRF-03 — frontend transport reconciliation

### Goal

Make the shared frontend API transport conform to backend CSRF without direct API-cookie reads.

### Primary source surfaces

```text
frontend/packages/foundation/contracts/src/client/csrf.ts
frontend/packages/foundation/contracts/src/client/api-client.ts
frontend typed endpoint definitions for auth/csrf bootstrap
frontend client tests
```

### Required design

Replace `getCsrfToken()` document-cookie/meta behavior with an instance-scoped provider owned by `createNotrelixClient` or its canonical transport dependency.

Conceptually:

```text
csrfToken: string | null
csrfBootstrapPromise: Promise<string> | null

ensureCsrfToken()
  if token exists → return it
  if bootstrap in-flight → await same promise
  else GET auth/csrf with credentials: include
       validate response
       store token in memory
       return token
```

### Unsafe request path

Before a CSRF-required unsafe browser request:

```text
ensureCsrfToken()
→ X-CSRF-Token: <token>
→ credentials: include
```

### Refresh path

`refreshOnce()` MUST NOT remain a raw fetch branch that omits shared security transport.

Refactor it to reuse the same CSRF-aware low-level request primitive while preserving:

```text
single-flight refresh
correlation ID
no infinite refresh recursion
session-expired callback semantics
one retry only
```

Do not call the high-level API method in a way that recursively triggers refresh.

### Token invalidation/rebootstrap

Define deterministic behavior for a CSRF failure attributable to stale/rotated token:

```text
clear in-memory token
bootstrap once
retry the original unsafe request at most once if policy allows
```

Do not create an unbounded CSRF retry loop.

If canonical API policy does not permit automatic retry after 403, surface the failure and require the next unsafe request to rebootstrap; encode whichever policy P13-CSRF-01 accepts.

### Prohibited implementation

Remove canonical dependence on:

```text
document.cookie XSRF-TOKEN
meta csrf-token
X-XSRF-TOKEN
persistent browser storage
```

### Tests required

```text
bootstrap is single-flight
bootstrap uses credentials include
memory token reused
GET does not bootstrap unnecessarily
unsafe request attaches X-CSRF-Token
refresh attaches X-CSRF-Token
multiple simultaneous unsafe requests share bootstrap
no local/session storage write
no document.cookie dependency
retry/invalidation bounded
```

### Exit

All browser transport paths, including refresh, use one CSRF-aware primitive.

## 120. P13-CSRF-04 — cross-stack integration and enablement gate

### Required integration scenarios

Use the supported frontend/API origins/topology rather than same-origin-only mocks.

Mandatory flow:

```text
1. clean browser/client instance
2. bootstrap CSRF
3. login or establish cookie session as supported
4. unsafe authenticated mutation succeeds
5. refresh session succeeds with CSRF
6. unsafe mutation after refresh succeeds
7. reload/new client memory state
8. bootstrap again
9. unsafe mutation succeeds
```

Negative flow:

```text
missing header → rejected
wrong header → rejected
stale token according to accepted policy → deterministic recovery/rejection
API-token request → unaffected by browser CSRF
```

### Rollout gate

Only after backend + frontend + integration tests pass:

```text
Security:Csrf:Enabled = true
```

for the intended deployable environment/configuration.

If config differs by environment, document exact rollout expectation.

### Exit

`IA-API-003 = DONE` only here.

---

# Phase 13B — Authorization closure

## 121. P13-AUTHZ-003A — freeze current Account role/action baseline

### Source evidence

Current Domain roles:

```text
Owner
Admin
Member
BillingAdmin
SecurityAdmin
```

Current Governance actions include `ViewWorkspace` and `CreateWorkspace`.

Current `PermissionService.EvaluateAccountAsync`:

```text
requires active AccountMember
Owner → allow
then explicit PermissionRule evaluation
ViewWorkspace → default allow
other action → deny
```

This currently denies Admin `CreateWorkspace` without an explicit rule even though Account admin semantics are not frozen.

### Closure decision

For current account-scope actions only:

| AccountRole | ViewWorkspace | CreateWorkspace |
|---|---:|---:|
| Owner | allow | allow |
| Admin | allow | allow |
| Member | allow | deny |
| BillingAdmin | allow | deny |
| SecurityAdmin | allow | deny |

### Decision order

Preserve one central evaluator:

```text
active membership
→ Owner authority
→ explicit Governance rule (deny/allow)
→ role baseline fallback
→ deny
```

Do not create role checks in `CreateWorkspaceCommandHandler`.

### Scope boundary

Do NOT add speculative actions to `PermissionAction` for billing/security simply because role names exist.

Future real use cases must add actions through Governance/product authority.

### Required tests

For each of five roles verify current action baseline.

Also verify:

```text
inactive member → deny
missing member → deny
wrong Account → deny
explicit Governance deny overrides Admin fallback
explicit Governance allow may grant otherwise-denied non-owner action when policy supports it
Owner behavior remains canonical
```

### Exit

A coding agent no longer needs to infer what `Admin` means for current Account-scope actions.

## 122. P13-AUTHZ-003B — implement centralized Account policy

### Primary source surfaces

```text
backend/src/Notrelix.Application/Common/Security/PermissionService.cs
backend/src/Notrelix.Domain/Accounts/Members/AccountRole.cs
backend/src/Notrelix.Domain/Governance/Permissions/PermissionAction.cs
backend/src/Notrelix.Application/Features/Workspaces/Workspaces/Commands/CreateWorkspace/**
backend Application/Architecture tests
```

### Required implementation

Implement the baseline inside the canonical centralized evaluator or a single policy helper owned by the same authorization mechanism.

If extracted, the helper must be a policy table/function used only by `PermissionService`; it must not become a second authorization service.

`CreateWorkspaceCommand` remains pipeline-authorized.

The handler must not ask for current role or call permission service again.

### Exit

`IA-AUTHZ-003 = DONE` when policy and negative tests pass.

## 123. P13-AUTHZ-004A — inventory handler/endpoint authorization bypasses

### Problem

The existing API source gate that forbids raw endpoint `.RequireAuthorization()`/`.AllowAnonymous()` usage does not prove Application handlers are free of bypasses.

### Search scope

Scan production Application/API source for:

```text
IPermissionService
IPermissionEvaluator
IWorkspacePermissionService
IAuthorizationDecisionStore
EnsureAllowedAsync
AuthorizeAsync
HasPermissionAsync
AccountRole.
WorkspaceRole.
member.Role
current actor/current user role checks
membership query followed by Forbidden/Unauthorized
```

Use actual source names discovered at the candidate SHA.

### Classification table

For every hit record:

```text
File/type:
Operation/request:
Current actor or target entity?:
Authorization decision or business invariant?:
Already pipeline-protected?:
Classification:
  REMOVE_BYPASS
  RETAIN_BUSINESS_INVARIANT
  RETAIN_APPROVED_SPECIAL_CONTRACT
Reason:
Required test:
```

### Mandatory semantic rule

Current-actor role/membership checks deciding whether a protected request may execute are presumptive bypasses.

Target member/entity role checks after authorization may be legitimate Domain/Application business invariants.

### Exit

No role/permission hit remains unclassified.

## 124. P13-AUTHZ-004B — executable authorization bypass architecture gate

### Goal

Turn IA-AUTHZ-004 from a one-time source review into a regression gate.

### Primary test surface

Extend the existing Architecture test project rather than creating a new project.

Add a focused authorization gate under the current architecture-test organization.

### Gate A — direct service use

Protected Application handlers MUST NOT directly inject/use canonical authorization services when authorization belongs to the pipeline.

At minimum guard current equivalents of:

```text
IPermissionService
IPermissionEvaluator
IWorkspacePermissionService
IAuthorizationDecisionStore
```

### Gate B — request declaration

Protected requests must declare one accepted authorization contract:

```text
IRequirePermission
or approved explicit self-service/special contract
```

Do not infer protection from handler implementation.

### Gate C — role-check registry

Role checks in handlers must either:

```text
be absent for current-actor authorization
or
exist in an exact allowlist as a business invariant
```

Allowed exception metadata must contain:

```text
exact type/file
exact reason
invariant owner
review trigger
```

No directory/namespace wildcard.

### Gate D — endpoint raw auth

Preserve the existing endpoint raw-auth architecture gate.

Do not weaken it to accommodate closure changes.

### Exit

`IA-AUTHZ-004 = DONE` only when:

```text
inventory complete
true bypasses removed
exact legitimate exceptions recorded
architecture gates green
```

---

# Phase 13C — Event inventory and payload closure

## 125. P13-EVT-001A — build source-complete event inventory

### Goal

Inventory public Identity/Account integration contracts from source, independent of runtime queue state.

### Source surfaces

Inspect current equivalents of:

```text
Identity/Accounts Domain events
IIntegrationEvent implementations
EventNameAttribute metadata
IntegrationEventCatalog
ContractRegistrySetup
ConsumerRegistrySetup
IConsumer<T> implementations
stub consumer classes
outbox/envelope mappings
architecture tests
```

### Inventory fields

For every Identity/Account event:

```text
CLR/source type
Domain event / Integration event
logical name
version
producer
semantic fact
payload fields
Account scope
Workspace/resource scope if applicable
classification
PII/security content
registered consumer
actual IConsumer<T>
consumer bounded context
consumer maturity: IMPLEMENTED | STUB | NONE
compatibility status
```

### Required distinction

A `ConsumerRegistry` row does not prove implementation.

If the registered consumer is a stub, record `STUB`.

### Required output

The human inventory may live in Phase 13 execution evidence/PR notes.

The machine-verifiable canonical contract output is implemented in P13-EVT-003C.

### Exit

Every Identity/Account public integration event can be accounted for from source without querying a deployed broker.

## 126. P13-EVT-001B — event inventory drift gate

### Goal

Prevent a new event/consumer from escaping inventory/compatibility governance.

### Architecture checks

Extend existing event architecture tests so that:

```text
new IIntegrationEvent without EventName/version → fail
new public event missing contract manifest entry → fail
registered consumer missing maturity classification → fail
actual consumer/registry mismatch → fail
unexpected source event removed/renamed → fail unless contract migration updated
```

### Consumer maturity implementation

Prefer an explicit metadata value on the existing consumer contract/registry model rather than source-name heuristics.

Allowed values:

```text
Implemented
Stub
```

No registry entry means `NONE` in generated inventory.

Do not derive maturity from a class name containing "Stub" as the final architecture.

### Exit

`IA-EVT-001 = DONE` when source inventory and drift gate are deterministic.

## 127. P13-EVT-002A — prohibited secret payload gate

### Goal

Make event secret minimization system-wide for Identity/Accounts public events.

### Hard-forbidden material

Public integration-event payloads must not expose raw:

```text
Password
PasswordHash
AccessToken
RefreshToken
ApiKey/API token secret
ClientSecret
PrivateKey
Mfa/TOTP secret
RecoveryCode
Authorization header/session secret
OAuth credentials
```

Use semantic/property/type inspection and targeted tests; do not rely only on literal property names when a strongly typed secret wrapper exists.

### Review existing sensitive events

At minimum re-audit:

```text
identity.user-registered
identity.user-email-changed
OAuth/security/token lifecycle events
Account lifecycle/admin events
```

### Exit

Architecture/security tests fail if prohibited secret material enters a public Identity/Account integration contract.

## 128. P13-EVT-002B — PII classification and minimization

### Goal

Differentiate "PII intentionally required" from "PII happened to be serialized".

### For each PII-bearing public event

Record in canonical contract metadata/evidence:

```text
field
purpose
consumer(s)
why stable ID/reference is insufficient
retention/delivery implication
```

### Decision rule

If a consumer only needs stable identity and can use an approved read contract, remove unnecessary mutable PII from the integration event.

If the consumer genuinely needs email/display data for delivery semantics, retain it intentionally and classify it.

Do not remove a field solely for aesthetics if a real consumer contract requires it.

### Logging/outbox review

Ensure outbox/DLQ/error logging does not dump prohibited secret payloads or casually emit intentional PII at inappropriate log levels.

### Exit

`IA-EVT-002 = DONE` when every public payload is secret-safe and PII-bearing contracts are intentional.

---

# Phase 13D — Event versioning and compatibility closure

## 129. P13-EVT-003A — introduce compound event contract key

### Problem

Current `IntegrationEventCatalog` is keyed by logical name only and resolves with:

```text
Resolve(messageName)
TryResolve(messageName)
```

while source metadata already carries versions.

This makes version metadata non-authoritative at runtime.

### Target

Introduce one reusable value/record in the current messaging contract owner:

```text
EventContractKey
  Name
  Version
```

Do not place messaging infrastructure into Domain merely to share the key.

### Required migrations

Update current equivalents of:

```text
IIntegrationEventCatalog
IntegrationEventCatalog
ContractRegistry / ContractDefinition lookup
Event type/deserialization registry
message envelope resolution where name/version are available
consumer contract lookup
architecture tests
```

Target APIs:

```text
Resolve(name, version)
TryResolve(name, version, out type)
```

or `Resolve(EventContractKey)` equivalent.

### Backward API rule

A temporary name-only overload may exist only inside the same atomic migration if required to compile intermediate code.

It must not remain as the final production resolution path.

### Exit

Runtime resolution requires both logical name and version.

## 130. P13-EVT-003B — permit v1/v2 coexistence

### Problem

Existing uniqueness tests are inconsistent if one verifies `(Name, Version)` while another requires `Name` alone to be unique.

### Required rule

Public integration-event uniqueness is:

```text
(Name, Version) unique
```

The same logical name MAY coexist with multiple versions.

### Tests

Add fixtures/types proving:

```text
same name + v1 and v2 → valid
same name + same version twice → fail
unknown name → fail
known name + unknown version → fail deterministically
```

Update/remove any architecture test that incorrectly requires logical name alone to be unique for versioned public integration events.

Do not weaken Domain-event semantics if Domain events intentionally use a different internal naming rule; classify the two test families explicitly.

### Exit

Repository tests can represent a real v1/v2 migration without contradiction.

## 131. P13-EVT-003C — canonical event contract manifest and schema drift gate

### Canonical file

Create/use exactly:

```text
backend/contracts/events/notrelix.events.json
```

Do not create a competing second event snapshot elsewhere.

### Manifest scope and ownership boundary

The manifest is GLOBAL for backend public integration events, not scoped only to Identity/Accounts.

Phase 13 may:

```text
change Identity/Accounts event rows
change shared manifest schema/generator mechanics required by the accepted versioning design
```

Phase 13 must not silently change unrelated bounded-context business contracts:

```text
payload semantics
logical event version
producer ownership
consumer maturity
```

If the global generation/drift check exposes unrelated semantic drift, stop the affected manifest acceptance and route it to the owning bounded context. Do not edit another context's event merely to make the global snapshot green.

Domain-only internal events remain outside this public integration-event manifest unless they are explicitly promoted through the canonical integration-event contract.

Required verification is `IA-TST-EVT-CONTRACT-004`.

### Manifest source

Generate the expected model from the canonical event/consumer registries and reflection metadata through a reusable test/helper in the existing Architecture test project.

The manifest records at minimum:

```text
name
version
source type
serialized properties
serialized types/nullability
scope metadata where available
classification
PII fields/purpose marker where applicable
consumer maturity summary
```

### Compatibility policy

During foundation freeze:

```text
ANY public integration-event payload schema change
→ requires explicit version bump
```

The architecture gate must fail if:

```text
source shape changes but manifest/version does not
manifest changes without source/accepted contract reason
same (name, version) has incompatible source shape
```

### Serialization fidelity

Use the actual serializer naming/nullability conventions used by production messaging.

Do not fingerprint raw CLR reflection names if production serialization transforms them differently.

### Exit

A public event schema cannot drift silently.

## 132. P13-EVT-003D — migration protocol and consumer dual-read

### Required protocol

For any actual breaking event change required by Phase 13:

```text
add v2
→ consumer dual-read
→ verify v2
→ switch producer
→ drain v1 backlog
→ retire v1 consumer
→ remove v1 after retention/rollback window
```

### Current closure behavior

If Phase 13 requires only infrastructure support and no existing public payload is being broken:

```text
implement version-aware infrastructure
add v1/v2 coexistence tests
create manifest
record "no producer contract bump required"
```

Do not invent v2 event versions unnecessarily.

### Exit

`IA-EVT-003 = DONE` when version identity, coexistence, schema guard and migration protocol all exist.

## 133. P13-EVT-OPS-001 — operational backlog/DLQ evidence

### Scope

This work unit is operational evidence only.

It does not own source event inventory/versioning.

### Evidence categories

Where an environment/platform supports inspection, record:

```text
pending outbox by event/version
oldest pending age
retry backlog by consumer
DLQ/poison by event/version
unsupported-version count
```

### Status

Allowed final states:

```text
VERIFIED
NOT_APPLICABLE_UNTIL_DEPLOYMENT
```

`DEFERRED` is not a substitute for completing IA-EVT-001/002/003.

---

# Phase 13E — Hard-close gate

## 134. P13-FINAL-01 — Phase 13 hard-close certification

### Required source status

All must be DONE:

```text
IA-API-002
IA-API-003
IA-API-004
IA-AUTHZ-001
IA-AUTHZ-002
IA-AUTHZ-003
IA-AUTHZ-004
IA-EVT-001
IA-EVT-002
IA-EVT-003
```

### Required tests

Run canonical suites for affected code:

```text
Architecture
Domain where events/invariants changed
Application
Infrastructure
Platform
API
Integration
frontend contract/client tests for CSRF
```

### Required generated checks

```text
OpenAPI drift clean
event contract manifest drift clean
authorization bypass gate clean
```

### Required report

```text
Phase 13 candidate SHA:
Work unit status table:
Backend suites + counts:
Frontend CSRF suite result:
OpenAPI result:
Event manifest result:
Operational event evidence:
Known exclusions:
```

### Stop

If any source-level unit is OPEN/PARTIAL/DEFERRED:

```text
Phase 13 NOT DONE
```

---

# Phase 14 — Persistence / Migration / Compatibility closure

## 135. P14-MIG-001 — candidate schema diff review

### Goal

Ensure Phase 13 closure does not accidentally introduce unreviewed persistence changes.

### Required actions

Compare candidate model/migrations against the pre-closure baseline.

Classify every change:

```text
no schema change
additive
backfill-required
breaking
index/constraint
secret format
ownership move
```

### Expected closure baseline

CSRF transport and event registry changes should normally require no business DB migration.

If implementation introduces persisted event-contract metadata or a new DB column, justify why source/config metadata was insufficient before accepting it.

### Tests

If EF model changes:

```text
fresh database
upgrade database
pending-model gate
seed/init
startup
```

### Exit

No unexplained persistence delta remains.

## 136. P14-MIG-002 — event registry caller compatibility

### Goal

Treat the name-only → `(Name, Version)` migration as a compatibility change even without DB schema change.

### Required inventory

Find all callers of:

```text
Resolve(messageName)
TryResolve(messageName)
contract registry name lookup
event deserialization by name
consumer endpoint/contract version assumptions
```

### Required result

Every production call site either:

```text
passes envelope/event version
or
is explicitly proven not to be a public integration-event resolution path
```

No hidden name-only fallback remains.

## 137. P14-MIG-003 — CSRF deployment compatibility

### Goal

Prevent frontend/backend rollout ordering from causing mass 403 failures.

### Required rollout sequence

Preferred:

```text
1. deploy backend bootstrap/protocol while Security:Csrf:Enabled=false
2. deploy compatible frontend transport
3. verify integration/smoke
4. enable CSRF
```

If backend/frontend deploy atomically, record that evidence.

### Rollback

Rollback must not require reintroducing the old XSRF convention.

If urgent rollback disables the feature flag, record it as operational rollback, not target architecture.

## 138. P14-MIG-004 — public contract compatibility review

Review Phase 13 changes for:

```text
OpenAPI
browser CSRF bootstrap response
event contract manifest
consumer registry metadata
public event versions
```

Any breaking consumer contract needs rollout/forward-fix evidence.

## 139. Phase 14 exit

Phase 14 closes only when:

- DB migration applicability is explicit;
- event registry call sites are version-aware;
- CSRF rollout order is executable;
- public contract compatibility is recorded;
- no pending EF model change exists if EF changed.

---

# Phase 15 — Security hardening closure

## 140. P15-SEC-001 — full secret surface scan

Review Identity/Account:

```text
API DTOs
ProblemDetails
logs/traces
Domain/integration events
outbox serialization
DLQ/poison diagnostics
OAuth/MFA/API-token persistence
CSRF diagnostics
```

Prohibited raw material includes:

```text
password/hash
session/JWT/refresh secret
OAuth token
MFA secret/recovery code
raw API token
Authorization header
client/private secret
CSRF token in ordinary logs
```

## 141. P15-SEC-002 — CSRF attack matrix

Verify:

```text
missing token
mismatch
token from another browser/client instance
stale token behavior
unsafe request before bootstrap
refresh without token
cross-origin credentials
API-token request without CSRF
safe GET
```

Do not treat CORS as CSRF protection.

## 142. P15-SEC-003 — authorization bypass matrix

Verify:

```text
Owner current actions
Admin current actions
Member denial
BillingAdmin default denial for CreateWorkspace
SecurityAdmin default denial for CreateWorkspace
explicit Governance deny
explicit Governance allow where supported
inactive membership
wrong Account
handler cannot run before pipeline denial
```

## 143. P15-SEC-004 — event privacy/security matrix

Verify:

```text
prohibited secret properties absent
intentional PII classified
manifest does not expose secret default values
outbox/DLQ logging redacts sensitive payloads
unsupported version failure does not dump secret payload
```

## 144. P15-SEC-005 — enumeration/replay/revocation regression

Re-run existing security matrices for:

```text
login/recovery
OAuth replay/collision
MFA challenge/recovery
session revoke/refresh race
API-token revoke/use race
```

Closure work must not weaken earlier phases.

## 145. P15-SEC-006 — abuse controls

Confirm sensitive operations use the existing Platform abuse-control mechanism where required.

Do not create a CSRF-local or Identity-local rate-limit framework.

## 146. Phase 15 exit

No material high-sensitivity path lacks a negative/security test.

---

# Phase 16 — Observability / Reliability / Performance closure

## 147. P16-OBS-001 — safe trace correlation

Critical path correlation should remain possible:

```text
request
→ authentication/session
→ Account resolution
→ authorization
→ use case
→ persistence
→ event/outbox
```

Allowed safe context may include stable IDs/correlation IDs.

Never raw secrets.

## 148. P16-OBS-002 — closure failure categories

Using existing observability infrastructure, expose/retain diagnosable categories for:

```text
csrf_validation_failed
authorization_denied
authorization_misconfiguration
unknown_event_contract
unsupported_event_version
consumer_retry/poison where platform exposes them
```

Exact metric names follow existing conventions; do not add a new observability vendor.

## 149. P16-REL-001 — unknown/unsupported event behavior

Unknown logical event name or unsupported version must fail deterministically.

It must not:

- deserialize into an arbitrary fallback type;
- silently use the latest version;
- drop the message as success;
- expose payload secrets in logs.

Use existing poison/retry policy after type resolution failure as architecture dictates.

## 150. P16-REL-002 — CSRF bootstrap/refresh failure behavior

Verify deterministic behavior for:

```text
bootstrap network failure
bootstrap 4xx/5xx
refresh 401/403
CSRF 403
concurrent bootstrap
concurrent refresh
```

No infinite retry loop or request storm.

## 151. P16-PERF-001 — authorization hot path regression

Closure must not cause:

```text
pipeline permission evaluation
+
handler permission evaluation
```

for the same operation.

Review query counts for representative Account/Workspace operations.

## 152. P16-PERF-002 — CSRF client request overhead

After a valid in-memory token exists, normal unsafe requests should not bootstrap again unnecessarily.

Concurrent unsafe requests should share one bootstrap when no token exists.

## 153. Phase 16 exit

Correctness/reliability is proven without redundant auth/CSRF work or unbounded retry behavior.

---

# Phase 17 — Cross-team integration hardening closure

## 154. P17-X-001 — frontend/browser contract handoff

### Required contract

Frontend must consume:

```text
GET auth/csrf bootstrap
csrf_token cookie implicitly
X-CSRF-Token header explicitly
canonical ProblemDetails/error taxonomy
```

No frontend package may retain a competing XSRF convention.

Search the frontend repository for:

```text
XSRF-TOKEN
X-XSRF-TOKEN
X-CSRF-Token
csrf_token
getCsrfToken
```

Classify all hits.

## 155. P17-X-002 — Workspace/Governance authorization handoff

Verify representative current Account-scoped operations use the same Governance/permission mechanism.

Workspace/Governance must not need to inspect Identity handlers or AccountRole directly to compensate for missing authorization semantics.

## 156. P17-X-003 — event consumer handoff

For Workspace/Billing/other actual consumers of Identity/Account events:

```text
consumer uses stable IDs/scope
consumer version is explicit
consumer maturity classified
no private Identity persistence access
```

If a consumer is only a stub, record it as STUB rather than integrated.

## 157. P17-X-004 — v1/v2 migration fixture

Even if no production event is bumped during closure, add at least one controlled contract fixture proving the platform can host v1 and v2 of one logical event without registry collision.

Do not create a fake production event name that leaks into runtime registration; use test fixtures or dedicated test assembly/types.

## 158. P17-X-005 — operational event evidence handoff

If a deployed environment exists, collect P13-EVT-OPS-001 evidence.

If not:

```text
NOT_APPLICABLE_UNTIL_DEPLOYMENT
```

with the reason and future runbook trigger.

## 159. Phase 17 exit

Cross-stack and cross-context consumers no longer depend on the pre-closure ambiguities.

---

# Phase 18 — TESTS handoff

## 160. IA-TEST-HO-001 — sync TESTS with closure implementation

`identity-accounts.tests.md` must contain concrete test IDs for every Phase 13–17 closure requirement.

Mandatory families:

```text
IA-TST-CSRF-*
IA-TST-AUTHZ-*
IA-TST-EVT-INV-*
IA-TST-EVT-SEC-*
IA-TST-EVT-VER-*
IA-TST-EVT-CONTRACT-*
IA-TST-MIG-*
IA-TST-OBS-*
IA-TST-REL-*
IA-TST-X-*
```

## 161. IA-TEST-HO-002 — map tests to actual projects

For every required test record:

```text
Test ID
Requirement IDs
Source under test
Project/suite
CI job
positive/negative/security/concurrency classification
```

Do not leave a test at "architecture/source review" if the closure requirement is machine-enforceable.

## 162. IA-TEST-HO-003 — full regression

Run canonical backend suites:

```text
Architecture
Domain
Application
Infrastructure
Platform
API
Integration
```

and required frontend contract/client tests.

Record actual counts rather than carrying forward counts from the pre-closure SHA.

## 163. IA-TEST-HO-004 — non-zero execution

Every CI filter used as evidence must execute intended tests.

Green zero-test execution is not evidence.

## 164. Phase 18 exit

TESTS has complete Phase 13–17 traceability and actual implementation has no material missing test.

---

# Phase 19 — Documentation and generated-contract handoff

## 165. IA-DOC-001 — update CSRF authority

Update/supersede:

```text
backend/docs/decisions/ADR-003-csrf-protection.md
frontend/docs/decisions/FE-ADR-005-auth-session-model.md
```

or their current canonical equivalents.

Both must describe the same protocol.

Do not leave the old XSRF convention documented as active source truth.

## 166. IA-DOC-002 — canonical API/OpenAPI evidence

If CSRF bootstrap changes public API:

```text
regenerate canonical OpenAPI
update typed/generated clients if repository workflow requires
run drift gate
```

No handwritten drift.

## 167. IA-DOC-003 — event contract artifact

Ensure:

```text
backend/contracts/events/notrelix.events.json
```

matches candidate source and is checked by CI/architecture tests.

If generated docs/indexes include contract artifacts, regenerate through repository tooling.

## 168. IA-DOC-004 — authorization architecture docs

If canonical security/authorization docs do not already express the Phase 13 single-authority rule, update them to state:

```text
protected Application use case
→ request authorization contract
→ AuthorizationBehavior
→ handler business logic
```

and explain that target-role business invariants are distinct from current-actor authorization.

## 169. IA-DOC-005 — event versioning architecture docs

Update the canonical events/realtime/delivery architecture to define:

```text
EventContractKey(Name, Version)
v1/v2 coexistence
schema baseline
consumer maturity
producer/consumer rollout
outbox/DLQ drain before retirement
```

## 170. IA-DOC-006 — workstream status

Update Identity & Accounts execution state only from evidence.

Phase 13 may be marked DONE only after P13-FINAL-01.

## 171. Phase 19 exit

No canonical document contradicts implemented CSRF/authz/event closure contracts.

---

# Phase 20 — Final certification handoff

## 172. IA-CERT-HO-001 — prepare exact candidate evidence

Certification inputs must include:

```text
branch
candidate SHA
clean/known working-tree state
migration head
OpenAPI checksum/drift result
event contract manifest checksum/drift result
Phase 13 work-unit table
Phase 14 compatibility result
Phase 15 security result
Phase 16 reliability/performance result
Phase 17 cross-team result
backend test suites/counts
frontend CSRF/client suite result
CI jobs for exact SHA
```

## 173. IA-CERT-HO-002 — Phase 13 closure table

Certification must explicitly contain:

| Work unit | Required final status |
|---|---|
| IA-API-002 | DONE |
| IA-API-003 | DONE |
| IA-API-004 | DONE |
| IA-AUTHZ-001 | DONE |
| IA-AUTHZ-002 | DONE |
| IA-AUTHZ-003 | DONE |
| IA-AUTHZ-004 | DONE |
| IA-EVT-001 | DONE |
| IA-EVT-002 | DONE |
| IA-EVT-003 | DONE |
| IA-EVT-OPS | VERIFIED or NOT_APPLICABLE_UNTIL_DEPLOYMENT |

No source-level `DEFERRED` status is accepted.

## 174. IA-CERT-HO-003 — no prefilled PASS

Do not prefill PASS before commands/CI complete on the exact candidate SHA.

## 175. IA-CERT-HO-004 — closure blocker rule

Certification must fail if any of the following remain:

```text
frontend/backend CSRF convention mismatch
CSRF disabled only because frontend is incompatible
handler authorization bypass unclassified
Account Admin/CreateWorkspace semantics unresolved
name-only integration-event runtime resolution
event schema/version drift gate absent
prohibited secret in public event payload
unclassified material PII in public event
required backend suite failing/zero-executed
OpenAPI/event manifest drift
```

## 176. Phase 20 exit

`IDENTITY & ACCOUNTS TEAM SCOPE CERTIFIED` may be issued only when certification can prove the complete closure chain without unresolved critical ambiguity.

---

# Phase 13–20 execution order

## 177. Required serial order

Execute:

```text
P13-CLOSE-00
↓
P13-CSRF-01
↓
P13-CSRF-02
↓
P13-CSRF-03
↓
P13-CSRF-04
↓
P13-AUTHZ-003A
↓
P13-AUTHZ-003B
↓
P13-AUTHZ-004A
↓
P13-AUTHZ-004B
↓
P13-EVT-001A
↓
P13-EVT-001B
↓
P13-EVT-002A
↓
P13-EVT-002B
↓
P13-EVT-003A
↓
P13-EVT-003B
↓
P13-EVT-003C
↓
P13-EVT-003D
↓
P13-EVT-OPS-001
↓
P13-FINAL-01
↓
Phase 14 compatibility
↓
Phase 15 security
↓
Phase 16 reliability/performance
↓
Phase 17 cross-team integration
↓
Phase 18 tests handoff
↓
Phase 19 docs/generated contracts
↓
Phase 20 certification
```

## 178. Safe parallelization

After P13-CSRF-01 fixes the contract, backend CSRF implementation and event source inventory may proceed in parallel if they do not touch shared Platform messaging/auth files.

Authorization closure may proceed in parallel with event payload inventory after its Account role/action decision is frozen.

## 179. Unsafe parallelization

Do not independently modify:

```text
IntegrationEventCatalog/registry identity
consumer registry version semantics
event manifest format
```

across multiple PRs without one owner.

Do not enable CSRF before frontend transport is merged/deployable.

---

# Phase 13+ normative traceability matrix

## Normative mapping rule

This table supplements the detailed work-unit bodies; it does not replace them.

For every execution report, `SPEC requirements:` MUST use the requirement IDs mapped here. If implementation discovers that a work unit materially affects an additional requirement, update this matrix and the corresponding TESTS mapping before coding that semantic expansion.

A work unit with no traceable requirement/test/evidence row is not ready to close.

| Work unit / phase | Normative SPEC requirements | Mandatory TESTS families / IDs | Primary implementation surface | Required CI / artifact evidence |
|---|---|---|---|---|
| `P13-CLOSE-00` | `IAREQ140` | `IA-TST-CLOSE-001..003`, `IA-TST-TRACE-001` | execution evidence/docs | exact baseline SHA + status table |
| `P13-CSRF-01` | `IAREQ126–IAREQ130` | `IA-TST-CSRF-ARCH-001`, `IA-TST-X-CSRF-001` | ADR/API endpoint metadata/frontend contract | architecture/docs validation + ADR evidence |
| `P13-CSRF-02` | `IAREQ126–IAREQ130` | `IA-TST-CSRF-INF-001`, `IA-TST-CSRF-API-001..007`, `IA-TST-CSRF-CFG-001`, `IA-TST-CSRF-ARCH-001` | Infrastructure + API | infrastructure/api/architecture tests + OpenAPI if public contract changes |
| `P13-CSRF-03` | `IAREQ126`, `IAREQ127`, `IAREQ129`, `IAREQ130` | `IA-TST-CSRF-CLIENT-001..006`, `IA-TST-CSRF-REL-001`, `IA-TST-X-CSRF-001` | frontend contracts/client | frontend contract/client CI |
| `P13-CSRF-04` | `IAREQ128–IAREQ130`, `IAREQ140` | `IA-TST-CSRF-INT-001..002`, `IA-TST-MIG-CSRF-001..002` | API + frontend + deployment config | integration tests + enabled-config smoke |
| `P13-AUTHZ-003A` | `IAREQ090`, `IAREQ138` | `IA-TST-AUTHZ-APP-004..007` | Domain role inputs + Application policy | Application tests |
| `P13-AUTHZ-003B` | `IAREQ090`, `IAREQ136`, `IAREQ138` | `IA-TST-AUTHZ-APP-002`, `IA-TST-AUTHZ-APP-004..007`, `IA-TST-X-AUTHZ-001`, `IA-TST-PERF-001` | Application authorization evaluator | Application + integration tests |
| `P13-AUTHZ-004A` | `IAREQ136`, `IAREQ137` | `IA-TST-AUTHZ-ARCH-001..005` | Application/API source inventory | architecture-tests |
| `P13-AUTHZ-004B` | `IAREQ136`, `IAREQ137` | `IA-TST-AUTHZ-ARCH-001..005`, `IA-TST-PERF-001` | Architecture tests + Application handlers | architecture-tests + Application regression |
| `P13-EVT-001A` | `IAREQ084`, `IAREQ085`, `IAREQ087`, `IAREQ133` | `IA-TST-EVT-INV-ARCH-001..003`, `IA-TST-EVT-INT-001`, `IA-TST-X-EVT-001..003` | event/consumer registries | architecture + integration evidence |
| `P13-EVT-001B` | `IAREQ133` | `IA-TST-EVT-INV-ARCH-001..003` | Architecture tests/registry metadata | architecture-tests |
| `P13-EVT-002A` | `IAREQ086`, `IAREQ094`, `IAREQ123` | `IA-TST-EVT-SEC-001`, `IA-TST-SEC-MASTER-001`, `IA-TST-SEC-MASTER-007` | public event contracts/logging | architecture + security regression |
| `P13-EVT-002B` | `IAREQ086`, `IAREQ123–IAREQ125`, `IAREQ133` | `IA-TST-EVT-PRIV-001..002`, `IA-TST-PRIV-002..003` | event contracts/manifest metadata | architecture/privacy tests |
| `P13-EVT-003A` | `IAREQ088`, `IAREQ131`, `IAREQ139` | `IA-TST-EVT-VER-INF-001..003`, `IA-TST-MIG-EVT-001..002` | messaging catalog/registries/envelope | Infrastructure + architecture + integration |
| `P13-EVT-003B` | `IAREQ088`, `IAREQ131`, `IAREQ134` | `IA-TST-EVT-VER-ARCH-001..002` | registries + architecture tests | architecture-tests |
| `P13-EVT-003C` | `IAREQ088`, `IAREQ132`, `IAREQ133`, `IAREQ139` | `IA-TST-EVT-CONTRACT-001..004`, `IA-TST-EVT-INV-ARCH-001..003` | global event manifest/generator | architecture-tests + `backend/contracts/events/notrelix.events.json` |
| `P13-EVT-003D` | `IAREQ088`, `IAREQ134`, `IAREQ139` | `IA-TST-EVT-MIG-001..002`, `IA-TST-MIG-EVT-001..002`, `IA-TST-X-EVT-004` | producer/consumer migration path | architecture + integration evidence |
| `P13-EVT-OPS-001` | `IAREQ135` | `IA-TST-EVT-OPS-001` | operational evidence/runbook | `VERIFIED` or `NOT_APPLICABLE_UNTIL_DEPLOYMENT` |
| `P13-FINAL-01` | `IAREQ083`, `IAREQ140` plus all Phase 13 mapped requirements | `IA-TST-CLOSE-001..003`, `IA-TST-TRACE-001`, full mandatory Phase 13 set | all affected layers | all required suites + OpenAPI + event manifest + exact SHA |
| `Phase 14` | `IAREQ083`, `IAREQ104–IAREQ110`, `IAREQ130–IAREQ132`, `IAREQ139` | applicable `IA-TST-MIG-*`, `IA-TST-EVT-MIG-*`, generated-contract checks | persistence/compatibility/rollout | migration + OpenAPI/event contract evidence |
| `Phase 15` | `IAREQ086`, `IAREQ090`, `IAREQ093–IAREQ098`, `IAREQ123–IAREQ132`, `IAREQ136–IAREQ138` | `IA-TST-SEC-MASTER-*` plus affected CSRF/event/authz security tests | security-sensitive surfaces | security regression suites |
| `Phase 16` | `IAREQ111–IAREQ122`, `IAREQ127`, `IAREQ131`, `IAREQ135`, `IAREQ136` | `IA-TST-OBS-*`, `IA-TST-PERF-*`, `IA-TST-REL-*` | observability/reliability/performance | affected backend/frontend suites |
| `Phase 17` | `IAREQ068–IAREQ078`, `IAREQ087`, `IAREQ090`, `IAREQ126–IAREQ135`, `IAREQ136–IAREQ140` as applicable to the cross-team handoff | `IA-TST-X-*` | frontend + downstream bounded contexts | cross-team integration evidence |
| `Phase 18` | all applicable requirements under verification + `IAREQ140` | all mapped test IDs; `IA-TST-TRACE-001` | TESTS/CI handoff | non-zero full regression |
| `Phase 19` | `IAREQ083`, applicable `IAREQ126–IAREQ140` | generated-contract and traceability checks | canonical docs/generated contracts | OpenAPI + event manifest + docs/architecture validation |
| `Phase 20` | `IAREQ140`, `IAAC017`, `IAAC021` | `IA-TST-CLOSE-*`, full required suites | certification evidence | exact candidate SHA + CI + generated artifact checks |

## Traceability ownership rule

The table is intentionally requirement-centric rather than file-centric.

Physical source paths may move through accepted refactors, but a coding agent MUST NOT:

- remove a requirement mapping because a file moved;
- claim a work unit complete with only a subset of its mapped invariant families;
- broaden a work unit into an unrelated bounded-context business decision;
- auto-update this table to match an implementation that violated the SPEC.

`IA-TST-TRACE-001` validates that the Phase 13 work units and Phase 14–20 handoff remain represented by this normative mapping.

---

# Recommended PR/commit slicing

## 180. PR-IA-13A — CSRF contract + backend

Contains:

```text
ADR/contract decision
backend bootstrap
middleware/protector
backend API/Platform tests
```

Do not include event versioning.

## 181. PR-IA-13B — frontend CSRF transport

Contains:

```text
csrf provider
api-client unsafe path
refresh integration
frontend tests
```

May be a frontend-owned PR if repository team workflow requires it.

## 182. PR-IA-13C — Account authorization closure

Contains:

```text
role/action baseline
PermissionService central policy
handler bypass inventory/fixes
architecture gate
Application tests
```

## 183. PR-IA-13D — event inventory + payload safety

Contains:

```text
consumer maturity metadata
inventory drift gate
secret payload gate
PII classification
```

## 184. PR-IA-13E — event versioning + manifest

Contains:

```text
EventContractKey
version-aware catalog/registries
v1/v2 coexistence
contract manifest
schema drift gate
compatibility tests
```

## 185. PR-IA-13F — cross-cutting closure/certification docs

Only after code PRs are green:

```text
Phase 14–19 review output
workstream status
certification inputs
```

Avoid turning this into a miscellaneous refactor bucket.

---

# Coding-agent execution contract

## 186. Required report after every work unit

```text
Work unit:
SPEC requirements:
Baseline SHA:
Source inspected:
Current problem:
Decision used:
Files changed:
Why:
Domain impact:
Application impact:
Infrastructure impact:
Platform impact:
API impact:
Frontend impact:
Migration/compatibility:
Downstream consumers:
Tests added/updated:
Commands run:
Result:
Source debt discovered:
Architecture/product decision required:
Stop condition triggered:
```

## 187. No hidden decisions

If implementation needs a choice not already defined by SPEC/PLAN:

```text
STOP affected semantic change
→ record conflict
→ update canonical decision before coding the choice
```

Do not silently select the easiest option.

## 188. Preserve valid completed work

Phase 13 closure is not permission to rewrite:

```text
IA-API-002 error taxonomy
IA-AUTHZ-001 resource/action inventory
IA-AUTHZ-002 self-service semantics
IA-API-004 OpenAPI workflow
```

unless a closure test proves a concrete defect.

---

# Stop-condition registry — Phase 13+

## 189. IA-PLAN-STOP-016 — CSRF frontend/backend topology unresolved

If the supported browser/API topology contradicts the bootstrap/cookie policy:

```text
STOP enablement
→ architecture decision
```

## 190. IA-PLAN-STOP-017 — Account admin product policy contradicts baseline

Do not invent a new role matrix.

Update SPEC/PLAN/TESTS only after canonical Governance/product decision.

## 191. IA-PLAN-STOP-018 — authorization exception cannot be classified

If a handler role/permission check might be either current-actor authorization or a business invariant and source semantics are unclear:

```text
STOP removal
→ identify invariant owner
```

Do not delete a valid last-owner/transfer invariant merely to satisfy the gate.

## 192. IA-PLAN-STOP-019 — event name/version unavailable in runtime envelope

If production deserialization cannot access event version:

```text
STOP EventContractKey rollout
→ inspect envelope/transport contract
→ migrate envelope/version propagation first
```

Do not synthesize version `1` silently for all messages as final architecture.

## 193. IA-PLAN-STOP-020 — serializer contract unknown

If manifest generation cannot determine the actual production serialized property names/types:

```text
STOP schema fingerprint implementation
→ bind manifest builder to production serializer settings
```

Do not fingerprint an inaccurate CLR-only model.

## 194. IA-PLAN-STOP-021 — real consumer requires unclassified PII

Do not remove or retain by preference.

Classify consumer purpose/retention first.

## 195. IA-PLAN-STOP-022 — CI gate weakened

Do not exclude new closure files/types from architecture/contract tests merely to make CI pass unless they are genuinely outside the bounded contract and the exclusion is canonically justified.

---

# Required source searches — Phase 13+

## 196. CSRF search

Use repository-native equivalent:

```bash
rg -n "csrf_token|X-CSRF-Token|XSRF-TOKEN|X-XSRF-TOKEN|getCsrfToken|Csrf" \
  backend frontend
```

After closure, all remaining legacy XSRF hits must be classified as test fixture/history/stale docs or removed.

## 197. Authorization bypass search

```bash
rg -n \
  "IPermissionService|IPermissionEvaluator|IWorkspacePermissionService|IAuthorizationDecisionStore|EnsureAllowedAsync|AuthorizeAsync|HasPermissionAsync|AccountRole\\.|WorkspaceRole\\.|\\.Role" \
  backend/src/Notrelix.Application backend/src/Notrelix.API
```

Review semantically; do not bulk-delete hits.

## 198. Event registry search

```bash
rg -n \
  "IIntegrationEvent|EventName|Version|IntegrationEventCatalog|ContractRegistry|ConsumerRegistry|IConsumer<|Resolve\\(|TryResolve\\(" \
  backend/src backend/tests
```

## 199. Event sensitive payload search

```bash
rg -n \
  "Password|PasswordHash|AccessToken|RefreshToken|ApiKey|ClientSecret|PrivateKey|Mfa|Totp|RecoveryCode|Email" \
  backend/src/Notrelix.Domain/Identity \
  backend/src/Notrelix.Domain/Accounts \
  backend/src/Notrelix.Infrastructure/Messaging
```

Every hit is evidence to classify, not automatic proof of a violation.

---

# Required commands — certification class

## 200. Backend build

Use the repository canonical build command.

If still canonical:

```bash
dotnet build backend/backend.slnx
```

## 201. Backend test families

Run repository/CI canonical commands for:

```text
Architecture
Domain
Application
Infrastructure
Platform
API
Integration
```

Do not reuse pre-closure test counts as final evidence.

## 202. Frontend CSRF tests

Run the canonical frontend workspace/package tests that cover:

```text
foundation/contracts client
api-client
session refresh
CSRF bootstrap/unsafe transport
```

Use the repo's package-manager/workspace commands discovered from source.

## 203. Generated contracts

Run canonical OpenAPI generation/check.

Run the event manifest architecture/contract check introduced by P13-EVT-003C.

## 204. Docs

When docs tooling is canonical:

```bash
make docs-generate
make docs-check
```

Do not suppress metadata/drift errors to close the phase.

---

# Phase-level acceptance — revised

## 205. Phase 13 accepted when

Every source-level API/authz/event work unit is DONE and P13-FINAL-01 passes.

Operational event evidence may be `NOT_APPLICABLE_UNTIL_DEPLOYMENT` only as defined.

## 206. Phase 14 accepted when

All persistence and public-contract compatibility impacts are classified and migration/rollout paths are executable.

## 207. Phase 15 accepted when

CSRF/authz/event payload and prior Identity security matrices pass without security downgrades.

## 208. Phase 16 accepted when

Closure failure modes are diagnosable and retry/cache/authz paths avoid duplicate/unbounded work.

## 209. Phase 17 accepted when

Frontend and real/stub downstream consumer states are explicit and no consumer depends on old ambiguous contracts.

## 210. Phase 18 accepted when

TESTS contains complete traceability and implemented tests exist for material closure requirements.

## 211. Phase 19 accepted when

Canonical docs/ADRs/OpenAPI/event contract artifacts match candidate implementation.

## 212. Phase 20 accepted when

Certification can prove the exact SHA with no unresolved material Phase 13–19 gap.

---

# Definition of Done — Phase 13+

## 213. API/CSRF DoD

- one `csrf_token` / `X-CSRF-Token` protocol;
- explicit bootstrap under existing Auth group;
- no API-cookie JavaScript-read dependency;
- refresh uses CSRF-aware transport;
- non-browser explicit credentials are not forced into browser CSRF;
- canonical ProblemDetails on failure;
- feature enablement occurs only after cross-stack evidence;
- OpenAPI reflects public bootstrap contract.

## 214. Authorization DoD

- current Account role/action baseline encoded centrally;
- `Admin` can perform current approved Account admin baseline action(s) without handler hacks;
- explicit Governance deny/allow semantics preserved;
- protected handlers do not re-authorize via permission services;
- all role checks classified;
- exact architecture gate prevents regression.

## 215. Event DoD

- Identity/Account events source-inventoried;
- consumer maturity IMPLEMENTED/STUB/NONE explicit;
- prohibited secrets blocked;
- PII intentional/justified;
- runtime contract key is `(Name, Version)`;
- v1/v2 coexistence tested;
- canonical event manifest checked in;
- schema drift requires version bump;
- migration protocol documented/executable;
- operational evidence separated from source closure.

## 216. Compatibility/security/reliability DoD

- no unexplained EF migration;
- name-only event callers eliminated;
- CSRF deploy order safe;
- negative security matrices green;
- unsupported event versions fail deterministically;
- no retry loops/request storms;
- no duplicate pipeline+handler authorization query.

## 217. Documentation/certification DoD

- old CSRF ADR assumptions superseded;
- frontend ADR matches backend protocol;
- security/events architecture docs reflect closure;
- OpenAPI and event manifest drift clean;
- exact candidate SHA recorded;
- all required backend/frontend suites green and non-zero;
- no material `OPEN`, `PARTIAL`, `DEFERRED`, `TODO` or hidden follow-up remains in Phase 13 source work.

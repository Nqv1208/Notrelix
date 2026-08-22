---
document_id: WRK-TESTS-IDENTITY-ACCOUNTS
document_type: workstream-tests
status: active
owner: identity-accounts-team
applies_to:
  - backend
  - identity
  - accounts
  - actor
  - users
  - profiles
  - authentication
  - sessions
  - registration
  - credentials
  - oauth
  - sso
  - mfa
  - security
  - api-tokens
  - account-context
  - tenant-isolation
  - migrations
  - cross-context-contracts
evidence:
  - docs/workstreams/execution/identity-accounts/identity-accounts.spec.md
  - docs/workstreams/execution/identity-accounts/identity-accounts.plan.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/teams/identity-accounts.md
  - docs/workstreams/teams/platform-foundation.md
  - backend/docs/architecture/domain-modeling.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/api-and-contracts.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/testing-and-quality-gates.md
review_on:
  - identity-spec-change
  - identity-plan-change
  - account-ownership-resolution
  - authentication-contract-change
  - session-contract-change
  - oauth-sso-change
  - mfa-change
  - api-token-change
  - tenant-isolation-change
  - migration-change
  - ci-gate-change
---

# TESTS — Identity & Accounts

## 1. Purpose

This document is the canonical verification plan for the Identity & Accounts execution package.

It answers:

```text
Which requirement is verified?
At which test layer?
In which test project/suite?
With which positive case?
With which negative case?
With which concurrency/security/migration case?
What result proves the requirement?
Which CI gate must execute it?
```

This document does not define implementation.

Implementation belongs to:

```text
identity-accounts.plan.md
```

Target semantics belong to:

```text
identity-accounts.spec.md
```

Certification evidence belongs to:

```text
identity-accounts.certification.md
```

## 2. Verification principle

Every material requirement MUST have verification evidence.

The required chain is:

```text
SPEC requirement
        ↓
PLAN work unit
        ↓
TEST scenario
        ↓
test project/suite
        ↓
CI gate
        ↓
certification evidence
```

A requirement without a verification strategy is incomplete.

A test without a requirement or regression rationale is suspect.

## 3. Test authority rule

Tests verify accepted architecture and product behavior.

Tests MUST NOT become a hidden source of new business semantics.

If existing tests contradict the canonical SPEC:

```text
do not preserve the test blindly
→ classify whether source/test is stale
→ fix the stale side deliberately
```

Do not weaken a valid architecture/security test merely because implementation fails.

## 4. Existing backend test topology

The current backend test architecture is expected to include the canonical families already used by the repository:

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

Exact project names and paths MUST be discovered from the repository before implementation.

This TESTS document does not authorize a new test project for Identity & Accounts unless the existing architecture genuinely cannot express required coverage and architecture authority approves the addition.

## 5. Test-level model

Identity & Accounts uses the following verification layers.

### T0 — Static / compile / source guards

Used for:

- no forbidden dependencies;
- generated contract drift;
- analyzers;
- nullable/type safety;
- secret-pattern guards where repository supports them.

### T1 — Domain tests

Used for:

- aggregate invariants;
- state transitions;
- value-object validation;
- Domain event production.

### T2 — Application tests

Used for:

- command/query orchestration;
- validation;
- authorization declarations;
- context requirements;
- error/result mapping;
- idempotency where applicable.

### T3 — Infrastructure tests

Used for:

- persistence mappings;
- indexes/constraints;
- hashing/protection adapters;
- provider adapters;
- cache/session/token persistence;
- migration behavior.

### T4 — API tests

Used for:

- endpoint contract;
- authentication/authorization;
- CSRF;
- HTTP errors;
- OpenAPI;
- secret response minimization.

### T5 — Architecture tests

Used for:

- Domain purity;
- layer direction;
- bounded-context isolation;
- authorization-pipeline ownership;
- forbidden private dependencies.

### T6 — Integration tests

Used for:

- real production DI graph;
- DB behavior;
- tenant isolation;
- authentication/session runtime;
- provider boundary;
- cross-context producer/consumer contracts.

### T7 — Security tests

Used for:

- replay;
- enumeration resistance;
- secret non-exposure;
- tenant spoofing;
- revocation;
- collision/takeover cases;
- abuse controls.

### T8 — Concurrency tests

Used for:

- identity uniqueness;
- OAuth linking;
- Account bootstrap;
- session revocation;
- API-token revoke/create races.

### T9 — Migration tests

Used for:

- clean DB;
- upgrade DB;
- backfill;
- ID/ownership migration;
- OAuth/MFA/token storage changes.

### T10 — Performance / reliability tests

Used only where the capability is a hot path or has failure-mode risk:

- session/actor resolution;
- Account context;
- provider outage;
- cache invalidation;
- revocation latency.

### T11 — Cross-context contract tests

Used for:

- Workspace/Governance;
- Billing;
- WorkManagement;
- Documents/Collaboration;
- Automation;
- Analytics.

## 6. Test naming convention

Test IDs use:

```text
IA-TST-<AREA>-<LAYER>-<NNN>
```

Examples:

```text
IA-TST-USER-DOM-001
IA-TST-SESSION-APP-004
IA-TST-ACCOUNT-INT-003
IA-TST-OAUTH-SEC-006
IA-TST-TOKEN-CONC-002
IA-TST-MIG-ACCOUNT-004
```

These are execution/test IDs, not governance rule IDs.

## 7. Scenario naming convention

Each scenario should contain:

```text
Given
When
Then
Requirement IDs
Expected result
```

Use business language in test names where possible.

Avoid test names that only describe implementation methods.

## 8. Required test metadata in implementation notes

For each new/updated critical test, record:

```text
Test ID:
Requirement IDs:
Test project:
Source under test:
Positive/negative:
Security-sensitive:
Migration-sensitive:
CI gate:
```

This may live in PR evidence rather than source comments.

# Traceability master map

## 9. Requirement family mapping

| Requirement range | Capability | Primary test layers |
|---|---|---|
| IAREQ001–IAREQ005 | User identity/lifecycle | Domain, Infrastructure, Integration |
| IAREQ006–IAREQ009 | Actor | Application, Architecture, Integration |
| IAREQ010–IAREQ011 | Profile | Domain/Application/API |
| IAREQ012–IAREQ015 | Authentication | Application/API/Security |
| IAREQ016–IAREQ023 | Session | Domain/Application/Infrastructure/API/Integration |
| IAREQ024–IAREQ030 | Account | Domain/Application/Infrastructure/Integration |
| IAREQ031–IAREQ036 | Account context/tenant | Application/API/Integration/Security |
| IAREQ037–IAREQ039 | Consumer contract | Architecture/Integration |
| IAREQ040–IAREQ047 | OAuth | Application/Infrastructure/API/Security/Integration |
| IAREQ048–IAREQ054 | MFA | Domain/Application/Infrastructure/API/Security |
| IAREQ055–IAREQ058 | Security settings | Application/API/Security/Audit |
| IAREQ059–IAREQ067 | API tokens | Domain/Application/Infrastructure/API/Security |
| IAREQ068–IAREQ073 | Cross-team contracts | Architecture/Integration |
| IAREQ074–IAREQ078 | Data ownership | Architecture/Infrastructure |
| IAREQ079–IAREQ092 | API/events/authz | API/Application/Architecture/Integration |
| IAREQ093–IAREQ103 | Security/concurrency | Security/Concurrency/Integration |
| IAREQ104–IAREQ110 | Migration | Migration/Infrastructure/Integration |
| IAREQ111–IAREQ118 | Observability/performance | Integration/Performance/Source review |
| IAREQ119–IAREQ122 | Reliability/recovery | Integration/Infrastructure |
| IAREQ123–IAREQ125 | Privacy/data minimization | API/Security/Architecture |

# User identity tests

## 10. IA-TST-USER-DOM-001 — stable canonical User ID

Requirements:

```text
IAREQ001
```

Given a User created through a supported authentication/registration path  
When the User authenticates through another supported mechanism  
Then the canonical User ID remains the same.

Primary layer:

```text
Domain + Application + Integration
```

Failure prevented:

```text
parallel user stores per auth mechanism
```

## 11. IA-TST-USER-DOM-002 — mutable email does not change User identity

Requirements:

```text
IAREQ001
```

Given an existing User with a mutable email identifier  
When email/profile identity changes according to policy  
Then the canonical User ID remains unchanged.

## 12. IA-TST-USER-INF-001 — supported unique identifier enforced under concurrency

Requirements:

```text
IAREQ002
IAREQ099
```

Given two concurrent create/link operations for the same unique canonical login identifier  
When both reach persistence  
Then at most one canonical mapping succeeds and the other receives a deterministic conflict.

Test must use the actual database/unique constraint where correctness depends on DB concurrency.

## 13. IA-TST-USER-DOM-003 — valid User lifecycle transition

Requirements:

```text
IAREQ003
```

Test every canonical lifecycle transition defined in source/product authority.

Do NOT invent states merely to satisfy test completeness.

## 14. IA-TST-USER-DOM-004 — invalid User lifecycle transition rejected

Requirements:

```text
IAREQ003
```

Given a User in state X  
When an invalid transition Y is requested  
Then Domain/Application rejects without persistence side effect.

## 15. IA-TST-USER-INT-001 — deactivation blocks future protected authentication/session use

Requirements:

```text
IAREQ004
IAREQ098
```

Given an active User with valid security state  
When User is disabled/deactivated  
Then protected authentication/session behavior follows canonical revocation policy.

## 16. IA-TST-USER-INT-002 — historical downstream references remain valid

Requirements:

```text
IAREQ005
```

Given downstream historical references to a User  
When User is disabled/deleted according to policy  
Then stable identity references remain resolvable/tombstoned as designed and business history is not silently destroyed.

Cross-context fixture may use one representative consumer rather than every context if contract is shared.

# Actor tests

## 17. IA-TST-ACTOR-ARCH-001 — Domain has no HTTP actor dependency

Requirements:

```text
IAREQ006
```

Architecture test ensures Domain does not reference:

- HttpContext;
- API principal implementation;
- web authentication abstractions.

## 18. IA-TST-ACTOR-APP-001 — Application receives trusted Actor abstraction

Requirements:

```text
IAREQ006
IAREQ009
```

Given a protected Application operation  
When executed under authenticated runtime  
Then Actor comes from the approved abstraction and not from user-supplied payload.

Negative case:

```text
payload.ActorId != authenticated actor
→ payload must not override runtime actor
```

## 19. IA-TST-ACTOR-APP-002 — supported principal type mapping

Requirements:

```text
IAREQ007
IAREQ008
```

For every currently supported principal type:

- browser session;
- API token;
- system/background principal if approved;

verify canonical Actor mapping.

Do not add tests for principal types not supported by source.

## 20. IA-TST-ACTOR-SEC-001 — client cannot impersonate arbitrary Actor

Requirements:

```text
IAREQ009
```

Given authenticated User A  
When request contains User B/Actor B ID in a client-controlled field not semantically intended for delegation  
Then effective Actor remains A and authorization does not use B.

# Profile tests

## 21. IA-TST-PROFILE-DOM-001 — profile field ownership

Requirements:

```text
IAREQ010
```

Verify Identity profile can mutate only fields owned by Identity.

Workspace-member-specific fields MUST NOT be accepted through generic Identity profile mutation.

## 22. IA-TST-PROFILE-APP-001 — self profile update

Requirements:

```text
IAREQ011
```

Given authenticated User  
When updating allowed self-service profile fields  
Then update succeeds.

## 23. IA-TST-PROFILE-APP-002 — forbidden sensitive profile mutation

Requirements:

```text
IAREQ011
IAREQ056
```

Attempt to change security-sensitive identity fields through generic profile update.

Expected:

```text
rejected
```

# Authentication tests

## 24. IA-TST-AUTH-APP-001 — supported auth mechanism resolves canonical User

Requirements:

```text
IAREQ012
```

For every enabled auth mechanism:

```text
credential/auth source
→ canonical User
```

No duplicate User record is created for an already linked identity.

## 25. IA-TST-AUTH-APP-002 — successful authentication produces canonical auth result

Requirements:

```text
IAREQ013
```

Verify Application result contains/causes the expected canonical identity/session semantics and delegates transport to API/Platform.

## 26. IA-TST-AUTH-API-001 — invalid credential failure does not expose sensitive detail

Requirements:

```text
IAREQ014
IAREQ095
```

Compare invalid identifier vs invalid secret responses according to canonical anti-enumeration policy.

Expected:

- no password validity leak;
- no MFA secret state leak;
- no provider token material.

## 27. IA-TST-AUTH-INF-001 — stored credentials are protected

Requirements:

```text
IAREQ015
```

Persistence test verifies raw supported credential secret is not stored directly where architecture expects hashing/protection.

Do not assert implementation-specific hash algorithm unless canonical security authority requires it.

# Session tests

## 28. IA-TST-SESSION-DOM-001 — create valid Session

Requirements:

```text
IAREQ016
IAREQ017
```

Given valid authenticated User  
When a Session is created  
Then required lifecycle fields/invariants are valid.

## 29. IA-TST-SESSION-DOM-002 — invalid Session state rejected

Requirements:

```text
IAREQ016
IAREQ017
```

Examples based on source semantics:

- expiry before issue time;
- revoked session reactivation;
- invalid owner.

Only assert real invariants.

## 30. IA-TST-SESSION-APP-001 — current session/bootstrap query

Requirements:

```text
IAREQ018
```

Given authenticated session  
When current identity/bootstrap operation runs  
Then it returns canonical identity/session information without requiring feature-specific probing.

## 31. IA-TST-SESSION-INT-001 — expired Session rejected

Requirements:

```text
IAREQ019
```

Given expired authoritative Session  
When a protected request is executed  
Then authentication fails as session-expired/unauthenticated according to API contract.

## 32. IA-TST-SESSION-API-001 — expired vs forbidden distinction

Requirements:

```text
IAREQ019
IAREQ080
```

Given an expired session and separately a valid session lacking permission  
Then API behavior distinguishes authentication failure from authorization denial as policy defines.

## 33. IA-TST-SESSION-APP-002 — logout current session

Requirements:

```text
IAREQ020
```

Given Session S1  
When logout-current executes  
Then S1 is revoked and cannot authorize later protected operations.

## 34. IA-TST-SESSION-APP-003 — revoke all sessions

Requirements:

```text
IAREQ020
IAREQ021
```

Only if operation is product-supported.

Given S1/S2/S3 for one User  
When revoke-all executes  
Then all targeted sessions become invalid according to policy.

## 35. IA-TST-SESSION-INT-002 — revocation survives cache

Requirements:

```text
IAREQ021
IAREQ117
```

Given session validity cached as valid  
When authoritative session is revoked  
Then subsequent request becomes invalid within accepted security window.

The test must exercise actual cache mechanism if production uses one.

## 36. IA-TST-SESSION-CONC-001 — revoke while request/refresh races

Requirements:

```text
IAREQ022
IAREQ102
```

Given Session valid  
When revoke and refresh/validation race  
Then final security state obeys canonical revocation semantics.

## 37. IA-TST-SESSION-INT-003 — User disable affects sessions correctly

Requirements:

```text
IAREQ004
IAREQ023
IAREQ098
```

Given active sessions  
When User is disabled  
Then session behavior matches canonical policy.

## 38. IA-TST-SESSION-API-002 — sensitive session metadata minimized

Requirements:

```text
IAREQ023
IAREQ081
```

Session-list/current-session APIs must not expose:

- raw secrets;
- full auth headers;
- unsafe device fingerprints;
- internal security material.

# Account ownership resolution tests

## 39. IA-TST-ACCOUNT-ARCH-001 — one canonical Account owner

Requirements:

```text
IAREQ024
IAREQ025
IAREQ075
IAREQ076
IAREQ104
```

After Phase 1 ownership decision, architecture/source test or review gate must prove there is one canonical business Account model.

This may be structural architecture test when expressible.

It MUST fail if a second canonical Account aggregate/table is introduced.

## 40. IA-TST-ACCOUNT-DOM-001 — Account ID stability

Requirements:

```text
IAREQ025
```

Given Account created  
When metadata/lifecycle changes  
Then Account ID remains stable.

## 41. IA-TST-ACCOUNT-DOM-002 — valid Account lifecycle

Requirements:

```text
IAREQ026
```

Verify every supported transition.

## 42. IA-TST-ACCOUNT-DOM-003 — invalid Account lifecycle rejected

Requirements:

```text
IAREQ026
```

Verify invalid transitions do not persist.

## 43. IA-TST-ACCOUNT-APP-001 — bootstrap administrator semantics

Requirements:

```text
IAREQ027
IAREQ091
```

Given creation flow requiring initial admin/owner  
When Account is created  
Then bootstrap authority is established exactly as product/Governance contract requires.

Negative:

```text
bootstrap path must not become permanent authorization bypass
```

## 44. IA-TST-ACCOUNT-APP-002 — User/Account relationship semantics

Requirements:

```text
IAREQ028
```

Test canonical relationship only after Phase 1 resolution.

Do not test guessed membership behavior.

## 45. IA-TST-ACCOUNT-INT-001 — Billing references stable Account

Requirements:

```text
IAREQ029
IAREQ069
```

Representative contract test confirms Billing can bind the canonical Account ID without private Account persistence dependency.

## 46. IA-TST-ACCOUNT-INT-002 — Account lifecycle does not silently cascade private context data

Requirements:

```text
IAREQ030
```

Given Account lifecycle transition  
Then downstream effects follow explicit contract/event/orchestration rather than accidental DB cascade.

Test may inspect representative DB behavior plus emitted contract.

# Current Account / tenant-isolation tests

## 47. IA-TST-CTX-APP-001 — current Account resolution

Requirements:

```text
IAREQ031
IAREQ032
```

Given an operation requiring Account scope  
When executed under valid context  
Then approved resolver yields the expected Account.

## 48. IA-TST-CTX-SEC-001 — Account ID does not grant authorization

Requirements:

```text
IAREQ033
```

Given Actor A knows Account B ID  
When requesting protected B resource without permission  
Then request is denied.

## 49. IA-TST-CTX-INT-001 — Account A cannot read Account B state

Requirements:

```text
IAREQ034
```

Critical tenant-isolation scenario.

Must exercise real production-like persistence and authorization path.

## 50. IA-TST-CTX-INT-002 — Account A cannot mutate Account B state

Requirements:

```text
IAREQ034
```

Must verify no DB side effect.

## 51. IA-TST-CTX-SEC-002 — API token cannot escape Account scope

Requirements:

```text
IAREQ034
IAREQ063
```

Given token scoped to Account A  
When used against Account B  
Then rejected.

## 52. IA-TST-CTX-INT-003 — background job requires Account context

Requirements:

```text
IAREQ035
```

Given Account-scoped background work with missing/invalid Account context  
Then work fails safely instead of running globally.

## 53. IA-TST-CTX-SEC-003 — null/default Account does not become global access

Requirements:

```text
IAREQ035
```

Explicit negative regression test where architecture supports such context injection.

## 54. IA-TST-CTX-APP-002 — Account switch semantic validation

Requirements:

```text
IAREQ036
```

Backend contract verifies Actor may switch/select only Accounts they can access according to canonical ownership/membership semantics.

Frontend cache behavior belongs to frontend tests, but backend authorization must be independent.

# Consumer-contract tests

## 55. IA-TST-X-ARCH-001 — downstream does not reference Identity EF entities

Requirements:

```text
IAREQ037
IAREQ038
IAREQ074
```

Architecture test should prohibit cross-context dependencies on Identity persistence entity/configuration types where the current architecture test framework can express it.

## 56. IA-TST-X-INT-001 — Workspace consumes stable User/Actor/Account contract

Requirements:

```text
IAREQ037
IAREQ068
```

Representative integration test:

```text
Identity User/Actor + Account
→ Workspace/Governance consumer
```

without private-table dependency.

## 57. IA-TST-X-INT-002 — mutable Profile does not corrupt downstream identity

Requirements:

```text
IAREQ039
```

Given downstream consumer references User  
When display name/avatar changes  
Then downstream identity remains valid.

## 58. IA-TST-X-INT-003 — Billing consumes Account lifecycle fact/contract

Requirements:

```text
IAREQ069
```

No credential/session internals exposed.

## 59. IA-TST-X-ARCH-002 — WorkManagement has no Identity private-table dependency

Requirements:

```text
IAREQ070
```

Architecture/source guard.

## 60. IA-TST-X-INT-004 — Documents/Collaboration historical author survives identity lifecycle

Requirements:

```text
IAREQ071
```

Representative historical attribution test.

## 61. IA-TST-X-APP-001 — approved background/system actor only

Requirements:

```text
IAREQ072
```

If system/background actor is supported, verify only approved actor type can execute system path.

If not supported, no test should invent it.

## 62. IA-TST-X-SEC-001 — Analytics contract excludes sensitive identity data

Requirements:

```text
IAREQ073
```

Verify approved event/report contract does not expose secret/security fields.

# OAuth tests

## 63. IA-TST-OAUTH-APP-001 — OAuth start creates protected state

Requirements:

```text
IAREQ040
IAREQ041
```

Verify:

- provider recognized;
- state generated;
- expiry set;
- PKCE/nonce where canonical;
- safe callback/return target.

## 64. IA-TST-OAUTH-SEC-001 — invalid state rejected

Requirements:

```text
IAREQ041
IAREQ042
IAREQ096
```

Cases:

- missing state;
- wrong state;
- expired state;
- state bound to another flow if binding exists.

## 65. IA-TST-OAUTH-SEC-002 — callback replay rejected/idempotently safe

Requirements:

```text
IAREQ042
IAREQ044
IAREQ096
```

Replay same callback/code/state.

Expected according to provider/architecture:

- rejected;
- or no duplicate link/session side effect.

## 66. IA-TST-OAUTH-INT-001 — provider subject maps to canonical User

Requirements:

```text
IAREQ040
IAREQ043
```

Given existing provider link  
When login callback succeeds  
Then same canonical User is resolved.

## 67. IA-TST-OAUTH-APP-002 — link flow requires authenticated User

Requirements:

```text
IAREQ043
```

Unauthenticated link attempt rejected.

## 68. IA-TST-OAUTH-SEC-003 — provider identity linked to another User

Requirements:

```text
IAREQ044
```

Expected:

```text
conflict/rejected according to policy
```

Never silent reassignment.

## 69. IA-TST-OAUTH-SEC-004 — same email, different provider subject

Requirements:

```text
IAREQ044
```

Behavior must exactly match approved security/product policy.

If policy absent, implementation must remain blocked rather than invent expected result.

## 70. IA-TST-OAUTH-CONC-001 — concurrent linking same provider subject

Requirements:

```text
IAREQ044
IAREQ100
```

Two Users race to link same provider subject.

Expected:

- one canonical owner;
- other conflict;
- no duplicate mapping.

## 71. IA-TST-OAUTH-APP-003 — unlink preserves viable authentication policy

Requirements:

```text
IAREQ045
```

If policy requires at least one method, removing last method is rejected.

## 72. IA-TST-OAUTH-INF-001 — provider tokens protected at rest

Requirements:

```text
IAREQ046
```

Where provider tokens are persisted, verify protected storage contract.

Do not assert ciphertext implementation details beyond architecture.

## 73. IA-TST-OAUTH-SEC-005 — provider token absent from logs/events

Requirements:

```text
IAREQ046
IAREQ094
```

Use captured logs/event payload assertions where test infrastructure allows.

## 74. IA-TST-OAUTH-API-001 — provider failure normalized

Requirements:

```text
IAREQ047
```

Provider-specific failure maps to stable API/Application error without secret/raw response leakage.

# SSO tests

## 75. IA-TST-SSO-ARCH-001 — SSO semantic classification

This is a plan/architecture evidence test rather than runtime behavior.

Before runtime SSO tests exist, execution must classify whether SSO is:

- OIDC;
- SAML;
- enterprise auth abstraction;
- placeholder.

Do not create false runtime expectations.

## 76. IA-TST-SSO-SEC-001 — IdP assertion does not auto-grant unrelated Account

Requirements derive from:

```text
IAREQ024
IAREQ033
IAREQ043
```

If enterprise SSO is implemented:

Given identity assertion for User  
When Account access/membership is not valid  
Then authentication does not automatically grant Account authorization.

## 77. IA-TST-SSO-SEC-002 — replay/signature validation

Only if protocol requires it.

Verify canonical SSO replay/signature/issuer/audience checks through provider adapter or protocol library integration.

# MFA tests

## 78. IA-TST-MFA-DOM-001 — enrollment pending vs active state

Requirements:

```text
IAREQ048
IAREQ049
```

Creating enrollment MUST NOT make method active before required verification.

## 79. IA-TST-MFA-APP-001 — successful enrollment verification

Requirements:

```text
IAREQ049
```

Given pending enrollment  
When valid verification occurs  
Then method becomes active once.

## 80. IA-TST-MFA-SEC-001 — invalid enrollment code rejected

Requirements:

```text
IAREQ049
IAREQ053
```

No partial activation.

## 81. IA-TST-MFA-APP-002 — challenge succeeds only with eligible active method

Requirements:

```text
IAREQ050
```

## 82. IA-TST-MFA-SEC-002 — challenge replay rejected

Requirements:

```text
IAREQ050
IAREQ096
```

## 83. IA-TST-MFA-SEC-003 — challenge expiry enforced

Requirements:

```text
IAREQ050
```

## 84. IA-TST-MFA-SEC-004 — repeated invalid challenge follows abuse control policy

Requirements:

```text
IAREQ050
IAREQ097
```

Test integration with generic Platform mechanism if current architecture exposes it.

## 85. IA-TST-MFA-APP-003 — recovery uses approved mechanism only

Requirements:

```text
IAREQ051
```

Test every approved recovery path.

Do not create a "fallback" test for unsupported weak recovery.

## 86. IA-TST-MFA-SEC-005 — knowledge of email is insufficient to disable MFA

Requirements:

```text
IAREQ051
```

Critical negative test if recovery API exists.

## 87. IA-TST-MFA-APP-004 — disable/reset requires proof/authorization

Requirements:

```text
IAREQ052
```

Test self-service and admin paths separately if both exist.

## 88. IA-TST-MFA-SEC-006 — MFA secrets absent from ordinary read API

Requirements:

```text
IAREQ053
IAREQ081
IAREQ094
```

## 89. IA-TST-MFA-INT-001 — MFA state change session impact

Requirements:

```text
IAREQ054
IAREQ098
```

For each security action whose policy affects sessions:

- enable;
- disable;
- reset;
- recovery;

verify session behavior.

# Security settings tests

## 90. IA-TST-SEC-APP-001 — User security setting cannot modify Workspace policy

Requirements:

```text
IAREQ055
```

Reject/make impossible cross-boundary mutation.

## 91. IA-TST-SEC-APP-002 — sensitive operation requires stronger verification where configured

Requirements:

```text
IAREQ056
```

Test only product-approved operations.

## 92. IA-TST-SEC-INT-001 — security event emitted after authoritative change

Requirements:

```text
IAREQ057
```

Given security-sensitive mutation succeeds  
Then approved audit/security fact is emitted/recorded.

## 93. IA-TST-SEC-SEC-001 — security event contains no secret material

Requirements:

```text
IAREQ058
IAREQ086
IAREQ094
```

# API token tests

## 94. IA-TST-TOKEN-DOM-001 — token metadata lifecycle

Requirements:

```text
IAREQ059
IAREQ062
```

Test create/revoke/expire state according to canonical Domain model.

## 95. IA-TST-TOKEN-SEC-001 — raw token shown only according to issuance contract

Requirements:

```text
IAREQ060
```

Expected typically:

```text
create response may contain raw secret once
list/read responses do not
```

Use exact source/product policy.

## 96. IA-TST-TOKEN-INF-001 — raw token not persisted

Requirements:

```text
IAREQ060
IAREQ061
```

Verify persisted verification material is protected/non-reversible where architecture requires hashing.

## 97. IA-TST-TOKEN-APP-001 — valid token resolves approved principal

Requirements:

```text
IAREQ059
IAREQ061
```

## 98. IA-TST-TOKEN-SEC-002 — invalid token does not reveal token existence

Requirements:

```text
IAREQ061
IAREQ095
```

## 99. IA-TST-TOKEN-SEC-003 — token Account scope enforced

Requirements:

```text
IAREQ063
```

Given token for Account A  
Then access to Account B is rejected.

## 100. IA-TST-TOKEN-APP-002 — token still passes Governance authorization

Requirements:

```text
IAREQ064
```

Authenticated token lacking required Governance permission is denied.

## 101. IA-TST-TOKEN-APP-003 — token scope intersection

Requirements:

```text
IAREQ065
```

Only if token scopes exist.

Verify effective permission follows canonical intersection model.

Do not invent expected behavior if unresolved.

## 102. IA-TST-TOKEN-INT-001 — revoked token becomes invalid

Requirements:

```text
IAREQ066
```

Exercise cache if present.

## 103. IA-TST-TOKEN-CONC-001 — revoke vs use race

Requirements:

```text
IAREQ066
IAREQ103
```

Final authoritative revocation must win according to accepted security window.

## 104. IA-TST-TOKEN-SEC-004 — token audit/log does not expose raw secret

Requirements:

```text
IAREQ067
IAREQ094
```

# Data ownership / architecture tests

## 105. IA-TST-OWN-ARCH-001 — Identity persistence private

Requirements:

```text
IAREQ074
```

No unrelated Domain/Application context may reference Identity persistence implementation types.

## 106. IA-TST-OWN-ARCH-002 — Account persistence has one owner

Requirements:

```text
IAREQ075
IAREQ076
```

After Account ownership resolution, enforce structurally where practical.

## 107. IA-TST-OWN-ARCH-003 — external OAuth mapping remains Identity-owned

Requirements:

```text
IAREQ077
```

Integrations must not own login identity mapping.

## 108. IA-TST-OWN-ARCH-004 — Session business state not moved to Platform

Requirements:

```text
IAREQ078
```

Platform may provide mechanism, but canonical Session entity/state must remain Identity-owned.

# API contract tests

## 109. Phase 13+ verification rule

From Phase 13 onward, source review alone is not sufficient when the invariant can be made executable.

The required chain remains:

```text
SPEC requirement
→ PLAN work unit
→ TEST ID
→ actual test/source gate
→ CI job
→ candidate-SHA certification evidence
```

The audited closure baseline is `develop @ 4efd37bdff79f93f97059586928aa94af67ba8b1`.

Tests that prove already completed work (`IA-API-002`, `IA-AUTHZ-001`, `IA-AUTHZ-002`, `IA-API-004`) remain regression requirements while the newly reopened work is closed.

## 110. IA-TST-API-CONTRACT-001 — endpoint-to-use-case mapping

Requirements:

```text
IAREQ079
```

Each Identity/Account endpoint maps to an approved Application use case without endpoint-local business rules.

Architecture/source gate is preferred when current API organization makes this deterministic.

## 111. IA-TST-API-CONTRACT-002 — authentication vs authorization status mapping

Requirements:

```text
IAREQ080
```

Representative matrix:

```text
no/invalid auth              → authentication failure
expired/revoked session      → authentication/session failure
valid auth but forbidden     → authorization failure
not-found privacy case       → canonical privacy mapping
```

The test must use the canonical ProblemDetails/error pipeline.

## 112. IA-TST-API-SEC-001 — sensitive fields absent from response DTOs

Requirements:

```text
IAREQ081
```

Check representative Identity/Account response DTOs/endpoints for prohibited secret/internal fields.

## 113. IA-TST-API-CONTRACT-003 — stable typed/system IDs

Requirements:

```text
IAREQ082
```

Verify public DTOs do not substitute provider/EF-specific IDs for canonical User/Account IDs.

## 114. IA-TST-API-OAS-001 — OpenAPI drift

Requirements:

```text
IAREQ083
```

Canonical OpenAPI generation/check must execute on the candidate SHA.

If the CSRF bootstrap endpoint is public API, the canonical spec must contain its route and response contract.

---

# CSRF closure tests

## 115. IA-TST-CSRF-API-001 — bootstrap returns body token and cookie

Requirements:

```text
IAREQ126
IAREQ127
```

Given CSRF is enabled for test configuration  
When the browser/client calls the canonical Auth CSRF bootstrap GET  
Then:

```text
2xx response
body.token is non-empty
Set-Cookie contains csrf_token
body token == cookie token value
```

Do not assert the exact random token.

## 116. IA-TST-CSRF-INF-001 — production cookie policy

Requirements:

```text
IAREQ128
```

Under production environment/configuration assert the CSRF cookie has:

```text
Secure = true
SameSite = None
Path = /
no widened Domain unless canonical decision says otherwise
```

Under development policy assert the accepted development SameSite/Secure behavior.

## 117. IA-TST-CSRF-API-002 — valid unsafe browser request passes

Requirements:

```text
IAREQ126
IAREQ129
```

Given matching:

```text
csrf_token cookie
X-CSRF-Token header
```

When a representative CSRF-required POST/PATCH/DELETE executes  
Then request reaches normal authentication/authorization/use-case processing.

## 118. IA-TST-CSRF-API-003 — missing cookie rejected

Requirements:

```text
IAREQ129
IAREQ130
```

Unsafe CSRF-required browser request with header only must be rejected using canonical forbidden/security ProblemDetails.

## 119. IA-TST-CSRF-API-004 — missing header rejected

Requirements:

```text
IAREQ129
IAREQ130
```

Unsafe CSRF-required browser request with cookie only must be rejected.

## 120. IA-TST-CSRF-API-005 — mismatch rejected in canonical error shape

Requirements:

```text
IAREQ130
```

Cookie/header mismatch must:

```text
return canonical 403/security error
not reveal expected token
not write raw tokens to logs
```

## 121. IA-TST-CSRF-API-006 — safe GET does not require validation

Requirements:

```text
IAREQ129
```

A normal safe GET that is not the bootstrap must not require a CSRF token.

The test should also prove middleware no longer emits a new CSRF cookie on every unrelated GET.

## 122. IA-TST-CSRF-API-007 — API-token principal is outside browser CSRF

Requirements:

```text
IAREQ129
```

Given a request authenticated exclusively through the canonical non-ambient API-token mechanism  
When it performs an otherwise valid unsafe API-token operation  
Then it is not rejected solely for missing browser CSRF cookie/header.

This test MUST still prove normal Governance authorization applies.

## 123. IA-TST-CSRF-CFG-001 — disabled feature flag preserves pre-enable operation

Requirements:

```text
IAREQ130
```

When `Security:Csrf:Enabled=false`, CSRF middleware must not reject requests because token bootstrap is absent.

This is a rollout compatibility test, not permission to leave the final deployable target disabled.

## 124. IA-TST-CSRF-CLIENT-001 — frontend does not read API cookie

Requirements:

```text
IAREQ126
IAREQ127
```

Frontend client test must prove the canonical CSRF provider does not require:

```text
document.cookie
XSRF-TOKEN
meta[name="csrf-token"]
```

A DOM with no accessible API cookie must still support bootstrap from response body.

## 125. IA-TST-CSRF-CLIENT-002 — bootstrap is instance-scoped single-flight

Requirements:

```text
IAREQ127
```

Given no in-memory token  
When multiple unsafe requests start concurrently  
Then exactly one bootstrap operation is in-flight for that client instance and all waiting requests receive the resulting token.

Do not assert global singleton behavior across independently created clients.

## 126. IA-TST-CSRF-CLIENT-003 — unsafe request sends X-CSRF-Token

Requirements:

```text
IAREQ126
IAREQ129
```

After bootstrap, POST/PUT/PATCH/DELETE requiring browser CSRF must attach:

```text
X-CSRF-Token: <memory token>
credentials: include
```

It must not send `X-XSRF-TOKEN`.

## 127. IA-TST-CSRF-CLIENT-004 — safe GET does not bootstrap unnecessarily

Requirements:

```text
IAREQ127
IAREQ129
```

A normal GET must not create an avoidable CSRF bootstrap request.

## 128. IA-TST-CSRF-CLIENT-005 — refresh uses the same CSRF-aware primitive

Requirements:

```text
IAREQ129
IAREQ102
```

Given a refresh is needed  
Then refresh request includes the canonical CSRF header and cookie credentials while preserving existing single-flight refresh behavior.

The test must fail if `refreshOnce()` bypasses the shared CSRF transport through raw fetch.

## 129. IA-TST-CSRF-CLIENT-006 — CSRF token is memory-only

Requirements:

```text
IAREQ127
```

Assert no write to:

```text
localStorage
sessionStorage
persistent auth storage adapter
```

for CSRF token lifecycle.

## 130. IA-TST-CSRF-REL-001 — CSRF recovery/retry is bounded

Requirements:

```text
IAREQ130
IAREQ119
IAREQ120
```

For accepted stale-token behavior, verify either:

```text
clear → bootstrap → retry once
```

or the canonical non-auto-retry policy.

In all cases:

```text
no infinite loop
no unbounded bootstrap storm
no refresh/CSRF recursion
```

## 131. IA-TST-CSRF-INT-001 — cross-origin browser flow

Requirements:

```text
IAREQ126–IAREQ130
```

Use production-like frontend/API origin separation.

Required flow:

```text
bootstrap
→ establish/authenticate browser session
→ unsafe mutation
→ refresh
→ unsafe mutation
→ new client/reload memory state
→ bootstrap again
→ unsafe mutation
```

This test must not rely on frontend JavaScript reading an API-host cookie.

## 132. IA-TST-CSRF-INT-002 — enablement smoke

Requirements:

```text
IAREQ130
IAREQ140
```

With the final intended CSRF-enabled configuration, the supported browser login/session/mutation flow remains usable and the negative CSRF matrix remains enforced.

---

# Event inventory and payload tests

## 133. IA-TST-EVT-DOM-001 — identity event emitted only for successful state change

Requirements:

```text
IAREQ084
```

Representative critical Identity state changes emit the canonical event only after the Domain transition succeeds.

## 134. IA-TST-EVT-DOM-002 — Account lifecycle event ownership

Requirements:

```text
IAREQ085
```

Representative Account lifecycle facts originate from Accounts rather than a downstream context.

## 135. IA-TST-EVT-INV-ARCH-001 — every public integration event is manifest/inventory visible

Requirements:

```text
IAREQ084
IAREQ085
IAREQ133
```

Discover all non-abstract `IIntegrationEvent` types in the production contract assemblies.

For Identity/Accounts events assert:

```text
EventName exists
Version > 0
(name, version) appears in canonical manifest
producer/context classification exists
```

A new event missing the manifest/inventory must fail.

## 136. IA-TST-EVT-INV-ARCH-002 — consumer registry maturity is explicit

Requirements:

```text
IAREQ133
```

For every registered consumer of an Identity/Account public event:

```text
maturity == Implemented or Stub
```

No implicit/default maturity value is accepted.

For events without a consumer, generated inventory records `NONE`.

## 137. IA-TST-EVT-INV-ARCH-003 — registered consumer and actual consumer type agree

Requirements:

```text
IAREQ133
```

Where a registry claims an implemented consumer, the actual production consumer type/registration must exist.

A stub registry entry must not satisfy the implemented-consumer assertion.

## 138. IA-TST-EVT-SEC-001 — prohibited secret material absent from public payloads

Requirements:

```text
IAREQ086
```

Architecture/security test inspects Identity/Accounts public integration-event serialized contracts for raw/prohibited secret classes or properties including semantic equivalents of:

```text
password/password hash
OAuth access/refresh token
MFA/TOTP secret
recovery material
raw API token
client/private secret
authorization/session secret
```

Use targeted contract tests when property-name scanning alone would miss a typed secret wrapper.

## 139. IA-TST-EVT-PRIV-001 — PII-bearing public events are explicitly classified

Requirements:

```text
IAREQ086
IAREQ134
IAREQ123
IAREQ124
IAREQ125
```

For every public event containing PII such as email/display identity:

```text
PII field listed in manifest metadata
purpose non-empty
consumer justification non-empty
```

A PII field that appears without classification must fail the contract gate.

## 140. IA-TST-EVT-PRIV-002 — stable ID replaces unnecessary mutable PII where accepted

Requirements:

```text
IAREQ086
IAREQ087
```

For events whose accepted consumer contract requires only stable identity/scope, assert unnecessary mutable PII is not part of the serialized integration contract.

Only apply after consumer semantics are resolved; do not invent removals in the test.

## 141. IA-TST-EVT-INT-001 — consumer receives sufficient stable identity/scope

Requirements:

```text
IAREQ087
```

Use a representative implemented Workspace/Billing consumer where available.

Verify the event provides required stable User/Account/scope data without the consumer reading private Identity/Account persistence.

If only a stub exists, this test cannot be counted as implemented cross-context evidence; record the limitation.

---

# Event versioning and compatibility tests

## 142. IA-TST-EVT-VER-ARCH-001 — contract key uniqueness is Name + Version

Requirements:

```text
IAREQ088
IAREQ131
```

Assert:

```text
same name + same version twice → fail
same name + different versions → allowed
```

Any old architecture test that requires logical name alone to be unique for versioned public integration events must be corrected.

Domain-only event naming tests may retain different rules when intentionally internal; test names must make the distinction clear.

## 143. IA-TST-EVT-VER-INF-001 — runtime catalog resolves exact version

Requirements:

```text
IAREQ131
```

Given registered:

```text
example.fact v1 → TypeV1
example.fact v2 → TypeV2
```

Then:

```text
Resolve(example.fact, 1) → TypeV1
Resolve(example.fact, 2) → TypeV2
```

No "latest" implicit fallback.

## 144. IA-TST-EVT-VER-INF-002 — unknown version fails deterministically

Requirements:

```text
IAREQ131
IAREQ119
```

Given known logical name but unsupported version  
Then production resolution throws/returns the canonical unknown/unsupported-contract failure.

It must not deserialize as another version.

## 145. IA-TST-EVT-VER-INF-003 — unknown logical name fails deterministically

Requirements:

```text
IAREQ131
IAREQ119
```

Preserve deterministic unknown event-type behavior without payload dumping.

## 146. IA-TST-EVT-VER-ARCH-002 — v1/v2 coexistence fixture

Requirements:

```text
IAREQ088
IAREQ131
IAREQ134
```

Use test-only event types to prove one logical name can coexist as v1/v2 through:

```text
contract registry
integration event catalog
manifest builder
consumer contract lookup where applicable
```

Test-only fixture must not leak into production event registration.

## 147. IA-TST-EVT-CONTRACT-001 — canonical event manifest drift

Requirements:

```text
IAREQ132
```

Generate current canonical public event manifest using production serializer/registry conventions and compare with:

```text
backend/contracts/events/notrelix.events.json
```

Difference fails with a diagnostic identifying event name/version and changed contract fields.

## 148. IA-TST-EVT-CONTRACT-002 — same version cannot change serialized schema

Requirements:

```text
IAREQ088
IAREQ132
```

Given checked-in contract `name=v1`  
When source serialized property name/type/nullability changes without version bump  
Then the manifest/compatibility test fails.

The failure message should direct the developer to either:

```text
restore v1 schema
or
introduce a new version and migration plan
```

## 149. IA-TST-EVT-CONTRACT-003 — manifest uses production serialization names

Requirements:

```text
IAREQ132
```

Use at least one fixture/property where CLR and serialized naming conventions could differ and prove the manifest represents production wire names, not naive raw reflection names.

## 150. IA-TST-EVT-MIG-001 — consumer dual-read fixture

Requirements:

```text
IAREQ134
IAREQ139
```

Using test fixtures, prove a consumer migration can accept both v1 and v2 during a compatibility window.

Do not require a real production event to bump version merely to satisfy this test.

## 151. IA-TST-EVT-MIG-002 — producer switch does not remove v1 registration prematurely

Requirements:

```text
IAREQ134
IAREQ139
```

Where an actual production event migration is performed, test/config evidence must prove v1 remains resolvable/consumable until the migration retirement step.

If no production event is bumped during Phase 13, mark this scenario `NOT_APPLICABLE — infrastructure-only versioning closure` with evidence.

## 152. IA-TST-EVT-OPS-001 — operational backlog evidence schema

Requirements:

```text
IAREQ135
```

The operational runbook/evidence format must be able to record:

```text
name
version
consumer
outbox pending
oldest pending
retry backlog
DLQ/poison
unsupported version
```

This may be `NOT_APPLICABLE_UNTIL_DEPLOYMENT`; it is not a mandatory CI runtime test when no environment exists.

---

# Authorization tests

## 153. IA-TST-AUTHZ-APP-001 — Identity protected operation declares authorization requirement

Requirements:

```text
IAREQ089
IAREQ136
```

Representative permission-protected request must be denied by the pipeline before handler side effects when actor lacks permission.

## 154. IA-TST-AUTHZ-APP-002 — Account protected operation declares authorization requirement

Requirements:

```text
IAREQ090
IAREQ136
```

Representative Account/Workspace create operation must carry the canonical account-scope authorization declaration.

## 155. IA-TST-AUTHZ-SEC-001 — bootstrap authority cannot become permanent bypass

Requirements:

```text
IAREQ091
```

After Account bootstrap, normal administration must pass the canonical permission/Governance path.

## 156. IA-TST-AUTHZ-APP-003 — self-service semantics remain explicit

Requirements:

```text
IAREQ092
```

Representative self-resource operation uses accepted actor-is-self semantics without requiring a broad Account/Workspace admin role.

## 157. IA-TST-AUTHZ-APP-004 — Account role/current action matrix

Requirements:

```text
IAREQ090
IAREQ138
```

Mandatory data-driven matrix:

| Role | ViewWorkspace | CreateWorkspace |
|---|---:|---:|
| Owner | allow | allow |
| Admin | allow | allow |
| Member | allow | deny |
| BillingAdmin | allow | deny |
| SecurityAdmin | allow | deny |

Run through the canonical `PermissionService`/evaluator path, not a duplicated test-only policy function.

## 158. IA-TST-AUTHZ-APP-005 — inactive/missing/wrong Account membership denies

Requirements:

```text
IAREQ090
IAREQ138
```

Cases:

```text
missing AccountMember
inactive/suspended member according to source model
member from Account B evaluated against Account A
```

Expected: deny with canonical reason class.

## 159. IA-TST-AUTHZ-APP-006 — explicit Governance deny overrides Admin fallback

Requirements:

```text
IAREQ138
```

Given active Admin otherwise allowed `CreateWorkspace`  
When an applicable explicit Governance deny exists  
Then decision is deny.

## 160. IA-TST-AUTHZ-APP-007 — explicit Governance allow can grant a baseline-denied non-owner action when supported

Requirements:

```text
IAREQ138
```

Use a current action and subject for which existing Governance rule semantics permit explicit allow.

Do not invent a new PermissionAction only for this test.

## 161. IA-TST-AUTHZ-ARCH-001 — protected handlers do not inject canonical authorization services

Requirements:

```text
IAREQ136
```

Architecture/source gate scans production Application handlers for direct dependencies on current equivalents of:

```text
IPermissionService
IPermissionEvaluator
IWorkspacePermissionService
IAuthorizationDecisionStore
```

A protected pipeline-owned handler using one of these must fail unless an exact approved special-contract exception exists.

## 162. IA-TST-AUTHZ-ARCH-002 — protected requests declare canonical authorization contract

Requirements:

```text
IAREQ136
```

Protected requests must implement/declare:

```text
IRequirePermission
or approved explicit self-service/special authorization contract
```

Do not infer protection from handler code.

## 163. IA-TST-AUTHZ-ARCH-003 — role-check allowlist is exact and semantic

Requirements:

```text
IAREQ137
```

Production handler role checks must either be absent or listed with:

```text
exact type/file
reason
owned business invariant
review trigger
```

Wildcard namespace/directory allowlisting fails.

A current-actor role authorization check cannot be allowlisted as a business invariant.

## 164. IA-TST-AUTHZ-ARCH-004 — target-role business invariants remain allowed

Requirements:

```text
IAREQ137
```

Use at least one real approved business invariant if source contains one, such as last-owner/ownership-transfer protection.

Prove the architecture gate can distinguish it from current-actor authorization and does not force removal of valid business rules.

If no such production invariant exists, use a test fixture in the architecture-test assembly and do not add production code merely for coverage.

## 165. IA-TST-AUTHZ-ARCH-005 — endpoint raw-auth gate remains green

Requirements:

```text
IAREQ136
```

Preserve existing API architecture guard preventing endpoint-local raw auth conventions where pipeline/route-group architecture owns them.

---

# Security master matrix — Phase 13+

## 166. IA-TST-SEC-MASTER-001 — secret logging scan

Requirements:

```text
IAREQ093
IAREQ094
```

For critical flows capture logs/traces and assert controlled sentinel secrets are absent:

```text
password
session/refresh secret
OAuth token
MFA/recovery secret
API token
Authorization header
client/private secret
CSRF token
```

Do not print the sentinel in test failure output.

## 167. IA-TST-SEC-MASTER-002 — enumeration resistance regression

Requirements:

```text
IAREQ095
```

Applicable endpoints:

```text
login
registration/recovery
token management
OAuth linking
```

Closure work must not make error taxonomy more revealing.

## 168. IA-TST-SEC-MASTER-003 — replay matrix

Requirements:

```text
IAREQ096
```

Test applicable:

```text
OAuth callback
MFA challenge
recovery/reset token
one-time credential result
CSRF stale/rotated pairing according to accepted policy
```

## 169. IA-TST-SEC-MASTER-004 — abuse-control integration

Requirements:

```text
IAREQ097
```

Sensitive operations use approved generic Platform abuse-control mechanism.

Do not assert an Identity-local bucket implementation.

## 170. IA-TST-SEC-MASTER-005 — security change revocation matrix

Requirements:

```text
IAREQ098
```

Populate actual policy from canonical authority:

| Security change | Session effect | Token effect | OAuth effect |
|---|---|---|---|
| User disable | canonical | canonical | canonical |
| Password/credential change | canonical | if defined | if defined |
| MFA reset | canonical | if defined | if defined |
| Security admin action | canonical | canonical | canonical |

## 171. IA-TST-SEC-MASTER-006 — CSRF is not replaced by CORS

Requirements:

```text
IAREQ126–IAREQ130
```

Test/config review must show CSRF enforcement remains required even when CORS allowlist is configured.

Do not create a false test that considers CORS headers alone sufficient.

## 172. IA-TST-SEC-MASTER-007 — event failure logging does not dump payload

Requirements:

```text
IAREQ094
IAREQ086
IAREQ119
```

Unknown/unsupported event resolution failures must not log the full sensitive payload by default.

---

# Concurrency tests — retained and extended

## 173. IA-TST-CONC-USER-001 — duplicate identity creation

Requirements:

```text
IAREQ099
```

Use real DB transaction/constraint where correctness depends on concurrency.

## 174. IA-TST-CONC-OAUTH-001 — same provider subject link race

Requirements:

```text
IAREQ100
```

At most one canonical link wins.

## 175. IA-TST-CONC-ACCOUNT-001 — Account bootstrap uniqueness race

Requirements:

```text
IAREQ101
```

Only if Account has a real unique bootstrap invariant.

## 176. IA-TST-CONC-SESSION-001 — session revoke vs refresh race

Requirements:

```text
IAREQ102
```

Final authoritative revocation must win according to accepted security window.

When refresh is browser-cookie based, the refresh leg must also use valid CSRF.

## 177. IA-TST-CONC-TOKEN-001 — API token revoke vs use race

Requirements:

```text
IAREQ103
```

Revocation cannot be undone by stale write/cache behavior.

## 178. IA-TST-CONC-CSRF-001 — concurrent unsafe requests share bootstrap

Requirements:

```text
IAREQ127
IAREQ130
```

Frontend transport concurrency test: multiple unsafe requests starting with no token cause one bootstrap per client instance, not N bootstrap requests.

## 179. IA-TST-CONC-CSRF-002 — refresh and CSRF bootstrap do not deadlock/recurse

Requirements:

```text
IAREQ102
IAREQ120
```

Simulate a request that needs CSRF bootstrap and later auth refresh.

Assert bounded completion/failure and no circular promise wait.

---

# Migration and compatibility tests

## 180. IA-TST-MIG-ACCOUNT-001 — Account location/identity compatibility

Requirements:

```text
IAREQ104
IAREQ107
```

Phase 13+ must preserve the accepted canonical Account owner/ID unless an explicit migration is opened.

Architecture/source test should fail if a second canonical Account aggregate/table is introduced.

## 181. IA-TST-MIG-IDENTITY-001 — no incidental User/Session/Token ID migration

Requirements:

```text
IAREQ105
IAREQ106
```

Closure changes must not alter canonical identity IDs unless migration evidence exists.

## 182. IA-TST-MIG-OAUTH-001 — OAuth mapping continuity

Requirements:

```text
IAREQ108
```

Run only if closure touches provider mapping/storage.

## 183. IA-TST-MIG-MFA-001 — MFA enrolled-user continuity

Requirements:

```text
IAREQ109
```

Run only if storage/method representation changes.

## 184. IA-TST-MIG-TOKEN-001 — API token verifier compatibility

Requirements:

```text
IAREQ110
```

Run only if token format/hash changes.

## 185. IA-TST-MIG-EVT-001 — all production event resolution callers propagate version

Requirements:

```text
IAREQ131
IAREQ139
```

Architecture/source gate finds all production calls into the event catalog/type registry and proves public integration-event resolution supplies both name and version.

No final name-only overload/call site remains.

## 186. IA-TST-MIG-EVT-002 — envelope carries/retains event version

Requirements:

```text
IAREQ131
IAREQ139
```

Using the production envelope/serializer path, publish/serialize a versioned event and assert the receiving resolution path observes the same version.

Do not satisfy by hard-coding version 1 in the test adapter.

## 187. IA-TST-MIG-CSRF-001 — deploy compatibility with feature flag off

Requirements:

```text
IAREQ130
```

Backend containing the new bootstrap/protocol but with CSRF enforcement disabled must remain compatible with the pre-enable runtime flow.

This proves safe staged rollout.

## 188. IA-TST-MIG-CSRF-002 — final enabled config requires compatible frontend

Requirements:

```text
IAREQ130
IAREQ140
```

The final integration/deploy smoke with CSRF enabled must use the new frontend transport; a legacy XSRF client must fail rather than silently bypass protection.

## 189. IA-TST-MIG-DB-001 — pending model gate

Requirements:

```text
IAREQ104–IAREQ110
```

If EF model changed:

```text
fresh DB succeeds
upgrade DB succeeds
seed/init succeeds
startup has no PendingModelChangesWarning
```

Do not suppress the warning.

If no EF model change exists, record migration DB test as `NOT_APPLICABLE — no schema delta` with source/model-diff evidence.

---

# Observability tests

## 190. IA-TST-OBS-001 — auth/session failure categories are safe

Requirements:

```text
IAREQ111
```

Representative auth/session failures emit diagnosable category/correlation without secret values.

## 191. IA-TST-OBS-002 — Account context trace is scoped

Requirements:

```text
IAREQ112
```

Where Account ID is logged/traced, verify the correct stable Account ID is associated and no cross-tenant value leaks through reused context.

## 192. IA-TST-OBS-003 — security operation correlation

Requirements:

```text
IAREQ113
```

Representative sensitive mutation can be correlated:

```text
request
→ Application
→ persistence
→ event/audit
```

using non-secret identifiers.

## 193. IA-TST-OBS-004 — CSRF failure category

Requirements:

```text
IAREQ114
```

CSRF rejection creates the existing observability/error category expected by canonical instrumentation without logging token content.

## 194. IA-TST-OBS-005 — unsupported event version category

Requirements:

```text
IAREQ114
IAREQ119
```

Unsupported `(name, version)` produces a diagnosable category/counter/log event under existing observability conventions, without payload dump.

## 195. IA-TST-OBS-006 — consumer maturity/ops evidence is not inferred from metrics

Requirements:

```text
IAREQ133
IAREQ135
```

Source contract tests remain authoritative for IMPLEMENTED/STUB/NONE classification; absence of runtime traffic must not reclassify source maturity.

---

# Performance tests/reviews

## 196. IA-TST-PERF-001 — authorization is not evaluated twice

Requirements:

```text
IAREQ115
IAREQ116
IAREQ136
```

For representative pipeline-protected Account/Workspace operations, verify closure does not add a handler-level duplicate permission lookup.

This may use query counting/instrumented fake only if the instrumentation observes the real production composition path.

## 197. IA-TST-PERF-002 — CSRF token reuse avoids repeated bootstrap

Requirements:

```text
IAREQ115
IAREQ127
```

After successful bootstrap, N sequential unsafe requests in the same client instance reuse the in-memory token until accepted invalidation/expiry policy requires renewal.

## 198. IA-TST-PERF-003 — correctness before caching

Requirements:

```text
IAREQ116
IAREQ117
IAREQ118
```

Any authorization/session/Account cache optimization must preserve revoke/tenant-isolation correctness.

No arbitrary latency threshold is invented here; use canonical system quality budgets where present.

---

# Reliability tests

## 199. IA-TST-REL-001 — external provider failure leaves local state consistent

Requirements:

```text
IAREQ119
```

OAuth/SSO timeout/invalid response/temporary failure must not create partial canonical identity link state.

## 200. IA-TST-REL-002 — sensitive persistence failure does not return success

Requirements:

```text
IAREQ120
```

Representative session/token/MFA/OAuth mutation fails without false success when authoritative persistence fails.

## 201. IA-TST-REL-003 — partial Account bootstrap is explicit

Requirements:

```text
IAREQ121
```

Where Account creation orchestrates external/context bootstrap, partial failure follows the accepted compensation/retry model.

## 202. IA-TST-REL-004 — repair path authorization/audit

Requirements:

```text
IAREQ122
```

If repair tooling/endpoints exist, verify strong authorization, Account scope, audit, and no secret readback.

## 203. IA-TST-REL-005 — unknown event name does not fallback

Requirements:

```text
IAREQ119
IAREQ131
```

Unknown logical name must not resolve to arbitrary/default type.

## 204. IA-TST-REL-006 — unsupported event version does not fallback to latest/oldest

Requirements:

```text
IAREQ119
IAREQ131
```

Known name + unknown version fails deterministically and follows canonical retry/poison policy.

## 205. IA-TST-REL-007 — bootstrap network failure is bounded

Requirements:

```text
IAREQ119
IAREQ127
```

Frontend CSRF bootstrap network failure must surface one deterministic client error and release the single-flight promise for later retry.

## 206. IA-TST-REL-008 — refresh failure remains session failure, not infinite CSRF recovery

Requirements:

```text
IAREQ119
IAREQ120
```

401/403 refresh behavior must preserve the canonical session-expired callback semantics without looping between refresh and CSRF bootstrap.

---

# Privacy tests

## 207. IA-TST-PRIV-001 — profile/API minimization

Requirements:

```text
IAREQ123
IAREQ124
```

Representative profile/current-identity APIs expose only product-required personal fields.

## 208. IA-TST-PRIV-002 — Account and User personal data remain separated

Requirements:

```text
IAREQ125
```

Account DTO/event contract must not automatically embed creator/user personal profile data unless explicitly required.

## 209. IA-TST-PRIV-003 — event manifest contains no raw secret values

Requirements:

```text
IAREQ086
IAREQ123
```

Canonical event contract manifest may describe field names/types/classification but must never contain actual runtime secret/PII sample values.

---

# Cross-team integration tests

## 210. IA-TST-X-CSRF-001 — frontend contains no active legacy XSRF contract

Requirements:

```text
IAREQ126
IAREQ140
```

Source/test gate over production frontend contract/client code must classify all hits for:

```text
XSRF-TOKEN
X-XSRF-TOKEN
getCsrfToken document-cookie behavior
```

Active production legacy convention fails.

Historical ADR text may remain only if explicitly marked superseded and excluded from active-contract checks.

## 211. IA-TST-X-AUTHZ-001 — Workspace creation uses Account pipeline permission

Requirements:

```text
IAREQ090
IAREQ136
IAREQ138
```

Production-graph integration test executes `CreateWorkspace` for representative roles and proves handler does not contain a separate role branch.

## 212. IA-TST-X-EVT-001 — implemented consumer uses versioned contract

Requirements:

```text
IAREQ087
IAREQ131
IAREQ133
```

For at least one implemented downstream consumer, verify registration includes the exact event version and consumption succeeds for that version.

## 213. IA-TST-X-EVT-002 — stub consumer is reported as stub

Requirements:

```text
IAREQ133
```

Pick a current stub consumer and prove the generated inventory/manifest reports `STUB`, not `IMPLEMENTED`.

If no stubs remain by candidate SHA, mark the scenario `NOT_APPLICABLE — no current stub consumers` and let the architecture maturity gate cover future additions.

## 214. IA-TST-X-EVT-003 — downstream consumer does not require private Identity persistence

Requirements:

```text
IAREQ087
```

Preserve/extend bounded-context architecture tests preventing Workspace/Billing/etc. from reading Identity/Accounts infrastructure types directly.

## 215. IA-TST-X-EVT-004 — v1/v2 compatibility fixture crosses registry boundary

Requirements:

```text
IAREQ088
IAREQ134
```

Test fixture must exercise at least:

```text
producer contract metadata
registry/catalog
consumer contract selection
```

rather than only constructing `EventContractKey` as a value object.

---

# Phase 13 hard-close gate tests

## 216. IA-TST-CLOSE-001 — no unresolved source work unit

Requirements:

```text
IAREQ140
```

Certification/execution evidence must enumerate:

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

Any status other than `DONE` fails Phase 13 closure.

This may be a certification/doc validation gate if the repository has execution metadata tooling; otherwise it is a mandatory certification review item backed by test evidence below.

## 217. IA-TST-CLOSE-002 — operational evidence cannot hide source gap

Requirements:

```text
IAREQ135
IAREQ140
```

`NOT_APPLICABLE_UNTIL_DEPLOYMENT` is permitted only for P13-EVT-OPS evidence.

It must not appear as the final status of IA-EVT-001/002/003 or IA-API-003/IA-AUTHZ-003/004.

## 218. IA-TST-CLOSE-003 — OpenAPI and event manifest both clean

Requirements:

```text
IAREQ083
IAREQ132
IAREQ140
```

Candidate SHA must pass both independent generated-contract drift gates.

---

# Infrastructure test requirements

## 219. Persistence roundtrip

Critical Identity/Account entities retain existing persistence roundtrip tests where custom mappings/value conversions exist.

Phase 13 closure should not add persistence merely for CSRF/event contract metadata unless explicitly justified.

## 220. Unique indexes

Retain DB constraint tests for:

- canonical User identifier;
- provider subject;
- Account unique keys if any;
- token identifier/hash if applicable.

## 221. Tenant filters/RLS

Identity/Account integration tests must prove correct behavior through production-like DbContext/RLS configuration where current backend uses it.

Phase 13 authorization changes must not replace DB isolation.

## 222. Secret columns

Sensitive persisted material is never returned through ordinary query DTOs.

---

# API test requirements

## 223. Contract test matrix

For every release-scoped endpoint classify applicability of:

```text
happy path
validation failure
authentication failure
authorization failure
not-found/privacy
security-sensitive response shape
CSRF required/not-required
```

Do not copy CSRF tests to every endpoint; shared middleware plus representative high-risk operations are sufficient when applicability is separately classified.

## 224. CSRF representative endpoint classes

At minimum cover:

```text
session/auth refresh
one authenticated self-service mutation
one Account/Workspace mutation
API-token/non-browser unsafe request
```

## 225. OpenAPI

Any public bootstrap/response change is caught by canonical OpenAPI drift gate.

---

# Architecture-test requirements

## 226. Domain purity

Identity/Account Domain cannot reference:

- Infrastructure;
- API;
- EF;
- HTTP;
- provider SDKs;
- CSRF transport;
- event manifest file I/O.

## 227. Bounded-context isolation

Preserve Identity/Account/Workspace/Governance ownership boundaries.

## 228. Authorization pipeline

Architecture suite MUST include:

```text
no protected handler direct auth-service dependency
protected request declaration
exact role-check exception registry
endpoint raw-auth gate
```

## 229. Event contracts

Architecture suite MUST include:

```text
EventName/version completeness
(Name, Version) uniqueness
manifest drift
consumer maturity completeness
secret payload guard
PII classification guard
version-aware caller guard
```

## 230. Private persistence

Downstream Application/Domain contexts must not depend on Identity Infrastructure/private persistence types.

---

# Integration-test requirements

## 231. Production graph

At least one critical path per closure domain resolves through real production DI:

```text
browser session + CSRF
Account authorization
versioned event catalog/consumer
```

Do not prove closure solely with manually constructed services that bypass DI registrations.

## 232. Database

Use actual production-like DB provider when correctness depends on:

```text
unique constraints
tenant isolation
migration
session/token persistence
```

SQLite-only evidence is insufficient for provider-specific behavior when production differs.

## 233. Cross-origin transport

CSRF end-to-end test must model the supported origin separation/cookie policy.

A same-origin-only test cannot be the sole evidence for IAREQ128.

## 234. Cross-context event contract

Use actual Application/messaging contracts rather than direct repository reads.

---

# CI mapping — revised

## 235. architecture-tests

Must include non-zero execution of:

```text
IA-TST-AUTHZ-ARCH-*
IA-TST-EVT-INV-ARCH-*
IA-TST-EVT-VER-ARCH-*
IA-TST-EVT-CONTRACT-*
IA-TST-CSRF-ARCH-001
IA-TST-TRACE-001
IA-TST-MIG-EVT-001 where implemented as architecture/source gate
existing Domain purity/bounded-context/private-persistence gates
```

## 236. core-tests — Domain

Must include affected Domain tests for:

```text
Identity/Account events
Account role/invariants if Domain behavior changed
existing User/Session/OAuth/MFA/API-token invariants
```

Do not force Domain tests for pure API/Platform CSRF transport.

## 237. core-tests — Application

Must include:

```text
IA-TST-AUTHZ-APP-*
existing Identity/Account application regression
```

## 238. core-tests — Infrastructure

Must include:

```text
IA-TST-CSRF-INF-001
IA-TST-EVT-VER-INF-*
relevant migration/persistence tests
```

according to existing project ownership.

## 239. platform-tests

Must include shared mechanism tests where CSRF/messaging contracts live in Platform/shared infrastructure.

If implementation remains entirely Infrastructure/API, do not create artificial Platform tests; keep the CI job regression-green.

## 240. api-tests

Must include:

```text
IA-TST-API-*
IA-TST-CSRF-API-*
canonical OpenAPI drift
```

## 241. integration-tests

Must include:

```text
IA-TST-CSRF-INT-*
IA-TST-X-AUTHZ-001
IA-TST-X-EVT-* applicable implemented-consumer paths
IA-TST-MIG-EVT-002
tenant/revocation/production graph regression
```

## 242. frontend contract/client tests

The frontend CI/package test gate must execute:

```text
IA-TST-CSRF-CLIENT-*
IA-TST-X-CSRF-001
```

or their code-level test names mapped to these IDs in execution evidence.

## 243. CI non-zero requirement

Relevant filters must match at least one intended test.

Green zero-test job is not evidence.

## 244. Exact-SHA requirement

Certification references only the exact candidate SHA whose required CI/gates passed.

If frontend/backend are certified at separate SHAs before merge, final integration certification must identify the combined source/deployment candidate explicitly.

---

# Test implementation order — Phase 13+

## 245. Required sequence

```text
1. preserve existing Phase 13 completed regression tests
2. CSRF backend contract tests
3. CSRF frontend client tests
4. CSRF cross-origin integration
5. Account role/action Application tests
6. authorization bypass architecture gates
7. event inventory/maturity gates
8. event secret/PII gates
9. EventContractKey/runtime version tests
10. v1/v2 coexistence fixture
11. event manifest/schema drift gate
12. compatibility/migration tests
13. security/reliability/performance regression
14. cross-team integration
15. full suite + generated contracts
16. exact-SHA certification evidence
```

## 246. Test-first rule

Write failing tests first when the target is already frozen by SPEC/PLAN:

```text
CSRF cookie/header names
Account role/action matrix
no handler direct permission injection
(Name, Version) uniqueness
unknown version behavior
manifest schema drift
secret payload prohibition
```

Do not write expected behavior before an unresolved stop condition is settled.

---

# Test fixture design — Phase 13+

## 247. CSRF client fixture

Provide a deterministic fake fetch/server harness capable of:

```text
bootstrap response + Set-Cookie semantics
unsafe request observation
refresh response
403/401/network failure
concurrent requests
```

Do not make the fake expose API-host cookie through `document.cookie`, because that would hide the real cross-origin constraint.

## 248. Authorization fixture

Use canonical AccountMember/PermissionRule builders for:

```text
Owner
Admin
Member
BillingAdmin
SecurityAdmin
active/inactive
Account A/Account B
explicit allow/deny
```

Fixture must not duplicate policy logic to compute expected result.

## 249. Event version fixture

Use test-only `IIntegrationEvent` types for:

```text
same logical name v1
same logical name v2
duplicate same name/version
unknown version
serialization naming difference
```

Keep test types outside production contract discovery or explicitly scope discovery so fixtures do not enter runtime manifest.

## 250. Sensitive payload fixture

Use controlled sentinel secret/PII values.

Test failure output must not echo the sentinel secret.

## 251. Consumer maturity fixture

Use at least one implemented and one stub classification when current source has both.

Do not infer from naming in the final mechanism.

---

# Test anti-patterns — Phase 13+

## 252. Do not mock away cross-origin CSRF

Bad:

```text
frontend reads csrf_token from document.cookie in test
→ request passes
```

Good:

```text
bootstrap response body provides client token
browser cookie remains ambient/server-visible
```

## 253. Do not test a duplicated authorization matrix

Bad:

```text
test helper contains same role switch as production
→ compares production to duplicated switch
```

Good:

```text
data-driven expected matrix
→ execute canonical PermissionService
```

## 254. Do not prove event versioning with metadata only

Bad:

```text
EventNameAttribute.Version == 2
```

while catalog still resolves by name.

Good:

```text
runtime catalog/registry selects exact (name, version)
```

## 255. Do not weaken compatibility by snapshot auto-update

The event manifest test must fail on drift.

CI must not regenerate and overwrite the checked-in baseline automatically before comparison.

Contract baseline changes require intentional source review/commit.

## 256. Do not classify registry presence as implemented consumer

Consumer maturity is explicit.

`STUB` is not `IMPLEMENTED`.

## 257. Do not allow wildcard auth exceptions

No namespace/folder wildcard for handler role/permission bypasses.

## 258. Do not use operational absence as source proof

No broker traffic/DLQ does not prove an event has no consumer.

Source registry/consumer inventory is separate.

---

# Requirement-by-requirement verification completeness — revised

## 259. IAREQ001–IAREQ011

Covered by existing:

```text
IA-TST-USER-*
IA-TST-ACTOR-*
IA-TST-PROFILE-*
```

## 260. IAREQ012–IAREQ023

Covered by existing:

```text
IA-TST-AUTH-*
IA-TST-SESSION-*
```

## 261. IAREQ024–IAREQ039

Covered by existing:

```text
IA-TST-ACCOUNT-*
IA-TST-CTX-*
IA-TST-X-*
```

## 262. IAREQ040–IAREQ067

Covered by existing OAuth/MFA/Security/API-token families.

## 263. IAREQ068–IAREQ078

Covered by:

```text
IA-TST-X-*
IA-TST-OWN-*
```

## 264. IAREQ079–IAREQ083

Must be covered by:

```text
IA-TST-API-*
```

## 265. IAREQ126–IAREQ130

Must be covered by:

```text
IA-TST-CSRF-API-*
IA-TST-CSRF-INF-*
IA-TST-CSRF-CLIENT-*
IA-TST-CSRF-INT-*
IA-TST-CSRF-REL-*
IA-TST-MIG-CSRF-*
IA-TST-X-CSRF-*
```

## 266. IAREQ084–IAREQ088

Must be covered by:

```text
IA-TST-EVT-DOM-*
IA-TST-EVT-SEC-*
IA-TST-EVT-PRIV-*
IA-TST-EVT-INT-*
IA-TST-EVT-VER-*
```

## 267. IAREQ131–IAREQ135

Must be covered by:

```text
IA-TST-EVT-INV-*
IA-TST-EVT-VER-*
IA-TST-EVT-CONTRACT-*
IA-TST-EVT-MIG-*
IA-TST-EVT-OPS-*
IA-TST-MIG-EVT-*
```

## 268. IAREQ089–IAREQ092

Must be covered by:

```text
IA-TST-AUTHZ-APP-*
IA-TST-AUTHZ-SEC-*
```

## 269. IAREQ136–IAREQ138

Must be covered by:

```text
IA-TST-AUTHZ-APP-004..007
IA-TST-AUTHZ-ARCH-*
IA-TST-X-AUTHZ-001
IA-TST-PERF-001
```

## 270. IAREQ093–IAREQ103

Must be covered by:

```text
IA-TST-SEC-MASTER-*
IA-TST-CONC-*
```

## 271. IAREQ104–IAREQ110 + IAREQ139

Must be covered by applicable:

```text
IA-TST-MIG-*
IA-TST-EVT-MIG-*
```

If no DB/schema migration exists, DB migration rows may be `NOT_APPLICABLE` with model-diff evidence; event contract compatibility is still mandatory.

## 272. IAREQ111–IAREQ125

Must be covered by:

```text
IA-TST-OBS-*
IA-TST-PERF-*
IA-TST-REL-*
IA-TST-PRIV-*
```

plus source/config review only where runtime assertion is not practical.

## 273. IAREQ140

Must be covered by:

```text
IA-TST-CLOSE-*
full required suite execution
OpenAPI drift
event manifest drift
exact-SHA certification review
```

---

# Phase 13 mandatory minimum set

## 274. Non-optional closure tests

Phase 13 cannot close without evidence equivalent to all of:

```text
CSRF bootstrap body + cookie
CSRF valid/missing/mismatch
CSRF API-token non-browser exclusion
CSRF endpoint applicability inventory/drift gate
frontend no cookie-read/XSRF dependency
frontend refresh with CSRF
cross-origin CSRF flow
AccountRole × ViewWorkspace/CreateWorkspace matrix
explicit Governance deny precedence
handler no direct permission-service gate
role-check exact exception gate
event inventory completeness
consumer maturity classification
secret payload guard
PII classification guard
(Name, Version) runtime resolution
v1/v2 coexistence
unknown version deterministic failure
event manifest drift
global event manifest scope/owner-isolation gate
same-version schema-change rejection
version propagation through envelope/caller
OpenAPI drift
Phase 13 PLAN traceability gate
Phase 13 status closure gate
```

No representative subset may replace this list where the item protects a distinct closure invariant.

---

# Full Phase 13–20 regression set

## 275. Backend suites

Candidate must pass canonical:

```text
Architecture
Domain
Application
Infrastructure
Platform
API
Integration
```

with actual candidate counts recorded later in certification.

## 276. Frontend suites

Candidate frontend contract/client tests covering CSRF must pass.

If frontend and backend live in one monorepo candidate, final CI evidence should reference the same integrated SHA where possible.

## 277. Generated artifacts

Both must be clean:

```text
backend/contracts/openapi/notrelix.v1.json
backend/contracts/events/notrelix.events.json
```

using repository canonical generation/check semantics.

## 278. Operational event evidence

May be:

```text
VERIFIED
NOT_APPLICABLE_UNTIL_DEPLOYMENT
```

only for runtime backlog/DLQ evidence.

---

# Certification handoff format — revised

## 279. Test evidence row

For each closure capability provide:

```text
Capability/work unit:
Requirement IDs:
Test IDs:
Test project/suite:
Command:
Result/count:
CI job:
Candidate SHA:
Generated artifact/check:
Known exclusions:
```

Do not put future PASS values in this TESTS document.

## 280. Missing test handling

If a material requirement cannot be tested because infrastructure is missing:

```text
mark verification gap
→ open explicit test-infrastructure work unit
→ Phase 13/affected phase cannot close
```

Do not downgrade to manual review when an executable gate is reasonably implementable.

## 281. Operational-only exception

Only P13-EVT-OPS environment evidence may remain `NOT_APPLICABLE_UNTIL_DEPLOYMENT` without blocking source closure.

---

# Test-review checklist — Phase 13+

## 282. CSRF review

- [ ] one cookie/header naming convention;
- [ ] bootstrap response-body transport;
- [ ] host-only cookie does not require JS read;
- [ ] production cookie attributes tested;
- [ ] safe GET behavior tested;
- [ ] missing/mismatch tested;
- [ ] refresh tested;
- [ ] API-token path tested;
- [ ] every release-scoped unsafe operation in this workstream is applicability-classified;
- [ ] new unsafe endpoint/auth-mode drift fails until classified;
- [ ] cross-origin topology tested;
- [ ] retry/single-flight bounded;
- [ ] final enabled configuration smoke tested.

## 283. Authorization review

- [ ] all 5 Account roles in current action matrix;
- [ ] explicit deny precedence;
- [ ] missing/inactive/wrong Account denial;
- [ ] protected request declaration gate;
- [ ] no direct auth-service handler dependency;
- [ ] role checks classified;
- [ ] no wildcard allowlist;
- [ ] endpoint raw-auth gate remains green;
- [ ] no duplicate pipeline + handler evaluation.

## 284. Event inventory/privacy review

- [ ] every Identity/Account integration event inventoried;
- [ ] Domain vs integration classification explicit;
- [ ] consumer IMPLEMENTED/STUB/NONE explicit;
- [ ] secret payload gate;
- [ ] PII classification/justification;
- [ ] representative consumer scope sufficiency;
- [ ] no private persistence dependency.

## 285. Event versioning review

- [ ] `(Name, Version)` key used at runtime;
- [ ] same name/different version allowed;
- [ ] duplicate pair rejected;
- [ ] unknown version deterministic;
- [ ] no implicit latest/default fallback;
- [ ] v1/v2 fixture crosses registries;
- [ ] manifest uses production serialization;
- [ ] global manifest scope is explicit and unrelated bounded-context business contracts are owner-isolated;
- [ ] same-version schema drift rejected;
- [ ] envelope/callers propagate version;
- [ ] dual-read migration fixture exists.

## 286. Migration review

- [ ] no incidental Identity/Account ID change;
- [ ] no unexplained EF model delta;
- [ ] pending model gate if applicable;
- [ ] CSRF staged rollout test;
- [ ] event registry caller migration complete;
- [ ] public contract compatibility documented.

## 287. Security/reliability review

- [ ] secret logging scan;
- [ ] enumeration regression;
- [ ] replay regression;
- [ ] revocation races;
- [ ] CSRF not replaced by CORS;
- [ ] event failure does not dump payload;
- [ ] unsupported event version failure path;
- [ ] bootstrap/refresh failure bounded.

## 288. CI review

- [ ] architecture-tests non-zero;
- [ ] Domain/Application/Infrastructure/Platform/API/Integration green as applicable;
- [ ] frontend CSRF/client tests green;
- [ ] OpenAPI drift green;
- [ ] event manifest drift green;
- [ ] Phase 13+ normative PLAN traceability validation green;
- [ ] exact candidate SHA recorded;
- [ ] no source-level Phase 13 deferred item.

---

# Definition of Done — TESTS artifact revised

## 289. TESTS document is complete when

- all prior Identity/Accounts requirement families remain mapped;
- Phase 13 CSRF/authz/event closure has concrete executable tests;
- Phase 14 compatibility has explicit applicability/tests;
- Phase 15 security regression is explicit;
- Phase 16 reliability/performance failure modes are explicit;
- Phase 17 cross-team frontend/consumer evidence is explicit;
- Phase 18–20 handoff can reference real test IDs and CI jobs;
- no material closure invariant is represented only by a vague review sentence when a machine gate is feasible.

## 290. TESTS document does not mean implementation already exists

This artifact defines required verification.

Execution may classify each test as:

```text
existing and sufficient
existing but stale
missing — implement
not applicable with SPEC-supported evidence
blocked by explicit stop condition
```

`missing` does not mean the requirement may be deferred and Phase 13 still close.

## 291. Final verification rule

Before Phase 13 or final Identity & Accounts scope is marked stable, ask:

```text
Can we prove:
- browser CSRF across real origin boundaries?
- Account administration through one authorization path?
- absence of handler auth bypass regression?
- source-complete event inventory?
- event payload secret/PII discipline?
- exact (Name, Version) event resolution?
- v1/v2 compatibility behavior?
- generated API/event contract stability?
- failure/retry behavior?
- exact-SHA CI execution?
```

If the answer is no for a material item:

```text
the relevant phase is not closed
```

That rule is mandatory.

---

# Residual closure gates — targeted completion

## 292. IA-TST-CSRF-ARCH-001 — endpoint applicability inventory is complete and drift-safe

Requirements:

```text
IAREQ129
IAREQ130
IAREQ140
```

Use the existing Architecture test project to discover release-scoped API endpoint registrations/metadata for:

```text
Identity endpoints
Account/Workspace mutations owned by this workstream even when physically outside Identity/
```

For every unsafe `POST` / `PUT` / `PATCH` / `DELETE` operation in scope, the gate must resolve or require explicit classification containing:

```text
endpoint/operation identity
semantic endpoint family
accepted authentication mode(s)
ambient cookie session used/established? yes/no
CSRF_REQUIRED | CSRF_NOT_REQUIRED
reason
```

Required semantics:

- endpoint mapping metadata may be an input, but the test must not infer CSRF from route strings;
- `Public` is not a blanket CSRF exemption for an unsafe operation that establishes ambient cookie state;
- `Authenticated`/Account/Workspace is not proof that every credential mode is ambient cookie auth;
- explicit non-ambient API-token/service-principal invocation is outside browser CSRF unless another accepted contract says otherwise;
- login/refresh/logout or equivalent session-establishing/session-mutating operations are classified from actual session behavior;
- a new unsafe release-scoped endpoint or newly accepted auth mode fails until the classification contract is updated.

The classification may be represented by canonical endpoint/auth metadata or a focused architecture-test inventory fixture. It MUST NOT become a production path-string allowlist.

The test must report unclassified endpoint type/operation and semantic metadata without leaking credential/token values.

## 293. IA-TST-EVT-CONTRACT-004 — global event manifest scope preserves bounded-context ownership

Requirements:

```text
IAREQ132
IAREQ133
IAREQ139
```

Generate/validate `backend/contracts/events/notrelix.events.json` from the complete production public integration-event registry.

Assert:

```text
all production public integration events expected by the canonical registry are represented
Identity/Accounts rows carry their accepted producer/contract metadata
Domain-only internal events are not promoted merely by manifest generation
```

Identity & Accounts Phase 13 may alter shared manifest schema/generator mechanics required by the accepted versioning design, but the resulting semantic diff MUST NOT silently change an unrelated bounded context's:

```text
business payload shape
logical event version
producer ownership
consumer maturity
```

If unrelated semantic drift exists, fail with the affected `(name, version)` and owning context so the change is routed to that owner.

Deterministic ordering/format changes produced by the canonical generator are not themselves a bounded-context business-contract mutation; compare semantic contract data, not incidental JSON formatting.

## 294. IA-TST-TRACE-001 — Phase 13+ PLAN work units have normative requirement traceability

Requirements:

```text
IAREQ140
IAAC017
IAAC021
```

Implement this in the existing Architecture test project; do not create a new production project or a second documentation-validation authority for this workstream.

The gate must read the canonical `identity-accounts.plan.md` and verify that the `Phase 13+ normative traceability matrix` contains:

```text
every P13-* source/closure work unit defined by the PLAN
P13-FINAL-01
Phase 14
Phase 15
Phase 16
Phase 17
Phase 18
Phase 19
Phase 20
```

Each required row must have non-empty mappings for:

```text
SPEC requirement(s) / accepted IAAC where applicable
TESTS family or test ID
primary implementation/evidence surface
CI/artifact evidence
```

The gate fails when:

```text
a required work unit/phase is absent
a Phase 13 source work unit has no SPEC requirement mapping
a mapped test ID/family is removed without updating the canonical TESTS contract
a duplicate row creates competing traceability authority
```

This gate validates documentation traceability only. It does not replace the implementation tests mapped by the table.


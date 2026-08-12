---
document_id: QLT-SECURITY
document_type: quality-standard
status: active
owner: engineering-security
applies_to:
  - repository
  - backend
  - frontend
  - api
  - background-processing
  - integrations
  - ci
evidence:
  - RULE.md
  - docs/quality/engineering-quality-standard.md
  - docs/quality/testing-strategy.md
  - docs/product/contexts/identity.md
  - docs/product/contexts/workspaces.md
  - docs/product/contexts/governance.md
  - docs/product/contexts/integrations.md
  - docs/product/contexts/billing.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/decisions/ADR-002-rls-bootstrap-connection-lifecycle.md
  - backend/docs/decisions/ADR-003-csrf-protection.md
  - backend/docs/decisions/ADR-004-rate-limiting-architecture.md
  - .github/workflows/be-ci.yml
review_on:
  - authentication-boundary-change
  - authorization-boundary-change
  - tenant-isolation-change
  - secret-or-configuration-change
  - webhook-or-provider-boundary-change
  - file-upload-change
  - public-sharing-change
  - billing-payment-change
  - dependency-security-policy-change
  - incident-learnings-change
---

# Security Quality Standard

> **Security quality means hostile, stale, malformed, cross-tenant, over-privileged, replayed, and partially compromised inputs fail safely without turning convenience boundaries into authority.**
>
> Security is a system property across product semantics, API/Application enforcement, persistence/RLS, cache, messaging, realtime, providers, frontend presentation, configuration, and operations.

This document owns repository-wide secure-engineering quality requirements.

It does not replace:

- product authorization semantics owned by Governance;
- Identity lifecycle semantics;
- backend security/tenancy implementation architecture;
- incident response/runbooks;
- deployment-specific secret-manager configuration.

OWASP ASVS/Top 10 can be used as verification references. They do not replace Notrelix-specific tenant, product, and provider threat contracts.

---

# 1. Security model

Notrelix assumes:

```text
client input is untrusted
provider input is untrusted
authenticated users can still be unauthorized
tenant IDs can be forged
resource IDs can be guessed/leaked
messages can duplicate/reorder/replay
provider outcomes can be uncertain
configuration can be wrong
caches can become stale
dependencies can become vulnerable
operators can make mistakes
```

Security quality is the evidence that these realities are handled intentionally.

---

# 2. QLT-SEC-001 — External input is untrusted

Validate at the owning boundary:

```text
shape
size
type/discriminator
format
range
normalization
business constraints
scope
authorization
```

Do not infer trust from:

- authenticated transport;
- internal-looking endpoint;
- signed provider request;
- generated client;
- database origin from a foreign context.

---

# 3. Trusted transport versus trusted meaning

TLS, webhook signatures, or authenticated sessions prove limited properties.

They do not prove the payload is semantically valid.

Example:

```text
valid provider signature
≠ valid Notrelix Workspace/resource mutation
```

---

# 4. QLT-SEC-002 — Validation and authorization are separate

A valid request can be unauthorized.

An authorized caller can submit invalid business input.

Tests and error handling preserve the distinction.

---

# 5. Parsing

Untrusted parsers should:

- bound input size;
- reject malformed/unknown schema safely;
- avoid catastrophic complexity;
- avoid unsafe polymorphic deserialization;
- preserve compatibility policy.

---

# 6. QLT-SEC-003 — Unknown discriminator fails safely

Unknown provider/event/block/action/request types MUST NOT silently map to a privileged/default implementation.

Use explicit compatibility or rejection.

---

# 7. Injection

Use parameterized/query-safe APIs and context-appropriate encoding.

Do not construct executable SQL, HTML, shell commands, template code, or provider query syntax from unchecked input.

---

# 8. QLT-SEC-004 — Encoding happens for the destination context

Input validation does not replace output encoding.

Examples:

```text
HTML
URL
SQL parameter
CSV
shell/process argument
log/telemetry
```

have different safety requirements.

---

# 9. Authentication

Identity proves the principal/session/credential according to Identity contracts.

Authentication is not general product permission.

---

# 10. QLT-SEC-005 — Authentication success does not grant product access

Every protected resource/action still requires current scope and authorization.

A valid session can legitimately receive:

```text
403 / denied
```

for a resource.

---

# 11. Session security

Session/refresh/token tests should cover:

- expiry;
- revocation;
- replay;
- rotation;
- invalidation after security change;
- cross-device/session scope.

---

# 12. QLT-SEC-006 — Revoked credentials do not revive through stale state

Cache, realtime connection, refresh overlap, or retry must not restore revoked authority accidentally.

---

# 13. MFA/recovery

Security-sensitive recovery should require adequate proof and invalidate authorities according to policy.

Do not weaken recovery because the user knows profile data.

---

# 14. API/service credentials

API/service tokens have:

```text
subject
scope
expiry
status
revocation
safe metadata
```

They are not unrestricted immortal User impersonation.

---

# 15. QLT-SEC-007 — Service principal authority is bounded

Automation, Integrations, scheduled jobs, and system processes use explicit bounded principal/scope rather than an unrestricted “system bypass” by default.

---

# 16. Authorization

Backend Application/resource authorization is final for protected reads and writes.

Frontend visibility is UX.

RLS is defense in depth.

---

# 17. QLT-SEC-008 — Authorization is server enforced

Do not use:

- hidden button;
- disabled menu;
- client role;
- route guard

as the security boundary.

---

# 18. Query authorization

Protected operations include:

```text
get
list
search
export
analytics/drilldown
realtime subscribe
download
```

not only mutations.

---

# 19. QLT-SEC-009 — Unauthorized data is filtered before exposure

Avoid:

```text
fetch everything
→ materialize sensitive rows
→ filter in frontend/in-memory
```

when server/query-layer enforcement is feasible.

---

# 20. Resource existence

Unauthorized errors should avoid leaking resource existence where policy requires indistinguishability.

Operators can retain correlation diagnostics internally.

---

# 21. QLT-SEC-010 — Sensitive errors disclose minimum necessary detail

Untrusted responses do not expose:

- raw stack;
- SQL;
- connection string;
- token claims;
- provider secret;
- permission internals;
- hidden resource metadata.

---

# 22. Tenant isolation

Tenant isolation is required across:

```text
HTTP
Application
DB/RLS
cache
search
messages
background jobs
realtime
analytics
integrations
frontend state
```

---

# 23. QLT-SEC-011 — Tenant scope is explicit at every relevant boundary

Do not rely on:

- globally unique-looking IDs;
- process-local current tenant;
- frontend-selected Workspace alone.

---

# 24. Tenant derivation

Tenant/resource scope should be derived from trusted authoritative relationships where possible.

Client-provided Account/Workspace values are validated against resource ownership and principal access.

---

# 25. QLT-SEC-012 — Client cannot select another tenant by parameter substitution

Security tests include foreign Account/Workspace/resource identifiers in otherwise valid requests.

---

# 26. RLS

RLS complements Application authorization and should fail closed when tenant context is absent/incorrect according to architecture.

---

# 27. QLT-SEC-013 — RLS proof uses PostgreSQL-realistic infrastructure

SQLite or an in-memory fake cannot certify PostgreSQL RLS semantics.

---

# 28. Background scope

Consumers/jobs that touch tenant data establish scope explicitly before persistence/cache/search operations.

---

# 29. QLT-SEC-014 — Background execution is not trusted global tenant context

A message/job carries/resolves the required tenant/resource identity.

Worker process identity does not authorize cross-tenant access.

---

# 30. Realtime scope

Subscriptions/channels/groups are protected resources.

Permission may change while a socket remains connected.

---

# 31. QLT-SEC-015 — Realtime authorization can be revoked mid-session

After membership/share/policy revocation:

- future private events stop;
- stale subscriptions are removed/revalidated;
- client converges to safe state.

---

# 32. Cache isolation

Protected cache keys include enough scope to avoid cross-user/tenant/resource reuse.

Permission-sensitive cache also reflects authority version/invalidation.

---

# 33. QLT-SEC-016 — Cache key includes security-relevant identity

A key such as:

```text
resource:{id}
```

is insufficient if content differs by tenant/principal/permission.

---

# 34. Cache invalidation

Security changes that may invalidate cached data/decisions include:

- membership;
- permission;
- share-link status;
- entitlement where protected;
- resource lifecycle;
- user/session security.

---

# 35. Secrets

Secrets include:

```text
password/credential material
JWT/signing keys
API keys
OAuth access/refresh tokens
webhook signing secrets
DB passwords
provider credentials
payment secrets
MFA seeds
```

---

# 36. QLT-SEC-017 — Secrets never enter source, docs, logs, events, or frontend bundles

Use approved secret injection/storage.

Examples and fixtures use unmistakably fake non-sensitive values.

Do not commit realistic active-looking secret material to “test the scanner”.

---

# 37. Secret references

Domain/product state can retain safe:

- credential ID;
- hash/verifier;
- provider reference;
- last4/fingerprint;
- expiry/status metadata.

Raw reusable secret stays outside ordinary state.

---

# 38. QLT-SEC-018 — Logs redact secret-bearing fields structurally

Do not depend only on developers remembering not to interpolate one particular token.

Use safe logging models/redaction where risk warrants it.

---

# 39. Configuration

Non-trivial configuration defines:

```text
owner
type
allowed values
default/effective behavior
startup/runtime validation
failure mode
```

---

# 40. QLT-SEC-019 — Invalid security configuration fails safe

Missing/invalid tenant, auth, provider, encryption, CORS/CSRF, or security-critical config must not silently fall back to permissive behavior.

---

# 41. Feature flags

Flags have rollout purpose, owner, environment/default, telemetry/decision criteria, and removal condition.

Both paths preserve security invariants.

---

# 42. QLT-SEC-020 — Feature flag does not become security bypass

A rollout flag must not create an unreviewed path that skips:

- authorization;
- tenant isolation;
- validation;
- audit;
- secret handling.

---

# 43. CSRF

Cookie/session-based state-changing browser requests require the approved CSRF architecture.

API authentication mode and host behavior determine the exact mechanism.

---

# 44. QLT-SEC-021 — CSRF protection is tested at the real host/request boundary

Do not certify CSRF through a pure Domain test.

Test valid/invalid/missing origin/token behavior according to accepted host architecture.

---

# 45. CORS

CORS is browser policy, not authorization.

Allowed origins/methods/credentials are deployment/config controlled.

---

# 46. QLT-SEC-022 — CORS is not an API security boundary

A non-browser caller is not constrained by browser CORS enforcement.

Server authz remains required.

---

# 47. Rate limiting

Rate limiting protects abuse/resource exhaustion according to accepted architecture.

Rate-limit identity/scope should fit the threat:

```text
IP
principal
Account/Workspace
credential
endpoint class
provider
```

---

# 48. QLT-SEC-023 — Rate limiting fails predictably, not globally

One abusive tenant/credential should not unnecessarily deny unrelated tenants when a narrower safe scope is available.

---

# 49. Abuse versus business quota

Security rate limit and Billing product quota are different concepts.

Do not merge:

```text
requests/minute abuse protection
```

with:

```text
monthly Automation executions entitlement
```

---

# 50. Webhooks

Inbound provider callbacks are hostile external boundaries even when expected.

---

# 51. QLT-SEC-024 — Webhook authenticity precedes business processing

Canonical sequence:

```text
raw request/body
→ bounds
→ signature/timestamp/replay validation
→ verified Connection
→ tenant mapping
→ schema/business validation
→ async processing
```

---

# 52. Replay

Provider event/delivery identity is scoped and deduplicated.

Timestamp windows/nonces may supplement provider event identity.

---

# 53. QLT-SEC-025 — Webhook replay cannot duplicate business effect

Repeated valid signed delivery produces one logical effect where provider semantics identify one logical event.

---

# 54. Provider payload

Authenticated provider payload is still untrusted business input.

Validate types, IDs, URLs, resource scope, content, and target constraints.

---

# 55. SSRF

Outbound URLs from:

- user input;
- webhook payload;
- integration config;
- import/embed;
- callbacks

need explicit destination policy.

---

# 56. QLT-SEC-026 — Server-side outbound destinations are constrained

Where arbitrary remote URL fetch is not a product requirement, use allow-listed schemes/hosts/ports/path policy.

Block local/link-local/cloud-metadata/internal-network destinations as appropriate.

Redirects are revalidated.

---

# 57. DNS/rebinding

If remote URL fetching exists, threat design should consider hostname resolution/re-resolution and private-address transitions according to implementation.

---

# 58. Open redirects

User/provider-supplied return URLs must be validated against approved destinations.

---

# 59. QLT-SEC-027 — Redirect target is not trusted because it came from OAuth state or query string

State protects request correlation; redirect authorization still has its own policy.

---

# 60. File upload

File boundaries validate:

```text
authorization
size
declared type
actual content signature where material
filename/path
storage identity
malware/content policy where required
download disposition
```

---

# 61. QLT-SEC-028 — Filename is display metadata, not filesystem path authority

Never concatenate untrusted filename into arbitrary server filesystem paths.

Use generated object identity/storage abstraction.

---

# 62. File content

MIME/type headers are untrusted hints.

For risky formats, inspect/match actual content as appropriate.

---

# 63. QLT-SEC-029 — Uploaded active content is rendered safely

HTML/SVG/office/archive/media handling follows explicit sandbox/sanitization/download policy.

Do not blindly inline user-provided active content.

---

# 64. Archive/decompression

If compressed archives are accepted, enforce:

- entry count;
- uncompressed size;
- recursion;
- path traversal;
- resource limits.

---

# 65. Downloads

Download authorization is re-evaluated.

Possession of object key or stale URL does not permanently grant access.

---

# 66. QLT-SEC-030 — Signed download URLs are scoped and bounded

Use appropriate expiry/scope and avoid exposing durable provider credentials.

---

# 67. Rich content / XSS

Comments/Documents/provider-imported rich content must use an approved safe content model/sanitization and destination encoding.

---

# 68. QLT-SEC-031 — “Frontend will escape it” is not the content-security contract

The supported rich-content format and sanitization responsibility are explicit.

---

# 69. Formula/automation scripting

User-configurable formulas/actions must not become arbitrary server code execution.

---

# 70. QLT-SEC-032 — User-defined execution is allow-listed or sandboxed by explicit design

Current Automation action/formula semantics use typed known capabilities.

A future scripting feature requires separate sandbox/security architecture.

---

# 71. Mass assignment

API binding must not allow clients to set internal fields merely because DTO/entity property exists.

Use explicit request contracts.

---

# 72. QLT-SEC-033 — Internal security/lifecycle fields are not client-writable by reflection convenience

Examples:

```text
TenantId
OwnerId
CreatedBy
PermissionLevel
Status
Version
provider secret reference
```

change only through approved operations.

---

# 73. Object-level authorization

IDs supplied in URL/body/filter must be authorized at resource scope.

This includes child resources.

---

# 74. QLT-SEC-034 — Parent authorization does not imply arbitrary child/linked-resource access

Examples:

```text
Page access
≠ private embedded Board access

Workspace membership
≠ every private resource

Comment target access
≠ every linked target
```

---

# 75. Search/export/analytics

Bulk discovery surfaces need special leakage review.

Aggregation can still reveal private information.

---

# 76. QLT-SEC-035 — Bulk surfaces preserve security semantics

Search, export, analytics, reporting, and admin bulk endpoints must not bypass per-scope/field security because they are “read models”.

---

# 77. Data minimization

Contracts/events/logs/projections include only data needed for their purpose.

Avoid broad serialization of aggregates/users/provider payloads.

---

# 78. QLT-SEC-036 — Sensitive data does not propagate “just in case”

A downstream consumer requiring one ID should not receive full profile/token/resource payload.

---

# 79. Events/messages

Tenant-scoped facts carry required scope.

Public/integration events avoid reusable secrets and overbroad PII.

---

# 80. QLT-SEC-037 — Message authenticity/scope is not inferred from event name

Consumer validates/resolves message identity, tenant/resource scope, producer contract, and idempotency as appropriate.

---

# 81. Poison messages

Quarantine identity must be narrow enough not to suppress unrelated valid messages.

---

# 82. Provider operations

External provider effects use least-privilege provider scopes and stable connection/principal identity.

---

# 83. QLT-SEC-038 — Provider OAuth scope is least privilege

Request only capabilities required by approved product behavior.

New provider scope is a security-impacting change and may require re-consent.

---

# 84. Provider outcome uncertainty

Unknown outcome is reconciled, not blindly retried if duplicate external effect is possible.

This is correctness and abuse/security quality.

---

# 85. Payment data

Billing does not store raw card secrets/PAN/CVC.

Use provider tokenization and safe references.

---

# 86. QLT-SEC-039 — Financial administration receives stronger authorization/review

Changing Subscription, payment method, invoices/export, or billing ownership is separate from consuming entitled features.

---

# 87. Public sharing

Share links/public forms/public Pages are explicit capabilities.

They grant the minimum designed action/resource scope.

---

# 88. QLT-SEC-040 — Public capability is non-transitive by default

Sharing one resource does not automatically expose:

- linked resources;
- Workspace enumeration;
- embedded private content;
- member directory.

---

# 89. Share-token lifecycle

Share token:

- high entropy;
- safe storage/verifier;
- expiry where applicable;
- revocation;
- cache/realtime convergence.

---

# 90. QLT-SEC-041 — Revoked public capability stops working despite stale client/cache

Security invalidation must reach cached decisions/subscriptions within the designed contract.

---

# 91. Dependency security

Current backend CI performs transitive package vulnerability scanning and fails when vulnerable packages are reported.

That is one security signal, not the whole dependency policy.

---

# 92. QLT-SEC-042 — Known vulnerability is assessed, not ignored by habit

Review:

```text
affected dependency/version
reachability/exposure
severity
available fix
compensating control
upgrade risk
exception expiry
```

Do not suppress globally without explicit decision/exception.

---

# 93. Dependency provenance

New critical dependencies should be evaluated for:

- maintenance;
- release/update health;
- transitive risk;
- license;
- security history;
- supply-chain trust;
- necessity.

---

# 94. QLT-SEC-043 — Dependency addition has security cost

Do not add a high-privilege/parser/crypto/network dependency solely to save trivial local implementation when risk/maintenance cost outweighs value.

---

# 95. Lockfiles

Committed lockfiles and reproducible install/restore reduce supply-chain ambiguity.

CI should use locked/frozen modes when that is the repository contract.

---

# 96. Build artifacts

Build/release pipeline should not inject secret values into frontend/public artifacts.

---

# 97. QLT-SEC-044 — Public frontend environment variables are public

Variables compiled into web/mobile client bundles MUST be safe for any user to inspect.

A `PUBLIC_`/`VITE_` prefix does not protect a secret.

---

# 98. Logging

Security logs should support diagnosis while minimizing:

- secrets;
- raw tokens;
- full request bodies;
- PII;
- payment/provider data.

---

# 99. QLT-SEC-045 — Correlation replaces sensitive dump logging

Use correlation/logical operation/resource-safe metadata rather than logging entire confidential payload to debug distributed failures.

---

# 100. Audit

Governance Audit is append-oriented evidence for selected security/admin actions.

It remains distinct from user Activity and operational logs.

---

# 101. QLT-SEC-046 — Security-sensitive administrative change is auditable when policy requires

Examples:

- role/permission grant;
- owner change;
- share-link lifecycle;
- SSO/SCIM config;
- session/API-token revocation;
- Account/Workspace deletion;
- Billing admin change.

---

# 102. Audit integrity

Audit should not expose raw secret before/after values.

Record safe changed-field or action metadata.

---

# 103. Privacy

Security does not justify collecting unlimited personal data.

Data minimization, retention, and access remain explicit.

---

# 104. QLT-SEC-047 — Diagnostic convenience does not override privacy boundary

Do not retain raw provider/request payload indefinitely because it might help future debugging.

---

# 105. Error handling

Fail closed on security decisions when required facts are missing/ambiguous.

Do not hide operational failure from operators.

---

# 106. QLT-SEC-048 — Fail closed to clients, fail observable to operators

Safe external error + internal correlation/telemetry is preferable to either:

- verbose secret-leaking client error;
- silent generic failure with no diagnostic path.

---

# 107. Threat modeling

Threat review is required when a change creates or changes a trust boundary.

Examples:

```text
new public endpoint
new authentication flow
new share capability
new provider/webhook
new file parser/upload
new privileged background principal
new cross-tenant analytics/admin flow
new secret
new scripting/execution capability
```

---

# 108. QLT-SEC-049 — Threat review follows data/control flow

Identify:

```text
assets
actors
entry points
trust boundaries
scope/tenant
privilege
storage
external calls
failure/replay
logging
recovery
```

Avoid checklist-only threat modeling detached from actual architecture.

---

# 109. Abuse cases

For critical workflow include malicious cases, not only accidental failure.

Examples:

- guess resource ID;
- replay invitation/share token;
- spam public form;
- forge Workspace ID;
- upload active content;
- redirect to attacker URL;
- webhook replay;
- privilege escalation through role assignment.

---

# 110. QLT-SEC-050 — Security tests include negative and adversarial scenarios

Happy path does not certify a security boundary.

---

# 111. Security testing layers

Use the closest meaningful evidence:

```text
pure security/value rule
→ unit/Domain

authorization orchestration
→ Application

RLS
→ PostgreSQL integration

CSRF/rate limit
→ API/host integration

webhook signature/replay
→ raw HTTP/provider fixture

file handling
→ upload/download integration

realtime revocation
→ server/client integration
```

---

# 112. Authentication tests

Cover relevant:

```text
invalid credentials
expired/revoked session
refresh replay
MFA state
OAuth collision/state
API token scope/revoke
```

---

# 113. Authorization tests

Cover:

```text
no membership
wrong tenant
wrong resource
insufficient action
guest
revoked share
stale cache
protected list/search/export
```

---

# 114. Tenant tests

At least two real tenant datasets are required for tenant-isolation proof.

Include wrong/missing tenant context for RLS where architecture expects fail-closed.

---

# 115. Provider tests

Use realistic signed fixtures without live secrets.

Cover:

```text
valid
invalid signature
expired timestamp
replay
oversized/malformed
wrong tenant mapping
unsupported provider version
unknown external outcome
```

---

# 116. QLT-SEC-051 — Security test fixture is obviously non-secret

Test keys/tokens must be unmistakably synthetic and documented as test fixtures.

Do not weaken secret scanning globally to accommodate them.

---

# 117. Security regression gate

A recurring high-severity class should become executable gate when deterministic enough.

Examples:

- forbidden dependency;
- Domain secret exposure;
- SQLite/RLS substitution;
- mobile unsafe dependency;
- public contract secret field.

---

# 118. Current source/CI alignment

Current backend security architecture states:

```text
authentication at API
Application authorization authoritative
RLS as Infrastructure defense in depth
tenant context through background Platform work
permission-sensitive cache scoping
secrets outside Domain/events/log/client
Audit separate from Activity
```

Current backend CI includes:

```text
vulnerability scan
architecture tests
RLS tests
API/integration security-sensitive suites
```

These are current evidence, not the entirety of this standard.

---

# 119. Security review checklist

```text
[ ] trust boundary identified
[ ] external input bounded/validated
[ ] principal authentication path known
[ ] resource authorization server-side
[ ] tenant scope explicit
[ ] negative cross-tenant case tested
[ ] RLS/cache/realtime/background scope reviewed
[ ] secret handling safe
[ ] config fails safe
[ ] replay/idempotency considered
[ ] SSRF/redirect/file risks considered
[ ] error/log data minimized
[ ] dependency/security impact reviewed
[ ] audit/privacy/retention considered
[ ] recovery/revocation behavior clear
```

---

# 120. Webhook checklist

```text
[ ] raw body preserved where verification requires
[ ] body/header size bounded
[ ] signature checked
[ ] timestamp/replay checked
[ ] Connection/provider identity resolved
[ ] tenant derived from trusted mapping
[ ] event/delivery identity deduplicated
[ ] payload schema validated
[ ] target authorization/service principal bounded
[ ] poison/unknown outcome modeled
[ ] logs redact secret/payload
```

---

# 121. File boundary checklist

```text
[ ] uploader authorized
[ ] target scope known
[ ] size bounded
[ ] filename treated as metadata
[ ] content/type validated
[ ] active content policy
[ ] object key generated
[ ] download reauthorized
[ ] signed URL expiry
[ ] cleanup/retention
[ ] malware/archive policy if needed
```

---

# 122. Secret checklist

```text
[ ] value not in repository
[ ] not in docs/example as realistic active credential
[ ] not in frontend bundle
[ ] not in Domain/public event
[ ] safe storage/injection
[ ] rotation/revocation
[ ] logging redaction
[ ] test fixture synthetic
```

---

# 123. Public-sharing checklist

```text
[ ] explicit resource/action capability
[ ] high-entropy token/verifier
[ ] expiry/status
[ ] revocation
[ ] non-transitive linked-resource behavior
[ ] tenant/resource isolation
[ ] abuse/rate limiting
[ ] realtime/cache invalidation
```

---

# 124. Stop conditions

Stop rather than merge if:

- security relies on hidden frontend controls;
- tenant scope is trusted from client/provider payload without validation;
- a protected list/search/export filters only after exposure;
- new background/service principal has unrestricted system authority;
- raw reusable secret enters source/docs/log/event/client;
- security-critical config falls back permissively;
- webhook business processing occurs before authenticity/replay validation;
- outbound arbitrary URL has no SSRF policy;
- uploaded active content is served unsafely;
- public sharing becomes transitive accidentally;
- provider/Billing timeout is blindly retried with duplicate effect risk;
- security dependency warning is broadly suppressed without assessment;
- critical security change has no negative/adversarial evidence.

---

# 125. Related canonical owners

```text
docs/quality/engineering-quality-standard.md
docs/quality/testing-strategy.md
docs/quality/accessibility-standard.md
docs/product/contexts/identity.md
docs/product/contexts/accounts.md
docs/product/contexts/workspaces.md
docs/product/contexts/governance.md
docs/product/contexts/integrations.md
docs/product/contexts/billing.md
docs/architecture/contract-boundaries.md
docs/architecture/data-ownership-and-consistency.md
backend/docs/architecture/security-tenancy-authorization.md
```

---

# 126. Final security rule

For every security-sensitive change, answer:

```text
What asset/trust boundary changed?
What input/principal is untrusted?
Which tenant/resource scope applies?
Where is authentication proven?
Where is authorization enforced?
What defense-in-depth mechanism exists?
What secret/provider/file/URL risk exists?
What replay/concurrency/cache/realtime risk exists?
What does failure disclose?
What negative/adversarial test proves the boundary?
How is revocation/recovery observed?
```

The target is:

> **security that fails closed at trust boundaries, remains tenant-safe across every execution path, keeps secrets and external inputs constrained, and is proven by adversarial evidence rather than UI assumptions or optimistic configuration.**

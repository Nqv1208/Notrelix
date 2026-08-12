---
document_id: INFRA-ENVIRONMENT-MODEL
document_type: infrastructure-standard
status: active
owner: infrastructure
applies_to:
  - local
  - ci
  - staging
  - production
  - configuration
  - secrets
  - feature-flags
  - environment-data
evidence:
  - README.md
  - .env.example
  - .gitignore
  - docker-compose.dev.yml
  - docker-compose.yml
  - docker-compose.staging.yml
  - docker-compose.prod.yml
  - docs/delivery/local-development.md
  - docs/delivery/release-and-rollout.md
  - docs/quality/security-quality-standard.md
  - docs/operations/observability.md
review_on:
  - environment-model-change
  - configuration-contract-change
  - secret-delivery-change
  - environment-isolation-change
  - provider-credential-change
  - feature-flag-runtime-change
  - staging-or-production-topology-change
---

# Environment Model

> **Environments differ by credentials, endpoints, capacity, exposure, and operational policy—not by silent product semantics.**
>
> The same product and architecture contracts apply everywhere. Environment-specific configuration supplies runtime values without becoming a second source of business truth.

This document is the canonical repository-level owner for environment identity, environment isolation, configuration delivery, secret delivery, feature/config compatibility, and environment parity.

It does **not** own application configuration type definitions, deployment topology, release sequencing, or provider-specific secret-manager implementation.

Those belong to backend/frontend runtime docs, `deployment-runtime.md`, Delivery, and concrete infrastructure automation.

---

# 1. Environment classes

Notrelix may operate through environments such as:

```text
local development
CI/test
staging/preproduction
production
```

Additional environments MAY exist when justified.

The canonical rule is their **purpose and isolation**, not a fixed environment count.

---

# 2. INFRA-ENV-001 — Environment does not redefine product semantics

A Board, Workspace, permission, Subscription, event, or API operation has the same semantic meaning across environments.

Do not use environment checks to create hidden business rules such as:

```text
if Production:
    enforce permission
else:
    allow
```

---

# 3. Legitimate environment differences

Expected differences can include:

```text
credentials
DNS/endpoints
resource capacity
replica/count/topology
feature rollout exposure
observability exporters
provider test/live accounts
retention
logging verbosity
TLS/certificate
public host names
```

---

# 4. INFRA-ENV-002 — Runtime difference is explicit configuration or deployment state

Do not hide environment behavior in:

- machine hostname;
- developer username;
- source branch name;
- directory path;
- undocumented convention.

---

# 5. Environment identity

Runtime knows its environment through one explicit deployment/runtime configuration source.

Application code may use framework environment concepts where needed, but product decisions should prefer typed feature/capability configuration over scattered environment-name checks.

---

# 6. INFRA-ENV-003 — Environment name is not feature architecture

Avoid:

```text
if env == "staging":
    enable_new_model()
```

for durable product behavior.

Use governed feature/config rollout when behavior itself is being staged.

---

# 7. Local

Local exists for:

```text
development
focused integration
debugging
migration rehearsal
tenant/security testing
```

It should be reproducible from repository evidence.

---

# 8. INFRA-ENV-004 — Local defaults are unmistakably non-production

Local configuration SHOULD make accidental production access difficult through:

```text
local hostnames
local database names
synthetic credentials
separate provider test credentials
explicit env files
```

---

# 9. CI/test

CI exists to provide clean deterministic evidence.

It SHOULD use ephemeral or isolated state rather than long-lived shared developer data.

---

# 10. INFRA-ENV-005 — CI does not depend on personal environment state

Required CI MUST NOT require:

```text
developer .env
personal cloud credentials
interactive login
local Docker volume
private IDE config
```

unless explicitly provisioned as CI secret/resource.

---

# 11. Staging/preproduction

Staging exists to reproduce production-relevant protocol and deployment behavior closely enough to validate material changes.

It is not automatically a miniature production copy of every scale dimension.

---

# 12. INFRA-ENV-006 — Staging parity follows protected property

Examples:

```text
PostgreSQL/RLS semantics
→ staging/integration must use PostgreSQL-equivalent behavior

rolling deployment compatibility
→ staging deployment should reproduce old/new overlap when tested

provider protocol
→ use realistic test/sandbox provider where available
```

Scale can differ unless capacity itself is under test.

---

# 13. Production

Production owns real customer/runtime state and requires strongest controls.

Production configuration, credentials, storage, and provider identities are isolated from lower environments.

---

# 14. INFRA-ENV-007 — Production state is isolated

Lower environments MUST NOT reuse production:

```text
database
Redis namespace
message broker/vhost/topic
object-storage namespace
OAuth client secret
payment provider account
email/provider credentials
```

unless an explicit security-controlled process is approved.

---

# 15. Data isolation

Environment isolation applies to both infrastructure and application-visible identifiers.

A staging Workspace is not a “test Workspace inside production” by default.

---

# 16. INFRA-ENV-008 — Testing does not use production tenant as ordinary sandbox

Synthetic or isolated non-production tenant state is preferred.

Production testing requires explicit operational/security design.

---

# 17. Production-derived data

If production-derived data is ever copied to a lower environment, apply:

```text
approval
classification
minimization
sanitization/anonymization
secret removal
retention
access control
```

---

# 18. INFRA-ENV-009 — Production-derived data remains sensitive after copying

Changing the database name to `staging` does not remove privacy/security obligations.

---

# 19. Configuration model

Configuration is runtime input required to compose the application with environment-specific values.

It is not an alternative business database.

---

# 20. INFRA-ENV-010 — Configuration is typed and validated

A non-trivial required setting has:

```text
owner
type
allowed range/shape
default if safe
requiredness
failure behavior
```

Validation SHOULD happen at startup/composition where practical.

---

# 21. Missing configuration

For security/data/provider-critical configuration:

```text
missing
invalid
ambiguous
```

must fail startup or the affected capability safely.

---

# 22. INFRA-ENV-011 — Critical config never falls back permissively

Examples:

```text
missing JWT key
→ fail

missing production CORS origin
→ fail

missing DB connection
→ fail

invalid provider secret
→ affected provider unavailable
```

not “allow everything”.

---

# 23. Defaults

Defaults are appropriate only when the value is genuinely safe and semantically stable.

Production-sensitive credentials/hosts SHOULD generally be required explicitly.

---

# 24. INFRA-ENV-012 — Development convenience default does not become production default accidentally

A local default such as:

```text
dev-only JWT secret
local RabbitMQ password
localhost callback
```

must not silently apply to production.

---

# 25. Configuration source

Configuration MAY come from:

```text
environment variables
deployment platform config
secret references
mounted configuration
managed parameter store
```

according to infrastructure choice.

Application semantics should not depend on which delivery mechanism supplied the same typed value.

---

# 26. INFRA-ENV-013 — Config delivery mechanism is replaceable

Moving from env vars to a managed configuration service should not require product/domain rewrites.

---

# 27. Config names

Config keys are deployment contracts.

Rename/removal can break old/new binaries during rolling deployment.

---

# 28. INFRA-ENV-014 — Config rename is compatibility work

Use as needed:

```text
old + new key overlap
fallback with explicit precedence
coordinated deployment
deprecation
```

until old runtime versions are gone.

---

# 29. Config precedence

If several configuration sources exist, precedence MUST be deterministic and documented by runtime/tooling.

Avoid environment where one hidden source silently overrides another.

---

# 30. INFRA-ENV-015 — Effective config is inspectable without exposing secrets

Operators/developers should be able to diagnose:

```text
which non-secret value/source is active
which required setting is missing
which feature/config version is effective
```

without dumping credentials.

---

# 31. Secrets

Secrets are reusable sensitive values such as:

```text
database passwords
JWT/signing keys
OAuth client secrets
provider API keys
SMTP credentials
webhook secrets
payment provider secrets
```

---

# 32. INFRA-ENV-016 — Secret is delivered through an approved secret channel

Do not commit or bake production secrets into:

```text
source
Dockerfile
container layer
frontend bundle
generated contract
README
example env
CI artifact
```

---

# 33. Environment variables and secrets

Environment variables MAY be one injection mechanism, but “stored in env var” does not by itself establish secure secret lifecycle.

Infrastructure must still define:

```text
source
access
rotation
audit
scope
```

---

# 34. INFRA-ENV-017 — Secret lifecycle is independent of source deployment

Secrets SHOULD be rotatable/revocable without requiring product source-code change.

---

# 35. Least privilege

Credential scope follows runtime process responsibility.

Do not give every process one shared admin credential.

---

# 36. INFRA-ENV-018 — Runtime identity has minimum required privilege

Examples:

```text
steady-state API credential
migration/admin credential
backup/restore credential
provider connection credential
```

can be distinct when supported.

---

# 37. Database credentials

Application runtime credential SHOULD have only permissions required by steady-state application operation.

Migration/bootstrap MAY require additional privileges.

---

# 38. INFRA-ENV-019 — Migration privilege is not assumed as runtime privilege

A process does not need unrestricted DDL/admin rights merely because migrations exist in the repository.

---

# 39. Provider credentials

Provider credentials are isolated by environment and provider account where possible.

Staging sandbox provider state MUST NOT accidentally affect production users.

---

# 40. INFRA-ENV-020 — Provider environment boundary is explicit

A lower environment cannot silently switch to live payment/email/OAuth/provider account because one endpoint value was omitted.

---

# 41. Frontend public configuration

Web/mobile bundles execute on untrusted client devices.

Values compiled into client code are public.

---

# 42. INFRA-ENV-021 — Public frontend configuration contains no server secret

Prefixes such as:

```text
NEXT_PUBLIC_
VITE_
```

mean client-visible configuration, not secret storage.

---

# 43. Frontend API endpoint

Client-visible API/gateway URLs are deployment configuration.

They may vary by environment while the API contract remains the same.

---

# 44. Build-time versus runtime config

Frontend frameworks can bake values at build time.

Backend commonly consumes values at runtime.

The release model MUST know which values require rebuild versus runtime injection.

---

# 45. INFRA-ENV-022 — Config timing is explicit

For every material config determine:

```text
build-time
startup-time
runtime-refreshable
```

Do not assume changing an environment variable changes an already-built static bundle.

---

# 46. Configuration drift

Drift is when effective environment configuration differs unintentionally from declared desired state.

---

# 47. INFRA-ENV-023 — Environment config drift is detectable

Critical configuration SHOULD be represented through versioned deployment/IaC/config automation where possible.

Emergency manual changes are reconciled afterward.

---

# 48. Manual config

Emergency operational config changes may be necessary.

They MUST be:

```text
authorized
recorded
observable
reversible where possible
reconciled into desired state
```

---

# 49. INFRA-ENV-024 — Production-only undocumented config is debt, not architecture

After incident/temporary change, either:

- encode it in canonical deployment config;
- revert it.

---

# 50. Feature flags

Release flags control exposure.

Product entitlements and authorization remain separate authorities.

---

# 51. INFRA-ENV-025 — Environment config and feature flag are distinct

Environment configuration answers:

```text
how this environment is connected/composed
```

Release flag answers:

```text
who/what is exposed to a staged behavior
```

Do not use environment names as a permanent feature flag system.

---

# 52. Flag defaults

A flag MUST have a safe default for each environment/deployment stage.

Production default cannot be inferred from staging behavior.

---

# 53. INFRA-ENV-026 — Flag state is explicit during release

Operators can determine:

```text
current state
cohort/scope
owner
version
```

without reverse-engineering code.

---

# 54. Flag security

Both flag paths preserve auth/tenant guarantees.

---

# 55. Environment-specific URLs

OAuth/webhook/callback URLs vary by environment.

They must map to the correct environment/provider registration.

---

# 56. INFRA-ENV-027 — Callback URLs cannot cross environments accidentally

A staging OAuth callback or webhook MUST NOT route into production, and vice versa, unless explicitly designed.

---

# 57. DNS/TLS

Environment hostnames/certificates are infrastructure-owned.

TLS termination location may vary by topology, but transport/security requirements remain constant.

---

# 58. INFRA-ENV-028 — TLS/environment routing does not change application authorization

A trusted internal network does not remove normal resource authorization/tenant requirements.

---

# 59. Network reachability

Lower environments SHOULD expose only what their purpose requires.

Production data services SHOULD NOT be public simply for developer convenience.

---

# 60. INFRA-ENV-029 — Direct data-service exposure is environment-specific and deliberate

For example, local Docker may publish PostgreSQL/Redis ports.

Production SHOULD use network isolation/private connectivity according to deployment topology.

---

# 61. Seed data

Seed data is environment-specific operational bootstrap/testing support.

It is not product authority.

---

# 62. INFRA-ENV-030 — Production seed behavior is explicit

Development/staging seed conveniences MUST NOT silently reset/populate production data.

Current production Compose evidence disables seeding; this is the expected safety direction.

---

# 63. Migration-on-startup

Automatic migration can be convenient in local development.

Production migration execution needs explicit deployment policy because startup race/privilege/lock risks differ.

---

# 64. INFRA-ENV-031 — Migration execution strategy is environment-aware but semantically identical

The same intended migration chain applies.

Only the **execution mechanism/timing/credential** may differ.

---

# 65. Local parity

Local does not need identical scale.

It should reproduce the protocols that matter to the change.

---

# 66. INFRA-ENV-032 — Parity follows semantics, not visual similarity

Examples:

```text
PostgreSQL behavior
RLS
Redis protocol
RabbitMQ semantics
HTTP/realtime
object/provider contract fixtures
```

matter more than matching production CPU/memory count locally.

---

# 67. Test doubles

Mocks/fakes may replace external services for focused tests.

They must not be cited as proof for semantics they do not reproduce.

---

# 68. INFRA-ENV-033 — Dependency substitution has declared fidelity

A fake email provider may prove orchestration.

It cannot prove:

- provider signature;
- OAuth;
- real rate-limit;
- network/TLS;
- provider idempotency.

---

# 69. Staging promotion

Staging should receive release artifacts/configuration compatible with production promotion model where possible.

Do not use entirely different manual deployment semantics and claim rollout proof.

---

# 70. INFRA-ENV-034 — Environment promotion preserves artifact identity

Promote or rebuild with proven provenance according to deployment policy.

Do not change source revision between evidence and promotion silently.

---

# 71. Production access

Administrative access to production configuration/secrets/data is restricted and auditable according to security/operations policy.

---

# 72. INFRA-ENV-035 — Local developer access is not production runtime privilege

Do not reuse personal/developer credentials as steady-state application/service identity.

---

# 73. Break-glass

If a break-glass path is introduced, it requires explicit security/operations design.

Do not use “temporary admin env var” as an informal backdoor.

---

# 74. Configuration logging

Application may log safe effective configuration metadata such as feature enabled/state/version.

Never log secret values.

---

# 75. INFRA-ENV-036 — Config diagnostics are redacted structurally

A generic object dump of all environment variables is not an acceptable production diagnostic strategy.

---

# 76. Validation at startup

Invalid required config SHOULD fail before accepting workload when possible.

For optional providers, the affected capability may become unavailable while unrelated capability remains healthy.

---

# 77. INFRA-ENV-037 — Optional provider misconfiguration is isolated when architecture permits

A missing optional integration key should not necessarily take down Work Management, but it must not produce fake provider success.

---

# 78. Environment readiness

Environment readiness includes:

```text
required resources
required config
required secret access
schema/migrations
network/routes
health
```

---

# 79. INFRA-ENV-038 — “Container started” is not environment readiness

The runtime must be correctly composed for its intended role.

---

# 80. Environment drift inventory

When repository prose, `.env.example`, Compose, CI, and deployed config disagree, classify and fix drift.

Do not copy the disagreement into another handbook.

---

# 81. INFRA-ENV-039 — Executable environment config is evidence; canonical policy decides intent

Example:

```text
Compose currently supplies X
```

is current evidence.

It does not automatically make X a durable architecture choice.

---

# 82. Current repository evidence

Current repository shows:

```text
.env.example
→ local/staging template

docker-compose.dev.yml
→ self-contained development stack

docker-compose.yml
→ shared staging/production dependency base

docker-compose.staging.yml
→ staging application/gateway overlay

docker-compose.prod.yml
→ production application/gateway overlay
```

The exact Compose topology is current executable evidence.

---

# 83. Current environment distinctions

Current evidence includes:

```text
Development:
  migrate/startup + seed conveniences
  published dependency ports
  SDK/live-reload containers

Staging:
  ASPNETCORE_ENVIRONMENT=Staging
  controlled seeding
  hardened container capabilities

Production:
  ASPNETCORE_ENVIRONMENT=Production
  seeding disabled
  required production config
  read-only runtime filesystem where configured
```

These are current implementation details, not eternal requirements for every future orchestrator.

---

# 84. INFRA-ENV-040 — Infrastructure provider/orchestrator may change without product rewrite

Moving from Compose to another deployment platform MAY change:

```text
secret injection
networking
scaling
resource declaration
health wiring
```

while preserving product/backend/frontend contracts.

---

# 85. Environment checklist

```text
[ ] purpose
[ ] isolated state
[ ] isolated credentials
[ ] explicit runtime identity
[ ] typed config
[ ] secret channel
[ ] network exposure
[ ] provider accounts
[ ] migration/seed behavior
[ ] logging/observability
[ ] rollout/flag state
[ ] recovery ownership
```

---

# 86. Configuration checklist

```text
[ ] owner
[ ] key/name
[ ] type
[ ] required/default
[ ] safe failure
[ ] build/startup/runtime timing
[ ] secret/public classification
[ ] old/new compatibility
[ ] effective-state diagnostics
```

---

# 87. Secret checklist

```text
[ ] not committed
[ ] environment-isolated
[ ] least privilege
[ ] rotation path
[ ] redaction/log safety
[ ] client bundle exclusion
[ ] CI/build-layer exclusion
[ ] revocation/recovery
```

---

# 88. New environment checklist

```text
[ ] documented purpose
[ ] state/resource isolation
[ ] config/secret provisioning
[ ] provider isolation
[ ] TLS/DNS/routing
[ ] migration strategy
[ ] observability
[ ] release promotion path
[ ] recovery/deletion lifecycle
```

---

# 89. Stop conditions

Stop rather than normalize if:

- environment name changes product business rules;
- production shares lower-environment credentials/state casually;
- a required security config has permissive fallback;
- secrets are baked into image/frontend/generated artifacts;
- production uses local seed/reset behavior unintentionally;
- staging provider callback can mutate production;
- config rename breaks rolling old binaries with no overlap strategy;
- local/staging substitution is cited as proof for protocol it does not reproduce;
- manual production config has no reconciliation path;
- environment prose conflicts with executable config and the drift is duplicated rather than fixed.

---

# 90. Related canonical owners

```text
docs/infrastructure/deployment-runtime.md
docs/infrastructure/containerization-and-local-services.md
docs/delivery/local-development.md
docs/delivery/release-and-rollout.md
docs/quality/security-quality-standard.md
docs/operations/observability.md
docs/operations/recovery-and-data-safety.md
backend/docs/operations/configuration-and-runtime.md
frontend/docs/
```

---

# 91. Final environment rule

For every environment/configuration change, answer:

```text
What changes between environments and why?
Does product meaning remain identical?
Which resource/state/credential is isolated?
Is the setting typed, validated, and safe on missing/invalid input?
Is it secret or public?
When is it bound—build, startup, or runtime?
Can old/new binaries coexist with the config?
Can the value be rotated without source change?
Does lower environment reproduce the protocol property being tested?
Can operators inspect effective non-secret state without exposing credentials?
```

The target is:

> **environment-specific composition without environment-specific business truth: isolated state and credentials, typed fail-safe configuration, replaceable secret delivery, and enough parity to prove production-relevant protocols without pretending every environment must be identical.**

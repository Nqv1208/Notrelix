---
document_id: BE-CONFIGURATION-RUNTIME
document_type: operations
status: active
owner: backend-runtime-operations
applies_to:
  - backend
  - backend-runtime
  - backend-configuration
  - backend-secrets
  - backend-startup
  - backend-dependencies
  - backend-local-development
evidence:
  - backend/src/Notrelix.API/Program.cs
  - backend/src/Notrelix.API/appsettings.json
  - backend/src/Notrelix.API/appsettings.Development.json
  - backend/src/Notrelix.API/appsettings.Staging.json
  - backend/src/Notrelix.API/appsettings.Production.json
  - backend/src/Notrelix.Infrastructure/DependencyInjection.cs
  - .env.example
  - docker-compose.dev.yml
  - docker-compose.yml
  - docker-compose.staging.yml
  - docker-compose.prod.yml
  - Makefile
review_on:
  - backend-configuration-source-change
  - runtime-dependency-change
  - startup-validation-change
  - secret-delivery-change
  - environment-model-change
  - runtime-command-change
  - provider-configuration-change
---

# Configuration and Runtime

> **Runtime configuration selects and binds technical mechanisms. It must not silently redefine product semantics, security rules, tenant ownership, or business invariants.**
>
> Backend configuration is environment-aware, typed, validated, secret-safe, and fail-safe. The host owns binding/composition. Application/Domain consume explicit contracts/facts rather than ambient environment variables.

This document is the canonical backend operational owner for:

- backend configuration precedence and ownership;
- `appsettings*.json` usage;
- environment variables and secret delivery;
- typed options and startup validation;
- local/dev/staging/production runtime differences;
- backend runtime dependencies;
- startup/database command modes;
- data-protection runtime behavior;
- provider/runtime enablement;
- messaging transport selection;
- safe local reset/runtime commands;
- configuration drift and operational diagnostics.

It does not define product feature semantics, schema migration policy, deployment-provider topology, or frontend public configuration.

---

# 1. Runtime configuration model

Backend runtime configuration conceptually flows through:

```text
committed non-secret defaults
        ↓
environment-specific appsettings
        ↓
environment variables / secret delivery
        ↓
typed options
        ↓
validation
        ↓
Infrastructure/API composition
        ↓
runtime dependency behavior
```

The exact .NET configuration precedence remains executable host behavior.

The architecture requirement is that higher-precedence runtime sources can safely override lower-precedence non-secret defaults without turning documentation into a competing config source.

---

# 2. BE-OPS-CFG-001 — Configuration is technical input, not product authority

Environment/runtime config MAY select:

```text
database endpoint
Redis endpoint
messaging transport
provider credential
provider endpoint
logging level
health thresholds
HTTPS behavior
runtime capacity
```

It MUST NOT silently decide:

```text
Workspace owner semantics
Billing entitlement meaning
resource permission semantics
Domain lifecycle
```

unless the product explicitly models a configurable product fact through the owning context.

---

# 3. Base appsettings

`appsettings.json` provides shared/default host configuration.

Current source includes sections such as:

```text
Database
Rls
DataProtection
SeedData
JwtSettings
ForwardedHeaders
Cors
HealthChecks
RateLimiting
HttpsRedirection
N8n
Email
Smtp
Messaging
OAuth
Security
```

This is current evidence, not a permanent complete section registry.

---

# 4. BE-OPS-CFG-002 — Base defaults are safe, not production credentials

Committed base configuration MAY contain:

```text
disabled feature/provider default
safe local host
empty required secret placeholder
non-secret timeout/port/default
```

It MUST NOT contain a usable production secret or credential.

---

# 5. Environment-specific appsettings

Current backend has:

```text
appsettings.Development.json
appsettings.Staging.json
appsettings.Production.json
```

These override technical runtime behavior.

Do not treat one environment file as the universal architecture.

---

# 6. BE-OPS-CFG-003 — Environment difference changes mechanism, not product meaning

Allowed differences:

```text
logging verbosity
dependency endpoint
transport
HTTPS
seed enablement
migration startup policy
provider credential
capacity
```

Forbidden implicit difference:

```text
Production has stronger authorization rule than Development
Development ignores tenant ownership
Staging uses a different product lifecycle
```

Core product semantics should remain environment-independent.

---

# 7. Current environment evidence

Current source currently expresses differences including:

```text
Development:
- Database.MigrateOnStartup = true
- Rls.Enabled = true
- Rls.SetSessionContext = true
- SeedData enabled/run-on-startup
- Messaging RabbitMQ
- HTTPS redirect disabled

Staging:
- MigrateOnStartup = false
- Seed enabled but not run-on-startup
- HTTPS redirect enabled
- RabbitMQ transport

Production:
- MigrateOnStartup = false
- Seed disabled
- HTTPS redirect enabled
- RabbitMQ transport
```

These values are current executable evidence.

They are not timeless architecture constants.

---

# 8. BE-OPS-CFG-004 — Current environment values are source facts, not duplicated policy

If source values change legitimately:

```text
update runtime source
update this document only when the operational model changes
```

Do not maintain every numeric/default value here as a second configuration file.

---

# 9. Environment variables

Repository `.env.example` is the current environment-variable template.

It includes categories for:

```text
PostgreSQL
Redis
backend/frontend ports
JWT
email/SMTP
OAuth
RabbitMQ
n8n
pgAdmin
```

A template documents names/intent, not secret values.

---

# 10. BE-OPS-CFG-005 — Example environment file contains placeholders only

`.env.example` MUST NOT contain:

```text
live production key
real customer/provider secret
real DB password
usable OAuth secret
```

Synthetic local examples must be unmistakably non-production.

---

# 11. Local environment file

Current repository workflow commonly copies:

```text
.env.example
→ .env.dev
```

for local Compose usage.

Local developer values are disposable runtime state.

Do not commit `.env.dev`.

---

# 12. BE-OPS-CFG-006 — Local env file is not architecture

A local `.env.dev` can differ by developer machine.

Do not encode product decisions in it.

Do not cite one developer's local values as canonical docs.

---

# 13. Secret sources

Secrets belong in:

```text
environment variables
secret store
orchestrator secret mechanism
approved encrypted credential store
```

depending on runtime architecture.

They do not belong in:

```text
source
docs
committed appsettings
container layers
generated frontend assets
logs
```

---

# 14. BE-OPS-CFG-007 — Secret delivery and source deployment have separate lifecycles

A secret SHOULD be rotatable without changing product source code.

A source release SHOULD not require embedding a secret into the artifact.

---

# 15. Secret names versus values

A committed configuration/template can name:

```text
JwtSettings:SecretKey
OAuth:Providers:Google:ClientSecret
Messaging:RabbitMQ:Password
```

without storing the real value.

Documentation may discuss the key contract.

Never publish the real credential value.

---

# 16. BE-OPS-CFG-008 — Secret value never becomes diagnostic output

Resolved configuration/logging/health output MUST redact:

```text
password
token
API key
OAuth secret
JWT signing material
webhook secret
```

Do not print full `IConfiguration` for debugging in production.

---

# 17. Typed options

Infrastructure/API SHOULD bind related configuration into typed options.

Benefits:

```text
known type
known required fields
validation
testability
clear ownership
```

Do not read arbitrary string keys throughout feature code.

---

# 18. BE-OPS-CFG-009 — Runtime options are validated near composition

Required production values SHOULD fail during startup/config validation before serving traffic.

Do not delay a missing critical secret until the first customer request discovers it.

---

# 19. ValidateOnStart

Current API uses `.ValidateOnStart()` for Data Protection options and a custom environment-aware validator.

This is an example of the required pattern.

Not every options class must use identical implementation.

---

# 20. BE-OPS-CFG-010 — Critical option invalidity fails safe

Examples:

```text
invalid signing key
invalid database configuration
invalid RLS mode
invalid credentialed CORS
missing required provider secret for enabled provider
```

should not silently fall back to an insecure/partial mode.

---

# 21. Disabled capability

Optional technical capability can be disabled explicitly.

Examples:

```text
Email.Enabled = false
OAuth provider disabled
N8n disabled
optional messaging profile not started locally
```

When disabled, the owning use case must have an explicit supported degradation/path.

---

# 22. BE-OPS-CFG-011 — Disabled provider is explicit state

Do not treat:

```text
missing key
provider initialization failed
```

as the same as intentionally disabled.

Configuration error and deliberate disabled state require different diagnostics.

---

# 23. Runtime composition

Current Infrastructure composition root delegates registration to capability-specific registrations:

```text
persistence
messaging
background jobs
caching
auth/security
governance
storage
email
realtime
integrations
billing
operations
observability
OAuth
```

This keeps the root thin.

---

# 24. BE-OPS-CFG-012 — Composition binds implementations, not product policy

Dependency registration MAY select:

```text
Redis cache adapter
RabbitMQ transport
provider client
PostgreSQL implementation
```

It MUST NOT become the only place a product permission/lifecycle rule exists.

---

# 25. Runtime dependency classification

Each dependency should have an authority class.

Examples:

```text
PostgreSQL
→ authoritative relational persistence

Redis
→ derived cache/coordination where architecture defines

RabbitMQ
→ transport mechanism

provider API
→ external authority for provider-owned state

object storage
→ binary/object durability mechanism
```

This affects degradation/recovery.

---

# 26. BE-OPS-CFG-013 — Dependency outage behavior follows authority class

Do not respond to a cache outage the same way as authoritative DB outage.

Do not fabricate provider success when provider outcome is unknown.

---

# 27. Database configuration

Database runtime config includes:

```text
connection string
migration/startup policy
pool/provider options
```

Product data semantics remain outside configuration.

---

# 28. BE-OPS-CFG-014 — Database connection secret is runtime-only

Never copy production connection strings into:

```text
docs
test snapshots
OpenAPI
frontend environment
```

Use placeholders/references.

---

# 29. MigrateOnStartup

Current source supports:

```text
Database:MigrateOnStartup
```

and current environment profiles differ.

Startup migration is a runtime mechanism choice.

Migration correctness remains owned by `migrations-and-data-change.md`.

---

# 30. BE-OPS-CFG-015 — MigrateOnStartup does not authorize unsafe production DDL

Even if the switch exists:

```text
production migration privilege
rollout compatibility
migration review
```

must still satisfy migration/deployment policy.

---

# 31. Database command mode

Current API `Program.cs` supports early database command execution through:

```text
RunDatabaseCommandsAsync(args)
```

before normal host startup.

Repository Makefile currently uses commands such as:

```text
--migrate
--seed
--rls-apply
```

This gives explicit operational invocation paths.

---

# 32. BE-OPS-CFG-016 — Administrative database command exits instead of serving traffic

A migration/seed/RLS command invocation should perform its declared operation and terminate, rather than accidentally starting the full API host afterward.

---

# 33. Local database commands

Current repository exposes:

```text
make db-migrate
make db-seed
make db-init
make db-rls
make db-psql
```

These are local/operator conveniences.

The Makefile is the executable command authority.

---

# 34. BE-OPS-CFG-017 — Docs route to commands; Makefile defines exact implementation

Do not duplicate long shell command implementations here.

If exact command changes, update Makefile/help and route accordingly.

---

# 35. Seed configuration

Current seed options include concepts such as:

```text
Enabled
RunOnStartup
Profile
ResetBeforeSeed
```

Seed is data bootstrap/development support.

It is not a product data authority.

---

# 36. BE-OPS-CFG-018 — Seed and reset are environment-safe

Production MUST NOT run destructive development reset/seed silently.

Seed/reset operations SHOULD be explicit and guarded.

---

# 37. Current seed evidence

Current Production config disables seed.

Current Development enables seed and startup seeding.

Staging currently enables the seed capability but does not run it on startup.

Treat this as current environment evidence only.

---

# 38. Seed system scope

Current initialiser temporarily sets system tenant context while seeding, then clears it.

This is a privileged operational mechanism.

---

# 39. BE-OPS-CFG-019 — Seed privileged scope is bounded and always cleared

A failed seed MUST NOT leave a global/system tenant context active in a reused scope/process.

Use `try/finally` or equivalent lifecycle.

---

# 40. Reset semantics

Current local reset can delete/recreate local volumes and run:

```text
restore
migration
seed
RLS apply
startup
```

This is explicitly destructive local behavior.

---

# 41. BE-OPS-CFG-020 — Reset command cannot ambiguously target production

A destructive reset command MUST make environment/context unambiguous.

Do not create a generic `reset-db` that can operate on production from ambient credentials without an explicit guard.

---

# 42. RLS runtime configuration

Current RLS options include concepts such as:

```text
Enabled
ApplyPoliciesOnStartup
SetSessionContext
```

These control persistence security mechanics.

The security architecture determines required production posture.

---

# 43. BE-OPS-CFG-021 — Security config cannot silently disable required RLS

If a protected environment requires RLS, invalid/missing config SHOULD fail safe rather than run broad tenant queries.

Development/test exceptions must be explicit and scoped.

---

# 44. ApplyPoliciesOnStartup

Applying policies during normal startup can require elevated DB privilege.

This may be useful in controlled environments.

It is not automatically suitable for production.

---

# 45. BE-OPS-CFG-022 — Policy-application privilege is separable from normal runtime

Prefer an explicit migration/RLS operation identity where production least privilege requires it.

Normal API runtime should not need broad schema-policy administration merely because policy scripts exist.

---

# 46. Messaging transport

Current base/default and environment-specific config can select transport such as:

```text
InMemory
RabbitMQ
```

Transport choice is a technical runtime mechanism.

Platform owns logical delivery semantics.

---

# 47. BE-OPS-CFG-023 — Transport selection cannot change message correctness

Switching:

```text
InMemory ↔ RabbitMQ
```

MUST preserve the intended:

```text
logical identity
idempotency
ordering
retry/poison
contract
```

for any environment claiming equivalent behavior.

---

# 48. Local messaging

Local development may use optional or enabled RabbitMQ depending on current Compose/profile/config.

A developer can use a lower-fidelity transport only when the property under development does not require real broker behavior.

---

# 49. BE-OPS-CFG-024 — Lower-fidelity runtime is declared

If local/test uses InMemory transport:

```text
do not claim RabbitMQ-specific behavior proven
```

Use production-like integration for broker-specific claims.

---

# 50. RabbitMQ options

Current config exposes technical settings including:

```text
host
port
vhost
username/password
SSL
prefetch
retry/circuit-breaker values
```

Exact numeric values are runtime tuning, not canonical architecture constants.

---

# 51. BE-OPS-CFG-025 — Runtime tuning is environment/workload-driven

Do not hard-code universal:

```text
prefetch
retry count
pool size
circuit threshold
```

into architecture docs.

Validate/tune with capacity/reliability evidence.

---

# 52. Redis configuration

Redis endpoint/password/port are runtime settings.

Redis is not product truth.

If Redis is unavailable, behavior follows cache/rate-limit/coordination authority and failure mode.

---

# 53. BE-OPS-CFG-026 — Redis outage cannot broaden security

Security-sensitive cache/rate-limit failure modes MUST follow approved security architecture.

Do not default to broad authorization because Redis is unavailable.

---

# 54. Email/SMTP

Current backend can configure email/provider mechanics.

Provider enablement and credential belong to Infrastructure runtime.

Product decides when an email/notification should exist.

---

# 55. BE-OPS-CFG-027 — Provider enablement does not move recipient semantics into config

Do not encode a list of privileged business recipients/permission rules in provider options unless product explicitly owns that configurable fact.

---

# 56. OAuth provider config

OAuth runtime config can include:

```text
Enabled
ClientId
ClientSecret
AuthorizationEndpoint
TokenEndpoint
UserInfoEndpoint
JwksUri
RedirectUri
Scopes
```

Provider-specific technical details remain at the Infrastructure/API edge.

---

# 57. BE-OPS-CFG-028 — OAuth redirect/provider config is environment-scoped

Development/staging/production callbacks MUST NOT accidentally cross environments.

A staging callback MUST NOT authenticate into production state.

---

# 58. Frontend redirect URLs

Backend can need frontend success/failure URLs for OAuth flows.

These are environment routing config, not product authorization.

Validate allowed targets.

---

# 59. BE-OPS-CFG-029 — Redirect target is trusted configuration

Do not accept arbitrary client-provided post-auth redirect URL without a safe allowlist/contract.

Avoid open redirect.

---

# 60. JWT settings

Current config includes:

```text
Audience
Issuer
SecretKey
expiration settings
```

Signing material is secret.

Issuer/audience define trust context.

---

# 61. BE-OPS-CFG-030 — JWT signing config fails safe

An enabled JWT authentication host MUST NOT silently start with:

```text
empty/weak placeholder production secret
wrong issuer/audience
```

Startup validation/security tests should detect this.

---

# 62. Data Protection

Current API binds Data Protection options and can persist keys to a configured filesystem path.

Data Protection key continuity affects:

```text
cookies/tokens/protected payloads
```

depending on use.

---

# 63. BE-OPS-CFG-031 — Data Protection key lifecycle matches deployment topology

If multiple instances/restarts must decrypt the same protected data, key persistence/shared availability must support that contract.

Ephemeral instance-local keys are only valid when losing decryptability is acceptable.

---

# 64. Data Protection path

A filesystem path is runtime mechanism.

In containers, ensure:

```text
writable path
persistence if required
least privilege
secret/key protection
```

Do not assume container filesystem is durable.

---

# 65. BE-OPS-CFG-032 — Container filesystem is not durable business/key storage by default

Use explicit mounted/persistent/key service when durability is required.

---

# 66. Forwarded headers

Current config supports:

```text
TrustAllInDevelopment
ForwardLimit
KnownProxies
KnownNetworks
```

Proxy trust affects:

```text
scheme
source IP
rate limiting
security audit
```

---

# 67. BE-OPS-CFG-033 — Forwarded headers are trusted only from configured proxy boundary

Do not trust arbitrary internet-supplied `X-Forwarded-*` values.

Development convenience MUST NOT leak into production trust policy.

---

# 68. CORS

Allowed origins are environment-specific runtime configuration.

They must match the actual frontend deployment origins requiring browser access.

CORS is not resource authorization.

---

# 69. BE-OPS-CFG-034 — Credentialed origin config has no permissive production fallback

Missing production origins SHOULD fail or deny rather than switch to wildcard.

---

# 70. HTTPS redirection/HSTS

Current environment profiles differ in HTTPS-redirection behavior.

Host security can rely on proxy/TLS topology.

Do not infer actual public TLS termination from one local setting.

---

# 71. BE-OPS-CFG-035 — TLS/forwarding behavior is resolved with deployment topology

Avoid redirect loops or false secure-scheme assumptions behind reverse proxy.

Production proxy/trusted-forwarding config must be coherent.

---

# 72. Health thresholds

Current config contains health thresholds for outbox/dead-letter age/count.

Numeric thresholds are operational values.

Do not freeze them in architecture.

---

# 73. BE-OPS-CFG-036 — Health threshold has an operational owner and meaning

A threshold SHOULD correspond to:

```text
degraded
unhealthy
actionable backlog
```

not arbitrary numbers copied across environments.

---

# 74. OpenAPI export mode

Current `Program.cs` supports OpenAPI export mode and injects safe dummy connection/security values to build endpoint metadata without normal runtime dependency connections.

This is a tooling mode, not a production runtime mode.

---

# 75. BE-OPS-CFG-037 — Export/tooling mode cannot be reachable as normal production service behavior

Tooling configuration MAY bypass normal runtime connections only to generate deterministic artifacts and terminate.

It MUST NOT serve customer traffic with dummy credentials/config.

---

# 76. DI validation in export mode

Current export mode disables some service-provider validation because endpoint metadata generation does not require normal runtime resolution.

This exception is scoped to export mode.

---

# 77. BE-OPS-CFG-038 — Tooling exception does not become runtime precedent

Do not disable DI scope/build validation in normal host merely because export mode does.

---

# 78. Startup middleware/runtime order

Program startup currently orders host middleware explicitly:

```text
forwarded headers
exception handling
correlation
CSRF
security headers
pre-auth rate limiting
HSTS/dev Swagger
CORS
HTTPS redirect
authentication
request context
authenticated rate limit
security audit
authorization
endpoints
```

Exact implementation is source evidence.

Dependency/trust ordering is architectural.

---

# 79. BE-OPS-CFG-039 — Runtime middleware ordering follows prerequisite state

Examples:

```text
authenticated rate limit
→ after authentication

request execution context
→ after principal

endpoint execution
→ after host security pipeline
```

Do not reorder without security/API review.

---

# 80. Startup dependency readiness

A service should not report ready before dependencies/config required for its workload are safe.

Do not block liveness on every optional provider.

Distinguish readiness from liveness per operations architecture.

---

# 81. BE-OPS-CFG-040 — Optional dependency failure does not automatically fail whole process

If a capability is safely disabled/degraded:

```text
service can remain ready for unaffected workloads
```

provided health/degradation is observable and product semantics remain honest.

---

# 82. Runtime shutdown

Current host sets a bounded shutdown timeout.

Shutdown must allow:

```text
request drain
consumer stop
lease/claim cleanup
flush/commit boundaries
```

as mechanisms require.

Exact timeout is tuning.

---

# 83. BE-OPS-CFG-041 — Graceful shutdown preserves delivery correctness

Do not acknowledge/abandon in-flight durable work in a way that makes it unrecoverable.

Crash/restart remains a supported failure mode.

---

# 84. Local Compose

Current local runtime uses `docker-compose.dev.yml`.

It provides development topology for dependencies and app hosts.

Local Compose reproduces protocols and workflows.

It does not claim production scale/topology.

---

# 85. BE-OPS-CFG-042 — Local runtime prioritizes protocol fidelity, not production topology symmetry

Use real PostgreSQL/Redis/RabbitMQ where their semantics matter.

Do not reproduce every production infrastructure component merely for symmetry.

---

# 86. Staging/production Compose

Current repository has staging/production Compose overlays.

They are current deployment evidence.

Repository Infrastructure docs own the canonical deployment-runtime model.

Backend docs only describe how backend configuration participates.

---

# 87. BE-OPS-CFG-043 — Backend runtime does not assume one deployment provider

Configuration contracts should survive migration from Compose to another orchestrator/provider.

Provider/orchestrator-specific secret injection belongs to deployment/IaC evidence.

---

# 88. Configuration drift

Potential drift surfaces:

```text
appsettings
env template
Compose env mapping
typed options
startup validation
provider registration
tests
```

A rename/change must update all required producers/consumers.

---

# 89. BE-OPS-CFG-044 — Configuration key rename is compatibility work

During overlapping old/new deployment, support or coordinate:

```text
old env key
new env key
rollout order
deprecation/removal
```

when the runtime platform can contain mixed revisions.

Do not rename a production secret key casually.

---

# 90. Unknown configuration

Unused/stale configuration should be removed after consumer inventory.

Do not keep dead production secret names indefinitely because no one knows if they matter.

Removal should be evidence-based.

---

# 91. BE-OPS-CFG-045 — Configuration has one logical owner

For every setting know:

```text
who binds it
who validates it
what capability consumes it
whether secret
binding time
failure behavior
```

Avoid duplicate differently-named settings controlling the same mechanism.

---

# 92. Build-time versus startup/runtime config

Backend server configuration is normally startup/runtime bound.

Do not confuse it with frontend public build-time variables.

If a backend value becomes build-time, justify because it reduces deployment flexibility.

---

# 93. BE-OPS-CFG-046 — Secret is never compile-time baked into backend artifact by default

Runtime injection is preferred so artifacts can be promoted unchanged across environments.

---

# 94. Environment promotion

A release artifact should be promotable with environment-specific runtime config/secrets.

Product behavior remains the same.

Deployment rollout details belong to repository Delivery/Infrastructure.

---

# 95. BE-OPS-CFG-047 — Promotion changes environment binding, not source identity

Do not rebuild application source differently for staging/prod merely to embed secrets.

Use the same tested artifact where release architecture supports it.

---

# 96. Configuration diagnostics

Operators need to diagnose effective non-secret config.

Expose safe categories/state such as:

```text
provider enabled/disabled
transport type
RLS enabled
migration-on-startup mode
environment name
release identity
```

without secret values.

---

# 97. BE-OPS-CFG-048 — Effective configuration is inspectable safely

If a runtime failure depends on configuration, operators should not need to dump all environment variables to discover the mode.

Use safe structured startup/health diagnostics.

---

# 98. Logging level

Logging verbosity varies by environment.

Do not enable sensitive EF/provider detail in production merely to match Development diagnostics.

Use temporary controlled escalation if needed.

---

# 99. BE-OPS-CFG-049 — Diagnostic verbosity does not leak secrets/private data

Higher log level must still honor redaction/privacy rules.

Debug mode is not authorization to print credentials.

---

# 100. Runtime config tests

Tests should cover critical option validation and mode-specific registration where failure could create:

```text
security bypass
wrong provider
data loss
partial startup
```

Do not test every simple scalar default with low value.

---

# 101. BE-OPS-CFG-050 — Test config boundary at the point of risk

Examples:

```text
missing production JWT secret fails
invalid RLS option fails
provider disabled selects supported fallback
RabbitMQ config maps correctly
```

Use targeted tests.

---

# 102. Configuration change classification

Typical:

```text
non-secret logging tweak
→ C7 low risk

new provider/runtime option
→ C7

JWT/CORS/RLS/CSRF/rate-limit
→ C6 + C7

database/migration runtime policy
→ C4 + C7

secret key rename
→ C7 + rollout compatibility
```

---

# 103. Operational review checklist

```text
[ ] setting owner
[ ] secret/non-secret
[ ] source/default
[ ] environment override
[ ] typed binding
[ ] startup validation
[ ] binding time
[ ] failure mode
[ ] local/staging/prod behavior
[ ] rollout compatibility
[ ] safe diagnostics
[ ] tests
```

---

# 104. Provider config checklist

```text
[ ] enabled state
[ ] endpoint
[ ] credential source
[ ] timeout
[ ] retry/failure policy
[ ] callback/redirect
[ ] environment isolation
[ ] secret redaction
[ ] startup validation
```

---

# 105. Database runtime checklist

```text
[ ] connection secret
[ ] migration mode
[ ] RLS mode
[ ] seed mode
[ ] system privilege
[ ] pool/session assumptions
[ ] startup/readiness
[ ] explicit admin commands
```

---

# 106. Stop conditions

Stop runtime/config implementation if:

- product behavior is being changed through ambient environment instead of owning product contract;
- production secret would be committed;
- missing security config would fail open;
- provider missing credential is indistinguishable from intentionally disabled;
- local reset can ambiguously target production;
- RLS required environment can silently start disabled;
- migration/admin privilege is being given permanently to steady-state runtime without need;
- callback/redirect can cross environments unexpectedly;
- config rename has no mixed-version rollout plan;
- OpenAPI export/tooling dummy config could serve normal traffic;
- a config dump would expose secret values;
- development trust/defaults are being copied to production.

---

# 107. Executable evidence

Current primary evidence:

```text
backend/src/Notrelix.API/Program.cs
backend/src/Notrelix.API/appsettings*.json
backend/src/Notrelix.Infrastructure/DependencyInjection.cs
.env.example
docker-compose*.yml
Makefile
```

Useful local commands are defined by the Makefile, including:

```text
make dev-up
make dev-down
make db-migrate
make db-seed
make db-init
make db-rls
make be-build
make be-test
make config-dev
```

---

# 108. Related canonical owners

Backend architecture:

```text
../architecture/application-model.md
../architecture/infrastructure-and-data.md
../architecture/platform-and-messaging.md
../architecture/api-and-contracts.md
../architecture/security-tenancy-authorization.md
```

Repository:

```text
../../../docs/infrastructure/environment-model.md
../../../docs/infrastructure/deployment-runtime.md
../../../docs/infrastructure/containerization-and-local-services.md
../../../docs/delivery/local-development.md
../../../docs/operations/service-degradation.md
```

---

# 109. Non-responsibilities

This document does not define:

```text
schema/data migration semantics
exact production cloud provider
Kubernetes/Terraform manifests
frontend VITE/NEXT public config architecture
product feature flags/entitlement semantics
exact SLO/RPO/RTO values
```

Use the owning documents.

---

# 110. Final runtime rule

A healthy backend runtime can be stated as:

```text
promoted artifact
+
validated environment configuration
+
external secret delivery
+
explicit dependency selection
+
least-privilege runtime identity
        ↓
safe composition/startup
        ↓
honest readiness/degradation
        ↓
server-authoritative use cases
```

with:

```text
no committed production secret
no ambient product semantics
no permissive security fallback
no ambiguous destructive command
no accidental environment crossover
no hidden provider mode
no tooling dummy config serving traffic
```

The objective is a runtime whose technical variability is explicit and replaceable while the product/security/data contracts remain stable.

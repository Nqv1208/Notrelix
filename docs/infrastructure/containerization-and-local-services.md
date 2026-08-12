---
document_id: INFRA-CONTAINERIZATION-LOCAL-SERVICES
document_type: infrastructure-standard
status: active
owner: infrastructure
applies_to:
  - container-builds
  - container-images
  - docker-compose
  - local-services
  - local-networking
  - local-volumes
  - development-tooling
  - packaging-ci
evidence:
  - docs/engineering/07-infrastructure/02-container-builds.md
  - Dockerfile
  - .dockerignore
  - backend/Dockerfile
  - backend/.dockerignore
  - frontend/Dockerfile
  - frontend/Dockerfile.marketing
  - docker-compose.dev.yml
  - docker-compose.yml
  - docker-compose.staging.yml
  - docker-compose.prod.yml
  - Makefile
  - .github/workflows/be-ci.yml
  - .github/workflows/fe-packaging.yml
  - docs/infrastructure/environment-model.md
  - docs/infrastructure/deployment-runtime.md
  - docs/delivery/local-development.md
review_on:
  - dockerfile-change
  - build-context-change
  - base-image-change
  - compose-topology-change
  - local-service-change
  - container-hardening-change
  - packaging-ci-change
  - package-manager-or-sdk-change
---

# Containerization and Local Services

> **Containers package declared application artifacts and reproduce required runtime protocols; they do not become a second architecture, a secret-distribution shortcut, or an alternative source of product truth.**
>
> Local Compose exists to make development reproducible. Production containers exist to make release artifacts portable and constrained. The two may share protocols and build definitions without pretending that a writable SDK development container is equivalent to a hardened production runtime.

This document is the canonical repository-level owner for:

- container build invariants;
- Docker build contexts and ignore policy;
- multi-stage/runtime image composition;
- container identity and health;
- local Docker Compose service roles;
- local networks, volumes, and profiles;
- packaging verification;
- the boundary between development containers and production artifacts.

`environment-model.md` owns environment/configuration/secrets semantics.

`deployment-runtime.md` owns runtime topology/process/dependency authority.

`local-development.md` owns developer workflow commands.

This file owns the **container and local-service mechanism** that implements those contracts.

---

# 1. Container model

Canonical model:

```text
committed source
+ pinned/declared toolchain
+ locked dependencies
+ deterministic generation
+ bounded build context
        ↓
build stages
        ↓
runtime artifact/image
        ↓
environment config + secret injection
        ↓
runtime composition
```

---

# 2. INFRA-CTR-001 — Release container is reproducible from repository evidence

A release image SHOULD be buildable from:

```text
committed source
declared SDK/runtime
locked dependency manifests
declared build arguments
generated contract/source producers
versioned Dockerfile/build config
```

without developer-local state.

---

# 3. Reproducibility meaning

“Reproducible” means an engineer/CI can reconstruct the same intended application artifact from the same declared inputs.

It does not require byte-for-byte identical image hashes when upstream image metadata/build timestamps prevent that, unless the release process explicitly adopts stronger reproducible-build requirements.

---

# 4. INFRA-CTR-002 — Build inputs are explicit

Do not depend on:

- local `node_modules`;
- local NuGet cache contents as unique source;
- uncommitted generated files;
- private files outside build context;
- globally installed SDK not declared by repository.

Build caches MAY accelerate identical declared work.

---

# 5. Toolchain authority

Current repository declares:

```text
backend
→ .NET SDK/runtime 9.x family through global.json/project/container source

frontend
→ Node 22 + pnpm 10 family through package manifest/container source
```

Exact repository manifests remain executable authority.

---

# 6. INFRA-CTR-003 — Container toolchain and repository toolchain stay compatible

A Dockerfile MUST NOT silently compile with a materially different language/runtime/package-manager contract than local/CI manifests.

When upgrading:

```text
manifest
Dockerfile
CI
local tooling
runtime image
```

are reviewed together.

---

# 7. Dependency locking

Backend dependency versions are resolved through repository .NET manifests.

Frontend production builds use the committed pnpm lockfile with frozen-lock semantics.

---

# 8. INFRA-CTR-004 — Release dependency restore is locked

A production/package build MUST NOT silently rewrite dependency locks to make the image build.

If manifests and lockfile disagree:

```text
fix source manifests
regenerate lock intentionally
review
commit
```

---

# 9. Code generation

Generated contract/code artifacts follow their producer.

If generation participates in image build, drift MUST fail rather than be silently accepted.

---

# 10. INFRA-CTR-005 — Container build does not become hidden code generator authority

A generated file existing only inside an image is not sufficient repository contract evidence when consumers/CI require it committed or checked.

---

# 11. Build context

Docker build context is a security and reproducibility boundary.

Every file sent to the builder is potentially exposed to:

- local Docker daemon;
- remote BuildKit builder;
- cache;
- build metadata;
- accidental wildcard `COPY`.

---

# 12. INFRA-CTR-006 — Build context contains only required source/build inputs

Exclude as appropriate:

```text
.git
.env / .env.*
secrets
credentials
node_modules
bin/obj
coverage
Playwright reports
IDE state
local logs
cache/output
unrelated artifacts
```

---

# 13. `.dockerignore`

`.dockerignore` is a security/performance control, not cosmetic cleanup.

It SHOULD match the actual context used by each Dockerfile.

---

# 14. INFRA-CTR-007 — Ignore policy follows the build-context root

A safe:

```text
backend/.dockerignore
```

does not protect a Docker build whose context is repository root.

Each context root requires its own effective ignore rules.

---

# 15. Current context evidence

Current builds use at least two context shapes:

```text
backend image
→ context: backend/

frontend web/marketing images
→ context: repository root
  dockerfile: frontend/...
```

Therefore backend and frontend root-context ignore requirements differ.

---

# 16. Current source debt — root env files

Current repository-root `.dockerignore` excludes build/cache/test artifacts but does not currently exclude:

```text
.env
.env.*
secrets/
```

while staging/production frontend builds use repository root as context.

This is source debt against the container security contract.

---

# 17. INFRA-CTR-008 — Secret-bearing local files never enter a remote/root build context unnecessarily

Final synchronization SHOULD patch root `.dockerignore` to exclude secret-bearing environment/local credential paths compatible with required build inputs.

Do not rely only on “the Dockerfile currently does not COPY it”.

---

# 18. Git ignore versus Docker ignore

`.gitignore` does not guarantee Docker excludes a file from build context.

---

# 19. INFRA-CTR-009 — Git exclusion is not container-context exclusion

A local secret can be safely untracked by Git and still be sent to a Docker builder unless `.dockerignore` or equivalent context selection excludes it.

---

# 20. Build secrets

If a build genuinely needs a private credential, use a mechanism designed for non-persistent build secrets when supported.

---

# 21. INFRA-CTR-010 — Build secret does not persist in layer, cache, history, or log

Do not use:

```dockerfile
ARG PRIVATE_TOKEN=...
ENV PRIVATE_TOKEN=...
COPY .env.prod ...
```

as ordinary secret delivery into release image.

---

# 22. Frontend public build configuration

Frontend build variables can be compiled into public artifacts.

They MUST be treated as public.

---

# 23. INFRA-CTR-011 — Client build arguments are reviewed as public data

A Docker build argument used for:

```text
VITE_*
NEXT_PUBLIC_*
EXPO_PUBLIC_*
```

MUST NOT contain a server secret.

---

# 24. Multi-stage builds

Use multi-stage builds where it reduces final runtime surface and separates:

```text
SDK/compiler/dependency restore
```

from:

```text
runtime files
```

---

# 25. INFRA-CTR-012 — Runtime image contains runtime needs only

Do not leave in final image without reason:

```text
SDK/compiler
source tree
test projects
test reports
package-manager cache
development hot-reload tooling
local env files
build credentials
```

---

# 26. Current backend image

Current backend Dockerfile uses:

```text
dotnet/sdk:9.0 build stage
→ dotnet publish
→ aspnet:9.0 runtime stage
```

and runs as a dedicated `notrelix` non-root user.

This is aligned with the target pattern.

---

# 27. INFRA-CTR-013 — Backend runtime executes published output, not source checkout

Production image SHOULD run the published API artifact.

Development bind-mounted `dotnet watch` belongs to development topology.

---

# 28. Backend restore optimization

Current Dockerfile copies project/package metadata before source to improve layer caching.

This optimization is valid only if every project/manfiest required by restore is included.

---

# 29. INFRA-CTR-014 — Restore-layer optimization cannot omit dependency graph inputs

When project references/package manifests change, Dockerfile restore-copy inventory MUST change atomically.

A cache optimization that causes stale/partial restore is defective.

---

# 30. Build cache

BuildKit/GitHub caches MAY accelerate restore/build.

Cache key/input rules must invalidate on relevant source/manifest changes.

---

# 31. INFRA-CTR-015 — Cache hit is not proof of fresh generated/dependency state

CI still runs:

- codegen drift;
- restore;
- tests;
- build;

according to quality policy.

Do not use a cached image to bypass required changed-source evidence.

---

# 32. Backend final runtime tools

A minimal runtime image MAY include operational utilities only when required by health/diagnostics.

Current backend final image installs `wget` for Docker health check.

---

# 33. INFRA-CTR-016 — Runtime diagnostic utility has an explicit purpose

Do not install broad shell/network/admin toolsets in release image merely for convenience.

Prefer the smallest capability that satisfies health/operations.

---

# 34. Frontend web image

Current web Dockerfile:

```text
Node 22 builder
→ frozen pnpm install
→ Vite web build
→ nginx runtime serving static assets
```

---

# 35. INFRA-CTR-017 — Static web image contains built assets, not development workspace

Final web image SHOULD serve the immutable generated web output and runtime web-server config.

---

# 36. Frontend marketing image

Current marketing Dockerfile:

```text
Node 22 builder
→ frozen pnpm install
→ Next build
→ standalone Node runner
```

with a dedicated non-root `nextjs` user.

---

# 37. INFRA-CTR-018 — Marketing runtime carries only standalone/runtime assets

Do not ship entire frontend monorepo/node_modules tree when standalone output is the accepted runtime model.

---

# 38. Frontend build context breadth

Both frontend production Dockerfiles currently build from repository root because they reference paths prefixed with `frontend/`.

This is an implementation choice, not a requirement.

---

# 39. INFRA-CTR-019 — Build context is narrowed when practical

If frontend Dockerfiles can later build correctly from a narrower context, prefer smaller context because it improves:

```text
security
upload time
cache precision
reasoning
```

Do not refactor context merely for symmetry if it breaks required workspace inputs.

---

# 40. Base images

Base image tags are dependency inputs.

Current source uses families such as:

```text
mcr.microsoft.com/dotnet/sdk:9.0
mcr.microsoft.com/dotnet/aspnet:9.0
node:22-alpine
nginx:alpine / nginx:1.27-alpine
postgres:16-alpine
redis:7-alpine
rabbitmq:3.13-management-alpine
```

---

# 41. INFRA-CTR-020 — Base-image updates are dependency/security changes

Review:

```text
runtime compatibility
OS/package changes
CVEs
health tools
user/permission behavior
binary compatibility
image size
```

before promotion.

---

# 42. Image pinning

Canonical policy requires identifiable/reviewable image versions.

Exact tag-versus-digest pinning policy MAY evolve with supply-chain/release tooling.

---

# 43. INFRA-CTR-021 — Mutable broad tag is not sole release provenance

Production deployment MUST still identify:

```text
Notrelix artifact revision
base-image update decision
```

through build/release evidence.

---

# 44. Image identity

A release image SHOULD be tagged/annotated so operators can map it to exact source/build.

---

# 45. INFRA-CTR-022 — Image identity includes source/build provenance

Avoid production identity based solely on:

```text
notrelix/backend:latest
```

without immutable SHA/version/digest evidence.

---

# 46. OCI metadata

When packaging pipeline matures, OCI labels MAY carry:

```text
source repository
revision
version
created/build identity
```

This is a mechanism, not a requirement to choose a registry vendor.

---

# 47. Container user

Run as non-root where practical.

If the base/runtime requires a privileged master process or selected capabilities, compensate at orchestrator/runtime and document why.

---

# 48. INFRA-CTR-023 — Root privilege is minimized across image + deployment

Security is evaluated from the effective runtime:

```text
Dockerfile USER
capabilities
read-only filesystem
security options
writable mounts
```

not one line in isolation.

---

# 49. Web nginx nuance

Current web image does not declare a `USER` itself.

Production/staging Compose applies capability/security hardening at service level for selected processes/gateway, while the web image's standalone security posture depends on the nginx base image/runtime.

---

# 50. INFRA-CTR-024 — Artifact and orchestrator hardening are both part of effective container security

A packaging smoke test run outside production overlay MUST NOT assume overlay-only hardening is present.

---

# 51. Read-only filesystem

Production containers SHOULD use read-only root filesystem where compatible.

Writable paths should be explicit:

```text
/tmp
nginx cache/run
application temp
```

---

# 52. INFRA-CTR-025 — Writable container state is disposable unless declared durable mount

Do not store:

- customer data;
- uploaded files;
- durable jobs;
- secrets;

on container root filesystem.

---

# 53. Volumes

Volumes are classified by purpose:

```text
authoritative dependency data
derived/cache data
developer cache
tool data
temporary runtime
```

---

# 54. INFRA-CTR-026 — Volume purpose is explicit

Example current dev volumes:

```text
postgres_data
redis_data
rabbitmq_data
backend_nuget
backend_dataprotection
frontend_node_modules
pgadmin_data
```

These have different durability/security meanings.

---

# 55. Development volumes

Developer dependency caches can be deleted safely.

Database/message volumes contain local application state and are deleted by destructive reset commands intentionally.

---

# 56. INFRA-CTR-027 — Dev volume deletion is not modeled as production recovery

`docker compose down -v` is a local reset mechanism.

It is not a production data-management pattern.

---

# 57. Named volumes versus bind mounts

Development source uses bind mounts for live editing and named volumes for dependency/runtime state.

Production should not bind-mount source checkout as application runtime.

---

# 58. INFRA-CTR-028 — Source bind mount is development-only by default

A production artifact must carry its built runtime code with release identity.

---

# 59. Local Compose purpose

`docker-compose.dev.yml` is a self-contained local stack intended to reduce onboarding friction.

It currently includes:

```text
PostgreSQL
Redis
optional RabbitMQ
backend SDK/hot reload
marketing dev host
web dev host
nginx gateway
optional pgAdmin
```

---

# 60. INFRA-CTR-029 — Local Compose reproduces protocols, not production scale

It should preserve relevant:

```text
PostgreSQL
RLS/runtime DB behavior
Redis
broker protocol
HTTP/gateway
frontend host
```

without pretending to reproduce production resource counts/capacity.

---

# 61. PostgreSQL local service

Current development uses PostgreSQL 16 Alpine with persistent named volume and initialization scripts.

---

# 62. INFRA-CTR-030 — Local database uses the production database class

PostgreSQL-specific behavior such as:

```text
RLS
Npgsql
locking
migration
constraints
```

should not be replaced with SQLite as general local/integration parity.

---

# 63. Database initialization scripts

`infra/postgres/init` is mounted into PostgreSQL initialization.

Initialization scripts are infrastructure/bootstrap mechanisms.

EF migrations/Application bootstrap remain the application persistence evolution authority as defined elsewhere.

---

# 64. INFRA-CTR-031 — Container init scripts do not replace application migrations

Use init scripts only for infrastructure/bootstrap needs appropriate to a newly created database service.

Schema/business evolution remains reviewed migration work.

---

# 65. Redis local service

Current Redis uses:

```text
password
AOF
bounded maxmemory
LRU policy
named volume
```

in development/base Compose evidence.

---

# 66. INFRA-CTR-032 — Redis local durability does not make cache product truth

AOF/volume improve local/runtime behavior.

They do not transfer authoritative data ownership to Redis.

---

# 67. RabbitMQ local service

RabbitMQ is currently optional via:

```text
profile: messaging
```

and includes management UI in local/base image.

---

# 68. INFRA-CTR-033 — Messaging profile is opt-in for work that needs broker protocol

Pure Domain/frontend work should not require RabbitMQ merely because it exists.

Messaging/Platform integration work SHOULD enable realistic broker behavior as required by test strategy.

---

# 69. Optional profiles

Compose profiles keep auxiliary dependencies/tools out of the default stack.

Current profiles include:

```text
messaging
tools
```

---

# 70. INFRA-CTR-034 — Optional tool/profile does not become application dependency accidentally

pgAdmin or broker management UI are operational/developer tools.

Application code MUST NOT depend on their presence.

---

# 71. pgAdmin

Current local tools profile exposes pgAdmin for developer database inspection.

---

# 72. INFRA-CTR-035 — Admin tooling is local/restricted by default

Do not expose pgAdmin/RabbitMQ management publicly in production solely because local Compose publishes it.

---

# 73. Networks

Current development separates:

```text
frontend-network
backend-network
data-network
```

while base staging/prod uses internal backend network plus frontend network.

---

# 74. INFRA-CTR-036 — Local network boundaries model connectivity intent

Network separation documents:

```text
who needs frontend access?
who needs data access?
who bridges ingress to backend?
```

It does not replace application authorization.

---

# 75. Development port publication

Local Compose publishes ports to host for developer access.

This is intentionally broader than production private data-service exposure.

---

# 76. INFRA-CTR-037 — Published dev port is not production exposure precedent

Production network exposure follows `deployment-runtime.md` and executable infrastructure.

---

# 77. Gateway

Current local nginx bridges frontend/backend networks and provides one development gateway port.

---

# 78. INFRA-CTR-038 — Local gateway is composition convenience, not security bypass

Backend still performs authentication/authorization/tenant enforcement.

---

# 79. Health checks

Container health checks provide process/dependency evidence.

Current examples:

```text
PostgreSQL pg_isready
Redis ping
RabbitMQ diagnostics
backend /health/live
web HTTP root
```

---

# 80. INFRA-CTR-039 — Container health check is bounded and side-effect free

A health command SHOULD:

- terminate quickly;
- avoid customer mutation;
- avoid expensive full-system work;
- distinguish liveness/readiness appropriately.

---

# 81. Health utility availability

If health command needs `wget`, `curl`, or provider CLI, the runtime image must intentionally contain it or use an orchestrator-native mechanism.

---

# 82. INFRA-CTR-040 — Health command cannot depend on a tool omitted from final image

Container verification MUST run against the actual final stage.

---

# 83. Compose `depends_on`

Development Compose may wait for dependency health to improve startup ergonomics.

It does not guarantee the dependency stays healthy forever.

---

# 84. INFRA-CTR-041 — Startup ordering is not runtime resilience

Application still needs:

```text
timeouts
retry where safe
degradation
observability
recovery
```

after startup.

---

# 85. Development backend

Current dev backend:

```text
dotnet/sdk:9.0
bind-mounted backend source
NuGet cache volume
DataProtection volume
dotnet watch
migrate/seed enabled
```

This is intentionally different from release backend image.

---

# 86. INFRA-CTR-042 — Development SDK container is not release artifact

Do not validate production image security/size/startup using the dev SDK container alone.

---

# 87. Development frontend

Current dev web/marketing containers:

```text
node:22-alpine
bind-mounted frontend source
shared node_modules volume
pnpm install
dev servers/watch polling
```

---

# 88. INFRA-CTR-043 — Dev install convenience is not release dependency proof

Release/CI still uses frozen-lock and explicit quality gates.

A dev container successfully running `pnpm install` cannot certify lockfile synchronization.

---

# 89. Shared frontend `node_modules` volume

Current web and marketing dev services share the same named volume mounted at `/app/node_modules`.

This can reduce install duplication.

Its correctness depends on one coherent workspace dependency state.

---

# 90. INFRA-CTR-044 — Shared developer dependency volume is disposable cache

If it becomes inconsistent, recreate it.

Do not treat container volume contents as dependency source of truth.

---

# 91. Hot reload

Polling settings exist to make host-mounted development work across Docker/macOS environments.

These are development mechanics only.

---

# 92. Local reset

Current Makefile distinguishes:

```text
dev-down
→ stop

dev-clean
→ stop + delete dev volumes

dev-reset
→ delete volumes + restore + migrate + seed + RLS + start

dev-reset-full
→ additionally clear/force restore caches
```

---

# 93. INFRA-CTR-045 — Local reset reconstructs from declared sources

After destructive reset, repository manifests/migrations/seeds/bootstrap must be sufficient to recreate a working local stack.

Private tribal database state indicates onboarding debt.

---

# 94. Local reset safety

Environment file, Compose file, and local project identity SHOULD make destructive reset difficult to target at production.

---

# 95. INFRA-CTR-046 — Destructive Compose helpers are environment-scoped

A local `down -v` command MUST NOT be generalized into a production recovery command.

---

# 96. Container build verification

Three different proof levels:

```text
Dockerfile parses/builds
image starts
image serves intended runtime contract
```

These are not equivalent.

---

# 97. INFRA-CTR-047 — Successful image build is not startup proof

At minimum, a material image/build-definition change SHOULD be verified with an appropriate:

```text
container start
health check
HTTP/startup smoke
runtime composition smoke
```

depending on image role.

---

# 98. Backend CI current evidence

Current backend CI runs Docker build only after required:

```text
architecture
core
Platform
API/OpenAPI
integration
```

jobs succeed.

This correctly treats packaging as downstream evidence.

---

# 99. INFRA-CTR-048 — Image build cannot compensate for skipped tests

A Docker build succeeding does not prove:

- Domain invariants;
- RLS;
- idempotency;
- API compatibility;
- migration;
- realtime.

Those remain owned by their gates.

---

# 100. Frontend packaging current evidence

Current `fe-packaging.yml` builds:

```text
web Dockerfile
marketing Dockerfile
```

from repository-root context.

The job is named `Container smoke`, but current steps only execute `docker build`.

---

# 101. INFRA-CTR-049 — Packaging workflow names match actual evidence

Until the workflow starts/health-checks containers, treat current frontend packaging as:

```text
container build evidence
```

not runtime smoke evidence.

Final implementation SHOULD either:

```text
add actual start/health smoke
```

or rename the job to avoid overstating proof.

---

# 102. Production-like startup verification

A frontend web image can be started and checked for:

```text
HTTP response
static asset availability
health
```

Marketing can be checked for Node server startup and expected route/health.

Backend can be checked for process startup/health with safe config/dependencies.

---

# 103. INFRA-CTR-050 — Startup smoke uses final image stage

Do not run the builder stage and claim the runtime image works.

---

# 104. Composition smoke

When Compose topology/network config changes, validate at least one representative composed path:

```text
gateway → frontend
gateway → API
API → PostgreSQL/Redis
optional worker/broker path where changed
```

---

# 105. INFRA-CTR-051 — Composition smoke proves changed connectivity, not every product feature

Keep container/infrastructure smoke bounded.

Full product correctness remains in integration/E2E/quality suites.

---

# 106. Image inspection

For security/build changes inspect as appropriate:

```text
effective user
environment
filesystem
installed tools
image history
unexpected files
ports
entrypoint
```

---

# 107. INFRA-CTR-052 — Final image contains no known local secret/config artifact

Verification SHOULD catch accidental inclusion of:

```text
.env*
private keys
credential files
test reports containing secrets
developer home/config
```

---

# 108. Image size

Image size is a performance/supply-chain signal.

Do not optimize size by removing required certificate/timezone/native runtime data blindly.

---

# 109. INFRA-CTR-053 — Minimal means sufficient, not arbitrarily smallest

Correct TLS/runtime/health behavior outranks a cosmetic image-size target.

---

# 110. Dependency vulnerability

Base/runtime dependencies should be scanned through repository/deployment security policy.

Do not assume application package scan covers OS/base-image CVEs.

---

# 111. INFRA-CTR-054 — Container/base-image vulnerability is part of release security

When container scanning is introduced/available, findings follow the same assessed-not-ignored policy as application dependencies.

---

# 112. Build network

Package restore requires network access unless dependency cache/mirror is supplied.

Do not allow build steps to download arbitrary executable code outside declared dependency tooling without review.

---

# 113. INFRA-CTR-055 — Build performs declared dependency acquisition only

Scripts such as:

```text
curl random-url | sh
```

inside production Dockerfile require explicit supply-chain review.

---

# 114. Build arguments

Build args are part of build contract and can affect cache/reproducibility.

---

# 115. INFRA-CTR-056 — Material build argument is versioned/evidenced

Examples:

```text
release SHA
build configuration
public API URL if baked
feature build mode
```

must be traceable in packaging pipeline.

---

# 116. Build-time release SHA

Current frontend CI injects release SHA into web/mobile build environment.

Container packaging SHOULD preserve compatible release identity semantics when packaging those same hosts.

---

# 117. INFRA-CTR-057 — Container and non-container builds expose coherent release identity

Do not make:

```text
CI web artifact says SHA A
container built later from SHA B
```

look like one release.

---

# 118. Static asset cache

Versioned immutable static assets SHOULD avoid unsafe cache collisions across releases.

Exact nginx/CDN cache policy belongs to host/infrastructure implementation.

---

# 119. INFRA-CTR-058 — Container rebuild does not overwrite incompatible immutable asset identity

Old browser HTML/bundles can coexist during deployment.

Frontend packaging must preserve release/cache compatibility.

---

# 120. Compose base + overlays

Current staging/production compose model:

```text
docker-compose.yml
+ docker-compose.staging.yml
```

or:

```text
docker-compose.yml
+ docker-compose.prod.yml
```

This is current composition evidence.

---

# 121. INFRA-CTR-059 — Base/overlay merge is inspected as resolved config

Before deployment, inspect effective Compose config when changes affect:

```text
environment
network
volume
port
security
build
```

Do not reason from only one YAML fragment.

---

# 122. Build-on-target caveat

Current staging/prod commands use `up -d --build`.

As defined in `deployment-runtime.md`, this is current executable evidence, not the canonical final immutable-promotion target.

---

# 123. INFRA-CTR-060 — Container docs do not normalize build-on-host deployment as release architecture

Container build definitions remain reusable when CI/CD later becomes:

```text
build once
→ scan/test
→ push immutable image
→ promote exact digest/tag
```

---

# 124. Local services versus managed services

Local Compose may use local PostgreSQL/Redis/RabbitMQ even if production later uses managed equivalents.

---

# 125. INFRA-CTR-061 — Managed/local substitution preserves required protocol semantics

Moving to a managed provider MUST review:

```text
version/features
TLS/auth
RLS/database behavior
Redis commands/persistence
broker ordering/delivery
limits
backup/recovery
network
```

---

# 126. Local service versions

Local major versions SHOULD align closely enough with supported production versions for protected semantics.

Exact patch may differ intentionally.

---

# 127. INFRA-CTR-062 — Version skew is deliberate

Do not discover at production deployment that local/CI relied on a feature absent from deployed dependency version.

---

# 128. Local service health

Health checks improve deterministic startup but do not replace integration tests.

---

# 129. Local logging

Compose json-file log rotation prevents unbounded local disk growth.

Production log collection/retention belongs to Operations/Infrastructure runtime.

---

# 130. INFRA-CTR-063 — Local log driver is not production observability authority

Do not encode product SLI/alerting around Docker local json logs.

---

# 131. Container labels/names

Fixed container names may be convenient locally.

Production orchestrators/scaling may assign dynamic identities.

---

# 132. INFRA-CTR-064 — Container name is not logical operation/service identity

Telemetry and contracts use semantic service/release identifiers, not one local `container_name`.

---

# 133. Local networking and SSRF

Published/local network services are still untrusted boundaries for application input.

Do not disable SSRF/security controls merely because dependencies run in Docker.

---

# 134. INFRA-CTR-065 — Local network trust does not weaken security code paths

Security-sensitive integration tests SHOULD exercise the same validation logic.

---

# 135. Container file ownership

Copied runtime files/config should be readable/writable only as required by effective user.

---

# 136. INFRA-CTR-066 — Non-root runtime can access only intended paths

Do not fix permission issues by making the whole container filesystem world-writable.

---

# 137. Signals and shutdown

Long-running processes SHOULD respond correctly to container stop/termination signals.

Background workers should stop claiming new work and finish/cancel according to delivery semantics.

---

# 138. INFRA-CTR-067 — Container shutdown preserves delivery correctness

A termination must not:

- ack unprocessed message;
- corrupt in-progress write;
- abandon unknown provider effect without recovery state.

---

# 139. Restart policy

Compose may use `restart: unless-stopped`.

Restart policy is availability mechanism, not fix for deterministic crash loop.

---

# 140. INFRA-CTR-068 — Crash loop is diagnosable, not hidden by restart

Repeated startup/config/schema failure should remain visible and alertable.

---

# 141. Development tooling evolution

Docker/Compose is the current supported repository mechanism.

Future devcontainer/Nix/cloud-workspace tooling MAY be added if it reuses the same manifests/contracts.

---

# 142. INFRA-CTR-069 — New developer environment does not create another dependency authority

A new tool consumes:

```text
global.json
package.json/lockfile
Compose/IaC/config
```

rather than maintaining independent hidden versions.

---

# 143. Container source-debt inventory

Current source evidence requiring final/incremental improvement includes:

```text
1. root .dockerignore does not exclude .env/.env.* despite root-context frontend builds;
2. frontend packaging job named "Container smoke" only builds images;
3. staging/prod Compose currently builds on deployment target rather than promoting an already-built immutable image.
```

The third item is already classified in `deployment-runtime.md`.

---

# 144. INFRA-CTR-070 — Current source debt is not rewritten as canonical rule

Documentation records current evidence and target invariant separately.

Implementation may migrate toward the target without preserving current weaknesses.

---

# 145. Build-context checklist

```text
[ ] context root intentional
[ ] effective .dockerignore
[ ] .env/.env.* excluded
[ ] secrets/credentials excluded
[ ] caches/output excluded
[ ] only required workspace paths copied
[ ] generated/lock inputs included
[ ] no broad accidental COPY
```

---

# 146. Runtime-image checklist

```text
[ ] final stage only
[ ] runtime dependency only
[ ] non-root/effective hardening
[ ] no secret/local env
[ ] deterministic entrypoint
[ ] bounded health check
[ ] release identity
[ ] writable paths explicit
[ ] required CA/runtime data present
[ ] startup smoke
```

---

# 147. Local-Compose checklist

```text
[ ] required protocols
[ ] persistent local volumes classified
[ ] optional profiles
[ ] health checks
[ ] network intent
[ ] developer-only published ports
[ ] migrations/RLS path
[ ] destructive reset clearly local
[ ] no production credential defaults
[ ] resolved config inspectable
```

---

# 148. Packaging-CI checklist

```text
[ ] clean checkout
[ ] locked restore
[ ] build after required quality gates
[ ] exact Dockerfile/context
[ ] cache invalidates correctly
[ ] final image built
[ ] image inspected when risk warrants
[ ] startup/health smoke when claiming smoke evidence
[ ] release SHA/version traceable
[ ] no secret artifacts
```

---

# 149. Container-change evidence matrix

| Change | Required evidence |
|---|---|
| Dockerfile build stage | clean image build + affected app build/tests |
| dependency restore layer | lock/manifests + clean no-cache/representative cache test when needed |
| runtime base image | image build/start + vulnerability/runtime compatibility review |
| USER/capabilities/fs | startup/health + writable-path verification |
| health check | final-image health execution |
| root `.dockerignore` | context review + packaging builds |
| Compose network | resolved config + connectivity smoke |
| Compose volume | lifecycle/reset/recovery review |
| local DB/Redis/Rabbit version | protocol/integration compatibility |
| frontend image | production build + container HTTP/start smoke |
| gateway config | resolved config + frontend/API routing smoke |

---

# 150. Stop conditions

Stop rather than merge if:

- release build depends on local/uncommitted state;
- package lock is modified silently inside image;
- `.env`/secret file enters build context unnecessarily;
- secret is passed through persistent Docker ARG/ENV/layer;
- final runtime contains SDK/tests/source without purpose;
- restore cache omits a changed project/package manifest;
- container runs privileged solely because permissions were not designed;
- customer data is written to disposable container filesystem;
- local DB is replaced by SQLite and cited as PostgreSQL/RLS parity;
- optional pgAdmin/management UI becomes production public dependency;
- `depends_on` is treated as runtime resilience;
- job says “container smoke” but only proves `docker build`;
- a Docker build is used to waive architecture/integration/security tests;
- build-on-target is documented as immutable release promotion;
- source debt in ignore/packaging is copied into new canonical docs as desired behavior.

---

# 151. Related canonical owners

```text
docs/infrastructure/environment-model.md
docs/infrastructure/deployment-runtime.md
docs/delivery/local-development.md
docs/delivery/release-and-rollout.md
docs/quality/security-quality-standard.md
docs/quality/testing-strategy.md
docs/operations/observability.md
docs/operations/service-degradation.md
backend/docs/architecture/infrastructure-and-data.md
backend/docs/architecture/platform-and-messaging.md
frontend/docs/
```

---

# 152. Final containerization rule

For every container/local-service change, answer:

```text
What is the exact build context?
Which files are intentionally sent to the builder?
Can any secret/developer state enter it?
Which toolchain/lock/generator inputs define the artifact?
What remains in the final runtime stage?
Which user/filesystem/capabilities does it need?
Which state is durable versus disposable?
Which local services/protocols are being reproduced?
Which ports/networks/profiles are development-only?
Does CI prove only build, or actual startup/runtime health?
Can the exact image be tied to the source revision whose gates passed?
Can future deployment tooling replace Docker/Compose without changing product semantics?
```

The target is:

> **containerization that produces traceable minimal runtime artifacts from declared source, keeps secrets outside build context/layers, reproduces production-relevant protocols locally without copying production topology blindly, and treats packaging/startup evidence honestly instead of letting Docker become a hidden architecture or quality bypass.**

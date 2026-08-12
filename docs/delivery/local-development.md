---
document_id: DEL-LOCAL-DEVELOPMENT
document_type: delivery-handbook
status: active
owner: engineering-delivery
applies_to:
  - repository
  - local-development
  - onboarding
  - backend
  - frontend
  - docker
  - developer-tooling
evidence:
  - README.md
  - Makefile
  - .env.example
  - .gitignore
  - docker-compose.dev.yml
  - backend/global.json
  - backend/README.md
  - frontend/package.json
  - frontend/README.md
  - docs/delivery/definition-of-done.md
  - docs/quality/testing-strategy.md
review_on:
  - prerequisite-change
  - toolchain-version-change
  - environment-template-change
  - docker-compose-change
  - local-bootstrap-change
  - validation-command-change
  - database-bootstrap-change
  - codegen-change
---

# Local Development

> **Local development is reproducible when a new engineer can clone the repository, discover the declared toolchain, create safe local configuration, start the required dependencies/hosts, create realistic local data, and run the appropriate validation without private instructions.**
>
> Local shortcuts improve feedback speed. They MUST NOT redefine production architecture or weaken tenant/security assumptions.

This document owns local-development and onboarding quality.

Root `README.md` remains the concise entry point.

Backend/frontend READMEs own local commands specific to those workspaces.

This document explains the reproducible workflow and the invariants that keep local development representative.

# 1. Goals

A contributor should be able to:

```text
discover prerequisites
restore/install dependencies
create local environment safely
start infrastructure
run backend/frontend
initialize/reset data
run focused validation
run full validation
run documentation checks
understand destructive commands
diagnose common setup drift
```

# 2. DEL-DEV-001 — Setup is repository-discoverable

Required setup information lives in versioned repository files.

Do not rely on:

- private chat;
- personal shell aliases;
- hidden wiki;
- one engineer's machine;
- globally installed mutable tooling when repository-pinned tooling exists.

# 3. Current toolchain authority

Current repository evidence declares:

```text
backend .NET SDK
→ backend/global.json

frontend Node/pnpm requirements
→ frontend/package.json

frontend package manager
→ packageManager field / lockfile

Docker services/images
→ docker-compose*.yml

commands
→ Makefile / package scripts / project manifests
```

# 4. DEL-DEV-002 — Tool versions come from manifests

Documentation may summarize versions.

The executable manifest remains the current exact authority.

# 5. Current backend SDK

Current `backend/global.json` pins:

```text
.NET SDK 9.0.313
rollForward latestPatch
allowPrerelease false
```

If this changes, update onboarding/README/CI references atomically.

# 6. Current frontend toolchain

Current `frontend/package.json` requires:

```text
Node >= 22
pnpm >= 10
packageManager pnpm@10.0.0
```

Use the repository-selected package manager.

# 7. DEL-DEV-003 — One package manager owns the frontend workspace

Do not introduce npm/yarn lockfiles or installation instructions alongside pnpm.

Use the committed pnpm lockfile and workspace configuration.

# 8. Docker

Docker Compose is the preferred full-stack local path because it provides:

```text
PostgreSQL
Redis
backend
web
marketing
nginx gateway
optional RabbitMQ
optional pgAdmin
```

according to current `docker-compose.dev.yml`.

# 9. DEL-DEV-004 — Container path does not hide required architecture

Docker is a reproducible execution mechanism.

It does not permit:

- bypassing RLS;
- fake authentication as production default;
- different persistence ownership;
- undocumented provider substitutes.

# 10. Local configuration

Current repository keeps `.env.example` committed and ignores `.env` / `.env.*` except the example.

Current template explicitly supports copying to `.env.dev`.

# 11. Canonical current bootstrap

From repository root:

```bash
cp .env.example .env.dev
```

Then populate required **local-only** values.

Do not commit `.env.dev`.

# 12. DEL-DEV-005 — Local secret files are untracked

`.env`, `.env.dev`, `.env.staging`, `.env.prod` or other secret-bearing environment files MUST NOT be committed unless the repository explicitly defines a non-secret example/template exception.

# 13. Example values

`.env.example` contains names/placeholders, not production credentials.

Local-only test secrets should be unmistakably synthetic.

# 14. DEL-DEV-006 — Example configuration never contains reusable production-like secrets

Do not weaken secret scanning to accommodate realistic credentials in fixtures/examples.

# 15. Current onboarding drift

Current root README references:

```text
.env.dev.example
```

while the current repository template is:

```text
.env.example
```

and the Makefile consumes:

```text
.env.dev
```

This is a documentation drift to correct during repository synchronization.

# 16. DEL-DEV-007 — Onboarding drift is fixed at the source, not duplicated

Do not add another setup path to compensate for stale README.

Correct root README/template/commands coherently.

# 17. Required local environment values

The Compose configuration currently requires at least safe local values for dependency credentials such as PostgreSQL and Redis passwords, with additional provider/OAuth/email values optional according to enabled features.

The exact variable inventory belongs to `.env.example` and runtime configuration docs.

# 18. DEL-DEV-008 — This handbook does not duplicate the entire environment schema

Environment template/runtime config are executable/current evidence.

This document owns setup procedure and safety, not every variable.

# 19. Full-stack start

Current root Makefile provides:

```bash
make dev-up
```

which runs the development Compose stack using `.env.dev`.

# 20. DEL-DEV-009 — Start command is idempotent enough for daily use

Repeated start should converge to running local services rather than require undocumented cleanup.

# 21. Logs

Current helpers:

```bash
make dev-logs
make backend-logs
```

Use them before introducing custom ad-hoc container commands.

# 22. Stop

```bash
make dev-down
```

stops the development stack without deleting named volumes.

# 23. Destructive cleanup

```bash
make dev-clean
```

removes development volumes.

This is destructive local state reset.

# 24. DEL-DEV-010 — Destructive local commands are unmistakably local/destructive

They MUST NOT share production connection defaults or be presented as generic database maintenance commands.

# 25. Reset

Current Makefile provides:

```bash
make dev-reset
make dev-reset-full
```

which rebuild local data from a clean volume, restore backend dependencies, migrate, seed, apply RLS, and start.

`dev-reset-full` additionally clears/restores NuGet more aggressively.

# 26. DEL-DEV-011 — Reset recreates architecture-relevant local state

A reset path should include required:

```text
schema migration
seed
RLS/bootstrap
```

rather than only recreating tables without security policy.

# 27. Dependency-only local services

For backend/manual work:

```bash
make db-up
```

starts PostgreSQL and Redis.

RabbitMQ is optional profile:

```bash
make messaging-up
make messaging-down
```

# 28. DEL-DEV-012 — Optional service is enabled only when the tested path needs it

Do not require every provider/tool for pure Domain/frontend work.

Do require realistic service when proving its protocol/property.

# 29. pgAdmin

Optional local tooling:

```bash
make dev-tools
```

starts tools profile including pgAdmin.

Tooling is not part of application architecture.

# 30. Configuration inspection

Current Makefile provides:

```bash
make config-dev
```

to print resolved development Compose configuration using a synthetic local JWT value for resolution.

Use it to diagnose variable/interpolation issues.

# 31. DEL-DEV-013 — Config inspection must not reveal real secrets unnecessarily

Developer diagnostics should avoid copying/pasting resolved secret-bearing production config into issues/docs.

# 32. Local endpoints

Ports are configurable through `.env.dev`.

Current Compose defaults differ between direct services and gateway.

Therefore this handbook does not declare one permanent port set as architecture.

# 33. DEL-DEV-014 — Runtime address comes from resolved local configuration

Use:

```bash
make config-dev
```

or the active Compose config instead of relying on stale documentation ports.

# 34. Current Compose defaults

Current direct development defaults include:

```text
backend container host port: 8000
web: 5173
marketing: 3000
gateway: 3080
PostgreSQL: 5432
Redis: 6379
RabbitMQ management when enabled: 15672
```

These are current evidence and can be overridden by environment values.

# 35. Root README drift note

Current root README still lists a different older access set for some hosts.

During final synchronization, update it to route developers to the current resolved Compose configuration rather than maintaining duplicate stale port truth.

# 36. Backend manual workflow

From `backend/`:

```bash
dotnet restore backend.slnx
dotnet build backend.slnx
dotnet test backend.slnx
```

Current backend README exposes these as baseline commands.

# 37. DEL-DEV-015 — Backend solution inventory is `backend.slnx`

Do not maintain a second handwritten list as build authority.

# 38. Backend Docker helpers

From repository root:

```bash
make be-build
make be-test
make be-shell
make backend-image-build
```

These use the Docker-backed backend environment.

# 39. Focused backend work

Prefer the closest relevant project/test during iteration.

Examples:

```bash
dotnet test tests/Notrelix.Domain.Tests/Notrelix.Domain.Tests.csproj
dotnet test tests/Notrelix.Application.Tests/Notrelix.Application.Tests.csproj
```

Exact test project paths can be discovered from `backend.slnx`.

# 40. DEL-DEV-016 — Focused test is not reported as full validation

Use focused tests for speed.

Run all gates implied by the classified change before claiming completion.

# 41. Backend format/build

Use repository/CI-equivalent formatting/build commands documented by backend tooling/CI.

Do not invent a different local quality policy.

# 42. Database workflow

Current root Makefile provides:

```bash
make db-migrate
make db-seed
make db-init
make db-rls
make db-psql
```

# 43. DEL-DEV-017 — Local schema changes use migrations

For intended persistence change:

```text
change model
→ create reviewed migration
→ apply locally
→ test upgrade/current schema
```

Do not manually alter the local DB and forget the migration.

# 44. EF migration creation

Exact EF invocation/project/startup arguments belong to backend persistence docs/tooling.

Use those rather than guessing when the context/startup project requires explicit options.

# 45. Model drift

Pending model changes locally are a signal to:

- generate/fix migration;
- correct unintended model change.

Do not suppress the warning as a local workaround.

# 46. DEL-DEV-018 — Local workaround cannot normalize migration debt

If local startup only works after suppressing migration/RLS checks, the setup or implementation is wrong.

# 47. Seed data

Seed data exists to enable realistic development/testing.

It is not canonical product truth.

# 48. DEL-DEV-019 — Tenant-sensitive local seed contains multiple scopes

When working on tenant-sensitive behavior, local data should include at least:

```text
Account/Workspace A
Account/Workspace B
```

so missing scope does not look correct accidentally.

# 49. Developer account

Avoid one globally privileged developer identity for every path.

Where practical, include:

```text
owner/admin
member
guest/restricted
cross-tenant user
```

for authorization testing.

# 50. DEL-DEV-020 — Local convenience does not erase permission states

Dev bypasses must be explicit, isolated, and incapable of silently becoming production defaults.

# 51. Frontend install

From `frontend/`:

```bash
pnpm install --frozen-lockfile
```

when lockfile and package manifests are synchronized.

# 52. DEL-DEV-021 — Frozen lockfile failure is fixed at manifests/lockfile

Do not normalize:

```bash
pnpm install --no-frozen-lockfile
```

in CI/onboarding merely to hide stale lockfile.

Update the lockfile intentionally and commit it.

# 53. Frontend development hosts

Current scripts:

```bash
pnpm dev:web
pnpm dev:mobile
pnpm dev:marketing
pnpm dev
```

`pnpm dev` uses Turborepo to run workspace dev tasks.

# 54. DEL-DEV-022 — Run only required host during focused work when possible

This reduces local resource usage and feedback time.

Full-stack composition remains available when the change requires it.

# 55. Frontend fast validation

Current frontend script:

```bash
pnpm validate:fast
```

runs current fast gates including:

```text
codegen drift
architecture
architecture-doc drift
test taxonomy
lint coverage
typecheck
lint
format check
guarded Node tests
guarded web tests
```

# 56. DEL-DEV-023 — Fast validation is fast feedback, not universal completion

Changes affecting:

- integration;
- mobile;
- generators;
- UI foundation;
- production E2E

still require corresponding evidence.

# 57. Frontend full local validation

Current script:

```bash
pnpm validate
```

adds guarded:

```text
integration
mobile
generator
```

tests to `validate:fast`.

# 58. UI foundation

Current UI commands include:

```bash
pnpm test:ui:a11y
pnpm test:ui:visual
pnpm test:ui:freeze
```

Use when shared UI/design-system/accessibility surfaces change.

# 59. E2E

Current frontend script:

```bash
pnpm e2e
```

uses Playwright configuration.

Production-like E2E workflow requirements remain owned by testing/CI docs.

# 60. Code generation

Current frontend commands:

```bash
pnpm codegen
pnpm codegen:check
```

`codegen:check` regenerates and fails if committed generated contracts drift.

# 61. DEL-DEV-024 — Generated contract is regenerated, not hand-edited

If generated frontend contract changes unexpectedly:

```text
inspect producer/source
→ regenerate
→ review diff
```

# 62. Frontend architecture

Current commands:

```bash
pnpm check:architecture
pnpm check:architecture-docs
pnpm check:deps
```

These execute the frontend dependency architecture owner.

# 63. DEL-DEV-025 — Local deep import is not an acceptable shortcut

If architecture/dependency check fails, fix ownership/export/import or use governed exception.

Do not disable the check for local convenience.

# 64. Test taxonomy

Current frontend categorizes tests into:

```text
node
web
integration
mobile
generators
```

Guarded variants fail on zero execution.

# 65. DEL-DEV-026 — New tests use the correct runtime category

Do not place browser/native/integration behavior under a faster incorrect category merely to make validation pass.

# 66. Documentation validation

Current root Makefile provides:

```bash
make docs-check
```

which currently invokes the documentation checker.

The checker itself is under migration during the docs refoundation and must be updated atomically with the new canonical tree.

# 67. DEL-DEV-027 — Local docs check follows current documentation authority

When the docs governance migration completes, local command and CI must validate the new canonical tree and stop enforcing retired paths.

# 68. Root-level workflow

Recommended current high-level onboarding:

```bash
git clone <repository>
cd Notrelix
cp .env.example .env.dev
# populate required local-only values

make dev-up
make dev-logs
```

Then use focused backend/frontend commands for the area being changed.

# 69. DEL-DEV-028 — First-run workflow has an actionable failure path

If prerequisites/config are missing, scripts should fail with messages identifying:

```text
missing tool
missing env variable
failed service
migration problem
port conflict
```

rather than opaque generic exit where feasible.

# 70. Native/manual development

Developers may run backend/frontend directly on host while Docker runs dependencies.

This is valid when the host toolchain matches repository manifests.

# 71. DEL-DEV-029 — Manual path must preserve the same external contracts

Host-run backend/frontend must still use:

- PostgreSQL/Redis/etc. as required;
- same migrations/RLS;
- same auth/tenant semantics.

Do not replace production-critical dependencies with unrelated in-memory alternatives and call it equivalent.

# 72. Local provider substitutes

Email/provider services may be disabled/faked for unrelated development.

When testing actual provider contract, use realistic deterministic fixtures/emulators/integration environment as quality strategy requires.

# 73. DEL-DEV-030 — Fake provider has declared fidelity boundary

A fake is evidence only for the behavior it reproduces.

It cannot prove:

- provider signature;
- actual rate limit;
- OAuth redirect;
- external idempotency;
- SQL/RLS.

# 74. Messaging

RabbitMQ is currently an optional development profile.

Enable when changing/testing Platform messaging path.

Backend tests may use dedicated test infrastructure according to test suite.

# 75. Search/storage/provider services

Start only the services required by the current capability.

Do not turn onboarding into “install every possible future dependency”.

# 76. DEL-DEV-031 — Onboarding prerequisites follow current executable need

When a dependency becomes mandatory, update:

```text
Compose
README/onboarding
config template
CI
runtime docs
```

in the same change.

# 77. IDE/editor

IDE is developer choice unless repository has a required generated/tool integration.

Do not make Cursor/VS Code/Rider-specific settings architecture authority.

# 78. DEL-DEV-032 — Provider/tool-specific assistant folders are not project architecture

Local AI/IDE tool directories can be ignored/generated according to repository policy.

Canonical instructions remain repository docs/AGENTS.

# 79. OS differences

Docker-based path should minimize macOS/Linux differences.

When a command is platform-sensitive, document it near the executable workflow.

# 80. Windows

If Windows native support becomes first-class, ensure scripts/shell assumptions are tested.

Current Makefile uses `/bin/bash`, so Docker/WSL/Unix-like shell expectations should be explicit rather than pretending universal native support.

# 81. DEL-DEV-033 — Supported local platform claims are evidence-based

Do not promise a host workflow that repository scripts have never been exercised on.

# 82. Ports

Port collisions are local environment problems.

Use `.env.dev` overrides rather than editing committed Compose files solely for one workstation.

# 83. DEL-DEV-034 — Personal machine override stays local

Do not commit developer-specific ports/paths/credentials into shared manifests without product/tooling need.

# 84. Local TLS

If local HTTPS becomes required for auth/provider/security semantics, add a reproducible certificate/bootstrap path.

Do not require private certificate folklore.

# 85. Database inspection

`make db-psql` is current canonical shortcut for local PostgreSQL shell.

Manual inspection must not substitute for migration/test evidence.

# 86. Logs

Local logs can include development detail, but secret/PII redaction standards still apply.

# 87. DEL-DEV-035 — Development mode is not permission to log secrets

Security quality applies locally too, especially because logs/screenshots are often shared in issues/chats.

# 88. Reset troubleshooting

Before destructive reset, prefer:

```text
inspect logs
inspect resolved config
check service health
check migration/model drift
check dependency restore
```

Use reset when local data state itself is the problem.

# 89. DEL-DEV-036 — Reset is not the default fix for reproducible defects

If every developer must delete volumes after a normal code change, the migration/bootstrap process is likely broken.

# 90. Clean dependency restore

`dev-reset-full` / NuGet cleanup can repair corrupted caches.

Do not use force/no-cache permanently in normal flow without root cause.

# 91. Frontend install troubleshooting

Frozen-lockfile failure usually means:

```text
package.json/workspace manifest
≠
pnpm-lock.yaml
```

Fix/commit the lockfile intentionally.

# 92. DEL-DEV-037 — Dependency drift is repaired, not bypassed

Local/CI onboarding should converge to deterministic dependency state.

# 93. Build artifacts

Generated/local build artifacts (`node_modules`, `.turbo`, binaries, volumes) are disposable.

Do not treat them as source.

# 94. Local data lifecycle

Development data can be reset.

Do not use real customer production exports as default local seed.

# 95. DEL-DEV-038 — Local sample data is synthetic/minimized

If production-derived data is ever needed for a controlled debugging workflow, privacy/security policy and sanitization apply.

# 96. Branch/update workflow

After pulling changes that affect:

- lockfile;
- SDK;
- migration;
- Compose;
- generated contracts;

rerun the corresponding restore/install/migrate/codegen steps.

# 97. DEL-DEV-039 — Onboarding changes are atomic with toolchain changes

When a prerequisite/command changes, update:

```text
manifest
automation
README/onboarding
CI
generated/tool docs
```

in the same classified change.

# 98. CI parity

Local validation should share commands/producers with CI when practical.

Do not maintain a separate private “real CI command”.

# 99. DEL-DEV-040 — Local command and CI prove the same contract where practical

CI may add:

- clean environment;
- artifact upload;
- service containers;
- exact non-zero verification;
- production build.

The underlying test/generator should remain shared.

# 100. Focused validation matrix

| Change | Start with |
|---|---|
| Domain rule | relevant Domain tests |
| Application behavior | Application tests |
| EF/schema | DB + Infrastructure/Integration |
| messaging | Platform tests + messaging infrastructure as needed |
| API contract | API tests + OpenAPI/codegen |
| frontend pure/state | node/web focused suite |
| frontend integration | integration suite |
| mobile | mobile suite |
| UI primitive | Storybook a11y/visual |
| generated contract | codegen:check |
| docs | docs-check |
| full frontend change | `pnpm validate` + affected build/E2E |

Final completion follows `definition-of-done.md`, not this table alone.

# 101. New engineer first-hour checklist

```text
[ ] clone repository
[ ] read root README / AGENTS / CONTEXT-MAP
[ ] verify .NET SDK from backend/global.json
[ ] verify Node/pnpm from frontend/package.json
[ ] copy .env.example → .env.dev
[ ] add local-only required values
[ ] make dev-up
[ ] inspect make dev-logs
[ ] verify backend/frontend path through current gateway/direct ports
[ ] run one backend focused test
[ ] run pnpm validate:fast
[ ] run make docs-check
```

# 102. Tenant-sensitive development checklist

```text
[ ] at least two Account/Workspace scopes
[ ] restricted User/guest where relevant
[ ] wrong-tenant resource ID case
[ ] RLS applied
[ ] cache/query key scoped
[ ] realtime subscription scoped
[ ] no global dev bypass
```

# 103. Database-change checklist

```text
[ ] model change intentional
[ ] migration generated/reviewed
[ ] local apply succeeds
[ ] existing-data behavior considered
[ ] RLS/index/constraint reviewed
[ ] Infrastructure/Integration tests
[ ] no suppressed model drift
```

# 104. Frontend-change checklist

```text
[ ] correct package owner
[ ] pnpm lockfile synchronized
[ ] codegen if contracts changed
[ ] architecture checks
[ ] correct test category
[ ] type/lint/format
[ ] accessibility when UI changed
[ ] build/runtime host affected
```

# 105. Current drift inventory for synchronization

Current repository evidence shows these onboarding items need final alignment:

```text
root README:
  references .env.dev.example
current repository:
  provides .env.example
  Makefile consumes .env.dev

root README:
  contains older fixed host-port list
current docker-compose.dev.yml:
  has configurable ports with different current defaults
```

This handbook does not create a third truth.

Final integration should patch the root README to route to the current executable configuration.

# 106. DEL-DEV-041 — Current-state discrepancy is recorded, not silently copied

When docs/source disagree:

```text
identify canonical executable source
classify drift
fix the stale owner during synchronization
```

# 107. Stop conditions

Stop and fix onboarding/tooling rather than documenting a workaround if:

- setup requires an untracked private file nobody can derive;
- docs instruct copying a file that does not exist;
- local path uses a different package manager than CI;
- frozen lockfile is bypassed permanently;
- local database bypasses migrations/RLS;
- one global dev tenant makes cross-tenant bugs invisible;
- provider/auth mock silently becomes production default;
- architecture check is disabled for local convenience;
- required setup depends on personal shell alias/IDE;
- destructive reset can point at production/staging by default;
- README, Makefile, Compose, and manifests describe conflicting canonical commands and no drift is recorded.

# 108. Related canonical owners

```text
README.md
AGENTS.md
CONTEXT-MAP.md
docs/delivery/team-ownership.md
docs/delivery/definition-of-done.md
docs/delivery/migration-policy.md
docs/quality/testing-strategy.md
docs/quality/security-quality-standard.md
backend/README.md
backend/docs/
frontend/README.md
frontend/docs/
```

# 109. Final local-development rule

A new engineer should be able to answer from the repository:

```text
Which tool versions are required?
Which environment template is safe to copy?
Which local file stores secrets?
How do I start/stop/reset the stack?
How do I run only backend/frontend/dependencies I need?
How do I apply migrations/RLS and create realistic tenant data?
What is the fastest trustworthy validation for my change?
What is the full required validation?
How do generated contracts and docs checks run?
Which commands are destructive?
Where do I look when executable manifests disagree with prose?
```

The target is:

> **a local environment that is reproducible, safe, tenant-realistic, and close enough to production contracts to catch architectural mistakes—without requiring every developer to reproduce full production infrastructure or learn private setup folklore.**

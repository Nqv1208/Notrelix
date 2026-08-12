---
document_id: FE-ADR-002
document_type: architecture-decision
status: Accepted
owner: frontend-architecture
applies_to:
  - frontend-package-management
  - frontend-workspace
  - frontend-lockfile
  - frontend-ci-install
  - frontend-toolchain
evidence:
  - frontend/package.json
  - frontend/pnpm-workspace.yaml
  - frontend/.npmrc
  - frontend/pnpm-lock.yaml
  - .github/actions/setup-frontend/action.yml
  - frontend/docs/architecture/dependency-boundaries.md
  - frontend/docs/architecture/testing-and-quality-gates.md
review_on:
  - frontend-package-manager-change
  - frontend-lockfile-model-change
  - frontend-workspace-model-change
  - frontend-ci-install-policy-change
---

# FE-ADR-002 — Package Manager

## ID

`FE-ADR-002`

## Status

**Accepted**

## Date

**2026-07-12**

This date is preserved from the original ADR.

## Owners

**Current stewardship:** `frontend-architecture`

**Historical decision owner/authorship:** Not recorded explicitly in the original ADR.

Current stewardship does not imply historical authorship.

---

# Context

The original ADR recorded that the frontend monorepo needed a package-management foundation supporting:

- workspace protocol dependencies such as `workspace:*`;
- strict dependency boundaries;
- frozen-lockfile installation in CI;
- Turborepo integration.

The original problem was therefore broader than choosing a CLI command.

It concerned:

```text
workspace dependency resolution
+
single dependency lock authority
+
CI reproducibility
+
monorepo task integration
```

The decision selected pnpm as the package manager for the frontend workspace.

---

# Decision

Use **pnpm** as the frontend package manager.

The original ADR explicitly recorded:

```text
pnpm@10.0.0
```

as the selected version at the time of the decision.

It also recorded:

```text
single lockfile at frontend workspace root
no app-level lockfiles
CI install via pnpm install --frozen-lockfile
```

The durable decision identity is:

> The Notrelix frontend uses pnpm as the single workspace package manager, with one shared frontend lockfile and reproducible locked installation for certification/CI.

---

# Decision boundaries

This ADR decides:

```text
package manager
→ pnpm

workspace dependency notation
→ pnpm workspace protocol where applicable

lockfile authority
→ one frontend workspace lockfile

CI dependency installation
→ frozen/locked resolution

monorepo package participation
→ pnpm workspace
```

It does not decide:

```text
every current package version
every workspace glob forever
every Turborepo task
every .npmrc tuning option forever
every cache implementation
```

Those can evolve while the package-manager decision remains unchanged.

---

# Durable identity of the decision

The architectural identity of `FE-ADR-002` is not:

```text
pnpm must remain exactly 10.0.0 forever
```

The exact `10.0.0` version was explicitly part of the original accepted configuration and remains current evidence today.

However, a routine compatible pnpm upgrade does not necessarily change the architectural decision.

A switch from pnpm to another package manager, or a move away from the shared workspace-lockfile model, is much more likely to require supersession.

---

# Current source alignment

Current frontend source still aligns strongly with the accepted decision.

`frontend/package.json` currently declares:

```json
"packageManager": "pnpm@10.0.0"
```

and engine policy:

```json
"pnpm": ">=10.0.0"
```

The package scripts invoke pnpm throughout the workspace.

---

# Current workspace model

`frontend/pnpm-workspace.yaml` currently discovers packages from families including:

```text
apps/*
packages/foundation/*
packages/runtimes/*
packages/ui/*
packages/product/*/*
packages/features/*
tooling/*
tooling/storybook/web
```

It also centralizes dependency version aliases through a pnpm catalog.

The exact current globs/catalog entries are executable configuration, not the historical decision identity.

---

# Current lockfile model

Current `.npmrc` explicitly includes:

```text
shared-workspace-lockfile=true
```

and the workspace uses:

```text
frontend/pnpm-lock.yaml
```

as the frontend lockfile.

This remains aligned with the original:

```text
single lockfile at root
no app-level lockfiles
```

decision.

---

# Current workspace-linking model

Current `.npmrc` also includes:

```text
link-workspace-packages=true
prefer-workspace-packages=true
```

and:

```text
auto-install-peers=false
strict-peer-dependencies=false
```

These are current package-manager configuration details.

They are **not all historical decision clauses** merely because they exist today.

---

# CI installation

Current shared GitHub Actions setup pins:

```text
pnpm/action-setup
version: 10.0.0
```

and installs from the frontend workspace with:

```bash
pnpm install --frozen-lockfile
```

The Node cache is keyed from:

```text
frontend/pnpm-lock.yaml
```

This is direct current evidence of the frozen-lockfile part of the accepted decision.

---

# FE-ADR-002-I1 — One frontend package manager

The frontend MUST NOT use:

```text
pnpm for some packages
npm lockfiles for some apps
yarn lockfiles for another app
```

as independent package-management authorities under this decision.

---

# FE-ADR-002-I2 — One shared frontend lockfile

The frontend workspace uses one shared lockfile as dependency-resolution authority.

App-local/package-local lockfiles would create competing resolution authorities and violate the accepted model unless a future decision changes the workspace architecture.

---

# FE-ADR-002-I3 — Workspace dependencies use the workspace model

Internal package dependencies SHOULD use the approved pnpm workspace model such as:

```text
workspace:*
```

where package manifests require explicit internal workspace dependencies.

Package discovery alone does not grant architectural import permission.

---

# FE-ADR-002-I4 — Locked CI installation is required evidence

Certification/CI dependency installation uses a frozen lockfile.

If package manifests and lockfile disagree:

```text
fix the lockfile/source dependency state
```

rather than permanently disabling frozen installation.

---

# FE-ADR-002-I5 — Package manager does not own dependency architecture

pnpm can resolve/install workspace packages.

It does not determine:

```text
which internal package may import which other package
```

That authority belongs to:

```text
frontend/tooling/dependency-rules/src/architecture-manifest.ts
```

and the dependency architecture.

---

# Relationship to dependency boundaries

This distinction is critical:

```text
pnpm-workspace.yaml
→ package discovery

package.json
→ declared dependencies

pnpm-lock.yaml
→ resolved dependency graph/version lock

architecture-manifest.ts
→ allowed Notrelix internal dependency graph
```

`FE-ADR-002` chooses package-management mechanics.

It does not replace `frontend/docs/architecture/dependency-boundaries.md`.

---

# Relationship to FE-ADR-003

`FE-ADR-002` decides:

```text
how packages are managed/resolved as a workspace
```

`FE-ADR-003` decides:

```text
how packages expose public APIs to consumers
```

They are complementary but independent.

A package can be correctly installed through pnpm while still violating the public-export architecture through a deep import.

---

# Relationship to Turborepo

The original ADR listed Turborepo integration as a package-manager requirement/context.

Current root scripts still use:

```text
turbo
```

for build/typecheck/lint/codegen/task orchestration.

However:

```text
Turborepo
```

is not the package manager.

`pnpm` manages workspace packages/dependencies.

`Turbo` orchestrates tasks.

---

# FE-ADR-002-I6 — Package management and task orchestration remain distinct concerns

Do not treat:

```text
Turbo task graph
```

as lockfile/dependency resolution authority.

Do not treat:

```text
pnpm workspace
```

as the architecture task graph.

Each tool owns a separate concern.

---

# Historical Consequences

The original ADR recorded the following consequences.

## Strict dependency isolation by default

The original record expected pnpm's dependency model to improve isolation compared with flatter installation assumptions.

This remains part of the historical rationale.

Current architecture additionally enforces internal Notrelix boundaries through a dedicated architecture checker rather than relying solely on package-manager behavior.

## Faster installs than npm/yarn

The original ADR explicitly recorded:

```text
Faster installs than npm/yarn
```

as a consequence.

This normalization preserves that statement as **historical rationale**.

It is **not** presented as a newly benchmarked 2026 claim.

No benchmark was recorded in the original ADR, and this normalization does not fabricate one.

## Better monorepo support

The original ADR recorded better monorepo support as a consequence.

Current workspace protocol, shared lockfile, catalog and filters are consistent with that intended usage.

## Frozen CI installation

The original ADR recorded:

```bash
pnpm install --frozen-lockfile
```

as a consequence/CI rule.

Current CI still executes that exact installation form.

---

# Current consequences

These are present-day implications of the accepted decision, not claims that the original ADR explicitly listed them.

## Reproducible dependency resolution

A source revision and committed lockfile determine the intended dependency resolution used by CI.

## Workspace-local filtering

Current scripts can target packages through:

```bash
pnpm --filter ...
```

which supports independent host/tooling workflows within one workspace.

## Shared dependency catalog

Current `pnpm-workspace.yaml` can centralize common dependency version aliases through `catalog:`.

This reduces version duplication.

The catalog feature itself is current implementation detail, not a permanent ADR requirement.

## Package-manager configuration affects containers/CI

Files such as:

```text
frontend/.npmrc
frontend/pnpm-lock.yaml
frontend/package.json
```

must be present/consistent in build environments.

Missing `.npmrc` can alter dependency-resolution behavior.

---

# Compatibility / Migration

## Historical migration plan

**Not recorded in the original ADR.**

The original record states the target package manager and lockfile rules but does not document migration from a previous package manager.

No previous npm/yarn migration chronology is invented here.

## Current compatibility contract

The workspace expects:

```text
Node >= 22
pnpm >= 10
current frontend lockfile
current .npmrc behavior
```

according to current source.

CI currently pins pnpm `10.0.0`.

## Package-manager version upgrades

A pnpm version upgrade should verify:

```text
lockfile compatibility
workspace protocol behavior
peer-dependency behavior
Docker/CI setup
Turbo integration
generated/architecture tooling
all required frontend gates
```

A routine version upgrade does not automatically require a new ADR if the architectural model remains unchanged.

---

# What does not require superseding this ADR

Examples:

```text
pnpm 10.x maintenance upgrade
catalog version update
new workspace package
new workspace glob needed by an approved package family
cache tuning
CI cache optimization
package script changes
```

provided the accepted pnpm/shared-lockfile/frozen-install model remains intact.

---

# What can require superseding this ADR

A new decision is likely required for:

```text
pnpm → npm
pnpm → yarn
pnpm → bun as package-manager authority
multiple independent app lockfiles
splitting the frontend into independently resolved workspaces
abandoning locked/frozen CI dependency resolution as policy
```

The exact future decision must be evaluated when proposed.

---

# Alternatives Considered

## Historical alternatives

The original ADR does **not** contain a formal alternatives section.

Although its historical consequence text mentions:

```text
npm/yarn
```

in a performance comparison, that does not prove that npm and Yarn were formally evaluated as decision alternatives with recorded tradeoffs.

Therefore:

**Detailed historical alternatives are not recorded.**

No retrospective comparison matrix is invented here.

---

# Why alternatives are not reconstructed

It would be easy to fabricate modern arguments such as:

```text
npm lacks feature X
Yarn has issue Y
Bun was immature
```

but those statements would describe current reviewer opinions, not necessarily the 2026-07-12 decision process.

This normalization intentionally does not do that.

---

# Evidence

## Original ADR

The original record explicitly contains:

```text
Date: 2026-07-12
Status: Accepted

Context:
workspace:*
strict dependency boundaries
frozen lockfile
Turborepo

Decision:
pnpm
version 10.0.0
single root lockfile
no app lockfiles

Consequence:
pnpm install --frozen-lockfile
```

## Current root package evidence

`frontend/package.json` currently declares:

```text
packageManager = pnpm@10.0.0
engines.pnpm >= 10.0.0
```

and current frontend scripts use pnpm for workspace operations.

## Current workspace evidence

`frontend/pnpm-workspace.yaml` defines current package discovery and catalogs.

## Current `.npmrc` evidence

Current configuration contains:

```text
shared-workspace-lockfile=true
link-workspace-packages=true
prefer-workspace-packages=true
auto-install-peers=false
```

among its package-manager settings.

## Current CI evidence

`.github/actions/setup-frontend/action.yml`:

```text
installs pnpm 10.0.0
uses frontend/pnpm-lock.yaml for cache dependency
runs pnpm install --frozen-lockfile
```

---

# Evidence interpretation

Current evidence demonstrates that the accepted package-manager decision remains implemented.

It does not make all current `.npmrc` lines part of the original decision.

It also does not prove the original performance comparison against npm/Yarn with a benchmark.

---

# Current known alignment

At normalization time:

```text
package manager
→ pnpm

current pinned packageManager
→ pnpm@10.0.0

shared workspace lockfile
→ enabled

CI frozen lockfile
→ enabled
```

No reviewed evidence indicates the decision has been superseded.

Status therefore remains:

```text
Accepted
```

---

# Historical fidelity notes

This normalization does not claim:

- who originally approved pnpm;
- which package managers were formally evaluated;
- measured install-time benchmark results;
- that every current `.npmrc` setting existed on the decision date;
- that current workspace globs are permanent;
- that pnpm 10.0.0 can never be upgraded.

---

# Review triggers

Review this ADR when a proposal changes:

```text
frontend package-manager authority
shared lockfile authority
workspace resolution model
frozen CI install policy
workspace decomposition into separately locked dependency universes
```

Routine package additions or pnpm-compatible dependency updates do not automatically reopen it.

---

# Supersedes

**None.**

No earlier frontend ADR is recorded as superseded by `FE-ADR-002`.

---

# Superseded By

**None.**

At normalization time, no recorded frontend ADR supersedes `FE-ADR-002`.

---

# Normalization note

This file structurally normalizes the original Accepted ADR.

It preserves:

```text
Date
Status
Context
pnpm decision
pnpm@10.0.0 historical version
single-lockfile rule
no app-lockfile rule
recorded consequences
```

It adds:

```text
Owners
decision identity
current architecture relationship
Alternatives Considered
Compatibility / Migration
current Evidence
Supersedes
Superseded By
historical-fidelity notes
```

Missing historical rationale is left unknown rather than fabricated.

The accepted decision itself has not been changed.

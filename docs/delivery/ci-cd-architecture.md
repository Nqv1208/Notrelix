---
document_id: DEL-CICD-ARCHITECTURE
document_type: delivery-policy
status: active
owner: engineering-delivery
applies_to:
  - repository
  - backend
  - frontend
  - api
evidence:
  - RULE.md
  - AGENTS.md
  - .github/workflows/ci.yml
  - .github/workflows/security-ci.yml
  - docs/delivery/product-delivery-lifecycle.md
  - delivery/catalog.toml
  - delivery/policy.toml
  - delivery/images.lock.toml
  - tools/deliveryctl/
review_on:
  - ci-cd-architecture-change
  - provider-lane-change
  - evidence-contract-change
---

# Notrelix CI/CD — Delivery Platform Architecture

## 1. Goal

One workflow, `.github/workflows/ci.yml` (`Notrelix CI`), is the pull-request gate. It is the single orchestrator: jobs run in parallel and only the side that changed. A merge needs one required check, `CI gate`.

Continuous delivery is dormant (TRANSITION). `release.yml` waits for a `Notrelix CI` run that contains a `release-candidate` job, and `ci.yml` has no such job, so release, promotion and deploy do not run until `ci.yml` produces that input again.

## 2. Canonical authorities

```text
delivery/catalog.toml       component/provider/build/deploy contracts
delivery/policy.toml        routing, proof, source-control and migration policy
delivery/environments.toml  deployment/promotion policy
delivery/images.lock.toml   immutable build/runtime/tooling image authority
```

Only `tools/deliveryctl` interprets these files. `ci.yml` does not consume them; the release path (`release.yml`, `promote-release.yml`, `deploy.yml`) does, and `delivery/images.lock.toml` feeds it the image digests.

## 3. Delivery tooling

`tools/deliveryctl` validates the delivery authorities, resolves images and runs architecture guards. It does not drive CI execution. It uses system Python >=3.11 and the standard library only. `python3 -m tools.deliveryctl validate` and `architecture-check` run locally; they are not in `ci.yml`.

## 4. The CI job graph

```text
changes -> backend-unit ------\
        -> backend-integration (3 shards: workspaces-auth, work-rls, rest)
        -> frontend -----------+-> CI gate (required, if: always())
        -> dependencies -------/
```

- `changes` selects the backend and frontend side from the changed paths.
- `backend-unit` — `dotnet format`, build, the unit test projects (Domain, Application, Infrastructure, Architecture, Platform, API) and OpenAPI drift.
- `backend-integration` — three shards of the integration tests.
- `frontend` — `format:check`, `typecheck`, `lint`, `check:architecture`, `check:i18n`, `codegen:check`, `test:node`, `test:web` and the `app-web` build.
- `dependencies` — calls `security-ci.yml` (reusable) for the NuGet and pnpm dependency audit.
- `CI gate` — fails unless every job passed or was skipped as not needed.

Other workflows: `security-scheduled` and `codeql` (scheduled/code scanning), `postgres-image` (derived postgres runtime image), `stack-smoke`, `container-ci` (`workflow_call` only, no caller), and the dormant release path (section 9).

Not in any pull-request workflow, so not gated by CI today:

- `make docs-check` runs locally with `make docs-check`; not in `ci.yml`.
- `scripts/ci/validate-infra.py` runs locally with `python3 scripts/ci/validate-infra.py`; not in `ci.yml`.
- deliveryctl `validate` and `architecture-check` run locally with `python3 -m tools.deliveryctl <command>`; not in `ci.yml`.
- Migration discipline runs locally with `python3 scripts/ci/check-migration-discipline.py`; not in `ci.yml`.
- Required-proof TRX verification runs locally with `python3 scripts/ci/verify-required-proofs-trx.py` against `backend/tests/ci-proofs.json`; not in `ci.yml`.
- Storybook a11y/visual and Playwright e2e (mock or real) run locally; not in `ci.yml`.
- Docker image build and the Trivy image gate (`container-ci.yml`) run only when called; no caller exists.

## 9. Release state machine

PR CI proves mergeability. Production release subjects are created only from trusted `main` CI.

```text
VERIFIED_CHANGE
  -> RELEASE_CANDIDATE
  -> STAGING_DEPLOYED
  -> STAGING_VERIFIED
  -> PRODUCTION_PROMOTED
  -> PRODUCTION_STABLE
```

A ReleaseCandidate binds source SHA, evidence-summary hash, schema-change state, release contract and exact application/runtime image digests.

Staging consumes that candidate without rebuilding and seals a `StagingVerifiedRelease`. Production promotion accepts only that staging-verified manifest and promotes the same digest set.

## 10. Deployment adapter

The current adapter is Docker Compose over SSH. Compose is a replaceable runtime adapter, not the CI/CD architecture. `release.yml`, `promote-release.yml` and `deploy.yml` verify, pull, migrate, deploy, health-check, smoke and roll back compatible stateless releases against the digests in `delivery/images.lock.toml`. They may not build application images. The path is dormant (section 1).

## 11. Stateful changes and database migrations

Schema evolution follows expand/migrate/contract. Migration execution is an explicit deployment phase.

If a schema-changing migration starts, automatic application downgrade is disabled because previous-application compatibility with the mutated schema is not assumed. Stateful runtime image changes similarly require explicit environment authorization and are never automatically downgraded.

## 12. Infrastructure proof

`python3 scripts/ci/validate-infra.py` renders the production and staging Compose trees and asserts topology guards (mandatory PostgreSQL/Redis/RabbitMQ, network isolation, rootless gateway and application containers, staging RLS and DataProtection settings). It runs locally; `ci.yml` does not run it.

## 13. Source-control topology

```text
feature/* --PR--> develop --PR--> main --cut--> release/X.Y --tag--> vX.Y.Z-rc.N ... vX.Y.Z
```

- `develop` is the integration branch: feature PRs land here, squash-merged, behind `CI gate`.
- `main` is the canonical trunk. It receives `develop` (merge commit) and is always green.
- `release/X.Y` is cut from `main` when every use case in that version's scope is DONE (`product-delivery-lifecycle.md` DEL-014). It takes fixes only, never capability. A fix lands on `main` first and is cherry-picked into the release branch, so `main` never lacks a fix the release has. The branch is kept until the next minor version ships.
- Tags live on the release branch: `vX.Y.Z-rc.N` is the candidate the owner accepts, `vX.Y.Z` is the released version. A hotfix is fixed on `main`, cherry-picked, and tagged `vX.Y.Z+1`.

### 13.1 Version numbers

One Semantic Version for the whole product, because the API, web and operator hosts ship together.

- `0.Y.Z` before customers use the product: `Y` grows with each version that adds use cases, `Z` with fix-only releases.
- `1.0.0` is general availability. After it, MAJOR grows only for a breaking public contract (`NRX-008`).
- The REST contract version (`/v1`) is separate and does not follow the product version.

### 13.2 Release readiness

Release readiness, the release stages and the acceptance of a version by the owner are owned by [`product-delivery-lifecycle.md`](product-delivery-lifecycle.md) (DEL-009 to DEL-014). This document keeps only the branch, tag and version mechanics above.

### 13.3 Transition

Continuous delivery is dormant (§1). Until `ci.yml` produces the release-candidate input again, a release is the tagged commit on `release/X.Y`; the release path moves to release tags when delivery is reactivated.

## 14. Architectural invariants

`python3 -m tools.deliveryctl architecture-check` encodes guards written for the former six-workflow model. Some of them (for example rejecting `ci.yml`) no longer match the repository. Treat a failure there as code debt in `tools/deliveryctl`, not as a rule that forbids `ci.yml`.

Still true: no mutable external Action references or `*-latest` runners, no Docker builds in release/promotion/deploy workflows, and no staging/production Makefile build bypass.

## 15. Runtime authorities

| Runtime | Authority |
|---|---|
| Python | system Python >=3.11 (`tools/deliveryctl`, `scripts/ci`) |
| Node | `frontend/.node-version` |
| pnpm | `frontend/package.json` `packageManager` |
| .NET SDK | `backend/global.json` |
| Runtime images | `delivery/images.lock.toml` |

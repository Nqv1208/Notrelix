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

A push to `main` runs the CI gate only: it builds no release candidate and deploys nothing. Delivery runs from `ci.yml` on a push to a `release/**` branch. After `CI gate` is green, `release-plan` runs `deliveryctl plan`; when the pushed range touches deployable code, `release-images` (`container-ci.yml`) builds each image natively for amd64 and arm64, scans it, and publishes one index digest per image. `release-candidate` builds the manifest from those digests, `release-stack` (`stack-smoke.yml`) runs the full stack on exactly those digests, and `release-seal` attests the manifest and uploads the `release-candidate` artifact. `release.yml` then deploys it to `staging`. Documentation-only and workflow-only pushes produce no candidate. Production is `promote-release.yml`, started by hand against a staging-verified release, behind the `production` GitHub Environment approval.

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
changes -> fast-checks-backend  (format | contract) ---\
        -> fast-checks-frontend (typecheck | rest) ------> fast-checks (one verdict)
fast-checks -> backend-build -> backend-unit (5 projects)
                             -> backend-integration (9 shards)
fast-checks -> frontend
            -> frontend-web (4 shards)
dependencies (no needs)
every job above -> CI gate (required, if: always())
```

- `changes` selects the backend and frontend side from the changed paths. A change under `.github/` or `backend/contracts/` counts as both sides.
- `fast-checks-backend` — two parallel checks: `format` (`dotnet format --verify-no-changes`, then the self-test of the pre-push hook, `bash .githooks/test/pre-push.test.sh`) and `contract` (the Architecture tests and the OpenAPI export compared with `backend/contracts/openapi/notrelix.v1.json`). It keeps its own restore and build.
- `fast-checks-frontend` — `typecheck`, and `rest` (`format:check`, `lint`, `check:architecture`, `check:i18n`, `check:mock-consumer-surface`, `codegen:check`).
- `fast-checks` — one verdict: every fast check of a changed side must be success and a side with no change must be skipped. Any failure keeps every heavy job below from starting.
- `backend-build` — the only restore and build of `backend.slnx` in Release for the heavy jobs. It saves the NuGet package cache and uploads `bin/` and `obj/` as one tar artifact (`backend-build`, kept one day).
- `backend-unit` — one runner per test project (Domain, Application, Infrastructure, Platform, API), each running `dotnet test --no-build --no-restore` on the unpacked build (composite action `use-backend-build`); the legs do not cancel each other, so every project reports. The Architecture tests run in `fast-checks-backend`. Each leg uploads its `.trx` as `unit-trx-<project>` (kept seven days).
- `backend-integration` — nine shards of `Notrelix.Integration.Tests`, each run with `--no-build --no-restore` on the same build: `workspaces`, `auth`, `work-management`, `rls-1`, `rls-2`, `rls-3`, `rls-4`, `rls-rest` and `rest`. Their filters are exact complements, so every test runs in exactly one shard (section 4.1).
- `frontend` — `test:node` and the `app-web` build.
- `frontend-web` — the web component suite, split across 4 vitest shards (`--shard=N/4`).
- `dependencies` — calls `security-ci.yml` (reusable) for the NuGet and pnpm dependency audit.
- `CI gate` — fails unless every job passed or was skipped as not needed: a changed side must be success, and a skip is allowed only when that side did not change. Branch protection requires only this check.

A heavy job starts on `!cancelled()` plus its own side and the fast-check verdict (or `backend-build` for the backend test jobs), never on the implicit `success()`. The implicit check also counts the skipped fast check of the other side further up the chain and would skip the job on a one-sided pull request. The release jobs follow the same rule.

### 4.1 Integration shards

The Rls tests run one class after another, so a long class makes the whole shard long. The split is set by measured time:

- `rls-1` to `rls-4` name their classes (`FullyQualifiedName~Notrelix.Integration.Tests.Rls.<Class>.`, joined with `|`).
- `rls-rest` is the complement: every other Rls class, including new ones (`...Rls.` and `&FullyQualifiedName!~...Rls.<Class>.` for each listed class).
- `workspaces`, `auth` and `work-management` select by namespace; `rest` is the complement of those and of `Rls.`.

Each shard uploads its `.trx` as the artifact `integration-trx-<shard>` (kept seven days). To rebalance, download those from one run, sum the time of each class (the span from its first start to its last end, plus about 4 s between classes), pack the large classes into the explicit shards and leave the small ones to `rls-rest`. Keep every job under about 8 minutes. Prove the new filters before pushing: `dotnet test tests/Notrelix.Integration.Tests/Notrelix.Integration.Tests.csproj --no-build --list-tests --filter "<filter>"` for each shard must select every test of the unfiltered list exactly once.

Other workflows: `security-scheduled` and `codeql` (scheduled/code scanning), `postgres-image` (derived postgres runtime image), `stack-smoke`, `container-ci` (`workflow_call`, called by the `release-images` job), and the release path (section 9).

Not in any pull-request workflow, so not gated by CI today:

- `make docs-check` runs locally with `make docs-check`; not in `ci.yml`.
- `scripts/ci/validate-infra.py` runs locally with `python3 scripts/ci/validate-infra.py`; not in `ci.yml`.
- deliveryctl `validate` and `architecture-check` run locally with `python3 -m tools.deliveryctl <command>`; not in `ci.yml`.
- Migration discipline runs locally with `python3 scripts/ci/check-migration-discipline.py`; not in `ci.yml`.
- Required-proof TRX verification runs locally with `python3 scripts/ci/verify-required-proofs-trx.py` against `backend/tests/ci-proofs.json`; not in `ci.yml`.
- Storybook a11y/visual and Playwright e2e (mock or real) run locally; not in `ci.yml`.
- Docker image build and the Trivy image gate (`container-ci.yml`) run only when called; no caller exists.

## 9. Release state machine

PR CI proves mergeability. Production release subjects are created only from trusted `release/**` CI; `main` CI never builds a candidate or deploys.

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

Staging is deployed from `release/X.Y` pushes by the release path (§1); `main` never deploys. A release is the tagged commit on `release/X.Y`; promotion to production is manual.

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

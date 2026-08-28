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
  - .github/workflows/backend-ci.yml
  - .github/workflows/frontend-ci.yml
  - .github/workflows/docs-ci.yml
  - .github/workflows/infra-ci.yml
  - .github/workflows/container-ci.yml
  - .github/workflows/ci-definition.yml
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

Notrelix CI/CD is split into independently-owned continuous-integration workflows (one per domain) plus a frozen release path. Each domain owns its complete proof chain locally; no central orchestrator plans execution.

The dependency direction is fixed:

```text
domain change
  -> domain-local change detection
  -> domain proof jobs
  -> domain final gate (aggregates the required checks)
  -> GitHub branch protection (required checks)
  -> main merge
  -> ReleaseCandidate
  -> staging verification
  -> production promotion
```

The reverse direction is forbidden. Workflows do not parse `delivery/*.toml` and do not rediscover component/image/environment policy. Only `tools/deliveryctl` interprets the delivery authorities.

## 2. Canonical authorities

```text
delivery/catalog.toml       component/provider/build/deploy contracts
delivery/policy.toml        routing, proof, source-control and migration policy
delivery/environments.toml  deployment/promotion policy
delivery/images.lock.toml   immutable build/runtime/tooling image authority
```

Only `tools/deliveryctl` interprets these files.

## 3. Delivery tooling

`tools/deliveryctl` is the repository-local delivery compiler. It owns:

- authority validation;
- resolved image resolution;
- architecture regression guards;
- visual documentation checks.

It does not own CI execution: there is no planner-generated execution matrix consumed by workflows, and no central gate depends on a control-plane plan.

The tooling uses system Python >=3.11 and Python stdlib only. There is no repository `.python-version`, `actions/setup-python`, `setup-ci-python`, pip bootstrap, virtualenv or Poetry dependency.

## 4. Domain-owned workflows

Every workflow is standalone (receives the standard `pull_request`/`merge_group`/`push`/`workflow_dispatch` triggers, never `workflow_call`) and follows one shape:

```text
changes job:  resolve base/head SHAs for the triggering event,
              diff the changed paths against domain ownership patterns,
              emit a per-domain selection
domain jobs:  run only when the domain selection is true
final gate:   always runs; requires change detection itself and every
              proof job to have succeeded when the domain was selected
```

Change detection is duplicated intentionally: each workflow may have different authority (e.g. backend sources vs. OpenAPI contract vs. infra topology) and a shared detector would become the same central orchestrator the architecture forbids.

Current domains:

- **Backend CI** — .NET quality/architecture/domain/application/infrastructure/platform/API/integration proof, migration append-only discipline, dependency-graph and NuGet vulnerability guards, OpenAPI contract drift;
- **Frontend CI** — repository invariants, affected tests, exact-artifact host E2E, mock persona/state E2E, production/mock artifact isolation and pnpm dependency audit;
- **Documentation CI** — `make docs-check` documentation governance;
- **Infrastructure CI** — static Compose/gateway/rootless topology validation plus an assembled staging stack health run (migrations, RLS, HTTP live probes);
- **Container CI** — build, Trivy HIGH/CRITICAL gate and SPDX SBOM for exact backend/web/marketing image bytes (validation-only; publish/attest live on the release path);
- **CI Definition** — deliveryctl validation, `architecture-check`, actionlint, artifact-helper roundtrip and runtime image-lock security scanning.

Adding a component that fits an existing domain changes that domain's ownership patterns and proof jobs; adding a wholly new domain adds a new standalone workflow, not a plan step in a central file.

## 5. Frontend exact-artifact boundary

Host E2E jobs are execution environments. They may run Node/pnpm/Playwright but may not parse delivery authority.

Host E2E flow:

```text
build host workspace
  -> package declared output
  -> SHA-256 manifest
  -> upload exact artifact
  -> E2E job downloads artifact
  -> verify SHA-256 and archive members
  -> restore exact artifact
  -> run E2E command
```

The E2E lane does not rebuild the host artifact.

UI visual baselines are bound to the Playwright declaration/runtime version, Storybook versions and Playwright configuration through `frontend/e2e/ui/visual-baseline.lock.json`.

## 6. Backend proof contract

Backend CI preserves explicit critical-test execution guards in addition to running the full test projects. This includes architecture boundaries, RLS/data-event infrastructure guards, platform reliability, API idempotency and critical integration/production-composition tests.

Runtime dependency images used in CI service containers (Redis and publish-runtime images) are declared locally by the workflow; the backend domain does not own a duplicate runtime image authority (digests live only in `delivery/images.lock.toml`, consumed by the release path and by the CI Definition runtime-lock scans).

The NuGet vulnerability gate fails on any project reporting vulnerable package data rather than only emitting a report.

## 7. Gate aggregation

Each domain terminates in a named final gate job (`Backend CI gate`, `Frontend gate`, `Documentation Governance gate`, `Infrastructure CI gate`, `Container CI gate`, `CI Definition Safety gate`). Gates run `if: always()` and require the change-detection job plus every domain job to have succeeded when selected. GitHub branch protection lists these gates (and the security workflow checks) as required checks; there is no evidence aggregation step and no single orchestrator check name.

## 8. Build and supply-chain model

Application image construction is CI validation plus a release-stage publish:

```text
Container CI: build once -> runtime smoke exact image -> HIGH/CRITICAL
              vulnerability gate -> SPDX SBOM (validation only)
Release path: publish the same validated build -> resolve registry digest
              -> provenance attestation -> SBOM attestation
```

Release identity is an immutable digest, never a mutable tag.

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

The current adapter is Docker Compose over SSH. Compose is a replaceable runtime adapter, not the CI/CD architecture.

`release.yml`, `promote-release.yml` and `deploy.yml` may verify, pull, migrate, deploy, health-check, smoke and roll back compatible stateless releases. They may not build application images.

The Makefile intentionally exposes development lifecycle only; staging/production build/deploy targets fail rather than bypass the release-manifest authority.

## 11. Stateful changes and database migrations

Schema evolution follows expand/migrate/contract. Migration execution is an explicit deployment phase.

If a schema-changing migration starts, automatic application downgrade is disabled because previous-application compatibility with the mutated schema is not assumed. Stateful runtime image changes similarly require explicit environment authorization and are never automatically downgraded.

## 12. Infrastructure proof

Infrastructure CI renders the production and staging Compose trees directly from the repository compose files and asserts semantic guards for:

- mandatory PostgreSQL/Redis/RabbitMQ topology;
- backend internal/egress network isolation;
- restricted forwarded-header trust;
- rootless/read-only gateway and application containers;
- staging RLS and DataProtection fail-closed settings;
- gateway upstream wiring;
- no sensitive environment variables on public frontend/gateway services;
- Dockerfile rootless/runtime invariants.

The validator is standalone (`python3 scripts/ci/validate-infra.py`) and does not import the delivery control plane or accept planner-resolved contracts. A second job assembles the full staging stack and verifies migrations, RLS application and live HTTP endpoints.

## 13. Source-control topology

`main` is the canonical trunk and release branch. `develop` remains an accepted PR base only during the current migration and is not an architectural dependency.

## 14. Architectural invariants

`python3 -m tools.deliveryctl architecture-check` rejects, among other regressions:

- `.python-version` / setup-ci-python / setup-python in CI;
- the former central orchestrator (`ci.yml`, `ci-v2.yml`, `ci-orchestrator.yml`, `ci-gate.yml`);
- `tools.deliveryctl plan` inside a domain workflow;
- evidence emission inside a domain workflow;
- provider reads of delivery authority/control plane;
- duplicated runtime image digests in providers;
- fail-open frontend final-gate patterns;
- dropped frontend production/mock isolation check;
- dropped backend critical-test guards or NuGet vulnerability gate;
- container publish/attest in CI (release path only);
- weakened docs governance;
- control-plane imports from infra execution helpers;
- infra consumption of planner runtime contracts;
- staging/production Makefile build bypasses;
- Docker builds in release/promotion/deploy workflows;
- mutable external Action references or `*-latest` runners.

## 15. Runtime authorities

| Runtime | Authority | Scope |
|---|---|---|
| Python | system Python >=3.11 on pinned `ubuntu-24.04` | `tools/deliveryctl` validation and CI scripts |
| Node | `frontend/.node-version` | frontend execution workflows |
| pnpm | `frontend/package.json` `packageManager` | frontend execution workflows |
| .NET SDK | `backend/global.json` | backend execution workflow |
| Runtime images | `delivery/images.lock.toml` | release path and CI Definition runtime-lock scans |

New workflow jobs should consume already-declared configuration declared by their owning workflow. A job needing `delivery/*.toml` interpretation belongs in `tools/deliveryctl`, not in a workflow; the deliverctl validation and `architecture-check` live in the CI Definition workflow.
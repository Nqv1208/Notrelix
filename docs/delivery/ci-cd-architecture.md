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

Notrelix CI/CD is a delivery platform, not a set of independent workflow scripts. Product topology and release policy are declarative; GitHub Actions executes resolved contracts.

The dependency direction is fixed:

```text
delivery authorities
  -> tools/deliveryctl
  -> ExecutionPlan / environment / release contracts
  -> reusable execution providers
  -> evidence
  -> Notrelix CI Gate
  -> ReleaseCandidate
  -> staging verification
  -> production promotion
```

The reverse direction is forbidden. Execution providers do not parse `delivery/*.toml` and do not rediscover component/image/environment policy.

## 2. Canonical authorities

```text
delivery/catalog.toml       component/provider/build/deploy contracts
delivery/policy.toml        routing, proof, source-control and migration policy
delivery/environments.toml  deployment/promotion policy
delivery/images.lock.toml   immutable build/runtime/tooling image authority
```

Only `tools/deliveryctl` interprets these files.

## 3. Control plane

`tools/deliveryctl` is the repository-local delivery compiler. It owns:

- authority validation;
- change detection and affected planning;
- fully resolved provider matrices;
- immutable image resolution;
- expected proof calculation;
- environment contract resolution;
- evidence completeness validation;
- release-manifest validation;
- deployment bundle materialization;
- architecture regression guards.

The control plane uses system Python >=3.11 and Python stdlib only. There is no repository `.python-version`, `actions/setup-python`, `setup-ci-python`, pip bootstrap, virtualenv or Poetry dependency for CI planning.

## 4. Execution providers

Provider workflows are deterministic execution lanes. They receive resolved values as typed reusable-workflow inputs.

Current lanes:

- backend — .NET quality/architecture/domain/application/infrastructure/platform/API/integration proof;
- frontend — repository invariants, affected tests, exact-artifact host E2E, mock and UI proof;
- container — build/test/scan/SBOM/publish/attest exact application image bytes;
- infra — resolved Compose/gateway/runtime topology proof;
- security — dependency vulnerability proof;
- docs — governed documentation proof;
- stack — exact-digest release topology proof.

Adding a component that fits an existing provider changes catalog/policy registration, not `ci.yml`.

## 5. Frontend and renderer boundary

Playwright renderer jobs are execution environments, not control-plane environments. They may run Node/pnpm/Playwright but may not run delivery Python or parse delivery authority.

Host E2E flow:

```text
build resolved workspace
 -> package declared output
 -> SHA-256 manifest
 -> upload exact artifact
 -> renderer job downloads artifact
 -> verify SHA-256 and archive members
 -> restore exact artifact
 -> verify installed Playwright version == resolved renderer version
 -> run resolved E2E command
```

The E2E lane does not rebuild the host artifact.

Visual baselines are bound to the renderer digest, Playwright declaration/runtime version, Storybook versions and Playwright configuration through `frontend/e2e/ui/visual-baseline.lock.json`.

## 6. Backend proof contract

Backend CI preserves explicit critical-test execution guards in addition to running the full test projects. This includes architecture boundaries, RLS/data-event infrastructure guards, platform reliability, API idempotency and critical integration/production-composition tests.

Runtime dependency images such as Redis are resolved by the planner and passed as provider input. Backend CI does not own a parallel runtime image authority.

## 7. Evidence model

The ExecutionPlan declares the exact `expected_proofs`. Provider-level evidence records bind proof ID to source SHA and workflow run identity and carry an integrity hash.

Evidence aggregation fails closed on:

- missing proof;
- failed proof;
- duplicate proof;
- unexpected proof;
- stale source SHA;
- foreign workflow run;
- tampered evidence record.

`Notrelix CI Gate` depends on successful exact proof completeness rather than hard-coding every component job.

## 8. Build and supply-chain model

Releaseable application bytes follow:

```text
build once
 -> runtime smoke exact image
 -> HIGH/CRITICAL vulnerability gate
 -> SPDX SBOM
 -> publish same tested bytes
 -> resolve registry digest
 -> provenance attestation
 -> SBOM attestation
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

A ReleaseCandidate binds source SHA, execution-plan hash, evidence-summary hash, schema-change state, release contract and exact application/runtime image digests.

Staging consumes that candidate without rebuilding and seals a `StagingVerifiedRelease`. Production promotion accepts only that staging-verified manifest and promotes the same digest set.

## 10. Deployment adapter

The current adapter is Docker Compose over SSH. Compose is a replaceable runtime adapter, not the CI/CD architecture.

`release.yml`, `promote-release.yml` and `deploy.yml` may verify, pull, migrate, deploy, health-check, smoke and roll back compatible stateless releases. They may not build application images.

The Makefile intentionally exposes development lifecycle only; staging/production build/deploy targets fail rather than bypass the release-manifest authority.

## 11. Stateful changes and database migrations

Schema evolution follows expand/migrate/contract. Migration execution is an explicit deployment phase.

If a schema-changing migration starts, automatic application downgrade is disabled because previous-application compatibility with the mutated schema is not assumed. Stateful runtime image changes similarly require explicit environment authorization and are never automatically downgraded.

## 12. Infrastructure proof

Infrastructure validation consumes planner-resolved runtime/application contracts and preserves semantic checks for:

- immutable release subjects;
- removal of application `build` definitions in the release overlay;
- mandatory PostgreSQL/Redis/RabbitMQ topology;
- backend internal/egress network isolation;
- restricted forwarded-header trust;
- rootless/read-only gateway and application containers;
- staging RLS and DataProtection fail-closed settings;
- gateway upstream wiring;
- no sensitive environment variables on public frontend/gateway services.

The helper is execution-only and does not import the delivery control plane.

## 13. Source-control topology

`main` is the canonical trunk and release branch. `develop` remains an accepted PR base only during the current migration and is not an architectural dependency.

## 14. Architectural invariants

`python3 -m tools.deliveryctl architecture-check` rejects, among other regressions:

- `.python-version` / setup-ci-python / setup-python in CI;
- provider reads of delivery authority/control plane;
- duplicated runtime image digests in providers;
- component IDs hard-coded into the orchestrator;
- fail-open frontend final-gate patterns;
- dropped backend critical-test guards;
- weakened docs governance;
- control-plane imports from infra execution helpers;
- staging/production Makefile build bypasses;
- Docker builds in release/promotion/deploy workflows;
- mutable external Action references or `*-latest` runners.

## 15. Runtime authorities

| Runtime | Authority | Scope |
|---|---|---|
| Control-plane Python | system Python >=3.11 on pinned `ubuntu-24.04` | `tools/deliveryctl` control jobs only |
| Node | `frontend/.node-version` | frontend execution providers |
| pnpm | `frontend/package.json` `packageManager` | frontend execution providers |
| .NET SDK | `backend/global.json` | backend execution provider |
| Playwright renderer | `delivery/images.lock.toml` resolved into ExecutionPlan | UI/host-E2E containers |

New provider jobs should consume already-resolved contracts. A new job needing authority interpretation belongs in the control plane instead of adding another parser/bootstrap path.

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
  - scripts/ci/
review_on:
  - ci-cd-architecture-change
  - provider-lane-change
  - evidence-contract-change
---

# Notrelix CI/CD V4 Architecture

## 1. Goal

V4 makes CI/CD a stable delivery platform rather than a collection of application-specific workflow files. The core is closed to normal product growth: existing provider lanes accept new catalog components through matrices, while routing, proof completeness, packaging and deployment are generated from machine-readable contracts.

The design follows four rules:

1. **One planning authority** — Git changes are translated into one execution plan.
2. **Proof before promotion** — the plan declares proof obligations; providers emit evidence; the final gate checks exact completeness.
3. **Build once** — deployable bytes are built, scanned and smoked before publication; later environments consume those same digests.
4. **Additive extension** — normal new components change the catalog/policy, not `ci.yml`, the final gate or the release engine.

## 2. Control-plane files

```text
delivery/catalog.toml       component/provider/artifact/deploy contracts
delivery/policy.toml        change routing, proof profiles, migration authority
delivery/environments.toml  enforceable deployment-adapter policy (overlay, promotion mode, migrations, smoke, stateful-image policy)
delivery/images.lock.toml   immutable build/runtime dependency images
scripts/ci/build-plan.py    canonical planner
scripts/ci/delivery_model.py shared parser/model
scripts/ci/aggregate-evidence.py proof completeness authority
```

No reusable provider workflow owns independent `paths` routing rules.

## 3. CI topology

```text
PR / merge_group / protected push / manual full proof
                         |
             +-----------+-----------+
             |                       |
             v                       v
    CI Definition Safety          Notrelix CI
 actionlint + model guards             |
                                       v
                              Canonical Execution Plan
                         catalog + policy + dependency graph
                                       |
            +------------+-------------+--------------+-------------+
            |            |             |              |             |
            v            v             v              v             v
         backend      frontend        docs           infra       security
        provider      provider       provider       provider      provider
            |            |             |              |             |
            +------------+-------------+--------------+-------------+
                                       |
                                       v
                               container provider
                         affected PR / complete main set
                                       |
                          main deployable change only
                                       v
                              exact release stack
                                       |
                                       v
                              evidence aggregation
                                       |
                               release candidate seal
                                       |
                                       v
                               Notrelix CI Gate
```

The orchestrator is intentionally generic. Component names are forbidden in `ci.yml` by `validate-ci-layout.py`.

Concrete proof IDs are also closed out of the planner. Components select a `proof_profile` from `catalog.toml`; planes/security/package/release dimensions select profiles through `policy.toml` proof bindings. `build-plan.py` resolves those profiles and is forbidden from synthesizing provider proof strings. This makes proof vocabulary a policy contract rather than Python branching.

## 4. Planning semantics

### Pull requests and merge queue

The planner uses the Git merge-base and head. It asks “what did this change introduce?” rather than comparing the moving base tip with the feature head. Base-branch commits therefore cannot accidentally trigger unrelated proof.

### Push

The event `before -> head` range is used when trustworthy. Missing/zero/unavailable ranges fail safe to full CI; the planner never falls back to only `HEAD^` for a multi-commit push.

### Unknown paths and workspaces

Unknown delivery-relevant surfaces fail safe to broad proof. New work can temporarily cost more runner time, but it cannot silently disappear from CI.

### Documentation

Known documentation paths are exclusive documentation changes and do not invoke application proof.

### Contracts

Public backend contract changes explicitly fan out to frontend consumers even when frontend source was not directly edited.

### Main release candidates

A deployable `main` change seals a coherent candidate from the complete deployable application set. This is deliberate delivery composition: production receives one known set of mutually compatible digests.

## 5. Provider model

GitHub Actions does not support dynamically selecting a reusable workflow from an arbitrary expression. V4 therefore has stable provider lanes and dynamic matrices inside those lanes.

Current providers:

- `backend` — .NET modular-monolith proof.
- `frontend-host` — web/marketing-style host applications.
- `mobile` — mobile application proof without server deployment.
- `docs` — documentation governance.
- `infra` — Compose/gateway topology proof.
- `security` — dependency/security proof.
- `container` — generic container build/scan/smoke/publish.
- `stack` — exact-digest assembled release topology.

Adding another component using an existing provider does not require a new branch in the orchestrator.

## 6. Backend proof contract

`backend-ci.yml` preserves deep application/architecture guarantees:

- deterministic restore and Release build;
- format enforcement;
- architectural no-SQLite invariant;
- append-only applied migration discipline;
- architecture tests and required-test execution verification;
- Domain/Application/Infrastructure suites;
- platform/messaging reliability;
- API/idempotency proof;
- OpenAPI generation/drift check;
- PostgreSQL/Redis integration;
- tenant isolation/RLS;
- outbox, deduplication and realtime behavior;
- migration/production-composition smoke.

The provider emits one canonical `backend:gate` evidence record only after all required jobs succeed.

## 7. Frontend proof contract

`frontend-ci.yml` separates global invariants from affected application proof.

Global invariants include generated-contract drift, architecture, architecture documentation, test taxonomy, lint coverage and formatting.

Affected work then controls:

- dependency-closure typecheck/lint;
- node/web/mobile/tooling/UI/mock tests;
- host builds;
- exact-build host E2E;
- mock artifact isolation.

Frontend host E2E consumes the artifact generated by the corresponding build job. It may not rebuild an independent artifact.

Frontend Dockerfiles use Turbo prune so the image build closure follows the package dependency graph instead of copying the entire monorepo indiscriminately. The authenticated web image runs Nginx as a non-root user on unprivileged port 8080; the marketing image already runs as its dedicated non-root Node user.

## 8. Evidence model

The execution plan contains `expected_proofs` and a plan digest. Each provider writes a proof record containing the proof ID and run/source context. `aggregate-evidence.py` rejects:

- missing expected proof;
- failed proof;
- duplicate/conflicting proof;
- any unexpected proof;
- stale or foreign-run evidence.

The final gate therefore does not need one hard-coded `needs` branch for every future component.

## 9. Container/artifact model

For every selected deployable component, `container-ci.yml` performs:

```text
catalog lookup
 -> build one local image
 -> HIGH/CRITICAL vulnerability gate
 -> runtime smoke against provider health contract
 -> SPDX SBOM
 -> for CI release candidates: publish those tested bytes
 -> resolve immutable registry digest
 -> provenance attestation
 -> SBOM attestation
 -> generic component result/evidence
```

Build-image inputs are digest locked through `delivery/images.lock.toml` and Docker build arguments. The backend Dockerfile uses one NuGet restore mode; it intentionally does not hide a failed locked restore behind an unlocked fallback. Repo-wide NuGet lockfiles may be introduced separately as a dependency-governance change.

## 10. Exact release stack

`stack-smoke.yml` merges the application digest set with immutable runtime infrastructure locks. It scans runtime subjects and boots the exact release topology through generated image overrides.

The base/staging/production Compose files and Nginx integration configs are part of the delivery overlay because runtime topology is itself a release contract. RabbitMQ is mandatory for Staging/Production, backend proxy trust uses the same isolated CIDR declared by Compose IPAM, and backend external egress is separated from frontend peer connectivity.

The stack proof exists to catch failures that isolated images cannot prove:

- wrong Compose service wiring;
- invalid networks/upstreams;
- incompatible application set;
- gateway routing errors;
- migration/RLS startup failure;
- runtime dependency failure.

Only a successful exact-stack proof may contribute `stack:release-candidate` evidence.

## 11. Release and promotion

```text
successful Notrelix CI on main
  + sealed release-candidate.json
              |
              v
       Release Candidate
 verify CI workflow/event/ref/SHA
              |
              v
       deploy same digests to staging
              |
              v
   staging-verified release-manifest.json
              |
      production environment approval
              |
              v
   Promote Staging-Verified Release
       (production today; any manual-promotion environment)
              |
              v
      deploy same SHA + same digests
```

`release.yml`, `deploy.yml` and `promote-release.yml` are forbidden from running Docker builds. The deployment engine renders generic overrides from the manifest; it does not contain fixed backend/web/marketing image variables.

Environment names are themselves the GitHub Environment identities. Job-level GitHub Environment/concurrency expressions are generic functions of the selected environment name because those expressions cannot truthfully be sourced from a TOML file at step runtime. `environments.toml` therefore models only values the deployment adapter actually consumes. Automatic staging and manual-promotion modes are checked against the source-owned environment contract before deployment.

## 12. Stateful dependencies

PostgreSQL, Redis and RabbitMQ runtime images are digest locked and marked stateful. A normal application release cannot silently adopt a newer upstream tag.

A stateful image change requires an explicit deployment override and automatic rollback never downgrades database/cache/broker subjects. Schema changes follow expand/migrate/contract. The planner detects migration-file changes and seals `schema_change=true` into the release manifest. If a schema-changing migration has started, automatic application rollback is disabled because previous-app compatibility with the mutated schema is not assumed.

## 13. Rollback

The deployment host records the last successful release marker containing source/config SHA plus the immutable image set.

For a failure where no stateful image changed and no schema-changing migration started, rollback restores:

- previous Git source/config SHA;
- previous application images;
- previous approved stateless runtime images;
- Compose deployment with `--no-build`;
- post-rollback health verification.

Database schema rollback is deliberately outside automatic rollback. A schema-changing release that has begun migrations enters manual-recovery semantics even if the application health check later fails.

## 14. Security/governance

- external Actions use full immutable commit SHAs;
- runner OS is pinned;
- `CI Definition Safety` actionlints workflows before trusting application CI changes;
- CODEOWNERS protects workflows/actions/planner/delivery contracts/infra packaging files;
- CodeQL reuses planner semantics so languages are affected-aware;
- dependency scanning runs change-driven and scheduled;
- deployment verifies OCI attestations before mutation;
- pinned SSH known-host material prevents trust-on-first-use.

Repository rulesets must additionally enforce CODEOWNER review, required checks and code-scanning severity policy.

## 15. Extension boundary

### No core refactor expected

Normal additions using an existing provider:

- new frontend host;
- new mobile app;
- another backend component using the current backend provider contract;
- another containerized deployable using an existing container contract;
- another `manual-promotion` environment using the generic deployment adapter (registration + GitHub Environment only).

These should be catalog/policy/config registrations plus their source/test definitions.

### New provider extension

A fundamentally new runtime/proof type may require a new reusable provider workflow and one provider lane. That is an extension point, not a rewrite of planning/evidence/release semantics.

### Deployment-platform replacement

Moving from Compose to Kubernetes/ECS/Nomad replaces the deployment adapter and environment-specific stack proof. Catalog, plan, evidence, immutable manifests and promotion authority remain valid.

## 16. Architectural invariants enforced in code

`validate-delivery.py` validates component uniqueness, workspace IDs, deploy env vars, Compose services, lock references, image immutability, migration owner and environment policy.

`validate-ci-layout.py` validates required workflow inventory, removal of legacy workflows, explicit permissions, pinned runners, full-SHA Actions, no workflow-level paths on the required orchestrator, closed-core component independence, no CD rebuilds and CODEOWNERS coverage.

`test_build_plan.py` is the regression suite for routing and extension behavior.

## 17. CI Runtime Authorities

Every repository CI job runs under explicitly declared runtime authorities:

| Authority | Location | Consumed by |
|---|---|---|
| Shell | Workflow-level `defaults.run.shell: bash` | All `run:` steps |
| Python | `.python-version` (root) | `setup-ci-python` action |
| Node | `frontend/.node-version` | `setup-frontend` action |
| pnpm | `frontend/package.json` `packageManager` | `setup-frontend` action |
| Visual renderer | `delivery/images.lock.toml` | `image-info.py` + renderer job |

New CI jobs that call `scripts/ci/*.py` must include the `setup-ci-python` step immediately after checkout. No other job-local Python version selection is permitted.

The Python minimum (`delivery_model.MIN_PYTHON`) is checked at module import time. The workflow-level `shell: bash` contract applies to all steps unless a job explicitly declares its own `defaults.run.shell` (renderer job).

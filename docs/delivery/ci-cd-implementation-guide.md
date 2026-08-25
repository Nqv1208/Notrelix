# Notrelix CI/CD V4 — Implementation and Operations Guide

## 1. Canonical files

Do not duplicate these decisions elsewhere:

| Decision | Canonical file |
|---|---|
| component/provider/build/deploy metadata | `delivery/catalog.toml` |
| changed-path routing/proof/release policy | `delivery/policy.toml` |
| enforceable deployment-adapter behavior | `delivery/environments.toml` |
| build/runtime image subjects | `delivery/images.lock.toml` |
| execution plan | `scripts/ci/build-plan.py` |
| final proof completeness | `scripts/ci/aggregate-evidence.py` |

`ci.yml` consumes generated matrices. It must not accumulate product-specific `if web...`, `if marketing...`, etc.

## 2. Required GitHub checks

After migration and after observing the new workflows green, protected branches should require:

```text
Workflow definition lint
Notrelix CI Gate
```

Do not require individual reusable provider jobs because their existence is affected-plan dependent.

If CodeQL is enabled, configure repository/ruleset code-scanning enforcement independently at the desired severity threshold.

## 3. Repository governance

Require CODEOWNER review for CI/CD control-plane paths. Keep `.github/CODEOWNERS` aligned with repository ownership.

Recommended platform settings:

- block force pushes/deletion on protected branches;
- require pull request reviews and CODEOWNER review;
- require conversations resolved;
- require the two stable CI checks;
- require code scanning results;
- enable secret scanning/push protection;
- keep default `GITHUB_TOKEN` permissions read-only where practical;
- protect `production` with required reviewers.

## 4. Environment configuration

Create GitHub Environments named exactly as the delivery environment IDs (`staging`, `production`). The reusable deploy workflow uses the environment ID directly as the GitHub Environment name. Production must have required-reviewer protection in GitHub settings; repository files cannot truthfully enforce that external platform rule.

Deployment workflow expects environment-specific SSH/deployment values. Keep sensitive values in secrets, non-sensitive configuration in variables.

Typical contract:

| Name | Type | Purpose |
|---|---|---|
| `DEPLOY_HOST` | secret/variable | deployment host |
| `DEPLOY_SSH_USER` | secret | least-privilege SSH user |
| `DEPLOY_SSH_PRIVATE_KEY` | secret | private deployment key |
| `DEPLOY_SSH_KNOWN_HOSTS` | secret | pinned host-key entry; never generated with TOFU in CI |
| `DEPLOY_PATH` | variable | existing checkout, e.g. `/srv/notrelix` |
| `DEPLOY_ENV_FILE` | variable | host runtime env file |
| `DEPLOY_HEALTH_URL` | variable | post-deploy backend health |
| `DEPLOY_SMOKE_URL` | variable | gateway smoke endpoint |
| `DEPLOY_GHCR_USER` | variable | registry user when private GHCR login is required |
| `DEPLOY_GHCR_TOKEN` | secret | narrowly scoped package token when required |
| `DEPLOY_ALLOW_STATEFUL_IMAGE_CHANGE` | variable | explicit one-environment authorization for a reviewed stateful runtime digest change |

Keep runtime `.env` files outside Git.

Do **not** configure `DEPLOY_RUN_MIGRATIONS`. Whether migrations run is owned by the selected entry in `delivery/environments.toml` and materialized into the immutable deployment bundle.

## 5. Deployment-host prerequisites

The current Compose adapter expects:

- Linux host;
- Docker Engine + Compose v2;
- Git;
- curl;
- SSH user with only the Docker/repository permissions required for deployment;
- checkout at `DEPLOY_PATH` with working origin access;
- outbound access to GHCR/source repository;
- enough disk for candidate + previous rollback images.

## 6. TLS and proxy boundary

Production TLS terminates at a managed edge/load balancer. The origin gateway is deliberately HTTP-only, runs as the `nginx` user on internal port `8080`, and must be firewall-restricted so only the trusted edge can reach the host port.

`infra/nginx/nginx.prod.conf` forwards the canonical public request contract as `X-Forwarded-Proto=https` and `X-Forwarded-Port=443`. ASP.NET is configured by Compose to trust only `BACKEND_NETWORK_SUBNET` (default `172.28.0.0/24`), the isolated Docker network containing the gateway and backend dependencies. Do not replace this with trust-all forwarded headers.

If the default CIDR conflicts on a deployment host, change `BACKEND_NETWORK_SUBNET` in the deployment environment file; the Compose IPAM subnet and ASP.NET known-network binding consume the same value.

Do not publish container `443` without adding a complete in-container certificate provisioning/renewal/secret/smoke contract. Do not expose the origin HTTP port directly to the public Internet.

## 7. Planner behavior to verify during migration

Use controlled PRs to prove:

| Change | Expected plan |
|---|---|
| docs-only | docs provider only |
| backend runtime | backend + relevant security/container |
| backend tests | backend proof, no release |
| web runtime | affected frontend host + web container |
| marketing runtime | marketing host + marketing container |
| mobile runtime | mobile proof, no server image |
| shared UI/foundation | reverse-dependency frontend hosts/capabilities |
| public OpenAPI | backend + registered frontend consumers |
| Dockerfile | owning component package + infra/security |
| Compose/Nginx/image lock | infra + complete deployable package set on release branch |
| CI/control-plane | full proof, no product release by itself |
| unknown workspace/path | fail-safe broad proof |

## 8. Backend workflow maintenance

Keep architecture/critical-test verification explicit. Do not replace required-test execution checks with broad test filters that can pass after discovering zero target tests.

Applied migrations are append-only. `check-migration-discipline.py` protects existing migration history from silent rewrite/removal. New schema behavior should be introduced with new migration artifacts.

For destructive database changes, use expand/migrate/contract across releases; image rollback alone is not a database rollback strategy.

## 9. Frontend workflow maintenance

Global invariants should remain global only when they validate repository-wide contracts. App/package proof should use affected dependency closures.

When adding a new host using `frontend-host`:

- register workspace/component in the catalog;
- ensure build output can be packaged/restored by the generic artifact scripts;
- define E2E command/runtime metadata;
- define container contract if deployable;
- add planner regression coverage for any new routing semantics.

Never let E2E silently rebuild a different artifact from the build job.

## 10. Container workflow maintenance

Container proof is generic and driven by the catalog. Every deployable component must define:

- build context;
- Dockerfile;
- image name;
- Compose service;
- deploy env var;
- runtime port/scheme/health path;
- immutable build-image lock arguments where applicable.

A candidate image is promoted only after scan + runtime smoke. CD must not rebuild it.

Runtime topology invariants:

- Staging/Production require RabbitMQ; it is not profile-gated because their application configuration selects `Messaging:Transport=RabbitMQ`.
- backend waits for healthy PostgreSQL, Redis and RabbitMQ;
- backend uses an internal data/proxy network plus a dedicated outbound egress network; it does not join the frontend network;
- web Nginx runs non-root on port 8080;
- production root Nginx runs non-root on port 8080 with all Linux capabilities dropped.

## 11. Image-lock updates

`delivery/images.lock.toml` is a reviewed dependency lock, not a generated-at-deploy file. It is the only image-lock authority; do not add `infra/images.lock.env`, committed release overlays, or another parallel lock representation.

For each update:

1. update source tag/version intentionally;
2. resolve and commit immutable digest;
3. run delivery validation;
4. let container/security/stack proof exercise the new subject;
5. treat stateful service changes as infrastructure changes requiring explicit deployment authorization.

## 12. Evidence contract

Provider workflows should emit evidence only through the common evidence action/script. Every evidence record must map to a proof expected by the execution plan. The set is exact: missing, failed, duplicate, stale/foreign or unexpected evidence fails the gate.

When introducing a new proof type:

1. define the policy/profile obligation;
2. implement provider proof;
3. emit stable proof ID;
4. add planner/gate regression tests.

Do not add a special-case condition directly to `Notrelix CI Gate`.

## 13. Release lifecycle

For deployable `main` changes:

```text
Notrelix CI
 -> complete candidate image set
 -> vulnerability + runtime smoke
 -> SBOM/provenance attestations
 -> exact assembled-stack proof
 -> evidence gate
 -> release-candidate.json
 -> candidate-manifest attestation
```

`Release Candidate` accepts only a successful `Notrelix CI` authority on `main`, deploys the sealed manifest to staging and creates a staging-verified promotion manifest.

Manual release input is an existing CI run ID, not an arbitrary SHA/image set.

## 14. Production promotion

Manual promotion uses `promote-release.yml` and accepts an existing successful `Release Candidate` workflow run plus an environment whose source-owned `promotion_mode` is `manual-promotion`. `production` is the current protected target, but another manual environment does not require another promotion workflow. It validates workflow identity, source SHA/run relation and staging verification, then invokes the same generic deployment provider under the protected `production` Environment.

Promotion may not:

- build images;
- replace digests with tags;
- silently change stateful runtime subjects;
- deploy a different source/config SHA from the manifest.

## 15. Attestation verification

Before host mutation, application OCI subjects are verified against repository/source identity and the expected signer workflow. Keep attestation verification before SSH deployment side effects.

If repository visibility/plan changes affect GitHub artifact-attestation availability, update the security contract explicitly rather than silently skipping verification.

## 16. Rollback and migration drill

Run periodic staging drills:

- deploy candidate A;
- deploy deliberately failing stateless candidate B;
- verify automatic restoration of A source/config + images;
- verify health after rollback.

For schema releases, the planner marks `schema_change=true` from the configured migration paths and that fact is sealed into the candidate and promotion manifest. When a schema-changing migration starts, the deployment adapter disables automatic application rollback. Recovery is manual/rehearsed; the system does not assume that the previous application is compatible with the mutated schema.

Keep schema evolution expand/migrate/contract. Do not test database downgrade by destroying real data.

## 17. Scheduled security

Scheduled dependency scanning exists because vulnerability knowledge changes without source changes. CodeQL also has scheduled/manual full-language coverage while PR/push analysis remains affected-aware.

## 18. Local checks

```bash
python3 scripts/ci/validate-delivery.py
python3 scripts/ci/validate-ci-layout.py
python3 scripts/ci/test_build_plan.py
python3 scripts/ci/test_delivery_contracts.py
python3 -m compileall -q scripts/ci
node --check frontend/scripts/marketing-e2e-server.mjs
```

Docker-capable environment:

```bash
python3 scripts/ci/validate-infra.py
```

GitHub additionally executes actionlint via `CI Definition Safety`.

## 19. What not to do

Do not:

- add workflow-level `paths` to `Notrelix CI`;
- create a new per-app CI workflow when an existing provider contract fits;
- hard-code component IDs into `ci.yml`;
- add image variables directly to release/deploy workflows;
- use mutable Docker tags in the image lock;
- use `*-latest` GitHub runners;
- use mutable external Action tags instead of full commit SHA;
- rebuild between CI, staging and production;
- use `ssh-keyscan` at deploy time as trust-on-first-use;
- automatically downgrade stateful services during application rollback.

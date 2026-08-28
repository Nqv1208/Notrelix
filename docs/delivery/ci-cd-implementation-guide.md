---
document_id: DEL-CICD-IMPLEMENTATION
document_type: delivery-policy
status: active
owner: engineering-delivery
applies_to:
  - repository
  - backend
  - frontend
evidence:
  - docs/delivery/ci-cd-architecture.md
  - .github/workflows/
  - tools/deliveryctl/
review_on:
  - ci-cd-architecture-change
  - release-process-change
---

# Notrelix CI/CD — Implementation and Operations Guide

## 1. Canonical files

| Decision | Canonical source |
|---|---|
| components/build/deploy metadata | `delivery/catalog.toml` |
| routing/release/migration policy | `delivery/policy.toml` |
| environment promotion/deployment behavior | `delivery/environments.toml` |
| immutable build/runtime/tooling images | `delivery/images.lock.toml` |
| authority model and validation | `tools/deliveryctl/model.py` |
| architecture regression policy | `tools/deliveryctl/architecture.py` |
| release validation | `tools/deliveryctl/release.py` |
| deployment bundle materialization | `tools/deliveryctl/bundle.py` |
| CI domain proofs | `.github/workflows/*-ci.yml` |
| infra topology proof | `scripts/ci/validate-infra.py` |

Workflows are standalone domain owners; they consume no delivery authority and do not receive planner-resolved contracts.

## 2. Required repository checks

Protected trunk should require the stable domain gates plus security enforcement:

```text
Backend CI gate
Frontend gate
Documentation Governance gate
Infrastructure CI gate
Container CI gate
CI Definition Safety gate
Security CI
CodeQL (repository governance)
```

Do not require per-job names as branch-protection authorities; the fail-closed domain gates are the stable check names.

## 3. Local validation

```bash
python3 -m tools.deliveryctl validate
python3 -m tools.deliveryctl architecture-check
python3 -m tools.deliveryctl visual --check
python3 -m unittest discover -s tools/deliveryctl/tests -p 'test_*.py' -v
python3 -m compileall -q tools/deliveryctl scripts/ci
node --check frontend/scripts/ci/package-host-artifact.mjs
node --check frontend/scripts/ci/restore-host-artifact.mjs
node frontend/scripts/ci/host-artifact.roundtrip.test.mjs
make docs-check
```

A Docker-capable environment additionally runs pinned actionlint and `python3 scripts/ci/validate-infra.py`.

## 4. Domain maintenance

### Backend

Keep explicit critical-test execution verification. Broad project execution is not a replacement for proving critical named tests were discovered and executed. Runtime service images (Redis) are declared locally by the workflow; image digests still live only in `delivery/images.lock.toml`. The NuGet vulnerability guard must fail on vulnerable packages, not merely report.

### Frontend

Keep repository invariants separate from affected proof. Host E2E restores the exact artifact produced by host-build and does not rebuild it. Verify production/mock artifact isolation with the restored production artifact.

Final domain gates are fail closed. Never hide `require` failures behind `|| true`.

### Container

CI is validation-only: build once -> runtime smoke exact image -> HIGH/CRITICAL gate -> SPDX SBOM. Publish and attestation belong to the release path; CI must not push images or create attestations.

### Infrastructure

`validate-infra.py` renders Compose directly and must not import deliveryctl or parse `delivery/*.toml`, while retaining topology/rootless/RLS/staging semantic checks. The assembled job validates the full staging stack (migrations, RLS, HTTP probes) with ephemeral credentials.

### Documentation/security

Documentation executes repository `make docs-check`. Dependency security keeps diagnostic artifacts and fails the scan contract. Security and release workflows are outside the domain-owned migration and remain untouched.

## 5. GitHub Environments

Create `staging` and `production` environments matching `delivery/environments.toml`. Production should require approval. Keep deployment host secrets at environment scope.

Current Compose/SSH adapter expects values such as deployment host/user/key/known-hosts/path/runtime-env/health/smoke endpoints and optional GHCR credentials. Runtime `.env` files remain outside Git.

## 6. Release lifecycle

For a releaseable `main` push:

```text
domain CI gates
  -> merge to main
  -> ReleaseCandidate attestation
  -> staging deploy without rebuild
  -> StagingVerifiedRelease attestation
  -> protected production promotion
```

Manual release/promotion inputs identify successful workflow runs, not arbitrary image/tag/SHA combinations.

## 7. Database and rollback

Migration commands are source-owned release policy and are materialized into the deployment bundle. Use expand/migrate/contract.

If a schema-changing migration has begun, automatic application downgrade is disabled. Stateful image changes require explicit environment authorization and are not automatically downgraded.

## 8. Image-lock updates

`delivery/images.lock.toml` is the only immutable image authority. Update source version intentionally, resolve/commit digest, run delivery validation. The CI Definition runtime-lock security job scans the pinned runtime digests when the lock changes.

Do not create another committed lock/env file as a parallel authority.

## 9. Production operations

Development builds remain available through Make. Staging/production build/deploy targets intentionally fail; operators use Release Candidate / Promote Staging-Verified Release workflows so production bytes always come from a sealed manifest.

`release.yml`, `promote-release.yml` and `deploy.yml` must contain no application build path.

## 10. What not to do

Do not:

- reintroduce `.python-version`, setup-ci-python or `actions/setup-python` into delivery execution;
- reintroduce a central orchestrator (`ci.yml`, `ci-v2.yml`, `ci-orchestrator.yml`, `ci-gate.yml`);
- call `tools.deliveryctl plan` or read delivery authority from a workflow;
- emit evidence from a domain workflow;
- duplicate runtime image digests in providers;
- weaken named critical-test execution guards;
- use fail-open final-gate shell constructs or `|| true` on gate checks;
- reuse exact host artifacts between transient and production profiles (verify isolation);
- build container images for publish/attestation in CI (release path only);
- rebuild between CI, staging and production;
- replace digests with tags;
- bypass deployment manifests through Make/Compose;
- automatically downgrade stateful services or schema-mutated releases.
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
| components/providers/build/deploy metadata | `delivery/catalog.toml` |
| routing/proofs/release/migration policy | `delivery/policy.toml` |
| environment promotion/deployment behavior | `delivery/environments.toml` |
| immutable build/runtime/tooling images | `delivery/images.lock.toml` |
| authority model and validation | `tools/deliveryctl/model.py` |
| execution plan | `tools/deliveryctl/planner.py` |
| evidence completeness | `tools/deliveryctl/evidence.py` |
| release validation | `tools/deliveryctl/release.py` |
| deployment bundle materialization | `tools/deliveryctl/bundle.py` |
| architecture regression policy | `tools/deliveryctl/architecture.py` |

Providers consume resolved contracts; they do not own parallel authorities.

## 2. Required repository checks

Protected trunk should require the stable top-level checks:

```text
Workflow definition lint
Notrelix CI Gate
```

CodeQL/code-scanning enforcement is configured as repository governance. Do not require affected-dependent matrix job names as branch-protection authorities.

## 3. Local validation

```bash
python3 -m tools.deliveryctl validate
python3 -m tools.deliveryctl architecture-check
python3 -m unittest discover -s tools/deliveryctl/tests -p 'test_*.py' -v
python3 -m compileall -q tools/deliveryctl scripts/ci
python3 -m tools.deliveryctl visual --check
node --check frontend/scripts/ci/package-host-artifact.mjs
node --check frontend/scripts/ci/restore-host-artifact.mjs
make docs-check
```

A Docker-capable environment additionally runs the resolved infrastructure validation and actionlint through `CI Definition Safety`.

## 4. Provider maintenance

### Backend

Keep explicit critical-test execution verification. Broad project execution is not a replacement for proving critical named tests were discovered and executed. Runtime image dependencies such as Redis are planner inputs.

### Frontend

Keep repository invariants separate from affected proof. Host E2E restores the exact artifact produced by host-build and verifies the renderer/package Playwright version contract before executing E2E.

Final provider gates are fail closed. Never hide `require` failures behind `|| true`.

### Container

The provider receives build context, Dockerfile, build args, health contract and smoke dependencies from the ExecutionPlan. Sequence is build once -> smoke/scan -> SBOM -> publish same bytes -> digest -> attest.

### Infrastructure

`validate-infra.py` consumes resolved runtime/application JSON. It must not import deliveryctl or parse TOML, while retaining topology/rootless/RLS/staging/release-no-build semantic checks.

### Documentation/security

Documentation executes repository `make docs-check`. Dependency security keeps diagnostic artifacts and fails the selected scan contract.

## 5. GitHub Environments

Create `staging` and `production` environments matching `delivery/environments.toml`. Production should require approval. Keep deployment host secrets at environment scope.

Current Compose/SSH adapter expects values such as deployment host/user/key/known-hosts/path/runtime-env/health/smoke endpoints and optional GHCR credentials. Runtime `.env` files remain outside Git.

## 6. Release lifecycle

For a releaseable `main` push:

```text
Notrelix CI
 -> exact application image proof
 -> complete application + runtime digest set
 -> exact assembled-stack proof
 -> exact evidence completeness
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

`delivery/images.lock.toml` is the only immutable image authority. Update source version intentionally, resolve/commit digest, run delivery validation, and allow security/container/stack proof to exercise the new subject.

Do not create another committed lock/env file as a parallel authority.

## 9. Production operations

Development builds remain available through Make. Staging/production build/deploy targets intentionally fail; operators use Release Candidate / Promote Staging-Verified Release workflows so production bytes always come from a sealed manifest.

`release.yml`, `promote-release.yml` and `deploy.yml` must contain no application build path.

## 10. What not to do

Do not:

- reintroduce `.python-version`, setup-ci-python or `actions/setup-python` into delivery execution;
- call deliveryctl/TOML authority from reusable providers;
- create component-specific `ci.yml` branches;
- duplicate runtime image digests in providers;
- weaken named critical-test execution guards;
- use fail-open final-gate shell constructs;
- rebuild between CI, staging and production;
- replace digests with tags;
- bypass deployment manifests through Make/Compose;
- automatically downgrade stateful services or schema-mutated releases.

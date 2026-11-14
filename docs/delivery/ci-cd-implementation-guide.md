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
| pull-request gate | `.github/workflows/ci.yml` (`Notrelix CI`) |
| infra topology proof | `scripts/ci/validate-infra.py` (local) |

`ci.yml` consumes no delivery authority. The release path (`release.yml`, `promote-release.yml`, `deploy.yml`) does.

## 2. Required repository checks

Protected trunk requires the single check `CI gate` (it aggregates `changes`, `backend-unit`, `backend-integration`, `frontend` and `dependencies`), plus the CodeQL checks GitHub emits. Do not require per-job names.

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

None of the commands above run in `ci.yml`. A Docker-capable environment additionally runs `python3 scripts/ci/validate-infra.py`; `ci.yml` does not.

## 4. Maintenance notes

- Backend: runtime service images are declared in the workflow; image digests live only in `delivery/images.lock.toml`.
- Frontend: never hide failures behind `|| true`.
- Container images are validation-only in CI; publish and attestation belong to the release path.
- Dependency security (`security-ci.yml`) is called by `ci.yml` as the `dependencies` job.

## 5. GitHub Environments

Create `staging` and `production` environments matching `delivery/environments.toml`. Production should require approval. Keep deployment host secrets at environment scope.

Current Compose/SSH adapter expects values such as deployment host/user/key/known-hosts/path/runtime-env/health/smoke endpoints and optional GHCR credentials. Runtime `.env` files remain outside Git.

## 6. Release lifecycle

Dormant until `ci.yml` produces the release-candidate input `release.yml` waits for. Intended flow for a releaseable `main` push:

```text
CI gate
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

`delivery/images.lock.toml` is the only immutable image authority. Update source version intentionally, resolve/commit digest, run delivery validation. 

Do not create another committed lock/env file as a parallel authority.

### Derived postgres runtime image

`[images.postgres]` is the derived image published by `.github/workflows/postgres-image.yml` (see `infra/postgres/Dockerfile`), not an official tag, once it is pinned to a `ghcr.io/<owner>/notrelix-postgres@sha256:...` digest. The Dockerfile pins its base (`POSTGRES_BASE`), the Go toolchain (`GO_IMAGE`) and the gosu source commit by digest/commit; there are no floating tags.

Rebuild obligation until upstream ships a patched gosu: every time the official postgres digest is bumped, update `POSTGRES_BASE` in the Dockerfile, let the workflow rebuild and publish on `main` (or run it with `workflow_dispatch`), then re-pin `[images.postgres].ref` (and the `FALLBACK_*` copy in `container-ci.yml`) to the new derived digest in a reviewed commit. A stateful image change is never deployed automatically; the target environment needs `DEPLOY_ALLOW_STATEFUL_IMAGE_CHANGE=true` for that release.

Reassessment condition: drop the derived image and pin the official digest again when an official postgres digest ships `gosu` built with Go >= 1.26.10 / 1.27.3 and passes the runtime-image gate without any ignore file.

## 9. Production operations

Development builds remain available through Make. Staging/production build/deploy targets intentionally fail; operators use Release Candidate / Promote Staging-Verified Release workflows so production bytes always come from a sealed manifest.

`release.yml`, `promote-release.yml` and `deploy.yml` must contain no application build path.

## 10. What not to do

Do not:

- reintroduce `.python-version`, setup-ci-python or `actions/setup-python` into delivery execution;
- read delivery authority from `ci.yml`;
- duplicate runtime image digests in providers;
- weaken named critical-test execution guards;
- use fail-open final-gate shell constructs or `|| true` on gate checks;
- reuse exact host artifacts between transient and production profiles (verify isolation);
- build container images for publish/attestation in CI (release path only);
- rebuild between CI, staging and production;
- replace digests with tags;
- bypass deployment manifests through Make/Compose;
- automatically downgrade stateful services or schema-mutated releases.
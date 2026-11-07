---
document_id: DEL-CICD-EXTENSION
document_type: delivery-policy
status: active
owner: engineering-delivery
applies_to:
  - repository
  - backend
  - frontend
evidence:
  - docs/delivery/ci-cd-architecture.md
  - delivery/catalog.toml
  - delivery/policy.toml
  - tools/deliveryctl/
review_on:
  - ci-cd-architecture-change
  - new-component-registration
---

# Notrelix CI/CD — Extension Guide

## 1. Extension model

`.github/workflows/ci.yml` is the single pull-request orchestrator. Its jobs are `changes`, `backend-unit`, `backend-integration`, `frontend` and `dependencies`, and `CI gate` aggregates them. Adding CI coverage for new behavior means extending one of those jobs, or adding a job and listing it in `CI gate`'s `needs` and result checks.

```text
identify the owning job (backend-unit | backend-integration | frontend | dependencies)
 -> add the step or shard there
 -> if a new job: add it to CI gate needs and its result check
 -> branch protection still requires only "CI gate"
```

## 2. Which job owns a change

- `backend-unit` — format, build, unit test projects, OpenAPI drift.
- `backend-integration` — integration test shards (`workspaces-auth`, `work-rls`, `rest`). A new shard is a new matrix entry.
- `frontend` — `format:check`, `typecheck`, `lint`, `check:architecture`, `check:i18n`, `codegen:check`, `test:node`, `test:web`, `app-web` build.
- `dependencies` — reusable `security-ci.yml`.

Checks that exist but are not in `ci.yml` (docs-check, validate-infra, deliveryctl, migration discipline, required-proof TRX, Storybook, Playwright e2e, image build) are listed in `docs/delivery/ci-cd-architecture.md` section 4. Adding one to `ci.yml` is a deliberate change to the gate; slow or flaky suites stay out of the PR gate.

## 3. Required checks on protected branches

Protected trunk requires the single check `CI gate`, never per-job names, plus the CodeQL checks GitHub emits.

## 4. Runtime rule for new jobs

- Backend jobs use `./.github/actions/setup-backend`; frontend jobs use `./.github/actions/setup-frontend`.
- Python helpers use system Python >=3.11 and the standard library.
- Pin external Actions by commit SHA and use `ubuntu-24.04`, never `*-latest`.

## 5. What stays closed

```text
delivery/images.lock.toml  the only committed image authority, referenced by digest
delivery authority parsing  tools/deliveryctl only
release/promotion/deployment workflows  dormant; they do not build application images
```

Release and deploy stay dormant until `ci.yml` produces the release-candidate input `release.yml` waits for.

## 6. Review checklist

- the job that actually executes the new proof;
- `CI gate` still fails when the new job fails or is cancelled;
- runtime images referenced by digest from `delivery/images.lock.toml`, never tags.

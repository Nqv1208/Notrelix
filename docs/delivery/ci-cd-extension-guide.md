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

`.github/workflows/ci.yml` is the single pull-request orchestrator. Its jobs are `changes`, `fast-checks-backend`, `fast-checks-frontend`, `fast-checks`, `backend-build`, `backend-unit`, `backend-integration`, `frontend`, `frontend-web` and `dependencies`, and `CI gate` aggregates them (the graph is in `ci-cd-architecture.md` section 4). Adding CI coverage for new behavior means extending one of those jobs, or adding a job and listing it in `CI gate`'s `needs` and result checks.

```text
identify the owning job (fast-checks-backend | fast-checks-frontend | backend-unit | backend-integration | frontend | frontend-web | dependencies)
 -> add the step or shard there
 -> if a new job: add it to CI gate needs and its result check
 -> if it is a heavy job: start it on !cancelled() plus its side and the fast-check verdict, not on the implicit success()
 -> branch protection still requires only "CI gate"
```

## 2. Which job owns a change

- `fast-checks-backend` — `format` (format check, pre-push hook self-test) and `contract` (Architecture tests, OpenAPI drift). Cheap checks go here, so a slip costs minutes and stops the heavy jobs. A new check is a step in one of the two matrix entries, or a new entry.
- `fast-checks-frontend` — `typecheck` and `rest` (`format:check`, `lint`, `check:architecture`, `check:i18n`, `check:mock-consumer-surface`, `codegen:check`).
- `backend-build` — the single Release build of the solution. A heavy backend job takes this build through `./.github/actions/use-backend-build` and runs `dotnet test --no-build --no-restore`; it never restores or builds again.
- `backend-unit` — the Domain, Application, Infrastructure, Platform and API test projects, one matrix leg each. A new unit test project is a new `project` entry.
- `backend-integration` — integration test shards (`workspaces`, `auth`, `work-management`, `rls-1`, `rls-2`, `rls-3`, `rls-4`, `rls-rest`, `rest`). A new shard is a new matrix entry whose filter keeps the shards exact complements (see `ci-cd-architecture.md` section 4.1); a new test class needs no entry, it lands in `rls-rest` or `rest`.
- `frontend` — `test:node`, `app-web` build.
- `frontend-web` — `test:web`, split across 4 vitest shards (`--shard=N/4`).
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
release/promotion/deployment workflows  consume the release-candidate manifest; they do not build application images
```

Release and deploy consume the release-candidate input that the `release-*` jobs of `ci.yml` produce on a push to `main`.

## 6. Review checklist

- the job that actually executes the new proof;
- `CI gate` still fails when the new job fails or is cancelled;
- runtime images referenced by digest from `delivery/images.lock.toml`, never tags.

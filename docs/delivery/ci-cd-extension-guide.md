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

CI is split into independently-owned domain workflows. Each domain owns its change detection, its proof jobs and its final gate. There is no central `ci.yml`, no execution planner, no provider matrix and no shared evidence aggregator.

Adding CI coverage for new behavior is therefore a **domain-local** change: decide which domain owns the change, extend that domain's ownership patterns, prove the behavior in that domain's explicit jobs, and keep the existing stable domain gate.

The normal extension path is:

```text
identify the owning domain
 -> update the domain-local ownership patterns
 -> extend/add the explicit domain proof jobs (when needed)
 -> the stable domain final gate still aggregates
 -> branch protection still requires the same gate name
```

## 2. Which domain owns a change

Current domains and their ownership:

- **Backend CI** — backend sources, the backend Dockerfile and repository-wide .NET proof (quality, architecture, domain, application, infrastructure, platform, API and integration tests; migration append-only discipline; dependency-graph and NuGet vulnerability guards; OpenAPI contract drift). Final gate: `Backend CI gate`.
- **Frontend CI** — frontend host/apps/packages/tooling proof (repository invariants, affected tests, exact-artifact host E2E, mock persona/state E2E, production/mock artifact isolation, pnpm dependency audit). Final gate: `Frontend gate`.
- **Documentation CI** — `docs/**`, `backend/docs/**`, `frontend/docs/**`, `*.md`, `Makefile` and the Documentation workflow itself; runs `make docs-check`. Final gate: `Documentation Governance gate`.
- **Infrastructure CI** — Compose trees, `infra/**`, infrastructure helpers (`scripts/ci/validate-infra.py`, `scripts/ci/write-test-env.sh`), `frontend/apps/web/nginx.conf`, and the Dockerfiles shared with the stack. Final gate: `Infrastructure CI gate`.
- **Container CI** — Dockerfile/`.dockerignore`, backend/apps/tooling/shared-frontend-config and the workflow itself; builds and gates the backend, web and marketing images (validation only). Final gate: `Container CI gate`.
- **CI Definition** — delivery authority, `tools/deliveryctl/**`, `delivery/**`, the emit-evidence action, and runtime image-lock security. Final gate: `CI Definition Safety gate`.

The durable ownership table in `docs/delivery/ci-cd-architecture.md` is authoritative when ownership is ambiguous.

## 3. Extend an existing domain

For a capability inside an existing domain:

1. Add the new source roots to the owning workflow's change-detection patterns.
2. Add or extend that domain's explicit jobs so the new behavior is actually executed, and keep the fail-closed final gate untouched.
3. Never add a new final gate or a new required check for a change inside an existing domain; the stable gate name is the branch-protection authority.

## 4. Add a new CI domain

A wholly new execution domain (for example a new toolchain or execution host) gets a new standalone workflow, never a plan step in a central file:

```text
.github/workflows/<domain>-ci.yml
  changes job:   resolve base/head SHAs for the triggering event,
                 diff against <domain> ownership patterns, emit the
                 domain selection (fail-closed on unresolved or invalid
                 ranges)
  domain jobs:   run only when the domain selection is true
  <Domain> gate: if: always(); requires the changes result and every
                 domain job to have succeeded when selected, and the
                 domain jobs to be skipped when not selected
```

Then add the new gate name to the protected-branch required checks. Do not create a shared detector used by many workflows; change detection is duplicated deliberately so each domain keeps its own authority and no central orchestrator re-emerges.

## 5. Required checks on protected branches

Protected trunk requires the six stable domain gates as check names, never per-job names:

```text
Backend CI gate
Frontend gate
Documentation Governance gate
Infrastructure CI gate
Container CI gate
CI Definition Safety gate
```

plus the exact CodeQL security checks that GitHub emits for the repository on each workflow run.

## 6. Runtime rule for new jobs

There is no repository CI Python bootstrap.

- CI-definition control jobs use system Python >=3.11 and invoke `python3 -m tools.deliveryctl ...` (validate, architecture-check, visual --check) and the deliveryctl unit tests.
- Backend jobs use the backend toolchain.
- Frontend jobs use Node/pnpm/Playwright.
- Execution/domain jobs never invoke deliveryctl and never parse `delivery/*.toml`.

If a new execution helper needs to decide *what* should run, move that decision into the owning workflow's change detection and pass the result as job selection, not into tooling.

## 7. What stays closed

Routine product growth should leave these contracts closed:

```text
the six domain final gates
the Compose/Dockerfile stack proofs (unchanged by product growth)
delivery/images.lock.toml  the only committed image authority, referenced by digest
delivery authority parsing  tools/deliveryctl only
release/promotion/deployment workflows  frozen; they do not build application images
```

Continuous delivery stays frozen/dormant during and after the CI migration; no new push/promotion/deployment wiring is introduced for product growth. CD reactivation is a separate future workstream after CI stabilization.

## 8. Review checklist

Before extending a domain or adding a new one, confirm:

- owning domain and its ownership patterns;
- an explicit job that actually executes the new proof;
- fail-closed gate behavior preserved (`require changes` first; selected requires the proof jobs to succeed; unselected requires the proof jobs to be skipped);
- container changes remain validation-only (no `docker push`, no attestation, no `packages: write` or `id-token: write`);
- runtime images referenced by digest from `delivery/images.lock.toml`, never tags;
- a new domain's gate is added to branch protection as a required check.

## 9. Validation

`python3 -m tools.deliveryctl validate`, `python3 -m tools.deliveryctl architecture-check` and `python3 -m tools.deliveryctl visual --check` must pass; the deliveryctl unit suite (`python3 -m unittest discover -s tools/deliveryctl/tests -p 'test_*.py' -v`) must pass; running actionlint must report zero workflow syntax errors.
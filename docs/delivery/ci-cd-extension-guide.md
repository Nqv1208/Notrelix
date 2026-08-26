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

## 1. Registration versus architecture change

A normal product addition should be a registration in `delivery/catalog.toml` and, only when routing/proof semantics differ, `delivery/policy.toml`.

The normal extension path is:

```text
catalog/policy registration
 -> deliveryctl compiles resolved contract
 -> existing provider executes contract
 -> existing evidence/release machinery remains unchanged
```

Do not add component-specific branches to `.github/workflows/ci.yml`.

## 2. Add a frontend host

Register provider `frontend-host`, source roots, workspace, build/E2E scripts, artifact paths and container/deployment metadata when deployable. The planner creates host/container matrix entries automatically.

E2E must consume the exact build artifact through the generic Node package/restore primitives.

## 3. Add a mobile app

Use provider `mobile` when the current proof contract fits. Keep `deployable = false` until an actual mobile artifact/delivery provider exists; do not model app-store artifacts as Docker images.

## 4. Add backend/worker runtime

If the current backend proof contract applies, register it. If runtime/build/test semantics genuinely differ, add one reusable provider type and corresponding resolved-plan serialization/proof profile. Do not duplicate delivery authority parsing in the new provider.

## 5. Add a proof

Proof vocabulary belongs in `delivery/policy.toml` proof profiles/bindings. The provider emits exactly that proof ID; the planner resolves expectations; the evidence aggregator remains generic.

Do not hard-code the proof into the final CI gate.

## 6. Add an environment

For the current Compose adapter, register the environment in `delivery/environments.toml` and create a matching GitHub Environment with its protection/secrets/variables. Manual-promotion environments reuse `promote-release.yml`.

A new deployment platform is an adapter extension, not a CI rewrite.

## 7. Open/closed boundary

Routine product growth should leave these contracts closed:

```text
.github/workflows/ci.yml
tools/deliveryctl/evidence.py
release manifest semantics
generic deployment adapter contract
Notrelix CI Gate
```

A new capability may extend `tools/deliveryctl/planner.py` only when the canonical execution-plan schema genuinely needs a new provider contract. It must not create a parallel planner/parser.

## 8. Runtime rule for new jobs

There is no repository CI Python bootstrap.

- Control-plane jobs use system Python >=3.11 and invoke `python3 -m tools.deliveryctl ...`.
- Backend jobs use the backend toolchain.
- Frontend/renderer jobs use Node/pnpm/Playwright.
- Execution providers do not invoke deliveryctl or parse `delivery/*.toml`.

If a new execution helper needs to decide *what* should run, move that decision into deliveryctl and pass the result as provider input.

## 9. Review checklist

Before registering a deployable, confirm:

- source ownership/root;
- provider and proof profile;
- security domain;
- build artifact;
- container context/Dockerfile/image identity;
- immutable build-image locks;
- Compose/deployment service and env var;
- health contract;
- release intent;
- migration/stateful implications.

`python3 -m tools.deliveryctl validate` must reject invalid or conflicting registration.

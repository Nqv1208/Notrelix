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
review_on:
  - ci-cd-architecture-change
  - new-component-registration
---

# Notrelix CI/CD V4 — Extension Guide

This guide defines whether a future change is a registration, a provider extension, or a genuine architecture change.

## 1. Add a frontend host using the existing provider

Expected changes:

```text
frontend/apps/<host>/...
delivery/catalog.toml
(optional) delivery/policy.toml for special routing semantics
(optional) Dockerfile / Compose service when deployable
planner regression test when semantics are new
```

Do **not** edit `ci.yml` or the final gate.

Register:

- component ID;
- provider = `frontend-host`;
- root/workspace;
- proof profile;
- build/E2E artifact metadata;
- container/deployment metadata if deployable.

The planner adds it to the host/container matrices and resolves its proof set from the registered `proof_profile`; no planner proof-ID branch is added.

## 2. Add a mobile app

Use provider `mobile` when the current mobile proof contract is sufficient. Mark `deployable = false` until a real mobile delivery provider (EAS/App Store/Play Store/OTA) exists.

Adding mobile CD later should add an artifact/delivery provider rather than pretending an IPA/AAB is a Docker image.

## 3. Add a containerized backend/worker

If it shares the current backend proof model, register it under that provider and give it a container contract. If it has a fundamentally different build/runtime/test stack, add a new reusable provider lane and a new proof profile.

The release manifest remains generic either way.

## 4. Add a new CI proof

Examples: mutation tests, license policy, performance budget, accessibility, IaC scanner.

The proof vocabulary belongs in `delivery/policy.toml`:

```text
proof profile/binding
 -> provider implementation emits that exact proof ID
 -> planner resolves the profile
 -> exact evidence gate
```

Do not construct the new proof ID in `build-plan.py` and do not teach the final gate about it. Add a planner/contract regression test proving the profile/binding selects the proof.

## 5. Add a deployment environment

For another environment using the same Compose adapter and **manual-promotion** topology:

1. add one `delivery/environments.toml` entry;
2. create the GitHub Environment with the same name and its secrets/variables/protection;
3. use the existing generic `promote-release.yml` workflow with that environment name.

No environment-specific promotion workflow is added.

Adding another automatic stage to the release train (for example staging → QA → production) changes promotion topology and therefore requires an explicit release-workflow change. That is not misrepresented as data-only registration.

If an environment uses a different deployment platform, create another deployment adapter while preserving the same sealed manifest/evidence authority.

## 6. Move from Docker Compose to Kubernetes/ECS

This is an adapter change, not a CI rewrite.

Keep:

- catalog/component identities;
- affected plan;
- proof/evidence semantics;
- immutable image digests;
- release candidate manifest;
- staging-verification/promotion authority.

Replace:

- release overlay renderer;
- environment mutation adapter;
- platform-specific stack/health proof.

## 7. Add a new provider type

A new provider is justified only when the current provider contracts cannot represent the runtime/build/proof semantics cleanly.

Required work:

1. reusable workflow with typed `workflow_call` inputs;
2. stable proof IDs;
3. planner matrix serialization for the provider lane;
4. policy proof profile (proof IDs stay out of planner code);
5. evidence emission;
6. tests proving affected routing and unknown fail-safe behavior;
7. CI-layout rule if a new architectural invariant is introduced.

This is extension of a stable core, not a rewrite of the core planner or release manifest.

## 8. Catalog review checklist

Every registered deployable must answer:

- Who owns its source root?
- Which provider proves it?
- Which dependency/security domain owns it?
- What artifact is produced?
- How is it runtime-smoked?
- What immutable image/build dependency inputs exist?
- Which Compose/platform service consumes it?
- What health endpoint proves startup?
- Does changing it create release intent?

`validate-delivery.py` should reject missing or conflicting answers whenever they can be mechanically checked.

## 9. Open/closed rule

A normal product addition should be **open for extension** through catalog/policy/provider data while the following remain **closed for modification**:

```text
.github/workflows/ci.yml
scripts/ci/aggregate-evidence.py
release-manifest schema semantics
generic deployment authority
Notrelix CI Gate contract
```

If a routine new app requires edits to several of those files, treat that as an architecture regression and fix the abstraction rather than accepting permanent special cases.

## 10. CI runtime when adding a new job

Any workflow job that invokes `scripts/ci/*.py` must:

1. use `actions/checkout` (full SHA);
2. include `uses: ./.github/actions/setup-ci-python` immediately after checkout;
3. then proceed with other setup and logic.

Do not rely on the host runner's system Python. The CI Python authority is `.python-version` at repository root, consumed by `setup-ci-python`.

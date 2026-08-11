---
title: "Contract Compatibility Runbook"
document_class: handbook
normative: true
owner: operations
maturity: FROZEN
conformance: CANONICAL
applies_to: runtime
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Contract Compatibility Runbook

Use when frontend/generated client, API, realtime or integration-event producer/consumer versions disagree.

## Diagnose
Identify exact deployed producer/consumer versions/SHAs and contract artifact version. Determine whether failure is additive-field tolerance, removed/renamed field, enum/discriminator unknown value, route/version mismatch, realtime event identity/payload or stale generated client/cache/CDN asset.

## Contain
Prefer restoring a compatible producer/consumer pair or re-enabling compatibility adapter. Do not patch generated files in production source or loosen validation globally. If old frontend versions are still active, backend must maintain promised compatibility or explicitly force/restrict upgrade according to product policy.

## Recover
Implement additive compatibility or coordinated rollout, regenerate artifacts, run drift/contract tests and deploy in safe order. Verify old and new supported clients through representative calls/events before removing shim.

Record why pre-deploy compatibility gates did not detect the mismatch.


## Decision tree

If only generated frontend assets are stale but backend still supports their contract, restore/regenerate the client bundle; if producer removed a required field/enum behavior, restore compatibility at the producer first; if a new event consumer rejects unknown versions, pause that consumer while a version-aware mapper is deployed; if database schema incompatibility caused API shape failure, follow the migration recovery plan rather than editing generated clients.

## Verification matrix

Exercise the oldest still-supported client/consumer and current version against the repaired producer. Confirm auth/tenant/error behavior as well as happy payload shape. Monitor incompatibility/error telemetry until old cached/mobile clients age out according to the supported-version policy.

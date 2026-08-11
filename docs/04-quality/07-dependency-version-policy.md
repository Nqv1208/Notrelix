---
title: "Dependency and Version Policy"
document_class: handbook
normative: true
owner: engineering-quality
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Dependency and Version Policy

Dependencies add maintenance, security and compatibility surface.

## QLT-DEP-101 — New dependency needs ownership and boundary fit

Before adding a library/package, verify existing capability cannot meet the need, license/security posture is acceptable, bundle/runtime impact is understood and the dependency belongs in the correct project/package family.

## QLT-DEP-102 — Lockfiles are part of reproducible build

Manifest and lockfile changes land together. CI uses frozen/immutable lockfile semantics where supported. A stale lockfile is fixed by regenerating it with the canonical package manager, not by weakening CI.

## QLT-DEP-103 — Major upgrades are compatibility changes

Review release/migration notes, generated outputs, runtime/platform support and affected tests. Do not bulk-upgrade unrelated foundational dependencies inside a feature change unless required and explicitly scoped.

## QLT-DEP-104 — Provider SDK types do not leak inward

Infrastructure/runtime adapters isolate vendor APIs. This reduces upgrade blast radius and preserves pure/application boundaries.

## Removal

Delete unused dependency references and obsolete adapters after migration. Retaining unused providers “for maybe later” increases attack/build surface.

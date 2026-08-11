---
title: "Git, Branch and Pull Request Policy"
document_class: handbook
normative: true
owner: engineering-quality
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Git, Branch and Pull Request Policy

Version-control policy protects reviewability and exact-SHA certification.

## QLT-GIT-101 — One branch/change has coherent semantic scope

Separate unrelated dependency upgrades/refactors from product changes when they increase review risk. A cross-layer vertical feature is coherent when all layers are required for one capability transaction.

## QLT-GIT-102 — Commits remain build/review friendly where practical

Use messages that describe intent, not file activity. Do not rewrite shared remote history without explicit coordination. Local unpushed commits may be reorganized when needed before review.

## QLT-GIT-103 — Protected branch promotion requires required checks

Normal work flows through the repository's configured integration/protected branches. The branch configuration in GitHub is executable authority for exact names/check requirements; documentation must not invent a different live policy. Release/freeze claims reference the exact green SHA.

## PR/change description

Include semantic owner, problem/result, contract/schema/event impact, rollout/migration, tests/gates and architecture exception/ADR links when applicable. Reviewers should be able to identify blast radius without reconstructing it from dozens of files.

## Generated artifacts

Commit generated/lockfile/migration artifacts when repository policy treats them as source-controlled evidence. Never edit generated outputs without changing their source/generator.

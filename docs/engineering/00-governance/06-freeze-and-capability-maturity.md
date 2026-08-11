---
title: "Freeze and Capability Maturity"
document_class: handbook
normative: true
owner: architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Freeze and Capability Maturity

Freeze means a foundation/capability has stable enough contracts, ownership, dependency boundaries and verification that teams can build in parallel **without reopening the base for local convenience**. Freeze does not mean “no future change”.

## Maturity states

- **Experimental:** ownership/contracts may move; no compatibility promise beyond explicit consumers.
- **Stabilizing:** target architecture chosen; remaining debt/exceptions tracked; new work follows target.
- **Frozen:** public/architecture contracts are protected; change requires impact/migration review.

## Freeze evidence

A frozen area needs:

1. explicit owner/responsibility boundary;
2. canonical rules/semantics;
3. executable dependency/behavior proof;
4. contract/schema compatibility policy;
5. no unknown critical exceptions;
6. CI that executes relevant suites on the target SHA.

## What freeze forbids

- new bypasses because “feature delivery is urgent”;
- copying transitional patterns into new code;
- widening shared/platform/foundation scope without architecture decision;
- changing public/event/schema contracts without migration/consumer inventory;
- marking a capability frozen merely to silence a gate.

---
title: "Code Review Contract"
document_class: handbook
normative: true
owner: engineering-quality
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Code Review Contract

Review is the human gate for semantics automation cannot fully infer.

## Review order

1. product/bounded-context semantics;
2. security, authorization and tenant scope;
3. architecture ownership/dependencies;
4. consistency/concurrency/idempotency/migration impact;
5. API/realtime compatibility;
6. tests/gates and failure behavior;
7. maintainability/performance/accessibility;
8. style only after substantive correctness.

## QLT-REV-101 — Reviewer checks the change boundary, not just diff lines

Inspect callers/consumers, migrations, generated artifacts, events, query keys, public exports and CI gates affected by the changed contract. A locally correct file can still break a system contract.

## QLT-REV-102 — Existing pattern is evidence, not automatic approval

If source conflicts with canonical docs, classify it as approved evolution, transitional exception, stale docs or regression. Do not repeat a questionable pattern merely because it exists nearby.

## QLT-REV-103 — Material choices must be explicit

The PR/change description records architecture/product/security decisions that were necessary. A reviewer should not have to infer why a boundary changed from code alone.

## Review comments

Block on correctness/architecture/security/compatibility. Mark optional readability preferences as non-blocking. Avoid forcing unrelated refactors into the change unless needed to make the requested transaction safe.

## Approval evidence

Approval is meaningful only after required generated files/migrations/tests are present. Review before CI is useful but does not waive gates.

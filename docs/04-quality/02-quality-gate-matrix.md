---
title: "Quality Gate Matrix"
document_class: handbook
normative: true
owner: engineering-quality
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Quality Gate Matrix

This matrix maps change classes to minimum proof. Technology handbooks may add stricter gates.

| Change class | Minimum proof |
|---|---|
| Domain aggregate/value semantics | Domain behavior tests + architecture project |
| Application request/pipeline | Application tests + affected Domain/Infrastructure as applicable |
| EF mapping/schema/RLS | Infrastructure + migration validation + integration/RLS |
| Platform messaging/idempotency/order | Platform tests + integration production graph |
| API endpoint/contract | API tests + OpenAPI drift + affected integration |
| Backend project dependency | architecture tests |
| Frontend package dependency/export | dependency-rules + TypeScript/lint |
| Frontend server-state/query/realtime | state tests + transition/realtime scenarios |
| Mobile production path | mobile tests + native dependency gate |
| Generated contract/client | codegen/drift + consumers compile/test |
| Security/authorization/tenant scope | focused authz/RLS/negative tests + threat review when boundary changes |
| Migration/destructive data change | migration plan + upgrade/rollback/forward-recovery evidence |
| CI/gate definition | self-test proving intended suites execute non-zero work |

## QLT-GATE-101 — No bypass as implementation strategy

Disabling a rule, suppressing a warning, weakening a test filter or adding broad exclusions is not a fix unless the canonical architecture decision itself changed. Any temporary bypass requires an architecture exception with owner, scope, expiry/removal condition and compensating evidence.

## QLT-GATE-102 — Exact revision matters

Release/freeze certification references the exact commit SHA whose required jobs passed. “Same branch was green earlier” is not release evidence after new commits.

## QLT-GATE-103 — Gate ownership follows protected property

Architecture tests own dependency invariants; contract drift owns generated compatibility; integration tests own provider/database protocol semantics. Avoid duplicating the same check in many jobs unless defense-in-depth is intentional and cheap.

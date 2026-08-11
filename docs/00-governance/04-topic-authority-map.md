---
title: "Topic Authority Map"
document_class: context
normative: true
owner: architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Topic Authority Map

| Topic | Canonical owner |
|---|---|
| documentation/decision authority | `00-governance/00-documentation-authority.md` |
| system boundaries/context map | `01-system/00-system-overview.md`, `01-context-map-and-ownership.md` |
| tenant/resource scope | `01-system/02-tenancy-and-resource-scope.md` |
| security/authorization | `01-system/03-security-and-authorization.md` + backend authorization handbook |
| REST/realtime/events/versioning | `01-system/04-contract-boundaries.md` through `08-contract-versioning.md` |
| Domain modeling | `02-backend/01-domain-modeling.md` |
| Application structure/pipeline | `02-backend/03-application-vertical-slices.md`, `04-application-pipeline.md` |
| persistence/RLS/migrations | `02-backend/06-infrastructure-persistence.md`, `07-database-migrations-and-rls.md` |
| Platform/messaging/idempotency | `02-backend/09-platform-messaging-idempotency.md` |
| frontend dependency model | `03-frontend/01-package-dependency-model.md` |
| frontend query/cache/state | `03-frontend/05-query-state-cache.md` |
| frontend realtime | `03-frontend/06-realtime.md` |
| product bounded-context semantics | `08-product/contexts/<context>.md` |
| CI/testing/quality gates | `04-quality/01-testing-strategy.md`, `02-quality-gate-matrix.md` |
| rollout/release | `05-delivery/03-release-rollout-feature-flags.md` |
| incidents/recovery | `06-operations/` |

If a topic appears elsewhere, that document must either link to this owner or discuss a different local operational concern.


## Ownership rule

A topic owner is responsible for definitions and normative detail. Other documents may provide a local checklist or consequence, but link back instead of restating the full rule. When a change seems to require editing several documents with the same paragraph, first identify the single semantic owner and reduce the others to references.

Cross-topic changes can legitimately update several owners—for example a new Work Management event can update Work Management semantics, system event/versioning contract and Automation consumer semantics—but each file owns a different decision surface rather than copying the same text.

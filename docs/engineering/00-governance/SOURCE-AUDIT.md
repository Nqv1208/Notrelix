---
title: "Source and Legacy Documentation Audit"
document_class: context
normative: false
owner: architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Source and Legacy Documentation Audit

## Baseline

V4 was authored against repository baseline `main @ 4b60c332a36227b80cb0c19b385de8ed3c23ebf8` and the public repository topology was rechecked on 2026-08-10. The baseline was selected because it includes the backend foundation and frontend freeze line previously used for the canonical documentation work.

## Current source evidence retained

Backend remains a five-production-project modular monolith: Domain, Application, Infrastructure, Platform and API, with dedicated Domain/Application/Infrastructure/Platform/API/Integration/Architecture test projects and testing-support projects. Current project references continue to support the dependency model documented by the backend handbook.

Frontend source is a pnpm/Turborepo workspace with three app hosts (`web`, `mobile`, `marketing`) and package families for foundation, runtimes, UI, product, features and tooling. The web host is Vite-based; mobile is Expo/React Native; marketing is a separate host. Work Management's `core → state → web/mobile` packaging is current structural evidence for the product-slice model.

## Legacy documentation classified as stale

The repository's older root README/AGENTS/RULE material contains a previous frontend description based on a Next.js/Bun-style single application and older folder conventions. Those passages are historical evidence only and MUST NOT be copied as current frontend architecture. V4 routes current frontend truth to `frontend/`, `03-frontend/`, package manifests and dependency-rules.

Several older root/backend rule documents also combined product semantics, database table notes, implementation recipes and global architecture into one file. Their location/authority model is superseded by V4, but useful semantics were retained when they still align with the frozen system direction.

## Retained legacy ideas and new canonical owners

| Useful prior idea | V4 canonical owner |
|---|---|
| Board is a work database, not only Kanban | `08-product/contexts/work-management.md` |
| View stores configuration over shared item data | Work Management context |
| BoardGroup is not Kanban status | Work Management context |
| FieldType owns settings/value/filter/sort/render contract | Work Management context |
| People/Relation validate external targets in Application | Work Management + backend Application/Domain docs |
| Page + Block structured documents | Documents context |
| Resource links do not transfer authorization | Documents/Governance/System security docs |
| Automation through durable outbox/background processing | Automation + backend Platform docs |
| Automation execution idempotency | Automation + Platform docs |
| ActivityLog differs from AuditLog | Collaboration + Governance contexts |
| Notifications have explicit recipients | Collaboration context |
| Backend final permission authority | root/backend constitutions + Governance/System security |
| Query endpoints authorize | Governance + backend authz/API docs |
| Domain failure atomicity/no-op/version/event discipline | backend Domain Modeling |
| Application pipeline owns authz/concurrency/idempotency/commit | backend Application Pipeline |
| consumer dedup/order/poison identity | backend Platform Messaging |
| frontend realtime complements authoritative queries | frontend Query/Realtime docs |

## Evidence rule

Source/tests/manifests/CI prove what the repository currently does. They do not automatically override a canonical decision. A discrepancy is classified as accepted evolution, transitional exception, stale documentation or regression; material source and docs are repaired in the same delivery transaction.

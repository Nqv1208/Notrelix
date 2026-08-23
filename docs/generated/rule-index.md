---
document_id: DOC-GEN-RULE-INDEX
document_type: generated
status: generated
owner: documentation-governance
applies_to:
  - repository-rule-inventory
  - architecture-rule-discovery
  - documentation-governance-evidence
evidence:
  - scripts/docs/generate-rule-index.mjs
  - scripts/docs/check-rule-ids.mjs
  - docs/governance/documentation-authority.md
  - docs/governance/documentation-quality-gates.md
review_on:
  - canonical-rule-added
  - canonical-rule-removed
  - canonical-rule-renamed
  - rule-namespace-change
  - rule-index-generator-change
---

# Notrelix Rule Index

<!-- GENERATED FILE — DO NOT EDIT. -->
<!-- Producer: scripts/docs/generate-rule-index.mjs -->
<!-- Source: stable rule headings in canonical authored documentation -->
<!-- Regenerate: node scripts/docs/generate-rule-index.mjs -->
<!-- Check drift: node scripts/docs/generate-rule-index.mjs --check -->

> This file is generated discovery evidence.
> Rule meaning remains owned by the canonical source document in which the rule is declared.

Rule count: 2663

## Namespace summary

| Namespace | Count |
|:---|---:|
| `ACC` | 20 |
| `ANA` | 38 |
| `AUT` | 35 |
| `BE-API` | 59 |
| `BE-APP` | 65 |
| `BE-DEC` | 20 |
| `BE-DOM` | 52 |
| `BE-INF` | 72 |
| `BE-OPS-CFG` | 50 |
| `BE-OPS-DATA` | 67 |
| `BE-PLT` | 63 |
| `BE-SEC` | 59 |
| `BE-TST` | 67 |
| `BIL` | 33 |
| `COL` | 29 |
| `DCT` | 31 |
| `DEC` | 26 |
| `DEL-CHG` | 40 |
| `DEL-CON` | 40 |
| `DEL-DEV` | 41 |
| `DEL-DONE` | 44 |
| `DEL-MIG` | 46 |
| `DEL-OWN` | 40 |
| `DEL-REL` | 41 |
| `DOC` | 18 |
| `FE-API` | 60 |
| `FE-ARCH` | 72 |
| `FE-ARCH-CHG` | 74 |
| `FE-DEC` | 35 |
| `FE-DEP` | 79 |
| `FE-HOST` | 79 |
| `FE-RT` | 68 |
| `FE-STATE` | 81 |
| `FE-TST` | 78 |
| `FE-UI` | 80 |
| `GOV` | 24 |
| `ID` | 18 |
| `INFRA-CTR` | 70 |
| `INFRA-ENV` | 40 |
| `INFRA-RUN` | 52 |
| `INT` | 38 |
| `NRX` | 18 |
| `OPS-DEG` | 57 |
| `OPS-INC` | 40 |
| `OPS-OBS` | 40 |
| `OPS-REC` | 45 |
| `PROD` | 24 |
| `PROD-UX` | 15 |
| `QLT` | 46 |
| `QLT-A11Y` | 45 |
| `QLT-PERF` | 43 |
| `QLT-SEC` | 51 |
| `QLT-TST` | 45 |
| `SYS` | 20 |
| `SYS-ACT` | 1 |
| `SYS-AUD` | 1 |
| `SYS-CON` | 12 |
| `SYS-CTX` | 5 |
| `SYS-DATA` | 20 |
| `SYS-EVT` | 11 |
| `SYS-EXT` | 16 |
| `SYS-NOTIF` | 1 |
| `SYS-OBS` | 1 |
| `SYS-RT` | 4 |
| `WM` | 38 |
| `WSP` | 20 |

## Rule inventory

| Rule ID | Namespace | Rule | Source |
|:---|:---|:---|:---|
| `ACC-001` | `ACC` | Account is not a Workspace | [`docs/product/accounts.md`](../product/accounts.md) |
| `ACC-002` | `ACC` | Account scope is explicit | [`docs/product/accounts.md`](../product/accounts.md) |
| `ACC-003` | `ACC` | Source placement does not automatically expand Account ownership | [`docs/product/accounts.md`](../product/accounts.md) |
| `ACC-004` | `ACC` | Account lifecycle is not Billing lifecycle | [`docs/product/accounts.md`](../product/accounts.md) |
| `ACC-005` | `ACC` | Suspension effects are coordinated, not hidden cascades | [`docs/product/accounts.md`](../product/accounts.md) |
| `ACC-006` | `ACC` | Account deletion is not generic soft delete | [`docs/product/accounts.md`](../product/accounts.md) |
| `ACC-007` | `ACC` | Account membership and Workspace membership are distinct | [`docs/product/accounts.md`](../product/accounts.md) |
| `ACC-008` | `ACC` | Account role is not universal authorization | [`docs/product/accounts.md`](../product/accounts.md) |
| `ACC-009` | `ACC` | Account invitation does not imply Workspace membership | [`docs/product/accounts.md`](../product/accounts.md) |
| `ACC-010` | `ACC` | Domain claim and Identity authentication are separate | [`docs/product/accounts.md`](../product/accounts.md) |
| `ACC-011` | `ACC` | Account IdP configuration does not make Accounts the Identity context | [`docs/product/accounts.md`](../product/accounts.md) |
| `ACC-012` | `ACC` | Provisioning orchestrates ownership; it does not absorb it | [`docs/product/accounts.md`](../product/accounts.md) |
| `ACC-013` | `ACC` | Region is an Account policy/input, not a silent data migration | [`docs/product/accounts.md`](../product/accounts.md) |
| `ACC-014` | `ACC` | Workspace routing does not make Accounts the Workspace owner | [`docs/product/accounts.md`](../product/accounts.md) |
| `ACC-015` | `ACC` | Administrative invariant is not generic authorization policy | [`docs/product/accounts.md`](../product/accounts.md) |
| `ACC-016` | `ACC` | Billing is the authoritative commercial owner | [`docs/product/accounts.md`](../product/accounts.md) |
| `ACC-017` | `ACC` | Backend is final authority for Account administration | [`docs/product/accounts.md`](../product/accounts.md) |
| `ACC-018` | `ACC` | Account events carry stable scope, not full object graphs | [`docs/product/accounts.md`](../product/accounts.md) |
| `ACC-019` | `ACC` | Cross-context consequence does not transfer ownership | [`docs/product/accounts.md`](../product/accounts.md) |
| `ACC-020` | `ACC` | High-impact Account changes do not silently overwrite stale state | [`docs/product/accounts.md`](../product/accounts.md) |
| `ANA-001` | `ANA` | Analytics is derived state | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-002` | `ANA` | Analytical control is not hidden source mutation | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-003` | `ANA` | Metric definition is versioned business semantics | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-004` | `ANA` | One named Metric has one semantic owner | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-005` | `ANA` | Analytics source is explicit | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-006` | `ANA` | Dashboard owns visualization configuration, not product facts | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-007` | `ANA` | Dashboard tenant scope is explicit | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-008` | `ANA` | Dashboard visibility does not bypass source authorization | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-009` | `ANA` | Dashboard Source preserves source owner | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-010` | `ANA` | Widget Type has a validated configuration contract | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-011` | `ANA` | Analytics layout order never mutates source order | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-012` | `ANA` | Aggregation unit and denominator are explicit | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-013` | `ANA` | Time zone is part of Metric/report semantics when relevant | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-014` | `ANA` | Freshness is explicit | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-015` | `ANA` | Reporting Snapshot is captured derived truth, not current source authority | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-016` | `ANA` | Snapshot schema evolution is migration-aware | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-017` | `ANA` | Source deletion does not use accidental SQL cascade into Analytics | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-018` | `ANA` | Projection has one or more named authoritative sources | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-019` | `ANA` | Projection rebuild is a supported correctness path | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-020` | `ANA` | Backfill cannot double-count live projection traffic | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-021` | `ANA` | Analytics does not full-scan arbitrary flexible JSON per dashboard request at enterprise scale | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-022` | `ANA` | Analytics cannot issue commercial charges or entitlements | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-023` | `ANA` | Aggregation does not erase confidentiality | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-024` | `ANA` | Cross-tenant analytics is explicit privileged product capability | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-025` | `ANA` | Analytics cache keys preserve tenant + semantic version | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-026` | `ANA` | Export uses the same metric and authorization semantics | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-027` | `ANA` | Visually precise number requires semantically precise definition | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-028` | `ANA` | Historical comparability is not assumed across breaking Metric versions | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-029` | `ANA` | Null, zero, unknown, and unauthorized are distinct | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-030` | `ANA` | Multi-source report exposes meaningful completeness/freshness | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-031` | `ANA` | Aggregate visibility does not imply detail visibility | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-032` | `ANA` | Widget configuration is validated against source semantics | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-033` | `ANA` | Dashboard deletion is non-destructive to source contexts | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-034` | `ANA` | Product contract may be ahead of implementation, but current source gaps remain explicit | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-035` | `ANA` | Operational telemetry is not product Analytics by default | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-036` | `ANA` | Realtime analytics updates remain reconcilable | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-037` | `ANA` | Derived data follows source security/data-location obligations | [`docs/product/analytics.md`](../product/analytics.md) |
| `ANA-038` | `ANA` | Historical Snapshot and rebuilt current projection can legitimately differ | [`docs/product/analytics.md`](../product/analytics.md) |
| `AUT-001` | `AUT` | Automation Rule has explicit valid configuration | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-002` | `AUT` | Invalid configuration cannot execute | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-003` | `AUT` | Disabled Rule stops new executions without erasing history | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-004` | `AUT` | Execution uses an explicit Rule version/config snapshot | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-005` | `AUT` | Trigger identity is stable and contract-driven | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-006` | `AUT` | Source transaction does not execute Automation side effects inline | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-007` | `AUT` | Schedule intent is product state; clock/scheduler is infrastructure | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-008` | `AUT` | Time zone and daylight-saving semantics are explicit | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-009` | `AUT` | One logical trigger occurrence creates one logical Execution | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-010` | `AUT` | Condition evaluation is deterministic at a defined consistency point | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-011` | `AUT` | Condition failure is not execution failure | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-012` | `AUT` | Actions use normal capability contracts | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-013` | `AUT` | Action configuration is typed, not arbitrary unvalidated JSON | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-014` | `AUT` | Action dependency semantics are explicit | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-015` | `AUT` | Automation does not absorb Integrations | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-016` | `AUT` | Execution identity is stable and idempotent | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-017` | `AUT` | Execution state advances after durable action outcome | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-018` | `AUT` | Retry never means repeat side effect blindly | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-019` | `AUT` | Execution history is product evidence, not transport log | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-020` | `AUT` | Automation never silently escalates privilege | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-021` | `AUT` | Automation history and Audit remain distinct | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-022` | `AUT` | Recursive Automation is bounded | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-023` | `AUT` | Billing limit does not own Automation lifecycle | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-024` | `AUT` | Execution claim concurrency cannot duplicate logical work | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-025` | `AUT` | Target context revalidates at action time | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-026` | `AUT` | Automation Template is creation input, not live shared Rule authority | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-027` | `AUT` | Agent capability requires explicit product admission | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-028` | `AUT` | Automation actions are allow-listed capabilities | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-029` | `AUT` | Automation configuration never stores reusable provider secrets | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-030` | `AUT` | Execution facts use logical identity, not worker attempt identity | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-031` | `AUT` | Realtime progress is recoverable from durable Automation state | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-032` | `AUT` | Raw provider webhook does not trigger product Automation directly | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-033` | `AUT` | Automation cannot casually modify authorization policy | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-034` | `AUT` | Broken Automation reference is explicit | [`docs/product/automation.md`](../product/automation.md) |
| `AUT-035` | `AUT` | Execution failure reason is actionable and safely redacted | [`docs/product/automation.md`](../product/automation.md) |
| `BE-API-001` | `BE-API` | Endpoint is a transport adapter | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-002` | `BE-API` | Endpoint grouping follows public capability, not business ownership authority | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-003` | `BE-API` | Public DTO is explicit transport shape | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-004` | `BE-API` | Transport parsing completes before business mutation | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-005` | `BE-API` | Authentication precedes protected Application request | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-006` | `BE-API` | API endpoint access metadata does not replace Application authorization | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-007` | `BE-API` | Anonymous is an access mode, not “no security” | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-008` | `BE-API` | HTTP context is translated, not leaked inward | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-009` | `BE-API` | Correlation value is safe and bounded | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-010` | `BE-API` | CSRF is a host boundary separate from Application authorization | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-011` | `BE-API` | CSRF bypass is explicit and narrowly justified | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-012` | `BE-API` | Credentialed CORS configuration is explicit and environment-safe | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-013` | `BE-API` | Security audit records safe semantic metadata | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-014` | `BE-API` | API rate limit uses transport-visible partition only | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-015` | `BE-API` | Rate-limit key is not exposed secret identity | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-016` | `BE-API` | Rate-limit failure mode is risk-classified | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-017` | `BE-API` | Idempotency header does not create idempotent semantics by itself | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-018` | `BE-API` | Retry of one logical HTTP operation preserves the same idempotency key | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-019` | `BE-API` | Same semantic failure maps consistently across endpoints | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-020` | `BE-API` | Public error code is semantic and stable | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-021` | `BE-API` | OpenAPI is generated from the API producer | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-022` | `BE-API` | Generated OpenAPI diff is reviewed, not blindly accepted | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-023` | `BE-API` | Producer and generated consumer change atomically at contract level | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-024` | `BE-API` | Version changes when supported consumer compatibility requires it | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-025` | `BE-API` | Old API version remains a supported consumer contract until retirement criteria are met | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-026` | `BE-API` | Route does not define aggregate boundary | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-027` | `BE-API` | GET/HEAD remain safe from business mutation | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-028` | `BE-API` | Unbounded input is rejected before expensive protected work | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-029` | `BE-API` | Pagination preserves stable order and tenant authorization | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-030` | `BE-API` | Filter/sort cannot expand visible scope | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-031` | `BE-API` | Bulk response reports semantic per-operation outcome honestly | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-032` | `BE-API` | Accepted means accepted, not completed | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-033` | `BE-API` | Concurrency conflict is stable public outcome | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-034` | `BE-API` | Serializer configuration is part of public compatibility | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-035` | `BE-API` | Public identity format remains stable across persistence refactor | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-036` | `BE-API` | Credential transport has one intentional trust model | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-037` | `BE-API` | Capability token is scoped and revocable according to Product contract | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-038` | `BE-API` | Health endpoint is bounded and side-effect free | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-039` | `BE-API` | Admin endpoint is protected by explicit server policy | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-040` | `BE-API` | Internal network is not identity | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-041` | `BE-API` | Webhook authenticity/replay validation precedes Application effect | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-042` | `BE-API` | REST read remains authoritative reconciliation path unless another owner explicitly replaces it | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-043` | `BE-API` | Security metadata drift is a contract/security defect | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-044` | `BE-API` | Deprecation has a replacement/removal policy | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-045` | `BE-API` | Every production endpoint is discoverable through composition/OpenAPI/access gate as appropriate | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-046` | `BE-API` | Composition dependency does not transfer business ownership to API | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-047` | `BE-API` | Misconfigured critical security host fails safe | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-048` | `BE-API` | Middleware order follows trust/context prerequisites | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-049` | `BE-API` | One public error mapping path avoids endpoint-specific drift | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-050` | `BE-API` | HTTP cacheability is reviewed independently from server cache | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-051` | `BE-API` | Content-Disposition/content-type are validated public transport metadata | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-052` | `BE-API` | Upload acceptance state is honest | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-053` | `BE-API` | Client disconnect does not rewrite authoritative outcome | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-054` | `BE-API` | Metrics use bounded route/operation identity | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-055` | `BE-API` | API tests prove transport/public behavior, not substitute for Domain/Application tests | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-056` | `BE-API` | Contract test asserts semantics, not only snapshot bytes | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-057` | `BE-API` | Rejected request produces no protected effect | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-058` | `BE-API` | OpenAPI exclusion is explicit | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-API-059` | `BE-API` | Security mechanism change requires accepted ADR alignment | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-APP-001` | `BE-APP` | Application owns orchestration, not business truth | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-002` | `BE-APP` | Request type expresses intent | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-003` | `BE-APP` | New use cases follow the canonical module-first placement | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-004` | `BE-APP` | Application Common contains cross-cutting use-case mechanics only | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-005` | `BE-APP` | Cross-cutting behavior is declared, not reimplemented ad hoc | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-006` | `BE-APP` | Marker contract does not hide feature semantics | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-007` | `BE-APP` | Pipeline order follows dependency, not aesthetics | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-008` | `BE-APP` | Outer behavior cannot depend on inner transactional state | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-009` | `BE-APP` | Post-commit scope records intent; it does not execute side effects early | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-010` | `BE-APP` | Transaction policy is Application-owned; transaction technology is Infrastructure-owned | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-011` | `BE-APP` | Authorization happens before protected business effects | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-012` | `BE-APP` | Authentication identity is not authorization | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-013` | `BE-APP` | Client tenant/resource identifiers are inputs, not authority | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-014` | `BE-APP` | Resource resolution port is a read contract, not foreign mutation capability | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-015` | `BE-APP` | Application validation does not replace Domain invariant | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-016` | `BE-APP` | Application external-fact query is explicit in the use case | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-017` | `BE-APP` | Time is sampled deliberately | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-018` | `BE-APP` | Handler remains orchestration-focused | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-019` | `BE-APP` | Read optimization does not bypass authorization or source ownership | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-020` | `BE-APP` | Port is use-case language, not provider language | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-021` | `BE-APP` | Repository/port exists because the use case needs a persistence capability | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-022` | `BE-APP` | New handler-local DbContext usage is forbidden without a new governed exception/decision | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-023` | `BE-APP` | Required local state and durable enrollment commit atomically | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-024` | `BE-APP` | Distributed transaction is not simulated by holding DB transaction around provider call | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-025` | `BE-APP` | Conflict is returned before committed stale overwrite | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-026` | `BE-APP` | Idempotency identity is stable across retry | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-027` | `BE-APP` | Idempotency success follows authoritative success | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-028` | `BE-APP` | Commercial gate and resource permission are separate decisions | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-029` | `BE-APP` | Gate failure occurs before protected mutation | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-030` | `BE-APP` | System operation is explicit capability, not if IsSystem then bypass all | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-031` | `BE-APP` | Cacheability is a use-case contract | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-032` | `BE-APP` | Cache read/write cannot run ahead of authorization/transaction guarantees | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-033` | `BE-APP` | Post-commit work is classified by durability need | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-034` | `BE-APP` | Integration event mapping preserves semantic owner and stable identity | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-035` | `BE-APP` | Cross-context write routes to the target owner | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-036` | `BE-APP` | Cross-context read contract is minimal | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-037` | `BE-APP` | Application result does not expose Infrastructure exception | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-038` | `BE-APP` | Exception mapping is explicit and loss-aware | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-039` | `BE-APP` | Cancellation respects transaction/external-effect state | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-040` | `BE-APP` | Application use case is host-independent | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-041` | `BE-APP` | Ambient request context supplements, not replaces, explicit use-case input | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-042` | `BE-APP` | Audit, Activity, Notification, Domain Event are distinct outputs | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-043` | `BE-APP` | Realtime notification follows authoritative state | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-044` | `BE-APP` | Mock only the outer mechanism when testing Application semantics | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-045` | `BE-APP` | New pipeline behavior states its prerequisite and produced state | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-046` | `BE-APP` | Cross-cutting frequency alone is not enough; ordering semantics must be clear | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-047` | `BE-APP` | Outer implementation is injected through inward contract | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-048` | `BE-APP` | One Application use case may have multiple transports | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-049` | `BE-APP` | Provider unknown outcome is a semantic state when required | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-050` | `BE-APP` | Consistency model is explicit | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-051` | `BE-APP` | Query model does not become write model by convenience | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-052` | `BE-APP` | Application service remains use-case focused | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-053` | `BE-APP` | Mapping is structural, not business authorization/invariant execution | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-054` | `BE-APP` | Event contract captures the intended committed fact deterministically | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-055` | `BE-APP` | Invalid request contract fails before handler execution | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-056` | `BE-APP` | Observability cannot change use-case outcome | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-057` | `BE-APP` | Gate owner is explicit | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-058` | `BE-APP` | Execution mode changes mechanism, not product permission | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-059` | `BE-APP` | Application names lifecycle by product intent | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-060` | `BE-APP` | Bulk semantics are explicit | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-061` | `BE-APP` | Pagination contract is semantic before query optimization | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-062` | `BE-APP` | Plan display name is not capability policy | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-063` | `BE-APP` | Runtime configuration enters through typed outer contract | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-064` | `BE-APP` | Rejection leaves no committed protected partial effect | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-APP-065` | `BE-APP` | Performance optimization preserves use-case contract | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-DEC-001` | `BE-DEC` | ADR does not override newer canonical architecture silently | [`backend/docs/decisions/README.md`](../../backend/docs/decisions/README.md) |
| `BE-DEC-002` | `BE-DEC` | ADR ID is immutable | [`backend/docs/decisions/README.md`](../../backend/docs/decisions/README.md) |
| `BE-DEC-003` | `BE-DEC` | Registry records current ADR status | [`backend/docs/decisions/README.md`](../../backend/docs/decisions/README.md) |
| `BE-DEC-004` | `BE-DEC` | Never reserve ADR IDs speculatively in docs | [`backend/docs/decisions/README.md`](../../backend/docs/decisions/README.md) |
| `BE-DEC-005` | `BE-DEC` | Routine implementation following canonical rules needs no ADR | [`backend/docs/decisions/README.md`](../../backend/docs/decisions/README.md) |
| `BE-DEC-006` | `BE-DEC` | One decision has one owning ADR scope | [`backend/docs/decisions/README.md`](../../backend/docs/decisions/README.md) |
| `BE-DEC-007` | `BE-DEC` | Historical normalization does not change the decision | [`backend/docs/decisions/README.md`](../../backend/docs/decisions/README.md) |
| `BE-DEC-008` | `BE-DEC` | Current stewardship is not rewritten as historical authorship | [`backend/docs/decisions/README.md`](../../backend/docs/decisions/README.md) |
| `BE-DEC-009` | `BE-DEC` | Accepted decision requires implementation/current-doc evidence | [`backend/docs/decisions/README.md`](../../backend/docs/decisions/README.md) |
| `BE-DEC-010` | `BE-DEC` | Evidence update does not rewrite rationale | [`backend/docs/decisions/README.md`](../../backend/docs/decisions/README.md) |
| `BE-DEC-011` | `BE-DEC` | Supersession links are bidirectional | [`backend/docs/decisions/README.md`](../../backend/docs/decisions/README.md) |
| `BE-DEC-012` | `BE-DEC` | Proposed ADR is not merge permission by itself | [`backend/docs/decisions/README.md`](../../backend/docs/decisions/README.md) |
| `BE-DEC-013` | `BE-DEC` | Exception never becomes silent ADR | [`backend/docs/decisions/README.md`](../../backend/docs/decisions/README.md) |
| `BE-DEC-014` | `BE-DEC` | ADR Compatibility/Migration is durable consequence, not execution tracker | [`backend/docs/decisions/README.md`](../../backend/docs/decisions/README.md) |
| `BE-DEC-015` | `BE-DEC` | Pipeline concrete class count is evidence, not ADR identity | [`backend/docs/decisions/README.md`](../../backend/docs/decisions/README.md) |
| `BE-DEC-016` | `BE-DEC` | RLS bootstrap mechanism change can supersede ADR-002 | [`backend/docs/decisions/README.md`](../../backend/docs/decisions/README.md) |
| `BE-DEC-017` | `BE-DEC` | Credential-model change may invalidate CSRF decision assumptions | [`backend/docs/decisions/README.md`](../../backend/docs/decisions/README.md) |
| `BE-DEC-018` | `BE-DEC` | Numeric rate limits are runtime policy evidence, not immutable ADR identity | [`backend/docs/decisions/README.md`](../../backend/docs/decisions/README.md) |
| `BE-DEC-019` | `BE-DEC` | Registry is not manually duplicated elsewhere | [`backend/docs/decisions/README.md`](../../backend/docs/decisions/README.md) |
| `BE-DEC-020` | `BE-DEC` | Do not write decision after the fact merely to justify code | [`backend/docs/decisions/README.md`](../../backend/docs/decisions/README.md) |
| `BE-DOM-001` | `BE-DOM` | Domain remains framework/provider independent | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-002` | `BE-DOM` | Ambient time/random/user/provider state stays outside Domain | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-003` | `BE-DOM` | External fact supply does not transfer fact ownership to Domain | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-004` | `BE-DOM` | Aggregate boundary follows invariant ownership | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-005` | `BE-DOM` | Public child mutation cannot bypass root consistency | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-006` | `BE-DOM` | Do not create typed IDs mechanically per table | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-007` | `BE-DOM` | Identity type is not persistence ownership | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-008` | `BE-DOM` | Version changes exactly with accepted semantic mutation | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-009` | `BE-DOM` | Value object equality is structural semantic equality | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-010` | `BE-DOM` | Value object normalization is part of its semantic contract | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-011` | `BE-DOM` | One invariant has one enforcing semantic owner | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-012` | `BE-DOM` | Cross-aggregate Domain rule consumes facts, not repository callbacks | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-013` | `BE-DOM` | Rejected mutation is failure-atomic | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-014` | `BE-DOM` | No-op is decided after semantic normalization, before mutation | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-015` | `BE-DOM` | Audit metadata changes only with the mutation contract | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-016` | `BE-DOM` | Shared deletion mechanism requires context-specific admission | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-017` | `BE-DOM` | Deletion does not hide previous business state in a repair field by default | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-018` | `BE-DOM` | Domain event represents a completed owned fact | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-019` | `BE-DOM` | Event scope matches the fact | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-020` | `BE-DOM` | CLR class name is not the durable public event identity by default | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-021` | `BE-DOM` | Provider/transport fields do not pollute Domain event by default | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-022` | `BE-DOM` | Raised event payload is stable after raise | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-023` | `BE-DOM` | Rule code is semantic, not source-location identity | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-024` | `BE-DOM` | SharedKernel requires stable cross-context meaning | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-025` | `BE-DOM` | Common is not a business dumping ground | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-026` | `BE-DOM` | Aggregate references another root by immutable reference, not navigation ownership | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-027` | `BE-DOM` | Ordering primitive does not own product move semantics | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-028` | `BE-DOM` | Constructor/factory does not discover external facts | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-029` | `BE-DOM` | Collection mutation goes through intent method | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-030` | `BE-DOM` | Event payload reflects committed semantic state | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-031` | `BE-DOM` | Actor presence is not authorization | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-032` | `BE-DOM` | Tenant scope is immutable where the product identity requires it | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-033` | `BE-DOM` | Lifecycle guard precedes mutation | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-034` | `BE-DOM` | Generic update method is not a substitute for business transition | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-035` | `BE-DOM` | Aggregate size follows consistency, not object-graph convenience | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-036` | `BE-DOM` | Database constraint reinforces, not replaces, product invariant meaning | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-037` | `BE-DOM` | Stale conflict does not silently reapply business mutation | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-038` | `BE-DOM` | No-event mutation is allowed when no durable Domain fact contract is needed | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-039` | `BE-DOM` | Domain service remains pure | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-040` | `BE-DOM` | Domain does not discover authorization state | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-041` | `BE-DOM` | Domain validates owned semantics even if outer layer prevalidates | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-042` | `BE-DOM` | Domain operation has explicit data requirements | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-043` | `BE-DOM` | Identity generation does not replace business timestamp | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-044` | `BE-DOM` | Public event compatibility is not inferred from internal type compatibility | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-045` | `BE-DOM` | Test behavior, not implementation ceremony | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-046` | `BE-DOM` | Supplied aggregate facts have clear freshness requirement | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-047` | `BE-DOM` | Snapshot duplication is intentional contract, not shared mutable model | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-048` | `BE-DOM` | Domain is not the universal data-shape layer | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-049` | `BE-DOM` | Ordering failure is failure-atomic | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-050` | `BE-DOM` | Restore is a new transition against current facts | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-051` | `BE-DOM` | Addressability does not imply aggregate-root status | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-DOM-052` | `BE-DOM` | Domain test setup is small relative to the invariant | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-INF-001` | `BE-INF` | Infrastructure depends inward | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-002` | `BE-INF` | DbContext belongs to Infrastructure | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-003` | `BE-INF` | DbContext is not a cross-context integration API | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-004` | `BE-INF` | Persistence mapping does not bypass Domain API | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-005` | `BE-INF` | One table has one logical semantic owner | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-006` | `BE-INF` | Constraint failure maps to stable semantic outcome | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-007` | `BE-INF` | Index follows query/invariant evidence | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-008` | `BE-INF` | Provider substitution is architecture work | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-009` | `BE-INF` | Pending model changes are a failure to resolve, not a warning to suppress | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-010` | `BE-INF` | Migration changes real old state to the target meaning | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-011` | `BE-INF` | Destructive contraction waits for objective removal proof | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-012` | `BE-INF` | Migration privilege is separable from steady-state runtime | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-013` | `BE-INF` | Seed is environment-scoped and idempotent where repeatable | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-014` | `BE-INF` | RLS and Application authorization are both required where the architecture declares them | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-015` | `BE-INF` | RLS session context is connection-scoped state that must not leak | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-016` | `BE-INF` | Bootstrap RLS context is minimal, not a global bypass | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-017` | `BE-INF` | RLS lifecycle changes require connection-pooling/reuse proof | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-018` | `BE-INF` | RLS policy is context-aware persistence enforcement | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-019` | `BE-INF` | RLS test uses real PostgreSQL semantics | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-020` | `BE-INF` | Filter bypass is explicit and narrow | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-021` | `BE-INF` | System DB context is not a universal tenant bypass | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-022` | `BE-INF` | Persistence concurrency cannot silently overwrite newer state | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-023` | `BE-INF` | Infrastructure does not extend transaction around arbitrary external provider calls | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-024` | `BE-INF` | Interceptor cannot invent Domain event/business state | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-025` | `BE-INF` | Read model may denormalize, but source writes remain owned | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-026` | `BE-INF` | Cross-context read coupling is visible and replaceable | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-027` | `BE-INF` | Cache is never the sole authorization or product authority | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-028` | `BE-INF` | Authorization-sensitive cache is scope/version aware | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-029` | `BE-INF` | Cache fallback is capacity-aware and fail-safe | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-030` | `BE-INF` | Provider DTO does not leak into Domain/Application public semantics | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-031` | `BE-INF` | Adapter distinguishes transient failure, terminal rejection, and unknown outcome | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-032` | `BE-INF` | Provider retry never creates duplicate business side effect silently | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-033` | `BE-INF` | Authentication provider mechanism does not define Workspace authorization | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-034` | `BE-INF` | Object URL/key is not authorization | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-035` | `BE-INF` | Search index is rebuildable/derived unless explicitly classified otherwise | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-036` | `BE-INF` | Projection has explicit source facts and freshness | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-037` | `BE-INF` | Broker provider implementation does not own delivery semantics | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-038` | `BE-INF` | Delivery schema changes preserve Platform identity/invariant | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-039` | `BE-INF` | Persisted discriminator/event name is a compatibility contract | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-040` | `BE-INF` | JSON schema/version evolves deliberately | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-041` | `BE-INF` | Converter preserves Domain equality/meaning | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-042` | `BE-INF` | Persistence identity mapping is stable across provider/internal IDs | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-043` | `BE-INF` | Delete behavior is reviewed with semantic ownership | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-044` | `BE-INF` | Query filter is persistence convenience, not lifecycle rule | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-045` | `BE-INF` | Infrastructure audit mechanism consumes decided semantic facts | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-046` | `BE-INF` | SQL/provider logging is privacy and cardinality aware | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-047` | `BE-INF` | Connection state does not leak between requests | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-048` | `BE-INF` | Retry classification is mechanism-aware | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-049` | `BE-INF` | Backfill has stable traversal and checkpoint | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-050` | `BE-INF` | Restore does not skip reconciliation | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-051` | `BE-INF` | Secret value never becomes Application/Domain config data | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-052` | `BE-INF` | DI registration does not erase ownership | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-053` | `BE-INF` | Background Infrastructure does not use unrestricted DB scope by default | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-054` | `BE-INF` | Realtime adapter revalidates/uses authoritative scope contract | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-055` | `BE-INF` | Financial provider reconciliation preserves external reality | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-056` | `BE-INF` | Unverified webhook never mutates product state | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-057` | `BE-INF` | Durable external-provider migration has an explicit data/state plan | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-058` | `BE-INF` | Test fidelity matches the Infrastructure property | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-059` | `BE-INF` | Architecture guard cannot replace runtime RLS proof | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-060` | `BE-INF` | Infrastructure remains the persistence implementation owner while EF exception exists | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-061` | `BE-INF` | CLR rename does not automatically rename persisted identity | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-062` | `BE-INF` | Persistence uniqueness follows current product lifecycle | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-063` | `BE-INF` | Retention mechanism does not erase required history blindly | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-064` | `BE-INF` | Repair derives target value from authoritative semantics | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-065` | `BE-INF` | Measured performance problem does not transfer semantic ownership | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-066` | `BE-INF` | Provider client separates shared transport from tenant credential/state | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-067` | `BE-INF` | Secret is not persisted into ordinary business table/event accidentally | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-068` | `BE-INF` | Raw provider payload is not default durable storage | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-069` | `BE-INF` | Persistence-generated technical metadata does not overwrite business timestamp semantics | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-070` | `BE-INF` | Read port documents freshness/authority where material | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-071` | `BE-INF` | Scaling topology follows evidence | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-INF-072` | `BE-INF` | Extraction changes data topology, not semantic owner | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-OPS-CFG-001` | `BE-OPS-CFG` | Configuration is technical input, not product authority | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-002` | `BE-OPS-CFG` | Base defaults are safe, not production credentials | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-003` | `BE-OPS-CFG` | Environment difference changes mechanism, not product meaning | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-004` | `BE-OPS-CFG` | Current environment values are source facts, not duplicated policy | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-005` | `BE-OPS-CFG` | Example environment file contains placeholders only | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-006` | `BE-OPS-CFG` | Local env file is not architecture | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-007` | `BE-OPS-CFG` | Secret delivery and source deployment have separate lifecycles | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-008` | `BE-OPS-CFG` | Secret value never becomes diagnostic output | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-009` | `BE-OPS-CFG` | Runtime options are validated near composition | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-010` | `BE-OPS-CFG` | Critical option invalidity fails safe | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-011` | `BE-OPS-CFG` | Disabled provider is explicit state | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-012` | `BE-OPS-CFG` | Composition binds implementations, not product policy | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-013` | `BE-OPS-CFG` | Dependency outage behavior follows authority class | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-014` | `BE-OPS-CFG` | Database connection secret is runtime-only | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-015` | `BE-OPS-CFG` | MigrateOnStartup does not authorize unsafe production DDL | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-016` | `BE-OPS-CFG` | Administrative database command exits instead of serving traffic | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-017` | `BE-OPS-CFG` | Docs route to commands; Makefile defines exact implementation | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-018` | `BE-OPS-CFG` | Seed and reset are environment-safe | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-019` | `BE-OPS-CFG` | Seed privileged scope is bounded and always cleared | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-020` | `BE-OPS-CFG` | Reset command cannot ambiguously target production | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-021` | `BE-OPS-CFG` | Security config cannot silently disable required RLS | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-022` | `BE-OPS-CFG` | Policy-application privilege is separable from normal runtime | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-023` | `BE-OPS-CFG` | Transport selection cannot change message correctness | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-024` | `BE-OPS-CFG` | Lower-fidelity runtime is declared | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-025` | `BE-OPS-CFG` | Runtime tuning is environment/workload-driven | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-026` | `BE-OPS-CFG` | Redis outage cannot broaden security | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-027` | `BE-OPS-CFG` | Provider enablement does not move recipient semantics into config | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-028` | `BE-OPS-CFG` | OAuth redirect/provider config is environment-scoped | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-029` | `BE-OPS-CFG` | Redirect target is trusted configuration | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-030` | `BE-OPS-CFG` | JWT signing config fails safe | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-031` | `BE-OPS-CFG` | Data Protection key lifecycle matches deployment topology | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-032` | `BE-OPS-CFG` | Container filesystem is not durable business/key storage by default | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-033` | `BE-OPS-CFG` | Forwarded headers are trusted only from configured proxy boundary | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-034` | `BE-OPS-CFG` | Credentialed origin config has no permissive production fallback | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-035` | `BE-OPS-CFG` | TLS/forwarding behavior is resolved with deployment topology | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-036` | `BE-OPS-CFG` | Health threshold has an operational owner and meaning | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-037` | `BE-OPS-CFG` | Export/tooling mode cannot be reachable as normal production service behavior | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-038` | `BE-OPS-CFG` | Tooling exception does not become runtime precedent | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-039` | `BE-OPS-CFG` | Runtime middleware ordering follows prerequisite state | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-040` | `BE-OPS-CFG` | Optional dependency failure does not automatically fail whole process | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-041` | `BE-OPS-CFG` | Graceful shutdown preserves delivery correctness | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-042` | `BE-OPS-CFG` | Local runtime prioritizes protocol fidelity, not production topology symmetry | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-043` | `BE-OPS-CFG` | Backend runtime does not assume one deployment provider | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-044` | `BE-OPS-CFG` | Configuration key rename is compatibility work | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-045` | `BE-OPS-CFG` | Configuration has one logical owner | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-046` | `BE-OPS-CFG` | Secret is never compile-time baked into backend artifact by default | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-047` | `BE-OPS-CFG` | Promotion changes environment binding, not source identity | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-048` | `BE-OPS-CFG` | Effective configuration is inspectable safely | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-049` | `BE-OPS-CFG` | Diagnostic verbosity does not leak secrets/private data | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-CFG-050` | `BE-OPS-CFG` | Test config boundary at the point of risk | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-OPS-DATA-001` | `BE-OPS-DATA` | Meaning changes before representation | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-002` | `BE-OPS-DATA` | Migration source and model must agree | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-003` | `BE-OPS-DATA` | PendingModelChangesWarning is not silenced to make deploy green | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-004` | `BE-OPS-DATA` | Applied migration history is immutable evidence by default | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-005` | `BE-OPS-DATA` | Existing production state requires upgrade proof | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-006` | `BE-OPS-DATA` | Migration operation is explicit | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-007` | `BE-OPS-DATA` | Migration capability and startup invocation are separate | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-008` | `BE-OPS-DATA` | Every migration phase has one semantic authority | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-009` | `BE-OPS-DATA` | Expansion does not require new writer immediately | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-010` | `BE-OPS-DATA` | Dual-read precedence is deterministic | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-011` | `BE-OPS-DATA` | Dual write still has one authoritative result | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-012` | `BE-OPS-DATA` | Backfill is resumable | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-013` | `BE-OPS-DATA` | Traversal key is immutable enough for the backfill contract | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-014` | `BE-OPS-DATA` | Batch size is bounded and tunable | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-015` | `BE-OPS-DATA` | Backfill target mapping is deterministic | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-016` | `BE-OPS-DATA` | Unknown legacy state is not guessed away | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-017` | `BE-OPS-DATA` | Migration assumption has executable preflight where practical | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-018` | `BE-OPS-DATA` | Constraint rollout considers old data and mixed writers | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-019` | `BE-OPS-DATA` | Unique index is not product-rule discovery | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-020` | `BE-OPS-DATA` | Index change is tested against target query shape | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-021` | `BE-OPS-DATA` | Large-table DDL has lock/availability analysis | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-022` | `BE-OPS-DATA` | RLS change proves allowed and denied tenants | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-023` | `BE-OPS-DATA` | RLS policy and session-context rollout are compatible | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-024` | `BE-OPS-DATA` | RLS administration uses least privilege | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-025` | `BE-OPS-DATA` | Converter change verifies round-trip old and new data | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-026` | `BE-OPS-DATA` | Persisted identity changes intentionally | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-027` | `BE-OPS-DATA` | Persisted JSON version is explicit when incompatible evolution exists | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-028` | `BE-OPS-DATA` | Lazy migration does not create hidden unbounded write storm | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-029` | `BE-OPS-DATA` | Ownership migration declares owner per phase | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-030` | `BE-OPS-DATA` | Cross-context FK does not imply cross-context write authority | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-031` | `BE-OPS-DATA` | Destructive data change requires explicit semantic approval | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-032` | `BE-OPS-DATA` | Product deletion and physical purge are separate transitions | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-033` | `BE-OPS-DATA` | Retention job has an authoritative policy source | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-034` | `BE-OPS-DATA` | Repair is deterministic and scoped | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-035` | `BE-OPS-DATA` | Repair privilege expires with the repair workflow | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-036` | `BE-OPS-DATA` | Migration does not depend on development seed ordering | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-037` | `BE-OPS-DATA` | Persisted delivery state migration preserves Platform identities | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-038` | `BE-OPS-DATA` | Old queued payload is migration input | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-039` | `BE-OPS-DATA` | Derived-state migration never becomes source authority | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-040` | `BE-OPS-DATA` | DB migration and external object migration reconcile both sides | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-041` | `BE-OPS-DATA` | External provider reality is part of migration verification | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-042` | `BE-OPS-DATA` | Rollback is assessed per durable side effect | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-043` | `BE-OPS-DATA` | Recovery starts from observed reality | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-044` | `BE-OPS-DATA` | Completion is measurable | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-045` | `BE-OPS-DATA` | Execution evidence is operational artifact, not architecture authority | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-046` | `BE-OPS-DATA` | Migration drift is detected before release when practical | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-047` | `BE-OPS-DATA` | Persistence migration proof is provider-realistic | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-048` | `BE-OPS-DATA` | Fixture scope follows risk | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-049` | `BE-OPS-DATA` | Backfill verifies tenant invariants | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-050` | `BE-OPS-DATA` | Backfill/live-write race has an explicit resolution | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-051` | `BE-OPS-DATA` | Read cutover does not silently mix conflicting sources | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-052` | `BE-OPS-DATA` | Write cutover has one authoritative writer | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-053` | `BE-OPS-DATA` | Cleanup is part of migration completion | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-054` | `BE-OPS-DATA` | Destructive operation is never hidden inside innocuous migration name | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-055` | `BE-OPS-DATA` | Type conversion proves every supported old value | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-056` | `BE-OPS-DATA` | Missing value receives product-approved meaning or blocks transition | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-057` | `BE-OPS-DATA` | Rename is compatibility work, not cosmetic refactor | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-058` | `BE-OPS-DATA` | Table topology follows semantic owner after migration | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-059` | `BE-OPS-DATA` | Every deployed stage is compatible | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-060` | `BE-OPS-DATA` | Recovery procedure is phase-aware | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-061` | `BE-OPS-DATA` | Backup is evidence/mechanism, not automatic rollback | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-062` | `BE-OPS-DATA` | Production migration workload has capacity bounds | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-063` | `BE-OPS-DATA` | One tenant does not block all migration progress unnecessarily | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-064` | `BE-OPS-DATA` | Post-migration verification checks business meaning | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-065` | `BE-OPS-DATA` | DB-only verification is insufficient for behavior-changing migration | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-066` | `BE-OPS-DATA` | Completion proof precedes destructive cleanup | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-OPS-DATA-067` | `BE-OPS-DATA` | Required migration gate executes non-zero | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-PLT-001` | `BE-PLT` | Platform stays mechanism-oriented | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-002` | `BE-PLT` | Public message identity is logical, not CLR-location identity | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-003` | `BE-PLT` | Event descriptor describes compatibility-relevant facts | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-004` | `BE-PLT` | Envelope metadata and payload semantics remain separate | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-005` | `BE-PLT` | Retry preserves logical message identity | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-006` | `BE-PLT` | Runtime instance identity is not semantic producer identity | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-007` | `BE-PLT` | Consumer identity is stable across deploy/restart | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-008` | `BE-PLT` | Canonicalization is deterministic and compatibility-aware | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-009` | `BE-PLT` | Serialization change inventories persisted/backlogged messages | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-010` | `BE-PLT` | Topic change is compatibility work when old messages/consumers exist | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-011` | `BE-PLT` | Publish-before-commit is forbidden for committed business facts | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-012` | `BE-PLT` | Durability class is explicit | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-013` | `BE-PLT` | Claiming is lease/recovery aware | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-014` | `BE-PLT` | Duplicate delivery is expected behavior, not exceptional corruption | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-015` | `BE-PLT` | Idempotency is per consumer effect | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-016` | `BE-PLT` | Duplicate and identity conflict are distinct | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-017` | `BE-PLT` | Acknowledge/advance only after approved consumer success | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-018` | `BE-PLT` | Retry policy is failure-class aware | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-019` | `BE-PLT` | Retry cannot create an amplification storm | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-020` | `BE-PLT` | Open circuit is a degraded failure mode, not fake success | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-021` | `BE-PLT` | Poison identity is scoped to message + consumer | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-022` | `BE-PLT` | Dead-letter is not deletion | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-023` | `BE-PLT` | Ordering scope is the smallest scope that preserves the invariant | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-024` | `BE-PLT` | Sequence advancement occurs after handler success | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-025` | `BE-PLT` | Ordering gap is observable | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-026` | `BE-PLT` | Consumer host is not a business service layer | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-027` | `BE-PLT` | Background work is explicitly tenant-scoped | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-028` | `BE-PLT` | External inbound messages cross a trust boundary | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-029` | `BE-PLT` | Correlation, causation, message ID, idempotency ID are distinct | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-030` | `BE-PLT` | Compatibility is evaluated against supported consumers/backlog, not only newest producer | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-031` | `BE-PLT` | Event version increases when incompatible semantic contract changes require it | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-032` | `BE-PLT` | Replay is not “republish everything” | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-033` | `BE-PLT` | Replay checkpoint advances after replayed unit success | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-034` | `BE-PLT` | Forced replay is a governed operation | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-035` | `BE-PLT` | Recovery traffic preserves tenant and live-work fairness | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-036` | `BE-PLT` | Transport adapter is replaceable behind Platform contract | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-037` | `BE-PLT` | InMemory success is not broker reliability proof | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-038` | `BE-PLT` | Required delivery fails configuration/readiness if transport is unavailable by policy | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-039` | `BE-PLT` | Provider broker retry cannot violate Platform retry/idempotency semantics | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-040` | `BE-PLT` | Schedule occurrence identity survives scale-out | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-041` | `BE-PLT` | Durable delay uses durable state | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-042` | `BE-PLT` | Observability follows semantic identities | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-043` | `BE-PLT` | Oldest backlog age is first-class delivery signal | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-044` | `BE-PLT` | Platform diagnostic tooling cannot mutate product state as a shortcut | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-045` | `BE-PLT` | Delivery state schema follows logical identity | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-046` | `BE-PLT` | Retention cannot delete dedup/replay evidence before supported horizon | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-047` | `BE-PLT` | Delivery diagnostics use metadata before payload | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-048` | `BE-PLT` | Delivery retry never converts revoked authority into permission | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-049` | `BE-PLT` | Platform does not hide eventual consistency | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-050` | `BE-PLT` | Old queued messages are deployed consumers | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-051` | `BE-PLT` | Consumer success-condition change reviews replay/idempotency state | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-052` | `BE-PLT` | Delivery dedup does not erase legitimate repeated business occurrences | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-053` | `BE-PLT` | Poison recovery does not create a new fake event to bypass poison state | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-054` | `BE-PLT` | Platform cannot authorize semantic skip on its own | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-055` | `BE-PLT` | Broker outage does not erase committed outbox intent | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-056` | `BE-PLT` | Reliability state failure fails safely | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-057` | `BE-PLT` | Consumer retry and provider retry are one end-to-end attempt model | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-058` | `BE-PLT` | More workers does not automatically increase safe throughput | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-059` | `BE-PLT` | Realtime delivery failure does not change source truth | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-060` | `BE-PLT` | Automation event consumption preserves source event identity and target authorization semantics | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-061` | `BE-PLT` | Financial effect is deduplicated by business operation, not transport attempt | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-062` | `BE-PLT` | Platform change requires production-graph proof when source transaction matters | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-PLT-063` | `BE-PLT` | Test a deliberate failure path, not only happy delivery | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-SEC-001` | `BE-SEC` | Authentication and authorization are distinct | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-002` | `BE-SEC` | Raw credential mechanics remain at the outer boundary | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-003` | `BE-SEC` | Principal type is explicit | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-004` | `BE-SEC` | User existence does not authorize tenant access | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-005` | `BE-SEC` | Security scope is explicit at every protected boundary | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-006` | `BE-SEC` | Authorization is resource/action oriented | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-007` | `BE-SEC` | Product role names do not become hard-coded authorization protocol by accident | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-008` | `BE-SEC` | Authorization failure and authorization-system failure are distinct | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-009` | `BE-SEC` | Authorization does not fail open by convenience | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-010` | `BE-SEC` | Resource identity is not authority | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-011` | `BE-SEC` | Resource scope is resolved from authoritative server data | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-012` | `BE-SEC` | Scope mismatch never broadens access | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-013` | `BE-SEC` | Protected Application use case has one authoritative authorization path | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-014` | `BE-SEC` | Late denial cannot be the normal security model | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-015` | `BE-SEC` | Domain actor check is not general authorization | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-016` | `BE-SEC` | RLS complements, never replaces, Application authorization | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-017` | `BE-SEC` | RLS context cannot leak across pooled connections | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-018` | `BE-SEC` | Bootstrap privilege is minimal | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-019` | `BE-SEC` | Query-filter bypass does not imply tenant bypass | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-020` | `BE-SEC` | System context is not ordinary request fallback | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-021` | `BE-SEC` | Background execution is never tenantless when touching tenant data | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-022` | `BE-SEC` | Delayed execution states its authority time semantics | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-023` | `BE-SEC` | Permission-sensitive cache has tenant/resource/principal or permission-version separation | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-024` | `BE-SEC` | Revocation invalidates stale authorization decisions | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-025` | `BE-SEC` | Security cache is optimization, never sole source | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-026` | `BE-SEC` | Realtime connection authorization does not authorize every resource forever | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-027` | `BE-SEC` | Server never sends foreign tenant data expecting client-side filtering | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-028` | `BE-SEC` | Search does not become a cross-tenant discovery oracle | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-029` | `BE-SEC` | Provider credential possession is not provider-connection authorization | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-030` | `BE-SEC` | Unverified webhook cannot mutate tenant state | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-031` | `BE-SEC` | API key permission is least-scoped | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-032` | `BE-SEC` | Share capability is non-transitive | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-033` | `BE-SEC` | Capability revocation has an invalidation path | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-034` | `BE-SEC` | One-time token is purpose-bound | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-035` | `BE-SEC` | Sensitive token comparison/storage is designed for disclosure resistance | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-036` | `BE-SEC` | Encryption mechanism stays outer; secret lifecycle owner stays semantic | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-037` | `BE-SEC` | Security observability uses safe identifiers, not credential material | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-038` | `BE-SEC` | CSRF bypass is credential-mode specific | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-039` | `BE-SEC` | CORS is not a server authorization boundary | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-040` | `BE-SEC` | Rate limit does not grant or deny resource permission | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-041` | `BE-SEC` | Abuse-sensitive path has a stable partition without leaking account existence | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-042` | `BE-SEC` | Security audit is not replaced by user-visible Activity | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-043` | `BE-SEC` | Privileged bypass is explicit, scoped, and auditable | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-044` | `BE-SEC` | Service/system authority is least privilege | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-045` | `BE-SEC` | Permission semantic owner is singular | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-046` | `BE-SEC` | Entitlement never grants resource permission by itself | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-047` | `BE-SEC` | Audit creator field is not permission ownership by default | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-048` | `BE-SEC` | Scope-changing mutation revalidates destination authority | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-049` | `BE-SEC` | Tenant identity is not mutable by generic update | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-050` | `BE-SEC` | Lifecycle access invalidation propagates to derived security surfaces | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-051` | `BE-SEC` | Existence disclosure policy is consistent for a resource class | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-052` | `BE-SEC` | Security log cannot become authorization source | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-053` | `BE-SEC` | Production security config has no silent permissive fallback | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-054` | `BE-SEC` | Network-derived identity is trusted only through configured proxy boundary | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-055` | `BE-SEC` | Storage key/path is not tenant authorization | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-056` | `BE-SEC` | Authorization authority remains server-source state | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-057` | `BE-SEC` | Security proof includes a negative path | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-058` | `BE-SEC` | Structural security invariant becomes executable when practical | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-SEC-059` | `BE-SEC` | Security critical gate must do non-zero work | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-TST-001` | `BE-TST` | Test the property, not the file | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-002` | `BE-TST` | Cheapest reliable seam first | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-003` | `BE-TST` | Domain invariant proof remains infrastructure-free | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-004` | `BE-TST` | Negative outcome proves non-mutation where relevant | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-005` | `BE-TST` | Application test does not mock away the behavior under test | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-006` | `BE-TST` | Security test has at least one negative path | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-007` | `BE-TST` | SQLite is not backend persistence substitute | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-008` | `BE-TST` | InMemory evidence states its fidelity boundary | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-009` | `BE-TST` | RLS denial is first-class test evidence | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-010` | `BE-TST` | Existing-data change requires upgrade proof | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-011` | `BE-TST` | Reliability property includes failure path | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-012` | `BE-TST` | Cursor/ack ordering is explicitly tested | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-013` | `BE-TST` | Duplicate and conflict are separate test cases | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-014` | `BE-TST` | Poison test verifies scope | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-015` | `BE-TST` | Replay test proves bounded recovery, not mass republish | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-016` | `BE-TST` | API test proves transport/public contract | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-017` | `BE-TST` | OpenAPI gate has two layers | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-018` | `BE-TST` | Critical-test discoverability is part of CI contract | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-019` | `BE-TST` | Integration test adds a distinct cross-boundary property | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-020` | `BE-TST` | “Production graph” claim names what is real versus substituted | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-021` | `BE-TST` | Architecture gate protects canonical rule | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-022` | `BE-TST` | Architecture gate failure is diagnosed against canonical authority first | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-023` | `BE-TST` | Critical gate should be falsifiable | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-024` | `BE-TST` | Test support preserves visible semantic preconditions | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-025` | `BE-TST` | Fixture convenience cannot create false-green privilege | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-026` | `BE-TST` | One-tenant tests cannot certify cross-tenant isolation | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-027` | `BE-TST` | Mock cannot prove the behavior it replaces | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-028` | `BE-TST` | Async test synchronizes on semantic condition | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-029` | `BE-TST` | Quarantined critical test is a governed exception | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-030` | `BE-TST` | Snapshot update is not mechanical acceptance | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-031` | `BE-TST` | Critical test rename is a gate change | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-032` | `BE-TST` | Empty success is failure for required evidence | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-033` | `BE-TST` | Focused pass is reported as focused pass | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-034` | `BE-TST` | Certification follows protected property, not fixed “run everything” ritual alone | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-035` | `BE-TST` | CI topology may evolve; protected properties do not silently disappear | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-036` | `BE-TST` | Build/format/vulnerability success is not semantic correctness | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-037` | `BE-TST` | Required architecture gate is merge-blocking evidence | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-038` | `BE-TST` | Integration critical list is curated evidence, not a complete test inventory | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-039` | `BE-TST` | Docker build is packaging proof only | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-040` | `BE-TST` | Final CI gate is an aggregator, not a substitute | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-041` | `BE-TST` | Relevant change detection cannot hide backend-affecting input | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-042` | `BE-TST` | Certification is SHA-specific | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-043` | `BE-TST` | Evidence can have secondary guard | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-044` | `BE-TST` | Coverage percentage is diagnostic, not acceptance by itself | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-045` | `BE-TST` | Advanced technique is justified by risk, not fashion | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-046` | `BE-TST` | Provider evidence names fidelity | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-047` | `BE-TST` | In-memory cache does not certify Redis-specific behavior | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-048` | `BE-TST` | Transport test fidelity follows transport claim | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-049` | `BE-TST` | Mixed-version compatibility is independent test dimension | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-050` | `BE-TST` | Migration fixture identifies its source version/state | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-051` | `BE-TST` | Test secret is intentionally fake | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-052` | `BE-TST` | Performance proof states workload/cardinality | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-053` | `BE-TST` | Race test controls the race | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-054` | `BE-TST` | Failure injection verifies resulting durable state | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-055` | `BE-TST` | Parallel safety is explicit | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-056` | `BE-TST` | Eventual assertion has a bounded deadline and meaningful condition | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-057` | `BE-TST` | Gate failure is actionable | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-058` | `BE-TST` | Failing valid test is evidence, not obstacle | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-059` | `BE-TST` | Test refactor preserves protected property | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-060` | `BE-TST` | Exception narrows gate, never deletes architecture protection globally | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-061` | `BE-TST` | Hang is a diagnosable failure class | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-062` | `BE-TST` | Generated artifact check is producer-oriented | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-063` | `BE-TST` | Test does not become canonical product spec by accident | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-064` | `BE-TST` | Green test with stale canonical rule is not enough | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-065` | `BE-TST` | Downstream packaging cannot bypass failed semantic proof | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-066` | `BE-TST` | Evidence claim never exceeds execution | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BE-TST-067` | `BE-TST` | Higher-risk change accumulates proof | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `BIL-001` | `BIL` | Plan is a global catalog | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-002` | `BIL` | Plan name is not an entitlement API | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-003` | `BIL` | Plan lifecycle and Subscription lifecycle are separate | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-004` | `BIL` | Historical price meaning is preserved | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-005` | `BIL` | FeatureCode is stable commercial vocabulary | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-006` | `BIL` | Limit has defined unit and scope | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-007` | `BIL` | Billing Customer does not replace Account identity | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-008` | `BIL` | Subscription lifecycle is explicit | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-009` | `BIL` | Provider webhook does not directly set arbitrary Subscription state | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-010` | `BIL` | Cancellation request and effective cancellation are distinct facts | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-011` | `BIL` | Provider/payment failure does not corrupt existing product state | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-012` | `BIL` | Downgrade is non-destructive by default | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-013` | `BIL` | Entitlement is a business contract, not scattered feature flags | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-014` | `BIL` | Entitlement scope is not authorization scope | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-015` | `BIL` | Entitlement resolution is deterministic and versionable | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-016` | `BIL` | Entitlement cache cannot outlive authority changes silently | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-017` | `BIL` | Billing usage metric has commercial semantics | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-018` | `BIL` | Usage ingestion is idempotent | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-019` | `BIL` | Derived usage total is not the only truth | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-020` | `BIL` | Usage reset is period transition, not history deletion | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-021` | `BIL` | Billing Usage and Analytics Metric are distinct authorities | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-022` | `BIL` | Invoice is append/evidence-oriented commercial state | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-023` | `BIL` | Billing Domain does not store raw card secrets | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-024` | `BIL` | Provider identifiers are references, not Domain ownership | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-025` | `BIL` | Commercial callbacks and commands are idempotent | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-026` | `BIL` | Billing uncertainty is represented explicitly | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-027` | `BIL` | Account closure coordinates Billing; Billing does not own Account deletion | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-028` | `BIL` | Billing administration is separately authorized | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-029` | `BIL` | Billing public events are canonical commercial facts | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-030` | `BIL` | Security-sensitive entitlement failure never grants capability accidentally | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-031` | `BIL` | Usage is recorded from successful source facts | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-032` | `BIL` | Hard quota concurrency is designed explicitly | [`docs/product/billing.md`](../product/billing.md) |
| `BIL-033` | `BIL` | Commercial retention is independent of ordinary product deletion | [`docs/product/billing.md`](../product/billing.md) |
| `COL-001` | `COL` | Collaboration target is explicit and scoped | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-002` | `COL` | Collaboration never mutates target aggregate implicitly | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-003` | `COL` | Comment target existence/access is validated outside pure Domain | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-004` | `COL` | Reply remains on the same target/thread | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-005` | `COL` | Anchor is locator, not target-content ownership | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-006` | `COL` | Comment deletion policy is explicit | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-007` | `COL` | Mention resolves to stable identity | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-008` | `COL` | Mention delivery is idempotent | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-009` | `COL` | Reaction uniqueness is deterministic | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-010` | `COL` | Attachment stores metadata/object identity, not arbitrary binary Domain data | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-011` | `COL` | Attachment download capability is scoped and short-lived where appropriate | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-012` | `COL` | Presence is ephemeral collaboration state | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-013` | `COL` | Presence does not authorize access | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-014` | `COL` | Read state belongs to user + resource scope | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-015` | `COL` | Unread count derives from stable read boundary | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-016` | `COL` | Watch state is explicit user-resource preference | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-017` | `COL` | Notification has explicit recipient | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-018` | `COL` | Provider delivery result is not Notification truth | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-019` | `COL` | Activity and Governance Audit are different products | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-020` | `COL` | Activity maps product facts, not transport attempts | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-021` | `COL` | New target type is cross-context contract change | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-022` | `COL` | Collaboration scope follows the target | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-023` | `COL` | Historical Notification does not freeze resource permission | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-024` | `COL` | Collaboration realtime is replay-safe | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-025` | `COL` | Optimistic Collaboration state reconciles to authoritative identity | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-026` | `COL` | Target deletion does not automatically cascade Collaboration history | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-027` | `COL` | Historical Collaboration can outlive active Identity | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-028` | `COL` | Collaboration histories are purpose-specific | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `COL-029` | `COL` | Collaboration content is safe to render by contract | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `DCT-001` | `DCT` | Documents is Page + typed Block content, not opaque HTML | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-002` | `DCT` | A Page belongs to one Workspace | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-003` | `DCT` | Archive and delete are different lifecycle operations | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-004` | `DCT` | Page visibility is not the whole permission model | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-005` | `DCT` | Page hierarchy is tenant-safe and acyclic | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-006` | `DCT` | A Block belongs to exactly one Page | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-007` | `DCT` | Block content validates against Block Type | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-008` | `DCT` | Arbitrary unvalidated Block JSON is forbidden | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-009` | `DCT` | Block-tree validation uses supplied ancestry facts | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-010` | `DCT` | Block ordering is server-authoritative and deterministic | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-011` | `DCT` | Document editing is version/concurrency aware | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-012` | `DCT` | Realtime transport is not collaborative-editing semantics | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-013` | `DCT` | Comment anchors never become content ownership | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-014` | `DCT` | Resource Link preserves target ownership | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-015` | `DCT` | Document sharing is non-transitive across resource links | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-016` | `DCT` | History/version is not aggregate concurrency version | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-017` | `DCT` | Snapshot is not a second live document | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-018` | `DCT` | Page Template is creation input, not hidden live authority | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-019` | `DCT` | Documents does not authorize by visibility alone | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-020` | `DCT` | Page/Block content does not become Work Management storage | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-021` | `DCT` | Search/index is not document truth | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-022` | `DCT` | Binary payload is not Domain/event content | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-023` | `DCT` | Public document events expose facts, not full trees | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-024` | `DCT` | Missed realtime cannot permanently corrupt document state | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-025` | `DCT` | No-op edit does not create false content history | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-026` | `DCT` | Import/export formats are boundary representations | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-027` | `DCT` | Cross-Workspace document movement is a migration, not a tree move | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-028` | `DCT` | Page deletion is lifecycle workflow, not ORM cascade | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-029` | `DCT` | Block deletion defines child behavior | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-030` | `DCT` | Restore validates current constraints | [`docs/product/documents.md`](../product/documents.md) |
| `DCT-031` | `DCT` | Document history is not Governance Audit | [`docs/product/documents.md`](../product/documents.md) |
| `DEC-001` | `DEC` | Current architecture and decision history are separate | [`docs/decisions/README.md`](../decisions/README.md) |
| `DEC-002` | `DEC` | ADR ID is immutable | [`docs/decisions/README.md`](../decisions/README.md) |
| `DEC-003` | `DEC` | Do not recycle ADR IDs | [`docs/decisions/README.md`](../decisions/README.md) |
| `DEC-004` | `DEC` | Proposed ADR does not silently redefine architecture | [`docs/decisions/README.md`](../decisions/README.md) |
| `DEC-005` | `DEC` | Accepted ADR requires current-architecture alignment | [`docs/decisions/README.md`](../decisions/README.md) |
| `DEC-006` | `DEC` | Superseded ADR is preserved | [`docs/decisions/README.md`](../decisions/README.md) |
| `DEC-007` | `DEC` | Rejected ADR is not current architecture | [`docs/decisions/README.md`](../decisions/README.md) |
| `DEC-008` | `DEC` | ADR contains enough context to understand the choice independently | [`docs/decisions/README.md`](../decisions/README.md) |
| `DEC-009` | `DEC` | Decision is specific enough to constrain implementation | [`docs/decisions/README.md`](../decisions/README.md) |
| `DEC-010` | `DEC` | Rejected alternatives are represented fairly | [`docs/decisions/README.md`](../decisions/README.md) |
| `DEC-011` | `DEC` | ADR records trade-offs, not marketing copy | [`docs/decisions/README.md`](../decisions/README.md) |
| `DEC-012` | `DEC` | ADR does not become a migration tracker | [`docs/decisions/README.md`](../decisions/README.md) |
| `DEC-013` | `DEC` | ADR rationale and executable evidence remain distinct | [`docs/decisions/README.md`](../decisions/README.md) |
| `DEC-014` | `DEC` | ADR owner is logical responsibility | [`docs/decisions/README.md`](../decisions/README.md) |
| `DEC-015` | `DEC` | Routine feature implementation does not require an ADR | [`docs/decisions/README.md`](../decisions/README.md) |
| `DEC-016` | `DEC` | System ADR is not a backend ADR with a broader title | [`docs/decisions/README.md`](../decisions/README.md) |
| `DEC-017` | `DEC` | Existing Backend ADR history is preserved | [`docs/decisions/README.md`](../decisions/README.md) |
| `DEC-018` | `DEC` | Existing Frontend ADR history is preserved | [`docs/decisions/README.md`](../decisions/README.md) |
| `DEC-019` | `DEC` | Each ADR is registered exactly once in its scope registry | [`docs/decisions/README.md`](../decisions/README.md) |
| `DEC-020` | `DEC` | No placeholder ADR | [`docs/decisions/README.md`](../decisions/README.md) |
| `DEC-021` | `DEC` | Accepted ADR is not silently rewritten to match the new choice | [`docs/decisions/README.md`](../decisions/README.md) |
| `DEC-022` | `DEC` | Meaning-changing edit requires new decision history | [`docs/decisions/README.md`](../decisions/README.md) |
| `DEC-023` | `DEC` | Exception is not an ADR | [`docs/decisions/README.md`](../decisions/README.md) |
| `DEC-024` | `DEC` | Feature size does not determine ADR need | [`docs/decisions/README.md`](../decisions/README.md) |
| `DEC-025` | `DEC` | Durable incident learning is rehomed | [`docs/decisions/README.md`](../decisions/README.md) |
| `DEC-026` | `DEC` | Decision acceptance and implementation evidence are independent facts | [`docs/decisions/README.md`](../decisions/README.md) |
| `DEL-CHG-001` | `DEL-CHG` | Analyze consumers before changing a contract | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-002` | `DEL-CHG` | Delivery obligations are cumulative | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-003` | `DEL-CHG` | Local implementation preserves all external and persisted semantics | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-004` | `DEL-CHG` | Persisted or public identity rename is not refactor-only | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-005` | `DEL-CHG` | Additive means existing consumers remain valid | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-006` | `DEL-CHG` | Shape compatibility does not prove semantic compatibility | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-007` | `DEL-CHG` | Breaking change has explicit compatibility strategy | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-008` | `DEL-CHG` | Independent consumers define the mixed-version window | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-009` | `DEL-CHG` | Schema change includes existing production data | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-010` | `DEL-CHG` | Destructive contraction waits for completion proof | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-011` | `DEL-CHG` | Backfill is production code | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-012` | `DEL-CHG` | Dual write never means dual authority | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-013` | `DEL-CHG` | Architecture change requires decision impact review | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-014` | `DEL-CHG` | Folder movement and architecture movement are distinguished | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-015` | `DEL-CHG` | Security change requires negative-path evidence | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-016` | `DEL-CHG` | Configuration is a deployed contract | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-017` | `DEL-CHG` | Irreversible change requires forward-recovery reasoning | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-018` | `DEL-CHG` | Mobile lag requires backward-compatible server window | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-019` | `DEL-CHG` | Backlog consumers must understand old in-flight contract | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-020` | `DEL-CHG` | External provider is an independently changing/deployed participant | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-021` | `DEL-CHG` | Generated artifact changes through producer | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-022` | `DEL-CHG` | Feature flag has removal contract | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-023` | `DEL-CHG` | Rollback capability is stated, not assumed | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-024` | `DEL-CHG` | Search beats architectural guessing | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-025` | `DEL-CHG` | Existing coupling is not automatic precedent | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-026` | `DEL-CHG` | Contract producer and consumer are not assumed atomic | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-027` | `DEL-CHG` | Failure contract is part of compatibility | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-028` | `DEL-CHG` | Consistency promise is classified | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-029` | `DEL-CHG` | Authorization migration includes stored policy and active state | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-030` | `DEL-CHG` | Logical identity outranks implementation name | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-031` | `DEL-CHG` | Removal requires proof of non-use or completed migration | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-032` | `DEL-CHG` | Dependency version number does not determine semantic class alone | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-033` | `DEL-CHG` | Removing/weakening a gate is a semantic delivery change | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-034` | `DEL-CHG` | Canonical documentation change can require implementation follow-up | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-035` | `DEL-CHG` | ADR records the decision, not the implementation task list | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-036` | `DEL-CHG` | Every deployed stage is valid on its own | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-037` | `DEL-CHG` | Cleanup is a separate proven phase | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-038` | `DEL-CHG` | “All tests passed” is not sufficient evidence report | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-039` | `DEL-CHG` | Process depth follows change impact | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CHG-040` | `DEL-CHG` | Agent does not invent missing migration semantics | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CON-001` | `DEL-CON` | Product semantics precede transport shape | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-002` | `DEL-CON` | Internal implementation is not a contract | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-003` | `DEL-CON` | Contract identity is logical, not accidental source identity | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-004` | `DEL-CON` | Producer and consumer inventory is explicit | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-005` | `DEL-CON` | Consumers are classified by deployment independence | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-006` | `DEL-CON` | Mixed-version behavior is designed explicitly | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-007` | `DEL-CON` | Additive means consumer-compatible, not merely schema-additive | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-008` | `DEL-CON` | Defaults are semantic contract | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-009` | `DEL-CON` | Failure semantics are versioned with success semantics | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-010` | `DEL-CON` | Completion semantics are contractual | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-011` | `DEL-CON` | Contract does not smuggle authorization through transport fields | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-012` | `DEL-CON` | REST contract follows resource semantics, not database CRUD shape | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-013` | `DEL-CON` | OpenAPI drift is intentional or failing | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-014` | `DEL-CON` | Generated contracts change through producer source | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-015` | `DEL-CON` | Domain event is not automatically public event | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-016` | `DEL-CON` | Public event change considers stored backlog and replay | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-017` | `DEL-CON` | Realtime payload can differ from integration event | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-018` | `DEL-CON` | Consumer imports public exports, not internal paths | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-019` | `DEL-CON` | Provider schema is translated, not propagated into product domains | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-020` | `DEL-CON` | Verified provider transport still passes product validation | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-021` | `DEL-CON` | Persisted discriminator/key change is classified as contract + data change | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-022` | `DEL-CON` | Rollout order follows compatibility, not team ownership | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-023` | `DEL-CON` | Compatibility path is temporary and observable | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-024` | `DEL-CON` | Backend remains compatible with supported mobile floor | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-025` | `DEL-CON` | Worker compatibility includes in-flight messages | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-026` | `DEL-CON` | Contract test asserts public meaning, not private implementation | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-027` | `DEL-CON` | Critical consumer semantics are tested explicitly | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-028` | `DEL-CON` | Consumer convenience does not move source ownership | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-029` | `DEL-CON` | Contract includes only data needed for its purpose | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-030` | `DEL-CON` | List/detail shapes may differ intentionally | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-031` | `DEL-CON` | Concurrency failure is a first-class contract outcome | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-032` | `DEL-CON` | Idempotency semantics are shared producer-consumer contract | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-033` | `DEL-CON` | Time meaning is semantic, not formatting | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-034` | `DEL-CON` | Large payload transfer uses purpose-built boundary | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-035` | `DEL-CON` | Cross-context contract preserves target-context invariants | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-036` | `DEL-CON` | Async is not used to hide unclear ownership | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-037` | `DEL-CON` | Generated facts are generated; authored docs explain semantics | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-038` | `DEL-CON` | PR boundaries follow safe review/deployment units | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-039` | `DEL-CON` | Drift is not normalized by handwritten adapter hacks | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-CON-040` | `DEL-CON` | Old contract removal is a separate classified change | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-DEV-001` | `DEL-DEV` | Setup is repository-discoverable | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-002` | `DEL-DEV` | Tool versions come from manifests | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-003` | `DEL-DEV` | One package manager owns the frontend workspace | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-004` | `DEL-DEV` | Container path does not hide required architecture | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-005` | `DEL-DEV` | Local secret files are untracked | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-006` | `DEL-DEV` | Example configuration never contains reusable production-like secrets | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-007` | `DEL-DEV` | Onboarding drift is fixed at the source, not duplicated | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-008` | `DEL-DEV` | This handbook does not duplicate the entire environment schema | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-009` | `DEL-DEV` | Start command is idempotent enough for daily use | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-010` | `DEL-DEV` | Destructive local commands are unmistakably local/destructive | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-011` | `DEL-DEV` | Reset recreates architecture-relevant local state | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-012` | `DEL-DEV` | Optional service is enabled only when the tested path needs it | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-013` | `DEL-DEV` | Config inspection must not reveal real secrets unnecessarily | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-014` | `DEL-DEV` | Runtime address comes from resolved local configuration | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-015` | `DEL-DEV` | Backend solution inventory is backend.slnx | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-016` | `DEL-DEV` | Focused test is not reported as full validation | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-017` | `DEL-DEV` | Local schema changes use migrations | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-018` | `DEL-DEV` | Local workaround cannot normalize migration debt | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-019` | `DEL-DEV` | Tenant-sensitive local seed contains multiple scopes | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-020` | `DEL-DEV` | Local convenience does not erase permission states | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-021` | `DEL-DEV` | Frozen lockfile failure is fixed at manifests/lockfile | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-022` | `DEL-DEV` | Run only required host during focused work when possible | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-023` | `DEL-DEV` | Fast validation is fast feedback, not universal completion | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-024` | `DEL-DEV` | Generated contract is regenerated, not hand-edited | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-025` | `DEL-DEV` | Local deep import is not an acceptable shortcut | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-026` | `DEL-DEV` | New tests use the correct runtime category | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-027` | `DEL-DEV` | Local docs check follows current documentation authority | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-028` | `DEL-DEV` | First-run workflow has an actionable failure path | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-029` | `DEL-DEV` | Manual path must preserve the same external contracts | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-030` | `DEL-DEV` | Fake provider has declared fidelity boundary | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-031` | `DEL-DEV` | Onboarding prerequisites follow current executable need | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-032` | `DEL-DEV` | Provider/tool-specific assistant folders are not project architecture | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-033` | `DEL-DEV` | Supported local platform claims are evidence-based | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-034` | `DEL-DEV` | Personal machine override stays local | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-035` | `DEL-DEV` | Development mode is not permission to log secrets | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-036` | `DEL-DEV` | Reset is not the default fix for reproducible defects | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-037` | `DEL-DEV` | Dependency drift is repaired, not bypassed | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-038` | `DEL-DEV` | Local sample data is synthetic/minimized | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-039` | `DEL-DEV` | Onboarding changes are atomic with toolchain changes | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-040` | `DEL-DEV` | Local command and CI prove the same contract where practical | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DEV-041` | `DEL-DEV` | Current-state discrepancy is recorded, not silently copied | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-DONE-001` | `DEL-DONE` | Definition of Done follows classified impact | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-002` | `DEL-DONE` | All applicable surfaces complete together | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-003` | `DEL-DONE` | Source and canonical semantics agree | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-004` | `DEL-DONE` | Correct result in wrong owner is not Done | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-005` | `DEL-DONE` | Failure state is implemented, not only happy path | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-006` | `DEL-DONE` | Rejected input leaves state/effects consistent | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-007` | `DEL-DONE` | “Migration runs on empty DB” is not sufficient | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-008` | `DEL-DONE` | Change is not globally Done while mandatory migration phase remains incomplete | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-009` | `DEL-DONE` | Tenant isolation has negative evidence | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-010` | `DEL-DONE` | Authorization TODO means not Done | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-011` | `DEL-DONE` | Security exception is explicit, bounded, and visible | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-012` | `DEL-DONE` | Generated contract drift means not Done | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-013` | `DEL-DONE` | Old contract removal and new contract introduction are independently proven | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-014` | `DEL-DONE` | Latest mobile source is not proof for installed old clients | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-015` | `DEL-DONE` | Optimistic UI reconciles after rejection/conflict | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-016` | `DEL-DONE` | Realtime feature is recoverable without perfect delivery | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-017` | `DEL-DONE` | Retry without idempotency/reconciliation is incomplete | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-018` | `DEL-DONE` | Provider happy path alone is not Done | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-019` | `DEL-DONE` | Focused tests are reported as focused tests | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-020` | `DEL-DONE` | Green empty suite is not Done | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-021` | `DEL-DONE` | Architecture gate is not disabled to complete delivery | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-022` | `DEL-DONE` | Critical inaccessible workflow is not Done | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-023` | `DEL-DONE` | Performance claim states workload assumptions | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-024` | `DEL-DONE` | Durable async/external failure must be diagnosable | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-025` | `DEL-DONE` | Manual undocumented environment configuration means not Done | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-026` | `DEL-DONE` | Feature flag is not Done until lifecycle is defined | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-027` | `DEL-DONE` | Production stage validity is part of Done | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-028` | `DEL-DONE` | “Can rollback” requires evidence | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-029` | `DEL-DONE` | Documentation is not post-merge cleanup for semantic change | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-030` | `DEL-DONE` | Missing required decision record means not Done | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-031` | `DEL-DONE` | Exception is not invisible debt | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-032` | `DEL-DONE` | Transitional path has explicit end state | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-033` | `DEL-DONE` | Git is history; dead commented code is not | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-034` | `DEL-DONE` | Exact SHA is completion evidence | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-035` | `DEL-DONE` | Local environment cannot certify properties it does not reproduce | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-036` | `DEL-DONE` | Evidence report distinguishes verified from assumed | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-037` | `DEL-DONE` | Not-applicable requires a reason for material checklists | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-038` | `DEL-DONE` | Stage Done and Feature Done are distinct | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-039` | `DEL-DONE` | Prototype success does not waive production obligations | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-040` | `DEL-DONE` | Production state change is reproducible | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-041` | `DEL-DONE` | One-time repair is governed production code | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-042` | `DEL-DONE` | Fix root protective property, not only observed symptom | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-043` | `DEL-DONE` | Production completion can require evidence after deployment | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-DONE-044` | `DEL-DONE` | Cleanup debt is explicit and bounded | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-MIG-001` | `DEL-MIG` | Migration starts from real old state | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-002` | `DEL-MIG` | Migration has one declared target authority | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-003` | `DEL-MIG` | Expand precedes destructive contraction when mixed versions exist | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-004` | `DEL-MIG` | Expansion does not change authority accidentally | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-005` | `DEL-MIG` | Compatibility code has bounded lifetime | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-006` | `DEL-MIG` | Backfill is idempotent or safely resumable | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-007` | `DEL-MIG` | Backfill is bounded | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-008` | `DEL-MIG` | Migration is tenant-safe production code | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-009` | `DEL-MIG` | Invalid legacy data is not silently invented | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-010` | `DEL-MIG` | Migration default and product default are distinguished | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-011` | `DEL-MIG` | Constraint enforcement follows data proof | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-012` | `DEL-MIG` | Persisted rename is copy/compatibility migration when old readers exist | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-013` | `DEL-MIG` | Status migration preserves lifecycle meaning | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-014` | `DEL-MIG` | Flexible persisted JSON is versioned when compatibility requires it | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-015` | `DEL-MIG` | RLS migration is tested with real tenant data | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-016` | `DEL-MIG` | Security policy and tenant table become valid in the same safe stage | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-017` | `DEL-MIG` | Large index change has an operational plan | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-018` | `DEL-MIG` | Constraint migration anticipates concurrent writes | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-019` | `DEL-MIG` | Dual read has deterministic precedence | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-020` | `DEL-MIG` | Dual write is not dual authority | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-021` | `DEL-MIG` | Partial dual-write failure is modeled explicitly | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-022` | `DEL-MIG` | Shadow comparison is side-effect free | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-023` | `DEL-MIG` | Cutover condition is objective | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-024` | `DEL-MIG` | Cutover is observable | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-025` | `DEL-MIG` | Old path removal requires non-use/completion proof | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-026` | `DEL-MIG` | Persistence schema changes through reviewed migrations | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-027` | `DEL-MIG` | Model drift is not suppressed to make startup pass | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-028` | `DEL-MIG` | Applied migration history is append-oriented | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-029` | `DEL-MIG` | Existing-data upgrade is separate required proof | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-030` | `DEL-MIG` | Provider-specific persistence is tested on the real provider class | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-031` | `DEL-MIG` | Long backfill is decoupled from startup when necessary | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-032` | `DEL-MIG` | Backfill completion is measurable | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-033` | `DEL-MIG` | Migration protects production workload | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-034` | `DEL-MIG` | Ownership migration changes contracts before storage convenience | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-035` | `DEL-MIG` | Foreign context never mutates new owner storage directly as transitional shortcut | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-036` | `DEL-MIG` | Stable logical identity is preserved unless migration explicitly changes identity | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-037` | `DEL-MIG` | Rebuildable projection prefers rebuild over complex dual-authority migration | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-038` | `DEL-MIG` | Cache compatibility never dictates business schema authority | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-039` | `DEL-MIG` | Backlog compatibility is a migration obligation | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-040` | `DEL-MIG` | Provider mapping migration preserves external and Notrelix identities | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-041` | `DEL-MIG` | Destructive migration requires explicit irreversibility analysis | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-042` | `DEL-MIG` | Backup claim includes restore feasibility | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-043` | `DEL-MIG` | Migration resumes from durable checkpoint | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-044` | `DEL-MIG` | Completion proof matches the migrated meaning | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-045` | `DEL-MIG` | Migration state does not masquerade as final success | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-MIG-046` | `DEL-MIG` | Migration plan is delivery evidence, not new canonical architecture | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-OWN-001` | `DEL-OWN` | Logical owner is stable; staffing is not architecture | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-002` | `DEL-OWN` | Business vocabulary chooses the semantic owner | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-003` | `DEL-OWN` | Mechanism owner does not own business decisions | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-004` | `DEL-OWN` | Layers do not become separate product owners | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-005` | `DEL-OWN` | Repository policy cannot be silently overridden by local owner | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-006` | `DEL-OWN` | New folder/module/team does not create a new bounded context | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-007` | `DEL-OWN` | Technical capability has technical owner unless product semantics justify a context | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-008` | `DEL-OWN` | Backend layer owner and product owner cooperate | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-009` | `DEL-OWN` | Platform cannot define context event meaning by convenience | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-010` | `DEL-OWN` | Provider adapter owner preserves target semantic owner | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-011` | `DEL-OWN` | HTTP endpoint location does not determine business owner | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-012` | `DEL-OWN` | Frontend architecture does not become product owner | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-013` | `DEL-OWN` | Shared UI primitive does not absorb feature behavior | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-014` | `DEL-OWN` | Quality owner defines proof obligations, not product meaning | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-015` | `DEL-OWN` | Delivery owner does not become implementation owner for every change | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-016` | `DEL-OWN` | Operations feedback does not silently redefine product semantics | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-017` | `DEL-OWN` | Deployment topology and semantic ownership are separate | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-018` | `DEL-OWN` | Document owner is accountable for coherence, not personal authorship | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-019` | `DEL-OWN` | CODEOWNERS is review routing, not semantic authority | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-020` | `DEL-OWN` | Documentation does not invent people or GitHub teams | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-021` | `DEL-OWN` | Review follows affected contracts | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-022` | `DEL-OWN` | Initiating owner coordinates; affected owners retain their authority | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-023` | `DEL-OWN` | Collaboration between teams does not authorize boundary bypass | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-024` | `DEL-OWN` | Consumer convenience does not transfer source ownership | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-025` | `DEL-OWN` | Migration executor is not necessarily target semantic owner | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-026` | `DEL-OWN` | Incident remediation returns durable knowledge to canonical owners | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-027` | `DEL-OWN` | Reorg does not trigger package/context rewrite by default | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-028` | `DEL-OWN` | Team topology can be many-to-many with logical capabilities | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-029` | `DEL-OWN` | Ownership matrices do not duplicate the topic authority map | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-030` | `DEL-OWN` | Source folder is evidence, not final ownership answer | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-031` | `DEL-OWN` | Ambiguity does not default to Shared/Common | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-032` | `DEL-OWN` | Cross-cutting use does not imply cross-cutting ownership | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-033` | `DEL-OWN` | Service extraction does not require one current team per context | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-034` | `DEL-OWN` | Security review augments, not replaces, semantic review | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-035` | `DEL-OWN` | Protected property decides escalation path | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-036` | `DEL-OWN` | Generator owner cannot redefine producer semantics | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-037` | `DEL-OWN` | Test helper owner cannot weaken product setup silently | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-038` | `DEL-OWN` | Ownership handoff is architecture/data change, not ticket reassignment | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-039` | `DEL-OWN` | Contributor and owner are distinct | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-OWN-040` | `DEL-OWN` | Durable ownership is repository-discoverable | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DEL-REL-001` | `DEL-REL` | Every deployed stage is valid on its own | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-002` | `DEL-REL` | Compatibility is checked across actual deployment units | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-003` | `DEL-REL` | Release evidence refers to exact revision/artifact | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-004` | `DEL-REL` | Promotion does not silently change the artifact | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-005` | `DEL-REL` | Green CI is necessary but not sufficient for material rollout | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-006` | `DEL-REL` | Staging is evidence only when it reproduces the property | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-007` | `DEL-REL` | Producer-first and consumer-first are deliberate choices | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-008` | `DEL-REL` | Rolling deployment has mixed-instance compatibility | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-009` | `DEL-REL` | Database contraction follows reader/writer migration | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-010` | `DEL-REL` | Worker rollout accounts for queued old work | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-011` | `DEL-REL` | Mobile rollout assumes supported old clients remain active | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-012` | `DEL-REL` | Web deployment does not assume instant browser refresh | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-013` | `DEL-REL` | Feature flag declares lifecycle | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-014` | `DEL-REL` | Release flag and product entitlement are different authorities | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-015` | `DEL-REL` | Cohort assignment is stable | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-016` | `DEL-REL` | Cohort boundary follows shared-state compatibility | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-017` | `DEL-REL` | Feature flag cannot create hidden dual authority | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-018` | `DEL-REL` | Cohort change is a supported state transition | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-019` | `DEL-REL` | Rollout percentage never weakens authorization | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-020` | `DEL-REL` | Kill switch is tested before relying on it | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-021` | `DEL-REL` | Successful rollout includes flag cleanup | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-022` | `DEL-REL` | Canary has explicit expansion and abort criteria | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-023` | `DEL-REL` | Shadow path has no unintended side effects | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-024` | `DEL-REL` | Rollout telemetry uses logical operation/cohort identity | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-025` | `DEL-REL` | Health check does not replace functional smoke evidence | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-026` | `DEL-REL` | Smoke test cannot mutate real customer state casually | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-027` | `DEL-REL` | Rollback is analyzed per surface | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-028` | `DEL-REL` | A green previous binary is not automatically rollback-safe | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-029` | `DEL-REL` | Published facts require forward/compensating recovery | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-030` | `DEL-REL` | External side effect rollback is operation-specific | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-031` | `DEL-REL` | Forward recovery is first-class | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-032` | `DEL-REL` | Rollback decision protects data/security before availability convenience | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-033` | `DEL-REL` | Pause does not mean half-applied invalid migration | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-034` | `DEL-REL` | Config default change is rollout-impacting | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-035` | `DEL-REL` | Credential rotation has overlap/revocation sequence | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-036` | `DEL-REL` | Scheduler ownership prevents duplicate occurrence during rollout | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-037` | `DEL-REL` | Release certification is revision-specific | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-038` | `DEL-REL` | Release does not bypass required gates by manual deployment | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-039` | `DEL-REL` | Expansion waits for relevant health evidence | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-040` | `DEL-REL` | Recovery targets root failure surface | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-REL-041` | `DEL-REL` | Cleanup does not erase useful historical decision evidence | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DOC-001` | `DOC` | One Topic Has One Canonical Normative Owner | [`docs/governance/documentation-authority.md`](../governance/documentation-authority.md) |
| `DOC-002` | `DOC` | Scope Does Not Override Semantic Role | [`docs/governance/documentation-authority.md`](../governance/documentation-authority.md) |
| `DOC-003` | `DOC` | Summaries Route; They Do Not Re-Own | [`docs/governance/documentation-authority.md`](../governance/documentation-authority.md) |
| `DOC-004` | `DOC` | Current Context Is Evidence, Not Durable Intent | [`docs/governance/documentation-authority.md`](../governance/documentation-authority.md) |
| `DOC-005` | `DOC` | Source, Tests, and CI Are Evidence, Not Automatic Precedent | [`docs/governance/documentation-authority.md`](../governance/documentation-authority.md) |
| `DOC-006` | `DOC` | ADRs Preserve Decisions; Canonical Docs Preserve Current Contract | [`docs/governance/documentation-authority.md`](../governance/documentation-authority.md) |
| `DOC-007` | `DOC` | Exact Inventories Are Producer-Owned | [`docs/governance/documentation-authority.md`](../governance/documentation-authority.md) |
| `DOC-008` | `DOC` | Scoped Documentation Requires Distinct Local Responsibility | [`docs/governance/documentation-authority.md`](../governance/documentation-authority.md) |
| `DOC-009` | `DOC` | Skills and Provider Routers Are Procedure, Not Architecture | [`docs/governance/documentation-authority.md`](../governance/documentation-authority.md) |
| `DOC-010` | `DOC` | Cross-Topic Changes May Update Several Owners Without Duplicating Ownership | [`docs/governance/documentation-authority.md`](../governance/documentation-authority.md) |
| `DOC-011` | `DOC` | Authority Migration Is Transactional | [`docs/governance/documentation-authority.md`](../governance/documentation-authority.md) |
| `DOC-012` | `DOC` | Normative Semantic Changes Are Product/Architecture Changes | [`docs/governance/documentation-authority.md`](../governance/documentation-authority.md) |
| `DOC-013` | `DOC` | Historical Artifacts Do Not Remain Active Authority | [`docs/governance/documentation-authority.md`](../governance/documentation-authority.md) |
| `DOC-014` | `DOC` | Versioned/Final/Frozen Filename Generations Are Forbidden Authority Management | [`docs/governance/documentation-authority.md`](../governance/documentation-authority.md) |
| `DOC-015` | `DOC` | Canonical References Are Repository-Relative and Resolvable | [`docs/governance/documentation-authority.md`](../governance/documentation-authority.md) |
| `DOC-016` | `DOC` | Canonical Metadata Is Minimal, Stable, and Machine-Useful | [`docs/governance/documentation-authority.md`](../governance/documentation-authority.md) |
| `DOC-017` | `DOC` | Authority Conflict Is a Stop Condition | [`docs/governance/documentation-authority.md`](../governance/documentation-authority.md) |
| `DOC-018` | `DOC` | Documentation Gates Are Required Executable Architecture | [`docs/governance/documentation-authority.md`](../governance/documentation-authority.md) |
| `FE-API-001` | `FE-API` | Backend/system producer owns wire meaning | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-002` | `FE-API` | Contract producer input is not replaced by handwritten DTOs | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-003` | `FE-API` | Generator owns generated output | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-004` | `FE-API` | REST and realtime wire contracts are distinct but coordinated | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-005` | `FE-API` | Codegen drift is a merge-blocking contract signal | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-006` | `FE-API` | Mapping is allowed; duplication without semantic purpose is not | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-007` | `FE-API` | One mapping owner per semantic representation | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-008` | `FE-API` | Package export defines supported frontend contract surface | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-009` | `FE-API` | API client owns generic transport, not product operation semantics | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-010` | `FE-API` | Components do not construct ad-hoc root API clients | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-011` | `FE-API` | Endpoint constants do not become a second API specification | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-012` | `FE-API` | Product API adapter is thin and typed | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-013` | `FE-API` | Handwritten transport wrapper cannot weaken generated type fidelity | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-014` | `FE-API` | Product packages do not read credential storage | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-015` | `FE-API` | Auth refresh is centralized per client/session owner | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-016` | `FE-API` | Authentication retry is bounded | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-017` | `FE-API` | Transport reports session failure; host decides navigation | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-018` | `FE-API` | Correlation identity is diagnostic, not idempotency identity | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-019` | `FE-API` | Correlation IDs should be unique enough for trace linkage | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-020` | `FE-API` | Idempotency is operation-defined, not globally auto-generated for every mutation | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-021` | `FE-API` | Logical command identity survives transport retry | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-022` | `FE-API` | Cancellation is a transport/lifecycle signal, not server rollback | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-023` | `FE-API` | Response parser preserves transport distinction | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-024` | `FE-API` | Client behavior branches on stable error semantics, not prose | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-025` | `FE-API` | Validation mapping does not invent server field meaning | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-026` | `FE-API` | Conflict is not generic retryable network failure | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-027` | `FE-API` | Client does not reverse-engineer protected resource existence | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-028` | `FE-API` | Retry behavior follows operation/error class | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-029` | `FE-API` | CSRF cookie/header names are a cross-boundary contract | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-030` | `FE-API` | CSRF wire contract is single-spelling, drift-guarded | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-031` | `FE-API` | Feature flag must not hide contract incompatibility | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-032` | `FE-API` | Valid CSRF token grants no resource permission | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-033` | `FE-API` | Browser CSRF belongs to browser transport/runtime | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-034` | `FE-API` | Client does not encode mutation semantics into safe-method workarounds | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-035` | `FE-API` | Accepted is not completed | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-036` | `FE-API` | Pagination state participates in request/cache identity | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-037` | `FE-API` | UI filter model maps to API filter contract explicitly | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-038` | `FE-API` | Client consumes supported public contract, not backend implementation accident | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-039` | `FE-API` | Open enums/unions require forward-compatible client behavior | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-040` | `FE-API` | Contract migration plans for mixed-version deployment | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-041` | `FE-API` | One operation has one active client authority per migration phase | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-042` | `FE-API` | Abstraction depth follows real policy | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-043` | `FE-API` | Product components do not own raw transport by default | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-044` | `FE-API` | Product service group is composition convenience, not global service locator | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-045` | `FE-API` | Test seams are explicit dependencies | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-046` | `FE-API` | Mock convenience does not redefine contract | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-047` | `FE-API` | Contract-critical headers require direct tests | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-048` | `FE-API` | Regeneration is necessary but not sufficient | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-049` | `FE-API` | Realtime event shape and realtime state application are separate owners | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-050` | `FE-API` | Transport codec follows endpoint contract | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-051` | `FE-API` | Default header is not universal protocol | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-052` | `FE-API` | Credential mode is runtime security architecture | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-053` | `FE-API` | Endpoint environment is host/runtime configuration | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-054` | `FE-API` | Diagnostics preserve privacy | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-055` | `FE-API` | User-facing error uses safe normalized message | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-056` | `FE-API` | Backend internal refactor is not frontend contract change unless public behavior changes | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-057` | `FE-API` | Client targets public capability contract, not internal service decomposition | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-058` | `FE-API` | Missing producer contract is a stop condition | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-059` | `FE-API` | Contract disagreement is not fixed by whichever side is easier to edit | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-API-060` | `FE-API` | CSRF debt must be closed before relying on enabled protection | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-ARCH-001` | `FE-ARCH` | Frontend is a client platform, not a second backend | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-002` | `FE-ARCH` | Backend contract precedes durable client behavior | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-003` | `FE-ARCH` | Host framework split is intentional | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-004` | `FE-ARCH` | Apps compose; apps do not own reusable product semantics | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-005` | `FE-ARCH` | Exact dependency permission is executable | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-006` | `FE-ARCH` | Architecture layer and product context are different dimensions | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-007` | `FE-ARCH` | Freeze means stable foundation, not finished product | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-008` | `FE-ARCH` | Foundation is not a reuse dumping ground | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-009` | `FE-ARCH` | Kernel remains semantically small | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-010` | `FE-ARCH` | Generated contract output is evidence, not hand-authored truth | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-011` | `FE-ARCH` | Platform abstraction is narrow and typed | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-012` | `FE-ARCH` | Generic query mechanism and product query policy are separate | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-013` | `FE-ARCH` | Realtime transport does not own product semantics | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-014` | `FE-ARCH` | Runtime adapts platform; runtime does not own product behavior | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-015` | `FE-ARCH` | Runtime direction is host inward | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-016` | `FE-ARCH` | Share design semantics, not incompatible rendering machinery | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-017` | `FE-ARCH` | Tokens are semantic design inputs | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-018` | `FE-ARCH` | Product family shape follows capability need, not symmetry | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-019` | `FE-ARCH` | Product core does not become backend Domain duplicate | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-020` | `FE-ARCH` | Product state is derived from authoritative server state | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-021` | `FE-ARCH` | Collaboration semantics and realtime mechanism remain separate | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-022` | `FE-ARCH` | Product plugin scope is bounded | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-023` | `FE-ARCH` | Adapter direction is product semantics toward platform presentation | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-024` | `FE-ARCH` | Verification code is not production dependency | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-025` | `FE-ARCH` | Feature package remains least privilege | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-026` | `FE-ARCH` | Package type follows ownership pressure, not folder size | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-027` | `FE-ARCH` | Lower semantic layers do not depend on host composition | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-028` | `FE-ARCH` | Cross-package deep imports are forbidden by default | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-029` | `FE-ARCH` | Public export expansion is deliberate | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-030` | `FE-ARCH` | Workspace membership does not grant import permission | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-031` | `FE-ARCH` | Tooling does not become product runtime dependency by default | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-032` | `FE-ARCH` | Build tooling is not runtime architecture | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-033` | `FE-ARCH` | Contract direction is producer to client | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-034` | `FE-ARCH` | Client cache is derived state | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-035` | `FE-ARCH` | State ownership follows state class | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-036` | `FE-ARCH` | Realtime is reconciliation, not independent truth | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-037` | `FE-ARCH` | Frontend authorization UX is non-authoritative | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-038` | `FE-ARCH` | Client scope identifiers are inputs, not authority | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-039` | `FE-ARCH` | Product parity does not require component parity | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-040` | `FE-ARCH` | Native-safe graph is architectural | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-041` | `FE-ARCH` | Marketing remains isolated from product runtime | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-042` | `FE-ARCH` | Client packaging does not redefine bounded contexts | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-043` | `FE-ARCH` | UI composition does not transfer write ownership | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-044` | `FE-ARCH` | Routine feature work should not require architecture-wide edits | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-045` | `FE-ARCH` | Dependency allow-list change is architecture evidence | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-046` | `FE-ARCH` | Package count is not an architecture quality metric | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-047` | `FE-ARCH` | Structural simplification preserves semantic separation | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-048` | `FE-ARCH` | Architecture drift is classified before repair | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-049` | `FE-ARCH` | Manifest change and architecture doc change are atomic when semantics change | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-050` | `FE-ARCH` | Machine-detectable foundation rules should be gated | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-051` | `FE-ARCH` | Existing dependency is not automatic precedent | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-052` | `FE-ARCH` | Host dependency breadth is not reusable-package permission | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-053` | `FE-ARCH` | Host isolation is enforced by dependency direction | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-054` | `FE-ARCH` | Neutrality is behavioral, not naming | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-055` | `FE-ARCH` | Framework dependency follows package responsibility | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-056` | `FE-ARCH` | State architecture survives library replacement conceptually | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-057` | `FE-ARCH` | URL/navigation is host/client state, not product aggregate state | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-058` | `FE-ARCH` | Observability failure does not change product correctness | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-059` | `FE-ARCH` | Accessibility belongs in architecture boundaries, not late visual polish | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-060` | `FE-ARCH` | Platform adaptation may differ while operation meaning remains stable | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-061` | `FE-ARCH` | Client-delivered code contains no server secret | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-062` | `FE-ARCH` | Capability-scoped public UI remains bounded | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-063` | `FE-ARCH` | Performance optimization does not bypass architecture silently | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-064` | `FE-ARCH` | Shared foundation changes have higher coordination cost | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-065` | `FE-ARCH` | Future flexibility does not justify speculative abstraction | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-066` | `FE-ARCH` | Package boundary and deployment boundary are different | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-067` | `FE-ARCH` | Product extraction preserves source-of-truth boundaries | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-068` | `FE-ARCH` | Promotion is ownership migration, not copy-and-leave | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-069` | `FE-ARCH` | Simplification does not erase capability semantics | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-070` | `FE-ARCH` | Architecture significance follows durable coupling | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-071` | `FE-ARCH` | Executable gate and authored rationale stay aligned | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-072` | `FE-ARCH` | Architecture docs avoid volatile inventory duplication | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-CHG-001` | `FE-ARCH-CHG` | Architecture is changed deliberately, never incidentally | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-002` | `FE-ARCH-CHG` | Additive behavior inside existing boundaries is not architecture change by default | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-003` | `FE-ARCH-CHG` | Architecture significance follows durable coupling | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-004` | `FE-ARCH-CHG` | Change the correct authority | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-005` | `FE-ARCH-CHG` | Conflict classification precedes repair | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-006` | `FE-ARCH-CHG` | DOCSTALE does not require a new architecture decision | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-007` | `FE-ARCH-CHG` | SOURCEDEBT is repaired toward accepted architecture unless architecture is intentionally changed | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-008` | `FE-ARCH-CHG` | Transition has owner and removal condition | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-009` | `FE-ARCH-CHG` | Contract change follows producer and compatibility authority | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-010` | `FE-ARCH-CHG` | UNRESOLVED is not permission to choose locally | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-011` | `FE-ARCH-CHG` | ADR explains why; architecture document explains how it works now | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-012` | `FE-ARCH-CHG` | Accepted ADR history is not silently rewritten | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-013` | `FE-ARCH-CHG` | Consequential foundation choice requires decision history | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-014` | `FE-ARCH-CHG` | Do not create ADRs for routine implementation choices | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-015` | `FE-ARCH-CHG` | Exception is bounded and temporary | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-016` | `FE-ARCH-CHG` | Manifest edit is not the first step for a forbidden import | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-017` | `FE-ARCH-CHG` | New edge requires least-privilege rationale | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-018` | `FE-ARCH-CHG` | Remove permission only after source no longer requires it | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-019` | `FE-ARCH-CHG` | Layer change is architecture-significant | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-020` | `FE-ARCH-CHG` | Package creation is justified by boundary value | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-021` | `FE-ARCH-CHG` | Package removal proves semantic owner continuity | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-022` | `FE-ARCH-CHG` | Rename classification is explicit | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-023` | `FE-ARCH-CHG` | Public export change receives contract review | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-024` | `FE-ARCH-CHG` | Deep import never becomes precedent by repetition | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-025` | `FE-ARCH-CHG` | New host requires ADR and host architecture plan | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-026` | `FE-ARCH-CHG` | Host framework replacement preserves inner contracts where practical | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-027` | `FE-ARCH-CHG` | Microfrontend adoption requires a new ADR | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-028` | `FE-ARCH-CHG` | Runtime ownership changes require platform-impact review | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-029` | `FE-ARCH-CHG` | Global service locator requires explicit architecture decision | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-030` | `FE-ARCH-CHG` | State authority migration requires ADR when foundation changes | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-031` | `FE-ARCH-CHG` | Mechanism replacement does not automatically require ADR | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-032` | `FE-ARCH-CHG` | Broad persisted cache requires architecture decision | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-033` | `FE-ARCH-CHG` | Realtime ordering/recovery foundation change requires durable rationale | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-034` | `FE-ARCH-CHG` | New adapter is not ADR-worthy by default | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-035` | `FE-ARCH-CHG` | Contract-generation foundation is governed | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-036` | `FE-ARCH-CHG` | Frontend records consumer impact; backend owns producer approval | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-037` | `FE-ARCH-CHG` | Auth/session foundation change requires ADR | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-038` | `FE-ARCH-CHG` | Repair contract drift without inventing a new decision | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-039` | `FE-ARCH-CHG` | Foundational UI changes require migration across evidence | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-040` | `FE-ARCH-CHG` | Visual redesign does not require ADR when design-system architecture remains intact | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-041` | `FE-ARCH-CHG` | Critical gate-foundation change is architecture/governance change | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-042` | `FE-ARCH-CHG` | CI optimization preserves evidence semantics | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-043` | `FE-ARCH-CHG` | Freeze-scope change requires explicit rationale | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-044` | `FE-ARCH-CHG` | Migration has explicit phases | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-045` | `FE-ARCH-CHG` | Compatibility layer has removal condition | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-046` | `FE-ARCH-CHG` | Semantic architecture change updates all affected authorities in one governed transaction | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-047` | `FE-ARCH-CHG` | Generated evidence is output, not a review shortcut | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-048` | `FE-ARCH-CHG` | Canonical MUST without executable protection is reviewed for gateability | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-049` | `FE-ARCH-CHG` | Do not delete the gate while keeping the rule unenforced accidentally | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-050` | `FE-ARCH-CHG` | Architecture review is cross-boundary where impact is cross-boundary | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-051` | `FE-ARCH-CHG` | Governance protects foundations, not every line of feature code | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-052` | `FE-ARCH-CHG` | Architecture change requires stronger negative proof | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-053` | `FE-ARCH-CHG` | Classification states old rule and new rule | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-054` | `FE-ARCH-CHG` | Do not fabricate ADR alternatives | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-055` | `FE-ARCH-CHG` | Current stewardship and historical authorship are distinct | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-056` | `FE-ARCH-CHG` | Supersession is explicit and bidirectional | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-057` | `FE-ARCH-CHG` | Do not pre-reserve speculative ADR IDs in architecture docs | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-058` | `FE-ARCH-CHG` | New architecture topic requires a real authority gap | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-059` | `FE-ARCH-CHG` | Local docs route outward for global rules | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-060` | `FE-ARCH-CHG` | Migrate unique knowledge, then retire old authority | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-061` | `FE-ARCH-CHG` | Do not expand feature scope silently to redesign foundation | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-062` | `FE-ARCH-CHG` | Urgency does not make temporary architecture permanent | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-063` | `FE-ARCH-CHG` | Security patch and architecture record converge | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-064` | `FE-ARCH-CHG` | Client/server compatibility assumes non-atomic rollout unless proven otherwise | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-065` | `FE-ARCH-CHG` | Monorepo atomic source does not justify unbounded internal contracts | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-066` | `FE-ARCH-CHG` | Rollback claim includes durable/client-cache effects | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-067` | `FE-ARCH-CHG` | Completion is evidence-based | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-068` | `FE-ARCH-CHG` | Old green CI does not certify new architecture | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-069` | `FE-ARCH-CHG` | Package appears in manifest exactly once | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-070` | `FE-ARCH-CHG` | Dependency graph is reviewed before code proliferation | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-071` | `FE-ARCH-CHG` | Two state authorities cannot remain indefinitely | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-072` | `FE-ARCH-CHG` | Realtime migration proves gap/duplicate/order behavior | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-073` | `FE-ARCH-CHG` | UI migration avoids permanent parallel design systems | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-CHG-074` | `FE-ARCH-CHG` | Gate migration has no unprotected window | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-DEC-001` | `FE-DEC` | Frontend ADR IDs use FE-ADR-NNN | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-002` | `FE-DEC` | The registry reflects ADR status; it does not invent status | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-003` | `FE-DEC` | ADR is rationale history, not current operating manual | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-004` | `FE-DEC` | Routine feature work does not require an ADR | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-005` | `FE-DEC` | ADR depth follows decision cost | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-006` | `FE-DEC` | New ADRs use the full repository schema | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-007` | `FE-DEC` | Normalization is not historical rewriting | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-008` | `FE-DEC` | Current stewardship is distinct from historical authorship | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-009` | `FE-DEC` | Alternatives must be historically recoverable or current-decision real | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-010` | `FE-DEC` | Historical consequence and current evidence are labeled separately | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-011` | `FE-DEC` | Evidence may evolve without a new ADR | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-012` | `FE-DEC` | Accepted ADR is superseded, not silently edited | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-013` | `FE-DEC` | Superseded ADR remains discoverable | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-014` | `FE-DEC` | Rejected status preserves decision context, not backlog | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-015` | `FE-DEC` | Deprecated is not a substitute for Superseded | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-016` | `FE-DEC` | Proposed decision does not authorize source drift | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-017` | `FE-DEC` | Temporary debt does not become an ADR merely to legitimize it | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-018` | `FE-DEC` | ADR does not replace migration plan | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-019` | `FE-DEC` | ADR does not duplicate volatile package inventory | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-020` | `FE-DEC` | Accepted decision and current architecture move together | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-021` | `FE-DEC` | Registry is complete for active frontend ADR files | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-022` | `FE-DEC` | ADR ID uniqueness is directory-wide | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-023` | `FE-DEC` | Do not reserve FE-ADR-006 speculatively | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-024` | `FE-DEC` | FE-ADR-001 identity is the host/framework separation decision | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-025` | `FE-DEC` | Tool version update does not automatically supersede package-manager ADR | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-026` | `FE-DEC` | New supported subpath can be current contract evolution without new ADR | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-027` | `FE-DEC` | FE-ADR-004 is about framework contamination, not banning all web-specific code | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-028` | `FE-DEC` | Accepted status does not freeze stale protocol spelling | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-029` | `FE-DEC` | Historical implementation defects can coexist with an accepted architectural decision | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-030` | `FE-DEC` | New contributors read architecture before ADR history | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-031` | `FE-DEC` | Evidence claim is no stronger than its source | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-032` | `FE-DEC` | Periodic evidence refresh does not reopen accepted decision automatically | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-033` | `FE-DEC` | Normalization date is not decision date | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-034` | `FE-DEC` | Git author is not automatically architecture decision owner | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEC-035` | `FE-DEC` | Compatibility section may distinguish historical record from current implications | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-DEP-001` | `FE-DEP` | Package boundaries are architectural constraints | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-002` | `FE-DEP` | The manifest is closed-world | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-003` | `FE-DEP` | Exact allowed internal imports come from the manifest | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-004` | `FE-DEP` | Invalid policy data fails before import scanning | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-005` | `FE-DEP` | Workspace membership does not imply import permission | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-006` | `FE-DEP` | Generated dependency docs are not edited by hand | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-007` | `FE-DEP` | Volatile inventory remains generated | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-008` | `FE-DEP` | Layer names encode responsibility, not prestige | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-009` | `FE-DEP` | Conceptual direction does not replace exact policy | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-010` | `FE-DEP` | Foundation imports inward, not outward | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-011` | `FE-DEP` | Kernel may not absorb product/runtime concepts | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-012` | `FE-DEP` | Wire contracts do not depend on feature presentation | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-013` | `FE-DEP` | Abstraction does not import its host implementation | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-014` | `FE-DEP` | Product query semantics depend on query foundation | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-015` | `FE-DEP` | Realtime foundation cannot import product event consumers | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-016` | `FE-DEP` | Telemetry vendor dependency does not propagate by convenience | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-017` | `FE-DEP` | Runtime packages remain product-agnostic | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-018` | `FE-DEP` | Web runtime is not a mobile dependency | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-019` | `FE-DEP` | Mobile runtime remains DOM-free | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-020` | `FE-DEP` | UI tokens do not depend on rendering implementation | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-021` | `FE-DEP` | Generic web UI does not import product packages | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-022` | `FE-DEP` | Web/mobile UI implementations remain separate | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-023` | `FE-DEP` | Visual asset packages remain semantically neutral | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-024` | `FE-DEP` | Product core remains host-neutral | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-025` | `FE-DEP` | State owner does not depend on screen composition | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-026` | `FE-DEP` | Collaboration does not bypass product state ownership accidentally | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-027` | `FE-DEP` | Plugin boundary is not an architecture escape hatch | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-028` | `FE-DEP` | Adapter is the preferred platform-coupling boundary for reusable product capability | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-029` | `FE-DEP` | Verification edges are one-way | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-030` | `FE-DEP` | Feature dependencies are explicit, not inherited from a universal base architecture | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-031` | `FE-DEP` | Cross-feature dependency requires ownership analysis | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-032` | `FE-DEP` | App breadth does not justify package breadth | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-033` | `FE-DEP` | Web app must not deep-import package internals | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-034` | `FE-DEP` | Mobile app does not mirror web app dependency breadth | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-035` | `FE-DEP` | Marketing cannot import authenticated product runtime | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-036` | `FE-DEP` | Public entrypoints are the cross-package contract | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-037` | `FE-DEP` | Deep-import prohibition applies even when TypeScript resolves it | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-038` | `FE-DEP` | Export only stable package responsibility | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-039` | `FE-DEP` | Supported subpath is not equivalent to arbitrary internal path | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-040` | `FE-DEP` | External dependency belongs to the narrowest owner | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-041` | `FE-DEP` | Host framework libraries stay at host/adapter/UI boundaries unless architecture explicitly permits otherwise | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-042` | `FE-DEP` | Type-only coupling can still violate ownership | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-043` | `FE-DEP` | Internal dependency permission is direct and explicit | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-044` | `FE-DEP` | Import permission and package declaration both matter | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-045` | `FE-DEP` | New internal cycles are forbidden by default | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-046` | `FE-DEP` | Composition resolves dependencies; low-level packages do not locate outer services | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-047` | `FE-DEP` | Mobile production packages reject web-only packages and APIs | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-048` | `FE-DEP` | Native safety includes source syntax, not only package.json | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-049` | `FE-DEP` | Share behavior, not incompatible platform implementation | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-050` | `FE-DEP` | Web rendering compatibility does not erase marketing semantic isolation | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-051` | `FE-DEP` | New package is an architecture action | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-052` | `FE-DEP` | Generator does not authorize architecture | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-053` | `FE-DEP` | Remove authority, not only files | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-054` | `FE-DEP` | Package rename preserves architecture identity deliberately | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-055` | `FE-DEP` | Cross-package move requires semantic review | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-056` | `FE-DEP` | Lower-layer promotion increases reuse obligations | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-057` | `FE-DEP` | Repeated dependency friction triggers architecture review | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-058` | `FE-DEP` | Allow-list edits require rationale | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-059` | `FE-DEP` | Manifest and generated package map move atomically | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-060` | `FE-DEP` | Architecture checker failure is not solved by weakening the checker first | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-061` | `FE-DEP` | Architecture tooling is production-governance code | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-062` | `FE-DEP` | Allowed edge is maximum permission, not required edge | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-063` | `FE-DEP` | Permission surface should not grow monotonically without review | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-064` | `FE-DEP` | Test convenience does not redefine production dependency architecture | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-065` | `FE-DEP` | Exception is temporary permission, not precedent | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-066` | `FE-DEP` | Client secret handling cannot be solved by package placement | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-067` | `FE-DEP` | Bundle optimization does not justify boundary bypass | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-068` | `FE-DEP` | Architecture optimizes ownership clarity, not package count | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-069` | `FE-DEP` | Do not normalize product topology for visual symmetry | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-070` | `FE-DEP` | Feature capability determines dependency capability | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-071` | `FE-DEP` | Do not assume the newest source edge is the intended architecture | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-072` | `FE-DEP` | Current architecture describes now; ADR explains why | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-073` | `FE-DEP` | Next.js is not a general reusable-package framework | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-074` | `FE-DEP` | Navigation dependencies stay outer | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-075` | `FE-DEP` | Mechanism APIs stay in mechanism/state owners | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-076` | `FE-DEP` | Provider composition does not erase package ownership | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-077` | `FE-DEP` | Creator owns disposal unless a narrower lifecycle contract says otherwise | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-078` | `FE-DEP` | Importing a package should not secretly start the application | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-DEP-079` | `FE-DEP` | Architecture permission does not transfer semantic ownership | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-HOST-001` | `FE-HOST` | Apps are composition roots | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-002` | `FE-HOST` | Host framework differences remain host-local | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-003` | `FE-HOST` | Environment is normalized before runtime construction | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-004` | `FE-HOST` | Public frontend environment is not secret storage | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-005` | `FE-HOST` | Runtime construction happens at an outer owner | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-006` | `FE-HOST` | Application services are typed composition, not global lookup | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-007` | `FE-HOST` | The lifecycle owner disposes what it creates | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-008` | `FE-HOST` | Avoid import-time runtime startup | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-009` | `FE-HOST` | Provider tree composes owners; it does not transfer ownership | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-010` | `FE-HOST` | Provider order follows dependency prerequisites | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-011` | `FE-HOST` | Disposal occurs once at the owning boundary | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-012` | `FE-HOST` | Router belongs to the host | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-013` | `FE-HOST` | Routes delegate reusable behavior | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-014` | `FE-HOST` | Route generation source and output remain synchronized | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-015` | `FE-HOST` | Router context is typed host composition | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-016` | `FE-HOST` | Inner runtime requests navigation through an outward adapter | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-017` | `FE-HOST` | Redirect/return URLs are untrusted navigation input | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-018` | `FE-HOST` | Route IDs are inputs, not authorization | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-019` | `FE-HOST` | Route guard does not replace backend authorization | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-020` | `FE-HOST` | Session composition is outer; session semantics remain package-owned | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-021` | `FE-HOST` | Principal change is a lifecycle boundary | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-022` | `FE-HOST` | Workspace route transition is not merely URL replacement | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-023` | `FE-HOST` | Scope transition must prevent old-scope bleed | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-024` | `FE-HOST` | Global fallback is not a substitute for product loading UX | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-025` | `FE-HOST` | Host error boundary contains failures; it does not hide them | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-026` | `FE-HOST` | Error-boundary telemetry is diagnostic only | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-027` | `FE-HOST` | Route 404 and resource 404 are different concerns | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-028` | `FE-HOST` | Mobile navigation is native-host owned | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-029` | `FE-HOST` | Mobile runtime/service construction is native-specific but lifecycle-equivalent | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-030` | `FE-HOST` | Mobile route files do not become product state owners | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-031` | `FE-HOST` | Mobile lifecycle handling stays in host/runtime boundary | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-032` | `FE-HOST` | Deep link capability does not grant resource capability | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-033` | `FE-HOST` | Marketing owns content/SEO/marketing routing | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-034` | `FE-HOST` | Marketing remains product-runtime isolated | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-035` | `FE-HOST` | Next server/client boundary must remain explicit | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-036` | `FE-HOST` | Apps do not import each other's internals | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-037` | `FE-HOST` | Shell is outer composition, not product authority | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-038` | `FE-HOST` | Global surface ownership separates mechanism and semantics | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-039` | `FE-HOST` | Host wires theme runtime; UI system owns theme semantics | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-040` | `FE-HOST` | One host-scoped query runtime does not mean one global state owner | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-041` | `FE-HOST` | Host coordinates realtime lifecycle; product packages own product reconciliation | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-042` | `FE-HOST` | Service lifetime matches scope | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-043` | `FE-HOST` | Host state does not absorb server resource state | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-044` | `FE-HOST` | Shareable navigation state prefers URL where product UX requires it | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-045` | `FE-HOST` | Route loader delegates to state/contracts | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-046` | `FE-HOST` | Lazy loading does not change semantic ownership | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-047` | `FE-HOST` | Navigation visibility is UX, not security | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-048` | `FE-HOST` | Auth route component is adapter around auth feature | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-049` | `FE-HOST` | Post-auth destination is not trusted client memory | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-050` | `FE-HOST` | Clear sensitive scoped state before exposing new principal/public shell | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-051` | `FE-HOST` | Recovery matches failure scope | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-052` | `FE-HOST` | Critical host configuration validates early | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-053` | `FE-HOST` | Partially initialized runtime is not normal application state | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-054` | `FE-HOST` | Test substitution happens at composition contracts | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-055` | `FE-HOST` | One host test does not prove another host | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-056` | `FE-HOST` | Build proves packaging, not user-flow correctness | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-057` | `FE-HOST` | Mobile architecture gate and mobile build are complementary | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-058` | `FE-HOST` | Marketing build does not prove deployment health | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-059` | `FE-HOST` | Host transport differences do not fork product mutation meaning | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-060` | `FE-HOST` | Browser security mechanism belongs to web host/runtime boundary | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-061` | `FE-HOST` | Credential storage is runtime/session concern | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-062` | `FE-HOST` | Marketing does not reuse browser app session by default | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-063` | `FE-HOST` | Host observability enriches; it does not centralize all product analytics semantics | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-064` | `FE-HOST` | Development HMR must not leak long-lived services | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-065` | `FE-HOST` | Long-lived service creation is outside unstable render paths or memoized/owned deliberately | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-066` | `FE-HOST` | Construction, render and side-effect phases remain explicit | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-067` | `FE-HOST` | Root provider change has high fan-out | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-068` | `FE-HOST` | Route count growth does not imply route-layer business ownership | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-069` | `FE-HOST` | Mobile parity is semantic, not route-file parity | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-070` | `FE-HOST` | Marketing demo visuals are not production product state | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-071` | `FE-HOST` | Account switch is a principal-scope transition | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-072` | `FE-HOST` | Workspace switch has one coordinated transition contract | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-073` | `FE-HOST` | Route availability does not replace server feature/entitlement enforcement | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-074` | `FE-HOST` | Public route does not automatically mean public backend data | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-075` | `FE-HOST` | Share route does not bootstrap full authenticated Workspace authority by default | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-076` | `FE-HOST` | Host composition remains explicit static dependency composition by default | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-077` | `FE-HOST` | Replaceability is achieved through correct boundaries, not universal wrappers | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-078` | `FE-HOST` | Composition drift requires ownership review | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-HOST-079` | `FE-HOST` | ADR rationale is historical; this file describes current desired host architecture | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-RT-001` | `FE-RT` | Realtime supplements backend authority | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-002` | `FE-RT` | Event wire shape is generated/producer-owned | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-003` | `FE-RT` | Foundation owns mechanism, not product mutation meaning | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-004` | `FE-RT` | Realtime client is lifecycle-owned | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-005` | `FE-RT` | Connection state is explicit | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-006` | `FE-RT` | Connection authentication is runtime/session-owned | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-007` | `FE-RT` | Connection descriptor is an outer adapter | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-008` | `FE-RT` | Web/mobile connection mechanism may differ while event semantics remain shared | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-009` | `FE-RT` | Realtime endpoint is runtime configuration | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-010` | `FE-RT` | Transport construction has explicit test seams | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-011` | `FE-RT` | Reconnect policy is bounded and observable | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-012` | `FE-RT` | Reconnect tuning is not product state | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-013` | `FE-RT` | Manual lifecycle transition does not auto-reconnect | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-014` | `FE-RT` | Disposal removes retained realtime state | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-015` | `FE-RT` | Liveness detection is transport responsibility | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-016` | `FE-RT` | Connection failure and business failure are distinct | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-017` | `FE-RT` | Untrusted realtime input is validated before product dispatch | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-018` | `FE-RT` | Control protocol does not leak into product adapters | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-019` | `FE-RT` | Event identity is stable enough for duplicate suppression | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-020` | `FE-RT` | Dedup memory is bounded | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-021` | `FE-RT` | Product event handling remains idempotent where practical | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-022` | `FE-RT` | Sequence scope matches producer ordering scope | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-023` | `FE-RT` | Stale sequence is not applied to product state | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-024` | `FE-RT` | Gap means local incremental history is incomplete | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-025` | `FE-RT` | Recovery re-establishes both data truth and sequence continuity | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-026` | `FE-RT` | Current recovery continuation is UNRESOLVED until proven | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-027` | `FE-RT` | Recovery scope follows subscription/event ownership | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-028` | `FE-RT` | Workspace subscription is explicitly scoped | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-029` | `FE-RT` | Subscription identity is not UI component identity | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-030` | `FE-RT` | Host coordinates connection/subscription lifecycle | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-031` | `FE-RT` | Session generation prevents stale credential lifecycle reuse | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-032` | `FE-RT` | Logout disconnects protected realtime before old principal state remains active | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-033` | `FE-RT` | Workspace switch unbinds old Workspace subscriptions | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-034` | `FE-RT` | Active route scope is subscription input, not permission authority | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-035` | `FE-RT` | Product adapters are explicit dispatch boundaries | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-036` | `FE-RT` | Multiple adapters handling one event must not create competing truth | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-037` | `FE-RT` | Unknown event is observable and safely ignored/recovered | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-038` | `FE-RT` | Product adapter failure is isolated and observable | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-039` | `FE-RT` | Realtime adapter invalidates/updates through state-owner contracts | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-040` | `FE-RT` | Invalidate when event payload cannot prove a complete safe patch | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-041` | `FE-RT` | Event patch observes version/order contract | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-042` | `FE-RT` | REST and realtime converge on one state owner | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-043` | `FE-RT` | Optimistic command and realtime event share logical identity where contract permits | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-044` | `FE-RT` | Presence state is explicitly ephemeral | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-045` | `FE-RT` | Collaboration package consumes realtime; realtime foundation does not import collaboration | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-046` | `FE-RT` | Reconnect is complete only after subscription/recovery contract is restored | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-047` | `FE-RT` | Offline is a freshness condition | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-048` | `FE-RT` | Native lifecycle does not leak into product adapters | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-049` | `FE-RT` | Realtime telemetry uses safe identifiers | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-050` | `FE-RT` | Subscriber exception does not stop unrelated subscribers | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-051` | `FE-RT` | Filter is a client optimization/correctness guard, not server authorization | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-052` | `FE-RT` | Permission change invalidates previously visible state | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-053` | `FE-RT` | Membership loss is a scope transition | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-054` | `FE-RT` | Forward compatibility avoids total client failure on unknown event type | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-055` | `FE-RT` | Realtime contract migration considers mixed client/server versions | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-056` | `FE-RT` | Sequence does not automatically imply replay | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-057` | `FE-RT` | Sequence epoch changes are explicit | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-058` | `FE-RT` | Wall-clock timestamp is not sequence substitute by default | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-059` | `FE-RT` | Client dedup uses event identity, not payload equality | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-060` | `FE-RT` | Recovery UX describes freshness, not transport jargon | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-061` | `FE-RT` | Transport properties have deterministic unit tests | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-062` | `FE-RT` | Adapter test proves product consequence, not only handler call | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-063` | `FE-RT` | Gap recovery test proves next valid live event can be consumed | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-064` | `FE-RT` | Cross-Workspace negative test is mandatory for lifecycle changes | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-065` | `FE-RT` | Old-session socket cannot remain authoritative | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-066` | `FE-RT` | Mock socket success does not prove backend realtime compatibility | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-067` | `FE-RT` | Recovery/order architecture change is not a local refactor | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-RT-068` | `FE-RT` | Existing realtime shortcut is not precedent | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-STATE-001` | `FE-STATE` | Backend is authoritative for server state | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-002` | `FE-STATE` | State class determines owner | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-003` | `FE-STATE` | Query foundation owns mechanism, not every resource | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-004` | `FE-STATE` | Query client defaults are mechanism policy, not product truth | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-005` | `FE-STATE` | Retry is error/operation-aware | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-006` | `FE-STATE` | Every server-state query key has an explicit scope root | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-007` | `FE-STATE` | Global scope is semantic, not convenience | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-008` | `FE-STATE` | Workspace-scoped key includes Workspace identity | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-009` | `FE-STATE` | Product key factory is the owner of resource key structure | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-010` | `FE-STATE` | Parent/child key hierarchy is deliberate | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-011` | `FE-STATE` | Account-scoped cache must distinguish Account identity or be hard-reset on Account transition | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-012` | `FE-STATE` | Query-key safety is proven by identity plus lifecycle | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-013` | `FE-STATE` | Resource ID alone is insufficient for tenant-scoped cache | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-014` | `FE-STATE` | Same key means same authoritative query identity | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-015` | `FE-STATE` | Query function does not contain UI rendering state | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-016` | `FE-STATE` | Multiple views share server state rather than fork authoritative models | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-017` | `FE-STATE` | Derived projection is recomputable | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-018` | `FE-STATE` | Cache mutation occurs through the owning state boundary | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-019` | `FE-STATE` | Cache transformation uses product semantics from the correct owner | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-020` | `FE-STATE` | Mutation owner defines the full client lifecycle | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-021` | `FE-STATE` | Optimistic update converges to server truth | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-022` | `FE-STATE` | Rollback restores absence as well as value | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-023` | `FE-STATE` | Multi-cache command rolls back as one logical operation | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-024` | `FE-STATE` | One logical optimistic command has one update plan per target key | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-025` | `FE-STATE` | Client command identity and optimistic projection belong to one logical operation | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-026` | `FE-STATE` | Optimism is a UX optimization, not a default mutation architecture | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-027` | `FE-STATE` | Temporary identity never silently becomes permanent server identity | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-028` | `FE-STATE` | Optimistic entity fills only predictable client fields | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-029` | `FE-STATE` | Multiple mutation implementation styles must preserve one lifecycle contract | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-030` | `FE-STATE` | Invalidate when local patch cannot be proven complete | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-031` | `FE-STATE` | Authoritative response patch is stronger than optimistic guess | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-032` | `FE-STATE` | Convergence path is explicit | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-033` | `FE-STATE` | Conflict policy is product/operation-specific | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-034` | `FE-STATE` | Non-idempotent mutation is never blindly auto-retried | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-035` | `FE-STATE` | Prevent stale in-flight query from overwriting optimistic state | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-036` | `FE-STATE` | Old-scope response cannot populate new-scope identity | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-037` | `FE-STATE` | Workspace transition cannot expose old Workspace data under new Workspace context | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-038` | `FE-STATE` | Account transition is a cache-security boundary | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-039` | `FE-STATE` | Principal change invalidates previous principal's protected state | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-040` | `FE-STATE` | QueryClient scope and cache isolation contract are explicit | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-041` | `FE-STATE` | Persisted server cache requires an explicit policy | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-042` | `FE-STATE` | Server-state duplication into local store requires a bounded reason | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-043` | `FE-STATE` | Form draft has explicit reset/rebase behavior | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-044` | `FE-STATE` | URL state is not duplicated into global store without purpose | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-045` | `FE-STATE` | Locality is preferred for ephemeral UI state | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-046` | `FE-STATE` | Do not store recomputable derived state as competing authority | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-047` | `FE-STATE` | Selection does not mutate server state unless product operation says so | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-048` | `FE-STATE` | Pending is not committed | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-049` | `FE-STATE` | Long-running command separates submission from execution status | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-050` | `FE-STATE` | REST mutation and realtime event converge on one state owner | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-051` | `FE-STATE` | Reconciliation is order-tolerant | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-052` | `FE-STATE` | Destructive optimism requires stronger rollback proof | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-053` | `FE-STATE` | Create success reconciles all server-assigned fields | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-054` | `FE-STATE` | Partial update patch distinguishes omitted and explicit null | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-055` | `FE-STATE` | Move optimism updates all affected containers consistently | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-056` | `FE-STATE` | Client ordering projection is not persistence authority | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-057` | `FE-STATE` | Product cache transformer avoids hidden side effects | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-058` | `FE-STATE` | Do not mutate cached objects in place unless library/owner contract explicitly permits it | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-059` | `FE-STATE` | Aggregate cache is a product decision, not global default | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-060` | `FE-STATE` | Duplicate representations require explicit fan-out or authoritative invalidation | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-061` | `FE-STATE` | Cross-context UI composition does not own cross-context cache mutation | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-062` | `FE-STATE` | Query/mutation cannot execute with unresolved required scope | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-063` | `FE-STATE` | Non-null assertion is not scope validation | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-064` | `FE-STATE` | Missing identity is not a cache key | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-065` | `FE-STATE` | Error state is request/query state, not resource truth | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-066` | `FE-STATE` | Permission-sensitive cache cannot remain visible indefinitely after revocation signal | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-067` | `FE-STATE` | Deletion outcome removes active ownership | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-068` | `FE-STATE` | Cache lifecycle follows product lifecycle | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-069` | `FE-STATE` | Offline mutation queue requires explicit architecture | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-070` | `FE-STATE` | Network recovery revalidates authority | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-071` | `FE-STATE` | Cache timing is not correctness boundary | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-072` | `FE-STATE` | Freshness policy is deliberate | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-073` | `FE-STATE` | Prefetch does not create a parallel cache contract | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-074` | `FE-STATE` | Hydration data is still server-derived cache | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-075` | `FE-STATE` | Query-key isolation has direct tests | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-076` | `FE-STATE` | Generic optimistic helper tests do not prove product transformation | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-077` | `FE-STATE` | Cross-scope negative proof is mandatory for scope architecture changes | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-078` | `FE-STATE` | Realtime and REST convergence tests target final authoritative state | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-079` | `FE-STATE` | State authority change is consequential | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-080` | `FE-STATE` | Do not wrap every query API for hypothetical replacement | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-STATE-081` | `FE-STATE` | Existing cache behavior is not automatic precedent | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-TST-001` | `FE-TST` | Test the protected property at the cheapest reliable seam | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-002` | `FE-TST` | Command name is not the architecture | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-003` | `FE-TST` | Every governed Vitest file belongs to one explicit suite class | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-004` | `FE-TST` | Test taxonomy is executable governance | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-005` | `FE-TST` | Node tests do not prove browser/native integration | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-006` | `FE-TST` | Web tests prove web behavior only | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-007` | `FE-TST` | Integration test has an explicit integration boundary | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-008` | `FE-TST` | Mobile verification is a first-class suite | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-009` | `FE-TST` | Mobile category guard evolves with mobile architecture | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-010` | `FE-TST` | Governance tooling is critical code | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-011` | `FE-TST` | Generator tests cover failure paths, not only happy output | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-012` | `FE-TST` | Zero discovered tests is failure for critical guarded suites | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-013` | `FE-TST` | Test-count guard is necessary but not sufficient | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-014` | `FE-TST` | Machine-detectable architecture rules are executable | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-015` | `FE-TST` | Architecture policy defects fail the gate | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-016` | `FE-TST` | Generated architecture evidence must match its producer exactly | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-017` | `FE-TST` | Producer/generated contract drift is a quality failure | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-018` | `FE-TST` | Generated output is reviewed, not auto-blessed | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-019` | `FE-TST` | Typecheck is not runtime proof | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-020` | `FE-TST` | Lint coverage must include intended source | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-021` | `FE-TST` | Formatting is hygiene, not semantic correctness | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-022` | `FE-TST` | Accessibility is a required UI quality property | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-023` | `FE-TST` | Automated a11y plus reasoned interaction review | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-024` | `FE-TST` | Snapshot changes require intent review | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-025` | `FE-TST` | UI freeze means protected contract stability, not permanent visuals | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-026` | `FE-TST` | Web build proves packaging for the exact source revision | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-027` | `FE-TST` | Production E2E executes the exact CI web build artifact | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-028` | `FE-TST` | Marketing has independent packaging evidence | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-029` | `FE-TST` | Mobile build is separate from mobile unit tests | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-030` | `FE-TST` | Evidence names match executed behavior | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-031` | `FE-TST` | E2E is cross-boundary user-flow evidence | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-032` | `FE-TST` | E2E environment declaration is part of evidence scope | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-033` | `FE-TST` | One browser project does not prove every browser | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-034` | `FE-TST` | Retry does not normalize flaky behavior | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-035` | `FE-TST` | Failure artifacts support diagnosis, not pass/fail substitution | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-036` | `FE-TST` | Final frontend gate is an AND gate | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-037` | `FE-TST` | Static/generated architecture quality runs before broader suites | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-038` | `FE-TST` | Core job remains multi-seam evidence | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-039` | `FE-TST` | Mobile required categories cannot silently disappear | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-040` | `FE-TST` | Generator tooling failure blocks frontend certification | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-041` | `FE-TST` | UI foundation evidence is independent from web component tests | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-042` | `FE-TST` | Host builds are independent gates | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-043` | `FE-TST` | E2E dependency graph and final certification graph are distinct | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-044` | `FE-TST` | Producer contract changes are frontend-relevant CI changes | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-045` | `FE-TST` | Cancelled superseded PR run is not certification | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-046` | `FE-TST` | validate:fast is fast feedback, not full frontend CI | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-047` | `FE-TST` | validate still does not automatically equal current full CI | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-048` | `FE-TST` | Focused green is intermediate evidence | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-049` | `FE-TST` | Validation obligations accumulate | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-050` | `FE-TST` | Boundary changes require negative tests | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-051` | `FE-TST` | Bug fix adds proof against recurrence where feasible | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-052` | `FE-TST` | Repeated architecture defect becomes executable rule when detectable | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-053` | `FE-TST` | Flaky required tests are quality debt | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-054` | `FE-TST` | Deterministic time/identity seams are preferred | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-055` | `FE-TST` | Test double scope is disclosed in evidence | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-056` | `FE-TST` | Production-graph claim names substitutions | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-057` | `FE-TST` | Tests clean up global/runtime state | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-058` | `FE-TST` | Snapshot is not assertion outsourcing | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-059` | `FE-TST` | Fixture convenience cannot create impossible production states silently | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-060` | `FE-TST` | High-risk UI states are represented in verification fixtures | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-061` | `FE-TST` | Removing a critical test requires replacement/retirement rationale | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-062` | `FE-TST` | Required gate removal is architecture/governance change | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-063` | `FE-TST` | CI optimization preserves property observability | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-064` | `FE-TST` | Timeout is not a pass with infrastructure excuse | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-065` | `FE-TST` | Local failure/success scope is explicit | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-066` | `FE-TST` | Green evidence is SHA-specific | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-067` | `FE-TST` | Branch label is not architecture evidence | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-068` | `FE-TST` | Freeze artifact is not current architecture authority | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-069` | `FE-TST` | Evidence claim cannot exceed execution | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-070` | `FE-TST` | Unrun required proof remains unresolved | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-071` | `FE-TST` | Security-sensitive changes prove reject/failure paths | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-072` | `FE-TST` | Tenant isolation is not proven by two successful loads | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-073` | `FE-TST` | Generated inventory is checked by equality, not stale constants | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-074` | `FE-TST` | CI trigger coverage is part of gate correctness | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-075` | `FE-TST` | Shared setup changes affect all frontend evidence | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-076` | `FE-TST` | CI must not silently mutate dependency resolution | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-077` | `FE-TST` | Diagnostic artifacts do not become authority | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-TST-078` | `FE-TST` | Test-foundation changes follow architecture-change policy | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-UI-001` | `FE-UI` | Design semantics are centralized before component styling | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-002` | `FE-UI` | Tokens are framework-neutral | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-003` | `FE-UI` | Primitive and semantic token roles are distinct | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-004` | `FE-UI` | Components do not default to primitive color literals for semantic meaning | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-005` | `FE-UI` | Brand palette has one token authority | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-006` | `FE-UI` | Multiple color representations must map through one semantic theme contract | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-007` | `FE-UI` | Product status maps to semantic presentation explicitly | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-008` | `FE-UI` | Surface role follows elevation/context | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-009` | `FE-UI` | Appearance mode is one axis | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-010` | `FE-UI` | Accent theme modifies semantic accent roles, not arbitrary component palettes | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-011` | `FE-UI` | Theme persistence is runtime-owned, theme semantics are UI-owned | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-012` | `FE-UI` | System appearance follows OS changes while system mode is active | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-013` | `FE-UI` | Initial theme is applied before visually stable paint where feasible | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-014` | `FE-UI` | Theme transition is coherent and bounded | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-015` | `FE-UI` | Motion uses semantic duration/easing tokens | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-016` | `FE-UI` | Motion supports task comprehension | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-017` | `FE-UI` | Reduced motion preserves functionality | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-018` | `FE-UI` | Spacing tokens express repeated layout rhythm | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-019` | `FE-UI` | Dense product surfaces use explicit density semantics | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-020` | `FE-UI` | Density completeness requires component-level evidence | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-021` | `FE-UI` | Typography hierarchy is semantic | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-022` | `FE-UI` | Radius communicates one system language | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-023` | `FE-UI` | Elevation is semantic, not copied shadow literals | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-024` | `FE-UI` | Interactive controls have visible keyboard focus | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-025` | `FE-UI` | ui-web owns reusable web primitives, not product screens | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-026` | `FE-UI` | Third-party primitive does not become architecture authority | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-027` | `FE-UI` | Vendored/generated UI does not justify global quality-rule weakening | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-028` | `FE-UI` | Mobile UI is a native implementation | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-029` | `FE-UI` | Semantic parity does not require prop-for-prop parity | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-030` | `FE-UI` | Icon alone is not an accessible label by default | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-031` | `FE-UI` | Component location follows semantics, not visual reuse alone | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-032` | `FE-UI` | Variants model meaningful visual/interaction roles | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-033` | `FE-UI` | Primary action styling derives from semantic brand/theme contract | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-034` | `FE-UI` | Gradient usage is intentional and role-based | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-035` | `FE-UI` | Marketing extension cannot fork the core design language silently | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-036` | `FE-UI` | Every reusable web component supports light and dark semantic themes | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-037` | `FE-UI` | Accent theme cannot override semantic safety roles arbitrarily | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-038` | `FE-UI` | Contrast is verified in rendered states | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-039` | `FE-UI` | Clickable div is not the default interactive primitive | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-040` | `FE-UI` | Composite/overlay components define focus entry, trap/containment where appropriate, and return | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-041` | `FE-UI` | Form error is programmatically associated with its control | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-042` | `FE-UI` | Disabled and read-only states are not interchangeable | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-043` | `FE-UI` | Visual density does not reduce interaction target below accessibility requirements | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-044` | `FE-UI` | Responsive behavior is component/product semantics, not only global CSS breakpoints | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-045` | `FE-UI` | Responsive web does not replace native mobile architecture | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-046` | `FE-UI` | Loading state preserves layout and interaction expectations | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-047` | `FE-UI` | Visual similarity does not collapse semantic state distinctions | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-048` | `FE-UI` | Toast does not replace persistent actionable state | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-049` | `FE-UI` | Overlay accessibility is part of primitive contract | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-050` | `FE-UI` | Data-table primitive separates generic mechanics from product columns/data semantics | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-051` | `FE-UI` | Work Management views share design language but remain product-owned | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-052` | `FE-UI` | Specialized editor interaction may be product-owned | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-053` | `FE-UI` | Storybook is a design-system verification surface | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-054` | `FE-UI` | Story matrix follows risk, not combinatorial completeness | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-055` | `FE-UI` | Automated accessibility checks are required but not sufficient | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-056` | `FE-UI` | Visual snapshots protect intentional contracts | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-057` | `FE-UI` | Snapshot threshold does not define acceptable UX regression | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-058` | `FE-UI` | UI freeze protects contract, not visual stagnation | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-059` | `FE-UI` | Verification location follows component owner | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-060` | `FE-UI` | Theme tests target semantic boundaries | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-061` | `FE-UI` | Token existence does not prove token consumption | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-062` | `FE-UI` | Web components consume semantic CSS variables/classes instead of theme branching when practical | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-063` | `FE-UI` | Cross-platform tokens expose semantic data, not CSS-only assumptions | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-064` | `FE-UI` | Declared token export must exist and be verified | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-065` | `FE-UI` | Theme API separates state, persistence and application | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-066` | `FE-UI` | Missing required UI provider is not silently accepted unless fallback is intentional | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-067` | `FE-UI` | Preference persistence failure degrades safely | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-068` | `FE-UI` | Theme preview metadata is not styling authority | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-069` | `FE-UI` | Product copy belongs to product/feature owner | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-070` | `FE-UI` | Component contract avoids fixed-width assumptions tied to one language | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-071` | `FE-UI` | Color alone does not communicate destructive consequence | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-072` | `FE-UI` | Disabled UX preserves discoverability where appropriate | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-073` | `FE-UI` | Skeleton is presentation placeholder, not fake product data | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-074` | `FE-UI` | Data visualization does not hide the underlying meaning | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-075` | `FE-UI` | Library default is not automatically Notrelix quality | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-076` | `FE-UI` | Foundational UI change updates consumers and verification atomically | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-077` | `FE-UI` | One semantic role has one active token authority per migration phase | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-078` | `FE-UI` | Existing hard-coded visual value is not precedent | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-079` | `FE-UI` | Marketing expressiveness and product clarity have different motion/density budgets | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-UI-080` | `FE-UI` | Performance optimization preserves accessible semantics | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `GOV-001` | `GOV` | Backend authorization is final | [`docs/product/governance.md`](../product/governance.md) |
| `GOV-002` | `GOV` | Authorization happens before protected data leaks | [`docs/product/governance.md`](../product/governance.md) |
| `GOV-003` | `GOV` | Commercial entitlement does not directly grant resource permission | [`docs/product/governance.md`](../product/governance.md) |
| `GOV-004` | `GOV` | Protected resource identity is logical and stable | [`docs/product/governance.md`](../product/governance.md) |
| `GOV-005` | `GOV` | Authorization action reflects business operation | [`docs/product/governance.md`](../product/governance.md) |
| `GOV-006` | `GOV` | One ACL entry is not the whole authorization system | [`docs/product/governance.md`](../product/governance.md) |
| `GOV-007` | `GOV` | Scope inheritance is explicit | [`docs/product/governance.md`](../product/governance.md) |
| `GOV-008` | `GOV` | Hidden UI field is not a security boundary | [`docs/product/governance.md`](../product/governance.md) |
| `GOV-009` | `GOV` | Guest access is resource-limited by default | [`docs/product/governance.md`](../product/governance.md) |
| `GOV-010` | `GOV` | Share link is a scoped capability, not global public mode | [`docs/product/governance.md`](../product/governance.md) |
| `GOV-011` | `GOV` | Share-link revocation is authoritative | [`docs/product/governance.md`](../product/governance.md) |
| `GOV-012` | `GOV` | Role is a permission composition, not an identity | [`docs/product/governance.md`](../product/governance.md) |
| `GOV-013` | `GOV` | Admin does not silently become Owner | [`docs/product/governance.md`](../product/governance.md) |
| `GOV-014` | `GOV` | Permission template is creation/configuration input, not hidden live authority | [`docs/product/governance.md`](../product/governance.md) |
| `GOV-015` | `GOV` | Authorization fails closed | [`docs/product/governance.md`](../product/governance.md) |
| `GOV-016` | `GOV` | Governance never mutates protected aggregate as authorization side effect | [`docs/product/governance.md`](../product/governance.md) |
| `GOV-017` | `GOV` | Permission-sensitive cache is scope/version aware | [`docs/product/governance.md`](../product/governance.md) |
| `GOV-018` | `GOV` | Realtime access can be revoked while connected | [`docs/product/governance.md`](../product/governance.md) |
| `GOV-019` | `GOV` | Collection authorization is not fetch-all-then-hide | [`docs/product/governance.md`](../product/governance.md) |
| `GOV-020` | `GOV` | Audit evidence is not mutable Activity | [`docs/product/governance.md`](../product/governance.md) |
| `GOV-021` | `GOV` | Permission change is auditable when policy requires it | [`docs/product/governance.md`](../product/governance.md) |
| `GOV-022` | `GOV` | Every new protected resource has explicit authorization vocabulary | [`docs/product/governance.md`](../product/governance.md) |
| `GOV-023` | `GOV` | Frontend capability state is derived | [`docs/product/governance.md`](../product/governance.md) |
| `GOV-024` | `GOV` | Authorization is compositional but deterministic | [`docs/product/governance.md`](../product/governance.md) |
| `ID-001` | `ID` | Authentication and authorization are separate | [`docs/product/identity.md`](../product/identity.md) |
| `ID-002` | `ID` | Mutable email is not universal identity | [`docs/product/identity.md`](../product/identity.md) |
| `ID-003` | `ID` | Identity deletion does not cascade business history | [`docs/product/identity.md`](../product/identity.md) |
| `ID-004` | `ID` | Secret material never becomes ordinary Domain/client data | [`docs/product/identity.md`](../product/identity.md) |
| `ID-005` | `ID` | Session lifecycle is explicit | [`docs/product/identity.md`](../product/identity.md) |
| `ID-006` | `ID` | Security events invalidate the intended authorities | [`docs/product/identity.md`](../product/identity.md) |
| `ID-007` | `ID` | MFA lifecycle is security lifecycle, not Workspace policy | [`docs/product/identity.md`](../product/identity.md) |
| `ID-008` | `ID` | Provider subject identity outranks email-only linking | [`docs/product/identity.md`](../product/identity.md) |
| `ID-009` | `ID` | Account IdP configuration and Identity authentication remain separate | [`docs/product/identity.md`](../product/identity.md) |
| `ID-010` | `ID` | API token is a scoped identity credential | [`docs/product/identity.md`](../product/identity.md) |
| `ID-011` | `ID` | Security configuration is split by semantic owner | [`docs/product/identity.md`](../product/identity.md) |
| `ID-012` | `ID` | Authentication success is not product-access success | [`docs/product/identity.md`](../product/identity.md) |
| `ID-013` | `ID` | Acting principal must be explicit | [`docs/product/identity.md`](../product/identity.md) |
| `ID-014` | `ID` | Security events never expose reusable secrets | [`docs/product/identity.md`](../product/identity.md) |
| `ID-015` | `ID` | Security-sensitive stale writes fail safely | [`docs/product/identity.md`](../product/identity.md) |
| `ID-016` | `ID` | Identity retention is separate from business-resource retention | [`docs/product/identity.md`](../product/identity.md) |
| `ID-017` | `ID` | Current Account/Workspace is session/UI context, not principal identity | [`docs/product/identity.md`](../product/identity.md) |
| `ID-018` | `ID` | Identity merge is explicit, not heuristic | [`docs/product/identity.md`](../product/identity.md) |
| `INFRA-CTR-001` | `INFRA-CTR` | Release container is reproducible from repository evidence | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-002` | `INFRA-CTR` | Build inputs are explicit | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-003` | `INFRA-CTR` | Container toolchain and repository toolchain stay compatible | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-004` | `INFRA-CTR` | Release dependency restore is locked | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-005` | `INFRA-CTR` | Container build does not become hidden code generator authority | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-006` | `INFRA-CTR` | Build context contains only required source/build inputs | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-007` | `INFRA-CTR` | Ignore policy follows the build-context root | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-008` | `INFRA-CTR` | Secret-bearing local files never enter a remote/root build context unnecessarily | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-009` | `INFRA-CTR` | Git exclusion is not container-context exclusion | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-010` | `INFRA-CTR` | Build secret does not persist in layer, cache, history, or log | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-011` | `INFRA-CTR` | Client build arguments are reviewed as public data | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-012` | `INFRA-CTR` | Runtime image contains runtime needs only | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-013` | `INFRA-CTR` | Backend runtime executes published output, not source checkout | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-014` | `INFRA-CTR` | Restore-layer optimization cannot omit dependency graph inputs | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-015` | `INFRA-CTR` | Cache hit is not proof of fresh generated/dependency state | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-016` | `INFRA-CTR` | Runtime diagnostic utility has an explicit purpose | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-017` | `INFRA-CTR` | Static web image contains built assets, not development workspace | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-018` | `INFRA-CTR` | Marketing runtime carries only standalone/runtime assets | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-019` | `INFRA-CTR` | Build context is narrowed when practical | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-020` | `INFRA-CTR` | Base-image updates are dependency/security changes | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-021` | `INFRA-CTR` | Mutable broad tag is not sole release provenance | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-022` | `INFRA-CTR` | Image identity includes source/build provenance | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-023` | `INFRA-CTR` | Root privilege is minimized across image + deployment | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-024` | `INFRA-CTR` | Artifact and orchestrator hardening are both part of effective container security | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-025` | `INFRA-CTR` | Writable container state is disposable unless declared durable mount | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-026` | `INFRA-CTR` | Volume purpose is explicit | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-027` | `INFRA-CTR` | Dev volume deletion is not modeled as production recovery | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-028` | `INFRA-CTR` | Source bind mount is development-only by default | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-029` | `INFRA-CTR` | Local Compose reproduces protocols, not production scale | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-030` | `INFRA-CTR` | Local database uses the production database class | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-031` | `INFRA-CTR` | Container init scripts do not replace application migrations | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-032` | `INFRA-CTR` | Redis local durability does not make cache product truth | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-033` | `INFRA-CTR` | Messaging profile is opt-in for work that needs broker protocol | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-034` | `INFRA-CTR` | Optional tool/profile does not become application dependency accidentally | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-035` | `INFRA-CTR` | Admin tooling is local/restricted by default | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-036` | `INFRA-CTR` | Local network boundaries model connectivity intent | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-037` | `INFRA-CTR` | Published dev port is not production exposure precedent | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-038` | `INFRA-CTR` | Local gateway is composition convenience, not security bypass | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-039` | `INFRA-CTR` | Container health check is bounded and side-effect free | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-040` | `INFRA-CTR` | Health command cannot depend on a tool omitted from final image | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-041` | `INFRA-CTR` | Startup ordering is not runtime resilience | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-042` | `INFRA-CTR` | Development SDK container is not release artifact | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-043` | `INFRA-CTR` | Dev install convenience is not release dependency proof | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-044` | `INFRA-CTR` | Shared developer dependency volume is disposable cache | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-045` | `INFRA-CTR` | Local reset reconstructs from declared sources | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-046` | `INFRA-CTR` | Destructive Compose helpers are environment-scoped | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-047` | `INFRA-CTR` | Successful image build is not startup proof | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-048` | `INFRA-CTR` | Image build cannot compensate for skipped tests | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-049` | `INFRA-CTR` | Packaging workflow names match actual evidence | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-050` | `INFRA-CTR` | Startup smoke uses final image stage | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-051` | `INFRA-CTR` | Composition smoke proves changed connectivity, not every product feature | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-052` | `INFRA-CTR` | Final image contains no known local secret/config artifact | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-053` | `INFRA-CTR` | Minimal means sufficient, not arbitrarily smallest | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-054` | `INFRA-CTR` | Container/base-image vulnerability is part of release security | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-055` | `INFRA-CTR` | Build performs declared dependency acquisition only | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-056` | `INFRA-CTR` | Material build argument is versioned/evidenced | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-057` | `INFRA-CTR` | Container and non-container builds expose coherent release identity | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-058` | `INFRA-CTR` | Container rebuild does not overwrite incompatible immutable asset identity | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-059` | `INFRA-CTR` | Base/overlay merge is inspected as resolved config | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-060` | `INFRA-CTR` | Container docs do not normalize build-on-host deployment as release architecture | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-061` | `INFRA-CTR` | Managed/local substitution preserves required protocol semantics | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-062` | `INFRA-CTR` | Version skew is deliberate | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-063` | `INFRA-CTR` | Local log driver is not production observability authority | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-064` | `INFRA-CTR` | Container name is not logical operation/service identity | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-065` | `INFRA-CTR` | Local network trust does not weaken security code paths | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-066` | `INFRA-CTR` | Non-root runtime can access only intended paths | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-067` | `INFRA-CTR` | Container shutdown preserves delivery correctness | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-068` | `INFRA-CTR` | Crash loop is diagnosable, not hidden by restart | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-069` | `INFRA-CTR` | New developer environment does not create another dependency authority | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-CTR-070` | `INFRA-CTR` | Current source debt is not rewritten as canonical rule | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-ENV-001` | `INFRA-ENV` | Environment does not redefine product semantics | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-002` | `INFRA-ENV` | Runtime difference is explicit configuration or deployment state | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-003` | `INFRA-ENV` | Environment name is not feature architecture | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-004` | `INFRA-ENV` | Local defaults are unmistakably non-production | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-005` | `INFRA-ENV` | CI does not depend on personal environment state | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-006` | `INFRA-ENV` | Staging parity follows protected property | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-007` | `INFRA-ENV` | Production state is isolated | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-008` | `INFRA-ENV` | Testing does not use production tenant as ordinary sandbox | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-009` | `INFRA-ENV` | Production-derived data remains sensitive after copying | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-010` | `INFRA-ENV` | Configuration is typed and validated | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-011` | `INFRA-ENV` | Critical config never falls back permissively | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-012` | `INFRA-ENV` | Development convenience default does not become production default accidentally | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-013` | `INFRA-ENV` | Config delivery mechanism is replaceable | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-014` | `INFRA-ENV` | Config rename is compatibility work | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-015` | `INFRA-ENV` | Effective config is inspectable without exposing secrets | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-016` | `INFRA-ENV` | Secret is delivered through an approved secret channel | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-017` | `INFRA-ENV` | Secret lifecycle is independent of source deployment | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-018` | `INFRA-ENV` | Runtime identity has minimum required privilege | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-019` | `INFRA-ENV` | Migration privilege is not assumed as runtime privilege | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-020` | `INFRA-ENV` | Provider environment boundary is explicit | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-021` | `INFRA-ENV` | Public frontend configuration contains no server secret | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-022` | `INFRA-ENV` | Config timing is explicit | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-023` | `INFRA-ENV` | Environment config drift is detectable | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-024` | `INFRA-ENV` | Production-only undocumented config is debt, not architecture | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-025` | `INFRA-ENV` | Environment config and feature flag are distinct | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-026` | `INFRA-ENV` | Flag state is explicit during release | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-027` | `INFRA-ENV` | Callback URLs cannot cross environments accidentally | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-028` | `INFRA-ENV` | TLS/environment routing does not change application authorization | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-029` | `INFRA-ENV` | Direct data-service exposure is environment-specific and deliberate | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-030` | `INFRA-ENV` | Production seed behavior is explicit | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-031` | `INFRA-ENV` | Migration execution strategy is environment-aware but semantically identical | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-032` | `INFRA-ENV` | Parity follows semantics, not visual similarity | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-033` | `INFRA-ENV` | Dependency substitution has declared fidelity | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-034` | `INFRA-ENV` | Environment promotion preserves artifact identity | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-035` | `INFRA-ENV` | Local developer access is not production runtime privilege | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-036` | `INFRA-ENV` | Config diagnostics are redacted structurally | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-037` | `INFRA-ENV` | Optional provider misconfiguration is isolated when architecture permits | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-038` | `INFRA-ENV` | “Container started” is not environment readiness | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-039` | `INFRA-ENV` | Executable environment config is evidence; canonical policy decides intent | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-ENV-040` | `INFRA-ENV` | Infrastructure provider/orchestrator may change without product rewrite | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `INFRA-RUN-001` | `INFRA-RUN` | Deployment packaging does not redefine bounded contexts | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-002` | `INFRA-RUN` | Shared process does not imply shared business model | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-003` | `INFRA-RUN` | Extraction preserves semantic owner | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-004` | `INFRA-RUN` | Process role has explicit responsibility | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-005` | `INFRA-RUN` | Worker is not globally trusted administrator | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-006` | `INFRA-RUN` | Multiple scheduler instances cannot create uncontrolled duplicate logical occurrences | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-007` | `INFRA-RUN` | Migration role is separable from steady-state runtime | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-008` | `INFRA-RUN` | Frontend runtime is untrusted client/public-delivery surface | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-009` | `INFRA-RUN` | Network trust does not replace resource authorization | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-010` | `INFRA-RUN` | Runtime artifact is immutable for one release identity | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-011` | `INFRA-RUN` | Mutable latest is not the only production identity | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-012` | `INFRA-RUN` | Container/process filesystem is not authoritative product storage | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-013` | `INFRA-RUN` | PostgreSQL runtime preserves Application transaction and tenant contracts | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-014` | `INFRA-RUN` | Connection capacity is bounded and observable | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-015` | `INFRA-RUN` | Public database exposure is not production convenience | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-016` | `INFRA-RUN` | Cache cannot become authorization or product truth | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-017` | `INFRA-RUN` | Broker semantics must satisfy Platform delivery contract | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-018` | `INFRA-RUN` | Similar API does not prove dependency equivalence | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-019` | `INFRA-RUN` | Broker outage must not erase committed outbox work | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-020` | `INFRA-RUN` | Object storage identity is referenced, not embedded as business data | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-021` | `INFRA-RUN` | Storage URL is not permanent permission | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-022` | `INFRA-RUN` | Provider call has bounded runtime policy | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-023` | `INFRA-RUN` | Provider admin credential is not default application credential | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-024` | `INFRA-RUN` | Network path follows least connectivity | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-025` | `INFRA-RUN` | Service discovery is replaceable | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-026` | `INFRA-RUN` | Admin tooling is not production public surface by default | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-027` | `INFRA-RUN` | Runtime permissions follow process responsibility | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-028` | `INFRA-RUN` | Writable filesystem is intentional | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-029` | `INFRA-RUN` | Privilege hardening cannot break required runtime semantics silently | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-030` | `INFRA-RUN` | Readiness means safe to receive intended workload | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-031` | `INFRA-RUN` | Liveness avoids dependency restart loops | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-032` | `INFRA-RUN` | Scale-out preserves idempotency and coordination semantics | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-033` | `INFRA-RUN` | Process-local cache/session is not sole durable authority | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-034` | `INFRA-RUN` | More workers are not automatically more throughput | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-035` | `INFRA-RUN` | Resource limits are operational configuration, not product architecture | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-036` | `INFRA-RUN` | Long data backfill is not ordinary process startup | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-037` | `INFRA-RUN` | Runtime restart cannot reset production data | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-038` | `INFRA-RUN` | Migration is coordinated with rolling runtime | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-039` | `INFRA-RUN` | Production promotion preserves exact evidence identity | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-040` | `INFRA-RUN` | Build-on-target is not normalized as canonical release strategy | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-041` | `INFRA-RUN` | Shared dependency stays compatible through overlap window | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-042` | `INFRA-RUN` | Gateway cannot bypass API security semantics | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-043` | `INFRA-RUN` | Static asset deployment accounts for immutable/cacheable assets | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-044` | `INFRA-RUN` | Local log rotation is not production observability architecture | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-045` | `INFRA-RUN` | Telemetry path does not block critical request indefinitely | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-046` | `INFRA-RUN` | Backup mechanism does not replace recovery runbook | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-047` | `INFRA-RUN` | New dependency has declared authority class | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-048` | `INFRA-RUN` | Provider replacement evaluates semantics, not product logo/API similarity | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-049` | `INFRA-RUN` | Development topology is not production topology authority | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-050` | `INFRA-RUN` | Exact cloud/provider resources come from executable infrastructure | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-051` | `INFRA-RUN` | Runtime drift is detectable and reconciled | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-RUN-052` | `INFRA-RUN` | Runtime inventory is machine-evidenced where possible | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INT-001` | `INT` | Provider models are anti-corruption boundaries | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-002` | `INT` | Connection lifecycle is explicit | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-003` | `INT` | Connection scope is explicit | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-004` | `INT` | Provider consent does not bypass Governance | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-005` | `INT` | Provider secrets are isolated | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-006` | `INT` | Secret validity and Connection product state are related but distinct | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-007` | `INT` | Webhook is authenticated before business processing | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-008` | `INT` | Webhook tenant routing never trusts provider JSON alone | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-009` | `INT` | Inbound provider processing is idempotent | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-010` | `INT` | Provider delivery identity and product fact identity are distinct | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-011` | `INT` | Sync has explicit direction and authority | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-012` | `INT` | External provider never silently becomes owner of Notrelix Domain state | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-013` | `INT` | Sync cursor is progress state, not business truth | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-014` | `INT` | Sync progress advances after successful durable processing | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-015` | `INT` | Connection state and Sync health are not one status | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-016` | `INT` | Mapping preserves both identities | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-017` | `INT` | Disconnect does not automatically delete business resources | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-018` | `INT` | Provider deletion does not automatically equal Notrelix deletion | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-019` | `INT` | Outbound operations have stable logical identity | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-020` | `INT` | Unknown provider outcome is reconciled, not blindly retried | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-021` | `INT` | Provider rate limiting is a bounded operational state | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-022` | `INT` | Sync conflict policy is explicit | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-023` | `INT` | Calendar integration is provider mapping, not Work Management calendar truth | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-024` | `INT` | Calendar date/time semantics preserve product meaning | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-025` | `INT` | Provider user identity does not create Notrelix membership automatically | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-026` | `INT` | Provider payload is untrusted business input | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-027` | `INT` | Automation retries do not replace Integration idempotency | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-028` | `INT` | Integrations never writes Work Management persistence directly | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-029` | `INT` | Background Integration principal is bounded | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-030` | `INT` | Billing entitlement does not own Connection identity | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-031` | `INT` | Integration public events expose translated product meaning | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-032` | `INT` | Integration progress realtime is recoverable | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-033` | `INT` | Disconnect is a product lifecycle operation, not secret deletion only | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-034` | `INT` | Provider-held copies are separate deletion responsibility | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-035` | `INT` | Provider permission scopes are least-privilege | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-036` | `INT` | Provider capability limitation stays in Integration boundary | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-037` | `INT` | Generic integration table cannot be schema-less JSON dumping ground | [`docs/product/integrations.md`](../product/integrations.md) |
| `INT-038` | `INT` | Provider isolation can justify service extraction without changing product context | [`docs/product/integrations.md`](../product/integrations.md) |
| `NRX-001` | `NRX` | Product Semantics Outrank Representation and Implementation Convenience | [`RULE.md`](../../RULE.md) |
| `NRX-002` | `NRX` | Architecture Boundaries Are Executable Contracts | [`RULE.md`](../../RULE.md) |
| `NRX-003` | `NRX` | Tenant Isolation Is Correctness and Security | [`RULE.md`](../../RULE.md) |
| `NRX-004` | `NRX` | Backend Authorization Is Authoritative | [`RULE.md`](../../RULE.md) |
| `NRX-005` | `NRX` | Pure Business and Foundation Layers Stay Deterministic and Provider-Free | [`RULE.md`](../../RULE.md) |
| `NRX-006` | `NRX` | Shared/Common Abstractions Require Stable Ownership | [`RULE.md`](../../RULE.md) |
| `NRX-007` | `NRX` | Cross-Boundary Contracts Are Explicit, Owned, and Versionable | [`RULE.md`](../../RULE.md) |
| `NRX-008` | `NRX` | Breaking Public or Persisted Changes Are Migrations | [`RULE.md`](../../RULE.md) |
| `NRX-009` | `NRX` | Consistency and Transaction Ownership Are Explicit | [`RULE.md`](../../RULE.md) |
| `NRX-010` | `NRX` | Retryable Effects Require Stable Identity and Idempotency Semantics | [`RULE.md`](../../RULE.md) |
| `NRX-011` | `NRX` | Lifecycle and Destructive Data Operations Require Explicit Product Policy | [`RULE.md`](../../RULE.md) |
| `NRX-012` | `NRX` | Secrets and Sensitive Data Are Protected by Default | [`RULE.md`](../../RULE.md) |
| `NRX-013` | `NRX` | Generated Artifacts Are Producer-Owned and Drift-Checked | [`RULE.md`](../../RULE.md) |
| `NRX-014` | `NRX` | Client State Cannot Become Competing Server Truth | [`RULE.md`](../../RULE.md) |
| `NRX-015` | `NRX` | Accessibility and Host Safety Are Release-Quality Contracts | [`RULE.md`](../../RULE.md) |
| `NRX-016` | `NRX` | Required Validation Must Execute Meaningful Non-Zero Work | [`RULE.md`](../../RULE.md) |
| `NRX-017` | `NRX` | Architecture Exceptions Are Explicit, Owned, and Temporary or Reviewable | [`RULE.md`](../../RULE.md) |
| `NRX-018` | `NRX` | Documentation, Decisions, Source Evidence, and Generated Evidence Remain Coherent | [`RULE.md`](../../RULE.md) |
| `OPS-DEG-001` | `OPS-DEG` | Correctness is preserved before throughput | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-002` | `OPS-DEG` | Dependency role determines degraded mode | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-003` | `OPS-DEG` | Optional acceleration may be bypassed only through authoritative safe path | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-004` | `OPS-DEG` | Authoritative-store failure cannot degrade to invented writable truth | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-005` | `OPS-DEG` | Degraded UX states the real limitation | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-006` | `OPS-DEG` | Database degradation prioritizes correctness-critical work | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-007` | `OPS-DEG` | DB retry is bounded and transaction-aware | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-008` | `OPS-DEG` | Read-only mode is explicit across API and clients | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-009` | `OPS-DEG` | Stale cache is not an emergency source of truth by default | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-010` | `OPS-DEG` | Cache bypass is capacity-aware | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-011` | `OPS-DEG` | Cache population is not transaction success authority | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-012` | `OPS-DEG` | Permission cache failure fails safe | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-013` | `OPS-DEG` | Durable outbox allows delayed delivery, not lost delivery | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-014` | `OPS-DEG` | Broker outage has backpressure/capacity plan | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-015` | `OPS-DEG` | One failing consumer does not poison unrelated delivery globally | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-016` | `OPS-DEG` | Retry has bounded backoff and jitter where appropriate | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-017` | `OPS-DEG` | Poison handling preserves ordering semantics | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-018` | `OPS-DEG` | Realtime outage does not block authoritative writes by default | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-019` | `OPS-DEG` | Realtime reconnect reconciles authoritative state | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-020` | `OPS-DEG` | Realtime recovery does not restore revoked subscriptions blindly | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-021` | `OPS-DEG` | Object-storage failure does not fabricate successful file state | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-022` | `OPS-DEG` | Orphan/missing object is reconciled | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-023` | `OPS-DEG` | Provider failure is classified before retry | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-024` | `OPS-DEG` | Provider outage does not become Notrelix retry storm | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-025` | `OPS-DEG` | Provider dependency does not own source transaction unless product explicitly requires it | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-026` | `OPS-DEG` | Unknown external outcome is a distinct degraded state | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-027` | `OPS-DEG` | Sync degradation does not rewrite connection identity | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-028` | `OPS-DEG` | Webhook recovery assumes duplicates | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-029` | `OPS-DEG` | Authentication degradation fails closed for new authority | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-030` | `OPS-DEG` | SSO outage never creates unauthorized fallback | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-031` | `OPS-DEG` | Frontend delivery incident accounts for old loaded clients | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-032` | `OPS-DEG` | Server degradation strategy respects supported mobile clients | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-033` | `OPS-DEG` | Search failure does not change source data | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-034` | `OPS-DEG` | Analytics freshness degradation is explicit | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-035` | `OPS-DEG` | Automation pause preserves execution identity/history | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-036` | `OPS-DEG` | Payment-provider outage does not destroy existing product state | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-037` | `OPS-DEG` | Degradation cannot invent unlimited entitlement | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-038` | `OPS-DEG` | Load shedding has priority order | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-039` | `OPS-DEG` | Degraded capacity preserves noisy-neighbor controls | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-040` | `OPS-DEG` | Scale after identifying bottleneck | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-041` | `OPS-DEG` | Circuit open state maps to explicit product outcome | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-042` | `OPS-DEG` | Timeout includes outcome semantics | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-043` | `OPS-DEG` | Failure isolation follows actual failure domain | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-044` | `OPS-DEG` | Fallback does not fabricate equivalent behavior | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-045` | `OPS-DEG` | Security-sensitive state is not served stale casually | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-046` | `OPS-DEG` | Deferred write requires durable accepted-work contract | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-047` | `OPS-DEG` | Volatile buffering cannot return durable success | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-048` | `OPS-DEG` | Dependency recovery is verified before full workload reopening | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-049` | `OPS-DEG` | Recovery does not unleash backlog at maximum concurrency automatically | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-050` | `OPS-DEG` | Reconnect storm is a capacity scenario | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-051` | `OPS-DEG` | User messaging distinguishes unavailable, pending, and stale | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-052` | `OPS-DEG` | Degraded mode is observable | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-053` | `OPS-DEG` | Manual degradation cannot become forgotten production config | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-054` | `OPS-DEG` | Automatic degradation has hysteresis/recovery semantics where needed | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-055` | `OPS-DEG` | Optional subsystem failure does not take down unrelated capabilities | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-056` | `OPS-DEG` | Emergency availability shortcut cannot reduce tenant/security guarantee | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-DEG-057` | `OPS-DEG` | Availability metric does not justify false success | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `OPS-INC-001` | `OPS-INC` | Stabilize tenant/data/security safety first | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-002` | `OPS-INC` | Severity follows impact, not visibility | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-003` | `OPS-INC` | Incident coordinator coordinates, not personally diagnoses everything | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-004` | `OPS-INC` | Incident decisions are time-stamped | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-005` | `OPS-INC` | Last-known-good is evidence, not automatic rollback target | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-006` | `OPS-INC` | Do not delete evidence to make monitoring green | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-007` | `OPS-INC` | Containment is as narrow as safely possible | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-008` | `OPS-INC` | Degraded mode preserves product invariants | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-009` | `OPS-INC` | Rollout is stopped before broadening uncertain impact | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-010` | `OPS-INC` | One hypothesis-changing action at a time where practical | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-011` | `OPS-INC` | Dependency outage and product bug are distinguished | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-012` | `OPS-INC` | Do not patch generated files ad hoc during compatibility incident | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-013` | `OPS-INC` | Messaging recovery starts from logical identity | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-014` | `OPS-INC` | Poison recovery is not “delete until green” | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-015` | `OPS-INC` | Never clear dedup/idempotency state blindly | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-016` | `OPS-INC` | Timeout is not proof that provider effect failed | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-017` | `OPS-INC` | Database availability is not data recovery | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-018` | `OPS-INC` | Security containment can override availability optimization | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-019` | `OPS-INC` | Scaling is containment only when it addresses the bottleneck safely | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-020` | `OPS-INC` | Recovery method follows irreversible state | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-021` | `OPS-INC` | Unsafe rollback is not mitigation | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-022` | `OPS-INC` | Replay is controlled reconstruction, not mass resend | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-023` | `OPS-INC` | Verify the original failure mode directly | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-024` | `OPS-INC` | Incident closure requires stable recovery evidence | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-025` | `OPS-INC` | External/internal communication does not invent root cause early | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-026` | `OPS-INC` | Timeline uses factual observations and decisions | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-027` | `OPS-INC` | Corrective action targets protective property | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-028` | `OPS-INC` | Incident follow-up returns durable knowledge to canonical owners | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-029` | `OPS-INC` | Recovery path exists before high-risk launch | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-030` | `OPS-INC` | Untested recovery assumption is operational risk | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-031` | `OPS-INC` | Runbook does not authorize destructive shortcut | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-032` | `OPS-INC` | Handoff does not restart diagnosis from zero | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-033` | `OPS-INC` | False page is observability feedback | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-034` | `OPS-INC` | Shared cause does not erase separate data/security impact | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-035` | `OPS-INC` | Incident urgency does not transfer semantic ownership | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-036` | `OPS-INC` | Emergency manual fix creates follow-up source-of-truth repair | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-037` | `OPS-INC` | Incident config drift is closed after recovery | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-038` | `OPS-INC` | Contract recovery verifies error/auth/tenant semantics too | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-039` | `OPS-INC` | Incident evidence is protected | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-INC-040` | `OPS-INC` | Response metrics are learning signals, not blame targets | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-OBS-001` | `OPS-OBS` | Observability follows semantic identifiers | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-002` | `OPS-OBS` | Correlation does not become authorization | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-003` | `OPS-OBS` | Logs are structured around operations, not prose archaeology | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-004` | `OPS-OBS` | Diagnostic usefulness does not justify sensitive payload dumping | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-005` | `OPS-OBS` | Expected client rejection is not server-failure noise | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-006` | `OPS-OBS` | Metric labels have controlled cardinality | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-007` | `OPS-OBS` | Trace propagation respects trust boundaries | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-008` | `OPS-OBS` | Liveness is not product health | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-009` | `OPS-OBS` | Dependency metrics are diagnostics, not user-impact SLI replacements | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-010` | `OPS-OBS` | SLI follows user-visible capability | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-011` | `OPS-OBS` | SLI exclusion is explicit | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-012` | `OPS-OBS` | Critical latency objectives do not rely on average alone | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-013` | `OPS-OBS` | Eventual consistency has observable lag | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-014` | `OPS-OBS` | Numerical SLOs remain explicit TBD until approved | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-015` | `OPS-OBS` | Security and data-integrity incidents are not budgeted away | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-016` | `OPS-OBS` | Paging alert is actionable | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-017` | `OPS-OBS` | Raw resource threshold is not automatically a paging alert | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-018` | `OPS-OBS` | Alert ownership follows capability, not dashboard author | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-019` | `OPS-OBS` | Noisy alert is operational debt | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-020` | `OPS-OBS` | Paging alerts are verified | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-021` | `OPS-OBS` | Dashboard has a stated operational question | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-022` | `OPS-OBS` | Rollout health is attributable to the changed version/cohort | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-023` | `OPS-OBS` | Migration completion is observable | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-024` | `OPS-OBS` | Message backlog age matters more than raw count alone | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-025` | `OPS-OBS` | Retry reasons are classified | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-026` | `OPS-OBS` | Ordering stall is diagnosable without advancing cursor unsafely | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-027` | `OPS-OBS` | Realtime health includes convergence, not socket count only | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-028` | `OPS-OBS` | Provider telemetry distinguishes provider failure from Notrelix defect | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-029` | `OPS-OBS` | Automation retries do not hide repeated business failure | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-030` | `OPS-OBS` | DB availability does not prove data correctness | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-031` | `OPS-OBS` | Frontend telemetry is privacy-minimized | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-032` | `OPS-OBS` | Client version is operationally visible for compatibility incidents | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-033` | `OPS-OBS` | New durable consumer has backlog/retry/poison observability | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-034` | `OPS-OBS` | “Degradable” still has a correctness story | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-035` | `OPS-OBS` | Telemetry exporter is not product transaction authority | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-036` | `OPS-OBS` | Errors and critical security/data signals have deliberate sampling policy | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-037` | `OPS-OBS` | Telemetry retention matches diagnostic value and data sensitivity | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-038` | `OPS-OBS` | Canonical observability policy is vendor-neutral | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-039` | `OPS-OBS` | Approved SLO is a product/operations contract | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-OBS-040` | `OPS-OBS` | Synthetic check uses controlled test identity/data | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-REC-001` | `OPS-REC` | Correctness precedes availability | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-002` | `OPS-REC` | Recovery scope is explicit | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-003` | `OPS-REC` | Evidence is preserved before destructive repair | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-004` | `OPS-REC` | Backup existence is not restore proof | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-005` | `OPS-REC` | RPO/RTO are explicit approved objectives, not invented defaults | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-006` | `OPS-REC` | Recovery point is evaluated across side effects | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-007` | `OPS-REC` | PITR selection considers replay/reconciliation cost | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-008` | `OPS-REC` | Targeted repair derives values from authoritative semantics | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-009` | `OPS-REC` | Repair is not performed while known bad writer continues | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-010` | `OPS-REC` | Reachable database with wrong schema is not recovered | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-011` | `OPS-REC` | Recovery correction uses new reproducible migration/repair | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-012` | `OPS-REC` | Model drift is repaired, not suppressed | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-013` | `OPS-REC` | Recovery is tenant-safe | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-014` | `OPS-REC` | RLS is reverified after data/schema recovery | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-015` | `OPS-REC` | Row count parity is not semantic recovery proof | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-016` | `OPS-REC` | Recovery does not reset concurrency versions casually | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-017` | `OPS-REC` | Source state and outbox are reconciled together | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-018` | `OPS-REC` | Delivered facts require reconciliation or compensation | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-019` | `OPS-REC` | Dedup state is part of recovery | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-020` | `OPS-REC` | Ordering cursor is not rewound/skipped without proof | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-021` | `OPS-REC` | Replay is idempotent and bounded | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-022` | `OPS-REC` | DLQ is evidence, not garbage | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-023` | `OPS-REC` | External outcome is reconciled from provider reality | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-024` | `OPS-REC` | Unknown provider operation is reconciled before retry | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-025` | `OPS-REC` | Financial recovery preserves commercial evidence | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-026` | `OPS-REC` | Database restore and object-store restore are reconciled | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-027` | `OPS-REC` | Rebuildable search prefers rebuild from authoritative source | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-028` | `OPS-REC` | Cache is invalidated/rebuilt after relevant recovery | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-029` | `OPS-REC` | Current projection and historical snapshot are distinguished | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-030` | `OPS-REC` | Recovery triggers convergence to authoritative state | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-031` | `OPS-REC` | Recovery does not trust stale client retries | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-032` | `OPS-REC` | Restore validation does not write into live production accidentally | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-033` | `OPS-REC` | Recovery tooling is exercised periodically according to risk | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-034` | `OPS-REC` | Partial restore includes dependency graph | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-035` | `OPS-REC` | Recovery merge never uses blind last-write-wins | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-036` | `OPS-REC` | Permanent loss is explicit | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-037` | `OPS-REC` | Manual repair is reviewed production code | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-038` | `OPS-REC` | Recovery capability is not ordinary product API | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-039` | `OPS-REC` | Derived systems reopen after source correctness | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-040` | `OPS-REC` | Reopening requires write-path verification | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-041` | `OPS-REC` | Restore success does not automatically close incident | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-042` | `OPS-REC` | Vendor tooling does not define recovery semantics | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-043` | `OPS-REC` | Backup is not a lower-security copy | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-044` | `OPS-REC` | Deleted data in backups follows explicit retention policy | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-REC-045` | `OPS-REC` | Drill validates decision points, not only commands | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `PROD-001` | `PROD` | Product semantics outrank implementation convenience | [`docs/product/product-model.md`](../product/product-model.md) |
| `PROD-002` | `PROD` | One authoritative owner per business fact | [`docs/product/product-model.md`](../product/product-model.md) |
| `PROD-003` | `PROD` | Foreign references do not transfer ownership | [`docs/product/product-model.md`](../product/product-model.md) |
| `PROD-004` | `PROD` | Account, Identity, and Workspace remain distinct | [`docs/product/product-model.md`](../product/product-model.md) |
| `PROD-005` | `PROD` | Work Management is not Kanban CRUD | [`docs/product/product-model.md`](../product/product-model.md) |
| `PROD-006` | `PROD` | Views do not own duplicate work data | [`docs/product/product-model.md`](../product/product-model.md) |
| `PROD-007` | `PROD` | BoardGroup is not the universal Kanban status | [`docs/product/product-model.md`](../product/product-model.md) |
| `PROD-008` | `PROD` | Documents and Work Management are separate semantic capabilities | [`docs/product/product-model.md`](../product/product-model.md) |
| `PROD-009` | `PROD` | Collaboration attaches to resources without taking ownership | [`docs/product/product-model.md`](../product/product-model.md) |
| `PROD-010` | `PROD` | Governance is cross-cutting authority, not scattered role checks | [`docs/product/product-model.md`](../product/product-model.md) |
| `PROD-011` | `PROD` | Commercial availability and authorization are distinct | [`docs/product/product-model.md`](../product/product-model.md) |
| `PROD-012` | `PROD` | Automation reacts through approved contracts | [`docs/product/product-model.md`](../product/product-model.md) |
| `PROD-013` | `PROD` | Provider vocabulary does not become Notrelix product vocabulary automatically | [`docs/product/product-model.md`](../product/product-model.md) |
| `PROD-014` | `PROD` | Billing controls commercial capability without taking product ownership | [`docs/product/product-model.md`](../product/product-model.md) |
| `PROD-015` | `PROD` | Analytics is derived insight, not source mutation authority | [`docs/product/product-model.md`](../product/product-model.md) |
| `PROD-016` | `PROD` | Search does not become a business context merely because a package/service exists | [`docs/product/product-model.md`](../product/product-model.md) |
| `PROD-017` | `PROD` | Cross-context write is never shared aggregate mutation | [`docs/product/product-model.md`](../product/product-model.md) |
| `PROD-018` | `PROD` | Strong cross-context consistency is exceptional | [`docs/product/product-model.md`](../product/product-model.md) |
| `PROD-019` | `PROD` | Lifecycle names are context language | [`docs/product/product-model.md`](../product/product-model.md) |
| `PROD-020` | `PROD` | Deletion policy belongs to semantic owner | [`docs/product/product-model.md`](../product/product-model.md) |
| `PROD-021` | `PROD` | Failure semantics are user/product semantics | [`docs/product/product-model.md`](../product/product-model.md) |
| `PROD-022` | `PROD` | User-visible consistency matches actual consistency | [`docs/product/product-model.md`](../product/product-model.md) |
| `PROD-023` | `PROD` | Product context declares resource/action vocabulary | [`docs/product/product-model.md`](../product/product-model.md) |
| `PROD-024` | `PROD` | New screen/table/package/team is not context evidence | [`docs/product/product-model.md`](../product/product-model.md) |
| `PROD-UX-001` | `PROD-UX` | The work is the visual priority | [`docs/product/product-experience.md`](../product/product-experience.md) |
| `PROD-UX-002` | `PROD-UX` | Calm density, not low information | [`docs/product/product-experience.md`](../product/product-experience.md) |
| `PROD-UX-003` | `PROD-UX` | Coherence follows semantics | [`docs/product/product-experience.md`](../product/product-experience.md) |
| `PROD-UX-004` | `PROD-UX` | Language is plain and precise | [`docs/product/product-experience.md`](../product/product-experience.md) |
| `PROD-UX-005` | `PROD-UX` | Product and marketing have different volume | [`docs/product/product-experience.md`](../product/product-experience.md) |
| `PROD-UX-006` | `PROD-UX` | Accessibility is product quality | [`docs/product/product-experience.md`](../product/product-experience.md) |
| `PROD-UX-007` | `PROD-UX` | System state is designed explicitly | [`docs/product/product-experience.md`](../product/product-experience.md) |
| `PROD-UX-008` | `PROD-UX` | Permission state must not flash unauthorized data | [`docs/product/product-experience.md`](../product/product-experience.md) |
| `PROD-UX-009` | `PROD-UX` | Pending is not completed | [`docs/product/product-experience.md`](../product/product-experience.md) |
| `PROD-UX-010` | `PROD-UX` | User-visible consistency matches system consistency | [`docs/product/product-experience.md`](../product/product-experience.md) |
| `PROD-UX-011` | `PROD-UX` | Optimism is provisional | [`docs/product/product-experience.md`](../product/product-experience.md) |
| `PROD-UX-012` | `PROD-UX` | Scope is visible when mistakes could be costly | [`docs/product/product-experience.md`](../product/product-experience.md) |
| `PROD-UX-013` | `PROD-UX` | Feedback matches action weight | [`docs/product/product-experience.md`](../product/product-experience.md) |
| `PROD-UX-014` | `PROD-UX` | Scope and consequence precede enterprise action | [`docs/product/product-experience.md`](../product/product-experience.md) |
| `PROD-UX-015` | `PROD-UX` | Experience regressions are product regressions | [`docs/product/product-experience.md`](../product/product-experience.md) |
| `QLT-001` | `QLT` | Correctness is semantic, not syntactic | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-002` | `QLT` | Evidence owner follows protected property | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-003` | `QLT` | Quality depth follows blast radius | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-004` | `QLT` | One responsibility has one visible owner | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-005` | `QLT` | Public surface is smaller than implementation | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-006` | `QLT` | Invariants have one authoritative implementation owner | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-007` | `QLT` | Failure behavior is explicit | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-008` | `QLT` | Rejected operations do not emit success evidence | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-009` | `QLT` | Semantic no-op is observable as no mutation | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-010` | `QLT` | Architecture violations fail mechanically where practical | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-011` | `QLT` | Disabling a gate is not a fix | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-012` | `QLT` | Cross-tenant negative evidence is mandatory for risky boundaries | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-013` | `QLT` | Protected queries are tested, not only protected commands | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-014` | `QLT` | Secret handling is part of correctness | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-015` | `QLT` | Transaction success and side-effect success are not conflated | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-016` | `QLT` | Concurrency-sensitive invariants require competing-operation tests | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-017` | `QLT` | Retry proof includes duplicate delivery | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-018` | `QLT` | Ordering state advances only after successful durable processing | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-019` | `QLT` | Generated output is never hand-maintained authority | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-020` | `QLT` | Compatibility is proven at consumer-relevant boundaries | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-021` | `QLT` | A substitute database cannot prove provider-specific semantics | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-022` | `QLT` | Migration correctness includes old data, not only empty schema | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-023` | `QLT` | Client cache cannot become independent business authority | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-024` | `QLT` | Mobile safety is explicit | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-025` | `QLT` | Accessibility failure is a product regression | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-026` | `QLT` | Snapshot evidence is supplemental | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-027` | `QLT` | Performance defects can be correctness defects | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-028` | `QLT` | A critical failure path without diagnosable evidence is incomplete | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-029` | `QLT` | Documentation is part of the change when semantics change | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-030` | `QLT` | Review is not style policing | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-031` | `QLT` | Hidden permanent TODO is not governance | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-032` | `QLT` | Testability serves design, not mock count | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-033` | `QLT` | Flaky required test is a failing quality system | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-034` | `QLT` | Required suite may not pass with zero meaningful work | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-035` | `QLT` | Current CI topology is evidence, not eternal architecture | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-036` | `QLT` | Exact revision matters | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-037` | `QLT` | “Works locally” is not merge evidence | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-038` | `QLT` | Quality debt cannot become invisible baseline | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-039` | `QLT` | Gate count is not quality | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-040` | `QLT` | Irreversible operation requires stronger proof | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-041` | `QLT` | Gate output is actionable | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-042` | `QLT` | Test helpers do not conceal the property under test | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-043` | `QLT` | Test isolation is an invariant | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-044` | `QLT` | Mock fidelity must match the property being proven | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-045` | `QLT` | E2E cannot compensate for missing lower-level ownership tests | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-046` | `QLT` | Scenario completeness outranks coverage percentage | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-A11Y-001` | `QLT-A11Y` | Accessibility is a release-quality requirement | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-002` | `QLT-A11Y` | Standards baseline does not replace usability | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-003` | `QLT-A11Y` | Custom control has a complete interaction contract | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-004` | `QLT-A11Y` | Visual tooltip is not the only accessible name | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-005` | `QLT-A11Y` | Programmatic state matches visible state | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-006` | `QLT-A11Y` | Keyboard path reaches every primary product action | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-007` | `QLT-A11Y` | No accidental keyboard trap | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-008` | `QLT-A11Y` | Focus indicator remains perceptible | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-009` | `QLT-A11Y` | Focus order follows meaningful interaction order | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-010` | `QLT-A11Y` | Focus is not hidden by authored overlays | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-011` | `QLT-A11Y` | Overlay lifecycle manages focus explicitly | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-012` | `QLT-A11Y` | Visual hierarchy has semantic structure | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-013` | `QLT-A11Y` | Same function is identified consistently | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-014` | `QLT-A11Y` | State has non-color cue | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-015` | `QLT-A11Y` | Design tokens do not excuse contrast failure | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-016` | `QLT-A11Y` | Zoom does not remove primary actions | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-017` | `QLT-A11Y` | Dense work surface remains operable, not merely visible | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-018` | `QLT-A11Y` | Tiny icon targets are not default interaction design | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-019` | `QLT-A11Y` | Dragging has an accessible alternative | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-020` | `QLT-A11Y` | Reduced motion preserves information and operation | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-021` | `QLT-A11Y` | Dynamic update is announced only when useful | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-022` | `QLT-A11Y` | Form error is programmatically associated with its input | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-023` | `QLT-A11Y` | Required/invalid semantics are exposed programmatically | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-024` | `QLT-A11Y` | High-impact action has accessible review/confirmation | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-025` | `QLT-A11Y` | Authentication supports password managers and paste | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-026` | `QLT-A11Y` | Session expiry does not cause silent data loss without warning where avoidable | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-027` | `QLT-A11Y` | Toast is not sole container for critical information | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-028` | `QLT-A11Y` | Chart meaning is not encoded only in color or hover | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-029` | `QLT-A11Y` | Virtualization cannot make focused content disappear unpredictably | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-030` | `QLT-A11Y` | Complex composite follows one consistent keyboard model | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-031` | `QLT-A11Y` | Inline edit failure preserves user context | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-032` | `QLT-A11Y` | Rich editor accessibility is a feature contract | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-033` | `QLT-A11Y` | Mobile parity is semantic, not DOM parity | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-034` | `QLT-A11Y` | Marketing accessibility has the same release-quality status | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-035` | `QLT-A11Y` | Localization cannot remove semantic labels | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-036` | `QLT-A11Y` | Permission transitions preserve focus/context | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-037` | `QLT-A11Y` | Disabled state remains understandable | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-038` | `QLT-A11Y` | Primitive accessibility is tested centrally | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-039` | `QLT-A11Y` | Automated scan does not replace keyboard review | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-040` | `QLT-A11Y` | Screen-reader evidence targets critical semantics, not exhaustive browser multiplication | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-041` | `QLT-A11Y` | Screenshot pass does not certify accessibility | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-042` | `QLT-A11Y` | Evidence matches the interaction property | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-043` | `QLT-A11Y` | Blocked critical workflow is release-blocking by default | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-044` | `QLT-A11Y` | Primitive regression has system-wide impact | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-A11Y-045` | `QLT-A11Y` | Upstream component pedigree is not accessibility evidence | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-PERF-001` | `QLT-PERF` | Tenant-scale work is bounded | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-002` | `QLT-PERF` | Complexity is reviewed against expected cardinality | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-003` | `QLT-PERF` | Optimize only after semantic ownership is correct | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-004` | `QLT-PERF` | Read optimization never creates a second mutation authority | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-005` | `QLT-PERF` | Query shape is reviewed with data shape | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-006` | `QLT-PERF` | Round trips and result amplification are both bounded | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-007` | `QLT-PERF` | Pagination is part of the query contract | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-008` | `QLT-PERF` | Common filter/sort paths are indexable or intentionally projected | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-009` | `QLT-PERF` | Dynamic values do not require full JSON scans for recurring hot queries | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-010` | `QLT-PERF` | Index cost is part of the change | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-011` | `QLT-PERF` | External I/O does not extend source DB transaction unnecessarily | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-012` | `QLT-PERF` | Contention is measured at the real coordination key | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-013` | `QLT-PERF` | Resource usage is bounded per request/job | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-014` | `QLT-PERF` | Payload size is a first-class performance property | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-015` | `QLT-PERF` | List contract is intentionally narrower than detail contract | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-016` | `QLT-PERF` | Cache has measurable reason to exist | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-017` | `QLT-PERF` | Cache failure does not trigger unbounded origin amplification | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-018` | `QLT-PERF` | Cache key is semantically scoped and cardinality-aware | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-019` | `QLT-PERF` | Invalidation fan-out is bounded | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-020` | `QLT-PERF` | Async work has backpressure | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-021` | `QLT-PERF` | Retry load is part of capacity planning | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-022` | `QLT-PERF` | Shared capacity has a noisy-neighbor strategy | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-023` | `QLT-PERF` | Event fan-out is intentional | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-024` | `QLT-PERF` | Ordering scope is no broader than invariant scope | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-025` | `QLT-PERF` | Automation has runaway-work bounds | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-026` | `QLT-PERF` | Provider throughput respects provider capability | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-027` | `QLT-PERF` | Webhook request path is bounded | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-028` | `QLT-PERF` | Document editing does not require full-history/full-workspace reload per edit | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-029` | `QLT-PERF` | Collaboration histories are paged/windowed | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-030` | `QLT-PERF` | Ephemeral signals stay lightweight | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-031` | `QLT-PERF` | Dashboard requests do not compute arbitrary tenant-wide scans repeatedly | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-032` | `QLT-PERF` | Performance optimization cannot weaken authorization filtering | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-033` | `QLT-PERF` | Large frontend collections are windowed where measured need exists | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-034` | `QLT-PERF` | Memoization follows measured render cost | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-035` | `QLT-PERF` | Realtime event does not trigger application-wide fan-out by default | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-036` | `QLT-PERF` | Dependency cost is evaluated at host boundary | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-037` | `QLT-PERF` | Mobile performance is not assumed from web performance | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-038` | `QLT-PERF` | No universal performance number is invented in this document | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-039` | `QLT-PERF` | Performance evidence states workload assumptions | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-040` | `QLT-PERF` | Load test protects a stated capacity question | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-041` | `QLT-PERF` | Noisy benchmark is not a false precision gate | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-042` | `QLT-PERF` | Retention is a scalability input | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-PERF-043` | `QLT-PERF` | Cost optimization cannot transfer semantic ownership | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-SEC-001` | `QLT-SEC` | External input is untrusted | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-002` | `QLT-SEC` | Validation and authorization are separate | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-003` | `QLT-SEC` | Unknown discriminator fails safely | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-004` | `QLT-SEC` | Encoding happens for the destination context | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-005` | `QLT-SEC` | Authentication success does not grant product access | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-006` | `QLT-SEC` | Revoked credentials do not revive through stale state | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-007` | `QLT-SEC` | Service principal authority is bounded | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-008` | `QLT-SEC` | Authorization is server enforced | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-009` | `QLT-SEC` | Unauthorized data is filtered before exposure | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-010` | `QLT-SEC` | Sensitive errors disclose minimum necessary detail | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-011` | `QLT-SEC` | Tenant scope is explicit at every relevant boundary | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-012` | `QLT-SEC` | Client cannot select another tenant by parameter substitution | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-013` | `QLT-SEC` | RLS proof uses PostgreSQL-realistic infrastructure | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-014` | `QLT-SEC` | Background execution is not trusted global tenant context | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-015` | `QLT-SEC` | Realtime authorization can be revoked mid-session | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-016` | `QLT-SEC` | Cache key includes security-relevant identity | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-017` | `QLT-SEC` | Secrets never enter source, docs, logs, events, or frontend bundles | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-018` | `QLT-SEC` | Logs redact secret-bearing fields structurally | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-019` | `QLT-SEC` | Invalid security configuration fails safe | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-020` | `QLT-SEC` | Feature flag does not become security bypass | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-021` | `QLT-SEC` | CSRF protection is tested at the real host/request boundary | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-022` | `QLT-SEC` | CORS is not an API security boundary | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-023` | `QLT-SEC` | Rate limiting fails predictably, not globally | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-024` | `QLT-SEC` | Webhook authenticity precedes business processing | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-025` | `QLT-SEC` | Webhook replay cannot duplicate business effect | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-026` | `QLT-SEC` | Server-side outbound destinations are constrained | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-027` | `QLT-SEC` | Redirect target is not trusted because it came from OAuth state or query string | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-028` | `QLT-SEC` | Filename is display metadata, not filesystem path authority | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-029` | `QLT-SEC` | Uploaded active content is rendered safely | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-030` | `QLT-SEC` | Signed download URLs are scoped and bounded | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-031` | `QLT-SEC` | “Frontend will escape it” is not the content-security contract | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-032` | `QLT-SEC` | User-defined execution is allow-listed or sandboxed by explicit design | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-033` | `QLT-SEC` | Internal security/lifecycle fields are not client-writable by reflection convenience | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-034` | `QLT-SEC` | Parent authorization does not imply arbitrary child/linked-resource access | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-035` | `QLT-SEC` | Bulk surfaces preserve security semantics | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-036` | `QLT-SEC` | Sensitive data does not propagate “just in case” | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-037` | `QLT-SEC` | Message authenticity/scope is not inferred from event name | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-038` | `QLT-SEC` | Provider OAuth scope is least privilege | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-039` | `QLT-SEC` | Financial administration receives stronger authorization/review | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-040` | `QLT-SEC` | Public capability is non-transitive by default | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-041` | `QLT-SEC` | Revoked public capability stops working despite stale client/cache | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-042` | `QLT-SEC` | Known vulnerability is assessed, not ignored by habit | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-043` | `QLT-SEC` | Dependency addition has security cost | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-044` | `QLT-SEC` | Public frontend environment variables are public | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-045` | `QLT-SEC` | Correlation replaces sensitive dump logging | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-046` | `QLT-SEC` | Security-sensitive administrative change is auditable when policy requires | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-047` | `QLT-SEC` | Diagnostic convenience does not override privacy boundary | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-048` | `QLT-SEC` | Fail closed to clients, fail observable to operators | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-049` | `QLT-SEC` | Threat review follows data/control flow | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-050` | `QLT-SEC` | Security tests include negative and adversarial scenarios | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-SEC-051` | `QLT-SEC` | Security test fixture is obviously non-secret | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-TST-001` | `QLT-TST` | Test pyramid is contractual, not numeric | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-002` | `QLT-TST` | Primary proof lives near the violated contract | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-003` | `QLT-TST` | Layered tests prove different properties | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-004` | `QLT-TST` | Rejection paths are first-class | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-005` | `QLT-TST` | Failure test proves absence of unintended effects | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-006` | `QLT-TST` | No-op tests protect event/history quality | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-007` | `QLT-TST` | Time is controlled when time affects semantics | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-008` | `QLT-TST` | Parallel execution is safe by design | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-009` | `QLT-TST` | Domain tests do not mock infrastructure | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-010` | `QLT-TST` | Application test distinguishes orchestration from provider implementation | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-011` | `QLT-TST` | Architecture tests fail on zero discovery | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-012` | `QLT-TST` | Provider/database semantics use realistic dependencies | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-013` | `QLT-TST` | Tenant-isolation test uses two real tenant datasets | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-014` | `QLT-TST` | Migration smoke includes production-like upgrade path | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-015` | `QLT-TST` | Outbox atomicity is an integration property | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-016` | `QLT-TST` | Message reliability tests include failure before success | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-017` | `QLT-TST` | API contract test uses public semantics | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-018` | `QLT-TST` | Production graph is tested without test-only architecture shortcuts | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-019` | `QLT-TST` | Mobile suite proves category coverage, not only test count | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-020` | `QLT-TST` | Generator test proves deterministic producer behavior | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-021` | `QLT-TST` | Test classification is architectural metadata | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-022` | `QLT-TST` | Rejected mutation does not leave optimistic cache corrupted | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-023` | `QLT-TST` | Realtime test includes refetch/reconciliation path | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-024` | `QLT-TST` | UI visual test is not behavior/security proof | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-025` | `QLT-TST` | E2E uses production-like build/configuration | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-026` | `QLT-TST` | Producer and generated consumer drift is a required gate | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-027` | `QLT-TST` | Event tests distinguish Domain event from public integration event | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-028` | `QLT-TST` | Verified webhook payload is still validated business input | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-029` | `QLT-TST` | Stronger test technique follows uncertainty | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-030` | `QLT-TST` | Snapshot review must be intentional | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-031` | `QLT-TST` | Mock only outside the property under test | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-032` | `QLT-TST` | External provider availability is not CI dependency | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-033` | `QLT-TST` | Required tests cannot pass through empty filters | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-034` | `QLT-TST` | Non-zero verifier protects property, not historical test name | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-035` | `QLT-TST` | Retry is not permanent flake handling | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-036` | `QLT-TST` | Failure artifacts are bounded and safe | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-037` | `QLT-TST` | Test helper cannot silently make invalid scenario valid | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-038` | `QLT-TST` | Valid-default builder does not hide the invariant under test | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-039` | `QLT-TST` | Coverage does not waive scenario review | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-040` | `QLT-TST` | Refactor does not delete evidence accidentally | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-041` | `QLT-TST` | CI topology may evolve while proof obligations remain | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-042` | `QLT-TST` | A critical meta-gate should be testable | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-043` | `QLT-TST` | Timeout reflects expected behavior | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-044` | `QLT-TST` | Do not cache test pass/fail across code changes as proof | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `QLT-TST-045` | `QLT-TST` | Same branch, different SHA is different evidence | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `SYS-001` | `SYS` | Business semantics precede deployment topology | [`docs/architecture/system-overview.md`](../architecture/system-overview.md) |
| `SYS-002` | `SYS` | Modular monolith is the backend deployment default | [`docs/architecture/system-overview.md`](../architecture/system-overview.md) |
| `SYS-003` | `SYS` | Extraction readiness is designed before extraction | [`docs/architecture/system-overview.md`](../architecture/system-overview.md) |
| `SYS-004` | `SYS` | Product capabilities are vertically owned | [`docs/architecture/system-overview.md`](../architecture/system-overview.md) |
| `SYS-005` | `SYS` | Backend is authoritative for protected durable business state | [`docs/architecture/system-overview.md`](../architecture/system-overview.md) |
| `SYS-006` | `SYS` | Authentication, authorization, tenant scope are distinct concerns | [`docs/architecture/system-overview.md`](../architecture/system-overview.md) |
| `SYS-007` | `SYS` | Tenant scope travels across every relevant boundary | [`docs/architecture/system-overview.md`](../architecture/system-overview.md) |
| `SYS-008` | `SYS` | Cross-stack communication uses explicit contracts | [`docs/architecture/system-overview.md`](../architecture/system-overview.md) |
| `SYS-009` | `SYS` | Internal refactors do not automatically break contracts | [`docs/architecture/system-overview.md`](../architecture/system-overview.md) |
| `SYS-010` | `SYS` | One authoritative owner per business fact | [`docs/architecture/system-overview.md`](../architecture/system-overview.md) |
| `SYS-011` | `SYS` | Foreign keys do not decide business ownership | [`docs/architecture/system-overview.md`](../architecture/system-overview.md) |
| `SYS-012` | `SYS` | Cross-context references preserve ownership | [`docs/architecture/system-overview.md`](../architecture/system-overview.md) |
| `SYS-013` | `SYS` | Strong consistency is explicit and narrow | [`docs/architecture/system-overview.md`](../architecture/system-overview.md) |
| `SYS-014` | `SYS` | Async delivery assumes retries and duplicates | [`docs/architecture/system-overview.md`](../architecture/system-overview.md) |
| `SYS-015` | `SYS` | Client realtime and cache must converge | [`docs/architecture/system-overview.md`](../architecture/system-overview.md) |
| `SYS-016` | `SYS` | Technical layers do not become product ownership silos | [`docs/architecture/system-overview.md`](../architecture/system-overview.md) |
| `SYS-017` | `SYS` | Cache is derived state | [`docs/architecture/system-overview.md`](../architecture/system-overview.md) |
| `SYS-018` | `SYS` | Failure semantics are part of architecture | [`docs/architecture/system-overview.md`](../architecture/system-overview.md) |
| `SYS-019` | `SYS` | Defense in depth does not duplicate ownership | [`docs/architecture/system-overview.md`](../architecture/system-overview.md) |
| `SYS-020` | `SYS` | Technical sharing may not erase semantic ownership | [`docs/architecture/system-overview.md`](../architecture/system-overview.md) |
| `SYS-ACT-001` | `SYS-ACT` | Activity is not transport history | [`docs/architecture/events-realtime-and-delivery-boundary.md`](../architecture/events-realtime-and-delivery-boundary.md) |
| `SYS-AUD-001` | `SYS-AUD` | Audit is not activity | [`docs/architecture/events-realtime-and-delivery-boundary.md`](../architecture/events-realtime-and-delivery-boundary.md) |
| `SYS-CON-001` | `SYS-CON` | Producer and consumers are explicit | [`docs/architecture/contract-boundaries.md`](../architecture/contract-boundaries.md) |
| `SYS-CON-002` | `SYS-CON` | Product semantics outrank transport representation | [`docs/architecture/contract-boundaries.md`](../architecture/contract-boundaries.md) |
| `SYS-CON-003` | `SYS-CON` | Implementation classes are not transport contracts | [`docs/architecture/contract-boundaries.md`](../architecture/contract-boundaries.md) |
| `SYS-CON-004` | `SYS-CON` | Contract identity is logical | [`docs/architecture/contract-boundaries.md`](../architecture/contract-boundaries.md) |
| `SYS-CON-005` | `SYS-CON` | Tenant scope is contract semantics | [`docs/architecture/contract-boundaries.md`](../architecture/contract-boundaries.md) |
| `SYS-CON-006` | `SYS-CON` | Semantic break counts as breaking | [`docs/architecture/contract-boundaries.md`](../architecture/contract-boundaries.md) |
| `SYS-CON-007` | `SYS-CON` | Generated output changes through producer | [`docs/architecture/contract-boundaries.md`](../architecture/contract-boundaries.md) |
| `SYS-CON-008` | `SYS-CON` | Compatibility is evaluated per consumer class | [`docs/architecture/contract-boundaries.md`](../architecture/contract-boundaries.md) |
| `SYS-CON-009` | `SYS-CON` | Mixed-version window is designed | [`docs/architecture/contract-boundaries.md`](../architecture/contract-boundaries.md) |
| `SYS-CON-010` | `SYS-CON` | Removal follows consumer proof | [`docs/architecture/contract-boundaries.md`](../architecture/contract-boundaries.md) |
| `SYS-CON-011` | `SYS-CON` | Security cannot be weakened for compatibility | [`docs/architecture/contract-boundaries.md`](../architecture/contract-boundaries.md) |
| `SYS-CON-012` | `SYS-CON` | Provider contracts never become Notrelix ubiquitous language by default | [`docs/architecture/contract-boundaries.md`](../architecture/contract-boundaries.md) |
| `SYS-CTX-001` | `SYS-CTX` | Context ownership follows semantic lifecycle, not storage topology | [`docs/architecture/bounded-context-map.md`](../architecture/bounded-context-map.md) |
| `SYS-CTX-002` | `SYS-CTX` | One authoritative owner per business fact | [`docs/architecture/bounded-context-map.md`](../architecture/bounded-context-map.md) |
| `SYS-CTX-003` | `SYS-CTX` | Context boundaries protect meaning, not folder symmetry | [`docs/architecture/bounded-context-map.md`](../architecture/bounded-context-map.md) |
| `SYS-CTX-004` | `SYS-CTX` | Cross-context references are contracts | [`docs/architecture/bounded-context-map.md`](../architecture/bounded-context-map.md) |
| `SYS-CTX-005` | `SYS-CTX` | Cross-context writes preserve target ownership | [`docs/architecture/bounded-context-map.md`](../architecture/bounded-context-map.md) |
| `SYS-DATA-001` | `SYS-DATA` | One authoritative owner per business fact | [`docs/architecture/data-ownership-and-consistency.md`](../architecture/data-ownership-and-consistency.md) |
| `SYS-DATA-002` | `SYS-DATA` | Physical database sharing does not authorize cross-context mutation | [`docs/architecture/data-ownership-and-consistency.md`](../architecture/data-ownership-and-consistency.md) |
| `SYS-DATA-003` | `SYS-DATA` | Ownership follows lifecycle/invariants | [`docs/architecture/data-ownership-and-consistency.md`](../architecture/data-ownership-and-consistency.md) |
| `SYS-DATA-004` | `SYS-DATA` | Strong consistency is business-justified | [`docs/architecture/data-ownership-and-consistency.md`](../architecture/data-ownership-and-consistency.md) |
| `SYS-DATA-005` | `SYS-DATA` | Choose consistency before implementation mechanism | [`docs/architecture/data-ownership-and-consistency.md`](../architecture/data-ownership-and-consistency.md) |
| `SYS-DATA-006` | `SYS-DATA` | Source commit precedes downstream asynchronous effect | [`docs/architecture/data-ownership-and-consistency.md`](../architecture/data-ownership-and-consistency.md) |
| `SYS-DATA-007` | `SYS-DATA` | Commit before irreversible external effect | [`docs/architecture/data-ownership-and-consistency.md`](../architecture/data-ownership-and-consistency.md) |
| `SYS-DATA-008` | `SYS-DATA` | At-least-once delivery requires idempotent consumption | [`docs/architecture/data-ownership-and-consistency.md`](../architecture/data-ownership-and-consistency.md) |
| `SYS-DATA-009` | `SYS-DATA` | Ordering guarantee is no broader than the business invariant | [`docs/architecture/data-ownership-and-consistency.md`](../architecture/data-ownership-and-consistency.md) |
| `SYS-DATA-010` | `SYS-DATA` | Concurrency conflicts fail closed | [`docs/architecture/data-ownership-and-consistency.md`](../architecture/data-ownership-and-consistency.md) |
| `SYS-DATA-011` | `SYS-DATA` | Durable invariant protection belongs at every necessary layer | [`docs/architecture/data-ownership-and-consistency.md`](../architecture/data-ownership-and-consistency.md) |
| `SYS-DATA-012` | `SYS-DATA` | Projections are disposable relative to source truth | [`docs/architecture/data-ownership-and-consistency.md`](../architecture/data-ownership-and-consistency.md) |
| `SYS-DATA-013` | `SYS-DATA` | Cache identity includes semantic scope | [`docs/architecture/data-ownership-and-consistency.md`](../architecture/data-ownership-and-consistency.md) |
| `SYS-DATA-014` | `SYS-DATA` | Frontend cache must not outlive its authority scope | [`docs/architecture/data-ownership-and-consistency.md`](../architecture/data-ownership-and-consistency.md) |
| `SYS-DATA-015` | `SYS-DATA` | Optimistic state is not business commit | [`docs/architecture/data-ownership-and-consistency.md`](../architecture/data-ownership-and-consistency.md) |
| `SYS-DATA-016` | `SYS-DATA` | Delete/archive policy belongs to semantic owner | [`docs/architecture/data-ownership-and-consistency.md`](../architecture/data-ownership-and-consistency.md) |
| `SYS-DATA-017` | `SYS-DATA` | Schema migration does not silently change semantic ownership | [`docs/architecture/data-ownership-and-consistency.md`](../architecture/data-ownership-and-consistency.md) |
| `SYS-DATA-018` | `SYS-DATA` | Dual write does not imply dual authority | [`docs/architecture/data-ownership-and-consistency.md`](../architecture/data-ownership-and-consistency.md) |
| `SYS-DATA-019` | `SYS-DATA` | Consistency promise includes user-visible semantics | [`docs/architecture/data-ownership-and-consistency.md`](../architecture/data-ownership-and-consistency.md) |
| `SYS-DATA-020` | `SYS-DATA` | Retry is bounded by semantics, not generic transport policy alone | [`docs/architecture/data-ownership-and-consistency.md`](../architecture/data-ownership-and-consistency.md) |
| `SYS-EVT-001` | `SYS-EVT` | Events describe facts, not hidden commands | [`docs/architecture/events-realtime-and-delivery-boundary.md`](../architecture/events-realtime-and-delivery-boundary.md) |
| `SYS-EVT-002` | `SYS-EVT` | Success event follows state success | [`docs/architecture/events-realtime-and-delivery-boundary.md`](../architecture/events-realtime-and-delivery-boundary.md) |
| `SYS-EVT-003` | `SYS-EVT` | Domain event is transport-free | [`docs/architecture/events-realtime-and-delivery-boundary.md`](../architecture/events-realtime-and-delivery-boundary.md) |
| `SYS-EVT-004` | `SYS-EVT` | Integration event is a governed contract | [`docs/architecture/events-realtime-and-delivery-boundary.md`](../architecture/events-realtime-and-delivery-boundary.md) |
| `SYS-EVT-005` | `SYS-EVT` | Public event granularity follows stable product facts | [`docs/architecture/events-realtime-and-delivery-boundary.md`](../architecture/events-realtime-and-delivery-boundary.md) |
| `SYS-EVT-006` | `SYS-EVT` | Outbox enrollment follows the source transaction contract | [`docs/architecture/events-realtime-and-delivery-boundary.md`](../architecture/events-realtime-and-delivery-boundary.md) |
| `SYS-EVT-007` | `SYS-EVT` | Logical identity is independent of class names | [`docs/architecture/events-realtime-and-delivery-boundary.md`](../architecture/events-realtime-and-delivery-boundary.md) |
| `SYS-EVT-008` | `SYS-EVT` | Durable consumers assume duplicate delivery | [`docs/architecture/events-realtime-and-delivery-boundary.md`](../architecture/events-realtime-and-delivery-boundary.md) |
| `SYS-EVT-009` | `SYS-EVT` | Sequence advances after successful processing | [`docs/architecture/events-realtime-and-delivery-boundary.md`](../architecture/events-realtime-and-delivery-boundary.md) |
| `SYS-EVT-010` | `SYS-EVT` | Poison handling does not replace compatibility governance | [`docs/architecture/events-realtime-and-delivery-boundary.md`](../architecture/events-realtime-and-delivery-boundary.md) |
| `SYS-EVT-011` | `SYS-EVT` | Platform owns delivery mechanics, not business meaning | [`docs/architecture/events-realtime-and-delivery-boundary.md`](../architecture/events-realtime-and-delivery-boundary.md) |
| `SYS-EXT-001` | `SYS-EXT` | Bounded context is an extraction seam, not an extraction order | [`docs/architecture/capability-extraction-strategy.md`](../architecture/capability-extraction-strategy.md) |
| `SYS-EXT-002` | `SYS-EXT` | Extraction preserves product semantics | [`docs/architecture/capability-extraction-strategy.md`](../architecture/capability-extraction-strategy.md) |
| `SYS-EXT-003` | `SYS-EXT` | Extraction value must exceed recurring distributed cost | [`docs/architecture/capability-extraction-strategy.md`](../architecture/capability-extraction-strategy.md) |
| `SYS-EXT-004` | `SYS-EXT` | No extraction with ambiguous fact ownership | [`docs/architecture/capability-extraction-strategy.md`](../architecture/capability-extraction-strategy.md) |
| `SYS-EXT-005` | `SYS-EXT` | Extraction transport follows the semantic boundary | [`docs/architecture/capability-extraction-strategy.md`](../architecture/capability-extraction-strategy.md) |
| `SYS-EXT-006` | `SYS-EXT` | Data moves after ownership is clear | [`docs/architecture/capability-extraction-strategy.md`](../architecture/capability-extraction-strategy.md) |
| `SYS-EXT-007` | `SYS-EXT` | No foreign direct writes after authority cutover | [`docs/architecture/capability-extraction-strategy.md`](../architecture/capability-extraction-strategy.md) |
| `SYS-EXT-008` | `SYS-EXT` | Cross-service atomicity is not assumed | [`docs/architecture/capability-extraction-strategy.md`](../architecture/capability-extraction-strategy.md) |
| `SYS-EXT-009` | `SYS-EXT` | Public API stability is independent of service topology | [`docs/architecture/capability-extraction-strategy.md`](../architecture/capability-extraction-strategy.md) |
| `SYS-EXT-010` | `SYS-EXT` | Tenant context survives extraction | [`docs/architecture/capability-extraction-strategy.md`](../architecture/capability-extraction-strategy.md) |
| `SYS-EXT-011` | `SYS-EXT` | Operational evidence exists before cutover | [`docs/architecture/capability-extraction-strategy.md`](../architecture/capability-extraction-strategy.md) |
| `SYS-EXT-012` | `SYS-EXT` | Team topology does not redefine product semantics | [`docs/architecture/capability-extraction-strategy.md`](../architecture/capability-extraction-strategy.md) |
| `SYS-EXT-013` | `SYS-EXT` | Make the monolith modular before making it distributed | [`docs/architecture/capability-extraction-strategy.md`](../architecture/capability-extraction-strategy.md) |
| `SYS-EXT-014` | `SYS-EXT` | Cutover remains recoverable until old-path removal | [`docs/architecture/capability-extraction-strategy.md`](../architecture/capability-extraction-strategy.md) |
| `SYS-EXT-015` | `SYS-EXT` | Do not clone monolith structure by symmetry | [`docs/architecture/capability-extraction-strategy.md`](../architecture/capability-extraction-strategy.md) |
| `SYS-EXT-016` | `SYS-EXT` | Technical service does not imply business context | [`docs/architecture/capability-extraction-strategy.md`](../architecture/capability-extraction-strategy.md) |
| `SYS-NOTIF-001` | `SYS-NOTIF` | Notification semantic is separate from delivery provider | [`docs/architecture/events-realtime-and-delivery-boundary.md`](../architecture/events-realtime-and-delivery-boundary.md) |
| `SYS-OBS-001` | `SYS-OBS` | Correlation links layers without merging them | [`docs/architecture/events-realtime-and-delivery-boundary.md`](../architecture/events-realtime-and-delivery-boundary.md) |
| `SYS-RT-001` | `SYS-RT` | Realtime logical identity is stable | [`docs/architecture/events-realtime-and-delivery-boundary.md`](../architecture/events-realtime-and-delivery-boundary.md) |
| `SYS-RT-002` | `SYS-RT` | Subscription scope is explicit and narrow | [`docs/architecture/events-realtime-and-delivery-boundary.md`](../architecture/events-realtime-and-delivery-boundary.md) |
| `SYS-RT-003` | `SYS-RT` | Realtime clients assume duplicate/out-of-order delivery | [`docs/architecture/events-realtime-and-delivery-boundary.md`](../architecture/events-realtime-and-delivery-boundary.md) |
| `SYS-RT-004` | `SYS-RT` | Patch only when correctness can be proven | [`docs/architecture/events-realtime-and-delivery-boundary.md`](../architecture/events-realtime-and-delivery-boundary.md) |
| `WM-001` | `WM` | Work Management is not Kanban CRUD | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-002` | `WM` | One work model, many views | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-003` | `WM` | Board belongs to exactly one Workspace | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-004` | `WM` | Board lifecycle is not view lifecycle | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-005` | `WM` | Template is creation input, not hidden shared runtime authority | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-006` | `WM` | Field identity survives label rename | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-007` | `WM` | A Field Type has one semantic contract | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-008` | `WM` | Default is creation behavior, not retroactive data mutation | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-009` | `WM` | System fields are capability-protected | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-010` | `WM` | Item is the authoritative work record | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-011` | `WM` | Value must match current field schema | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-012` | `WM` | Item mutation uses optimistic concurrency for competing writes | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-013` | `WM` | Query-heavy values cannot require full-Board arbitrary JSON scans | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-014` | `WM` | BoardGroup is not universal status | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-015` | `WM` | Kanban move mutates grouping field, not BoardGroup by default | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-016` | `WM` | Ordering is deterministic and concurrency-aware | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-017` | `WM` | View configuration never owns duplicate Items | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-018` | `WM` | View configuration validates against current Board schema | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-019` | `WM` | Form submission maps to authoritative Work Management operations | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-020` | `WM` | Public form capability is write-scoped, not Workspace visibility | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-021` | `WM` | Relation is stable identity, not embedded aggregate graph | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-022` | `WM` | Formula result is derived and non-authoritative | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-023` | `WM` | Rollup is derived projection, not editable source | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-024` | `WM` | Labels must have distinct semantics from select/status fields | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-025` | `WM` | Checklist is subordinate work, not a duplicate Board | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-026` | `WM` | Approval decision is explicit business state | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-027` | `WM` | Workload is derived from owned work facts unless explicitly edited as capacity policy | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-028` | `WM` | Work template instantiation creates owned identities | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-029` | `WM` | Extension capability must not create duplicate Item truth | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-030` | `WM` | Every material Work Management operation is server-authorized | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-031` | `WM` | Public events expose stable product facts, not aggregate dumps | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-032` | `WM` | Realtime assumes duplicate/out-of-order delivery | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-033` | `WM` | Board deletion is explicit product lifecycle, not cascade symmetry | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-034` | `WM` | Conflicts fail without partial hidden mutation | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-035` | `WM` | Import uses Work Management semantics | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-036` | `WM` | External-reference validation uses supplied facts/contracts | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-037` | `WM` | Option identity survives rename | [`docs/product/work-management.md`](../product/work-management.md) |
| `WM-038` | `WM` | Work Status is context-local field semantics | [`docs/product/work-management.md`](../product/work-management.md) |
| `WSP-001` | `WSP` | Workspace is a collaboration tenant, not a generic global scope | [`docs/product/workspaces.md`](../product/workspaces.md) |
| `WSP-002` | `WSP` | Workspace scope is explicit | [`docs/product/workspaces.md`](../product/workspaces.md) |
| `WSP-003` | `WSP` | Workspace lifecycle is not Account lifecycle | [`docs/product/workspaces.md`](../product/workspaces.md) |
| `WSP-004` | `WSP` | Account relationship does not replace Workspace membership | [`docs/product/workspaces.md`](../product/workspaces.md) |
| `WSP-005` | `WSP` | Workspace membership is a security boundary | [`docs/product/workspaces.md`](../product/workspaces.md) |
| `WSP-006` | `WSP` | Invitation is not Membership | [`docs/product/workspaces.md`](../product/workspaces.md) |
| `WSP-007` | `WSP` | Workspace role is not universal resource permission | [`docs/product/workspaces.md`](../product/workspaces.md) |
| `WSP-008` | `WSP` | Cross-member invariants fail atomically | [`docs/product/workspaces.md`](../product/workspaces.md) |
| `WSP-009` | `WSP` | Invitation acceptance is retry-safe | [`docs/product/workspaces.md`](../product/workspaces.md) |
| `WSP-010` | `WSP` | One Identity may belong to many Workspaces | [`docs/product/workspaces.md`](../product/workspaces.md) |
| `WSP-011` | `WSP` | Entitlement and membership are distinct | [`docs/product/workspaces.md`](../product/workspaces.md) |
| `WSP-012` | `WSP` | Workspace settings cannot absorb Account administration | [`docs/product/workspaces.md`](../product/workspaces.md) |
| `WSP-013` | `WSP` | Space is organizational structure, not implicit authorization | [`docs/product/workspaces.md`](../product/workspaces.md) |
| `WSP-014` | `WSP` | Team membership requires Workspace membership compatibility | [`docs/product/workspaces.md`](../product/workspaces.md) |
| `WSP-015` | `WSP` | Workspace switch invalidates old-scope assumptions | [`docs/product/workspaces.md`](../product/workspaces.md) |
| `WSP-016` | `WSP` | Workspace identity survives every relevant boundary | [`docs/product/workspaces.md`](../product/workspaces.md) |
| `WSP-017` | `WSP` | Workspace events carry stable scope, not foreign aggregates | [`docs/product/workspaces.md`](../product/workspaces.md) |
| `WSP-018` | `WSP` | Workspace deletion is a process, not ORM cascade | [`docs/product/workspaces.md`](../product/workspaces.md) |
| `WSP-019` | `WSP` | Membership and invitation races resolve deterministically | [`docs/product/workspaces.md`](../product/workspaces.md) |
| `WSP-020` | `WSP` | External provisioning uses Workspaces contracts | [`docs/product/workspaces.md`](../product/workspaces.md) |

## Generation contract

Stable rules are declared in authored canonical Markdown headings.

Example:

```markdown
## FE-STATE-001 — Server state remains backend-authoritative
```

To change the rule index:

```text
change the canonical source rule
→ run check-rule-ids.mjs
→ regenerate this index
```

Do not edit this generated index manually.

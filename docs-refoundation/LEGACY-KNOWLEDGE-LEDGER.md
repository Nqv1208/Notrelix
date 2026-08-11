# Notrelix Documentation Re-foundation Ledger

Baseline SHA: `f050dab92d63819d53472dc02fa671e878aa3686`

This ledger records the disposition of the legacy documentation corpus before
legacy files are removed from the active reading path. It is migration evidence,
not a canonical architecture handbook.

## Backend Legacy Corpus

| Legacy path | Status | Destination / reason | Verified against | Deletion allowed |
|---|---|---|---|---|
| `backend/RULE.md` | MIGRATE | `backend/docs/architecture/*.md`, `backend/AGENTS.md` | `backend/backend.slnx`, `backend/src/**`, `backend/tests/**` | YES |
| `backend/PROMPT.md` | DUPLICATE | `backend/AGENTS.md` | root `AGENTS.md`, backend docs | YES |
| `backend/CONFIGURATION.md` | MIGRATE | `backend/docs/operations/configuration-and-runtime.md` | `.env.*`, `docker-compose*.yml`, API host config | YES |
| `backend/docs/ADR/ADR-001-pipeline-boundary.md` | ADR | `backend/docs/decisions/ADR-001-pipeline-boundary.md` | Application pipeline source/tests | YES |
| `backend/docs/ADR/ADR-002-rls-bootstrap-connection-lifecycle.md` | ADR | `backend/docs/decisions/ADR-002-rls-bootstrap-connection-lifecycle.md` | Infrastructure RLS source/migrations | YES |
| `backend/docs/ADR/ADR-003-csrf-protection.md` | ADR | `backend/docs/decisions/ADR-003-csrf-protection.md` | API auth/CSRF configuration | YES |
| `backend/docs/ADR/ADR-004-rate-limiting-architecture.md` | ADR | `backend/docs/decisions/ADR-004-rate-limiting-architecture.md` | API rate-limit configuration/tests | YES |
| `backend/docs/ADR/THREAT-MODEL.md` | MIGRATE | `backend/docs/architecture/security-tenancy-authorization.md` | auth, tenant, RLS, cache, background docs/source | YES |
| `backend/docs/api/*` | MIGRATE | `backend/docs/architecture/api-and-contracts.md` | endpoint source, OpenAPI/contracts | YES |
| `backend/docs/application/**` | MIGRATE | `application-model.md`, security, messaging, testing docs | Application requests/behaviors/tests | YES |
| `backend/docs/architecture/application-persistence-boundary.md` | MIGRATE | `application-model.md`, `infrastructure-and-data.md` | Application and Infrastructure references | YES |
| `backend/docs/audits/**` | HISTORICAL | Git history; durable rules migrated | tests/source/docs | YES |
| `backend/docs/caching/authorized-cache.md` | MIGRATE | `application-model.md`, `security-tenancy-authorization.md`, `infrastructure-and-data.md` | cache abstractions and tenant rules | YES |
| `backend/docs/concurrency/optimistic-concurrency.md` | MIGRATE | `domain-modeling.md`, `application-model.md`, `migrations-and-data-change.md` | versioned aggregates/tests | YES |
| `backend/docs/database/notrelix-enterprise-schema-v2-clean-baseline.sql` | STALE | migration chain/schema model is authority | Infrastructure migrations | YES |
| `backend/docs/database/notrelix-enterprise-schema-v2-explained.md` | MIGRATE | `infrastructure-and-data.md`, `migrations-and-data-change.md` | EF mappings/migrations | YES |
| `backend/docs/domain/**` | MIGRATE | `domain-modeling.md`, product context docs, ADRs when needed | Domain source/tests/events | YES |
| `backend/docs/infrastructure/rules/**` | MIGRATE | `infrastructure-and-data.md`, `platform-and-messaging.md`, operations/testing docs | Infrastructure source/tests | YES |
| `backend/docs/issues/**` | ISSUE | unresolved work belongs to issue/project tracking; durable rules migrated | current source/tests | YES |
| `backend/docs/messaging/consumer-idempotency.md` | MIGRATE | `platform-and-messaging.md` | Platform outbox/idempotency source/tests | YES |
| `backend/docs/security/tenant-isolation.md` | MIGRATE | `security-tenancy-authorization.md` | Application auth, RLS, cache, API tests | YES |
| `backend/docs/superpowers/specs/**` | HISTORICAL | Git history; product semantics migrated when durable | current product docs/source | YES |
| `backend/docs/testing/**` | MIGRATE | `testing-and-quality-gates.md` | `backend.slnx`, test projects, CI | YES |
| `backend/docs/WorkManagement-Roadmap.md` | HISTORICAL | durable semantics owned by product/context docs | WorkManagement source/tests | YES |
| `backend/docs/notrelix-backend-enterprise-roadmap.md` | HISTORICAL | Git history; durable rules migrated | backend topology/source | YES |
| `backend/docs/notrelix_enterprise_development_blueprint_bounded_contexts.md` | MIGRATE | backend overview and product context docs | bounded-context source/docs | YES |

## Frontend Legacy Corpus

| Legacy path | Status | Destination / reason | Verified against | Deletion allowed |
|---|---|---|---|---|
| `frontend/ARCHITECTURE.md` | MIGRATE | `frontend/docs/architecture/*.md` | manifest, apps, packages, tests | YES |
| `frontend/RULES.md` | MIGRATE | `frontend/AGENTS.md`, architecture docs | dependency-rules, package manifests | YES |
| `frontend/MIGRATION_TRACKER.md` | HISTORICAL | Git history/issues | current gates/source | YES |
| `frontend/docs/adr/ADR-001-frontend-web-freeze-scope.md` | HISTORICAL | durable policy in `architecture-change-policy.md` | dependency gates and docs | YES |
| `frontend/docs/client-architecture/**` | MIGRATE | `frontend/docs/architecture/*.md` | current source/manifests/tests | YES |
| `frontend/docs/client/adr/ADR-001-framework-split.md` | ADR | `frontend/docs/decisions/FE-ADR-001-framework-split.md` | package manifests/apps | YES |
| `frontend/docs/client/adr/ADR-002-package-manager.md` | ADR | `frontend/docs/decisions/FE-ADR-002-package-manager.md` | `package.json`, lockfile | YES |
| `frontend/docs/client/adr/ADR-003-package-exports.md` | ADR | `frontend/docs/decisions/FE-ADR-003-package-exports.md` | package exports/dependency-rules | YES |
| `frontend/docs/client/adr/ADR-004-no-next-in-packages.md` | ADR | `frontend/docs/decisions/FE-ADR-004-no-next-in-packages.md` | package dependencies/import checks | YES |
| `frontend/docs/client/adr/ADR-005-auth-session-model.md` | ADR | `frontend/docs/decisions/FE-ADR-005-auth-session-model.md` | runtime/auth/session source | YES |
| `frontend/docs/client/architecture/boundary-matrix.md` | GENERATED | `frontend/docs/generated/package-boundaries.md` plus `dependency-boundaries.md` | architecture manifest/generator | YES |
| `frontend/docs/client/architecture/package-boundaries.generated.md` | GENERATED | `frontend/docs/generated/package-boundaries.md` | `docs:generate` / `docs:check` | YES |
| `frontend/docs/client/archive/**` | HISTORICAL | Git history | current docs/source | YES |
| `frontend/docs/client/audits/**` | HISTORICAL / ISSUE | durable rules migrated; unresolved work to tracker | source/tests | YES |
| `frontend/docs/client/migration/tracker.md` | HISTORICAL | Git history/issues | current gates/source | YES |
| `frontend/docs/plans/**` | HISTORICAL | durable architecture migrated; progress removed | current source/tests | YES |
| `frontend/docs/FRONTEND_PLATFORM_FREEZE_SPEC.md` | MIGRATE | architecture docs and change policy | dependency-rules/tests | YES |
| `frontend/docs/notrelix-client-technical-project-structure.md` | MIGRATE | frontend overview, dependency boundaries, host docs | workspace/package manifests | YES |

## Sample Fidelity Audit

- Aggregate boundary, mutation ordering, no-op, lifecycle/deletion, version and event rules are represented in `backend/docs/architecture/domain-modeling.md`.
- Pipeline order, tenant/resource resolution, authorization, concurrency and post-commit rules are represented in `backend/docs/architecture/application-model.md`.
- RLS, EF migrations, indexes, cache/provider adapters and schema authority are represented in `backend/docs/architecture/infrastructure-and-data.md` and `backend/docs/operations/migrations-and-data-change.md`.
- Outbox, consumer idempotency, retry, poison/dead-letter, ordering and tenant context are represented in `backend/docs/architecture/platform-and-messaging.md`.
- Framework split, package manager, exports, no-Next-in-packages and auth session decisions are preserved as frontend ADRs.
- Frontend package boundaries are generated from `frontend/tooling/dependency-rules/src/architecture-manifest.ts`.

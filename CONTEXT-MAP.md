# CONTEXT-MAP.md — Notrelix Documentation Router

Use this file when you know what kind of change you are making but not which
document owns it. This file is navigation only.

## Root Entry Points

| Need | Read |
|---|---|
| Repository orientation | `README.md` |
| Repository-wide invariant | `RULE.md` |
| Product meaning | `PRODUCT.md` |
| Product/design principles | `DESIGN.md` |
| Coding-agent workflow | `AGENTS.md` |
| Backend work | `backend/AGENTS.md`, `backend/docs/README.md` |
| Frontend work | `frontend/AGENTS.md`, `frontend/docs/README.md` |

## Backend

| Change type | Canonical owner |
|---|---|
| Project/layer topology | `backend/docs/architecture/backend-overview.md` |
| Domain aggregate/rule/event/lifecycle | `backend/docs/architecture/domain-modeling.md` |
| Application use case/pipeline/transaction | `backend/docs/architecture/application-model.md` |
| EF/schema/RLS/cache/provider | `backend/docs/architecture/infrastructure-and-data.md` |
| Messaging/outbox/background/idempotency | `backend/docs/architecture/platform-and-messaging.md` |
| HTTP/OpenAPI/public contract | `backend/docs/architecture/api-and-contracts.md` |
| Auth/tenant/security | `backend/docs/architecture/security-tenancy-authorization.md` |
| Tests/gates | `backend/docs/architecture/testing-and-quality-gates.md` |
| Config/runtime | `backend/docs/operations/configuration-and-runtime.md` |
| Migrations/data changes | `backend/docs/operations/migrations-and-data-change.md` |
| Historical backend rationale | `backend/docs/decisions/README.md` |

## Frontend

| Change type | Canonical owner |
|---|---|
| Workspace/host/package overview | `frontend/docs/architecture/frontend-overview.md` |
| Package dependency boundaries | `frontend/docs/architecture/dependency-boundaries.md` |
| Providers/routing/host composition | `frontend/docs/architecture/hosts-composition-routing.md` |
| API/generated contracts/client behavior | `frontend/docs/architecture/api-and-contracts.md` |
| Query keys/cache/mutations | `frontend/docs/architecture/state-query-mutations.md` |
| Realtime | `frontend/docs/architecture/realtime.md` |
| UI implementation/accessibility | `frontend/docs/architecture/ui-and-design-system.md` |
| Tests/gates | `frontend/docs/architecture/testing-and-quality-gates.md` |
| Architecture change policy | `frontend/docs/architecture/architecture-change-policy.md` |
| Historical frontend rationale | `frontend/docs/decisions/README.md` |

## Generated Evidence

- Backend project inventory: `backend/backend.slnx`
- Frontend package inventory: `frontend/tooling/dependency-rules/src/architecture-manifest.ts`
- Frontend generated package boundaries: `frontend/docs/generated/package-boundaries.md`

## Governance

Run `make docs-check` after documentation or generated-evidence changes.

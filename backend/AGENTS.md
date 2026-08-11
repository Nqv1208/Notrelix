# backend/AGENTS.md — Backend Execution Contract

Read root `AGENTS.md`, root `RULE.md`, this file, the nearest project
`AGENTS.md`, the owning product-context contract, and the relevant backend
canonical topic before editing.

## Start from the use case

Identify, in order:

1. bounded context and business module;
2. aggregate/consistency owner;
3. command or query semantics;
4. actor, tenant and authorization requirement;
5. external/cross-aggregate facts;
6. persistence/migration/RLS implications;
7. domain/integration/realtime contract implications;
8. idempotency/concurrency/retry requirements;
9. affected tests and architecture gates.

Do not start from a controller, table, or “service” class and then invent semantics around it.

## Route by responsibility

- Domain behavior/invariant/value/event → `docs/architecture/domain-modeling.md`
- Use case/pipeline/authorization/ports → `docs/architecture/application-model.md`
- EF/RLS/provider/persistence/cache adapter → `docs/architecture/infrastructure-and-data.md`
- reusable delivery/messaging/runtime mechanism → `docs/architecture/platform-and-messaging.md`
- HTTP/OpenAPI/host composition → `docs/architecture/api-and-contracts.md`
- Auth/tenant/security → `docs/architecture/security-tenancy-authorization.md`
- tests/gates/support libraries → `docs/architecture/testing-and-quality-gates.md`
- runtime/configuration → `docs/operations/configuration-and-runtime.md`
- migrations/data changes → `docs/operations/migrations-and-data-change.md`

Then read the nearest scoped `AGENTS.md` when present:

- `src/Notrelix.Domain/AGENTS.md`
- `src/Notrelix.Application/AGENTS.md`
- `src/Notrelix.Infrastructure/AGENTS.md`
- `src/Notrelix.Platform/AGENTS.md`
- `src/Notrelix.API/AGENTS.md`
- `tests/AGENTS.md`

Do not use deleted legacy docs, old rule packs, roadmap/freeze artifacts or
audits as precedent. If one contains knowledge that is not represented in the
canonical docs, stop and classify the drift before implementing behavior.

## Vertical transaction principle

A feature may require edits across projects. Make the smallest **complete business transaction**, not arbitrary per-layer handoffs. If a command changes an aggregate, persists a new field, emits an event and exposes a REST contract, all required pieces belong to the same change unless compatibility explicitly requires staged rollout.

## Backend change report

State: owning context/use case, aggregate/authorization/tenant scope,
migrations, contracts/events, tests/gates executed, and any unresolved
externally-owned dependency.

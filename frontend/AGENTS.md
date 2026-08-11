# Frontend Execution Contract

Read root `AGENTS.md`, root `RULE.md`, this file, `frontend/README.md`, and
the relevant canonical frontend document before editing.

## Start from the owner

Identify:

1. host, product capability, cross-product feature, foundation, runtime, UI or tooling owner;
2. backend REST/realtime contract dependency;
3. query/server-state owner and cache scope;
4. web, mobile and marketing impact;
5. dependency-manifest impact;
6. accessibility and loading/error/empty/permission/conflict states;
7. tests and gates that prove the change.

Do not start from a component file and invent architecture around it.

## Route by responsibility

- Architecture overview and package families: `docs/architecture/frontend-overview.md`
- Dependency graph, exports, deep imports, runtime/mobile purity: `docs/architecture/dependency-boundaries.md`
- App providers, routing, auth/session and host differences: `docs/architecture/hosts-composition-routing.md`
- REST/generated contracts/API client behavior: `docs/architecture/api-and-contracts.md`
- Query keys, mutations, optimistic updates and workspace transitions: `docs/architecture/state-query-mutations.md`
- Realtime connection/subscriptions/reconciliation: `docs/architecture/realtime.md`
- UI primitives/design-system/accessibility: `docs/architecture/ui-and-design-system.md`
- Tests/gates/non-zero-work evidence: `docs/architecture/testing-and-quality-gates.md`
- Architecture decisions/change control: `docs/architecture/architecture-change-policy.md`

The executable package/dependency authority is
`tooling/dependency-rules/src/architecture-manifest.ts`.

## Stop Conditions

Stop and record an unresolved decision rather than guessing when frontend
dependency tooling contradicts documented package intent, backend REST/realtime
contracts are missing or conflicting, web/mobile runtime ownership is unclear,
product semantics require backend/domain decisions, or an ADR conflicts with
current source and no superseding decision exists.

## Rules

- Apps compose; reusable product behavior belongs in owning packages.
- No deep imports across package boundaries.
- No Next.js/web-only runtime inside reusable packages.
- Mobile production packages must stay free of DOM/react-dom/web-runtime imports.
- Server state remains backend-authoritative.
- UX permission checks do not replace backend authorization.

## Completion Report

Report files changed, owner/invariant, contracts touched, tests/gates run, and
remaining external decisions.

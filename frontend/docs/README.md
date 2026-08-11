# Frontend Documentation

This is the frontend documentation index. It separates authored architecture,
historical decisions and generated executable evidence.

## Reading Order

1. `../README.md` for workspace orientation and commands.
2. `../AGENTS.md` for coding-agent execution rules.
3. The architecture document for the concern being changed.
4. Related ADRs under `decisions/`.
5. Generated inventory under `generated/`.
6. Source, tests, manifests and CI gates for current evidence.

## Architecture

- [Frontend overview](architecture/frontend-overview.md)
- [Dependency boundaries](architecture/dependency-boundaries.md)
- [Hosts, composition and routing](architecture/hosts-composition-routing.md)
- [API and contracts](architecture/api-and-contracts.md)
- [State, query and mutations](architecture/state-query-mutations.md)
- [Realtime](architecture/realtime.md)
- [UI and design system](architecture/ui-and-design-system.md)
- [Testing and quality gates](architecture/testing-and-quality-gates.md)
- [Architecture change policy](architecture/architecture-change-policy.md)

## Decisions

- [Frontend ADR registry](decisions/README.md)

## Generated Evidence

- [Package boundaries](generated/package-boundaries.md)

Generated evidence names its producer and must be checked for drift. Do not
hand-edit generated files.

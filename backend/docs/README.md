# Backend Documentation

This is the backend documentation index. It separates authored architecture,
historical decisions, and operational runbooks.

## Reading Order

1. `../README.md` for orientation and commands.
2. `../AGENTS.md` for coding-agent execution rules.
3. The architecture document for the concern being changed.
4. Related ADRs under `decisions/`.
5. Source, tests, contracts, migrations, and CI gates for current evidence.

## Architecture

- [Backend overview](architecture/backend-overview.md)
- [Domain modeling](architecture/domain-modeling.md)
- [Application model](architecture/application-model.md)
- [Infrastructure and data](architecture/infrastructure-and-data.md)
- [Platform and messaging](architecture/platform-and-messaging.md)
- [API and contracts](architecture/api-and-contracts.md)
- [Security, tenancy, and authorization](architecture/security-tenancy-authorization.md)
- [Testing and quality gates](architecture/testing-and-quality-gates.md)

## Operations

- [Configuration and runtime](operations/configuration-and-runtime.md)
- [Migrations and data change](operations/migrations-and-data-change.md)

## Decisions

- [Decision registry](decisions/README.md)

No roadmap, freeze checklist, audit snapshot, or migration tracker is a backend
architecture authority. If source and documentation disagree, classify the drift
before changing behavior.

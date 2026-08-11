# Configuration and Runtime

## Scope

Backend configuration precedence, environment files, options, secrets, local
runtime dependencies, Docker, environment-specific behavior and safe local reset.

## Responsibility / Ownership

Runtime configuration is owned by the host and operations mechanisms.
Application/Domain behavior consumes typed options and explicit dependencies.

## Current Architecture

The repository uses `appsettings*.json`, `.env.*` files and
`docker-compose*.yml` to compose local and environment-specific runtime.

## Normative Contracts

- Non-secret defaults belong in `appsettings*.json`.
- Secrets and credentials belong in environment variables or secret stores, not
  committed docs/source.
- Docker compose files define local dependency topology.
- Options must be validated at startup for required production values.
- Local reset/seed commands must be explicit and safe for the selected
  environment.
- Provider endpoints/keys stay outside Domain/Application business contracts.

## Allowed Design

- Environment-specific compose files for dev/staging/prod topology.
- Typed options validation and fail-fast startup.
- Local-only example values in `.env.example`.

## Forbidden Design

- Production secret defaults in committed docs/source.
- Business behavior that changes because of ambient environment instead of a
  declared feature/config contract.
- Reset/migration commands that silently target production.

## Failure Modes

- Missing production config reaches runtime as a partial service.
- Dev credentials are copied into prod docs.
- Compose and appsettings describe conflicting dependency names.

## Change Impact Rules

Runtime/config changes require source verification in host startup/options,
compose/env files and any affected integration tests.

## Executable Evidence / Tests / Gates

- `.env.example`, `.env.dev`, `.env.prod`, `.env.staging`
- `docker-compose*.yml`
- API host startup/options validation

## Related ADRs

See `../decisions/README.md`.

## Related Source Manifests

Docker compose and API project configuration.

## Non-responsibilities

This document does not define Domain rules, data migrations or frontend env
contracts.

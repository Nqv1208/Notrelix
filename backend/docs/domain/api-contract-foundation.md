# API Contract Foundation

> Stable for v1. Additive changes allowed. Breaking changes require review.

## Route convention

- Versioned: `/api/v1/{context}/{resource}`
- Unversioned: `/health/*`, OAuth callbacks

## Operation ID convention

- Format: `Context.Resource.Action`
- Example: `WorkManagement.Board.Create`
- Unique across v1
- 231+ endpoints have explicit `.WithName()`

## Schema convention

- `CustomSchemaIds`: full CLR type name (`Namespace.Type`)
- No Domain entity schemas exposed
- Transport DTOs separate from Application/Domain types

## Error contract

- `application/problem+json` for all errors
- `ApiProblemDetails`: ErrorCode, TraceId, Errors[]
- Status mapping: 400 validation, 401 unauthenticated, 403 forbidden,
  404 not found, 409 conflict, 412 precondition failed, 422 business rule,
  500 unexpected

## Header contracts

- `X-Correlation-Id`: optional request, always response
- `X-Workspace-Id`: required for workspace-scoped requests
- `Idempotency-Key`: required for idempotent commands (1-128 ASCII chars)
- `If-Match` / `ETag`: strong numeric version for concurrent resources

## Provider independence

- API source does not reference Npgsql or EF exception types
- Provider exception translation is Infrastructure responsibility
- Architecture test enforces this boundary

## OpenAPI artifact

- Export: `dotnet run --project src/Notrelix.API -- --export-openapi`
- Canonical path: `backend/contracts/openapi/notrelix.v1.json`
- CI drift gate: export + `git diff --exit-code`
- Deterministic: UTF-8 no BOM, no timestamp

## Frontend codegen

- `openapi-typescript` generates `schema.ts` from committed spec
- Fails when spec is missing (no silent skip)
- Generated types in `@notrelix/contracts/generated/rest`

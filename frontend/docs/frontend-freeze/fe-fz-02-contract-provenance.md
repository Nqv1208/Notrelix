# FE-FZ-02 Contract Provenance Status

## Status

`BLOCKED_BY_BACKEND_EXPORT`

Frontend generator hard-code has been replaced with deterministic traversal of the committed contract artifacts:

- `artifacts/contracts/openapi.v1.json`
- `artifacts/contracts/realtime.v1.json`

Generated files no longer include absolute machine paths, and `@notrelix/contracts` publicly exports generated REST and realtime types.

## Verified Backend Gap

The FE-FZ-02 plan requires a deterministic backend export command such as:

```bash
dotnet run --project backend/src/Notrelix.API -- --export-contracts artifacts/contracts
```

Current verification shows this mode is not implemented. Running the command builds the API and enters normal application startup, then fails before any contract artifact export.

Observed failure:

```text
Unhandled exception. System.InvalidOperationException: Redis connection string is missing
```

## Artifact Coverage Gap

The committed OpenAPI artifact is a small sample and does not match current backend route shape:

- Artifact paths omit `/api/v1`.
- Artifact has only `getBoardDetail` and `createBoardItem`.
- Backend route names include current operations such as `WorkManagement.Boards.GetOverview` and `WorkManagement.BoardItems.Move`.

Therefore FE-FZ-02 backend provenance, release-scope artifact coverage, and required production vertical-slice adoption cannot be honestly marked complete without adding or enabling backend contract export.

## Backend Issue

Detailed backend follow-up issue:

```text
frontend/docs/issues/fe-fz-02-backend-contract-export-provenance.md
```

## Phase Continuation Decision

The backend export gap remains open, but frontend work is allowed to continue under the explicit constraint that backend code is not changed in this phase.

Before final frontend freeze, backend must resolve `../issues/fe-fz-02-backend-contract-export-provenance.md`, then frontend must regenerate and verify contract artifacts from backend HEAD.

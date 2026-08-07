# FE-FZ-02 Backend Contract Export and Provenance Issue

## Status

`OPEN`

## Owner

Backend/API platform

## Phase

FE-FZ-02 — REST/realtime contract generation as source of truth

## Summary

The frontend codegen pipeline can now generate deterministic REST and realtime TypeScript contracts from the committed artifacts, but FE-FZ-02 cannot be completed because the backend does not currently provide a deterministic contract export command and the committed artifacts do not match the current backend route surface.

This blocks the freeze requirement that generated frontend transport contracts must be traceable to backend HEAD.

## Evidence

The FE-FZ-02 plan requires a backend command equivalent to:

```bash
dotnet run --project backend/src/Notrelix.API -- --export-contracts artifacts/contracts
```

Observed behavior when this command was executed:

```text
Using launch settings from backend/src/Notrelix.API/Properties/launchSettings.json...
Building...
Unhandled exception. System.InvalidOperationException: Redis connection string is missing
```

The command does not export contract artifacts. It enters normal API startup and fails during infrastructure registration.

## Artifact Mismatch

The committed REST artifact currently contains only a small sample:

```text
artifacts/contracts/openapi.v1.json
  getBoardDetail
  createBoardItem
```

It does not match backend route shape observed in source:

```text
backend routes use /api/v1/...
artifact paths omit /api/v1
backend has WorkManagement.Boards.GetOverview
backend has WorkManagement.BoardItems.Move
artifact has no MoveBoardItem operation
```

This means frontend code cannot honestly adopt generated operation types for required production vertical slices such as `GetBoard` or `MoveBoardItem` without either using stale/mismatched contracts or inventing frontend-only contract names.

## Required Backend Work

1. Add a deterministic contract export mode for `Notrelix.API`.
2. The export mode must not require Redis, Postgres, background workers, or normal web app startup dependencies.
3. Export REST OpenAPI to:

```text
artifacts/contracts/openapi.v1.json
```

4. Export realtime/AsyncAPI contract to:

```text
artifacts/contracts/realtime.v1.json
```

5. Ensure artifact output is deterministic:
   - No timestamps.
   - No local machine paths.
   - Stable ordering.
   - No local-only server URL.

6. Ensure REST operations include stable `operationId` values for release-scope public API, including at minimum the FE-FZ-02 vertical slices:

```text
WorkManagement_Boards_GetOverview or equivalent current GetBoard/full-board operation
WorkManagement_BoardItems_Move
```

7. Ensure operation paths match actual frontend transport paths, including `/api/v1` if that is the production route prefix.

## Acceptance Criteria

FE-FZ-02 backend provenance is complete only when all checks below pass:

```bash
dotnet run --project backend/src/Notrelix.API -- --export-contracts artifacts/contracts
cd frontend
pnpm codegen
git diff --exit-code -- ../artifacts/contracts
git diff --exit-code -- packages/foundation/contracts/src/generated
```

Expected result:

- Backend export command exits `0`.
- `artifacts/contracts/openapi.v1.json` reflects backend HEAD.
- `artifacts/contracts/realtime.v1.json` reflects backend HEAD.
- Frontend generated files are deterministic.
- Generated contracts include operations needed by frontend production vertical slices.

## Frontend State After FE-FZ-02 Frontend Work

Completed frontend-side work:

- REST generator traverses committed OpenAPI `paths` and `operationId` values instead of hard-coding sample endpoints.
- Realtime generator traverses committed AsyncAPI `components.messages` instead of hard-coding board item event names.
- Generated output no longer contains absolute machine paths.
- `@notrelix/contracts` exports generated REST/realtime types and subpaths.

Remaining blocker:

- Backend artifact provenance and release-scope coverage are not available without backend work.

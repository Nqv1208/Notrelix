# FE-FZ-00 Baseline

## Metadata

| Field | Value |
|-------|-------|
| Scope | `frontend-web-platform-v1.0.0` |
| Status | `NOT_FROZEN` |
| Commit | Captured by `frontend/scripts/freeze-audit.mjs` from `git rev-parse HEAD` |
| Plan | `plans/notrelix-frontend-web-platform-freeze-plan-v2.md` |

## Current Baseline Rules

- Web product platform scope is limited to `apps/web`, web runtime, web UI, foundation packages, web product packages, feature packages used by product web, and frontend tooling.
- Marketing has an independent lifecycle and is not frozen by the web platform certificate.
- Mobile parity is excluded from this certificate.
- No immutable freeze certificate is emitted during FE-FZ-00.
- `frontend/scripts/freeze-audit.mjs` records HEAD metadata and writes only `docs/frontend-freeze/last-audit-result.json` until FE-FZ-17.

## Open Status

Track A and Track B gates remain open. The final tag `frontend-web-platform-v1.0.0` must not be created until FE-FZ-00 through FE-FZ-17 pass on a clean working tree.

# FE-FZ-17 Issue: Freeze Certificate Blockers

## Scope

Frontend updated freeze audit governance:

- gate list no longer duplicates `validate`
- audit includes architecture, codegen, Storybook, web build, marketing build, production startup, and critical E2E gates
- dirty worktree prevents a freeze certificate
- audit records lockfile/OpenAPI/realtime spec hashes when files exist
- production startup uses a finite checker script instead of a long-running preview command

## Blockers

The frontend platform must remain `NOT_FROZEN` until these are resolved:

- FE-FZ-12 Automation backend REST/realtime contracts are missing.
- FE-FZ-14 Storybook/axe/visual/density gates are incomplete.
- FE-FZ-15 production telemetry transport and capture coverage are incomplete.
- FE-FZ-16 critical E2E fixtures/topology/coverage are incomplete.
- Worktree is currently dirty because freeze implementation work is still in progress.

## Acceptance Criteria

- All FE-FZ-00 through FE-FZ-17 gates pass.
- Worktree is clean.
- `frontend/.freeze/frontend-web-platform-v1.0.0-<shortSha>.json` is generated from HEAD metadata.
- Certificate status is `FROZEN` only when all gates pass.
- Git tag `frontend-web-platform-v1.0.0` is created only after user approval.

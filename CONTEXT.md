# CONTEXT.md — Current Repository Facts

This file is a compact snapshot of current repository shape. It is not a second
architecture handbook. Normative intent lives in `RULE.md`, `PRODUCT.md`,
`DESIGN.md`, `backend/docs/` and `frontend/docs/`.

## Backend

The backend solution is `backend/backend.slnx`.

Production projects:

- `src/Notrelix.Domain`
- `src/Notrelix.Application`
- `src/Notrelix.Infrastructure`
- `src/Notrelix.Platform`
- `src/Notrelix.API`

Test projects include Domain, Application, Infrastructure, Platform, API,
Integration, Architecture and shared Testing support projects.

Backend docs:

- `backend/README.md`
- `backend/AGENTS.md`
- `backend/docs/README.md`

## Frontend

The frontend workspace is `frontend/` and uses pnpm/Turborepo.

Hosts:

- `apps/web` for Vite web.
- `apps/mobile` for Expo mobile.
- `apps/marketing` for Next marketing.

Package families:

- `packages/foundation`
- `packages/runtimes`
- `packages/ui`
- `packages/product`
- `packages/features`
- `tooling`

Frontend docs:

- `frontend/README.md`
- `frontend/AGENTS.md`
- `frontend/docs/README.md`

Executable package authority:

- `frontend/tooling/dependency-rules/src/architecture-manifest.ts`
- `frontend/docs/generated/package-boundaries.md`

## Documentation Governance

Roadmaps, audits, freeze specs, migration trackers and old rule packs are not
active architecture authorities. Run:

```bash
make docs-check
```

after documentation, ADR, generated-evidence or reference-path changes.

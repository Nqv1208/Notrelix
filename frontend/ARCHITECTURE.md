# Notrelix Frontend Architecture

This file is a pointer to the canonical architecture documentation.

## Canonical Sources

- **Architecture**: `docs/client/architecture/`
- **Technical Structure**: `docs/notrelix-client-technical-project-structure.md`
- **Migration Tracker**: `docs/client/migration/tracker.md`
- **Dependency Rules**: `tooling/dependency-rules/`

## Quick Reference

```
apps/marketing  = Next.js App Router (SEO/public)
apps/web        = Vite + React + TanStack Router (product app)
apps/mobile     = Expo placeholder

packages/foundation  = contracts, kernel, platform, query, realtime, observability
packages/runtimes    = web, mobile adapters
packages/ui          = tokens, web (shadcn), mobile, icons
packages/product     = work-management, docs, automation
packages/features    = auth, workspace, account, billing, etc.
```

## Key Rules

- Packages must not import `next/*`
- `apps/web` must not import `next/*`
- Import via package exports, not deep paths
- No delete without Safe Delete Audit

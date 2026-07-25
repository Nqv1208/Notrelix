# Notrelix Frontend

Multi-client enterprise workspace UI for Notrelix.

## Architecture

```
frontend/
  apps/
    marketing/     Next.js App Router (SEO/public)
    web/           Vite + React + TanStack Router (product app)
    mobile/        Expo placeholder

  packages/
    foundation/    contracts, kernel, platform, query, realtime, observability
    runtimes/      web, mobile adapters
    ui/            tokens, web (shadcn), mobile, icons
    product/       work-management, docs, automation
    features/      auth, workspace, account, billing, etc.

  tooling/         eslint, tsconfig, dependency-rules, testing, codegen
```

## Tech Stack

| App | Framework | Router | Bundler |
|-----|-----------|--------|---------|
| marketing | Next.js 16 | App Router | Webpack |
| web | React 19 | TanStack Router | Vite |
| mobile | Expo | Expo Router | Metro |

## Quick Start

```bash
# Requirements: Node 22+, pnpm 10+
pnpm install
pnpm dev:web        # http://localhost:5173
pnpm dev:marketing  # http://localhost:3000
```

## Scripts

```bash
pnpm dev            # Run all apps
pnpm build          # Build all apps
pnpm typecheck      # Type check all packages
pnpm lint           # Lint all packages
pnpm test           # Run all tests
pnpm check:deps     # Check dependency boundaries
pnpm validate       # typecheck + lint + test + check:deps
```

## Rules

- Packages must not import `next/*` (except apps/marketing)
- `apps/web` must not import `next/*`
- `packages/*` must not read env directly
- Import via package exports, not deep paths
- No delete without Safe Delete Audit

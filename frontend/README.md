# Notrelix Frontend

The frontend is a pnpm/Turborepo workspace for the Notrelix enterprise
work-management platform.

## Hosts

- Vite web app: `apps/web`
- Expo mobile app: `apps/mobile`
- Next marketing app: `apps/marketing`

## Package Families

- `packages/foundation/*` for framework/runtime-neutral primitives and contracts.
- `packages/runtimes/*` for host runtime adapters.
- `packages/ui/*` for tokens and platform UI implementations.
- `packages/product/*/*` for product capability packages.
- `packages/features/*` for cross-product feature slices.
- `tooling/*` for generators, dependency rules and test infrastructure.

## Requirements

- Node `>=22`
- pnpm `>=10`

## Commands

```bash
pnpm install --frozen-lockfile
pnpm dev:web
pnpm dev:mobile
pnpm dev:marketing
pnpm build
pnpm typecheck
pnpm lint
pnpm test
pnpm validate:fast
pnpm validate
```

## Documentation

- [Frontend agent contract](AGENTS.md)
- [Frontend documentation index](docs/README.md)
- [Frontend overview](docs/architecture/frontend-overview.md)
- [Dependency boundaries](docs/architecture/dependency-boundaries.md)
- [Testing and quality gates](docs/architecture/testing-and-quality-gates.md)

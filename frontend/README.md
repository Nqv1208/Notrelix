# Notrelix Client Monorepo

> **Notrelix Enterprise Work-Management OS Frontend Platform**

---

## Architecture Documentation

For complete, authoritative frontend architecture documentation, refer to the [Client Architecture Specification](file:///Users/nqvinh/Documents/projects/Notrelix/frontend/docs/client-architecture/README.md).

### Quick Links

- [Dependency Model](file:///Users/nqvinh/Documents/projects/Notrelix/frontend/docs/client-architecture/dependency-model.md)
- [Application Composition](file:///Users/nqvinh/Documents/projects/Notrelix/frontend/docs/client-architecture/application-composition.md)
- [Module Template](file:///Users/nqvinh/Documents/projects/Notrelix/frontend/docs/client-architecture/module-template.md)
- [API & Contracts](file:///Users/nqvinh/Documents/projects/Notrelix/frontend/docs/client-architecture/api-and-contracts.md)
- [Realtime Architecture](file:///Users/nqvinh/Documents/projects/Notrelix/frontend/docs/client-architecture/realtime.md)
- [Freeze Governance](file:///Users/nqvinh/Documents/projects/Notrelix/frontend/docs/client-architecture/freeze-governance.md)

---

## Development Workflow

### Requirements

- Node.js >= 22
- pnpm >= 10

### Common Commands

```bash
pnpm install           # Install dependencies
pnpm typecheck         # Type-check all packages
pnpm lint              # Lint all packages
pnpm test              # Run unit & component tests
pnpm check:deps        # Run AST architecture checks
pnpm build             # Build web and marketing apps
pnpm validate          # Full local validation suite
```

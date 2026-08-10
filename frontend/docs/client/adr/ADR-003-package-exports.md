# ADR-003: Package Exports

**Date:** 2026-07-12
**Status:** Accepted

## Context

Packages need to expose public APIs without allowing deep imports into internals.

## Decision

Use `exports` field in `package.json` for each package.

Example:

```json
{
  "exports": {
    ".": "./src/index.ts",
    "./ui/button": "./src/components/ui/button.tsx"
  }
}
```

## Rules

- Import via package name: `import { Button } from '@notrelix/ui-web'`
- Or via subpath: `import { Button } from '@notrelix/ui-web/ui/button'`
- Never import deep paths like `../../packages/ui/web/src/components/ui/button`

## Consequences

- Clear public API boundary
- Internal refactoring doesn't break consumers
- TypeScript resolves correctly via package.json exports

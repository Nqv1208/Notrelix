# Notrelix Client — Dependency Boundary Matrix

**Version:** 4.0  
**Status:** Active  
**Enforcement:** `tooling/dependency-rules/src/check.mjs`

---

## Overview

This document defines the allowed and forbidden imports between packages in the Notrelix Client v4 architecture. These rules ensure:

1. **Clean separation of concerns** — Foundation packages have no business logic
2. **Platform independence** — Core packages work across web and mobile
3. **Feature isolation** — Features don't cross-contaminate
4. **Dependency direction** — Higher layers depend on lower layers, never reverse

---

## Layer Hierarchy

```txt
┌─────────────────────────────────────────────────────────┐
│  Apps Layer: apps/web, apps/marketing, apps/mobile      │
├─────────────────────────────────────────────────────────┤
│  Features Layer: packages/features/*                    │
├─────────────────────────────────────────────────────────┤
│  Product Layer: packages/product/{work-management,      │
│                 docs, automation}/*                     │
├─────────────────────────────────────────────────────────┤
│  UI Layer: packages/ui/{tokens, web, mobile, icons}     │
├─────────────────────────────────────────────────────────┤
│  Runtimes Layer: packages/runtimes/{web, mobile}        │
├─────────────────────────────────────────────────────────┤
│  Foundation Layer: packages/foundation/*                │
└─────────────────────────────────────────────────────────┘
```

---

## Allowed Imports Matrix

### Foundation Packages

| Package | Allowed Imports |
|:---|:---|
| `@notrelix/contracts` | _(none — pure types)_ |
| `@notrelix/kernel` | _(none — pure utilities)_ |
| `@notrelix/platform` | `@notrelix/kernel`, `@notrelix/contracts` |
| `@notrelix/query` | `@notrelix/kernel` |
| `@notrelix/realtime` | `@notrelix/kernel`, `@notrelix/contracts` |
| `@notrelix/observability` | `@notrelix/kernel` |

### Runtime Packages

| Package | Allowed Imports |
|:---|:---|
| `@notrelix/runtime-web` | `@notrelix/platform` |
| `@notrelix/runtime-mobile` | `@notrelix/platform` |

### UI Packages

| Package | Allowed Imports |
|:---|:---|
| `@notrelix/ui-tokens` | _(none — pure design tokens)_ |
| `@notrelix/ui-web` | `@notrelix/ui-tokens` |
| `@notrelix/ui-mobile` | `@notrelix/ui-tokens` |
| `@notrelix/ui-icons` | _(none — pure icons)_ |

### Product: Work Management

| Package | Allowed Imports |
|:---|:---|
| `@notrelix/wm-core` | `@notrelix/contracts`, `@notrelix/kernel` |
| `@notrelix/wm-state` | `@notrelix/wm-core`, `@notrelix/contracts`, `@notrelix/query`, `@notrelix/realtime`, `@notrelix/platform` |
| `@notrelix/wm-plugins` | `@notrelix/wm-core` |
| `@notrelix/wm-web` | `@notrelix/wm-core`, `@notrelix/wm-state`, `@notrelix/wm-plugins`, `@notrelix/ui-web`, `@notrelix/platform` |
| `@notrelix/wm-mobile` | `@notrelix/wm-core`, `@notrelix/wm-state`, `@notrelix/wm-plugins`, `@notrelix/ui-mobile`, `@notrelix/platform` |
| `@notrelix/wm-testing` | `@notrelix/wm-core`, `@notrelix/wm-state` |

### Product: Docs

| Package | Allowed Imports |
|:---|:---|
| `@notrelix/docs-core` | `@notrelix/contracts`, `@notrelix/kernel` |
| `@notrelix/docs-collaboration` | `@notrelix/docs-core`, `@notrelix/realtime` |
| `@notrelix/docs-web` | `@notrelix/docs-core`, `@notrelix/docs-collaboration`, `@notrelix/ui-web`, `@notrelix/platform` |
| `@notrelix/docs-mobile` | `@notrelix/docs-core`, `@notrelix/docs-collaboration`, `@notrelix/ui-mobile`, `@notrelix/platform` |

### Product: Automation

| Package | Allowed Imports |
|:---|:---|
| `@notrelix/automation-core` | `@notrelix/contracts`, `@notrelix/kernel` |
| `@notrelix/automation-web` | `@notrelix/automation-core`, `@notrelix/ui-web`, `@notrelix/platform` |
| `@notrelix/automation-mobile` | `@notrelix/automation-core`, `@notrelix/ui-mobile`, `@notrelix/platform` |

### Features (Standard Set)

All features share the same base allowed set:

```txt
@notrelix/contracts
@notrelix/kernel
@notrelix/platform
@notrelix/query
@notrelix/ui-web
@notrelix/ui-mobile
```

**Exceptions:**
- `@notrelix/feature-notifications` also allows `@notrelix/realtime`
- `@notrelix/feature-collaboration` also allows `@notrelix/realtime`

### Apps

| App | Allowed Imports |
|:---|:---|
| `@notrelix/app` | All packages (composition layer) |
| `@notrelix/marketing` | `@notrelix/ui-tokens`, `@notrelix/ui-web`, `@notrelix/ui-icons` |

---

## Forbidden Imports

These imports are **always forbidden** regardless of context:

| Package | Forbidden Imports |
|:---|:---|
| `@notrelix/contracts` | `react`, `react-dom`, `react-native` |
| `@notrelix/kernel` | `react`, `react-dom`, `react-native`, `@notrelix/platform`, `@notrelix/ui-*` |
| `@notrelix/ui-tokens` | `react`, `react-dom`, `react-native` |
| `@notrelix/wm-core` | `react`, `react-dom`, `@notrelix/ui-web`, `@notrelix/ui-mobile` |
| `@notrelix/wm-state` | `react`, `react-dom`, `@notrelix/ui-web`, `@notrelix/ui-mobile` |
| `@notrelix/wm-plugins` | `react`, `react-dom`, `@notrelix/ui-web`, `@notrelix/ui-mobile` |
| `@notrelix/docs-core` | `react`, `react-dom`, `@notrelix/ui-web`, `@notrelix/ui-mobile` |
| `@notrelix/automation-core` | `react`, `react-dom`, `@notrelix/ui-web`, `@notrelix/ui-mobile` |
| `@notrelix/ui-web` | `@notrelix/ui-mobile`, `react-native` |
| `@notrelix/ui-mobile` | `@notrelix/ui-web`, `@radix-ui`, `shadcn` |
| `@notrelix/wm-web` | `@notrelix/ui-mobile`, `@notrelix/runtime-mobile`, `react-native` |
| `@notrelix/wm-mobile` | `@notrelix/ui-web`, `@notrelix/runtime-web`, `@radix-ui`, `shadcn` |
| `@notrelix/runtime-web` | `react-native` |
| `@notrelix/runtime-mobile` | `@radix-ui`, `shadcn` |
| `@notrelix/marketing` | `@notrelix/wm-state`, `@notrelix/wm-core`, `@notrelix/realtime`, `@notrelix/platform` |

---

## Enforcement

### Automated Check

Run the boundary checker:

```bash
node tooling/dependency-rules/src/check.mjs
```

### Integration

Add to `package.json`:

```json
{
  "scripts": {
    "check:boundaries": "node tooling/dependency-rules/src/check.mjs"
  }
}
```

Run in CI:

```bash
pnpm check:boundaries
```

---

## Rationale

### Why These Rules?

1. **Foundation has no React** — Ensures platform independence and testability
2. **Core has no UI** — Separates business logic from presentation
3. **No cross-runtime imports** — Prevents web code leaking into mobile and vice versa
4. **Features are isolated** — Each feature is independently deployable
5. **Apps compose, not implement** — Apps only wire packages together

### What Happens If I Violate?

1. **Local check fails** — `pnpm check:boundaries` exits with error
2. **CI fails** — PR cannot merge until violation is fixed
3. **Architectural debt** — Cross-boundary imports create hidden dependencies

---

## Migration Notes

During migration from monolith to packages:

1. **Re-export barrels allowed** — Temporary re-exports from old locations are permitted
2. **Gradual migration** — Features can be moved incrementally
3. **No new violations** — Do not add new forbidden imports during migration
4. **Track in migration tracker** — Document all moves in `docs/client/migration/tracker.md`

---

## Questions?

See also:
- `docs/client/audits/current-frontend-audit.md` — Current state analysis
- `docs/client/migration/tracker.md` — Migration progress
- `docs/notrelix-client-v4-2-structure-for-coding-agents.md` — Full architecture spec

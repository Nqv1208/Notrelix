---
document_id: FE-ADR-006
document_type: architecture-decision
status: Accepted
owner: frontend-architecture
applies_to:
  - frontend-i18n
  - frontend-foundation
  - frontend-web
  - frontend-mobile
evidence:
  - frontend/packages/foundation/i18n/
  - frontend/tooling/dependency-rules/src/architecture-manifest.ts
  - frontend/tooling/dependency-rules/src/check-i18n-catalogs.ts
  - frontend/apps/web/src/providers/app-providers.tsx
  - frontend/docs/architecture/ui-and-design-system.md
  - docs/workstreams/executions/workspace-experience/workspace-experience.plan.md
review_on:
  - frontend-i18n-library-change
  - frontend-i18n-package-boundary-change
  - frontend-locale-set-change
  - decision-superseded
---

# FE-ADR-006: i18n library and package boundary

## ID

`FE-ADR-006`

## Status

Accepted

## Date

`2026-10-10`

## Owners

- `frontend-architecture`

## Context

The workspace rebuild (WX-DEC-008) requires the authenticated UI to be internationalized from the start, in Vietnamese and English. Before this decision the frontend had no i18n mechanism: copy was hard-coded per component, sometimes in Vietnamese (theme descriptions in `ui-web`) and sometimes in English (shell, settings).

Constraints:

- the mechanism is product-agnostic and must serve the web host now and the native host later, so it cannot depend on DOM, `react-dom` or browser storage (FE-AGENTS §12, FE-DEP-010);
- product copy belongs to the product/feature owner (FE-UI-069), so each package must own its own catalog;
- composition stays explicit; no global registry or service locator (FE-AGENTS §19);
- Vietnamese has no plural inflection while English has `one`/`other`, so plural handling must follow CLDR per locale;
- dates and numbers must follow the active locale;
- missing translations must be caught in CI, not at runtime.

A new foundation package and a new dependency edge are architecture changes (FE-DEP-051, FE-DEP-058), and the library choice is durable because every surface will depend on its API. WX-DEC-012 recorded the library choice at plan level on 2026-10-10; this ADR records the rationale and the integration rules.

## Decision

1. **Library.** Use `i18next` with `react-i18next` (pinned exact versions in the owning package).
2. **Owner.** A foundation package `@notrelix/i18n` at `packages/foundation/i18n` owns the mechanism. It is registered in the architecture manifest with `allowedInternalImports: []` and is forbidden from importing `react-dom`, `react-native`, Next.js or UI packages. It never reads `navigator` or browser storage: the host passes a storage port and the browser language list in.
3. **Locales.** `vi` and `en`. Detection order: stored preference → browser languages → `vi`. The stored key is `notrelix-locale`.
4. **Catalogs.** Each package owns its catalogs in files named `*.catalog.ts`, one namespace per catalog, declared with `defineCatalog(namespace, { vi, en })`. Plural keys use i18next CLDR suffixes (`_one`, `_other`, …). The host composes the catalogs it renders into `<I18nProvider catalogs={…}>`; two catalogs may not share a namespace.
5. **Runtime.** `I18nProvider` creates an isolated i18next instance (never the global singleton) with all resources loaded synchronously, so the first render is translated and tests do not share state. Public hooks: `useT(catalog)`, `useLocale()`, `useFormatters()`. Date, number, relative-time and list formatting use `Intl`, not an i18next plugin.
6. **Gate.** `pnpm check:i18n` (CI repository-integrity job and `validate:fast`) fails when a key is missing in either locale, a plural key lacks a CLDR form the locale needs, a message is empty, or a namespace is duplicated. A unit test runs the same check against the repository.
7. **UI primitives** in `@notrelix/ui-web` do not depend on `@notrelix/i18n`; they take translated copy through props (FE-UI-069). Granting a feature or product package the `@notrelix/i18n` edge is a manifest change made when that package first ships a catalog.

Unchanged: package export model (FE-ADR-003), framework split (FE-ADR-001), host composition.

## Alternatives Considered

### Alternative A — Lingui

**Description**

Lingui with message extraction (macros) and compiled ICU catalogs.

**Benefits**

- ICU MessageFormat, small runtime, compile-time extraction.

**Costs / risks**

- Requires a Babel/SWC macro step in every host build (Vite, Expo, Storybook) and in Vitest, which the current tooling does not have.
- Extraction-based catalogs move key ownership to a build step rather than to the owning package's source file.

**Why not chosen**

The build-integration cost across three hosts and the test runner outweighed the ICU benefit for a two-locale product; i18next works without a compile step.

### Alternative B — No library (Intl plus hand-rolled lookup)

**Description**

A small in-house `t()` over plain objects plus `Intl` formatters.

**Benefits**

- No dependency.

**Costs / risks**

- Re-implements interpolation, plural selection, fallback and React re-render on locale change.

**Why not chosen**

The features needed (plurals, interpolation, fallback, React binding) are exactly what i18next provides; owning them is not a Notrelix concern.

## Consequences

### Positive

- One mechanism for web and native; host-neutral package.
- Catalogs live with their owners; missing keys fail CI.
- Formatting follows the active locale through `Intl` with no extra dependency.

### Negative / trade-offs

- Two runtime dependencies (`i18next`, `react-i18next`) in the web bundle.
- Keys are untyped strings. The completeness gate compares locales with each other, so a key mistyped at a call site is not caught; it renders the key text instead.
- Each package that adopts i18n needs a manifest edge, reviewed per package.

### New obligations

- tests/gates: `pnpm check:i18n`; package tests in `packages/foundation/i18n/src/__tests__`.
- documentation: this ADR; `ui-and-design-system.md` §137 points copy owners here.
- ownership: `frontend-architecture` owns the package; each catalog's namespace is owned by its package.

## Compatibility / Migration

No persisted data or public contract changes. Existing hard-coded copy keeps working and is migrated surface by surface in later workspace-experience phases; Phase 1 translates only the theme and language pickers. The old `ui-web` Vietnamese theme descriptions were removed with the seven legacy accent themes (WX-FND-002).

## Evidence

### Canonical current architecture

- `frontend/docs/architecture/ui-and-design-system.md`
- `frontend/docs/architecture/dependency-boundaries.md`

### Source / manifests

- `frontend/packages/foundation/i18n/src/`
- `frontend/tooling/dependency-rules/src/architecture-manifest.ts` (`@notrelix/i18n`, `@notrelix/app-web` edge)
- `frontend/tooling/dependency-rules/src/forbidden-imports.ts`
- `frontend/apps/web/src/providers/app-providers.tsx`
- `frontend/apps/web/src/i18n/appearance.catalog.ts`

### Tests / gates

- `frontend/packages/foundation/i18n/src/__tests__/` (locale detection, catalog completeness, plurals, formatting, provider locale switch)
- `frontend/tooling/dependency-rules/src/__tests__/check-i18n-catalogs.unit.test.ts`
- `pnpm check:i18n`, `pnpm check:architecture`

### Contracts / migrations / generated evidence

- `frontend/docs/generated/package-boundaries.md`

## Supersedes

`None`

## Superseded By

`None`

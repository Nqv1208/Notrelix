---
document_id: FE-DEV-UI-MIGRATION-DEBT-REGISTER
document_type: execution-evidence
status: active
owner: frontend-platform
applies_to:
  - frontend-ui-construction-foundation
  - governed-web-ui-surfaces
source_execution:
  - .codex/executions/frontend-ui-construction-foundation-execution-v2/frontend-ui-construction.plan.md
source_work_unit:
  - FUIC-WU-097
review_on:
  - governed-web-ui-source-change
  - ui-evidence-manifest-change
---

# Governed Web UI migration debt register

Wave 9 critical-surface inventory rows that were not elevated to a registered
pure surface + manifest row in this execution. These are explicitly not counted
as completed critical coverage; each row records owner and the exact reason.

A row is recorded here only when:

- it is under a covered root (`frontend/packages/ui/web/src`,
  `frontend/packages/product/*/web/src`, `frontend/packages/features/*/src`);
- it was classified `candidate` (critical by rule) in
  `ui-critical-surface-inventory.md`; and
- its owning Wave 9 work unit did not produce deterministic scenario, collected
  story, manifest row, and evidence.

## Work Management remaining product UI

| owner                           | surface                       | source                                                                     | reason                                                                                              |
| ------------------------------- | ----------------------------- | -------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------- |
| `@notrelix/work-management-web` | Board layout shell/toolbar    | `packages/product/work-management/web/src/components/board-layout/*.tsx`   | owns query/view composition; extracting a pure layout surface requires a dedicated seam decision     |
| `@notrelix/work-management-web` | Board workspace composition   | `packages/product/work-management/web/src/components/board-workspace-view-content.tsx` | owns query/view composition at baseline; not yet split into a pure composition surface             |

## Workspace remaining feature UI

| owner                       | surface                      | source                                                                   | reason                                                                                              |
| --------------------------- | ---------------------------- | ------------------------------------------------------------------------ | --------------------------------------------------------------------------------------------------- |
| `@notrelix/features-workspace` | Workspace contextual toolbar | `packages/features/workspace/src/web/components/workspace-contextual-toolbar.tsx` | mostly-pure toolbar; not yet promoted to a standalone manifest row (covered surfaces do not yet claim it) |

## Account / Governance / Integrations

| owner                        | surface      | terminal reason                                                                                            |
| ---------------------------- | ------------ | ---------------------------------------------------------------------------------------------------------- |
| `@notrelix/features-account` | Account UI   | `NOT_APPLICABLE` — package has web hooks only at baseline; no UI component source under `src/web`.         |
| `@notrelix/features-governance` | Governance UI | `NOT_APPLICABLE` — package has web hooks only at baseline; no UI component source under `src/web`.         |
| `@notrelix/features-integrations` | Integrations UI | `NOT_APPLICABLE` — package has web hooks only at baseline; no UI component source under `src/web`.      |

## Follow-up contract

Each `candidate` row above becomes a legal completed critical surface only when
a future execution adds, in the owning package:

1. a named pure entry;
2. a deterministic scenario source;
3. an owner-local Storybook story with the required `fui-surface--*` /
   `fui-state--*` tags;
4. a `verification/ui-evidence.manifest.json` row binding the surface, its
   stories, and (when interaction is required) an owner-local
   `*.component.test.tsx`; and
5. green `check:ui-purity` + `check:ui-evidence` + guarded interaction coverage.

Until then these rows are debt, not D5 coverage.

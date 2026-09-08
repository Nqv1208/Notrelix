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

Manifest authority note (FUIR-WU-014): the active coverage authority is the
owner-local `verification/ui-evidence.manifest.json` and `check:ui-purity` +
`check:ui-evidence` + guarded interaction coverage. This register and
`ui-critical-surface-inventory.md` are historical documentation only; no table
in either document determines pass/fail. Each row here records the exact reason
a surface did not complete coverage at its owning execution.

A row is recorded here only when:

- it is under a covered root (`frontend/packages/ui/web/src`,
  `frontend/packages/product/*/web/src`, `frontend/packages/features/*/src`);
- it was classified `candidate` (critical by rule) in
  `ui-critical-surface-inventory.md`; and
- its owning work unit did not produce deterministic scenario, collected
  story, manifest row, and evidence.

## Resolved rows

The following Wave 9 candidate rows were later completed by the frontend
UI readiness closure execution (FUIR-WU-020/021/022) and removed from debt:

- `@notrelix/work-management-web` **Board layout shell/toolbar** — completed as
  `wm.board-layout.shell` / `wm.board-layout.toolbar` / `wm.board-layout.view-tabs`
  in the Work Management manifest; `<BoardToolbar onFilter … />` typed action
  added; covered by FUIR-TST-030.
- `@notrelix/work-management-web` **Board workspace composition** — completed as
  `wm.board-workspace.surface` pure seam (`board-workspace-surface.tsx`); the
  query/view composition stays in `board-workspace-view-content.tsx` (FUIR-WU-021).
- `@notrelix/features-workspace` **Workspace contextual toolbar** — completed as
  `workspace.contextual-toolbar` in the Workspace manifest with typed
  action/search contract (FUIR-WU-022).

## Account / Governance / Integrations

| owner                             | surface         | terminal reason                                                                                    |
| --------------------------------- | --------------- | -------------------------------------------------------------------------------------------------- |
| `@notrelix/features-account`      | Account UI      | `NOT_APPLICABLE` — package has web hooks only at baseline; no UI component source under `src/web`. |
| `@notrelix/features-governance`   | Governance UI   | `NOT_APPLICABLE` — package has web hooks only at baseline; no UI component source under `src/web`. |
| `@notrelix/features-integrations` | Integrations UI | `NOT_APPLICABLE` — package has web hooks only at baseline; no UI component source under `src/web`. |

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

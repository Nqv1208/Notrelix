# Gate checklists

These are the per-gate evidence checklists of the [delivery process](SKILL.md). The canonical Definition of Done is `docs/delivery/definition-of-done.md` (DEL-DONE-*), together with `docs/delivery/change-classification.md` and `docs/delivery/contract-first-delivery.md`, and AGENTS.md §31. Each checklist below only says at which gate, and with what evidence, those rules are proven. A box counts as ticked only when there is evidence for it: a file, a test name, a SHA, or a measured number.

## Brief (G1)

- [ ] Problem and users named, with real roles (owner, manager, member, commenter, viewer, guest) and the scope (account, workspace, board, item).
- [ ] Vocabulary from PRODUCT.md and the owning `docs/product/contexts/*.md`. No new synonyms.
- [ ] Every flow written step by step, including the unhappy paths: validation, conflict, lost permission, offline/network error, deleted or archived target, empty data, very large data.
- [ ] Permission matrix: each action against each role, as allowed, hidden or disabled with a reason.
- [ ] Every UI state listed per surface: empty, loading, ready, error, no permission, conflict, saving, read-only, 320 px, 768 px, 1280 px, 1440 px, dark.
- [ ] Acceptance criteria numbered AC-1…AC-n. Each one can be observed in a test.
- [ ] Out-of-scope list. Every open question answered and recorded in `decisions.md`.

## Design (G2)

- [ ] One artboard per state in the brief, on the single canvas, using the canvas tokens and Be Vietnam Pro.
- [ ] Rendered and measured: box numbers (x, y, w, h), type sizes and gaps in `design.md`.
- [ ] Contrast at least 4.5:1 for text and at least 3:1 for icons and essential borders, in light and dark.
- [ ] Touch targets of at least 44 px at phone width. No horizontal overflow at 320.
- [ ] Keyboard path and focus order are written down. No keyboard-hint chips. No top header bar on desktop or tablet.
- [ ] Design reviewer verdict is PASS on the current canvas version.

## Technical design (G3)

- [ ] Change classes per AGENTS.md §3 and the NRX rules that apply.
- [ ] The product owner and technical owner of each durable fact.
- [ ] API contract: routes, request and response shapes, error codes, and compatibility class (additive or breaking, plus the migration plan).
- [ ] Authorization at the Application layer plus RLS. Tenant scope for reads, lists, search, export, realtime and background jobs.
- [ ] Transaction owner, expected version, idempotency, and the events and realtime frames each write emits.
- [ ] Data: entities, migration that applies to an existing database with RLS policies, indexes, backfill.
- [ ] Front end: query keys scoped to workspace and resource, mutation rules (optimistic or not, rollback, conflict), realtime handling.
- [ ] Task split: each task has one role and an explicit file set, and no two tasks share a file. Tasks are ordered (contract first).

## Pull request (G4–G7)

- [ ] Implements only its task. Every changed line traces to an AC or the tech design.
- [ ] Tests that fail without the change: unit, integration (authorization and tenant), component, mock e2e as the task needs.
- [ ] Generated artifacts regenerated, never hand-edited. Mock backend serves the new routes.
- [ ] UI matches the approved artboard within 1 px (measured), in light and dark, at every width in the brief.
- [ ] Pre-push passed in full.
- [ ] Code reviewer APPROVE on this head SHA.
- [ ] QA PASS on this head SHA.
- [ ] Every CI check green on this head SHA.

## Feature close (G8)

- [ ] Every PR of the feature is merged. No open follow-ups in scope.
- [ ] QA ran every AC on develop on the dev stack and recorded the result.
- [ ] Canonical docs updated where the contract changed (AGENTS.md §18).
- [ ] Roadmap row set to `frozen` with the merge SHAs. Closing report delivered to the owner.

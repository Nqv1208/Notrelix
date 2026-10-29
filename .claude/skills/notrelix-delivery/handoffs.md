# Hand-off templates

These are the files the roles pass to each other in `docs/superpowers/features/<NN>-<slug>/`. Every section heading is required. If a section does not apply, write "None" and a one-line reason.

## brief.md (business analyst)

```markdown
# <Feature name>: brief
Owner context: <docs/product/contexts/...> · Scope: <account|workspace|board|item> · Roadmap row: <NN>

## Problem and outcome
## Users and roles
## Vocabulary
## Flows
### <Flow name>
1. ... (happy path)
- Unhappy: validation / conflict / lost permission / network / deleted target / empty / large data
## Permission matrix
| Action | Owner | Manager | Member | Commenter | Viewer | Guest |
## Surfaces and states
| Surface | States (empty, loading, ready, error, no-permission, conflict, saving, read-only) | Widths | Themes |
## Business rules
## Acceptance criteria
- AC-1 Given … when … then … (observable)
## Out of scope
## Open questions
```

## design.md (UX/UI designer)

```markdown
# <Feature>: design
Canvas version: <n> · Artboards: <names>
## State coverage (brief state → artboard)
## Measurements (element → x, y, w×h, font, colour token)
## Interaction notes (keyboard path, focus order, motion, reduced motion)
## Accessibility (contrast table light/dark, touch targets, aria roles)
```

## tech-design.md (solution architect)

```markdown
# <Feature>: technical design
Change classes: <A–J> · NRX rules: <ids>
## Ownership (fact → product owner → technical owner)
## Contract (route, request, response, errors, compatibility)
## Authorization and tenant (Application marker, RLS, async paths)
## Consistency (transaction owner, expected version, idempotency, events, realtime)
## Data (entities, migration on an existing RLS database, indexes, backfill, rollback)
## Front end (query keys, mutations, optimistic/rollback/conflict, realtime handling)
## Tasks
| # | Role | Files (exclusive) | Depends on | Done when |
## Risks
```

## Code review verdict (code reviewer → coordinator)

```markdown
PR #<n> · head <sha> · Verdict: APPROVE | CHANGES
## Findings (most severe first)
1. <file:line> — <defect> — <concrete failing case> — <smallest fix>
## Checked
brief ACs traced, tech design followed, NRX rules, tests fail-without-change, generated artifacts, migration on RLS DB
```

## QA report (QA engineer → coordinator)

```markdown
Head <sha> · Environment: mock | dev stack · Verdict: PASS | FAIL
| AC | Test (file › name) | Mock | Dev stack | Result |
## Matrix (role × action), a11y (axe serious/critical = 0), widths, dark
## Bugs (AC, steps, expected, actual, evidence path)
```

## Status / closing report (reporter → owner)

```markdown
Feature <NN> <name> · Gate <Gx> · <date>
Done since last report: … (PR, SHA)
In progress: … (role, gate)
Blocked / needs owner: …
Risks: …
```

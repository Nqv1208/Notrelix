---
document_id: FE-DECISIONS-INDEX
document_type: decision-registry
status: active
owner: frontend-architecture
applies_to:
  - frontend-decisions
  - frontend-architecture-history
  - frontend-adr-governance
evidence:
  - frontend/docs/architecture/
  - frontend/docs/decisions/FE-ADR-001-framework-split.md
  - frontend/docs/decisions/FE-ADR-002-package-manager.md
  - frontend/docs/decisions/FE-ADR-003-package-exports.md
  - frontend/docs/decisions/FE-ADR-004-no-next-in-packages.md
  - frontend/docs/decisions/FE-ADR-005-auth-session-model.md
review_on:
  - frontend-adr-created
  - frontend-adr-status-change
  - frontend-adr-supersession
  - frontend-architecture-foundation-change
  - frontend-decision-policy-change
---

# Frontend Architecture Decisions

> **Frontend ADRs preserve historical rationale for consequential frontend architecture choices.**
>
> Current frontend architecture lives under `../architecture/`. ADRs explain why durable choices were made; they are not the first place to learn how the frontend works today.

This registry is the canonical index for frontend Architecture Decision Records.

It owns:

- the frontend ADR namespace;
- the current ADR registry;
- status and supersession conventions;
- ADR admission criteria;
- normalization rules for legacy ADRs;
- relationship between ADRs and current architecture;
- relationship between ADRs and temporary exceptions/transitions;
- decision synchronization requirements.

It does not replace:

```text
../architecture/
```

as the current architecture owner.

---

# 1. Decision planes

Notrelix separates decision history by scope.

```text
System-wide decisions
→ docs/decisions/SYS-ADR-*.md

Backend decisions
→ backend/docs/decisions/ADR-*.md

Frontend decisions
→ frontend/docs/decisions/FE-ADR-*.md
```

The namespaces are intentionally independent.

---

# 2. FE-DEC-001 — Frontend ADR IDs use `FE-ADR-NNN`

Frontend decision IDs MUST use:

```text
FE-ADR-001
FE-ADR-002
...
```

IDs are immutable once assigned.

Renaming a file does not change the ADR ID.

---

# 3. Current registry

Current frontend ADRs are:

| ID | Decision | Status | Historical date | Current architecture owner |
|---|---|---|---|---|
| `FE-ADR-001` | Framework split | Accepted | 2026-07-12 | `../architecture/frontend-overview.md`, `../architecture/hosts-composition-routing.md` |
| `FE-ADR-002` | Package manager | Accepted | 2026-07-12 | `../architecture/dependency-boundaries.md`, frontend workspace/tooling configuration |
| `FE-ADR-003` | Package exports | Accepted | 2026-07-12 | `../architecture/dependency-boundaries.md` |
| `FE-ADR-004` | No Next.js in reusable packages | Accepted | 2026-07-12 | `../architecture/dependency-boundaries.md`, `../architecture/hosts-composition-routing.md` |
| `FE-ADR-005` | Auth session model | Accepted | 2026-07-09 | `../architecture/hosts-composition-routing.md`, `../architecture/api-and-contracts.md` |

The status/date values above are recovered directly from the current legacy ADR documents.

---

# 4. FE-DEC-002 — The registry reflects ADR status; it does not invent status

Allowed statuses are:

```text
Proposed
Accepted
Superseded
Rejected
Deprecated
```

Do not mark an ADR `Accepted` merely because source currently resembles it.

Status is part of the decision record.

---

# 5. Current architecture versus historical decision

Use:

```text
../architecture/
```

to answer:

```text
How should the frontend work now?
Where should code live?
What is the current dependency/state/runtime/UI rule?
```

Use an ADR to answer:

```text
Why was this consequential choice made?
What historical context existed?
What decision was accepted?
What consequences were understood?
What later decision superseded it?
```

---

# 6. FE-DEC-003 — ADR is rationale history, not current operating manual

An Accepted ADR can contain:

```text
historical file names
historical versions
historical examples
historical rollout details
```

that are no longer current evidence.

Current architecture/source remains separately documented.

Do not treat every historical implementation detail as an eternal invariant.

---

# 7. Decision admission

Create an FE ADR when the change is a durable, consequential frontend architecture choice.

Typical triggers include:

```text
host framework split
new application host
package-manager foundation
package export foundation
new architecture layer
package graph foundation
auth/session foundation
state-authority foundation
realtime ordering/recovery foundation
design-system theme/platform foundation
microfrontend/runtime federation
critical test/gate foundation
```

Use `../architecture/architecture-change-policy.md` for classification.

---

# 8. FE-DEC-004 — Routine feature work does not require an ADR

Do not create ADRs for:

```text
new screen
new query
new mutation
new product component
routine package-local refactor
test addition
bug fix
```

when current architecture already determines the correct solution.

Important decisions should remain discoverable.

---

# 9. Durable-decision test

A useful ADR test is:

```text
Would reversing this choice later require broad migration,
change many package/host relationships,
or materially change frontend architecture?
```

If yes, ADR is likely appropriate.

If no, a feature spec/code review may be enough.

---

# 10. FE-DEC-005 — ADR depth follows decision cost

A framework split deserves deeper rationale/compatibility analysis than a local implementation choice.

Do not force every ADR to the same length.

Do not omit important consequences merely to keep ADRs short.

---

# 11. Required ADR schema

New frontend ADRs MUST contain:

```text
ID
Status
Date
Owners
Context
Decision
Alternatives Considered
Consequences
Compatibility / Migration
Evidence
Supersedes
Superseded By
```

Repository ADR metadata/front matter should also be present according to documentation governance.

---

# 12. FE-DEC-006 — New ADRs use the full repository schema

Legacy ADRs may predate this schema.

New ADRs do not.

Use:

```text
docs/templates/adr-template.md
```

as the structural baseline.

---

# 13. Legacy ADR normalization

Existing FE-ADR-001…005 were written before the complete documentation architecture.

During normalization:

```text
preserve original decision meaning
preserve original status/date
add structural fields where safe
refresh current evidence separately
state unknown historical data explicitly
```

---

# 14. FE-DEC-007 — Normalization is not historical rewriting

Normalization MUST NOT:

```text
invent alternatives
invent original owners/authors
invent rationale
change accepted meaning
erase inconvenient historical details
silently update old decision to match today's source
```

---

# 15. Missing historical owner

If original ADR did not record decision owner/authorship:

```text
Historical decision owner/authorship:
Not recorded explicitly in the original ADR.
```

Current stewardship can still be assigned in document metadata.

---

# 16. FE-DEC-008 — Current stewardship is distinct from historical authorship

Example:

```text
owner: frontend-architecture
```

means the current team maintains the ADR record.

It does not mean that team/person authored the original 2026 decision.

---

# 17. Missing alternatives

If the original record contains no recoverable alternatives:

```text
Other alternatives:
Not recorded in the original ADR.
```

Do not fabricate an alternatives section such as:

```text
"Use Angular"
"Use Flutter"
"Use Remix"
```

merely because they seem plausible.

---

# 18. FE-DEC-009 — Alternatives must be historically recoverable or current-decision real

For legacy normalization:

```text
only record alternatives evidenced by original text/history
```

For a new ADR:

```text
record alternatives genuinely considered during the decision
```

---

# 19. Consequences

Preserve historical consequences.

You MAY add current evidence showing whether the accepted decision remains implemented.

Do not rewrite historical consequences into today's architecture prose.

---

# 20. FE-DEC-010 — Historical consequence and current evidence are labeled separately

Example:

```text
Historical consequence:
mobile can share packages with web.

Current evidence:
mobile imports native-safe product/runtime packages through the closed-world manifest.
```

The second does not replace the first.

---

# 21. Evidence

ADR evidence can evolve.

Useful current evidence includes:

```text
package manifests
architecture manifest
source composition
tests
CI
generated package-boundary docs
```

Evidence demonstrates the decision remains implemented.

---

# 22. FE-DEC-011 — Evidence may evolve without a new ADR

If:

```text
Vite version changes
Expo version changes
source file moves
test name changes
```

but the accepted architecture decision remains the same, update evidence/current architecture as appropriate.

No new ADR is required merely for evidence refresh.

---

# 23. Decision change

A new ADR is required when the accepted decision itself changes materially.

Examples:

```text
web product app moves from Vite SPA to Next.js
marketing and product web collapse into one framework/runtime
mobile switches from native Expo host to web wrapper as architecture
Next.js becomes approved inside reusable packages
auth session foundation changes from HttpOnly cookies to JS-managed bearer tokens
```

---

# 24. FE-DEC-012 — Accepted ADR is superseded, not silently edited

When a decision changes:

```text
old ADR
Status: Superseded
Superseded By: FE-ADR-NNN

new ADR
Status: Accepted
Supersedes: FE-ADR-OLD
```

Preserve old context/decision.

---

# 25. Supersession direction

Supersession explains decision history.

It is not the same as file replacement.

An old ADR stays in the registry as history.

---

# 26. FE-DEC-013 — Superseded ADR remains discoverable

Do not delete a superseded ADR solely because the current architecture no longer follows it.

The record explains why old source/history existed.

---

# 27. Rejected ADR

A rejected ADR records a real considered proposal that was not adopted.

Do not create rejected ADRs for random ideas never seriously considered.

---

# 28. FE-DEC-014 — Rejected status preserves decision context, not backlog

A rejected ADR should explain:

```text
proposal
reason it was not accepted
```

It is not a feature wishlist.

---

# 29. Deprecated ADR

`Deprecated` can be used when a decision is no longer applicable and has no direct replacement, according to repository decision governance.

Use sparingly.

---

# 30. FE-DEC-015 — Deprecated is not a substitute for Superseded

If a new decision replaced the old one:

```text
Superseded
```

is more truthful.

Use `Deprecated` when the decision simply ceased to apply.

---

# 31. Proposed ADR

A Proposed ADR is under review.

It MUST NOT be cited as already accepted architecture.

---

# 32. FE-DEC-016 — Proposed decision does not authorize source drift

Do not implement a broad architecture change as permanent source solely because an ADR draft exists.

Follow the change/migration approval process.

---

# 33. Decision versus exception

An architecture exception is temporary permission.

An ADR records a durable architecture decision.

They are not interchangeable.

---

# 34. FE-DEC-017 — Temporary debt does not become an ADR merely to legitimize it

If one package temporarily violates an otherwise accepted rule:

```text
use bounded exception/transition governance
```

rather than a new ADR saying the violation is architecture.

---

# 35. Decision versus migration

ADR:

```text
why the target architecture is chosen
```

Migration plan:

```text
how old source moves to the target
```

These should not be conflated.

---

# 36. FE-DEC-018 — ADR does not replace migration plan

Consequential changes with staged compatibility SHOULD have a separate migration plan when required.

The ADR can summarize the compatibility direction.

---

# 37. Decision versus generated facts

Example:

```text
FE-ADR-001
→ why three hosts/frameworks were chosen

frontend-overview.md
→ current three-host architecture semantics

architecture-manifest.ts
→ exact currently governed package graph

package-boundaries.md
→ generated readable graph

package.json files
→ actual current framework dependencies
```

Each has a distinct role.

---

# 38. FE-DEC-019 — ADR does not duplicate volatile package inventory

Do not maintain exact current package dependency tables inside ADRs unless the exact dependency is itself the decision.

Use generated/source evidence.

---

# 39. Decision synchronization

When a new Accepted FE ADR changes current architecture, update all affected current owners.

Potential synchronization set:

```text
frontend/docs/architecture/*
frontend/tooling/dependency-rules/src/architecture-manifest.ts
frontend/**/package.json
frontend source
frontend tests
frontend/docs/generated/package-boundaries.md
frontend/package.json scripts
.github/workflows/fe-ci.yml
backend/system contracts when cross-boundary
```

as applicable.

---

# 40. FE-DEC-020 — Accepted decision and current architecture move together

Do not land:

```text
ADR accepted today
architecture/source migration "later"
```

without an explicit transition plan defining the temporary state.

---

# 41. Registry synchronization

When creating or changing an ADR:

```text
update this registry
```

with:

```text
ID
title
status
date
current architecture owner
supersession relation
```

as applicable.

---

# 42. FE-DEC-021 — Registry is complete for active frontend ADR files

An ADR file MUST NOT exist unindexed indefinitely.

A registry entry MUST NOT point to a missing ADR.

Documentation gates should detect reliable inventory/link drift.

---

# 43. ADR filenames

Preferred file pattern:

```text
FE-ADR-NNN-short-decision-name.md
```

ID is more important than exact slug.

Do not change ID when renaming slug for clarity.

---

# 44. FE-DEC-022 — ADR ID uniqueness is directory-wide

Before assigning a new FE ADR ID:

```text
inspect current registry/files
inspect concurrent branch/PR activity when relevant
```

Do not blindly assume the next number from memory.

---

# 45. Next available ID

With the current registry:

```text
FE-ADR-001 ... FE-ADR-005
```

the next normally available ID is:

```text
FE-ADR-006
```

but it MUST be rechecked at creation time for concurrent changes.

---

# 46. FE-DEC-023 — Do not reserve FE-ADR-006 speculatively

Documentation MAY say:

```text
next normally available ID: FE-ADR-006
```

but MUST NOT create placeholder files/decisions without a real decision.

---

# 47. Framework split decision ownership

`FE-ADR-001` explains why Notrelix chose separate:

```text
marketing
web
mobile
```

hosts/frameworks.

Current implementation still reflects that split.

---

# 48. FE-DEC-024 — FE-ADR-001 identity is the host/framework separation decision

Its identity is not:

```text
one specific Vite minor version
one Next minor version
one current route library version
```

Those can evolve while the decision remains accepted.

---

# 49. Package-manager decision ownership

`FE-ADR-002` records pnpm as the frontend package-manager foundation.

Historical version detail is evidence/context of the decision.

The durable decision identity is:

```text
pnpm
single workspace lockfile model
workspace usage
```

unless a superseding ADR changes it.

---

# 50. FE-DEC-025 — Tool version update does not automatically supersede package-manager ADR

Updating from one pnpm 10.x release to another does not necessarily change the decision.

Switching package manager likely does.

---

# 51. Package-export decision ownership

`FE-ADR-003` records public package exports as the cross-package API boundary.

Current dependency architecture carries that decision forward.

---

# 52. FE-DEC-026 — New supported subpath can be current contract evolution without new ADR

Routine export additions under the same package-export model do not require a new ADR.

Changing to unrestricted deep imports or a fundamentally different package API model would.

---

# 53. No-Next decision ownership

`FE-ADR-004` records the framework boundary keeping Next.js out of reusable packages and non-marketing hosts.

Current package/architecture enforcement remains evidence.

---

# 54. FE-DEC-027 — FE-ADR-004 is about framework contamination, not banning all web-specific code

`ui-web` and `runtime-web` may legitimately be web-specific.

The decision prevents Next.js application-framework coupling from spreading into reusable package architecture.

---

# 55. Auth-session decision ownership

`FE-ADR-005` records the cookie-based browser auth/session foundation and decoupled navigation/client boundaries.

Some historical implementation details in the current legacy ADR require normalization against present contract evidence.

---

# 56. FE-DEC-028 — Accepted status does not freeze stale protocol spelling

If an Accepted auth ADR contains a historical header/file name that conflicts with current producer contract:

```text
preserve the historical record
label current drift/evidence
repair source/contract according to authority
```

Do not silently rewrite history.

Do not treat stale spelling as current authority merely because the ADR is Accepted.

---

# 57. Current CSRF caution

Current frontend architecture review has identified a frontend/backend CSRF naming mismatch.

That issue belongs to:

```text
current API/security contract reconciliation
```

not to retroactive rewriting of `FE-ADR-005`.

When FE-ADR-005 is normalized, historical text must remain truthful and current evidence must be distinguished.

---

# 58. FE-DEC-029 — Historical implementation defects can coexist with an accepted architectural decision

An architecture decision may be sound while one source implementation/detail is wrong.

Repair source debt without superseding the decision unless the architecture itself changes.

---

# 59. Current architecture routing

For current frontend behavior read:

```text
../architecture/frontend-overview.md
../architecture/dependency-boundaries.md
../architecture/hosts-composition-routing.md
../architecture/api-and-contracts.md
../architecture/state-query-mutations.md
../architecture/realtime.md
../architecture/ui-and-design-system.md
../architecture/testing-and-quality-gates.md
../architecture/architecture-change-policy.md
```

---

# 60. FE-DEC-030 — New contributors read architecture before ADR history

Default onboarding order:

```text
current architecture
→ relevant ADR only when rationale/history matters
```

This prevents old implementation details from being mistaken for current instructions.

---

# 61. Evidence quality

Decision evidence SHOULD point to stable current proof such as:

```text
app package manifests
architecture manifest
host source
architecture tests
CI
```

Avoid relying on screenshots or old audit reports when executable evidence exists.

---

# 62. FE-DEC-031 — Evidence claim is no stronger than its source

Examples:

```text
package.json
→ proves declared framework dependency

architecture manifest
→ proves allowed internal package graph

build CI
→ proves packaging for exact revision

ADR
→ proves historical decision
```

Do not use one as proof of a different property.

---

# 63. Decision review trigger

Review an ADR when:

```text
its status changes
a superseding decision is proposed
current architecture no longer appears consistent
a historical ambiguity affects a new decision
```

Review does not mean rewriting accepted history.

---

# 64. FE-DEC-032 — Periodic evidence refresh does not reopen accepted decision automatically

An Accepted decision remains Accepted until architecture governance intentionally changes/retires it.

Do not churn ADR status because source file paths moved.

---

# 65. Historical date

Preserve the original recorded Date where present.

Do not replace it with normalization date.

---

# 66. FE-DEC-033 — Normalization date is not decision date

If metadata needs a documentation-updated timestamp in future, keep it separate.

The `Date` field remains the historical decision date.

---

# 67. Owners

For normalized legacy ADRs:

```text
Current stewardship
→ frontend-architecture

Historical decision owner/authorship
→ preserve recorded value, or state not recorded
```

Do not infer from Git account/name unless governance explicitly treats commit author as decision owner.

---

# 68. FE-DEC-034 — Git author is not automatically architecture decision owner

Commit history can recover:

```text
date
text
change chronology
```

but not necessarily group decision ownership.

Do not conflate authorship with decision authority.

---

# 69. Compatibility

Legacy ADRs may not contain a migration section.

Normalization should describe compatibility only when safely inferable from the accepted decision/current evidence.

Unknown historical migration detail remains unknown.

---

# 70. FE-DEC-035 — Compatibility section may distinguish historical record from current implications

Example:

```text
Historical migration plan:
Not recorded.

Current compatibility implication:
three host apps retain separate framework/build boundaries.
```

This adds useful structure without inventing history.

---

# 71. Architecture change checklist

Before creating a new FE ADR:

```text
[ ] current architecture cannot safely absorb change
[ ] durable foundation is changing
[ ] scope is frontend or system?
[ ] real alternatives considered
[ ] compatibility/migration known
[ ] current owners identified
[ ] proof strategy identified
[ ] next FE ADR ID rechecked
```

---

# 72. Legacy normalization checklist

```text
[ ] original text read
[ ] original date/status preserved
[ ] original decision preserved
[ ] historical owner not invented
[ ] alternatives not invented
[ ] current architecture owner linked
[ ] current evidence refreshed
[ ] Supersedes/Superseded By truthful
[ ] historical/current details labeled
```

---

# 73. Supersession checklist

```text
[ ] new ADR exists
[ ] old status changed to Superseded
[ ] old points to new
[ ] new points to old
[ ] architecture updated
[ ] source/manifest migrated
[ ] tests/generated evidence updated
[ ] transition/removal complete or tracked
```

---

# 74. Stop conditions

Stop ADR work if:

- an Accepted ADR is being silently rewritten to fit today's source;
- original owner/rationale/alternatives are being guessed;
- a routine feature choice is being promoted into an ADR for ceremony;
- a temporary exception is being converted into permanent architecture without a real decision;
- a generated package matrix is being pasted into an ADR;
- ADR status is inferred only from source similarity;
- a new FE ADR ID is assigned without checking current registry;
- a superseded ADR is being deleted;
- current architecture is being explained only by old ADRs;
- historical protocol/file names are being treated as current authority without current evidence;
- a frontend ADR attempts to approve a backend/system contract outside frontend authority.

---

# 75. Final decision model

Use the frontend decision plane as:

```text
CURRENT ARCHITECTURE
../architecture/
        ↑
        │ implemented because of / constrained by
        │
DECISION HISTORY
FE-ADR-*.md
        │
        ↓
current source / manifest / tests / generated evidence
```

The ADR plane succeeds when a future engineer can understand:

```text
what was decided
when it was decided
why it was consequential
what was known then
what decision replaced it, if any
```

without confusing historical evidence with current architecture or allowing architecture to change through silent source drift.

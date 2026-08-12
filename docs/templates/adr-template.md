---
document_id: TEMPLATE-ADR
document_type: template
status: active
owner: documentation-governance
applies_to:
  - docs/decisions
  - backend/docs/decisions
  - frontend/docs/decisions
evidence:
  - docs/decisions/README.md
  - docs/governance/decision-and-exception-policy.md
  - docs/governance/documentation-lifecycle.md
review_on:
  - adr-format-change
  - decision-status-change
  - decision-lifecycle-change
  - documentation-metadata-change
---

# ADR Template

Use this template for consequential durable architecture decisions.

Choose exactly one namespace:

```text
System:
SYS-ADR-NNN

Backend:
ADR-NNN

Frontend:
FE-ADR-NNN
```

Do not use this template for:

- routine implementation;
- temporary exception;
- feature task checklist;
- migration execution tracker;
- incident timeline.

Before creating the ADR, read:

```text
docs/governance/decision-and-exception-policy.md
docs/decisions/README.md
```

---

# Usage rules

1. Assign the next unused ID in the correct scope registry.
2. Keep the ID immutable.
3. Use one allowed status:
   - `Proposed`
   - `Accepted`
   - `Superseded`
   - `Rejected`
   - `Deprecated`
4. Record stable logical owners, not temporary personnel names.
5. Explain a real architecture choice, not generic principles.
6. Record serious alternatives fairly.
7. Include negative consequences.
8. State compatibility/migration implications at architecture level.
9. Link current architecture and executable evidence.
10. Never silently rewrite an Accepted ADR when the decision changes; create a superseding ADR.

---

# Copy from here

```markdown
---
document_id: <SYS-ADR-NNN | ADR-NNN | FE-ADR-NNN>
document_type: architecture-decision
status: <Proposed | Accepted | Superseded | Rejected | Deprecated>
owner: <logical-owner>
applies_to:
  - <system/backend/frontend scope>
evidence:
  - <canonical architecture document>
  - <source/test/manifest/contract evidence>
review_on:
  - decision-superseded
---

# <ID>: <Decision title>

## ID

`<ID>`

## Status

<Proposed | Accepted | Superseded | Rejected | Deprecated>

## Date

`YYYY-MM-DD`

## Owners

- `<logical-owner>`
- `<additional logical owner if genuinely shared>`

## Context

Describe the architectural problem and the constraints that make a durable decision necessary.

Include only context needed to understand the choice.

Answer where relevant:

- What current behavior/boundary exists?
- What is failing, limiting, or becoming ambiguous?
- Which product/system contracts constrain the decision?
- Which deployment/compatibility/security constraints matter?
- Why is this consequential enough for an ADR?

Do not turn Context into a full architecture handbook.

## Decision

State the chosen architecture precisely.

A reviewer should be able to answer:

- What will be true after this decision?
- Which boundary/contract/technology/ownership rule changes?
- Which choices are explicitly ruled out?
- What remains unchanged?

Use normative language where it improves precision.

## Alternatives Considered

### Alternative A — <name>

**Description**

<What this option would do.>

**Benefits**

- ...

**Costs / risks**

- ...

**Why not chosen**

<Material reason.>

### Alternative B — <name>

**Description**

...

**Benefits**

- ...

**Costs / risks**

- ...

**Why not chosen**

...

Add only serious alternatives that were actually plausible.

Do not add strawman options.

## Consequences

### Positive

- ...

### Negative / trade-offs

- ...

### New obligations

- tests/gates:
- operations:
- security:
- performance:
- documentation:
- ownership:

Record the real cost of the choice.

## Compatibility / Migration

State durable migration consequences.

Address as applicable:

- old/new version coexistence;
- persisted data/schema;
- event/message backlog;
- mobile/external consumers;
- generated contracts;
- provider state;
- rollout sequencing;
- old-path retirement.

If no migration is required, state why.

Detailed task-by-task execution belongs in Delivery plans, not this ADR.

## Evidence

### Canonical current architecture

- `<path>`

### Source / manifests

- `<path>`

### Tests / gates

- `<path or test/gate name>`

### Contracts / migrations / generated evidence

- `<path>`

Evidence may be incomplete while Status is Proposed.

For Accepted decisions, identify the evidence expected to prove implementation/current state.

## Supersedes

`None`

or:

- `<OLD-ADR-ID>` — <title>

## Superseded By

`None`

or:

- `<NEW-ADR-ID>` — <title>

## Notes

Optional factual clarification only.

Do not use Notes to introduce a new architecture decision without supersession.
```

---

# Scope selection

Use **System** ADR when the choice genuinely spans repository authority planes.

Typical examples:

```text
context/service extraction model
system-wide external contract strategy
cross-stack tenancy architecture
repository-wide event/versioning model
```

Use **Backend** ADR when the choice is specifically backend architecture.

Typical examples:

```text
Application pipeline boundary
RLS bootstrap mechanism
CSRF host architecture
rate-limiting implementation architecture
```

Use **Frontend** ADR when the choice is specifically frontend architecture.

Typical examples:

```text
host framework split
package manager
public package exports
frontend session model
```

Do not choose System merely because Backend and Frontend both need to know that the other side exists.

---

# Context quality test

A good Context section answers:

```text
What problem exists?
What constraints are durable?
Why can normal canonical rules not determine the solution already?
```

A weak Context section looks like:

```text
We need scalability.
Microservices are popular.
The team prefers X.
```

---

# Decision quality test

A good Decision is concrete:

```text
Public integration events use stable logical event identities and are mapped
from internal Domain events; Domain CLR type names are not public event names.
```

A weak Decision:

```text
Use event-driven architecture carefully.
```

---

# Alternative quality test

A useful alternative was plausible under the same constraints.

For each alternative, describe enough that a future reader understands why it lost.

Do not write:

```text
Alternative: do nothing.
Rejected because bad.
```

unless doing nothing was genuinely the principal alternative and the reason is concrete.

---

# Consequence quality test

Every accepted decision should expose at least one material cost, unless the choice is truly trivial—in which case it may not need an ADR.

Examples:

```text
additional migration stage
new operational failure mode
more complex compatibility window
higher test burden
new infrastructure dependency
reduced implementation freedom
```

---

# Compatibility / Migration quality test

The section should answer durable questions, not copy an issue checklist.

Good:

```text
The new event contract must coexist with v1 until all supported mobile and
background consumers no longer read the old event shape. Old messages in
replay storage remain readable until the v1 retention horizon passes.
```

Too execution-specific:

```text
Tuesday:
- Alice updates file X.
Wednesday:
- deploy job Y.
```

The latter belongs in a migration/release plan.

---

# Evidence quality test

Evidence should map the decision to current reality.

Prefer:

```text
canonical architecture doc
source manifest/config
architecture/integration test
contract/schema
migration
generated evidence
CI gate
```

Avoid:

```text
Slack discussion
memory
screenshot with no source
private note
```

unless retained only as non-authoritative background.

---

# Supersession procedure

When the decision changes materially:

```text
old ADR:
Status → Superseded
Superseded By → NEW-ID

new ADR:
Status → Accepted
Supersedes → OLD-ID
```

Then update:

```text
current architecture docs
source
tests/gates
contracts/migrations
registry
```

Do not replace the old file contents with the new decision.

---

# Accepted ADR maintenance

Allowed:

```text
fix broken link
fix spelling
fix formatting
add supersession metadata
add evidence reference
```

Not allowed without new ADR:

```text
change chosen architecture
change rejected alternatives to fit hindsight
remove old negative consequences
change historical rationale
```

---

# Rejected ADR

A Rejected ADR should still explain:

```text
proposal
reason rejected
important alternatives/context
```

if preserving it is useful.

Do not implement it unless a later accepted ADR changes the decision.

---

# Deprecated ADR

Use only when the decision is intentionally retired and there is no direct replacement that warrants a superseding ADR.

If a new choice replaces the old choice, prefer:

```text
Superseded
```

---

# ADR review checklist

Before marking Accepted:

```text
[ ] correct scope namespace
[ ] unique immutable ID
[ ] allowed status
[ ] date
[ ] logical owner
[ ] problem/constraints clear
[ ] precise decision
[ ] serious alternatives
[ ] negative consequences
[ ] compatibility/migration
[ ] security/tenant implications
[ ] operations implications
[ ] current-doc update identified
[ ] evidence identified
[ ] supersession links correct
[ ] registry update
```

---

# Common failure modes

Do not:

```text
create an ADR for every feature
use ADR as current architecture README
copy entire system design into Context
invent alternatives after the fact
record only benefits
hide migration consequences
reuse old IDs
renumber Backend/Frontend history
rewrite Accepted decision
use exception as permanent ADR
use Proposed ADR as current authority
use team staffing as architecture rationale
```

---

# Minimal completion rule

An ADR is ready for review when a reader unfamiliar with the discussion can answer:

```text
What was the consequential choice?
Why did it need a decision?
What did we choose?
What credible alternatives existed?
What did the choice cost?
What migration/compatibility follows?
What current architecture/evidence will implement it?
What decision, if any, does it supersede?
```

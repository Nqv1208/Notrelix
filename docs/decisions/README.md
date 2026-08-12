---
document_id: DECISIONS-INDEX
document_type: decision-registry
status: active
owner: architecture
applies_to:
  - repository
  - system-architecture
  - backend
  - frontend
  - architecture-decisions
evidence:
  - docs/governance/decision-and-exception-policy.md
  - docs/governance/topic-authority-map.md
  - docs/templates/adr-template.md
  - backend/docs/decisions/README.md
  - frontend/docs/decisions/README.md
review_on:
  - adr-namespace-change
  - decision-status-change
  - decision-lifecycle-change
  - system-architecture-decision
  - backend-or-frontend-decision-registry-change
---

# Architecture Decision Records

> **ADRs preserve why consequential architectural choices were made. They do not replace the current architecture documents that define what the system is now.**
>
> Accepted history is append-oriented. When a consequential choice changes, create a superseding ADR and update the current canonical architecture; do not silently rewrite the old accepted decision to make history look simpler.

This directory is the repository-level registry for **system-wide** Architecture Decision Records.

Backend-specific ADRs remain under:

```text
backend/docs/decisions/
```

Frontend-specific ADRs remain under:

```text
frontend/docs/decisions/
```

The shared ADR format is owned by:

```text
docs/templates/adr-template.md
```

The policy that decides **when an ADR is required** is owned by:

```text
docs/governance/decision-and-exception-policy.md
```

---

# 1. ADR authority

An ADR answers:

```text
Why did we choose this consequential architecture?
Which alternatives were considered?
What compatibility/migration consequences followed?
Which current architecture documents and evidence implement the choice?
```

An ADR does **not** become the day-to-day current architecture handbook.

---

# 2. DEC-001 — Current architecture and decision history are separate

Use:

```text
current intended architecture
→ docs/architecture/**
→ backend/docs/architecture/**
→ frontend/docs/architecture/**

historical rationale
→ docs/decisions/**
→ backend/docs/decisions/**
→ frontend/docs/decisions/**
```

If an accepted ADR and current architecture doc appear to disagree:

1. inspect status;
2. check whether a newer ADR supersedes it;
3. inspect executable evidence;
4. classify documentation/source drift;
5. repair the canonical current owner.

Do not treat the oldest ADR text as permanently overriding later accepted architecture.

---

# 3. Decision namespaces

Notrelix uses three independent ADR namespaces.

| Scope | Namespace | Directory | Owner |
|---|---|---|---|
| System/repository | `SYS-ADR-*` | `docs/decisions/` | architecture |
| Backend | `ADR-*` | `backend/docs/decisions/` | backend-architecture |
| Frontend | `FE-ADR-*` | `frontend/docs/decisions/` | frontend-architecture |

Examples:

```text
SYS-ADR-001-...
ADR-001-...
FE-ADR-001-...
```

The numeric spaces are independent.

---

# 4. DEC-002 — ADR ID is immutable

Once assigned:

```text
SYS-ADR-003
ADR-004
FE-ADR-005
```

the ID does not change because:

- title wording improves;
- filename spelling changes;
- team name changes;
- the ADR becomes superseded.

A superseding decision gets a **new ID**.

---

# 5. Filename

Preferred filename:

```text
<SCOPE-ID>-<short-kebab-title>.md
```

Examples:

```text
SYS-ADR-001-context-extraction-boundary.md
ADR-004-rate-limiting-architecture.md
FE-ADR-003-package-exports.md
```

The filename is navigation.

The ADR ID is identity.

---

# 6. DEC-003 — Do not recycle ADR IDs

If a proposal is abandoned/rejected after receiving an ID, that identifier remains historical.

Do not reuse it for another decision.

---

# 7. Allowed statuses

Canonical ADR statuses:

```text
Proposed
Accepted
Superseded
Rejected
Deprecated
```

Do not invent synonyms such as:

```text
Final
Done
Active
Archived
Obsolete
```

for ADR lifecycle status.

---

# 8. Proposed

`Proposed` means:

- the decision is under review;
- implementation MAY be exploratory;
- it is not yet architecture authority.

---

# 9. DEC-004 — Proposed ADR does not silently redefine architecture

Until accepted, current canonical architecture remains authoritative unless an explicit transition/experiment is documented.

---

# 10. Accepted

`Accepted` means the choice is approved.

The current architecture owner must reflect the accepted decision.

---

# 11. DEC-005 — Accepted ADR requires current-architecture alignment

Acceptance is incomplete if:

```text
ADR says new architecture
but
canonical architecture docs still describe the old model
```

unless the change is deliberately staged and transition is explicit.

---

# 12. Superseded

`Superseded` means a later ADR replaces the decision.

The older ADR remains readable historical evidence.

Its `Superseded By` field points to the replacement.

---

# 13. DEC-006 — Superseded ADR is preserved

Do not delete/rewrite the old decision merely because it no longer governs current architecture.

Historical rationale helps explain migrations and previously valid source.

---

# 14. Rejected

`Rejected` records a considered proposal that was deliberately not chosen.

Use when preserving the rejected rationale is materially useful.

---

# 15. DEC-007 — Rejected ADR is not current architecture

Do not cite a rejected proposal as a normative rule.

It is decision history only.

---

# 16. Deprecated

`Deprecated` means the decision is intentionally no longer recommended/authoritative but may not yet have a direct superseding ADR.

Use sparingly.

If a new architecture choice replaces it, prefer `Superseded`.

---

# 17. Required ADR sections

Every ADR must contain:

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

The shared template contains the required structure.

---

# 18. DEC-008 — ADR contains enough context to understand the choice independently

The ADR should identify:

```text
problem
constraints
decision drivers
affected boundaries
```

without requiring the reader to reconstruct the entire debate from chat/issues.

Do not copy an entire architecture handbook into Context.

---

# 19. Decision

The Decision section states the chosen architecture clearly.

A reviewer should be able to tell:

```text
what changes?
what stays?
which boundary/contract is created?
```

---

# 20. DEC-009 — Decision is specific enough to constrain implementation

Avoid vague ADRs such as:

```text
Use best practices.
Improve scalability.
Use clean architecture.
Prefer microservices where useful.
```

A consequential decision must resolve a real choice.

---

# 21. Alternatives

Record serious alternatives and why they were not chosen.

Do not create strawman options solely to make the chosen approach appear obvious.

---

# 22. DEC-010 — Rejected alternatives are represented fairly

For each meaningful alternative record the material:

```text
benefit
cost
reason rejected
```

at the time of decision.

---

# 23. Consequences

Consequences include positive and negative effects.

Examples:

```text
coupling
operational complexity
migration cost
failure modes
test burden
future extraction
developer ergonomics
performance
security
```

---

# 24. DEC-011 — ADR records trade-offs, not marketing copy

Every consequential architecture choice has costs.

If an ADR contains only benefits, it is probably incomplete.

---

# 25. Compatibility / Migration

If the decision changes existing architecture/contracts/data, describe at decision level:

```text
compatibility strategy
migration shape
mixed-version concern
rollout constraints
old-path retirement
```

Detailed execution belongs to Delivery plans/policies.

---

# 26. DEC-012 — ADR does not become a migration tracker

The ADR records the durable migration consequence.

Temporary wave/task/progress details belong to migration/release execution artifacts.

---

# 27. Evidence

Evidence links to:

```text
canonical architecture docs
source/manifests
tests/gates
contracts
migrations
generated artifacts
```

that implement/prove the accepted choice.

---

# 28. DEC-013 — ADR rationale and executable evidence remain distinct

ADR answers **why**.

Architecture docs answer **what**.

Source/tests/CI answer **what currently exists/proves it**.

Do not collapse these into one document class.

---

# 29. Date

Use the date the decision status was established or the proposal was created according to repository process.

Dates are historical evidence.

Do not rewrite dates when updating links or marking superseded.

---

# 30. Owners

Use stable logical owners.

Examples:

```text
architecture
backend-architecture
frontend-architecture
engineering-security
```

Do not bind ADR identity to temporary employee/team names.

---

# 31. DEC-014 — ADR owner is logical responsibility

People can author/review ADRs without becoming permanent architecture owner.

---

# 32. ADR trigger

A new ADR is generally required for consequential durable choices involving:

```text
bounded-context ownership
dependency direction
cross-context/service boundary
public/versioning strategy
persistence technology/model
tenant/security foundation
major integration mechanism
foundation/platform mechanism
host/runtime framework split
high-cost difficult-to-reverse technology
```

The canonical trigger policy remains `decision-and-exception-policy.md`.

---

# 33. DEC-015 — Routine feature implementation does not require an ADR

Do not create ADRs for every:

```text
endpoint
handler
component
query
minor refactor
bug fix
```

when current canonical architecture already determines the solution.

ADR count is not architecture quality.

---

# 34. System ADR

Use `SYS-ADR-*` when the choice spans or governs more than one implementation authority plane.

Examples:

```text
system-wide context extraction strategy
cross-stack public contract/version policy
repository-wide multi-tenant platform boundary
shared external event architecture
```

---

# 35. DEC-016 — System ADR is not a backend ADR with a broader title

If the choice is purely backend implementation architecture, use `ADR-*`.

If purely frontend, use `FE-ADR-*`.

Use `SYS-ADR-*` only when the system-level choice genuinely needs repository authority.

---

# 36. Backend ADR

Backend-specific choices use:

```text
backend/docs/decisions/ADR-*.md
```

Current registry contains:

```text
ADR-001 Pipeline boundary
ADR-002 RLS bootstrap connection lifecycle
ADR-003 CSRF protection
ADR-004 Rate limiting architecture
```

Their historical IDs remain unchanged.

---

# 37. DEC-017 — Existing Backend ADR history is preserved

Migration to the new documentation architecture MUST NOT renumber Backend ADRs into `SYS-ADR-*`.

Their scope and identity remain Backend.

---

# 38. Frontend ADR

Frontend-specific choices use:

```text
frontend/docs/decisions/FE-ADR-*.md
```

Current registry contains:

```text
FE-ADR-001 Framework split
FE-ADR-002 Package manager
FE-ADR-003 Package exports
FE-ADR-004 No Next in packages
FE-ADR-005 Auth session model
```

---

# 39. DEC-018 — Existing Frontend ADR history is preserved

Do not collapse FE-ADR numbers into the Backend or System namespace.

---

# 40. Registry

This README is the registry for `SYS-ADR-*`.

Backend and frontend keep their own registries.

Avoid one giant manually duplicated table containing all ADR metadata if each registry already owns exact files.

---

# 41. DEC-019 — Each ADR is registered exactly once in its scope registry

System ADR:

```text
docs/decisions/README.md
```

Backend ADR:

```text
backend/docs/decisions/README.md
```

Frontend ADR:

```text
frontend/docs/decisions/README.md
```

Cross-links MAY reference decisions elsewhere.

---

# 42. System ADR registry

Current repository-level system ADRs:

```text
None yet.
```

Do not create a `SYS-ADR-001` merely to populate the directory.

The first real consequential system decision will receive the first ID.

---

# 43. DEC-020 — No placeholder ADR

A registry may legitimately be empty.

Do not fabricate architecture history for symmetry.

---

# 44. Superseding flow

When an accepted decision changes:

```text
1. create new ADR with a new ID
2. explain changed drivers/context
3. set new ADR: Supersedes = old ADR
4. set old ADR: Status = Superseded
5. set old ADR: Superseded By = new ADR
6. update current architecture docs
7. implement/migrate
8. update evidence/gates
```

---

# 45. DEC-021 — Accepted ADR is not silently rewritten to match the new choice

Allowed maintenance on old ADR includes:

```text
fix broken links
clarify spelling/format
add supersession metadata
add factual evidence link
```

without changing the historical decision meaning.

---

# 46. Changing an accepted ADR before implementation

If the choice was accepted but later found wrong before broad implementation, still supersede when the architectural decision materially changed.

Do not rewrite history because “nothing shipped yet” if the accepted record was already architecture governance.

---

# 47. Minor clarification

A clarification that does not change the decision may edit the ADR.

If a reasonable implementer could choose differently after the edit, it is probably a decision change and should use a superseding ADR.

---

# 48. DEC-022 — Meaning-changing edit requires new decision history

Use the test:

```text
Would the old text and new text authorize materially different architecture?
```

If yes, supersede.

---

# 49. ADR and exceptions

ADR and exception solve different problems.

```text
ADR
→ durable consequential architecture choice

Exception
→ temporary permission to violate an existing rule
```

---

# 50. DEC-023 — Exception is not an ADR

Do not create an ADR to normalize a temporary violation.

Do not create an exception for a permanent architecture choice that requires decision governance.

---

# 51. ADR and feature spec

Feature spec owns a feature's intended behavior/scope when a separate spec is needed.

ADR owns consequential architecture decisions arising from it.

One feature can require zero, one, or several ADRs.

---

# 52. DEC-024 — Feature size does not determine ADR need

A small feature can trigger a consequential security/persistence decision.

A large feature may require no ADR if it follows established architecture.

---

# 53. ADR and migration plan

Migration plan is execution-specific and temporary.

ADR records why the target architecture was chosen.

---

# 54. ADR and incident

Incident may expose the need to change architecture.

Incident timeline itself does not become the ADR.

Create a decision record if a consequential durable choice follows.

---

# 55. DEC-025 — Durable incident learning is rehomed

After incident:

```text
architecture choice → ADR/current architecture
quality invariant → quality docs/gate
recovery procedure → operations
```

Do not leave the only rationale in incident notes.

---

# 56. ADR and source drift

An accepted ADR whose implementation is incomplete does not prove source already conforms.

Classify:

```text
TRANSITION
SOURCE_DEBT
UNRESOLVED
```

according to documentation governance.

---

# 57. DEC-026 — Decision acceptance and implementation evidence are independent facts

An ADR may be:

```text
Accepted
```

while implementation rollout remains staged.

Do not mark architecture current without explaining the transition.

---

# 58. Decision quality checklist

Before accepting:

```text
[ ] consequential durable decision?
[ ] correct namespace/scope?
[ ] current problem/constraints clear?
[ ] decision precise?
[ ] serious alternatives included?
[ ] negative consequences included?
[ ] compatibility/migration understood?
[ ] security/tenant impact?
[ ] operational impact?
[ ] evidence/current-doc updates identified?
[ ] supersession links correct?
```

---

# 59. ADR review checklist

```text
[ ] ID unique
[ ] status allowed
[ ] logical owners
[ ] date present
[ ] no historical rewrite
[ ] scope matches namespace
[ ] decision is implementable
[ ] alternatives fair
[ ] consequences balanced
[ ] migration durable implications stated
[ ] evidence links relevant
```

---

# 60. Registry maintenance checklist

```text
[ ] file registered in correct README
[ ] title/ID match
[ ] no duplicate ID
[ ] superseded link is bidirectional
[ ] broken links fixed
[ ] current architecture references updated
```

---

# 61. Stop conditions

Stop and resolve governance rather than creating/editing an ADR if:

- the task is a routine feature implementation already decided by canonical architecture;
- a `SYS-ADR-*` is being used for a purely Backend/Frontend choice;
- an accepted ADR is being rewritten to hide a later decision;
- an old ADR ID is being reused;
- only benefits are recorded;
- migration consequences are material but omitted;
- an exception is being normalized as architecture;
- the ADR is treated as current architecture handbook;
- source is claimed compliant solely because the ADR is Accepted;
- a placeholder ADR is being invented to make numbering look complete.

---

# 62. Related canonical owners

```text
docs/governance/decision-and-exception-policy.md
docs/governance/topic-authority-map.md
docs/templates/adr-template.md
docs/architecture/**
backend/docs/architecture/**
frontend/docs/architecture/**
backend/docs/decisions/README.md
frontend/docs/decisions/README.md
```

---

# 63. Final decision rule

Before creating or changing an ADR, answer:

```text
Is this a consequential durable architecture decision?
Which scope truly owns it: System, Backend, or Frontend?
What current problem/constraints force a choice?
What exactly is being decided?
Which realistic alternatives were rejected and why?
What costs/failure modes does the chosen option add?
What compatibility/migration obligations follow?
Which canonical architecture docs must change?
Which evidence proves the implementation?
Does this supersede an accepted historical decision?
```

The target is:

> **small, durable decision records that preserve consequential architectural reasoning without competing with current architecture docs, rewriting history, duplicating registries, or forcing routine implementation work through unnecessary ADR ceremony.**

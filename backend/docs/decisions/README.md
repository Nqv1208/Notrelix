---
document_id: BE-DECISIONS-INDEX
document_type: decision-registry
status: active
owner: backend-architecture
applies_to:
  - backend
  - backend-architecture-decisions
evidence:
  - docs/decisions/README.md
  - docs/governance/decision-and-exception-policy.md
  - docs/templates/adr-template.md
  - backend/docs/architecture/
  - backend/docs/decisions/ADR-001-pipeline-boundary.md
  - backend/docs/decisions/ADR-002-rls-bootstrap-connection-lifecycle.md
  - backend/docs/decisions/ADR-003-csrf-protection.md
  - backend/docs/decisions/ADR-004-rate-limiting-architecture.md
  - backend/docs/decisions/ADR-005-csrf-cross-origin-bootstrap.md
review_on:
  - backend-adr-added
  - backend-adr-status-change
  - backend-adr-supersession
  - backend-decision-policy-change
  - backend-architecture-decision
---

# Backend Architecture Decisions

> **Backend ADRs preserve why consequential backend architecture choices were made. They are historical decision records, not the current backend architecture handbook.**
>
> Current architecture lives under `../architecture/`. If an accepted decision changes, create a new backend ADR with a new immutable ID, supersede the old ADR, and update the canonical architecture and executable evidence in the same governed change.

This directory is the registry for **backend-specific** Architecture Decision Records.

Repository/system decisions live under:

```text
../../../docs/decisions/
```

Frontend decisions live under:

```text
../../../frontend/docs/decisions/
```

The shared ADR schema is owned by:

```text
../../../docs/templates/adr-template.md
```

The policy deciding when an ADR is required is owned by:

```text
../../../docs/governance/decision-and-exception-policy.md
```

---

# 1. Scope of this registry

A backend ADR is appropriate when a durable consequential choice is contained primarily within backend architecture, for example:

```text
Application pipeline structure
RLS/session/bootstrap mechanism
backend authentication/CSRF host mechanism
rate-limiting architecture
persistence mechanism
messaging/delivery foundation
backend dependency boundary
```

Do not use a backend ADR for a system-wide choice merely because the backend implements part of it.

Do not use a backend ADR for routine feature work whose solution is already determined by canonical architecture.

---

# 2. Current architecture versus ADR history

Use:

```text
Current backend architecture
→ ../architecture/

Historical backend rationale
→ this directory
```

Examples:

```text
Application pipeline as it should work now
→ ../architecture/application-model.md

Why the six-zone pipeline model was accepted
→ ADR-001
```

```text
RLS/security architecture now
→ ../architecture/security-tenancy-authorization.md
→ ../architecture/infrastructure-and-data.md

Why bootstrap uses the same physical Npgsql connection
→ ADR-002
```

---

# 3. BE-DEC-001 — ADR does not override newer canonical architecture silently

If an ADR appears inconsistent with current architecture:

1. inspect its `Status`;
2. inspect `Superseded By`;
3. inspect later ADRs;
4. inspect current canonical architecture;
5. inspect source/tests;
6. classify any remaining drift.

Do not treat the oldest accepted wording as timeless current architecture.

---

# 4. Backend ADR namespace

Backend ADR IDs use:

```text
ADR-NNN
```

Examples:

```text
ADR-001
ADR-004
ADR-015
```

This namespace is independent from:

```text
SYS-ADR-NNN
FE-ADR-NNN
```

---

# 5. BE-DEC-002 — ADR ID is immutable

Once assigned, the ID does not change because:

```text
title wording changes
filename wording changes
owner changes
ADR becomes superseded/deprecated
```

A replacement decision receives a new ID.

---

# 6. Filename

Preferred:

```text
ADR-NNN-short-kebab-title.md
```

The filename is navigation.

The ADR ID is identity.

Do not renumber older ADRs to eliminate gaps.

---

# 7. Allowed statuses

Backend ADRs use only:

```text
Proposed
Accepted
Superseded
Rejected
Deprecated
```

Do not use:

```text
Active
Final
Done
Archived
Obsolete
```

as ADR decision statuses.

---

# 8. BE-DEC-003 — Registry records current ADR status

Every ADR in this directory MUST appear once in this registry with:

```text
ID
title
status
current architecture owner/topic
supersession if any
```

The registry does not restate the decision body.

---

# 9. Current backend ADR registry

| ID | Decision | Status | Current architecture owner | Superseded by |
|---|---|---|---|---|
| `ADR-001` | [Pipeline Boundary Zones](ADR-001-pipeline-boundary.md) | `Accepted` | `../architecture/application-model.md` | None |
| `ADR-002` | [RLS Bootstrap Connection Lifecycle](ADR-002-rls-bootstrap-connection-lifecycle.md) | `Accepted` | `../architecture/security-tenancy-authorization.md`, `../architecture/infrastructure-and-data.md` | None |
| `ADR-003` | [CSRF Protection via Double Submit Cookie](ADR-003-csrf-protection.md) | `Superseded` | `../architecture/api-and-contracts.md`, `../architecture/security-tenancy-authorization.md` | [ADR-005](ADR-005-csrf-cross-origin-bootstrap.md) |
| `ADR-004` | [5-Tier Rate Limiting Architecture](ADR-004-rate-limiting-architecture.md) | `Accepted` | `../architecture/api-and-contracts.md`, `../architecture/security-tenancy-authorization.md` | None |
| `ADR-005` | [Cross-Origin CSRF Bootstrap Protocol](ADR-005-csrf-cross-origin-bootstrap.md) | `Accepted` | `../architecture/api-and-contracts.md`, `../architecture/security-tenancy-authorization.md` | None |

Backend ADRs currently recorded here as:

```text
Superseded
```

```text
ADR-003 — superseded by ADR-005 (transport assumptions only; Double Submit core carried forward)
```

---

# 10. Current ADR sequence

Current used IDs:

```text
ADR-001
ADR-002
ADR-003
ADR-004
ADR-005
```

Therefore the next new backend ADR would normally be:

```text
ADR-006
```

provided no concurrent/unmerged backend ADR has already reserved that identifier.

Always check the repository registry/current branch before assigning the next ID.

---

# 11. BE-DEC-004 — Never reserve ADR IDs speculatively in docs

Do not create placeholder:

```text
ADR-005 TBD
ADR-006 future messaging
```

merely to plan work.

Assign an ID when a real decision artifact is created.

---

# 12. Backend ADR admission

A backend ADR is normally warranted when the choice changes a durable backend foundation such as:

```text
production project/dependency boundary
Application pipeline foundation
transaction model
RLS/session architecture
authentication/CSRF foundation
rate-limiting foundation
idempotency foundation
message/consumer identity
ordering/retry/poison foundation
database/provider strategy
durable external-provider abstraction
```

---

# 13. BE-DEC-005 — Routine implementation following canonical rules needs no ADR

Examples that normally do **not** need an ADR:

```text
new command/query under existing Application model
new aggregate invariant under existing Domain model
new endpoint following current API model
new EF mapping following current persistence rules
new consumer following current Platform delivery model
ordinary test addition
```

Feature size alone does not create ADR necessity.

---

# 14. System-scope escalation

Escalate to a system ADR when the choice materially changes cross-plane contracts such as:

```text
repository-wide event versioning strategy
cross-stack tenancy model
system-wide service extraction strategy
frontend/backend public contract strategy
shared identity semantics across planes
```

Do not duplicate the same decision as both `SYS-ADR-*` and `ADR-*` unless there is a genuinely distinct backend-specific choice.

---

# 15. BE-DEC-006 — One decision has one owning ADR scope

If one system ADR already owns a decision, backend docs should:

```text
reference the system ADR
implement the backend consequence
```

not copy the same rationale into a second backend ADR.

---

# 16. Required ADR structure

All **new** backend ADRs MUST contain:

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

and the standard document metadata.

Use:

```text
../../../docs/templates/adr-template.md
```

---

# 17. Existing ADR normalization

ADR-001 through ADR-004 predate the full documentation schema.

Normalization follows a stricter historical rule:

```text
preserve original decision
add recoverable structure
do not invent missing rationale
mark unknown historical information explicitly
```

Do not create plausible-but-unrecorded alternatives merely to fill the template.

---

# 18. BE-DEC-007 — Historical normalization does not change the decision

Allowed in-place normalization of an Accepted historical ADR includes:

```text
front matter
ID field
date recovered from history
current stewardship note
required section headings
current evidence links
supersession metadata
editorial clarity
```

only while preserving the original accepted meaning.

A semantic reversal/change requires a new ADR.

---

# 19. Historical unknowns

If an old ADR did not record:

```text
original owner
alternatives
migration discussion
```

and this information cannot be recovered safely:

```text
state "Not recorded in the original ADR."
```

Do not infer it from what seems architecturally reasonable today.

---

# 20. BE-DEC-008 — Current stewardship is not rewritten as historical authorship

A normalized ADR may say:

```text
Current stewardship: backend-architecture
```

when useful.

It must not claim that current team/owner authored the historical decision unless evidence supports it.

---

# 21. Accepted ADR

`Accepted` means the architecture decision is approved.

For a current accepted decision:

```text
canonical architecture
source/config
tests/gates
```

should align with it.

---

# 22. BE-DEC-009 — Accepted decision requires implementation/current-doc evidence

An ADR alone is insufficient.

Evidence should identify current:

```text
architecture doc
source/config
tests/gates
contracts/migrations where applicable
```

that demonstrate the accepted decision remains implemented.

---

# 23. Evidence evolution

Evidence paths/tests can evolve after the accepted decision.

Updating evidence is allowed when the historical decision remains unchanged.

Example:

```text
old test file renamed
→ update Evidence
```

without creating a new ADR.

---

# 24. BE-DEC-010 — Evidence update does not rewrite rationale

Do not use an “evidence refresh” commit to change:

```text
what was decided
why it was chosen
which alternative was rejected
```

without supersession.

---

# 25. Supersession

When an accepted backend decision changes materially:

```text
create new ADR-NNN
        ↓
new ADR:
Supersedes → old ADR

old ADR:
Status → Superseded
Superseded By → new ADR
        ↓
update current architecture
source/tests/contracts/migration
registry
```

Keep both ADRs.

---

# 26. BE-DEC-011 — Supersession links are bidirectional

The old and new records should point to each other.

Do not make readers reconstruct the chain from Git history.

---

# 27. Proposed decision

A `Proposed` ADR is under review.

It does not silently become current architecture.

Implementation experiments MAY exist under an explicit experimental/exception process where governance permits.

---

# 28. BE-DEC-012 — Proposed ADR is not merge permission by itself

Production source should not violate current accepted architecture merely because a proposal exists.

Use the decision/exception process.

---

# 29. Rejected decision

`Rejected` records a serious option that was deliberately not selected.

Preserve it if the rejected option is likely to recur and the rationale matters.

Do not delete because it was not implemented.

---

# 30. Deprecated decision

`Deprecated` means the decision is still historical/currently recognizable but is being phased out or discouraged without a direct replacement decision that would be better represented as `Superseded`.

Use sparingly.

---

# 31. ADR versus exception

ADR:

```text
changes/chooses architecture
```

Exception:

```text
temporarily permits a bounded deviation while current architecture remains correct
```

Do not create an ADR to authorize temporary noncompliance.

---

# 32. BE-DEC-013 — Exception never becomes silent ADR

Example:

```text
EX-BE-APP-EF-001
```

does not mean:

```text
Application now owns EF persistence
```

It means a current bounded exception exists while the canonical Application/Infrastructure boundary remains authoritative.

---

# 33. ADR versus feature specification

A feature spec states product/use-case behavior and acceptance.

An ADR states why a consequential architecture choice was made.

A large feature can need:

```text
feature spec
+
ADR
```

only when it introduces a genuinely consequential architecture choice.

Do not put the whole feature plan inside the ADR.

---

# 34. ADR versus migration plan

An ADR can record high-level compatibility/migration consequences.

Detailed:

```text
phases
backfill
checkpoints
commands
cutover
rollback/forward recovery
```

belong in a migration plan when needed.

---

# 35. BE-DEC-014 — ADR Compatibility/Migration is durable consequence, not execution tracker

An accepted ADR should remain useful after a one-time migration finishes.

Do not keep percentage/status tables in the ADR.

---

# 36. ADR versus incident

Incident timeline/root-cause evidence belongs to incident artifacts.

If an incident produces a new durable architecture choice:

```text
incident
→ learning
→ new ADR if consequential
→ canonical architecture update
```

Do not turn the ADR into an incident timeline.

---

# 37. Current ADR-001 routing

`ADR-001` preserves the accepted rationale for Application pipeline boundary zones.

Current authority:

```text
../architecture/application-model.md
```

Current executable evidence includes:

```text
src/Notrelix.Application/DependencyInjection.cs
tests/Notrelix.Architecture.Tests/ApplicationLayer/ApplicationArchitectureTests.cs
```

The exact concrete behavior inventory can evolve while the decision's zone/dependency semantics remain current.

---

# 38. BE-DEC-015 — Pipeline concrete class count is evidence, not ADR identity

If a new behavior is introduced in the correct accepted zone without changing the architectural decision:

```text
update source/tests/current architecture
```

A new ADR is not automatically required.

If the zone model itself changes materially, supersede ADR-001.

---

# 39. Current ADR-002 routing

`ADR-002` preserves why tenant bootstrap uses the same physical Npgsql connection with minimal session RLS context before full transaction-local context.

Current authority:

```text
../architecture/security-tenancy-authorization.md
../architecture/infrastructure-and-data.md
../architecture/application-model.md
```

---

# 40. BE-DEC-016 — RLS bootstrap mechanism change can supersede ADR-002

Examples:

```text
different bootstrap trust model
different connection lifecycle
different RLS session architecture
removal of bootstrap path
```

can require a new ADR if consequential.

Routine implementation fixes preserving the accepted model do not.

---

# 41. Current ADR-003 routing

`ADR-003` preserves the historical rationale for the Double Submit Cookie CSRF choice under its original transport assumptions (JavaScript-readable cookie, implicit GET issuance, `SameSite=Strict`).

Its transport assumptions are superseded by `ADR-005`. The Double Submit Cookie pattern itself is carried forward there.

Current authority:

```text
../architecture/api-and-contracts.md
../architecture/security-tenancy-authorization.md
ADR-005-csrf-cross-origin-bootstrap.md
```

---

# 42. BE-DEC-017 — Credential-model change may invalidate CSRF decision assumptions

If Notrelix moves to a materially different browser credential/session model, reassess:

```text
SameSite requirements
CSRF threat model
token mechanism
frontend participation
```

and supersede the governing CSRF ADR if the foundation changes.

The governing CSRF decision is currently `ADR-005` (bootstrap protocol + applicability classification).

---

# 43. Current ADR-004 routing

`ADR-004` preserves the accepted split rate-limiting architecture:

```text
transport-visible partitions at API
+
tenant-aware partitioning in Application
```

Current authority:

```text
../architecture/api-and-contracts.md
../architecture/security-tenancy-authorization.md
```

---

# 44. BE-DEC-018 — Numeric rate limits are runtime policy evidence, not immutable ADR identity

Changing:

```text
60/min → another value
prefill/burst capacity
environment threshold
```

does not necessarily require a new ADR if the five-tier/two-layer architecture remains unchanged.

Changing the partition/ownership model can.

---

# 44a. Current ADR-005 routing

`ADR-005` owns why browser CSRF uses a bootstrap-response token protocol with evidence-based applicability classification instead of ADR-003's JavaScript-readable cookie transport.

Current authority:

```text
../architecture/api-and-contracts.md
../architecture/security-tenancy-authorization.md
```

---

# 44b. BE-DEC-021 — Token/cookie policy values are runtime policy evidence, not ADR-005 identity

Changing token length, cookie lifetime, or environment cookie attribute values does not require a new ADR while the bootstrap protocol and applicability model remain unchanged.

Changing how applicability is classified (e.g., moving from request-evidence classification to path allowlists) or abandoning the Double Submit pattern requires supersession.

---

# 45. Registry update transaction

When adding or changing ADR status, update as applicable:

```text
ADR file
this registry
canonical architecture
source/config
tests/gates
contracts/generated docs
migration/compatibility
```

Do not merge an accepted decision while the repository intentionally describes a conflicting current architecture without an explicit transition.

---

# 46. BE-DEC-019 — Registry is not manually duplicated elsewhere

Repository-level `docs/decisions/README.md` may route to this backend registry.

Do not maintain a second authored list of backend ADRs in:

```text
root README
backend README
architecture overview
AGENTS
```

Those files should route here when decision history is needed.

---

# 47. Generated decision index

Repository documentation governance may later generate a cross-repository decision index.

If introduced:

```text
generated index
→ derived discovery surface
this README
→ backend decision registry authority
ADR files
→ decision records
```

Do not hand-edit generated decision indexes.

---

# 48. Backend ADR creation workflow

For a new consequential backend choice:

```text
1. identify the unresolved architecture choice
2. confirm canonical rules cannot already determine it
3. choose backend versus system scope
4. reserve next unused backend ID
5. instantiate ADR template
6. document serious recorded alternatives
7. classify compatibility/migration
8. collect evidence plan
9. review/accept
10. update canonical architecture
11. implement + tests/gates
12. update registry/generated indices
```

---

# 49. BE-DEC-020 — Do not write decision after the fact merely to justify code

An ADR can document an already implemented historical decision during migration only when the original meaning can be recovered honestly.

For new architecture work, use the ADR before or as part of the governed decision—not as retroactive approval theater.

---

# 50. Backend ADR review checklist

```text
[ ] correct scope: backend, not system/frontend
[ ] unused immutable ID
[ ] allowed status
[ ] date
[ ] logical owners/current stewardship
[ ] real context
[ ] precise decision
[ ] serious recorded alternatives only
[ ] positive + negative consequences
[ ] compatibility/migration
[ ] current architecture evidence
[ ] source/test evidence
[ ] supersession metadata
[ ] registry updated
```

---

# 51. Historical normalization checklist

For ADR-001…ADR-004:

```text
[ ] original decision preserved
[ ] original status preserved
[ ] date recovered only from reliable history
[ ] historical owner not fabricated
[ ] alternatives not fabricated
[ ] current evidence can be refreshed
[ ] missing fields explicitly marked
[ ] no architectural reversal hidden in formatting
```

---

# 52. Stop conditions

Stop ADR creation/normalization if:

- the proposed scope is actually system-wide;
- the decision is routine implementation already determined by current architecture;
- historical rationale/alternatives are being invented;
- an Accepted ADR is being semantically reversed in place;
- status is unclear;
- old/new supersession links are inconsistent;
- canonical architecture would remain knowingly inconsistent after acceptance;
- a temporary exception is being disguised as a permanent decision;
- a migration tracker is being inserted into the ADR;
- the registry would list a status different from the ADR file.

---

# 53. Current backend decision set

As of the Phase 13 Identity & Accounts closure, the backend decision set is:

```text
ADR-001 — Pipeline Boundary Zones — Accepted
ADR-002 — RLS Bootstrap Connection Lifecycle — Accepted
ADR-003 — CSRF Protection via Double Submit Cookie — Superseded (by ADR-005)
ADR-004 — 5-Tier Rate Limiting Architecture — Accepted
ADR-005 — Cross-Origin CSRF Bootstrap Protocol — Accepted
```

No additional backend ADR should be invented simply to make the registry look more complete.

---

# 54. Final registry rule

The backend decision plane should remain:

```text
current architecture
→ ../architecture/

why a consequential backend choice exists
→ ADR-NNN

temporary deviation from current architecture
→ scoped exception at violated canonical owner

cross-system choice
→ ../../../docs/decisions/SYS-ADR-NNN

frontend-only choice
→ ../../../frontend/docs/decisions/FE-ADR-NNN
```

Keep ADR history small, consequential, honest, and linked to current executable evidence.

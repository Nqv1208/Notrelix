---
document_id: DOC-LIFECYCLE
document_type: governance
status: active
owner: documentation-governance
applies_to:
  - repository
evidence:
  - docs/governance/documentation-authority.md
  - RULE.md
  - AGENTS.md
  - CONTEXT.md
  - scripts/
review_on:
  - document-lifecycle-change
  - documentation-metadata-change
  - documentation-authority-change
  - generated-document-model-change
  - documentation-retention-policy-change
---

# Documentation Lifecycle

> **This document defines how Notrelix documentation is created, activated, reviewed, superseded, generated, migrated, and removed.**
>
> Documentation lifecycle is not architecture maturity.
>
> A stable architecture may have an actively maintained document.
>
> A document marked `active` may describe a frozen/protected engineering contract.
>
> A document marked `superseded` may still be historically useful.
>
> A generated document may be current without being normative design authority.

This document is normative for documentation lifecycle.

Authority itself is defined by:

[`documentation-authority.md`](documentation-authority.md)

Decision and exception behavior is defined by:

[`decision-and-exception-policy.md`](decision-and-exception-policy.md)

Executable documentation validation is defined by:

[`documentation-quality-gates.md`](documentation-quality-gates.md)

---

# 1. Purpose

Documentation degrades when lifecycle is implicit.

Typical failure patterns include:

```text
old file remains beside replacement
"FINAL" filename discourages maintenance
"FROZEN" frontmatter is interpreted as "never update"
stale commit SHA creates false confidence
migration tracker becomes permanent architecture
superseded ADR is silently rewritten
generated file is manually maintained
empty README/AGENTS shells survive after their purpose disappears
archive directories become a second reading path
```

The lifecycle model exists to ensure that every active document has:

- a reason to exist;
- a known owner;
- a defined status;
- a current authority relationship;
- evidence;
- review triggers;
- a clear replacement/removal path.

---

# 2. Lifecycle is separate from authority

Authority answers:

> **Who is allowed to define this topic?**

Lifecycle answers:

> **What state is this document currently in?**

These are independent.

Example:

```text
backend/docs/architecture/application-model.md

Authority:
    canonical backend Application architecture owner

Lifecycle:
    active
```

If the Application architecture is protected/frozen:

```text
Architecture maturity:
    protected/frozen foundation

Document lifecycle:
    still active and maintainable
```

Do not convert architecture maturity into document status.

---

# 3. Lifecycle is separate from source freshness

Source freshness answers:

> **Does the current implementation/evidence still align with the document?**

A document can be:

```text
status: active
```

and still become stale because source changed incorrectly or documentation was not updated.

Likewise source can violate an active document because implementation drifted from intended architecture.

Therefore lifecycle status MUST NOT be used as proof of source conformance.

Use:

- evidence;
- review triggers;
- architecture tests;
- generated checks;
- documentation governance;
- drift classification.

---

# 4. Lifecycle is separate from architecture maturity

Notrelix may use engineering maturity concepts such as:

```text
experimental
stabilizing
protected
frozen foundation
```

where another canonical engineering process defines them.

Those are properties of the **engineering contract/capability**, not the Markdown file.

A `FROZEN` architecture document that cannot be corrected is a governance bug.

The document must remain editable whenever:

- source evidence changes;
- wording is ambiguous;
- links move;
- an ADR supersedes a decision;
- migration changes current contract;
- the architecture itself is intentionally revised.

---

# 5. Supported documentation lifecycle statuses

Canonical Notrelix documentation uses four lifecycle statuses:

```text
draft
active
superseded
generated
```

No additional status should be introduced without changing this governance contract.

These statuses are intentionally few.

Status proliferation creates ambiguous transition semantics.

---

# 6. Status state machine

Authored document:

```text
         ┌──────────────┐
         │    draft     │
         └──────┬───────┘
                │ approval / admission
                ▼
         ┌──────────────┐
         │    active    │
         └──────┬───────┘
                │ replacement / authority change
                ▼
         ┌──────────────┐
         │ superseded   │
         └──────┬───────┘
                │ retention no longer justified
                ▼
             delete
          (Git retains history)
```

Generated document:

```text
producer exists
      │
      ▼
┌──────────────┐
│  generated   │
└──────┬───────┘
       │ producer removed / replacement completed
       ▼
     delete
```

A generated document does not transition to `active`.

If humans need a normative authored contract, create an authored canonical owner and keep the generated file as evidence.

---

# 7. `draft`

## Meaning

`draft` means:

> The document is being designed or reviewed and is not yet approved as the current normative owner.

A draft MAY contain complete proposed architecture.

It is still not active authority.

---

## 7.1 Draft may be used for

- proposed system architecture;
- new product-context specification;
- new governance contract;
- new quality standard;
- proposed replacement for an existing canonical document;
- substantial rewrite before activation.

---

## 7.2 Draft MUST identify

At minimum:

```yaml
document_id:
document_type:
status: draft
owner:
applies_to:
evidence:
review_on:
```

It SHOULD also make the proposal/transition relationship explicit in body text.

---

## 7.3 Draft MUST NOT

- silently be treated as merge-gating current architecture unless the transition explicitly says so;
- be linked from AGENTS/CONTEXT-MAP as the only mandatory current authority before activation;
- use `CANONICAL`, `FINAL`, or `FROZEN` to imply approval;
- coexist indefinitely with an active owner without a decision path;
- be referenced by generated rule/topic indices as active normative truth unless the index explicitly distinguishes draft status.

---

## 7.4 Draft review

Before activation, reviewers must verify:

- correct semantic owner;
- no competing active owner;
- authority plane;
- document class;
- product/architecture alignment;
- evidence;
- ADR implications;
- migration implications;
- link integrity;
- metadata validity;
- required gates;
- transition/removal of any replaced owner.

---

# 8. `active`

## Meaning

`active` means:

> The document is the approved current document for its declared role and scope.

For a canonical normative file, this means it is the current approved owner.

For an index/router, it means the current approved navigation artifact.

For a runbook, it means the currently approved procedure.

---

## 8.1 Active does not mean immutable

An active document MUST be updated when its current contract changes.

Examples:

```text
pipeline behavior changes
→ update Application architecture

new bounded context approved
→ update product/system ownership docs

deployment model changes
→ update infrastructure docs

new documentation authority path
→ update governance/router docs
```

---

## 8.2 Active does not prove implementation conformance

An active document may reveal source debt.

The correct interpretation is:

```text
active normative owner
+
non-conforming source
=
SOURCE_DEBT
```

not:

```text
source exists
therefore active doc should be rewritten to match source
```

unless the source change itself was the approved contract change.

---

## 8.3 Active document maintenance obligations

An active canonical document MUST have:

- stable path;
- stable document ID;
- semantic owner;
- clear scope;
- review triggers;
- evidence references;
- no unresolved competing owner.

It SHOULD remain free of:

- branch-specific state;
- progress percentage;
- one-time migration checklist;
- stale SHA claims;
- temporary implementation notes better placed in CONTEXT/issues.

---

# 9. `superseded`

## Meaning

`superseded` means:

> This authored document is no longer the current normative owner because another approved artifact replaced its contract.

Superseded is not a synonym for:

```text
old
deprecated code
temporarily stale
needs review
```

It is a formal replacement state.

---

## 9.1 Superseded document MUST identify replacement

A retained superseded document MUST clearly state:

```text
Superseded by:
<document path or ADR>

Superseded because:
<concise reason>
```

If metadata supports it, a `superseded_by` field MAY be added by governance tooling in the future.

Until then, body-level replacement notice is sufficient.

---

## 9.2 Superseded document MUST NOT

- appear in mandatory current reading paths;
- contain active MUST/MUST NOT rules without clearly indicating historical status;
- be listed as canonical topic owner;
- be cited by Coding Agents as current authority;
- remain linked from root/project READMEs except as historical/decision context where justified.

---

## 9.3 Superseded retention is exceptional

The default long-term archive is Git.

A superseded file should remain checked in only if it still provides ongoing value such as:

- externally referenced compatibility documentation;
- legally/compliance-required history;
- operational transition still in progress;
- migration consumers still depend on the old contract;
- a deliberately retained historical specification whose discoverability is required.

Otherwise delete it after the replacement/migration is complete.

---

# 10. `generated`

## Meaning

`generated` means:

> The document is produced from an executable source of exact facts.

Examples:

```text
frontend package-boundary inventory
backend project map
documentation index
rule index
generated public contract reference
```

Generated files are evidence.

They are not authored architecture rationale.

---

## 10.1 Generated document requirements

A generated file MUST have a discoverable contract containing:

```text
Producer
Generation command
Do-not-edit status
Drift check
```

It MUST be reproducible.

---

## 10.2 Generated file ownership

The producer is authoritative.

Example:

```text
architecture-manifest.ts
    = producer

package-boundaries.md
    = generated representation
```

Changing the generated Markdown MUST NOT be used to change architecture.

Change the producer.

Regenerate.

Verify drift.

---

## 10.3 Generated lifecycle

Generated files remain `generated` while:

- producer exists;
- consumers/readers need the generated form;
- CI validates reproducibility.

When producer/output is no longer needed:

- remove references;
- remove generator/output;
- update governance;
- delete generated file.

Do not mark it `superseded` merely to preserve a stale generated snapshot.

Git already retains history.

---

# 11. Forbidden lifecycle statuses

Do not use the following as documentation lifecycle statuses:

```text
FROZEN
FINAL
CANONICAL
CURRENT
DONE
COMPLETE
APPROVED
LOCKED
DEPRECATED
ARCHIVED
LEGACY
MIGRATING
IN-PROGRESS
```

These words may describe another concept in prose where appropriate.

They are not lifecycle states.

---

# 12. Why `CANONICAL` is not a lifecycle status

Canonicality is an **authority property**.

A document may be:

```text
status: active
```

and be the canonical owner because the topic authority map says so.

Using:

```yaml
conformance: CANONICAL
```

as a general badge is redundant and dangerous when several overlapping files all carry it.

Canonical ownership belongs in the authority model, not a self-asserted badge.

---

# 13. Why `FROZEN` is not a lifecycle status

Frozen/protected may describe an engineering foundation.

It does not mean documentation stops changing.

A frozen architecture still needs documentation updates when:

- wording is corrected;
- evidence paths change;
- tests improve;
- links move;
- an explicit architecture change is approved;
- source debt is discovered;
- implementation converges to the already-approved contract.

Therefore:

```text
engineering maturity
≠ document lifecycle
```

---

# 14. Why `FINAL` is forbidden

Software architecture is version-controlled and evolvable.

The filename:

```text
architecture-final.md
```

eventually produces:

```text
architecture-final-2.md
architecture-final-v4.md
architecture-really-final.md
```

Stable path + lifecycle + ADR + Git history is the correct model.

---

# 15. Why `DEPRECATED` is not a documentation status

Deprecated is usually a property of:

- API;
- contract;
- feature;
- package;
- behavior.

A document describing a deprecated API may still be the active current document for that API's compatibility window.

Therefore document status remains:

```text
active
```

until the document itself is replaced or removed.

---

# 16. Why `ARCHIVED` is not a documentation status

Git is the default documentation archive.

Keeping an `archived` lifecycle inside the normal tree encourages stale reading paths.

If a document must remain checked in for an exceptional historical reason, retain it as `superseded` with an explicit replacement/historical reason.

Otherwise delete it.

---

# 17. Authority and lifecycle combinations

Valid combinations include:

| Document | Status | Authority meaning |
|---|---|---|
| `RULE.md` | active | current repository constitution |
| proposed `RULE.md` replacement | draft | proposal only |
| old architecture handbook retained during compatibility migration | superseded | historical/transition reference |
| generated package map | generated | exact machine-derived evidence |

Invalid examples:

```text
active + "maybe canonical"
draft + required current architecture
generated + manually authored normative rules
superseded + still listed as current owner
```

---

# 18. Document creation lifecycle

A new authored canonical document should follow:

```text
need identified
→ owner/admission test
→ draft
→ review
→ authority/reference migration if replacing an owner
→ activate
→ validate
```

Do not create a new file directly as active simply because a folder needs symmetry.

---

# 19. Admission before draft

Before creating even a draft canonical file, answer:

```text
What distinct question does it own?
Why can no current owner own this question?
What document class is it?
What is its semantic scope?
Who owns maintenance?
What evidence supports it?
What triggers review?
Does it replace anything?
```

If there is no distinct owner, add content to the existing canonical owner instead.

---

# 20. Draft-to-active activation checklist

A draft may transition to active only when:

```text
[ ] authority is unambiguous
[ ] document class is correct
[ ] stable document_id exists
[ ] owner exists
[ ] scope is explicit
[ ] evidence exists
[ ] review triggers exist
[ ] normative terms are coherent
[ ] links resolve
[ ] topic map is correct
[ ] routers/indexes are correct
[ ] ADR/decision implications are handled
[ ] migration/compatibility implications are handled
[ ] replaced owner is migrated/neutralized
[ ] generated indices are updated
[ ] documentation gates pass
```

Activation is an authority event, not merely a metadata edit.

---

# 21. Active document review

Notrelix uses **event-driven review** as the primary freshness model.

Review triggers are encoded through:

```yaml
review_on:
  - <semantic trigger>
```

Examples:

```text
application-pipeline-change
bounded-context-owner-change
message-identity-change
frontend-package-model-change
tenant-authorization-change
deployment-runtime-change
documentation-authority-change
```

---

# 22. Event-driven review

When a trigger occurs, the owning change MUST review the corresponding canonical document.

Review means:

```text
read affected section
compare with source/decision
update if needed
confirm evidence
run relevant docs gates
```

It does not mean blindly editing the document in every triggered change.

A correct review may conclude:

```text
no documentation change required
```

but the reviewer must have considered the contract.

---

# 23. Calendar review

Periodic/calendar review MAY be added for:

- compliance;
- operational runbooks;
- security procedures;
- external certification;
- legal/policy obligations.

Calendar review MUST NOT be the only architecture freshness mechanism.

Architecture changes happen when source/decisions change, not when a quarterly reminder fires.

---

# 24. Evidence review

Review the strongest evidence appropriate to the document.

Examples:

```text
backend overview
→ backend.slnx + csproj + architecture tests

frontend dependency boundary
→ architecture manifest + generated package map + checks

API contract
→ OpenAPI producer + generated consumers + contract tests

RLS
→ persistence config + integration tests

product context
→ product contract + Domain/Application/frontend behavior evidence
```

Do not verify an exact generated inventory manually if a producer can prove it.

---

# 25. Review outcome classes

A document review should lead to one of:

```text
NO_CHANGE
DOC_UPDATE
SOURCE_DEBT
TRANSITION
CONTRACT_CHANGE
UNRESOLVED
SUPERSEDE
DELETE
REGENERATE
```

These are **review outcomes**, not lifecycle statuses.

---

# 26. `NO_CHANGE`

Use when:

- trigger occurred;
- canonical contract remains correct;
- evidence remains aligned;
- no wording/reference update is needed.

No metadata churn is required merely to prove the review happened unless external governance requires it.

---

# 27. `DOC_UPDATE`

Use when:

- intended contract is unchanged;
- wording/evidence/reference became stale or incomplete.

Update the active document.

Do not create a new versioned filename.

---

# 28. `SOURCE_DEBT`

Use when:

- active canonical contract is still intended;
- source violates it.

Keep document active.

Track/fix source debt.

Do not modify docs to normalize accidental architecture.

---

# 29. `TRANSITION`

Use when old and new structures intentionally coexist.

The active documentation MUST clarify:

```text
target
legacy scope
new-code rule
transition owner
completion condition
```

Transition is not a document status.

A document can be:

```text
status: active
```

while describing a transition.

---

# 30. `CONTRACT_CHANGE`

Use when intended normative semantics themselves are changing.

Required:

```text
change classification
owner update
ADR where consequential
migration/compatibility
source/tests/gates
dependent docs
```

The active path remains stable when possible.

---

# 31. `UNRESOLVED`

Use when evidence or ownership conflicts cannot be safely resolved.

Do not change lifecycle merely to hide uncertainty.

Stop and use the decision/exception process.

---

# 32. `SUPERSEDE`

Use when the document itself is replaced by another approved owner.

Transition:

```text
active
→ superseded
```

Then update all mandatory reading paths.

---

# 33. `DELETE`

Use when:

- document no longer has independent purpose;
- no current consumer requires it;
- historical value is adequately preserved by Git/ADR;
- references have moved.

Deletion is often the correct end state.

---

# 34. `REGENERATE`

Use for generated files when producer changed.

Do not manually edit.

---

# 35. Supersession workflow

When replacing an authored active document:

```text
1. identify replacement owner
2. inventory durable knowledge
3. migrate unique current semantics
4. migrate rationale to ADR if appropriate
5. update product/system/project references
6. update topic authority map
7. update routers/indexes
8. activate replacement
9. mark old document superseded only if temporary retention is needed
10. delete old document when retention reason ends
11. run docs governance
```

---

# 36. Superseding versus editing in place

Prefer **edit in place** when:

- owner remains the same;
- path remains semantically correct;
- contract evolves;
- no historical compatibility document is needed.

Use **supersession** when:

- topic owner moves;
- document scope fundamentally changes;
- one large document is intentionally replaced by a new authority model;
- compatibility requires old and new specifications to coexist briefly.

Do not create a new file for every architecture revision.

---

# 37. Example — Application architecture evolves

Current owner:

```text
backend/docs/architecture/application-model.md
```

Application pipeline changes intentionally.

Correct:

```text
ADR if consequential
→ update application-model.md in place
→ update tests/gates
```

Incorrect:

```text
application-model-v2.md
```

unless the old and new contracts genuinely need a temporary compatibility period with explicit authority boundaries.

---

# 38. Example — authority moves out of legacy docs

Old:

```text
docs/engineering/02-backend/...
```

Target:

```text
backend/docs/...
```

This is authority migration.

Correct:

```text
migrate unique durable knowledge
→ update target backend canonical docs
→ migrate references
→ remove old engineering authority
```

Do not retain the old tree as `archived` on the active reading path.

---

# 39. Example — generated package map

Producer:

```text
frontend/tooling/dependency-rules/src/architecture-manifest.ts
```

Output:

```text
frontend/docs/generated/package-boundaries.md
```

Package changes:

```text
update approved manifest
→ regenerate
→ check drift
```

Lifecycle remains:

```text
generated
```

No new `package-boundaries-v2.md`.

---

# 40. Document deletion lifecycle

Deletion is a governance action.

Before deleting:

```text
[ ] document class understood
[ ] current authority checked
[ ] topic map checked
[ ] unique knowledge classified
[ ] successor identified if needed
[ ] references found
[ ] ADR/history impact checked
[ ] generated producer checked
[ ] operational/legal retention checked
[ ] removal does not break active migration
[ ] docs checks pass after deletion
```

---

# 41. Delete empty shells

Empty/superficial docs should not survive merely to preserve tree shape.

Examples:

```text
README with only folder name
AGENTS that only says "follow parent"
CONTEXT that duplicates generated package list
RULE that only repeats root rules
```

If a file has no distinct responsibility, delete it.

---

# 42. Historical retention

Git is the primary history mechanism.

Keep a superseded document in-tree only when current users still need to discover it for an active reason.

Examples:

```text
compatibility window
current migration
regulatory history
operational transition
```

Do not retain files merely because deleting documentation feels risky.

Risk is handled by knowledge migration and Git history.

---

# 43. No archive directory by default

Do not create:

```text
docs/archive/
docs/legacy/
docs/old/
```

as a general lifecycle stage.

Archive folders:

- remain indexed/searchable;
- attract stale links;
- create alternate reading paths;
- encourage Coding Agents to select obsolete material.

If exceptional checked-in historical documentation is required, make its historical role explicit and keep the scope narrow.

---

# 44. Temporary migration documents

Migration artifacts MAY exist temporarily.

Examples:

```text
knowledge migration ledger
authority migration inventory
one-time rollout checklist
```

They are not canonical architecture.

They SHOULD identify:

```text
purpose
owner
start condition
completion condition
deletion condition
canonical targets
```

When complete:

```text
migrate durable knowledge
→ delete temporary artifact
```

---

# 45. Roadmaps and plans

Roadmaps/plans are execution artifacts.

They SHOULD live in:

- issue/project tracker;
- PR;
- task artifact;
- temporary migration area when repository-local coordination is truly required.

They MUST NOT become permanent architecture authority.

After completion:

- extract durable contract changes;
- preserve decision rationale where needed;
- delete the plan from active docs.

---

# 46. Audits and assessments

A point-in-time audit answers:

> What did we observe at this point?

It is not a canonical architecture document.

Audit findings should be converted into:

```text
issue
architecture update
ADR
test/gate
current context
```

as appropriate.

Do not keep “score 96/100” as permanent product/system truth.

---

# 47. Freeze certification artifacts

Freeze/certification artifacts are point-in-time evidence.

They may contain:

```text
baseline SHA
test counts
gate results
known exceptions
```

They MUST NOT replace current architecture documentation.

They MAY be stored as build/CI/release evidence where useful.

They are not lifecycle metadata for canonical docs.

---

# 48. Metadata lifecycle

Canonical authored metadata should remain lightweight.

Required model:

```yaml
document_id:
document_type:
status:
owner:
applies_to:
evidence:
review_on:
```

Do not change metadata frequently without semantic reason.

Metadata churn is not documentation quality.

---

# 49. Stable document ID

`document_id` survives:

- ordinary edits;
- typo fixes;
- section reorganization;
- architecture updates under same owner/path.

A document ID changes only when the semantic document itself is replaced or ownership model fundamentally changes.

Do not create:

```text
SYS-OVERVIEW-V2
SYS-OVERVIEW-2026
```

for ordinary evolution.

---

# 50. Path stability

Canonical paths SHOULD be stable.

Moving a path is justified when:

- authority plane was wrong;
- ownership changed;
- documentation topology was intentionally redesigned;
- name materially misrepresents topic.

Do not churn paths for style.

Path moves require reference migration and docs checks.

---

# 51. Owner lifecycle

If ownership team/name changes but semantic topic remains the same:

```text
update owner metadata
```

Do not supersede the document solely because maintainers changed.

If semantic ownership moves to a different authority plane/context:

```text
authority migration
```

may be required.

---

# 52. Scope lifecycle

If `applies_to` expands or narrows:

- determine whether the same semantic owner still exists;
- check for new overlap;
- update topic map/router;
- review evidence.

A major scope change may require document split/merge.

Do not simply widen scope until one file owns everything.

---

# 53. Evidence lifecycle

When source paths move:

- update evidence references;
- do not supersede architecture solely for file movement.

When evidence no longer proves a claim:

- find new evidence;
- classify drift;
- update source/docs appropriately.

Evidence is maintained with the active document.

---

# 54. Review-trigger lifecycle

`review_on` should evolve when actual change triggers become clearer.

Do not add every conceivable event.

A trigger should be:

- semantically meaningful;
- discoverable in change review;
- actionable.

Examples:

```text
message-identity-change
not
any-backend-change
```

---

# 55. Status transition rules

Allowed transitions:

```text
draft       → active
draft       → delete

active      → superseded
active      → delete        # only if no successor is required
active      → active        # normal maintenance

superseded  → delete

generated   → generated     # regeneration
generated   → delete
```

Disallowed by default:

```text
superseded → active
generated  → active
active     → draft
```

If an exceptional reversal is needed, treat it as a new governance decision rather than changing status casually.

---

# 56. Why `superseded → active` is disallowed by default

Restoring an old specification often ignores changes made after supersession.

If an old design becomes relevant again:

- re-evaluate against current product/source/security/contracts;
- create/update the current canonical owner;
- create a new ADR if needed.

Do not “reactivate history” by flipping metadata.

---

# 57. Why `active → draft` is disallowed

Once a document is the approved current owner, uncertainty does not make it retroactively a proposal.

If it becomes incorrect:

```text
fix it
supersede it
or classify unresolved conflict
```

Do not downgrade status to avoid accountability.

---

# 58. Why generated does not become active

Generated and authored are different classes.

If a generated table reveals a stable architecture principle, write that principle in an authored architecture owner.

The generated output remains evidence.

---

# 59. Document split lifecycle

Split a document only when it has developed multiple distinct owners/lifecycles.

Process:

```text
identify topics
→ define new owners
→ draft split documents
→ migrate durable content
→ update topic map/router
→ activate new documents
→ reduce/delete old document
→ run governance
```

Do not split merely because a file is long.

Depth is acceptable when ownership is coherent.

---

# 60. Document merge lifecycle

Merge when several files:

- own the same topic;
- always change together;
- have indistinguishable scope;
- create navigation overhead without semantic benefit.

Process:

```text
choose canonical target
→ migrate unique knowledge
→ update IDs/refs as governed
→ activate target
→ supersede/delete sources
→ update maps
```

---

# 61. Long documents

Length does not trigger supersession.

A 2,000-line file may be correct if:

- one semantic owner exists;
- structure is navigable;
- content is non-duplicative;
- evidence is clear.

A 20-line duplicate file may be more harmful.

Lifecycle is about responsibility, not word count.

---

# 62. Staleness definition

A document is stale when a material claim no longer matches the approved current contract or current evidence it claims to describe.

Staleness is not a lifecycle status.

Possible resolutions:

```text
DOC_UPDATE
SOURCE_DEBT
CONTRACT_CHANGE
SUPERSEDE
DELETE
UNRESOLVED
```

---

# 63. Stale current facts

If README/CONTEXT says:

```text
Node >= 20
```

but executable manifest now says:

```text
Node >= 22
```

and the manifest change was approved:

```text
DOC_STALE
→ update current fact
```

No new README version.

---

# 64. Stale architecture versus source debt

If canonical architecture says:

```text
Domain has no provider dependency
```

and source introduces one without approved architecture change:

```text
SOURCE_DEBT
```

Do not “fix stale docs” by documenting the violation as intended architecture.

---

# 65. Review on source-derived inventory changes

Where exact facts are generated, prefer:

```text
producer change
→ regenerate
→ drift check
```

rather than event-triggered manual prose edits.

Examples:

- project maps;
- package maps;
- rule index;
- documentation index.

---

# 66. Broken link lifecycle

A moved/deleted document MUST migrate active references in the same authority migration.

Do not keep redirect Markdown files indefinitely by default.

A temporary compatibility pointer MAY exist when external/current consumers require it.

It must have:

```text
reason
owner
removal condition
target
```

---

# 67. Compatibility pointer lifecycle

A pointer file is procedural/compatibility, not canonical.

Examples may include provider/router compatibility files.

It SHOULD remain very small.

If no consumer needs it, delete it.

Do not allow pointer files to accumulate independent rules over time.

---

# 68. Provider-specific file lifecycle

Provider-specific instructions such as `CLAUDE.md` remain active only while:

- provider/workflow uses them;
- they provide needed compatibility routing.

They should not be superseded for every architecture update because they should route to canonical owners.

If provider integration is removed:

```text
delete provider file
```

after reference cleanup.

---

# 69. Skill lifecycle

A skill exists while a repeatable workflow is actively useful.

Skill change triggers include:

- workflow changes;
- canonical reading path changes;
- validation command changes;
- tool/provider changes.

Delete obsolete skills.

Do not keep one skill per historical workflow generation.

---

# 70. Template lifecycle

Templates remain active while the governed process uses them.

When governance requirements change:

```text
update template in place
```

unless the old template must remain available for an active compatibility reason.

Old completed artifacts created from a template do not need the old template to remain canonical.

---

# 71. Runbook lifecycle

Runbooks have stronger operational freshness needs.

Review when:

- service topology changes;
- incident ownership changes;
- monitoring/alerting changes;
- recovery commands change;
- provider dependency changes;
- backup/restore behavior changes.

An obsolete runbook is dangerous.

Prefer deletion/replacement over keeping several generations.

---

# 72. Security documentation lifecycle

Security docs must be reviewed when:

- auth model changes;
- authorization policy changes;
- tenant/RLS model changes;
- secret/config flow changes;
- sensitive logging changes;
- provider security contract changes.

Security staleness is a blocker when it can direct implementation/operations unsafely.

---

# 73. Product-context lifecycle

A product-context document remains active while the bounded context exists.

Feature additions generally update it in place.

Supersession is appropriate only when:

- context is merged/split;
- ownership fundamentally changes;
- product semantics are replaced.

A bounded context rename/merge is a product architecture change, not an editorial lifecycle change.

---

# 74. Architecture-document lifecycle

Architecture docs evolve in place through approved changes.

They are superseded when:

- authority moves;
- architecture scope is fundamentally replaced;
- compatibility requires parallel historical/current specifications.

Do not version the filename for ordinary changes.

---

# 75. ADR lifecycle relationship

ADR status is separate from documentation lifecycle.

An ADR can have decision statuses such as:

```text
Proposed
Accepted
Superseded
Rejected
Deprecated
```

according to ADR policy.

The Markdown file itself remains an ADR record.

Do not apply `status: active` metadata to mean “ADR Accepted” unless governance explicitly distinguishes fields.

ADR decision status belongs inside the ADR model.

This lifecycle document governs canonical authored handbooks/standards/routers/generated docs, not the decision semantics of ADR status.

---

# 76. Generated-index lifecycle

Repository indices such as:

```text
docs/generated/document-index.md
docs/generated/rule-index.md
```

must be regenerated when their producer inputs change.

They should not be manually patched to get CI green.

If the generator changes format, regenerate in place.

---

# 77. Documentation migration lifecycle

During the current re-foundation, legacy documentation may remain temporarily while knowledge is migrated.

The migration MUST avoid two bad states:

### Bad state A

```text
new canonical tree added
+
old canonical tree still active
+
both linked as authoritative
```

### Bad state B

```text
old tree deleted
before unique durable knowledge is migrated
```

Correct:

```text
inventory
→ migrate knowledge
→ switch authority/routes
→ delete old authority
→ certify
```

---

# 78. Migration ledger lifecycle

A migration ledger is temporary.

It exists only to prove:

- old knowledge was reviewed;
- retained claims have destinations;
- stale claims were intentionally dropped;
- deletion is safe.

After migration certification:

```text
delete ledger
```

unless there is an explicit ongoing audit/legal reason to retain it.

Git preserves the migration history.

---

# 79. Documentation transition completion

A documentation transition is complete when:

```text
[ ] target owner exists
[ ] target content is complete
[ ] unique old knowledge migrated
[ ] topic map updated
[ ] routers updated
[ ] old refs removed
[ ] old active owner removed
[ ] generated indices updated
[ ] docs gates pass
[ ] temporary migration artifact deletion criteria met
```

---

# 80. No silent lifecycle mutation

A status change must correspond to a real lifecycle event.

Do not change:

```text
draft → active
```

only to make a checker pass.

Do not change:

```text
active → superseded
```

without a replacement/removal decision.

Do not change:

```text
generated → active
```

to allow manual editing.

---

# 81. Lifecycle review in PRs

When a PR changes documentation topology, reviewers should ask:

```text
Is this a new owner or an edit?
Is status correct?
Does another active owner exist?
Are references migrating?
Is deletion safe?
Is this content temporary?
Could this be generated?
Does the document have a removal condition?
```

---

# 82. Lifecycle and CI

Documentation gates SHOULD validate:

- allowed status values;
- required metadata;
- active canonical path existence;
- no superseded file listed as canonical;
- no generated file missing producer marker where required;
- forbidden version/final/frozen authority patterns;
- broken links;
- duplicate IDs;
- generated drift;
- forbidden legacy paths.

---

# 83. Lifecycle and rule index

The generated rule index SHOULD include rules only from appropriate active normative owners.

Draft rules may be excluded from current active rule index or clearly marked as draft by the generator.

Superseded rules MUST NOT appear as current active rules.

Generated output must follow the producer's lifecycle semantics.

---

# 84. Lifecycle and document index

The generated document index SHOULD expose lifecycle status.

Suggested columns:

```text
Document ID
Type
Status
Owner
Path
Applies To
```

This allows tooling/reviewers to find:

- drafts;
- active owners;
- retained superseded docs;
- generated files.

---

# 85. Lifecycle and topic authority map

The topic authority map MUST point only to the current active canonical owner.

It MUST NOT point to:

- draft proposal;
- superseded document;
- generated representation when an authored semantic owner exists;
- temporary migration plan.

---

# 86. Lifecycle and CONTEXT

`CONTEXT.md` may describe a migration involving documents.

It does not change their lifecycle itself.

Example:

```text
CONTEXT:
docs/engineering still exists during migration.

Lifecycle:
those legacy files remain transitional legacy evidence,
not target canonical ownership.
```

The authority/lifecycle migration must still be completed explicitly.

---

# 87. Lifecycle and Git history

Git provides:

- past content;
- authorship;
- timestamps;
- diffs;
- deleted file recovery.

Therefore active docs do not need to retain large historical sections solely because “we may need the old version”.

Use ADRs for rationale.

Use Git for old content.

Use current docs for current contracts.

---

# 88. Lifecycle and release artifacts

Release notes, certificates, and freeze evidence may be immutable point-in-time artifacts.

They are not canonical architecture lifecycle states.

Their storage/retention may be defined by delivery/CI/release governance separately.

Do not treat them as `active` architecture documents.

---

# 89. Lifecycle anti-patterns

The following are prohibited or strongly discouraged:

```text
architecture-final-v3.md
README-old.md
RULE-new.md
docs/archive/
per-package empty CONTEXT.md
FROZEN metadata on living architecture docs
CANONICAL badges self-asserted by several overlapping files
manual last_verified_sha freshness theater
keeping migration trackers forever
keeping generated snapshots after producer removal
activating drafts because implementation already landed accidentally
```

---

# 90. Lifecycle smells

Review when:

- directory contains several filenames for the same topic;
- superseded file is linked from root onboarding;
- draft has been unchanged for months while being used as current architecture;
- old migration artifact contains the only copy of an important rule;
- generated file is hand-edited;
- document status disagrees with topic map;
- no one can explain why a superseded file remains checked in;
- frontmatter describes architecture maturity instead of lifecycle;
- a canonical document cannot be corrected because it is called “frozen”.

---

# 91. Documentation lifecycle severity

## Blocker

- draft being used as sole current architecture;
- superseded doc still listed as canonical owner;
- generated file manually overriding producer;
- old and new canonical owners both active after migration;
- stale security/tenant/runbook docs can cause unsafe behavior.

## Major

- unnecessary retained superseded documents;
- transition without removal condition;
- stale evidence/review triggers;
- versioned filename generations;
- missing producer metadata for generated docs.

## Minor

- non-critical metadata wording;
- review trigger could be more precise;
- historical link cleanup with no authority impact.

---

# 92. Lifecycle exception

If a use case requires behavior outside this lifecycle model:

- do not invent a new status locally;
- use the decision/exception policy;
- define why existing states are insufficient;
- define scope;
- define tooling impact;
- define migration;
- update this governance document if the change becomes permanent.

---

# 93. Lifecycle change protocol

Changing the lifecycle model itself requires review of:

```text
documentation-authority.md
topic-authority-map.md
documentation-quality-gates.md
docs/README.md
CONTEXT-MAP.md
generated document index
scripts/docs
CI
```

If metadata schema changes, migrate all canonical documents atomically or through a defined transition.

---

# 94. Document creation checklist

```text
[ ] distinct owner exists
[ ] correct authority plane
[ ] document class selected
[ ] document_id assigned
[ ] status starts as draft unless activation is part of same reviewed change
[ ] owner declared
[ ] applies_to declared
[ ] evidence declared
[ ] review_on declared
[ ] no existing active owner conflicts
[ ] router/topic map impact handled
[ ] docs gates pass
```

---

# 95. Active maintenance checklist

```text
[ ] contract remains current
[ ] evidence still relevant
[ ] links resolve
[ ] review triggers remain meaningful
[ ] no duplicate owner appeared
[ ] no temporary migration prose became permanent
[ ] exact inventories remain generated
[ ] decision history remains coherent
```

---

# 96. Supersession checklist

```text
[ ] replacement owner identified
[ ] replacement approved/active
[ ] durable knowledge migrated
[ ] rationale preserved where needed
[ ] topic map moved
[ ] routers/indexes moved
[ ] old file clearly marked superseded if retained
[ ] retention reason documented
[ ] deletion condition defined
[ ] docs gates pass
```

---

# 97. Deletion checklist

```text
[ ] file is not current owner
[ ] unique durable knowledge migrated
[ ] active references removed
[ ] external/current consumer impact checked
[ ] producer checked if generated
[ ] operational/legal retention checked
[ ] transition complete
[ ] topic/document indices updated
[ ] docs gates pass
```

---

# 98. Generated-document checklist

```text
[ ] producer exists
[ ] generation command exists
[ ] do-not-edit marker exists
[ ] drift check exists
[ ] status = generated
[ ] no normative rationale depends only on generated prose
[ ] regeneration succeeds
```

---

# 99. Current Notrelix migration interpretation

The checked-in legacy governance generation used concepts such as:

```text
maturity: FROZEN
conformance: CANONICAL
last_verified_sha
```

and a documentation tree that overlaps with backend/frontend canonical project docs.

The target lifecycle replaces that model with:

```text
authority:
    owned separately

lifecycle:
    draft / active / superseded / generated

architecture maturity:
    owned by engineering/product processes, not document status

freshness:
    evidence + review triggers + executable checks

history:
    ADR + Git
```

The migration should preserve durable governance knowledge while removing overlapping authority and stale lifecycle signals.

---

# 100. Final lifecycle rule

A document should exist in the active repository only while it has a current reason to exist.

For every document, the repository must be able to answer:

```text
Why does this file exist?
Who owns it?
What status is it in?
What authority does it have?
What evidence supports it?
What triggers review?
What replaces it?
When can it be deleted?
```

If those answers are unclear, the documentation lifecycle is unhealthy.

The target is not permanent documentation accumulation.

The target is:

> **living current contracts, explicit decision history, generated exact evidence, and aggressive removal of obsolete authority.**

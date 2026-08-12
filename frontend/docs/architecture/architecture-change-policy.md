---
document_id: FE-ARCH-CHANGE-POLICY
document_type: architecture-policy
status: active
owner: frontend-platform
applies_to:
  - frontend-architecture-changes
  - frontend-package-changes
  - frontend-host-changes
  - frontend-contract-foundation
  - frontend-state-foundation
  - frontend-realtime-foundation
  - frontend-ui-foundation
  - frontend-testing-foundation
evidence:
  - frontend/docs/architecture/
  - frontend/docs/decisions/
  - frontend/tooling/dependency-rules/src/architecture-manifest.ts
  - frontend/docs/generated/package-boundaries.md
  - frontend/package.json
  - .github/workflows/fe-ci.yml
review_on:
  - architecture-authority-change
  - adr-policy-change
  - package-graph-change
  - host-framework-change
  - state-authority-change
  - runtime-boundary-change
  - testing-gate-foundation-change
---

# Frontend Architecture Change Policy

> **A frontend architecture change is a deliberate change to durable ownership, dependency direction, runtime/host boundary, state authority, public package contract, generated-contract flow, or engineering gate foundation.**
>
> Normal feature work stays inside approved architecture. Source debt is repaired back to architecture. Transitional compatibility is temporary and owned. Consequential foundation changes require explicit decision history instead of silently editing manifests, exports, routes, providers, or tests until the code compiles.

This document is the canonical frontend policy for:

- distinguishing feature work from architecture change;
- classifying source/docs/contract drift;
- deciding when FE ADR is required;
- changing package graph/layers;
- adding/removing packages or hosts;
- changing public exports;
- changing generated-contract flow;
- changing state/realtime/UI/runtime foundations;
- changing testing/gate topology;
- compatibility and migration;
- temporary transitions/exceptions;
- atomic documentation/source/generated/test updates;
- review evidence and stop conditions.

It does not approve backend product/API changes by itself.

---

# 1. Policy objective

The policy exists to prevent this pattern:

```text
feature needs import
→ manifest edited
→ export widened
→ provider moved
→ test weakened
→ architecture changed accidentally
```

Instead:

```text
feature requirement
→ classify
→ use current architecture
or
→ deliberately change architecture
→ migrate atomically
→ prove
```

---

# 2. FE-ARCH-CHG-001 — Architecture is changed deliberately, never incidentally

A source change that modifies durable ownership/dependency semantics MUST be recognized as architecture-affecting before merge.

Compilation success is not sufficient approval.

---

# 3. What is normal feature work?

Normal feature work:

```text
uses existing package owners
uses allowed imports
uses existing public contracts
uses existing host/runtime model
uses existing state/realtime/UI rules
adds/changes product behavior
adds tests
```

without changing the foundation.

---

# 4. FE-ARCH-CHG-002 — Additive behavior inside existing boundaries is not architecture change by default

Examples:

```text
new Work Management mutation
new Documents screen
new Billing component
new test
new route using existing host patterns
```

can be normal feature work when ownership/contracts stay unchanged.

---

# 5. What is architecture change?

Architecture-affecting dimensions include:

```text
semantic owner
package layer
allowed dependency direction
host framework/composition foundation
runtime ownership
public package export foundation
state authority/cache model
realtime ownership/recovery model
design-system theme/platform foundation
generated contract pipeline
test/gate foundation
```

---

# 6. FE-ARCH-CHG-003 — Architecture significance follows durable coupling

The number of changed lines is not the criterion.

A five-line manifest edge can be more architectural than a thousand-line feature implementation.

---

# 7. Architecture authority stack

Current frontend authority layers:

```text
current semantics
→ frontend/docs/architecture/

historical consequential rationale
→ frontend/docs/decisions/

exact package graph
→ architecture-manifest.ts

generated readable graph
→ generated/package-boundaries.md

current implementation
→ source/config/package manifests

proof
→ tests/CI
```

---

# 8. FE-ARCH-CHG-004 — Change the correct authority

Do not update:

```text
ADR
```

when only current implementation changed without decision change.

Do not update:

```text
generated docs
```

by hand.

Do not update:

```text
architecture docs
```

to bless accidental source debt.

---

# 9. Drift classification

Before reconciling disagreement classify it as one of:

```text
DOC_STALE
SOURCE_DEBT
TRANSITION
CONTRACT_CHANGE
UNRESOLVED
```

using repository documentation governance.

---

# 10. FE-ARCH-CHG-005 — Conflict classification precedes repair

Do not assume:

```text
source wins
docs win
ADR wins
```

without identifying what kind of conflict exists.

The correct fix depends on authority and intent.

---

# 11. DOC_STALE

Use when:

```text
architecture/source are already correct
but authored/generated explanation is outdated
```

Example:

```text
package renamed intentionally
source/manifest migrated
architecture meaning unchanged
one authored route still names old package
```

---

# 12. FE-ARCH-CHG-006 — DOC_STALE does not require a new architecture decision

Repair the canonical document/generated output.

Do not create an ADR for editorial synchronization.

---

# 13. SOURCE_DEBT

Use when current source violates accepted architecture.

Examples:

```text
deep import escaped
mobile imports web-only dependency
app owns reusable state
frontend CSRF helper conflicts with accepted producer contract
```

---

# 14. FE-ARCH-CHG-007 — SOURCE_DEBT is repaired toward accepted architecture unless architecture is intentionally changed

Do not edit canonical docs merely to make the debt look compliant.

If the intended architecture changed, reclassify as architecture/contract change and follow decision process.

---

# 15. TRANSITION

Use for explicit migration coexistence.

Examples:

```text
old/new package
old/new export
old/new endpoint adapter
old/new state representation
```

with a cutover plan.

---

# 16. FE-ARCH-CHG-008 — Transition has owner and removal condition

A transition MUST define:

```text
old authority
new authority
compatibility boundary
consumer migration
completion proof
removal condition
```

Do not normalize dual authority indefinitely.

---

# 17. CONTRACT_CHANGE

Use when the intended public producer/client contract changes.

Examples:

```text
OpenAPI operation
realtime event
CSRF header/cookie contract
auth session contract
public package API
```

depending on contract owner.

---

# 18. FE-ARCH-CHG-009 — Contract change follows producer and compatibility authority

Frontend MUST NOT independently rewrite backend public contract.

Coordinate:

```text
producer
generated artifacts
adapters
state
tests
rollout
```

according to contract-first delivery.

---

# 19. UNRESOLVED

Use when evidence is insufficient to determine safe target.

Examples already identified in current architecture can include:

```text
account-cache transition isolation not proven
realtime gap continuation not proven
```

until source/tests/decision resolve them.

---

# 20. FE-ARCH-CHG-010 — UNRESOLVED is not permission to choose locally

Document the missing decision/evidence.

Complete independent safe work.

Do not let a coding agent invent the architecture.

---

# 21. ADR purpose

Frontend ADRs preserve historical rationale for consequential durable choices.

Current registry uses:

```text
FE-ADR-NNN
```

IDs.

---

# 22. FE-ARCH-CHG-011 — ADR explains why; architecture document explains how it works now

Do not make an ADR the only current operating guide.

After an Accepted architecture decision:

```text
update canonical architecture
```

so new contributors do not reconstruct current behavior from history.

---

# 23. Existing frontend decisions

Current registry includes historical decisions for:

```text
framework split
package manager
package exports
no Next in reusable packages
auth session model
```

Check status/supersession in the registry.

---

# 24. FE-ARCH-CHG-012 — Accepted ADR history is not silently rewritten

If an Accepted decision changes:

```text
create a new FE-ADR
mark old decision Superseded
link both directions
```

Preserve historical meaning.

Editorial normalization MAY add missing metadata only when historical meaning is recoverable safely.

---

# 25. When ADR is required

An FE ADR is generally required when a change makes a durable, costly-to-reverse frontend foundation choice.

Examples:

```text
host framework split
package manager
package export model
new architecture layer
new host
microfrontend/runtime federation
auth/session foundation
state authority foundation
realtime ordering/recovery foundation
UI platform/theme foundation
package architecture foundation
```

---

# 26. FE-ARCH-CHG-013 — Consequential foundation choice requires decision history

If future teams would reasonably ask:

```text
"Why is the whole frontend built this way?"
```

and reversing it would affect many packages/hosts, record the decision.

---

# 27. When ADR is not required

Routine feature design does not need an ADR when canonical rules already determine the answer.

Examples:

```text
new Board component
new query under current scope model
new route under current host model
new allowed feature behavior without graph change
bug fix
test addition
```

---

# 28. FE-ARCH-CHG-014 — Do not create ADRs for routine implementation choices

Excess ADRs hide important decisions in noise.

Use feature spec/code review/local tests for routine work.

---

# 29. Architecture exception

A temporary exception, where repository governance permits one, is:

```text
temporary permission
```

not:

```text
second architecture
```

---

# 30. FE-ARCH-CHG-015 — Exception is bounded and temporary

An exception MUST include:

```text
owner
scope
reason
risk
removal condition
review trigger
proof
```

Another team/package MUST NOT copy it as precedent.

---

# 31. Manifest change

`architecture-manifest.ts` is executable package authority.

Changing:

```text
layer
freezeScope
allowedInternalImports
package inventory
```

can be architecture-affecting.

---

# 32. FE-ARCH-CHG-016 — Manifest edit is not the first step for a forbidden import

First determine:

```text
correct owner?
correct provider?
could behavior move inward?
is a public contract missing?
is architecture actually changing?
```

Then edit the manifest if justified.

---

# 33. Allowed import addition

A new edge broadens the consumer's dependency permission.

---

# 34. FE-ARCH-CHG-017 — New edge requires least-privilege rationale

Review must explain:

```text
consumer responsibility
provider responsibility
why edge is stable
runtime/mobile impact
why composition/move is worse
```

“Needed to compile” is not enough.

---

# 35. Allowed import removal

Removing stale permission can tighten architecture.

It may require consumer migration if actively used.

---

# 36. FE-ARCH-CHG-018 — Remove permission only after source no longer requires it

Manifest and source should move atomically.

Do not create transient red mainline unless a staged migration explicitly manages it.

---

# 37. Layer change

Moving a package between:

```text
feature
foundation
product-core
product-state
runtime
adapter
app
```

changes reuse/dependency expectations.

---

# 38. FE-ARCH-CHG-019 — Layer change is architecture-significant

Document:

```text
old responsibility
new responsibility
consumer/provider graph
runtime neutrality
migration
```

Do not call it folder cleanup.

---

# 39. New package

A new package can be normal structural work or architecture-significant depending on boundary.

It requires explicit owner and manifest entry.

---

# 40. FE-ARCH-CHG-020 — Package creation is justified by boundary value

Valid reasons include:

```text
semantic ownership
runtime separation
stable public contract
team parallelism
independent verification
```

Invalid default:

```text
folder is large
another product has this package
```

---

# 41. Package removal/merge

Removing a package can collapse an architectural boundary.

---

# 42. FE-ARCH-CHG-021 — Package removal proves semantic owner continuity

Before removal:

```text
identify new owner
migrate consumers
preserve runtime safety
remove old exports/manifest/docs
```

Do not leave duplicated old/new authority.

---

# 43. Package rename

A rename can be:

```text
same owner/new name
```

or:

```text
ownership migration disguised as rename
```

---

# 44. FE-ARCH-CHG-022 — Rename classification is explicit

If semantics/layer change, treat it as architecture migration, not mechanical rename.

---

# 45. Public exports

Package public exports are internal monorepo contracts.

A new export increases coupling.

---

# 46. FE-ARCH-CHG-023 — Public export change receives contract review

For addition/removal/rename review:

```text
consumer set
semantic stability
deep-import migration
platform safety
compatibility period
```

Do not export internals solely to bypass architecture.

---

# 47. Deep import migration

A deep import found in source is normally source debt.

The correct fix can be:

```text
move code
add legitimate public export
change consumer owner
```

---

# 48. FE-ARCH-CHG-024 — Deep import never becomes precedent by repetition

Three existing deep imports do not justify a fourth.

Classify and migrate them.

---

# 49. New host

Adding:

```text
desktop
extension
embedded
another web app
```

is a consequential architecture decision.

---

# 50. FE-ARCH-CHG-025 — New host requires ADR and host architecture plan

At minimum decide:

```text
framework/runtime
navigation
UI platform
contracts
state/query
realtime
auth/session
testing
build/deployment
manifest
```

Do not create `apps/foo` and let it import everything.

---

# 51. Host framework replacement

Changing Vite/Expo/Next or the host framework foundation is consequential.

---

# 52. FE-ARCH-CHG-026 — Host framework replacement preserves inner contracts where practical

Migration should isolate:

```text
router
environment
runtime
rendering
```

outward.

Do not rewrite product core/state solely because the host framework changed.

---

# 53. Microfrontend/runtime federation

Current package architecture is not a microfrontend deployment architecture.

---

# 54. FE-ARCH-CHG-027 — Microfrontend adoption requires a new ADR

It changes:

```text
deployment boundary
runtime dependency loading
version compatibility
shared state/auth
observability
failure isolation
```

and cannot be introduced as a bundler optimization.

---

# 55. Runtime boundary

Moving browser/native APIs inward changes platform coupling.

---

# 56. FE-ARCH-CHG-028 — Runtime ownership changes require platform-impact review

Check:

```text
web
mobile
marketing
testability
secret/storage lifecycle
service disposal
```

before moving runtime behavior.

---

# 57. Service locator

Introducing a global dynamic service registry changes dependency visibility.

---

# 58. FE-ARCH-CHG-029 — Global service locator requires explicit architecture decision

The default remains:

```text
typed explicit composition
```

Do not introduce hidden runtime dependency graph for convenience.

---

# 59. State authority change

Changing where server state lives is consequential.

Examples:

```text
TanStack Query cache
→ persistent local database

product query owner
→ global app store

workspace-keyed cache
→ tenant-blind shared cache
```

---

# 60. FE-ARCH-CHG-030 — State authority migration requires ADR when foundation changes

Review:

```text
source of truth
scope isolation
offline
persistence
realtime
optimistic mutation
principal transition
migration
```

Do not treat it as library refactor.

---

# 61. Query library replacement

Replacing TanStack Query can be implementation or architecture change depending on whether state contracts/lifecycle change.

---

# 62. FE-ARCH-CHG-031 — Mechanism replacement does not automatically require ADR

If:

```text
server authority
scope model
cache ownership
mutation lifecycle
```

remain unchanged, a library migration can use a migration plan without new foundation decision.

If those semantics change, ADR is warranted.

---

# 63. Persisted/offline cache

Adding durable server-state persistence changes security/lifecycle semantics.

---

# 64. FE-ARCH-CHG-032 — Broad persisted cache requires architecture decision

Define:

```text
data classes
encryption/storage
principal/account scope
expiry
logout
migration
offline writes
```

before implementation.

---

# 65. Realtime foundation

Changing:

```text
event identity
ordering scope
gap/recovery model
subscription model
connection ownership
```

is architecture-significant.

---

# 66. FE-ARCH-CHG-033 — Realtime ordering/recovery foundation change requires durable rationale

Especially when it alters:

```text
sequence
replay
snapshot recovery
checkpoint
```

update producer/client compatibility and tests.

---

# 67. Realtime adapter addition

Adding a normal event adapter within the accepted model is feature/product work.

---

# 68. FE-ARCH-CHG-034 — New adapter is not ADR-worthy by default

Use current:

```text
generated event
→ adapter
→ state owner
```

architecture and test it.

ADR only if the dispatch/ownership foundation changes.

---

# 69. API/generated contract pipeline

Changing codegen producer/path/generator/public generated surface can be architecture-significant.

---

# 70. FE-ARCH-CHG-035 — Contract-generation foundation is governed

Update:

```text
producer
generator
exports
docs
codegen drift
consumer migration
```

together.

Do not create parallel handwritten generated-contract authority.

---

# 71. Backend contract change

A backend API change can affect frontend but is not approved solely by frontend policy.

---

# 72. FE-ARCH-CHG-036 — Frontend records consumer impact; backend owns producer approval

Coordinate through system/backend contract-first delivery.

Frontend adapts after public contract intent is established.

---

# 73. Auth/session foundation

Changing credential/session model affects runtime, cache, realtime, routing, security.

---

# 74. FE-ARCH-CHG-037 — Auth/session foundation change requires ADR

Examples:

```text
cookie session → token storage model
refresh ownership change
multi-account session foundation
```

are durable security/runtime choices.

---

# 75. CSRF reconciliation

A mismatch against accepted backend browser contract is source/contract debt, not automatically a new frontend architecture.

---

# 76. FE-ARCH-CHG-038 — Repair contract drift without inventing a new decision

If the intended backend CSRF contract remains:

```text
csrf_token + X-CSRF-Token
```

fix frontend source/tests.

Create new ADR only if the intended security contract itself changes.

---

# 77. UI/design-system foundation

Changing:

```text
token authority
appearance/accent theme model
ui-web/ui-mobile split
density foundation
public primitive API
```

can be architecture-significant.

---

# 78. FE-ARCH-CHG-039 — Foundational UI changes require migration across evidence

Update:

```text
tokens/components
consumers
Storybook
a11y
visual baselines
docs
```

atomically.

ADR if the durable model changes materially.

---

# 79. Marketing redesign

A marketing page/section redesign is usually product/design implementation, not frontend architecture.

---

# 80. FE-ARCH-CHG-040 — Visual redesign does not require ADR when design-system architecture remains intact

Use existing:

```text
tokens
theme
UI primitives
marketing owner
```

and update component/visual tests.

---

# 81. Testing/gate foundation

Changing required test taxonomy/gate set/zero-test model/final CI gate can alter engineering governance.

---

# 82. FE-ARCH-CHG-041 — Critical gate-foundation change is architecture/governance change

If a required property is removed/replaced:

```text
name replacement evidence
update CI
update testing architecture
update tooling tests
```

and use ADR when the durable quality foundation is materially changed.

---

# 83. CI optimization

Reordering/parallelizing jobs can be operational when protected properties/final AND semantics remain.

---

# 84. FE-ARCH-CHG-042 — CI optimization preserves evidence semantics

You MAY:

```text
cache
parallelize
split
combine operationally
```

provided:

```text
required properties still execute
final certification still requires them
scope remains observable
```

---

# 85. Freeze scope

Manifest `freezeScope` indicates architecture coverage, not feature completion.

Changing it changes what the architecture gate considers core/verification/isolated.

---

# 86. FE-ARCH-CHG-043 — Freeze-scope change requires explicit rationale

Do not move a problematic production package to:

```text
verification
```

or isolated scope merely to avoid production rules.

---

# 87. Architecture migration

A consequential change needs a migration plan when old/new structures must coexist.

---

# 88. FE-ARCH-CHG-044 — Migration has explicit phases

Typical phases:

```text
prepare
introduce compatible target
migrate consumers
cut over authority
verify
remove old path
```

Do not leave the repository permanently in “migration mode.”

---

# 89. Compatibility layer

Aliases/adapters can ease migration.

---

# 90. FE-ARCH-CHG-045 — Compatibility layer has removal condition

Document:

```text
who still uses it
what proves migration complete
when it can be deleted
```

Do not let alias become second permanent public API.

---

# 91. Atomicity

Architecture changes often touch multiple evidence classes.

---

# 92. FE-ARCH-CHG-046 — Semantic architecture change updates all affected authorities in one governed transaction

As applicable:

```text
canonical architecture
ADR
manifest
package manifests/exports
source
generated docs
tests
CI
migration
```

If staged, document stage boundaries and temporary compatibility.

---

# 93. Generated evidence

After manifest change regenerate:

```text
frontend/docs/generated/package-boundaries.md
```

through its producer.

---

# 94. FE-ARCH-CHG-047 — Generated evidence is output, not a review shortcut

The generated diff shows exact graph effect.

Reviewer still evaluates whether that graph change is architecturally justified.

---

# 95. Architecture test update

If a new machine-detectable rule is introduced, implement its gate where reliable.

---

# 96. FE-ARCH-CHG-048 — Canonical MUST without executable protection is reviewed for gateability

Not every semantic rule can be machine-enforced.

But package/import/runtime-source boundaries often can and should be.

---

# 97. Removing architecture test

A gate can be removed only if rule retired or replaced.

---

# 98. FE-ARCH-CHG-049 — Do not delete the gate while keeping the rule unenforced accidentally

State:

```text
new proof
or
rule retirement
```

in the same change.

---

# 99. Review ownership

Architecture review should involve owners affected by dependency/runtime/public contract changes.

---

# 100. FE-ARCH-CHG-050 — Architecture review is cross-boundary where impact is cross-boundary

Examples:

```text
mobile graph change
→ mobile owner

UI foundation
→ UI/platform owners

backend contract
→ backend/system producer owner

auth/session
→ security/runtime owner
```

Do not approve solely inside the consuming feature team when the foundation changes.

---

# 101. Feature-team autonomy

The architecture should enable teams to work without central approval for routine in-boundary features.

---

# 102. FE-ARCH-CHG-051 — Governance protects foundations, not every line of feature code

Avoid turning architecture review into bottleneck for ordinary feature behavior that follows current owners/contracts.

---

# 103. Evidence requirements

Architecture change must prove:

```text
new structure
migration correctness
negative forbidden edges
host/mobile safety
generated synchronization
affected behavior
```

as applicable.

---

# 104. FE-ARCH-CHG-052 — Architecture change requires stronger negative proof

Examples:

```text
old import path no longer used
mobile still cannot import web
old package removed
generated graph matches
old cache authority no longer active
```

---

# 105. Change classification record

For non-trivial architecture PR/spec include a short classification.

---

# 106. FE-ARCH-CHG-053 — Classification states old rule and new rule

A reviewer should see:

```text
Before:
A owns X / edge forbidden

After:
B owns X / edge allowed

Reason:
...

Migration:
...

Proof:
...
```

Do not bury the architecture delta inside a large diff.

---

# 107. Decision alternatives

When creating ADR, record real alternatives considered.

---

# 108. FE-ARCH-CHG-054 — Do not fabricate ADR alternatives

If historical/current work did not meaningfully consider an option, do not add it just to fill a template.

Unknown historical rationale remains unknown.

---

# 109. Decision date/owner

New ADR records current decision date and current decision owners.

Historical normalization does not infer missing authorship.

---

# 110. FE-ARCH-CHG-055 — Current stewardship and historical authorship are distinct

Do not rewrite an old ADR to imply current team authored the original decision.

---

# 111. Supersession

When decision changes, old ADR status/links reflect supersession.

---

# 112. FE-ARCH-CHG-056 — Supersession is explicit and bidirectional

New ADR:

```text
Supersedes: FE-ADR-NNN
```

Old ADR:

```text
Superseded By: FE-ADR-MMM
```

according to repository decision schema.

---

# 113. ADR ID

Frontend IDs are immutable:

```text
FE-ADR-NNN
```

Use next available ID after checking current/concurrent registry.

---

# 114. FE-ARCH-CHG-057 — Do not pre-reserve speculative ADR IDs in architecture docs

Assign when the real decision is created.

Avoid placeholder “FE-ADR-006 maybe” authority.

---

# 115. Documentation routing

After architecture change ensure README/docs router points to the same canonical owner if topic/files changed.

---

# 116. FE-ARCH-CHG-058 — New architecture topic requires a real authority gap

Do not add another architecture Markdown file because a PR is large.

Create a topic owner only when existing canonical ownership cannot remain clear.

---

# 117. Package-local docs

A package README can explain local operational details.

It does not override architecture.

---

# 118. FE-ARCH-CHG-059 — Local docs route outward for global rules

Do not copy the manifest/dependency policy into package README.

Reference the canonical owner.

---

# 119. Legacy architecture docs

Retired files such as:

```text
frontend/ARCHITECTURE.md
frontend/RULES.md
frontend/MIGRATION_TRACKER.md
```

must not reappear as competing authority.

---

# 120. FE-ARCH-CHG-060 — Migrate unique knowledge, then retire old authority

Do not delete unique durable knowledge before rehoming it.

Do not keep old authority indefinitely after migration.

---

# 121. Source debt discovery during feature work

A feature may uncover unrelated architecture debt.

---

# 122. FE-ARCH-CHG-061 — Do not expand feature scope silently to redesign foundation

If debt blocks the feature:

```text
classify
fix narrowly if target is already decided
or
create separate architecture change
```

Do not invent a broad redesign inside the feature PR.

---

# 123. Urgent fix

A production incident may require temporary compatibility/exception.

---

# 124. FE-ARCH-CHG-062 — Urgency does not make temporary architecture permanent

Record:

```text
temporary boundary
risk
owner
follow-up/removal condition
```

Then remove/reconcile after incident.

---

# 125. Security issue

Security fixes can require immediate source change.

Durable new security architecture still needs documentation/decision synchronization.

---

# 126. FE-ARCH-CHG-063 — Security patch and architecture record converge

Do not delay critical mitigation for documentation ceremony.

But before considering foundation complete:

```text
canonical docs
tests/gates
ADR if consequential
```

must reflect the durable model.

---

# 127. Backward compatibility

Frontend assets can be cached and backend/frontend deployments can be mixed.

Architecture migration must consider version overlap.

---

# 128. FE-ARCH-CHG-064 — Client/server compatibility assumes non-atomic rollout unless proven otherwise

For public contract changes assess:

```text
old client → new server
new client → old server
```

as applicable.

Do not assume simultaneous deploy by default.

---

# 129. Internal package compatibility

Monorepo packages usually ship together, but tests/builds still depend on synchronized public exports.

---

# 130. FE-ARCH-CHG-065 — Monorepo atomic source does not justify unbounded internal contracts

Stable exports/dependency rules still improve parallel work and refactor safety.

Do not deep-import because packages release together.

---

# 131. Rollback

Architecture migration should assess rollback/forward recovery.

---

# 132. FE-ARCH-CHG-066 — Rollback claim includes durable/client-cache effects

Examples:

```text
persisted cache schema
local storage key
auth storage migration
route contract
generated client compatibility
```

can survive a code rollback.

Plan accordingly.

---

# 133. Removal proof

Old path is removed only after consumers migrate.

---

# 134. FE-ARCH-CHG-067 — Completion is evidence-based

Examples:

```text
no imports of old package
old export unused
generated graph no longer contains edge
compatibility adapter unreferenced
tests pass without old path
```

not calendar-based “migration should be done.”

---

# 135. Architecture certification

After architecture change use exact-revision gates.

---

# 136. FE-ARCH-CHG-068 — Old green CI does not certify new architecture

Run:

```text
architecture
generated docs
affected tests
host builds
broader required CI
```

for the changed revision.

---

# 137. New package workflow

```text
1. define semantic owner
2. choose layer/freeze scope
3. define least-privilege imports
4. define public exports
5. add package/workspace/manifest
6. add tests
7. regenerate package boundaries
8. run architecture/tooling/consumer gates
```

---

# 138. FE-ARCH-CHG-069 — Package appears in manifest exactly once

No shadow/unregistered package.

No stale duplicate path.

The closed-world checker remains authoritative.

---

# 139. New dependency workflow

```text
1. identify need
2. challenge ownership
3. confirm public contract
4. check runtime/mobile
5. edit package manifest
6. edit architecture manifest if needed
7. regenerate docs
8. run gates
```

---

# 140. FE-ARCH-CHG-070 — Dependency graph is reviewed before code proliferation

Resolve the edge early.

Do not spread imports across many files then ask architecture review at the end.

---

# 141. State migration workflow

```text
1. old state authority
2. new state authority
3. query/cache migration
4. optimistic/realtime overlap
5. principal/scope lifecycle
6. compatibility
7. tests
8. remove old owner
```

---

# 142. FE-ARCH-CHG-071 — Two state authorities cannot remain indefinitely

During migration define one active truth per phase.

Avoid:

```text
query cache
+
global store
```

both accepting independent mutations forever.

---

# 143. Realtime migration workflow

```text
1. producer contract
2. old/new event compatibility
3. transport changes
4. adapter changes
5. sequence/recovery
6. state convergence
7. tests
8. rollout/removal
```

---

# 144. FE-ARCH-CHG-072 — Realtime migration proves gap/duplicate/order behavior

Do not certify only connection success.

The event-state correctness properties must survive migration.

---

# 145. UI foundation migration workflow

```text
1. semantic token/component contract
2. new implementation
3. consumer migration
4. light/dark/accent
5. accessibility
6. visual diff
7. remove old token/variant
```

---

# 146. FE-ARCH-CHG-073 — UI migration avoids permanent parallel design systems

Do not keep:

```text
legacy-primary
new-primary
marketing-primary
```

without an explicit transition/removal plan.

---

# 147. Test-foundation migration workflow

```text
1. protected property
2. old gate
3. new gate
4. overlap if needed
5. CI final gate update
6. tooling tests
7. remove old gate
```

---

# 148. FE-ARCH-CHG-074 — Gate migration has no unprotected window

During migration, the critical property remains protected by old or new evidence.

Do not remove first and “add replacement later.”

---

# 149. Review checklist

```text
[ ] classification
[ ] old architecture
[ ] new architecture
[ ] ADR required?
[ ] package graph
[ ] public exports
[ ] host/runtime
[ ] web/mobile/marketing
[ ] contract compatibility
[ ] state/realtime
[ ] UI/accessibility
[ ] generated artifacts
[ ] tests/gates
[ ] migration/removal
[ ] exact-revision proof
```

---

# 150. ADR-required checklist

```text
[ ] durable/costly choice
[ ] context/problem
[ ] real alternatives
[ ] decision
[ ] consequences
[ ] compatibility/migration
[ ] evidence
[ ] supersession
[ ] canonical architecture update
```

---

# 151. Exception checklist

```text
[ ] exact rule being bypassed
[ ] narrow package/file/scope
[ ] reason
[ ] risk
[ ] owner
[ ] expiry/review trigger
[ ] removal condition
[ ] negative containment test if possible
```

---

# 152. Stop conditions

Stop implementation/review if:

- a forbidden import is being solved by editing allow-list before ownership analysis;
- an Accepted FE ADR is being rewritten instead of superseded;
- a new package/layer exists only for symmetry;
- a package is moved to foundation solely to make imports easy;
- a public export is widened only for test convenience;
- mobile starts depending on web runtime/UI;
- a new host is added without ADR;
- server state authority is moved into a global/persistent client store without architecture review;
- realtime recovery/order foundation changes without compatibility tests;
- a critical CI gate is removed without replacement property;
- generated package docs are hand-edited;
- source debt is being documented as the new architecture without deliberate decision;
- an UNRESOLVED question is being answered by coding-agent preference;
- a transition has no owner/removal condition;
- old and new architecture authorities are both left active indefinitely;
- prior green CI is cited after architecture source changed.

---

# 153. Executable evidence

Primary current sources:

```text
frontend/docs/architecture/
frontend/docs/decisions/
frontend/tooling/dependency-rules/src/architecture-manifest.ts
frontend/docs/generated/package-boundaries.md
frontend/**/package.json
frontend/package.json
frontend/tooling/
.github/workflows/fe-ci.yml
```

Current FE ADR registry explicitly says:

```text
current architecture lives under ../architecture
decision change supersedes old ADR instead of rewriting history
```

---

# 154. Related repository governance

Also use:

```text
docs/governance/documentation-authority.md
docs/governance/documentation-lifecycle.md
docs/governance/decision-and-exception-policy.md
docs/delivery/change-classification.md
docs/delivery/migration-policy.md
docs/delivery/definition-of-done.md
```

for repository-wide decision/migration rules.

---

# 155. Related frontend architecture

This policy governs changes to:

```text
frontend-overview.md
dependency-boundaries.md
hosts-composition-routing.md
api-and-contracts.md
state-query-mutations.md
realtime.md
ui-and-design-system.md
testing-and-quality-gates.md
```

Each remains the current semantic owner for its topic.

---

# 156. Explicit non-responsibilities

This policy does not:

```text
approve backend Domain/Application changes
approve product semantics
replace feature specification
replace migration plan for a complex change
make every package edit ADR-worthy
```

It tells frontend teams when and how durable architecture itself may change.

---

# 157. Final architecture-change model

Use this decision flow:

```text
requested change
        ↓
Does current architecture already define a safe owner/path?
        ├─ YES
        │   ↓
        │ routine feature/refactor/bug fix
        │   ↓
        │ implement + affected proof
        │
        └─ NO / conflict
            ↓
        classify:
        DOC_STALE
        SOURCE_DEBT
        TRANSITION
        CONTRACT_CHANGE
        UNRESOLVED
            ↓
        Is durable architecture intentionally changing?
            ├─ NO
            │   ↓
            │ repair stale/debt/transition
            │
            └─ YES
                ↓
            ADR if consequential
                ↓
            update canonical architecture
            + manifest/contracts/source
            + generated evidence
            + migration
            + tests/CI
                ↓
            remove old authority/path
```

The policy succeeds when feature teams can move quickly inside stable boundaries, while durable frontend foundations cannot drift through convenience edits, hidden allow-list expansion, rewritten ADR history, or weakened evidence.

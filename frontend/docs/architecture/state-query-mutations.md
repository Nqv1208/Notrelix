---
document_id: FE-ARCH-STATE-QUERY-MUTATIONS
document_type: architecture
status: active
owner: frontend-platform
applies_to:
  - frontend-server-state
  - frontend-query
  - frontend-cache
  - frontend-mutations
  - frontend-optimistic-updates
  - frontend-scope-transitions
  - frontend-client-state
evidence:
  - frontend/packages/foundation/query/
  - frontend/packages/product/work-management/state/
  - frontend/packages/product/automation/state/
  - frontend/packages/features/
  - frontend/apps/web/src/providers/
  - frontend/apps/mobile/src/providers/
  - frontend/tooling/dependency-rules/src/architecture-manifest.ts
review_on:
  - query-key-model-change
  - cache-ownership-change
  - mutation-lifecycle-change
  - optimistic-update-foundation-change
  - account-workspace-transition-change
  - client-persistence-change
  - query-library-foundation-change
---

# State, Query, and Mutations

> **Backend is authoritative for server state. Frontend query/cache is a scoped, derived representation used for interaction and performance.**
>
> Every server-state area has an explicit owner. Query identity must include sufficient scope to prevent cross-account/workspace/resource leakage. Mutations must define submission, optimistic admission when used, rollback/conflict behavior, authoritative reconciliation, and stale-response protection.

This document is the canonical frontend owner for:

- server-state versus local-state classification;
- QueryClient responsibility;
- query-key roots and scope;
- product query-key factories;
- query ownership;
- cache ownership;
- query fetch/retry/staleness defaults;
- mutation ownership;
- optimistic updates;
- rollback;
- conflict handling;
- invalidation versus cache patching;
- authoritative convergence;
- cancellation;
- stale response/race handling;
- principal/account/workspace transitions;
- pagination/filter/sort cache identity;
- local/global stores;
- client persistence;
- derived UI state.

It does not define:

- backend mutation invariants;
- backend authorization;
- REST wire contract details;
- realtime delivery ordering/gap mechanics;
- UI component design.

---

# 1. State architecture objective

The frontend state model should remain:

```text
backend authoritative resource state
        ↓
query/cache representation
        ↓
product/feature derived state
        ↓
view model / UI interaction state
```

with optimistic projections converging back to server truth.

Avoid:

```text
backend
+
permanent duplicate client database
+
realtime shadow state
+
component-local copies
```

for the same resource.

---

# 2. FE-STATE-001 — Backend is authoritative for server state

For server-owned resources, the frontend cache/store MUST NOT independently become the durable source of truth.

Examples:

```text
Board
Item
Page
Automation
Workspace
Billing state
permissions
```

remain backend-authoritative.

---

# 3. State classes

Before storing data classify it as:

```text
server state
URL/navigation state
local interaction state
form draft
runtime/session state
persisted client preference
ephemeral realtime/presence state
```

Different classes have different owners/lifetimes.

---

# 4. FE-STATE-002 — State class determines owner

Do not put all state into one global store.

Examples:

```text
server Board
→ product query/state owner

selected tab
→ route/local state

open menu
→ local component state

theme preference
→ UI/runtime preference owner
```

---

# 5. Query foundation

Current foundation package:

```text
@notrelix/query
```

depends on:

```text
@notrelix/kernel
@tanstack/react-query
```

and provides generic query mechanisms.

Product state packages build resource semantics on top.

---

# 6. FE-STATE-003 — Query foundation owns mechanism, not every resource

Foundation MAY own:

```text
QueryClient construction
scope helpers
generic optimistic command
generic assertions
```

It MUST NOT contain:

```text
every Board query key
every Page invalidation rule
every Billing mutation
```

Product/feature state owns resource semantics.

---

# 7. Query client

Current `createQueryClient()` configures default query/mutation behavior including:

```text
stale time
garbage-collection time
window-focus refetch behavior
query retry classification
mutation retry disabled
```

These are current defaults, not immutable architecture values.

---

# 8. FE-STATE-004 — Query client defaults are mechanism policy, not product truth

A product query MAY override generic defaults when justified.

Do not encode a product lifecycle assumption solely because global stale time is 30 seconds today.

---

# 9. Current generic retry

Current query client avoids retry for `AppError` kinds including:

```text
auth
forbidden
not_found
conflict
validation
```

and bounds ordinary retries.

Mutations do not retry by default.

---

# 10. FE-STATE-005 — Retry is error/operation-aware

Do not retry:

```text
validation
forbidden
conflict
non-idempotent mutation
```

as if all failures were transient.

Retry belongs to the operation/error contract.

---

# 11. Query-key root model

Current query foundation defines canonical roots:

```text
global
account
workspace
```

with helper constructors.

The helper also provides query-key validation for tests/tooling.

---

# 12. FE-STATE-006 — Every server-state query key has an explicit scope root

A server-state key SHOULD start with one of the approved scope classes.

Do not use ambiguous keys such as:

```text
["boards"]
["user"]
["settings"]
```

when the resource belongs to a broader server scope.

---

# 13. Global scope

`global` is for data genuinely independent from current Account/Workspace.

Use it sparingly.

---

# 14. FE-STATE-007 — Global scope is semantic, not convenience

Do not use `global` merely to avoid passing:

```text
accountId
workspaceId
```

If data changes when tenant scope changes, it is not global.

---

# 15. Workspace scope

Current `workspaceQueryKey` includes:

```text
["workspace", workspaceId, resource, ...segments]
```

and rejects missing/empty workspace IDs through assertion tooling.

---

# 16. FE-STATE-008 — Workspace-scoped key includes Workspace identity

Workspace resources MUST NOT share a key across Workspace identities.

This is a tenant-isolation and correctness property.

---

# 17. Work Management query keys

Current Work Management state explicitly requires `workspaceId` and builds all keys through `workspaceQueryKey`.

Current source states there is no compatibility overload without Workspace ID.

This is strong current alignment with the architecture.

---

# 18. FE-STATE-009 — Product key factory is the owner of resource key structure

Product/feature packages SHOULD define a stable key factory for their server-state area.

Components SHOULD NOT assemble arbitrary tuple keys independently.

---

# 19. Key hierarchy

A useful key hierarchy can support:

```text
all capability state
list
detail
sub-resource
filter/page
```

within a scope.

The exact hierarchy is product-owned.

---

# 20. FE-STATE-010 — Parent/child key hierarchy is deliberate

If invalidating a parent key is intended to invalidate children by TanStack Query prefix matching, design the tuple accordingly.

Do not depend on accidental tuple ordering.

---

# 21. Account scope

Current foundation `accountQueryKey(resource, ...segments)` uses an `account` root but does not encode an explicit `accountId` parameter.

That helper alone does not prove cross-account isolation.

---

# 22. FE-STATE-011 — Account-scoped cache must distinguish Account identity or be hard-reset on Account transition

Canonical requirement:

```text
Account A data
MUST NOT
be reusable as Account B data
```

This can be achieved by:

```text
account ID in key
or
a proven hard cache boundary/recreation on account change
```

but the behavior must be explicit and tested.

---

# 23. Current account-scope alignment risk

Because the current generic account helper is not ID-bearing:

```text
["account", resource, ...]
```

its safe use depends on an explicit Account-transition cache lifecycle.

Current helper shape alone is insufficient evidence of that lifecycle.

Classification until proven by the owning transition implementation/tests:

```text
UNRESOLVED
```

for account-switch isolation when account-rooted queries are used.

Do not silently assume safety.

---

# 24. FE-STATE-012 — Query-key safety is proven by identity plus lifecycle

A correct-looking tuple is insufficient if stale scoped cache survives transition incorrectly.

Likewise a cache clear strategy is insufficient if asynchronous stale responses can repopulate the new scope.

Prove both.

---

# 25. Resource identity

Detail queries include stable resource identity inside the appropriate Account/Workspace scope.

---

# 26. FE-STATE-013 — Resource ID alone is insufficient for tenant-scoped cache

Avoid:

```text
["card", cardId]
```

when the same client can transition tenant scope and IDs should be contextualized.

Prefer the owning scope + resource identity.

---

# 27. Filter/sort/page identity

Query parameters that materially change server results participate in the key.

Examples:

```text
page
cursor
filter
sort
search
view projection
```

as applicable.

---

# 28. FE-STATE-014 — Same key means same authoritative query identity

If two requests can return different server result sets because of parameter differences, they SHOULD NOT share one cache key unless the difference is intentionally normalized away.

---

# 29. Query functions

Query functions live with the resource/state owner and call typed API services.

They return normalized/product-owned server-state representation.

---

# 30. FE-STATE-015 — Query function does not contain UI rendering state

Do not mix:

```text
open modal
selected row
hover
```

into server query result ownership.

Derive UI state separately.

---

# 31. Query owner

A product/feature area has one primary query/cache owner for a server-state representation.

Multiple views consume it.

---

# 32. FE-STATE-016 — Multiple views share server state rather than fork authoritative models

For Work Management:

```text
Kanban
Table
Calendar
Timeline
Dashboard
```

are views/projections over shared work data.

Do not create a separate permanent Item database per view.

---

# 33. Derived view state

Views may derive:

```text
grouping
sorting
layout
filtered projection
calendar buckets
timeline positions
```

from shared server state.

---

# 34. FE-STATE-017 — Derived projection is recomputable

If a view projection can be recomputed from authoritative cache + view config, avoid persisting it as a second independent server-state truth.

---

# 35. Cache ownership

The owning state package decides:

```text
which queries exist
how keys are structured
what mutations patch
what invalidates
what realtime reconciles
```

in collaboration with API/realtime architecture.

---

# 36. FE-STATE-018 — Cache mutation occurs through the owning state boundary

Avoid route/component code calling:

```text
queryClient.setQueryData(...)
```

on another product's key by tuple guess.

Expose an owner function/hook/adapter.

---

# 37. Cache helper

Current Work Management contains cache helpers such as:

```text
full-board-cache.ts
optimistic-card.ts
```

that centralize resource transformations.

This is current evidence of owned cache behavior.

---

# 38. FE-STATE-019 — Cache transformation uses product semantics from the correct owner

Generic query foundation can snapshot/rollback.

Product-specific transformations such as moving/updating a Card belong in Work Management core/state.

---

# 39. Mutation lifecycle

A mutation must define more than:

```text
POST request
```

It can affect:

```text
optimistic state
pending UI
conflict
rollback
cache reconciliation
invalidation
realtime
navigation
```

---

# 40. FE-STATE-020 — Mutation owner defines the full client lifecycle

For each mutation answer:

```text
submit how?
optimistic or pessimistic?
what cache changes?
what rollback?
what conflict?
what authoritative result?
what invalidation/refetch?
what realtime overlap?
```

before calling it complete.

---

# 41. Generic optimistic command

Current query foundation provides `executeOptimisticCommand()`.

Its current lifecycle is approximately:

```text
cancel target queries
        ↓
snapshot
        ↓
apply optimistic updates
        ↓
perform mutation
        ↓
optional reconcile
        ↓
on conflict: rollback or refetch policy
        ↓
on failure: reverse rollback unless refetch chosen
        ↓
finally invalidate targets
```

This is current mechanism evidence.

---

# 42. FE-STATE-021 — Optimistic update converges to server truth

Optimistic state is a temporary prediction.

After operation outcome, the state must converge through:

```text
server response patch
invalidation/refetch
realtime authoritative event
or
another explicit contract
```

Do not leave optimistic state indefinitely because the UI “looks right.”

---

# 43. Snapshot

Rollback requires capturing enough previous cache state.

Current generic mechanism records whether the query existed and its previous value.

---

# 44. FE-STATE-022 — Rollback restores absence as well as value

If optimistic update creates a cache entry that did not exist before, rollback should remove that entry rather than set `undefined` ambiguously.

Preserve pre-command cache existence semantics.

---

# 45. Multi-query optimistic update

One logical command can affect multiple cache entries.

The generic mechanism supports multiple updates/snapshots.

---

# 46. FE-STATE-023 — Multi-cache command rolls back as one logical operation

If a mutation optimistically changes:

```text
list
detail
aggregate view
```

and the command fails, rollback/reconcile all affected owned entries consistently.

Avoid partial optimistic success.

---

# 47. Duplicate optimistic targets

Current generic command rejects duplicate query keys in one command.

This avoids ambiguous multiple snapshots for the same key.

---

# 48. FE-STATE-024 — One logical optimistic command has one update plan per target key

Compose product transformation before registering the same query key twice.

Do not depend on update order for duplicate target declarations.

---

# 49. Mutation identity

Current generic optimistic command carries:

```text
commandId
correlationId
idempotencyKey
```

with defaults tied to command identity.

This supports transport tracing/idempotency integration.

---

# 50. FE-STATE-025 — Client command identity and optimistic projection belong to one logical operation

Retries/reconciliation should preserve the logical operation identity according to backend contract.

Do not create a second optimistic command because a transport retry occurred.

---

# 51. Optimistic admission

Not every mutation should be optimistic.

Use optimism when result is predictable and reversible.

---

# 52. FE-STATE-026 — Optimism is a UX optimization, not a default mutation architecture

Prefer pessimistic/explicit pending behavior when:

```text
server may reject frequently
external provider outcome is uncertain
operation is destructive/irreversible
rollback is misleading
conflict probability is high
```

---

# 53. Optimistic IDs

Create operations may need temporary/client-generated IDs.

The scheme must reconcile cleanly with authoritative server IDs.

---

# 54. FE-STATE-027 — Temporary identity never silently becomes permanent server identity

On success:

```text
map/replace temporary object
with authoritative server response/ID
```

or invalidate/refetch.

Do not keep a fake ID because the screen rendered it first.

---

# 55. Work Management optimistic card

Current Work Management can construct optimistic Card representations for create flows.

This is current evidence, not permission to invent backend-only fields arbitrarily.

---

# 56. FE-STATE-028 — Optimistic entity fills only predictable client fields

If server exclusively determines:

```text
permission-sensitive fields
canonical position
version
audit metadata
workflow outcome
```

do not invent them as authoritative.

Use temporary/pending representation or reconcile immediately.

---

# 57. Mutation `onMutate`

Some current Work Management hooks implement direct TanStack Query `onMutate` snapshot/patch logic.

Others use the generic optimistic command helper.

Both are current mechanisms.

---

# 58. FE-STATE-029 — Multiple mutation implementation styles must preserve one lifecycle contract

The architecture does not require every mutation to call one helper.

It requires consistent properties:

```text
cancel as needed
snapshot
optimistic patch
rollback
reconcile/invalidate
conflict handling
```

Do not let helper choice create semantic divergence.

---

# 59. Invalidation

Invalidation requests authoritative refetch according to query library lifecycle.

It is often safer than complex patching.

---

# 60. FE-STATE-030 — Invalidate when local patch cannot be proven complete

Prefer invalidation/refetch when server may change:

```text
multiple related resources
computed fields
permissions
ordering
side effects
```

beyond the client prediction.

---

# 61. Direct cache patch

Patching is useful when response contains enough authoritative data or transformation is fully known.

---

# 62. FE-STATE-031 — Authoritative response patch is stronger than optimistic guess

After success, prefer server-returned canonical representation where available.

Do not keep pre-success optimistic timestamps/versions if server returned real ones.

---

# 63. Finally invalidation

Current generic optimistic command invalidates configured target queries in `finally`, providing authoritative convergence even after success/failure.

This is a conservative mechanism.

---

# 64. FE-STATE-032 — Convergence path is explicit

Every optimistic command SHOULD have a defined final convergence path.

No command should depend on “eventually some other screen refetches.”

---

# 65. Conflict handling

Current generic mechanism identifies `AppError.kind === "conflict"` and allows product policy to choose:

```text
rollback
refetch
```

---

# 66. FE-STATE-033 — Conflict policy is product/operation-specific

Generic query foundation detects conflict.

The owning product decides whether conflict means:

```text
rollback
refetch
rebase
show compare UI
ask user
```

Do not centralize all conflicts into “retry once.”

---

# 67. Mutation retry

Current generic QueryClient disables mutation retries.

Operation-specific retry can still be implemented deliberately.

---

# 68. FE-STATE-034 — Non-idempotent mutation is never blindly auto-retried

Before retrying a mutation after timeout/network failure determine:

```text
did server possibly commit?
is idempotency supported?
can status be queried?
```

Transport uncertainty is not proof of failure.

---

# 69. Cancellation before optimistic update

Current generic optimistic command cancels ongoing refetches for target queries before applying optimistic state.

This reduces stale fetch overwrite.

---

# 70. FE-STATE-035 — Prevent stale in-flight query from overwriting optimistic state

When applying optimistic updates to a query, cancel/coordinate relevant in-flight fetches or use version/scope guards.

Do not let a pre-mutation response land afterward and erase the optimistic transition silently.

---

# 71. Stale response race

Scope changes and rapid query changes can produce old responses after new navigation.

Abort signals alone may not cover every completed response.

---

# 72. FE-STATE-036 — Old-scope response cannot populate new-scope identity

The strongest defense is correct query identity:

```text
Workspace A
→ key includes A

Workspace B
→ key includes B
```

plus lifecycle cleanup as needed.

Do not reuse one key and depend only on timing.

---

# 73. Workspace transition

Workspace change affects:

```text
query identity
cache visibility
permissions
realtime subscriptions
feature state
routes
```

Host coordinates transition; state/realtime owners perform their cleanup/rebind.

---

# 74. FE-STATE-037 — Workspace transition cannot expose old Workspace data under new Workspace context

During transition:

```text
old scope remains keyed as old
or
is cleared
```

and UI must not label it as new scope.

---

# 75. Account transition

Account transition can affect broader data:

```text
Workspace list
Billing
Governance
Integrations
account settings
```

The cache strategy must be explicit.

---

# 76. FE-STATE-038 — Account transition is a cache-security boundary

If account-rooted keys do not encode Account ID, the transition MUST clear/recreate all account-scoped cache before rendering the new Account.

This behavior requires tests.

---

# 77. Principal transition

Login/logout/user change is broader than Account switch.

Old principal data must not remain available as active cache state.

---

# 78. FE-STATE-039 — Principal change invalidates previous principal's protected state

On logout/session replacement:

```text
disconnect/rebind runtime
clear protected query state
clear scoped local persistence
reset feature state
```

according to architecture.

Do not leave private data visible after sign-out.

---

# 79. QueryClient lifetime

Current web/mobile host composition provides a host-level query client.

The detailed service lifetime belongs to host architecture.

---

# 80. FE-STATE-040 — QueryClient scope and cache isolation contract are explicit

If one QueryClient survives principal/account/workspace changes, scoped keys/clear logic must guarantee isolation.

Alternatively recreate the client at a broader security boundary.

Do not leave lifetime accidental.

---

# 81. Cache persistence

Persistent query cache/offline cache is not currently assumed as universal architecture.

Adding persistence changes security/lifecycle semantics.

---

# 82. FE-STATE-041 — Persisted server cache requires an explicit policy

Before persisting server state define:

```text
which data
tenant/principal scope
encryption/platform storage
expiry
logout clear
account/workspace transition
schema/version migration
offline semantics
```

Do not enable broad cache persistence globally by convenience.

---

# 83. Local stores

Local stores are appropriate for non-server state when ownership/lifecycle justify them.

Do not copy query results into another store solely to access them more easily.

---

# 84. FE-STATE-042 — Server-state duplication into local store requires a bounded reason

Valid cases may include:

```text
editable draft snapshot
offline queue
complex editor staging
```

with explicit reconciliation.

Invalid default:

```text
query data → Redux/Zustand/global context copy forever
```

---

# 85. Form drafts

A form draft can diverge intentionally from server state while editing.

It is not authoritative until mutation succeeds.

---

# 86. FE-STATE-043 — Form draft has explicit reset/rebase behavior

When server state changes while a form is open, define:

```text
ignore until submit
warn
rebase
reset
conflict
```

according to product UX.

Do not silently overwrite user edits.

---

# 87. URL state

Filters/view/navigation state can belong in URL for shareability/back-forward.

It can be mapped to query parameters/keys.

---

# 88. FE-STATE-044 — URL state is not duplicated into global store without purpose

Use one navigation authority when possible.

If mirrored for performance/UX, define synchronization direction.

---

# 89. Local UI state

Local interaction state stays local unless multiple owners genuinely need it.

Examples:

```text
popover open
selected temporary row
hover
focus
```

---

# 90. FE-STATE-045 — Locality is preferred for ephemeral UI state

Do not make every component interaction a package-wide store action.

Global state increases coupling and transition cleanup cost.

---

# 91. Derived state

Compute from current source state where cheap/stable.

Avoid storing both source and derived values unless needed.

---

# 92. FE-STATE-046 — Do not store recomputable derived state as competing authority

Examples:

```text
filtered cards
count of done items
grouped calendar buckets
```

can often be derived.

Stored derived state requires invalidation rules and can drift.

---

# 93. Selection state

Selection can be local/URL/product interaction state depending on whether it must survive navigation/share across components.

Classify intentionally.

---

# 94. FE-STATE-047 — Selection does not mutate server state unless product operation says so

Selecting a row/card is normally UI state.

Do not create server mutations for ephemeral selection merely to centralize state.

---

# 95. Pending mutation state

Mutation libraries expose pending/error/success.

Product UI can derive pending controls/feedback.

---

# 96. FE-STATE-048 — Pending is not committed

Do not mark durable completion while mutation is merely:

```text
pending
accepted
optimistically projected
```

Use authoritative success/completion semantics.

---

# 97. Long-running operations

A mutation may return accepted/operation status.

Store/query the operation according to backend contract.

---

# 98. FE-STATE-049 — Long-running command separates submission from execution status

Architecture:

```text
submit
→ accepted
→ operation state/event
→ completed/failed
```

Do not keep one boolean `isSuccess` as business completion when backend is asynchronous.

---

# 99. Realtime overlap

Realtime may deliver an update for the same mutation.

Detailed dedup/order/gap logic belongs to `realtime.md`.

State owner must still have a reconciliation contract.

---

# 100. FE-STATE-050 — REST mutation and realtime event converge on one state owner

Do not let:

```text
REST hook
and
realtime reducer
```

maintain independent copies of the same resource.

Both update/invalidate the owning cache/state.

---

# 101. Mutation event ordering

A realtime event can arrive before/after mutation response.

State architecture cannot rely on fixed arrival order.

---

# 102. FE-STATE-051 — Reconciliation is order-tolerant

Use:

```text
resource version
event identity
authoritative refetch
idempotent patch
```

as provided by contracts.

Do not apply blind “last callback wins” when ordering is not guaranteed.

---

# 103. Delete mutation

Optimistic delete can remove entity temporarily.

Rollback must restore correct position/relationships or refetch.

---

# 104. FE-STATE-052 — Destructive optimism requires stronger rollback proof

For delete/archive/move across containers, test:

```text
failure
conflict
server reorder
related list/detail cache
```

Do not rely only on happy path.

---

# 105. Create mutation

Optimistic create can insert a temporary entity.

Authoritative server result may assign:

```text
ID
position
version
audit metadata
defaults
```

---

# 106. FE-STATE-053 — Create success reconciles all server-assigned fields

Do not keep client-estimated:

```text
createdAt
position
status
version
```

when server result differs.

---

# 107. Update mutation

Patch mutation can optimistically merge predictable fields.

Server may normalize/compute others.

---

# 108. FE-STATE-054 — Partial update patch distinguishes omitted and explicit null

Client mapping must preserve backend contract semantics:

```text
omitted
≠
null
```

when the API uses both meanings.

Do not collapse them in optimistic update.

---

# 109. Move/reorder mutation

Move operations involve order/position semantics and can affect multiple collections.

Current Work Management `useMoveCard` uses generic optimistic command over full-board query.

---

# 110. FE-STATE-055 — Move optimism updates all affected containers consistently

When moving entity from A to B:

```text
remove from A
insert into B
update owning fields/order
```

as one logical optimistic state change.

Rollback reverses the entire move.

---

# 111. Server canonical ordering

Server may produce canonical fractional/order positions.

Client can predict for UX, but server result wins.

---

# 112. FE-STATE-056 — Client ordering projection is not persistence authority

If server rejects/rebalances/normalizes position:

```text
reconcile/refetch
```

Do not preserve a conflicting client order indefinitely.

---

# 113. Cache helper purity

Cache transformation functions should be pure where practical.

Pure functions are easier to test and reuse across optimistic/realtime paths.

---

# 114. FE-STATE-057 — Product cache transformer avoids hidden side effects

Prefer:

```text
old state + operation
→ new state
```

rather than transformer mutating unrelated globals/services.

---

# 115. Structural sharing

Immutable cache updates preserve React Query change detection and rollback safety.

---

# 116. FE-STATE-058 — Do not mutate cached objects in place unless library/owner contract explicitly permits it

Create new structures for changed branches.

In-place mutation can hide updates and corrupt snapshots.

---

# 117. Full aggregate cache

Current Work Management has a `FullBoardResponse` cache representation used by several hooks.

Large aggregate cache can simplify coordinated board views but has invalidation cost.

---

# 118. FE-STATE-059 — Aggregate cache is a product decision, not global default

Use an aggregate cache when:

```text
views need cohesive snapshot
mutation/realtime semantics can update/revalidate it
payload/performance is acceptable
```

Do not model every context as one giant response automatically.

---

# 119. Detail versus aggregate cache

Same entity may appear in:

```text
list
detail
aggregate
```

If multiple caches exist, mutation/realtime owner must reconcile them.

---

# 120. FE-STATE-060 — Duplicate representations require explicit fan-out or authoritative invalidation

Do not update only detail while list remains stale if both are simultaneously visible/meaningful.

Use owner-managed update/invalidate strategy.

---

# 121. Cache ownership boundary

A feature composing Work Management + Billing should not directly patch both caches ad hoc.

Each owner exposes mutation/state behavior.

---

# 122. FE-STATE-061 — Cross-context UI composition does not own cross-context cache mutation

A composition screen coordinates operations.

Each product/context remains responsible for its state.

---

# 123. Query enablement

Queries may be disabled until required scope/ID/session exists.

Avoid non-null assertion patterns that can execute with missing scope.

---

# 124. FE-STATE-062 — Query/mutation cannot execute with unresolved required scope

If `workspaceId` is required:

```text
validate/require it before query/mutation
```

Do not depend on `workspaceId!` if caller can actually pass undefined.

---

# 125. Current Work Management scope note

Some current mutation hooks accept optional `workspaceId` and then use a non-null assertion to build a workspace-scoped key.

This is a current implementation risk if call sites can pass `undefined`.

The canonical target remains:

```text
required Workspace scope
→ required typed/function input
or
explicit disabled/error path
```

---

# 126. FE-STATE-063 — Non-null assertion is not scope validation

TypeScript `!` removes compiler uncertainty.

It does not prove runtime Workspace identity exists.

Make required scope explicit in API/hook contract.

---

# 127. Query disabled state

When a resource ID/scope is not available, use an explicit query-disabled state or don't render the query owner.

Do not issue `undefined`/empty IDs to backend.

---

# 128. FE-STATE-064 — Missing identity is not a cache key

Avoid keys containing accidental:

```text
undefined
""
null
```

for a required resource/scope because they can merge unrelated pending states.

---

# 129. Error state

Query errors stay typed/normalized.

UI derives appropriate error experience.

Do not copy error object into long-lived server cache.

---

# 130. FE-STATE-065 — Error state is request/query state, not resource truth

A transient network error does not mean the resource is deleted.

Preserve last-known data/stale UX according to query policy where safe.

---

# 131. Permission change

Cached resource can become unauthorized.

Refetch/realtime/session transition must remove or hide data according to server result.

---

# 132. FE-STATE-066 — Permission-sensitive cache cannot remain visible indefinitely after revocation signal

When authoritative permission/session state changes:

```text
invalidate/clear relevant resource state
```

according to security architecture.

Do not rely only on hiding actions.

---

# 133. Not-found after deletion

After delete, a detail query may become not-found.

Navigate/remove state according to product UX.

Do not keep stale resource as active entity indefinitely.

---

# 134. FE-STATE-067 — Deletion outcome removes active ownership

Once authoritative delete succeeds:

```text
remove/mark according to product contract
invalidate related caches
```

Do not keep it as ordinary active cache because an old component still references it.

---

# 135. Soft-delete/archive semantics

Backend product semantics decide whether resource is archived/deleted/restorable.

Frontend cache models the public contract.

Do not infer lifecycle from HTTP alone.

---

# 136. FE-STATE-068 — Cache lifecycle follows product lifecycle

Do not translate every `DELETE` into physical disappearance if backend contract represents archive/soft-delete differently.

Use product contract.

---

# 137. Offline

Offline behavior is not globally assumed.

Queries may show stale cache; mutations may require online authority.

---

# 138. FE-STATE-069 — Offline mutation queue requires explicit architecture

Before queueing writes define:

```text
durable queue
idempotency
ordering
conflict
principal/account scope
logout behavior
replay
user visibility
```

Do not simply retry indefinitely later.

---

# 139. Network reconnect

On reconnect, stale server state may require refetch.

Realtime also has reconnect/gap handling.

---

# 140. FE-STATE-070 — Network recovery revalidates authority

Do not assume cached state remains current after long offline/reconnect.

Use query/realtime revalidation according to capability.

---

# 141. Cache time defaults

Current QueryClient uses finite stale/gc values.

Those values are tuning evidence.

---

# 142. FE-STATE-071 — Cache timing is not correctness boundary

Correct tenant isolation, conflict handling and authoritative convergence MUST NOT depend on “cache expires soon anyway.”

Time-based expiration is performance/freshness policy.

---

# 143. Refetch-on-focus

Current generic client disables automatic window-focus refetch.

Product/host can revalidate through other lifecycle rules.

---

# 144. FE-STATE-072 — Freshness policy is deliberate

If a security/operational screen requires stronger freshness, override/query explicitly.

Do not assume one global focus policy fits all resources.

---

# 145. Prefetch

Host/routes can prefetch product queries through state owner.

Prefetch should use the same query key/function.

---

# 146. FE-STATE-073 — Prefetch does not create a parallel cache contract

Use canonical query options/key.

Do not implement a second route-loader cache with different identity.

---

# 147. Initial data/hydration

SSR/marketing or future host hydration may seed query cache.

Hydrated data must preserve scope/version identity.

---

# 148. FE-STATE-074 — Hydration data is still server-derived cache

Do not treat server-rendered initial data as permanent local source.

Normal invalidation/reconciliation applies.

---

# 149. Test architecture

State proof should cover protected properties, not just hooks render.

Relevant categories:

```text
query-key scope
cache transforms
mutation lifecycle
optimistic rollback
conflict
scope transition
stale response
REST/realtime convergence
```

---

# 150. FE-STATE-075 — Query-key isolation has direct tests

For critical tenant-scoped keys prove:

```text
Workspace A key != Workspace B key
resource/filter differences are distinct
invalid required scope is rejected
```

This is cheaper and more reliable than relying only on E2E.

---

# 151. Optimistic tests

Current query foundation has unit tests for optimistic command.

Product mutations need product-specific cache/result tests.

---

# 152. FE-STATE-076 — Generic optimistic helper tests do not prove product transformation

A helper can rollback correctly while a product transformation moves the wrong Card/list.

Test both layers.

---

# 153. Transition tests

Account/workspace/principal switch should have lifecycle tests.

Especially when one QueryClient survives transitions.

---

# 154. FE-STATE-077 — Cross-scope negative proof is mandatory for scope architecture changes

Test that old-scope data does **not** appear after transition.

Positive “new Workspace loads” alone is insufficient.

---

# 155. Realtime/state tests

Detailed event semantics belong to realtime docs, but state owner tests should prove final cache result for relevant events.

---

# 156. FE-STATE-078 — Realtime and REST convergence tests target final authoritative state

Do not assert only that callback executed.

Assert cache/resource outcome and duplicate/order behavior as contract requires.

---

# 157. State architecture change

Changes to:

```text
scope root
QueryClient lifetime
persistent cache
global store authority
optimistic command foundation
product cache owner
```

can be architecture-significant.

Use architecture-change policy.

---

# 158. FE-STATE-079 — State authority change is consequential

Moving server state from:

```text
product query cache
→ global local store
```

or vice versa is not a mechanical refactor.

Review migration, realtime, scope, persistence and tests.

---

# 159. Query-library replacement

TanStack Query is current mechanism.

Architecture concepts should remain clear if library changes:

```text
server authority
scoped identity
cache owner
mutation lifecycle
convergence
```

---

# 160. FE-STATE-080 — Do not wrap every query API for hypothetical replacement

Preserve conceptual boundaries.

Introduce abstraction only where it owns real policy/reuse, not speculative library independence.

---

# 161. State drift classification

When code and this architecture disagree classify:

```text
DOC_STALE
SOURCE_DEBT
TRANSITION
CONTRACT_CHANGE
UNRESOLVED
```

Examples include missing scope, duplicate cache owner, stale transition logic.

---

# 162. FE-STATE-081 — Existing cache behavior is not automatic precedent

If a current hook uses an unsafe key or non-null assertion:

```text
classify/fix source debt
```

rather than copy the pattern into new state packages.

---

# 163. New query checklist

```text
[ ] server-state owner
[ ] scope root
[ ] account/workspace/resource identity
[ ] filter/page/sort identity
[ ] key factory
[ ] typed API function
[ ] retry/freshness policy
[ ] permission lifecycle
[ ] tests
```

---

# 164. New mutation checklist

```text
[ ] backend operation contract
[ ] logical command identity
[ ] optimistic decision
[ ] target cache keys
[ ] cancel in-flight query if required
[ ] snapshot/rollback
[ ] conflict behavior
[ ] authoritative result/reconcile
[ ] invalidation
[ ] realtime overlap
[ ] pending/error UX
[ ] tests
```

---

# 165. Workspace transition checklist

```text
[ ] route/scope identity
[ ] query keys
[ ] old cache visibility
[ ] in-flight old requests
[ ] feature-local state
[ ] realtime subscriptions
[ ] permission state
[ ] pending mutations
[ ] negative old-scope test
```

---

# 166. Account/principal transition checklist

```text
[ ] QueryClient lifetime
[ ] account-scoped key isolation
[ ] protected cache clear
[ ] Workspace inventory reset
[ ] persisted state cleanup
[ ] runtime/realtime rebind
[ ] route/navigation
[ ] old-principal negative test
```

---

# 167. Optimistic update checklist

```text
[ ] prediction deterministic enough
[ ] temporary IDs handled
[ ] all affected caches identified
[ ] snapshots captured
[ ] reverse rollback
[ ] conflict path
[ ] server result reconciliation
[ ] invalidation/refetch
[ ] duplicate realtime tolerance
[ ] failure test
```

---

# 168. Stop conditions

Stop implementation if:

- a tenant/workspace resource uses a tenant-blind cache key;
- Account switching exists but account cache has neither Account identity nor proven hard reset;
- a route/component mutates another product's cache by hand;
- query data is copied permanently into a second global store without a bounded reason;
- an optimistic mutation has no rollback/reconcile path;
- a non-idempotent mutation is blindly retried;
- `workspaceId!` is used where runtime scope can actually be missing;
- old Workspace/principal responses can populate new scope;
- query key omits filter/page/sort fields that change result identity;
- server-returned canonical fields are ignored in favor of optimistic guesses;
- realtime and REST maintain separate resource truth;
- cache timing is relied on for tenant/security isolation;
- persisted cache is introduced without principal/scope/logout policy.

---

# 169. FE-STATE-085 — Pilot verification preserves state authority

An isolated UI seam receives semantic models and typed callbacks; it does not create a second query cache, transport client, or mutation owner. The runtime view owns Work Management hooks and adapts mutations to presentation callbacks. Focused pilot proof must cover scoped query keys, loading/error mapping, optimistic success and rollback, conflict behavior where the contract exposes it, in-flight scope transitions, realtime duplicate/out-of-order recovery, and mapper/request shapes. If the public contract lacks version or conflict semantics, record that case as authority-backed not applicable rather than inventing fields.

# 170. Executable evidence

Primary current evidence:

```text
frontend/packages/foundation/query/
frontend/packages/product/work-management/state/
frontend/packages/product/automation/state/
frontend/packages/features/
frontend/apps/web/src/providers/
frontend/apps/mobile/src/providers/
frontend/tooling/dependency-rules/src/architecture-manifest.ts
frontend state/query tests
```

Current Work Management state includes explicit:

```text
api
cache
hooks
mutations
queries
services
```

boundaries, demonstrating the product-state ownership model.

---

# 170. Related frontend architecture

Read:

```text
api-and-contracts.md
realtime.md
hosts-composition-routing.md
dependency-boundaries.md
testing-and-quality-gates.md
architecture-change-policy.md
```

---

# 171. Related product/backend authority

Read relevant:

```text
docs/product/*
backend/docs/architecture/api-and-contracts.md
docs/architecture/data-ownership-and-consistency.md
docs/architecture/events-realtime-and-delivery-boundary.md
```

for producer/product semantics.

---

# 172. Explicit non-responsibilities

This document does not define:

```text
backend transaction invariants
backend authorization
exact REST DTOs
realtime transport protocol
visual loading/error components
```

It defines how frontend owns derived server state and mutation lifecycle.

---

# 173. Final state model

The intended frontend state architecture is:

```text
BACKEND AUTHORITY
        ↓
typed API/realtime contracts
        ↓
product/feature query owner
        ↓
scoped query key
        ↓
derived cache
        ↓
view projection / UI
```

Mutations flow:

```text
logical command
        ↓
optional optimistic admission
        ↓
server mutation
        ↓
success / conflict / failure
        ↓
rollback/reconcile
        ↓
invalidate/refetch/realtime convergence
        ↓
authoritative cache
```

Scope transitions flow:

```text
principal/account/workspace changes
        ↓
new scope identity
        ↓
old cache/subscription cannot bleed
        ↓
new authoritative fetch/rebind
```

The state architecture succeeds when the client remains fast and interactive without becoming a second database, leaking data across scopes, or allowing optimistic/realtime convenience to outrank server truth.

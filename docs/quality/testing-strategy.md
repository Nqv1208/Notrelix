---
document_id: QLT-TESTING
document_type: testing-strategy
status: active
owner: engineering-quality
applies_to:
  - repository
  - backend
  - frontend
  - ci
evidence:
  - docs/quality/engineering-quality-standard.md
  - .github/workflows/be-ci.yml
  - .github/workflows/fe-ci.yml
  - backend/backend.slnx
  - backend/tests/
  - frontend/package.json
  - frontend/tooling/testing/
  - frontend/playwright.config.ts
  - frontend/playwright.storybook.config.ts
review_on:
  - test-taxonomy-change
  - required-suite-change
  - ci-topology-change
  - architecture-gate-change
  - integration-test-infrastructure-change
  - frontend-test-runtime-change
  - e2e-strategy-change
---

# Testing Strategy

> **Tests prove contracts at the closest trustworthy boundary, then selected integration and end-to-end tests prove composition.**
>
> The goal is not the largest test suite. The goal is the smallest complete evidence system that reliably fails when a protected property is broken.

This document owns repository-wide testing strategy.

Backend/frontend documentation owns framework-specific test conventions and exact local commands where those differ.

---

# 1. Test philosophy

Testing follows:

```text
risk
ownership
failure mode
consumer boundary
```

not an arbitrary global ratio of unit/integration/E2E tests.

---

# 2. QLT-TST-001 — Test pyramid is contractual, not numeric

Typical evidence:

```text
pure business/value behavior
→ many fast behavior tests

application orchestration
→ focused orchestration/component tests

persistence/provider/tenant
→ realistic integration tests

API/events/realtime
→ contract/integration tests

critical user journeys
→ selected E2E
```

No fixed percentage is canonical.

---

# 3. Test the owner

Place the primary test near the authoritative owner.

Examples:

```text
Domain state transition
→ Domain test

Application authorization orchestration
→ Application test

RLS
→ Infrastructure/Integration test

message ordering
→ Platform + integration

OpenAPI endpoint contract
→ API test + contract drift

query-cache rollback
→ frontend state/integration test
```

---

# 4. QLT-TST-002 — Primary proof lives near the violated contract

A failing business invariant should not require a browser E2E to detect it if a deterministic Domain test can prove it directly.

---

# 5. Layered proof

Critical properties may require more than one layer because different layers prove different things.

Example authorization:

```text
Governance/domain policy
→ business rule

Application pipeline
→ enforcement ownership

API/query integration
→ protected endpoint/data path

E2E
→ selected user journey
```

This is intentional layered evidence, not duplication.

---

# 6. QLT-TST-003 — Layered tests prove different properties

Do not copy identical assertions at every layer.

Each layer should add proof not already established below.

---

# 7. Success and rejection

Critical mutations need:

```text
success
validation rejection
authorization rejection
not-found
conflict/concurrency
relevant transient failure
```

according to the contract.

---

# 8. QLT-TST-004 — Rejection paths are first-class

A mutation suite is incomplete if it only proves happy path for an invariant that can reject.

---

# 9. Rejection atomicity

For rejected Domain mutation, verify where applicable:

```text
state unchanged
version unchanged
events unchanged
```

For Application/integration rejection, verify no forbidden side effect escaped.

---

# 10. QLT-TST-005 — Failure test proves absence of unintended effects

Do not assert only “exception thrown”.

Also verify that:

- state;
- outbox;
- provider effect;
- usage;
- notification;
- cache

did not mutate when the contract requires atomic rejection.

---

# 11. Semantic no-op

Test explicit no-op behavior for high-churn entities where it matters.

Examples:

- same normalized field value;
- same name;
- same order;
- remove missing idempotent relation.

---

# 12. QLT-TST-006 — No-op tests protect event/history quality

Verify that semantic no-op does not create fake version/event/activity where the contract says no mutation.

---

# 13. Determinism

Tests control:

- time;
- random;
- unique IDs where needed;
- external facts;
- provider responses.

Avoid arbitrary sleeps.

---

# 14. QLT-TST-007 — Time is controlled when time affects semantics

Use explicit clocks/time sources in tests for:

- token expiry;
- schedules;
- billing periods;
- share-link expiry;
- retries/backoff;
- analytics time windows.

---

# 15. Randomness

Use deterministic seeded/value injection for algorithms where output matters.

Do not depend on chance to trigger edge cases.

---

# 16. Test isolation

Tests must not depend on:

- execution order;
- previous test data;
- shared mutable singleton;
- global current tenant;
- external internet.

---

# 17. QLT-TST-008 — Parallel execution is safe by design

If tests run in parallel, data namespaces/resources are isolated.

Disabling parallelism globally to hide shared-state defects is not the preferred fix.

---

# 18. Backend test projects

Current backend solution includes:

```text
Notrelix.Architecture.Tests
Notrelix.Domain.Tests
Notrelix.Application.Tests
Notrelix.Infrastructure.Tests
Notrelix.Platform.Tests
Notrelix.API.Tests
Notrelix.Integration.Tests
```

plus testing-support projects.

These map well to protected responsibilities.

---

# 19. Domain tests

Domain tests prove:

```text
value-object rules
aggregate invariants
lifecycle
state transitions
semantic no-op
version/events
ordering algorithms
local reference rules
```

They should be fast and provider-free.

---

# 20. QLT-TST-009 — Domain tests do not mock infrastructure

If a Domain test needs DB/network/provider mocks, reconsider whether the behavior belongs to Domain.

External facts may be supplied as plain values/facts.

---

# 21. Domain event tests

Test:

- event emitted after meaningful transition;
- normalized committed payload;
- no event on rejection/no-op;
- stable logical meaning.

Do not snapshot entire aggregate blindly.

---

# 22. Value-object/property tests

Good candidates include:

- FractionalIndex;
- IDs;
- money;
- ranges;
- normalized identifiers;
- parsing/validation.

Boundary and property tests are often more valuable than a few examples.

---

# 23. Application tests

Application tests prove:

```text
pipeline ownership
authorization call/order
validation orchestration
transaction orchestration
port usage
idempotency classification
external-fact loading
handler result mapping
```

---

# 24. QLT-TST-010 — Application test distinguishes orchestration from provider implementation

Mock/replace ports where the property is orchestration.

Do not use an unrealistic mock to prove provider/SQL semantics.

---

# 25. Pipeline tests

Critical pipeline tests cover:

- validation;
- authorization;
- idempotency;
- transaction boundaries;
- behavior ordering;
- short-circuit behavior.

---

# 26. Architecture tests

Architecture tests prove:

```text
project dependency
Domain purity
bounded-context isolation
Application port ownership
forbidden dependency
```

They are executable architecture.

---

# 27. QLT-TST-011 — Architecture tests fail on zero discovery

If a namespace/project/folder pattern changes such that a critical architecture test examines nothing, the gate must fail or explicitly prove its scope.

---

# 28. Infrastructure tests

Infrastructure tests prove implementation-specific behavior such as:

- EF mappings;
- interceptors;
- RLS configuration;
- serialization;
- repositories;
- provider adapters where realistic enough.

---

# 29. Integration tests

Integration tests prove composition with realistic dependencies.

Current backend integration evidence includes critical RLS, tenant isolation, outbox, idempotency, deduplication, migration, Redis, realtime, and production-composition tests.

---

# 30. QLT-TST-012 — Provider/database semantics use realistic dependencies

PostgreSQL/RLS tests should use PostgreSQL-equivalent infrastructure rather than SQLite when provider behavior matters.

---

# 31. Database tests

Test relevant:

```text
mapping
constraint
transaction
concurrency
RLS
migration
query shape
index/provider behavior
```

Avoid integration tests that merely repeat Domain logic.

---

# 32. RLS tests

Required negative scenarios include:

```text
tenant A cannot read tenant B
tenant A cannot mutate tenant B
unset/wrong tenant context fails safely
privileged/admin path only when explicitly authorized
```

---

# 33. QLT-TST-013 — Tenant-isolation test uses two real tenant datasets

A single-tenant happy-path test cannot prove isolation.

---

# 34. Migration tests

Migration evidence should include:

```text
existing schema/data
upgrade
RLS/index/constraint preservation
application startup/migration smoke
forward-recovery/rollback where required
```

---

# 35. QLT-TST-014 — Migration smoke includes production-like upgrade path

Empty database creation is useful but insufficient for risky existing-data migration.

---

# 36. Outbox tests

Outbox tests prove:

```text
source state + outbox atomicity
claim/reclaim
dispatch retry
no event before commit
duplicate-safe consumption
```

---

# 37. QLT-TST-015 — Outbox atomicity is an integration property

A mock repository test cannot prove DB transaction atomicity between source mutation and outbox row.

---

# 38. Messaging tests

Platform tests prove:

```text
consumer identity
message identity
ordering
poison detection
retry
dedup
delivery contract
```

---

# 39. QLT-TST-016 — Message reliability tests include failure before success

For ordering/sequence:

```text
receive N
handler fails
sequence not advanced
retry succeeds
sequence advances
```

Happy-path order alone is insufficient.

---

# 40. Poison handling

Test poison identity with:

```text
message
consumer
attempt/failure context
```

not only event name.

---

# 41. API tests

API tests prove:

```text
routing/versioning
authn/authz behavior
request validation
status/error contract
idempotency endpoint behavior
OpenAPI consistency
```

---

# 42. QLT-TST-017 — API contract test uses public semantics

Avoid asserting controller/endpoint private implementation.

Test HTTP contract and produced behavior.

---

# 43. OpenAPI drift

Current backend CI exports the API spec and compares it with committed `backend/contracts/openapi/notrelix.v1.json`.

Drift is a failing compatibility signal requiring explicit regeneration/review.

---

# 44. Idempotent API tests

Test:

```text
same idempotency key + same operation
→ one effect / replayed result

same key + conflicting request
→ explicit conflict according to contract
```

where relevant.

---

# 45. Production composition tests

Composition tests prove the production DI/application graph can be created with expected implementations.

They should catch:

- missing registration;
- wrong lifetime;
- test-only substitution accidentally required.

---

# 46. QLT-TST-018 — Production graph is tested without test-only architecture shortcuts

A graph test that replaces most production services cannot prove the production graph.

---

# 47. Frontend taxonomy

Current frontend defines separate Vitest categories:

```text
node
web
integration
mobile
generators
```

and guarded variants that assert non-zero test execution.

It also has Storybook Playwright UI tests and production Playwright E2E.

---

# 48. Frontend Node tests

Use Node category for:

- pure/foundation logic;
- package utilities;
- reducers/state;
- non-DOM contracts;
- architecture-adjacent tooling where appropriate.

---

# 49. Frontend web tests

Use web/jsdom/browser-like component tests for:

- components;
- hooks;
- web runtime behavior;
- accessibility semantics that do not require full browser.

---

# 50. Frontend integration tests

Use integration category for interactions across:

- query/state;
- API abstraction;
- routing/composition;
- runtime boundaries;
- feature integration.

---

# 51. Mobile tests

Mobile tests prove:

- native-safe imports;
- mobile runtime behavior;
- product state behavior;
- native-specific components/navigation where applicable.

---

# 52. QLT-TST-019 — Mobile suite proves category coverage, not only test count

Current guarded mobile command also asserts mobile test-category coverage.

A mobile gate should not pass because only unrelated pure tests ran.

---

# 53. Generator tests

Generators/codegen need:

- golden path;
- deterministic output;
- drift;
- invalid input;
- compatibility.

---

# 54. QLT-TST-020 — Generator test proves deterministic producer behavior

Generated output should be identical for identical source/config.

---

# 55. Dependency architecture tests

Frontend `dependency-rules` owns exact package graph rules.

Tests/checks should catch:

- forbidden layer edge;
- undeclared package;
- mobile-unsafe import;
- public-export boundary drift.

---

# 56. Test taxonomy

A test belongs to the category that matches runtime/property.

Do not put a browser-dependent test in Node category merely to make it faster.

---

# 57. QLT-TST-021 — Test classification is architectural metadata

Taxonomy influences which runtime/dependencies are proven.

Incorrect classification can create false CI evidence.

---

# 58. Query/server-state tests

Frontend state tests should cover:

```text
query-key tenant/workspace scope
mutation success
mutation rejection/rollback
invalidations
stale response after scope switch
optimistic reconciliation
```

---

# 59. QLT-TST-022 — Rejected mutation does not leave optimistic cache corrupted

Test both UI-visible state and authoritative query/cache recovery.

---

# 60. Workspace-switch tests

Test:

```text
old query pending
switch Workspace
old response arrives
new scope remains unpolluted
```

and relevant realtime subscription disposal/recreation.

---

# 61. Realtime client tests

Test:

```text
duplicate
out-of-order
gap
reconnect
missed event
scope transition
optimistic mutation race
```

---

# 62. QLT-TST-023 — Realtime test includes refetch/reconciliation path

When local patch cannot prove safe ordering/version, test invalidate/refetch to authoritative state.

---

# 63. UI foundation tests

Current frontend CI runs Storybook Playwright a11y/visual gates.

This is useful for:

- design-system primitives;
- accessibility;
- visual states;
- interaction foundations.

---

# 64. QLT-TST-024 — UI visual test is not behavior/security proof

Pair UI snapshots with behavior/accessibility tests where those properties matter.

---

# 65. Accessibility tests

Automated evidence includes:

- axe;
- accessible name/role;
- focus;
- keyboard interaction.

Manual review remains necessary for complex flows.

---

# 66. E2E

Production E2E proves critical composed user journeys using an actual built web artifact.

Current frontend CI builds web once and runs E2E against that exact build artifact.

---

# 67. QLT-TST-025 — E2E uses production-like build/configuration

A dev-server-only journey cannot prove production build/startup composition by itself.

---

# 68. E2E selection

Good E2E candidates:

```text
auth/bootstrap
Workspace switch
critical protected navigation
core Work Management journey
document editing journey
critical billing/admin flow
```

Do not encode every validation branch in E2E.

---

# 69. Contract generation tests

Generated frontend contracts consume backend producer schemas.

Test:

```text
generator succeeds
committed output unchanged
consumer compiles
critical consumer behavior
```

---

# 70. QLT-TST-026 — Producer and generated consumer drift is a required gate

Handwritten duplicate DTOs should not hide generated contract drift.

---

# 71. Event contract tests

For integration/public events, test:

```text
logical event name
version
required scope
serialization
nullability
stable identity
consumer compatibility
```

---

# 72. QLT-TST-027 — Event tests distinguish Domain event from public integration event

Do not accidentally turn internal aggregate event shape into public compatibility contract.

---

# 73. Realtime contract tests

Test server/client payload semantics around:

- resource identity;
- workspace scope;
- revision/version;
- gap/reconnect;
- authorization revocation.

---

# 74. Provider/webhook tests

Use captured/synthetic fixtures that reflect real provider protocol.

Test:

```text
signature
timestamp
replay
malformed payload
unsupported version
duplicate delivery
tenant routing
unknown outcome
```

---

# 75. QLT-TST-028 — Verified webhook payload is still validated business input

A correct signature does not prove semantic validity.

Tests should cover malicious/impossible values after authentication.

---

# 76. File upload tests

Test:

- type/size;
- authorization;
- object metadata;
- signed URL expiry;
- malicious filename/content;
- cleanup/idempotency.

---

# 77. Billing tests

Critical Billing evidence includes:

```text
subscription lifecycle
entitlement resolution
usage dedup
hard quota concurrency
provider callback replay/out-of-order
non-destructive downgrade
payment-secret non-exposure
```

---

# 78. Automation tests

Critical Automation evidence includes:

```text
rule validation/version
duplicate trigger
schedule identity
condition determinism
action idempotency
partial failure
permission revocation
recursion loop/depth
provider unknown outcome
```

---

# 79. Documents tests

Critical Documents evidence includes:

```text
typed block validation
hierarchy cycles
cross-page/workspace rejection
fractional ordering
version conflict
resource-link security
history/snapshot restore
```

---

# 80. Work Management tests

Critical Work Management evidence includes:

```text
field type semantics
item value validation
semantic no-op
optimistic concurrency
ordering
view schema
relation cycles/scope
formula/rollup
forms
approval terminal state
```

---

# 81. Governance tests

Critical Governance evidence includes:

```text
allow/deny
guest
share-link expiry/revoke
custom role precedence
field-level restrictions
list/search/export filtering
cache invalidation
realtime revocation
```

---

# 82. Identity tests

Critical Identity evidence includes:

```text
session refresh/revoke
refresh replay
one-time token replay
MFA lifecycle
OAuth subject collision
API token scope/revoke
secret non-exposure
```

---

# 83. Workspaces tests

Critical Workspaces evidence includes:

```text
membership lifecycle
last-owner
invitation accept/revoke race
cross-workspace team/space
workspace switch scope
tenant isolation
```

---

# 84. Property tests

Use property-based tests for algorithms with broad input spaces.

Examples:

- FractionalIndex;
- normalization;
- parsers;
- range/value objects.

---

# 85. Fuzz/security tests

For untrusted input parsers:

- webhook;
- file metadata;
- rich text/import;
- formula parser;
- public share token;

fuzzing or malicious fixtures may be justified.

---

# 86. QLT-TST-029 — Stronger test technique follows uncertainty

Do not mandate fuzz/property/mutation testing everywhere.

Use it when example tests are unlikely to cover the risk space.

---

# 87. Snapshots/golden tests

Good uses:

- generated contracts;
- serializers;
- UI visual;
- stable formatted output.

Bad sole use:

- business invariant;
- authorization;
- transaction.

---

# 88. QLT-TST-030 — Snapshot review must be intentional

Regenerating snapshots blindly to make CI green is equivalent to weakening a test.

Review semantic difference.

---

# 89. Mocks

Mocks are appropriate for:

- deterministic orchestration;
- failure injection;
- call-boundary verification.

They are dangerous when used to pretend to prove real protocol semantics.

---

# 90. QLT-TST-031 — Mock only outside the property under test

If testing SQL behavior, do not mock SQL.

If testing provider signature, do not mock signature verifier.

If testing Application call order, mocking the provider port can be correct.

---

# 91. Fakes

In-memory fakes can be useful when their semantic contract is deliberately equivalent for the tested property.

Document limitations.

---

# 92. Testcontainers

Containers are appropriate for PostgreSQL/Redis/RabbitMQ-like protocol evidence where local/CI cost is acceptable.

Do not turn every pure test into a container test.

---

# 93. Network

Required tests should not depend on public internet/provider uptime.

Use deterministic provider fixtures/emulators/contracts.

---

# 94. QLT-TST-032 — External provider availability is not CI dependency

CI should prove Notrelix adapter behavior without relying on a live SaaS account unless an explicitly separate non-required certification environment exists.

---

# 95. CI non-zero execution

Current backend uses TRX verification for named critical suites/classes.

Current frontend guarded scripts parse Vitest result JSON and assert count/category coverage.

This pattern is canonical in principle:

```text
required critical proof
→ assert it actually ran
```

---

# 96. QLT-TST-033 — Required tests cannot pass through empty filters

Any critical job using:

- grep;
- namespace filter;
- test project glob;
- package selection

must fail if the intended scope becomes empty.

---

# 97. Named critical tests

Pinning named critical test classes in CI can protect foundation properties.

It should not freeze names forever without migration.

When refactoring names, update verifier atomically.

---

# 98. QLT-TST-034 — Non-zero verifier protects property, not historical test name

A verifier should evolve when tests move while continuing to prove the same protected property.

---

# 99. Flaky tests

Classify root cause:

```text
race
clock
network
shared state
resource exhaustion
eventual consistency timing
test bug
product bug
```

Fix it.

---

# 100. QLT-TST-035 — Retry is not permanent flake handling

A CI retry may help diagnose rare infrastructure issues, but cannot become the proof strategy for a nondeterministic test.

---

# 101. Wait/poll strategy

For eventual behavior:

- poll explicit condition with bounded timeout;
- use signal/latch when possible.

Avoid fixed sleeps.

---

# 102. Failure diagnostics

On failure, preserve useful:

- test report;
- logs;
- Playwright trace/report;
- relevant container logs;
- correlation IDs

without leaking secrets.

---

# 103. QLT-TST-036 — Failure artifacts are bounded and safe

Diagnostics should aid repair while respecting secret/PII policy and retention.

---

# 104. Test naming

Name tests around behavior:

```text
when_<condition>_should_<result>
```

or another readable convention.

Avoid names tied only to private method structure.

---

# 105. Arrange/Act/Assert

Structure should make:

```text
precondition
operation
assertion
```

clear.

Do not create ceremony when a concise test is clearer.

---

# 106. Test helper policy

Helpers are good for repetitive mechanics.

Critical facts such as:

- tenant;
- expected version;
- authorization;
- logical event ID

should remain visible in tests when they matter.

---

# 107. QLT-TST-037 — Test helper cannot silently make invalid scenario valid

A helper must not auto-grant permission/create missing membership unless the scenario explicitly intends it.

---

# 108. Fixture lifecycle

Database/container fixtures should isolate state.

Per-test schema/transaction/database strategies may differ, but cross-test contamination is forbidden.

---

# 109. Cleanup

Tests should be safe under abrupt failure.

Prefer disposable isolated resources over fragile manual cleanup.

---

# 110. Test data builders

Builders may provide valid defaults.

But invariant-sensitive fields should be explicit in the scenario.

---

# 111. QLT-TST-038 — Valid-default builder does not hide the invariant under test

A last-owner test should clearly show all owners/members involved rather than rely on a hidden builder default.

---

# 112. Coverage reporting

Coverage is diagnostic.

Useful questions:

- critical branch untested?
- new module has zero tests?
- rejection path missing?

Do not optimize to a repository-wide percentage alone.

---

# 113. QLT-TST-039 — Coverage does not waive scenario review

100% line coverage can still miss concurrency, idempotency, ordering, or semantic assertions.

---

# 114. Test deletion

Deleting a test requires understanding which property it protected.

If property remains, replace/move proof.

---

# 115. QLT-TST-040 — Refactor does not delete evidence accidentally

Moving code/project/package must update:

- tests;
- CI filters;
- non-zero verifier;
- architecture gates;
- test taxonomy.

---

# 116. Change-driven local workflow

Recommended:

```text
1. closest failing/changed tests
2. affected project/package suite
3. architecture/contract checks
4. relevant integration
5. required full validation for change
```

This optimizes feedback without reducing final proof.

---

# 117. Backend CI mapping

Current backend:

```text
quality
→ restore/format/build/vulnerability/SQLite guards

architecture-tests
→ architecture project + critical gate verification

core-tests
→ Domain/Application/Infrastructure

platform-tests
→ messaging/reliability

api-tests
→ API + idempotency + OpenAPI drift

integration-tests
→ RLS/outbox/idempotency/realtime/migration/production graph

docker-build
→ after required backend evidence

backend-ci
→ final exact result gate
```

---

# 118. Frontend CI mapping

Current frontend:

```text
quality
→ codegen/architecture/docs/test taxonomy/type/lint/format

test-core
→ guarded Node/Web/Integration

test-mobile
→ guarded mobile/category coverage

test-tooling
→ guarded generators

ui-foundation
→ Storybook Playwright a11y/visual

build-web/marketing/mobile

e2e-production
→ exact built web artifact

frontend-gate
→ every required job success
```

---

# 119. QLT-TST-041 — CI topology may evolve while proof obligations remain

Do not encode current job names as permanent architecture.

Preserve the protected evidence when reorganizing CI.

---

# 120. Docs/testing relationship

When a canonical rule changes, testing strategy asks:

```text
Which existing proof becomes wrong?
Which new rejection/compatibility case is required?
Does an architecture gate need change?
Does CI verifier still select real tests?
```

---

# 121. Quality-gate self-tests

Scripts that enforce test counts/taxonomy/drift should themselves have fixture tests where complexity justifies it.

---

# 122. QLT-TST-042 — A critical meta-gate should be testable

Examples:

- empty result fails;
- missing test category fails;
- malformed report fails closed;
- expected test present succeeds.

---

# 123. CI fail-closed

If a required verifier/report parser cannot run, the job fails.

Do not treat missing report as “nothing to check”.

---

# 124. Test timeout

Timeout protects CI from hangs.

A timeout failure requires diagnosis, not simply increasing timeout indefinitely.

---

# 125. QLT-TST-043 — Timeout reflects expected behavior

Set timeouts based on realistic suite/operation behavior with margin.

Large unexplained timeout growth is a signal.

---

# 126. Test performance

Keep pure tests fast.

Keep integration suites targeted.

Parallelize independent groups where safe.

Cache dependencies/build artifacts, not semantic test results.

---

# 127. QLT-TST-044 — Do not cache test pass/fail across code changes as proof

Reuse compilation/dependency artifacts safely, but each required revision must execute the required proof.

---

# 128. Exact revision

The final merge/release evidence is the exact SHA that ran required checks.

---

# 129. QLT-TST-045 — Same branch, different SHA is different evidence

Any commit after green CI invalidates the previous revision's certification for release/freeze.

---

# 130. Critical test inventory

Exact names are source/CI-owned and can change.

The strategy owns categories/properties, not a giant handwritten inventory.

Generated/CI output may list exact required tests.

---

# 131. Test ownership matrix

| Property | Primary test owner |
|---|---|
| Domain invariant | Domain |
| Application orchestration | Application |
| Project/package architecture | Architecture/dependency tooling |
| EF/provider persistence | Infrastructure |
| RLS/tenant runtime | Integration |
| Messaging order/poison | Platform |
| Outbox atomicity | Integration |
| HTTP/API contract | API |
| OpenAPI drift | API contract gate |
| Frontend state/query | Web/Node/Integration |
| Mobile runtime safety | Mobile |
| UI accessibility/visual | UI foundation |
| Production composition | Integration/E2E |
| Generated client | generator/drift + consumer |
| Critical user journey | E2E |

---

# 132. Definition of test-complete

A change is test-complete when:

```text
all changed contracts have primary proof
critical rejection paths are covered
integration-sensitive properties use realistic evidence
architecture/contract drift is checked
client reconciliation is covered where relevant
required suite executes non-zero work
exact CI revision passes
```

---

# 133. Stop conditions

Stop rather than claim test-complete if:

- only happy path exists for a rejecting critical mutation;
- Domain rejection test checks exception but not state/version/events;
- tenant isolation was changed with only one tenant in tests;
- SQLite/mock is used to claim RLS/PostgreSQL semantics;
- messaging retry/order path lacks failure scenario;
- frontend realtime test lacks gap/reconnect recovery;
- provider webhook signature test bypasses raw request/replay;
- critical required CI filter can select zero tests;
- snapshot was blindly updated without semantic review;
- E2E is the only proof of a lower-level invariant;
- test passes only when run after another test.

---

# 134. Final testing rule

For every changed behavior, answer:

```text
What exact contract is being proven?
What is the closest trustworthy test boundary?
What rejection/concurrency/retry case can break it?
What realistic integration proof is required?
What composition/E2E proof adds unique value?
How do we know the required suite actually ran?
Can the same test run repeatedly/parallel without hidden state?
Does the exact changed revision have green evidence?
```

The target is:

> **a layered, deterministic, risk-driven test system that fails close to the violated owner and still proves enough real composition to trust production behavior.**

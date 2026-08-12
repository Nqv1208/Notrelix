---
document_id: BE-TESTS-AGENTS
document_type: agent-instructions
status: active
owner: engineering-quality
applies_to:
  - backend-tests
  - backend-testing-support
  - backend-coding-agents
evidence:
  - backend/backend.slnx
  - backend/tests/
  - backend/docs/architecture/testing-and-quality-gates.md
  - docs/quality/testing-strategy.md
  - docs/quality/engineering-quality-standard.md
review_on:
  - backend-test-topology-change
  - testing-support-change
  - test-quality-policy-change
  - required-gate-change
  - persistence-test-strategy-change
---

# Backend Tests — Agent Instructions

> **Tests are executable proof for owned contracts. They do not invent architecture, hide missing semantics behind fixtures, or maximize test count for its own sake.**
>
> Put a test at the cheapest reliable seam that proves the property, then add broader evidence only when it proves a different boundary or production interaction.

This file applies to:

```text
backend/tests/**
```

It extends:

```text
../AGENTS.md
../../AGENTS.md
```

Canonical test architecture:

```text
../docs/architecture/testing-and-quality-gates.md
```

Repository test strategy:

```text
../../docs/quality/testing-strategy.md
```

---

# 1. Current test project map

Current backend solution contains:

```text
Notrelix.Domain.Tests
Notrelix.Application.Tests
Notrelix.Infrastructure.Tests
Notrelix.Platform.Tests
Notrelix.API.Tests
Notrelix.Integration.Tests
Notrelix.Architecture.Tests
```

and support libraries:

```text
Notrelix.Testing.Core
Notrelix.Testing.Domain
Notrelix.Testing.Application
Notrelix.Testing.Integration
```

`backend.slnx` is the inventory authority.

---

# 2. Primary placement rule

Choose the project from the **property being proven**, not the source file merely being touched.

| Property | Primary project |
|---|---|
| aggregate/value-object invariant | Domain.Tests |
| Domain state transition/no-op/event/version | Domain.Tests |
| command/query/handler orchestration | Application.Tests |
| validator/pipeline/authorization/result | Application.Tests |
| EF mapping/query/adapter/provider mechanics | Infrastructure.Tests |
| PostgreSQL/RLS/migration behavior | Infrastructure.Tests or Integration.Tests |
| outbox/post-commit/idempotency/order/retry/poison | Platform.Tests |
| HTTP binding/auth host/error/OpenAPI | API.Tests |
| real cross-layer production graph | Integration.Tests |
| dependency/placement/forbidden reference | Architecture.Tests |

A changed source file may require tests in more than one project.

---

# 3. Test pyramid is not the goal

The goal is:

```text
fastest trustworthy proof
+
necessary cross-boundary proof
```

Do not force a fixed percentage of unit/integration tests.

Do not add E2E/integration tests when a pure Domain test fully proves the invariant.

Do not use a unit test when the property only exists under PostgreSQL or production host composition.

---

# 4. Domain tests

Use `Notrelix.Domain.Tests` for pure owned business behavior.

Prove:

```text
valid transition
invalid transition
failure atomicity
semantic no-op
event emission/non-emission
version/concurrency semantics where Domain-owned
value-object equality/validation
ordering/business calculation
```

Avoid provider/persistence bootstrapping.

---

# 5. Domain test shape

Prefer scenario language:

```text
Given aggregate state
When business operation
Then resulting state
And emitted facts
And rejected invalid transitions
```

over assertions coupled to private helper call sequences.

Test public/intentional Domain behavior whenever possible.

---

# 6. Failure atomicity

For a rejected Domain operation, verify important state did not partially mutate.

Example dimensions:

```text
status
collection membership
ordering key
version
events
timestamps
```

as relevant.

Do not only assert that an exception/result occurred.

---

# 7. Semantic no-op

If repeating an operation with already-current state is a no-op, test:

```text
no version increment if designed
no event if designed
no history mutation if designed
same observable state
```

No-op semantics are product behavior, not optimization detail.

---

# 8. Domain event tests

Test:

```text
event exists when owned fact commits
event payload contains owned fact
event is absent on rejection/no-op when intended
```

Do not test that an internal Domain event happens to serialize into a public integration contract unless that mapping is intentionally the property under test elsewhere.

---

# 9. Application tests

Use `Notrelix.Application.Tests` for use-case semantics.

Typical responsibilities:

```text
handler orchestration
validator behavior
pipeline marker/behavior
authorization
resource/tenant resolution
transaction/result semantics
expected version
idempotency orchestration
cache interaction contract
post-commit enrollment intent
cross-context ports
```

---

# 10. Application test boundary

Application tests may mock/replace Infrastructure ports when the property is orchestration.

Do not mock the business result you are trying to prove.

Example:

Good:

```text
mock repository returns aggregate
execute handler
assert Domain mutation/result
assert expected port interaction
```

Bad:

```text
mock Domain service to return Success
assert handler returned Success
```

when the handler's responsibility includes coordinating the real Domain operation.

---

# 11. Authorization tests

For protected Application operations, include as relevant:

```text
authorized principal
unauthorized principal
wrong resource scope
wrong tenant/workspace
revoked/changed authority
```

Do not only test role-name happy path.

Test the resource/action semantics owned by Application.

---

# 12. Application transaction tests

A unit-level Application test can verify orchestration contract where transaction abstraction is mocked/faked.

If the property is actual database rollback/commit/RLS/outbox behavior, use Infrastructure/Integration evidence.

Do not claim real transaction semantics from a mock that cannot roll back.

---

# 13. Current EF InMemory availability

Current test projects include EF Core InMemory in several places.

It is permitted only for properties that do not depend on PostgreSQL-specific behavior.

Suitable examples can include:

```text
simple test-only Application query setup
host composition not asserting RLS/provider SQL
fixture construction
```

when the test's protected property is elsewhere.

---

# 14. EF InMemory forbidden proof claims

Do **not** use InMemory to prove:

```text
PostgreSQL RLS
Npgsql conversion
real relational FK/constraint behavior
PostgreSQL locking
real transaction isolation
migration DDL
index/query-plan behavior
PostgreSQL-specific SQL
```

Use PostgreSQL/Testcontainers.

---

# 15. Infrastructure tests

Use `Notrelix.Infrastructure.Tests` for implementation mechanics.

Typical proof:

```text
EF mappings
value converters
query/repository behavior
constraints/index expectations
RLS
migrations
Redis/cache adapter
provider adapter protocol mapping
storage/search adapter
auth provider mechanics
```

Choose real dependency integration when the provider behavior is the property.

---

# 16. PostgreSQL Infrastructure tests

Current Infrastructure test project references Testcontainers PostgreSQL.

Use PostgreSQL-realistic tests for:

```text
RLS
schema/migration
Npgsql mapping
transaction/locking
constraint/index semantics
tenant session context
```

Keep test data bounded and deterministic.

---

# 17. Migration tests

A migration change should prove more than:

```text
new empty database reaches latest schema
```

When existing production data matters, prove:

```text
representative old schema/data
→ migration apply
→ transformed data semantics
→ RLS/constraints/indexes
→ application read/write behavior
```

For complex backfills, test idempotency/resume and invalid legacy data handling at the right seam.

---

# 18. RLS tests

RLS evidence must include both sides:

```text
authorized tenant can access intended data
foreign tenant cannot access the data
```

Also test background/runtime session context if the changed mechanism uses it.

Do not infer RLS correctness from policy SQL text alone.

---

# 19. Provider adapter tests

For an adapter, distinguish:

```text
mapping/orchestration test
real protocol integration test
```

A mocked HTTP/provider client can prove mapping/retry classification.

It cannot prove:

```text
real OAuth
signature
rate limit
network/TLS
provider idempotency
```

Do not overclaim fidelity.

---

# 20. Cache tests

Test the protected cache property:

```text
key scope
tenant/user/resource dimension
invalidation
fallback
serialization/version
```

Do not build tests that make cache the source of truth.

For authorization-sensitive cache, include stale/revoked scenarios where relevant.

---

# 21. Platform tests

Use `Notrelix.Platform.Tests` for reusable runtime/delivery mechanisms.

Typical proof:

```text
message identity
consumer identity
outbox/post-commit
idempotency
retry/backoff
poison/dead-letter
ordering
scheduler/claim mechanics
background scope mechanics
```

Product-specific business meaning should remain in Domain/Application tests.

---

# 22. Platform success ordering

For delivery/ordering mechanisms, explicitly prove:

```text
handler/effect success
happens before
ack / cursor / sequence advancement
```

and failure does **not** advance completion state improperly.

This is a high-value regression seam.

---

# 23. Idempotency tests

Where applicable include:

```text
first execution
same identity duplicate
same identity conflicting payload
retry after transient failure
concurrent duplicate
retention/expiry
```

For provider-visible side effects, verify the provider call is not duplicated or the result is reconciled.

---

# 24. Poison tests

Prove poison is scoped to the intended identity:

```text
message
+
consumer
```

unless a broader ordering invariant requires different handling.

Include:

```text
deterministic invalid
transient failure
retry exhaustion
dead-letter identity
recovery/replay
```

as relevant.

---

# 25. Ordering tests

Test the exact business/mechanism ordering scope.

Examples:

```text
same aggregate/resource preserves order
different resources can progress independently
failed item blocks only required scope
cursor advances only after success
```

Avoid global-order tests when the architecture does not require global ordering.

---

# 26. Integration tests

Use `Notrelix.Integration.Tests` for properties that require the composed production graph.

Current project references:

```text
Domain
Application
Infrastructure
API
Testing.Integration
```

and Testcontainers/PostgreSQL plus host/testing dependencies.

Typical proof:

```text
HTTP/API → Application → persistence
RLS under real PostgreSQL
transaction + outbox
idempotency across layers
production DI graph
migration smoke
cross-layer auth/tenant
realtime/background effects when composed
```

---

# 27. Integration test cost discipline

Integration tests are more expensive and less localizing.

Add them when the property crosses real boundaries.

Do not duplicate every Domain happy path through a full host unless the host/integration boundary itself is the contract being protected.

---

# 28. Production graph

When a test claims production composition, instantiate the same or deliberately equivalent DI/configuration graph.

Do not replace critical services in the test so heavily that the property under test disappears.

If a replacement is necessary, state what remains unproven.

---

# 29. API tests

Use `Notrelix.API.Tests` for host/transport/public contract.

Typical proof:

```text
route/binding
authentication host integration
authorization result translation
validation/error response
idempotency/concurrency header behavior
OpenAPI shape
API version behavior
composition/startup
```

Business invariants remain cheaper in Domain/Application tests.

---

# 30. API InMemory caution

Current API tests have EF InMemory available.

This can help host/transport testing where DB semantics are not the protected property.

Do not use such a test to certify PostgreSQL/RLS behavior.

Add Integration/PostgreSQL proof when those semantics matter.

---

# 31. OpenAPI drift

When public REST shape changes:

```text
producer source
→ OpenAPI output
→ drift/snapshot/generator evidence
→ consumer regeneration/check
```

Review semantic diff.

Do not update snapshots mechanically without understanding:

```text
field
requiredness
error
version
operation identity
```

changes.

---

# 32. Architecture tests

Use `Notrelix.Architecture.Tests` for structural invariants.

Examples:

```text
project/layer dependency
forbidden namespace/reference
Domain purity
pipeline-owned authorization
context isolation
public contract boundary
solution/docs project inventory consistency
```

A structurally automatable `MUST` should be enforced here or in another machine gate where practical.

---

# 33. Architecture fixture rule

If an architecture test needs a deliberate violating fixture, make the fixture explicit and isolated.

Do not make production source itself the only example proving the gate can fail.

Critical gates should be demonstrably capable of rejecting a known violation where practical.

---

# 34. Architecture failure rule

When Architecture.Tests fails:

1. identify the canonical rule;
2. determine whether source is wrong, rule is stale, or a governed exception/change exists;
3. fix the correct owner.

Do not start by weakening the test.

---

# 35. Testing support projects

Support libraries can own reusable mechanics.

## Testing.Core

Good candidates:

```text
generic deterministic IDs/time utilities
base assertion helpers
small test primitives
```

No production dependency should be introduced merely for convenience.

## Testing.Domain

May reference Domain to provide reusable Domain builders/fixtures.

Do not encode one feature's expected business outcome as a universal builder default.

## Testing.Application

May reference Application + Domain and reusable lower-level support.

Do not grant authorization/tenant state invisibly.

## Testing.Integration

May provide reusable environment/host/database integration setup.

Do not hide which real dependencies are actually running.

---

# 36. Fixture transparency

A fixture should make material preconditions visible.

Bad:

```csharp
var user = Fixture.CreateUser();
```

where the fixture silently:

```text
creates Account
creates Workspace
grants Owner
enables entitlement
sets current tenant
```

for a permission-sensitive test.

Better:

```text
explicit builder/scenario
```

or a clearly named helper that states the role/scope it creates.

---

# 37. Builder defaults

Defaults are acceptable for irrelevant details.

Material semantic fields should be explicit when they affect the test.

Examples:

```text
Workspace role
tenant/account
resource status
aggregate version
entitlement
provider outcome
```

Do not let defaults cause every test to run as privileged owner accidentally.

---

# 38. Multi-tenant test data

For tenant-sensitive paths, use at least two distinct scopes where practical:

```text
Account/Workspace A
Account/Workspace B
```

Then prove:

```text
A can access A
A cannot access B
```

This catches missing filters that one-tenant fixtures cannot reveal.

---

# 39. Determinism

Tests should control:

```text
time
IDs
randomness
ordering
external responses
```

when those affect assertions.

Do not depend on wall-clock sleeps for concurrency/eventual behavior if a deterministic signal/clock can prove the contract.

---

# 40. Async tests

Prefer synchronization on observable state/event/condition.

Avoid:

```csharp
await Task.Delay(5000);
```

as the primary correctness proof.

If eventual processing is involved, poll/bound by a clear timeout and assert the semantic result.

---

# 41. Concurrency tests

For concurrency-sensitive code, prove relevant races with:

```text
controlled barriers
parallel tasks
database transactions
expected-version conflict
duplicate request
```

rather than probabilistic repeated loops where possible.

---

# 42. Flakiness rule

A flaky required test is a defect.

Do not solve recurring flake by:

```text
retry test N times
increase sleep indefinitely
skip test
```

without identifying the nondeterministic dependency.

If a temporary quarantine is unavoidable, govern and remove it explicitly.

---

# 43. Snapshot tests

Snapshots can prove stable large representations when semantic diff review is valuable.

They are not suitable as unreviewed mass-output approval.

When updating:

```text
inspect why each material diff changed
```

Do not regenerate solely because CI asks.

---

# 44. Mock discipline

Mock boundaries, not implementation detail.

Good mock targets:

```text
provider port
clock
authorization dependency when testing handler orchestration
repository/query port
message transport boundary
```

Avoid asserting long exact call order unless call order is itself the contract.

---

# 45. NSubstitute/Moq choice

Current repository has Moq broadly and NSubstitute available centrally.

Do not introduce competing mocking style inside one test area without need.

Follow the local test project convention unless there is a concrete readability/capability reason to change.

Tool choice is secondary to proof quality.

---

# 46. Assertions

Prefer assertions on:

```text
observable result
state
event
persisted data
HTTP contract
security denial
message effect
```

over implementation-private details.

Assertion libraries help readability but do not define test architecture.

---

# 47. Test naming

Name the scenario/property.

Prefer:

```text
UpdateItem_WhenExpectedVersionIsStale_ReturnsConflictWithoutMutation
```

over:

```text
TestUpdateItem2
```

For architecture tests, include the forbidden/required property in the name.

---

# 48. Arrange / Act / Assert

Use AAA or Given/When/Then where it improves readability.

Do not force ceremony into tiny value-object tests.

The test should make the protected property obvious.

---

# 49. One test, one primary reason to fail

A test can have several assertions about one scenario.

Avoid giant scenario tests where unrelated failures make diagnosis ambiguous.

Split when separate contracts deserve independent proof.

---

# 50. Boundary duplication

Duplicating one critical scenario at two seams can be valuable if they prove different things.

Example:

```text
Domain test
→ invariant

Integration test
→ same operation respects RLS + transaction in production graph
```

This is not wasteful duplication.

Duplicating the same assertion through five layers with no added property is waste.

---

# 51. Bug regression workflow

For a defect:

```text
1. identify failed contract
2. reproduce at cheapest reliable seam
3. ensure test fails before fix when practical
4. fix source
5. test passes
6. add broader proof only if failure crossed another boundary
```

Do not first write a broad integration test if a Domain invariant is the actual defect.

---

# 52. Security regression

Security bugs usually need a negative case.

Examples:

```text
forged resource ID
wrong Workspace
revoked membership
stale permission cache
missing tenant context in consumer
invalid webhook signature/replay
```

Prove denied/no mutation, not just status code if data effects matter.

---

# 53. Failure-path assertions

For rejected operations verify:

```text
no partial DB change
no Domain event if invalid
no outbox enrollment
no provider side effect
no cache update
no cursor advancement
```

as applicable.

The absence of unintended effects is part of the contract.

---

# 54. Idempotency failure-path proof

A retry test should distinguish:

```text
same logical operation repeated
```

from:

```text
two legitimate separate operations
```

Do not create new idempotency keys for each retry and claim duplicate safety.

---

# 55. Provider unknown outcome test

If an external write can time out after the provider may have committed:

```text
timeout
→ state Unknown/Pending
→ reconcile by provider identity
→ retry only if safe
```

Test this semantics at adapter/Application seam as appropriate.

---

# 56. Test data cleanup

Prefer isolated disposable state:

```text
transaction
unique tenant IDs
new Testcontainer/database
deterministic reset harness
```

according to project.

Do not let one test depend on execution order or leftovers from another.

---

# 57. Parallelization

Tests may run in parallel unless shared external state makes that unsafe.

If disabling parallelization, scope it narrowly and document the real shared resource.

Do not globally serialize the suite to hide unsafe test isolation.

---

# 58. Test secrets

Use unmistakably synthetic test values.

Never commit:

```text
live API key
real OAuth secret
real customer token/data
```

to tests/fixtures/snapshots.

Do not disable secret scanning to accommodate realistic-looking live credentials.

---

# 59. Generated fixtures

Generated test data should remain deterministic/reproducible.

If a generator produces snapshots/contracts, its producer is the authority.

Do not hand-edit generated proof files without updating generator/source.

---

# 60. Focused command rule

During implementation run the smallest relevant project/filter.

Examples:

```bash
dotnet test tests/Notrelix.Domain.Tests/Notrelix.Domain.Tests.csproj
dotnet test tests/Notrelix.Application.Tests/Notrelix.Application.Tests.csproj
dotnet test tests/Notrelix.Platform.Tests/Notrelix.Platform.Tests.csproj
```

Use filters when they select meaningful intended tests.

---

# 61. Non-zero rule

A required filtered command that runs zero intended tests is a failure.

If using a filter, verify the intended test count/execution rather than trusting exit code.

This is especially important for CI "critical test" filters.

---

# 62. Focused versus certification

Focused:

```text
fast iteration evidence
```

Certification:

```text
all gates required by the classified change
```

Never report:

```text
backend fully green
```

after only one focused test project.

---

# 63. Broader solution command

Current broad baseline:

```bash
cd backend
dotnet restore backend.slnx
dotnet build backend.slnx
dotnet test backend.slnx
```

This is useful broad evidence.

Material public/persistence/security/runtime changes may still have dedicated required CI/gates beyond this local command.

---

# 64. Change-to-proof routing

## Domain-only semantic change

Run:

```text
focused Domain tests
+
affected higher-level contract test if externally observable
```

## Application authorization/pipeline

Run:

```text
Application tests
+
security/integration negative proof as required
+
architecture gate if pipeline boundary changed
```

## Persistence/RLS/migration

Run:

```text
Infrastructure/PostgreSQL
+
Integration
+
migration/RLS critical gates
```

## Platform messaging

Run:

```text
Platform
+
production-graph Integration proof
```

## API contract

Run:

```text
API
+
OpenAPI drift
+
generated consumer evidence where applicable
```

## Architecture

Run:

```text
Architecture.Tests
+
affected normal tests
```

---

# 65. Public contract test quality

When REST/event/realtime shape changes, assert semantics:

```text
required/optional field
stable logical identity
error behavior
version compatibility
tenant scope
```

Do not only assert raw serialized JSON equals an entire snapshot when a smaller semantic assertion would be more robust.

Use snapshots where the full surface itself is the contract and diffs are meaningfully reviewed.

---

# 66. Migration upgrade fixtures

Keep representative previous-state fixtures/snapshots only when they are intentionally maintained evidence.

Do not create manual database schema snapshots that become a second schema authority.

Prefer real migration chain/bootstrap of an earlier schema/data state.

---

# 67. Existing-data migration proof

Include representative cases:

```text
normal legacy row
edge/null legacy row
invalid/unknown row
multiple tenants
large/batch boundary
already-migrated row for idempotency
```

as appropriate.

---

# 68. RLS bootstrap/lifecycle tests

When connection/session/RLS bootstrap changes, prove:

```text
new request/transaction receives correct scope
scope does not leak across pooled connections
background execution establishes scope
foreign tenant is denied
failure resets/cleans state
```

according to accepted backend ADR/architecture.

---

# 69. API host tests

For CSRF/rate limit/auth host mechanisms, test via the host/API seam where middleware/filters/config actually run.

Do not prove middleware behavior by unit-testing only a helper that production does not invoke directly.

---

# 70. Architecture rule IDs

When a canonical `MUST` has a stable rule ID and an executable gate, reference it in the test name/message/fixture where practical.

This improves:

```text
failure → rule → canonical owner
```

Do not create test-only rule IDs that compete with canonical docs.

---

# 71. Failure messages

Architecture/guard tests should explain:

```text
what violated
where
which allowed path exists
which canonical rule/topic applies
```

when practical.

A cryptic list of types with no meaning slows correct repair.

---

# 72. Test review checklist

Before adding a test:

```text
[ ] protected property identified
[ ] cheapest reliable seam chosen
[ ] test project correct
[ ] production dependency fidelity sufficient
[ ] tenant scope visible if material
[ ] failure/no-effect behavior covered if relevant
[ ] deterministic setup
[ ] no hidden product assertions in fixture
[ ] no live secrets/customer data
[ ] test name states scenario
```

---

# 73. Persistence-test checklist

```text
[ ] real PostgreSQL needed?
[ ] RLS allowed + denied
[ ] migration existing data
[ ] Npgsql/converter behavior
[ ] transaction/rollback
[ ] constraint/index
[ ] tenant scope
[ ] cleanup/isolation
```

---

# 74. Messaging-test checklist

```text
[ ] stable message identity
[ ] stable consumer identity
[ ] duplicate
[ ] transient retry
[ ] deterministic poison
[ ] dead-letter
[ ] ordering scope
[ ] cursor/ack after success
[ ] tenant context
[ ] provider/downstream effect
```

---

# 75. Authorization-test checklist

```text
[ ] authorized
[ ] insufficient permission
[ ] wrong resource/tenant
[ ] revoked/stale authority if relevant
[ ] no protected mutation on denial
[ ] RLS defense where persistence-sensitive
```

---

# 76. API-contract checklist

```text
[ ] binding
[ ] auth integration
[ ] validation/error category
[ ] permission result
[ ] idempotency/concurrency transport
[ ] OpenAPI
[ ] old/new compatibility
[ ] generated consumer
```

---

# 77. Architecture-test checklist

```text
[ ] canonical rule exists
[ ] structural property is machine-detectable
[ ] test fails on violation
[ ] false positives bounded
[ ] failure message actionable
[ ] exception handling governed, not ad hoc
```

---

# 78. Testing-support checklist

```text
[ ] reusable across multiple scenarios
[ ] no hidden privileged state
[ ] no business outcome assertion hidden in helper
[ ] dependencies point inward only as needed
[ ] defaults are semantically safe
[ ] fixture name exposes material scope/role
```

---

# 79. Test deletion rule

Delete/replace a test only when:

```text
contract no longer exists
test duplicates another proof with no unique value
test itself is invalid
canonical architecture deliberately changed
```

Do not delete a valid failing test solely to make the suite green.

If a decision changed, update canonical docs/ADR first as required.

---

# 80. Test weakening rule

Changing:

```text
exact invariant assertion
→ vague "not null"
```

or:

```text
real PostgreSQL
→ InMemory
```

to avoid a failure is a semantic weakening and requires justification.

Do not disguise it as test cleanup.

---

# 81. CI relationship

CI executes tests/gates in clean/reproducible conditions.

Tests remain the contract evidence; workflow topology is execution orchestration.

Do not encode business semantics only in shell filters or job names.

---

# 82. Critical CI filter rule

If CI selects critical tests by class/name/filter:

```text
test names/classes become part of gate discoverability
```

Rename them carefully.

A renamed test that makes a required filter select zero is a gate regression even if the test still exists elsewhere.

---

# 83. Packaging relationship

A successful Docker build is downstream packaging evidence.

It does not prove:

```text
Domain
authorization
RLS
migration
messaging
OpenAPI
```

Do not remove tests because image build succeeds.

---

# 84. Test documentation boundary

This `AGENTS.md` owns **how agents place/write/interpret tests**.

Canonical testing architecture owns **which test topology/properties exist**.

Repository Quality owns **cross-system testing standards**.

CI owns **when/how required suites execute**.

Do not duplicate exact CI YAML here.

---

# 85. Source/doc drift

If current test layout conflicts with canonical testing architecture:

1. inspect `backend.slnx`;
2. inspect csproj/source;
3. inspect canonical testing docs;
4. classify stale doc/source debt/transition;
5. update the correct owner.

Do not simply place a new test beside a legacy test if that placement is wrong.

---

# 86. Test completion report

When reporting test work, include:

```text
Protected property:
Test project:
Test/scenario:
Dependency fidelity:
Commands executed:
Relevant test count:
Result:
Broader gates:
Not applicable:
Remaining unverified:
```

For security/migration/async changes, include the critical negative/failure scenario.

---

# 87. Evidence honesty examples

Good:

```text
Verified:
- 14 Domain tests in WorkManagement ordering suite
- Platform ordering failure regression
- Integration RLS wrong-tenant scenario

Not verified:
- production rollout
```

Bad:

```text
All tests passed
```

when only a filter ran.

---

# 88. Stop conditions

Stop test implementation/review if:

- the product/architecture behavior itself is unresolved;
- the chosen test seam cannot reproduce the protected property;
- RLS/PostgreSQL behavior is being asserted through InMemory;
- a fixture must silently grant permissions for the test to pass;
- a required critical filter selects zero tests;
- a valid architecture test must be weakened to allow the source change;
- a provider mock is being cited as real provider protocol proof;
- a migration test covers only a clean empty DB for existing-data change;
- retry/async test relies only on long sleeps;
- a test requires real production secret/customer data;
- the test passes only because order/shared state leaks between tests.

---

# 89. Final test-agent rule

Before writing a backend test, be able to state:

```text
This is the protected property.
This project is the cheapest reliable seam.
These dependencies are real enough to prove it.
These fixtures expose all material tenant/permission/lifecycle assumptions.
This failure path proves no unintended effects.
This command executes the intended tests non-zero.
These broader gates, if any, prove the production boundary.
```

If you cannot state those things, the test plan is not ready.

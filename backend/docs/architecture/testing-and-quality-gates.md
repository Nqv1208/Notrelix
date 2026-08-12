---
document_id: BE-TESTING-QUALITY-GATES
document_type: architecture
status: active
owner: backend-quality-architecture
applies_to:
  - backend-tests
  - backend-ci
  - backend-quality-gates
  - backend-architecture-tests
  - backend-contract-tests
evidence:
  - backend/backend.slnx
  - backend/tests/Notrelix.Domain.Tests/
  - backend/tests/Notrelix.Application.Tests/
  - backend/tests/Notrelix.Infrastructure.Tests/
  - backend/tests/Notrelix.Platform.Tests/
  - backend/tests/Notrelix.API.Tests/
  - backend/tests/Notrelix.Integration.Tests/
  - backend/tests/Notrelix.Architecture.Tests/
  - backend/tests/Notrelix.Testing.Core/
  - backend/tests/Notrelix.Testing.Domain/
  - backend/tests/Notrelix.Testing.Application/
  - backend/tests/Notrelix.Testing.Integration/
  - .github/workflows/be-ci.yml
review_on:
  - backend-test-topology-change
  - architecture-gate-change
  - critical-test-filter-change
  - backend-ci-protected-property-change
  - openapi-drift-change
  - persistence-test-strategy-change
  - production-graph-proof-change
---

# Testing and Quality Gates

> **Tests are executable proof of protected properties. CI orchestrates that proof on a clean revision. Architecture documentation states which properties must be proven. None of these is a substitute for the others.**
>
> The backend test system is designed around **property ownership and fidelity**, not test-count maximization. A passing command that selected zero intended tests is not proof; a mock that cannot reproduce PostgreSQL/RLS/provider semantics is not proof; a Docker image build is not proof of Domain or authorization correctness.

This document is the canonical backend owner for:

- backend test topology;
- what each test project is responsible for proving;
- test placement by protected property;
- architecture-test role;
- PostgreSQL/RLS fidelity;
- public/OpenAPI contract proof;
- Platform reliability proof;
- production-graph Integration proof;
- non-zero critical execution;
- focused versus certification validation;
- test-support boundaries;
- CI protected-property model;
- evidence honesty.

Agent-level test-writing instructions are in:

```text
backend/tests/AGENTS.md
```

Repository-wide testing principles are in:

```text
../../../docs/quality/testing-strategy.md
```

---

# 1. Quality model

Backend quality follows:

```text
canonical rule
        ↓
implementation
        ↓
test/gate at reliable seam
        ↓
clean CI execution
        ↓
exact revision evidence
```

A document without executable proof can drift.

A test without a canonical protected property can become ceremony.

---

# 2. BE-TST-001 — Test the property, not the file

Choose the test project from:

```text
what must be proven?
```

not merely:

```text
which production file changed?
```

A change in one source file can require multiple test seams.

---

# 3. Current test topology

`backend/backend.slnx` currently includes:

```text
Notrelix.Domain.Tests
Notrelix.Application.Tests
Notrelix.Infrastructure.Tests
Notrelix.Platform.Tests
Notrelix.API.Tests
Notrelix.Integration.Tests
Notrelix.Architecture.Tests
```

Testing support:

```text
Notrelix.Testing.Core
Notrelix.Testing.Domain
Notrelix.Testing.Application
Notrelix.Testing.Integration
```

This topology maps intentionally to architecture seams.

---

# 4. Primary placement matrix

| Protected property | Primary proof |
|---|---|
| Domain invariant/value/lifecycle | Domain.Tests |
| Application orchestration/authorization/result | Application.Tests |
| EF/PostgreSQL/RLS/provider/cache mechanics | Infrastructure.Tests |
| delivery/idempotency/order/retry/poison | Platform.Tests |
| HTTP/auth host/error/OpenAPI | API.Tests |
| production graph/cross-layer/real PostgreSQL | Integration.Tests |
| dependency/placement/forbidden pattern | Architecture.Tests |

Broader proof can supplement but should not erase the cheaper primary seam.

---

# 5. BE-TST-002 — Cheapest reliable seam first

Prefer:

```text
Domain test
```

over:

```text
full API integration
```

for a pure invariant.

Prefer:

```text
PostgreSQL Integration test
```

over:

```text
mock/InMemory test
```

for RLS.

Speed is secondary to fidelity.

---

# 6. Domain tests

Domain tests prove deterministic owned behavior without provider/bootstrap dependencies.

Typical properties:

```text
valid transition
invalid transition
failure atomicity
semantic no-op
version
Domain event
value object normalization/equality
ordering/business calculation
lifecycle
```

---

# 7. BE-TST-003 — Domain invariant proof remains infrastructure-free

If a basic Domain invariant test requires:

```text
DbContext
HTTP host
Redis
RabbitMQ
provider
```

inspect whether the implementation boundary is wrong.

---

# 8. Failure atomicity tests

Rejected Domain operations should assert relevant absence of side effects:

```text
state unchanged
version unchanged
audit unchanged
event collection unchanged
```

not only thrown exception/result.

---

# 9. BE-TST-004 — Negative outcome proves non-mutation where relevant

A rejection is not proven fully when the test only asserts:

```text
error returned
```

and ignores partial state mutation.

---

# 10. Application tests

Application tests prove:

```text
request/handler orchestration
validation
pipeline markers
authorization
tenant/resource resolution
transaction declaration
external fact acquisition
concurrency
idempotency orchestration
cache/post-commit contracts
semantic result
```

Mock outer ports only to isolate the Application property.

---

# 11. BE-TST-005 — Application test does not mock away the behavior under test

Good:

```text
mock repository/provider port
run real handler + Domain behavior
assert result/effect contract
```

Bad:

```text
mock the Domain/business result
assert handler returns the mock
```

when orchestration of that business behavior is the property.

---

# 12. Authorization tests

Protected use cases should cover as relevant:

```text
allowed
denied
wrong tenant/resource
revoked permission
missing required scope
```

Happy-path authorization alone is insufficient for material C6 changes.

---

# 13. BE-TST-006 — Security test has at least one negative path

Test denial **and** no protected effect where applicable.

For cross-tenant security, use two distinct tenant scopes.

---

# 14. Infrastructure tests

Infrastructure tests prove:

```text
EF mappings
converters
query/read ports
PostgreSQL constraints
RLS
migrations
Redis/cache adapters
provider adapter mapping
storage/search mechanics
identity/auth provider mechanics
```

Use the real dependency when it defines the property.

---

# 15. PostgreSQL fidelity

Notrelix production relational semantics depend on PostgreSQL/Npgsql.

Current backend CI explicitly guards against SQLite dependencies/usage and directs persistence fidelity toward PostgreSQL/Testcontainers.

---

# 16. BE-TST-007 — SQLite is not backend persistence substitute

Do not introduce SQLite to make tests easier for properties that must match production PostgreSQL.

Current CI treats SQLite dependency/source usage as a guard failure.

---

# 17. EF InMemory

EF InMemory can be used for tests whose protected property does not depend on real relational/provider semantics.

It cannot prove:

```text
RLS
Npgsql conversion
locking
transaction isolation
migration DDL
real FK/index/constraint behavior
PostgreSQL SQL
```

---

# 18. BE-TST-008 — InMemory evidence states its fidelity boundary

Do not report an InMemory API/Application test as:

```text
RLS verified
migration verified
PostgreSQL transaction verified
```

Add Infrastructure/Integration proof.

---

# 19. RLS tests

RLS proof requires real PostgreSQL and both sides:

```text
allowed tenant can access
foreign tenant cannot access
```

where applicable also:

```text
pool reuse
bootstrap
full request scope
background/system execution
```

---

# 20. BE-TST-009 — RLS denial is first-class test evidence

Do not infer isolation from:

```text
query includes WorkspaceId
```

or policy SQL text alone.

Execute PostgreSQL policy under realistic session context.

---

# 21. Migration tests

A migration touching existing data should prove:

```text
representative previous schema/data
→ apply migration
→ data transformed correctly
→ constraints/RLS valid
→ Application read/write remains correct
```

not only clean DB creation.

---

# 22. BE-TST-010 — Existing-data change requires upgrade proof

If production can contain old rows, an empty database is not representative evidence.

Include legacy/edge data and invalid-data policy where relevant.

---

# 23. Platform tests

Platform tests prove reusable delivery mechanics:

```text
message/consumer identity
envelope
compatibility
outbox/post-commit
idempotency
ordering
retry
poison/dead-letter
replay
transport policy
```

Current CI explicitly verifies critical Platform tests for ordering, poison, and consumer delivery contracts executed.

---

# 24. BE-TST-011 — Reliability property includes failure path

A messaging change SHOULD reproduce the changed failure mode:

```text
duplicate
handler failure
retry
gap
poison
replay
```

not only one successful delivery.

---

# 25. Ordering tests

Applicable matrix:

```text
same key ordered
out-of-order
gap
failed handler
retry
cursor after success
different key independent progress
```

---

# 26. BE-TST-012 — Cursor/ack ordering is explicitly tested

For ordered delivery, prove:

```text
handler succeeds
→ cursor/ack advances
```

and:

```text
handler fails
→ cursor/ack does not falsely advance
```

---

# 27. Idempotency tests

Applicable:

```text
first execution
same identity/same request
same identity/conflicting request
concurrent duplicate
retry after transient failure
consumer-specific dedup
retention/replay edge
```

---

# 28. BE-TST-013 — Duplicate and conflict are separate test cases

Same key/message identity with a different semantic request MUST NOT be silently asserted as duplicate success.

---

# 29. Poison tests

Applicable:

```text
deterministic invalid
transient recovery
retry exhaustion
consumer-scoped poison
dead-letter identity
recovery/replay
```

---

# 30. BE-TST-014 — Poison test verifies scope

If consumer A poisons message X, prove consumer B is unaffected when architecture says poison is consumer-scoped.

---

# 31. Replay tests

Applicable:

```text
bounded selection
checkpoint resume
dedup interaction
ordering
force/reprocess semantics
tenant scope
provider side-effect safety
throttle/fairness
```

---

# 32. BE-TST-015 — Replay test proves bounded recovery, not mass republish

A replay engine passing a “publish all” happy test does not prove safe production recovery.

---

# 33. API tests

API tests prove:

```text
route/binding
authentication host integration
CSRF/rate limit/security middleware
ProblemDetails
idempotency transport
versioning
OpenAPI metadata/export
public contract shape
```

Business invariants remain cheaper in Domain/Application tests.

---

# 34. BE-TST-016 — API test proves transport/public contract

Do not duplicate every Domain transition through HTTP merely to increase coverage.

Add API proof when:

```text
host/security/binding/error/contract
```

is the property.

---

# 35. OpenAPI drift

Current backend CI exports:

```text
contracts/openapi/notrelix.v1.json
```

from the API producer and compares generated output for drift.

This proves generated artifact consistency.

It does not decide whether a changed contract is backward compatible.

---

# 36. BE-TST-017 — OpenAPI gate has two layers

Required reasoning:

```text
producer ↔ generated artifact drift
+
semantic compatibility review
```

A regenerated spec that matches source can still be a breaking product contract.

---

# 37. API idempotency gate

Current CI explicitly verifies:

```text
IdempotencyEndpointContractTests
```

executed in the API suite.

This is a critical transport contract.

Do not rename/remove it in a way that makes the required filter select zero without updating the gate deliberately.

---

# 38. BE-TST-018 — Critical-test discoverability is part of CI contract

When CI verifies named classes/suites, refactoring those test identifiers requires changing the verifier atomically.

Green zero-work is forbidden.

---

# 39. Integration tests

Integration tests prove properties requiring the composed production graph.

Current Integration suite/CI includes critical proof for:

```text
idempotency store
tenant isolation
cross-tenant isolation
RLS runtime enforcement
messaging dedup
outbox dispatch/claim/atomicity
migration smoke
production composition
production graph
Redis cache behavior
realtime dispatch
```

This is current executable evidence.

---

# 40. BE-TST-019 — Integration test adds a distinct cross-boundary property

Use Integration when:

```text
real PostgreSQL
RLS
transaction + outbox
DI composition
host + Application + persistence
cache/provider/runtime graph
```

matters.

Do not duplicate pure Domain behavior with no additional boundary.

---

# 41. Production composition

A production-composition test should instantiate the real or deliberately equivalent DI graph.

Replacing critical services can invalidate the claim.

---

# 42. BE-TST-020 — “Production graph” claim names what is real versus substituted

If a test replaces:

```text
provider
transport
cache
DB
```

state what remains unproven.

Do not call a heavily mocked host “production graph”.

---

# 43. Architecture tests

Architecture tests prove machine-detectable structural rules.

Current project references production assemblies and Roslyn and has current areas including:

```text
ApplicationLayer
Authorization
DataAccess
DomainPurity
EndpointContracts
Events
InfrastructureLayer
LayerRules
Pipeline
PlatformMessaging
ScopingRules
Solution
```

plus legacy/baseline/freeze support.

Exact folder inventory is current evidence.

---

# 44. BE-TST-021 — Architecture gate protects canonical rule

Examples:

```text
project dependency direction
Domain purity
handler data-port boundary
authorization ownership
endpoint contract
context isolation
pipeline placement/order
forbidden dependency
```

Do not encode arbitrary style preference as architecture gate.

---

# 45. Domain purity gates

Current CI explicitly verifies critical architecture classes such as:

```text
DomainBoundedContextSignatureTests
DomainReferenceGraphTests
```

executed.

This is foundation evidence for Domain isolation.

---

# 46. Application data-port gates

Current CI also verifies:

```text
HandlerDataPortGateTests
HandlerConstructorPortGateTests
```

executed.

These are executable evidence that handler persistence/dependency boundaries are foundation properties.

---

# 47. BE-TST-022 — Architecture gate failure is diagnosed against canonical authority first

When a gate fails ask:

```text
source wrong?
canonical rule changed?
approved exception?
test false positive?
```

Do not immediately weaken/delete the gate.

---

# 48. Deliberate violating fixture

For critical structural gates, it is valuable to prove the test can reject a known violation.

Do this with isolated fixtures where practical.

Do not use production source debt as the only demonstration that the gate works.

---

# 49. BE-TST-023 — Critical gate should be falsifiable

A gate that can never fail due to a broken scan/filter is not protection.

Validate its discovery/execution path.

---

# 50. Test-support projects

Current reusable support layers are:

```text
Testing.Core
Testing.Domain
Testing.Application
Testing.Integration
```

They reduce setup duplication.

They do not own hidden product truth.

---

# 51. BE-TST-024 — Test support preserves visible semantic preconditions

A helper MUST NOT silently:

```text
grant Owner
create entitlement
set system context
disable RLS
authorize every resource
```

for security-sensitive tests without clear naming/intent.

---

# 52. Fixture default rule

Defaults are good for irrelevant details.

Material state should be explicit:

```text
tenant
role
permission
resource status
aggregate version
entitlement
provider outcome
```

when it affects the scenario.

---

# 53. BE-TST-025 — Fixture convenience cannot create false-green privilege

If every default user is Owner/admin, authorization tests can miss missing policy checks.

Use multi-role/multi-tenant scenarios.

---

# 54. Multi-tenant fixture

For tenant-sensitive behavior, representative fixture should include at least:

```text
Account/Workspace A
Account/Workspace B
```

where practical.

---

# 55. BE-TST-026 — One-tenant tests cannot certify cross-tenant isolation

A query with no tenant predicate passes perfectly in a database containing only one tenant.

Add a foreign-tenant row and assert non-observation.

---

# 56. Mocks

Mock boundaries, not implementation choreography.

Good:

```text
provider port
clock
repository/query port
authorization dependency for unrelated handler test
```

Avoid long `Verify` chains for private call order unless order is the contract.

---

# 57. BE-TST-027 — Mock cannot prove the behavior it replaces

A mocked provider cannot prove:

```text
real OAuth
webhook signature
network timeout
provider idempotency
rate limit
TLS
```

Use the appropriate integration/contract seam.

---

# 58. Determinism

Control where relevant:

```text
time
IDs
randomness
ordering
provider response
concurrency barrier
```

Avoid wall-clock sleeps as primary proof.

---

# 59. BE-TST-028 — Async test synchronizes on semantic condition

Prefer:

```text
poll bounded state
await completion signal
test clock
barrier
```

over arbitrary:

```text
Task.Delay(5000)
```

for correctness.

---

# 60. Flakiness

A flaky required gate is quality debt.

Do not normalize:

```text
retry test N times
increase sleep
disable parallelization globally
skip
```

without root-cause reasoning.

---

# 61. BE-TST-029 — Quarantined critical test is a governed exception

If temporary quarantine is unavoidable:

```text
owner
scope
reason
risk
removal condition
replacement evidence
```

must exist.

Do not silently remove from required CI.

---

# 62. Snapshot tests

Snapshots are useful when the whole large representation is a meaningful contract.

Current Domain CI also checks committed snapshots did not drift after Domain tests.

Review the diff semantics.

---

# 63. BE-TST-030 — Snapshot update is not mechanical acceptance

Do not regenerate a changed snapshot solely because source changed.

Ask whether the semantic output should have changed.

---

# 64. Test naming

Names/classes should identify the protected scenario/property.

This is especially important when CI discovers critical suites by fully qualified class name.

---

# 65. BE-TST-031 — Critical test rename is a gate change

Update:

```text
test identifier
CI verifier/filter
docs if referenced
```

atomically.

Do not leave CI verifying a class that no longer exists.

---

# 66. Non-zero execution

A test command can exit 0 while selecting zero tests.

Critical suites need explicit execution verification.

Current backend CI uses TRX inspection through:

```text
scripts/ci/verify-required-tests-trx.py
```

for critical architecture/Infrastructure/Platform/API/integration suites.

---

# 67. BE-TST-032 — Empty success is failure for required evidence

If a critical gate expected test X and X did not execute:

```text
fail
```

even if `dotnet test` returned success.

---

# 68. Focused validation

During implementation run the smallest useful test.

Examples:

```bash
dotnet test tests/Notrelix.Domain.Tests/Notrelix.Domain.Tests.csproj
dotnet test tests/Notrelix.Application.Tests/Notrelix.Application.Tests.csproj
dotnet test tests/Notrelix.Platform.Tests/Notrelix.Platform.Tests.csproj
```

Focused feedback is not final certification.

---

# 69. BE-TST-033 — Focused pass is reported as focused pass

Do not say:

```text
backend tests pass
```

after one project/filter.

Report exact command/scope.

---

# 70. Broad local baseline

Current broad commands:

```bash
cd backend
dotnet restore backend.slnx
dotnet build backend.slnx
dotnet test backend.slnx
```

These provide broad local evidence.

CI may add protected checks not represented by one `dotnet test` command.

---

# 71. Certification

Certification is change-class dependent.

Examples:

```text
C2 Domain behavior
→ focused Domain + affected contract proof + required CI

C4 migration/RLS
→ Infrastructure + Integration PostgreSQL + migration/RLS critical gates

C5 architecture
→ Architecture.Tests + affected normal suites

C6 security
→ Application/API/Infrastructure/Integration negative proof + architecture gates

C3 public API
→ API + OpenAPI drift + consumer compatibility
```

---

# 72. BE-TST-034 — Certification follows protected property, not fixed “run everything” ritual alone

Running everything is useful.

But a broad suite can still miss:

```text
critical test filter selected zero
migration old-state fixture absent
provider not real enough
architecture gate missing
```

Required evidence remains explicit.

---

# 73. Current CI protected-property topology

Current backend CI separates:

```text
change detection
quality/security guards
architecture tests
Domain/Application/Infrastructure tests
Platform tests
API + OpenAPI
Integration/provider tests
Docker build
final Backend CI gate
```

This current topology is implementation evidence.

The durable architecture is the set of protected properties and dependencies between them.

---

# 74. BE-TST-035 — CI topology may evolve; protected properties do not silently disappear

A workflow refactor MAY merge/split jobs.

It MUST preserve required:

```text
quality
architecture
core behavior
Platform
API/OpenAPI
Integration
packaging
final certification
```

or explicitly change the architecture/governance.

---

# 75. Quality guards

Current quality job performs:

```text
restore
format verification
solution build
vulnerability scan
SQLite dependency/source guards
```

These are build/security hygiene.

They do not replace behavioral tests.

---

# 76. BE-TST-036 — Build/format/vulnerability success is not semantic correctness

A perfect build can still contain:

```text
authorization bypass
RLS bug
wrong Domain invariant
message loss
breaking API
```

Keep semantic proof separate.

---

# 77. Architecture job dependency

Current architecture tests depend on quality passing.

Architecture failure blocks downstream final gate.

Do not make architecture informational-only if it protects canonical MUST rules.

---

# 78. BE-TST-037 — Required architecture gate is merge-blocking evidence

If a canonical foundation rule is machine-enforced, CI failure must fail certification unless an approved exception/change updates the rule/gate.

---

# 79. Core tests

Current CI groups:

```text
Domain
Application
Infrastructure
```

under a core job while still executing separate test projects and verifying critical Infrastructure guards.

Grouping is workflow convenience.

Test ownership remains separate.

---

# 80. Platform job

Current CI executes Platform tests independently and verifies critical reliability suites.

This ensures messaging/reliability mechanisms are not hidden inside a broad solution pass.

---

# 81. API job

Current CI runs API tests, verifies API idempotency contract test execution, exports OpenAPI, and fails on drift.

This protects transport/public contract.

---

# 82. Integration job

Current CI runs Integration tests with Redis service and verifies critical foundation provider/production-graph tests.

The integration project uses real PostgreSQL/Testcontainers according to project architecture.

---

# 83. BE-TST-038 — Integration critical list is curated evidence, not a complete test inventory

The named critical classes guarantee foundation coverage.

Other tests remain valuable and run as part of the suite.

Do not assume tests not on the critical list are optional/irrelevant.

---

# 84. Docker build

Current CI builds the backend image after:

```text
architecture
core
Platform
API
Integration
```

succeed.

This establishes packaging dependency.

---

# 85. BE-TST-039 — Docker build is packaging proof only

A successful image build proves:

```text
Dockerfile/build context/dependencies compile/package
```

It does not prove runtime smoke, authorization, RLS, messaging, or API semantics unless separate steps execute them.

---

# 86. Final gate

Current `backend-ci` job evaluates required upstream job results and reports success for the exact workflow SHA.

This gives one branch-protection-friendly certification surface.

---

# 87. BE-TST-040 — Final CI gate is an aggregator, not a substitute

The final gate MUST fail when a required upstream protected property fails/skips unexpectedly.

Do not turn the aggregator green while ignoring failed required jobs.

---

# 88. Change detection

Current workflow avoids expensive backend jobs for frontend-only PRs while still returning a final gate.

This is CI efficiency, not semantic skipping.

---

# 89. BE-TST-041 — Relevant change detection cannot hide backend-affecting input

If root/shared files affect backend behavior, CI path detection must include them or deliberately run backend checks.

Do not optimize paths at the cost of missing a required change.

---

# 90. Exact revision

CI evidence belongs to the exact source revision executed.

Do not cite green CI from an older SHA after code changed.

---

# 91. BE-TST-042 — Certification is SHA-specific

Required merge/release claim should identify:

```text
exact SHA
required gates
results
```

where the workflow/release process needs certification.

---

# 92. Test command exit code

Exit code is one signal.

For critical filters, also verify:

```text
intended tests actually ran
```

For snapshots/generated artifacts, verify no unexpected drift.

---

# 93. BE-TST-043 — Evidence can have secondary guard

Examples:

```text
dotnet test
+
TRX required-class verification

Domain tests
+
git diff snapshot check

API tests
+
OpenAPI regenerate/cmp
```

when empty/drift failure is otherwise possible.

---

# 94. Test count

Do not chase a global target test count/coverage percentage as primary quality metric.

Coverage can identify gaps.

Protected properties and representative failure modes matter more.

---

# 95. BE-TST-044 — Coverage percentage is diagnostic, not acceptance by itself

100% line coverage can still miss:

```text
cross-tenant data
concurrency race
wrong event order
migration old data
provider unknown outcome
```

Use risk/property-based proof.

---

# 96. Mutation testing

Mutation testing MAY help high-risk pure logic if useful.

It is not a repository-wide mandatory ceremony unless adopted explicitly.

Do not introduce costly tooling without a concrete gap.

---

# 97. Property-based testing

Property-based testing MAY be useful for:

```text
ordering
value objects
serialization/canonicalization
index/key generators
```

where invariant space is large.

Use when it increases confidence beyond example tests.

---

# 98. BE-TST-045 — Advanced technique is justified by risk, not fashion

Do not require:

```text
property tests
mutation tests
fuzzing
```

for every trivial feature.

Use them where they protect a real class of failure.

---

# 99. Fuzz/security testing

Parsers/public/webhook/file/serialization boundaries may benefit from adversarial/fuzz tests.

Keep representative cases in deterministic regression tests.

---

# 100. Provider test fidelity

Provider mocks can prove:

```text
mapping
classification
retry orchestration
```

Real sandbox/contract tests can prove provider protocol behavior when safe/available.

Do not make CI depend on flaky live production providers by default.

---

# 101. BE-TST-046 — Provider evidence names fidelity

Report:

```text
mock adapter test
sandbox provider test
contract fixture
real PostgreSQL
real Redis
```

rather than generic “integration tested”.

---

# 102. Redis tests

Current Integration CI runs a Redis service and includes critical Redis cache behavior proof.

Use real Redis when:

```text
serialization
TTL
atomic command
connection/failure behavior
```

is the protected property.

---

# 103. BE-TST-047 — In-memory cache does not certify Redis-specific behavior

Use fake cache for Application cache orchestration.

Use Redis-realistic integration for Redis mechanics.

---

# 104. Broker tests

InMemory transport can prove generic Platform semantics.

Real broker evidence is needed when RabbitMQ-specific:

```text
ack/redelivery
routing
durability
connection recovery
```

is the property.

Do not overclaim InMemory.

---

# 105. BE-TST-048 — Transport test fidelity follows transport claim

If no real-broker test exists for a broker-specific property, state it as unverified rather than claiming production broker behavior.

---

# 106. Contract compatibility tests

For event/API/realtime changes, test:

```text
old producer/new consumer
new producer/old consumer
```

as required by support matrix.

Do not only test new/new.

---

# 107. BE-TST-049 — Mixed-version compatibility is independent test dimension

Same-revision unit tests cannot prove old mobile/worker/backlog compatibility automatically.

Use fixtures/schema compatibility/generator checks appropriate to the contract.

---

# 108. Migration fixtures

Keep representative old-state fixtures only when maintained intentionally.

Do not create a handwritten schema snapshot that becomes a second schema authority.

Build old state through real migrations/data setup where practical.

---

# 109. BE-TST-050 — Migration fixture identifies its source version/state

A fixture named only:

```text
legacy
```

without known meaning becomes stale/ambiguous.

Record which old shape/semantic it represents.

---

# 110. Test data privacy

Never use real customer data/secrets in tests.

Synthetic data should be clearly synthetic.

Do not disable secret scanning to keep realistic-looking live credentials.

---

# 111. BE-TST-051 — Test secret is intentionally fake

Use values that cannot authenticate to real services.

Keep secret-shaped test data scoped to fixtures/CI config and documented as synthetic where scanners require allowlisting.

---

# 112. CI secret scanning/vulnerability

Current quality job includes dependency vulnerability scanning.

Secret scanning may be another repository/platform control.

Do not treat a scanner passing as proof no sensitive runtime data can leak to logs/events.

Behavioral security tests/code review still matter.

---

# 113. Performance tests

Performance-sensitive changes may need:

```text
query-plan evidence
benchmark
load test
allocation/profile
```

according to risk.

Do not invent universal latency thresholds.

Repository performance standard owns methodology.

---

# 114. BE-TST-052 — Performance proof states workload/cardinality

A benchmark without:

```text
dataset size
tenant distribution
concurrency
operation
environment
```

is weak evidence.

---

# 115. Concurrency tests

Use:

```text
barriers
parallel operations
real transactions
expected version
duplicate key
```

to reproduce races deterministically where possible.

Avoid probabilistic loops as the only proof.

---

# 116. BE-TST-053 — Race test controls the race

A concurrency regression should fail reliably under the buggy implementation, not once in 10,000 runs by chance.

---

# 117. Failure injection

Infrastructure/Platform tests may inject:

```text
DB failure
provider timeout
transport failure
consumer exception
```

to prove rollback/retry/recovery semantics.

Use controlled fakes where they preserve the property.

---

# 118. BE-TST-054 — Failure injection verifies resulting durable state

Do not assert only “exception thrown”.

Check:

```text
transaction rolled back
idempotency state
outbox
cursor
retry classification
provider effect count
```

as applicable.

---

# 119. Test isolation

Tests must not depend on execution order or stale shared state.

Use:

```text
unique IDs
isolated DB/container/schema
transaction reset
deterministic fixture cleanup
```

according to seam.

---

# 120. BE-TST-055 — Parallel safety is explicit

If tests cannot run in parallel due to a real shared resource, scope serialization narrowly.

Do not globally disable parallelization to hide isolation defects.

---

# 121. Flaky external timing

Avoid exact millisecond assertions for asynchronous delivery unless time itself is the contract.

Use bounded eventually/assertion around semantic state.

---

# 122. BE-TST-056 — Eventual assertion has a bounded deadline and meaningful condition

Do not poll forever.

Do not use a deadline so large that hung behavior delays CI excessively without diagnostic value.

---

# 123. Error messages in tests

Architecture/gate failure should explain:

```text
violating type/path
canonical rule/topic
expected allowed dependency/placement
```

where practical.

This improves correct repair.

---

# 124. BE-TST-057 — Gate failure is actionable

A test that only prints:

```text
false
```

or a massive undifferentiated type list is poor governance evidence.

---

# 125. Test deletion

Delete/change a valid test only when:

```text
contract deliberately changed
test duplicated with no unique property
test itself is invalid
architecture changed with approved authority
```

Do not delete a regression because implementation no longer passes.

---

# 126. BE-TST-058 — Failing valid test is evidence, not obstacle

Fix source or change the canonical contract deliberately.

Do not normalize regression by weakening assertion.

---

# 127. Test weakening

Examples of semantic weakening:

```text
real PostgreSQL → InMemory
exact denied result → not-null
wrong-tenant assertion removed
cursor-after-success assertion removed
```

Such changes require review as protected-property changes.

---

# 128. BE-TST-059 — Test refactor preserves protected property

A cleaner test is welcome.

It must prove at least the same contract unless the contract itself changed.

---

# 129. Architecture exception testing

A temporary exception can require:

```text
test that prevents scope expansion
test that keeps unrelated cases enforcing canonical rule
```

Do not disable the whole gate for one scoped exception.

---

# 130. BE-TST-060 — Exception narrows gate, never deletes architecture protection globally

Implement allowlist/scope with removal condition where machine-enforceable.

---

# 131. CI timeout

Timeout protects runner capacity and detects hangs.

Do not reduce timeout below normal healthy duration simply to make failures faster.

Do not increase indefinitely to hide deadlocks/hangs.

Current jobs use bounded timeouts and `--blame-hang-timeout`.

---

# 132. BE-TST-061 — Hang is a diagnosable failure class

Use blame/dumps/logs where available.

Do not rerun hung suite until it happens to pass and call it green.

---

# 133. Artifacts/results

TRX/log/generated diff can support diagnosis.

They are execution evidence, not permanent architecture authority.

Do not retain sensitive test/provider payloads unnecessarily.

---

# 134. Generated test evidence

Generated snapshots/OpenAPI must be deterministic.

If generator/source changes intentionally:

```text
regenerate
review diff
commit producer + output
```

Do not hand-edit output.

---

# 135. BE-TST-062 — Generated artifact check is producer-oriented

When drift occurs, inspect the producer first.

The generated file is not where architecture should be fixed.

---

# 136. Test ownership

The product/context owner owns semantic expected behavior.

Backend quality architecture owns proof topology/mechanism.

Test project maintainers do not acquire product authority by writing assertions.

---

# 137. BE-TST-063 — Test does not become canonical product spec by accident

If a test reveals a durable new product rule not documented anywhere, update the canonical product/context owner.

Do not force future engineers to infer all semantics from test names.

---

# 138. Documentation versus test

Docs say:

```text
what must be true
```

Tests say:

```text
the implementation satisfies it here
```

CI says:

```text
that proof ran successfully on this revision
```

Keep all three aligned.

---

# 139. BE-TST-064 — Green test with stale canonical rule is not enough

If architecture/product decision changed, update:

```text
canonical docs/ADR
implementation
tests/gates
```

as one governed change.

---

# 140. Current CI dependencies

Current flow requires Docker packaging only after architecture/core/Platform/API/integration jobs succeed.

Final gate aggregates all required job results.

This ordering is current executable evidence.

---

# 141. BE-TST-065 — Downstream packaging cannot bypass failed semantic proof

Do not run/build/package a release as “certified” if required upstream behavior/security/architecture gates failed.

---

# 142. Required branch/path behavior

Current backend workflow runs on selected branches and handles PR change detection without leaving required checks pending for frontend-only PRs.

Exact branches/path filters are workflow policy, not backend architecture.

Do not duplicate them here as permanent rules.

---

# 143. Evidence report

When reporting verification include:

```text
protected property
test project
exact command
relevant test/class count or named critical suite
result
broader gate
exact SHA when CI
unverified/pending
```

---

# 144. BE-TST-066 — Evidence claim never exceeds execution

Allowed:

```text
Verified Application authorization suite.
Integration RLS gate pending.
```

Not allowed:

```text
All backend security verified.
```

after only unit tests.

---

# 145. Change-to-proof table

## Domain behavior

```text
Domain.Tests
+
affected Application/API/Integration only if another boundary changes
```

## Application pipeline/authz

```text
Application.Tests
+
Architecture.Tests
+
Integration negative/production proof as required
```

## RLS/migration

```text
Infrastructure.Tests
+
Integration PostgreSQL
+
critical RLS/migration gate
```

## Platform reliability

```text
Platform.Tests
+
Integration source-commit/persistence proof
```

## API/public contract

```text
API.Tests
+
OpenAPI drift
+
consumer compatibility
```

## architecture

```text
Architecture.Tests
+
affected normal behavior suites
```

---

# 146. BE-TST-067 — Higher-risk change accumulates proof

A C4+C6 migration does not choose between:

```text
migration
or
security
```

It needs both.

Change obligations are cumulative.

---

# 147. Review checklist

```text
[ ] protected property stated
[ ] cheapest reliable seam
[ ] correct project
[ ] dependency fidelity sufficient
[ ] negative/failure path
[ ] tenant/security data where relevant
[ ] no hidden privileged fixture
[ ] no mock overclaim
[ ] non-zero execution
[ ] broader required gate
[ ] generated/drift check
[ ] exact evidence report
```

---

# 148. Architecture-gate checklist

```text
[ ] canonical MUST exists
[ ] machine detection reliable
[ ] violation fixture/path possible
[ ] false-positive scope acceptable
[ ] failure actionable
[ ] exception path governed
[ ] CI executes non-zero
```

---

# 149. Integration-gate checklist

```text
[ ] real dependency required
[ ] real PostgreSQL/RLS where relevant
[ ] production-like DI graph
[ ] tenant A/B
[ ] failure path
[ ] cleanup/isolation
[ ] deterministic bounded wait
[ ] critical class discoverable
```

---

# 150. CI-gate checklist

```text
[ ] relevant changes trigger it
[ ] required predecessor jobs
[ ] non-zero critical proof
[ ] clean restore/build
[ ] generated drift
[ ] artifact/package proof
[ ] final aggregation
[ ] exact SHA
[ ] no silent skip
```

---

# 151. Stop conditions

Stop test/gate implementation if:

- the behavior itself is unresolved;
- the chosen seam cannot reproduce the protected property;
- RLS/PostgreSQL is being “proven” through InMemory;
- every fixture silently grants privileged access;
- critical filter selects zero tests;
- architecture gate is being weakened to make source pass;
- Docker build is being cited as semantic proof;
- migration test covers only clean DB for existing-data change;
- provider mock is cited as real provider protocol proof;
- replay/concurrency correctness relies only on arbitrary sleeps;
- CI green belongs to another SHA;
- a failing valid regression is being deleted rather than fixed.

---

# 152. Executable evidence

Primary inventory:

```text
backend/backend.slnx
backend/tests/**
```

Current workflow evidence:

```text
.github/workflows/be-ci.yml
```

Broad local:

```bash
cd backend
dotnet restore backend.slnx
dotnet build backend.slnx
dotnet test backend.slnx
```

Current CI additionally verifies critical suites and generated/public contract drift.

---

# 153. Related canonical owners

Backend:

```text
backend-overview.md
domain-modeling.md
application-model.md
infrastructure-and-data.md
platform-and-messaging.md
api-and-contracts.md
security-tenancy-authorization.md
```

Repository:

```text
../../../docs/quality/engineering-quality-standard.md
../../../docs/quality/testing-strategy.md
../../../docs/quality/security-quality-standard.md
../../../docs/delivery/definition-of-done.md
../../../docs/delivery/change-classification.md
```

---

# 154. Non-responsibilities

This document does not define:

```text
frontend test architecture
exact permanent CI job names
organization branch-protection settings
test coverage percentage target
product-specific acceptance scenarios
vendor observability platform
release approval staffing
```

Those belong to other owners.

---

# 155. Final testing rule

Backend evidence is trustworthy when it can be stated as:

```text
canonical protected property
        ↓
cheapest reliable test seam
        ↓
real enough dependency fidelity
        ↓
positive + failure/negative behavior
        ↓
non-zero execution
        ↓
broader cross-boundary gate when required
        ↓
clean CI on exact revision
```

and when:

```text
InMemory is not called PostgreSQL
mock is not called provider proof
Docker build is not called runtime correctness
snapshot update is not called semantic review
zero selected tests are not called green
focused pass is not called full certification
```

The objective is not the largest test suite.

The objective is a **small number of explicit, executable proofs for every property whose failure would break product semantics, tenant safety, persistence correctness, delivery reliability, public compatibility, or the architecture that protects those properties**.

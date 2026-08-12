---
document_id: QLT-ENGINEERING
document_type: quality-standard
status: active
owner: engineering-quality
applies_to:
  - repository
  - backend
  - frontend
  - documentation
  - ci
evidence:
  - RULE.md
  - PRODUCT.md
  - DESIGN.md
  - docs/governance/documentation-quality-gates.md
  - docs/quality/testing-strategy.md
  - backend/backend.slnx
  - frontend/package.json
  - .github/workflows/be-ci.yml
  - .github/workflows/fe-ci.yml
review_on:
  - quality-bar-change
  - required-ci-gate-change
  - architecture-gate-change
  - test-strategy-change
  - review-policy-change
  - release-quality-change
---

# Engineering Quality Standard

> **Quality is executable evidence that a change preserves product meaning, architecture boundaries, security and tenant guarantees, compatibility, operational correctness, and maintainability.**
>
> Formatting, lint, and test counts are useful evidence. None of them, individually or together, are the definition of quality.

This document owns the repository-wide engineering quality bar.

Technology-specific implementation details remain in backend/frontend documentation and executable tooling.

Detailed testing ownership belongs to `testing-strategy.md`.

Security, accessibility, and performance have dedicated quality owners.

---

# 1. Purpose

This standard defines what “ready”, “correct”, and “high quality” mean for Notrelix engineering work.

A change is complete only when:

```text
intended semantics are clear
architecture ownership remains valid
implementation preserves invariants
failure behavior is intentional
security/tenant boundaries remain intact
compatibility impact is handled
required evidence exists
CI executes meaningful work
documentation remains coherent
```

---

# 2. QLT-001 — Correctness is semantic, not syntactic

A buildable, formatted, lint-clean change can still be defective if it:

- violates product ownership;
- weakens authorization;
- breaks tenant isolation;
- introduces duplicate truth;
- changes public contract accidentally;
- hides failure;
- bypasses concurrency/idempotency;
- makes required CI execute zero meaningful work.

---

# 3. Quality evidence model

Evidence is layered:

```text
canonical docs
    define intended contract

source
    implements it

tests
    exercise behavior/invariants

architecture gates
    enforce structural rules

contract/generated drift checks
    enforce compatibility

CI
    proves exact revision executed required evidence

review
    evaluates semantics not mechanically captured
```

No layer silently replaces another.

---

# 4. QLT-002 — Evidence owner follows protected property

Examples:

```text
Domain invariant
→ Domain behavior tests

project/package dependency
→ architecture/dependency gate

RLS / database protocol
→ realistic integration evidence

public API contract
→ OpenAPI/contract drift + API tests

frontend package graph
→ dependency-rules

generated client
→ codegen drift

critical user journey
→ production-like E2E
```

Do not duplicate every gate in every job.

---

# 5. Quality dimensions

Repository quality is evaluated across:

```text
semantic correctness
architecture
maintainability
failure atomicity
security
tenant isolation
data consistency
concurrency
idempotency/retry
compatibility
testing
accessibility
performance/scalability
observability/recovery
documentation
delivery/release evidence
```

Depth follows risk.

---

# 6. QLT-003 — Quality depth follows blast radius

A local copy change and a migration of tenant ownership do not require identical proof.

Higher-risk changes require broader evidence.

Risk dimensions include:

```text
customer-data impact
security
cross-tenant risk
public compatibility
irreversibility
distributed side effects
schema migration
high concurrency
large fan-out
mobile/external consumer lag
operational recovery cost
```

---

# 7. Quality is not file symmetry

Do not create:

- wrappers;
- abstractions;
- test projects;
- docs;
- interfaces

merely because another module has one.

A quality mechanism exists when it protects a real property.

---

# 8. QLT-004 — One responsibility has one visible owner

A type/module/package should make its responsibility and architectural owner understandable.

Names such as:

```text
Common
Utils
Helpers
Manager
Misc
Shared
```

are warning signs when they conceal mixed ownership.

They are not automatically forbidden.

---

# 9. Cohesion

A unit should change for a coherent reason.

Split when it mixes:

- business policy and provider protocol;
- orchestration and persistence mechanics;
- product semantics and UI rendering;
- unrelated lifecycle responsibilities.

Do not split solely by arbitrary line count.

---

# 10. QLT-005 — Public surface is smaller than implementation

Expose only stable consumer needs.

Do not make an implementation type public merely:

- for a test;
- because another package wants convenience access;
- to avoid defining the correct contract.

Prefer behavior tests or explicit justified contracts.

---

# 11. Internal visibility

Test-specific/internal access may be acceptable when narrowly governed.

It must not become a production dependency shortcut.

---

# 12. QLT-006 — Invariants have one authoritative implementation owner

Client/API validation may improve UX.

It does not replace server/Domain/Application enforcement of authoritative rules.

Avoid duplicated formula/state-transition logic that can drift independently.

---

# 13. Validation layering

Validation can exist at multiple layers for different purposes:

```text
UI
→ immediate user feedback

API/Application
→ request/contract validation

Domain
→ business invariant
```

Duplication is acceptable only when each layer has a distinct role and the authoritative owner remains clear.

---

# 14. Failure taxonomy

Code should preserve meaningful distinctions such as:

```text
validation
authorization
not found
conflict
concurrency
entitlement/limit
transient dependency failure
provider unknown outcome
terminal provider failure
```

when callers/users need different recovery.

---

# 15. QLT-007 — Failure behavior is explicit

Do not catch broad exceptions merely to:

- return success;
- return null/default;
- hide partial failure;
- retry blindly.

Failure translation must preserve semantics.

---

# 16. Failure atomicity

Rejected mutations should not leave partial authoritative changes unless the operation explicitly defines partial success.

For critical Domain mutation rejection:

```text
state unchanged
version unchanged
events unchanged
```

is the default expectation.

---

# 17. QLT-008 — Rejected operations do not emit success evidence

A rejected operation must not create misleading:

- success event;
- audit success;
- activity success;
- usage charge;
- notification;
- provider side effect.

---

# 18. No-op quality

Semantic no-op should avoid fake mutation where the product contract treats it as unchanged.

This improves:

- event quality;
- history quality;
- concurrency;
- automation;
- audit/activity signal.

---

# 19. QLT-009 — Semantic no-op is observable as no mutation

Where appropriate:

```text
no state change
no version increment
no success event
no duplicated side effect
```

---

# 20. Dependency direction

Architecture dependency rules are executable contracts.

Code review is not the only defense.

Backend and frontend each have their own exact structural authority.

---

# 21. QLT-010 — Architecture violations fail mechanically where practical

Examples:

- forbidden project reference;
- Domain outer dependency;
- frontend forbidden package edge;
- mobile-unsafe dependency;
- generated-contract drift.

Architecture rules that matter repeatedly should not depend only on human memory.

---

# 22. Architecture exceptions

A temporary exception:

```text
is narrow
has an owner
has removal condition
has expiry/review trigger
prevents spread
```

It is not a second architecture.

---

# 23. QLT-011 — Disabling a gate is not a fix

Weakening:

- filter;
- rule;
- test;
- warning;
- architecture check;
- secret scan;
- lint coverage

to make CI green is not acceptable unless the canonical contract itself changes or an explicit exception exists.

---

# 24. Tenant isolation

Tenant correctness is a product/security invariant.

It spans:

```text
HTTP
Application
database/RLS
cache
search
events
background jobs
realtime
frontend state
analytics
integrations
```

---

# 25. QLT-012 — Cross-tenant negative evidence is mandatory for risky boundaries

When tenant scoping logic changes, include tests proving that foreign-tenant identities/resources are rejected or invisible.

Positive happy-path tests alone are insufficient.

---

# 26. Authorization

Authentication, membership, entitlement, and resource authorization remain distinct.

Quality review must detect accidental merging.

---

# 27. QLT-013 — Protected queries are tested, not only protected commands

List/search/export/realtime/subscription paths can leak data and require authorization evidence.

---

# 28. Security

Detailed secure-coding policy belongs to `security-quality-standard.md`.

This standard requires security-sensitive changes to include appropriate:

- negative tests;
- secret handling;
- threat review;
- dependency/vulnerability checks;
- authorization/tenant proof.

---

# 29. QLT-014 — Secret handling is part of correctness

A feature is not complete if it works functionally but logs/persists/exposes reusable secrets incorrectly.

---

# 30. Data consistency

Transaction boundaries, source ownership, outbox, projections, and eventual consistency follow system architecture.

Quality proof should target the actual consistency contract.

---

# 31. QLT-015 — Transaction success and side-effect success are not conflated

If provider/async work occurs after commit, tests and UI semantics must distinguish:

```text
source commit succeeded
downstream effect pending
downstream effect failed/unknown
```

---

# 32. Concurrency

Concurrency is required evidence for:

- optimistic versioned aggregates;
- hard quota;
- ordering;
- membership owner invariant;
- approval terminal state;
- idempotency;
- provider callbacks.

---

# 33. QLT-016 — Concurrency-sensitive invariants require competing-operation tests

A sequential test cannot prove behavior that fails only under stale or simultaneous operations.

---

# 34. Idempotency

Retryable operations must identify logical work.

Idempotency should be tested at the boundary where duplication can happen.

---

# 35. QLT-017 — Retry proof includes duplicate delivery

For retryable messaging/API/provider workflows, test:

```text
same logical request/event delivered twice
→ one logical business effect
```

where required.

---

# 36. Ordering

Ordering guarantees require tests for:

- duplicate;
- out-of-order;
- retry;
- failed handler;
- prefix/boundary cases;
- sequence advancement.

---

# 37. QLT-018 — Ordering state advances only after successful durable processing

A test that only verifies happy sequential order is insufficient for message-order reliability.

---

# 38. Generated artifacts

Generated outputs must have:

- one producer;
- deterministic generation;
- drift check;
- consumer compatibility.

---

# 39. QLT-019 — Generated output is never hand-maintained authority

If a generated file is wrong:

```text
fix source/producer
→ regenerate
```

Do not patch generated output directly and leave producer wrong.

---

# 40. Public contracts

API/event/realtime/generated-client changes require compatibility review.

Internal refactor is not automatically a public contract change.

---

# 41. QLT-020 — Compatibility is proven at consumer-relevant boundaries

Examples:

```text
OpenAPI drift
generated frontend contracts compile
event serialization compatibility
mobile client compatibility
provider webhook fixtures
```

---

# 42. Persistence

Database behavior that depends on PostgreSQL semantics should be tested against PostgreSQL-compatible realistic infrastructure where needed.

---

# 43. QLT-021 — A substitute database cannot prove provider-specific semantics

Do not use SQLite to “prove” PostgreSQL/RLS behavior when semantics differ.

Current backend CI explicitly guards SQLite dependencies/source usage.

---

# 44. Migrations

Schema/data migrations require:

- migration generation correctness;
- upgrade evidence;
- existing-data compatibility;
- tenant/RLS preservation;
- rollback/forward-recovery plan where risk warrants.

---

# 45. QLT-022 — Migration correctness includes old data, not only empty schema

A migration test that only creates a brand-new database may miss production upgrade failures.

---

# 46. Frontend state correctness

Frontend quality includes:

```text
server-authoritative state
query-key scope
optimistic rollback
workspace transition
realtime duplicate/out-of-order/gap
loading/permission states
host safety
```

---

# 47. QLT-023 — Client cache cannot become independent business authority

Tests should prove reconciliation to server state after:

- mutation rejection;
- stale query;
- reconnect;
- workspace switch;
- realtime gap.

---

# 48. Multi-host quality

Web, mobile, and marketing may have different implementation tests.

Shared semantics should remain coherent.

---

# 49. QLT-024 — Mobile safety is explicit

Frontend dependency architecture must protect mobile bundles from Node/web-only packages and unsupported runtime assumptions.

---

# 50. Accessibility

Detailed policy belongs to `accessibility-standard.md`.

Quality completion still requires accessibility evidence for affected user-facing interactions.

---

# 51. QLT-025 — Accessibility failure is a product regression

Keyboard/focus/semantic-role/accessible-name failures are not cosmetic defects.

---

# 52. Visual regression

Visual snapshots/screenshots can protect:

- layout;
- tokens;
- states;
- responsive composition.

They are not enough to prove behavior/accessibility/business correctness.

---

# 53. QLT-026 — Snapshot evidence is supplemental

Do not use snapshot-only testing for:

- business transition;
- authorization;
- persistence;
- idempotency;
- provider protocol.

---

# 54. Performance

Detailed performance/scalability standards belong to `performance-and-scalability.md`.

Quality review must still detect obvious architecture that contradicts known enterprise-scale constraints.

---

# 55. QLT-027 — Performance defects can be correctness defects

Examples:

- full-tenant scan per request;
- N+1 query on core list;
- unbounded retry;
- unbounded fan-out;
- synchronous provider call inside source transaction.

At scale, these can violate product availability/correctness.

---

# 56. Observability

Operationally meaningful failure paths should expose enough signal to diagnose:

- tenant-safe correlation;
- logical operation identity;
- provider/message result;
- retry state.

Observability must not leak secrets/PII unnecessarily.

---

# 57. QLT-028 — A critical failure path without diagnosable evidence is incomplete

Do not ship durable async/provider workflows that can fail invisibly.

---

# 58. Documentation quality

Canonical docs, source, tests, generated outputs, and CI must stay coherent.

Drift is classified, not silently normalized.

---

# 59. QLT-029 — Documentation is part of the change when semantics change

Update canonical owner when the change alters:

- product meaning;
- architecture;
- public contract;
- data ownership;
- quality rule;
- operational contract.

Comments alone are not sufficient.

---

# 60. Review

Review focuses on properties automated tools cannot fully prove.

Review questions should include:

```text
correct owner?
duplicated authority?
failure atomic?
tenant-safe?
authorization-safe?
concurrency-safe?
retry-safe?
compatible?
migratable?
test evidence meaningful?
```

---

# 61. QLT-030 — Review is not style policing

Formatter/linter own style mechanics.

Human review should prioritize semantics, architecture, risk, compatibility, tests, and maintainability.

---

# 62. Comments

Comments explain:

- why;
- invariant;
- compatibility constraint;
- non-obvious safety decision.

Avoid comments that merely repeat code.

---

# 63. TODO/FIXME

Architectural/non-trivial TODO should link to tracked debt/exception/migration and removal condition.

Dead commented code is deleted; Git is history.

---

# 64. QLT-031 — Hidden permanent TODO is not governance

A TODO cannot authorize architecture drift indefinitely.

---

# 65. Testability

A unit that is difficult to test may indicate hidden dependencies or mixed responsibility.

Do not introduce interfaces solely to mock everything.

---

# 66. QLT-032 — Testability serves design, not mock count

Prefer deterministic seams for:

- clock;
- random;
- external provider;
- filesystem/network;
- persistence boundary.

Do not abstract pure language/framework constructs unnecessarily.

---

# 67. Determinism

Required tests/gates should be deterministic.

Avoid:

- arbitrary sleeps;
- dependency on test execution order;
- shared global mutable state;
- uncontrolled current time/random;
- public internet.

---

# 68. QLT-033 — Flaky required test is a failing quality system

Repeated retrying of flaky tests in CI is not an acceptable permanent strategy.

Fix isolation/synchronization/root cause.

---

# 69. Required checks

A required job must:

- run when relevant;
- fail when required evidence fails;
- not pass on accidental zero work;
- be fail-closed when tooling is unavailable.

---

# 70. QLT-034 — Required suite may not pass with zero meaningful work

A successful command that selected zero tests/checks is not valid evidence for a critical/foundation gate.

Use count/assertion/equivalent proof.

---

# 71. Current backend evidence

Current backend CI has explicit jobs for:

```text
quality/security guards
architecture tests
Domain/Application/Infrastructure tests
Platform messaging tests
API/OpenAPI tests
integration/provider tests
Docker build
final backend gate
```

It verifies named critical architecture, RLS, messaging, idempotency, outbox, tenant isolation, realtime, migration, production-composition tests through TRX assertions.

---

# 72. Current frontend evidence

Current frontend CI includes:

```text
codegen drift
architecture/dependency checks
architecture docs drift
test taxonomy
typecheck
lint/format
guarded Node/Web/Integration/Mobile/Generator tests
Storybook accessibility/visual tests
web/marketing/mobile builds
production Playwright E2E
final frontend gate
```

---

# 73. QLT-035 — Current CI topology is evidence, not eternal architecture

Exact job names/scripts can evolve.

Protected quality properties remain stable.

Docs should not require retaining a job solely because its historical name exists.

---

# 74. Change-class proof

Minimum proof should follow the affected property.

Typical mapping:

| Change | Minimum proof |
|---|---|
| Domain/value semantics | Domain behavior + relevant architecture |
| Application/pipeline | Application tests + affected dependencies |
| EF/RLS/schema | Infrastructure + migration + integration/RLS |
| Messaging/idempotency/order | Platform + integration |
| API/public contract | API + OpenAPI/contract drift + consumer proof |
| Frontend package boundary | dependency-rules + typecheck/lint |
| Server state/realtime | state + transition/realtime tests |
| Mobile path | mobile tests + native-safe dependency gate |
| Generated contract | producer drift + consumer build/test |
| Security/tenant | negative authz/RLS + threat review when boundary changes |
| Destructive migration | migration + recovery evidence |
| CI/gate | self-test/non-zero proof |

---

# 75. QLT-036 — Exact revision matters

Release/freeze evidence belongs to the exact commit SHA whose required jobs passed.

A branch being green before later commits is not certification.

---

# 76. Local versus CI

Local focused testing improves speed.

Completion requires all gates implied by the change's blast radius.

CI is final protected-branch evidence.

---

# 77. QLT-037 — “Works locally” is not merge evidence

Environment/protocol/composition properties that only CI/integration proves still need their required gate.

---

# 78. Fast feedback

Prefer fast failure ordering:

```text
format/type/architecture
→ unit/behavior
→ integration
→ builds/E2E
```

when dependencies permit.

Do not sacrifice correctness merely to shorten pipeline time.

---

# 79. Cost control

Expensive tests should be:

- high-value;
- deterministic;
- correctly scoped;
- parallelized where safe.

Do not remove critical integration evidence solely because it is slower.

---

# 80. Quality debt

A failing known property must be one of:

```text
fixed
explicit active exception
tracked transition with compensating evidence
```

not silently ignored.

---

# 81. QLT-038 — Quality debt cannot become invisible baseline

A permanent skipped test or broad exclusion is not an acceptable debt ledger.

---

# 82. New quality gate admission

Add a new required gate when:

```text
property is important
violations can recur
human review alone is unreliable
check is deterministic enough
false-positive cost is acceptable
ownership is clear
```

---

# 83. Gate removal

Remove/replace gate only when:

- protected property no longer exists;
- another gate proves it more directly;
- architecture decision changes.

Document migration.

---

# 84. QLT-039 — Gate count is not quality

More CI jobs do not automatically mean stronger quality.

The objective is complete, non-overlapping, trusted evidence.

---

# 85. Security-sensitive review

Changes involving:

- auth;
- tenant;
- secrets;
- webhook;
- file upload;
- public sharing;
- billing/payment;
- provider credentials

require explicit threat/failure review beyond happy-path tests.

---

# 86. Data-destructive review

Changes involving:

- delete;
- purge;
- migration;
- cascade;
- retention;
- Account/Workspace closure

require explicit data-impact and recovery reasoning.

---

# 87. QLT-040 — Irreversible operation requires stronger proof

The harder a failure is to reverse, the stronger the migration/recovery/negative-path evidence must be.

---

# 88. Critical workflow review

Examples:

- login/session refresh;
- Workspace switch;
- membership/owner changes;
- Item mutation;
- document edit;
- automation execution;
- webhook sync;
- subscription change.

These should have layered evidence near each responsibility.

---

# 89. Generated drift

Generated artifact checks should fail if generator output differs from committed generated output.

Do not auto-commit generated changes in validation jobs.

---

# 90. Quality status reporting

A gate failure should tell the engineer:

```text
which property failed
which artifact/test failed
what expected contract was
where to investigate
```

Avoid opaque “quality failed” wrapper scripts.

---

# 91. QLT-041 — Gate output is actionable

The quality system should reduce diagnosis time rather than require reverse-engineering the gate itself.

---

# 92. Testing-library/support code

Shared test helpers are infrastructure for tests.

They should:

- reduce noise;
- preserve explicit setup;
- avoid hiding critical semantics.

---

# 93. QLT-042 — Test helpers do not conceal the property under test

A helper that secretly performs authorization, tenant setup, retries, or assertions can make tests misleading.

Keep critical facts visible.

---

# 94. Test data

Test data should be:

- minimal for the scenario;
- explicit;
- tenant-scoped;
- deterministic.

Massive generic fixtures often hide invariants.

---

# 95. QLT-043 — Test isolation is an invariant

Parallel or repeated execution must not rely on accidental shared state.

---

# 96. Provider tests

Mock/provider stubs are useful for local error modeling.

Real protocol/container/fixture evidence is required where provider semantics matter.

---

# 97. QLT-044 — Mock fidelity must match the property being proven

Do not use a permissive mock to prove:

- SQL/RLS;
- provider signature verification;
- transaction behavior;
- serialization compatibility.

---

# 98. E2E scope

E2E is selected for critical composed journeys, not every branch.

It proves composition across boundaries.

---

# 99. QLT-045 — E2E cannot compensate for missing lower-level ownership tests

A slow journey test should not be the only proof of a Domain invariant or architecture boundary.

---

# 100. Coverage

Line/branch coverage can identify blind spots.

It is not a quality target by itself.

---

# 101. QLT-046 — Scenario completeness outranks coverage percentage

A critical invariant with one missing rejection scenario is not “covered” because line coverage is high.

---

# 102. Mutation/property testing

Use stronger techniques where risk justifies:

- property tests for value objects/order algorithms;
- fuzzing/parsing for untrusted input;
- mutation testing for critical pure rule sets.

They are tools, not universal mandates.

---

# 103. Quality ownership routes

Repository-wide quality:

```text
docs/quality/engineering-quality-standard.md
docs/quality/testing-strategy.md
docs/quality/security-quality-standard.md
docs/quality/accessibility-standard.md
docs/quality/performance-and-scalability.md
```

Technology-specific proof routes into backend/frontend docs.

---

# 104. Final completion checklist

Before calling a change complete:

```text
[ ] semantic owner is correct
[ ] authoritative invariant is enforced
[ ] dependency direction is valid
[ ] failure/no-op behavior is intentional
[ ] tenant/authz negative paths are covered where relevant
[ ] concurrency/retry behavior is covered where relevant
[ ] public/generated contracts are synchronized
[ ] migrations are proven where needed
[ ] frontend state/realtime recovers where needed
[ ] accessibility/performance/security evidence is sufficient
[ ] required tests execute non-zero work
[ ] docs reflect semantic/architecture changes
[ ] exact CI revision is green
```

---

# 105. Stop conditions

Stop rather than merge when:

- required critical gate selects zero work;
- architecture failure is suppressed instead of fixed;
- semantic owner is unresolved;
- security/tenant boundary changed without negative proof;
- retryable side effect lacks idempotency/reconciliation;
- migration is tested only on empty schema when existing data matters;
- frontend optimistic/realtime state can remain incorrect after rejection/gap;
- generated source and committed artifact drift;
- current CI green depends on broad test exclusion;
- source/docs contract conflict remains unclassified.

---

# 106. Final quality rule

Notrelix quality is sufficient when an engineer can answer:

```text
What contract changed?
Which owner implements it?
Which failure modes were considered?
Which executable evidence proves it?
Which architecture/security/tenant properties remain intact?
Which compatibility/migration concerns were handled?
Did required gates execute real work on this exact revision?
```

The target is:

> **quality as trusted executable evidence, not ceremony, test-count inflation, or a green pipeline that can pass while the protected property is broken.**

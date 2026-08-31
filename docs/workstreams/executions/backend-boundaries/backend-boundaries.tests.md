---
document_id: WRK-TESTS-BACKEND-BOUNDARIES-V3
document_type: execution-tests
status: draft
owner: backend-architecture
version: 3
audit_snapshot:
  repository: Nqv1208/Notrelix
  branch: main
  commit: 6030d06051e8bbb4844746150be5c1d5d4c53bbd
depends_on:
  - WRK-SPEC-BACKEND-BOUNDARIES-V3
  - WRK-PLAN-BACKEND-BOUNDARIES-V3
higher_authorities:
  - RULE.md
  - AGENTS.md
  - backend/AGENTS.md
  - backend/docs/architecture/testing-and-quality-gates.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/platform-and-messaging.md
  - backend/docs/architecture/security-tenancy-authorization.md
applies_to:
  - backend/tests/Notrelix.Domain.Tests
  - backend/tests/Notrelix.Application.Tests
  - backend/tests/Notrelix.Infrastructure.Tests
  - backend/tests/Notrelix.Integration.Tests
  - backend/tests/Notrelix.API.Tests
  - backend/tests/Notrelix.Architecture.Tests
  - cross-context contract verification
  - architecture fitness functions
  - future service-runtime verification
---

# TESTS — Backend Boundary Execution V3

## 1. Purpose

This document defines the evidence required to prove `SPEC V3` and `PLAN V3`.

It does not redefine architecture.

The testing objective is not:

```text
feature happy path passes
```

It is:

```text
business behavior is correct
+
bounded-context ownership is preserved
+
cross-context interaction is explicit
+
frozen request pipeline remains authoritative
+
data ownership is enforceable
+
legacy debt cannot silently grow
+
runtime topology can change without rewriting Application semantics
```

Tests must protect semantic behavior first and implementation mechanism second.

---

## 2. Audited test baseline

Current architecture test project already contains dedicated areas:

```text
ApplicationLayer
Authorization
Baselines
Contracts
DataAccess
DomainPurity
EndpointContracts
Events
Freeze
InfrastructureLayer
LayerRules
Performance
```

This is sufficient structure.

Do not create a new top-level:

```text
Boundaries/
```

folder merely to group this execution work.

Place new tests in the existing area that owns the concern.

---

## 3. Audited architecture-test capabilities

The architecture test project already references:

```text
Notrelix.Domain
Notrelix.Application
Notrelix.Infrastructure
Notrelix.API
```

and includes:

```text
xUnit
FluentAssertions
Moq
Microsoft.CodeAnalysis.CSharp
```

Therefore available enforcement mechanisms include:

```text
reflection
filesystem/source scan
compiled type graph
Roslyn syntax/semantic analysis
```

Use the least complex mechanism that produces reliable evidence.

---

## 4. Audited existing boundary enforcement

Current:

```text
DataAccess/DbContextBoundaryArchitectureTests.cs
```

already:

```text
finds Application Feature handler files
maps feature root to expected context DbContext
rejects known foreign context DbContext names
supports KnownCrossModuleViolations
```

This is partial implementation of:

```text
ARCH-BC-001
```

Do not delete or replace it without preserving its current coverage.

---

## 5. Audited Domain reference enforcement

Current `DomainPurity` already contains:

```text
CrossContextReferenceTests.cs
DomainReferenceGraph.cs
DomainReferenceGraphTests.cs
DomainBoundedContextSignatureTests.cs
...
```

`CrossContextReferenceTests` already maps context namespace prefixes and detects some cross-context entity/aggregate references.

This is the foundation for:

```text
ARCH-BC-002
```

Do not build a duplicate Domain graph scanner.

Extend existing graph/test logic where required.

---

## 6. Audited pipeline boundary enforcement

Current `ApplicationLayer` includes:

```text
HandlerConstructorPortGate.cs
HandlerConstructorPortGateTests.cs
HandlerDataPortGateTests.cs
```

`HandlerConstructorPortGate` already enforces an important frozen-pipeline rule:

```text
handlers must not inject forbidden authorization decision ports
because permission decisions belong to pipeline markers/behaviors
```

Boundary tests MUST preserve this principle.

Do not write a boundary test that accidentally requires every protected handler to inject an authorization port.

---

## 7. Audited event enforcement

Current `Events` contains:

```text
ContractRegistryCompletenessTests.cs
PublicEventContractArchitectureTests.cs
PlanTraceabilityMatrixArchitectureTests.cs
```

This is the extension point for:

```text
ARCH-BC-007
```

Do not create a second event registry or second public-event discovery mechanism.

---

## 8. Audited baseline mechanism

Current architecture baselines include:

```text
application-legacy-ef-usage.approved.txt
async-cache-baseline.json
request-execution-baseline.json
```

The repo therefore already accepts explicit baseline files as an architecture-hardening mechanism.

Boundary baseline design SHOULD reuse this convention when a concrete machine-readable baseline is necessary.

Do not introduce a separate custom baseline framework.

---

## 9. Test-layer model

Use:

```text
Domain Tests
    ↓
Application Tests
    ↓
Infrastructure/Adapter Tests
    ↓
Integration Tests
    ↓
API Tests where transport affected

Architecture Tests run orthogonally.
```

Only after real extraction:

```text
Transport Contract Tests
Service Integration Tests
Mixed-Version Tests
Network Failure Tests
Writer-Cutover Tests
```

Current monolith MUST NOT be burdened with fake network testing.

---

## 10. Test ownership matrix

| Concern | Primary test project |
|---|---|
| aggregate invariant | Domain.Tests |
| use-case behavior | Application.Tests |
| pure ACL translation | Application.Tests |
| Infrastructure adapter mapping | Infrastructure.Tests |
| DbContext/transaction/outbox integration | Integration.Tests |
| HTTP binding/result/OpenAPI | API.Tests / EndpointContracts |
| structural boundaries | Architecture.Tests |
| public event structure | Architecture.Tests/Events |
| distributed runtime | future service/integration test suite |

Do not prove business rules only through architecture tests.

Do not prove architecture only through unit mocks.

---

## 11. Naming convention for boundary architecture tests

Use existing naming style where possible.

Recommended:

```text
CrossContextPersistenceBoundaryTests
CrossContextApplicationDependencyTests
PublicSemanticContractArchitectureTests
ApplicationTransportBoundaryTests
```

Do not force `ARCH-BC-001` into the C# class name.

Rule ID should appear in:

```text
test comments
failure message
execution traceability
```

where useful.

---

## 12. Context identity model for tests

Context resolution MUST be deterministic.

Canonical business context IDs:

```text
Accounts
Identity
Workspaces
Governance
WorkManagement
Documents
Collaboration
Automation
Integrations
Billing
Analytics
```

Support capabilities such as:

```text
Search
Operations
Notifications
```

MUST NOT automatically be added as business contexts merely because Application Feature folders exist.

Their ownership classification comes from canonical architecture.

---

## 13. Application context resolution

For Application feature code:

```text
Notrelix.Application.Features.{Context}.*
```

or path:

```text
Features/{Context}/...
```

is the primary context resolution mechanism for known business contexts.

Do not derive context from module names below the first Feature segment.

Example:

```text
Features/WorkManagement/Boards/...
→ WorkManagement
```

---

## 14. Domain context resolution

For Domain:

```text
Notrelix.Domain.{Context}.*
```

is primary.

Existing Domain tests may have historically recognized extra prefixes such as:

```text
Notrelix.Domain.Teams
```

Do not automatically interpret such historical namespace as a new BC.

When existing source uses a namespace whose semantic owner differs from namespace spelling:

```text
explicit ownership mapping
```

must be used.

Do not silently create architecture authority from source naming.

---

## 15. Context map source

Preferred progression:

```text
1. small test-local mapping using canonical business context IDs
2. reuse existing DomainReferenceGraph/context mapping where possible
3. extract shared architecture-test helper only when duplication becomes material
4. machine-readable catalog only under ARCH-BC-009 admission
```

Do not start with a repository-wide YAML/TOML manifest.

---

## 16. Boundary test quality requirements

Every architecture test must be:

```text
deterministic
fast
CI-friendly
stable across machines
actionable
narrow enough to explain failure
```

Avoid:

```text
network access
random ordering
clock-dependent assertions
large generated snapshots without reason
regex that matches comments only
```

---

## 17. Failure message standard

Failure message must identify:

```text
RuleId
source context
target/producer context
source type/file
forbidden dependency
approved migration direction
```

Example:

```text
ARCH-BC-003 violation:
Automation type ExecuteActionHandler references
WorkManagement internal UpdateBoardItemCommand.

Cross-context consumers must use Automation-owned IWorkActionPort
and a WorkManagement Public action contract.
```

---

## 18. False-positive policy

When a gate flags legitimate code:

Do not immediately whitelist by filename.

First classify:

```text
test bug
ownership map bug
approved technical shared primitive
approved reviewed exception
actual architecture violation
```

Only approved exception may enter baseline/allowlist.

---

## 19. Exception precision

Avoid:

```text
HashSet<string> ignoredFiles = ["WorkManagement"]
```

Prefer:

```text
fully-qualified type
+
target dependency
+
Rule ID
```

or exact deterministic signature.

Example:

```text
Notrelix.Application.Features.X.Y.SomeHandler
→ Notrelix.Application.Features.Z.Public.SomeTemporaryContract
```

with an explicit reviewed reason.

---

## 20. Baseline growth rule

CI must ensure:

```text
existing baseline may shrink
existing baseline may remain temporarily
new baseline entry requires deliberate reviewed change
```

Never silently auto-update approved baseline during ordinary test run.

---

## 21. Baseline format for new boundary violations

If baseline becomes necessary, recommended JSON:

```json
{
  "ruleId": "ARCH-BC-003",
  "violations": [
    {
      "consumerContext": "Automation",
      "producerContext": "WorkManagement",
      "sourceType": "Notrelix.Application.Features.Automation....",
      "targetType": "Notrelix.Application.Features.WorkManagement....",
      "owner": "automation-integrations-team",
      "risk": "R1",
      "migrationTrigger": "next material edit"
    }
  ]
}
```

Use existing baseline conventions if one is already more appropriate.

Do not duplicate the same violation in multiple baseline files.

---

## 22. BND-M0 test requirements — inventory

`BND-M0` is primarily discovery.

Required proof is not a new production test suite.

Run existing:

```text
Architecture.Tests
Application.Tests
relevant Integration.Tests
```

to establish baseline.

Record current failing/approved conditions.

No test should be changed merely to make BND-M0 "have code".

---

## 23. BND-M1 test requirements — debt baseline

For each proposed baseline item prove:

```text
the scanner detects the violation before baseline filtering
the baseline suppresses exactly that violation
a second synthetic/new violation would still fail
```

Where practical, helper tests should ensure:

```text
baseline cannot become wildcard
duplicate entries rejected
unknown Rule ID rejected
```

if a generic baseline loader is introduced.

Do not create such loader unless more than one gate needs it.

---

## 24. BND-M2 / ARCH-BC-001 — Foreign Persistence Dependency

### Rule

Consumer Application code must not depend on producer persistence abstraction.

Primary examples:

```text
WorkManagement → IWorkspaceDbContext
Automation → IWorkManagementDbContext
Collaboration → IDocumentDbContext
```

---

## 25. ARCH-BC-001 current test extension strategy

Current file:

```text
DataAccess/DbContextBoundaryArchitectureTests.cs
```

already checks Handler files.

First enhancement should preserve current logic while addressing scope gaps.

Candidate expansion:

```text
Handlers
Application Services
Process Managers
Event handlers
Projection handlers
```

Do not scan every `.cs` file blindly if that creates false positives from:

```text
contract declarations
test-only strings
comments
namespace aliases
```

Prefer type-level/reflection or Roslyn once handler-file source scanning stops being precise.

---

## 26. ARCH-BC-001 exact assertions

Test cases:

```text
WorkManagement handler + IWorkManagementDbContext
→ PASS

WorkManagement handler + IWorkspaceDbContext
→ FAIL

Automation process + IAutomationDbContext
→ PASS

Automation process + IWorkManagementDbContext
→ FAIL
```

Infrastructure is allowed to implement multiple context interfaces.

This rule targets consumer Application ownership.

---

## 27. ARCH-BC-001 constructor vs hidden static/service-locator dependency

Constructor scan alone is insufficient long term.

Test should eventually catch:

```text
constructor parameter
primary constructor parameter
field/property injected dependency
service-provider resolution
direct static access if architecture permits detection
```

Prioritize constructor/DI dependency first.

Do not overbuild static-analysis coverage before concrete violations exist.

---

## 28. ARCH-BC-001 baseline rule

Current:

```text
KnownCrossModuleViolations = []
```

is ideal.

Keep it empty if possible.

If real legacy violations are discovered:

```text
replace filename-only whitelist
```

with a more precise baseline signature.

Do not add broad filename skip because renamed files can evade governance.

---

## 29. ARCH-BC-002 — Foreign Domain Model Dependency

### Rule

Another BC must not depend on producer Domain model as integration API.

Forbidden:

```text
aggregate
entity
producer-internal value object
producer Domain enum
Domain service
```

Allowed:

```text
Producer.Public Fact
stable ID
ResourceRef
approved shared technical primitive
```

---

## 30. ARCH-BC-002 audited extension point

Use:

```text
DomainPurity/CrossContextReferenceTests.cs
DomainReferenceGraph.cs
```

as foundation.

Current test already rejects some cross-context concrete entity/aggregate references.

Extend rather than replace.

---

## 31. ARCH-BC-002 gap: Application references to foreign Domain

Existing Domain graph primarily protects Domain→Domain references.

Boundary execution also needs:

```text
Application.Features.{Consumer}
→ Notrelix.Domain.{Producer}
```

detection.

Recommended implementation location:

```text
ApplicationLayer/
CrossContextApplicationDependencyTests.cs
```

or a focused companion in `DomainPurity` if reuse of graph helpers makes that materially cleaner.

---

## 32. ARCH-BC-002 source/type detection strategy

Preferred:

```text
Roslyn or reflection/type metadata
```

because simple source `Contains("Notrelix.Domain.WorkManagement")` can be fooled by:

```text
comments
aliases
fully-qualified test strings
```

Architecture test project already has Roslyn package.

Still use Roslyn only for the cases where reflection cannot reliably identify source context/foreign target.

---

## 33. ARCH-BC-002 generic/nested types

Detection must inspect referenced type graphs including:

```text
constructor params
method params
return types
properties
fields
generic arguments
base types/interfaces where relevant
```

Example forbidden:

```text
Task<WorkManagement.Board>
IReadOnlyList<Documents.Document>
```

Do not only inspect direct non-generic type.

---

## 34. ARCH-BC-002 value-object policy

Do not globally allow every `struct`, `record struct`, or enum.

Rule:

```text
producer Domain-owned semantic type
```

is foreign unless explicitly shared/public.

Current Domain test has historical primitive/value-type allowances; new Application-level enforcement must not inherit them blindly.

Stable shared primitives are explicit exceptions.

---

## 35. ARCH-BC-003 — Producer Internal / MediatR Dependency

### Rule

Consumer must not directly reference producer internal Application implementation.

High-value forbidden cases:

```text
consumer imports producer Commands/* internal request
consumer imports producer Queries/* internal request
consumer calls IMediator.Send(producer internal request)
consumer calls producer handler directly
```

Allowed:

```text
Producer.Public
Consumer Port
```

---

## 36. ARCH-BC-003 placement

Recommended:

```text
ApplicationLayer/
CrossContextApplicationDependencyTests.cs
```

One class may cover:

```text
ARCH-BC-002
ARCH-BC-003
```

only if failure output remains clearly separated by Rule ID.

Do not create one huge `BoundaryTests.cs` with unrelated rules.

---

## 37. ARCH-BC-003 Public detection

Cross-context reference into:

```text
Notrelix.Application.Features.{Producer}.Public.*
```

may be allowed.

Reference into:

```text
Notrelix.Application.Features.{Producer}.{Module}.Commands.*
Notrelix.Application.Features.{Producer}.{Module}.Queries.*
Notrelix.Application.Features.{Producer}.Abstractions.*
```

is forbidden by default.

Do not decide based on CLR `public` modifier.

Architecture Public is namespace/surface authority.

---

## 38. ARCH-BC-003 MediatR syntax detection

When type-reference analysis already catches producer internal command type:

```text
additional IMediator-specific analyzer may not be needed
```

If a pattern evades reflection because of source/generated indirection:

```text
Roslyn invocation analysis
```

may inspect:

```text
mediator.Send(...)
sender.Send(...)
```

and resolve request type.

Do not ban MediatR globally.

---

## 39. ARCH-BC-004 — Cross-context EF Navigation / Cascade

### Rule

Forbidden semantic coupling:

```text
cross-BC ORM navigation
cross-BC cascade delete
```

Physical FK may remain as reviewed integrity debt.

---

## 40. ARCH-BC-004 test strategy

This rule spans:

```text
Domain model properties
Infrastructure EF configuration
```

Use multiple focused tests rather than one brittle scanner.

#### Domain navigation test

Using Domain graph:

```text
entity/aggregate navigation to another BC
→ FAIL
```

#### EF cascade test

Inspect Infrastructure configuration/source for known cross-context relationship endpoints.

Potential methods:

```text
model metadata generated by ApplicationDbContext
configuration reflection
focused source/Roslyn analysis
```

Choose whichever current test infrastructure supports most reliably.

---

## 41. ARCH-BC-004 false-positive guard

Do not treat:

```text
Guid WorkspaceId
Guid AccountId
ResourceRef
```

as navigation.

Do not reject same-context relationships.

Do not assume every FK constraint in DB is semantic violation.

Cascade/navigation are the primary prohibited behavior.

---

## 42. ARCH-BC-005 — Public Semantic Contract Purity

### Rule

Types in:

```text
Application/Features/{Producer}/Public/**
```

must remain semantic/transport neutral.

Reject references to:

```text
DbContext
EF Core
Infrastructure namespaces
provider SDK
generated gRPC types
ASP.NET types
producer Domain aggregate/entity
internal handler
internal request
```

---

## 43. ARCH-BC-005 test placement

Recommended:

```text
Contracts/PublicSemanticContractArchitectureTests.cs
```

Existing `Contracts/` currently has Admin-specific contract tests; adding a focused public semantic contract test is consistent.

Alternative:

```text
ApplicationLayer/
```

only if Contract folder conventions make it unsuitable.

Prefer Contracts.

---

## 44. ARCH-BC-005 discovery

Discover only actual:

```text
Features/*/Public/**
```

types.

No test should require every Feature context to have Public.

Zero Public surfaces is valid for a context with no consumers.

---

## 45. ARCH-BC-005 allowed references

Possible allowed roots:

```text
System.*
Application.Common technical primitives approved by canonical architecture
shared ResourceRef if canonical
stable Result/error primitives if contract policy allows
other Public semantic contracts only when explicitly justified
```

Be conservative about Public→Public chains across producers.

A Public contract should usually expose producer semantics directly.

---

## 46. ARCH-BC-006 — Application Transport / Provider Purity

### Rule

Domain/Application business code must not directly depend on runtime/network/provider clients.

Flag:

```text
HttpClient
HttpRequestMessage
GrpcChannel
generated gRPC client
provider SDK client types
broker producer types
Redis/Npgsql concrete client when used directly as boundary mechanism
```

---

## 47. ARCH-BC-006 placement

Recommended:

```text
ApplicationLayer/ApplicationTransportBoundaryTests.cs
```

Domain transport/framework purity may continue under:

```text
DomainPurity/DomainFrameworkDependencyTests.cs
```

Do not duplicate Domain framework tests.

---

## 48. ARCH-BC-006 detection strategy

Start with:

```text
assembly/type references
known forbidden namespace/type roots
```

Then Roslyn for:

```text
new HttpClient(...)
IHttpClientFactory usage directly in Application
generated client field declarations
```

if reflection coverage is insufficient.

Do not flag:

```text
Application-defined semantic port
```

whose Infrastructure implementation later uses HttpClient.

---

## 49. ARCH-BC-006 compatibility exception

Canonical Application architecture may allow narrowly documented EF compatibility in existing Application code.

Do not turn ARCH-BC-006 into a generic "Application references zero Infrastructure packages" test if that conflicts with frozen architecture.

Boundary test targets:

```text
transport/provider mechanism in business/cross-context code
```

not unrelated canonical exceptions.

---

## 50. ARCH-BC-007 — Integration Event Ownership / Version

### Rule

Touched/new outward event must:

```text
have producer ownership
have supported version identity
represent producer-completed fact
participate in canonical registry/discovery
```

---

## 51. ARCH-BC-007 extension strategy

Extend:

```text
Events/PublicEventContractArchitectureTests.cs
Events/ContractRegistryCompletenessTests.cs
```

Do not create:

```text
BoundaryEventRegistry
```

parallel to existing registry.

---

## 52. ARCH-BC-007 structural assertions

Machine-testable:

```text
registered outward event
version follows canonical event contract convention
contract is immutable/serializable as required
registry completeness
event type ownership can be mapped
```

Potentially review-only:

```text
is name/meaning producer fact vs consumer instruction?
```

Do not pretend semantic intent can be perfectly inferred from class name.

Use structural smell list + certification review.

---

## 53. ARCH-BC-008 — Common Semantic Leakage

### Rule

New context-owned business vocabulary must not migrate into `Application.Common` by convenience.

This is partly semantic.

---

## 54. ARCH-BC-008 test strategy

Use layered enforcement:

#### Machine-detectable known types

Flag additions matching explicit owner-sensitive concepts:

```text
PlanTier
SubscriptionTier
WorkspaceRole
context-specific permission enum
context-specific FeatureCode enum
```

#### Diff/review gate

New types under:

```text
Application/Common
Domain/Common
SharedKernel
```

must be reviewed for ownership.

#### Existing hotspot baseline

`Common/Entitlements` is known migration debt.

Do not make current branch fail immediately without baseline.

---

## 55. ARCH-BC-008 anti-regression

The most important initial rule:

```text
no new consumer plan/tier semantics added to Common/Entitlements
```

and:

```text
no new business-wide enum introduced without owner review
```

This may be enforced with targeted source/type assertions plus review.

---

## 56. ARCH-BC-009 — Optional Boundary Catalog

Do not implement initially.

Admission only if:

```text
context mapping duplicated across >= several gates
or
existing mappings drift
or
baseline/exception handling becomes inconsistent
```

If admitted, add tests proving catalog:

```text
contains canonical context IDs
has unique roots
does not encode future service topology
does not encode transport
does not encode roadmap priority
```

---

## 57. Frozen request pipeline tests

Boundary execution MUST preserve frozen request-pipeline authority.

For protected request such as `CreateBoardInWorkspaceCommand`, architecture/application tests should prove:

```text
request declares expected marker/descriptor semantics
handler does not inject forbidden authorization port
handler does not manually query Governance
handler does not duplicate workspace scope resolution
```

Use existing:

```text
HandlerConstructorPortGate
request execution baseline/tests
Authorization architecture tests
```

where possible.

---

## 58. Pipeline-vs-port anti-regression test

High-value test concept:

For requests that implement:

```text
IRequirePermission
```

their handlers should not inject a known direct authorization-decision port that bypasses/repeats frozen pipeline.

Existing `HandlerConstructorPortGate` already implements this style.

Extend forbidden-port set only when a real new port would duplicate pipeline.

Do not generically forbid all `*AuthorizationPort`.

Some non-standard workflow may legitimately need a consumer semantic port outside ordinary request pipeline.

---

## 59. CreateBoardInWorkspace Application test matrix

Current slice should prove existing behavior without forcing artificial ports.

#### Request contract

```text
IWriteRequest
IRequirePermission
IAuthenticatedRequest
IWorkspaceRequest
IIdempotentRequest
```

as appropriate to current canonical source.

#### Handler behavior

```text
creates Board
creates default BoardFields
uses request Account/Workspace execution context
mutates only WorkManagement persistence
enrolls existing realtime/integration output correctly
```

#### Boundary behavior

```text
no Workspace DbContext
no Governance DbContext
no Billing DbContext
no foreign Domain
no foreign internal request
```

---

## 60. CreateBoard permission tests

Authorization pipeline tests own:

```text
unauthenticated
wrong workspace
permission denied
resource not found semantics
allowed
```

Do not duplicate these as handler mocks if pipeline already owns them.

Handler unit test may assume an authorized resolved context.

---

## 61. CreateBoard Workspace-fact extension test

Only if implementation adds a real Workspace Public fact beyond pipeline:

Test:

```text
workspace active → continue
workspace archived/inactive → reject according to semantic contract
not found → correct business failure
technical dependency unavailable → technical dependency policy
```

If no new Workspace fact is needed:

```text
no such test should be added
```

---

## 62. CreateBoard Billing-capability test

Only if product capability is actually added.

#### Pipeline-owned gate

Test:

```text
request capability descriptor/marker
access facts include producer-owned grant
policy allowed/denied
handler not invoked when denied
```

#### Handler/use-case-specific port

Test:

```text
consumer Port allowed/denied
ACL mapping
technical dependency policy
```

Do not test both architectures simultaneously for same semantic.

---

## 63. Automation → Work Application test matrix

Consumer Automation tests:

```text
action selected
IWorkActionPort invoked with stable semantic input
Work acceptance → execution success state
Work semantic rejection → correct Automation outcome
unknown technical outcome → retry/pending/failure policy
duplicate Automation execution → no duplicate semantic action where required
```

Do not mock:

```text
IWorkManagementDbContext
Work Board aggregate
```

---

## 64. Work Public action tests

Producer Work tests:

```text
valid action accepted
invalid Work transition rejected
missing resource
scope/authorization as required
idempotency key behavior if exposed
owned transaction behavior
Work integration event behavior
```

These are Work-owned tests.

Automation does not test Work invariants by reconstructing them.

---

## 65. Work Public facade delegation test

If Public implementation delegates to internal MediatR:

```text
Public action
→ internal owned request
```

may be tested as producer integration/application behavior.

Architecture test must ensure:

```text
only WorkManagement code references that internal request
```

not external consumers.

---

## 66. In-process adapter tests

For:

```text
Infrastructure/CrossContext/{Consumer}/{Producer}
```

test:

```text
consumer semantic request mapping
producer public call
producer result mapping
scope/correlation forwarding if present
technical failure mapping
```

Do not test producer business rule inside adapter test.

---

## 67. ACL tests

ACL is pure Application code.

Table-driven tests should cover all producer result variants.

Example:

```text
Billing Allowed
→ Work Allowed

Billing LimitReached
→ Work CapabilityDenied(limit)

Billing SubscriptionInactive
→ Work CapabilityDenied(subscription)

unknown/additive producer status
→ deterministic safe mapping
```

The ACL test must not query DB/network.

---

## 68. Business failure vs technical failure tests

Keep two test families.

#### Semantic/business

```text
NotFound
Inactive
Denied
LimitExceeded
InvalidState
```

#### Technical dependency

```text
timeout
unavailable
protocol failure
```

Application result should not contain raw:

```text
RpcException
HttpRequestException
provider SDK error
```

---

## 69. Technical failure mapping tests

Current in-process adapter may not naturally produce network failures.

Do not fake gRPC merely to test future.

Test current technical failures that actually exist.

After remote adapter exists, test:

```text
transport timeout
unavailable
auth failure
contract error
```

mapping.

---

## 70. Transaction boundary tests

For one owned mutation:

```text
owned state writes
outbox/event enrollment
commit
rollback on local failure
```

Cross-BC test must not assert:

```text
consumer state + producer state rollback together
```

unless explicitly accepted architecture exception exists.

---

## 71. Unknown-outcome tests for future remote command

Once remote target-owned command exists:

```text
producer commit succeeds
response is lost
consumer sees timeout
consumer retries same operation ID
producer does not duplicate mutation
```

This test is mandatory before enabling automatic retry for mutation.

---

## 72. ResourceRef tests

Test:

```text
kind preserved
id preserved
scope validated
unsupported kind behavior
missing target behavior
deleted target behavior
authorization not implied by reference
```

No EF navigation is necessary to construct target.

---

## 73. Cross-context lifecycle tests

Where cross-context cascade was removed:

Test explicit behavior.

Example:

```text
Board archived/deleted
→ Collaboration comments behavior is explicit
```

Possible semantics:

```text
retain comments
hide comments
mark target unavailable
event-driven cleanup
```

Do not let DB cascade decide product behavior invisibly.

---

## 74. Integration Event producer tests

For touched event:

```text
Domain mutation
→ Domain Event where applicable
→ Integration Event mapping
→ canonical registry/outbox
→ local commit
```

Do not assert specific consumer execution.

---

## 75. Integration Event consumer tests

Test:

```text
first delivery
duplicate delivery
invalid/unsupported version if applicable
consumer local failure
retry
poison path if canonical
```

Consumer business idempotency is consumer-owned.

Platform generic retry mechanism remains Platform-tested.

---

## 76. Event semantic smell review

Machine tests can flag names like:

```text
Run*
Refresh*
Reindex*
Trigger*
```

for review.

But such name MUST NOT automatically fail without semantic evidence.

Use certification review:

```text
Does producer own this action?
or
is this really a completed fact?
```

---

## 77. Projection tests

Application projection model:

```text
event/fact input
→ deterministic derived state
```

Test:

```text
first application
duplicate
revision order
older revision
rebuild
scope
staleness
```

Infrastructure projection store tests:

```text
persistence
concurrency
indexing/lookup
```

---

## 78. Security projection tests

Additional:

```text
revocation
stale permission
wrong tenant
wrong resource
fail closed
invalid revision
```

A stale security projection that grants access without accepted design is a blocker.

---

## 79. Process Manager tests

Use deterministic state-machine tests.

Required:

```text
start
participant success
participant retryable failure
participant terminal failure
duplicate outcome
out-of-order outcome
timeout
compensation
restart/resume
terminal success
terminal failure
```

Also prove:

```text
participant local mutations remain participant-owned
```

---

## 80. Common/Entitlements migration tests

Before migrating:

Create consumer inventory evidence.

For one touched consumer:

#### Pipeline gate path

Test:

```text
capability declaration
access-fact resolution
policy allow/deny
handler not called on deny
```

#### Use-case port path

Test:

```text
Port semantic result
ACL
handler behavior
```

#### Billing mutation path

Test target-owned Billing usage semantics separately.

Do not keep old and new semantic path both active without compatibility rationale.

---

## 81. Entitlement anti-regression architecture test

High-value checks:

```text
new product context source must not call HasSubscriptionTierAsync
new product context source must not compare hard-coded "Pro"/"Enterprise"
```

Initial enforcement may target:

```text
new/touched WorkManagement
Documents
Automation
Integrations
Collaboration
```

Do not globally fail untouched legacy baseline until inventory/migration is established.

---

## 82. FeatureCode test policy

If `FeatureCode` remains:

Determine whether it is:

```text
stable owner-neutral capability key
or
global enum leaking context semantics
```

Machine test should not assume either without classification.

Certification must record owner.

If new values are added:

```text
review semantic owner
consumer scope
pipeline need
```

---

## 83. API composition tests

For read composition:

```text
all sources authorized
correct partial failure behavior
tenant isolation
response contract
```

For mutation endpoint:

Test that API delegates to:

```text
one owning Application use case
or
explicit Process Manager entry
```

not direct multi-context orchestration.

---

## 84. API contract purity

API `Contracts/` are HTTP transport contracts.

They are allowed to differ from:

```text
Application Producer.Public semantic contracts
```

Tests should ensure API DTOs do not become required Application business models.

Application must not depend on `Notrelix.API.Contracts`.

---

## 85. Infrastructure adapter dependency-direction test

Potential architecture test:

```text
Infrastructure CrossContext adapter
→ Application semantic contracts
```

allowed.

Forbidden:

```text
Application
→ Infrastructure CrossContext adapter
```

Existing project references likely already enforce this assembly-level direction.

Do not duplicate if LayerRules already proves it.

---

## 86. DI composition tests

When adding a Consumer Port adapter:

Test DI can resolve:

```text
Consumer Port
→ expected in-process adapter
```

under normal current configuration.

If multiple modes exist:

```text
Database
DevNull
Testing
```

test each supported mode.

Do not make Application unit tests depend on DI container.

---

## 87. DevNull/test implementation tests

Current Billing supports development/testing bypass implementations.

Boundary migration must ensure:

```text
DevNull cannot run in production
semantic contract remains consistent
new Consumer Port has appropriate testing implementation or adapter path
```

Do not accidentally bypass capability enforcement in production through migration.

---

## 88. Architecture-test source root resolution

Current tests often discover backend root by walking to:

```text
backend.slnx
```

New source-scan tests should reuse/extract a shared root helper only when at least several tests duplicate logic materially.

Do not create a generic filesystem framework prematurely.

---

## 89. File scanning rules

When source scanning:

Ignore:

```text
bin
obj
generated artifacts where appropriate
comments when testing symbols by text
test snapshots
```

Normalize path separators.

Use ordinal comparisons where path/namespace IDs are canonical.

---

## 90. Comment stripping caution

Existing DbContext test strips comments.

For new rules:

```text
prefer syntax tree
```

if text matching requires increasingly complex comment/string filtering.

Do not keep stacking regex until test becomes unreliable.

---

## 91. Roslyn admission rule

Use Roslyn when test needs to answer semantic questions such as:

```text
what TypeSymbol does this reference resolve to?
which namespace owns this constructor parameter?
what request type is passed to mediator.Send?
is a generated client field actually used by Application type?
```

Do not use Roslyn for simple directory existence or project-reference checks.

---

## 92. Roslyn compilation source

When semantic model is required:

Prefer compiling/parsing using project source/reference information available in test environment.

Do not shell out to external compiler unless existing test infrastructure already does so.

Keep test deterministic.

---

## 93. Architecture-test performance budget

Wave-1 architecture tests should remain suitable for every backend PR.

Targets:

```text
single-digit seconds where practical
no external process startup loops
no database
no network
```

If Roslyn compilation becomes expensive:

```text
cache compilation per test fixture
share immutable model
or narrow scanned source
```

---

## 94. BND-M4 test traceability

CreateBoard reference evidence should map:

```text
BOUND-OWN-002
BOUND-APP-002
BOUND-DATA-002
BOUND-MEDIATOR-001
BOUND-AUTH-*
BOUND-API-*
```

to concrete tests.

Do not write a giant meta-test solely for traceability.

Certification can reference existing tests.

---

## 95. BND-M5 test traceability

Automation→Work maps:

```text
BOUND-CMD-001
BOUND-CMD-002
BOUND-CMD-003
BOUND-PORT-002
BOUND-INFRA-*
BOUND-MEDIATOR-001
BOUND-DATA-002
```

Required evidence includes both consumer and producer tests.

---

## 96. BND-M6 test traceability

Events:

```text
BOUND-EVT-*
BOUND-PLATFORM-*
```

ResourceRef:

```text
BOUND-REF-*
BOUND-DATA-003
```

Projection:

```text
BOUND-PROJ-*
```

Do not require all three for a feature that only touches one mechanism.

---

## 97. BND-M7 per-feature minimum test matrix

For cross-context sync read:

```text
producer semantic contract
consumer positive/negative
scope
failure policy
architecture gates
```

For cross-context command:

```text
producer mutation behavior
consumer reaction
idempotency if needed
architecture gates
```

For event:

```text
producer contract/outbox
consumer idempotency
registry architecture
```

For projection:

```text
update
duplicate/order
rebuild/staleness
```

For Process Manager:

```text
state machine
participant outcomes
recovery
```

---

## 98. Architecture Wave 1 activation criteria

Enable blocking CI when:

```text
scanner/test is deterministic
current violations are known
baseline is exact
failure output actionable
false-positive review complete
```

Do not enable a noisy test then normalize ignoring failures.

---

## 99. Architecture Wave 2 activation criteria

For:

```text
ARCH-BC-004
ARCH-BC-005
ARCH-BC-006
```

require:

```text
ownership map stable
first reference slices completed
known legacy violations inventoried
```

---

## 100. Architecture Wave 3 activation criteria

For:

```text
ARCH-BC-007
ARCH-BC-008
ARCH-BC-009
```

activate based on:

```text
real drift risk
event/Common change frequency
machine enforcement value
```

Do not force ARCH-BC-009 catalog merely for completeness.

---

## 101. Test deletion rule

Do not delete an old architecture test because a new boundary test overlaps.

First establish:

```text
new test superset
same intended exceptions
same or better failure quality
same CI scope
```

Then consolidation may occur in a dedicated cleanup.

---

## 102. Duplicate enforcement rule

Avoid two tests enforcing same semantic with different mappings.

Example bad:

```text
DbContextBoundaryArchitectureTests has context map A
CrossContextBoundaryTests has context map B
```

If shared map is genuinely needed:

```text
extract architecture-test helper
```

only after duplication is real.

---

## 103. Test data and fixture ownership

Synthetic types used to prove gates should live near the gate tests.

Do not add fake production classes merely to test architecture scanners.

Roslyn source snippets may be used for scanner unit tests when appropriate.

---

## 104. Gate self-tests

For custom gate helpers, write tests proving:

```text
detects intended violation
does not flag allowed pattern
does not ignore nested/generic dependency if relevant
deterministic ordering
clear message
```

Example existing style:

```text
HandlerConstructorPortGate
HandlerConstructorPortGateTests
```

Follow this pattern for reusable gate helpers.

---

## 105. ARCH-BC-001 gate self-test examples

Synthetic source/type:

```text
WorkManagement handler → IWorkspaceDbContext
→ violation
```

Allowed:

```text
WorkManagement handler → IWorkManagementDbContext
→ no violation
```

Allowed Infrastructure:

```text
ApplicationDbContext implements both
→ no Application boundary violation
```

---

## 106. ARCH-BC-002 gate self-test examples

Violation:

```text
Automation Application property type = WorkManagement.Board
```

Violation:

```text
Task<IReadOnlyList<WorkManagement.Board>>
```

Allowed:

```text
Guid BoardId
ResourceRef
WorkManagement.Public.BoardFact
```

---

## 107. ARCH-BC-003 gate self-test examples

Violation:

```text
Automation
→ WorkManagement.Boards.Commands.UpdateBoardItemCommand
```

Allowed:

```text
Automation
→ WorkManagement.Public.Commands.IWorkActions
```

Violation:

```text
Automation IMediator.Send(Work internal request)
```

Allowed:

```text
Work Public implementation internally sends Work internal request
```

---

## 108. ARCH-BC-005 gate self-test examples

Violation Public contract field:

```text
DbSet<Board>
HttpRequest
GrpcRequest
WorkManagement.Board aggregate
```

Allowed:

```text
Guid
string
ResourceRef
small immutable Fact
approved Result/status
```

---

## 109. ARCH-BC-006 gate self-test examples

Violation:

```text
Application handler constructor(HttpClient)
Application service constructor(SomeGeneratedGrpcClient)
Application type uses provider SDK request
```

Allowed:

```text
Application handler constructor(IWorkActionPort)
Infrastructure adapter constructor(HttpClient)
```

---

## 110. Integration-test boundary philosophy

Integration tests prove:

```text
real DI
real DB transaction
real outbox
real adapter wiring
```

They should not bypass Application by directly writing tables unless the test is specifically persistence infrastructure.

For boundary feature tests:

```text
invoke use case through canonical Application/API path
```

where feasible.

---

## 111. Current shared DB integration tests

Current monolith may use one physical `ApplicationDbContext`.

Test must still assert logical ownership.

Example:

```text
CreateBoard
→ work tables changed
→ billing tables unchanged
→ workspace tables unchanged
```

where practical.

Do not infer context transaction from physical connection only.

---

## 112. Cross-context sync integration test

For direct Producer.Public:

```text
consumer use case
→ real in-process producer semantic implementation
```

Test semantic result.

No serialization.

For Consumer Port adapter:

```text
consumer use case
→ real adapter
→ real Producer.Public implementation
```

Test ACL mapping and DI.

---

## 113. Contract tests for Producer.Public

Producer owns contract tests.

Test:

```text
required fields
nullability
scope identity
semantic status set
compatibility expectations
no implementation leakage
```

Consumer owns interpretation tests.

---

## 114. Compatibility in current monolith

Because all five projects deploy together today:

```text
mixed-version producer/consumer runtime tests
```

are generally not required for internal semantic contracts.

Still avoid unnecessary breaking churn because teams work in parallel.

Use roadmap D-level and producer handshake.

---

## 115. Future extraction transport tests

Only after approved extraction.

For gRPC:

```text
request mapping
response mapping
deadline
status/error mapping
service identity
tenant/actor context
correlation/tracing
```

For HTTP:

```text
status mapping
serialization
timeout
service auth
scope propagation
```

Application semantic tests remain unchanged.

---

## 116. Future contract packaging tests

If future packages exist:

Assert dependency direction:

```text
Semantic package
!→ Transport package

Application
!→ generated transport package
```

Integration contracts may depend on approved generic message envelope, not transport-specific request DTOs.

---

## 117. Future mixed-version tests

Required after independent deployment.

Test supported matrix:

```text
new consumer → old producer
old consumer → new producer
new event consumer → previous event version where supported
```

Define compatibility window in extraction ADR.

---

## 118. Future service-auth tests

Test:

```text
valid service identity
invalid caller
expired credentials
actor propagation
tenant propagation
resource scope
```

Internal network does not bypass business authorization by default.

---

## 119. Future network resilience tests

Test only mechanisms actually enabled:

```text
deadline
retry
circuit breaker
load shedding
fallback/projection
```

Do not test a resilience pattern the production adapter does not use.

---

## 120. Future writer-cutover tests

Mandatory extraction integration test:

```text
old writer enabled + new writer read-only
→ only old writes

cutover
→ old writer rejects/disables writes
→ new writer writes

rollback
→ authority transitions according to approved plan
```

Never accept:

```text
both active writers
```

even temporarily.

---

## 121. Future data-extraction tests

If physical DB moves:

Test:

```text
source-of-truth cutover
migration completeness
consumer no longer reads old tables
outbox/event continuity
rollback/forward reconciliation
```

Do not make physical migration a prerequisite for semantic extraction.

---

## 122. Test evidence format for certification

Certification should reference existing test evidence.

Example:

```text
Rule: BOUND-DATA-002
Evidence:
- DbContextBoundaryArchitectureTests
- CreateBoardInWorkspace application test
- integration state-diff test
```

Do not duplicate test results into large prose evidence files.

---

## 123. Mandatory test STOP conditions

Stop certification when tests reveal or require:

```text
foreign DbContext to instantiate consumer
foreign Domain entity as consumer test input
foreign internal MediatR request from consumer
handler-local auth logic replacing pipeline
shared transaction across mutation authorities
raw network/provider exception as business outcome
cross-context cascade required for lifecycle
consumer plan/tier assertion outside Billing
dual authoritative writers
```

---

## 124. Test anti-patterns

Do not write tests that normalize bad architecture.

Examples:

```text
mock foreign DbContext in consumer handler test
mock foreign aggregate in consumer unit test
assert MediatR sends foreign internal command
assert Plan.Pro in Work test
start local HTTP server to simulate future service
snapshot thousands of architecture violations without ownership
```

---

## 125. Coding Agent test implementation order

For a boundary slice:

```text
1. read existing tests first
2. identify which existing test owns each concern
3. add semantic contract test
4. add consumer Application test
5. add producer Application/Domain test
6. add adapter test if adapter exists
7. add integration test if wiring/DB/event matters
8. add/extend architecture gate
9. run focused tests
10. run architecture suite
11. run affected project suite
12. run canonical backend gate required by repo
```

Do not create a new test class when an existing class is the clear authority and can be extended without becoming incoherent.

---

## 126. Focused test command principle

Use repository-canonical `dotnet test` commands and solution/test project paths.

Do not hard-code machine-specific absolute paths.

Exact commands should be taken from current repo tooling/AGENTS/CI at execution time.

This document does not override those commands.

---

## 127. Architecture enforcement traceability matrix

| Rule | Primary enforcement target |
|---|---|
| ARCH-BC-001 | DataAccess existing DbContext boundary tests + extensions |
| ARCH-BC-002 | DomainPurity graph + Application cross-context dependency test |
| ARCH-BC-003 | ApplicationLayer producer-internal dependency test |
| ARCH-BC-004 | DomainPurity + Infrastructure/Data relationship checks |
| ARCH-BC-005 | Contracts/Public semantic contract purity |
| ARCH-BC-006 | ApplicationLayer transport/provider purity |
| ARCH-BC-007 | Events existing public event/registry tests |
| ARCH-BC-008 | Common targeted anti-regression + review |
| ARCH-BC-009 | optional catalog tests only if admitted |

This matrix is normative for placement direction, not exact C# filenames.

---

## 128. BND milestone test exit gates

### BND-M0

```text
current test baseline known
no hidden failing gate
```

### BND-M1

```text
concrete violations classified
baseline exact
```

### BND-M2

```text
ARCH-BC-001..003 blocking new violations
```

### BND-M3

```text
dependency-spine contracts have semantic/application evidence
pipeline is not duplicated
```

### BND-M4

```text
CreateBoard reference slice passes semantic + architecture proof
```

### BND-M5

```text
Automation→Work target-owned mutation proven
```

### BND-M6

```text
touched event/ref/projection mechanisms proven
```

### BND-M7

```text
cross-context feature PRs consistently include appropriate tests
```

### BND-M8

```text
Wave-2/3 gates stable
```

### BND-M9

```text
distributed runtime tests pass according to extraction ADR
```

---

## 129. Test Definition of Ready

Before implementing boundary tests:

```text
Rule ID known
semantic owner known
source context resolution known
target context resolution known
existing overlapping test identified
legacy violations inventoried
expected allowed exceptions known
failure message designed
```

If ownership itself is unresolved:

```text
do not encode it into test
```

---

## 130. Test Definition of Done

A boundary test/gate is done when:

```text
detects intended violation
permits intended valid pattern
has deterministic ordering/output
has focused self-tests if custom helper exists
does not duplicate another authority
baseline is exact
failure message gives migration path
runs in normal CI budget
```

---

## 131. Feature Boundary Definition of Done

A cross-context feature is test-complete when:

```text
Domain behavior proven
Application orchestration proven
pipeline behavior proven where pipeline-owned
producer contract behavior proven
consumer interpretation proven
adapter mapping proven if adapter exists
transaction ownership proven
business failures proven
technical policy proven where relevant
idempotency proven where required
event/projection behavior proven where used
ARCH gates pass
no test relies on forbidden foreign model/persistence/internal request
```

---

## 132. Final testing invariant

The strongest proof of the architecture is:

```text
same consumer Application semantic tests
```

remain valid when:

```text
today:
in-process implementation

becomes:

future:
remote adapter
```

The transport-specific tests change.

The business use-case tests do not need to be rewritten around HTTP/gRPC.

That is the expected testing signature of a healthy bounded-context boundary.

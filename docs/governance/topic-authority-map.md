---
document_id: DOC-TOPIC-AUTHORITY
document_type: governance
status: active
owner: documentation-governance
applies_to:
  - repository
evidence:
  - docs/governance/documentation-authority.md
  - docs/governance/documentation-lifecycle.md
  - CONTEXT-MAP.md
  - docs/README.md
  - backend/docs/README.md
  - frontend/docs/README.md
  - backend/backend.slnx
  - frontend/tooling/dependency-rules/src/architecture-manifest.ts
review_on:
  - canonical-topic-owner-change
  - documentation-authority-change
  - bounded-context-owner-change
  - backend-architecture-owner-change
  - frontend-architecture-owner-change
  - repository-documentation-topology-change
---

# Topic Authority Map

> **This document is the canonical registry of topic ownership for Notrelix documentation.**
>
> It answers:
>
> **For topic X, which document is allowed to define the current normative contract?**

This file does not replace the canonical documents it maps.

It does not repeat their rules.

It records:

```text
topic
→ canonical owner
→ semantic scope
→ executable/current evidence
→ decision registry
→ related routing
```

The governing rule is:

> **one topic → one canonical normative owner**

Authority semantics are defined by:

[`documentation-authority.md`](documentation-authority.md)

Lifecycle semantics are defined by:

[`documentation-lifecycle.md`](documentation-lifecycle.md)

Task-oriented reading paths are defined by:

[`../../CONTEXT-MAP.md`](../../CONTEXT-MAP.md)

---

# 1. Registry contract

Every mapped normative topic MUST have:

- one stable topic ID;
- one canonical owner;
- one semantic scope;
- evidence appropriate to the claim;
- one decision registry scope where consequential rationale belongs;
- zero competing canonical owners.

A topic MAY reference several supporting documents.

Supporting documents are not co-owners unless the topic is split into distinct subtopics.

---

# 2. What this map owns

This map owns the registry of canonical topic ownership.

It does not own:

- product semantics themselves;
- backend implementation rules;
- frontend implementation rules;
- current source state;
- ADR rationale;
- generated exact inventories;
- migration procedure;
- execution workflow.

Those live in their mapped owners.

---

# 3. Topic IDs

Topic IDs are stable registry identifiers.

Prefixes:

```text
ROOT-*      repository-root topic
DOC-*       documentation governance
SYS-*       cross-stack/system architecture
PROD-*      product semantics
BE-*        backend architecture/operations
FE-*        frontend architecture
QLT-*       repository quality
DEL-*       delivery/change management
OPS-*       operations
INFRA-*     repository infrastructure/runtime
ADR-*       decision-registry ownership
GEN-*       generated evidence ownership
```

Topic IDs do not need to match document IDs one-to-one.

They identify semantic topics.

---

# 4. Registry fields

Each topic entry uses:

```text
Topic ID
Topic
Canonical owner
Scope
Primary evidence
Decision registry
Supporting references
Notes / non-ownership
```

The canonical owner is the only normative owner for that exact topic.

---

# 5. Root topic registry

## ROOT-ORIENTATION — Repository orientation and onboarding

**Canonical owner**

```text
README.md
```

**Scope**

```text
repository orientation
tech-stack summary
quick start
repository navigation
development entry points
```

**Primary evidence**

```text
backend/global.json
backend/Directory.Build.props
backend/Directory.Packages.props
backend/**/*.csproj
frontend/package.json
frontend/pnpm-workspace.yaml
frontend/pnpm-lock.yaml
docker-compose*.yml
Makefile
```

**Decision registry**

```text
none by default
```

**Supporting references**

```text
PRODUCT.md
RULE.md
DESIGN.md
AGENTS.md
CONTEXT.md
CONTEXT-MAP.md
docs/README.md
backend/README.md
frontend/README.md
```

**Non-ownership**

README MUST NOT own deep product/backend/frontend architecture.

---

## ROOT-PRODUCT — Repository product constitution

**Canonical owner**

```text
PRODUCT.md
```

**Scope**

```text
product thesis
product-wide semantics
product-wide invariants
bounded-context set at constitution level
one-work-model-many-views principle
```

**Primary evidence**

```text
docs/product/
backend/src/Notrelix.Domain/
frontend product capability source/tests
```

**Decision registry**

```text
docs/decisions/
```

when a consequential system/product architecture decision is required.

**Supporting references**

```text
docs/product/product-model.md
docs/product/contexts/*.md
```

**Non-ownership**

Detailed context semantics belong to each context document.

---

## ROOT-DESIGN — Product design constitution

**Canonical owner**

```text
DESIGN.md
```

**Scope**

```text
calm · focused · confident
design principles
interaction principles
product-vs-marketing register
accessibility baseline
product state grammar
```

**Primary evidence**

```text
frontend/packages/ui/
frontend product UI source
frontend tests
docs/quality/accessibility-standard.md
```

**Decision registry**

```text
docs/decisions/
frontend/docs/decisions/
```

according to decision scope.

**Non-ownership**

Literal tokens/primitives remain frontend-source-owned.

---

## ROOT-RULES — Repository-wide invariants

**Canonical owner**

```text
RULE.md
```

**Scope**

```text
NRX-* repository invariants
```

**Primary evidence**

```text
architecture tests
security tests
contract tests
generated checks
CI
```

**Decision registry**

```text
docs/decisions/
```

for consequential repository-wide invariant change.

**Non-ownership**

Local implementation detail belongs to project architecture docs.

---

## ROOT-AGENT-EXECUTION — Repository Coding Agent execution contract

**Canonical owner**

```text
AGENTS.md
```

**Scope**

```text
task classification
preflight
reasoning workflow
no-guess policy
stop conditions
validation/reporting
```

**Primary evidence**

```text
scoped AGENTS.md
skills
repository commands/tests/tooling
```

**Decision registry**

```text
none by default
```

**Non-ownership**

AGENTS MUST NOT redefine product/architecture semantics.

---

## ROOT-CURRENT-CONTEXT — Current repository state

**Canonical owner**

```text
CONTEXT.md
```

**Scope**

```text
current source facts
active transitions
known drift
current authority producers
```

**Primary evidence**

```text
source
tests
manifests
generated evidence
Makefile
Compose
CI
```

**Decision registry**

```text
none
```

**Non-ownership**

Current facts are non-normative unless separately owned elsewhere.

---

## ROOT-TASK-ROUTER — Task-to-authority routing

**Canonical owner**

```text
CONTEXT-MAP.md
```

**Scope**

```text
task → reading path
```

**Primary evidence**

```text
this topic-authority-map.md
docs/README.md
project docs indexes
```

**Decision registry**

```text
none
```

**Non-ownership**

CONTEXT-MAP routes; it does not define topic semantics.

---

# 6. Documentation governance registry

## DOC-AUTHORITY — Documentation authority semantics

**Canonical owner**

```text
docs/governance/documentation-authority.md
```

**Scope**

```text
authority planes
document classes
canonical ownership
summary-vs-owner
scoped-doc admission
generated authority
conflict handling
authority migration
```

**Primary evidence**

```text
docs tree
routers
metadata
docs governance scripts
```

**Decision registry**

```text
docs/decisions/
```

for consequential repository-wide governance changes.

---

## DOC-LIFECYCLE — Documentation lifecycle

**Canonical owner**

```text
docs/governance/documentation-lifecycle.md
```

**Scope**

```text
draft
active
superseded
generated
review triggers
supersession
deletion
retention
```

**Primary evidence**

```text
document metadata
generated index
docs checks
```

**Decision registry**

```text
docs/decisions/
```

when lifecycle model changes materially.

---

## DOC-TOPIC-MAP — Canonical topic registry

**Canonical owner**

```text
docs/governance/topic-authority-map.md
```

**Scope**

```text
topic → owner registry
```

**Primary evidence**

```text
canonical paths
routers
generated document index
```

**Decision registry**

```text
none by default
```

---

## DOC-DECISIONS-EXCEPTIONS — Architecture decision and exception policy

**Canonical owner**

```text
docs/governance/decision-and-exception-policy.md
```

**Scope**

```text
ADR admission
ADR supersession
architecture/product exception
exception owner
expiry/review
normalization
```

**Primary evidence**

```text
docs/decisions/
backend/docs/decisions/
frontend/docs/decisions/
architecture exception registry/tooling where implemented
```

**Decision registry**

```text
self-governed by system ADR if policy itself changes materially
```

---

## DOC-QUALITY-GATES — Documentation quality gates

**Canonical owner**

```text
docs/governance/documentation-quality-gates.md
```

**Scope**

```text
links
metadata
authority
rule IDs
ADR IDs
required paths
source inventory
generated drift
CI behavior
```

**Primary evidence**

```text
scripts/docs/
Makefile
.github/workflows/
```

**Decision registry**

```text
docs/decisions/
```

for material governance architecture changes.

---

# 7. System architecture registry

## SYS-OVERVIEW — System overview

**Canonical owner**

```text
docs/architecture/system-overview.md
```

**Scope**

```text
system boundary
modular-monolith backend
multi-host frontend
trust boundaries
external-system categories
system-wide server-authority principle
```

**Primary evidence**

```text
backend/backend.slnx
frontend/pnpm-workspace.yaml
frontend architecture manifest
Compose/deployment manifests
public contracts
```

**Decision registry**

```text
docs/decisions/
```

---

## SYS-BOUNDED-CONTEXTS — Bounded-context map

**Canonical owner**

```text
docs/architecture/bounded-context-map.md
```

**Scope**

```text
business context list
semantic ownership boundaries
upstream/downstream relations
context seams
technical capability distinction
```

**Primary evidence**

```text
PRODUCT.md
docs/product/contexts/
backend Domain/Application modules
frontend capability/package evidence
```

**Decision registry**

```text
docs/decisions/
```

**Non-ownership**

Context-specific semantics belong to `docs/product/contexts/*.md`.

---

## SYS-CONTRACTS — Cross-boundary contracts

**Canonical owner**

```text
docs/architecture/contract-boundaries.md
```

**Scope**

```text
REST/OpenAPI
realtime
integration/public events
generated clients/types
message contract boundaries
public package exports when cross-boundary
compatibility/versioning/deprecation
```

**Primary evidence**

```text
backend API/OpenAPI producer
artifacts/contracts/
frontend codegen
backend Platform message contracts
frontend realtime contracts
```

**Decision registry**

```text
docs/decisions/
```

---

## SYS-DATA-CONSISTENCY — Data ownership and consistency

**Canonical owner**

```text
docs/architecture/data-ownership-and-consistency.md
```

**Scope**

```text
authoritative ownership
local transaction boundaries
cross-context eventual consistency
projections
cache-derived state
frontend cache projection
process-manager admission
retry/idempotency relation
```

**Primary evidence**

```text
Domain/Application/Infrastructure/Platform source/tests
frontend state/realtime source/tests
integration tests
```

**Decision registry**

```text
docs/decisions/
```

---

## SYS-EVENT-BOUNDARY — Events, realtime, and delivery taxonomy

**Canonical owner**

```text
docs/architecture/events-realtime-and-delivery-boundary.md
```

**Scope**

```text
Domain event
integration/public event
outbox record
message envelope
realtime notification
activity
audit
```

**Primary evidence**

```text
backend Domain events
Platform delivery source/tests
frontend realtime source/tests
activity/audit source
```

**Decision registry**

```text
docs/decisions/
```

---

## SYS-EXTRACTION — Capability/service extraction strategy

**Canonical owner**

```text
docs/architecture/capability-extraction-strategy.md
```

**Scope**

```text
modular-monolith position
extraction triggers
service prerequisites
data/contract/runtime extraction conditions
anti-premature-microservice rules
```

**Primary evidence**

```text
bounded-context map
backend project structure
contract boundaries
deployment/runtime evidence
```

**Decision registry**

```text
docs/decisions/
```

---

# 8. Product registry

## PROD-MODEL — Cross-context product model

**Canonical owner**

```text
docs/product/product-model.md
```

**Scope**

```text
cross-context product vocabulary
capability relationships
shared product-level concepts
```

**Primary evidence**

```text
PRODUCT.md
context docs
Domain/Application/frontend behavior evidence
```

**Decision registry**

```text
docs/decisions/
```

---

## PROD-EXPERIENCE — Product experience semantics

**Canonical owner**

```text
docs/product/product-experience.md
```

**Scope**

```text
cross-capability product experience
coherence
long-session work behavior
product language
state integrity expectations
enterprise product behavior
```

**Primary evidence**

```text
DESIGN.md
frontend product/UI behavior
accessibility tests
product contexts
```

**Decision registry**

```text
docs/decisions/
```

---

## PROD-ACCOUNTS — Accounts semantics

**Canonical owner**

```text
docs/product/contexts/accounts.md
```

**Scope**

```text
Account meaning
account-level ownership
account lifecycle
account administration
```

**Primary evidence**

```text
backend Domain/Application Accounts source/tests
frontend account capability source/tests
```

**Decision registry**

```text
docs/decisions/
```

---

## PROD-IDENTITY — Identity semantics

**Canonical owner**

```text
docs/product/contexts/identity.md
```

**Scope**

```text
user identity
authentication identity
session
credential
MFA
OAuth identity
API/security principal lifecycle
```

**Primary evidence**

```text
backend Identity Domain/Application/API/Infrastructure source/tests
frontend auth source/tests
```

**Decision registry**

```text
docs/decisions/
```

---

## PROD-WORKSPACES — Workspace semantics

**Canonical owner**

```text
docs/product/contexts/workspaces.md
```

**Scope**

```text
Workspace
membership
invitation
workspace lifecycle
collaboration tenancy
```

**Primary evidence**

```text
backend Workspaces source/tests
frontend workspace source/tests
```

**Decision registry**

```text
docs/decisions/
```

---

## PROD-GOVERNANCE — Governance semantics

**Canonical owner**

```text
docs/product/contexts/governance.md
```

**Scope**

```text
permission meaning
sharing
resource access
guest/share-link semantics
administrative/security audit meaning
```

**Primary evidence**

```text
backend Governance source/tests
authorization tests
frontend governance/permission UX
```

**Decision registry**

```text
docs/decisions/
```

---

## PROD-WORK-MANAGEMENT — Work Management semantics

**Canonical owner**

```text
docs/product/contexts/work-management.md
```

**Scope**

```text
Board
BoardField
BoardItem
BoardGroup
BoardView
field engine
item/group ordering
Table
Kanban
Calendar
Timeline
Form
Dashboard
relations/formulas/rollups
work lifecycle
```

**Primary evidence**

```text
backend Domain/Application WorkManagement source/tests
frontend Work Management product packages/tests
API/contracts
```

**Decision registry**

```text
docs/decisions/
```

**Non-ownership**

Backend/frontend architecture docs implement these semantics; they do not redefine them.

---

## PROD-DOCUMENTS — Documents semantics

**Canonical owner**

```text
docs/product/contexts/documents.md
```

**Scope**

```text
Page
Block
hierarchy
document content
document lifecycle
resource links/embeds
```

**Primary evidence**

```text
backend Documents source/tests
frontend Documents product packages/tests
```

**Decision registry**

```text
docs/decisions/
```

---

## PROD-COLLABORATION — Collaboration semantics

**Canonical owner**

```text
docs/product/contexts/collaboration.md
```

**Scope**

```text
comments
threads
mentions
reactions
notifications
presence
user-facing activity semantics
```

**Primary evidence**

```text
backend Collaboration source/tests
frontend collaboration/notification/activity source/tests
realtime evidence
```

**Decision registry**

```text
docs/decisions/
```

---

## PROD-AUTOMATION — Automation semantics

**Canonical owner**

```text
docs/product/contexts/automation.md
```

**Scope**

```text
rule
trigger
condition
action
schedule
execution identity
automation lifecycle
```

**Primary evidence**

```text
backend Automation source/tests
frontend Automation product packages/tests
Platform execution evidence
```

**Decision registry**

```text
docs/decisions/
```

---

## PROD-INTEGRATIONS — Integrations semantics

**Canonical owner**

```text
docs/product/contexts/integrations.md
```

**Scope**

```text
connection
provider lifecycle
webhook
mapping
synchronization
external anti-corruption boundary
provider conflict/revision semantics
```

**Primary evidence**

```text
backend Integrations source/tests
provider adapters
frontend integrations feature
```

**Decision registry**

```text
docs/decisions/
```

---

## PROD-BILLING — Billing semantics

**Canonical owner**

```text
docs/product/contexts/billing.md
```

**Scope**

```text
plan
subscription
entitlement
usage
limit
commercial lifecycle
downgrade/payment-failure behavior
```

**Primary evidence**

```text
backend Billing source/tests
frontend billing source/tests
provider billing adapters
```

**Decision registry**

```text
docs/decisions/
```

---

## PROD-ANALYTICS — Analytics / Reporting semantics

**Canonical owner**

```text
docs/product/contexts/analytics.md
```

**Scope**

```text
metric
dashboard
widget
reporting
aggregation
freshness
derived insight
```

**Primary evidence**

```text
backend Analytics source/tests
frontend analytics/dashboard behavior
query/reporting evidence
```

**Decision registry**

```text
docs/decisions/
```

---

## PROD-SEARCH — Search product/technical capability boundary

**Canonical owner**

```text
docs/product/product-model.md
```

for the statement that Search is a supporting capability unless explicitly promoted to a business context.

**Scope**

```text
classification of Search relative to business context model
```

**Primary evidence**

```text
frontend features-search
backend search/indexing implementation
bounded-context map
```

**Decision registry**

```text
docs/decisions/
```

**Supporting implementation owners**

```text
backend/docs/architecture/infrastructure-and-data.md
backend/docs/architecture/security-tenancy-authorization.md
frontend/docs/architecture/state-query-mutations.md
frontend/docs/architecture/dependency-boundaries.md
```

**Non-ownership**

Search implementation specifics are not owned by `product-model.md`.

---

# 9. Backend architecture registry

## BE-OVERVIEW — Backend topology and project responsibilities

**Canonical owner**

```text
backend/docs/architecture/backend-overview.md
```

**Scope**

```text
five production project roles
dependency direction
composition
bounded-context placement philosophy
Platform vs Infrastructure
```

**Primary evidence**

```text
backend/backend.slnx
backend/**/*.csproj
backend/tests/Notrelix.Architecture.Tests/
```

**Decision registry**

```text
backend/docs/decisions/
```

---

## BE-DOMAIN — Domain modeling

**Canonical owner**

```text
backend/docs/architecture/domain-modeling.md
```

**Scope**

```text
aggregate admission
entity/value object modeling
deterministic invariants
mutation order
semantic no-op
failure atomicity
version/audit/event semantics
lifecycle/soft-delete principles
typed IDs
SharedKernel admission
ordering/hierarchy
```

**Primary evidence**

```text
backend/src/Notrelix.Domain/
backend/tests/Notrelix.Domain.Tests/
backend/tests/Notrelix.Architecture.Tests/
```

**Decision registry**

```text
backend/docs/decisions/
```

---

## BE-APPLICATION — Application use-case model

**Canonical owner**

```text
backend/docs/architecture/application-model.md
```

**Scope**

```text
commands/queries
handler responsibility
vertical use-case organization
orchestration
external facts
authorization integration
transaction ownership
expected version
idempotency contract
post-commit coordination
```

**Primary evidence**

```text
backend/src/Notrelix.Application/
backend/tests/Notrelix.Application.Tests/
backend/tests/Notrelix.Integration.Tests/
```

**Decision registry**

```text
backend/docs/decisions/
```

---

## BE-APP-PIPELINE — Application pipeline order and behavior contract

**Canonical owner**

```text
backend/docs/architecture/application-model.md
```

**Scope**

```text
marker contracts
behavior ordering
authorization
validation
transaction
expected-version
idempotency
post-commit
```

**Primary evidence**

```text
Application registrations
behavior implementations
Application tests
integration tests
```

**Decision registry**

```text
backend/docs/decisions/
```

**Non-ownership**

This registry does not encode exact order; the canonical owner plus source/tests do.

---

## BE-INFRA-DATA — Persistence/data/provider implementation

**Canonical owner**

```text
backend/docs/architecture/infrastructure-and-data.md
```

**Scope**

```text
DbContext strategy
EF mappings
PostgreSQL
RLS implementation
migrations relationship
indexes
Redis/cache implementation
provider adapters
search/index implementation
```

**Primary evidence**

```text
backend/src/Notrelix.Infrastructure/
backend/tests/Notrelix.Infrastructure.Tests/
backend/tests/Notrelix.Integration.Tests/
```

**Decision registry**

```text
backend/docs/decisions/
```

---

## BE-PLATFORM — Platform/messaging/reliability

**Canonical owner**

```text
backend/docs/architecture/platform-and-messaging.md
```

**Scope**

```text
message identity
consumer identity
outbox delivery
dedup/idempotency
ordering
retry
dead-letter
poison detection
background delivery runtime
tenant execution context for platform flows
```

**Primary evidence**

```text
backend/src/Notrelix.Platform/
backend/tests/Notrelix.Platform.Tests/
backend/tests/Notrelix.Integration.Tests/
```

**Decision registry**

```text
backend/docs/decisions/
```

---

## BE-API — API/OpenAPI/public HTTP contract implementation

**Canonical owner**

```text
backend/docs/architecture/api-and-contracts.md
```

**Scope**

```text
endpoint conventions
request/result mapping
auth integration
error mapping
OpenAPI
API versioning
pagination/filter/sort conventions
idempotency input
public compatibility
```

**Primary evidence**

```text
backend/src/Notrelix.API/
backend/tests/Notrelix.API.Tests/
OpenAPI artifacts
integration tests
```

**Decision registry**

```text
backend/docs/decisions/
```

---

## BE-SECURITY — Backend security, tenancy, authorization

**Canonical owner**

```text
backend/docs/architecture/security-tenancy-authorization.md
```

**Scope**

```text
authentication/authorization boundary
tenant/resource resolution
Application authorization authority
RLS defense-in-depth
background principal/tenant context
permission-sensitive cache/search/export/realtime implications
```

**Primary evidence**

```text
Application security source/tests
Infrastructure RLS source/tests
API auth source/tests
integration tests
```

**Decision registry**

```text
backend/docs/decisions/
```

---

## BE-TESTING — Backend testing and quality gates

**Canonical owner**

```text
backend/docs/architecture/testing-and-quality-gates.md
```

**Scope**

```text
backend test-project responsibility
architecture tests
Domain/Application/Infrastructure/Platform/API/Integration gates
OpenAPI drift
non-zero test execution
```

**Primary evidence**

```text
backend/backend.slnx
backend/tests/
.github/workflows/
```

**Decision registry**

```text
backend/docs/decisions/
```

when test architecture itself changes materially.

---

## BE-CONFIG — Backend configuration/runtime

**Canonical owner**

```text
backend/docs/operations/configuration-and-runtime.md
```

**Scope**

```text
backend configuration model
precedence
runtime options
secrets/config integration
local/container runtime behavior
```

**Primary evidence**

```text
backend configuration source
.env.example
docker-compose*.yml
Makefile
```

**Decision registry**

```text
backend/docs/decisions/
```

---

## BE-MIGRATIONS — Backend migrations/data change

**Canonical owner**

```text
backend/docs/operations/migrations-and-data-change.md
```

**Scope**

```text
EF migration process
pending model changes
expand/contract
backfill
RLS/index/schema considerations
deploy sequencing
recovery
```

**Primary evidence**

```text
EF migrations
DbContext/mappings
migration scripts
integration tests
```

**Decision registry**

```text
backend/docs/decisions/
```

for durable backend data-architecture decisions.

---

# 10. Frontend architecture registry

## FE-OVERVIEW — Frontend architecture and package-family responsibilities

**Canonical owner**

```text
frontend/docs/architecture/frontend-overview.md
```

**Scope**

```text
multi-host frontend
package families
apps compose
foundation/runtime/ui/product/feature/tooling roles
```

**Primary evidence**

```text
frontend/pnpm-workspace.yaml
frontend/tooling/dependency-rules/src/architecture-manifest.ts
frontend/package.json
```

**Decision registry**

```text
frontend/docs/decisions/
```

---

## FE-DEPENDENCIES — Frontend package dependency boundaries

**Canonical owner**

```text
frontend/docs/architecture/dependency-boundaries.md
```

**Scope**

```text
package dependency rules
public exports
deep-import prohibition
closed-world manifest semantics
mobile/web dependency safety
```

**Primary evidence**

```text
frontend/tooling/dependency-rules/src/architecture-manifest.ts
frontend/docs/generated/package-boundaries.md
architecture checks
```

**Decision registry**

```text
frontend/docs/decisions/
```

---

## FE-HOSTS — Frontend hosts, composition, routing

**Canonical owner**

```text
frontend/docs/architecture/hosts-composition-routing.md
```

**Scope**

```text
web host
mobile host
marketing host
bootstrap/providers
routing/navigation ownership
environment/runtime composition
```

**Primary evidence**

```text
frontend/apps/
frontend/packages/runtimes/
host tests
```

**Decision registry**

```text
frontend/docs/decisions/
```

---

## FE-CONTRACTS — Frontend API/generated contract usage

**Canonical owner**

```text
frontend/docs/architecture/api-and-contracts.md
```

**Scope**

```text
generated contract consumption
API client boundary
error normalization
auth/session transport
public compatibility on client side
```

**Primary evidence**

```text
artifacts/contracts/
frontend codegen tooling
frontend contract/client source/tests
backend OpenAPI producer
```

**Decision registry**

```text
frontend/docs/decisions/
```

---

## FE-STATE — Frontend server state, queries, mutations

**Canonical owner**

```text
frontend/docs/architecture/state-query-mutations.md
```

**Scope**

```text
query-key taxonomy
tenant/workspace/resource scope
server-state ownership
cache invalidation
mutation lifecycle
optimistic admission/rollback
stale-response protection
workspace transitions
local-vs-server state
```

**Primary evidence**

```text
frontend query/state source
frontend tests
integration/E2E where relevant
```

**Decision registry**

```text
frontend/docs/decisions/
```

---

## FE-REALTIME — Frontend realtime

**Canonical owner**

```text
frontend/docs/architecture/realtime.md
```

**Scope**

```text
connection state
subscription ownership
event identity/scope
duplicate/out-of-order/gap
heartbeat
reconnect/backoff
cache reconciliation
workspace transition
mobile lifecycle
```

**Primary evidence**

```text
frontend realtime source/tests
backend realtime/message producer evidence
```

**Decision registry**

```text
frontend/docs/decisions/
```

---

## FE-UI — Frontend UI/design-system implementation

**Canonical owner**

```text
frontend/docs/architecture/ui-and-design-system.md
```

**Scope**

```text
token implementation ownership
web/mobile primitives
component ownership
product component vs primitive
accessibility implementation
Storybook/gallery
vendor/shadcn policy
```

**Primary evidence**

```text
frontend/packages/ui/
frontend product UI source
component/a11y tests
```

**Decision registry**

```text
frontend/docs/decisions/
```

---

## FE-TESTING — Frontend testing and quality gates

**Canonical owner**

```text
frontend/docs/architecture/testing-and-quality-gates.md
```

**Scope**

```text
typecheck/lint/test architecture
architecture checks
codegen drift
UI/a11y/visual
E2E
container/host proof
non-zero guarded suites
```

**Primary evidence**

```text
frontend/package.json
frontend tests
frontend/e2e
architecture tooling
CI
```

**Decision registry**

```text
frontend/docs/decisions/
```

---

## FE-ARCH-CHANGE — Frontend architecture-change policy

**Canonical owner**

```text
frontend/docs/architecture/architecture-change-policy.md
```

**Scope**

```text
package graph change
host/runtime model change
foundation admission
product/feature ownership move
architecture exception/deprecation
```

**Primary evidence**

```text
architecture manifest
ADRs
architecture tests
```

**Decision registry**

```text
frontend/docs/decisions/
```

---

# 11. Repository quality registry

## QLT-ENGINEERING — Engineering quality standard

**Canonical owner**

```text
docs/quality/engineering-quality-standard.md
```

**Scope**

```text
ownership clarity
complexity
dependency hygiene
error handling
dead compatibility
architecture-aware review
documentation-as-code
```

**Primary evidence**

```text
lint/analyzers
architecture checks
tests
code review
CI
```

**Decision registry**

```text
docs/decisions/
```

when repository-wide standard changes materially.

---

## QLT-TESTING — Repository testing strategy

**Canonical owner**

```text
docs/quality/testing-strategy.md
```

**Scope**

```text
test philosophy
behavior-vs-implementation
unit/Domain
integration
architecture
contract
E2E
fixture policy
flaky policy
non-zero proof
```

**Primary evidence**

```text
backend/frontend testing docs
CI
test projects/suites
```

**Decision registry**

```text
docs/decisions/
```

---

## QLT-SECURITY — Security quality standard

**Canonical owner**

```text
docs/quality/security-quality-standard.md
```

**Scope**

```text
secret safety
sensitive data
dependency security
secure logging
security testing
vulnerability handling
auth/authz engineering quality
```

**Primary evidence**

```text
security scanners
backend security tests
secret scanning
CI
```

**Decision registry**

```text
docs/decisions/
```

---

## QLT-ACCESSIBILITY — Accessibility standard

**Canonical owner**

```text
docs/quality/accessibility-standard.md
```

**Scope**

```text
WCAG target
keyboard/focus
screen reader
contrast
touch
reduced motion
accessibility proof
```

**Primary evidence**

```text
DESIGN.md
frontend UI tests
axe
E2E/manual accessibility review
```

**Decision registry**

```text
docs/decisions/
```

---

## QLT-PERFORMANCE — Performance and scalability standard

**Canonical owner**

```text
docs/quality/performance-and-scalability.md
```

**Scope**

```text
bounded queries
pagination
index/queryability
cache discipline
payload size
realtime fan-out
frontend large-data rendering
performance evidence
```

**Primary evidence**

```text
query tests
indexes/migrations
performance tests
frontend large-data tests
observability
```

**Decision registry**

```text
docs/decisions/
```

---

# 12. Delivery registry

## DEL-CLASSIFICATION — Change classification

**Canonical owner**

```text
docs/delivery/change-classification.md
```

**Scope**

```text
local refactor
behavior change
product semantic change
public contract change
schema/data change
architecture change
security change
operational change
```

**Primary evidence**

```text
AGENTS.md
RULE.md
change review/CI
```

**Decision registry**

```text
none by default
```

---

## DEL-MIGRATION — Change impact and migration

**Canonical owner**

```text
docs/delivery/change-impact-and-migration.md
```

**Scope**

```text
consumer inventory
compatibility
expand/contract
staged rollout
backfill
deprecation
cleanup
migration proof
```

**Primary evidence**

```text
contracts
migrations
consumer source
CI/release evidence
```

**Decision registry**

```text
docs/decisions/
```

for system-wide migration strategy changes.

---

## DEL-DOD — Repository definition of done

**Canonical owner**

```text
docs/delivery/definition-of-done.md
```

**Scope**

```text
complete change requirements
tests
architecture/security gates
docs
generated artifacts
migration
observability
cleanup
```

**Primary evidence**

```text
project test/gate docs
CI
PR review
```

**Decision registry**

```text
none by default
```

---

## DEL-ROLLOUT — Release, rollout, recovery

**Canonical owner**

```text
docs/delivery/release-rollout-and-recovery.md
```

**Scope**

```text
release sequencing
compatibility rollout
rollback/roll-forward principles
recovery readiness
```

**Primary evidence**

```text
deployment manifests
CI/CD
migration plans
operations evidence
```

**Decision registry**

```text
docs/decisions/
```

---

# 13. Operations registry

## OPS-OBSERVABILITY — Observability

**Canonical owner**

```text
docs/operations/observability.md
```

**Scope**

```text
logs
metrics
traces
correlation
operational identifiers
sensitive-data limits
diagnostic ownership
```

**Primary evidence**

```text
backend/frontend observability source
runtime config
monitoring/CI
```

**Decision registry**

```text
docs/decisions/
```

---

## OPS-INCIDENT — Incident readiness

**Canonical owner**

```text
docs/operations/incident-readiness.md
```

**Scope**

```text
incident classification
roles
diagnosis
escalation
evidence
recovery decision flow
```

**Primary evidence**

```text
monitoring
runbooks
operational procedures
```

**Decision registry**

```text
docs/decisions/
```

---

## OPS-RECOVERY — Recovery and data safety

**Canonical owner**

```text
docs/operations/recovery-and-data-safety.md
```

**Scope**

```text
backup/restore principles
data corruption response
recovery validation
destructive event recovery
```

**Primary evidence**

```text
backup/restore tooling
database scripts
recovery tests
```

**Decision registry**

```text
docs/decisions/
```

---

## OPS-DEGRADATION — Service degradation

**Canonical owner**

```text
docs/operations/service-degradation.md
```

**Scope**

```text
dependency outage behavior
Redis degradation
messaging degradation
provider degradation
realtime degradation
read-only/degraded product modes
```

**Primary evidence**

```text
runtime behavior
integration tests
resilience configuration
frontend state behavior
```

**Decision registry**

```text
docs/decisions/
```

---

# 14. Infrastructure registry

## INFRA-ENVIRONMENT — Environment model

**Canonical owner**

```text
docs/infrastructure/environment-model.md
```

**Scope**

```text
local/development/staging/production model
configuration/secrets flow at repository level
environment isolation
```

**Primary evidence**

```text
.env.example
Compose
deployment config
Makefile
```

**Decision registry**

```text
docs/decisions/
```

---

## INFRA-DEPLOYMENT — Deployment runtime

**Canonical owner**

```text
docs/infrastructure/deployment-runtime.md
```

**Scope**

```text
deployed runtime topology
gateway
service/container relationships
deployment dependencies
environment rollout topology
```

**Primary evidence**

```text
docker-compose*.yml
infra/
CI/CD
deployment manifests
```

**Decision registry**

```text
docs/decisions/
```

---

## INFRA-CONTAINERS — Containerization and local services

**Canonical owner**

```text
docs/infrastructure/containerization-and-local-services.md
```

**Scope**

```text
local containers
Compose role
development dependencies
optional profiles/tools
developer runtime boundaries
```

**Primary evidence**

```text
docker-compose.yml
docker-compose.dev.yml
Makefile
Dockerfiles
```

**Decision registry**

```text
docs/decisions/
```

---

# 15. Decision-registry ownership

## ADR-SYSTEM — System/repository ADR registry

**Canonical owner**

```text
docs/decisions/README.md
```

**Scope**

```text
SYS-ADR-* registry
repository-wide/system decisions
```

**Primary evidence**

```text
docs/decisions/SYS-ADR-*.md
```

---

## ADR-BACKEND — Backend ADR registry

**Canonical owner**

```text
backend/docs/decisions/README.md
```

**Scope**

```text
ADR-* backend-specific decisions
```

**Primary evidence**

```text
backend/docs/decisions/ADR-*.md
```

---

## ADR-FRONTEND — Frontend ADR registry

**Canonical owner**

```text
frontend/docs/decisions/README.md
```

**Scope**

```text
FE-ADR-* frontend-specific decisions
```

**Primary evidence**

```text
frontend/docs/decisions/FE-ADR-*.md
```

---

# 16. Generated-evidence registry

## GEN-DOCUMENT-INDEX — Documentation index

**Canonical producer**

```text
scripts/docs/generate-document-index.mjs
```

**Generated output**

```text
docs/generated/document-index.md
```

**Scope**

```text
exact canonical document metadata inventory
```

**Primary input**

```text
canonical document frontmatter
```

**Non-ownership**

Generated output does not define authority semantics.

---

## GEN-RULE-INDEX — Rule index

**Canonical producer**

```text
scripts/docs/generate-rule-index.mjs
```

**Generated output**

```text
docs/generated/rule-index.md
```

**Scope**

```text
exact stable rule-ID inventory
```

**Primary input**

```text
active normative documents
```

---

## GEN-BACKEND-PROJECT-MAP — Backend project map

**Canonical producer inputs**

```text
backend/backend.slnx
backend/**/*.csproj
```

**Target generator**

```text
scripts/docs/generate-backend-project-map.mjs
```

**Generated output**

```text
backend/docs/generated/project-map.md
```

**Scope**

```text
exact backend project/reference inventory
```

---

## GEN-FRONTEND-PACKAGE-MAP — Frontend package-boundary map

**Canonical producer**

```text
frontend/tooling/dependency-rules/src/architecture-manifest.ts
```

**Generated output**

```text
frontend/docs/generated/package-boundaries.md
```

**Scope**

```text
exact frontend registered package/dependency architecture
```

**Generation/drift command**

Use the actual dependency-rules package scripts.

Do not invent a replacement command in this registry.

---

# 17. Non-canonical artifact registry

The following artifact classes are explicitly non-canonical for durable architecture unless another governance document says otherwise.

---

## Plans / roadmaps

Examples:

```text
freeze plan
implementation plan
wave plan
migration checklist
```

**Owner**

```text
task/issue/project/temporary migration artifact
```

**Canonical effect**

None by themselves.

Durable decisions must migrate into canonical owners.

---

## Audits / assessments

Examples:

```text
architecture score
readiness assessment
freeze audit
baseline analysis
```

**Owner**

```text
point-in-time evidence
```

**Canonical effect**

None by themselves.

Findings become issues/docs/ADRs/tests as appropriate.

---

## Freeze certificates

**Owner**

```text
CI/release evidence
```

**Canonical effect**

None on current architecture ownership.

---

## Migration ledgers

**Owner**

```text
temporary documentation migration process
```

**Canonical effect**

None after migration completes.

Delete when retention purpose ends.

---

# 18. Forbidden competing owners

The following target relationships are forbidden.

## Backend

Forbidden canonical duplicates:

```text
docs/engineering/02-backend/*
docs/backend/*
backend/RULE.md
backend/PROMPT.md
backend/src/*/architecture-handbook.md
```

when they redefine topics already owned by `backend/docs/architecture/*`.

---

## Frontend

Forbidden canonical duplicates:

```text
docs/engineering/03-frontend/*
docs/frontend/*
frontend/RULES.md
frontend/ARCHITECTURE.md
frontend/MIGRATION_TRACKER.md
```

when they redefine topics already owned by `frontend/docs/architecture/*`.

---

## Root

Forbidden canonical duplicates:

```text
SKILL.md
MEMORY.md
RULE-v2.md
PRODUCT-final.md
architecture-final-v4.md
```

for topics already owned by the root/canonical tree.

---

# 19. Shared topic versus local consequence

A repository topic can have one owner while local docs own implementation consequences.

Example:

```text
NRX-003 tenant isolation
→ RULE.md
```

Frontend consequence:

```text
workspace-scoped query keys must preserve scope
→ frontend/docs/architecture/state-query-mutations.md
```

Backend consequence:

```text
Application auth + RLS defense
→ backend/docs/architecture/security-tenancy-authorization.md
```

These are not duplicate owners because the local topics are narrower implementation consequences.

---

# 20. Product topic versus backend/frontend implementation

Example:

```text
BoardView semantics
→ docs/product/contexts/work-management.md
```

Backend modeling:

```text
aggregate/event implementation
→ backend Domain/Application docs
```

Frontend representation:

```text
view-state/UI implementation
→ frontend state/UI docs
```

The product context remains the semantic owner.

---

# 21. Cross-stack topic versus project detail

Example:

```text
Public contract compatibility
→ docs/architecture/contract-boundaries.md
```

Backend API-specific behavior:

```text
→ backend/docs/architecture/api-and-contracts.md
```

Frontend generated-client consumption:

```text
→ frontend/docs/architecture/api-and-contracts.md
```

Do not place exact endpoint implementation in the system contract document.

---

# 22. Authority move protocol

When a topic moves:

1. update this registry;
2. migrate durable content;
3. update `CONTEXT-MAP.md`;
4. update `docs/README.md` / project indexes;
5. update generated document index;
6. update ADR/decision scope if required;
7. remove old canonical claim;
8. remove old references;
9. run docs governance.

A topic move is not complete while this registry and the old owner disagree.

---

# 23. Topic split protocol

Split a topic only when one entry currently hides two distinct normative questions.

Example:

```text
"security"
```

may need separate topics:

```text
product Governance semantics
backend authorization/RLS implementation
repository security quality standard
```

Do not map all three to one document simply because they share the word “security”.

---

# 24. Topic merge protocol

Merge topics when:

- they have the same owner;
- they always change together;
- their semantic boundary is indistinguishable;
- separate entries add no routing/governance value.

Do not merge merely to reduce table size.

---

# 25. Evidence rules

Evidence is not co-ownership.

Example:

```text
Topic:
Frontend dependency boundaries

Owner:
frontend/docs/architecture/dependency-boundaries.md

Evidence:
architecture-manifest.ts
package-boundaries.md
architecture checks
```

The manifest can be authoritative for exact package edges without becoming the prose owner of all frontend architecture rationale.

---

# 26. Decision registry rules

Decision registry indicates where consequential rationale belongs.

It does not mean every change requires an ADR.

Use:

```text
docs/governance/decision-and-exception-policy.md
```

to determine ADR threshold.

---

# 27. Current context relationship

`CONTEXT.md` may mention:

```text
current project/package
current legacy tree
current transition
current toolchain
```

It is evidence for the current repository.

This map remains the authority registry.

Current file existence does not alter topic ownership automatically.

---

# 28. Router relationship

`CONTEXT-MAP.md` should route tasks to the owners in this registry.

If it routes a task to a different canonical owner:

```text
documentation governance is inconsistent
```

Fix the mismatch.

Do not allow two parallel routing models.

---

# 29. README relationship

`docs/README.md` may summarize this registry by directory/role.

It must not maintain a conflicting topic-owner table manually.

If a precise topic owner changes, update this registry first, then adjust README routing.

---

# 30. Generated topic-map possibility

This registry is authored because semantic topic boundaries require human architecture decisions.

However, portions MAY become machine-validated.

Examples:

- canonical paths exist;
- document IDs match;
- mapped owner metadata matches topic owner;
- no mapped owner is draft/superseded;
- no forbidden legacy owner remains;
- router references resolve.

Do not fully auto-generate semantic topic ownership from directory names.

Directories cannot decide architecture semantics.

---

# 31. Minimum CI invariants for this registry

Documentation governance SHOULD fail when:

```text
mapped canonical owner path missing
mapped canonical owner status != active
duplicate Topic ID
same exact normative topic mapped to multiple owners
forbidden legacy path mapped as canonical
generated output mapped as semantic owner when authored owner exists
CONTEXT-MAP route conflicts with registry
document metadata owner/type incompatible with mapped topic
```

---

# 32. Topic-owner change severity

## Blocker

- product fact mapped to two contexts;
- backend/frontend implementation owner duplicated;
- security/tenant topic has conflicting owners;
- router points to a non-owner;
- generated file treated as semantic owner incorrectly.

## Major

- evidence paths stale;
- decision registry wrong;
- supporting reference implies competing authority;
- topic too broad and hiding multiple owners.

## Minor

- description wording;
- support-reference completeness;
- evidence path granularity.

---

# 33. Review checklist

Before changing this registry:

```text
[ ] topic definition is explicit
[ ] new owner exists or is being created
[ ] owner document class is correct
[ ] owner status is active at activation time
[ ] old owner identified
[ ] durable knowledge migrated
[ ] decision/ADR implications handled
[ ] CONTEXT-MAP updated
[ ] docs/README/project index updated
[ ] generated index updated
[ ] old canonical references removed
[ ] docs governance passes
```

---

# 34. Compact canonical-owner matrix

| Topic ID | Topic | Canonical owner |
|---|---|---|
| ROOT-ORIENTATION | Repository onboarding | `README.md` |
| ROOT-PRODUCT | Product constitution | `PRODUCT.md` |
| ROOT-DESIGN | Design constitution | `DESIGN.md` |
| ROOT-RULES | Repository invariants | `RULE.md` |
| ROOT-AGENT-EXECUTION | Agent execution | `AGENTS.md` |
| ROOT-CURRENT-CONTEXT | Current repository facts | `CONTEXT.md` |
| ROOT-TASK-ROUTER | Task routing | `CONTEXT-MAP.md` |
| DOC-AUTHORITY | Documentation authority | `docs/governance/documentation-authority.md` |
| DOC-LIFECYCLE | Documentation lifecycle | `docs/governance/documentation-lifecycle.md` |
| DOC-TOPIC-MAP | Topic ownership registry | `docs/governance/topic-authority-map.md` |
| DOC-DECISIONS-EXCEPTIONS | Decision/exception governance | `docs/governance/decision-and-exception-policy.md` |
| DOC-QUALITY-GATES | Documentation gates | `docs/governance/documentation-quality-gates.md` |
| SYS-OVERVIEW | System overview | `docs/architecture/system-overview.md` |
| SYS-BOUNDED-CONTEXTS | Bounded-context map | `docs/architecture/bounded-context-map.md` |
| SYS-CONTRACTS | Cross-boundary contracts | `docs/architecture/contract-boundaries.md` |
| SYS-DATA-CONSISTENCY | Data ownership/consistency | `docs/architecture/data-ownership-and-consistency.md` |
| SYS-EVENT-BOUNDARY | Event/realtime taxonomy | `docs/architecture/events-realtime-and-delivery-boundary.md` |
| SYS-EXTRACTION | Capability extraction | `docs/architecture/capability-extraction-strategy.md` |
| PROD-MODEL | Product model | `docs/product/product-model.md` |
| PROD-EXPERIENCE | Product experience | `docs/product/product-experience.md` |
| PROD-ACCOUNTS | Accounts | `docs/product/contexts/accounts.md` |
| PROD-IDENTITY | Identity | `docs/product/contexts/identity.md` |
| PROD-WORKSPACES | Workspaces | `docs/product/contexts/workspaces.md` |
| PROD-GOVERNANCE | Governance | `docs/product/contexts/governance.md` |
| PROD-WORK-MANAGEMENT | Work Management | `docs/product/contexts/work-management.md` |
| PROD-DOCUMENTS | Documents | `docs/product/contexts/documents.md` |
| PROD-COLLABORATION | Collaboration | `docs/product/contexts/collaboration.md` |
| PROD-AUTOMATION | Automation | `docs/product/contexts/automation.md` |
| PROD-INTEGRATIONS | Integrations | `docs/product/contexts/integrations.md` |
| PROD-BILLING | Billing | `docs/product/contexts/billing.md` |
| PROD-ANALYTICS | Analytics/Reporting | `docs/product/contexts/analytics.md` |
| BE-OVERVIEW | Backend architecture | `backend/docs/architecture/backend-overview.md` |
| BE-DOMAIN | Domain modeling | `backend/docs/architecture/domain-modeling.md` |
| BE-APPLICATION | Application model | `backend/docs/architecture/application-model.md` |
| BE-INFRA-DATA | Infrastructure/data | `backend/docs/architecture/infrastructure-and-data.md` |
| BE-PLATFORM | Platform/messaging | `backend/docs/architecture/platform-and-messaging.md` |
| BE-API | API/contracts | `backend/docs/architecture/api-and-contracts.md` |
| BE-SECURITY | Backend security/tenancy/authz | `backend/docs/architecture/security-tenancy-authorization.md` |
| BE-TESTING | Backend tests/gates | `backend/docs/architecture/testing-and-quality-gates.md` |
| BE-CONFIG | Backend configuration/runtime | `backend/docs/operations/configuration-and-runtime.md` |
| BE-MIGRATIONS | Backend migrations/data change | `backend/docs/operations/migrations-and-data-change.md` |
| FE-OVERVIEW | Frontend overview | `frontend/docs/architecture/frontend-overview.md` |
| FE-DEPENDENCIES | Frontend dependencies | `frontend/docs/architecture/dependency-boundaries.md` |
| FE-HOSTS | Hosts/composition/routing | `frontend/docs/architecture/hosts-composition-routing.md` |
| FE-CONTRACTS | Frontend API/contracts | `frontend/docs/architecture/api-and-contracts.md` |
| FE-STATE | Frontend state/query/mutations | `frontend/docs/architecture/state-query-mutations.md` |
| FE-REALTIME | Frontend realtime | `frontend/docs/architecture/realtime.md` |
| FE-UI | Frontend UI/design system | `frontend/docs/architecture/ui-and-design-system.md` |
| FE-TESTING | Frontend tests/gates | `frontend/docs/architecture/testing-and-quality-gates.md` |
| FE-ARCH-CHANGE | Frontend architecture change | `frontend/docs/architecture/architecture-change-policy.md` |
| QLT-ENGINEERING | Engineering quality | `docs/quality/engineering-quality-standard.md` |
| QLT-TESTING | Testing strategy | `docs/quality/testing-strategy.md` |
| QLT-SECURITY | Security quality | `docs/quality/security-quality-standard.md` |
| QLT-ACCESSIBILITY | Accessibility | `docs/quality/accessibility-standard.md` |
| QLT-PERFORMANCE | Performance/scalability | `docs/quality/performance-and-scalability.md` |
| DEL-CLASSIFICATION | Change classification | `docs/delivery/change-classification.md` |
| DEL-MIGRATION | Change impact/migration | `docs/delivery/change-impact-and-migration.md` |
| DEL-DOD | Definition of done | `docs/delivery/definition-of-done.md` |
| DEL-ROLLOUT | Release/rollout/recovery | `docs/delivery/release-rollout-and-recovery.md` |
| OPS-OBSERVABILITY | Observability | `docs/operations/observability.md` |
| OPS-INCIDENT | Incident readiness | `docs/operations/incident-readiness.md` |
| OPS-RECOVERY | Recovery/data safety | `docs/operations/recovery-and-data-safety.md` |
| OPS-DEGRADATION | Service degradation | `docs/operations/service-degradation.md` |
| INFRA-ENVIRONMENT | Environment model | `docs/infrastructure/environment-model.md` |
| INFRA-DEPLOYMENT | Deployment runtime | `docs/infrastructure/deployment-runtime.md` |
| INFRA-CONTAINERS | Containers/local services | `docs/infrastructure/containerization-and-local-services.md` |

---

# 35. Final registry rule

A canonical topic must never depend on readers guessing which file is “more important”.

For every durable topic, this registry should make the answer deterministic:

```text
Topic
→ exactly one owner
→ supporting evidence
→ decision registry
→ routers
```

If two files appear equally entitled to define the same normative topic:

```text
do not add another cross-reference
do not mark both canonical
do not choose by file location
```

Instead:

```text
split the semantic question
or
choose one owner
migrate knowledge
remove competing authority
```

The documentation system is healthy when:

> **a human, Coding Agent, and CI can all resolve the same topic to the same canonical owner.**

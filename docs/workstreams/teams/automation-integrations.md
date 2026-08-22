---
document_id: WRK-TEAM-AUTOMATION-INTEGRATIONS
document_type: workstream-team-spec
status: active
owner: automation-integrations-team
applies_to:
  - automation
  - automation-rules
  - triggers
  - conditions
  - actions
  - automation-executions
  - integrations
  - connectors
  - provider-auth
  - webhooks
  - external-actions
evidence:
  - docs/product/automation.md
  - docs/product/integrations.md
  - docs/architecture/contract-boundaries.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - docs/delivery/team-ownership.md
  - docs/workstreams/capability-map.md
  - docs/workstreams/cross-team-dependencies.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/platform-and-messaging.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/api-and-contracts.md
  - frontend/docs/architecture/api-and-contracts.md
  - frontend/docs/architecture/state-query-mutations.md
  - frontend/docs/architecture/realtime.md
  - frontend/docs/generated/package-boundaries.md
review_on:
  - automation-domain-change
  - trigger-contract-change
  - action-contract-change
  - source-event-change
  - provider-connection-change
  - provider-auth-change
  - webhook-contract-change
  - credential-storage-change
  - execution-retry-change
---

# Automation & Integrations Workstream

## 1. Purpose

This workstream defines execution for the Automation and Integrations bounded contexts.

The team owns two separate business capabilities:

Automation:

- rule definition;
- trigger semantics;
- condition semantics where modeled;
- automation action semantics;
- enable/disable lifecycle;
- execution orchestration;
- execution business state.

Integrations:

- connector/provider catalog;
- connection lifecycle;
- provider authorization;
- configuration;
- external request/operation adaptation;
- inbound webhooks/provider events;
- connection health.

They share one delivery team because they interact frequently, but they MUST NOT become one domain.

## 2. Core separation

Automation answers:

```text
When should a rule run?
What conditions apply?
What action should be requested?
What is the execution state?
```

Integrations answers:

```text
How is an external provider connected?
How is a provider operation invoked?
How is an inbound provider event verified and mapped?
```

Platform answers:

```text
How is a message reliably delivered?
How is retry transport implemented?
How are secrets generically stored?
How is observability transported?
```

Do not collapse these three concerns.

## 3. Explicit non-ownership

Automation does NOT own:

- source bounded-context business state;
- provider SDK clients;
- provider credentials;
- generic message transport;
- Governance policy;
- Billing entitlement calculation.

Integrations does NOT own:

- Automation rule orchestration;
- source-context business state;
- generic outbox/inbox;
- Identity;
- Workspace lifecycle;
- Governance permission semantics.

## 4. Capability decomposition

Automation:

```text
AUT-001 Rule lifecycle
AUT-002 Trigger model
AUT-003 Condition model
AUT-004 Action model
AUT-005 Enable/disable lifecycle
AUT-006 Source-event subscription/matching
AUT-007 Execution creation
AUT-008 Execution state machine
AUT-009 Business retry/failure policy
AUT-010 Execution history/query
AUT-011 Automation authoring frontend
AUT-012 Realtime execution status
AUT-013 Entitlement integration
AUT-014 Hardening/observability
```

Integrations:

```text
INT-001 Connector catalog
INT-002 Connection lifecycle
INT-003 Provider authorization/OAuth
INT-004 Connection configuration
INT-005 Credential reference/storage contract
INT-006 Outbound provider operation
INT-007 Inbound webhook verification
INT-008 Inbound event mapping
INT-009 Connection health
INT-010 Provider failure normalization
INT-011 Integration management frontend
INT-012 Rate/retry/provider limits
INT-013 Entitlement integration
INT-014 Hardening/security
```

Cross-context:

```text
AIX-01 Source event → Automation trigger
AIX-02 Automation action → Integration operation
AIX-03 Integration inbound event → internal fact
AIX-04 Governance authorization
AIX-05 Billing entitlement
AIX-06 Analytics reporting
```

## 5. Delivery waves

### AI Wave A — Automation model

```text
AUT-001 Rule
AUT-002 Trigger
AUT-003 Condition
AUT-004 Action
AUT-005 Enable/disable
```

### AI Wave B — Integration connection foundation

```text
INT-001 Connector catalog
INT-002 Connection
INT-003 Provider auth
INT-004 Configuration
INT-005 Credential contract
```

### AI Wave C — execution pipeline

```text
AUT-006 Event matching
AUT-007 Execution creation
AUT-008 Execution state
AUT-009 Business retry
INT-006 Outbound operation
AIX-02 Automation→Integration
```

### AI Wave D — inbound integration

```text
INT-007 Webhook verification
INT-008 Event mapping
AIX-03 Inbound internal facts
```

### AI Wave E — frontend/observability/hardening

```text
AUT-010 History
AUT-011 Authoring UI
AUT-012 Realtime status
AUT-013 Entitlements
AUT-014 Hardening
INT-009 Health
INT-010 Failure normalization
INT-011 Management UI
INT-012 Provider limits
INT-013 Entitlements
INT-014 Security hardening
```

# Automation model

## 6. Rule lifecycle (AUT-001)

Define:

- create;
- read;
- update;
- delete/archive;
- stable rule identity;
- workspace/account scope;
- owner/actor;
- trigger;
- conditions;
- actions;
- enabled/disabled state.

### Invariants

A rule must:

- belong to one valid account/workspace scope;
- reference supported trigger/action types;
- remain valid when disabled;
- not store raw provider credentials;
- not embed source-context private entities.

## 7. Trigger model (AUT-002)

### Trigger ownership

Automation owns trigger interpretation.

Source context owns source-event meaning.

A trigger may reference:

```text
event type/fact
resource scope
filter/condition input
```

but MUST NOT redefine the source event into an Automation-owned source model.

### Trigger registration

Before adding a trigger:

- producer team identified;
- source event contract stable enough;
- replay/idempotency behavior understood;
- tenant/workspace scope available;
- authorization/entitlement implications known.

## 8. Condition model (AUT-003)

If conditions exist, define:

- supported operands;
- operators;
- value types;
- missing/null behavior;
- source payload availability;
- deterministic evaluation;
- versioning.

Do not allow arbitrary executable code expressions unless explicitly designed and sandboxed.

A condition DSL is a public/persisted contract if rules store it.

Changes require migration/compatibility.

## 9. Action model (AUT-004)

Automation action describes desired business execution, not provider SDK details.

Examples conceptually:

```text
update WorkManagement item
send integration request
create notification
invoke approved connector action
```

Action type must define:

- stable identity;
- input schema;
- validation;
- authorization model;
- idempotency expectation;
- execution owner.

## 10. Enable / disable lifecycle (AUT-005)

Define:

- enable validation;
- disable;
- behavior for already queued executions;
- invalid connection/action on enable;
- re-enable;
- audit/activity;
- entitlement change behavior.

Disabling a rule should not rely solely on frontend state.

# Event consumption

## 11. Source event subscription/matching (AUT-006)

### Producer ownership

Source bounded context owns event.

Automation consumes it.

Do not require source handlers to call Automation internals synchronously merely to make rule evaluation easy.

### Event contract requirements

Automation must understand:

- event ID;
- source type;
- resource ID;
- account/workspace scope;
- event occurrence time;
- version/compatibility;
- payload fields required for trigger evaluation.

### Consumer inventory

Before changing a source event used by Automation:

- Automation must be listed as consumer;
- additive vs breaking classified;
- rollout considered;
- replay/idempotency tested.

## 12. Event replay/idempotency

Automation event consumption must tolerate duplicate delivery according to platform guarantees.

The same source message MUST NOT create duplicate executions unless product semantics explicitly allow it.

Execution identity should be derived from stable:

```text
source message
rule
possibly action index/identity
```

as architecture requires.

## 13. Ordering

Do not assume every automation source requires global ordering.

Classify ordering scope:

- none;
- per resource;
- per rule;
- per workspace;
- another approved scope.

Over-ordering can reduce throughput.

Under-ordering can produce invalid business behavior.

# Execution

## 14. Execution creation (AUT-007)

An execution should capture enough stable information to explain:

- which rule;
- which source event/request;
- which account/workspace;
- which action(s);
- current execution state;
- correlation identity.

Do not persist raw secrets in execution payloads.

## 15. Execution state machine (AUT-008)

Define explicit states.

Conceptually possible states:

```text
pending
running
succeeded
failed
cancelled
retrying
terminal
```

Exact states follow canonical product/source design.

State transitions must be validated.

A transport delivery attempt is not automatically the same thing as an Automation execution state.

## 16. Business retry and failure policy (AUT-009)

Separate:

```text
Platform transport retry
```

from:

```text
Automation business retry
```

### Transport retry

Handles technical delivery failure.

### Business retry

Handles an action execution policy such as provider temporary rejection or action-specific retry.

Define:

- retryable failures;
- terminal failures;
- retry count;
- delay/backoff policy owner;
- idempotency;
- user-visible state;
- cancellation.

Do not stack independent retry layers blindly and create retry storms.

## 17. Poison/terminal failure

If source message or execution becomes terminal:

- state must be observable;
- reason must be safe to expose;
- retries must stop according to policy;
- next unrelated execution must continue;
- one bad message must not poison every event of same type.

# Integrations foundation

## 18. Connector catalog (INT-001)

Catalog defines available connector/provider capability.

It should separate:

```text
provider identity
supported operations
authorization method
configuration metadata
availability/feature flags where applicable
```

Do not hard-code provider catalogs independently across backend/frontend.

## 19. Connection lifecycle (INT-002)

Define:

- create/initiate;
- authorize;
- active;
- invalid/expired;
- reconnect;
- disable;
- delete/revoke;
- ownership scope;
- actor/admin permissions.

Connection state belongs to Integrations.

## 20. Provider authorization / OAuth (INT-003)

Provider authorization must define:

- provider;
- redirect/callback;
- state;
- PKCE where applicable;
- callback expiration;
- workspace/account installation scope;
- actor initiating connection;
- credential/token exchange;
- revoke/reconnect.

### Separation from Identity OAuth

Identity OAuth:

```text
who is the user?
```

Integration OAuth:

```text
which external provider connection is installed?
```

They may use similar protocols but are different business ownership.

Do not reuse Identity linkage semantics blindly for integrations.

## 21. Connection configuration (INT-004)

Define:

- config schema;
- required fields;
- validation;
- secret vs non-secret values;
- provider-specific options;
- migration/versioning;
- frontend form rendering.

Do not store secret fields alongside general public configuration when security architecture provides secret references.

## 22. Credential reference/storage contract (INT-005)

### Ownership

Integrations owns:

- which credential is required;
- connection credential lifecycle meaning.

Platform/Infrastructure may own:

- encryption;
- secret vault/storage mechanism;
- rotation technical mechanism.

### Security requirements

Never:

- log raw access/refresh tokens;
- return secrets to normal read APIs;
- place secrets in Domain events;
- place secrets in Analytics;
- use raw credential material as rate-limit partition identity.

## 23. Secret rotation/revocation

Define:

- provider expiry;
- refresh;
- revoke;
- reconnect;
- failed refresh;
- connection health transition.

Connection should not silently remain "healthy" after credentials are invalid.

# Outbound integration

## 24. External provider operation (INT-006)

An outbound provider action must define:

- connection;
- provider operation;
- input;
- idempotency where provider supports/requires it;
- timeout;
- rate-limit behavior;
- retry classification;
- response mapping;
- failure normalization.

### Provider mapping

Provider payload/status values MUST be mapped into internal Integration semantics.

Do not let provider-specific status enums become cross-system domain vocabulary.

## 25. AIX-02 — Automation action → Integration operation

Correct boundary:

```text
Automation
→ requests an Integration-owned operation contract

Integrations
→ executes provider-specific behavior

Automation
→ records action/execution result
```

Incorrect:

```text
Automation handler
→ instantiate provider SDK directly
```

## 26. Failure mapping

Integrations should normalize failures into stable categories where useful:

```text
authentication/credential
rate limited
temporary provider
invalid request
permission denied
resource missing
terminal provider failure
unknown
```

Exact categories should remain purposeful and not mirror every provider error code.

Automation can then apply business retry policy without embedding provider-specific logic.

# Inbound integration

## 27. Webhook verification (INT-007)

Every inbound webhook must address:

- provider authenticity;
- signature;
- timestamp/replay window where applicable;
- raw body requirements;
- endpoint routing;
- provider connection resolution;
- account/workspace scope;
- idempotency;
- abuse/rate controls;
- observability.

Never process unverified provider events when verification is available/required.

## 28. Replay protection

Provider webhook retries are normal.

The same provider event should not create duplicate internal effects.

Define stable provider-event identity where available.

Combine with provider/connection scope as required.

## 29. Inbound event mapping (INT-008)

Provider webhook payload is transport input.

Map it into an Integration-owned fact or approved downstream command/event.

Do not leak raw provider JSON throughout unrelated bounded contexts.

### Unsupported events

Unknown/unhandled provider events should:

- be safely ignored or recorded according to policy;
- not crash the entire webhook endpoint;
- remain observable.

# Connection health and rate limits

## 30. Connection health (INT-009)

Health may represent:

- connected;
- expired;
- revoked;
- misconfigured;
- provider unavailable;
- degraded.

Exact semantics belong to Integrations.

Do not equate a single HTTP request failure with permanent connection death without classification.

## 31. Provider failure normalization (INT-010)

Failure mapping should be stable enough for:

- UI messaging;
- retry decisions;
- observability;
- support diagnostics.

Sensitive provider payloads should not be exposed to users/logs indiscriminately.

## 32. Provider rate limits (INT-012)

Differentiate:

```text
Notrelix API rate limiting
```

from:

```text
external provider quota/rate limiting
```

Integrations owns provider-specific handling.

Platform may own generic retry/backoff utilities.

### Rate-limit behavior

Define:

- provider response detection;
- retry-after parsing;
- retry policy;
- queue/backpressure;
- user-visible degraded state;
- terminal threshold.

Do not create synchronized retry storms.

# Authorization and tenancy

## 33. Workspace/account scope

Automation rules and Integration connections must live in explicit account/workspace scope according to product semantics.

A connection from Workspace A MUST NOT be usable by Workspace B unless sharing is explicitly supported.

## 34. Governance

Protected operations include:

- create/edit/delete rule;
- enable/disable;
- install connector;
- reconnect;
- update connection;
- execute administrative provider action.

Governance owns policy.

Automation/Integrations own resource/action meaning.

Platform owns enforcement.

## 35. Background execution actor

Background work needs explicit actor/system semantics.

Possible models may include:

- original user actor;
- workspace system actor;
- service principal;
- delegated connection identity.

The team MUST follow canonical architecture.

Do not use "background" as permission to bypass authorization.

# Billing entitlements

## 36. / INT-013 — Entitlement integration (AUT-013)

Billing may gate:

- automation rule count;
- executions;
- premium triggers/actions;
- connectors;
- provider operations;
- advanced history.

Automation/Integrations consume entitlement decisions.

They do not calculate plan rules.

Frontend gating is UX; backend/application enforcement remains required.

# Frontend execution

## 37. Automation authoring UI

Define:

- trigger picker;
- condition builder;
- action builder;
- validation;
- connection requirements;
- permission state;
- entitlement state;
- save/enable flow.

Do not allow frontend-only validation to become canonical rule validity.

## 38. Integration management UI

Define:

- connector catalog;
- connect/auth;
- configuration;
- health;
- reconnect;
- revoke/delete;
- permission/error states.

Do not expose provider secrets after connection establishment.

## 39. Query keys and state

Queries must distinguish:

- account;
- workspace;
- rule;
- execution;
- connection/provider.

Account/workspace transitions must clear or partition state safely.

## 40. Realtime execution status

If execution status is realtime:

- subscription scope;
- execution identity;
- duplicate/out-of-order behavior;
- reconnect/gap recovery

must be defined.

A stale execution UI must not trigger duplicate execution.

# Events and messaging

## 41. Producer/consumer ownership

Source event:

```text
owned by source bounded context
```

Automation trigger consumption:

```text
owned by Automation
```

Delivery:

```text
owned by Platform
```

Integration provider event:

```text
owned/mapped by Integrations
```

Downstream internal fact:

```text
owned by appropriate receiving context
```

## 42. No synchronous hidden coupling

Avoid:

```text
WorkManagement transaction
→ directly run Automation
→ call provider
→ block original transaction
```

unless an explicit core invariant requires synchronous behavior.

Normal automation should decouple source transaction from external provider failure.

## 43. Delivery identity

Automation execution must correlate:

- source message/request;
- rule;
- action;
- connection;
- execution.

Correlation should support supportability without leaking secret data.

# Data ownership

## 44. Automation persistence

Automation owns:

- rules;
- trigger/condition/action configuration;
- execution domain state/history as canonical.

## 45. Integrations persistence

Integrations owns:

- connection;
- provider config;
- credential reference;
- provider metadata;
- health state.

## 46. Source persistence

Automation/Integrations MUST NOT mutate source-context private tables.

Use:

- source commands/contracts;
- events;
- approved APIs.

# Migration and compatibility

## 47. Persisted rule schema

Changing trigger/condition/action schema affects stored rules.

Define:

- version;
- old schema;
- migration;
- invalid legacy rules;
- disable/fail-safe behavior;
- rollback.

Do not silently reinterpret old rules after a type schema change.

## 48. Provider configuration migration

Changing connection config requires:

- versioning;
- default mapping;
- secret reference migration;
- invalid config handling;
- frontend compatibility.

## 49. Event-contract migration

Breaking source events consumed by Automation require producer/consumer coordination.

If deployment is not atomic, define compatibility window.

## 50. Credential migration

Credential migrations are security-sensitive.

Define:

- encryption/storage transition;
- rotation;
- rollback;
- no raw secret logging;
- old credential invalidation.

# Observability

## 51. Automation observability

Record, where safe:

- rule;
- execution;
- source event/request;
- action;
- state transition;
- retry;
- terminal failure;
- latency.

## 52. Integration observability

Record, where safe:

- provider;
- connection ID;
- operation;
- response category;
- retry/rate limit;
- webhook verification result;
- latency.

Never log secrets.

## 53. Correlation

Critical flow should be traceable:

```text
source event
→ delivery
→ rule match
→ execution
→ integration operation
→ provider result
→ execution final state
```

# Testing

## 54. Automation Domain tests

Cover:

- rule lifecycle;
- valid/invalid trigger;
- condition evaluation;
- action validation;
- enable/disable;
- execution state transitions;
- business retry semantics.

## 55. Automation Application tests

Cover:

- event matching;
- duplicate event;
- authorization;
- entitlement;
- execution creation;
- retry classification;
- invalid/missing connection.

## 56. Integration Domain/Application tests

Cover:

- connection lifecycle;
- config validation;
- provider auth transitions;
- health transitions;
- outbound operation mapping;
- inbound event mapping.

## 57. Infrastructure tests

Cover:

- credential persistence/reference;
- provider adapter;
- webhook verification;
- replay/idempotency;
- rate-limit handling;
- provider error normalization;
- migrations.

## 58. API tests

Cover:

- rule CRUD;
- connection CRUD;
- provider callback;
- webhook;
- authorization;
- CSRF where browser mutations apply;
- OpenAPI/contract.

## 59. Frontend tests

Automation:

- authoring;
- validation;
- connection requirement;
- enable/disable;
- execution history/status.

Integrations:

- catalog;
- connect;
- callback;
- config;
- health;
- reconnect/revoke.

## 60. Critical integration/E2E

Primary happy path:

```text
WorkManagement event
→ delivered once effectively
→ Automation matches rule
→ execution created
→ Integration invokes provider
→ execution succeeds
→ UI observes final status
```

Failure paths:

```text
duplicate source event
→ no duplicate execution

provider temporary failure
→ defined business retry

provider credential revoked
→ connection unhealthy
→ action fails safely

webhook replay
→ no duplicate internal effect

unauthorized user
→ cannot install/change rule/connection
```

# Performance and reliability

## 61. Throughput model

Measure:

- events/sec;
- rule matches/event;
- executions/sec;
- external calls/sec;
- queue depth;
- retry volume;
- provider rate-limit rate;
- execution latency.

## 62. Fanout

One source event may match many rules.

Define safeguards against:

- unbounded fanout;
- recursive automation loops;
- provider storm;
- retry explosion.

## 63. Automation loops

If Automation actions can produce events that retrigger rules, define:

- loop detection/prevention;
- execution depth;
- correlation;
- max chain;
- explicit allow cases if product-defined.

Do not rely on "users probably won't configure loops".

## 64. Provider backpressure

When provider is degraded/rate-limited:

- queue should not grow without visibility;
- retry should respect provider limits;
- unrelated providers should not be globally blocked;
- terminal/degraded state should be observable.

# Delivery governance

## 65. Dependency readiness matrix

| Capability | Upstream readiness |
|---|---|
| Rule lifecycle | Workspace/authz D5 |
| Trigger model | source event contract D4+ |
| Condition model | trigger payload contract D4+ |
| Action model | execution-owner contract D4+ |
| Event matching | Platform messaging D5 |
| Execution state | idempotency/message identity D4+ |
| Integration connection | Identity/Workspace/Governance D5 |
| Provider OAuth | session/security foundation D5 |
| Outbound operation | connection/credential D4+ |
| Webhook | provider verification/idempotency D4+ |
| Realtime status | Platform realtime D4+ |
| Entitlements | Billing contract D4+ |

## 66. Safe parallelization

After rule/action/connection contracts stabilize, parallel work can include:

- authoring frontend;
- provider adapters;
- execution history;
- connection management UI;
- inbound webhook adapters.

## 67. Unsafe parallelization

Do not let provider teams independently invent:

- connection states;
- credential storage;
- retry semantics;
- webhook idempotency;
- provider error models;
- Automation action contracts.

## 68. Cross-team handoff template

For a source-event integration:

```text
Source context:
Event:
Semantic owner:
Automation trigger:
Required payload:
Ordering:
Idempotency:
Current readiness:
Required readiness:
Compatibility:
Tests:
```

For Automation→Integration:

```text
Action type:
Automation owner:
Integration operation:
Connection requirement:
Credential requirement:
Retry classification:
Idempotency:
Provider failure mapping:
Tests:
```

# Decision authority

## 69. Team-local decisions

May decide locally:

- private rule evaluator decomposition;
- provider adapter internals;
- UI component structure;
- local retry implementation preserving approved policy;
- test fixtures;
- performance optimization preserving contracts.

## 70. Decisions requiring escalation

Escalate:

- new expression execution/scripting engine;
- new secret-storage architecture;
- source-event breaking change;
- new global message-delivery semantics;
- background actor model change;
- new cross-context command path;
- provider SDK introduced as repository-wide dependency;
- service extraction;
- arbitrary code execution in rules;
- new cross-cutting workflow engine.

## 71. Stop conditions

Stop when:

- source-event owner/meaning is unclear;
- trigger needs private source data unavailable by contract;
- action needs direct source DB access;
- credential handling requires raw secret exposure;
- provider webhook cannot be verified safely;
- retry layers can cause duplicate external effects;
- background authorization model is undefined;
- Automation loop behavior is undefined;
- persisted rule schema changes without migration;
- provider-specific logic is leaking into Automation domain;
- service split is proposed merely to simplify team ownership.

# Completion criteria

## 72. Capability Definition of Done

A slice is `DONE` only when:

- context ownership is explicit;
- trigger/action/connection contracts are stable enough;
- event consumption is idempotent;
- authorization is server-enforced;
- credentials remain secret-safe;
- retry/failure semantics are defined;
- provider mapping is isolated;
- tenant/workspace scope is preserved;
- migrations are handled;
- observability exists;
- tests prove critical success/failure paths;
- architecture gates remain green.

## 73. Automation foundation exit criteria

Automation is ready for broad feature delivery when:

- rule/trigger/action contracts are stable;
- source event consumption is D5;
- execution identity/state is stable;
- duplicate delivery cannot create duplicate logical executions;
- business retry is separated from transport retry;
- authoring UI consumes canonical rule schema;
- entitlement/authz integration is explicit.

## 74. Integrations foundation exit criteria

Integrations is ready for broad connector delivery when:

- connection lifecycle is stable;
- credential storage/reference is D5;
- provider OAuth/callback contract is stable;
- outbound operation contract is stable;
- webhook verification/idempotency is D5;
- provider failure normalization exists;
- health/reconnect semantics are defined;
- connector teams can add providers without redefining platform contracts.

## 75. Service extraction readiness

Automation and Integrations remain separate future extraction candidates.

Before Automation extraction prove:

- stable source-event contracts;
- independent rule/execution data;
- messaging/idempotency guarantees;
- no private source DB access;
- clear background authz;
- operational throughput need.

Before Integrations extraction prove:

- independent connection/credential ownership;
- provider callback routing;
- secret-management boundary;
- webhook reliability;
- outbound operation contract;
- no Automation internal dependency.

One team owning both is not a reason to deploy them as one service.

# Notrelix Domain Rulebook

This rulebook is binding for `backend/src/Notrelix.Domain`. It translates the
enterprise hardening plan into rules that Application, Infrastructure, API, and
future agents must not invent ad hoc.

## What Domain May Contain

Domain may contain business concepts that have long-lived meaning inside
Notrelix:

- aggregate roots with lifecycle and invariants, such as `Workspace`, `Board`,
  `BoardItem`, `BoardField`, `Page`, `Block`, `Comment`, `Subscription`, and
  `Entitlement`;
- child entities whose lifecycle is owned by an aggregate, such as field
  options, board item values, role permissions, and execution steps;
- value objects that validate and compare by value, such as `Email`,
  `ResourceRef`, `FeatureCode`, `Money`, `FractionalIndex`, and `JsonValue`;
- domain events that describe meaningful business facts, not delivery details;
- pure rules, policies, and domain services that require no repository,
  provider, clock, HTTP, Redis, message broker, or database access;
- domain exceptions and guard helpers.

## What Domain Must Not Contain

Domain must not reference or model infrastructure concerns:

- EF Core, DbContext, migrations, query includes, repositories, or persistence
  mapping;
- HTTP, controllers, endpoints, request/response DTOs, cookies, JWT, or API
  problem details;
- Redis, MassTransit, background workers, provider SDKs, storage clients,
  SMTP, search clients, or schedulers;
- outbox messages, processed event records, idempotency keys, job locks,
  worker retry state, permission caches, or search index documents;
- Application command/query handlers, validators, authorization pipeline logic,
  cache invalidation, realtime delivery, or transaction orchestration.

Domain may expose facts and rules that outer layers use. Outer layers may load
data and pass primitive facts or reference objects into Domain. Domain must not
load those facts itself.

## When To Create An Aggregate Root

Create an aggregate root when a concept has an independent lifecycle, direct use
cases, business invariants, state transitions, and persistence/loading needs.

Notrelix examples:

| Aggregate root | Reason |
|---|---|
| `Workspace` | Tenant root, lifecycle, settings, archive/delete state. |
| `WorkspaceMember` | Membership lifecycle and last-owner protection. |
| `WorkspaceInvitation` | Invite token lifecycle and expiry/accept/revoke transitions. |
| `Board` | Board metadata, visibility, default group, item identity sequence. |
| `BoardItem` | Item lifecycle, field values, parent relation, group placement. |
| `BoardField` | Dynamic schema definition, options, classification, formula config. |
| `Page` | Document tree metadata and page lifecycle. |
| `Block` | Independently edited document content unit. |
| `Comment` | Comment lifecycle and target validation. |
| `ResourcePermission` | Permission grant lifecycle for one target/subject pair. |
| `Subscription` | Billing subscription lifecycle. |
| `Entitlement` | Workspace feature access lifecycle. |

Do not create an aggregate root only because a table exists. A table can be a
projection, ops record, cache, or persistence detail.

## When To Create An Entity

Create an entity when the object has identity but its lifecycle is owned by a
parent aggregate or context policy.

Examples:

- `BoardItemValue` is owned by `BoardItem`.
- `FieldOption` is owned by `BoardField`.
- `CustomRolePermission` is owned by `CustomRole`.
- `AutomationExecutionStep` is owned by `AutomationExecution`.
- `AuditLog` may be an append-only entity, but it is not a mutable business
  aggregate.

Child entities must not be mutated directly by Application handlers. Mutations
must go through the aggregate root or a documented pure domain policy.

## When To Create A Value Object

Create a value object when the concept has no independent identity, validates a
business shape, and is equal by value.

Examples:

- `Email`, `Url`, `Color`, `Money`, `FeatureCode`, `PermissionScope`,
  `ResourceRef`, `TokenHash`, `SecretRef`, `DocumentSnapshot`.

Value objects should be immutable from the public API. Provider payloads,
request DTOs, and database JSON blobs are not value objects unless they enforce
domain semantics.

## When To Keep A Model Out Of Domain

Keep a model out of Domain when it exists to serve delivery, runtime, reporting,
or operational needs:

- search documents and index jobs;
- outbox messages and processed-event records;
- idempotency keys and job locks;
- permission inheritance caches;
- unread counters when used only as denormalized inbox state;
- presence sessions when used only as connection/runtime state;
- analytics snapshots when rebuildable from source data;
- provider webhook retry records and worker cursors.

If such a model has user-facing lifecycle and invariants, document the reason in
the bounded-context rulebook before treating it as Domain.

## Bounded Context Communication

Bounded contexts communicate through IDs, value objects, `ResourceRef`, and
domain events. They must not require full aggregate object references across
contexts.

Allowed examples:

- WorkManagement references a workspace by `WorkspaceId`.
- Collaboration targets a board item or page through `ResourceRef`.
- Governance protects resources through registered `ResourceType` values.
- Billing references a workspace by `WorkspaceId` and a plan by `PlanId`.

Forbidden examples:

- `Comment` receiving a full `BoardItem` or `Page` aggregate.
- Billing storing a payment-provider SDK model.
- Automation executing webhooks inside an aggregate method.
- Search updating index documents inside core aggregate mutations.

## Preparing Domain For Application Use Cases

Before Application implements a command/query, the Domain must already answer:

1. Which bounded context owns the use case?
2. Which aggregate owns the invariant?
3. Is the target global, workspace root, workspace-scoped, or system-owned?
4. Which domain method or pure policy is called?
5. Which state transition happens?
6. Does version increment?
7. Is a domain event emitted, audit-only, activity-only, or no-event?
8. Is a side effect Application/Infrastructure responsibility?
9. Does `ResourceRef` need a registered `ResourceType`?
10. Is behavior allowed while archived or after soft delete?
11. Which pure Domain test proves this behavior?

If these answers are missing, Application work must stop and Domain hardening
must happen first.

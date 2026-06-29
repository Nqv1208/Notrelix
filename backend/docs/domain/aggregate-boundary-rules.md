# Aggregate Boundary Rules

Aggregate boundaries define where Notrelix enforces transactional consistency.
They are not folder boundaries and they are not database table boundaries.

## Aggregate Ownership

An aggregate owns:

- the state it can change atomically;
- the invariants it can enforce from its own state and supplied facts;
- child entities whose lifecycle is subordinate to the root;
- domain events that describe changes inside that consistency boundary.

Examples:

| Aggregate | Owns | Does not own |
|---|---|---|
| `Workspace` | name, slug, settings, account assignment, archive/delete state | members, invitations, boards, pages, billing state |
| `WorkspaceMember` | member role/status and last-owner checks from supplied owner count | user identity or workspace metadata |
| `Board` | board metadata, visibility, default group pointer, item sequence | item values, fields, views, comments |
| `BoardItem` | item name, group placement, position, parent, timeline, completion, field values | field definitions or board metadata |
| `BoardField` | field schema, options, settings, formula/classification flags | item values or formula execution runtime |
| `Page` | page tree metadata, title, visibility/status | realtime editor operations or block content snapshots |
| `Block` | block type/content/properties/order | page tree policy beyond supplied parent facts |
| `Comment` | comment content/status/target | target aggregate lifecycle or notification delivery |
| `Subscription` | subscription status, plan, period, cancellation flag | payment provider details or webhook idempotency store |
| `Entitlement` | workspace feature limit/status/expiry | usage aggregation or billing provider state |

## Invariant Ownership

Put an invariant in the aggregate that owns the state being protected.

- `BoardItem.UpdateFieldValue` may validate a supplied `BoardField` reference
  because both are in WorkManagement and field definition controls item value
  validity.
- `WorkspaceMember.ChangeRole` receives `activeOwnerCount` from Application and
  calls a pure owner rule because the count spans multiple members.
- `Comment.Create` validates target workspace through `ResourceRef`; it must not
  load the target resource.
- `Subscription.ChangePlan` rejects canceled/expired state inside the aggregate;
  provider payment checks stay outside Domain.

Cross-aggregate data must be passed in as primitive facts, reference objects, or
small pure policy inputs. Domain must not query repositories.

## Child Entity Mutation Policy

Application must not mutate child entity state directly. It must call the
aggregate root method that owns the invariant.

Allowed:

```csharp
boardField.AddOption("Done", color, position, actorId, now);
boardItem.UpdateFieldValue(field, value, actorId, now);
customRole.AddPermission("board.update", actorId, now);
```

Forbidden:

```csharp
field.Options.Add(option);
item.FieldValues.First().UpdateValue(value);
role.Permissions.Remove(permission);
```

If a child entity needs independent use cases, concurrency, or loading, promote
it to an aggregate root and document the boundary change first.

## Cross-Aggregate Reference Policy

Aggregate roots reference other aggregate roots by ID or value object.

Allowed:

- `Guid WorkspaceId`
- `Guid BoardId`
- `Guid PageId`
- `ResourceRef Target`
- `FeatureCode Feature`
- `SecretRef TokenSecretRef`

Avoid full aggregate object references across contexts. Full aggregate
parameters inside one context are allowed only when documented as a pure policy
input and kept narrow.

Current accepted same-context example:

- `BoardItem.UpdateFieldValue(BoardField field, FieldValue newValue, ...)`.

Risk to audit later:

- If `BoardField` grows large or loads unrelated state, replace this with a
  smaller `BoardFieldRef` or field-definition policy object.

## When To Use A Domain Service

Use a pure Domain service or policy when an invariant spans multiple aggregates
but remains business logic.

Use a Domain service for:

- hierarchy cycle validation when Application supplies ancestor facts;
- permission precedence rules once Application supplies candidate grants;
- formula syntax/reference validation without executing queries;
- last-owner rules using owner count loaded by Application.

Do not use a Domain service for:

- loading state from a database;
- checking current user permissions from HTTP context;
- publishing messages;
- calling providers;
- cache invalidation;
- background job orchestration.

## IDs Instead Of Object References

Prefer IDs when:

- only identity is needed;
- the referenced aggregate is in another bounded context;
- the reference is polymorphic and belongs in `ResourceRef`;
- loading the full object would tempt Domain to inspect unrelated state.

Use a richer reference object when the aggregate needs a small immutable fact
set:

- `BoardGroupRef` for `WorkspaceId`, `BoardId`, and `GroupId`;
- future `BoardFieldRef` for field identity/type/options if field validation is
  extracted;
- future `RegisteredResourceRef` if `ResourceRef` capability checks are
  enforced inside Domain.

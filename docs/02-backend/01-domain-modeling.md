---
title: "Domain Modeling and Mutation Contract"
document_class: handbook
normative: true
owner: backend-domain
maturity: FROZEN
conformance: CANONICAL
applies_to: backend/domain
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Domain Modeling and Mutation Contract

The Domain project protects business invariants and deterministic state transitions. It is not a persistence model, validation helper library or DTO namespace.

## Aggregate selection

### BE-DOM-101 — Aggregate root = transactional consistency boundary

**Rule.** Promote a type to aggregate root when it must independently protect invariants/lifecycle under concurrent commands. Table existence, public ID or event emission is supporting evidence—not sufficient by itself.

**Supporting evidence.** Independent lifecycle, commands, loading/concurrency needs, stable identity, event consumers.

**Forbidden.** Making every table an aggregate; putting all workspace resources inside `Workspace` for convenience.

**Proof.** Domain model review + concurrency/behavior tests.

### BE-DOM-102 — Cross-root references use identity/immutable facts

A root may reference another root by stable ID or immutable context snapshot. It MUST NOT hold a mutable navigation object to another aggregate as a way to bypass its boundary.

## Entity/value semantics

Owned entities exist only inside their root consistency boundary and do not expose mutation paths that bypass the root. Value objects are immutable, validated at construction, deterministic in equality/comparison and copy caller-owned collections when needed.

## Mutation protocol

### BE-DOM-MUT-101 — Validate before commit

For a meaningful mutation:

```text
1. validate lifecycle
2. validate actor/required IDs
3. normalize input
4. validate owned business invariants
5. load/accept approved external facts already supplied by Application
6. detect semantic no-op
7. construct prospective child/value state without attaching
8. prepare audit update without applying it
9. mutate business state
10. apply audit
11. increment Version exactly once
12. raise approved Domain event(s)
```

Ordering may vary only when equivalently safe; no fallible validation may occur after irreversible in-memory mutation unless rollback is explicit and proven.

### BE-DOM-MUT-102 — Failure atomicity

A rejected mutation MUST leave unchanged:

```text
business fields
lifecycle/deletion state
owned entities/collections
audit state
Version
pending DomainEvents
```

**Forbidden example.** Add a child to a collection and then call a validation/audit method that can throw.

**Proof.** Before/after equality scenario tests including version and event collection.

### BE-DOM-MUT-103 — Semantic no-op is side-effect free

A semantic no-op does not change state, audit, version or events and does not attach/remove/mutate children. Required actor/lifecycle/business preconditions may still be validated, but stale timestamps alone must not make an otherwise valid no-op fail when timestamp is irrelevant.

### BE-DOM-MUT-104 — Version changes exactly once per successful meaningful mutation

Creation follows the established initial-version strategy. A successful state-changing command increments once; rejected/no-op operations do not increment. Infrastructure maps Domain version to optimistic-concurrency enforcement.

## External facts

### BE-DOM-201 — Domain receives facts; it does not fetch them

Application supplies:

- current actor/time;
- parent/ancestor path;
- cross-aggregate counts or uniqueness precheck facts;
- authorization/entitlement result when Domain needs a business fact rather than policy service;
- approved random/generated input where business identity requires it.

Domain MUST NOT receive repository callbacks, DbContext, HTTP/service-provider handles or provider clients.

## Hierarchy

For parent/ancestor invariants:

```text
Application loads parent/ancestor facts
→ builds immutable ParentPath/AncestorPath/context
→ Domain validates tenant/cycle/depth rules
→ Domain derives stored level/depth if needed
→ aggregate mutates
```

Do not accept both parent ID and caller-computed depth/level as independent truth.

## Domain events

### BE-DOM-EVT-101 — Event = completed business fact

Event is raised only after successful mutation, absent for no-op/rejection, uses normalized persisted values and correct tenant scope. Event name is past-tense business semantics and logical identity is stable independently of CLR refactor.

### BE-DOM-EVT-102 — Event is justified by consumption contract

Use a Domain event when the fact is consumed by another context, outbox/integration mapping, activity/audit/realtime projection or independent read model. Do not emit an event merely because every method “should have one”. Internal root-owned mutation may explicitly have no event.

### BE-DOM-EVT-103 — Event payload is safe and immutable

Copy caller-owned collections, include stable identities/scope needed by consumers, and never carry raw secrets/tokens/provider clients/full mutable entities.

## Determinism

### BE-DOM-301 — No ambient nondeterminism

Domain must not call `DateTime.Now/UtcNow`, `Random.Shared`, current culture, filesystem/network/provider I/O or environment-dependent business behavior. Application supplies time/identity/random/external facts.

Use ordinal/explicit comparison semantics for identifiers/keys unless a business-specific culture rule exists.

## Identity

Follow existing identity strategy. Typed identity is justified when it protects aggregate/public-contract correctness; do not create a wrapper type for every persistence row mechanically. Do not introduce ad-hoc `Guid.NewGuid()` when the context has an established factory/strategy.

## Lifecycle/deletion

### BE-DOM-LIFE-101 — Generic soft delete is not universal lifecycle

Every aggregate declares the relevant lifecycle pattern: recoverable delete, archive, business termination/cancel/revoke, append-only, owned removal, tombstone or deletion unsupported.

A recoverable deletion mechanism must not secretly rewrite business status. Use explicit verbs: Archive/Unarchive, Revoke/Expire, Cancel/Renew, Suspend/Activate, Resolve/Reopen, Remove.

Append-only audit/commercial/usage facts do not receive generic Delete/Restore merely for framework symmetry.

## Configuration/value payloads

Typed closed configuration uses strong types, validation, immutability and deterministic equality. Persisted polymorphic/opaque JSON requires explicit discriminator/schema-version/object-root validation/unknown-type handling and persistence round-trip tests when evolution requires it.

## Testing contract

For every critical aggregate mutation cover:

```text
success
rejection with unchanged state
semantic no-op
version
DomainEvents
lifecycle guard
tenant/cross-reference guard
boundary values/normalization
```

These tests prove Domain semantics, not EF mapping or handler orchestration.

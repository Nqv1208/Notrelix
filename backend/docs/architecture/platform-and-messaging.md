# Platform and Messaging

## Scope

Reusable delivery mechanisms, message envelopes, outbox, idempotency,
post-commit work, consumers, background jobs, retry/dead-letter behavior,
ordering, poison detection and observability.

## Responsibility / Ownership

Platform owns reusable runtime mechanisms. Product/Application code decides
business meaning; Platform ensures reliable, scoped delivery.

## Current Architecture

Platform source lives in `backend/src/Notrelix.Platform` with tests in
`Notrelix.Platform.Tests` and integration coverage for production-graph flows.

## Normative Contracts

- Messages have stable identity, producer identity, consumer identity, tenant
  scope where applicable and deterministic dedup keys.
- Outbox enrollment belongs to the same commit as the state change.
- Post-commit work runs only after successful transaction commit.
- Consumer idempotency state is keyed by message and consumer identity.
- Ordering/sequence state is explicit when a workflow depends on order.
- Poison detection distinguishes deterministic invalid messages from transient
  failures.
- Retry and dead-letter behavior must preserve observability and dedup state.
- Background jobs/consumers execute with explicit tenant/RLS context when
  reading or writing tenant data.
- Observability records message identity, consumer, retry/dead-letter status,
  tenant/resource scope where safe and correlation IDs.

## Allowed Design

- Generic envelope, outbox, idempotency and post-commit primitives.
- Application-supplied handlers that keep business decisions outside Platform.
- Integration tests that exercise commit plus delivery behavior.

## Forbidden Design

- Business policy in Platform because many contexts call it.
- Publishing before the state change commits.
- Consumer retry loops without idempotency.
- Tenant-scoped background work without explicit tenant context.
- Dead-letter records that omit enough identity for diagnosis.

## Failure Modes

- Duplicate messages cause duplicate side effects.
- A post-commit handler observes uncommitted or rolled-back state.
- Poison messages retry forever.
- Background delivery bypasses RLS/session context.

## Change Impact Rules

Envelope identity, outbox claim/delivery, idempotency, ordering, retry,
dead-letter or tenant execution context changes require Platform tests and at
least one production-graph integration proof.

## Executable Evidence / Tests / Gates

- `backend/src/Notrelix.Platform`
- `backend/tests/Notrelix.Platform.Tests`
- `backend/tests/Notrelix.Integration.Tests`

## Related ADRs

See `../decisions/README.md`.

## Related Source Manifests

`backend/backend.slnx`.

## Non-responsibilities

Platform does not define Domain events, use-case authorization, endpoint shapes
or provider business semantics.

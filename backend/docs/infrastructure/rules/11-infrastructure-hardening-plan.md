# 11 — Infrastructure Hardening Plan

This file lists remaining Infrastructure weaknesses observed during review and the recommended fixes.

## P0/P1 — Consumer idempotency mechanism conflict

### Problem

There are signs of multiple idempotency mechanisms:

- `DeduplicationConsumeFilter`
- `ConsumerPipelineExecutor`
- possible manual dedup in handlers/consumers

Multiple mechanisms can cause:

- double transaction;
- duplicate processed event insert;
- inconsistent consumer names;
- some consumers bypassing the selected mechanism;
- false confidence in idempotency.

### Decision needed

Choose exactly one active mechanism.

Recommended for current MassTransit setup:

```txt
Use MassTransit filter-level idempotency if filter fully owns:
- tenant context or runs after tenant filter;
- transaction;
- processed event check;
- mark processed after successful consumer;
- rollback on failure.
```

If filter cannot own those semantics, use `ConsumerPipelineExecutor` and remove dedup filter from MassTransit pipeline.

### Required actions

1. Write ADR: `docs/adr/ADR-XXX-consumer-idempotency.md`.
2. Remove inactive mechanism.
3. Remove manual dedup from handlers.
4. Add tests for duplicate and concurrent duplicate delivery.

---

## P1 — PermissionVersionProvider does not include account boundary

### Problem

`IPermissionVersionProvider.GetVersionAsync(accountId, workspaceId, userId)` receives `accountId`, but current SQL/version string may not use it.

### Risk

Cache version is less explicit than cache key dimensions. If workspace IDs are globally unique, risk is lower, but defense-in-depth is weak.

### Fix

- Include `accountId` in version string.
- Add account condition if permission tables include account boundary or join workspace to account.
- Add tests:
  - version differs by user;
  - version differs by account/workspace;
  - membership change changes version;
  - role assignment change changes version;
  - resource permission change changes version.

---

## P1 — Concurrency must fail fast

### Problem

If `IExpectedVersionRequest` cannot be verified, system must not silently continue.

### Rule

```txt
IExpectedVersionRequest means concurrency is required.
If unsupported resource type/current version missing/invalid expected version -> fail fast.
```

### Fix

- Unsupported resource type throws security misconfiguration or not supported exception.
- ExpectedVersion <= 0 throws validation/security misconfiguration.
- Current version null returns not found/conflict according to use case semantics.
- Tests updated to expect failure, not warning+continue.

---

## P1 — RULE.md and docs drift

### Problem

Application docs may be updated while Infrastructure rules remain implicit. Coding Agent can create wrong files/mechanisms.

### Fix

- Merge `RULE-infrastructure-layer-patch.md` into `backend/RULE.md`.
- Add this docs folder under `backend/docs/infrastructure/`.
- Add ADRs for selected idempotency mechanism and RLS/system context usage.

---

## P1 — Raw SQL and IgnoreQueryFilters need allowlist

### Problem

Infrastructure legitimately needs raw SQL and `IgnoreQueryFilters` in some places. Without allowlist, future code may bypass tenant/RLS accidentally.

### Fix

- Create architecture test scanning for raw SQL and `IgnoreQueryFilters` usage.
- Allowlist only approved services.
- Require comment/doc for every allowlisted usage.
- Add integration test for tenant isolation around allowlisted paths.

---

## P2 — ConsumerName stability

### Problem

If consumer name is derived from endpoint address, renaming endpoint changes idempotency key.

### Fix

- Prefer explicit consumer name constant or full consumer type name.
- Document endpoint-name-derived consumer names if kept.
- Test that same consumer name is used across retries.

---

## P2 — Monolithic ApplicationDbContext blast radius

### Problem

Single DbContext is practical now, but it maps many bounded contexts.

### Risk

- cross-context query coupling;
- large migration blast radius;
- accidental dependency from one module into another.

### Fix now

- Keep single DbContext.
- Enforce handlers use bounded-context interfaces.
- Architecture tests forbid injecting `ApplicationDbContext` outside Infrastructure/tests.
- Review cross-context queries carefully.

### Future option

Split DbContext per bounded context only when module boundaries and transaction/SAGA strategy are mature.

---

## P2 — Options validation coverage

### Problem

Some registrations validate options well; others may not.

### Fix

- Add tests for every options validator.
- Require `ValidateOnStart()` for production-critical options.
- Block dev-null providers outside Development.

---

## P2 — External side-effect durability classification

### Problem

Email/realtime/webhook/storage side effects have different reliability requirements.

### Fix

Classify each side effect:

```txt
Best-effort post-commit: realtime notification, non-critical UI refresh.
Durable outbox: webhook, billing sync, integration sync, important email.
Immediate technical operation: storage upload/delete if part of use case, with compensation plan.
```

Document in `docs/infrastructure/external-side-effects.md` if needed.

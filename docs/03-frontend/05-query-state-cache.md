---
title: "Query, State and Cache Contract"
document_class: handbook
normative: true
owner: frontend-architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: frontend
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Query, State and Cache Contract

Server state is authoritative outside the client. Frontend state management exists to cache, compose and present server truth safely across account/workspace scopes; it must not create a second competing business database.

## FE-STATE-101 — One authoritative owner per server resource

Each server-backed resource has one capability state owner responsible for query keys, fetch/query options, mutations, normalization and invalidation/patch policy. Components and apps consume that owner instead of inventing parallel keys or direct fetches.

## FE-STATE-102 — Query keys encode security/tenant scope

Canonical shapes are conceptually:

```text
["global", resource, ...identity]
["account", accountId, resource, ...identity]
["workspace", workspaceId, resource, ...identity]
```

The exact helper API lives in the Foundation/query package, but the semantic requirement is invariant: every tenant-scoped cache entry must include the authoritative scope identity. An account/workspace id MUST NOT be omitted because the URL or current-session singleton “already knows it”.

**Proof:** query-key tests plus architecture/review checks; transition integration tests for high-risk resources.

## FE-STATE-103 — Mutations update client state only after server success

Optimistic UI is allowed only where rollback/conflict semantics are explicit. Default mutation flow is:

```text
validate local input needed for UX
→ invoke generated/owned API adapter
→ receive authoritative success/version
→ patch narrowly or invalidate owned keys
→ let realtime reconcile complementary observers
```

Do not update canonical cached business state before authorization/concurrency success and leave it mutated when the server rejects.

## FE-STATE-104 — Patch vs invalidate is an ownership decision

Patch when the response contains sufficient authoritative state and every affected key can be enumerated safely. Invalidate when server-side projection/permissions/order can affect unknown observers. Never copy a mutation result into caches owned by another capability without that owner's public updater/contract.

## FE-TEN-101 — Scope transition is a protocol, not a variable assignment

Changing active account/workspace must:
1. stop/dispose old-scope realtime subscriptions and in-flight observers as owned;
2. prevent old-scope responses from becoming active new-scope UI;
3. clear/remove or quarantine old scope-sensitive cache according to cache policy;
4. update authoritative session/scope identity;
5. create new-scope query/realtime consumers;
6. only then render child scope content as ready.

A stale response arriving after transition MUST NOT overwrite new-scope state.

## FE-STATE-105 — Client-only state is separated from server cache

Ephemeral UI state (open panels, local drafts before submission, selection, gesture state) can use local stores/hooks. It must not duplicate authoritative server entities merely to avoid query usage. Persisted local preferences need explicit scope/version/migration semantics.

## Failure/concurrency

409/412-style concurrency conflict, authorization changes, not-found and validation errors are distinct states. Mutation layers must preserve enough typed error information for the UI to present correct recovery, rather than flattening every failure to “Something went wrong”.

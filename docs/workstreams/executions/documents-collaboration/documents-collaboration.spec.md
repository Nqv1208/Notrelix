---
document_id: WRK-SPEC-DOCUMENTS-COLLABORATION
document_type: workstream-specification
status: active
owner: documents-collaboration-team
applies_to: [backend, documents, collaboration, p4b, pages, blocks, comments, realtime]
evidence:
  - docs/product/documents.md
  - docs/product/collaboration.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/teams/documents-collaboration.md
  - docs/workstreams/cross-team-dependencies.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/platform-and-messaging.md
review_on: [p4b-scope-change, document-model-change, comment-target-change, realtime-contract-change]
---

# SPEC — P4B Documents & Collaboration

## 1. Objective

P4B delivers document authoring and resource-scoped collaboration while preserving two distinct semantic owners:

```text
Documents owns Page/Block/document state.
Collaboration owns Comment/collaboration state.
```

Neither context may absorb the other's private persistence.

## 2. Entry gate

| Contract | Required |
|---|---|
| Actor/Account | D5 |
| Workspace | D5 |
| Governance authorization | D5 |
| shared ordering primitive, if reused | D4+ |
| realtime transport | D3+ for scaffolding, D4+ before hardening |

WorkManagement is not required for Documents core. Collaboration on Board/BoardItem requires the corresponding WorkManagement resource contract D4+.

## 3. Documents sequence

```text
Page
→ Page hierarchy
→ Block
→ Block content contract
→ Block ordering
→ query/editor integration
→ realtime/recovery
```

## 4. Page

Required:

- stable Page identity;
- Account/Workspace containment;
- explicit parent/child hierarchy;
- lifecycle semantics;
- resource/action authorization;
- archive/delete behavior;
- hierarchy mutation concurrency.

## 5. Block

Blocks belong to a Page and carry typed content.

A block type has one validated semantic contract. Provider/editor payload shape must not become Domain meaning accidentally.

Block ordering is persisted, deterministic and concurrency-aware.

## 6. Editor reconciliation

Editor local state is transient client state.

Authoritative success follows server-accepted mutations.

P4B does not claim CRDT/OT collaborative editing unless explicitly admitted by product/architecture authority.

## 7. Collaboration sequence

```text
Comment
→ target contract
→ authorization
→ realtime
→ target deletion/retention
```

## 8. Comment target contract

Collaboration stores stable target references, not foreign aggregate graphs.

The target owner provides minimal resource facts needed for:

- existence;
- tenant/workspace scope;
- authorization category;
- lifecycle/visibility where required.

Collaboration must not join directly to private Documents or WorkManagement tables as its cross-context contract.

## 9. Comment authorization

Comment read/write permission derives from:

```text
authenticated actor
+ active tenant/workspace scope
+ target access contract
+ Collaboration-specific action semantics
```

Target access is revalidated when required; a stale cached target decision must not silently preserve access.

## 10. Target deletion/retention

Target deletion does not imply accidental cascade semantics.

Required behavior must distinguish:

- comment retention;
- tombstone/reference state;
- user-visible deletion;
- historical attribution;
- privacy/deletion obligations.

## 11. Realtime/recovery

Document/comment realtime must tolerate duplicate/out-of-order delivery and support recovery from durable authoritative state.

Realtime transport does not own document/comment semantics.

## 12. Events

Documents publishes stable Page/Block facts only where consumers need them.

Collaboration publishes stable Comment facts independently.

Activity, audit and integration events remain distinct outputs.

## 13. Data ownership

Documents persistence and Collaboration persistence remain separately owned even in one physical database.

No dual truth for Page, Block or Comment.

## 14. Performance

Protect:

- bounded Page tree loading;
- block pagination/chunking where required;
- stable block ordering;
- bounded comment pagination;
- no cross-context N+1 target lookup;
- no per-comment authorization fan-out.

## 15. Exit gate

P4B can hand off downstream contracts when Page/Block and Comment/target semantics are verified, protected authorization is stable, and realtime-critical consumers have D4+ recovery evidence.

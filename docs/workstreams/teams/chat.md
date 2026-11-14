---
document_id: WRK-TEAM-CHAT
document_type: workstream-team-spec
status: draft
owner: chat-team
applies_to:
  - chat
  - channels
  - conversations
  - messages
  - threads
  - chat-realtime
  - chat-read-state
evidence:
  - docs/product/chat.md
  - docs/decisions/SYS-ADR-001-chat-bounded-context.md
  - docs/workstreams/use-cases/chat.md
  - docs/workstreams/specs/chat-foundation.md
  - docs/workstreams/chat-delivery-plan.md
  - docs/architecture/bounded-context-map.md
  - docs/architecture/contract-boundaries.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - docs/delivery/team-ownership.md
  - docs/workstreams/capability-delivery-map.md
  - docs/workstreams/cross-team-dependencies.md
  - backend/docs/architecture/domain-modeling.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/api-and-contracts.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - frontend/docs/architecture/realtime.md
review_on:
  - chat-model-change
  - chat-access-change
  - chat-realtime-change
  - capability-state-change
  - team-ownership-change
---

# Chat Workstream

> **This team executes the Chat context: channels, conversations, messages, threads, read state and the chat realtime scope. It reuses Collaboration policy and never takes over resources a channel may link to.**

# 1. Scope

In: everything in `docs/product/chat.md` section 2, delivered in three phases (`docs/workstreams/chat-delivery-plan.md`). Out: voice and video, bots and webhooks, end-to-end encryption, cross-Workspace channels.

# 2. Canonical authorities

Product rules `docs/product/chat.md` (CHT-001 to CHT-024); decision `SYS-ADR-001`; realtime rules SYS-RT-001 to SYS-RT-003 in `docs/architecture/events-realtime-and-delivery-boundary.md`; mention, reaction, attachment and notification rules COL-007 to COL-011, COL-017, COL-018, COL-029, COL-030 in `docs/product/collaboration.md`; backend rules in `backend/AGENTS.md` and `backend/docs/architecture/*`.

# 3. Owned bounded contexts

Chat (new). Logical owner `chat-team`; no team handle is invented (DEL-OWN-020).

# 4. Capabilities

| Letter | ID | Name | Use cases | Phase |
|---|---|---|---|---|
| A | CHT-001 | Channels and channel groups | UC-CHT-A01 to A08 | 1 |
| B | CHT-002 | Messages | UC-CHT-B01 to B10 | 1 |
| C | CHT-003 | Realtime delivery | UC-CHT-C01 to C09 | 1 and 2 |
| D | CHT-004 | Read state and notification levels | UC-CHT-D01 to D07 | 1 |
| E | CHT-005 | Direct and group conversations | UC-CHT-E01 to E07 | 2 |
| F | CHT-006 | Threads, replies and pins | UC-CHT-F01 to F08 | 2 |
| G | CHT-007 | Mentions, reactions, attachments, notification handoff | UC-CHT-G01 to G09 | 2 |
| H | CHT-008 | Access, moderation, retention, search, hardening | UC-CHT-H01 to H10 | 3 |

# 5. Backend ownership surfaces

```text
backend/src/Notrelix.Domain/Chat/
  Spaces/        Space (aggregate root: channel or conversation), Membership, ChannelGroup
  Messages/      Message (aggregate root), MessageKind, Thread, Pin, Reaction
  ReadStates/    read boundary and notification level are on Membership
  Rules/         ChatRuleCodes, limits, permission names
backend/src/Notrelix.Application/Features/Chat/
  Abstractions/  IChatDbContext, ISpaceSequenceAllocator
  Public/        facts and queries other contexts may call (member-of-space check)
  Channels/ Conversations/ Messages/ Threads/ ReadState/ Mentions/ Reactions/ Search/
    Commands/<UseCase>  Queries/<UseCase>  DTOs  Ports  Services
backend/src/Notrelix.Application/Events/Chat/        integration events
backend/src/Notrelix.Application/EventMappers/Chat/  fact -> event mappers
backend/src/Notrelix.Infrastructure/
  Data/DbSchemas.cs (Chat = "chat"), Data/Configurations/Chat/, Data/Chat/
  CrossContext/Chat/  Messaging/Consumers/Chat/  Realtime (scope kind Channel)
backend/src/Notrelix.API/Endpoints/Chat/<Area>/Map*Endpoints.cs
backend/src/Notrelix.API/Contracts/Chat/
```

No new `.csproj`. Handlers use `IChatDbContext` only; reads of other contexts go through ports (Workspaces membership, Governance permission, Collaboration mention and notification, Documents and Work Management resource view).

# 6. Frontend ownership surfaces

```text
frontend/packages/features/chat/        (@notrelix/features-chat)
  src/core/{types,api,query}            DTOs, REST client, query keys
  src/web/components/                   sidebar, space header, message list, composer, thread panel,
                                        member panel, pins panel, mention picker, search panel
  src/web/query/                        hooks, cache reducers for realtime events
  src/verification/                     fixtures and evidence manifest
frontend/apps/web/src/                  route chat.routes.tsx, destination `chat`, command palette,
                                        realtime adapter registration, rail badge
frontend/packages/dev/mock-backend/src/contexts/chat/
```

UI additions: a virtualized list and an emoji picker (not in `ui-web` today). The editor starts as textarea plus mention picker; TipTap stays in Documents.

# 7. Producer dependencies (Chat needs)

| From | Need | Contract |
|---|---|---|
| Workspaces | Member list, role, member-removed and workspace-deleted facts | `Public` queries and facts |
| Governance | Permission checks for `chat.*`, Audit sink | pipeline authorization, Audit facts |
| Collaboration | Mention identity, reaction rule, attachment storage, notification delivery, Inbox | ports and facts |
| Platform | Realtime gateway, outbox, rate limiting, object storage, Redis | existing infrastructure |
| Work Management, Documents | Resource view check for links | `ResourceRef` resolution port |

# 8. Consumer dependencies (others need Chat)

| Consumer | Need |
|---|---|
| Collaboration | Notification intents from Chat facts |
| Governance | Audit facts |
| Search | Indexable message facts with access inputs |
| Analytics | Optional usage facts (not in Phase 1) |
| Web shell | Unread totals for the rail badge |

# 9. Authorization model

Resource kinds `chat.channel` and `chat.conversation`. Actions: `chat.channel.create`, `chat.channel.manage`, `chat.group.manage`, `chat.member.invite`, `chat.message.post`, `chat.message.moderate`, `chat.mention.broadcast`, `chat.message.pin`, `chat.message.react`, `chat.search`. Public channels: Workspace members with a default grant; private channels, direct and group conversations: membership of the space is required in addition to the action. Workspace Owner and Admin do not inherit access to private spaces (CHT-004). The permission inventory and its test (`PermissionActionInventoryTests`) gain these actions. Realtime authorization is a Chat-owned query evaluated at subscribe and every 60 seconds.

# 10. Data ownership

Schema `chat`, tenant scoped (`workspace_id`, `account_id`), row level security through `ops.apply_scoped_business_policies('chat', <table>, true)`; child tables inherit through `apply_scope_expression_policies`. Tables are defined in `docs/workstreams/specs/chat-foundation.md`. Chat owns no foreign key into another schema. Attachments are rows in the Collaboration attachment table with target kind `chat.message`.

# 11. API and contracts

REST prefix `/api/v1/workspaces/{workspaceId}/chat`; endpoints in the use-case catalog; OpenAPI exported to `backend/contracts/openapi/notrelix.v1.json`; frontend client generated by `pnpm codegen`. Idempotency key header on message send, space creation and conversation start. Cursor pagination on messages, spaces and threads. Error mapping follows `backend/docs/architecture/api-and-contracts.md`.

# 12. Events and realtime

Integration events (outbox): `chat.channel.created|updated|archived|deleted`, `chat.membership.added|removed`, `chat.message.created|updated|deleted`, `chat.reaction.changed`, `chat.message.attachment.changed`, `chat.read.advanced`, `chat.mention.created`. Mapped to realtime by `RealtimeChangeMapper`. Realtime protocol v1.1: scope kind `Channel`, event frames with a compact payload, client frames `typing` and `presence`. The contract file `artifacts/contracts/realtime.v1.json` and `frontend/docs/architecture/realtime.md` are updated with the protocol. Limits unchanged: 50 subscriptions and 8 connections per user, 4096 bytes and 20 frames per second per connection.

# 13. Migrations

Expand-only migrations per phase (`backend/docs/operations/migrations-and-data-change.md`): Phase 1 creates `spaces`, `channel_groups`, `memberships`, `messages`; Phase 2 adds `threads`, `thread_followers`, `pins`, `reactions`, `blocks`, `direct_pairs`; Phase 3 adds `permission_overrides`, `retention_policies`, `export_jobs`, search structures. Each migration is immutable once applied, with the RLS script lines and `011_verification.sql` coverage in the same change.

# 14. Test and evidence matrix

| Level | What |
|---|---|
| Domain | Space and Message invariants, kind transitions, tombstone rules, sequence monotonicity |
| Application | Handlers per use case with fake ports; idempotent send; access matrix per kind; mention privacy |
| API | OpenAPI contract tests; authorization attributes on every endpoint; rate-limit mapping |
| Integration (real DB) | RLS cross-Workspace and cross-space negative tests; concurrent sends produce distinct gap-free sequences; outbox write in the same transaction |
| Architecture | Chat added to the context lists in `CrossContextBoundaryScanner`, `DbContextBoundaryArchitectureTests`, `DbContextBoundaryTests`, `HandlerDataPortGateTests`, `FreezeResidualBlockerTests`, `ApiArchitectureTests.expectedBcs`, `UseCaseSecurityClassificationTests` |
| Realtime | Subscribe authorization, duplicate, out-of-order, gap, reconnect, access-changed, rate limits |
| Frontend unit and component | Cache reducers, composer, list, picker, a11y (live region, keyboard) |
| Mock e2e | Two browser contexts send and receive; unread; mention; reconnect; lost access; no real socket |
| Real e2e | Send and receive through the real gateway; run by the lead or owner |
| Load | Fan-out to a large channel; budget recorded in the spec |

# 15. Delivery sequence

Per `docs/workstreams/chat-delivery-plan.md`: Phase 0 documents and admission; Phase 1 slices S1.1 to S1.8; Phase 2 slices S2.1 to S2.8; Phase 3 slices S3.1 to S3.6. Backend slice, contract, mock handlers and frontend slice of one capability ship in the order contract, backend, mock, frontend.

# 16. Blockers

1. SYS-ADR-001 Accepted (admission, protocol v1.1).
2. Owner decisions in `docs/product/chat.md` section 34 for Phase 3 items (moderation, retention, guests, limits); Phase 1 can proceed with the defaults in the feature spec.
3. Hard-coded context lists updated before the first Chat code merges.
4. The mock backend needs an event emit helper before mock e2e for realtime can be written.

# 17. Decisions the team may make locally

Internal class names, table column order, index names, query shapes, component structure, copy text in en and vi, pagination sizes within the stated bounds, throttle values for typing and presence.

# 18. Decisions requiring escalation

Any change to access rules (CHT-004), the sequence rule, the realtime frame set, retention and purge, moderator rights, new permission actions, cross-context ports, and anything on the stop-condition list in `docs/product/chat.md`.

# 19. Definition of Done

A use case is DONE per `docs/delivery/product-delivery-lifecycle.md` (DEL-003): backend, frontend, design 1 px against the owner's artifact, and real e2e. Agents build to HARDENING; the owner sets DONE. In addition: Chat docs and generated indexes in the same change, negative tenant and space tests for every read and write path, migration discipline check, architecture tests, `make docs-check`, codegen check.

# 20. Service-extraction readiness

Chat can be extracted when: no foreign key leaves the `chat` schema, every cross-context call goes through a port or fact, realtime scope and authorization query are self-contained, and the outbox is the only way facts leave. Extraction is not planned; the readiness list is kept true from Phase 1.

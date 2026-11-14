---
document_id: SPEC-CHAT-FOUNDATION
document_type: feature-spec
status: draft
owner: chat
applies_to:
  - chat
  - channels
  - conversations
  - messages
  - chat-realtime
evidence:
  - docs/product/chat.md
  - docs/decisions/SYS-ADR-001-chat-bounded-context.md
  - docs/workstreams/use-cases/chat.md
  - docs/workstreams/teams/chat.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - backend/src/Notrelix.API/Realtime/RealtimeProtocol.cs
  - backend/src/Notrelix.Application/Common/Realtime/Gateway/RealtimeScope.cs
review_on:
  - implementation-complete
  - behavior-changed
  - chat-realtime-change
---

# Chat foundation

## 1. Summary

### Problem

Teams have comments on work and documents but nowhere to talk about the team and the work in general. They leave the product for another chat tool, which splits context, access control and notifications.

### Outcome

A Discord-style chat inside each Workspace: channels in categories (public and private), direct and group conversations, threads, mentions, reactions, attachments, unread state and presence, with the same tenant, permission and notification rules as the rest of the product.

### Semantic owner

- Bounded context: `Chat` (SYS-ADR-001)
- Capability: spaces (channels, conversations) and messages
- Canonical product doc: `docs/product/chat.md`

### Primary actors

Workspace Owner, Admin, Member, Guest, Channel manager, System (jobs).

### Product scope

Workspace.

### Required related artifacts

- ADR: `docs/decisions/SYS-ADR-001-chat-bounded-context.md`
- Use cases: `docs/workstreams/use-cases/chat.md`
- Team spec: `docs/workstreams/teams/chat.md`
- Delivery plan: `docs/workstreams/chat-delivery-plan.md`

## 2. User and business problem

Context switching and unmanaged access: private conversations about work live outside the product with no link to Boards and Pages, no Workspace permission model and no Audit.

## 3. Success outcome

- A member can send and receive a message in a channel with no visible delay on a healthy connection, and never sees the same message twice or out of order.
- Unread and mention state is correct across devices and tabs.
- A person without access never sees a space, a message, a mention target or a search hit from it.
- An administrator can see who can read what and can archive, moderate and export.

## 4. Non-goals

Voice, video and stage channels; bots and webhooks; end-to-end encryption; cross-Workspace channels; scheduled messages; translation.

## 5. Ubiquitous language

See `docs/product/chat.md` section 4: Channel, Channel group, Conversation, Space, Message, Sequence, Thread, Read boundary, Notification level, Link.

## 6. Semantic ownership

| Concept | Owner | Chat uses it by |
|---|---|---|
| Space, Membership, Message, Thread, Pin, Reaction row, read boundary | Chat | owns |
| Mention identity, attachment policy, notification delivery, Inbox | Collaboration | port and facts |
| Permission, Audit | Governance | pipeline and facts |
| Workspace member and role | Workspaces | `Public` query and facts |
| Board, Page, Team | their contexts | `ResourceRef` resolution port |

## 7. Actors and access

See `docs/product/chat.md` section 29. Access to a space requires the `chat.*` action and, for private channels and conversations, membership. Owner and Admin roles do not imply access to private spaces. Guests only see spaces they were added to.

## 8. Preconditions

The caller is authenticated, in an Active Workspace they belong to, and the Workspace is not suspended or deleted. Posting needs an Active (not archived) space.

## 9. State model

```text
Space:      Active <-> Archived;  Active|Archived -> Deleted -> Purged
Membership: Invited -> Member -> Left | Removed
Message:    Sent -> Edited* -> Deleted (tombstone) -> Purged (retention only)
Thread:     Open <-> Archived
```

Transitions are commands with an expected version. A transition from a terminal state is rejected with a stable code.

## 10. Invariants

1. A message belongs to exactly one space; the Workspace of the message equals the Workspace of the space.
2. `(space_id, sequence)` is unique, gap-free and monotonic; a tombstone keeps its sequence.
3. `(space_id, author_id, client_message_id)` is unique.
4. A direct space has exactly two participants and is unique per unordered pair per Workspace.
5. A read boundary only increases and never exceeds the space's last sequence.
6. A private space is never readable through a path that skips membership.
7. A system message cannot be edited or deleted by a user.
8. At least one manager exists on a channel unless it is archived or deleted.

## 11. Operation semantics

See the use-case catalog for each operation. Cross-cutting rules:

- Every command receives the Workspace from the request context and the expected version where it mutates.
- **Send.** In one database transaction: lock the space row (`UPDATE chat.spaces SET last_sequence = last_sequence + 1 ... RETURNING last_sequence`), insert the message with that sequence, update the author's read boundary, bump mention counters, write outbox facts. The lock serializes writers per space only.
- **Edit and delete** use the expected version and never change the sequence.
- **Join and leave** write a system message in channels.

## 12. User-visible behavior

Layout: rail icon with badge; a second column listing categories, channels and conversations; the message area with header (name, topic, members, pins, search); the composer; a right panel for threads, members and pins. On narrow screens the lists and panels are drawers. States for every surface: loading skeleton, empty, error with retry, lost access, archived (read-only banner), offline banner, pending and failed message states.

## 14. Accessibility contract

The message list is a `log` live region with polite announcements for new messages in the open space and none for muted ones; every action is reachable by keyboard; shortcuts for switch space, next unread, reply, edit last message; focus returns to the composer after send; contrast and reduced motion follow the design system.

## 15. API contract

Prefix `/api/v1/workspaces/{workspaceId}/chat` (the Workspace is in the path, validated by membership, and every handler requires the space to belong to it, otherwise the same NotFound as a missing space), JSON, session cookie and CSRF as the rest of the API, `Idempotency-Key` on creates and sends.

| Area | Endpoints |
|---|---|
| Channels | `POST/GET /channels`, `GET/PATCH/DELETE /channels/{id}`, `POST /channels/{id}/archive`, `PATCH /channels/order`, `PUT /channels/{id}/link`, `PUT /channels/{id}/permissions` |
| Groups | `POST/PATCH/DELETE /channel-groups` |
| Members | `POST /channels/{id}/members`, `POST /channels/{id}/members/me`, `DELETE /channels/{id}/members/{userId}` |
| Conversations | `POST/GET /conversations`, `POST /conversations/{id}/participants`, `DELETE /conversations/{id}/participants/me`, `PUT /conversations/{id}/visibility` |
| Messages | `POST/GET /spaces/{id}/messages` (cursor: `before`, `after`, `around`), `PATCH/DELETE /messages/{id}` |
| Threads, pins | `POST/GET /messages/{id}/replies`, `PUT /threads/{id}/follow`, `PUT/DELETE /messages/{id}/pin`, `GET /spaces/{id}/pins`, `GET /me/threads` |
| Reactions | `PUT/DELETE /messages/{id}/reactions/{kind}`, `GET /messages/{id}/reactions` |
| Read state | `GET /me/unread`, `PUT /spaces/{id}/read`, `POST /me/read-all`, `PUT /spaces/{id}/notification-level` |
| Mentions, links | `GET /spaces/{id}/mentionables`, `GET /links/resolve` |
| Search | `GET /search` (Chat.Search.Query, UC-CHT-H07; live phrase match, no index) |
| Retention | `GET/PUT /retention` (Workspace rule: Owner sets, every member reads; `expectedVersion`, 412) |
| Export | `POST /exports` (Owner or Admin; 202), `GET /exports/{id}` (status), `GET /exports/{id}/content` (the JSON file, the requester only, while it has not expired) |

Error codes are stable (`space-not-found`, `channel-archived`, `name-conflict`, `version-conflict` (HTTP 412, the codebase convention for a stale expected version), `rate-limited`, `limit-reached`). Responses for no-access and missing are identical.

A rate limit or a channel's slow mode is HTTP 429 with a `Retry-After` header, `errorCode` `rate_limit.exceeded` and `ruleCode` `rate-limited` or `slow-mode`; the body also has `retryAfter` (whole seconds), and `policy`, `scope` and `limit` (a rate limit) or `slowModeSeconds` (slow mode). The policies are `chat.send`, `chat.react`, `chat.thread-reply`, `chat.upload`, `chat.broadcast` and `chat.search`, per person and per space (UC-CHT-H08), and `chat.export` (5 per person and 20 per Workspace per hour, UC-CHT-H06). A channel's slow mode is `slowModeSeconds` (0 to 3600) on the channel and is set with `PATCH /channels/{id}`.

## 16. Generated client and frontend contract

OpenAPI is exported from the backend and the client regenerated with `pnpm codegen`; hand-maintained route entries in `endpoints.ts`. The frontend never builds a Workspace id from the client; it uses the active Workspace.

## 17. Realtime contract (protocol v1.1, additive)

- Scope kind `channel` (value is the space id). Subscribe is authorized by a Chat-owned query; failure returns `subscription-error` with no detail.
- Server frame `event` with `eventType` in `chat.message.created|updated|deleted`, `chat.reaction.changed`, `chat.message.attachment.changed`, `chat.membership.changed`, `chat.space.changed` (the change is `pinned`, `unpinned`, `permissions`, `slow-mode` or `retention`; a retention batch sends one `retention` frame per space, not one delete frame per message, and readers refetch the messages, pins and channel views of the space); `streamKey` per space, gap-free `streamVersion`; payload compact (id, sequence, author, content, edited).
- `chat.message.attachment.changed` (payloadContract `chat.attachment.v1`) is emitted by Chat.Attachments.Upload and Delete in the same transaction as the file change (domain fact on the message, outbox, the shared chat message consumer partitioned by space), so it rides the gap-free channel stream and resumes like any message frame. An added file carries `{ messageId, change, attachmentId, fileName, sizeBytes, contentType, uploadedBy, createdAt, downloadPath }`, a removed file only the ids; no bytes; `detailsOmitted` when the name does not fit the 3000-byte payload budget (the reader refetches `GET .../attachments`). Readers are exactly the readers of the message; the message version does not change. A rejected upload emits nothing. The uploader's in-progress state is not broadcast (no frame): others see the file once stored.
- Per-user stream `chat.unread.changed` (space id, unread, mentions) using the existing per-user notification stream channel.
- Client frames `typing` (`{ type, subscriptionId }`, the id of a channel subscription) and `presence` (`{ type, state }`, state online, idle or offline); both ephemeral, throttled (typing at most one per 3 seconds from the client, the gateway drops one sooner than 2 seconds; presence a heartbeat about every 30 seconds), counted inside the 4096-byte and 20 frames per second limits, and never stored.
- Server frames `typing` (`{ subscriptionId, userId, expiresInMs: 5000 }`) and `presence` (`{ subscriptionId, userId, state, ttlMs: 75000 }`) go to the other subscribers of the space, never to the sender. Authorization is the channel-scope authorizer: a typing frame is ignored without a reply when the connection holds no such subscription or the authorizer no longer grants the space (archived included); presence is heard only through a space subscription, so a Guest sees it only in spaces they were added to. Presence is held in connection memory with an expiry (offline after 75 seconds without a heartbeat or at close) and rebuilt from heartbeats after a restart; a new subscriber is told who is here by a one-time request the other connections answer. Neither frame is stored, replayed or used for authorization. Instances exchange them on an ephemeral bus (Redis pub/sub, in process in Development), not on the event stream.
- `access-changed` closes affected subscriptions (CHT-017).
- Clients assume duplicates, out-of-order, gaps and reconnects; recovery is `GET .../messages?after=<lastSequence>`.

## 18. Cross-context facts and side effects

Outbox facts listed in the team spec section 12. Consumers: Collaboration (notification intents, mention counters), Governance (Audit facts), Search (index), Realtime mapper. No consumer may block the send.

## 19. Data semantics

Schema `chat` (all tenant scoped; `workspace_id`, `account_id` on every table).

| Table | Key columns |
|---|---|
| `spaces` | id, workspace_id, kind (`channel_public`, `channel_private`, `direct`, `group`), name, topic, group_id, position, link_kind, link_id, archived_at, deleted_at, last_sequence, version |
| `channel_groups` | id, workspace_id, name, position |
| `memberships` | space_id, user_id, role (`manager`, `member`), notification_level, muted_until, read_sequence, mention_count, joined_sequence, left_at |
| `messages` | id, space_id, sequence, author_id, kind (`user`, `system`), content, client_message_id, reply_to_id, thread_root_id, edited_at, deleted_at, deleted_by, version |
| `threads` | root_message_id, reply_count, last_reply_at, archived_at |
| `thread_followers` | root_message_id, user_id, muted |
| `pins` | space_id, message_id, pinned_by, pinned_at |
| `reactions` | message_id, user_id, kind, created_at (unique on the triple) |
| `blocks` | workspace_id, blocker_id, blocked_id |
| `direct_pairs` | workspace_id, user_low, user_high, space_id (unique on the first three) |

Indexes: `messages (space_id, sequence desc)`, unique `(space_id, sequence)`, unique `(space_id, author_id, client_message_id)`, `memberships (user_id)`, `spaces (workspace_id, kind)`.

## 20. Schema and migration impact

Additive. Phase 1 creates the first four tables; later phases add the rest. Phase 3 (S3.3) adds `chat.retention_policies` (one row per Workspace, the id is the Workspace id) and `chat.export_jobs`, both with RLS lines, in the migration `AddChatRetentionAndExports` (expand-only). RLS policy lines are added to `008_policies_workspace_scoped_domain.sql`, grants to `005_grants.sql`, verification to `011_verification.sql`.

## 21. Failure semantics

Network failure on send leaves a pending message that retries with the same `clientMessageId`. A server rejection leaves the text as a draft with the reason. A realtime failure falls back to polling the `after` cursor at a slow interval until the socket returns. A failed consumer retries from the outbox and never blocks a send.

## 22. Idempotency and duplicate behavior

Send, create space, start conversation and reactions are idempotent by key. Duplicate realtime events are dropped by `(space, sequence, version)`.

## 23. Ordering

Order is the per-space sequence. Edits and deletes carry versions and never reorder.

## 24. Performance and scale expectations

Budgets to be confirmed by a load test before Phase 1 HARDENING: send p95 under 150 ms server time; first page of history p95 under 200 ms for a space of 100,000 messages; end-to-end send-to-receive under 500 ms p95 on a healthy connection; fan-out to a 2,000-member channel without exceeding gateway queue limits. Pages are 50 messages by default, 100 maximum. Message lists are virtualized.

## 25. Security and privacy

RLS by Workspace; membership checks in the Application layer for private spaces and conversations; identical NotFound for no access and missing; content rendered through the safe contract; link previews resolved with the reader's own access; per-person and per-space rate limits; Audit facts for administration; no durable write over the socket; typing and presence never grant access.

## 26. Observability and operations

Background jobs: `Chat.Retention.Apply` (hosted service, `Chat:Retention`, batches of 200 messages per space, each under a transaction advisory lock on the space; one System Audit fact per batch with counts only) and the export worker (builds the JSON file into platform object storage; a sweep expires files after 24 hours and requeues a job stuck Running for 15 minutes). Metrics: sends per second, send latency, send-to-receive latency, active subscriptions, gaps recovered, rate limited sends, outbox lag for chat facts. Logs carry space id and Workspace id, never message content. Alerts on outbox lag and gap-recovery rate.

## 27. Notifications, activity and collaboration

Notification intents: mention, direct message, thread reply, broadcast, invitation. Collaboration applies preferences, levels and privacy at delivery time. Activity feed entries are not produced for ordinary messages.

## 31. Acceptance scenarios

1. Given two members of a channel, when one sends a message, then the other sees it once, in order, without a refetch.
2. Given a send whose response is lost, when the client retries, then exactly one message exists.
3. Given 20 concurrent senders, then the stored sequences are distinct and gap-free.
4. Given a private channel, then a Workspace Admin who is not a member cannot list it, read it, search it or subscribe to it.
5. Given a removed member with the channel open, then the pane shows lost access and no later event reaches them.
6. Given a mention of someone who cannot see the channel, then they are not notified and gain nothing, and the sender was warned.
7. Given two devices, then reading on one clears the badge on the other.
8. Given a dropped realtime frame, then the next recovery query shows the missing message and nothing is duplicated.
9. Given an archived channel, then posting is refused and history stays readable.
10. Given a Guest, then only the spaces they were added to are visible.

## 32. Acceptance proof matrix

| Scenario | Proof |
|---|---|
| 1, 8 | Realtime tests, mock e2e with two contexts |
| 2, 3 | Application and integration tests (real DB) |
| 4, 5, 10 | Integration RLS and authorization tests, realtime access tests |
| 6 | Application test with Collaboration port fake, integration test of the consumer |
| 7 | Mock e2e with two sessions, API test |
| 9 | Application test, component test |

## 33. Definition of Done additions

Docs and generated indexes updated; context lists in architecture tests updated; negative tenant and space tests present; migration discipline check; contract exports and codegen check; load test result recorded.

## 34. Resolved product decisions

Discord-style scope; gateway extended with writes through REST; three phases (2026-11-10). Defaults used in Phase 1 until the owner decides otherwise: message length 4,000; page 50; edit allowed by the author at any time; group cap 8; pin limit 50 per space; typing throttle 3 seconds; thread auto-archive after 7 days of inactivity.

## 35. Blocking unresolved questions

Moderator rights, retention window and group history on leave, guest details, per-Workspace limits, silent join to public channels (`docs/product/chat.md` section 34). None blocks Phase 1.

## 36. Out of scope

See section 4 and the use-case catalog section 6.

## 37. Related canonical documents

`docs/product/chat.md`, `docs/product/collaboration.md`, `docs/product/governance.md`, `docs/architecture/bounded-context-map.md`, `docs/architecture/events-realtime-and-delivery-boundary.md`, `backend/docs/architecture/*`, `frontend/docs/architecture/realtime.md`.

## 38. Implementation handoff

Order per capability: contract (OpenAPI and realtime), backend (Domain, Application, Infrastructure, API with architecture tests), mock handlers, frontend slice, e2e. Lanes work to HARDENING; the owner sets DONE.

## 39. Final feature acceptance

Accepted when every use case of the phase is HARDENING with evidence, the acceptance scenarios above pass for that phase, and the owner confirms in the real stack.

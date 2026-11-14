---
document_id: PROD-CHAT
document_type: product-context
status: active
owner: chat
applies_to:
  - chat
  - channels
  - channel-groups
  - conversations
  - messages
  - threads
  - pins
  - chat-read-state
  - chat-presence
  - chat-notifications
evidence:
  - PRODUCT.md
  - docs/product/product-model.md
  - docs/product/collaboration.md
  - docs/product/workspaces.md
  - docs/product/governance.md
  - docs/architecture/bounded-context-map.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - docs/decisions/SYS-ADR-001-chat-bounded-context.md
  - docs/workstreams/use-cases/chat.md
  - docs/workstreams/specs/chat-foundation.md
review_on:
  - channel-model-change
  - message-model-change
  - conversation-model-change
  - chat-access-change
  - chat-realtime-change
  - chat-retention-change
  - chat-moderation-change
---

# Chat Context

> **Chat owns free-standing conversation inside a Workspace: channels and their groups, direct and group conversations, messages, threads, pins, and each person's read position and notification level in a conversation.**
>
> It reuses Collaboration's mention, reaction, attachment and notification rules and never owns the resources a channel may link to.

This document is the canonical product owner for Chat semantics. 

Collaboration owns comments on product resources. Governance owns permissions and security Audit. Workspaces owns membership. Platform owns realtime delivery mechanics.

# 1. Mission

Chat gives a team one place to talk without leaving the Workspace: fast, ordered, searchable, and bound by the same tenant and permission rules as the rest of the product.

It supports:

```text
public and private channels grouped in categories
direct and small-group conversations
threads, replies, pins
mentions, reactions, attachments
unread positions and notification levels
typing and presence (ephemeral)
```

# 2. Owns

- Channel, channel group (category), channel membership and channel notification level.
- Conversation (direct and group) and its participants.
- Message, its sequence, edit history marker, tombstone, reply reference and thread root.
- Thread, pin.
- Per-user, per-channel read boundary.
- Chat-specific access rules: who can see, join, post, edit and moderate.

# 3. Does not own

- Comments on Board items and Pages (Collaboration).
- Mention identity, reaction uniqueness, attachment file policy and notification delivery rules (Collaboration; Chat applies them).
- The Inbox and notification preferences matrix (Collaboration).
- Workspace membership, roles and security Audit (Workspaces, Governance).
- Realtime transport, fan-out and connection limits (Platform).
- Boards, items, Pages and Teams that a channel may link to (their source contexts).
- Voice, video, stage channels, screen sharing and calls (out of scope, separate ADR).

# 4. Ubiquitous language

| Term | Meaning |
|---|---|
| Channel | A named conversation space in a Workspace. Kind is public or private. |
| Channel group | A category that orders channels in the sidebar. It has no access meaning. |
| Conversation | A direct (two people) or group (three or more) space with a fixed participant set. |
| Space | Either a Channel or a Conversation; the unit that has messages, a sequence and a read boundary. |
| Message | One immutable-order entry in a space with content, author and sequence. |
| Sequence | Server-assigned, gap-free, monotonic integer per space. |
| Thread | Replies that hang off one root message in the same space. |
| Read boundary | The highest sequence a person has seen in a space. |
| Notification level | all, mentions-only or none, per person per space. |
| Link | An optional `ResourceRef` from a channel to a Board, Page or Team. |

# 5. CHT-001 — A space is the unit of scope and access

Every message, thread, pin, read boundary and subscription belongs to exactly one space (Channel or Conversation).

Access to a message is access to its space. There is no per-message permission except author-only edit and delete (CHT-007).

# 6. CHT-002 — Chat is Workspace-scoped and the Workspace is derived

A space belongs to one Workspace. The Workspace is derived from the space on the server and never trusted from the client.

A request for a space in another Workspace, or one the caller cannot see, returns the same NotFound as a missing space.

# 7. CHT-003 — Space kinds

| Kind | Who can see it | Who can join |
|---|---|---|
| Public channel | Every non-guest Workspace member | Self-join |
| Private channel | Its members only | Invited by a member with the invite action |
| Direct conversation | Its two participants | Fixed; one per unordered pair per Workspace |
| Group conversation | Its participants | Fixed at creation; participants can leave; a member may add others up to the cap |

The kind of a channel does not change after creation except public to private and back by a user with the manage action; the change is recorded and announced in the channel.

# 8. CHT-004 — Membership and access

Public channels are visible to Workspace members according to Governance. Private channels, direct and group conversations are visible to members of the space only; Workspace Owners and Admins do **not** see them by role alone.

A Guest sees only the spaces they were explicitly added to and no member directory beyond those spaces.

Losing Workspace membership, or being removed from a space, ends access at once, including live subscriptions (CHT-017).

# 9. CHT-005 — Order is server-assigned

Each space has a gap-free, monotonic sequence assigned when the message is stored.

Clients order by sequence, never by their own clock or arrival order. A temporary client message shows in place and is reconciled to its sequence when the server answers (COL-025).

# 10. CHT-006 — Sending is idempotent

A send carries a `clientMessageId` unique per space and sender. A retry with the same id returns the stored message and creates no second one.

A send to a space the caller cannot post to, an archived space, or with invalid content is rejected without storing anything.

# 11. CHT-007 — Edit and delete

Only the author edits their message. Edit uses the expected version and shows an "edited" marker.

Delete by the author leaves a tombstone (author, time, sequence kept; content removed). A tombstone with live replies stays in its thread (the same rule as COL-006).

Moderator delete by a person with the moderate action is recorded in Audit and shows as removed by a moderator. Who may moderate is an open decision (see Open decisions).

# 12. CHT-008 — Content limits and safe rendering

A message has 1 to 4,000 characters after trim, or at least one attachment. Markup and links follow the safe-render contract of COL-029. A message never carries executable content.

# 13. CHT-009 — Read state is a monotonic boundary

Read state is per person and space. Mark-read only moves the boundary forward. Unread count is derived from the boundary and the space's last sequence, not a mutable counter (COL-014, COL-015).

A mention to a person is counted separately from the unread count.

# 14. CHT-010 — Mentions reuse Collaboration identity rules

Mentions resolve to stable identities (COL-007). Mentioning someone who cannot view the space stores the mention, notifies nobody and grants nothing; the composer warns before sending (COL-030).

`@channel` and `@here` need the broadcast action in the space and show a confirmation when the space has more than a configured number of members. `@here` reaches members currently present only.

# 15. CHT-011 — Reactions reuse Collaboration uniqueness

Reaction uniqueness is `message + actor + kind` and retries are idempotent (COL-009).

# 16. CHT-012 — Attachments reuse the shared file policy

Message attachments store metadata and object identity only, with short-lived download capability, the shared size cap, blocked extensions and sniffed type (COL-010, COL-011). The attachment target kind is `chat.message`.

# 17. CHT-013 — Threads

A thread belongs to one root message in the same space. A reply has no membership of its own: it inherits the space's access. A thread can be followed or muted per person and archives itself after inactivity; a new reply reopens it.

# 18. CHT-014 — Conversation identity

There is at most one direct conversation per unordered pair of people per Workspace; creating it twice returns the first. A group conversation has a participant cap set by configuration.

A person who leaves a group conversation sees its history up to that moment only if the retention decision allows it (open decision).

# 19. CHT-015 — Realtime is a freshness channel with a compact payload

Subscription is at the narrowest boundary: one space per subscription, plus one per-user stream for unread and mention badges. Clients assume duplicates, out-of-order delivery, gaps and reconnects (SYS-RT-003) and recover by querying from the last known sequence.

The `message` event carries a compact payload so a reader can render without a round trip; REST remains the source of truth.

# 20. CHT-016 — Typing and presence are ephemeral and never authority

Typing and presence signals are not stored as history and never grant access (COL-012). Joining a presence channel needs normal authorization. Presence lives in gateway memory or Redis, not in a durable table.

# 21. CHT-017 — Access change is immediate for live connections

When membership, role or space access changes, the gateway sends `access-changed`, closes the affected subscription and the client drops cached messages of that space. A removed person receives no further events.

# 22. CHT-018 — Archived and deleted spaces

An archived channel is read-only and still searchable by its members. A deleted channel is soft deleted with a recovery window, then purged by the retention rule. Realtime events for a deleted space carry enough identity for clients to remove it (SYS-RT, §61).

# 23. CHT-019 — Notifications are intents; Collaboration delivers

Chat emits notification intents (mention, direct message, thread reply, broadcast). Collaboration decides delivery by the person's preferences and the space's notification level (COL-017, D15). A muted space, thread or person produces no in-app or email notification, and a mention still counts as a badge unless the space is muted.

Provider delivery is not notification truth (COL-018).

# 24. CHT-020 — Rate limits and abuse controls

Sending, reacting, typing and creating spaces are rate limited per person and per space. A limit returns a clear, retryable error and never loses the draft. Attachments obey the shared file policy.

# 25. CHT-021 — Retention and anonymization

History outlives an anonymized author (COL-027). Retention defaults and per-Workspace overrides are an owner decision (open). Deletion is tombstone by default; purge is explicit and audited.

# 26. CHT-022 — Search is derived and authorization-scoped

A search result is filtered by the caller's current access to the space at query time. Search indexes never become a second source of truth, and a person removed from a space no longer finds its messages.

# 27. CHT-023 — Links to resources never mutate them

A channel may be linked to a Board, Page or Team by `ResourceRef`, and a message may carry a resource link. The link is validated through Application and Governance contracts and never changes the target (COL-002). A message may be turned into a Work Management item only through a Work Management command called by the user.

# 28. CHT-024 — Chat does not own Audit

Chat emits facts for Governance Audit (channel created, privacy changed, member added or removed, moderator delete). The Audit record is Governance's. A message history is not an audit log.

# 29. CHT-025 — Images are shown in a gallery and never move the layout

A message may carry several images. They are shown as one gallery: one image at its own aspect ratio, two to four as a grid, and from five images four tiles with a `+N` tile. The box of an image is reserved from its dimensions (or from the local file while uploading), so the list does not jump when the image loads. An uploading image shows progress and can be cancelled. A full-screen viewer pages through the images of the message. Size and type policy stay with Collaboration (COL-010, COL-011).

# 30. CHT-026 — A voice message is a message with one audio attachment

A voice message has one audio attachment, a duration and a waveform, and is at most five minutes (owner decision pending). It is edited and deleted like any message. A sticker is a message with no text and no attachment. GIFs come only from a provider the server calls on the person's behalf, enabled per Workspace; the browser never contacts a third party.

# 31. CHT-027 — A call belongs to a space and follows its access

A voice or video call is started in a space the caller can write to and is offered only to its members who are not blocked. A space has at most one ringing or active call and a person is in at most one active call; a second invitation is answered as busy. Ringing ends as missed after 45 seconds. Every ended call leaves one system message with the result and the duration, and a missed call notifies the callee once. Notrelix does not record, store or transcribe call media. Calls require the media infrastructure of SYS-ADR-002; when it is unavailable, the call cannot start and the space is unaffected.

# 32. Actors and permissions

| Actor | Can |
|---|---|
| Workspace Owner, Admin | Create and manage channels and groups; see public channels; moderate if granted; manage limits |
| Member | Join public channels, create private channels and conversations if allowed, post, edit own, react, mention, attach |
| Guest | Read and post only in spaces they were added to; no broadcast mention; no member directory |
| Channel manager | Rename, change topic, archive, add and remove members of that channel |
| System | Announce joins, privacy changes and archive in the channel |

Permission actions are modeled in Governance as `chat.*` actions on resource kinds `chat.channel` and `chat.conversation` (see the team spec).

# 33. State models

```text
Channel:       Active -> Archived -> Active
               Active|Archived -> Deleted (soft) -> Purged
Message:       Sent -> Edited (n times) -> Deleted (tombstone)
Thread:        Open -> Archived (inactive) -> Open (new reply)
Membership:    Invited -> Member -> Left | Removed
```

# 34. Capabilities and phases

| Capability | Scope | Phase |
|---|---|---|
| A, CHT-001 | Channels and channel groups | 1 |
| B, CHT-002 | Messages | 1 |
| C, CHT-003 | Realtime delivery, typing, presence | 1 core, 2 rest |
| D, CHT-004 | Read state and notification levels | 1 |
| E, CHT-005 | Direct and group conversations | 2 |
| F, CHT-006 | Threads, replies, pins | 2 |
| G, CHT-007 | Mentions, reactions, attachments, notification handoff | 2 |
| H, CHT-008 | Access hardening, moderation, retention, search, digest | 3 |
| I, CHT-009 | Rich media: gallery, viewer, voice messages, stickers, GIFs | 4 |
| J, CHT-010 | Calls: one-to-one voice and video, then group and screen sharing | 5 and 6 |

The team capability IDs `CHT-001` to `CHT-008` are catalog coordinates; they are not the product rule IDs above, in the same way as `COL-001` to `COL-008` in the Collaboration team spec.

# 35. Boundaries with neighbors

| Neighbor | Chat gets | Chat gives |
|---|---|---|
| Collaboration | Mention, reaction, attachment and notification rules; Inbox | Notification intents; mention facts |
| Governance | Permission checks, Audit sink | Audit facts |
| Workspaces | Member and role lookups, Workspace lifecycle | Reaction to member removal and Workspace delete |
| Work Management, Documents | Resource existence and view checks for links | Nothing; links are by `ResourceRef` |
| Platform | Realtime transport, outbox, rate limiting, object storage | Scope kind, authorization query, event mappers |
| Search | Index infrastructure | Indexable facts, authorization filter inputs |

# 36. Current source alignment

At the time of writing there is no Chat code. Only a placeholder route (`/workspaces/$workspaceId/chat`), a navigation entry in the unmounted legacy sidebar and a `"chat"` recent-item type exist in the frontend.

# 37. Open decisions (owner)

1. Who may moderate (delete others' messages): Workspace Owner and Admin only, or a delegated role.
2. Retention window and whether a person who left a group conversation keeps history.
3. Guest model details (member directory visibility, default spaces).
4. Per-Workspace limits: channels, members per channel, group size, broadcast confirmation threshold, message rate.
5. Whether a public channel can be joined without notification to members.

# 38. Stop conditions

Stop and ask the owner before implementing if a change would:

- let a person read a private space or conversation by role alone,
- trust a client Workspace or space scope,
- store typing or presence as durable authority,
- order messages by a client clock,
- accept a durable write over the realtime socket,
- notify or grant access to a mentioned person who cannot view the space,
- purge history without an explicit, audited rule.

# 39. Change impact

| Change | Also update |
|---|---|
| Message or channel model | Use-case catalog, team spec, feature spec, domain-modeling, migrations |
| Access rules | Governance permission inventory, RLS policies, realtime authorization, security docs |
| Realtime frames | Events and realtime boundary, frontend realtime doc, realtime contract |
| Retention | Operations recovery doc, retention jobs, tests |

# 40. Testing and evidence

Each use case in `docs/workstreams/use-cases/chat.md` names its backend, frontend, design and real e2e evidence. Cross-tenant and cross-space negative tests are required for every read and write path. Realtime tests cover duplicate, out-of-order, gap, reconnect and access-change.

# 41. Related canonical owners

Collaboration `docs/product/collaboration.md`, Governance `docs/product/governance.md`, Workspaces `docs/product/workspaces.md`, bounded contexts `docs/architecture/bounded-context-map.md`, realtime `docs/architecture/events-realtime-and-delivery-boundary.md`.

# 42. Final rule

If a conversation is about a product resource, it is a Collaboration comment. If it is about the team, it is Chat. Neither context owns the other's messages, and neither lets access follow anything but its own rule.

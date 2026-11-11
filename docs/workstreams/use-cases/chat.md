---
document_id: WRK-UC-CHAT
document_type: workstream-use-case-catalog
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
  - use-case-catalog
  - capability-acceptance
evidence:
  - docs/workstreams/use-cases/README.md
  - docs/product/chat.md
  - docs/workstreams/teams/chat.md
  - docs/workstreams/specs/chat-foundation.md
  - docs/decisions/SYS-ADR-001-chat-bounded-context.md
  - docs/product/collaboration.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
review_on:
  - channel-model-change
  - message-model-change
  - conversation-model-change
  - chat-access-change
  - chat-realtime-change
  - capability-state-change
---

# Use cases: Chat

Dates: this catalog was written on 2026-11-10 as a specification. No Chat code exists yet, so every use case is DEFINED. Evidence lines name the intended endpoints and surfaces; they become real references as lanes build them.

The product rules are in `docs/product/chat.md` (CHT-001 to CHT-024). The capability letters map to the team capabilities CHT-001 to CHT-008 of `docs/workstreams/teams/chat.md`; these are catalog coordinates, not product rule IDs.

## 1. Assessment

### 1.1 State per capability

| Letter | Capability | Use cases | DEFINED | IMPLEMENTING | INTEGRATING | HARDENING | DONE | BLOCKED |
|---|---|---|---|---|---|---|---|---|
| A | CHT-001 Channels and channel groups | 8 | 8 | 0 | 0 | 0 | 0 | 0 |
| B | CHT-002 Messages | 10 | 10 | 0 | 0 | 0 | 0 | 0 |
| C | CHT-003 Realtime delivery | 9 | 9 | 0 | 0 | 0 | 0 | 0 |
| D | CHT-004 Read state and notification levels | 7 | 7 | 0 | 0 | 0 | 0 | 0 |
| E | CHT-005 Direct and group conversations | 7 | 7 | 0 | 0 | 0 | 0 | 0 |
| F | CHT-006 Threads, replies and pins | 8 | 8 | 0 | 0 | 0 | 0 | 0 |
| G | CHT-007 Mentions, reactions, attachments and notification handoff | 9 | 9 | 0 | 0 | 0 | 0 | 0 |
| H | CHT-008 Access, moderation, retention, search and hardening | 10 | 10 | 0 | 0 | 0 | 0 | 0 |
| | Total | 68 | 68 | 0 | 0 | 0 | 0 | 0 |

### 1.2 Gaps

1. No Chat domain, tables, endpoints or events exist.
2. The realtime gateway is notify-only with a closed scope enum (`Workspace`, `Board`) and no client data frame; Chat needs a Channel scope, message payload frames, typing and presence (SYS-ADR-001 decision 4).
3. There is no virtual list or emoji picker in the UI package; TipTap is confined to the Documents package.
4. Moderator delete rules, retention, guest model and limits are owner decisions that are not made yet.
5. Presence and read state exist in Collaboration as Domain classes with no use cases (UC-COL-E03, G14); Chat defines its own per-space versions and the Collaboration catalog must point here to avoid duplicates.
6. The mock backend has no Chat handlers and cannot emit realtime events to tests.
7. Hard-coded context lists in architecture tests and doc tooling must gain Chat.

### 1.3 Defects in shipped code

- The route `/workspaces/$workspaceId/chat` renders a static "coming soon" page with an unused workspace parameter.
- The "Chat Rooms" entry in the legacy sidebar metadata points at the Workspace root and the sidebar is not mounted.

### 1.4 Enterprise baseline

A team chat is expected to provide ordered, idempotent delivery, unread state across devices, mentions with privacy, retention and export, moderation with Audit, search filtered by access, rate limits and accessible live regions. Phase 3 closes the baseline.

## 2. Index

| ID | Use case | Capability | Actors | BE | FE | Design | Real e2e | State | Gap |
|---|---|---|---|---|---|---|---|---|---|
| UC-CHT-A01 | Create a channel | A | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-A02 | Create and manage a channel group | A | Workspace Owner, Admin, Member with chat.group.manage | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-A03 | Rename a channel and set its topic | A | Channel manager, Workspace Admin, Owner | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-A04 | Reorder channels and groups | A | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-A05 | Browse and join a public channel | A | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-A06 | Create a private channel and invite people | A | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-A07 | Leave a channel or remove a member | A | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-A08 | Archive, unarchive and delete a channel | A | Channel manager, Workspace Admin, Owner | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-B01 | Send a message | B | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-B02 | Load history with a cursor | B | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-B03 | Jump to a message | B | Any member who can view the space | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-B04 | Edit a message | B | Author only | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-B05 | Delete my message | B | Author | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-B06 | Recover from a failed send | B | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-B07 | Keep a draft per space | B | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-B08 | Render content safely | B | Any reader | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-B09 | See system announcements | B | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-B10 | Copy a link to a message | B | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-C01 | Subscribe to a space | C | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-C02 | Receive a new message live | C | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-C03 | Receive an edit or delete live | C | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-C04 | Recover from a gap, duplicate or out-of-order event | C | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-C05 | Reconnect after a network loss | C | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-C06 | Handle a change of access while connected | C | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-C07 | See who is typing | C | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 2; not built. |
| UC-CHT-C08 | See who is online | C | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 2; not built. |
| UC-CHT-C09 | Receive unread and mention badges on a per-user stream | C | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-D01 | See unread counts per space and in total | D | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-D02 | Mark a space as read | D | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-D03 | Open a space at the first unread message | D | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-D04 | See mention badges separately | D | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-D05 | Mute a space | D | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-D06 | Set the notification level for a space | D | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-D07 | Mark everything as read | D | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 1; not built. |
| UC-CHT-E01 | Start a direct conversation | E | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 2; not built. |
| UC-CHT-E02 | Start a group conversation | E | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 2; not built. |
| UC-CHT-E03 | Add people to a group conversation | E | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 2; not built. |
| UC-CHT-E04 | Leave a group conversation | E | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 2; not built. |
| UC-CHT-E05 | List my conversations | E | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 2; not built. |
| UC-CHT-E06 | Mute or block a person in direct messages | E | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 2; not built. |
| UC-CHT-E07 | Hide and restore a conversation from my list | E | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 2; not built. |
| UC-CHT-F01 | Reply in a thread | F | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 2; not built. |
| UC-CHT-F02 | Quote-reply in the main flow | F | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 2; not built. |
| UC-CHT-F03 | Open a thread and read its replies | F | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 2; not built. |
| UC-CHT-F04 | Follow or mute a thread | F | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 2; not built. |
| UC-CHT-F05 | Archive an inactive thread and reopen it on reply | F | System | no | no | no | no | DEFINED | Phase 2; not built. |
| UC-CHT-F06 | Pin a message | F | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 2; not built. |
| UC-CHT-F07 | Unpin and list pins | F | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 2; not built. |
| UC-CHT-F08 | List the threads I follow | F | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 2; not built. |
| UC-CHT-G01 | Mention a person | G | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 2; not built. |
| UC-CHT-G02 | Warn when a mention cannot be seen | G | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 2; not built. |
| UC-CHT-G03 | Broadcast with @channel or @here | G | Workspace Owner, Admin, Member with the broadcast action | no | no | no | no | DEFINED | Phase 2; not built. |
| UC-CHT-G04 | Link to another channel with # | G | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 2; not built. |
| UC-CHT-G05 | Link a Board item or Page in a message | G | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 2; not built. |
| UC-CHT-G06 | React to a message | G | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 2; not built. |
| UC-CHT-G07 | Remove a reaction and see who reacted | G | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 2; not built. |
| UC-CHT-G08 | Attach a file to a message | G | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 2; not built. |
| UC-CHT-G09 | Hand a notification intent to the Inbox | G | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 2; not built. |
| UC-CHT-H01 | Override channel permissions by role | H | Workspace Owner, Admin, Channel manager | no | no | no | no | DEFINED | Phase 3; not built. |
| UC-CHT-H02 | Guest access to chat | H | Owner, Admin | no | no | no | no | DEFINED | Phase 3; not built. |
| UC-CHT-H03 | Delete another person's message as a moderator | H | Roles per owner decision | no | no | no | no | DEFINED | Phase 3; not built. |
| UC-CHT-H04 | Emit Audit facts for chat administration | H | System | no | no | no | no | DEFINED | Phase 3; not built. |
| UC-CHT-H05 | Apply retention and purge | H | System | no | no | no | no | DEFINED | Phase 3; not built. |
| UC-CHT-H06 | Export a person's chat data | H | Owner, Admin (privacy request) | no | no | no | no | DEFINED | Phase 3; not built. |
| UC-CHT-H07 | Search messages | H | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 3; not built. |
| UC-CHT-H08 | Rate limit and abuse controls | H | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 3; not built. |
| UC-CHT-H09 | Email digest for missed mentions | H | Workspace Owner, Admin, Member | no | no | no | no | DEFINED | Phase 3; not built. |
| UC-CHT-H10 | Link a channel to a Board, Page or Team | H | Channel manager | no | no | no | no | DEFINED | Phase 3; not built. |

## 3. Use cases

### Capability A: CHT-001 Channels and channel groups

### UC-CHT-A01 Create a channel
- Capability: A, CHT-001 Channels and channel groups
- Actors and permission: Workspace Owner, Admin, Member; permission chat.channel.create.
- Preconditions: Caller is a Workspace member; the Workspace is under its channel limit.
- Input: Name (1 to 80 characters, unique per Workspace among active channels), kind public or private, optional topic, optional group, optional ResourceRef link.
- Output: A Channel (Active) with the creator as manager and member; a channel-created fact; a system message in the channel.
- Main flow: 1. Caller opens New channel. 2. POST /api/v1/workspaces/{workspaceId}/chat/channels with a client request id. 3. Pipeline authorizes chat.channel.create. 4. Handler stores the Channel and the first Membership, sequence 1 is the system message. 5. Sidebar adds the channel.
- No-op / rejection / concurrency / retry: Duplicate name: 409. Empty or too long name: 400. Limit reached: 422 with the limit. Retry with the same request id returns the same channel. Other Workspace: identical NotFound.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-001, CHT-002, CHT-003, CHT-018.
- Acceptance: Given a Member, when they create a public channel, then every non-Guest member sees it in Browse channels. Given a duplicate name, then 409 and nothing is stored.
- Evidence: BE planned POST /api/v1/workspaces/{workspaceId}/chat/channels (Chat.Channels.Create) · FE planned chat channel create dialog, sidebar · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-A02 Create and manage a channel group
- Capability: A, CHT-001 Channels and channel groups
- Actors and permission: Workspace Owner, Admin, Member with chat.group.manage.
- Preconditions: Workspace member with the manage action.
- Input: Group name (1 to 60), optional position.
- Output: A ChannelGroup (category) that orders channels; no access meaning.
- Main flow: 1. Caller chooses New category. 2. POST /api/v1/workspaces/{workspaceId}/chat/channel-groups. 3. Sidebar shows the empty category.
- No-op / rejection / concurrency / retry: Duplicate name in the Workspace: 409. No manage action: 403.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-001, CHT-003.
- Acceptance: Given an Admin, when they create a category and move a channel into it, then the sidebar shows the channel under it for everyone who can see the channel.
- Evidence: BE planned POST/PATCH/DELETE /api/v1/workspaces/{workspaceId}/chat/channel-groups (Chat.ChannelGroups.*) · FE planned chat sidebar category · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-A03 Rename a channel and set its topic
- Capability: A, CHT-001 Channels and channel groups
- Actors and permission: Channel manager, Workspace Admin, Owner.
- Preconditions: The channel is Active.
- Input: New name and/or topic, expected version.
- Output: Updated Channel; a system message in the channel; channel-updated fact.
- Main flow: 1. Manager edits channel settings. 2. PATCH /api/v1/workspaces/{workspaceId}/chat/channels/{id} with expected version. 3. Header and sidebar update live.
- No-op / rejection / concurrency / retry: Stale version: 412 with the current version. Duplicate name: 409. Not a manager: 403 (private channel non-member: NotFound).
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-001, CHT-018.
- Acceptance: Given two managers editing at once, then the second save gets 409 and sees the first change.
- Evidence: BE planned PATCH /api/v1/workspaces/{workspaceId}/chat/channels/{id} (Chat.Channels.Update) · FE planned channel settings panel · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-A04 Reorder channels and groups
- Capability: A, CHT-001 Channels and channel groups
- Actors and permission: Workspace Owner, Admin, Member; manage rights for shared order, anyone for personal collapse.
- Preconditions: Channels or groups exist.
- Input: New position, collapsed state.
- Output: Order stored for the Workspace; collapse stored per person.
- Main flow: 1. Caller drags a channel. 2. PATCH positions. 3. Others see the order live.
- No-op / rejection / concurrency / retry: Stale order version: 412, client refetches. No manage action on shared order: 403.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-001.
- Acceptance: Given a collapsed category, when the page reloads, then it stays collapsed for that person only.
- Evidence: BE planned PATCH /api/v1/workspaces/{workspaceId}/chat/channels/order, /api/v1/workspaces/{workspaceId}/chat/me/sidebar (Chat.Channels.Reorder) · FE planned sidebar drag and collapse · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-A05 Browse and join a public channel
- Capability: A, CHT-001 Channels and channel groups
- Actors and permission: Workspace Owner, Admin, Member.
- Preconditions: The channel is public, Active and in the caller's Workspace.
- Input: Channel id.
- Output: Membership created; a system message announces the join unless the owner decision says otherwise.
- Main flow: 1. Caller opens Browse channels. 2. GET /api/v1/workspaces/{workspaceId}/chat/channels?kind=public. 3. Join: POST /api/v1/workspaces/{workspaceId}/chat/channels/{id}/members/me.
- No-op / rejection / concurrency / retry: Private or unknown channel: identical NotFound. Guest: 403. Already a member: 200, no change.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-003, CHT-004.
- Acceptance: Given a public channel, when a Member joins, then the channel appears in their sidebar and its history is readable from the first message.
- Evidence: BE planned GET /api/v1/workspaces/{workspaceId}/chat/channels, POST /api/v1/workspaces/{workspaceId}/chat/channels/{id}/members/me (Chat.Channels.Join) · FE planned browse channels dialog · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-A06 Create a private channel and invite people
- Capability: A, CHT-001 Channels and channel groups
- Actors and permission: Workspace Owner, Admin, Member; chat.channel.create then chat.member.invite.
- Preconditions: People invited are Workspace members; Guests only by explicit add.
- Input: Name, kind private, invitee ids.
- Output: A private Channel with Memberships; invitees get a notification intent (UC-CHT-G09).
- Main flow: 1. Caller creates a private channel. 2. POST /api/v1/workspaces/{workspaceId}/chat/channels/{id}/members with user ids. 3. Invitees see it in their sidebar.
- No-op / rejection / concurrency / retry: Invitee not in the Workspace: skipped and reported. Not a member of a private channel: NotFound.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-003, CHT-004, CHT-019.
- Acceptance: Given a private channel, then Workspace Admins who are not members cannot list it or read its messages.
- Evidence: BE planned POST /api/v1/workspaces/{workspaceId}/chat/channels/{id}/members (Chat.Members.Add) · FE planned channel invite picker · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-A07 Leave a channel or remove a member
- Capability: A, CHT-001 Channels and channel groups
- Actors and permission: Workspace Owner, Admin, Member; managers remove others.
- Preconditions: The caller is a member, or a manager of the channel.
- Input: User id (self when leaving).
- Output: Membership ended; live subscription closed (UC-CHT-C06); a system message in public channels.
- Main flow: 1. Caller chooses Leave or Remove. 2. DELETE /api/v1/workspaces/{workspaceId}/chat/channels/{id}/members/{userId}. 3. Access-changed reaches the removed person's connections.
- No-op / rejection / concurrency / retry: Last manager leaving: 422 unless another manager exists. Not a member: 200 no change.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-004, CHT-017.
- Acceptance: Given a removed member with the channel open, then within one re-authorization interval or at once by access-changed the pane shows lost access and caches are cleared.
- Evidence: BE planned DELETE /api/v1/workspaces/{workspaceId}/chat/channels/{id}/members/{userId} (Chat.Members.Remove) · FE planned member list, lost-access state · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-A08 Archive, unarchive and delete a channel
- Capability: A, CHT-001 Channels and channel groups
- Actors and permission: Channel manager, Workspace Admin, Owner.
- Preconditions: Channel exists.
- Input: Action, expected version.
- Output: Archived: read-only. Deleted: soft delete with recovery window.
- Main flow: 1. Manager chooses Archive or Delete in settings. 2. POST /api/v1/workspaces/{workspaceId}/chat/channels/{id}/archive or DELETE. 3. Realtime tells clients the space changed.
- No-op / rejection / concurrency / retry: Stale version: 412. Delete needs typed name confirmation in the UI. Not a manager: 403.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-018, CHT-021.
- Acceptance: Given an archived channel, then posting returns 409 channel-archived and history remains readable and searchable by members.
- Evidence: BE planned POST /api/v1/workspaces/{workspaceId}/chat/channels/{id}/archive, DELETE /api/v1/workspaces/{workspaceId}/chat/channels/{id} (Chat.Channels.Archive, Delete) · FE planned channel settings danger zone · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### Capability B: CHT-002 Messages

### UC-CHT-B01 Send a message
- Capability: B, CHT-002 Messages
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to; permission chat.message.post.
- Preconditions: Caller is a member of the space; the space is Active; the Workspace is not suspended.
- Input: ClientMessageId, content 1 to 4,000 characters after trim or an attachment, optional reply reference, optional mentions.
- Output: A Message with the next sequence; a message-created fact; realtime event; notification intents for mentions.
- Main flow: 1. Caller types and sends. 2. The client shows a temporary message. 3. POST /api/v1/workspaces/{workspaceId}/chat/spaces/{id}/messages with the idempotency key. 4. Handler checks membership, assigns the sequence in the same transaction as the insert, writes the outbox fact. 5. The client reconciles the temporary id to the sequence.
- No-op / rejection / concurrency / retry: Empty or too long: 400. No access or other Workspace: identical NotFound. Archived: 409. Duplicate ClientMessageId: 200 with the stored message. Rate limit: 429 with retry-after.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-001, CHT-005, CHT-006, CHT-008, CHT-020.
- Acceptance: Given a member, when they send twice with the same ClientMessageId, then one message exists. Given two members sending at once, then both get distinct, gap-free sequences.
- Evidence: BE planned POST /api/v1/workspaces/{workspaceId}/chat/spaces/{spaceId}/messages (Chat.Messages.Send) · FE planned chat composer, message list · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-B02 Load history with a cursor
- Capability: B, CHT-002 Messages
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: Caller can view the space.
- Input: Cursor (before or after a sequence), page size up to 100.
- Output: Messages ordered by sequence with authors, reactions summary, thread counts and tombstones.
- Main flow: 1. Caller scrolls up. 2. GET /api/v1/workspaces/{workspaceId}/chat/spaces/{id}/messages?before=&limit=. 3. The list prepends without moving the scroll position.
- No-op / rejection / concurrency / retry: No access: NotFound. Invalid cursor: 400. Empty space: empty page and an empty state.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-002, CHT-005.
- Acceptance: Given 10,000 messages, then the first page loads within the performance budget and older pages load on scroll.
- Evidence: BE planned GET /api/v1/workspaces/{workspaceId}/chat/spaces/{spaceId}/messages (Chat.Messages.List) · FE planned virtualized message list, skeleton, empty state · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-B03 Jump to a message
- Capability: B, CHT-002 Messages
- Actors and permission: Any member who can view the space.
- Preconditions: The target message is in the space.
- Input: Message id or sequence.
- Output: The list loads a window around the message and highlights it.
- Main flow: 1. Caller opens a message link or a search hit. 2. GET /api/v1/workspaces/{workspaceId}/chat/spaces/{id}/messages?around=. 3. The list scrolls and highlights.
- No-op / rejection / concurrency / retry: Unknown or tombstoned id: shows the nearest messages and a notice. No access: NotFound.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-002, CHT-005.
- Acceptance: Given a link to a message in a private channel the caller cannot see, then the response is the same NotFound as for a missing one.
- Evidence: BE planned GET /api/v1/workspaces/{workspaceId}/chat/spaces/{spaceId}/messages?around= (Chat.Messages.List) · FE planned jump highlight · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-B04 Edit a message
- Capability: B, CHT-002 Messages
- Actors and permission: Author only.
- Preconditions: Message not deleted; space Active; edit window per owner decision.
- Input: Content, expected version.
- Output: Message with edited marker and a new version; a message-updated fact.
- Main flow: 1. Author chooses Edit. 2. PATCH /api/v1/workspaces/{workspaceId}/chat/messages/{id} with expected version. 3. Everyone sees the edit live.
- No-op / rejection / concurrency / retry: Not the author: 403 (non-member: NotFound). Stale version: 412. Empty: 400.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-007, CHT-008.
- Acceptance: Given an edit, then readers see the new text and an edited marker, and the sequence is unchanged.
- Evidence: BE planned PATCH /api/v1/workspaces/{workspaceId}/chat/messages/{id} (Chat.Messages.Edit) · FE planned inline editor, edited marker · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-B05 Delete my message
- Capability: B, CHT-002 Messages
- Actors and permission: Author.
- Preconditions: Message exists.
- Input: Message id, expected version.
- Output: Tombstone: content removed, author, time and sequence kept; a message-deleted fact.
- Main flow: 1. Author chooses Delete and confirms. 2. DELETE /api/v1/workspaces/{workspaceId}/chat/messages/{id}. 3. The message becomes a tombstone live for everyone.
- No-op / rejection / concurrency / retry: Not the author: 403 (moderators use UC-CHT-H03). Already deleted: 200 no change.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-007, CHT-021.
- Acceptance: Given a deleted root message with replies, then the thread stays visible with a tombstone parent.
- Evidence: BE planned DELETE /api/v1/workspaces/{workspaceId}/chat/messages/{id} (Chat.Messages.Delete) · FE planned delete confirm, tombstone row · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-B06 Recover from a failed send
- Capability: B, CHT-002 Messages
- Actors and permission: Workspace Owner, Admin, Member.
- Preconditions: A send failed or the page lost its connection.
- Input: The pending message.
- Output: The message is sent once, or the user is told it was not sent and keeps the text.
- Main flow: 1. The client keeps the pending message with its ClientMessageId. 2. On reconnect or Retry it resends with the same key. 3. The server returns the stored message if it already exists.
- No-op / rejection / concurrency / retry: Permanent failure (no access, archived): the message stays as a draft with the reason. Network failure: retries with backoff, bounded.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-006.
- Acceptance: Given a send whose response was lost, when the client retries, then one message exists.
- Evidence: BE planned POST /api/v1/workspaces/{workspaceId}/chat/spaces/{spaceId}/messages (same key) · FE planned pending, failed, retry states · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-B07 Keep a draft per space
- Capability: B, CHT-002 Messages
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: None.
- Input: Text typed in the composer.
- Output: The draft is restored when returning to the space; it never leaves the device.
- Main flow: 1. Caller types and switches space. 2. The client stores the draft locally keyed by space. 3. The draft returns on re-entry.
- No-op / rejection / concurrency / retry: Storage unavailable: drafts work in memory only. Sent or discarded draft is cleared.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-008.
- Acceptance: Given a draft in channel A, when the caller visits B and returns, then the draft is intact.
- Evidence: BE planned none (client only) · FE planned composer draft · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-B08 Render content safely
- Capability: B, CHT-002 Messages
- Actors and permission: Any reader.
- Preconditions: A message is displayed.
- Input: Message content.
- Output: Text with the allowed markup (bold, italic, code, lists, quotes), autolinked URLs with safe attributes and previews only for permitted hosts.
- Main flow: 1. Client renders through the shared safe-render contract (COL-029). 2. Mentions render from identities, not text.
- No-op / rejection / concurrency / retry: Disallowed markup is shown as text. A mention of a removed user shows the stored name.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-008, CHT-010.
- Acceptance: Given content with script or event attributes, then nothing executes and the text is shown inert.
- Evidence: BE planned none (render contract) · FE planned message body renderer · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-B09 See system announcements
- Capability: B, CHT-002 Messages
- Actors and permission: Workspace Owner, Admin, Member.
- Preconditions: A system-relevant change happens.
- Input: Join, leave, rename, privacy change, archive.
- Output: A system message with its own kind and sequence, attributed to the actor.
- Main flow: 1. A command stores a system message in the same transaction. 2. It appears in the list with muted style.
- No-op / rejection / concurrency / retry: System messages cannot be edited or deleted by users.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-005, CHT-018.
- Acceptance: Given a rename, then a system message names the actor and the new name.
- Evidence: BE planned (emitted by Chat.Channels.* commands) · FE planned system message row · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-B10 Copy a link to a message
- Capability: B, CHT-002 Messages
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: The caller can view the message.
- Input: Message id.
- Output: A link that opens the space at the message if the opener has access.
- Main flow: 1. Caller chooses Copy link. 2. The client builds /workspaces/{id}/chat/{space}?m={sequence}.
- No-op / rejection / concurrency / retry: Opening it without access: the same NotFound state as for a missing message.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-002, CHT-004.
- Acceptance: Given a link shared outside the channel, then a non-member learns nothing about the message.
- Evidence: BE planned none (client builds URL; GET per UC-CHT-B03) · FE planned message actions menu · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### Capability C: CHT-003 Realtime delivery

### UC-CHT-C01 Subscribe to a space
- Capability: C, CHT-003 Realtime delivery
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: Caller can view the space; fewer than 50 subscriptions on the connection.
- Input: Subscribe frame with scope kind channel and the space id.
- Output: Subscribed frame with the current stream version, or a subscription-error.
- Main flow: 1. Client opens a space. 2. Subscribe frame. 3. Gateway authorizes through Chat's authorization query. 4. Stream version returned.
- No-op / rejection / concurrency / retry: No access: subscription-error without detail. Limit reached: the oldest idle subscription is dropped by the client. Frame over 4096 bytes: connection error.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-015, CHT-001.
- Acceptance: Given a non-member, then subscribe returns the same error as for an unknown space.
- Evidence: BE planned WS /api/v1/realtime subscribe (scope channel) · FE planned connection indicator · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-C02 Receive a new message live
- Capability: C, CHT-003 Realtime delivery
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: Subscribed to the space.
- Input: Event frame with the compact message payload and stream version.
- Output: The message appears at the end of the list with no refetch.
- Main flow: 1. A message is stored and the outbox fact is mapped. 2. The gateway fans out to subscribers. 3. The client inserts by sequence and dedupes by ClientMessageId.
- No-op / rejection / concurrency / retry: Duplicate: ignored. Gap: see UC-CHT-C04.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-005, CHT-015.
- Acceptance: Given two readers, when a third sends, then both show it in under the latency budget.
- Evidence: BE planned WS event (chat.message.created) · FE planned message list live insert · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-C03 Receive an edit or delete live
- Capability: C, CHT-003 Realtime delivery
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: Subscribed to the space.
- Input: Event frames for updated and deleted messages with the version.
- Output: The row changes in place; a tombstone replaces a deleted message.
- Main flow: 1. Fact mapped to an event. 2. The client patches the cache by id and ignores older versions.
- No-op / rejection / concurrency / retry: Event for an unknown message: ignored and the window refetches if it is in range.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-007, CHT-015.
- Acceptance: Given a delete while the reader scrolls, then the row turns into a tombstone without moving the list.
- Evidence: BE planned WS event (chat.message.updated, chat.message.deleted) · FE planned in-place update · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-C04 Recover from a gap, duplicate or out-of-order event
- Capability: C, CHT-003 Realtime delivery
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: Subscribed to the space.
- Input: Stream versions and sequences.
- Output: The list is consistent with the server.
- Main flow: 1. The client tracks last stream version. 2. A jump means a gap: it queries messages after the last known sequence. 3. Duplicates and older versions are dropped.
- No-op / rejection / concurrency / retry: Repeated gaps: the client drops the cache for the space and reloads the newest page.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-015, CHT-005.
- Acceptance: Given a dropped frame, then the missing message appears after the recovery query and no message is shown twice.
- Evidence: BE planned GET /api/v1/workspaces/{workspaceId}/chat/spaces/{id}/messages?after= · FE planned gap recovery · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-C05 Reconnect after a network loss
- Capability: C, CHT-003 Realtime delivery
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: The socket closed or slept.
- Input: Backoff reconnect; resubscribe all open spaces.
- Output: Spaces are consistent; unread and badges are current.
- Main flow: 1. Client reconnects with jitter. 2. Resubscribes. 3. Runs the gap query per open space and a badge refresh.
- No-op / rejection / concurrency / retry: Auth expired: sign-in flow. Connection lifetime (1 h): silent reconnect.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-015.
- Acceptance: Given an offline minute, then returning shows every missed message once.
- Evidence: BE planned WS /api/v1/realtime · FE planned offline banner · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-C06 Handle a change of access while connected
- Capability: C, CHT-003 Realtime delivery
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: A member's access changed.
- Input: access-changed frame.
- Output: The subscription closes, the space shows lost access and local caches of it are cleared.
- Main flow: 1. Membership, role or Workspace change produces an access signal. 2. Gateway re-evaluates and closes affected subscriptions. 3. The client clears the space.
- No-op / rejection / concurrency / retry: Spurious signal: re-evaluation keeps access and resubscribes.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-017.
- Acceptance: Given a removed member with the channel open, then no event for that channel reaches them after the signal.
- Evidence: BE planned WS access-changed · FE planned lost-access state · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-C07 See who is typing
- Capability: C, CHT-003 Realtime delivery
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: Subscribed; space Active.
- Input: Typing frame from the client, throttled.
- Output: A typing indicator for up to 5 seconds after the last signal.
- Main flow: 1. Composer sends typing at most every 3 seconds. 2. Gateway fans out to space subscribers except the sender. 3. Indicator expires.
- No-op / rejection / concurrency / retry: Rate exceeded: the frame is dropped. Not authorized: ignored.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-016, CHT-020.
- Acceptance: Given one person typing, then others see one indicator that disappears when they send or stop.
- Evidence: BE planned WS typing frame · FE planned typing indicator · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 2 — not built.

### UC-CHT-C08 See who is online
- Capability: C, CHT-003 Realtime delivery
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: Connected.
- Input: Presence heartbeat; state online, idle, offline.
- Output: Presence dots in member lists, DM list and avatars; never used for authorization.
- Main flow: 1. Client reports activity. 2. Gateway keeps presence in memory or Redis with expiry. 3. Subscribers of the space or conversation receive changes.
- No-op / rejection / concurrency / retry: Gateway restart: presence rebuilds from heartbeats; no durable record.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-016.
- Acceptance: Given a closed tab, then the person shows offline after the expiry and nothing is stored.
- Evidence: BE planned WS presence frame · FE planned presence dot · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 2 — not built.

### UC-CHT-C09 Receive unread and mention badges on a per-user stream
- Capability: C, CHT-003 Realtime delivery
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: Connected.
- Input: Per-user stream events with the space id and counts.
- Output: Rail and sidebar badges update without opening each space.
- Main flow: 1. A message in a space the person belongs to updates their counters. 2. The per-user stream notifies. 3. The client updates badges.
- No-op / rejection / concurrency / retry: Muted spaces update the count silently and do not badge.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-009, CHT-015, CHT-019.
- Acceptance: Given a message in a background channel, then its badge increments within the latency budget.
- Evidence: BE planned WS user stream (chat.unread.changed) · FE planned rail and sidebar badges · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### Capability D: CHT-004 Read state and notification levels

### UC-CHT-D01 See unread counts per space and in total
- Capability: D, CHT-004 Read state and notification levels
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: Member of the spaces.
- Input: None.
- Output: Per-space unread count and a total on the rail icon.
- Main flow: 1. GET /api/v1/workspaces/{workspaceId}/chat/me/unread. 2. Counts are last sequence minus read boundary, excluding own messages and muted spaces for the total.
- No-op / rejection / concurrency / retry: Large counts display as 99+.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-009.
- Acceptance: Given 120 unread, then the badge shows 99+ and the divider still finds the first unread.
- Evidence: BE planned GET /api/v1/workspaces/{workspaceId}/chat/me/unread (Chat.ReadState.Summary) · FE planned badges · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-D02 Mark a space as read
- Capability: D, CHT-004 Read state and notification levels
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: The space is open and visible.
- Input: Read boundary sequence.
- Output: The boundary moves forward only.
- Main flow: 1. The client reports the highest visible sequence after a short delay. 2. PUT /api/v1/workspaces/{workspaceId}/chat/spaces/{id}/read. 3. Counts update on all the person's devices.
- No-op / rejection / concurrency / retry: A lower sequence is a no-op. No access: NotFound.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-009.
- Acceptance: Given two devices, then reading on one clears the badge on the other.
- Evidence: BE planned PUT /api/v1/workspaces/{workspaceId}/chat/spaces/{spaceId}/read (Chat.ReadState.Mark) · FE planned badges · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-D03 Open a space at the first unread message
- Capability: D, CHT-004 Read state and notification levels
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: Unread exists.
- Input: None.
- Output: The list opens at a New messages divider with a jump to latest.
- Main flow: 1. Client reads the boundary. 2. Loads messages around boundary + 1. 3. Draws the divider.
- No-op / rejection / concurrency / retry: Boundary older than retained history: opens at the oldest retained message.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-009, CHT-005.
- Acceptance: Given 40 unread, when the person opens the channel, then the divider is on the first unread message.
- Evidence: BE planned GET /api/v1/workspaces/{workspaceId}/chat/spaces/{id}/messages?around= · FE planned new messages divider · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-D04 See mention badges separately
- Capability: D, CHT-004 Read state and notification levels
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: A message mentions the person.
- Input: None.
- Output: A mention count per space that persists until the mention is read.
- Main flow: 1. Mention facts increment a per-space mention counter. 2. Reading past it clears it.
- No-op / rejection / concurrency / retry: A mention in a muted space counts but does not badge.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-009, CHT-010.
- Acceptance: Given a mention in a background channel, then the sidebar shows a distinct mention badge.
- Evidence: BE planned GET /api/v1/workspaces/{workspaceId}/chat/me/unread · FE planned mention badge · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-D05 Mute a space
- Capability: D, CHT-004 Read state and notification levels
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: Member.
- Input: Space id, optional until time.
- Output: No notification or badge for the space; history still readable.
- Main flow: 1. Caller chooses Mute. 2. PUT /api/v1/workspaces/{workspaceId}/chat/spaces/{id}/notification-level with none and until.
- No-op / rejection / concurrency / retry: Past until time: no-op. Mentions still count unless the level is none.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-019.
- Acceptance: Given a muted channel, then a mention shows no push or email but shows in the space's own mention count.
- Evidence: BE planned PUT /api/v1/workspaces/{workspaceId}/chat/spaces/{spaceId}/notification-level (Chat.ReadState.SetLevel) · FE planned mute menu · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-D06 Set the notification level for a space
- Capability: D, CHT-004 Read state and notification levels
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: Member.
- Input: Level all, mentions, none.
- Output: The level is used by notification intents (UC-CHT-G09).
- Main flow: 1. Caller opens space notification settings. 2. PUT the level.
- No-op / rejection / concurrency / retry: Invalid level: 400.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-019.
- Acceptance: Given level mentions, then plain messages produce no notification and mentions do.
- Evidence: BE planned PUT /api/v1/workspaces/{workspaceId}/chat/spaces/{spaceId}/notification-level (Chat.ReadState.SetLevel) · FE planned notification settings · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### UC-CHT-D07 Mark everything as read
- Capability: D, CHT-004 Read state and notification levels
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: Member.
- Input: Optional group.
- Output: Every boundary in scope moves to the latest sequence.
- Main flow: 1. Caller chooses Mark all read. 2. POST /api/v1/workspaces/{workspaceId}/chat/me/read-all.
- No-op / rejection / concurrency / retry: Concurrent new messages after the call stay unread.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-009.
- Acceptance: Given new messages arriving during the call, then only older ones are marked.
- Evidence: BE planned POST /api/v1/workspaces/{workspaceId}/chat/me/read-all (Chat.ReadState.MarkAll) · FE planned mark all read · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 1 — not built.

### Capability E: CHT-005 Direct and group conversations

### UC-CHT-E01 Start a direct conversation
- Capability: E, CHT-005 Direct and group conversations
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to if the other person shares a space.
- Preconditions: Both people are active in the Workspace.
- Input: Other user id.
- Output: The one direct Conversation for the pair; created if absent.
- Main flow: 1. Caller opens a profile or the new message picker. 2. POST /api/v1/workspaces/{workspaceId}/chat/conversations with kind direct. 3. The existing conversation is returned if there is one.
- No-op / rejection / concurrency / retry: Yourself: 400. Person outside the Workspace or blocked: NotFound. Race: both calls return the same conversation.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-003, CHT-014, CHT-004.
- Acceptance: Given two simultaneous starts between the same pair, then one conversation exists.
- Evidence: BE planned POST /api/v1/workspaces/{workspaceId}/chat/conversations (Chat.Conversations.Start) · FE planned new message picker, DM list · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 2 — not built.

### UC-CHT-E02 Start a group conversation
- Capability: E, CHT-005 Direct and group conversations
- Actors and permission: Workspace Owner, Admin, Member.
- Preconditions: Participants are active Workspace members; count within the cap.
- Input: 2 to N other user ids, optional name.
- Output: A group Conversation with fixed participants.
- Main flow: 1. Caller picks people. 2. POST /api/v1/workspaces/{workspaceId}/chat/conversations with kind group.
- No-op / rejection / concurrency / retry: Over the cap: 422. A participant outside the Workspace: skipped and reported.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-003, CHT-014.
- Acceptance: Given three people, then each sees the group in their list and no other Admin does.
- Evidence: BE planned POST /api/v1/workspaces/{workspaceId}/chat/conversations (Chat.Conversations.Start) · FE planned group creation · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 2 — not built.

### UC-CHT-E03 Add people to a group conversation
- Capability: E, CHT-005 Direct and group conversations
- Actors and permission: Workspace Owner, Admin, Member.
- Preconditions: Caller is a participant; the group is under the cap.
- Input: User ids.
- Output: New participants see history from their join point (owner decision may allow full history).
- Main flow: 1. Caller opens participants. 2. POST /api/v1/workspaces/{workspaceId}/chat/conversations/{id}/participants.
- No-op / rejection / concurrency / retry: Over the cap: 422. Not a participant: NotFound.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-014, CHT-004.
- Acceptance: Given an added person, then they cannot read messages before they joined unless the decision allows.
- Evidence: BE planned POST /api/v1/workspaces/{workspaceId}/chat/conversations/{id}/participants (Chat.Conversations.AddParticipants) · FE planned participants panel · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 2 — not built.

### UC-CHT-E04 Leave a group conversation
- Capability: E, CHT-005 Direct and group conversations
- Actors and permission: Workspace Owner, Admin, Member.
- Preconditions: Caller is a participant.
- Input: Conversation id.
- Output: The person is removed; a system message tells the group.
- Main flow: 1. Caller chooses Leave. 2. DELETE /api/v1/workspaces/{workspaceId}/chat/conversations/{id}/participants/me.
- No-op / rejection / concurrency / retry: Last participant leaving: the conversation closes and follows retention.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-014, CHT-017.
- Acceptance: Given a person who left, then no event for the group reaches them.
- Evidence: BE planned DELETE /api/v1/workspaces/{workspaceId}/chat/conversations/{id}/participants/me (Chat.Conversations.Leave) · FE planned leave confirm · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 2 — not built.

### UC-CHT-E05 List my conversations
- Capability: E, CHT-005 Direct and group conversations
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: None.
- Input: Cursor.
- Output: Conversations ordered by last activity with last message preview, unread and presence.
- Main flow: 1. GET /api/v1/workspaces/{workspaceId}/chat/conversations. 2. Realtime keeps the order current.
- No-op / rejection / concurrency / retry: Hidden conversations are excluded.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-014, CHT-009.
- Acceptance: Given a new message, then its conversation moves to the top.
- Evidence: BE planned GET /api/v1/workspaces/{workspaceId}/chat/conversations (Chat.Conversations.List) · FE planned DM list · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 2 — not built.

### UC-CHT-E06 Mute or block a person in direct messages
- Capability: E, CHT-005 Direct and group conversations
- Actors and permission: Workspace Owner, Admin, Member.
- Preconditions: A direct conversation exists.
- Input: User id, action.
- Output: Mute: no notifications. Block: the person cannot start a new direct conversation or notify the blocker.
- Main flow: 1. Caller chooses Mute or Block. 2. PUT /api/v1/workspaces/{workspaceId}/chat/me/blocks.
- No-op / rejection / concurrency / retry: Blocking does not hide public channel messages; it is stated in the UI.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-014, CHT-020.
- Acceptance: Given a block, then new direct conversation attempts by that person return NotFound.
- Evidence: BE planned PUT /api/v1/workspaces/{workspaceId}/chat/me/blocks (Chat.Conversations.Block) · FE planned block menu · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 2 — not built.

### UC-CHT-E07 Hide and restore a conversation from my list
- Capability: E, CHT-005 Direct and group conversations
- Actors and permission: Workspace Owner, Admin, Member.
- Preconditions: Conversation exists.
- Input: Conversation id.
- Output: Hidden for this person only; a new message restores it.
- Main flow: 1. Caller chooses Close. 2. PUT /api/v1/workspaces/{workspaceId}/chat/conversations/{id}/visibility.
- No-op / rejection / concurrency / retry: None.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-014.
- Acceptance: Given a hidden conversation, when a new message arrives, then it returns to the list.
- Evidence: BE planned PUT /api/v1/workspaces/{workspaceId}/chat/conversations/{id}/visibility (Chat.Conversations.SetVisibility) · FE planned close conversation · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 2 — not built.

### Capability F: CHT-006 Threads, replies and pins

### UC-CHT-F01 Reply in a thread
- Capability: F, CHT-006 Threads, replies and pins
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: The root message is not deleted without replies; the space is Active.
- Input: Root message id, content, ClientMessageId.
- Output: A Message with ThreadRootId; the root shows reply count and last reply; the author follows the thread.
- Main flow: 1. Caller chooses Reply in thread. 2. POST /api/v1/workspaces/{workspaceId}/chat/messages/{rootId}/replies. 3. The panel and the root row update live.
- No-op / rejection / concurrency / retry: Archived thread: reopens. Deleted root with no replies: 409. Same rules as UC-CHT-B01.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-013, CHT-006.
- Acceptance: Given a reply, then the root shows 1 reply and followers get a thread-reply intent.
- Evidence: BE planned POST /api/v1/workspaces/{workspaceId}/chat/messages/{rootId}/replies (Chat.Threads.Reply) · FE planned thread panel · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 2 — not built.

### UC-CHT-F02 Quote-reply in the main flow
- Capability: F, CHT-006 Threads, replies and pins
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: The quoted message is visible.
- Input: Quoted message id and content.
- Output: A Message with ReplyToId and a quote preview.
- Main flow: 1. Caller chooses Reply. 2. POST message with replyTo.
- No-op / rejection / concurrency / retry: Quoted message deleted later: the preview shows a tombstone.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-005, CHT-007.
- Acceptance: Given a quote of a deleted message, then the preview says it was deleted and no content leaks.
- Evidence: BE planned POST /api/v1/workspaces/{workspaceId}/chat/spaces/{id}/messages (replyTo) · FE planned quote preview · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 2 — not built.

### UC-CHT-F03 Open a thread and read its replies
- Capability: F, CHT-006 Threads, replies and pins
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: Caller can view the space.
- Input: Root id, cursor.
- Output: Replies ordered by sequence with the root on top.
- Main flow: 1. Caller opens the thread. 2. GET /api/v1/workspaces/{workspaceId}/chat/messages/{rootId}/replies. 3. Subscribes through the space subscription.
- No-op / rejection / concurrency / retry: No access: NotFound.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-013.
- Acceptance: Given a thread with 200 replies, then the panel loads in pages.
- Evidence: BE planned GET /api/v1/workspaces/{workspaceId}/chat/messages/{rootId}/replies (Chat.Threads.List) · FE planned thread panel list · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 2 — not built.

### UC-CHT-F04 Follow or mute a thread
- Capability: F, CHT-006 Threads, replies and pins
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: Thread exists.
- Input: Thread id, follow state.
- Output: Follow state per person; followers get thread-reply intents.
- Main flow: 1. Caller toggles Follow. 2. PUT /api/v1/workspaces/{workspaceId}/chat/threads/{id}/follow.
- No-op / rejection / concurrency / retry: Muted threads produce no notification.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-013, CHT-019.
- Acceptance: Given a muted thread, then no notification is created for its replies.
- Evidence: BE planned PUT /api/v1/workspaces/{workspaceId}/chat/threads/{id}/follow (Chat.Threads.Follow) · FE planned follow toggle · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 2 — not built.

### UC-CHT-F05 Archive an inactive thread and reopen it on reply
- Capability: F, CHT-006 Threads, replies and pins
- Actors and permission: System.
- Preconditions: Thread has had no reply for the configured period.
- Input: None.
- Output: Thread marked archived; a new reply reopens it.
- Main flow: 1. A sweep archives threads. 2. A reply reopens one.
- No-op / rejection / concurrency / retry: Archived threads stay readable and searchable.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-013.
- Acceptance: Given an archived thread, when someone replies, then it is open again.
- Evidence: BE planned (background sweep, Chat.Threads.Archive) · FE planned archived thread chip · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 2 — not built.

### UC-CHT-F06 Pin a message
- Capability: F, CHT-006 Threads, replies and pins
- Actors and permission: Workspace Owner, Admin, Member; pin action.
- Preconditions: The message is visible and not deleted.
- Input: Message id.
- Output: A Pin on the space; a system message.
- Main flow: 1. Caller chooses Pin. 2. PUT /api/v1/workspaces/{workspaceId}/chat/messages/{id}/pin.
- No-op / rejection / concurrency / retry: Over the pin limit: 422. Not allowed: 403.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-001, CHT-007.
- Acceptance: Given a pinned message that is later deleted, then the pin is removed.
- Evidence: BE planned PUT /api/v1/workspaces/{workspaceId}/chat/messages/{id}/pin (Chat.Pins.Pin) · FE planned pin action, pinned list · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 2 — not built.

### UC-CHT-F07 Unpin and list pins
- Capability: F, CHT-006 Threads, replies and pins
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: Caller can view the space.
- Input: None.
- Output: Pinned messages ordered by pin time.
- Main flow: 1. GET /api/v1/workspaces/{workspaceId}/chat/spaces/{id}/pins. 2. DELETE removes a pin.
- No-op / rejection / concurrency / retry: None.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-001.
- Acceptance: Given a pin, then every member sees it in the pins panel.
- Evidence: BE planned GET /api/v1/workspaces/{workspaceId}/chat/spaces/{spaceId}/pins, DELETE /api/v1/workspaces/{workspaceId}/chat/messages/{id}/pin (Chat.Pins.*) · FE planned pins panel · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 2 — not built.

### UC-CHT-F08 List the threads I follow
- Capability: F, CHT-006 Threads, replies and pins
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: None.
- Input: Cursor.
- Output: Threads with unread replies ordered by last reply.
- Main flow: 1. GET /api/v1/workspaces/{workspaceId}/chat/me/threads.
- No-op / rejection / concurrency / retry: None.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-013, CHT-009.
- Acceptance: Given a thread with new replies, then it shows an unread dot.
- Evidence: BE planned GET /api/v1/workspaces/{workspaceId}/chat/me/threads (Chat.Threads.Mine) · FE planned threads view · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 2 — not built.

### Capability G: CHT-007 Mentions, reactions, attachments and notification handoff

### UC-CHT-G01 Mention a person
- Capability: G, CHT-007 Mentions, reactions, attachments and notification handoff
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to for people in the space.
- Preconditions: The composer is open.
- Input: Typed @ and a query; selected user.
- Output: A stored mention by stable identity; a mention intent.
- Main flow: 1. Caller types @. 2. The picker queries space members. 3. Send stores MentionedUserIds. 4. A mention fact follows.
- No-op / rejection / concurrency / retry: Unknown user: shown as text. Rate limit: 429.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-010.
- Acceptance: Given a mention, then the target gets a mention badge and notification by their preferences.
- Evidence: BE planned GET /api/v1/workspaces/{workspaceId}/chat/spaces/{id}/mentionables, POST message with mentions (Chat.Mentions.Resolve) · FE planned mention picker · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 2 — not built.

### UC-CHT-G02 Warn when a mention cannot be seen
- Capability: G, CHT-007 Mentions, reactions, attachments and notification handoff
- Actors and permission: Workspace Owner, Admin, Member.
- Preconditions: A mentioned person cannot view the space.
- Input: Mentioned ids.
- Output: The composer warns before send; the mention is stored and notifies nobody.
- Main flow: 1. The picker marks non-members. 2. Send shows a warning with Invite or Send anyway.
- No-op / rejection / concurrency / retry: Invite requires the invite action.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-010, CHT-004.
- Acceptance: Given a mention of a non-member, then no notification is created and nothing is granted.
- Evidence: BE planned POST /api/v1/workspaces/{workspaceId}/chat/spaces/{id}/messages (mention check) · FE planned mention warning · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 2 — not built.

### UC-CHT-G03 Broadcast with @channel or @here
- Capability: G, CHT-007 Mentions, reactions, attachments and notification handoff
- Actors and permission: Workspace Owner, Admin, Member with the broadcast action.
- Preconditions: The space has more than one member.
- Input: Token @channel or @here.
- Output: Every member is notified (@channel) or every present member (@here).
- Main flow: 1. Caller types the token. 2. The UI confirms above the member threshold. 3. The server checks the action and fans out intents.
- No-op / rejection / concurrency / retry: No action: the token is plain text with a notice. Guests cannot broadcast.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-010, CHT-020.
- Acceptance: Given a Guest typing @channel, then it is not a broadcast.
- Evidence: BE planned POST message with broadcast (Chat.Mentions.Broadcast) · FE planned broadcast confirm · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 2 — not built.

### UC-CHT-G04 Link to another channel with #
- Capability: G, CHT-007 Mentions, reactions, attachments and notification handoff
- Actors and permission: Workspace Owner, Admin, Member.
- Preconditions: Target channel is visible to the sender.
- Input: Typed # and channel.
- Output: A link chip to the channel; no access is granted.
- Main flow: 1. Picker lists visible channels. 2. The chip renders for readers who can see it and as plain text otherwise.
- No-op / rejection / concurrency / retry: Private channel names are not revealed to non-members.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-010, CHT-004.
- Acceptance: Given a link to a private channel, then non-members see only the plain text.
- Evidence: BE planned GET /api/v1/workspaces/{workspaceId}/chat/channels (picker) · FE planned channel link chip · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 2 — not built.

### UC-CHT-G05 Link a Board item or Page in a message
- Capability: G, CHT-007 Mentions, reactions, attachments and notification handoff
- Actors and permission: Workspace Owner, Admin, Member.
- Preconditions: The resource is visible to the sender.
- Input: Resource reference.
- Output: A link preview with the title only if the reader can view the resource.
- Main flow: 1. Caller pastes a product URL. 2. The client resolves a ResourceRef. 3. Readers resolve the preview with their own access.
- No-op / rejection / concurrency / retry: No access: a generic link without title. The resource is never changed.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-023.
- Acceptance: Given a link to an item the reader cannot see, then the preview shows no title.
- Evidence: BE planned GET /api/v1/workspaces/{workspaceId}/chat/links/resolve (Chat.Links.Resolve) · FE planned link preview · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 2 — not built.

### UC-CHT-G06 React to a message
- Capability: G, CHT-007 Mentions, reactions, attachments and notification handoff
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: The message is visible and the space is Active.
- Input: Message id, reaction kind.
- Output: One reaction per message, actor and kind; a reaction fact.
- Main flow: 1. Caller picks an emoji. 2. PUT /api/v1/workspaces/{workspaceId}/chat/messages/{id}/reactions/{kind}.
- No-op / rejection / concurrency / retry: Repeat: 200 no change. Archived: 409.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-011.
- Acceptance: Given a double click, then one reaction exists.
- Evidence: BE planned PUT /api/v1/workspaces/{workspaceId}/chat/messages/{id}/reactions/{kind} (Chat.Reactions.Add) · FE planned reaction bar, picker · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 2 — not built.

### UC-CHT-G07 Remove a reaction and see who reacted
- Capability: G, CHT-007 Mentions, reactions, attachments and notification handoff
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: A reaction exists.
- Input: Message id, kind.
- Output: The caller's reaction is removed; the reactor list is readable.
- Main flow: 1. DELETE removes. 2. GET lists reactors on hover.
- No-op / rejection / concurrency / retry: Removing a missing reaction: 200.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-011.
- Acceptance: Given a removal, then the count drops live.
- Evidence: BE planned DELETE /api/v1/workspaces/{workspaceId}/chat/messages/{id}/reactions/{kind} (Chat.Reactions.Remove) · FE planned reactor list · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 2 — not built.

### UC-CHT-G08 Attach a file to a message
- Capability: G, CHT-007 Mentions, reactions, attachments and notification handoff
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: The upload passes the shared file policy.
- Input: File, message.
- Output: An Attachment with target kind chat.message; the message shows it.
- Main flow: 1. Caller picks a file. 2. The shared attachment flow stores the object. 3. The message references it.
- No-op / rejection / concurrency / retry: Too large or blocked type: 400 with the reason. A download URL is short-lived and permission checked per request.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-012.
- Acceptance: Given a blocked extension, then it is refused before upload completes.
- Evidence: BE planned POST /api/v1/workspaces/{workspaceId}/chat/spaces/{id}/messages (attachments), shared attachments API · FE planned attachment chips · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 2 — not built.

### UC-CHT-G09 Hand a notification intent to the Inbox
- Capability: G, CHT-007 Mentions, reactions, attachments and notification handoff
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: A mention, direct message, thread reply or broadcast occurs.
- Input: Intent with recipient and reason.
- Output: An Inbox notification by the recipient's preferences and level.
- Main flow: 1. A consumer maps the chat fact to a notification intent. 2. Collaboration applies preferences and writes the notification. 3. The bell updates.
- No-op / rejection / concurrency / retry: Recipient cannot view the space: no notification (checked at delivery). Muted: none. Duplicate fact: one notification.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-019, CHT-010.
- Acceptance: Given a mention of a person who left the channel before delivery, then no notification is delivered.
- Evidence: BE planned (consumer) Chat.Notifications.Intent · FE planned Inbox item for a chat message · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 2 — not built.

### Capability H: CHT-008 Access, moderation, retention, search and hardening

### UC-CHT-H01 Override channel permissions by role
- Capability: H, CHT-008 Access, moderation, retention, search and hardening
- Actors and permission: Workspace Owner, Admin, Channel manager.
- Preconditions: The channel exists.
- Input: Role and action overrides (post, react, attach, broadcast, invite).
- Output: Effective permissions per member computed from the Workspace role and overrides.
- Main flow: 1. Manager opens channel permissions. 2. PUT /api/v1/workspaces/{workspaceId}/chat/channels/{id}/permissions.
- No-op / rejection / concurrency / retry: Cannot remove the last manager. Cannot grant above the caller's own rights.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-004, CHT-001.
- Acceptance: Given an announcements channel where Members cannot post, then their composer is read-only.
- Evidence: BE planned PUT /api/v1/workspaces/{workspaceId}/chat/channels/{id}/permissions (Chat.Channels.Permissions) · FE planned channel permissions panel · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 3 — not built.

### UC-CHT-H02 Guest access to chat
- Capability: H, CHT-008 Access, moderation, retention, search and hardening
- Actors and permission: Owner, Admin; Guest only in spaces they were added to.
- Preconditions: A Guest exists in the Workspace.
- Input: Spaces the Guest is added to.
- Output: The Guest sees only those spaces, no member directory beyond them, and cannot broadcast or create channels.
- Main flow: 1. Admin adds the Guest to a channel. 2. The Guest's sidebar lists only it.
- No-op / rejection / concurrency / retry: A Guest asking for a public channel not added: NotFound.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-004.
- Acceptance: Given a Guest, then the channel browser is empty and the member list shows only the space's members.
- Evidence: BE planned GET /api/v1/workspaces/{workspaceId}/chat/channels (guest filter) · FE planned guest sidebar · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 3 — not built.

### UC-CHT-H03 Delete another person's message as a moderator
- Capability: H, CHT-008 Access, moderation, retention, search and hardening
- Actors and permission: Roles per owner decision.
- Preconditions: Moderate action granted.
- Input: Message id, reason.
- Output: A tombstone marked removed by a moderator; an Audit fact.
- Main flow: 1. Moderator chooses Remove. 2. DELETE /api/v1/workspaces/{workspaceId}/chat/messages/{id}?moderation=true with a reason.
- No-op / rejection / concurrency / retry: No action: 403. The author is notified.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-007, CHT-024.
- Acceptance: Given a moderator delete, then the tombstone shows it and Audit records actor, message and reason.
- Evidence: BE planned DELETE /api/v1/workspaces/{workspaceId}/chat/messages/{id} (Chat.Messages.Moderate) · FE planned moderate dialog · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 3 — not built.

### UC-CHT-H04 Emit Audit facts for chat administration
- Capability: H, CHT-008 Access, moderation, retention, search and hardening
- Actors and permission: System.
- Preconditions: An administrative action happens.
- Input: Fact.
- Output: Governance Audit entries for channel created, privacy changed, member added or removed, moderator delete and retention changes.
- Main flow: 1. A command writes the fact to the outbox. 2. Governance consumes it.
- No-op / rejection / concurrency / retry: Fact failure does not lose the action: the outbox retries.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-024.
- Acceptance: Given a privacy change, then Audit shows actor, channel and old and new kind.
- Evidence: BE planned (outbox) Chat.Audit.Facts · FE planned audit entries · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 3 — not built.

### UC-CHT-H05 Apply retention and purge
- Capability: H, CHT-008 Access, moderation, retention, search and hardening
- Actors and permission: System; Owner configures.
- Preconditions: A retention rule exists.
- Input: Policy per Workspace.
- Output: Messages past retention become tombstones or are purged as configured; purges are audited.
- Main flow: 1. A job selects expired messages. 2. It applies the rule in batches. 3. Facts and Audit are written.
- No-op / rejection / concurrency / retry: Legal or recovery holds skip a space. A failed batch resumes.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-021, CHT-018.
- Acceptance: Given a Workspace with a 90-day rule, then older messages are removed from reads and search.
- Evidence: BE planned (background job) Chat.Retention.Apply · FE planned retention settings · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 3 — not built.

### UC-CHT-H06 Export a person's chat data
- Capability: H, CHT-008 Access, moderation, retention, search and hardening
- Actors and permission: Owner, Admin (privacy request).
- Preconditions: A privacy or data request exists.
- Input: User id, range.
- Output: An export of the person's authored messages and membership, subject to policy.
- Main flow: 1. Admin starts an export. 2. A job builds it. 3. A short-lived download is offered.
- No-op / rejection / concurrency / retry: Private spaces the requester cannot see are excluded unless policy says otherwise.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-021, CHT-004.
- Acceptance: Given an export, then it contains only what the policy permits and is audited.
- Evidence: BE planned POST /api/v1/workspaces/{workspaceId}/chat/exports (Chat.Exports.Create) · FE planned export request · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 3 — not built.

### UC-CHT-H07 Search messages
- Capability: H, CHT-008 Access, moderation, retention, search and hardening
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: Caller has access to at least one space.
- Input: Query, filters (space, author, date, has attachment).
- Output: Results restricted to spaces the caller can view now.
- Main flow: 1. Caller searches. 2. GET /api/v1/workspaces/{workspaceId}/chat/search. 3. The server filters by current membership.
- No-op / rejection / concurrency / retry: Empty query: 400. Removed from a space: its messages stop appearing.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-022, CHT-004.
- Acceptance: Given a person removed from a private channel, then their search no longer returns its messages.
- Evidence: BE planned GET /api/v1/workspaces/{workspaceId}/chat/search (Chat.Search.Query) · FE planned search panel · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 3 — not built.

### UC-CHT-H08 Rate limit and abuse controls
- Capability: H, CHT-008 Access, moderation, retention, search and hardening
- Actors and permission: Workspace Owner, Admin, Member; Guest only in spaces they were added to.
- Preconditions: A person sends or reacts quickly.
- Input: Counts per window.
- Output: Over-limit calls return 429 with retry-after; spaces can slow mode.
- Main flow: 1. A limiter checks per person and per space. 2. The UI shows a retry notice and keeps the draft.
- No-op / rejection / concurrency / retry: Admins are not exempt from broadcast limits.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-020.
- Acceptance: Given a burst of 100 messages, then the excess returns 429 and nothing is lost from the draft.
- Evidence: BE planned (pipeline) rate limit policy chat.* · FE planned rate limit notice · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 3 — not built.

### UC-CHT-H09 Email digest for missed mentions
- Capability: H, CHT-008 Access, moderation, retention, search and hardening
- Actors and permission: Workspace Owner, Admin, Member.
- Preconditions: The person has unread mentions and email enabled.
- Input: Preferences.
- Output: A digest email by the Collaboration preference matrix.
- Main flow: 1. A job groups missed mention intents. 2. Collaboration sends the digest.
- No-op / rejection / concurrency / retry: Muted and removed-from-space items are excluded.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-019.
- Acceptance: Given three missed mentions, then one digest lists them.
- Evidence: BE planned (job) Chat.Notifications.Digest · FE planned digest preferences · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 3 — not built.

### UC-CHT-H10 Link a channel to a Board, Page or Team
- Capability: H, CHT-008 Access, moderation, retention, search and hardening
- Actors and permission: Channel manager.
- Preconditions: The resource is visible to the manager.
- Input: ResourceRef.
- Output: The channel shows a link chip; resource pages can show a Discuss entry point for linked channels.
- Main flow: 1. Manager sets the link. 2. PUT /api/v1/workspaces/{workspaceId}/chat/channels/{id}/link.
- No-op / rejection / concurrency / retry: No access to the resource: 403. The resource is never changed.
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline, optimistic temporary id.
- Rules: Product CHT-023.
- Acceptance: Given a link, then a Board's header offers Discuss and opens the channel only for members.
- Evidence: BE planned PUT /api/v1/workspaces/{workspaceId}/chat/channels/{id}/link (Chat.Channels.Link) · FE planned linked resource chip · Design none yet · Tests planned, see the test matrix of docs/workstreams/teams/chat.md · Real e2e none yet
- State: DEFINED, Phase 3 — not built.

## 4. Product decisions applied

- Product decision (owner, 2026-11-10): item 1, Chat is Discord-style: channels in categories, public and private, direct and group conversations, threads, mentions, reactions, attachments, unread state and presence.
- Product decision (owner, 2026-11-10): item 2, realtime extends the existing gateway (Channel scope, message payload frames, typing and presence); writes stay REST.
- Product decision (owner, 2026-11-10): item 3, delivery in three phases: core (1), interaction (2), advanced (3). Voice and video are out of scope.
- Lead decision (2026-11-10) L1: Chat is a separate bounded context (SYS-ADR-001).
- Lead decision (2026-11-10) L2: presence and typing stay in gateway memory or Redis, in line with Collaboration L16.

## 5. Canonical updates required

- `docs/product/collaboration.md`: add Chat to Does not own and amend the sentence that says there is no separate chat channel.
- `docs/workstreams/use-cases/collaboration.md`: UC-COL-E02 (live unread count) and UC-COL-E03 (presence) cover resources only; chat unread and presence are UC-CHT-C08, C09 and D01.
- `docs/product/product-model.md`, `docs/architecture/bounded-context-map.md`, `PRODUCT.md`, `CONTEXT-MAP.md`, `CONTEXT.md`, `docs/product/README.md`, topic authority map: register the twelfth context.
- `docs/architecture/events-realtime-and-delivery-boundary.md` and `frontend/docs/architecture/realtime.md`: protocol v1.1.
- Governance: permission actions `chat.*` and resource kinds `chat.channel`, `chat.conversation`.

## 6. Not covered

Voice, video, stage channels, screen sharing and calls; bots and incoming webhooks; end-to-end encryption; cross-Workspace shared channels; message scheduling and reminders; message translation; mobile push delivery details (handled by Notifications).

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

Dates: this catalog was written on 2026-11-10 as a specification and recounted on 2026-11-13 against the code. A use case is HARDENING only when its backend, mock handler, UI and tests all exist (checked by operation, handler, component and test file); where one is missing the use case stays DEFINED and its State line says what is missing. The owner sets DONE; no agent does.

The product rules are in `docs/product/chat.md` (CHT-001 to CHT-024). The capability letters map to the team capabilities CHT-001 to CHT-008 of `docs/workstreams/teams/chat.md`; these are catalog coordinates, not product rule IDs.

## 1. Assessment

### 1.1 State per capability

| Letter | Capability | Use cases | DEFINED | IMPLEMENTING | INTEGRATING | HARDENING | DONE | BLOCKED |
|---|---|---|---|---|---|---|---|---|
| A | CHT-001 Channels and channel groups | 8 | 4 | 0 | 0 | 4 | 0 | 0 |
| B | CHT-002 Messages | 10 | 3 | 0 | 0 | 7 | 0 | 0 |
| C | CHT-003 Realtime delivery | 9 | 0 | 0 | 0 | 9 | 0 | 0 |
| D | CHT-004 Read state and notification levels | 7 | 1 | 0 | 0 | 6 | 0 | 0 |
| E | CHT-005 Direct and group conversations | 7 | 0 | 0 | 0 | 7 | 0 | 0 |
| F | CHT-006 Threads, replies and pins | 8 | 1 | 0 | 0 | 7 | 0 | 0 |
| G | CHT-007 Mentions, reactions, attachments and notification handoff | 9 | 0 | 0 | 0 | 9 | 0 | 0 |
| H | CHT-008 Access, moderation, retention, search and hardening | 10 | 0 | 0 | 0 | 10 | 0 | 0 |
| | Total | 68 | 9 | 0 | 0 | 59 | 0 | 0 |

The counts are those of the index below (section 2), counted by State on 2026-11-13: 59 HARDENING, 9 DEFINED, none DONE. The nine DEFINED rows have built parts but are not complete: A02 (no UI to create, rename or delete a category), A04 (no reorder or collapse UI), A07 (no member removal or channel leave UI), A08 (no unarchive or delete UI), B07 (draft kept in memory only and untested), B08 (plain text only, no markup or autolinks), B10 (no Copy link), D07 (no Mark all as read entry), F05 (no persisted sweep and no archived chip). Capabilities I and J are specified in the catalog change of its own pull request; they are not in this count.

### 1.2 Gaps

1. The nine DEFINED rows above, with what each lacks.
2. No real e2e, design record, axe record or owner check exists for any Chat use case; the owner tests and sets DONE.
3. The operations runbook, the performance budgets and the load test (typing and presence included) are not done (see the delivery plan).
4. A thread's reply count never goes down: a deleted reply keeps its thread sequence and stays counted, so the "N replies" chip counts tombstones.
5. The category names are read with a separate list call (Chat.ChannelGroups.List) and refreshed on the stale interval, not pushed by a realtime frame.

### 1.3 Defects in shipped code

None recorded for Chat at this recount. The static "coming soon" route and the unused legacy sidebar entry named by the first draft of this catalog were replaced by the Chat workspace.

### 1.4 Enterprise baseline

A team chat is expected to provide ordered, idempotent delivery, unread state across devices, mentions with privacy, retention and export, moderation with Audit, search filtered by access, rate limits and accessible live regions. Phase 3 closes the baseline.

## 2. Index

| ID | Use case | Capability | Actors | BE | FE | Design | Real e2e | State | Gap |
|---|---|---|---|---|---|---|---|---|---|
| UC-CHT-A01 | Create a channel | A | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-A02 | Create and manage a channel group | A | Workspace Owner, Admin, Member with chat.group.manage | yes | partial | no | no | DEFINED | BE and mock exist (Chat.ChannelGroups.Create, Update, Delete, List) and the sidebar shows each category by its real name (UI read of Chat.ChannelGroups.List); the UI has no entry to create, rename or delete a category, and the web service has no write method for them. |
| UC-CHT-A03 | Rename a channel and set its topic | A | Channel manager, Workspace Admin, Owner | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-A04 | Reorder channels and groups | A | Workspace Owner, Admin, Member | yes | no | no | no | DEFINED | BE and mock exist (Chat.Channels.Reorder, ReorderChannels); the UI has no drag to reorder, no category order and no per-person collapse, and the web service has no reorder method. |
| UC-CHT-A05 | Browse and join a public channel | A | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-A06 | Create a private channel and invite people | A | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-A07 | Leave a channel or remove a member | A | Workspace Owner, Admin, Member | yes | no | no | no | DEFINED | BE and mock exist (Chat.Members.Remove); the web service has removeMember but no hook or component uses it: no member list with Remove and no Leave channel entry. Leaving a conversation (UC-CHT-E04) is built. |
| UC-CHT-A08 | Archive, unarchive and delete a channel | A | Channel manager, Workspace Admin, Owner | yes | partial | no | no | DEFINED | BE and mock exist (Chat.Channels.Archive, Chat.Channels.Delete); the UI archives a channel from the channel settings but has no unarchive entry and no delete entry (the web service has no deleteChannel). |
| UC-CHT-B01 | Send a message | B | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-B02 | Load history with a cursor | B | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-B03 | Jump to a message | B | Any member who can view the space | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-B04 | Edit a message | B | Author only | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-B05 | Delete my message | B | Author | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-B06 | Recover from a failed send | B | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-B07 | Keep a draft per space | B | Workspace Owner, Admin, Member | no | partial | no | no | DEFINED | The per-space draft is kept in memory by the workspace page and returns when the caller re-enters the space, but no test covers it and it is not kept across a reload (the package may not use browser storage; a host-provided store is an owner decision). |
| UC-CHT-B08 | Render content safely | B | Any reader | yes | partial | no | no | DEFINED | The UI draws message text as plain React text with mention chips (inert by construction) but has no bold, italic, code, lists or quotes, no autolinks and no permitted-host previews for plain URLs, and no test of the script or event-attribute case. |
| UC-CHT-B09 | See system announcements | B | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-B10 | Copy a link to a message | B | Workspace Owner, Admin, Member | yes | no | no | no | DEFINED | No Copy link action exists in the message menu and the client builds no ?m= link; opening a message by URL is covered by UC-CHT-B03 only. |
| UC-CHT-C01 | Subscribe to a space | C | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-C02 | Receive a new message live | C | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-C03 | Receive an edit or delete live | C | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-C04 | Recover from a gap, duplicate or out-of-order event | C | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-C05 | Reconnect after a network loss | C | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-C06 | Handle a change of access while connected | C | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-C07 | See who is typing | C | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. No load test with typing and presence. |
| UC-CHT-C08 | See who is online | C | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. No load test with typing and presence. |
| UC-CHT-C09 | Receive unread and mention badges on a per-user stream | C | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. The latency budget is not measured. |
| UC-CHT-D01 | See unread counts per space and in total | D | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-D02 | Mark a space as read | D | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-D03 | Open a space at the first unread message | D | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-D04 | See mention badges separately | D | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-D05 | Mute a space | D | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-D06 | Set the notification level for a space | D | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-D07 | Mark everything as read | D | Workspace Owner, Admin, Member | yes | no | no | no | DEFINED | BE and mock exist (Chat.ReadState.MarkAllRead); the web service and the useMarkAllRead hook exist but no component calls it: there is no Mark all as read entry. |
| UC-CHT-E01 | Start a direct conversation | E | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-E02 | Start a group conversation | E | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-E03 | Add people to a group conversation | E | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-E04 | Leave a group conversation | E | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-E05 | List my conversations | E | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-E06 | Mute or block a person in direct messages | E | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | Muting a conversation is its notification level (UC-CHT-D05). The block list is read from the server (Chat.Blocks.List); a Guest sees only the blocks of people they share a space with. No real e2e, design record, owner check or axe record. |
| UC-CHT-E07 | Hide and restore a conversation from my list | E | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-F01 | Reply in a thread | F | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-F02 | Quote-reply in the main flow | F | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-F03 | Open a thread and read its replies | F | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | The root carries threadReplyCount and threadLastReplyAt, read in one batched query per page, so the chip shows after a reload; a deleted reply stays counted (the count is the thread sequence). No real e2e, design record, owner check or axe record. |
| UC-CHT-F04 | Follow or mute a thread | F | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-F05 | Archive an inactive thread and reopen it on reply | F | System | yes | partial | no | no | DEFINED | No sweep stores the archive: a thread reads as archived by the derived rule (inactive for the configured days) and a reply reopens it (MessageThread.RecordReply, ThreadHandlerTests); the summary carries archived but the UI shows no archived chip. |
| UC-CHT-F06 | Pin a message | F | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-F07 | Unpin and list pins | F | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-F08 | List the threads I follow | F | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-G01 | Mention a person | G | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-G02 | Warn when a mention cannot be seen | G | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-G03 | Broadcast with @channel or @here | G | Workspace Owner, Admin, Member with the broadcast action | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. An @here gives no Inbox notification (no presence data). |
| UC-CHT-G04 | Link to another channel with # | G | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-G05 | Link a Board item or Page in a message | G | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | Built (S2.8); the owner tests and sets DONE. |
| UC-CHT-G06 | React to a message | G | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-G07 | Remove a reaction and see who reacted | G | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-G08 | Attach a file to a message | G | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. |
| UC-CHT-G09 | Hand a notification intent to the Inbox | G | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | Built (S2.8); @here gives no Inbox notification (no presence); the owner tests and sets DONE. |
| UC-CHT-H01 | Override channel permissions by role | H | Workspace Owner, Admin, Channel manager | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. An allow override is accepted but changes nothing (a deny is the only effect); custom-role grants are not in the capability flags. |
| UC-CHT-H02 | Guest access to chat | H | Owner, Admin | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. Chat search (UC-CHT-H07) is built, so the Guest search check is testable and pinned by tests. Three leaks were found and fixed (see Readings). |
| UC-CHT-H03 | Delete another person's message as a moderator | H | Roles per owner decision | yes | yes | no | no | HARDENING | Owner decision open: moderators are Workspace Owner and Admin (and a custom role with the moderate action); channel managers are not. The author is not notified (UC-CHT-G09 is not built). No real e2e, design record, owner check or axe record. |
| UC-CHT-H04 | Emit Audit facts for chat administration | H | System | yes | partial | no | no | HARDENING | No Audit screen entries for Chat yet; no fact for a privacy or member-role change (no such Chat command exists); self-join and self-leave are not audited. |
| UC-CHT-H05 | Apply retention and purge | H | System; Owner configures | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. Legal hold does not exist in the platform, so only the recovery hold (deleted spaces) skips a space; the purge of a deleted space after its recovery window is not built; the search hook is satisfied by UC-CHT-H07 (see Readings 11). |
| UC-CHT-H06 | Export a person's chat data | H | Owner, Admin (privacy request) | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. There is no list of past exports (create, status and download only); the 20 MiB and 50,000-message bounds are not tuned against real data. |
| UC-CHT-H07 | Search messages | H | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. Search is a live phrase match, not ranked and not indexed: a very large Workspace scans (a trigram index is the known gap). |
| UC-CHT-H08 | Rate limit and abuse controls | H | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. The limits are the defaults of RateLimiting:Chat, not tuned against load; a thread reply and a reaction show the generic slow-down notice, not the countdown. |
| UC-CHT-H09 | Email digest for missed mentions | H | Workspace Owner, Admin, Member | yes | yes | no | no | HARDENING | No real e2e, design record, owner check, axe record or ops runbook. No real mail was sent (the digest is queued on the shared outbox and checked there; the dispatcher send is the existing path and was not run against a real mail server); a Guest gets no digest; one window is reported, with no catch-up of windows the job missed; the link opens the channel, not the message. |
| UC-CHT-H10 | Link a channel to a Board, Page or Team | H | Channel manager | yes | yes | no | no | HARDENING | No real e2e, design record, owner check or axe record. A Team cannot be linked yet (Governance publishes no Team visibility fact); the Discuss entry is wired on the Board header only, the Page header has no slot for it (see Readings). |

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
- Evidence: BE POST /api/v1/workspaces/{workspaceId}/chat/channels (Chat.Channels.Create, CreateChannelCommand) · FE CreateChannelDialog (frontend/packages/features/chat/src/web/components/chat-dialogs.tsx), the sidebar New channel entry · Mock frontend/packages/dev/mock-backend/src/contexts/chat/channels.handlers.ts (chat.channels.create) · Tests backend/tests/Notrelix.Application.Tests/Features/Chat/ChannelHandlerTests.cs, ChatChannelRlsTests, chat-handlers.unit.test.ts · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE planned POST/PATCH/DELETE /api/v1/workspaces/{workspaceId}/chat/channel-groups (Chat.ChannelGroups.*) · FE planned chat sidebar category · Design none yet · Tests exist for the built part (see the note); the use case as a whole is not tested · Real e2e none yet
- State: DEFINED — Not complete: BE and mock exist (Chat.ChannelGroups.Create, Update, Delete, List) and the sidebar shows each category by its real name (UI read of Chat.ChannelGroups.List); the UI has no entry to create, rename or delete a category, and the web service has no write method for them.

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
- Evidence: BE PATCH /api/v1/workspaces/{workspaceId}/chat/channels/{id} (Chat.Channels.Update, UpdateChannelCommand) · FE RenameChannelDialog (frontend/packages/features/chat/src/web/components/chat-dialogs.tsx) · Mock frontend/packages/dev/mock-backend/src/contexts/chat/channels.handlers.ts (chat.channels.update) · Tests backend/tests/Notrelix.Application.Tests/Features/Chat/ChannelHandlerTests.cs, ChatChannelRlsTests, chat-handlers.unit.test.ts · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE planned PATCH /api/v1/workspaces/{workspaceId}/chat/channels/order, /api/v1/workspaces/{workspaceId}/chat/me/sidebar (Chat.Channels.Reorder) · FE planned sidebar drag and collapse · Design none yet · Tests exist for the built part (see the note); the use case as a whole is not tested · Real e2e none yet
- State: DEFINED — Not complete: BE and mock exist (Chat.Channels.Reorder, ReorderChannels); the UI has no drag to reorder, no category order and no per-person collapse, and the web service has no reorder method.

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
- Evidence: BE GET /api/v1/workspaces/{workspaceId}/chat/channels?browse=true (Chat.Channels.List) and PUT .../channels/{id}/members/me (Chat.Members.Join) · FE the browse section of chat-sidebar.tsx and the join action · Mock frontend/packages/dev/mock-backend/src/contexts/chat/channels.handlers.ts, frontend/packages/dev/mock-backend/src/contexts/chat/members.handlers.ts (chat.members.join) · Tests backend/tests/Notrelix.Application.Tests/Features/Chat/ChannelHandlerTests.cs, ChatChannelRlsTests, GuestAccessHandlerTests, chat-sidebar.component.test.tsx, chat-handlers.unit.test.ts · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE POST /api/v1/workspaces/{workspaceId}/chat/channels/{id}/members (Chat.Members.Add, AddChannelMembersCommand) and kind private on create · FE the invite picker (useAddMembers in chat-workspace.tsx, chat-person-picker.tsx) and the private option of CreateChannelDialog · Mock frontend/packages/dev/mock-backend/src/contexts/chat/members.handlers.ts (chat.members.add) · Tests backend/tests/Notrelix.Application.Tests/Features/Chat/ChannelHandlerTests.cs, ChatChannelRlsTests, chat-handlers.unit.test.ts · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE planned DELETE /api/v1/workspaces/{workspaceId}/chat/channels/{id}/members/{userId} (Chat.Members.Remove) · FE planned member list, lost-access state · Design none yet · Tests exist for the built part (see the note); the use case as a whole is not tested · Real e2e none yet
- State: DEFINED — Not complete: BE and mock exist (Chat.Members.Remove); the web service has removeMember but no hook or component uses it: no member list with Remove and no Leave channel entry. Leaving a conversation (UC-CHT-E04) is built.

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
- Evidence: BE planned POST /api/v1/workspaces/{workspaceId}/chat/channels/{id}/archive, DELETE /api/v1/workspaces/{workspaceId}/chat/channels/{id} (Chat.Channels.Archive, Delete) · FE planned channel settings danger zone · Design none yet · Tests exist for the built part (see the note); the use case as a whole is not tested · Real e2e none yet
- State: DEFINED — Not complete: BE and mock exist (Chat.Channels.Archive, Chat.Channels.Delete); the UI archives a channel from the channel settings but has no unarchive entry and no delete entry (the web service has no deleteChannel).

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
- Evidence: BE POST /api/v1/workspaces/{workspaceId}/chat/spaces/{spaceId}/messages (Chat.Messages.Send, SendMessageCommand) · FE chat-composer.tsx and chat-message-list.tsx · Mock frontend/packages/dev/mock-backend/src/contexts/chat/messages.handlers.ts (chat.messages.send) · Tests MessageHandlerTests, ChatMessageConcurrencyIntegrationTests, ChatMessageRlsTests, chat-handlers.unit.test.ts, chat-composer.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE GET /api/v1/workspaces/{workspaceId}/chat/spaces/{spaceId}/messages (Chat.Messages.List, ListMessagesQuery) · FE chat-message-list.tsx (older and newer paging, skeleton and empty states) · Mock frontend/packages/dev/mock-backend/src/contexts/chat/messages.handlers.ts (chat.messages.list) · Tests MessageHandlerTests, chat-handlers.unit.test.ts, chat-message-list.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE GET .../messages?around= (Chat.Messages.List) · FE chat-highlight-context.tsx and the jump in chat-workspace.tsx · Mock frontend/packages/dev/mock-backend/src/contexts/chat/messages.handlers.ts (around) · Tests MessageHandlerTests, chat-handlers.unit.test.ts, message-cache.unit.test.ts, chat-model.unit.test.ts · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE PATCH /api/v1/workspaces/{workspaceId}/chat/messages/{id} (Chat.Messages.Edit) · FE the inline editor and edited marker (chat-message-list.tsx, chat-message-row.tsx) · Mock frontend/packages/dev/mock-backend/src/contexts/chat/messages.handlers.ts (chat.messages.edit) · Tests MessageHandlerTests, chat-handlers.unit.test.ts, create-chat-hooks.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE DELETE /api/v1/workspaces/{workspaceId}/chat/messages/{id} (Chat.Messages.Delete) · FE ConfirmDialog (frontend/packages/features/chat/src/web/components/chat-dialogs.tsx) and the tombstone row · Mock frontend/packages/dev/mock-backend/src/contexts/chat/messages.handlers.ts (chat.messages.delete) · Tests MessageHandlerTests, chat-handlers.unit.test.ts, chat-message-list.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE POST .../spaces/{spaceId}/messages with the same clientMessageId (idempotent retry) · FE pending, failed and retry rows (chat-message-list.tsx, ChatPendingRow; onRetryPending) · Mock frontend/packages/dev/mock-backend/src/contexts/chat/messages.handlers.ts, chat-writes.ts (idempotent create) · Tests MessageHandlerTests (retry with the same key), chat-model.unit.test.ts, chat-message-list.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE planned none (client only) · FE planned composer draft · Design none yet · Tests exist for the built part (see the note); the use case as a whole is not tested · Real e2e none yet
- State: DEFINED — Not complete: The per-space draft is kept in memory by the workspace page and returns when the caller re-enters the space, but no test covers it and it is not kept across a reload (the package may not use browser storage; a host-provided store is an owner decision).

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
- Evidence: BE planned none (render contract) · FE planned message body renderer · Design none yet · Tests exist for the built part (see the note); the use case as a whole is not tested · Real e2e none yet
- State: DEFINED — Not complete: The UI draws message text as plain React text with mention chips (inert by construction) but has no bold, italic, code, lists or quotes, no autolinks and no permitted-host previews for plain URLs, and no test of the script or event-attribute case.

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
- Evidence: BE emitted by the Chat.Channels.* and member commands (ChatSystemMessages) · FE the system row of chat-message-row.tsx · Mock frontend/packages/dev/mock-backend/src/contexts/chat/chat-writes.ts (system messages) · Tests ChannelHandlerTests, chat-handlers.unit.test.ts, chat-model.unit.test.ts · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE planned none (client builds URL; GET per UC-CHT-B03) · FE planned message actions menu · Design none yet · Tests exist for the built part (see the note); the use case as a whole is not tested · Real e2e none yet
- State: DEFINED — Not complete: No Copy link action exists in the message menu and the client builds no ?m= link; opening a message by URL is covered by UC-CHT-B03 only.

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
- Evidence: BE WS /api/v1/realtime subscribe with scope channel (AuthorizeChannelRealtimeSubscription) · FE chat-realtime-adapter.ts, the connection indicator (chat-status-banner.tsx) · Mock packages/dev/mock-backend/src/realtime and frontend/packages/dev/mock-backend/src/contexts/chat/chat-realtime.ts · Tests AuthorizeChannelRealtimeSubscriptionTests, ChatRealtimeGatewayTests, ChatRealtimeTransportTests, chat-scope-subscribe.unit.test.ts, chat-realtime-adapter.unit.test.ts · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE WS event chat.message.created (ChatMessageEventMapper, ChatRealtime message frame) · FE chat-realtime-adapter.ts and the live insert of the message cache · Mock frontend/packages/dev/mock-backend/src/contexts/chat/chat-realtime.ts · Tests ChatMessageEventMapperTests, ChatRealtimeFactsTests, ChatRealtimeIntegrationTests, chat-realtime.unit.test.ts, chat-realtime-adapter.unit.test.ts, chat-realtime-reducers.unit.test.ts · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE WS events chat.message.updated and chat.message.deleted · FE chat-realtime-adapter.ts and chat-realtime-reducers.ts (in-place update) · Mock frontend/packages/dev/mock-backend/src/contexts/chat/chat-realtime.ts · Tests ChatMessageEventMapperTests, ChatRealtimeFactsTests, chat-realtime.unit.test.ts, chat-realtime-adapter.unit.test.ts · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE GET .../messages?after= (Chat.Messages.List) for the backfill · FE gap, duplicate and out-of-order handling in chat-realtime-adapter.ts and message-cache.ts · Mock frontend/packages/dev/mock-backend/src/contexts/chat/messages.handlers.ts (after) · Tests MessageHandlerTests, chat-realtime-adapter.unit.test.ts, message-cache.unit.test.ts · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE WS /api/v1/realtime (resubscribe and recovery) · FE the offline banner (chat-status-banner.tsx), chat-realtime-adapter.ts recover · Mock packages/dev/mock-backend/src/realtime · Tests ChatRealtimeGatewayTests, chat-realtime-adapter.unit.test.ts, realtime-chat-lifecycle.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE WS access-changed (RemoveChannelMember and SuspendMember signal the person) · FE the lost-access state (chat-workspace.tsx) and the recover reasons of chat-realtime-adapter.ts · Mock frontend/packages/dev/mock-backend/src/contexts/chat/chat-realtime.ts · Tests ChatWorkspaceExitTests, GuestAccessHandlerTests, chat-realtime-adapter.unit.test.ts · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE WS typing frame (ChatRealtime) · FE chat-typing-indicator.tsx, use-ephemeral.ts · Mock packages/dev/mock-backend/src/realtime (typing) · Tests ChatRealtimeTransportTests, chat-typing-presence.unit.test.ts, chat-ephemeral.component.test.tsx, chat-ephemeral.unit.test.ts · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record. No load test with typing and presence.

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
- Evidence: BE WS presence frame (ChatRealtime) · FE chat-presence.tsx, use-ephemeral.ts · Mock packages/dev/mock-backend/src/realtime (presence) · Tests ChatRealtimeTransportTests, chat-typing-presence.unit.test.ts, chat-ephemeral.component.test.tsx, chat-ephemeral.unit.test.ts · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record. No load test with typing and presence.

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
- Evidence: BE WS user stream (chat.unread.changed, ReadStateRealtime) · FE the badges of chat-sidebar.tsx and the unread projection of chat-realtime-adapter.ts · Mock frontend/packages/dev/mock-backend/src/contexts/chat/chat-realtime.ts · Tests ChatRealtimeFactsTests, ReadStateHandlerTests, chat-realtime.unit.test.ts, chat-realtime-adapter.unit.test.ts, chat-sidebar.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record. The latency budget is not measured.

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
- Evidence: BE GET /api/v1/workspaces/{workspaceId}/chat/me/unread (Chat.ReadState.Unread, GetUnreadSummary) · FE the badges of chat-sidebar.tsx · Mock frontend/packages/dev/mock-backend/src/contexts/chat/read-state.handlers.ts (chat.readState.unread) · Tests ReadStateHandlerTests, ChatReadStateIntegrationTests, ChatReadStateRlsTests, chat-sidebar.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE PUT /api/v1/workspaces/{workspaceId}/chat/spaces/{spaceId}/read (Chat.ReadState.MarkRead) · FE useMarkRead in chat-workspace.tsx · Mock frontend/packages/dev/mock-backend/src/contexts/chat/read-state.handlers.ts (chat.readState.markRead) · Tests ReadStateHandlerTests, ChatReadStateIntegrationTests, chat-handlers.unit.test.ts, create-chat-hooks.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE GET .../messages?around= with the read boundary (Chat.ReadState.Unread, Chat.Messages.List) · FE the New messages divider (chat-message-list.tsx, buildChatRows firstUnread) · Mock frontend/packages/dev/mock-backend/src/contexts/chat/read-state.handlers.ts, messages.handlers.ts · Tests ReadStateHandlerTests, chat-model.unit.test.ts, chat-message-list.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE GET .../chat/me/unread (mention counts) · FE the mention badge of chat-sidebar.tsx · Mock frontend/packages/dev/mock-backend/src/contexts/chat/read-state.handlers.ts · Tests ReadStateHandlerTests, MentionHandlerTests, chat-sidebar.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE PUT .../spaces/{spaceId}/notification-level (Chat.ReadState.SetNotificationLevel, level muted) · FE the mute menu (chat-header.tsx, useSetNotificationLevel) · Mock frontend/packages/dev/mock-backend/src/contexts/chat/read-state.handlers.ts (chat.readState.setNotificationLevel) · Tests ReadStateHandlerTests, chat-handlers.unit.test.ts, chat-sidebar.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE PUT .../spaces/{spaceId}/notification-level (Chat.ReadState.SetNotificationLevel) · FE the level selection in chat-header.tsx · Mock frontend/packages/dev/mock-backend/src/contexts/chat/read-state.handlers.ts · Tests ReadStateHandlerTests, chat-handlers.unit.test.ts · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE planned POST /api/v1/workspaces/{workspaceId}/chat/me/read-all (Chat.ReadState.MarkAll) · FE planned mark all read · Design none yet · Tests exist for the built part (see the note); the use case as a whole is not tested · Real e2e none yet
- State: DEFINED — Not complete: BE and mock exist (Chat.ReadState.MarkAllRead); the web service and the useMarkAllRead hook exist but no component calls it: there is no Mark all as read entry.

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
- Evidence: BE POST /api/v1/workspaces/{workspaceId}/chat/conversations (Chat.Conversations.Start, direct) · FE chat-new-message-dialog.tsx, chat-conversation-list.tsx · Mock frontend/packages/dev/mock-backend/src/contexts/chat/conversations.handlers.ts (chat.conversations.start) · Tests ConversationHandlerTests, ChatConversationIntegrationTests, ChatConversationRlsTests, chat-conversations.unit.test.ts, chat-conversations.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE POST .../conversations (Chat.Conversations.Start, group) · FE chat-new-message-dialog.tsx (group name and people) · Mock frontend/packages/dev/mock-backend/src/contexts/chat/conversations.handlers.ts · Tests ConversationHandlerTests, chat-conversations.unit.test.ts, chat-conversations.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE POST .../conversations/{id}/participants (Chat.Conversations.AddParticipants) · FE the participants panel (chat-conversation-view.tsx) · Mock frontend/packages/dev/mock-backend/src/contexts/chat/conversations.handlers.ts · Tests ConversationHandlerTests, chat-conversations.unit.test.ts, create-conversation-hooks.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE DELETE .../conversations/{id}/participants/me (Chat.Conversations.Leave) · FE the leave confirm (chat-workspace.tsx, chat-conversation-view.tsx) · Mock frontend/packages/dev/mock-backend/src/contexts/chat/conversations.handlers.ts · Tests ConversationHandlerTests, chat-conversations.unit.test.ts, create-conversation-hooks.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE GET .../conversations (Chat.Conversations.List) · FE chat-conversation-list.tsx · Mock frontend/packages/dev/mock-backend/src/contexts/chat/conversations.handlers.ts · Tests ConversationHandlerTests, chat-conversations.unit.test.ts, chat-conversations.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE PUT .../chat/me/blocks (Chat.Blocks.Set) and GET .../chat/me/blocks (Chat.Blocks.List) · FE the block and unblock entries (chat-conversation-view.tsx) and the Blocked people dialog (chat-blocked-people.tsx) · Mock frontend/packages/dev/mock-backend/src/contexts/chat/blocks.handlers.ts · Tests ConversationHandlerTests, GuestAccessHandlerTests, PersonBlockListTests, chat-conversations.unit.test.ts, chat-lists.unit.test.ts, chat-blocked-people.component.test.tsx, create-conversation-hooks.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record. Muting a conversation is its notification level (UC-CHT-D05). The block list is read from the server (Chat.Blocks.List); a Guest sees only the blocks of people they share a space with.

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
- Evidence: BE PUT .../conversations/{id}/visibility (Chat.Conversations.SetVisibility) · FE the hide and show entries (chat-conversation-view.tsx) · Mock frontend/packages/dev/mock-backend/src/contexts/chat/conversations.handlers.ts · Tests ConversationHandlerTests, chat-conversations.unit.test.ts, create-conversation-hooks.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE POST /api/v1/workspaces/{workspaceId}/chat/messages/{rootId}/replies (Chat.Threads.Reply) · FE chat-thread-panel.tsx · Mock frontend/packages/dev/mock-backend/src/contexts/chat/threads.handlers.ts (chat.threads.reply) · Tests ThreadHandlerTests, ChatThreadIntegrationTests, ChatThreadRlsTests, chat-threads.unit.test.ts, chat-thread-panel.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE POST .../spaces/{id}/messages with replyToId (ReplyPreviews) · FE chat-quote.tsx and the quote preview of chat-message-row.tsx · Mock frontend/packages/dev/mock-backend/src/contexts/chat/messages.handlers.ts · Tests MessageHandlerTests, PinHandlerTests (quote of a deleted message), chat-handlers.unit.test.ts, chat-interactions.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE GET .../messages/{rootId}/replies (Chat.Threads.ListReplies) with threadReplyCount and threadLastReplyAt on the root · FE chat-thread-panel.tsx and the N replies chip (chat-message-row.tsx) · Mock frontend/packages/dev/mock-backend/src/contexts/chat/threads.handlers.ts (chat.threads.listReplies) · Tests ThreadHandlerTests, ChatThreadIntegrationTests, chat-threads.unit.test.ts, chat-thread-panel.component.test.tsx, chat-message-list.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record. The root carries threadReplyCount and threadLastReplyAt, read in one batched query per page, so the chip shows after a reload; a deleted reply stays counted (the count is the thread sequence).

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
- Evidence: BE PUT .../threads/{id}/follow (Chat.Threads.Follow) and PUT .../threads/{id}/read (Chat.Threads.MarkRead) · FE the follow toggle of chat-thread-panel.tsx · Mock frontend/packages/dev/mock-backend/src/contexts/chat/threads.handlers.ts · Tests ThreadHandlerTests, chat-threads.unit.test.ts, chat-thread-panel.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE planned (background sweep, Chat.Threads.Archive) · FE planned archived thread chip · Design none yet · Tests exist for the built part (see the note); the use case as a whole is not tested · Real e2e none yet
- State: DEFINED — Not complete: No sweep stores the archive: a thread reads as archived by the derived rule (inactive for the configured days) and a reply reopens it (MessageThread.RecordReply, ThreadHandlerTests); the summary carries archived but the UI shows no archived chip.

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
- Evidence: BE PUT .../messages/{id}/pin (Chat.Pins.Pin) · FE the pin action (chat-message-row.tsx, chat-workspace.tsx) and chat-pins.tsx · Mock frontend/packages/dev/mock-backend/src/contexts/chat/pins.handlers.ts (chat.pins.pin) · Tests PinHandlerTests, ChatPinIntegrationTests, ChatPinRlsTests, chat-pins.unit.test.ts, chat-interactions.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE GET .../spaces/{spaceId}/pins (Chat.Pins.List) and DELETE .../messages/{id}/pin (Chat.Pins.Unpin) · FE chat-pins.tsx (pins panel) · Mock frontend/packages/dev/mock-backend/src/contexts/chat/pins.handlers.ts · Tests PinHandlerTests, ChatPinIntegrationTests, chat-pins.unit.test.ts, chat-interactions.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE GET .../chat/me/threads (Chat.Threads.Mine, ListFollowedThreads) · FE chat-followed-threads.tsx · Mock frontend/packages/dev/mock-backend/src/contexts/chat/threads.handlers.ts (chat.threads.mine) · Tests ThreadHandlerTests, ChatThreadIntegrationTests, chat-threads.unit.test.ts, chat-thread-panel.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE GET .../spaces/{id}/mentionables (Chat.Mentions.ListMentionables) and mentions on send · FE the mention picker of chat-composer.tsx, chat-mention-text.tsx · Mock frontend/packages/dev/mock-backend/src/contexts/chat/mentions.handlers.ts, chat-mentions.ts · Tests MentionHandlerTests, ChatMentionIntegrationTests, ChatMentionRlsTests, chat-mentions.unit.test.ts, chat-mentions.component.test.tsx, mention-model.unit.test.ts · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE mention notices on the send and edit answers (MentionResolver) · FE the sender notices of chat-message-row.tsx and chat-mentions-panel.tsx · Mock frontend/packages/dev/mock-backend/src/contexts/chat/chat-mentions.ts · Tests MentionHandlerTests, chat-mentions.unit.test.ts, chat-mentions.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE broadcast through IBroadcastAuthorizer on send (@channel, @here) · FE the broadcast entries of the picker (chat-composer.tsx) · Mock frontend/packages/dev/mock-backend/src/contexts/chat/chat-mentions.ts · Tests MentionHandlerTests, chat-mentions.unit.test.ts, chat-mentions.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record. An @here gives no Inbox notification (no presence data).

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
- Evidence: BE channel links (#) stored as mentions and filtered by the viewer's access (MessageMentions) · FE the # picker and the chip (chat-composer.tsx, chat-mention-text.tsx) · Mock frontend/packages/dev/mock-backend/src/contexts/chat/chat-mention-views.ts · Tests MentionHandlerTests, GuestAccessHandlerTests, chat-mentions.unit.test.ts, mention-model.unit.test.ts, channel-link-model.unit.test.ts · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE GET /api/v1/workspaces/{workspaceId}/chat/links/resolve (Chat.Links.Resolve; Board items and Pages through cross-context public contracts, a title only when the caller can view the resource) · FE link preview chip in messages (loading, no access, resolved) · Design none yet · Tests unit (Application, API contract, mock, features-chat) · Real e2e none yet
- State: HARDENING — built; the owner tests and sets DONE.

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
- Evidence: BE PUT .../messages/{id}/reactions/{emoji} (Chat.Reactions.Add) · FE chat-reactions.tsx, chat-emoji-picker.tsx · Mock frontend/packages/dev/mock-backend/src/contexts/chat/reactions.handlers.ts · Tests ReactionHandlerTests, ChatReactionIntegrationTests, ChatReactionRlsTests, chat-reactions.unit.test.ts, create-chat-hooks.reactions.component.test.tsx, chat-emoji-picker.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE DELETE .../reactions/{emoji} (Chat.Reactions.Remove) and GET .../reactions/{emoji} (Chat.Reactions.ListReactors) · FE chat-reactions.tsx (who reacted) · Mock frontend/packages/dev/mock-backend/src/contexts/chat/reactions.handlers.ts · Tests ReactionHandlerTests, ChatReactionIntegrationTests, chat-reactions.unit.test.ts, create-chat-hooks.reactions.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE POST .../messages/{id}/attachments (Chat.Attachments.Upload, List, Delete, GetContent) · FE chat-attachments.tsx and the composer file picker · Mock frontend/packages/dev/mock-backend/src/contexts/chat/attachments.handlers.ts · Tests AttachmentHandlerTests, ChatAttachmentRlsTests, ChatAttachmentContractOpenApiTests, chat-attachments.unit.test.ts, chat-attachments.component.test.tsx, chat-composer.attachments.component.test.tsx · Design none yet · Real e2e none yet
- State: HARDENING — Built to the use case: backend, mock, UI and tests exist (checked by operation, handler, component and test file; the owner tests and sets DONE). Not done: real e2e, design record, owner check, axe record.

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
- Evidence: BE consumer ChatNotificationIntentConsumer (ChatMessageCreated) maps mention, direct or group message, followed thread reply and @channel to intents; Collaboration writes the Inbox notification through IChatNotificationIntents on the existing notification path, one per message, recipient and reason · FE Inbox rows for chat notifications · Design none yet · Tests unit, consumer, integration against Postgres · Real e2e none yet
- State: HARDENING — built; @here gives no Inbox notification (presence is not in this base); the owner tests and sets DONE.

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
- Evidence: BE GET, PUT and DELETE /api/v1/workspaces/{workspaceId}/chat/channels/{channelId}/permissions (Chat.Permissions.Get, Chat.Permissions.Set, Chat.Permissions.Delete), SetChannelPermissionsCommandHandler, ClearChannelPermissionsCommandHandler, GetChannelPermissionsQueryHandler (backend/src/Notrelix.Application/Features/Chat/Permissions/), ChannelPermissionChecks (every Chat handler asks it), Space.SetPermissionOverrides and ChannelPermissionResolver (backend/src/Notrelix.Domain/Chat/Permissions/), table chat.permission_overrides with RLS (migration 20261112054319_AddChatPermissionOverrides) · FE frontend/packages/features/chat/src/web/components/chat-permissions-dialog.tsx (role matrix, opened from Channel settings), chat-workspace.tsx (read-only banner, disabled composer), chat-header.tsx, core/model/permission-model.ts, web/query/create-chat-hooks.ts (useChannelPermissions, useSetChannelPermissions); mock frontend/packages/dev/mock-backend/src/contexts/chat/permissions.handlers.ts, chat-permissions.ts · Design none yet · Tests backend/tests/Notrelix.Domain.Tests/Chat/Permissions/ChannelPermissionTests.cs; backend/tests/Notrelix.Application.Tests/Features/Chat/Permissions/ChannelPermissionAdminHandlerTests.cs and the honour-the-override cases in MessageHandlerTests, ThreadHandlerTests, ReactionHandlerTests, PinHandlerTests, AttachmentHandlerTests, MentionHandlerTests, ChannelHandlerTests; backend/tests/Notrelix.API.Tests/Contracts/ChatPermissionContractOpenApiTests.cs; backend/tests/Notrelix.Integration.Tests/Chat/ChatPermissionIntegrationTests.cs; backend/tests/Notrelix.Integration.Tests/Rls/ChatPermissionRlsTests.cs; frontend/packages/features/chat/src/__tests__/chat-permissions.component.test.tsx, permission-model.unit.test.ts, create-chat-hooks.permissions.component.test.tsx, chat-realtime-adapter.unit.test.ts; frontend/packages/dev/mock-backend/src/__tests__/chat-permissions.unit.test.ts · Real e2e none yet
- Readings: (1) The roles an override addresses are channel manager, Member and Guest; a Workspace Owner or Admin keeps every action and is never a subject. (2) The actions are post, react, attach, pin, broadcast and invite. Pin is added to the list the use case names, because the Workspace baseline already treats it as its own action. (3) A deny of the person's subject turns the action off. An allow is accepted and validated (it may not exceed the default of the role it names, nor what the caller holds: 422 permission-exceeds-role) but changes nothing, because an override never exceeds the Workspace role and a deny beats an allow of the same pair. (4) "Cannot remove the last manager" is read as "a manager cannot be denied inviting people" (422 permission-lockout), since an override cannot remove a manager. (5) Editing or deleting your own message ignores the post override (it stays yours). (6) Opening the channel permissions is the channel manager, an Owner or an Admin; an archived channel is 409. (7) The capability flags on the channel view (canPost, canReact, canAttach, canPin, canBroadcast, canInvite, canModerate, canManagePermissions) come from the Workspace role default and the overrides; a custom-role grant is not reflected in them. (8) The use case says "channel permissions panel" and the UI has no settings dialog, so the Permissions entry is a menu item of Channel settings that opens a dialog with the matrix.
- State: HARDENING — Built to the use case with the readings above. Not done: real e2e, design record, owner check, axe record; the owner decisions on the readings are open.

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
- Evidence: BE GET /api/v1/workspaces/{workspaceId}/chat/channels (a Guest browses nothing), ChannelAccess (RequireVisible, IsGuest, CallerAsync), ConversationAccess (MayReach, SharesSpaceAsync), MessageMentions.LoadAsync (viewerIsGuest), SetPersonBlockCommandHandler, the Governance WorkspaceRoleBaseline (a Guest is denied channel create and manage, group manage, invite, moderate, broadcast and pin) · FE frontend/packages/features/chat/src/web/components/chat-sidebar.tsx (guest empty state, no create control), chat-workspace.tsx (no browse, no new-message entry for a Guest), chat-conversation-list.tsx · Design none yet · Tests backend/tests/Notrelix.Application.Tests/Features/Chat/Guests/GuestAccessHandlerTests.cs (25 cases: channels, members, mentionables, conversations, threads, links, blocks, pins, reactions, attachments, broadcast, create, moderate, removal and deactivation); frontend/packages/features/chat/src/__tests__/chat-rate-limit.component.test.tsx (guest sidebar); frontend/packages/dev/mock-backend/src/__tests__/chat-limits.unit.test.ts (guest cases) · Real e2e none yet
- Readings: (1) A Guest sees a space only when they are an active member of it; a public channel they were not added to is 404 (NotFound), the same as a channel that does not exist. (2) JoinChannel by a Guest stays 403 with a constant answer that does not say whether the channel exists. (3) A Guest's mentionables list only the people of the space; there is no Workspace directory. A Guest may start a direct or group conversation only with a person they share a space with (the existing MayReach rule); the sidebar does not offer the entry point to a Guest. (4) Removal from a space and deactivation take effect at the next call, because every handler reads the live membership; RemoveChannelMember signals the channel and SuspendMember signals the person, so `access-changed` is sent and the realtime connection authorizes its subscriptions again. A suspended Guest is refused by the pipeline (the Workspace member must be Active); ChannelAccess.CallerAsync also treats a non-active member as NotFound. (5) Leaks found and fixed: (a) the channel links of a message were checked with isGuest false, so a Guest saw the id of a public channel they were not added to; MessageMentions.LoadAsync and ReplyPreviews now take the viewer's guest status. (b) SetPersonBlock answered success or NotFound depending on whether the target was in the Workspace, a membership probe for a Guest; a Guest now gets NotFound for a person they share no space with. (c) CallerAsync accepted the membership fact of a suspended member. (6) Chat search (UC-CHT-H07) is built and filters by live membership, so a Guest finds only the spaces they were added to. (7) Realtime subscription, attachments, reactions and pins were audited and already ask the space membership first; the tests pin that.
- State: HARDENING — Built to the use case with the readings above. Not done: real e2e, design record, owner check, axe record; search-by-name is testable now that UC-CHT-H07 is built.

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
- Evidence: BE POST /api/v1/workspaces/{workspaceId}/chat/messages/{messageId}/moderation (Chat.Messages.Moderate), ModerateDeleteMessageCommandHandler (backend/src/Notrelix.Application/Features/Chat/Messages/Commands/ModerateDeleteMessage/), Message.DeleteAsModerator and IsRemovedByModerator (backend/src/Notrelix.Domain/Chat/Messages/Message.cs), MessageTombstones (shared with the author's delete) · FE frontend/packages/features/chat/src/web/components/chat-dialogs.tsx (ModerateMessageDialog), chat-message-row.tsx (Delete as moderator toolbar button, "removed by a moderator" tombstone), chat-workspace.tsx, web/query/create-chat-hooks.ts (useModerateMessage); mock frontend/packages/dev/mock-backend/src/contexts/chat/permissions.handlers.ts · Design none yet · Tests backend/tests/Notrelix.Domain.Tests/Chat/Permissions/ChannelPermissionTests.cs (moderator tombstone); backend/tests/Notrelix.Application.Tests/Features/Chat/Permissions/ChannelPermissionAdminHandlerTests.cs; backend/tests/Notrelix.API.Tests/Contracts/ChatPermissionContractOpenApiTests.cs; frontend/packages/features/chat/src/__tests__/chat-permissions.component.test.tsx, create-chat-hooks.permissions.component.test.tsx; frontend/packages/dev/mock-backend/src/__tests__/chat-permissions.unit.test.ts · Real e2e none yet
- Readings: (1) The moderate action is held by a Workspace Owner and Admin by the Governance baseline (and by a custom role that grants it); a channel manager is not a moderator. This is the open owner decision "roles per owner decision". (2) The use case names `DELETE ...?moderation=true`; the built operation is `POST .../messages/{id}/moderation` with the reason and expectedVersion in the body, because a DELETE with a required body is not portable and the author's DELETE already means "my own message". (3) The reason (1 to 500 characters) is stored in the Audit fact only, never on the message; the message becomes the same tombstone as an author's delete, with the moderator as the one who removed it (removedByModerator). (4) The author is not notified: that handoff is UC-CHT-G09, which is not built. (5) A moderator does not read a private channel by role (CHT-004): 404. Your own message is 400 self-target (delete it as its author); a system message is 409; a message that is already a tombstone is 204 with no second Audit fact; an archived channel is 409. (6) A moderated message loses its pin, reactions, mentions and files, like an author's delete.
- State: HARDENING — Built to the use case with the readings above. Not done: real e2e, design record, owner check, axe record; the owner decision on who moderates is open.

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
- Evidence: BE ChatAuditFacts (backend/src/Notrelix.Application/Features/Chat/Audit/Services/ChatAuditFacts.cs) over the existing IAuditService, called from CreateChannel, ArchiveChannel, DeleteChannel, AddChannelMembers, RemoveChannelMember, SetChannelPermissions, ClearChannelPermissions and ModerateDeleteMessage · FE none (the Governance Audit screen lists the facts; no Chat-specific screen) · Design none yet · Tests backend/tests/Notrelix.Application.Tests/Features/Chat/Permissions/ChannelPermissionAdminHandlerTests.cs (audit written once, not on a no-op); backend/tests/Notrelix.Application.Tests/Features/Chat/ChannelHandlerTests.cs (create and remove) · Real e2e none yet
- Readings: (1) "Outbox, Governance consumes it" is read as the existing audit contract: IAuditService writes the AuditLog row in the same transaction as the change (the Chat context never touches the Governance DbContext, and there is no second audit path). A failed fact therefore rolls the action back instead of being retried; the fact and the action cannot diverge. (2) The facts are ChatChannelCreated, ChatChannelArchived, ChatChannelUnarchived, ChatChannelDeleted, ChatMemberAdded, ChatMemberRemoved, ChatPermissionsSet, ChatPermissionsCleared and ChatMessageModerated. (3) The use case lists "privacy changed" and "retention changes": no Chat command changes a channel's privacy or a member's role yet, and the retention facts (ChatRetentionChanged, ChatRetentionApplied) and the export facts (ChatExportRequested, ChatExportCompleted) arrived with UC-CHT-H05 and H06; the privacy and role facts are still not emitted. (4) Joining and leaving by yourself are not administration and are not audited; removing someone else is. (5) A command that changes nothing (a repeated archive, a set equal to the stored one) writes no fact. (6) The moderation fact carries the reason and the author as the change subject; the message text is never copied.
- State: HARDENING — Built to the use case with the readings above. Not done: real e2e, owner check; the facts for privacy and role change wait for the commands that make them.

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
- Evidence: BE Chat.Retention.Get and Chat.Retention.Set (backend/src/Notrelix.API/Endpoints/Chat/Retention/MapChatRetentionEndpoints.cs), SetRetentionPolicy and GetRetentionPolicy (backend/src/Notrelix.Application/Features/Chat/Retention), ChatRetentionApplier (backend/src/Notrelix.Application/Features/Chat/Retention/Services/ChatRetentionApplier.cs), the hosted job ChatRetentionService (backend/src/Notrelix.Infrastructure/BackgroundJobs/ChatRetentionJob.cs), ChatSystemAudit, RetentionPolicy (backend/src/Notrelix.Domain/Chat/Retention), migration AddChatRetentionAndExports · FE Chat settings dialog (frontend/packages/features/chat/src/web/components/chat-admin-settings.tsx and chat-admin-settings-host.tsx), retention-model.ts, the `retention` space frame in chat-realtime-adapter.ts · Design none yet · Tests backend/tests/Notrelix.Domain.Tests/Chat/Retention/RetentionPolicyTests.cs; backend/tests/Notrelix.Application.Tests/Features/Chat/Retention/ChatRetentionApplierTests.cs and RetentionPolicyHandlerTests.cs and ChatRetentionEventMapperTests.cs; backend/tests/Notrelix.Infrastructure.Tests/ChatRetentionOptionsTests.cs; backend/tests/Notrelix.Integration.Tests/Chat/ChatRetentionIntegrationTests.cs and Rls/ChatRetentionRlsTests.cs; backend/tests/Notrelix.API.Tests/Contracts/ChatRetentionExportContractOpenApiTests.cs; frontend/packages/dev/mock-backend/src/__tests__/chat-retention.unit.test.ts; frontend/packages/features/chat/src/__tests__/chat-admin-settings.component.test.tsx, create-chat-hooks.retention.component.test.tsx, retention-model.unit.test.ts, chat-realtime-adapter.unit.test.ts · Real e2e none yet
- Readings: (1) The rule is one row per Workspace (the id is the Workspace id) with a window of 7 to 3650 days, or none, and a mode, tombstone or purge. A Workspace with no row reads as no retention at version 1; the first rule must be sent with expectedVersion 1 (else 412). Turning retention off keeps the mode. (2) Permissions reuse existing actions, so the catalog does not change: Set is ManageWorkspace (the Workspace Owner only; an Admin reads), Get is ViewWorkspace (every member; the DTO carries canChange). (3) Retention only touches user messages; system messages stay. (4) Tombstone mode turns a message past the window into the same tombstone a delete makes, with no deleter (DeletedBy null); its pin, reactions, mentions and files are removed (files are soft-deleted with the policy owner as deleter). It raises no per-message event. (5) Purge mode hard-deletes main-flow messages past the window, old tombstones included. A thread goes as a whole (root, replies, follower rows, pins, reactions), and only when its last reply is past the window. (6) Sequence gaps are accepted (the spec says "Purged (retention only)"); Space.LastSequence is untouched and unread counts are computed from the remaining rows, so they stay right; thread reply counts go with the thread. (7) The job Chat.Retention.Apply runs every hour by default in batches of 200 messages per space, at most 50 batches per space per run (Chat:Retention: Enabled, Interval, MaxBatchesPerSpace). Each batch takes a transaction advisory lock on the space, so two instances never work on one space together; a failed batch rolls back and the next run selects the same rows again, so the work resumes and is idempotent. (8) One Audit fact per batch with counts only (ChatRetentionApplied, actor type System, no actor, no text); a rule change writes ChatRetentionChanged with the old and new window and mode. The fact is written directly as an AuditLog row of the System actor (as DeletedWorkspacePurge does) through IChatSystemAudit, because IAuditService needs a user actor. (9) Realtime: one chat.space.changed frame with the change `retention` per batch per space, not one delete frame per message; readers refetch the messages, pins and channel views of the space. (10) Holds: a space deleted and still inside its recovery window is skipped. A legal hold is not a platform concept, so it is not implemented. (11) Search hook, satisfied by UC-CHT-H07: the search has no index and no copy of a message body; it reads the live message table in the same statement as the permission filter, so a tombstone or purge takes effect at the next search. Between the window and the hourly job, the search itself hides a message older than the policy window (CreatedAt below now minus the window days).
- State: HARDENING — Built to the use case with the readings above. Not done: real e2e, design record, owner check, axe record; legal hold (no such concept); the purge of deleted spaces after their recovery window; search needs no removal step (UC-CHT-H07 reads the live table).

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
- Evidence: BE Chat.Exports.Create, Chat.Exports.Get and Chat.Exports.GetContent (backend/src/Notrelix.API/Endpoints/Chat/Exports/MapChatExportEndpoints.cs), CreateChatExport, GetChatExport and GetChatExportContent (backend/src/Notrelix.Application/Features/Chat/Exports), ChatExportBuilder, the worker ChatExportService (backend/src/Notrelix.Infrastructure/BackgroundJobs/ChatExportWorker.cs), ChatExportJob (backend/src/Notrelix.Domain/Chat/Exports), the Export rate limit policy (ChatRateLimits) · FE Chat settings dialog export section (frontend/packages/features/chat/src/web/components/chat-admin-settings.tsx) · Design none yet · Tests backend/tests/Notrelix.Domain.Tests/Chat/Exports/ChatExportJobTests.cs; backend/tests/Notrelix.Application.Tests/Features/Chat/Exports/ChatExportTests.cs and RateLimits/ChatRateLimiterTests.cs; backend/tests/Notrelix.Integration.Tests/Rls/ChatRetentionRlsTests.cs; backend/tests/Notrelix.API.Tests/Contracts/ChatRetentionExportContractOpenApiTests.cs; frontend/packages/dev/mock-backend/src/__tests__/chat-retention.unit.test.ts; frontend/packages/features/chat/src/__tests__/chat-admin-settings.component.test.tsx and create-chat-hooks.retention.component.test.tsx · Real e2e none yet
- Readings: (1) Create, status and download need ManageWorkspaceSettings (Owner and Admin). The subject must be a member of the Workspace (404 otherwise). Only the requester reads the status or downloads the file; another Admin gets 404. (2) The file is JSON `notrelix.chat-export.v1` with the subject's memberships, their messages (id, space, time, content) and a summary. Scope is what the requester can see: public channels, and private channels or conversations where the requester is an active member. Spaces the requester cannot see are left out and only counted (the count is in the file summary and the job; no name, no content). Deleted spaces are out of scope. (3) Tombstones and system messages are not listed; tombstones are counted. Other people's content is never in the file. (4) Bounds: 50,000 messages or 20 MiB, else the job fails with the code export-too-large. (5) The file lives in platform object storage for 24 hours; the download is the authorized route of the API (permission and expiry checked on every request), never a public URL. A sweep expires the file and requeues a job stuck Running for more than 15 minutes. (6) The worker checks again that the requester is still an active Owner or Admin (failure code no-access). (7) Rate limit policy Export through the S3.2 limiter: 5 per person and 20 per Workspace per hour. (8) Audited: ChatExportRequested (requester, subject, range) on create and ChatExportCompleted (counts only) by the worker. (9) There is no list of past exports.
- State: HARDENING — Built to the use case with the readings above. Not done: real e2e, design record, owner check, axe record; a list of past exports; the bounds are not tuned against real data.

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
- Evidence: BE GET /api/v1/workspaces/{workspaceId}/chat/search (Chat.Search.Query, policy chat.search) · FE search panel opened from the chat header, the conversation header and the channel list · Design none yet · Tests: Application ChatSearchTests (permission matrix, parity with RequireVisible, filters, cursor, snippet), ChatRateLimiterTests, API ChatSearchContractOpenApiTests, Integration ChatSearchRlsTests (real PostgreSQL, RLS, Vietnamese with and without marks), Architecture baselines, mock chat-search.unit.test.ts, FE search-model, chat-search-panel and chat-workspace.search tests · Real e2e none
- Readings: (1) The text is a phrase, 2 to 100 characters after white space is collapsed, matched as a substring ignoring case and Vietnamese marks (SearchTextFolding, the same fold as the member search); typed characters are never a pattern. Text stored in decomposed form may not match. (2) There is no index: the match runs live beside the permission filter (ChannelAccess.VisibleSpaces, the SQL twin of RequireVisible, checked by a parity test), so removal, delete, tombstone and retention take effect at the next search. Large Workspaces scan; a trigram index is a known gap. (3) Visible now means a live space that is public (not for a Guest) or where the caller is an active member. A Workspace Admin who is not in a private channel finds nothing there; archived channels are searchable; deleted spaces, tombstones and system messages are not. (4) Order is newest first (createdAt, id), not by rank; the cursor is opaque; no total is returned so hidden spaces are never counted; limit 1 to 50, default 20. (5) Blocked authors are still found, because history does not hide them. (6) mentionsMe means a direct person mention only. (7) hasAttachment scans up to 10 batches of 200 rows through the attachments port (no cross-context query); a scan that hits the cap returns a cursor to continue. (8) A space filter the caller cannot see answers an empty page, the same as a space that does not exist. (9) The retention cutoff is per message, not per thread. (10) Snippets are plain text with match ranges, 40 characters before and 100 after, never HTML; the panel draws them with mark elements. (11) Rate limit chat.search: 30 per minute per person and 300 per Workspace. (12) No content in logs or audit; recents live in memory only. (13) No header search button existed, so one was added to the chat header, the conversation header and the channel list.
- State: HARDENING — Built to the use case with the readings above. Not done: real e2e, design record, owner check, axe record; trigram index for very large Workspaces.

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
- Evidence: BE POST .../chat/spaces/{spaceId}/messages (Chat.Messages.Send), POST .../threads/.../replies (Chat.Threads.Reply), PUT .../reactions (Chat.Reactions.Add), POST .../attachments (Chat.Attachments.Upload) answer 429 with Retry-After, errorCode rate_limit.exceeded and ruleCode rate-limited or slow-mode; PATCH .../chat/channels/{channelId} (Chat.Channels.Update) sets slowModeSeconds; IChatRateLimiter and ChatRateLimitOptions (RateLimiting:Chat), SlowMode (backend/src/Notrelix.Application/Features/Chat/RateLimits/), TooManyRequestsException, Space.SetSlowMode and the column chat.spaces.slow_mode_seconds (migration 20261112071824_AddChatSlowMode, expand-only), ChatSlowModeEventMapper (chat.space.changed with the change slow-mode) · FE frontend/packages/features/chat/src/web/components/chat-rate-limit.tsx (countdown notice), chat-composer.tsx (slow-mode hint), chat-dialogs.tsx (slow mode choice in Channel settings), core/api/chat-failure.ts (retryAfterSeconds, limit), core/model/rate-limit-model.ts, web/realtime/chat-realtime-adapter.ts (slow-mode frame); mock frontend/packages/dev/mock-backend/src/contexts/chat/chat-limits.ts (virtual clock) · Design none yet · Tests backend/tests/Notrelix.Application.Tests/Features/Chat/RateLimits/ChatRateLimiterTests.cs, the limit and slow-mode cases in MessageHandlerTests, ThreadHandlerTests, ReactionHandlerTests, AttachmentHandlerTests, MentionHandlerTests; backend/tests/Notrelix.Domain.Tests/Chat/Spaces/SlowModeTests.cs; backend/tests/Notrelix.API.Tests/Contracts/ChatRateLimitContractOpenApiTests.cs; backend/tests/Notrelix.Integration.Tests/Chat/ChatSlowModeIntegrationTests.cs; frontend/packages/features/chat/src/__tests__/chat-rate-limit.component.test.tsx, rate-limit-model.unit.test.ts, chat-failure.unit.test.ts, chat-realtime-adapter.unit.test.ts; frontend/packages/dev/mock-backend/src/__tests__/chat-limits.unit.test.ts, chat-attachments.unit.test.ts · Real e2e none yet
- Readings: (1) The limits use the shared IRateLimitService (Redis sliding window) with the policy names chat.send, chat.react, chat.thread-reply, chat.upload and chat.broadcast, counted per person (`person:{id}`) and per space (`space:{id}`). Defaults per minute, person and space: send 30 and 300, react 60 and 600, thread reply 30 and 300, upload 10 and 100, broadcast 3 and 10; they are set under RateLimiting:Chat and validated at start. (2) The limits are checked in the handlers (IChatRateLimiter), not in the API middleware, because the middleware partitions by user, IP or API key only and has no space. This is a deviation from "pipeline"; there is no second framework. (3) A count is an attempt: the sliding window also counts the calls it refuses. A retry of a stored send (the same clientMessageId) and a reaction the person already has cost nothing. (4) The store failing opens the limit (the call goes through) and logs a warning. (5) Nothing is exempt: a Workspace Admin is limited on @channel and @here like anyone (the use case), and, read the same way, no role is exempt from slow mode (the use case names no exemption for moderators or managers). (6) Slow mode is a per-channel number of seconds (0 to 3600; 0 is off) between two messages of one person; a send and a thread reply are messages. The wait runs from the person's own last message in the space, so deleting it does not reset it; system messages do not count. It is set through the existing PATCH Chat.Channels.Update with an optional slowModeSeconds (a request that leaves it out keeps it), by a channel manager, an Owner or an Admin, with expectedVersion (412 when stale); a change bumps the version and sends chat.space.changed with the change slow-mode. No Audit fact is written for a slow-mode change. (7) The 429 body carries ruleCode, retryAfter (whole seconds), policy, scope and limit (or slowModeSeconds); the Retry-After header carries the same seconds. (8) The UI deviates from "keeps the draft": the composer empties on send as before, and a 429 leaves the message as a failed pending entry that holds its text and files, with a status notice that counts down and offers Send again. The countdown is announced politely once and again when sending is possible. A thread reply or a reaction refused with 429 shows the generic slow-down notice without a countdown.
- State: HARDENING — Built to the use case with the readings above. Not done: real e2e, design record, owner check, axe record; the limits are not tuned against load, and no browser check was made.

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
- Evidence: BE the hosted job Chat.Notifications.Digest (ChatDigestService, ChatDigestRunner, ChatDigestOptions `Chat:Digest`; backend/src/Notrelix.Infrastructure/BackgroundJobs/ChatDigestJob.cs), ChatDigestComposer (backend/src/Notrelix.Infrastructure/Notifications/ChatDigest), the Chat port IChatDigestFacts and its ChatDigestFacts (backend/src/Notrelix.Application/Features/Chat/Public/Digest and Chat/Notifications/Services), the Collaboration renderer ChatDigestMessage and ChatDigestWindow (backend/src/Notrelix.Application/Features/Collaboration/Notifications/ChatDigest), the Identity port IMemberDeliveryAddresses (Features/Identity/Public/DeliveryAddresses) with MemberDeliveryAddresses, the shared email outbox (EmailOutboxMessage, EmailDispatcher) · FE the existing digest row of the notification preferences (frontend/packages/features/account/src/web/notifications, copy now names chat messages you missed); mock `runDigest` (frontend/packages/dev/mock-backend/src/contexts/chat/chat-digest.ts, virtual clock) and `state.digestOutbox` · Design none yet · Tests backend/tests/Notrelix.Application.Tests/Features/Chat/Notifications/ChatDigestFactsTests.cs and ChatDigestMessageTests.cs; backend/tests/Notrelix.Infrastructure.Tests/Notifications/ChatDigestComposerTests.cs; backend/tests/Notrelix.Integration.Tests/Chat/ChatDigestIntegrationTests.cs (real PostgreSQL, the worker address read); frontend/packages/dev/mock-backend/src/__tests__/chat-digest.unit.test.ts · Real e2e none yet
- Readings: (1) There was no Collaboration email path for notifications (UC-COL-D13 and D14 are DEFINED), so the digest is a new producer on the shared email outbox, the one mail path and dispatcher with retries; there is no second scheduler or sender. Chat supplies grouped facts through IChatDigestFacts; the composer (Infrastructure, on the Collaboration side because the notification tables are there) applies the preference matrix and queues the mail. (2) Opt-in is the `digest` type on the `email` channel (off by default), plus the in-app cell of each item's type (mention for mention, direct message and broadcast; watching for a thread reply). The matrix has no frequency, so the cadence is the window length (24 hours, a UTC day, Chat:Digest:Window). (3) Only the latest completed window is reported: items created in [start, end) that are still unread. They are not repeated in a later window, and windows the job missed are not caught up. (4) One mail per person, Workspace and window: the deduplication key is `chat-digest:{workspace}:{user}:{window}` on the unique outbox key, so a repeated pass adds nothing and a failed send is retried by the dispatcher on the same row. (5) Left out, checked at send: muted channels and level None, a Mentions level for what it does not cover, a space the person left or that is deleted, an item already read (the read boundary and the notification read state), a deleted or system message, and the author. (6) A line is a short plain excerpt (400 characters at most) with the channel name and the reason; the permission is checked again at send through the same membership rules; it is capped at 10 lines with "and N more". (7) The deep link is the channel path only, because no message-level route exists. (8) The address comes from Identity's system-sender fact (a Guest has none, so a Guest gets no digest); the existing disclosure function needs an Owner or Admin reader and a job has none. (9) Locales are English and Vietnamese, by the person's locale. (10) The Inbox row "Daily digest" description was changed: it no longer promises 08:00. (11) Not built: a per-person frequency or send hour, a digest of other notification types, unsubscribe headers.
- State: HARDENING — Built to the use case with the readings above. Not done: real e2e, design record, owner check, axe record, ops runbook; no real mail was sent.

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
- Evidence: BE PUT and DELETE /api/v1/workspaces/{workspaceId}/chat/channels/{channelId}/link (Chat.Channels.Link and Chat.Channels.Unlink), GET .../chat/links/linked-channels?kind&id (Chat.Channels.ListLinked), LinkChannelCommand, UnlinkChannelCommand, ListLinkedChannelsQuery and ChatResourceViews (backend/src/Notrelix.Application/Features/Chat), Space.SetLink and ClearLink and ChannelLinkKinds (backend/src/Notrelix.Domain/Chat/Spaces), the columns chat.spaces.link_kind and link_id (migration 20261112141009_AddChatChannelLink, expand-only), ChatLinkEventMapper (chat.space.changed with the change link), the Audit facts ChatChannelLinkSet and ChatChannelLinkCleared (ChatAuditFacts), IBoardTitleFacts (WorkManagement public port) · FE header chip, the Link resource dialog with preview and the unlink entry (frontend/packages/features/chat/src/web/components/chat-resource-link.tsx, chat-header.tsx), the hooks useLinkChannel, useUnlinkChannel and useLinkedChannels and the exported DiscussButton (chat-discuss-button.tsx), the link realtime frame, BoardDiscuss on the Board page (frontend/apps/web/src/shell/workspace/board-discuss.tsx); mock links.handlers.ts and chat-links.ts · Design none yet · Tests backend/tests/Notrelix.Domain.Tests/Chat/Spaces/ChannelLinkTests.cs; backend/tests/Notrelix.Application.Tests/Features/Chat/Links/ChannelLinkHandlerTests.cs; backend/tests/Notrelix.Integration.Tests/Chat/ChatChannelLinkPipelineTests.cs (real pipeline and PostgreSQL, 403 and the version check) and ChatChannelLinkIntegrationTests.cs; backend/tests/Notrelix.API.Tests/Contracts/ChatChannelLinkContractOpenApiTests.cs; frontend/packages/dev/mock-backend/src/__tests__/chat-channel-link.unit.test.ts; frontend/packages/features/chat/src/__tests__/chat-channel-link.component.test.tsx, chat-workspace.link.component.test.tsx, channel-link-model.unit.test.ts, chat-realtime-adapter.unit.test.ts · Real e2e none yet
- Readings: (1) A channel has one link; a second replaces the first. Boards and Pages are linkable. A Team is in the domain kinds (`governance.team`) but the app cannot set it (400): Governance publishes no Team visibility fact, so the manager's right to view a Team could not be checked. It is a recorded gap; a link of that kind, if stored, shows as a generic chip. (2) The manager must be a channel manager, an Owner or an Admin of a live, unarchived channel, and must be able to view the resource now (Board: through the Board title fact with the viewer's own access; Page: the Documents page view access); otherwise 403, the same answer for a hidden, deleted or missing resource. A Guest cannot link. The resource is never changed, and unlinking needs no access to the resource. (3) The link is stored on the space (kind and id only, no title); RLS is unchanged (the columns are on an RLS-protected table). A stale expectedVersion is 412 (the pipeline decides it, listed in the expected-version inventory). Setting the same link again changes nothing. (4) The channel view carries the link: kind, id, and the title only when the viewer can view the resource now; otherwise accessible is false with no title (a deleted, hidden or missing resource look the same). (5) The reverse lookup returns only the channels the caller can see (a public channel, or any channel the caller is in), and an empty list to a caller who cannot view the resource; archived channels are returned with a flag and the Discuss button hides them. (6) The dialog takes a pasted product address (/workspaces/{id}/boards/{id} or /docs/{id}) or an id with the kind chosen, resolves it with the existing Chat.Links.Resolve (which gained the Board kind by reference; its URL form is unchanged and still needs a Board item), shows the preview and links only after it. (7) Discuss is exported from features-chat (hooks.useLinkedChannels(workspaceId, resource) and DiscussButton) and wired on the Board page through the existing notice slot, a host-only edit. The Page header has no slot for a host action in the docs package, so the wiring is a recorded gap (a slot in DocPageHost is the needed edit). (8) The audit fact for a link or unlink is written through the Chat audit helper (resource, kind and id, no title).
- State: HARDENING — Built to the use case with the readings above. Not done: real e2e, design record, owner check, axe record; Team linking; the Discuss entry on the Page header.

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

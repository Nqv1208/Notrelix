---
document_id: WRK-UC-COLLABORATION
document_type: workstream-use-case-catalog
status: active
owner: documents-collaboration-team
applies_to:
  - collaboration
  - comments
  - mentions
  - reactions
  - attachments
  - activity
  - watchers
  - notifications
  - notification-preferences
  - presence
  - use-case-catalog
  - capability-acceptance
evidence:
  - docs/workstreams/use-cases/README.md
  - docs/product/collaboration.md
  - docs/workstreams/teams/documents-collaboration.md
  - docs/workstreams/executions/documents-collaboration/documents-collaboration.spec.md
  - docs/workstreams/executions/documents-collaboration/documents-collaboration.tests.md
  - docs/workstreams/executions/documents-collaboration/documents-collaboration.certification.md
  - backend/src/Notrelix.API/Endpoints/Collaboration/
  - backend/src/Notrelix.Application/Features/Collaboration/
  - backend/src/Notrelix.Application/Features/Notifications/
  - backend/src/Notrelix.Domain/Collaboration/
  - frontend/packages/features/collaboration/
  - frontend/packages/features/notifications/
review_on:
  - comment-model-change
  - mention-model-change
  - reaction-model-change
  - notification-model-change
  - attachment-change
  - presence-or-read-state-change
  - watcher-change
  - capability-state-change
---

# Use cases: Collaboration

Product authority: [collaboration.md](../../product/collaboration.md). Team spec: [documents-collaboration.md](../teams/documents-collaboration.md). Execution pack: [spec](../executions/documents-collaboration/documents-collaboration.spec.md), [tests](../executions/documents-collaboration/documents-collaboration.tests.md), [certification](../executions/documents-collaboration/documents-collaboration.certification.md). Router and acceptance standard: [README.md](README.md). Target-specific behaviour: [work-management.md](work-management.md) (items) and [documents.md](documents.md) (pages).

Dates: this catalog was written on 2026-10-30. No use case has a recorded owner check, a 1-px design diff or an axe record, so none is DONE. The certification pack of the execution lists every Collaboration milestone as NOT_EVALUATED.

## 1. Assessment

### 1.1 State per capability

| Letter | Capability | Use cases | DEFINED | IMPLEMENTING | INTEGRATING | HARDENING | DONE | BLOCKED |
|---|---|---|---|---|---|---|---|---|
| A | COL-001 Comment lifecycle | 11 | 1 | 0 | 8 | 2 | 0 | 0 |
| B | COL-002 Comment target contract | 18 | 2 | 0 | 10 | 6 | 0 | 0 |
| C | COL-003 Comment authorization | 6 | 0 | 0 | 5 | 0 | 0 | 1 |
| D | COL-004 Comment activity/events | 14 | 5 | 1 | 5 | 3 | 0 | 0 |
| E | COL-005 Collaboration realtime | 5 | 3 | 1 | 0 | 0 | 0 | 1 |
| F | COL-006 Target deletion/retention | 7 | 1 | 2 | 1 | 0 | 0 | 3 |
| G | COL-007 Collaboration query/state | 16 | 2 | 1 | 7 | 6 | 0 | 0 |
| H | COL-008 Collaboration hardening | 8 | 3 | 2 | 1 | 0 | 0 | 2 |
| | Total | 85 | 17 | 8 | 36 | 17 | 0 | 7 |

Collaboration has no dedicated capability for inbox, preferences, attachments, watchers or presence in the team spec. They sit in the nearest capability: attachments and watchers under B (target contract), notifications production under D, presence under E, Inbox, preferences and read state under G.

### 1.2 The ten most important gaps

1. Fixed: edit, delete, resolve and reopen of a comment, and delete of an attachment, were authorized as `UpdateItem` on the kinds `collaboration.comment` and `collaboration.attachment`, which Governance does not resolve, so only the workspace Owner passed. They now pass a membership gate and the handler decides from the target: author for edit and delete, thread starter or editor for resolve and reopen, editor of the target for attachment delete. Still open: expected version, moderator delete (UC-COL-C02), a real e2e.
2. No Collaboration fact is published to realtime: comments, reactions, Inbox counts and presence all need a manual refetch (UC-COL-E01 to E03).
3. Competing writes: comment create has no idempotency key, and edit, delete, resolve have no expected version (UC-COL-H01, H02). Reactions and watchers have no unique index (UC-COL-H03).
4. Pages are second-class: no page reactions, no page followers, no page notifications, no page activity (UC-COL-A11, B17, D09, D12).
5. Target deletion has no Collaboration rule: no purge, no restore link, no test (UC-COL-F01, F02, F05).
6. Comment list is unbounded and has no pagination (UC-COL-G01).
7. Mention privacy is incomplete: page-comment mentions skip the view check, and no mention re-checks access at delivery (UC-COL-B02, D04).
8. Email, digest, quiet hours do not exist; the preference matrix is read by only one producer (UC-COL-D13, D14, G12, G13).
9. Presence, read state and unread markers are Domain-only classes with no use case (UC-COL-E03, G14).
10. Identity anonymization, Workspace deletion and moderator delete wait for owner or sibling-context decisions (UC-COL-F04, F05, C02).

### 1.3 Defects in shipped code

| Defect | Where |
|---|---|
| Authorization of comment edit, delete, resolve, reopen and attachment delete (fixed): decided in the handler from the target, no longer Owner-only. The attachment delete route still ignores `{boardItemId}`. | `backend/src/Notrelix.Application/Features/Collaboration/Comments/Services/`, `.../Attachments/Services/AttachmentAccessRules.cs`, `.../Comments/Commands/*`, `.../Attachments/Commands/DeleteAttachment/DeleteAttachment.cs` |
| No expected version on competing comment writes; comment create has no idempotency key. | `.../Comments/Commands/UpdateComment/UpdateComment.cs`, `backend/src/Notrelix.API/Endpoints/Collaboration/Comments/Commands/CreateCommentEndpoint.cs` |
| Check-then-insert races: no unique index on reactions (target, user, emoji) or watchers (target, user). | `backend/src/Notrelix.Infrastructure/Data/Configurations/Collaboration/ReactionConfiguration.cs`, `ResourceWatcherConfiguration.cs`, `.../Reactions/Commands/AddReaction/AddReaction.cs`, `.../Watchers/Commands/FollowBoardItem/FollowBoardItem.cs` |
| Unbounded read: all comments, reactions and actor names for a target in one call. | `.../Comments/Queries/GetComments/GetComments.cs` |
| Length limit mismatch: validator 50,000 characters, Domain 10,000. | `CreateCommentCommandValidator.cs`, `Comment.cs` |
| Page-comment mentions are stored for anyone without a view check; the notification consumer does not re-check view access at delivery (COL-030). | `CreateComment.cs` (ResolveMentionBoardAsync returns null for pages), `.../Messaging/Consumers/Collaboration/MentionCreatedNotificationConsumer.cs` |
| Edit cannot add mentions: `UpdateCommentRequest` has no mention list. | `backend/src/Notrelix.API/Contracts/Collaboration/Comments/Requests/UpdateCommentRequest.cs` |
| Deleted parent leaves replies listed with an orphan `ParentId`. | `GetComments.cs` |
| Reactions and the reaction palette exist only on board-item comments; the command carries `BoardItemId`. | `.../Reactions/`, `.../Endpoints/Collaboration/Comments/Commands/ReactionEndpoints.cs` |
| Page comments are read under the `ViewBoard` action name, not `ViewPage` (confirm the engine maps it). | `GetComments.cs` |
| Mock-only and dead frontend: `/notifications`, `/notifications/{id}/read`, `/notifications/read-all` and the hooks `createNotificationBell`, `createUseNotifications`, `createUseMarkRead`, `createUseArchiveNotification`, `createUseUnreadCount` call routes the backend does not have; only `InboxBell` and the preferences hook are used by the app. | `frontend/packages/features/notifications/src/web/`, `frontend/packages/foundation/contracts/src/endpoints/endpoints.ts`, `frontend/packages/dev/mock-backend/src/contexts/notifications/notifications.handlers.ts` |
| Dead backend code: `PresenceSession`, `ResourceReadState`, `MentionType.Team/Page/Board`, `Comment.Restore`, `Comment.Delete(reason)`, stored `ArchivedAt` and `DismissedAt` on notification recipients have no use case. | `backend/src/Notrelix.Domain/Collaboration/`, `ICollaborationDbContext.cs` |
| The Inbox list handler sits in the Work Management feature while Collaboration owns the Inbox. | `backend/src/Notrelix.Application/Features/WorkManagement/BoardItems/Queries/GetWorkspaceInbox/GetWorkspaceInbox.cs` |
| No rate limit on any Collaboration route. | `backend/src/Notrelix.API/Endpoints/Collaboration/` |
| Only two of nine fact kinds reach the outbox as integration events (comment-created, mention-created). | `backend/src/Notrelix.Application/EventMappers/Collaboration/CommentEventMapper.cs` |
| Workspace activity row for a comment has a fixed English title with the raw kind. | `.../Messaging/Consumers/Collaboration/ActivityProjectionConsumers.cs` |
| Mock-only frontend paths with no real e2e: comment edit, delete, reactions, resolve, reopen, attachments, preferences, Inbox read-all, activity tab. | `frontend/e2e/mock/comment-replies-reactions.mock.e2e.spec.ts` is the only mock spec; `frontend/e2e/real/` has none for these |

### 1.4 Enterprise baseline comparison

Products compared and what they show:

- Asana Inbox: notifications can be archived one at a time or in a batch, and Inbox can pause notifications during set hours ([Asana Inbox help](https://help.asana.com/s/article/inbox), [Asana inbox feature page](https://asana.com/features/project-management/inbox.md)).
- Linear Inbox: snooze with presets (hour, tomorrow, next week, custom); a snoozed entry returns unread; new activity cancels the snooze; delivery channels are set apart from the Inbox ([Linear Inbox](https://linear.app/docs/inbox), [Linear Snooze](https://linear.app/docs/snooze)).
- Notion comments: page and inline discussions, resolve and reopen, reactions on comments, replies from the Inbox, and a mention of someone who cannot open the page notifies nobody ([Notion comments, mentions and reactions](https://www.notion.com/help/comments-mentions-and-reminders)).

| Table stake | Notrelix today | Use case |
|---|---|---|
| Edit and delete own comment | Built; author-only rule enforced | UC-COL-A05, A06, C01 |
| Resolve and reopen threads | Built (pages) | UC-COL-A07, A08 |
| Reactions on comments | Items only | UC-COL-A09 to A11 |
| Mention notifies only people who can open the source | Items: yes; pages: no | UC-COL-B01, B02 |
| Reply from the Inbox | Built | UC-COL-G10 |
| Mark done, snooze, mute thread | Built | UC-COL-G07 to G09 |
| Archive all, clear batch | Read all only | UC-COL-G06 |
| Pause or quiet hours | Missing | UC-COL-G13 |
| Snooze cancelled by new activity | Not done (decision L12) | UC-COL-G08 |
| Notify on reply to my comment | Missing | UC-COL-D11 |
| Follow a page | Missing | UC-COL-B17 |
| Email and digest for notifications | Missing | UC-COL-D13, D14 |
| Live comment updates and presence | Missing | UC-COL-E01, E03 |
| Per-type, per-channel preferences | Built (one producer reads it) | UC-COL-G11, G12 |
| Search comments | Missing | UC-COL-G16 |

Added as "(enterprise baseline)" with state DEFINED: UC-COL-A11, B17, D09, D11, D12, D13, D14, G13, G16.

## 2. Index

Letter to capability map (team spec [documents-collaboration.md](../teams/documents-collaboration.md) section 4): A = COL-001 Comment lifecycle; B = COL-002 Comment target contract; C = COL-003 Comment authorization; D = COL-004 Comment activity/events; E = COL-005 Collaboration realtime; F = COL-006 Target deletion/retention; G = COL-007 Collaboration query/state; H = COL-008 Collaboration hardening.

| ID | Use case | Capability | Actors | BE | FE | Design | Real e2e | State | Gap |
|---|---|---|---|---|---|---|---|---|---|
| UC-COL-A01 | Comment on a board item | A | Workspace Owner, Admin, Member | yes | yes | yes | yes | HARDENING | No idempotency key; validator and Domain limits differ; no 1-px diff, owner check or axe record. |
| UC-COL-A02 | Comment on a Documents page | A | Workspace Owner, Admin, Member with Commenter level on the page | yes | yes | yes | partial | HARDENING | Same gaps as UC-COL-A01; no real e2e for the docs panel itself. |
| UC-COL-A03 | Reply in a thread | A | Same as UC-COL-A01 or UC-COL-A02 | yes | yes | yes | no | INTEGRATING | No real e2e; behaviour when the parent is deleted is undefined (UC-COL-A06). |
| UC-COL-A04 | Anchor a page comment to text | A | Same as UC-COL-A02 | yes | yes | yes | no | INTEGRATING | No real e2e; no Application test for foreign-Block rejection found. |
| UC-COL-A05 | Edit a comment | A | The comment's author (Lead decision L1) | yes | yes | yes | no | INTEGRATING | Author-only rule in place; no expected version and no real e2e. |
| UC-COL-A06 | Delete a comment | A | The author (Lead decision L1) | yes | yes | yes | no | INTEGRATING | Author-only rule in place; no expected version, no real e2e; list query drops the deleted comment and leaves replies with an orphan ParentId. |
| UC-COL-A07 | Resolve a thread | A | The thread starter, or a person with edit right on the target (Product | yes | partial | yes | no | INTEGRATING | Thread-starter-or-editor rule in place; no real e2e; item UI for resolve not found. |
| UC-COL-A08 | Reopen a thread | A | Same rule as UC-COL-A07 (decision 24) | yes | partial | yes | no | INTEGRATING | As UC-COL-A07. |
| UC-COL-A09 | React to a comment on a board item | A | Anyone with CreateComment on the item | yes | yes | yes | no | INTEGRATING | Check-then-insert with no unique index: concurrent requests can store two rows; no real e2e. |
| UC-COL-A10 | Withdraw a reaction | A | The person who reacted (only their own reaction) | yes | yes | yes | no | INTEGRATING | No real e2e. |
| UC-COL-A11 | React to a comment on a page (enterprise baseline) | A | Anyone with CreateComment on the page | no | no | no | no | DEFINED | Add page routes and a target-generic reaction command; design the chips for the docs panel. |
| UC-COL-B01 | Mention a person in an item comment | B | Any commenter | yes | yes | yes | yes | HARDENING | Mention of a Team, Page or Board has no use case; delivery-time recheck absent (UC-COL-B02). |
| UC-COL-B02 | Mention a person in a page comment | B | Any page commenter | partial | yes | yes | partial | INTEGRATING | Defect: page comments create a Mention for any listed user without a view check. |
| UC-COL-B03 | Mention a person inside a page block | B | A page editor (Documents UC) | yes | yes | yes | yes | HARDENING | No recorded owner check or design diff. |
| UC-COL-B04 | Mention a team | B | Any commenter | no | no | no | no | DEFINED | Add a team mention field, fan-out at delivery and the suggestion list. |
| UC-COL-B05 | Suggest people to mention without leaking others | B | Any commenter | partial | partial | yes | no | INTEGRATING | No Collaboration-owned suggestion read; the page composer has no suggestion list proven; no real e2e. |
| UC-COL-B06 | Attach a file to a board item | B | Anyone with UpdateItem on the item | yes | yes | yes | no | INTEGRATING | No real e2e; no cross-tenant negative upload test found; no virus scan. |
| UC-COL-B07 | Attach a link to a board item | B | Anyone with UpdateItem on the item | yes | partial | yes | no | INTEGRATING | No Url scheme allow-list found in the validator (javascript: and data: must be refused); no real e2e. |
| UC-COL-B08 | Attach a file to a page | B | Anyone with UpdatePage on the page | yes | yes | yes | no | INTEGRATING | No real e2e (documents.real e2e has no upload). |
| UC-COL-B09 | Register a link on a page | B | Anyone with UpdatePage on the page | yes | partial | yes | no | INTEGRATING | As UC-COL-B07. |
| UC-COL-B10 | List and open attachments of an item or page | B | Anyone who can view the item (ViewBoard) or page (ViewPage) | yes | yes | yes | no | INTEGRATING | No real e2e. |
| UC-COL-B11 | Delete an attachment from an item | B | A person with UpdateItem on the item (Lead decision) | yes | yes | yes | no | INTEGRATING | Editor-only rule in place; no real e2e. |
| UC-COL-B12 | Delete an attachment from a page | B | A person with UpdatePage on the page (Lead decision) | yes | partial | yes | no | INTEGRATING | Editor-only rule in place; no real e2e. |
| UC-COL-B13 | Follow a board item | B | Anyone who can view the item's board (ViewBoard) | yes | yes | yes | yes | HARDENING | No unique index on (user, target): concurrent calls can store two watchers. |
| UC-COL-B14 | Unfollow a board item | B | The follower | yes | yes | yes | yes | HARDENING | No recorded owner check or design diff. |
| UC-COL-B15 | See whether I follow an item | B | Anyone who can view the item | yes | yes | yes | yes | HARDENING | No recorded owner check or design diff. |
| UC-COL-B16 | Follow automatically after creating, being assigned or commenting | B | System, acting for the person | yes | partial | no | yes | HARDENING | No unit test of the follow consumers found; reason for following is not shown. |
| UC-COL-B17 | Follow a page (enterprise baseline) | B | Anyone who can view the page | no | no | no | no | DEFINED | Add page watcher routes, auto-follow for page commenters and the docs UI. |
| UC-COL-B18 | Add a new collaboration target kind | B | A team that owns a resource kind (system change, not an end-user flow) | yes | no | no | no | INTEGRATING | Two kinds only (board item, page); no registry document. |
| UC-COL-C01 | Author-only edit and delete of a comment | C | The comment's author (Lead decision L1) | yes | partial | yes | no | INTEGRATING | Backend rule and Application tests exist; no real e2e and no expected version. |
| UC-COL-C02 | Moderator removes another person's comment | C | A person Governance names as moderator of the target (not yet decided) | no | no | no | no | BLOCKED | Decide in Governance which permission (for example a new Moderate action held by Board Manager, Page Manager and workspace Admin and Owner)  |
| UC-COL-C03 | Viewer reads comments but cannot write | C | Viewer level on the target (ViewBoard or ViewPage only) | yes | yes | yes | no | INTEGRATING | Viewer write refusal not proven in a real e2e. |
| UC-COL-C04 | Guest collaborates only where explicitly shared | C | A Guest with explicit access to the item or page | yes | no | no | partial | INTEGRATING | The guest real e2e does not post a comment. |
| UC-COL-C05 | Identical NotFound across tenants and targets | C | Any caller | yes | no | yes | partial | INTEGRATING | No per-endpoint cross-tenant test for comments, reactions, watchers and inbox found. |
| UC-COL-C06 | Upload refused by file policy | C | A person allowed to upload (UC-COL-B06, B08) | yes | partial | yes | no | INTEGRATING | No real e2e; no malware scan (Lead decision L23 says not in this cut). |
| UC-COL-D01 | Read an item's activity history | D | Anyone with ViewBoard on the item | yes | yes | yes | no | INTEGRATING | No real e2e; offset paging (not a cursor); comments are not part of the history (decision 56). |
| UC-COL-D02 | Item facts become activity rows | D | System (consumers) | yes | no | no | no | INTEGRATING | No integration test through the real outbox for every fact; historical events before release have no rows (decision 56). |
| UC-COL-D03 | A comment becomes a workspace activity row | D | System | yes | partial | no | no | INTEGRATING | Title is a fixed English string ("Comment added to board-item") with the raw kind; no test; edit and delete are not reflected (product secti |
| UC-COL-D04 | A mention becomes an Inbox notification | D | System | yes | yes | yes | yes | HARDENING | The consumer does not re-check that the person can view the source at delivery (COL-030 says it must); the Inbox list hides unreadable entri |
| UC-COL-D05 | Followers are told about changes | D | System | yes | yes | yes | yes | HARDENING | No unit tests of the notifier; follower view access is not re-checked at delivery. |
| UC-COL-D06 | Approval requested and decided notify the right person | D | System | yes | yes | yes | yes | HARDENING | Preference type approval is stored but the consumers do not read it (verify). |
| UC-COL-D07 | Due-soon notification | D | System (daily job) | yes | yes | yes | no | INTEGRATING | No real e2e. |
| UC-COL-D08 | Nudge an assignee | D | A person with view access, never to self (decision 35) | yes | yes | yes | no | INTEGRATING | No real e2e. |
| UC-COL-D09 | Read a page's activity (enterprise baseline) | D | Anyone with ViewPage | no | no | no | no | DEFINED | Needs Documents to publish page facts and an activity reader for kind documents.page; version history stays a Documents feature. |
| UC-COL-D10 | Publish comment, reaction and attachment facts for other contexts | D | System | partial | no | no | no | IMPLEMENTING | Seven of nine fact kinds have no integration event; add them with schema versions. |
| UC-COL-D11 | A reply notifies the thread's author (enterprise baseline) | D | System | no | no | no | no | DEFINED | Build the consumer and Inbox wording; add the `reply` kind to the Inbox DTO copy. |
| UC-COL-D12 | A page comment notifies page followers (enterprise baseline) | D | System | no | partial | partial | no | DEFINED | Needs page watchers. |
| UC-COL-D13 | Email a notification (enterprise baseline) | D | System | no | partial | yes | no | DEFINED | Needs a Platform or Integrations provider decision; Lead decision L14 keeps it out of the current cut. |
| UC-COL-D14 | Daily digest (enterprise baseline) | D | System | no | partial | yes | no | DEFINED | Depends on UC-COL-D13. |
| UC-COL-E01 | See new comments live on an open item or page | E | Anyone with view access to the open target | no | no | partial | no | DEFINED | Add mappers for comment create, edit, delete, resolve and reaction; the client refetches on notify. |
| UC-COL-E02 | Inbox unread count updates live | E | The recipient | no | no | yes | no | DEFINED | Publisher missing. |
| UC-COL-E03 | See who else is viewing a page or item (presence) | E | Anyone with view access | partial | partial | no | no | IMPLEMENTING | Decide storage (Lead decision L16: gateway memory or Redis, not the PresenceSession table); build the topic, authorization and client. |
| UC-COL-E04 | Typing and cursor indicators | E | Editors of a page | no | no | no | no | BLOCKED | Waits for the live co-editing ADR that decision 3 leaves open (Documents owns it). |
| UC-COL-E05 | Recover after reconnect, gap or revoked access | E | Any connected client | partial | partial | yes | no | DEFINED | Gateway exists for board streams; no Collaboration stream to recover. |
| UC-COL-F01 | Item deleted or archived: comments follow the item | F | System | partial | no | partial | no | IMPLEMENTING | No purge consumer; orphan Collaboration rows remain after purge; no test. |
| UC-COL-F02 | Page removed or block deleted: comments stay as history | F | System | partial | yes | yes | no | IMPLEMENTING | Server side lifecycle missing; no real e2e. |
| UC-COL-F03 | Purge attachment objects after retention | F | System (background job) | yes | no | no | no | INTEGRATING | Retention default and operator doc not checked here. |
| UC-COL-F04 | A person is anonymized or leaves: history stays | F | System, on an Identity event | partial | partial | no | no | BLOCKED | Needs Identity's deletion and anonymization contract ([identity.md](identity.md)); privacy policy is not decided. |
| UC-COL-F05 | Workspace deleted or archived: retention, export and purge | F | System | no | no | no | no | BLOCKED | Destructive data policy; needs the Workspaces deletion workflow ([workspaces.md](workspaces.md)). |
| UC-COL-F06 | Notification retention | F | System (daily job) | no | no | no | no | DEFINED | Job missing; the table grows without bound. |
| UC-COL-F07 | Restore a deleted comment | F | A moderator or the author within a window (undecided) | partial | no | no | no | BLOCKED | Depends on UC-COL-C02 (who may restore). |
| UC-COL-G01 | List the comments of an item or page | G | ViewBoard on the item or view on the page | yes | yes | yes | partial | INTEGRATING | Unbounded read; page comments read under the ViewBoard action name instead of ViewPage (confirm the engine maps it). |
| UC-COL-G02 | Comment counts on cards and lists | G | Anyone who can see the card | yes | yes | yes | no | INTEGRATING | Counting rule for deleted and resolved comments undocumented. |
| UC-COL-G03 | Open the Inbox list | G | Any active member | yes | yes | yes | yes | HARDENING | The handler lives in the Work Management feature although Collaboration owns the Inbox; no design-fidelity spec for Inbox. |
| UC-COL-G04 | Unread count per workspace (bell) | G | Any signed-in person | yes | yes | yes | no | INTEGRATING | No real e2e; not live (UC-COL-E02). |
| UC-COL-G05 | Mark one notification read | G | The recipient | yes | yes | yes | yes | HARDENING | No recorded owner check or design diff. |
| UC-COL-G06 | Mark every notification read | G | The recipient | yes | yes | yes | no | INTEGRATING | No real e2e; one statement over all rows (fine until very large). |
| UC-COL-G07 | Mark done and take back into the Inbox | G | The recipient | yes | yes | yes | yes | HARDENING | No recorded owner check or design diff. |
| UC-COL-G08 | Snooze a notification | G | The recipient | yes | yes | yes | yes | HARDENING | No recorded owner check or design diff. |
| UC-COL-G09 | Mute and unmute a thread | G | The recipient | yes | yes | yes | yes | HARDENING | Follower and approval consumers' honouring of mute not proven by test. |
| UC-COL-G10 | Reply to a mention from the Inbox | G | The recipient with CreateComment on the target | yes | yes | yes | yes | HARDENING | No recorded owner check or design diff. |
| UC-COL-G11 | Read my notification preferences | G | An active member, only for themself in that Workspace | yes | yes | yes | no | INTEGRATING | No real e2e. |
| UC-COL-G12 | Save my notification preferences | G | The person, only their own matrix | yes | yes | yes | no | INTEGRATING | Only the mention producer reads the matrix; follower, approval, assigned, due consumers must read it; no real e2e. |
| UC-COL-G13 | Quiet hours and pause (enterprise baseline) | G | The person | no | no | no | no | DEFINED | Meaningful only after UC-COL-D13. |
| UC-COL-G14 | Unread comment markers (read state) | G | Any viewer | partial | no | no | no | IMPLEMENTING | No Application or API; decide before building (Lead decision L17). |
| UC-COL-G15 | Switch workspace or account without leaking collaboration state | G | Any signed-in person | no | yes | no | no | INTEGRATING | No real e2e of a workspace switch with comments open. |
| UC-COL-G16 | Search comments (enterprise baseline) | G | Anyone, limited to targets they can view | no | no | partial | no | DEFINED | Owned together with Search; not built. |
| UC-COL-H01 | Duplicate comment submit has one effect | H | Any commenter | no | no | no | no | DEFINED | Backend and client both missing. |
| UC-COL-H02 | Competing edits carry an expected version | H | Author, resolver | no | no | no | no | DEFINED | Add version to the DTO, commands and UI conflict state. |
| UC-COL-H03 | Concurrent reactions and follows do not duplicate | H | Any person | partial | no | no | no | IMPLEMENTING | Migration and concurrency test (RLS-safe change rule applies). |
| UC-COL-H04 | Comment content is safe to render | H | Any reader | partial | partial | no | no | IMPLEMENTING | Align validator to 10,000; add XSS and scheme tests on the renderer. |
| UC-COL-H05 | Rate limits and abuse controls | H | Any caller | no | no | no | no | DEFINED | Policies not attached. |
| UC-COL-H06 | A mention retry notifies once | H | System | yes | no | no | no | INTEGRATING | Read-then-insert without a confirmed unique index on DeduplicationKey; add a test with concurrent delivery. |
| UC-COL-H07 | Provider comment sync maps through Collaboration operations | H | An integration (provider adapter) | no | no | no | no | BLOCKED | Needs the integration execution principal ([automation-integrations.md](automation-integrations.md)); a security-principal decision. |
| UC-COL-H08 | Guest and public comments | H | Anonymous or public-link visitors | no | no | no | no | BLOCKED | Anonymous principal and abuse controls are an owner decision. |

## 3. Use cases

### Capability A: COL-001 Comment lifecycle

### UC-COL-A01 Comment on a board item
- Capability: A, COL-001 Comment lifecycle
- Actors and permission: Workspace Owner, Admin, Member; Guest only with explicit access to the item. Permission CreateComment on the item (Commenter level or above).
- Preconditions: The item exists in the caller's Workspace and is not deleted; the caller can view it.
- Input: ContentMd 1 to 10,000 characters after trim (Domain limit; the validator allows 50,000, see defects); optional MentionedUserIds; optional ParentCommentId.
- Output: A Comment (Active) on the item; comment-created fact in the outbox; follower notifications, activity row and mention notifications follow (UC-COL-D03 to D05). The item is not changed (COL-002).
- Main flow: 1. Caller types in the item's Updates tab and sends. 2. POST /board-items/{id}/comments. 3. Pipeline authorizes CreateComment on the item. 4. Handler builds Comment with target ResourceRef and Workspace from the request context. 5. Mentions are created for people who can view the board. 6. The list refetches.
- No-op / rejection / concurrency / retry: Empty or too long text: 400. No access or other Workspace: identical NotFound. Viewer: 403. Retry or double click: creates two comments (no idempotency key, see UC-COL-H01). No expected version applies to a create.
- UI states: loading, empty (no comments), success, validation error, permission denied, read-only (viewer sees no composer), pending, offline, optimistic temporary id.
- Rules: Product COL-001, COL-002, COL-003, COL-022; spec DCREQ037, DCREQ040 to DCREQ044, DCREQ047.
- Acceptance: Given a Member who can comment, when they send text, then the comment appears for every reader with the author's name and the item is unchanged. Given a Viewer, then the composer is absent and the API returns 403. Given a user of another Workspace, then the API returns the same NotFound as for a missing item.
- Evidence: BE POST /api/v1/board-items/{boardItemId}/comments (Collaboration.Comments.CreateBoardItemComment), CreateCommentCommandHandler (backend/src/Notrelix.Application/Features/Collaboration/Comments/Commands/CreateComment/) · FE frontend/packages/product/work-management/state/src/ (item-comments.api.ts, use-card-comments.ts, mutations); frontend/packages/features/collaboration/src/web/components/resource-comments.tsx · Design WM-Comments, Final-Detail, Final-T-Detail, Final-M-Detail · Tests backend/tests/Notrelix.Application.Tests/Features/Collaboration/CreateCommentResourceRefTests.cs, CreateCommentTargetPolicyMatrixTests.cs; backend/tests/Notrelix.Domain.Tests/Collaboration/Comments/CommentTests.cs; backend/tests/Notrelix.Integration.Tests/Collaboration/CommentCreatedOutboxEvidenceTests.cs; frontend/e2e/mock/comment-replies-reactions.mock.e2e.spec.ts · Real e2e frontend/e2e/real/inbox.real.e2e.spec.ts (comment POST), follow-notifications.real.e2e.spec.ts
- State: HARDENING — No idempotency key; validator and Domain limits differ; no 1-px diff, owner check or axe record.

### UC-COL-A02 Comment on a Documents page
- Capability: A, COL-001 Comment lifecycle
- Actors and permission: Workspace Owner, Admin, Member with Commenter level on the page; Guest with explicit access. Permission CreateComment on the page.
- Preconditions: The page exists in the caller's Workspace, is not in the trash and the caller can view it.
- Input: As UC-COL-A01; optional Anchor (UC-COL-A04); MentionedUserIds.
- Output: A Comment on the page; comment-created fact; activity row. No follower notification exists for pages (UC-COL-B17).
- Main flow: 1. Caller opens the comments panel of the page and sends. 2. POST /pages/{id}/comments. 3. Authorization on the page. 4. Comment saved. 5. Panel refetches.
- No-op / rejection / concurrency / retry: As UC-COL-A01. A page in the trash is NotFound for comment writes. Mentions of people who cannot view the page are not filtered (defect, UC-COL-B02).
- UI states: loading, empty, success, validation error, permission denied, read-only, pending, offline.
- Rules: Product COL-001, COL-003, COL-022; spec DCREQ040 to DCREQ044; Documents rules in [documents.md](documents.md).
- Acceptance: Given an authorized Member, when they send a comment on a page, then it shows in the panel for all page readers. Given a user with no page access, then NotFound.
- Evidence: BE POST /api/v1/pages/{pageId}/comments (Collaboration.Comments.CreatePageComment), CreateCommentCommandHandler · FE frontend/packages/product/docs/state/src/query/hooks/ (use-page-comments, use-create-comment, ...); frontend/packages/product/docs/web/src/components/doc-page-screen.tsx · Design Final-Docs-2, Final-Docs-2-320-Sheet, Final-M-Docs · Tests backend/tests/Notrelix.Application.Tests/Features/Collaboration/CreateCommentTargetPolicyMatrixTests.cs; backend/tests/Notrelix.API.Tests/Contracts/PageCommentAnchorContractOpenApiTests.cs; frontend/packages/product/docs/state/src/__tests__/page-comment-resolve.unit.test.ts · Real e2e frontend/e2e/real/inbox-page-reply.real.e2e.spec.ts (page comment POST)
- State: HARDENING — Same gaps as UC-COL-A01; no real e2e for the docs panel itself.

### UC-COL-A03 Reply in a thread
- Capability: A, COL-001 Comment lifecycle
- Actors and permission: Same as UC-COL-A01 or UC-COL-A02.
- Preconditions: The parent comment exists on the same target, is not deleted and is in the same Account and Workspace.
- Input: ParentCommentId plus the comment fields. A reply to a reply joins the top-level comment (one level deep).
- Output: A reply Comment with ParentId = top-level comment; comment-created fact carrying ParentCommentId.
- Main flow: 1. Caller presses Reply under a comment. 2. POST on the same target route with ParentCommentId. 3. Handler loads the live parent, climbs to the top-level one. 4. Comment.CreateReply checks scope, target and deleted state.
- No-op / rejection / concurrency / retry: Parent missing or deleted: NotFound or CannotReplyToDeleted. Parent on another target: ParentMustBeInSameTarget. Parent in another Workspace: ParentScopeMismatch.
- UI states: loading, success, validation error, permission denied, read-only, folded replies (more than three), pending.
- Rules: Product COL-004, COL-006; decision 60 (one level); spec DCREQ150 to DCREQ152, DCREQ214.
- Acceptance: Given a thread, when a member replies to a reply, then the reply sits under the top-level comment, oldest first. Given a parent of another item, then the API refuses with ParentMustBeInSameTarget.
- Evidence: BE POST .../comments with parentCommentId (same endpoints as A01, A02); Comment.CreateReply · FE frontend/packages/product/work-management/state/src/ (item-comments.api.ts, use-card-comments.ts, mutations); resource-comments.tsx (Show N more replies) · Design WM-Comments · Tests backend/tests/Notrelix.Domain.Tests/Collaboration/Comments/CommentReplyInvariantTests.cs, CommentReplyScopeTests.cs; frontend/e2e/mock/comment-replies-reactions.mock.e2e.spec.ts · Real e2e none
- State: INTEGRATING — No real e2e; behaviour when the parent is deleted is undefined (UC-COL-A06).

### UC-COL-A04 Anchor a page comment to text
- Capability: A, COL-001 Comment lifecycle
- Actors and permission: Same as UC-COL-A02. Only top-level comments on a page.
- Preconditions: The anchored Block is live and belongs to the page.
- Input: Anchor {blockId, quote, start, end}: offsets in UTF-16 code units of the Block's plain text; quote length equals end minus start.
- Output: A Comment with a text-range anchor. The stored anchor is never rewritten.
- Main flow: 1. Caller selects text and chooses Comment. 2. POST /pages/{id}/comments with Anchor. 3. Handler rejects a reply or non-page target, builds the anchor, asks Documents whether the Block belongs to the page. 4. Editor shows the highlight; if the quote is gone it shows the comment as detached with its quote.
- No-op / rejection / concurrency / retry: Anchor on an item comment, on a reply, on a foreign Block, or with a mismatched quote: AnchorInvalid (400/422).
- UI states: loading, success, validation error, detached (stale anchor), read-only, mobile sheet.
- Rules: Product COL-005 and the Comment Anchor section; spec DCREQ147 to DCREQ149.
- Acceptance: Given selected text, when the comment is sent, then the highlight and the panel entry show. Given the text is later deleted, then the comment shows detached with its quote and is never attached to other text.
- Evidence: BE POST /pages/{pageId}/comments (anchor), CreateCommentCommandHandler.ResolveAnchorAsync, CommentAnchor.ForTextRange, IPageBlockFacts · FE frontend/packages/product/docs/web/src/editor/comment-anchors.ts, frontend/packages/product/docs/core/src/types/comment.ts · Design Final-Docs-2 · Tests backend/tests/Notrelix.Domain.Tests/Collaboration/Comments/CommentAnchorTests.cs; backend/tests/Notrelix.API.Tests/Contracts/PageCommentAnchorContractOpenApiTests.cs; frontend/packages/product/docs/core/src/__tests__/comment-anchor.unit.test.ts; frontend/packages/product/docs/state/src/__tests__/page-comment-anchor.unit.test.ts · Real e2e none
- State: INTEGRATING — No real e2e; no Application test for foreign-Block rejection found.

### UC-COL-A05 Edit a comment
- Capability: A, COL-001 Comment lifecycle
- Actors and permission: The comment's author (Lead decision L1), while the author can still view the target.
- Preconditions: The comment is live and its target is readable by the caller.
- Input: ContentMd (same limits); optional MentionedUserIds for newly added people (Lead decision L2); ExpectedVersion (Lead decision L2).
- Output: Content replaced; version +1; comment-updated domain event; the list shows an edited mark. Same text after trim is a semantic no-op: no version, no event.
- Main flow: 1. Author chooses Edit, changes text, saves. 2. PATCH /comments/{id}. 3. Authorization. 4. Comment.UpdateContent.
- No-op / rejection / concurrency / retry: Not the author: 403. Deleted comment: NotFound. Stale ExpectedVersion: 412 with the current comment. Empty text: 400.
- UI states: loading, success, validation error, permission denied, conflict, pending, edited mark.
- Rules: Product COL-006 area, Comment edit, Edited-comment mentions; spec DCREQ045, DCREQ155.
- Acceptance: Given the author, when they edit, then everyone sees the new text and an edited mark. Given another Member, then 403. Given a stale version, then 412 and the text is unchanged.
- Evidence: BE PATCH /api/v1/comments/{commentId} (Collaboration.Comments.Update), UpdateCommentCommandHandler · FE frontend/packages/product/work-management/state/src/ (item-comments.api.ts, use-card-comments.ts, mutations) (use-update-card-update.ts) · Design WM-Comments · Tests backend/tests/Notrelix.Domain.Tests/Collaboration/Comments/CommentTests.cs (UpdateContent); frontend mock handler cards.handlers.ts ; backend/tests/Notrelix.Application.Tests/Features/Collaboration/CommentAuthorizationTests.cs (author edit, other member 403, no view access NotFound); backend/tests/Notrelix.Integration.Tests/Governance/GovernanceResourcePermissionFlowTests.cs (UpdateComment_On*) · Real e2e none
- State: INTEGRATING — Author-only rule implemented (author and view access decided in the handler from the comment's target); no expected version (Lead decision L2) and no real e2e before this can be DONE. Moderator delete stays UC-COL-C02.

### UC-COL-A06 Delete a comment
- Capability: A, COL-001 Comment lifecycle
- Actors and permission: The author (Lead decision L1); moderator delete is BLOCKED (UC-COL-C02).
- Preconditions: The comment is live.
- Input: Comment id; ExpectedVersion (Lead decision L2).
- Output: Soft delete (DeletedAt); comment-deleted event. A top-level comment with replies stays as a placeholder "This comment was deleted" with its replies (Lead decision L3); without replies it disappears. Delete of a deleted comment is a no-op.
- Main flow: 1. Author chooses Delete and confirms. 2. DELETE /comments/{id}. 3. Authorization. 4. Comment.Delete.
- No-op / rejection / concurrency / retry: Not the author: 403. Unknown: NotFound. Stale version: 412.
- UI states: destructive confirmation, success, permission denied, conflict, pending, placeholder thread.
- Rules: Product COL-006, Comment delete, Thread deletion; spec DCREQ037, DCREQ045, DCREQ214.
- Acceptance: Given the author, when they confirm delete, then the text is gone for all readers and replies stay under a placeholder. Given another Member, then 403.
- Evidence: BE DELETE /api/v1/comments/{commentId} (Collaboration.Comments.Delete), DeleteCommentCommandHandler · FE frontend/packages/product/work-management/state/src/ (item-comments.api.ts, use-card-comments.ts, mutations) (use-delete-card-update.ts); frontend/packages/product/docs/state/src/query/hooks/ (use-page-comments, use-create-comment, ...) (use-delete-comment.ts) · Design WM-Comments, Final-Docs-2 · Tests backend/tests/Notrelix.Domain.Tests/Collaboration/Comments/CommentDeletionAtomicityTests.cs ; backend/tests/Notrelix.Application.Tests/Features/Collaboration/CommentAuthorizationTests.cs (author delete, other member 403, no view access NotFound) · Real e2e none
- State: INTEGRATING — Author-only rule implemented; no expected version, no real e2e; list query drops the deleted comment and leaves replies with an orphan ParentId.

### UC-COL-A07 Resolve a thread
- Capability: A, COL-001 Comment lifecycle
- Actors and permission: The thread starter, or a person with edit right on the target (Product decision (owner, 2026-10-26) 20, applied to items by Lead decision L1).
- Preconditions: The top-level comment is live.
- Input: Comment id.
- Output: CommentStatus Resolved, version +1, comment-resolved event; the thread hides behind "Show resolved"; resolving a resolved thread is a no-op.
- Main flow: 1. Caller presses Resolve on a thread. 2. POST /comments/{id}/resolve. 3. Comment.Resolve.
- No-op / rejection / concurrency / retry: No right: 403. Deleted or unknown: NotFound.
- UI states: success, permission denied, pending, resolved filter, highlight dimmed in the editor.
- Rules: Product Comment status (CommentStatus is local); decision 20; spec DCREQ045.
- Acceptance: Given a thread, when an allowed person resolves it, then it leaves the default list and shows under resolved. Given a repeat, then nothing changes.
- Evidence: BE POST /api/v1/comments/{commentId}/resolve (Collaboration.Comments.Resolve), ResolveCommentCommandHandler · FE frontend/packages/product/docs/state/src/query/hooks/ (use-page-comments, use-create-comment, ...) (use-resolve-comment.ts); frontend/packages/dev/mock-backend/src/contexts/documents/comments.handlers.ts · Design Final-Docs-2 · Tests backend/tests/Notrelix.Application.Tests/Features/Collaboration/GetCommentsResolvedTests.cs; frontend/packages/product/docs/state/src/__tests__/page-comment-resolve.unit.test.ts ; backend/tests/Notrelix.Application.Tests/Features/Collaboration/CommentAuthorizationTests.cs (thread starter, editor of the target, plain commenter 403) · Real e2e none
- State: INTEGRATING — Thread-starter-or-editor rule implemented; no real e2e; item UI for resolve not found.

### UC-COL-A08 Reopen a thread
- Capability: A, COL-001 Comment lifecycle
- Actors and permission: Same rule as UC-COL-A07 (decision 24).
- Preconditions: The thread is Resolved.
- Input: Comment id.
- Output: CommentStatus Active, version +1, comment-reopened event; an Active thread is a no-op.
- Main flow: 1. Caller presses Reopen under Show resolved. 2. POST /comments/{id}/reopen.
- No-op / rejection / concurrency / retry: As UC-COL-A07.
- UI states: success, permission denied, pending.
- Rules: Decisions 20 and 24; spec DCREQ045.
- Acceptance: Given a resolved thread, when an allowed person reopens it, then it returns to the default list.
- Evidence: BE POST /api/v1/comments/{commentId}/reopen (Collaboration.Comments.Reopen), ReopenCommentCommandHandler · FE frontend/packages/product/docs/state/src/query/hooks/ (use-page-comments, use-create-comment, ...) (use-reopen-comment.ts) · Design Final-Docs-2 · Tests backend/tests/Notrelix.Application.Tests/Features/Collaboration/ReopenCommentTests.cs ; backend/tests/Notrelix.Application.Tests/Features/Collaboration/CommentAuthorizationTests.cs (thread starter, editor of the target, plain commenter 403) · Real e2e none
- State: INTEGRATING — As UC-COL-A07; no real e2e.

### UC-COL-A09 React to a comment on a board item
- Capability: A, COL-001 Comment lifecycle
- Actors and permission: Anyone with CreateComment on the item; a Viewer sees chips but cannot react (decision 59).
- Preconditions: The comment is live and on this item.
- Input: Emoji from the fixed palette (thumbs up, heart, smile, party, eyes, rocket).
- Output: One Reaction per person per emoji per comment; a repeat is a no-op.
- Main flow: 1. Caller clicks an emoji chip or picks from the palette. 2. PUT /board-items/{id}/comments/{commentId}/reactions/{emoji}. 3. Handler checks the comment sits on the item and adds the Reaction unless it exists.
- No-op / rejection / concurrency / retry: Emoji outside the palette: 400. Comment of another item: NotFound. Viewer: 403.
- UI states: success, optimistic toggle with rollback, permission denied, read-only chips with who reacted.
- Rules: Product COL-009, Reaction retry, Reaction count; decision 59; spec DCREQ158 to DCREQ161.
- Acceptance: Given a Member, when they click an emoji, then the chip count rises and highlights; a second click removes it. Given a Viewer, then the chips are read-only.
- Evidence: BE PUT /api/v1/board-items/{boardItemId}/comments/{commentId}/reactions/{emoji} (Collaboration.Comments.AddReaction), AddReactionCommandHandler · FE frontend/packages/product/work-management/state/src/ (item-comments.api.ts, use-card-comments.ts, mutations) (use-toggle-comment-reaction.ts) · Design WM-Comments · Tests backend/tests/Notrelix.Application.Tests/Features/Collaboration/CommentReactionTests.cs; backend/tests/Notrelix.Domain.Tests/Collaboration/Reactions/; backend/tests/Notrelix.API.Tests/Contracts/CommentReactionContractOpenApiTests.cs; frontend/e2e/mock/comment-replies-reactions.mock.e2e.spec.ts · Real e2e none
- State: INTEGRATING — Check-then-insert with no unique index: concurrent requests can store two rows; no real e2e.

### UC-COL-A10 Withdraw a reaction
- Capability: A, COL-001 Comment lifecycle
- Actors and permission: The person who reacted (only their own reaction).
- Preconditions: None beyond item access.
- Input: Emoji.
- Output: The caller's Reaction is removed; removing one that does not exist is a no-op (204).
- Main flow: 1. Caller clicks their highlighted chip. 2. DELETE .../reactions/{emoji}.
- No-op / rejection / concurrency / retry: No access: NotFound. Emoji outside the palette: 400.
- UI states: success, optimistic rollback, permission denied.
- Rules: Product Reaction retry; decision 59; spec DCREQ160.
- Acceptance: Given the caller holds a reaction, when they remove it, then the count drops; a repeat changes nothing.
- Evidence: BE DELETE /api/v1/board-items/{boardItemId}/comments/{commentId}/reactions/{emoji} (Collaboration.Comments.RemoveReaction), RemoveReactionCommandHandler · FE frontend/packages/product/work-management/state/src/ (item-comments.api.ts, use-card-comments.ts, mutations) (use-toggle-comment-reaction.ts) · Design WM-Comments · Tests backend/tests/Notrelix.Application.Tests/Features/Collaboration/CommentReactionTests.cs; frontend/e2e/mock/comment-replies-reactions.mock.e2e.spec.ts · Real e2e none
- State: INTEGRATING — No real e2e.

### UC-COL-A11 React to a comment on a page (enterprise baseline)
- Capability: A, COL-001 Comment lifecycle
- Actors and permission: Anyone with CreateComment on the page.
- Preconditions: The comment is live on the page.
- Input: Emoji from the same palette.
- Output: As UC-COL-A09, on a page comment.
- Main flow: 1. PUT /pages/{pageId}/comments/{commentId}/reactions/{emoji}. 2. DELETE to withdraw.
- No-op / rejection / concurrency / retry: As UC-COL-A09.
- UI states: As UC-COL-A09.
- Rules: Product COL-009; Lead decision L6; spec DCREQ158 to DCREQ161.
- Acceptance: Given a page comment, when a commenter picks an emoji, then the chip shows for all page readers.
- Evidence: BE none: reaction routes exist only under board-items; AddReactionCommand carries BoardItemId · FE none · Design none (Final-Docs-2 shows no reaction chips) · Tests none · Real e2e none
- State: DEFINED — Add page routes and a target-generic reaction command; design the chips for the docs panel.

### Capability B: COL-002 Comment target contract

### UC-COL-B01 Mention a person in an item comment
- Capability: B, COL-002 Comment target contract
- Actors and permission: Any commenter. The mentioned person must be a Workspace member who can view the item's board.
- Preconditions: Mentioned ids come from the composer's suggestion list (stable user ids, never typed names).
- Input: MentionedUserIds in the comment request; the display text `@name` stays in ContentMd.
- Output: One Mention per distinct person who can view the board; mention-created fact; Inbox notification and activity (UC-COL-D04). A person who cannot view stays as text and gets nothing (COL-030).
- Main flow: 1. Caller types @ and picks a person. 2. Comment is sent with ids. 3. Handler drops empty and repeated ids and people who cannot view the board; creates Mentions. 4. Consumers notify.
- No-op / rejection / concurrency / retry: Unknown id: kept out silently if not visible; a mention in a comment the retry sends twice creates two comments and two mentions (UC-COL-H01).
- UI states: suggestion list with a "will not be notified" mark, success, validation error.
- Rules: Product COL-007, COL-008, COL-030, Mention authorization; decision 42; spec DCREQ153 to DCREQ157.
- Acceptance: Given a Member who cannot view the board, when they are mentioned, then no Mention and no notification exist. Given a viewer of the board, then their Inbox shows the mention.
- Evidence: BE POST .../comments (mentionedUserIds), CreateCommentCommandHandler (ResolveMentionBoardAsync, CanViewBoardAsync), IResourceVisibility, IWorkItemNotificationFacts · FE frontend/packages/product/work-management/state/src/ (item-comments.api.ts, use-card-comments.ts, mutations); frontend/packages/features/collaboration/src/web/components/resource-comments.tsx · Design WM-Comments · Tests backend/tests/Notrelix.Domain.Tests/Collaboration/Mentions/MentionTests.cs; backend/tests/Notrelix.Application.Tests/Features/Collaboration/CreateCommentTargetPolicyMatrixTests.cs · Real e2e frontend/e2e/real/inbox.real.e2e.spec.ts
- State: HARDENING — Mention of a Team, Page or Board has no use case; delivery-time recheck absent (UC-COL-B02).

### UC-COL-B02 Mention a person in a page comment
- Capability: B, COL-002 Comment target contract
- Actors and permission: Any page commenter; the mentioned person must be able to view the page.
- Preconditions: As UC-COL-B01.
- Input: MentionedUserIds.
- Output: A Mention only for people who can view the page (Lead decision L7 adds the missing check).
- Main flow: As UC-COL-B01 with page visibility.
- No-op / rejection / concurrency / retry: A mention of a person who cannot view the page creates no Mention and no notification.
- UI states: As UC-COL-B01.
- Rules: Product COL-030, Mention privacy; decision 12; spec DCREQ157.
- Acceptance: Given a person without page access, when mentioned in a page comment, then no notification exists. Given a viewer, then one notification exists.
- Evidence: BE POST /pages/{pageId}/comments, CreateCommentCommandHandler (no page visibility check today: ResolveMentionBoardAsync returns null for pages) · FE frontend/packages/product/docs/state/src/query/hooks/ (use-page-comments, use-create-comment, ...) · Design Final-Docs-2 · Tests none for the page path · Real e2e frontend/e2e/real/inbox-page-reply.real.e2e.spec.ts (block mention, UC-COL-B03)
- State: INTEGRATING — Defect: page comments create a Mention for any listed user without a view check. Add IResourceVisibility.VisiblePageIdsAsync check and a test.

### UC-COL-B03 Mention a person inside a page block
- Capability: B, COL-002 Comment target contract
- Actors and permission: A page editor (Documents UC); the mentioned person must be able to view the page; never the writer themself.
- Preconditions: The editor saved blocks containing a mention.
- Input: Block rich text with a mention mark (user id).
- Output: A Mention only when newly added by that save; removing it retracts nothing (decision 12).
- Main flow: 1. Documents publishes page-blocks-saved. 2. PageBlocksSavedMentionConsumer turns each new mention into the same Mention as a comment, keyed by (save, block, person). 3. Notification path as UC-COL-D04.
- No-op / rejection / concurrency / retry: Redelivered save: no new Mention. Person without view: none.
- UI states: Inbox entry naming the page (success); see Documents for the editor.
- Rules: Decision 12; product COL-008; spec DCREQ156. Editor side: [documents.md](documents.md).
- Acceptance: Given a saved edit adds @person, when the consumer runs, then one notification exists; saving again adds none.
- Evidence: BE backend/src/Notrelix.Infrastructure/Messaging/Consumers/Collaboration/PageBlocksSavedMentionConsumer.cs · FE Inbox: workspace-inbox-surface.tsx; editor in Documents · Design Final-Inbox · Tests backend/tests/Notrelix.Infrastructure.Tests/Messaging/Consumers/PageBlocksSavedConsumersTests.cs · Real e2e frontend/e2e/real/inbox-page-reply.real.e2e.spec.ts
- State: HARDENING — No recorded owner check or design diff.

### UC-COL-B04 Mention a team
- Capability: B, COL-002 Comment target contract
- Actors and permission: Any commenter; Team members who can view the source are notified.
- Preconditions: The Team belongs to the Workspace.
- Input: Team id in the mention list.
- Output: A Mention of type Team; at delivery each member who can view the source gets a notification; others none (COL-030).
- Main flow: 1. Composer lists Teams. 2. Comment sent with team ids. 3. Mention consumer fans out to members at delivery time.
- No-op / rejection / concurrency / retry: Team of another Workspace: refused.
- UI states: suggestion list with Teams, success.
- Rules: Product Mention, COL-030 (Team mention); Lead decision L18; Teams live in [workspaces.md](workspaces.md).
- Acceptance: Given a Team of three where one cannot view the item, when the Team is mentioned, then two notifications exist.
- Evidence: BE none: MentionType.Team exists in Domain; the request has only MentionedUserIds · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Add a team mention field, fan-out at delivery and the suggestion list.

### UC-COL-B05 Suggest people to mention without leaking others
- Capability: B, COL-002 Comment target contract
- Actors and permission: Any commenter.
- Preconditions: None.
- Input: Typed text after @.
- Output: A list of members who can view the target, with those who cannot marked or left out.
- Main flow: 1. Caller types @. 2. Composer asks the member list of the board or page. 3. List shows only Workspace members.
- No-op / rejection / concurrency / retry: Never lists people outside the Workspace.
- UI states: loading, empty, success, "will not be notified" mark.
- Rules: Product Mention privacy (section 73), COL-030; decision 42.
- Acceptance: Given a user outside the Workspace, then they never appear in the list.
- Evidence: BE Workspace members and board members reads (owned by Workspaces and Work Management; see [work-management.md](work-management.md)) · FE frontend/packages/product/work-management/web (comment composer) · Design WM-Comments · Tests frontend component tests of the composer · Real e2e none
- State: INTEGRATING — No Collaboration-owned suggestion read; the page composer has no suggestion list proven; no real e2e.

### UC-COL-B06 Attach a file to a board item
- Capability: B, COL-002 Comment target contract
- Actors and permission: A person with UpdateItem on the item, whoever uploaded the file. Others who can view the item get 403 `collaboration.attachment_editor_only`; no view access is NotFound.
- Preconditions: The item exists; fewer than 50 live attachments.
- Input: multipart/form-data file, streamed; Idempotency-Key header. Size cap 100 MB, blocked extensions (exe, msi, dll, bat, cmd, com, scr, ps1, vbs, sh, jar, apk, js, mjs, html, htm), content type sniffed from the first 512 bytes.
- Output: An Attachment (stored file) with sniffed type; object in storage; 201 with the content route. Image types inline only for png, jpeg, gif, webp; all others download.
- Main flow: 1. Caller picks or drops a file. 2. POST /board-items/{id}/attachments/files. 3. Handler takes the target lock, checks count and policy, streams the object, saves metadata. 4. List refetches.
- No-op / rejection / concurrency / retry: Over size 413; blocked or executable content 415; over count 409; no access NotFound; replay with the same key returns the first result.
- UI states: loading, uploading progress, success, validation error, permission denied, failed upload, offline.
- Rules: Product COL-010, COL-011, Attachment authorization; decision 4; spec DCREQ162 to DCREQ166, DCREQ208.
- Acceptance: Given an editor, when they upload a PDF, then it lists and downloads; given an .exe, then 415 and nothing is stored; given the same key twice, then one attachment.
- Evidence: BE POST /api/v1/board-items/{boardItemId}/attachments/files (Collaboration.Attachments.UploadFile), UploadBoardItemAttachmentCommandHandler, AttachmentContentClassifier, IAttachmentTargetLock · FE frontend/packages/product/work-management/state/src/mutations/use-item-file-uploads.ts; frontend/packages/foundation/contracts/src/client/__tests__/upload.unit.test.ts · Design WM-Sheet-Files · Tests backend/tests/Notrelix.Application.Tests/Features/Collaboration/UploadBoardItemAttachmentTests.cs, AttachmentFileAuthorizationTests.cs; backend/tests/Notrelix.API.Tests/Collaboration/AttachmentFileEndpointTests.cs; backend/tests/Notrelix.Integration.Tests/Collaboration/AttachmentObjectLifecycleIntegrationTests.cs · Real e2e none
- State: INTEGRATING — No real e2e; no cross-tenant negative upload test found; no virus scan.

### UC-COL-B07 Attach a link to a board item
- Capability: B, COL-002 Comment target contract
- Actors and permission: Anyone with UpdateItem on the item.
- Preconditions: As UC-COL-B06.
- Input: Filename, Url, optional SizeBytes, ContentType, Source.
- Output: An Attachment that records a link (metadata only).
- Main flow: 1. Caller pastes a link. 2. POST /board-items/{id}/attachments.
- No-op / rejection / concurrency / retry: Invalid input 400; over count 409.
- UI states: success, validation error, permission denied.
- Rules: Product COL-010; spec DCREQ162.
- Acceptance: Given a valid link, then it lists on the item.
- Evidence: BE POST /api/v1/board-items/{boardItemId}/attachments (Collaboration.Attachments.Create), CreateBoardItemAttachmentCommandHandler · FE item sheet Files section (WM) · Design WM-Sheet-Files · Tests backend/tests/Notrelix.Domain.Tests/Collaboration/Attachments/AttachmentTests.cs · Real e2e none
- State: INTEGRATING — No Url scheme allow-list found in the validator (javascript: and data: must be refused); no real e2e.

### UC-COL-B08 Attach a file to a page
- Capability: B, COL-002 Comment target contract
- Actors and permission: A person with UpdatePage on the page, whoever uploaded the file. Others who can view the page get 403 `collaboration.attachment_editor_only`; no view access is NotFound.
- Preconditions: Fewer than 500 live attachments on the page.
- Input: As UC-COL-B06.
- Output: An Attachment on the page that an image or file block references by id.
- Main flow: 1. Editor inserts an image or file. 2. POST /pages/{id}/attachments/files with Idempotency-Key. 3. Block references the id.
- No-op / rejection / concurrency / retry: As UC-COL-B06; an attachment id of another target is not found through page routes.
- UI states: As UC-COL-B06.
- Rules: Product Attachment authorization (page target); decision 10; spec DCREQ163, DCREQ164.
- Acceptance: Given a page editor, when they upload an image, then the block shows it; given a viewer, then 403 or NotFound.
- Evidence: BE POST /api/v1/pages/{pageId}/attachments/files (Collaboration.PageAttachments.UploadFile), UploadPageAttachmentCommandHandler · FE frontend/packages/product/docs/state/src/query/hooks/ (use-remember-page-attachment.ts); frontend/packages/dev/mock-backend/src/contexts/documents/page-attachments.handlers.ts · Design Final-Docs-2 · Tests backend/tests/Notrelix.Application.Tests/Features/Collaboration/PageAttachmentTests.cs; backend/tests/Notrelix.API.Tests/Collaboration/PageAttachmentEndpointTests.cs; backend/tests/Notrelix.API.Tests/Contracts/PageAttachmentContractOpenApiTests.cs; frontend/packages/product/docs/state/src/__tests__/attachment-uploads.unit.test.ts, page-attachments.unit.test.ts · Real e2e none
- State: INTEGRATING — No real e2e (documents.real e2e has no upload).

### UC-COL-B09 Register a link on a page
- Capability: B, COL-002 Comment target contract
- Actors and permission: Anyone with UpdatePage on the page.
- Preconditions: As UC-COL-B08.
- Input: Filename, Url.
- Output: A link Attachment on the page.
- Main flow: POST /pages/{id}/attachments.
- No-op / rejection / concurrency / retry: As UC-COL-B07.
- UI states: success, validation error, permission denied.
- Rules: Product COL-010.
- Acceptance: Given a valid link, then it lists on the page.
- Evidence: BE POST /api/v1/pages/{pageId}/attachments (Collaboration.PageAttachments.Create), CreatePageAttachmentCommandHandler · FE docs state hooks · Design Final-Docs-2 · Tests backend/tests/Notrelix.Application.Tests/Features/Collaboration/PageAttachmentTests.cs · Real e2e none
- State: INTEGRATING — As UC-COL-B07.

### UC-COL-B10 List and open attachments of an item or page
- Capability: B, COL-002 Comment target contract
- Actors and permission: Anyone who can view the item (ViewBoard) or page (ViewPage).
- Preconditions: None.
- Input: Target id; for content, attachment id.
- Output: List of attachment metadata; content is streamed through the API after authorization; there is no public address and no durable signed URL.
- Main flow: 1. Caller opens Files. 2. GET .../attachments. 3. Caller opens one: GET .../attachments/{id}/content, forced download except safe inline images.
- No-op / rejection / concurrency / retry: An attachment of another target or Workspace: NotFound. Purged object: gone.
- UI states: loading, empty, success, permission denied, failed download.
- Rules: Product COL-011, attachment target kinds; spec DCREQ165.
- Acceptance: Given a viewer, when they open an attachment, then bytes stream; given a user without access, then the same NotFound as a missing id.
- Evidence: BE GET /api/v1/board-items/{boardItemId}/attachments and /{attachmentId}/content (Collaboration.Attachments.Get, GetContent); GET /api/v1/pages/{pageId}/attachments and /{attachmentId}/content (Collaboration.PageAttachments.Get, GetContent) · FE frontend/packages/product/docs/state/src/query/hooks/use-attachment-content.ts; WM item sheet · Design WM-Sheet-Files · Tests backend/tests/Notrelix.API.Tests/Collaboration/PageAttachmentEndpointTests.cs, AttachmentFileEndpointTests.cs · Real e2e none
- State: INTEGRATING — No real e2e.

### UC-COL-B11 Delete an attachment from an item
- Capability: B, COL-002 Comment target contract
- Actors and permission: Anyone with UpdateItem on the item.
- Preconditions: The attachment is live and belongs to the item.
- Input: Attachment id.
- Output: Soft delete; the stored object stays until the retention job purges it (UC-COL-F03).
- Main flow: 1. Caller removes a file. 2. DELETE /board-items/{id}/attachments/{attachmentId}.
- No-op / rejection / concurrency / retry: Unknown or other target: NotFound; no right: 403.
- UI states: destructive confirmation, success, permission denied.
- Rules: Product Attachment deletion; decision 10; spec DCREQ166.
- Acceptance: Given an editor, when they delete, then it leaves the list; given a viewer, then 403.
- Evidence: BE DELETE /api/v1/board-items/{boardItemId}/attachments/{attachmentId} (Collaboration.Attachments.Delete), DeleteAttachmentCommandHandler · FE frontend/packages/product/work-management/state/src/mutations/use-delete-card-file.ts · Design WM-Sheet-Files · Tests backend/tests/Notrelix.Domain.Tests/Collaboration/Attachments/AttachmentTests.cs ; backend/tests/Notrelix.Application.Tests/Features/Collaboration/AttachmentDeleteAuthorizationTests.cs (editor including the Owner, read-only uploader 403, other member 403, no view access NotFound) · Real e2e none
- State: INTEGRATING — Editor-only rule implemented, decided from the attachment's own target; no real e2e. The route's boardItemId is still ignored.

### UC-COL-B12 Delete an attachment from a page
- Capability: B, COL-002 Comment target contract
- Actors and permission: Anyone with UpdatePage on the page.
- Preconditions: The attachment belongs to exactly this page.
- Input: Attachment id.
- Output: Soft delete; deleting a block does not delete the attachment.
- Main flow: DELETE /pages/{id}/attachments/{attachmentId}.
- No-op / rejection / concurrency / retry: As UC-COL-B11.
- UI states: destructive confirmation, success, permission denied.
- Rules: Decision 10; product section 90.
- Acceptance: Given an editor, then the attachment is removed from the page list.
- Evidence: BE DELETE /api/v1/pages/{pageId}/attachments/{attachmentId} (Collaboration.PageAttachments.Delete), DeletePageAttachmentCommandHandler · FE docs state hooks · Design Final-Docs-2 · Tests backend/tests/Notrelix.Application.Tests/Features/Collaboration/PageAttachmentTests.cs ; backend/tests/Notrelix.Application.Tests/Features/Collaboration/AttachmentDeleteAuthorizationTests.cs · Real e2e none
- State: INTEGRATING — No real e2e.

### UC-COL-B13 Follow a board item
- Capability: B, COL-002 Comment target contract
- Actors and permission: Anyone who can view the item's board (ViewBoard).
- Preconditions: The item is readable.
- Input: Item id; Idempotency-Key header.
- Output: A ResourceWatcher for the caller; following twice is one watcher; the person is told of status changes, comments and assignment (UC-COL-D05).
- Main flow: 1. Caller presses Follow on the item. 2. PUT /board-items/{id}/following.
- No-op / rejection / concurrency / retry: No access: NotFound. Replay: same result.
- UI states: success, optimistic toggle, permission denied.
- Rules: Product COL-016, Watchers; decision 78.1; spec DCREQ175, DCREQ177, DCREQ178.
- Acceptance: Given a reader, when they follow, then the item appears under Following in My work and they get follower notifications.
- Evidence: BE PUT /api/v1/board-items/{boardItemId}/following (Collaboration.Watchers.Follow), FollowBoardItemCommandHandler · FE frontend/packages/product/work-management/web/src/components/card-detail/use-item-sheet-extras.ts · Design Final-Detail · Tests backend/tests/Notrelix.Application.Tests/Features/Collaboration/FollowBoardItemTests.cs; backend/tests/Notrelix.Domain.Tests/Collaboration/Watchers/ · Real e2e frontend/e2e/real/following.real.e2e.spec.ts
- State: HARDENING — No unique index on (user, target): concurrent calls can store two watchers.

### UC-COL-B14 Unfollow a board item
- Capability: B, COL-002 Comment target contract
- Actors and permission: The follower.
- Preconditions: None.
- Input: Item id; Idempotency-Key.
- Output: The watcher is removed; no comment, access or notification history is touched; a repeat is a no-op.
- Main flow: DELETE /board-items/{id}/following.
- No-op / rejection / concurrency / retry: No access: NotFound.
- UI states: success, optimistic toggle.
- Rules: Product Unwatch (section 51); spec DCREQ177.
- Acceptance: Given a follower, when they unfollow, then no new follower notification arrives and old ones stay.
- Evidence: BE DELETE /api/v1/board-items/{boardItemId}/following (Collaboration.Watchers.Unfollow), UnfollowBoardItemCommandHandler · FE item sheet extras · Design Final-Detail · Tests backend/tests/Notrelix.Application.Tests/Features/Collaboration/FollowBoardItemTests.cs · Real e2e frontend/e2e/real/following.real.e2e.spec.ts
- State: HARDENING — No recorded owner check or design diff.

### UC-COL-B15 See whether I follow an item
- Capability: B, COL-002 Comment target contract
- Actors and permission: Anyone who can view the item.
- Preconditions: None.
- Input: Item id.
- Output: Whether the caller follows (and how many follow, if exposed).
- Main flow: GET /board-items/{id}/following.
- No-op / rejection / concurrency / retry: No access: NotFound.
- UI states: loading, success.
- Rules: Product COL-016.
- Acceptance: Given a follower, then the toggle shows on.
- Evidence: BE GET /api/v1/board-items/{boardItemId}/following (Collaboration.Watchers.GetFollowing), GetBoardItemFollowingQueryHandler · FE item sheet extras; My work Following list (WM) · Design Final-Detail · Tests backend/tests/Notrelix.Application.Tests/Features/Collaboration/FollowBoardItemTests.cs · Real e2e frontend/e2e/real/following.real.e2e.spec.ts
- State: HARDENING — No recorded owner check or design diff.

### UC-COL-B16 Follow automatically after creating, being assigned or commenting
- Capability: B, COL-002 Comment target contract
- Actors and permission: System, acting for the person.
- Preconditions: A committed item fact.
- Input: board-item-created, member-assigned and comment-created facts.
- Output: The creator and the assignee become followers, once per message identity (decision 78.1); the explanation is shown in the item's follow toggle.
- Main flow: Consumers BoardItemCreatedFollowConsumer, BoardItemMemberAssignedFollowConsumer and CommentCreatedFollowConsumer call ItemFollowerNotifier.
- No-op / rejection / concurrency / retry: A redelivered fact adds nothing.
- UI states: Following state visible on the toggle with its reason (proposed, not built).
- Rules: Product COL-016 and Automatic watching (rule must be explicit); decision 78.1.
- Acceptance: Given an assignment, then the assignee follows the item without pressing Follow.
- Evidence: BE backend/src/Notrelix.Infrastructure/Messaging/Consumers/Collaboration/ItemFollowerConsumers.cs, ItemFollowerNotifier.cs · FE none beyond the toggle · Design none · Tests backend/tests/Notrelix.Infrastructure.Tests/Messaging/Consumers/ItemActivityConsumersTests.cs (activity); follow consumers covered by e2e · Real e2e frontend/e2e/real/follow-notifications.real.e2e.spec.ts
- State: HARDENING — No unit test of the follow consumers found; reason for following is not shown.

### UC-COL-B17 Follow a page (enterprise baseline)
- Capability: B, COL-002 Comment target contract
- Actors and permission: Anyone who can view the page.
- Preconditions: The page is readable.
- Input: Page id; Idempotency-Key.
- Output: A ResourceWatcher on the page; the person is told of new page comments (UC-COL-D12).
- Main flow: PUT and DELETE /pages/{id}/following; GET to read.
- No-op / rejection / concurrency / retry: No access: NotFound.
- UI states: success, optimistic toggle, permission denied.
- Rules: Product COL-016; Lead decision L7; Notion and Asana let people follow pages and tasks.
- Acceptance: Given a page reader, when they follow, then a later page comment notifies them.
- Evidence: BE none: watcher routes exist only for board items · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Add page watcher routes, auto-follow for page commenters and the docs UI.

### UC-COL-B18 Add a new collaboration target kind
- Capability: B, COL-002 Comment target contract
- Actors and permission: A team that owns a resource kind (system change, not an end-user flow).
- Preconditions: The owner context states scope, authorization, deep link, deletion behaviour and realtime scope.
- Input: Resource kind string, owner context, permissions.
- Output: The kind is accepted by comments, attachments, watchers and activity routes only after the contract is reviewed.
- Main flow: 1. Owner writes the target contract. 2. Collaboration adds factories and routes. 3. Governance maps permissions. 4. Tests cover scope and revocation.
- No-op / rejection / concurrency / retry: An unregistered kind is refused; a naked id is never trusted.
- UI states: not applicable.
- Rules: Product COL-001, COL-021; spec DCREQ041.
- Acceptance: Given a new kind without a contract, then no route accepts it.
- Evidence: BE backend/src/Notrelix.Application/Features/Collaboration/Comments/.. ResourceKind factories (CreateCommentCommand.ForBoardItem, ForPage); AttachmentTargets · FE none · Design none · Tests backend/tests/Notrelix.Application.Tests/Features/Collaboration/ResourceKindFactoriesTests.cs; backend/tests/Notrelix.Architecture.Tests/Contracts/CollaborationReadBoundaryArchitectureTests.cs · Real e2e none
- State: INTEGRATING — Two kinds only (board item, page); no registry document.

### Capability C: COL-003 Comment authorization

### UC-COL-C01 Author-only edit and delete of a comment
- Capability: C, COL-003 Comment authorization
- Actors and permission: The comment's author (Lead decision L1).
- Preconditions: The caller can still view the target.
- Input: Comment id.
- Output: Edit and delete succeed only for the author; another person gets 403 (target readable) or NotFound (target not readable).
- Main flow: 1. Application loads the comment. 2. Handler compares CreatedBy to the caller before calling the Domain. 3. Pipeline authorization stays on the target (CreateComment level).
- No-op / rejection / concurrency / retry: Not the author: 403 with a stable problem code. Comment of another Workspace: NotFound.
- UI states: Edit and Delete actions shown only to the author; permission denied toast if raced.
- Rules: Product Author versus moderator (section 72), COL-003 of the team spec; spec DCREQ045, DCREQ052; decision 20 (UI offers delete only to the author, the backend still authorizes).
- Acceptance: Given Member A's comment, when Member B sends PATCH or DELETE, then 403 and the comment is unchanged. Given A, then both succeed.
- Evidence: BE UpdateCommentCommandHandler and DeleteCommentCommandHandler call CommentAccessRules.RequireAuthorAsync (CreatedBy against the caller; view access to the target through CommentTargetAccess); pipeline gate is ViewBoard on the comment · FE frontend/packages/product/work-management/web (comment actions); docs panel shows delete to the author only · Design WM-Comments · Tests backend/tests/Notrelix.Application.Tests/Features/Collaboration/CommentAuthorizationTests.cs; backend/tests/Notrelix.Integration.Tests/Governance/GovernanceResourcePermissionFlowTests.cs (UpdateComment_On*) · Real e2e none
- State: INTEGRATING — Backend rule and Application tests exist; a real e2e is missing before HARDENING.

### UC-COL-C02 Moderator removes another person's comment
- Capability: C, COL-003 Comment authorization
- Actors and permission: A person Governance names as moderator of the target (not yet decided).
- Preconditions: None.
- Input: Comment id; optional reason.
- Output: Comment deleted with the moderator as actor; the author is not notified by default.
- Main flow: To be set after the decision.
- No-op / rejection / concurrency / retry: Not a moderator: 403.
- UI states: destructive confirmation with reason (proposed).
- Rules: Product Author versus moderator, section 106 (Governance owns moderation policy), COL-003 team capability.
- Acceptance: Given a moderator, when they delete another member's comment, then it disappears and the Domain records the moderator as the actor.
- Evidence: BE none; Comment.Delete accepts a reason argument that no use case passes · FE none · Design none · Tests none · Real e2e none
- State: BLOCKED — Decide in Governance which permission (for example a new Moderate action held by Board Manager, Page Manager and workspace Admin and Owner) allows deleting others' comments. Security-principal choice; the lead does not settle it here.

### UC-COL-C03 Viewer reads comments but cannot write
- Capability: C, COL-003 Comment authorization
- Actors and permission: Viewer level on the target (ViewBoard or ViewPage only).
- Preconditions: None.
- Input: Any write request.
- Output: Reads succeed; comment, reply, react, attach and resolve refuse with 403.
- Main flow: Pipeline compares the required rank (Commenter for CreateComment, Editor for UpdateItem and UpdatePage) with the caller's effective level.
- No-op / rejection / concurrency / retry: 403; NotFound when the target is not visible.
- UI states: read-only: composer hidden, chips visible with who reacted.
- Rules: Product Target authorization (section 71); decision 59; spec DCREQ044, DCREQ192.
- Acceptance: Given a Viewer, then the composer and reaction picker are absent, and each write API call returns 403.
- Evidence: BE Governance AccessPolicyEngine (CreateComment rank), Collaboration command Action declarations · FE resource-comments-surface.tsx read-only state · Design WM-Comments · Tests backend/tests/Notrelix.Application.Tests/Features/Collaboration/CreateCommentTargetPolicyMatrixTests.cs; frontend/e2e/mock/comment-replies-reactions.mock.e2e.spec.ts (viewer case); frontend/e2e/mock/work-management-roles.mock.e2e.spec.ts · Real e2e none
- State: INTEGRATING — Viewer write refusal not proven in a real e2e.

### UC-COL-C04 Guest collaborates only where explicitly shared
- Capability: C, COL-003 Comment authorization
- Actors and permission: A Guest with explicit access to the item or page.
- Preconditions: Guest has a resource permission on the target.
- Input: Comment or read request.
- Output: A Guest without explicit access on an item gets NotFound; with access can comment up to Editor level.
- Main flow: AccessPolicyEngine: guest without explicit resource permission is NotFound for CreateComment on items.
- No-op / rejection / concurrency / retry: NotFound (never 403) so existence is not revealed.
- UI states: as C03.
- Rules: Product Target authorization; [governance.md](../../product/governance.md) guests and sharing; spec DCREQ044.
- Acceptance: Given a Guest without access, then NotFound; given shared access, then they can comment.
- Evidence: BE backend/src/Notrelix.Application/Features/Governance/Authorization/AccessPolicyEngine.cs (CreateComment branch) · FE none specific · Design none · Tests backend/tests/Notrelix.Application.Tests/Features/Collaboration/CreateCommentTargetPolicyMatrixTests.cs; frontend/e2e/real/guest-permission.real.e2e.spec.ts · Real e2e frontend/e2e/real/guest-permission.real.e2e.spec.ts (guest sees shared page only)
- State: INTEGRATING — The guest real e2e does not post a comment.

### UC-COL-C05 Identical NotFound across tenants and targets
- Capability: C, COL-003 Comment authorization
- Actors and permission: Any caller.
- Preconditions: None.
- Input: A comment, attachment, reaction or watcher id or target id from another Workspace or Account, or an id that does not exist.
- Output: The same 404 body in both cases; no data from the other tenant; RLS blocks reads even if the Application check slipped.
- Main flow: Target authorization locates the resource; the handler filters by the request's Workspace.
- No-op / rejection / concurrency / retry: Never 403 for a foreign tenant.
- UI states: permission denied or not found page.
- Rules: Product COL-001, COL-022, Cross-Workspace target; spec DCREQ042, DCREQ047.
- Acceptance: Given an id of another Workspace, then the response equals the response for a random GUID.
- Evidence: BE Governance pipeline; ResourceLocator (collaboration.comment, collaboration.attachment); RLS policies · FE none · Design Final-States · Tests backend/tests/Notrelix.Integration.Tests/Integration/TenantIsolationTests.cs, CrossTenantIsolationTests.cs, RlsRuntimeEnforcementTests.cs; backend/tests/Notrelix.Domain.Tests/Collaboration/*/*WorkspaceScopeTests.cs · Real e2e frontend/e2e/real/workspace-isolation.real.e2e.spec.ts (not collaboration specific)
- State: INTEGRATING — No per-endpoint cross-tenant test for comments, reactions, watchers and inbox found.

### UC-COL-C06 Upload refused by file policy
- Capability: C, COL-003 Comment authorization
- Actors and permission: A person allowed to upload (UC-COL-B06, B08).
- Preconditions: None.
- Input: A file that is too big, has a blocked extension or executable content, or a mismatched type.
- Output: Nothing stored; a clear problem (413, 415, 409); the declared Content-Type is never trusted.
- Main flow: The upload handler reads the first 512 bytes, classifies, applies the blocked list and the size cap while streaming.
- No-op / rejection / concurrency / retry: 413 over size; 415 blocked type; 409 attachment limit.
- UI states: validation error with the reason and the limit, failed upload row with retry.
- Rules: Product COL-010, Attachment authorization (file policy); spec DCREQ162, DCREQ164.
- Acceptance: Given an .exe renamed .pdf, then 415; given a 101 MB file, then 413 and no object remains.
- Evidence: BE UploadBoardItemAttachment and UploadPageAttachment handlers, AttachmentContentClassifier, AttachmentUploadOptions (Collaboration:Attachments) · FE upload client (frontend/packages/foundation/contracts/src/client) · Design WM-Sheet-Files · Tests backend/tests/Notrelix.API.Tests/ProblemDetails/AttachmentProblemDetailsTests.cs; backend/tests/Notrelix.Application.Tests/Features/Collaboration/AttachmentFileAuthorizationTests.cs · Real e2e none
- State: INTEGRATING — No real e2e; no malware scan (Lead decision L23 says not in this cut).

### Capability D: COL-004 Comment activity/events

### UC-COL-D01 Read an item's activity history
- Capability: D, COL-004 Comment activity/events
- Actors and permission: Anyone with ViewBoard on the item.
- Preconditions: The item exists.
- Input: page (default 1), pageSize (default 20, max 100).
- Output: A typed page, newest first: stable type (work.item-*), actor name and avatar resolved at read time, English fallback phrase and a language-neutral change. Sensitive and long fields record that they changed, never the value.
- Main flow: 1. Caller opens the History tab. 2. GET /board-items/{id}/activity. 3. Reader returns rows for the Workspace and item.
- No-op / rejection / concurrency / retry: No access: NotFound. Unknown page: empty list.
- UI states: loading, empty, success, permission denied, load more.
- Rules: Product COL-019, COL-020 (Activity is not Audit); decision 56; spec DCREQ185 to DCREQ187.
- Acceptance: Given an item with edits, then the tab lists them newest first with the actor; given no access, then NotFound.
- Evidence: BE GET /api/v1/board-items/{boardItemId}/activity (Collaboration.Activity.GetResourceActivity), GetResourceActivityQueryHandler, ResourceActivityReader · FE frontend/packages/product/work-management/web/src/components/card-detail/task-activity-tab.tsx, item-page/item-page-activity.tsx · Design Final-Detail, Final-T-Detail · Tests backend/tests/Notrelix.Application.Tests/Features/Collaboration/GetResourceActivityTests.cs, ItemChangeWordsTests.cs; backend/tests/Notrelix.API.Tests/Contracts/ItemActivityContractOpenApiTests.cs; backend/tests/Notrelix.Infrastructure.Tests/CrossContext/WorkItemActivityFactsAdapterTests.cs; frontend task-activity-tab.component.test.tsx · Real e2e none
- State: INTEGRATING — No real e2e; offset paging (not a cursor); comments are not part of the history (decision 56).

### UC-COL-D02 Item facts become activity rows
- Capability: D, COL-004 Comment activity/events
- Actors and permission: System (consumers).
- Preconditions: Work Management committed the fact through the outbox.
- Input: Item created, renamed, field changed, moved, moved to board, assigned, unassigned, label added or removed, archived, unarchived, deleted, restored, checklist events.
- Output: One append-only row per source event id; a redelivery adds nothing.
- Main flow: One consumer per fact, declared ordering not applicable, writes through ItemActivityRecorder.
- No-op / rejection / concurrency / retry: Duplicate event id: ignored.
- UI states: none (read in D01).
- Rules: Product COL-020; decision 56; spec DCREQ186, DCREQ190.
- Acceptance: Given a redelivered event, then one row exists.
- Evidence: BE backend/src/Notrelix.Infrastructure/Messaging/Consumers/Collaboration/ItemActivityConsumers.cs, ItemActivityRecorder.cs · FE none · Design none · Tests backend/tests/Notrelix.Infrastructure.Tests/Messaging/Consumers/ItemActivityConsumersTests.cs · Real e2e none
- State: INTEGRATING — No integration test through the real outbox for every fact; historical events before release have no rows (decision 56).

### UC-COL-D03 A comment becomes a workspace activity row
- Capability: D, COL-004 Comment activity/events
- Actors and permission: System.
- Preconditions: comment-created fact.
- Input: CommentCreatedIntegrationEvent.
- Output: A WorkspaceActivityLog row "collab.comment-created" keyed by the event id; shown in the Workspace activity feed (owned by [workspaces.md](workspaces.md)).
- Main flow: CommentCreatedActivityConsumer.
- No-op / rejection / concurrency / retry: No workspace on the event: skipped.
- UI states: Workspace activity feed.
- Rules: Product COL-020; spec DCREQ186.
- Acceptance: Given a comment, then one activity row exists even after redelivery.
- Evidence: BE backend/src/Notrelix.Infrastructure/Messaging/Consumers/Collaboration/ActivityProjectionConsumers.cs (CommentCreatedActivityConsumer, MentionCreatedActivityConsumer, WorkspaceMemberAddedActivityConsumer, BoardCreatedActivityConsumer) · FE Workspace activity (Workspaces) · Design none · Tests none found for the consumer · Real e2e none
- State: INTEGRATING — Title is a fixed English string ("Comment added to board-item") with the raw kind; no test; edit and delete are not reflected (product section 88: Activity stays when a comment is deleted).

### UC-COL-D04 A mention becomes an Inbox notification
- Capability: D, COL-004 Comment activity/events
- Actors and permission: System; recipient is the mentioned person.
- Preconditions: mention-created fact; the person turned in-app mention on for the Workspace (default on); the thread is not muted.
- Input: MentionCreatedIntegrationEvent.
- Output: One NotificationItem and one recipient row, keyed mention-created:{mentionId}:{userId}; type mention; the self-mention is never created.
- Main flow: 1. Consumer reads the dedupe key. 2. Checks the in-app preference and the thread mute. 3. Creates the notification.
- No-op / rejection / concurrency / retry: Duplicate: no second notification. Preference off or muted: none, the Mention and activity stay.
- UI states: Inbox entry with excerpt and "needs reply".
- Rules: Product COL-008, COL-017, COL-018, COL-030, Preferences (producers); spec DCREQ156, DCREQ180 to DCREQ184.
- Acceptance: Given a mention, then one Inbox entry exists; given a redelivery, still one; given the preference off, none.
- Evidence: BE backend/src/Notrelix.Infrastructure/Messaging/Consumers/Collaboration/MentionCreatedNotificationConsumer.cs · FE frontend/packages/features/workspace/src/web/inbox/, frontend/apps/web/src/routes/workspaces/$workspaceId/inbox.tsx · Design Final-Inbox, Final-M-Inbox · Tests backend/tests/Notrelix.Integration.Tests/Collaboration/NotificationPreferencesIntegrationTests.cs; backend/tests/Notrelix.Application.Tests/Features/Collaboration/NotificationExcerptTests.cs · Real e2e frontend/e2e/real/inbox.real.e2e.spec.ts
- State: HARDENING — The consumer does not re-check that the person can view the source at delivery (COL-030 says it must); the Inbox list hides unreadable entries.

### UC-COL-D05 Followers are told about changes
- Capability: D, COL-004 Comment activity/events
- Actors and permission: System; recipients are followers except the actor.
- Preconditions: A committed item fact (status changed, assigned, comment created) and a follower who left the in-app preference of the type on.
- Input: Item facts.
- Output: One in-app notification per follower, keyed by the message identity; the actor is skipped.
- Main flow: ItemFollowerNotifier.NotifyFollowersAsync after the follow rules ran (UC-COL-B16). A comment also marks the author's own earlier mentions as replied.
- No-op / rejection / concurrency / retry: Duplicate: none. Muted thread or preference off: none.
- UI states: Inbox entries.
- Rules: Product Audience fan-out, COL-017; decision 78.2; spec DCREQ184.
- Acceptance: Given a follower and a later comment by another member, then the follower has one Inbox entry.
- Evidence: BE ItemFollowerConsumers.cs (BoardItemStatusChangedFollowConsumer, CommentCreatedFollowConsumer, BoardItemMemberAssignedFollowConsumer), ItemFollowerNotifier.cs · FE frontend/packages/features/workspace/src/web/inbox/, frontend/apps/web/src/routes/workspaces/$workspaceId/inbox.tsx · Design Final-Inbox · Tests covered by e2e only · Real e2e frontend/e2e/real/follow-notifications.real.e2e.spec.ts
- State: HARDENING — No unit tests of the notifier; follower view access is not re-checked at delivery.

### UC-COL-D06 Approval requested and decided notify the right person
- Capability: D, COL-004 Comment activity/events
- Actors and permission: System; the approver, then the requester.
- Preconditions: Work Management approval facts.
- Input: ApprovalRequested and ApprovalDecided events.
- Output: An approval.requested notification to each named approver and a decision notification to the requester; the Inbox entry carries the request status.
- Main flow: ApprovalRequestedNotificationConsumer, ApprovalDecidedNotificationConsumer. Approval rules belong to [work-management.md](work-management.md).
- No-op / rejection / concurrency / retry: Duplicate: none.
- UI states: Inbox entry with approve and reject actions while pending.
- Rules: Product COL-017; decision 78.2.
- Acceptance: Given a request, then the approver sees one entry; after the decision, the requester is told.
- Evidence: BE backend/src/Notrelix.Infrastructure/Messaging/Consumers/Collaboration/ApprovalNotificationConsumers.cs · FE frontend/packages/features/workspace/src/web/inbox/, frontend/apps/web/src/routes/workspaces/$workspaceId/inbox.tsx · Design WM-Approval, Final-Inbox · Tests covered by e2e · Real e2e frontend/e2e/real/approvals.real.e2e.spec.ts
- State: HARDENING — Preference type approval is stored but the consumers do not read it (verify).

### UC-COL-D07 Due-soon notification
- Capability: D, COL-004 Comment activity/events
- Actors and permission: System (daily job); recipients are the item's followers and assignees.
- Preconditions: Item due within 24 hours (decision 78.2).
- Input: The daily due-date job.
- Output: One notification per person per item per day, keyed by a deduplication scope.
- Main flow: The job names its own dedupe scope and calls the notifier.
- No-op / rejection / concurrency / retry: Duplicate: none.
- UI states: Inbox entry.
- Rules: Decision 78.2; product COL-017.
- Acceptance: Given an item due tomorrow, then followers get one entry; a second run adds none.
- Evidence: BE ItemFollowerNotifier (deduplicationScope overload); the job lives in Work Management · FE frontend/packages/features/workspace/src/web/inbox/, frontend/apps/web/src/routes/workspaces/$workspaceId/inbox.tsx · Design Final-Inbox · Tests backend/tests/Notrelix.Integration.Tests/Collaboration/DueDateNotificationsIntegrationTests.cs · Real e2e none
- State: INTEGRATING — No real e2e.

### UC-COL-D08 Nudge an assignee
- Capability: D, COL-004 Comment activity/events
- Actors and permission: A person with view access, never to self (decision 35); Work Management owns the action.
- Preconditions: Item open, assignee other than the sender; at most once per item per sender per 24 hours.
- Input: Item id.
- Output: An Inbox notification to each assignee naming the sender.
- Main flow: Work Management raises the reminder fact; BoardItemOwnersRemindedNotificationConsumer delivers.
- No-op / rejection / concurrency / retry: Within 24 hours: explained, nothing sent.
- UI states: Inbox entry; toast on the sender's side.
- Rules: Decision 35; link [work-management.md](work-management.md).
- Acceptance: Given an assignee, when nudged, then one entry exists; a second press within 24 hours sends none.
- Evidence: BE ItemFollowerConsumers.cs (BoardItemOwnersRemindedNotificationConsumer) · FE WM My work (see Work Management file) · Design WM-Actions-Remind · Tests covered by Work Management tests · Real e2e none
- State: INTEGRATING — No real e2e.

### UC-COL-D09 Read a page's activity (enterprise baseline)
- Capability: D, COL-004 Comment activity/events
- Actors and permission: Anyone with ViewPage.
- Preconditions: None.
- Input: page, pageSize.
- Output: Typed activity for page created, renamed, restored, comment added, resolved.
- Main flow: GET /pages/{id}/activity (new).
- No-op / rejection / concurrency / retry: No access: NotFound.
- UI states: loading, empty, success, permission denied.
- Rules: Product Activity source (section 62); Notion and Confluence show page history of actions; spec DCREQ187 (query must be real or explicitly unsupported).
- Acceptance: Given a page with edits, then the panel lists them newest first.
- Evidence: BE none: the activity route supports board items only · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Needs Documents to publish page facts and an activity reader for kind documents.page; version history stays a Documents feature.

### UC-COL-D10 Publish comment, reaction and attachment facts for other contexts
- Capability: D, COL-004 Comment activity/events
- Actors and permission: System.
- Preconditions: A committed Collaboration change.
- Input: comment-updated, comment-deleted, comment-resolved, comment-reopened, reaction, attachment and watcher domain events.
- Output: Integration events with tenant identity and versioning so Automation, Integrations and Analytics can consume them; today only comment-created and mention-created are mapped.
- Main flow: CommentEventMapper maps domain to integration events through the outbox.
- No-op / rejection / concurrency / retry: Never carry notification templates or automation fields.
- UI states: none.
- Rules: Product Automation, Integrations (sections 102, 103); spec DCREQ055 to DCREQ058; team DCX-03 and DCX-04.
- Acceptance: Given a resolved comment, then a comment-resolved integration event exists once.
- Evidence: BE backend/src/Notrelix.Application/EventMappers/Collaboration/CommentEventMapper.cs · FE none · Design none · Tests backend/tests/Notrelix.Infrastructure.Tests/Messaging/CommentEventMapperTests.cs; backend/tests/Notrelix.Architecture.Tests/Events/DocumentsCollaborationEventPinningArchitectureTests.cs · Real e2e none
- State: IMPLEMENTING — Seven of nine fact kinds have no integration event; add them with schema versions.

### UC-COL-D11 A reply notifies the thread's author (enterprise baseline)
- Capability: D, COL-004 Comment activity/events
- Actors and permission: System; recipient is the author of the parent comment.
- Preconditions: A reply by someone else; recipient not muted.
- Input: comment-created with ParentCommentId.
- Output: One in-app notification of type watching to the parent's author and earlier participants (Lead decision L8).
- Main flow: A new consumer on comment-created reads the parent and participants.
- No-op / rejection / concurrency / retry: Self-reply: none; duplicate: none.
- UI states: Inbox entry "replied to your comment".
- Rules: Product Audience fan-out; Asana, Notion and Linear notify participants of a thread.
- Acceptance: Given Anna's comment and Ben's reply, then Anna has one entry.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Build the consumer and Inbox wording; add the `reply` kind to the Inbox DTO copy.

### UC-COL-D12 A page comment notifies page followers (enterprise baseline)
- Capability: D, COL-004 Comment activity/events
- Actors and permission: System; recipients are page followers and the page owner's mentions.
- Preconditions: UC-COL-B17 exists.
- Input: comment-created on a page.
- Output: One notification per follower with view access at delivery.
- Main flow: Consumer extended from CommentCreatedFollowConsumer, which returns early for non-item targets.
- No-op / rejection / concurrency / retry: As D05.
- UI states: Inbox entry naming the page.
- Rules: Product Notification source (section 55); Lead decision L7.
- Acceptance: Given a page follower, when another member comments, then they have one entry.
- Evidence: BE none: CommentCreatedFollowConsumer exits when the target is not a board item · FE Inbox already names pages (GetWorkspaceInbox page titles) · Design Final-Inbox · Tests none · Real e2e none
- State: DEFINED — Needs page watchers.

### UC-COL-D13 Email a notification (enterprise baseline)
- Capability: D, COL-004 Comment activity/events
- Actors and permission: System; recipient who left email on for the type.
- Preconditions: Preference email on (defaults on for assigned, mention, approval, invitation) and a delivery path exists.
- Input: A created notification.
- Output: One email per notification with its own delivery state; failure never removes the notification.
- Main flow: A Platform delivery consumer reads the recipient row and sends through the provider adapter.
- No-op / rejection / concurrency / retry: Provider failure: retried, dead-lettered; notification stays.
- UI states: Preference page already offers the email column.
- Rules: Product COL-018, Delivery channels (section 57), Preferences (no email path today); spec DCREQ182, DCREQ210.
- Acceptance: Given a mention with email on, then one email is sent; given a retry, still one.
- Evidence: BE none: the email channel is a stored choice no consumer reads; backend/src/Notrelix.Application/Features/Notifications has only link builders · FE account notifications route · Design Final-Account · Tests none · Real e2e none
- State: DEFINED — Needs a Platform or Integrations provider decision; Lead decision L14 keeps it out of the current cut.

### UC-COL-D14 Daily digest (enterprise baseline)
- Capability: D, COL-004 Comment activity/events
- Actors and permission: System; recipient who turned digest on.
- Preconditions: Digest type on, email path (D13).
- Input: Unread, undone notifications of the last day.
- Output: One digest email per person per Workspace per day.
- Main flow: Daily job.
- No-op / rejection / concurrency / retry: Nothing unread: no email.
- UI states: Preference row digest exists.
- Rules: Product Preferences (digest has no producer).
- Acceptance: Given 5 unread items, then one digest lists them.
- Evidence: BE none · FE account notifications route (row only) · Design Final-Account · Tests none · Real e2e none
- State: DEFINED — Depends on UC-COL-D13.

### Capability E: COL-005 Collaboration realtime

### UC-COL-E01 See new comments live on an open item or page
- Capability: E, COL-005 Collaboration realtime
- Actors and permission: Anyone with view access to the open target.
- Preconditions: Realtime connection authorized for the item or page scope.
- Input: Comment facts.
- Output: The list converges without reload; duplicates, out-of-order and gaps are safe because the client refetches.
- Main flow: A change mapper publishes RealtimeResourceChangedV1 for the item or page channel; the client patches or invalidates.
- No-op / rejection / concurrency / retry: After access is lost no more events arrive.
- UI states: live update, offline, reconnect, conflict free.
- Rules: Product COL-024, Realtime (sections 76 to 79); decision 59 (no push in the first cut); spec DCREQ059 to DCREQ065, DCREQ061.
- Acceptance: Given two members on one item, when one comments, then the other sees it within seconds without reload.
- Evidence: BE none: no Collaboration command implements IRealtimeChangeMapper; channel names exist in RealtimeChannelResolver (Item, Page) · FE frontend/packages/foundation/realtime (no comment events) · Design BC-Realtime-1440 (board realtime only) · Tests none · Real e2e none
- State: DEFINED — Add mappers for comment create, edit, delete, resolve and reaction; the client refetches on notify.

### UC-COL-E02 Inbox unread count updates live
- Capability: E, COL-005 Collaboration realtime
- Actors and permission: The recipient.
- Preconditions: Connected on the user's notification channel.
- Input: Notification created, read, done or snoozed.
- Output: The bell badge and the Inbox list refresh.
- Main flow: Publish to user:{id}:notifications after commit.
- No-op / rejection / concurrency / retry: Never delivers another person's entry.
- UI states: badge, list.
- Rules: Product COL-024; spec DCREQ061.
- Acceptance: Given a new mention, then the badge rises without reload.
- Evidence: BE none: RealtimeChannelResolver.UserNotifications is defined and unused · FE InboxBell (polling by query) · Design Final-Inbox · Tests none · Real e2e none
- State: DEFINED — Publisher missing.

### UC-COL-E03 See who else is viewing a page or item (presence)
- Capability: E, COL-005 Collaboration realtime
- Actors and permission: Anyone with view access; shown to others with view access.
- Preconditions: Authorized join of the target's presence channel.
- Input: Join, heartbeat every 20 seconds, leave (Lead decision L16).
- Output: A list of avatars; entries expire after 45 seconds without a heartbeat; nothing is stored as content or authority.
- Main flow: 1. Client joins after the page or item loads. 2. Gateway tracks connection ids per user and target. 3. Changes are broadcast within the Workspace and target scope.
- No-op / rejection / concurrency / retry: No access: join refused. Duplicate connections of one user count once.
- UI states: avatar stack, away, offline, reconnect.
- Rules: Product COL-012, COL-013, Presence scope, status and cleanup; spec DCREQ167 to DCREQ170; decision 19 (auto-snapshot waits for this signal).
- Acceptance: Given two members on a page, then each sees the other; given one closes the tab, then the avatar is gone within 45 seconds; presence never grants access.
- Evidence: BE Domain only: PresenceSession and PresenceUpdatedDomainEvent, table configured; no use case, endpoint or gateway topic · FE foundation realtime has a board.presence event type only; docs and item panels use a local placeholder · Design none for the stack; docs toolbar shows collaborators · Tests backend/tests/Notrelix.Domain.Tests/Collaboration/Presence/PresenceExperimentalTests.cs, PresenceIsolationTests.cs · Real e2e none
- State: IMPLEMENTING — Decide storage (Lead decision L16: gateway memory or Redis, not the PresenceSession table); build the topic, authorization and client.

### UC-COL-E04 Typing and cursor indicators
- Capability: E, COL-005 Collaboration realtime
- Actors and permission: Editors of a page.
- Preconditions: Live co-editing exists.
- Input: Cursor and typing signals.
- Output: Ephemeral indicators, never stored.
- Main flow: Part of presence.
- No-op / rejection / concurrency / retry: Rate limited.
- UI states: cursor labels.
- Rules: Product Cursor/typing (section 42); spec DCREQ209; decision 3 (no CRDT now).
- Acceptance: Not defined until live co-editing is decided.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: BLOCKED — Waits for the live co-editing ADR that decision 3 leaves open (Documents owns it).

### UC-COL-E05 Recover after reconnect, gap or revoked access
- Capability: E, COL-005 Collaboration realtime
- Actors and permission: Any connected client.
- Preconditions: A live subscription.
- Input: Reconnect, missed stream version, access revoked.
- Output: Durable state is refetched by query; presence is rebuilt; events stop after revocation and the client shows a protected state.
- Main flow: Gateway versions per stream; the client compares and refetches.
- No-op / rejection / concurrency / retry: Out-of-order or duplicate event: no visible change.
- UI states: offline banner, reconnecting, permission denied.
- Rules: Product COL-024, sections 78 and 79; spec DCREQ063 to DCREQ065, DCREQ190.
- Acceptance: Given a dropped connection, then after reconnect the comment list equals the server list.
- Evidence: BE backend/src/Notrelix.API/Realtime/ (gateway, RealtimeAuthorization.cs) · FE frontend/packages/foundation/realtime · Design BC-Realtime-1440, BC-Realtime-768-320 · Tests backend/tests/Notrelix.API.Tests (gateway tests, not specific to Collaboration) · Real e2e none
- State: DEFINED — Gateway exists for board streams; no Collaboration stream to recover.

### Capability F: COL-006 Target deletion/retention

### UC-COL-F01 Item deleted or archived: comments follow the item
- Capability: F, COL-006 Target deletion/retention
- Actors and permission: System; Work Management owns the item lifecycle.
- Preconditions: Item moved to trash, archived, restored or purged.
- Input: Item lifecycle facts.
- Output: While the item is in the trash or archived its comments, reactions, attachments and watchers are hidden with it and return on restore; at purge (after 30 days) Collaboration purges them (Lead decision L9). Activity rows and sent notifications stay; Inbox hides entries the person can no longer read.
- Main flow: A Collaboration consumer on item-deleted and item-purged keeps or removes state according to the rule.
- No-op / rejection / concurrency / retry: No comment writes on an unavailable target.
- UI states: NotFound or archived notice; trash restore brings comments back.
- Rules: Product COL-026, Item deletion (87), Target deletion (84); spec DCREQ048, DCREQ049, DCREQ189; decision 50 (comments stay with a moved item).
- Acceptance: Given a deleted item, then its comments are unreachable; after restore they return; after purge they are gone.
- Evidence: BE partial: reads fail with NotFound because the item is not visible; only ItemDeletedActivityConsumer reacts; no purge of comments, reactions, watchers or attachments · FE none · Design Final-States · Tests none · Real e2e none
- State: IMPLEMENTING — No purge consumer; orphan Collaboration rows remain after purge; no test.

### UC-COL-F02 Page removed or block deleted: comments stay as history
- Capability: F, COL-006 Target deletion/retention
- Actors and permission: System; Documents owns pages and blocks.
- Preconditions: Page trashed, restored or deleted; block deleted.
- Input: Documents facts.
- Output: Comments of a trashed page are hidden and return on restore; after page purge they are purged like F01; a comment anchored to a deleted block shows detached with its quote and is never moved to other text.
- Main flow: Reader-side detach is built in the editor; lifecycle consumer to be built.
- No-op / rejection / concurrency / retry: No new comment on a trashed page.
- UI states: detached comment with quote, trash notice.
- Rules: Product Page/Block deletion (86), COL-005, COL-026; spec DCREQ189, DCREQ220; team DCX-01.
- Acceptance: Given a deleted block, then its comment shows detached; given a restored page, then comments return.
- Evidence: BE partial: no consumer for page deletion · FE frontend/packages/product/docs/web/src/editor/comment-anchors.ts (detached) · Design Final-Docs-2 · Tests frontend comment-anchor unit tests · Real e2e none
- State: IMPLEMENTING — Server side lifecycle missing; no real e2e.

### UC-COL-F03 Purge attachment objects after retention
- Capability: F, COL-006 Target deletion/retention
- Actors and permission: System (background job).
- Preconditions: An attachment soft-deleted longer than the retention, or an orphan object past the grace period.
- Input: Retention options.
- Output: The object is removed, then the row is marked purged; orphan objects with no row are deleted; each step is idempotent.
- Main flow: AttachmentObjectReaper runs on a schedule.
- No-op / rejection / concurrency / retry: A failure on one object is logged and retried on the next pass.
- UI states: A purged attachment returns ObjectPurged.
- Rules: Product COL-010, Attachment retention (90), Attachment deletion; spec DCREQ166.
- Acceptance: Given a deleted attachment past retention, then the object is gone and the row says purged.
- Evidence: BE backend/src/Notrelix.Infrastructure/BackgroundJobs/AttachmentObjectReaper.cs, AttachmentObjectReaperService.cs · FE none · Design none · Tests backend/tests/Notrelix.Infrastructure.Tests/Storage/AttachmentObjectReaperTests.cs; backend/tests/Notrelix.Integration.Tests/Collaboration/AttachmentObjectLifecycleIntegrationTests.cs · Real e2e none
- State: INTEGRATING — Retention default and operator doc not checked here.

### UC-COL-F04 A person is anonymized or leaves: history stays
- Capability: F, COL-006 Target deletion/retention
- Actors and permission: System, on an Identity event.
- Preconditions: Identity deletes or anonymizes a user; or a member leaves a Workspace.
- Input: Identity lifecycle fact.
- Output: Comments, reactions and mentions stay with a stable tombstoned author; a member who only left keeps their name on history.
- Main flow: To be defined with Identity.
- No-op / rejection / concurrency / retry: Not applicable.
- UI states: "Former member" or "Deleted user" in place of a name.
- Rules: Product COL-027, Identity deletion (91); team COL-006 historical attribution; spec DCREQ050.
- Acceptance: Given an erased user, then their comments remain readable with a placeholder author and no profile data.
- Evidence: BE partial: GetCommentsQueryHandler shows "Unknown" when the actor cannot be resolved; no lifecycle rule · FE comment surface shows the name returned · Design none · Tests none · Real e2e none
- State: BLOCKED — Needs Identity's deletion and anonymization contract ([identity.md](identity.md)); privacy policy is not decided.

### UC-COL-F05 Workspace deleted or archived: retention, export and purge
- Capability: F, COL-006 Target deletion/retention
- Actors and permission: System; Workspaces owns the workflow.
- Preconditions: Workspace deletion or archive.
- Input: Workspace lifecycle fact.
- Output: Explicit retention, export and purge of all Collaboration data of the Workspace.
- Main flow: To be defined with Workspaces.
- No-op / rejection / concurrency / retry: Not applicable.
- UI states: none.
- Rules: Product Workspace deletion (93); spec DCREQ213.
- Acceptance: Not defined.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: BLOCKED — Destructive data policy; needs the Workspaces deletion workflow ([workspaces.md](workspaces.md)).

### UC-COL-F06 Notification retention
- Capability: F, COL-006 Target deletion/retention
- Actors and permission: System (daily job).
- Preconditions: Read or done notifications older than 180 days, any notification older than 365 days (Lead decision L10).
- Input: Retention options.
- Output: Recipient rows and items are removed; Activity and Audit are untouched.
- Main flow: Daily job in batches.
- No-op / rejection / concurrency / retry: None.
- UI states: Older entries vanish.
- Rules: Product Activity retention (65), COL-028; spec DCREQ189.
- Acceptance: Given a notification read 181 days ago, then it no longer lists.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Job missing; the table grows without bound.

### UC-COL-F07 Restore a deleted comment
- Capability: F, COL-006 Target deletion/retention
- Actors and permission: A moderator or the author within a window (undecided).
- Preconditions: Comment soft-deleted.
- Input: Comment id.
- Output: Comment returns; comment-restored event.
- Main flow: Comment.Restore exists in Domain; no use case.
- No-op / rejection / concurrency / retry: Purged: refused.
- UI states: Undo toast after delete (proposed).
- Rules: Product Comment delete; Domain Restore.
- Acceptance: Given an undo within 10 seconds, then the comment returns.
- Evidence: BE Domain only: Comment.Restore, CommentRestoredDomainEvent · FE none · Design none · Tests backend/tests/Notrelix.Domain.Tests/Collaboration/Comments/CommentTests.cs · Real e2e none
- State: BLOCKED — Depends on UC-COL-C02 (who may restore).

### Capability G: COL-007 Collaboration query/state

### UC-COL-G01 List the comments of an item or page
- Capability: G, COL-007 Collaboration query/state
- Actors and permission: ViewBoard on the item or view on the page.
- Preconditions: None.
- Input: Target id. Proposed: cursor and limit 50 threads (Lead decision L4).
- Output: Comments oldest first with author, edited mark, resolved time, reactions with who, and the anchor; deleted comments are left out.
- Main flow: 1. Panel mounts. 2. GET .../comments. 3. Handler loads all live comments, reactions and actor names.
- No-op / rejection / concurrency / retry: No access: NotFound.
- UI states: loading, empty, success, permission denied, load more.
- Rules: Product Comment query; spec DCREQ051 to DCREQ053; team COL-007.
- Acceptance: Given 3 comments, then they list oldest first. Given 5,000 comments, then the first page is at most 50 threads.
- Evidence: BE GET /api/v1/board-items/{boardItemId}/comments and GET /api/v1/pages/{pageId}/comments (Collaboration.Comments.GetBoardItemComments, GetPageComments), GetCommentsQueryHandler · FE frontend/packages/features/collaboration/src/web/query/hooks/use-comments.ts; WM use-card-comments.ts; docs use-page-comments.ts · Design WM-Comments, Final-Docs-2 · Tests backend/tests/Notrelix.Application.Tests/Features/Collaboration/GetCommentsResolvedTests.cs; backend/tests/Notrelix.Application.Tests/Features/WorkManagement/Boards/GetFullBoardCommentCountTests.cs · Real e2e frontend/e2e/real/inbox-page-reply.real.e2e.spec.ts (reads the page comments)
- State: INTEGRATING — Unbounded read; page comments read under the ViewBoard action name instead of ViewPage (confirm the engine maps it).

### UC-COL-G02 Comment counts on cards and lists
- Capability: G, COL-007 Collaboration query/state
- Actors and permission: Anyone who can see the card.
- Preconditions: None.
- Input: Board read.
- Output: A comment count per item added to the board items projection.
- Main flow: ICollaborationResourceSummary answers counts to Work Management through a published port.
- No-op / rejection / concurrency / retry: No access: item not returned.
- UI states: badge on the card (decision 39).
- Rules: Decision 39; team DCX-02.
- Acceptance: Given an item with two comments, then the card shows 2.
- Evidence: BE backend/src/Notrelix.Application/Features/Collaboration/Public/ResourceSummary/, Infrastructure/CrossContext/Collaboration/ResourceSummary/PostgresCollaborationResourceSummary.cs · FE kanban-card.tsx · Design Kanban-Card-States · Tests backend/tests/Notrelix.Application.Tests/Features/WorkManagement/Boards/GetFullBoardCommentCountTests.cs · Real e2e none
- State: INTEGRATING — Counting rule for deleted and resolved comments undocumented.

### UC-COL-G03 Open the Inbox list
- Capability: G, COL-007 Collaboration query/state
- Actors and permission: Any active member; only their own notifications.
- Preconditions: None.
- Input: unreadOnly, cursor, limit (1 to 50, default 20), state inbox, done or snoozed.
- Output: Newest first, one page at a time; each entry has type, actor, excerpt, target (item or page title when readable), unread, needsReply, muted, snoozedUntil and approval status. Entries about targets the caller can no longer read are not shown. A snoozed entry whose time passed is back, unread.
- Main flow: 1. Inbox route loads. 2. GET /workspaces/{id}/inbox. 3. Reader scans up to 100 rows, filters by readability, returns a page and a cursor.
- No-op / rejection / concurrency / retry: Bad cursor or state: 400. Not a member: NotFound.
- UI states: loading, empty, success, error, permission denied, done and snoozed filters.
- Rules: Product COL-017, COL-023, Notification privacy (74); decision 34; spec DCREQ180, DCREQ183.
- Acceptance: Given 25 notifications, then the first page has 20 and a cursor; given a revoked board, then its entries do not show.
- Evidence: BE GET /api/v1/workspaces/{workspaceId}/inbox (Collaboration.Notifications.GetWorkspaceInbox), GetWorkspaceInboxQueryHandler (backend/src/Notrelix.Application/Features/WorkManagement/BoardItems/Queries/GetWorkspaceInbox/) · FE frontend/packages/features/workspace/src/web/inbox/, frontend/apps/web/src/routes/workspaces/$workspaceId/inbox.tsx · Design Final-Inbox, Final-M-Inbox · Tests backend/tests/Notrelix.Application.Tests/Features/WorkManagement/BoardItems/WorkspaceInboxTests.cs; backend/tests/Notrelix.Integration.Tests/Collaboration/InboxStatesIntegrationTests.cs; frontend/packages/features/workspace/src/web/inbox/__tests__/workspace-inbox.interaction.component.test.tsx · Real e2e frontend/e2e/real/inbox.real.e2e.spec.ts, inbox-states.real.e2e.spec.ts
- State: HARDENING — The handler lives in the Work Management feature although Collaboration owns the Inbox; no design-fidelity spec for Inbox.

### UC-COL-G04 Unread count per workspace (bell)
- Capability: G, COL-007 Collaboration query/state
- Actors and permission: Any signed-in person.
- Preconditions: None.
- Input: None.
- Output: Unread counts per Workspace the caller is an active member of, and the total; empty Workspaces omitted.
- Main flow: GET /inbox/summary.
- No-op / rejection / concurrency / retry: Only the caller's rows are counted.
- UI states: badge with 99+ cap.
- Rules: Product COL-014, COL-015 (count derived from recipient rows).
- Acceptance: Given 3 unread in one Workspace, then the summary says 3.
- Evidence: BE GET /api/v1/inbox/summary (Collaboration.Notifications.GetInboxSummary), GetInboxSummaryQueryHandler, PostgresNotificationInbox · FE frontend/packages/features/notifications/src/web/components/inbox-bell.tsx; apps/web/src/shell/workspace/workspace-shell.tsx · Design Final-Inbox, Final-M-Inbox · Tests backend/tests/Notrelix.Application.Tests/Features/Collaboration/GetInboxSummaryTests.cs; frontend inbox-bell.component.test.tsx · Real e2e none
- State: INTEGRATING — No real e2e; not live (UC-COL-E02).

### UC-COL-G05 Mark one notification read
- Capability: G, COL-007 Collaboration query/state
- Actors and permission: The recipient.
- Preconditions: None.
- Input: Notification id.
- Output: ReadAt set; a notification that is not theirs or already read changes nothing and still answers success.
- Main flow: POST /workspaces/{id}/inbox/{notificationId}/read.
- No-op / rejection / concurrency / retry: None.
- UI states: success, optimistic.
- Rules: Product Notification read/dismiss (59), COL-014; mutates Collaboration state only; spec DCREQ212.
- Acceptance: Given an unread entry, when opened, then it shows read and the badge drops.
- Evidence: BE POST /api/v1/workspaces/{workspaceId}/inbox/{notificationId}/read (Collaboration.Notifications.MarkInboxEntryRead), backend/src/Notrelix.Application/Features/Collaboration/Notifications/Commands/MarkInboxEntryRead/ · FE frontend/packages/features/workspace/src/web/inbox/, frontend/apps/web/src/routes/workspaces/$workspaceId/inbox.tsx · Design Final-Inbox · Tests backend/tests/Notrelix.Integration.Tests/Collaboration/InboxStatesIntegrationTests.cs · Real e2e frontend/e2e/real/inbox.real.e2e.spec.ts
- State: HARDENING — No recorded owner check or design diff.

### UC-COL-G06 Mark every notification read
- Capability: G, COL-007 Collaboration query/state
- Actors and permission: The recipient.
- Preconditions: None.
- Input: Workspace id.
- Output: All the caller's unread entries in the Workspace become read; answers the count; zero is a no-op.
- Main flow: POST /workspaces/{id}/inbox/read-all.
- No-op / rejection / concurrency / retry: None.
- UI states: success with count, pending.
- Rules: As G05.
- Acceptance: Given 7 unread, then 7 are marked and the badge is 0.
- Evidence: BE POST /api/v1/workspaces/{workspaceId}/inbox/read-all (Collaboration.Notifications.MarkWorkspaceInboxRead), backend/src/Notrelix.Application/Features/Collaboration/Notifications/Commands/MarkWorkspaceInboxRead/ · FE frontend/packages/features/workspace/src/web/inbox/, frontend/apps/web/src/routes/workspaces/$workspaceId/inbox.tsx · Design Final-Inbox · Tests backend/tests/Notrelix.Integration.Tests/Collaboration/InboxStatesIntegrationTests.cs · Real e2e none (mock: work-feed handler only)
- State: INTEGRATING — No real e2e; one statement over all rows (fine until very large).

### UC-COL-G07 Mark done and take back into the Inbox
- Capability: G, COL-007 Collaboration query/state
- Actors and permission: The recipient.
- Preconditions: None.
- Input: Notification id.
- Output: Done: read, leaves the default list, appears under Done, no longer needs a reply. Reopen: clears done and snooze. Repeats are no-ops.
- Main flow: POST .../done and POST .../reopen.
- No-op / rejection / concurrency / retry: Not theirs: success, no change.
- UI states: success, toast with undo, Done filter.
- Rules: Product Notification read/dismiss, Delivery channels separate from read state (57); decision 34.
- Acceptance: Given a mention, when done, then it shows under Done; when reopened, it returns.
- Evidence: BE POST .../inbox/{notificationId}/done and /reopen (Collaboration.Notifications.MarkInboxEntryDone, ReopenInboxEntry) · FE frontend/packages/features/workspace/src/web/inbox/, frontend/apps/web/src/routes/workspaces/$workspaceId/inbox.tsx · Design Final-Inbox · Tests backend/tests/Notrelix.Integration.Tests/Collaboration/InboxStatesIntegrationTests.cs · Real e2e frontend/e2e/real/inbox-states.real.e2e.spec.ts
- State: HARDENING — No recorded owner check or design diff.

### UC-COL-G08 Snooze a notification
- Capability: G, COL-007 Collaboration query/state
- Actors and permission: The recipient.
- Preconditions: None.
- Input: Until: a future time within 365 days.
- Output: The entry leaves the default list and returns unread when the time passes; evaluated at read time, no scheduler. New activity does not cancel the snooze (Lead decision L12).
- Main flow: POST /workspaces/{id}/inbox/{notificationId}/snooze.
- No-op / rejection / concurrency / retry: Past time or beyond a year: 400.
- UI states: presets (1 hour, tomorrow, next week, pick a date), snoozed filter, success.
- Rules: Product Notification read/dismiss; Linear returns a snoozed entry unread.
- Acceptance: Given a snooze to tomorrow, then it is hidden today and shows unread tomorrow.
- Evidence: BE POST /api/v1/workspaces/{workspaceId}/inbox/{notificationId}/snooze (Collaboration.Notifications.SnoozeInboxEntry), SnoozeInboxEntryCommandHandler · FE frontend/packages/features/workspace/src/web/inbox/, frontend/apps/web/src/routes/workspaces/$workspaceId/inbox.tsx; apps/web/src/shell/workspace/inbox-view.ts (snoozeUntil) · Design Final-Inbox · Tests backend/tests/Notrelix.Integration.Tests/Collaboration/InboxStatesIntegrationTests.cs; frontend inbox-view.unit.test.ts · Real e2e frontend/e2e/real/inbox-states.real.e2e.spec.ts
- State: HARDENING — No recorded owner check or design diff.

### UC-COL-G09 Mute and unmute a thread
- Capability: G, COL-007 Collaboration query/state
- Actors and permission: The recipient.
- Preconditions: The notification has a thread (an item or page).
- Input: Notification id.
- Output: No new notification from that item or page reaches the person; earlier ones stay; repeats are no-ops.
- Main flow: POST .../mute and .../unmute; consumers check NotificationThreadMutes.
- No-op / rejection / concurrency / retry: No thread: success, no change.
- UI states: success, muted mark.
- Rules: Product Preferences (type by channel), Notification read/dismiss.
- Acceptance: Given a muted item, then a later comment adds no entry.
- Evidence: BE POST .../inbox/{notificationId}/mute and /unmute (Collaboration.Notifications.MuteInboxThread, UnmuteInboxThread) · FE frontend/packages/features/workspace/src/web/inbox/, frontend/apps/web/src/routes/workspaces/$workspaceId/inbox.tsx · Design Final-Inbox · Tests backend/tests/Notrelix.Integration.Tests/Collaboration/InboxStatesIntegrationTests.cs · Real e2e frontend/e2e/real/inbox-states.real.e2e.spec.ts
- State: HARDENING — Follower and approval consumers' honouring of mute not proven by test.

### UC-COL-G10 Reply to a mention from the Inbox
- Capability: G, COL-007 Collaboration query/state
- Actors and permission: The recipient with CreateComment on the target.
- Preconditions: A mention entry on an item or page.
- Input: Reply text.
- Output: A comment on the same target through UC-COL-A01 or A02, plus Inbox read; no separate reply endpoint.
- Main flow: Reading pane composer posts the comment then marks the entry read.
- No-op / rejection / concurrency / retry: As A01 and A02.
- UI states: composer in reading pane, success, permission denied.
- Rules: Decision 34; product COL-017.
- Acceptance: Given a mention, when the person replies, then a comment exists on the item and the entry is read and no longer needs a reply.
- Evidence: BE A01 and A02 routes; MarkMentionsRepliedAsync in ItemFollowerNotifier · FE frontend/packages/features/workspace/src/web/inbox/, frontend/apps/web/src/routes/workspaces/$workspaceId/inbox.tsx · Design Final-Inbox · Tests see A01 · Real e2e frontend/e2e/real/inbox-page-reply.real.e2e.spec.ts
- State: HARDENING — No recorded owner check or design diff.

### UC-COL-G11 Read my notification preferences
- Capability: G, COL-007 Collaboration query/state
- Actors and permission: An active member, only for themself in that Workspace.
- Preconditions: Active membership.
- Input: Workspace id.
- Output: Seven types by two channels; untouched cells show defaults; Version 0 until the first save.
- Main flow: GET /workspaces/{id}/notification-preferences.
- No-op / rejection / concurrency / retry: Not a member: NotFound.
- UI states: loading, success, error.
- Rules: Product Preferences (D15); spec DCREQ211.
- Acceptance: Given a new member, then in-app is on for all seven and email on for assigned, mention, approval and invitation.
- Evidence: BE GET /api/v1/workspaces/{workspaceId}/notification-preferences (Collaboration.Preferences.GetNotificationPreferences) · FE apps/web/src/routes/workspaces/$workspaceId/account/notifications.tsx; frontend/packages/features/notifications/src/web/query/hooks/use-notification-preferences.ts · Design Final-Account · Tests backend/tests/Notrelix.Application.Tests/Features/Collaboration/NotificationPreferencesHandlerTests.cs; backend/tests/Notrelix.API.Tests/Contracts/NotificationPreferencesOpenApiShapeTests.cs; backend/tests/Notrelix.Domain.Tests/Collaboration/Preferences/ · Real e2e none
- State: INTEGRATING — No real e2e.

### UC-COL-G12 Save my notification preferences
- Capability: G, COL-007 Collaboration query/state
- Actors and permission: The person, only their own matrix.
- Preconditions: ExpectedVersion from the last read.
- Input: ExpectedVersion and rows [{type, inApp, email}] naming only the cells to change.
- Output: Partial, all-or-nothing change; full matrix answered; values that already hold are a no-op that keeps the version.
- Main flow: PATCH /workspaces/{id}/notification-preferences.
- No-op / rejection / concurrency / retry: Stale version: 412 and a refetch; unknown type, unknown channel or no change: 400 or 422.
- UI states: saving, saved, error, conflict (refetched).
- Rules: Product Preferences (D15); spec DCREQ211.
- Acceptance: Given the mention in-app switched off, then a later mention creates no entry for that person (UC-COL-D04).
- Evidence: BE PATCH /api/v1/workspaces/{workspaceId}/notification-preferences (Collaboration.Preferences.UpdateNotificationPreferences), NotificationPreferenceSet · FE account/notifications.tsx, createUseNotificationPreferences · Design Final-Account · Tests backend/tests/Notrelix.Integration.Tests/Collaboration/NotificationPreferencesIntegrationTests.cs; backend/tests/Notrelix.Domain.Tests/Collaboration/Preferences/NotificationPreferenceSetTests.cs · Real e2e none
- State: INTEGRATING — Only the mention producer reads the matrix; follower, approval, assigned, due consumers must read it; no real e2e.

### UC-COL-G13 Quiet hours and pause (enterprise baseline)
- Capability: G, COL-007 Collaboration query/state
- Actors and permission: The person.
- Preconditions: None.
- Input: Pause until a time, or daily quiet hours with a time zone.
- Output: Email and push are held; in-app entries still arrive; mandatory security mail is never held.
- Main flow: Extension of the preference set.
- No-op / rejection / concurrency / retry: Invalid range: 400.
- UI states: pause toggle and hours editor.
- Rules: Product Preferences (later decision), COL-018 last line; Asana pauses notifications by hours and days.
- Acceptance: Given quiet hours, then no email is sent in the window.
- Evidence: BE none · FE none (the page says mobile and quiet hours are not offered) · Design none · Tests none · Real e2e none
- State: DEFINED — Meaningful only after UC-COL-D13.

### UC-COL-G14 Unread comment markers (read state)
- Capability: G, COL-007 Collaboration query/state
- Actors and permission: Any viewer.
- Preconditions: None.
- Input: Mark read up to a comment id.
- Output: ResourceReadState keyed by user and resource with a monotonic last-read boundary; unread count is derived (comments after the boundary, not own, not deleted).
- Main flow: PUT /board-items/{id}/read-state and /pages/{id}/read-state (proposed).
- No-op / rejection / concurrency / retry: An older marker never moves the boundary back.
- UI states: New comments divider, unread dot on cards.
- Rules: Product COL-014, COL-015, Mark read (47); spec DCREQ171 to DCREQ174; Lead decision L17.
- Acceptance: Given 3 comments after the boundary, then the marker says 3; given an older mark, then it stays 3.
- Evidence: BE Domain only: ResourceReadState (monotonic); no use case or endpoint · FE none · Design none · Tests backend/tests/Notrelix.Domain.Tests/Collaboration/ReadStates/ReadStateMonotonicityTests.cs · Real e2e none
- State: IMPLEMENTING — No Application or API; decide before building (Lead decision L17).

### UC-COL-G15 Switch workspace or account without leaking collaboration state
- Capability: G, COL-007 Collaboration query/state
- Actors and permission: Any signed-in person.
- Preconditions: None.
- Input: Workspace or account change.
- Output: Query keys carry workspace and target so no comment, Inbox or preference list of the old scope shows in the new one; late responses are dropped.
- Main flow: collaborationKeys include workspace and target; the Inbox keys include workspace.
- No-op / rejection / concurrency / retry: None.
- UI states: loading after switch.
- Rules: Product COL-025; spec DCREQ219; team COL-007.
- Acceptance: Given a switch, then the comment panel never shows the previous target's comments.
- Evidence: BE none · FE frontend/packages/features/collaboration/src/core/query/keys.ts; frontend/packages/features/notifications/src/query/keys.ts · Design none · Tests frontend/packages/features/notifications/src/__tests__/notifications-query-keys.unit.test.ts; collaboration.unit.test.ts · Real e2e none
- State: INTEGRATING — No real e2e of a workspace switch with comments open.

### UC-COL-G16 Search comments (enterprise baseline)
- Capability: G, COL-007 Collaboration query/state
- Actors and permission: Anyone, limited to targets they can view.
- Preconditions: None.
- Input: Query text.
- Output: Comments whose target the caller can view, derived from the comment table at query time.
- Main flow: A Search context read that filters by visible boards and pages (Lead decision L20).
- No-op / rejection / concurrency / retry: None.
- UI states: command palette group.
- Rules: Product Search (100); spec DCREQ217.
- Acceptance: Given a hidden board, then its comments never appear.
- Evidence: BE none · FE none · Design Final-Command · Tests none · Real e2e none
- State: DEFINED — Owned together with Search; not built.

### Capability H: COL-008 Collaboration hardening

### UC-COL-H01 Duplicate comment submit has one effect
- Capability: H, COL-008 Collaboration hardening
- Actors and permission: Any commenter.
- Preconditions: A client-generated Idempotency-Key per submit (Lead decision L5).
- Input: Idempotency-Key header.
- Output: The same key returns the first comment id; no second comment, mention or notification.
- Main flow: WithIdempotencyKey on the comment create routes and IIdempotentRequest on the command, as follow and upload already do.
- No-op / rejection / concurrency / retry: A key reused with a different body: 422.
- UI states: pending state disables Send; offline retry is safe.
- Rules: Product COL-008, COL-025; team COL-008; spec DCREQ159, DCREQ219.
- Acceptance: Given a double click, then one comment exists.
- Evidence: BE none: CreateCommentEndpoint has no WithIdempotencyKey; the command is not IIdempotentRequest · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Backend and client both missing.

### UC-COL-H02 Competing edits carry an expected version
- Capability: H, COL-008 Collaboration hardening
- Actors and permission: Author, resolver.
- Preconditions: The client sends the version it read.
- Input: ExpectedVersion on edit, delete, resolve and reopen.
- Output: A stale write changes nothing and answers 412 with the current comment.
- Main flow: CommentDto gains Version; commands check it.
- No-op / rejection / concurrency / retry: 412.
- UI states: conflict state with the current text.
- Rules: Product Comment edit; RULE concurrency (NRX); team COL-008.
- Acceptance: Given two edits from version 3, then the second gets 412.
- Evidence: BE none: UpdateCommentCommand has no ExpectedVersion; Comment has a version that is incremented only · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Add version to the DTO, commands and UI conflict state.

### UC-COL-H03 Concurrent reactions and follows do not duplicate
- Capability: H, COL-008 Collaboration hardening
- Actors and permission: Any person.
- Preconditions: None.
- Input: Two simultaneous identical requests.
- Output: One row. A unique index on (target, user, emoji) for reactions and (target, user) for watchers settles the race.
- Main flow: Migration plus insert that treats a unique violation as success.
- No-op / rejection / concurrency / retry: None.
- UI states: none.
- Rules: Product COL-009, Reaction retry, COL-016; spec DCREQ159, DCREQ177.
- Acceptance: Given two parallel PUT requests, then the count is 1.
- Evidence: BE partial: handlers check with AnyAsync before insert; ReactionConfiguration and ResourceWatcherConfiguration have non-unique indexes only · FE none · Design none · Tests backend/tests/Notrelix.Domain.Tests/Collaboration/Reactions/ReactionDuplicateTests.cs (Domain only) · Real e2e none
- State: IMPLEMENTING — Migration and concurrency test (RLS-safe change rule applies).

### UC-COL-H04 Comment content is safe to render
- Capability: H, COL-008 Collaboration hardening
- Actors and permission: Any reader.
- Preconditions: None.
- Input: ContentMd.
- Output: Content is Markdown text; raw HTML is never rendered; links get rel noopener and only http, https and mailto schemes; mentions render from ids.
- Main flow: Server trims and limits length; clients render through one safe renderer (Lead decision L22).
- No-op / rejection / concurrency / retry: Over 10,000 characters: 400.
- UI states: rendered text, link chips.
- Rules: Product COL-029, Content sanitization (98).
- Acceptance: Given `<script>` in a comment, then it shows as text.
- Evidence: BE Domain trims and limits to 10,000; the validator allows 50,000 (mismatch) · FE resource-comments.tsx renderer · Design none · Tests none for XSS strings · Real e2e none
- State: IMPLEMENTING — Align validator to 10,000; add XSS and scheme tests on the renderer.

### UC-COL-H05 Rate limits and abuse controls
- Capability: H, COL-008 Collaboration hardening
- Actors and permission: Any caller.
- Preconditions: None.
- Input: Bursts of comments, reactions or uploads.
- Output: 429 with retry-after beyond 30 comments, 60 reactions and 20 uploads per person per minute per Workspace (Lead decision L15).
- Main flow: Rate limit policies on the Collaboration routes.
- No-op / rejection / concurrency / retry: 429.
- UI states: toast with wait time.
- Rules: Product Abuse controls (97); spec DCREQ216.
- Acceptance: Given 31 comments in a minute, then the 31st gets 429.
- Evidence: BE none: the API has a rate limit framework (backend/src/Notrelix.API/RateLimiting/) but no Collaboration route uses it · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Policies not attached.

### UC-COL-H06 A mention retry notifies once
- Capability: H, COL-008 Collaboration hardening
- Actors and permission: System.
- Preconditions: Redelivery of mention-created.
- Input: Same event twice.
- Output: One notification per (mention, person) by deduplication key.
- Main flow: MentionCreatedNotificationConsumer reads the key before insert.
- No-op / rejection / concurrency / retry: Second delivery ignored.
- UI states: none.
- Rules: Product COL-008, COL-018.
- Acceptance: Given two deliveries, then one entry.
- Evidence: BE MentionCreatedNotificationConsumer (dedupe by DeduplicationKey); unique index not confirmed · FE none · Design none · Tests none direct · Real e2e none
- State: INTEGRATING — Read-then-insert without a confirmed unique index on DeduplicationKey; add a test with concurrent delivery.

### UC-COL-H07 Provider comment sync maps through Collaboration operations
- Capability: H, COL-008 Collaboration hardening
- Actors and permission: An integration (provider adapter).
- Preconditions: Approved provider mapping.
- Input: External comment and identity.
- Output: A Collaboration comment created through the normal command with an integration actor.
- Main flow: Owned by Integrations.
- No-op / rejection / concurrency / retry: Unmapped identity: refused.
- UI states: none.
- Rules: Product Integrations (103), Activity actor (64); spec DCREQ218.
- Acceptance: Not defined.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: BLOCKED — Needs the integration execution principal ([automation-integrations.md](automation-integrations.md)); a security-principal decision.

### UC-COL-H08 Guest and public comments
- Capability: H, COL-008 Collaboration hardening
- Actors and permission: Anonymous or public-link visitors.
- Preconditions: None.
- Input: Comment through a share link.
- Output: Not offered.
- Main flow: None.
- No-op / rejection / concurrency / retry: Refused.
- UI states: none.
- Rules: Product Abuse controls (97); owner-only question: anonymous public execution principal.
- Acceptance: Not defined.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: BLOCKED — Anonymous principal and abuse controls are an owner decision.

### Endpoints with no use case

None. Every route under `backend/src/Notrelix.API/Endpoints/Collaboration/` (35 routes) appears in an Evidence line above. `backend/src/Notrelix.Application/Features/Notifications/` holds only the email-verification and workspace-invitation link builders; they belong to Identity and Workspaces, not to this context. The workspace activity route `/api/v1/workspaces/{workspaceId}/activity` is owned by Workspaces ([workspaces.md](workspaces.md)).

Design ids are file names of artboards on the owner's canvas (`18L4jUXGfdXh7Wn9YQvA2x`); where no Collaboration artboard exists the line says none.

## 4. Product decisions applied

- Product decision (owner, 2026-10-12): COL-030, a mention of someone who cannot view the source notifies nobody and grants nothing (collaboration.md already records it).
- Product decision (owner, 2026-10-23): D15, the notification preference matrix is Collaboration-owned, with seven types and two channels and the defaults in collaboration.md (already recorded there).
- Product decision (owner, 2026-10-26): item 4 and item 10, page images and files are Collaboration attachments with target `documents.page`; upload and delete need edit on the page; list and content need view; no base64 and no public URL.
- Product decision (owner, 2026-10-26): item 12, a mention inside a page block notifies the person once when newly added in a saved edit, only if they can view the page, never the writer; removing it retracts nothing.
- Product decision (owner, 2026-10-26): item 19, the last-editor-leaves snapshot waits for a presence signal.
- Product decision (owner, 2026-10-26): items 20 and 24, comment delete is offered only to the author in the docs panel while the backend still authorizes; resolved threads hide behind Show resolved; reopen is a real command with the same permission as resolve (resolver, comment author or page editor), no expected version, no-op on an active thread.
- Product decision (owner, 2026-10-26): item 34, replying to a mention is a comment on its thread plus Inbox read; the phone bell shows the workspace Inbox unread count and opens the Inbox.
- Product decision (owner, 2026-10-26): item 35, nudge sends each assignee an Inbox notification, at most once per item per sender per 24 hours, never to self.
- Product decision (owner, 2026-10-26): item 39, the card shows the comment count.
- Product decision (owner, 2026-10-26): item 42, item-comment @ suggestions list only people who can view the board; a mention of someone who cannot is kept as text and sends nothing.
- Product decision (owner, 2026-10-26): item 50, comments and attachments move with an item moved to another board.
- Product decision (owner, 2026-10-27): item 56, item history is Collaboration-owned, append-only, one row per source event id, read by GET /board-items/{id}/activity with ViewBoard, page size capped at 100; sensitive values are never copied; comments stay in the Updates tab.
- Product decision (owner, 2026-10-28): items 59 and 60, reactions use a fixed palette of six emoji, one per emoji per person per comment, toggled by click, commenter and up, no realtime push in the first cut; replies are one level deep.
- Product decision (owner, 2026-10-30): item 78.1 and 78.2, being assigned makes the person a watcher; item notifications with a producer are mention, assigned to me, comment on a watched item, status change on a watched item, due within 24 hours, approval requested or decided, nudge and new form submission.
- Lead decision (2026-10-30) L1: only the author edits or deletes a comment; resolve and reopen need the thread starter, a previous resolver or edit right on the target; deleting another person's comment (moderation) is BLOCKED on Governance (UC-COL-C02).
- Lead decision (2026-10-30) L2: comment edit, delete, resolve and reopen carry the expected version (412 on stale); edit accepts MentionedUserIds, and only newly added people are notified.
- Lead decision (2026-10-30) L3: deleting a top-level comment that has replies keeps the thread under the placeholder "This comment was deleted"; without replies it disappears; the deleted text is never returned.
- Lead decision (2026-10-30) L4: comment lists page by cursor, 50 top-level threads per page with their replies.
- Lead decision (2026-10-30) L5: comment create takes an Idempotency-Key header like follow and upload.
- Lead decision (2026-10-30) L6: page comments get the same reactions and palette as item comments.
- Lead decision (2026-10-30) L7: a person may follow a page; commenting on or being mentioned in a page follows it; page comment mentions need view access to the page; page followers are notified of new comments.
- Lead decision (2026-10-30) L8: a reply notifies the parent comment's author and earlier participants with type watching, never the replier.
- Lead decision (2026-10-30) L9: a trashed or archived item or page hides its comments, reactions, attachments and watchers and brings them back on restore; at purge Collaboration purges them; Activity rows and sent notifications stay.
- Lead decision (2026-10-30) L10: read or done notifications are deleted after 180 days, any notification after 365 days.
- Lead decision (2026-10-30) L12: a snooze lasts up to 365 days and new activity does not cancel it; presets are one hour, tomorrow, next week and a date.
- Lead decision (2026-10-30) L13: quiet hours and pause hold email and push only; in-app entries still arrive. Built after email exists.
- Lead decision (2026-10-30) L14: email and digest delivery stay out of the current cut until Platform or Integrations names a provider path.
- Lead decision (2026-10-30) L15: rate limits per person per Workspace per minute: 30 comments, 60 reactions, 20 uploads.
- Lead decision (2026-10-30) L16: presence lives in the realtime gateway (memory or Redis), heartbeat 20 seconds, expiry 45 seconds; the PresenceSession table is not used for live presence.
- Lead decision (2026-10-30) L17: read state is a per-user, per-target monotonic boundary on the last read comment; the unread count is derived (comments after it, not own, not deleted).
- Lead decision (2026-10-30) L18: a Team mention fans out at delivery time to members who can view the source.
- Lead decision (2026-10-30) L19: mentions of a Page or a Board are not offered; only User and Team.
- Lead decision (2026-10-30) L20: comment search is derived at query time and filtered by the caller's visible boards and pages.
- Lead decision (2026-10-30) L21: the comment length limit is 10,000 characters after trim, in Domain and validator alike.
- Lead decision (2026-10-30) L22: comment content is Markdown text; raw HTML is never rendered; links allow http, https and mailto and open with noopener.
- Lead decision (2026-10-30) L23: attachments stay attached to the item or page, not to a single comment; no malware scan in this cut; link attachments accept only http and https.
- Lead decision (2026-10-30) L24: archive and dismiss of a notification are the same as Done; the stored ArchivedAt and DismissedAt columns stay unused.

## 5. Canonical updates required

Text that [collaboration.md](../../product/collaboration.md) must absorb (the owner decisions 20, 24, 34, 56, 59, 60 and 78 are not yet in it):

- Comment authorization: the author edits and deletes own comments; resolve and reopen need the thread starter, a previous resolver or edit right on the target; moderation delete needs a Governance permission still to be named. Edit, delete, resolve and reopen carry the expected version (L1, L2).
- Comment delete: a top-level comment with replies stays as a placeholder; without replies it disappears (L3). This closes COL-006 for threads.
- Comment query: cursor pages of 50 threads (L4). Create takes an Idempotency-Key (L5). Length 10,000 (L21). Content is safe Markdown (L22).
- Reactions: fixed palette of six, one per emoji per person per comment, items and pages (decisions 59 and L6); uniqueness is enforced by a unique index.
- Watchers: assigned and creator become watchers (decision 78.1); a person may follow a page, commenting on a page follows it (L7).
- Notifications: producers are mention, assigned, comment on watched item, status change on watched item, due within 24 hours, approval, nudge, page comment to followers, reply to my comment (decisions 78.2, L7, L8). Mention delivery re-checks view access (COL-030). Email, digest and quiet hours wait (L13, L14).
- Inbox: states inbox, done and snoozed; snooze up to 365 days and not cancelled by new activity; mute by thread; archive and dismiss mean Done (decision 34, L12, L24).
- Target deletion: hide on trash or archive, restore on restore, purge at purge; Activity and sent notifications stay (L9). Notification retention 180 and 365 days (L10).
- Presence: gateway-held, heartbeat 20 seconds, expiry 45 seconds (L16). Read state: monotonic boundary, derived count (L17).
- Mentions: User and Team only; Team fans out at delivery (L18, L19).
- Attachments: item and page targets, not comment-level; link schemes http and https; rate limits (L15, L23).
- Activity: item history is Collaboration-owned (decision 56), read-only, English fallback phrase plus a language-neutral change.
- Move the Inbox list query from Work Management to Collaboration (code placement, not a product change).

Fix in code before the catalog can reach DONE: the authorization defects in section 1.3, the missing unique indexes (RLS-safe migration), the page mention view check and the delivery-time recheck.

## 6. Not covered

| Product section or checklist | Disposition |
|---|---|
| Sections 1 to 7 (mission, owns, does not own, target, COL-001, COL-002) | Covered as rules by UC-COL-A01, A02, B18, C05. |
| Sections 8 to 19 (comment, content, reply, anchor, edit, delete, status) | UC-COL-A01 to A08, H04. |
| Sections 20 to 25 (mentions) | UC-COL-B01 to B05, D04, H06. |
| Sections 26 to 29 (reactions) | UC-COL-A09 to A11, H03. |
| Sections 30 to 35 (attachments) | UC-COL-B06 to B12, C06, F03. Upload states pending, uploaded, failed, removed: only the failed row and the stored result exist in the UI; a durable pending state is deliberately not modelled (an upload is one request). Signed upload URLs are not used (decision L23). |
| Sections 36 to 42 (presence, cursor) | UC-COL-E03, E04. |
| Sections 43 to 47 (read state) | UC-COL-G14. |
| Sections 48 to 51 (watchers) | UC-COL-B13 to B17. |
| Sections 52 to 59 (notifications, preferences) | UC-COL-D04 to D14, G03 to G13. Provider delivery state separate from read state: UC-COL-D13 (no delivery path yet). |
| Sections 60 to 65 (activity) | UC-COL-D01 to D03, D09. Activity actor kinds Automation, Integration and System: only User actors exist today; listed under UC-COL-D10 and H07. |
| Sections 66 to 71 (target registry, scope, authorization) | UC-COL-B18, C01 to C05. |
| Sections 72 to 75 (author versus moderator, privacy) | UC-COL-C01, C02, B05, G03. |
| Sections 76 to 83 (realtime, optimistic) | UC-COL-E01 to E05, G15, H01. Optimistic reaction and comment: UC-COL-A09, A10 UI states. |
| Sections 84 to 95 (deletion, retention, identity, thread, edit history) | UC-COL-F01 to F07, A06. Comment edit history (section 95, DCREQ215) is deliberately not built: only an edited mark is shown; add a use case if the owner asks for history. |
| Sections 96 to 99 (histories purpose-specific, abuse, sanitization) | UC-COL-H04, H05, H08; histories are separate by design (D01 and G07 do not merge them). |
| Sections 100 to 108 (search, analytics, automation, integrations, neighbours) | UC-COL-G16, D10, H07. Analytics consumption is Analytics' use case ([analytics.md](analytics.md)). |
| Sections 109 to 123 (source alignment, change impact, checklists, stop conditions) | Not use cases. The comment checklist maps to A01 to A08; the notification checklist to D04 to D14; the presence checklist to E03; the read and watch checklist to B13 to B17 and G14. |
| Mentions of type Page and Board | Deliberately not offered (decision L19). |
| Comment-level attachments | Deliberately not offered (decision L23); attachments belong to the item or page. |
| Account-level policy over notification types and urgency (section 58) | Not built; needs an Account policy owner ([accounts.md](accounts.md)). |

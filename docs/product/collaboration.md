---
document_id: PROD-COLLABORATION
document_type: product-context
status: active
owner: collaboration
applies_to:
  - collaboration
  - comments
  - mentions
  - reactions
  - attachments
  - presence
  - read-state
  - watchers
  - notifications
  - user-activity
evidence:
  - PRODUCT.md
  - docs/product/product-model.md
  - docs/product/product-experience.md
  - docs/product/contexts/identity.md
  - docs/product/contexts/workspaces.md
  - docs/product/contexts/governance.md
  - docs/product/contexts/work-management.md
  - docs/product/contexts/documents.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - docs/architecture/data-ownership-and-consistency.md
  - backend/src/Notrelix.Domain/Collaboration/
  - backend/tests/
  - frontend/packages/features/collaboration/
review_on:
  - comment-model-change
  - collaboration-target-change
  - mention-model-change
  - reaction-model-change
  - notification-model-change
  - activity-model-change
  - attachment-change
  - presence-or-read-state-change
  - watcher-change
  - collaboration-deletion-or-retention-change
---

# Collaboration Context

> **Collaboration owns human interaction around product resources: comments, replies, mentions, reactions, collaboration attachments, watch/read state, user-facing activity/notifications, and ephemeral presence.**
>
> It references Work Management, Documents, and other product resources without taking ownership of them.

This document is the canonical product owner for Collaboration semantics.

Governance owns security Audit. Source contexts own the resources being discussed. Platform/realtime owns delivery mechanics.

# 1. Mission

Collaboration turns product resources into shared working surfaces without duplicating their business state.

It supports:

```text
conversation
attention
reaction
read/unread
watch/follow
presence
user-facing history
notification
```

around explicit scoped product targets.

# 2. Owns

Collaboration owns:

```text
Comment
reply/thread relation
CommentAnchor
Mention
Reaction
Attachment metadata associated with collaboration
ResourceReadState
ResourceWatcher/watch state
PresenceSession
user-facing Activity
Notification recipient/state semantics
collaboration lifecycle/retention
```

Current source has first-class Attachments, Comments, Mentions, Presence, Reactions, ReadStates, and Watchers.

# 3. Does not own

```text
target Board/Item/Field/View
→ Work Management

target Page/Block
→ Documents

target Account/Workspace
→ owning context

target authorization/share policy
→ Governance

principal authentication
→ Identity

security Audit
→ Governance

email/push/websocket delivery provider
→ Platform/Infrastructure/Integrations
```

# 4. Collaboration target

Every Collaboration object attaches to an explicit product target/source.

A target uses stable resource kind/type, resource ID, and Account/Workspace scope where required.

# 5. COL-001 — Collaboration target is explicit and scoped

Creating, reading, mutating, reacting to, watching, or subscribing to Collaboration state MUST validate the target through Application/Governance/source-owner contracts.

A naked target ID or client Workspace value is not trusted authority.

# 6. Target ownership

Comment on Item remains Collaboration-owned; Item remains Work Management-owned.

Comment on Page remains Collaboration-owned; Page remains Documents-owned.

# 7. COL-002 — Collaboration never mutates target aggregate implicitly

Creating/editing/deleting comments, reactions, mentions, watch state, or read state does not mutate the target product resource unless a separate approved target-context operation is invoked.

# 8. Comment

A Comment is authored collaborative conversation attached to a resource.

Current source carries Account, Workspace, target ResourceRef, parent, content, CommentAnchor, and CommentStatus.

# 9. Comment content

Comment content defines:

```text
normalization
size limits
mention extraction
supported formatting
edit/delete policy
```

It never stores mutable foreign resource object graphs.

# 10. COL-003 — Comment target existence/access is validated outside pure Domain

Application resolves target existence, scope, and permission, then supplies Collaboration-local facts.

Pure Domain does not fetch foreign repositories.

# 11. Reply/thread

A reply references a parent Comment in a compatible target/thread.

Cross-resource and cross-Workspace reply chains are invalid.

# 12. COL-004 — Reply remains on the same target/thread

ParentCommentId alone is insufficient; target identity remains part of thread integrity.

# 13. Comment Anchor

Current source has selector/offset anchor data.

An Anchor points to a more specific location inside target content.

# 14. COL-005 — Anchor is locator, not target-content ownership

Anchor can become stale as content changes.

Collaboration may preserve, re-resolve, or mark it unresolved; it does not become a second copy of target content.

# 15. Anchored comment after content changes

Product behavior should define whether anchor:

```text
re-resolves
becomes outdated
remains historical
loses exact position but retains target
```

It must not silently attach to unrelated content.

# 16. Comment edit

Edit preserves target/thread identity and follows author/moderation policy.

Product may expose edited metadata.

# 17. Comment delete

Deletion may mean tombstoned/hidden/purged according to policy.

Thread shape and reply history require explicit behavior.

# 18. COL-006 — Comment deletion policy is explicit

Deleting a parent Comment must define child-reply behavior.

Generic ORM cascade is not product semantics.

# 19. Comment status

CommentStatus is Collaboration-local.

It must not be shared with Page, Work Item, Automation, or Integration lifecycle status.

# 20. Mentions

Mention is an intentional reference to a supported subject.

Current source stores source ResourceRef, MentionType, MentionedId, Workspace scope, and creation time.

# 21. COL-007 — Mention resolves to stable identity

Durable Mention resolves display syntax such as `@name` to stable User/team/approved subject identity.

Display text alone is not durable identity.

# 22. Mention authorization

Validate source access and mentioned subject scope.

A typed email/display name cannot create a cross-tenant Mention by itself.

# 23. Mention and attention

A committed Mention may cause Notification, Activity, and realtime attention.

Downstream delivery follows the committed fact.

# 24. COL-008 — Mention delivery is idempotent

Retrying the same logical Mention must not create duplicate Notification/email/push effects.

# 25. Edited-comment mentions

Editing can add/remove Mentions.

Unchanged Mentions should not be re-notified repeatedly unless product policy explicitly says so.

# 26. Reactions

Reaction is one actor's lightweight response to a target.

Current source stores target ResourceRef, UserId, Emoji, and Workspace scope.

# 27. COL-009 — Reaction uniqueness is deterministic

Conceptually:

```text
target + actor + reaction kind
```

or an approved equivalent defines uniqueness.

# 28. Reaction retry

Repeated create/delete must be idempotent and must not duplicate counts/events.

# 29. Reaction count

Counts may be cached/projected.

The reaction membership set is the source fact, not one stale client counter.

# 30. Attachments

Current source includes Attachment, AttachmentType, and FileMetadata.

Attachments represent safe file/object metadata associated with collaboration content.

# 31. COL-010 — Attachment stores metadata/object identity, not arbitrary binary Domain data

Object storage/provider I/O is outside Domain.

Collaboration owns safe metadata/reference and association.

# 32. Upload lifecycle

Where product-visible:

```text
pending
uploaded
failed
removed
```

can represent attachment upload state.

Signed upload URL is a short-lived infrastructure artifact.

# 33. Attachment authorization

Upload/download/delete uses target authorization plus file policy.

Possession of object key alone is not permission.

# 34. COL-011 — Attachment download capability is scoped and short-lived where appropriate

Provider URL/token is not durable public credential in Domain/events.

# 35. Attachment deletion

Metadata and underlying object retention may differ.

Cleanup is explicit and idempotent.

# 36. Presence

Current source has PresenceSession containing Account/Workspace/User/Connection, status, and last-seen timestamps.

Presence is collaboration awareness, not content truth.

# 37. COL-012 — Presence is ephemeral collaboration state

Presence can tolerate loss, delay, reconnect, and duplicate connection updates.

It MUST NOT become durable Page/Item content or authorization state.

# 38. Presence scope

Presence is scoped to User, Account/Workspace, and resource/channel where needed.

No global cross-tenant broadcast.

# 39. Presence status

Online/away/offline are best-effort collaboration states.

They are not proof of latest permission, latest read state, or durable authentication authority.

# 40. Presence cleanup

Stale/disconnected sessions expire according to runtime policy.

Ghost presence is tolerable; corrupted product state is not.

# 41. COL-013 — Presence does not authorize access

Joining a presence channel requires normal server authorization.

Existing presence cannot be reused as permission proof.

# 42. Cursor/typing

Cursor and typing indicators are ephemeral signals and generally not durable business history.

# 43. Read State

Current source has ResourceReadState keyed by user/resource with LastReadAt, LastReadCommentId, and UnreadCount.

# 44. COL-014 — Read state belongs to user + resource scope

Read/unread is personal Collaboration state, not one global target property.

# 45. Unread semantics

Define:

```text
what counts as unread
last-read boundary
self-authored behavior
deleted-comment behavior
thread/mention behavior
```

# 46. COL-015 — Unread count derives from stable read boundary

A mutable unread counter is a materialized convenience, not unrecoverable sole truth.

# 47. Mark read

Marking read is scope-aware, retry-safe, and monotonic where appropriate.

An older marker must not move the user backward accidentally.

# 48. Watchers

Current source has a first-class Watchers area with ResourceWatcher.

Watch/follow state is user interest in updates around a resource.

# 49. COL-016 — Watch state is explicit user-resource preference

Watching is not automatically equivalent to recently opening a resource, Workspace membership, or one historic Mention.

# 50. Automatic watching

If users auto-watch after assignment/comment/mention, the rule is explicit and explainable.

# 51. Unwatch

Unwatch stops watch-driven future attention.

It does not delete comments, revoke access, or erase Notification history.

# 52. Notifications

A durable Notification represents user-facing attention caused by a product fact.

Current implementation placement may be elsewhere or incomplete; product ownership remains Collaboration until architecture explicitly changes.

# 53. COL-017 — Notification has explicit recipient

A durable Notification identifies:

```text
recipient
scope
type
source/target resource
creation time
read/state
```

Broadcast requires explicit audience/fan-out semantics.

# 54. Audience fan-out

A source fact may resolve many recipients through:

- Mention;
- Watcher;
- assignment;
- membership/policy;
- another approved audience rule.

Each recipient gets independent attention/read state.

# 55. Notification source versus delivery

Notification creation is authoritative independently from email, push, websocket, or provider result.

# 56. COL-018 — Provider delivery result is not Notification truth

Email/push failure does not erase the Notification.

Provider retry must not create a second Notification.

# 57. Delivery channels

A Notification may have several delivery attempts/channels.

Channel delivery state is separate from Notification read state.

# 58. Preferences

Notification preferences may depend on User, Account policy, type, and urgency.

Provider preferences cannot weaken mandatory security delivery when policy says otherwise.

# 59. Notification read/dismiss

Read/dismiss mutates Collaboration attention state only.

It does not mutate source resource.

# 60. Activity

User-facing Activity narrates meaningful product actions.

It may be grouped, summarized, filtered, and formatted for humans.

# 61. COL-019 — Activity and Governance Audit are different products

Activity optimizes human understanding.

Audit optimizes governed evidence, integrity, and retention.

Collaboration cannot mutate Governance Audit history.

# 62. Activity source

Activity can derive from committed product facts such as Item changes, document changes, comments, approvals, or Automation completion.

# 63. COL-020 — Activity maps product facts, not transport attempts

Retry/outbox delivery must not create duplicate user-facing Activity for one logical action.

# 64. Activity actor

Actor can be User, Automation, Integration, or System according to product meaning.

Process hostname is not a product actor.

# 65. Activity retention

Activity retention may differ from Comment, Notification, and Audit retention.

# 66. Resource target registry

Supported Collaboration targets require a stable target vocabulary.

New target support requires owner context, scope, authorization, deep link/rendering, deletion behavior, and realtime scope.

# 67. COL-021 — New target type is cross-context contract change

“Allow comments on X” is not only adding a new enum/string.

# 68. Target existence

Do not create new Collaboration state against deleted/inaccessible target unless an explicit historical/tombstone workflow exists.

# 69. Cross-Workspace target

Target scope and Collaboration scope must match unless an explicit cross-scope contract exists.

# 70. COL-022 — Collaboration scope follows the target

Account/Workspace scope is derived/validated from the target, not trusted only from client input.

# 71. Target authorization

Actions can include:

```text
view comments
comment/reply
edit/delete
moderate
react
mention
attach
watch
read activity
```

Governance/source owner defines required permission semantics.

# 72. Author versus moderator

Author ownership and moderation/admin capabilities are distinct.

Do not use one generic role shortcut.

# 73. Mention privacy

Mention autocomplete/commit must not reveal Users outside allowed scope.

# 74. Notification privacy

Notification payload must not grant or leak source data after resource permission is revoked.

# 75. COL-023 — Historical Notification does not freeze resource permission

Receiving one notification once does not grant perpetual resource access.

# 76. Realtime

Collaboration realtime can deliver Comment, Reaction, Presence, Notification, and other relevant changes.

Clients assume duplicate/out-of-order/reconnect unless stronger contract exists.

# 77. COL-024 — Collaboration realtime is replay-safe

Repeated delivery cannot duplicate Comment, reaction count, Notification, unread count, or presence state.

# 78. Reconnect

Durable state is recovered by query; ephemeral presence is rebuilt.

# 79. Permission revocation while connected

Future events stop after access revocation and clients converge to safe protected state.

# 80. Optimistic comment

Client may use temporary identity before server acceptance.

# 81. COL-025 — Optimistic Collaboration state reconciles to authoritative identity

Temporary client IDs cannot become durable cross-client identity.

# 82. Optimistic reaction/read-state

Optimism is acceptable when create/remove/read semantics are idempotent and deterministic.

Authoritative server state wins.

# 83. Attachment optimistic UI

Do not present file as durably available before the accepted upload/metadata state is sufficiently committed.

# 84. Target deletion

Target owner emits the deletion/archive fact.

Collaboration decides behavior for comments, attachments, mentions, reactions, watchers, read state, notifications, Activity, and presence.

# 85. COL-026 — Target deletion does not automatically cascade Collaboration history

Depending on policy, Collaboration may hide, tombstone, retain read-only, or purge later.

# 86. Page/Block deletion

Anchored comments may become unresolved/historical.

Documents owns content deletion; Collaboration owns discussion reaction.

# 87. Item deletion

Item-related comments/activity can remain historical while target becomes unavailable/tombstoned.

# 88. Comment delete versus Activity

Activity is separate state and does not automatically disappear when Comment is edited/deleted.

# 89. Mention removal

Removing Mention can change future attention, while already-delivered historical Notification may remain.

# 90. Attachment retention

Object cleanup lifecycle may differ from Comment metadata lifecycle.

# 91. Identity deletion/anonymization

Identity deletion may anonymize author/actor/Mention/Watcher/ReadState references according to retention/privacy policy.

# 92. COL-027 — Historical Collaboration can outlive active Identity

Historical conversation can remain while actor identity is anonymized/tombstoned.

# 93. Workspace deletion

Workspace deletion/archive is cross-context workflow with explicit Collaboration retention/export/purge rules.

# 94. Thread deletion

Parent/child reply semantics preserve coherent thread display.

# 95. Edit history

Comment edit history, if supported, is Collaboration history.

It is distinct from Documents history and Governance Audit.

# 96. COL-028 — Collaboration histories are purpose-specific

Do not merge Comment history, Document history, Activity, Audit, and Notification history into one generic product “event log”.

# 97. Abuse controls

Public/guest Collaboration can require rate limits, moderation, content limits, and abuse controls.

# 98. Content sanitization

Supported rich Comment content defines allowed markup, links, escaping, mentions, and attachments.

# 99. COL-029 — Collaboration content is safe to render by contract

Rendering security cannot depend only on “frontend will escape it”.

# 100. Search

Comment/Activity search is derived and authorization-scoped.

# 101. Analytics

Analytics may derive Collaboration metrics but never becomes mutation authority.

# 102. Automation

Automation may consume stable Collaboration facts and invoke normal context operations.

# 103. Integrations

Provider messaging/comment sync maps external content/identity into approved Collaboration operations.

# 104. Work Management

Collaboration targets Work Management resources through stable ResourceRef and target authorization.

# 105. Documents

Collaboration targets Page/Block/anchors while Documents retains content ownership.

# 106. Governance

Governance owns target permission, moderation policy, sharing, and Audit.

# 107. Identity

Identity supplies stable principal identity for author, recipient, Mention, Watcher, and Presence.

# 108. Workspaces

Workspaces supplies Workspace membership/scope.

Cross-Workspace participant/target mismatches are rejected.

# 109. Current source alignment

Current Collaboration source contains:

```text
Attachments
Comments
Mentions
Presence
Reactions
ReadStates
Rules
Watchers
```

Current classes include Comment, CommentAnchor, Mention, PresenceSession, Reaction, ResourceReadState, Attachment/FileMetadata, and ResourceWatcher-related state.

# 110. Current source gap/ownership note

The top-level current Domain tree does not show dedicated Notification or Activity folders.

That does not automatically transfer product ownership elsewhere.

Until explicit product architecture changes:

```text
user-facing Notification / Activity
→ Collaboration semantics
```

# 111. Current ambiguity watch

Do not normalize:

```text
Presence → authorization
ReadState → resource ownership
provider delivery status → Notification truth
Activity → Audit
ResourceRef → permission
object key → download authority
Watcher → Workspace membership
```

# 112. Change impact — Comment/target

Review source context, Governance, scope, anchors, Mentions, Reactions, Attachments, Notifications, Activity, realtime, search, and deletion.

# 113. Change impact — Mention/Notification

Review Identity, Workspaces, target authorization, fan-out, preference, providers, idempotency, read state, and deep links.

# 114. Change impact — Presence

Review realtime runtime, target authorization, Workspace switch, mobile lifecycle, connection identity, and privacy.

# 115. Change impact — ReadState/Watcher

Review Notification fan-out, unread semantics, target lifecycle, Workspace scope, migration, and frontend cache.

# 116. Comment checklist

```text
[ ] stable Comment ID
[ ] scoped target
[ ] target authorization
[ ] author
[ ] reply compatibility
[ ] content validation
[ ] anchor semantics
[ ] edit/delete policy
[ ] Mentions/Reactions
[ ] Attachments
[ ] events/realtime
[ ] retention
```

# 117. Notification checklist

```text
[ ] explicit recipient
[ ] source fact
[ ] scope/target
[ ] type
[ ] read state
[ ] channel preferences
[ ] idempotent fan-out
[ ] provider delivery separate
[ ] permission revocation behavior
[ ] retention
```

# 118. Presence checklist

```text
[ ] Workspace scope
[ ] User
[ ] resource/channel scope
[ ] connection identity
[ ] authorization
[ ] stale expiry
[ ] reconnect
[ ] ephemeral only
```

# 119. Read/watch checklist

```text
[ ] user + resource scope
[ ] target authorization
[ ] retry-safe read boundary
[ ] derived unread count
[ ] explicit/automatic watch rule
[ ] unwatch semantics
[ ] target deletion reaction
```

# 120. Testing/evidence

Critical evidence covers:

```text
target scope/authorization
cross-Workspace rejection
Comment create/reply/edit/delete
parent/target consistency
CommentAnchor
Mention identity/scope/idempotency
Reaction uniqueness/retry
Attachment metadata/security
Presence scope/reconnect/expiry
ReadState monotonic/unread behavior
Watcher idempotency
Notification recipient/fan-out/provider retry
Activity != Audit
target deletion/tombstone
realtime duplicate/out-of-order/reconnect
Identity anonymization
```

# 121. Stop conditions

Stop rather than guess if:

- comments embed foreign product graphs;
- target scope is trusted only from client input;
- reply can cross target/resource;
- Mention has no stable identity;
- Reaction retry duplicates counts;
- raw binary/signed URL becomes durable Domain/event data;
- Presence becomes content truth or authorization;
- unread count is unrecoverable sole truth;
- Notification lacks recipient;
- provider delivery becomes Notification authority;
- Activity is used as Audit;
- realtime is unscoped/unauthorized;
- target deletion cascades history with no product policy.

# 122. Related canonical owners

```text
PRODUCT.md
docs/product/product-model.md
docs/product/product-experience.md
docs/product/contexts/identity.md
docs/product/contexts/workspaces.md
docs/product/contexts/governance.md
docs/product/contexts/work-management.md
docs/product/contexts/documents.md
docs/product/contexts/automation.md
docs/product/contexts/integrations.md
docs/product/contexts/analytics.md
docs/architecture/events-realtime-and-delivery-boundary.md
docs/architecture/data-ownership-and-consistency.md
docs/architecture/contract-boundaries.md
frontend/docs/architecture/realtime.md
frontend/docs/architecture/state-query-mutations.md
```

# 123. Final Collaboration rule

For every Collaboration capability, answer:

```text
What product resource is the target?
Who owns that target?
Which Workspace/Account scopes it?
Who may view/interact?
Who authored/receives the Collaboration fact?
Is it durable conversation, attention, read/watch state, or ephemeral presence?
How are duplicate/retry/realtime handled?
What happens when target is deleted?
What is Activity versus Governance Audit?
Which provider delivery is only a side effect?
```

The target is:

> **a scoped human-interaction layer that makes work collaborative without duplicating target-resource ownership, weakening authorization, or confusing ephemeral attention state with durable product truth.**

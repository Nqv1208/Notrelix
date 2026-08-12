---
document_id: PROD-DOCUMENTS
document_type: product-context
status: active
owner: documents
applies_to:
  - documents
  - pages
  - blocks
  - document-hierarchy
  - resource-links
  - document-versions
  - document-templates
evidence:
  - PRODUCT.md
  - docs/product/product-model.md
  - docs/product/product-experience.md
  - docs/product/contexts/workspaces.md
  - docs/product/contexts/governance.md
  - docs/product/contexts/work-management.md
  - docs/product/contexts/collaboration.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - backend/src/Notrelix.Domain/Documents/
  - backend/tests/
  - frontend/packages/product/docs-core/
  - frontend/packages/product/docs-web/
  - frontend/packages/product/docs-mobile/
review_on:
  - page-lifecycle-change
  - block-type-change
  - block-content-schema-change
  - hierarchy-or-ordering-change
  - document-versioning-change
  - resource-link-change
  - document-template-change
  - document-sharing-change
  - collaborative-editing-change
  - document-deletion-or-retention-change
---

# Documents Context

> **Documents owns durable authored knowledge/content: Page identity, Page hierarchy, typed Block content, document-local ordering, resource links, version/history semantics, and document templates.**
>
> Documents may reference Work Management and participate in Collaboration without merging those data models into document content.

This document is the canonical product owner for Documents semantics.

It does not own comments, mentions, reactions, notifications, presence/read state, Work Management records, Governance policy, object-storage mechanics, or realtime transport.

# 1. Mission

Documents provides a structured authoring model for long-form and block-based knowledge work.

The product model supports:

```text
Page
Block
hierarchy
typed content
ordering
links/embeds
history/versioning
templates
```

without collapsing into one giant HTML string or generic arbitrary JSON blob.

# 2. Owns

Documents owns:

```text
Page lifecycle
Page hierarchy
Page title/metadata
Page visibility intent
Block identity/type/content/properties
Block parent/child hierarchy
Block sibling ordering
ResourceLink
document version/snapshot semantics
PageTemplate
document content events
document deletion/restore behavior
```

Current source has first-class `Pages`, `Blocks`, `ResourceLinks`, `Templates`, and `Versions`.

# 3. Does not own

```text
Workspace lifecycle/membership
→ Workspaces

resource permission/share-link policy
→ Governance

Board/Item/Field
→ Work Management

comments/mentions/reactions/watch/read-state/presence
→ Collaboration

automation rule/execution
→ Automation

provider sync
→ Integrations

search index
→ derived technical capability

analytics/reporting
→ Analytics
```

# 4. Ubiquitous language

**Page** — durable Workspace-scoped document/container.

**Block** — typed content node belonging to one Page.

**Block Type** — semantic discriminator defining content/properties contract.

**Block Content** — validated type-specific authored content.

**Block Properties** — validated type-specific auxiliary configuration.

**Resource Link** — stable typed relationship to another product resource.

**Document Version** — user/history version identity.

**Document Snapshot** — captured document state for history/recovery.

**Page Template** — reusable Page/content creation input.

# 5. DCT-001 — Documents is Page + typed Block content, not opaque HTML

Canonical authored content is structured.

Raw HTML may exist only as sanitized import/export/rendering representation. It MUST NOT become the only trusted document model.

# 6. Structured-content purpose

Typed content preserves:

```text
validation
semantic editing
accessibility
mobile rendering
search/indexing
migration
embeds
history
automation/integration compatibility
```

# 7. Page

A Page is a durable Workspace-scoped content resource.

It may own stable ID, title, parent relation, lifecycle, visibility intent, content-tree relation, and history/version metadata.

# 8. DCT-002 — A Page belongs to one Workspace

Parenting, Blocks, queries, links, events, search, and realtime preserve the Page's Workspace scope.

# 9. Page identity

Page identity survives rename, movement, visibility change, content edits, archive, and restore.

Title/path is not durable identity.

# 10. Page lifecycle

Current source separates `Active` and `Archived`, and separately supports delete/restore.

Canonical semantics therefore distinguish:

```text
active
archived
deleted
restored where allowed
```

rather than reintroducing universal `SoftDeleted`.

# 11. DCT-003 — Archive and delete are different lifecycle operations

Archive retains content under inactive/restricted semantics.

Delete follows Documents deletion/retention policy.

Physical purge is a separate retention operation.

# 12. Page visibility

Current source exposes:

```text
Private
Workspace
Public
```

Visibility describes document access intent/default posture.

Governance still owns final authorization.

# 13. DCT-004 — Page visibility is not the whole permission model

Visibility MUST NOT bypass policy, share-link rules, resource-specific restrictions, or embedded-target authorization.

# 14. Page hierarchy

Page parent/child relation must preserve:

```text
same Workspace
valid parent
no cycle
valid lifecycle
authorized move
```

# 15. DCT-005 — Page hierarchy is tenant-safe and acyclic

A Page cannot become its own parent, descendant of itself, or child of another Workspace's Page.

# 16. Page movement

Moving a Page changes organization, not identity.

Derived breadcrumb/path state reconciles after move.

# 17. Page path

A path is derived from current hierarchy and names.

It is not competing durable ownership.

# 18. Block

A Block is one typed node in one Page content tree.

Current source includes Account, Workspace, Page, Parent, Type, Content, Properties, and fractional Position.

# 19. DCT-006 — A Block belongs to exactly one Page

Block scope must match Account/Workspace/Page.

A raw ParentId cannot move it into another Page or Workspace.

# 20. Block Type

A Block Type is a semantic content contract.

Potential types include paragraph, heading, todo, code, image, quote/callout, divider, and resource reference/embed.

Exact supported types are executable evidence.

# 21. DCT-007 — Block content validates against Block Type

Each Block Type defines as applicable:

```text
content schema
properties schema
normalization
validation
empty/null behavior
web/mobile renderer/editor
import/export
migration/evolution
```

# 22. Block Content

Block Content owns authored content for that Block.

It must not embed mutable copies of BoardItem, User, provider objects, or Collaboration threads.

# 23. Block Properties

Properties configure typed behavior such as heading level, code language, media presentation, or embed mode.

They remain schema-validated.

# 24. DCT-008 — Arbitrary unvalidated Block JSON is forbidden

Flexible serialization is allowed only behind a defined Block-Type contract.

# 25. Block hierarchy

Parent Block must be in the same Account, Workspace, and Page.

Self/descendant cycles are invalid.

# 26. DCT-009 — Block-tree validation uses supplied ancestry facts

Application may supply ancestry/path facts.

Pure Domain validates local rules without querying persistence/provider itself.

# 27. Block movement

Move changes parent and position as one coherent semantic mutation.

Cross-Page or cyclic state must never commit.

# 28. Ordering

Sibling Blocks use deterministic sortable ordering.

Current source uses `FractionalIndex`.

Ordering must preserve adjacency, prefix/boundary correctness, concurrency behavior, and relative order under rebalance.

# 29. DCT-010 — Block ordering is server-authoritative and deterministic

No floating midpoint, array-index durable identity, or client-only ordering authority.

# 30. Reorder no-op

Moving to the effective current parent/position should avoid unnecessary version/event/history churn.

# 31. Content editing

Durable Page/Block edits must protect against stale overwrite.

# 32. DCT-011 — Document editing is version/concurrency aware

Use expected version, approved CRDT/OT semantics, or another explicit conflict protocol.

Presence/cursor state is not concurrency control.

# 33. Collaborative editing

If simultaneous editing is supported, define:

```text
authoritative state
operation identity
merge/conflict
offline/reconnect
permission revocation
version/checkpoint
history relationship
```

# 34. DCT-012 — Realtime transport is not collaborative-editing semantics

Websocket connectivity alone does not define durability, merge, conflict, ordering, or recovery.

# 35. Presence

Presence/cursors belong to Collaboration.

Documents exposes resource/version identity as needed but does not store presence as content truth.

# 36. Comment anchors

Collaboration may anchor a comment to Page, Block, selector, or range.

Documents owns target content; Collaboration owns comment/anchor discussion lifecycle.

# 37. DCT-013 — Comment anchors never become content ownership

Deleting/editing a Comment cannot mutate document content implicitly.

Moving content may make an anchor stale without transferring content ownership.

# 38. Resource Links

A Resource Link is a stable relationship between document content and another product resource.

Current source stores source/target `ResourceRef`, Workspace scope, and LinkType.

# 39. DCT-014 — Resource Link preserves target ownership

Documents stores link identity/metadata.

The foreign target remains owned by its context and is never directly mutated through Documents persistence.

# 40. Linked-resource authorization

Rendering/opening an embed re-evaluates target authorization as required.

# 41. DCT-015 — Document sharing is non-transitive across resource links

```text
Public Page
→ embeds private Board
```

does not make the Board public unless Governance explicitly grants access.

# 42. Link types

Link type may communicate reference/embed/relation semantics.

It never creates hidden mutation authority.

# 43. Resource-link deletion

Removing a Resource Link removes only the relationship, not the target.

# 44. Target deletion

Deleted/archived target behavior must be explicit: unavailable reference, tombstone, hidden embed, historical link, or cleanup.

# 45. Cross-Workspace links

Forbidden by default unless an explicit cross-scope contract exists.

# 46. Document Versions

Current source has first-class `DocumentVersion` and `DocumentSnapshot`.

This is distinct from aggregate concurrency counters.

# 47. DCT-016 — History/version is not aggregate concurrency version

Possible version concepts:

```text
expected aggregate version
document history version
snapshot/checkpoint
realtime sequence
```

They are not interchangeable.

# 48. Document history

If user-facing history exists, define what creates a version, actor/time, recoverability, retention, and Block/Page identity relationship.

# 49. Snapshot

Snapshot is captured historical state for history, restore, checkpoint, or migration.

It has explicit source version/schema.

# 50. DCT-017 — Snapshot is not a second live document

Historical snapshot is immutable historical representation unless restored through a deliberate current-state mutation.

# 51. History granularity

Do not create permanent user-visible history for every internal write unless product semantics need it.

# 52. Restore historical version

Restore authorizes, validates the historical version, creates a new current mutation/version, and preserves the fact newer history once existed.

# 53. Templates

Current source has `PageTemplate` and `PageTemplateStatus`.

# 54. DCT-018 — Page Template is creation input, not hidden live authority

Template instantiation creates ordinary Page/Block identities.

Later template edits do not silently mutate existing Pages unless a linked-template feature is explicitly designed.

# 55. Template lifecycle

Template status/lifecycle is separate from Page lifecycle.

# 56. Template content validation

Template Blocks obey normal Block-Type schemas and validation.

# 57. Template versioning

Reapply/upgrade, if supported, is an explicit migration/conflict feature.

# 58. Documents and Workspaces

Workspaces supplies tenant/membership.

Documents owns Page/Block state.

# 59. Documents and Governance

Documents declares protected actions such as:

```text
view
edit
move
archive/delete
manage visibility
view/restore history
manage template
```

Governance evaluates authorization.

# 60. DCT-019 — Documents does not authorize by visibility alone

Visibility is one input, not the final policy.

# 61. Documents and Work Management

Allowed stable relationships include Page↔Board/Item references/embeds.

The two contexts remain separate.

# 62. DCT-020 — Page/Block content does not become Work Management storage

Do not store BoardItem business values as arbitrary document Blocks or rich Page content as hidden Board internals merely to collapse models.

# 63. Documents and Collaboration

Collaboration owns comments, replies, mentions, reactions, read/watch state, notifications/activity, presence, and collaboration attachments.

Documents owns Page/Block content/history.

# 64. Documents and Automation

Automation consumes approved document facts and invokes normal Documents operations.

# 65. Documents and Integrations

External provider formats are translated into the Notrelix Page/Block model.

Provider schema does not become canonical Documents schema automatically.

# 66. Documents and Analytics

Analytics derives document metrics and remains non-authoritative.

# 67. Documents and Search

Search indexes Page/Block content as derived state and remains authorization-filtered.

# 68. DCT-021 — Search/index is not document truth

Index lag/failure cannot become content mutation authority.

# 69. Files/media in Blocks

Blocks may reference uploaded objects.

Documents owns content/file-reference meaning; object-storage mechanics remain Infrastructure.

# 70. File identity

Store stable object/file identity and safe metadata rather than large binary payloads.

# 71. DCT-022 — Binary payload is not Domain/event content

Large binary data must not be embedded in Domain events, logs, or arbitrary Block JSON.

# 72. Events/facts

Potential facts include:

```text
PageCreated/Renamed/Moved/Archived/Deleted/Restored
PageVisibilityChanged
BlockCreated/Changed/Moved/Deleted
ResourceLinkCreated/Removed
DocumentVersionCreated
DocumentRestored
TemplateCreated/Changed
```

# 73. DCT-023 — Public document events expose facts, not full trees

Do not publish the entire Page/Block tree or full snapshot for every change.

# 74. Realtime

Realtime may signal Page/Block/schema/lifecycle/version changes.

It does not replace authoritative content/history.

# 75. DCT-024 — Missed realtime cannot permanently corrupt document state

Clients recover via current query, collaborative resync, or snapshot/checkpoint as applicable.

# 76. Permission revocation while connected

Realtime/document editing must converge to revoked access and safe client state.

# 77. Archived/read-only behavior

Archived/deleted/inaccessible Pages do not silently accept edits.

# 78. Semantic no-op

Identical title/content/properties/effective position should avoid fake mutation where possible.

# 79. DCT-025 — No-op edit does not create false content history

No-op should not create fake version, event, or misleading activity.

# 80. Block-Type evolution

Adding/changing a type requires review of schema, persistence, API, web/mobile, accessibility, templates, history, search, import/export, Automation, and Integrations.

# 81. Unknown Block Type

Older clients need explicit compatibility behavior; never silently coerce unknown types to paragraph.

# 82. Import

External content maps into validated Page/Block semantics.

Untrusted raw HTML/JSON must be sanitized/validated.

# 83. Export

HTML/Markdown/PDF/etc. are derived boundary representations.

# 84. DCT-026 — Import/export formats are boundary representations

Any round-trip guarantee is explicit per format.

# 85. Duplicate Page

Duplicate creates new Page and Block identities.

Resource links must define whether they still target original resources.

# 86. Move within Workspace

Moving under another organizational parent preserves Page identity and Workspace scope.

# 87. Move across Workspace

Cross-Workspace move affects tenant, authorization, links, comments, history, search, automation, and integrations.

# 88. DCT-027 — Cross-Workspace document movement is a migration, not a tree move

Do not simply change WorkspaceId.

# 89. Page deletion

Deletion must review Blocks, links, versions/snapshots, Collaboration, Automation, Integrations, Search, Analytics, and retention.

# 90. DCT-028 — Page deletion is lifecycle workflow, not ORM cascade

Dependent contexts choose retain/tombstone/hide/cleanup/revoke under policy.

# 91. Block deletion

Deleting a Block affects child Blocks, anchors, links, history, and search.

# 92. DCT-029 — Block deletion defines child behavior

Choose deterministic policy: recursive subtree delete, promote children, or reject until children handled.

# 93. Historical references

Deleted content identity may remain in history/activity/audit under retention rules.

# 94. Restore

Restore preserves valid scope/hierarchy and current constraints.

# 95. DCT-030 — Restore validates current constraints

Historical state is not automatically valid under current hierarchy, schema, authorization, or retention rules.

# 96. Privacy and retention

Documents may contain sensitive customer/user content.

Retention covers source content, history snapshots, search projections, exports, and backups.

# 97. Audit versus document history

Document history tracks content evolution; Governance audit tracks governed evidence.

# 98. DCT-031 — Document history is not Governance Audit

They can be produced by the same edit but have different purpose, retention, integrity, and user interaction.

# 99. Frontend editor implications

Editors preserve Block Type semantics, stable IDs, Workspace/Page scope, concurrency, ordering, authorization/read-only state, and unknown-type compatibility.

# 100. Mobile

Mobile may support a reduced editing set but must not corrupt unsupported Block Types.

# 101. Offline editing

If supported, offline editing requires operation identity, reconciliation, conflict handling, authorization recheck, and deleted/moved target behavior.

# 102. Accessibility

Block semantics should remain accessible: headings, lists, todos, code, images, focus, and keyboard behavior.

# 103. Current source alignment

Current Documents source contains:

```text
Blocks
Pages
ResourceLinks
Rules
Templates
Versions
```

Current Block behavior validates typed content, enforces Account/Workspace/Page parent scope, prevents tree cycles, uses fractional ordering, versions mutations, and emits create/change/move/delete/restore facts.

Current Page source separates archive from delete/restore, and visibility includes Private/Workspace/Public.

# 104. Current ambiguity watch

Do not normalize:

```text
PageVisibility.Public → transitive embed access
DocumentVersion → aggregate Version
raw JSON → schema-less extensibility
realtime connection → concurrency protocol
ResourceLink → target ownership
snapshot → editable current state
```

# 105. Change impact — Block Type

Review Domain validation, persistence, API/contracts, web/mobile, templates, snapshots, search, import/export, Automation, Integrations, and accessibility.

# 106. Change impact — hierarchy/ordering

Review Page tree, Block tree, fractional indexing, anchors, search paths, realtime, history, and editor behavior.

# 107. Change impact — visibility/sharing

Review Governance, embeds, search, realtime, public clients, export, and Collaboration.

# 108. Change impact — version/history

Review snapshot schema, storage, restore, migration, privacy/retention, conflict handling, and compatibility.

# 109. Change impact — Resource Link

Review target context, authorization, cross-Workspace policy, deletion/tombstone, search, rendering, and Automation.

# 110. Page checklist

```text
[ ] stable Page ID
[ ] one Workspace
[ ] hierarchy acyclic
[ ] lifecycle explicit
[ ] visibility != full authorization
[ ] Block tree scoped
[ ] version/conflict model
[ ] sharing/link semantics
[ ] deletion/retention
[ ] history behavior
```

# 111. Block-Type checklist

```text
[ ] semantic purpose
[ ] content schema
[ ] properties schema
[ ] normalization/validation
[ ] web/mobile behavior
[ ] accessibility
[ ] import/export
[ ] search
[ ] template compatibility
[ ] history migration
[ ] Automation/Integration impact
```

# 112. Collaboration boundary checklist

```text
[ ] comments belong to Collaboration
[ ] anchor semantics explicit
[ ] mentions/reactions separate
[ ] presence ephemeral
[ ] read/watch state separate
[ ] target authorization rechecked
[ ] deletion reaction defined
[ ] audit separate from history/activity
```

# 113. Testing/evidence

Critical evidence covers:

```text
Page create/rename/move/archive/delete/restore
Page visibility/hierarchy/cycles/scope
Block type/content validation
Block create/move/delete/restore
same Page/Workspace enforcement
fractional-order edge cases
semantic no-op
expected-version/conflict
ResourceLink scope/self/authorization
template instantiation
version/snapshot/restore
comment-anchor integration
realtime recovery
retention
persistence round-trip
```

# 114. Stop conditions

Stop rather than guess if:

- content becomes one opaque HTML string;
- Block JSON becomes schema-less;
- Blocks can move cross-Page/Workspace through raw IDs;
- presence is treated as content/concurrency truth;
- Page visibility bypasses Governance;
- sharing Page exposes private embedded resources;
- Documents directly mutates Work Management persistence;
- Collaboration data is embedded in Page aggregate;
- history and concurrency versions are conflated;
- deletion is only DB cascade;
- template changes rewrite existing Pages silently.

# 115. Related canonical owners

```text
PRODUCT.md
docs/product/product-model.md
docs/product/product-experience.md
docs/product/contexts/workspaces.md
docs/product/contexts/governance.md
docs/product/contexts/work-management.md
docs/product/contexts/collaboration.md
docs/product/contexts/automation.md
docs/product/contexts/integrations.md
docs/product/contexts/analytics.md
docs/architecture/data-ownership-and-consistency.md
docs/architecture/events-realtime-and-delivery-boundary.md
docs/architecture/contract-boundaries.md
frontend/docs/architecture/realtime.md
frontend/docs/architecture/state-query-mutations.md
```

# 116. Final Documents rule

For every Documents capability, answer:

```text
Which Page owns this content?
Which Workspace scopes it?
Which Block Type/schema applies?
What hierarchy/order invariant applies?
What is authoritative content?
What version/conflict protects edits?
Is this a link or embedded ownership?
Who authorizes the target?
What history/snapshot semantics apply?
Which Collaboration state is separate?
What happens on archive/delete/restore?
How does the client recover from realtime/offline divergence?
```

The target is:

> **a structured, typed, version-aware document model whose content remains authoritative in Documents while links, collaboration, realtime, search, and history preserve clear independent semantics.**

---
title: "Documents Context"
document_class: constitution
normative: true
owner: documents
maturity: FROZEN
conformance: CANONICAL
applies_to: documents
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Documents Context

## Mission

Documents owns structured pages, hierarchical organization and block-based document content. It provides knowledge/content capabilities that can link to Work Management without merging the two data models.

## Ubiquitous language

**Page**: durable document/container with workspace/parent/lifecycle metadata. **Block**: typed content node/element owned by a Page/document tree. **Block type**: content schema/behavior discriminator. **ResourceLink**: stable cross-resource relationship/embedding metadata, not ownership transfer.

## DOC-101 — Document content is structured Page + Block, not opaque HTML

The canonical content model is block-based. A block has a type and validated type-specific content/properties. Raw HTML may exist only as an explicitly sanitized/import/export representation, never as the only trusted document model.

## DOC-102 — Block content validates against block type

Paragraph/heading/todo/code/image/reference and future types define content/properties schema, normalization and evolution. Unknown discriminator/version handling is explicit. Arbitrary JSON accepted without schema validation is forbidden.

## DOC-103 — Hierarchy is tenant-safe and acyclic

Page/block parent changes validate same workspace/document ownership, no self/descendant cycles and any depth/path rule. Application supplies ancestor facts when required; Domain does not query the tree itself.

## DOC-104 — Content editing is concurrency/version aware

Block/page updates use expected version or the approved collaborative editing protocol. A stale write cannot silently overwrite newer durable content. Realtime cursor/presence is not a substitute for durable concurrency control.

## DOC-105 — Resource links preserve target ownership

Page/Block may reference Board/Item and other resources by stable ID/type/link contract. Reading/rendering a linked resource rechecks target authorization. A page share does not automatically expose a private Board unless an explicit Governance rule grants it.

## Ordering

Block sibling order uses the established deterministic ordering strategy and survives concurrent insert/move without float-midpoint corruption. Move validates source/target hierarchy and results in one coherent ordering mutation.

## Collaboration

Comments/mentions/reactions/activity around pages/blocks belong to Collaboration. Documents emits content/lifecycle facts needed by Collaboration/realtime/automation/indexing but does not store notification state inside Page.

## Lifecycle

Archive/delete/restore if supported must protect hierarchy, linked resources, revisions/history and retention. Archived/deleted content is not silently editable. Physical purge is separate from user-facing delete and follows retention/privacy policy.

## Forbidden designs

- one giant mutable HTML string as document source of truth;
- unvalidated block JSON;
- cross-workspace parent/link without explicit authorized cross-scope contract;
- collaboration comments/notifications embedded in document aggregate;
- assuming realtime means last-write-wins is acceptable;
- direct Work Management table access to resolve embeds.

## Testing/change impact

Cover block-type validation, hierarchy/cycle/move ordering, failure atomicity/version, link authorization, archive/delete guards, event/realtime semantics and persistence round-trip. New block types require frontend editor/renderer, import/export/search/indexing and migration compatibility decisions.

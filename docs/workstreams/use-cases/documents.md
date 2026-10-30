---
document_id: WRK-UC-DOCUMENTS
document_type: workstream-use-case-catalog
status: active
owner: documents-collaboration-team
applies_to:
  - documents
  - pages
  - blocks
  - document-versions
  - document-templates
  - use-case-catalog
  - capability-acceptance
evidence:
  - docs/workstreams/use-cases/README.md
  - docs/product/documents.md
  - docs/workstreams/teams/documents-collaboration.md
  - docs/workstreams/executions/documents-collaboration/documents-collaboration.spec.md
  - docs/workstreams/executions/documents-collaboration/documents-collaboration.tests.md
  - docs/workstreams/executions/documents-collaboration/documents-collaboration.certification.md
  - backend/src/Notrelix.API/Endpoints/Documents/
  - backend/src/Notrelix.Application/Features/Documents/
  - backend/src/Notrelix.Domain/Documents/
  - frontend/packages/product/docs/
  - frontend/e2e/real/documents.real.e2e.spec.ts
  - frontend/e2e/real/documents-trash-links.real.e2e.spec.ts
review_on:
  - page-lifecycle-change
  - block-type-change
  - hierarchy-or-ordering-change
  - document-versioning-change
  - document-template-change
  - capability-state-change
---

# Use cases: Documents

Product meaning: [documents](../../product/documents.md). Team: [documents-collaboration](../teams/documents-collaboration.md). Execution pack: [spec](../executions/documents-collaboration/documents-collaboration.spec.md), [tests](../executions/documents-collaboration/documents-collaboration.tests.md), [certification](../executions/documents-collaboration/documents-collaboration.certification.md). Permission and role meaning: [governance](../../product/governance.md). Comments, mentions delivery, attachments, presence and notifications are Collaboration use cases: see [collaboration.md](collaboration.md). Board items and the links from items to pages are Work Management use cases: see [work-management.md](work-management.md).

Id note: the product doc has rules `DCT-001` to `DCT-031`, and the team spec reuses `DCT-001` to `DCT-012` as capability numbers. In this file "product rule DCT-n" always means the product rule and "capability n" means the team capability. Requirement ids `DCREQ001` to `DCREQ191` are cited from the pack, not restated.

# 1. Assessment

## 1.1 Capability map

Letters follow the Documents list in the team spec (section 4): A Page lifecycle, B Page hierarchy, C Page move / reparent, D Block lifecycle, E Block types and content contract, F Block ordering, G Document query and loading, H Editor state reconciliation, I Document permissions, J Document events and activity, K Document realtime, L Recovery and conflict hardening. The product doc also covers versions and history, templates, import and export, resource links, search and mobile reading; they sit under the capability that owns their behaviour (versions under J and L, templates, import and export under A, search and links under G, mobile under H).

| Letter | Capability | Use cases | DEFINED | IMPLEMENTING | INTEGRATING | HARDENING | DONE | BLOCKED |
|---|---|---|---|---|---|---|---|---|
| A | Page lifecycle | 17 | 0 | 2 | 6 | 6 | 0 | 3 |
| B | Page hierarchy | 5 | 0 | 0 | 5 | 0 | 0 | 0 |
| C | Page move / reparent | 3 | 1 | 0 | 2 | 0 | 0 | 0 |
| D | Block lifecycle | 6 | 0 | 0 | 4 | 2 | 0 | 0 |
| E | Block types and content contract | 9 | 1 | 0 | 6 | 2 | 0 | 0 |
| F | Block ordering | 4 | 0 | 1 | 3 | 0 | 0 | 0 |
| G | Document query and loading | 7 | 2 | 0 | 3 | 2 | 0 | 0 |
| H | Editor state reconciliation | 8 | 1 | 0 | 6 | 1 | 0 | 0 |
| I | Document permissions | 7 | 2 | 0 | 3 | 2 | 0 | 0 |
| J | Document events and activity | 8 | 2 | 1 | 5 | 0 | 0 | 0 |
| K | Document realtime | 3 | 0 | 0 | 2 | 0 | 0 | 1 |
| L | Recovery and conflict hardening | 5 | 1 | 0 | 3 | 0 | 0 | 1 |
| | Total | 82 | 10 | 4 | 48 | 15 | 0 | 5 |

No use case is DONE: none has an owner check or a 1-px design diff. 15 use cases are HARDENING because a `frontend/e2e/real` spec drives them against the real backend: `documents.real.e2e.spec.ts` (create, write a block, rename, reload, share, guest sees it only after sharing) and `documents-trash-links.real.e2e.spec.ts` (trash, restore, delete for good, page reference and backlink). Everything else that has both backend and frontend code is INTEGRATING.

## 1.2 The ten most important gaps

1. Real end-to-end proof covers 15 of 82 use cases. Versions, restore, templates, import, export, move to space, search, stale-write rebase and mobile have only unit, component and mock e2e proof.
2. Trashing a page leaves its child pages active; the tree then shows them as roots. Subtree trash, restore and delete are undefined in code (UC-DCT-A08).
3. Moving a page under another parent cannot be done: the handler throws `NotImplementedException` and there is no route (UC-DCT-C02). The cycle rule is therefore unreachable (UC-DCT-B04).
4. Competing writes without an expected version: block reorder (UC-DCT-F02); page rename and single block delete are fixed (UC-DCT-A02, D04). Reorder also accepts client-made sort keys, which breaks "ordering is server-authoritative", and announces no page save, so reorders create no automatic version and no live refresh.
5. Rename is authorized with `ManageBoard`, a Work Management action, and three stubs do the same (UC-DCT-I02).
6. No bounds: block count per page, page count per list and tree, version list, trash list and backlinks are all unbounded reads (UC-DCT-G02, G06, J05).
7. No idempotency for creates (page, template, version, page from template) (UC-DCT-L04).
8. Page visibility Public, favorites and retention are undecided, so five use cases are BLOCKED (UC-DCT-A15, A16, A17, K03, L05); live co-editing needs an ADR (UC-DCT-K03).
9. Only three integration events exist (`PageCreated`, `PageArchived`, `PageBlocksSaved`); rename, move, restore, delete and block facts reach no consumer, so Automation, Analytics and page activity cannot be built (UC-DCT-J02, J08). The backend realtime frame for a saved page is not evidenced (UC-DCT-K01).
10. Import, duplicate and convert-to-task are multi-write client orchestrations that can leave an empty page or an unlinked line; offline editing, unknown block types and revoked-access behaviour have no UI (UC-DCT-A09, A13, D06, H04, E09, I07). The canvas has no artboard for the trash dialog, version history, templates, import and export dialogs, table and media blocks or the conflict notice.

## 1.3 Defects in shipped code

| # | Defect | Where |
|---|---|---|
| 1 | Fixed for rename: `UpdatePage` (owner decision 2026-10-30), expected version, dead request fields removed; the page icon is still UC-DCT-A03 | `backend/src/Notrelix.Application/Features/Documents/Pages/Commands/UpdatePage/UpdatePage.cs`, `backend/src/Notrelix.API/Endpoints/Documents/Pages/Commands/UpdatePageEndpoint.cs` |
| 2 | Dead handlers that throw `NotImplementedException` (the move handler is now built, UC-DCT-C02) | `.../PublishPage/PublishPage.cs`, `.../SetPageDeadline/SetPageDeadline.cs` (under `backend/src/Notrelix.Application/Features/Documents/`) |
| 3 | `GET /pages/{pageId}/history` always returns an empty list; the frontend still calls it | `.../Pages/Queries/GetPageHistory/GetPageHistory.cs`, `frontend/packages/product/docs/state/src/api/history.api.ts` |
| 4 | Reorder takes client sort keys, has no expected version, loads no page and announces no save | `.../Blocks/Commands/ReorderBlocks/ReorderBlocks.cs` |
| 5 | Fixed: single block delete takes the expected version and deletes the blocks under it, like the batch (UC-DCT-D04) | `.../Blocks/Commands/DeleteBlock/DeleteBlock.cs` |
| 6 | Fixed: trash, restore and permanent delete work on the subtree (UC-DCT-A08) | `.../Pages/Commands/ArchivePage/ArchivePage.cs`, `.../Pages/Queries/GetPageTree/GetPageTree.cs` |
| 7 | Fixed for create, edit, delete and batch: the handler refuses a trashed page (UC-DCT-D05); reorder follows with UC-DCT-F02 | `.../Blocks/Commands/*` |
| 8 | Unbounded reads | `.../Pages/Queries/GetWorkspacePages`, `GetPageTree`, `GetTrashedPages`, `GetPageBacklinks`, `.../Versions/Queries/ListPageVersions`, `.../Blocks/Queries/GetPageBlocks` |
| 9 | No idempotency on creates | `CreatePage`, `CreatePageFromTemplate`, `CreatePageTemplate`, `CreatePageVersion` handlers |
| 10 | `ResourceLink` aggregate, table and events have no command, query or route | `backend/src/Notrelix.Domain/Documents/ResourceLinks/`, `backend/src/Notrelix.Infrastructure/Data/Configurations/Documents/ResourceLinkConfiguration.cs` |
| 11 | Template aggregate can archive and publish, but no command or route exists; templates can only be created and listed | `backend/src/Notrelix.Domain/Documents/Templates/` |
| 12 | `PageCreated` and `PageArchived` consumers only log | `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Documents/DocumentsStubConsumers.cs` |
| 13 | Frontend: favorite toggle is a no-op that logs a warning; permission read falls back to a `/pages/{id}/permissions` route that does not exist | `frontend/packages/product/docs/state/src/api/favorite.api.ts`, `.../api/page-extras.api.ts` |
| 14 | Frontend: import, duplicate and convert-to-task are several writes; a failure part-way leaves an empty page or an unlinked line | `frontend/packages/product/docs/state/src/api/import.api.ts`, `.../api/page-extras.api.ts`, `onDuplicatePage` in `frontend/packages/product/docs/web/src/components/doc-page-screen.tsx` |

Mock-only frontend paths: none of the Docs screens depends on a mock-only endpoint except the dead calls in defect 13; the mock e2e specs (`docs-page`, `documents`, `design-fidelity-docs`, `real-pages-detail-docs-command`) run against mock handlers and do not prove the backend.

## 1.4 Enterprise baseline

Compared with Notion, Confluence and Google Docs (public help pages; features beyond what these pages state were not verified):

| Capability | Leaders | Notrelix today |
|---|---|---|
| Export a page as PDF, HTML or Markdown; with subpages; whole workspace in the background ([Notion: export your content](https://www.notion.com/help/export-your-content)) | yes | Page export in the browser (Markdown, HTML, print to PDF); subtree and workspace export missing (UC-DCT-A14) |
| Page history listing every saved version, compare two versions, restore (Confluence: [view page information](https://confluence.atlassian.com/doc/view-page-information-139408.html)) | yes | List, view, restore with a guard; compare missing (UC-DCT-J07) |
| Named versions that can be filtered (Google Docs: [version history essentials](https://www.techrepublic.com/article/version-history-essentials-for-google-docs-sheets-and-slides/)) | yes | Named and automatic entries exist; no filter, rename or delete of a named entry |
| Page-level restrictions shown on the page (Confluence page information) | yes | Share dialog through Governance (UC-DCT-I03) |
| Templates managed by an owner | yes | Create and use only (UC-DCT-A12) |
| Search inside page text | yes | Title only (UC-DCT-G07) |
| Live co-editing | yes | Not offered; block versions arbitrate (UC-DCT-K03) |

Table-stakes use cases added from this comparison: UC-DCT-A12, A14 (subtree and workspace part), G06, G07, J07.

# 2. Index

Capability letters: see section 1.1. Columns BE and FE say whether code exists: `yes` (route or screen exists and is used), `partial` (part exists or is a stub), `none`. Design names the canvas artboard (the canvas is [claude.ai artifact 18L4jUXGfdXh7Wn9YQvA2x](https://claude.ai/artifact/18L4jUXGfdXh7Wn9YQvA2x)); `none` means no artboard draws it. The Docs artboards are Final-Docs-2, Final-Docs-2-Dem (night), Final-Docs-2-320, Final-Docs-2-320-Sheet, Final-Docs-2-Notes (measures), Final-M-Docs, Final-Share, Final-M-Share and Final-Command; Final-Docs is superseded (decision 75). Real e2e is `real` when a `frontend/e2e/real` spec drives the use case.

| ID | Use case | Capability | Actors | BE | FE | Design | Real e2e | State | Gap |
|---|---|---|---|---|---|---|---|---|---|
| UC-DCT-A01 | Create a page | A | Editor of the parent or space, workspace member | yes | yes | Final-Docs-2 | real | HARDENING | Tests and owner check |
| UC-DCT-A02 | Rename a page | A | Editor | partial | yes | Final-Docs-2 | real | HARDENING | Tests and owner check |
| UC-DCT-A03 | Set a page icon | A | Editor | partial | partial | Final-Docs-2 | none | IMPLEMENTING | API drops icon; no endpoint field |
| UC-DCT-A04 | Move a page to the trash | A | Editor | yes | yes | Final-Docs-2 | real | HARDENING | Tests and owner check |
| UC-DCT-A05 | Restore a page from the trash | A | Editor | yes | yes | Final-Docs-2 | real | HARDENING | Tests and owner check |
| UC-DCT-A06 | Delete a trashed page permanently | A | Manager | yes | yes | Final-Docs-2 | real | HARDENING | Tests and owner check |
| UC-DCT-A07 | Browse the trash | A | Viewer of a trashed page | yes | yes | none | real | HARDENING | Tests and owner check |
| UC-DCT-A08 | Trash or delete a page that has children | A | Editor, Manager | yes | yes | none | none | INTEGRATING | Tests and owner check |
| UC-DCT-A09 | Duplicate a page | A | Editor | partial | yes | none | none | INTEGRATING | Tests and owner check |
| UC-DCT-A10 | Save a page as a template | A | Editor | yes | yes | none | none | INTEGRATING | Tests and owner check |
| UC-DCT-A11 | Create a page from a template | A | Editor of the target | yes | yes | none | none | INTEGRATING | Tests and owner check |
| UC-DCT-A12 | Rename, archive or delete a template (enterprise baseline) | A | Template creator, Workspace Admin | none | none | none | none | IMPLEMENTING | Domain archive only; no command or endpoint |
| UC-DCT-A13 | Import a file as a new page | A | Editor of the target | partial | yes | none | none | INTEGRATING | Tests and owner check |
| UC-DCT-A14 | Export a page, a subtree or a workspace | A | Viewer; Admin for workspace | none | partial | none | none | INTEGRATING | Page export client-side only; subtree and workspace not built |
| UC-DCT-A15 | Set page visibility | A | Manager | partial | none | none | none | BLOCKED | Public needs the anonymous principal decision |
| UC-DCT-A16 | Favorite a page | A | Any viewer | none | partial | none | none | BLOCKED | Owner context unknown; FE call is a no-op |
| UC-DCT-A17 | Retain and purge deleted pages | A | System | none | none | none | none | BLOCKED | Retention window not decided |
| UC-DCT-B01 | Show the page tree | B | Workspace member | yes | yes | Final-Docs-2 | none | INTEGRATING | Tests and owner check |
| UC-DCT-B02 | Create a child page | B | Editor of the parent | yes | yes | Final-Docs-2 | none | INTEGRATING | Tests and owner check |
| UC-DCT-B03 | Show the breadcrumb | B | Viewer | yes | yes | Final-Docs-2 | none | INTEGRATING | Tests and owner check |
| UC-DCT-B04 | Refuse a cycle or a foreign parent | B | System | yes | partial | none | none | INTEGRATING | Tests and owner check |
| UC-DCT-B05 | Limit nesting depth | B | System | yes | partial | none | none | INTEGRATING | Tests and owner check |
| UC-DCT-C01 | Move a root page and its subtree to a space | C | Editor of the page and of the space | yes | yes | Final-Docs-2 | none | INTEGRATING | Tests and owner check |
| UC-DCT-C02 | Move a page under another parent | C | Editor of the page, Editor of the new parent | yes | yes | none | none | INTEGRATING | Tests and owner check |
| UC-DCT-C03 | Refuse a move to another workspace | C | System | none | none | none | none | DEFINED | No explicit rejection test |
| UC-DCT-D01 | Add a block | D | Editor | yes | yes | Final-Docs-2 | real | HARDENING | Tests and owner check |
| UC-DCT-D02 | Edit a block | D | Editor | yes | yes | Final-Docs-2 | real | HARDENING | Tests and owner check |
| UC-DCT-D03 | Save several blocks as one change | D | Editor | yes | yes | Final-Docs-2 | none | INTEGRATING | Tests and owner check |
| UC-DCT-D04 | Delete a block | D | Editor | yes | yes | Final-Docs-2 | none | INTEGRATING | Tests and owner check |
| UC-DCT-D05 | Refuse edits to a trashed page | D | System | yes | partial | none | none | INTEGRATING | Tests and owner check |
| UC-DCT-D06 | Convert a checklist line to a task | D | Editor with item-create right | partial | yes | Final-Docs-2 | none | INTEGRATING | Tests and owner check |
| UC-DCT-E01 | Write rich text with marks | E | Editor | yes | yes | Final-Docs-2 | real | HARDENING | Tests and owner check |
| UC-DCT-E02 | Change a block's type | E | Editor | yes | yes | Final-Docs-2 | none | INTEGRATING | Tests and owner check |
| UC-DCT-E03 | Use structured blocks | E | Editor | yes | yes | Final-Docs-2 | none | INTEGRATING | Tests and owner check |
| UC-DCT-E04 | Use a table block | E | Editor | yes | yes | none | none | INTEGRATING | Tests, artboard and owner check |
| UC-DCT-E05 | Add an image, file or video block | E | Editor | yes | yes | none | none | INTEGRATING | Tests, artboard and owner check |
| UC-DCT-E06 | Embed a board in a page | E | Editor | yes | yes | none | none | INTEGRATING | Tests, artboard and owner check |
| UC-DCT-E07 | Reference another page | E | Editor | yes | yes | Final-Docs-2 | real | HARDENING | Tests and owner check |
| UC-DCT-E08 | Mention a person in a page | E | Editor | yes | yes | Final-Docs-2 | none | INTEGRATING | Tests and owner check |
| UC-DCT-E09 | Open a block type this client does not know | E | Any viewer | partial | partial | none | none | DEFINED | No placeholder behaviour |
| UC-DCT-F01 | Insert a block between two blocks | F | Editor | yes | yes | Final-Docs-2 | none | INTEGRATING | No real e2e |
| UC-DCT-F02 | Reorder blocks | F | Editor | partial | yes | none | none | INTEGRATING | Client sets keys; no version; no page fact |
| UC-DCT-F03 | Nest or un-nest a block | F | Editor | yes | yes | none | none | INTEGRATING | No real e2e |
| UC-DCT-F04 | Insert many blocks densely | F | Editor | yes | none | none | none | IMPLEMENTING | Rebalance undefined |
| UC-DCT-G01 | Open a page | G | Viewer | yes | yes | Final-Docs-2 | real | HARDENING | Block list unbounded |
| UC-DCT-G02 | List the pages of a workspace | G | Workspace member | yes | yes | none | none | INTEGRATING | Unbounded |
| UC-DCT-G03 | Search pages by title | G | Workspace member | yes | yes | Final-Command | none | INTEGRATING | No real e2e |
| UC-DCT-G04 | Read the pages that reference this page | G | Viewer | yes | yes | Final-Docs-2 | real | HARDENING | Unbounded |
| UC-DCT-G05 | Show the tasks and boards linked to a page | G | Viewer | partial | yes | Final-Docs-2 | none | INTEGRATING | Owned by Work Management |
| UC-DCT-G06 | Load a very large page (enterprise baseline) | G | Viewer | none | none | none | none | DEFINED | No block limit or windowing |
| UC-DCT-G07 | Search inside page text (enterprise baseline) | G | Workspace member | none | none | none | none | DEFINED | Title search only |
| UC-DCT-H01 | Autosave edits and show the saved state | H | Editor | yes | yes | Final-Docs-2 | real | HARDENING | Offline state not proven |
| UC-DCT-H02 | Rebase after a stale save | H | Editor | yes | yes | none | none | INTEGRATING | No artboard; no real e2e |
| UC-DCT-H03 | Roll back a failed optimistic write | H | Editor | yes | yes | none | none | INTEGRATING | No real e2e |
| UC-DCT-H04 | Edit while offline | H | Editor | none | none | none | none | DEFINED | Not built |
| UC-DCT-H05 | Find and replace in a page | H | Editor | none | yes | none | none | INTEGRATING | FE only; no artboard |
| UC-DCT-H06 | Read a page on a phone app | H | Viewer | yes | yes | Final-M-Docs | none | INTEGRATING | Read-only; no device e2e |
| UC-DCT-H07 | Use the 320 px page and its overflow sheet | H | Viewer, Editor | yes | yes | Final-Docs-2-320 | none | INTEGRATING | 1-px diff not run |
| UC-DCT-H08 | Edit with keyboard and screen reader | H | Editor | none | yes | none | none | INTEGRATING | No axe run on real stack |
| UC-DCT-I01 | See a page only when allowed | I | Viewer | yes | yes | Final-Docs-2 | real | HARDENING | No cross-tenant negative test |
| UC-DCT-I02 | Edit only with edit permission | I | Editor | partial | yes | none | none | INTEGRATING | Rename uses ManageBoard |
| UC-DCT-I03 | Share a page with a member | I | Manager | yes | yes | Final-Share | real | HARDENING | Owned by Governance |
| UC-DCT-I04 | Creator becomes Manager | I | Creator | yes | none | none | none | INTEGRATING | No real e2e |
| UC-DCT-I05 | Embedding a board never shares it | I | System | partial | partial | none | none | DEFINED | No proof |
| UC-DCT-I06 | Read-only and trashed page states | I | Viewer | partial | yes | Final-Docs-2 | none | INTEGRATING | Trashed-page edit not proven |
| UC-DCT-I07 | Lose access while the page is open | I | Editor | none | none | none | none | DEFINED | Behaviour not built |
| UC-DCT-J01 | Announce a saved or created page | J | System | yes | none | none | none | INTEGRATING | Only three integration events |
| UC-DCT-J02 | Announce rename, move, restore and delete | J | System | partial | none | none | none | DEFINED | Domain events have no integration event |
| UC-DCT-J03 | Capture an automatic version | J | System | yes | yes | none | none | INTEGRATING | No last-editor-leaves rule |
| UC-DCT-J04 | Save a named version | J | Editor | yes | yes | none | none | INTEGRATING | No real e2e |
| UC-DCT-J05 | Browse the version history | J | Viewer | yes | yes | none | none | INTEGRATING | Unbounded list |
| UC-DCT-J06 | View a version | J | Viewer | yes | yes | none | none | INTEGRATING | No real e2e |
| UC-DCT-J07 | Compare two versions (enterprise baseline) | J | Viewer | none | none | none | none | DEFINED | Not built |
| UC-DCT-J08 | Read page activity | J | Viewer | partial | partial | none | none | IMPLEMENTING | History endpoint always empty |
| UC-DCT-K01 | See other people's changes on an open page | K | Viewer | partial | yes | none | none | INTEGRATING | Backend frame producer not proven |
| UC-DCT-K02 | Recover after a dropped connection | K | Viewer | partial | yes | none | none | INTEGRATING | No real e2e |
| UC-DCT-K03 | Edit together with live cursors | K | Editor | none | none | none | none | BLOCKED | Needs an ADR |
| UC-DCT-L01 | Restore a version with the guard | L | Editor | yes | yes | none | none | INTEGRATING | No real e2e |
| UC-DCT-L02 | Refuse a stale block write | L | Editor | yes | yes | none | none | INTEGRATING | No real e2e |
| UC-DCT-L03 | Apply a batch completely or not at all | L | Editor | yes | yes | none | none | INTEGRATING | Atomicity relies on session transaction; no real e2e |
| UC-DCT-L04 | Retry a create without a second effect | L | Editor | partial | none | none | none | DEFINED | Page create has no key; block replay returns 409 |
| UC-DCT-L05 | Retain and delete the dependents of a page | L | System | none | none | none | none | BLOCKED | Retention windows not decided |

## 2.1 Endpoint coverage

Every route under `backend/src/Notrelix.API/Endpoints/Documents/` appears in at least one use case.

| Route | Use cases |
|---|---|
| `GET /api/v1/workspaces/{workspaceId}/pages` | UC-DCT-G02 |
| `GET /api/v1/workspaces/{workspaceId}/pages/tree` | UC-DCT-B01 |
| `GET /api/v1/workspaces/{workspaceId}/pages/search` | UC-DCT-G03 |
| `GET /api/v1/workspaces/{workspaceId}/pages/trash` | UC-DCT-A07 |
| `POST /api/v1/workspaces/{workspaceId}/pages` | UC-DCT-A01, B02, A09, A13 |
| `GET /api/v1/pages/{pageId}` | UC-DCT-G01, I01, I06 |
| `PATCH /api/v1/pages/{pageId}` | UC-DCT-A02, A03 |
| `POST /api/v1/pages/{pageId}/archive` | UC-DCT-A04, A08 |
| `POST /api/v1/pages/{pageId}/restore` | UC-DCT-A05 |
| `DELETE /api/v1/pages/{pageId}` | UC-DCT-A06, A08 |
| `GET /api/v1/pages/{pageId}/breadcrumb` | UC-DCT-B03 |
| `GET /api/v1/pages/{pageId}/history` | UC-DCT-J08 |
| `GET /api/v1/pages/{pageId}/backlinks` | UC-DCT-G04, E07 |
| `POST /api/v1/pages/{pageId}/move` | UC-DCT-C02, B04, B05 |
| `POST /api/v1/pages/{pageId}/move-to-space` | UC-DCT-C01, C03 |
| `GET /api/v1/pages/{pageId}/blocks` | UC-DCT-G01, G06 |
| `POST /api/v1/pages/{pageId}/blocks` | UC-DCT-D01, F01 |
| `POST /api/v1/pages/{pageId}/blocks/batch` | UC-DCT-D03, L03 |
| `PATCH /api/v1/pages/{pageId}/blocks/{blockId}` | UC-DCT-D02, E01, E02, L02 |
| `DELETE /api/v1/pages/{pageId}/blocks/{blockId}` | UC-DCT-D04 |
| `POST /api/v1/blocks/reorder` | UC-DCT-F02, F03 |
| `GET /api/v1/pages/{pageId}/versions` | UC-DCT-J05 |
| `GET /api/v1/pages/{pageId}/versions/{versionId}` | UC-DCT-J06 |
| `POST /api/v1/pages/{pageId}/versions` | UC-DCT-J04 |
| `POST /api/v1/pages/{pageId}/versions/{versionId}/restore` | UC-DCT-L01 |
| `GET /api/v1/workspaces/{workspaceId}/page-templates` | UC-DCT-A11 |
| `POST /api/v1/workspaces/{workspaceId}/page-templates` | UC-DCT-A10 |
| `POST /api/v1/workspaces/{workspaceId}/page-templates/{templateId}/pages` | UC-DCT-A11 |

Endpoints with no use case: none.

# 3. Use cases

## Capability A: Page lifecycle

Team capability 1 (team id DCT-001). Requirements `DCREQ001` to `DCREQ005`, `DCREQ113` to `DCREQ116`, `DCREQ135`, `DCREQ138`, `DCREQ139`, `DCREQ142` to `DCREQ146`.

### UC-DCT-A01 Create a page
- Capability: A Page lifecycle
- Actors and permission: a workspace member. `CreatePage` on the parent page when a parent is named, else on the Space, else on the workspace.
- Preconditions: the workspace is active; the parent is an active page of the same workspace; the Space is active.
- Input: `title` (1 to 500 characters after trim), optional `parentId`, optional `spaceId`. A parent decides the Space; `spaceId` is ignored when a parent is named.
- Output: a new page with a new id, version 1, status Active, in the caller's workspace and account; the creator is granted Manager on it; `PageCreated` is announced. The response is the new page id with `201`.
- Main flow: 1. The person chooses New page in the sidebar, a Space, or under a page. 2. The client posts the title. 3. The server authorizes against the placement, creates the page and the Manager grant in one unit of work. 4. The client opens the new page and focuses the title.
- No-op / rejection / concurrency / retry: an empty or over-long title is `400`; a missing, archived or foreign parent is `404`; a person without the right gets the same `404` as for a missing page; an archived Space is refused. Create is never a no-op. A retry of the same request creates a second page today (see UC-DCT-L04).
- UI states: loading (creating), validation error (title), permission denied (hidden New page), pending, conflict not applicable, offline (create disabled with a notice).
- Rules: product doc sections 7 to 9 (product rules DCT-001, DCT-002, DCT-005); GOV-028 placement authorization.
- Acceptance: Given an Editor of a Space, When they create a page in it, Then the page exists in that Space and they are its Manager. Given a viewer of the Space, When they post a create, Then the answer is `404` and nothing is stored. Given a parent in another workspace, When posted, Then `404`.
- Evidence: BE `POST /api/v1/workspaces/{workspaceId}/pages` (requires `Idempotency-Key`; `[IdempotencyOperation("documents.pages.create-page.v1")]` on `CreatePageCommand`, replay for 24 hours), `CreatePageCommandHandler` · FE `useCreatePage`, `DocPageScreen`, `useCreateResource` (one key per creation attempt, reset when the name changes) · Design Final-Docs-2 · Tests `CreatePageTargetPolicyMatrixTests`, `PageTests`, `PageCreatedOutboxEvidenceTests` · Real e2e `documents.real.e2e.spec.ts`
- State: HARDENING — tests and owner check (replay with one key and a conflicting body, cross-tenant negative test; the real e2e helpers that post a page directly must send `Idempotency-Key`).

### UC-DCT-A02 Rename a page
- Capability: A Page lifecycle
- Actors and permission: Editor of the page. Target permission `UpdatePage`; shipped code asks for `ManageBoard` (defect, see Part 1).
- Preconditions: the page is active (not in the trash).
- Input: `title` (1 to 500 characters after trim), `expectedVersion` (the page version the editor read, lead decision LD6).
- Output: the page title changes, page version +1, `PageRenamed` raised; breadcrumb, tree and search results show the new title after refetch.
- Main flow: 1. The person edits the page title in place. 2. On blur or Enter the client patches the title with the version it read. 3. The server renames and returns. 4. The client updates the tree and breadcrumb.
- No-op / rejection / concurrency / retry: the same title (after trim) is a semantic no-op: no version bump, no event. A trashed page is refused (`Documents_Page_CannotRenameArchived`). A stale `expectedVersion` is `412` with the current page; the client reloads the title and tells the person. Retry with the same body after success is a no-op.
- UI states: loading, validation error, read-only, conflict, pending, permission denied, offline.
- Rules: product doc section 9 and product rule DCT-025; `DCREQ113`, `DCREQ136`, `DCREQ137`.
- Acceptance: Given a title change, When patched, Then version +1 and one `PageRenamed`. Given the same title, Then no version change. Given a stale version, Then `412` and the title is unchanged. Given a Viewer, Then `404`.
- Evidence: BE `PATCH /api/v1/pages/{pageId}` (body `title`, `expectedVersion`; the dead `iconType`, `iconValue`, `coverUrl` fields are gone), `UpdatePageCommand` is authorized as `UpdatePage` and adds `IExpectedVersionRequest` (registered in `ExpectedVersionTargetMap`), `UpdatePageCommandHandler` · FE `useDocEditor.updateTitle` (sends the cached page version; on 412 it reads the page, treats an already applied rename as success, otherwise shows the stored title and the notice `header.titleConflict`) · Design Final-Docs-2 · Tests `PageTests`, `PageRulesTests`, `ExpectedVersionArchitectureTests` · Real e2e `documents.real.e2e.spec.ts`
- State: HARDENING — tests and owner check (stale-write test, no-op test, Viewer `404`, real e2e for the 412; the handler tests and `GovernanceResourcePermissionFlowTests` still build the old `UpdatePageCommand(pageId, title)`). Rename authorized as UpdatePage (owner decision 2026-10-30).

### UC-DCT-A03 Set a page icon
- Capability: A Page lifecycle
- Actors and permission: Editor of the page (`UpdatePage`).
- Preconditions: the page is active.
- Input: `icon` (one emoji or icon token, at most 32 characters; empty clears it), `expectedVersion`. Cover images are deferred (lead decision LD8).
- Output: icon stored on the page, version +1, shown in tree, breadcrumb, backlinks and page head.
- Main flow: 1. The person opens the icon picker on the page head. 2. The client patches `icon`. 3. Tree and head update.
- No-op / rejection / concurrency / retry: the same icon is a no-op; unknown or over-long token is `400`; stale version is `412`; trashed page refused.
- UI states: loading, validation error, read-only, conflict, pending.
- Rules: product doc section 7 (Page title and metadata); `DCREQ113`.
- Acceptance: Given an Editor, When they pick an icon, Then the tree and head show it after reload. Given a Viewer, Then no picker is offered.
- Evidence: BE `PATCH /api/v1/pages/{pageId}` (request has `icon` and `cover` fields that the endpoint does not forward), `Page.Icon` · FE `UpdatePagePayload` · Design Final-Docs-2 (icon shown, picker not drawn) · Tests none · Real e2e none
- State: IMPLEMENTING — the API ignores the icon; no setter on the aggregate; no picker artboard.

### UC-DCT-A04 Move a page to the trash
- Capability: A Page lifecycle
- Actors and permission: Editor of the page (`ArchivePage`).
- Preconditions: the page is active.
- Input: none (page id in the route).
- Output: status Archived, version +1, `PageArchived` announced; the page leaves the tree, lists, search and page reads; it appears in the trash list.
- Main flow: 1. File menu, Move to trash. 2. The client posts archive. 3. The page disappears from the sidebar and the client opens the workspace docs home.
- No-op / rejection / concurrency / retry: an already trashed page is a no-op (no version, no event). A person without the right gets `404`. Retry is safe.
- UI states: pending, permission denied, offline, destructive confirmation not required (reversible), undo toast with Restore.
- Rules: product doc sections 10, 11 (product rule DCT-003); `DCREQ004`; decision LD1 for children.
- Acceptance: Given an active page, When trashed, Then `GET /pages/{id}` is `404` and the trash list holds it. Given it is trashed again, Then no second event.
- Evidence: BE `POST /api/v1/pages/{pageId}/archive`, `ArchivePageCommandHandler` archives the page and every active descendant with one archive time in one unit of work through `IPageSubtreeStore`; `IPageSubtreeAuthorizer` first evaluates `ArchivePage` for every descendant it would change and one refused page refuses the whole operation (`403` `documents.page.subtree-not-permitted`, generic message, no count); an already trashed page is a no-op · FE `useArchivePage`, File menu, `pagesBelow` count, `DocTrashConfirmDialogSurface` when pages sit under the page, toast with Undo (`host.notify` action restores and reopens the page) · Design Final-Docs-2 (confirmation and toast not drawn) · Tests `ArchivePageCommandHandlerTests`, `PageTrashHandlerTests`, `PageArchivedOutboxEvidenceTests` · Real e2e `documents-trash-links.real.e2e.spec.ts`
- State: HARDENING — tests and owner check (subtree trash, refusal when one descendant is not allowed, no-op, cross-tenant negative test; the handler tests still construct the handler with `IDocumentDbContext`; the subtree check costs one access-facts lookup per descendant).

### UC-DCT-A05 Restore a page from the trash
- Capability: A Page lifecycle
- Actors and permission: Editor of the page (`ArchivePage`, same right as trashing).
- Preconditions: the page is in the trash and not deleted for good.
- Input: none.
- Output: status Active, version +1, `PageRestored`; the page is back in the tree under its parent (or at the root when the parent is trashed).
- Main flow: 1. File menu, Open trash. 2. The person chooses the page. 3. The client posts restore and closes the dialog. 4. The page is readable again.
- No-op / rejection / concurrency / retry: restoring an active page is a no-op. A parent that is trashed or gone: the page is restored at the root of its Space (lead decision LD1). Retry is safe.
- UI states: loading, empty trash, pending, permission denied, offline.
- Rules: product doc sections 94, 95 (product rule DCT-030: restore validates current constraints).
- Acceptance: Given a trashed page, When restored, Then `GET /pages/{id}` is `200` and the trash list no longer holds it.
- Evidence: BE `POST /api/v1/pages/{pageId}/restore`, `RestorePageCommandHandler` restores the page and the archived pages that went to the trash with it (same archive time as the page) in one unit of work, after `IPageSubtreeAuthorizer` evaluated `RestorePage` for each of them; a parent that is trashed, deleted or missing brings the page back at the root of its Space (`Page.Unarchive(detachFromParent)`), an archived Space is refused with `409` `workspaces.space.inactive`; an active page is a no-op · FE `useRestorePage`, trash dialog · Design Final-Docs-2 (trash dialog not drawn) · Tests `PageTrashHandlerTests`, `PageTrashTests` · Real e2e `documents-trash-links.real.e2e.spec.ts`
- State: HARDENING — tests and owner check (subtree restore, a page trashed on its own earlier stays in the trash, detach to root, archived Space, refusal when one page is not allowed, no-op; trash dialog artboard; moving a root to a Space rewrites the archive time of archived pages below it, which then stay in the trash on restore).

### UC-DCT-A06 Delete a trashed page permanently
- Capability: A Page lifecycle
- Actors and permission: Manager of the page (`DeletePage`).
- Preconditions: the page is in the trash.
- Input: none; the client asks for a second confirmation.
- Output: the page is deleted: reads answer `404`, it leaves the trash list, `PageDeleted` is raised. Rows are marked deleted; physical purge is a separate retention step (UC-DCT-A17).
- Main flow: 1. Trash dialog, Delete permanently. 2. Confirm. 3. The page is gone from the list.
- No-op / rejection / concurrency / retry: an active page is refused (`Documents_Page_NotInTrash`, `400`-class business rule). A repeat on a deleted page is `404`. Retry is safe.
- UI states: destructive confirmation, pending, permission denied, offline.
- Rules: product doc sections 11, 89, 90 (product rules DCT-003, DCT-028); `DCREQ004`.
- Acceptance: Given a trashed page and a Manager, When deleted, Then `GET` is `404`. Given an active page, Then refused and unchanged. Given an Editor who is not Manager, Then `404`.
- Evidence: BE `DELETE /api/v1/pages/{pageId}`, `DeletePageCommandHandler` deletes the page and the pages that went to the trash with it (same archive time) in one unit of work, after `IPageSubtreeAuthorizer` evaluated `DeletePage` for each of them (an active page or a page trashed on its own below it is left alone) · FE `useDeletePage`, trash dialog (note names the subtree) · Design Final-Docs-2 (confirm step not drawn) · Tests `PageTrashHandlerTests`, `PageTrashTests` · Real e2e `documents-trash-links.real.e2e.spec.ts`
- State: HARDENING — tests and owner check (dependents (versions, links, comments, attachments, search) still wait for the retention decision, UC-DCT-A17 and L05; confirm artboard).

### UC-DCT-A07 Browse the trash
- Capability: A Page lifecycle
- Actors and permission: any person who could view the page before it was trashed (`ViewPage` evaluated on the trashed page); the route needs `ViewWorkspace`.
- Preconditions: none.
- Input: none.
- Output: trashed pages the caller may see, newest change first, each with title, Space and trashed time.
- Main flow: 1. File menu, Open trash. 2. The dialog lists the pages.
- No-op / rejection / concurrency / retry: pages the caller may not see are absent, never counted. The list is not paged today.
- UI states: loading, empty ("Trash is empty"), permission denied, offline, error with retry.
- Rules: product doc section 10; `DCREQ025`.
- Acceptance: Given two trashed pages and a person who can see one, When listing, Then one row.
- Evidence: BE `GET /api/v1/workspaces/{workspaceId}/pages/trash?cursor&limit` answers a `CursorPage<PageDto>` (default 50, at most 200, keyset over last change and id), `GetTrashedPagesQueryHandler` · FE `useTrashedPages` (infinite query), trash dialog with Load more and a list-failed state with retry · Design none · Tests `PageTrashHandlerTests` · Real e2e `documents-trash-links.real.e2e.spec.ts`
- State: HARDENING — tests and owner check (paging, visibility filter with a cursor, invalid cursor `400`; artboard).

### UC-DCT-A08 Trash or delete a page that has children
- Capability: A Page lifecycle
- Actors and permission: Editor (trash), Manager (delete) of the page being acted on.
- Preconditions: the page has child pages.
- Input: as UC-DCT-A04 and UC-DCT-A06.
- Output (lead decision LD1): the whole subtree goes to the trash as one unit, each child keeps its own id and place; restoring the parent restores the subtree; deleting the parent permanently deletes the subtree. A child can be restored alone only when its parent is active.
- Main flow: 1. Trash a page with children. 2. The confirmation (trash) names the count ("and 4 pages under it"). 3. The server marks the page and descendants archived in one unit of work. 4. Restore reverses it.
- No-op / rejection / concurrency / retry: the caller needs the right on the page acted on; a descendant the caller cannot see is still trashed with it and is not named. A page created under it meanwhile is included. Retry is safe.
- UI states: destructive confirmation with count, pending, permission denied.
- Rules: product doc sections 89, 90 (product rule DCT-028: lifecycle workflow, not ORM cascade); `DCREQ004`, `DCREQ006`. New rule in Part 5.
- Acceptance: Given a page with two children, When trashed, Then neither child is in the tree or readable. When the parent is restored, Then both are back.
- Evidence: BE `ArchivePageCommandHandler`, `RestorePageCommandHandler`, `DeletePageCommandHandler` apply the subtree through `IPageSubtreeStore` and `IPageSubtreeAuthorizer` · FE confirmation with the count of visible active pages below (`pagesBelow`, the set the trash changes), undo toast, trash note, the refusal message · Design none · Tests none · Real e2e none
- State: INTEGRATING — tests and owner check (subtree trash, restore and delete; descendants the caller cannot see; confirmation artboard).

### UC-DCT-A09 Duplicate a page
- Capability: A Page lifecycle
- Actors and permission: Editor of the page (to read all content and create beside it).
- Preconditions: the page is active.
- Input: none; the title becomes "Copy of <title>" (lead decision LD7).
- Output: a new page next to the original with new page and block ids, same Space and parent; text, table and embed blocks copied; images, files and videos not copied (as UC-DCT-A11); comments, versions, attachments and permissions not copied; page references and board embeds still point at their original targets; the caller is Manager of the copy.
- Main flow: 1. File menu, Duplicate. 2. The client creates the page, then saves the blocks in one batch. 3. The copy opens.
- No-op / rejection / concurrency / retry: if the block batch fails after the page exists, the client trashes the empty copy and says so (today it leaves it). A notice names how many blocks were left out.
- UI states: pending, notice for dropped blocks, error, permission denied.
- Rules: product doc section 85 (resource links in a copy), product rule DCT-018 analogue; `DCREQ145`; decisions 17.
- Acceptance: Given a page with a table and an image, When duplicated, Then the copy has the table, no image, a notice, and different block ids.
- Evidence: BE `POST /api/v1/workspaces/{workspaceId}/pages`, `POST /api/v1/pages/{pageId}/blocks/batch` · FE `onDuplicatePage` in `DocPageScreen` (leaves image and file blocks behind, toast `makeCopyDropped` names how many), `useImportPage` (trashes the half-made copy when a block write fails) · Design none · Tests `import-page.unit.test.ts` · Real e2e none
- State: INTEGRATING — tests and owner check (the copy is still two client calls, cleaned up on failure; the client has no video block yet; real e2e).

### UC-DCT-A10 Save a page as a template
- Capability: A Page lifecycle
- Actors and permission: Editor of the source page (`UpdatePage`), decision 14.
- Preconditions: the page is active, in the caller's workspace.
- Input: `pageId`, `name`, optional `description`.
- Output: a Published workspace template holding a snapshot of the title, icon and blocks as the page is now; visible to every workspace member in the template list.
- Main flow: 1. File menu, Save as template. 2. Name it. 3. Confirm; a toast says it is saved.
- No-op / rejection / concurrency / retry: empty name is `400`; a page of another workspace is `404`. A second save makes a second template (names need not be unique). Later edits of the page never change the template (product rule DCT-018).
- UI states: validation error, pending, permission denied, offline.
- Rules: product doc sections 53 to 57; `DCREQ135`, `DCREQ138`, `DCREQ139`.
- Acceptance: Given an Editor, When they save, Then the template is listed with the snapshot taken at that time.
- Evidence: BE `POST /api/v1/workspaces/{workspaceId}/page-templates` (requires `Idempotency-Key`, `documents.templates.create-page-template.v1`), `CreatePageTemplateCommandHandler` · FE `useCreatePageTemplate` · Design none · Tests `PageTemplateHandlerTests`, `PageTemplateTests`, `PageHistoryAndTemplatesContractOpenApiTests` · Real e2e none
- State: INTEGRATING — tests and owner check (replay, real e2e, artboard, visibility of a template whose source the viewer cannot read).

### UC-DCT-A11 Create a page from a template
- Capability: A Page lifecycle
- Actors and permission: `CreatePage` on the chosen placement.
- Preconditions: the template is Published in the same workspace.
- Input: `templateId` in the route, optional `title`, `parentId`, `spaceId`.
- Output: an ordinary page and ordinary blocks with new ids; the template icon is carried to the page (decision 23); images, files and videos are not copied and `droppedBlockCount` says how many; the caller is Manager.
- Main flow: 1. New page, From template. 2. Pick a template and placement. 3. The page opens; a notice names dropped blocks.
- No-op / rejection / concurrency / retry: unknown, archived or foreign template is `404`. Retry makes another page.
- UI states: loading (gallery), empty (no templates), notice, permission denied, error.
- Rules: product doc section 54 (product rule DCT-018); `DCREQ135`; decisions 14, 17, 23.
- Acceptance: Given a template with an image and a paragraph, When used, Then the page has the paragraph and the result reports 1 dropped block.
- Evidence: BE `GET /api/v1/workspaces/{workspaceId}/page-templates`, `POST /api/v1/workspaces/{workspaceId}/page-templates/{templateId}/pages` (requires `Idempotency-Key`, `documents.templates.create-page-from-template.v1`), `CreatePageFromTemplateCommandHandler`, `ListPageTemplatesQueryHandler` · FE `useCreatePageFromTemplate`, `usePageTemplates`, `useCreateResource` (one key per attempt, dropped-block toast) · Design none · Tests `PageTemplateHandlerTests` · Real e2e none
- State: INTEGRATING — tests and owner check (replay, real e2e, gallery artboard, dropped-block notice proof).

### UC-DCT-A12 Rename, archive or delete a template (enterprise baseline)
- Capability: A Page lifecycle
- Actors and permission: the template's creator or a Workspace Admin (lead decision LD9); the permission name is added to Governance.
- Preconditions: the template exists in the workspace.
- Input: new `name` or `description`; archive; delete.
- Output: renamed template; an archived template leaves the gallery but pages already made from it are unchanged; delete removes it.
- Main flow: 1. Template gallery, manage. 2. Choose rename, archive or delete. 3. Confirm delete.
- No-op / rejection / concurrency / retry: same name is a no-op; archiving an archived template is a no-op; stale `expectedVersion` is `412`.
- UI states: destructive confirmation, validation error, permission denied, pending.
- Rules: product doc section 55 (template lifecycle separate from page lifecycle), section 59; `DCREQ138`.
- Acceptance: Given an archived template, When listing, Then it is absent and earlier pages are unchanged.
- Evidence: BE none (domain `PageTemplate` has archive and publish events and `PageTemplateStatus`; there is no command or route) · FE none · Design none (Work Management has WM-Template-Manage for board templates) · Tests `DocumentTemplateDefinitionTests` · Real e2e none
- State: IMPLEMENTING — commands, routes, permission, UI.

### UC-DCT-A13 Import a file as a new page
- Capability: A Page lifecycle
- Actors and permission: `CreatePage` on the placement.
- Preconditions: a file of at most 10 MB (lead decision LD14) of type `.md`, `.txt`, `.html` or `.docx`.
- Input: the file, read in the browser; the server sees ordinary page and block writes.
- Output: a new page; a leading `# heading` becomes the title; nested Markdown lists flatten; `.txt` is read as Markdown; Word images are dropped with a notice; all content passes the rich-text v1 validation and import sanitizing (decisions 15, 18).
- Main flow: 1. File menu, Import. 2. Choose a file. 3. The client converts, creates the page, saves blocks. 4. The page opens with a notice.
- No-op / rejection / concurrency / retry: unsupported type or unreadable file is a validation error shown in the dialog; script and event-handler content is stripped. A failure after the page exists leaves an empty page today (should be removed, as UC-DCT-A09).
- UI states: validation error, progress, notice, permission denied, offline.
- Rules: product doc sections 82, 84 (product rule DCT-026); `DCREQ142`; decisions 15, 18.
- Acceptance: Given a Markdown file with a heading and a list, When imported, Then a page titled by the heading with a list block. Given a file with a script tag, Then it is not stored.
- Evidence: BE `POST /api/v1/workspaces/{workspaceId}/pages`, `POST /api/v1/pages/{pageId}/blocks/batch` · FE `useImportPage` (trashes the page when a block write fails), `read-import-file.ts` (10 MB limit, one constant shared with the dialog), `import-markdown.ts`, `sanitize-import.ts` · Design none · Tests `page-transfer.unit.test.ts`, `import-file.component.test.ts`, `import-page.unit.test.ts` · Real e2e none
- State: INTEGRATING — tests and owner check (real e2e, dialog artboard; the 5 MB cases in the unit tests move to 10 MB).

### UC-DCT-A14 Export a page, a subtree or a workspace
- Capability: A Page lifecycle
- Actors and permission: Viewer of the page for page export; Workspace Admin for workspace export (enterprise baseline, lead decision LD22).
- Preconditions: the page is readable.
- Input: format Markdown, HTML or PDF; scope page, page with subpages, or workspace.
- Output: a file the browser downloads. Page export is built in the browser from the loaded blocks: Markdown, sanitized HTML, and PDF through the print stylesheet (decision 15). Subtree and workspace export (Markdown plus files in a ZIP, built by a background job and sent as a download link to the Admin) are not built. Export includes only pages the caller may read.
- Main flow: 1. File menu, Download, choose a format. 2. The browser saves the file.
- No-op / rejection / concurrency / retry: unreadable pages are left out silently; a round trip is guaranteed only for Markdown text blocks (product rule DCT-026).
- UI states: pending, error, permission denied.
- Rules: product doc sections 83, 84, 96; `DCREQ143`; enterprise baseline (Notion and Confluence export).
- Acceptance: Given a page with a heading and a table, When exported to Markdown, Then the file has both. Given a subtree export by a Viewer who cannot read one child, Then that child is absent.
- Evidence: BE none · FE `export-markdown.ts`, `export-html.ts`, File menu Download · Design none · Tests `page-transfer.unit.test.ts`, `format-contract.unit.test.ts` · Real e2e none
- State: INTEGRATING — subtree and workspace export, retention of generated files, real e2e.

### UC-DCT-A15 Set page visibility
- Capability: A Page lifecycle
- Actors and permission: Manager of the page.
- Preconditions: the page is active.
- Input: Private, Workspace or Public.
- Output: for Private and Workspace the intent is expressed as Governance sharing (share dialog, UC-DCT-I03); Public means readable without sign-in.
- Main flow: not defined for Public.
- No-op / rejection / concurrency / retry: the shipped `PublishPageCommandHandler` throws `NotImplementedException` and has no route; the stub is retired (lead decision LD17).
- UI states: not applicable until decided.
- Rules: product doc sections 12, 13 (product rules DCT-004, DCT-019); `DCREQ114` to `DCREQ116`.
- Acceptance: not writable until the anonymous principal is decided.
- Evidence: BE none (`PageVisibility` enum, `PublishPageCommand` stub) · FE none · Design Final-Share (Governance) · Tests `PageViewAccessTests` · Real e2e none
- State: BLOCKED — owner must decide whether anonymous public pages exist, the anonymous principal, abuse limits and crawler policy (same family as the public form decision for Work Management).

### UC-DCT-A16 Favorite a page
- Capability: A Page lifecycle
- Actors and permission: any person who can view the page.
- Preconditions: none.
- Input: page id.
- Output: the page appears in the person's favorites in the sidebar.
- Main flow: not available; the client logs a warning and does nothing.
- No-op / rejection / concurrency / retry: not defined.
- UI states: not applicable.
- Rules: not in the product doc.
- Acceptance: not writable until the owner context is decided.
- Evidence: BE none · FE `createFavoriteApi` (toggle is a stub; list filters `isFavorited` of the page list) · Design none · Tests none · Real e2e none
- State: BLOCKED — decide which context owns per-user favorites (Identity user preferences, Collaboration, or Documents); until then remove the dead toggle.

### UC-DCT-A17 Retain and purge deleted pages
- Capability: A Page lifecycle
- Actors and permission: system job.
- Preconditions: a page was deleted (UC-DCT-A06).
- Input: retention window.
- Output: rows, versions, search projections, exports and backups of the page are physically removed after the window.
- Main flow: not defined.
- No-op / rejection / concurrency / retry: none yet.
- UI states: not applicable.
- Rules: product doc sections 11, 96 (product rule DCT-003).
- Acceptance: not writable until the window and backup policy are decided.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: BLOCKED — owner decides the retention window for deleted pages, versions and backups (a data-destruction choice).

## Capability B: Page hierarchy

Team capability 2 (team id DCT-002). Requirements `DCREQ006`, `DCREQ007`, `DCREQ010`, `DCREQ025`.

### UC-DCT-B01 Show the page tree
- Capability: B Page hierarchy
- Actors and permission: a workspace member (`ViewWorkspace`); each page is filtered by the per-page `ViewPage` decision.
- Preconditions: none.
- Input: workspace id.
- Output: the active pages the caller may see, each with id, title, icon, parent id, "has children", version and Space; sorted by title (lead decision LD3: case and mark insensitive; manual sibling order is not offered). A visible page under a parent the caller cannot see is shown as a root and the hidden parent's id is never sent.
- Main flow: 1. The sidebar asks for the tree. 2. It builds the nested list; the open page is highlighted and its ancestors expanded.
- No-op / rejection / concurrency / retry: trashed and deleted pages are absent. A person with no visible page gets an empty list, not an error. The whole list is returned (see UC-DCT-G02 for paging).
- UI states: loading, empty ("No pages yet" with New page), permission denied, error with retry, realtime refresh.
- Rules: product doc sections 14, 16, 17 (product rule DCT-005); `DCREQ006`, `DCREQ025`; GOV-029.
- Acceptance: Given a private child under a visible parent, When a person without access lists, Then the child is absent and the parent shows no child marker for it.
- Evidence: BE `GET /api/v1/workspaces/{workspaceId}/pages/tree?parentId`, `GetPageTreeQueryHandler` (sorted by `PageTitleOrder`, case and marks ignored; whole tree up to 2 000 visible pages, beyond that root pages only, and `parentId` answers the visible children of that page, a hidden or missing parent is `404`; `hasChildren` counts visible children only) · FE `docs-sidebar-tree.ts`, `usePageList` (the sidebar builds its tree from the page list, not from this endpoint, so no client reads the one-level mode yet) · Design Final-Docs-2 (sidebar) · Tests `PageViewAccessTests`, `PageHierarchyTests` · Real e2e none
- State: INTEGRATING — tests and owner check (sort order, the 2 000 switch, `parentId` of a hidden page, hidden-parent test over the API, real e2e; the sidebar still reads the unpaged page list, see UC-DCT-G02).

### UC-DCT-B02 Create a child page
- Capability: B Page hierarchy
- Actors and permission: `CreatePage` on the parent page.
- Preconditions: the parent is active and in the same workspace; depth is below the limit (UC-DCT-B05).
- Input: as UC-DCT-A01 with `parentId`.
- Output: a child page in the parent's Space; the tree shows it under the parent.
- Main flow: 1. In the sidebar, "Add a page" on a row. 2. The client posts with `parentId`. 3. The tree expands and the new page opens.
- No-op / rejection / concurrency / retry: a trashed, missing or foreign parent is `404`; over the depth limit is `400`.
- UI states: as UC-DCT-A01.
- Rules: product doc section 14; `DCREQ006`.
- Acceptance: Given a parent page, When a child is created, Then the child's `parentId` is the parent and its Space equals the parent's.
- Evidence: BE `POST /api/v1/workspaces/{workspaceId}/pages` and the from-template route refuse a parent that would put the page deeper than 10 levels (`PageTreeRules.EnsureCanNestUnder`, `IPageSubtreeStore.DepthOfAsync`, `400` `Documents_PageTree_MaxDepthExceeded`) · FE `useCreatePage` (no client creates a page under a chosen parent yet: the File menu makes a sibling) · Design Final-Docs-2 · Tests `CreatePageTargetPolicyMatrixTests`, `PageHierarchyTests` · Real e2e none
- State: INTEGRATING — tests and owner check (depth 10 refused, depth 9 accepted, real e2e; the handler tests construct the two create handlers without `IPageSubtreeStore`).

### UC-DCT-B03 Show the breadcrumb
- Capability: B Page hierarchy
- Actors and permission: Viewer of the page (`ViewPage`).
- Preconditions: none.
- Input: page id.
- Output: the chain of ancestor pages from the root, then the page; ancestors the caller cannot see are shown as one "page unavailable" crumb without title or id.
- Main flow: the page head loads it with the page.
- No-op / rejection / concurrency / retry: a move or rename refreshes it (derived state, product section 17).
- UI states: loading, error.
- Rules: product doc sections 16, 17; `DCREQ025`.
- Acceptance: Given a page two levels deep, When opened, Then the crumb shows root, parent, page.
- Evidence: BE `GET /api/v1/pages/{pageId}/breadcrumb`, `GetPageBreadcrumbQueryHandler` climbs one parent at a time inside the workspace (it read every page of the tenant before), evaluates `ViewPage` for each ancestor through `IPageViewAccess`, and answers one `unavailable` crumb without id, title or icon for a run of hidden or missing ancestors · FE `usePageBreadcrumb`, `mapBreadcrumb`, crumb text `crumbUnavailable` · Design Final-Docs-2 · Tests `PageHierarchyTests` · Real e2e none
- State: INTEGRATING — tests and owner check (hidden-ancestor proof over the API, run of hidden ancestors collapses to one crumb, real e2e; one access check per ancestor, at most 9 with the depth limit).

### UC-DCT-B04 Refuse a cycle or a foreign parent
- Capability: B Page hierarchy
- Actors and permission: system rule on any move or create.
- Preconditions: none.
- Input: a requested parent.
- Output: refused when the page would be its own parent or descendant (`Documents_PageTree_CannotBeOwnParent`, `Documents_PageTree_MoveWouldCreateCycle`), or the parent is in another workspace or archived.
- Main flow: the Application supplies the ancestor chain; the Domain validates.
- No-op / rejection / concurrency / retry: two concurrent moves that together make a cycle: the second one fails on the ancestry it reads inside the same transaction (target rule, with the page version).
- UI states: validation error ("Cannot move a page into itself").
- Rules: product doc sections 14, 15 (product rule DCT-005); `DCREQ007`, `DCREQ010`.
- Acceptance: Given pages A > B, When A is moved under B, Then refused and unchanged.
- Evidence: BE `Page.Move`, `PageTreeRules.EnsureNoCycle`, reached through `POST /api/v1/pages/{pageId}/move` (the handler supplies the new parent's ancestry; a parent in another workspace, trashed or deleted is `404`) · FE the Move to dialog lists only places that are not the page, its own pages or its current parent, and words a `400` as a rule message · Design none · Tests `PageTreeRulesTests`, `PageHierarchyTests` · Real e2e none
- State: INTEGRATING — tests and owner check (cycle and own-parent refusals through the API, concurrent moves with the version).

### UC-DCT-B05 Limit nesting depth
- Capability: B Page hierarchy
- Actors and permission: system rule.
- Preconditions: none.
- Input: a create or move that would put a page deeper than 10 levels (lead decision LD2).
- Output: refused with a business-rule error naming the limit.
- Main flow: the Application supplies the depth of the new parent.
- No-op / rejection / concurrency / retry: moving a subtree checks the deepest descendant.
- UI states: validation error.
- Rules: new rule in Part 5; team spec section 7 "maximum nesting if product-defined".
- Acceptance: Given a page at depth 10, When a child is created, Then refused.
- Evidence: BE `PageTreeRules.MaxDepth` and `EnsureCanNestUnder(parentDepth, subtreeDepth)` applied on create, create from template and move (the move passes the level of the deepest descendant) · FE the Move to dialog words the `400` as a rule message · Design none · Tests none · Real e2e none
- State: INTEGRATING — tests and owner check (depth 10 refused on create and on a move that deepens a subtree).

## Capability C: Page move / reparent

Team capability 3 (team id DCT-003). Requirements `DCREQ008` to `DCREQ010`, `DCREQ034`.

### UC-DCT-C01 Move a root page and its subtree to a space
- Capability: C Page move / reparent
- Actors and permission: `UpdatePage` on the page and permission to place content in the target Space (GOV-028).
- Preconditions: the page is a root page (no parent) and active.
- Input: `spaceId` (or null for the workspace root), `expectedVersion` of the root page.
- Output: the root page and every descendant take the new Space; versions +1; the tree shows them under the new Space.
- Main flow: 1. The person picks Move to space on a root page. 2. The client posts the target and version. 3. The server authorizes both ends, moves, and answers `204`.
- No-op / rejection / concurrency / retry: the same Space is a no-op. A nested page is refused (`Documents_Page_NestedPageFollowsRootSpace`). A trashed page or archived Space is refused. A stale version is `412` and nothing moves. A target Space the caller cannot see answers `404`.
- UI states: pending, conflict, permission denied, validation error, offline.
- Rules: product doc sections 16, 86; `DCREQ008`, `DCREQ034`, `DCREQ010`.
- Acceptance: Given a root page with two children, When moved to Space S, Then all three show Space S. Given a stale version, Then `412` and no change.
- Evidence: BE `POST /api/v1/pages/{pageId}/move-to-space`, `MovePageToSpaceCommandHandler` (now evaluates `UpdatePage` for every page below the root through `IPageSubtreeAuthorizer` before any page changes Space, and skips deleted pages that used to make the move throw) · FE `useMovePageToSpace`, the dialog shows a specific message when a page below is refused · Design Final-Docs-2 · Tests `page-move-to-space.unit.test.ts`, `PageHierarchyTests` · Real e2e none
- State: INTEGRATING — tests and owner check (refusal when a page below is not the caller's, deleted page below, real e2e for success, 412 and hidden target, artboard for the dialog).

### UC-DCT-C02 Move a page under another parent
- Capability: C Page move / reparent
- Actors and permission: `UpdatePage` on the page and `CreatePage` on the new parent (lead decision LD4).
- Preconditions: the page and the new parent are active, same workspace.
- Input: `parentId` (null for the Space root), `expectedVersion` of the page.
- Output: the page takes the new parent; the subtree follows the Space of the new root; versions +1; `PageMoved` raised; breadcrumb and tree refresh.
- Main flow: 1. Drag in the sidebar or Move to in the menu. 2. The client posts the target. 3. The server supplies ancestry to the Domain and commits.
- No-op / rejection / concurrency / retry: same parent is a no-op; cycle, foreign parent, trashed page, depth over the limit are refused; stale version is `412`; hidden destination is `404`.
- UI states: pending, conflict, validation error, permission denied, offline.
- Rules: product doc sections 14 to 16 (product rule DCT-005); `DCREQ008`, `DCREQ010`, `DCREQ034`; team spec section 8 lists what a move must define.
- Acceptance: Given pages A and B, When A is moved under B, Then A's breadcrumb is B > A and a stale second move is `412`.
- Evidence: BE `POST /api/v1/pages/{pageId}/move` (body `parentId`, `expectedVersion`), `MovePageCommand` is `UpdatePage` plus `IExpectedVersionRequest` (in `ExpectedVersionTargetMap`); `MovePageCommandHandler` authorizes `CreatePage` on the new parent (`IPageSpacePlacement.AuthorizeParentAsync`) or in the Space for a root page, refuses a trashed, deleted or foreign parent (`404`), a cycle and a nesting beyond 10 levels (`400`), puts the page and its subtree in the new parent's Space with one version bump (`Page.Move` with Space) and, when the Space changes, first evaluates `UpdatePage` for every page below · FE File menu Move to, `DocMovePageDialogSurface`, `useMovePage` (refreshes lists, tree, page and breadcrumb; conflict, subtree and rule messages), drag in the sidebar not built · Design none · Tests `PageHierarchyTests`, `PageTreeRulesTests` · Real e2e none
- State: INTEGRATING — tests and owner check (the dead `MovePageCommand` stub is replaced; `PublishPageCommand` and `SetPageDeadlineCommand` still throw `NotImplementedException` and are retired with UC-DCT-A15).

### UC-DCT-C03 Refuse a move to another workspace
- Capability: C Page move / reparent
- Actors and permission: system rule.
- Preconditions: none.
- Input: any attempt to put a page under a parent or Space of another workspace.
- Output: refused; to bring content over the person exports and imports it, or duplicates it into the other workspace (product rule DCT-027: a cross-workspace move is a migration, not a tree move; lead decision LD5).
- Main flow: not offered in the UI; the API refuses.
- No-op / rejection / concurrency / retry: the answer is `404` for a destination the caller cannot see, else a business-rule error.
- UI states: validation error where a picker could offer it.
- Rules: product doc sections 87, 88; `DCREQ009`.
- Acceptance: Given a page and a Space of another workspace, When moved, Then refused and unchanged.
- Evidence: BE `MovePageToSpaceCommandHandler` (reads the caller's workspace) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — explicit rejection test.

## Capability D: Block lifecycle

Team capability 4 (team id DCT-004). Requirements `DCREQ011` to `DCREQ013`, `DCREQ117` to `DCREQ121`.

### UC-DCT-D01 Add a block
- Capability: D Block lifecycle
- Actors and permission: Editor of the page (`UpdatePage` on the page; a block has no permission of its own).
- Preconditions: the page is active; the parent block, if any, is a live block of the same page.
- Input: `type` (closed set), optional `id` chosen by the editor, `content`, `properties`, optional `afterBlockId`, `parentBlockId`.
- Output: a block with version 1 in the page's account and workspace, placed after the named sibling or last; the page announces a save (UC-DCT-J01).
- Main flow: 1. The person types in the empty page, uses `/`, or presses Enter. 2. The client creates the block with its own id. 3. The block shows saved.
- No-op / rejection / concurrency / retry: unknown type, invalid content or properties is `400`; a parent in another page is `404`; an id already used is `409` (UC-DCT-L04 for replay). Create is not a no-op.
- UI states: pending, validation error, read-only, permission denied, offline, conflict.
- Rules: product doc sections 18, 19, 25 (product rules DCT-006, DCT-009); `DCREQ011`, `DCREQ012`, `DCREQ118`.
- Acceptance: Given an Editor, When a block is created, Then it is listed with version 1 after the named sibling. Given a parent block of another page, Then `404`.
- Evidence: BE `POST /api/v1/pages/{pageId}/blocks`, `CreateBlockCommandHandler` (refuses a trashed page, refuses a page past 5 000 live blocks with `Documents_Page_BlockLimitReached` through `PageRules.EnsureBlockCountWithinLimit`, answers the stored block when the id repeats with the same type, content and properties through `Block.IsRetryOfCreate`, else `409 documents.block.id-taken`) · FE `useCreateBlock`, `DocEditorSurface` · Design Final-Docs-2 · Tests `BlockEditingHandlerTests`, `BlockTests`, `BlockCreateChildScopeTests` · Real e2e `documents.real.e2e.spec.ts`
- State: HARDENING — tests and owner check (limit refusal, replay and conflicting replay, cross-tenant negative test).

### UC-DCT-D02 Edit a block
- Capability: D Block lifecycle
- Actors and permission: Editor of the page (`UpdatePage`).
- Preconditions: the block is live in that page.
- Input: `expectedVersion` (required, positive), optional `type`, `content`, `properties`.
- Output: the block changes, its version +1, and the stored block returns; mentions added by this save are announced once (UC-DCT-E08).
- Main flow: 1. The person types. 2. The saver sends the changed block after a short pause or on leaving the block. 3. The server answers the new version.
- No-op / rejection / concurrency / retry: an edit that changes nothing leaves the version and emits nothing. A missing or non-positive `expectedVersion` is `400`; a stale one is `412` carrying `currentBlock`. Retry with the new version after a rebase (UC-DCT-H02).
- UI states: saved, saving, conflict, validation error, read-only, offline.
- Rules: product doc sections 31, 32, 78, 79 (product rules DCT-011, DCT-025); `DCREQ028` to `DCREQ030`, `DCREQ137`.
- Acceptance: Given version 3, When a write carries 3, Then version 4. When another write still carries 3, Then `412`, block unchanged.
- Evidence: BE `PATCH /api/v1/pages/{pageId}/blocks/{blockId}`, `UpdateBlockCommandHandler`, `PageBlockSet.EnsureVersion` · FE `useUpdateBlock`, `doc-block-saver.ts` · Design Final-Docs-2 · Tests `BlockEditingHandlerTests`, `BlockEditTests`, `doc-block-saver.unit.test.ts` · Real e2e `documents.real.e2e.spec.ts`
- State: HARDENING — tests and owner check (real e2e for the 412 and rebase; the handler now also refuses a trashed page before the version check).

### UC-DCT-D03 Save several blocks as one change
- Capability: D Block lifecycle
- Actors and permission: Editor of the page.
- Preconditions: as D01 and D02.
- Input: up to 500 operations: `upsert` (create with the caller's id, or edit with `expectedVersion`) or `delete` (with `expectedVersion`), each with optional placement.
- Output: all operations apply in order or none; the answer lists the saved, not deleted blocks; the page announces one save.
- Main flow: the editor saver and the importer send batches (typing bursts, paste, import, restore, duplicate).
- No-op / rejection / concurrency / retry: any failing operation fails the whole batch and nothing is stored; a new block needs its type; an id used by a block of another page is `409`; over 500 operations is `400`.
- UI states: saving, conflict, validation error, offline.
- Rules: product doc section 21A ("a batch applies completely or not at all"); `DCREQ028`.
- Acceptance: Given a batch whose third operation is stale, When sent, Then the first two are not stored and the answer is `412`.
- Evidence: BE `POST /api/v1/pages/{pageId}/blocks/batch`, `BatchUpdateBlocksCommandHandler` (refuses a trashed page and a result past 5 000 live blocks; an upsert with a type and no version for an id the page already holds answers the stored block when type, content and properties match, so a retried save does not fail, and is `409` otherwise; delete goes through `PageBlockSet.Delete`) · FE `batch-block-writer.ts`, `useDocSaver` · Design Final-Docs-2 · Tests `BatchUpdateBlocksHandlerTests`, `doc-block-saver.unit.test.ts` · Real e2e none
- State: INTEGRATING — tests and owner check (retry of a whole batch, limit, real e2e with a mid-batch failure).

### UC-DCT-D04 Delete a block
- Capability: D Block lifecycle
- Actors and permission: Editor of the page.
- Preconditions: the block is live.
- Input: block id; in a batch also `expectedVersion`.
- Output: the block and every block under it are deleted (lead decision LD10, product rule DCT-029); the page announces a save; the blocks stay recoverable through version history.
- Main flow: 1. The person deletes a line or a block from its menu. 2. The client sends a batch delete (or the single route). 3. The line disappears.
- No-op / rejection / concurrency / retry: deleting a deleted block is `404`. Batch delete needs `expectedVersion`; the single `DELETE` route has none and does not delete the children (defect, Part 1).
- UI states: pending, undo toast, conflict, read-only.
- Rules: product doc sections 91, 92, 93 (product rule DCT-029); `DCREQ013`, `DCREQ121`.
- Acceptance: Given a toggle with two children, When it is deleted, Then no live block remains under it.
- Evidence: BE `DELETE /api/v1/pages/{pageId}/blocks/{blockId}?expectedVersion=` (`DeleteBlockCommandHandler`: version checked, `412` with the current block when stale, trashed page refused, the blocks under it deleted through `PageBlockSet.Delete`, one page save announced); batch delete in `BatchUpdateBlocksCommandHandler` uses the same cascade · FE `useDocEditor.deleteBlock` sends the cached version and treats `404` and `412` as "show what is stored" (no failed save), `useDeleteBlock` · Design Final-Docs-2 · Tests `BlockEditingHandlerTests`, `BatchUpdateBlocksHandlerTests`, `docs-blocks-trash.unit.test.ts` · Real e2e none
- State: INTEGRATING — tests and owner check (cascade and `412` on the single route, real e2e; the editor only deletes an empty line, a block menu with an undo toast is not built).

### UC-DCT-D05 Refuse edits to a trashed page
- Capability: D Block lifecycle
- Actors and permission: system rule on every block write.
- Preconditions: the page is in the trash.
- Input: any create, edit, delete, reorder or batch write.
- Output: refused with `Documents_Page_CannotEditArchived` (lead decision LD12); nothing changes.
- Main flow: the handler checks the page status before applying.
- No-op / rejection / concurrency / retry: today the authorization layer treats a trashed page as not active, so the call is refused as not found; no handler checks the status and no test proves it. Version restore and version save do check.
- UI states: read-only banner "This page is in the trash".
- Rules: product doc section 77; `DCREQ003`, `DCREQ004`.
- Acceptance: Given a trashed page, When a block is created, Then refused and no block exists.
- Evidence: BE `PageRules.EnsureCanEdit` called by `CreateBlockCommandHandler`, `UpdateBlockCommandHandler`, `DeleteBlockCommandHandler` and `BatchUpdateBlocksCommandHandler` before anything changes, and by `Page.RecordBlocksSaved`; `PostgresPageAuthorizationFacts` still hides a trashed page first; `CreatePageVersionCommandHandler` (checks) · FE none · Design none · Tests none · Real e2e none
- State: INTEGRATING — tests and owner check (reorder joins the guard with UC-DCT-F02; the read-only banner for a page trashed under an open editor belongs to UC-DCT-I06).

### UC-DCT-D06 Convert a checklist line to a task
- Capability: D Block lifecycle
- Actors and permission: Editor of the page and the right to create an item on the chosen board (Work Management).
- Preconditions: the line is a checklist line with text; the person can see at least one board with a group.
- Input: board and group; the line text becomes the item name.
- Output: a board item, a link from the item to the page, and the line marked with a `[[task:<id>]]` chip. Each write is separate (product doc section 20); a failure after the item exists leaves the line unlinked, and asking again finishes the conversion without creating another item (the item and the link are sent under keys derived from one attempt key, and a line that already holds the chip counts as done).
- Main flow: 1. "Convert to task" on a line. 2. Pick board and group. 3. The line becomes a chip of the task.
- No-op / rejection / concurrency / retry: board without rights is not offered; a stale line version is `412` and the item stays; the dialog names the step that failed (item, link or line) and says no second task is made on retry.
- UI states: pending, error naming the step that failed, permission denied.
- Rules: product doc section 20 (editor orchestration), product rule DCT-020; `DCREQ122`. Work Management owns the item: [work-management.md](work-management.md).
- Acceptance: Given a checklist line, When converted, Then the item exists on the board and the line shows its chip.
- Evidence: BE `POST /api/v1/boards/{boardId}/items` and `POST /api/v1/board-items/{itemId}/pages` (Work Management), `PATCH` block · FE `useConvertTodo` (keeps the attempt key of a failed conversion until it succeeds), `convertTodo` in `page-extras.api.ts` (`ConvertTodoFailed` names the step), `DocConvertView.failedStep` and the `convert.failedLink` / `failedLine` copy in the dialog · Design Final-Docs-2 · Tests `ItemPageLinkTests` · Real e2e none
- State: INTEGRATING — tests and owner check (partial-failure proof for the link and line steps, real e2e; a single server call for the three writes needs a Work Management and Documents decision and is not built).

## Capability E: Block types and content contract

Team capability 5 (team id DCT-005). Requirements `DCREQ014` to `DCREQ017`, `DCREQ139`, `DCREQ144` to `DCREQ146`. The closed type set is `BlockType` in the Domain: Text, Heading1 to Heading3, BulletList, NumberedList, ToDo, Toggle, Quote, Divider, Code, Image, File, Video, Board, Table, Callout, PageReference, TableOfContents.

### UC-DCT-E01 Write rich text with marks
- Capability: E Block types and content contract
- Actors and permission: Editor of the page.
- Preconditions: the block is a text-like type.
- Input: `{"v":1,"runs":[{"text","marks":[]}]}`; marks `bold`, `italic`, `underline`, `strike`, `code`, `link` (`http`, `https`, `mailto`), `color`, `highlight`, `mention`, `pageRef`; at most 20 000 characters per block.
- Output: the server normalises (empty runs dropped, equal neighbours merged, marks ordered, `code` keeps only `link`) and stores; the editor shows the stored form.
- Main flow: the person types and formats; the saver writes the block (UC-DCT-D02).
- No-op / rejection / concurrency / retry: an unknown mark, a bad link scheme, a mention that is not a user id, content over the limit or an unknown field is `400` with a rule code (`Documents_Block_MarkInvalid`, `Documents_Block_LinkHrefInvalid`, `Documents_Block_ContentTooLarge`, `Documents_Block_ContentInvalid`) and nothing is stored.
- UI states: saved, validation error on the block, read-only.
- Rules: product doc section 21A (product rules DCT-007, DCT-008); `DCREQ015`, `DCREQ016`; decision 2.
- Acceptance: Given two adjacent runs with the same marks, When saved, Then one run. Given a `javascript:` link, Then `400`.
- Evidence: BE `PATCH /api/v1/pages/{pageId}/blocks/{blockId}`, `RichText`, `BlockRules` · FE `use-doc-rich-editor.ts`, `block-mapping.ts`, rich-text types in docs-core · Design Final-Docs-2 · Tests `BlockContentValidatorTests`, `BlockPropertiesValidatorTests`, `rich-text.unit.test.ts`, `block-mapping.unit.test.ts`, `format-commands.component.test.tsx` · Real e2e `documents.real.e2e.spec.ts`
- State: HARDENING — tests and owner check (real e2e for a rejected write; the server refuses every case listed through `BlockContentValidator`).

### UC-DCT-E02 Change a block's type
- Capability: E Block types and content contract
- Actors and permission: Editor of the page.
- Preconditions: the block is live.
- Input: new `type` on an edit with `expectedVersion`.
- Output: the block keeps its id; text carries over between text-like types; a change to or from any other type starts empty (product doc section 20).
- Main flow: the person picks Turn into in the block menu or types a markdown shortcut.
- No-op / rejection / concurrency / retry: the same type is a no-op; unknown type is `400`; stale version is `412`.
- UI states: saved, conflict, validation error.
- Rules: product doc sections 20, 21; `DCREQ014`, `DCREQ016`.
- Acceptance: Given a paragraph with text, When turned into Heading 2, Then the id is unchanged and the text kept.
- Evidence: BE `UpdateBlockCommandHandler`, `Block.Edit` · FE `editor-commands.ts`, `slash-menu.tsx` · Design Final-Docs-2 · Tests `BlockChangeTypeTests`, `format-commands.component.test.tsx` · Real e2e none
- State: INTEGRATING — tests and owner check (real e2e; the server keeps the id, converts text between text-like types and starts empty otherwise, and a trashed page refuses the edit).

### UC-DCT-E03 Use structured blocks
- Capability: E Block types and content contract
- Actors and permission: Editor.
- Preconditions: none.
- Input: lists, checklist lines (`checked`), toggle (`collapsed`), quote, callout (`tone`, `icon`), code (`language`), divider, table of contents; heading levels and alignment as typed properties.
- Output: stored with closed-set properties; the table of contents block renders the outline of the page's headings; checklist state and toggle collapse are saved properties.
- Main flow: slash menu, markdown shortcuts, toolbar.
- No-op / rejection / concurrency / retry: a property outside the closed set is `400` (`Documents_Block_PropertiesInvalid`).
- UI states: saved, validation error, read-only.
- Rules: product doc sections 20, 21, 23, 99, 102; `DCREQ014`, `DCREQ015`.
- Acceptance: Given a code block with language `ts`, When reloaded, Then the language is kept and highlighted.
- Evidence: BE `BlockProperties`, `BlockRules` · FE `extensions.tsx`, `code-block.tsx`, `toggle-collapse.ts`, `doc-table-of-contents.tsx` · Design Final-Docs-2 · Tests `BlockPropertiesTests`, `code-fence.component.test.tsx`, `list-continuation.component.test.tsx`, `toggle-collapse.component.test.tsx`, `table-of-contents.component.test.tsx`, `outline.component.test.tsx` · Real e2e none
- State: INTEGRATING — tests and owner check (one real e2e per type family).

### UC-DCT-E04 Use a table block
- Capability: E Block types and content contract
- Actors and permission: Editor.
- Preconditions: none.
- Input: rows of rich-text cells in the block's properties; add and remove rows and columns, header row.
- Output: the table is saved as typed properties; cells obey rich-text v1.
- Main flow: slash menu, Table; edit cells; table menu for rows and columns.
- No-op / rejection / concurrency / retry: an invalid cell is `400`; the whole table is one block, so two people editing different cells conflict on the block version (`412`).
- UI states: saved, conflict, validation error, read-only.
- Rules: product doc section 21A; `DCREQ015`.
- Acceptance: Given a 2 by 2 table, When a row is added and saved, Then reload shows 3 rows.
- Evidence: BE `BlockProperties` (table rows) · FE `table.ts`, `table-commands.ts`, `table-menu.tsx`, `block-wire.ts`; a table edit refused as stale keeps the member's cells in the recovery panel row by row (`draftRecoveryText`) under the note "A table is one block, so two people editing different cells still conflict" (`recovery.bodyTable`) · Design none · Tests `table-commands.component.test.tsx`, `table-mapping.unit.test.ts`, `block-wire-table.unit.test.ts` · Real e2e none
- State: INTEGRATING — tests, artboard and owner check (no artboard draws the table menu or the conflict note; real e2e).

### UC-DCT-E05 Add an image, file or video block
- Capability: E Block types and content contract
- Actors and permission: Editor of the page (upload and delete need edit; list and read need view, decision 10).
- Preconditions: a file of at most 100 MB with an allowed extension.
- Input: the file is uploaded as a Collaboration attachment of the page (target `documents.page`); the block stores only `attachmentId` and a caption (never base64, decision 4).
- Output: the block shows the file by an authorized content read; deleting the block does not hard-delete the object.
- Main flow: slash menu or drop; progress; the block appears.
- No-op / rejection / concurrency / retry: a blocked extension or oversize file is refused in the dialog; the upload retries with the same idempotency key; a person who can view the page can read the file, nobody gets a public URL.
- UI states: progress, error, retry, permission denied, read-only.
- Rules: product doc sections 69 to 71 (product rule DCT-022); `DCREQ144` to `DCREQ146`; decisions 4, 10. Attachments are Collaboration use cases: see [collaboration.md](collaboration.md).
- Acceptance: Given an image, When inserted and the page reloaded, Then it renders through an authorized read; a person without page access gets `404` for the file.
- Evidence: BE Collaboration page-attachment routes (see collaboration.md) · FE `media.tsx`, `media-commands.ts`, `attachment-uploads.ts`, `usePageAttachments` · Design none · Tests `media.component.test.tsx`, `attachment-uploads.unit.test.ts`, `page-attachments.unit.test.ts`, `PageAttachmentEndpointTests`, `PageAttachmentTests` · Real e2e none
- State: INTEGRATING — tests, artboard and owner check (no artboard draws the upload states; duplicate leaves image and file blocks behind with a toast and a template drops them with `droppedBlockCount`, see UC-DCT-A09 and A11; real e2e).

### UC-DCT-E06 Embed a board in a page
- Capability: E Block types and content contract
- Actors and permission: Editor of the page to place it; the viewer's own right on the board to see it.
- Preconditions: the board exists and the editor can view it.
- Input: `boardId` and optional `viewId` in the block's properties.
- Output: a live view of the board shown inside the page for people who can view the board; others see "unavailable" without a hint whether it exists (decision 11).
- Main flow: slash menu, Board; choose a board and view.
- No-op / rejection / concurrency / retry: a deleted or archived target shows "unavailable"; the stored reference is kept.
- UI states: loading, unavailable, read-only, permission denied.
- Rules: product doc sections 38 to 44 (product rules DCT-014, DCT-015, DCT-020); `DCREQ122` to `DCREQ127`.
- Acceptance: Given a page that embeds a private board, When a person without access opens the page, Then they see "unavailable" and no board data.
- Evidence: BE block type `Board`; board reads in Work Management · FE `doc-embeds` tests, `media-host.ts` · Design none · Tests `doc-embeds.component.test.tsx`, `doc-embeds.unit.test.ts` · Real e2e none
- State: INTEGRATING — tests, artboard and owner check (server proof that the page read carries only `boardId` and `viewId` and no board data, so the reader's own Work Management right decides what shows; no artboard; real e2e). Not built, needs a decision: a write-time check that the target exists and the editor can view it would also refuse a duplicate or import that keeps an embed whose board the person cannot see (lead decision LD7 keeps references pointing at the original targets), so the editor's picker lists only boards the person can view and the server keeps a stored reference as written.

### UC-DCT-E07 Reference another page
- Capability: E Block types and content contract
- Actors and permission: Editor places it; the reader sees the target's name only when the reader may read it.
- Preconditions: the target is an active page of the same workspace.
- Input: a `PageReference` block (`pageId`) or a `pageRef` mark.
- Output: a chip with the target's title and icon; unreadable, trashed or missing targets show one generic "page unavailable" (decisions 11, LD28); the target lists the referrer under backlinks (UC-DCT-G04).
- Main flow: type `[[` or use the slash menu; pick a page. Plain click opens the page in read-only mode; while editing Cmd or Ctrl+click opens it and Alt+Enter opens the chip under the caret (decision 21).
- No-op / rejection / concurrency / retry: a target in another workspace is refused (`Documents_Block_PageReferenceTargetInvalid`).
- UI states: loading, unavailable, read-only.
- Rules: product doc sections 20, 38 to 45; `DCREQ122` to `DCREQ127`; decisions 11, 21.
- Acceptance: Given a reference to a page the reader cannot see, When rendered, Then "page unavailable" with no title.
- Evidence: BE `BlockRules`, `GetPageBacklinks` · FE `mark-chips.tsx`, `doc-embeds` · Design Final-Docs-2 · Tests `mark-chips.component.test.tsx`, `BlockContentValidatorTests` · Real e2e `documents-trash-links.real.e2e.spec.ts`
- State: HARDENING — tests and owner check (trashed-target proof: the page list the viewer reads excludes trashed pages, so the chip reads "page unavailable"; backlinks also leave out trashed referrers). Not built, needs a decision: refusing a reference to a page in another workspace at write time has the same conflict with LD7 as the board embed, and the chip lookup reads the whole page list, which unit G bounds.

### UC-DCT-E08 Mention a person in a page
- Capability: E Block types and content contract
- Actors and permission: Editor.
- Preconditions: the person is a workspace member who can view the page.
- Input: a `mention` mark with the user id; suggestions list only people who can view the page (decisions 12, 42).
- Output: the person is notified once, when the mention is newly added in a saved edit, never for self-mentions and not on every save; removing the mention does not retract it. The notification path is Collaboration's.
- Main flow: type `@`, choose a person.
- No-op / rejection / concurrency / retry: a mention of someone who cannot view the page stays as text and sends nothing; the same mention saved again sends nothing.
- UI states: suggestion list loading and empty, saved.
- Rules: product doc sections 35, 63; `DCREQ153` to `DCREQ157`; decision 12. See [collaboration.md](collaboration.md).
- Acceptance: Given a new mention, When saved, Then one notification; When saved again unchanged, Then none.
- Evidence: BE `BlockMentions`, `BlockSaveTracker`, `PageBlocksSavedMentionConsumer` · FE `mention-trigger.tsx` · Design Final-Docs-2 · Tests `BlockMentionsTests`, `PageBlocksSavedTests`, `PageBlocksSavedConsumersTests`, `mention.component.test.tsx` · Real e2e none
- State: INTEGRATING — tests and owner check (real e2e of notify once; `PageBlocksSavedMentionConsumer` already skips self-mentions, dedups by save, block and person, and notifies only people who can view the page). The suggestion list still offers every workspace member: a Collaboration-owned read of people who can view the page is UC-COL-B05.

### UC-DCT-E09 Open a block type this client does not know
- Capability: E Block types and content contract
- Actors and permission: any viewer.
- Preconditions: the server stores a type the client's decoder lacks (older web build, mobile).
- Input: a block with an unknown type.
- Output (lead decision LD24): the client shows a read-only placeholder "This block type is not supported in this app", keeps the block's id, type, content and properties untouched, never turns it into a paragraph, and never rewrites it; a save of other blocks leaves it as stored. The server refuses a write with an unknown type (`400`).
- Main flow: automatic on load.
- No-op / rejection / concurrency / retry: mobile and web share one decoder in docs-core (decision 26).
- UI states: read-only placeholder.
- Rules: product doc sections 81, 100 (product rule DCT-007); `DCREQ017`.
- Acceptance: Given a block of type `Whiteboard`, When opened in this client, Then a placeholder appears and the block is unchanged after any save.
- Evidence: BE `PageBlockSet.ParseType` (rejects unknown on write) · FE `block-wire.ts` in docs-core · Design none · Tests `docs-mobile-contract.mobile.test.ts` · Real e2e none
- State: DEFINED — placeholder behaviour and tests.

## Capability F: Block ordering

Team capability 6 (team id DCT-006). Requirements `DCREQ018` to `DCREQ022`. Ordering uses `FractionalIndex` and `FractionalIndexGenerator` (product rule DCT-010).

### UC-DCT-F01 Insert a block between two blocks
- Capability: F Block ordering
- Actors and permission: Editor.
- Preconditions: the sibling named by `afterBlockId` is a live sibling under the same parent.
- Input: `afterBlockId`, `parentBlockId` on create; or placement on a batch upsert.
- Output: the server computes the sort key between the named block and the next one; the block shows exactly there after any reload.
- Main flow: Enter in the middle of a list, or the plus handle between blocks.
- No-op / rejection / concurrency / retry: an `afterBlockId` that is not a sibling is `400`; two people inserting after the same block get two distinct keys and both blocks stay.
- UI states: saved, conflict.
- Rules: product doc sections 28, 29; `DCREQ018` to `DCREQ021`.
- Acceptance: Given blocks A and B, When C is inserted after A, Then the order is A, C, B for every reader.
- Evidence: BE `PageBlockSet.PositionFor`, `FractionalIndexGenerator` · FE `block-insert.ts` · Design Final-Docs-2 · Tests `BlockEditingHandlerTests`, `BlockTreeRulesTests`, `block-mapping.unit.test.ts` · Real e2e none
- State: INTEGRATING — real e2e of the order after reload.

### UC-DCT-F02 Reorder blocks
- Capability: F Block ordering
- Actors and permission: Editor.
- Preconditions: the blocks are live in the page.
- Input: `{ pageId, items: [{ blockId, position, parentId }] }`; the client sends the sort keys (a hover grip, or Alt+Shift+Arrow Up or Down; moves are announced in a live region, decision 22).
- Output: blocks take the new keys and parents.
- Main flow: drag the grip or press the keys.
- No-op / rejection / concurrency / retry: lead decision LD11: the server accepts a key only when it lies between the siblings the request names, carries the block `expectedVersion`, treats an unchanged place as a no-op, and announces one page save. Shipped code takes the client's key as is, has no expected version, and does not announce a save (so no automatic version or live refresh). A block of another page is `404`.
- UI states: moving, conflict, announced result, read-only.
- Rules: product doc sections 27, 28, 30 (product rule DCT-010: ordering is server-authoritative); `DCREQ018`, `DCREQ021`.
- Acceptance: Given three blocks, When the third moves to the top, Then every reader sees it first and one page save is announced.
- Evidence: BE `POST /api/v1/blocks/reorder`, `ReorderBlocksCommandHandler` · FE `useReorderBlocks`, `block-move.ts`, `block-handle.tsx` · Design none · Tests `block-move.component.test.tsx`, `BlockMoveFailureAtomicityTests` · Real e2e none
- State: INTEGRATING — server-validated keys, version, page fact, real e2e.

### UC-DCT-F03 Nest or un-nest a block
- Capability: F Block ordering
- Actors and permission: Editor.
- Preconditions: the new parent is a live block of the same page and not a descendant of the moved block.
- Input: new parent and position (reorder or batch placement).
- Output: parent and position change together as one move (product doc section 27); a cross-page or cyclic state never commits.
- Main flow: Tab or Shift+Tab in a list; drag onto a parent.
- No-op / rejection / concurrency / retry: cycle is refused (`Documents_BlockTree_MoveWouldCreateCycle`); parent in another page is refused (`Documents_BlockTree_ParentMustBeInSamePage`); a failed move leaves the tree unchanged.
- UI states: moving, validation error.
- Rules: product doc sections 25 to 27 (product rules DCT-006, DCT-009); `DCREQ117` to `DCREQ120`.
- Acceptance: Given a block, When moved under its own child, Then refused and unchanged.
- Evidence: BE `Block.MoveUnder`, `BlockTreeRules` · FE `block-move.ts` · Design none · Tests `BlockHierarchyTests`, `BlockTreeRulesTests`, `BlockMoveFailureAtomicityTests` · Real e2e none
- State: INTEGRATING — real e2e.

### UC-DCT-F04 Insert many blocks densely
- Capability: F Block ordering
- Actors and permission: Editor.
- Preconditions: none.
- Input: repeated inserts at the same place, paste of long content, import.
- Output: keys stay valid and ordered; key length stays bounded.
- Main flow: automatic.
- No-op / rejection / concurrency / retry: a key that cannot be generated between two neighbours is a defined server rebalance of that sibling set (not defined yet); malformed keys are `400`.
- UI states: none.
- Rules: product doc section 28; team spec section 11 required tests; `DCREQ020`, `DCREQ022`.
- Acceptance: Given 1 000 inserts after the same block, When listed, Then order is the insert order reversed and every key is valid.
- Evidence: BE `FractionalIndexGenerator` · FE none · Design none · Tests none for Documents (Work Management has `WorkOrderingRebalancerTests` for its own ordering) · Real e2e none
- State: IMPLEMENTING — rebalance rule, dense-insert test through the API.

## Capability G: Document query and loading

Team capability 7 (team id DCT-007). Requirements `DCREQ023` to `DCREQ026`, `DCREQ122` to `DCREQ127`, `DCREQ140`, `DCREQ141`.

### UC-DCT-G01 Open a page
- Capability: G Document query and loading
- Actors and permission: Viewer of the page (`ViewPage`).
- Preconditions: the page is active.
- Input: page id.
- Output: the page (title, icon, Space, status, version), its breadcrumb and its live blocks in order, each with id, type, content, properties and version.
- Main flow: 1. The route loads the page, breadcrumb and blocks. 2. The editor renders; the title and the first blocks show.
- No-op / rejection / concurrency / retry: a missing, trashed, deleted, other-workspace or unreadable page gives the identical `404`; the client shows one "no access" state for all of them.
- UI states: loading skeleton, error with retry, no access, read-only, empty page ("Type / to add").
- Rules: product doc sections 7, 8, 99; `DCREQ023`, `DCREQ025`, `DCREQ026`.
- Acceptance: Given a readable page, When opened, Then title and blocks render in order. Given an unreadable page and a missing page, Then the same `404` body.
- Evidence: BE `GET /api/v1/pages/{pageId}`, `GET /api/v1/pages/{pageId}/blocks`, `GetPageQueryHandler`, `GetPageBlocksQueryHandler` · FE route `/workspaces/$workspaceId/docs/$docId`, `usePage`, `usePageBlocks`, `DocPageScreen` · Design Final-Docs-2, Final-Docs-2-Dem, Final-Docs-2-320 · Tests `PageReadVersionTests`, `DocumentsOpenApiShapeTests` · Real e2e `documents.real.e2e.spec.ts`
- State: HARDENING — block limit and windowing (UC-DCT-G06), identical-404 test, 1-px diff.

### UC-DCT-G02 List the pages of a workspace
- Capability: G Document query and loading
- Actors and permission: workspace member (`ViewWorkspace`), per-page `ViewPage` filter.
- Preconditions: none.
- Input: workspace id; cursor and page size 200 (lead decision LD15).
- Output: the active pages the caller may see, by title; trashed pages absent.
- Main flow: used by pickers, search seeds, favorites and import targets.
- No-op / rejection / concurrency / retry: an empty result is `200` with an empty list. Shipped code returns every page.
- UI states: loading, empty, error.
- Rules: product doc sections 7, 8; `DCREQ024`, `DCREQ025`.
- Acceptance: Given 450 pages, When listed, Then 3 pages of results of at most 200 each, none hidden-page data.
- Evidence: BE `GET /api/v1/workspaces/{workspaceId}/pages`, `GetWorkspacePagesQueryHandler` · FE `usePageList` · Design none · Tests `PageViewAccessTests`, `DocumentsOpenApiShapeTests` · Real e2e none
- State: INTEGRATING — cursor paging, real e2e.

### UC-DCT-G03 Search pages by title
- Capability: G Document query and loading
- Actors and permission: workspace member; results filtered by `ViewPage`.
- Preconditions: query of at least 2 characters.
- Input: `query`.
- Output: up to 25 active pages whose title contains the text (case insensitive; mark-insensitive is a later index step, decision 33), by title. Command search (⌘K) shows at most 5 docs, by title only.
- Main flow: the docs search box and the command palette call it.
- No-op / rejection / concurrency / retry: a shorter query returns an empty list; a page the caller cannot read never appears (not even by count). Search is derived state and never decides access (product rule DCT-021).
- UI states: loading, empty ("No match"), error.
- Rules: product doc section 67; `DCREQ140`, `DCREQ141`; decision 33.
- Acceptance: Given a private page "Salary plan", When another member searches "salary", Then no result.
- Evidence: BE `GET /api/v1/workspaces/{workspaceId}/pages/search`, `SearchPagesQueryHandler`, `PageSearchFactsAdapterTests` · FE `useDocsSearch`, `search.api.ts` · Design Final-Command · Tests `PageSearchFactsAdapterTests`, `PageViewAccessTests` · Real e2e none
- State: INTEGRATING — real e2e, mark-insensitive match, index for scale.

### UC-DCT-G04 Read the pages that reference this page
- Capability: G Document query and loading
- Actors and permission: Viewer of the page.
- Preconditions: none.
- Input: page id.
- Output: pages that name this page, readable by the caller only, with title, icon and Space (the department name is shown only when the caller can see that Space).
- Main flow: the editor side panel Links tab lists them.
- No-op / rejection / concurrency / retry: trashed referrers are absent; unreadable referrers are absent without a count.
- UI states: loading, empty ("No page links here"), error.
- Rules: product doc sections 20, 38, 40 (product rule DCT-014); `DCREQ122`, `DCREQ125`.
- Acceptance: Given page A references B, When B is opened, Then A is listed in B's backlinks; a reader who cannot see A does not see it.
- Evidence: BE `GET /api/v1/pages/{pageId}/backlinks`, `GetPageBacklinksQueryHandler` · FE `usePageBacklinks` · Design Final-Docs-2 (Liên kết tab) · Tests `PageViewAccessTests` · Real e2e `documents-trash-links.real.e2e.spec.ts`
- State: HARDENING — result bound, unreadable-referrer test over the API.

### UC-DCT-G05 Show the tasks and boards linked to a page
- Capability: G Document query and loading
- Actors and permission: Viewer of the page; each task or board shown only if the viewer may see it.
- Preconditions: items were linked to the page from Work Management.
- Input: page id.
- Output: "Related work" card with linked tasks and boards; unreadable ones show a locked row without title.
- Main flow: the page loads links owned by Work Management (the page stores no copy; link create and remove belong to Work Management, product rule DCT-020).
- No-op / rejection / concurrency / retry: removing a link removes the relation only (`DCREQ126`); a deleted target shows unavailable (`DCREQ127`). The Documents `ResourceLink` aggregate and its table are not used by any use case or route (Part 1, dead code).
- UI states: loading, empty, locked row, error.
- Rules: product doc sections 38 to 45 (product rules DCT-014, DCT-015); `DCREQ122` to `DCREQ127`. Owner: [work-management.md](work-management.md).
- Acceptance: Given a page linked to two items and one is private to others, Then the reader sees one row and one locked row.
- Evidence: BE `POST /api/v1/board-items/{itemId}/pages` (Work Management), item reads · FE `linkedTasks`, `linkedBoards` in `DocPageScreen` · Design Final-Docs-2 (Việc liên quan card), WM-Actions-LinkedDocs · Tests `ItemPageLinkTests`, `DocPageSurfaces` interaction tests · Real e2e none
- State: INTEGRATING — real e2e; decide the fate of the `ResourceLink` aggregate.

### UC-DCT-G06 Load a very large page (enterprise baseline)
- Capability: G Document query and loading
- Actors and permission: Viewer.
- Preconditions: none.
- Input: page id.
- Output (lead decision LD14): a page holds at most 5 000 live blocks (a create beyond it is refused); the block read returns them in order with a cursor of 1 000 blocks; the client renders the first window at once and loads the rest while scrolling.
- Main flow: automatic.
- No-op / rejection / concurrency / retry: the cap is a business-rule error with the limit named.
- UI states: loading more, error with retry.
- Rules: product doc section 99; `DCREQ024`; AGENTS.md section 25.
- Acceptance: Given a page with 5 000 blocks, When opened, Then the first window renders before the rest is fetched. When a 5 001st block is created, Then refused.
- Evidence: BE `GetPageBlocksQueryHandler` (reads all blocks, comment says the set is bounded; nothing enforces it) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — limit, cursor, windowed rendering.

### UC-DCT-G07 Search inside page text (enterprise baseline)
- Capability: G Document query and loading
- Actors and permission: workspace member.
- Preconditions: a derived search projection of block text exists.
- Input: query.
- Output: pages whose body matches, with a snippet; filtered by `ViewPage`; index lag never changes content (product rule DCT-021).
- Main flow: the docs search and ⌘K show body matches after title matches once built (ADR-010 keeps title-only for now, decision 33).
- No-op / rejection / concurrency / retry: a deleted page leaves the index; a revoked reader stops seeing the snippet.
- UI states: loading, empty, error.
- Rules: product doc sections 67, 68; `DCREQ140`, `DCREQ141`; Notion and Confluence search body text.
- Acceptance: Given the word "payroll" inside a readable page, When searched, Then the page with a snippet; for an unreadable page, nothing.
- Evidence: BE `PageSearchFactsAdapter` (title facts only) · FE none · Design none · Tests `SearchDocumentRecordTests` · Real e2e none
- State: DEFINED — projection, snippet, tests.

## Capability H: Editor state reconciliation

Team capability 8 (team id DCT-008). Requirements `DCREQ027` to `DCREQ031`. Decisions 1, 3, 5, 25, 26 and 75. The editor engine is TipTap 3 inside `docs-web` (decision 1); server state stays in the query cache, never in a second document store.

### UC-DCT-H01 Autosave edits and show the saved state
- Capability: H Editor state reconciliation
- Actors and permission: Editor.
- Preconditions: the page is open and editable.
- Input: typing, formatting, block moves.
- Output: edits are sent as block writes with their versions; the page bar shows "Saving" then "Saved" with the last editor and time; the word count updates.
- Main flow: the saver collects changed blocks, sends them (batch or single), takes the new versions from the answer, and keeps unsent typing in the editor.
- No-op / rejection / concurrency / retry: unchanged blocks are not sent; a failed write keeps the text, shows "Not saved" with Retry, and retries with the same payload.
- UI states: saving, saved, not saved with retry, offline, conflict, read-only.
- Rules: product doc section 99; `DCREQ027`, `DCREQ028`.
- Acceptance: Given typing, When the person leaves the block, Then the block is stored and the bar says Saved. Given the network drops, Then the bar says Not saved and the text stays.
- Evidence: BE block routes (D02, D03) · FE `doc-block-saver.ts`, `useDocSaver`, `doc-page-bar-surface.tsx` · Design Final-Docs-2 · Tests `doc-block-saver.unit.test.ts`, `doc-editor.component.test.tsx` · Real e2e `documents.real.e2e.spec.ts`
- State: HARDENING — offline and failure states in a real e2e, 1-px diff.

### UC-DCT-H02 Rebase after a stale save
- Capability: H Editor state reconciliation
- Actors and permission: Editor.
- Preconditions: another person (or another tab) changed the same block.
- Input: a `412` carrying the current block.
- Output: the editor replaces the block with the stored one and keeps the person's text in a visible conflict notice so nothing typed is lost; at most 5 rebases per save, then the save stops with a message.
- Main flow: the saver catches `BlockVersionConflict`, applies the stored block, re-applies the person's change when it does not overlap, otherwise raises `onConflict`.
- No-op / rejection / concurrency / retry: the person chooses to keep theirs (resend with the new version) or take the stored one.
- UI states: conflict notice with both texts, saving.
- Rules: product doc sections 31, 32 (product rule DCT-011); `DCREQ030`, `DCREQ031`; decision 3 (no CRDT, no OT).
- Acceptance: Given two editors on one block, When both save, Then the second sees the first's text, keeps their own in the notice, and no text is lost.
- Evidence: BE `PageBlockSet.Stale` (`412` with `currentBlock`) · FE `BlockVersionConflict`, `onConflict` in `doc-block-saver.ts` · Design none (notice not drawn) · Tests `doc-block-saver.unit.test.ts`, `BlockEditingHandlerTests` · Real e2e none
- State: INTEGRATING — conflict artboard, real e2e with two sessions.

### UC-DCT-H03 Roll back a failed optimistic write
- Capability: H Editor state reconciliation
- Actors and permission: Editor.
- Preconditions: an optimistic block create, delete, move or type change was shown before the server answered.
- Input: a rejected write (`400`, `404`, `412`, network).
- Output: the editor reverts that change to the last stored state and shows why; a temporary client id is replaced by the stored block's id (the editor chooses the id up front, so there is none to swap).
- Main flow: snapshot, optimistic patch, request, reconcile or roll back, refetch.
- No-op / rejection / concurrency / retry: a late answer from an earlier page or workspace never changes the current page.
- UI states: pending, error toast, conflict.
- Rules: product doc section 99; team spec section 13 "mutation reconciliation"; `DCREQ028`, `DCREQ029`.
- Acceptance: Given a block create that the server refuses, Then the block vanishes and the person sees the reason.
- Evidence: BE block routes · FE `useCreateBlock`, `useDeleteBlock`, `useReorderBlocks`, `useRefreshPageBlocks` · Design none · Tests `docs-blocks-trash.unit.test.ts`, `doc-block-saver.unit.test.ts` · Real e2e none
- State: INTEGRATING — real e2e of a rejected write.

### UC-DCT-H04 Edit while offline
- Capability: H Editor state reconciliation
- Actors and permission: Editor.
- Preconditions: the connection is lost with the page open.
- Input: typing while offline.
- Output (lead decision LD16): true offline editing is not supported. The bar says "Offline, changes not saved yet", unsent edits stay in the open tab, and the browser warns before the tab closes; on reconnect the saver sends them with their expected versions and conflicts follow UC-DCT-H02. Structural actions (create page, move, trash, import) are disabled while offline.
- Main flow: automatic on connection change.
- No-op / rejection / concurrency / retry: access is checked again when the writes arrive; revoked access follows UC-DCT-I07.
- UI states: offline banner, not saved, saving after reconnect.
- Rules: product doc section 101; `DCREQ031`.
- Acceptance: Given an offline typing session of one minute, When the connection returns, Then the text is saved or shown as a conflict, never silently dropped.
- Evidence: BE none · FE none (no offline state in `doc-page-bar-surface.tsx` evidence found) · Design none · Tests none · Real e2e none
- State: DEFINED — banner, leave warning, resend, tests.

### UC-DCT-H05 Find and replace in a page
- Capability: H Editor state reconciliation
- Actors and permission: Editor (replace), Viewer (find).
- Preconditions: none.
- Input: search text, case and whole-word options, replacement.
- Output: matches highlighted; replace one or all as normal block edits.
- Main flow: Edit menu, Find; panel with next and previous.
- No-op / rejection / concurrency / retry: replace-all is one batch; a stale block is rebased as UC-DCT-H02.
- UI states: no matches, read-only (replace hidden).
- Rules: product doc section 99.
- Acceptance: Given three matches, When Replace all runs, Then three runs change in one save.
- Evidence: BE batch route · FE `find-replace.ts`, `find-panel.tsx` · Design none · Tests `find-replace.component.test.tsx` · Real e2e none
- State: INTEGRATING — artboard, real e2e.

### UC-DCT-H06 Read a page on a phone app
- Capability: H Editor state reconciliation
- Actors and permission: Viewer.
- Preconditions: a mobile host with a session.
- Input: page id.
- Output (decisions 25, 26): read-only rendering of all block types known to the shared decoder; toggles always expanded; images and files through the host's `renderAttachment` (placeholder otherwise); a missing, forbidden or other-workspace page shows one identical "no access" state. The Expo route fetches blocks in the host.
- Main flow: open from the host's route.
- No-op / rejection / concurrency / retry: unknown types show the placeholder (UC-DCT-E09); no writes exist.
- UI states: loading, error, no access, empty.
- Rules: product doc sections 100, 102; `DCREQ017`; decisions 25, 26.
- Acceptance: Given a page with a table and a toggle, When opened on mobile, Then both render and nothing is editable.
- Evidence: BE `GET /api/v1/pages/{pageId}`, `GET /api/v1/pages/{pageId}/blocks` · FE `MobileDocumentScreen` in `docs-mobile` · Design Final-M-Docs · Tests `docs-mobile-screen.mobile.test.tsx`, `docs-mobile-renderer.mobile.test.tsx`, `docs-mobile-layout.mobile.test.tsx`, `docs-mobile-contract.mobile.test.ts` · Real e2e none
- State: INTEGRATING — device or native e2e, 1-px diff at 390.

### UC-DCT-H07 Use the 320 px page and its overflow sheet
- Capability: H Editor state reconciliation
- Actors and permission: Viewer, Editor.
- Preconditions: viewport 320 to 480 px.
- Input: taps.
- Output (decision 75): Back to Docs, Comments with count and More; the side panel is a bottom sheet (Links, Contents, Comments) at most 85 percent of the viewport; Share, Approve, Contents and History live in the More sheet; no extra tab bar; the global bottom bar is hidden on Docs; menu and tool rows scroll sideways with a fade; every control is at least 44 px.
- Main flow: automatic by viewport.
- No-op / rejection / concurrency / retry: none.
- UI states: sheet open and closed, loading, read-only.
- Rules: product doc sections 99, 102; decision 75.
- Acceptance: Given 320 px, When the page opens, Then no horizontal page scroll and the sheet reaches Links, Contents and Comments.
- Evidence: BE none · FE `doc-side-panel-surface.tsx`, `doc-page-bar-surface.tsx`, `use-doc-panel-open.ts` · Design Final-Docs-2-320, Final-Docs-2-320-Sheet, Final-M-Docs, Final-Docs-2-Notes · Tests `use-doc-panel-open.component.test.tsx`, `doc-page-surfaces.interaction.component.test.tsx`, `design-fidelity-docs.mock.e2e.spec.ts`, `docs-page.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — real e2e at 320, 1-px diff, closable right panel on desktop remembered per viewer (decision 5).

### UC-DCT-H08 Edit with keyboard and screen reader
- Capability: H Editor state reconciliation
- Actors and permission: Editor.
- Preconditions: none.
- Input: keys: formatting and block shortcuts from the shortcut catalog, Alt+Shift+Arrow to move a block, Alt+Enter on a page chip, Cmd or Ctrl+click on a chip.
- Output: every action has a keyboard route; moves and saves are announced in a live region; headings, lists, checklists and code keep their semantics; no shortcut hint appears on buttons (tooltips, `aria-keyshortcuts` and the shortcut sheet carry them).
- Main flow: automatic.
- No-op / rejection / concurrency / retry: none.
- UI states: focus visible, announcements, reduced motion.
- Rules: product doc section 102; `docs/quality/accessibility-standard.md`.
- Acceptance: Given the editor, When a block moves by keyboard, Then the new place is announced and no axe serious violation is reported.
- Evidence: BE none · FE `shortcut-catalog.ts`, `doc-keys.ts`, `block-handle.tsx` · Design Final-Docs-2 · Tests `block-move.component.test.tsx`, `doc-menu-bar.component.test.tsx` · Real e2e none
- State: INTEGRATING — axe run on the real stack.

## Capability I: Document permissions

Team capability 9 (team id DCT-009). Requirements `DCREQ032` to `DCREQ035`, `DCREQ114` to `DCREQ116`, `DCREQ125`, `DCREQ191`. Permission meaning and roles are in [governance](../../product/governance.md). Documents names the actions: `ViewPage`, `CreatePage`, `UpdatePage` (all block writes, versions, templates), `ArchivePage` (trash and restore), `DeletePage` (permanent). Roles: Viewer reads, Editor edits and trashes, Manager also deletes and shares.

### UC-DCT-I01 See a page only when allowed
- Capability: I Document permissions
- Actors and permission: any person; `ViewPage` is decided per page by Governance (inherited from Space or workspace, explicit, shared).
- Preconditions: none.
- Input: any read of a page, its blocks, versions, breadcrumb, backlinks, tree, list, search or trash.
- Output: allowed reads return data; every other case returns the identical `404` (missing, other workspace, no access), and lists leave such pages out without counts. Visibility is one input, not the decision (product rules DCT-004, DCT-019).
- Main flow: the pipeline authorizes before the handler; list handlers filter with the visible-page set.
- No-op / rejection / concurrency / retry: a person in another tenant gets `404` and never an existence hint.
- UI states: one "no access" state, empty lists.
- Rules: product doc sections 12, 13, 59, 60; `DCREQ032` to `DCREQ035`; GOV-029; NRX-003, NRX-004.
- Acceptance: Given a page and a member without access, When they request it, Then `404` identical to a random id. Given a user of another account, Then the same.
- Evidence: BE `IRequirePermission` on every Documents request, `PostgresPageAuthorizationFacts`, `IResourceVisibility` · FE no-access state in `DocPageScreen` · Design Final-Docs-2 · Tests `PageViewAccessTests`, `PageSearchFactsAdapterTests` · Real e2e `documents.real.e2e.spec.ts` (guest sees it only once shared)
- State: HARDENING — cross-tenant negative test over the API for every route, RLS test.

### UC-DCT-I02 Edit only with edit permission
- Capability: I Document permissions
- Actors and permission: Editor (`UpdatePage`).
- Preconditions: none.
- Input: any block write, version save or restore, template save, move to space.
- Output: refused with `404` for people without the right; read-only editor for Viewers.
- Main flow: the pipeline authorizes; the client hides write controls from the permission state (UX only).
- No-op / rejection / concurrency / retry: rename (`UpdatePage` command) is authorized with `ManageBoard`, a Work Management action, on a page (drift; lead decision LD6 sets it to `UpdatePage`). `MovePage`, `PublishPage` and `SetPageDeadline` also ask for `ManageBoard` but throw before use.
- UI states: read-only, permission denied.
- Rules: product doc section 59; `DCREQ032`, `DCREQ033`, `DCREQ191`.
- Acceptance: Given a Viewer, When they patch a block, Then `404` and no change. Given an Editor who is not a board manager, When they rename the page, Then it succeeds.
- Evidence: BE `UpdatePageCommand`, `CreateBlockCommand` and the other commands (`Action`) · FE permission state in `usePagePermissions` · Design none · Tests `ArchivePageCommandHandlerTests`, `BlockEditingHandlerTests` · Real e2e none
- State: INTEGRATING — fix rename action, matrix test of every request against its action, real e2e.

### UC-DCT-I03 Share a page with a member
- Capability: I Document permissions
- Actors and permission: Manager of the page.
- Preconditions: the person is a workspace member (outside people are guests, Governance).
- Input: person and level (view, comment, edit, manage as the share dialog offers).
- Output: an explicit permission on `documents.page`; the person can then open the page; revoking removes it.
- Main flow: Share button, add person, choose level. Governance owns the grant and its rules.
- No-op / rejection / concurrency / retry: see [governance.md](governance.md).
- UI states: share dialog states from Final-Share; permission denied for non-Managers.
- Rules: product doc sections 59, 107; `DCREQ115`, `DCREQ116`.
- Acceptance: Given a page and a guest without access, When the owner shares it, Then the guest can open it; before that they get `404`.
- Evidence: BE `POST /api/v1/resources/documents.page/{pageId}/permissions` (Governance) · FE `useSharePage`, `usePagePermissions`, `doc-share-dialog-surface.tsx` · Design Final-Share, Final-M-Share · Tests `design-fidelity-share.mock.e2e.spec.ts` · Real e2e `documents.real.e2e.spec.ts`
- State: HARDENING — owned and proven by Governance; a fallback read of `/pages/{id}/permissions` that has no backend route is dead code.

### UC-DCT-I04 Creator becomes Manager
- Capability: I Document permissions
- Actors and permission: the person who creates a page (or a page from a template).
- Preconditions: none.
- Input: create.
- Output: a Manager grant on the page in the same unit of work, so the creator can share and manage it.
- Main flow: automatic.
- No-op / rejection / concurrency / retry: if the grant fails, the page is not created.
- UI states: none.
- Rules: product doc section 59; `DCREQ032`.
- Acceptance: Given a new page, When the creator opens its Share dialog, Then they are Manager.
- Evidence: BE `IResourceCreatorGrant.GrantManager` in `CreatePageCommandHandler`, `CreatePageFromTemplateCommandHandler` · FE none · Design none · Tests `CreatePageTargetPolicyMatrixTests` · Real e2e none
- State: INTEGRATING — assert the grant in an integration test.

### UC-DCT-I05 Embedding a board never shares it
- Capability: I Document permissions
- Actors and permission: system rule.
- Preconditions: a page embeds or references another resource.
- Input: a reader opens the page.
- Output (lead decision LD27): access to the page never grants access to an embedded board, item or page; the embed re-checks the reader's own right on the target at render; Public visibility (if ever enabled) never reaches a private target (product rule DCT-015).
- Main flow: the embed host calls the target's own read as the reader.
- No-op / rejection / concurrency / retry: unauthorized targets show "unavailable".
- UI states: unavailable.
- Rules: product doc sections 40, 41 (product rule DCT-015); `DCREQ125`.
- Acceptance: Given a shared page that embeds a private board, When the guest opens it, Then no board data is sent.
- Evidence: BE none dedicated · FE `doc-embeds` · Design none · Tests `doc-embeds.component.test.tsx` · Real e2e none
- State: DEFINED — server and e2e proof.

### UC-DCT-I06 Read-only and trashed page states
- Capability: I Document permissions
- Actors and permission: Viewer; any person on a trashed page.
- Preconditions: none.
- Input: open a page without edit right, or follow a link to a trashed page.
- Output: Viewers get the editor without write controls and a note "You can read this page"; a trashed page is not readable (`404`); people who may restore it reach it from the trash list.
- Main flow: automatic.
- No-op / rejection / concurrency / retry: right changes while open follow UC-DCT-I07.
- UI states: read-only, no access.
- Rules: product doc section 77.
- Acceptance: Given a Viewer, When the page opens, Then every write control is absent and typing is blocked.
- Evidence: BE `GET /api/v1/pages/{pageId}` · FE read-only note in `DocPageScreen` · Design Final-Docs-2 ("Bạn thấy trang…" note) · Tests `doc-page-surfaces.interaction.component.test.tsx`, `docs-page.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — real e2e as Viewer.

### UC-DCT-I07 Lose access while the page is open
- Capability: I Document permissions
- Actors and permission: Editor whose right is revoked.
- Preconditions: the page is open.
- Input: the next write or refresh after revocation.
- Output (lead decision LD25): the write is `404`; the editor goes read-only or to the "no access" state, copies unsent text to a "Copy my unsaved text" action first, and stops live updates; a realtime frame or the next read triggers the check.
- Main flow: automatic.
- No-op / rejection / concurrency / retry: none.
- UI states: no access, unsaved text notice.
- Rules: product doc section 76 (permission revocation while connected); `DCREQ035`.
- Acceptance: Given an Editor typing, When access is revoked, Then the next save is refused, the editor locks, and their last text is copyable.
- Evidence: BE authorization pipeline (answers `404`) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — UI behaviour and e2e.

## Capability J: Document events and activity

Team capability 10 (team id DCT-010). Requirements `DCREQ128` to `DCREQ134`, `DCREQ136`, `DCREQ137`. Version history is user-facing document history; it is not the Governance audit trail (product rule DCT-031). A history entry never changes a page or block concurrency version (product rule DCT-016).

### UC-DCT-J01 Announce a saved or created page
- Capability: J Document events and activity
- Actors and permission: system.
- Preconditions: a committed change.
- Input: page create, trash, and every block save that changed something.
- Output: integration events `PageCreated`, `PageArchived`, `PageBlocksSaved` (page, actor, mentions added) through the outbox after commit; consumers: automatic version (J03), mention notification (E08); the created and archived consumers only log. A save that changed nothing announces nothing.
- Main flow: the Domain raises, the outbox publishes, consumers run once per message.
- No-op / rejection / concurrency / retry: duplicates are ignored by consumer identity; events carry facts, not trees (product rule DCT-023).
- UI states: none.
- Rules: product doc sections 72, 73 (product rules DCT-023, DCT-025); `DCREQ137`.
- Acceptance: Given a block edit, When committed, Then one `PageBlocksSaved`; given a no-op edit, Then none.
- Evidence: BE `PageEventMapper`, `PageBlocksSavedIntegrationEvent`, `BlockSaveTracker` · FE none · Design none · Tests `PageBlocksSavedTests`, `PageBlocksSavedConsumersTests`, `PageCreatedOutboxEvidenceTests`, `PageArchivedOutboxEvidenceTests`, `DocumentsCollaborationEventPinningArchitectureTests` · Real e2e none
- State: INTEGRATING — real-stack proof of delivery, reorder does not announce (F02).

### UC-DCT-J02 Announce rename, move, restore and delete
- Capability: J Document events and activity
- Actors and permission: system.
- Preconditions: a committed change.
- Input: page rename, move, space change, restore, permanent delete; block create, change, move, delete.
- Output: integration events with stable names and versions for the facts Automation, Analytics and Collaboration activity consume. The Domain raises the page events; no integration event exists for them.
- Main flow: map each domain event to a versioned integration event with a consumer list.
- No-op / rejection / concurrency / retry: contracts follow the compatibility policy of NRX-007 and NRX-008.
- UI states: none.
- Rules: product doc section 72; `DCREQ137`; team spec section 15.
- Acceptance: Given a rename, When committed, Then `PageRenamed` is published once with old and new title.
- Evidence: BE Domain `Page*DomainEvent` files only · FE none · Design none · Tests `PageTests` (domain events) · Real e2e none
- State: DEFINED — event contracts, mapper, pinning test, first consumer decision with Automation and Collaboration.

### UC-DCT-J03 Capture an automatic version
- Capability: J Document events and activity
- Actors and permission: system, as the editor of the save.
- Preconditions: the page has unsnapshotted edits and at least 10 minutes passed since the last entry (decision 13).
- Input: a `PageBlocksSaved` message.
- Output: an Automatic history entry holding the page title and live blocks; no entry when the content equals the newest entry; history is kept in full for now (decision 16). The "last editor leaves" trigger waits for a presence signal (decision 19).
- Main flow: the consumer calls `CaptureAutomaticAsync`.
- No-op / rejection / concurrency / retry: duplicate messages create no second entry for the same content.
- UI states: the entry appears in history marked Automatic.
- Rules: product doc sections 46 to 51 (product rules DCT-016, DCT-017); `DCREQ128` to `DCREQ131`; decisions 13, 16, 19.
- Acceptance: Given edits after 10 quiet minutes, When saved, Then one Automatic entry; given identical content, Then none.
- Evidence: BE `PageBlocksSavedConsumers`, `PageVersionRecorder.CaptureAutomaticAsync` · FE history list shows it · Design none · Tests `PageVersionHandlerTests`, `PageBlocksSavedConsumersTests`, `DocumentVersionTests` · Real e2e none
- State: INTEGRATING — real e2e, presence trigger.

### UC-DCT-J04 Save a named version
- Capability: J Document events and activity
- Actors and permission: Editor (`UpdatePage`).
- Preconditions: the page is active.
- Input: `name` (required, at most the domain limit).
- Output: a Named entry with the next number holding the page as it is now.
- Main flow: File menu, Save version; name it.
- No-op / rejection / concurrency / retry: empty or too long name is `400` (`Documents_Version_NameRequired`, `Documents_Version_NameTooLong`); a trashed page is refused. Saving twice makes two entries.
- UI states: validation error, pending, permission denied.
- Rules: product doc sections 48, 49; `DCREQ129`, `DCREQ130`; decision 13.
- Acceptance: Given a page, When "Before launch" is saved, Then the list shows it newest first.
- Evidence: BE `POST /api/v1/pages/{pageId}/versions`, `CreatePageVersionCommandHandler` · FE `useCreateVersion`, `doc-history-dialogs-surface.tsx` · Design none · Tests `PageVersionHandlerTests`, `page-versions.unit.test.ts`, `doc-history-dialogs.interaction.component.test.tsx` · Real e2e none
- State: INTEGRATING — real e2e, dialog artboard.

### UC-DCT-J05 Browse the version history
- Capability: J Document events and activity
- Actors and permission: Viewer.
- Preconditions: none.
- Input: page id.
- Output: entries newest first with number, kind (Automatic, Named, Restore), name, author and time.
- Main flow: File menu or More, Version history.
- No-op / rejection / concurrency / retry: the list is not paged today.
- UI states: loading, empty, error.
- Rules: product doc section 48; `DCREQ132`.
- Acceptance: Given three saves, When opened, Then three entries newest first.
- Evidence: BE `GET /api/v1/pages/{pageId}/versions`, `ListPageVersionsQueryHandler` · FE `usePageVersions` · Design none · Tests `PageVersionHandlerTests`, `page-versions.unit.test.ts` · Real e2e none
- State: INTEGRATING — cursor paging, artboard, real e2e.

### UC-DCT-J06 View a version
- Capability: J Document events and activity
- Actors and permission: Viewer.
- Preconditions: the entry exists on this page.
- Input: version id.
- Output: the captured title and blocks, read-only; a snapshot is immutable and not a second live document (product rule DCT-017).
- Main flow: choose an entry; the read-only view opens; Restore is offered to Editors (UC-DCT-L01).
- No-op / rejection / concurrency / retry: an entry of another page is `404`.
- UI states: loading, error, read-only.
- Rules: product doc sections 49, 50; `DCREQ130`, `DCREQ131`.
- Acceptance: Given a version, When viewed, Then no control edits it.
- Evidence: BE `GET /api/v1/pages/{pageId}/versions/{versionId}`, `GetPageVersionQueryHandler` · FE `useVersionSnapshot` · Design none · Tests `PageVersionHandlerTests`, `DocumentSnapshotTests`, `DocumentVersionImmutabilityTests` · Real e2e none
- State: INTEGRATING — real e2e, artboard.

### UC-DCT-J07 Compare two versions (enterprise baseline)
- Capability: J Document events and activity
- Actors and permission: Viewer.
- Preconditions: two entries.
- Input: two version ids, or one against the current page.
- Output (lead decision LD21): a read-only side-by-side text comparison with added, removed and changed blocks marked; Restore stays a separate Editor action.
- Main flow: History, select two, Compare.
- No-op / rejection / concurrency / retry: identical entries show "No differences".
- UI states: loading, no differences, error.
- Rules: product doc section 48; Confluence compares any two versions and Google Docs shows named versions.
- Acceptance: Given two versions that differ in one paragraph, When compared, Then that paragraph is marked.
- Evidence: BE none (reads of two versions suffice) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — view, artboard, tests.

### UC-DCT-J08 Read page activity
- Capability: J Document events and activity
- Actors and permission: Viewer.
- Preconditions: none.
- Input: page id.
- Output (lead decision LD20): a short feed of who renamed, moved, trashed or restored the page, owned by Collaboration activity like the item history, built from the facts of UC-DCT-J02. The shipped `GET /pages/{pageId}/history` always returns an empty list and is retired; content history is the version list (J05).
- Main flow: not built.
- No-op / rejection / concurrency / retry: unreadable pages give `404`.
- UI states: loading, empty, error.
- Rules: product doc sections 97, 98 (product rule DCT-031: history is not audit); `DCREQ132`, `DCREQ134`.
- Acceptance: Given a rename, When the feed loads, Then a line "X renamed the page".
- Evidence: BE `GET /api/v1/pages/{pageId}/history`, `GetPageHistoryQueryHandler` (empty list) · FE `createHistoryApi`, `usePageHistory` · Design none · Tests `PageHistoryAndTemplatesContractOpenApiTests` · Real e2e none
- State: IMPLEMENTING — retire the empty endpoint, deliver through Collaboration activity.

## Capability K: Document realtime

Team capability 11 (team id DCT-011). Requirements `DCREQ030`, `DCREQ031`. Realtime transport belongs to Platform. Frames carry no payload: the client rereads what changed from Documents (product rules DCT-012, DCT-024). Presence and cursors are Collaboration: see [collaboration.md](collaboration.md).

### UC-DCT-K01 See other people's changes on an open page
- Capability: K Document realtime
- Actors and permission: Viewer or Editor with the page open.
- Preconditions: a realtime session on the workspace stream.
- Input: a frame about this page's id.
- Output: the client refetches the blocks (and tree on structure change); the editor shows the stored result and never patches from the frame.
- Main flow: `watchDocumentChanges` listens on the workspace stream, keeps frames whose `resourceId` is the page, and calls refresh.
- No-op / rejection / concurrency / retry: duplicate and out-of-order frames only cause an extra reread; the person's unsent typing in a block is kept while that block is open.
- UI states: "Updated by X" notice, loading refresh.
- Rules: product doc sections 74, 75 (product rules DCT-012, DCT-024); `DCREQ030`.
- Acceptance: Given two clients on a page, When A saves a block, Then B shows A's text without reload.
- Evidence: BE `PageBlocksSaved` announced; the gateway frame for it is not evidenced in Documents code · FE `useDocLive`, `watchDocumentChanges` in `docs-collaboration` · Design none · Tests `docs-collaboration.unit.test.ts` · Real e2e none
- State: INTEGRATING — confirm the backend frame, real e2e with two sessions.

### UC-DCT-K02 Recover after a dropped connection
- Capability: K Document realtime
- Actors and permission: Viewer or Editor.
- Preconditions: the connection dropped and returned, or frames were missed.
- Input: reconnect.
- Output: on reconnect the client refetches the page's blocks and tree and converges to the stored state; missed frames never corrupt state.
- Main flow: reconnect triggers `refresh` (UC-DCT-K01).
- No-op / rejection / concurrency / retry: unsent edits follow UC-DCT-H04.
- UI states: reconnecting, refreshed.
- Rules: product doc sections 75, 76 (product rule DCT-024).
- Acceptance: Given B offline while A saves three times, When B reconnects, Then B shows A's final state.
- Evidence: BE reads · FE `useDocLive` · Design none · Tests `docs-collaboration.unit.test.ts` · Real e2e none
- State: INTEGRATING — real e2e.

### UC-DCT-K03 Edit together with live cursors
- Capability: K Document realtime
- Actors and permission: Editors of one page.
- Preconditions: none.
- Input: simultaneous typing in the same block.
- Output: not offered. Block versions arbitrate (decision 3); presence is ephemeral and never concurrency truth (product doc section 35).
- Main flow: not defined.
- No-op / rejection / concurrency / retry: not defined.
- UI states: not applicable.
- Rules: product doc sections 33, 34 (product rule DCT-012); `DCREQ031`.
- Acceptance: not writable until an ADR defines authority, merge, offline and history.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: BLOCKED — needs the ADR that decision 3 names (CRDT or OT, merge, offline, permission revocation, history relationship).

## Capability L: Recovery and conflict hardening

Team capability 12 (team id DCT-012). Requirements `DCREQ133`, `DCREQ136`, `DCREQ137`, `DCREQ030`.

### UC-DCT-L01 Restore a version with the guard
- Capability: L Recovery and conflict hardening
- Actors and permission: Editor (`UpdatePage`).
- Preconditions: the page is active; the entry belongs to this page.
- Input: `currentBlocks`: `[{blockId, version}]` for every live block the editor saw (decision 16).
- Output: the page's blocks are put back to the entry; a "Before restoring version N" Automatic entry is recorded first unless the newest entry already holds the current content; a Restore entry is added; history is never rewritten; the page announces a save.
- Main flow: 1. History, choose an entry, Restore. 2. Confirm. 3. The client sends the seen blocks. 4. The editor shows the restored page.
- No-op / rejection / concurrency / retry: any live block that changed, appeared or vanished since is `412` carrying the current blocks, and nothing is restored; the client rereads and asks again. A trashed page is refused. Retry after success restores again and adds entries.
- UI states: confirmation, pending, conflict, permission denied, error.
- Rules: product doc sections 52, 95 (product rules DCT-016, DCT-030); `DCREQ133`; decisions 13, 16.
- Acceptance: Given a block edited after the client's read, When restore is sent, Then `412` and no block changed. Given a matching set, Then the blocks equal the entry and two entries are added.
- Evidence: BE `POST /api/v1/pages/{pageId}/versions/{versionId}/restore`, `RestorePageVersionCommandHandler` · FE `useRestoreVersion`, `doc-history-dialogs-surface.tsx` · Design none · Tests `PageVersionHandlerTests`, `page-versions.unit.test.ts`, `doc-history-dialogs.interaction.component.test.tsx` · Real e2e none
- State: INTEGRATING — real e2e, check that a restored block type or property is still valid under today's schema (product rule DCT-030), artboard.

### UC-DCT-L02 Refuse a stale block write
- Capability: L Recovery and conflict hardening
- Actors and permission: Editor.
- Preconditions: the block changed since the editor read it.
- Input: a write with an old `expectedVersion`.
- Output: `412` with the current block; nothing changes; the editor rebases (UC-DCT-H02). The same holds for rename and move to space (page version).
- Main flow: the server compares the version before applying.
- No-op / rejection / concurrency / retry: a missing version is `400`. Block reorder, single delete and page rename have no version today (Part 1).
- UI states: conflict.
- Rules: product doc sections 31, 32 (product rule DCT-011); `DCREQ030`, `DCREQ136`.
- Acceptance: Given two writes with version 5, Then the first wins and the second is `412`.
- Evidence: BE `PageBlockSet.EnsureVersion`, `IExpectedVersionRequest` on `MovePageToSpaceCommand` · FE `BlockVersionConflict` · Design none · Tests `BlockEditingHandlerTests`, `ExpectedVersionArchitectureTests`, `ExpectedVersionConcurrencyIntegrationTests`, `EfRequestDataSessionExpectedVersionTests` · Real e2e none
- State: INTEGRATING — cover every competing write, real e2e.

### UC-DCT-L03 Apply a batch completely or not at all
- Capability: L Recovery and conflict hardening
- Actors and permission: Editor.
- Preconditions: a batch of operations.
- Input: see UC-DCT-D03.
- Output: no partial write: a failing operation throws before the unit of work commits.
- Main flow: the data session owns the transaction.
- No-op / rejection / concurrency / retry: concurrent batches on one block: the second gets `412`.
- UI states: conflict, error.
- Rules: product doc section 21A; `DCREQ028`.
- Acceptance: Given a batch with one invalid block, Then none of the blocks is stored.
- Evidence: BE `BatchUpdateBlocksCommandHandler` · FE `batch-block-writer.ts` · Design none · Tests `BatchUpdateBlocksHandlerTests`, `BlockMoveFailureAtomicityTests` · Real e2e none
- State: INTEGRATING — integration test against the database for rollback, real e2e.

### UC-DCT-L04 Retry a create without a second effect
- Capability: L Recovery and conflict hardening
- Actors and permission: Editor, workspace member.
- Preconditions: a create request was sent and the answer was lost.
- Input: the same request again.
- Output (lead decision LD26): create page accepts an `Idempotency-Key` header and returns the same page id for the same key and body for 24 hours; a block create or batch upsert that repeats an id with the same content returns the stored block instead of `409`; the same id with different content is `409`.
- Main flow: the client sends one key per user action.
- No-op / rejection / concurrency / retry: today a page create makes a second page, and a repeated block id is `409`.
- UI states: pending, retry.
- Rules: product doc section 101; NRX-010; `DCREQ029`.
- Acceptance: Given two identical page creates with one key, Then one page.
- Evidence: BE `CreateBlockCommandHandler` (id check), `CreatePageCommandHandler` (none) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — keys, replay rule, tests.

### UC-DCT-L05 Retain and delete the dependents of a page
- Capability: L Recovery and conflict hardening
- Actors and permission: system.
- Preconditions: a page is deleted or its workspace is closed.
- Input: retention policy.
- Output: blocks, versions, snapshots, template snapshots, search projections, exports, attachments, comments and backups follow a defined retain, tombstone, hide or purge rule (product rule DCT-028).
- Main flow: not defined.
- No-op / rejection / concurrency / retry: not defined.
- UI states: not applicable.
- Rules: product doc sections 89, 90, 96; `DCREQ004`, `DCREQ134`.
- Acceptance: not writable until the retention windows are decided.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: BLOCKED — owner decides retention windows and backup handling (data destruction), with Collaboration and Platform.

# 4. Product decisions applied

Owner decisions come from the lead's decision record of 2026-10-26 (item 75 dated 2026-10-29). The numbers below are that record's item numbers.

## 4.1 Owner decisions

- Product decision (owner, 2026-10-26): item 1, the editor engine is TipTap 3, product-local in `docs-web`; it moves to the shared UI package only when a second consumer appears (UC-DCT-H01).
- Product decision (owner, 2026-10-26): item 2, content stays Page plus typed Block rows; block content is rich-text v1 validated per block type by the server; legacy flat flags were upgraded by mapper and data migration (UC-DCT-E01).
- Product decision (owner, 2026-10-26): item 3, concurrency is an expected version per block (`412` plus the current block); no CRDT or OT now; live co-editing needs a later ADR (UC-DCT-D02, K03, L02).
- Product decision (owner, 2026-10-26): item 4, images and files go through Collaboration attachments; never base64 in content (UC-DCT-E05).
- Product decision (owner, 2026-10-26): item 5, the docs right panel is closable and its state is remembered per viewer (UC-DCT-H07).
- Product decision (owner, 2026-10-26): item 10, attachments have a second target kind `documents.page`; upload and delete need edit, list and read need view; 100 MB, blocked extensions; deleting a block does not hard-delete the object; no public URLs (UC-DCT-E05).
- Product decision (owner, 2026-10-26): item 11, a page reference has one generic "unavailable" fallback so existence is not leaked (UC-DCT-E07, E06).
- Product decision (owner, 2026-10-26): item 12, a mention in a block notifies the person once, when newly added in a saved edit, only if they can view the page, never for self-mentions, no retraction (UC-DCT-E08).
- Product decision (owner, 2026-10-26): item 13, an automatic snapshot is taken when a page has unsnapshotted edits and either 10 minutes passed or the last editor leaves; a manual named version; restore creates a new version and needs edit (UC-DCT-J03, J04, L01).
- Product decision (owner, 2026-10-26): item 14, workspace templates are made from a page by editors; a page from a template copies blocks (UC-DCT-A10, A11).
- Product decision (owner, 2026-10-26): item 15, export is Markdown and sanitised HTML, PDF through the print stylesheet; import is Markdown and `.docx` into a new page through rich-text v1 validation (UC-DCT-A13, A14).
- Product decision (owner, 2026-10-26): item 16, the restore request carries `currentBlocks [{blockId, version}]` for every live block; any mismatch is `412` and nothing is restored; a "before restoring" entry is recorded first; all versions are kept for now (UC-DCT-L01, J03).
- Product decision (owner, 2026-10-26): item 17, templates copy text, table and embed blocks only; image, file and video blocks are not copied and `droppedBlockCount` is returned; this supersedes the re-link part of item 14 (UC-DCT-A09, A11).
- Product decision (owner, 2026-10-26): item 18, Word import drops images with a notice; nested Markdown lists flatten; `.txt` imports as Markdown; a leading `# heading` becomes the page title (UC-DCT-A13).
- Product decision (owner, 2026-10-26): item 19, the "last editor leaves" snapshot waits for a presence signal; the 10-minute rule on the next save covers it until then (UC-DCT-J03).
- Product decision (owner, 2026-10-26): item 20, comment delete is offered only to the author in the docs panel; resolved threads hide behind "Show resolved"; reopen mirrors resolve (Collaboration use cases; see [collaboration.md](collaboration.md)).
- Product decision (owner, 2026-10-26): item 21, page reference chips: plain click navigates in read-only mode; while editing Cmd or Ctrl+click navigates; Alt+Enter opens the chip under the caret (UC-DCT-E07, H08).
- Product decision (owner, 2026-10-26): item 22, block reorder uses a hover grip outside the tab order and Alt+Shift+Arrow Up or Down; moves are announced in a live region; touch drag later (UC-DCT-F02, H08).
- Product decision (owner, 2026-10-26): item 23, the template icon is carried to the page created from it (UC-DCT-A11).
- Product decision (owner, 2026-10-26): item 24, comment reopen mirrors resolve exactly (Collaboration).
- Product decision (owner, 2026-10-26): item 25, mobile Docs is read-only for now: toggles always expanded, images and files through a host `renderAttachment`; the Expo route fetches blocks in the host (UC-DCT-H06).
- Product decision (owner, 2026-10-26): item 26, block wire decoding is owned by docs-core and shared by web and mobile; missing, forbidden and other-workspace pages show one identical "no access" state on mobile (UC-DCT-H06, E09).
- Product decision (owner, 2026-10-26): item 33, command search items, docs and boards the caller may open, caps 6, 5 and 5; docs by title only for now (UC-DCT-G03).
- Product decision (owner, 2026-10-26): item 42, mentions list only people who can view the resource; a mention of someone who cannot view it is kept as text and sends nothing (UC-DCT-E08).
- Product decision (owner, 2026-10-29): item 75, the shipped Docs code is the product of record and Final-Docs is superseded; mobile Docs at 320 follows Final-M-Docs (Back button, overflow menu, bottom sheet for Links, Contents, Comments; no extra tab bar; the global bottom bar hidden); control borders keep the system `--line2` token; the approved canvas v98 has Final-Docs-2, -Dem, -320, -320-Sheet, -Notes; at 320 Share, Approve, Contents and History live in the More sheet and the side sheet is at most 85 percent of the viewport height (UC-DCT-H07).

## 4.2 Lead decisions

- Lead decision (2026-10-30): LD1, trashing a page trashes its whole subtree as one unit; restoring the parent restores the subtree; permanent delete removes the subtree; a child is restored alone only when its parent is active, and a page whose parent is gone is restored at the root of its Space (UC-DCT-A04, A05, A08).
- Lead decision (2026-10-30): LD2, pages nest at most 10 levels; a create or move beyond it is refused (UC-DCT-B05).
- Lead decision (2026-10-30): LD3, sibling pages sort by title (case and mark insensitive); manual page ordering is not offered until the owner asks for it (UC-DCT-B01).
- Lead decision (2026-10-30): LD4, moving a page under another parent needs `UpdatePage` on the page and `CreatePage` on the new parent, the same workspace, the page's expected version and a cycle check from supplied ancestry; the subtree follows the new root's Space (UC-DCT-C02).
- Lead decision (2026-10-30): LD5, a page is never moved to another workspace; people export and import or duplicate instead (UC-DCT-C03).
- Lead decision (2026-10-30): LD6, rename is authorized with `UpdatePage` and carries the page `expectedVersion`; a stale version is `412` with the current page (UC-DCT-A02, I02).
- Lead decision (2026-10-30): LD7, a duplicate is titled "Copy of <title>", copies text, table and embed blocks, copies no image, file, video, comment, version, attachment or permission, keeps page references and board embeds pointing at the original targets, and makes the caller Manager (UC-DCT-A09).
- Lead decision (2026-10-30): LD8, a page has an icon (one emoji or icon token, at most 32 characters) that Editors can set; cover images are deferred (UC-DCT-A03).
- Lead decision (2026-10-30): LD9, a template is managed (rename, archive, delete) by its creator or a Workspace Admin; archiving never changes pages already made from it (UC-DCT-A12).
- Lead decision (2026-10-30): LD10, deleting a block deletes the blocks under it in every route; the single and batch routes behave the same (UC-DCT-D04).
- Lead decision (2026-10-30): LD11, reorder keeps the client's sort key only when it lies between the siblings the request names, carries the block expected version, is a no-op when the place is unchanged, and announces one page save (UC-DCT-F02).
- Lead decision (2026-10-30): LD12, every block write on a trashed page is refused by the handler with `Documents_Page_CannotEditArchived` (UC-DCT-D05).
- Lead decision (2026-10-30): LD13, search stays title-only with at least 2 characters and 25 results until a derived index exists; body search then returns snippets filtered by `ViewPage` (UC-DCT-G03, G07).
- Lead decision (2026-10-30): LD14, a page holds at most 5 000 live blocks; imports are at most 10 MB; the block read pages by cursor of 1 000 above that size (UC-DCT-G06, A13).
- Lead decision (2026-10-30): LD15, the page list is paged by cursor with 200 per page; the tree stays whole until a workspace has more than 2 000 pages, then loads children by parent (UC-DCT-G02, B01).
- Lead decision (2026-10-30): LD16, offline editing is not supported: the editor shows an offline banner, keeps unsent edits in the tab, warns on close, and sends on reconnect with expected versions; structural actions are disabled offline (UC-DCT-H04).
- Lead decision (2026-10-30): LD17, Private and Workspace visibility are expressed through Governance sharing; Public stays undecided and the `PublishPage` stub is retired (UC-DCT-A15).
- Lead decision (2026-10-30): LD20, the empty `GET /pages/{pageId}/history` route is retired; content history is the version list; the page activity feed (rename, move, trash, restore) is a Collaboration activity built from Documents facts (UC-DCT-J08, J02).
- Lead decision (2026-10-30): LD21, version compare is a read-only side-by-side comparison of two entries, or one against the current page (UC-DCT-J07).
- Lead decision (2026-10-30): LD22, workspace export is a background job for Workspace Admins that produces a ZIP of Markdown and files and sends a download link; only pages the Admin can read are included (UC-DCT-A14).
- Lead decision (2026-10-30): LD24, a client that does not know a block type shows a read-only placeholder and never rewrites or coerces that block; the server refuses a write of an unknown type (UC-DCT-E09).
- Lead decision (2026-10-30): LD25, when access is revoked while a page is open the next write or read is `404`, the editor locks, and the person can copy their unsaved text (UC-DCT-I07).
- Lead decision (2026-10-30): LD26, create page accepts an `Idempotency-Key` header and returns the same page for 24 hours; a block create that repeats an id with the same content returns the stored block, and with different content is `409` (UC-DCT-L04).
- Lead decision (2026-10-30): LD27, access to a page never grants access to an embedded or referenced resource; embeds re-check the reader's own right (UC-DCT-I05, E06).
- Lead decision (2026-10-30): LD28, a page reference to a trashed, deleted or unreadable page shows "page unavailable" and nothing else (UC-DCT-E07).

## 4.3 Open decisions (BLOCKED)

- Public page visibility: needs the anonymous principal, abuse limits and crawler policy (UC-DCT-A15).
- Favorites: needs the owning context (UC-DCT-A16).
- Retention window and purge of deleted pages, versions, search projections, exports and backups (UC-DCT-A17, L05).
- Live co-editing: needs an ADR (UC-DCT-K03).

# 5. Canonical updates required

Text that `docs/product/documents.md` and the authority documents named must absorb. Wording is ready to copy; each item names the product doc section that takes it.

1. Section 10 (Page lifecycle), after the lifecycle list: "Trashing a page trashes its whole subtree as one unit; restoring the parent restores the subtree; permanent delete removes the subtree. A child page can be restored alone only when its parent is active; a page whose parent is gone is restored at the root of its Space."
2. Section 14 (Page hierarchy): "Pages nest at most 10 levels. Sibling pages sort by title (case and mark insensitive); there is no manual order."
3. Section 16 (Page movement): "A page moves under another parent when the caller may update the page and create under the new parent, the parent is in the same workspace and active, the move carries the page's expected version, and ancestry facts show no cycle. The subtree follows the Space of the new root. A page is never moved to another workspace; content crosses workspaces by export and import or by duplicate."
4. Section 7 (Page): "A page has an icon, one emoji or icon token of at most 32 characters, settable by Editors. Cover images are not part of the page yet. Rename and icon changes carry the page's expected version."
5. Section 59 (Documents and Governance): "Rename, icon, block writes, version save and restore, save as template and move to space require `UpdatePage`; trash and restore require `ArchivePage`; permanent delete requires `DeletePage`; template management (rename, archive, delete) is allowed to the template creator and Workspace Admins and needs a Governance action name."
6. Section 25 and 91 (Block hierarchy and deletion): "Deleting a block deletes the blocks under it in every route. Every block write on a trashed page is refused."
7. Section 28 and 29 (Ordering): "A reorder carries the expected version of each moved block, is a no-op when the place is unchanged, announces one page save, and the server accepts a client sort key only when it lies between the siblings the request names."
8. Section 21A (Block content schema): "A page holds at most 5 000 live blocks. The block read returns blocks in order and pages by cursor above 1 000 blocks."
9. Section 81 (Unknown Block Type): "A client that does not know a block type shows a read-only placeholder and never rewrites, coerces or drops that block. The server refuses a write of an unknown type."
10. Section 54 to 56 (Templates): "A template is managed by its creator or a Workspace Admin. Archiving a template never changes pages made from it. A duplicate of a page copies text, table and embed blocks, never images, files, videos, comments, versions, attachments or permissions, and keeps resource references pointing at the original targets."
11. Section 48 (Document history): "History keeps automatic, named and restore entries; all are kept until a retention window is decided. Two entries, or one entry and the current page, can be compared read-only. The page activity feed is a Collaboration activity built from Documents facts; Documents publishes no activity list."
12. Section 72 (Events): "Documents publishes versioned integration events for page created, renamed, moved, archived, restored, deleted and saved, with consumers named in the events and realtime architecture document."
13. Section 76 and 101 (Revocation, offline editing): "When access is revoked while a page is open, the next write or read is refused as not found, the editor locks and the person can copy unsaved text. Offline editing is not supported: unsent edits stay in the tab, are sent on reconnect with expected versions, and conflicts are shown."
14. Section 78 and 101 (Retry): "Creating a page accepts an idempotency key and returns the same page for 24 hours for the same key. A block create that repeats an id with the same content returns the stored block."
15. Section 67 (Search): "Page search is title-only (at least 2 characters, 25 results) and filtered by page access until a derived body index exists."
16. Section 83 (Export): "Page export is built in the browser (Markdown, sanitised HTML, print to PDF). Workspace export is a background job for Workspace Admins that produces a ZIP and a download link and includes only pages the Admin can read."
17. Section 40 and 41 (Linked-resource authorization): "Access to a page never grants access to an embedded or referenced resource; embeds re-check the reader's own right. A reference to a trashed, deleted or unreadable page shows one generic unavailable state."
18. `docs/product/governance.md`: add the Documents action names above and the template management action once named.
19. `docs/workstreams/executions/documents-collaboration/documents-collaboration.spec.md` section 14 (authorization drift) and section 13 (source debt) list items that this catalog classifies: the three `NotImplementedException` handlers are RETIRE (move and publish are re-done as UC-DCT-C02 and UC-DCT-A15; the deadline stub has no product meaning), and the history endpoint is RETIRE (lead decision LD20).

# 6. Not covered

| Product doc section | Reason |
|---|---|
| 1, 2, 3, 6, 115, 116 (mission, ownership, non-ownership, purpose, related owners, final rule) | Orientation text with no behaviour; the final rule questions are answered across the use cases. |
| 4, 5, 9, 18, 19, 21, 22, 23, 24 (language, structured content, identity, block rules) | Meaning and invariants; they are exercised by UC-DCT-A01, D01, D02 and E01 to E09. |
| 35, 36, 37 (presence, comment anchors, anchors never own content) | Collaboration use cases; see [collaboration.md](collaboration.md). |
| 38 to 45 (resource links) | Covered as read and embed behaviour in UC-DCT-E06, E07, G04, G05, I05. Creating and removing a Documents `ResourceLink` has no use case because no code uses the aggregate and links from items to pages are owned by Work Management (Part 1, defect 10); cross-workspace links (section 45) are refused by default and need no use case. |
| 57 (template reapply or upgrade) | Not supported; a later linked-template feature needs a decision. |
| 58, 61 to 66 (other contexts) | Owned by Workspaces, Work Management, Automation, Integrations and Analytics; see their files. Page facts for Automation and Analytics: UC-DCT-J02. |
| 68 (index lag) | Rule on search; covered by UC-DCT-G03 and G07. |
| 85 (duplicate: links) | Covered in UC-DCT-A09. |
| 87, 88 (cross-workspace move) | Covered as a refusal in UC-DCT-C03. |
| 92 (child policy) | Covered in UC-DCT-D04 (recursive delete). |
| 100, 102 (mobile, accessibility) | Covered in UC-DCT-H06 to H08. |
| 103, 104, 105 to 109 (source alignment, ambiguity watch, change impact) | Review guidance for future changes, not user behaviour. |
| 110, 111, 112 (checklists) | Page checklist: UC-DCT-A01, B04, I01, D04, J03, A04 to A06. Block-type checklist: UC-DCT-E01 to E09 (import, export, search and history migration for new types is review work). Collaboration boundary checklist: [collaboration.md](collaboration.md). |
| 113, 114 (testing evidence, stop conditions) | Test plans and stop rules; the pack test file is the home. |
| Team spec capabilities `DCX-01` to `DCX-04` and all Collaboration capabilities | Owned by the Collaboration catalog. |
| `SetPageDeadline` (stub) | Not in the product doc; no use case. Retire the handler (Part 5, item 19). |

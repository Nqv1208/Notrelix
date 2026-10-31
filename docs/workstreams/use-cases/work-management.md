---
document_id: WRK-UC-WORK-MANAGEMENT
document_type: workstream-use-case-catalog
status: active
owner: work-management-team
applies_to:
  - work-management
  - use-case-catalog
  - capability-acceptance
evidence:
  - docs/workstreams/use-cases/README.md
  - docs/product/work-management.md
  - docs/workstreams/teams/work-management.md
  - docs/workstreams/executions/work-management/work-management.spec.md
  - docs/workstreams/executions/work-management/work-management.tests.md
  - docs/workstreams/executions/work-management/work-management.certification.md
  - docs/workstreams/executions/work-management-views/work-management-views.spec.md
  - backend/src/Notrelix.API/Endpoints/WorkManagement/
  - backend/src/Notrelix.Application/Features/WorkManagement/
  - backend/src/Notrelix.Domain/WorkManagement/
  - frontend/packages/product/work-management/
  - frontend/e2e/real/
review_on:
  - work-management-domain-change
  - work-management-contract-change
  - work-view-model-change
  - product-semantic-change
  - capability-state-change
---

# Use cases: Work Management

Product meaning: [work-management](../../product/work-management.md). Team: [work-management](../teams/work-management.md). Execution packs: [spec](../executions/work-management/work-management.spec.md), [tests](../executions/work-management/work-management.tests.md), [certification](../executions/work-management/work-management.certification.md), [plan](../executions/work-management/work-management.plan.md) and the P4A [views spec](../executions/work-management-views/work-management-views.spec.md). Delivery context: [capability delivery map](../capability-delivery-map.md), [cross-team dependencies](../cross-team-dependencies.md).

Requirement ids cited below are `WMREQ001` to `WMREQ156` of the transactional core pack. The views pack has no requirement ids, so it is cited as "views pack section n". Product decisions are cited as `D28` to `D75` (owner decision items 28 to 75), `D76.n`, `D77.n`, `D78.n` (owner decision items 76 to 78) and `Q1` to `Q13` (Board core answers); each is written out in part 4. `LDn` are lead decisions of this catalog, also in part 4.

State reflects `develop`. Work that sits in an open pull request (#201 filters and personal view state, #204 Table field columns, #193 realtime) is not on `develop`; it is described as "in open PR #n" in the gap text and does not raise the state.

# 1. Assessment

## 1.1 Capability map

Letters follow the capability list of the team spec (section 5): A Board lifecycle, B BoardItem lifecycle, C BoardField definitions, D Field values, E Groups and grouping, F Ordering, G Checklists, H Query, filter and sort foundation, I Table view, J Kanban view, K Calendar view, L Timeline view, M Dashboard projection, N Form input, O Activity and event production, P Realtime synchronization, Q Automation handoff, R Collaboration handoff, S Billing and entitlement integration, T Performance and hardening. The team capability ids are `WM-001` to `WM-020` in that order.

The product doc also owns approvals, labels, templates, import, export, search and workload, which the team spec does not name as capabilities. They are placed under the capability whose code and rules they use: approvals, import and export under B; templates, sharing and membership under A; labels under C, D and H; search and My Work under H. Part 6 records the placement.


| Letter | Capability | Team id | Use cases | DEFINED | IMPLEMENTING | INTEGRATING | HARDENING | DONE | BLOCKED |
|---|---|---|---|---|---|---|---|---|---|
| A | Board lifecycle | WM-001 | 26 | 4 | 0 | 3 | 19 | 0 | 0 |
| B | BoardItem lifecycle | WM-002 | 36 | 5 | 0 | 13 | 18 | 0 | 0 |
| C | BoardField definitions | WM-003 | 22 | 1 | 2 | 5 | 14 | 0 | 0 |
| D | Field values | WM-004 | 12 | 0 | 0 | 3 | 9 | 0 | 0 |
| E | Groups and grouping | WM-005 | 6 | 0 | 0 | 0 | 6 | 0 | 0 |
| F | Ordering | WM-006 | 4 | 0 | 0 | 0 | 4 | 0 | 0 |
| G | Checklists | WM-007 | 2 | 0 | 0 | 0 | 2 | 0 | 0 |
| H | Query, filter and sort foundation | WM-008 | 19 | 2 | 0 | 2 | 15 | 0 | 0 |
| I | Table view | WM-009 | 3 | 0 | 0 | 0 | 3 | 0 | 0 |
| J | Kanban view | WM-010 | 6 | 0 | 0 | 0 | 6 | 0 | 0 |
| K | Calendar view | WM-011 | 1 | 0 | 0 | 0 | 1 | 0 | 0 |
| L | Timeline view | WM-012 | 1 | 0 | 0 | 0 | 1 | 0 | 0 |
| M | Dashboard projection | WM-013 | 1 | 0 | 0 | 0 | 1 | 0 | 0 |
| N | Form input | WM-014 | 6 | 0 | 0 | 0 | 5 | 0 | 1 |
| O | Activity and event production | WM-015 | 4 | 0 | 0 | 2 | 2 | 0 | 0 |
| P | Realtime synchronization | WM-016 | 4 | 0 | 0 | 0 | 4 | 0 | 0 |
| Q | Automation handoff | WM-017 | 3 | 1 | 0 | 0 | 2 | 0 | 0 |
| R | Collaboration handoff | WM-018 | 8 | 0 | 0 | 1 | 7 | 0 | 0 |
| S | Billing and entitlement integration | WM-019 | 1 | 1 | 0 | 0 | 0 | 0 | 0 |
| T | Performance and hardening | WM-020 | 5 | 4 | 0 | 1 | 0 | 0 | 0 |
| | Total | | 170 | 18 | 2 | 30 | 119 | 0 | 1 |

Counts cover the 170 live use cases. A further 8 are retired by decision and listed in part 6.1; with them the catalog holds 178 use cases, all of the prior catalog. Seven of the 170 are enterprise-baseline additions.

## 1.2 The ten most important gaps

1. Competing writes are last-writer-wins. Eight item commands (status, due date, complete, reopen, move to board, assign, unassign), label update and delete, member role change, view config writes and form question writes carry no expected version, and bulk edit sends none per item. The decision is that all of them carry it (UC-WM-B05, UC-WM-B06, UC-WM-B08, UC-WM-B14, UC-WM-A17, UC-WM-C22, UC-WM-H02, UC-WM-N02; D77.3; WMREQ110 to WMREQ113).
2. Realtime is a notify-only board stream: every board change sends one content-free frame and the open board and card refetch (UC-WM-P01 to UC-WM-P04, UC-WM-Q02). The connection status, the draft choice and live role lowering are built; real e2e and owner check remain. The stream version is also a wall-clock counter, not the resource version (LD4).
3. Large boards do not scale. Board, item and trash reads are unpaged, filter and sort run on the client, and no read returns filtered totals (UC-WM-T01, UC-WM-H17, UC-WM-I01, UC-WM-J01; WMREQ147).
4. The delete lifecycle is half built. Board restore applies no 30-day rule, field delete is a hard delete with no restore, and no purge job exists for boards, fields or items (UC-WM-A07, UC-WM-C05, UC-WM-B17).
5. Personal view state (filter, sort, group, swimlane, return to the shared view, stale-reference hint) exists only in open PR #201; saved filters and preferences are stored but never applied on the server (UC-WM-H06, UC-WM-H07, UC-WM-H08, UC-WM-J04, UC-WM-H11, UC-WM-H12, UC-WM-H09).
6. Approvals are inconsistent with the decided model. Team approvers can never decide, two decide paths exist (one without expected version), and a re-request does not link to the previous request (UC-WM-B25, UC-WM-B26, UC-WM-B27).
7. ~~Link cycles are not blocked.~~ Fixed for creation: a link that would close a waits-on cycle on the board is refused naming the path (UC-WM-B20). Open: links across boards, and a kind change, for which no update command exists yet (UC-WM-B21).
8. Forms do not match the decided model. A member submission records an Accepted entry only, but there is no inbox flow proven by a test, no API-level tests exist for Forms, and anonymous public forms are BLOCKED on an owner security decision (UC-WM-N01 to N06).
9. Table-stakes functions are not built: import, export, duplicate board, Relation field, required fields, WIP limit, published limits, guest invitation to one board, last-manager protection, native mobile (UC-WM-B29 to B32, UC-WM-A11, UC-WM-C18, UC-WM-C19, UC-WM-J06, UC-WM-T03, UC-WM-A19, UC-WM-A26, UC-WM-T04).
10. Proof is thin. Only 35 of 170 use cases touch a real e2e spec, 24 reach HARDENING, and none has a recorded 1-px design diff or owner check. About 40 contract operations have no mock handler, so mock e2e cannot prove those flows.

No use case is DONE: DONE needs the recorded 1-px box-tree diff at 1440, 1024 and 390 px in light and dark, and the owner check on the development stack, and neither exists for any use case. A use case is HARDENING only when a real e2e spec proves its main flow; specs that only pass through it (marked implicit, smoke, negative only, count only or partial in the Real e2e cell) leave it INTEGRATING. A use case with an open-PR dependency is judged on `develop` only.


## 1.3 Defects in shipped code

Verified against `develop`. B is backend (24 listed), F is frontend (14 listed); 38 in total. The prior ids B-20 to B-22, F-12 and F-15 are withdrawn because owner decisions 77.1, 77.2 and 77.4 made the behaviour correct (the Nhắc nudge, the Accepted-only form submission and private views stay); their numbers are not reused. Paths are repository-relative. B-25 to B-27 were found while verifying the prior unproven cells; the prior defect B-25 (assign-time view check) is withdrawn because `AssignBoardItemMember` does perform the check (comment `WM-040` in the handler).

| ID | Defect | Where | Use cases affected | Files |
|---|---|---|---|---|
| B-01 | Item lifecycle and assignment writes carry no expected version; last writer wins silently | UpdateBoardItemStatus, SetBoardItemDueDate, CompleteBoardItem, ReopenBoardItem, MoveBoardItemToBoard, AssignBoardItemMember, UnassignBoardItemMember | UC-WM-B05 UC-WM-D04 UC-WM-D05 UC-WM-B06 UC-WM-B08 UC-WM-B33 | backend/src/Notrelix.Application/Features/WorkManagement/BoardItems/Commands/<each>/<each>.cs; backend/src/Notrelix.Infrastructure/Data/Concurrency/ExpectedVersionTargetMap.cs |
| B-02 | Bulk update has no per-item expected version (D32), which acceptance section 1 does not allow for competing writes | BulkUpdateBoardItems | UC-WM-B14 UC-WM-B36 | backend/src/Notrelix.Application/Features/WorkManagement/BoardItems/Commands/BulkUpdateBoardItems/BulkUpdateBoardItems.cs |
| B-03 | Fixed in code, tests pending: board member role change and removal now carry the board version | UpdateBoardMemberRole | UC-WM-A17 | backend/src/Notrelix.Application/Features/WorkManagement/Boards/Commands/UpdateBoardMemberRole/UpdateBoardMemberRole.cs |
| B-04 | Label update and delete have no expected version | UpdateLabel, DeleteLabel | UC-WM-C22 | backend/src/Notrelix.Application/Features/WorkManagement/Labels/Commands/UpdateLabel/UpdateLabel.cs; backend/src/Notrelix.Application/Features/WorkManagement/Labels/Commands/DeleteLabel/DeleteLabel.cs |
| B-05 | Field delete has no expected version, is a hard delete, and no restore endpoint exists although the domain event does (D76.5 wants 30-day soft delete) | DeleteBoardField | UC-WM-C04 UC-WM-C05 | backend/src/Notrelix.Application/Features/WorkManagement/BoardFields/Commands/DeleteBoardField/DeleteBoardField.cs |
| B-06 | View config writes are last-writer-wins: PUT upsert and PATCH config carry no expected version | SaveBoardView, UpdateBoardViewConfig | UC-WM-H02 UC-WM-J05 | backend/src/Notrelix.Application/Features/WorkManagement/BoardViews/Commands/SaveBoardView/SaveBoardView.cs; backend/src/Notrelix.Application/Features/WorkManagement/BoardViews/Commands/UpdateBoardViewConfig/UpdateBoardViewConfig.cs |
| B-07 | Form question add, edit and remove have no expected version; UpdateFormQuestion has no test | AddFormQuestion, UpdateFormQuestion, RemoveFormQuestion | UC-WM-N02 | backend/src/Notrelix.Application/Features/WorkManagement/Forms/Commands/UpdateFormQuestion/UpdateFormQuestion.cs |
| B-08 | Approval decision has two paths (board-scoped approve/reject with EV, workspace /decision without EV); team approvers can never decide (ApproverTeamId ignored), against D76.11 | DecideApproval, ApproveApprovalRequest, RejectApprovalRequest | UC-WM-B25 UC-WM-B26 | backend/src/Notrelix.Application/Features/WorkManagement/Approvals/Commands/DecideApproval/DecideApproval.cs |
| B-09 | Fixed for the cycle part: CreateBoardItemLink now runs DependencyRules.EnsureNoCycle and refuses a waits-on or blocks link that closes a cycle, naming the path. Open: links are same-board only (cross-board links not built); no kind-change command exists | CreateBoardItemLink; DependencyRules.EnsureNoCycle | UC-WM-B20 UC-WM-B21 UC-WM-L01 UC-WM-B35 | backend/src/Notrelix.Application/Features/WorkManagement/ItemLinks/Commands/CreateBoardItemLink/CreateBoardItemLink.cs; backend/src/Notrelix.Domain/WorkManagement/Items/DependencyRules.cs |
| B-10 | PATCH /board-items/{id} has expected version but no Idempotency-Key, unlike sibling mutations | UpdateBoardItem | UC-WM-B04 UC-WM-B19 | backend/src/Notrelix.Application/Features/WorkManagement/BoardItems/Commands/UpdateBoardItem/UpdateBoardItem.cs |
| B-11 | Unbounded reads: board items, full board and trash load everything; computed fields add O(board) work per request (AGENTS.md section 25 red flag) | GetBoardItems, GetFullBoard, ListBoardItemTrash | UC-WM-A09 UC-WM-I01 UC-WM-J01 UC-WM-T01 UC-WM-B13 UC-WM-D07 | backend/src/Notrelix.Application/Features/WorkManagement/BoardItems/Queries/GetBoardItems/GetBoardItemsQuery.cs; backend/src/Notrelix.Application/Features/WorkManagement/Boards/Queries/GetFullBoard/GetFullBoard.cs; backend/src/Notrelix.Application/Features/WorkManagement/BoardItems/Queries/ListBoardItemTrash/ListBoardItemTrash.cs |
| B-12 | Realtime StreamVersion is a wall-clock counter, not the resource version; every board write handler now announces on the board stream; version semantics unchanged | RealtimeResourceChangedV1, RealtimeChangeMapper | UC-WM-P01 UC-WM-P03 | backend/src/Notrelix.Application/Common/Realtime/RealtimeResourceChangedV1.cs; backend/src/Notrelix.Application/Common/Realtime/RealtimeChangeMapper.cs |
| B-13 | Dead code: unregistered duplicate endpoint, handler with no endpoint, stub consumers | CreateChecklistItemEndpoint, GetBoardsBySlugQuery, WM Stub consumers | UC-WM-G02 UC-WM-A09 UC-WM-O03 UC-WM-O04 | backend/src/Notrelix.API/Endpoints/WorkManagement/Checklists/Commands/CreateChecklistItemEndpoint.cs; backend/src/Notrelix.Application/Features/WorkManagement/Boards/Queries/GetBoardsBySlug/GetBoardsBySlug.cs; backend/src/Notrelix.Infrastructure/Messaging/ConsumerRegistrySetup.cs |
| B-14 | Handlers with zero test references | ListApprovalRequests, Archive/Unarchive/RestoreBoardView, UpdateFormQuestion, Delete/Pause/ResumeBoardRelation | UC-WM-B28 UC-WM-H03 UC-WM-N02 UC-WM-C18 | backend/src/Notrelix.Application/Features/WorkManagement/Approvals, BoardViews, Forms, Relations |
| B-15 | No API-level endpoint tests for Forms, Approvals, Templates, Relations, SavedFilters, Preferences | all endpoints of those groups | UC-WM-N01 to N05 UC-WM-B24 to B28 UC-WM-A12 to A15 UC-WM-H09 UC-WM-H06 | backend/tests (API tests project); backend/src/Notrelix.API/Endpoints/WorkManagement |
| B-16 | UpdateBoardVisibility accepts PublicLink but nothing serves it (public links are not built, D76.4); no anonymous RLS scope tied to WM | UpdateBoardVisibility | UC-WM-A21 UC-WM-A28 | backend/src/Notrelix.Application/Features/WorkManagement/Boards/Commands/UpdateBoardVisibility/UpdateBoardVisibility.cs |
| B-17 | Form default Visibility=PublicLink while SubmitForm needs a signed-in member with CreateItem (D67 pending) | SubmitForm | UC-WM-N04 UC-WM-N06 | backend/src/Notrelix.Application/Features/WorkManagement/Forms/Commands/SubmitForm/SubmitForm.cs |
| B-18 | Board archive does not pause automation rules or integration sync, and unarchive does not resume them (D76.1) | ArchiveBoard, UnarchiveBoard | UC-WM-A04 UC-WM-A05 | backend/src/Notrelix.Application/Features/WorkManagement/Boards/Commands/ArchiveBoard/ArchiveBoard.cs |
| B-19 | No purge job: deleted boards (D76.1), fields (D76.5) and trashed items (D51) are never removed after 30 days | none exists | UC-WM-A06 UC-WM-A07 UC-WM-C05 UC-WM-B17 | n/a (missing job) |
| B-23 | Template create copies structure only; no include-items option, no 500-item refusal (66, D76.13) | CreateBoardTemplate | UC-WM-A12 | backend/src/Notrelix.Application/Features/WorkManagement/Templates/Commands/CreateBoardTemplate/CreateBoardTemplate.cs |
| B-24 | Saved filters and preferences are stored but never applied server-side; no q, filter, sort or count on item reads | GetBoardItems, SavedFilters.*, BoardPreferences.* | UC-WM-H06 UC-WM-H07 UC-WM-H08 UC-WM-H15 UC-WM-H17 UC-WM-T01 | backend/src/Notrelix.Application/Features/WorkManagement/BoardItems/Queries/GetBoardItems/GetBoardItemsQuery.cs |
| B-25 | Fixed in code, tests pending (the purge job is still missing): board restore now refuses after 30 days. Was: board restore has no retention check; a board deleted long ago restores, and no purge job exists | RestoreBoard, Board.Restore | UC-WM-A07 | backend/src/Notrelix.Application/Features/WorkManagement/Boards/Commands/RestoreBoard/RestoreBoard.cs; backend/src/Notrelix.Domain/WorkManagement/Boards/Board.cs |
| B-26 | Fixed in code, tests pending: the last Manager of a board can no longer be removed or demoted. Was: the last Manager of a board can be removed or demoted; no guard in either handler | RemoveBoardMember, UpdateBoardMemberRole | UC-WM-A26 UC-WM-A17 | backend/src/Notrelix.Application/Features/WorkManagement/Boards/Commands/RemoveBoardMember/RemoveBoardMember.cs; backend/src/Notrelix.Application/Features/WorkManagement/Boards/Commands/UpdateBoardMemberRole/UpdateBoardMemberRole.cs |
| B-27 | A moved item gets a new key but the old key is not stored as an alias, so it no longer resolves | MoveBoardItemToBoard | UC-WM-A10 UC-WM-B08 | backend/src/Notrelix.Application/Features/WorkManagement/BoardItems/Commands/MoveBoardItemToBoard/MoveBoardItemToBoard.cs |
| F-01 | Item extras bypass the typed client: raw path strings for links, approvals, following, linked pages, trash, child items, remind, complete, reopen, unarchive, restore, move-to-board | item-extras.api.ts | UC-WM-B03 UC-WM-B06 UC-WM-B08 UC-WM-B10 UC-WM-B13 UC-WM-B20 UC-WM-B21 UC-WM-R05 UC-WM-R06 UC-WM-B24 UC-WM-B25 UC-WM-B26 | frontend/packages/product/work-management/state/src/api/item-extras.api.ts |
| F-02 | Board archive and delete use raw paths | archiveBoard, deleteBoard | UC-WM-A04 UC-WM-A06 | frontend/packages/product/work-management/state/src/api/board-settings.api.ts:367,373 |
| F-03 | Raw /boards/:id/plan-items path for Calendar and Timeline; the backend inventory lists no such route and a real e2e asserts it is not called | plan-items | UC-WM-K01 UC-WM-L01 | frontend/packages/product/work-management/state/src/api/item-extras.api.ts:276 |
| F-04 | About 40 contract operations have no mock handler by id (Approvals.*, Relations.*, BoardPreferences.*, many BoardItems.*, Boards.*, SavedFilters update ops); mock e2e cannot prove those flows | mock-backend | UC-WM-A08 UC-WM-B10 UC-WM-B20 UC-WM-B24 to B28 UC-WM-H09 | frontend/packages/dev/mock-backend/src/contexts/work-management/ |
| F-05 | Contract operations never called by the UI: BoardPreferences.* (open PR #201), Boards.Restore/Unarchive, BoardGroups.Archive/Unarchive, BoardViews.Archive/Restore, ClearFieldValue, UpdateFieldValues, GetSchema, SavedFilters update ops | generated client | UC-WM-A05 UC-WM-A07 UC-WM-E03 UC-WM-H03 UC-WM-D03 UC-WM-D01 UC-WM-H09 | frontend/packages/product/work-management/state/src (no callers) |
| F-06 | The Work Management adapter refetches the open board on a board frame; the connection status shows while reconnecting or lost | module-adapter-registry | UC-WM-P01 UC-WM-P02 UC-WM-P03 UC-WM-P04 UC-WM-Q02 | frontend/apps/web/src/realtime/module-adapter-registry.ts |
| F-07 | No virtualization or paging; sort and filter run client-side on a fully loaded board | table, kanban views | UC-WM-I01 UC-WM-J01 UC-WM-T01 UC-WM-H17 | frontend/packages/product/work-management/web/src/components/views/table, views/kanban |
| F-08 | Hard-coded capability flags lock field archive, field rules, view split, board automations; field archive contradicts D76.5 soft delete with restore | UNAVAILABLE | UC-WM-C04 UC-WM-C05 UC-WM-Q01 | frontend/packages/product/work-management/state/src/api/board-settings.api.ts |
| F-09 | Kanban has no multi-select (Q8) and no WIP limit (D37) | KanbanView | UC-WM-B14 UC-WM-I03 UC-WM-J06 | frontend/packages/product/work-management/web/src/components/views/kanban/kanban-view.tsx |
| F-10 | Stub-like fallback copy ('not available yet') where the capability is decided or built | item-sheet.copy.ts, board-header.copy.ts, board-settings.copy.ts | UC-WM-B18 UC-WM-B19 UC-WM-A02 | frontend/packages/product/work-management/web/src/components/card-detail/item-sheet.copy.ts; frontend/packages/product/work-management/web/src/components/board-layout/board-header.copy.ts; frontend/packages/product/work-management/web/src/components/board-settings/board-settings.copy.ts |
| F-11 | Conflict and deleted notices carry no actor name (Q12 needs a contract addition) | card-command.ts, ItemPageConflictNotice | UC-WM-D02 UC-WM-B19 UC-WM-B23 UC-WM-B33 UC-WM-B34 | frontend/packages/product/work-management/state/src/mutations/card-command.ts |
| F-13 | Plugins and mobile packages are placeholders with no consumer | plugins, mobile | UC-WM-T04 | frontend/packages/product/work-management/plugins; frontend/packages/product/work-management/mobile |
| F-14 | Form builder shows 'Sắp có' question types; fill page refuses unsupported types; no unauthenticated route | forms.copy.ts, form-builder-surface.tsx | UC-WM-N02 UC-WM-N04 UC-WM-N06 | frontend/packages/product/work-management/web/src/components/forms/forms.copy.ts:111; frontend/packages/product/work-management/web/src/components/forms/form-builder-surface.tsx:651 |
| F-16 | Reports use raw api paths, not generated operations (consumes WM data; no WM UC owns it) | reports-api.ts | none (Analytics) | frontend/apps/web/src/routes/workspaces/$workspaceId/reports/reports-api.ts |


## 1.4 Enterprise baseline

Measured against monday.com, Asana, ClickUp, Jira Cloud, Smartsheet, Notion databases and Linear. Evidence is from vendor help pages; cells marked as common knowledge need a pass through each vendor's help centre before they are used as acceptance proof.

- monday.com: [recycle bin](https://monday.com/blog/product/recycle-bin-rescue-things-youve-deleted/), [audit log](https://support.monday.com/hc/en-us/articles/360001259429-The-Audit-Log)
- Asana: [recover deleted tasks and projects](https://help.asana.com/s/article/recover-deleted-tasks-projects-and-more), [audit log API](https://developers.asana.com/docs/audit-log-api)
- ClickUp: [Trash](https://help.clickup.com/hc/en-us/articles/6311742742423-Restore-items-from-the-Trash), [custom field types](https://help.clickup.com/hc/en-us/articles/6303499162647-Custom-Field-types), [audit logs](https://help.clickup.com/hc/en-us/articles/21929900448535-Workspace-audit-logs)
- Jira Cloud: [roadmap and timeline limits](https://support.atlassian.com/jira-software-cloud/docs/set-up-the-roadmap/), [project trash](https://support.atlassian.com/jira-cloud-administration/docs/trash-for-jira-cloud-projects/), [audit activities](https://support.atlassian.com/jira-cloud-administration/docs/audit-activities-in-jira-applications/)
- Smartsheet: [limitations](https://developers.smartsheet.com/api/smartsheet/guides/limitations), [recover a deleted sheet](https://help.smartsheet.com/articles/2482403-recover-deleted-sheet)
- Notion: [database performance and limits](https://www.notion.com/help/optimize-database-load-times-and-performance), [data retention](https://www.notion.com/help/custom-data-retention-settings)
- Linear: [delete and archive issues](https://linear.app/docs/delete-archive-issues), [audit log](https://linear.app/docs/audit-log)

Table stakes (offered by at least 5 of 7 vendors) and where Notrelix stands on `develop`:

| Capability (vendors of 7) | Use cases | Position |
|---|---|---|
| Board lifecycle with duplicate (7) | UC-WM-A01 to A11 | Create, rename, archive, delete built; duplicate missing (UC-WM-A11); restore and purge incomplete (UC-WM-A07, UC-WM-B17) |
| Item CRUD and sub-items (7) | UC-WM-B01 to B04, UC-WM-B07 to B14 | Built; expected version missing on most writes (gap 1) |
| Rich custom field types (6) | UC-WM-C01 to C19, UC-WM-D01, UC-WM-F01, UC-WM-H15 | Eleven types built; Relation (UC-WM-C18) and required fields (UC-WM-C19) missing |
| Groups and sections (6) | UC-WM-E01 to E05, UC-WM-F02 | Built; group delete needs a target group (D76.6) |
| Table and Kanban (7) | UC-WM-I01, UC-WM-J01 | Built, with custom Table columns; no WIP limit (UC-WM-J06) |
| Calendar (6) | UC-WM-K01 | Built; drag and day-prefill proof missing |
| Saved, shared and personal views (6) | UC-WM-H01 to H05, UC-WM-H09 to H11, UC-WM-I02 | Shared and private views built; personal state in open PR #201 |
| Filter, sort, group (7) | UC-WM-H06 to H08, UC-WM-H15 | Client-side only; server-side on paged boards missing (gap 3) |
| Bulk edit (7) | UC-WM-B14, UC-WM-I03 | Built all-or-nothing; no per-item version, no Kanban selection |
| Trash and restore with 30-day window (6) | UC-WM-B11 to B13, UC-WM-A06, UC-WM-A07, UC-WM-C04, UC-WM-C05 | Items built; boards and fields incomplete |
| Item detail panel (6) | UC-WM-B18 to B23, UC-WM-G01 to G02, UC-WM-O01, UC-WM-R01 to R06 | Built; actor names on conflict notices missing |
| Dependencies (7) | UC-WM-B20, UC-WM-B21, UC-WM-L01 | Built; cycle check runs on create (gap 7 fixed); kind change and cross-board open |
| Templates (7) | UC-WM-A12 to A15 | Built; items-included option and 500-item refusal missing |
| Forms (6) | UC-WM-N01 to N06 | Member forms built; public forms BLOCKED |
| Import and export (7) | UC-WM-B29 to B32 | Not built |
| Search (7) | UC-WM-H16 to H18 | Command search and My Work built; in-board text filter is client-side |
| Realtime collaboration (7) | UC-WM-B33 to B36, UC-WM-D12, UC-WM-F04, UC-WM-P01 to P04 | Refetch only; adapter in open PR #193 |
| Board roles and guests (7) | UC-WM-A16 to A26 | Roles built; guest invitation to one board and last-manager guard missing |
| Near-stakes: timeline with dependencies (4), formula and rollup (4), published limits (4) | UC-WM-L01, UC-WM-C20 to C21, UC-WM-D07 to D10, UC-WM-T03 | Timeline and formula built; limits missing |
| Differentiators: approvals (3), workload (3) | UC-WM-B24 to B28, retired workload | Approvals built with defects; workload is retired from board core (D76.12) |

Not owned here, with the owner: audit log, SSO and SCIM, field-level and view-level permissions (Governance and Identity); notification delivery (Collaboration; UC-WM-R07 covers the Work Management side). Table-stakes use cases that the product doc did not list were added and marked "(enterprise baseline)": UC-WM-A11, UC-WM-A26, UC-WM-C18, UC-WM-C19, UC-WM-B17, UC-WM-T03, UC-WM-T04.


# 2. Index

Capability letters: A Board lifecycle, B BoardItem lifecycle, C BoardField definitions, D Field values, E Groups and grouping, F Ordering, G Checklists, H Query, filter and sort foundation, I Table view, J Kanban view, K Calendar view, L Timeline view, M Dashboard projection, N Form input, O Activity and event production, P Realtime synchronization, Q Automation handoff, R Collaboration handoff, S Billing and entitlement integration, T Performance and hardening.

Column values: BE and FE say `yes`, `partial`, `no`, or `not applicable` (no server or no screen involved); a use case whose central code is in an open pull request says so in the cell. Design says `complete`, `partial`, `none` (needs design) or `not applicable`. Real e2e names the `frontend/e2e/real` spec that passes through the use case, or `none`; the Evidence line of the use case says whether the spec proves the main flow.

| ID | Use case | Capability | Actors | BE | FE | Design | Real e2e | State | Gap |
|---|---|---|---|---|---|---|---|---|---|
| UC-WM-A01 | Create a board (blank, preset or from template) | A | Owner/Admin/Member (Space: Editor); Guest denied | yes | partial | partial | none | HARDENING | No real e2e; default-group proof (D49, D58); template gallery entry; Guest 403 test |
| UC-WM-A02 | Rename the board and edit its settings | A | Manager | yes | yes | partial | board-settings, work-board-management | HARDENING | Test prefix change affects new items only; design Er/320/768; icon change |
| UC-WM-A03 | Place a board in a Space or move it between Spaces | A | Manager (+ CreateBoard on target Space) | yes | partial | none | none | HARDENING | Design; access-impact warning with grant offer (UC-WM-A25); real e2e |
| UC-WM-A04 | Archive a board | A | Manager | partial | partial | partial | none | HARDENING | automation and integration sync are not paused (D76.1; defect B-18); archive uses a raw path, not the typed client (defect F-02); no real e2e; peers see the banner live only with open PR #193. |
| UC-WM-A05 | Unarchive a board | A | Manager | yes | no | none | none | HARDENING | Unarchive UI; design; resume automation/sync; real e2e |
| UC-WM-A06 | Delete a board | A | Manager (workspace Owner) | partial | partial | none | none | HARDENING | 30-day soft delete and purge (D76.1); impact counts in confirm; typed client; design |
| UC-WM-A07 | Restore a deleted board | A | Manager | partial | no | none | none | HARDENING | no 30-day window check in Board.Restore, no deleted-boards list or UI, no purge job (see UC-WM-B17); verified in source. |
| UC-WM-A08 | List boards the person may see | A | Any member; Guest only shared boards | yes | yes | partial | guest-permission | HARDENING | Parity test: Private board absent for non-member; mock handler |
| UC-WM-A09 | Open a board (read: schema, groups, items, preferences) | A | Viewer and above; Guest per grant | partial | partial | complete | board-views-and-settings | HARDENING | per-group paging (50, max 200) with filtered totals is not built (Q9; defect B-11); proof of the empty, archived and unknown-view states. |
| UC-WM-A10 | Assign and keep the item key (prefix and sequence) | A | System on create | partial | yes | complete | work-board-management | HARDENING | no stored alias, so an old key does not resolve after a move (verified: no alias in Domain or Application); no concurrency test for the sequence. |
| UC-WM-A11 | Duplicate a board | A | Manager (ManageBoard); CreateBoard in target Space | no | no | none | none | DEFINED | no endpoint, no UI, no design; behaviour is decided in D78.5. |
| UC-WM-A12 | Save a board as a template | A | Manager of the board | partial | yes | complete | none | HARDENING | Include-items option and 500 refusal (D76.13, 66); design Er |
| UC-WM-A13 | Publish a template | A | ManageBoard on the template (D66) | yes | yes | complete | none | HARDENING | API tests; real e2e; the product doc says Owner or Admin publishes and D66 says ManageBoard on the template, so the product doc must align (part 5). |
| UC-WM-A14 | Archive or delete a template | A | ManageBoard on the template (D66) | yes | yes | complete | none | HARDENING | API tests; real e2e; same ManageBoard rule as UC-WM-A13 (D66). |
| UC-WM-A15 | Create a board from a template or built-in starter | A | As UC-WM-A01 | yes | partial | complete | none | HARDENING | Gallery route on New board; real e2e |
| UC-WM-A16 | Read who has access (sharing summary) | A | ViewBoard (counts); members/managers (entries, log) | yes | yes | partial | board-sharing | HARDENING | Direct GetSharing tests; design Er/L/E |
| UC-WM-A17 | Add, change or remove a board member | A | Manager (up to own level) | yes | yes | partial | board-sharing | HARDENING | Expected version on role change; last-manager guard (UC-WM-A26) |
| UC-WM-A18 | Grant explicit access to a user or team | A | Manager (ManageBoardPermission) | yes | yes | partial | guest-permission, board-sharing | HARDENING | Expiry input removed (D76.4); Guest capped at Editor proof |
| UC-WM-A19 | Invite an external guest to one board | A | ManageBoardPermission | no | no | none | none | DEFINED | no Work Management call to the Workspaces resource-invitation API from the Share dialog; no UI. |
| UC-WM-A20 | Resend or revoke a resource invitation | A | Issuer or Owner/Admin | no | no | none | none | DEFINED | Depends on UC-WM-A19; design; BE; FE |
| UC-WM-A21 | Change board visibility (Workspace or Private) | A | Manager | partial | partial | partial | none | HARDENING | Reject PublicLink (D76.4); affected-assignee warning (UC-WM-A25); UI for Workspace visibility |
| UC-WM-A22 | Make a board private with impact preview and bulk add | A | Manager | yes | yes | complete | none | HARDENING | Direct handler tests; real e2e; one-transaction proof |
| UC-WM-A23 | Open a shared board as a Guest | A | Guest with explicit access | yes | partial | none | guest-permission | INTEGRATING | design for the Guest board view; proof that pickers show no emails (Q7); positive guest journey e2e. |
| UC-WM-A24 | Copy item/board link | A | Any viewer | not applicable | yes | complete | none | INTEGRATING | non-member gets NotFound e2e |
| UC-WM-A25 | Warn when an access change would strand assignees | A | Manager | partial | partial | none | none | INTEGRATING | Warning on visibility change and move-to-space; design |
| UC-WM-A26 | Block removing or demoting the last board manager | A | Manager | partial | no | none | none | DEFINED | no last-manager guard in UpdateBoardMemberRole or RemoveBoardMember (verified in source); no design for the message. |
| UC-WM-B01 | Create an item (full dialog) | B | Editor and above; Guest if Editor | yes | yes | complete | none | HARDENING | no real e2e; proof matrix for the create dialog (limits, retry, partial failure); sub-item mode; 'retry the missing part' check. |
| UC-WM-B02 | Quick-add an item in a group or lane | B | Editor and above | yes | yes | complete | none | HARDENING | WIP warning on over-limit add (D37); scroll when outside window; real e2e |
| UC-WM-B03 | Create a sub-item | B | Editor and above | yes | yes | partial | sub-items | HARDENING | Typed client; sheet add; Q3 create-dialog sub-item mode |
| UC-WM-B04 | Edit the item title in place | B | Editor and above | partial | yes | complete | none | HARDENING | PATCH /board-items/{id} has expected version but no Idempotency-Key (see defect B-10). |
| UC-WM-B05 | Assign people to an item | B | Editor and above (AssignItem) | partial | yes | complete | none | HARDENING | expected version on assign and unassign (D77.3; defect B-01); invite action in the 'share the board first' message; Guest picker with no emails; real e2e. |
| UC-WM-B06 | Complete, reopen, remind | B | Editor and above | partial | partial | partial | work-board-management | HARDENING | expected version on complete and reopen (D77.3; defect B-01); the Nhắc dialog calls a raw path and is not on the typed client (defect F-01); the real e2e proves only reopen. |
| UC-WM-B07 | Duplicate an item | B | Editor and above (CreateItem) | partial | yes | partial | none | HARDENING | Prove copy set (incl. sub-items option) and exclusions; design Er/busy |
| UC-WM-B08 | Move an item to another board | B | UpdateItem source + CreateItem target | partial | yes | partial | none | HARDENING | Old-key alias (D76.3); mapping notice proof (D50, D54); EV |
| UC-WM-B09 | Archive an item | B | Editor and above | yes | yes | complete | sub-items, item-lifecycle | HARDENING | owner check |
| UC-WM-B10 | Browse archived items and unarchive | B | Editor and above (UpdateItem) | partial | no | complete | item-lifecycle | HARDENING | BE paged archived list; FE panel; unarchive restores sub-items |
| UC-WM-B11 | Delete an item (to Trash) | B | Editor and above | yes | yes | complete | none | HARDENING | Sub-items trashed with parent (Q10) proof; actor on deleted frame (Q12 contract); real e2e |
| UC-WM-B12 | Restore an item from Trash | B | Editor and above | yes | yes | complete | none | HARDENING | Real e2e; >30 days refusal proof |
| UC-WM-B13 | Open the board Trash | B | Editor and above (UpdateItem) | partial | yes | complete | none | HARDENING | Paging; typed client; real e2e |
| UC-WM-B14 | Bulk-edit items (Table and Kanban) | B | Editor and above | yes | partial | complete | none | HARDENING | Kanban multi-select is missing (Q8; defect F-09); 200-item cap message; per-item expected versions (D77.3; defect B-02); real e2e. |
| UC-WM-B15 | Use the item action menu everywhere | B | Any viewer | not applicable | yes | complete | none | INTEGRATING | real e2e; proof that an item deleted while the menu is open is announced. |
| UC-WM-B16 | See item face states (card and row) | B | Any viewer | yes | yes | complete | none | INTEGRATING | Non-colour marks proof; 99+ cap |
| UC-WM-B17 | Purge items, boards and fields after their 30-day window | B | System (job); Manager restores before | no | partial | none | none | DEFINED | no purge job for items, boards or fields; decided in D78.10; needs the audit fact and tests. |
| UC-WM-B18 | Open an item (side sheet or full page) | B | Viewer and above; Guest per grant | yes | yes | complete | item-page | INTEGRATING | paged comments, history and sub-items; replace the 'not available yet' fallback copy (defect F-10); proof of the section order and the sheet behaviours. |
| UC-WM-B19 | Edit the item description | B | Editor and above | partial | yes | complete | none | INTEGRATING | no actor name on the conflict response (Q12 needs a contract addition); PATCH has no Idempotency-Key. |
| UC-WM-B20 | Link two items (waits on / related) | B | Editor | partial | partial | partial | none | INTEGRATING | Server acyclic check (D76.8); cross-board; typed client; design for sheet |
| UC-WM-B21 | Change or remove an item link | B | Editor | partial | partial | partial | none | INTEGRATING | Dangling-link policy (D76.8); typed client |
| UC-WM-B22 | See sub-items and derived progress in the item | B | Any viewer | yes | yes | partial | sub-items | HARDENING | the progress rule of D76.10 is partly built (see UC-WM-D09); design for the progress bar. |
| UC-WM-B23 | Recover from item deleted/archived/moved while open | B | Any viewer | partial | yes | complete | none | INTEGRATING | Actor in API/realtime frames (Q12); real e2e |
| UC-WM-B24 | Request an approval on an item or board | B | Requester with Editor | yes | yes | partial | approvals | HARDENING | One approver per request validation (D76.11); typed client; no mock handler |
| UC-WM-B25 | Decide (approve or reject) a request | B | Named approver (team: any member) | partial | yes | none | approvals | HARDENING | ApproverTeamId is ignored so team approvers can never decide; two decide paths exist (board-scoped with version, workspace without) (defect B-08). |
| UC-WM-B26 | Cancel a request | B | Requester or manager | yes | yes | none | none | INTEGRATING | Race test; real e2e; design |
| UC-WM-B27 | Re-request after rejection or cancel | B | Requester | partial | partial | none | none | INTEGRATING | no link from the new request to the previous one (verified: no previous-request field in the Approvals domain); no design. |
| UC-WM-B28 | See approval state and waiting-review figures | B | ViewBoard | yes | yes | partial | approvals | HARDENING | Tests for List; waiting-review count-once proof |
| UC-WM-B29 | Import tabular data into a board | B | Editor-level (CreateItem) | no | no | none | none | DEFINED | Roadmap 05: CSV/XLSX import, 5,000 rows, all-or-nothing (D76.15); design, BE, FE |
| UC-WM-B30 | Map import columns to fields | B | Editor | no | no | none | none | DEFINED | Part of D76.15 import; design, BE, FE |
| UC-WM-B31 | Export a board or view | B | Anyone who can view, except Guests | no | no | none | none | DEFINED | CSV/XLSX of the current view with filters (D76.15); design, BE, FE |
| UC-WM-B32 | Provider/migration import | B | Integrations | no | no | none | none | DEFINED | Integrations-owned migration path; no WM work scheduled |
| UC-WM-B33 | Handle a stale-version write conflict | B | Editor | partial | yes | complete | none | INTEGRATING | Actor on conflict (Q12 contract); EV gaps; real e2e |
| UC-WM-B34 | Item deleted or archived by someone else while open | B | Any viewer (see UC-) | partial | yes | complete | none | INTEGRATING | actor name on the deleted notice needs the Q12 contract addition; live delivery needs open PR #193; real e2e. |
| UC-WM-B35 | Recover from a target lost during commit | B | System | partial | not applicable | not applicable | none | INTEGRATING | the race is not covered by a test. |
| UC-WM-B36 | See a bulk failure | B | Editor | yes | yes | complete | none | INTEGRATING | real e2e; proof that the failing item is named and the selection stays. |
| UC-WM-C01 | Add a custom field | C | Editor | yes | yes | complete | none | HARDENING | Real e2e; DateTime and file types deferred; design Er/T/M |
| UC-WM-C02 | Rename a field or change its settings | C | Editor | yes | yes | complete | none | HARDENING | Real e2e; stale-schema conflict proof |
| UC-WM-C03 | Change a field default | C | Editor | yes | partial | complete | none | INTEGRATING | Test existing values unchanged; UI proof |
| UC-WM-C04 | Archive or delete a custom field | C | Editor | partial | yes | none | work-board-management | HARDENING | Soft delete with 30-day restore (D76.5); dependents rejection; EV; design |
| UC-WM-C05 | Restore an archived field | C | Editor | no | no | none | none | IMPLEMENTING | no restore endpoint although the domain event exists; field delete is a hard delete today (see Part 1 defects); the UI flag locks field archive. |
| UC-WM-C06 | Attempt to edit or delete a system field | C | Any (refused) | partial | yes | complete | none | HARDENING | API test: delete/edit system field refused |
| UC-WM-C07 | Manage Select / Status / Priority options | C | Editor (statusManageRoles on views) | yes | yes | partial | board-settings | HARDENING | design for the replacement picker; live refresh needs open PR #193. |
| UC-WM-C08 | Set a Status option's category | C | Editor | yes | yes | partial | none | HARDENING | API test 'last Done option'; e2e |
| UC-WM-C09 | Define Text and Long text fields | C | Editor | yes | partial | complete | none | HARDENING | tests and owner check; Table cell editing is built. |
| UC-WM-C10 | Define a Number field | C | Editor | yes | partial | complete | none | HARDENING | Real e2e; normalized-value proof |
| UC-WM-C11 | Define Date and DateTime fields | C | Editor | yes | partial | complete | none | HARDENING | Real e2e; tz-independent day proof; DateTime deferred |
| UC-WM-C12 | Define Select and Multi-select fields | C | Editor | yes | partial | complete | none | HARDENING | Real e2e; +N display |
| UC-WM-C13 | Define a Person (People) field | C | Editor | yes | yes | complete | none | HARDENING | Guest picker no-email proof; real e2e |
| UC-WM-C14 | Define a Link (and file) field | C | Editor | yes | partial | complete | none | HARDENING | Real e2e; absolute http(s) rule proof |
| UC-WM-C15 | Define a Checkbox field | C | Editor | yes | partial | complete | none | HARDENING | Real e2e |
| UC-WM-C16 | Status and Priority as specialized select types | C | Editor | yes | yes | complete | none | INTEGRATING | Real e2e; status per board proof |
| UC-WM-C17 | Keep one semantic contract per field type | C | Engineering (acceptance gate) | yes | yes | not applicable | none | INTEGRATING | fixed-dataset parity test (client vs server) for 12 types |
| UC-WM-C18 | Relation field: reference items of another board | C | Editor | partial | no | none | none | IMPLEMENTING | Relations.* endpoints exist (create, list, delete, pause, resume) with no tests and no UI; no Relation field type exists. |
| UC-WM-C19 | Mark a field required | C | Editor | no | no | none | none | DEFINED | no required flag, no enforcement on any write path; decided in D78.7. |
| UC-WM-C20 | Define a Formula field | C | Editor | yes | yes | partial | none | INTEGRATING | Cycle error design; formula Er; real e2e |
| UC-WM-C21 | Define a Rollup field | C | Editor | yes | yes | complete | none | INTEGRATING | Real e2e |
| UC-WM-C22 | Create, rename, recolor and delete a label | C | Editor | partial | yes | complete | none | HARDENING | EV on update/delete; real e2e |
| UC-WM-D01 | Validate a value against the current schema | D | System (every write path) | yes | partial | not applicable | none | HARDENING | Cross-board field-id rejection test |
| UC-WM-D02 | Edit a custom field value in place (cell, sheet, page) | D | Editor and above | yes | partial | complete | none | HARDENING | tests and owner check; conflict copy names no actor until the API carries one (Q12). |
| UC-WM-D03 | Clear a value (explicit clear) | D | Editor | partial | partial | partial | none | HARDENING | Wire clear in UI; test omitted-date means unchanged |
| UC-WM-D04 | Set start and due dates | D | Editor | partial | yes | complete | none | HARDENING | EV on due date; inverted-range proof; real e2e |
| UC-WM-D05 | Change an item's status | D | Editor and above | partial | yes | complete | none | HARDENING | EV on status write; four-path completion-time test; real e2e |
| UC-WM-D06 | Set priority | D | Editor | yes | yes | complete | none | HARDENING | Real e2e |
| UC-WM-D07 | Read a formula value (valid, stale, error) | D | Any viewer | partial | yes | partial | none | HARDENING | Valid/stale/error states; design |
| UC-WM-D08 | Rollup and relation authorization | D | Any viewer | partial | not applicable | not applicable | none | INTEGRATING | masking of unreadable children is not proven by a test; the rollup is evaluated in BoardItemFieldProjection. |
| UC-WM-D09 | Show derived progress of an item with child Items | D | Any viewer | partial | partial | partial | sub-items | HARDENING | only the child-item share is derived today (BoardItemReadValues); the checklist and status-category fallbacks are not. |
| UC-WM-D10 | Keep one progress truth | D | Product rule | partial | partial | not applicable | none | INTEGRATING | implement the D76.10 precedence on the server; test. |
| UC-WM-D11 | Add or remove labels on an item | D | Editor and above | yes | yes | complete | none | HARDENING | Real e2e; 200-label window |
| UC-WM-D12 | Handle a client with a stale field or view schema | D | Editor with old schema | partial | partial | none | none | INTEGRATING | no test of a stale-field write refusal. |
| UC-WM-E01 | Create a group | E | Editor (CreateItem class) | yes | yes | partial | board-settings | HARDENING | Design for create flow; no-group notice proof |
| UC-WM-E02 | Rename or edit a group | E | Editor | yes | yes | partial | board-settings | HARDENING | Design Er/P; no-op (same name) proof |
| UC-WM-E03 | Archive or delete a group | E | Editor | yes | partial | none | work-board-management | HARDENING | Design; archive/unarchive UI or drop; dialog-open case |
| UC-WM-E04 | Move an item between groups (Table) | E | Editor | yes | yes | complete | none | HARDENING | Real e2e; status-unchanged proof |
| UC-WM-E05 | Collapse or expand a group (personal) | E | Any role (personal, client-side) | not applicable | partial | complete | none | HARDENING | proof that a collapsed group stays collapsed after a refetch; Table group collapse persistence. |
| UC-WM-E06 | Move an item between groups (Table) | E | Editor and above | yes | yes | complete | none | HARDENING | Real e2e; two-user same-position test |
| UC-WM-F01 | Reorder fields / columns | F | Editor (shared); any role (personal) | yes | partial | partial | none | HARDENING | tests and owner check; concurrent reorder test; design. Personal Table column order is built (Alt+Arrow in the Cột menu, kept on the board preference). |
| UC-WM-F02 | Reorder groups | F | Editor | yes | yes | none | none | HARDENING | Design; deterministic-order test |
| UC-WM-F03 | Reorder an item within its group or lane | F | Editor | yes | yes | complete | none | HARDENING | No-op move proof; real e2e |
| UC-WM-F04 | Resolve an ordering collision | F | System | yes | yes | not applicable | none | HARDENING | test of ten concurrent inserts into one ordering scope. |
| UC-WM-G01 | Add and edit a checklist on an item | G | Editor and above | yes | yes | complete | none | HARDENING | Optional assignee and convert-to-sub-item (D69) proof; real e2e |
| UC-WM-G02 | Complete, reorder and delete checklist items | G | Editor | yes | yes | complete | none | HARDENING | a second unregistered CreateChecklistItem endpoint file is dead code (defect B-13). |
| UC-WM-H01 | Create a view | H | Editor | yes | yes | none | saved-views | HARDENING | design for view create, edit and delete; proof that Calendar needs a date field. |
| UC-WM-H02 | Edit a shared view's configuration | H | Editor | partial | yes | complete | saved-views | HARDENING | PUT and PATCH carry no expected version (defect B-06). |
| UC-WM-H03 | Delete or archive a view | H | Editor | partial | partial | none | none | HARDENING | delete-default asks for the next default (D76.9); archive and restore handlers have no test (defect B-14); design. |
| UC-WM-H04 | Set the default view | H | Editor | yes | yes | none | saved-views | HARDENING | Exactly-one default invariant and delete flow (D76.9); design |
| UC-WM-H05 | Create and use a private view | H | Editor (creator only sees it) | partial | yes | none | saved-views | HARDENING | Design states; prove absent from others' list and realtime (D77.4) |
| UC-WM-H06 | Filter the view (personal state) | H | Any role incl. Viewer | partial | yes | complete | none | HARDENING | tests and owner check; server-side filter on paged boards (Q9) is not built. |
| UC-WM-H07 | Sort the view | H | Any role | partial | yes | complete | none | HARDENING | tests and owner check; server-side sort is not built. |
| UC-WM-H08 | Group the view | H | Any role | partial | yes | complete | none | HARDENING | tests and owner check; 'No value' group proof. |
| UC-WM-H09 | Save, share, edit and delete a saved filter | H | Editor (create/share); Viewer applies | yes | yes | complete | none | HARDENING | API tests; real e2e; criteria without sort; open PR #201 touches the menu. |
| UC-WM-H10 | Apply a saved filter | H | Anyone who can see it | yes | yes | complete | none | INTEGRATING | real e2e; re-check against open PR #201. |
| UC-WM-H11 | Return to the shared view ("Về view chung") | H | Any role | yes | yes | complete | none | HARDENING | tests and owner check; the owner confirms the icon button with a dot. |
| UC-WM-H12 | Repair stale view references | H | System; Editor | not applicable | no (open PR #201) | complete | none | DEFINED | the stale-condition hint is in open PR #201; real e2e. |
| UC-WM-H13 | Switch and open views via tabs and URL | H | Any viewer | yes | yes | complete | saved-views | HARDENING | proof that Back and Forward follow the URL; unknown-view notice. |
| UC-WM-H14 | Apply workspace time zone to date filters and buckets | H | Any member | yes | partial | none | work-board-management | HARDENING | proof of date buckets in the workspace time zone, after open PR #201. |
| UC-WM-H15 | Make a field filterable, sortable, groupable per its type | H | System | partial | partial | not applicable | none | INTEGRATING | server-side filter, sort and group with parity to the client for every field type. |
| UC-WM-H16 | Search items and boards | H | Any member; Guest shared only | yes | yes | complete | none | HARDENING | Real e2e; Private-board no-leak test |
| UC-WM-H17 | Filter by text inside a board | H | Any role | no | partial | complete | none | HARDENING | BoardItems.List has no q parameter; search runs on the client. |
| UC-WM-H18 | List My Work | H | Any member | yes | yes | complete | work-feeds | HARDENING | Prove D76.17 rule (not-done, assigned, due order); Er/L design |
| UC-WM-H19 | Filter items by label | H | Any role | partial | no (open PR #201) | complete | none | DEFINED | label operators are in open PR #201, not on develop; no server-side label filter. |
| UC-WM-I01 | Use the Table view | I | Any viewer; edit per role | yes | yes | complete | none | HARDENING | tests and owner check; virtualization (UC-WM-T01) |
| UC-WM-I02 | Arrange columns (personal) and save to the shared view | I | Any role (personal); Editor (save to view) | yes | yes | complete | none | HARDENING | tests and owner check |
| UC-WM-I03 | Select items across views and windows | I | Editor and above | not applicable | partial | complete | none | HARDENING | tests and owner check; proof of the 200 cap; no selection across windows. |
| UC-WM-J01 | Use the Kanban view | J | Any viewer; edit per role | yes | yes | complete | none | HARDENING | tests and owner check; the checkbox on the card face is not on an artboard; the bar archives at once (no undo window as in the Table). |
| UC-WM-J02 | Move an item between Kanban lanes | J | Editor and above | yes | yes | complete | none | HARDENING | tests and owner check |
| UC-WM-J03 | Move an item across swimlanes | J | Editor | yes | yes | complete | none | HARDENING | tests and owner check; Q13 multi-assignee proof. |
| UC-WM-J04 | Use swimlanes | J | Any role | yes | yes | complete | none | HARDENING | tests and owner check; Q13 multi-assignee proof; 320 snap-scroll design. |
| UC-WM-J05 | Choose fields shown on the card | J | Editor | yes | yes | complete | none | HARDENING | tests and owner check |
| UC-WM-J06 | Set a WIP limit and see the warning | J | Editors of the view | yes | yes | complete | none | HARDENING | tests and owner check. |
| UC-WM-K01 | Use the Calendar view | K | Any viewer | partial | yes | complete | board-views-and-settings | HARDENING | tests and owner check; detail is a later roadmap item (D76.18); multi-day bars and the undated aside redraw are design (D72); a Calendar over a chosen date field is not built (it shows the start and due of each item). |
| UC-WM-L01 | Use the Timeline (Gantt) view | L | Any viewer; editors resize | partial | yes | complete | board-views-and-settings | HARDENING | tests and owner check; detail is a later roadmap item (D76.18); no auto-reschedule test; cycle check test |
| UC-WM-M01 | Provide board work figures to the Space overview | M | System; Workspaces | yes | yes | complete | none | HARDENING | tests and owner check (cross-tenant and Private negative test); the product doc counts a request aimed at the Board in waiting review, the port counts only Items: owner to decide which holds. |
| UC-WM-N01 | Create a form on a board | N | Editor | yes | yes | complete | none | HARDENING | tests and owner check |
| UC-WM-N02 | Map form questions to fields | N | Editor | yes | yes | complete | none | HARDENING | tests and owner check; UpdateFormQuestion has no test (defect B-14). |
| UC-WM-N03 | Publish, close, delete or restore a form | N | Manager | yes | yes | complete | none | HARDENING | tests and owner check |
| UC-WM-N04 | Submit a form as an authenticated member | N | Member who can create items | partial | yes | complete | none | HARDENING | tests and owner check; the form fill page refuses unsupported question types. |
| UC-WM-N05 | Process, reject or mark spam on a submission | N | Editor (UpdateItem) | yes | yes | complete | none | HARDENING | tests and owner check. |
| UC-WM-N06 | Submit a public (anonymous) form | N | Public (anonymous) | no | no | none | none | BLOCKED | Owner decision 67: execution principal and abuse controls |
| UC-WM-O01 | Read item activity / history | O | Viewer and above | yes | yes | partial | none | HARDENING | Design E/L/Er; one-line-per-action proof; real e2e |
| UC-WM-O02 | Provide first-work-item facts for getting-started | O | System | partial | not applicable | not applicable | none | INTEGRATING | the adapter exists in Infrastructure (FirstWorkItemFactsAdapter); no test of it was found; add one. |
| UC-WM-O03 | Expose events and projections to Analytics | O | Analytics (consumer) | partial | not applicable | not applicable | none | INTEGRATING | 38 integration events; three consumers implemented and the rest are logging stubs (defect B-13). |
| UC-WM-O04 | Publish stable Work Management facts | O | System | partial | not applicable | not applicable | none | HARDENING | Single-logical-event proof under retry |
| UC-WM-P01 | See a colleague's change live | P | Any viewer | yes | yes | complete | none | HARDENING | real e2e spec for two users on one board and card, tests of the notifier and the adapter, and owner check. |
| UC-WM-P02 | Keep editing safe while others edit | P | Editor | not applicable | yes | complete | none | HARDENING | real e2e spec, tests that an unsaved draft and a drag survive a frame, the table cell choice driven live, and owner check. |
| UC-WM-P03 | Lose and regain the connection | P | Any viewer | yes | yes | partial | none | HARDENING | real e2e spec, reconnect and gap tests, the status measured in dark and at 1024, and owner check. |
| UC-WM-P04 | Lose access or role while the board is open | P | Any viewer | yes | yes | complete | none | HARDENING | real e2e spec, a test of the 60 s re-check, and owner check. |
| UC-WM-Q01 | Let an Automation rule act on items | Q | Rule executor identity | yes | yes | complete | automation | HARDENING | Loop-guard and executor-lost tests |
| UC-WM-Q02 | Show automation-caused changes on the board | Q | Any viewer | partial | no (open PR #193) | complete | none | HARDENING | live refresh is in open PR #193, not on develop; test that history shows the rule or executor as actor. |
| UC-WM-Q03 | Sync items with an external provider | Q | Integrations | no | no | not applicable | none | DEFINED | Integrations-owned; idempotent replay test when built |
| UC-WM-R01 | Comment on an item | R | Commenter and above | yes | yes | partial | none | HARDENING | Design gaps; real e2e |
| UC-WM-R02 | Reply, edit or delete a comment | R | Author; moderator separate | yes | yes | partial | none | HARDENING | Design edit/delete; real e2e |
| UC-WM-R03 | Mention a person in an item comment | R | Commenter and above | yes | yes | complete | none | HARDENING | No-notify-for-no-access proof; Guest list |
| UC-WM-R04 | Attach files to an item | R | Editor and above (upload); ViewBoard (view) | yes | yes | complete | none | HARDENING | Real e2e; Viewer upload refused test |
| UC-WM-R05 | Link a Document from an item | R | Editor | yes | yes | complete | none | HARDENING | Typed client; no-title-for-unreadable proof; real e2e |
| UC-WM-R06 | Watch or unwatch an item | R | Any viewer | yes | yes | none | following, follow-notifications | HARDENING | Typed client; design; auto-watch decision open |
| UC-WM-R07 | Notify and attend (assignment, mention, approval, overdue, watching) | R | Collaboration | partial | yes | complete | follow-notifications | HARDENING | Other types have no producer; decide set |
| UC-WM-R08 | Reference items from Documents | R | Documents | partial | partial | not applicable | none | INTEGRATING | Private-board embed test |
| UC-WM-S01 | Apply billing entitlements | S | Billing decides | partial | not applicable | not applicable | none | DEFINED | no Work Management command implements IRequireFeature (verified); entitlement checks stay behind the port and allow. |
| UC-WM-T01 | Use the view on a large (paged) board | T | Any viewer | no | no | complete | none | DEFINED | BoardItems.List, GetFullBoard and Trash reads are unpaged; filter and sort run on the client (defects B-11, F-07). |
| UC-WM-T02 | Use the view at 1440 / 1280 / 1024 / 768 / 320 px | T | Any viewer | not applicable | partial | complete | none | INTEGRATING | 1-px box-tree diff at 1440, 1024 and 390 px in light and dark; 44 px targets. |
| UC-WM-T03 | Published limits and limit-reached states | T | System; any viewer | no | no | none | none | DEFINED | no limit is published or enforced; the limits are decided in D78.8. |
| UC-WM-T04 | Work with boards on the native mobile app | T | Any member on native app | not applicable | no | partial | none | DEFINED | the mobile package is placeholder screens; web slices come first (D78.14). |
| UC-WM-T05 | Rebuild derived and materialized projections | T | System | no | not applicable | not applicable | none | DEFINED | no projection exists; per-read evaluation is accepted until a board at the limit misses the performance budget (D78.8). |

# 3. Use cases

Standard rules used in the blocks:

- [S1] No ViewBoard (other workspace, or the board is missing): one identical NotFound that never names the board. A child of an archived or deleted board is NotFound to non-managers.
- [S2] Has ViewBoard but lacks the level: 403; the UI shows the control disabled with the reason. The server decides every write.
- [S3] Archived board, or archived or deleted item: writes are refused with the reason; reads work.
- [S4] Stale expected version: the conflict fails before commit and nothing changes; the UI offers re-read or keep mine; the notice names the actor (Guests see "Một người khác", Q12).
- [S5] Semantic no-op: no version bump, no event, no activity, no automation trigger.
- [S6] Realtime is notify-only; other members refetch; duplicate, out-of-order or missed frames are harmless.
- [S7] A retry of the same submission reuses the Idempotency-Key and has no second effect; changed content is a new submission.

Evidence conventions. Routes are shown without the `/api/v1` prefix. Handler paths are under `backend/src/Notrelix.Application/Features/WorkManagement/`. In a route entry, `[EV / Idem]` means the write requires an expected version and an Idempotency-Key; `-` means it does not. The Tests cell lists backend test files that name the handler's command or query; `none found` means no test references it. FE paths are repository-relative. Design names are artboards of the design canvas (owner artifact `18L4jUXGfdXh7Wn9YQvA2x`); `Er`, `L`, `E`, `M` and `NP` in a design note mean error, loading, empty, mobile and no-permission states.

## A. Board lifecycle

Team capability WM-001.

### UC-WM-A01 Create a board (blank, preset or from template)
- Capability: A Board lifecycle (WM-001)
- Actors and permission: Owner/Admin/Member of the Workspace via `CreateBoard` at root; in a Space the Space level decides (Editor on the Space); Guest denied.
- Preconditions: Active Workspace; a target Space, when given, belongs to the same Workspace.
- Input: name (required), optional key prefix, optional description, template or preset or board type, optional Space. Name length limit not stated in docs.
- Output: Board with its own identity, default Status field (Cần làm / Đang làm / Chờ duyệt / Hoàn thành, WM-042), default view; creator becomes board Owner/Manager; BoardCreated fact. Getting-started `first-item` stays derived.
- Main flow: 1 choose "New board"; 2 pick template (published workspace templates + built-in starters) or blank; 3 name it; 4 submit; 5 land on default view.
- No-op / rejection / concurrency / retry: Board belongs to exactly one Workspace (WM-003); template is creation input only, instance is independent (WM-005, WM-028); cross-Workspace Space rejected before mutation; Billing may cap board count (§112); no permission = [S2]; retry idempotent [S7].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 7-9, 12-13, 101-102; WM-003, WM-005, WM-028, WM-042; WMREQ001-003, WMREQ008-010, WMREQ016
- Acceptance: Given a Member in Workspace W, when they create a board from a template, then a new Board with new Field/Group/View identities exists in W and later template edits do not change it. Given a Guest, when they look for "New board", then it is not available (403 on API). Given a Space from another Workspace, when chosen, then the request is rejected and nothing is created.
- Evidence: BE Boards.Create = POST /workspaces/{workspaceId:guid}/boards -> CreateBoardInWorkspaceCommand (`Boards/Commands/CreateBoardInWorkspace/CreateBoardInWorkspace.cs`) [- / Idem]; Templates.CreateBoard = POST /templates/{templateId:guid}/create-board -> CreateBoardFromTemplateCommand (`Templates/Commands/CreateBoardFromTemplate/CreateBoardFromTemplate.cs`) [- / Idem] · FE `frontend/apps/web/src/shell/workspace/use-create-resource.tsx`; no template gallery route · Design Final-Space `tao-moi`, Final-M-Space; no Er, L, name-taken · Tests `BoardTemplateIntegrationTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; design Er and name-taken; tests and owner check. Evidence: real-stack check 2026-10-31: created a blank, a starter and a workspace-template board from New Board as a member (201, lands on the board, the reload keeps it); a retry under one key made one board; an empty or 121-character name 400; a Guest 403; an unknown Space 404, commit 33ed8c465.

### UC-WM-A02 Rename the board and edit its settings
- Capability: A Board lifecycle (WM-001)
- Actors and permission: Manager (ManageBoard) incl. Owner; Editor reads settings only. Guest never.
- Preconditions: Board is active (not archived or deleted).
- Input: Name 1-120 characters after trim (D78.3), description, key prefix, typed flexible settings validated per setting.
- Output: Board identity unchanged by rename; items keep their keys (WM-043); BoardChanged fact; other members see the new name by [S6].
- Main flow: 1. Open Settings, General. 2. Edit name, description, prefix or a setting. 3. Save; the page shows the saved values.
- No-op / rejection / concurrency / retry: invalid or unknown setting = validation error, never stored as raw JSON; [S2] for Editor/Commenter/Viewer; [S4] if version carried; [S5].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 9, 14; WM-043; WMREQ005, WMREQ010, WMREQ095-097
- Acceptance: Given a Manager renames the board, when saved, then URL id and item keys are unchanged and members see the new name without reload. Given an Editor opens Settings, then fields are read-only with reason. Given items PRJ-1..3 and a prefix changed to NEW, when saved, then PRJ-1..3 keep their keys and the next item is NEW-4.
- Evidence: BE Boards.Rename = PATCH /boards/{boardId:guid} -> UpdateBoardCommand (`Boards/Commands/UpdateBoard/UpdateBoard.cs`) [EV / Idem] · FE `frontend/packages/product/work-management/web/src/components/board-settings/board-settings-general.tsx` (icon unchangeable) · Design Final-Settings `chung`; Er-on-save, 320, 768 missing · Tests `BoardManagementLevelTests.cs` · Real e2e `board-settings.real.e2e.spec.ts`, `work-board-management.real.e2e.spec.ts` (key prefix)
- State: HARDENING — tests and owner check; a board name is 1 to 120 characters on create and rename (D78.3); not built: icon change, design Er/320/768.

### UC-WM-A03 Place a board in a Space or move it between Spaces
- Capability: A Board lifecycle (WM-001)
- Actors and permission: Manager (ManageBoard on the board; and CreateBoard on target Space when moving into one).
- Preconditions: Board is active; the target Space exists in the same Workspace; the actor may create boards there.
- Input: Target Space id, or none for the Workspace root.
- Output: placement reference changes; tenant identity unchanged (WM-003); access may change because Space inheritance changes; users who lose access get [S1] next read.
- Main flow: open board menu, "Move to Space", pick Space, confirm after access-impact warning.
- No-op / rejection / concurrency / retry: cross-Workspace rejected; warns about open Items assigned to people who will lose access and offers to grant first (WM-040); no unassign afterwards.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline; destructive confirm (names the impact)
- Rules: product sections 101-102; WM-003, WM-040; WMREQ002, WMREQ005
- Acceptance: Given a Manager moves a board to another Space, when confirmed, then the board remains in the same Workspace and a person with only the old Space's access receives NotFound. Given assignees would lose access, then a warning lists them and offers to grant access.
- Evidence: BE Boards.MoveToSpace = POST /boards/{boardId:guid}/move-to-space -> MoveBoardToSpaceCommand (`Boards/Commands/MoveBoardToSpace/MoveBoardToSpace.cs`) [EV / Idem] · FE `frontend/apps/web/src/shell/workspace/use-move-to-space.tsx`, `frontend/apps/web/src/shell/workspace/board-placement-bridge.ts`; no impact warning · Design none; needs move-board dialog, access-impact warning list, error · Tests `BoardManagementLevelTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; access-impact warning with grant offer (UC-WM-A25), move dialog design; tests and owner check. Evidence: real-stack check 2026-10-31: moved a board to a department from the Library dialog and the reload shows the new location; the same Space is a no-op; a member 403; an unknown Space 404, commit 33ed8c465.

### UC-WM-A04 Archive a board
- Capability: A Board lifecycle (WM-001)
- Actors and permission: Manager (ManageBoard).
- Preconditions: Board is active.
- Input: Board id and expected version.
- Output: board read-only with archived banner; items, fields, views remain; BoardArchived fact; Automation/Integrations pause per lifecycle contract.
- Main flow: 1. Open Settings, General, Archive. 2. Confirm the dialog that names the effect. 3. The board turns read-only with an archived banner; its rules and sync pause.
- No-op / rejection / concurrency / retry: Archiving an archived board is a no-op [S5]. Below Manager = 403 [S2]. Stale version = conflict [S4]. Retry with the same Idempotency-Key has no second effect [S7]. Reads keep working; writes are refused with the reason [S3].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline; destructive confirm (names the impact)
- Rules: product sections 10-11; D76.1; WMREQ004-005, WMREQ110-113
- Acceptance: . Given an archived board, when a Member tries any write via API, then it is refused with the archived reason and no version/event changes. Given a board with enabled automation rules, when it is archived, then its rules and integration sync pause; when unarchived they resume.
- Evidence: BE Boards.Archive = POST /boards/{boardId:guid}/archive -> ArchiveBoardCommand (`Boards/Commands/ArchiveBoard/ArchiveBoard.cs`) [- / Idem] · FE `frontend/packages/product/work-management/state/src/api/board-settings.api.ts`:367 raw path; archived banner · Design Final-Settings `luu-tru`, BC-Shell archived banner · Tests `ArchiveBoardCommandHandlerTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; automation and integration sync are not paused (D76.1); tests and owner check. Built: archive carries the board version (`BoardVersionRequest`), the typed client, a confirmation that names the effect with item, open-item and member counts (`GET /boards/{id}/lifecycle-impact`), an archived board opens read-only with a banner (`GET /boards/{id}/full` no longer refuses it) and the views, item sheet and item page turn every write control off through `useBoardCapabilities`, and the archived list at `/workspaces/{id}/archived-boards`. Built: an archived board and its children are reachable to whoever passes GOV-025 steps 2-6, so the read, the access read and the archived list answer, and the policy refuses every write on the board or a child with `WorkManagement_Board_Archived` once the decision is Allow, after the NotFound checks (owner decision 2026-10-30; the one rule covers every child write through the board-child relation, no extra pipeline behaviour); archive, unarchive, delete and restore carry `IBoardLifecycleRequest` and pass; the viewer's personal view state stays on screen only on an archived board. Archive carries the lifecycle marker too, so archiving an archived board stays a no-op and a retry under the same key replays. Not built: pausing automation rules and integration sync (D76.1; defect B-18), live banner for peers (open PR #193). Evidence: real-stack check 2026-10-31: archived from Settings through the confirm that names the effect (204); the reload shows the archived banner; a write answers 403 with the reason and a member still reads; archiving again bumps nothing; a stale version 412; a member 403, commit 33ed8c465.

### UC-WM-A05 Unarchive a board
- Capability: A Board lifecycle (WM-001)
- Actors and permission: Manager.
- Preconditions: Board is archived.
- Input: Board id and expected version.
- Output: Board returns to active; every item, field and view is unchanged; a BoardChanged fact is emitted; automation rules and integration sync resume (D76.1).
- Main flow: 1. Open the archived board's Settings. 2. Choose Unarchive. 3. Writes re-enable and paused rules and sync resume.
- No-op / rejection / concurrency / retry: [S2] below Manager; archive-then-unarchive keeps all items unchanged; no-op if already active [S5].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 10-11; D76.1; WMREQ004-005
- Acceptance: Given an archived board, when a Manager unarchives it, then all controls re-enable for Editors without reload.
- Evidence: BE Boards.Unarchive = POST /boards/{boardId:guid}/unarchive -> UnarchiveBoardCommand (`Boards/Commands/UnarchiveBoard/UnarchiveBoard.cs`) [- / Idem] · FE none; operation unused, no UI · Design none; needs unarchive action on archived banner/settings · Tests `BoardManagementLevelTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; resuming automation and integration sync (D76.1); tests and owner check. Built: unarchive carries the board version; the archived banner and the General settings danger zone offer Unarchive to a Manager; the archived list at `/workspaces/{id}/archived-boards` unarchives from a row. Built: unarchive now reaches the handler (the access facts no longer answer NotFound for an archived board) and passes the policy as a lifecycle command. Not built: resuming automation and integration sync (D76.1), design. Evidence: real-stack check 2026-10-31: unarchived from the banner (204) and the banner stays gone after reload; a second unarchive bumps nothing; a stale version 412; writes work again, commit 33ed8c465.

### UC-WM-A06 Delete a board
- Capability: A Board lifecycle (WM-001)
- Actors and permission: Manager (delete/restore are ManageBoard). Owner of workspace.
- Preconditions: Board is active or archived.
- Input: Board id, expected version, typed confirmation.
- Output: board leaves lists and becomes NotFound to non-managers; BoardDeleted fact. Deletion is explicit product lifecycle, not FK cascade symmetry (WM-033); comments/documents/automation/integration/analytics data is not destroyed by cascade.
- Main flow: 1. Open Settings, General, Delete. 2. Confirm the dialog that names the 30-day restore window. 3. The board leaves every list and becomes NotFound to people without ManageBoard.
- No-op / rejection / concurrency / retry: Deleting a deleted board is a no-op [S5]. Below Manager = 403 [S2]. Stale version = conflict [S4]. Retry with the same key has no second effect [S7]. Children of a deleted board are NotFound to non-managers [S1].
- UI states: destructive confirm (names the impact)
- Rules: product sections 121-122; WM-033; D76.1; WMREQ004-005, WMREQ113
- Acceptance: Given a Manager deletes a board, when a member reloads, then the generic no-access state shows and Collaboration comments of its items are retained (not hard-deleted). Given a board deleted 29 days ago, when a Manager restores it, then it returns; given 31 days (after purge), then restore is refused.
- Evidence: BE Boards.Delete = DELETE /boards/{boardId:guid} -> DeleteBoardCommand (`Boards/Commands/DeleteBoard/DeleteBoard.cs`) [- / Idem] · FE `frontend/packages/product/work-management/state/src/api/board-settings.api.ts`:373 raw DELETE, typed-name confirm · Design none; needs delete confirm naming impact, deleted state · Tests `BoardAuthorizationHardeningIntegrationTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; purge job after 30 days (UC-WM-B17), design; tests and owner check. Built: delete carries the board version (`?expectedVersion=n`), the typed client, a confirmation that names the 30-day window and the item count, and a redirect to the deleted list. Built: delete passes the policy on an active or archived board as a lifecycle command, and a deleted board stays reachable for Delete (`IBoardReachesDeletedRequest`, ManageBoard only), so deleting a deleted board is a no-op and a retry under the same key replays; a deleted board is NotFound to every other request. Not built: the purge job after 30 days (see UC-WM-B17), design. Evidence: real-stack check 2026-10-31: deleted with the typed name (204) and landed on the deleted list; a member and an unknown id now both answer 404 (restore and delete answered 403 before); a stale version 412; a member 403 on an active board, commit 33ed8c465.

### UC-WM-A07 Restore a deleted board
- Capability: A Board lifecycle (WM-001)
- Actors and permission: Manager.
- Preconditions: Board is deleted and inside the 30-day window.
- Input: Board id.
- Output: board and children visible again as before.
- Main flow: 1. A Manager opens the deleted-boards list. 2. Chooses Restore. 3. The board and its children are visible again as before.
- No-op / rejection / concurrency / retry: Outside the window restore is refused with the reason. Below Manager = 403 [S2]. Restoring an active board is a no-op [S5]. Retry with the same key has no second effect [S7].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 121-122; WM-033; D76.1; WMREQ004
- Acceptance: Given a deleted board, when a Manager restores it within retention, then items, fields, views return unchanged.
- Evidence: BE Boards.Restore = POST /boards/{boardId:guid}/restore -> RestoreBoardCommand (`Boards/Commands/RestoreBoard/RestoreBoard.cs`) [- / Idem] · FE none; no UI · Design none; needs deleted-boards list, restore, restore-refused · Tests `BoardManagementLevelTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; listing for Space and workspace managers who are neither the deleter nor a board Owner or Admin; purge job; tests and owner check. Built: `Board.Restore` refuses after 30 days with rule code `WorkManagement_Board_RestoreWindowElapsed`; restore carries the board version; `GET /workspaces/{id}/boards/deleted` lists the boards the caller deleted or holds Owner or Admin on, and the page at `/workspaces/{id}/archived-boards?tab=deleted` restores with the conflict, window, denied and gone states. Built: restore now reaches the handler: a deleted board is reachable only for `IBoardReachesDeletedRequest` (restore and delete; the facts query reads the deleted board and its Space only then). Not built: listing for Space and workspace managers who are neither the deleter nor a board Owner or Admin (needs a Governance listing), the purge job (UC-WM-B17). Evidence: real-stack check 2026-10-31: restored from the deleted list (204) and the board opens for admin and member after reload; past 30 days it is refused with the reason; restoring an active board bumps nothing; a retry under the same key replays (it answered 500 before); a stale version 412; a member 404, commit 33ed8c465.

### UC-WM-A08 List boards the person may see
- Capability: A Board lifecycle (WM-001)
- Actors and permission: any member; Guest sees only explicitly shared boards.
- Preconditions: Signed-in member of the Workspace.
- Input: Workspace id; optional Space id.
- Output: exactly the boards a single read would allow, computed in the server query, never filtered in memory (GOV-019/GOV-029); My Work lists included.
- Main flow: 1. Open the workspace or Space. 2. The server returns only boards a single read would allow. 3. The list renders with state, key and owner.
- No-op / rejection / concurrency / retry: A Guest never enumerates the Workspace [S1]. A board the person cannot see is absent, not marked.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1])
- Rules: product sections 103-105; WM-030; WMREQ105-108
- Acceptance: Given a Private board and a non-member Member, when boards are listed, then it is absent and a direct URL gives [S1]..
- Evidence: BE Boards.ListWorkspaceBoards = GET /workspaces/{workspaceId:guid}/boards -> GetBoardsQuery (`Boards/Queries/GetBoards/GetBoards.cs`) [- / -] · FE useWorkspaceBoards; contract op has no mock handler · Design Final-Space `chi-xem`, Final-M-Space · Tests `CollectionVisibilityIntegrationTests.cs` · Real e2e `guest-permission.real.e2e.spec.ts` (negative only)
- State: HARDENING — real e2e spec, tests, design measurement and owner check; tests and owner check. Evidence: real-stack check 2026-10-31: a Private board is absent from a member's list and present for its manager; the Library list shows the boards of the workspace, commit 33ed8c465.

### UC-WM-A09 Open a board (read: schema, groups, items, preferences)
- Capability: A Board lifecycle (WM-001)
- Actors and permission: Viewer and above, Guest per grant.
- Preconditions: Viewer or above on the board; Guest per grant.
- Input: Board id; optional view id in the URL.
- Output: shell and tabs at once, skeleton of the view, then schema + groups + first page (50, max 200) of active top-level items per group with totals; archived items and child items are not listed; default view shown unless URL names another; personal BoardPreference applied.
- Main flow: 1. Open the board URL. 2. Header and tabs render at once with a skeleton of the view. 3. Schema, groups and the first page of items arrive. 4. The default view, or the one named in the URL, shows.
- No-op / rejection / concurrency / retry: [S1]; read failure shows error with Retry; unknown view falls back to default with a notice; empty board states; archived banner; stale content kept but marked when a prior read exists.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1])
- Rules: product sections 26, 27.2, 46; WM-030; WMREQ111, WMREQ147-148
- Acceptance: (1) Given an archived board, when it opens, then a banner states archived, every write control is disabled with that reason, and reads work. (2) Given a view URL naming a deleted or unknown view, when opened, then the default view shows and one notice says the view no longer exists. (3) Given a board with no BoardGroup, when a Member tries to add, then the no-group notice names the reason and offers "Add group"; adding never returns silently (D49).
- Evidence: BE Boards.Get = GET /boards/{boardId:guid} -> GetBoardQuery (`Boards/Queries/GetBoard/GetBoard.cs`) [- / -]; Boards.GetOverview = GET /boards/{boardId:guid}/full -> GetFullBoardQuery (`Boards/Queries/GetFullBoard/GetFullBoard.cs`) [- / -]; BoardFields.GetSchema = GET /boards/{boardId:guid}/schema -> GetBoardSchemaQuery (`BoardSchema/Queries/GetBoardSchema/GetBoardSchemaQuery.cs`) [- / -]; BoardItems.List = GET /boards/{boardId:guid}/items -> GetBoardItemsQuery (`BoardItems/Queries/GetBoardItems/GetBoardItemsQuery.cs`) [- / -] · FE BoardScreen; whole board loaded, no paging · Design BC-Shell-1440, BC-Shell-1280-768, BC-Shell-320, BC-Index · Tests `CacheIdentityHashTests.cs`, `ComputedFieldsIntegrationTests.cs`, `GetBoardSchemaTests.cs`, `NotrelixApiFactory.cs` · Real e2e `board-views-and-settings.real.e2e.spec.ts` (implicit)
- State: HARDENING — real e2e spec, tests, design measurement and owner check; per-group paging (UC-WM-T01); tests and owner check. Evidence: real-stack check 2026-10-31: a board opens with its views; an archived board shows the banner and still reads; a deleted or Private board shows the not-available state to a member; the reload keeps the view, commit 33ed8c465.

### UC-WM-A10 Assign and keep the item key (prefix and sequence)
- Capability: A Board lifecycle (WM-001)
- Actors and permission: system on create (any actor who can create items).
- Preconditions: Actor may create items on the board.
- Input: None from the actor; the server allocates the key on create.
- Output: every Item gets key `{prefix}-{n}` or `n`, unique per board, same sequence for root and child items, never changes when moved/regrouped, never reused (WM-043). Key is shown on card, row, sheet, page, copy-link.
- Main flow: 1. The actor creates an item or sub-item. 2. The server allocates the next sequence for the board. 3. The key {prefix}-{n} (or n) shows on card, row, sheet, page and copied link.
- No-op / rejection / concurrency / retry: Concurrent creates never collide. A prefix change applies to new items only (D76.3). A moved item takes the target board's next key; the old key keeps resolving (D76.3).
- UI states: success (key shown on card, row, sheet, page and copied link)
- Rules: product sections 27.1; WM-043; WMREQ015
- Acceptance: Given 20 parallel creates on one board, then keys are unique and gapless-or-gapped but never duplicated; given an item is moved to another board, then it takes a key on the target board's sequence (see UC-WM-B08). Given an item moved to board B and keyed B-9, when its old key is opened, then it resolves to the same item.
- Evidence: BE BoardItems.Create = POST /boards/{boardId:guid}/items -> CreateBoardItemCommand (`BoardItems/Commands/CreateBoardItem/CreateBoardItem.cs`) [- / Idem] · FE key display (D39) · Design WM-Card-Face (key) · Tests `CreateBoardItemTests.cs` · Real e2e `work-board-management.real.e2e.spec.ts` (prefix only)
- State: HARDENING — real e2e spec, tests, design measurement and owner check; no stored alias, so an old key does not resolve after a move; tests and owner check. Evidence: real-stack check 2026-10-31: 20 parallel creates on one board gave 20 distinct keys, commit 33ed8c465.

### UC-WM-A11 Duplicate a board
- Capability: A Board lifecycle (WM-001)
- Actors and permission: Manager of the source board (ManageBoard) and CreateBoard on the target Space.
- Preconditions: Source board readable; actor has ManageBoard on it and CreateBoard on the target Space.
- Input: Source board id, new name, target Space, include-items option (default off).
- Output: A new independent board with new identities. It copies settings, fields, groups, views, labels and forms; items only when chosen, following the item duplicate rules; members are not copied; the duplicator becomes Manager (D78.5).
- Main flow: 1. Open the board menu, Duplicate. 2. Choose whether to copy items. 3. Submit; the new board opens.
- No-op / rejection / concurrency / retry: More than 500 items with include-items = refused with the reason (D76.13). Retry with the same key creates one copy [S7]. A failure leaves no half board.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 12-13, 97; WM-005, WM-028; D78.5; WMREQ008-009, WMREQ115-117
- Acceptance: Given a Manager duplicates a board without items, then a new board has the same fields, groups, labels and views and no items. Given the same request retried, then one copy exists.
- Evidence: BE none: no endpoint (inventory: board duplicate missing) · FE none · Design none; needs duplicate board dialog, in-progress, partial, done · Tests none found · Real e2e none
- State: DEFINED — no endpoint, no UI, no design; behaviour is decided in D78.5.

### UC-WM-A12 Save a board as a template
- Capability: A Board lifecycle (WM-001)
- Actors and permission: right to manage that Board (Manager).
- Preconditions: Actor has ManageBoard on the source board.
- Input: Template name, optional include-items flag.
- Output: workspace-scoped draft template copying fields, Status options, groups, labels, Board-visible views; items only on request: top-level items with field values, at most 500; never comments, files, members or sub-items.
- Main flow: 1. Open the board menu, Save as template. 2. Name it; optionally tick include items. 3. A draft template appears in the Workspace template list.
- No-op / rejection / concurrency / retry: More than 500 top-level items with include-items = refused with the reason, never truncated (D76.13). Below Manager = 403 [S2]. Retry with the same key creates one template [S7].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 96-98; WM-005, WM-028; D66; D76.13
- Acceptance: Given a board with 600 items and "include items", then the action states the 500 cap. Given a board with 501 items and include items ticked, then the action is refused with the reason and nothing is truncated.
- Evidence: BE Templates.Create = POST /boards/{boardId:guid}/templates -> CreateBoardTemplateCommand (`Templates/Commands/CreateBoardTemplate/CreateBoardTemplate.cs`) [- / Idem] · FE `frontend/packages/product/work-management/web/src/components/board-settings/board-settings-templates.tsx` · Design WM-Template-Save; Er missing · Tests `BoardManagementLevelTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; include-items 500 refusal driven; design Er; tests and owner check. Built: more than 500 top-level items with include-items is refused with rule code `WorkManagement_BoardTemplate_TooManyItems` instead of being cut short, and the form names the cap and disables Save. Not built: design Er. Evidence: real-stack check 2026-10-31: saved a board as a draft template (201, listed as Draft); an empty name 400; a member without Manage 403, commit 33ed8c465.

### UC-WM-A13 Publish a template
- Capability: A Board lifecycle (WM-001)
- Actors and permission: workspace Owner or Admin only.
- Preconditions: Template is a draft; actor has ManageBoard on the template (D66).
- Input: Template id.
- Output: draft becomes published and appears in "New board".
- Main flow: 1. Open Templates. 2. Choose Publish. 3. The template appears in New board for every member.
- No-op / rejection / concurrency / retry: Publishing a published template is a no-op [S5]. Without ManageBoard on the template = 403 [S2].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 96-98; WM-005; D66
- Acceptance: Given a Manager who is not Owner/Admin, when they publish, then 403.
- Evidence: BE Templates.Publish = PATCH /templates/{templateId:guid}/publish -> PublishBoardTemplateCommand (`Templates/Commands/PublishBoardTemplate/PublishBoardTemplate.cs`) [- / Idem] · FE `frontend/packages/product/work-management/web/src/components/board-settings/board-settings-templates.tsx` · Design WM-Template-Manage; denied/Er missing · Tests `BoardTemplateIntegrationTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; the template list returns Draft and Archived templates to every member; the product doc says Owner or Admin publishes and D66 says ManageBoard; tests and owner check. Evidence: real-stack check 2026-10-31: published a template (200); publishing again changes nothing; a member 403; it appears in New board for a member, commit 33ed8c465.

### UC-WM-A14 Archive or delete a template
- Capability: A Board lifecycle (WM-001)
- Actors and permission: Owner/Admin only; an archived template is not published again.
- Preconditions: Template exists; actor has ManageBoard on the template (D66).
- Input: Template id.
- Output: removed from "New board"; existing boards unaffected (WM-005).
- Main flow: 1. Open Templates. 2. Archive or delete the template. 3. It leaves New board; boards made from it are unchanged.
- No-op / rejection / concurrency / retry: Repeating the action is a no-op [S5]. Without ManageBoard on the template = 403 [S2].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline; destructive confirm (names the impact)
- Rules: product sections 96-98; WM-005; D66
- Acceptance: Given a template archived, then boards created from it are unchanged and it is no longer offered.
- Evidence: BE Templates.Archive = PATCH /templates/{templateId:guid}/archive -> ArchiveBoardTemplateCommand (`Templates/Commands/ArchiveBoardTemplate/ArchiveBoardTemplate.cs`) [- / Idem]; Templates.Delete = DELETE /templates/{templateId:guid} -> DeleteBoardTemplateCommand (`Templates/Commands/DeleteBoardTemplate/DeleteBoardTemplate.cs`) [- / Idem] · FE `frontend/packages/product/work-management/web/src/components/board-settings/board-settings-templates.tsx`; no unarchive · Design WM-Template-Manage · Tests `BoardTemplateIntegrationTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; same list gap as UC-WM-A13; no unarchive; tests and owner check. Evidence: real-stack check 2026-10-31: archived a template (200) and it left New board and could not be used (404); a member delete 403; delete 204, commit 33ed8c465.

### UC-WM-A15 Create a board from a template or built-in starter
- Capability: A Board lifecycle (WM-001)
- Actors and permission: as UC-WM-A01.
- Preconditions: As UC-WM-A01; the template is published or a built-in starter.
- Input: Template id or starter, board name, optional Space.
- Output: instance has normal identities; later template changes never rewrite the board (WM-028); built-in starters exist for every workspace.
- Main flow: 1. Choose New board. 2. Pick a published template or a starter. 3. Name it and submit. 4. Land on the new board's default view.
- No-op / rejection / concurrency / retry: As UC-WM-A01; the new board has no live link to the template (WM-028).
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1]); validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 12-13, 96-98; WM-005, WM-028; D66; WMREQ008-009
- Acceptance: Given a template edited after instantiation, then the existing board is unchanged.
- Evidence: BE Templates.CreateBoard = POST /templates/{templateId:guid}/create-board -> CreateBoardFromTemplateCommand (`Templates/Commands/CreateBoardFromTemplate/CreateBoardFromTemplate.cs`) [- / Idem]; Templates.List = GET /workspaces/{workspaceId:guid}/templates -> ListBoardTemplatesQuery (`Templates/Queries/ListBoardTemplates/ListBoardTemplates.cs`) [- / -] · FE create from template in settings; no gallery on New board path · Design WM-Template-Gallery; L, Er, E, M missing · Tests `BoardTemplateIntegrationTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; tests and owner check. Evidence: real-stack check 2026-10-31: a member created boards from a starter and from a workspace template (201, groups and fields copied, no items); a Guest 403; an unknown Space 404, commit 33ed8c465.

### UC-WM-A16 Read who has access (sharing summary)
- Capability: A Board lifecycle (WM-001)
- Actors and permission: ViewBoard to read counts; entries (names, levels, source) only for Board managers and Board members; log, privateImpact, permissionId, grantedBy only for managers. Guest is shown capped at Editor with `levelCapped`, no email.
- Preconditions: Viewer or above on the board.
- Input: Board id.
- Output: `GET /boards/{id}/sharing` composed from Governance sharing fact + Work Management figures; one entry per person at highest of explicit grant and board-member level, one per granted Team; Space data only if caller can see the Space.
- Main flow: 1. Open Share. 2. The server returns the access summary. 3. Managers and members see names, levels and sources; others see counts.
- No-op / rejection / concurrency / retry: [S1]; disclosure rule never leaks a hidden Space's roles; no `expired` kind (access has no end date, but see B05).
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1])
- Rules: product sections 103-106; WM-030; D78.12; WMREQ006-007
- Acceptance: Given a Commenter (not a member), when they open sharing, then only counts show. Given a manager, then log, privateImpact and grantedBy appear.
- Evidence: BE Boards.GetAccess = GET /boards/{boardId:guid}/access -> GetBoardAccessQuery (`Boards/Queries/GetBoardAccess/GetBoardAccess.cs`) [- / -]; Boards.GetSharing = GET /boards/{boardId:guid}/sharing -> GetBoardSharingQuery (`Boards/Queries/GetBoardSharing/GetBoardSharing.cs`) [- / -] · FE BoardShareDialogSurface, useBoardSharing · Design Final-Share; Er, L, E, T, conflict missing · Tests `BoardAccessSummaryIntegrationTests.cs`, `BoardSharingIntegrationTests.cs` · Real e2e `board-sharing.real.e2e.spec.ts`
- State: HARDENING — Direct GetSharing tests; design Er/L/E

### UC-WM-A17 Add, change or remove a board member
- Capability: A Board lifecycle (WM-001)
- Actors and permission: Manager (Board management); assigns/changes roles only up to their own level; below Manager nobody; Guest never.
- Preconditions: Board is active; actor is a Manager; the target is a Workspace participant.
- Input: User id, board role (Manager, Editor, Commenter, Viewer).
- Output: Board member row; effective level = highest of sources, Guest capped at Editor; recorded in `governance.resource_access_changes` (retained 90 days) and enterprise audit.
- Main flow: 1. Open Share. 2. Add a person or change a role, or remove the member. 3. The summary updates and the change is logged.
- No-op / rejection / concurrency / retry: person not a Workspace participant = rejected; Observer cannot update items; [S2]; target's level is higher of board role and explicit grant.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 103-105; WM-030; WMREQ007, WMREQ105-108
- Acceptance: Given a Manager adds a person as Member, when that person opens the board, then they can create items. Given an Admin-level actor, when they try to assign a role above their own, then it is refused.
- Evidence: BE Boards.GetMembers = GET /boards/{boardId:guid}/members -> GetBoardMembersQuery (`Boards/Queries/GetBoardMembers/GetBoardMembers.cs`) [- / -]; Boards.AddMember = POST /boards/{boardId:guid}/members -> AddBoardMemberCommand (`Boards/Commands/AddBoardMember/AddBoardMember.cs`) [- / Idem]; Boards.RemoveMember = DELETE /boards/{boardId:guid}/members/{userId:guid} -> RemoveBoardMemberCommand (`Boards/Commands/RemoveBoardMember/RemoveBoardMember.cs`) [- / Idem]; Boards.UpdateMemberRole = PATCH /boards/{boardId:guid}/members/{userId:guid}/role -> UpdateBoardMemberRoleCommand (`Boards/Commands/UpdateBoardMemberRole/UpdateBoardMemberRole.cs`) [- / Idem] · FE BoardShareDialogSurface; AddMember op unused · Design Final-Share, Final-Members · Tests `AddBoardMemberTests.cs`, `BoardAuthorizationHardeningIntegrationTests.cs`, `NotrelixApiFactory.cs` · Real e2e `board-sharing.real.e2e.spec.ts`
- State: HARDENING — tests and owner check. Built: role change and removal carry the board version (the sharing read returns it; a member add, role change or removal advances it), a stale write changes nothing and the dialog says so; the last manager cannot be lowered or removed (UC-WM-A26).

### UC-WM-A18 Grant explicit access to a user or team
- Capability: A Board lifecycle (WM-001)
- Actors and permission: ManageBoardPermission (Manager); requested level may not exceed grantor's own.
- Preconditions: Board is active; actor has ManageBoardPermission.
- Input: User or team id, level up to the grantor's own level.
- Output: resource permission, logged; access takes effect on next decision; realtime/cache converge.
- Main flow: 1. Open Share. 2. Pick a person or team and a level. 3. Submit; access takes effect on the next decision.
- No-op / rejection / concurrency / retry: Guest cannot hold Manager effect (GOV-030) and UI shows Manager option disabled with reason; [S2].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 103-106; WM-030; WMREQ006-007, WMREQ106
- Acceptance: Given a Guest granted Manager, then effective level is Editor and no management control is shown.
- Evidence: BE Governance.ResourcePermissions Grant/Revoke (Governance-owned) · FE `frontend/apps/web/src/shell/workspace/board-share-dialog.tsx` · Design Final-Share (levels); Guest-Manager disabled reason not drawn · Tests none found · Real e2e `guest-permission.real.e2e.spec.ts`, `board-sharing.real.e2e.spec.ts`
- State: HARDENING — Expiry input removed (D76.4); Guest capped at Editor proof

### UC-WM-A19 Invite an external guest to one board
- Capability: A Board lifecycle (WM-001)
- Actors and permission: ManageBoardPermission on the board.
- Preconditions: Board is active; actor has ManageBoardPermission; the invitee is not yet a member.
- Input: Email, level up to Editor.
- Output: Pending Workspaces resource invitation naming the board and inviter; its UI is the board's Share dialog (D78.12).
- Main flow: 1. Open Share, Invite guest. 2. Enter email and level. 3. A resource invitation email is sent. 4. On accept the person becomes a Guest with a grant on this board only.
- No-op / rejection / concurrency / retry: invitation above Editor rejected; guest/member is a membership fact, never inferred from email domain; Admin cannot be invited.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 105; WM-030; D78.12
- Acceptance: Given an invitation to a board accepted by an outsider, then they reach only that board and `GET members` returns 403 while `members/me` returns only their own row.
- Evidence: BE none: no WM invite endpoint (Workspaces/Governance own invitations) · FE none · Design none; needs invite guest to one board, pending/accepted/revoked states · Tests none found · Real e2e none
- State: DEFINED — no Work Management call to the Workspaces resource-invitation API from the Share dialog; no UI.

### UC-WM-A20 Resend or revoke a resource invitation
- Capability: A Board lifecycle (WM-001)
- Actors and permission: issuer who still has ManageBoardPermission; Workspace Owner/Admin any.
- Preconditions: A pending invitation exists for this board; the actor still has ManageBoardPermission, or is Workspace Owner or Admin.
- Input: Invitation id.
- Output: resend replaces token (old stops working); revoke makes it unacceptable.
- Main flow: 1. Open Share, pending invitations. 2. Resend or revoke. 3. Resend replaces the token; revoke makes it unacceptable.
- No-op / rejection / concurrency / retry: only invitations for that exact resource; an issuer who lost permission is denied.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 105; D78.12
- Acceptance: Given a revoked invitation, when the link is opened, then acceptance fails.
- Evidence: BE none · FE none · Design none; needs invitation list, resend, revoke · Tests none found · Real e2e none
- State: DEFINED — Depends on UC-WM-A19; design; BE; FE

### UC-WM-A21 Change board visibility (Workspace or Private)
- Capability: A Board lifecycle (WM-001)
- Actors and permission: Manager (ManageBoard).
- Preconditions: Board is active; actor is a Manager.
- Input: Visibility (Workspace or Private).
- Output: Private requires board membership; Workspace-visible inherits Space access; visibility change reported to Governance sharing log.
- Main flow: 1. Open Settings, Access. 2. Pick a visibility. 3. If access narrows, review the assignee warning (UC-WM-A25). 4. Confirm.
- No-op / rejection / concurrency / retry: Choosing the current visibility is a no-op [S5]. Below Manager = 403 [S2]. Stale version = conflict [S4]. PublicLink is not offered (D76.4).
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline; destructive confirm (names the impact)
- Rules: product sections 103-106; WM-030, WM-040; D76.4; WMREQ006
- Acceptance: Given a board made Private, when a non-member Workspace Member opens it, then NotFound.
- Evidence: BE Boards.UpdateVisibility = PATCH /boards/{boardId:guid}/visibility -> UpdateBoardVisibilityCommand (`Boards/Commands/UpdateBoardVisibility/UpdateBoardVisibility.cs`) [- / Idem] · FE contract op unused; header offers Make private only · Design Final-Share (private) · Tests `BoardAuthorizationHardeningIntegrationTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; settings conflict notice names no actor and has no keep-mine (artboard Er-on-save needed); tests and owner check. Built: PublicLink is refused on create and on every visibility change (rule code `WorkManagement_Board_PublicLinkNotSupported`); the visibility write carries the board version. Not built: affected-assignee warning (UC-WM-A25), UI for Workspace visibility. Evidence: real-stack check 2026-10-31: switched a board to Private from the Share dialog and a member now gets the not-available state after reload; an unknown visibility value is now 400 (it answered 204 and stored it); the same value bumps nothing; a stale version 412, commit 33ed8c465.

### UC-WM-A22 Make a board private with impact preview and bulk add
- Capability: A Board lifecycle (WM-001)
- Actors and permission: Manager (`ManageBoard`), endpoint idempotent.
- Preconditions: Board is Workspace-visible; actor is a Manager.
- Input: People to add as board members.
- Output: `privateImpact` = people who reach the board only through its Space with their unfinished and overdue Items (unfinished = not completed, not archived, not deleted; overdue = due instant passed). `POST /boards/{id}/sharing/private` adds the given people as Board members who can edit (existing keep their role) and sets Private in one transaction.
- Main flow: 1. Choose Make private. 2. See the impact preview of people who reach the board only through the Space. 3. Pick whom to add as members. 4. Confirm.
- No-op / rejection / concurrency / retry: Anyone who is not a Workspace participant is rejected and nothing changes. Retry with the same key has no second effect [S7].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline; destructive confirm (names the impact)
- Rules: product sections 103-106; WM-030, WM-040; WMREQ006-007
- Acceptance: Given 3 Space-only people with open Items, when "Make private" lists them, then confirming with 2 selected adds those 2 as editors and the third loses access, all in one transaction.
- Evidence: BE Boards.MakePrivate = POST /boards/{boardId:guid}/sharing/private -> MakeBoardPrivateCommand (`Boards/Commands/MakeBoardPrivate/MakeBoardPrivate.cs`) [- / Idem] · FE BoardShareDialogSurface · Design Final-Share (private flow) · Tests `BoardSharingIntegrationTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; tests and owner check. Evidence: real-stack check 2026-10-31: made a board Private from the Share dialog with the impact count (204) and the reload keeps it; the people added keep access; a Guest cannot open it, commit 33ed8c465.

### UC-WM-A23 Open a shared board as a Guest
- Capability: A Board lifecycle (WM-001)
- Actors and permission: Guest with explicit access.
- Preconditions: A Guest holds an explicit grant on the board.
- Input: Board URL.
- Output: sees only the granted board; levels Viewer/Commenter/Editor; names of people on that board come from the board's own reads; never workspace enumeration (§105, GOV-009).
- Main flow: 1. The Guest signs in and opens the board. 2. Controls are capped at the granted level. 3. Pickers list only people on this board, without emails.
- No-op / rejection / concurrency / retry: pickers list board members only, no emails; conflict/deleted notices say "someone" for non-board people.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1]); validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 105; WM-030; Q7
- Acceptance: (1) Given a Guest, when a conflict or "deleted by" notice would name someone who is not on the board, then it says "someone" and shows no name or email (owner Q7). (2) Given a Guest, when they open an assignee, person-field or mention picker, then only people on that board are listed and no workspace directory data or email shows (owner Q7). (3) Given a Guest, when they open the assignee, Person-field, mention or filter-by-person pickers, then only board members appear, no email shows, and a request for another workspace member returns nothing (owner Q7).
- Evidence: BE Governance + board reads (Q7 pickers: board members only) · FE guest-capped controls; picker rule unproven · Design none; needs Guest board view (capped controls, board-members-only pickers) · Tests none found · Real e2e `guest-permission.real.e2e.spec.ts` (negative only)
- State: INTEGRATING — design for the Guest board view; proof that pickers show no emails (Q7); positive guest journey e2e.

### UC-WM-A24 Copy item/board link
- Capability: A Board lifecycle (WM-001)
- Actors and permission: any viewer.
- Preconditions: Viewer or above.
- Input: None.
- Output: URL locator; opens resource only for principals who already have access.
- Main flow: 1. Open the item or board action menu. 2. Choose Copy link. 3. The URL goes to the clipboard and a toast confirms.
- No-op / rejection / concurrency / retry: The link opens the resource only for people who already have access; others get NotFound [S1].
- UI states: success (toast: link copied), pending none, error (clipboard blocked)
- Rules: product sections 106; WM-030
- Acceptance: Given a copied link opened by a non-member, then [S1].
- Evidence: BE not applicable (client URL) · FE copy link in item action menu · Design Final-M-Share `da-sao-chep` · Tests none found · Real e2e none
- State: INTEGRATING — non-member gets NotFound e2e

### UC-WM-A25 Warn when an access change would strand assignees
- Capability: A Board lifecycle (WM-001)
- Actors and permission: the person changing access (Manager).
- Preconditions: A Manager is changing access, visibility or Space.
- Input: The pending access change.
- Output: warning listing open Items assigned to people who will lose access, with offer to grant access first (WM-040).
- Main flow: 1. The server counts open items assigned to people who would lose access. 2. The dialog lists them. 3. The Manager grants access first, or continues.
- No-op / rejection / concurrency / retry: Losing access later never unassigns anyone. It fires on make private, visibility change and Space move (D78.11).
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 68.2, 103; WM-040; D78.11
- Acceptance: Given Private visibility is chosen and 4 assignees lack access, then the warning names them with a "grant access" action.
- Evidence: BE privateImpact on MakePrivate only; none on Visibility/MoveToSpace · FE list inside make-private only · Design none; needs assignee-impact warning with Grant access action · Tests none found · Real e2e none
- State: INTEGRATING — Warning on visibility change and move-to-space; design

### UC-WM-A26 Block removing or demoting the last board manager
- Capability: A Board lifecycle (WM-001)
- Actors and permission: Manager.
- Preconditions: Board has one or more Managers.
- Input: Remove or demote a member.
- Output: Refused with a reason; membership unchanged.
- Main flow: 1. A Manager tries to remove or demote the last Manager. 2. The server refuses with a reason. 3. Membership is unchanged.
- No-op / rejection / concurrency / retry: A Workspace Owner or Admin can still manage through Governance. No partial change.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 103; WM-030; D78.9; WMREQ007
- Acceptance: Given one Manager, when that person is demoted or removed, then it is refused and the role is unchanged.
- Evidence: BE domain rule unverified on RemoveMember/UpdateMemberRole · FE none · Design none; needs last-manager blocked message · Tests none found · Real e2e none
- State: DEFINED — tests and owner check. Built: `BoardManagerGuard` refuses lowering or removing the last person or Team at Manager level on the board (rule code `WorkManagement_Board_LastManager`, nothing changes); the dialog toast shows the server's reason. Not built: design for the message, disabling the control up front.

## B. BoardItem lifecycle

Team capability WM-002.

### UC-WM-B01 Create an item (full dialog)
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: Editor and above (`CreateItem`); Owner/Manager/Member; Commenter/Viewer cannot (no button, no shortcut C); Guest only when granted Editor.
- Preconditions: Board active; actor may create items.
- Input: Name (required, 1-500 chars), description (up to 20,000), start and due, plus follow-up writes for status, priority, people, labels and custom values.
- Output: item with key (UC-WM-A10), appended to the bottom of its group, status of the lane; own facts (name, description, start, due) in one create write; status, priority, lane value, people, labels, custom values as follow-up writes with custom values in one batch (WM-044); ItemCreated fact; created item highlighted and scrolled into view; toast with Open and Hoàn tác; peers refetch.
- Main flow: 1. Press Add item. 2. Fill the dialog. 3. Submit. 4. The item appears at the bottom of its group.
- No-op / rejection / concurrency / retry: empty name inline error; 500/20,000 limits; invalid date/number/URL; idempotent by key [S7]; server rejection keeps inputs; partial failure names missing parts with "Thử lại phần còn thiếu"; dirty-close confirm with "Tiếp tục sửa" focused, no drafts across reload; status/group/option deleted meanwhile blocks submit, no silent substitution; CreateItem lost = 403 with reason, input kept; archived board disables submit; one create path for dialog, quick add, form response, Calendar/Timeline add; assignee must see board (rule 8, WM-040).
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 27.3, 28, 29; WM-011, WM-043, WM-044; D28, D30, Q2; WMREQ011-017, WMREQ020
- Acceptance: (1) Given a Commenter or Viewer, when they look for "Add item", then no button, no quick add and no shortcut C exist. (2) Given the person edits any input after a failed attempt, when they submit again, then a new idempotency key is used. (3) Given an empty name, when submitting, then an inline error appears under the name, focus goes to it, and no request is sent.
- Evidence: BE BoardItems.Create = POST /boards/{boardId:guid}/items -> CreateBoardItemCommand (`BoardItems/Commands/CreateBoardItem/CreateBoardItem.cs`) [- / Idem]; BoardItems.AssignMember = POST /board-items/{itemId:guid}/assignees -> AssignBoardItemMemberCommand (`BoardItems/Commands/AssignBoardItemMember/AssignBoardItemMember.cs`) [- / Idem]; BoardItems.AddLabel = POST /board-items/{itemId:guid}/labels -> AddLabelToBoardItemCommand (`Labels/Commands/AddLabelToBoardItem/AddLabelToBoardItem.cs`) [- / Idem]; BoardItems.UpdateFieldValues = PATCH /board-items/{itemId:guid}/field-values -> UpdateBoardItemFieldValuesCommand (`BoardItems/Commands/UpdateBoardItemFieldValues/UpdateBoardItemFieldValues.cs`) [EV / Idem] · FE card popup create mode (`card-popup/create/*`), `frontend/packages/product/work-management/state/src/mutations/use-create-item-with-details.ts` · Design Kanban-CardPopup-Create, -Create-States, -M-Create · Tests `AddLabelToBoardItemTests.cs`, `AssignBoardItemMemberTests.cs`, `CreateBoardItemTests.cs`, `ItemStatusAndSystemFieldWritesTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; the full-dialog partial-failure proof. Evidence: real-stack check 2026-10-31: created through the API and the quick add in the browser; empty and 501-char title, 20,001-char description, inverted dates and a foreign group refused (400/404); a retry under one key made one item; a member created one; a non-member of a Private board got 404; reload kept the item, commit f812cc4ff.

### UC-WM-B02 Quick-add an item in a group or lane
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: Editor and above; not drawn for Commenter/Viewer or read-only board.
- Preconditions: Board active; actor may create items; a group or lane is in view.
- Input: Item name.
- Output: item appended to bottom of the group even when outside window; scrolls into view; WIP limit reached = item still created with warning (D37).
- Main flow: 1. Click Add to the group. 2. Type the name. 3. Press Enter; keep typing to add more.
- No-op / rejection / concurrency / retry: empty name does nothing; failure keeps typed name, inline error, "Thử lại"; [S7] for retry.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 27.3; WM-044; D30, D39; WMREQ016-017
- Acceptance: (1) Given a quick add failing, when the write errors, then the typed name stays in the input with an inline error and "Thử lại". (2) Given a quick add or dialog create in a group that is scrolled away from its bottom, when it succeeds, then the new item is brought into view. (3) Given a quick add in a lane, when Enter is pressed with a name, then the item is appended to the bottom of that lane and the input stays focused for the next; with an empty name nothing happens (D39).
- Evidence: BE BoardItems.Create = POST /boards/{boardId:guid}/items -> CreateBoardItemCommand (`BoardItems/Commands/CreateBoardItem/CreateBoardItem.cs`) [- / Idem] · FE KanbanAddCard, table row add; WIP warning not found · Design Kanban-Card-States, BC-Kanban (quick add) · Tests `CreateBoardItemTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; WIP warning on over-limit add. Evidence: real-stack check 2026-10-31: quick add in the Kanban 'No status' lane in the browser created the item and it stayed after reload, commit f812cc4ff.

### UC-WM-B03 Create a sub-item
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: Editor and above.
- Preconditions: Parent item exists, is not itself a sub-item, and is active.
- Input: Sub-item name; the parent is fixed.
- Output: child Item (same board key sequence), listed under parent in Table sub-rows and sheet, absent from the group list as a top-level item; parent's derived progress updates (UC-WM-D09).
- Main flow: 1. Open the parent. 2. Add a sub-item in the Sub-items section. 3. It lists under the parent.
- No-op / rejection / concurrency / retry: sub-item cannot be chosen as a parent; archive of parent takes sub-items; delete takes sub-items to Trash together; [S2],[S3].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 26, 27.1, 77.1; WM-041, WM-043; Q3; WMREQ012-013, WMREQ016
- Acceptance: (1) Given the dialog opened from the item sheet's Sub-items "Add with details", when it opens, then the parent is fixed and shown, the Group is the parent's and not editable, and the created item is a sub-item (owner Q3). (2) Given a Member creates a sub-item from the sheet's Sub-items "Add with details" (owner Q3), when it is created, then its parent is the chosen item, one level only (a sub-item cannot be chosen as a parent), and it appears under the parent in Table sub-rows and in the sheet.
- Evidence: BE BoardItems.CreateChild = POST /board-items/{itemId:guid}/child-items -> CreateChildItemCommand (`BoardItems/Commands/CreateChildItem/CreateChildItem.cs`) [- / Idem]; BoardItems.ListChildren = GET /board-items/{itemId:guid}/child-items -> ListChildItemsQuery (`BoardItems/Queries/ListChildItems/ListChildItems.cs`) [- / -] · FE `frontend/packages/product/work-management/web/src/components/views/table/table-sub-items.tsx`, ItemPageChildItems; raw /child-items path · Design BC-Create sub-item mode; sheet add/reorder not drawn · Tests `SubItemTests.cs` · Real e2e `sub-items.real.e2e.spec.ts`
- State: HARDENING — real e2e spec, tests, design measurement and owner check; the sheet add and reorder design. Evidence: real-stack check 2026-10-31: child created through the API, a child of a child refused (400), the children list returned it, archive and delete of the parent took it along and restore brought it back, commit f812cc4ff.

### UC-WM-B04 Edit the item title in place
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: Editor and above.
- Preconditions: Item is active; actor may update items.
- Input: Title 1-500 chars after trim; expected version.
- Output: Title committed; single click still opens the item (D63).
- Main flow: 1. Double-click, press F2 or Enter on the title cell. 2. Edit. 3. Enter saves, Escape cancels, blur saves.
- No-op / rejection / concurrency / retry: Same title = no-op [S5]. Stale version = conflict [S4].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 28; D63; WMREQ020, WMREQ110-113; LD1
- Acceptance: Given an item title edited in the Table, when Enter is pressed, then the new title shows for everyone. Given Escape, then the old title stays. Given a stale version, then a conflict choice appears.
- Evidence: BE BoardItems.Update = PATCH /board-items/{itemId:guid} -> UpdateBoardItemCommand (`BoardItems/Commands/UpdateBoardItem/UpdateBoardItem.cs`) [EV / -] · FE table inline title (/Enter/double-click) · Design Final-Table `sua-o` · Tests `BoardAuthorizationHardeningIntegrationTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Evidence: real-stack check 2026-10-31: rename with the expected version bumped the version once, the same title changed nothing, a stale version answered 412 and kept the title, a blank title was refused, commit f812cc4ff.

### UC-WM-B05 Assign people to an item
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: Editor and above (`assign`); Commenter/Viewer disabled with reason.
- Preconditions: Item active; the person to assign is a Workspace participant who can view the board.
- Input: Person ids (set), expected version.
- Output: assignees set; assignment rejected for anyone who cannot ViewBoard on every write path (UI, API, Automation, Import) with message "share the board first" and an invite action for principals who may grant access (WM-040); losing access later does not unassign; assignment can trigger Collaboration auto-watch and notification.
- Main flow: 1. Open the assignee picker. 2. Pick people. 3. Save; each becomes a watcher (D78.1).
- No-op / rejection / concurrency / retry: Anyone who cannot view the board is refused with 'share the board first' (D41); no auto-share. Same set = no-op [S5]. Stale version = conflict [S4].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 68.2, 139; WM-040; D41; D78.1; WMREQ022, WMREQ110-113
- Acceptance: (Guest picker), Given a user without ViewBoard, when assigned via API, then rejected with the rule and nothing changes. Given an assignee loses access later, then the item still shows them as assignee.
- Evidence: BE BoardItems.AssignMember = POST /board-items/{itemId:guid}/assignees -> AssignBoardItemMemberCommand (`BoardItems/Commands/AssignBoardItemMember/AssignBoardItemMember.cs`) [- / Idem]; BoardItems.UnassignMember = DELETE /board-items/{itemId:guid}/assignees/{userId:guid} -> UnassignBoardItemMemberCommand (`BoardItems/Commands/UnassignBoardItemMember/UnassignBoardItemMember.cs`) [- / Idem] · FE ItemAssigneePicker; 'share the board first' with invite action unproven · Design WM-Sheet-Full, BC-Create-320-Pickers · Tests `AssignBoardItemMemberTests.cs`, `NotrelixApiFactory.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Evidence: real-stack check 2026-10-31: assign and unassign carry the expected version (stale 412, nothing changed), an assignee who cannot see a Private board got 403 with the share-first message, assigning twice changed nothing, non-member 404, commit f812cc4ff.

### UC-WM-B06 Complete, reopen, remind
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: Editor and above; Commenter/Viewer disabled with reason.
- Preconditions: Item active, or a done item to reopen.
- Input: Item id, expected version.
- Output: Nhắc sends each assignee an Inbox notification naming the sender; at most once per item per sender per 24 h; never to self; needs view access (D35, D77.2). No personal reminder is built.
- Main flow: 1. Use Complete, Reopen or Remind from the item menu. 2. Complete picks a done-category status; Reopen clears completion. 3. Remind (Nhắc) notifies each assignee.
- No-op / rejection / concurrency / retry: Archived item: Complete is disabled 'unarchive first'. No done-category status = refusal naming it. Stale version = conflict [S4]. A second Nhắc within 24 h explains when it is possible again.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 148.1; WM-042; D35, D77.2, D77.3; WMREQ014, WMREQ110-113
- Acceptance: Given an open item, when Complete is used, then it moves to a done-category status. Given Nhắc pressed twice in an hour, then the second is refused with the time it is possible again.
- Evidence: BE BoardItems.Complete = POST /board-items/{itemId:guid}/complete -> CompleteBoardItemCommand (`BoardItems/Commands/CompleteBoardItem/CompleteBoardItem.cs`) [- / Idem]; BoardItems.Reopen = POST /board-items/{itemId:guid}/reopen -> ReopenBoardItemCommand (`BoardItems/Commands/ReopenBoardItem/ReopenBoardItem.cs`) [- / Idem]; BoardItems.RemindOwners = POST /board-items/{itemId:guid}/remind (the Nhắc nudge, D35) · FE `frontend/packages/product/work-management/web/src/components/item-page/item-page-states.tsx`; `frontend/packages/product/work-management/web/src/components/item-actions/remind-dialog.tsx` raw /remind (nudge) · Design WM-Actions-Menu, WM-Actions-Remind; Er, M missing · Tests `ReopenBoardItemTests.cs` · Real e2e `work-board-management.real.e2e.spec.ts` (reopen)
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Evidence: real-stack check 2026-10-31: complete, reopen in the browser (POST complete 204, reopen 204 with the version) and through the API; a second complete or reopen changed nothing, a stale version answered 412, complete on an archived item was refused, commit f812cc4ff.

### UC-WM-B07 Duplicate an item
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: Editor and above (`CreateItem` on the board); Commenter/Viewer disabled.
- Preconditions: Source item active; actor has CreateItem on the board.
- Input: Item id; options for sub-items (default on), fields, labels and checklists.
- Output: Copies title, description, field values, labels, checklists and optionally sub-items; never comments, activity, approvals, links or attachments (D76.7).
- Main flow: 1. Open the item menu, Duplicate. 2. Choose options. 3. A new item appears next to the source with a new key.
- No-op / rejection / concurrency / retry: Refused with the reason when the item is archived. Retry with the same key creates one copy [S7].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 27, 28; WM-043; D76.7; WMREQ049
- Acceptance: Given a duplicate, then the new item has a new key and the original is unchanged. Given Duplicate with sub-items on (default), then the copy has title, description, field values, labels, checklist and sub-items, and no comments, activity, approvals, links or attachment files.
- Evidence: BE BoardItems.Duplicate = POST /board-items/{itemId:guid}/duplicate -> DuplicateBoardItemCommand (`BoardItems/Commands/DuplicateBoardItem/DuplicateBoardItem.cs`) [- / Idem] · FE `frontend/packages/product/work-management/web/src/components/item-actions/duplicate-item-dialog.tsx` · Design WM-Actions-Duplicate; Er, busy missing · Tests `CreateBoardItemTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Evidence: real-stack check 2026-10-31: duplicate with sub-items copied the item and its sub-item, a retry under one key made one copy, an archived item was refused (400), commit f812cc4ff.

### UC-WM-B08 Move an item to another board
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: UpdateItem on source and CreateItem on target (D50); Guest sees only target boards where they can create.
- Preconditions: Item active without dependencies; actor has UpdateItem on the source and CreateItem on the target.
- Input: Item id, target board, expected version.
- Output: Status maps by name, else the target's first status. Field values carry over to same-name same-type fields; others are dropped and named in a notice. Comments, attachments, sub-items and history move with the item (D50, D54).
- Main flow: 1. Open the item menu, Move to board. 2. Pick the board and review the mapping. 3. Confirm.
- No-op / rejection / concurrency / retry: Archived item or item with dependencies is refused with the reason. Assignees who cannot view the target are dropped and counted. Stale version = conflict [S4].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline; destructive confirm (names the impact)
- Rules: product sections 26, 27.1; WM-043, WM-040; D50, D54, D76.3; WMREQ018-019, WMREQ110-113
- Acceptance: (1) Given a bulk Move, when the target lacks CreateItem for the person, then Move is disabled with the reason (D50). (2) Given Move board with a target where the person lacks CreateItem or the item has dependencies, when attempted, then the refusal names the reason (D50/D54).
- Evidence: BE BoardItems.MoveToBoard = POST /board-items/{itemId:guid}/move-to-board -> MoveBoardItemToBoardCommand (`BoardItems/Commands/MoveBoardItemToBoard/MoveBoardItemToBoard.cs`) [- / Idem] · FE `frontend/packages/product/work-management/web/src/components/item-actions/move-board-dialog.tsx` (raw move-to-board) · Design WM-Actions-Move (mapping preview); Er/partial missing · Tests `MoveBoardItemToBoardTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; mapping-notice proof. Evidence: real-stack check 2026-10-31: move to another board with the expected version (stale 412), the same board and an archived item refused (400), links followed the item, commit f812cc4ff.

### UC-WM-B09 Archive an item
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: Editor and above.
- Preconditions: Item active.
- Input: Item id, expected version.
- Output: item leaves group lists, takes its sub-items with it; read-only; confirm names sub-items and dependency counts (pattern).
- Main flow: 1. Open the item menu, Archive. 2. Confirm; the dialog names sub-items and dependencies. 3. The item leaves group lists with its sub-items.
- No-op / rejection / concurrency / retry: Archived items refuse edits. Already archived = no-op [S5]. Stale version = conflict [S4].
- UI states: destructive confirm (names the impact)
- Rules: product sections 26, 123; WM-033; WMREQ014
- Acceptance: Given an item with 2 sub-items, when archived, then all three leave the lists. Given an archived item, when a description edit is sent, then it is refused with the reason.
- Evidence: BE BoardItems.Archive = POST /board-items/{itemId:guid}/archive -> ArchiveBoardItemCommand (`BoardItems/Commands/ArchiveBoardItem/ArchiveBoardItem.cs`) [EV / Idem] · FE useArchiveCard, `ArchiveItemDialog` (`item-lifecycle/archive-item-dialog.tsx`), ItemPageArchivedBanner · Design BC-Actions (confirm archive) · Tests `ArchiveBoardItemTests.cs`, `WorkMutationTransportRegressionTests.cs`, `archive-item-dialog` component tests, `card-commands.unit.test.ts` · Real e2e `item-lifecycle.real.e2e.spec.ts`, `sub-items.real.e2e.spec.ts`
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Evidence: real-stack check 2026-10-31: archive from the card popup in the browser removed the item from the board and kept it after reload; archiving twice changed nothing, an archived item refused edits, its sub-items were archived with it, commit f812cc4ff.

### UC-WM-B10 Browse archived items and unarchive
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: Editor and above (needs UpdateItem, D54); Viewer/Commenter do not see the entry; Guest as granted.
- Preconditions: Actor has UpdateItem on the board.
- Input: Page cursor, optional search.
- Output: Paged list with name, key, who archived and when (Q6).
- Main flow: 1. Open Archived items. 2. Search by name or key. 3. Choose Unarchive; the item and its archived sub-items return to their group.
- No-op / rejection / concurrency / retry: Viewer and Commenter do not see the entry [S2]. Unarchiving an active item is a no-op [S5]. Stale version = conflict [S4].
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1]); validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 123; WM-033; D51; Q6; WMREQ014; LD2
- Acceptance: (1) Given a board's Archived items panel, when a Member opens it, then archived items are listed with search, each with Unarchive; a Viewer does not see the entry (owner Q6). (2) Given the Archived items panel (owner Q6), when a Member opens it, then archived items list with name, key, who archived and when, paged, each with Unarchive; unarchive returns the item and the sub-items archived with it to its group.
- Evidence: BE BoardItems.Unarchive = POST /board-items/{itemId:guid}/unarchive -> UnarchiveBoardItemCommand (`BoardItems/Commands/UnarchiveBoardItem/UnarchiveBoardItem.cs`) [- / Idem] · FE none; no archive browse; unarchive via raw path · Design BC-Archive-Trash (archived list) · Tests `SubItemTests.cs` · Real e2e `item-lifecycle.real.e2e.spec.ts` (unarchive on item page only)
- State: HARDENING — real e2e spec, tests, design measurement and owner check; design measurement against BC-Archive-Trash (the sheet reuses the Deleted items sheet). Evidence: real-stack check 2026-10-31: Archived items sheet in the board menu listed the archived item, Unarchive put it back and it stayed after reload; the read answers 404 to a non-member; search by name works, commit f812cc4ff.

### UC-WM-B11 Delete an item (to Trash)
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: Editor and above (`delete Items`).
- Preconditions: Item active or archived; actor has UpdateItem.
- Input: Item id, expected version.
- Output: Soft delete with a 30-day window; sub-items go with the parent (Q10, D51).
- Main flow: 1. Open the item menu, Delete. 2. Confirm; the dialog names sub-item and dependency counts. 3. The item goes to Trash and a toast offers 'Hoàn tác'.
- No-op / rejection / concurrency / retry: Double delete = no-op [S5]. Stale version = conflict [S4].
- UI states: destructive confirm (names the impact)
- Rules: product sections 123; WM-033; D51; Q10; WMREQ014
- Acceptance: (1) Given the item was deleted by someone else while the menu was open, when an entry is chosen, then the person is told it was deleted and the row/card is gone. (2) Given the item is deleted while the sheet is open, when the change arrives, then the sheet shows "this item was deleted" with Restore for people who may and a link back to the board. (3) Given Delete, when chosen, then a confirmation names the item and the counts of sub-items and dependencies; confirming soft-deletes it; a toast offers Hoàn tác (restore); the item is listed in Trash for 30 days (D51).
- Evidence: BE BoardItems.Delete = DELETE /board-items/{itemId:guid} -> DeleteBoardItemCommand (`BoardItems/Commands/DeleteBoardItem/DeleteBoardItem.cs`) [- / Idem] · FE `frontend/packages/product/work-management/web/src/components/item-lifecycle/use-item-removal.tsx`, undo toast · Design WM-Actions-Trash, BC-Archive-Trash · Tests `BulkUpdateBoardItemsTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Evidence: real-stack check 2026-10-31: delete from the card popup in the browser asked for confirmation, the item went to Deleted items; a stale version answered 412; the sub-items went with the parent, commit f812cc4ff.

### UC-WM-B12 Restore an item from Trash
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: Editor and above (UpdateItem, D54).
- Preconditions: Item is in Trash inside the 30-day window; actor has UpdateItem.
- Input: Item id.
- Output: item and its sub-items return together to their group; after 30 days refused with reason; restore of a sub-item whose parent is still deleted is refused.
- Main flow: 1. Open the board Trash. 2. Choose Restore. 3. The item and its sub-items return to their group.
- No-op / rejection / concurrency / retry: After 30 days restore is refused with the reason. The restore guard checks the group still exists.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 123; WM-033; D51, D54, Q10; WMREQ014
- Acceptance: Given an item deleted yesterday, when restored, then it returns with its sub-items. Given one deleted 31 days ago, then restore is refused.
- Evidence: BE BoardItems.Restore = POST /board-items/{itemId:guid}/restore -> RestoreBoardItemCommand (`BoardItems/Commands/RestoreBoardItem/RestoreBoardItem.cs`) [- / Idem] · FE `frontend/packages/product/work-management/web/src/components/item-lifecycle/board-trash-sheet.tsx`, `frontend/packages/product/work-management/state/src/cache/card-restore.ts` · Design BC-Archive-Trash (restore, >30 days, parent deleted) · Tests `WorkItemPlacementIntegrationTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Evidence: real-stack check 2026-10-31: restore from the Deleted items sheet in the browser put the item back and it stayed after reload; an item past 30 days is not listed and its restore answers 409, commit f812cc4ff.

### UC-WM-B13 Open the board Trash
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: Editor and above; Viewer/Commenter hidden.
- Preconditions: Actor has UpdateItem on the board.
- Input: Page cursor.
- Output: Viewer and Commenter do not see the entry. Paged and bounded.
- Main flow: 1. Open Trash from the board menu. 2. The list shows deleted items with deletion time and days left. 3. Restore from a row.
- No-op / rejection / concurrency / retry: Viewer and Commenter do not see the entry [S2]. Items past 30 days are not listed. The list is paged, never unbounded.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1])
- Rules: product sections 123; D51; D54; WMREQ147
- Acceptance: Given the board Trash, when a Member restores an item within 30 days, then it returns to its group; after 30 days restore is refused with the reason (D54).
- Evidence: BE BoardItems.Trash = GET /boards/{boardId:guid}/items/trash -> ListBoardItemTrashQuery (`BoardItems/Queries/ListBoardItemTrash/ListBoardItemTrash.cs`) [- / -] · FE BoardTrashHost, useBoardTrash (raw trash path) · Design WM-Actions-Trash, BC-Archive-Trash · Tests `MoveBoardItemToBoardTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Evidence: real-stack check 2026-10-31: Deleted items sheet listed the deleted root items (at most 200), none past 30 days, 404 to a non-member, commit f812cc4ff.

### UC-WM-B14 Bulk-edit items (Table and Kanban)
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: Editor and above; Viewer/Commenter get no checkboxes.
- Preconditions: Items selected on the board; actor may update them.
- Input: 1-200 item ids with each item's expected version and one operation (set status, set priority, set due, clear due, assign, unassign-all, archive, delete).
- Output: one request, all-or-nothing, toast with count and "Hoàn tác" where an inverse exists; selection clears; idempotent retry with the same key.
- Main flow: 1. Select items. 2. Pick an action in the bulk bar. 3. One request runs all-or-nothing; the toast shows the count and 'Hoàn tác' where an inverse exists.
- No-op / rejection / concurrency / retry: Over 200 is disabled with the cap named. One stale item rejects the whole batch and names the first failing item (D77.3). Same Idempotency-Key retries never double-apply [S7]. Viewer and Commenter get no checkboxes.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline; destructive confirm (names the impact)
- Rules: product sections 34, 132; WM-012; WMREQ025, WMREQ110-113; D32, D43, D77.3, Q8
- Acceptance: (1) Given a bulk Move, when the target lacks CreateItem for the person, then Move is disabled with the reason (D50). (2) Given a bulk network failure, when retried, then the retry carries the same idempotency key and does not apply twice. (3) Given 201 items selected, when the person tries an action, then it is disabled with the cap stated and the selection is not truncated.
- Evidence: BE BoardItems.BulkUpdate = POST /boards/{boardId:guid}/items/bulk -> BulkUpdateBoardItemsCommand (`BoardItems/Commands/BulkUpdateBoardItems/BulkUpdateBoardItems.cs`) [- / Idem] · FE `frontend/packages/product/work-management/web/src/components/views/table/table-bulk-bar.tsx`; Kanban has no multi-select; no bulk move-to-board · Design BC-Bulk (Final-Table bar stale) · Tests `BulkUpdateBoardItemsTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; Kanban multi-select is not built (needs the BC-Bulk artboard states). Evidence: real-stack check 2026-10-31: bulk set-due-date and assign through the API with a version per item: one stale item refused the whole batch with 412 and named it, nothing changed; a retry under one key replayed; an empty list got 400 and an item of another board 404, commit f812cc4ff.

### UC-WM-B15 Use the item action menu everywhere
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: any viewer (some entries disabled).
- Preconditions: Any viewer; some entries depend on role.
- Input: Item.
- Output: One ordered menu on every surface: copy link, open full page, complete or reopen, remind, duplicate, move board, archive or unarchive, delete.
- Main flow: 1. Open the menu from the card, row, right-click, long-press, sheet header or page. 2. Pick an entry.
- No-op / rejection / concurrency / retry: An unavailable entry is not drawn; an unauthorised one is drawn disabled with the reason. A failure toast names the item and offers 'Thử lại'.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 26, 123
- Acceptance: Given a Viewer, then write entries are disabled with the reason. Given an item deleted while the menu is open, then the person is told and the row leaves.
- Evidence: BE not applicable (per-action routes) · FE item-actions (19 tests) · Design WM-Actions-Menu, BC-Actions · Tests none found · Real e2e none
- State: INTEGRATING — real e2e; proof that an item deleted while the menu is open is announced.

### UC-WM-B16 See item face states (card and row)
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: any viewer.
- Preconditions: Any viewer.
- Input: Item read model.
- Output: Up to 2 labels and 2 avatars then '+N'; due chip marks today and overdue; comment count comes from the board items read (D39).
- Main flow: 1. The card or row renders key, title, labels, avatars, due chip, priority glyph and counts. 2. States (overdue, blocked, archived, deleted) show.
- No-op / rejection / concurrency / retry: Rendering only; no write. An archived item shows a muted read-only face; a deleted one is absent.
- UI states: loading (skeleton), success, overdue, blocked, archived (muted, read-only), deleted (row absent), denied (identical NotFound)
- Rules: product sections 27.2; WM-043; D39
- Acceptance: Given 5 labels and 4 assignees, then the card shows 2 and 2 with '+N'. Given an overdue due date, then the chip says so.
- Evidence: BE comment count in /full (GetFullBoard tests) · FE KanbanCard, table-row · Design WM-Card-Face(-Dem), WM-Card-Compact, Kanban-Card-States · Tests none found · Real e2e none
- State: INTEGRATING — Non-colour marks proof; 99+ cap

### UC-WM-B17 Purge items, boards and fields after their 30-day window
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: System job.
- Preconditions: Item, board or field has sat in trash or deleted state for over 30 days.
- Input: None (system job).
- Output: Hard removal; restore is refused afterwards; Collaboration and Analytics data follow their own retention (D78.10).
- Main flow: 1. A daily job selects expired records in each tenant context. 2. It permanently removes them. 3. It records an audit fact.
- No-op / rejection / concurrency / retry: Idempotent; a failed run resumes; never purges inside the window; tenant and RLS context set.
- UI states: none: system behaviour with no screen of its own
- Rules: product sections 121-124; WM-033; D76.1, D76.5, D78.10; WMREQ090-091
- Acceptance: Given an item trashed 31 days ago, when the job runs, then it is gone and restore is refused. Given one trashed 29 days ago, then it stays.
- Evidence: BE none: no purge job (items, boards D76.1, fields D76.5) · FE trash copy says 30 days · Design none; needs days-left, permanently-deleted states · Tests none found · Real e2e none
- State: DEFINED — no purge job for items, boards or fields; decided in D78.10; needs the audit fact and tests.

### UC-WM-B18 Open an item (side sheet or full page)
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: Viewer and above (ViewBoard); Guest per grant.
- Preconditions: Viewer or above; Guest per grant.
- Input: Item id (from a card, row, link or URL).
- Output: sheet beside the view (no sheet over sheet), page via "Open full page"; sections: Properties, Description, Checklists, Sub-items, Files, Links/Docs, Approval, Comments | History; empty sections collapse to "+ Add"; Escape returns focus to origin; 320 px full-screen.
- Main flow: 1. Click the item. 2. The side sheet opens beside the view; 'Open full page' opens the page. 3. Escape returns focus to the opener.
- No-op / rejection / concurrency / retry: Level drops to Viewer while open: editors close and a banner explains. Item deleted: 'deleted by <name>' with Restore for those who may. Archived: read-only with Unarchive.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1]); validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 26-28; WM-044; D69; WMREQ011, WMREQ111
- Acceptance: Given a Member who clicks an item, then the sheet shows Properties, Description, Checklists, Sub-items, Files, Links, Approval and Comments or History. Given an empty section, then it collapses to '+ Add'. Given an item deleted by someone else while open, then the sheet says so and offers Restore only to those who may.
- Evidence: BE BoardItems.Get = GET /board-items/{itemId:guid} -> GetBoardItemQuery (`BoardItems/Queries/GetBoardItem/GetBoardItem.cs`) [- / -] · FE card popup (`card-popup/*`: CardPopupSurface, KanbanSelectedItem over Kanban, Table, Calendar, Timeline and My work; the item link redirects to the board with the popup open) · Design Kanban-CreateTask and Kanban-CardPopup-* (incl. -States-1/-2, -M-Card, -M-States), BC-Notices · Tests `BoardAuthorizationHardeningIntegrationTests.cs` · Real e2e `item-page.real.e2e.spec.ts` (smoke)
- State: INTEGRATING — tests and owner check. Built: a modal popup (full-height sheet at 390) replaces the side sheet and the item page; loading, error with retry, one generic gone-or-no-access message, per-field saving with retry, conflict notice, read-only (permission, archived). Left: paged comments and history, "Trường hiển thị trên thẻ" persistence.

### UC-WM-B19 Edit the item description
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: same permission as editing the Item (Editor+); archived or deleted Items refuse (WM-044).
- Preconditions: Item active; actor may update items.
- Input: Description up to 20,000 characters; expected version.
- Output: description advances Item version; same text = no-op [S5]; no event until a consumer needs one; not a field value, not a Document.
- Main flow: 1. Edit the Description section. 2. Save on blur or with the button.
- No-op / rejection / concurrency / retry: Same text = no-op [S5]. Over 20,000 stops with a message. Two editors: the second save keeps their text and a notice offers 'Xem bản mới' or 'Giữ của tôi', naming the actor. Archived or deleted items refuse (WM-044).
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 27.3; WM-044; D28; Q12; WMREQ020, WMREQ110-113; LD1
- Acceptance: Given two editors saving the same item, then the second sees the conflict notice naming the first. Given a 20,001 character text, then it is refused with the limit.
- Evidence: BE BoardItems.Update = PATCH /board-items/{itemId:guid} -> UpdateBoardItemCommand (`BoardItems/Commands/UpdateBoardItem/UpdateBoardItem.cs`) [EV / -] · FE item-description*.tsx · Design Final-Detail, WM-Sheet-Full · Tests `BoardAuthorizationHardeningIntegrationTests.cs` · Real e2e none
- State: INTEGRATING — no actor name on the conflict response (Q12 needs a contract addition); PATCH has no Idempotency-Key.

### UC-WM-B20 Link two items (waits on / related)
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: Editor (relations need CreateField/UpdateField class); target must exist and be viewable by the actor.
- Preconditions: Both items are active on the same board; actor may update the source item.
- Input: Target item id; kind (waits on, blocks, relates to).
- Output: one link per unordered pair; waits-on shows inverse "blocks" on the target; self-link rejected; waits-on cycles (direct or transitive across the Workspace) rejected naming the existing path; pickers disable such targets with that reason (WM-039).
- Main flow: 1. Open Links in the sheet. 2. Pick a target. 3. Choose the kind. 4. Save.
- No-op / rejection / concurrency / retry: A cycle of waits-on links is refused naming the path (D76.8). Self-link refused. Two concurrent opposite links on one board are serialized by the board item-links advisory lock. A target lost between check and commit fails. Retry with the same key has no second effect [S7].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 68, 68.1; WM-039; D76.8; WMREQ113
- Acceptance: Given A waits on B, when B waits on A is attempted, then rejected naming the path A to B. Given a target the viewer cannot read, then the link shows no title. Given a link to an archived item, then it stays and shows archived; given a link to a trashed item, then it is hidden while in trash and removed on purge.
- Evidence: BE BoardItems.ListLinks = GET /board-items/{itemId:guid}/links -> ListBoardItemLinksQuery (`BoardItems/Queries/ListBoardItemLinks/ListBoardItemLinks.cs`) [- / -]; BoardItems.CreateLink = POST /board-items/{itemId:guid}/links -> CreateBoardItemLinkCommand (`ItemLinks/Commands/CreateBoardItemLink/CreateBoardItemLink.cs`) [- / Idem] · FE ItemPageLinks (raw /links); blocks/blocked-by wording · Design WM-TL-Interactions (timeline only); Table/sheet link edit missing · Server cycle check: CreateBoardItemLinkCommandHandler loads the board's waits-on rows, then `DependencyRules.EnsureNoCycle`; rejection is a Domain BusinessRuleException, rule code `WorkManagement_Dependency_CannotCreateCycle`, HTTP 400 business-rule violation with the ordered path (titles and ids) in the message; blocks is stored as the inverse waits-on row, related never forms a cycle · Tests `CreateBoardItemLinkTests.cs` (direct, transitive with path, blocks inverse, related, self), `V4DomainRulesTests.cs`, `ListBoardItemLinksTests.cs` · Real e2e none, no integration test for item links
- State: INTEGRATING — Cycle check done (D76.8); open: cross-board, typed client, design for sheet, integration test of the real pipeline

### UC-WM-B21 Change or remove an item link
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: Editor.
- Preconditions: A link exists.
- Input: Link id and new kind, or remove.
- Output: A kind change is an update, not a second link. A link to an archived item stays and shows it as archived; a link to a trashed item is hidden and removed on purge (D76.8).
- Main flow: 1. Open Links. 2. Change the kind or remove the link.
- No-op / rejection / concurrency / retry: Below Editor = 403 [S2]. Same kind = no-op [S5]. Stale version = conflict [S4]. Removing a missing link returns NotFound.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline; destructive confirm (names the impact)
- Rules: product sections 68.1; WM-039; D76.8
- Acceptance: Given a link kind changed, then the link keeps its id. Given the target archived, then the link stays and shows archived.
- Evidence: BE BoardItems.DeleteLink = DELETE /board-items/{itemId:guid}/links/{linkId:guid} -> DeleteBoardItemLinkCommand (`ItemLinks/Commands/DeleteBoardItemLink/DeleteBoardItemLink.cs`) [- / Idem] · FE ItemPageLinks (raw) · Design WM-TL-Interactions (undo) · Tests `DeleteBoardItemLinkTests.cs` · Real e2e none
- State: INTEGRATING — Dangling-link policy (D76.8); typed client

### UC-WM-B22 See sub-items and derived progress in the item
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: Any viewer.
- Preconditions: Parent item with sub-items.
- Input: Item id.
- Output: Progress is derived and read-only (UC-WM-D09).
- Main flow: 1. Open the parent. 2. The Sub-items section lists children with a progress bar.
- No-op / rejection / concurrency / retry: Read only. Archived children are excluded from the count. A viewer who cannot read a child does not see it in the list.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1])
- Rules: product sections 77.1; WM-041; D76.10
- Acceptance: Given 5 sub-items with 3 done, then the item shows 3/5 and 60 percent.
- Evidence: BE BoardItems.ListChildren = GET /board-items/{itemId:guid}/child-items -> ListChildItemsQuery (`BoardItems/Queries/ListChildItems/ListChildItems.cs`) [- / -] · FE ItemPageChildItems · Design WM-Card-Face (count); parent progress bar missing · Tests none found · Real e2e `sub-items.real.e2e.spec.ts`
- State: HARDENING — the progress rule of D76.10 is partly built (see UC-WM-D09); design for the progress bar.

### UC-WM-B23 Recover from item deleted/archived/moved while open
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: any viewer with the item open.
- Preconditions: An item is open in a sheet or page.
- Input: A change by someone else.
- Output: Deleted: 'deleted by <name>' with Restore for those who may. Archived: read-only with Unarchive. Moved: a link to the new place.
- Main flow: 1. Another member deletes, archives or moves the item. 2. The open item shows the new state and the actor's name. 3. The person goes back or restores.
- No-op / rejection / concurrency / retry: Read only on the client; the server state is authoritative. A write sent from the stale sheet fails by [S1], [S3] or [S4] and nothing changes.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1]); validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 123; D54; Q12
- Acceptance: Given an open item deleted by someone else, then the sheet says so and keeps the person's draft text visible.
- Evidence: BE actor on deleted/conflict responses (Q12 contract addition) not found · FE ItemPageConflictNotice, ItemPageArchivedBanner · Design BC-Notices (deleted/archived/moved while open) · Tests none found · Real e2e none
- State: INTEGRATING — Actor in API/realtime frames (Q12); real e2e

### UC-WM-B24 Request an approval on an item or board
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: requester with Editor on the board; approvers are chosen principals validated by Governance/Workspaces.
- Preconditions: Item active; actor is an Editor; the approver can view the board.
- Input: Item id, one approver (person or team), optional note.
- Output: A Pending ApprovalRequest; counted as waiting review; an ApprovalRequested fact and a notification follow (D78.2).
- Main flow: 1. Open the Approval section. 2. Request approval and pick the approver. 3. The item shows Waiting for <name>.
- No-op / rejection / concurrency / retry: One approver per request (D76.11). A second Pending request on the item is refused. Retry with the same key has no second effect [S7].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 84-89; WM-026; D62, D76.11; LD3
- Acceptance: Given an item, when a request is created, then the Approval section shows Pending and the Space overview counts "waiting review". Given a request naming two approvers, then it is rejected: one approver per request.
- Evidence: BE Approvals.Create = POST /boards/{boardId:guid}/approvals -> CreateApprovalRequestCommand (`Approvals/Commands/CreateApprovalRequest/CreateApprovalRequest.cs`) [- / Idem]; Approvals.RequestForItem = POST /board-items/{itemId:guid}/approval-requests -> RequestItemApprovalCommand (`Approvals/Commands/RequestItemApproval/RequestItemApproval.cs`) [- / Idem] · FE `frontend/packages/product/work-management/web/src/components/item-page/item-page-approval.tsx` (raw /approval-requests) · Design WM-Approval; decide dialog, Er, L, E missing · Tests `ApprovalRequestCommandTests.cs`, `ItemApprovalTests.cs` · Real e2e `approvals.real.e2e.spec.ts`
- State: HARDENING — One approver per request validation (D76.11); typed client; no mock handler

### UC-WM-B25 Decide (approve or reject) a request
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: only a valid approver; Governance decides who may.
- Preconditions: Request is Pending; actor is the named approver, or any member of the approver team.
- Input: Decision approve or reject; a reason when rejecting.
- Output: committed decision state, not inferred from a comment or Status label (WM-026); side effects through explicit contracts: status transition, Automation trigger, notification/activity; ApprovalDecided fact.
- Main flow: 1. The approver opens the item. 2. Chooses Approve or Reject. 3. The request becomes terminal.
- No-op / rejection / concurrency / retry: A non-approver gets 403 [S2]. An already decided request returns a conflict showing the current state. A team approver: the first decision wins (D76.11).
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 86-88; WM-026; D62, D76.11
- Acceptance: Given a non-approver, when they decide, then 403 and no state change. Given a comment "approved", then the request is still Pending. Given a team approver, when any member decides, then the first decision wins and a later decision gets conflict.
- Evidence: BE Approvals.Approve = POST /approvals/{requestId:guid}/approve -> ApproveApprovalRequestCommand (`Approvals/Commands/ApproveApprovalRequest/ApproveApprovalRequest.cs`) [EV / Idem]; Approvals.Reject = POST /approvals/{requestId:guid}/reject -> RejectApprovalRequestCommand (`Approvals/Commands/RejectApprovalRequest/RejectApprovalRequest.cs`) [EV / Idem]; Approvals.Decide = POST /workspaces/{workspaceId:guid}/approvals/{requestId:guid}/decision -> DecideApprovalCommand (`Approvals/Commands/DecideApproval/DecideApproval.cs`) [- / Idem] · FE `frontend/packages/product/work-management/web/src/components/item-page/item-page-approval.tsx`, inbox (raw paths) · Design none; needs decide dialog, reject reason, conflict, L, Er · Tests `ApprovalRequestCommandTests.cs`, `ItemApprovalTests.cs` · Real e2e `approvals.real.e2e.spec.ts`
- State: HARDENING — ApproverTeamId is ignored so team approvers can never decide; two decide paths exist (board-scoped with version, workspace without) (defect B-08).

### UC-WM-B26 Cancel a request
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: requester or manager per Governance.
- Preconditions: Request is Pending; actor is the requester or a Manager.
- Input: Request id, expected version.
- Output: terminal Cancelled; concurrent decide/cancel/expiry resolves deterministically.
- Main flow: 1. Open the request. 2. Choose Cancel. 3. It becomes Cancelled.
- No-op / rejection / concurrency / retry: Cancel after a decision returns a conflict. Concurrent decide and cancel resolve deterministically [S4].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline; destructive confirm (names the impact)
- Rules: product sections 86-88; WM-026; D62
- Acceptance: Given decide and cancel at the same time, then exactly one wins and the other gets conflict.
- Evidence: BE Approvals.Cancel = POST /approvals/{requestId:guid}/cancel -> CancelApprovalRequestCommand (`Approvals/Commands/CancelApprovalRequest/CancelApprovalRequest.cs`) [EV / Idem] · FE cancel in `frontend/packages/product/work-management/web/src/components/item-page/item-page-approval.tsx` (raw) · Design none; needs cancel, race conflict · Tests `ApprovalRequestCommandTests.cs` · Real e2e none
- State: INTEGRATING — Race test; real e2e; design

### UC-WM-B27 Re-request after rejection or cancel
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: requester.
- Preconditions: The previous request is Rejected or Cancelled; actor is the requester.
- Input: Item id, approver.
- Output: a new request per the defined re-request behaviour.
- Main flow: 1. Choose Request again. 2. A new request links to the previous one.
- No-op / rejection / concurrency / retry: Refused while one is Pending. Approvals never expire (D76.11).
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 85, 89; WM-026; D76.11
- Acceptance: Given a rejected request, when re-requested, then a Pending request exists and the history shows both. Given a rejected request, when re-requested, then a new request exists linked to the previous one and both show in history.
- Evidence: BE Approvals.RequestForItem = POST /board-items/{itemId:guid}/approval-requests -> RequestItemApprovalCommand (`Approvals/Commands/RequestItemApproval/RequestItemApproval.cs`) [- / Idem] · FE request again from item section · Design none; needs re-request after reject/cancel · Tests `ItemApprovalTests.cs` · Real e2e none
- State: INTEGRATING — no link from the new request to the previous one (verified: no previous-request field in the Approvals domain); no design.

### UC-WM-B28 See approval state and waiting-review figures
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: ViewBoard.
- Preconditions: Viewer or above.
- Input: Item or board id.
- Output: Approval section on the item; 'waiting review' counts Pending requests on the board or its unfinished items.
- Main flow: 1. Open the Approval section, or the Space overview. 2. The state shows. 3. The waiting-review figure counts each item once.
- No-op / rejection / concurrency / retry: Read only. [S1] for people without ViewBoard. Counts never include items the viewer cannot read.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1])
- Rules: product sections 85, 111; WM-026
- Acceptance: Given two Pending requests on one item, then the figure counts the item once.
- Evidence: BE Approvals.List = GET /boards/{boardId:guid}/approvals -> ListApprovalRequestsQuery (`Approvals/Queries/ListApprovalRequests/ListApprovalRequests.cs`) [- / -]; Approvals.ListForItem = GET /board-items/{itemId:guid}/approval-requests -> ListItemApprovalRequestsQuery (`Approvals/Queries/ListItemApprovalRequests/ListItemApprovalRequests.cs`) [- / -] · FE `frontend/packages/product/work-management/web/src/components/item-page/item-page-approval.tsx`, space overview waiting review · Design WM-Approval (observer view) · Tests `ItemApprovalTests.cs` · Real e2e `approvals.real.e2e.spec.ts`
- State: HARDENING — Tests for List; waiting-review count-once proof

### UC-WM-B29 Import tabular data into a board
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: Editor-level (CreateItem).
- Preconditions: Actor may create items; a file of 5,000 rows or fewer.
- Input: CSV or XLSX file; new or existing board.
- Output: Items are created through normal item operations, all-or-nothing per file (D76.15).
- Main flow: 1. Open Import. 2. Upload the file. 3. Map columns (UC-WM-B30) and review the preview. 4. Confirm; items are created.
- No-op / rejection / concurrency / retry: More than 5,000 rows is refused with the reason. A required field without a value fails the file naming the row. Retry with the same key has no second effect [S7].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 133-134; WM-035; D76.15
- Acceptance: Given a CSV with a bad Number cell, then the import reports the row and the contract-defined outcome. Given a file of 5,001 rows or one bad cell, then nothing is imported and the report names the row.
- Evidence: BE none: no code · FE none · Design none; needs import wizard (upload, mapping, preview, error report, over-limit) · Tests none found · Real e2e none
- State: DEFINED — Roadmap 05: CSV/XLSX import, 5,000 rows, all-or-nothing (D76.15); design, BE, FE

### UC-WM-B30 Map import columns to fields
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: Editor.
- Preconditions: An import file is uploaded.
- Input: Column to field mapping.
- Output: mapping with type-compatibility check.
- Main flow: 1. See the preview. 2. Map each column to a field. 3. Fix incompatible ones.
- No-op / rejection / concurrency / retry: A mapping that is not type-compatible is shown as an error before import.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 134; WM-035; D76.15
- Acceptance: Given a column mapped to an incompatible field, then flagged before import.
- Evidence: BE none: no code · FE none · Design none; needs column-to-field mapping with preview · Tests none found · Real e2e none
- State: DEFINED — Part of D76.15 import; design, BE, FE

### UC-WM-B31 Export a board or view
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: per Board/Item permissions; field-level permissions if used.
- Preconditions: Actor can view the board and is not a Guest.
- Input: Current view with its filters; CSV or XLSX.
- Output: Respects the selected view and filter; hides sensitive fields; large queries are handled without full scans (D76.15).
- Main flow: 1. Open Export. 2. Pick the format. 3. The file downloads.
- No-op / rejection / concurrency / retry: A Guest is refused [S2].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 135, 104; WM-030; D76.15
- Acceptance: Given a person without export right, then 403. Given a Guest, when export is attempted, then it is refused; given a Viewer, then the current view's rows export.
- Evidence: BE none: no code (reports export is Analytics) · FE none; reports export only · Design none; needs export dialog (current view, queued/ready/expired) · Tests none found · Real e2e none
- State: DEFINED — CSV/XLSX of the current view with filters (D76.15); design, BE, FE

### UC-WM-B32 Provider/migration import
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: Integrations.
- Preconditions: An integration provider is connected.
- Input: Provider items.
- Output: separate governed migration path; idempotent on replay. See UC-WM-Q03.
- Main flow: 1. The provider import runs through the Integrations path. 2. It calls Work Management operations.
- No-op / rejection / concurrency / retry: Idempotent on replay. See UC-WM-Q03.
- UI states: none: system behaviour with no screen of its own
- Rules: product sections 110, 133; WM-035
- Acceptance: Given a provider event replayed twice, when it is processed, then one item exists. Given an item the provider cannot map, then the failure is recorded and nothing partial is stored.
- Evidence: BE none: no code · FE none · Design not applicable (no screen) · Tests none found · Real e2e none
- State: DEFINED — Integrations-owned migration path; no WM work scheduled

### UC-WM-B33 Handle a stale-version write conflict
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: editor.
- Preconditions: A write carries an expected version that is stale.
- Input: Write with an old version.
- Output: Version, events, ordering and audit are unchanged.
- Main flow: 1. The server rejects before commit. 2. The UI offers 'keep mine' (after a re-read) or 'use theirs', naming the actor.
- No-op / rejection / concurrency / retry: Conflict kinds: stale version, schema changed, field removed, relation target unavailable.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 127-128; WM-012, WM-034; WMREQ110-113; Q12
- Acceptance: (1) Given B has an unsaved edit on field F and A changes F, when B saves, then the in-cell conflict choice appears; B's draft is never overwritten before that. (2) Given a version conflict on a cell, when the write returns 412, then the cell offers "Giữ giá trị của tôi" (re-reads first, then writes) and "Dùng giá trị mới" (drops the typed value). (3) Given a version conflict on a cell, a description conflict, or an item deleted by another person, when the notice shows, then it names that person (the actor comes from the API, not the client); when the actor is unknown (system or removed user) it uses neutral copy (owner Q12; the actor on conflict responses and on deleted/moved realtime frames is a contract addition the architect designs).
- Evidence: BE EV on 38 of 149 endpoints; competing writes without EV (Part 3) · FE ItemPageConflictNotice, 409 handling; no actor name · Design BC-Notices, Final-Detail `xung-dot` · Tests none found · Real e2e none
- State: INTEGRATING — Actor on conflict (Q12 contract); EV gaps; real e2e

### UC-WM-B34 Item deleted or archived by someone else while open
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: Any viewer with the item open.
- Preconditions: An item is open or listed.
- Input: Another member deletes or archives it.
- Output: The notice names the actor; Guests see 'Một người khác' (Q12).
- Main flow: 1. The row leaves in place. 2. An open sheet shows 'deleted by <name>' or 'archived'.
- No-op / rejection / concurrency / retry: A write from the stale row fails by [S1] or [S3] and nothing changes. Restore after delete is offered only to people with UpdateItem.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1]); validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 123, 128; Q12
- Acceptance: Given an item deleted by another member, when it is open, then the sheet says it was deleted and names the actor, and the row leaves the list. Given a Guest viewer, then the notice says 'Một người khác'.
- Evidence: BE actor (Q12) missing · FE ItemPageConflictNotice · Design BC-Notices · Tests none found · Real e2e none
- State: INTEGRATING — actor name on the deleted notice needs the Q12 contract addition; live delivery needs open PR #193; real e2e.

### UC-WM-B35 Recover from a target lost during commit
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: system.
- Preconditions: A link or assignment is committed while its target disappears.
- Input: Race between validation and commit.
- Output: relation target or person disappearing between validation and commit = fail or approved tombstone, no falsely valid reference.
- Main flow: 1. The commit re-checks the target. 2. It fails with no false reference.
- No-op / rejection / concurrency / retry: A reference never becomes valid by accident: the commit re-checks the target and fails if it is gone. Retry with the same key has no second effect [S7].
- UI states: none: system behaviour with no screen of its own
- Rules: product sections 131; WM-034
- Acceptance: Given a link target deleted during the write, then the link is not created.
- Evidence: BE BoardItems.CreateLink = POST /board-items/{itemId:guid}/links -> CreateBoardItemLinkCommand (`ItemLinks/Commands/CreateBoardItemLink/CreateBoardItemLink.cs`) [- / Idem] · FE not applicable · Design not applicable (no screen) · Tests `CreateBoardItemLinkTests.cs` · Real e2e none
- State: INTEGRATING — the race is not covered by a test.

### UC-WM-B36 See a bulk failure
- Capability: B BoardItem lifecycle (WM-002)
- Actors and permission: editor.
- Preconditions: A bulk edit runs.
- Input: A batch with a failing item.
- Output: Board bulk is all-or-nothing, so failure is whole-batch and names the first failing item; retries never double-apply; partial success, where supported elsewhere, must be visible.
- Main flow: 1. The server rejects the whole batch. 2. The UI names the first failing item and keeps the selection.
- No-op / rejection / concurrency / retry: All-or-nothing: nothing changes on failure [S4]. Retry with the same key does not double-apply [S7]. Partial success is not offered.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 132; WM-034; D32, D77.3
- Acceptance: (1) Given a bulk network failure, when retried, then the retry carries the same idempotency key and does not apply twice. (2) Given a bulk action rejected by the server, when it returns, then nothing changed, the first failing item and reason are named, and the selection stays.
- Evidence: BE BoardItems.BulkUpdate = POST /boards/{boardId:guid}/items/bulk -> BulkUpdateBoardItemsCommand (`BoardItems/Commands/BulkUpdateBoardItems/BulkUpdateBoardItems.cs`) [- / Idem] · FE `frontend/packages/product/work-management/web/src/components/views/table/table-bulk-bar.tsx` · Design BC-Bulk (error, >200) · Tests `BulkUpdateBoardItemsTests.cs` · Real e2e none
- State: INTEGRATING — real e2e; proof that the failing item is named and the selection stays.

## C. BoardField definitions

Team capability WM-003.

### UC-WM-C01 Add a custom field
- Capability: C BoardField definitions (WM-003)
- Actors and permission: Editor (`CreateField`); Commenter/Viewer cannot; Guest per grant.
- Preconditions: Board is active.
- Input: Field name, type, type settings, optional default.
- Output: BoardField with stable id/key; appears as column unless hidden; FieldCreated fact; other members refetch (§117 schema change).
- Main flow: 1. Open Settings, Fields, Add field. 2. Pick a type and fill its settings. 3. Save; the field shows as a column.
- No-op / rejection / concurrency / retry: settings validated per type; default must be valid for type/settings; one semantic contract per type (WM-007); creates new Field Type only via full contract; [S2].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 15-22; WM-006, WM-007, WM-008; WMREQ026-028, WMREQ030
- Acceptance: Given an Editor, when they add a Select field with 3 options, then the field shows as a column for everyone and each option has a stable id. Given an invalid default for a Number with a maximum, when saved, then it is rejected with the reason. Given a Viewer, then Add field is absent.
- Evidence: BE BoardFields.Create = POST /boards/{boardId:guid}/fields -> CreateBoardFieldCommand (`BoardFields/Commands/CreateBoardField/CreateBoardField.cs`) [- / Idem] · FE `frontend/packages/product/work-management/web/src/components/board-settings/board-settings-fields.tsx`, useCreateItemField · Design Final-Settings `truong`/`them-truong`, WM-Field-Sheet(-Dem) · Tests `CreateBoardFieldTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; design measurement of the inline name message. Evidence: real-stack check 2026-10-31: fields of nine types created through the API; empty and 101-char names and an unknown type refused (400); a name already used on the board (trimmed, any case) answers 409 work-management.field.name-taken and the new-property form in the card popup shows it inline in the browser, commit b45baefb5, da1695a37.

### UC-WM-C02 Rename a field or change its settings
- Capability: C BoardField definitions (WM-003)
- Actors and permission: Editor.
- Preconditions: Board active; field exists and is not a system field.
- Input: New name and/or settings, expected version.
- Output: label is mutable; field identity/key stays (WM-006); settings change validated; values, views, filters, formulas keep working.
- Main flow: 1. Open the field editor. 2. Change the name or a setting. 3. Save; existing values, views and filters keep working.
- No-op / rejection / concurrency / retry: Type is immutable after creation (D76.5); a settings change that would strand values is refused. Same content = no-op [S5].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 16, 20; WM-006; WMREQ026, WMREQ030-032, WMREQ110-113
- Acceptance: Given a field renamed, then views, filters and automation referring to its key still work. Given an existing field, when the person looks for change type, then the UI says to create a new field.
- Evidence: BE BoardFields.Update = PATCH /boards/{boardId:guid}/fields/{fieldId:guid} -> UpdateBoardFieldCommand (`BoardFields/Commands/UpdateBoardField/UpdateBoardField.cs`) [EV / Idem] · FE field panel; 'change type' says create new field · Design Final-Settings `truong`; WM-Field-Sheet · Tests `BoardChildAccessIntegrationTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Evidence: real-stack check 2026-10-31: rename with the expected version, the same name changed nothing, a stale version answers 412, a taken name answers 409, a case-only rename of itself passes, commit b45baefb5, da1695a37.

### UC-WM-C03 Change a field default
- Capability: C BoardField definitions (WM-003)
- Actors and permission: Editor.
- Preconditions: Board active; field exists.
- Input: New default, valid for the field type and settings.
- Output: applies to new/unset values only; existing values not rewritten (WM-008). Default must be valid under type/settings.
- Main flow: 1. Open the field editor. 2. Set or clear the default. 3. Save.
- No-op / rejection / concurrency / retry: A default that is invalid for the type is refused. The default applies to new items only; existing values are never rewritten (WM-008). Same default = no-op [S5].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 21-22; WM-008; WMREQ033
- Acceptance: Given 10 existing items with the old default, when the default changes, then those 10 stay unchanged and a new item gets the new default.
- Evidence: BE BoardFields.Update = PATCH /boards/{boardId:guid}/fields/{fieldId:guid} -> UpdateBoardFieldCommand (`BoardFields/Commands/UpdateBoardField/UpdateBoardField.cs`) [EV / Idem] · FE default editor not evidenced · Design WM-Field-Sheet (default) · Tests `BoardChildAccessIntegrationTests.cs` · Real e2e none
- State: INTEGRATING — Test existing values unchanged; UI proof

### UC-WM-C04 Archive or delete a custom field
- Capability: C BoardField definitions (WM-003)
- Actors and permission: Editor (`DeleteField`).
- Preconditions: Board active; field is not a system field.
- Input: Field id; where dependants go when needed.
- Output: field leaves schema; historical values not reinterpreted as another field; dependents (views, filters, formulas, rollups, forms, automation, integrations) are migrated or rejected explicitly; stale conditions are ignored with a hint; items grouped by a deleted option show in "No value".
- Main flow: 1. Open the field menu, Delete. 2. Review the impact on views, filters, formulas and forms. 3. Confirm. 4. The field leaves the schema and can be restored for 30 days (D76.5).
- No-op / rejection / concurrency / retry: A field that a formula, rollup or form needs is rejected or its dependants are repaired and reported. Stale filter conditions are ignored with a hint (UC-WM-H12). Stale version = conflict [S4]. System fields refuse (UC-WM-C06).
- UI states: destructive confirm (names the impact)
- Rules: product sections 124, 25; WM-009; D76.5; WMREQ035, WMREQ040, WMREQ110-113
- Acceptance: Given a formula that references field F, when F is deleted, then the action is rejected naming the formula. Given a field deleted 5 days ago, when an Editor restores it, then its values return; after 30 days restore is refused.
- Evidence: BE BoardFields.Delete = DELETE /boards/{boardId:guid}/fields/{fieldId:guid} -> DeleteBoardFieldCommand (`BoardFields/Commands/DeleteBoardField/DeleteBoardField.cs`) [- / Idem] · FE board-settings-field-delete; fieldArchive flag locked · Design none; needs delete-field confirm listing dependents (formula, view, form) · Tests `DeleteBoardFieldTests.cs` · Real e2e `work-board-management.real.e2e.spec.ts` (delete field)
- State: HARDENING — real e2e spec, tests, design measurement and owner check; restore has no screen (UC-WM-C05). Evidence: real-stack check 2026-10-31: delete of a field names the expected version (stale 412) and the field left the schema; deleting a field a formula reads is refused (400) naming the formula, commit b45baefb5, da1695a37.

### UC-WM-C05 Restore an archived field
- Capability: C BoardField definitions (WM-003)
- Actors and permission: Editor.
- Preconditions: Field was deleted less than 30 days ago.
- Input: Field id.
- Output: values previously held return; views not auto-reapplied unless stored. [S2]
- Main flow: 1. Open Settings, Fields, Deleted. 2. Choose Restore. 3. The field and its values return; views are not re-applied unless stored.
- No-op / rejection / concurrency / retry: After 30 days restore is refused with the reason. Below Editor = 403 [S2]. Restoring an active field is a no-op [S5].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 124-125; D76.5; WMREQ035
- Acceptance: Given an archived field restored, then old item values show again.
- Evidence: BE none: no endpoint (domain event exists) · FE none · Design none; needs archived/deleted fields list, restore · Tests none found · Real e2e none
- State: IMPLEMENTING — the restore endpoint (POST /boards/{id}/fields/{fieldId}/restore, a Manager, within 30 days, a taken name refused with 409) and the deleted-fields list (GET /boards/{id}/fields/deleted) run through the API (real-stack check 2026-10-31, commit b45baefb5); no screen lists or restores a deleted field yet (needs an artboard and the owner's wording), tests and owner check.

### UC-WM-C06 Attempt to edit or delete a system field
- Capability: C BoardField definitions (WM-003)
- Actors and permission: any.
- Preconditions: A system field exists on the board.
- Input: Any edit or delete attempt.
- Output: refused; creation/update metadata and other system fields are read-only and non-deletable though they share BoardField storage (WM-009); mutability, visibility explicit.
- Main flow: 1. A person opens a system field's menu. 2. Edit and Delete are absent or disabled with the reason. 3. A direct API call is refused.
- No-op / rejection / concurrency / retry: System fields (creation and update metadata and the like) are read-only and non-deletable. API refusal is 403 or 409 with a stable error code and no state change.
- UI states: read-only (locked control with the reason), permission denied
- Rules: product sections 23-24; WM-009; WMREQ029
- Acceptance: Given a Created-at system field, when a Manager attempts delete via API, then refused.
- Evidence: BE domain rule (Fields 102 tests); no API refusal test · FE read-only column header lock · Design WM-Field-Editors (locked, ƒx header lock) · Tests none found · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Evidence: real-stack check 2026-10-31: renaming Title, Status, Assignee or Due Date answers 409 work-management.field.system-locked, the same name changes nothing, deleting one is refused (400); no screen offers a rename of a field today, commit b45baefb5, da1695a37.

### UC-WM-C07 Manage Select / Status / Priority options
- Capability: C BoardField definitions (WM-003)
- Actors and permission: Editor; status management on views may be restricted by `statusManageRoles` (D55).
- Preconditions: Board active; the field is a Status, Priority, Select or Multi-select field.
- Input: Option name, colour, order; for Status the category (UC-WM-C08).
- Output: option identity is stable; rename/recolor never turns values into unrelated data (WM-037); everyone sees the new name; deleted option with existing values: items go to visible "No value" group; multi-select set semantics and removed-option behaviour defined.
- Main flow: 1. Open the field's options. 2. Add, rename, recolour, reorder or delete an option. 3. Everyone sees the change.
- No-op / rejection / concurrency / retry: Deleting an option that holds values asks where those items go: a Status needs another Status; another Select may leave no value (D55). Stale version = conflict [S4]. Counts per option come from the settings read (D65).
- UI states: destructive confirm (names the impact)
- Rules: product sections 142-143, 148; WM-037, WM-038; D55; D65; WMREQ034-035, WMREQ074-075, WMREQ141
- Acceptance: Given option "In Progress" renamed, then 40 items keep the option and show the new label.
- Evidence: BE BoardFields.AddFieldOption = POST /boards/{boardId:guid}/fields/{fieldId:guid}/options -> AddFieldOptionCommand (`BoardFields/Commands/AddFieldOption/AddFieldOption.cs`) [EV / Idem]; BoardFields.RemoveFieldOption = DELETE /boards/{boardId:guid}/fields/{fieldId:guid}/options/{optionId:guid} -> RemoveFieldOptionCommand (`BoardFields/Commands/RemoveFieldOption/RemoveFieldOption.cs`) [EV / Idem]; BoardFields.UpdateFieldOption = PATCH /boards/{boardId:guid}/fields/{fieldId:guid}/options/{optionId:guid} -> UpdateFieldOptionCommand (`BoardFields/Commands/UpdateFieldOption/UpdateFieldOption.cs`) [EV / Idem]; BoardFields.MoveFieldOption = POST /boards/{boardId:guid}/fields/{fieldId:guid}/options/{optionId:guid}/move -> MoveFieldOptionCommand (`BoardFields/Commands/MoveFieldOption/MoveFieldOption.cs`) [EV / Idem] · FE `frontend/packages/product/work-management/state/src/cache/field-option.ts`, StatusOptionForm (16 tests) · Design Final-Settings status/select; where-items-go dialog not drawn · Tests `FieldOptionCategoryTests.cs`, `RemoveFieldOptionTests.cs`, `WorkItemPlacementIntegrationTests.cs` · Real e2e `board-settings.real.e2e.spec.ts` (adds option)
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Evidence: real-stack check 2026-10-31: option added, renamed, moved; a duplicate name, a non-hex colour and an empty name refused; removing an option moved the items to the replacement, a stale version answers 412, commit 6e59c53bb.

### UC-WM-C08 Set a Status option's category
- Capability: C BoardField definitions (WM-003)
- Actors and permission: Editor.
- Preconditions: Board active; field is a Status field.
- Input: Option id and category (todo, in progress, review, done).
- Output: categories todo / in progress / review / done; only done marks completion (WM-042).
- Main flow: 1. Open the Status options. 2. Set a category per option. 3. Save; items in done-category options count as complete.
- No-op / rejection / concurrency / retry: A Status field keeps at least one Todo and one Done option; a change that removes the last of either is refused.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 148.1; WM-042; WMREQ034
- Acceptance: Given a board with one Done option, when an Editor changes it to Review, then rejected with "last Done option".
- Evidence: BE RemoveFieldOption/UpdateFieldOption (last Todo/Done rule) · FE menu entry disabled with reason (D55) · Design Final-Settings status (category) · Tests none found · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Evidence: real-stack check 2026-10-31: changing the category of the last Done option is refused, commit 6e59c53bb.

### UC-WM-C09 Define Text and Long text fields
- Capability: C BoardField definitions (WM-003)
- Actors and permission: Editor.
- Preconditions: Add-field flow open (UC-WM-C01).
- Input: Type Text or Long text; optional length limit.
- Output: Canonical text; Long text edits in a popover; normalization before equality.
- Main flow: 1. Choose Text or Long text. 2. Set limits. 3. Save.
- No-op / rejection / concurrency / retry: Text over the limit is refused inline; unchanged text is a no-op [S5].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 19, 31; WM-007; WMREQ027-028, WMREQ037
- Acceptance: Given a Text field with a limit of 10, when someone saves 11 characters, then the cell shows an inline reason and nothing is stored. Given the same text saved twice, then the version does not change.
- Evidence: BE BoardFields.Create = POST /boards/{boardId:guid}/fields -> CreateBoardFieldCommand (`BoardFields/Commands/CreateBoardField/CreateBoardField.cs`) [- / Idem]; BoardItems.UpdateFieldValue = PATCH /board-items/{itemId:guid}/values/{fieldId:guid} -> UpdateBoardItemFieldValueCommand (`BoardItems/Commands/UpdateBoardItemFieldValue/UpdateBoardItemFieldValue.cs`) [EV / Idem] · FE sheet/page on develop; Table cell editors in PR #204 · Design WM-Field-Editors(-Dem) · Tests `BulkUpdateBoardItemsTests.cs`, `CreateBoardFieldTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Evidence: real-stack check 2026-10-31: field of that type created through the API; valid values set, cleared and read back, invalid ones refused (400), a stale version answers 412, commit 6e59c53bb.

### UC-WM-C10 Define a Number field
- Capability: C BoardField definitions (WM-003)
- Actors and permission: Editor.
- Preconditions: Add-field flow open.
- Input: Type Number; precision, range, display format.
- Output: Canonical value is separate from display format; out-of-range values are rejected inline.
- Main flow: 1. Choose Number. 2. Set precision and range. 3. Save.
- No-op / rejection / concurrency / retry: Same value = no-op [S5]. Out-of-range or non-numeric = validation error and nothing stored. Below Editor = 403 [S2].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 145; WM-007; WMREQ027-028
- Acceptance: Given a Number field with range 0 to 100, when someone enters 101, then an inline error appears and nothing is stored. Given a display format of 2 decimals, then the stored value is not rounded.
- Evidence: BE BoardFields.Create = POST /boards/{boardId:guid}/fields -> CreateBoardFieldCommand (`BoardFields/Commands/CreateBoardField/CreateBoardField.cs`) [- / Idem] · FE sheet/page; Table cells in PR #204 · Design WM-Field-Editors(-Dem) · Tests `CreateBoardFieldTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Evidence: real-stack check 2026-10-31: field of that type created through the API; valid values set, cleared and read back, invalid ones refused (400), a stale version answers 412, commit 6e59c53bb.

### UC-WM-C11 Define Date and DateTime fields
- Capability: C BoardField definitions (WM-003)
- Actors and permission: Editor.
- Preconditions: Add-field flow open.
- Input: Type Date (day) or DateTime (instant, deferred by D76.5).
- Output: A date-only value is a day, never silently converted to a UTC instant. DateTime is deferred (D76.5).
- Main flow: 1. Choose Date. 2. Save.
- No-op / rejection / concurrency / retry: Same day = no-op [S5]. Due before start = validation error. Below Editor = 403 [S2].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 146, 138; WM-007; D76.5; WMREQ028
- Acceptance: Given a Date field, when a day is saved, then the same day reads back in every time zone. Given an inverted start and due range, then it is rejected inline.
- Evidence: BE Date field (UTC days) · FE ItemDatePicker; Table date cell in PR #204 (calendar only) · Design WM-Field-Editors(-Dem) (Date) · Tests none found · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Evidence: real-stack check 2026-10-31: field of that type created through the API; valid values set, cleared and read back, invalid ones refused (400), a stale version answers 412, commit 6e59c53bb.

### UC-WM-C12 Define Select and Multi-select fields
- Capability: C BoardField definitions (WM-003)
- Actors and permission: Editor.
- Preconditions: Add-field flow open.
- Input: Options (name, colour, order); Select takes one value, Multi-select a set.
- Output: Options carry stable ids; Multi-select has set semantics (no duplicates).
- Main flow: 1. Choose Select or Multi-select. 2. Add options. 3. Save.
- No-op / rejection / concurrency / retry: Duplicate option names in one field are refused. Removing an option that holds values asks for a replacement (D55). Stale version = conflict [S4].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 142-144; WM-037; WMREQ034
- Acceptance: Given a Select option renamed, when the board is read, then items keep the same option id and show the new name. Given a Multi-select, when the same option is added twice, then the set holds it once.
- Evidence: BE field options + UpdateFieldValue · FE sheet/page; Table cells in PR #204 · Design WM-Field-Editors(-Dem) · Tests none found · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Evidence: real-stack check 2026-10-31: field of that type created through the API; valid values set, cleared and read back, invalid ones refused (400), a stale version answers 412; the board read now lists the options of a multi-select field, commit 6e59c53bb.

### UC-WM-C13 Define a Person (People) field
- Capability: C BoardField definitions (WM-003)
- Actors and permission: Editor.
- Preconditions: Add-field flow open.
- Input: Type People; single or multiple.
- Output: Values are stable principal ids; every person must be able to view the board (WM-040).
- Main flow: 1. Choose People. 2. Pick single or multiple. 3. Save.
- No-op / rejection / concurrency / retry: A person who cannot view the board is refused with 'share the board first' [S2]. Same set = no-op [S5].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 139; WM-036, WM-040; D41; Q7; WMREQ022
- Acceptance: Given a person without access to the board, when they are picked, then the write is refused with 'share the board first'. Given a Guest, then the picker lists only people on this board and no emails.
- Evidence: BE UpdateFieldValue + AssignMember (view-access check) · FE ItemAssigneePicker; Guest board-only list unproven · Design WM-Field-Editors; BC-Create-320-Pickers (1000 people) · Tests none found · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Evidence: real-stack check 2026-10-31: field of that type created through the API; valid values set, cleared and read back, invalid ones refused (400), a stale version answers 412, commit 6e59c53bb.

### UC-WM-C14 Define a Link (and file) field
- Capability: C BoardField definitions (WM-003)
- Actors and permission: Editor.
- Preconditions: Add-field flow open.
- Input: Type Link; absolute http or https URL. File uploads are deferred (D76.5).
- Output: URL reference with host display, distinct from an uploaded file identity.
- Main flow: 1. Choose Link. 2. Save.
- No-op / rejection / concurrency / retry: A non-URL or non-http(s) value is refused inline. Same value = no-op [S5].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 141; WM-007; D76.5; WMREQ027-028
- Acceptance: Given a Link field, when a non-URL or a non-http scheme is entered, then it is rejected inline. Given a valid URL, then the cell shows the host.
- Evidence: BE field value validation · FE link editor; file field deferred · Design WM-Field-Editors (Link) · Tests none found · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Evidence: real-stack check 2026-10-31: field of that type created through the API; valid values set, cleared and read back, invalid ones refused (400), a stale version answers 412, commit 6e59c53bb.

### UC-WM-C15 Define a Checkbox field
- Capability: C BoardField definitions (WM-003)
- Actors and permission: Editor.
- Preconditions: Add-field flow open.
- Input: Type Checkbox.
- Output: Boolean; one tap or Space toggles; groupable.
- Main flow: 1. Choose Checkbox. 2. Save.
- No-op / rejection / concurrency / retry: Same value = no-op [S5]. A computed field is not writable.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 19; WM-007
- Acceptance: Given a Checkbox cell, when a person presses Space, then the value flips and saves once. Given a group-by on a Checkbox field, then two groups appear.
- Evidence: BE UpdateFieldValue · FE sheet/page; Table cell in PR #204 · Design WM-Field-Editors (Checkbox) · Tests none found · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Evidence: real-stack check 2026-10-31: field of that type created through the API; valid values set, cleared and read back, invalid ones refused (400), a stale version answers 412, commit 6e59c53bb.

### UC-WM-C16 Status and Priority as specialized select types
- Capability: C BoardField definitions (WM-003)
- Actors and permission: Editor.
- Preconditions: Board active.
- Input: Status or Priority specialised select.
- Output: Stronger semantics than a plain Select; Status is context-local, never a global enum; option colour plus text.
- Main flow: 1. Use the default Status and Priority fields. 2. Edit options (UC-WM-C07).
- No-op / rejection / concurrency / retry: A Status field must keep one Todo and one Done option (UC-WM-C08). Same option = no-op [S5]. Stale version = conflict [S4].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 147-148; WM-038; WMREQ034
- Acceptance: Given a Priority option, then it shows its colour and its text. Given two boards, then each keeps its own Status options.
- Evidence: BE BoardFields.Create = POST /boards/{boardId:guid}/fields -> CreateBoardFieldCommand (`BoardFields/Commands/CreateBoardField/CreateBoardField.cs`) [- / Idem] · FE status-option, priority-glyph · Design Final-Settings, Kanban-Card-States · Tests `CreateBoardFieldTests.cs` · Real e2e none
- State: INTEGRATING — Real e2e; status per board proof

### UC-WM-C17 Keep one semantic contract per field type
- Capability: C BoardField definitions (WM-003)
- Actors and permission: product/engineering (acceptance for any new or changed type).
- Preconditions: A field type is added or changed.
- Input: The type's full contract.
- Output: One semantic contract per type, shared by web, mobile, Automation, import and export.
- Main flow: 1. The change lists settings schema, value, normalization, validation, default, equality, filter, sort, group, editor, renderer, import, export, automation and index. 2. All hosts agree before release.
- No-op / rejection / concurrency / retry: A new type that lacks any item of the contract is not released. An unknown field type fails closed.
- UI states: none: system behaviour with no screen of its own
- Rules: product sections 17-19, 170; WM-007; WMREQ027-028
- Acceptance: Given each of the field types, when sort and filter run on a fixed dataset, then client and server results are equal.
- Evidence: BE Domain Fields 102 tests; field-type registry · FE core/field-types registry · Design not applicable (no screen) · Tests none found · Real e2e none
- State: INTEGRATING — fixed-dataset parity test (client vs server) for 12 types

### UC-WM-C18 Relation field: reference items of another board
- Capability: C BoardField definitions (WM-003)
- Actors and permission: Editor (CreateField).
- Preconditions: Board active; the target board is in the same Workspace.
- Input: Field name, target board.
- Output: A set of stable item ids; a person sees only linked items they can view, others show as 'Không có quyền xem' (D78.6).
- Main flow: 1. Add a Relation field. 2. Pick the target board. 3. In an item, pick target items; the five Relations endpoints fold into this field (D78.6).
- No-op / rejection / concurrency / retry: Target deleted or archived follows the link policy of UC-WM-B21. Stale version = conflict [S4].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 63-67; WM-021, WM-029; D78.6
- Acceptance: Given a relation to a board the viewer cannot read, then the cell shows no titles. Given a target item moved to Trash, then the relation is hidden while in Trash.
- Evidence: BE Relations.* board relation endpoints exist (no tests, no UI); no relation field type; Relations.Create = POST /boards/{boardId:guid}/relations; Relations.List = GET /boards/{boardId:guid}/relations; Relations.Delete = DELETE /relations/{relationId:guid}; Relations.Pause = PATCH /relations/{relationId:guid}/pause; Relations.Resume = PATCH /relations/{relationId:guid}/resume · FE none; Relations.* never called · Design none; needs relation field picker (items of another board) · Tests none found · Real e2e none
- State: IMPLEMENTING — Relations.* endpoints exist (create, list, delete, pause, resume) with no tests and no UI; no Relation field type exists.

### UC-WM-C19 Mark a field required
- Capability: C BoardField definitions (WM-003)
- Actors and permission: Editor.
- Preconditions: Board active.
- Input: Required flag on a field.
- Output: Create without the value is refused naming the field; existing items are not made invalid retroactively (D78.7).
- Main flow: 1. Open the field editor. 2. Turn on Required. 3. New items must carry a value.
- No-op / rejection / concurrency / retry: Enforced on dialog, form, import and API. Unchanged flag = no-op [S5].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 21-22; WM-008; D78.7
- Acceptance: Given a required Text field, when an item is created without it, then rejected with the field named. Given existing items without a value, when the flag is turned on, then they are listed, not changed.
- Evidence: BE none · FE none · Design none; needs required marker, required-missing state · Tests none found · Real e2e none
- State: DEFINED — no required flag, no enforcement on any write path; decided in D78.7.

### UC-WM-C20 Define a Formula field
- Capability: C BoardField definitions (WM-003)
- Actors and permission: Editor (CreateField).
- Preconditions: Board active; the source fields exist and are compatible.
- Input: Formula expression in the current language (D76.10).
- Output: derived, non-authoritative, not editable (WM-022); dependency graph; cycles rejected or explicitly handled.
- Main flow: 1. Add a Formula field. 2. Write the expression. 3. Save; invalid references are refused.
- No-op / rejection / concurrency / retry: Cycles are refused; a reference to an inactive field is refused; below Editor = 403 [S2].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 69-73; WM-022; D76.10
- Acceptance: Given a formula referencing itself indirectly, when saved, then rejected naming the cycle.
- Evidence: BE BoardFields.Create = POST /boards/{boardId:guid}/fields -> CreateBoardFieldCommand (`BoardFields/Commands/CreateBoardField/CreateBoardField.cs`) [- / Idem] · FE `frontend/packages/product/work-management/web/src/components/board-settings/board-settings-computed-dialog.tsx` (no validation beyond node count) · Design WM-Field-Computed(-Dem); cycle, formula Er, stale missing · Tests `CreateBoardFieldTests.cs` · Real e2e none
- State: INTEGRATING — Cycle error design; formula Er; real e2e

### UC-WM-C21 Define a Rollup field
- Capability: C BoardField definitions (WM-003)
- Actors and permission: Editor.
- Preconditions: Board active; items with sub-items exist or will.
- Input: Aggregation (count, sum, avg, min, max) over a sub-item field.
- Output: Rollups work over sub-items only (D76.10); read-only derived projection.
- Main flow: 1. Add a Rollup field. 2. Pick the function and the sub-item field. 3. Save.
- No-op / rejection / concurrency / retry: incompatible source field types rejected.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 74-75; WM-023; D76.10
- Acceptance: Given a Rollup of sum over sub-item numbers, when a sub-item changes, then the parent's rollup updates on the next read. Given an incompatible source field, then the field is refused.
- Evidence: BE Rollup over sub-items (8 tests) · FE computed dialog · Design WM-Field-Computed (rollup) · Tests none found · Real e2e none
- State: INTEGRATING — Real e2e

### UC-WM-C22 Create, rename, recolor and delete a label
- Capability: C BoardField definitions (WM-003)
- Actors and permission: Editor (labels use the field-schema action class); Commenter/Viewer cannot.
- Preconditions: Board active; actor is an Editor.
- Input: Label name and colour from the palette.
- Output: Board child label; label vs Status/Select must keep distinct semantics (WM-024).
- Main flow: 1. Open Settings, Labels. 2. Create, rename, recolour or delete. 3. Delete shows how many items lose the label.
- No-op / rejection / concurrency / retry: Stale version on update or delete = conflict [S4] (not enforced today). Same name and colour = no-op [S5].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline; destructive confirm (names the impact)
- Rules: product sections 78-79; WM-024; D61; WMREQ023
- Acceptance: Given a label renamed, then items show the new name.
- Evidence: BE Labels.Create = POST /boards/{boardId:guid}/labels -> CreateLabelCommand (`Labels/Commands/CreateLabel/CreateLabel.cs`) [- / Idem]; Labels.Update = PATCH /labels/{labelId:guid} -> UpdateLabelCommand (`Labels/Commands/UpdateLabel/UpdateLabel.cs`) [- / Idem]; Labels.Delete = DELETE /labels/{labelId:guid} -> DeleteLabelCommand (`Labels/Commands/DeleteLabel/DeleteLabel.cs`) [- / Idem] · FE `frontend/packages/product/work-management/web/src/components/board-settings/board-settings-labels.tsx` · Design WM-Labels; Er missing · Tests `BoardManagementLevelTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; design. Evidence: real-stack check 2026-10-31: label created, renamed, recoloured and deleted through the API with the expected version (stale 412, same content changes nothing); a non-hex colour is refused; deleting it removed it from the item; a conflict shows the board settings reload notice, commit b45baefb5, da1695a37.

## D. Field values

Team capability WM-004.

### UC-WM-D01 Validate a value against the current schema
- Capability: D Field values (WM-004)
- Actors and permission: system on every write path (UI, API, Automation, Import, Forms, Integrations).
- Preconditions: Any write path that sets a field value.
- Input: Field id, value.
- Output: Unknown field ids, fields of another board, incompatible types and invalid references fail rather than persist; no partial write.
- Main flow: 1. The server loads the current schema. 2. It validates field id, board, type and value. 3. It stores the normalized value or refuses.
- No-op / rejection / concurrency / retry: Fail closed: no partial persistence. Stale schema = conflict [S4]. Same value = no-op [S5].
- UI states: none: system behaviour with no screen of its own
- Rules: product sections 29-31; WM-011; WMREQ024-025, WMREQ036, WMREQ040
- Acceptance: Given a field id of another board, when a value is written, then it is refused and nothing is stored. Given a bulk value write with one invalid value, then none is stored.
- Evidence: BE BoardFields.GetSchema = GET /boards/{boardId:guid}/schema -> GetBoardSchemaQuery (`BoardSchema/Queries/GetBoardSchema/GetBoardSchemaQuery.cs`) [- / -] · FE GetSchema op unused by UI · Design not applicable (no screen) · Tests `GetBoardSchemaTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Evidence: real-stack check 2026-10-31: values of every field type checked through the API: number text, link without http(s), checkbox string, bad date, duplicate multi-select ids, an option or a person that does not belong are refused (400); a field of another board answers 404; a batch with one invalid value changes nothing, commits 609c34dbd, d290bb373.

### UC-WM-D02 Edit a custom field value in place (cell, sheet, page)
- Capability: D Field values (WM-004)
- Actors and permission: Editor and above; Commenter/Viewer see values, no editor, header lock explains.
- Preconditions: Item active; the field is writable (not computed or system).
- Input: Field id, value, expected version.
- Output: committed normalized value shown; cell shows "Đang lưu"; derived fields update after read; FieldValueChanged only when value actually changes [S5]; peers refetch [S6].
- Main flow: 1. Click a cell, or use the sheet or page editor. 2. Edit the value. 3. Commit; the cell shows 'Đang lưu' then the stored value.
- No-op / rejection / concurrency / retry: invalid value keeps editor open with inline reason; 412 conflict shows "Giữ giá trị của tôi" / "Dùng giá trị mới", names actor; mid-edit permission loss = editor closes, reason line; network error keeps typed value with "Thử lại"; item deleted meanwhile = edit dropped with toast; stale schema = conflict and reload.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 29-32; WM-011; WMREQ024-025, WMREQ036-040
- Acceptance: (1) Given a Viewer or Commenter, when they select any cell, then no editor opens and the header lock explains why. (2) Given B has an unsaved edit on field F and A changes F, when B saves, then the in-cell conflict choice appears; B's draft is never overwritten before that. (3) Given a Member on any editable cell, when Enter/double-click opens it, then the editor matches the field type and Enter saves, Escape restores, leaving saves.
- Evidence: BE BoardItems.UpdateFieldValues = PATCH /board-items/{itemId:guid}/field-values -> UpdateBoardItemFieldValuesCommand (`BoardItems/Commands/UpdateBoardItemFieldValues/UpdateBoardItemFieldValues.cs`) [EV / Idem]; BoardItems.UpdateFieldValue = PATCH /board-items/{itemId:guid}/values/{fieldId:guid} -> UpdateBoardItemFieldValueCommand (`BoardItems/Commands/UpdateBoardItemFieldValue/UpdateBoardItemFieldValue.cs`) [EV / Idem] · FE sheet/page on develop; Table cell editors in PR #204 · Design WM-Field-Editors(-Dem), WM-Field-Sheet · Tests `BulkUpdateBoardItemsTests.cs`, `ItemStatusAndSystemFieldWritesTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; conflict copy names no actor until the API carries it; the Add field menu leaves focus on a button instead of the new input. Evidence: real-stack check 2026-10-31: in the card popup in the browser a Text and a Number field were added and edited in place (PATCH values 200), 'Enter a number' showed for 'abc' with no request, both stayed after reload; stale version answers 412 and keeps the value; the same value changes nothing, commits 609c34dbd, d290bb373.

### UC-WM-D03 Clear a value (explicit clear)
- Capability: D Field values (WM-004)
- Actors and permission: Editor.
- Preconditions: Item active; the field has a value.
- Input: Explicit clear (never an absent key).
- Output: Clear has one null semantic; a date absent from a request means unchanged (D29).
- Main flow: 1. Choose Clear on the field or press Delete. 2. The field becomes empty.
- No-op / rejection / concurrency / retry: Clearing an empty field is a no-op [S5].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 27.3, 31; WM-044; D29; WMREQ037, WMREQ039
- Acceptance: Given a due date, when Clear due is used, then the date is empty and the version bumps once. Given an already empty field, then nothing changes.
- Evidence: BE BoardItems.Update = PATCH /board-items/{itemId:guid} -> UpdateBoardItemCommand (`BoardItems/Commands/UpdateBoardItem/UpdateBoardItem.cs`) [EV / -]; BoardItems.ClearFieldValue = DELETE /board-items/{itemId:guid}/values/{fieldId:guid} -> ClearFieldValueCommand (`BoardItems/Commands/ClearFieldValue/ClearFieldValue.cs`) [EV / Idem] · FE ClearFieldValue unused by UI · Design WM-Field-Editors (clear) · Tests `BoardAuthorizationHardeningIntegrationTests.cs`, `ItemStatusAndSystemFieldWritesTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Evidence: real-stack check 2026-10-31: clearing in the popup removed the row and it stayed gone after reload; clearing through the API twice changed nothing the second time, a stale clear answers 412, commits 609c34dbd, d290bb373.

### UC-WM-D04 Set start and due dates
- Capability: D Field values (WM-004)
- Actors and permission: Editor.
- Preconditions: Item active.
- Input: Start and due as days; clearDueDate or clearStartDate to clear (D29).
- Output: dates stored as day values; overdue/today chips; Calendar/Timeline read them (UC-WM-K01, G09).
- Main flow: 1. Pick dates in the cell, sheet or quick picker. 2. Save. 3. Overdue marks follow the due day.
- No-op / rejection / concurrency / retry: Due before start is refused inline. Same dates = no-op [S5]. Stale version = conflict [S4].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 146, 138; WM-044; D29; WMREQ021, WMREQ110-113
- Acceptance: (1) Given due before start, an invalid date, a number out of range, or a non-URL Link, when submitting, then the inline error appears under that input, focus goes to the first invalid input, and nothing is sent. (2) Given a card, when it renders, then it shows key (or "#n"), title (max 2 lines, full title in tooltip and accessible name), up to 2 labels and 2 avatars then "+N", due chip with overdue and today states, priority glyph, and counts for checklist, comments and files, each count 99+ above 99 (D39).
- Evidence: BE BoardItems.Update = PATCH /board-items/{itemId:guid} -> UpdateBoardItemCommand (`BoardItems/Commands/UpdateBoardItem/UpdateBoardItem.cs`) [EV / -]; BoardItems.SetDueDate = PATCH /board-items/{itemId:guid}/due-date -> SetBoardItemDueDateCommand (`BoardItems/Commands/SetBoardItemDueDate/SetBoardItemDueDate.cs`) [- / Idem] · FE ItemDatePicker, `frontend/packages/product/work-management/web/src/components/views/calendar/calendar-dates-dialog.tsx` · Design WM-Field-Editors (date), Final-Calendar · Tests `BoardAuthorizationHardeningIntegrationTests.cs`, `PipelineTelemetryIntegrationTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Evidence: real-stack check 2026-10-31: start and due date set with the expected version, an inverted range refused (400), the same dates changed nothing, a stale version answers 412, clearing the due date works, commits 609c34dbd, d290bb373.

### UC-WM-D05 Change an item's status
- Capability: D Field values (WM-004)
- Actors and permission: Editor and above.
- Preconditions: Item active; the status option exists.
- Input: Status option id, expected version.
- Output: Status is an option id. Every status write path behaves the same (WM-042). A Kanban drag writes the grouping field (UC-WM-J02).
- Main flow: 1. Pick a status in the cell, sheet or card. 2. A done-category status completes the item; leaving it reopens.
- No-op / rejection / concurrency / retry: Same status = no-op [S5]. Stale version = conflict [S4]. Parent progress recomputes (UC-WM-D09).
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 148, 148.1; WM-038, WM-042; WMREQ110-113
- Acceptance: Given a done-category status chosen, when saved, then completion time is set. Given the item reopened, then it is cleared. Given a stale version, then the write is refused and nothing changes.
- Evidence: BE BoardItems.Move = POST /board-items/{itemId:guid}/move -> MoveBoardItemCommand (`BoardItems/Commands/MoveBoardItem/MoveBoardItem.cs`) [EV / Idem]; BoardItems.Complete = POST /board-items/{itemId:guid}/complete -> CompleteBoardItemCommand (`BoardItems/Commands/CompleteBoardItem/CompleteBoardItem.cs`) [- / Idem]; BoardItems.UpdateStatus = PATCH /board-items/{itemId:guid}/status -> UpdateBoardItemStatusCommand (`BoardItems/Commands/UpdateBoardItemStatus/UpdateBoardItemStatus.cs`) [- / Idem] · FE `frontend/packages/product/work-management/web/src/components/views/table/table-status-picker.tsx`, useUpdateItemStatus (optimistic, rollback) · Design Final-Table/BC-Kanban status picker · Tests `BulkUpdateBoardItemsTests.cs`, `CreateCommentTargetPolicyMatrixTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Evidence: real-stack check 2026-10-31: status change set and cleared completion, a status of another board refused, the same status changed nothing, a stale version answers 412, commits 609c34dbd, d290bb373.

### UC-WM-D06 Set priority
- Capability: D Field values (WM-004)
- Actors and permission: Editor.
- Preconditions: Item active; Priority field present.
- Input: Priority option id, expected version.
- Output: Priority value (specialized select); glyph on card.
- Main flow: 1. Pick a priority. 2. The card shows the glyph.
- No-op / rejection / concurrency / retry: Same option = no-op [S5]. Stale version = conflict [S4].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 147; WM-038; D43; WMREQ110-113
- Acceptance: Given a priority set, when saved, then the card glyph and the text change for everyone. Given the same option chosen again, then the version does not change.
- Evidence: BE BoardItems.UpdateFieldValue = PATCH /board-items/{itemId:guid}/values/{fieldId:guid} -> UpdateBoardItemFieldValueCommand (`BoardItems/Commands/UpdateBoardItemFieldValue/UpdateBoardItemFieldValue.cs`) [EV / Idem] · FE `frontend/packages/product/work-management/web/src/components/shared/item-priority.ts`, `frontend/packages/product/work-management/web/src/components/shared/priority-glyph.tsx` · Design WM-Card-Face (priority glyph) · Tests `BulkUpdateBoardItemsTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; design for the picker. Evidence: real-stack check 2026-10-31: priority set as a Select option through the API; an option of another field refused, commits 609c34dbd, d290bb373.

### UC-WM-D07 Read a formula value (valid, stale, error)
- Capability: D Field values (WM-004)
- Actors and permission: any viewer.
- Preconditions: A Formula or Rollup field exists.
- Input: Item or board read.
- Output: Valid, stale or error; never the previous value shown as current; per-read evaluation is accepted until a board at the limit misses the performance budget (D78.8).
- Main flow: 1. The read evaluates computed fields. 2. The cell shows the value, a stale marker or an error with the reason.
- No-op / rejection / concurrency / retry: A cycle or division error shows an error state, never a stale number. Read only.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1])
- Rules: product sections 69-73, 149; WM-022; D78.8; WMREQ147
- Acceptance: Given a formula that divides by zero, then the cell shows an error with the reason. Given a stale value, then it is marked and never shown as current.
- Evidence: BE evaluated per read; no stale state · FE read-only fx value · Design WM-Field-Computed; stale/recomputing missing · Tests none found · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; stale and error states in the popup. Evidence: real-stack check 2026-10-31: formula field computed [n] * 2 = 6 on the item, a bad expression refused (400), a write to a formula field refused (400), commits 609c34dbd, d290bb373.

### UC-WM-D08 Rollup and relation authorization
- Capability: D Field values (WM-004)
- Actors and permission: any viewer.
- Preconditions: A rollup or relation reads items the viewer may not read.
- Input: Item read.
- Output: A derived value never leaks data from a target the viewer cannot read.
- Main flow: 1. The server evaluates the rollup over the viewer's readable children. 2. Unreadable children contribute nothing and show nothing.
- No-op / rejection / concurrency / retry: Read only. Unreadable children are masked, never raised as an error that reveals them.
- UI states: success (masked value shows nothing of the unreadable child)
- Rules: product sections 76, 66; WM-023, WM-030; WMREQ148
- Acceptance: Given a sub-item the viewer cannot read, then the rollup excludes it and no title or value of it appears.
- Evidence: BE masking of unreadable children unproven · FE not applicable · Design not applicable (no screen) · Tests none found · Real e2e none
- State: INTEGRATING — masking of unreadable children is not proven by a test; the rollup is evaluated in BoardItemFieldProjection.

### UC-WM-D09 Show derived progress of an item with child Items
- Capability: D Field values (WM-004)
- Actors and permission: any viewer.
- Preconditions: A parent item with child items.
- Input: Item read.
- Output: Progress is the share of done sub-items; else the share of checked checklist items; else 0 or 100 from the status category (D76.10). Read-only.
- Main flow: 1. The read counts done-category non-archived children. 2. The item shows 'done/total' and a percent.
- No-op / rejection / concurrency / retry: Read only. Archived children are excluded. A parent with no children shows no bar.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1])
- Rules: product sections 77, 77.1; WM-041; D76.10
- Acceptance: Given 5 sub-items with 3 done, then progress reads 3/5. Given no sub-items and a checklist at 2 of 4, then it reads 50 percent.
- Evidence: BE derived in item DTO (BoardItemReadValues.cs); D76.10 fallbacks unproven · FE group/field progress copy · Design WM-Card-Face (count); progress bar missing · Tests none found · Real e2e `sub-items.real.e2e.spec.ts` (count only)
- State: HARDENING — real e2e spec, tests, design measurement and owner check; checklist share of the progress (D76.10 precedence) is not derived. Evidence: real-stack check 2026-10-31: a parent's progress counted a completed sub-item (1 of 2) and fell back to 0 of 2 on reopen, commits 609c34dbd, d290bb373.

### UC-WM-D10 Keep one progress truth
- Capability: D Field values (WM-004)
- Actors and permission: product rule.
- Preconditions: An item has more than one possible progress source.
- Input: None.
- Output: One progress truth; a checklist count shows as a checklist count and never competes with parent progress.
- Main flow: 1. The server applies one precedence. 2. All surfaces show the same number.
- No-op / rejection / concurrency / retry: Derived and read only; no write path exists.
- UI states: none: system behaviour with no screen of its own
- Rules: product sections 77; D76.10
- Acceptance: Given a parent with sub-items and a checklist, then the parent shows the sub-item progress and the checklist shows its own count.
- Evidence: BE no single progress source · FE none · Design not applicable (no screen) · Tests none found · Real e2e none
- State: INTEGRATING — implement the D76.10 precedence on the server; test.

### UC-WM-D11 Add or remove labels on an item
- Capability: D Field values (WM-004)
- Actors and permission: Editor and above; picker searches and windows with 200 labels; may create a label from the picker.
- Preconditions: Item active; actor may update items.
- Input: Label ids.
- Output: Up to 2 labels on the card and '+N'. Bulk label is deferred (Q8).
- Main flow: 1. Open the label picker. 2. Pick or create a label. 3. Save.
- No-op / rejection / concurrency / retry: Same label set = no-op [S5]. Below Editor = 403 [S2]. Stale version = conflict [S4]. A label of another board is refused.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 78-79; WM-024; D61; WMREQ023
- Acceptance: Given 200 labels, then the picker searches and windows. Given a typed text that matches nothing, then 'Create label' is offered.
- Evidence: BE BoardItems.AddLabel = POST /board-items/{itemId:guid}/labels -> AddLabelToBoardItemCommand (`Labels/Commands/AddLabelToBoardItem/AddLabelToBoardItem.cs`) [- / Idem]; BoardItems.RemoveLabel = DELETE /board-items/{itemId:guid}/labels/{labelId:guid} -> RemoveLabelFromBoardItemCommand (`Labels/Commands/RemoveLabelFromBoardItem/RemoveLabelFromBoardItem.cs`) [- / Idem]; Labels.List = GET /boards/{boardId:guid}/labels -> GetLabelsQuery (`Labels/Queries/GetLabels/GetLabels.cs`) [- / -] · FE `frontend/packages/product/work-management/web/src/components/card-detail/item-labels-popover.tsx` · Design WM-Labels, WM-Sheet-Create, BC-Create-320-Pickers · Tests `AddLabelToBoardItemTests.cs`, `GetLabelsTests.cs`, `NotrelixApiFactory.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Evidence: real-stack check 2026-10-31: label added, added again (no change), removed, and a label of another board answered 404, commits 609c34dbd, d290bb373.

### UC-WM-D12 Handle a client with a stale field or view schema
- Capability: D Field values (WM-004)
- Actors and permission: editor with an old schema.
- Preconditions: An editor holds an old field or view schema.
- Input: A write against a changed schema.
- Output: No invalid value or configuration persists.
- Main flow: 1. The write returns conflict or validation. 2. The client reloads the schema.
- No-op / rejection / concurrency / retry: Stale field or view reference = conflict or validation error, nothing persisted [S4]. The client reloads the schema before retry.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 129, 117; WM-011; WMREQ036, WMREQ040
- Acceptance: Given field F archived, when a stale client writes F, then conflict and no value stored.
- Evidence: BE BoardFields.Update = PATCH /boards/{boardId:guid}/fields/{fieldId:guid} -> UpdateBoardFieldCommand (`BoardFields/Commands/UpdateBoardField/UpdateBoardField.cs`) [EV / Idem] · FE settings conflict reloads (D64) · Design none; needs stale-schema conflict and reload · Tests `BoardChildAccessIntegrationTests.cs` · Real e2e none
- State: INTEGRATING — no test of a stale-field write refusal.

## E. Groups and grouping

Team capability WM-005.

### UC-WM-E01 Create a group
- Capability: E Groups and grouping (WM-005)
- Actors and permission: Editor (groups need CreateItem/UpdateItem class action); Commenter/Viewer cannot.
- Preconditions: Board active.
- Input: Group name, optional position relative to a neighbour.
- Output: A BoardGroup; a structural container, not a status (WM-014). Duplicating a group generates only its own position (WMREQ048).
- Main flow: 1. Use Add group in Table or Kanban. 2. Name it. 3. The group appears at the chosen place. A group can also be duplicated.
- No-op / rejection / concurrency / retry: BoardGroup is structural, not status (WM-014); [S2]; [S3].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 38-39; WM-014; D49; WMREQ044, WMREQ048, WMREQ132, WMREQ140
- Acceptance: Given an Editor, when they add a group, then it appears for everyone and no item changes. Given a Viewer, then Add group is absent.
- Evidence: BE BoardGroups.Create = POST /boards/{boardId:guid}/groups -> CreateBoardGroupCommand (`BoardGroups/Commands/CreateBoardGroup/CreateBoardGroup.cs`) [- / Idem]; BoardGroups.Duplicate = POST /board-groups/{groupId:guid}/duplicate (group duplicate; UI `useDuplicateGroup`) · FE `frontend/packages/product/work-management/web/src/components/views/table/table-add-group.tsx`, `frontend/packages/product/work-management/web/src/components/views/kanban/kanban-add-lane.tsx`, `frontend/packages/product/work-management/web/src/components/board-settings/board-settings-groups.tsx` · Design Final-Settings `nhom-viec`; create flow, Er, P not drawn · Tests `BoardManagementLevelTests.cs` · Real e2e `board-settings.real.e2e.spec.ts` (adds group)
- State: HARDENING — real e2e spec, tests, design measurement and owner check; no design for the create flow. Built: Duplicate in the group menu of board settings; the server copies the group's settings and its active items with their sub-items, skips archived items, refuses past 500 items with a stable code, and names the copy from the client's localised title. Not built: design for the create flow. Evidence: real-stack check 2026-10-31: Add group in the Kanban strip made the group (201) with a confirmation, no item changed; an outsider is refused with the identical NotFound; Duplicate exists in board settings, commit c8fa65246.

### UC-WM-E02 Rename or edit a group
- Capability: E Groups and grouping (WM-005)
- Actors and permission: Editor.
- Preconditions: Board active; group exists.
- Input: New name or settings, expected version.
- Output: Identity is stable; other members see the change by [S6].
- Main flow: 1. Open the group menu. 2. Rename or edit. 3. Save.
- No-op / rejection / concurrency / retry: Same name = no-op [S5]. Stale version = conflict [S4].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 38-39; WM-014; WMREQ110-113
- Acceptance: Given a renamed group, when another member reloads, then they see the new name and the same group id.
- Evidence: BE BoardGroups.Update = PATCH /board-groups/{groupId:guid} -> UpdateBoardGroupCommand (`BoardGroups/Commands/UpdateBoardGroup/UpdateBoardGroup.cs`) [EV / Idem] · FE `frontend/packages/product/work-management/web/src/components/board-settings/board-settings-groups.tsx` (rename, recolor) · Design Final-Settings `nhom-viec` · Tests `BoardChildAccessIntegrationTests.cs` · Real e2e `board-settings.real.e2e.spec.ts` (renames group)
- State: HARDENING — real e2e spec, tests, design measurement and owner check; rename lives in board settings only. Built: the renamed event carries the old title. Not built: design Er/P, rename from a group menu in the views (rename lives in board settings). Evidence: real-stack check 2026-10-31: rename from board settings wrote the group (200); the same name wrote nothing and kept the version (API: version 2 stays 2); a stale version answers 412, commit c8fa65246.

### UC-WM-E03 Archive or delete a group
- Capability: E Groups and grouping (WM-005)
- Actors and permission: Editor.
- Preconditions: Board active; group exists.
- Input: Group id; the group that its items move to when it holds items.
- Output: Items are never silently dropped (D76.6). A group deleted while a create dialog is open shows 'no longer available' and blocks submit.
- Main flow: 1. Open the group menu, Delete. 2. If items exist, pick the group they move to. 3. Confirm.
- No-op / rejection / concurrency / retry: Without a target group a non-empty group cannot be deleted. Stale version = conflict [S4].
- UI states: destructive confirm (names the impact)
- Rules: product sections 125, 38; WM-014, WM-033; D76.6
- Acceptance: Given a group with 3 items, when delete is attempted without a target, then it is refused. Given a target group, then the 3 items move and keep their keys.
- Evidence: BE BoardGroups.Archive = POST /board-groups/{groupId:guid}/archive -> ArchiveBoardGroupCommand (`BoardGroups/Commands/ArchiveBoardGroup/ArchiveBoardGroup.cs`) [- / Idem]; BoardGroups.Unarchive = POST /board-groups/{groupId:guid}/unarchive -> UnarchiveBoardGroupCommand (`BoardGroups/Commands/UnarchiveBoardGroup/UnarchiveBoardGroup.cs`) [- / Idem]; BoardGroups.Delete = DELETE /board-groups/{groupId:guid} -> DeleteBoardGroupCommand (`BoardGroups/Commands/DeleteBoardGroup/DeleteBoardGroup.cs`) [EV / Idem] · FE delete with item reassignment; create dialog guards a deleted group (`item-create-dialog.tsx`); archive/unarchive not in UI · Design none; needs delete group holding items (move-to picker), archive/unarchive · Tests `ArchiveBoardGroupTests.cs`, `BoardManagementLevelTests.cs` · Real e2e `work-board-management.real.e2e.spec.ts` (delete group with items)
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Built: a create dialog whose group was deleted says it is no longer available, offers the other groups and holds the submit. Built: archive (with a confirm naming the item count) and unarchive carry the group version; an archived group and its items leave every board view (full board, item list, schema), the settings list the archived groups with Unarchive, and an archived group refuses new or moved items with a stable code. Built: an item inside an archived group, and the checklists of such an item, stay readable and take no write: the policy refuses it with `WorkManagement_Group_Archived` through the board-child relation (owner decision 2026-10-30), and the item sheet and page turn the write controls, follow and remind off. Not built: My Work, inbox and report lists still count such items, design. Evidence: real-stack check 2026-10-31: Delete on a group with 3 items asked for the group to move them to and held the button; the delete named expectedVersion and the target, the 3 items kept their keys in the new group; without a target the API answers 400, commit c8fa65246.

### UC-WM-E04 Move an item between groups (Table)
- Capability: E Groups and grouping (WM-005)
- Actors and permission: Editor and above; drag or Alt+Arrow.
- Preconditions: Board active; the item and the target group are on the board.
- Input: Item id, target group, neighbour items.
- Output: Same fact as UC-WM-E06 seen from the group side; status is unchanged, key is unchanged.
- Main flow: 1. Drag the row to another group, or press Alt+Arrow. 2. The group and the ordering key change.
- No-op / rejection / concurrency / retry: Drop onto an empty group is allowed. Stale version = conflict [S4].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 38-41; WM-014, WM-043; WMREQ018-019, WMREQ044
- Acceptance: Given an item dragged to another group, when the board reloads, then the item is in the new group and its status is unchanged.
- Evidence: BE BoardItems.Move = POST /board-items/{itemId:guid}/move -> MoveBoardItemCommand (`BoardItems/Commands/MoveBoardItem/MoveBoardItem.cs`) [EV / Idem] · FE useMoveRow, table row reorder · Design BC-Table (drop onto empty group) · Tests `CreateCommentTargetPolicyMatrixTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; drag in the Table, design. Evidence: real-stack check 2026-10-31: moved through the API with the expected version: to another group (group changed), the same place changed nothing, a stale version answers 412, a group of another board 404, an archived group refused (400); real pointer drag in the browser (headless Chromium, mouse down, move, up) 2026-10-31: a card dropped on an empty lane wrote its status (version 2 to 3) and showed in the lane, a card dropped between two cards of another lane wrote the lane value, Hoàn tác put it back (built with this check: the toast shows after the server kept the move, never for a refused one), and a drag from a stale view answered 412, the card returned and the database kept the other person's value; the stale-drag toast names no actor and still says the old lane (S4 not met); swimlane drops, Alt+Arrow and the WIP warning not driven; commits bc1e10a10 and the Kanban undo commit that follows it.

### UC-WM-E05 Collapse or expand a group (personal)
- Capability: E Groups and grouping (WM-005)
- Actors and permission: any role (personal, client-side per workspace + view, D38).
- Preconditions: Any viewer.
- Input: Collapse or expand toggle.
- Output: Personal and client-side, per workspace and view; nothing is written to the server (D38).
- Main flow: 1. Click the group chevron. 2. The group collapses. 3. The state is remembered on this device.
- No-op / rejection / concurrency / retry: Client-only; no server rule. A collapsed group stays collapsed after a refetch.
- UI states: success (collapsed and expanded), empty group
- Rules: product sections 38; D38
- Acceptance: Given a collapsed group, when a refetch happens, then it stays collapsed. Given another device, then the group is expanded.
- Evidence: BE not applicable (client-side, 38) · FE `frontend/packages/product/work-management/web/src/components/views/kanban/use-kanban-collapsed-lanes.ts` (Kanban lanes and Table groups) · Design BC-Table collapsed, Kanban-Card-States · Tests none found · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Built: the Table keeps its collapsed groups per workspace and board on this device, as the Kanban does for lanes. Evidence: real-stack check 2026-10-31: collapsing a Table group survived a reload and showed expanded in a fresh browser context (per device), commit c8fa65246.

### UC-WM-E06 Move an item between groups (Table)
- Capability: E Groups and grouping (WM-005)
- Actors and permission: Editor and above; drag or Alt+Arrow.
- Preconditions: Board active; item and target group on the board.
- Input: Item id, target group, neighbour items, expected version.
- Output: Server-owned ordering key; one drag mutates one ordered entity.
- Main flow: 1. Drag the row, or press Alt+Arrow. 2. Group and ordering key change; status and key do not.
- No-op / rejection / concurrency / retry: Collision returns a stable conflict. Drop on an empty group is allowed. Stale version = conflict [S4].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 38-41, 42-43; WM-014, WM-016; WMREQ018-019, WMREQ042-044, WMREQ131, WMREQ139
- Acceptance: Given two users drop different items into the same position, then both persist with distinct keys.
- Evidence: BE BoardItems.Move = POST /board-items/{itemId:guid}/move -> MoveBoardItemCommand (`BoardItems/Commands/MoveBoardItem/MoveBoardItem.cs`) [EV / Idem] · FE useMoveRow, fractional-index · Design BC-Table · Tests `CreateCommentTargetPolicyMatrixTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; two-user same-position test. Evidence: real-stack check 2026-10-31: moved through the API with the expected version: to another group (group changed), the same place changed nothing, a stale version answers 412, a group of another board 404, an archived group refused (400); pointer drag in the browser: a drop on an empty lane shows the target and keeps the move, Hoàn tác puts it back, a stale drop answers 412 with a toast that names who moved the card and to which lane (the item reads carry who last edited it), commit 2eed7b832.

## F. Ordering

Team capability WM-006.

### UC-WM-F01 Reorder fields / columns
- Capability: F Ordering (WM-006)
- Actors and permission: Editor for the shared order; anyone for personal column order.
- Preconditions: Board active; field exists.
- Input: Field id and the neighbour fields (previous or next).
- Output: Shared field order uses canonical sortable keys. Personal column order is BoardPreference (Q5).
- Main flow: 1. Open the Fields list or the column menu. 2. Drag, or press Alt+Arrow, to move the field. 3. The server stores the new order.
- No-op / rejection / concurrency / retry: Moving to the current position is a no-op [S5]. An ordering collision returns a stable conflict, not a retry in the handler.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 42-45, 130; WM-016; WMREQ041-045, WMREQ114, WMREQ132, WMREQ141
- Acceptance: Given two Editors reorder at once, then the final order is deterministic and no duplicate keys exist.
- Evidence: BE BoardFields.Move = POST /boards/{boardId:guid}/fields/{fieldId:guid}/move -> MoveBoardFieldCommand (`BoardFields/Commands/MoveBoardField/MoveBoardField.cs`) [EV / Idem] · FE settings reorder; Alt+Arrow Cột menu (`table-toolbar.tsx`, personal order in `use-board-table.ts`) · Design WM-Table-Columns; field reorder drag/Er missing · Tests `WorkItemPlacementIntegrationTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; no personal Table column order here (Table unit); the conflict notice names no actor. Built: the field list of board settings moves a field by drag or Alt+Arrow on its handle, names its neighbours in the server's whole field order (locked system fields included), puts the old order back when the write fails, and announces the new place. Not built: personal Table column order (BoardPreference, with the Table unit), design for the Alt+Arrow and conflict states, the conflict notice does not name the actor (the 409 carries none). Evidence: real-stack check 2026-10-31: Alt+Arrow on a field handle moved it and announced the place (Fc moved to position 3 of 4); a pointer drag moved a field to the end; a move above the Status used to answer 409 because the neighbours named were not adjacent on the server, now fixed, commit c8fa65246.

### UC-WM-F02 Reorder groups
- Capability: F Ordering (WM-006)
- Actors and permission: Editor.
- Preconditions: Board active; group exists.
- Input: Group id and neighbour groups.
- Output: Fractional ordering key; rebalance is invisible.
- Main flow: 1. Drag a group header or press Alt+Arrow. 2. The server stores a new ordering key between neighbours.
- No-op / rejection / concurrency / retry: Moving to the current position is a no-op [S5]. A collision returns a stable conflict.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 42-45; WM-016; WMREQ042-044, WMREQ132, WMREQ140
- Acceptance: Given two groups swapped, when the board is read, then the order is the same for everyone. Given a drop on the current position, then the version does not change.
- Evidence: BE BoardGroups.Move = POST /board-groups/{groupId:guid}/move -> MoveBoardGroupCommand (`BoardGroups/Commands/MoveBoardGroup/MoveBoardGroup.cs`) [EV / Idem] · FE useMoveRow, Table menu 'move group up/down' · Design none; needs group reorder (move up/down), conflict · Tests `BoardManagementLevelTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; the conflict notice does not name the actor. Built: the group list of board settings has the same drag and Alt+Arrow move with rollback; an archived group between two visible groups no longer makes every move between them fail, because the server checks the neighbours against the visible groups and keeps the archived group's key. Not built: design for group reorder and conflict, the conflict notice does not name the actor. Evidence: real-stack check 2026-10-31: Alt+ArrowUp on a group handle moved it (POST move 204) and the list followed, commit c8fa65246.

### UC-WM-F03 Reorder an item within its group or lane
- Capability: F Ordering (WM-006)
- Actors and permission: Editor.
- Preconditions: Item active; a group or lane is in view.
- Input: Item id, neighbour items.
- Output: fractional ordering key between neighbours; effective current position = no-op [S5]; rebalance invisible; auto-scroll inside windowed lane.
- Main flow: 1. Drag within the group or lane, or press Alt+Arrow. 2. The server stores a key between the neighbours.
- No-op / rejection / concurrency / retry: Drop on the current position = no-op [S5]. A collision returns a stable conflict. Rebalance is invisible.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 42-45; WM-016; WMREQ041-044, WMREQ050-052, WMREQ114, WMREQ131, WMREQ139
- Acceptance: Given an item dragged one place down, when the board reloads, then the order holds for everyone. Given two people dragging at once, then one gets a conflict and the other order stands.
- Evidence: BE BoardItems.Move = POST /board-items/{itemId:guid}/move -> MoveBoardItemCommand (`BoardItems/Commands/MoveBoardItem/MoveBoardItem.cs`) [EV / Idem]; BoardItems.MoveChild = POST /board-items/{itemId:guid}/move-within-parent -> MoveChildItemCommand (`BoardItems/Commands/MoveChildItem/MoveChildItem.cs`) [EV / Idem] · FE `frontend/packages/product/work-management/web/src/components/views/kanban/kanban-dnd.ts`, useMoveRow · Design BC-Table, BC-Kanban · Tests `CreateCommentTargetPolicyMatrixTests.cs`, `SubItemTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; drag in the browser. Evidence: real-stack check 2026-10-31: reorder through the API: a card placed first took position 0 and the order followed, placing it where it is changed nothing, a sibling of another group answers 409 work.ordering.stale-placement, a stale version answers 412; pointer drag in the browser: a card dropped above another in its lane took the new place, a drop on the place it holds wrote nothing, Alt+ArrowDown moved it one place and a move past the end wrote nothing, commit c8fa65246.

### UC-WM-F04 Resolve an ordering collision
- Capability: F Ordering (WM-006)
- Actors and permission: system.
- Preconditions: Two ordering writes hit the same scope.
- Input: Concurrent drags.
- Output: Client position is never final authority.
- Main flow: 1. The scope lock serializes. 2. A real collision returns a stable conflict.
- No-op / rejection / concurrency / retry: A unique-constraint violation maps to a stable ordering conflict, not a handler retry. Drop on the current position = no-op [S5].
- UI states: conflict (stable ordering conflict, board re-read), pending
- Rules: product sections 130, 43; WM-016; WMREQ042, WMREQ050-052, WMREQ114
- Acceptance: Given 10 concurrent inserts at one position, then 10 distinct keys and a deterministic order.
- Evidence: BE fractional ordering/rebalance (Ordering 15 tests) · FE fractional-index (core) · Design not applicable (no screen) · Tests none found · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; ten-insert test. Evidence: real-stack check 2026-10-31: six parallel creates into one group all answered 200 and took six distinct positions, commit 4d4c7b5fa.

## G. Checklists

Team capability WM-007.

### UC-WM-G01 Add and edit a checklist on an item
- Capability: G Checklists (WM-007)
- Actors and permission: Editor and above; authorization follows the parent Item.
- Preconditions: Item active; actor may update items.
- Input: Checklist title; checklist item titles.
- Output: A checklist is subordinate to the item; authorization follows the parent item; changing a checklist item never changes Status.
- Main flow: 1. Open Checklists. 2. Add a checklist. 3. Add items.
- No-op / rejection / concurrency / retry: Same title = no-op [S5]. Parent version guards competing writes. Archived item refuses.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline; destructive confirm (names the impact)
- Rules: product sections 80-83; WM-025; WMREQ056-059, WMREQ066, WMREQ142
- Acceptance: Given a checklist item added, when the item is read, then the list shows it in order. Given a stale parent version, then the write is refused.
- Evidence: BE Checklists.List = GET /board-items/{itemId:guid}/checklists -> GetChecklistsQuery (`Checklists/Queries/GetChecklists/GetChecklists.cs`) [- / -]; Checklists.Create = POST /board-items/{itemId:guid}/checklists -> CreateChecklistCommand (`Checklists/Commands/CreateChecklist/CreateChecklist.cs`) [- / Idem]; Checklists.Update = PATCH /checklists/{checklistId:guid} -> UpdateChecklistCommand (`Checklists/Commands/UpdateChecklist/UpdateChecklist.cs`) [EV / Idem]; Checklists.Delete = DELETE /checklists/{checklistId:guid} -> DeleteChecklistCommand (`Checklists/Commands/DeleteChecklist/DeleteChecklist.cs`) [- / Idem] · FE `frontend/packages/product/work-management/web/src/components/checklists/item-checklists.tsx`, `frontend/packages/product/work-management/state/src/mutations/checklist-queue.ts` · Design WM-Checklist-Sheet, WM-Checklist-States · Tests `CreateChecklistTests.cs`, `DeleteChecklistTests.cs`, `GetChecklistsTests.cs`, `NotrelixApiFactory.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; convert-to-sub-item (D69) proof; focus lands on a button, not the new item input, after creating a checklist. Evidence: real-stack check 2026-10-31: checklist created, renamed and deleted through the API and in the card popup in the browser (it stayed after reload); an empty title refused (400), a stale version answers 412 on rename, add item and delete (the delete took no version before), the same title changed nothing, a non-member got 404, a new checklist on an archived item is refused (400); a checklist item can be given a person (403 for someone who cannot see the board) and a due time, commits 8b16f42a3.

### UC-WM-G02 Complete, reorder and delete checklist items
- Capability: G Checklists (WM-007)
- Actors and permission: Editor.
- Preconditions: A checklist with items exists.
- Input: Item id, desired completion state (set, not toggled), neighbours for order.
- Output: Completion is a desired state; a separate toggle command exists (WMREQ062). Order uses canonical keys.
- Main flow: 1. Tick an item. 2. Drag to reorder. 3. Delete from the menu.
- No-op / rejection / concurrency / retry: Setting the same state = no-op [S5]. Competing edits: parent-based version conflict [S4].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 83; WM-025; WMREQ060-062, WMREQ066, WMREQ142
- Acceptance: Given a checklist item set done twice, then the version bumps once. Given a reorder, then the order holds for everyone.
- Evidence: BE Checklists.CreateItemByChecklist = POST /checklists/{checklistId:guid}/items -> CreateChecklistItemCommand (`Checklists/Commands/CreateChecklistItem/CreateChecklistItem.cs`) [EV / Idem]; Checklists.Move = POST /checklists/{checklistId:guid}/move -> MoveChecklistCommand (`Checklists/Commands/MoveChecklist/MoveChecklist.cs`) [EV / Idem]; Checklists.MoveItem = POST /checklist-items/{itemId:guid}/move -> MoveChecklistItemCommand (`Checklists/Commands/MoveChecklistItem/MoveChecklistItem.cs`) [EV / Idem]; Checklists.UpdateItem = PATCH /checklist-items/{itemId:guid} -> UpdateChecklistItemCommand (`Checklists/Commands/UpdateChecklistItem/UpdateChecklistItem.cs`) [EV / Idem]; Checklists.DeleteItem = DELETE /checklist-items/{itemId:guid} -> DeleteChecklistItemCommand (`Checklists/Commands/DeleteChecklistItem/DeleteChecklistItem.cs`) [EV / Idem]; Checklists.ToggleItem = POST /checklist-items/{itemId:guid}/toggle -> ToggleChecklistItemCommand (`Checklists/Commands/ToggleChecklistItem/ToggleChecklistItem.cs`) [EV / Idem]; Checklists.CreateItem = a second endpoint class `Checklists/Commands/CreateChecklistItemEndpoint.cs` that is never mapped (dead code, defect B-13); the live route is Checklists.CreateItemByChecklist · FE `frontend/packages/product/work-management/web/src/components/checklists/item-checklists.tsx` · Design WM-Checklist-States (drag-sort) · Tests `CreateChecklistItemTests.cs`, `DeleteChecklistItemTests.cs`, `NotrelixApiFactory.cs`, `WorkChecklistIntegrationTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; a second unregistered CreateChecklistItem endpoint file is dead code. Evidence: real-stack check 2026-10-31: checklist item toggled, edited, moved first and deleted through the API: a stale version answers 412 on each; the order followed the move, commits 8b16f42a3.

## H. Query, filter and sort foundation

Team capability WM-008.

### UC-WM-H01 Create a view
- Capability: H Query, filter and sort foundation (WM-008)
- Actors and permission: Editor (`CreateBoardView`); Commenter/Viewer cannot.
- Preconditions: Board active; actor may create views.
- Input: View name, type (Table, Kanban, Calendar, Timeline), configuration.
- Output: BoardView with filters, sorts, visible fields, grouping, layout; no copy of items (WM-017); ViewCreated fact; tabs update for all.
- Main flow: 1. Press '+' in the view tabs. 2. Pick a type and fill its settings. 3. Save; the tab appears for everyone.
- No-op / rejection / concurrency / retry: references must resolve to compatible active fields (WM-018); Calendar needs a Date/DateTime field and never infers dates from text; Timeline needs start/end, invalid ranges rejected; not every field is groupable.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 46-49, 61; WM-017, WM-018; WMREQ153; views pack section 4, 11
- Acceptance: Given a Calendar view with a Text field chosen, then rejected. Given a new Kanban view, then no items are duplicated and counts equal the board.
- Evidence: BE BoardViews.Save = PUT /boards/{boardId:guid}/views -> SaveBoardViewCommand (`BoardViews/Commands/SaveBoardView/SaveBoardView.cs`) [- / Idem]; BoardViews.Create = POST /boards/{boardId:guid}/views -> CreateBoardViewCommand (`BoardViews/Commands/CreateBoardView/CreateBoardView.cs`) [- / Idem] · FE AddViewMenu, useSavedViews (Table/Kanban/Calendar/Timeline only) · Design none; needs create/rename/duplicate/delete shared view, tab menu, Er · Tests `BoardAuthorizationHardeningIntegrationTests.cs`, `CreateBoardViewTests.cs` · Real e2e `saved-views.real.e2e.spec.ts`
- State: HARDENING — design for view create, edit and delete; proof that Calendar needs a date field.

### UC-WM-H02 Edit a shared view's configuration
- Capability: H Query, filter and sort foundation (WM-008)
- Actors and permission: Editor (Save to shared view is an Editor change). Viewer/Commenter disabled with reason.
- Preconditions: Shared view exists; actor may edit views.
- Input: Configuration (filters, sorts, grouping, visible fields), expected version.
- Output: writes the shared BoardView; every member sees new defaults unless personal state overrides; ViewChanged fact.
- Main flow: 1. Change settings in the toolbar. 2. Choose 'Lưu vào view chung'. 3. Members see the new defaults unless personal state overrides.
- No-op / rejection / concurrency / retry: A reference to a deleted field is refused or repaired and reported. Two editors: the second gets a conflict [S4]. Network failure reverts with 'Thử lại'.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 47-49, 150; WM-017, WM-018; WMREQ110-113; views pack section 11
- Acceptance: (1) Given an Editor, when they change the shared sort, group or columns, then every other member sees the new defaults (unless their personal state overrides) without reload. (2) Given the Cột menu, when any person (including a Viewer) hides, reorders (Alt+Arrow) or resizes a column, then it is saved as their own BoardPreference, persists across reload, and other members' columns are unchanged; an Editor's "Lưu vào view chung" writes it as the shared default; a Viewer's "Lưu vào view chung" is disabled with the reason (owner Q5).
- Evidence: BE BoardViews.Save = PUT /boards/{boardId:guid}/views -> SaveBoardViewCommand (`BoardViews/Commands/SaveBoardView/SaveBoardView.cs`) [- / Idem]; BoardViews.UpdateConfig = PATCH /boards/{boardId:guid}/views/{viewId:guid} -> UpdateBoardViewConfigCommand (`BoardViews/Commands/UpdateBoardViewConfig/UpdateBoardViewConfig.cs`) [- / Idem]; BoardViews.Rename = PATCH /boards/{boardId:guid}/views/{viewId:guid}/rename -> RenameBoardViewCommand (`BoardViews/Commands/RenameBoardView/RenameBoardView.cs`) [- / Idem] · FE `frontend/packages/product/work-management/web/src/components/board-settings/board-settings-view-panel.tsx` · Design BC-Toolbar (Lưu vào view chung), BC-View-Personal · Tests `BoardAuthorizationHardeningIntegrationTests.cs`, `BoardViewVisibilityTests.cs`, `NotrelixApiFactory.cs` · Real e2e `saved-views.real.e2e.spec.ts` (rename)
- State: HARDENING — tests and owner check. Built: rename, config change and visibility of a saved view carry the view version (a stale one changes nothing, 409); the web client names the version it last read and takes the new one from the config response. Not built: the PUT upsert of the default view (legacy path, no version), design for the conflict state.

### UC-WM-H03 Delete or archive a view
- Capability: H Query, filter and sort foundation (WM-008)
- Actors and permission: Editor.
- Preconditions: View exists; actor may edit views.
- Input: View id; when it is the default, the next default.
- Output: Only the view configuration is removed; items stay. The person on that view moves to the default with a notice.
- Main flow: 1. Open the tab menu, Delete. 2. Pick the next default when needed. 3. Confirm.
- No-op / rejection / concurrency / retry: Below Editor = 403 [S2]. Deleting the only view is refused. Deleting a deleted view is a no-op [S5]. Stale version = conflict [S4].
- UI states: destructive confirm (names the impact)
- Rules: product sections 62, 125; WM-004, WM-017; D76.9
- Acceptance: Given the default view deleted, then another becomes default deterministically and no item is deleted.
- Evidence: BE BoardViews.Delete = DELETE /boards/{boardId:guid}/views/{viewId:guid} -> DeleteBoardViewCommand (`BoardViews/Commands/DeleteBoardView/DeleteBoardView.cs`) [- / Idem]; BoardViews.Archive = POST /boards/{boardId:guid}/views/{viewId:guid}/archive -> ArchiveBoardViewCommand (`BoardViews/Commands/ArchiveBoardView/ArchiveBoardView.cs`) [- / Idem]; BoardViews.Unarchive = POST /boards/{boardId:guid}/views/{viewId:guid}/unarchive -> UnarchiveBoardViewCommand (`BoardViews/Commands/UnarchiveBoardView/UnarchiveBoardView.cs`) [- / Idem]; BoardViews.Restore = POST /boards/{boardId:guid}/views/{viewId:guid}/restore -> RestoreBoardViewCommand (`BoardViews/Commands/RestoreBoardView/RestoreBoardView.cs`) [- / Idem] · FE delete; archive/restore unused · Design none; needs delete view, default-fallback, archive · Tests `DeleteBoardViewTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; archive and restore of a view not driven in the UI; tests and owner check. Built: delete, archive, unarchive and restore of a view carry the view version; deleting or archiving the default view hands the default to the view the person picks, else to the oldest shared view, in the same commit; board settings ask for the next default before deleting it and keep the only shared view undeletable. Not built: archive and restore in the web UI, design. Evidence: real-stack check 2026-10-31: deleted a shared view (204, a stale version 412, an Observer 403, deleting again 404); the default hands over to the view the person picks or the oldest shared one, commit 2ab39238c.

### UC-WM-H04 Set the default view
- Capability: H Query, filter and sort foundation (WM-008)
- Actors and permission: Editor.
- Preconditions: Two or more views, or one.
- Input: View id.
- Output: Exactly one default view per board; deleting it asks for the next (D76.9).
- Main flow: 1. Open the tab menu, Set as default. 2. The old default is cleared.
- No-op / rejection / concurrency / retry: Setting the current default is a no-op [S5]. Below Editor = 403 [S2].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 154; D76.9
- Acceptance: Given a new default set, then exactly one view is default.
- Evidence: BE BoardViews.SetDefault = POST /boards/{boardId:guid}/views/{viewId:guid}/set-default -> SetDefaultViewCommand (`BoardViews/Commands/SetDefaultView/SetDefaultView.cs`) [- / Idem] · FE `frontend/packages/product/work-management/web/src/components/board-settings/board-settings-view-panel.tsx` · Design none; needs pick next default when deleting default view · Tests `BoardManagementLevelTests.cs` · Real e2e `saved-views.real.e2e.spec.ts`
- State: HARDENING — real e2e spec, tests, design measurement and owner check; design for the next-default picker; tests and owner check. Built: set-default carries the view version and clears the old default in the same commit; the delete flow above keeps exactly one default. Not built: design for the next-default picker. Evidence: real-stack check 2026-10-31: set the default view (it answered 500 because the command named the board while its expected version binds the view, fixed); exactly one default after the write, setting the default again bumps nothing, a stale version 412, an Observer 403, commit 2ab39238c.

### UC-WM-H05 Create and use a private view
- Capability: H Query, filter and sort foundation (WM-008)
- Actors and permission: the owner subject.
- Preconditions: Actor may create views.
- Input: View configuration with visibility private.
- Output: Visible only to its creator; never listed or broadcast to others (D77.4).
- Main flow: 1. Create a view and set it private. 2. Only the creator sees it.
- No-op / rejection / concurrency / retry: Another member's list read omits it; realtime never carries it.
- UI states: success (private badge), validation (inline), denied (control disabled with reason), read-only (archived), pending, offline
- Rules: product sections 61, 153; D77.4; D71
- Acceptance: Given user A's private view, when B lists views or listens to realtime, then it is absent.
- Evidence: BE BoardViews.SetVisibility = PATCH /boards/{boardId:guid}/views/{viewId:guid}/visibility -> SetBoardViewVisibilityCommand (`BoardViews/Commands/SetBoardViewVisibility/SetBoardViewVisibility.cs`) [- / Idem] · FE private visibility in `frontend/packages/product/work-management/web/src/components/board-settings/board-settings-view-panel.tsx` · Design none; needs private badge, visibility switch · Tests `BoardViewVisibilityTests.cs` · Real e2e `saved-views.real.e2e.spec.ts` (keeps view private)
- State: HARDENING — tests and owner check. Built: the visibility write carries the view version; a private view cannot be the default (rule code already present). Not built: private badge and switch design, realtime absence proof.

### UC-WM-H06 Filter the view (personal state)
- Capability: H Query, filter and sort foundation (WM-008)
- Actors and permission: any role including Viewer (personal, BoardPreference, D71).
- Preconditions: Any viewer.
- Input: Filter conditions (field, operator, value).
- Output: applied at once, saved per user, board and view; bar shows "Cách xem riêng · Về view chung"; other members unaffected and shared view not written.
- Main flow: 1. Open the filter bar. 2. Add conditions. 3. Results update at once; state saves per user, board and view.
- No-op / rejection / concurrency / retry: An incomplete row is not applied. A condition on a deleted field is ignored with a hint. No result shows 'no results' with 'Xóa bộ lọc'. A save failure keeps the state for the session with 'Thử lại'.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1]); validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 150, 153; D68, D71; views pack section 4, 11
- Acceptance: (1) Given an incomplete condition row, when the view shows results, then the row stays visible and is not applied. (2) Given two Members, when A sets a personal filter, then B's view is unchanged and the shared BoardView is not written. (3) Given saving personal state fails, when the failure returns, then the state stays applied for the session and an alert with "Thử lại" shows.
- Evidence: BE BoardPreferences.Get = GET /boards/{boardId:guid}/preferences/{viewId:guid} -> GetBoardPreferenceQuery (`BoardPreferences/Queries/GetBoardPreference/GetBoardPreference.cs`) [- / -]; BoardPreferences.CreateOrUpdate = PUT /boards/{boardId:guid}/preferences/{viewId:guid} -> CreateOrUpdateBoardPreferenceCommand (`BoardPreferences/Commands/CreateOrUpdateBoardPreference/CreateOrUpdateBoardPreference.cs`) [- / -]; BoardPreferences.UpdateFilters = PATCH /boards/{boardId:guid}/preferences/{viewId:guid}/filters -> UpdateBoardPreferenceFiltersCommand (`BoardPreferences/Commands/UpdateBoardPreferenceFilters/UpdateBoardPreferenceFilters.cs`) [- / -] · FE `filters/filter-builder.tsx`, `filters/use-view-state.ts`, `filters/view-state-bar.tsx` in the toolbars of `main-table-view.tsx`, `kanban-view.tsx`, `board-calendar-view.tsx` and `board-timeline-view.tsx` · Design WM-Filter-Builder, -States, -320, BC-View-Personal, BC-Filter-Gaps · Tests `BoardPreferenceCommandTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; a condition on a deleted field is not shown with a hint; the typed rule writes were removed (they always answered 500 and nothing called them; the personal state is the criteria JSON); tests and owner check. Built: the compound filter model, the member's personal view state (filter, three sort levels, group, swimlane field) kept as versioned criteria on the board preference (server stores an opaque JSON object up to 8 KB), the preference read and write in the web client, the filter builder, sort and group menus and the view state bar with a state hook and mock handlers, and the bar in the Table, Kanban, Calendar and Timeline toolbars in place of the older filter menu. Not built: server-side filter on paged boards (Q9). Evidence: real-stack check 2026-10-31: added a condition in the Filter dialog: the count updates as typed and the view filters, the personal state is written per person (PUT preference) and survives the reload, another member's view is unaffected; no match shows No items match the filter with Clear filter; the system Title is no longer offered twice, commit 2ab39238c.

### UC-WM-H07 Sort the view
- Capability: H Query, filter and sort foundation (WM-008)
- Actors and permission: any role.
- Preconditions: Any viewer.
- Input: Up to 3 sort levels, each a sortable field and direction.
- Output: Empty values go last ascending and first descending; text uses collation; select uses option order.
- Main flow: 1. Open Sort. 2. Add levels. 3. Rows reorder.
- No-op / rejection / concurrency / retry: A non-sortable field cannot be chosen. More than 3 levels is refused. Personal state; the shared view is not written.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1]); validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 151; WM-016; views pack section 4
- Acceptance: Given a sort on a Number field ascending, then empty values come last. Given 3 levels, then ties on level 1 order by level 2.
- Evidence: BE BoardPreferences.UpdateSorts = PATCH /boards/{boardId:guid}/preferences/{viewId:guid}/sorts -> UpdateBoardPreferenceSortsCommand (`BoardPreferences/Commands/UpdateBoardPreferenceSorts/UpdateBoardPreferenceSorts.cs`) [- / -] · FE `filters/sort-menu.tsx` (3 levels) in the Table and Kanban toolbars · Design WM-Filter-SortGroup · Tests `BoardPreferenceCommandTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; server-side sort; tests and owner check. Built: up to three sort levels in the personal view state and the sort menu in the Table and Kanban toolbars; the Table header marks the first level. Not built: server-side sort. Evidence: real-stack check 2026-10-31: added sort levels in the Sort menu up to three (the fourth is disabled), the state is written to the preference and survives the reload, the shared view is not written, commit 2ab39238c.

### UC-WM-H08 Group the view
- Capability: H Query, filter and sort foundation (WM-008)
- Actors and permission: any role.
- Preconditions: Any viewer.
- Input: Group by a compatible field; date bucket when a date.
- Output: groups by compatible types only; items with a deleted option show in "No value"; totals add up.
- Main flow: 1. Open Group. 2. Pick a field. 3. Sections show with counts; items with no value fall into 'No value'.
- No-op / rejection / concurrency / retry: A non-groupable field cannot be chosen. Personal state; the shared view is not written.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1]); validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 152; D71, Q13; views pack section 4
- Acceptance: Given Group by Assignee on a multi-assignee item, then it shows in each lane and the board total counts it once (Q13).
- Evidence: BE BoardPreferences.UpdateGroup = PATCH /boards/{boardId:guid}/preferences/{viewId:guid}/group -> UpdateBoardPreferenceGroupCommand (`BoardPreferences/Commands/UpdateBoardPreferenceGroup/UpdateBoardPreferenceGroup.cs`) [- / -] · FE `filters/field-ref-menu.tsx` (group), `filters/use-view-state.ts` in the Table and Kanban toolbars · Design WM-Filter-SortGroup, BC-Kanban (No value) · Tests `BoardPreferenceCommandTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; server-side grouping on paged boards (Q9); tests and owner check. Built: group and swimlane field in the personal view state with date buckets, and the group menu in the Table and Kanban toolbars. Not built: server-side grouping on paged boards (Q9). Evidence: real-stack check 2026-10-31: grouped by Assignee from the Group menu, the state survives the reload; a non-groupable field is listed as unavailable with the reason, commit 2ab39238c.

### UC-WM-H09 Save, share, edit and delete a saved filter
- Capability: H Query, filter and sort foundation (WM-008)
- Actors and permission: Editor (`UpdateBoardView`/`CreateBoardView`) creates personal saved filters; share with the Board makes it visible to everyone who can view the Board, editable by whoever holds view-edit (Editor); only the creator changes who sees it. Viewer disabled with reason.
- Preconditions: Board active; actor may create views for sharing.
- Input: Name, filter, sort, group; share flag.
- Output: Personal by default; shared makes it visible to everyone who can view the board and editable by view editors. Delete is soft with restore (D68).
- Main flow: 1. Set a filter. 2. Choose 'Lưu bộ lọc' and name it. 3. Optionally share with the board. 4. Edit, rename or delete from the menu.
- No-op / rejection / concurrency / retry: Below Editor cannot share [S2]. Another member's personal filter is NotFound [S1]. Same content = no-op [S5]. Delete is soft with restore.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline; destructive confirm (names the impact)
- Rules: product sections 150, 153; D68; WMREQ110-113
- Acceptance: Given a personal saved filter, then another member cannot see it and gets NotFound by id. Given a shared one, then every viewer can apply it.
- Evidence: BE SavedFilters.Create = POST /boards/{boardId:guid}/saved-filters -> CreateSavedFilterCommand (`Views/Commands/CreateSavedFilter/CreateSavedFilter.cs`) [- / Idem]; SavedFilters.Rename = PATCH /saved-filters/{filterId:guid}/rename -> RenameSavedFilterCommand (`Views/Commands/RenameSavedFilter/RenameSavedFilter.cs`) [EV / Idem]; SavedFilters.UpdateVisibility = PATCH /saved-filters/{filterId:guid}/visibility -> UpdateSavedFilterVisibilityCommand (`Views/Commands/UpdateSavedFilterVisibility/UpdateSavedFilterVisibility.cs`) [EV / Idem]; SavedFilters.UpdateFilters = PATCH /saved-filters/{filterId:guid}/filters -> UpdateSavedFilterFiltersCommand (`Views/Commands/UpdateSavedFilterFilters/UpdateSavedFilterFilters.cs`) [EV / Idem]; SavedFilters.UpdateSorts = PATCH /saved-filters/{filterId:guid}/sorts -> UpdateSavedFilterSortsCommand (`Views/Commands/UpdateSavedFilterSorts/UpdateSavedFilterSorts.cs`) [EV / Idem]; SavedFilters.UpdateGroup = PATCH /saved-filters/{filterId:guid}/group -> UpdateSavedFilterGroupCommand (`Views/Commands/UpdateSavedFilterGroup/UpdateSavedFilterGroup.cs`) [EV / Idem]; SavedFilters.SoftDelete = DELETE /saved-filters/{filterId:guid} -> DeleteSavedFilterCommand (`Views/Commands/DeleteSavedFilter/DeleteSavedFilter.cs`) [EV / Idem]; SavedFilters.Restore = POST /saved-filters/{filterId:guid}/restore -> RestoreSavedFilterCommand (`Views/Commands/RestoreSavedFilter/RestoreSavedFilter.cs`) [- / Idem] · FE saved-filters-menu/control (11 tests); update ops unused · Design WM-SavedFilter-Menu, -Save, -States · Tests `BoardManagementLevelTests.cs`, `SavedFilterCommandTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; another member's private filter answers 403 instead of NotFound to a viewer-level member (the policy decides before the handler; handed to the access lane); applying and saving from the menu not driven; tests and owner check. Evidence: real-stack check 2026-10-31: created a private and a shared saved filter: a member sees only the shared one; rename (204, same name bumps nothing, a stale version 412), delete and restore (204), an empty name 400; an Observer's rename of another person's private filter answers 403, commit 2ab39238c.

### UC-WM-H10 Apply a saved filter
- Capability: H Query, filter and sort foundation (WM-008)
- Actors and permission: anyone who can see it.
- Preconditions: A saved filter is visible to the person.
- Input: Saved filter id.
- Output: Applying never changes the shared default view.
- Main flow: 1. Open the saved-filters menu. 2. Pick a filter. 3. The bar takes its filter, sort and group.
- No-op / rejection / concurrency / retry: A filter that is not visible to the person is NotFound [S1]. A stale condition is ignored with a hint (UC-WM-H12).
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1]); validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 150, 153; D68, D71
- Acceptance: Given a shared filter applied, then only this person's state changes.
- Evidence: BE SavedFilters.List = GET /boards/{boardId:guid}/saved-filters -> ListSavedFiltersQuery (`Views/Queries/ListSavedFilters/ListSavedFilters.cs`) [- / -] · FE `frontend/packages/product/work-management/web/src/components/saved-filters/saved-filters-menu.tsx` (changed by PR #201) · Design WM-SavedFilter-Menu · Tests `SavedFilterPipelineIntegrationTests.cs` · Real e2e none
- State: INTEGRATING — real e2e; re-check against open PR #201.

### UC-WM-H11 Return to the shared view ("Về view chung")
- Capability: H Query, filter and sort foundation (WM-008)
- Actors and permission: any role.
- Preconditions: Personal view state exists.
- Input: Choose 'Về view chung'.
- Output: personal state cleared on the server; shared view's own sort/group shown; bar shows the shared view's new defaults if it changed meanwhile.
- Main flow: 1. Press the dotted icon in the toolbar. 2. Personal state clears on the server. 3. The shared defaults show.
- No-op / rejection / concurrency / retry: With no personal state it is a no-op [S5]. Never writes the shared view.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 153; D71
- Acceptance: Given personal filter and sort, when 'Về view chung' is pressed, then both clear and the shared view's own sort shows.
- Evidence: BE BoardPreferences.CreateOrUpdate = PUT /boards/{boardId:guid}/preferences/{viewId:guid} -> CreateOrUpdateBoardPreferenceCommand (`BoardPreferences/Commands/CreateOrUpdateBoardPreference/CreateOrUpdateBoardPreference.cs`) [- / -] · FE `filters/view-state-bar.tsx` (Về view chung) in all four view toolbars · Design BC-View-Personal (icon + dot), BC-Toolbar · Tests `BoardPreferenceCommandTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; tests and owner check. Built: the view state bar clears the whole personal state including the saved criteria, in the Table, Kanban, Calendar and Timeline toolbars. Evidence: real-stack check 2026-10-31: Back to shared view clears the personal state on the server (204) and the shared defaults show after the reload; with no personal state the button is absent and nothing is written, commit 2ab39238c.

### UC-WM-H12 Repair stale view references
- Capability: H Query, filter and sort foundation (WM-008)
- Actors and permission: system and editor.
- Preconditions: A view or saved filter references a removed or incompatible field.
- Input: None.
- Output: No silently executable invalid configuration.
- Main flow: 1. The view loads. 2. Stale conditions are ignored with a visible hint. 3. Saving a fixed copy creates a new saved filter.
- No-op / rejection / concurrency / retry: Never rewrites the shared view silently. A hint names the ignored condition.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1]); validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 49, 150; D71; views pack section 11
- Acceptance: Given a filter on a deleted field, then the view loads, the condition is ignored and a hint says so.
- Evidence: BE not applicable (opaque criteria) · FE in open PR #201: stale condition ignored with hint · Design WM-Filter-States (deleted-field chip) · Tests none found · Real e2e none
- State: DEFINED — the stale-condition hint is in open PR #201; real e2e.

### UC-WM-H13 Switch and open views via tabs and URL
- Capability: H Query, filter and sort foundation (WM-008)
- Actors and permission: any viewer.
- Preconditions: Any viewer; several views exist.
- Input: Tab click or URL with ?view= or ?savedView=.
- Output: tabs Table/Kanban/Calendar/Timeline + "+"; URL names the view; Back/Forward follows URL every time; unknown view = default + notice; switching view with a pending personal save: save lands on its own view.
- Main flow: 1. Click a tab, or open a link. 2. The URL names the view. 3. Back and Forward follow the URL.
- No-op / rejection / concurrency / retry: An unknown view shows the default with one notice.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1]); validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 154; WM-018
- Acceptance: Given a URL naming a deleted view, then the default shows with a notice. Given Back, then the previous view returns.
- Evidence: BE BoardViews.Get = GET /boards/{boardId:guid}/views -> GetBoardViewQuery (`BoardViews/Queries/GetBoardView/GetBoardView.cs`) [- / -]; BoardViews.List = GET /boards/{boardId:guid}/views/saved -> ListBoardViewsQuery (`BoardViews/Queries/ListBoardViews/ListBoardViews.cs`) [- / -] · FE ViewTabs, `?view=`, `?savedView=` · Design BC-Toolbar, BC-Shell · Tests `BoardViewVisibilityTests.cs`, `GetBoardViewTests.cs` · Real e2e `saved-views.real.e2e.spec.ts` (partial)
- State: HARDENING — real e2e spec, tests, design measurement and owner check; tests and owner check. Evidence: real-stack check 2026-10-31: tabs write the layout to the address, Back and Forward follow it; an unknown layout in the address no longer fails the page and an unknown saved view opens the default with one notice, commit 2ab39238c.

### UC-WM-H14 Apply workspace time zone to date filters and buckets
- Capability: H Query, filter and sort foundation (WM-008)
- Actors and permission: any member.
- Preconditions: Any member.
- Input: Date filter or bucket.
- Output: date filters and day/week/month groups use the workspace time zone for everyone; members must be able to read it.
- Main flow: 1. Apply a date filter or group. 2. Dates resolve in the workspace time zone for everyone.
- No-op / rejection / concurrency / retry: A member who cannot read the time zone setting uses the workspace default and sees a note. Read only.
- UI states: success (time-zone note on date filters and buckets)
- Rules: product sections 138; D71
- Acceptance: Given a Workspace time zone of UTC+7, when 'today' is filtered, then it uses that zone for all members.
- Evidence: BE workspace time zone setting (Workspaces) · FE tz used by date presets in PR #201; buckets unproven · Design none; needs time-zone note on date filters/buckets · Tests none found · Real e2e `work-board-management.real.e2e.spec.ts` (workspace time zone)
- State: HARDENING — proof of date buckets in the workspace time zone, after open PR #201.

### UC-WM-H15 Make a field filterable, sortable, groupable per its type
- Capability: H Query, filter and sort foundation (WM-008)
- Actors and permission: system.
- Preconditions: Field exists.
- Input: Field type.
- Output: Only the operators defined for the type; groupable types only; sort puts empty last ascending and first descending.
- Main flow: 1. The filter, sort and group menus read the type's registry. 2. Only defined operators and sort orders appear.
- No-op / rejection / concurrency / retry: An operator not defined for the type is refused by the server. Unknown field type fails closed.
- UI states: success (only valid operators shown), validation (invalid operator refused)
- Rules: product sections 35-36, 150-152; WM-013
- Acceptance: Given a Number field, then filter offers numeric operators only. Given a Text field, then it cannot be chosen for a Kanban lane.
- Evidence: BE operators defined in domain; no server-side filter/sort/group endpoint · FE client-side filter/sort/group; PR #201 adds compound model · Design not applicable (no screen) · Tests none found · Real e2e none
- State: INTEGRATING — server-side filter, sort and group with parity to the client for every field type.

### UC-WM-H16 Search items and boards
- Capability: H Query, filter and sort foundation (WM-008)
- Actors and permission: any member; Guest sees only what is shared.
- Preconditions: Any member.
- Input: Query text.
- Output: Own items first; case and mark insensitive; never widens access (D33).
- Main flow: 1. Open command search. 2. Type. 3. Results list items, docs and boards the person may open (caps 6, 5, 5).
- No-op / rejection / concurrency / retry: search cannot skip authorization.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1]); validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 136; WM-030; D33
- Acceptance: Given a board the person cannot see, then it never appears in results.
- Evidence: BE GET /workspaces/{ws}/command-search (CommandSearchQuery, adjacent; 5 tests) · FE `frontend/apps/web/src/shell/workspace/use-command-search.ts`, features-search · Design Final-Command, Final-M-Search · Tests `CommandSearchQueryHandlerTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; the caps (6, 5, 5) were not driven; tests and owner check. Evidence: real-stack check 2026-10-31: command search finds a Private board and its item for its manager and returns nothing for a non-member and a Guest, commit 2ab39238c.

### UC-WM-H17 Filter by text inside a board
- Capability: H Query, filter and sort foundation (WM-008)
- Actors and permission: any role.
- Preconditions: Any viewer; a board is open.
- Input: Text typed in the view search.
- Output: Part of view state; server-side on paged boards.
- Main flow: 1. Type in the board search. 2. The view filters by text. 3. No match shows a 'no results' state.
- No-op / rejection / concurrency / retry: Empty text clears the filter. Read only; no write.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1]); validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 136; D71
- Acceptance: (1) Given a filter that matches nothing, when applied, then a "no results" state with "Xóa bộ lọc" shows, not the empty-board state, and group headers show 0. (2) Given a filter or sort on a paged board, when applied, then it is evaluated on the server, group totals show the filtered total, and the result equals the full-data result on a fixed dataset.
- Evidence: BE BoardItems.List = GET /boards/{boardId:guid}/items -> GetBoardItemsQuery (`BoardItems/Queries/GetBoardItems/GetBoardItemsQuery.cs`) [- / -] · FE useKanbanSearch client-side · Design BC-Toolbar (search), BC-Table filtered-empty · Tests `NotrelixApiFactory.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; BoardItems.List has no text parameter, the search runs on the client; tests and owner check. Evidence: real-stack check 2026-10-31: the board search box filters the table by text as typed, a no-match state names the filter with Clear filter, empty text clears it, commit 2ab39238c.

### UC-WM-H18 List My Work
- Capability: H Query, filter and sort foundation (WM-008)
- Actors and permission: any member.
- Preconditions: Any member.
- Input: None.
- Output: From the server query with the single-read decision; also 'waiting for others' (D35).
- Main flow: 1. Open My Work. 2. See not-done items assigned to me on boards I can view, ordered by due date (D76.17).
- No-op / rejection / concurrency / retry: Items on boards the person cannot view never appear [S1]. Read only.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1]); validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 136; D35, D76.17
- Acceptance: Given an item on a board the person cannot view, then it never appears. Given a done item assigned to me, then My Work does not list it; given open items, then they are ordered by due date.
- Evidence: BE BoardItems.ListMyWork = GET /workspaces/{workspaceId:guid}/my-work/items -> GetMyBoardItemsQuery (`BoardItems/Queries/GetMyBoardItems/GetMyBoardItems.cs`) [- / -]; BoardItems.GetMyWorkExtended = GET /workspaces/{workspaceId:guid}/my-work/extended -> GetMyWorkExtendedQuery (`BoardItems/Queries/GetMyWorkExtended/GetMyWorkExtended.cs`) [- / -] · FE `frontend/apps/web/src/routes/workspaces/$workspaceId/my-work.tsx` · Design Final-MyWork, Final-T-MyWork, Final-M-MyWork · Tests `CollectionVisibilityIntegrationTests.cs`, `MyWorkAndHomeFeedTests.cs` · Real e2e `work-feeds.real.e2e.spec.ts` (smoke)
- State: HARDENING — real e2e spec, tests, design measurement and owner check; Er and L design; tests and owner check. Evidence: real-stack check 2026-10-31: My Work for a member lists the assigned not-done items in due order with undated last, a done item is absent, items of a board the member cannot open never appear, commit 2ab39238c.

### UC-WM-H19 Filter items by label
- Capability: H Query, filter and sort foundation (WM-008)
- Actors and permission: any role (personal view state).
- Preconditions: Any viewer.
- Input: Label operators in the filter builder.
- Output: label operators in the filter builder.
- Main flow: 1. Open the filter bar. 2. Add a Label condition. 3. Items filter.
- No-op / rejection / concurrency / retry: A label of another board matches nothing. Personal state.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1]); validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 150; D71
- Acceptance: Given a label filter 'has any of', then only items with those labels show.
- Evidence: BE no server-side filter · FE in open PR #201: label operators in filter-builder.tsx · Design WM-Filter-Builder · Tests none found · Real e2e none
- State: DEFINED — label operators are in open PR #201, not on develop; no server-side label filter.

## I. Table view

Team capability WM-009.

### UC-WM-I01 Use the Table view
- Capability: I Table view (WM-009)
- Actors and permission: any viewer; editing per role.
- Preconditions: Any viewer; a Table view exists.
- Input: View id; personal column, sort and filter state.
- Output: Schema-driven columns (default columns, each custom field; derived fields are locked in the header). Group sections with counts.
- Main flow: 1. Open the Table tab. 2. Rows show canonical items; columns come from fields. 3. Inline edits run normal item commands.
- No-op / rejection / concurrency / retry: Read and edit follow the role [S2]. An inline edit fails by [S1], [S3] or [S4] and the cell reverts with the reason.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1]); validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 50, 152; views pack section 5; WM-017; WMREQ139
- Acceptance: (1) Given a Viewer or Commenter, when they select any cell, then no editor opens and the header lock explains why. (2) Given a title cell, when or double-click or Enter is pressed, then the title edits in place; single click opens the item (D63). (3) Given a column header, when sorted, then the registry comparator and null rule apply (empty last ascending, first descending) for every field type.
- Evidence: BE Boards.GetOverview = GET /boards/{boardId:guid}/full -> GetFullBoardQuery (`Boards/Queries/GetFullBoard/GetFullBoard.cs`) [- / -]; BoardItems.List = GET /boards/{boardId:guid}/items -> GetBoardItemsQuery (`BoardItems/Queries/GetBoardItems/GetBoardItemsQuery.cs`) [- / -] · FE `table-columns.ts`, `table-field-cell.tsx`, `table-field-editors.tsx` (custom-field columns, in-cell editors), `table-toolbar.tsx` · Design Final-Table, BC-Table, WM-Toolbar-Table · Tests `ComputedFieldsIntegrationTests.cs`, `NotrelixApiFactory.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; virtualization is UC-WM-T01. Built: every visible custom field of a type the table can show is a column with its type's cell editor, a lock in the header for calculated or uneditable types and for a viewer, and sorting by the type's rules. Evidence: real-stack check 2026-10-31: a board Observer sees locked headers that say 'Read only, you can't edit this field', a double click opens the card whose inputs are read-only with 'You can only view this card'; for an editor a double click edits the title in place (one PATCH) and a single click opens the item; the sort menu orders by Points with empty last ascending and first descending, commit 8240660a5.

### UC-WM-I02 Arrange columns (personal) and save to the shared view
- Capability: I Table view (WM-009)
- Actors and permission: hide/order/resize = any role (personal BoardPreference); "Lưu vào view chung" = Editor; Viewer disabled with reason.
- Preconditions: Any viewer for personal state; an editor to save to the view.
- Input: Hide, order and resize columns.
- Output: Personal by default (BoardPreference); other members are unchanged.
- Main flow: 1. Open the Cột menu. 2. Change columns or press Alt+Arrow. 3. Editors choose 'Lưu vào view chung'.
- No-op / rejection / concurrency / retry: Viewer cannot save to the shared view [S2]. Personal changes never write the shared view.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 46, 153; Q5, D73
- Acceptance: Given a hidden column, then only this person loses it. Given a Viewer, then 'Lưu vào view chung' is disabled with the reason.
- Evidence: BE BoardPreferences.Get = GET /boards/{boardId:guid}/preferences/{viewId:guid} -> GetBoardPreferenceQuery (`BoardPreferences/Queries/GetBoardPreference/GetBoardPreference.cs`) [- / -]; BoardPreferences.CreateOrUpdate = PUT /boards/{boardId:guid}/preferences/{viewId:guid} -> CreateOrUpdateBoardPreferenceCommand (`BoardPreferences/Commands/CreateOrUpdateBoardPreference/CreateOrUpdateBoardPreference.cs`) [- / -] · FE Cột menu with Alt+Arrow, "Lưu vào view chung" and "Về cột của view chung" (`table-toolbar.tsx`), personal hide, order and width in `use-board-table.ts` over the preference `columns` (`board-preference.api.ts`) · Design WM-Table-Columns, BC-View-Personal · Tests `BoardPreferenceCommandTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Built: hidden columns, column order and widths are kept per member on the board preference (opaque JSON up to 4 KB, migration `AddBoardPreferenceColumns`) and replace the shared view's for that member alone; an editor's "Lưu vào view chung" writes them to the shared view (with its version) and drops the personal copy only once that write went through, a viewer sees it disabled with the reason; "Về cột của view chung" drops them; a failed save keeps them with "Thử lại". Evidence: real-stack check 2026-10-31: hiding Notes in the Columns menu is personal (the member kept it, a fresh browser context of the admin did not show it); Save to shared view wrote the shared view (POST views); for the Observer the item is disabled with 'Only editors can change the columns of the shared view', commit 8240660a5.

### UC-WM-I03 Select items across views and windows
- Capability: I Table view (WM-009)
- Actors and permission: Editor and above.
- Preconditions: Items visible in Table or Kanban; actor may edit.
- Input: Checkbox, Space, Shift+click range, header checkbox (loaded rows), 'select all in group' capped at 200.
- Output: Selection is client state keyed by item id; Kanban shares the same bar (Q8).
- Main flow: 1. Tick items. 2. The bulk bar shows the count. 3. Selection survives refetch and clears on success.
- No-op / rejection / concurrency / retry: Client-only; no server rule. Items deleted or archived meanwhile drop out of the selection with a count.
- UI states: success (selection count in the bulk bar), validation (cap of 200 stated), read-only (no checkboxes)
- Rules: product sections 34; D32; Q8
- Acceptance: Given 201 items in a group, when select all is pressed, then 200 are selected and the cap is stated. Given a refetch, then the selection holds.
- Evidence: BE not applicable (client selection) · FE Table only; no Kanban selection; no window-spanning selection · Design BC-Bulk, BC-Large · Tests none found · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; the selection kept across a refetch and the over-200 lock were not driven. Evidence: real-stack check 2026-10-31: Select all on a 205-row group picked 200 and said 'At most 200 at a time. 5 more are not selected.'; a selection past 200 locks the actions and names the limit (built, not driven); the Kanban selection now exists (checkbox, Shift+click) and reuses the bar, commit 8240660a5.

## J. Kanban view

Team capability WM-010.

### UC-WM-J01 Use the Kanban view
- Capability: J Kanban view (WM-010)
- Actors and permission: any viewer; edits per role.
- Preconditions: Any viewer; a Kanban view exists.
- Input: View id.
- Output: Kanban is a projection over grouping and ordering; it owns no status data.
- Main flow: 1. Open the Kanban tab. 2. Lanes come from the grouping field. 3. Cards drag per UC-WM-J02.
- No-op / rejection / concurrency / retry: Edits follow the role [S2]. A drop fails by [S1], [S3] or [S4] and the card returns.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1]); validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 51, 40-41; WM-015; views pack section 6
- Acceptance: (1) Given an empty lane, when a card is dragged over it, then a drop target is shown and the drop works. (2) Given a quick add failing, when the write errors, then the typed name stays in the input with an inline error and "Thử lại". (3) Given Kanban, when a Member selects cards (Space, Shift+click, checkbox), then the same bulk bar as the Table appears (owner Q8).
- Evidence: BE BoardItems.List = GET /boards/{boardId:guid}/items -> GetBoardItemsQuery (`BoardItems/Queries/GetBoardItems/GetBoardItemsQuery.cs`) [- / -]; BoardItems.Move = POST /board-items/{itemId:guid}/move -> MoveBoardItemCommand (`BoardItems/Commands/MoveBoardItem/MoveBoardItem.cs`) [EV / Idem] · FE `kanban-view.tsx`, `kanban-selection.ts`, `kanban-bulk-bar.tsx` (the Table's bar over the picked cards), `kanban-card.tsx` (checkbox, Space, Shift+click) · Design BC-Kanban (Final-Board; Kanban-Board proposal, canonical unrecorded) · Tests `CreateCommentTargetPolicyMatrixTests.cs`, `NotrelixApiFactory.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; not built: an undo window for the bulk archive, the card checkbox is on no artboard. Evidence: real-stack check 2026-10-31: drop on an empty lane shows the target and keeps the move; a failed quick add keeps the typed name in the field with an inline error and Retry (retry created it); checkbox plus Shift+click picks cards and the bulk bar (BC-Bulk, dark bar at the bottom centre) set Hoàn thành for both in one request, commit 2eed7b832.

### UC-WM-J02 Move an item between Kanban lanes
- Capability: J Kanban view (WM-010)
- Actors and permission: Editor and above; drag or Alt+Arrow (Up/Down for swimlanes).
- Preconditions: Kanban view with a grouping field; item active.
- Input: Item id, target lane (a grouping field value), neighbour cards, expected version.
- Output: Writes the configured grouping field, not BoardGroup (WM-015).
- Main flow: 1. Drag a card to another lane, or press Alt+Arrow. 2. The grouping field value and ordering change. 3. 'Hoàn tác' stays for a few seconds.
- No-op / rejection / concurrency / retry: A read-only or derived lane field refuses the drop with the reason and the card returns. A WIP limit only warns. Lane removed mid-drag = refused and re-read. Stale version = conflict [S4].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 40-41, 51; WM-015; D36-D40; WMREQ018, WMREQ044, WMREQ131
- Acceptance: (1) Given an empty lane, when a card is dragged over it, then a drop target is shown and the drop works. (2) Given B is dragging a card, when A moves another card, then B's drag completes unaffected and the board re-reads after the drop. (3) Given a lane at its WIP limit, when a card is dropped in, then a warning shows and the drop is allowed; the server does not block (D37).
- Evidence: BE BoardItems.Move = POST /board-items/{itemId:guid}/move -> MoveBoardItemCommand (`BoardItems/Commands/MoveBoardItem/MoveBoardItem.cs`) [EV / Idem] · FE `frontend/packages/product/work-management/web/src/components/views/kanban/kanban-dnd.ts` + keyboard DnD (186 kanban tests) · Design BC-Kanban, Kanban-Card-States, WM-Swimlanes · Tests `CreateCommentTargetPolicyMatrixTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; pointer and keyboard drag in the browser, design measurement, tests. Evidence: real-stack check 2026-10-31: moved through the API with the expected version: to another group (group changed), the same place changed nothing, a stale version answers 412, a group of another board 404, an archived group refused (400); pointer drag in the browser: a drop on an empty lane shows the target and keeps the move, Hoàn tác puts it back, a stale drop answers 412 with a toast that names who moved the card and to which lane (the item reads carry who last edited it), commit 2eed7b832.

### UC-WM-J03 Move an item across swimlanes
- Capability: J Kanban view (WM-010)
- Actors and permission: Editor.
- Preconditions: Kanban view with a swimlane field; item active.
- Input: Item id, target lane and swimlane, expected version.
- Output: A multi-assignee item appears in every assignee lane; the move changes only the source assignee to the target (Q13).
- Main flow: 1. Drag a card into another lane and swimlane. 2. Both fields are written.
- No-op / rejection / concurrency / retry: Read-only lane or swimlane field refuses the drop with the reason. Stale version = conflict [S4].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 40-41; WM-015; D71; Q13
- Acceptance: Given an item in lane A and swimlane X, when dropped in lane B and swimlane Y, then both fields hold the new values. Given a multi-assignee item, then other assignees are unchanged.
- Evidence: BE BoardItems.Move = POST /board-items/{itemId:guid}/move -> MoveBoardItemCommand (`BoardItems/Commands/MoveBoardItem/MoveBoardItem.cs`) [EV / Idem] · FE `kanban-swimlane-board.tsx` (a drop on a cell writes the column and the lane value that changed) · Design WM-Swimlanes · Tests `CreateCommentTargetPolicyMatrixTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; the swimlane header is not sticky when the board scrolls. Evidence: real-stack check 2026-10-31: swimlanes by Assignee: a multi-assignee card shows in each assignee lane, a drop on another lane and column wrote the status and swapped only the source assignee (admin, member kept), the two writes run in turn and the column is put back when the lane write fails; a stale drop answers 412, commit 2eed7b832.

### UC-WM-J04 Use swimlanes
- Capability: J Kanban view (WM-010)
- Actors and permission: any role (personal); drop writes both fields (UC-WM-J03).
- Preconditions: Kanban view.
- Input: Second grouping field.
- Output: Personal state; a drop writes both fields (UC-WM-J03). Lane collapse is personal.
- Main flow: 1. Open the Kanban toolbar. 2. Choose Swimlanes. 3. Lanes split by the second field.
- No-op / rejection / concurrency / retry: Personal state. A drop writes both fields or neither [S4].
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1]); validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 51; D71, D38
- Acceptance: Given swimlanes by Assignee, then each swimlane header is sticky and collapses per person.
- Evidence: BE BoardPreferences.UpdateGroup = PATCH /boards/{boardId:guid}/preferences/{viewId:guid}/group -> UpdateBoardPreferenceGroupCommand (`BoardPreferences/Commands/UpdateBoardPreferenceGroup/UpdateBoardPreferenceGroup.cs`) [- / -] · FE `kanban-swimlane-board.tsx`, the swimlane field in the view bar (personal state, kept on the preference) · Design WM-Swimlanes · Tests `BoardPreferenceCommandTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; the collapsed state is not kept across a reload; 320 snap-scroll design. Evidence: real-stack check 2026-10-31: Lane: Assignee is personal (no view write, Back to shared view offered); each lane header collapses on its own, commit 2eed7b832.

### UC-WM-J05 Choose fields shown on the card
- Capability: J Kanban view (WM-010)
- Actors and permission: Editor.
- Preconditions: Kanban view; actor may edit views.
- Input: Fields to show on the card.
- Output: card face shows the chosen custom fields in type formats.
- Main flow: 1. Open card settings. 2. Tick fields. 3. Cards show them in their type formats.
- No-op / rejection / concurrency / retry: Below Editor cannot save the card fields [S2]. Same set = no-op [S5].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 51; D39
- Acceptance: Given a field ticked, then every card shows it formatted for its type.
- Evidence: BE BoardViews.UpdateConfig = PATCH /boards/{boardId:guid}/views/{viewId:guid} -> UpdateBoardViewConfigCommand (`BoardViews/Commands/UpdateBoardViewConfig/UpdateBoardViewConfig.cs`) [- / Idem] · FE Kanban settings panel (card face columns) · Design WM-Card-Face · Tests `NotrelixApiFactory.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Evidence: real-stack check 2026-10-31: Fields shown on the card: the panel opened without the crash it had, the switch wrote the view config and every card showed Points in its type format (5, or a dash when empty), commit 2eed7b832.

### UC-WM-J06 Set a WIP limit and see the warning
- Capability: J Kanban view (WM-010)
- Actors and permission: editors of the view (UpdateBoardView); anyone sees the warning.
- Preconditions: Kanban view; actor may edit the view.
- Input: Optional per-lane number stored in the Kanban view configuration (D37).
- Output: warning in the view when over the limit; never blocks an Item write (D37); a lane id that no longer names a lane is ignored.
- Main flow: 1. Open the lane menu. 2. Set a limit. 3. Cards over the limit show a warning.
- No-op / rejection / concurrency / retry: Exceeding the limit warns and still allows the drop; it never blocks writes on the server.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 51; D37
- Acceptance: Given a lane limit of 3 holding 3 cards, when a fourth is dropped, then the drop succeeds and the lane shows the warning.
- Evidence: BE BoardViews.UpdateConfig = PATCH /boards/{boardId:guid}/views/{viewId:guid} -> UpdateBoardViewConfigCommand (`BoardViews/Commands/UpdateBoardViewConfig/UpdateBoardViewConfig.cs`) [- / Idem] · FE the lane menu sets the limit (`kanban-column.tsx`, `kanban-view.tsx` writes `wipLimits` to the view config); the lane and the drop target warn · Design Kanban-Card-States (WIP warning) · Tests `NotrelixApiFactory.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check. Evidence: real-stack check 2026-10-31: lane menu Set item limit 1 wrote the view config; dragging a second card in showed the over-limit warning while hovering, the drop was kept and the lane header showed 2 / 1, commit 2eed7b832.

## K. Calendar view

Team capability WM-011.

### UC-WM-K01 Use the Calendar view
- Capability: K Calendar view (WM-011)
- Actors and permission: any viewer.
- Preconditions: Any viewer; a Calendar view with a date field.
- Input: View id, month or week, date field.
- Output: Date-only versus instant is defined by the field; Calendar never infers dates from text.
- Main flow: 1. Open the Calendar tab. 2. Items appear on their dates. 3. A day click opens Add item with the day prefilled.
- No-op / rejection / concurrency / retry: Dragging or resizing writes through the item date use cases [S2], [S4]. An item with no date is listed in the undated aside.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1]); validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 52, 138; WMREQ021; views pack section 7; LD5
- Acceptance: Given the Calendar over field Due, when an item's due changes, then it moves on the calendar for all members.
- Evidence: BE Boards.GetOverview = GET /boards/{boardId:guid}/full -> GetFullBoardQuery (`Boards/Queries/GetFullBoard/GetFullBoard.cs`) [- / -] · FE `board-calendar-view.tsx` with the view bar (filter, personal state, saved filters) in `calendar-toolbar.tsx` · Design Final-Calendar, WM-Cal-Interactions, WM-Toolbar-Calendar · Tests `ComputedFieldsIntegrationTests.cs` · Real e2e `board-views-and-settings.real.e2e.spec.ts` (draws dated items)
- State: HARDENING — real e2e spec, tests, design measurement and owner check; a date field chosen per Calendar view is not built; the quick-add makes two writes (create, then date); tests and owner check. Built: the Calendar filters with the member's own view state (an item with no date still lists in the undated aside; phases and milestones hide under a filter). Not built: a date field chosen per Calendar view. Evidence: real-stack check 2026-10-31: on a board with dated, ranged and undated items: dragged an item a day (200, the reload keeps it, an undo toast); Set date scheduled an undated item; a day click opened Add item with the day prefilled and the new item kept its date after reload; a stale drag answered 412 and the toast now names the actor, the item snapped back; an Observer sees no Add item or Set date and the drag and resize handles are disabled; an archived board hides Add item and refuses the drag; Week view opens, commit 4b8a14c28.

## L. Timeline view

Team capability WM-012.

### UC-WM-L01 Use the Timeline (Gantt) view
- Capability: L Timeline view (WM-012)
- Actors and permission: any viewer; editors resize/move via item write.
- Preconditions: Any viewer; a Timeline view with start and due fields.
- Input: View id, zoom.
- Output: Invalid ranges are rejected with a visible error; dependency lines never auto-reschedule (D76.9).
- Main flow: 1. Open the Timeline tab. 2. Bars show start to due. 3. Dependency lines show links.
- No-op / rejection / concurrency / retry: A range with due before start is refused. Dragging writes through the item date use cases [S2], [S4]. Dependency creation is a link write (UC-WM-B20).
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1]); validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 53, 68.1; WMREQ021; views pack section 8; D76.9, D76.18; LD5
- Acceptance: Given due before start, then the write is rejected (pattern).
- Evidence: BE BoardItems.CreateLink = POST /board-items/{itemId:guid}/links -> CreateBoardItemLinkCommand (`ItemLinks/Commands/CreateBoardItemLink/CreateBoardItemLink.cs`) [- / Idem] · FE `board-timeline-view.tsx` with the view bar in `timeline-toolbar.tsx`; raw plan-items · Design Final-Timeline, WM-TL-Interactions · Tests `CreateBoardItemLinkTests.cs` · Real e2e `board-views-and-settings.real.e2e.spec.ts` (draws dated items)
- State: HARDENING — real e2e spec, tests, design measurement and owner check; grouping and bar span choices are not built; a timeline drag shows no success toast or undo; the archived Timeline was not driven; tests and owner check. Built: the Timeline filters with the member's own view state (phases and milestones hide under a filter), and a due day moved before the start day from the keyboard is refused with a visible error (the pointer drag was already clamped). Not built: grouping and bar span choices (shown as unavailable). Evidence: real-stack check 2026-10-31: dragged a bar two days (200 with the expected version, the reload keeps it); a stale drag answered 412 and the toast now names who changed the item, the bar snapped back; Alt+Shift+Left past the start day was refused with a visible error; an Observer cannot drag or link; Link with... created a dependency and the line is drawn, a cycle is refused (400, and the dialog disables the item); the Timeline had no dependency lines and hid dependency-only items because the real backend has no plan read, now a board read gives the dependencies, commit c1c8abded.

## M. Dashboard projection

Team capability WM-013.

### UC-WM-M01 Provide board work figures to the Space overview
- Capability: M Dashboard projection (WM-013)
- Actors and permission: system; consumer Workspaces.
- Preconditions: The Space overview asks for board figures.
- Input: Board ids already filtered by Governance.
- Output: Never widens visibility.
- Main flow: 1. The Workspaces read asks the Work Management port. 2. It returns unfinished, overdue, done over total, unclaimed, waiting review, member count, last change and top assignees.
- No-op / rejection / concurrency / retry: Read only. Never widens visibility: boards the caller cannot see contribute nothing [S1].
- UI states: none: system behaviour with no screen of its own
- Rules: product sections 111; WM-030; WMREQ128
- Acceptance: Given a Private board the caller cannot read, then it contributes to no figure.
- Evidence: BE IBoardWorkSummaryFacts (port, not an endpoint) · FE `frontend/apps/web/src/routes/workspaces/$workspaceId/spaces/$spaceId.tsx` (space-page e2e 15) · Design Final-Space overview · Tests none found · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; a Private board created by an admin shows 'only 0 people are added' (the creator has no board member row: owner decision pending); tests and owner check. Evidence: real-stack check 2026-10-31: the Space overview as the admin gives progress 1/5 done, 1 overdue, 1 waiting review (an item with a pending approval counts once) and 3 unclaimed on a fresh board, the top assignee and the open work per person; a Private board in the Space shows for the admin and is hidden from a member who is not on it and from the member lens (a non-admin asking for the member lens gets 403, a Guest 403); an archived board leaves every figure; the figures are the same after a reload of the Space page, commit b823844bd.

## N. Form input

Team capability WM-014.

### UC-WM-N01 Create a form on a board
- Capability: N Form input (WM-014)
- Actors and permission: Editor (creating a form is an Editor schema change).
- Preconditions: Board active; actor may create forms.
- Input: Form name, description, questions.
- Output: A Form with questions; it owns no response database of its own (WM-019).
- Main flow: 1. Open Forms, New form. 2. Add questions and map them (UC-WM-N02). 3. Save as a draft.
- No-op / rejection / concurrency / retry: Below Editor = 403 [S2]. Stale version = conflict [S4] (not enforced on questions today). Same content = no-op [S5].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 54-58; WM-019; D70
- Acceptance: Given an Editor creates a form, then it is draft and invisible to submitters.
- Evidence: BE Forms.Create = POST /boards/{boardId:guid}/forms -> CreateFormCommand (`Forms/Commands/CreateForm/CreateForm.cs`) [- / Idem]; Forms.List = GET /boards/{boardId:guid}/forms -> ListFormsQuery (`Forms/Queries/ListForms/ListForms.cs`) [- / -]; Forms.UpdateDetails = PATCH /forms/{formId:guid} -> UpdateFormDetailsCommand (`Forms/Commands/UpdateFormDetails/UpdateFormDetails.cs`) [EV / Idem]; Forms.AddQuestion = POST /forms/{formId:guid}/questions -> AddFormQuestionCommand (`Forms/Commands/AddFormQuestion/AddFormQuestion.cs`) [- / Idem]; Forms.Get = GET /forms/{formId:guid} -> GetFormQuery (`Forms/Queries/GetForm/GetForm.cs`) [- / -] · FE FormBuilderPage, `frontend/packages/product/work-management/web/src/components/forms/form-builder-surface.tsx` · Design WM-Forms-List, WM-Forms-Builder · Tests `BoardManagementLevelTests.cs`, `FormCommandTests.cs`, `FormPipelineIntegrationTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; design for the builder conflict state; tests and owner check. Built: updating, publishing, closing and deleting a form, and adding, changing and removing a question, each name the form version the caller read (a missing one is a 400, a stale one a 409); the web client sends the version of the form it showed (the open form, else the row of the list, which now carries it), refuses a write that has none before sending it, and writes one at a time. The form endpoints sent no version before, so the pipeline refused every one of these writes. Evidence: real-stack check 2026-10-31: created a form (201; a second form of the same title in any tenant answered 409 because the unique slug came from the title alone, fixed); an empty title 400; an Observer 403; renaming from the builder writes 204 and the reload keeps it; the same content bumps nothing; a stale version 412, commit 5cc3d5082.

### UC-WM-N02 Map form questions to fields
- Capability: N Form input (WM-014)
- Actors and permission: Editor.
- Preconditions: A form draft exists.
- Input: Question title, type, target field, required flag.
- Output: mapping validated for compatibility; question type never replaces target field semantics.
- Main flow: 1. Add a question. 2. Pick the target field. 3. The builder validates the pair.
- No-op / rejection / concurrency / retry: File and signature questions are refused until uploads are decided (D70). A removed or incompatible field blocks publish. Stale version = conflict [S4] (not enforced today).
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 57, 55; WM-019; D70
- Acceptance: Given a Text question mapped to a Number field, then rejected.
- Evidence: BE Forms.UpdateQuestion = PATCH /forms/{formId:guid}/questions/{questionId:guid} -> UpdateFormQuestionCommand (`Forms/Commands/UpdateFormQuestion/UpdateFormQuestion.cs`) [- / Idem]; Forms.RemoveQuestion = DELETE /forms/{formId:guid}/questions/{questionId:guid} -> RemoveFormQuestionCommand (`Forms/Commands/RemoveFormQuestion/RemoveFormQuestion.cs`) [- / Idem] · FE builder; some question types 'Sắp có' · Design WM-Forms-Builder · Tests `SubmitFormCommandTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; an item name, description and due date come from the questions the builder marks Goes into the item, not from a question mapped to a system column; tests and owner check. Built: the three question writes carry the form version (UpdateFormQuestion now goes through the Form so the version moves, and a change to what the question already is moves nothing); UpdateFormQuestion has no test (defect B-14). Evidence: real-stack check 2026-10-31: added a Short text question on the Title column and a Date question on Due Date; a duplicate key 400; a stale version 412; a Date question on a text field and a file or signature question are now refused (both were accepted); updating with the same content bumps nothing, commit 5cc3d5082.

### UC-WM-N03 Publish, close, delete or restore a form
- Capability: N Form input (WM-014)
- Actors and permission: Manager (managing a form: questions, publish, close, delete, restore = Board management).
- Preconditions: A form exists; actor is a Manager.
- Input: Form id; publish, close, delete or restore.
- Output: status/publication decides who may submit; FormPublished fact.
- Main flow: 1. Open the form menu. 2. Publish, Close, Delete or Restore. 3. Submit access follows the status.
- No-op / rejection / concurrency / retry: Repeating a state is a no-op [S5]. Below Manager = 403 [S2].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline; destructive confirm (names the impact)
- Rules: product sections 56, 54; D70
- Acceptance: Given an Editor, when they publish, then 403.
- Evidence: BE Forms.Publish = POST /forms/{formId:guid}/publish -> PublishFormCommand (`Forms/Commands/PublishForm/PublishForm.cs`) [EV / Idem]; Forms.Close = POST /forms/{formId:guid}/close -> CloseFormCommand (`Forms/Commands/CloseForm/CloseForm.cs`) [EV / Idem]; Forms.SoftDelete = DELETE /forms/{formId:guid} -> DeleteFormCommand (`Forms/Commands/DeleteForm/DeleteForm.cs`) [EV / Idem]; Forms.Restore = POST /forms/{formId:guid}/restore -> RestoreFormCommand (`Forms/Commands/RestoreForm/RestoreForm.cs`) [- / Idem] · FE forms list panel (no Mở lại) · Design WM-Forms-List (states) · Tests `FormCommandTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; tests and owner check. Built: publish, close and delete name the form version (see UC-WM-N01); publishing a published form and updating a form with what it already holds change nothing. Evidence: real-stack check 2026-10-31: published (204; a stale version 412; an Observer 403; publishing again bumps nothing), closed (again bumps nothing), deleted (NotFound to a member) and restored; a form with a question on a deleted field cannot be published, commit 5cc3d5082.

### UC-WM-N04 Submit a form as an authenticated member
- Capability: N Form input (WM-014)
- Actors and permission: a principal who can create Items on the form's board; others get NotFound.
- Preconditions: Form is published; actor can create items on the board; the link needs sign-in.
- Input: Answers per question.
- Output: Submitting never creates an item; the submission records the submitter and validated answers (D70, D77.1).
- Main flow: 1. Open the in-app form link. 2. Answer. 3. Submit; an Accepted submission lands in the form inbox.
- No-op / rejection / concurrency / retry: Validation matches UC-WM-B01. Retry with the same key has no second effect [S7]. Others get NotFound [S1].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 55, 58; WM-019; D70, D77.1; WMREQ016
- Acceptance: Given a submission, then exactly one Item exists with mapped values and the submission references it. Given a signed-in member submits, then the Item exists immediately with mapped values and the submission references it.
- Evidence: BE Forms.Submit = POST /forms/{formId:guid}/submissions -> SubmitFormCommand (`Forms/Commands/SubmitForm/SubmitForm.cs`) [- / Idem] · FE FormFillPage · Design WM-Forms-Submit, WM-Forms-Submit-States · Tests `FormPipelineIntegrationTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; tests and owner check. Evidence: real-stack check 2026-10-31: a member filled the form: an empty required answer shows the inline error and writes nothing, a valid one answered 201 and shows the sent state; a retry under the same key replays; a bad date and an unknown question key 400; a Guest and an unknown form 404; a closed form refuses, commit 5cc3d5082.

### UC-WM-N05 Process, reject or mark spam on a submission
- Capability: N Form input (WM-014)
- Actors and permission: A person with UpdateItem on the board (D78.4); Viewer and Commenter are refused.
- Preconditions: A submission is Accepted; actor has UpdateItem on the board (D78.4).
- Input: Submission id; process, reject (optional reason up to 500 characters) or mark spam.
- Output: submission lifecycle; processing creates/updates the Item.
- Main flow: 1. Open the form inbox. 2. Choose Process; the ordinary create-item dialog opens prefilled from the answers. 3. Saving links the submission to the item.
- No-op / rejection / concurrency / retry: Reject and spam create no item. Processing an already processed submission is refused. Retry with the same key has no second effect [S7].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 58, 55; WM-019; D70, D77.1, D78.4
- Acceptance: Given an Accepted submission, when Process is chosen, then the create-item dialog opens prefilled from the answers and saving links the submission to the new item. Given Reject or spam, then no item is created and the reason is kept. Given a person with ViewBoard only, then the inbox is refused.
- Evidence: BE Forms.ListSubmissions = GET /forms/{formId:guid}/submissions -> ListFormSubmissionsQuery (`Forms/Queries/ListFormSubmissions/ListFormSubmissions.cs`) [- / -]; Forms.ProcessSubmission = POST /form-submissions/{submissionId:guid}/process -> ProcessFormSubmissionCommand (`Forms/Commands/ProcessFormSubmission/ProcessFormSubmission.cs`) [- / Idem]; Forms.RejectSubmission = POST /form-submissions/{submissionId:guid}/reject -> RejectFormSubmissionCommand (`Forms/Commands/RejectFormSubmission/RejectFormSubmission.cs`) [- / Idem]; Forms.MarkAsSpam = POST /form-submissions/{submissionId:guid}/spam -> MarkFormSubmissionAsSpamCommand (`Forms/Commands/MarkFormSubmissionAsSpam/MarkFormSubmissionAsSpam.cs`) [- / Idem]; Forms.DeleteSubmission = DELETE /form-submissions/{submissionId:guid} -> DeleteFormSubmissionCommand (`Forms/Commands/DeleteFormSubmission/DeleteFormSubmission.cs`) [- / Idem] · FE FormInboxPage · Design WM-Forms-Inbox(-States) · Tests `FormPipelineIntegrationTests.cs`, `FormSubmissionCommandTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; tests and owner check. Built: ListSubmissions needs UpdateItem (D78.4), so a person with ViewBoard only is refused. Built: process, reject, mark spam and delete need UpdateItem as well (D78.4, owner decision 2026-10-30), so an Editor handles responses, a Viewer, Commenter or Observer is refused, and the inbox page opens for editors and managers; the form builder stays the manager's. Evidence: real-stack check 2026-10-31: the inbox lists the response; Create item from response opened the prefilled dialog and processing linked the item (the response moved to Processed and stays after reload); reject and spam 204; a handled response is refused; processing it into a second item was accepted and replaced the first, now refused; a viewer cannot list or handle, commit 5cc3d5082.

### UC-WM-N06 Submit a public (anonymous) form
- Capability: N Form input (WM-014)
- Actors and permission: Public.
- Preconditions: Owner decision on execution principal and abuse controls.
- Input: Anonymous answers.
- Output: Write-scoped capability only; never board read or enumeration (WM-020).
- Main flow: 1. An anonymous visitor opens a public form. 2. Submits.
- No-op / rejection / concurrency / retry: BLOCKED: the execution principal for an unauthenticated submission and the abuse controls (rate limit, captcha, spam) are an owner-only security decision (D67).
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 59-60; WM-020; D67
- Acceptance: Given a public form link, when the visitor requests the board, then refused.
- Evidence: BE none: no anonymous submit endpoint · FE none; no unauthenticated route · Design none; needs public form page, spam/rate-limit/closed · Tests none found · Real e2e none
- State: BLOCKED — Owner decision 67: execution principal and abuse controls

## O. Activity and event production

Team capability WM-015.

### UC-WM-O01 Read item activity / history
- Capability: O Activity and event production (WM-015)
- Actors and permission: Viewer and above (read activity per target authorization).
- Preconditions: Viewer or above on the item.
- Input: Item id, page cursor (size up to 100).
- Output: Typed lines with a stable type, the actor resolved at read time, an English fallback phrase and a language-neutral change; short plain values are copied, sensitive fields are not (D56).
- Main flow: 1. Open the History tab in the sheet or page. 2. Newest facts first. 3. Load more.
- No-op / rejection / concurrency / retry: [S1] for people without ViewBoard. Read only. A page past 100 lines is refused with the cap.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1])
- Rules: product sections 113; D56; WMREQ118-119
- Acceptance: Given a status change, then a history line shows who, what and when, newest first. Given an item the person cannot view, then the read is NotFound.
- Evidence: BE GET /board-items/{id}/activity (Collaboration, adjacent) · FE `frontend/packages/product/work-management/web/src/components/card-detail/task-activity-tab.tsx`, `frontend/packages/product/work-management/web/src/components/card-detail/activity-words.ts` · Design Final-Detail `lich-su`; E, L, Er missing · Tests none found · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; a date change now leaves a history line (new dates-changed fact, from and to shown under Show details; the events manifest and AsyncAPI must be regenerated in the test phase); a field value other than status adds no line; the page cap of 100 was not driven; design E, L and Er; tests and owner check. Evidence: real-stack check 2026-10-31: the card popup lists the history newest first, one line per action by name (created, renamed, status, assigned, checklist, archived, restored, deleted); a same-value write adds no line; a person without access and an unknown id get NotFound; a member reads; the archive line was missing because the archived fact carried the item and board ids swapped, fixed, commit 6b9697e20.

### UC-WM-O02 Provide first-work-item facts for getting-started
- Capability: O Activity and event production (WM-015)
- Actors and permission: System; the consumer is Workspaces (getting-started).
- Preconditions: A workspace asks for first-item facts.
- Input: Workspace id.
- Output: 'first-item' is done when a readable board has an active item.
- Main flow: 1. Getting-started asks the port. 2. It returns the earliest board with an active item and the newest readable board.
- No-op / rejection / concurrency / retry: Read only. Returns nothing when no readable board has an item.
- UI states: none: system behaviour with no screen of its own
- Rules: product sections 101; WM-030
- Acceptance: Given the caller creates the first item, then `first-item` becomes done.
- Evidence: BE IFirstWorkItemFacts not verified · FE not applicable · Design not applicable (no screen) · Tests none found · Real e2e none
- State: INTEGRATING — the adapter exists in Infrastructure (FirstWorkItemFactsAdapter); no test of it was found; add one.

### UC-WM-O03 Expose events and projections to Analytics
- Capability: O Activity and event production (WM-015)
- Actors and permission: Analytics (consumer).
- Preconditions: A Work Management fact is committed.
- Input: Facts and read projections.
- Output: Analytics never edits Work Management truth.
- Main flow: 1. A fact is published through the outbox. 2. Analytics consumes it.
- No-op / rejection / concurrency / retry: A duplicate delivery is harmless; consumers dedupe on the event id. Consumer failure never repeats a Work mutation.
- UI states: none: system behaviour with no screen of its own
- Rules: product sections 111, 113-114, 119; WM-031; WMREQ118-123, WMREQ128
- Acceptance: Given a duplicate delivery of one ItemCreated, then no double count.
- Evidence: BE 38 integration events; 3 consumers implemented, rest Stub · FE not applicable · Design not applicable (no screen) · Tests none found · Real e2e none
- State: INTEGRATING — 38 integration events; three consumers implemented and the rest are logging stubs (defect B-13).

### UC-WM-O04 Publish stable Work Management facts
- Capability: O Activity and event production (WM-015)
- Actors and permission: system.
- Preconditions: A material Work Management change is committed.
- Input: Domain change.
- Output: Stable product facts, not aggregate internals; a no-op publishes nothing.
- Main flow: 1. The change commits. 2. The fact is enrolled in the outbox in the same transaction. 3. It publishes after commit.
- No-op / rejection / concurrency / retry: A semantic no-op publishes nothing [S5]. A broker failure does not roll back committed Work state.
- UI states: none: system behaviour with no screen of its own
- Rules: product sections 113-114; WM-031; WMREQ118-123
- Acceptance: Given a value set to the same value, then no event; given a retry, then one logical event.
- Evidence: BE domain/integration events (38 contracts); no-op suppression tests exist per command · FE not applicable · Design not applicable (no screen) · Tests none found · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; a replay under the same key now answers exactly what the first call answered (200 with the version); tests and owner check. Evidence: real-stack check 2026-10-31: a status write enrolled one logical event; a retry under the same key and a same-value write enrolled none; the archived fact carried the item and board ids swapped, fixed, commit 6b9697e20.

## P. Realtime synchronization

Team capability WM-016.

### UC-WM-P01 See a colleague's change live
- Capability: P Realtime synchronization (WM-016)
- Actors and permission: any viewer of the board.
- Preconditions: A board is open.
- Input: A realtime notify frame.
- Output: Focus, scroll, editor, selection and open sheet are preserved; no reorder under the pointer; bursts coalesce.
- Main flow: 1. Someone changes the board. 2. The client receives a notify-only frame. 3. It refetches the affected queries within 2 s.
- No-op / rejection / concurrency / retry: Frames of another board or a previous workspace are ignored [S6].
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1]); validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 115-116; WM-032; WMREQ124-126; LD4
- Acceptance: (1) Given reduced motion is set, when any remote change, drop or create highlight occurs, then no animation plays. (2) Given A creates the first item on an empty board, when the frame arrives, then B's empty state is replaced by the item. (3) Given a frame of another board or of the previous workspace arrives late, when received, then no view of the current board changes.
- Evidence: BE `IBoardRealtimeNotifier` called by the Work Management write handlers and the comment, attachment and reaction handlers (`BoardRealtimeNotifier`, `BoardRealtimeScopeAdapter`); development publisher `InProcessRealtimePublisher` · FE `work-management-realtime-adapter.ts`, `realtime-lifecycle.tsx`, `drag-gate.ts` · Design BC-Realtime-1440, BC-Realtime-768-320 · Tests none found · Real e2e none · real-stack check 2026-10-31: two browser contexts on one board and one open card; one person changed the title, comment, due date, labels, checklist, link, attachment, cover, vote, lane and assignee and created a card; the other person's popup and board showed each change in 0.1 to 1.2 s without a reload, one frame per change, no duplicate event id; a rejected write and a write that changed nothing (same title, same cover, same vote) sent no frame; after leaving the workspace no board frame arrived; over Redis and in-process, commit efd513a3f
- State: HARDENING — the real e2e spec, tests of the notifier and the adapter, and the owner check.

### UC-WM-P02 Keep editing safe while others edit
- Capability: P Realtime synchronization (WM-016)
- Actors and permission: editor.
- Preconditions: An editor has a draft or a drag in progress.
- Input: A realtime frame.
- Output: An unsaved cell draft stays; a drag completes and the board re-reads after the drop.
- Main flow: 1. A frame arrives. 2. The draft stays. 3. On save a conflict choice appears.
- No-op / rejection / concurrency / retry: A stale draft save returns a conflict choice [S4]; the draft is never discarded silently.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 115; WM-032
- Acceptance: (1) Given B is dragging a card, when A moves another card, then B's drag completes unaffected and the board re-reads after the drop. (2) Given B has a cell editor open on another item, when A changes something, then B's editor, caret, scroll position and selection are unchanged. (3) Given B has an unsaved edit on field F and A changes F, when B saves, then the in-cell conflict choice appears; B's draft is never overwritten before that.
- Evidence: BE not applicable · FE `use-server-draft.ts` and `popup-states.tsx` (two-way choice from BC-Notices), `use-field-cell-writes.ts` (stale cell opens the in-cell choice), `kanban-board.tsx` and `refetch-hold.ts` (drag hold) · Design BC-Notices, BC-Realtime-1440 · Tests none found · Real e2e none · real-stack check 2026-10-31: a person typing in the title kept the draft when another person renamed the item and saw "Someone else just changed the title to …"; leaving the field wrote nothing; "Use the new value" took theirs and "Keep my value" saved the draft; a card dragged while another person renamed another card caused no refetch until the drop, commit efd513a3f
- State: HARDENING — the real e2e spec, tests that a draft and a drag survive a frame, the table cell and the sheet cell choice driven live (typechecked only), and the owner check; the description editor still relies on the server's stale-version notice.

### UC-WM-P03 Lose and regain the connection
- Capability: P Realtime synchronization (WM-016)
- Actors and permission: any viewer.
- Preconditions: A board is open.
- Input: A connection loss.
- Output: A stream gap or reset forces a full refetch.
- Main flow: 1. The client shows 'reconnecting'. 2. Views stay readable. 3. On reconnect it refetches.
- No-op / rejection / concurrency / retry: Duplicate, out-of-order and missed frames are harmless [S6]. A write sent while offline fails with a reason and keeps the input.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1]); validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 115-116; WM-032; WMREQ126; LD4
- Acceptance: (1) Given a stream gap or reset, when detected, then the open board refetches from the server and shows no stale item. (2) Given connection loss, when the stream drops, then a "reconnecting" status shows, writes still follow/ failure rules, and on reconnect the board refetches.
- Evidence: BE gateway reconnect and re-authorization every 60 s · FE `realtime-lifecycle.tsx` refetches the workspace scope on resubscribe; `use-realtime-connection.ts` and `board-connection-notice.tsx` show the status · Design BC-Realtime-1440, BC-Realtime-768-320 · Tests none found · Real e2e none · real-stack check 2026-10-31: a viewer's socket was closed while another person commented and renamed; it reconnected within 1 s and the missed comment showed in 2.6 s without a reload; with the server killed a "Reconnecting…" pill showed at once and after 10 s the "Connection lost since 12:48 PM. Data may be out of date" notice with Reconnect; box check at 1440: banner 64 px high, button 44 px, pill 28 px high, as the artboard; at 320 banner 148 px high, button 44 px full width, as the artboard; commit efd513a3f
- State: HARDENING — the real e2e spec, reconnect and gap tests, the status measured in dark and at 1024 and 390, and the owner check; deviations: nothing shows while connected (the artboard's quiet "connected" line is left out so the measured headers keep their size), widths differ by the font and the shell (button 99 vs 94 px at 1440, banner 472 px wide and two lines at 768 because the rail takes the width), the offline write rule "fails and offers Try again" is the existing write failure.

### UC-WM-P04 Lose access or role while the board is open
- Capability: P Realtime synchronization (WM-016)
- Actors and permission: any viewer.
- Preconditions: A board is open.
- Input: Access or role removed on the server.
- Output: next frame/refetch returns NotFound, generic no-access, no data on screen; lowered role removes editors/handles/checkboxes live; an old notification never restores permission (COL-023).
- Main flow: 1. The next frame or read returns NotFound or a lower level. 2. The UI shows a generic no-access state with no data, or removes editors live.
- No-op / rejection / concurrency / retry: A frame never restores permission. The next read decides [S1].
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1]); validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 115, 103; WM-030; WMREQ105-108
- Acceptance: (1) Given B's level drops from Member to Viewer, when the access read updates, then editors, drag handles and bulk checkboxes disappear without reload. (2) Given B's access to the board is removed, when the next frame or refetch returns NotFound, then B's view shows the generic no-access state and no board data remains on screen. (3) Given the person's level drops to Viewer while the sheet is open, when the next frame or refetch arrives, then editors close, values remain, and a banner says the item is now read-only.
- Evidence: BE realtime subscription authorization (AuthorizeBoardRealtimeSubscriptionQuery), re-checked every 60 s · FE adapter and refetch, `use-full-board.ts` drops the cached board once the read answers NotFound or forbidden · Design BC-Realtime (access lost, permission reduced) · Tests `RealtimeSubscriptionAuthorizationTests.cs` · Real e2e none · real-stack check 2026-10-31: a workspace member without access and a guest were refused the private board's stream (subscription_denied) and received no frame while its owner received every change; their REST read returned the same NotFound as an unknown id; a member whose role was lowered to Observer saw the title editor turn read-only and the read-only banner without a reload; a member removed from the board saw the generic unavailable state with no board name or card, commit efd513a3f
- State: HARDENING — the real e2e spec, a test of the 60 s re-check, and the owner check; the lowered role was checked on the popup title and banner, not on bulk checkboxes or drag handles.

## Q. Automation handoff

Team capability WM-017.

### UC-WM-Q01 Let an Automation rule act on items
- Capability: Q Automation handoff (WM-017)
- Actors and permission: the rule's executor identity, never the trigger actor's authority.
- Preconditions: A rule's executor identity exists.
- Input: A rule action.
- Output: Automation calls normal authorized, idempotent WM use cases (create item, set field, assign, move) and never writes WM tables; WM-040 applies; loops guarded.
- Main flow: 1. A trigger fires. 2. Automation calls a normal authorized, idempotent Work Management use case as the executor.
- No-op / rejection / concurrency / retry: The executor lost permission = the action fails and the execution records it. An archived item is refused [S3].
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1]); validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 109, 118; WMREQ109, WMREQ127, WMREQ156
- Acceptance: Given a rule that sets a field, then it runs under the executor, writes one activity line and does not re-trigger itself.
- Evidence: BE IWorkItemActions via WorkItemActionAdapter · FE workspace-rule editor; board automations flag locked · Design Final-Automation · Tests none found · Real e2e `automation.real.e2e.spec.ts`
- State: HARDENING — real e2e spec, tests, design measurement and owner check; the executor-lost stop was not driven (the only executor is the rule's creator, who cannot be demoted here); tests and owner check. Evidence: real-stack check 2026-10-31: a board rule (moved to a group, then change the status and assign the member) ran under its executor on the real stack: both steps Succeeded, the item took the status and the assignee, the rule's own run list shows them; a second run of the same rule on a state it already set changed nothing (no history line, no frame, no version bump) so the rule does not re-trigger itself; a member cannot create a rule (403) and a Guest cannot list them; a stale move 412, commit 3b011817e.

### UC-WM-Q02 Show automation-caused changes on the board
- Capability: Q Automation handoff (WM-017)
- Actors and permission: any viewer.
- Preconditions: Any viewer.
- Input: An automation-caused change.
- Output: appear via [S6]; Activity records the rule/executor as actor.
- Main flow: 1. The rule changes an item. 2. The board converges by [S6]. 3. History shows the rule as actor.
- No-op / rejection / concurrency / retry: Read only for the viewer. Automation writes use the executor's authority (UC-WM-Q01).
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1])
- Rules: product sections 109; D56; WMREQ127
- Acceptance: Given an automation change, then a viewer sees it live and the history names the rule.
- Evidence: BE ItemActivityRecorder names the rule (IAutomationActivityFacts), WorkItemActions notifies the board · FE card-detail activity-words · Design BC-Realtime · Tests none found · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; the history actor is still the executor's name, followed by the rule; tests and owner check. Evidence: real-stack check 2026-10-31: a viewer with the board open saw the rule's status and assignee changes arrive live (a board frame per action, after the person's own move frame) and the board converged without a reload (the item left the not-done view); the item history reads 'set the status to X by the rule Q2 hand over' and 'assigned member by the rule Q2 hand over', in English and Vietnamese, commit 3b011817e.

### UC-WM-Q03 Sync items with an external provider
- Capability: Q Automation handoff (WM-017)
- Actors and permission: Integrations.
- Preconditions: An integration is connected to the workspace.
- Input: Provider events.
- Output: Replays and duplicates keep idempotency; a provider conflict is a conflict kind.
- Main flow: 1. The adapter translates the provider model. 2. It calls Work Management operations.
- No-op / rejection / concurrency / retry: A duplicate provider event has no second effect [S7]. A provider conflict is surfaced as a conflict kind [S4].
- UI states: none: system behaviour with no screen of its own
- Rules: product sections 110, 120; WMREQ115-117
- Acceptance: Given the same provider update twice, then one Item change.
- Evidence: BE none: no WM-side work · FE none · Design not applicable (no screen) · Tests none found · Real e2e none
- State: DEFINED — Integrations-owned; idempotent replay test when built

## R. Collaboration handoff

Team capability WM-018.

### UC-WM-R01 Comment on an item
- Capability: R Collaboration handoff (WM-018)
- Actors and permission: Commenter and above (`CreateComment`); Viewer disabled "needs permission to comment". Guest needs explicit access.
- Preconditions: Commenter or above on the board; item active.
- Input: Comment text.
- Output: Collaboration owns the comment; a board-item comment has no text anchor.
- Main flow: 1. Open Comments in the sheet or page. 2. Type and post. 3. The comment lists in order.
- No-op / rejection / concurrency / retry: A Viewer sees 'needs permission to comment'. A deleted or inaccessible item creates no Collaboration state.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 108, 159; WMREQ129
- Acceptance: Given a Commenter, when they post, then the comment shows for everyone with access. Given a Viewer, then the composer is disabled with the reason.
- Evidence: BE POST/GET /board-items/{id}/comments (Collaboration, adjacent) · FE comment-thread/*, `frontend/packages/product/work-management/web/src/components/card-detail/task-updates-tab.tsx` · Design WM-Comments; Er, E, edit/delete, M missing · Tests none found · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; tests and owner check. Evidence: real-stack check 2026-10-31: commented on an item (201); a retry under the same key now answers the first comment instead of adding a second (was not idempotent, fixed); an empty and a too-long comment 400; a Guest 404; the card panel posts a comment and it is still there after reload, commit 23cee8fa7.

### UC-WM-R02 Reply, edit or delete a comment
- Capability: R Collaboration handoff (WM-018)
- Actors and permission: author edits/deletes own; moderator separate ("author versus moderator").
- Preconditions: A comment exists; actor is its author.
- Input: Comment id, reply or edit text.
- Output: parent deletion defines child-reply behaviour (COL-006); status is Collaboration-local.
- Main flow: 1. Reply, edit or delete from the comment menu. 2. Replies sit one level deep under the parent (D60).
- No-op / rejection / concurrency / retry: Only the author edits or deletes in the item panel. Reactions use the six-emoji palette (D59).
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline; destructive confirm (names the impact)
- Rules: product sections 108; D59, D60; WMREQ129
- Acceptance: Given a reply to a reply, then it attaches to the top-level comment. Given a non-author, then Edit and Delete are absent.
- Evidence: BE PATCH/DELETE /comments/{id}, reactions (Collaboration, adjacent) · FE comment-thread, useToggleCommentReaction · Design WM-Comments (reply, reactions) · Tests none found · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; tests and owner check. Evidence: real-stack check 2026-10-31: replied, edited as the author, another author's edit 403, the same content bumps nothing, deleted; editing a deleted comment 404, commit 23cee8fa7.

### UC-WM-R03 Mention a person in an item comment
- Capability: R Collaboration handoff (WM-018)
- Actors and permission: Commenter and above.
- Preconditions: Commenter or above.
- Input: '@' and a name.
- Output: Suggestions list only people who can view the board; a mention of someone who cannot is kept as text and notifies no one (D42). A Guest sees only board members.
- Main flow: 1. Type '@'. 2. Pick a person from the list. 3. Post; the person is notified once.
- No-op / rejection / concurrency / retry: Mentioning someone without board access keeps the text and notifies no one. A Viewer cannot post [S2].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 108; D42; Q7
- Acceptance: Given a person without board access, when mentioned, then no notification is sent. Given a Guest, then the list holds board members only.
- Evidence: BE CreateComment mentions (Collaboration) · FE `frontend/packages/product/work-management/web/src/components/item-page/item-page-mention.tsx` · Design Final-Detail (mention) · Tests none found · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; tests and owner check. Evidence: real-stack check 2026-10-31: a mention of a member reaches their inbox and shows as a chip; a Guest with no access gets no notification and no chip, commit 23cee8fa7.

### UC-WM-R04 Attach files to an item
- Capability: R Collaboration handoff (WM-018)
- Actors and permission: upload/link/delete need UpdateItem (Editor+); view needs ViewBoard.
- Preconditions: Actor may update items; item active.
- Input: File or link.
- Output: Collaboration owns the attachment with a Work Management item target; a storage key alone is never permission.
- Main flow: 1. Open Files. 2. Upload or add a link. 3. The file lists on the item.
- No-op / rejection / concurrency / retry: Viewing needs ViewBoard; upload and delete need UpdateItem.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 108; WMREQ129
- Acceptance: Given an Editor, when a file is added, then the item shows the file. Given a Viewer, then Add is disabled.
- Evidence: BE attachments routes (Collaboration, adjacent) · FE ItemPageFiles, useItemFileUploads · Design WM-Sheet-Files · Tests none found · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; file upload (not a link) was not driven; tests and owner check. Evidence: real-stack check 2026-10-31: added a link attachment (201) and listed it; an unsafe url 400; an Observer cannot add or delete (403) but can list; a Guest 404; deleted (204) and deleting again 404, commit 23cee8fa7.

### UC-WM-R05 Link a Document from an item
- Capability: R Collaboration handoff (WM-018)
- Actors and permission: Editor.
- Preconditions: Actor may update items; the page is readable by the actor.
- Input: Document page.
- Output: Documents owns the page; linking exposes neither side to people who lacked access.
- Main flow: 1. Open Links and Docs. 2. Pick a page. 3. It lists on the item.
- No-op / rejection / concurrency / retry: Below Editor = 403 [S2]. A page the actor cannot read cannot be linked. Same link = no-op [S5].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 107, 157; WM-029
- Acceptance: Given a linked page the viewer cannot read, then the item shows an 'unavailable' chip with no title.
- Evidence: BE BoardItems.LinkPage = POST /board-items/{itemId:guid}/link-page -> LinkPageToBoardItemCommand (`BoardItems/Commands/LinkPageToBoardItem/LinkPageToBoardItem.cs`) [- / Idem]; BoardItems.UnlinkPage = DELETE /board-items/{itemId:guid}/link-page/{pageId:guid} -> UnlinkPageFromBoardItemCommand (`BoardItems/Commands/UnlinkPageFromBoardItem/UnlinkPageFromBoardItem.cs`) [- / Idem]; BoardItems.ListLinkedPages = GET /board-items/{itemId:guid}/linked-pages -> ListBoardItemPageLinksQuery (`BoardItems/Queries/ListBoardItemPageLinks/ListBoardItemPageLinks.cs`) [- / -] · FE `frontend/packages/product/work-management/web/src/components/item-actions/linked-docs-section.tsx` (raw paths) · Design WM-Actions-LinkedDocs · Tests `ItemPageLinkTests.cs` · Real e2e none
- State: HARDENING — real e2e spec, tests, design measurement and owner check; a deleted page stays listed until it is purged (page delete answered 400 in the probe); tests and owner check. Evidence: real-stack check 2026-10-31: linked a page (200), the same page again and a retry under the same key add nothing, the list shows it once; an unknown page 404; an Observer 403; a Guest 404; a page in a Private Space is counted in hiddenCount and not named for a member while the admin sees its title; unlinking twice 204, commit 23cee8fa7.

### UC-WM-R06 Watch or unwatch an item
- Capability: R Collaboration handoff (WM-018)
- Actors and permission: any viewer.
- Preconditions: Any viewer.
- Input: Watch toggle.
- Output: Explicit user-resource preference; assignment makes the person a watcher (D78.1).
- Main flow: 1. Choose Watch or Unwatch on the item. 2. The preference saves.
- No-op / rejection / concurrency / retry: Same state = no-op [S5].
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 108; D78.1, D78.2
- Acceptance: Given an assigned person, then they watch the item. Given Unwatch, then they get no watch notifications.
- Evidence: BE PUT/DELETE/GET /board-items/{id}/following (Collaboration, adjacent) · FE useItemFollowing (raw path) · Design none; needs watch toggle on sheet/page (no WM artboard found) · Tests none found · Real e2e `following.real.e2e.spec.ts`, `follow-notifications.real.e2e.spec.ts`
- State: HARDENING — Typed client; design; auto-watch decision open

### UC-WM-R07 Notify and attend (assignment, mention, approval, overdue, watching)
- Capability: R Collaboration handoff (WM-018)
- Actors and permission: Collaboration.
- Preconditions: A Work Management fact has a notification.
- Input: Fact.
- Output: Types with a producer: mention, assigned to me, comment on a watched item, status change on a watched item, due within 24 h, approval requested or decided, nudge, new submission on a managed form (D78.2).
- Main flow: 1. The fact is published. 2. Collaboration delivers per the recipient's preferences.
- No-op / rejection / concurrency / retry: A delivery failure is Collaboration's; Work Management never retries a notification. A recipient without access gets none.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1])
- Rules: product sections 108; D78.2, D35
- Acceptance: Given an item assigned to U with `assigned` in-app on, then one notification; access revoked later never leaks content.
- Evidence: BE mention producer; follow and nudge notifications exist · FE inbox · Design Final-Inbox · Tests none found · Real e2e `follow-notifications.real.e2e.spec.ts`
- State: HARDENING — Other types have no producer; decide set

### UC-WM-R08 Reference items from Documents
- Capability: R Collaboration handoff (WM-018)
- Actors and permission: Documents.
- Preconditions: A Page references a board or item.
- Input: Stable id.
- Output: a Page references a Board or Item by stable id; WM does not own Page content; sharing a page never exposes a private board.
- Main flow: 1. A Page links a board or item. 2. Work Management does not own Page content.
- No-op / rejection / concurrency / retry: A reference to a deleted or unreadable target shows the generic 'unavailable' chip. Sharing a page never exposes a private board.
- UI states: none: system behaviour with no screen of its own
- Rules: product sections 107; WM-029
- Acceptance: Given a page embedding a Private board, when a non-member opens it, then the board shows no content.
- Evidence: BE item link side owned by Documents · FE docs-web · Design not applicable (no screen) · Tests none found · Real e2e none
- State: INTEGRATING — real-stack check 2026-10-31: no Documents block embeds a board or an item yet, so there is nothing to drive; the linked-pages read already hides unreadable pages (R05); Documents owns the embed and its no-leak test, commit 23cee8fa7

## S. Billing and entitlement integration

Team capability WM-019.

### UC-WM-S01 Apply billing entitlements
- Capability: S Billing and entitlement integration (WM-019)
- Actors and permission: Billing decides; WM consumes.
- Preconditions: Billing decides an entitlement.
- Input: Entitlement decision.
- Output: limits on boards/members, premium views, automation/form/advanced fields; Billing never mutates WM state.
- Main flow: 1. A write checks the entitlement port. 2. The port allows (no plan gates now, D76.2).
- No-op / rejection / concurrency / retry: The port allows today. When Billing gates a feature, a refused write names the limit and changes nothing.
- UI states: none: system behaviour with no screen of its own
- Rules: product sections 112; D76.2
- Acceptance: Given a plan without a premium view type, when creating it, then rejected with an entitlement reason.
- Evidence: BE entitlement port allows (D76.2); unverified · FE not applicable · Design not applicable (no screen) · Tests none found · Real e2e none
- State: DEFINED — no Work Management command implements IRequireFeature (verified); entitlement checks stay behind the port and allow.

## T. Performance and hardening

Team capability WM-020.

### UC-WM-T01 Use the view on a large (paged) board
- Capability: T Performance and hardening (WM-020)
- Actors and permission: any viewer.
- Preconditions: A board with thousands of items.
- Input: Group cursor, page size 50 (max 200), filter, sort.
- Output: Server-side filter and sort with filtered totals; windowed rows.
- Main flow: 1. Open the board. 2. The server returns schema, group totals and one page per group. 3. Scrolling loads more.
- No-op / rejection / concurrency / retry: A page request over 200 is clamped. A filter that matches nothing returns zero totals, not an error.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1]); validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 36, 127; WMREQ147-148; views pack section 14
- Acceptance: Given a board of 5,000 items, when opened, then the first paint is under 2 s and at most 200 items per group return.
- Evidence: BE Boards.GetOverview = GET /boards/{boardId:guid}/full -> GetFullBoardQuery (`Boards/Queries/GetFullBoard/GetFullBoard.cs`) [- / -]; BoardItems.List = GET /boards/{boardId:guid}/items -> GetBoardItemsQuery (`BoardItems/Queries/GetBoardItems/GetBoardItemsQuery.cs`) [- / -] · FE none; no windowing/paging; client filter/sort · Design BC-Large · Tests `ComputedFieldsIntegrationTests.cs`, `NotrelixApiFactory.cs` · Real e2e none
- State: DEFINED — measured on a seeded board of 5,000 items (real stack 2026-10-31), before: the board read took 2.2 to 4.6 s and 3.4 MB, 1.6 s of it one per-field count that joined every value of every board (fixed, commit ea5287110); the Table drew all 5,000 rows, 130,000 nodes, 662 MB heap and 10.1 s to the first row (a group now draws fifty rows with the artboard's shown-of-total foot and show-more button, commit 9e539b23f: 1,739 nodes, first row 2.6 s); approval requests had only a primary key (index added, commit b7e9a1f0d). Still open: the board read is O(items) (about 0.2 ms per row, mostly the row-level security check `ops.has_workspace_access`, a plpgsql function that runs dynamic SQL per row: a Platform finding for the owner, not changed here), so the paged read with server-side filter, sort and filtered totals (Q9, BC-Large) is not built; Kanban already caps a column at 20 cards; `GET /boards/{id}/items` is unpaged (1.2 s for 5,000) and no client calls it; comments, form submissions and board approvals lists are unpaged.

### UC-WM-T02 Use the view at 1440 / 1280 / 1024 / 768 / 320 px
- Capability: T Performance and hardening (WM-020)
- Actors and permission: any viewer.
- Preconditions: Any viewer.
- Input: Viewport width.
- Output: One-row toolbar levels by container width (D74); 44 px touch targets.
- Main flow: 1. Open the board at 1440, 1280, 1024, 768 or 320 px. 2. The toolbar picks a level. 3. At 320 filters open in a bottom sheet.
- No-op / rejection / concurrency / retry: Presentation only; no write. Touch targets stay at least 44 px.
- UI states: success at each width, responsive (one-row toolbar levels, bottom sheet at 320), loading (skeleton)
- Rules: product sections 50; D74
- Acceptance: Given 1440 px, then the toolbar fits one row at level L1. Given 320 px, then filters open in a bottom sheet.
- Evidence: BE not applicable · FE one-row toolbar levels (D74); 320 sheets partly · Design BC-Shell, BC-Toolbar, WM-Toolbar-*, BC-Table (320/768) · Tests none found · Real e2e none
- State: INTEGRATING — 1-px box-tree diff at 1440, 1024 and 390 px in light and dark; 44 px targets.

### UC-WM-T03 Published limits and limit-reached states
- Capability: T Performance and hardening (WM-020)
- Actors and permission: System; any viewer.
- Preconditions: Any write that would exceed a limit.
- Input: A write at the limit.
- Output: Every limit is documented; a write over a limit fails with the limit named (current/max, what to do), never truncates silently.
- Main flow: 1. The write reaches 25,000 active items, 200 fields, 50 groups, 50 views or 200 bulk items. 2. The server refuses with a typed error naming the limit. 3. The UI shows the limit-reached state.
- No-op / rejection / concurrency / retry: Never truncates silently; no partial write.
- UI states: validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 127; D78.8; WMREQ146-147
- Acceptance: Given a limit of N fields, when the N+1th is created, then rejected naming the limit.
- Evidence: BE none: no published limits · FE none · Design none; needs limit-reached states (items, fields, options, groups, sub-items) · Tests none found · Real e2e none
- State: DEFINED — no limit is published or enforced; the limits are decided in D78.8.

### UC-WM-T04 Work with boards on the native mobile app
- Capability: T Performance and hardening (WM-020)
- Actors and permission: Any member.
- Preconditions: Any member on the native app.
- Input: App navigation.
- Output: Offline is a read cache only (D47). Same server rules as web.
- Main flow: 1. Open the board list. 2. Open a board in list view. 3. Open an item, comment, receive notifications.
- No-op / rejection / concurrency / retry: Same server rules as web; [S1]-[S7] apply.
- UI states: loading (skeleton), empty, error with Retry, denied (identical NotFound [S1]); validation (inline), denied (control disabled with reason [S2]), read-only (archived [S3]), conflict (names the actor [S4]), pending, offline
- Rules: product sections 103; D47, D78.14
- Acceptance: Given a member on mobile, when they open a board, then they see the same items as web. Given offline, then cached boards are readable and writes are blocked with a reason.
- Evidence: BE not applicable (same API) · FE none; mobile package is placeholder screens · Design Final-M-Board, Final-M-Calendar, Final-M-Detail, Final-M-MyWork · Tests none found · Real e2e none
- State: DEFINED — the mobile package is placeholder screens; web slices come first (D78.14).

### UC-WM-T05 Rebuild derived and materialized projections
- Capability: T Performance and hardening (WM-020)
- Actors and permission: system/operator.
- Preconditions: A board near the item limit.
- Input: None.
- Output: Until then derived values are evaluated per read (D78.8). A projection is derived, tenant-scoped and rebuildable, never a second mutation path.
- Main flow: 1. A projection is built only when a board at the limit misses the performance budget.
- No-op / rejection / concurrency / retry: A rebuild is idempotent.
- UI states: none: system behaviour with no screen of its own
- Rules: product sections 37, 73; WM-013; D78.8; WMREQ147
- Acceptance: Given a rebuild, then values equal source-derived values and no Item version changes.
- Evidence: BE none: derived per request; no materialized projection (WM-013) · FE not applicable · Design not applicable (no screen) · Tests none found · Real e2e none
- State: DEFINED — no projection exists; per-read evaluation is accepted until a board at the limit misses the performance budget (D78.8).

# 4. Product decisions applied

Decisions that this catalog relies on and that the product doc does not yet state. Part 5 lists the text the product doc must absorb.

Product decision (owner, 2026-10-26): D28. Create carries the item's own facts (name, description, start, due) atomically; status, priority, people, labels and custom values follow as separate writes and a partial failure is named in the toast. There is no cross-aggregate atomic create.

Product decision (owner, 2026-10-26): D29. Dates are cleared with explicit `clearDueDate` and `clearStartDate`; a PATCH null keeps meaning "unchanged".

Product decision (owner, 2026-10-26): D30. Board header "Add item" opens the full form; per-group "Add to" rows keep the quick inline add. Calendar and Timeline "Add item" open the form with the day prefilled.

Product decision (owner, 2026-10-26): D32. Bulk item edits use one endpoint per board, all-or-nothing, 1 to 200 items, with operations set status, set due, clear due, assign (sole assignee), unassign-all and archive. Its "no expected version" part is overridden by D77.3.

Product decision (owner, 2026-10-26): D33. Command search returns items, docs and boards the caller may open, capped at 6, 5 and 5, own items first, case and mark insensitive.

Product decision (owner, 2026-10-26): D35. My Work "waiting for others" lists open items the member created or follows where someone else is the assignee. "Nhắc" sends each assignee an Inbox notification naming the sender, at most once per item per sender per 24 h, needs view access to the item, never to self.

Product decision (owner, 2026-10-27): D36. Kanban is built from the canvas artboards Kanban-Board, Kanban-CreateTask and Kanban-Card-States.

Product decision (owner, 2026-10-27): D37. A WIP limit is an optional per-lane number stored on the Kanban view configuration; exceeding it warns and still allows the drop, and never blocks writes on the server.

Product decision (owner, 2026-10-27): D38. Lane collapse is a personal preference per member per view, kept client-side keyed by workspace and view.

Product decision (owner, 2026-10-27): D39. A card shows the comment count, at most 2 labels and 2 avatars then "+N"; the key is the board key prefix plus number, else "#number". Quick add appends to the bottom of the lane.

Product decision (owner, 2026-10-27): D40. The create popup opened from a lane prefills that lane's status; "create another" keeps status, priority, people and labels and clears title and description.

Product decision (owner, 2026-10-27): D41. Assigning an item requires the assignee to be able to view its board; otherwise it is refused with "share the board first". There is no auto-share. Board lists use the same visibility as the single read.

Product decision (owner, 2026-10-27): D42. In item comments the @ list shows only people who can view the board; a mention of someone who cannot is kept as text and sends no notification.

Product decision (owner, 2026-10-27): D43. Bulk set-priority and delete join the bulk endpoint. Move-to-board in bulk waits for a decision on item key, field mapping and permissions.

Product decision (owner, 2026-10-27): D47. Mobile uses a bearer and rotating refresh token, offline is a read cache only, bottom tabs, and Reports and Automation are read-only.

Product decision (owner, 2026-10-27): D49. A new board is created with default groups mirroring its default statuses; an idempotent repair gives group-less boards those groups. The UI never returns silently from an add: it shows the reason and offers the fix.

Product decision (owner, 2026-10-27): D50. Move item to another board needs UpdateItem on the source and CreateItem on the target. Status maps by name, else the target's first status; custom values carry to a field of the same name and type and others are dropped and named in a notice; comments, attachments, sub-items and history move with the item; the item gets the target board's next key.

Product decision (owner, 2026-10-27): D51. Item delete is a soft delete with restore from the board trash for 30 days; only people with UpdateItem may delete or restore; delete asks for confirmation.

Product decision (owner, 2026-10-27): D54. Move to another board: the target group is the group with the source group title, else the board default, else its first; an archived item or one with dependencies is refused with the reason; assignees who cannot view the target are dropped and counted. The board trash is read by people with UpdateItem.

Product decision (owner, 2026-10-27): D55. Deleting a status or a value of another Select field asks where its items go: a status always needs another status; another Select may leave the items without a value. The rewrite covers active, archived and trashed items.

Product decision (owner, 2026-10-27): D56. Work Management publishes completed item facts through the outbox; Collaboration owns the history rows. GET /board-items/{id}/activity needs ViewBoard, newest first, page size capped at 100, with a stable type, the actor resolved at read time and a language-neutral change.

Product decision (owner, 2026-10-28): D58. A new board's default Status options and groups are named in the creator's stored profile language ("vi" when unset); Application passes it to Domain as a creation fact.

Product decision (owner, 2026-10-28): D59. Comment reactions use a fixed palette of six emoji; one reaction per emoji per person per comment; reacting needs the same permission as commenting.

Product decision (owner, 2026-10-28): D60. Comment replies are one level deep; replying to a reply attaches to its top-level comment.

Product decision (owner, 2026-10-28): D61. Labels are managed in Board settings, Labels (name and colour from the palette; create, rename, recolour, delete). Delete confirms and says how many items lose the label. The item picker offers "Create label" when the typed text matches nothing.

Product decision (owner, 2026-10-28): D62. Approval decision: the named approver sees Approve and Reject in the item's Approval section; Reject asks for a reason; the requester can Cancel; everyone else sees the state.

Product decision (owner, 2026-10-28): D63. Table inline title edit: a single click opens the item; double-click, F2 or Enter edits in place (Enter saves, Escape cancels, blur saves); needs UpdateItem.

Product decision (owner, 2026-10-28): D64. A failed board-settings write never reverts silently: the field keeps what was typed, an inline error says what failed with Retry; a version conflict reloads the latest and says so.

Product decision (owner, 2026-10-28): D65. Board settings show each status or select option's live item count from the backend (active items only).

Product decision (owner, 2026-10-28): D66. Board templates are workspace-scoped creation input. "Save board as template" needs ManageBoard and copies fields, statuses, groups, labels and views, but no items, comments or members unless "include items" is ticked. "New board" opens a gallery of published templates plus built-in starters. Management needs ManageBoard on the template; listing needs ViewWorkspace.

Product decision (owner, 2026-10-28): D67. Public (anonymous) form submission waits for an owner decision on the execution principal and abuse controls.

Product decision (owner, 2026-10-28): D68. A saved filter is a named filter, sort and group of one board; personal by default, shareable with the board; applying it never changes the shared default view; delete is soft with restore.

Product decision (owner, 2026-10-28): D69. The work item card specification has three surfaces (card face, side sheet, item page) with the section order Properties, Description, Checklists, Sub-items, Files, Links and Docs, Approval, Comments and History.

Product decision (owner, 2026-10-29): D70. Member forms: submitting never creates an item; it records an Accepted submission in the form inbox. Creating an item from a submission opens the ordinary create-item dialog prefilled from the answers, then links the submission. Submit needs CreateItem. Reject takes an optional reason up to 500 characters. File and signature questions are refused until uploads are decided.

Product decision (owner, 2026-10-29): D71. Personal view state (filter, sort up to 3 levels, group with date bucket, Kanban swimlane) is stored per user, board and view in BoardPreference as one versioned criteria JSON; it overrides the shared view only for that person; "Về view chung" clears it; Viewers keep personal state; a stale field condition is ignored with a hint.

Product decision (owner, 2026-10-29): D72. Keyboard hint chips stay removed; code shipped before an artboard update wins only when a later approved artboard exists.

Product decision (owner, 2026-10-29): D73. Table custom-field columns are accepted as built: no "Mở" button on rows (Enter opens the item), conflict copy has no author name until the API carries one, date cells are calendar-only, column reorder is Alt+Arrow.

Product decision (owner, 2026-10-29): D74. The WM toolbar is one row; its level follows the bar's content width (1440 gives L1, 1280 L2, L3 drawn at 1024).

Product decision (owner, 2026-10-30): D76.1. Board archive pauses the board's automation rules and integration sync; unarchive resumes them. Board delete is soft: restorable by a Manager for 30 days, then purged.

Product decision (owner, 2026-10-30): D76.10. Formula keeps its current language. Rollups work over sub-items only. Progress is the share of done sub-items when sub-items exist, else the share of checked checklist items, else 0 or 100 from the status category.

Product decision (owner, 2026-10-30): D76.11. Approvals: one approver per request; a team approver means any member decides and the first decision wins; no expiry; re-request creates a new request linked to the previous one.

Product decision (owner, 2026-10-30): D76.12. Workload (capacity, allocation) belongs to a separate roadmap item and is out of board core.

Product decision (owner, 2026-10-30): D76.13. A template whose board has more than 500 items is refused with the reason; never truncated silently.

Product decision (owner, 2026-10-30): D76.15. Import: CSV and XLSX into a new or existing board, column-to-field mapping with preview, all-or-nothing per file, at most 5,000 rows. Export: CSV and XLSX of the current view with its filters, for anyone who can view the board except Guests.

Product decision (owner, 2026-10-30): D76.17. My Work lists not-done items assigned to me on boards I can view, ordered by due date. A dashboard is an Analytics surface, not a Work Management view.

Product decision (owner, 2026-10-30): D76.18. Calendar and Timeline detail (drag to reschedule, dependency creation) belongs to a later roadmap item.

Product decision (owner, 2026-10-30): D76.2. No billing plan gates in Work Management now; capability checks stay behind the entitlement port and allow.

Product decision (owner, 2026-10-30): D76.3. A key prefix change applies to new items only. An item moved to another board gets the target board's next key; its old key keeps resolving to the item.

Product decision (owner, 2026-10-30): D76.4. No expiry on board access in this scope. Public board share links are not built; Guests reach boards only through explicit membership.

Product decision (owner, 2026-10-30): D76.5. A field's type cannot change after creation. Field delete is soft with a 30-day restore. DateTime and uploaded-file field types are deferred.

Product decision (owner, 2026-10-30): D76.6. A group that still holds items cannot be deleted until the person picks the group its items move to.

Product decision (owner, 2026-10-30): D76.7. Duplicate copies title, description, field values, labels and checklist, plus sub-items (option, on by default); never comments, activity, approvals, links or attachment files. (The "Remind" part is withdrawn by D77.2.)

Product decision (owner, 2026-10-30): D76.8. Item links have two kinds: blocks and blocked-by (one directed pair, acyclic, enforced on the server) and relates-to. A link to an archived item stays and shows it as archived; a link to a trashed item is hidden while in trash and removed on purge.

Product decision (owner, 2026-10-30): D76.9. Every board has exactly one default view; deleting it asks for the next default. Timeline shows dependency lines without auto-rescheduling. (The "private views are not built" part is withdrawn by D77.4.)

Product decision (owner, 2026-10-30): D77.1. D76.14 is withdrawn: a member form submission never creates an item; it lands in the form inbox and "Process" opens the create-item dialog prefilled.

Product decision (owner, 2026-10-30): D77.2. D76.7 "Remind" is withdrawn: "Nhắc" notifies each assignee in their Inbox, at most once per item per sender per 24 h. No personal reminder is built.

Product decision (owner, 2026-10-30): D77.3. Every per-item write (status, due date, complete or reopen, assign or unassign, move to board, priority, label, delete) carries the expected version. Bulk edits send each item's version; one stale item rejects the whole batch.

Product decision (owner, 2026-10-30): D77.4. Private views already ship and stay: visible only to their creator, never listed or broadcast to others. BoardPreference remains the personal state over a shared view.

Product decision (owner, 2026-10-30): D78.10. A daily job permanently removes items, fields and boards that sat in trash for over 30 days.

Product decision (owner, 2026-10-30): D78.1. Being assigned to an item makes the person a watcher of it.

Product decision (owner, 2026-10-30): D78.11. The assignee access warning fires whenever access narrows: make private, visibility change, move to another Space.

Product decision (owner, 2026-10-30): D78.12. Guest invitation to a single board is a Workspaces resource invitation; its UI is the board's Share dialog.

Product decision (owner, 2026-10-30): D78.14. Native mobile boards are out of the web slices; they follow after Work Management web freezes.

Product decision (owner, 2026-10-30): D78.2. Item notifications with a producer in this scope: mention, assigned to me, comment on a watched item, status change on a watched item, due within 24 h, approval requested of me or decided on my request, nudge, new submission on a form I manage.

Product decision (owner, 2026-10-30): D78.3. Board name is 1 to 120 characters after trimming.

Product decision (owner, 2026-10-30): D78.4. Form submissions (list, process, reject, spam, delete) need edit rights on the board (UpdateItem), not ViewBoard, because they can hold respondent data. Submit still needs CreateItem.

Product decision (owner, 2026-10-30): D78.5. Duplicate board copies settings, fields, groups, views, labels and forms; items are optional and follow the duplicate rules for items; the 500-item cap applies; members are not copied; the duplicator becomes Manager.

Product decision (owner, 2026-10-30): D78.6. A Relation field links items of other boards in the same Workspace; a person sees only linked items they can view (others show as "Không có quyền xem"); the five Relations endpoints fold into it.

Product decision (owner, 2026-10-30): D78.7. Required fields are enforced on create (dialog, form, import, API); existing items are not made invalid retroactively.

Product decision (owner, 2026-10-30): D78.8. Published limits: 25,000 active items per board, 200 fields, 50 groups, 50 views, 200 items per bulk edit; each has a limit-reached state naming the limit. Per-read formula evaluation is accepted until a board at the limit misses the performance budget; then the projection is built.

Product decision (owner, 2026-10-30): D78.9. The last Manager of a board cannot be removed or demoted.

Product decision (owner, 2026-10-29): Q2. Custom-field values at create: own facts atomic at create, custom values in one batched follow-up write; partial failure named with "Thử lại phần còn thiếu".

Product decision (owner, 2026-10-29): Q3. Sub-item creation in the dialog: yes, parent fixed, parent's group, one level.

Product decision (owner, 2026-10-29): Q5. Column hide, order and width are personal for everyone (BoardPreference); Editors get "Lưu vào view chung".

Product decision (owner, 2026-10-29): Q6. An archived-items panel with restore is in Board core (additive backend read).

Product decision (owner, 2026-10-29): Q7. A Guest sees only people on that board in assignee, Person and mention pickers; never the workspace directory or emails.

Product decision (owner, 2026-10-29): Q8. Bulk: Kanban multi-select with the same bar; "select all in group" capped at 200; bulk label and bulk custom-field set are deferred.

Product decision (owner, 2026-10-29): Q9. Counts and paged reads: server-authoritative filtered totals; filter and sort server-side on paged boards; the unbounded read is kept one release.

Product decision (owner, 2026-10-29): Q10. Delete with sub-items: sub-items go to Trash with the parent and restore together; the confirm names the count.

Product decision (owner, 2026-10-29): Q12. Conflict and "deleted by" copy names the actor; it needs a contract addition (actor on conflict responses and deleted frames); Guests and unknown actors see "Một người khác".

Product decision (owner, 2026-10-29): Q13. Multi-assignee items under group-by-assignee show in every assignee lane; lane counts count them, board total and selection count once; a drop moves the source assignee to the target.

Lead decision (2026-10-30): LD1. Every first-party write sends an Idempotency-Key, including expected-version PATCH writes such as PATCH /board-items/{itemId} and view configuration writes; a retry with the same key and content has no second effect (WMREQ115 to WMREQ117, WMREQ143).

Lead decision (2026-10-30): LD2. Archived items are kept until a person unarchives or deletes them; no automatic purge applies to archived items. Only the 30-day trash window of D51 and D78.10 purges.

Lead decision (2026-10-30): LD3. An approver chosen for a request must be able to view the board of the item; otherwise the request is refused with "share the board first", the same rule as assignment (WM-040).

Lead decision (2026-10-30): LD4. Realtime frames carry the resource version (the same number a read returns for the next mutation), not a wall-clock counter, so a client can order frames and detect a gap (WMREQ125).

Lead decision (2026-10-30): LD5. Calendar and Timeline read dated items through the ordinary board item read with their date fields; the raw `plan-items` path of the frontend is removed because the backend has no such route.

## 4.9 Card popup capabilities (built 2026-10-30)

These are new capabilities of the item card popup; each is built backend to frontend and checked only by compiling, so the gap is the same for all of them: tests and owner check. Product rules: product section 178A.

- Item cover (colour or image attachment, strip or whole card, card face shows it): Evidence: BE BoardItems.SetCover = PUT /board-items/{itemId}/cover -> SetBoardItemCoverCommand (`BoardItems/Commands/SetBoardItemCover/SetBoardItemCover.cs`) [EV / Idem] · FE `components/card-popup/capabilities/cover-picker.tsx`, `card-cover.tsx`, `views/kanban/kanban-card.tsx`. State: INTEGRATING — tests and owner check.
- Repeat and complete-and-reopen: Evidence: BE BoardItems.SetRepeat -> SetBoardItemRepeatCommand; `BoardItem.CompleteOccurrence` used by CompleteBoardItemCommand and by a move to a done status (`BoardItemFieldWrites.Write`, `BoardItem.RepeatsOnCompletion`) [EV / Idem]; the mock rolls the dates in `rollRepeatOnCompletion` · FE `repeat-reminder-select.tsx`. State: INTEGRATING — tests and owner check.
- Reminder before due and followers on the due-date job: Evidence: BE BoardItems.SetReminder -> SetBoardItemReminderCommand; `DueDateNotificationSweeper` in `Infrastructure/BackgroundJobs/DueDateNotificationSweep.cs` · FE `repeat-reminder-select.tsx`. State: INTEGRATING — tests and owner check.
- Vote field type: Evidence: BE `FieldType.Vote`, BoardItems.SetVote / ListVotes -> SetBoardItemVoteCommand [- / Idem], `BoardItemFieldProjection` · FE `vote-viewers-chips.tsx`, `field-type-registry.ts`. State: INTEGRATING — tests and owner check.
- Viewers ("Đã xem"): Evidence: BE BoardItems.RecordView / ListViewers -> RecordBoardItemViewCommand through `IBoardItemViews` (one `INSERT ... ON CONFLICT` upsert, so two first views cannot collide), `board_item_views` with RLS · FE `ViewersChip`. State: INTEGRATING — tests and owner check.
- Colour-blind mode: Evidence: BE Identity.Profile.UpdatePreferences `colourBlindLabels` · FE `colour-blind-label-mark.tsx`, `apps/web/src/providers/app-providers.tsx`. State: INTEGRATING — tests and owner check.
- Copy a checklist: Evidence: BE Checklists.Copy -> CopyChecklistCommand, Checklists.ListForBoard · FE `ChecklistCopyFrom`. State: INTEGRATING — tests and owner check.
- Remove all links: Evidence: BE BoardItems.ClearLinks -> ClearBoardItemLinksCommand [- / Idem] · FE `ClearAllLinks`. State: INTEGRATING — tests and owner check.
- Face fields of the Kanban card: Evidence: FE `face-fields-popover.tsx`, `face-fields.ts`, and the board settings `updateView` (`board-settings.api.ts`) which writes the same list; view config `cardFieldIds` and `cardHideEmpty` (the unused backend `KanbanViewConfig.VisibleFieldIds` refuses an empty list, which would break showing no fields, so the saved view config carries it). State: INTEGRATING — tests and owner check.
- Card popup layout, assignee stack and priority: Evidence: FE `card-popup-surface.tsx` (popup stops 64 px from the top and bottom, header pinned, columns scroll), `popup-ui.tsx` `EmptySection` (empty description, attachments and links fold to one line) and `AvatarStack` (24 px marks, 6 px overlap, at most 3 then +N, also on the Kanban card face), `people-panel.tsx`, the priority picker in `field-editors.tsx`. State: INTEGRATING — tests and owner check.
- Custom field rows and the add menu: Evidence: FE `fields-section.tsx` (only fields with a value as rows, face-pinned fields as chips, `add-field-panel.tsx`), `add-menu.tsx` (search, Nội dung, Thuộc tính của board, Hiển thị), `attach-panel.tsx` (one panel, 25 MB, drop and paste on the card). State: INTEGRATING — tests and owner check.
- Start date with a time: Evidence: FE `dates-panel.tsx` writes the start as an instant; BE `BoardItem.StartedAt` is already a `DateTimeOffset` and `UpdateBoardItem`, `SetBoardItemDueDate` and `CreateBoardItem` take a `DateTime` start, so no schema or contract change. State: INTEGRATING — tests and owner check.
- Checklist item assignee and due ("Giao mục này", "Đặt hạn cho mục"): Evidence: BE Checklists.SetItemAssignee = PUT /checklist-items/{itemId}/assignee -> SetChecklistItemAssigneeCommand (assign right, the person must see the board) and Checklists.SetItemDue = PUT /checklist-items/{itemId}/due -> SetChecklistItemDueCommand [EV / Idem], `Checklist.SetItemAssignee` and `SetItemDue` (current value is a no-op), the `assignee_user_id` and `due_at` columns of `checklist_items` already had the checklist RLS · FE `checklist-item-controls.tsx`, `use-checklist-writes.ts`, mock `checklistItems.setAssignee` and `setDue`. State: INTEGRATING — tests and owner check.
- Link attachments in the mock: Evidence: mock `cards.attachments.createLink` (POST /board-items/{id}/attachments, Collaboration.Attachments.Create). State: INTEGRATING — tests and owner check.

# 5. Canonical updates required

The product doc [work-management](../../product/work-management.md) should absorb the following. Each item names the section that owns it.

- Section 10 (Board lifecycle) and 121 (Deletion of a Board): archive pauses the board's automation rules and integration sync and unarchive resumes them (D76.1). Delete is soft; a Manager may restore it for 30 days; a daily job then purges it (D76.1, D78.10). Board name is 1 to 120 characters after trim (D78.3). A restore after the window is refused with the reason.
- Sections 96 to 98 (Templates): publishing, archiving and deleting a template need ManageBoard on the template, not Workspace Owner or Admin (D66); listing needs ViewWorkspace.
- Section 12, 13 and 96 to 98 (Templates): "Save as template" copies fields, statuses, groups, labels and views; items only with "include items", at most 500 top-level items, otherwise refused (D66, D76.13). Add a rule for duplicating a board: it copies settings, fields, groups, views, labels and forms, items optionally, never members; the duplicator becomes Manager (D78.5).
- Section 27.1 (Item key): a prefix change applies to new items only; a moved item gets the target board's next key and its old key keeps resolving through a stored alias (D76.3).
- Sections 15 to 25 and 124 (Fields): a field's type cannot change after creation; field delete is soft with a 30-day restore; DateTime and uploaded-file types are deferred (D76.5). Add the Relation field type (D78.6) and the required flag (D78.7). Add the published limits table: 25,000 active items per board, 200 fields, 50 groups, 50 views, 200 items per bulk edit, each with a limit-reached state (D78.8).
- Sections 38 to 41 (Groups): a group that holds items cannot be deleted until the person picks the group its items move to (D76.6).
- Sections 26 to 34 (Items): duplicate copies title, description, field values, labels, checklist and optionally sub-items, never comments, activity, approvals, links or attachment files (D76.7). Move to another board follows D50 and D54. Every per-item write carries the expected version; bulk sends each item's version and one stale item rejects the batch (D77.3). Assignment makes the person a watcher (D78.1) and requires view access (D41). Archived items are kept until unarchived or deleted (LD2). "Nhắc" is the assignee nudge, at most once per item per sender per 24 h (D35, D77.2).
- Section 68 (Dependency): two link kinds, blocks and relates-to; waits-on is acyclic and enforced on the server; archived and trashed targets behave as in D76.8.
- Sections 46 to 49, 61, 153 and 154 (Views): exactly one default view per board; deleting it asks for the next (D76.9). Private views stay, visible only to their creator (D77.4). Personal filter, sort, group and swimlane state is BoardPreference (D71). WIP limits are per-lane numbers in the Kanban view configuration and only warn (D37). Timeline shows dependency lines without auto-rescheduling (D76.9).
- Section 77 (Progress): progress is the share of done sub-items, else the share of checked checklist items, else 0 or 100 from the status category (D76.10). Rollups work over sub-items only.
- Sections 84 to 89 (Approvals): one approver per request; a team approver means any member decides and the first decision wins; approvals never expire; a re-request creates a new request linked to the previous one (D76.11). The approver must be able to view the board (LD3).
- Sections 54 to 60 (Forms): a member submission never creates an item; it lands in the form inbox and "Process" opens the prefilled create-item dialog (D70, D77.1). Form submission list, process, reject, spam and delete need UpdateItem (D78.4). Anonymous public forms stay BLOCKED until the owner decides the execution principal and abuse controls (D67).
- Sections 90 to 95 (Workload): out of board core; the sections stay as definitions for the later roadmap item (D76.12).
- Sections 133 to 135 (Import, export): CSV and XLSX, at most 5,000 rows, all-or-nothing per file with a mapping preview; export of the current view for anyone who can view the board except Guests (D76.15).
- Sections 103 to 106 (Governance, guests, sharing): no expiry on board access and no public share link in this scope (D76.4); the last Manager cannot be removed or demoted (D78.9); the assignee access warning fires on any narrowing of access (D78.11); a Guest invitation to one board is a Workspaces resource invitation from the Share dialog (D78.12).
- Sections 111 and 136 (Analytics, search): a dashboard is an Analytics surface, not a Work Management view (D76.17). My Work lists not-done items assigned to me on boards I can view, by due date (D76.17).
- Sections 113 to 117 (Events, realtime): realtime frames carry the resource version (LD4). The item notifications with a producer are listed in D78.2. Every first-party write sends an Idempotency-Key (LD1).
- Section 112 (Billing): no plan gates in Work Management now; checks stay behind the entitlement port and allow (D76.2).

# 6. Not covered

## 6.1 Retired use cases

These were catalogued and then decided out of scope. They keep an id so none is dropped silently. They have no state and are not counted in part 1.

| Id | Use case | Capability | Deciding decision |
|---|---|---|---|
| UC-WM-A27 | Set or clear an expiry on access | A | D76.4: no expiry on board access in this scope |
| UC-WM-A28 | Create, use and revoke a board share link | A | D76.4: public board share links are not built in Work Management |
| UC-WM-B37 | Expire a request | B | D76.11: approvals never expire |
| UC-WM-M02 | Set capacity | M | D76.12: workload belongs to a separate roadmap item and is out of board core |
| UC-WM-M03 | View member workload | M | D76.12 |
| UC-WM-M04 | See workload freshness | M | D76.12 |
| UC-WM-M05 | Plan against capacity | M | D76.12 |
| UC-WM-M06 | Present a Dashboard view over board data | M | D76.17: a dashboard is an Analytics surface, not a Work Management view |

## 6.2 Product sections without a use case of their own

| Product sections | Treatment |
|---|---|
| 1 to 4 (Mission, Owns, Does not own, Ubiquitous language) | Context, not behaviour. Every use case cites the rules that apply. |
| 5, 6 and 11 (WM-001, WM-002, WM-004 principles) | Principles. Exercised by UC-WM-I01, UC-WM-J01, UC-WM-K01, UC-WM-L01, UC-WM-H01 and UC-WM-A04. |
| 7 to 9, 14 (Board, identity, flexible settings) | UC-WM-A01 to A11, UC-WM-A02, UC-WM-A10. |
| 15 to 25 (Fields, defaults, system fields, field lifecycle) | UC-WM-C01 to C19, UC-WM-D01, UC-WM-F01, UC-WM-H15. |
| 26 to 37 (Item, values, bulk, queryable values, projection) | UC-WM-B01 to B16, UC-WM-D02 to D06, UC-WM-E06, UC-WM-F03, UC-WM-I03, UC-WM-J02 to J03, UC-WM-J06, UC-WM-D01, UC-WM-T05. |
| 38 to 45 (Groups, Kanban grouping, ordering, rebalance) | UC-WM-E01 to E05, UC-WM-F02, UC-WM-E06, UC-WM-F03, UC-WM-J02 to J03. Rebalance (section 45) is an internal maintenance operation with no user flow (WMREQ053 to WMREQ055); covered by UC-WM-F04. |
| 46 to 53, 61, 62, 150 to 154 (Views) | UC-WM-H01 to H14, UC-WM-I01 to I02, UC-WM-J01, UC-WM-J04 to J05, UC-WM-K01, UC-WM-L01, UC-WM-T01 to T04. |
| 54 to 60 (Forms, public forms) | UC-WM-N01 to N06. |
| 63 to 68.2 (Relations, dependency, assignment access) | UC-WM-C18, UC-WM-B20, UC-WM-B21, UC-WM-B05. |
| 69 to 77.1 (Formula, rollup, progress) | UC-WM-C20 to C21, UC-WM-D07 to D10, UC-WM-T05. |
| 78, 79 (Labels) | UC-WM-C22, UC-WM-D11, UC-WM-H19. |
| 80 to 83 (Checklists) | UC-WM-G01, UC-WM-G02. |
| 84 to 89 (Approvals) | UC-WM-B24 to B28. |
| 90 to 95, 155 (Workload) | Retired, see 6.1. |
| 96 to 98 (Templates) | UC-WM-A12 to A15. |
| 99, 100, 156 to 159 (Boundaries with Approvals, Checklists, Forms, Workload, Labels and Documents) | Boundary rules; cited in the Rules lines of the use cases they limit, with no flow of their own. |
| 101 to 106 (Workspaces, Governance, guests, public sharing) | UC-WM-A03, UC-WM-A08, UC-WM-A16 to A26. Public sharing is retired, see 6.1. |
| 107 to 112 (Documents, Collaboration, Automation, Integrations, Analytics, Billing) | UC-WM-R08, UC-WM-B20 to B21, UC-WM-G01 to G02, UC-WM-O01, UC-WM-R01 to R06, UC-WM-Q01 to Q02, UC-WM-Q03, UC-WM-M01, UC-WM-O03, UC-WM-S01. |
| 113 to 120 (Events, realtime, stability) | UC-WM-O04, UC-WM-B33 to B36, UC-WM-D12, UC-WM-F04, UC-WM-P01 to P04. Sections 118 to 120 (stability) are constraints on UC-WM-O04, UC-WM-Q01, UC-WM-O03, UC-WM-Q03. |
| 121 to 126 (Deletion) | UC-WM-A06, UC-WM-A07, UC-WM-C04, UC-WM-C05, UC-WM-B11 to B13, UC-WM-H03, UC-WM-B17. Section 126 (derived state) is covered by UC-WM-D07, UC-WM-T05. |
| 127 to 132 (Conflicts, stale schema, ordering collision, relation target loss, bulk) | UC-WM-B33 to B36, UC-WM-D12, UC-WM-F04, UC-WM-P04, UC-WM-B14. |
| 133 to 135 (Import, export) | UC-WM-B29 to B32. |
| 136, 137 (Search, analytics queryability) | UC-WM-H16 to H18, UC-WM-O03. |
| 138 to 149 (Time zones, field type detail, status category, formula errors) | UC-WM-H14, UC-WM-C09 to C16, UC-WM-C08, UC-WM-D07. |
| 160, 161 (Source alignment, ambiguity watch) | Informational; the catalog replaces them with measured states. |
| 162 to 174 (Change impact, checklists) | Engineering checklists; UC-WM-C17 carries the field type checklist, UC-WM-B14 and UC-WM-D02 the item mutation checklist. |
| 175 to 178 (Testing, stop conditions, related owners, final rule) | Process rules; the acceptance lines of each use case apply them. |

## 6.3 Endpoints with no use case

Of the 149 registered endpoints (150 `WithName` declarations; one is the unmapped duplicate noted at UC-WM-G02), these three have no use case because they belong to another surface or are admin-only. All other endpoints appear in the Evidence line of at least one use case.

| Operation | Route | Why no use case |
|---|---|---|
| BoardItems.GetHomeFeed | GET /workspaces/{workspaceId:guid}/home/feed | Feeds the Home surface, owned by Workspaces; Work Management only supplies items. |
| Approvals.Delete | DELETE /approvals/{requestId:guid} | Administrative removal of an approval request; no decided user flow. Decision needed before a use case exists. |
| Approvals.Restore | POST /approvals/{requestId:guid}/restore | Restore of the above; same. |

# Notrelix Route Access Matrix

**Purpose:** Inventory every API route with its auth requirement, scope, and permission metadata.

**Source folders scanned:**
- `src/Notrelix.API/Program.cs`
- `src/Notrelix.API/Endpoints/**`

## P0 Risks

1. **No granular permission attributes on any endpoint** — Authorization is handled at Application layer (handler-level), not declaratively at API boundary.
2. **~60 endpoints lack workspace scope** — Resources accessed by direct ID without workspace qualification; relies on Application-layer isolation.
3. **Rate limiting only on auth endpoints** — All other endpoints have no rate limiting.

---

## 1. Health (Public)

| # | Method | Route | Endpoint File | Auth | Account scope | WS scope | Permission | Rate Limit | Risk |
|---|--------|-------|---------------|------|--------------|----------|------------|------------|------|
| 1 | GET | `/health` | HealthEndpoints.cs | Public | No | No | None | None | Safe |
| 2 | GET | `/health/live` | HealthEndpoints.cs | Public | No | No | None | None | Safe |
| 3 | GET | `/health/ready` | HealthEndpoints.cs | Public | No | No | None | None | Safe |

## 2. Admin (Admin-only)

| # | Method | Route | Endpoint File | Auth | Account scope | WS scope | Permission | Rate Limit | Risk |
|---|--------|-------|---------------|------|--------------|----------|------------|------------|------|
| 4 | GET | `/admin/outbox/stats` | OutboxDiagnosticsEndpoints.cs | Admin | No | No | Policy `admin` | None | Safe |
| 5 | GET | `/admin/outbox/pending` | OutboxDiagnosticsEndpoints.cs | Admin | No | No | Policy `admin` | None | Safe |
| 6 | GET | `/admin/outbox/failed` | OutboxDiagnosticsEndpoints.cs | Admin | No | No | Policy `admin` | None | Safe |
| 7 | GET | `/admin/outbox/{id:guid}` | OutboxDiagnosticsEndpoints.cs | Admin | No | No | Policy `admin` | None | Safe |

## 3. Identity — Auth (Public + Authenticated)

| # | Method | Route | Endpoint File | Auth | Account scope | WS scope | Permission | Rate Limit | Risk |
|---|--------|-------|---------------|------|--------------|----------|------------|------------|------|
| 8 | POST | `/api/v1/auth/register` | RegisterEndpoint.cs | Public | No | No | None | AuthStrictByIp (5 req/60s) | Safe |
| 9 | POST | `/api/v1/auth/login` | LoginEndpoint.cs | Public | No | No | None | AuthStrictByIp (5 req/60s) | Safe |
| 10 | POST | `/api/v1/auth/forgot-password` | ForgotPasswordEndpoint.cs | Public | No | No | None | AuthStrictByIp (5 req/60s) | Safe |
| 11 | POST | `/api/v1/auth/reset-password` | ResetPasswordEndpoint.cs | Public | No | No | None | AuthStrictByIp (5 req/60s) | Safe |
| 12 | POST | `/api/v1/auth/refresh` | RefreshTokenEndpoint.cs | Public | No | No | None | AuthStrictByIp (5 req/60s) | Safe |
| 13 | POST | `/api/v1/auth/logout` | LogoutEndpoint.cs | Authenticated | No | No | None | None | Safe |
| 14 | GET | `/api/v1/auth/me` | GetCurrentUserEndpoint.cs | Authenticated | Yes (JWT) | No | None | None | Safe |
| 15 | GET | `/api/v1/auth/bootstrap` | GetBootstrapEndpoint.cs | Authenticated | Yes (JWT) | No | None | None | Safe |

## 4. Identity — Profile (Authenticated)

| # | Method | Route | Endpoint File | Auth | Account scope | WS scope | Permission | Rate Limit | Risk |
|---|--------|-------|---------------|------|--------------|----------|------------|------------|------|
| 16 | PATCH | `/api/v1/profile` | UpdateProfileEndpoint.cs | Authenticated | Yes (JWT) | No | None | None | Safe |

## 5. Workspaces — CRUD (Authenticated)

| # | Method | Route | Endpoint File | Auth | Account scope | WS scope | Permission | Rate Limit | Risk |
|---|--------|-------|---------------|------|--------------|----------|------------|------------|------|
| 17 | GET | `/api/v1/workspaces` | ListUserWorkspacesEndpoint.cs | Authenticated | Yes (JWT) | No | None | None | Safe |
| 18 | POST | `/api/v1/workspaces` | CreateWorkspaceEndpoint.cs | Authenticated | No | No | None | None | Safe |
| 19 | GET | `/api/v1/workspaces/{workspaceId:guid}` | GetWorkspaceEndpoint.cs | Authenticated | No | Path | None | None | Safe |
| 20 | PATCH | `/api/v1/workspaces/{workspaceId:guid}` | UpdateWorkspaceEndpoint.cs | Authenticated | No | Path | None | None | Safe |
| 21 | POST | `/api/v1/workspaces/{workspaceId:guid}/archive` | ArchiveWorkspaceEndpoint.cs | Authenticated | No | Path | None | None | Safe |
| 22 | POST | `/api/v1/workspaces/{workspaceId:guid}/restore` | RestoreWorkspaceEndpoint.cs | Authenticated | No | Path | None | None | Safe |
| 23 | GET | `/api/v1/workspaces/by-slug/{slug}` | GetWorkspaceBySlugEndpoint.cs | Authenticated | No | Header X-Workspace-Id | None | None | Safe |

## 6. Workspaces — Members (Authenticated)

| # | Method | Route | Endpoint File | Auth | Account scope | WS scope | Permission | Rate Limit | Risk |
|---|--------|-------|---------------|------|--------------|----------|------------|------------|------|
| 24 | GET | `/api/v1/workspaces/{workspaceId:guid}/members` | ListMembersEndpoint.cs | Authenticated | No | Path | None | None | Safe |
| 25 | POST | `/api/v1/workspaces/{workspaceId:guid}/members` | InviteMemberEndpoint.cs | Authenticated | No | Path | None | None | Safe |
| 26 | PATCH | `/api/v1/workspaces/{workspaceId:guid}/members/{userId:guid}` | UpdateMemberRoleEndpoint.cs | Authenticated | No | Path | None | None | Safe |
| 27 | DELETE | `/api/v1/workspaces/{workspaceId:guid}/members/{userId:guid}` | RemoveMemberEndpoint.cs | Authenticated | No | Path | None | None | Safe |

## 7. Workspaces — Invitations (Authenticated + Public)

| # | Method | Route | Endpoint File | Auth | Account scope | WS scope | Permission | Rate Limit | Risk |
|---|--------|-------|---------------|------|--------------|----------|------------|------------|------|
| 28 | GET | `/api/v1/workspaces/{workspaceId:guid}/invitations` | ListWorkspaceInvitationsEndpoint.cs | Authenticated | No | Path | None | None | Safe |
| 29 | DELETE | `/api/v1/workspaces/{workspaceId:guid}/invitations/{invitationId:guid}` | CancelInvitationEndpoint.cs | Authenticated | No | Path | None | None | Safe |
| 30 | GET | `/api/v1/invitations/pending` | GetUserPendingInvitationsEndpoint.cs | Authenticated | Yes (JWT) | No | None | None | Safe |
| 31 | POST | `/api/v1/invitations/accept/{token}` | AcceptInvitationEndpoint.cs | Authenticated | No | No | None | None | Safe |
| 32 | GET | `/api/v1/invitations/by-token/{token}` | GetInvitationByTokenEndpoint.cs | Public | No | No | None | None | Mild (token enumeration) |

## 8. Workspaces — Activity (Authenticated)

| # | Method | Route | Endpoint File | Auth | Account scope | WS scope | Permission | Rate Limit | Risk |
|---|--------|-------|---------------|------|--------------|----------|------------|------------|------|
| 33 | GET | `/api/v1/workspaces/{workspaceId:guid}/activity` | GetWorkspaceActivityEndpoint.cs | Authenticated | No | Path | None | None | Safe |

## 9. Documents — Pages (Authenticated)

| # | Method | Route | Endpoint File | Auth | Account scope | WS scope | Permission | Rate Limit | Risk |
|---|--------|-------|---------------|------|--------------|----------|------------|------------|------|
| 34 | GET | `/api/v1/workspaces/{workspaceId:guid}/pages` | ListWorkspacePagesEndpoint.cs | Authenticated | No | Path | None | None | Safe |
| 35 | GET | `/api/v1/workspaces/{workspaceId:guid}/pages/tree` | GetPageTreeEndpoint.cs | Authenticated | No | Path | None | None | Safe |
| 36 | GET | `/api/v1/workspaces/{workspaceId:guid}/pages/search` | SearchPagesEndpoint.cs | Authenticated | No | Path | None | None | Safe |
| 37 | POST | `/api/v1/workspaces/{workspaceId:guid}/pages` | CreatePageEndpoint.cs | Authenticated | No | Path | None | None | Safe |
| 38 | GET | `/api/v1/pages/{pageId:guid}` | GetPageEndpoint.cs | Authenticated | No | No | None | None | Mild (no WS check) |
| 39 | PATCH | `/api/v1/pages/{pageId:guid}` | UpdatePageEndpoint.cs | Authenticated | No | No | None | None | Mild (no WS check) |
| 40 | DELETE | `/api/v1/pages/{pageId:guid}` | DeletePageEndpoint.cs | Authenticated | No | No | None | None | Mild (no WS check) |
| 41 | GET | `/api/v1/pages/{pageId:guid}/breadcrumb` | GetPageBreadcrumbEndpoint.cs | Authenticated | No | No | None | None | Mild (no WS check) |
| 42 | GET | `/api/v1/pages/{pageId:guid}/history` | GetPageHistoryEndpoint.cs | Authenticated | No | No | None | None | Mild (no WS check) |

## 10. Documents — Blocks (Authenticated)

| # | Method | Route | Endpoint File | Auth | Account scope | WS scope | Permission | Rate Limit | Risk |
|---|--------|-------|---------------|------|--------------|----------|------------|------------|------|
| 43 | GET | `/api/v1/pages/{pageId:guid}/blocks` | ListPageBlocksEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 44 | POST | `/api/v1/pages/{pageId:guid}/blocks` | CreateBlockEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 45 | POST | `/api/v1/pages/{pageId:guid}/blocks/batch` | BatchUpdateBlocksEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 46 | PATCH | `/api/v1/blocks/{blockId:guid}` | UpdateBlockEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 47 | DELETE | `/api/v1/blocks/{blockId:guid}` | DeleteBlockEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 48 | POST | `/api/v1/blocks/reorder` | ReorderBlocksEndpoint.cs | Authenticated | No | No | None | None | Mild |

## 11. Work Management — Boards (Authenticated)

| # | Method | Route | Endpoint File | Auth | Account scope | WS scope | Permission | Rate Limit | Risk |
|---|--------|-------|---------------|------|--------------|----------|------------|------------|------|
| 49 | GET | `/api/v1/workspaces/{workspaceId:guid}/boards` | ListWorkspaceBoardsEndpoint.cs | Authenticated | No | Path | None | None | Safe |
| 50 | POST | `/api/v1/workspaces/{workspaceId:guid}/boards` | CreateBoardEndpoint.cs | Authenticated | No | Path | None | None | Safe |
| 51 | GET | `/api/v1/boards/{boardId:guid}` | GetBoardEndpoint.cs | Authenticated | No | Header X-Workspace-Id | None | None | Safe |
| 52 | PATCH | `/api/v1/boards/{boardId:guid}` | RenameBoardEndpoint.cs | Authenticated | No | No | None | None | Mild (no WS check) |
| 53 | GET | `/api/v1/boards/{boardId:guid}/full` | GetBoardOverviewEndpoint.cs | Authenticated | No | No | None | None | Mild (no WS check) |
| 54 | POST | `/api/v1/boards/{boardId:guid}/archive` | ArchiveBoardEndpoint.cs | Authenticated | No | Hint via GetWorkspaceIdHint() | None | None | Mild |
| 55 | POST | `/api/v1/boards/{boardId:guid}/unarchive` | UnarchiveBoardEndpoint.cs | Authenticated | No | No | None | None | Mild (no WS check) |
| 56 | GET | `/api/v1/boards/{boardId:guid}/members` | GetBoardMembersEndpoint | Authenticated | No | Header X-Workspace-Id | None | None | Safe |
| 57 | POST | `/api/v1/boards/{boardId:guid}/members` | AddBoardMemberEndpoint | Authenticated | No | No | None | None | Mild (no WS check) |
| 58 | DELETE | `/api/v1/boards/{boardId:guid}/members/{userId:guid}` | RemoveBoardMemberEndpoint | Authenticated | No | No | None | None | Mild (no WS check) |

## 12. Work Management — Board Fields (Authenticated)

| # | Method | Route | Endpoint File | Auth | Account scope | WS scope | Permission | Rate Limit | Risk |
|---|--------|-------|---------------|------|--------------|----------|------------|------------|------|
| 59 | POST | `/api/v1/boards/{boardId:guid}/fields` | MapBoardFieldEndpoints.cs | Authenticated | No | Header X-Workspace-Id | None | None | Safe |
| 60 | PATCH | `/api/v1/boards/{boardId:guid}/fields/{fieldId:guid}` | MapBoardFieldEndpoints.cs | Authenticated | No | Header X-Workspace-Id | None | None | Safe |
| 61 | DELETE | `/api/v1/boards/{boardId:guid}/fields/{fieldId:guid}` | MapBoardFieldEndpoints.cs | Authenticated | No | Header X-Workspace-Id | None | None | Safe |
| 62 | POST | `/api/v1/boards/{boardId:guid}/fields/reorder` | MapBoardFieldEndpoints.cs | Authenticated | No | No | None | None | Mild |
| 63 | GET | `/api/v1/boards/{boardId:guid}/schema` | MapBoardFieldEndpoints.cs | Authenticated | No | Header X-Workspace-Id | None | None | Safe |

## 13. Work Management — Board Views (Authenticated)

| # | Method | Route | Endpoint File | Auth | Account scope | WS scope | Permission | Rate Limit | Risk |
|---|--------|-------|---------------|------|--------------|----------|------------|------------|------|
| 64 | GET | `/api/v1/boards/{boardId:guid}/views` | GetBoardViewEndpoint.cs | Authenticated | No | Header X-Workspace-Id | None | None | Safe |
| 65 | PUT | `/api/v1/boards/{boardId:guid}/views` | SaveBoardViewEndpoint.cs | Authenticated | No | Header X-Workspace-Id | None | None | Safe |
| 66 | POST | `/api/v1/boards/{boardId:guid}/views` | CreateBoardViewEndpoint.cs | Authenticated | No | Header X-Workspace-Id | None | None | Safe |
| 67 | PATCH | `/api/v1/boards/{boardId:guid}/views/{viewId:guid}` | UpdateBoardViewConfigEndpoint.cs | Authenticated | No | Header X-Workspace-Id | None | None | Safe |
| 68 | DELETE | `/api/v1/boards/{boardId:guid}/views/{viewId:guid}` | DeleteBoardViewEndpoint.cs | Authenticated | No | Header X-Workspace-Id | None | None | Safe |

## 14. Work Management — Board Items (Authenticated)

| # | Method | Route | Endpoint File | Auth | Account scope | WS scope | Permission | Rate Limit | Risk |
|---|--------|-------|---------------|------|--------------|----------|------------|------------|------|
| 69 | GET | `/api/v1/boards/{boardId:guid}/items` | ListBoardItemsEndpoint.cs | Authenticated | No | Header X-Workspace-Id | None | None | Safe |
| 70 | POST | `/api/v1/boards/{boardId:guid}/items` | CreateBoardItemEndpoint.cs | Authenticated | No | Header X-Workspace-Id | None | None | Safe |
| 71 | GET | `/api/v1/board-items/{itemId:guid}` | GetBoardItemEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 72 | PATCH | `/api/v1/board-items/{itemId:guid}` | UpdateBoardItemEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 73 | POST | `/api/v1/board-items/{itemId:guid}/archive` | ArchiveBoardItemEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 74 | POST | `/api/v1/board-items/{itemId:guid}/duplicate` | DuplicateBoardItemEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 75 | POST | `/api/v1/board-items/{itemId:guid}/move` | MoveBoardItemEndpoint.cs | Authenticated | No | Header X-Workspace-Id + X-Board-Id | None | None | Safe |
| 76 | PATCH | `/api/v1/board-items/{itemId:guid}/field-values` | UpdateBoardItemFieldValuesEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 77 | PATCH | `/api/v1/board-items/{itemId:guid}/values/{fieldId:guid}` | UpdateBoardItemFieldValueEndpoint.cs | Authenticated | No | Header X-Workspace-Id + X-Board-Id | None | None | Safe |
| 78 | POST | `/api/v1/board-items/{itemId:guid}/assignees` | AssignBoardItemMemberEndpoint.cs | Authenticated | No | Header X-Workspace-Id | None | None | Safe |
| 79 | DELETE | `/api/v1/board-items/{itemId:guid}/assignees/{userId:guid}` | UnassignBoardItemMemberEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 80 | POST | `/api/v1/board-items/{itemId:guid}/link-page` | LinkPageToBoardItemEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 81 | DELETE | `/api/v1/board-items/{itemId:guid}/link-page` | UnlinkPageFromBoardItemEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 82 | POST | `/api/v1/board-items/{itemId:guid}/labels` | AddLabelToBoardItemEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 83 | DELETE | `/api/v1/board-items/{itemId:guid}/labels/{labelId:guid}` | RemoveLabelFromBoardItemEndpoint.cs | Authenticated | No | No | None | None | Mild |

## 15. Work Management — Board Groups (Authenticated)

| # | Method | Route | Endpoint File | Auth | Account scope | WS scope | Permission | Rate Limit | Risk |
|---|--------|-------|---------------|------|--------------|----------|------------|------------|------|
| 84 | POST | `/api/v1/boards/{boardId:guid}/groups` | CreateBoardGroupEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 85 | POST | `/api/v1/boards/{boardId:guid}/groups/reorder` | ReorderBoardGroupsEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 86 | PATCH | `/api/v1/board-groups/{groupId:guid}` | UpdateBoardGroupEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 87 | POST | `/api/v1/board-groups/{groupId:guid}/duplicate` | DuplicateBoardGroupEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 88 | DELETE | `/api/v1/board-groups/{groupId:guid}` | ArchiveBoardGroupEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 89 | POST | `/api/v1/board-groups/{groupId:guid}/unarchive` | UnarchiveBoardGroupEndpoint.cs | Authenticated | No | No | None | None | Mild |

## 16. Work Management — Checklists (Authenticated)

| # | Method | Route | Endpoint File | Auth | Account scope | WS scope | Permission | Rate Limit | Risk |
|---|--------|-------|---------------|------|--------------|----------|------------|------------|------|
| 90 | GET | `/api/v1/board-items/{itemId:guid}/checklists` | GetChecklistsEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 91 | POST | `/api/v1/board-items/{itemId:guid}/checklists` | CreateChecklistEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 92 | PATCH | `/api/v1/checklists/{checklistId:guid}` | UpdateChecklistEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 93 | DELETE | `/api/v1/checklists/{checklistId:guid}` | DeleteChecklistEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 94 | POST | `/api/v1/checklists/{checklistId:guid}/items` | CreateChecklistItemEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 95 | PATCH | `/api/v1/checklist-items/{itemId:guid}` | UpdateChecklistItemEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 96 | DELETE | `/api/v1/checklist-items/{itemId:guid}` | DeleteChecklistItemEndpoint.cs | Authenticated | No | No | None | None | Mild |

## 17. Work Management — Labels (Authenticated)

| # | Method | Route | Endpoint File | Auth | Account scope | WS scope | Permission | Rate Limit | Risk |
|---|--------|-------|---------------|------|--------------|----------|------------|------------|------|
| 97 | GET | `/api/v1/boards/{boardId:guid}/labels` | ListLabelsEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 98 | POST | `/api/v1/boards/{boardId:guid}/labels` | CreateLabelEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 99 | PATCH | `/api/v1/labels/{labelId:guid}` | UpdateLabelEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 100 | DELETE | `/api/v1/labels/{labelId:guid}` | DeleteLabelEndpoint.cs | Authenticated | No | No | None | None | Mild |

## 18. Collaboration — Comments (Authenticated)

| # | Method | Route | Endpoint File | Auth | Account scope | WS scope | Permission | Rate Limit | Risk |
|---|--------|-------|---------------|------|--------------|----------|------------|------------|------|
| 101 | GET | `/api/v1/board-items/{boardItemId:guid}/comments` | GetCommentsEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 102 | POST | `/api/v1/board-items/{boardItemId:guid}/comments` | CreateCommentEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 103 | GET | `/api/v1/pages/{pageId:guid}/comments` | GetCommentsEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 104 | POST | `/api/v1/pages/{pageId:guid}/comments` | CreateCommentEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 105 | PATCH | `/api/v1/comments/{commentId:guid}` | UpdateCommentEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 106 | DELETE | `/api/v1/comments/{commentId:guid}` | DeleteCommentEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 107 | POST | `/api/v1/comments/{commentId:guid}/resolve` | ResolveCommentEndpoint.cs | Authenticated | No | No | None | None | Mild |

## 19. Collaboration — Attachments (Authenticated)

| # | Method | Route | Endpoint File | Auth | Account scope | WS scope | Permission | Rate Limit | Risk |
|---|--------|-------|---------------|------|--------------|----------|------------|------------|------|
| 108 | GET | `/api/v1/board-items/{boardItemId:guid}/attachments` | GetAttachmentsEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 109 | POST | `/api/v1/board-items/{boardItemId:guid}/attachments` | CreateAttachmentEndpoint.cs | Authenticated | No | No | None | None | Mild |
| 110 | DELETE | `/api/v1/board-items/{boardItemId:guid}/attachments/{attachmentId:guid}` | DeleteAttachmentEndpoint.cs | Authenticated | No | No | None | None | Mild |

## 20. Collaboration — Activity (Authenticated)

| # | Method | Route | Endpoint File | Auth | Account scope | WS scope | Permission | Rate Limit | Risk |
|---|--------|-------|---------------|------|--------------|----------|------------|------------|------|
| 111 | GET | `/api/v1/board-items/{boardItemId:guid}/activity` | GetResourceActivityEndpoint.cs | Authenticated | No | No | None | None | Mild |

## 21. Governance — Resource Permissions (Authenticated)

| # | Method | Route | Endpoint File | Auth | Account scope | WS scope | Permission | Rate Limit | Risk |
|---|--------|-------|---------------|------|--------------|----------|------------|------------|------|
| 112 | GET | `/api/v1/resources/{resourceType}/{resourceId:guid}/permissions` | GetResourcePermissionsEndpoint.cs | Authenticated | No | Path (workspaceId) | None | None | Safe |
| 113 | POST | `/api/v1/resources/{resourceType}/{resourceId:guid}/permissions` | GrantResourcePermissionEndpoint.cs | Authenticated | No | Path (workspaceId) | None | None | Safe |
| 114 | DELETE | `/api/v1/resources/{resourceType}/{resourceId:guid}/permissions/{permissionId:guid}` | RevokeResourcePermissionEndpoint.cs | Authenticated | No | Path (workspaceId) | None | None | Safe |

## 22. Governance — Share Links (Authenticated)

| # | Method | Route | Endpoint File | Auth | Account scope | WS scope | Permission | Rate Limit | Risk |
|---|--------|-------|---------------|------|--------------|----------|------------|------------|------|
| 115 | POST | `/api/v1/resources/{resourceType}/{resourceId:guid}/share-links` | CreateShareLinkEndpoint.cs | Authenticated | No | Path (workspaceId) | None | None | Safe |
| 116 | DELETE | `/api/v1/resources/{resourceType}/{resourceId:guid}/share-links/{shareLinkId:guid}` | DisableShareLinkEndpoint.cs | Authenticated | No | Path (workspaceId) | None | None | Safe |

## 23. Automation — Rules (Authenticated)

| # | Method | Route | Endpoint File | Auth | Account scope | WS scope | Permission | Rate Limit | Risk |
|---|--------|-------|---------------|------|--------------|----------|------------|------------|------|
| 117 | GET | `/api/v1/workspaces/{workspaceId:guid}/automations` | ListAutomationRulesEndpoint.cs | Authenticated | No | Path | None | None | Safe |
| 118 | POST | `/api/v1/workspaces/{workspaceId:guid}/automations` | CreateAutomationRuleEndpoint.cs | Authenticated | No | Path | None | None | Safe |
| 119 | PATCH | `/api/v1/workspaces/{workspaceId:guid}/automations/{automationId:guid}/enabled` | SetAutomationRuleEnabledEndpoint.cs | Authenticated | No | Path | None | None | Safe |

## 24. Automation — Executions (Authenticated)

| # | Method | Route | Endpoint File | Auth | Account scope | WS scope | Permission | Rate Limit | Risk |
|---|--------|-------|---------------|------|--------------|----------|------------|------------|------|
| 120 | GET | `/api/v1/automations/{automationId:guid}/executions` | ListAutomationExecutionsEndpoint.cs | Authenticated | No | No | None | None | Mild |

---

## Summary

| Metric | Count |
|--------|-------|
| Public endpoints | 6 |
| Admin-only endpoints | 4 |
| Authenticated endpoints | 110 |
| Workspace-scoped (path) | ~35 |
| Workspace-scoped (header X-Workspace-Id) | ~15 |
| No workspace scope (Mild risk) | ~60 |
| Rate-limited endpoints | 5 (auth only) |

**Pattern:** All endpoints use `ISender.Send()` MediatR. No granular `[Authorize(Roles="...")]` or `IRequirePermission` attributes at API level — all authorization delegated to Application layer.

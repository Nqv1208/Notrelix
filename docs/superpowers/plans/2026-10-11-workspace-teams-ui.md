# Workspace Teams Management + Space Picker Windowing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let workspace Owners/Admins create Teams, manage Team members (Lead/Member) and archive Teams from the web app, and window the Space tree picker in the move dialog.

**Architecture:** Backend first (Workspaces context): expose the Team aggregate version and a Team members read, bind `expectedVersion` on every Team write, then regenerate contracts. Frontend mirrors the Space org-tree feature slice exactly (`core/model` + `core/api` service + query keys + `createUse*` hooks + pure surfaces + fixtures/stories/interaction tests + manifest + app routes + mock backend + e2e). Picker windowing reuses `libraryWindow` with fixed-height rows.

**Tech Stack:** .NET 9 (MediatR pipeline, EF Core, xUnit, Testcontainers PostgreSQL), TypeScript, React, TanStack Query/Router, shadcn/Radix (`@notrelix/ui-web`), i18next (`@notrelix/i18n`), Vitest, Playwright, mock backend (`@notrelix/mock-backend`).

**Spec:** `docs/product/workspaces.md` §45–49 (WSP-014, §47 Team role, WSP-025), `docs/product/governance.md` §23.4 GOV-028 (ManageTeams → workspace Owner, Admin), `docs/workstreams/executions/workspace-experience/workspace-experience.plan.md` Phase 8 open items (lines 259–260). Baseline `develop` @ `4c98b66c`.

## Global Constraints

- Never stage or modify: `docker-compose.dev.yml`, `frontend/apps/web/src/shell/sidebar/sidebar.tsx`, `frontend/apps/web/vite.config.ts`, `frontend/packages/product/work-management/**`, `docs/workstreams/completion-assessment.md`, `docs/workstreams/work-management-table-be-dependencies.md`, `tmp-tac-m12-record.md`, `.playwright-mcp/`. Never reset/checkout/clean/force. Do not bypass hooks (state the facts and retry).
- Commit messages end with `Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>` and `Claude-Session: https://claude.ai/code/session_01QRjKSdX1C21xG9QVsAbRRW`.
- Versioned writes require `expectedVersion > 0`; stale → 412 `common.precondition-failed` (`backend/src/Notrelix.Infrastructure/Data/EfRequestDataSession.cs:81-88`).
- Permissions: Team writes keep `PermissionAction.ManageTeams` on `workspaces.workspace` (Owner + Admin per GOV-028); reads keep `ViewWorkspace`.
- Generated files are never hand-edited: OpenAPI via `dotnet run --project src/Notrelix.API/Notrelix.API.csproj -c Release --no-build -- --export-openapi <ABSOLUTE path to backend/contracts/openapi/notrelix.v1.json>` with env `DOTNET_ENVIRONMENT=Development Messaging__Transport=InMemory Database__MigrateOnStartup=false SeedData__RunOnStartup=false`; frontend via `pnpm codegen`.
- All UI copy in `vi` and `en` (`pnpm check:i18n`); design tokens only (`pnpm check:ui-purity` must report 0 new); overlays use the z-index scale; link-coloured text uses `text-link`.
- Every pure surface ships fixtures → stories → interaction tests with `FUI[<surfaceId>:<caseId>]` markers → a row in `frontend/packages/features/workspace/verification/ui-evidence.manifest.json`.
- Mock e2e: port 5174 may be held by the user's dev server; if so run with a copy of `playwright.mock.config.ts` on another port placed in `frontend/node_modules/.cache/*.mjs` (not the repo) and say so.

## Review Focus

1. Unknown `role` string on AddTeamMember/ChangeTeamMemberRole (e.g. `"Owner"`) must be a 400 validation failure, not an unhandled `ArgumentException` → Task 2 test `AddTeamMember_UnknownRole_FailsValidation`.
2. A stale `expectedVersion` on RemoveTeamMember must return 412 and leave the member active → Task 2 integration test.
3. Team member list and `memberCount` must agree and both exclude `Removed` members → Task 1 test `GetTeamMembers_ExcludesRemovedMembers_AndCountMatches`.
4. The creator Lead is created without `WorkspaceMemberId` (`TeamFactory.CreateWithLead`); removing or demoting that Lead must follow the last-Lead rule like any other → Task 2 integration test `LastLead_CannotBeRemovedOrDemoted`.
5. A workspace Member (not Admin) must see Teams read-only with the reason, and the server must still refuse writes (403) → Task 6 `FUI[workspace.teams:read-only]`, Task 2 integration `ManageTeams_DeniedForMember`.

---

## Backend

### Task 1: Team read contract — version, status, members

**Files:**
- Modify: `backend/src/Notrelix.Application/Features/Workspaces/Teams/Queries/GetTeam/GetTeam.cs` (TeamDto record, lines 6-13; projection ~42-44)
- Modify: `backend/src/Notrelix.Application/Features/Workspaces/Teams/Queries/GetWorkspaceTeams/GetWorkspaceTeams.cs` (projection ~33-40)
- Create: `backend/src/Notrelix.Application/Features/Workspaces/Teams/Queries/GetTeamMembers/GetTeamMembers.cs`
- Create: `backend/src/Notrelix.API/Endpoints/Workspaces/Teams/Queries/GetTeamMembersEndpoint.cs`; register in `backend/src/Notrelix.API/Endpoints/Workspaces/Teams/MapTeamEndpoints.cs`
- Test: `backend/tests/Notrelix.Application.Tests/Features/Workspaces/Teams/Queries/GetTeamMembersQueryHandlerTests.cs`
- Test: `backend/tests/Notrelix.Integration.Tests/Workspaces/TeamAdministrationReadIntegrationTests.cs`

**Interfaces:**
- Produces: `record TeamDto(Guid Id, string Name, string? Description, string Status, int MemberCount, long Version, DateTime CreatedAt)` — `Status` is the `TeamStatus` name (`"Active" | "Archived"`); `IsArchived` is removed (its only consumer is `parseTeams` in `spaces.service.ts`, updated in Task 3). `MemberCount` counts `TeamMemberStatus.Active` only.
- Produces: `record TeamMemberDto(Guid UserId, string DisplayName, string? Email, string Role, string Status)` — `Role` is the `TeamMemberRole` name; only `Active` rows are returned. Join users the same way `GetWorkspaceMembers` does (`.../Members/Queries/GetWorkspaceMembers/GetWorkspaceMembers.cs`).
- Produces: `record GetTeamMembersQuery(Guid WorkspaceId, Guid TeamId) : IQuery<IReadOnlyList<TeamMemberDto>>, IWorkspaceRequest, IRequirePermission` with `PermissionAction.ViewWorkspace` (copy the markers from `GetTeamQuery`). NotFound when the Team is deleted or belongs to another workspace.
- Produces: endpoint `GET /api/v1/workspaces/{workspaceId}/teams/{teamId}/members`, `WithName("Workspaces.Teams.GetTeamMembers")`, `Produces<IReadOnlyList<TeamMemberDto>>()`.

- [ ] **Step 1: Write failing Application tests** in `GetTeamMembersQueryHandlerTests.cs` (base `WorkspaceHandlerTestBase`, `TestDbSet` stub): `GetTeamMembers_ReturnsActiveMembersWithRole`, `GetTeamMembers_ExcludesRemovedMembers_AndCountMatches` (assert `TeamDto.MemberCount == members.Count`), `GetTeamMembers_OtherWorkspace_IsNotFound`.
- [ ] **Step 2: Run** `dotnet test tests/Notrelix.Application.Tests --filter GetTeamMembers` → FAIL (type missing).
- [ ] **Step 3: Implement** the DTO changes, the query/handler, and the endpoint; fix both existing projections to count `Status == TeamMemberStatus.Active` and to emit `Status`/`Version`.
- [ ] **Step 4: Run** the filter again → PASS; run `dotnet test tests/Notrelix.Architecture.Tests` → PASS (request-marker inventories include the new query).
- [ ] **Step 5: Integration test** `TeamAdministrationReadIntegrationTests` (pattern: `WorkspaceAdministrationReadIntegrationTests.cs`): create a Team via `CreateTeamCommand`, add + remove a member, assert `GET members` lists only active rows, `TeamDto.Version` equals the aggregate version, and a non-member gets NotFound. Run `dotnet test tests/Notrelix.Integration.Tests --filter TeamAdministrationRead` → PASS.
- [ ] **Step 6: Commit** `feat(workspaces): Team reads expose status, version and the active member list`.

### Task 2: Expected version and validation on Team writes

**Files:**
- Modify: `backend/src/Notrelix.Application/Features/Workspaces/Teams/Commands/*/*.cs` (RenameTeam, UpdateTeamDescription, ArchiveTeam, UnarchiveTeam, DeleteTeam, AddTeamMember, RemoveTeamMember, ChangeTeamMemberRole) — add `long ExpectedVersion`, implement `IExpectedVersionRequest` exactly as `RenameSpaceCommand` does; register the Team target in `ExpectedVersionTargetMap` the way the Space target was added in commit `ec529179` (`backend/src/Notrelix.Infrastructure/Data/Concurrency/ExpectedVersionTargetMap.cs`).
- Modify: `backend/src/Notrelix.API/Contracts/Workspaces/Teams/Requests/*.cs` — add `long ExpectedVersion` to `RenameTeamRequest`, `UpdateTeamDescriptionRequest`, `AddTeamMemberRequest`, `ChangeTeamMemberRoleRequest`; create `ArchiveTeamRequest(long ExpectedVersion)` reused by archive/unarchive; `DeleteTeam` and `RemoveTeamMember` read `[FromQuery] long expectedVersion` (same as `RemoveSpaceMemberEndpoint.cs` after commit `3056966b`). `RestoreTeam` stays version-less (deleted aggregate; mirror `RestoreSpace`).
- Modify: the eight write endpoints — `.Produces(StatusCodes.Status204NoContent)` and drop the phantom 200.
- Modify: validators for AddTeamMember/ChangeTeamMemberRole — `RuleFor(x => x.Role).Must(r => Enum.TryParse<TeamMemberRole>(r, true, out _))`.
- Test: `backend/tests/Notrelix.Application.Tests/Features/Workspaces/Teams/Commands/*` (existing + new), `backend/tests/Notrelix.API.Tests/Contracts/WorkspacesOpenApiShapeTests.cs`, `backend/tests/Notrelix.Integration.Tests/Workspaces/TeamWriteConcurrencyIntegrationTests.cs`, `backend/tests/Notrelix.Architecture.Tests/Pipeline/ExpectedVersionArchitectureTests.cs` (inventory).

**Interfaces:**
- Consumes: Task 1 DTOs.
- Produces: every Team write except Restore carries `expectedVersion` (body for PUT/POST, query for DELETE); OpenAPI shows 204 for them.

- [ ] **Step 1: Failing tests** — `AddTeamMember_UnknownRole_FailsValidation` (400, no handler call); update existing command handler tests to pass `ExpectedVersion: 1`; in `WorkspacesOpenApiShapeTests` add `[InlineData("Workspaces.Teams.DeleteTeam")]`, `[InlineData("Workspaces.Teams.RemoveTeamMember")]` to `VersionedDelete_ReadsExpectedVersionFromTheQuery` and a new theory `TeamWrite_Declares204` over the eight operation ids.
- [ ] **Step 2: Run** `dotnet test tests/Notrelix.Application.Tests --filter Teams` and `tests/Notrelix.API.Tests --filter WorkspacesOpenApiShape` → FAIL.
- [ ] **Step 3: Implement** commands, requests, endpoints, validators, target map.
- [ ] **Step 4: Export OpenAPI** (command in Global Constraints) and rerun both filters → PASS. Run `dotnet test tests/Notrelix.Architecture.Tests` → PASS (update the expected-version inventory if the test enumerates request types).
- [ ] **Step 5: Integration test** `TeamWriteConcurrencyIntegrationTests` (PostgreSQL, pattern `SpacePlacementIntegrationTests`): `Rename_WithCurrentVersion_Succeeds_AndStaleVersionIs412`; `RemoveTeamMember_StaleVersion_Is412_AndMemberStaysActive`; `LastLead_CannotBeRemovedOrDemoted` (asserts rule codes `Workspaces_Team_CannotRemoveLastLead`, `Workspaces_Team_CannotDowngradeLastLead`); `ManageTeams_DeniedForMember` (workspace Member → forbidden; Admin → allowed). Run `--filter TeamWriteConcurrency` → PASS.
- [ ] **Step 6: Full gates** `dotnet build backend.slnx -c Release`; Domain/Application/Architecture/API suites; full Integration suite; `cmp` OpenAPI drift → all PASS. Report counts.
- [ ] **Step 7: Commit** `feat(workspaces): bind expectedVersion on Team writes and validate the Team role`. Note in the message: no frontend consumer used Team writes (consumer surface lists only `Workspaces.Teams.GetWorkspaceTeams`), so the request-shape change has no active consumer.

### Task 3: Regenerate frontend contract and endpoint map

**Files:**
- Modify (generated): `frontend/packages/foundation/contracts/src/generated/rest/schema.ts` via `pnpm codegen`
- Modify: `frontend/packages/foundation/contracts/src/endpoints/endpoints.ts:78-79` — add next to `teams`: `team: (ws, teamId) => \`/workspaces/${ws}/teams/${teamId}\``, `teamDescription`, `teamArchive`, `teamUnarchive`, `teamMembers: (ws, teamId)`, `teamMember: (ws, teamId, userId)`, `teamMemberRole: (ws, teamId, userId)`.
- Modify: `frontend/packages/features/workspace/src/core/api/spaces.service.ts:127-140` `parseTeams` — read `status === "Active"` instead of `isArchived`, keep `memberCount`.
- Modify: `frontend/packages/dev/mock-backend/src/contexts/workspace/spaces.handlers.ts:539-561` (`workspace.teams.list` emits `status`, `version`, active `memberCount`).

- [ ] **Step 1:** `cd frontend && pnpm codegen` → `schema.ts` gains `TeamMemberDto`, `expectedVersion` fields and 204 responses.
- [ ] **Step 2:** Edit `endpoints.ts` and `parseTeams`; run `pnpm typecheck` and `pnpm test:node:guarded` → PASS (`workspace-spaces-model.unit.test.ts` team fixtures updated to `status`).
- [ ] **Step 3:** Update the mock list handler; `pnpm mock:freeze:check` → PASS.
- [ ] **Step 4: Commit** `chore(contracts): regenerate the Team contract and map the Team endpoints`.

## Frontend

### Task 4: Teams core model and service

**Files:**
- Create: `frontend/packages/features/workspace/src/core/model/teams.ts`
- Create: `frontend/packages/features/workspace/src/core/api/teams.service.ts`
- Modify: `frontend/packages/features/workspace/src/core/index.ts` (export both), `frontend/packages/features/workspace/src/query/keys.ts` (add `team: (ws, teamId) => workspaceQueryKey(ws, "workspace", "teams", teamId)`, `teamMembers: (ws, teamId) => … "teams", teamId, "members"`)
- Test: `frontend/packages/features/workspace/src/__tests__/workspace-teams-model.unit.test.ts`

**Interfaces:**
- Produces (model): `TEAM_ROLES = ["Lead","Member"] as const`, `TeamRole`, `interface Team { id; name; description: string|null; status: "Active"|"Archived"; memberCount: number; version: number; createdAt: string }`, `interface TeamMember { userId; displayName; email: string|null; role: TeamRole }`, `parseTeam(raw: unknown): Team`, `parseTeamMember(raw: unknown): TeamMember`, `countLeads(members: readonly TeamMember[]): number`, `teamCapabilities(team: Team, viewerRole: WorkspaceRole|null): { canManage: boolean; canEditMembers: boolean; blockedBy: "archived"|"not-admin"|null }` (uses `isWorkspaceAdmin` from `./members`), `teamMemberCapabilities(member: TeamMember, members: readonly TeamMember[], team: Team, canManage: boolean): { roles: readonly TeamRole[]; canRemove: boolean; blockedBy: "last-lead"|"archived"|"not-admin"|null }` (shape of `spaceMemberCapabilities`, `core/model/spaces.ts:442-454`), `addableTeamMembers(workspaceMembers: readonly WorkspaceMember[], members: readonly TeamMember[]): WorkspaceMember[]` (active workspace members not already in the Team).
- Produces (service): `createTeamsService(api: SpacesApiClient, endpoints: SpacesEndpoints)` returning `{ list(ws): Promise<Team[]>` (includes archived), `get(ws, teamId)`, `members(ws, teamId): Promise<TeamMember[]>`, `create(ws, { name, description }): Promise<string>` (201 body is the id), `rename(ws, teamId, { name, expectedVersion })`, `describe(ws, teamId, { description, expectedVersion })`, `archive(ws, teamId, { expectedVersion })`, `unarchive(...)`, `addMember(ws, teamId, { userId, role, expectedVersion })`, `removeMember(ws, teamId, userId, { expectedVersion })` (query string), `changeMemberRole(ws, teamId, userId, { newRole, expectedVersion })` }; type `TeamsService`. Delete is not offered in this UI (archive only, same as Spaces).

- [ ] **Step 1: Failing unit tests**: `parseTeam` maps `status`/`version`; `teamMemberCapabilities` → the last Lead has `roles: ["Lead"]`, `canRemove: false`, `blockedBy: "last-lead"`; a second Lead can be demoted; archived team → `blockedBy: "archived"`; non-admin → `"not-admin"`; `addableTeamMembers` excludes existing and suspended members; service `removeMember` builds `?expectedVersion=3`.
- [ ] **Step 2: Run** `pnpm --filter @notrelix/features-workspace test -- workspace-teams-model` → FAIL.
- [ ] **Step 3: Implement** model and service.
- [ ] **Step 4: Run** → PASS; `pnpm typecheck` → PASS.
- [ ] **Step 5: Commit** `feat(workspace): Team model, capabilities and service`.

### Task 5: Team query and mutation hooks

**Files:**
- Create: `frontend/packages/features/workspace/src/web/query/hooks/use-teams.ts`
- Create: `frontend/packages/features/workspace/src/web/hooks/mutations/use-team-mutations.ts`
- Modify: `frontend/packages/features/workspace/src/web/index.ts` (new section "Teams (WX-TEAM-001)")
- Test: `frontend/packages/features/workspace/src/web/hooks/mutations/__tests__/use-team-mutations.unit.test.tsx`

**Interfaces:**
- Consumes: Task 4 service; `workspaceQueryKeys.teams/team/teamMembers`.
- Produces: `createUseTeams({api, endpoints})` → `useTeams(workspaceId, enabled=true)` (key `teams`, `staleTime` 60_000); `createUseTeam(...)` → `useTeam(workspaceId, teamId)`; `createUseTeamMembers(...)` → `useTeamMembers(workspaceId, teamId)`; `createUseTeamMutations({api, endpoints})` → `useTeamMutations(workspaceId)` returning `{ create, rename, describe, archive, unarchive, addMember, removeMember, changeRole }` — every mutation `retry: false`; `changeRole` optimistic on `teamMembers` with rollback (copy `updateRole` in `use-member-mutations.ts`); all `onSettled` invalidate `teamMembers(teamId)`, `team(teamId)`, `teams`.

- [ ] **Step 1: Failing test** (pattern of the existing member-mutation tests): `changeRole` patches the cache before the request and rolls back on 412; `addMember` invalidates `teams` and `teamMembers`.
- [ ] **Step 2: Run** → FAIL. **Step 3: Implement.** **Step 4: Run** → PASS.
- [ ] **Step 5: Commit** `feat(workspace): Team query and mutation hooks`.

### Task 6: Teams surfaces (list + create dialog, team detail)

**Files:**
- Create under `frontend/packages/features/workspace/src/web/teams/`: `workspace-teams.copy.ts`, `workspace-teams.catalog.ts` (`defineCatalog("workspace-teams", { vi, en })`), `use-workspace-teams-copy.ts`, `workspace-teams-surface.tsx`, `create-team-dialog.tsx`, `workspace-team-surface.tsx`, `workspace-teams.stories.tsx`, `__tests__/workspace-teams.interaction.component.test.tsx`, `__tests__/workspace-team.interaction.component.test.tsx`
- Create: `frontend/packages/features/workspace/src/verification/workspace-teams-fixtures.ts`
- Modify: `frontend/packages/features/workspace/verification/ui-evidence.manifest.json` (rows `workspace.teams`, `workspace.create-team`, `workspace.team`), `web/index.ts`

**Interfaces:**
- `WorkspaceTeamsSurface` props: `{ copy: WorkspaceTeamsCopy; teams: SectionState<readonly Team[]>; filter: "active"|"archived"; onFilterChange; canManage: boolean; readOnlyReason: string|null; navigation: { getHref(teamId): string; onNavigate(teamId): void }; onCreate(): void; onRetry(): void }`. Dense rows: name (link, `text-link`), member count (`pluralForm`), Archived `Badge`; empty state with "New Team" when `canManage`.
- `CreateTeamDialog` props: `{ copy; open; pending; error: string|null; onOpenChange; onSubmit({ name, description }) }`; name required, max 160; description max 1024 (`Team.cs` limits).
- `WorkspaceTeamSurface` props: `{ copy; team: SectionState<Team>; members: SectionState<readonly TeamMember[]>; viewer: { userId: string|null; role: WorkspaceRole|null }; candidates: readonly WorkspaceMember[]; pendingUserIds: ReadonlySet<string>; saving: boolean; onRename(name); onDescribe(description); onArchive(); onUnarchive(); onAddMember({ userId, role }); onChangeRole(userId, role); onRemove(userId); onRetry() }`. Sections: header with inline rename (`Field`), description `Textarea` with its own save; Members table (name, email, role `Select` Lead/Member using the `workspace-members-surface.tsx:215-240` pattern; a blocked role shows a read-only span with the reason; Remove via `AlertDialog` with `variant="destructive"`); Add-member form (`Select` over `addableTeamMembers` with a text filter, role defaults to `Member`); Danger section archive/unarchive with confirm. Archived team: member edits disabled with `copy.archivedReason`.
- Copy keys include: `nav.teams`, `list.*`, `create.*`, `team.*`, `members.*`, `roles.Lead/Member`, `roleHelp.*`, `blocked.lastLead/archived/notAdmin`, `confirmRemove.*`, `confirmArchive.*`, errors `conflict`, `forbidden`.
- Fixtures: `teamsCopyFixture(locale)`, `teamFixture(overrides)`, `teamMemberFixture(overrides)`, `teams<State>Scenario()` and `team<State>Scenario()` for Default, Loading, Error, Empty, ReadOnly, Archived, EdgeData (Vietnamese names, 40-char names), HighDensity (200 members on the detail).
- Interaction markers: `FUI[workspace.teams:render]`, `:filter`, `:create`, `:read-only`, `:empty`; `FUI[workspace.create-team:validate]`, `:submit`; `FUI[workspace.team:render]`, `:rename`, `:role`, `:last-lead`, `:add`, `:remove`, `:archive`, `:archived-blocked`, `:read-only`, `:high-density`.

- [ ] **Step 1:** Write copy (vi + en), fixtures, and the failing interaction tests listed above (use `renderPureUi`, stub `scrollIntoView`/`hasPointerCapture` for Radix Select).
- [ ] **Step 2: Run** `pnpm --filter @notrelix/features-workspace test:web -- teams` → FAIL.
- [ ] **Step 3: Implement** the three surfaces and stories (Default, Loading, Error, Empty, ReadOnly, Archived, EdgeData, HighDensity; visual targets mobile/tablet/desktop × light/dark); add manifest rows with `stateCoverage`.
- [ ] **Step 4: Run** tests → PASS; `pnpm check:ui-evidence`, `pnpm check:ui-fixtures`, `pnpm check:ui-actions`, `pnpm check:ui-purity`, `pnpm check:i18n` → PASS. Build Storybook (`pnpm storybook:web:build`) and screenshot the Team detail at mobile dark and desktop light; fix overflow of long names/emails.
- [ ] **Step 5: Commit** `feat(workspace): Teams list, create dialog and Team detail surfaces (WX-TEAM-001)`.

### Task 7: App composition — routes, navigation, i18n

**Files:**
- Create: `frontend/apps/web/src/routes/workspaces/$workspaceId/teams.tsx`, `frontend/apps/web/src/routes/workspaces/$workspaceId/teams.$teamId.tsx`, `frontend/apps/web/src/shell/workspace/use-workspace-teams.ts` (hooks bundle built with `useAppRuntime().api` + `endpoints`, like `use-workspace-spaces.ts`)
- Modify: `frontend/apps/web/src/router/routes/workspace.routes.tsx` (two `createRoute`s; `teams` search `filter: z.enum(["active","archived"]).optional()`), `frontend/apps/web/src/router/route-tree.tsx:41-61`, `frontend/apps/web/src/shell/workspace/workspace-destinations.ts` (`routerTarget` case `"teams"` with optional `teamId`, `SECTION_BY_ROUTE_ID`), `frontend/apps/web/src/i18n/catalogs.ts` (add `workspaceTeamsCatalog`)
- Modify: `frontend/packages/features/workspace/src/core/model/navigation.ts:113-135` (`{ section: "teams"; teamId?: string; filter?: "active"|"archived" }`), `core/model/breadcrumb.ts:17-19` + `web/shell/shell-models.ts:118-137` (section label `teams`), `web/shell/workspace-sidebar-surface.tsx:529-542` (manage item `{ id: "teams", label: copy.nav.teams, icon: UsersRound }` shown to every member; writes gated inside), `web/shell/workspace-command-palette-surface.tsx:116-161`, `web/shell/workspace-shell.copy.ts` (`nav.teams` vi/en), `web/shell/__tests__/workspace-shell.interaction.component.test.tsx` (sidebar entry count)

**Interfaces:**
- Consumes: Task 5 hooks, Task 6 surfaces, `useWorkspaceViewer(workspaceId)` (`{ userId, role, members }`), `toastFailure`, `describeWorkspaceFailure` (409/412 → `conflict` → refetch + `copy.errors.conflict`).

- [ ] **Step 1:** Add the destination/section/breadcrumb/nav entries and update the shell sidebar test expectation → run `pnpm --filter @notrelix/features-workspace test:web -- workspace-shell` → PASS.
- [ ] **Step 2:** Implement the two route pages: create → navigate to the new Team; 412/409 → toast `conflict` and refetch; 403 → `forbidden`.
- [ ] **Step 3:** `pnpm typecheck`, `pnpm lint`, `pnpm check:architecture`, `pnpm check:i18n` → PASS.
- [ ] **Step 4: Commit** `feat(web): Teams routes, navigation and breadcrumb`.

### Task 8: Mock backend, consumer surface, e2e

**Files:**
- Modify: `frontend/packages/dev/mock-backend/src/state/mock-spaces.ts:41-49` (`MockTeamRecord` gains `version: number` and `members: Array<{ userId: string; role: "Lead"|"Member" }>` replacing `memberIds`; seed Helpdesk: owner `Lead`, member/viewer `Member`), `contexts/workspace/spaces.handlers.ts` (ops `workspace.teams.get`, `.members`, `.create` (creator = Lead), `.rename`, `.describe`, `.archive`, `.unarchive`, `.addMember` (404 unless an active workspace member; 400 `ruleViolation` on archived), `.removeMember` (400 last Lead), `.changeMemberRole` (400 last-Lead downgrade); every versioned write checks `expectedVersion` → `preconditionFailed()` and bumps `version`)
- Modify: `frontend/tooling/contracts/enabled-consumer-surface.ts` (one `CANONICAL_MOCKED` row per `Workspaces.Teams.*` operation, `consumerSymbol: "createTeamsService"`, `mutation: true` for writes)
- Create: mock conformance case `T-MFB-038` in `frontend/packages/dev/mock-backend/src/__tests__/contract-conformance.unit.test.ts` (create → add → promote → stale 412 → last-Lead 400 → archive)
- Create: `frontend/e2e/mock/workspace-teams.mock.e2e.spec.ts` — one test: open Teams from the sidebar → New Team "Phòng Kế toán" → add a member as Member → change to Lead → demote the original Lead (allowed, two Leads) → remove → archive → Archived filter shows it.

- [ ] **Step 1:** Write `T-MFB-038` and the e2e spec (role-based selectors, English copy, `localStorage notrelix-locale = "en"`); run `pnpm --filter @notrelix/mock-backend test` → FAIL.
- [ ] **Step 2:** Implement state + handlers + consumer rows. Run `pnpm --filter @notrelix/mock-backend test`, `pnpm mock:freeze:check`, `pnpm check:mock-consumer-surface`, `pnpm check:mock-mutation-coverage` → PASS.
- [ ] **Step 3:** Run mock e2e (see Global Constraints for the port) → `workspace-teams` passes and the total is 13; update `e2e:mock:count` if it pins the number.
- [ ] **Step 4: Commit** `feat(mock-backend): Team writes and members with version semantics; Teams e2e`.

### Task 9: Window the Space tree picker in the move dialog (WX-SPACE-005)

**Files:**
- Modify: `frontend/packages/features/workspace/src/web/spaces/move-to-space-dialog.tsx` (rows at 103-106, 211-229; `PickerOption` 255-317)
- Modify: `frontend/packages/features/workspace/src/web/spaces/__tests__/space-dialogs.interaction.component.test.tsx`, `verification/ui-evidence.manifest.json` (`workspace.move-to-space`: add case `windowing`, add `HighDensity` to required states), `workspace-spaces.stories.tsx` (story `MoveToSpaceHighDensity` from `spaceTreeHighDensityFixture()`)

**Interfaces:**
- Consumes: `libraryWindow({ rowCount, rowHeight, scrollTop, viewportHeight, overscan })` (`core/model/library.ts:220-234`); constants pattern from `space-tree-nav.tsx:61-66`.
- Produces: `PickerOption` rows are fixed `h-9` (36px, `PICKER_ROW_HEIGHT = 36`); the `RadioGroup` scroll region (`data-testid="move-to-space-window"`, `max-h-80`) renders only `libraryWindow` rows plus `aria-hidden` spacer rows when `rows.length > PICKER_WINDOW_THRESHOLD (150)`; the selected and the focused option are always rendered; focusing an option scrolls its row into view (as `focusRow` in `space-tree-nav.tsx:245-259`). Prop `windowThreshold?: number` exists for tests.

- [ ] **Step 1: Failing test** `FUI[workspace.move-to-space:windowing]`: render with 525 nodes and `windowThreshold: 150`, expect fewer than 80 `radio` elements, End-key/scroll reaches the last node and it becomes selectable, the selection survives scrolling away; search still narrows to a non-windowed list.
- [ ] **Step 2: Run** → FAIL. **Step 3: Implement.** **Step 4: Run** → PASS; `pnpm check:ui-evidence` → PASS; screenshot the HighDensity story.
- [ ] **Step 5: Commit** `feat(workspace): window the Space picker in the move dialog (WX-SPACE-005)`.

### Task 10: Plan doc and final gates

**Files:**
- Modify: `docs/workstreams/executions/workspace-experience/workspace-experience.plan.md` — new "Phase 9 — Teams" status table (WX-TEAM-001 surfaces, WX-TEAM-002 app composition + mock/e2e, WX-SPACE-005 picker windowing) with evidence; replace open-item lines 259-260 with "Closed in Phase 9"; record the Team contract changes (Tasks 1-2) in the backend gap table.

- [ ] **Step 1:** Edit the plan doc; `make docs-check` → 6 PASS.
- [ ] **Step 2:** Final gates from `frontend/`: `pnpm validate:fast` plus `pnpm check:ui-actions`, `pnpm check:ui-fixtures`, mock e2e. Backend: full suites as in Task 2 Step 6. Report every count.
- [ ] **Step 3: Commit** `docs(workspace-experience): record Phase 9 Teams and picker windowing`.

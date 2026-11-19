---
document_id: WRK-UC-WORKSPACES
document_type: workstream-use-case-catalog
status: active
owner: workspace-governance-team
applies_to:
  - workspaces
  - use-case-catalog
  - capability-acceptance
evidence:
  - docs/workstreams/use-cases/README.md
  - docs/product/workspaces.md
  - docs/workstreams/teams/workspace-governance.md
  - docs/workstreams/executions/workspace-governance/workspace-governance.spec.md
  - docs/workstreams/executions/workspace-governance/workspace-governance.certification.md
  - docs/workstreams/executions/workspace-governance/workspace-governance.tests.md
  - docs/workstreams/executions/workspace-experience/workspace-experience.plan.md
  - backend/src/Notrelix.API/Endpoints/Workspaces/
  - backend/src/Notrelix.Application/Features/Workspaces/
  - backend/src/Notrelix.Domain/Workspaces/
  - frontend/packages/features/workspace/
  - frontend/apps/web/src/routes/workspaces/
review_on:
  - workspace-lifecycle-change
  - workspace-membership-change
  - workspace-invitation-change
  - space-or-team-model-change
  - workspace-switch-change
  - onboarding-change
  - capability-state-change
---

# Use cases: Workspaces

Product meaning: [workspaces](../../product/workspaces.md). Team: [workspace-governance](../teams/workspace-governance.md). Execution packs: governance spec (removed; see git history), governance tests (removed; see git history), governance certification (removed; see git history), [workspace experience plan](../executions/workspace-experience/workspace-experience.plan.md).

Roles, permissions, policy, audit and the admin plane belong to Governance: see [governance.md](governance.md) and [governance product doc](../../product/governance.md). This file names the permission each use case needs and does not redefine it.

Requirement ids cited below are `WGREQ001` to `WGREQ043` (Workspace core, membership, invitations, Teams, Spaces) and a few later ones of the governance spec, plus `WX-*` ids of the experience plan. The product rules are `WSP-001` to `WSP-026`; `NRX-*` ids are repository rules. The word "section" always means a section of the Workspaces product doc.

# 1. Assessment

## 1.1 Capability map

Letters A to J follow the Workspaces list in the team spec (section 4, items 1 to 10). The team spec has no capability for Spaces, Teams, onboarding or Home, although the product doc and the API own them; letters K to N are added by this catalog and Part 5 asks the team spec to adopt them.

| Letter | Capability | Use cases | DEFINED | IMPLEMENTING | INTEGRATING | HARDENING | DONE | BLOCKED |
|---|---|---|---|---|---|---|---|---|
| A | Workspace create | 4 | 1 | 0 | 0 | 0 | 3 | 0 |
| B | Workspace read / resolve | 4 | 0 | 2 | 0 | 0 | 2 | 0 |
| C | Workspace update | 2 | 1 | 0 | 0 | 0 | 1 | 0 |
| D | Workspace lifecycle transition | 6 | 1 | 2 | 1 | 0 | 2 | 0 |
| E | Membership read | 2 | 0 | 0 | 0 | 0 | 2 | 0 |
| F | Membership add / remove / change | 10 | 1 | 2 | 2 | 2 | 2 | 1 |
| G | Invitation create | 9 | 1 | 3 | 0 | 0 | 5 | 0 |
| H | Invitation accept / reject / revoke | 6 | 0 | 0 | 0 | 1 | 5 | 0 |
| I | Workspace selection / switch integration | 6 | 0 | 1 | 0 | 3 | 2 | 0 |
| J | Workspace settings | 4 | 0 | 0 | 0 | 3 | 0 | 1 |
| K | Spaces (added by this catalog) | 15 | 0 | 1 | 5 | 6 | 3 | 0 |
| L | Teams (added by this catalog) | 10 | 0 | 1 | 1 | 8 | 0 | 0 |
| M | Getting started and onboarding (added by this catalog) | 7 | 0 | 0 | 0 | 7 | 0 | 0 |
| N | Home, navigation, search and activity (added by this catalog) | 6 | 1 | 1 | 0 | 4 | 0 | 0 |
|  | Total | 91 | 6 | 13 | 9 | 34 | 27 | 2 |

Twenty-seven use cases are DONE in the index (A01, A02, A04, B01, B04, C01, D03, D04, E01, E02, F02, F05, G01, G03, G04, G05, G06, H01, H02, H03, H05, H06, I02, I04, K01, K02, K15). Four of them (F05, G01, I04 and K01) were accepted 2026-11-02 by an independent acceptance agent (see their Evidence lines). Thirty-four use cases are HARDENING in the index. The rest of this section predates the current index rows; the index rows and State lines are the state of record. Real specs cover these against the real backend: C01 (settings), D03 and D04 (`workspace-delete-restore.real.e2e.spec.ts`, delete, gone for a member, restore), E01 and E02 (members read), I01 (`workspace-isolation.real.e2e.spec.ts`, a switch), H02, H05, H06 (`workspace-invitations-answer.real.e2e.spec.ts`, the invitee declines, is invited again and accepts from the rail list), F02 and F05 (`workspace-member-changes.real.e2e.spec.ts`, role change and removal), G01 and G03 to G06 (`workspace-invitations-manage.real.e2e.spec.ts`, invite, list, re-role, resend, revoke), H01 and H03 (`workspace-invitation-link.real.e2e.spec.ts`, the e-mail link and the join in one step), B01, B04, I02 and I04 (`workspace-read-and-lists.real.e2e.spec.ts`, reads, both lists and the realtime scope) and K01, K02, K15 (`workspace-spaces.real.e2e.spec.ts`, create, read and place). Every other use case that the web app calls is INTEGRATING: the generated client reaches the real producer (`frontend/tooling/contracts/enabled-consumer-surface.ts`), mock end-to-end specs under `frontend/e2e/mock` exercise the screens, but no real spec does. Use cases with a producer and no frontend caller are IMPLEMENTING.

The geometry specs (`design-fidelity-*.mock.e2e.spec.ts`) cover Shell, Home, Members, Space and Command at 1440 px and some narrower sizes. The canvas draws no Workspaces screen at 1440, 1024 and 390 px in both themes, so line 3 is met by a measured diff where an artboard exists (Shell, Space) and by reviewed screenshots at all three widths in both themes where it does not.

## 1.2 The ten most important gaps

1. **Deletion is built, but its downstream reaction is not.** Delete is Owner-only with a typed-name confirmation, a deleted workspace is NotFound to everyone, it is restorable by its Owner for 30 days from Recently deleted, and a purge job erases its data after that and writes an audit fact (D03, D04, owner decision 2026-11-01). Still open: no workspace export exists, the other contexts only have stub consumers for the lifecycle events (`WorkspacesStubConsumers`), provider-side integration cleanup and open realtime sockets are not handled.
2. **Archive is a dead end in the UI.** An archived workspace leaves every list, nothing calls `UnarchiveWorkspace`, and the settings copy says every member loses access while the product says archive keeps history readable (section 60).
3. **Competing writes without an expected version.** Member role, suspend, activate and remove; invitation role, resend and revoke; Space rename, description, type, visibility, archive, delete and restore; Team restore. Two admins can silently overwrite each other (NRX-011).
4. **No retry safety on creation.** `CreateWorkspace` and `InviteMember` are not idempotent requests; a double submit creates `acme` and `acme-3f9a1c`, or a second invitation attempt fails with unstable text.
5. **Unbounded reads.** Members, invitations, Spaces, Space tree, Teams and Team members have no paging or cap, and are read in full by the shell.
6. **Entitlement and account checks are missing or fail open.** `AddMember` now requires an active Account membership (`Workspaces_Member_NotInAccount`) and a seat (`Workspaces_Member_SeatUnavailable`) (fixed for direct add); the seat gate on acceptance is an optional dependency (`IBillingSeatGate? = null`); creation has no plan limit (WSP-004, WSP-011). Gap: seat checks on direct add and on acceptance do not serialize admissions, so two concurrent admissions can exceed the seat limit by one; a separate use case.
7. **Invitations are readable by Guests.** Fixed: `GetWorkspaceInvitations` needs `InviteMember` (lead decision 2026-11-02), so a Member and a Guest get 403 and only Owner and Admin see invited emails (section 82); the list is a cursor page.
8. **Account-wide policy sits in workspace settings.** `EnforceMfa` is stored and returned but enforced nowhere (WSP-012). J04 is BLOCKED.
9. **Offboarding is incomplete.** Removing or leaving does not clean Space and Team entries (F09); SCIM provisioning has no principal (F10 BLOCKED); Account suspension has no effect on workspaces (D06 DEFINED, decided 2026-11-01, not built).
10. **Almost no real end-to-end proof.** Four real specs touch Workspaces (switch, members read and active-member refusal, invitee answers). Create, invite by form, leave, Space and Team flows rely on mock specs only. Twenty operations have no frontend caller (list in 1.3).

## 1.3 Defects in shipped code

| Defect | Where |
|---|---|
| Creation is not idempotent | `backend/src/Notrelix.Application/Features/Workspaces/Workspaces/Commands/CreateWorkspace/CreateWorkspace.cs` |
| Invite is not idempotent and answers a duplicate with plain text | `backend/src/Notrelix.Application/Features/Workspaces/Invitations/Commands/InviteMember/InviteMember.cs` |
| Missing expected version on competing writes (members) | `backend/src/Notrelix.Application/Features/Workspaces/Members/Commands/{UpdateMemberRole,SuspendMember,ActivateMember,RemoveMember}/` |
| Missing expected version (invitations) | `backend/src/Notrelix.Application/Features/Workspaces/Invitations/Commands/{ChangeInvitationRole,ResendInvitation,CancelInvitation}/` |
| Missing expected version (Spaces) | `backend/src/Notrelix.Application/Features/Workspaces/Spaces/Commands/{RenameSpace,UpdateSpaceDescription,ChangeSpaceType,ChangeSpaceVisibility,ArchiveSpace,UnarchiveSpace,DeleteSpace,RestoreSpace}/` |
| Missing expected version (Team restore) | `backend/src/Notrelix.Application/Features/Workspaces/Teams/Commands/RestoreTeam/` |
| `AddMember` accepted any user id (fixed: Account-membership and seat checks), and has no frontend caller | `backend/src/Notrelix.Application/Features/Workspaces/Members/Commands/AddMember/AddMember.cs` |
| Seat gate is optional and fails open | `backend/src/Notrelix.Application/Features/Workspaces/Invitations/Services/InvitationAcceptanceService.cs`, `.../Commands/AcceptInvitation/AcceptInvitation.cs` |
| Invitation list readable by Guests and Members (fixed: `InviteMember`) | `backend/src/Notrelix.Application/Features/Workspaces/Invitations/Queries/GetWorkspaceInvitations/GetWorkspaceInvitations.cs` |
| Unbounded reads | `Members/Queries/GetWorkspaceMembers`, `Invitations/Queries/GetWorkspaceInvitations`, `Spaces/Queries/GetWorkspaceSpaces`, `Spaces/Queries/GetSpaceTree`, `Teams/Queries/GetWorkspaceTeams` (all under `backend/src/Notrelix.Application/Features/Workspaces/`) |
| Dead code, wrong count: query with no endpoint that counts members of every status | `backend/src/Notrelix.Application/Features/Workspaces/WorkspaceHome/Queries/GetWorkspaceDashboard/GetWorkspaceDashboard.cs` |
| Stub: activity read returns a constant empty page | `backend/src/Notrelix.Application/Features/Workspaces/WorkspaceHome/Queries/GetWorkspaceActivity/GetWorkspaceActivity.cs` |
| Stub consumers for lifecycle events | `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Workspaces/WorkspacesStubConsumers.cs` |
| Delete is authorized by `ManageWorkspace` alone | `backend/src/Notrelix.Application/Features/Workspaces/Workspaces/Commands/DeleteWorkspace/DeleteWorkspace.cs` |
| Account-wide MFA flag inside workspace settings | `backend/src/Notrelix.Domain/Workspaces/Workspaces/WorkspaceSettings.cs` |
| Duplicate list endpoints (`GET /api/v1/workspaces`, `GET /api/v1/account/workspaces`, `GET /api/v1/accounts/{accountId}/workspaces`) | `backend/src/Notrelix.API/Endpoints/Workspaces/Workspaces/Queries/`, `.../Members/Queries/ListMyWorkspacesEndpoint.cs` |
| Archive copy contradicts the product (members keep read access) | `frontend/packages/features/workspace/src/web/settings/workspace-settings.copy.ts` |
| Workspaces onboarding logic lives in the web app, not the feature package | `frontend/apps/web/src/routes/onboarding/onboarding-api.ts` |
| A non-member of an existing workspace gets 403 "Access to workspace denied" while a missing workspace gets 404, so every workspace-scoped route tells the two apart (README section 7 asks for the identical NotFound). One change in the gate decides it for every context; many integration tests across contexts assert Forbidden | `backend/src/Notrelix.Application/Common/Behaviors/ExecutionContextBehavior.cs`, `backend/src/Notrelix.Infrastructure/Services/TenantBootstrapStore.cs` |

Operations with no frontend caller (by `enabled-consumer-surface.ts` and a search of the web app and feature packages): `UnarchiveWorkspace`, `DeleteWorkspace`, `RestoreWorkspace`, `TransferOwnership`, `ListUserWorkspaces`, `ResolveSlug`, `GetAccountWorkspaces`, `AddMember`, `UpdateMemberJobTitle`, `DeclineInvitation`, `InviteGuestToResource`, `ResendResourceInvitation`, `RevokeResourceInvitation`, `GetSpace`, `DeleteSpace`, `RestoreSpace`, `DeleteTeam`, `RestoreTeam`, `GetWorkspaceDashboard` (no route), `AuthorizeWorkspaceRealtimeSubscription` (server only).

No mock-only Workspaces path was found: every frontend operation of this context has a producer. The `backendCapabilities` flags (`frontend/apps/web/src/config/backend-capabilities.ts`) hide billing and reports on the real backend; they do not gate Workspaces.

Found sound: every command and query of the context declares its permission through the pipeline marker; `LeaveWorkspace`, `SetDefaultWorkspace` and pin and recent reads use `ViewWorkspace`; Transfer, Suspend, Remove and role changes serialize on a workspace row lock (`IWorkspaceOwnerUpdateLocker`); `RecordRecentResource` and `ProvisionPersonalWorkspace` are idempotent; invitation tokens are stored as hashes.

## 1.4 Enterprise baseline

Products compared and what was taken:

- Slack: only the Primary Owner transfers ownership, takes effect at once and leaves the former owner as Owner; admins deactivate members and a deactivated person keeps messages and is removed from all channels. Source: [Transfer ownership of a workspace or org](https://slack.com/help/articles/204401633) and [Deactivate a member's account](https://slack.com/help/articles/204475027-Deactivate-a-member).
- Asana: admins add and deactivate members; deactivated people can be restored; a removed person's tasks move to a private project for reassignment; Guests are people outside the email domain and are not counted as members. Sources: [Admin Console](https://asana.com/features/admin-security/admin-console.md) and [member and team management](https://help.asana.com/s/article/member-and-team-management).
- Notion: delete workspace needs the typed name and is limited to owners and admins; teamspaces can be archived and restored; leaving a workspace hands ownership on. Third-party guides, not Notion's pages; treat as secondhand. Source: [Delete a workspace in Notion](https://thebricks.com/resources/how-to-delete-a-workspace-in-notion).

| Table-stakes feature | Product doc | This catalog |
|---|---|---|
| Ownership transfer, single accountable owner | Sections 17, 18 | D05 |
| Typed-name confirmation and a defined restore window for delete | Section 61 to 63 (decided 2026-11-01, built) | D03, D04 HARDENING |
| Deactivate and restore a member without losing history | Sections 20, 21 | F03, F04, F05 |
| Reassign a removed person's work | Not stated | Not covered: Work Management owns it (Part 6) |
| Clean a person's access when they leave | Section 19 (open) | F09 |
| Invite many people at once | Not stated | G09 |
| Change the workspace address | Section 48.4 (future) | C02 |
| Plan limit on workspaces and seats | Sections 31, 70 | A03, F01, F04, H02 |
| SCIM provisioning into workspaces | Sections 76, 77 | F10 BLOCKED |
| Admin figures overview | Section 85 | N02 |
| Archive and restore a Team or department | Sections 43.2, 48.1 | K08, K09, L08, L09 |

# 2. Index

| ID | Use case | Capability | Actors | BE | FE | Design | Real e2e | State | Gap |
|---|---|---|---|---|---|---|---|---|---|
| UC-WSP-A01 | Create a workspace in an Account | A | Account member with `CreateWorkspace` | yes | yes | Final-Onboarding | none | DONE | closed 2026-11-03, see the Evidence line |
| UC-WSP-A02 | Provision the personal workspace at registration | A | System | yes | n/a | none | none | DONE | closed 2026-11-03, see the Evidence line |
| UC-WSP-A03 | Stop creation at the plan limit (enterprise baseline) | A | Account member with `CreateWorkspace` | no | no | Final-Billing | none | DEFINED | Billing workspace-count entitlement; gate call; error contract; screen state. |
| UC-WSP-A04 | Make workspace creation retry-safe | A | Account member with `CreateWorkspace` | yes | yes | none | none | DONE | closed 2026-11-03, see the Evidence line |
| UC-WSP-B01 | Read one workspace | B | Any active member, Guest included | yes | yes | BC-Shell-1440 | workspace-read-and-lists.real.e2e.spec.ts | DONE | accepted 2026-11-02, see the Evidence line |
| UC-WSP-B02 | Open a workspace by its address | B | Anyone | yes | no | none | none | IMPLEMENTING | Built: the anonymous resolve route is rate-limited (`GeneralAnonymous`, 60 a minute per IP) and tested (`ResolveSlugRateLimitTests`); an enumeration test shows a deleted and a made-up address get the same refusal (`ResolveSlugEnumerationTests`, integration, CI). Deferred to 0.2 (lead ruling 2026-11-19): 0.1.0 keeps ids in every route, so no frontend caller; the vanity address route is a 0.2 item. |
| UC-WSP-B03 | List the workspaces of an Account | B | Account member with `ViewWorkspace` on the Account scope | yes | no | none | none | IMPLEMENTING | No caller; overlaps with I02 and B04; membership-filter proof. |
| UC-WSP-B04 | List every workspace I belong to (user scope) | B | Any signed-in user | yes | yes | none | workspace-read-and-lists.real.e2e.spec.ts | DONE | closed 2026-11-03, see the Evidence line |
| UC-WSP-C01 | Edit workspace name and description | C | Owner, Admin | yes | yes | none for workspace settings (`Final-Settings` is the Board settings page) | none | DONE | closed 2026-11-03, see the Evidence line |
| UC-WSP-C02 | Change the workspace address (enterprise baseline) | C | Owner, Admin | no | no | none | none | DEFINED | Command, endpoint, event, settings field. |
| UC-WSP-D01 | Archive a workspace | D | Owner | yes | yes | Final-Settings | none | DONE | Every state and rule of the row is covered by tests that ran in this PR, except the pause of automations and integration sync, which is deferred to the Automation/Integrations modules (neither has a live sync yet); see the State line. |
| UC-WSP-D02 | Unarchive a workspace | D | Owner | yes | yes | Wsp-Var-Archived-Banner, Wsp-Var-Danger-Archived | none | DONE | Every state and rule of the row is covered by tests that ran in this PR; see the State line. |
| UC-WSP-D03 | Delete a workspace | D | Owner | yes | yes | none (no workspace danger-zone artboard; built from the archive pattern of the same page) | workspace-delete-restore.real.e2e.spec.ts | DONE | closed 2026-11-03, see the Evidence line |
| UC-WSP-D04 | Restore a deleted workspace | D | Owner | yes | yes | none (the Recently deleted list reuses the archived-workspaces pattern) | workspace-delete-restore.real.e2e.spec.ts | DONE | closed 2026-11-03, see the Evidence line |
| UC-WSP-D05 | Transfer workspace ownership | D | Current Owner | yes | yes | Wsp-Var-Danger-Transfer, -None, -Conflict, -Done, -M, TransferStepUp, -Bad, -Pw, -Expired, -Refused, -M (Workspaces canvas) | none | DONE | Built on the Settings danger zone with a fresh MFA or password step-up proof before the handover; every state and rule tested. |
| UC-WSP-D06 | React to Account suspension or closure | D | System | no | no | Final-States | none | DEFINED | decided 2026-11-01, not built (owner decision 7, §9 of the README): A suspended Account is read-only end to end: automation runs and outbound webhooks are skipped and recorded with reason account.suspended, public forms and share-link writes refuse, reads stay open, nothing is deleted. The effect of Closed is not covered by this decision. |
| UC-WSP-E01 | List workspace members | E | Owner, Admin, Member | yes | yes | none for the roster (`Final-Members`, `Final-M-Members` are the department Members tab) | workspace-members.real.e2e.spec.ts | DONE | closed 2026-11-03, see the Evidence line |
| UC-WSP-E02 | Read my own membership | E | Any active member, Guest included | yes | yes | none | workspace-members.real.e2e.spec.ts | DONE | accepted 2026-11-02, see the Evidence line |
| UC-WSP-F01 | Add an existing user as a member directly | F | Owner, Admin | yes | no | none | none | IMPLEMENTING | Kept for admin use; no screen. Account-membership and seat checks are merged; tests. |
| UC-WSP-F02 | Change a member's workspace role | F | Owner, Admin | yes | yes | Final-Members | workspace-member-changes.real.e2e.spec.ts | DONE | accepted 2026-11-02, see the Evidence line |
| UC-WSP-F03 | Suspend a member | F | Owner, Admin | yes | yes | Final-Members | none | DONE | Every state and rule of the row is covered by tests that ran in this PR; see the State line. |
| UC-WSP-F04 | Activate a suspended member | F | Owner, Admin | yes | yes | Final-Members | none | DONE | Every state and rule of the row is covered by tests that ran in this PR; see the State line. A seat refusal shows the producer's reason only; upgrading belongs to Billing (decision 2026-11-19). |
| UC-WSP-F05 | Remove a member | F | Owner, Admin | yes | yes | Final-Members | workspace-member-changes.real.e2e.spec.ts | DONE | accepted 2026-11-02, see the Evidence line |
| UC-WSP-F06 | Leave a workspace | F | Any active member, Guest included | yes | yes | Final-Account | none | DONE | built and tested on the Account page; the sole owner is led to the handover (D05) and the leave cleans Space and Team entries (F09); see the Evidence line |
| UC-WSP-F07 | Set my own job title | F | Any active member | yes | yes | Final-Account | none | DONE | Every state and rule of the row is covered by tests that ran in this PR; see the State line. |
| UC-WSP-F08 | Set another member's job title | F | Owner, Admin | yes | yes | Wsp-Var-JobTitle, Wsp-Var-JobTitle-Err | none | DONE | Every state and rule of the row is covered by tests that ran in this PR; see the State line. |
| UC-WSP-F09 | Clean a person's Space and Team entries when they leave (enterprise baseline) | F | System | no | yes | Wsp-Var-Teams-NeedLead-T | none | DONE | built and tested for leave and removal, with the 'needs a lead' state; see the Evidence line |
| UC-WSP-F10 | Provision members from an identity directory (SCIM) | F | System principal of Accounts | no | no | none | none | BLOCKED | Decision: system principal for provisioning and its permission. |
| UC-WSP-G01 | Invite a person to the workspace by email | G | Owner, Admin | yes | yes | Final-Members | workspace-invitations-manage.real.e2e.spec.ts | DONE | accepted 2026-11-02, see the Evidence line; the duplicate and retry cases are proven on the real stack only |
| UC-WSP-G02 | Invite with initial Team and Space entries | G | Owner, Admin | yes | yes | Final-Members, Wsp-Var-Invites-Invite | none | HARDENING | Picker and refusals proven; per-entry refusal and the dialog form are open gaps on the State line. |
| UC-WSP-G03 | List workspace invitations | G | Owner, Admin | yes | yes | Final-Members | workspace-invitations-manage.real.e2e.spec.ts | DONE | closed 2026-11-03, see the Evidence line |
| UC-WSP-G04 | Change a pending invitation's role | G | Owner, Admin | yes | yes | Final-Members | workspace-invitations-manage.real.e2e.spec.ts | DONE | accepted 2026-11-02, see the Evidence line |
| UC-WSP-G05 | Resend an invitation | G | Owner, Admin | yes | yes | Final-Members | workspace-invitations-manage.real.e2e.spec.ts | DONE | accepted 2026-11-02, see the Evidence line |
| UC-WSP-G06 | Revoke an invitation | G | Owner, Admin | yes | yes | Final-Members | workspace-invitations-manage.real.e2e.spec.ts | DONE | accepted 2026-11-02, see the Evidence line |
| UC-WSP-G07 | Invite a guest to one Board or Page | G | Holder of `ManageBoardPermission` or `ManagePagePermission` on the resource | yes | no | Final-Share, Final-M-Share | none | IMPLEMENTING | Email invitation in the Share dialog (owner decision); real e2e; `AllowGuestInvites` proof. |
| UC-WSP-G08 | Resend or revoke a resource invitation | G | The issuer with the permission on that resource | yes | no | Final-Share | none | IMPLEMENTING | No frontend caller. |
| UC-WSP-G09 | Invite many people at once (enterprise baseline) | G | Owner, Admin | no | partial | Final-Onboarding | none | DEFINED | Batch contract; result UI. |
| UC-WSP-H01 | Preview an invitation from its link | H | Anyone holding the token | yes | yes | Final-Onboarding, Final-Auth | workspace-invitation-link.real.e2e.spec.ts | DONE | accepted 2026-11-02, see the Evidence line |
| UC-WSP-H02 | Accept an invitation while signed in | H | The invited person | yes | yes | Final-Onboarding | workspace-invitations-answer.real.e2e.spec.ts | DONE | closed 2026-11-03, see the Evidence line |
| UC-WSP-H03 | Accept an invitation and register in one step | H | A new person holding the token | yes | yes | Final-Onboarding | workspace-invitation-link.real.e2e.spec.ts | DONE | accepted 2026-11-02, see the Evidence line |
| UC-WSP-H04 | Decline an invitation from its link | H | Anyone holding the token | yes | yes | Final-Onboarding | none | DONE | Every state and rule of the row is covered by tests that ran in this PR; see the State line. |
| UC-WSP-H05 | Decline an invitation I see in my account | H | The invited person, signed in | yes | yes | none | workspace-invitations-answer.real.e2e.spec.ts | DONE | accepted 2026-11-02, see the Evidence line |
| UC-WSP-H06 | See my pending invitations | H | Any signed-in person | yes | yes | none | workspace-invitations-answer.real.e2e.spec.ts | DONE | closed 2026-11-03, see the Evidence line |
| UC-WSP-I01 | Switch the active workspace | I | Any member of both workspaces | n/a | yes | BC-Shell-1440, BC-Shell-1280-768, BC-Shell-320 | workspace-isolation.real.e2e.spec.ts | DONE | closed 2026-11-15, see the State line |
| UC-WSP-I02 | List the workspaces I belong to | I | Any signed-in person | yes | yes | BC-Shell-1440, Final-Account | workspace-read-and-lists.real.e2e.spec.ts | DONE | closed 2026-11-03, see the Evidence line |
| UC-WSP-I03 | Choose my default workspace | I | Any active member | yes | yes | Final-Account | none | DONE | Every state and rule of the row is covered by tests that ran in this PR; see the State line. |
| UC-WSP-I04 | Authorize a realtime subscription to a workspace | I | System | yes | yes | none | workspace-read-and-lists.real.e2e.spec.ts | DONE | accepted 2026-11-02, see the Evidence line |
| UC-WSP-I05 | See why a workspace is unavailable | I | Any signed-in person | partial | partial | Final-States | none | IMPLEMENTING | Archived and the access states are proven; the read-only (suspended Account) and plan-limited states wait on Billing and Account suspension. |
| UC-WSP-I06 | Always see which workspace I am in | I | Any member | n/a | yes | BC-Shell-1440, BC-Shell-320 | none | DONE | Every state and rule of the row is covered by tests that ran in this PR; see the State line. |
| UC-WSP-J01 | Read workspace settings | J | Any active member | yes | yes | Final-Settings | none | DONE | Every state and rule of the row is covered by tests that ran in this PR; see the State line. |
| UC-WSP-J02 | Change workspace settings | J | Owner, Admin | yes | yes | Final-Settings | none | HARDENING | Proven in part by tests that ran in this PR; the open gaps are on the State line. |
| UC-WSP-J03 | Read sharing and security extras | J | Owner, Admin, Member | yes | yes | Final-Settings, Final-Teams | none | HARDENING | Proven in part by tests that ran in this PR; the open gaps are on the State line. |
| UC-WSP-J04 | Keep Account-wide policy out of workspace settings | J | Owner, Admin | yes | no | none | none | BLOCKED | Decision: owner and enforcement of the MFA policy. |
| UC-WSP-K01 | Create a Space (department) | K | Owner, Admin | yes | yes | Final-Space, Final-M-Space | workspace-spaces.real.e2e.spec.ts | DONE | accepted 2026-11-02, see the Evidence line |
| UC-WSP-K02 | Read the Space tree and list Spaces | K | Any non-Guest member | yes | yes | Final-Space | workspace-spaces.real.e2e.spec.ts | DONE | closed 2026-11-03, see the Evidence line |
| UC-WSP-K03 | Read one Space | K | Any member who can see the Space | yes | yes | Final-Space | workspace-space-read.real.e2e.spec.ts | DONE | closed 2026-11-16, see the Evidence line |
| UC-WSP-K04 | Rename a Space or edit its description | K | Owner, Admin, Space Admin | yes | yes | Final-Space | workspace-space-manage.real.e2e.spec.ts | DONE | closed 2026-11-16, see the Evidence line |
| UC-WSP-K05 | Change a Space's type | K | Owner, Admin, Space Admin | yes | yes | Final-Space | none | DONE | Type select in the settings panel; see the State line. |
| UC-WSP-K06 | Change a Space's visibility | K | Owner, Admin, Space Admin | yes | yes | Final-Space | workspace-space-manage.real.e2e.spec.ts | DONE | closed 2026-11-16, see the Evidence line |
| UC-WSP-K07 | Move or reorder a Space | K | Owner, Admin, Space Admin | yes | yes | Final-Space | workspace-space-move.real.e2e.spec.ts | DONE | closed 2026-11-16, see the Evidence line |
| UC-WSP-K08 | Archive or unarchive a Space | K | Owner, Admin, Space Admin | yes | yes | Final-Space | workspace-space-manage.real.e2e.spec.ts | DONE | closed 2026-11-16, see the Evidence line |
| UC-WSP-K09 | Delete or restore a Space | K | Owner, Admin, Space Admin | yes | yes | Final-Space | workspace-space-trash.real.e2e.spec.ts | DONE | closed 2026-11-16, see the Evidence line |
| UC-WSP-K10 | See who is in a Space | K | Owner, Admin, Space members | yes | yes | Final-Space, Final-M-Space | none | INTEGRATING | Members-extra is not paged: the tab filters and counts client-side, so it needs a server filter and search contract (owner call). Owner question: the Space members read already pages (default 50, max 200) and could take a role and kind filter, but a name search needs names held by Identity, which the Workspaces read cannot filter on. Should search stay on the loaded page, and should the tab counts keep coming from the members-extra summary? |
| UC-WSP-K11 | Add a person or Team to a Space | K | Owner, Admin, Space Admin | yes | yes | Final-Space | workspace-space-manage.real.e2e.spec.ts | DONE | closed 2026-11-16, see the Evidence line |
| UC-WSP-K12 | Change a Space member's role | K | Owner, Admin, Space Admin | yes | yes | Final-Space | workspace-space-manage.real.e2e.spec.ts | DONE | closed 2026-11-16, see the Evidence line |
| UC-WSP-K13 | Remove a person or Team from a Space | K | Owner, Admin, Space Admin | yes | yes | Final-Space | workspace-space-manage.real.e2e.spec.ts | DONE | closed 2026-11-16, see the Evidence line |
| UC-WSP-K14 | Open the department page overview | K | Owner, Admin, Space members | yes | yes | Final-Space, Final-M-Space | none | INTEGRATING | Department page overflows at 390 px (the phong frame, measured in K02); no scale test for large Spaces. |
| UC-WSP-K15 | Place a Board or Page in a Space (facts only) | K | Work Management and Documents | yes | yes | Final-Space | workspace-spaces.real.e2e.spec.ts | DONE | closed 2026-11-03, see the Evidence line |
| UC-WSP-L01 | Create a Team | L | Owner, Admin | yes | yes | Final-Teams | workspace-teams.real.e2e.spec.ts | DONE | closed 2026-11-15, see the Evidence line |
| UC-WSP-L02 | List and read Teams | L | Any non-Guest member | yes | yes | Final-Teams | workspace-teams.real.e2e.spec.ts | DONE | closed 2026-11-15, see the Evidence line |
| UC-WSP-L03 | List Team members | L | Any non-Guest member | yes | yes | Final-Teams | workspace-teams.real.e2e.spec.ts | DONE | closed 2026-11-15, see the Evidence line |
| UC-WSP-L04 | Rename a Team or edit its description | L | Owner, Admin | yes | yes | Final-Teams | workspace-teams.real.e2e.spec.ts | DONE | closed 2026-11-15, see the Evidence line |
| UC-WSP-L05 | Add a person to a Team | L | Owner, Admin | yes | yes | Final-Teams | workspace-teams.real.e2e.spec.ts | DONE | closed 2026-11-15, see the Evidence line |
| UC-WSP-L06 | Change a Team member's role | L | Owner, Admin | yes | yes | Final-Teams | workspace-teams.real.e2e.spec.ts | DONE | closed 2026-11-15, see the Evidence line |
| UC-WSP-L07 | Remove a person from a Team | L | Owner, Admin | yes | yes | Final-Teams | workspace-teams.real.e2e.spec.ts | DONE | closed 2026-11-15, see the Evidence line |
| UC-WSP-L08 | Archive or unarchive a Team | L | Owner, Admin | yes | yes | Final-Teams | workspace-teams.real.e2e.spec.ts | DONE | closed 2026-11-15, see the Evidence line |
| UC-WSP-L09 | Delete or restore a Team | L | Owner, Admin | yes | no | Final-Teams | none | HARDENING | Built: Delete team on the Team page with a confirmation and a refusal state (`delete-team-dialog.tsx`), a deleted Team can be restored from the toast (`teams.$teamId.tsx`); the producer refuses a Team that Spaces still list with `workspaces.team.still-referenced` (`DeleteTeam.cs`). Open: Board grants of a Team are not checked (named gap); no list of deleted Teams. |
| UC-WSP-L10 | Read the Teams administration view | L | Owner, Admin, Member | yes | yes | Final-Teams | none | INTEGRATING | Board grants are not listed in the Teams administration read (waits on the Governance read contract). |
| UC-WSP-M01 | Save, read and clear my onboarding draft | M | The signed-in person | yes | yes | Final-Onboarding | none | DONE | Backend: `OnboardingIntegrationTests` draft tests (saved, replaced, read and dropped per user; another user's draft invisible; 30-day expiry and an invalid save changes nothing), `OnboardingDraftTests` (Domain), read by name, green in the PR's `CI gate`. Frontend: `onboarding-api.unit.test.ts`. Real stack: `workspace-onboarding.real.e2e.spec.ts` journey 1 (2 passed, 17.7 s on the real backend: Save and exit keeps the draft with step and workspace, the next sign-in resumes at the departments step, the finished setup clears it). Design: the draft has no screen of its own: the header's `Lưu và thoát` is measured by `design-fidelity-onboarding.mock.e2e.spec.ts`, and a resumed setup shows the step page itself (gaps of those pages are listed under M04). Real e2e `workspace-onboarding.real.e2e.spec.ts`. |
| UC-WSP-M02 | Sweep expired onboarding drafts | M | System | yes | n/a | none | none | DONE | Backend only (system use case): `OnboardingIntegrationTests` `DraftSweep_DeletesOnlyExpiredDrafts_InBatches_UnderTheWorkerScope` (expired rows gone, live ones stay, bounded batches, worker scope) and the options validator, read by name in the PR's `CI gate`. Frontend and design: not applicable, there is no UI. Real e2e: not applicable, a draft expires after 30 days and cannot be aged through the API; the integration test is the proof. |
| UC-WSP-M03 | Check that a workspace address is free | M | Account member with `CreateWorkspace` | yes | yes | Final-Onboarding | none | DONE | Backend: `OnboardingIntegrationTests` slug-check tests (unique per Account only, next free address, deleted workspace frees its address, every reserved word invalid, length 3 to 63, an outsider refused and told nothing), `WorkspaceSlugAndProfileTests`. Frontend: the page now accepts 3 to 63 characters like the producer (it refused over 30). Design: `design-fidelity-onboarding.mock.e2e.spec.ts` (first page, 2 tests) and `design-fidelity-onboarding-states.mock.e2e.spec.ts` (`tao-workspace` and `dia-chi-trung`, light and dark, 4 passed: every board text run by its words within 1 px and every painted box and icon by place and size, named differences listed in the file). Real stack: `workspace-onboarding.real.e2e.spec.ts` journey 1 (free, taken with `-2`, reserved word invalid with its rule code, a 40-character address free; the page blocks Continue on a taken address, offers the next one and accepts the long one). |
| UC-WSP-M04 | Create the company workspace through the four onboarding steps | M | New Account member | yes | yes | Final-Onboarding | none | DONE | Backend and frontend built; proven in the lane pull request (2026-11-16, branch feat/onboarding-screens-match-boards). Real stack: `workspace-onboarding.real.e2e.spec.ts` journey 1, 2 passed again after the screens changed (the four steps create the workspace, its Private departments, the invitation and the board, the draft is cleared). Design: all seven artboards match within 1 px in `design-fidelity-onboarding-states.mock.e2e.spec.ts` (`tao-workspace`, `dia-chi-trung`, `phong-ban`, `moi-nguoi`, `mau-board`, `xong`, `tham-gia`, light and dark, 14 passed; every board text run by its words and every painted box and icon by place and size, named differences listed in the file header). The departments are 44 px chips with the open colour picker, the lists and rows use the drawn type sizes (13.5 and 12.5), and the last page's summary rows and 0/3 list are laid out as drawn. 1024 and 390 px screenshots of every setup page, light and dark, in Vietnamese were reviewed: no sideways scroll, the invitation rows, template cards and footer stack on narrow screens. |
| UC-WSP-M05 | Join through an invitation and set my look and notifications | M | The invited person | yes | yes | Final-Onboarding | none | DONE | Backend and frontend built; proven in the lane pull request (2026-11-16, branch feat/onboarding-screens-match-boards). Real stack: `workspace-onboarding.real.e2e.spec.ts` journey 2, 2 passed again after the screens changed (an empty name is refused, the name reaches the profile and the mail choice the notification matrix, a second save carries the version of the first). Design: the `tham-gia` artboard matches within 1 px in `design-fidelity-onboarding-states.mock.e2e.spec.ts` (light and dark, 2 of the 14 passed; the mock world's access list is cut to the drawn department and its three boards, the person's name and the lead are set to the drawn words, named in the file header), with the 1024 and 390 px screenshots reviewed. The matrix is one per person for every workspace (owner decision 2026-11-15). |
| UC-WSP-M06 | See the getting-started card | M | Any member, Guest included | yes | yes | Final-Home, Final-Onboarding | none | DONE | Backend: `OnboardingIntegrationTests` getting-started tests (first item from readable Boards, `invite-more` once any invitation was sent, never the mobile item, hidden when dismissed or done, outsider refused), read by name. Frontend: the card on Home. Design: Final-Home draws no card, so it is an undrawn state on design-system tokens: `getting-started-card.mock.e2e.spec.ts` (6 passed: 1440, 1024 and 390, light and dark, in Vietnamese; fits the page, no sideways scroll, keyboard focus, hide button a one-line 44 px target on a phone) with the six screenshots reviewed. Real stack: `workspace-onboarding.real.e2e.spec.ts` journey 1 (the card after the setup: `first-item` not done, `invite-more` done; an outsider gets 404). Two defects fixed: the hide button wrapped to three lines at 390 px, and a sent invitation still read "Nobody has been invited yet". |
| UC-WSP-M07 | Dismiss the getting-started card | M | Any member | yes | yes | Final-Home | none | DONE | Backend: `OnboardingIntegrationTests` dismissal tests (per user and workspace, repeating is a no-op), read by name. Frontend: the hide button on the card. Design: undrawn state, same review as M06 (`getting-started-card.mock.e2e.spec.ts`, 6 passed). Real stack: `workspace-onboarding.real.e2e.spec.ts` journey 1 (hide, hidden after a reload, a repeat answers 204, a second member still sees the card). |
| UC-WSP-N01 | Open workspace Home | N | Any member | partial | yes | Final-Home, Final-M-Home | none | DONE | Every state and rule of the row is covered by tests that ran in this PR; see the State line. |
| UC-WSP-N02 | Show workspace figures to Owner and Admin (enterprise baseline) | N | Owner, Admin | partial | no | none | none | DEFINED | Fix the count, add the route or delete the query; screen. |
| UC-WSP-N03 | Read workspace activity | N | Any member | partial | partial | none | none | IMPLEMENTING | Deferred to 0.2; dependency: the Collaboration activity port, which does not exist yet. |
| UC-WSP-N04 | Remember recently opened resources | N | Any member | yes | yes | Final-Home, Final-Command | none | DONE | Every state and rule of the row is covered by tests that ran in this PR; see the State line. |
| UC-WSP-N05 | Pin and unpin resources | N | Any member | yes | yes | Final-Home, BC-Shell-1440 | none | HARDENING | Proven in part by tests that ran in this PR; the open gaps are on the State line. |
| UC-WSP-N06 | Search the workspace from the command palette | N | Any member | yes | yes | Final-Command, Final-M-Search | none | HARDENING | Loading, failed-search and phone states proven; one gap on the State line (a private Page on PostgreSQL). |

# 3. Use cases

The label of each part is its capability. Ids are `UC-WSP-<letter><nn>`. UI states not listed in a block are the ones of `docs/workstreams/README.md` feature-spec section 12 that apply to a screen of that kind.

## A. Workspace create

### UC-WSP-A01 Create a workspace in an Account
- Capability: A Workspace create
- Actors and permission: Account member with `CreateWorkspace`; verified email required
- Preconditions: Signed in; email confirmed; the Account is resolved from the request.
- Input: `name` (required), `description`, `isPersonal`, optional `slug`, `size`, `language`. Slug rule: 3-63 chars, lowercase letters, digits, single hyphens, not reserved.
- Output: One Workspace (Active, version 1), one Owner membership, the Owner's access grant, `WorkspaceCreated` through the outbox. No Board, Page or Space is created.
- Main flow: 1. Client sends `POST /api/v1/accounts/{accountId}/workspaces`. 2. The pipeline checks `CreateWorkspace` and the verified email. 3. A chosen slug is validated; no slug derives one from the name (random suffix on collision). 4. The Domain factory creates Workspace and Owner. 5. The grant projection is synced in the same transaction.
- No-op / rejection / concurrency / retry: A chosen slug that breaks the rule is rejected with its rule code; one taken in the Account is 409 `workspaces.workspace.slug-taken`; neither is changed silently. No expected version (creation). A retry under the same `Idempotency-Key` returns the first Workspace (A04). Plan limit is not checked (see A03).
- UI states: Form: loading, validation error (address, name), conflict (address taken, next free address offered), permission denied, pending, success; offline blocks submit.
- Rules: [workspaces](../../product/workspaces.md) sections 8, 9, 48.4; WSP-003, WSP-004; WGREQ001 to WGREQ004, WGREQ007; WX-DEC-002.
- Acceptance: Given a verified Account member, When they create a workspace named `Acme`, Then the workspace exists, they are its Owner and `acme` is its address. Given address `admin`, Then 400 with `Workspaces_Workspace_SlugReserved` and nothing is stored. Given an address held in the Account, Then 409 `workspaces.workspace.slug-taken`.
- Evidence: BE `POST /api/v1/accounts/{accountId}/workspaces`, `CreateWorkspaceCommandHandler`, `WorkspaceFactory.CreateWithOwner` · FE `create-workspace-dialog.tsx`, onboarding step 'workspace', `workspace.service.ts` · Design `Final-Onboarding`, `BC-Shell-1440` (rail Add) · Built (no tests written or run yet): the create is idempotent and the validator matches the domain limits (name 160, description 1024); the dialog sends no client-derived slug (the server derives a free address), keeps one key per request and says why a create was refused (offline, no permission, email not confirmed, name too long); onboarding keeps its key while the same request is retried; the mock backend derives the address and replays by key · Tests existing, to be re-run and extended with an `Idempotency-Key`: `CreateWorkspaceCommandHandlerTests`, `WorkspaceCreationPipelineAuthorizationTests`, `WorkspaceCreatedOutboxEvidenceTests`, `WorkspaceSlugAndProfileTests`, `CreateWorkspaceEndpointTests`, `WorkspaceAuthTests`, `WorkspaceLifecycleTests`, `RlsRuntimeEnforcementTests`, `PipelineTelemetryIntegrationTests` · Real e2e none · Line 3: `design-fidelity-onboarding.mock.e2e.spec.ts` (`Final-Onboarding` `tao-workspace`, 1440 px, light and dark, every box within 1 px; the artboard is drawn at this one width). One asserted difference: the artboard draws four company sizes, the accepted contract has five, so the fifth chip wraps and everything under the sizes sits 48 px lower. The shared frame, atoms and preview now follow the artboard type scale and spacing for every setup step.
- State: DONE — lines 1–4 proven in the lane pull request (2026-11-03, branch wsp/finish-0-1); see the Evidence line.

### UC-WSP-A02 Provision the personal workspace at registration
- Capability: A Workspace create
- Actors and permission: System (registration chain); no client permission
- Preconditions: A new User and personal Account were just created.
- Input: User id, Account id, display name; an idempotency key from the event.
- Output: One personal Workspace with the user as Owner, created by an event consumer, not by a client call.
- Main flow: 1. Registration commits and publishes its fact. 2. `WorkspaceProvisioningConsumer` runs `ProvisionPersonalWorkspaceCommand`. 3. The command creates Workspace and Owner under the idempotency pipeline. 4. The user lands in it after sign-in.
- No-op / rejection / concurrency / retry: A redelivery of the same fact creates nothing new (`IIdempotentRequest`). A failure retries and, when poisoned, is dead-lettered; the user can still create a workspace by hand (A01).
- UI states: No screen. The shell shows a loading state until the first workspace exists.
- Rules: [workspaces](../../product/workspaces.md) sections 8, 74; WGREQ008, WGREQ009, WGREQ158; NRX-009, NRX-010.
- Acceptance: Given a registration fact, When it is delivered twice, Then exactly one personal workspace exists. Given the consumer fails, Then the fact is retried and the registration itself is not rolled back.
- Evidence: BE `ProvisionPersonalWorkspaceCommand`, `WorkspaceProvisioningConsumer` · FE none · Design none · Tests `WorkspaceProvisioningConsumerTests`, `RegistrationWorkspaceRuntimeChainIntegrationTests` · Real e2e none · 2026-11-03: line 1 by `WorkspaceProvisioningConsumerTests`, `RegistrationWorkspaceRuntimeChainIntegrationTests` (production chain; a failing consumer leaves the registration committed) and `DeduplicationConsumeFilterFullIntegrationTests` (a duplicate delivery runs the consumer once; a failed command-owned consumer releases its claim so the retry succeeds); line 4 by `register-sign-in.real.e2e.spec.ts` (after confirming and signing in the person has exactly one workspace, sign-in lands inside it, and registering and signing in again makes no second one).
- State: DONE — lines 1–4 proven in the lane pull request (2026-11-03, branch wsp/finish-0-1); see the Evidence line.

### UC-WSP-A03 Stop creation at the plan limit (enterprise baseline)
- Capability: A Workspace create
- Actors and permission: Account member with `CreateWorkspace`
- Preconditions: Billing publishes a workspace-count entitlement for the Account.
- Input: Same as A01.
- Output: A refusal that names the limit; no Workspace is created.
- Main flow: 1. Before the factory runs, Application asks the Billing public gate for a workspace slot. 2. Allowed: continue as A01. 3. Refused: return a plan-limited error with the limit and the plan.
- No-op / rejection / concurrency / retry: Plan-limited is not a permission error and not a membership error (section 70). If Billing cannot answer, creation fails closed with 503 and a retry hint.
- UI states: Plan-limited state with the limit and a link to Billing; the rest as A01.
- Rules: [workspaces](../../product/workspaces.md) sections 31, 70, WSP-011; billing is the entitlement owner (see [billing](billing.md)).
- Acceptance: Given an Account at its limit, When a member creates a workspace, Then the answer names the limit and nothing is stored. Given Billing is down, Then creation fails with 503.
- Evidence: BE none (`CreateWorkspaceCommandHandler` has no Billing gate; only `InvitationAcceptanceService` uses `IBillingSeatGate`) · FE none · Design `Final-Billing` (limits) · Tests none · Real e2e none
- State: DEFINED — Billing workspace-count entitlement; gate call; error contract; screen state. Owner decision needed on the entitlement; not built in this unit.

### UC-WSP-A04 Make workspace creation retry-safe
- Capability: A Workspace create
- Actors and permission: Account member with `CreateWorkspace`
- Preconditions: The client sends an idempotency key (header) with the create call.
- Input: Same as A01 plus `Idempotency-Key`.
- Output: The same key returns the first result (same workspace id) and creates nothing more.
- Main flow: 1. Client generates one key per user submit. 2. The create command is an idempotent request. 3. A repeat with the same key and same body returns the stored result. 4. The same key with a different body is rejected.
- No-op / rejection / concurrency / retry: A double click or a network retry must not create `acme` and `acme-3f9a1c`. Different body under one key: refused (the pipeline answers 409 `IdempotencyPayloadMismatch`, not 422).
- UI states: Pending state disables the submit button; a retried request shows the original result.
- Rules: [workspaces](../../product/workspaces.md) section 73, WSP-019; WGREQ009; NRX-010.
- Acceptance: Given two identical create calls with one key, Then one workspace exists and both calls return its id.
- Evidence: BE `CreateWorkspaceCommand` is an `IIdempotentRequest` (`workspaces.workspace.create-workspace.v1`), the endpoint requires the key (`.WithIdempotencyKey()`), the partition is the Account · FE `use-create-workspace.ts` holds one key per input and reuses it on retry; `onboarding-api.ts` does the same · Design none · Tests none written yet · Real e2e none · Lane run 2026-11-03: line 4: real stack (project notrelix-fe-real-e2e, 2026-11-03, branch fix/workspaces-acceptance-gaps), `workspace-create.real.e2e.spec.ts` 'a workspace is created from the rail and by the API': two calls with one key give one workspace and the same id; the same key with another body is refused and creates nothing. Line 3 n/a (no screen).
- State: DONE — lines 1–4 proven in the lane pull request (2026-11-03, branch fix/workspaces-acceptance-gaps); see the Evidence line.

## B. Workspace read / resolve

### UC-WSP-B01 Read one workspace
- Capability: B Workspace read / resolve
- Actors and permission: Any active member, Guest included (`ViewWorkspace`)
- Preconditions: Signed in; active membership in the workspace.
- Input: `workspaceId`.
- Output: The workspace DTO: id, name, address, description, status, size, language, version, my role. Archived workspaces still read for members.
- Main flow: 1. `GET /api/v1/workspaces/{workspaceId}`. 2. Pipeline checks membership. 3. Handler returns the DTO.
- No-op / rejection / concurrency / retry: A caller without access gets the same NotFound as for a missing id. A removed or suspended member gets the same NotFound. Deleted workspace: NotFound.
- UI states: Loading skeleton, success, permission denied/not found page, archived banner (I05).
- Rules: [workspaces](../../product/workspaces.md) sections 3, 12, 65; WSP-005; WGREQ001, WGREQ002, WGREQ082.
- Acceptance: Given a member, When they read the workspace, Then they get it. Given a signed-in stranger, Then 404 identical to a random id.
- Evidence: BE `GET /api/v1/workspaces/{workspaceId}`, `GetWorkspaceQueryHandler` · FE `use-workspace.ts`, `workspace.service.ts` · Design `BC-Shell-1440` · Tests `WorkspaceQueryTests`, `WorkspaceAuthTests`, `WorkspaceScopeTests` · Real e2e workspace-read-and-lists.real.e2e.spec.ts · Acceptance run 2026-11-02 by an independent acceptance agent that did not build it, at develop `8d139a73e` (branch accept/batch-5); the backend suites ran at `2c9eb1874`, the parent of 8d139a73e, whose only change (#262) touches no backend file. Line 1: `WorkspaceQueryTests` 6 and `WorkspaceAuthTests` 17 (API) and Domain `WorkspaceScopeTests` within the filtered runs (Domain `Workspaces` 493, Application `Workspaces` 286, API `Workspaces` 188 and the Workspaces integration suites (membership and invitation concurrency, accept transaction, escalation, role baseline, invitation token flow, self-targeted change) 66 passed, 0 failed.); the real stack proves the cross-tenant case: `workspace-read-and-lists.real.e2e.spec.ts` (a signed-in stranger and a removed member get 404 identical to a made-up id, neither lists the workspace, the realtime gateway refuses the stranger). Line 2: `workspace-shell.interaction.component.test.tsx` (loading, not-found and error states of the scope boundary) passed with the workspace package (436 web component tests passed); the not-found page's Try again and exit buttons are 44 px at 390 px. Line 3: the catalog's Design cell names BC-Shell-1440, which draws the Board shell's states (frames sh1440-L, E, Er, Ev, NG, NP, R, RO, VM), not the workspace page, and no spec measures it; the workspace shell geometry is measured in `design-fidelity-shell` (18 of 18 passed) and the workspace not-found page has no artboard, so it was read: 6 sheets (1440, 1024, 390 px, light and dark), no overflow, 44 px buttons at 390 px, axe clean. Line 4: `workspace-read-and-lists.real.e2e.spec.ts` 3 of 3 inside Real stack (fresh `notrelix-fe-real-e2e` stack): `workspace-member-changes` 2, `workspace-roster-states` 2, `workspace-invitations-manage` 2, `workspace-invitation-link` 1, `workspace-invitations-answer` 1, `workspace-read-and-lists` 3, `workspace-cross-scope` 1 (added in this run), plus `work-item-states` 8, `board-sharing` 1, `approvals` 3, `item-lifecycle` 3, `sub-items` 2 and `board-settings` 1: 30 of 30 passed (the cross-scope spec passed on its rerun after a spec fix, see below).
- State: DONE — accepted by independent acceptance 2026-11-02 at `8d139a73e` (branch accept/batch-5); see the Evidence line.

### UC-WSP-B02 Open a workspace by its address
- Capability: B Workspace read / resolve
- Actors and permission: Anyone (public read); the route then requires sign-in and membership
- Preconditions: The URL carries an Account id and a workspace address.
- Input: `accountId`, `slug` (query).
- Output: The workspace id for a live address, or not found. It reveals nothing else.
- Main flow: 1. `GET /api/v1/accounts/{accountId}/resolve?slug=`. 2. The resolver maps the address to the id. 3. The client navigates to `/workspaces/{id}` where membership is checked.
- No-op / rejection / concurrency / retry: An unknown address and a deleted workspace both return not found. The endpoint is anonymous, so it must be rate-limited and return only the id.
- UI states: Loading, not found page.
- Rules: [workspaces](../../product/workspaces.md) sections 3, 52; WGREQ002, WGREQ032 (enumeration resistance).
- Acceptance: Given a live address, When resolved, Then the id returns. Given a deleted or unknown address, Then the same not-found answer.
- Evidence: BE `GET /api/v1/accounts/{accountId}/resolve`, `ResolveSlugQueryHandler`, `IWorkspaceSlugResolver` · FE none (routes use ids) · Design none · Tests `WorkspaceRouteTests` (Accounts), `AccountWorkspaceEndpointTests`, `ResolveSlugRateLimitTests` (API, 60 then 429), `ResolveSlugEnumerationTests` (integration, live vs deleted vs made-up) · Real e2e none
- State: IMPLEMENTING — Built: the resolve route is limited under `GeneralAnonymous` (`ResolveSlugEndpoint.cs`), tested by `ResolveSlugRateLimitTests` (passes 2026-11-10) and by the enumeration test `ResolveSlugEnumerationTests` (written and compiled, not run: Docker). A deleted and a made-up address answer the same refusal, compared by status and body without the correlation fields. The refusal is a 400 in the shipped code, not a 404. Deferred to 0.2 (lead ruling 2026-11-19): 0.1.0 keeps ids in every route, so no frontend caller is built; the vanity address route is a 0.2 item and the row stays IMPLEMENTING until then.

### UC-WSP-B03 List the workspaces of an Account
- Capability: B Workspace read / resolve
- Actors and permission: Account member with `ViewWorkspace` on the Account scope
- Preconditions: Signed in; Account resolved.
- Input: `accountId`.
- Output: Rows of the Account's workspaces the caller may see.
- Main flow: 1. `GET /api/v1/accounts/{accountId}/workspaces`. 2. Pipeline checks Account membership. 3. Handler returns rows.
- No-op / rejection / concurrency / retry: Being an Account member does not show workspaces the person does not belong to (WSP-004); the handler must filter by membership. Not checked by a test.
- UI states: Loading, empty, success, permission denied.
- Rules: [workspaces](../../product/workspaces.md) sections 9, 10; WSP-004, WSP-005.
- Acceptance: Given an Account member who belongs to two of three workspaces, When they list, Then two rows return.
- Evidence: BE `GET /api/v1/accounts/{accountId}/workspaces`, `GetAccountWorkspacesQueryHandler` · FE none (the account page uses I02) · Design none · Tests `AccountWorkspaceEndpointTests`, `AccountWorkspacesHttpFlowTests` · Real e2e none
- State: IMPLEMENTING — No caller; overlaps with I02 and B04; membership-filter proof.

### UC-WSP-B04 List every workspace I belong to (user scope)
- Capability: B Workspace read / resolve
- Actors and permission: Any signed-in user
- Preconditions: Signed in.
- Input: none.
- Output: The caller's active memberships across Accounts with workspace summaries.
- Main flow: 1. `GET /api/v1/workspaces`. 2. Handler reads the caller's memberships. 3. Returns summaries.
- No-op / rejection / concurrency / retry: Never returns another person's memberships. Removed, suspended, archived and deleted rows are excluded.
- UI states: Loading, empty, success.
- Rules: [workspaces](../../product/workspaces.md) sections 29, 19.1; WSP-010.
- Acceptance: Given a user in three workspaces, one left, When they list, Then two return.
- Evidence: BE `GET /api/v1/workspaces`, `GetUserWorkspacesQueryHandler` · FE `use-workspace-list.ts` (the workspace switcher and the scope boundary read this list) · Design none · Tests `GetUserWorkspacesQueryHandlerTests` · Real e2e workspace-read-and-lists.real.e2e.spec.ts · Lane run 2026-11-03: line 3 closed: the system-rail switcher list at 1440, 1024 and 390 px, light and dark, passes the audit (`workspace-acceptance` 'workspace switcher in the system rail', 6 cases). Decision LD-06: B04 (`GET /workspaces`, the switcher) and I02 (`GET /account/workspaces`, which also returns archived workspaces to Owners and Admins) stay as two use cases; their scopes differ. Line 4: real stack (project notrelix-fe-real-e2e, 2026-11-03, branch fix/workspaces-acceptance-gaps), `workspace-read-and-lists.real.e2e.spec.ts` 3 of 3 passed.
- State: DONE — lines 1–4 proven in the lane pull request (2026-11-03, branch fix/workspaces-acceptance-gaps); see the Evidence line.

## C. Workspace update

### UC-WSP-C01 Edit workspace name and description
- Capability: C Workspace update
- Actors and permission: Owner, Admin (`ManageWorkspace`)
- Preconditions: Workspace Active and not deleted; caller holds the current version.
- Input: `name` (trimmed, non-empty, at most 160 characters; omitted leaves it alone), `description` (at most 1,024; empty clears it; omitted leaves it alone), `expectedVersion`.
- Output: Updated profile, version +1 (once, even when name and description both change), `WorkspaceRenamed` when the name changed, `WorkspaceDescriptionUpdated` when the description changed. An unchanged value changes nothing.
- Main flow: 1. `PATCH /api/v1/workspaces/{id}/profile` with the version. 2. Pipeline checks the version. 3. Domain renames or updates the description. 4. Outbox emits the fact.
- No-op / rejection / concurrency / retry: Same value: no-op, no version bump, no event. Stale version: 412 `common.precondition-failed` and nothing changes; the web client reads 412 as a conflict and offers Reload. Archived workspace: rejected `Workspaces_Workspace_CannotRenameArchived` or `..._CannotUpdateDescriptionArchived`. Both values are checked before either is applied. A retry after success carries the old version, is refused with 412 and changes nothing.
- UI states: Form: pristine/dirty, validation error, pending, conflict (reload), permission denied (read-only fields), success toast.
- Rules: [workspaces](../../product/workspaces.md) section 3; WGREQ007; WX-SET-001; NRX-011.
- Acceptance: Given version 4, When Admin renames, Then version is 5 and the new name shows. Given the same name, Then version stays 4. Given a stale version, Then 409 and no change.
- Evidence: BE `PATCH /api/v1/workspaces/{id}/profile`, `UpdateWorkspaceProfileCommandHandler`, `Workspace.UpdateProfile`, `UpdateWorkspaceProfileCommandValidator` (name 160) · FE settings route `settings.tsx`, `workspace-settings-surface.tsx` (`ProfileForm`: pristine and dirty, validation, saving, saved, conflict with Reload, read-only for people who cannot manage, inline refusal and offline text), `use-workspace-settings-mutations.ts` · Design none for workspace settings; `Final-Settings` is the Board settings page · Tests `WorkspaceSlugAndProfileTests`, `WorkspaceVersionTests`, `WorkspaceMutationTests`, `WorkspaceAdministrationReadIntegrationTests` (stale version), `workspace-pages.mock.e2e.spec.ts` (WX-SET-001); none yet for `UpdateProfile` (one version step) · Real e2e none · Lane run 2026-11-03: Domain `WorkspaceVersionTests` 11 passed (name and description together step the version once, the same values are a no-op that raises nothing, 161 characters refused and nothing changes). At 390 px the settings page had 32 to 36 px inputs, selects, buttons and switches; now 44 px, audited at 1440, 1024 and 390 px light and dark (`workspace-acceptance` 'workspace settings and the delete confirmation'; no artboard draws workspace settings, so it is judged undrawn). Line 4: real stack (project notrelix-fe-real-e2e, 2026-11-03, branch fix/workspaces-acceptance-gaps), `workspace-create.real.e2e.spec.ts` 'an Admin renames the workspace in settings' passed (the Save button renames, version +1, the same name keeps the version, a stale version is refused and the name stays).
- State: DONE — lines 1–4 proven in the lane pull request (2026-11-03, branch fix/workspaces-acceptance-gaps); see the Evidence line.

### UC-WSP-C02 Change the workspace address (enterprise baseline)
- Capability: C Workspace update
- Actors and permission: Owner, Admin (`ManageWorkspace`)
- Preconditions: Workspace Active; new address passes the address rule.
- Input: `slug`, `expectedVersion`.
- Output: New address, version +1, `WorkspaceSlugChanged`. Links by id keep working; the old address is released.
- Main flow: 1. Caller sends the new slug. 2. `WorkspaceSlugRules` validates it and the Account uniqueness index. 3. The Domain changes it and emits the fact. 4. Clients follow the id.
- No-op / rejection / concurrency / retry: Taken: 409 `workspaces.workspace.slug-taken`. Same slug: no-op. Stale version: 409. Old address is not redirected in v1.
- UI states: Settings field with live check (M03), conflict, pending, success.
- Rules: [workspaces](../../product/workspaces.md) section 48.4 (no rename today; a rename must call the same rule); Part 4 LD-10.
- Acceptance: Given a free address, When changed, Then resolve (B02) returns the id for it and not for the old one.
- Evidence: BE none (`Rename` changes the name only) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Command, endpoint, event, settings field.

## D. Workspace lifecycle transition

### UC-WSP-D01 Archive a workspace
- Capability: D Workspace lifecycle transition
- Actors and permission: Owner (`ManageWorkspace`; lifecycle stays Owner-only, GOV-013)
- Preconditions: Workspace Active; caller holds the current version.
- Input: `expectedVersion`.
- Output: Status Archived, version +1, `WorkspaceArchived`. Data is kept; new mutation is blocked; history stays readable.
- Main flow: 1. `POST /api/v1/workspaces/{id}/archive`. 2. Domain sets Archived. 3. The fact reaches participant contexts, which pause automation and integration sync.
- No-op / rejection / concurrency / retry: Already archived: no-op. Deleted: rejected. Stale version: 412. Every write inside an archived workspace is refused by the access policy with the archived reason (`workspace.archived`, Forbidden), whichever context owns it; the five lifecycle commands (Archive, Unarchive, Delete, Restore, Leave) and per-user state stay allowed: Chat read state and follow (`MarkAllRead`, `MarkSpaceRead`, `SetNotificationLevel`, `FollowThread`, `MarkThreadRead`), inbox entry operations (`MarkInboxEntryDone`, `MarkInboxEntryRead`, `MarkInboxEntryUnread`, `MarkWorkspaceInboxRead`, `MuteInboxThread`, `ReopenInboxEntry`, `SnoozeInboxEntry`) and navigation (`PinResource`, `UnpinResource`, `RecordRecentResource`). Members still read an archived workspace.
- UI states: Destructive confirmation (type the name), pending, success, conflict, permission denied.
- Rules: [workspaces](../../product/workspaces.md) section 60; WGREQ005, WGREQ006; Part 4 LD-02.
- Acceptance: Given an Active workspace, When Owner archives it with the right version, Then status is Archived and members can still read it. Given a Member or an Admin, Then 403.
- Evidence: BE `POST /api/v1/workspaces/{id}/archive`, `ArchiveWorkspaceCommandHandler` · FE settings 'Danger zone' (`workspace-settings-surface.tsx`), `use-workspace-settings-mutations.ts` · Design `Final-Settings` · Tests `WorkspaceTests`, `WorkspaceMutationOrderingTests`, `WorkspaceLifecycleTests` · Real e2e none
- State: DONE — Archive, the archived write gate and the read rule are proven by tests that ran in this PR; pause of automations and integration sync deferred to the Automation/Integrations modules (neither has a live sync yet), so the fact still reaches log-only stub consumers. Domain: `WorkspaceTests` (`Archive_ShouldSetStatusToArchived_AndRaiseEvent`, `Archive_WhenAlreadyArchived_ShouldBeNoOp`), `WorkspaceMutationOrderingTests` (`Archive_NoOp_ShouldNotIncrementVersion`, archived refuses rename, description and settings), `ConcurrencyTests.Archive_AfterDelete_ShouldThrow` (79 Domain tests pass). API `WorkspaceMutationTests` (`ArchiveWorkspace_*`, zero version 400, missing workspace NotFound). Integration: `WorkspaceAdministrationReadIntegrationTests` (stale archive answers 412 and changes nothing), `WorkspaceRoleBaselineIntegrationTests` (Admin, Member and Guest are denied), and the new `ArchivedWorkspaceWriteGateIntegrationTests` (17 pass through the real policy engine and the real access-facts SQL: a job-title write is refused with `workspace.archived` for Owner, Admin, Member and Guest and changes nothing; a board and a page create are refused for Owner, Admin and Member, and a Guest who cannot create a board anyway keeps the plain refusal; every member reads an archived workspace; a stranger gets the identical NotFound for a read and a write; the Owner unarchives and the same write succeeds; a second archive is a no-op with the version unchanged; an Admin cannot unarchive; a member can still leave); all three CI Integration shards pass (Workspaces and Auth 545, work-rls 621, rest 787). Application `ArchivedWorkspaceWriteGateTests` and `WorkspaceAdministrationReadHandlerTests` (Governance and Workspaces namespaces pass), Architecture tests 680 pass. Failing-first: the gate tests failed with `Failed!  - Failed:     8, Passed:    15` and `Expected decision.Kind to be AccessDecisionKind.Forbidden {value: 2}, but found AccessDecisionKind.Allowed {value: 0}.`; reading an archived workspace failed with `NotFoundException : Entity "Workspace" ... GetWorkspace.cs:line 30` before the Active-only filter was dropped; the integration class failed `Failed: 10, Passed: 7` with `Expected a <ForbiddenException> to be thrown, but no exception was thrown.` and a member could not leave (the owner-update lock covered only active workspaces; fixed with `LockWorkspaceIncludingArchivedAsync`). Frontend `workspace-archive.component.test.tsx` (6 pass: confirm only after the exact name is typed and nothing is sent before, the write quotes the workspace version and leaves for Home, the action shows as running with no second write, a stale version says the workspace changed (failing-first `+ "err:Acme wasn't archived.|Something went wrong on the server. Nothing was changed; try again."`), a 403 says there is no permission, a plain member sees no Danger zone) and `workspace-settings.interaction.component.test.tsx` (13 pass: type-the-name confirm, read-only without permission). Follow-up, not a gap of this row: Chat writes that are not per-user read state (presence, typing, drafts) were not audited and stay refused in an archived workspace; revisit them if archived workspaces see real use.

### UC-WSP-D02 Unarchive a workspace
- Capability: D Workspace lifecycle transition
- Actors and permission: Owner (`ManageWorkspace`; lifecycle stays Owner-only, GOV-013). An Admin, a Member and a Guest get 403
- Preconditions: Workspace Archived; caller can reach it (LD-02).
- Input: `expectedVersion`.
- Output: Status Active, version +1, `WorkspaceUnarchived`; paused automation and sync resume.
- Main flow: 1. `POST /api/v1/workspaces/{id}/archive/unarchive`. 2. Domain sets Active. 3. Fact published.
- No-op / rejection / concurrency / retry: Already Active: no-op. Deleted: rejected. Stale version: 412 `common.precondition-failed`.
- UI states: Archived banner for every member, with an Unarchive action for the Owner only; pending; success (the banner leaves, members write again); conflict (stale version, with Reload); permission denied. Settings, Danger zone: the Owner sees Unarchive instead of Archive, an Admin reads that only the owner can; the profile and defaults are read-only while archived.
- Rules: [workspaces](../../product/workspaces.md) sections 60, 63; WGREQ005; Part 4 LD-02.
- Acceptance: Given an Archived workspace, When an Owner unarchives, Then members write again. Given an Admin, a Member or a Guest, Then the action is absent and the API returns 403.
- Evidence: BE `POST /api/v1/workspaces/{id}/archive/unarchive`, `UnarchiveWorkspaceCommandHandler` · FE `archived-workspace-banner.tsx` and `archived-workspace-banner-host.tsx` (shell banner), settings Danger zone (`workspace-settings-surface.tsx`), account workspace lists (Unarchive for the Owner, Open read-only for all) · Design `Wsp-Var-Archived-Banner`, `Wsp-Var-Danger-Archived` (Workspaces canvas) · Tests `UnarchiveWorkspaceCommandHandlerTests`, `ArchivedWorkspaceWriteGateIntegrationTests`, `archived-workspace-banner.component.test.tsx`, `archived-workspace-banner-host.component.test.tsx`, `workspace-settings.archived.component.test.tsx`, `account-workspaces.interaction.component.test.tsx` · Real e2e none
- State: DONE — Proven by tests that ran in this PR. Application `UnarchiveWorkspaceCommandHandlerTests` and `GetMyWorkspacesHandlerTests` (11 pass). Integration `ArchivedWorkspaceWriteGateIntegrationTests` (21 pass, 4 new: a Member and a Guest cannot unarchive, the workspace stays archived and the version is unchanged; a stale version is refused with `PreconditionFailedException` and nothing changes; unarchiving an Active workspace is a no-op with the version unchanged), with the earlier ones: an Admin cannot unarchive, the Owner unarchives and the same write succeeds again. Frontend: `archived-workspace-banner.component.test.tsx` (6 pass: the Owner sees the banner and Unarchive; every other member sees the banner without the action; pending is disabled and sends once; conflict offers Reload, not the stale write; denied and failed name what happened), `archived-workspace-banner-host.component.test.tsx` (5 pass: the write quotes the workspace version `/workspaces/w1/archive/unarchive` and the workspace record is read again so the banner leaves; 412 shows the conflict state and Reload reads again; 403 says only the owner can; Admin, Member and Guest see no action and send nothing), `workspace-settings.archived.component.test.tsx` (5 pass: Unarchive replaces Archive for the Owner with a confirmation, an Admin reads why not, pending, the forms are read-only with the reason, an active workspace is unchanged), `workspace-settings-service.unit.test.ts` (the write posts the version), `account-workspaces.interaction.component.test.tsx` (an Admin sees Open and no Unarchive). The settings copy "An owner can restore it later" is now true. Defect fixed with a failing-first test: an archived workspace is left out of the member list, and the scope check read a workspace missing from the list as lost access, so every member who opened it saw the "no access" state (red in `workspace-scope.archived.unit.test.ts`: `expected 'forbidden' to be null`). Existing tests changed: two cases of `account-workspaces.interaction.component.test.tsx` now use an Owner fixture, because the list offers Unarchive to the Owner only (lifecycle is Owner-only). Decision: the member lists keep their rule (an archived workspace is listed to its Owners and Admins, who open it read-only; a Member reaches it by its link and sees the banner); listing it to every Member is left as a product ruling. The pause and resume of automations and integration sync is deferred with D01 to the Automation and Integrations modules (no live sync yet).

### UC-WSP-D03 Delete a workspace
- Capability: D Workspace lifecycle transition
- Actors and permission: Owner only (`DeleteWorkspace`, Owner-only in the engine: no rule, grant, custom role or level can lend it). An Admin, a Member and a Guest of an active workspace get 403.
- Preconditions: Workspace exists, is not personal and is not deleted; caller holds the current version.
- Input: `expectedVersion` in the query.
- Output: 204. The workspace carries `deleted_at`, `deleted_by`, version +1, `WorkspaceDeleted`, and a Critical audit entry `DeleteWorkspace`. Its content is kept for 30 days.
- Main flow: 1. `DELETE /api/v1/workspaces/{id}?expectedVersion=`. 2. The engine checks the Owner role. 3. `Workspace.Delete` marks it. 4. From the commit on, every request on the workspace, in its Workspace scope or on a resource inside it (reads, writes, search, realtime subscription), is the identical NotFound for its Owner, Admins, Members, Guests and strangers; the database policy shows the row to its Owner only. 5. After 30 days the purge job removes its data (D04 describes the window).
- No-op / rejection / concurrency / retry: A repeat delete is the domain no-op (no version bump, no event, no second audit entry), though once deleted the pipeline answers NotFound first. A personal workspace is refused `Workspaces_Workspace_CannotDeletePersonal` (400). A stale version is 412 `common.precondition-failed` and nothing changes. Not a member or not an Owner of a deleted workspace: NotFound.
- UI states: Settings, Danger zone, second card "Delete this workspace" (shown to the Owner only; disabled with a reason on a personal workspace); destructive confirmation dialog with the consequence text and a typed workspace name (confirm stays off until the name matches exactly); pending ("Deleting..."); success toast and the person leaves the workspace (the switcher and rail drop it at once, its cached answers are removed); conflict and failure toasts. vi and en.
- Rules: [workspaces](../../product/workspaces.md) sections 61, 62, 64; WSP-018; WGREQ006; NRX-003, NRX-004, NRX-011; owner decision 2026-11-01 (delete is a process: members cut at once, content kept, restorable 30 days, then purged).
- Acceptance: Given an Owner and a Member of a workspace, When the Owner deletes it with its name typed, Then the Member's reads of it, its boards and its list entry answer NotFound at once and the Owner finds it in Recently deleted with 30 days left. Given an Admin, Then delete is 403. Given a personal workspace, Then delete is refused with its rule code. Given a stale version, Then 412.
- Evidence: BE `DELETE /api/v1/workspaces/{id}`, `DeleteWorkspaceCommandHandler`, `Workspace.Delete` and `RestoreWindow`, `AccessPolicyEngine` (Owner-only rule and the deleted-workspace NotFound), `028_deleted_workspaces.sql` (`ops.is_workspace_owner`, the narrowed `workspace.workspaces` policies), `DeletedWorkspacePurger` (the 30-day purge) · FE `workspace-settings-surface.tsx` (`DangerFlow`), `settings.tsx`, `use-workspace-settings-mutations.ts` (`remove`) · Design none: no workspace danger-zone artboard exists; built from the archive card of the same page · Tests `WorkspaceDeletionAtomicityTests` (personal, window, version, no-op), `DeleteWorkspaceCommandHandlerTests`, `DeletedWorkspaceAccessTests` (Application, every role and scope), `DeletedWorkspaceIntegrationTests` (real PostgreSQL under the application and worker logins: row hidden from members, strangers and another tenant, a non-Owner cannot delete even with raw SQL, stale version, personal refusal), `DeletedWorkspacePipelineIntegrationTests` (real pipeline: identical NotFound for Owner, Admin and Member on a workspace, its boards and a delete repeat), `workspace-settings.interaction.component.test.tsx` · Real e2e `workspace-delete-restore.real.e2e.spec.ts` · 2026-11-03: the delete now signals the realtime gateway (`IAccessChangeSignal.Workspace`), which re-authorizes the workspace's open subscriptions at once and revokes each; `DeleteWorkspaceCommandHandlerTests` (signal once, none on a no-op or refusal), `RealtimeConnectionTests` (a workspace access signal revokes at once, not at the periodic pass), `DeletedWorkspacePipelineIntegrationTests` (the workspace and board realtime authorization is the identical NotFound for Owner, Admin and Member, and answers again after restore), real e2e (a member's open socket ends as revoked within seconds, a new one is denied). Other contexts: the product doc (sections 61 to 64) specifies no reaction beyond the access cut and the purge, so none is built; no export or provider-side cleanup is specified. No workspace danger-zone artboard exists (line 3 by the screenshot review above).
- State: DONE — lines 1–4 proven in the lane pull request (2026-11-03, branch wsp/finish-0-1); see the Evidence line.

### UC-WSP-D04 Restore a deleted workspace
- Capability: D Workspace lifecycle transition
- Actors and permission: The workspace Owner (`RestoreWorkspace`, Owner-only like delete). The Owner finds it in the account-level Recently deleted list (`GET /api/v1/accounts/{accountId}/workspaces/deleted`, rows of the Account the person owns, deleted inside the last 30 days, newest first); anyone else gets an empty list.
- Preconditions: Workspace deleted less than 30 days ago (`Workspace.RestoreWindow`); caller holds the version the list carried.
- Input: `expectedVersion` in the body.
- Output: 204. The delete marker is cleared, version +1, `WorkspaceRestored` and a Warning audit entry `RestoreWorkspace`. Status (Active or Archived), memberships, grants, Spaces, Boards and pages are exactly as they were: nothing was touched at delete.
- Main flow: 1. Account page "Your workspaces", Recently deleted. 2. `POST /api/v1/workspaces/{id}/restore`. 3. The request marker `IWorkspaceReachesDeletedRequest` lets the Owner past the deleted-workspace NotFound; everyone else still gets it. 4. `Workspace.Restore` checks the window and clears the marker.
- No-op / rejection / concurrency / retry: Restoring an active workspace is the domain no-op (no version bump, no event, no audit). After the window the rule `Workspaces_Workspace_RestoreWindowExpired` refuses it (the entry is gone from the list at the same instant). A stale version is 412. A non-Owner, or a stranger, gets NotFound whether the workspace is deleted or not. Purge and restore race on the row lock: the purge claims the row `FOR UPDATE SKIP LOCKED`, so a restore waits for it and then finds NotFound.
- UI states: Recently deleted section on the account workspaces page (only when there are rows; a read failure shows a retry), each row with name, deleted date and the days left ("1 day left" at the last), Restore button; success notice; refusal text; vi and en.
- Rules: [workspaces](../../product/workspaces.md) section 63; WGREQ006; NRX-003, NRX-011; owner decision 2026-11-01.
- Acceptance: Given a deleted workspace inside the window, When its Owner restores it, Then its Members read it, its boards and its list entry again and it leaves Recently deleted. Given 31 days, Then restore is refused with its rule code. Given an Admin or a Member, Then restore is NotFound.
- Evidence: BE `POST /api/v1/workspaces/{id}/restore`, `RestoreWorkspaceCommandHandler`, `GET /api/v1/accounts/{accountId}/workspaces/deleted`, `GetDeletedWorkspacesQueryHandler`, `Workspace.Restore` · FE `account-workspaces-surface.tsx` (`DeletedCard`), `use-account-workspaces.ts` (`deleted`, `restore`), `account.service.ts` (`listDeletedWorkspaces`, `restoreWorkspace`, the Account named by the bootstrap) · Design none: no artboard for it; reuses the archived-workspaces group · Tests `WorkspaceDeletionAtomicityTests`, `DeletedWorkspaceIntegrationTests` (round trip with RLS, expired window, the Recently deleted list for an Owner, a member and another tenant), `DeletedWorkspacePipelineIntegrationTests` (restore by the Owner only, then every request answers again), `account-workspaces.interaction.component.test.tsx`, `account-deleted-workspaces.unit.test.ts` · Real e2e `workspace-delete-restore.real.e2e.spec.ts` · 2026-11-03: `/account/workspaces` outside a workspace (`AccountOutsideWorkspacePage`: the account frame with its one page that needs no workspace); `/home` lands there instead of onboarding when the person has a workspace in Recently deleted (`home-landing.component.test.tsx`, `account-pages.component.test.tsx` 'AccountOutsideWorkspacePage container'); screenshot review of the outside page at 1440, 1024 and 390 px, light and dark, in English and Vietnamese at 390 (`workspace-acceptance` 'your workspaces outside a workspace'; the Open link is now a 44 px target on a phone); real e2e restores from the outside page. A person with no workspace at all cannot be made on the seeded stack (every registration provisions a personal one), so that landing is proven by the component test.
- State: DONE — lines 1–4 proven in the lane pull request (2026-11-03, branch wsp/finish-0-1); see the Evidence line.

### UC-WSP-D05 Transfer workspace ownership
- Capability: D Workspace lifecycle transition
- Actors and permission: Current Owner (`ManageWorkspace`)
- Preconditions: Target is an active non-Guest member; caller is an active Owner.
- Input: `newOwnerId`, `expectedVersion`, `stepUpToken` (a single-use proof for the `TransferWorkspaceOwnership` step-up purpose, bound to the caller, the session and the purpose, valid 5 minutes).
- Output: Target becomes Owner, caller becomes Admin, grants re-synced, version +1. There is always at least one Owner.
- Main flow: 1. `POST /api/v1/workspaces/{id}/transfer-ownership`. 2. The workspace row is locked (serializes with remove, suspend, role change). 3. The caller is checked as the active Owner and the target as an active member, then the proof is spent (`ISecurityStepUpService.ConsumeAsync`). 4. Domain promotes target and demotes caller. 5. Grants sync in the same transaction.
- No-op / rejection / concurrency / retry: Target = caller: rejected. Target not an active member (removed, suspended, another workspace's person): NotFound. Target is a Guest: rejected with `Workspaces_Member_CannotPromoteGuestToOwner`. A caller who is not an active Owner: refused (Admin, Member and Guest get 403 from the policy; a stranger gets NotFound). Stale workspace version: 412 (the row said 409; the expected-version pipeline answers 412). A concurrent removal of the target cannot interleave (lock). No proof: 412 `identity.security.step-up-required` (a caller with no session too). A stale, spent, other-purpose, other-user or other-session proof: 400 `identity.security.step-up-invalid`. Ownership and version never change on either refusal, and a non-Owner gets the same NotFound before any proof is asked. A caller with an authenticator confirms with a code (or a recovery code), one without two-factor with the current password, one who signs in with a provider only has no factor yet: the dialog says to add a password or two-factor first (the provider re-authentication round trip is not built).
- UI states: Settings danger zone row (Owner only) and a dialog: choose among active non-Guest people (Admins first) with the effect named, nobody eligible (an invite path), then Confirm opens the step-up in the same dialog (authenticator code, recovery code, password, add-a-factor message), wrong code or password, too many tries (wait), expired confirmation (start again), refused for a missing or stale proof (confirm again), pending, success (toast, the former Owner's page becomes an Admin's), refused in place (stale list with Reload, rule refusal, no permission, offline, server error). Phone: the same dialog with touch-size buttons.
- Rules: [workspaces](../../product/workspaces.md) sections 17, 18; WSP-008; WGREQ019; Part 4 LD-05.
- Acceptance: Given Owner A and member B, When A transfers to B, Then B is Owner and A is Admin. Given B is removed in parallel, Then exactly one of the two succeeds.
- Evidence: BE `POST /api/v1/workspaces/{id}/transfer-ownership`, `TransferOwnershipCommandHandler` (spends the proof through `ISecurityStepUpService`), `StepUpPurpose.TransferWorkspaceOwnership` with `MfaChallengePurpose.StepUpTransferWorkspaceOwnership` and `StepUpPurposeMapping`, `TransferOwnershipRequest.StepUpToken`, `WorkspaceMember.PromoteToOwner` (Guest refused) · FE `transfer-ownership-flow.tsx` (choice then proof step), `ownership-step-up.ts` (the port the host fills), `workspace-settings-surface.tsx`, `settings.tsx` (candidates, failure mapping, step-up codes), `apps/web/src/shell/workspace/ownership-step-up.ts` (the host answers with the account feature's step-up, no feature imports another), `createWorkspaceSettingsService.transferOwnership`, mock operation `workspace.transferOwnership` (spends a proof from the account mock's store) · Design `Wsp-Var-Danger-Transfer` (dialog), `-None`, `-Conflict`, `-Done`, `-M`, and `Wsp-Var-Danger-TransferStepUp`, `-Bad`, `-Pw`, `-Expired`, `-Refused`, `-M` on the Workspaces canvas (step-up drawn 2026-11-19); lead decision (2026-11-19): the candidates rule stands (any active non-Guest member, Admins first, this row's rule, LD-05), and a step-up proof is required on the handover (owner decision of 2026-10-30, MFA step-up) · Tests Domain `WorkspaceMemberPromotionTests` (the Guest case failed first: "Expected a <BusinessRuleException> to be thrown, but no exception was thrown."); Application `TransferOwnershipCommandHandlerTests` (17; failed first without the check: "Expected result.Succeeded to be False, but found True." for a handover without a proof; now: no proof, no session, stale or other-purpose proof refused with ownership unchanged, the proof spent for this caller, session and purpose, a non-Owner is NotFound before any proof, plus promote and demote, Guest, suspended, self); Integration `WorkspaceLeaveAndHandoverIntegrationTests` (real pipeline, policy engine and step-up service: `AHandoverWithoutAFreshProof_IsRefused_AndOwnershipDoesNotChange`, `AFreshProof_HandsOver_AndAProofOlderThanItsWindow_IsRefused`, `AProofOfAnotherPurpose_IsRefused_AndOwnershipDoesNotChange`, `AProofOfAnotherUserOrAnotherSession_CannotBeReplayed`, `ASpentProof_CannotBeUsedAgain`, `AnAdminWithoutAProof_StillGetsTheNotFoundAnOutsiderGets`, and the earlier role, Guest, suspended, stale version and other-workspace cases, 26 passed with `WorkspaceMembershipConcurrencyEvidenceTests`); mock `workspace-transfer-ownership.unit.test.ts` (10: no proof 412, unknown or other-purpose proof 400, a proof spent by a refused rule cannot be reused); host `ownership-step-up.unit.test.ts` (4); component `transfer-ownership.interaction.component.test.tsx` (27: Owner only, list and effect, Confirm asks the proof and sends nothing before the code, wrong code, password for a caller without two-factor, recovery code, provider-only message, expired and start again, refused for a missing or stale proof and asked again, too many tries, offline while asking and while answering, cancel, reopen, pending, none eligible, stale list, five refusal kinds, version unknown, phone touch size on both steps, Vietnamese copy) · Real e2e none · PR #645 (the handover), this pull request (the step-up)

- State: DONE — every state and rule of the row has a test that ran and passed (Application 17, Integration 26, mock 10, host 4, component 27, named on the Evidence line), including the failing-first refusal without a fresh proof. The provider re-authentication round trip for a person with no password and no two-factor is not built: the dialog tells them to add a factor first (tested).

### UC-WSP-D06 React to Account suspension or closure
- Capability: D Workspace lifecycle transition
- Actors and permission: System (Accounts fact)
- Preconditions: Accounts publishes a suspended or closed fact.
- Input: Account id, new status.
- Output: Workspace operations are limited as policy says; Workspaces keeps its own lifecycle.
- Main flow: 1. Accounts publishes the fact. 2. A Workspaces consumer applies the approved policy. 3. Members see a clear state.
- No-op / rejection / concurrency / retry: Decided 2026-11-01 (see State); was open: which operations stop (read-only, no read, no sign-in), who may still export, and the principal that applies it are not decided. Tenant-isolation choice.
- UI states: Read-only banner or unavailable page (I05).
- Rules: [workspaces](../../product/workspaces.md) sections 7, 59; WSP-003; see [accounts](accounts.md).
- Acceptance: Not testable until the policy exists.
- Evidence: BE none · FE none · Design `Final-States` (generic) · Tests none · Real e2e none
- State: DEFINED — decided 2026-11-01, not built (owner decision 7, §9 of the README): A suspended Account is read-only end to end: automation runs and outbound webhooks are skipped and recorded with reason account.suspended, public forms and share-link writes refuse, reads stay open, nothing is deleted. The effect of Closed is not covered by this decision.

## E. Membership read

### UC-WSP-E01 List workspace members
- Capability: E Membership read
- Actors and permission: Owner, Admin, Member (`ViewMembers`); Guest refused
- Preconditions: Active membership, not Guest.
- Input: `workspaceId`; paging (LD-06).
- Output: A cursor page (`items`, `nextCursor`) of rows: user id, name, role, status, job title, joined time, version, and the phone when the person opted in and the reader is an active non-guest member (D15). The list carries no email; email is on the Team, Space and security reads (section 48.2).
- Main flow: 1. `GET /api/v1/workspaces/{id}/members?limit&cursor`. 2. Pipeline checks `ViewMembers`. 3. Handler pages oldest member first (user id on ties), maps rows and applies the phone rule. 4. The web client reads the pages it needs and joins them, because the shell, the Board share dialog and the Space and Team pages resolve names from the whole roster.
- No-op / rejection / concurrency / retry: Guest: 403 (GOV-009). Suspended or removed: the same NotFound as a stranger. A person with no seat in an existing workspace (same account, another account, an Account admin, a suspended member): the identical NotFound as for a made-up workspace, status and body (the shared access decision, every workspace-scoped route; `NonMemberDisclosureTests`). `limit` outside 1 to 200 or a cursor the list did not issue: 400. Read-only: no version, no no-op, no retry key.
- UI states: Loading, empty (only me), success with name search, no match, permission denied (a guest reads only their own row and is told why; a refused read says so without a retry), offline, error with retry.
- Rules: [workspaces](../../product/workspaces.md) sections 11, 48.2, 82; WSP-005; WGREQ012, WGREQ013; WX-MEM-001.
- Acceptance: Given 60 members, When any member lists, Then the first page holds 50 and `nextCursor` leads to the other 10, each person once. Given people who joined at the same instant, When walked one at a time, Then none repeats or is skipped. Given a removed member, Then they are never listed. Given a Member and an opted-in colleague, Then the phone shows; given a Guest, Then 403.
- Evidence: BE `GET /api/v1/workspaces/{id}/members`, `GetWorkspaceMembersQueryHandler`, `GetWorkspaceMembersQueryValidator` · FE `workspace-members-surface.tsx`, `members.service.ts` (`getList` joins the pages), `use-workspace-members.ts`, `section-state.ts` · Design none for the workspace roster; `Final-Members` and `Final-M-Members` are the department Members tab (`design-fidelity-members.mock.e2e.spec.ts` measures that tab) · Tests `GetWorkspaceMembersPagingTests` (15), `GetWorkspaceMembersPhoneVisibilityTests`, `WorkspaceAdministrationReadIntegrationTests` (10), `MemberProfileIntegrationTests`, `WorkspaceRoleBaselineIntegrationTests`, `MemberEndpointTests`, `WorkspacesOpenApiShapeTests`, `workspace-pages-model.unit.test.ts`, `guest-members-read.unit.test.ts`, `section-state.unit.test.ts`, `workspace-members.interaction.component.test.tsx` (21), `workspace-pages.mock.e2e.spec.ts` · Real e2e `workspace-members.real.e2e.spec.ts` · Lane run 2026-11-03: line 3 closed. At 390 px in Vietnamese the roster squeezed the Role column to a clipped label; the row is now a two-line grid under `sm`; `workspace-acceptance` 'roster in Vietnamese' and 'invitations in Vietnamese' (390, light and dark) assert no clipped text, no member cell over 80 px, 44 px targets and axe; screenshot read. Component `workspace-members.interaction` 23 passed. Line 4: real stack (project notrelix-fe-real-e2e, 2026-11-03, branch fix/workspaces-acceptance-gaps), `workspace-members.real.e2e.spec.ts` 2 of 2 passed.
- State: DONE — lines 1–4 proven in the lane pull request (2026-11-03, branch fix/workspaces-acceptance-gaps); see the Evidence line.

### UC-WSP-E02 Read my own membership
- Capability: E Membership read
- Actors and permission: Any active member, Guest included (`ViewWorkspace`)
- Preconditions: Active membership.
- Input: `workspaceId`.
- Output: My role, status, job title, version.
- Main flow: 1. `GET /api/v1/workspaces/{id}/members/me`. 2. Handler returns the caller's row.
- No-op / rejection / concurrency / retry: A person with no seat in an existing workspace, a suspended member and a removed one get the identical NotFound as for a made-up workspace (`NonMemberDisclosureTests`). Read-only: no version, no no-op, no retry key. Used by the shell to hide actions the person cannot do; the server still enforces them.
- UI states: Loading, success, not found.
- Rules: [workspaces](../../product/workspaces.md) sections 11, 15; WSP-007; WGREQ082.
- Acceptance: Given a Guest, When they read their membership, Then role is Guest.
- Evidence: BE `GET /api/v1/workspaces/{id}/members/me`, `GetMyWorkspaceMembershipQueryHandler` · FE `members.service.ts` (`parseMyMembership`), `use-workspace-viewer.ts` · Design none · Tests `WorkspaceAdministrationReadIntegrationTests` (own row and version, another workspace and a suspended member refused), `WorkspaceRoleBaselineIntegrationTests` (guest reads only their own row, outsider refused), `MemberProfileIntegrationTests` (a guest's own row carries no phone) · Real e2e `workspace-members.real.e2e.spec.ts` (an Owner and a Guest each read `members/me`)
- State: DONE — accepted 2026-11-02: independent acceptance at `4131c937a` (branch accept/batch-11, #289) found no gap in lines 1–3, CI was green on that commit for the Integration classes, and the real specs ran in the build run of #286 (lines 1–4 PASS). Builder's account: Line 1: `NonMemberDisclosureTests` (19 with its two neighbours, one Testcontainers run) now probes `members/me` beside the roster: a same-account outsider, a person of another account, an Account admin with no seat and a suspended member each get the identical status and body for an existing and for a made-up workspace; `WorkspaceAdministrationReadIntegrationTests` (own row and version, another workspace and a suspended member refused) and `WorkspaceRoleBaselineIntegrationTests` (a guest reads only their own row, an outsider is refused). Line 2: no screen of its own, the shell reads it; `use-my-workspace-membership.component` 2 (loading, role and version from `members/me`, not found leaves no role so the shell offers nothing) and `guest-members-read.unit`. Line 3: no artboard and no screen (the Guest roster rows carry it visually, see E01). Line 4: real stack, `workspace-members.real.e2e.spec.ts` (an Owner and a Guest each read `members/me`) and `workspace-read-and-lists.real.e2e.spec.ts` (a stranger and a removed member get the answer of a made-up id), 5 passed.

## F. Membership add / remove / change

### UC-WSP-F01 Add an existing user as a member directly
- Capability: F Membership add / remove / change
- Actors and permission: Owner, Admin (`InviteMember`); Admin adds only member or guest
- Preconditions: Target user exists; workspace Active.
- Input: `userId`, `role` (member, guest; Owner may add admin).
- Output: Active membership, grant synced, `WorkspaceMemberAdded`. A returning Removed or Suspended member is re-activated with the new role.
- Main flow: 1. `POST /api/v1/workspaces/{id}/members/add`. 2. Actor role cap checked. 3. Row created or re-activated. 4. Grant synced.
- No-op / rejection / concurrency / retry: Already active: rejected. Actor cannot assign the role or act on the existing row: `Workspaces_Member_ActorCannotAssignRole` / `ActorCannotActOnMember`. Defect: the handler does not check that the target is an active member of the same Account, nor a Billing seat (LD-09, LD-08).
- UI states: Add dialog with picker, validation error, pending, conflict, permission denied.
- Rules: [workspaces](../../product/workspaces.md) sections 10, 22.1, WSP-004, WSP-026, WSP-011; WGREQ016.
- Acceptance: Given an Admin, When they add a user as member, Then the user is active. Given the user is in another Account, Then it is refused and nothing changes. Given an Admin adds an Admin, Then rejected.
- Evidence: BE `POST /api/v1/workspaces/{id}/members/add`, `AddMemberCommandHandler`, now requiring an active Account membership (`Workspaces_Member_NotInAccount`) and a Billing seat (`Workspaces_Member_SeatUnavailable`) through the merged branch `fix/add-member-account-and-seat` · FE none (UI uses invitations and Space/Team pickers) · Design none · Tests `NewMemberEndpointTests`, `WorkspaceRoleBaselineIntegrationTests`, `WorkspaceMemberRulesTests`, plus the tests that branch added · Real e2e none
- State: IMPLEMENTING — The defect is closed. Lead decision: kept for admin use; no screen (no UI is built for it). Tests: refused outside the Account, refused with no seat, success, and a Removed member rejoins (before, the add of a Removed member threw because the Domain forbids activating a removed row; it now uses `Rejoin`). Two concurrent admissions can still exceed the seat limit by one (admissions do not serialize; separate use case).

### UC-WSP-F02 Change a member's workspace role
- Capability: F Membership add / remove / change
- Actors and permission: Owner, Admin (`ChangeMemberRole`); Admin only on member and guest targets
- Preconditions: Target is an active member; actor outranks the target.
- Input: `userId`, `role`.
- Output: New role, grants synced, `WorkspaceMemberRoleChanged`. The last Owner cannot be demoted.
- Main flow: 1. `PATCH /api/v1/workspaces/{id}/members/{userId}`. 2. Workspace row lock. 3. Rules: actor cap, last-owner guard. 4. Grant sync.
- No-op / rejection / concurrency / retry: Same role: no-op. Last owner: `Workspaces_Member_CannotActOnLastOwner`. Expected version required (LD-07): the body carries the member's `version`, a version that moved answers 412 `common.precondition-failed` and changes nothing; the same role is a no-op with no version step. Demote/remove race resolved by the lock.
- UI states: Role menu, confirmation for demotion, pending, conflict, permission denied.
- Rules: [workspaces](../../product/workspaces.md) sections 15, 17, 18; WSP-007, WSP-008, WSP-019; WGREQ019, WGREQ020, WGREQ144, WGREQ145; WX-MEM-001.
- Acceptance: Given two Owners, When one is demoted, Then it succeeds. Given one Owner, Then it fails and nothing changes. Given a stale version, Then 412 and nothing changes.
- Evidence: BE `PATCH /api/v1/workspaces/{id}/members/{userId}`, `UpdateMemberRoleCommand` is now an `IExpectedVersionRequest` mapped to the WorkspaceMember in `ExpectedVersionTargetMap`; the request body has `expectedVersion` · FE `members.service.ts` reads each member's `version` and sends it (a member with no known version is never written), `use-member-mutations.ts` (optimistic role, rolled back on failure), Members surface asks before a lowered role (Admin to Member, anything to Guest), conflict toast already existed; mock backend checks and moves the version · Design `Final-Members` (department Members tab; no roster artboard) · Tests `WorkspaceMemberRulesTests`, `WorkspaceMembershipConcurrencyEvidenceTests`, `WorkspaceMemberVersionTests`, `workspace-pages.mock.e2e.spec.ts` (WX-MEM-001); existing tests that build the command without a version need updating, see the test list under F05 · Real e2e workspace-member-changes.real.e2e.spec.ts · Acceptance run 2026-11-02 by an independent acceptance agent that did not build it, at develop `8d139a73e` (branch accept/batch-5); the backend suites ran at `2c9eb1874`, the parent of 8d139a73e, whose only change (#262) touches no backend file. Line 1: Backend: Domain `Workspaces` 493, Application `Workspaces` 286, API `Workspaces` 188 and the Workspaces integration suites (membership and invitation concurrency, accept transaction, escalation, role baseline, invitation token flow, self-targeted change) 66 passed, 0 failed. Domain `WorkspaceMemberRulesTests` and `WorkspaceMemberVersionTests`; Integration `WorkspaceMembershipConcurrencyEvidenceTests` (both Owners demoted at once never leave zero) and `SelfTargetedAccessChangeIntegrationTests`; on the real stack `workspace-member-changes.real.e2e.spec.ts` (raise, lowering with confirmation, same role is a no-op with the version unchanged, stale version 412 with nothing changed, a Member 403, the only Owner refused) and, for the cross-tenant negative, `workspace-cross-scope.real.e2e.spec.ts` (added in this run because no test covered it; it first failed because it fired four writes at once on one session and every CSRF read rotates the token, so it writes one after the other and passes 1 of 1): another workspace's member id on this workspace's route answers 404 and the row is unchanged, and a signed-in stranger gets the statuses a made-up workspace id gets. Line 2: `workspace-members.interaction.component.test.tsx` 21 passed (workspace, share and table packages: 436 web component tests and 625 node tests passed); `workspace-roster-states.real.e2e.spec.ts` (a 412 shows the conflict notice and the select returns to the member's role, an offline write is refused and says so, the row menu opens by keyboard, axe has no serious violation). Line 3 (the canvas draws only the department Members tab, so the workspace roster has no artboard; `design-fidelity-members` 9 of 9 passed for that tab): roster, row menu and removal confirmation, and the role-lowering confirmation (a test added to the acceptance spec in this run) and the read-only roster of a Member (read from a probe that is not committed), read at 1440, 1024 and 390 px in light and dark (`workspace-acceptance.mock.e2e.spec.ts`: 78 of 78, no overflow, 44 px targets at 390 px, axe clean); at 390 px the Owner row's name wraps to two lines beside the Role column and stays readable. Line 4: Real stack (fresh `notrelix-fe-real-e2e` stack): `workspace-member-changes` 2, `workspace-roster-states` 2, `workspace-invitations-manage` 2, `workspace-invitation-link` 1, `workspace-invitations-answer` 1, `workspace-read-and-lists` 3, `workspace-cross-scope` 1 (added in this run), plus `work-item-states` 8, `board-sharing` 1, `approvals` 3, `item-lifecycle` 3, `sub-items` 2 and `board-settings` 1: 30 of 30 passed (the cross-scope spec passed on its rerun after a spec fix, see below).
- State: DONE — accepted by independent acceptance 2026-11-02 at `8d139a73e` (branch accept/batch-5); see the Evidence line.

### UC-WSP-F03 Suspend a member
- Capability: F Membership add / remove / change
- Actors and permission: Owner, Admin (`RemoveMember`); Admin only on member and guest targets
- Preconditions: Target is an active member; not the last owner.
- Input: `userId`.
- Output: Status Suspended; the grant is revoked; history and work stay; person cannot enter.
- Main flow: 1. `POST /api/v1/workspaces/{id}/members/{userId}/suspend`. 2. Lock and rules. 3. Domain suspends. 4. Grant removed.
- No-op / rejection / concurrency / retry: Already suspended: no-op. Last owner: rejected. Yourself as target: rejected `Workspaces_Member_CannotActOnSelf`, nothing changes (a person leaves through F06). Expected version required (LD-07): body `expectedVersion`, stale answers 412 and changes nothing; a suspended member is a no-op with no version step. A suspended member's session loses access at the next request.
- UI states: Row action, confirmation naming consequences, pending, permission denied.
- Rules: [workspaces](../../product/workspaces.md) sections 13, 21; WGREQ015, WGREQ021.
- Acceptance: Given an active member, When suspended, Then reads of the workspace give NotFound. Given the last owner, Then rejected.
- Evidence: BE `POST /api/v1/workspaces/{id}/members/{userId}/suspend`, `SuspendMemberCommand` is an `IExpectedVersionRequest` (new `MemberVersionRequest` body) · FE `members.service.ts` and the Members surface send the member version; mock backend checks it · Design `Final-Members` · Tests `WorkspaceMemberTests`, `WorkspaceMembershipOutboxEvidenceTests`, `WorkspaceMemberSuspensionTests` (Domain: no-op, last owner, removed), `SuspendMemberCommandHandlerTests` (no-op, version and resource), `EfRequestDataSessionExpectedVersionTests` (stale and current target-member version) · Real e2e none
- State: DONE — Proven by tests that ran in this PR. Domain: `WorkspaceMemberSuspensionTests` (no-op, last owner, removed), `WorkspaceMemberTests` (79 Domain tests pass across the sweep classes). Application: `SuspendMemberCommandHandlerTests` (8 pass: suspend, grant revoked, API tokens stopped, already-suspended no-op with no version step, missing member NotFound, self refused before any change, version resource). Infrastructure: `EfRequestDataSessionExpectedVersionTests` (11 pass, stale answers 412 and changes nothing). Integration `WorkspaceRoleBaselineIntegrationTests` (`Admin_SuspendsActivatesAndRemoves_MemberAndGuestTargets`, `Admin_CannotRemoveSuspendOrActivate_OwnerOrAnotherAdmin`, `Owner_StillSuspendsAnAdmin_AndCannotRemoveThemselfHere`, `MemberAndGuest_CannotRemoveSuspendOrActivate`), `RlsRuntimeEnforcementTests.RuntimeMembershipSuspend_RevokesGrant_AndDeniesUnderAppRole` and `NonMemberDisclosureTests.SuspendedMember_GetsTheIdenticalNotFound_ForExistingAndMadeUpIds` (a suspended member reads nothing, identical NotFound); the Workspaces and Rls Integration shards pass (545 and 621). Frontend: `workspace-members.interaction.component.test.tsx` (25 pass: row action, confirmation, last owner offers no suspend, read-only for members) and `members-suspend-activate.component.test.tsx` (7 pass: the confirmation names the consequence and the workspace, nothing is sent before it, the write quotes the member version, the row shows busy, a stale version 412 and a 403 each say nothing was suspended).

### UC-WSP-F04 Activate a suspended member
- Capability: F Membership add / remove / change
- Actors and permission: Owner, Admin (`RemoveMember`)
- Preconditions: Target is Suspended.
- Input: `userId`.
- Output: Status Active; grant restored.
- Main flow: 1. `POST .../members/{userId}/activate`. 2. Domain activates. 3. Grant synced.
- No-op / rejection / concurrency / retry: Already active: no-op, no version step, no seat check. Removed member: rejected (re-add instead). A suspended member takes a seat again: Billing is asked first and no free seat is refused with `Workspaces_Member_SeatUnavailable` (LD-08). Expected version required (LD-07): stale answers 412.
- UI states: Row action, pending, success.
- Rules: [workspaces](../../product/workspaces.md) sections 13, 21; WGREQ021.
- Acceptance: Given a Suspended member, When activated, Then they read the workspace again.
- Evidence: BE `POST /api/v1/workspaces/{id}/members/{userId}/activate`, `ActivateMemberCommandHandler` now takes `IBillingSeatGate` and the command is an `IExpectedVersionRequest` · FE `members.service.ts`, the Members surface (the seat refusal shows the producer's rule message in the toast) · Design `Final-Members` · Tests `WorkspaceMemberTests`, `ActivateMemberCommandHandlerTests` (seat refusal, seat-free path, active no-op, removed refusal), `WorkspaceMemberSuspensionTests`; `MemberActorTargetGuardHandlerTests` builds the activate handler with the seat gate · Real e2e none
- State: DONE — Proven by tests that ran in this PR. Application: `ActivateMemberCommandHandlerTests` (5 pass: seat refusal, seat-free path, active no-op with no seat check, removed refusal), `MemberActorTargetGuardHandlerTests` (12 pass). Domain `WorkspaceMemberSuspensionTests`. Integration `WorkspaceRoleBaselineIntegrationTests` (Admin activates member and guest, Admin cannot activate an Owner or another Admin, Member and Guest cannot); Workspaces shard 545 pass. Stale version 412: `EfRequestDataSessionExpectedVersionTests` (11 pass). Frontend `members-suspend-activate.component.test.tsx` (7 pass): reactivating quotes the member version and succeeds, a refused seat shows the producer's reason in the toast, a stale version says nothing was reactivated. Decision (lead, 2026-11-19): a seat refusal offers no upgrade path on this screen; upgrading belongs to Billing.

### UC-WSP-F05 Remove a member
- Capability: F Membership add / remove / change
- Actors and permission: Owner, Admin (`RemoveMember`); Admin only on member and guest targets
- Preconditions: Target is a member; not the last owner.
- Input: `userId`.
- Output: Status Removed; grant revoked; assignments and history stay. A default workspace of the removed person is not cleared (only leaving clears it); readers treat a default the person cannot open as absent, and Identity is not written for another person.
- Main flow: 1. `DELETE /api/v1/workspaces/{id}/members/{userId}`. 2. Lock, rules. 3. Domain removes. 4. Grant revoked.
- No-op / rejection / concurrency / retry: Already removed: no-op. Last owner: rejected. Expected version required (LD-07): query `expectedVersion`, stale answers 412. Removal does not clean Space and Team entries (F09). Yourself as target: rejected `Workspaces_Member_CannotActOnSelf`, nothing changes (a person leaves through F06).
- UI states: Confirmation dialog, pending, success, permission denied.
- Rules: [workspaces](../../product/workspaces.md) sections 17, 20, 67; WGREQ017, WGREQ023; WX-MEM-001.
- Acceptance: Given a member with assigned items, When removed, Then the items keep their assignee id and the member reads nothing.
- Evidence: BE `DELETE /api/v1/workspaces/{id}/members/{userId}?expectedVersion=`, `RemoveMemberCommand` is an `IExpectedVersionRequest` · FE `use-member-mutations.ts`, Members surface · Design `Final-Members` · Tests `WorkspaceMemberSelfRemovalTests`, `WorkspaceMembershipConcurrencyEvidenceTests`, `workspace-pages.mock.e2e.spec.ts`. Existing tests that build the four member commands without a version and must be updated: `MemberActorTargetGuardHandlerTests` (7), `SuspendMemberCommandHandlerTests` (3), `RlsRuntimeEnforcementTests` (2), `WorkspaceMembershipConcurrencyEvidenceTests` (3), `WorkspaceRoleBaselineIntegrationTests` (11), and any activate handler test (new constructor argument) · Real e2e workspace-member-changes.real.e2e.spec.ts · Acceptance run 2026-11-02 by an independent acceptance agent that did not build it, at develop `a6708f501` (branch accept/batch-3). Line 1: Domain `WorkspaceMemberRulesTests`, `WorkspaceMemberVersionTests`, `WorkspaceMemberSelfRemovalTests` (leaving twice is a no-op, the last Owner cannot leave); Integration `WorkspaceRoleBaselineIntegrationTests` (Admin removes Member and Guest, cannot remove an Owner or another Admin, a Member and a Guest are Forbidden, the Owner cannot remove self) and `WorkspaceMembershipConcurrencyEvidenceTests`; the acceptance sentence had no test, so `RemovingAMemberWithAssignedItems_KeepsTheAssigneeRow_AndTheRemovedPersonReadsNothing` was added (the item keeps its assignee row, the removed person is refused the item); the 412 on a stale version, the 403 for a Member, the self-removal refusal and the read-nothing answer run on the real stack in `workspace-member-changes.real.e2e.spec.ts` (2 tests, 21 of 21 passed across the eight real specs of this run, 3.7 min). Line 2: `workspace-members.interaction.component.test.tsx` (confirmation in an AlertDialog before removal) and `workspace-roster-states.real.e2e.spec.ts` (2 tests, conflict, offline write, keyboard, axe). Line 3 (no roster artboard, so reviewed): the roster, row menu and removal dialog sheets of `workspace-acceptance.mock.e2e.spec.ts` (60 passed) were opened at 1440, 1024 and 390 px in both themes and read: the menu and the removal dialog fit and their buttons are 44 px at 390 px. Not driven in a browser: a repeat removal of a removed person (the Domain no-op is proven).
- State: DONE — accepted by independent acceptance 2026-11-02 at `a6708f501` (branch accept/batch-3); see the Evidence line.

### UC-WSP-F06 Leave a workspace
- Capability: F Membership add / remove / change
- Actors and permission: Any active member, Guest included; subject is the caller
- Preconditions: Active membership; another active Owner exists if the caller is an Owner.
- Input: `workspaceId` (path); no user input.
- Output: Membership ends like a removal; the grant is revoked in the same transaction; a default workspace pointing here is cleared.
- Main flow: 1. `DELETE /api/v1/account/workspaces/{workspaceId}`. 2. Handler resolves the caller. 3. Last-owner guard. 4. Remove and clear default.
- No-op / rejection / concurrency / retry: Last Owner: 409 `workspaces.member.last-owner-cannot-leave`. Already left: NotFound. A retry after success is NotFound (not idempotent); the account service now treats that 404 as done and the list is read again. The leave has no expected version (the caller's own membership is the only thing it touches). The workspace grant ends in the same transaction, so requests are refused at once; the realtime access signal is not enrolled on a leave (the outbox row would be written after the grant ended and the policy refuses it), so the leaver's live connection is cut by the periodic re-authorization pass, within 60 s. Lead decision (2026-11-19): accepted for 0.1; the API refusing at once is the security boundary and the RLS outbox path stays untouched.
- UI states: Account page: confirmation (names that the person also leaves departments and Teams), sole-owner disabled with reason (`canLeave`) and a Transfer ownership link to the workspace settings, pending, success, failure in words.
- Rules: [workspaces](../../product/workspaces.md) section 19, 19.1; WGREQ018.
- Acceptance: Given two Owners, When one leaves, Then success. Given one Owner, Then 409 and no change.
- Evidence: BE `DELETE /api/v1/account/workspaces/{workspaceId}`, `LeaveWorkspaceCommandHandler` (runs `MemberDepartureCleanup` before the grant ends) · FE account `workspaces.tsx` (the sole-owner note, the disabled button and the new handover link), `account-workspaces-surface.tsx`, `account.service.ts` (404 counts as done) · Design `Final-Account`; the Members-page entry drawn on `Wsp-Var-Members-Leave` and `-M` is a second surface this row does not list; lead decision (2026-11-19): skipped for 0.1.0 · Tests `WorkspaceMemberSelfRemovalTests`, `LeaveWorkspaceHandlerTests` (Guest, Member and Admin leave; an Owner leaves when another remains; the last Owner is refused with the dotted code and nothing changes; a suspended Owner does not count; not found), `AccountWorkspaceEndpointTests`; Integration `WorkspaceLeaveAndHandoverIntegrationTests` (the sole Owner is refused until another member holds Owner, then leaves; a Member and a Guest leave and a second leave is refused); component `account-workspaces.interaction.component.test.tsx` (confirm, sole owner with the handover link, none on other rows) and `account-pages.component.test.tsx` ('AccountWorkspacesPage container' and 'leave states': success, default fallback, onboarding fallback, last owner refused by the producer, pending, failed leave) · Real e2e none · PR #645
- State: DONE — every state and rule this row lists has a passing test (see Evidence); known limit, written in the rules above: the live connection of the leaver is cut within 60 s, not instantly.

### UC-WSP-F07 Set my own job title
- Capability: F Membership add / remove / change
- Actors and permission: Any active member (`ViewWorkspace` on own row)
- Preconditions: Active membership; current member version.
- Input: `jobTitle` (max 100, trimmed, no control chars; blank clears), `expectedVersion`.
- Output: Title stored on the membership, member version +1; shown in member and Team lists, only inside the workspace.
- Main flow: 1. `PUT /api/v1/workspaces/{id}/members/me/job-title`. 2. Version check. 3. Domain sets title.
- No-op / rejection / concurrency / retry: Same title: no-op. Stale version: 409. Removed member: not editable.
- UI states: Edit on the Account profile page (the profile dialog), validation error, pending, saved, conflict, error.
- Rules: [workspaces](../../product/workspaces.md) section 11 (D15, 2026-10-23).
- Acceptance: Given title `Lead`, When saved again, Then version is unchanged.
- Evidence: BE `PUT /api/v1/workspaces/{id}/members/me/job-title`, `UpdateMyJobTitle.cs` (`UpdateMyJobTitleCommandValidator.cs`) · FE consumer row `Workspaces.Members.UpdateMyJobTitle` (enabled surface) · Design none · Tests `MemberProfileIntegrationTests`, `WorkspaceMemberJobTitleTests`, `CoMemberProfileTests`, `account-pages.component.test.tsx`, `account-profile.interaction.component.test.tsx` (the Account profile control) · Real e2e none
- State: DONE — Proven by tests that ran in this PR. Domain `WorkspaceMemberJobTitleTests` (trim, same value no-op keeps the version, blank clears, 100 accepted and 101 rejected, control characters, removed refused). Application `UpdateJobTitleCommandHandlerTests` (12 pass, including the 100 ok, 101 rejected and control-character cases on both validators; a regression guard, the server already enforced the limit). Integration `MemberProfileIntegrationTests` (`StaleVersion_Is412_AndLeavesTheTitleUnchanged_AndAVersionlessWriteIsRejected`, `SemanticNoOp_KeepsTheVersion_AndBlankClears`, `TooLongOrMultilineTitle_IsRejected_AndNothingChanges`, `RemovedMember_IsNotEditable`, `PlainMemberAndGuest_CannotSetAnotherMembersTitle_ButEachCanSetTheirOwn`) in the Workspaces shard (545 pass). Frontend `account-profile.interaction.component.test.tsx` (17 pass) and `account-pages.component.test.tsx` (49 pass, describe `job title`): saved with the membership version (`PUT members/me/job-title`, `expectedVersion: 4`), blank clears, re-spacing sends nothing, over 100 characters is refused inline before anything is sent, the input waits while saving, a stale version 412 says nothing was saved, a server or network failure shows the save error. Defect found and fixed: the profile page had no length check on the job title; failing-first `expected 'ProfileUpdate your personal informati…' to contain 'A job title is at most 100 characters.'`. Decision (lead, 2026-11-19): the Account profile page is the profile dialog this row means.

### UC-WSP-F08 Set another member's job title
- Capability: F Membership add / remove / change
- Actors and permission: Owner, Admin (`ManageMemberProfile`)
- Preconditions: Target is an active member.
- Input: `userId`, `jobTitle`, `expectedVersion`.
- Output: Title stored; no role, status or permission changes.
- Main flow: 1. `PUT .../members/{userId}/job-title`. 2. Pipeline checks `ManageMemberProfile`. 3. Version check, set.
- No-op / rejection / concurrency / retry: As F07. A caller without the action: 403.
- UI states: Members page, row menu "Edit job title" opening the edit dialog; pending, success, validation (too long), conflict (stale version, with Reload), permission denied, failure. Offered to the Owner and Admin for every other member; never on the viewer's own row.
- Rules: [workspaces](../../product/workspaces.md) section 11.
- Acceptance: Given an Admin, When they set a Member's title, Then it shows in lists.
- Evidence: BE `PUT /api/v1/workspaces/{id}/members/{userId}/job-title`, `UpdateMemberJobTitleCommandHandler` · FE `edit-job-title-dialog.tsx`, Members row menu (`workspace-members-surface.tsx`), `members.service.ts` (`updateJobTitle`), `use-member-mutations.ts` · Design `Wsp-Var-JobTitle`, `Wsp-Var-JobTitle-Err` (Workspaces canvas) · Tests `MemberProfileIntegrationTests`, `ArchivedWorkspaceWriteGateIntegrationTests`, `workspace-members.job-title-entries.component.test.tsx`, `members-job-title-entries.unit.test.ts` · Real e2e none
- State: DONE — Proven by tests that ran in this PR. Integration `MemberProfileIntegrationTests` (Owner and Admin set another member's title and the list shows it with the new version; a plain Member and a Guest are refused and each can set their own; a stale version is 412 and leaves the title; a versionless write is rejected; a semantic no-op keeps the version and a blank clears; a too-long or multi-line title is rejected; a removed member is not editable; another workspace's Admin cannot edit or read a member), in the 29-test run with the invitation classes (exit 0); `ArchivedWorkspaceWriteGateIntegrationTests` (a title write is refused in an archived workspace for every role). Frontend `workspace-members.job-title-entries.component.test.tsx` (the edit saves the trimmed title and closes; a blank title clears it; past 100 characters shows the field error and sends nothing; pending disables Save and sends once; a stale version shows the conflict with Reload, which reads the members again; a refusal and a plain failure each say what happened; an Admin may edit the Owner's title; the viewer's own row offers no edit; no action without the host callback) and `members-job-title-entries.unit.test.ts` (the write is a PUT with the trimmed title and the member's version, a blank sends null, an unknown version is never written). No server rule was broken, so no failing-first test. Decisions: the edit is a dialog from the Members row menu, not the member profile page the board `Wsp-Var-JobTitle` draws (the profile page is not built); the limit is the server's 100 characters, the board's hint says 80 and is to be corrected on the canvas.

### UC-WSP-F09 Clean a person's Space and Team entries when they leave (enterprise baseline)
- Capability: F Membership add / remove / change
- Actors and permission: System (inside F05 and F06)
- Preconditions: A membership ends.
- Input: Workspace id, user id.
- Output: The person's User entries in Spaces and Teams are removed in the same transaction; a Team left without a Lead is flagged.
- Main flow: 1. Removal or leave runs. 2. The same transaction deletes Space entries and Team memberships of that user. 3. Facts are published.
- No-op / rejection / concurrency / retry: A sole Space Admin removal is allowed (Owners and Admins still manage every Space). A sole Team Lead removal is allowed and leaves the Team with no Lead (LD-12). Archived Spaces and Teams are cleaned too. A person with no entries is a plain leave or removal (no version bump, no event). Only this workspace and its Account are touched. Spaces and Teams that are soft-deleted are not read (the tenant filter and the soft-delete filter are one), so a deleted one restored later still lists the person.
- UI states: Teams list says 'Needs a Team lead' on the row and counts them in the summary; the Team page shows a banner.
- Rules: [workspaces](../../product/workspaces.md) section 19 ('still to define: cleanup'), WSP-024, WSP-014; Part 4 LD-12.
- Acceptance: Given a person in two Spaces and a Team, When removed, Then they hold no entry anywhere in the workspace.
- Evidence: BE `MemberDepartureCleanup` (Application, called by `LeaveWorkspaceCommandHandler` and `RemoveMemberCommandHandler` in the same unit of work), `Space.RemoveDepartedUser`, `Team.RemoveDepartedMember` (they raise `SpaceMemberRemovedDomainEvent` and `TeamMemberRemovedDomainEvent`) · FE `workspace-teams-models.ts` (`needsLead`), `workspace-teams-surface.tsx`, `workspace-team-surface.tsx` · Design `Wsp-Var-Teams-NeedLead-T` (Workspaces canvas) · Tests Domain `MemberDepartureCleanupDomainTests` (7: sole Admin and sole Lead go, archived, absent is a no-op, a Team entry is not a User entry); Application `MemberDepartureCleanupHandlerTests` (4, two of them failed first: "Expected InSpace(own, _person) to be False because the sole Space Admin entry goes with the person (LD-12), but found True."; removal and leave, this workspace only, a refused leave cleans nothing, no entries); Integration `WorkspaceLeaveAndHandoverIntegrationTests` (a person who is sole Admin of a Space, a plain entry of another and the sole Lead of a Team: after a leave and after a removal no Space or Team entry survives, other people's entries stay, the Team is left without a lead, another workspace's Owner cannot reach them; runs under the narrowed database roles); component `team-needs-lead.interaction.component.test.tsx` (3: list flag and count, extras unread is not flagged, Team banner) · Real e2e none · PR #645
- State: DONE — every state and rule this row lists has a passing test (see Evidence); the facts are raised as Domain events and reach the outbox through the existing dispatch, which is not asserted separately.

### UC-WSP-F10 Provision members from an identity directory (SCIM)
- Capability: F Membership add / remove / change
- Actors and permission: System principal of Accounts (undefined)
- Preconditions: Accounts SCIM directory is linked to the workspace.
- Input: Directory user, role.
- Output: Membership change through Workspaces rules.
- Main flow: 1. Accounts orchestrates. 2. It calls a Workspaces action. 3. Workspaces enforces invariants.
- No-op / rejection / concurrency / retry: BLOCKED: the execution principal and its authorization are undefined; no Workspaces provisioning action exists (only personal workspace). Security-principal choice.
- UI states: None in Workspaces.
- Rules: [workspaces](../../product/workspaces.md) sections 76, 77; WSP-020; see [accounts](accounts.md).
- Acceptance: Not testable until the principal model exists.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: BLOCKED — Decision: system principal for provisioning and its permission.

## G. Invitation create

### UC-WSP-G01 Invite a person to the workspace by email
- Capability: G Invitation create
- Actors and permission: Owner, Admin (`InviteMember`); Admin invites member or guest only
- Preconditions: Workspace Active; inviter has a verified email.
- Input: `email`, `role` (member or guest; never admin), optional entries (G02).
- Output: A Pending invitation (expiry from settings, default 7 days), only a token hash stored, and an email sent through the outbox.
- Main flow: 1. `POST /api/v1/workspaces/{id}/members`. 2. Role and actor rules. 3. Domain creates the invitation with a hashed token. 4. Outbox sends the email with the raw token once.
- No-op / rejection / concurrency / retry: Admin role: `Workspaces_Invitation_CannotInviteAsAdmin`. A pending invitation for the same email answers 409 with the stable code `workspaces.invitation.pending-exists` (LD-04). A retry under the same `Idempotency-Key` returns the first invitation and sends no second email (LD-01). The raw token is never stored or logged. An invitation to the address of someone who already belongs is not refused at creation (acceptance handles an existing member); the product docs do not say it must be. No Billing seat check at invite time (checked at accept).
- UI states: Invite dialog (emails, role), validation error, conflict, pending, success, permission denied.
- Rules: [workspaces](../../product/workspaces.md) sections 22.1 (WSP-026), 23, 25, 26; WSP-006; WGREQ024 to WGREQ028, WGREQ033; WX-MEM-001.
- Acceptance: Given an Admin, When they invite `a@x.com` as member, Then one Pending invitation exists and one email is queued. Given role admin, Then 400 with the rule code. Given a pending one for the same email, Then no second row.
- Evidence: BE `POST /api/v1/workspaces/{id}/members`, `InviteMemberCommandHandler`, now an `IIdempotentRequest` (`workspaces.invitation.invite-member.v1`, key required on the endpoint) and a `ConflictException` with the stable duplicate code · FE `invitations.service.ts`, `use-create-invitation.ts` (one key per address and role, reused on retry, cleared on success), Members surface (a duplicate raised by someone else shows the same message as the check before sending), `seat-invite.tsx`; mock backend keys and refuses the same way · Design `Final-Members` · Tests `WorkspaceInvitationTests`, `WorkspaceInvitationRulesTests`, `InvitationEndpointTests`, `NewInvitationEndpointTests`, `WorkspaceInvitationSecretTelemetryTests`, `SendInvitationEmailConsumerTests`, `workspace-pages.mock.e2e.spec.ts`; existing tests that send the invite without a key, or assert the old plain duplicate text, need updating · Real e2e workspace-invitations-manage.real.e2e.spec.ts · Acceptance run 2026-11-02 by an independent acceptance agent that did not build it, at develop `a6708f501` (branch accept/batch-3). Line 1: Domain `WorkspaceInvitationTests` and `WorkspaceInvitationEntriesAndResourceGrantTests` (an Owner role is refused), Integration `WorkspaceInvitationSecretTelemetryTests` (the raw token is never stored, returned or emitted) and `WorkspaceRoleBaselineIntegrationTests` (an Admin invites Member and Guest, not Admin), Infrastructure `SendInvitationEmailConsumerTests`; the duplicate (409 `workspaces.invitation.pending-exists`), the Admin role (400), a Member as inviter (403) and the same-key retry (the first invitation returned, one e-mail written) are proven on the real stack in `workspace-invitations-manage.real.e2e.spec.ts` (2 tests; 21 of 21 passed across the eight real specs of this run). Line 2: the Members component tests (`workspace-members.interaction.component.test.tsx`, 146 passed in the batch with the onboarding, Space dialog, table and mutation tests) and `workspace-roster-states.real.e2e.spec.ts`. Line 3 (no artboard for the form, so reviewed): the invitations sheets were opened at 1440, 1024 and 390 px in both themes; the form, the role select and the send button fit and are 44 px at 390 px (the pending list is G03). Added 2026-11-02 by the fix lane (`fix/acceptance-batch-3`): the Admin role refusal, the duplicate pending address (409 `workspaces.invitation.pending-exists`, no second row, another spelling of the address counts as the same) and the same-key retry (the first invitation replayed, one row) now also run through the real pipeline in `WorkspaceRoleBaselineIntegrationTests` (27 passed in the class). Not proven: delivery through a real mail provider.
- State: DONE — accepted by independent acceptance 2026-11-02 at `a6708f501` (branch accept/batch-3); see the Evidence line.

### UC-WSP-G02 Invite with initial Team and Space entries
- Capability: G Invitation create
- Actors and permission: Owner, Admin; `ManageTeams` for Team entries and `ManageSpaces` for Space entries
- Preconditions: Named Teams and Spaces are active in the workspace.
- Input: G01 plus `entries` (Team member or lead, Space role). A Guest invitation cannot name a Team lead or Space admin.
- Output: The invitation stores the entries; they apply atomically on acceptance.
- Main flow: 1. Same call as G01 with entries. 2. Each kind needs its own action. 3. Acceptance applies membership and entries in one transaction.
- No-op / rejection / concurrency / retry: Inactive Team or Space at acceptance: acceptance fails closed, nothing changes, the issuer invites again. A rule granting only one of the two actions does not authorize a mixed invitation. An existing entry is raised, never lowered.
- UI states: Entry picker in the invite form (Invitations tab): each chosen Team or Space with its role and a remove button, only what the inviter may assign; a kind the inviter holds no action for is not offered and says why; the producer's refusal of an entry shows next to the form; a Guest invitation offers no Team lead or Space admin.
- Rules: [workspaces](../../product/workspaces.md) section 22.1; WSP-026; WGREQ027.
- Acceptance: Given both actions, When an invitation names a Team and a Space, Then both entries exist after acceptance. Given only `ManageSpaces`, Then a mixed invitation is refused.
- Evidence: BE `InviteMemberCommandHandler`, `InvitationInitialEntries` · FE `InvitationEntries` in `workspace-members-surface.tsx`, `invitations.service.ts` (`initialEntries`), `members.tsx` (options by the inviter's actions) · Design `Wsp-Var-Invites-Invite`, `Wsp-Var-Invites-Entry-Denied` (Workspaces canvas), `Final-Members` · Tests `InviteMemberInitialEntriesTests`, `InvitationMixedEntriesAuthorizationIntegrationTests`, `GuestAndEntryInvitationIntegrationTests`, `workspace-members.job-title-entries.component.test.tsx`, `members-job-title-entries.unit.test.ts` · Real e2e none
- State: HARDENING — Proven by tests that ran in this PR. Application `InviteMemberInitialEntriesTests` (6 pass: a Space of another workspace, an archived Space and a Team that is not active are each refused and nothing is stored or sent; a Guest invitation naming a Team lead or a Space admin is refused with `Workspaces_Invitation_GuestEntryRoleTooPrivileged` and nothing is stored; active targets are stored with the invitation and it is sent). Integration `InvitationMixedEntriesAuthorizationIntegrationTests` (a mixed invitation needs both actions, one action alone is denied and persists nothing, a deny rule blocks an Admin) and `GuestAndEntryInvitationIntegrationTests` (membership and entries apply atomically, a failure rolls both back, a Team that is gone fails closed), 29 pass with the member-profile class (exit 0). Frontend `workspace-members.job-title-entries.component.test.tsx` (the chosen Team and Space entries are sent with the invitation; a Team lead entry is sent as lead and removing an entry drops it; switching to Guest turns a lead entry into member and says a Guest cannot lead; a kind the inviter cannot assign is not offered and says why; with no options there is no picker and a plain invitation carries no entries; the producer's refusal shows next to the form) and `members-job-title-entries.unit.test.ts` (the wire kind and role names). The server already refused every entry the inviter cannot grant, so no failing-first test. Remaining gaps: (1) the producer answers one message for all entries, so the refusal is not shown per entry as the row lists; (2) the picker is inline in the invite form, not the dialog `Wsp-Var-Invites-Invite` draws; (3) the Teams and Spaces the inviter may assign are limited by their actions, not by which Spaces they can see.

### UC-WSP-G03 List workspace invitations
- Capability: G Invitation create
- Actors and permission: Owner, Admin (`InviteMember`; a Member and a Guest get 403)
- Preconditions: Active membership.
- Input: `workspaceId`.
- Output: Rows: email, role, status (Pending, Accepted, Revoked, Expired, Declined), issuer (`invitedBy`, the issuing member's user id; the roster names them), expiry, version. No token.
- Main flow: 1. `GET /api/v1/workspaces/{id}/invitations`. 2. Handler lists.
- No-op / rejection / concurrency / retry: A Member and a Guest: 403 (`InviteMember`, lead decision 2026-11-02; section 82 calls invitations sensitive, so invited e-mail addresses are for the people who may invite, a pipeline-level rule and not a handler role check). The read is a cursor page (LD-06): default 50, at most 200, newest first; `limit` outside 1 to 200 or a cursor the list did not issue answers 400. A Pending invitation past its expiry is reported as Expired (LD-04); the sweep that writes the status is not built.
- UI states: Loading, empty, success, permission denied.
- Rules: [workspaces](../../product/workspaces.md) sections 14, 25, 82; WGREQ025, WGREQ032.
- Acceptance: Given a Member or a Guest, When they list invitations, Then 403. Given an Admin, Then rows without tokens.
- Evidence: BE `GET /api/v1/workspaces/{id}/invitations`, `GetWorkspaceInvitationsQueryHandler` · FE `use-workspace-invitations.ts`, Members surface 'pending-invitations' · Design `Final-Members` · Tests `InvitationScopeTests`, `InvitationEndpointTests`, `WorkspaceRoleBaselineIntegrationTests` (Guest 403, Admin and Member list) · FE gated by `enabled: admin` in `members.tsx` · Built: `GetWorkspaceInvitationsQuery(Cursor, Limit)` returns a `CursorPage`, rows carry `version`, expired read; `invitations.service.ts` joins the pages; mock backend pages the same way. Existing tests that read the list as an array (`WorkspaceAdministrationReadHandlerTests`, `WorkspaceAdministrationReadIntegrationTests`, `WorkspaceRoleBaselineIntegrationTests`) need updating · Real e2e workspace-invitations-manage.real.e2e.spec.ts · Lane run 2026-11-03: line 1: Application `GetWorkspaceInvitationsPagingTests` and API `InvitationEndpointTests` (default 50, limit 1–200, a bad cursor 400, ties, no repeats; 68 passed with the Space endpoint class). Line 2: component `workspace-members.interaction` 23 passed, among them the invitations loading skeleton (no rows, no empty text) and the error with a retry that calls only the invitations retry. Line 4: real stack (project notrelix-fe-real-e2e, 2026-11-03, branch fix/workspaces-acceptance-gaps), `workspace-invitations-manage.real.e2e.spec.ts` 2 of 2 passed.
- State: DONE — lines 1–4 proven in the lane pull request (2026-11-03, branch fix/workspaces-acceptance-gaps); see the Evidence line.

### UC-WSP-G04 Change a pending invitation's role
- Capability: G Invitation create
- Actors and permission: Owner, Admin (`InviteMember`)
- Preconditions: Invitation Pending.
- Input: `invitationId`, `newRole` (member or guest).
- Output: New role stored.
- Main flow: 1. `PATCH /api/v1/workspaces/{id}/invitations/{invitationId}/role`. 2. Domain changes the role.
- No-op / rejection / concurrency / retry: Admin or raise to admin: `Workspaces_Invitation_CannotInviteAsAdmin` (an old Admin invitation may be lowered, never kept or raised). Not pending: refused. Expected version required (LD-07): the body carries the invitation's `version`, and a version that moved (resent, changed, answered) answers 412 and changes nothing.
- UI states: Role menu in the pending row, conflict, pending.
- Rules: [workspaces](../../product/workspaces.md) section 22.1; WGREQ027, WGREQ031.
- Acceptance: Given a Pending admin invitation, When changed to member, Then it shows member.
- Evidence: BE `PATCH .../invitations/{invitationId}/role`, `ChangeInvitationRoleCommand` is an `IExpectedVersionRequest` mapped to `WorkspaceInvitation` in `ExpectedVersionTargetMap` · FE `invitations.service.ts` and the Members surface send the version (an invitation with no known version is never written); mock backend checks it · Design `Final-Members` · Tests `WorkspaceInvitationRulesTests`; tests that build the command without a version need updating · Real e2e workspace-invitations-manage.real.e2e.spec.ts · Independent acceptance 2026-11-02 by a fifth agent that did not build it, on develop `e368b0dc2` (branch accept/batch-7). Line 1: Domain filter `Invitation|PasswordReset` 102 passed (`WorkspaceInvitationTests`, `WorkspaceInvitationVersionTests`: change role succeeds and bumps the version, the same role is a no-op, a not-pending invitation and the Owner role are refused with nothing changed, a resend inside the cool-down is refused with nothing changed, revoke and expire bump the version); Application filter `Invitation|ForgotPassword|ResetPassword` 110 passed; API `InvitationEndpoint` and `PublicAuthEndpoint` 34 passed (a made-up invitation or workspace answers 404); Infrastructure `OtpService|OneTimeToken` 25 passed; Integration filter `Invitation|CredentialScope|SecurityActivityHttpFlow|ForgotPassword|PasswordReset` 111 passed in one Testcontainers run (accept racing revoke, escalation, token flow, invitation RLS scope, credential scope). Frontend: `packages/features/auth` web tests 55 passed; the screens call the typed client (`invitations.service.ts`, `auth-flow.service.ts`). Line 2 and line 3, undrawn Pending invitations list (the canvas draws only the department Members tab): `workspace-acceptance.mock.e2e.spec.ts` ran 78 of 78 with the invitations list and the six join pages at 1440, 1024 and 390 px in light and dark (no sideways scroll, 44 px targets at 390 px, axe no serious or critical violation); the screenshots `invitations` at 390 light, 1440 dark and 1024 light were opened and read: at 390 px the address has its own line above the Role select and the two 44 px icon buttons, no clipping at any width. A throwaway keyboard probe (not kept) sent an invitation, opened the role select and chose Guest, resent and revoked with the confirmation, all by keyboard: passed. Real stack (fresh `notrelix-fe-real-e2e` stack, one run): `workspace-invitations-manage` 2, `workspace-invitation-link` 1, `workspace-cross-scope` 1, `password-reset` 1 and `signed-out-routes` 5: 10 of 10 passed.
- State: DONE — accepted by independent acceptance 2026-11-02 at develop `e368b0dc2` (branch accept/batch-7); see the Evidence line.

### UC-WSP-G05 Resend an invitation
- Capability: G Invitation create
- Actors and permission: Owner, Admin (`InviteMember`)
- Preconditions: Invitation Pending or Expired.
- Input: `invitationId`.
- Output: A new token and expiry; the old link stops working; one email.
- Main flow: 1. `POST .../invitations/{invitationId}/resend`. 2. Domain replaces the token. 3. Outbox sends.
- No-op / rejection / concurrency / retry: Accepted, Revoked, Declined: `Workspaces_Invitation_CannotResendNonPendingExpired`. A resend is held for a minute after the invitation was last sent (`Workspaces_Invitation_ResendTooSoon`, LD-04), so rapid repeats no longer replace the token and send an email each time. The last send is read from the invitation's last change, so a role change also holds a resend for a minute.
- UI states: Row action, toast, pending.
- Rules: [workspaces](../../product/workspaces.md) sections 25, 26; WGREQ028, WGREQ030.
- Acceptance: Given a Pending invitation, When resent, Then the old token fails and the new one works.
- Evidence: BE `POST .../invitations/{invitationId}/resend`, `ResendInvitationCommandHandler`, `WorkspaceInvitation.Resend` with `ResendCooldown`; the refusal shows as the producer's rule message in the Members toast · FE `invitations.service.ts` · Design `Final-Members` · Tests `WorkspaceInvitationTests`, `InvitationTokenFlowHttpTests`; tests that resend right after creating now meet the cool-down and need a later time; the rule-code snapshot has the new code · Real e2e workspace-invitations-manage.real.e2e.spec.ts · Independent acceptance 2026-11-02 by a fifth agent that did not build it, on develop `e368b0dc2` (branch accept/batch-7). Line 1: Domain filter `Invitation|PasswordReset` 102 passed (`WorkspaceInvitationTests`, `WorkspaceInvitationVersionTests`: change role succeeds and bumps the version, the same role is a no-op, a not-pending invitation and the Owner role are refused with nothing changed, a resend inside the cool-down is refused with nothing changed, revoke and expire bump the version); Application filter `Invitation|ForgotPassword|ResetPassword` 110 passed; API `InvitationEndpoint` and `PublicAuthEndpoint` 34 passed (a made-up invitation or workspace answers 404); Infrastructure `OtpService|OneTimeToken` 25 passed; Integration filter `Invitation|CredentialScope|SecurityActivityHttpFlow|ForgotPassword|PasswordReset` 111 passed in one Testcontainers run (accept racing revoke, escalation, token flow, invitation RLS scope, credential scope). Frontend: `packages/features/auth` web tests 55 passed; the screens call the typed client (`invitations.service.ts`, `auth-flow.service.ts`). Line 2 and line 3, undrawn Pending invitations list (the canvas draws only the department Members tab): `workspace-acceptance.mock.e2e.spec.ts` ran 78 of 78 with the invitations list and the six join pages at 1440, 1024 and 390 px in light and dark (no sideways scroll, 44 px targets at 390 px, axe no serious or critical violation); the screenshots `invitations` at 390 light, 1440 dark and 1024 light were opened and read: at 390 px the address has its own line above the Role select and the two 44 px icon buttons, no clipping at any width. A throwaway keyboard probe (not kept) sent an invitation, opened the role select and chose Guest, resent and revoked with the confirmation, all by keyboard: passed. Real stack (fresh `notrelix-fe-real-e2e` stack, one run): `workspace-invitations-manage` 2, `workspace-invitation-link` 1, `workspace-cross-scope` 1, `password-reset` 1 and `signed-out-routes` 5: 10 of 10 passed.
- State: DONE — accepted by independent acceptance 2026-11-02 at develop `e368b0dc2` (branch accept/batch-7); see the Evidence line.

### UC-WSP-G06 Revoke an invitation
- Capability: G Invitation create
- Actors and permission: Owner, Admin (`InviteMember`)
- Preconditions: Invitation exists.
- Input: `invitationId`.
- Output: Pending becomes Revoked (Expired when already past expiry); it can no longer be accepted or resent.
- Main flow: 1. `DELETE .../invitations/{invitationId}`. 2. Handler revokes or expires.
- No-op / rejection / concurrency / retry: Accepted: cannot be revoked (remove the member instead). An accept and a revoke at the same moment have one winner (WGREQ031). No expected version; the status check decides. Real stack: a revoke of an accepted invitation answers 204 and changes nothing (status and version stay, the member stays Active).
- UI states: Row action with confirmation, success.
- Rules: [workspaces](../../product/workspaces.md) sections 14, 66; WSP-006, WSP-019; WGREQ029, WGREQ031, WGREQ146.
- Acceptance: Given a Pending invitation, When revoked, Then accepting its token gives the generic invalid answer.
- Evidence: BE `DELETE .../invitations/{invitationId}`, `CancelInvitationCommandHandler` · FE `invitations.service.ts` · Design `Final-Members` · Tests `WorkspaceInvitationConcurrencyEvidenceTests`, `AcceptInvitationTransactionEvidenceTests` · Real e2e workspace-invitations-manage.real.e2e.spec.ts · Independent acceptance 2026-11-02 by a fifth agent that did not build it, on develop `e368b0dc2` (branch accept/batch-7). Line 1: Domain filter `Invitation|PasswordReset` 102 passed (`WorkspaceInvitationTests`, `WorkspaceInvitationVersionTests`: change role succeeds and bumps the version, the same role is a no-op, a not-pending invitation and the Owner role are refused with nothing changed, a resend inside the cool-down is refused with nothing changed, revoke and expire bump the version); Application filter `Invitation|ForgotPassword|ResetPassword` 110 passed; API `InvitationEndpoint` and `PublicAuthEndpoint` 34 passed (a made-up invitation or workspace answers 404); Infrastructure `OtpService|OneTimeToken` 25 passed; Integration filter `Invitation|CredentialScope|SecurityActivityHttpFlow|ForgotPassword|PasswordReset` 111 passed in one Testcontainers run (accept racing revoke, escalation, token flow, invitation RLS scope, credential scope). Frontend: `packages/features/auth` web tests 55 passed; the screens call the typed client (`invitations.service.ts`, `auth-flow.service.ts`). Line 2 and line 3, undrawn Pending invitations list (the canvas draws only the department Members tab): `workspace-acceptance.mock.e2e.spec.ts` ran 78 of 78 with the invitations list and the six join pages at 1440, 1024 and 390 px in light and dark (no sideways scroll, 44 px targets at 390 px, axe no serious or critical violation); the screenshots `invitations` at 390 light, 1440 dark and 1024 light were opened and read: at 390 px the address has its own line above the Role select and the two 44 px icon buttons, no clipping at any width. A throwaway keyboard probe (not kept) sent an invitation, opened the role select and chose Guest, resent and revoked with the confirmation, all by keyboard: passed. Real stack (fresh `notrelix-fe-real-e2e` stack, one run): `workspace-invitations-manage` 2, `workspace-invitation-link` 1, `workspace-cross-scope` 1, `password-reset` 1 and `signed-out-routes` 5: 10 of 10 passed.
- State: DONE — accepted by independent acceptance 2026-11-02 at develop `e368b0dc2` (branch accept/batch-7); see the Evidence line.

### UC-WSP-G07 Invite a guest to one Board or Page
- Capability: G Invitation create
- Actors and permission: Holder of `ManageBoardPermission` or `ManagePagePermission` on the resource; level not above the inviter's own
- Preconditions: Resource exists; inviter can manage its permissions.
- Input: `resourceType` (board, page), `resourceId`, `email`, `level` (up to Editor).
- Output: A Pending resource invitation; on acceptance a guest membership plus one entry on that resource. Never a member, never Space or Team entries.
- Main flow: 1. `POST /api/v1/resources/{resourceType}/{resourceId}/invitations`. 2. Pipeline checks the permission on the resource. 3. Invitation stored with the resource grant. 4. Email `resource-invitation`.
- No-op / rejection / concurrency / retry: Level above the inviter's or above Editor: refused at creation. Resource gone at acceptance: fails closed. The workspace setting `AllowGuestInvites` must block it (not verified).
- UI states: Share dialog: email, level, validation error, permission denied, pending list (Work Management UI).
- Rules: [workspaces](../../product/workspaces.md) section 22.1; WSP-026; GOV-030; owner decision in Part 4; see [governance](governance.md).
- Acceptance: Given a Board manager, When they invite `g@x.com` at Viewer, Then on acceptance `g` is a Guest with one entry on that Board only.
- Evidence: BE `POST /api/v1/resources/{resourceType}/{resourceId}/invitations`, `InviteGuestToResourceCommandHandler` · FE none: the Board Share dialog (`board-share-dialog.tsx`, `use-board-sharing.ts`) adds existing people by user id and has no email invitation · Design `Final-Share`, `Final-M-Share` · Tests `ResourceInvitationIssuerIntegrationTests`, `GuestAndEntryInvitationIntegrationTests`, `BoardSharingIntegrationTests` · Real e2e none (`board-sharing.real.e2e.spec.ts` adds a workspace member, not a guest invitation)
- State: IMPLEMENTING — Email invitation in the Share dialog (owner decision); real e2e; `AllowGuestInvites` proof.

### UC-WSP-G08 Resend or revoke a resource invitation
- Capability: G Invitation create
- Actors and permission: The issuer with the permission on that resource; Owner and Admin through G05 and G06
- Preconditions: The caller issued the invitation for exactly this resource.
- Input: `resourceType`, `resourceId`, `invitationId`.
- Output: Resend replaces the token; revoke ends it.
- Main flow: 1. `POST .../invitations/{invitationId}/resend` or `DELETE .../invitations/{invitationId}`. 2. Permission on the resource. 3. Domain acts.
- No-op / rejection / concurrency / retry: Another member's invitation or another resource's: NotFound. An issuer who lost the permission: denied. A revoked invitation cannot be accepted or resent.
- UI states: Row actions in the Share dialog.
- Rules: [workspaces](../../product/workspaces.md) section 22.1; WGREQ029, WGREQ030.
- Acceptance: Given the issuer, When they revoke, Then the token fails. Given another member, Then NotFound.
- Evidence: BE `POST /api/v1/resources/{resourceType}/{resourceId}/invitations/{invitationId}/resend`, `DELETE .../{invitationId}`, `ResendResourceInvitationCommandHandler`, `RevokeResourceInvitationCommandHandler` · FE none · Design `Final-Share` · Tests `ResourceInvitationIssuerIntegrationTests` · Real e2e none
- State: IMPLEMENTING — No frontend caller.

### UC-WSP-G09 Invite many people at once (enterprise baseline)
- Capability: G Invitation create
- Actors and permission: Owner, Admin (`InviteMember`)
- Preconditions: As G01.
- Input: Up to 50 emails, one role, optional entries.
- Output: One result row per email: sent, already member, already pending, invalid.
- Main flow: 1. Client submits the list. 2. Each address is handled as G01 on its own. 3. The answer lists each outcome.
- No-op / rejection / concurrency / retry: No all-or-nothing; a retry of the whole list skips pending ones. Limit 50 per request.
- UI states: Paste-list field, per-row result, partial success.
- Rules: [workspaces](../../product/workspaces.md) section 23; Part 4 LD-13.
- Acceptance: Given three valid and one invalid address, Then three invitations and one row error.
- Evidence: BE none · FE onboarding invite step takes several emails but calls G01 per address · Design `Final-Onboarding` · Tests none · Real e2e none
- State: DEFINED — Batch contract; result UI.

## H. Invitation accept / reject / revoke

### UC-WSP-H01 Preview an invitation from its link
- Capability: H Invitation accept / reject / revoke
- Actors and permission: Anyone holding the token (anonymous)
- Preconditions: A link with a raw token.
- Input: `token` in the body.
- Output: Workspace name, role, inviter display name (generic label if none), expiry, state: valid, expired, revoked, accepted, declined, invalid. Never the inviter's email.
- Main flow: 1. `POST /api/v1/invitations/preview`. 2. The token is hashed and looked up. 3. State is computed from status and expiry.
- No-op / rejection / concurrency / retry: An unknown token gets the same `invalid` answer as a wrong one (no enumeration). Rate-limited. The token is never logged.
- UI states: Loading, valid, expired, revoked or used, invalid pages.
- Rules: [workspaces](../../product/workspaces.md) sections 25, 26, 48.4 'Join page'; WGREQ032, WGREQ033.
- Acceptance: Given a valid token, Then name and role show. Given a random token, Then `invalid`.
- Evidence: BE `POST /api/v1/invitations/preview`, `GetInvitationByTokenQueryHandler` · FE `invite/$token.tsx`, `invite/index.tsx` (the e-mail link, token in the fragment), `use-invitation-by-token.ts`, `onboarding-join-surface.tsx` · Design `Final-Onboarding`, `Final-Auth` · Tests `GetInvitationByTokenQueryHandlerTests`, `InvitationTokenFlowHttpTests`, `onboarding.mock.e2e.spec.ts` · Real e2e workspace-invitation-link.real.e2e.spec.ts · Independent acceptance 2026-11-02 by a fifth agent that did not build it, on develop `e368b0dc2` (branch accept/batch-7). Line 1: Domain filter `Invitation|PasswordReset` 102 passed (`WorkspaceInvitationTests`, `WorkspaceInvitationVersionTests`: change role succeeds and bumps the version, the same role is a no-op, a not-pending invitation and the Owner role are refused with nothing changed, a resend inside the cool-down is refused with nothing changed, revoke and expire bump the version); Application filter `Invitation|ForgotPassword|ResetPassword` 110 passed; API `InvitationEndpoint` and `PublicAuthEndpoint` 34 passed (a made-up invitation or workspace answers 404); Infrastructure `OtpService|OneTimeToken` 25 passed; Integration filter `Invitation|CredentialScope|SecurityActivityHttpFlow|ForgotPassword|PasswordReset` 111 passed in one Testcontainers run (accept racing revoke, escalation, token flow, invitation RLS scope, credential scope). Frontend: `packages/features/auth` web tests 55 passed. Line 3, drawn states: `design-fidelity-invitation.mock.e2e.spec.ts` 6 of 6 passed (Final-Auth `loi-moi` and `het-han` at 1440 px, Final-M-Auth `loi-moi` at 390 px, light and dark); the artboards were rendered again with `render.py` and a Chromium box dump and the spec numbers equal the artboard boxes (for example `loi-moi` avatar x 836.8 y 55.1 32 by 32, section 'Bạn sẽ thấy gì' y 248.2 h 96.8, buttons 298.8 and 91.2 wide at y 677.2; `het-han` h1 y 387.8 h 35, back button y 508.3 h 44; Final-M-Auth card y 282.8 h 244.1, buttons 170 by 50), the only offsets being the accepted 1.98 px of the 24 px title token; the artboards' light and dark colour values appear in the renders and match the spec palette except the accepted dark link token. `design-fidelity-auth` 16 passed. Undrawn states (signed-in join, used, made-up link, workspace join, and 1024 px): `workspace-acceptance.mock.e2e.spec.ts` 36 join-page cases at 1440, 1024 and 390 px in both themes passed (no overflow, 44 px targets at 390 px, axe clean); the screenshots join-new 390 dark, join-existing 1024 light, join-used 390 light, join-missing 1440 dark and join-workspace 390 light were opened and read. Note: at 1024 by 768 the shared brand panel of every Auth page lets the last line of its headline touch the top of the mark; it is the common Auth shell already accepted for the sign-in pages, not a part of this use case. Keyboard: a throwaway probe (not kept) finished the join by keyboard at 1440 px (name, password, Enter) and at 390 px (Accept opens the form in place with focus on the name, then password and Enter): passed. Real stack (fresh `notrelix-fe-real-e2e` stack, one run): `workspace-invitations-manage` 2, `workspace-invitation-link` 1, `workspace-cross-scope` 1, `password-reset` 1 and `signed-out-routes` 5: 10 of 10 passed.
- State: DONE — accepted by independent acceptance 2026-11-02 at develop `e368b0dc2` (branch accept/batch-7); see the Evidence line.

### UC-WSP-H02 Accept an invitation while signed in
- Capability: H Invitation accept / reject / revoke
- Actors and permission: The invited person (confirmed email equal to the invited email)
- Preconditions: Signed in; invitation Pending and unexpired.
- Input: `token`.
- Output: Active membership (or reused), entries and resource grant applied in one transaction; invitation Accepted; `WorkspaceMemberAdded`.
- Main flow: 1. `POST /api/v1/invitations/accept`. 2. Email and status checks. 3. Billing seat gate. 4. One transaction applies everything.
- No-op / rejection / concurrency / retry: Different email: refused. A repeat creates no second membership (WSP-009). Revoked meanwhile: one winner. Removed or suspended member with entries: cannot accept. The seat gate is a required dependency (LD-08): a new membership asks Billing first and a refusal fails the acceptance with the plan-limit message; an acceptance that cannot ask Billing does not admit. An existing membership reuses its row: a Removed member who accepts a plain invitation (no entries, no resource entry) returns through `WorkspaceMember.Rejoin` with the invited role, after the same seat check as direct add, refused with `Workspaces_Member_SeatUnavailable` when no seat is free; a Suspended member is refused with "Your membership in this workspace is not active" and stays suspended (an invitation never overrides an admin's suspension); one with entries or a resource entry is refused if Removed or Suspended (lead decision, product doc section 22.1).
- UI states: Join page: pending, success (go to the workspace), error states.
- Rules: [workspaces](../../product/workspaces.md) sections 14, 24, 75; WSP-006, WSP-009, WSP-019; WGREQ030, WGREQ031, WGREQ146; NRX-010.
- Acceptance: Given the right person, When they accept twice, Then one membership exists. Given another email, Then refused and no change.
- Evidence: BE `POST /api/v1/invitations/accept`, `AcceptInvitationCommandHandler`, `InvitationAcceptanceService` (now requires `IBillingSeatGate`; tests that build either without a gate need the argument) · FE `auth-flow.service.ts` (auth feature), `onboarding-join-surface.tsx` · Design `Final-Onboarding` · Tests `AcceptInvitationTransactionEvidenceTests`, `WorkspaceInvitationConcurrencyEvidenceTests`, `WorkspaceCredentialAndInvitationEscalationTests`, `InvitationTokenFlowHttpTests`, `onboarding.mock.e2e.spec.ts`; run: Application `AcceptInvitationCommandHandlerTests` (15), Integration `AcceptInvitationTransactionEvidenceTests` (4), `WorkspaceInvitationConcurrencyEvidenceTests` (3), `WorkspaceCredentialAndInvitationEscalationTests` (6), API `InvitationEndpointTests` (10) · Real e2e `frontend/e2e/real/workspace-invitations-answer.real.e2e.spec.ts` (a removed member is invited again and accepts from the list: back in the workspace with a membership; another signed-in person is refused) · Lane run 2026-11-03: Application `AcceptInvitationByIdTests` (the accept-by-id handler) and a real journey for `POST /invitations/accept` with a token while signed in: `workspace-invitation-accept-signed-in.real.e2e.spec.ts` passed on the real stack (project notrelix-fe-real-e2e, 2026-11-03, branch fix/workspaces-acceptance-gaps) (another signed-in person with the token gets 400 and a made-up token 404, nothing changes; the invitee opens the link, presses Accept, lands in the workspace as Member, the invitation is Accepted, the roster holds two; a second use is refused).
- State: DONE — lines 1–4 proven in the lane pull request (2026-11-03, branch fix/workspaces-acceptance-gaps); see the Evidence line.

### UC-WSP-H03 Accept an invitation and register in one step
- Capability: H Invitation accept / reject / revoke
- Actors and permission: A new person holding the token (anonymous)
- Preconditions: No account yet for the invited email.
- Input: `token`, `name`, `password`.
- Output: A User (email proven by the token), a membership, the invitation Accepted.
- Main flow: 1. `POST /api/v1/invitations/accept-with-registration`. 2. Identity creates the user. 3. Workspaces accepts as H02. 4. A session starts.
- No-op / rejection / concurrency / retry: Existing account for the email: sign in and use H02. Password policy errors come from Identity. A partial failure rolls back both. Rate-limited.
- UI states: Join form: name, password, validation, pending.
- Rules: [workspaces](../../product/workspaces.md) section 75; see [identity](identity.md); WGREQ033.
- Acceptance: Given a valid token and a strong password, Then user and membership exist and the person is signed in.
- Evidence: BE `POST /api/v1/invitations/accept-with-registration`, `AcceptInvitationWithRegistrationCommandHandler` · FE `auth-flow.service.ts`, join surface · Design `Final-Onboarding` · Tests `InvitationTokenFlowHttpTests`, `WorkspaceCredentialAndInvitationEscalationTests`, `onboarding.mock.e2e.spec.ts` · Real e2e workspace-invitation-link.real.e2e.spec.ts · Independent acceptance 2026-11-02 by a fifth agent that did not build it, on develop `e368b0dc2` (branch accept/batch-7). Line 1: Domain filter `Invitation|PasswordReset` 102 passed (`WorkspaceInvitationTests`, `WorkspaceInvitationVersionTests`: change role succeeds and bumps the version, the same role is a no-op, a not-pending invitation and the Owner role are refused with nothing changed, a resend inside the cool-down is refused with nothing changed, revoke and expire bump the version); Application filter `Invitation|ForgotPassword|ResetPassword` 110 passed; API `InvitationEndpoint` and `PublicAuthEndpoint` 34 passed (a made-up invitation or workspace answers 404); Infrastructure `OtpService|OneTimeToken` 25 passed; Integration filter `Invitation|CredentialScope|SecurityActivityHttpFlow|ForgotPassword|PasswordReset` 111 passed in one Testcontainers run (accept racing revoke, escalation, token flow, invitation RLS scope, credential scope). Frontend: `packages/features/auth` web tests 55 passed. Line 3, drawn states: `design-fidelity-invitation.mock.e2e.spec.ts` 6 of 6 passed (Final-Auth `loi-moi` and `het-han` at 1440 px, Final-M-Auth `loi-moi` at 390 px, light and dark); the artboards were rendered again with `render.py` and a Chromium box dump and the spec numbers equal the artboard boxes (for example `loi-moi` avatar x 836.8 y 55.1 32 by 32, section 'Bạn sẽ thấy gì' y 248.2 h 96.8, buttons 298.8 and 91.2 wide at y 677.2; `het-han` h1 y 387.8 h 35, back button y 508.3 h 44; Final-M-Auth card y 282.8 h 244.1, buttons 170 by 50), the only offsets being the accepted 1.98 px of the 24 px title token; the artboards' light and dark colour values appear in the renders and match the spec palette except the accepted dark link token. `design-fidelity-auth` 16 passed. Undrawn states (signed-in join, used, made-up link, workspace join, and 1024 px): `workspace-acceptance.mock.e2e.spec.ts` 36 join-page cases at 1440, 1024 and 390 px in both themes passed (no overflow, 44 px targets at 390 px, axe clean); the screenshots join-new 390 dark, join-existing 1024 light, join-used 390 light, join-missing 1440 dark and join-workspace 390 light were opened and read. Note: at 1024 by 768 the shared brand panel of every Auth page lets the last line of its headline touch the top of the mark; it is the common Auth shell already accepted for the sign-in pages, not a part of this use case. Keyboard: a throwaway probe (not kept) finished the join by keyboard at 1440 px (name, password, Enter) and at 390 px (Accept opens the form in place with focus on the name, then password and Enter): passed. Real stack (fresh `notrelix-fe-real-e2e` stack, one run): `workspace-invitations-manage` 2, `workspace-invitation-link` 1, `workspace-cross-scope` 1, `password-reset` 1 and `signed-out-routes` 5: 10 of 10 passed.
- State: DONE — accepted by independent acceptance 2026-11-02 at develop `e368b0dc2` (branch accept/batch-7); see the Evidence line.

### UC-WSP-H04 Decline an invitation from its link
- Capability: H Invitation accept / reject / revoke
- Actors and permission: Anyone holding the token (anonymous)
- Preconditions: A Pending token.
- Input: `token`.
- Output: Invitation Declined; it cannot be accepted.
- Main flow: 1. `POST /api/v1/invitations/decline`. 2. Domain declines.
- No-op / rejection / concurrency / retry: A repeat press answers the first answer (success, no second event). An unknown or malformed token answers a neutral NotFound. An accepted, revoked or expired invitation cannot be declined (a business-rule refusal).
- UI states: Decline link, declining (pending), done page (the confirmation; no confirm dialog is drawn on `Final-Auth`), refused or gone, unreachable.
- Rules: [workspaces](../../product/workspaces.md) section 66; WGREQ029.
- Acceptance: Given a Pending token, When declined, Then accept fails.
- Evidence: BE `POST /api/v1/invitations/decline`, `DeclineInvitationByTokenCommandHandler` · FE `invite-flow.tsx`, `auth-invite-surface.tsx` (decline control), `auth-flow.service.ts` · Design `Final-Onboarding` · Tests `DeclineInvitationByTokenTests`, `InvitationTokenFlowHttpTests` · Real e2e none
- State: DONE — Proven by tests that ran in this PR. Application: `DeclineInvitationByTokenTests` (8 pass: pending declined with no actor, unknown token NotFound with no scope adopted, scope adopted without a user, second press same success without a second event, accepted refused, revoked refused (`RevokedInvitation_CannotBeDeclined_AndStaysRevoked`), expired refused), `AcceptInvitationWithRegistrationTests.DeclinedInvitation_CreatesNoAccount` (10 pass: accept fails after a decline). Integration `InvitationTokenFlowHttpTests` (`Decline_ByToken_Is204Twice_AndTheInvitationStaysDeclined`, `Decline_AnAcceptedInvitation_IsRejected_AndUnknownTokenIsNotFound`) in the Auth shard (all Auth and Workspaces tests pass, 545). Frontend `auth-flows.component.test.tsx` (40 pass): declines with the token, the declining state keeps both buttons waiting then the done page names who to ask and offers the way back, a 404 reads as gone, a 400 re-reads the invitation and shows what it became, a network failure keeps the invitation open for a retry; `auth-form-surfaces.interaction.component.test.tsx` (the decline control). Decisions (lead, 2026-11-19): a repeat gets the first answer and an unknown token a neutral NotFound; the done page is the confirmation, no confirm dialog because none is drawn.

### UC-WSP-H05 Decline an invitation I see in my account
- Capability: H Invitation accept / reject / revoke
- Actors and permission: The invited person, signed in
- Preconditions: A Pending invitation for my confirmed email.
- Input: `invitationId`.
- Output: Invitation Declined.
- Main flow: 1. `POST /api/v1/invitations/{invitationId}/decline`. 2. The handler reads the invitation through the invitee policy (pending, addressed to the confirmed email), checks the email again, then declines inside the invitation token scope. 3. Domain declines.
- No-op / rejection / concurrency / retry: An invitation for another email, an unconfirmed email, an unknown id and a repeat: the same NotFound. An invitation that ran out meanwhile: "This invitation has expired."
- UI states: Decline in the invitations flyout (the buttons of the invitation being answered wait), gone invitation (the list is read again and says so), error.
- Rules: [workspaces](../../product/workspaces.md) section 14.
- Acceptance: Given my invitation, When I decline, Then it leaves my list.
- Evidence: BE `POST /api/v1/invitations/{invitationId}/decline`, `DeclineInvitationCommandHandler`, `Invitations/Services/InviteeInvitations.cs`; RLS `Infrastructure/Data/Rls/RlsSqlScripts/025_invitee_invitations.sql` · FE `packages/features/workspace/src/web/components/pending-invitations-menu.tsx`, `workspace-pending-invitations-surface.tsx`, `hooks/mutations/use-decline-invitation.ts`, mounted in the system rail by `apps/web/src/shell/workspace/workspace-shell.tsx`; mock `packages/dev/mock-backend/src/contexts/workspace/workspace.handlers.ts` · Design none (no artboard draws the invitee flyout: it reuses the rail entry and the account popover tokens) · Tests run: Application `DeclineInvitationTests` (2), `InviteeInvitationsOpenTests` (2), API `NewInvitationEndpointTests` (7), Integration `InvitationScopeTests` (23, the RLS policy with two people and two tenants) · Real e2e `frontend/e2e/real/workspace-invitations-answer.real.e2e.spec.ts` (the invitee declines in the flyout: it leaves the list and grants nothing; another person is refused with a 4xx)
- State: DONE — accepted 2026-11-02: independent acceptance at `4131c937a` (branch accept/batch-11, #289) found no gap in lines 1–3, CI was green on that commit for the Integration classes, and the real specs ran in the build run of #286 (lines 1–4 PASS). Builder's account: Line 1: Application `DeclineInvitationTests` 6 (new: an unconfirmed address gets NotFound and the invitation stays pending; another address and an unknown id get the same NotFound; a repeat gets NotFound and bumps no version; an invitation that ran out is refused with 'This invitation has expired.' and stays pending), `InviteeInvitationsOpenTests` 2; Integration `InvitationScopeTests` (RLS policy with two people and two tenants) in the 98 of H02. Line 2: flyout states through the generated client: loading, error with retry, empty, list, a refusal told above the list, an answered invitation gone, buttons wait while answering (`pending-invitations-menu.interaction.component` 3 and `workspace-ui-surfaces.interaction` 2); keyboard: `workspace-acceptance` 'invitee flyout: the keyboard opens it, reaches Accept and Decline, and Escape closes it' passes; axe in the flyout audits. Line 3: no artboard draws the flyout (it reuses the rail entry and account popover tokens), so the undrawn rule applies: the flyout with two waiting and then empty at 1440, 1024 and 390 px in light and dark (`workspace-acceptance`, 6 cases; the whole mock run of `workspace-acceptance` 93, `design-fidelity-invitation` 6 and `workspace-spaces.mock` 1 passed, 100 of 100): it was 320 px wide and ran off a 390 px screen and its buttons were 32 px high; both fixed (it now stays inside the screen beside the 76 px rail, buttons 44 px on a phone); no overflow, 44 px targets, axe clean; screenshots opened and read: list 1440 light, 1024 light and 390 light and dark, empty 390 light, 1440 dark and 1024 dark. Line 4: real stack, `workspace-invitations-answer.real.e2e.spec.ts` 1 passed (the invitee declines in the flyout, it leaves the list and grants nothing; another person is refused) and `workspace-invitations-refusals.real.e2e.spec.ts` (an unconfirmed address is refused like a made-up id; a repeat decline answers 404).

### UC-WSP-H06 See my pending invitations
- Capability: H Invitation accept / reject / revoke
- Actors and permission: Any signed-in person
- Preconditions: Confirmed email.
- Input: none.
- Output: Pending, unexpired invitations addressed to my email: workspace, role, expiry.
- Main flow: 1. `GET /api/v1/invitations/pending`. 2. The handler reads the pending, unexpired invitations addressed to my email through the invitee policy; for each, the workspace name and the inviter's display name are read inside that invitation's token scope. Invitations to an archived or deleted workspace are left out.
- No-op / rejection / concurrency / retry: Only my own email; an unconfirmed email gets none (the policy requires a confirmed address).
- UI states: The "Invites" entry of the system rail with a count badge, opening a flyout: loading, empty, error with retry, the list (Accept and Decline per invitation), a refusal told above the list.
- Rules: [workspaces](../../product/workspaces.md) sections 14, 75.
- Acceptance: Given two pending invitations, Then two rows.
- Evidence: BE `GET /api/v1/invitations/pending`, `GetUserPendingInvitationsQueryHandler`, `Invitations/Services/InviteeInvitations.cs`; RLS `Infrastructure/Data/Rls/RlsSqlScripts/025_invitee_invitations.sql` · FE `pending-invitations-menu.tsx`, `workspace-pending-invitations-surface.tsx`, `pending-invitations.copy.ts` (vi, en), `use-pending-invitations.ts`, rail slot in `shell/notrelix-system-rail-surface.tsx` · Design none for the flyout (the rail entry follows Final-Space) · Tests `InvitationEndpointTests`, run: Application `GetUserPendingInvitationsQueryHandlerTests` (8), API `InvitationEndpointTests` (10), Integration `InvitationScopeTests` (23), FE `workspace-ui-surfaces.interaction.component.test.tsx` (2), `use-accept-invitation.component.test.tsx` (2) · Real e2e `frontend/e2e/real/workspace-invitations-answer.real.e2e.spec.ts` (the invitee sees the one pending invitation in the rail flyout, another person sees none, accepting lands in the workspace and empties the list) · Lane run 2026-11-03: Application `GetUserPendingInvitationsQueryHandlerTests` now prove that an expired invitation and one to an archived or a deleted workspace are left out. Line 4: real stack (project notrelix-fe-real-e2e, 2026-11-03, branch fix/workspaces-acceptance-gaps), `workspace-invitations-answer.real.e2e.spec.ts` passed.
- State: DONE — lines 1–4 proven in the lane pull request (2026-11-03, branch fix/workspaces-acceptance-gaps); see the Evidence line.
- Earlier state: BLOCKED — Owner decision on the RLS and tenant model for an invitee-side read (items 1 and 3 below); nothing built beyond removing the token hash (2). (1) The pending list and decline-by-id (H05) are global requests that read workspace-scoped invitations without lifting the tenant filter, so in the real stack they likely return nothing or NotFound; this needs a real-stack check and, if confirmed, a read keyed to the caller's confirmed email, which may also need a database policy (schema change). (2) `UserPendingInvitationDto.Token` returned the stored token hash, which is credential material and could not be used to accept; it is removed (lead decision, contract regenerated). Accepting from the list needs an accept-by-id command with the same email and seat checks; it waits for item 1. (3) `PendingInvitationsMenu` is exported but mounted nowhere in the web app, is English-only and has no artboard-matched design. Decision needed from the lead on the invitee-side flow before building.

## I. Workspace selection / switch integration

### UC-WSP-I01 Switch the active workspace
- Capability: I Workspace selection / switch integration
- Actors and permission: Any member of both workspaces
- Preconditions: Signed in; the target is a workspace the person belongs to.
- Input: Target workspace id (from the rail).
- Output: The route changes to the target; old requests and subscriptions stop; the new scope loads fresh.
- Main flow: 1. User picks a workspace. 2. Router goes to `/workspaces/{id}`. 3. Query keys carry the workspace id. 4. The realtime subscription is disposed and reopened. 5. Late old responses are dropped.
- No-op / rejection / concurrency / retry: A late response or event of the old workspace must not patch the new scope (WSP-015). The selection is a hint; the server checks membership on every call.
- UI states: Rail open or closed, loading of the new scope, stale-scope boundary, 320 px sheet.
- Rules: [workspaces](../../product/workspaces.md) sections 50 to 53, 69; WSP-015, WSP-016; WX-SHELL-001; NRX-014.
- Acceptance: Given a Board open in W2, When the user switches to W3, Then the URL is W3's and no W2 Board shows.
- Evidence: BE none (server checks each call) · FE `workspace-scope-boundary.tsx`, `workspace-scope.ts`, system rail, `realtime-adapter.ts` · Design `BC-Shell-1440`, `BC-Shell-1280-768`, `BC-Shell-320` · Tests `workspace-shell.mock.e2e.spec.ts`, `design-fidelity-shell.mock.e2e.spec.ts` · Real e2e `workspace-isolation.real.e2e.spec.ts`
- State: DONE 2026-11-15 (branch wsp/switch-workspace-0-1). Lead decision 2026-11-14: shell follows Final-Board/Final-M. Where the state boards disagree with the shell (32 px against 34 px header mark, 55 px against 61 px header, the 320 back button and 44 px tab strip, the 768 icon rail) the Final boards win, and the state boards are measured only where they do not conflict. Line 1: the out-of-order proof is `LIFE-027` in `workspace-isolation.integration.test.ts` (a read of W-A that settles after the switch to W-B leaves no A data), with `LIFE-021`/`LIFE-025` for the cache clear and `account-isolation.integration.test.ts`. Line 2: the realtime adapter drops an event of another workspace, `LIFE-022` proves the connection is closed with the workspace-switch reason on every change and `LIFE-026` that a disposed lifecycle does nothing. Line 4: `workspace-isolation.real.e2e.spec.ts` on the real stack, 1 of 1 passed 2026-11-15 (project notrelix-fe-real-e2e on web port 5391 because the paused dev web container holds 5173; the sign-in helper now accepts the account home, where a person without a default workspace lands). Line 3, measured against `BC-Shell-1280-768` and `BC-Shell-320` read live, light and dark, in `design-fidelity-board-states.mock.e2e.spec.ts` and `design-fidelity-shell-widths.mock.e2e.spec.ts`: the view tab strip (1280, 320); the seven states Er, E, Ev, NP, VM, RO and NG at 1280, 768 and 320 across the five mock worlds; and L (new: the loading grid is the drawn toolbar switched off, the grid header and 36 px rows of 10 px bars, with the phone's 84 px cards at 320, 12 of 12 with the 1440/1024/390 review for overflow and axe). The 768 Er toolbar now draws the 60 px tablet bar. The sidebar specs now count the Chat row (six rows, rows below it 36 px lower). The loading state also draws its header mark and two title bars (measured at 1280: 120 by 10 and 220 by 18 px) and, at 320, the phone bar of a 44 px search button and a 44 px pill in a 60 px row (measured). Vitest `workspace-isolation.integration.test.ts` and `account-isolation.integration.test.ts` 12 of 12 passed 2026-11-15; the viewer overlay Ev at 320 light passed on re-run (6 of 6).

### UC-WSP-I02 List the workspaces I belong to
- Capability: I Workspace selection / switch integration
- Actors and permission: Any signed-in person
- Preconditions: Signed in.
- Input: none.
- Output: One row per active membership of an active workspace: id, name, my role, default flag, `canLeave`; member count except for Guests; department count for Owner and Admin only.
- Main flow: 1. `GET /api/v1/account/workspaces`. 2. Handler lists my active memberships.
- No-op / rejection / concurrency / retry: Left, removed, suspended, archived and deleted never appear (LD-02 adds archived for Owners and Admins). Never another person's rows.
- UI states: Loading, empty, success.
- Rules: [workspaces](../../product/workspaces.md) section 19.1; WSP-010.
- Acceptance: Given a person who left W1, When they list, Then W1 is absent.
- Evidence: BE `GET /api/v1/account/workspaces`, `GetMyWorkspacesQueryHandler` (now also returns an archived workspace, flagged `isArchived`, to its Owners and Admins only, never as default) · FE rail, account `workspaces.tsx` with an "Archived workspaces" group whose rows show an Unarchive action (GET workspace for its version, then `POST /workspaces/{id}/archive/unarchive`), `use-workspace-list.ts` · Design `BC-Shell-1440`, `Final-Account` (no artboard for the group; built by eye) · Tests `AccountWorkspaceEndpointTests`, `AccountWorkspacesHttpFlowTests`, `AccountWorkspacesAndAvatarRlsIntegrationTests`, `account-pages.mock.e2e.spec.ts`, to be extended: archived shown to Owner and Admin only, hidden from Member and Guest, never default; the existing `toWorkspaceEntry` and copy-catalog key tests need the new fields · Real e2e workspace-read-and-lists.real.e2e.spec.ts · Lane run 2026-11-03: line 3 closed: the account workspaces page with its Archived and Recently deleted groups at 1440, 1024 and 390 px, light and dark, passes the audit (the account tabs are now 44 px at 390); decision LD-06 as in UC-WSP-B04. Line 4: real stack (project notrelix-fe-real-e2e, 2026-11-03, branch fix/workspaces-acceptance-gaps), `workspace-read-and-lists.real.e2e.spec.ts` 3 of 3 passed.
- State: DONE — lines 1–4 proven in the lane pull request (2026-11-03, branch fix/workspaces-acceptance-gaps); see the Evidence line.

### UC-WSP-I03 Choose my default workspace
- Capability: I Workspace selection / switch integration
- Actors and permission: Any active member
- Preconditions: Active membership of the target.
- Input: `workspaceId`.
- Output: Identity stores the id on my profile; after sign-in the app opens it first (through the `?open=default` landing). A default I cannot open reads as absent. The Home launcher marks the default.
- Main flow: 1. `PUT /api/v1/account/default-workspace`. 2. Workspaces checks active membership. 3. Identity stores the id.
- No-op / rejection / concurrency / retry: The current default: no-op. Not a member: refused. Leaving clears it (F06).
- UI states: Account page choice, pending, success.
- Rules: [workspaces](../../product/workspaces.md) section 19.1.
- Acceptance: Given membership in W2, When I set it, Then the next sign-in opens W2.
- Evidence: BE `PUT /api/v1/account/default-workspace`, `SetDefaultWorkspaceCommandHandler` · FE account `workspaces.tsx`; a finished sign-in lands on `/home?open=default` (`sign-in-flow.tsx`, `sign-in.tsx`, `auth-callback.tsx`, `index.tsx`), and `/home` (`home.tsx`) opens the default workspace when one is set and not archived, otherwise it shows the launcher; plain `/home` (the way back to the launcher) never bounces · Design `Final-Account` · Tests `UserProfileDefaultWorkspaceTests`, `AccountWorkspaceEndpointTests`, `LeaveWorkspaceHandlerTests` (archived refusal, nothing stored), `account-pages.mock.e2e.spec.ts` · Real e2e none
- State: DONE — Proven by tests that ran in this PR. Domain `UserProfileDefaultWorkspaceTests` (a new profile has none, set is one version step, the same value is a no-op, clear, an empty id is refused). Application `LeaveWorkspaceHandlerTests` (12 pass: `SetDefault_ForAnActiveMember_ChoosesTheWorkspace`, archived refused with nothing stored, removed or suspended or absent member NotFound, leaving clears the default). API `AccountWorkspaceEndpointTests` (non-member 404, empty id 400). Integration `AccountWorkspacesAndAvatarRlsIntegrationTests` (set flags the list, non-member rejected and default unchanged, leave clears it) in the Workspaces shard (545 pass). Frontend: `account-workspaces.interaction.component.test.tsx` (15 pass: make default is offered only on another workspace, disabled while a change is in flight, an archived workspace offers Unarchive and never make-default), `account-pages.component.test.tsx` (49 pass: the choice is sent, the success notice, a refusal is explained in words), `home-landing.component.test.tsx` (12 pass: the default opens in place of the launcher, waits while the list loads, stays on the launcher with no default, an archived or unlisted default, a failed read, no flag, or after Make default), `sign-in-landing.component.test.tsx` (5 pass: sign-in, provider callback and root land on `/home?open=default`; a return address wins). Failing-first: the two opens-the-default tests failed with `expected [] to deeply equal [ '/workspaces/w-2' ]` before the landing was built. Note: commit `a6624b18d` turned `/home` into a launcher inside the unrelated PR #423 with no owner decision; this PR restores the row's promise, and sign-in opens the default only through `?open=default`, while plain `/home` never bounces. Known limit: sign-in lands on `/home?open=default`, and the flag stays in the URL when the launcher is shown. It is inert after the first decision, but a reload re-evaluates it.

### UC-WSP-I04 Authorize a realtime subscription to a workspace
- Capability: I Workspace selection / switch integration
- Actors and permission: System (realtime hub) for the caller (`ViewWorkspace`)
- Preconditions: The caller connects and asks to subscribe.
- Input: Workspace id.
- Output: Allow or deny; events of one workspace never reach a non-member.
- Main flow: 1. The hub sends `AuthorizeWorkspaceRealtimeSubscriptionQuery`. 2. The pipeline checks membership. 3. Subscribe or refuse.
- No-op / rejection / concurrency / retry: A removed member must lose the subscription at the next check. No HTTP route.
- UI states: None; the reconnecting state belongs to the shell.
- Rules: [workspaces](../../product/workspaces.md) sections 55, 56; WSP-016; NRX-014.
- Acceptance: Given a non-member, When they subscribe, Then refused.
- Evidence: BE `AuthorizeWorkspaceRealtimeSubscriptionQueryHandler`; the gateway (`RealtimeConnection.ReauthorizeAsync`) re-runs it for every open subscription every 60 seconds and revokes a denied one · FE `realtime-adapter.ts` · Design none · Tests `WorkspaceScopedArchitectureTests` · Real e2e workspace-read-and-lists.real.e2e.spec.ts · Acceptance run 2026-11-02 by an independent acceptance agent that did not build it, at develop `a6708f501` (branch accept/batch-3). Line 1: Application `RealtimeSubscriptionAuthorizationTests` (in the 67 passed), Architecture `WorkspaceScopedArchitectureTests`; on the real stack `workspace-read-and-lists.real.e2e.spec.ts` (3 tests passed, 21 of 21 across the eight real specs of this run): a stranger's subscription is denied and no event frame is sent, a member's is acknowledged, and a removed member's subscription is revoked within the check interval. Line 2: `apps/web` `realtime-lifecycle.component.test.tsx` (after the revoke a later event of the workspace is dropped and the workspace is not subscribed again; 47 passed with the share dialog, bulk bar and auth flow tests). Line 3: no screen. No HTTP route, so there is no cross-tenant HTTP case; the non-member case is the stranger above.
- State: DONE — accepted by independent acceptance 2026-11-02 at `a6708f501` (branch accept/batch-3); see the Evidence line.

### UC-WSP-I05 See why a workspace is unavailable
- Capability: I Workspace selection / switch integration
- Actors and permission: Any signed-in person
- Preconditions: The person opens a workspace route.
- Input: Route id.
- Output: One clear state: no membership, read-only, archived, plan-limited, temporarily unavailable.
- Main flow: 1. Reads return 404, 403 or a status. 2. The shell maps each to a state. 3. The Owner of an archived workspace sees Unarchive (D02); every member sees the read-only banner.
- No-op / rejection / concurrency / retry: No membership and missing look identical (no existence leak). Archived members read but cannot write.
- UI states: The five states of section 86, each with its next step.
- Rules: [workspaces](../../product/workspaces.md) sections 65, 86; DESIGN.md sections 35, 37.
- Acceptance: Given an archived workspace, When a Member opens it, Then a read-only banner shows and write controls are inert.
- Evidence: BE statuses of B01 · FE `workspace-scope-state-surface.tsx`, `feature-unavailable-surface.tsx` · Design `Final-States` · Tests `workspace-shell.mock.e2e.spec.ts` · Real e2e none
- State: IMPLEMENTING — Proven by tests that ran in this PR: no membership and a missing workspace look identical (`workspace-shell-routing.unit.test.ts`: `forbidden` and `not_found` both give the same scope state; `workspace-shell.interaction.component.test.tsx`: the forbidden state with its retry and way out, the temporarily-unavailable state separate from lost access, the loading state); archived: `archived-workspace-banner.component.test.tsx` and `archived-workspace-banner-host.component.test.tsx` (read-only banner for every member, Unarchive for the Owner only, pending, stale version, refusal), and the settings forms are read-only while archived (`workspace-settings.archived.component.test.tsx`); defect fixed with a failing-first test: an archived workspace was shown to its members as "no access" (`workspace-scope.archived.unit.test.ts`, red `expected 'forbidden' to be null`). Still open: (1) the read-only state of an Account that is suspended and (2) the plan-limited state (`Wsp-Unavail`, `Wsp-Var-Unavail-*`) are not built, they depend on Billing and on Account suspension (D06, DEFINED); (3) no real e2e. real-stack check 2026-10-31: a person who is not an active member (never joined, suspended, removed) now gets the same 404 as a made-up workspace id on every workspace-scoped route, from the one workspace gate in ExecutionContextBehavior; bodies byte-identical except echoed ids on members, members/me, members/me/access, roles, api-tokens, boards, pages, spaces and the custom-role PUT and DELETE; a member who lacks the action keeps 403; invitation accept by token and account routes unchanged, commit 9ca19ef3a

### UC-WSP-I06 Always see which workspace I am in
- Capability: I Workspace selection / switch integration
- Actors and permission: Any member
- Preconditions: A workspace route is open.
- Input: none.
- Output: The workspace name and mark show in the sidebar switcher and in destructive dialogs (members, Teams, Spaces, settings).
- Main flow: 1. The shell renders the switcher. 2. Dialogs name the workspace.
- No-op / rejection / concurrency / retry: Never blank while loading.
- UI states: Switcher, dialog titles.
- Rules: [workspaces](../../product/workspaces.md) section 84.
- Acceptance: Given a destructive dialog, Then it names the workspace.
- Evidence: BE none · FE `workspace-sidebar-surface.tsx` (skeleton with a status label while loading), `workspace-topbar-surface.tsx`; dialog audit done: member remove and suspend, workspace archive and leave already named the workspace; invitation revoke, Space archive and Team archive now do (copy en and vi) · Design `BC-Shell-1440`, `BC-Shell-320` · Tests `design-fidelity-shell.mock.e2e.spec.ts`; existing tests that assert the old revoke, Space archive and Team archive texts need updating; `space-page.interaction.component.test.tsx` (the Space delete confirm names the workspace) · Real e2e none
- State: DONE — Proven by tests that ran in this PR. Switcher: `workspace-shell.interaction.component.test.tsx` (25 pass: `FUI[workspace.sidebar:system-rail]` the workspace chip shows the name, `FUI[workspace.sidebar:workspace-mark]` the mark, `FUI[workspace.sidebar:states]` a labelled placeholder while loading, never blank). Destructive dialogs name the workspace: members remove, suspend, role change and invitation revoke (`workspace-members.interaction.component.test.tsx`, `FUI[workspace.members:workspace-named]`, 25 pass), workspace archive and delete (`workspace-settings.interaction.component.test.tsx`, 13 pass), leave the workspace (`account-workspaces.interaction.component.test.tsx`, `FUI[account.workspaces:leave]`), Space archive and delete (`space-page.interaction.component.test.tsx`, 35 pass), Team archive and remove (`workspace-team.interaction.component.test.tsx` 14 pass and `workspace-teams.interaction.component.test.tsx` 10 pass). Defect found and fixed: the Team archive confirmation did not name the workspace; failing-first both Team archive tests failed with `expected 'Lưu trữ Nhóm Kỹ sư ERP?3 người sẽ mất…' to contain 'Minh An Group'`, fixed in `workspace-teams.copy.ts` (en and vi) and `create-team-dialog.tsx`.

## J. Workspace settings

### UC-WSP-J01 Read workspace settings
- Capability: J Workspace settings
- Actors and permission: Any active member (`ViewWorkspace`)
- Preconditions: Active membership.
- Input: `workspaceId`.
- Output: Sharing and invitation policy, default member role, invitation expiry, guest default expiry, size, language, time zone, week start. The version a write quotes comes with the workspace read (`GetWorkspace`).
- Main flow: 1. `GET /api/v1/workspaces/{id}/settings`.
- No-op / rejection / concurrency / retry: A caller without access gets the same NotFound as for a missing id.
- UI states: Loading, success, permission denied.
- Rules: [workspaces](../../product/workspaces.md) sections 39, 48.2, 48.4; WGREQ010.
- Acceptance: Given a member, Then the stored values return, and the workspace read carries the version a write quotes.
- Evidence: BE `GET /api/v1/workspaces/{id}/settings`, `GetWorkspaceSettingsQueryHandler` · FE `workspace.service.ts`, `use-workspace-settings.ts` · Design `Final-Settings` (no settings-form artboard found; built by eye) · Tests `WorkspaceSettingsAccessIntegrationTests`, `ArchivedWorkspaceSettingsAndNavigationIntegrationTests`, `WorkspaceSettingsTests`, `workspace-settings.states.component.test.tsx`, `workspace-settings.interaction.component.test.tsx` · Real e2e none
- State: DONE — Proven by tests that ran in this PR. Integration `WorkspaceSettingsAccessIntegrationTests` (Owner, Admin, Member and Guest read the same stored values; a non-member and another tenant's Owner get NotFound, like a made-up id) and `ArchivedWorkspaceSettingsAndNavigationIntegrationTests` (the settings of an archived workspace still read for every member), in the Workspaces shard (573 pass; Auth and Workspaces shard exit 0). Domain `WorkspaceSettingsTests` (Domain run: 32 pass with the navigation tests). Frontend `workspace-settings.states.component.test.tsx` (a labelled loading status per section, the profile load error without a form, the defaults load error with its own retry, the stored values) and `workspace-settings.interaction.component.test.tsx` (read-only without the manage permission); 201 web tests pass across the settings, teams, home, command, shell and library folders. Known limits: a Guest reads the settings too (`ViewWorkspace`, as the row says); the settings read carries no version, a write quotes the workspace version.

### UC-WSP-J02 Change workspace settings
- Capability: J Workspace settings
- Actors and permission: Owner, Admin (`ManageWorkspace`)
- Preconditions: Caller holds the current version.
- Input: `allowPublicSharing`, `allowGuestInvites`, `defaultMemberRole`, `invitationExpiryDays`, `guestDefaultExpiryDays` (30, 90, 180, none), `size`, `language`, `timeZone`, `weekStart`, `expectedVersion`. A field left out keeps its value.
- Output: New settings, version +1; `PublicSharingChangedAt` set when sharing flips.
- Main flow: 1. `PUT /api/v1/workspaces/{id}/settings`. 2. Version check. 3. Domain validates each field. 4. Store.
- No-op / rejection / concurrency / retry: Unchanged: no-op. Invalid size or language: `Workspaces_Settings_SizeInvalid` / `LanguageInvalid`. Stale: 412 (`common.precondition-failed`). Guest expiry only pre-fills new entries (GOV-031).
- UI states: Form with save bar, validation, conflict, pending, permission denied (read-only).
- Rules: [workspaces](../../product/workspaces.md) sections 39, 40, 48.4; WSP-012; WGREQ010; WX-SET-001.
- Acceptance: Given version 5, When an Admin saves, Then version is 6 and a second save carries 6.
- Evidence: BE `PUT /api/v1/workspaces/{id}/settings`, `UpdateWorkspaceSettingsCommandHandler` · FE `use-workspace-settings-mutations.ts`, settings surface · Design `Final-Settings` · Tests `UpdateWorkspaceSettingsCommandHandlerTests`, `WorkspaceSettingsAccessIntegrationTests`, `ArchivedWorkspaceSettingsAndNavigationIntegrationTests`, `workspace-settings-service.unit.test.ts`, `workspace-settings.states.component.test.tsx`, `workspace-settings.interaction.component.test.tsx` · Real e2e none
- State: HARDENING — Proven by tests that ran in this PR: Integration `WorkspaceSettingsAccessIntegrationTests` (the Owner saves with the version read, the version moves by one and the next save carries it; Member and Guest are Forbidden and an outsider NotFound, nothing changes; a stale version is `common.precondition-failed` (412) and changes nothing; a missing version is refused; an unchanged save keeps the version and writes no audit fact; opening sharing stores one Warning fact and stamps the flip instant; an expiry of 0 or 31, the Owner as default role, a bad size, language or Guest expiry are refused and move nothing) and `ArchivedWorkspaceSettingsAndNavigationIntegrationTests` (a settings write in an archived workspace is Forbidden with `workspace.archived` and changes nothing), in the Workspaces shard (573 pass; Auth and Workspaces shard exit 0); Application `UpdateWorkspaceSettingsCommandHandlerTests` (11 pass: expiry rule, audit facts, fields left out keep their value); Frontend `workspace-settings.interaction.component.test.tsx` (validation, conflict with reload, saved, read-only without the permission, no version) and `workspace-settings.states.component.test.tsx`. Defect fixed with a failing-first test: the web client sent no `guestDefaultExpiryDays`, which the producer reads as none, so every save (the sharing switch included) cleared the Guest default (`workspace-settings-service.unit.test.ts`: 3 red, then 3 pass). Remaining gaps: (1) Admin: the row lists Owner and Admin, but the write needs `ManageWorkspace`, which the role baseline gives the Owner only (GOV-013), and the web form is offered to Admins, who then get a refusal; a product ruling decides which side moves, so no test pins Admin; (2) the Guest default expiry cannot be changed from the web (the Sharing page draws it read-only); (3) the stale answer is 412, and the rule line said 409.

### UC-WSP-J03 Read sharing and security extras
- Capability: J Workspace settings
- Actors and permission: Owner, Admin, Member (`ViewMembers`); Guest refused
- Preconditions: Active non-Guest membership.
- Input: `workspaceId`.
- Output: Active Guest count, default Guest expiry, when share links were turned off, company domain (an Accounts fact, may be null).
- Main flow: 1. `GET /api/v1/workspaces/{id}/security-ext`.
- No-op / rejection / concurrency / retry: Guest: 403. The domain row hides when null.
- UI states: Loading, success, permission denied.
- Rules: [workspaces](../../product/workspaces.md) section 48.2; GOV-031, GOV-032; see [governance](governance.md).
- Acceptance: Given links turned off on a date, Then that date returns.
- Evidence: BE `GET /api/v1/workspaces/{id}/security-ext`, `GetWorkspaceSecurityExtQueryHandler` · FE `use-teams-ext.ts`, `workspace-security-surface.tsx` · Design `Final-Settings`, `Final-Teams` · Tests `WorkspaceSecurityExtIntegrationTests`, `ArchivedWorkspaceSettingsAndNavigationIntegrationTests`, `workspace-security.states.component.test.tsx`, `workspace-team.interaction.component.test.tsx` · Real e2e none
- State: HARDENING — Proven by tests that ran in this PR: Integration `WorkspaceSecurityExtIntegrationTests` (the Guest count is this workspace's active Guests only; the company domain is the Account's first verified one and null when none; the links-off date follows the flips; the Guest expiry is stored and validated; Member and Admin read, a Guest is Forbidden, an outsider NotFound, another tenant's Owner refused) and `ArchivedWorkspaceSettingsAndNavigationIntegrationTests` (still reads in an archived workspace), in the Workspaces shard (573 pass; Auth and Workspaces shard exit 0); Frontend `workspace-security.states.component.test.tsx` (a loading status that names this page, the load error with its own retry, the domain row, the domain row hidden when null, saving holds the switch and shows the failure, the singular Guest count and an expiry with no end) and `workspace-team.interaction.component.test.tsx` (the links-off date, the switch, read-only for a Member). Defects fixed with failing-first tests: the domain row showed an empty field when the Account has no verified domain (the line says it hides), and the loading and error texts of this page said Teams (own copy, en and vi). Remaining gap: the permission-denied state. A Guest who opens the page gets the generic load error with Try again, not a denied state, and no board draws one.

### UC-WSP-J04 Keep Account-wide policy out of workspace settings
- Capability: J Workspace settings
- Actors and permission: Owner, Admin
- Preconditions: The settings value carries `EnforceMfa`.
- Input: `enforceMfa` on the settings write.
- Output: A decision on who owns MFA enforcement.
- Main flow: 1. The settings write accepts and stores `enforceMfa`. 2. Nothing in Workspaces or Identity enforces it.
- No-op / rejection / concurrency / retry: BLOCKED: WSP-012 forbids Account-wide security policy in workspace settings, yet the field exists in Domain, command and API. Decide: move it to Accounts or Identity, or keep it as a workspace policy with a named enforcer. Security-policy choice.
- UI states: Hide the control until decided.
- Rules: [workspaces](../../product/workspaces.md) sections 39, 40; WSP-012.
- Acceptance: Not testable until the owner is decided.
- Evidence: BE `WorkspaceSettings.EnforceMfa`, `UpdateWorkspaceSettingsCommand.EnforceMfa` · FE not shown · Design none · Tests `WorkspaceSettingsTests` · Real e2e none
- State: BLOCKED — Decision: owner and enforcement of the MFA policy.

## K. Spaces (added by this catalog)

### UC-WSP-K01 Create a Space (department)
- Capability: K Spaces (added by this catalog)
- Actors and permission: Owner, Admin (`ManageSpaces`); Space Admin of the parent where Governance allows
- Preconditions: Workspace Active; parent (if any) is Active and in the same workspace.
- Input: `name`, `description`, `type` (Folder, Portfolio, Department, ProjectArea), `visibility`, `parentId`, neighbour for position.
- Output: A Space with a fractional position key; the creator is its first Space Admin; Department defaults to Private, other types to Workspace.
- Main flow: 1. `POST /api/v1/workspaces/{id}/spaces`. 2. Application supplies ancestry and sibling names. 3. Domain validates and creates. 4. Closure index updated. 5. Creator entry added.
- No-op / rejection / concurrency / retry: Sibling name clash (case-insensitive): refused. Parent archived, deleted or in another workspace: refused before mutation. A stale neighbour intent is refused, not guessed. A retry under the same Idempotency-Key returns the Space the first attempt made; a different body under that key is refused.
- UI states: Loading, success, validation error, permission denied, conflict, pending, offline, destructive confirmation.
- Rules: [workspaces](../../product/workspaces.md) sections 41, 43.1, 44, 68; WSP-013, WSP-021, WSP-024; WGREQ038, WGREQ040, WGREQ041; WX-SPACE-001.
- Acceptance: Given an Admin, When they create `IT` as a Department, Then it is Private and the Admin is its Space Admin. Given a sibling `it`, Then refused.
- Evidence: BE `POST /api/v1/workspaces/{id}/spaces`, `CreateSpaceCommandHandler` · FE `create-space-dialog.tsx`, `use-create-space.tsx`, `spaces.service.ts` · Design `Final-Space`, `Final-M-Space` · Tests `SpaceTests`, `SpaceHierarchyTests`, `CreateSpaceCommandHandlerTests`, `SpaceHierarchyIntegrationTests`, `SpaceEndpointTests`, `workspace-spaces.mock.e2e.spec.ts` · Real e2e workspace-spaces.real.e2e.spec.ts · Acceptance run 2026-11-02 by an independent acceptance agent that did not build it, at develop `a6708f501` (branch accept/batch-3). Line 1: Domain `SpaceTests` and `SpaceHierarchyTests`, Application `CreateSpaceCommandHandlerTests`, API `SpaceEndpointTests`, Integration `SpaceHierarchyIntegrationTests` (the named classes passed); on the real stack `workspace-spaces.real.e2e.spec.ts` (2 tests: a sibling clash, a different body under one key, a Member and a foreign parent create nothing, a retry returns the first Space). Line 2: `space-dialogs.interaction.component.test.tsx` (11 passed; a Department is Private by default). Line 3: measured, `design-fidelity-space.mock.e2e.spec.ts` 10 passed; reviewed, the create and placement dialog sheets at 1440, 1024 and 390 px in both themes were opened and read (390 px light of both and 1440 px dark of the create dialog): no overflow or clipping, the field, type select and buttons are 44 px at 390 px. `workspace-acceptance.mock.e2e.spec.ts` 60 passed (axe with no serious finding).
- State: DONE — accepted by independent acceptance 2026-11-02 at `a6708f501` (branch accept/batch-3); see the Evidence line.

### UC-WSP-K02 Read the Space tree and list Spaces
- Capability: K Spaces (added by this catalog)
- Actors and permission: Any non-Guest member (`ViewWorkspace`); only Spaces the caller can see
- Preconditions: Active membership.
- Input: `workspaceId`.
- Output: The org tree (ordered siblings) and the flat list of visible Spaces.
- Main flow: 1. `GET .../spaces/tree` and `GET .../spaces`. 2. Governance visibility filters the ids. 3. Handler returns nodes.
- No-op / rejection / concurrency / retry: A Private Space the caller cannot reach is absent, never counted (GOV-029). Both reads are cursor pages (LD-06): 50 a page by default, at most 200; the tree pages in Space id order and the client reads every page and orders siblings by position; the list is ordered by name.
- UI states: Loading, empty (first steps), success, error.
- Rules: [workspaces](../../product/workspaces.md) sections 41, 43, 44.1; WSP-021, WSP-023; WX-SPACE-002.
- Acceptance: Given a Private Space and a Member outside it, Then the tree omits it.
- Evidence: BE `GET .../spaces/tree`, `GET .../spaces`, `GetSpaceTreeQueryHandler`, `GetWorkspaceSpacesQueryHandler` · FE `space-tree-nav.tsx`, `use-spaces.ts`, `use-space-tree-expansion.ts` · Design `Final-Space`, `BC-Shell-1440` · Tests `SpaceAccessMatrixIntegrationTests`, `CollectionVisibilityIntegrationTests`, `SpaceHierarchyHandlerTests` · Real e2e workspace-spaces.real.e2e.spec.ts · Line 3, lead decision 2026-11-03: `Final-M-Space` `danh-sach` and `phong` are native mobile-runtime tab frames (a tab bar app, like `Final-M-Auth`); on the web at 390 px the Space index is the tree in the drawer, an undrawn state. Screenshot review at 1440, 1024 and 390 px, light and dark: `workspace-acceptance` 'Space tree with a department open' (English) and 'Space tree in Vietnamese with a department open' (390 px, light and dark: no clipping, 44 px targets, axe clean). The department page overflow at 390 px (`phong`) stays with UC-WSP-K14 (0.3).
- State: DONE — lines 1–4 proven in the lane pull request (2026-11-03, branch wsp/finish-0-1); see the Evidence line.

### UC-WSP-K03 Read one Space
- Capability: K Spaces (added by this catalog)
- Actors and permission: Any member who can see the Space (`ViewWorkspace` plus visibility)
- Preconditions: The Space is visible to the caller.
- Input: `spaceId`.
- Output: Name, description, type, visibility, status, parent, version (my effective role is on the tree node).
- Main flow: 1. `GET .../spaces/{spaceId}`. 2. Visibility check. 3. Return.
- No-op / rejection / concurrency / retry: A caller without access gets the same NotFound as for a missing id.
- UI states: Loading, success, not found.
- Rules: [workspaces](../../product/workspaces.md) sections 41, 44.1; WSP-023; GOV-025.
- Acceptance: Given a Private Space and an outsider, Then 404 identical to a missing id.
- Evidence: BE `GET .../spaces/{spaceId}`, `GetSpaceQueryHandler` · FE `spaces.service.ts` · Design `Final-Space` · Tests `SpaceAccessMatrixIntegrationTests`, `SpaceEndpointTests`, `space-state.component.test.tsx`, `space-page.interaction.component.test.tsx`, `space-page.mock.e2e.spec.ts` · Real e2e workspace-space-read.real.e2e.spec.ts · Line 1: the producer is unchanged; the access matrix proves the single read equals the tree for every persona and an invisible Space is the same NotFound as a missing id; the real test shows a missing id, another workspace's department and a Private department for an outsider all answer 404. Line 2: the department page reads the Space by id (`createUseSpace`, `spaces.service.ts` `get`) and shows that live record (name, description, type, visibility, status, version) over the tree node, which keeps position and my access; pending, NotFound with a way back (Go to Home), and error with Retry are tested (`space-state.component.test.tsx`, `space-page.interaction.component.test.tsx`); every Space write refreshes the record. Line 3: no new drawn state; the not-found state is undrawn, so `space-page.mock.e2e.spec.ts` opens it at 1440, 1024 and 390 px, light and dark, in Vietnamese, with no horizontal scroll (6 passed) and the screenshots were reviewed; `design-fidelity-space.mock.e2e.spec.ts` still passes. Line 4: `workspace-space-read.real.e2e.spec.ts` 1 passed on the real stack, 2026-11-16 (a rename done through the API shows on the page; rejection: outsider, missing id and another workspace's id are 404, and the page reads not found). The error state needs a failing producer and is covered by the component test only.
- State: DONE — closed 2026-11-16; see the Evidence line.

### UC-WSP-K04 Rename a Space or edit its description
- Capability: K Spaces (added by this catalog)
- Actors and permission: Owner, Admin, Space Admin (`ManageSpaces`)
- Preconditions: Space Active.
- Input: `name` or `description`.
- Output: Updated Space, version +1.
- Main flow: 1. `PUT .../spaces/{spaceId}` (rename) or `PUT .../spaces/{spaceId}/description`. 2. Domain checks sibling uniqueness.
- No-op / rejection / concurrency / retry: Same value: no-op. Clash: refused. Both carry an expected version; a stale one is 412 and nothing changes.
- UI states: Inline edit, validation, pending, conflict.
- Rules: [workspaces](../../product/workspaces.md) section 43.1; WGREQ038, WGREQ039; WX-SPACE-003.
- Acceptance: Given a rename to a taken sibling name, Then refused.
- Evidence: BE `PUT .../spaces/{spaceId}`, `PUT .../spaces/{spaceId}/description`, `RenameSpaceCommandHandler`, `UpdateSpaceDescriptionCommandHandler` · FE `space-settings-panel.tsx`, `spaces.service.ts` · Design `Final-Space` · Tests `SpaceTests`, `SpaceVersionTests`, `SpaceHierarchyIntegrationTests` (stale-version case, CI), `space-page.mock.e2e.spec.ts` · Real e2e workspace-space-manage.real.e2e.spec.ts · Line 1: Domain `SpaceTests` (25), `SpaceVersionTests` (4), Integration `SpaceHierarchyIntegrationTests` (`AStaleExpectedVersion_IsRejected_ForEveryVersionedSpaceEdit_WithoutChanging`, `SiblingNames_AreUniqueCaseInsensitively_PerParent`, 13 in the class), API `SpaceEndpointTests`; on the real stack the first test of `workspace-space-manage.real.e2e.spec.ts` (a stale rename is 412, a sibling name in another case is refused, a Member is refused and an outsider gets 404, the same name, description and visibility bump no version, the repeat of a guarded write is a 412 with no second effect). Line 2: `space-page.mock.e2e.spec.ts` 13 passed (rename saves and the sidebar follows). Line 3: `design-fidelity-space.mock.e2e.spec.ts` 10 passed (the settings tab). Line 4: `workspace-space-manage.real.e2e.spec.ts` 2 passed on the real stack, 2026-11-16.
- State: DONE — closed 2026-11-16; see the Evidence line.

### UC-WSP-K05 Change a Space's type
- Capability: K Spaces (added by this catalog)
- Actors and permission: Owner, Admin, Space Admin (`ManageSpaces`)
- Preconditions: Space Active.
- Input: `spaceType`.
- Output: New type; visibility is not changed by it.
- Main flow: 1. `PUT .../spaces/{spaceId}/type`. 2. Domain sets the type.
- No-op / rejection / concurrency / retry: Same type: no-op. Carries an expected version; a stale one is 412.
- UI states: Select, pending.
- Rules: [workspaces](../../product/workspaces.md) sections 41, 44.1.
- Acceptance: Given a Folder, When set to Department, Then the type changes and visibility stays.
- Evidence: BE `PUT .../spaces/{spaceId}/type`, `ChangeSpaceTypeCommandHandler` · FE `space-settings-panel.tsx`, `spaces.service.ts` · Design `Final-Space` (no board draws the control) · Tests `SpaceHierarchyIntegrationTests`, `space-page.interaction.component.test.tsx`, `space-page.mock.e2e.spec.ts` · Real e2e workspace-space-type.real.e2e.spec.ts
- State: DONE — Line 1: `SpaceHierarchyIntegrationTests` (17 passed): Folder to Department keeps visibility, the same type is a no-op (version unchanged), a Member is refused, an admin of another workspace gets NotFound, a stale version is 412 (existing every-versioned-edit case); the archived Space refusal is in the domain. Line 2: a Type select (the create dialog's Select and labels) sits in the "Place in the tree" card of the settings tab, enabled for Owner, Admin and Space Admin, disabled for others and while archived; choosing the same type sends nothing; the save invalidates the tree and the Space record; `space-page.mock.e2e.spec.ts` and the component test pass. Line 3: no board draws it, so `design-fidelity-space.mock.e2e.spec.ts` passes unchanged (12 passed) and the row was reviewed at 1440, 1024 and 390, light and dark, in Vietnamese, with no horizontal scroll. Line 4: `workspace-space-type.real.e2e.spec.ts` 1 passed on the real stack, 2026-11-16 (Folder to Department from the UI, visibility stays, stale write 412, Member refused). Placement: the type row sits in the "Place in the tree" card, not next to visibility, because type describes where and what the Space is in the structure and moving it would break pinned board boxes (owner ruling 2026-11-16).

### UC-WSP-K06 Change a Space's visibility
- Capability: K Spaces (added by this catalog)
- Actors and permission: Owner, Admin, Space Admin (`ManageSpaces`)
- Preconditions: Space Active.
- Input: `visibility` (Private or Workspace).
- Output: New visibility; inheritance of Member and Viewer roles to descendants changes accordingly; access narrowing warns about assignees (Work Management).
- Main flow: 1. `PUT .../spaces/{spaceId}/visibility`. 2. Domain sets it. 3. Governance reads the new value.
- No-op / rejection / concurrency / retry: Same value: no-op. Carries an expected version; a stale one is 412. Making a Space Private may remove access from people who hold Boards in it.
- UI states: Choice with consequence text, pending, success.
- Rules: [workspaces](../../product/workspaces.md) sections 44.1; WSP-023; GOV-025.
- Acceptance: Given a Workspace Space, When set Private, Then a non-member loses it from the tree.
- Evidence: BE `PUT .../spaces/{spaceId}/visibility`, `ChangeSpaceVisibilityCommandHandler` · FE `space-settings-panel.tsx` · Design `Final-Space` · Tests `SpaceAccessMatrixIntegrationTests`, `SpaceHierarchyIntegrationTests` (stale-version case, CI), `space-page.mock.e2e.spec.ts` · Real e2e workspace-space-manage.real.e2e.spec.ts · Line 1: as K04, plus `SpaceAccessMatrixIntegrationTests` (5); the real test sets a Private department Open, a stale write is 412 and keeps it, narrowing to Private makes it 404 for a Member who was not added. Line 2: `space-page.mock.e2e.spec.ts` (visibility chosen with a click or the arrow keys, saved at once). Line 3: `design-fidelity-space.mock.e2e.spec.ts` 10 passed. Line 4: `workspace-space-manage.real.e2e.spec.ts` 2 passed on the real stack, 2026-11-16.
- State: DONE — closed 2026-11-16; see the Evidence line.

### UC-WSP-K07 Move or reorder a Space
- Capability: K Spaces (added by this catalog)
- Actors and permission: Owner, Admin, Space Admin (`ManageSpaces`)
- Preconditions: Target parent Active, same workspace, not a descendant.
- Input: `parentId`, neighbour keys, `expectedVersion`.
- Output: The subtree moves; position key recomputed.
- Main flow: 1. `POST .../spaces/{spaceId}/move`. 2. Application supplies ancestry. 3. Domain rejects cycles. 4. Closure rebuilt.
- No-op / rejection / concurrency / retry: Under itself or a descendant: refused before mutation (400; the picker lists the subtree disabled). Archived parent: refused (400). Deleted parent: NotFound (404). Stale neighbour: refused (409, nothing changes). Stale version: 412, nothing changes. Same place: no-op.
- UI states: Drag or picker (Move dialog), conflict, pending.
- Rules: [workspaces](../../product/workspaces.md) section 43.1; WSP-021; WGREQ038; WX-SPACE-004.
- Acceptance: Given A with child B, When A moves under B, Then refused and nothing changes.
- Evidence: BE `POST .../spaces/{spaceId}/move`, `MoveSpaceCommandHandler` · FE `move-to-space-dialog.tsx` (Boards and Docs), `use-move-to-space.tsx`, Space settings · Design `Final-Space` (no board draws the Space Move dialog) · Tests `SpaceHierarchyTests`, `SpaceHierarchyIntegrationTests`, `space-dialogs.interaction.component.test.tsx`, `space-page.mock.e2e.spec.ts` · Real e2e workspace-space-move.real.e2e.spec.ts · Line 1: `SpaceHierarchyIntegrationTests` 18 passed (Member refused, outsider NotFound, cycle refused with nothing changed, archived parent 400, deleted parent 404, stale neighbour 409, stale version 412, reorder after a sibling). Line 2: `space-dialogs.interaction.component.test.tsx` (pending, refusal message, retry; 70 passed in `src/web/spaces`); a retry after a conflict carries the live Space version. Line 3: no board draws it, so a screenshot review at 1440, 1024 and 390, light and dark, in Vietnamese, with no horizontal scroll and axe clean (`space-page.mock.e2e.spec.ts` 30 passed). Line 4: `workspace-space-move.real.e2e.spec.ts` 1 passed on the real stack, 2026-11-16 (move from the UI, version +1; A under its child B is not offered, the API refuses it with 400, a stale version is 412, nothing changes).
- State: DONE — closed 2026-11-16; see the Evidence line. Named limits, each needing a board first: the picker cannot place a Space between siblings or reorder (it appends; reorder works through the API only); tree drag is not built; a refusal from a race shows the server's English detail; the disabled-row pill shows a short word ("Bên trong", "Hiện tại", "Không có quyền") that fits whole at 390 px, with the full sentence in the tooltip and the screen-reader note.

### UC-WSP-K08 Archive or unarchive a Space
- Capability: K Spaces (added by this catalog)
- Actors and permission: Owner, Admin, Space Admin (`ManageSpaces`)
- Preconditions: Archive: no active child Space and no active Board or Page in it. Unarchive: parent not archived or deleted.
- Input: `spaceId`.
- Output: Status Archived or Active.
- Main flow: 1. `POST .../spaces/{spaceId}/archive` or `.../unarchive`. 2. Application supplies child and resource facts. 3. Domain checks.
- No-op / rejection / concurrency / retry: Archive with active content: refused with the reason (no cascade, WSP-022). Already in that state: no-op. Both carry an expected version in the body; a stale one is 412.
- UI states: Confirmation, blocked state with reason, pending.
- Rules: [workspaces](../../product/workspaces.md) section 43.2; WSP-022; WGREQ039.
- Acceptance: Given a Space with one Board, When archived, Then refused naming the Board.
- Evidence: BE `POST .../archive`, `POST .../unarchive`, `ArchiveSpaceCommandHandler`, `UnarchiveSpaceCommandHandler` · FE `space-settings-panel.tsx` · Design `Final-Space` · Tests `ArchiveSpaceCommandHandlerTests`, `SpaceHierarchyIntegrationTests` (stale-version case, CI), `space-page.mock.e2e.spec.ts` · Real e2e workspace-space-manage.real.e2e.spec.ts · Line 1: `ArchiveSpaceCommandHandlerTests` (2), `SpaceHierarchyIntegrationTests.ArchiveAndDelete_AreBlockedByActiveChildrenAndPlacedBoards_WithoutCascade`; the real test refuses to archive a department that holds a Board (nothing changes), archives an empty one through the confirm dialog (version +1), refuses a stale archive and a stale unarchive (412), treats a repeat as a no-op, refuses a rename while archived, and unarchives (version +1). Line 2: `space-page.mock.e2e.spec.ts` (blocked with the reason, confirm with focus return). Line 3: `design-fidelity-space.mock.e2e.spec.ts` 10 passed. Line 4: `workspace-space-manage.real.e2e.spec.ts` 2 passed on the real stack, 2026-11-16.
- State: DONE — closed 2026-11-16; see the Evidence line.

### UC-WSP-K09 Delete or restore a Space
- Capability: K Spaces (added by this catalog)
- Actors and permission: Owner, Admin, Space Admin (`ManageSpaces`)
- Preconditions: Delete: no non-deleted child and no non-deleted Board or Page in it.
- Input: `spaceId`.
- Output: Soft deletion, or restore when the parent is live.
- Main flow: 1. `DELETE .../spaces/{spaceId}` or `POST .../restore`. 2. Application supplies facts. 3. Domain checks.
- No-op / rejection / concurrency / retry: Delete takes `?expectedVersion=`; a stale version answers 412 and nothing changes. A repeat delete answers NotFound with no version bump. Delete is refused with the reason while the department has content. Restore takes no version; it is refused after the 30-day window (`Workspaces_Space_RestoreWindowExpired`, the window reused from `Board.RestoreWindow`) and under a deleted parent; a restore of a live Space is a no-op.
- UI states: Delete asks for the department name typed as confirmation, shows the blocked reason while it has content, and shows pending. The Deleted departments page at `/deleted-spaces` has pending, empty, error with Retry, permission denied, and a per-row refusal.
- Rules: [workspaces](../../product/workspaces.md) section 43.2; WSP-022; WGREQ039, WGREQ006.
- Acceptance: Given an empty Space, When deleted, Then it leaves the tree and restore returns it.
- Evidence: BE `DELETE .../spaces/{spaceId}?expectedVersion=`, `POST .../restore`, `GET .../spaces/deleted`, `DeleteSpaceCommandHandler`, `RestoreSpaceCommandHandler`, `GetDeletedSpacesQueryHandler`, `Space.RestoreWindow` · FE `space-settings-panel.tsx` (Delete, confirmation, blocked reason), `space-trash-surface.tsx`, `deleted-spaces.tsx`, `spaces.service.ts` (`remove`, `restore`, `listDeleted`), `use-space-mutations.ts` · Design `Final-Space` · Tests `DeletedSpacePurgeIntegrationTests` (3, real PostgreSQL: 31 days purged with its rows, 29 days and live kept, another workspace untouched, rerun a no-op), `DeleteSpaceCommandHandlerTests` (4), `SpaceTests` (restore window, repeat delete), `SpaceHierarchyIntegrationTests` (3 new, 16 of the class passed), `space-page.interaction.component.test.tsx`, `space-trash.component.test.tsx` (8), `space-delete-restore.unit.test.ts`, `space-team-delete-restore.unit.test.ts`, `space-page.mock.e2e.spec.ts`, `space-trash.mock.e2e.spec.ts` · Real e2e workspace-space-trash.real.e2e.spec.ts · Line 1: delete takes `expectedVersion` (a stale one answers 412 and nothing changes), a department with content is refused, a plain Member and an outsider are refused (outsider NotFound, also for the list and for a restore from another workspace), a repeat delete finds nothing (NotFound, no version bump), restore under a deleted parent and after the 30 days are refused with their rule codes, restore of a live department is a no-op, the list shows only what is still inside the window. Line 2: every state is built and tested: confirmation with the name, blocked with the reason, refused by the producer for hidden content, pending, deleted list loading, empty, error with Retry, permission denied, per-row failure, pending restore; axe found no serious issue on the open confirmation and on the list (`space-page.mock.e2e.spec.ts`, and again in the real run). Line 3: `design-fidelity-space.mock.e2e.spec.ts` 12 passed, including the delete confirmation measured against the `thiet-lap` artboard (the confirmation box, question, line, label, field and both buttons within 1 px, light, and the dark border); the deleted list has no board, so `space-trash.mock.e2e.spec.ts` (6 passed) opens it at 1440, 1024 and 390 px, light and dark, in Vietnamese, with no horizontal scroll and a 44 px touch target on the phone, and the screenshots were reviewed. Line 4: `workspace-space-trash.real.e2e.spec.ts` and `workspace-space-manage.real.e2e.spec.ts` 3 passed on the real stack, 2026-11-16.
- State: DONE — closed 2026-11-16; see the Evidence line. Retention (owner ruling): the Space trash reuses the Board trash window, 30 days (`Board.RestoreWindow`, Board.cs:10); delete now carries the version last read, as Team delete does, and restore stays version-less (RestoreTeam). The purge job `DeletedSpacePurge` (`DeletedSpacePurger`, option `Workspaces:Spaces:Purge`) removes a department deleted more than 30 days ago with its member and closure rows.

### UC-WSP-K10 See who is in a Space
- Capability: K Spaces (added by this catalog)
- Actors and permission: Owner, Admin, Space members (`ViewWorkspace` for the plain list; `ViewMembers` for the extra read); Guest refused on the extra read
- Preconditions: The Space is visible.
- Input: `spaceId`.
- Output: Entries (User or Team, role, version) and the extra read: people reached once at their highest role, job title, Teams that reach them, creator, pending invitations that name the Space.
- Main flow: 1. `GET .../spaces/{spaceId}/members`. 2. `GET .../spaces/{spaceId}/members-extra`.
- No-op / rejection / concurrency / retry: Guests are Board-level and not counted. Email only for Owner and Admin. Invisible Space: NotFound.
- UI states: Loading, empty, success, filters, permission denied.
- Rules: [workspaces](../../product/workspaces.md) sections 44.2, 44.3, 48.2; WSP-024, WSP-026.
- Acceptance: Given a Team entry, Then its active members show once at the highest role.
- Evidence: BE `GET .../members`, `GET .../members-extra`, `GetSpaceMembersQueryHandler`, `GetSpaceMembersExtraQueryHandler` · FE `space-members-view.tsx`, `use-space-members-extra.ts` · Design `Final-Space`, `Final-M-Space` · Tests `SpaceMembersExtraIntegrationTests`, `SpaceReadPagingTests`, `space-members-paging.unit.test.ts`, `space-members.mock.e2e.spec.ts`, `design-fidelity-members.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — Members-extra is not paged: the tab filters and counts client-side, so it needs a server filter and search contract (owner call). Owner question: the Space members read already pages (default 50, max 200), so only a filter is missing there. A name search would need names that Identity holds, which the Workspaces read cannot filter on, so the item stops here until the owner decides where search runs and whether the tab counts come from the members-extra summary. No code changed.

### UC-WSP-K11 Add a person or Team to a Space
- Capability: K Spaces (added by this catalog)
- Actors and permission: Owner, Admin, Space Admin (`ManageSpaces`)
- Preconditions: Subject is an active member of the same workspace (User) or an active Team of it; the Space is Active.
- Input: `subjectType`, `subjectId`, `role` (Admin, Member, Viewer), `expectedVersion`.
- Output: One entry per subject; Space version +1.
- Main flow: 1. `POST .../spaces/{spaceId}/members`. 2. Version check. 3. Domain adds.
- No-op / rejection / concurrency / retry: An outsider's email cannot be added here (invite instead, WSP-026); the dialog skips it with the reason. Existing entry: change the role (K12). Foreign-workspace id: refused.
- UI states: Add dialog, per-row skip reason, pending, conflict.
- Rules: [workspaces](../../product/workspaces.md) sections 22.1, 44.3; WSP-014, WSP-024, WSP-026.
- Acceptance: Given an active member, When added as Member, Then they appear once. Given a foreign user id, Then refused.
- Evidence: BE `POST .../spaces/{spaceId}/members`, `AddSpaceMemberCommandHandler` · FE `space-members-dialogs.tsx`, `spaces.service.ts` · Design `Final-Space` · Tests `SpaceRulesTests`, `SpaceAccessMatrixIntegrationTests` · Real e2e workspace-space-manage.real.e2e.spec.ts · Line 1: `SpaceRulesTests` (4), `SpaceAccessMatrixIntegrationTests` (5), `SpaceHierarchyIntegrationTests.Membership_AcceptsSameWorkspaceUsersAndTeams_AndKeepsALastAdmin`; the real test adds a person through the dialog (appears once, Viewer) and a Team, and refuses a foreign user id and a missing Team (404), a stale version (412), a Member and another role for an existing entry; the same role again is a no-op. Line 2: `space-members.mock.e2e.spec.ts` 11 passed (add by keyboard, outsider email skipped with the reason). Line 3: `design-fidelity-members.mock.e2e.spec.ts` 9 passed (the list, the add dialog). Line 4: `workspace-space-manage.real.e2e.spec.ts` second test, 2 passed on the real stack, 2026-11-16.
- State: DONE — closed 2026-11-16; see the Evidence line.

### UC-WSP-K12 Change a Space member's role
- Capability: K Spaces (added by this catalog)
- Actors and permission: Owner, Admin, Space Admin (`ManageSpaces`)
- Preconditions: Entry exists.
- Input: `subjectType`, `subjectId`, `role`, `expectedVersion`.
- Output: New role. The last Space Admin cannot be downgraded.
- Main flow: 1. `PATCH .../spaces/{spaceId}/members/{subjectType}/{subjectId}`. 2. Version check. 3. Domain applies the last-admin rule.
- No-op / rejection / concurrency / retry: Same role: no-op. Last Admin: refused. Two concurrent downgrades: one wins.
- UI states: Role menu, conflict, reason when blocked.
- Rules: [workspaces](../../product/workspaces.md) section 44.3; WSP-024; WGREQ040.
- Acceptance: Given one Space Admin, When lowered, Then refused with the reason.
- Evidence: BE `PATCH .../members/{subjectType}/{subjectId}`, `ChangeSpaceMemberRoleCommandHandler` · FE `space-members-view.tsx` (role menu), `spaces.service.ts` · Design `Final-Space` · Tests `SpaceRulesTests`, `SpaceVersionTests` · Real e2e workspace-space-manage.real.e2e.spec.ts · Line 1: `SpaceRulesTests`, `SpaceVersionTests`; the real test lifts a person with the role menu (version +1), refuses lowering the only Admin (400), a stale role (412), a Member, and an absent entry; the same role is a no-op. Line 2: `space-members.mock.e2e.spec.ts` (role menu keyboard, the only admin cannot be lowered, with the reason). Line 3: `design-fidelity-members.mock.e2e.spec.ts` 9 passed (the role menu). Line 4: `workspace-space-manage.real.e2e.spec.ts` second test, 2 passed on the real stack, 2026-11-16.
- State: DONE — closed 2026-11-16; see the Evidence line.

### UC-WSP-K13 Remove a person or Team from a Space
- Capability: K Spaces (added by this catalog)
- Actors and permission: Owner, Admin, Space Admin (`ManageSpaces`)
- Preconditions: Entry exists.
- Input: `subjectType`, `subjectId`, `expectedVersion`.
- Output: Entry gone; the dialog lists who loses access when a Team is removed.
- Main flow: 1. `DELETE .../spaces/{spaceId}/members/{subjectType}/{subjectId}?expectedVersion=`.
- No-op / rejection / concurrency / retry: Last Admin: refused. Absent entry: no-op or NotFound (state one).
- UI states: Confirmation listing consequences, pending.
- Rules: [workspaces](../../product/workspaces.md) section 44.3; WSP-024.
- Acceptance: Given a Team entry, When removed, Then its members lose the Space role unless they have another entry.
- Evidence: BE `DELETE .../members/{subjectType}/{subjectId}`, `RemoveSpaceMemberCommandHandler` · FE `space-members-view.tsx`, `space-members-dialogs.tsx`, `spaces.service.ts` · Design `Final-Space` · Tests `SpaceRulesTests` · Real e2e workspace-space-manage.real.e2e.spec.ts · Line 1: `SpaceRulesTests`; the real test removes a person through the confirm dialog (version +1), refuses removing the last Admin (400), a stale remove (412) and a Member, treats an absent entry as a no-op without a version, and removes a Team. Line 2: `space-members.mock.e2e.spec.ts` (confirm lists who loses access when a Team goes). Line 3: `design-fidelity-members.mock.e2e.spec.ts` 9 passed (the remove dialog). Line 4: `workspace-space-manage.real.e2e.spec.ts` second test, 2 passed on the real stack, 2026-11-16.
- State: DONE — closed 2026-11-16; see the Evidence line.

### UC-WSP-K14 Open the department page overview
- Capability: K Spaces (added by this catalog)
- Actors and permission: Owner, Admin, Space members (`ViewMembers`; Guest refused); `?as=member` only for a Space manager
- Preconditions: The Space is visible.
- Input: `spaceId`, optional `as=member`.
- Output: Structured figures: running Boards (at most 6), board and doc totals, people, child Spaces, access, workload (top 5), attention counts, root Pages (at most 4).
- Main flow: 1. `GET .../spaces/{spaceId}/overview`. 2. Governance visible-id sets filter Boards and Pages. 3. Work and Documents facts are read through ports. 4. The client writes the sentences.
- No-op / rejection / concurrency / retry: A Board or Page the caller cannot read is never shown nor counted. Preview by a non-manager: 403. A child Space the caller cannot read is omitted, not zero.
- UI states: Loading, empty first steps, success, partial error per section, permission denied.
- Rules: [workspaces](../../product/workspaces.md) section 48.3; WSP-013; GOV-025, GOV-029; WX-SPACE-005.
- Acceptance: Given a Private Board outside the caller's reach, Then it adds nothing to any count.
- Evidence: BE `GET .../spaces/{spaceId}/overview`, `GetSpaceOverviewQueryHandler` · FE `space-overview.tsx`, `use-space-overview.ts` · Design `Final-Space`, `Final-M-Space` · Tests `SpaceOverviewIntegrationTests`, `space-page.mock.e2e.spec.ts`, `design-fidelity-space.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — Department page overflows at 390 px (the phong frame, measured in K02); no scale test for large Spaces.

### UC-WSP-K15 Place a Board or Page in a Space (facts only)
- Capability: K Spaces (added by this catalog)
- Actors and permission: Work Management and Documents (consumers); Workspaces publishes Space facts
- Preconditions: A resource owner asks whether a Space exists, is Active and is in the same workspace.
- Input: Space id, workspace id.
- Output: A yes or no fact; the placement itself is stored by the resource owner.
- Main flow: 1. The Board or Page owner calls the Workspaces public placement port. 2. Workspaces answers from its facts.
- No-op / rejection / concurrency / retry: Foreign-workspace or archived Space: refused. A Space never owns the resource lifecycle.
- UI states: None here; pickers belong to the resource owner.
- Rules: [workspaces](../../product/workspaces.md) section 79; WSP-013; GOV-028; [ADR-009](../../../backend/docs/decisions/ADR-009-space-scoped-authorization.md).
- Acceptance: Given a Space of another workspace, Then placement is refused.
- Evidence: BE `Public/SpacePlacement`, `SpaceSubtree` ports · FE `move-to-space-dialog.tsx`, `board-placement-bridge.ts` · Design `Final-Space` · Tests `SpacePlacementIntegrationTests`, `SpacePlacementDomainTests` · Real e2e workspace-spaces.real.e2e.spec.ts · Lane run 2026-11-03: Application `SpacePlacementTargetTests` and Integration `SpacePlacementIntegrationTests` (a Space of another workspace refused with the identical NotFound; 16 passed with the bootstrap class), at 390 px the library row's Move and Pin sit in a sticky right column so Move ends inside the screen (`workspace-acceptance` 'Space placement dialog' asserts it). Line 4: real stack (project notrelix-fe-real-e2e, 2026-11-03, branch fix/workspaces-acceptance-gaps), `workspace-spaces.real.e2e.spec.ts` 2 of 2 passed.
- State: DONE — lines 1–4 proven in the lane pull request (2026-11-03, branch fix/workspaces-acceptance-gaps); see the Evidence line.

## L. Teams (added by this catalog)

### UC-WSP-L01 Create a Team
- Capability: L Teams (added by this catalog)
- Actors and permission: Owner, Admin (`ManageTeams`)
- Preconditions: Workspace Active.
- Input: `name`, `description`.
- Output: A Team (Active) inside the workspace.
- Main flow: 1. `POST /api/v1/workspaces/{id}/teams`. 2. Domain creates.
- No-op / rejection / concurrency / retry: Duplicate name: refused, case-insensitively, when another non-deleted Team of the Workspace has the name (`Workspaces_Team_DuplicateName`); the product doc does not state this rule (see the State line). A retry under the same Idempotency-Key returns the Team the first attempt made; a different body under that key is refused.
- UI states: Loading, success, validation error, permission denied, conflict, pending, offline, destructive confirmation.
- Rules: [workspaces](../../product/workspaces.md) sections 45, 78; WSP-014, WSP-025; WGREQ034; WX-TEAM-001.
- Acceptance: Given an Admin, When they create `Design`, Then the Team exists with no members.
- Evidence: BE `POST /api/v1/workspaces/{id}/teams`, `CreateTeamCommandHandler` · FE `create-team-dialog.tsx`, `use-team-mutations.ts` · Design `Final-Teams` · Tests `TeamTests`, `CreateTeamCommandHandlerTests`, `TeamEndpointTests`, `workspace-teams.mock.e2e.spec.ts` · FE `workspace-teams.mock.e2e.spec.ts` (7 passed: 6 owner, 1 member), component tests `workspace-teams.interaction` and `workspace-team.interaction` (24 passed), axe over the `workspace-teams` stories (28 of 28 passed, no critical or serious finding) · Design diff `design-fidelity-teams.mock.e2e.spec.ts` (10 of 10 passed: list, one Team, create dialog, archive confirm, member page, light and dark, 1 px, `e2e/support/artboards/teams.json`; the board is drawn at 1440 px only; 390 and 1024 px reviewed by screenshot, Vietnamese, light and dark) · Real e2e `workspace-teams.real.e2e.spec.ts` (real stack, project notrelix-fe-real-e2e, branch feat/workspace-teams-done, 2 of 2 passed: one Admin journey through the page and one rejection journey) · Lane run 2026-11-15: Domain `TeamTests` (name rules, case-insensitive duplicate), Application `CreateTeamCommandHandlerTests` (3 passed: success, workspace not found, duplicate refused), API `TeamEndpointTests` (17 passed: create, invalid name 400, unauthenticated 401), Integration `TeamWriteConcurrencyIntegrationTests` (5 passed, among them two creates of one name at the same time make one Team: the create now locks the workspace row until it commits), real e2e: create from the dialog; one Idempotency-Key twice returns the one Team and another body under it is refused; the same name in another case is refused with `Workspaces_Team_DuplicateName`; a Member's create is 403; a stranger and another workspace get 404.
- State: DONE — lines 1–4 proven in the lane pull request (2026-11-15, branch feat/workspace-teams-done); see the Evidence line.

### UC-WSP-L02 List and read Teams
- Capability: L Teams (added by this catalog)
- Actors and permission: Any non-Guest member (`ViewWorkspace`)
- Preconditions: Active membership.
- Input: `workspaceId`, optional `teamId`.
- Output: Team rows and one Team with its status and version.
- Main flow: 1. `GET .../teams`. 2. `GET .../teams/{teamId}`.
- No-op / rejection / concurrency / retry: The list is a cursor page (LD-06): ordered by name, id on ties, 50 a page by default, at most 200; archived Teams are included. Other workspace's Team: NotFound.
- UI states: Loading, empty, success, search.
- Rules: [workspaces](../../product/workspaces.md) sections 45, 46; WGREQ034.
- Acceptance: Given four Teams, Then four rows.
- Evidence: BE `GET .../teams`, `GET .../teams/{teamId}`, `GetWorkspaceTeamsQueryHandler`, `GetTeamQueryHandler` · FE `workspace-teams-surface.tsx`, `use-teams.ts` · Design `Final-Teams` · Tests `TeamEndpointTests`, `GetWorkspaceTeamsPagingTests`, `workspace-teams.mock.e2e.spec.ts` · FE `workspace-teams.mock.e2e.spec.ts` (7 passed: 6 owner, 1 member), component tests `workspace-teams.interaction` and `workspace-team.interaction` (24 passed), axe over the `workspace-teams` stories (28 of 28 passed, no critical or serious finding) · Design diff `design-fidelity-teams.mock.e2e.spec.ts` (10 of 10 passed: list, one Team, create dialog, archive confirm, member page, light and dark, 1 px, `e2e/support/artboards/teams.json`; the board is drawn at 1440 px only; 390 and 1024 px reviewed by screenshot, Vietnamese, light and dark) · Real e2e `workspace-teams.real.e2e.spec.ts` (real stack, project notrelix-fe-real-e2e, branch feat/workspace-teams-done, 2 of 2 passed: one Admin journey through the page and one rejection journey) · Lane run 2026-11-15: Application `GetWorkspaceTeamsPagingTests` (4 passed), API `TeamEndpointTests` (list, read), Integration `TeamAdministrationReadIntegrationTests` (4 passed: the read and the version agree, a Team of another workspace is NotFound, a non-member is denied), real e2e: the empty list, the list after create, a Member reads the Teams, a stranger and another workspace get the identical 404.
- State: DONE — lines 1–4 proven in the lane pull request (2026-11-15, branch feat/workspace-teams-done); see the Evidence line.

### UC-WSP-L03 List Team members
- Capability: L Teams (added by this catalog)
- Actors and permission: Any non-Guest member (`ViewWorkspace`)
- Preconditions: Active membership.
- Input: `teamId`.
- Output: Members with Team role (Lead, Member) and status.
- Main flow: 1. `GET .../teams/{teamId}/members`.
- No-op / rejection / concurrency / retry: Foreign Team: NotFound.
- UI states: Loading, empty, success.
- Rules: [workspaces](../../product/workspaces.md) sections 46, 47; WGREQ035.
- Acceptance: Given a Team with a Lead, Then the Lead is marked.
- Evidence: BE `GET .../teams/{teamId}/members`, `GetTeamMembersQueryHandler` · FE `workspace-team-surface.tsx` · Design `Final-Teams` · Tests `TeamAdministrationReadIntegrationTests` · FE `workspace-teams.mock.e2e.spec.ts` (7 passed: 6 owner, 1 member), component tests `workspace-teams.interaction` and `workspace-team.interaction` (24 passed), axe over the `workspace-teams` stories (28 of 28 passed, no critical or serious finding) · Design diff `design-fidelity-teams.mock.e2e.spec.ts` (10 of 10 passed: list, one Team, create dialog, archive confirm, member page, light and dark, 1 px, `e2e/support/artboards/teams.json`; the board is drawn at 1440 px only; 390 and 1024 px reviewed by screenshot, Vietnamese, light and dark) · Real e2e `workspace-teams.real.e2e.spec.ts` (real stack, project notrelix-fe-real-e2e, branch feat/workspace-teams-done, 2 of 2 passed: one Admin journey through the page and one rejection journey) · Lane run 2026-11-15: Application `GetTeamMembersQueryHandlerTests`, Integration `TeamAdministrationReadIntegrationTests` (4 passed: only active members listed, a foreign Team is NotFound, a non-member is denied), real e2e: the owner is listed as Lead and the other person as Member; a Member reads the people.
- State: DONE — lines 1–4 proven in the lane pull request (2026-11-15, branch feat/workspace-teams-done); see the Evidence line.

### UC-WSP-L04 Rename a Team or edit its description
- Capability: L Teams (added by this catalog)
- Actors and permission: Owner, Admin (`ManageTeams`)
- Preconditions: Team Active.
- Input: `name` or `description`, `expectedVersion`.
- Output: Updated Team, version +1.
- Main flow: 1. `PUT .../teams/{teamId}` or `PUT .../teams/{teamId}/description`. 2. Version check.
- No-op / rejection / concurrency / retry: Same value: no-op. Stale: 412. Archived Team: accepts no change.
- UI states: Inline edit, conflict, pending.
- Rules: [workspaces](../../product/workspaces.md) section 48.2; WGREQ036.
- Acceptance: Given a stale version, Then 412 and no change.
- Evidence: BE `PUT .../teams/{teamId}`, `PUT .../description`, `RenameTeamCommandHandler`, `UpdateTeamDescriptionCommandHandler` · FE `workspace-team-surface.tsx` (edit dialog), `use-team-mutations.ts` · Design `Final-Teams` · Tests `TeamVersionTests`, `TeamWriteConcurrencyIntegrationTests` · FE `workspace-teams.mock.e2e.spec.ts` (7 passed: 6 owner, 1 member), component tests `workspace-teams.interaction` and `workspace-team.interaction` (24 passed), axe over the `workspace-teams` stories (28 of 28 passed, no critical or serious finding) · Design diff `design-fidelity-teams.mock.e2e.spec.ts` (10 of 10 passed: list, one Team, create dialog, archive confirm, member page, light and dark, 1 px, `e2e/support/artboards/teams.json`; the board is drawn at 1440 px only; 390 and 1024 px reviewed by screenshot, Vietnamese, light and dark) · Real e2e `workspace-teams.real.e2e.spec.ts` (real stack, project notrelix-fe-real-e2e, branch feat/workspace-teams-done, 2 of 2 passed: one Admin journey through the page and one rejection journey) · Lane run 2026-11-15: Domain `TeamVersionTests` (6) and `TeamTests` (rename and description, no-op, archived refuses), Integration `TeamWriteConcurrencyIntegrationTests` (5 passed: a stale version is 412 and changes nothing), real e2e: the edit dialog renames and describes in one save (the two writes now run in turn, each on the version the one before left); the same name again is 204 and bumps no version; a stale version is 412 and the name stays; an archived Team refuses a rename. The stale answer is 412 (Precondition Failed), as everywhere in the API.
- State: DONE — lines 1–4 proven in the lane pull request (2026-11-15, branch feat/workspace-teams-done); see the Evidence line.

### UC-WSP-L05 Add a person to a Team
- Capability: L Teams (added by this catalog)
- Actors and permission: Owner, Admin (`ManageTeams`)
- Preconditions: Person is an active non-Guest member of the same workspace; Team Active.
- Input: `userId`, `role` (Member or Lead), `expectedVersion`.
- Output: Team membership; Team version +1.
- Main flow: 1. `POST .../teams/{teamId}/members`. 2. Version check. 3. Domain adds.
- No-op / rejection / concurrency / retry: Foreign or Guest user: refused. Duplicate: refused or no-op. Stale: 412.
- UI states: Add dialog, conflict, pending.
- Rules: [workspaces](../../product/workspaces.md) sections 46, 78; WSP-014; WGREQ035.
- Acceptance: Given a foreign user id, Then refused and nothing changes.
- Evidence: BE `POST .../teams/{teamId}/members`, `AddTeamMemberCommandHandler` · FE `workspace-team-surface.tsx`, `use-team-mutations.ts` · Design `Final-Teams` · Tests `TeamMemberTests`, `TeamRulesTests`, `TeamWriteConcurrencyIntegrationTests` · FE `workspace-teams.mock.e2e.spec.ts` (7 passed: 6 owner, 1 member), component tests `workspace-teams.interaction` and `workspace-team.interaction` (24 passed), axe over the `workspace-teams` stories (28 of 28 passed, no critical or serious finding) · Design diff `design-fidelity-teams.mock.e2e.spec.ts` (10 of 10 passed: list, one Team, create dialog, archive confirm, member page, light and dark, 1 px, `e2e/support/artboards/teams.json`; the board is drawn at 1440 px only; 390 and 1024 px reviewed by screenshot, Vietnamese, light and dark) · Real e2e `workspace-teams.real.e2e.spec.ts` (real stack, project notrelix-fe-real-e2e, branch feat/workspace-teams-done, 2 of 2 passed: one Admin journey through the page and one rejection journey) · Lane run 2026-11-15: Application `AddTeamMemberCommandHandlerTests` (4 passed, new: success, a Guest refused with `Workspaces_Team_GuestCannotJoin` and nothing changes, a non-member NotFound, another workspace NotFound), Domain `TeamTests` (a duplicate active member is a no-op) and `TeamRulesTests`, Integration `TeamWriteConcurrencyIntegrationTests` (5 passed), real e2e: add from the create dialog and from the Team page; a Guest is refused 400 and a stranger 404, the Team keeps its version.
- State: DONE — lines 1–4 proven in the lane pull request (2026-11-15, branch feat/workspace-teams-done); see the Evidence line.

### UC-WSP-L06 Change a Team member's role
- Capability: L Teams (added by this catalog)
- Actors and permission: Owner, Admin (`ManageTeams`)
- Preconditions: Member exists.
- Input: `userId`, `newRole`, `expectedVersion`.
- Output: New Team role. The last Lead cannot be downgraded.
- Main flow: 1. `PUT .../teams/{teamId}/members/{userId}/role`.
- No-op / rejection / concurrency / retry: Last Lead: refused (TeamLeadRules). Stale: 412.
- UI states: Role menu, reason when blocked.
- Rules: [workspaces](../../product/workspaces.md) section 47; WSP-014.
- Acceptance: Given one Lead, When lowered, Then refused.
- Evidence: BE `PUT .../members/{userId}/role`, `ChangeTeamMemberRoleCommandHandler` · FE `workspace-team-surface.tsx` (role menu), `use-team-mutations.ts` · Design `Final-Teams` · Tests `TeamRulesTests`, `TeamMemberTests` · FE `workspace-teams.mock.e2e.spec.ts` (7 passed: 6 owner, 1 member), component tests `workspace-teams.interaction` and `workspace-team.interaction` (24 passed), axe over the `workspace-teams` stories (28 of 28 passed, no critical or serious finding) · Design diff `design-fidelity-teams.mock.e2e.spec.ts` (10 of 10 passed: list, one Team, create dialog, archive confirm, member page, light and dark, 1 px, `e2e/support/artboards/teams.json`; the board is drawn at 1440 px only; 390 and 1024 px reviewed by screenshot, Vietnamese, light and dark) · Real e2e `workspace-teams.real.e2e.spec.ts` (real stack, project notrelix-fe-real-e2e, branch feat/workspace-teams-done, 2 of 2 passed: one Admin journey through the page and one rejection journey) · Lane run 2026-11-15: Domain `TeamRulesTests` (last Lead cannot be lowered) and `TeamMemberTests` (same role is a no-op), Application `ChangeTeamMemberRoleCommandHandlerTests` (3 passed), Integration `TeamWriteConcurrencyIntegrationTests` (`LastLead_CannotBeRemovedOrDemoted`), real e2e: the role menu makes a Member a Lead; lowering the only Lead is 400 and nothing changes.
- State: DONE — lines 1–4 proven in the lane pull request (2026-11-15, branch feat/workspace-teams-done); see the Evidence line.

### UC-WSP-L07 Remove a person from a Team
- Capability: L Teams (added by this catalog)
- Actors and permission: Owner, Admin (`ManageTeams`)
- Preconditions: Member exists.
- Input: `userId`, `expectedVersion`.
- Output: Membership Removed.
- Main flow: 1. `DELETE .../teams/{teamId}/members/{userId}?expectedVersion=`.
- No-op / rejection / concurrency / retry: Last Lead: refused. Stale: 412.
- UI states: Confirmation naming lost access.
- Rules: [workspaces](../../product/workspaces.md) section 46; GOV-026.
- Acceptance: Given a person with Team-based access, When removed, Then that access ends.
- Evidence: BE `DELETE .../members/{userId}`, `RemoveTeamMemberCommandHandler` · FE `workspace-team-surface.tsx`, `use-team-mutations.ts` · Design `Final-Teams` · Tests `TeamWriteConcurrencyIntegrationTests`, `workspace-teams.mock.e2e.spec.ts` · FE `workspace-teams.mock.e2e.spec.ts` (7 passed: 6 owner, 1 member), component tests `workspace-teams.interaction` and `workspace-team.interaction` (24 passed), axe over the `workspace-teams` stories (28 of 28 passed, no critical or serious finding) · Design diff `design-fidelity-teams.mock.e2e.spec.ts` (10 of 10 passed: list, one Team, create dialog, archive confirm, member page, light and dark, 1 px, `e2e/support/artboards/teams.json`; the board is drawn at 1440 px only; 390 and 1024 px reviewed by screenshot, Vietnamese, light and dark) · Real e2e `workspace-teams.real.e2e.spec.ts` (real stack, project notrelix-fe-real-e2e, branch feat/workspace-teams-done, 2 of 2 passed: one Admin journey through the page and one rejection journey) · Lane run 2026-11-15: Domain `TeamMemberTests` (removed member), Integration `TeamWriteConcurrencyIntegrationTests` (stale remove is 412 and the member stays Active; the last Lead cannot be removed), real e2e: Remove ends the membership while another Lead stays; removing the only Lead is 400 and nothing changes. The Team page removes inline as drawn on the board (the people card says the access ends with the membership); there is no confirm dialog.
- State: DONE — lines 1–4 proven in the lane pull request (2026-11-15, branch feat/workspace-teams-done); see the Evidence line.

### UC-WSP-L08 Archive or unarchive a Team
- Capability: L Teams (added by this catalog)
- Actors and permission: Owner, Admin (`ManageTeams`)
- Preconditions: Team exists.
- Input: `teamId`, `expectedVersion`.
- Output: Archived Team grants nothing and accepts no change; unarchive restores it.
- Main flow: 1. `POST .../teams/{teamId}/archive` or `.../unarchive`.
- No-op / rejection / concurrency / retry: Already in the state: no-op. Stale: 412. The row menu says who loses access first.
- UI states: Confirmation naming consequences, pending.
- Rules: [workspaces](../../product/workspaces.md) sections 44.3, 48.1; WSP-024, WSP-025; WGREQ036.
- Acceptance: Given a Team with a Space entry, When archived, Then its members lose that role.
- Evidence: BE `POST .../archive`, `POST .../unarchive`, `ArchiveTeamCommandHandler`, `UnarchiveTeamCommandHandler` · FE `workspace-team-surface.tsx` (archive dialog), `use-team-mutations.ts` · Design `Final-Teams` · Tests `TeamVersionTests`, `workspace-teams.mock.e2e.spec.ts` · FE `workspace-teams.mock.e2e.spec.ts` (7 passed: 6 owner, 1 member), component tests `workspace-teams.interaction` and `workspace-team.interaction` (24 passed), axe over the `workspace-teams` stories (28 of 28 passed, no critical or serious finding) · Design diff `design-fidelity-teams.mock.e2e.spec.ts` (10 of 10 passed: list, one Team, create dialog, archive confirm, member page, light and dark, 1 px, `e2e/support/artboards/teams.json`; the board is drawn at 1440 px only; 390 and 1024 px reviewed by screenshot, Vietnamese, light and dark) · Real e2e `workspace-teams.real.e2e.spec.ts` (real stack, project notrelix-fe-real-e2e, branch feat/workspace-teams-done, 2 of 2 passed: one Admin journey through the page and one rejection journey) · Lane run 2026-11-15: Domain `TeamVersionTests` (archive, unarchive, already-active is a no-op), Integration `TeamWriteConcurrencyIntegrationTests`, real e2e: Archive Team asks first and then archives, an archived Team refuses a rename (400), archiving an archived Team is 204 and bumps no version, Restore from the list returns it to the active list.
- State: DONE — lines 1–4 proven in the lane pull request (2026-11-15, branch feat/workspace-teams-done); see the Evidence line.

### UC-WSP-L09 Delete or restore a Team
- Capability: L Teams (added by this catalog)
- Actors and permission: Owner, Admin (`ManageTeams`)
- Preconditions: Team exists.
- Input: `teamId`, `expectedVersion` (delete).
- Output: Soft deletion or restore.
- Main flow: 1. `DELETE .../teams/{teamId}?expectedVersion=` or `POST .../teams/{teamId}/restore`.
- No-op / rejection / concurrency / retry: Restore has no expected version and is always allowed. Deleting a Team that Spaces still list is refused (409, `workspaces.team.still-referenced`) with the Spaces the caller can see and "and others you cannot see" without a number (LD-16, stricter answer); a stale version answers 412. Board grants of a Team are not checked yet.
- UI states: Confirmation, pending, refused (Spaces listed, others you cannot see), version changed (reload), denied, failed, deleted (toast with Restore).
- Rules: [workspaces](../../product/workspaces.md) section 48.1; WGREQ036.
- Acceptance: Given a deleted Team, When restored, Then its members return.
- Evidence: BE `DELETE .../teams/{teamId}`, `POST .../restore`, `DeleteTeamCommandHandler` (`RefuseWhenReferencedAsync`), `RestoreTeamCommandHandler` · FE `teams.service.ts` (`remove`, `restore`), `use-team-mutations.ts`, `delete-team-dialog.tsx`, `workspace-team-surface.tsx`, `parseTeamReferencesRefusal` (`errors.ts`), `teams.$teamId.tsx` · Design `Final-Teams`, `Wsp-Var-Teams-Refuse-T` (Workspaces canvas) · Tests `TeamDeleteReferencesIntegrationTests`, `TeamWriteConcurrencyIntegrationTests`, `TeamVersionTests`, `workspace-team.delete.component.test.tsx`, `team-references-refusal.unit.test.ts`, `space-team-delete-restore.unit.test.ts` · Real e2e none
- State: HARDENING — Built and tested 2026-11-19: delete refused with the code and a list limited to the Spaces the caller can see, an unreferenced Team deletes, restore returns its members (7 Integration tests, 41 Teams screen tests). Remaining: Board grants of a Team are not checked (dependency: a Governance read keyed by subject, named on UC-WSP-L10 for 0.2; today's reads are keyed by Board and would need N+1); a deleted Team is restorable only from the toast right after deleting (no deleted-Teams list read); no real e2e.

### UC-WSP-L10 Read the Teams administration view
- Capability: L Teams (added by this catalog)
- Actors and permission: Owner, Admin, Member (`ViewMembers`); Guest refused
- Preconditions: Active non-Guest membership.
- Input: `workspaceId`.
- Output: Per Team: people, who created and archived it, Spaces the caller can see where it holds a role, plus Owners, Admins and the people a Team may take.
- Main flow: 1. `GET /api/v1/workspaces/{id}/teams-ext`.
- No-op / rejection / concurrency / retry: Email only for Owner and Admin. Spaces the caller cannot see are omitted. Board grants of a Team are not listed yet.
- UI states: Loading, empty, success, permission denied.
- Rules: [workspaces](../../product/workspaces.md) section 48.2.
- Acceptance: Given a Team used in a Private Space the caller cannot see, Then that Space is not listed.
- Evidence: BE `GET /api/v1/workspaces/{id}/teams-ext`, `GetWorkspaceTeamsExtQueryHandler` · FE `use-teams-ext.ts`, `workspace-teams-surface.tsx` · Design `Final-Teams` · Tests `WorkspaceTeamsExtIntegrationTests`, `TeamAdministrationReadIntegrationTests`, `workspace-teams.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — Board grants are not listed in the Teams administration read (waits on the Governance read contract).

## M. Getting started and onboarding (added by this catalog)

### UC-WSP-M01 Save, read and clear my onboarding draft
- Capability: M Getting started and onboarding (added by this catalog)
- Actors and permission: The signed-in person (subject is always the caller)
- Preconditions: Signed in.
- Input: `step`, optional `workspaceId`, `data` (JSON object, max 64 KB, never interpreted, no secrets).
- Output: One draft per user, valid 30 days after the last save; an expired draft reads as absent.
- Main flow: 1. `PUT /api/v1/workspaces/onboarding-draft`. 2. `GET ...` on return. 3. `DELETE ...` when finished.
- No-op / rejection / concurrency / retry: A save replaces the draft. Delete of none: no-op. Oversize: 413 or 422. RLS repeats the user scope.
- UI states: Resume prompt, loading, none.
- Rules: [workspaces](../../product/workspaces.md) section 48.4; NRX-003.
- Acceptance: Given a draft saved yesterday, Then a read returns it. Given 31 days later, Then absent.
- Evidence: BE `GET`, `PUT`, `DELETE /api/v1/workspaces/onboarding-draft`, `SaveOnboardingDraftCommandHandler`, `GetOnboardingDraftQueryHandler`, `ClearOnboardingDraftCommandHandler` · FE `onboarding-api.ts` (feature package `web/onboarding`), `onboarding-page.tsx` · Design `Final-Onboarding` · Tests `OnboardingDraftTests`, `OnboardingIntegrationTests`, `apps/web` `onboarding-api.unit.test.ts` · Real e2e `workspace-onboarding.real.e2e.spec.ts`
- State: DONE — Backend: `OnboardingIntegrationTests` draft tests (saved, replaced, read and dropped per user; another user's draft invisible; 30-day expiry and an invalid save changes nothing), `OnboardingDraftTests` (Domain), read by name, green in the PR's `CI gate`. Frontend: `onboarding-api.unit.test.ts`. Real stack: `workspace-onboarding.real.e2e.spec.ts` journey 1 (2 passed, 17.7 s on the real backend: Save and exit keeps the draft with step and workspace, the next sign-in resumes at the departments step, the finished setup clears it). Design: the draft has no screen of its own: the header's `Lưu và thoát` is measured by `design-fidelity-onboarding.mock.e2e.spec.ts`, and a resumed setup shows the step page itself (gaps of those pages are listed under M04). Real e2e `workspace-onboarding.real.e2e.spec.ts`.

### UC-WSP-M02 Sweep expired onboarding drafts
- Capability: M Getting started and onboarding (added by this catalog)
- Actors and permission: System (hosted service)
- Preconditions: The sweep is enabled.
- Input: Options `Enabled`, `Interval` (1 h), `BatchSize` (500).
- Output: Expired rows are deleted in bounded batches.
- Main flow: 1. `OnboardingDraftSweepService` runs under the worker scope. 2. It deletes with `FOR UPDATE SKIP LOCKED`.
- No-op / rejection / concurrency / retry: Several instances are safe. It only reclaims storage; reads already hide expired drafts.
- UI states: None.
- Rules: [workspaces](../../product/workspaces.md) section 48.4.
- Acceptance: Given expired rows, When the sweep runs, Then they are gone and live ones stay.
- Evidence: BE `OnboardingDraftSweeper` in `OnboardingDraftSweep.cs`, registered in `BackgroundJobsRegistration.cs` · FE none (system use case) · Design none · Tests `OnboardingIntegrationTests` (draft sweep test) · Real e2e not applicable (system use case)
- State: DONE — Backend only (system use case): `OnboardingIntegrationTests` `DraftSweep_DeletesOnlyExpiredDrafts_InBatches_UnderTheWorkerScope` (expired rows gone, live ones stay, bounded batches, worker scope) and the options validator, read by name in the PR's `CI gate`. Frontend and design: not applicable, there is no UI. Real e2e: not applicable, a draft expires after 30 days and cannot be aged through the API; the integration test is the proof.

### UC-WSP-M03 Check that a workspace address is free
- Capability: M Getting started and onboarding (added by this catalog)
- Actors and permission: Account member with `CreateWorkspace`
- Preconditions: Account resolved.
- Input: `slug` (query).
- Output: `free`, `taken` (plus the next free address) or `invalid` (plus a rule code).
- Main flow: 1. `GET /api/v1/accounts/{accountId}/workspaces/slug-check?slug=`.
- No-op / rejection / concurrency / retry: Unique inside the Account only. It needs the create permission so it tells no other Account anything.
- UI states: Live hint under the field, loading, taken with suggestion, invalid with reason.
- Rules: [workspaces](../../product/workspaces.md) section 48.4; WSP-003.
- Acceptance: Given `acme` held in the Account, Then `taken` with `acme-2`.
- Evidence: BE `GET .../slug-check`, `CheckWorkspaceSlugQueryHandler` · FE `onboarding-api.ts`, `onboarding-page.tsx` (address step) · Design `Final-Onboarding` · Tests `OnboardingIntegrationTests`, `WorkspaceSlugAndProfileTests`, `onboarding.mock.e2e.spec.ts` · Real e2e `workspace-onboarding.real.e2e.spec.ts`
- State: DONE — Backend: `OnboardingIntegrationTests` slug-check tests (unique per Account only, next free address, deleted workspace frees its address, every reserved word invalid, length 3 to 63, an outsider refused and told nothing), `WorkspaceSlugAndProfileTests`. Frontend: the page now accepts 3 to 63 characters like the producer (it refused over 30). Design: `design-fidelity-onboarding.mock.e2e.spec.ts` (first page, 2 tests) and `design-fidelity-onboarding-states.mock.e2e.spec.ts` (`tao-workspace` and `dia-chi-trung`, light and dark, 4 passed: every board text run by its words within 1 px and every painted box and icon by place and size, named differences listed in the file). Real stack: `workspace-onboarding.real.e2e.spec.ts` journey 1 (free, taken with `-2`, reserved word invalid with its rule code, a 40-character address free; the page blocks Continue on a taken address, offers the next one and accepts the long one).

### UC-WSP-M04 Create the company workspace through the four onboarding steps
- Capability: M Getting started and onboarding (added by this catalog)
- Actors and permission: New Account member
- Preconditions: No workspace yet, or the person adds another.
- Input: Company name and address, size, language, invitees, first department.
- Output: A workspace (A01), departments (K01), invitations (G01) and the first Board by the owning contexts; the person lands on Home.
- Main flow: 1. Steps run in the app. 2. Each step calls the owning context's command. 3. The draft (M01) keeps progress. 4. Home shows the getting-started card.
- No-op / rejection / concurrency / retry: Onboarding creates nothing by itself and invents no demo data. A failed step keeps the earlier ones; resume from the draft.
- UI states: Step progress, per-step errors, resume, back.
- Rules: [workspaces](../../product/workspaces.md) sections 48.4, 74; DESIGN.md sections 65, 66.
- Acceptance: Given all four steps, Then a workspace, its departments and invitations exist and Home opens.
- Evidence: BE commands of A01, K01, G01 · FE `onboarding-surface.tsx`, `onboarding-steps.tsx`, `onboarding-page.tsx` · Design `Final-Onboarding` · Tests `onboarding.mock.e2e.spec.ts`, `design-fidelity-onboarding-states.mock.e2e.spec.ts`, `apps/web` `onboarding-api.unit.test.ts` (the Idempotency-Key kept on a retry after a lost answer, forgotten after a success or a refused row) · Real e2e `workspace-onboarding.real.e2e.spec.ts`
- State: DONE — lines 1–4 proven in the lane pull request (2026-11-16, branch feat/onboarding-screens-match-boards). Real stack: `workspace-onboarding.real.e2e.spec.ts` journey 1, 2 passed again after the screens changed (the four steps create the workspace, its Private departments, the invitation and the board, the draft is cleared). Design: all seven artboards match within 1 px in `design-fidelity-onboarding-states.mock.e2e.spec.ts` (`tao-workspace`, `dia-chi-trung`, `phong-ban`, `moi-nguoi`, `mau-board`, `xong`, `tham-gia`, light and dark, 14 passed; every board text run by its words and every painted box and icon by place and size, named differences listed in the file header). The departments are 44 px chips with the open colour picker, the lists and rows use the drawn type sizes (13.5 and 12.5), and the last page's summary rows and 0/3 list are laid out as drawn. 1024 and 390 px screenshots of every setup page, light and dark, in Vietnamese were reviewed: no sideways scroll, the invitation rows, template cards and footer stack on narrow screens.

### UC-WSP-M05 Join through an invitation and set my look and notifications
- Capability: M Getting started and onboarding (added by this catalog)
- Actors and permission: The invited person
- Preconditions: A valid invitation (H01 to H03).
- Input: Name, theme, notification preferences.
- Output: Membership plus preferences stored by Collaboration (one matrix per person, applying to every workspace; in-app and email).
- Main flow: 1. Join page. 2. Accept (H02 or H03). 3. Preferences are written through the Collaboration matrix.
- No-op / rejection / concurrency / retry: No preference contract of its own in Workspaces; Guests may set them.
- UI states: Join form, success.
- Rules: [workspaces](../../product/workspaces.md) section 48.4; see [collaboration](collaboration.md).
- Acceptance: Given a new member, Then one notification matrix per person exists, applying to every workspace.
- Evidence: BE notification preference endpoints (`MapNotificationPreferenceEndpoints.cs`) and the profile update · FE `onboarding-join-surface.tsx`, `welcome-page.tsx`, `onboarding-api.ts` `saveJoin` · Design `Final-Onboarding` · Tests `NotificationPreferencesIntegrationTests`, `onboarding.mock.e2e.spec.ts`, `design-fidelity-onboarding-states.mock.e2e.spec.ts` · Real e2e `workspace-onboarding.real.e2e.spec.ts`
- State: DONE — lines 1–4 proven in the lane pull request (2026-11-16, branch feat/onboarding-screens-match-boards). Real stack: `workspace-onboarding.real.e2e.spec.ts` journey 2, 2 passed again after the screens changed (an empty name is refused, the name reaches the profile and the mail choice the notification matrix, a second save carries the version of the first). Design: the `tham-gia` artboard matches within 1 px in `design-fidelity-onboarding-states.mock.e2e.spec.ts` (light and dark, 2 of the 14 passed; the mock world's access list is cut to the drawn department and its three boards, the person's name and the lead are set to the drawn words, named in the file header), with the 1024 and 390 px screenshots reviewed. The matrix is one per person for every workspace (owner decision 2026-11-15).

### UC-WSP-M06 See the getting-started card
- Capability: M Getting started and onboarding (added by this catalog)
- Actors and permission: Any member, Guest included (`ViewWorkspace`)
- Preconditions: Active membership.
- Input: `workspaceId`.
- Output: Derived items: `first-item` (done when a readable Board has an active Item; links the earliest Item's Board) and `invite-more` (done once any invitation was ever sent). `visible` unless dismissed or `first-item` done.
- Main flow: 1. `GET /api/v1/workspaces/{id}/getting-started`. 2. Items are derived from Board and invitation facts, never stored.
- No-op / rejection / concurrency / retry: The `mobile` item is not returned. Another workspace's invitations do not count. `invite-more` alone never hides the card.
- UI states: Card with progress, hidden, loading.
- Rules: [workspaces](../../product/workspaces.md) section 48.4; DESIGN.md section 66.
- Acceptance: Given one sent invitation, Then `invite-more` is done and the card stays.
- Evidence: BE `GET .../getting-started`, `GetGettingStartedQueryHandler` · FE `getting-started-card.tsx`, `home-getting-started.tsx` (apps/web), `workspace-home-surface.tsx` · Design `Final-Home`, `Final-Onboarding` · Tests `OnboardingIntegrationTests` · Real e2e `workspace-onboarding.real.e2e.spec.ts`
- State: DONE — Backend: `OnboardingIntegrationTests` getting-started tests (first item from readable Boards, `invite-more` once any invitation was sent, never the mobile item, hidden when dismissed or done, outsider refused), read by name. Frontend: the card on Home. Design: Final-Home draws no card, so it is an undrawn state on design-system tokens: `getting-started-card.mock.e2e.spec.ts` (6 passed: 1440, 1024 and 390, light and dark, in Vietnamese; fits the page, no sideways scroll, keyboard focus, hide button a one-line 44 px target on a phone) with the six screenshots reviewed. Real stack: `workspace-onboarding.real.e2e.spec.ts` journey 1 (the card after the setup: `first-item` not done, `invite-more` done; an outsider gets 404). Two defects fixed: the hide button wrapped to three lines at 390 px, and a sent invitation still read "Nobody has been invited yet".

### UC-WSP-M07 Dismiss the getting-started card
- Capability: M Getting started and onboarding (added by this catalog)
- Actors and permission: Any member (`ViewWorkspace`)
- Preconditions: Active membership.
- Input: `workspaceId`.
- Output: A dismissal stored per user and workspace.
- Main flow: 1. `POST .../getting-started/dismiss`.
- No-op / rejection / concurrency / retry: Dismissing again is a no-op.
- UI states: Close button, pending.
- Rules: [workspaces](../../product/workspaces.md) section 48.4.
- Acceptance: Given a dismissal, Then `visible` is false for that user only.
- Evidence: BE `POST .../getting-started/dismiss`, `DismissGettingStartedCommandHandler` · FE `getting-started-card.tsx` (`onHide`), `home-getting-started.tsx` (dismiss mutation) · Design `Final-Home` · Tests `OnboardingIntegrationTests` · Real e2e `workspace-onboarding.real.e2e.spec.ts`
- State: DONE — Backend: `OnboardingIntegrationTests` dismissal tests (per user and workspace, repeating is a no-op), read by name. Frontend: the hide button on the card. Design: undrawn state, same review as M06 (`getting-started-card.mock.e2e.spec.ts`, 6 passed). Real stack: `workspace-onboarding.real.e2e.spec.ts` journey 1 (hide, hidden after a reload, a repeat answers 204, a second member still sees the card).

## N. Home, navigation, search and activity (added by this catalog)

### UC-WSP-N01 Open workspace Home
- Capability: N Home, navigation, search and activity (added by this catalog)
- Actors and permission: Any member (Guest sees less)
- Preconditions: Active membership.
- Input: none.
- Output: Greeting, due windows and quick filters (Work Management feed), Continue (recents), pinned items, getting-started card.
- Main flow: 1. The route reads recents, pins and the Work Management feed. 2. The surface composes them.
- No-op / rejection / concurrency / retry: Approvals, mentions and the schedule have no producer yet and are not shown. A section that fails shows its own error.
- UI states: Loading per section, empty, success, partial error.
- Rules: [workspaces](../../product/workspaces.md) sections 85, 33; WX-HOME-001, WX-HOME-002.
- Acceptance: Given overdue items, Then the counts match My work.
- Evidence: BE reads of N04, N05, M06 and Work Management feeds · FE `workspace-home-surface.tsx`, route `home.tsx` · Design `Final-Home`, `Final-M-Home` · Tests `workspace-home.interaction.component.test.tsx` (counts test), `workspace-home.pinned-guest.component.test.tsx` (pinned list, Guest, phone; 54 pass across the Home folder), `home.mock.e2e.spec.ts`, `design-fidelity-home.mock.e2e.spec.ts`, `real-pages-guard.mock.e2e.spec.ts` · Real e2e none
- State: DONE — Proven by tests that ran in this PR (54 pass across the Home folder). `workspace-home.interaction.component.test.tsx`: the render; `FUI[workspace.home:counts-match-my-work]`, the open and overdue counts match My work's; stable loading skeletons per section; a retry per failed section; a partial error keeps the loaded sections; all-done; onboarding in a new workspace; no Continue card when the member has opened nothing; item, section and quick-filter navigation; completing an item inline. New `workspace-home.pinned-guest.component.test.tsx` (6 pass): `FUI[workspace.home:pinned]` the pinned list draws each pinned Board and Doc with where it lives and no opened-at time, and opens it; `pinned-empty` draws no card and keeps Continue; `pinned-error` shows a retry and keeps the other sections; `guest-sees-less` a Guest in a new workspace gets no create or invite action and no member, seat or invitation figure; `guest-unreadable-pins` a pin or a recent of a Board the Guest cannot read has no catalog match and is never drawn; `phone` every section stays reachable at 390 px with no inline width past the screen. The host (`home.tsx`) joins the pins to the same authorized catalog as the recents. Decision: the card shows the pin count, not a cap, because the producer's cap is 50 (N05) and the board `Wsp-Var-Home-Pin-T` draws 4 / 8. Not proven: pixel layout of the phone Home (no layout engine in the component tests), and the native `MobileWorkspaceHome` placeholder, which is the mobile app's scope.

### UC-WSP-N02 Show workspace figures to Owner and Admin (enterprise baseline)
- Capability: N Home, navigation, search and activity (added by this catalog)
- Actors and permission: Owner, Admin
- Preconditions: Active Owner or Admin.
- Input: `workspaceId`.
- Output: Active members, visible Spaces, Teams, pending invitations, archived flag.
- Main flow: 1. A query reads counts. 2. The settings or members header shows them.
- No-op / rejection / concurrency / retry: Today `GetWorkspaceDashboardQuery` exists with no endpoint and counts members of every status (Removed and Suspended too). It is dead code.
- UI states: Loading, success.
- Rules: [workspaces](../../product/workspaces.md) section 85; Part 4 LD-14.
- Acceptance: Given one removed member, Then the member count excludes them.
- Evidence: BE `GetWorkspaceDashboardQueryHandler` (no route) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Fix the count, add the route or delete the query; screen.

### UC-WSP-N03 Read workspace activity
- Capability: N Home, navigation, search and activity (added by this catalog)
- Actors and permission: Any member (`ViewWorkspace`)
- Preconditions: Active membership.
- Input: `page`, `pageSize`.
- Output: Today an empty list with the requested paging.
- Main flow: 1. `GET /api/v1/workspaces/{id}/activity`. 2. The handler returns a constant empty page.
- No-op / rejection / concurrency / retry: Collaboration owns activity (section 81); Workspaces must not invent a second feed (LD-15).
- UI states: Empty state until Collaboration serves it.
- Rules: [workspaces](../../product/workspaces.md) section 81; see [collaboration](collaboration.md).
- Acceptance: Given workspace changes, Then the feed lists them with actors (when built).
- Evidence: BE `GET /api/v1/workspaces/{id}/activity`, `GetWorkspaceActivityQueryHandler` (stub) · FE `activity.service.ts`, `use-workspace-activity.ts` · Design none · Tests none · Real e2e none
- State: IMPLEMENTING — Deferred to 0.2 (lead ruling 2026-11-19). Dependency: the Collaboration activity port; the workspace activity read comes from Collaboration through a port that does not exist yet, so the stub stays and Workspaces builds no second feed.

### UC-WSP-N04 Remember recently opened resources
- Capability: N Home, navigation, search and activity (added by this catalog)
- Actors and permission: Any member (`ViewWorkspace`)
- Preconditions: Active membership.
- Input: `kind`, `resourceId` (`Idempotency-Key`).
- Output: A recent entry per member and resource; only the newest 30 are kept.
- Main flow: 1. `POST /api/v1/workspaces/{id}/recents`. 2. `GET /api/v1/workspaces/{id}/recents` lists them newest first.
- No-op / rejection / concurrency / retry: A repeat updates the time. Kinds are an allowed set. Entries the caller can no longer read must not be shown, and a Board or Doc the caller cannot read is not recorded (NotFound).
- UI states: Continue list, empty.
- Rules: [workspaces](../../product/workspaces.md) sections 33 to 35; DESIGN.md section 27.
- Acceptance: Given 31 opened resources, Then 30 remain.
- Evidence: BE `POST` and `GET .../recents`, `RecordRecentResourceCommandHandler`, `GetRecentResourcesQueryHandler` · FE `navigation.service.ts`, `use-workspace-navigation.ts` · Design `Final-Home`, `Final-Command` · Tests `WorkspaceNavigationTests`, `WorkspaceNavigationHandlerTests` (a revoked Board and Page leave the recents; a Board or Page the caller cannot read is refused), `WorkspaceNavigationIntegrationTests`, `WorkspaceNavigationReadAccessIntegrationTests`, `ArchivedWorkspaceSettingsAndNavigationIntegrationTests`, `workspace-navigation-state.component.test.tsx`, `workspace-home.interaction.component.test.tsx`, `command-palette.mock.e2e.spec.ts` · Real e2e none
- State: DONE — Proven by tests that ran in this PR. Domain `WorkspaceNavigationTests` (supported kinds, the newer open moves the time and an older one does not, eviction beyond 30 with a deterministic tie-break; Domain run: 32 pass with the settings tests). Application `WorkspaceNavigationHandlerTests` (27 pass in the Navigation folder: a new entry per member, the same resource again updates the time without a duplicate, the 31st evicts the oldest, another workspace, another account or an unknown id is NotFound, a Board or Page the caller cannot read is NotFound and stores nothing, an archived Board or trashed Page the caller may read still works, newest first and per member, a revoked Board and Page leave the list on the next read, unsupported kinds refused). Integration `WorkspaceNavigationIntegrationTests` (per member, the 30 newest, the same key replays as a no-op, a non-member is denied, a revoked Board leaves the recents), `WorkspaceNavigationReadAccessIntegrationTests` (a Private Board the member cannot read, or a Board never shared with a Guest, is NotFound on record and pin and nothing is stored; withdrawing a Guest's share empties both lists at once) and `ArchivedWorkspaceSettingsAndNavigationIntegrationTests` (recording stays allowed in an archived workspace), in the Workspaces shard (573 pass; Auth and Workspaces shard exit 0). Frontend `workspace-navigation-state.component.test.tsx` (a late answer of the previous workspace never shows; a failed record is swallowed and each open has its own idempotency key; a recent recorded while the first list read is in flight shows up) and `workspace-home.interaction.component.test.tsx` (the Continue list, no Continue card when empty, a retry for a failed section). Defect fixed with a failing-first test: recording or pinning a Private Board the member could not read answered success, so its existence leaked and a pin counted against the cap (red: `Expected a <Notrelix.Application.Common.Exceptions.NotFoundException> to be thrown because a Guest cannot read a Board nobody shared with them, but no exception was thrown.`).

### UC-WSP-N05 Pin and unpin resources
- Capability: N Home, navigation, search and activity (added by this catalog)
- Actors and permission: Any member (`ViewWorkspace`)
- Preconditions: Active membership.
- Input: `kind`, `resourceId`.
- Output: A personal pin; at most 50 per member per workspace.
- Main flow: 1. `PUT /api/v1/workspaces/{id}/pins/{kind}/{resourceId}`. 2. `DELETE` the same path. 3. `GET /api/v1/workspaces/{id}/pins` lists.
- No-op / rejection / concurrency / retry: Pin of a pinned resource and unpin of an unpinned one are no-ops. The 51st pin: `Workspaces_Navigation_PinLimitReached`.
- UI states: Pin toggle, limit message.
- Rules: [workspaces](../../product/workspaces.md) sections 83, 85.
- Acceptance: Given 50 pins, When a 51st is added, Then refused.
- Evidence: BE `PUT`, `DELETE`, `GET .../pins`, `PinResourceCommandHandler`, `UnpinResourceCommandHandler`, `GetPinnedResourcesQueryHandler` · FE `use-workspace-navigation-mutations.ts`, sidebar · Design `Final-Home`, `BC-Shell-1440` · Tests `WorkspaceNavigationTests`, `WorkspaceNavigationHandlerTests`, `WorkspaceNavigationIntegrationTests`, `WorkspaceNavigationReadAccessIntegrationTests`, `ArchivedWorkspaceSettingsAndNavigationIntegrationTests`, `workspace-navigation-state.component.test.tsx`, `failure-toast.unit.test.ts` · Real e2e none
- State: HARDENING — Proven by tests that ran in this PR: Domain `WorkspaceNavigationTests` (the 50th pin is refused with `Workspaces_Navigation_PinLimitReached`); Application `WorkspaceNavigationHandlerTests` (pin, pin again keeps the first time, over the cap refused, the cap counts the member only, another workspace NotFound, a Board or Page the caller cannot read NotFound, unpin removes only the member's own pin, unpin of an unpinned resource is a no-op and needs no live resource, newest first, a hidden reference leaves the list); Integration `WorkspaceNavigationIntegrationTests` (the cap of 50, two concurrent pins at the boundary admit one, repeats are no-ops), `WorkspaceNavigationReadAccessIntegrationTests` and `ArchivedWorkspaceSettingsAndNavigationIntegrationTests` (pin and unpin stay allowed in an archived workspace), in the Workspaces shard (573 pass; Auth and Workspaces shard exit 0); Frontend `workspace-navigation-state.component.test.tsx` (an optimistic pin rolls back when the producer rejects it) and `failure-toast.unit.test.ts` (the refusal of the 51st pin reaches the member in the producer's words). Remaining gaps: (1) a pin of a Board or Page the member can no longer read stays stored and counts against the 50, but the list hides it, so the member cannot unpin it; whether a read or a pin prunes such rows is a product ruling; (2) the limit message is the generic failure toast with the producer's text, with no drawn limit state, and the Library page's pin toggle has no component test.

### UC-WSP-N06 Search the workspace from the command palette
- Capability: N Home, navigation, search and activity (added by this catalog)
- Actors and permission: Any member (`ViewWorkspace`); results filtered by Governance
- Preconditions: Active membership.
- Input: `q`.
- Output: Boards (5), Docs (5) and Items (6) the caller can read, with counts.
- Main flow: 1. `GET /api/v1/workspaces/{id}/command-search?q=`. 2. Each context answers through its authorized search port.
- No-op / rejection / concurrency / retry: Never returns what the caller cannot read ([ADR-010](../../../backend/docs/decisions/ADR-010-authorized-command-search.md)). Short or empty query returns commands only on the client.
- UI states: Loading, no match, results, filters.
- Rules: [workspaces](../../product/workspaces.md) section 55; DESIGN.md section 26.
- Acceptance: Given a Private Board outside the caller's reach, Then it is not found.
- Evidence: BE `GET .../command-search`, `CommandSearchQueryHandler` · FE `command-palette-surface.tsx`, `use-command-search.ts` · Design `Final-Command`, `Final-M-Search` · Tests `CommandSearchQueryHandlerTests`, `CommandSearchVisibilityIntegrationTests`, `ArchivedWorkspaceSettingsAndNavigationIntegrationTests`, `CommandSearchCrossTenantTests` (Rls, application role, HTTP: tenant B with tenant A's workspace id gets the made-up-id answer and no Board title), `command-palette.mock.e2e.spec.ts`, `design-fidelity-command.mock.e2e.spec.ts` · Real e2e none
- State: HARDENING — Proven by tests that ran in this PR: Integration `CommandSearchVisibilityIntegrationTests` (a Private Board outside the caller's reach is neither listed nor counted and its Items are not returned, while the owner finds it; a Guest finds nothing until a Board is shared with them, then only that Board; a Board shared afterwards appears on the next search; a non-member gets NotFound; a null, empty or one-letter query answers empty) and `ArchivedWorkspaceSettingsAndNavigationIntegrationTests` (search works in an archived workspace), in the Workspaces shard (573 pass; Auth and Workspaces shard exit 0); `CommandSearchCrossTenantTests` ran in the work-RLS shard (622 pass with the Rls folder); Application `CommandSearchQueryHandlerTests` (short text asks no context, only the readable ids are asked, nothing readable asks nobody, names and Spaces are disclosed only when visible); Frontend `workspace-command.interaction.component.test.tsx` (17 pass: recent, search with counts, filters by click and Tab, arrows and Enter, commands, people, empty, dismiss), `use-command-search.unit.test.ts` and the new `workspace-command.search-states.component.test.tsx` (9 pass: `FUI[workspace.command-palette:loading]` skeleton rows while the search is in flight and no no-match line, local rows stay usable while it loads, `error` a failed search says so with a retry and never reads as no match, a failed search keeps the local rows, no-match still reads as no match, and on the phone sheet `Final-M-Search` the results with chips and count and Cancel, no match, loading and error) and `use-command-search.status.component.test.tsx` (3 pass: the hook is loading, then ready; error with data null, and retry asks again; a short word or a command prefix asks nothing). The failed-search state is drawn on `Wsp-Var-Cmd-Error-T` (Workspaces canvas, added in this PR); the loading state is `Wsp-Var-Cmd-Loading-T`. Remaining gap: Docs trimmed by permission are proven through the Page search adapter and the handler, not through a private Page on PostgreSQL.

# 4. Product decisions applied

Owner decisions this catalog relies on:

- Product decision (owner, 2026-10-26): the dev seed makes `admin@` own the seeded workspaces and seeds Spaces, and an idempotent Development-only repair heals the existing dev database without a reseed. Used by every real end-to-end spec that signs in as a seeded workspace owner (I01).
- Product decision (owner, 2026-10-30): a guest invitation to a single Board is a Workspaces resource invitation; its UI is the Board's Share dialog. Used by G07 and G08.

Lead decisions of this catalog (2026-10-30). Each is written as a rule in Part 5.

- Lead decision (2026-10-30), LD-01: workspace creation and invitation creation take an idempotency key, like `RecordRecentResource`.
- Lead decision (2026-10-30), LD-02: an archived workspace is read-only for every member and keeps its history. It is hidden from normal lists but listed to its Owners and Admins in an "Archived" group, where Owner and Admin can unarchive it. Other members see an archived banner when they open it by link.
- Lead decision (2026-10-30), LD-03: creating a workspace asks Billing for a slot and fails closed (503, retry) when Billing cannot answer.
- Lead decision (2026-10-30), LD-04: a second invite to an email with a pending invitation answers 409 with a stable code `workspaces.invitation.pending-exists`; a resend is allowed at most once per minute per invitation; reads treat a pending invitation past its expiry as Expired and a sweep writes the status.
- Lead decision (2026-10-30), LD-05: only an active Owner transfers ownership, only to an active non-Guest member; the former Owner becomes Admin (as built). Several Owners may exist.
- Lead decision (2026-10-30), LD-06: lists that grow with the workspace (members, invitations, Teams, Team members, Space members) page by cursor, default 50, at most 200; the Space tree returns whole up to 2,000 Spaces and then refuses with a clear error until paging is built. `GET /api/v1/workspaces` (B04) and `GET /api/v1/accounts/{accountId}/workspaces` (B03) stay as producers; the account page keeps using `GET /api/v1/account/workspaces` (I02). No endpoint is removed in this pass.
- Lead decision (2026-10-30), LD-07: every Workspaces command that overwrites an existing value carries the expected version; creation and set-membership commands do not.
- Lead decision (2026-10-30), LD-08: the Billing seat gate is mandatory and fails closed for invitation acceptance, direct add and member activation.
- Lead decision (2026-10-30), LD-09: adding a member directly requires the person to be an active member of the same Account; anyone else is invited.
- Lead decision (2026-10-30), LD-10: Owner and Admin may change the workspace address; the old address is released at once and not redirected; links by id keep working.
- Lead decision (2026-10-30), LD-11: `EnforceMfa` leaves workspace settings when the owner decides (J04); until then the control stays hidden.
- Lead decision (2026-10-30), LD-12: removal and leave delete the person's Space and Team entries in the same transaction; a Team left without a Lead is flagged "needs a lead" and the last-Lead rule applies only to explicit role changes.
- Lead decision (2026-10-30), LD-13: bulk invite takes at most 50 emails; each is handled on its own and the answer lists each outcome.
- Lead decision (2026-10-30), LD-14: the dead dashboard query is fixed (count only active members) and shown to Owner and Admin in a header, or deleted if no artboard asks for it.
- Lead decision (2026-10-30), LD-15: Workspaces serves no activity of its own; the activity route returns Collaboration's feed through a port, and the web hides the block until it does.
- Lead decision (2026-10-30), LD-16: deleted Spaces and Teams stay restorable for 30 days, the window already used for Board and item trash, then are purged by a job.

# 5. Canonical updates required

For `docs/product/workspaces.md`:

1. Section 6 and 60: "An archived workspace is read-only for every member and keeps its history. Owners and Admins see it in an Archived group of their workspace list and may unarchive it. Participant contexts reject writes with an archived reason and pause automation and integration sync."
2. Section 19.1: replace "A Workspace that ... is archived or deleted never appears" with "an archived workspace appears only to its Owners and Admins, in an Archived group".
3. Sections 61 to 63: record the delete rule decided 2026-11-01 (Owner only, typed-name confirmation, 30-day recoverable window with export and restore, then a purge with an audit fact); state that today no purge exists.
4. Section 73: "Workspace creation and invitation creation are idempotent requests keyed by the client's idempotency key. The same key and body return the first result; the same key with another body is refused."
5. Section 70: "Creating a workspace takes a Billing slot and fails closed when Billing cannot answer."
6. Section 26: "A second invitation for an email with a pending one answers 409 `workspaces.invitation.pending-exists`. A resend replaces the token at most once a minute. A pending invitation past its expiry reads as Expired."
7. Section 17: "Only an active Owner transfers ownership, to an active non-Guest member; the former Owner becomes Admin."
8. Section 71: "Every command that overwrites an existing Workspaces value carries the expected version."
9. Section 22.1: "Billing's seat gate is mandatory and fails closed for acceptance, direct add and activation. Direct add needs an active Account membership of the person."
10. Section 19: add "removal and leave delete the person's Space and Team entries in the same transaction; a Team without a Lead is flagged".
11. Section 40: either move `EnforceMfa` to its Account or Identity owner or name its enforcer.
12. Section 48.2: state that workspace invitation lists need `InviteMember` (Owner, Admin), and lists page by cursor (default 50, max 200).
13. Section 43.2: add the 30-day restore window for deleted Spaces and Teams.
14. Section 81: "Workspaces serves no activity feed; Collaboration does."
15. A new section "Bulk invitation": at most 50 emails per request, per-row results.

For `docs/workstreams/teams/workspace-governance.md` section 4: add capabilities Spaces, Teams, Getting started and onboarding, and Home, navigation, search and activity to the Workspaces list.

For `docs/product/governance.md`: none beyond the permissions named in use cases (`ManageSpaces`, `ManageTeams`, `ManageMemberProfile`, `InviteMember`, `RemoveMember`, `ChangeMemberRole`, `ManageWorkspace`).

# 6. Not covered

| Product-doc section | Reason |
|---|---|
| 1, 2, 87 to 96 (mission, non-ownership, service extraction, source alignment, change impact, stop conditions, final rule) | Orientation and review rules, not behaviour. They are inputs to the Rules lines. |
| 5, 56, 69 and 16 (explicit scope, scope survives boundaries, stale switch) | Cross-cutting; proven in I01, I04 and in every use case's Rules. |
| 15, 16 and 30 (role vocabulary, role is not permission, Governance relation) | Governance owns role and permission meaning; see [governance.md](governance.md). |
| 28 (Identity relation) | Identity owns password, MFA, sessions, OAuth; see [identity.md](identity.md). |
| 31, 32 and 70 (Billing relation) | Billing owns entitlement; the gates are A03, F01, F04, H02. |
| 33 to 38 (Work Management, Documents, Collaboration, Automation, Integrations, Analytics relations) | Owned by those contexts; Workspaces supplies facts (K15, I04). |
| 57, 58 (workspace facts and events) | Emitted facts are listed in each Output line; no consumer-facing catalog is needed. |
| 59 (Account lifecycle effects) | D06, DEFINED (decided 2026-11-01). |
| 80 (audit) | Governance owns audit evidence; see [governance.md](governance.md). |
| 81 (activity) | N03 records the stub; Collaboration owns the feed. |
| 82 (privacy) | Applied as rules inside E01, G03, K02, K14, L10. |
| 87 (service extraction) | Architecture, not a use case. |
| 63 (restore details) | D04, HARDENING (restore window 30 days, Owner only; owner decision 2026-11-01). |
| 21, 22 (suspend member, guests) | F03 and G07. Guest vocabulary stays in Governance. |
| Reassigning a removed person's work (enterprise baseline) | Work Management owns assignment; request it in [work-management.md](work-management.md). |
| "Suspended" as a workspace state (section 6) | The product says "where product defines them"; none is defined, only Active and Archived exist. |

Endpoints with no use case: none. `GetWorkspaceDashboardQuery` and `ProvisionPersonalWorkspaceCommand` have no route; they appear in N02 and A02.

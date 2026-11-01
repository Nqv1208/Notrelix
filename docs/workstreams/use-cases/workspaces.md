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

Product meaning: [workspaces](../../product/workspaces.md). Team: [workspace-governance](../teams/workspace-governance.md). Execution packs: [governance spec](../executions/workspace-governance/workspace-governance.spec.md), [governance tests](../executions/workspace-governance/workspace-governance.tests.md), [governance certification](../executions/workspace-governance/workspace-governance.certification.md), [workspace experience plan](../executions/workspace-experience/workspace-experience.plan.md).

Roles, permissions, policy, audit and the admin plane belong to Governance: see [governance.md](governance.md) and [governance product doc](../../product/governance.md). This file names the permission each use case needs and does not redefine it.

Requirement ids cited below are `WGREQ001` to `WGREQ043` (Workspace core, membership, invitations, Teams, Spaces) and a few later ones of the governance spec, plus `WX-*` ids of the experience plan. The product rules are `WSP-001` to `WSP-026`; `NRX-*` ids are repository rules. The word "section" always means a section of the Workspaces product doc.

# 1. Assessment

## 1.1 Capability map

Letters A to J follow the Workspaces list in the team spec (section 4, items 1 to 10). The team spec has no capability for Spaces, Teams, onboarding or Home, although the product doc and the API own them; letters K to N are added by this catalog and Part 5 asks the team spec to adopt them.

| Letter | Capability | Use cases | DEFINED | IMPLEMENTING | INTEGRATING | HARDENING | DONE | BLOCKED |
|---|---|---|---|---|---|---|---|---|
| A | Workspace create | 4 | 1 | 0 | 3 | 0 | 0 | 0 |
| B | Workspace read / resolve | 4 | 0 | 2 | 0 | 2 | 0 | 0 |
| C | Workspace update | 2 | 1 | 0 | 0 | 1 | 0 | 0 |
| D | Workspace lifecycle transition | 6 | 1 | 2 | 1 | 2 | 0 | 0 |
| E | Membership read | 2 | 0 | 0 | 0 | 2 | 0 | 0 |
| F | Membership add / remove / change | 10 | 1 | 2 | 4 | 2 | 0 | 1 |
| G | Invitation create | 9 | 1 | 3 | 0 | 5 | 0 | 0 |
| H | Invitation accept / reject / revoke | 6 | 0 | 0 | 1 | 5 | 0 | 0 |
| I | Workspace selection / switch integration | 6 | 0 | 1 | 2 | 3 | 0 | 0 |
| J | Workspace settings | 4 | 0 | 0 | 3 | 0 | 0 | 1 |
| K | Spaces (added by this catalog) | 15 | 0 | 1 | 11 | 3 | 0 | 0 |
| L | Teams (added by this catalog) | 10 | 0 | 1 | 9 | 0 | 0 | 0 |
| M | Getting started and onboarding (added by this catalog) | 7 | 0 | 0 | 7 | 0 | 0 | 0 |
| N | Home, navigation, search and activity (added by this catalog) | 6 | 1 | 1 | 4 | 0 | 0 | 0 |
| | Total | 91 | 6 | 13 | 45 | 25 | 0 | 2 |

No use case is DONE: none has a recorded owner check or a 1-px design diff. Twenty-five use cases are HARDENING because a real spec runs them against the real backend: C01 (settings), D03 and D04 (`workspace-delete-restore.real.e2e.spec.ts`, delete, gone for a member, restore), E01 and E02 (members read), I01 (`workspace-isolation.real.e2e.spec.ts`, a switch), H02, H05, H06 (`workspace-invitations-answer.real.e2e.spec.ts`, the invitee declines, is invited again and accepts from the rail list), F02 and F05 (`workspace-member-changes.real.e2e.spec.ts`, role change and removal), G01 and G03 to G06 (`workspace-invitations-manage.real.e2e.spec.ts`, invite, list, re-role, resend, revoke), H01 and H03 (`workspace-invitation-link.real.e2e.spec.ts`, the e-mail link and the join in one step), B01, B04, I02 and I04 (`workspace-read-and-lists.real.e2e.spec.ts`, reads, both lists and the realtime scope) and K01, K02, K15 (`workspace-spaces.real.e2e.spec.ts`, create, read and place). Every other use case that the web app calls is INTEGRATING: the generated client reaches the real producer (`frontend/tooling/contracts/enabled-consumer-surface.ts`), mock end-to-end specs under `frontend/e2e/mock` exercise the screens, but no real spec does. Use cases with a producer and no frontend caller are IMPLEMENTING.

The geometry specs (`design-fidelity-*.mock.e2e.spec.ts`) cover Shell, Home, Members, Space and Command at 1440 px and some narrower sizes. No Workspaces screen has a recorded diff at 1440, 1024 and 390 px in both themes, so Design is never DONE.

## 1.2 The ten most important gaps

1. **Deletion is built, but its downstream reaction is not.** Delete is Owner-only with a typed-name confirmation, a deleted workspace is NotFound to everyone, it is restorable by its Owner for 30 days from Recently deleted, and a purge job erases its data after that and writes an audit fact (D03, D04, owner decision 2026-11-01). Still open: no workspace export exists, the other contexts only have stub consumers for the lifecycle events (`WorkspacesStubConsumers`), provider-side integration cleanup and open realtime sockets are not handled.
2. **Archive is a dead end in the UI.** An archived workspace leaves every list, nothing calls `UnarchiveWorkspace`, and the settings copy says every member loses access while the product says archive keeps history readable (section 60).
3. **Competing writes without an expected version.** Member role, suspend, activate and remove; invitation role, resend and revoke; Space rename, description, type, visibility, archive, delete and restore; Team restore. Two admins can silently overwrite each other (NRX-011).
4. **No retry safety on creation.** `CreateWorkspace` and `InviteMember` are not idempotent requests; a double submit creates `acme` and `acme-3f9a1c`, or a second invitation attempt fails with unstable text.
5. **Unbounded reads.** Members, invitations, Spaces, Space tree, Teams and Team members have no paging or cap, and are read in full by the shell.
6. **Entitlement and account checks are missing or fail open.** `AddMember` now requires an active Account membership (`Workspaces_Member_NotInAccount`) and a seat (`Workspaces_Member_SeatUnavailable`) (fixed for direct add); the seat gate on acceptance is an optional dependency (`IBillingSeatGate? = null`); creation has no plan limit (WSP-004, WSP-011). Gap: seat checks on direct add and on acceptance do not serialize admissions, so two concurrent admissions can exceed the seat limit by one; a separate use case.
7. **Invitations are readable by Guests.** Fixed: `GetWorkspaceInvitations` needs `ViewMembers`, so a Guest gets 403 (section 82). Members still see invited emails; the list stays unbounded.
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
| Invitation list readable by Guests (fixed: `ViewMembers`) | `backend/src/Notrelix.Application/Features/Workspaces/Invitations/Queries/GetWorkspaceInvitations/GetWorkspaceInvitations.cs` |
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
| UC-WSP-A01 | Create a workspace in an Account | A | Account member with `CreateWorkspace` | yes | yes | Final-Onboarding | none | INTEGRATING | Tests and owner check; plan limit (A03). |
| UC-WSP-A02 | Provision the personal workspace at registration | A | System | yes | n/a | none | none | INTEGRATING | Tests and owner check: real e2e that registers and reaches the workspace; partial-failure proof (WGREQ158). |
| UC-WSP-A03 | Stop creation at the plan limit (enterprise baseline) | A | Account member with `CreateWorkspace` | no | no | Final-Billing | none | DEFINED | Billing workspace-count entitlement; gate call; error contract; screen state. |
| UC-WSP-A04 | Make workspace creation retry-safe | A | Account member with `CreateWorkspace` | yes | yes | none | none | INTEGRATING | Tests and owner check. |
| UC-WSP-B01 | Read one workspace | B | Any active member, Guest included | yes | yes | BC-Shell-1440 | workspace-read-and-lists.real.e2e.spec.ts | HARDENING | Owner check; design diff at 3 widths and both themes; the archived and deleted read are not driven in a browser. |
| UC-WSP-B02 | Open a workspace by its address | B | Anyone | yes | no | none | none | IMPLEMENTING | No frontend caller; rate-limit and enumeration test; vanity URL route. |
| UC-WSP-B03 | List the workspaces of an Account | B | Account member with `ViewWorkspace` on the Account scope | yes | no | none | none | IMPLEMENTING | No caller; overlaps with I02 and B04; membership-filter proof. |
| UC-WSP-B04 | List every workspace I belong to (user scope) | B | Any signed-in user | yes | yes | none | workspace-read-and-lists.real.e2e.spec.ts | HARDENING | Owner check; duplicate of I02 with fewer fields, decide to keep one (Part 4 LD-06); no design artboard of its own. |
| UC-WSP-C01 | Edit workspace name and description | C | Owner, Admin | yes | yes | none for workspace settings (`Final-Settings` is the Board settings page) | none | HARDENING | Tests and owner check; real e2e; an approved workspace-settings artboard. |
| UC-WSP-C02 | Change the workspace address (enterprise baseline) | C | Owner, Admin | no | no | none | none | DEFINED | Command, endpoint, event, settings field. |
| UC-WSP-D01 | Archive a workspace | D | Owner, Admin | yes | yes | Final-Settings | none | INTEGRATING | UI copy says every member loses access, the product says read-only (fix copy); participant contexts only have stub consumers (`WorkspacesStubConsumers`); real e2e. |
| UC-WSP-D02 | Unarchive a workspace | D | Owner, Admin | yes | no | none | none | IMPLEMENTING | Frontend entry for an archived workspace (it vanishes from lists); banner; copy. |
| UC-WSP-D03 | Delete a workspace | D | Owner | yes | yes | none (no workspace danger-zone artboard; built from the archive pattern of the same page) | workspace-delete-restore.real.e2e.spec.ts | HARDENING | Owner check; an approved artboard; no workspace export exists (left out); no downstream reaction in the other contexts; open realtime sockets are not closed. |
| UC-WSP-D04 | Restore a deleted workspace | D | Owner | yes | yes | none (the Recently deleted list reuses the archived-workspaces pattern) | workspace-delete-restore.real.e2e.spec.ts | HARDENING | Owner check; an approved artboard; a person with no workspace left cannot reach the list (account page lives inside a workspace). |
| UC-WSP-D05 | Transfer workspace ownership | D | Current Owner | yes | no | none | none | IMPLEMENTING | Screen (members page or settings); artboard; step-up for the Owner. |
| UC-WSP-D06 | React to Account suspension or closure | D | System | no | no | Final-States | none | DEFINED | decided 2026-11-01, not built (owner decision 7, §9 of the README): A suspended Account is read-only end to end: automation runs and outbound webhooks are skipped and recorded with reason account.suspended, public forms and share-link writes refuse, reads stay open, nothing is deleted. The effect of Closed is not covered by this decision. |
| UC-WSP-E01 | List workspace members | E | Owner, Admin, Member | yes | yes | none for the roster (`Final-Members`, `Final-M-Members` are the department Members tab) | workspace-members.real.e2e.spec.ts | HARDENING | Owner check; an approved roster artboard and its diff at 3 widths and both themes; identical NotFound for a non-member (pipeline-wide, see Part 1.3). |
| UC-WSP-E02 | Read my own membership | E | Any active member, Guest included | yes | yes | none | workspace-members.real.e2e.spec.ts | HARDENING | Owner check; identical NotFound for a non-member (pipeline-wide, see Part 1.3). |
| UC-WSP-F01 | Add an existing user as a member directly | F | Owner, Admin | yes | no | none | none | IMPLEMENTING | Kept for admin use; no screen. Account-membership and seat checks are merged; tests. |
| UC-WSP-F02 | Change a member's workspace role | F | Owner, Admin | yes | yes | Final-Members | workspace-member-changes.real.e2e.spec.ts | HARDENING | Owner check; an approved roster artboard (none) and its diff at 3 widths and both themes; conflict and offline states driven in a browser; axe and keyboard run. |
| UC-WSP-F03 | Suspend a member | F | Owner, Admin | yes | yes | Final-Members | none | INTEGRATING | Tests and owner check: expected version, stale 412, no-op; real e2e. |
| UC-WSP-F04 | Activate a suspended member | F | Owner, Admin | yes | yes | Final-Members | none | INTEGRATING | Tests and owner check: seat refusal, expected version, no-op; real e2e. |
| UC-WSP-F05 | Remove a member | F | Owner, Admin | yes | yes | Final-Members | workspace-member-changes.real.e2e.spec.ts | HARDENING | Owner check; roster artboard and diff (none drawn); conflict and offline states in a browser; axe and keyboard run; Space and Team entries cleanup is F09 (DEFINED, not built). |
| UC-WSP-F06 | Leave a workspace | F | Any active member, Guest included | yes | yes | Final-Account | none | INTEGRATING | Tests and owner check: real e2e. A sole owner has no screen to hand ownership over (D05, IMPLEMENTING); Space and Team cleanup is F09 (DEFINED, not built). |
| UC-WSP-F07 | Set my own job title | F | Any active member | yes | partial | none | none | INTEGRATING | Tests and owner check: real e2e; no screen artboard. Nothing found to build. |
| UC-WSP-F08 | Set another member's job title | F | Owner, Admin | yes | no | none | none | IMPLEMENTING | No frontend caller; artboard. |
| UC-WSP-F09 | Clean a person's Space and Team entries when they leave (enterprise baseline) | F | System | no | no | none | none | DEFINED | Transaction step, event, 'needs a lead' state. |
| UC-WSP-F10 | Provision members from an identity directory (SCIM) | F | System principal of Accounts | no | no | none | none | BLOCKED | Decision: system principal for provisioning and its permission. |
| UC-WSP-G01 | Invite a person to the workspace by email | G | Owner, Admin | yes | yes | Final-Members | workspace-invitations-manage.real.e2e.spec.ts | HARDENING | Owner check; a roster/invite artboard diff (Final-Members is the department tab); invitation e-mail delivery to a real provider (the real stack proves the development mail log only); axe and keyboard run. |
| UC-WSP-G02 | Invite with initial Team and Space entries | G | Owner, Admin | yes | no | Final-Members | none | IMPLEMENTING | No frontend field for entries. |
| UC-WSP-G03 | List workspace invitations | G | Owner, Admin, Member | yes | yes | Final-Members | workspace-invitations-manage.real.e2e.spec.ts | HARDENING | Owner check; owner question: may Members see invited emails (they can today); the issuer is not in the row; expired-sweep not built; design diff. |
| UC-WSP-G04 | Change a pending invitation's role | G | Owner, Admin | yes | yes | Final-Members | workspace-invitations-manage.real.e2e.spec.ts | HARDENING | Owner check; conflict and offline states in a browser; design diff; axe and keyboard run. |
| UC-WSP-G05 | Resend an invitation | G | Owner, Admin | yes | yes | Final-Members | workspace-invitations-manage.real.e2e.spec.ts | HARDENING | Owner check; a resend still uses a fixed 7-day expiry, not the workspace setting (a J question); design diff. |
| UC-WSP-G06 | Revoke an invitation | G | Owner, Admin | yes | yes | Final-Members | workspace-invitations-manage.real.e2e.spec.ts | HARDENING | Owner check; conflict and offline states in a browser; design diff; axe and keyboard run. |
| UC-WSP-G07 | Invite a guest to one Board or Page | G | Holder of `ManageBoardPermission` or `ManagePagePermission` on the resource | yes | no | Final-Share, Final-M-Share | none | IMPLEMENTING | Email invitation in the Share dialog (owner decision); real e2e; `AllowGuestInvites` proof. |
| UC-WSP-G08 | Resend or revoke a resource invitation | G | The issuer with the permission on that resource | yes | no | Final-Share | none | IMPLEMENTING | No frontend caller. |
| UC-WSP-G09 | Invite many people at once (enterprise baseline) | G | Owner, Admin | no | partial | Final-Onboarding | none | DEFINED | Batch contract; result UI. |
| UC-WSP-H01 | Preview an invitation from its link | H | Anyone holding the token | yes | yes | Final-Onboarding, Final-Auth | workspace-invitation-link.real.e2e.spec.ts | HARDENING | Owner check; rate-limit proof (the real stack raises the credential limit to 1000); design diff of the join page (Final-Onboarding, Final-Auth) for the workspace wording. |
| UC-WSP-H02 | Accept an invitation while signed in | H | The invited person | yes | yes | Final-Onboarding | workspace-invitations-answer.real.e2e.spec.ts | HARDENING | Owner check; repeat accept and seat refusal are not driven in a browser |
| UC-WSP-H03 | Accept an invitation and register in one step | H | A new person holding the token | yes | yes | Final-Onboarding | workspace-invitation-link.real.e2e.spec.ts | HARDENING | Owner check; the partial-failure rollback is proven by backend tests only; design diff of the join page; axe and keyboard run. |
| UC-WSP-H04 | Decline an invitation from its link | H | Anyone holding the token | yes | yes | Final-Onboarding | none | INTEGRATING | Tests and owner check: real e2e. Nothing found to build. |
| UC-WSP-H05 | Decline an invitation I see in my account | H | The invited person, signed in | yes | yes | none | workspace-invitations-answer.real.e2e.spec.ts | HARDENING | Owner check; no artboard |
| UC-WSP-H06 | See my pending invitations | H | Any signed-in person | yes | yes | none | workspace-invitations-answer.real.e2e.spec.ts | HARDENING | Owner check; no artboard for the flyout |
| UC-WSP-I01 | Switch the active workspace | I | Any member of both workspaces | n/a | yes | BC-Shell-1440, BC-Shell-1280-768, BC-Shell-320 | workspace-isolation.real.e2e.spec.ts | HARDENING | Tests and owner check: out-of-order response test; realtime dispose proof; design diff at 3 widths and both themes. |
| UC-WSP-I02 | List the workspaces I belong to | I | Any signed-in person | yes | yes | BC-Shell-1440, Final-Account | workspace-read-and-lists.real.e2e.spec.ts | HARDENING | Owner check; the Archived group has no artboard and no way to open an archived workspace yet (banner is I05/D02); design diff. |
| UC-WSP-I03 | Choose my default workspace | I | Any active member | yes | yes | Final-Account | none | INTEGRATING | Tests and owner check: real e2e; sign-in open check. |
| UC-WSP-I04 | Authorize a realtime subscription to a workspace | I | System | yes | yes | none | workspace-read-and-lists.real.e2e.spec.ts | HARDENING | Owner check; the application socket (not a raw test socket) is not asserted to resubscribe after a revoke; design none. |
| UC-WSP-I05 | See why a workspace is unavailable | I | Any signed-in person | partial | partial | Final-States | none | IMPLEMENTING | Archived banner with action; plan-limited state; real e2e. |
| UC-WSP-I06 | Always see which workspace I am in | I | Any member | n/a | yes | BC-Shell-1440, BC-Shell-320 | none | INTEGRATING | Tests and owner check: real e2e; Space delete confirm and the two unused Team confirm texts still do not name the workspace. |
| UC-WSP-J01 | Read workspace settings | J | Any active member | yes | yes | Final-Settings | none | INTEGRATING | Real e2e; design diff. |
| UC-WSP-J02 | Change workspace settings | J | Owner, Admin | yes | yes | Final-Settings | none | INTEGRATING | Real e2e; design diff; audit of the change (Governance). |
| UC-WSP-J03 | Read sharing and security extras | J | Owner, Admin, Member | yes | yes | Final-Settings, Final-Teams | none | INTEGRATING | Real e2e. |
| UC-WSP-J04 | Keep Account-wide policy out of workspace settings | J | Owner, Admin | yes | no | none | none | BLOCKED | Decision: owner and enforcement of the MFA policy. |
| UC-WSP-K01 | Create a Space (department) | K | Owner, Admin | yes | yes | Final-Space, Final-M-Space | workspace-spaces.real.e2e.spec.ts | HARDENING | Owner check; design diff of the dialog and tree at 3 widths and both themes (Final-Space, Final-M-Space); axe and keyboard run. |
| UC-WSP-K02 | Read the Space tree and list Spaces | K | Any non-Guest member | yes | yes | Final-Space | workspace-spaces.real.e2e.spec.ts | HARDENING | Owner check; windowing of a very large tree in the nav is not built; design diff. |
| UC-WSP-K03 | Read one Space | K | Any member who can see the Space | yes | yes | Final-Space | none | INTEGRATING | Real e2e. |
| UC-WSP-K04 | Rename a Space or edit its description | K | Owner, Admin, Space Admin | yes | yes | Final-Space | none | INTEGRATING | Expected version; real e2e. |
| UC-WSP-K05 | Change a Space's type | K | Owner, Admin, Space Admin | yes | yes | Final-Space | none | INTEGRATING | Expected version; real e2e. |
| UC-WSP-K06 | Change a Space's visibility | K | Owner, Admin, Space Admin | yes | yes | Final-Space | none | INTEGRATING | Expected version; real e2e. |
| UC-WSP-K07 | Move or reorder a Space | K | Owner, Admin, Space Admin | yes | partial | Final-Space | none | INTEGRATING | Frontend tree drag; real e2e. |
| UC-WSP-K08 | Archive or unarchive a Space | K | Owner, Admin, Space Admin | yes | yes | Final-Space | none | INTEGRATING | Expected version; real e2e. |
| UC-WSP-K09 | Delete or restore a Space | K | Owner, Admin, Space Admin | yes | no | Final-Space | none | IMPLEMENTING | Frontend calls; trash and retention rule; expected version. |
| UC-WSP-K10 | See who is in a Space | K | Owner, Admin, Space members | yes | yes | Final-Space, Final-M-Space | none | INTEGRATING | Pagination for large Teams; real e2e; design diff. |
| UC-WSP-K11 | Add a person or Team to a Space | K | Owner, Admin, Space Admin | yes | yes | Final-Space | none | INTEGRATING | Real e2e; design diff. |
| UC-WSP-K12 | Change a Space member's role | K | Owner, Admin, Space Admin | yes | yes | Final-Space | none | INTEGRATING | Real e2e. |
| UC-WSP-K13 | Remove a person or Team from a Space | K | Owner, Admin, Space Admin | yes | yes | Final-Space | none | INTEGRATING | Real e2e. |
| UC-WSP-K14 | Open the department page overview | K | Owner, Admin, Space members | yes | yes | Final-Space, Final-M-Space | none | INTEGRATING | Real e2e; design diff; scale test for large Spaces. |
| UC-WSP-K15 | Place a Board or Page in a Space (facts only) | K | Work Management and Documents | yes | yes | Final-Space | workspace-spaces.real.e2e.spec.ts | HARDENING | Owner check; the Board-side placement dialog and a Doc placement are not driven in a browser (the real stack drives the API); design diff. |
| UC-WSP-L01 | Create a Team | L | Owner, Admin | yes | yes | Final-Teams | none | INTEGRATING | Name rule; real e2e; design diff. |
| UC-WSP-L02 | List and read Teams | L | Any non-Guest member | yes | yes | Final-Teams | none | INTEGRATING | Pagination (LD-06); real e2e. |
| UC-WSP-L03 | List Team members | L | Any non-Guest member | yes | yes | Final-Teams | none | INTEGRATING | Real e2e. |
| UC-WSP-L04 | Rename a Team or edit its description | L | Owner, Admin | yes | yes | Final-Teams | none | INTEGRATING | Real e2e. |
| UC-WSP-L05 | Add a person to a Team | L | Owner, Admin | yes | yes | Final-Teams | none | INTEGRATING | Real e2e. |
| UC-WSP-L06 | Change a Team member's role | L | Owner, Admin | yes | yes | Final-Teams | none | INTEGRATING | Real e2e. |
| UC-WSP-L07 | Remove a person from a Team | L | Owner, Admin | yes | yes | Final-Teams | none | INTEGRATING | Real e2e. |
| UC-WSP-L08 | Archive or unarchive a Team | L | Owner, Admin | yes | yes | Final-Teams | none | INTEGRATING | Real e2e. |
| UC-WSP-L09 | Delete or restore a Team | L | Owner, Admin | yes | no | Final-Teams | none | IMPLEMENTING | Frontend calls; reference rule; expected version on restore. |
| UC-WSP-L10 | Read the Teams administration view | L | Owner, Admin, Member | yes | yes | Final-Teams | none | INTEGRATING | Board usage; real e2e; design diff. |
| UC-WSP-M01 | Save, read and clear my onboarding draft | M | The signed-in person | yes | yes | Final-Onboarding | none | INTEGRATING | Real e2e; the onboarding API sits in the web app route folder, not the feature package (move it). |
| UC-WSP-M02 | Sweep expired onboarding drafts | M | System | yes | n/a | none | none | INTEGRATING | No real e2e; it is a system use case. |
| UC-WSP-M03 | Check that a workspace address is free | M | Account member with `CreateWorkspace` | yes | yes | Final-Onboarding | none | INTEGRATING | Real e2e. |
| UC-WSP-M04 | Create the company workspace through the four onboarding steps | M | New Account member | yes | yes | Final-Onboarding | none | INTEGRATING | Real e2e of the full run; retry of a step (A04). |
| UC-WSP-M05 | Join through an invitation and set my look and notifications | M | The invited person | yes | yes | Final-Onboarding | none | INTEGRATING | Real e2e. |
| UC-WSP-M06 | See the getting-started card | M | Any member, Guest included | yes | yes | Final-Home, Final-Onboarding | none | INTEGRATING | Real e2e. |
| UC-WSP-M07 | Dismiss the getting-started card | M | Any member | yes | yes | Final-Home | none | INTEGRATING | Real e2e. |
| UC-WSP-N01 | Open workspace Home | N | Any member | partial | yes | Final-Home, Final-M-Home | none | INTEGRATING | Real e2e; design diff; sections without producers. |
| UC-WSP-N02 | Show workspace figures to Owner and Admin (enterprise baseline) | N | Owner, Admin | partial | no | none | none | DEFINED | Fix the count, add the route or delete the query; screen. |
| UC-WSP-N03 | Read workspace activity | N | Any member | partial | partial | none | none | IMPLEMENTING | Real source from Collaboration through a port. |
| UC-WSP-N04 | Remember recently opened resources | N | Any member | yes | yes | Final-Home, Final-Command | none | INTEGRATING | Readability filter proof; real e2e. |
| UC-WSP-N05 | Pin and unpin resources | N | Any member | yes | yes | Final-Home, BC-Shell-1440 | none | INTEGRATING | Real e2e. |
| UC-WSP-N06 | Search the workspace from the command palette | N | Any member | yes | yes | Final-Command, Final-M-Search | none | INTEGRATING | Real e2e; cross-tenant negative at HTTP; design diff. |

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
- Evidence: BE `POST /api/v1/accounts/{accountId}/workspaces`, `CreateWorkspaceCommandHandler`, `WorkspaceFactory.CreateWithOwner` · FE `create-workspace-dialog.tsx`, onboarding step 'workspace', `workspace.service.ts` · Design `Final-Onboarding`, `BC-Shell-1440` (rail Add) · Built (no tests written or run yet): the create is idempotent and the validator matches the domain limits (name 160, description 1024); the dialog sends no client-derived slug (the server derives a free address), keeps one key per request and says why a create was refused (offline, no permission, email not confirmed, name too long); onboarding keeps its key while the same request is retried; the mock backend derives the address and replays by key · Tests existing, to be re-run and extended with an `Idempotency-Key`: `CreateWorkspaceCommandHandlerTests`, `WorkspaceCreationPipelineAuthorizationTests`, `WorkspaceCreatedOutboxEvidenceTests`, `WorkspaceSlugAndProfileTests`, `CreateWorkspaceEndpointTests`, `WorkspaceAuthTests`, `WorkspaceLifecycleTests`, `RlsRuntimeEnforcementTests`, `PipelineTelemetryIntegrationTests` · Real e2e none
- State: INTEGRATING — Tests and owner check (success, rejection, duplicate under one key, validation limits, real e2e); plan limit (A03).

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
- Evidence: BE `ProvisionPersonalWorkspaceCommand`, `WorkspaceProvisioningConsumer` · FE none · Design none · Tests `WorkspaceProvisioningConsumerTests`, `RegistrationWorkspaceRuntimeChainIntegrationTests` · Real e2e none
- State: INTEGRATING — Tests and owner check: real e2e that registers and reaches the workspace; partial-failure proof (WGREQ158). Nothing to build in this unit.

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
- Evidence: BE `CreateWorkspaceCommand` is an `IIdempotentRequest` (`workspaces.workspace.create-workspace.v1`), the endpoint requires the key (`.WithIdempotencyKey()`), the partition is the Account · FE `use-create-workspace.ts` holds one key per input and reuses it on retry; `onboarding-api.ts` does the same · Design none · Tests none written yet · Real e2e none
- State: INTEGRATING — Tests and owner check (the duplicate test, mismatch refusal).

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
- Evidence: BE `GET /api/v1/workspaces/{workspaceId}`, `GetWorkspaceQueryHandler` · FE `use-workspace.ts`, `workspace.service.ts` · Design `BC-Shell-1440` · Tests `WorkspaceQueryTests`, `WorkspaceAuthTests`, `WorkspaceScopeTests` · Real e2e workspace-read-and-lists.real.e2e.spec.ts
- State: HARDENING — Runs on the real stack (`workspace-read-and-lists.real.e2e.spec.ts`): an active member and a Guest read the workspace; a signed-in stranger and a removed member get 404 with the same body shape as a made-up id. Left for DONE: Owner check; design diff of the shell at 3 widths and both themes; an archived workspace read and a deleted one driven in a browser. Acceptance 2026-11-02 at `6ac912d0c` (independent acceptance agent, branch accept/batch-1), stays HARDENING. Line 4 holds: `workspace-read-and-lists.real.e2e.spec.ts` 2 passed. Line 3 gap: the shell home at 1440, 1024 and 390 in light and dark was opened (no overflow, axe clean) but At 390 px the four shell top-bar icon buttons (navigation 32, command palette 32, theme 32, notifications 36) are under 44 px on every workspace screen. `design-fidelity-shell.mock.e2e.spec.ts` 17 passed and 1 failed (`:226`, rail pinned: the all-workspace entries sit 75 px lower than drawn). An archived and a deleted workspace read are not driven in a browser. Line 1: the named backend classes in the Domain, Application, API and Integration projects ran and passed (their cases were not audited one by one against the line); the Architecture, Platform and Infrastructure projects were not run, so `WorkspaceScopedArchitectureTests`, `SendInvitationEmailConsumerTests` and `SuspendMemberCommandHandlerTests` are unrun where an Evidence line names them. Backend runs of this acceptance: Domain 200, Application 150 and API 97 (filtered to the classes this use case names, 0 failed) and Postgres Integration 229 (0 failed).

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
- Evidence: BE `GET /api/v1/accounts/{accountId}/resolve`, `ResolveSlugQueryHandler`, `IWorkspaceSlugResolver` · FE none (routes use ids) · Design none · Tests `WorkspaceRouteTests` (Accounts), `AccountWorkspaceEndpointTests` · Real e2e none
- State: IMPLEMENTING — No frontend caller; rate-limit and enumeration test; vanity URL route.

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
- Evidence: BE `GET /api/v1/workspaces`, `GetUserWorkspacesQueryHandler` · FE `use-workspace-list.ts` (the workspace switcher and the scope boundary read this list) · Design none · Tests `GetUserWorkspacesQueryHandlerTests` · Real e2e workspace-read-and-lists.real.e2e.spec.ts
- State: HARDENING — Runs on the real stack (`workspace-read-and-lists.real.e2e.spec.ts`): the workspace switcher and the scope boundary read this list; a member sees the workspace in it, a stranger never does, and a removed member loses it at once. Left for DONE: Owner check; decide to keep one of B04 and I02 (Part 4 LD-06); the left-a-workspace and archived exclusions are proven by backend tests only. Acceptance 2026-11-02 at `6ac912d0c` (independent acceptance agent, branch accept/batch-1), stays HARDENING. Line 4 holds: `workspace-read-and-lists.real.e2e.spec.ts` 2 passed. Line 3 gap: At 390 px the four shell top-bar icon buttons (navigation 32, command palette 32, theme 32, notifications 36) are under 44 px on every workspace screen. The Part 4 LD-06 decision (keep B04 or I02) is still open and the left-a-workspace and archived exclusions are proven by backend tests only. Line 1: the named backend classes in the Domain, Application, API and Integration projects ran and passed (their cases were not audited one by one against the line); the Architecture, Platform and Infrastructure projects were not run, so `WorkspaceScopedArchitectureTests`, `SendInvitationEmailConsumerTests` and `SuspendMemberCommandHandlerTests` are unrun where an Evidence line names them. Backend runs of this acceptance: Domain 200, Application 150 and API 97 (filtered to the classes this use case names, 0 failed) and Postgres Integration 229 (0 failed).

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
- Evidence: BE `PATCH /api/v1/workspaces/{id}/profile`, `UpdateWorkspaceProfileCommandHandler`, `Workspace.UpdateProfile`, `UpdateWorkspaceProfileCommandValidator` (name 160) · FE settings route `settings.tsx`, `workspace-settings-surface.tsx` (`ProfileForm`: pristine and dirty, validation, saving, saved, conflict with Reload, read-only for people who cannot manage, inline refusal and offline text), `use-workspace-settings-mutations.ts` · Design none for workspace settings; `Final-Settings` is the Board settings page · Tests `WorkspaceSlugAndProfileTests`, `WorkspaceVersionTests`, `WorkspaceMutationTests`, `WorkspaceAdministrationReadIntegrationTests` (stale version), `workspace-pages.mock.e2e.spec.ts` (WX-SET-001); none yet for `UpdateProfile` (one version step) · Real e2e none
- State: HARDENING — Tests (the single version step, the 160 cap) and owner check; real e2e; an approved workspace-settings artboard (none exists).

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
- Actors and permission: Owner, Admin (`ManageWorkspace`)
- Preconditions: Workspace Active; caller holds the current version.
- Input: `expectedVersion`.
- Output: Status Archived, version +1, `WorkspaceArchived`. Data is kept; new mutation is blocked; history stays readable.
- Main flow: 1. `POST /api/v1/workspaces/{id}/archive`. 2. Domain sets Archived. 3. The fact reaches participant contexts, which pause automation and integration sync.
- No-op / rejection / concurrency / retry: Already archived: no-op. Deleted: rejected. Stale version: 409. Writes inside an archived workspace are rejected by the owning contexts (WM, Documents) with an archived reason.
- UI states: Destructive confirmation (type the name), pending, success, conflict, permission denied.
- Rules: [workspaces](../../product/workspaces.md) section 60; WGREQ005, WGREQ006; Part 4 LD-02.
- Acceptance: Given an Active workspace, When Owner archives it with the right version, Then status is Archived and members can still read it. Given a Member, Then 403.
- Evidence: BE `POST /api/v1/workspaces/{id}/archive`, `ArchiveWorkspaceCommandHandler` · FE settings 'Danger zone' (`workspace-settings-surface.tsx`), `use-workspace-settings-mutations.ts` · Design `Final-Settings` · Tests `WorkspaceTests`, `WorkspaceMutationOrderingTests`, `WorkspaceLifecycleTests` · Real e2e none
- State: INTEGRATING — UI copy says every member loses access, the product says read-only (fix copy); participant contexts only have stub consumers (`WorkspacesStubConsumers`); real e2e.

### UC-WSP-D02 Unarchive a workspace
- Capability: D Workspace lifecycle transition
- Actors and permission: Owner, Admin (`ManageWorkspace`)
- Preconditions: Workspace Archived; caller can reach it (LD-02).
- Input: `expectedVersion`.
- Output: Status Active, version +1, `WorkspaceUnarchived`; paused automation and sync resume.
- Main flow: 1. `POST /api/v1/workspaces/{id}/archive/unarchive`. 2. Domain sets Active. 3. Fact published.
- No-op / rejection / concurrency / retry: Already Active: no-op. Deleted: rejected. Stale version: 409.
- UI states: Archived banner with an Unarchive action for Owner and Admin; pending; success.
- Rules: [workspaces](../../product/workspaces.md) sections 60, 63; WGREQ005; Part 4 LD-02.
- Acceptance: Given an Archived workspace, When an Owner unarchives, Then members write again. Given a Member, Then the action is absent and the API returns 403.
- Evidence: BE `POST /api/v1/workspaces/{id}/archive/unarchive`, `UnarchiveWorkspaceCommandHandler` · FE none (the settings copy promises 'an owner can restore it' but nothing calls it) · Design none · Tests `WorkspaceTests` · Real e2e none
- State: IMPLEMENTING — Frontend entry for an archived workspace (it vanishes from lists); banner; copy.

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
- Evidence: BE `DELETE /api/v1/workspaces/{id}`, `DeleteWorkspaceCommandHandler`, `Workspace.Delete` and `RestoreWindow`, `AccessPolicyEngine` (Owner-only rule and the deleted-workspace NotFound), `028_deleted_workspaces.sql` (`ops.is_workspace_owner`, the narrowed `workspace.workspaces` policies), `DeletedWorkspacePurger` (the 30-day purge) · FE `workspace-settings-surface.tsx` (`DangerFlow`), `settings.tsx`, `use-workspace-settings-mutations.ts` (`remove`) · Design none: no workspace danger-zone artboard exists; built from the archive card of the same page · Tests `WorkspaceDeletionAtomicityTests` (personal, window, version, no-op), `DeleteWorkspaceCommandHandlerTests`, `DeletedWorkspaceAccessTests` (Application, every role and scope), `DeletedWorkspaceIntegrationTests` (real PostgreSQL under the application and worker logins: row hidden from members, strangers and another tenant, a non-Owner cannot delete even with raw SQL, stale version, personal refusal), `DeletedWorkspacePipelineIntegrationTests` (real pipeline: identical NotFound for Owner, Admin and Member on a workspace, its boards and a delete repeat), `workspace-settings.interaction.component.test.tsx` · Real e2e `workspace-delete-restore.real.e2e.spec.ts`
- State: HARDENING — Owner check and an approved artboard (none exists); no workspace export exists, so none is offered in the 30 days; the other contexts have no reaction to `WorkspaceDeleted` (stub consumers); provider-side cleanup of integrations is not done; sockets already open when the workspace is deleted are not closed (new subscriptions are refused).

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
- Evidence: BE `POST /api/v1/workspaces/{id}/restore`, `RestoreWorkspaceCommandHandler`, `GET /api/v1/accounts/{accountId}/workspaces/deleted`, `GetDeletedWorkspacesQueryHandler`, `Workspace.Restore` · FE `account-workspaces-surface.tsx` (`DeletedCard`), `use-account-workspaces.ts` (`deleted`, `restore`), `account.service.ts` (`listDeletedWorkspaces`, `restoreWorkspace`, the Account named by the bootstrap) · Design none: no artboard for it; reuses the archived-workspaces group · Tests `WorkspaceDeletionAtomicityTests`, `DeletedWorkspaceIntegrationTests` (round trip with RLS, expired window, the Recently deleted list for an Owner, a member and another tenant), `DeletedWorkspacePipelineIntegrationTests` (restore by the Owner only, then every request answers again), `account-workspaces.interaction.component.test.tsx`, `account-deleted-workspaces.unit.test.ts` · Real e2e `workspace-delete-restore.real.e2e.spec.ts`
- State: HARDENING — Owner check and an approved artboard (none exists); a person left with no workspace cannot reach the page (the account page lives inside a workspace); no story and no design diff for the new rows.

### UC-WSP-D05 Transfer workspace ownership
- Capability: D Workspace lifecycle transition
- Actors and permission: Current Owner (`ManageWorkspace`)
- Preconditions: Target is an active non-Guest member; caller is an active Owner.
- Input: `newOwnerId`, `expectedVersion`.
- Output: Target becomes Owner, caller becomes Admin, grants re-synced, version +1. There is always at least one Owner.
- Main flow: 1. `POST /api/v1/workspaces/{id}/transfer-ownership`. 2. The workspace row is locked (serializes with remove, suspend, role change). 3. Domain promotes target and demotes caller. 4. Grants sync in the same transaction.
- No-op / rejection / concurrency / retry: Target = caller: rejected. Target not an active member: NotFound. Stale version: 409. A concurrent removal of the target cannot interleave (lock).
- UI states: Confirmation naming the person and the effect, pending, success, conflict.
- Rules: [workspaces](../../product/workspaces.md) sections 17, 18; WSP-008; WGREQ019; Part 4 LD-05.
- Acceptance: Given Owner A and member B, When A transfers to B, Then B is Owner and A is Admin. Given B is removed in parallel, Then exactly one of the two succeeds.
- Evidence: BE `POST /api/v1/workspaces/{id}/transfer-ownership`, `TransferOwnershipCommandHandler` · FE none · Design none (Final-Members shows no transfer) · Tests `WorkspaceOwnerRulesTests`, `WorkspaceMembershipConcurrencyEvidenceTests` · Real e2e none
- State: IMPLEMENTING — Screen (members page or settings); artboard; step-up for the Owner.

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
- No-op / rejection / concurrency / retry: Guest: 403 (GOV-009). Suspended or removed: 403. Non-member of an existing workspace: 403 today, while a missing workspace is 404 (pipeline gate `ExecutionContextBehavior`, every workspace-scoped route; see Part 1.3). `limit` outside 1 to 200 or a cursor the list did not issue: 400. Read-only: no version, no no-op, no retry key.
- UI states: Loading, empty (only me), success with name search, no match, permission denied (a guest reads only their own row and is told why; a refused read says so without a retry), offline, error with retry.
- Rules: [workspaces](../../product/workspaces.md) sections 11, 48.2, 82; WSP-005; WGREQ012, WGREQ013; WX-MEM-001.
- Acceptance: Given 60 members, When any member lists, Then the first page holds 50 and `nextCursor` leads to the other 10, each person once. Given people who joined at the same instant, When walked one at a time, Then none repeats or is skipped. Given a removed member, Then they are never listed. Given a Member and an opted-in colleague, Then the phone shows; given a Guest, Then 403.
- Evidence: BE `GET /api/v1/workspaces/{id}/members`, `GetWorkspaceMembersQueryHandler`, `GetWorkspaceMembersQueryValidator` · FE `workspace-members-surface.tsx`, `members.service.ts` (`getList` joins the pages), `use-workspace-members.ts`, `section-state.ts` · Design none for the workspace roster; `Final-Members` and `Final-M-Members` are the department Members tab (`design-fidelity-members.mock.e2e.spec.ts` measures that tab) · Tests `GetWorkspaceMembersPagingTests` (15), `GetWorkspaceMembersPhoneVisibilityTests`, `WorkspaceAdministrationReadIntegrationTests` (10), `MemberProfileIntegrationTests`, `WorkspaceRoleBaselineIntegrationTests`, `MemberEndpointTests`, `WorkspacesOpenApiShapeTests`, `workspace-pages-model.unit.test.ts`, `guest-members-read.unit.test.ts`, `section-state.unit.test.ts`, `workspace-members.interaction.component.test.tsx` (21), `workspace-pages.mock.e2e.spec.ts` · Real e2e `workspace-members.real.e2e.spec.ts`
- State: HARDENING — Owner check. Open: an approved roster artboard (none exists) and its diff; identical NotFound for a non-member (pipeline-wide); the shell still reads every page of the roster to resolve names.

### UC-WSP-E02 Read my own membership
- Capability: E Membership read
- Actors and permission: Any active member, Guest included (`ViewWorkspace`)
- Preconditions: Active membership.
- Input: `workspaceId`.
- Output: My role, status, job title, version.
- Main flow: 1. `GET /api/v1/workspaces/{id}/members/me`. 2. Handler returns the caller's row.
- No-op / rejection / concurrency / retry: A non-member of an existing workspace gets 403 today and a missing workspace 404 (pipeline gate, see Part 1.3); a suspended or removed member gets 403. Read-only: no version, no no-op, no retry key. Used by the shell to hide actions the person cannot do; the server still enforces them.
- UI states: Loading, success, not found.
- Rules: [workspaces](../../product/workspaces.md) sections 11, 15; WSP-007; WGREQ082.
- Acceptance: Given a Guest, When they read their membership, Then role is Guest.
- Evidence: BE `GET /api/v1/workspaces/{id}/members/me`, `GetMyWorkspaceMembershipQueryHandler` · FE `members.service.ts` (`parseMyMembership`), `use-workspace-viewer.ts` · Design none · Tests `WorkspaceAdministrationReadIntegrationTests` (own row and version, another workspace and a suspended member refused), `WorkspaceRoleBaselineIntegrationTests` (guest reads only their own row, outsider refused), `MemberProfileIntegrationTests` (a guest's own row carries no phone) · Real e2e `workspace-members.real.e2e.spec.ts` (an Owner and a Guest each read `members/me`)
- State: HARDENING — Owner check. Open: identical NotFound for a non-member (pipeline-wide).

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
- Acceptance: Given two Owners, When one is demoted, Then it succeeds. Given one Owner, Then it fails and nothing changes. Given a stale version, Then 409.
- Evidence: BE `PATCH /api/v1/workspaces/{id}/members/{userId}`, `UpdateMemberRoleCommand` is now an `IExpectedVersionRequest` mapped to the WorkspaceMember in `ExpectedVersionTargetMap`; the request body has `expectedVersion` · FE `members.service.ts` reads each member's `version` and sends it (a member with no known version is never written), `use-member-mutations.ts` (optimistic role, rolled back on failure), Members surface asks before a lowered role (Admin to Member, anything to Guest), conflict toast already existed; mock backend checks and moves the version · Design `Final-Members` (department Members tab; no roster artboard) · Tests `WorkspaceMemberRulesTests`, `WorkspaceMembershipConcurrencyEvidenceTests`, `WorkspaceMemberVersionTests`, `workspace-pages.mock.e2e.spec.ts` (WX-MEM-001); existing tests that build the command without a version need updating, see the test list under F05 · Real e2e workspace-member-changes.real.e2e.spec.ts
- State: HARDENING — Runs on the real stack (`workspace-member-changes.real.e2e.spec.ts`, 2 tests): the Owner raises a Member to Admin and lowers the role with the confirmation dialog, a same-role write is a no-op with no version step, a stale version answers 412 and changes nothing, a Member is refused 403, the only Owner cannot be demoted. Left for DONE: Owner check; an approved roster artboard (none) and its 1-px diff at 3 widths and both themes; conflict and offline states driven in a browser; axe and keyboard run. The demote and remove race under the workspace row lock is proven by backend tests only. Acceptance 2026-11-02 at `6ac912d0c` (independent acceptance agent, branch accept/batch-1), stays HARDENING. Line 4 holds: `workspace-member-changes.real.e2e.spec.ts` 2 passed. Line 3 gaps (roster has no artboard; screenshots at 1440, 1024 and 390 in light and dark opened, no overflow or clipping): axe `aria-hidden-focus` (serious) with the row menu open at all three widths; at 390 the Invite people button (32 px), the Members and Invitations tabs (29 px) and the filter field (32 px) are under 44 px. At 390 px the four shell top-bar icon buttons (navigation 32, command palette 32, theme 32, notifications 36) are under 44 px on every workspace screen. Conflict and offline states are not driven in a browser. Line 1: the named backend classes in the Domain, Application, API and Integration projects ran and passed (their cases were not audited one by one against the line); the Architecture, Platform and Infrastructure projects were not run, so `WorkspaceScopedArchitectureTests`, `SendInvitationEmailConsumerTests` and `SuspendMemberCommandHandlerTests` are unrun where an Evidence line names them. Backend runs of this acceptance: Domain 200, Application 150 and API 97 (filtered to the classes this use case names, 0 failed) and Postgres Integration 229 (0 failed).

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
- Evidence: BE `POST /api/v1/workspaces/{id}/members/{userId}/suspend`, `SuspendMemberCommand` is an `IExpectedVersionRequest` (new `MemberVersionRequest` body) · FE `members.service.ts` and the Members surface send the member version; mock backend checks it · Design `Final-Members` · Tests `WorkspaceMemberTests`, `WorkspaceMembershipOutboxEvidenceTests` · Real e2e none
- State: INTEGRATING — Tests and owner check: stale 412, no-op, last owner, real e2e; the mock e2e covered role and removal only.

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
- Evidence: BE `POST /api/v1/workspaces/{id}/members/{userId}/activate`, `ActivateMemberCommandHandler` now takes `IBillingSeatGate` and the command is an `IExpectedVersionRequest` · FE `members.service.ts`, the Members surface (the seat refusal shows the producer's rule message in the toast) · Design `Final-Members` · Tests `WorkspaceMemberTests`; handler tests that construct it need the seat gate · Real e2e none
- State: INTEGRATING — Tests and owner check: seat refused, seat free, active member no-op, stale 412, real e2e. No upgrade path is offered on a seat refusal (the invite flow has one).

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
- Evidence: BE `DELETE /api/v1/workspaces/{id}/members/{userId}?expectedVersion=`, `RemoveMemberCommand` is an `IExpectedVersionRequest` · FE `use-member-mutations.ts`, Members surface · Design `Final-Members` · Tests `WorkspaceMemberSelfRemovalTests`, `WorkspaceMembershipConcurrencyEvidenceTests`, `workspace-pages.mock.e2e.spec.ts`. Existing tests that build the four member commands without a version and must be updated: `MemberActorTargetGuardHandlerTests` (7), `SuspendMemberCommandHandlerTests` (3), `RlsRuntimeEnforcementTests` (2), `WorkspaceMembershipConcurrencyEvidenceTests` (3), `WorkspaceRoleBaselineIntegrationTests` (11), and any activate handler test (new constructor argument) · Real e2e workspace-member-changes.real.e2e.spec.ts
- State: HARDENING — Runs on the real stack (`workspace-member-changes.real.e2e.spec.ts`, 2 tests): the row menu asks, the person is removed and loses the workspace at once (members/me and the workspace answer not found), a stale expected version answers 412, a Member is refused 403, removing yourself is refused and the Owner stays, the other member is untouched. Left for DONE: Owner check; roster artboard and 1-px diff (none drawn); conflict and offline states in a browser; axe and keyboard run. Entries cleanup is F09 (DEFINED, not built); a repeat removal of a removed person is not driven in a browser. Acceptance 2026-11-02 at `6ac912d0c` (independent acceptance agent, branch accept/batch-1), stays HARDENING. Line 4 holds: `workspace-member-changes.real.e2e.spec.ts` 2 passed. Line 3 gaps (roster has no artboard; screenshots at 1440, 1024 and 390 in light and dark opened, no overflow or clipping): axe `aria-hidden-focus` (serious) with the row menu open at all three widths; at 390 the Invite people button (32 px), the Members and Invitations tabs (29 px) and the filter field (32 px) are under 44 px. At 390 px the four shell top-bar icon buttons (navigation 32, command palette 32, theme 32, notifications 36) are under 44 px on every workspace screen. Conflict and offline states are not driven in a browser. Line 1: the named backend classes in the Domain, Application, API and Integration projects ran and passed (their cases were not audited one by one against the line); the Architecture, Platform and Infrastructure projects were not run, so `WorkspaceScopedArchitectureTests`, `SendInvitationEmailConsumerTests` and `SuspendMemberCommandHandlerTests` are unrun where an Evidence line names them. Backend runs of this acceptance: Domain 200, Application 150 and API 97 (filtered to the classes this use case names, 0 failed) and Postgres Integration 229 (0 failed).

### UC-WSP-F06 Leave a workspace
- Capability: F Membership add / remove / change
- Actors and permission: Any active member, Guest included; subject is the caller
- Preconditions: Active membership; another active Owner exists if the caller is an Owner.
- Input: `workspaceId` (path); no user input.
- Output: Membership ends like a removal; the grant is revoked in the same transaction; a default workspace pointing here is cleared.
- Main flow: 1. `DELETE /api/v1/account/workspaces/{workspaceId}`. 2. Handler resolves the caller. 3. Last-owner guard. 4. Remove and clear default.
- No-op / rejection / concurrency / retry: Last Owner: 409 `workspaces.member.last-owner-cannot-leave`. Already left: NotFound. A retry after success is NotFound (not idempotent); the account service now treats that 404 as done and the list is read again.
- UI states: Account page: confirmation, sole-owner disabled with reason (`canLeave`), pending, success.
- Rules: [workspaces](../../product/workspaces.md) section 19, 19.1; WGREQ018.
- Acceptance: Given two Owners, When one leaves, Then success. Given one Owner, Then 409 and no change.
- Evidence: BE `DELETE /api/v1/account/workspaces/{workspaceId}`, `LeaveWorkspaceCommandHandler` · FE account `workspaces.tsx` (the sole-owner note and the disabled button already name what to do first), `account.service.ts` (404 counts as done) · Design `Final-Account` · Tests `WorkspaceMemberSelfRemovalTests`, `AccountWorkspaceEndpointTests`, `account-pages.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — Tests and owner check: real e2e, retry-after-success. The sole-owner note says to hand ownership over but no screen does it (D05, IMPLEMENTING, outside this order); Space and Team cleanup is F09 (DEFINED, not built).

### UC-WSP-F07 Set my own job title
- Capability: F Membership add / remove / change
- Actors and permission: Any active member (`ViewWorkspace` on own row)
- Preconditions: Active membership; current member version.
- Input: `jobTitle` (max 100, trimmed, no control chars; blank clears), `expectedVersion`.
- Output: Title stored on the membership, member version +1; shown in member and Team lists, only inside the workspace.
- Main flow: 1. `PUT /api/v1/workspaces/{id}/members/me/job-title`. 2. Version check. 3. Domain sets title.
- No-op / rejection / concurrency / retry: Same title: no-op. Stale version: 409. Removed member: not editable.
- UI states: Inline edit in the profile dialog, validation error, pending, conflict.
- Rules: [workspaces](../../product/workspaces.md) section 11 (D15, 2026-10-23).
- Acceptance: Given title `Lead`, When saved again, Then version is unchanged.
- Evidence: BE `PUT /api/v1/workspaces/{id}/members/me/job-title`, `UpdateMyJobTitleCommandHandler` · FE consumer row `Workspaces.Members.UpdateMyJobTitle` (enabled surface) · Design none · Tests `MemberProfileIntegrationTests`, `WorkspaceMemberJobTitleTests`, `CoMemberProfileTests` · Real e2e none
- State: INTEGRATING — Tests and owner check: real e2e; no screen artboard. Nothing found to build (the version is already required).

### UC-WSP-F08 Set another member's job title
- Capability: F Membership add / remove / change
- Actors and permission: Owner, Admin (`ManageMemberProfile`)
- Preconditions: Target is an active member.
- Input: `userId`, `jobTitle`, `expectedVersion`.
- Output: Title stored; no role, status or permission changes.
- Main flow: 1. `PUT .../members/{userId}/job-title`. 2. Pipeline checks `ManageMemberProfile`. 3. Version check, set.
- No-op / rejection / concurrency / retry: As F07. A caller without the action: 403.
- UI states: Member row edit, pending, conflict, permission denied.
- Rules: [workspaces](../../product/workspaces.md) section 11.
- Acceptance: Given an Admin, When they set a Member's title, Then it shows in lists.
- Evidence: BE `PUT /api/v1/workspaces/{id}/members/{userId}/job-title`, `UpdateMemberJobTitleCommandHandler` · FE none · Design none · Tests `MemberProfileIntegrationTests` · Real e2e none
- State: IMPLEMENTING — No frontend caller; artboard.

### UC-WSP-F09 Clean a person's Space and Team entries when they leave (enterprise baseline)
- Capability: F Membership add / remove / change
- Actors and permission: System (inside F05 and F06)
- Preconditions: A membership ends.
- Input: Workspace id, user id.
- Output: The person's User entries in Spaces and Teams are removed in the same transaction; a Team left without a Lead is flagged.
- Main flow: 1. Removal or leave runs. 2. The same transaction deletes Space entries and Team memberships of that user. 3. Facts are published.
- No-op / rejection / concurrency / retry: A sole Space Admin removal is allowed (Owners and Admins still manage every Space). A sole Team Lead removal is allowed and leaves the Team with no Lead (LD-12). Today nothing is cleaned.
- UI states: Team shows 'needs a lead'.
- Rules: [workspaces](../../product/workspaces.md) section 19 ('still to define: cleanup'), WSP-024, WSP-014; Part 4 LD-12.
- Acceptance: Given a person in two Spaces and a Team, When removed, Then they hold no entry anywhere in the workspace.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Transaction step, event, 'needs a lead' state.

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
- Evidence: BE `POST /api/v1/workspaces/{id}/members`, `InviteMemberCommandHandler`, now an `IIdempotentRequest` (`workspaces.invitation.invite-member.v1`, key required on the endpoint) and a `ConflictException` with the stable duplicate code · FE `invitations.service.ts`, `use-create-invitation.ts` (one key per address and role, reused on retry, cleared on success), Members surface (a duplicate raised by someone else shows the same message as the check before sending), `seat-invite.tsx`; mock backend keys and refuses the same way · Design `Final-Members` · Tests `WorkspaceInvitationTests`, `WorkspaceInvitationRulesTests`, `InvitationEndpointTests`, `NewInvitationEndpointTests`, `WorkspaceInvitationSecretTelemetryTests`, `SendInvitationEmailConsumerTests`, `workspace-pages.mock.e2e.spec.ts`; existing tests that send the invite without a key, or assert the old plain duplicate text, need updating · Real e2e workspace-invitations-manage.real.e2e.spec.ts
- State: HARDENING — Runs on the real stack (`workspace-invitations-manage.real.e2e.spec.ts`): the form sends one invitation, the list shows it, one e-mail is written with the link; a second invitation to the address answers 409 `workspaces.invitation.pending-exists`, an Admin role answers 400 and writes no row, a Member as inviter answers 403, a retry under one Idempotency-Key returns the first invitation and writes no second e-mail. Left for DONE: Owner check; design diff at 3 widths and both themes; delivery through a real mail provider; axe and keyboard run. Acceptance 2026-11-02 at `6ac912d0c` (independent acceptance agent, branch accept/batch-1), stays HARDENING. Line 4 holds: `workspace-invitations-manage.real.e2e.spec.ts` 2 passed (`workspace-invitations-answer.real.e2e.spec.ts` 1 passed). Line 3 gaps: only the empty Invitations tab was screenshotted (the populated list with the role select, resend and revoke was not reviewed); axe `color-contrast` (serious) in light at 1440 and 1024; at 390 the tabs (29 px), the address field (42 px) and the role select (36 px) are under 44 px. At 390 px the four shell top-bar icon buttons (navigation 32, command palette 32, theme 32, notifications 36) are under 44 px on every workspace screen. Line 1: the named backend classes in the Domain, Application, API and Integration projects ran and passed (their cases were not audited one by one against the line); the Architecture, Platform and Infrastructure projects were not run, so `WorkspaceScopedArchitectureTests`, `SendInvitationEmailConsumerTests` and `SuspendMemberCommandHandlerTests` are unrun where an Evidence line names them. Backend runs of this acceptance: Domain 200, Application 150 and API 97 (filtered to the classes this use case names, 0 failed) and Postgres Integration 229 (0 failed).

### UC-WSP-G02 Invite with initial Team and Space entries
- Capability: G Invitation create
- Actors and permission: Owner, Admin; `ManageTeams` for Team entries and `ManageSpaces` for Space entries
- Preconditions: Named Teams and Spaces are active in the workspace.
- Input: G01 plus `entries` (Team member or lead, Space role). A Guest invitation cannot name a Team lead or Space admin.
- Output: The invitation stores the entries; they apply atomically on acceptance.
- Main flow: 1. Same call as G01 with entries. 2. Each kind needs its own action. 3. Acceptance applies membership and entries in one transaction.
- No-op / rejection / concurrency / retry: Inactive Team or Space at acceptance: acceptance fails closed, nothing changes, the issuer invites again. A rule granting only one of the two actions does not authorize a mixed invitation. An existing entry is raised, never lowered.
- UI states: Entry picker in the invite dialog, per-entry validation, permission denied per kind.
- Rules: [workspaces](../../product/workspaces.md) section 22.1; WSP-026; WGREQ027.
- Acceptance: Given both actions, When an invitation names a Team and a Space, Then both entries exist after acceptance. Given only `ManageSpaces`, Then a mixed invitation is refused.
- Evidence: BE `InviteMemberCommandHandler`, `InvitationInitialEntries` · FE none (no entry field) · Design `Final-Members` · Tests `InvitationMixedEntriesAuthorizationIntegrationTests`, `GuestAndEntryInvitationIntegrationTests`, `WorkspaceInvitationEntriesAndResourceGrantTests` · Real e2e none
- State: IMPLEMENTING — No frontend field for entries.

### UC-WSP-G03 List workspace invitations
- Capability: G Invitation create
- Actors and permission: Owner, Admin, Member (`ViewMembers`; a Guest gets 403)
- Preconditions: Active membership.
- Input: `workspaceId`.
- Output: Rows: email, role, status (Pending, Accepted, Revoked, Expired, Declined), issuer, expiry, version. No token.
- Main flow: 1. `GET /api/v1/workspaces/{id}/invitations`. 2. Handler lists.
- No-op / rejection / concurrency / retry: Guest: 403 (`ViewMembers`, fixed; section 82 calls invitations sensitive). A Member still sees invited emails, because the member list's email masking is a handler role check the architecture tests forbid; hiding them needs a pipeline-level rule. The read is a cursor page (LD-06): default 50, at most 200, newest first; `limit` outside 1 to 200 or a cursor the list did not issue answers 400. A Pending invitation past its expiry is reported as Expired (LD-04); the sweep that writes the status is not built.
- UI states: Loading, empty, success, permission denied.
- Rules: [workspaces](../../product/workspaces.md) sections 14, 25, 82; WGREQ025, WGREQ032.
- Acceptance: Given a Guest, When they list invitations, Then 403. Given an Admin, Then rows without tokens.
- Evidence: BE `GET /api/v1/workspaces/{id}/invitations`, `GetWorkspaceInvitationsQueryHandler` · FE `use-workspace-invitations.ts`, Members surface 'pending-invitations' · Design `Final-Members` · Tests `InvitationScopeTests`, `InvitationEndpointTests`, `WorkspaceRoleBaselineIntegrationTests` (Guest 403, Admin and Member list) · FE gated by `enabled: admin` in `members.tsx` · Built: `GetWorkspaceInvitationsQuery(Cursor, Limit)` returns a `CursorPage`, rows carry `version`, expired read; `invitations.service.ts` joins the pages; mock backend pages the same way. Existing tests that read the list as an array (`WorkspaceAdministrationReadHandlerTests`, `WorkspaceAdministrationReadIntegrationTests`, `WorkspaceRoleBaselineIntegrationTests`) need updating · Real e2e workspace-invitations-manage.real.e2e.spec.ts
- State: HARDENING — Runs on the real stack (`workspace-invitations-manage.real.e2e.spec.ts`): rows carry no token key, an Owner and a Member read them, a Guest gets 403, cursor pages cover each row once, limits 0 and 201 and a made-up cursor answer 400. Left for DONE: Owner check; the owner question (may Members see invited emails); the issuer in the row; the expiry sweep that writes Expired; design diff. Acceptance 2026-11-02 at `6ac912d0c` (independent acceptance agent, branch accept/batch-1), stays HARDENING. Line 4 holds: `workspace-invitations-manage.real.e2e.spec.ts` 2 passed (`workspace-invitations-answer.real.e2e.spec.ts` 1 passed). Line 3 gaps: only the empty Invitations tab was screenshotted (the populated list with the role select, resend and revoke was not reviewed); axe `color-contrast` (serious) in light at 1440 and 1024; at 390 the tabs (29 px), the address field (42 px) and the role select (36 px) are under 44 px. At 390 px the four shell top-bar icon buttons (navigation 32, command palette 32, theme 32, notifications 36) are under 44 px on every workspace screen. Line 1: the named backend classes in the Domain, Application, API and Integration projects ran and passed (their cases were not audited one by one against the line); the Architecture, Platform and Infrastructure projects were not run, so `WorkspaceScopedArchitectureTests`, `SendInvitationEmailConsumerTests` and `SuspendMemberCommandHandlerTests` are unrun where an Evidence line names them. Backend runs of this acceptance: Domain 200, Application 150 and API 97 (filtered to the classes this use case names, 0 failed) and Postgres Integration 229 (0 failed).

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
- Evidence: BE `PATCH .../invitations/{invitationId}/role`, `ChangeInvitationRoleCommand` is an `IExpectedVersionRequest` mapped to `WorkspaceInvitation` in `ExpectedVersionTargetMap` · FE `invitations.service.ts` and the Members surface send the version (an invitation with no known version is never written); mock backend checks it · Design `Final-Members` · Tests `WorkspaceInvitationRulesTests`; tests that build the command without a version need updating · Real e2e workspace-invitations-manage.real.e2e.spec.ts
- State: HARDENING — Runs on the real stack (`workspace-invitations-manage.real.e2e.spec.ts`): the role select on the row offers Guest and the list shows it with a higher version, a stale version answers 412 and changes nothing, a raise to Admin answers 400, the same role is a no-op with no version step. Left for DONE: Owner check; the conflict and offline states driven in a browser; design diff at 3 widths and both themes; axe and keyboard run. Acceptance 2026-11-02 at `6ac912d0c` (independent acceptance agent, branch accept/batch-1), stays HARDENING. Line 4 holds: `workspace-invitations-manage.real.e2e.spec.ts` 2 passed (`workspace-invitations-answer.real.e2e.spec.ts` 1 passed). Line 3 gaps: only the empty Invitations tab was screenshotted (the populated list with the role select, resend and revoke was not reviewed); axe `color-contrast` (serious) in light at 1440 and 1024; at 390 the tabs (29 px), the address field (42 px) and the role select (36 px) are under 44 px. At 390 px the four shell top-bar icon buttons (navigation 32, command palette 32, theme 32, notifications 36) are under 44 px on every workspace screen. Line 1: the named backend classes in the Domain, Application, API and Integration projects ran and passed (their cases were not audited one by one against the line); the Architecture, Platform and Infrastructure projects were not run, so `WorkspaceScopedArchitectureTests`, `SendInvitationEmailConsumerTests` and `SuspendMemberCommandHandlerTests` are unrun where an Evidence line names them. Backend runs of this acceptance: Domain 200, Application 150 and API 97 (filtered to the classes this use case names, 0 failed) and Postgres Integration 229 (0 failed).

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
- Evidence: BE `POST .../invitations/{invitationId}/resend`, `ResendInvitationCommandHandler`, `WorkspaceInvitation.Resend` with `ResendCooldown`; the refusal shows as the producer's rule message in the Members toast · FE `invitations.service.ts` · Design `Final-Members` · Tests `WorkspaceInvitationTests`, `InvitationTokenFlowHttpTests`; tests that resend right after creating now meet the cool-down and need a later time; the rule-code snapshot has the new code · Real e2e workspace-invitations-manage.real.e2e.spec.ts
- State: HARDENING — Runs on the real stack (`workspace-invitations-manage.real.e2e.spec.ts`): a resend inside the minute answers a rule refusal and writes no e-mail and no version step; after the minute the row button sends a new e-mail with a new token, the new link previews valid and the old one does not. Left for DONE: Owner check; the resend still uses a fixed 7-day expiry, not the workspace setting (a J question); design diff; axe and keyboard run. Acceptance 2026-11-02 at `6ac912d0c` (independent acceptance agent, branch accept/batch-1), stays HARDENING. Line 4 holds: `workspace-invitations-manage.real.e2e.spec.ts` 2 passed (`workspace-invitations-answer.real.e2e.spec.ts` 1 passed). Line 3 gaps: only the empty Invitations tab was screenshotted (the populated list with the role select, resend and revoke was not reviewed); axe `color-contrast` (serious) in light at 1440 and 1024; at 390 the tabs (29 px), the address field (42 px) and the role select (36 px) are under 44 px. At 390 px the four shell top-bar icon buttons (navigation 32, command palette 32, theme 32, notifications 36) are under 44 px on every workspace screen. Line 1: the named backend classes in the Domain, Application, API and Integration projects ran and passed (their cases were not audited one by one against the line); the Architecture, Platform and Infrastructure projects were not run, so `WorkspaceScopedArchitectureTests`, `SendInvitationEmailConsumerTests` and `SuspendMemberCommandHandlerTests` are unrun where an Evidence line names them. Backend runs of this acceptance: Domain 200, Application 150 and API 97 (filtered to the classes this use case names, 0 failed) and Postgres Integration 229 (0 failed).

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
- Evidence: BE `DELETE .../invitations/{invitationId}`, `CancelInvitationCommandHandler` · FE `invitations.service.ts` · Design `Final-Members` · Tests `WorkspaceInvitationConcurrencyEvidenceTests`, `AcceptInvitationTransactionEvidenceTests` · Real e2e workspace-invitations-manage.real.e2e.spec.ts
- State: HARDENING — Runs on the real stack (`workspace-invitations-manage.real.e2e.spec.ts`): the trash button asks, the row leaves the pending list and reads Revoked, the link previews used with the address withheld, accepting it with a registration and resending it are refused, a Member is refused 403, a revoke of an accepted invitation answers 204 and changes nothing (the person stays an Active member). Left for DONE: Owner check; the accept-and-revoke race is proven by backend tests only; design diff; axe and keyboard run. Acceptance 2026-11-02 at `6ac912d0c` (independent acceptance agent, branch accept/batch-1), stays HARDENING. Line 4 holds: `workspace-invitations-manage.real.e2e.spec.ts` 2 passed (`workspace-invitations-answer.real.e2e.spec.ts` 1 passed). Line 3 gaps: only the empty Invitations tab was screenshotted (the populated list with the role select, resend and revoke was not reviewed); axe `color-contrast` (serious) in light at 1440 and 1024; at 390 the tabs (29 px), the address field (42 px) and the role select (36 px) are under 44 px. At 390 px the four shell top-bar icon buttons (navigation 32, command palette 32, theme 32, notifications 36) are under 44 px on every workspace screen. Line 1: the named backend classes in the Domain, Application, API and Integration projects ran and passed (their cases were not audited one by one against the line); the Architecture, Platform and Infrastructure projects were not run, so `WorkspaceScopedArchitectureTests`, `SendInvitationEmailConsumerTests` and `SuspendMemberCommandHandlerTests` are unrun where an Evidence line names them. Backend runs of this acceptance: Domain 200, Application 150 and API 97 (filtered to the classes this use case names, 0 failed) and Postgres Integration 229 (0 failed).

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
- Evidence: BE `POST /api/v1/invitations/preview`, `GetInvitationByTokenQueryHandler` · FE `invite/$token.tsx`, `invite/index.tsx` (the e-mail link, token in the fragment), `use-invitation-by-token.ts`, `onboarding-join-surface.tsx` · Design `Final-Onboarding`, `Final-Auth` · Tests `GetInvitationByTokenQueryHandlerTests`, `InvitationTokenFlowHttpTests`, `onboarding.mock.e2e.spec.ts` · Real e2e workspace-invitation-link.real.e2e.spec.ts
- State: HARDENING — Runs on the real stack (`workspace-invitation-link.real.e2e.spec.ts`): the link of the e-mail (`/invite#token=`) opens the join page signed out, which names the inviter, the workspace and the offered role; the preview answers valid with workspace name, role and address; a made-up token and a malformed one both answer 404 with the same body; a used link reads used and withholds the address. Left for DONE: Owner check; the rate limit is not proven on the real stack (it is raised there); the workspace wording of the join page has no artboard (the drawn page is the board-guest invitation) and needs a design diff; axe and keyboard run. Acceptance 2026-11-02 at `6ac912d0c` (independent acceptance agent, branch accept/batch-1), stays HARDENING. Line 4 holds: `workspace-invitation-link.real.e2e.spec.ts` 1 passed. Line 3 gaps: the valid-invitation join page was opened at 1440, 1024 and 390 in light and dark (no overflow, axe clean) but the unavailable states (used, revoked, unknown link) were not captured (the second navigation only changed the hash and did not reload, so that screenshot repeated the valid page); at 390 the show-password button is 36 px and the 'Sign in to accept' link 24 px; the workspace wording has no artboard. Line 1: the named backend classes in the Domain, Application, API and Integration projects ran and passed (their cases were not audited one by one against the line); the Architecture, Platform and Infrastructure projects were not run, so `WorkspaceScopedArchitectureTests`, `SendInvitationEmailConsumerTests` and `SuspendMemberCommandHandlerTests` are unrun where an Evidence line names them. Backend runs of this acceptance: Domain 200, Application 150 and API 97 (filtered to the classes this use case names, 0 failed) and Postgres Integration 229 (0 failed).

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
- Evidence: BE `POST /api/v1/invitations/accept`, `AcceptInvitationCommandHandler`, `InvitationAcceptanceService` (now requires `IBillingSeatGate`; tests that build either without a gate need the argument) · FE `auth-flow.service.ts` (auth feature), `onboarding-join-surface.tsx` · Design `Final-Onboarding` · Tests `AcceptInvitationTransactionEvidenceTests`, `WorkspaceInvitationConcurrencyEvidenceTests`, `WorkspaceCredentialAndInvitationEscalationTests`, `InvitationTokenFlowHttpTests`, `onboarding.mock.e2e.spec.ts`; run: Application `AcceptInvitationCommandHandlerTests` (15), Integration `AcceptInvitationTransactionEvidenceTests` (4), `WorkspaceInvitationConcurrencyEvidenceTests` (3), `WorkspaceCredentialAndInvitationEscalationTests` (6), API `InvitationEndpointTests` (10) · Real e2e `frontend/e2e/real/workspace-invitations-answer.real.e2e.spec.ts` (a removed member is invited again and accepts from the list: back in the workspace with a membership; another signed-in person is refused)
- State: HARDENING. Run 2026-11-01 at `d99b4cca0`: the real spec accepts by id through the flyout and the tests above pass (Domain filter 1184, Application filter 589, API filter 386, Integration filter 476, none failed; 12 of 12 real specs). Missing: owner check; seat refused, a repeat accept, a Suspended member and another email are proven by Application and Integration tests only, not in a browser; no artboard for the flyout. The route of this entry is the token route `POST /api/v1/invitations/accept`; the real spec drives accept by id (UC-WSP-H06), which goes through the same `InvitationAcceptanceService`.

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
- Evidence: BE `POST /api/v1/invitations/accept-with-registration`, `AcceptInvitationWithRegistrationCommandHandler` · FE `auth-flow.service.ts`, join surface · Design `Final-Onboarding` · Tests `InvitationTokenFlowHttpTests`, `WorkspaceCredentialAndInvitationEscalationTests`, `onboarding.mock.e2e.spec.ts` · Real e2e workspace-invitation-link.real.e2e.spec.ts
- State: HARDENING — Runs on the real stack (`workspace-invitation-link.real.e2e.spec.ts`): a person with no account fills name and password on the join page, a password below the policy is refused in the form and creates nothing, a valid one creates the account, the Member membership and the session and lands in the workspace; the invitation reads Accepted and the link cannot be used again. Left for DONE: Owner check; the rollback of a partial failure and the existing-account path (sign in, then H02) are proven by backend tests only; design diff of the join page; axe and keyboard run. Acceptance 2026-11-02 at `6ac912d0c` (independent acceptance agent, branch accept/batch-1), stays HARDENING. Line 4 holds: `workspace-invitation-link.real.e2e.spec.ts` 1 passed. Line 3 gaps: the valid-invitation join page was opened at 1440, 1024 and 390 in light and dark (no overflow, axe clean) but the unavailable states (used, revoked, unknown link) were not captured (the second navigation only changed the hash and did not reload, so that screenshot repeated the valid page); at 390 the show-password button is 36 px and the 'Sign in to accept' link 24 px; the workspace wording has no artboard. Line 1: the named backend classes in the Domain, Application, API and Integration projects ran and passed (their cases were not audited one by one against the line); the Architecture, Platform and Infrastructure projects were not run, so `WorkspaceScopedArchitectureTests`, `SendInvitationEmailConsumerTests` and `SuspendMemberCommandHandlerTests` are unrun where an Evidence line names them. Backend runs of this acceptance: Domain 200, Application 150 and API 97 (filtered to the classes this use case names, 0 failed) and Postgres Integration 229 (0 failed).

### UC-WSP-H04 Decline an invitation from its link
- Capability: H Invitation accept / reject / revoke
- Actors and permission: Anyone holding the token (anonymous)
- Preconditions: A Pending token.
- Input: `token`.
- Output: Invitation Declined; it cannot be accepted.
- Main flow: 1. `POST /api/v1/invitations/decline`. 2. Domain declines.
- No-op / rejection / concurrency / retry: A repeat or unknown token gets the same neutral answer.
- UI states: Decline link, confirmation, done page.
- Rules: [workspaces](../../product/workspaces.md) section 66; WGREQ029.
- Acceptance: Given a Pending token, When declined, Then accept fails.
- Evidence: BE `POST /api/v1/invitations/decline`, `DeclineInvitationByTokenCommandHandler` · FE `auth-flow.service.ts` · Design `Final-Onboarding` · Tests `InvitationTokenFlowHttpTests` · Real e2e none
- State: INTEGRATING — Real e2e.

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
- State: HARDENING. Run 2026-11-01 at `d99b4cca0`: the real spec and the tests above pass (Domain filter 1184, Application filter 589, API filter 386, Integration filter 476, none failed; 12 of 12 real specs). Built (2026-10-30 decision): the email-keyed read is one RLS policy (invitation row only, user scope, confirmed email); the handler reads through it, checks the email, and answers inside the token and invitee scopes the link flow already uses. Missing: owner check; the expired and unconfirmed-email cases are not driven in a browser; 1 px diff (no artboard).

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
- Evidence: BE `GET /api/v1/invitations/pending`, `GetUserPendingInvitationsQueryHandler`, `Invitations/Services/InviteeInvitations.cs`; RLS `Infrastructure/Data/Rls/RlsSqlScripts/025_invitee_invitations.sql` · FE `pending-invitations-menu.tsx`, `workspace-pending-invitations-surface.tsx`, `pending-invitations.copy.ts` (vi, en), `use-pending-invitations.ts`, rail slot in `shell/notrelix-system-rail-surface.tsx` · Design none for the flyout (the rail entry follows Final-Space) · Tests `InvitationEndpointTests`, run: Application `GetUserPendingInvitationsQueryHandlerTests` (8), API `InvitationEndpointTests` (10), Integration `InvitationScopeTests` (23), FE `workspace-ui-surfaces.interaction.component.test.tsx` (2), `use-accept-invitation.component.test.tsx` (2) · Real e2e `frontend/e2e/real/workspace-invitations-answer.real.e2e.spec.ts` (the invitee sees the one pending invitation in the rail flyout, another person sees none, accepting lands in the workspace and empties the list)
- State: HARDENING. Run 2026-11-01 at `d99b4cca0`: the real spec and the tests above pass (Domain filter 1184, Application filter 589, API filter 386, Integration filter 476, none failed; 12 of 12 real specs). Built (2026-10-30 decision): the read policy, the two handlers reading through it, accept by id (`POST /api/v1/invitations/{invitationId}/accept`, `AcceptInvitationByIdCommandHandler`: same email, seat, Suspended and Removed rules, through the shared `InvitationAcceptanceService`), the flyout in the system rail with vi and en copy and every state, the contract and the client. Missing: owner check; 1 px diff (no artboard for the flyout). Rollout: `--rls-apply` first (additive), then the API, then the web app; no schema migration.
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
- State: HARDENING — Tests and owner check: out-of-order response test; realtime dispose proof; design diff at 3 widths and both themes. Nothing found to build: the workspace realtime adapter already drops an event whose workspace is not the open one, and query keys carry the workspace id.

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
- Evidence: BE `GET /api/v1/account/workspaces`, `GetMyWorkspacesQueryHandler` (now also returns an archived workspace, flagged `isArchived`, to its Owners and Admins only, never as default) · FE rail, account `workspaces.tsx` with an "Archived workspaces" group whose rows show an Unarchive action (GET workspace for its version, then `POST /workspaces/{id}/archive/unarchive`), `use-workspace-list.ts` · Design `BC-Shell-1440`, `Final-Account` (no artboard for the group; built by eye) · Tests `AccountWorkspaceEndpointTests`, `AccountWorkspacesHttpFlowTests`, `AccountWorkspacesAndAvatarRlsIntegrationTests`, `account-pages.mock.e2e.spec.ts`, to be extended: archived shown to Owner and Admin only, hidden from Member and Guest, never default; the existing `toWorkspaceEntry` and copy-catalog key tests need the new fields · Real e2e workspace-read-and-lists.real.e2e.spec.ts
- State: HARDENING — Runs on the real stack (`workspace-read-and-lists.real.e2e.spec.ts`): a Member's row carries the role and `canLeave`, a Guest gets no member or department count, an Owner gets both, the account page lists the workspace, a stranger and a removed member never see it. Left for DONE: Owner check; the Archived group needs an owner look (no artboard) and an archived workspace still cannot be opened (banner is I05/D02); design diff at 3 widths and both themes. Acceptance 2026-11-02 at `6ac912d0c` (independent acceptance agent, branch accept/batch-1), stays HARDENING. Line 4 holds: `workspace-read-and-lists.real.e2e.spec.ts` 2 passed. Defect: the Your workspaces page shows 'We couldn't load your recently deleted workspaces. Try again' for the Owner of Workspace 3 at every width and theme on the real stack (the backend log shows authorization failures on that read). Line 3 gap: At 390 px the four shell top-bar icon buttons (navigation 32, command palette 32, theme 32, notifications 36) are under 44 px on every workspace screen. The Archived group still needs an owner look. Line 1: the named backend classes in the Domain, Application, API and Integration projects ran and passed (their cases were not audited one by one against the line); the Architecture, Platform and Infrastructure projects were not run, so `WorkspaceScopedArchitectureTests`, `SendInvitationEmailConsumerTests` and `SuspendMemberCommandHandlerTests` are unrun where an Evidence line names them. Backend runs of this acceptance: Domain 200, Application 150 and API 97 (filtered to the classes this use case names, 0 failed) and Postgres Integration 229 (0 failed).

### UC-WSP-I03 Choose my default workspace
- Capability: I Workspace selection / switch integration
- Actors and permission: Any active member
- Preconditions: Active membership of the target.
- Input: `workspaceId`.
- Output: Identity stores the id on my profile; the app opens it first. A default I cannot open reads as absent.
- Main flow: 1. `PUT /api/v1/account/default-workspace`. 2. Workspaces checks active membership. 3. Identity stores the id.
- No-op / rejection / concurrency / retry: The current default: no-op. Not a member: refused. Leaving clears it (F06).
- UI states: Account page choice, pending, success.
- Rules: [workspaces](../../product/workspaces.md) section 19.1.
- Acceptance: Given membership in W2, When I set it, Then the next sign-in opens W2.
- Evidence: BE `PUT /api/v1/account/default-workspace`, `SetDefaultWorkspaceCommandHandler` · FE account `workspaces.tsx`; `/home` (`home.tsx`) now opens the default workspace, read from the account list, and falls back to the first by name when none is chosen, the choice is archived or not listed, or the read fails (before, it always opened the first by name, so the choice had no effect) · Design `Final-Account` · Tests `UserProfileDefaultWorkspaceTests`, `AccountWorkspaceEndpointTests`, `account-pages.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — Tests and owner check: real e2e (set W2, open `/home`, land in W2); the command now refuses an archived workspace (business-rule failure; product doc 19.1 updated), so an archived default chosen earlier still reads as absent and home falls back to the first workspace. Tests to add: the refusal and that the stored default is unchanged.

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
- Evidence: BE `AuthorizeWorkspaceRealtimeSubscriptionQueryHandler`; the gateway (`RealtimeConnection.ReauthorizeAsync`) re-runs it for every open subscription every 60 seconds and revokes a denied one · FE `realtime-adapter.ts` · Design none · Tests `WorkspaceScopedArchitectureTests` · Real e2e workspace-read-and-lists.real.e2e.spec.ts
- State: HARDENING — Runs on the real stack (`workspace-read-and-lists.real.e2e.spec.ts`, a second test): a stranger's workspace subscription is answered `subscription_denied` and never acknowledged; a member's is acknowledged and, once the Owner removes them, the gateway sends `subscription_revoked` at its next check (within its 60-second interval). Left for DONE: Owner check; the web app's own socket is not asserted to drop the workspace after a revoke. Acceptance 2026-11-02 at `6ac912d0c` (independent acceptance agent, branch accept/batch-1), stays HARDENING. Line 4 holds: `workspace-read-and-lists.real.e2e.spec.ts` second test passed (stranger refused, removed member ended). Gap kept: the web app's own socket is not asserted to drop the workspace after a revoke. No screen to measure. Line 1: the named backend classes in the Domain, Application, API and Integration projects ran and passed (their cases were not audited one by one against the line); the Architecture, Platform and Infrastructure projects were not run, so `WorkspaceScopedArchitectureTests`, `SendInvitationEmailConsumerTests` and `SuspendMemberCommandHandlerTests` are unrun where an Evidence line names them. Backend runs of this acceptance: Domain 200, Application 150 and API 97 (filtered to the classes this use case names, 0 failed) and Postgres Integration 229 (0 failed).

### UC-WSP-I05 See why a workspace is unavailable
- Capability: I Workspace selection / switch integration
- Actors and permission: Any signed-in person
- Preconditions: The person opens a workspace route.
- Input: Route id.
- Output: One clear state: no membership, read-only, archived, plan-limited, temporarily unavailable.
- Main flow: 1. Reads return 404, 403 or a status. 2. The shell maps each to a state. 3. Owner and Admin of an archived workspace see Unarchive (D02).
- No-op / rejection / concurrency / retry: No membership and missing look identical (no existence leak). Archived members read but cannot write.
- UI states: The five states of section 86, each with its next step.
- Rules: [workspaces](../../product/workspaces.md) sections 65, 86; DESIGN.md sections 35, 37.
- Acceptance: Given an archived workspace, When a Member opens it, Then a read-only banner shows and write controls are inert.
- Evidence: BE statuses of B01 · FE `workspace-scope-state-surface.tsx`, `feature-unavailable-surface.tsx` · Design `Final-States` · Tests `workspace-shell.mock.e2e.spec.ts` · Real e2e none
- State: IMPLEMENTING — Archived banner with action; plan-limited state; real e2e. real-stack check 2026-10-31: a person who is not an active member (never joined, suspended, removed) now gets the same 404 as a made-up workspace id on every workspace-scoped route, from the one workspace gate in ExecutionContextBehavior; bodies byte-identical except echoed ids on members, members/me, members/me/access, roles, api-tokens, boards, pages, spaces and the custom-role PUT and DELETE; a member who lacks the action keeps 403; invitation accept by token and account routes unchanged, commit 9ca19ef3a

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
- Evidence: BE none · FE `workspace-sidebar-surface.tsx` (skeleton with a status label while loading), `workspace-topbar-surface.tsx`; dialog audit done: member remove and suspend, workspace archive and leave already named the workspace; invitation revoke, Space archive and Team archive now do (copy en and vi) · Design `BC-Shell-1440`, `BC-Shell-320` · Tests `design-fidelity-shell.mock.e2e.spec.ts`; existing tests that assert the old revoke, Space archive and Team archive texts need updating · Real e2e none
- State: INTEGRATING — Tests and owner check: real e2e. Still not naming the workspace: the Space delete confirm (inline type-to-confirm, delete not available yet) and the `confirmRemove` and `confirmArchive` Team texts, which no screen renders.

## J. Workspace settings

### UC-WSP-J01 Read workspace settings
- Capability: J Workspace settings
- Actors and permission: Any active member (`ViewWorkspace`)
- Preconditions: Active membership.
- Input: `workspaceId`.
- Output: Sharing and invitation policy, default member role, invitation expiry, guest default expiry, size, language, time zone, week start, version.
- Main flow: 1. `GET /api/v1/workspaces/{id}/settings`.
- No-op / rejection / concurrency / retry: A caller without access gets the same NotFound as for a missing id.
- UI states: Loading, success, permission denied.
- Rules: [workspaces](../../product/workspaces.md) sections 39, 48.2, 48.4; WGREQ010.
- Acceptance: Given a member, Then the values return with the version.
- Evidence: BE `GET /api/v1/workspaces/{id}/settings`, `GetWorkspaceSettingsQueryHandler` · FE `workspace.service.ts`, `use-workspace-settings.ts` · Design `Final-Settings` · Tests `WorkspaceSettingsEndpointTests`, `WorkspaceSettingsTests` · Real e2e none
- State: INTEGRATING — Tests and owner check: real e2e; design diff (no artboard for the settings form was found; built by eye).

### UC-WSP-J02 Change workspace settings
- Capability: J Workspace settings
- Actors and permission: Owner, Admin (`ManageWorkspace`)
- Preconditions: Caller holds the current version.
- Input: `allowPublicSharing`, `allowGuestInvites`, `defaultMemberRole`, `invitationExpiryDays`, `guestDefaultExpiryDays` (30, 90, 180, none), `size`, `language`, `timeZone`, `weekStart`, `expectedVersion`. A field left out keeps its value.
- Output: New settings, version +1; `PublicSharingChangedAt` set when sharing flips.
- Main flow: 1. `PUT /api/v1/workspaces/{id}/settings`. 2. Version check. 3. Domain validates each field. 4. Store.
- No-op / rejection / concurrency / retry: Unchanged: no-op. Invalid size or language: `Workspaces_Settings_SizeInvalid` / `LanguageInvalid`. Stale: 409. Guest expiry only pre-fills new entries (GOV-031).
- UI states: Form with save bar, validation, conflict, pending, permission denied (read-only).
- Rules: [workspaces](../../product/workspaces.md) sections 39, 40, 48.4; WSP-012; WGREQ010; WX-SET-001.
- Acceptance: Given version 5, When an Admin saves, Then version is 6 and a second save carries 6.
- Evidence: BE `PUT /api/v1/workspaces/{id}/settings`, `UpdateWorkspaceSettingsCommandHandler` · FE `use-workspace-settings-mutations.ts`, settings surface · Design `Final-Settings` · Tests `UpdateWorkspaceSettingsCommandHandlerTests`, `WorkspaceSettingsEndpointTests`, `workspace-pages.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — Tests and owner check. Built: the invitation expiry setting is applied when inviting and when resending (it was stored but ignored, a fixed seven days), and a change that takes effect is audited (warning when it opens public sharing or drops the MFA requirement). Tests to update: `UpdateWorkspaceSettingsCommandHandlerTests` (new `IAuditService` argument), invitation expiry tests. Real e2e; design diff.

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
- Evidence: BE `GET /api/v1/workspaces/{id}/security-ext`, `GetWorkspaceSecurityExtQueryHandler` · FE `use-teams-ext.ts`, security route · Design `Final-Settings`, `Final-Teams` · Tests `WorkspaceSecurityExtIntegrationTests`, `workspace-teams.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — Tests and owner check: real e2e.

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
- Evidence: BE `POST /api/v1/workspaces/{id}/spaces`, `CreateSpaceCommandHandler` · FE `create-space-dialog.tsx`, `use-create-space.tsx`, `spaces.service.ts` · Design `Final-Space`, `Final-M-Space` · Tests `SpaceTests`, `SpaceHierarchyTests`, `CreateSpaceCommandHandlerTests`, `SpaceHierarchyIntegrationTests`, `SpaceEndpointTests`, `workspace-spaces.mock.e2e.spec.ts` · Real e2e workspace-spaces.real.e2e.spec.ts
- State: HARDENING — Runs on the real stack (`workspace-spaces.real.e2e.spec.ts`): Create department in the tree makes a Private Department and opens it; a sibling with the same name in another case is refused, a Member is refused 403, a parent outside the workspace is refused, a retry under one key makes one Space and another body under that key is refused. Left for DONE: Owner check; design diff at 3 widths and both themes; axe and keyboard run. Acceptance 2026-11-02 at `6ac912d0c` (independent acceptance agent, branch accept/batch-1), stays HARDENING. Line 4 holds: `workspace-spaces.real.e2e.spec.ts` 2 passed. Line 3 gaps: the department tree in the shell was seen at 1440, 1024 and 390 in light and dark (no overflow, axe clean) but the create-department dialog, the Space page and the placement dialog were not screenshotted; `design-fidelity-space.mock.e2e.spec.ts` 10 passed. At 390 px the four shell top-bar icon buttons (navigation 32, command palette 32, theme 32, notifications 36) are under 44 px on every workspace screen. Line 1: the named backend classes in the Domain, Application, API and Integration projects ran and passed (their cases were not audited one by one against the line); the Architecture, Platform and Infrastructure projects were not run, so `WorkspaceScopedArchitectureTests`, `SendInvitationEmailConsumerTests` and `SuspendMemberCommandHandlerTests` are unrun where an Evidence line names them. Backend runs of this acceptance: Domain 200, Application 150 and API 97 (filtered to the classes this use case names, 0 failed) and Postgres Integration 229 (0 failed).

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
- Evidence: BE `GET .../spaces/tree`, `GET .../spaces`, `GetSpaceTreeQueryHandler`, `GetWorkspaceSpacesQueryHandler` · FE `space-tree-nav.tsx`, `use-spaces.ts`, `use-space-tree-expansion.ts` · Design `Final-Space`, `BC-Shell-1440` · Tests `SpaceAccessMatrixIntegrationTests`, `CollectionVisibilityIntegrationTests`, `SpaceHierarchyHandlerTests` · Real e2e workspace-spaces.real.e2e.spec.ts
- State: HARDENING — Runs on the real stack (`workspace-spaces.real.e2e.spec.ts`, a second test): the Owner reads a Private Space, a Member's tree, list and read omit it (404), the nav tree shows the Member only the open Space, both reads are cursor pages that cover every Space once and refuse limits 0 and 201 and a made-up cursor. Left for DONE: Owner check; windowing (virtualising) of a very large tree in the nav; design diff at 3 widths and both themes. Acceptance 2026-11-02 at `6ac912d0c` (independent acceptance agent, branch accept/batch-1), stays HARDENING. Line 4 holds: `workspace-spaces.real.e2e.spec.ts` 2 passed. Line 3 gaps: the department tree in the shell was seen at 1440, 1024 and 390 in light and dark (no overflow, axe clean) but the create-department dialog, the Space page and the placement dialog were not screenshotted; `design-fidelity-space.mock.e2e.spec.ts` 10 passed. At 390 px the four shell top-bar icon buttons (navigation 32, command palette 32, theme 32, notifications 36) are under 44 px on every workspace screen. Line 1: the named backend classes in the Domain, Application, API and Integration projects ran and passed (their cases were not audited one by one against the line); the Architecture, Platform and Infrastructure projects were not run, so `WorkspaceScopedArchitectureTests`, `SendInvitationEmailConsumerTests` and `SuspendMemberCommandHandlerTests` are unrun where an Evidence line names them. Backend runs of this acceptance: Domain 200, Application 150 and API 97 (filtered to the classes this use case names, 0 failed) and Postgres Integration 229 (0 failed).

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
- Evidence: BE `GET .../spaces/{spaceId}`, `GetSpaceQueryHandler` · FE `spaces.service.ts` · Design `Final-Space` · Tests `SpaceAccessMatrixIntegrationTests`, `SpaceEndpointTests` · Real e2e none
- State: INTEGRATING — Tests and owner check. Built: the read returns the version (the client re-reads it between the writes of one save). Real e2e.

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
- Evidence: BE `PUT .../spaces/{spaceId}`, `PUT .../spaces/{spaceId}/description`, `RenameSpaceCommandHandler`, `UpdateSpaceDescriptionCommandHandler` · FE `space-settings-panel.tsx`, `spaces.service.ts` · Design `Final-Space` · Tests `SpaceTests`, `SpaceVersionTests`, `space-page.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — Tests and owner check. Built: `expectedVersion` on rename and description (body), mapped in the expected-version target map; the Space page sends the node version and, when one save changes several fields, re-reads the version between the writes. Tests to update: rename and description handler and integration tests (command shape), `SpaceVersionTests`. Real e2e.

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
- Evidence: BE `PUT .../spaces/{spaceId}/type`, `ChangeSpaceTypeCommandHandler` · FE `spaces.service.ts` · Design `Final-Space` · Tests `SpaceTests` · Real e2e none
- State: INTEGRATING — Tests and owner check. Built: `expectedVersion` on the type write (same path as K04). Not built: a control to change the type (the settings panel shows the type but offers no control; no artboard shows one). Real e2e.

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
- Evidence: BE `PUT .../spaces/{spaceId}/visibility`, `ChangeSpaceVisibilityCommandHandler` · FE `space-settings-panel.tsx` · Design `Final-Space` · Tests `SpaceAccessMatrixIntegrationTests`, `space-page.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — Tests and owner check. Built: `expectedVersion` on the visibility write (same path as K04). Real e2e.

### UC-WSP-K07 Move or reorder a Space
- Capability: K Spaces (added by this catalog)
- Actors and permission: Owner, Admin, Space Admin (`ManageSpaces`)
- Preconditions: Target parent Active, same workspace, not a descendant.
- Input: `parentId`, neighbour keys, `expectedVersion`.
- Output: The subtree moves; position key recomputed.
- Main flow: 1. `POST .../spaces/{spaceId}/move`. 2. Application supplies ancestry. 3. Domain rejects cycles. 4. Closure rebuilt.
- No-op / rejection / concurrency / retry: Under itself or a descendant: refused before mutation. Archived or deleted parent: refused. Stale neighbour: refused. Stale version: 409.
- UI states: Drag or picker (Move dialog), conflict, pending.
- Rules: [workspaces](../../product/workspaces.md) section 43.1; WSP-021; WGREQ038; WX-SPACE-004.
- Acceptance: Given A with child B, When A moves under B, Then refused and nothing changes.
- Evidence: BE `POST .../spaces/{spaceId}/move`, `MoveSpaceCommandHandler` · FE `move-to-space-dialog.tsx` (Boards and Docs), Space settings · Design `Final-Space` · Tests `SpaceHierarchyTests`, `SpaceHierarchyIntegrationTests` · Real e2e none
- State: INTEGRATING — Tests and owner check. The Move picker in Space settings already sends the version. Not built: dragging in the tree (no artboard shows it; the picker is the keyboard path). Real e2e.

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
- Evidence: BE `POST .../archive`, `POST .../unarchive`, `ArchiveSpaceCommandHandler`, `UnarchiveSpaceCommandHandler` · FE `space-settings-panel.tsx` · Design `Final-Space` · Tests `ArchiveSpaceCommandHandlerTests`, `space-page.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — Tests and owner check. Built: `expectedVersion` on archive and unarchive (new body), sent by the Space page. Tests to update: `ArchiveSpaceCommandHandlerTests` and the integration tests that archive a Space (command shape). Real e2e.

### UC-WSP-K09 Delete or restore a Space
- Capability: K Spaces (added by this catalog)
- Actors and permission: Owner, Admin, Space Admin (`ManageSpaces`)
- Preconditions: Delete: no non-deleted child and no non-deleted Board or Page in it.
- Input: `spaceId`.
- Output: Soft deletion, or restore when the parent is live.
- Main flow: 1. `DELETE .../spaces/{spaceId}` or `POST .../restore`. 2. Application supplies facts. 3. Domain checks.
- No-op / rejection / concurrency / retry: Delete with content: refused. Restore under an archived or deleted parent: refused. No retention window or purge is defined for deleted Spaces (LD-16). No expected version.
- UI states: Confirmation, blocked reason, pending. The mock page keeps Delete inert.
- Rules: [workspaces](../../product/workspaces.md) section 43.2; WSP-022; WGREQ039, WGREQ006.
- Acceptance: Given an empty Space, When deleted, Then it leaves the tree and restore returns it.
- Evidence: BE `DELETE .../spaces/{spaceId}`, `POST .../restore`, `DeleteSpaceCommandHandler`, `RestoreSpaceCommandHandler` · FE none (Delete stays inert in the page) · Design `Final-Space` · Tests `SpaceTests` · Real e2e none
- State: IMPLEMENTING — Frontend calls; trash and retention rule; expected version.

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
- Evidence: BE `GET .../members`, `GET .../members-extra`, `GetSpaceMembersQueryHandler`, `GetSpaceMembersExtraQueryHandler` · FE `space-members-view.tsx`, `use-space-members-extra.ts` · Design `Final-Space`, `Final-M-Space` · Tests `SpaceMembersExtraIntegrationTests`, `space-members.mock.e2e.spec.ts`, `design-fidelity-members.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — Tests and owner check. Not built: paging of the members-extra read. The tab filters and counts people client-side over the whole set, so paging it needs a server-side filter and search contract: owner call. Real e2e; design diff.

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
- Evidence: BE `POST .../spaces/{spaceId}/members`, `AddSpaceMemberCommandHandler` · FE `space-members-dialogs.tsx`, `spaces.service.ts` · Design `Final-Space` · Tests `SpaceRulesTests`, `SpaceAccessMatrixIntegrationTests`, `space-members.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — Real e2e; design diff.

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
- Evidence: BE `PATCH .../members/{subjectType}/{subjectId}`, `ChangeSpaceMemberRoleCommandHandler` · FE `space-members-view.tsx` · Design `Final-Space` · Tests `SpaceRulesTests`, `SpaceVersionTests`, `space-members.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — Real e2e.

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
- Evidence: BE `DELETE .../members/{subjectType}/{subjectId}`, `RemoveSpaceMemberCommandHandler` · FE `space-members-dialogs.tsx` · Design `Final-Space` · Tests `SpaceRulesTests`, `space-members.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — Real e2e.

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
- State: INTEGRATING — Real e2e; design diff; scale test for large Spaces.

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
- Evidence: BE `Public/SpacePlacement`, `SpaceSubtree` ports · FE `move-to-space-dialog.tsx`, `board-placement-bridge.ts` · Design `Final-Space` · Tests `SpacePlacementIntegrationTests`, `SpacePlacementDomainTests` · Real e2e workspace-spaces.real.e2e.spec.ts
- State: HARDENING — Runs on the real stack (`workspace-spaces.real.e2e.spec.ts`, a second test): a Board created in a Space is placed there; a move into a Space of another workspace and into an archived Space is refused and the Board stays; a move into a Space of its own workspace is read back. Left for DONE: Owner check; the placement dialog on the Board side and a Doc placement driven in a browser; design diff. Acceptance 2026-11-02 at `6ac912d0c` (independent acceptance agent, branch accept/batch-1), stays HARDENING. Line 4 holds: `workspace-spaces.real.e2e.spec.ts` 2 passed. Line 3 gaps: the department tree in the shell was seen at 1440, 1024 and 390 in light and dark (no overflow, axe clean) but the create-department dialog, the Space page and the placement dialog were not screenshotted; `design-fidelity-space.mock.e2e.spec.ts` 10 passed. At 390 px the four shell top-bar icon buttons (navigation 32, command palette 32, theme 32, notifications 36) are under 44 px on every workspace screen. Line 1: the named backend classes in the Domain, Application, API and Integration projects ran and passed (their cases were not audited one by one against the line); the Architecture, Platform and Infrastructure projects were not run, so `WorkspaceScopedArchitectureTests`, `SendInvitationEmailConsumerTests` and `SuspendMemberCommandHandlerTests` are unrun where an Evidence line names them. Backend runs of this acceptance: Domain 200, Application 150 and API 97 (filtered to the classes this use case names, 0 failed) and Postgres Integration 229 (0 failed).

## L. Teams (added by this catalog)

### UC-WSP-L01 Create a Team
- Capability: L Teams (added by this catalog)
- Actors and permission: Owner, Admin (`ManageTeams`)
- Preconditions: Workspace Active.
- Input: `name`, `description`.
- Output: A Team (Active) inside the workspace.
- Main flow: 1. `POST /api/v1/workspaces/{id}/teams`. 2. Domain creates.
- No-op / rejection / concurrency / retry: Duplicate name rule is not stated in the product (see Part 5; owner decision, not changed). A retry under the same Idempotency-Key returns the Team the first attempt made; a different body under that key is refused.
- UI states: Loading, success, validation error, permission denied, conflict, pending, offline, destructive confirmation.
- Rules: [workspaces](../../product/workspaces.md) sections 45, 78; WSP-014, WSP-025; WGREQ034; WX-TEAM-001.
- Acceptance: Given an Admin, When they create `Design`, Then the Team exists with no members.
- Evidence: BE `POST /api/v1/workspaces/{id}/teams`, `CreateTeamCommandHandler` · FE `create-team-dialog.tsx`, `use-team-mutations.ts` · Design `Final-Teams` · Tests `TeamTests`, `TeamEndpointTests`, `workspace-teams.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — Tests and owner check. Built: the create takes an Idempotency-Key (endpoint, dialog hook keeps one key per input, mock replays by key). Open owner decision: whether Team names are unique in a workspace. Tests to update: `TeamEndpointTests` (key). Real e2e; design diff.

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
- Evidence: BE `GET .../teams`, `GET .../teams/{teamId}`, `GetWorkspaceTeamsQueryHandler`, `GetTeamQueryHandler` · FE `workspace-teams-surface.tsx`, `use-teams.ts` · Design `Final-Teams` · Tests `TeamEndpointTests`, `workspace-teams.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — Tests and owner check. Built: cursor page on the list (shared name cursor with the Space list), the Team service reads every page, the mock pages. Tests to update: `TeamEndpointTests`, `TeamAdministrationReadIntegrationTests` (the list now returns a page), OpenAPI shape tests. Real e2e.

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
- Evidence: BE `GET .../teams/{teamId}/members`, `GetTeamMembersQueryHandler` · FE `workspace-team-surface.tsx` · Design `Final-Teams` · Tests `TeamAdministrationReadIntegrationTests`, `workspace-teams.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — Real e2e.

### UC-WSP-L04 Rename a Team or edit its description
- Capability: L Teams (added by this catalog)
- Actors and permission: Owner, Admin (`ManageTeams`)
- Preconditions: Team Active.
- Input: `name` or `description`, `expectedVersion`.
- Output: Updated Team, version +1.
- Main flow: 1. `PUT .../teams/{teamId}` or `PUT .../teams/{teamId}/description`. 2. Version check.
- No-op / rejection / concurrency / retry: Same value: no-op. Stale: 409. Archived Team: accepts no change.
- UI states: Inline edit, conflict, pending.
- Rules: [workspaces](../../product/workspaces.md) section 48.2; WGREQ036.
- Acceptance: Given a stale version, Then 409 and no change.
- Evidence: BE `PUT .../teams/{teamId}`, `PUT .../description`, `RenameTeamCommandHandler`, `UpdateTeamDescriptionCommandHandler` · FE `use-team-mutations.ts` · Design `Final-Teams` · Tests `TeamVersionTests`, `TeamWriteConcurrencyIntegrationTests` · Real e2e none
- State: INTEGRATING — Real e2e.

### UC-WSP-L05 Add a person to a Team
- Capability: L Teams (added by this catalog)
- Actors and permission: Owner, Admin (`ManageTeams`)
- Preconditions: Person is an active non-Guest member of the same workspace; Team Active.
- Input: `userId`, `role` (Member or Lead), `expectedVersion`.
- Output: Team membership; Team version +1.
- Main flow: 1. `POST .../teams/{teamId}/members`. 2. Version check. 3. Domain adds.
- No-op / rejection / concurrency / retry: Foreign or Guest user: refused. Duplicate: refused or no-op. Stale: 409.
- UI states: Add dialog, conflict, pending.
- Rules: [workspaces](../../product/workspaces.md) sections 46, 78; WSP-014; WGREQ035.
- Acceptance: Given a foreign user id, Then refused and nothing changes.
- Evidence: BE `POST .../teams/{teamId}/members`, `AddTeamMemberCommandHandler` · FE `workspace-team-surface.tsx`, `use-team-mutations.ts` · Design `Final-Teams` · Tests `TeamMemberTests`, `TeamRulesTests`, `TeamWriteConcurrencyIntegrationTests` · Real e2e none
- State: INTEGRATING — Real e2e.

### UC-WSP-L06 Change a Team member's role
- Capability: L Teams (added by this catalog)
- Actors and permission: Owner, Admin (`ManageTeams`)
- Preconditions: Member exists.
- Input: `userId`, `newRole`, `expectedVersion`.
- Output: New Team role. The last Lead cannot be downgraded.
- Main flow: 1. `PUT .../teams/{teamId}/members/{userId}/role`.
- No-op / rejection / concurrency / retry: Last Lead: refused (TeamLeadRules). Stale: 409.
- UI states: Role menu, reason when blocked.
- Rules: [workspaces](../../product/workspaces.md) section 47; WSP-014.
- Acceptance: Given one Lead, When lowered, Then refused.
- Evidence: BE `PUT .../members/{userId}/role`, `ChangeTeamMemberRoleCommandHandler` · FE `use-team-mutations.ts` · Design `Final-Teams` · Tests `TeamRulesTests`, `TeamMemberTests` · Real e2e none
- State: INTEGRATING — Real e2e.

### UC-WSP-L07 Remove a person from a Team
- Capability: L Teams (added by this catalog)
- Actors and permission: Owner, Admin (`ManageTeams`)
- Preconditions: Member exists.
- Input: `userId`, `expectedVersion`.
- Output: Membership Removed.
- Main flow: 1. `DELETE .../teams/{teamId}/members/{userId}?expectedVersion=`.
- No-op / rejection / concurrency / retry: Last Lead: refused. Stale: 409.
- UI states: Confirmation naming lost access.
- Rules: [workspaces](../../product/workspaces.md) section 46; GOV-026.
- Acceptance: Given a person with Team-based access, When removed, Then that access ends.
- Evidence: BE `DELETE .../members/{userId}`, `RemoveTeamMemberCommandHandler` · FE `use-team-mutations.ts` · Design `Final-Teams` · Tests `TeamWriteConcurrencyIntegrationTests`, `workspace-teams.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — Real e2e.

### UC-WSP-L08 Archive or unarchive a Team
- Capability: L Teams (added by this catalog)
- Actors and permission: Owner, Admin (`ManageTeams`)
- Preconditions: Team exists.
- Input: `teamId`, `expectedVersion`.
- Output: Archived Team grants nothing and accepts no change; unarchive restores it.
- Main flow: 1. `POST .../teams/{teamId}/archive` or `.../unarchive`.
- No-op / rejection / concurrency / retry: Already in the state: no-op. Stale: 409. The row menu says who loses access first.
- UI states: Confirmation naming consequences, pending.
- Rules: [workspaces](../../product/workspaces.md) sections 44.3, 48.1; WSP-024, WSP-025; WGREQ036.
- Acceptance: Given a Team with a Space entry, When archived, Then its members lose that role.
- Evidence: BE `POST .../archive`, `POST .../unarchive`, `ArchiveTeamCommandHandler`, `UnarchiveTeamCommandHandler` · FE `use-team-mutations.ts` · Design `Final-Teams` · Tests `TeamVersionTests`, `workspace-teams.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — Real e2e.

### UC-WSP-L09 Delete or restore a Team
- Capability: L Teams (added by this catalog)
- Actors and permission: Owner, Admin (`ManageTeams`)
- Preconditions: Team exists.
- Input: `teamId`, `expectedVersion` (delete).
- Output: Soft deletion or restore.
- Main flow: 1. `DELETE .../teams/{teamId}?expectedVersion=` or `POST .../teams/{teamId}/restore`.
- No-op / rejection / concurrency / retry: Restore has no expected version. References by Space entries and permissions need a rule (LD-16).
- UI states: Confirmation, pending.
- Rules: [workspaces](../../product/workspaces.md) section 48.1; WGREQ036.
- Acceptance: Given a deleted Team, When restored, Then its members return.
- Evidence: BE `DELETE .../teams/{teamId}`, `POST .../restore`, `DeleteTeamCommandHandler`, `RestoreTeamCommandHandler` · FE none · Design `Final-Teams` · Tests `TeamVersionTests` · Real e2e none
- State: IMPLEMENTING — Frontend calls; reference rule; expected version on restore.

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
- State: INTEGRATING — Board usage; real e2e; design diff.

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
- Evidence: BE `GET`, `PUT`, `DELETE /api/v1/workspaces/onboarding-draft`, `SaveOnboardingDraftCommandHandler`, `GetOnboardingDraftQueryHandler`, `ClearOnboardingDraftCommandHandler` · FE `onboarding-api.ts`, `onboarding-page.tsx` · Design `Final-Onboarding` · Tests `OnboardingDraftTests`, `OnboardingIntegrationTests`, `onboarding.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — Tests and owner check. Built: the onboarding API moved from the web app route folder into the workspace feature package (`web/onboarding/onboarding-api.ts`, exported as `createOnboardingApi`); the app keeps only the hook that hands it the runtime client. Test to update: `onboarding-api.unit.test.ts` now imports from the package. Real e2e.

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
- Evidence: BE `OnboardingDraftSweepService` · FE none · Design none · Tests `OnboardingIntegrationTests` · Real e2e none
- State: INTEGRATING — No real e2e; it is a system use case.

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
- Evidence: BE `GET .../slug-check`, `CheckWorkspaceSlugQueryHandler` · FE `onboarding-api.ts`, onboarding address step · Design `Final-Onboarding` · Tests `OnboardingIntegrationTests`, `WorkspaceSlugAndProfileTests`, `onboarding.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — Real e2e.

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
- Evidence: BE commands of A01, K01, G01 · FE `onboarding-surface.tsx`, `onboarding-steps.tsx`, `onboarding-page.tsx` · Design `Final-Onboarding` · Tests `onboarding.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — Tests and owner check. Built: every create step (workspace, each department, each invitation, the first Board) keeps one Idempotency-Key while the same request is retried and forgets it once it succeeded or was refused for good, so a retry after a lost answer returns what the first attempt made. Real e2e of the full run.

### UC-WSP-M05 Join through an invitation and set my look and notifications
- Capability: M Getting started and onboarding (added by this catalog)
- Actors and permission: The invited person
- Preconditions: A valid invitation (H01 to H03).
- Input: Name, theme, notification preferences.
- Output: Membership plus preferences stored by Collaboration (per workspace, in-app and email).
- Main flow: 1. Join page. 2. Accept (H02 or H03). 3. Preferences are written through the Collaboration matrix.
- No-op / rejection / concurrency / retry: No preference contract of its own in Workspaces; Guests may set them.
- UI states: Join form, success.
- Rules: [workspaces](../../product/workspaces.md) section 48.4; see [collaboration](collaboration.md).
- Acceptance: Given a new member, Then preferences exist for the workspace.
- Evidence: BE Collaboration preference endpoints · FE `onboarding-join-surface.tsx` · Design `Final-Onboarding` · Tests `onboarding.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — Real e2e.

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
- Evidence: BE `GET .../getting-started`, `GetGettingStartedQueryHandler` · FE `getting-started-card.tsx`, `home-getting-started.tsx` · Design `Final-Home`, `Final-Onboarding` · Tests `OnboardingIntegrationTests`, `onboarding.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — Real e2e.

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
- Evidence: BE `POST .../getting-started/dismiss`, `DismissGettingStartedCommandHandler` · FE `getting-started-card.tsx` · Design `Final-Home` · Tests `OnboardingIntegrationTests` · Real e2e none
- State: INTEGRATING — Real e2e.

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
- Evidence: BE reads of N04, N05, M06 and Work Management feeds · FE `workspace-home-surface.tsx`, route `home.tsx` · Design `Final-Home`, `Final-M-Home` · Tests `home.mock.e2e.spec.ts`, `design-fidelity-home.mock.e2e.spec.ts`, `real-pages-guard.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — Tests and owner check. Reviewed, nothing to build: the sections that fail show their own error, and the sections without a producer (approvals, mentions, schedule) stay hidden. Real e2e; design diff.

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
- State: IMPLEMENTING — Real source from Collaboration through a port.

### UC-WSP-N04 Remember recently opened resources
- Capability: N Home, navigation, search and activity (added by this catalog)
- Actors and permission: Any member (`ViewWorkspace`)
- Preconditions: Active membership.
- Input: `kind`, `resourceId` (`Idempotency-Key`).
- Output: A recent entry per member and resource; only the newest 30 are kept.
- Main flow: 1. `POST /api/v1/workspaces/{id}/recents`. 2. `GET /api/v1/workspaces/{id}/recents` lists them newest first.
- No-op / rejection / concurrency / retry: A repeat updates the time. Kinds are an allowed set. Entries the caller can no longer read must not be shown.
- UI states: Continue list, empty.
- Rules: [workspaces](../../product/workspaces.md) sections 33 to 35; DESIGN.md section 27.
- Acceptance: Given 31 opened resources, Then 30 remain.
- Evidence: BE `POST` and `GET .../recents`, `RecordRecentResourceCommandHandler`, `GetRecentResourcesQueryHandler` · FE `navigation.service.ts`, `use-workspace-navigation.ts` · Design `Final-Home`, `Final-Command` · Tests `WorkspaceNavigationTests`, `WorkspaceNavigationHandlerTests`, `WorkspaceNavigationIntegrationTests`, `command-palette.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — Tests and owner check. Reviewed, nothing to build: `GetRecentResourcesQueryHandler` already drops references the caller can no longer read (Governance Board and Page visibility sets), the record is idempotent and checks the resource is in the workspace, and 30 are kept. Proof needed: a test that a revoked Board or Page leaves the list. Real e2e.

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
- Evidence: BE `PUT`, `DELETE`, `GET .../pins`, `PinResourceCommandHandler`, `UnpinResourceCommandHandler`, `GetPinnedResourcesQueryHandler` · FE `use-workspace-navigation-mutations.ts`, sidebar · Design `Final-Home`, `BC-Shell-1440` · Tests `WorkspaceNavigationTests`, `workspace-pages.mock.e2e.spec.ts` (WX-DIR-001) · Real e2e none
- State: INTEGRATING — Tests and owner check. Reviewed, nothing to build: the pin limit, the no-ops and the readability filter on the read are in place (`GetPinnedResourcesQueryHandler` uses the same visibility sets as recents). Real e2e.

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
- Evidence: BE `GET .../command-search`, `CommandSearchQueryHandler` · FE `command-palette-surface.tsx`, `use-command-search.ts` · Design `Final-Command`, `Final-M-Search` · Tests `CommandSearchQueryHandlerTests`, `command-palette.mock.e2e.spec.ts`, `design-fidelity-command.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING — Tests and owner check. Reviewed, nothing to build: `CommandSearchQueryHandler` selects only through the Governance visibility sets and the producers' search facts, answers empty under two characters and stores nothing. Proof needed: the cross-tenant negative at HTTP. Real e2e; design diff.

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
12. Section 48.2: state that workspace invitation lists need `ViewMembers`, and lists page by cursor (default 50, max 200).
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

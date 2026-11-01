---
document_id: WRK-UC-GOVERNANCE
document_type: workstream-use-case-catalog
status: active
owner: engineering-delivery
applies_to:
  - governance
  - authorization
  - resource-access
  - guests
  - share-links
  - security-audit
  - api-tokens
  - admin-plane
evidence:
  - docs/product/governance.md
  - docs/workstreams/teams/workspace-governance.md
  - docs/workstreams/executions/workspace-governance/workspace-governance.spec.md
  - docs/workstreams/use-cases/README.md
  - backend/src/Notrelix.API/Endpoints/Governance
  - backend/src/Notrelix.API/Endpoints/Admin
  - backend/src/Notrelix.Application/Features/Governance
  - frontend/packages/features/governance
review_on:
  - capability-state-change
  - product-semantic-change
  - permission-vocabulary-change
  - share-link-change
  - audit-semantics-change
---

# Use cases: Governance

This file lists every use case of the Governance context, grouped by the capabilities of the [Workspace & Governance team spec](../teams/workspace-governance.md). Product meaning is in [Governance](../../product/governance.md). Workspace membership, invitations, Teams and Spaces are in [Workspaces use cases](workspaces.md) and are linked, not repeated. The rules of this catalog are in the [catalog README](README.md).

Scope note. The Governance API surface is small: `Endpoints/Governance` holds resource permissions and share links; `Endpoints/Admin` holds outbox diagnostics for the platform operator. The API-token endpoints sit under the Identity folder and are covered here as governance credentials. The "admin plane" of the roadmap (workspace and account administration) is not built and its owner decisions are pending, so its use cases are BLOCKED.

# 1. Assessment

## 1.1 Capabilities

Capability letters follow the Governance list of the team spec, in order.

| Letter | Capability | Use cases | DEFINED | IMPLEMENTING | INTEGRATING | HARDENING | DONE | BLOCKED |
|---|---|---|---|---|---|---|---|---|
| A | WG-GOV resource/action model | 4 | 1 | 2 | 0 | 1 | 0 | 0 |
| B | Built-in roles | 5 | 0 | 1 | 2 | 2 | 0 | 0 |
| C | Custom roles | 6 | 4 | 0 | 0 | 0 | 0 | 2 |
| D | Permission assignment | 15 | 2 | 2 | 6 | 4 | 0 | 1 |
| E | Effective permission evaluation | 12 | 1 | 5 | 2 | 4 | 0 | 0 |
| F | Protected operation integration | 5 | 2 | 1 | 1 | 1 | 0 | 0 |
| G | Share-link lifecycle and policy | 10 | 3 | 2 | 3 | 0 | 0 | 2 |
| H | Governance administration UI | 14 | 5 | 6 | 1 | 0 | 0 | 2 |
| I | Architecture coverage for resource kinds and actions | 3 | 1 | 0 | 0 | 2 | 0 | 0 |
| | Total | 74 | 19 | 19 | 15 | 14 | 0 | 7 |

No use case is DONE: none has a recorded owner check or a 1-px design diff, and none meets every line of README section 7. The execution pack certifies Governance parts as VERIFIED or PARTIALLY_VERIFIED on a base commit ([certification](../executions/workspace-governance/workspace-governance.certification.md), records P2B-CERT-004 to P2B-CERT-016); that is backend proof only.

## 1.2 The ten most important gaps

1. Share links cannot be used. They can be created and disabled, but no endpoint resolves a token, so no visitor can open one (UC-GOV-G03, BLOCKED on the owner decision for an anonymous principal).
2. The workspace sharing policy is stored and shown but never enforced when a link is created (UC-GOV-G06).
3. Custom roles exist only in the Domain. There is no handler, endpoint, screen or design, and the frontend package calls routes that do not exist (UC-GOV-C01 to C06).
4. There is no audit-log read path, no screen and no export (UC-GOV-H02 to H04), although audit rows are written.
5. Audit coverage is thin. Only permission grant and revoke, resource invitations and Board access changes are recorded. Role change, member removal, API-token issue and revoke, share-link create and disable and sharing-policy change write nothing (UC-GOV-F04).
6. API tokens have a finished backend (Owner only, step-up proof) and no screen, no design and no lifetime cap (UC-GOV-H05 to H08).
7. Competing writes on permissions carry no expected version, and revoke of an already revoked grant returns NotFound instead of a safe no-op (UC-GOV-D03, D04).
8. Expiry of a resource entry (GOV-031) conflicts with the owner decision that excludes expiry; the setting `guestDefaultExpiryDays` exists with nothing behind it (UC-GOV-D12, BLOCKED).
9. Admin-plane use cases wait for owner decisions 1 to 9 (UC-GOV-H13, H14, BLOCKED).
10. Real end-to-end proof is thin: only the Board grant, change and revoke journey and one Guest read check run against the real backend (`frontend/e2e/real/board-sharing.real.e2e.spec.ts`, `guest-permission.real.e2e.spec.ts`). Page sharing, Team grants, invitations and the Sharing and security page have none.

## 1.3 Defects in shipped code

| Defect | Where |
|---|---|
| Share-link policy not enforced: `CreateShareLinkCommandHandler` never reads `AllowPublicSharing`. | `backend/src/Notrelix.Application/Features/Governance/ShareLinks/Commands/CreateShareLink/CreateShareLinkCommand.cs` |
| Share-link create ignores the request `Level`, hard-codes access mode `WorkspaceOnly`, returns the token hash in the DTO, writes no audit row and has no idempotency key. | same file |
| Share-link disable is authorized as `ManageWorkspace` on a `governance.share-link` resource, writes no audit row, and does not compare the route's resource id with the link's. | `.../ShareLinks/Commands/DisableShareLink/DisableShareLinkCommand.cs` |
| Grant endpoint accepts `ExpiresAt` in the API contract but the validator always rejects it; the field is dead on the wire. | `.../ResourcePermissions/Commands/GrantResourcePermission/GrantResourcePermissionCommandValidator.cs`, `Notrelix.API/Endpoints/Governance/ResourcePermissions/Commands/GrantResourcePermissionEndpoint.cs` |
| Grant and revoke carry no expected version; revoke of a revoked grant gives NotFound, not an idempotent result. | `GrantResourcePermissionCommand.cs`, `RevokeResourcePermissionCommand.cs` |
| Permission list is unbounded and the frontend does not call it (it reads the sharing summary). | `.../Queries/GetResourcePermissions/GetResourcePermissionsQuery.cs`, `frontend/packages/foundation/contracts/src/endpoints/endpoints.ts` |
| Dead frontend package: `features/governance` defines roles, permissions and audit-log routes with no backend and no importer. | `frontend/packages/features/governance/src/core/api/governance.service.ts` |
| Dead Domain: `CustomRole`, `MemberRoleAssignment`, `PermissionTemplate`, `FieldPermission`, `GuestAccessPolicy`, `ResourcePolicy`, `SharingPolicy` have no Application use case (`GuestAccessPolicy` has no Application reference at all). | `backend/src/Notrelix.Domain/Governance` |
| Audit service fills no subject, before or after fields and no correlation id. | `backend/src/Notrelix.Infrastructure/Auditing/AuditService.cs` |
| The 90-day retention of `resource_access_changes` has no purge job in source. | `backend/src/Notrelix.Infrastructure/Data/Authz/EfResourceAccessChangeReader.cs` and configuration |
| Realtime authorization is checked at subscribe time; ending access while connected (GOV-018) was not found in source or tests. | `backend/src/Notrelix.API/Realtime/RealtimeAuthorization.cs` |
| Real spec accepts 403 or 404 for a hidden Board where README section 7 demands identical NotFound. | `frontend/e2e/real/guest-permission.real.e2e.spec.ts` |
| Mock-only frontend paths: the Sharing and security page and Space and Team extras run only against mock handlers in e2e (`frontend/e2e/mock/workspace-teams.mock.e2e.spec.ts`). | `frontend/apps/web/src/routes/workspaces/$workspaceId/security.tsx` |
| API tokens: unbounded list, no expiry required, no audit. | `backend/src/Notrelix.Application/Features/Identity/ApiTokens` |

## 1.4 Enterprise baseline

Leaders compared: Atlassian (organization audit log), Asana (roles and admin console), Notion (sharing and audit log), Slack (audit logs API), GitHub (token policy). Sources: [Atlassian audit log export](https://support.atlassian.com/security-and-access-policies/docs/export-audit-logs/), [Atlassian view audit log](https://support.atlassian.com/security-and-access-policies/docs/view-audit-log-activities/), [Asana custom roles](https://help.asana.com/s/article/role-based-access-control-with-custom-roles), [Asana permissions management add-on](https://help.asana.com/s/article/permissions-management-add-on), [Notion sharing and permissions](https://www.notion.com/help/sharing-and-permissions), [Notion audit log](https://Notion.com/help/audit-log), [Slack audit logs API](https://docs.slack.dev/tools/java-slack-sdk/guides/audit-logs-api), [GitHub token policy](https://docs.github.com/en/organizations/managing-programmatic-access-to-your-organization/setting-a-personal-access-token-policy-for-your-organization).

| Capability of the leaders | Notrelix today | Use case |
|---|---|---|
| Admin-read audit log with filters and CSV export, limited window (Atlassian: 180 days, 10,000 rows), read-only, Enterprise tier (Notion, Slack) | Rows written, nothing readable | UC-GOV-H02, H03, H04 (enterprise baseline) |
| Custom roles built on standard roles, set in the admin console (Asana Enterprise) | Domain only | UC-GOV-C01 to C06 |
| Guest controls: admins may block guest invites, restrict by domain, auto-remove idle guests (Notion, Asana) | Company domain and guest count shown; no switch | UC-GOV-G10 (enterprise baseline) |
| Org-level control of public links (Asana, Notion) | Policy flag stored, not enforced | UC-GOV-G06 |
| List of active public links per resource with revoke | Create and disable only | UC-GOV-G04 (enterprise baseline) |
| Token policy: maximum lifetime, org-wide (GitHub) | No cap | UC-GOV-H08 (enterprise baseline) |
| Audit of the audit: reads and exports are logged | None | UC-GOV-H04 |
| Security events and denied attempts recorded for sensitive actions | None | UC-GOV-F05 (enterprise baseline) |

# 2. Index

Letter to capability map (Governance list of the team spec, in order): A resource/action model; B built-in roles; C custom roles; D permission assignment; E effective permission evaluation; F protected operation integration; G share-link lifecycle and policy; H governance administration UI (it also holds audit, API tokens and the admin plane); I architecture coverage for resource kinds and actions. Guests and policies sit under D and G. Columns BE, FE, Design and Real e2e say Yes, Part or No.

| ID | Use case | Capability | Actors | BE | FE | Design | Real e2e | State | Gap |
|---|---|---|---|---|---|---|---|---|---|
| UC-GOV-A01 | Admit a protected resource kind with its action vocabulary | A | Developer of an owning context | Yes | No | No | No | IMPLEMENTING | Kinds exist for Board, Page and workspace; Items, Automation and Integration rely on the workspace kind or the Board. A written ... |
| UC-GOV-A02 | Resolve a resource's kind, workspace and owner from its id | A | Application pipeline (system) | Yes | No | No | Part | HARDENING | tests and owner check |
| UC-GOV-A03 | Classify every permission action in the role baseline | A | Developer | Yes | No | No | No | IMPLEMENTING | Stored-rule migration on rename is specified (§85) but has no test. |
| UC-GOV-A04 | Field-level permission (deferred) | A | Workspace Owner, Admin | Part | No | No | No | DEFINED | Lead decision L11: deferred behind Work Management field work. Domain type exists and is unused. |
| UC-GOV-B01 | Decide a workspace action from the built-in role baseline | B | Server (every protected request) | Yes | Part | Part | Part | HARDENING | tests and owner check |
| UC-GOV-B02 | Decide an account-scoped action from the account role | B | Server | Yes | No | No | No | IMPLEMENTING | No frontend check of denial; no API-level test of the Account cell. |
| UC-GOV-B03 | Keep Owner-only actions Owner-only (Admin never silently becomes Owner) | B | Workspace Owner; Admin is denied | Yes | Yes | Part | No | INTEGRATING | tests and owner check |
| UC-GOV-B04 | A Guest does not enumerate the workspace | B | Guest | Yes | Yes | Yes | Yes | HARDENING | tests and owner check |
| UC-GOV-B05 | A Guest's resource level is capped at Viewer | B | Guest; Board or Page manager who shares | Yes | Yes | Yes | No | INTEGRATING | tests and owner check |
| UC-GOV-C01 | Create a custom role (enterprise baseline) | C | Workspace Owner | Part | No | No | No | DEFINED | No Application use case, no endpoint, no design. The frontend package calls non-existent routes (dead code). |
| UC-GOV-C02 | Change a custom role's name or actions | C | Workspace Owner | Part | No | No | No | DEFINED | No Application use case, no endpoint, no design. The frontend package calls non-existent routes (dead code). |
| UC-GOV-C03 | Archive a custom role that has assignments | C | Workspace Owner | Part | No | No | No | DEFINED | No Application use case, no endpoint, no design. The frontend package calls non-existent routes (dead code). |
| UC-GOV-C04 | Assign a custom role to a workspace member | C | Workspace Owner | Part | No | No | No | BLOCKED | Decision needed from the owner: at which step of the GOV-025 order a custom role's actions apply (as part of the workspace baseline ... |
| UC-GOV-C05 | Remove a custom role from a member | C | Workspace Owner | Part | No | No | No | BLOCKED | Same decision as UC-GOV-C04. |
| UC-GOV-C06 | List roles and the permission catalog | C | Workspace Owner, Admin | No | Part | No | No | DEFINED | No endpoint, no screen, no artboard. Package `features/governance` is not imported by any app. |
| UC-GOV-D01 | Give a workspace member access to a Board or Page | D | Board or Page manager | Yes | Yes | Yes | Yes | HARDENING | tests and owner check |
| UC-GOV-D02 | Give a Team access to a Board or Page | D | Board or Page manager | Yes | Part | Part | No | INTEGRATING | Team picker, then tests and owner check |
| UC-GOV-D03 | Change the level of an existing grant | D | Board or Page manager | Yes | Yes | Yes | Yes | HARDENING | tests and owner check |
| UC-GOV-D04 | Remove someone's access to a Board or Page | D | Board or Page manager | Yes | Yes | Yes | Yes | HARDENING | tests and owner check |
| UC-GOV-D05 | List who holds explicit access on a resource | D | Board or Page manager | Yes | Part | Yes | No | INTEGRATING | tests and owner check |
| UC-GOV-D06 | Refuse an unsafe grant | D | Board or Page manager | Yes | Part | No | No | IMPLEMENTING | No frontend mapping for each refusal; no real e2e of a refusal. |
| UC-GOV-D07 | Invite a Guest to one resource | D | Board or Page manager; Workspace Owner or Admin may invite | Yes | Yes | Yes | No | INTEGRATING | tests and owner check |
| UC-GOV-D08 | A Guest reaches only what was shared with them | D | Guest | Yes | Part | Part | Part | HARDENING | tests and owner check |
| UC-GOV-D09 | Make a Board Private and see who loses it | D | Board manager | Yes | Yes | Yes | No | INTEGRATING | tests and owner check |
| UC-GOV-D10 | Read the sharing and access summary of a Board | D | Anyone who can read the Board | Yes | Yes | Yes | Part | INTEGRATING | tests and owner check |
| UC-GOV-D11 | Keep a 90-day log of Board access changes | D | Board manager reads; system writes | Yes | Yes | Yes | No | BLOCKED | owner decision 3 (retention and deletion) |
| UC-GOV-D12 | End an explicit entry on a date (expiry) | D | Board or Page manager | No | No | No | No | BLOCKED | Owner must decide whether GOV-031 (and the Guest default expiry pre-fill, `guestDefaultExpiryDays`) is built now or removed from the ... |
| UC-GOV-D13 | Explicit permission rules with priority and time window | D | Workspace Owner (future API) | Part | No | No | No | DEFINED | Engine evaluates rules; no command, endpoint or UI creates them. Lead decision L15: not built until a product need exists. |
| UC-GOV-D14 | Apply a permission template | D | Workspace Owner (future) | Part | No | No | No | DEFINED | Domain only; no handler or endpoint. Not part of the owner's next scope. |
| UC-GOV-D15 | Share a Page | D | Page manager | Yes | Part | No | No | IMPLEMENTING | Frontend page share uses a Documents extras route, not the Governance contract; no sharing summary for Pages; no real e2e. |
| UC-GOV-E01 | Decide a Board or Page action in the fixed order | E | Server | Yes | Part | No | Part | HARDENING | tests and owner check |
| UC-GOV-E02 | Space-derived access | E | Server | Yes | Yes | Yes | No | INTEGRATING | tests and owner check |
| UC-GOV-E03 | A Team is an evaluated subject | E | Server | Yes | No | No | No | IMPLEMENTING | No frontend or real e2e proof. |
| UC-GOV-E04 | Board child resources are authorized through their Board | E | Server | Yes | Part | No | No | HARDENING | tests and owner check |
| UC-GOV-E05 | Collections return what a single read would allow | E | Server | Yes | Yes | No | Part | HARDENING | tests and owner check |
| UC-GOV-E06 | Return the same NotFound to people without access | E | Server | Yes | Part | No | Part | HARDENING | tests and owner check |
| UC-GOV-E07 | Fail closed when facts are missing, stale or unavailable | E | Server | Yes | No | No | No | IMPLEMENTING | No fault-injection integration test of a provider outage. |
| UC-GOV-E08 | Permission-sensitive cache carries scope and version | E | Server | Part | No | No | No | IMPLEMENTING | A projection table exists; invalidation-on-change tests are not found. Treat as unproven. |
| UC-GOV-E09 | Realtime access ends when authority ends | E | Connected user | Part | No | No | No | IMPLEMENTING | Authorization at subscribe time is proven; revocation while connected is not found in source or tests. |
| UC-GOV-E10 | Export, search and analytics keep source authorization | E | Any reader | Part | No | No | No | DEFINED | Export audit not built; owned per context, Governance supplies the rule. |
| UC-GOV-E11 | Entitlement and permission stay separate | E | Server | Yes | No | No | No | IMPLEMENTING | Billing is not built; the port always allows. |
| UC-GOV-E12 | Show permission state in the UI as a projection of the server | E | Any user | Yes | Yes | Part | No | INTEGRATING | tests and owner check |
| UC-GOV-F01 | Declare the resource and action on every request | F | Developer | Yes | No | No | No | HARDENING | tests and owner check |
| UC-GOV-F02 | Authorize background and system actions explicitly | F | System, Automation, Integration principal | Yes | No | No | No | IMPLEMENTING | Automation execution identity decided per context; no cross-context end-to-end proof. |
| UC-GOV-F03 | Record audit evidence for permission grant and revoke | F | System | Yes | No | No | No | INTEGRATING | tests and owner check |
| UC-GOV-F04 | Audit every high-impact governance and administration change | F | System | Part | No | No | No | DEFINED | Missing audit on role change, member lifecycle, API tokens, share links, sharing policy. |
| UC-GOV-F05 | Audit denied attempts on security-sensitive actions | F | System | No | No | No | No | DEFINED | Not built. |
| UC-GOV-G01 | Create a share link for a Board or Page | G | Board or Page sharer | Part | No | Part | No | IMPLEMENTING | Defects: access mode is hard-coded `WorkspaceOnly`; request `Level` is ignored; the DTO returns `TokenHash`; no audit; no policy check; ... |
| UC-GOV-G02 | Revoke (disable) a share link | G | Board or Page sharer, workspace Owner | Part | No | No | No | IMPLEMENTING | Authorization built on the shared resource (decided 2026-10-30); tests and owner check; no audit |
| UC-GOV-G03 | A visitor opens a resource through a share link | G | Anonymous or signed-in visitor | No | No | No | No | BLOCKED | Owner must decide the execution principal of an anonymous visitor, abuse controls and whether Board links are in scope at all (76.4 says ... |
| UC-GOV-G04 | List the share links of a resource | G | Board or Page sharer | No | No | Part | No | DEFINED | No endpoint. Enterprise baseline. |
| UC-GOV-G05 | Link expiry, password and rotation | G | Link sharer | Part | No | No | No | BLOCKED | Depends on UC-GOV-G03 (no consumer). Domain rules exist and are tested. |
| UC-GOV-G06 | Turn share links off for the whole workspace | G | Workspace Owner, Admin | Part | Yes | Yes | No | INTEGRATING | tests and owner check |
| UC-GOV-G07 | Show the sharing policy inside the resource Share dialog | G | Board or Page sharer | Yes | Yes | Yes | No | INTEGRATING | tests and owner check |
| UC-GOV-G08 | Copy the resource address (a locator, not a share link) | G | Any viewer | Yes | Yes | Yes | No | INTEGRATING | tests and owner check |
| UC-GOV-G09 | Sharing is never transitive | G | Server | Part | No | No | No | DEFINED | No test pins non-transitivity. |
| UC-GOV-G10 | Allow or stop guest invitations for the workspace (enterprise baseline) | G | Workspace Owner, Admin | No | Part | Part | No | DEFINED | New workspace setting, new check in UC-GOV-D07. |
| UC-GOV-H01 | View Sharing and security of the workspace | H | Workspace Owner, Admin | Yes | Yes | Yes | No | INTEGRATING | tests and owner check |
| UC-GOV-H02 | View the audit log of a workspace | H | Workspace Owner, Admin | No | Part | No | No | DEFINED | No endpoint, screen or artboard. Frontend service is dead code. |
| UC-GOV-H03 | Export the audit log | H | Workspace Owner | No | No | No | No | DEFINED | Not built. |
| UC-GOV-H04 | Audit reads of the audit log | H | System | No | No | No | No | DEFINED | Depends on UC-GOV-H02. |
| UC-GOV-H05 | Issue an API token for a workspace | H | Workspace Owner | Yes | No | No | No | IMPLEMENTING | No screen and no artboard; no audit row; no token lifetime cap; the token's scope and rights are not narrower than its issuer's by a ... |
| UC-GOV-H06 | List API tokens | H | Workspace Owner | Yes | No | No | No | IMPLEMENTING | No screen; unbounded read. |
| UC-GOV-H07 | Revoke an API token | H | Workspace Owner | Yes | No | No | No | IMPLEMENTING | No screen; no audit. |
| UC-GOV-H08 | Cap API token lifetime by workspace policy (enterprise baseline) | H | Workspace Owner | No | No | No | No | DEFINED | Not built. |
| UC-GOV-H09 | Manage roles on a governance screen | H | Workspace Owner | No | Part | No | No | DEFINED | Depends on C01-C06 and on a design. |
| UC-GOV-H10 | Read outbox statistics (platform operator) | H | Platform operator (`SystemAdmin` role) | Yes | No | No | No | IMPLEMENTING | Operator surface only; how the `SystemAdmin` role is granted and audited is not documented. |
| UC-GOV-H11 | List pending and failed outbox messages | H | Platform operator | Yes | No | No | No | IMPLEMENTING | Cap on the failed path not verified here; cross-tenant read by design; no audit of operator reads. |
| UC-GOV-H12 | Read one outbox message | H | Platform operator | Yes | No | No | No | IMPLEMENTING | Detail fields not reviewed for payload or secret exposure; no test of the 404. |
| UC-GOV-H13 | Workspace administration console | H | Workspace Owner, Admin | Part | Part | Part | No | BLOCKED | Owner decisions 1-9 of the admin plane (roadmap AD-1) are pending. Needed: what the console contains, who may open each area, whether ... |
| UC-GOV-H14 | Account administration console | H | Account Owner, Admin | Part | Part | Part | No | BLOCKED | Same pending owner decisions as UC-GOV-H13; Accounts owns the content, Governance only the access rule. |
| UC-GOV-I01 | Every endpoint declares its authorization | I | Developer, CI | Yes | No | No | No | HARDENING | tests and owner check |
| UC-GOV-I02 | Tenant isolation holds for governance data | I | Developer, CI | Yes | No | No | Part | HARDENING | tests and owner check |
| UC-GOV-I03 | Migrate the action and resource vocabulary safely | I | Developer | Part | No | No | No | DEFINED | No rehearsal test of a vocabulary migration. |

# 3. Use cases

## Capability A: WG-GOV resource/action model

### UC-GOV-A01 Admit a protected resource kind with its action vocabulary
- Capability: A WG-GOV resource/action model (team spec, Governance list)
- Actors and permission: Developer of an owning context; none (design-time gate)
- Preconditions: The owning context has decided its resource, owner, scope, default policy and guest behaviour.
- Input: Resource kind (`context.resource`), read/list, mutation, share and administration actions, default policy, guest behaviour, audit-sensitive actions.
- Output: A canonical `ResourceKind` and `PermissionAction` entries exist, each classified in the role baseline; no check is written before this.
- Main flow: 1. Name the kind in `context.resource` form. 2. Add the actions with business names. 3. Classify each action in the baseline. 4. Declare the requirement on each request. 5. Add the guest and audit rules.
- No-op / rejection / concurrency / retry: An unclassified action fails the inventory test. A kind that is not canonical is refused by the command validators. Two kinds for one resource are refused in review.
- UI states: none; server-only use case
- Rules: [Governance §87-§88](../../product/governance.md) GOV-022, GOV-004, GOV-005; execution pack WGREQ044-WGREQ053, WGREQ118-WGREQ122.
- Acceptance: Given a new resource, When its kind has no classified action, Then the architecture test fails. Given a command with a non-canonical kind, When validated, Then it is refused.
- Evidence: BE `ResourceKind.TryCreate` and the `CanonicalKindValidatorsTests` · Tests `backend/tests/Notrelix.Architecture.Tests/Authorization/PermissionActionInventoryTests.cs`, `backend/tests/Notrelix.Application.Tests/Features/Governance/CanonicalKindValidatorsTests.cs` · Real e2e none · Design none
- State: IMPLEMENTING - Kinds exist for Board, Page and workspace; Items, Automation and Integration rely on the workspace kind or the Board. A written admission checklist test is missing.

### UC-GOV-A02 Resolve a resource's kind, workspace and owner from its id
- Capability: A WG-GOV resource/action model (team spec, Governance list)
- Actors and permission: Application pipeline (system); none (internal)
- Preconditions: A request carries a resource kind and id.
- Input: Resource kind and id. The id is opaque to Governance.
- Output: Workspace, account and lifecycle facts of the resource, or NotFound.
- Main flow: 1. The pipeline asks the locator for the resource. 2. The locator reads the owning context. 3. A resource of another workspace, missing, archived or deleted gives NotFound.
- No-op / rejection / concurrency / retry: Cross-workspace id: the same NotFound as a missing id (no existence leak). Locator failure fails closed.
- UI states: none; server-only use case
- Rules: [Governance §13](../../product/governance.md) GOV-004, GOV-025 step 1; WGREQ046, WGREQ047, WGREQ086, WGREQ096.
- Acceptance: Given a Board id of workspace W2, When a W1 member requests it, Then the answer equals the answer for a random id.
- Evidence: BE `Notrelix.Infrastructure/Services/ResourceLocator.cs` · Tests `backend/tests/Notrelix.Integration.Tests/Governance/GovernanceResourcePermissionFlowTests.cs` · Real e2e `frontend/e2e/real/workspace-isolation.real.e2e.spec.ts` (workspace switch only) · Design none
- State: HARDENING. Nothing left to build: `ResourceLocator` covers every canonical kind (Board and its children, Page, Block, Comment, Attachment, permission, share link, automation, calendar integration), runs in the actor's user scope so a row the actor holds no grant on is not found, and answers null for an unknown kind. Missing: tests and owner check (a cross-tenant negative test per kind, Page and share link first).

### UC-GOV-A03 Classify every permission action in the role baseline
- Capability: A WG-GOV resource/action model (team spec, Governance list)
- Actors and permission: Developer; none (design-time gate)
- Preconditions: A `PermissionAction` is added or renamed.
- Input: Action name and its Owner/Admin/Member/Guest cells.
- Output: The typed baseline table classifies the action; an unclassified action fails the build.
- Main flow: 1. Add the action. 2. Add its row to the baseline. 3. Run the inventory and baseline tests.
- No-op / rejection / concurrency / retry: An unclassified action fails `WorkspaceRoleBaselineTests` and the inventory test. Renaming an action is a data migration (stored rules, templates, audit).
- UI states: none; server-only use case
- Rules: [Governance §14-§15, §44.1, §85](../../product/governance.md) GOV-005; WGREQ050-WGREQ053, WGREQ172.
- Acceptance: Given a new action with no baseline row, When tests run, Then they fail naming the action.
- Evidence: BE `WorkspaceRoleBaseline` · Tests `backend/tests/Notrelix.Application.Tests/Features/Governance/WorkspaceRoleBaselineTests.cs`, `backend/tests/Notrelix.Architecture.Tests/Authorization/PermissionActionInventoryTests.cs` · Real e2e none · Design none
- State: IMPLEMENTING - Stored-rule migration on rename is specified (§85) but has no test.

### UC-GOV-A04 Field-level permission (deferred)
- Capability: A WG-GOV resource/action model (team spec, Governance list)
- Actors and permission: Workspace Owner, Admin; future `ManageBoard` on the field
- Preconditions: A Board field is restricted from a group of people.
- Input: Field, subject, read or edit.
- Output: Not delivered. Backend read, mutation, export and realtime paths would enforce it; the UI hiding a field is never the boundary.
- Main flow: Deferred. Until built, no field is secured; the product must not present a field as restricted.
- No-op / rejection / concurrency / retry: A request to restrict a field is answered with a clear not-available state, not a silent no-op.
- UI states: unavailable
- Rules: [Governance §24-§25](../../product/governance.md) GOV-008; WGREQ054.
- Acceptance: Given the feature is off, When a manager looks for field security, Then no control implies protection.
- Evidence: BE `Notrelix.Domain/Governance/Permissions/FieldPermission.cs` (Domain only, no handler, no endpoint) · Tests none · Real e2e none · Design none
- State: DEFINED - Lead decision L11: deferred behind Work Management field work. Domain type exists and is unused.

## Capability B: Built-in roles

### UC-GOV-B01 Decide a workspace action from the built-in role baseline
- Capability: B Built-in roles (team spec, Governance list)
- Actors and permission: Server (every protected request); the action's baseline cell
- Preconditions: An active workspace member with a role of Owner, Admin, Member or Guest.
- Input: Workspace id, actor, action.
- Output: Allow or deny per the final matrix; resource-level cells (L) defer to the resource decision.
- Main flow: 1. Resolve the actor's active role. 2. Look up the action cell. 3. A suspended or removed member is denied. 4. Compose with explicit rules and Space access (E01).
- No-op / rejection / concurrency / retry: Unknown role or missing membership: deny (fail closed). Denial for a non-member of the workspace is NotFound.
- UI states: permission denied; read-only; loading while the role is unresolved
- Rules: [Governance §44, §44.1](../../product/governance.md) GOV-013, GOV-015; WGREQ063-WGREQ065, WGREQ081-WGREQ085, WGREQ181.
- Acceptance: Given a Member, When they call `ManageWorkspaceSettings`, Then 403. Given an Admin, Then 200. Given a removed member, Then NotFound.
- Evidence: BE `WorkspaceRoleBaseline`, `AccessPolicyEngine` · FE `frontend/apps/web/src/router/guards/require-permission.ts` · Tests `WorkspaceRoleBaselineTests`, `GovernanceResourcePermissionFlowTests` · Real e2e `frontend/e2e/real/guest-permission.real.e2e.spec.ts` (Guest only) · Design `Final-Members`
- State: HARDENING. Built end to end: the baseline cells in `WorkspaceRoleBaseline`, the route guard `require-permission.ts`; nothing left to build. Missing: tests and owner check (real e2e of an Admin and a Member denial; the cell-by-cell proof is unit-level).

### UC-GOV-B02 Decide an account-scoped action from the account role
- Capability: B Built-in roles (team spec, Governance list)
- Actors and permission: Server; `CreateWorkspace`, `ManageAccount`
- Preconditions: An Account member.
- Input: Account id, actor, action.
- Output: Owner and Admin allowed, Member denied; `ViewWorkspace` open to every Account member.
- Main flow: 1. Resolve the Account role (Accounts owns it). 2. Look up `AccountRoleBaseline`. 3. Account membership never implies workspace access.
- No-op / rejection / concurrency / retry: No Account membership: NotFound. Account disabled: deny.
- UI states: permission denied
- Rules: [Governance §44.1, §74](../../product/governance.md) GOV-007, GOV-013; WGREQ115-WGREQ116. Account facts: Accounts owns them.
- Acceptance: Given an Account Member, When they create a workspace, Then 403. Given an Account Admin, Then 201.
- Evidence: BE `AccountRoleBaseline` (Application Governance) · Tests `WorkspaceRoleBaselineTests` · Real e2e none · Design none
- State: IMPLEMENTING - No frontend check of denial; no API-level test of the Account cell.

### UC-GOV-B03 Keep Owner-only actions Owner-only (Admin never silently becomes Owner)
- Capability: B Built-in roles (team spec, Governance list)
- Actors and permission: Workspace Owner; Admin is denied; `ChangeMemberRole`, `ManageWorkspace`, `ArchiveWorkspace`, `RestoreWorkspace`, `DeleteWorkspace`, `ManageApiTokens`
- Preconditions: An Admin attempts a lifecycle, role-minting or credential action.
- Input: Action and target.
- Output: Denied for Admin. Admin may invite and remove only Members and Guests (role cap and actor-versus-target guards live in Workspaces).
- Main flow: 1. Baseline denies the Admin cell. 2. Domain guards refuse role minting and acting on an Owner or another Admin.
- No-op / rejection / concurrency / retry: 403 with a stable rule code. The last Owner cannot be removed.
- UI states: permission denied (control disabled with the reason)
- Rules: [Governance §45, §44.2](../../product/governance.md) GOV-013; WGREQ154. Role caps and guards: see [Workspaces use cases](workspaces.md).
- Acceptance: Given an Admin, When they delete the workspace or change a role, Then denied and nothing changes. Given an Admin, When they remove an Owner, Then refused.
- Evidence: BE baseline; Workspaces member commands · Tests `WorkspaceRoleBaselineTests`, `backend/tests/Notrelix.Integration.Tests/Governance/GovernanceResourcePermissionFlowTests.cs` · Real e2e none · Design `Final-Members`
- State: INTEGRATING. Built: the baseline denies the Admin cell and Workspaces guards refuse role minting and acting on an Owner or Admin; the Members surface receives the viewer's role (the member guards and surface are Workspaces'). Nothing left to build here. Missing: tests and owner check (real e2e of the denied path).

### UC-GOV-B04 A Guest does not enumerate the workspace
- Capability: B Built-in roles (team spec, Governance list)
- Actors and permission: Guest; `ViewWorkspace` (own row only); `ViewMembers` denied
- Preconditions: A Guest of the workspace with explicit access to one resource.
- Input: Workspace id.
- Output: `GET /workspaces/{id}/members/me` returns the Guest's own row; the member list answers 403; names on a shared resource come from that resource's reads. Boards the Guest cannot open are absent from lists.
- Main flow: 1. Shell asks for `members/me`. 2. A Guest role never requests the list. 3. Board and Page lists apply the single-read decision in the query.
- No-op / rejection / concurrency / retry: Member list 403. Board the Guest was not given: NotFound (or 403 on the workspace listing).
- UI states: read-only own row; permission denied; empty
- Rules: [Governance §29-§30, §44.3, §95](../../product/governance.md) GOV-009, GOV-029; WGREQ155.
- Acceptance: Given a Guest, When they list boards, Then only shared boards appear. When they read an unshared board, Then 403 or 404. When they list members, Then 403.
- Evidence: BE `GET /api/v1/workspaces/{workspaceId}/members/me` (Workspaces.Members.GetMyMembership) · FE `frontend/apps/web/src/routes/workspaces/$workspaceId/members.tsx`, `use-workspace-viewer.ts` · Tests `backend/tests/Notrelix.Integration.Tests/Governance/GovernanceResourcePermissionFlowTests.cs` · Real e2e `frontend/e2e/real/guest-permission.real.e2e.spec.ts` · Design `Final-Members`, `Final-M-Members`
- State: HARDENING. Built: `members/me`, a 403 on the member list for a Guest, and the Members page reads only the Guest's own row (`members.tsx` uses `viewer.mine` for a Guest). Nothing left to build here. Missing: tests and owner check (real e2e for the member-list 403 and the read-only Members page; a 1 px check of the Guest view).

### UC-GOV-B05 A Guest's resource level is capped at Viewer
- Capability: B Built-in roles (team spec, Governance list)
- Actors and permission: Guest; Board or Page manager who shares; `ManageBoardPermission`, `ManagePagePermission`
- Preconditions: A Guest holds a Manager-level entry, Team grant, board role or Space role.
- Input: Level (Viewer, Commenter, Editor, Manager).
- Output: Effective level is the lower of the computed level and Viewer (owner decision 2026-10-31: outside people are view-only). A higher entry for a Guest is stored but never effective; a resource invitation is accepted at Viewer only.
- Main flow: 1. Engine computes the level from every source. 2. Applies the Guest cap. 3. The sharing read marks the entry `levelCapped`. 4. The UI shows Manager disabled for a Guest with the reason.
- No-op / rejection / concurrency / retry: A Guest never manages settings, visibility, move, archive, delete, board members or permissions. No 422 on granting Manager (stored and capped, by decision).
- UI states: disabled option with reason; read-only
- Rules: [Governance §30.1](../../product/governance.md) GOV-030; WGREQ154.
- Acceptance: Given a Guest with a Manager entry, When they change board settings, Then denied. When the manager opens sharing, Then the entry shows Viewer with `levelCapped`.
- Evidence: BE `AccessPolicyEngine`, `ResourceAccessSummaryService.Sharing.cs` · FE `frontend/apps/web/src/shell/workspace/board-share-dialog.tsx` · Tests `EffectiveBoardAccessTests`, `ResourceAccessSummaryServiceTests` · Real e2e none · Design `Final-Share`
- State: INTEGRATING. Built end to end (the engine caps a Guest at Editor, the sharing read marks the entry `levelCapped`, the Share dialog shows Manager disabled with the reason); nothing left to build. Missing: tests and owner check (real e2e). Accepted by GOV-030 and left as is: a Manager entry for a Guest is stored at grant time and never effective.

## Capability C: Custom roles

### UC-GOV-C01 Create a custom role (enterprise baseline)
- Capability: C Custom roles (team spec, Governance list)
- Actors and permission: Workspace Owner; `ManageWorkspace` (Owner)
- Preconditions: Workspace is active.
- Input: Name (1-60 characters, unique per workspace after trim), description, a set of permission actions.
- Output: A custom role in Active state; `CustomRoleCreated` fact after commit; audit entry.
- Main flow: 1. Owner opens Roles. 2. Names the role and ticks actions. 3. Save. 4. The role appears with 0 members.
- No-op / rejection / concurrency / retry: Duplicate name: 409. An Owner-only action in the set (GOV-013 list): 422. Empty set: 422. Retry with the same idempotency key: same role, no second row. No expected version on create.
- UI states: loading, empty, validation error, permission denied, pending, success
- Rules: [Governance §41-§47](../../product/governance.md) GOV-012, GOV-013; WGREQ063-WGREQ070, WGREQ147, WGREQ173; Lead decision L1, L2.
- Acceptance: Given an Owner, When they create role 'Reviewer' with ViewBoard, Then the role is listed. Given an Admin, Then 403. Given a duplicate name, Then 409.
- Evidence: BE `Notrelix.Domain/Governance/Roles/CustomRole.cs`, `CustomRolePermission.cs`, `MemberRoleAssignment.cs`, `IGovernanceDbContext.CustomRoles` (Domain and persistence only; no handler, no endpoint) · FE `frontend/packages/features/governance/src/core/api/governance.service.ts` calls role routes the backend does not have · Tests `backend/tests/Notrelix.Domain.Tests/Governance/Roles/CustomRoleTests.cs` · Real e2e none · Design none
- State: DEFINED - No Application use case, no endpoint, no design. The frontend package calls non-existent routes (dead code).

### UC-GOV-C02 Change a custom role's name or actions
- Capability: C Custom roles (team spec, Governance list)
- Actors and permission: Workspace Owner; `ManageWorkspace` (Owner)
- Preconditions: The role is Active.
- Input: Role id, expected version, new name, new action set.
- Output: New version; members of the role get the new set on the next evaluation; permission cache for the workspace is invalidated; audit entry.
- Main flow: 1. Open the role. 2. Edit. 3. Save with the version. 4. Confirm dialog shows how many members change.
- No-op / rejection / concurrency / retry: Stale version: 412 and nothing changes. No change: no-op, no version bump, no event. Removing an action is applied at once to all holders.
- UI states: conflict, pending, destructive confirmation (action removed), validation error
- Rules: [Governance §41-§47](../../product/governance.md) GOV-012, GOV-013; WGREQ063-WGREQ070, WGREQ147, WGREQ173; Lead decision L1, L2.
- Acceptance: Given a stale version, When saved, Then 412 and the role is unchanged. Given the same set, Then the version is unchanged.
- Evidence: BE `Notrelix.Domain/Governance/Roles/CustomRole.cs`, `CustomRolePermission.cs`, `MemberRoleAssignment.cs`, `IGovernanceDbContext.CustomRoles` (Domain and persistence only; no handler, no endpoint) · FE `frontend/packages/features/governance/src/core/api/governance.service.ts` calls role routes the backend does not have · Tests `backend/tests/Notrelix.Domain.Tests/Governance/Roles/CustomRoleTests.cs` · Real e2e none · Design none
- State: DEFINED - No Application use case, no endpoint, no design. The frontend package calls non-existent routes (dead code).

### UC-GOV-C03 Archive a custom role that has assignments
- Capability: C Custom roles (team spec, Governance list)
- Actors and permission: Workspace Owner; `ManageWorkspace` (Owner)
- Preconditions: The role exists.
- Input: Role id, expected version.
- Output: Role Archived; its assignments stay visible but grant nothing from that instant; no hard delete is offered; Activate restores it.
- Main flow: 1. Choose Archive. 2. Dialog states how many members lose access. 3. Confirm.
- No-op / rejection / concurrency / retry: Stale version: 412. Archive twice: no-op. Archived role cannot be assigned.
- UI states: destructive confirmation, conflict, empty (no roles)
- Rules: [Governance §47](../../product/governance.md); Lead decision L2.
- Acceptance: Given a role with 3 holders, When archived, Then the 3 lose the role's actions on the next request and the list shows 'archived'.
- Evidence: BE `Notrelix.Domain/Governance/Roles/CustomRole.cs`, `CustomRolePermission.cs`, `MemberRoleAssignment.cs`, `IGovernanceDbContext.CustomRoles` (Domain and persistence only; no handler, no endpoint) · FE `frontend/packages/features/governance/src/core/api/governance.service.ts` calls role routes the backend does not have · Tests `backend/tests/Notrelix.Domain.Tests/Governance/Roles/CustomRoleTests.cs` · Real e2e none · Design none
- State: DEFINED - No Application use case, no endpoint, no design. The frontend package calls non-existent routes (dead code).

### UC-GOV-C04 Assign a custom role to a workspace member
- Capability: C Custom roles (team spec, Governance list)
- Actors and permission: Workspace Owner; `ChangeMemberRole` (Owner)
- Preconditions: Active member of the same workspace; Active role of the same workspace.
- Input: Member id, role id.
- Output: Assignment row; effective permissions change on the next evaluation; audit entry.
- Main flow: 1. Open the member. 2. Pick roles. 3. Save.
- No-op / rejection / concurrency / retry: Cross-workspace member or role id: NotFound. Already assigned: no-op. BLOCKED on where the role enters the decision order (see state).
- UI states: pending, permission denied, validation error
- Rules: [Governance §46](../../product/governance.md) GOV-007, GOV-012, GOV-024; WGREQ066-WGREQ067.
- Acceptance: Given a member and a role, When assigned, Then the member can do the role's actions on the next request. Given an id of another workspace, Then NotFound.
- Evidence: BE `Notrelix.Domain/Governance/Roles/CustomRole.cs`, `CustomRolePermission.cs`, `MemberRoleAssignment.cs`, `IGovernanceDbContext.CustomRoles` (Domain and persistence only; no handler, no endpoint) · FE `frontend/packages/features/governance/src/core/api/governance.service.ts` calls role routes the backend does not have · Tests `backend/tests/Notrelix.Domain.Tests/Governance/Roles/CustomRoleTests.cs` · Real e2e none · Design none
- State: BLOCKED - Decision needed from the owner: at which step of the GOV-025 order a custom role's actions apply (as part of the workspace baseline only, or also able to reach Private Boards and Pages), and whether Deny rules outrank it. Lead proposal in Part 4 L1.

### UC-GOV-C05 Remove a custom role from a member
- Capability: C Custom roles (team spec, Governance list)
- Actors and permission: Workspace Owner; `ChangeMemberRole` (Owner)
- Preconditions: The assignment exists.
- Input: Member id, role id.
- Output: Assignment ended; access derived from it ends on the next evaluation; audit entry.
- Main flow: 1. Open the member. 2. Untick the role. 3. Save.
- No-op / rejection / concurrency / retry: Not assigned: no-op. Removing a member from the workspace ends every assignment (Workspaces).
- UI states: pending, permission denied
- Rules: [Governance §46-§47](../../product/governance.md); WGREQ147.
- Acceptance: Given an assigned role, When removed, Then the member loses the role's actions on the next request.
- Evidence: BE `Notrelix.Domain/Governance/Roles/CustomRole.cs`, `CustomRolePermission.cs`, `MemberRoleAssignment.cs`, `IGovernanceDbContext.CustomRoles` (Domain and persistence only; no handler, no endpoint) · FE `frontend/packages/features/governance/src/core/api/governance.service.ts` calls role routes the backend does not have · Tests `backend/tests/Notrelix.Domain.Tests/Governance/Roles/CustomRoleTests.cs` · Real e2e none · Design none
- State: BLOCKED - Same decision as UC-GOV-C04.

### UC-GOV-C06 List roles and the permission catalog
- Capability: C Custom roles (team spec, Governance list)
- Actors and permission: Workspace Owner, Admin; `ViewMembers` plus role read (Lead decision L1)
- Preconditions: Workspace is active.
- Input: Workspace id; paging.
- Output: Built-in roles with their baseline, custom roles with member counts, and the action catalog with business descriptions.
- Main flow: 1. Open Roles. 2. The page shows built-in rows read-only and custom rows editable.
- No-op / rejection / concurrency / retry: Guest: 403. Unbounded lists are paged.
- UI states: loading, empty, permission denied, error
- Rules: [Governance §43-§44, §100](../../product/governance.md); WGREQ064-WGREQ065.
- Acceptance: Given an Admin, When they open Roles, Then they see built-in and custom roles; Given a Guest, Then 403.
- Evidence: BE none · FE `frontend/packages/features/governance/src/core/types/governance.ts` (`GovernanceRole`, `GovernancePermission` types and service for routes that do not exist) · Tests `frontend/packages/features/governance/src/__tests__/governance.unit.test.ts` (service shape only) · Real e2e none · Design none
- State: DEFINED - No endpoint, no screen, no artboard. Package `features/governance` is not imported by any app.

## Capability D: Permission assignment

### UC-GOV-D01 Give a workspace member access to a Board or Page
- Capability: D Permission assignment (team spec, Governance list)
- Actors and permission: Board or Page manager; `ManageBoardPermission` or `ManagePagePermission` (Manager level; Owner level needs Owner)
- Preconditions: Resource visible to the actor; subject is an active member of the same workspace.
- Input: Resource kind and id; subject type `User`; subject id; level Viewer, Commenter, Editor, Manager or Owner. No expiry, no deny, no condition.
- Output: A `ResourcePermission` (Allow, priority 100); access-change record for Boards; audit `GrantResourcePermission`; `ResourcePermissionGranted` fact after commit.
- Main flow: 1. Manager opens Share. 2. Picks a person and a level. 3. Add. 4. The person appears; they gain access on their next request.
- No-op / rejection / concurrency / retry: Subject outside the workspace: 422 `PermissionSubjectMustBelongToWorkspace`. Level above the actor's own: refused by the grant ceiling. Same level again: no-op (no version bump, no record). Different level: changes the level (UC-GOV-D03). Retry: the unique subject-per-resource rule makes a second call a no-op. A level change of an existing grant must name its version (UC-GOV-D03).
- UI states: loading, empty, validation error, permission denied, pending, conflict, success
- Rules: [Governance §16, §19, §23.3](../../product/governance.md) GOV-006, GOV-027, GOV-021; WGREQ076-WGREQ079, WGREQ180; certification P2B-CERT-010.
- Acceptance: Given a Manager, When they add member M as Editor, Then 201 and M can edit. Given M outside the workspace, Then 422. Given a Member, Then 403/404.
- Evidence: BE `POST /api/v1/resources/{resourceType}/{resourceId}/permissions` (Governance.ResourcePermissions.Grant, `GrantResourcePermissionCommand`) · FE `frontend/apps/web/src/shell/workspace/board-share-dialog.tsx`, `use-board-sharing.ts`, `frontend/packages/foundation/contracts/src/endpoints/endpoints.ts` (`resourcePermissions`) · Design `Final-Share`, `Final-M-Share` · Tests `backend/tests/Notrelix.Integration.Tests/Governance/GovernanceResourcePermissionFlowTests.cs`, `backend/tests/Notrelix.Application.Tests/Features/Governance/GrantResourcePermissionWorkspaceMembershipTests.cs`, `GrantResourcePermissionCommandValidatorTests.cs`, `backend/tests/Notrelix.Domain.Tests/Governance/Permissions/ResourcePermissionTests.cs` · Real e2e `frontend/e2e/real/board-sharing.real.e2e.spec.ts`
- State: HARDENING. Built end to end for a Board; a grant at the level the subject already holds is a no-op and records nothing; a grant for an existing subject at another level must name the grant's version (UC-GOV-D03). Missing: tests and owner check (real e2e for a Page share and an Application test of the cross-tenant grant; a 1 px check).

### UC-GOV-D02 Give a Team access to a Board or Page
- Capability: D Permission assignment (team spec, Governance list)
- Actors and permission: Board or Page manager; `ManageBoardPermission` or `ManagePagePermission`
- Preconditions: Active Team of the same workspace.
- Input: As D01 with subject type `Team` and the Team id.
- Output: A Team entry; every active member of the Team gets the level while they are a member and the Team is active (no per-user copy).
- Main flow: 1. Pick a Team in Share. 2. Choose a level. 3. Add.
- No-op / rejection / concurrency / retry: Archived or deleted Team: grants nothing from the next evaluation. Team of another workspace: 422. Subject types other than User and Team: refused (`WorkspaceRole`, `PublicLink`, `ExternalEmail` evaluate to nothing).
- UI states: empty, validation error, permission denied, pending
- Rules: [Governance §16, §23.2](../../product/governance.md) GOV-026; WGREQ037.
- Acceptance: Given a Team grant, When a member leaves the Team, Then their access ends on the next request.
- Evidence: BE same route as D01 · FE share dialog lists people; Team rows are read from the sharing read, no Team picker is shown · Tests `ResourceAccessSummaryServiceTests`, `PostgresAccessFactsProviderTests` · Real e2e none · Design `Final-Teams` (Team access shown in `chia-se`)
- State: INTEGRATING. Built: the grant route accepts a Team subject and the sharing read shows Team rows. Not built: a Team picker in the Share dialog (the dialog lists people only); it needs the Teams list inside the dialog and an artboard of the picker. Missing: tests and owner check (real e2e, an Application test of the Team-in-workspace check on grant).

### UC-GOV-D03 Change the level of an existing grant
- Capability: D Permission assignment (team spec, Governance list)
- Actors and permission: Board or Page manager; `ManageBoardPermission` or `ManagePagePermission`
- Preconditions: A grant exists for the subject.
- Input: Same subject, new level, and the version of the grant being changed.
- Output: Level changed; access-change record `Changed` with previous and new level; audit entry; `ResourcePermissionLevelChanged` fact.
- Main flow: 1. Open the level menu of a row. 2. Choose a level. 3. The row updates.
- No-op / rejection / concurrency / retry: Same level: no-op. Level above actor's: refused. Demoting the last Manager of a Board is refused (WM rule). A Guest row shows Manager disabled. Two managers changing at once: the second names a version that is gone and gets 412.
- UI states: pending, conflict, optimistic update with rollback, permission denied
- Rules: [Governance §21, §23.3, §30.1](../../product/governance.md) GOV-027, GOV-030; WGREQ079.
- Acceptance: Given a grant at Viewer, When changed to Editor, Then the subject can edit. When the call fails, Then the row returns to Viewer.
- Evidence: BE `POST /api/v1/resources/{resourceType}/{resourceId}/permissions` (Governance.ResourcePermissions.Grant, `GrantResourcePermissionCommand`) · FE `frontend/apps/web/src/shell/workspace/board-share-dialog.tsx`, `use-board-sharing.ts`, `frontend/packages/foundation/contracts/src/endpoints/endpoints.ts` (`resourcePermissions`) · Design `Final-Share`, `Final-M-Share` · Tests `backend/tests/Notrelix.Integration.Tests/Governance/GovernanceResourcePermissionFlowTests.cs`, `backend/tests/Notrelix.Application.Tests/Features/Governance/GrantResourcePermissionWorkspaceMembershipTests.cs`, `GrantResourcePermissionCommandValidatorTests.cs`, `backend/tests/Notrelix.Domain.Tests/Governance/Permissions/ResourcePermissionTests.cs` · Real e2e `frontend/e2e/real/board-sharing.real.e2e.spec.ts` · BE (version) `GrantPermissionRequest.ExpectedVersion`, `ResourceGrantEntry.PermissionVersion`, `BoardSharingEntry.PermissionVersion`; FE `board-share-models.ts` (`planLevelChange`), `use-board-sharing.ts`
- State: HARDENING. Built: changing the level of an existing grant names the grant's version (`expectedVersion` in the grant body, read from `permissionVersion` of a sharing entry or `version` of a permission row); a stale or missing version answers 412 `governance.permission.version-conflict`, and the row's own version guard stops two writes that read the same version; a grant at the level already held is a no-op whatever the version. The dialog shows "Someone else changed this access first" and reads the list again. The mock backend answers the same 412. Missing: tests and owner check (an Application test of the conflict, a mock e2e of the conflict toast).

### UC-GOV-D04 Remove someone's access to a Board or Page
- Capability: D Permission assignment (team spec, Governance list)
- Actors and permission: Board or Page manager; `ManageBoardPermission` or `ManagePagePermission`
- Preconditions: The grant exists and belongs to the resource in the workspace.
- Input: Resource kind, resource id, permission id.
- Output: Grant revoked; access-change record `Removed`; audit `RevokeResourcePermission`; `ResourcePermissionRevoked` fact. Other sources (Space role, Team, Board member) still apply.
- Main flow: 1. Open the row menu. 2. Remove access. 3. Confirm. 4. The row disappears.
- No-op / rejection / concurrency / retry: Unknown id or id of another resource or workspace: NotFound. Already revoked: success, nothing changes. Revoking above the actor's level is refused. Removing yourself is allowed unless you are the last Manager.
- UI states: destructive confirmation, pending, permission denied, optimistic update with rollback
- Rules: [Governance §73](../../product/governance.md) GOV-021; WGREQ079, WGREQ149.
- Acceptance: Given a grant, When revoked, Then DELETE returns 204 and the subject gets NotFound on the resource unless another source applies.
- Evidence: BE `DELETE /api/v1/resources/{resourceType}/{resourceId}/permissions/{permissionId}` (Governance.ResourcePermissions.Revoke, `RevokeResourcePermissionCommand`) · FE `board-share-dialog.tsx` · Tests as D01, `BoardMemberRemovalCeilingTests` · Real e2e `frontend/e2e/real/board-sharing.real.e2e.spec.ts` · Design `Final-Share`
- State: HARDENING. Built: revoking an entry that is already revoked answers success and changes nothing (a retry, or two managers removing one entry); an id that never belonged to this resource in this workspace is still NotFound. Missing: tests and owner check (realtime and cache convergence after revoke, an Application test of the repeat).

### UC-GOV-D05 List who holds explicit access on a resource
- Capability: D Permission assignment (team spec, Governance list)
- Actors and permission: Board or Page manager; `ManageBoardPermission` or `ManagePagePermission`
- Preconditions: Resource visible to the actor.
- Input: Resource kind and id.
- Output: The resource's grants (id, subject, level, creator, deleted flag).
- Main flow: 1. The dialog opens. 2. The list loads.
- No-op / rejection / concurrency / retry: A non-manager gets the counts through the sharing read (UC-GOV-D10), not this list. Unknown resource: NotFound. The list leaves out revoked rows and holds at most 500 entries.
- UI states: loading, empty, permission denied, error
- Rules: [Governance §6, §62](../../product/governance.md) GOV-001, GOV-019; WGREQ168.
- Acceptance: Given a manager, When they GET the list, Then they see every active grant. Given a Member without management, Then denied.
- Evidence: BE `GET /api/v1/resources/{resourceType}/{resourceId}/permissions` (Governance.ResourcePermissions.Get, `GetResourcePermissionsQuery`) · FE `endpoints.ts` `resourcePermissions.list` (dialog reads the sharing summary instead) · Tests `GovernanceResourcePermissionFlowTests` · Real e2e none · Design `Final-Share`
- State: INTEGRATING. Built: the list leaves out revoked rows, is ordered and capped at 500 entries (a guard: a resource's explicit entries are people and Teams), and each row carries its `version`. Expiry is out of scope by the owner decision. Missing: tests and owner check. The dialog keeps reading the sharing summary, which carries the same facts, so no screen calls this list except the Page share.

### UC-GOV-D06 Refuse an unsafe grant
- Capability: D Permission assignment (team spec, Governance list)
- Actors and permission: Board or Page manager; as D01
- Preconditions: A grant request.
- Input: Any request input.
- Output: A refusal with a stable rule code and no state change.
- Main flow: 1. The pipeline checks the actor's level against the requested level and subject. 2. The validator checks kind, subject type, level and expiry. 3. The handler checks workspace membership.
- No-op / rejection / concurrency / retry: Owner level by non-Owner: refused. Level `None`: 422. Subject type other than User/Team: 422. `ExpiresAt` present: 422 (not supported). Unknown kind: 422. Subject not in workspace: 422.
- UI states: validation error, permission denied
- Rules: [Governance §16, §23.3](../../product/governance.md) GOV-006, GOV-027, GOV-031 (expiry not built, see D12); WGREQ154.
- Acceptance: Given `ExpiresAt` set, When granted, Then 422 and no row. Given Level Owner by a Manager, Then refused.
- Evidence: BE `GrantResourcePermissionCommandValidator`, `IRequireGrantPermission` seam · Tests `GrantResourcePermissionCommandValidatorTests`, `BoardRoleAssignmentCeilingTests` · Real e2e none · Design none
- State: IMPLEMENTING - No frontend mapping for each refusal; no real e2e of a refusal.

### UC-GOV-D07 Invite a Guest to one resource
- Capability: D Permission assignment (team spec, Governance list)
- Actors and permission: Board or Page manager; Workspace Owner or Admin may invite; `InviteMember` (workspace) and the resource's permission-management action
- Preconditions: Email of an outside person; a Board or Page the actor manages.
- Input: Email, resource kind and id, level (Viewer, Commenter, Editor).
- Output: A resource invitation; on accept the person becomes a Guest of the workspace with an explicit grant on that resource only; audit entries for invite, resend, revoke and accept.
- Main flow: 1. Manager opens Share. 2. Enters an email. 3. The invite is sent. 4. The Guest accepts and sees only that resource.
- No-op / rejection / concurrency / retry: Expired or replayed token: rejected. Revoke before accept: the token stops working. Level above Editor: refused. Guest invitation off by policy (UC-GOV-G10): refused.
- UI states: validation error, pending, permission denied, empty
- Rules: [Governance §29-§30](../../product/governance.md) GOV-009, GOV-030; invitation lifecycle owned by [Workspaces](workspaces.md); WGREQ027-WGREQ033.
- Acceptance: Given a manager, When they invite a new email to Board B, Then after accept the Guest reads B and no other board.
- Evidence: BE `IGuestResourceInvitationFacts`, `IGuestResourceAccessActions` (Governance Public); Workspaces commands `InviteGuestToResource`, `AcceptInvitation` · FE share dialog invite control · Tests `backend/tests/Notrelix.Application.Tests/Features/Governance/GrantResourcePermissionWorkspaceMembershipTests.cs` · Real e2e none · Design `Final-Share`
- State: INTEGRATING. Built end to end (the invitation commands in Workspaces, the Governance facts, the invite control in the Share dialog); nothing left to build here. Missing: tests and owner check (real e2e from invite to accept; a named test of the Governance side of the handshake).

### UC-GOV-D08 A Guest reaches only what was shared with them
- Capability: D Permission assignment (team spec, Governance list)
- Actors and permission: Guest; explicit grant only
- Preconditions: A Guest with one or more explicit grants.
- Input: Any read.
- Output: Lists contain only granted resources; ungranted ones are NotFound; the Guest never sees workspace navigation implying more.
- Main flow: 1. Guest signs in. 2. The shell shows only granted resources. 3. Search and My Work apply the same decision.
- No-op / rejection / concurrency / retry: Search, export and analytics drill-down never widen the set. A Guest never exports (owner decision 76.15).
- UI states: empty, permission denied, read-only
- Rules: [Governance §29-§30, §62, §95](../../product/governance.md) GOV-009, GOV-019, GOV-029.
- Acceptance: Given a Guest with one board, When listing, Then one board. When opening another by id, Then 403/404.
- Evidence: BE Board list decision (GOV-029) · FE shell navigation by capability · Tests `BoardChildAccessMatrixTests`, `EffectiveBoardAccessTests` · Real e2e `frontend/e2e/real/guest-permission.real.e2e.spec.ts` · Design `Final-M-Members`
- State: HARDENING. Nothing left to build: Board lists apply the single-read decision and navigation follows capability. Missing: tests and owner check (real e2e of search, My Work and Page lists for a Guest).

### UC-GOV-D09 Make a Board Private and see who loses it
- Capability: D Permission assignment (team spec, Governance list)
- Actors and permission: Board manager; `ManageBoard`
- Preconditions: A Workspace-visible Board.
- Input: Board id.
- Output: Visibility Private: only Board members and explicit grants keep access; the impact list is shown before and recorded after.
- Main flow: 1. Open Share. 2. Choose Private. 3. The dialog lists who loses access. 4. Confirm.
- No-op / rejection / concurrency / retry: Already Private: no-op. Assignees without access trigger the warning (owner decision 78.11).
- UI states: destructive confirmation, pending, permission denied
- Rules: [Governance §23.1, §44.3](../../product/governance.md) GOV-025; Work Management owns Board visibility ([Work Management use cases](work-management.md)).
- Acceptance: Given a public-to-workspace Board, When made Private, Then non-members get NotFound.
- Evidence: BE `POST /api/v1/boards/{boardId}/sharing/private` (WorkManagement.Boards.MakePrivate) · FE `board-share-dialog.tsx` · Tests `ResourceAccessSummaryServiceTests` · Real e2e none · Design `Final-Share`
- State: INTEGRATING. Built end to end (the confirmation lists who loses access, the endpoint is served by Work Management); nothing left to build. Missing: tests and owner check (real e2e).

### UC-GOV-D10 Read the sharing and access summary of a Board
- Capability: D Permission assignment (team spec, Governance list)
- Actors and permission: Anyone who can read the Board; `ViewBoard`
- Preconditions: Board visible.
- Input: Board id.
- Output: Managers and Board members see entries, levels, sources, Space people and the log; others see counts only; a Guest is `levelCapped`.
- Main flow: 1. Share opens. 2. The read returns by caller rights.
- No-op / rejection / concurrency / retry: Cannot read the Board: NotFound identical to a missing Board. Hidden Space: no Space data returned.
- UI states: loading, empty, permission denied, read-only
- Rules: [Governance §44.3](../../product/governance.md) GOV-025, GOV-029; certification CTR-GAP-WM-BOARD-SHARING.
- Acceptance: Given a non-manager, When reading sharing, Then counts only and no `grantedBy`.
- Evidence: BE `GET /api/v1/boards/{boardId}/sharing` (WorkManagement.Boards.GetSharing), `GET /api/v1/boards/{boardId}/access` (WorkManagement.Boards.GetAccess), `IBoardSharingSummary` · FE `use-board-sharing.ts` · Tests `ResourceAccessSummaryServiceTests` · Real e2e none (grant spec reads it implicitly) · Design `Final-Share`
- State: INTEGRATING. Built for a Board; each entry now also carries `permissionVersion` for a manager (UC-GOV-D03). Not built: a Page equivalent of the sharing read (Page share is UC-GOV-D15, IMPLEMENTING). Missing: tests and owner check (a dedicated real e2e).

### UC-GOV-D11 Keep a 90-day log of Board access changes
- Capability: D Permission assignment (team spec, Governance list)
- Actors and permission: Board manager reads; system writes; `ManageBoardPermission` to read details
- Preconditions: A Board access change commits.
- Input: Change kind, subject, previous and new level.
- Output: A row in `governance.resource_access_changes` inside the request transaction, retained 90 days.
- Main flow: 1. Any grant, change, removal, Board member change or visibility change writes a row. 2. Share shows the log.
- No-op / rejection / concurrency / retry: A failed request leaves no row. Rows older than 90 days must be removed.
- UI states: empty, loading
- Rules: [Governance §44.3, §65-§72](../../product/governance.md) GOV-020, GOV-021.
- Acceptance: Given a revoke, When committed, Then a `Removed` row exists; When the request rolls back, Then none.
- Evidence: BE `ResourceAccessChangeRecorder`, `EfResourceAccessChangeReader` · FE `buildSharingLog` in work-management web · Tests `backend/tests/Notrelix.Domain.Tests/Governance/ResourceAccessChangeTests.cs` · Real e2e none · Design `Final-Share` (Nhật ký) · BE (retention) `Infrastructure/BackgroundJobs/ResourceAccessChangeRetention.cs`, `Governance:AccessChanges:Retention` in `configuration-and-runtime.md`
- State: BLOCKED on owner decision 3 (deletion and retention). Built: a hosted sweep that deletes rows past their own `retention_until` in bounded `SKIP LOCKED` batches under the worker scope (reads already hide them). It permanently deletes history and `docs/product/governance.md` sets no retention for these rows, so it is off by default (`Governance:AccessChanges:Retention:Enabled` is false and no environment config sets it) until the owner sets the rule. Page-level changes are still not recorded: the log is for Boards, as this use case says. Missing: the owner rule, then tests and owner check.

### UC-GOV-D12 End an explicit entry on a date (expiry)
- Capability: D Permission assignment (team spec, Governance list)
- Actors and permission: Board or Page manager; `ManageBoardPermission` or `ManagePagePermission`
- Preconditions: A grant exists.
- Input: Expiry instant.
- Output: Not delivered. From the instant, the entry grants nothing; managers see it marked expired until renewed.
- Main flow: Blocked.
- No-op / rejection / concurrency / retry: Grant validator rejects `ExpiresAt` today.
- UI states: unavailable
- Rules: [Governance §30.2](../../product/governance.md) GOV-031 conflicts with Product decision (owner, 2026-10-30) of Part 4, which excludes expiry from this scope.
- Acceptance: Given the decision is reversed, When an entry expires, Then the holder gets NotFound at decision time and in lists.
- Evidence: BE validator rule `Expiration is not supported for resource permissions` · Tests `GrantResourcePermissionCommandValidatorTests` · Real e2e none · Design none (Final-Share shows no expiry control)
- State: BLOCKED - Owner must decide whether GOV-031 (and the Guest default expiry pre-fill, `guestDefaultExpiryDays`) is built now or removed from the product doc. The setting already exists in `WorkspaceSettings`.

### UC-GOV-D13 Explicit permission rules with priority and time window
- Capability: D Permission assignment (team spec, Governance list)
- Actors and permission: Workspace Owner (future API); `ManageWorkspace`
- Preconditions: A rule is needed that a level cannot express.
- Input: Subject, action, scope, effect Allow or Deny, priority, window.
- Output: A rule evaluated before level thresholds; lowest priority value first; Deny wins at equal priority.
- Main flow: No API creates a rule today; rules exist only in the engine and tests.
- No-op / rejection / concurrency / retry: Condition JSON is stored and not evaluated.
- UI states: unavailable
- Rules: [Governance §18-§20](../../product/governance.md) GOV-024; WGREQ060-WGREQ062; certification P2B-CERT-004.
- Acceptance: Given two rules of equal priority, one Deny, When evaluated, Then Deny wins regardless of load order.
- Evidence: BE `Notrelix.Domain/Governance/Permissions/PermissionRule.cs`, `AccessPolicyEngine` · Tests `backend/tests/Notrelix.Domain.Tests/Governance/Permissions/PermissionRuleLifecycleTests.cs`, `PermissionRulesTests.cs` · Real e2e none · Design none
- State: DEFINED - Engine evaluates rules; no command, endpoint or UI creates them. Lead decision L15: not built until a product need exists.

### UC-GOV-D14 Apply a permission template
- Capability: D Permission assignment (team spec, Governance list)
- Actors and permission: Workspace Owner (future); `ManageWorkspace`
- Preconditions: A template exists.
- Input: Template id, target resource.
- Output: Ordinary grants are created from the template; later template edits never rewrite them.
- Main flow: Not delivered.
- No-op / rejection / concurrency / retry: A mutable template must never be read at decision time.
- UI states: unavailable
- Rules: [Governance §48-§50](../../product/governance.md) GOV-014; WGREQ111-WGREQ113.
- Acceptance: Given an applied template, When the template changes, Then existing grants are unchanged.
- Evidence: BE `Notrelix.Domain/Governance/Templates/PermissionTemplate.cs` (Domain only) · Tests `backend/tests/Notrelix.Domain.Tests/Governance/PermissionTemplateLifecycleTests.cs` and 8 sibling files · Real e2e none · Design none
- State: DEFINED - Domain only; no handler or endpoint. Not part of the owner's next scope.

### UC-GOV-D15 Share a Page
- Capability: D Permission assignment (team spec, Governance list)
- Actors and permission: Page manager; `ManagePagePermission`, `SharePage` (Editor)
- Preconditions: A visible Page.
- Input: As D01 with kind `documents.page`.
- Output: A Page grant using the same route; a Page owner decision stays in Documents.
- Main flow: 1. Page share control. 2. Add person. 3. Level.
- No-op / rejection / concurrency / retry: A shared Page never grants the Boards it embeds (UC-GOV-G09).
- UI states: as D01
- Rules: [Governance §40, §79](../../product/governance.md) GOV-010; see [Documents use cases](documents.md).
- Acceptance: Given a Page grant, When the Page embeds a Board, Then the Board still needs its own access.
- Evidence: BE same route as D01 with `documents.page` · FE `frontend/packages/product/docs/state/src/api/page-extras.api.ts` (page `/permissions` extras route, not the Governance route) · Tests `GovernanceResourcePermissionFlowTests` (Board path) · Real e2e `frontend/e2e/real/documents.real.e2e.spec.ts` (no share step) · Design none specific
- State: IMPLEMENTING - Frontend page share uses a Documents extras route, not the Governance contract; no sharing summary for Pages; no real e2e.

## Capability E: Effective permission evaluation

### UC-GOV-E01 Decide a Board or Page action in the fixed order
- Capability: E Effective permission evaluation (team spec, Governance list)
- Actors and permission: Server; the requested action
- Preconditions: Resource, actor and workspace known.
- Input: Actor, action, resource.
- Output: Allow or deny by the first matching step: resource state, Owner, explicit rules, Private restriction, Space level, workspace default.
- Main flow: 1. NotFound if missing, archived, deleted or other workspace. 2. Workspace Owner allows. 3. Explicit rules (Deny wins at equal priority). 4. Private needs membership or grant. 5. Space level. 6. Workspace default (Members allowed, Guests need a grant).
- No-op / rejection / concurrency / retry: Missing facts deny. Order is deterministic and not load-order dependent. A hidden resource is NotFound, never 403.
- UI states: permission denied, loading, read-only
- Rules: [Governance §23.1, §99](../../product/governance.md) GOV-024, GOV-025, GOV-027; WGREQ080-WGREQ086.
- Acceptance: Given a Private Board and a Space Admin who is not a member, When they read it, Then NotFound. Given the Owner, Then allowed.
- Evidence: BE `AccessPolicyEngine`, `EffectiveResourceAccess`, `PostgresAccessFactsProvider` · Tests `EffectiveBoardAccessTests`, `ResourceLevelEnforcementMatrixTests`, `BoardManagementLevelTests`, `GovernanceResourcePermissionFlowTests`, `PostgresAccessFactsProviderTests` · Real e2e `frontend/e2e/real/guest-permission.real.e2e.spec.ts` (partial) · Design none
- State: HARDENING. Nothing left to build: the engine decides in the fixed order and a hidden resource is NotFound. Missing: tests and owner check (a real e2e beyond one Guest path; Page decisions have a thinner matrix than Board decisions). The archived-board rule is a pending owner decision and was not touched.

### UC-GOV-E02 Space-derived access
- Capability: E Effective permission evaluation (team spec, Governance list)
- Actors and permission: Server; Space role mapped to a level
- Preconditions: A resource placed in a Space.
- Input: Actor, Space, resource.
- Output: Space Admin gives Manager, Member Editor, Viewer Viewer; Private Spaces stop Member and Viewer inheritance; Admin always passes.
- Main flow: 1. Compute effective Space access (explicit entries, Team entries, ancestors, open Space default). 2. Map to a level. 3. Apply thresholds.
- No-op / rejection / concurrency / retry: Unknown Space closure: deny. A Private resource ignores Space roles.
- UI states: permission denied, empty
- Rules: [Governance §23, §23.1, §23.3-§23.4](../../product/governance.md) GOV-007, GOV-025, GOV-027, GOV-028; Workspace Spaces: see [Workspaces use cases](workspaces.md).
- Acceptance: Given Space S with a Viewer, When they edit a Board in S, Then denied; When they view it, Then allowed.
- Evidence: BE `AccessPolicyEngine`, `EffectiveResourceAccess`, `PostgresAccessFactsProvider` · FE Space pages `frontend/e2e/mock/space-members.mock.e2e.spec.ts` · Tests `EffectiveBoardAccessTests`, `BoardChildAccessMatrixTests` · Real e2e none · Design `Final-Space`
- State: INTEGRATING. Built end to end (the engine maps Space roles to levels, an unknown Space closure denies, a Private resource ignores Space roles; the Space pages exist); nothing left to build. Missing: tests and owner check (real e2e of Space-derived access).

### UC-GOV-E03 A Team is an evaluated subject
- Capability: E Effective permission evaluation (team spec, Governance list)
- Actors and permission: Server; Team grants
- Preconditions: A grant addressed to a Team.
- Input: Actor, resource.
- Output: The actor holds the Team's grants only while an active member of an active Team of the same workspace.
- Main flow: 1. Facts provider loads Team memberships. 2. Engine includes Team entries.
- No-op / rejection / concurrency / retry: Removing the member or archiving the Team revokes on the next evaluation.
- UI states: permission denied
- Rules: [Governance §23.2](../../product/governance.md) GOV-026.
- Acceptance: Given a Team grant, When the Team is archived, Then members lose the derived access.
- Evidence: BE `AccessPolicyEngine`, `EffectiveResourceAccess`, `PostgresAccessFactsProvider` · Tests `backend/tests/Notrelix.Integration.Tests/Governance/PostgresAccessFactsProviderTests.cs` · Real e2e none · Design none
- State: IMPLEMENTING - No frontend or real e2e proof.

### UC-GOV-E04 Board child resources are authorized through their Board
- Capability: E Effective permission evaluation (team spec, Governance list)
- Actors and permission: Server; child action mapped to Board level
- Preconditions: An item, comment, field, view, label, form or saved filter.
- Input: Actor, child id, action.
- Output: Decision equals the Board decision; a personal saved filter is NotFound to others.
- Main flow: 1. Resolve the child's Board. 2. Evaluate Viewer for read, Commenter for comment, Editor for change, Manager for board management.
- No-op / rejection / concurrency / retry: Child of an archived or deleted Board: NotFound. A child accepts only Board-child actions.
- UI states: permission denied, read-only
- Rules: [Governance §23.6](../../product/governance.md) GOV-033.
- Acceptance: Given a Viewer, When they edit an item, Then denied. Given a Commenter, When they comment, Then allowed.
- Evidence: BE `AccessPolicyEngine`, `EffectiveResourceAccess`, `PostgresAccessFactsProvider` · Tests `BoardChildAccessMatrixTests`, `BoardChildHardeningTests`, `BoardTemplateManagementLevelTests` · Real e2e none · Design none
- State: HARDENING. Nothing left to build: a child is authorized through its Board and a personal saved filter is NotFound to others. Missing: tests and owner check (a real e2e of a child denial; Board relations are not reachable yet).

### UC-GOV-E05 Collections return what a single read would allow
- Capability: E Effective permission evaluation (team spec, Governance list)
- Actors and permission: Server; `ViewBoard`, `ViewPage`, Space visibility
- Preconditions: A list, tree, search or My Work request.
- Input: Query.
- Output: Only allowed resources, filtered inside the query.
- Main flow: 1. Query joins access facts. 2. Result equals the single-read decision.
- No-op / rejection / concurrency / retry: Never load all then hide in memory. Unknown Space closure: deny.
- UI states: loading, empty
- Rules: [Governance §23.5, §62-§63](../../product/governance.md) GOV-019, GOV-029; WGREQ170.
- Acceptance: Given 100 boards and 10 allowed, When listed, Then 10 and the query plan filters in SQL.
- Evidence: BE Board, Page, Space list queries · Tests parity tests in `backend/tests/Notrelix.Integration.Tests/Governance/GovernanceResourcePermissionFlowTests.cs` · Real e2e `frontend/e2e/real/guest-permission.real.e2e.spec.ts` · Design none
- State: HARDENING. Nothing left to build: Board, Page and Space lists filter inside the query. Missing: tests and owner check (parity tests for Page search and My Work, a load test at scale).

### UC-GOV-E06 Return the same NotFound to people without access
- Capability: E Effective permission evaluation (team spec, Governance list)
- Actors and permission: Server; none
- Preconditions: A request for a resource the actor cannot see.
- Input: Resource id.
- Output: A response identical in status, body and timing class to a missing resource.
- Main flow: 1. Locator and engine deny. 2. Pipeline maps to NotFound.
- No-op / rejection / concurrency / retry: 403 is allowed only where the actor can see the resource but lacks the action.
- UI states: not found
- Rules: [Governance §6](../../product/governance.md) GOV-025; WGREQ086, WGREQ155.
- Acceptance: Given a Private Board, When a non-member reads it, Then the body equals that of a random id.
- Evidence: BE `AccessPolicyEngine`, `EffectiveResourceAccess`, `PostgresAccessFactsProvider` · Tests `GovernanceResourcePermissionFlowTests` · Real e2e `frontend/e2e/real/guest-permission.real.e2e.spec.ts` (accepts 403 or 404) · Design none
- State: HARDENING. Nothing left to build: the pipeline maps a denied or hidden resource to `NotFoundException(kind, id)`, the same exception and body as a missing resource of that kind and id; 403 only where the resource is visible and the action is not allowed. Missing: tests and owner check (the real spec must assert NotFound, not 403 or 404).

### UC-GOV-E07 Fail closed when facts are missing, stale or unavailable
- Capability: E Effective permission evaluation (team spec, Governance list)
- Actors and permission: Server; none
- Preconditions: Facts provider error, unknown role, unresolved resource.
- Input: Any request.
- Output: Deny; no default allow; the error is logged without secrets.
- Main flow: 1. Provider fails. 2. Pipeline denies. 3. A trace id is returned.
- No-op / rejection / concurrency / retry: Placeholder cache versions such as `unknown` never allow.
- UI states: error, permission denied
- Rules: [Governance §52](../../product/governance.md) GOV-015; WGREQ085, WGREQ151, WGREQ159.
- Acceptance: Given the provider throws, When a request runs, Then denied and no data returned.
- Evidence: BE `AccessControlBehavior` · Tests `backend/tests/Notrelix.Architecture.Tests/Authorization/AuthPipelineArchitectureTests.cs`, `HandlerAuthorizationBypassArchitectureTests.cs` · Real e2e none · Design none
- State: IMPLEMENTING - No fault-injection integration test of a provider outage.

### UC-GOV-E08 Permission-sensitive cache carries scope and version
- Capability: E Effective permission evaluation (team spec, Governance list)
- Actors and permission: Server; none
- Preconditions: A cached access fact.
- Input: Workspace, subject, resource, policy and membership versions.
- Output: A cache hit only when every dimension matches; invalidation on member, role, policy, grant, share-link and Team change.
- Main flow: 1. Cache keyed by scope. 2. Change bumps version. 3. Next read misses.
- No-op / rejection / concurrency / retry: Cache outage falls back to the source, not to allow.
- UI states: none; server-only use case
- Rules: [Governance §56-§58, §84](../../product/governance.md) GOV-017; WGREQ059, WGREQ153, WGREQ161, WGREQ169.
- Acceptance: Given a grant revoked, When the subject reads again, Then denied immediately.
- Evidence: BE `Notrelix.Infrastructure/Data/Governance/Projections/ResourcePermissionInheritanceCacheEntry.cs` · Tests none named for invalidation · Real e2e none · Design none
- State: IMPLEMENTING - A projection table exists; invalidation-on-change tests are not found. Treat as unproven.

### UC-GOV-E09 Realtime access ends when authority ends
- Capability: E Effective permission evaluation (team spec, Governance list)
- Actors and permission: Connected user; view right of the subscribed resource
- Preconditions: An open subscription.
- Input: Subscribe request; later a revoke, removal, policy or Private change.
- Output: Future events stop; the client leaves or refetches the protected state.
- Main flow: 1. Subscription is authorized when opened. 2. A revoke fact triggers re-check or close. 3. The client refetches and shows NotFound.
- No-op / rejection / concurrency / retry: A late event of the old scope never overwrites new state.
- UI states: permission denied, offline, conflict
- Rules: [Governance §59-§61](../../product/governance.md) GOV-018, GOV-011; WGREQ142.
- Acceptance: Given a connected Guest, When their grant is revoked, Then no further Board events reach them without a reconnect.
- Evidence: BE `Notrelix.API/Realtime/RealtimeAuthorization.cs` (authorizes at subscribe) · Tests `backend/tests/Notrelix.Application.Tests/Common/Realtime/RealtimeSubscriptionAuthorizationTests.cs` · Real e2e none · Design none
- State: IMPLEMENTING - Authorization at subscribe time is proven; revocation while connected is not found in source or tests.

### UC-GOV-E10 Export, search and analytics keep source authorization
- Capability: E Effective permission evaluation (team spec, Governance list)
- Actors and permission: Any reader; view right plus export right
- Preconditions: An export, search or drill-down.
- Input: Query or export request.
- Output: Results contain only resources the actor may view; export requires the view right, excludes Guests, and is audited.
- Main flow: 1. Query applies the single-read decision. 2. Export records an audit entry.
- No-op / rejection / concurrency / retry: Projected data never exposes an inaccessible resource.
- UI states: permission denied, empty
- Rules: [Governance §62, §64, §83](../../product/governance.md) GOV-019; Lead decision L14; Product decision (owner, 2026-10-30) 76.15. Owners: Work Management, Analytics.
- Acceptance: Given a Guest, When they request an export, Then denied.
- Evidence: BE search and report queries of other contexts · Tests none Governance-owned · Real e2e none · Design none
- State: DEFINED - Export audit not built; owned per context, Governance supplies the rule.

### UC-GOV-E11 Entitlement and permission stay separate
- Capability: E Effective permission evaluation (team spec, Governance list)
- Actors and permission: Server; both decisions
- Preconditions: A feature with a plan limit.
- Input: Actor, action, plan.
- Output: Both must allow; a plan never grants a resource right and a resource right never creates entitlement.
- Main flow: 1. Entitlement port answers. 2. Authorization answers. 3. Both required.
- No-op / rejection / concurrency / retry: No Billing yet: the port allows (owner decision 76.2).
- UI states: none; server-only use case
- Rules: [Governance §10-§11, §77](../../product/governance.md) GOV-003; WGREQ124-WGREQ125; Product decision (owner, 2026-10-30) 76.2.
- Acceptance: Given an entitled plan, When the actor lacks the right, Then denied.
- Evidence: BE entitlement port in Application Common · Tests `backend/tests/Notrelix.Architecture.Tests` boundary rules · Real e2e none · Design none
- State: IMPLEMENTING - Billing is not built; the port always allows.

### UC-GOV-E12 Show permission state in the UI as a projection of the server
- Capability: E Effective permission evaluation (team spec, Governance list)
- Actors and permission: Any user; none (derived)
- Preconditions: A screen that depends on a right.
- Input: Server capability or denial.
- Output: Controls are enabled, disabled with a reason, or absent; protected content never flashes while unresolved.
- Main flow: 1. Load shows a safe loading state. 2. Result enables or disables. 3. A 403 or NotFound from the server wins over any local flag.
- No-op / rejection / concurrency / retry: A local `canEdit` is never authoritative.
- UI states: loading, permission denied, read-only, destructive controls inactive
- Rules: [Governance §91-§95](../../product/governance.md) GOV-023; WGREQ155.
- Acceptance: Given a Viewer, When the board opens, Then edit controls are disabled with a reason.
- Evidence: FE `frontend/packages/foundation/platform/src/permissions/ability.ts`, `core.ts`, `frontend/apps/web/src/router/guards/require-permission.ts`, `frontend/packages/ui/web/src/components/feedback/forbidden-state.tsx` · Tests `frontend/packages/foundation/platform/src/__tests__/platform-boundaries.unit.test.ts` · Real e2e none · Design `Final-States`
- State: INTEGRATING. Built end to end (loading state, disabled or absent controls, the forbidden state, a server 403 or NotFound wins over a local flag); nothing left to build. Missing: tests and owner check (real e2e, an axe check of the forbidden state).

## Capability F: Protected operation integration

### UC-GOV-F01 Declare the resource and action on every request
- Capability: F Protected operation integration (team spec, Governance list)
- Actors and permission: Developer; none
- Preconditions: A new command or query.
- Input: Request type implementing the requirement markers.
- Output: The pipeline enforces authorization before the handler.
- Main flow: 1. Implement the markers. 2. The behavior runs before the handler. 3. Architecture tests reject a request without a declaration or with a handler-local role check.
- No-op / rejection / concurrency / retry: A bypass fails the build.
- UI states: none; server-only use case
- Rules: [Governance §5-§7, §107](../../product/governance.md) GOV-001, GOV-002, GOV-005; WGREQ087-WGREQ090, WGREQ185; certification P2B-CERT-006.
- Acceptance: Given a request with no marker, When tests run, Then the architecture suite fails.
- Evidence: BE `AccessControlBehavior`, `IRequirePermission` · Tests `AuthPipelineArchitectureTests`, `HandlerAuthorizationBypassArchitectureTests`, `UseCaseSecurityClassificationTests` · Real e2e none · Design none
- State: HARDENING. Nothing left to build: the pipeline markers and the architecture tests exist. Missing: tests and owner check (release SHA evidence is recorded as partially verified in the certification).

### UC-GOV-F02 Authorize background and system actions explicitly
- Capability: F Protected operation integration (team spec, Governance list)
- Actors and permission: System, Automation, Integration principal; internal principal contract
- Preconditions: A job, consumer or automation acts for a workspace.
- Input: Principal, workspace, action.
- Output: The action runs under an explicit principal with workspace scope; it cannot exceed what that principal may do.
- Main flow: 1. The consumer sets tenant context. 2. The internal principal contract is checked. 3. RLS applies.
- No-op / rejection / concurrency / retry: Creating an automation does not freeze its author's rights forever (Automation owns the model).
- UI states: none; server-only use case
- Rules: [Governance §81-§82, §59](../../product/governance.md); WGREQ091, WGREQ126-WGREQ127; certification P2B-CERT-016.
- Acceptance: Given a consumer without tenant context, When it handles a message, Then it fails closed.
- Evidence: BE `InternalPrincipal` rules · Tests `backend/tests/Notrelix.Architecture.Tests/Authorization/InternalPrincipalArchitectureTests.cs`, `SystemContextUsageTests.cs`, `RlsPrincipalScopeArchitectureTests.cs` · Real e2e none · Design none
- State: IMPLEMENTING - Automation execution identity decided per context; no cross-context end-to-end proof.

### UC-GOV-F03 Record audit evidence for permission grant and revoke
- Capability: F Protected operation integration (team spec, Governance list)
- Actors and permission: System; none
- Preconditions: A grant or revoke commits.
- Input: Actor, action, target resource.
- Output: An `EnterpriseAuditLog` row in the same transaction with outcome Succeeded, workspace, actor and resource.
- Main flow: 1. Handler calls the audit service before commit. 2. The row commits with the change.
- No-op / rejection / concurrency / retry: A failed change leaves no audit row. Metadata holds no secret. Before and after payloads are not filled today.
- UI states: none; server-only use case
- Rules: [Governance §65-§70](../../product/governance.md) GOV-020, GOV-021; WGREQ105-WGREQ110.
- Acceptance: Given a revoke, When committed, Then an audit row with action `RevokeResourcePermission` exists.
- Evidence: BE `Notrelix.Infrastructure/Auditing/AuditService.cs`, `GrantResourcePermissionCommandHandler` · Tests `GovernanceResourcePermissionFlowTests` · Real e2e none · Design none · BE `Application/Common/Auditing/IAuditService.cs` (`AuditChange`)
- State: INTEGRATING. Built: the audit row now carries the subject (type and id), the level before and after (a level word, no secret), the correlation and causation ids, and the client address and user agent read from the request; a grant at the level the subject already holds records nothing (a semantic no-op); a failed change leaves no row. Missing: tests and owner check (the audit service is shared with Workspaces and Work Management callers, which now also get the correlation id and the client address), the read path (UC-GOV-H02).

### UC-GOV-F04 Audit every high-impact governance and administration change
- Capability: F Protected operation integration (team spec, Governance list)
- Actors and permission: System; none
- Preconditions: Any change listed below commits.
- Input: Actor, action, target, before and after (safe).
- Output: An audit row for: role change, member add/remove/suspend, API token issue/revoke, share-link create/disable, sharing policy change, workspace lifecycle, guest invite/accept, audit-log read, export.
- Main flow: 1. Each handler records through the audit service. 2. A test per action proves the row.
- No-op / rejection / concurrency / retry: Today only grant, revoke, resource invitations, accept and Board access changes are audited.
- UI states: none; server-only use case
- Rules: [Governance §69-§70](../../product/governance.md) GOV-021; WGREQ104-WGREQ108; Lead decision L3. Enterprise baseline: Atlassian, Slack, Notion.
- Acceptance: Given an Owner changes a member's role, When committed, Then an audit row exists.
- Evidence: BE `IAuditService` callers: 8 files (grant, revoke, resource invitations, accept, Board access log) · Tests none for the missing actions · Real e2e none · Design none
- State: DEFINED - Missing audit on role change, member lifecycle, API tokens, share links, sharing policy.

### UC-GOV-F05 Audit denied attempts on security-sensitive actions
- Capability: F Protected operation integration (team spec, Governance list)
- Actors and permission: System; none
- Preconditions: A denial on a governed action.
- Input: Actor, action, resource, reason code.
- Output: An audit row with outcome Denied for the governed set only; no row for ordinary read denials.
- Main flow: 1. The pipeline flags the governed set. 2. Denial writes a row outside the failed transaction.
- No-op / rejection / concurrency / retry: Rate-limited per actor to avoid noise.
- UI states: none; server-only use case
- Rules: [Governance §71](../../product/governance.md); Lead decision L4.
- Acceptance: Given a Member calls `ManageApiTokens`, When denied, Then a Denied audit row exists.
- Evidence: BE none · Tests none · Real e2e none · Design none
- State: DEFINED - Not built.

## Capability G: Share-link lifecycle and policy

### UC-GOV-G01 Create a share link for a Board or Page
- Capability: G Share-link lifecycle and policy (team spec, Governance list)
- Actors and permission: Board or Page sharer; `ShareBoardView` or `SharePage` (Editor); other kinds `ManageWorkspace`
- Preconditions: Resource visible; workspace sharing policy allows links (UC-GOV-G06).
- Input: Resource kind and id, level, optional expiry.
- Output: An Active link with a token hash stored; the raw token is returned once in the create response; audit entry; `ShareLinkCreated` fact.
- Main flow: 1. Open Share. 2. Create link with level and expiry. 3. Copy the URL (shown once). 4. The link appears in the list (UC-GOV-G04).
- No-op / rejection / concurrency / retry: Policy Disabled: refused. Expiry in the past: 422. Level above Commenter: 422 (Lead decision L5). Retry: needs an idempotency key so a retry returns the first link. Raw token never goes to logs, audit or events.
- UI states: validation error, permission denied, pending, success (URL shown once)
- Rules: [Governance §31-§39](../../product/governance.md) GOV-010, GOV-032; WGREQ097-WGREQ102, WGREQ156; Lead decisions L5, L6.
- Acceptance: Given an Editor, When they create a link at Viewer with a 7-day expiry, Then the response holds one raw token and the stored row holds only its hash.
- Evidence: BE `POST /api/v1/resources/{resourceType}/{resourceId}/share-links` (Governance.ShareLinks.Create, `CreateShareLinkCommand`) · FE none (Share dialog offers only 'copy link' of the resource address) · Domain `Notrelix.Domain/Governance/ShareLinks/ShareLink.cs`, `ShareLinkTokenHash.cs` · Tests `backend/tests/Notrelix.Domain.Tests/Governance/ShareLinks/ShareLinkTests.cs`, `ShareLinkBoundaryTests.cs`, `ShareLinkTokenHashTests.cs` · Real e2e none · Design `Final-Share` (link row)
- State: IMPLEMENTING - Defects: access mode is hard-coded `WorkspaceOnly`; request `Level` is ignored; the DTO returns `TokenHash`; no audit; no policy check; no past-expiry check; no idempotency key; no consumer of the link exists.

### UC-GOV-G02 Revoke (disable) a share link
- Capability: G Share-link lifecycle and policy (team spec, Governance list)
- Actors and permission: Board or Page sharer, workspace Owner; the resource's share action (`ShareBoardView` on a Board, `SharePage` on a Page, other kinds `ManageWorkspace`), the same action as creating a link there (decided 2026-10-30)
- Preconditions: The link exists.
- Input: Resource kind, resource id, link id.
- Output: Status Disabled; access through the link ends at once; `ShareLinkDisabled` fact; audit entry.
- Main flow: 1. Open the link row. 2. Revoke. 3. Confirm.
- No-op / rejection / concurrency / retry: Already Disabled: no-op (204). Link of another resource or workspace: NotFound (the link's kind and id are compared with the route's, so a Board manager cannot turn off a link on a board they do not manage).
- UI states: destructive confirmation, pending, permission denied
- Rules: [Governance §34, §36](../../product/governance.md) GOV-011; WGREQ102, WGREQ149.
- Acceptance: Given an active link, When revoked, Then a visitor with the old URL is denied on the next request.
- Evidence: BE `DELETE /api/v1/resources/{resourceType}/{resourceId}/share-links/{shareLinkId}` (Governance.ShareLinks.Disable, `DisableShareLinkCommand`, `ShareLinks/ShareLinkResources.cs` shared with create) · FE none · Domain `Notrelix.Domain/Governance/ShareLinks/ShareLink.cs`, `ShareLinkTokenHash.cs` · Tests `backend/tests/Notrelix.Domain.Tests/Governance/ShareLinks/ShareLinkTests.cs`, `ShareLinkBoundaryTests.cs`, `ShareLinkTokenHashTests.cs` · Real e2e none · Design none
- State: IMPLEMENTING - Built (2026-10-30): authorized on the shared resource named by the route with the action create uses, so a Board manager who is not workspace Admin can revoke their own link; the link must belong to that resource or the answer is NotFound. Left: no audit; tests and owner check (Application and API tests: Board manager allowed, member without the action and a link of another board both NotFound, already-off no-op).

### UC-GOV-G03 A visitor opens a resource through a share link
- Capability: G Share-link lifecycle and policy (team spec, Governance list)
- Actors and permission: Anonymous or signed-in visitor; the link's capability
- Preconditions: An Active, unexpired link whose workspace policy allows links.
- Input: Link token.
- Output: Read of exactly the target resource at the link's level; nothing linked or embedded.
- Main flow: Not delivered. No endpoint resolves a token.
- No-op / rejection / concurrency / retry: Revoked, expired or policy-off: the same denial as an unknown token. Rate limiting and abuse controls are needed.
- UI states: loading, error, permission denied, read-only
- Rules: [Governance §33, §37, §40](../../product/governance.md) GOV-010, GOV-011; WGREQ103, WGREQ156; Product decision (owner, 2026-10-30) 76.4 (no public Board share links in Work Management); owner item 67 (anonymous principal).
- Acceptance: Given an Active link, When a visitor opens it, Then they read the target and no other resource.
- Evidence: BE none (links can be created but never used) · FE none · Tests none · Real e2e none · Design none
- State: BLOCKED - Owner must decide the execution principal of an anonymous visitor, abuse controls and whether Board links are in scope at all (76.4 says no). Same open question as anonymous public forms.

### UC-GOV-G04 List the share links of a resource
- Capability: G Share-link lifecycle and policy (team spec, Governance list)
- Actors and permission: Board or Page sharer; `ShareBoardView` or `SharePage`
- Preconditions: Resource visible.
- Input: Resource kind and id.
- Output: Links with id, level, status, expiry, creator, created time; never the token or its hash.
- Main flow: 1. The Share dialog loads links.
- No-op / rejection / concurrency / retry: Non-sharer: none shown, counts only. Paged.
- UI states: loading, empty, permission denied
- Rules: [Governance §94](../../product/governance.md); Lead decision L6. Enterprise baseline: Notion, Asana.
- Acceptance: Given two links, When listed, Then two rows without secrets.
- Evidence: BE none · FE none · Tests none · Real e2e none · Design `Final-Share` (link row, no list)
- State: DEFINED - No endpoint. Enterprise baseline.

### UC-GOV-G05 Link expiry, password and rotation
- Capability: G Share-link lifecycle and policy (team spec, Governance list)
- Actors and permission: Link sharer; same as G01
- Preconditions: An Active link.
- Input: Expiry, password, rotate request.
- Output: Expired links stop authorizing; password mode is a capability guard only; rotation issues a new token and invalidates the old.
- Main flow: 1. Set expiry on create. 2. Optionally password. 3. Rotate when leaked.
- No-op / rejection / concurrency / retry: Expiry is evaluated server-side; client clocks do not decide. Rotate twice: each returns a new token.
- UI states: validation error, pending
- Rules: [Governance §37-§39](../../product/governance.md) GOV-010; WGREQ101.
- Acceptance: Given an expired link, When used, Then denied.
- Evidence: BE Domain `IsExpired`, `Expire`, `RotateTokenHash`, modes `Public`, `PasswordProtected`, `WorkspaceOnly` (no application path) · Domain `Notrelix.Domain/Governance/ShareLinks/ShareLink.cs`, `ShareLinkTokenHash.cs` · Tests `backend/tests/Notrelix.Domain.Tests/Governance/ShareLinks/ShareLinkTests.cs`, `ShareLinkBoundaryTests.cs`, `ShareLinkTokenHashTests.cs` · Real e2e none · Design none
- State: BLOCKED - Depends on UC-GOV-G03 (no consumer). Domain rules exist and are tested.

### UC-GOV-G06 Turn share links off for the whole workspace
- Capability: G Share-link lifecycle and policy (team spec, Governance list)
- Actors and permission: Workspace Owner, Admin; `ManageWorkspaceSettings`
- Preconditions: Workspace active; expected version of the workspace.
- Input: `allowPublicSharing` true or false, expected version.
- Output: While off, no link authorizes access and none can be created or activated; links are kept; turning on again revives links that are neither revoked nor expired; audit entry.
- Main flow: 1. Open Sharing and security. 2. Toggle links. 3. The page shows 'off since' and who can change it.
- No-op / rejection / concurrency / retry: Stale workspace version: 412. Same value: no-op. Convergence follows revocation rules.
- UI states: loading, error, conflict, pending, permission denied
- Rules: [Governance §39.1](../../product/governance.md) GOV-032, GOV-011, GOV-018; WGREQ074, WGREQ148.
- Acceptance: Given policy off, When an Editor creates a link, Then refused. Given policy off then on, Then a valid link works again.
- Evidence: BE `PUT /api/v1/workspaces/{workspaceId}/settings` (Workspaces.Settings.UpdateWorkspaceSettings), `GET .../settings` (GetWorkspaceSettings), `WorkspaceSettings.AllowPublicSharing` · FE `frontend/apps/web/src/routes/workspaces/$workspaceId/security.tsx` · Tests `backend/tests/Notrelix.Domain.Tests/Governance/Policies/WorkspacePolicyTests.cs`; Workspaces settings tests · Real e2e none (`frontend/e2e/mock/workspace-teams.mock.e2e.spec.ts` only) · Design `Final-Teams`, `Final-Settings` · BE (enforcement) `Governance/Abstractions/IWorkspaceSharingPolicy.cs`, `Infrastructure/Services/WorkspaceSharingPolicy.cs`, `CreateShareLinkCommandHandler`
- State: INTEGRATING. Built: creating a share link is refused with the rule `ShareLinksTurnedOff` while the workspace setting `AllowPublicSharing` is off (it only refuses; nothing was widened), and a workspace that never turned the setting on does not allow links. No visitor path exists to serve a link (UC-GOV-G03 waits on the owner decision on the anonymous principal), so "no link authorizes access" holds trivially. Not built: the audit entry of the setting change (UC-GOV-F04, DEFINED). Missing: tests and owner check (an Application test of the refusal, a real e2e of the toggle). Setting is owned by Workspaces ([Workspaces use cases](workspaces.md)).

### UC-GOV-G07 Show the sharing policy inside the resource Share dialog
- Capability: G Share-link lifecycle and policy (team spec, Governance list)
- Actors and permission: Board or Page sharer; `ShareBoardView`
- Preconditions: Share dialog open.
- Input: Workspace settings read.
- Output: When links are off, the dialog shows the policy state and names the workspace admins instead of a link control.
- Main flow: 1. Dialog reads settings. 2. Renders state.
- No-op / rejection / concurrency / retry: A reader who cannot change it sees the names only.
- UI states: read-only, permission denied
- Rules: [Governance §39.1, §94](../../product/governance.md) GOV-032.
- Acceptance: Given links off, When a sharer opens Share, Then the link control is replaced by the policy note.
- Evidence: FE `frontend/apps/web/src/shell/workspace/board-share-dialog.tsx` (`createUseWorkspaceSettings`) · Tests `frontend/apps/web/src/shell/workspace/__tests__/use-board-sharing.component.test.tsx` · Real e2e none · Design `Final-Share`, `Final-M-Share`
- State: INTEGRATING. Built for a Board: the dialog reads the settings and shows the policy state. Not built: a page-level Share equivalent (Page share is UC-GOV-D15, IMPLEMENTING). Missing: tests and owner check (the path only runs against the mock backend in e2e).

### UC-GOV-G08 Copy the resource address (a locator, not a share link)
- Capability: G Share-link lifecycle and policy (team spec, Governance list)
- Actors and permission: Any viewer; view right
- Preconditions: A visible resource.
- Input: None.
- Output: The app URL is copied; it opens only for people who already have access.
- Main flow: 1. Click Copy link. 2. Toast.
- No-op / rejection / concurrency / retry: Pasting it for a person without access gives NotFound.
- UI states: success toast, permission denied on open
- Rules: [Governance §39.1](../../product/governance.md) GOV-010, GOV-032.
- Acceptance: Given a non-member, When they open a copied address, Then NotFound.
- Evidence: FE `board-share-dialog.tsx` · Tests `use-board-sharing.component.test.tsx` · Real e2e none · Design `Final-Share`
- State: INTEGRATING. Built end to end (the app address is copied; a person without access gets NotFound on opening it); nothing left to build. Missing: tests and owner check (a real e2e of the non-member open).

### UC-GOV-G09 Sharing is never transitive
- Capability: G Share-link lifecycle and policy (team spec, Governance list)
- Actors and permission: Server; target resource's own rule
- Preconditions: A shared Page embeds or links a Board, or a link covers a resource that links to another.
- Input: Any read.
- Output: The linked resource is authorized on its own; the share never widens to it.
- Main flow: 1. Evaluate the target. 2. Evaluate each embedded resource separately.
- No-op / rejection / concurrency / retry: Embedded resource without access renders as 'no access', never its content.
- UI states: permission denied (embedded), empty
- Rules: [Governance §33, §40, §79](../../product/governance.md) GOV-010, GOV-007; Product decision (owner, 2026-10-30) 78.6.
- Acceptance: Given a Page shared with M that embeds Board B, When M opens the Page, Then B is shown as no-access.
- Evidence: BE per-context reads · Tests none Governance-owned · Real e2e none · Design none
- State: DEFINED - No test pins non-transitivity.

### UC-GOV-G10 Allow or stop guest invitations for the workspace (enterprise baseline)
- Capability: G Share-link lifecycle and policy (team spec, Governance list)
- Actors and permission: Workspace Owner, Admin; `ManageWorkspaceSettings`
- Preconditions: Workspace active.
- Input: `guestInvitations` On or Off, expected version.
- Output: Off: no resource invitation to an outside email can be created or accepted; existing Guests keep access.
- Main flow: 1. Open Sharing and security. 2. Switch guest invitations. 3. The share dialog hides the invite control and names admins.
- No-op / rejection / concurrency / retry: Stale version: 412. Same value: no-op.
- UI states: conflict, permission denied, pending
- Rules: [Governance §29, §31](../../product/governance.md) GOV-009; Lead decision L8. Enterprise baseline: Notion, Asana.
- Acceptance: Given invitations off, When a manager invites an email, Then refused with the reason.
- Evidence: BE none (settings hold `GuestDefaultExpiryDays` and a company domain only) · FE `security.tsx` shows guest count and domain · Tests none · Real e2e none · Design `Final-Teams`
- State: DEFINED - New workspace setting, new check in UC-GOV-D07.

## Capability H: Governance administration UI

### UC-GOV-H01 View Sharing and security of the workspace
- Capability: H Governance administration UI (team spec, Governance list)
- Actors and permission: Workspace Owner, Admin; `ViewMembers`, `ManageWorkspaceSettings`
- Preconditions: Workspace active.
- Input: Workspace id.
- Output: Links on or off with the date, guest count, default guest end-of-access, company domain.
- Main flow: 1. Open the page. 2. Read the extras.
- No-op / rejection / concurrency / retry: A Guest and a Member: 403 or hidden.
- UI states: loading, error, permission denied, success
- Rules: [Governance §94-§95, §44.3](../../product/governance.md) GOV-009, GOV-032.
- Acceptance: Given an Admin, When opening, Then the numbers match the data. Given a Member, Then 403.
- Evidence: BE `GET /api/v1/workspaces/{workspaceId}/security-ext` (Workspaces.Settings.GetWorkspaceSecurityExt) · FE `.../routes/workspaces/$workspaceId/security.tsx`, `use-teams-ext.ts` · Tests `frontend/e2e/mock/workspace-teams.mock.e2e.spec.ts` · Real e2e none · Design `Final-Teams`, `Final-Settings`
- State: INTEGRATING. Built end to end (the Sharing and security page reads `security-ext`, shows links on or off with the date, guests and the company domain; the endpoint is Workspaces'); nothing left to build here. Missing: tests and owner check (a real e2e, a 1 px check against Final-Teams and Final-Settings, the page only runs against the mock backend in e2e).

### UC-GOV-H02 View the audit log of a workspace
- Capability: H Governance administration UI (team spec, Governance list)
- Actors and permission: Workspace Owner, Admin; `ManageWorkspaceSettings` (Lead decision L3)
- Preconditions: Audit rows exist.
- Input: Workspace id, filters (actor, action, target, date range), paging.
- Output: Rows with actor, action, target, outcome, time, scope; read-only, newest first.
- Main flow: 1. Open Audit. 2. Filter. 3. Page.
- No-op / rejection / concurrency / retry: Member and Guest: 403. No edit or delete control. Reads are paged and bounded.
- UI states: loading, empty, error, permission denied
- Rules: [Governance §65-§68, §96](../../product/governance.md) GOV-020; WGREQ105-WGREQ110, WGREQ157; Lead decision L3. Enterprise baseline: Atlassian, Slack, Notion.
- Acceptance: Given an Admin, When they filter by actor and a date range, Then only matching rows return, never rows of another workspace.
- Evidence: BE none; rows are written to `Notrelix.Infrastructure/Data/Configurations/Audit/AuditLogConfiguration.cs` tables · FE `frontend/packages/features/governance/src/core/api/governance.service.ts` `listAuditLogs` calls a route that does not exist · Tests none · Real e2e none · Design none
- State: DEFINED - No endpoint, screen or artboard. Frontend service is dead code.

### UC-GOV-H03 Export the audit log
- Capability: H Governance administration UI (team spec, Governance list)
- Actors and permission: Workspace Owner; `ManageWorkspaceSettings`
- Preconditions: Rows exist.
- Input: Filters, format CSV.
- Output: A CSV of at most 10,000 rows from the last 365 days; the export is itself audited.
- Main flow: 1. Choose Export. 2. Link or download is produced.
- No-op / rejection / concurrency / retry: Too many rows: ask to narrow filters. Retry makes a new export, not a duplicate effect.
- UI states: pending, error, permission denied
- Rules: [Governance §64, §72](../../product/governance.md); Lead decision L3. Enterprise baseline: Atlassian export cap.
- Acceptance: Given a filter that matches 12,000 rows, When exporting, Then the UI asks to narrow.
- Evidence: BE none · FE none · Tests none · Real e2e none · Design none
- State: DEFINED - Not built.

### UC-GOV-H04 Audit reads of the audit log
- Capability: H Governance administration UI (team spec, Governance list)
- Actors and permission: System; none
- Preconditions: An audit read or export.
- Input: Actor and filters.
- Output: An audit row for the read.
- Main flow: 1. The read handler records itself.
- No-op / rejection / concurrency / retry: Fails closed if the record cannot be written for an export.
- UI states: none; server-only use case
- Rules: [Governance §69](../../product/governance.md); Lead decision L3.
- Acceptance: Given an export, When completed, Then an audit row names the actor and the filters.
- Evidence: BE none · Tests none · Real e2e none · Design none
- State: DEFINED - Depends on UC-GOV-H02.

### UC-GOV-H05 Issue an API token for a workspace
- Capability: H Governance administration UI (team spec, Governance list)
- Actors and permission: Workspace Owner; `ManageApiTokens` (Owner only) plus a single-use step-up proof
- Preconditions: Owner has a fresh step-up proof for `IssueApiToken`.
- Input: Name, optional expiry (future), step-up token.
- Output: A token whose raw secret is shown once; only a hash is stored; audit entry (to build).
- Main flow: 1. Owner opens API tokens. 2. Names the token and sets expiry. 3. Completes step-up. 4. Copies the secret.
- No-op / rejection / concurrency / retry: Missing or reused step-up proof: rejected. Expiry in the past: 400. Admin: 403. Retry with the same proof: refused (single use). No expiry accepted today (Lead decision L7 requires one).
- UI states: validation error, permission denied, pending, success (secret shown once)
- Rules: [Governance §35, §44.1](../../product/governance.md) GOV-013; WGREQ098, WGREQ137, WGREQ164; identity side: [Identity use cases](identity.md); Lead decision L7. Enterprise baseline: GitHub.
- Acceptance: Given an Owner with step-up, When they create a token, Then the response holds the secret once and a later list shows only prefix and metadata.
- Evidence: BE `POST /api/v1/workspaces/{workspaceId}/api-tokens` (Identity.ApiTokens.Create, `CreateApiTokenCommand`) · FE none · Tests `backend/tests/Notrelix.Application.Tests/Features/Identity/ApiTokens/ApiTokenHandlerTests.cs`, `backend/tests/Notrelix.Integration.Tests/Auth/ApiTokenHttpFlowTests.cs`, `ApiTokenFlowTests.cs`, `backend/tests/Notrelix.Integration.Tests/Rls/ApiTokenScopeTests.cs` · Real e2e none · Design none
- State: IMPLEMENTING - No screen and no artboard; no audit row; no token lifetime cap; the token's scope and rights are not narrower than its issuer's by a stated rule.

### UC-GOV-H06 List API tokens
- Capability: H Governance administration UI (team spec, Governance list)
- Actors and permission: Workspace Owner; `ManageApiTokens`
- Preconditions: Workspace active.
- Input: Workspace id.
- Output: Name, prefix, creator, created, last used, expiry, status; never the secret or hash.
- Main flow: 1. Open API tokens. 2. The list loads.
- No-op / rejection / concurrency / retry: Admin and below: 403. List is unbounded today.
- UI states: loading, empty, permission denied
- Rules: [Governance §44.1](../../product/governance.md); WGREQ137.
- Acceptance: Given an Admin, When they list, Then 403 and the Owner's tokens are not visible.
- Evidence: BE `GET /api/v1/workspaces/{workspaceId}/api-tokens` (Identity.ApiTokens.List, `ListApiTokensQuery`) · FE none · Tests `ApiTokenHandlerTests` · Real e2e none · Design none
- State: IMPLEMENTING - No screen; unbounded read.

### UC-GOV-H07 Revoke an API token
- Capability: H Governance administration UI (team spec, Governance list)
- Actors and permission: Workspace Owner; `ManageApiTokens`
- Preconditions: The token exists in the workspace.
- Input: Workspace id, token id.
- Output: Token revoked; it can no longer authenticate; audit entry (to build).
- Main flow: 1. Open the row. 2. Revoke. 3. Confirm.
- No-op / rejection / concurrency / retry: Already revoked: no-op. Token of another workspace: NotFound.
- UI states: destructive confirmation, pending, permission denied
- Rules: [Governance §44.1](../../product/governance.md); WGREQ102 analogy for credentials.
- Acceptance: Given a revoked token, When it calls the API, Then 401.
- Evidence: BE `DELETE /api/v1/workspaces/{workspaceId}/api-tokens/{tokenId}` (Identity.ApiTokens.Revoke, `RevokeApiTokenCommand`) · FE none · Tests `ApiTokenHttpFlowTests` · Real e2e none · Design none
- State: IMPLEMENTING - No screen; no audit.

### UC-GOV-H08 Cap API token lifetime by workspace policy (enterprise baseline)
- Capability: H Governance administration UI (team spec, Governance list)
- Actors and permission: Workspace Owner; `ManageApiTokens`
- Preconditions: Policy set.
- Input: Maximum lifetime in days.
- Output: A token cannot be created without an expiry within the cap; old tokens above the cap are flagged, not silently revoked.
- Main flow: 1. Owner sets a cap. 2. Issue enforces it.
- No-op / rejection / concurrency / retry: Stale workspace version: 412.
- UI states: validation error, conflict
- Rules: Lead decision L7. Enterprise baseline: GitHub org PAT policy.
- Acceptance: Given a cap of 90 days, When a token with a 120-day expiry is requested, Then 422.
- Evidence: BE none · FE none · Tests none · Real e2e none · Design none
- State: DEFINED - Not built.

### UC-GOV-H09 Manage roles on a governance screen
- Capability: H Governance administration UI (team spec, Governance list)
- Actors and permission: Workspace Owner; see C01-C06
- Preconditions: Roles delivered.
- Input: As C01-C06.
- Output: One Roles screen listing built-in and custom roles with member counts.
- Main flow: Follows C01-C06.
- No-op / rejection / concurrency / retry: As C01-C06.
- UI states: loading, empty, conflict, destructive confirmation, permission denied
- Rules: [Governance §41-§47](../../product/governance.md).
- Acceptance: Given an Owner, When they edit a role on the screen, Then C02 acceptance holds.
- Evidence: FE `frontend/packages/features/governance/src/web/index.ts` (hooks only, no screen) · Tests `governance.unit.test.ts` · Real e2e none · Design none
- State: DEFINED - Depends on C01-C06 and on a design.

### UC-GOV-H10 Read outbox statistics (platform operator)
- Capability: H Governance administration UI (team spec, Governance list)
- Actors and permission: Platform operator (`SystemAdmin` role); authorization policy `SystemAdmin`
- Preconditions: A signed-in principal holding the `SystemAdmin` role claim.
- Input: None.
- Output: Counts of outbox messages by status across all tenants.
- Main flow: 1. Operator calls the endpoint. 2. The service counts.
- No-op / rejection / concurrency / retry: Unauthenticated: 401. Tenant user: 403.
- UI states: none; no UI
- Rules: Lead decision L9; WGREQ105 (operations evidence); owner: platform, see [Platform use cases](platform-foundation.md).
- Acceptance: Given a tenant admin, When they call stats, Then 403.
- Evidence: BE `GET /admin/outbox/stats` (GetOutboxStats) · Tests `backend/tests/Notrelix.API.Tests/Admin/AdminEndpointAuthorizationTests.cs` · Real e2e none · Design none
- State: IMPLEMENTING - Operator surface only; how the `SystemAdmin` role is granted and audited is not documented.

### UC-GOV-H11 List pending and failed outbox messages
- Capability: H Governance administration UI (team spec, Governance list)
- Actors and permission: Platform operator; `SystemAdmin`
- Preconditions: As H10.
- Input: `limit` (default 50).
- Output: Recent pending or processing messages, and failed or dead-letter messages, with id, name, type, status, retry count, times and workspace id. No payload.
- Main flow: 1. Call pending or failed.
- No-op / rejection / concurrency / retry: `limit` is capped at 200 in the service (pending path).
- UI states: none; no UI
- Rules: Lead decision L9.
- Acceptance: Given limit 5000, When called, Then at most 200 rows.
- Evidence: BE `GET /admin/outbox/pending` (GetPendingOutboxMessages), `GET /admin/outbox/failed` (GetFailedOutboxMessages) · Tests `AdminEndpointAuthorizationTests` (success cases) · Real e2e none · Design none
- State: IMPLEMENTING - Cap on the failed path not verified here; cross-tenant read by design; no audit of operator reads.

### UC-GOV-H12 Read one outbox message
- Capability: H Governance administration UI (team spec, Governance list)
- Actors and permission: Platform operator; `SystemAdmin`
- Preconditions: As H10.
- Input: Message id.
- Output: Message detail or NotFound.
- Main flow: 1. Call with id.
- No-op / rejection / concurrency / retry: Unknown id: 404.
- UI states: none; no UI
- Rules: Lead decision L9.
- Acceptance: Given an unknown id, When called, Then 404.
- Evidence: BE `GET /admin/outbox/{id}` (GetOutboxMessageById) · Tests none specific · Real e2e none · Design none
- State: IMPLEMENTING - Detail fields not reviewed for payload or secret exposure; no test of the 404.

### UC-GOV-H13 Workspace administration console
- Capability: H Governance administration UI (team spec, Governance list)
- Actors and permission: Workspace Owner, Admin; `ManageWorkspaceSettings` and per-area rights
- Preconditions: Roadmap item AD-1 (admin plane).
- Input: Not specified.
- Output: A console that gathers workspace settings, members, teams, sharing, tokens, audit and board settings internals.
- Main flow: Not specified.
- No-op / rejection / concurrency / retry: Not specified.
- UI states: not specified
- Rules: Product decision pending (owner decisions 1-9 of the admin plane are not yet taken).
- Acceptance: Cannot be written until the decisions exist.
- Evidence: BE settings and members endpoints exist per context · FE routes `settings.tsx`, `members.tsx`, `security.tsx` · Tests none for the console · Real e2e `frontend/e2e/real/admin-persistence.real.e2e.spec.ts` (an item rename, not the console) · Design `Final-Settings`
- State: BLOCKED - Owner decisions 1-9 of the admin plane (roadmap AD-1) are pending. Needed: what the console contains, who may open each area, whether Admin sees Owner-only areas, navigation and the audit surface, token and role areas, board settings internals, mobile.

### UC-GOV-H14 Account administration console
- Capability: H Governance administration UI (team spec, Governance list)
- Actors and permission: Account Owner, Admin; `ManageAccount`
- Preconditions: Roadmap item AD-1.
- Input: Not specified.
- Output: Account-level administration (members, domains, identity provider, regions) gathered in one place.
- Main flow: Not specified.
- No-op / rejection / concurrency / retry: Not specified.
- UI states: not specified
- Rules: Product decision pending (same admin-plane decisions); semantics owned by Accounts, see [Accounts use cases](accounts.md).
- Acceptance: Cannot be written until the decisions exist.
- Evidence: FE `frontend/apps/web/src/routes/workspaces/$workspaceId/account/security.tsx` (personal security) · Tests none · Real e2e none · Design `Final-Account`
- State: BLOCKED - Same pending owner decisions as UC-GOV-H13; Accounts owns the content, Governance only the access rule.

## Capability I: Architecture coverage for resource kinds and actions

### UC-GOV-I01 Every endpoint declares its authorization
- Capability: I Architecture coverage for resource kinds and actions (team spec, Governance list)
- Actors and permission: Developer, CI; none
- Preconditions: A new or changed endpoint.
- Input: Route registration.
- Output: CI fails if an endpoint has no authorization policy or a handler does its own role check.
- Main flow: 1. Endpoint uses a resource, workspace or admin mapping helper. 2. Architecture tests inspect the contract.
- No-op / rejection / concurrency / retry: Admin endpoints require `SystemAdmin`; internal endpoints `InternalService`.
- UI states: none; server-only use case
- Rules: [Governance §5, §107](../../product/governance.md) GOV-001; WGREQ090, WGREQ135, WGREQ188.
- Acceptance: Given an endpoint mapped with plain `MapGet`, When tests run, Then the architecture suite fails.
- Evidence: BE `backend/src/Notrelix.API/Endpoints/EndpointMappingExtensions.cs` · Tests `backend/tests/Notrelix.Architecture.Tests/EndpointContracts/ApiArchitectureTests.cs` · Real e2e none · Design none
- State: HARDENING. Nothing left to build in source: every Governance route uses a mapping helper (`MapResourceGet`, `MapResourcePost`, `MapResourceDelete`, and `MapAdminGet` for the outbox routes). Missing: tests and owner check (the architecture inventory is the proof). The wrong action on share-link disable (`ManageWorkspace` on a `governance.share-link` resource) belongs to UC-GOV-G02; changing it would let a Board manager disable links, which widens who may act, so it waits for an owner decision.

### UC-GOV-I02 Tenant isolation holds for governance data
- Capability: I Architecture coverage for resource kinds and actions (team spec, Governance list)
- Actors and permission: Developer, CI; none
- Preconditions: A Governance table or query.
- Input: Any query.
- Output: Every Governance row is workspace-scoped with a policy; other tenants get nothing.
- Main flow: 1. Row-level security policy on each table. 2. Cross-tenant negative tests. 3. Background jobs set tenant context.
- No-op / rejection / concurrency / retry: RLS is defence in depth, never a substitute for Application authorization.
- UI states: none; server-only use case
- Rules: [Governance §61, §100](../../product/governance.md); WGREQ129-WGREQ130, WGREQ152, WGREQ187; certification P2B-CERT-009.
- Acceptance: Given workspace W1 context, When W2 permission rows are queried, Then none returned.
- Evidence: BE `Notrelix.Infrastructure/Data/Configurations/Governance` · Tests `backend/tests/Notrelix.Architecture.Tests/Authorization/RlsArchitectureTests.cs`, `backend/tests/Notrelix.Integration.Tests/Rls/ApiTokenScopeTests.cs` · Real e2e `frontend/e2e/real/workspace-isolation.real.e2e.spec.ts` · Design none
- State: HARDENING. Nothing left to build in source: `007_policies_platform.sql` applies the scoped business policies to `share_links` and `resource_access_changes`, as it does to the permission tables. Missing: tests and owner check (named cross-tenant negative tests for share links and access changes in the Governance folder).

### UC-GOV-I03 Migrate the action and resource vocabulary safely
- Capability: I Architecture coverage for resource kinds and actions (team spec, Governance list)
- Actors and permission: Developer; none
- Preconditions: An action or kind is renamed or split.
- Input: Old and new names.
- Output: Stored rules, roles, templates, audit mappings, frontend guards and API mappings move together; generated clients are regenerated.
- Main flow: 1. Expand with both names. 2. Migrate data. 3. Move consumers. 4. Remove the old.
- No-op / rejection / concurrency / retry: No enum-only rename; historical migrations stay untouched.
- UI states: none; server-only use case
- Rules: [Governance §85-§86, §101](../../product/governance.md); WGREQ052, WGREQ172-WGREQ174.
- Acceptance: Given a renamed action, When deployed, Then old stored rows still evaluate.
- Evidence: BE migrations under `Notrelix.Infrastructure/Data/Migrations` · Tests none dedicated · Real e2e none · Design none
- State: DEFINED - No rehearsal test of a vocabulary migration.

# 4. Product decisions applied

## 4.1 Owner decisions relied on

- Product decision (owner, 2026-10-27): assigning an item needs the assignee to be able to view its board; refused with "share the board first", no auto-share. Board lists use the same visibility as the single read, and deny when the Space closure is unknown (UC-GOV-E05, D09).
- Product decision (owner, 2026-10-30): no billing plan gates in Work Management now; the entitlement port allows (UC-GOV-E11).
- Product decision (owner, 2026-10-30): no expiry on Board access in this scope, and public Board share links are not built in Work Management; Guests reach Boards only through explicit membership (UC-GOV-D12, G03).
- Product decision (owner, 2026-10-30): export of the current view is for anyone who can view the Board except Guests (UC-GOV-E10).
- Product decision (owner, 2026-10-30): a relation field shows linked items only to people who can view them (UC-GOV-G09).
- Product decision (owner, 2026-10-30): the last Manager of a Board cannot be removed or demoted; the assignee access warning fires whenever access narrows (UC-GOV-D03, D09).
- Product decision (owner, 2026-10-30): a Guest invitation to one Board is a Workspaces resource invitation and its UI is the Board Share dialog (UC-GOV-D07).
- Product decision (owner, 2026-10-26): anonymous public forms wait for the owner because the execution principal and abuse controls are a security choice (UC-GOV-G03 shares this question).
- Pending (roadmap AD-1): owner decisions 1 to 9 of the admin plane are not yet taken (UC-GOV-H13, H14).

## 4.2 Lead decisions

- Lead decision (2026-10-30) L1: a custom role is a named set of permission actions at workspace scope. It adds to the built-in baseline and never subtracts. It cannot contain an Owner-only action of GOV-013. A member holds one built-in role and any number of custom roles; the effective set is the union. Explicit Deny rules outrank it. Where it enters the GOV-025 order, and whether it may reach Private resources, is not settled, so C04 and C05 are BLOCKED.
- Lead decision (2026-10-30) L2: archiving a custom role keeps its assignments visible, grants nothing from that instant, and can be reversed by activating the role. No hard delete is offered.
- Lead decision (2026-10-30) L3: workspace Owner and Admin read the audit log; only the Owner exports. Rows are append-only and kept 365 days. One export is a CSV of at most 10,000 rows. Reading and exporting are audited. This follows the Atlassian window and cap.
- Lead decision (2026-10-30) L4: denied attempts are audited only for permission management, share-link management, API-token actions, audit reads and role changes; ordinary read denials are not, and denials are rate-limited per actor.
- Lead decision (2026-10-30) L5: a share link grants Viewer or Commenter only, never Editor or above, and creation requires an expiry within a workspace maximum. v1 creates links that open for people signed in to the workspace; public and password modes come after the anonymous-principal decision.
- Lead decision (2026-10-30) L6: link lists never contain the token or its hash. The raw token is shown once, at create.
- Lead decision (2026-10-30) L7: an API token needs an expiry. The default maximum is 365 days; the Owner may set a shorter workspace cap. Tokens above a lowered cap are flagged, not revoked.
- Lead decision (2026-10-30) L8: Owner and Admin can switch guest invitations off for the workspace; existing Guests keep access.
- Lead decision (2026-10-30) L9: outbox diagnostics belong to the platform-operator plane (`SystemAdmin` role), are read-only, return no payload and may read across tenants by design. Tenant Owners and Admins never get them.
- Lead decision (2026-10-30) L10: no admin-plane console is built before the pending owner decisions. Existing per-context pages stay.
- Lead decision (2026-10-30) L11: field-level permission is deferred; no field is presented as secured until it is built end to end (GOV-008).
- Lead decision (2026-10-30) L12: while a capability is unresolved the UI shows a loading or read-only state, never an enabled destructive control.
- Lead decision (2026-10-30) L13: share-link validity is checked on every request and never cached; revocation and policy-off converge at once (GOV-011, GOV-018).
- Lead decision (2026-10-30) L14: an export needs the view right, excludes Guests and is audited.
- Lead decision (2026-10-30) L15: explicit permission rules and permission templates are not exposed through any API until a product need names them; the engine keeps evaluating stored rules.

# 5. Canonical updates required

The owning document is [Governance](../../product/governance.md) unless stated.

1. Section 44.1 and a new section: add the rule of L1 and L2 (custom role composition and archive). State the GOV-025 position once the owner decides it.
2. Section 39 and 39.1: add L5, L6 and L13: link level cap, mandatory expiry, token shown once, no cached validity. Add that the anonymous visitor principal is an owner decision.
3. Section 30.2: reconcile GOV-031 with the owner decision that excludes expiry. Either mark the section "deferred" or build it; the entry validator and `guestDefaultExpiryDays` must then agree.
4. Section 65 to 72: add L3 and L4 (audit read roles, retention 365 days, export cap, audited reads, denied-attempt set). Add the list of audited actions of UC-GOV-F04.
5. Section 44.1: add L7 (API token lifetime) next to `ManageApiTokens`.
6. Section 29: add L8 (guest invitation switch) and name the setting in the sharing policy (GOV-032 extended).
7. Section 62 and 64: add L14 (export rule).
8. Section 24: state L11 (field-level permission deferred) so no part of the product implies it.
9. [Workspaces](../../product/workspaces.md): record that the sharing policy `allowPublicSharing` is enforced by Governance at share-link creation and access time.
10. A new short section naming the platform-operator plane of L9 and the `SystemAdmin` role, owned with Platform.
11. After the owner takes decisions 1 to 9 of the admin plane: add the admin-plane section here and unblock UC-GOV-H13 and H14.

# 6. Not covered

| Product-doc section | Reason |
|---|---|
| 1 to 4, 8 to 12 (mission, ownership, authentication versus authorization, membership, entitlement) | Principles with no separate user action; exercised by UC-GOV-B01, E01, E11. |
| 16 subjects `WorkspaceRole`, `PublicLink`, `ExternalEmail` | Not evaluated; grants nothing; not a use case until the owner defines them (G03, B04 context). |
| 26 to 28 Resource, Workspace and Guest-access policy classes | Domain classes with no Application use; the live policy is the sharing flag (UC-GOV-G06) and the baseline (UC-GOV-B01). Revisit when UC-GOV-G10 builds a guest policy. |
| 54 to 55 business invariant versus authorization, no mutation as a side effect (GOV-016) | Architecture rule checked by UC-GOV-F01 tests; no actor action. |
| 74 to 77 Governance and Accounts, Workspaces, Identity, Billing | Context relations; covered by UC-GOV-B02, B03, E11 and the Workspaces catalog. |
| 78 to 83 relations with Work Management, Documents, Collaboration, Automation, Integrations, Analytics | Covered where Governance rules are exercised (UC-GOV-D15, E04, E10, F02); each owner lists its own use cases. |
| 87 to 90 new-resource admission and role-only migration | Developer process, UC-GOV-A01, I03. |
| 100 to 106 testing evidence, change impact, checklists | Review checklists; used as acceptance material in UC-GOV-A01 and I01 to I03, not separate use cases. |
| Workspace membership, invitation lifecycle, Teams, Spaces, role caps and actor-versus-target guards | Owned by Workspaces; see [Workspaces use cases](workspaces.md). |
| Credentials, sessions, MFA, step-up proof flow | Owned by Identity; step-up is used by UC-GOV-H05 only. |
| Board and Page lifecycle, board visibility mutation, board members | Owned by Work Management and Documents. |

# Endpoints with no use case

None. Every endpoint of this context appears in the Evidence line of a use case:

| Endpoint | Use case |
|---|---|
| `GET /api/v1/resources/{resourceType}/{resourceId}/permissions` | UC-GOV-D05 |
| `POST /api/v1/resources/{resourceType}/{resourceId}/permissions` | UC-GOV-D01, D02, D03, D06, D15 |
| `DELETE /api/v1/resources/{resourceType}/{resourceId}/permissions/{permissionId}` | UC-GOV-D04 |
| `POST /api/v1/resources/{resourceType}/{resourceId}/share-links` | UC-GOV-G01 |
| `DELETE /api/v1/resources/{resourceType}/{resourceId}/share-links/{shareLinkId}` | UC-GOV-G02 |
| `GET /admin/outbox/stats` | UC-GOV-H10 |
| `GET /admin/outbox/pending`, `GET /admin/outbox/failed` | UC-GOV-H11 |
| `GET /admin/outbox/{id}` | UC-GOV-H12 |
| `GET/POST/DELETE /api/v1/workspaces/{workspaceId}/api-tokens` (Identity folder) | UC-GOV-H05, H06, H07 |
| `GET /api/v1/workspaces/{workspaceId}/members/me`, `GET/PUT .../settings`, `GET .../security-ext` (Workspaces folder) | UC-GOV-B04, G06, H01 |
| `GET /api/v1/boards/{boardId}/sharing`, `GET .../access`, `POST .../sharing/private` (Work Management folder) | UC-GOV-D09, D10 |

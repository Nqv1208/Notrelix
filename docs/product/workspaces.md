---
document_id: PROD-WORKSPACES
document_type: product-context
status: active
owner: workspaces
applies_to:
  - workspaces
  - workspace-membership
  - workspace-invitations
  - spaces
  - teams
  - workspace-scope
evidence:
  - PRODUCT.md
  - docs/product/product-model.md
  - docs/product/contexts/accounts.md
  - docs/product/contexts/identity.md
  - docs/product/contexts/governance.md
  - docs/architecture/bounded-context-map.md
  - backend/src/Notrelix.Domain/Workspaces/
  - backend/docs/decisions/ADR-009-space-scoped-authorization.md
  - backend/tests/Notrelix.Domain.Tests/
  - backend/tests/Notrelix.Integration.Tests/
  - frontend/packages/features/workspace/
review_on:
  - workspace-lifecycle-change
  - workspace-membership-change
  - workspace-invitation-change
  - workspace-role-change
  - team-or-space-model-change
  - workspace-scope-change
  - workspace-switch-change
  - account-workspace-relationship-change
  - workspace-deletion-or-retention-change
---

# Workspaces Context

> **Workspaces owns the collaboration tenant structure in which most day-to-day product work is scoped.**
>
> It owns Workspace lifecycle, Workspace membership/invitations, and Workspace-owned organizational containers where product-approved.

This document is the canonical product owner for Workspaces semantics.

# 1. Mission

Workspaces provides a stable collaboration/work tenant boundary for:

```text
membership
workspace scope
resource containment/reference
organization inside a Workspace
tenant transition
workspace lifecycle
```

It does not own every resource merely because that resource is Workspace-scoped.

# 2. Does not own

```text
principal authentication
→ Identity

Account-level administration/membership
→ Accounts

generic permission/policy/sharing
→ Governance

Boards/Items
→ Work Management

Pages/Blocks
→ Documents

comments/activity
→ Collaboration

commercial entitlement
→ Billing
```

# 3. Workspace identity

A Workspace has stable identity independent of:

- display name;
- current members;
- current plan entitlement;
- frontend “current workspace” selection;
- Account display metadata.

# 4. WSP-001 — Workspace is a collaboration tenant, not a generic global scope

Workspace is the collaboration boundary for most product resources.

Do not invent Workspace scope for operations that are genuinely:

- Identity-global;
- Account-level;
- provider-global;
- system-level.

# 5. WSP-002 — Workspace scope is explicit

Workspace-scoped commands, queries, caches, search, events, background jobs, realtime subscriptions, and persistence/RLS contexts carry or resolve Workspace identity explicitly where required.

A process-local “current workspace” variable is not sufficient durable identity.

# 6. Workspace lifecycle

Workspace lifecycle may include product states such as:

```text
active
archived/suspended where product defines them
deleted
restored where allowed
```

The lifecycle is independent from Account/Billing lifecycle.

# 7. WSP-003 — Workspace lifecycle is not Account lifecycle

An Account can contain multiple Workspaces with independent lifecycle.

Account suspension may affect Workspace access through policy, but the Account does not become the Workspace aggregate.

# 8. Workspace creation

Creating a Workspace establishes:

- stable Workspace identity;
- parent Account relationship where applicable;
- initial settings;
- initial owner/member relationship;
- valid initial lifecycle.

Additional resources should be created through their own context contracts.

# 9. Account relationship

An Account may administratively contain or route Workspaces.

Accounts may control:

- who can create;
- default region;
- routing/onboarding;
- entitlement prerequisites.

Workspaces owns the created Workspace.

# 10. WSP-004 — Account relationship does not replace Workspace membership

Being an Account Member does not automatically imply active membership in every Workspace unless explicit product policy provisions it.

# 11. Workspace Member

A Workspace Member represents the relationship between a stable Identity and one Workspace.

It owns Workspace participation lifecycle.

A Workspace Member also carries a small member profile that belongs to the Workspace, not to the person's
account: the **job title** (D15, accepted 2026-10-23). It is optional text of at most 100 characters,
trimmed with inner whitespace collapsed, without control characters or line breaks; a blank value clears it.
The member may set their own title, and workspace Owner and Admin may set any member's title in that
Workspace (Governance action `ManageMemberProfile`, default Owner/Admin). It carries the member's version for
optimistic concurrency, an unchanged title is a no-op, a removed member's profile is not editable, and the
title is never visible outside the Workspace. It does not change role, status or any permission. The member
list and the Team member list expose it.

# 12. WSP-005 — Workspace membership is a security boundary

A principal must not read/write a Workspace-scoped resource merely because:

- the resource ID exists;
- they are authenticated;
- they are an Account Member.

Authoritative server mechanisms must establish the required Workspace/access facts.

# 13. Membership lifecycle

Membership may include:

```text
active
suspended
removed/left
```

where product-approved.

Invitation is not a membership state.

# 14. WSP-006 — Invitation is not Membership

A pending Workspace invitation is not active access.

Acceptance creates/resolves membership through explicit workflow.

Revoked/expired invitations must not authorize access.

# 15. Membership role

Workspace role expresses Workspace-level participation/administration where product-approved.

It may be:

```text
owner
admin
member
guest
```

or another defined vocabulary.

# 16. WSP-007 — Workspace role is not universal resource permission

A Workspace role may be an input to Governance authorization.

It does not automatically grant every action on every Board/Page/resource.

Resource policy and context semantics still apply.

# 17. Last-owner/admin invariant

Workspace should not enter an administratively orphaned state when product policy requires at least one owner/admin.

Removing, leaving, or demoting the last required owner must fail before authoritative mutation.

# 18. WSP-008 — Cross-member invariants fail atomically

When membership changes depend on external counts/facts:

- Application may load needed facts;
- Domain validates transition;
- rejection leaves state/version/events unchanged.

Do not hide repository callbacks inside Domain rules.

# 19. Leave Workspace

Leaving is a membership transition, not Identity deletion.

It should define:

- last-owner restriction;
- transfer requirements if any;
- effect on assigned work;
- historical attribution;
- future notifications.

# 20. Remove Member

Removal ends one Workspace participation relationship.

It does not globally disable Identity.

# 21. Suspend Member

Suspension can temporarily block Workspace participation while retaining the relationship/history if product policy supports it.

# 22. Guest/external participation

Guest/external membership may have constrained Workspace/resource access.

Do not infer guest semantics only from email domain.

# 22.1. WSP-026 — Outsiders join through an invitation, never through a grant to a raw email

Decided 2026-10-12 (UI mockup review). A person who is not yet a Workspace member can be brought in only this way:

```text
Workspace Owner/Admin
  → Workspace invitation, role member or guest; it may name initial Teams and Space
    entries, applied atomically when the invitation is accepted
Principal with ManageBoardPermission / ManagePagePermission on a resource
  → resource invitation by email; accepting it creates (or reuses) a guest Workspace
    membership plus the explicit resource entry on that one resource, capped by GOV-030
```

- a resource invitation never creates a `member` membership and never adds Space or Team entries;
- Space member management adds only active Workspace members and Teams (WSP-024); it cannot invite an outsider, and the UI offers a request to a workspace admin or a guest invitation on a specific Board instead;
- until accepted, an invitation grants nothing (WSP-006) and is shown as pending with resend/revoke actions to whoever issued it;
- the stored subject of the resulting grant is the guest's User identity, so Governance keeps evaluating only User and Team subjects (GOV §16); an `ExternalEmail` subject is never created by this flow;
- guest or member is a fact of the membership, never inferred from the email domain (§22).
- the resource invitation is `POST /api/v1/resources/{resourceType}/{resourceId}/invitations` (Board or Page only); it is authorized as `ManageBoardPermission` / `ManagePagePermission` on the resource and the requested level may not exceed the inviter's own level; a level above Editor is rejected when the invitation is created;
- naming initial Team or Space entries on a Workspace invitation is authorized as `ManageTeams` (Team entries) and `ManageSpaces` (Space entries), which are Workspace Owner/Admin by default (GOV-028); an invitation naming both kinds needs both actions, so a custom rule granting only one of them does not authorize it; a Guest invitation cannot name a Team lead or a Space admin entry;
- issuing an invitation, adding a member directly, and removing, suspending or activating a member are Workspace Owner and Admin actions (decided 2026-10-23); an Admin adds only a member or guest and acts only on member and guest targets, never on an Owner or another Admin (rule codes `Workspaces_Member_ActorCannotAssignRole`, `Workspaces_Member_ActorCannotActOnMember`; matrix in `governance.md` section 44.1);
- a Workspace invitation offers `member` or `guest` only (decided 2026-10-23): `admin` is granted by changing the role of an existing member after they join, never by an invitation. Creating an invitation or changing a pending invitation's role to `admin` is rejected with rule code `Workspaces_Invitation_CannotInviteAsAdmin`. An Admin invitation issued before this rule stays as stored: it can still be accepted, resent or revoked, and its role can be lowered to member or guest, but never kept or raised to admin through a role change; no data migration is needed;
- the issuer of a resource invitation may resend and revoke their own: `POST /api/v1/resources/{resourceType}/{resourceId}/invitations/{invitationId}/resend` and `DELETE /api/v1/resources/{resourceType}/{resourceId}/invitations/{invitationId}`. They are authorized as `ManageBoardPermission` / `ManagePagePermission` on the resource, so an issuer who lost that permission is denied, and they act only on an invitation they issued for that exact resource (another member's invitation, or one for another resource, is not found). A Workspace Owner/Admin resends or revokes any invitation through the workspace endpoints. A resend replaces the token (the old link stops working) and a revoked invitation can no longer be accepted or resent;
- a resource invitation is delivered with its own email template (`resource-invitation`) that names the Board or Page and the inviter instead of "a workspace"; the delivery event carries the resource fields additively (`ResourceKind`, `ResourceId`, `ResourceName`, `InviterName`), absent for a Workspace invitation, so the workspace template and existing consumers are unchanged;
- acceptance applies the membership, the entries and the resource entry in one transaction; if a named Team or Space is no longer active, or the resource is gone, acceptance fails closed and changes nothing, and the issuer must invite again; accepting reuses an existing membership and an existing entry (an entry is raised to the invited level, never lowered); a removed or suspended member cannot accept an invitation that carries entries or a resource entry.

---

# 23. Workspace invitation

Invitation represents proposed Workspace participation.

It may include:

```text
Workspace
invitee identity/email
intended role
inviter
expiry
token/claim identity
status
```

# 24. WSP-009 — Invitation acceptance is retry-safe

Accepting the same valid invitation repeatedly must not create duplicate memberships.

Revocation/acceptance races need deterministic outcome.

# 25. Invitation token

Raw invitation secrets/tokens are security-sensitive.

Workspace product semantics may expose safe invitation identity/status, not raw reusable token storage.

# 26. Invitation expiry

Expired invitations cannot be used to create access.

Resending may create a new invitation authority or extend according to explicit policy.

# 27. Account invitation versus Workspace invitation

Account invitations belong to Accounts.

Workspace invitations belong to Workspaces.

They may be combined in onboarding flow, but they remain different scopes.

# 28. Identity relation

Workspace Member references stable Identity.

Workspaces does not own:

- password;
- MFA;
- sessions;
- OAuth login.

# 29. WSP-010 — One Identity may belong to many Workspaces

Identity is global/stable enough to participate in several Workspaces.

Workspace-specific role/status must not be stored as one User-global property.

# 30. Governance relation

Governance consumes Workspace facts such as:

- membership;
- role;
- Workspace/resource relation.

Governance evaluates operation-level access.

# 31. Billing relation

Billing entitlement can limit:

- Workspace creation;
- member count;
- premium Workspace features.

Billing does not own membership or Workspace lifecycle.

# 32. WSP-011 — Entitlement and membership are distinct

A Workspace Member may be valid while a capability is not commercially entitled.

A paid plan does not automatically grant a principal membership.

# 33. Work Management relation

Boards/Items are often Workspace-scoped.

Work Management owns those resources.

Workspaces supplies tenant identity/membership facts.

# 34. Documents relation

Pages/Documents may be Workspace-scoped.

Documents remains owner of document content/lifecycle.

# 35. Collaboration relation

Comments/activity/notifications target Workspace-scoped resources.

Collaboration owns those facts.

# 36. Automation relation

Automations may be Workspace-scoped and react to Workspace resources.

Automation owns rules/executions.

# 37. Integrations relation

Connections may be Account- or Workspace-scoped depending on product semantics.

A Workspace integration does not make Workspaces own provider synchronization.

# 38. Analytics relation

Analytics may aggregate Workspace-level facts.

Analytics remains derived.

# 39. Workspace settings

Workspace-owned settings should be limited to semantics genuinely attached to Workspace lifecycle/behavior.

Do not put Account-wide SSO/SCIM/Billing/security policy into Workspace settings merely for UI convenience.

# 40. WSP-012 — Workspace settings cannot absorb Account administration

Account-wide domain, IdP, SCIM, region, ownership, and commercial semantics remain with their canonical owners.

# 41. Spaces

A Space is a Workspace-owned organizational container. Product-approved use (2026-10-11): a Workspace is composed of department Spaces (for example IT, Accounting, Marketing) organized as an org tree, with people partitioned per department.

A Space has:

```text
stable identity
Workspace (tenant) — immutable
parent Space (optional) — same Workspace only
sibling position (fractional ordering key)
name, description, type (Folder | Portfolio | Department | ProjectArea)
visibility (Private | Workspace)
status (Active | Archived), soft deletion
members (Users and Teams with a Space role)
```

Boards (Work Management) and Pages (Documents) may be placed in a Space. Placement is a reference held by the resource owner; the Space never owns their lifecycle (§79).

# 42. WSP-013 — Space is organizational structure, not implicit authorization

Space hierarchy, visibility and membership are inputs to Governance policy (`docs/product/governance.md` GOV-025..GOV-029).

They never become an undocumented permission shortcut: the meaning of a Space role for a protected operation is defined by Governance, not by the Space or by a client.

# 43. Space hierarchy

# 43.1. WSP-021 — Space hierarchy is one acyclic tree per Workspace

- a parent Space must belong to the same Workspace;
- cycles are forbidden: a Space cannot become its own ancestor (moving a Space under itself or any of its descendants is rejected before mutation);
- depth is unlimited;
- moving a Space moves its whole subtree and preserves Workspace scope;
- siblings are ordered by a fractional position key; placement is neighbor-relative and a stale neighbor intent is rejected, not guessed;
- sibling names are unique, case-insensitively, among the non-deleted Spaces under the same parent (the Workspace root counts as one parent);
- a Space cannot be created under, or moved under, an archived or deleted parent.

The Domain validates cycles and sibling uniqueness from facts supplied by Application (ancestry of the target parent, sibling names); it never queries the hierarchy itself.

# 43.2. WSP-022 — Archive and delete never cascade through a Space

To keep organization changes from silently hiding or destroying work owned by other contexts:

```text
archive Space
→ rejected while it has an active (not archived, not deleted) child Space
  or an active Board/Page placed in it

delete Space
→ rejected while it has any non-deleted child Space
  or any non-deleted Board/Page placed in it

unarchive/restore Space
→ rejected while its parent is archived or deleted
```

The owner empties, moves, archives or deletes the children and resources first. There is no ORM/database cascade and no implicit move to the parent.

# 44. Space visibility

# 44.1. WSP-023 — Space visibility controls who inherits access, Governance defines what access means

```text
Private
→ only the Space's own members (and Space Admins of an ancestor,
  workspace Owners/Admins) reach the Space and what is placed in it

Workspace
→ open to everyone who can reach the parent;
  a root Space with Workspace visibility is open to every
  workspace Member (not Guests)
```

A department Space (`type = Department`) is Private by default when the creator does not choose a visibility; other Space types default to Workspace (open to the parent's audience). Visibility can be changed later by a Space manager.

A visibility label alone never implements resource access; Governance composes it (GOV-025).

# 44.2. Space membership

# 44.3. WSP-024 — Space membership is Users and Teams with a Space role

A Space member entry is:

```text
subject: User (an active member of the same Workspace)
       | Team (an active Team of the same Workspace)
role:    Admin | Member | Viewer
```

- the creator of a Space becomes its first Space Admin;
- one entry per subject per Space; changing the role of an existing entry is an explicit operation;
- the last Admin entry of a Space cannot be removed or downgraded (mirrors the last-Team-lead rule); workspace Owners/Admins can still manage every Space, so a Space is never administratively orphaned;
- a Team entry grants its role to the Team's active members only while the Team itself is active;
- membership inherits downward: a member of a Space is a member of its descendants with the same role, except that a Private descendant stops Member and Viewer inheritance (it keeps its own member list); Admin inheritance is not stopped, so a Space Admin manages the whole subtree;
- membership changes are versioned Space mutations (expected version) so concurrent last-Admin races resolve deterministically.

Existing Spaces created before membership existed receive their creator as Space Admin during migration.

# 45. Teams

A Team can group Workspace members for collaboration/organization.

Current source includes Team and TeamMember concepts.

Team membership is distinct from Workspace membership.

# 46. WSP-014 — Team membership requires Workspace membership compatibility

A Team belongs to a Workspace.

A Team Member should reference a principal/member compatible with that Workspace.

Do not allow cross-Workspace Team membership through raw IDs.

# 47. Team role

Team-level role (Lead/Member) describes responsibility inside the Team.

It is not a Workspace role, not a Space role and not a universal resource permission.

# 48. Teams and Governance

# 48.1. WSP-025 — A Team is a membership group that Governance may evaluate as a subject

Workspaces owns Team composition (members, lead, lifecycle).

Governance owns what a Team grant means: a Team may be the subject of a Space membership entry, a resource permission or a permission rule (GOV-026). Managing Teams is a workspace Owner/Admin capability by default.


# 48.2. Read extras for the Space, Team and security pages

Three read-only queries serve the department Members tab, the Teams pages and the sharing and security page. They add no durable fact and no new owner: each is composed from Workspaces facts and filtered by Governance visibility.

- `GET /workspaces/{workspaceId}/spaces/{spaceId}/members-extra`: people reached by the Space's User and Team entries, once each at their highest role (Teams expanded; Guests are Board-level grantees and are not counted), with job title, the Space's Teams that reach the person, and who made the Space; plus pending, unexpired invitations that name the Space (WSP-026). A Space the caller cannot see is NotFound.
- `GET /workspaces/{workspaceId}/teams-ext`: per Team its people, who created and archived it (an archived Team accepts no change, so its last update is the archival), and the Spaces the caller can see where it holds a role, with the people who also hold a direct entry; plus Workspace Owners and Admins and the non-Guest active members a Team may take.
- `GET /workspaces/{workspaceId}/security-ext`: active Guest count, the default Guest expiry for new entries, and when share links were turned off (the Workspace creation instant if the policy never changed; null while links are on).

All three need `ViewMembers`, so a Guest is refused (GOV-009).

Intentionally empty fields, not defects. The web client hides them until the named decision exists:

- `email` (people, admins, directory, Team members): shown only to a Workspace Owner or Admin and only for active non-Guest members of the same Workspace; every other reader gets null and a Guest's address is never shown. The address comes from the Identity public facts port (`IIdentityUserFacts.FindEmailsAsync`); Workspaces holds the visibility rule (`IMemberEmailDisclosure`) and reads no Identity table.
- `lastActiveAt` null and `activeNow` false: no activity source exists.
- Space `guests` of `members-extra`: always empty; Board guests of a Space are served by the Space overview (`access.guests`, 48.3) from the Board sharing read (`GET /boards/{boardId}/sharing`, Work Management), where a guest is the Workspace role Guest. `invitations[].name` is null.
- `companyDomain`: the Account's primary verified domain, read from the Accounts public fact (`IAccountDomainFacts`, ACC-010); null when the Account has none, and the page hides the row.
- Team usage lists Space entries only; Board grants of a Team are not listed yet.

The Workspace sharing policy also stores `guestDefaultExpiryDays` (30, 90, 180 or none). It only pre-fills new Guest entries and never changes existing ones (GOV-031); it is written with the Workspace settings by an Owner or Admin.

# 48.3. Space overview (CTR-GAP-WS-SPACE-OVERVIEW)

`GET /workspaces/{workspaceId}/spaces/{spaceId}/overview[?as=member]` is a Workspaces read facade for the department page. It returns structured figures only (counts, ids, kinds, flags, times); the client composes every sentence and label. It adds no durable fact: people and child Spaces are Workspaces facts, Board figures are Work Management's published facts (`IBoardWorkSummaryFacts`) and root Page figures are Documents' (`IPageSpaceOverviewFacts`). Workspaces reads no Work or Documents table.

Authorization (all through the pipeline): the caller needs `ViewMembers` on the Workspace (a Guest is refused, GOV-009) and the Space must be one the caller can see, otherwise it is NotFound exactly like a missing Space (GOV-025, GOV-029). Boards and Pages enter only through the Governance visible id sets (`IResourceVisibility`), so a Board or Page the caller cannot read never appears, is never counted and never contributes to any figure. `?as=member` previews a plain department member and only a Space manager may ask (403 otherwise): Private Boards, Private child Spaces and Board guests are left out and not counted.

**Running board rule.** A Board is *running* when it is not archived, not deleted, readable by the viewer, and has at least one unfinished Item. An Item counts when it is not archived and not deleted; it is unfinished when it has no completion time; it may be a sub-Item. Finished Boards (every Item done) and Boards without Items are not running: they appear only in `boardTotal`, which counts the Boards of the Space that are readable and not archived. `boards` lists the running Boards, most recently changed first, at most six (`MaxBoards`); `docs` are capped at four (`MaxDocs`); the totals (`boardTotal`, `docTotal`) keep counting past the caps.

Per Board (accepted 2026-10-24): `kind` is `inbox` for an Intake Board, else `kanban` when the Board's default, not archived View is a Kanban View, else `table` (a Board without a default View, or whose default View is not Kanban, is a `table`; a non-default Kanban View never makes a Board a kanban); `progress` is done of total on top-level Items (null without any); `attention` is `overdue` (unfinished Items past their due time), `waitingReview` (unfinished Items with at least one pending approval request, each Item counted once however many requests it has; a request aimed at the Board itself is not an Item) and `unclaimed` (unfinished top-level Items nobody is assigned to), null when all are zero; `people` are the up to three people with the most unfinished Items; `updatedAt` is the latest change of the Board or of any of its Items; a Private Board carries `addedCount` (its members).

The page aside lists only non-zero `attention` entries: `overdue`, `unassigned` (unclaimed Items on inbox Boards), `review` and `stale` (running Boards untouched for 14 days), each with the Board holding most of the count (the stalest Board for `stale`). `workload` is the top five (`MaxWorkload`) people by unfinished Items across the readable Boards of the Space (most open first, then most overdue). `docTotal` counts readable root Pages of the Space (no parent, not archived) and `docs` lists the four most recently changed with the last editor.

People: `peopleTotal` counts everyone the Space's User and Team entries reach once, Teams expanded, at the highest role, Guests excluded (WSP-026); `people` are the first four (admins first). `children` lists only child Spaces the caller can read (and, in the preview, only Workspace-visible ones) with their head count and the number of readable Boards in them; a child the caller cannot read is omitted, never zero. `access` carries admins, and members and viewers as a head count plus the Team entries at that role with their head count; `access.guests` are Workspace Guests with access to readable Boards of the Space, taken from the Board sharing read, so only a caller who may see a Board's entries (Board manager or member) gets them.

Not served: templates (the first-step template catalog has no owner yet), the per-Doc "linked Items" note and the editor avatar.

# 48.4. Onboarding owned by Workspaces

Accepted 2026-10-23. Workspaces owns the pre-workspace onboarding state and the getting-started card; it creates no workspace by itself (creation stays `CreateWorkspace`, departments `CreateSpace`, invitations `InviteMember`, the first Board `Boards.Create`).

- `GET /accounts/{accountId}/workspaces/slug-check?slug=`: `free`, `taken` or `invalid`, plus the next free address (`name-2`, `name-3`, ...) when taken, and a stable `reason` rule code when `invalid`. An address is unique within the Account only, never globally: active (not deleted) workspaces of the Account hold addresses, exactly as the unique index does (`ux_workspaces_account_slug_active`). It needs the permission to create a workspace in the Account, so it never tells another Account's members anything.
- **Workspace address rule** (accepted 2026-10-24, Domain `WorkspaceSlugRules`, enforced by `Workspace.Create` and so by every creation path and by the slug check): after trimming and lower-casing, lowercase letters, digits and single hyphens (`SharedKernel_Slug_InvalidFormat`), 3 to 63 characters (`Workspaces_Workspace_SlugTooShort`, `Workspaces_Workspace_SlugTooLong`), and not one of the reserved words `admin`, `api`, `app`, `www`, `settings`, `login`, `signup`, `billing`, `support`, `help`, `new`, `invite`, `join`, `onboarding`, `workspaces` (`Workspaces_Workspace_SlugReserved`; only the exact word is reserved, `admin-team` is free). `CreateWorkspace` takes an optional `slug`: a chosen address that breaks the rule is refused with its rule code and one already held in the Account is a conflict (`workspaces.workspace.slug-taken`); neither is silently changed. When no slug is sent the address is derived from the name, and a derived address that is too short, reserved or taken gets a short random suffix instead of failing the creation. The rule applies when an address is chosen. A workspace created before it keeps its address, whatever it is, and every read, resolve and other operation keeps working: no migration rewrites a stored slug and no read re-validates one. There is no slug rename today (`Rename` changes the name only); a future rename must call the same rule.
- **Workspace size and language** (accepted 2026-10-24, owned by Workspaces): `size` is one of `1-10`, `11-50`, `51-200`, `201-1000`, `1000+` and `language` is `vi` or `en`; both are part of the workspace settings value (columns `settings_size`, `settings_language`, nullable, additive migration), unset (null) on a workspace that never chose them. They are set when the workspace is created (`CreateWorkspace`) and changed afterwards by an Owner or Admin through the Workspace settings write (`PUT .../settings`, the existing `ManageWorkspace` permission; a field left out keeps its stored value). They are returned by the workspace reads (`size`, `language` on the workspace DTO) and the settings read. An invalid value is refused with `Workspaces_Settings_SizeInvalid` or `Workspaces_Settings_LanguageInvalid`.
- `GET|PUT|DELETE /workspaces/onboarding-draft`: the caller's own unfinished onboarding (`step`, optional `workspaceId`, and a JSON-object `data` of at most 64 KB the server does not interpret). One draft per user, 30 days after the last save (an expired draft reads as absent and the next save replaces it); the subject is always the principal and RLS repeats it on the user. It is personal state, never workspace truth, and it holds no secret by contract. A hosted sweep (`OnboardingDraftSweepService`, options `Workspaces:Onboarding:DraftSweep`: `Enabled`, `Interval` 1 hour, `BatchSize` 500) deletes rows whose `expires_at` has passed in bounded batches under the worker scope with `FOR UPDATE SKIP LOCKED`, so several instances can run it; it only reclaims storage because an expired draft already reads as absent.
- `GET /workspaces/{workspaceId}/getting-started` and `POST .../getting-started/dismiss`: any member, Guests included (`ViewWorkspace`). Served items (derived, never stored): `first-item`, done when a Board the caller can read has an active Item (`boardId` is then the Board of the earliest created Item, otherwise the newest Board the caller can read), and `invite-more` (accepted 2026-10-24), done once at least one invitation was ever sent in the Workspace, whatever became of it (accepted, revoked or expired still count as sent; another workspace's invitations do not). The `mobile` item is not returned until a signal source exists. `dismissed` is stored per user and Workspace and dismissing again is a no-op. `visible` is derived: the card is shown unless the caller dismissed it or `first-item` is done (`invite-more` alone never hides it).
- **Join page** (accepted 2026-10-24): the invitation read by token (`GetInvitationByToken`) shows the holder of the token the inviter's display name (`inviterName`, a generic label when the inviter has none) and never the inviter's email or any other inviter fact. The notification preferences a person sets while joining use the existing Collaboration preference matrix (`notifications.notification_preferences`, per workspace and type, in-app and email channels, Guests included); the join flow has no preference contract of its own.

# 49. Organizational containers

Spaces and Teams exist because they provide real organization value (department org tree, people partitioning).

Do not create more hierarchy levels or container kinds by symmetry.

# 50. Workspace switch

Workspace switch is a scope transition across frontend/backend state.

It is not simply changing one client variable.

# 51. WSP-015 — Workspace switch invalidates old-scope assumptions

On switch:

```text
old subscriptions dispose
old query/cache scope no longer applies
late old HTTP responses must not patch new scope
new membership/authz resolves
new child data loads
```

# 52. Backend scope

Every Workspace-scoped request establishes/validates Workspace identity server-side.

Backend never trusts only a client “active workspace” singleton.

# 53. Frontend scope

Frontend state/query keys/realtime subscriptions include Workspace scope where required.

Selection state is provisional UI context; it cannot override server authorization.

# 54. RLS/persistence

Workspace scope should propagate into persistence/RLS context where the data model requires tenant isolation.

RLS is defense in depth, not the sole membership/permission rule.

# 55. Cache/search/realtime

Workspace identity participates in cache, search, query, event, and realtime scope as required to avoid cross-tenant collisions/leakage.

# 56. WSP-016 — Workspace identity survives every relevant boundary

Scope must not disappear when work moves from:

```text
HTTP
→ Application
→ DB/cache/search
→ outbox/message
→ background worker
→ realtime
→ frontend query cache
```

# 57. Workspace facts/events

Potential stable facts include:

```text
WorkspaceCreated
WorkspaceRenamed
WorkspaceArchived/Suspended
WorkspaceDeleted
WorkspaceRestored
WorkspaceMemberAdded
WorkspaceMemberRoleChanged
WorkspaceMemberRemoved
WorkspaceInvitationAccepted/Revoked
SpaceCreated/Moved
TeamMembershipChanged
```

Only publicize facts with stable consumers.

# 58. WSP-017 — Workspace events carry stable scope, not foreign aggregates

Cross-boundary events should carry stable IDs/facts.

Do not serialize complete Account/User/Board/Page aggregates into Workspace events.

# 59. Account lifecycle effects

Account suspension/closure may restrict Workspace operations.

The Account fact comes from Accounts.

Workspaces owns how its Workspace lifecycle/access reacts according to approved policy.

# 60. Workspace archive

Archive may:

- preserve data;
- block new mutation;
- retain historical access;
- pause integrations/automation.

Exact semantics belong to Workspaces plus participant contexts.

# 61. Workspace deletion

Deletion has cross-context consequences.

It must define:

```text
members
Boards
Documents
Collaboration
Automation
Integrations
Billing association
Analytics
audit
retention/export
```

# 62. WSP-018 — Workspace deletion is a process, not ORM cascade

A database cascade cannot define product retention, legal/export, provider, or historical-reference semantics.

# 63. Restore

If Workspace restore exists, define:

- membership restoration;
- child-resource access;
- integrations/automation;
- deletion window;
- what remains irreversible.

# 64. Cross-context deletion

Workspaces should not directly delete every downstream aggregate.

Use explicit workflow/contracts/events and context-owned retention rules.

# 65. Failure — no membership

Authenticated principal with no valid membership cannot use membership-required Workspace resources.

# 66. Failure — invitation not active

Expired/revoked/accepted invitation cannot be reused as active invitation authority.

# 67. Failure — last owner

Leave/remove/demote that violates last-owner policy fails before mutation.

# 68. Failure — cross-Workspace parent/member

Space/Team relationships must reject resources/members from another Workspace.

A Space parent, Space member (User or Team) or placed Board/Page from another Workspace is rejected before mutation.

# 69. Failure — stale Workspace switch

Old Workspace request/realtime response arriving after switch must not contaminate current scope.

# 70. Failure — entitlement

Workspace creation/member limit/premium feature can fail due to Billing entitlement.

This is different from membership/authorization failure.

# 71. Concurrency

High-risk membership/role/lifecycle changes should avoid silent stale overwrite.

Examples:

- simultaneous owner demotions;
- invitation accept/revoke race;
- Workspace archive vs mutation;
- Team membership changes.

# 72. WSP-019 — Membership and invitation races resolve deterministically

Retry/concurrent acceptance/removal must not produce duplicate active memberships or violate owner invariants.

# 73. Idempotency

Creation/invitation acceptance/member removal may be retried at transport/application level.

Stable operation semantics must prevent duplicated membership/resource effects.

# 74. Workspace onboarding

Conceptual:

```text
Account/principal eligible
→ create Workspace
→ establish owner membership
→ create minimal settings
→ optional product setup through owning context contracts
```

# 75. Invite onboarding

```text
issue Workspace invitation
→ invitee authenticates/resolves Identity
→ validate invitation
→ create/activate membership
→ invitation becomes terminal
```

# 76. Account-to-Workspace provisioning

Accounts/SCIM may provision Workspace memberships through explicit Workspaces operations.

Provisioning does not directly edit Workspace tables from Accounts.

# 77. WSP-020 — External provisioning uses Workspaces contracts

SCIM/Account orchestration can request membership changes.

Workspaces remains responsible for membership invariants.

# 78. Team onboarding

Creating Team establishes Workspace-owned Team identity.

Adding Team Member must respect Workspace membership compatibility.

# 79. Space organization

Moving resources into Spaces requires the target resource/context to support the relationship explicitly.

Space containment does not become a generic cross-context object graph.

Current approved placements:

```text
Board (Work Management)  → work.boards.space_id
Page  (Documents)        → docs.pages.space_id
```

- the resource owner stores and changes the placement; Workspaces publishes the Space facts it needs (exists, same Workspace, active);
- a nested Page follows the Space of its root Page; moving a root Page to a Space moves its whole Page subtree;
- creating a resource in a Space and moving a resource between Spaces are authorized on the target Space (and, for a move, on the resource) by Governance (GOV-028).

# 80. Audit

High-impact Workspace operations may require audit:

- owner/admin change;
- member remove/suspend;
- invitation/security-sensitive changes;
- archive/delete/restore;
- Team/Space policy-relevant changes.

Audit is Governance/security evidence, not ordinary activity.

# 81. Activity

User-facing collaboration activity may report Workspace changes.

It remains Collaboration semantics and may be curated differently from audit.

# 82. Privacy

Workspace membership lists, guest identities, Teams, invitations, and private Spaces can be sensitive.

Queries/search/realtime must enforce scope and authorization.

# 83. Frontend implications

The frontend Workspace feature should own:

- Workspace selection/switch UX;
- Workspace membership/admin surfaces;
- invitations;
- Workspace-owned organizational settings.

It must not absorb Account-wide administration or resource-specific business state.

# 84. Scope indicator

Current Workspace should be visible enough to avoid accidental actions in the wrong tenant.

Especially during:

- member management;
- destructive actions;
- Team/Space organization;
- Workspace settings.

# 85. Empty state

A new Workspace may legitimately contain no Boards/Documents yet.

The empty experience should route creation to the owning product capabilities without implying Workspaces owns those resources.

# 86. Read-only/degraded state

Archived/suspended or unauthorized Workspace should clearly distinguish:

- no membership;
- read-only;
- archived;
- plan-limited;
- temporarily unavailable.

# 87. Service extraction

Workspaces is a semantic extraction seam.

Extraction requires stable membership/authz contracts, tenant propagation, no direct foreign writes, and clear Account/Governance/Billing relationships.

# 88. Current source alignment

Current Domain Workspaces contains:

```text
Workspaces
Members
Invitations
Spaces
Teams
Rules
```

Current source includes `Workspace`, `WorkspaceSettings`, `WorkspaceStatus`, `WorkspaceMember`, `WorkspaceRole`, `WorkspaceInvitation`, `Space`, `Team`, and related status/role concepts.

Space hierarchy, ordering, membership (`SpaceMember`, `SpaceRole`, `SpaceMemberSubjectType`) and the `workspace.space_paths` closure index implement §41–§49 (decision record: `backend/docs/decisions/ADR-009-space-scoped-authorization.md`).

This supports a richer Workspace organization context than membership alone.

# 89. Source ambiguity watch

Do not normalize:

- WorkspaceRole into universal permission;
- SpaceVisibility or SpaceRole into complete authorization (Governance composes them, GOV-025);
- TeamRole into WorkspaceRole;
- current Workspace selection into durable Identity state.

Those are different semantic layers.

# 90. Change impact — membership

Review:

```text
Identity
Accounts
Governance
all Workspace-scoped contexts
frontend query/realtime scope
RLS
audit
```

# 91. Change impact — Workspace lifecycle

Review:

```text
Work Management
Documents
Collaboration
Automation
Integrations
Billing
Analytics
retention/export
frontend shell
```

# 92. Change impact — Space/Team

Review resource organization, Governance permissions, cross-Workspace guards, frontend navigation, search, and migration.

# 93. Testing/evidence

Critical tests should cover:

```text
Workspace creation/lifecycle
membership add/remove/suspend/role
last-owner guard
invitation accept/revoke/expiry/retry
cross-Workspace rejection
Space cycles/parentage, sibling order and name uniqueness
Space archive/delete blocked by active children/resources
Space membership (User/Team), last-Space-Admin guard
Team membership compatibility
tenant propagation
RLS/authz integration
Workspace switch stale-response/realtime behavior
deletion/retention workflows
```

# 94. Stop conditions

Stop rather than guess if:

- authenticated User is treated as Workspace Member automatically;
- Account Member is assumed to belong to every Workspace;
- invitation is treated as membership;
- Workspace role is treated as universal resource authorization;
- Team/Space hierarchy bypasses Governance;
- cross-Workspace references are accepted by raw ID;
- switching Workspace only changes one frontend variable;
- Workspace deletion is implemented only by cascade;
- Account-wide SSO/SCIM/region is being moved into Workspace settings for convenience;
- source conflicts with ownership and no drift classification exists.

# 95. Related canonical owners

```text
docs/product/contexts/accounts.md
docs/product/contexts/identity.md
docs/product/contexts/governance.md
docs/product/contexts/billing.md
docs/product/contexts/work-management.md
docs/product/contexts/documents.md
docs/product/product-model.md
docs/architecture/bounded-context-map.md
docs/architecture/data-ownership-and-consistency.md
backend/docs/architecture/security-tenancy-authorization.md
frontend/docs/architecture/state-query-mutations.md
frontend/docs/architecture/realtime.md
```

# 96. Final Workspaces rule

For every Workspace capability, answer:

```text
Which Workspace is the tenant?
Which Identity participates?
Is there an active membership?
What Workspace role exists?
What Governance permission still applies?
What Account relationship exists?
What Billing entitlement applies?
What child resources are only scoped versus actually owned?
What happens on switch, suspension, archive, or delete?
How is scope preserved through backend/realtime/frontend?
```

The target is:

> **a stable collaboration tenant and membership boundary that scopes product work strongly without becoming the owner of every Workspace-scoped resource or every authorization rule.**

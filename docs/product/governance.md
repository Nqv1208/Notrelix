---
document_id: PROD-GOVERNANCE
document_type: product-context
status: active
owner: governance
applies_to:
  - governance
  - authorization
  - permissions
  - policies
  - roles
  - sharing
  - share-links
  - security-audit
evidence:
  - PRODUCT.md
  - docs/product/product-model.md
  - docs/product/contexts/accounts.md
  - docs/product/contexts/identity.md
  - docs/product/contexts/workspaces.md
  - docs/architecture/bounded-context-map.md
  - docs/architecture/contract-boundaries.md
  - docs/architecture/data-ownership-and-consistency.md
  - backend/src/Notrelix.Domain/Governance/
  - backend/docs/decisions/ADR-009-space-scoped-authorization.md
  - backend/src/Notrelix.Application/
  - backend/tests/
  - frontend/packages/features/governance/
review_on:
  - authorization-model-change
  - permission-vocabulary-change
  - role-or-policy-change
  - share-link-change
  - guest-access-change
  - permission-cache-change
  - audit-semantics-change
  - new-protected-resource-kind
  - account-or-workspace-role-change
---

# Governance Context

> **Governance owns the product semantics for deciding who may do what to which protected resource, under which scope and policy, and for preserving governed security/administrative evidence.**
>
> It does not own authentication, membership lifecycle, commercial entitlement, or the protected business resources themselves.

This document is the canonical product owner for Governance semantics.

Backend security architecture owns exact authorization pipeline/runtime mechanics.

Frontend owns authorization-aware presentation.

Product contexts remain owners of their protected business state.

---

# 1. Mission

Governance gives Notrelix a coherent authorization language across product contexts.

It exists so that protected operations do not devolve into:

```text
if user.Role == "Admin"
```

scattered across:

- handlers;
- endpoints;
- UI components;
- queries;
- background consumers.

Governance turns explicit principal, membership, policy, entitlement, scope, and resource facts into a fail-closed access decision.

---

# 2. Owns

Governance owns product semantics for:

```text
permission action vocabulary
protected resource vocabulary
permission subject vocabulary
permission rules
resource permissions
policy semantics
custom roles where supported
role-permission composition
sharing policy
guest access policy
share-link lifecycle
permission templates where product-approved
authorization decision meaning
governed security/administrative audit meaning
```

Current Domain source contains `Permissions`, `Policies`, `Roles`, `ShareLinks`, and `Templates`, which is strong implementation evidence for these capabilities.

---

# 3. Does not own

```text
credentials, sessions, MFA, OAuth login
→ Identity

Account membership/enterprise administration
→ Accounts

Workspace membership/invitations/Teams/Spaces
→ Workspaces

Board/Item/Field/View state
→ Work Management

Page/Block state
→ Documents

subscription/entitlement/payment
→ Billing

user-facing comments/activity/notifications
→ Collaboration
```

Governance may consume facts from those owners.

It does not absorb their lifecycle.

---

# 4. Governance inputs

A material authorization decision may depend on:

```text
principal
credential/session security
Account scope
Workspace scope
membership
Account/Workspace role
Team/group relation
resource identity
resource lifecycle
resource ownership
permission policy
share-link capability
commercial entitlement
context-specific business constraints
```

Not every decision uses every input.

The inputs remain owned by their source contexts.

---

# 5. GOV-001 — Backend authorization is final

Every material operation over protected data is authorized server-side.

This includes:

```text
command
query
list
search
export
realtime subscription
background action on behalf of a principal
```

Frontend permission guards are UX only.

---

# 6. Queries are protected operations

A query can leak:

- resource existence;
- names;
- membership;
- private configuration;
- sensitive metadata;
- exportable data.

Therefore “read only” is not an authorization exemption.

---

# 7. GOV-002 — Authorization happens before protected data leaks

Do not:

```text
load all rows
→ filter unauthorized objects in memory
```

when the query path can expose or materialize protected data.

Permission/resource scope must be enforced at an appropriate server data-access/application boundary.

---

# 8. Authentication versus authorization

Identity establishes the principal.

Governance decides protected access.

A valid session does not imply access to an arbitrary Account, Workspace, Board, Page, or administrative operation.

---

# 9. Membership versus authorization

Workspaces owns Workspace membership.

Accounts owns Account membership.

Membership can be an authorization input.

Membership is not the entire permission system.

---

# 10. Entitlement versus authorization

Billing answers whether a commercial capability/limit is available.

Governance answers whether a principal may perform an action on a protected resource.

Both may be required.

They remain separate decisions/facts.

---

# 11. GOV-003 — Commercial entitlement does not directly grant resource permission

A paid plan does not automatically mean:

```text
this User may edit this Board
```

Likewise, resource permission cannot create Billing entitlement.

---

# 12. Resource vocabulary

Each protected context should expose stable resource kinds/identities.

Examples:

```text
account
workspace
board
board-item
board-view
page
document
automation
integration-connection
billing-admin surface
```

Exact vocabulary must follow product semantics.

---

# 13. GOV-004 — Protected resource identity is logical and stable

Resource authorization must not rely only on:

- endpoint path;
- UI route;
- database table name;
- CLR class name.

A protected resource kind should survive internal implementation refactors.

---

# 14. Action vocabulary

Actions represent product operations such as:

```text
view
list
create
edit
delete
share
manage-members
manage-permissions
export
configure-security
administer
```

A context may require more specific actions.

Action names should represent product meaning, not HTTP verbs alone.

---

# 15. GOV-005 — Authorization action reflects business operation

`POST`, `PUT`, `DELETE`, or handler class name is not sufficient product authorization vocabulary.

Two mutations may have different risk despite using the same HTTP method.

---

# 16. Subject vocabulary

Authorization subjects may include:

```text
User
Account Member
Workspace Member
Team/group
custom role assignment
share-link/capability
service/system principal
```

Subject identity and subject type must be explicit.

Evaluated subjects today (2026-10-11): `User`, and `Team` (GOV-026). Other stored subject types (`WorkspaceRole`, `PublicLink`, `ExternalEmail`) are not evaluated by the access engine and grant nothing until a product decision defines them.

Certified core of a `ResourcePermission` entry (2026-10-23):

```text
subject    User or Team only
effect     Allow only (stored priority 100, no condition)
expiry     none
```

The `ResourcePermission` Domain factory refuses any other subject type, and the grant command rejects `WorkspaceRole`, `PublicLink`, `ExternalEmail`, an expiry, and any Deny, condition or priority input (the command has no field for them). Deny, condition, priority and expiry semantics belong to `PermissionRule` (time window and priority are evaluated there; a rule's condition JSON is not evaluated today), not to a resource entry; a product decision is required before a resource entry may carry them.

Rows written before this bound that hold another subject type stay in place and fail closed: the access-facts query matches only `User` and `Team` subjects, so those rows grant nothing. No destructive migration is applied; they remain visible to revoke.

---

# 17. Current Permission source

Current Governance source includes concepts such as:

```text
PermissionAction
PermissionEffect
PermissionLevel
PermissionRule
PermissionScope
PermissionSubjectType
ResourcePermission
FieldPermission
```

This indicates a richer permission model than simple role strings.

Exact current types remain implementation evidence.

---

# 18. Permission rule

A permission rule represents a governed relationship between:

- subject;
- action/capability;
- resource/scope;
- effect/level;
- lifecycle/status

where product-approved.

Rules do not automatically replace inherited role/membership/policy semantics.

---

# 19. GOV-006 — One ACL entry is not the whole authorization system

An explicit resource permission cannot by itself represent every source of authority such as:

```text
Workspace membership
Account administration
inherited role
guest policy
share link
entitlement
resource-specific invariant
```

The final decision composes relevant facts intentionally.

---

# 20. Allow and deny

If Governance supports explicit allow/deny semantics, conflict precedence must be deterministic and documented.

Do not rely on iteration order or whichever rule is loaded last.

---

# 21. Permission level

Permission levels may simplify common capability groups.

They must map deterministically to concrete actions.

A level label must not become an undocumented wildcard.

---

# 22. Permission scope

Permission scope can identify where a rule applies:

```text
Account
Workspace
Space (Workspace organizational container, GOV-025)
specific resource
field or subresource where explicitly supported
```

Scope inheritance must be explicit.

---

# 23. GOV-007 — Scope inheritance is explicit

Do not assume:

```text
Account access
→ all Workspace access
→ all resource access
```

unless an approved policy says so.

Similarly, resource sharing does not make linked resources transitively public.

The only approved inheritance chains are:

```text
Space membership → descendant Spaces (WSP-024; Private descendants stop Member/Viewer inheritance)
Space access     → Workspace-visible Boards/Pages placed in that Space (GOV-025)
Team membership  → the Team's grants (GOV-026)
```

Account membership still never implies Workspace access, and Workspace membership alone never reaches a Private Space.

---

# 23.1. GOV-025 — Space-scoped access composes in a fixed order

For a Board or Page in Workspace `W` (placed in Space `S` or unplaced), the decision is evaluated in this order; the first matching step decides:

```text
1. resource missing / archived / deleted / other Workspace → NotFound
2. Workspace Owner                                        → Allow
3. explicit permission rules for the action (User/Team subject),
   lowest priority value first, Deny wins at equal priority → Deny / Allow
4. resource restriction: a Private Board/Page requires board membership
   or an explicit resource permission (User/Team); Space roles never reach
   a Private resource                                      → NotFound if absent
5. Space-derived level (resource placed in S, Workspace-visible)
6. Workspace default (resource unplaced, Workspace-visible): Members allowed,
   Guests need explicit access
```

A Workspace-visible resource inherits the access of its Space and may be restricted further by making it Private and granting outsiders explicitly. A resource placed in a Space is hidden (NotFound) from a principal who has no Space access, no board membership and no explicit grant.

Effective Space access of a principal on Space `S`:

```text
workspace Owner/Admin         → Space Admin on every Space
explicit entries              → the highest role of a User entry or an active Team entry
                                on S or an ancestor, where a Private Space between the
                                ancestor (exclusive) and S (inclusive) stops Member/Viewer
                                (Admin always passes)
open Space (no Private Space  → workspace Members (not Guests) hold Member level
from the root down to S)
```

# 23.2. GOV-026 — Team is an evaluated subject

A principal holds the grants addressed to a Team (Space membership, resource permission, permission rule) only while it is an active member of that Team and the Team is active and belongs to the same Workspace. Removing the member, archiving or deleting the Team revokes the derived access on the next evaluation; no copy of the grant is made per user.

# 23.3. GOV-027 — Space roles map to permission levels deterministically

```text
Space Admin  → Manager
Space Member → Editor
Space Viewer → Viewer
```

Every source of access to a Board or Page gives a level (Space role as above; explicit grant; board membership role, GOV-033; the Workspace default of an unplaced Workspace-visible Board/Page = Editor for non-Guest members). The effective level is the highest of them (a Guest is capped at Editor, GOV-030), and the action thresholds apply to that effective level whatever the source:

```text
Viewer    : ViewBoard, ViewPage, ViewWorkspace, ViewMembers
Commenter : + CreateComment
Editor    : + content: create, update, move, assign and delete Items; Fields, Views,
            Groups, Labels, saved filters, forms, templates and relations;
            every other Page action
Manager   : + Board management (ManageBoard on a Board: settings and title, visibility,
            move to a Space, archive/unarchive, delete/restore, board members),
            ManageBoardPermission, ManagePagePermission, ArchivePage
```

Board content commands declare the content action they perform (groups: CreateItem/UpdateItem; labels, forms, templates, relations: CreateField/UpdateField/DeleteField; default view and saved filters: UpdateBoardView/CreateBoardView); `ManageBoard` on a Board is Board management only. Board-member semantics still apply on top of the level (for example an Observer cannot update items).

# 23.4. GOV-028 — Space and Team management authority

```text
ManageSpaces on the Workspace (create a root Space)  → workspace Owner, Admin
ManageSpaces on a Space (rename, describe, change type/visibility,
  archive/unarchive/delete, manage members,
  create a child Space)                               → workspace Owner, Admin, or effective Space Admin
restore a deleted Space                               → workspace Owner, Admin (a deleted Space has no
                                                        evaluable Space access)
move a Space                                          → ManageSpaces on the Space and on the target parent
                                                        (or on the Workspace for the root)
ManageTeams (create/rename/archive/delete Teams, manage Team members) → workspace Owner, Admin
ManageMemberProfile (edit another member's Workspace job title; D15)  → workspace Owner, Admin
                                                                        (a member edits their own title as a plain member)
create a Board/Page in a Space (CreateBoard/CreatePage on the Space)  → Editor level on the Space
move a Board/Page to another Space or out of a Space                  → ManageBoard on the resource
                                                                        and the create right on the target
```

A principal who cannot see a Space receives NotFound for it, not Forbidden.

Scope of Space inheritance today: the Space level is evaluated where the decision is made on the Board or Page itself (and on the Space). Board-child resources (items, groups, fields, views, …) are decided on their Board (GOV-033). Workspace Admin capability over Spaces and Teams does not extend to Owner-only actions (GOV-013) nor to Private Boards/Pages (GOV-025 step 4).

# 23.5. GOV-029 — Collections apply the single-read decision in the datastore

Board lists (by Workspace and by slug), Space lists/tree, Page lists/tree/search and My Work return exactly the resources a single read of each would allow (`ViewBoard`/`ViewPage`/Space visibility), computed in the server query (GOV-019), never by loading everything and hiding it in memory. A parity test pins the list decision to the single-read engine.


# 23.6. GOV-033 — Board child resources are authorized through their Board

Decided 2026-10-13. Every resource that lives inside a Board — Items (and their comments, activity, attachments, links and checklists), Checklist items, Groups, Fields, Views and Labels — is authorized by evaluating the requested action against its parent Board:

- the same visibility: a principal who cannot see the Board receives NotFound for any of its children (GOV-025), and a child of an archived or deleted Board, or one whose Board cannot be resolved, is NotFound;
- the same order as the Board itself: explicit permission rules scoped to the Board (or to the child) apply to its children, then the level and action thresholds (GOV-027) and the Guest cap (GOV-030): reading needs Viewer, commenting on an Item needs Commenter, every other change needs Editor;
- an explicit grant on the child itself still counts as a source of access in addition to the Board's sources;
- a child accepts only Board-child actions (read, comment, item, field and view changes, Board management); any other action on a child is refused.

Board membership roles carry a level: Owner and Admin → Manager, Member → Editor, Observer and Guest → Viewer. Managing board members (adding, removing, changing a role) is Board management and needs Manager; a Manager assigns, changes or removes roles up to their own level (the target's level is the higher of its board role and any explicit grant). Below Manager no one manages board members, a Guest never does (GOV-030). Saving the Board's shared view is an Editor change.

Saved filters are Board children too (they resolve to their Board): a personal filter is its creator's alone, so another principal gets NotFound for it; a shared filter is visible to everyone who can view the Board and is changed by whoever holds the view-edit right (UpdateBoardView), while only its creator changes who sees it.

Forms and form submissions are Board children too (they resolve to their Board): managing a form (questions, publish, close, delete, restore) and handling its submissions (process, reject, mark spam, delete) is Board management and needs Manager; creating a form is an Editor schema change; submitting to a published form needs the right to create Items on its Board, so a principal without Board access gets NotFound. Public (anonymous) submission is not wired (owner decision pending).

Not yet wired (their requests are unreachable today): Board relations.
---

# 24. Field-level permissions

Current source contains `FieldPermission`.

Field-level authorization is more granular than ordinary Board/Item permission.

If used, it must define:

- applicable field types;
- read versus edit semantics;
- interaction with hidden/visible UI fields;
- export/search behavior;
- Automation/Integrations behavior.

---

# 25. GOV-008 — Hidden UI field is not a security boundary

If a field is security-restricted, backend query/mutation/export/realtime paths must enforce that policy.

Frontend hiding alone is insufficient.

---

# 26. Policies

Current source contains:

```text
ResourcePolicy
SharingPolicy
GuestAccessPolicy
WorkspacePolicy
```

Policies represent reusable product-level authorization rules around context facts.

They should remain explicit rather than dispersed conditionals.

---

# 27. Resource policy

A Resource Policy defines how a protected resource participates in authorization.

It may use:

- ownership;
- visibility;
- role;
- explicit permission;
- lifecycle;
- sharing.

The protected context still owns the resource state.

---

# 28. Workspace policy

Workspace policy may translate membership/Workspace role into baseline capability.

Workspaces owns membership/role facts.

Governance owns the permission meaning assigned to them.

---

# 29. Guest access policy

Guest access is constrained by default.

A guest/external principal may have access to explicitly shared resources without receiving workspace-wide enumeration.

---

# 30. GOV-009 — Guest access is resource-limited by default

Guest or external access MUST NOT imply:

```text
list every Board
list every Page
enumerate every Workspace member
discover every private resource
```

unless an explicit policy intentionally grants such capability.

---

# 30.1. GOV-030 — A Guest's resource level is capped at Viewer

Decided 2026-10-12 (UI mockup review) with a cap of Editor; tightened 2026-10-31 (owner: security first, a person outside the company reads what was shared with them). A Guest (Workspace role `guest`, WSP-026) can hold on a Board or Page:

```text
Viewer                       → effective
Commenter | Editor | Manager → never effective: the level is capped at Viewer when evaluated
```

- the cap applies whatever the source of the level (direct entry, Team grant, board membership role, Space role): the effective level of a Guest is the lower of the computed level and Viewer;
- a Guest therefore never edits, comments on or manages a Board or Page (settings, visibility, moving, archiving, deleting, board members — also on an unplaced Workspace-visible Board), never holds `ManageBoardPermission` / `ManagePagePermission`, cannot change who has access, and cannot share the resource further;
- a resource invitation (WSP-026) is created at Viewer only; a higher level is rejected when the invitation is created (`Workspaces_Invitation_ResourceLevelAboveViewer`). A stored entry or an invitation that predates the cap is not rewritten: it grants Viewer when evaluated, and accepting an older invitation writes a Viewer entry;
- comment for outside people may be enabled later through a catalog flag; until then it is not part of the guest level;
- resource access UIs show every option above Viewer disabled for a Guest with the reason, instead of hiding it.

---

# 30.2. GOV-031 — An explicit resource entry may expire

Decided 2026-10-12 (UI mockup review). A board membership or explicit resource permission may carry an expiry instant.

- expiry is evaluated server-side at decision time, like share-link expiry (§39); from that instant the entry grants nothing, and a principal left without any other source of access receives the same NotFound as any principal without access (GOV-025);
- collections apply the same check in the datastore query (GOV-029), so an expired entry never makes a resource appear in a list;
- setting, changing or clearing an expiry is a `ManageBoardPermission` / `ManagePagePermission` mutation and is audited;
- an expired entry stays visible to the resource's managers, marked expired, until it is removed or renewed; renewing is an explicit change, never automatic;
- an expiry only ends that entry; other sources (Space role, Team grant) are evaluated independently.

The UI proposes an expiry when the subject is a Guest and allows one on any entry. Workspace Owner/Admin may set a default guest expiry in the Workspace sharing policy (GOV-032); it only pre-fills new guest entries and never changes existing ones.

---

# 31. Sharing policy

Sharing determines how an owned resource can expose access beyond default membership/policy.

Sharing does not transfer resource ownership to Governance.

---

# 32. Share link

A Share Link represents an explicit access capability associated with a resource/scope.

Current source includes:

```text
ShareLink
ShareLinkAccessMode
ShareLinkStatus
ShareLinkTokenHash
```

This is evidence for a first-class share-link lifecycle.

---

# 33. GOV-010 — Share link is a scoped capability, not global public mode

A Share Link grants only the approved capability to the approved target resource/scope.

It does not make:

- the entire Workspace public;
- linked resources public;
- embedded resources transitively accessible.

---

# 34. Share-link lifecycle

Product semantics should distinguish as applicable:

```text
created
active
expired
revoked
disabled
```

A revoked/expired link must stop authorizing future access.

---

# 35. Share-link secret

A public/share token is security-sensitive.

Persist only a safe verifier/hash where possible.

Do not expose raw reusable token in:

- logs;
- audit payload;
- events;
- generated client state beyond the creation/URL flow that requires it.

---

# 36. GOV-011 — Share-link revocation is authoritative

Revocation must invalidate access even if:

- the client retains an old URL;
- a realtime connection exists;
- a permission decision was cached.

Cache/session/realtime behavior must converge to revoked authority.

---

# 37. Share-link access mode

If a link supports modes such as view/comment/edit, the mode must map to explicit action semantics.

Do not implement “edit link” as a boolean that bypasses every resource-specific rule.

---

# 38. Password-protected links

If supported, link password/proof is a capability guard.

It does not authenticate the visitor as a full Identity unless a separate login occurs.

---

# 39. Share-link expiry

Expiry is evaluated authoritatively server-side.

Client clocks/UI labels do not decide validity.

---

# 39.1. GOV-032 — The Workspace sharing policy can turn share links off

Decided 2026-10-12 (UI mockup review). Workspace Owner/Admin set the Workspace sharing policy `shareLinks = Enabled | Disabled`.

- while Disabled, no share link of any resource in the Workspace authorizes access, and creating or activating one is rejected; existing links are kept, not deleted, so re-enabling the policy lets links that are neither revoked nor expired work again;
- the policy is evaluated server-side at access time; turning it off converges like a revocation (GOV-011, GOV-018);
- resource sharing UIs show the policy state in place of the share-link control, naming the workspace admins as the people who can change it;
- the resource address copied from the app ("copy link") is a locator, not a share link: it opens the resource only for principals who already have access (GOV-010).

---

# 40. Linked-resource access

Example:

```text
public Page embeds private Board
```

The Page link does not automatically grant Board permission.

Target authorization is evaluated independently unless an approved compound-sharing rule exists.

---

# 41. Roles

Current Governance source includes:

```text
CustomRole
CustomRolePermission
MemberRoleAssignment
```

This supports governed reusable custom-role semantics beyond hard-coded Workspace role labels.

---

# 42. GOV-012 — Role is a permission composition, not an identity

A custom role groups or names permission semantics.

It does not become:

- User identity;
- Workspace membership itself;
- Account membership itself.

Role assignment attaches the role to a valid subject/context.

---

# 43. Built-in versus custom roles

Built-in roles may originate from Account/Workspace product semantics.

Custom roles may extend authorization vocabulary.

The system must define how built-in and custom roles interact.

---

# 44. Owner, Admin, Member, Guest

These labels are not interchangeable.

Typical product distinction:

```text
Owner
    final lifecycle/security responsibility

Admin
    broad operational administration

Member
    normal collaboration capability

Guest
    constrained/external access
```

High-risk owner semantics remain fail-closed.

## 44.1. Workspace role baseline (final matrix, 2026-10-23)

The default decision of a workspace role for every `PermissionAction`, before explicit permission rules, resource permissions and Space evaluation compose on top of it (GOV-025 order, unchanged). Source of truth in code: the typed table `WorkspaceRoleBaseline` (Application Governance); an exhaustive test classifies every action, so a new action fails until it is classified. Account-scoped actions (`CreateWorkspace`, `ManageAccount`) are decided on the Account role (`AccountRoleBaseline`): Owner and Admin allowed, Member denied; `ViewWorkspace` is open to every Account member.

`A` = allow, `D` = deny, `L` = decided on the resource level (Board, Page or Space: Owner → Owner level, Admin and Member on an unplaced Workspace-visible resource → Editor, Admin on a resource placed in a Space → Manager, Guest → explicit access only, capped at Editor; GOV-025/027/030).

| Actions | Owner | Admin | Member | Guest | Note |
|---|---|---|---|---|---|
| `ViewWorkspace` | A | A | A | A | Every active member reaches the shell. |
| `ViewMembers` | A | A | A | D | Changed: a Guest does not enumerate the workspace (GOV-009). A Guest reads only their own row through `GET /workspaces/{workspaceId}/members/me` (`ViewWorkspace`), and the names of people on a shared resource through that resource's own reads (see 44.3). |
| `InviteMember` (add member, invitations) | A | A | D | D | Changed from Owner-only. Role cap in 44.2. |
| `RemoveMember` (remove, suspend, activate) | A | A | D | D | Changed from Owner-only. Actor-versus-target guard in 44.2. |
| `ManageWorkspaceSettings` | A | A | D | D | Changed from Owner-only. Targets Automation rules, Analytics placement rebuild and the calendar-sync trigger; it never covers credentials or security configuration (see `ManageApiTokens`). |
| `ManageApiTokens` (create, list, revoke API tokens) | A | D | D | D | Credential issuance is security configuration and stays Owner (GOV-013). Split out of `ManageWorkspaceSettings` so Admin cannot mint a credential or read or revoke the Owner's tokens. |
| `ChangeMemberRole` | A | D | D | D | Minting Admin or Owner stays Owner. |
| `ManageWorkspace`, `ArchiveWorkspace`, `RestoreWorkspace`, `DeleteWorkspace` | A | D | D | D | Lifecycle and security stay Owner (GOV-013). |
| `ManageSpaces`, `ManageTeams`, `ManageMemberProfile`, `ManageIntegrations` | A | A | D | D | GOV-028, D15, M8. In a Space the Space level decides. |
| `CreateBoard` at the workspace root | A | A | A | D | Changed: Admin and Member may create unplaced Boards (usage right, Guests excluded, same as `CreatePage`). In a Space the Space level decides (Editor). |
| `CreatePage` | A | A | A | D | Usage right. |
| Board actions (`ViewBoard`, `ManageBoard`, `ManageBoardPermission`, board view, field and item actions, `ShareBoardView`) | A | L | L | L | GOV-027 thresholds. Public share needs Editor (unchanged). |
| Page actions (`ViewPage`, `UpdatePage`, `DeletePage`, `ArchivePage`, `SharePage`, `ManagePagePermission`) | A | L | L | L | GOV-027. `SharePage` needs Editor (unchanged). |
| `CreateComment` | A | L | L | L | Decided on the target resource; a Guest needs explicit access on it. |

An Allow cell applies to the workspace resource itself (and, for `ManageWorkspaceSettings`, to the Automation, Analytics and Integrations resources it targets, and for `ManageIntegrations` also the calendar integration); it never widens to another resource kind.

## 44.2. Guards the changed rows depend on

Without these the changed rows would be an escalation. Both are Domain rules fed explicit facts (the actor's active workspace role, the target's role) by Application; they run before any state changes and use stable rule codes.

- **Role cap** (`Workspaces_Member_ActorCannotAssignRole`): adding a member directly assigns Member or Guest only unless the actor is an Owner. Invitations already offer Member or Guest only (`Workspaces_Invitation_CannotInviteAsAdmin`). Re-adding a returning member whose existing row is an Owner or Admin is refused for a non-Owner (`Workspaces_Member_ActorCannotActOnMember`).
- **Resend** (`Workspaces_Member_ActorCannotAssignRole`): resending an invitation re-issues the token that grants the invited role, so it applies the same role cap to the invitation's role. A non-Owner cannot re-arm a legacy Admin invitation (created before Admin stopped being invitable); an Admin still resends Member and Guest invitations. Cancelling and changing an invitation role cannot create an Admin or Owner membership (the Domain refuses an Admin or Owner role on create and on change), so they need no extra guard.
- **Actor-versus-target** (`Workspaces_Member_ActorCannotActOnMember`): remove, suspend and activate act on any target only for an Owner; an Admin acts only on Member and Guest targets, never on an Owner or another Admin. The last-owner invariant and the owner row lock are unchanged.

## 44.3. ViewMembers for a Guest (closed 2026-10-23)

GOV-009 says a Guest does not enumerate the workspace, so `ViewMembers` is denied for a Guest: `GET /workspaces/{workspaceId}/members` answers 403. Two guest-safe reads replace the list:

- **Own membership.** `GET /workspaces/{workspaceId}/members/me` (`Workspaces.Members.GetMyMembership`, `ViewWorkspace`) returns the caller's own `WorkspaceMemberDto` (role, status, job title, version) to any active member, a Guest included. It never returns another person. The web shell derives the viewer's role from it and requests the member list only for a role that may list people.
- **Names on a shared resource.** Reads that show people on a resource (full board, board item, comments, board members, document page people) resolve names through the actor lookup and are authorized on the resource (a `ViewMembers` request against a Board is decided by the Board level, GOV-027, not by the workspace baseline cell), so they do not need the workspace member list. A Guest with access to a Board still sees assignee and comment author names.

Members page for a Guest: shows only the Guest's own row, read-only.

Board sharing read (CTR-GAP-WM-BOARD-SHARING). Governance publishes the Board's sharing view (`IBoardSharingSummary`) and Work Management serves it as `GET /boards/{boardId}/sharing`. Entries, names and levels are shown only to a caller who may manage the Board's permissions or is a Board member; every other caller who can read the Board gets the counts only; a caller who cannot read it gets the same NotFound as a missing Board (GOV-025). One entry per person (id = user id) at the higher of the explicit grant and the Board-member level, and one per granted Team (id = team id). A Guest is identified by the Workspace role, never by email domain, is shown at most at Editor with `levelCapped` (GOV-030 stored and capped, no 422), and has no email. The log, the Private impact, the explicit grant id (`permissionId`) and who granted (`grantedBy`) are for managers only: a Board member who cannot manage sees the entries (person or Team, level, source) without them. Space-derived data (`spaceId`, `spaceName`, `spacePeople`, each entry's `spaceRole` and `outsideSpace`) is filled only when the caller can see the Space; otherwise it is null or false, so a hidden Space's roles never leak through a Board. Governance records every Board access change (explicit grant added, changed, removed; Board member added, role changed, removed; visibility changed or made Private) in `governance.resource_access_changes` inside the request transaction, retained 90 days; it is a workspace-scoped read source, separate from the admin-only enterprise audit log, which also receives the Board member and visibility actions. There is no `expired` kind: access given on a Board has no end date.

The Space members extras, Teams extras and security extras reads (docs/product/workspaces.md section 48.2) are `ViewMembers` reads too: a Guest receives 403, and Team usage and Space people list only Spaces the caller can see (GOV-029). `guestDefaultExpiryDays` in the sharing policy only pre-fills new Guest entries (GOV-031).

---

# 45. GOV-013 — Admin does not silently become Owner

Actions such as:

- deleting/closing critical scope;
- transferring ownership;
- security configuration;
- final-owner removal

must use explicit policy.

Broad Admin capability must not silently include every Owner-only action.

Approved Admin defaults (2026-10-11): manage every Space and Team in the Workspace (GOV-028). They do not include Private Boards/Pages, Workspace deletion or ownership transfer.

---

# 46. Role assignment

Role assignment requires:

- valid subject;
- valid role;
- compatible Account/Workspace/resource scope;
- assignment lifecycle;
- authorization to assign.

Cross-scope role assignment through raw IDs is forbidden.

---

# 47. Role deletion

Deleting/deactivating a custom role must define what happens to assignments.

Do not leave ambiguous orphaned authorization state.

---

# 48. Permission template

Current Governance source includes `PermissionTemplate` and related definition/entry/scope/status types.

A Permission Template can seed repeatable permission configuration.

---

# 49. GOV-014 — Permission template is creation/configuration input, not hidden live authority

Unless product explicitly defines linked templates, applying a template creates ordinary permission/policy state.

Later editing the template must not silently rewrite all previously configured resources.

---

# 50. Template versioning

If templates evolve, product semantics should distinguish:

- template definition;
- applied permission state;
- migration/re-application.

Do not make runtime authorization depend on a mutable template by accident.

---

# 51. Permission decision

A decision should be semantically one of:

```text
allow
deny
```

with safe reason/context metadata where needed.

Do not expose internal rule stack to unauthorized clients.

---

# 52. GOV-015 — Authorization fails closed

When required facts are:

- missing;
- stale beyond safe use;
- ambiguous;
- unsupported;
- unavailable with no safe fallback,

protected operations must not default to allow.

---

# 53. Policy evaluation

Policy evaluation may combine:

```text
Identity principal
Account facts
Workspace membership/role
Team/group
resource owner/lifecycle
explicit permission
share capability
Billing entitlement
context business prerequisite
```

The order/precedence is governed and testable.

---

# 54. Business invariant versus authorization

Authorization answers whether the caller may attempt the operation.

The owning product context still validates:

- lifecycle;
- domain invariant;
- concurrency;
- semantic preconditions.

Allow does not guarantee business success.

---

# 55. GOV-016 — Governance never mutates protected aggregate as authorization side effect

Authorization should return a decision/context.

It must not:

```text
authorize edit
→ also mutate Board
```

Governance remains separate from target business ownership.

---

# 56. Permission cache

Permission decisions can be cached only with safe identity and invalidation semantics.

Relevant dimensions may include:

```text
Account
Workspace
principal/subject
resource
action
policy/permission version
membership version
entitlement version
```

depending on the decision.

---

# 57. GOV-017 — Permission-sensitive cache is scope/version aware

A cache entry such as:

```text
user123 -> canEdit = true
```

is insufficient when access varies by Workspace/resource/policy version.

Placeholder versions such as `unknown` must not make protected cache fail-open.

---

# 58. Cache invalidation

Changes that can invalidate decisions include:

- member removed/suspended;
- role change;
- policy change;
- permission grant/revoke;
- share-link revoke;
- entitlement change;
- resource lifecycle/ownership change.

The invalidation model must match actual authority.

---

# 59. Realtime authorization

Realtime subscription itself is a protected operation.

Connection establishment does not permanently freeze access for the connection lifetime.

---

# 60. GOV-018 — Realtime access can be revoked while connected

If membership/share/policy changes remove access:

- future events stop;
- subscription/session state converges;
- client should refetch/leave protected state as needed.

Do not keep sending private events until reconnect.

---

# 61. Event payload security

Permission/governance events should not reveal sensitive ACL details to unauthorized consumers.

Event routing and payload must preserve tenant/resource security.

---

# 62. Search/list/export

Authorization must apply to:

```text
single resource
collection
search result
export
analytics/drill-down
```

A bulk endpoint is not exempt.

---

# 63. GOV-019 — Collection authorization is not fetch-all-then-hide

Query design should filter protected resource access in the server query/projection path.

Do not materialize unauthorized records before filtering when this can leak or scale badly.

For Spaces, Boards, Pages and My Work the collection decision is GOV-029.

---

# 64. Export

Export often exposes more data than ordinary screen views.

It may require:

- stronger action permission;
- field-level permission;
- audit;
- rate/size policy.

---

# 65. Audit

Governance owns the product meaning of security/administrative audit.

Audit is append-oriented evidence, not ordinary editable content.

---

# 66. Audit versus activity

```text
Audit
→ governed/security/administrative evidence

Activity
→ user-facing collaborative history
```

They may derive from the same product operation.

They are not interchangeable stores.

---

# 67. GOV-020 — Audit evidence is not mutable Activity

Audit must not be edited/deleted like ordinary comments/activity merely because both record actions.

Retention/privacy policy governs audit lifecycle.

---

# 68. Audit record semantics

A governed audit fact may need:

```text
actor
principal type
operation/action
target/resource
Account/Workspace scope
decision/outcome
time
correlation
safe metadata
```

Do not store reusable secrets.

---

# 69. Audit coverage

High-impact examples include:

- permission grant/revoke;
- role assignment;
- membership/owner change;
- share-link lifecycle;
- security policy change;
- Account/Workspace lifecycle;
- export/delete;
- audit-log access;
- enterprise identity/provisioning configuration.

The exact required set is policy-owned.

---

# 70. GOV-021 — Permission change is auditable when policy requires it

A successful grant/revoke/role/share change should not silently disappear from security history.

Audit recording must follow the committed result.

---

# 71. Denied-operation audit

Some denied attempts may require audit.

Not every ordinary authorization denial must necessarily become durable audit noise.

Policy defines the governed set.

---

# 72. Audit retention

Audit may outlive the source object or membership.

Retention must account for:

- security;
- compliance;
- privacy;
- legal requirements.

Generic soft-delete symmetry is inappropriate.

---

# 73. Governance deletion

Permission rules, role assignments, share links, and policies can be revoked/deactivated/deleted according to their own lifecycle.

Historical audit remains separate.

---

# 74. Governance and Accounts

Accounts owns:

- Account lifecycle;
- Account members/owner relations;
- organization-wide IdP/SCIM/domain/region administration.

Governance uses Account facts to decide protected Account operations.

---

# 75. Governance and Workspaces

Workspaces owns:

- Workspace lifecycle;
- membership;
- Workspace role;
- Team/Space composition.

Governance interprets those facts within authorization policy.

---

# 76. Governance and Identity

Identity proves the principal and credential/security state.

Governance consumes principal facts.

It does not manage password/MFA/session.

---

# 77. Governance and Billing

Billing supplies entitlement/usage/commercial facts.

Governance can include entitlement in a decision.

Billing does not directly grant a resource permission.

---

# 78. Governance and Work Management

Work Management declares protected resource/action semantics such as:

```text
Board view/edit/delete
Item create/edit/delete
Field schema manage
View manage/share
Form publish/respond
Approval decide
```

Governance evaluates access.

Work Management still validates its own invariants.

---

# 79. Governance and Documents

Documents supplies Page/Block/document resource semantics.

A shared Page does not automatically authorize embedded Work Management resources.

---

# 80. Governance and Collaboration

Comments/mentions/activity may require target-resource permission.

Collaboration does not independently reimplement target authorization from role strings.

---

# 81. Governance and Automation

Automation actions execute under an explicit principal/service authorization model.

Automation cannot bypass Governance merely because a rule was previously created by an authorized user.

---

# 82. Governance and Integrations

Integration callbacks/sync operate under explicit connection/service/resource authority.

Provider authentication is not automatically authorization to mutate every mapped resource.

---

# 83. Governance and Analytics

Analytics/reporting must preserve source authorization.

A report cannot expose inaccessible resources merely because data was projected earlier.

---

# 84. Policy versioning

A material policy change may invalidate:

- permission cache;
- active share/realtime access;
- role-derived decisions;
- generated access projections.

Version/invalidation semantics must be explicit.

---

# 85. Permission migration

Changing action/resource vocabulary may require migration of:

```text
stored rules
roles
templates
frontend guards
audit mappings
API resource/action mapping
tests
```

Treat it as contract/data change, not enum-only refactor.

---

# 86. Resource rename/move

Renaming a CLR/domain type does not necessarily rename the logical authorization resource kind.

Changing logical resource identity is a compatibility migration.

---

# 87. New protected resource admission

For a new product resource define:

```text
resource kind
owner context
scope
read/list actions
mutation actions
administration/share actions
default policy
guest behavior
audit-sensitive actions
frontend states
```

before scattering checks across implementation.

---

# 88. GOV-022 — Every new protected resource has explicit authorization vocabulary

A product feature is incomplete if it introduces a protected resource but no coherent resource/action policy.

---

# 89. Default policy

Defaults should be fail-closed enough to prevent accidental broad access.

Do not rely on “everything visible until permission system catches up”.

---

# 90. Migration from role-only authorization

If legacy code uses role strings directly, migrate:

```text
identify product action/resource
→ map current role behavior
→ encode canonical policy
→ add tests
→ remove scattered role checks
```

Do not preserve duplicate authority.

---

# 91. Frontend implications

Frontend may consume:

- permission result;
- capability set;
- read-only reason;
- entitlement distinction.

It should not reproduce server policy independently.

---

# 92. GOV-023 — Frontend capability state is derived

Client-side `canEdit`/`canDelete` state is a presentation/cache projection of server authority.

It must not become independently authored policy.

---

# 93. Permission uncertainty UX

While authorization is unresolved:

- avoid flashing protected content;
- avoid showing destructive controls as active;
- fail toward safe/read-only/loading presentation.

---

# 94. Share-link UX

Share UI should show:

- target;
- access mode;
- expiry;
- status;
- revocation;
- password requirement where supported.

Do not imply transitive access to embedded/linked resources.

---

# 95. Guest UX

Guest users should see only the scope/capabilities they have.

Do not expose Workspace navigation that implies unavailable enumeration.

---

# 96. Audit UX

Audit surfaces should communicate:

- actor;
- action;
- target;
- outcome;
- time/scope.

Do not allow ordinary inline editing of audit evidence.

---

# 97. Current source alignment

Current Governance Domain contains:

```text
Permissions
Policies
Roles
ShareLinks
Templates
```

with current classes including `PermissionAction`, `PermissionRule`, `PermissionScope`, `ResourcePermission`, `FieldPermission`, `ResourcePolicy`, `SharingPolicy`, `GuestAccessPolicy`, `WorkspacePolicy`, `CustomRole`, `MemberRoleAssignment`, `ShareLink`, and `PermissionTemplate`.

This is implementation evidence for a broad authorization/governance context. citeturn333630view0turn333630view1turn333630view2turn851615view0turn851615view1

---

# 98. Current ambiguity watch

Do not normalize these shortcuts if they appear in source:

```text
WorkspaceRole
→ universal permission

SpaceVisibility
→ complete resource security
  (visibility is one input of GOV-025, never the whole decision)

Billing plan
→ resource permission

share-link visibility
→ transitive public access

frontend hidden field
→ field security
```

Canonical Governance semantics remain richer.

---

# 99. GOV-024 — Authorization is compositional but deterministic

Multiple relevant facts may participate in one decision.

The result must still be deterministic, testable, explainable to operators/developers, and fail closed.

Avoid unbounded ad-hoc policy accumulation.

---

# 100. Testing/evidence

Critical evidence should cover:

```text
allow/deny
authentication vs authorization
Account/Workspace role distinctions
Owner/Admin/Member/Guest
cross-Workspace resource ID rejection
list/search/export filtering
custom role assignment
explicit permission grant/revoke
permission precedence
share-link create/expire/revoke
non-transitive sharing
field-level security where used
permission cache invalidation/version
realtime subscription revocation
audit creation/retention behavior
frontend permission states
```

---

# 101. Change impact — resource/action vocabulary

Review:

```text
all affected product contexts
Application authorization
API resources
frontend capability handling
stored roles/rules/templates
audit
cache
tests
```

---

# 102. Change impact — role/policy

Review:

```text
Accounts
Workspaces
custom roles
share links
guest behavior
frontend settings
permission cache
audit
```

---

# 103. Change impact — sharing

Review:

```text
resource owner
linked/embedded resources
public clients
realtime
search
export
expiry/revocation
audit
```

---

# 104. Change impact — audit

Review:

```text
retention/privacy
Operations
security incident response
Account/Workspace lifecycle
export/delete
frontend audit surfaces
```

---

# 105. Governance checklist

```text
[ ] protected resource owner is explicit
[ ] logical resource kind is stable
[ ] action vocabulary is explicit
[ ] subject type is explicit
[ ] Account/Workspace scope is explicit
[ ] membership is not mistaken for full authorization
[ ] entitlement is not mistaken for permission
[ ] query/list/search/export are protected
[ ] guest behavior is explicit
[ ] sharing is non-transitive unless explicitly designed
[ ] permission cache has scope/version
[ ] audit-sensitive actions are identified
```

---

# 106. New permission feature checklist

```text
[ ] owner context
[ ] resource
[ ] action
[ ] subject
[ ] scope
[ ] default/fallback
[ ] precedence/inheritance
[ ] cache invalidation
[ ] query behavior
[ ] realtime behavior
[ ] audit
[ ] frontend UX
[ ] migration
[ ] tests
```

---

# 107. Stop conditions

Stop rather than guess if:

- protected operation relies on frontend-only guard;
- handler hard-codes a role string without canonical policy justification;
- a query fetches unauthorized resources then hides them;
- Account/Workspace membership is being equated with full resource authorization;
- Billing entitlement is becoming permission;
- share link grants transitive hidden-resource access accidentally;
- permission cache lacks scope/version;
- audit is being treated as editable activity;
- resource/action vocabulary for a new protected capability is undefined;
- custom role/permission precedence is ambiguous.

---

# 108. Related canonical owners

```text
docs/product/product-model.md
docs/product/product-experience.md
docs/product/contexts/accounts.md
docs/product/contexts/identity.md
docs/product/contexts/workspaces.md
docs/product/contexts/work-management.md
docs/product/contexts/documents.md
docs/product/contexts/collaboration.md
docs/product/contexts/automation.md
docs/product/contexts/integrations.md
docs/product/contexts/billing.md

docs/architecture/contract-boundaries.md
docs/architecture/data-ownership-and-consistency.md
docs/architecture/events-realtime-and-delivery-boundary.md

backend/docs/architecture/security-tenancy-authorization.md
```

---

# 109. Final Governance rule

For every protected operation, Notrelix must be able to answer:

```text
Who is the principal?
Which Account/Workspace/resource is targeted?
Which context owns the resource?
What action is being attempted?
Which membership/role/team facts apply?
Which explicit permissions/policies apply?
Does a share capability apply?
Does Billing entitlement matter separately?
What happens if facts are stale/missing?
What must queries/realtime/export hide?
What audit evidence is required?
```

The target is:

> **one coherent, fail-closed authorization language that composes identity, membership, resource, policy, sharing, and entitlement facts without turning Governance into the owner of the protected business state.**

# Notrelix Product Development Thinking

> Product strategy and domain thinking for building Notrelix as an Enterprise SaaS work-management platform.
>
> This document defines how to reason about Account, Workspace, Space, membership, data isolation, permissions, and product growth. It is not an implementation ticket. It is a product/domain decision guide for future backend, frontend, and agent-driven development.

---

## 1. Core Product Principle

Notrelix must support different organization models without forcing every company into one rigid hierarchy.

Some companies are small and transparent. They may want one shared workspace where Product, Marketing, Engineering, Sales, and HR can see each other’s work but only edit their own areas.

Some companies are large, regulated, client-facing, or security-sensitive. They may need strict separation between departments, projects, clients, regions, or confidential teams.

Therefore, the product model must support both:

```txt
Account
  └── Workspace
        └── Space
              └── Board
                    └── Item
```

But the meaning of each level must be clear:

```txt
Account   = tenant / organization / billing boundary
Workspace = data isolation + collaboration boundary
Space     = organizational grouping inside a workspace
Board     = concrete work surface
Item      = unit of work
```

The system must not assume:

```txt
Department = Workspace
```

or:

```txt
Department = Space
```

Instead:

```txt
A department/team/project can be modeled as a Workspace when it needs strong data isolation.
A department/team/project can be modeled as a Space when it only needs organization and read/write permission differences.
```

---

## 2. Account

### 2.1 Meaning

An Account represents the organization, company, tenant, or customer boundary.

Examples:

```txt
Acme Company
Notrelix Internal
Agency Client A
University Department
Startup Team
```

Account is the top-level SaaS boundary for:

```txt
- billing
- subscription
- plan
- quota
- enterprise settings
- account-level roles
- account-level invitations
- tenant isolation
- account audit
- workspace grouping
```

Account is not just a container. It is a business aggregate.

---

### 2.2 Account is not shown everywhere in user-facing URLs

User-facing app routes should stay clean:

```txt
/w/{workspaceId}
/w/{workspaceId}/boards/{boardId}
```

AccountId is internal tenant context. It does not need to appear in normal app URLs.

However, account-scoped API actions may use account context explicitly:

```txt
POST /api/v1/accounts/{accountId}/workspaces
```

or use a trusted selected-account context:

```txt
POST /api/v1/workspaces
Header: X-Account-Id: {accountId}
```

Do not confuse:

```txt
User-facing product route
```

with:

```txt
Backend API route / tenant context
```

---

## 3. User, AccountMember, WorkspaceMember

## 3.1 User

A User is the global identity.

UserId answers:

```txt
Who is performing this action?
```

Used for:

```txt
- authentication
- audit
- actor in domain events
- permission evaluation
- membership lookup
- notifications
```

UserId does not tell us which account or workspace the request is operating in.

---

## 3.2 AccountMember

AccountMember is the relationship between:

```txt
AccountId + UserId
```

It answers:

```txt
Does this user belong to this account?
What account-level role/status does this user have?
```

It should contain:

```txt
AccountMember.Id
AccountMember.AccountId
AccountMember.UserId
AccountMember.Role
AccountMember.Status
```

It should not contain WorkspaceId.

AccountMember is used for account-level capabilities:

```txt
- CreateWorkspace
- ManageAccountSettings
- ManageBilling
- InviteAccountMember
- RemoveAccountMember
- ViewAccountWorkspaces
```

Important rule:

```txt
AccountMember verifies access to a selected account.
AccountMember must not be used to choose the current account with FirstOrDefault.
```

Wrong:

```txt
UserId -> AccountMembers.FirstOrDefault() -> AccountId
```

Correct:

```txt
SelectedAccountId -> verify AccountMember(AccountId, UserId) -> run account-scoped use case
```

---

## 3.3 WorkspaceMember

WorkspaceMember is the relationship between:

```txt
AccountId + WorkspaceId + UserId
```

It answers:

```txt
Does this user belong to this workspace?
What workspace-level role/status does this user have?
```

It should contain:

```txt
WorkspaceMember.Id
WorkspaceMember.AccountId
WorkspaceMember.WorkspaceId
WorkspaceMember.UserId
WorkspaceMember.Role
WorkspaceMember.Status
```

WorkspaceMember contains AccountId because workspace membership is still tenant-scoped data.

This supports:

```txt
- RLS
- tenant filtering
- permission versioning
- audit consistency
- event metadata
- preventing workspace/account mismatch
```

Required invariant:

```txt
WorkspaceMember.AccountId == Workspace.AccountId
```

Recommended enterprise rule:

```txt
WorkspaceMember should imply an AccountMember exists for the same AccountId + UserId.
```

This avoids users floating inside workspace-level access without account-level tenant membership.

---

## 4. Workspace

### 4.1 Meaning

Workspace is the primary data isolation and collaboration boundary inside an Account.

A Workspace is not merely a folder. It is a boundary for:

```txt
- member set
- permission scope
- workspace settings
- lifecycle
- activity feed
- search scope
- integrations
- archive/delete behavior
- client/external access
```

Workspace should be used when a group of work needs meaningful separation.

Examples:

```txt
Internal Company
Client Portal A
Product Division
Confidential R&D
Finance Confidential
Regional Office Vietnam
```

---

### 4.2 When to create separate Workspaces

Use separate Workspaces when:

```txt
- departments should not see each other’s data
- clients or external users are involved
- membership sets are very different
- data has compliance or confidentiality requirements
- lifecycle/archive behavior differs
- integrations differ
- search/activity must not be mixed
- audit boundary needs to be clear
```

Example:

```txt
Account: Enterprise Corp
  Workspace: Product Division
    Space: Roadmap
    Space: Sprint

  Workspace: Marketing Division
    Space: Campaigns
    Space: Content

  Workspace: Finance Confidential
    Space: Payroll
    Space: Budget
```

---

### 4.3 When not to create separate Workspaces

Do not create separate Workspaces just because teams have different names.

If teams can share visibility but need different edit rights, use one Workspace with Spaces and permissions.

Example:

```txt
Account: SmallTech
  Workspace: SmallTech HQ
    Space: Product
    Space: Marketing
    Space: Sales
```

Here, Product may be able to view Marketing but not edit Marketing boards.

That is a permission problem, not necessarily a workspace isolation problem.

---

## 5. Space

### 5.1 Meaning

Space is an organizational grouping inside a Workspace.

Space can represent:

```txt
- team
- department
- project area
- function
- squad
- initiative group
```

Examples:

```txt
Product
Marketing
Engineering
Sales
HR
Roadmap
Sprint
Campaigns
```

Space is the right choice when:

```txt
- the work belongs to the same workspace boundary
- users can share some visibility
- organization is needed in the sidebar
- read/write permission differs but full data isolation is not required
```

---

### 5.2 Space is not automatically a security boundary

A Space can have permissions, but it should not be assumed to provide the same isolation as a Workspace.

If a company needs strong data isolation, use separate Workspaces.

If a company only needs read/write differences, use Spaces with resource-level permission.

---

## 6. Board and Item

Board is the work surface.

Examples:

```txt
Product Roadmap
Sprint Backlog
Bug Tracking
Campaign Calendar
Hiring Pipeline
Content Pipeline
```

Item is the unit of work.

Examples:

```txt
Task
Bug
Feature
Campaign asset
Candidate
Approval request
```

Board should belong to a Workspace and optionally belong to a Space, depending on the final product model.

Recommended direction:

```txt
Board should be workspace-scoped and optionally space-scoped.
```

This supports both:

```txt
Workspace -> Board
```

and:

```txt
Workspace -> Space -> Board
```

without forcing every board to live under a Space too early.

---

## 7. Enterprise Usage Models

## 7.1 Small Company Model

A small company may use one workspace and multiple spaces.

```txt
Account: SmallTech
  Workspace: SmallTech HQ
    Space: Product
      Board: Roadmap
      Board: Bugs

    Space: Marketing
      Board: Campaign Calendar
      Board: Content Pipeline

    Space: Sales
      Board: Deals
```

Permission example:

```txt
Product team:
- View Product Space
- Edit Product Space
- View Marketing Space
- Cannot edit Marketing Space

Marketing team:
- View Marketing Space
- Edit Marketing Space
- View Product Space
- Cannot edit Product Space
```

This requires resource-level permissions, not multiple Workspaces.

---

## 7.2 Secure Enterprise Model

A larger enterprise may use separate Workspaces for isolation.

```txt
Account: Enterprise Corp
  Workspace: Product Department
    Space: Roadmap
    Space: Sprint

  Workspace: Marketing Department
    Space: Campaigns
    Space: Content

  Workspace: Finance Confidential
    Space: Payroll
    Space: Budget
```

In this model:

```txt
Product users cannot see Marketing unless explicitly added.
Marketing users cannot see Finance.
Finance search/activity/integrations are separate.
```

This is appropriate when data exposure risk matters.

---

## 7.3 Hybrid Model

Most real enterprises will use a hybrid.

```txt
Account: Acme Company
  Workspace: Company HQ
    Space: Product
    Space: Marketing
    Space: Engineering

  Workspace: Confidential Finance
    Space: Payroll
    Space: Budget

  Workspace: Client Portal A
    Space: Delivery
    Space: Feedback
```

This model supports both open collaboration and strict isolation.

---

## 8. Account Onboarding Flow

When a company signs up:

```txt
RegisterOrganization
  -> Create User
  -> Create Account
  -> Create AccountMember(UserId, AccountId, Owner)
  -> Optionally create default Workspace
  -> Optionally create WorkspaceMember(UserId, WorkspaceId, Owner)
```

The first user is usually:

```txt
Account Owner
```

They can then invite account-level managers.

---

## 9. Account Invitation Flow

Account invitation is different from Workspace invitation.

Account Owner/Admin invites people into the organization:

```txt
InviteAccountMember
  -> AccountInvitation
  -> AcceptAccountInvitation
  -> AccountMember(AccountId, UserId, Role)
```

AccountMember role controls account-level capabilities:

```txt
- CreateWorkspace
- ManageBilling
- ManageAccountSettings
- InviteAccountMember
- RemoveAccountMember
```

This is not the same as being inside a Workspace.

---

## 10. Workspace Creation Flow

Workspace creation is account-scoped.

Correct flow:

```txt
1. Request has selected AccountId from route/header/session/claim.
2. System verifies user is active AccountMember of selected AccountId.
3. Authorization checks PermissionAction.CreateWorkspace at Account scope.
4. Handler creates Workspace under selected AccountId.
5. Handler creates WorkspaceMember for creator as Owner.
```

Wrong flow:

```txt
UserId -> find first AccountMember -> use that AccountId -> create Workspace
```

AccountId must not be guessed.

---

## 11. Workspace Invitation Flow

Workspace invitation invites a user into a specific Workspace.

```txt
InviteWorkspaceMember
  -> WorkspaceInvitation(AccountId, WorkspaceId, Email, Role)
  -> AcceptWorkspaceInvitation
  -> Ensure AccountMember exists
  -> Create WorkspaceMember(AccountId, WorkspaceId, UserId, Role)
```

Recommended rule:

```txt
WorkspaceMember should not exist without AccountMember in the same Account.
```

If invited user is not yet an AccountMember, create a low-level AccountMember such as Member or Guest depending on product policy.

---

## 12. Permission Model Thinking

Do not use one broad permission for everything.

Avoid:

```txt
ManageWorkspace controls all member, settings, archive, invite, and board operations.
```

Prefer granular actions:

```txt
CreateWorkspace
ViewWorkspace
ManageWorkspace
ArchiveWorkspace
RestoreWorkspace
InviteMember
RemoveMember
ChangeMemberRole
ManageWorkspaceSettings
ManageSpaces
ViewSpace
ManageSpace
ViewBoard
CreateBoard
UpdateBoard
DeleteBoard
```

Permissions should be evaluated according to scope:

```txt
Account scope    -> account-level action, e.g. CreateWorkspace
Workspace scope  -> workspace-level action, e.g. InviteMember
Space scope      -> space-level action, e.g. ManageSpace
Board scope      -> board-level action, e.g. UpdateBoard
```

---

## 13. Read/Write Separation

Small businesses may want cross-team visibility without write access.

This should be modeled with permissions:

```txt
ViewSpace = true
ManageSpace = false
ViewBoard = true
UpdateBoard = false
```

Do not create separate Workspaces only to solve read/write differences.

Create separate Workspaces when the data should not be visible at all.

---

## 14. Routing and Identity

Canonical product routes should use stable IDs:

```txt
/w/{workspaceId}
/w/{workspaceId}/boards/{boardId}
```

Workspace slug is metadata, not canonical identity.

Workspace slug should be unique per account:

```txt
unique(account_id, slug)
```

Prefer partial unique index if soft-deleted slugs can be reused:

```sql
UNIQUE(account_id, slug) WHERE deleted_at IS NULL
```

Do not use global workspace slug as primary identity.

Do not expose accountId in normal app URLs unless product design requires it.

---

## 15. API Context Rule

Account-scoped mutations need account context.

Acceptable designs:

```txt
POST /api/v1/accounts/{accountId}/workspaces
```

or:

```txt
POST /api/v1/workspaces
X-Account-Id: {accountId}
```

or:

```txt
selectedAccountId in secure session/token
```

The backend must verify:

```txt
AccountMember(AccountId, UserId, Active)
```

before running account-scoped use cases.

---

## 16. Data Isolation Rules

Workspace is the main data isolation boundary inside Account.

Use Workspace when:

```txt
- users should not see data across boundaries
- search/activity should not mix
- external/client access differs
- compliance/audit differs
- lifecycle differs
```

Use Space when:

```txt
- data can exist in the same workspace
- organization is needed
- read/write permission differs
- shared dashboards/search are useful
```

Use resource permissions when:

```txt
- data is visible but not editable
- only some boards/spaces are sensitive
- owner/editor/viewer behavior is needed
```

---

## 17. Anti-patterns

Avoid these patterns:

```txt
UserId -> AccountMembers.FirstOrDefault() -> AccountId
```

```txt
WorkspaceMember used to resolve AccountId for account-scoped requests
```

```txt
AccountMember used as current account selector
```

```txt
Department always equals Workspace
```

```txt
Department always equals Space
```

```txt
Global unique workspace slug
```

```txt
WorkspaceSlugReservation table for normal workspace slugs
```

```txt
ResourceRef.Account(Guid.Empty)
```

```txt
ManageWorkspace used as catch-all for all workspace actions
```

```txt
Slug-based mutation endpoints secured after handler resolution
```

---

## 18. Development Rules for Coding Agents

When implementing a use case, always answer these questions first:

```txt
1. Is this Account-scoped, Workspace-scoped, Space-scoped, Board-scoped, or Global?
2. Where does AccountId come from?
3. Where does WorkspaceId come from?
4. Can the resource be resolved before AuthorizationBehavior?
5. Which membership table verifies access?
6. Which PermissionAction is required?
7. Does this mutation require ExpectedVersion?
8. Does this use case need a Domain method/event?
9. Does it cross bounded contexts?
10. Does it need integration event or only domain event?
```

If resource identity is not available before authorization, do not add permission markers blindly.

For slug-based mutation routes, either:

```txt
- resolve slug before authorization with a dedicated resolver behavior;
```

or:

```txt
- use canonical ID-based routes for mutations.
```

Do not half-secure slug-based commands.

---

## 19. Product Growth Direction

Recommended growth path:

```txt
Phase 1: Account + Workspace foundation
Phase 2: Workspace members and invitations
Phase 3: Spaces and basic boards
Phase 4: Resource-level permissions
Phase 5: Activity, notifications, search
Phase 6: Enterprise controls: audit, SSO, custom roles, client portals
```

Do not build advanced SpaceMember or complex ACL tables too early unless the use cases require them.

Start with:

```txt
AccountMember
WorkspaceMember
Workspace-level permissions
Resource permission model
```

Add SpaceMember only if Governance permission cannot express the needed product behavior cleanly.

---

## 20. Final Mental Model

The system should support this product philosophy:

```txt
Account gives the company a tenant.
Workspace gives the company a data boundary.
Space gives teams a way to organize work.
Permission gives the company read/write/manage control.
```

Small company:

```txt
One Account -> One Workspace -> Many Spaces
```

Secure enterprise:

```txt
One Account -> Many Workspaces -> Many Spaces
```

Hybrid enterprise:

```txt
One Account -> Shared Workspace + Confidential Workspaces + Client Workspaces
```

Notrelix must support all three without changing the core schema.

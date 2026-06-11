# AGENTS.md — Notrelix Enterprise Agent Guide

> This file is mandatory context for AI agents working in Notrelix.
>
> Read this before editing code. Follow `RULE.md` for hard rules and `SKILL.md` for execution mindset.

---

# 1. Product Identity

Notrelix is an enterprise work-management platform.

It is not:

```txt
- A Trello clone
- A simple Kanban app
- A CRUD dashboard
- A Notion clone
- A database-first experiment
```

It is a workspace operating system for teams:

```txt
Workspace
  → Space / Folder
    → Board
      → BoardField
      → BoardItem
      → BoardView
  → Documents
  → Collaboration
  → Governance
  → Automation
  → Integrations
  → Billing
```

Kanban, Calendar, Timeline, Table, Dashboard, and Form are views over the same work data. They are not separate data models.

---

# 2. Repository Thinking

When entering the repo, first identify the target area:

```txt
backend/Notrelix.Domain          Business model, invariants, domain events
backend/Notrelix.Application     Use cases, orchestration, authorization, DTOs
backend/Notrelix.Infrastructure  EF Core, PostgreSQL, Redis, outbox, providers
backend/Notrelix.Api             HTTP boundary only
frontend                         Next.js UI, routes, client state, API calls
```

Do not cross layer boundaries to make a quick fix.

Dependency rule:

```txt
API → Application → Domain
Infrastructure → Application → Domain
Domain → no outer layer
```

Domain must never reference:

```txt
EF Core
DbContext
HTTP
Redis
SignalR
S3/R2
Email/SMS providers
Search providers
Message brokers
Application handlers
DTOs
Controllers
```

---

# 3. Bounded Contexts

Target bounded contexts:

```txt
Identity
Workspaces
Governance
WorkManagement
Documents
Collaboration
Automation
Integrations
Billing
Analytics / Reporting optional
```

Technical modules that must not become core Domain:

```txt
Search
Operations
Outbox
JobLocks
Idempotency
SearchIndexJobs
Provider clients
EF configurations
API endpoints
Application handlers
DTOs
```

A bounded context may contain multiple aggregate roots.

Example:

```txt
Workspaces bounded context
├── Workspace
├── WorkspaceMember
├── WorkspaceInvitation
├── Space
└── Team
```

This is correct. Do not force everything under `Workspace` as child entities.

---

# 4. Domain Layer Rules for Agents

Domain is where business truth lives.

Domain should contain:

```txt
- Aggregate roots
- Entities
- Value Objects
- Domain Events
- Domain exceptions
- Pure Domain Rules
- State transitions
```

Domain should not contain:

```txt
- Repositories
- Queries to database
- EF attributes/configuration
- HTTP concepts
- DTOs
- Request/response models
- Provider SDK calls
- Search/indexing jobs
- Outbox persistence models
```

No `DateTime.UtcNow` or `DateTimeOffset.UtcNow` in Domain. Application supplies timestamps.

Correct:

```csharp
item.Rename(name, currentUserId, clock.UtcNow);
```

Wrong:

```csharp
UpdatedAt = DateTimeOffset.UtcNow;
```

---

# 5. Aggregate Boundary Rules

A class should be an `AggregateRoot` if it has:

```txt
- Independent lifecycle
- Business invariants
- Direct use cases
- Domain events
- Independent persistence/loading needs
```

A class should be a child entity if it only makes sense inside a parent aggregate.

Examples:

```txt
WorkspaceMember  AggregateRoot
Team             AggregateRoot
TeamMember       Child entity of Team
BoardField       AggregateRoot
FieldOption      Child entity of BoardField
BoardItem        AggregateRoot
BoardItemValue   Child entity of BoardItem
Page             AggregateRoot if it is document root
Block            AggregateRoot if edited independently
```

Aggregate roots reference other aggregate roots by ID, not object navigation.

Correct:

```csharp
public Guid WorkspaceId { get; private set; }
```

Avoid in Domain:

```csharp
public Workspace Workspace { get; private set; }
```

---

# 6. WorkManagement Rules

Core model:

```txt
Board       = work table/database
BoardField  = dynamic schema field/column
BoardItem   = row/task/item
BoardGroup  = table section/group
BoardView   = saved view config
```

Do not introduce legacy naming in new Domain code:

```txt
Card         → BoardItem
List         → BoardGroup
BoardColumn  → BoardField
Permission   → ResourcePermission
```

Kanban is a view. Kanban columns come from a `BoardField` in `BoardViewConfig`, usually Status/Select/People.

Dragging an item in Kanban changes a field value. It does not move a card to a list.

Board views do not own data. They store config only.

---

# 7. Documents Rules

Docs support work management first:

```txt
- Project specs
- Meeting notes
- Requirements
- Wiki pages
- Linked documents
```

Do not build a Google Docs clone before work-management core is stable.

Rules:

```txt
- Page tree must not cycle
- Block tree must not cycle
- Parent resources must belong to same workspace scope
- Deleted/archived resources cannot be edited unless restored
- BlockContent must validate against BlockType
```

If tree validation needs parent-chain data, Application loads the data and calls a pure Domain Rule.

---

# 8. Workspaces Rules

`Workspace` manages workspace metadata and lifecycle.

`WorkspaceMember` manages membership lifecycle.

`Space` manages workspace structure.

`Team` manages team membership.

`WorkspaceInvitation` manages invite lifecycle.

Workspace creation must always create an owner membership in the same use case/transaction, either through `WorkspaceFactory.CreateWithOwner` or Application orchestration.

Last-owner rules must be enforced using a Domain Rule supplied with owner count by Application:

```txt
- Cannot remove last owner
- Cannot downgrade last owner
- Cannot suspend last owner
```

Do not put a repository into Domain to count owners.

---

# 9. Governance Rules

Governance owns access control and audit concepts.

Resource permissions should use:

```txt
WorkspaceId
ResourceType
ResourceId
SubjectType
SubjectId
PermissionLevel
```

Permission checks should be centralized in Application services/behaviors. Do not hard-code permission checks randomly inside handlers.

Permission inheritance cache is a projection, not a Domain aggregate.

Audit log is append-only.

---

# 10. Collaboration Rules

Collaboration includes:

```txt
Comments
Mentions
Reactions
Notifications
Attachments
Activity
```

Rules:

```txt
- Workspace-scoped collaboration data must carry WorkspaceId
- No UtcNow in Domain
- Comment parent must belong to same target/workspace
- Reaction uniqueness is Target + User + Emoji
- Notification read/archive needs timestamp
- Attachments store metadata and object references, not binary content
```

---

# 11. Automation / Integrations / Billing Rules

Automation:

```txt
AutomationRule cannot be enabled without valid trigger/actions.
AutomationExecution must enforce state transitions:
Queued → Running → Succeeded/Failed/Cancelled
```

Integrations:

```txt
Never store raw secrets.
Use SecretRef or secret hash value objects.
Connection lifecycle includes disconnect/reconnect/expire/rotate secret.
```

Billing:

```txt
Plan is global catalog.
Do not fake WorkspaceId with Guid.Empty.
Subscription has lifecycle: start, change plan, schedule cancellation, renew, expire, cancel.
```

---

# 12. Application Layer Rules

Application orchestrates use cases.

Application may:

```txt
- Load aggregates
- Query counts/state needed for cross-aggregate rules
- Call Domain Rules
- Call aggregate methods
- Manage transaction boundaries
- Dispatch/collect domain events
- Enforce authorization
- Build DTOs
- Invalidate cache through abstractions
```

Application must not put business invariants only in handlers when they belong in Domain.

For cross-aggregate rules:

```txt
Application loads data → Domain Rule validates → Aggregate method changes state
```

---

# 13. Infrastructure Rules

Infrastructure implements persistence and providers.

Do not begin EF Core mapping until Domain boundary is stable.

Infrastructure owns:

```txt
- DbContext
- EF configurations
- PostgreSQL mappings
- Repositories
- Outbox persistence
- Redis
- Search indexing
- File storage
- Email/SMS/provider clients
- Background workers
```

Outbox persistence model is Infrastructure, not Domain.

Search documents and search indexing jobs are projections/jobs, not Domain aggregates.

---

# 14. API Rules

API is a thin boundary.

API should:

```txt
- Bind request
- Validate request shape
- Call Application command/query
- Return response/problem details
```

API should not:

```txt
- Directly mutate Domain entities
- Query DbContext for business workflows
- Implement permission logic directly
- Dispatch provider calls directly
```

---

# 15. Frontend Rules

Frontend must follow product model.

Do not hard-code board columns. Render by `BoardField` schema.

Do not build Kanban as a separate data model. Kanban uses the same `BoardItem` data and changes field values.

Keep API access in feature/lib layers, not scattered through UI components.

Use workspaceId routing for workspace-scoped resources.

---

# 16. Testing Rules

For every Domain change, prefer Domain tests.

Test:

```txt
- Valid state transitions
- Invalid state transitions
- Domain event payload
- Soft delete / restore
- Cross-aggregate rule behavior using supplied data
- No UtcNow behavior in refactored entities
```

Run targeted tests unless the user asks for full test suite.

---

# 17. Agent Behavior

When asked to implement:

1. Scan existing code.
2. Identify bounded context.
3. Identify aggregate root.
4. Identify invariant.
5. Implement Domain behavior first.
6. Add/update tests.
7. Only then move outward to Application/Infrastructure/API/UI.

When unsure, do not invent architecture. Ask or write a clearly marked assumption.

Do not claim the whole Domain is complete unless all bounded contexts have been audited and tests pass.

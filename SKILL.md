---
name: notrelix-enterprise-system-development
description: Use this skill when working on the Notrelix repository. Guides agents to design, refactor, test, and implement Notrelix as an enterprise work management platform using Clean Architecture, DDD, Modular Monolith, .NET backend, and Next.js frontend. Applies to domain modeling, application use cases, infrastructure persistence, API contracts, UI workflows, and system refactoring.
license: Project internal development guide
---

# SKILL.md — Notrelix Enterprise Development Skill

## Purpose

This skill defines how an AI coding agent should think and operate inside Notrelix.

Notrelix is not a CRUD app and not a Trello clone. It is an enterprise work-management SaaS inspired by Monday.com, Notion, Trello, and workspace collaboration tools.

Core product model:

```txt
Workspace
  → Space / Folder
    → Board
      → BoardField
      → BoardItem
      → BoardView
```

Docs, comments, notifications, automation, integrations, billing, and governance support the work-management core.

---

# 1. Operating Mode

Before editing code:

1. Read `AGENTS.md`.
2. Read `RULE.md`.
3. Inspect existing implementation before adding new code.
4. Identify the target layer: Domain, Application, Infrastructure, API, Frontend, Tests.
5. Make the smallest safe change that respects current architecture.
6. Do not rewrite unrelated modules.
7. Do not rename/delete large sets of files unless explicitly requested.

Always report:

```txt
- Files changed
- Business rule implemented
- Tests added/updated
- Commands run
- Remaining risks/deferred work
```

---

# 2. Architecture Thinking

Notrelix uses Clean Architecture + DDD + Modular Monolith.

Dependency direction:

```txt
API → Application → Domain
Infrastructure → Application → Domain
Domain → no outer layer
```

Domain must never depend on:

```txt
EF Core
DbContext
HTTP
Redis
SignalR
S3/R2
Email provider
Search provider
Message broker
Application handlers
DTOs
Controllers
```

If a business object needs data from another aggregate, Application queries that data, then passes the required value into a Domain method or Domain Rule.

Do not put repositories inside Domain.

---

# 3. Bounded Context Thinking

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

Technical modules that are not core Domain:

```txt
Search
Operations
Outbox
JobLocks
Idempotency
SearchIndexJobs
Provider clients
EF configurations
Controllers
DTOs
Handlers
```

A bounded context can contain multiple AggregateRoots.

Example:

```txt
Workspaces bounded context
├── Workspace          AggregateRoot
├── WorkspaceMember    AggregateRoot
├── Space              AggregateRoot
├── Team               AggregateRoot
└── WorkspaceInvitation AggregateRoot
```

Do not assume `Workspace` must own every object under a workspace. Use IDs between aggregate roots.

---

# 4. Domain Modeling Skill

Use Domain for:

```txt
- Business invariants
- State transitions
- Aggregate behavior
- Value Objects
- Domain Events
- Domain exceptions
- Cross-aggregate pure rules
```

Do not create data bags with public setters.

Correct pattern:

```csharp
public void Rename(string name, Guid updatedBy, DateTimeOffset updatedAt)
{
    EnsureNotDeleted();
    Guard.NotNullOrWhiteSpace(name);

    var normalizedName = name.Trim();
    if (Name == normalizedName) return;

    var oldName = Name;
    Name = normalizedName;
    SetAuditOnUpdate(updatedBy, updatedAt);
    AddDomainEvent(new ResourceRenamedEvent(Id, oldName, Name, updatedBy, updatedAt));
}
```

Avoid:

```csharp
entity.Name = request.Name;
entity.UpdatedAt = DateTimeOffset.UtcNow;
```

Domain must not call `DateTime.UtcNow` or `DateTimeOffset.UtcNow`. Application passes time into Domain.

---

# 5. Aggregate Boundary Skill

A class should be an AggregateRoot when it has:

```txt
- Independent lifecycle
- Business invariants
- Direct use cases
- Domain events
- Independent persistence/loading needs
```

A class should be an Entity child when it only makes sense inside its parent aggregate.

Examples:

```txt
WorkspaceMember  → AggregateRoot
Team             → AggregateRoot
TeamMember       → Entity child of Team
BoardField       → AggregateRoot
FieldOption      → Entity child of BoardField
BoardItem        → AggregateRoot
BoardItemValue   → Entity child of BoardItem
Page             → AggregateRoot if document root
Block            → AggregateRoot if edited independently
```

Do not embed aggregate roots inside other aggregate roots. Reference them by ID.

---

# 6. WorkManagement Skill

Never model Notrelix as:

```txt
Board → List → Card
```

Target language:

```txt
BoardField  = dynamic schema column
BoardItem   = row/task/item
BoardGroup  = table section/group, not Kanban column
BoardView   = saved view config, not data copy
```

Kanban is a view over `BoardItem`, grouped by a `BoardField` from `BoardViewConfig`.

Dragging a Kanban card must update a field value, not move it to a legacy `ListId`.

`board_items.values_json` is useful for flexible render. Query-heavy values must sync to typed `board_item_values`.

---

# 7. Documents Skill

Docs support work management first:

```txt
- Requirements
- Meeting notes
- Specs
- Wiki
- Linked documentation
```

Do not build a Google Docs clone too early.

Page/block tree rules:

```txt
- No cycles
- Parent must belong to same workspace/resource scope
- Deleted/archived resources cannot be edited unless explicitly restored
- BlockContent must validate against BlockType
```

If a tree rule needs parent-chain data, Application loads the chain and calls a pure Domain Rule. Domain must not query repositories.

---

# 8. Governance Skill

Governance handles permissions, audit, sharing, policies, and access rules.

Resource permissions should be generic:

```txt
ResourceType
ResourceId
SubjectType
SubjectId
PermissionLevel
WorkspaceId
```

Do not hard-code permission checks in random handlers. Use Application authorization behavior/services.

Permission inheritance cache is not a Domain aggregate. It is an Infrastructure/Application projection.

Audit logs are append-only and belong to compliance/security workflows.

---

# 9. Collaboration Skill

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
- Workspace-scoped collaboration data should include WorkspaceId.
- No UtcNow inside Domain.
- Comment thread parent must target same resource/workspace.
- Reaction uniqueness should be enforced per Target + User + Emoji.
- Notifications should record CreatedAt/ReadAt/ArchivedAt from Application time.
- Attachments store metadata and external object references, never binary file content.
```

---

# 10. Automation / Integrations / Billing Skill

Automation:

```txt
AutomationRule must not be enabled unless trigger/actions are valid.
AutomationExecution must enforce transitions:
Queued → Running → Succeeded/Failed/Cancelled
```

Integrations:

```txt
Never store raw secrets in Domain.
Use SecretRef / WebhookSecretHash.
Connection lifecycle should include disconnect, reconnect, expire, rotate secret.
```

Billing:

```txt
Plan is global catalog, not workspace resource.
Do not use Guid.Empty as fake WorkspaceId.
Subscription must validate period start/end and expose lifecycle behavior.
```

---

# 11. Testing Skill

For Domain changes, add Domain tests first or alongside implementation.

Test:

```txt
- State transitions
- Invalid transitions
- Domain events payload
- Soft delete / restore behavior
- Cross-aggregate rules using supplied data
- No UtcNow behavior where refactored
```

Do not run the entire solution unless requested. Prefer targeted tests.

---

# 12. Implementation Discipline

When asked to implement a feature:

1. Identify business concept.
2. Identify bounded context.
3. Identify aggregate root.
4. Add/modify Domain behavior.
5. Add Domain tests.
6. Add Application use case.
7. Add Infrastructure mapping/repository only after Domain is stable.
8. Add API endpoint last.
9. Update frontend only after contract is stable.

If Domain is not stable, do not start EF Core mapping yet.

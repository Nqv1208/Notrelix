# AGENTS.md — Notrelix Enterprise Agent Contract

> Mandatory context for every AI coding agent working in Notrelix.
>
> Read this file before editing. Then read the nearest scoped `AGENTS.md`, relevant `RULE.md` sections, and `SKILL.md`.
>
> `AGENTS.md` defines repository navigation, architectural boundaries, decision protocol, execution order, and verification obligations. `RULE.md` defines product/domain hard rules. `SKILL.md` defines execution mindset.

---

# 0. Instruction Precedence

When instructions conflict:

```txt
1. Explicit task/user instruction
2. Nearest scoped AGENTS.md
3. Root AGENTS.md
4. RULE.md
5. Context architecture/product documentation
6. SKILL.md
7. Existing code patterns
```

Existing code is evidence, not automatic precedent.

When source and documentation disagree:

```txt
- Inspect source, tests, callers, mappings, migrations, and current behavior.
- Distinguish current implementation from target architecture.
- Preserve compatibility unless the task intentionally changes the contract.
- Record the discrepancy.
- Do not silently choose one side.
```

Do not guess through a material product decision. Gather evidence, state the exact unresolved decision, and stop only at that boundary.

---

# 1. Product Identity

Notrelix is an enterprise work-management platform and workspace operating system.

It is not:

```txt
- A Trello clone
- A simple Kanban app
- A CRUD dashboard
- A Notion clone
- A database-first experiment
```

Core model:

```txt
Account
  → Workspace
      → optional Space/Folder organization
      → Board
          → BoardField
          → BoardItem
          → BoardGroup
          → BoardView
      → Documents
      → Collaboration
      → Governance
      → Automation
      → Integrations
      → Analytics
  → Billing and account administration
```

Important:

```txt
- A Board may exist directly under a Workspace or be organized by Space.
- Kanban, Calendar, Timeline, Table, Dashboard, and Form are views over work data.
- Views do not own separate BoardItem data.
- Plan is a global billing catalog.
- Accounts and Identity are not children of Workspace.
```

Read `PRODUCT.md` and relevant `RULE.md` sections before changing product semantics.

---

# 2. Repository Map

Use the real paths:

```txt
backend/
  src/
    Notrelix.Domain/
    Notrelix.Application/
    Notrelix.Infrastructure/
    Notrelix.Platform/
    Notrelix.API/
  tests/
    Notrelix.Domain.Tests/
    Notrelix.Application.Tests/
    Notrelix.Infrastructure.Tests/
    Notrelix.API.Tests/
    Notrelix.Integration.Tests/
    Notrelix.Architecture.Tests/
  backend.slnx

frontend/
  app/
  features/
  components/
  hooks/
  lib/
  types/
  ARCHITECTURE.md
  RULES.md

infra/
docs/
RULE.md
SKILL.md
```

Before editing:

```txt
1. Confirm the actual target path.
2. Read nearby README/ARCHITECTURE/RULES files.
3. Inspect current tests and direct callers.
4. Inspect git status.
5. Do not overwrite unrelated work.
```

---

# 3. Architecture

Notrelix is a modular monolith using Clean Architecture and DDD.

Dependency direction:

```txt
Notrelix.API
  → Notrelix.Application
  → Notrelix.Domain

Notrelix.Infrastructure
  → Notrelix.Application
  → Notrelix.Domain

Notrelix.Platform
  → Notrelix.Application
  → Notrelix.Domain

Notrelix.Domain
  → no outer project
```

Domain and Application must not depend on Platform.

Domain must never reference:

```txt
EF Core / DbContext
HTTP / ASP.NET Core
Redis / SignalR
S3/R2
Email/SMS/provider SDKs
Search providers
Message brokers
Application handlers/DTOs
Controllers/endpoints
Infrastructure persistence models
Platform runtime services
```

## Known transitional exceptions

Current source may contain transitional exceptions such as:

```txt
- Domain InternalsVisibleTo Infrastructure
- EF Core package references in Application
```

These are debt, not precedent.

Rules:

```txt
- Do not add new usages.
- Do not widen internal access.
- Do not add direct DbContext usage to new Application code.
- Inventory callers before removing an exception.
- Record owner and removal condition for any temporary exception.
```

---

# 4. Bounded Contexts and Capability Status

Current bounded contexts:

```txt
Accounts
Identity
Workspaces
Governance
WorkManagement
Documents
Collaboration
Automation
Integrations
Billing
Analytics
```

Supporting Domain modules:

```txt
Common
SharedKernel
```

Technical modules that must not become core Domain:

```txt
Platform
Search
Operations
Outbox
JobLocks
Infrastructure idempotency
SearchIndexJobs
Provider clients
EF configurations
API endpoints
Application handlers/DTOs
Background workers
```

A bounded context may contain multiple aggregate roots. Do not force all workspace-scoped resources into the `Workspace` aggregate.

Every capability is:

```txt
Frozen
Stabilizing
Experimental
```

Before changing a Domain capability:

```txt
- Inspect its effective capability status.
- Do not let a new namespace inherit Frozen accidentally.
- Do not mark a capability Frozen to silence a gate.
```

Changing a Frozen contract requires:

```txt
public API review
event logical-name/version review
rule-code review
caller/consumer inventory
persistence migration review
mutation scenario tests
snapshot diff review
certification impact report
```

---

# 5. Domain Contract

Domain contains:

```txt
Aggregate roots
Owned entities
Value Objects
Domain Events
Domain exceptions/rule codes
Pure Domain Rules
State transitions
Tenant-scope contracts
```

Domain does not contain:

```txt
Repositories
Database queries
EF mapping
DTOs
Provider calls
Search/index jobs
Outbox persistence
Runtime scheduling
Cache implementations
```

Cross-aggregate rule pattern:

```txt
Application loads required facts
→ builds an immutable Domain snapshot/path/context
→ Domain Rule validates
→ aggregate performs its own transition
```

Domain must not accept repository callbacks.

---

# 6. Aggregate Boundaries

Primary aggregate-root criterion:

```txt
A transactional consistency boundary that protects invariants.
```

Supporting evidence:

```txt
Independent lifecycle
Independent commands/use cases
Independent loading/concurrency needs
Stable identity
Domain events when consumed
```

Do not create an aggregate root only because a class has a table or event.

A child entity:

```txt
- Exists only within its root boundary.
- Is loaded/saved with the root.
- Cannot protect invariants independently.
- Must not expose public mutation that bypasses the root.
```

Aggregate roots reference other roots by ID, not mutable Domain navigation objects.

Correct:

```csharp
public Guid WorkspaceId { get; private set; }
```

Avoid:

```csharp
public Workspace Workspace { get; private set; }
```

---

# 7. Mutation Protocol

Every production mutation follows:

```txt
1. Validate lifecycle.
2. Validate actor and required IDs.
3. Validate business invariants.
4. Normalize input.
5. Detect semantic no-op.
6. Prepare audit timestamp/update.
7. Construct prospective children/value objects without attaching.
8. Mutate root/owned state.
9. Apply audit.
10. Increment Version exactly once.
11. Raise the approved event or follow the documented no-event contract.
```

Actor validation is separate from audit timestamp validation.

Correct:

```csharp
Guard.NotEmpty(updatedBy);

if (Name == normalizedName)
    return;

var audit = PrepareAuditUpdate(updatedBy, updatedAt);
```

Wrong:

```csharp
var audit = PrepareAuditUpdate(updatedBy, updatedAt);

if (Name == normalizedName)
    return;
```

Wrong:

```csharp
_children.Add(child);
var audit = PrepareAuditUpdate(updatedBy, updatedAt);
```

Wrong:

```csharp
child.Move(position);
var audit = PrepareAuditUpdate(updatedBy, updatedAt);
```

---

# 8. Failure Atomicity, No-op, Audit, and Version

A rejected mutation leaves unchanged:

```txt
Root state
Business status
Deletion state
Owned entities
Owned collections
Audit
Version
DomainEvents
```

A semantic no-op:

```txt
- Does not change state/audit/Version/events.
- Does not attach/remove/mutate children.
- Does not fail only because the timestamp is stale.
```

Required actor IDs and lifecycle/business preconditions may still be validated before no-op.

For successful persistent mutations:

```txt
- Application supplies actor and timestamp.
- Audit is applied only after all throwing validation succeeds.
- Version increments exactly once.
- No-op and rejection do not increment Version.
```

Infrastructure owns optimistic concurrency implementation. It must not bypass Domain Version.

---

# 9. Determinism and Identity

Domain must not use ambient nondeterminism:

```txt
DateTime.Now / UtcNow
DateTimeOffset.Now / UtcNow
Random.Shared
Environment-dependent business behavior
CurrentCulture-dependent business comparison
Thread culture
Network/filesystem/provider access
```

Application supplies time, actor, external facts, counts, parent paths, and approved random input.

Use ordinal or explicitly approved comparisons.

Follow the existing ID strategy. Do not introduce ad-hoc `Guid.NewGuid()` when a context has an established factory. Do not create a typed-ID wrapper for every persistence row by default; use typed identity where it protects aggregate or public-contract correctness.

---

# 10. Tenant Scope

Every aggregate and event has one scope:

```txt
Global
Account-scoped
Workspace-scoped
Hybrid
```

Rules:

```txt
- Required tenant IDs are non-empty.
- Tenant IDs are immutable.
- Nullable tenant IDs reject Guid.Empty when present.
- Cross-tenant references are rejected.
- Registry classification does not replace actual scope fields.
```

Event base must match the business fact:

```txt
Global fact     → GlobalDomainEvent
Account fact    → AccountScopedDomainEvent
Workspace fact  → WorkspaceScopedDomainEvent
Hybrid fact     → explicit hybrid contract and direct tests
```

Never emit a global event for a workspace-scoped mutation.

---

# 11. Domain Events

A Domain event:

```txt
- Represents a completed business fact.
- Is raised only after successful mutation.
- Is absent for no-op/rejection.
- Uses normalized persisted values.
- Carries correct tenant scope.
- Copies caller-owned collections.
- Contains no raw secrets/tokens.
- Has a stable logical event name.
- Is versioned when a Frozen payload changes.
```

Do not add an event only because a method mutates state.

Use an event when the fact is consumed by:

```txt
another bounded context
outbox/integration mapping
activity/audit projection
realtime projection
independent read model
```

Otherwise document the no-event contract or make the mutation root-owned/internal.

Do not rename logical event identities for style. CLR type name and logical event name are separate contracts.

---

# 12. Deletion and Lifecycle Policy

Soft delete is not a default strategy.

Every aggregate uses one explicit policy:

```txt
NotSupported
RecoverableDelete
ArchiveOnly
BusinessTerminationOnly
AppendOnly
OwnedRemoval
BusinessTombstone
```

For `RecoverableDelete`, `Delete/Restore` change only:

```txt
IsDeleted
DeletedAt
DeletedBy
DeleteReason
```

They must not change business status.

Forbidden:

```txt
Status = SoftDeleted
_statusBeforeDeletion
Delete → Status = Revoked
Restore → Status = Active
```

Use real business language:

```txt
Archive / Unarchive
Revoke / Expire
Cancel / Renew
Suspend / Activate
Remove
Resolve / Reopen
Watch / Unwatch
```

Append-only facts such as audit logs, issued invoices, usage facts, and reporting snapshots do not receive generic Delete/Restore.

Test delete/restore only for policies that support it. Do not require soft-delete tests for every Domain change.

---

# 13. Cross-Aggregate and Hierarchy Rules

Aggregate roots reference other roots by IDs and immutable contexts.

Hierarchy pattern:

```txt
Application loads parent/ancestor facts
→ constructs ParentPath/AncestorPath
→ Domain validates scope/cycle
→ Domain derives level/depth
→ aggregate mutates
```

Caller must not independently submit both a parent ID and a derived level/depth.

Stored paths/collections must be copied and validated.

Cross-aggregate uniqueness requires:

```txt
Application transactional check
+ Infrastructure unique constraint
```

Do not put a repository into Domain.

---

# 14. Value Objects and Configuration

Value Objects are:

```txt
Immutable
Validated at construction
Deterministically comparable
Safe from caller collection mutation
```

Typed closed configuration requires:

```txt
Strong types
Validation
Immutability
Deterministic equality
```

Opaque or polymorphic persisted JSON requires:

```txt
Discriminator/type
SchemaVersion
Object-root validation
Unknown discriminator rejection
Persistence round-trip tests
```

Do not add schema versioning based only on names such as `Config`, `Settings`, or `Payload`.

---

# 15. Product-Critical Context Rules

Detailed product rules remain in `RULE.md`. The following rules are mandatory navigation guards.

## WorkManagement

```txt
Board       = work table/database
BoardField  = dynamic schema field/column
BoardItem   = row/task/item
BoardGroup  = table section/group
BoardView   = saved view configuration
```

```txt
- Kanban is a view grouped by a compatible BoardField.
- Kanban drag changes a field value, not a legacy ListId.
- BoardView stores configuration, not data.
- BoardField defaults and options must be typed/validated.
- BoardItem hierarchy uses validated parent context and derived level.
- Complete and Reopen are separate operations.
```

## Workspaces

```txt
- Workspace, WorkspaceMember, WorkspaceInvitation, Space, and Team may be separate roots.
- Workspace creation and owner membership occur in one Application transaction.
- Last-owner count is loaded by Application and validated by a pure Domain Rule.
- WorkspaceMember removal is a business lifecycle, not generic deletion.
```

## Documents

```txt
- Page/Block trees cannot cycle.
- Parent/child scope must match.
- BlockContent validates against BlockType.
- Application supplies parent-chain context.
```

## Governance and Collaboration

```txt
- Permission decisions are centralized in Application.
- Audit facts are append-only.
- Permission caches are projections.
- Comment parent/target scope must match.
- Reaction uniqueness is Target + User + Emoji.
- Delete is not content redaction.
```

## Automation

Automation Domain may contain deterministic definitions, validators, and state transitions. Runtime provider execution, scheduling, retries, repository lookup, and external I/O do not belong in Frozen core Domain.

Activation invariant:

```txt
AutomationRule.Status == Active
⇒ Name is valid
⇒ Configuration exists
⇒ Trigger exists and its definition is valid
⇒ At least one required Action exists and every action definition is valid
```

Rules:

```txt
- Enable must enforce the activation invariant inside Domain before audit preparation or state mutation.
- Application/request validation does not replace this aggregate invariant.
- Use the pure AutomationRuleValidator/trigger/action validators; do not duplicate validation in handlers.
- Updating configuration while the rule is Active must validate the prospective configuration before assignment.
- A rejected Enable or active configuration update leaves Status, Configuration, audit, Version, and DomainEvents unchanged.
- Calling Enable on an already Active rule is a no-op; it does not revalidate timestamp, increment Version, or raise another event.
- Draft/Disabled incomplete-configuration policy must be explicit. Do not infer that every draft must already be activatable.
- AutomationExecution transitions are explicit: Queued → Running → Succeeded / Failed / Cancelled.
```

Correct transition order:

```txt
EnsureNotDeleted
→ validate actor
→ Active no-op
→ validate current/prospective configuration for activation
→ prepare audit
→ mutate Status/Configuration
→ apply audit
→ increment Version once
→ raise event
```

## Integrations, Billing, Analytics

```txt
- Integrations never store raw secrets.
- Delete/Restore does not revoke or reconnect integrations.
- Plan is global; Guid.Empty is not a fake WorkspaceId.
- Subscription lifecycle is explicit.
- Reporting snapshots are append-only and preserve SchemaVersion.
- Dashboard owned-state mutations are failure-atomic.
```

---

# 16. Application Layer

Application owns:

```txt
Use-case orchestration
Authorization
Aggregate loading
Cross-aggregate queries
Transaction boundaries
Idempotency coordination
Calling Domain Rules
Mapping commands/results
Coordinating event/outbox flow through abstractions
```

Application must not:

```txt
Mutate private Domain state
Leave aggregate invariants only in handlers
Add new direct DbContext usage
Contain provider SDK calls
Return EF entities as contracts
```

Authorization does not replace Domain invariants.

---

# 17. Infrastructure and Platform

Infrastructure owns:

```txt
DbContext and EF mappings
PostgreSQL
Repositories
Indexes/query filters
Outbox persistence
Redis/search
File storage
Provider clients
Workers
Migrations
```

Infrastructure must not invoke private Domain mutation methods through reflection.

Database constraints and query filters complement Domain invariants; they do not replace them.

`Notrelix.Platform` may depend on Application/Domain. Domain/Application must not depend on Platform.

Do not place business invariants in EF mappings, interceptors, provider adapters, workers, or composition code.

---

# 18. API and Frontend

API is a thin boundary:

```txt
bind request
validate shape
resolve auth context
call Application
translate result to HTTP/problem details
```

API must not mutate Domain, query DbContext for workflows, implement permission decisions, or call providers directly.

Before frontend changes, read:

```txt
frontend/ARCHITECTURE.md
frontend/RULES.md
frontend/README.md
```

Frontend guards:

```txt
- app/ owns route/layout/composition.
- features/ owns vertical feature slices.
- components/ui is generic and cannot import features.
- lib/ owns technical frontend infrastructure.
- Use centralized routes and permission evaluation.
- Render board schema dynamically.
- Kanban uses BoardItem field updates.
```

---

# 19. Persistence and Migration

Sequence:

```txt
Domain contract
→ Domain tests
→ Application callers
→ Infrastructure mapping
→ migration
→ API/frontend contracts
```

For data migration:

```txt
- Run preflight queries.
- Count affected rows.
- Define deterministic mapping.
- Fail on unknown legacy state.
- Do not guess defaults.
- Document retired enum values.
- Test indexes/query filters where relevant.
```

Do not create a database migration for a pure Domain type change when the existing converter/storage remains compatible.

Do not drop legacy storage before all readers/writers and rollback strategy are known.

---

# 20. Testing

For each changed Domain mutation, test applicable scenarios:

```txt
Success
NoOp
Rejected
FailureAtomicity
SideEffects
Lifecycle
```

Side effects include:

```txt
Audit
Version
Event or approved no-event behavior
Tenant scope
```

Per aggregate, test as applicable:

```txt
Creation
Tenant scope
Deletion policy
Owned-state encapsulation
Collection immutability
Persistence round-trip for versioned contracts
```

Do not add tests only to satisfy test count or coverage metadata.

Freeze gates must:

```txt
- Resolve exact mutation overloads.
- Enforce required scenarios.
- Exclude Stabilizing/Experimental contracts from Frozen snapshots.
- Fail closed when source/project compilation is unavailable.
- Never skip silently.
```

Use targeted tests while implementing. Run broader gates when changing public contracts, events, mappings, migrations, capability status, or snapshots.

---

# 21. Commands

From `backend/`:

```bash
dotnet build   src/Notrelix.Domain/Notrelix.Domain.csproj   -c Release   -warnaserror

dotnet test   tests/Notrelix.Domain.Tests/Notrelix.Domain.Tests.csproj   -c Release
```

Filtered:

```bash
dotnet test   tests/Notrelix.Domain.Tests/Notrelix.Domain.Tests.csproj   -c Release   --filter "FullyQualifiedName~TargetFixture"
```

Broader backend:

```bash
dotnet build backend.slnx -c Release
dotnet test -c Release
```

Docker workflow from repository root:

```bash
make be-build
make be-test
```

Frontend from `frontend/`:

```bash
bun run type-check
bun run lint
bun run test
bun run quality
bun run build
```

Before delivery:

```bash
git diff --check
git status --short
```

Never claim a command passed unless it was run.

---

# 22. Agent Workflow

Before implementation:

```txt
1. Read root and nearest scoped instructions.
2. Identify bounded context and capability status.
3. Identify aggregate consistency boundary.
4. Identify invariant and tenant scope.
5. Inspect implementation, tests, callers, events, mappings, and migrations.
6. Classify the change: internal behavior, public API, event, persistence, or Experimental.
7. Plan the smallest coherent change.
```

Implementation order:

```txt
Domain behavior
→ Domain tests
→ Application callers
→ Infrastructure mapping/migration
→ API/frontend
```

After implementation:

```txt
1. Run required tests/builds.
2. Review event/snapshot diffs.
3. Verify no skipped/failing architecture gate.
4. Review git diff/status.
5. Report exact results and remaining risks.
```

Keep unrelated refactors out of the change.

---

# 23. Decision Protocol

When a contract is unclear:

```txt
1. Inspect source and tests.
2. Inventory direct callers.
3. Inventory event/projection consumers.
4. Inspect mappings and existing data.
5. Read RULE.md and context docs.
6. Determine whether evidence supports one contract.
```

If one option is supported, implement and record evidence.

If multiple material product contracts remain valid:

```txt
- Do not implement past the decision boundary.
- State exact options.
- State compatibility/migration impact.
- Recommend one option.
- Mark the unresolved decision.
```

Examples:

```txt
Public event vs root-owned batch event
Recoverable delete vs archive-only
ParentId in creation event
Typed config vs versioned JSON
Reusable vs reserved unique key after deletion
```

---

# 24. Absolute Do Not

Never:

```txt
- Put repositories/providers in Domain.
- Add direct DbContext use to new Application code.
- Mutate state before all throwing validation completes.
- Expose child mutators that bypass the root.
- Use Status = SoftDeleted with IsDeleted.
- Add _statusBeforeDeletion to repair overwritten state.
- Restore revoked/expired/cancelled resources through generic Restore.
- Emit a global event for a workspace fact.
- Store raw secrets/tokens in Domain/events.
- Compare IDs using ToString formatting.
- Accept caller-controlled derived hierarchy level.
- Use ambient time/random/culture in Domain.
- Add an event only to make a gate green.
- Rename logical event identity for style.
- Mark a namespace Frozen to silence tests.
- Regenerate snapshots without reviewing diffs.
- Guess migration values.
- Run unrelated broad rewrites.
- Claim Domain Freeze without auditing all production capabilities and passing gates.
```

---

# 25. Delivery Report

Every implementation response/PR reports:

```txt
Baseline commit
Bounded context and capability status
Business invariant
Files changed/created/deleted
Public Domain signatures
Events and versions
Rule codes
Application caller migration
Infrastructure mapping/migration
Tests
Commands actually run and exact results
Snapshot changes
Remaining risks/decisions
```

Do not hide failures or unrun verification.

---

# 26. Definition of Done

A change is complete only when:

```txt
- Correct bounded context owns it.
- Aggregate boundary is preserved.
- Domain owns business invariants.
- Mutation order and failure atomicity are correct.
- Tenant scope is correct.
- Event/no-event behavior is intentional.
- Stored caller inputs are copied.
- Direct callers are migrated.
- Mapping/migration is compatible.
- Targeted and required broader gates pass.
- Documentation/snapshots match the contract.
- No unrelated change is included.
- Remaining uncertainty is explicit.
```

The Domain is Frozen only when every production capability is audited, no production capability remains Stabilizing, Experimental capabilities are isolated, all freeze gates pass without skips, and certification identifies the exact code being certified.

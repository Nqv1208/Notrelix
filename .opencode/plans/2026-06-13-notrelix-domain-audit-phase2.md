# Notrelix Domain Audit — Phase 2 (5 sub-phases)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix version/audit consistency, reclassify lifecycle entities to AggregateRoot, fix event semantics, add cross-resource validation, and introduce typed value objects in Notrelix.Domain.

**Architecture:** Sequential phases by dependency — version/audit first (foundation for concurrency), then AggregateRoot reclassification, then event semantics, then validation, then value objects last. Each phase produces compilable code with tests.

**Tech Stack:** .NET 10, C#, xUnit, FluentAssertions, Notrelix.Domain

**Phase Order:** 2.1 → 2.2 → 2.4 → 2.5 → 2.3

---

### Phase 2.1: Version + Audit Consistency

**Scope:** Add `IncrementVersion()`, missing `SetAuditOnUpdate()`, and missing events to existing AggregateRoots.

**Files Modified:**
- `backend/Notrelix.Domain/Identity/Users/User.cs`
- `backend/Notrelix.Domain/Identity/Profile/Events/UserProfileUpdatedEvent.cs`
- `backend/Notrelix.Domain/Workspaces/Members/WorkspaceMember.cs`
- `backend/Notrelix.Domain/Workspaces/Teams/Team.cs`
- `backend/Notrelix.Domain/Workspaces/Spaces/Space.cs`
- `backend/Notrelix.Domain/Automation/Executions/AutomationExecution.cs`
- `backend/Notrelix.Domain/Automation/Agents/AiAgentRun.cs`
- `backend/Notrelix.Domain/Billing/Plans/Plan.cs`
- `backend/Notrelix.Domain/Collaboration/Notifications/Notification.cs`
- `backend/Notrelix.Domain/Automation/Scheduled/ScheduledJob.cs`

#### Task 2.1.1: User.cs — all mutation methods

**Files:**
- Modify: `backend/Notrelix.Domain/Identity/Users/User.cs`

Add `IncrementVersion()` to each mutation:
- `UpdateProfile()` — also add `UserProfileUpdatedEvent`
- `UpdateEmail()` — also add IncrementVersion
- `UpdatePassword()` — also add IncrementVersion
- `RecordLogin()` — also add `SetAuditOnUpdate(updatedBy, updatedAt)` + IncrementVersion
- `Activate()` — also add IncrementVersion
- `Deactivate()` — also add IncrementVersion
- `Suspend()` — also add IncrementVersion
- `LinkOAuthAccount()` — also add IncrementVersion
- `UnlinkOAuthAccount()` — also add IncrementVersion
- `RotateOAuthToken()` — also add IncrementVersion

Pattern:
```csharp
SetAuditOnUpdate(updatedBy, updatedAt);
IncrementVersion();
AddDomainEvent(new XxxEvent(WorkspaceId, Id, updatedBy, updatedAt));
```

Also update `UserProfileUpdatedEvent` to include `WorkspaceId` and `UpdatedBy`:
```csharp
public sealed record UserProfileUpdatedEvent(Guid WorkspaceId, Guid UserId, Guid UpdatedBy, DateTimeOffset OccurredAt)
    : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
```

#### Task 2.1.2: WorkspaceMember.cs — 7 methods

**Files:**
- Modify: `backend/Notrelix.Domain/Workspaces/Members/WorkspaceMember.cs`

Add `IncrementVersion()` to: `ChangeRole`, `Suspend`, `Activate`, `Remove`, `SoftDelete`, `Restore`. Add `SetAuditOnUpdate` to SoftDelete/Restore if missing.

#### Task 2.1.3: Team.cs — 7 methods

**Files:**
- Modify: `backend/Notrelix.Domain/Workspaces/Teams/Team.cs`

Add `IncrementVersion()` to: `Rename`, `Archive`, `AddMember`, `RemoveMember`, `SoftDelete`, `Restore`. Add `SetAuditOnUpdate` to SoftDelete/Restore.

#### Task 2.1.4: Space.cs — 6 methods

**Files:**
- Modify: `backend/Notrelix.Domain/Workspaces/Spaces/Space.cs`

Add `IncrementVersion()` to: `Rename`, `Move`, `Archive`, `SoftDelete`, `Restore`. Add `SetAuditOnUpdate` to SoftDelete/Restore.

#### Task 2.1.5: AutomationExecution.cs — 6 methods

**Files:**
- Modify: `backend/Notrelix.Domain/Automation/Executions/AutomationExecution.cs`

Add `IncrementVersion()` to: `SetPayload`, `Start`, `Succeed`, `Fail`, `Cancel`.

#### Task 2.1.6: AiAgentRun.cs — 5 methods

**Files:**
- Modify: `backend/Notrelix.Domain/Automation/Agents/AiAgentRun.cs`

Add `IncrementVersion()` to: `Start`, `Succeed`, `Fail`, `Cancel`.

#### Task 2.1.7: Plan.cs — 5 methods

**Files:**
- Modify: `backend/Notrelix.Domain/Billing/Plans/Plan.cs`

Add `IncrementVersion()` to: `AddLimit`, `UpdateDescription`, `Archive`, `Deprecate`.

#### Task 2.1.8: Notification.cs — 3 methods

**Files:**
- Modify: `backend/Notrelix.Domain/Collaboration/Notifications/Notification.cs`

Add `IncrementVersion()` to: `MarkAsRead`, `Archive`.

#### Task 2.1.9: ScheduledJob.cs — Guid.Empty fix

**Files:**
- Modify: `backend/Notrelix.Domain/Automation/Scheduled/ScheduledJob.cs`

In `Cancel()`: change `SetAuditOnUpdate(Guid.Empty, cancelledAt)` to `SetAuditOnUpdate(null, cancelledAt)`.

#### Task 2.1.10: Phase 2.1 Tests

**Files:**
- Create: `backend/Notrelix.Tests/Domain/Phase2_1AuditTests.cs`

Write tests for each modified AggregateRoot:
- Mutation increments Version
- Mutation emits correct domain event
- Audit fields set correctly
- No-op calls skip version increment (if guarded)

---

### Phase 2.2: AggregateRoot Reclassification

**Scope:** Convert 7 classes to `AggregateRoot`.

**Files Modified:**
- `backend/Notrelix.Domain/Documents/Pages/Page.cs`
- `backend/Notrelix.Domain/Collaboration/Comments/Comment.cs`
- `backend/Notrelix.Domain/Collaboration/Attachments/Attachment.cs`
- `backend/Notrelix.Domain/WorkManagement/Relations/BoardRelation.cs`
- `backend/Notrelix.Domain/Governance/ShareLinks/ShareLink.cs`
- `backend/Notrelix.Domain/Governance/Roles/CustomRole.cs`
- `backend/Notrelix.Domain/Documents/ResourceLinks/ResourceLink.cs`

#### Task 2.2.1: Page → AggregateRoot

Change base: `SoftDeletableEntity, IWorkspaceScoped` → `AggregateRoot, IWorkspaceScoped`

Add to `Rename`, `Move`, `Archive`, `SoftDelete`, `Restore`:
```csharp
SetAuditOnUpdate(...);
IncrementVersion();
AddDomainEvent(new XxxEvent(...));
```

Events already exist from Phase 2 propagation.

#### Task 2.2.2: Comment → AggregateRoot

Change base. Add version/events to `UpdateContent`, `Resolve`, `SoftDelete`.

#### Task 2.2.3: Attachment → AggregateRoot

Change base. Add version/events to `SoftDelete`, `Restore`.

#### Task 2.2.4: BoardRelation → AggregateRoot

Change base to `AggregateRoot, IWorkspaceScoped`. Replace `Version++` → `IncrementVersion()` in `Pause`, `Resume`, `MarkBroken`. Add to `SoftDelete`.

#### Task 2.2.5: ShareLink → AggregateRoot

Change base: `AuditableEntity, IWorkspaceScoped` → `AggregateRoot, IWorkspaceScoped`. Add version/events to `Disable`, `RotateTokenHash`, `Expire`.

Note: AggregateRoot inherits SoftDeletableEntity, so ShareLink gains soft-delete. Evaluate if needed.

#### Task 2.2.6: CustomRole → AggregateRoot

Change base to `AggregateRoot, IWorkspaceScoped`. Add version/events to all 9 mutation methods.

#### Task 2.2.7: ResourceLink → AggregateRoot

Change base. Add version/event to `SoftDelete`.

#### Task 2.2.8: Phase 2.2 Tests

Create `Phase2_2AuditTests.cs`. For each reclassified entity:
- Base type is AggregateRoot
- Version starts at 1
- Each mutation increments Version
- Domain events emitted
- SoftDelete/Restore work correctly

---

### Phase 2.4: Event Semantic Cleanup

**Files:**
- Create 5 new event files
- Modify 5 mutation methods
- Create test file

#### Task 2.4.1–5: Create event files

Create each with standard pattern:
```csharp
public sealed record AutomationRuleRestoredEvent(Guid WorkspaceId, Guid RuleId, Guid RestoredBy, DateTimeOffset OccurredAt)
    : DomainEvent(OccurredAt, WorkspaceId, RestoredBy);
```

Files:
- `Automation/Executions/Events/AutomationRuleRestoredEvent.cs`
- `WorkManagement/Boards/Events/BoardUnarchivedEvent.cs`
- `Integrations/Connections/Events/IntegrationConnectionExpiredEvent.cs`
- `Documents/Blocks/Events/BlockRestoredEvent.cs`
- `Collaboration/Attachments/Events/AttachmentRestoredEvent.cs`

#### Task 2.4.6–10: Fix mutation methods

- `AutomationRule.Restore()`: replace `AutomationRuleCreatedEvent` with `AutomationRuleRestoredEvent`
- `Board.Unarchive()`: add `BoardUnarchivedEvent`
- `IntegrationConnection.MarkExpired()`: add `IntegrationConnectionExpiredEvent`
- `Block.Restore()`: add `BlockRestoredEvent`
- `Attachment.Restore()`: add `AttachmentRestoredEvent`

#### Task 2.4.11: Tests

Verify each event type is emitted.

---

### Phase 2.5: Validation

**Files:**
- `backend/Notrelix.Domain/Documents/ResourceLinks/ResourceLink.cs` — workspace mismatch guard
- `backend/Notrelix.Domain/Documents/Blocks/Block.cs` — cycle prevention
- `backend/Notrelix.Domain/WorkManagement/Items/BoardItem.cs` — cycle prevention

Pattern uses `getParentChain` delegate (supplied by Application) to keep Domain pure.

#### Tests

Phase2_5AuditTests.cs — test workspace mismatch, self-parent, cycle detection.

---

### Phase 2.3: Typed Value Objects

**Design-heavy phase. Full spec needed before implementation.**

Scope:
- `PermissionRule` string → enum (ScopeType, SubjectType, Action, Status)
- `AutomationRule` trigger/action/configuration → value objects with schema validation
- `ApiToken.ScopesJson` → `ApiTokenScopes` value object
- `SsoProvider.MetadataJson` → `SsoProviderConfiguration`
- `FormQuestion.QuestionType` → enum, `ConfigJson` → typed
- `DashboardWidget.Config` → validated per widget type
- `AiAgent` ModelPolicy/Instruction/ToolPermissions → value objects

---

## Execution

```
Phase 2.1 (9 tasks) → Phase 2.2 (9 tasks) → Phase 2.4 (11 tasks) → Phase 2.5 (4 tasks) → Phase 2.3 (8 tasks)
```

Each task: code → test → commit. Sequential (no parallel writes on same files).

## Verification

```bash
dotnet build backend/Notrelix.Domain/Notrelix.Domain.csproj && dotnet test backend/Notrelix.Tests/Domain/Notrelix.Domain.Tests.csproj --filter "Phase2"
```

Expected: 0 errors, ~440-460 total tests.

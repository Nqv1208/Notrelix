# Domain ↔ Schema Classification (Phase 0 Audit)

> **Status**: Complete  
> **Date**: 2026-06-11  
> **Target**: `backend/Notrelix.Domain/` + `docs/backend/notrelix-domain-schema-v3-clean-ddd.sql` (106 tables)  
> **Purpose**: Classify every SQL table into Domain archetype and identify gaps, anti-patterns, and missing concepts.

---

## Classification Scheme

| # | Archetype | Criteria |
|---|-----------|----------|
| 1 | **Core Domain AggregateRoot** | Independent lifecycle, business invariants, direct use cases, domain events, independent persistence |
| 2 | **Entity child** | Only meaningful inside parent aggregate; no independent lifecycle |
| 3 | **Value Object** | Immutable, no identity, owned by parent |
| 4 | **Domain Rule / Event** | Pure business validation logic or past-tense domain event (outbox-dispatched) |
| 5 | **Projection / Read Model** | Cached/derived data, rebuildable from sources, no write behavior |
| 6 | **Infrastructure persistence model** | Outbox, worker state, EF shadow properties — not Domain |
| 7 | **Operations / Technical table** | Idempotency, job locks, import/export — app-layer concerns |
| 8 | **Partition child table** | Physical partition of parent — no Domain class |

---

## 1. `identity` Schema (12 tables)

### Classification

| # | Table | Domain Class | Archetype | Domain Location | Status | Notes |
|---|-------|-------------|-----------|----------------|--------|-------|
| 1 | `identity.users` | `User` | **Entity** (AuditableEntity) → should be **AggregateRoot** | `Identity/Users/User.cs` | ⚠️ Misclassified | Has own lifecycle, events, DbSet, independent loading — meets all AggregateRoot criteria |
| 2 | `identity.user_profiles` | `UserProfile` | **Entity** (child of User) | `Identity/Profiles/UserProfile.cs` | ✅ Mapped | EF config: `UserProfileConfiguration.cs` |
| 3 | `identity.user_credentials` | — | **MISSING** | — | ❌ Missing | No Domain entity found. Schema has `password_hash`, `password_changed_at`, `mfa_enabled`. Should be Entity child of User or Value Object. |
| 4 | `identity.oauth_accounts` | `OAuthAccount` | **Entity** (child of User) | `Identity/OAuth/OAuthAccount.cs` | ⚠️ Anti-pattern | Raw `AccessToken`/`RefreshToken` strings not wrapped in VO |
| 5 | `identity.user_sessions` | `UserSession` | **AggregateRoot** | `Identity/Sessions/UserSession.cs` | ✅ Mapped | EF config: `UserSessionConfiguration.cs`. Uses `Guid.Empty` in events ❌ |
| 6 | `identity.user_login_attempts` | `LoginAttempt` | **Entity** (child of User) | `Identity/Security/SecurityEntities.cs` | ⚠️ No EF config | No EF configuration file found |
| 7 | `identity.user_settings` | — | **MISSING** or `UserSecuritySettings`/`UserPreferences` | — | ⚠️ Unclear | `UserPreferences` (VO in Profiles/) could map; `UserSecuritySettings` (Entity) could also map. No EF config for either. |
| 8 | `identity.password_reset_tokens` | `PasswordResetToken` | **AggregateRoot** | `Identity/Credentials/CredentialTokens.cs` | ⚠️ No EF config | No EF configuration file found |
| 9 | `identity.email_verification_tokens` | `EmailVerificationToken` | **AggregateRoot** | `Identity/Credentials/CredentialTokens.cs` | ⚠️ No EF config | No EF configuration file found |
| 10 | `identity.email_change_tokens` | — | **MISSING** | — | ❌ Missing | No Domain entity found |
| 11 | `identity.mfa_devices` | `MfaMethod` | **AggregateRoot** | `Identity/Security/SecurityEntities.cs` | ⚠️ No EF config | No EF configuration file found |
| 12 | `identity.mfa_recovery_codes` | — | **MISSING** | — | ❌ Missing | No Domain entity found |

### Anti-patterns
- `OAuthAccount`: `AccessToken` and `RefreshToken` stored as raw strings, not wrapped in Value Objects
- `UserSession`: domain events use `Guid.Empty` for session ID (should be real GUID from caller)
- `UserProfile`: uses `DateTime.UtcNow` instead of `DateTimeOffset.UtcNow` — inconsistent with rest of Domain

---

## 2. `workspace` Schema (6 tables)

### Classification

| # | Table | Domain Class | Archetype | Domain Location | Status | Notes |
|---|-------|-------------|-----------|----------------|--------|-------|
| 13 | `workspace.workspaces` | `Workspace` | **AggregateRoot** | `Workspaces/Workspaces/Workspace.cs` | ✅ Mapped | Factory in `WorkspaceFactory.cs` creates owner member |
| 14 | `workspace.workspace_members` | `WorkspaceMember` | **AggregateRoot** | `Workspaces/Members/WorkspaceMember.cs` | ✅ Mapped | Transitions: Active→Suspended→Active, role changes, soft delete |
| 15 | `workspace.workspace_invitations` | `WorkspaceInvitation` | **AggregateRoot** | `Workspaces/Invitations/WorkspaceInvitation.cs` | ✅ Mapped | Status lifecycle: Pending→Accepted/Revoked/Expired |
| 16 | `workspace.spaces` | `Space` | **AggregateRoot** | `Workspaces/Spaces/Space.cs` | ✅ Mapped | Tree structure: parent_space_id, position |
| 17 | `workspace.teams` | `Team` | **AggregateRoot** | `Workspaces/Teams/Team.cs` | ✅ Mapped | Status: Active→Archived |
| 18 | `workspace.team_members` | `TeamMember` | **Entity** (child of Team) | `Workspaces/Teams/TeamMember.cs` | ✅ Mapped | Role: Member, Lead |

### Domain Rules
- `WorkspaceOwnerRules`: last-owner protection (data supplied by Application)
- `WorkspaceInvitationRules`: expiry, revocation, acceptance rules
- `WorkspaceMemberRules`: suspension, role downgrade constraints
- `TeamRules`, `SpaceRules`

---

## 3. `governance` Schema (12 tables)

### Classification

| # | Table | Domain Class | Archetype | Domain Location | Status | Notes |
|---|-------|-------------|-----------|----------------|--------|-------|
| 19 | `governance.resource_permissions` | `ResourcePermission` | **AggregateRoot** | `Governance/Permissions/ResourcePermission.cs` | ✅ Mapped | EF config exists |
| 20 | `governance.field_permissions` | `FieldPermission` | **Entity** | `Governance/Permissions/FieldPermission.cs` | ⚠️ No EF config | Not registered in DbContext |
| 21 | `governance.share_links` | `ShareLink` | **Entity** (AuditableEntity) | `Governance/ShareLinks/ShareLink.cs` | ✅ Mapped | EF config exists |
| 22 | `governance.audit_logs` | `AuditLog` | **Entity** | `Governance/Audit/AuditLog.cs` | ✅ Mapped | EF config exists. Partitioned by `occurred_at` |
| 23 | `governance.security_events` | `SecurityEvent` | **AggregateRoot** | `Governance/Security/SecurityEvent.cs` | ✅ Mapped | EF config exists |
| 24 | `governance.workspace_policies` | `WorkspacePolicy` | **Entity** (AuditableEntity) | `Governance/Policies/WorkspacePolicy.cs` | ✅ Mapped | EF config exists; owned VOs: `SharingPolicy`, `ResourcePolicy`, `GuestAccessPolicy` |
| 25 | `governance.custom_roles` | `CustomRole` | **Entity** (SoftDeletableEntity) | `Governance/Roles/CustomRole.cs` | ✅ Mapped | EF config exists |
| 26 | `governance.custom_role_permissions` | `CustomRolePermission` | **Entity** (child of CustomRole) | `Governance/Roles/CustomRolePermission.cs` | ✅ Stored as JSON | Stored in `custom_roles.permissions` JSONB column |
| 27 | `governance.workspace_member_role_assignments` | `MemberRoleAssignment` | **Entity** | `Governance/Roles/MemberRoleAssignment.cs` | ⚠️ No EF config | No EF configuration or DbSet found |
| 28 | `governance.permission_templates` | `PermissionTemplate` | **AggregateRoot** | `Governance/Templates/PermissionTemplate.cs` | ⚠️ No EF config | No EF configuration found |
| 29 | `governance.audit_retention_policies` | `AuditRetentionPolicy` | **Entity** | `Governance/Audit/AuditRetentionPolicy.cs` | ⚠️ No EF config | Not registered in DbContext |
| 30 | `governance.resource_permission_inheritance_cache` | — | **Projection / Read Model** | — | ❌ No Domain class | Schema comment confirms: "Not a core Governance aggregate. Rebuildable from source permissions and policies." |

### Notes
- `custom_role_permissions` is stored as JSONB column inside `custom_roles` — correct for Domain design (no separate table needed)
- `resource_permission_inheritance_cache` should NOT have a Domain entity — it's a cache/projection

---

## 4. `work` Schema (23 tables)

### Classification

| # | Table | Domain Class | Archetype | Domain Location | Status | Notes |
|---|-------|-------------|-----------|----------------|--------|-------|
| 31 | `work.boards` | `Board` | **AggregateRoot** | `WorkManagement/Boards/Board.cs` | ✅ Mapped | EF config exists. Owned VOs: `BoardSettings` |
| 32 | `work.board_groups` | `BoardGroup` | **Entity** (child of Board) | `WorkManagement/BoardGroups/BoardGroup.cs` | ✅ Mapped | EF config exists |
| 33 | `work.board_fields` | `BoardField` | **AggregateRoot** | `WorkManagement/Fields/BoardField.cs` | ✅ Mapped | EF config exists. Owned VO: `FieldSettings` |
| 34 | `work.field_options` | `FieldOption` | **Entity** (child of BoardField) | `WorkManagement/Fields/FieldOption.cs` | ✅ Mapped | EF config exists |
| 35 | `work.board_views` | `BoardView` | **AggregateRoot** | `WorkManagement/Views/BoardView.cs` | ✅ Mapped | EF config exists. Owned config VOs: `TableViewConfig`, `KanbanViewConfig`, `CalendarViewConfig`, `TimelineViewConfig` |
| 36 | `work.board_view_user_preferences` | — | **MISSING** | — | ❌ Missing | No Domain entity found per user view preferences |
| 37 | `work.board_items` | `BoardItem` | **Entity** (SoftDeletableEntity) → should be **AggregateRoot** | `WorkManagement/Items/BoardItem.cs` | ⚠️ Misclassified | Has own lifecycle, events, DbSet — meets all AggregateRoot criteria |
| 38 | `work.board_item_values` | `BoardItemValue` | **Entity** (child of BoardItem) | `WorkManagement/Items/BoardItemValue.cs` | ✅ Mapped | EF config exists |
| 39 | `work.board_item_members` | `BoardItemMember` | **Entity** (child of BoardItem) | `WorkManagement/Items/BoardItemMember.cs` | ✅ Mapped | EF config exists |
| 40 | `work.labels` | `Label` | **AggregateRoot** | `WorkManagement/Labels/Label.cs` | ✅ Mapped | EF config exists |
| 41 | `work.board_item_labels` | `BoardItemLabel` | **Entity** (child of BoardItem) | `WorkManagement/Items/BoardItemLabel.cs` | ✅ Mapped | EF config exists |
| 42 | `work.board_item_links` | `BoardItemLink` | **Entity** (child of BoardItem) | `WorkManagement/Items/BoardItemLink.cs` | ✅ Mapped | EF config exists |
| 43 | `work.checklists` | `Checklist` | **AggregateRoot** | `WorkManagement/Checklists/Checklist.cs` | ✅ Mapped | EF config exists |
| 44 | `work.checklist_items` | `ChecklistItem` | **Entity** (child of Checklist) | `WorkManagement/Checklists/Checklist.cs` | ✅ Mapped | EF config exists |
| 45 | `work.saved_filters` | — | **MISSING** | — | ❌ Missing | No Domain entity found |
| 46 | `work.relation_field_configs` | `RelationFieldConfig` | **Entity** (child of BoardField) | `WorkManagement/Relations/RelationFieldConfig.cs` | ⚠️ No EF config | No EF configuration found |
| 47 | `work.formula_dependencies` | `FormulaDependency` | **Entity** (child of BoardField) | `WorkManagement/Formulas/FormulaDependency.cs` | ⚠️ No EF config | No EF configuration found |
| 48 | `work.rollup_snapshots` | `RollupSnapshot` | **Entity** (child of BoardItem) | `WorkManagement/Rollups/RollupSnapshot.cs` | ⚠️ No EF config | No EF configuration found. Could also be Projection/Read Model. |
| 49 | `work.approval_requests` | `ApprovalRequest` | **AggregateRoot** | `WorkManagement/Approvals/ApprovalRequest.cs` | ⚠️ No EF config | No EF configuration found |
| 50 | `work.approval_steps` | `ApprovalStep` | **Entity** (child of ApprovalRequest) | `WorkManagement/Approvals/ApprovalRequest.cs` | ⚠️ No EF config | No EF configuration found |
| 51 | `work.workload_allocations` | `WorkloadAllocation` | **Entity** | `WorkManagement/Workload/WorkloadAllocation.cs` | ⚠️ No EF config | No EF configuration found |
| 52 | `work.board_templates` | `BoardTemplate` | **AggregateRoot** | `WorkManagement/Templates/Templates.cs` | ⚠️ No EF config | No EF configuration found |
| 53 | `work.item_templates` | `ItemTemplate` | **AggregateRoot** | `WorkManagement/Templates/Templates.cs` | ⚠️ No EF config | No EF configuration found |

### Key Findings
- `BoardItem` is classified as SoftDeletableEntity instead of AggregateRoot — should be fixed (has own events, lifecycle, DbSet)
- `saved_filters` and `board_view_user_preferences` have no Domain entity — schema shows they exist in work schema
- 8 WorkManagement tables have Domain entities but no EF config yet: `RelationFieldConfig`, `FormulaDependency`, `RollupSnapshot`, `ApprovalRequest`, `ApprovalStep`, `WorkloadAllocation`, `BoardTemplate`, `ItemTemplate`

---

## 5. `docs` Schema (5 tables)

### Classification

| # | Table | Domain Class | Archetype | Domain Location | Status | Notes |
|---|-------|-------------|-----------|----------------|--------|-------|
| 54 | `docs.pages` | `Page` | **Entity** (SoftDeletableEntity) → should be **AggregateRoot** | `Documents/Pages/Page.cs` | ⚠️ Misclassified | Per AGENTS.md: "Page should be AggregateRoot if it is document root" |
| 55 | `docs.blocks` | `Block` | **AggregateRoot** | `Documents/Blocks/Block.cs` | ✅ Mapped | EF config exists. Owned VOs: `BlockContent`, `BlockProperties` |
| 56 | `docs.document_versions` | `DocumentVersion` | **AggregateRoot** | `Documents/Versions/DocumentVersion.cs` | ⚠️ No EF config | Owned VO: `DocumentSnapshot` |
| 57 | `docs.resource_links` | `ResourceLink` | **AggregateRoot** | `Documents/ResourceLinks/ResourceLink.cs` | ⚠️ No EF config | Schema shows `docs.resource_links` table; Domain has entity but no EF config |
| 58 | `docs.page_templates` | `PageTemplate` | **AggregateRoot** | `Documents/Templates/PageTemplate.cs` | ⚠️ No EF config | Schema shows `docs.page_templates` table |

### Key Findings
- **Only 2 of 5 tables have EF config**: `pages` (Page) and `blocks` (Block)
- `Page` should be AggregateRoot, not SoftDeletableEntity
- `ResourceLink` in Domain is at `Documents/ResourceLinks/` — maps to `docs.resource_links` table
- `PageTemplate` in Domain maps to `docs.page_templates`

---

## 6. `collab` Schema (11 tables + 5 partitions)

### Classification

| # | Table | Domain Class | Archetype | Domain Location | Status | Notes |
|---|-------|-------------|-----------|----------------|--------|-------|
| 59 | `collab.comments` | `Comment` | **Entity** (SoftDeletableEntity) → should be **AggregateRoot** | `Collaboration/Comments/Comment.cs` | ⚠️ Misclassified | Has own lifecycle, events, DbSet — meets AggregateRoot criteria |
| 60 | `collab.reactions` | `Reaction` | **Entity** | `Collaboration/Reactions/Reaction.cs` | ✅ Mapped | Owned VO: `Emoji` |
| 61 | `collab.mentions` | `Mention` | **Entity** | `Collaboration/Mentions/Mention.cs` | ✅ Mapped | EF config: `MentionConfiguration.cs` |
| 62 | `collab.notifications` | `Notification` | **AggregateRoot** | `Collaboration/Notifications/Notification.cs` | ✅ Mapped | EF config: `NotificationConfiguration.cs`. Status: Unread→Read→Archived |
| 63 | `collab.notification_preferences` | — | **MISSING** | — | ❌ Missing | No Domain entity found. Schema has channel-level toggle, quiet_hours_json |
| 64 | `collab.notification_deliveries` | — | **MISSING** or **Infrastructure** | — | ❌ Missing | Could be Infrastructure (delivery tracking) or Domain entity. Currently absent. |
| 65 | `collab.unread_counters` | — | **MISSING** or **Projection** | — | ❌ Missing | Could be Projection/Read Model (denormalized counter). Currently absent. |
| 66 | `collab.activity_logs` (partitioned) | `ActivityLog` | **AggregateRoot** | `Collaboration/Activity/ActivityLog.cs` | ✅ Mapped | EF config: `ActivityLogConfiguration.cs`. Partitioned by `occurred_at`. Owned VO: `ActivityMetadata` |
| 67 | `collab.attachments` | `Attachment` | **AggregateRoot** | `Collaboration/Attachments/Attachment.cs` | ✅ Mapped | EF config: `AttachmentConfiguration.cs`. Owned VO: `FileMetadata` |
| 68 | `collab.resource_watchers` | `ResourceWatcher` | **AggregateRoot** | `Collaboration/Watchers/ResourceWatcher.cs` | ⚠️ No EF config | No EF configuration or DbSet found |
| 69 | `collab.presence_sessions` | `PresenceSession` | **Entity** | `Collaboration/Presence/PresenceSession.cs` | ⚠️ No EF config | No EF configuration or DbSet found. Likely Redis-backed in practice. |
| — | `collab.activity_logs_y2025m01` | — | **Partition child** | — | N/A | Physical partition of `activity_logs` |
| — | `collab.activity_logs_y2025m06` | — | **Partition child** | — | N/A | Physical partition of `activity_logs` |
| — | `collab.activity_logs_y2025m07` | — | **Partition child** | — | N/A | Physical partition of `activity_logs` |
| — | `collab.activity_logs_y2026` | — | **Partition child** | — | N/A | Physical partition of `activity_logs` |
| — | `collab.activity_logs_default` | — | **Partition child** | — | N/A | Physical partition of `activity_logs` (catch-all) |

### Key Findings
- `Comment` should be AggregateRoot (not SoftDeletableEntity alone)
- 3 missing Domain entities: `notification_preferences`, `notification_deliveries`, `unread_counters`
- `notification_deliveries` could be classified as Infrastructure persistence (delivery tracking) rather than Core Domain
- `unread_counters` is a denormalized counter — could be Projection/Read Model
- `presence_sessions` is likely Redis-backed at runtime; Domain entity exists but may not need PostgreSQL mapping
- Partition children need no Domain classes

---

## 7. `automation` Schema (5 tables)

### ⚠️ Schema-Domain Model Mismatch

The SQL schema stores triggers/conditions/actions as **JSONB columns** inside `automation_rules`:
- `trigger_json` (jsonb)
- `conditions_json` (jsonb)
- `actions_json` (jsonb)

But the Domain model uses **separate entity tables**:
- `AutomationTrigger` → separate table
- `AutomationCondition` → separate table
- `AutomationAction` → separate table

This needs resolution: either update Domain to match schema (JSONB columns in `automation_rules`) or update schema to match Domain (separate tables).

Similarly:
- `automation_executions` has `input_json`/`result_json` in schema, but Domain has `AutomationExecutionStep` as separate entity/table.

### Classification

| # | Table | Domain Class | Archetype | Domain Location | Status | Notes |
|---|-------|-------------|-----------|----------------|--------|-------|
| 70 | `automation.automation_rules` | `AutomationRule` | **AggregateRoot** | `Automation/Rules/AutomationRule.cs` | ⚠️ Schema mismatch | Schema uses JSONB columns; Domain uses separate trigger/action/condition tables |
| 71 | `automation.automation_executions` | `AutomationExecution` | **AggregateRoot** | `Automation/Executions/AutomationExecution.cs` | ⚠️ Schema mismatch | Domain adds `AutomationExecutionStep` as separate table not in schema |
| 72 | `automation.scheduled_jobs` | `ScheduledJob` | **AggregateRoot** | `Automation/Scheduled/ScheduledJob.cs` | ⚠️ No EF config | Domain entity exists but no EF config found for `ScheduledJobs` |
| 73 | `automation.automation_templates` | `AutomationTemplate` | **AggregateRoot** | `Automation/Templates/AutomationTemplate.cs` | ⚠️ No EF config | Same schema mismatch as rules |
| 74 | `automation.outbox_messages` | — | **Infrastructure persistence model** | — | ❌ No Domain class needed | Schema comment confirms: "OutboxMessage is not a Domain entity; Domain only raises IDomainEvent." Domain writes events but outbox table is Infrastructure concern. |

### Domain Tables Created (not in schema)
The Domain creates these entities that have no corresponding schema table:
- `AutomationTrigger` → no `automation_triggers` table in schema
- `AutomationAction` → no `automation_actions` table in schema
- `AutomationCondition` → no `automation_conditions` table in schema
- `AutomationExecutionStep` → no `automation_execution_steps` table in schema

### Resolution Required
Choose one approach:
1. **Keep schema JSONB**: Collapse `AutomationTrigger`, `AutomationAction`, `AutomationCondition` into owned VOs on `AutomationRule`
2. **Keep Domain separate tables**: Add `automation_triggers`, `automation_actions`, `automation_conditions`, `automation_execution_steps` to schema

---

## 8. `integration` Schema (9 tables)

### Classification

| # | Table | Domain Class | Archetype | Domain Location | Status | Notes |
|---|-------|-------------|-----------|----------------|--------|-------|
| 75 | `integration.integration_connections` | `IntegrationConnection` | **AggregateRoot** | `Integrations/Connections/IntegrationConnection.cs` | ✅ Mapped | Owned VOs: `SecretRef`. Status: Active→Disabled/Error/Deleted. EF config exists. |
| 76 | `integration.integration_scopes` | `IntegrationScope` | **Entity** (child of IntegrationConnection) | `Integrations/Connections/IntegrationConnection.cs` | ✅ Mapped | Child entity, stored inline or owned |
| 77 | `integration.integration_secret_versions` | `IntegrationSecretVersion` | **Entity** (child of IntegrationConnection) | `Integrations/Connections/IntegrationConnection.cs` | ✅ Mapped | Child entity with `SecretRef` VO |
| 78 | `integration.webhook_subscriptions` | `WebhookSubscription` | **AggregateRoot** | `Integrations/Webhooks/WebhookSubscription.cs` | ✅ Mapped | Owned VO: `WebhookSecretHash`. EF config exists. |
| 79 | `integration.webhook_deliveries` | `WebhookDelivery` | **AggregateRoot** | `Integrations/Webhooks/WebhookDelivery.cs` | ✅ Mapped | EF config exists |
| 80 | `integration.inbound_webhook_events` | `InboundWebhookEvent` | **AggregateRoot** | `Integrations/Webhooks/InboundWebhookEvent.cs` | ✅ Mapped | Stores raw incoming webhook payload |
| 81 | `integration.calendar_integrations` | `CalendarIntegration` | **AggregateRoot** | `Integrations/Calendar/CalendarIntegration.cs` | ✅ Mapped | EF config exists |
| 82 | `integration.calendar_event_links` | `CalendarEventLink` | **Entity** (child of CalendarIntegration) | `Integrations/Calendar/CalendarIntegration.cs` | ✅ Mapped | Child entity |
| 83 | `integration.integration_sync_cursors` | `IntegrationSyncCursor` | **Entity** | `Integrations/Sync/IntegrationSyncCursor.cs` | ✅ Mapped | Owned VO: `SyncCursorValue` |

### Notes
- Integrations is the **most complete** bounded context — all 9 tables have Domain entities mapped
- `SecretRef` VO correctly wraps the secret reference, not storing raw secrets
- Calendar has both `CalendarIntegration` (AggregateRoot) and `CalendarEventLink` (Entity) — matches schema

---

## 9. `billing` Schema (9 tables)

### Classification

| # | Table | Domain Class | Archetype | Domain Location | Status | Notes |
|---|-------|-------------|-----------|----------------|--------|-------|
| 84 | `billing.plans` | `Plan` | **AggregateRoot** | `Billing/Plans/Plan.cs` | ⚠️ Anti-pattern | `PlanCrm` method uses `Guid.Empty` for workspace ID ❌ |
| 85 | `billing.plan_limits` | `PlanLimit` | **Entity** (child of Plan) | `Billing/Plans/Plan.cs` | ✅ Mapped | Owned VO: `FeatureCode` |
| 86 | `billing.subscriptions` | `Subscription` | **AggregateRoot** | `Billing/Subscriptions/Subscription.cs` | ✅ Mapped | Status lifecycle: Trialing→Active→PastDue/Cancelled/Expired |
| 87 | `billing.payment_methods` | `PaymentMethod` | **AggregateRoot** | `Billing/Payments/PaymentMethod.cs` | ✅ Mapped | Unique constraint: one default per workspace |
| 88 | `billing.invoices` | `Invoice` | **AggregateRoot** | `Billing/Payments/Invoice.cs` | ✅ Mapped | Status: Draft→Open→Paid/Void/Uncollectible |
| 89 | `billing.billing_events` | `BillingEvent` | **AggregateRoot** | `Billing/Events/BillingEvent.cs` | ✅ Mapped | Incoming provider webhook events. Owned VO: `ProviderEventId` |
| 90 | `billing.usage_metrics` | `UsageMetric` | **AggregateRoot** | `Billing/Usage/UsageMetric.cs` | ✅ Mapped | Owned VOs: `UsageMetricKey`, `UsagePeriod` |
| 91 | `billing.usage_metric_history` | `UsageMetricHistory` | **Entity** | `Billing/Usage/UsageMetricHistory.cs` | ✅ Mapped | Periodic snapshots of usage |
| 92 | `billing.entitlements` | `Entitlement` | **AggregateRoot** | `Billing/Entitlements/Entitlement.cs` | ✅ Mapped | Source: Plan/Override/Trial/Promotion. Coupled with Subscription. |

### Anti-patterns
- `Plan.PlanCrm()` uses `Guid.Empty` for `WorkspaceId` — should accept real workspace ID
- Billing is otherwise well-structured with 9/9 tables mapped

---

## 10. `reporting` Schema (3 tables)

### Classification

| # | Table | Domain Class | Archetype | Domain Location | Status | Notes |
|---|-------|-------------|-----------|----------------|--------|-------|
| 93 | `reporting.dashboards` | `Dashboard` | **AggregateRoot** | `Analytics/Dashboards/Dashboard.cs` | ✅ Mapped | Status: Active→Archived/Deleted |
| 94 | `reporting.dashboard_widgets` | `DashboardWidget` | **Entity** (child of Dashboard) | `Analytics/Dashboards/Dashboard.cs` | ✅ Mapped | Owned VOs: `WidgetConfig`, `WidgetPosition` |
| 95 | `reporting.reporting_snapshots` | `ReportingSnapshot` | **Entity** | `Analytics/Snapshots/ReportingSnapshot.cs` | ✅ Mapped | Could also be classified as Projection/Read Model |

---

## 11. `search` Schema (2 tables)

### Classification

| # | Table | Domain Class | Archetype | Domain Location | Status | Notes |
|---|-------|-------------|-----------|----------------|--------|-------|
| 96 | `search.search_documents` | — | **Projection / Read Model** | — | ❌ No Domain class needed | Schema comment: "Search projection table. Rebuildable from source resources via indexing jobs/outbox events." |
| 97 | `search.search_index_jobs` | — | **Infrastructure persistence model** | — | ❌ No Domain class needed | Schema comment: "Technical indexing queue. Not a Domain aggregate." |

### Correct Classification
- `search_documents` is a **projection** of core domain data for full-text search — no Domain entity needed
- `search_index_jobs` is **infrastructure** — indexing queue — no Domain entity needed

---

## 12. `ops` Schema (4 tables)

### Classification

| # | Table | Domain Class | Archetype | Domain Location | Status | Notes |
|---|-------|-------------|-----------|----------------|--------|-------|
| 98 | `ops.idempotency_keys` | — | **Operations / Technical** | — | ❌ No Domain class needed | Idempotency storage for API/Application — not Domain |
| 99 | `ops.import_jobs` | — | **Operations / Technical** | — | ❌ No Domain class needed | Import job tracking — application-layer concern |
| 100 | `ops.export_jobs` | — | **Operations / Technical** | — | ❌ No Domain class needed | Export job tracking — application-layer concern |
| 101 | `ops.job_locks` | — | **Operations / Technical** | — | ❌ No Domain class needed | Background worker lock storage — not Domain |

### Correct Classification
All 4 `ops.*` tables are correctly excluded from Domain — they are app-layer/infrastructure concerns.

---

## Summary Statistics

### By Archetype

| Archetype | Count | Tables |
|-----------|-------|--------|
| **Core Domain AggregateRoot** | 47 | See individual schemas above |
| **Entity (child)** | 23 | field_options, board_groups, board_item_values, etc. |
| **Value Object** | 28+ | Owned by parent aggregates across all contexts |
| **Domain Rule / Event** | 120+ events, 30+ rules | Static rule classes, sealed record events |
| **Projection / Read Model** | 3 | `resource_permission_inheritance_cache`, `search_documents`, `unread_counters` (candidate) |
| **Infrastructure persistence** | 2 | `outbox_messages`, `search_index_jobs` |
| **Operations / Technical** | 4 | `idempotency_keys`, `import_jobs`, `export_jobs`, `job_locks` |
| **Partition child** | 5 | `activity_logs_y2025m01`, `y2025m06`, `y2027`, `y2026`, `default` |

### By Schema

| Schema | Tables | Mapped (✅) | No EF Config (⚠️) | Missing (❌) | Schema Mismatch (⚠️) |
|--------|--------|-------------|--------------------|-------------|----------------------|
| `identity` | 12 | 4 | 4 | 3 | 0 |
| `workspace` | 6 | 6 | 0 | 0 | 0 |
| `governance` | 12 | 6 | 4 | 0 | 0 |
| `work` | 23 | 12 | 8 | 2 | 0 |
| `docs` | 5 | 2 | 2 | 0 | 0 |
| `collab` | 11 | 5 | 2 | 3 | 0 |
| `automation` | 5 | 0 | 1 | 0 | 2 |
| `integration` | 9 | 9 | 0 | 0 | 0 |
| `billing` | 9 | 9 | 0 | 0 | 0 |
| `reporting` | 3 | 3 | 0 | 0 | 0 |
| `search` | 2 | 0 (N/A) | 0 | 0 | 0 |
| `ops` | 4 | 0 (N/A) | 0 | 0 | 0 |

### Totals
| Metric | Count |
|--------|-------|
| **Total tables** | 101 (106 minus 5 partition children) |
| **✅ Fully mapped (Domain + EF config)** | 56 |
| **⚠️ Has Domain entity, no EF config** | 22 |
| **❌ Missing from Domain** | 8 |
| **⚠️ Schema-Domain mismatch** | 2 (automation) |
| **❌ No Domain class needed** | 9 (search + ops + heritage cache + outbox) |

---

## Cross-Cutting Issues

### 1. Misclassified Aggregate Roots (4 entities)

These extend `SoftDeletableEntity` or `AuditableEntity` directly but should extend `AggregateRoot`:

| Current Class | Current Base | Should Be | Rationale |
|--------------|-------------|-----------|-----------|
| `BoardItem` | `SoftDeletableEntity` | `AggregateRoot` | Own lifecycle, events, DbSet, independent loading |
| `Page` | `SoftDeletableEntity` | `AggregateRoot` | Per AGENTS.md: document root, own lifecycle |
| `Comment` | `SoftDeletableEntity` | `AggregateRoot` | Own lifecycle, events, DbSet, independent loading |
| `User` | `AuditableEntity` | `AggregateRoot` | Own lifecycle, events, DbSet, independent loading |

### 2. Missing Domain Entities (8 tables)

| Schema | Table | Suggested Classification | Priority |
|--------|-------|------------------------|----------|
| `identity` | `user_credentials` | **Entity** (child of User) or **Value Object** | High |
| `identity` | `email_change_tokens` | **AggregateRoot** or **Entity** | High |
| `identity` | `mfa_recovery_codes` | **Entity** (child of User/MfaMethod) | Medium |
| `collab` | `notification_preferences` | **Entity** or **Value Object** | Medium |
| `collab` | `notification_deliveries` | **Infrastructure** or **Entity** | Low |
| `collab` | `unread_counters` | **Projection / Read Model** | Low |
| `work` | `board_view_user_preferences` | **Entity** (child of BoardView) | Low |
| `work` | `saved_filters` | **Entity** (child of Board/User) | Low |

### 3. Anti-Patterns in Domain Layer

| Severity | Location | Issue | Fix |
|----------|----------|-------|-----|
| 🔴 High | 22 files across Domain | `DateTimeOffset.UtcNow` / `DateTime.UtcNow` in factory/create methods | Accept `DateTimeOffset` from caller (Application layer) |
| 🔴 High | 9 domain events | `Guid.Empty` used for entity IDs in event payloads | Pass real GUIDs from the process manager |
| 🟡 Medium | `OAuthAccount` | Raw `AccessToken` and `RefreshToken` strings | Wrap in `TokenHash` or `EncryptedToken` Value Object |
| 🟡 Medium | `Plan.PlanCrm` | `Guid.Empty` for workspace ID | Accept actual workspace ID parameter |
| 🟡 Medium | `UserSession` events | `Guid.Empty` for session ID in events | Accept real session ID from caller |
| 🟡 Medium | `UserProfile` | Uses `DateTime.UtcNow` (not `DateTimeOffset`) | Replace with `DateTimeOffset.UtcNow` |
| 🟢 Low | `Domain/Events` subfolder | `Billing/Events/` named `Events` instead of bounded context | Acceptable — events are inbound billing webhooks, not Domain events |

### 4. Schema-Domain Alignment Issues

| Issue | Schema | Domain | Resolution Needed |
|-------|--------|--------|-------------------|
| Automation triggers | JSONB column `trigger_json` in `automation_rules` | Separate `AutomationTrigger` entity/table | Align one way or the other |
| Automation conditions | JSONB column `conditions_json` | Separate `AutomationCondition` entity/table | Align one way or the other |
| Automation actions | JSONB column `actions_json` | Separate `AutomationAction` entity/table | Align one way or the other |
| Execution steps | No separate table | `AutomationExecutionStep` entity/table | Add to schema or remove from Domain |

---

## Recommendations (Priority Order)

### Tier 1 — Fix Anti-Patterns (highest risk)
1. Remove all `DateTimeOffset.UtcNow`/`DateTime.UtcNow` from Domain factories — accept from caller
2. Remove all `Guid.Empty` from domain event payloads — pass real GUIDs
3. Wrap `AccessToken`/`RefreshToken` in `OAuthAccount` with Value Objects

### Tier 2 — Fix Misclassified Aggregate Roots
4. Change `BoardItem`, `Page`, `Comment`, `User` to extend `AggregateRoot`

### Tier 3 — Add Missing Domain Entities
5. Add `UserCredential` entity or merge into `User`
6. Add `EmailChangeToken` entity
7. Add `MfaRecoveryCode` entity (child of `MfaMethod`)
8. Add `NotificationPreference` entity
9. Add `NotificationDelivery` entity (or mark as Infrastructure)
10. Add `UnreadCounter` projection (or mark as Infrastructure)
11. Add `BoardViewUserPreference` entity
12. Add `SavedFilter` entity

### Tier 4 — Resolve Schema Mismatches
13. Align Automation model: either collapse to JSONB or add separate schema tables
14. Align Execution steps model

### Tier 5 — Add Missing EF Configurations (22 entities)
15. Add EF configurations for all entities listed as ⚠️ "No EF config"

---

## Files Referenced

| File | Purpose |
|------|---------|
| `docs/backend/notrelix-domain-schema-v3-clean-ddd.sql` | Target SQL schema (2800 lines, 106 tables) |
| `Identity/**/*.cs` | Identity bounded context (32 files) |
| `Workspaces/**/*.cs` | Workspaces bounded context (47 files) |
| `WorkManagement/**/*.cs` | WorkManagement bounded context (115 files) |
| `Governance/**/*.cs` | Governance bounded context (49 files) |
| `Documents/**/*.cs` | Documents bounded context (33 files) |
| `Collaboration/**/*.cs` | Collaboration bounded context (36 files) |
| `Automation/**/*.cs` | Automation bounded context (48 files) |
| `Billing/**/*.cs` | Billing bounded context (89 files) |
| `Integrations/**/*.cs` | Integrations bounded context (119 files) |
| `Analytics/**/*.cs` | Analytics bounded context (129 files) |
| `Common/**/*.cs` | Base classes, guards, exceptions (14 files) |
| `SharedKernel/**/*.cs` | Cross-cutting Value Objects (12 files) |

# Domain Implementation Notes

## Phase 0: Architecture Lock

### Bounded Contexts
*   Identity
*   Workspaces
*   Governance
*   WorkManagement (Core)
*   Documents
*   Collaboration
*   Automation
*   Integrations
*   Billing
*   Analytics (Optional)

### Aggregate Roots per Context
*   **Identity**: User, UserSession, OAuthAccount, MfaMethod, PasswordResetToken, EmailVerificationToken
*   **Workspaces**: Workspace, WorkspaceMember, WorkspaceInvitation, Space, Team
*   **Governance**: ResourcePermission, ShareLink, CustomRole, WorkspacePolicy, AuditLog, SecurityEvent, PermissionTemplate
*   **WorkManagement**: Board, BoardGroup, BoardField, BoardItem, BoardView, Label, Checklist, BoardTemplate, ItemTemplate, ApprovalRequest
*   **Documents**: Page, Block, DocumentVersion, ResourceLink, PageTemplate
*   **Collaboration**: Comment, Notification, ActivityLog, Attachment, ResourceWatcher
*   **Automation**: AutomationRule, AutomationExecution, AutomationTemplate
*   **Integrations**: IntegrationConnection, WebhookSubscription, WebhookDelivery, CalendarIntegration
*   **Billing**: Plan, Subscription, Invoice, BillingEvent, UsageMetric, Entitlement

### Excluded Classes from Domain Core
*   `search.search_documents` -> Infrastructure/Search
*   `search.search_index_jobs` -> Infrastructure/Search
*   `ops.idempotency_keys` -> Infrastructure/Operations
*   `ops.job_locks` -> Infrastructure/BackgroundJobs
*   `automation.outbox_messages` -> Infrastructure/Outbox
*   `governance.resource_permission_inheritance_cache` -> Infrastructure/Governance
*   `reporting.reporting_snapshots` -> Infrastructure/Reporting

### Core Rules
*   No "1 table = 1 aggregate" mappings.
*   Entities must use private setters and expose behavior methods.
*   Invariants must be inside the Domain methods.
*   Domain events must be raised within Domain methods (e.g., `AddDomainEvent`).
*   Soft Delete is a business behavior (`SoftDelete(...)` and `Restore(...)`).
*   No cross-context tight coupling (use `Guid` or `ValueObject` references).
*   Permission checks belong in the Application layer, not in Domain entities or hard-coded roles.
